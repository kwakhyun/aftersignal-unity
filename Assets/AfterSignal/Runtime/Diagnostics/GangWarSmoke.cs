using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    public sealed class GangWarSmoke : MonoBehaviour
    {
        [Serializable]
        sealed class Report
        {
            public bool completed;
            public int gangShots, policeShots;
            public List<string> passed = new List<string>(), errors = new List<string>();
        }
        readonly Report report = new Report();
        string output;
        bool finished;
        float began;
        GameDirector game;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Environment.GetCommandLineArgs().Contains("-gang-war-smoke"))
                new GameObject("Gang war integration check").AddComponent<GangWarSmoke>();
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            LifeState.SuppressSave = true;
            LifeState.Load();
            LifeState.Heat = LifeState.HiddenSeconds = 0;
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/GangWar"));
            Directory.CreateDirectory(output);
            Application.logMessageReceived += Log;
            began = Time.realtimeSinceStartup;
        }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - began > 85) { Check(false, "smoke timeout"); Finish(); }
        }
        void Check(bool value, string label) => (value ? report.passed : report.errors).Add(label);

        IEnumerator Start()
        {
            GameDirector.SkipTitle = true;
            yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
            while (!GameDirector.Instance || !GameDirector.Instance.Ready) yield return null;
            game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            if (game.Dialogue) game.CloseDialogue();
            game.SetPaused(false);
            yield return new WaitForSeconds(4);
            var war = CityGangWar.Instance;
            Check(war && war.SiteCount == 12, "Twelve encounter sites across the actual city");
            Check(war && war.ActiveEncounters > 0 && war.ActiveEncounters <= CityGangWar.MaxEncounters, "Nearby encounters spawn within the three-site budget");
            var gangs = FindObjectsByType<GangMember>();
            var officers = FindObjectsByType<PoliceOfficer>();
            if (gangs.Length == 0)
            {
                Debug.Log("Gang spawn probe player=" + game.Player.transform.position);
                for (int i = 0; i < 5; i++)
                {
                    var p = new Vector3(56 + (i % 2 == 0 ? -.7f : .7f), .02f, -224 + (i < 3 ? -7 - i * 3 : 12 + (i - 3) * 4));
                    bool safe = CityGangWar.FindGround(p, out var ground);
                    Physics.Raycast(p + Vector3.up * 3, Vector3.down, out var floor, 5, 1, QueryTriggerInteraction.Ignore);
                    Debug.Log("Gang spawn probe " + p + " safe=" + safe + " ground=" + ground + " floor=" + floor.collider + " floorY=" + floor.point.y);
                }
            }
            Check(gangs.Length >= 3 && officers.Any(p => p.Ambient), "Ambient gangs and dedicated police spawn without wanted status");
            Check(WantedSystem.Instance.Officers.Count == 0, "Ambient police do not consume wanted-response slots");
            bool gangDamaged = false, policeDamaged = false, defeated = false;
            float playerHealth = game.Player.Health;
            float until = Time.time + 27;
            bool captured = false;
            while (Time.time < until)
            {
                foreach (var member in gangs)
                    if (member)
                    {
                        gangDamaged |= member.Body.health < 58;
                        defeated |= !member.Body.Alive;
                    }
                foreach (var officer in officers)
                    if (officer)
                    {
                        policeDamaged |= officer.Body.health < 101;
                        defeated |= !officer.Body.Alive;
                    }
                report.gangShots = Mathf.Max(report.gangShots, gangs.Where(g => g).Sum(g => g.ShotsFired));
                report.policeShots = Mathf.Max(report.policeShots, officers.Where(p => p).Sum(p => p.ShotsFired));
                if (!captured && report.gangShots > 0 && report.policeShots > 0 && gangs.Any(g => g))
                {
                    captured = true;
                    Capture(gangs.First(g => g).transform.position);
                }
                yield return null;
            }
            Check(report.gangShots > 0 && report.policeShots > 0, "Both factions aim and fire autonomously");
            Check(gangDamaged && policeDamaged, "Physical shots damage both opposing factions");
            Check(defeated, "Faction casualties reach the defeat state");
            Check(LifeState.Heat == 0 && game.Player.Health == playerHealth, "Bystander player receives neither wanted heat nor targeted damage");
            game.SetPaused(true);
            if (war) war.enabled = false;

            // Isolated geometry checks reuse the exact live projectile path.
            var source = Dummy("Gang test", new Vector3(-400, 2, 0), true);
            var target = Dummy("Police test", new Vector3(-390, 2, 0), false);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(-395, 3, 0);
            wall.transform.localScale = new Vector3(1, 4, 4);
            Physics.SyncTransforms();
            FactionCombat.Fire(source, source.Center + Vector3.right, target.Center, 20, 10, SignalEffects.Red, false);
            Check(target.health == 70, "Walls block faction bullets");
            Destroy(wall);
            yield return null;
            target.police = false;
            target.gang = true;
            Physics.SyncTransforms();
            FactionCombat.Fire(source, source.Center + Vector3.right, target.Center, 20, 10, SignalEffects.Red, false);
            Check(target.health == 70, "Friendly faction is not damaged");
            target.police = true;
            target.gang = false;
            FactionCombat.Fire(source, source.Center + Vector3.right, target.Center, 20, 10, SignalEffects.Red, false);
            Check(target.health == 60 && LifeState.Heat == 0, "Attributed NPC hit damages police without blaming player");
            var playerVictim = Dummy("Police player-damage test", new Vector3(-380, 2, 0), false);
            playerVictim.Damage(10, Vector3.right);
            Check(WantedSystem.Level > 0, "Player attacking police still triggers wanted status");
            LifeState.Heat = LifeState.HiddenSeconds = 0;
            Destroy(source.gameObject);
            Destroy(target.gameObject);
            Destroy(playerVictim.gameObject);
            Finish();
        }

        static WorldActor Dummy(string name, Vector3 at, bool gang)
        {
            var go = new GameObject(name, typeof(CapsuleCollider), typeof(WorldActor));
            go.layer = 9;
            go.transform.position = at;
            var capsule = go.GetComponent<CapsuleCollider>();
            capsule.center = Vector3.up * 1.05f;
            capsule.height = 2.1f;
            capsule.radius = .34f;
            var body = go.GetComponent<WorldActor>();
            body.gang = gang;
            body.police = !gang;
            return body;
        }

        void Capture(Vector3 at)
        {
            var cam = Camera.main;
            var originalPosition = cam.transform.position;
            var originalRotation = cam.transform.rotation;
            var center = at + Vector3.forward * 8 + Vector3.up;
            cam.transform.position = center + new Vector3(-13, 15, -24);
            cam.transform.LookAt(center);
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            var old = RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, "city-gang-fight.png"), image.EncodeToPNG());
            RenderTexture.active = old;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(image);
            cam.transform.SetPositionAndRotation(originalPosition, originalRotation);
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            report.completed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "gang-war.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
