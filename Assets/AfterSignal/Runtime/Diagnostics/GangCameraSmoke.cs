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
    public sealed class GangCameraSmoke : MonoBehaviour
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
            if (Environment.GetCommandLineArgs().Contains("-gang-camera-smoke"))
                new GameObject("Gang sprite / camera verification").AddComponent<GangCameraSmoke>();
        }

        void Awake()
        {
            Application.runInBackground = true;
            LifeState.SuppressSave = true;
            GameDirector.SkipTitle = true;
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/GangCamera"));
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
                report.errors.Add("Gang sprite / camera verification timed out");
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
            if (game.Dialogue) game.CloseDialogue();
            game.SetPaused(false);
            game.enabled = false;
            var rig = game.CameraRig;
            game.Player.Respawn(new Vector3(25, 100, 0));
            rig.Snap();
            var look = ControlFrame.Empty;
            look.look = true;
            look.lookDelta = new Vector2(500, 150);
            rig.ReadLook(look);
            yield return new WaitForSecondsRealtime(.7f);
            Check(rig.FreeOrbit && rig.OrbitPitch < 0, "Third-person horizontal and upward mouse orbit");
            float yaw = rig.OrbitYaw;
            look.lookDelta = new Vector2(-300, -300);
            rig.ReadLook(look);
            yield return new WaitForSecondsRealtime(.7f);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw, rig.OrbitYaw) + 54) < .1f && rig.OrbitPitch > 0, "Reverse horizontal and downward orbit");
            float pitch = rig.OrbitPitch;
            rig.ReadLook(ControlFrame.Empty);
            yield return null;
            Check(Mathf.Abs(pitch - rig.OrbitPitch) < .01f, "Orbit persists after mouse release");
            look.lookDelta = new Vector2(2000, -5000);
            rig.ReadLook(look);
            Check(Mathf.Approximately(rig.OrbitPitch, 78) && rig.OrbitYaw >= 0 && rig.OrbitYaw < 360, "Vertical limit and full yaw wrap");
            look.lookDelta = new Vector2(0, (rig.OrbitPitch - 20) / .18f);
            rig.ReadLook(look);
            yield return new WaitForSecondsRealtime(.7f);
            var right = rig.ViewRight;
            var forward = rig.MoveDirection(Vector2.up);
            Check(Vector3.Dot(rig.MoveDirection(Vector2.right), right) > .99f && Mathf.Abs(Vector3.Dot(forward, right)) < .01f, "Camera-relative horizontal movement basis");
            var dash = ControlFrame.Empty;
            dash.move = Vector2.up; dash.dash = true;
            dash.pointer = Camera.main.WorldToScreenPoint(game.Player.Shoulder + right * 10);
            game.Player.Tick(dash, .02f);
            Check(Vector3.Dot(game.Player.DashDirection, forward) > .99f && Vector3.Dot(game.Player.Velocity, forward) > 1, "Forward dash follows the rotated view");
            var focus = game.Player.Shoulder;
            var direction = (rig.transform.position - focus).normalized;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Camera collision probe";
            wall.transform.position = focus + direction * 4;
            wall.transform.rotation = Quaternion.LookRotation(direction);
            wall.transform.localScale = new Vector3(20, 20, .5f);
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.5f);
            Check(Vector3.Distance(rig.transform.position, focus) < 3.8f && !Physics.CheckSphere(rig.transform.position, .15f, 1, QueryTriggerInteraction.Ignore), "Camera retracts in front of a wall without entering it");
            Destroy(wall);
            game.SetPaused(true);
            yaw = rig.OrbitYaw;
            look.lookDelta = new Vector2(300, 100);
            rig.ReadLook(look);
            Check(!rig.CanLook && Mathf.Approximately(yaw, rig.OrbitYaw), "Pause blocks camera input");
            game.SetPaused(false);
            var reset = ControlFrame.Empty; reset.cameraReset = true;
            rig.ReadLook(reset);
            Check(!rig.FreeOrbit, "HOME restores the default camera");
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
            for (int i = 0; i < 3; i++)
            {
                var member = GangMember.Create(new Vector3(-1.4f + i * 2.8f, 0, 0), i, 0);
                member.enabled = false;
                var actor = member.GetComponent<PixelActor>();
                actors.Add(actor);
                var sprites = Resources.LoadAll<Sprite>("Art/" + GangSpriteCatalog.Art(i));
                Check(sprites.Length == GangSpriteCatalog.FrameCount && actor.art == GangSpriteCatalog.Art(i), GangMember.CrewName(i) + ": eight dedicated gang sprites");
                Check(sprites.Length > 0 && Mathf.Abs(sprites[0].bounds.size.y - 2.1f) < .1f, GangMember.CrewName(i) + ": consistent human scale");
            }
            string[] labels = { "SEO / SCALE", "RED IRON", "VIOLET CREW", "RUST FANG" };
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
            for (int frame = 0; frame < GangSpriteCatalog.FrameCount; frame++)
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
            File.WriteAllText(Path.Combine(output, "gang-camera.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
