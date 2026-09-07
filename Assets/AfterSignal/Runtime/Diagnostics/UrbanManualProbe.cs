using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterSignal
{
    // Opt-in observer of real device input. Never supplies or synthesizes ControlFrame input.
    public sealed class UrbanManualProbe : MonoBehaviour
    {
        static bool initialized;
        static Dictionary<string, float> saved = new Dictionary<string, float>();
        static HashSet<string> existed = new HashSet<string>();
        [Serializable]
        class Snapshot
        {
            public int directions, wheelEvents, shots, reloads, weapon, entries;
            public float facing, speed, fuel;
            public Vector3 position;
            public bool driving, paused, map;
            public string input;
        }

        Snapshot state = new Snapshot();
        GameDirector game;
        float next;
        string output;
        IEnumerator Start()
        {
            game = GetComponent<GameDirector>();
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/Urban/Manual"));
            Directory.CreateDirectory(output);
            if (!initialized)
            {
                initialized = true;
                foreach (string suffix in new[]
                {
                    "Site",
                    "Car",
                    "CarX",
                    "CarZ",
                    "CarYaw",
                    "CarFuel",
                    "CarStage",
                    "CarHealth",
                    "CarType"
                }

                )
                {
                    string key = UrbanCatalog.Prefix + suffix;
                    if (PlayerPrefs.HasKey(key))
                        existed.Add(key);
                    saved[key] = suffix == "Site" || suffix == "Car" || suffix == "CarStage" || suffix == "CarType" ? PlayerPrefs.GetInt(key) : PlayerPrefs.GetFloat(key);
                }

                UrbanCatalog.Reset();
                GameDirector.SkipTitle = true;
                yield return SceneManager.LoadSceneAsync(CampaignRules.Scene(StageId.UrbanCity));
                yield break;
            }

            yield return null;
            game.Player.Respawn(new Vector3(73, .15f, -257), false);
            game.CameraRig.Snap();
            game.SetPaused(false);
        }

        void LateUpdate()
        {
            if (!game || !game.Ready || game.stage != StageId.UrbanCity)
                return;
            var c = game.Input.Frame;
            if (c.move.x < -.1f)
                state.directions |= 1;
            if (c.move.x > .1f)
                state.directions |= 2;
            if (c.move.y < -.1f)
                state.directions |= 4;
            if (c.move.y > .1f)
                state.directions |= 8;
            if (c.weaponCycle != 0)
                state.wheelEvents++;
            if (Time.unscaledTime < next)
                return;
            next = Time.unscaledTime + .15f;
            var sim = UrbanSimulation.Instance;
            state.shots = game.Player.ShotsFired;
            state.reloads = game.Player.Reloads;
            state.weapon = (int)game.Player.Weapon;
            state.facing = game.Player.Facing;
            state.position = game.Player.transform.position;
            state.paused = game.Paused;
            state.input = game.Input.Diagnostics;
            state.driving = sim && sim.Driving;
            state.entries = sim ? sim.Entries : 0;
            state.speed = sim && sim.Current ? sim.Current.speed : 0;
            state.fuel = sim && sim.Current ? sim.Current.fuel : 0;
            state.map = sim && sim.MapOpen;
            File.WriteAllText(Path.Combine(output, "manual-input.json"), JsonUtility.ToJson(state, true));
        }

        void OnApplicationQuit()
        {
            foreach (var kv in saved)
            {
                if (!existed.Contains(kv.Key))
                    PlayerPrefs.DeleteKey(kv.Key);
                else if (kv.Key.EndsWith("X") || kv.Key.EndsWith("Z") || kv.Key.EndsWith("Yaw") || kv.Key.EndsWith("Fuel") || kv.Key.EndsWith("Health"))
                    PlayerPrefs.SetFloat(kv.Key, kv.Value);
                else
                    PlayerPrefs.SetInt(kv.Key, (int)kv.Value);
            }

            PlayerPrefs.Save();
        }
    }
}
