using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal.Editor
{
    public static class ArtCraftReview
    {
        [MenuItem("AFTERSIGNAL/ArtCraft/Render facility map review")]
        public static void Render()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            RenderTexture target = null;
            Texture2D pixels = null;
            Camera camera = null;
            var previous = RenderTexture.active;
            var respawnPoints = (Vector3[])RespawnNetwork.Points.Clone();
            try
            {
                var cameraObject = new GameObject("ArtCraft review camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.GetComponent<Camera>();
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 4.8f;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.025f, .05f, .075f);
                camera.cullingMask = 1 << 5;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 50;
                target = new RenderTexture(1440, 960, 24);
                camera.targetTexture = target;
                var root = new GameObject("Map review", typeof(RectTransform), typeof(Canvas));
                root.layer = 5;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(1440, 960);
                root.transform.localScale = Vector3.one * .01f;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                var font = Resources.Load<Font>("Fonts/NotoSansKR");
                Text text(string title, float x, float y, float width, float height, int size, Color color)
                {
                    var go = new GameObject(title, typeof(RectTransform), typeof(Text));
                    go.layer = 5; go.transform.SetParent(root.transform, false);
                    var value = go.GetComponent<Text>(); value.font = font; value.text = title;
                    value.fontSize = size; value.color = color; value.raycastTarget = false;
                    Place(value.rectTransform, x, y, width, height);
                    return value;
                }
                text("AFTER / SIGNAL     CITY ATLAS", 40, 12, 1300, 64, 30, new Color(.85f, 1, .96f));
                text("애프터라이트 · 시설 아이콘 / 실제 지도 컴포넌트 렌더", 42, 77, 1300, 30, 18, new Color(.45f, .75f, .8f));
                var mapObject = new GameObject("Runtime atlas", typeof(RectTransform), typeof(CanvasRenderer), typeof(AtlasViewport));
                mapObject.layer = 5; mapObject.transform.SetParent(root.transform, false);
                var map = mapObject.GetComponent<AtlasViewport>(); map.follow = false;
                Place(map.rectTransform, 40, 128, 1020, 780);
                map.Focus(FourCityCatalog.Centers[0], 2100);
                RespawnNetwork.Resolve();
                var labels = mapObject.AddComponent<AtlasLabels>(); labels.Initialize(map);
                labels.RefreshLabels();
                var examples = new[] { VenueKind.Hospital, VenueKind.Police, VenueKind.FireStation, VenueKind.Bank,
                    VenueKind.Restaurant, VenueKind.Cafe, VenueKind.Military, VenueKind.Hotel,
                    VenueKind.Football, VenueKind.Cinema };
                var titles = new[] { "병원 / 의료 서비스", "경찰서 / 치안", "소방서 / 구조", "은행 / 금융",
                    "식당 / 식사", "카페 / 휴식", "방위군 / 군사 시설", "호텔 / 숙박", "경기장 / 스포츠", "영화관 / 문화" };
                for (int i = 0; i < examples.Length; i++)
                {
                    var go = new GameObject(titles[i], typeof(RectTransform), typeof(Image));
                    go.layer = 5; go.transform.SetParent(root.transform, false);
                    var image = go.GetComponent<Image>(); image.sprite = FacilityGlyphs.For(examples[i]);
                    Place(image.rectTransform, 1094, 145 + i * 70, 42, 42);
                    text(titles[i], 1150, 153 + i * 70, 260, 32, 18, new Color(.84f, .94f, .96f));
                }
                Canvas.ForceUpdateCanvases();
                map.canvasRenderer.cull = false;
                map.SetAllDirty();
                map.Rebuild(CanvasUpdate.PreRender);
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(1440, 960, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, 1440, 960), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Artifacts/ArtCraft");
                File.WriteAllBytes("Artifacts/ArtCraft/Unity-Map-Review.png", pixels.EncodeToPNG());
                Debug.Log("ARTCRAFT_MAP_REVIEW: Artifacts/ArtCraft/Unity-Map-Review.png");
            }
            finally
            {
                RenderTexture.active = previous;
                System.Array.Copy(respawnPoints, RespawnNetwork.Points, respawnPoints.Length);
                if (pixels) Object.DestroyImmediate(pixels);
                if (camera) camera.targetTexture = null;
                if (target) { target.Release(); Object.DestroyImmediate(target); }
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
