using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal
{
    public sealed class ShopArtSmoke : MonoBehaviour
    {
        [Serializable]
        sealed class Report
        {
            public bool completed;
            public List<string> passed = new List<string>(), errors = new List<string>();
        }
        readonly Report report = new Report();
        bool oldSitePresent, finished;
        int oldSite;
        string output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Environment.GetCommandLineArgs().Contains("-shop-art-smoke")) return;
            GameDirector.SkipTitle = true;
            new GameObject("Shop item UI check").AddComponent<ShopArtSmoke>();
        }
        void Awake()
        {
            Application.runInBackground = true;
            LifeState.SuppressSave = true;
            oldSitePresent = PlayerPrefs.HasKey(UrbanCatalog.Prefix + "Site");
            oldSite = UrbanCatalog.Current;
            output = Path.GetFullPath(QualitySession.Arg("-quality-output", "Artifacts/ShopItems/Smoke"));
            Directory.CreateDirectory(output);
            Application.logMessageReceived += Log;
        }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message);
        }
        void OnDestroy() => Application.logMessageReceived -= Log;
        void Update()
        {
            if (!finished && Time.realtimeSinceStartup > 60) { Check(false, "UI check timed out"); Finish(); }
        }
        void Check(bool value, string label) => (value ? report.passed : report.errors).Add(label);

        IEnumerator Start()
        {
            while (!GameDirector.Instance || !GameDirector.Instance.Ready) yield return null;
            var game = GameDirector.Instance;
            game.Input.ExternalControl = true;
            game.Input.ExternalFrame = ControlFrame.Empty;
            game.SetPaused(true);
            // Exercise the real service selection/UI without travelling or changing the save.
            game.stage = StageId.UrbanInterior;
            Check(ShopItemArt.Keys.All(key => ShopItemArt.Get(key)), "All eight item images load from packaged Resources");
            var used = new HashSet<string>();
            foreach (int site in new[] { 6, 7, 9, 15 })
            {
                PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", site);
                CityLife.Instance.Services();
                game.SetPaused(false);
                yield return null;
                yield return null;
                var options = CityLife.Instance.Options.Where(o => !string.IsNullOrEmpty(o.image)).ToArray();
                foreach (var option in options) used.Add(option.image);
                var images = FindObjectsByType<Image>().Where(i => i.name == "Shop item image" && i.isActiveAndEnabled && i.sprite).ToArray();
                Check(options.Length == 2 && images.Length == 2 && images.All(i => i.preserveAspect && i.rectTransform.rect.width >= 190),
                    UrbanCatalog.Name(site) + ": two visible purchase illustrations");
                yield return Capture("shop-" + site);
            }
            Check(used.SetEquals(ShopItemArt.Keys), "Every purchasable item maps to its own illustration");

            PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", 7);
            CityLife.Instance.Services();
            LifeState.Earn(100);
            yield return null;
            yield return null;
            int credits = LifeState.Credits;
            var lunch = FindObjectsByType<Image>().First(i => i.name == "Shop item image" && i.isActiveAndEnabled && i.sprite == ShopItemArt.Get("LunchBox"));
            lunch.GetComponentInParent<Button>().onClick.Invoke();
            Check(LifeState.Credits == credits - 45 && CityLife.Instance.Body.Contains("이용 완료"), "Illustrated purchase button charges the existing item price");

            LifeState.Spend(LifeState.Credits);
            CityLife.Instance.Options[0].action();
            Check(LifeState.Credits == 0 && CityLife.Instance.Body.Contains("부족"), "Insufficient funds still prevents purchase");
            PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", 3);
            CityLife.Instance.Services();
            yield return null;
            yield return null;
            Check(!FindObjectsByType<Image>().Any(i => i.name == "Shop item image" && i.isActiveAndEnabled), "Non-item services restore the compact menu");
            Finish();
        }

        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f);
            var cam = Camera.main;
            var canvas = FindAnyObjectByType<Canvas>();
            var mode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            float distance = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(image);
            canvas.renderMode = mode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = distance;
        }
        void Restore()
        {
            if (oldSitePresent) PlayerPrefs.SetInt(UrbanCatalog.Prefix + "Site", oldSite);
            else PlayerPrefs.DeleteKey(UrbanCatalog.Prefix + "Site");
            PlayerPrefs.Save();
        }
        void OnApplicationQuit() => Restore();
        void Finish()
        {
            if (finished) return;
            finished = true;
            Restore();
            report.completed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(output, "shop-art.json"), JsonUtility.ToJson(report, true));
            Application.Quit(report.completed ? 0 : 1);
        }
    }
}
