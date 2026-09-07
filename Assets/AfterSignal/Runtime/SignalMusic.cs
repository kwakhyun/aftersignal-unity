using UnityEngine;

namespace AfterSignal
{
    // A separate persistent transport keeps music running across scene loads.
    // Two streaming voices provide both track crossfades and overlapping loop restarts.
    public sealed class SignalMusic : MonoBehaviour
    {
        public static SignalMusic Instance { get; private set; }
        public const float DefaultLevel = .55f;
        public const float TrackFadeSeconds = 1.8f;
        public const float LoopFadeSeconds = 2f;
        readonly AudioSource[] voices = new AudioSource[2];
        readonly MusicCue[] cues = new MusicCue[2];
        readonly AudioClip[] clips = new AudioClip[6];
        readonly float[] weights = new float[2];
        ResourceRequest loading;
        MusicCue loadingCue, wanted;
        int active;
        float fadeElapsed, fadeDuration, outgoingStart, duck = 1, master = .45f;
        bool hasRequest, started, fading, paused, bossEngaged;
        GameDirector owner;
        public MusicCue CurrentCue => cues[active];
        public AudioSource CurrentSource => voices[active];
        public bool IsPaused => paused;
        public bool IsFading => fading;
        public int LoopCount { get; private set; }
        public float MusicLevel { get; private set; } = DefaultLevel;

        public static SignalMusic Ensure()
        {
            if (!Instance) new GameObject("MUSIC / persistent streaming soundtrack").AddComponent<SignalMusic>();
            return Instance;
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MusicLevel = Mathf.Clamp01(PlayerPrefs.GetFloat("AFTERSIGNAL.Unity.MusicVolume", DefaultLevel));
            for (int i = 0; i < 2; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].loop = false;
                voices[i].spatialBlend = 0;
                voices[i].priority = 32;
                voices[i].volume = 0;
            }
        }

        public void SetMasterVolume(float value) { master = Mathf.Clamp01(value); ApplyVolumes(); }
        public void SetMusicVolume(float value)
        {
            MusicLevel = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("AFTERSIGNAL.Unity.MusicVolume", MusicLevel);
            ApplyVolumes();
        }
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            foreach (var voice in voices) { if (value) voice.Pause(); else voice.UnPause(); }
        }
        public void Request(MusicCue cue) { wanted = cue; hasRequest = true; }

        void Update()
        {
            var game = GameDirector.Instance;
            float duckTarget = 1;
            if (game && game.Ready)
            {
                if (owner != game) { owner = game; bossEngaged = false; }
                // Latch engagement so retreating across the activation boundary cannot restart the boss cue.
                if (game.stage == StageId.Roof && game.Boss && game.Boss.Active) bossEngaged = true;
                Request(MusicCatalog.Select(game.stage, UrbanCatalog.Current, bossEngaged,
                    game.stage == StageId.Roof && game.Boss && !game.Boss.Alive));
                SetMasterVolume(game.Audio.Volume);
                SetPaused(game.Paused || game.Dead);
                duckTarget = game.Dialogue || (CityLife.Instance && CityLife.Instance.Mode.Length > 0) ? .45f : 1;
            }
            if (paused) return;
            float dt = Time.unscaledDeltaTime;
            duck = Mathf.MoveTowards(duck, duckTarget, dt * 1.8f);
            if (loading != null && loading.isDone)
            {
                clips[(int)loadingCue] = loading.asset as AudioClip;
                if (!clips[(int)loadingCue])
                {
                    Debug.LogError("Missing soundtrack: " + MusicCatalog.Path(loadingCue));
                    hasRequest = false;
                }
                loading = null;
            }
            if (fading)
            {
                fadeElapsed += dt;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(fadeElapsed / fadeDuration));
                weights[active] = t;
                weights[1 - active] = outgoingStart * (1 - t);
                if (fadeElapsed >= fadeDuration)
                {
                    fading = false;
                    voices[1 - active].Stop();
                    voices[1 - active].clip = null;
                    weights[1 - active] = 0;
                }
            }
            if (hasRequest && (!started || CurrentCue != wanted) && !fading)
            {
                if (clips[(int)wanted]) Begin(wanted, TrackFadeSeconds);
                else if (loading == null)
                {
                    loadingCue = wanted;
                    loading = Resources.LoadAsync<AudioClip>(MusicCatalog.Path(wanted));
                }
            }
            if (started && !fading && CurrentCue == wanted &&
                (!CurrentSource.isPlaying || CurrentSource.time >= LoopEnd(CurrentCue, CurrentSource.clip) - LoopFadeSeconds))
            {
                Begin(CurrentCue, LoopFadeSeconds);
                LoopCount++;
            }
            ApplyVolumes();
        }

        // Measured audible bounds exclude silence at the start/end; masters stay untouched.
        static readonly float[] starts = { 0, 0, .2f, .1f, .3f, 0 };
        static readonly float[] ends = { 121.1f, 196.1f, 178f, 48.1f, 158.1f, 119.7f };
        public static float LoopStart(MusicCue cue) => starts[(int)cue];
        public static float LoopEnd(MusicCue cue, AudioClip clip) => Mathf.Min(ends[(int)cue], clip.length);

        void Begin(MusicCue cue, float seconds)
        {
            int next = started ? 1 - active : active;
            outgoingStart = started ? weights[active] : 0;
            var voice = voices[next];
            voice.Stop();
            voice.clip = clips[(int)cue];
            cues[next] = cue;
            voice.volume = 0;
            weights[next] = 0;
            voice.time = LoopStart(cue);
            voice.Play();
            active = next;
            started = fading = true;
            fadeElapsed = 0;
            fadeDuration = seconds;
        }

        void ApplyVolumes()
        {
            for (int i = 0; i < 2; i++)
                if (voices[i]) voices[i].volume = master * MusicLevel * duck * weights[i] * MusicCatalog.Gains[(int)cues[i]];
        }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
