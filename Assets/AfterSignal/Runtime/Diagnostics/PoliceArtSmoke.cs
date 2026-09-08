using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    // Opt-in import/appearance check. It does not change the save or wanted level.
    public sealed class PoliceArtSmoke : MonoBehaviour
    {
        [Serializable]
        sealed class Report
        {
            public bool completed;
            public List<string> passed = new List<string>(), errors = new List<string>();
        }

        readonly Report report = new Report();
        string output;
        bool finished;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Environment.GetCommandLineArgs().Contains("-police-art-smoke"))
                new GameObject("Police sprite verification").AddComponent<PoliceArtSmoke>();
        }

        void Awake()
        {
            Application.runInBackground = true;
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/PoliceArt"));
            Directory.CreateDirectory(output);
            Application.logMessageReceived += Log;
        }

        void OnDestroy() => Application.logMessageReceived -= Log;
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                report.errors.Add(message);
        }

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup > 60)
            {
                report.errors.Add("Police sprite verification timed out");
                Finish();
            }
        }

        void Check(bool value, string label)
        {
            (value ? report.passed : report.errors).Add(label);
        }

        IEnumerator Start()
        {
            while (!GameDirector.Instance || !GameDirector.Instance.Ready) yield return null;
            var game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(true);
            game.CameraRig.enabled = false;
            var cam = Camera.main;
            var occlusion = cam.GetComponent<CameraOcclusion>();
            if (occlusion) occlusion.enabled = false;
            cam.transform.SetPositionAndRotation(new Vector3(0, .85f, -12), Quaternion.identity);
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.11f, .16f, .20f);
            cam.cullingMask = 1 << 31;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            foreach (var canvas in FindObjectsByType<Canvas>()) canvas.enabled = false;
            var actors = new List<PixelActor>();
            var hero = new GameObject("Seo scale reference").AddComponent<PixelActor>();
            hero.transform.position = new Vector3(-4.2f, 0, 0);
            hero.Initialize();
            actors.Add(hero);
            var weapons = new[] { PoliceWeapon.Pistol, PoliceWeapon.Shotgun, PoliceWeapon.Rifle };
            for (int i = 0; i < weapons.Length; i++)
            {
                var officer = PoliceOfficer.Create(WantedSystem.Instance, new Vector3(-1.4f + i * 2.8f, 0, 0), i == 0 ? 1 : i == 1 ? 2 : 4, i == 1 ? 1 : 0);
                officer.enabled = false;
                var actor = officer.GetComponent<PixelActor>();
                actors.Add(actor);
                var sprites = Resources.LoadAll<Sprite>("Art/" + PoliceSpriteCatalog.Art(weapons[i]));
                Check(officer.Weapon == weapons[i] && sprites.Length == PoliceSpriteCatalog.FrameCount,
                    weapons[i] + ": correct dispatch and eight dedicated sprites");
                Check(!officer.GetComponentInChildren<EnemyWeaponRig>() && !actor.art.StartsWith("Enemies/"),
                    weapons[i] + ": dedicated uniform and authored weapon");
                Check(sprites.Length > 0 && Mathf.Abs(sprites[0].bounds.size.y - 2.1f) < .1f,
                    weapons[i] + ": consistent human scale");
            }

            string[] labels = { "SEO / SCALE", "POLICE / PISTOL", "POLICE / SHOTGUN", "SWAT / RIFLE" };
            for (int i = 0; i < actors.Count; i++)
            {
                foreach (var t in actors[i].GetComponentsInChildren<Transform>()) t.gameObject.layer = 31;
                var label = new GameObject(labels[i], typeof(TextMesh));
                label.layer = 31;
                label.transform.position = new Vector3(actors[i].transform.position.x, -.6f, -.1f);
                var text = label.GetComponent<TextMesh>();
                text.text = labels[i];
                text.anchor = TextAnchor.MiddleCenter;
                text.fontSize = 40;
                text.characterSize = .045f;
                text.color = new Color(.75f, .84f, .87f);
            }

            string[] poses = { "idle", "run-a", "run-b", "aim", "fire", "hurt", "kneel", "prone" };
            for (int frame = 0; frame < PoliceSpriteCatalog.FrameCount; frame++)
            {
                for (int i = 0; i < actors.Count; i++) actors[i].Pose(i == 0 ? 0 : frame, 1);
                yield return null;
                Capture(cam, "lineup-" + poses[frame]);
            }
            foreach (var actor in actors) actor.Pose(0, -1);
            yield return null;
            Capture(cam, "lineup-left");
            Finish();
        }

        void Capture(Camera cam, string name)
        {
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            var image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(image);
        }

        void Finish()
        {
            finished = true;
            report.completed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "police-art.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
