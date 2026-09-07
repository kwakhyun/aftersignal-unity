using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/Expansion/Build town and new districts")]
        public static void BuildExpansion()
        {
            DistrictMaterials();
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(resourceRoot + "GameTuning.asset");
            BuildScene(StageId.Haven, tuning);
            foreach (var spec in CampaignCatalog.Districts)
                BuildScene(spec.id, tuning);
            var scenes = new EditorBuildSettingsScene[CampaignRules.Scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                scenes[i] = new EditorBuildSettingsScene("Assets/AfterSignal/Scenes/" + CampaignRules.Scenes[i] + ".unity", true);
            EditorBuildSettings.scenes = scenes;
            for (int i = 3; i < scenes.Length; i++)
            {
                var scene = EditorSceneManager.OpenScene(scenes[i].path);
                PolishDistrict((StageId)i);
                EditorSceneManager.SaveScene(scene);
            }

            OptimizeAllScenes();
            AssetDatabase.SaveAssets();
        }

        public static void ExpansionAndBuild()
        {
            BuildExpansion();
            BuildWindows();
        }

        public static void ExpansionAndRelease()
        {
            BuildExpansion();
            BuildRelease();
        }

        static void DistrictMaterials()
        {
            CreateMaterial("DistrictStone", "#67665b", .04f, .22f);
            CreateMaterial("DistrictWarm", "#ad7651", .08f, .28f);
            CreateMaterial("DistrictIvory", "#aeb8af", .05f, .24f);
            CreateMaterial("DistrictRed", "#874b43", .12f, .3f);
            CreateMaterial("DistrictBlue", "#3b6474", .16f, .3f);
            CreateMaterial("DistrictWater", "#234751", .25f, .48f);
            CreateMaterial("DistrictLight", "#a7bbad", 0, .2f, .28f);
            CreateMaterial("DistrictWindow", "#496576", .18f, .35f);
            foreach (string n in new[]
            {
                "DistrictStone",
                "DistrictIvory",
                "DistrictWarm"
            }

            )
            {
                var m = Mat(n);
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot + "Materials/CeramicAlbedo.png"));
                m.SetTextureScale("_BaseMap", new Vector2(1, 1));
                EditorUtility.SetDirty(m);
            }
        }

        static void Deck(float a, float b, float y, float z = 0, float depth = 7.4f)
        {
            Box("Walkable deck", (a + b) / 2, y - .3f, z, b - a, .6f, depth, "Tile", true);
            Box("Structural fascia", (a + b) / 2, y - 1, z, b - a, .8f, depth, "DarkMetal");
            foreach (float side in new[]
            {
                -1f,
                1f
            }

            )
                Box("Path edge", (a + b) / 2, y + .025f, z + side * (depth / 2 - .18f), b - a, .04f, .1f, "Gold");
        }

        static MovingLift Lift(float x, float bottom, float top, float z = 0)
        {
            var go = new GameObject("LIFT / " + x);
            go.transform.SetParent(world);
            var lift = go.AddComponent<MovingLift>();
            lift.bottom = bottom;
            lift.top = top;
            var platform = Box("Lift platform", x, bottom - .18f, z, 3.4f, .36f, 3.4f, "Chrome", true);
            var pivot = new GameObject("Lift moving frame");
            pivot.transform.SetParent(go.transform);
            pivot.transform.position = new Vector3(x, bottom, z);
            platform.transform.SetParent(pivot.transform, true);
            lift.platform = pivot.transform;
            foreach (float dx in new[]
            {
                -1.8f,
                1.8f
            }

            )
                Box("Lift guide", x + dx, (bottom + top) / 2 + 1.3f, z + 1.8f, .16f, top - bottom + 3, .16f, "Chrome");
            Box("Lift back light", x, (bottom + top) / 2 + 1.5f, z + 2, .09f, top - bottom + 2, .08f, "DistrictLight");
            Sign("승강기 / E", new Vector3(x, top + 3, z + 2), 3.4f, .6f, "Amber");
            foreach (float y in new[]
            {
                bottom,
                top
            }

            )
            {
                var call = Interact(InteractionKind.Lift, "승강기 호출 / 이동", new Vector3(x, y + 1.3f, z));
                call.lift = lift;
                call.radius = 2.2f;
            }

            return lift;
        }

        static void Escalator(float a, float b, float low, float high, float z = 0)
        {
            var start = new Vector3(a, low, z);
            var end = new Vector3(b, high, z);
            float length = Vector3.Distance(start, end), angle = Mathf.Atan2(high - low, b - a) * Mathf.Rad2Deg;
            var ramp = Box("Escalator collision", (a + b) / 2, (low + high) / 2 - .16f, z, length, .3f, 3, "Metal", true);
            ramp.transform.rotation = Quaternion.Euler(0, 0, angle);
            var surface = ramp.AddComponent<EscalatorSurface>();
            surface.from = start;
            surface.to = end;
            for (int i = 0; i < 32; i++)
            {
                float t = i / 31f;
                Box("Escalator tread", Mathf.Lerp(a, b, t), Mathf.Lerp(low, high, t) + .02f, z, .48f, .08f, 2.7f, "Chrome");
            }

            foreach (float side in new[]
            {
                -1.65f,
                1.65f
            }

            )
            {
                var rail = Box("Escalator rail", (a + b) / 2, (low + high) / 2 + 1, z + side, length, .1f, .12f, "Rubber");
                rail.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        static void ExitFrame(InteractionKind kind, string title, Vector3 p, bool gate = false)
        {
            foreach (float dx in new[]
            {
                -1.9f,
                1.9f
            }

            )
                Box("Exit arch", p.x + dx, p.y + 2, p.z + .8f, .22f, 4, .3f, "Chrome");
            Box("Exit lintel", p.x, p.y + 4, p.z + .8f, 4, .24f, .4f, "Gold");
            Sign(title, p + new Vector3(0, 4.7f, .8f), 5.4f, .7f, "Amber");
            var root = new GameObject("ROUTE / " + title);
            root.transform.SetParent(world);
            root.transform.position = p + new Vector3(0, 2, p.z == 0 ? 1.2f : 0);
            var beacon = root.AddComponent<RouteBeacon>();
            beacon.districtExit = gate;
            var ring = new GameObject("Signal aperture");
            ring.transform.SetParent(root.transform, false);
            beacon.ring = ring.transform;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                var r = Box("Aperture segment", root.transform.position.x + Mathf.Cos(a) * 1.2f, root.transform.position.y + Mathf.Sin(a) * 1.2f, root.transform.position.z, .55f, .1f, .1f, "DistrictLight", false, ring.transform);
                r.transform.rotation = Quaternion.Euler(0, 0, i * 45 + 90);
            }

            Interact(kind, title, p + Vector3.up * 1.3f);
        }

        static void District(DistrictSpec s)
        {
            if (s.layout == DistrictLayout.Facade)
                FacadeRoute(s);
            else if (s.layout == DistrictLayout.Canal)
            {
                Deck(-8, 24, 0);
                Deck(29, 52, 0);
                Deck(57, s.length + 5, 0);
                Anchor(30, 8, 0);
                Anchor(58, 8, 0);
                Box("Canal water", s.length / 2, -3, 1, s.length + 30, .2f, 20, "DistrictWater");
            }
            else if (s.layout == DistrictLayout.Escalator)
            {
                Deck(-8, 30, 0);
                Deck(46, s.length + 5, 5);
                Escalator(30, 46, 0, 5);
            }
            else if (s.layout == DistrictLayout.Lift)
            {
                Deck(-8, 28, 0);
                Deck(28, s.length + 5, 6);
                Lift(26, 0, 6);
            }
            else if (s.layout == DistrictLayout.Exterior)
            {
                Deck(-8, 28, 0);
                Deck(28, 45, 4);
                Deck(52, s.length + 5, 8);
                Lift(26, 0, 4);
                Anchor(42, 11, 0);
                Anchor(54, 15, 0);
            }
            else
                Deck(-8, s.length + 8, 0);
            DistrictBackdrop(s);
            if (s.glass && s.layout != DistrictLayout.Facade)
            {
                float x = s.layout == DistrictLayout.Exterior ? 60 : s.layout == DistrictLayout.Lift ? 47 : 55;
                Box("Breakable entry glazing", x, s.EndHeight + 1.9f, 0, .12f, 3.8f, 7.6f, "Glass", true).AddComponent<BreakableGlass>();
                Sign("격벽 · 공격", new Vector3(x, s.EndHeight + 4.5f, 1.5f), 3.4f, .55f, "Amber");
            }

            int first = s.layout == DistrictLayout.Facade ? 92 : s.layout == DistrictLayout.Street || s.layout == DistrictLayout.Canal ? 14 : 52;
            for (int i = 0; i < s.enemies; i++)
            {
                float x = Mathf.Lerp(first, s.length - 12, i / (float)Mathf.Max(1, s.enemies - 1));
                if (s.layout == DistrictLayout.Canal && (x > 22 && x < 31 || x > 50 && x < 59))
                    x += 7;
                string kind = i % 4 == 1 ? "gunner" : i % 4 == 2 ? "stalker" : "blade";
                Enemy(kind, x, i % 2 == 0 ? -.8f : .85f, x - 12, kind == "gunner" ? 64 : kind == "stalker" ? 60 : 72);
                var e = world.GetChild(world.childCount - 1).GetComponent<EnemyBrain>();
                if (e)
                    e.transform.position = new Vector3(x, s.layout == DistrictLayout.Canal ? 0 : s.EndHeight, i % 2 == 0 ? -.8f : .85f) + Vector3.up * .1f;
            }

            Terminal(InteractionKind.DistrictRelay, "区 / 中継 · 중계기 복구", s.length - 9, s.EndHeight, -1.2f);
            Interact(InteractionKind.Memory, "지역 기록 보존", new Vector3(s.length - 18, s.EndHeight + 1.25f, 2), s.record);
            Box("District record", s.length - 18, s.EndHeight + .7f, 2, .3f, .65f, .2f, "Amber");
            ExitFrame(InteractionKind.DistrictExit, s.Finale ? "귀환 / AFTERLIGHT" : "다음 구역 / E", new Vector3(s.length - 3, s.EndHeight, 0), true);
            var shutter = Box("Relay safety shutter", s.length - 5, s.EndHeight + 1.9f, 0, .18f, 3.8f, 7.4f, "DarkMetal", true);
            shutter.AddComponent<DistrictGate>();
            if (s.id == StageId.Harbor)
            {
                Npc("해진 / 선착장", "haejin", new Vector3(10, .05f, 1), "먼 강에서도 이 도시의 새 방송이 들린대. 창고의 보급품도 잊지 마.");
                Interact(InteractionKind.Supply, "보급 상자 확보", new Vector3(24, 1.3f, -1));
                Box("Supply crate", 24, .5f, -1, 1.4f, 1, 1, "Gold");
            }
        }

        static void DistrictBackdrop(DistrictSpec s)
        {
            if (s.id == StageId.Haven)
                return;
            bool water = s.theme == "water" || s.theme == "harbor", factory = s.theme == "foundry" || s.theme == "freight", indoor = s.theme == "lab" || s.theme == "archive" || s.theme == "atrium" || s.theme == "origin";
            string wall = indoor ? "DistrictIvory" : factory ? "DistrictRed" : water ? "DistrictBlue" : "DistrictStone";
            var rng = new System.Random(1300 + (int)s.id);
            for (float x = -14; x < s.length + 22; x += 7)
            {
                if (s.id == StageId.Haven && x > 88)
                    continue;
                float h = indoor ? 22 : 12 + (float)rng.NextDouble() * 16;
                Box("District wall volume", x, h / 2 - 2, 10, 6.8f, h, 5, wall);
                Box("Recessed facade", x, 7, 7.38f, 5.6f, 12, .14f, "DarkMetal");
                for (int row = 0; row < 5; row++)
                    for (int c = 0; c < 3; c++)
                    {
                        float px = x - 1.85f + c * 1.85f, py = 1.8f + row * 3.25f;
                        Box("Window inset", px, py, 7.27f, 1.25f, 2, .07f, rng.NextDouble() > .73 ? "DistrictLight" : "DistrictWindow");
                        Box("Window lintel", px, py + 1, 7.19f, 1.4f, .09f, .12f, "Chrome");
                    }

                Box("Facade pier", x - 3.2f, 9, 7.1f, .28f, 22, .4f, "Metal");
                Box("Service conduit", x + 2.85f, 7, 7, .09f, 16, .12f, "Gold");
                if (factory)
                {
                    Cylinder("Process tank", new Vector3(x, 2.5f, 5.2f), new Vector3(1.4f, 2.5f, 1.4f), "Chrome");
                    Box("Steam header", x, 8, 6, 7, .55f, .55f, "DistrictWarm");
                }

                if (water)
                {
                    Box("Water channel ledge", x, -1, 6, 7, .45f, 3, "Concrete");
                    Box("Canal railing", x, 1.4f, 4.5f, 6.7f, .1f, .1f, "Gold");
                }

                if (s.theme == "market" || s.theme == "harbor")
                {
                    Box("Awning", x, 4, 5, 5.8f, .15f, 2.5f, "DistrictWarm");
                    Box("Market counter", x, .8f, 4.6f, 4.8f, 1.6f, 1.2f, "Metal");
                    for (int k = 0; k < 4; k++)
                        Box("Supply bin", x - 1.5f + k, 1.9f, 4.6f, .7f, .5f, .7f, k % 2 == 0 ? "Gold" : "DistrictRed");
                }
            }

            for (float x = 3; x < s.length; x += 18)
            {
                float y = s.layout == DistrictLayout.Street || s.layout == DistrictLayout.Canal ? 0 : s.EndHeight;
                LightAt(new Vector3(x, y + 5, 1), factory ? new Color(1, .68f, .4f) : new Color(.75f, .86f, .83f), 9, 12);
                Box("Street light pole", x, y + 2.8f, 3.5f, .12f, 5.6f, .12f, "Chrome");
                Box("Street light", x + .6f, y + 5.5f, 3.1f, 1.4f, .14f, .45f, "DistrictLight");
            }

            Sign(s.title, new Vector3(9, 6, 6.4f), 7.5f, 1, "Amber");
            Sign(s.Finale ? "RETURN SIGNAL" : "路線 / DISTRICT", new Vector3(s.length - 16, s.EndHeight + 6, 6.5f), 7, .9f);
            if (!indoor)
            {
                for (int i = 0; i < 10; i++)
                {
                    float x = -25 + i * 19, h = 18 + i % 4 * 5;
                    Box("Distant mass", x, h / 2, 35, 13, h, 10, "Building");
                    Box("Distant crown", x, h + .4f, 35, 10, .25f, 9, "DistrictBlue");
                }

                Box("Horizon", s.length / 2, -6, 60, s.length + 90, 1, 80, "DarkMetal");
            }
        }

        static void ExpandedHaven()
        {
            Deck(-10, 126, 0, 0, 15);
            Deck(23, 54, 5, 4, 3.2f);
            Escalator(11, 23, 0, 5, 4);
            Deck(66, 91, 6, 0, 8);
            Lift(64, 0, 6);
            Deck(102, 121, 11, 3, 6);
            Escalator(88, 102, 6, 11, 3);
            var town = new DistrictSpec(StageId.Haven, "애프터라이트 / 돌아온 기억", "market", 0, DistrictLayout.Street, 0, 118, false, "", "");
            DistrictBackdrop(town);
            Npc("NOA / 노아", "noa", new Vector3(10, .05f, .5f), "", InteractionKind.QuestGiver);
            Npc("MIN / 민", "min", new Vector3(19, .05f, 1), "", InteractionKind.MinJob);
            Npc("YUN / 윤", "yun", new Vector3(32, 5.05f, 4), "", InteractionKind.YunJob);
            Npc("DAMI / 다미", "dami", new Vector3(76, 6.05f, 1), "", InteractionKind.Rest);
            Npc("HAEJIN / 해진", "haejin", new Vector3(108, 11.05f, 3), "옥상 안테나에서 강을 따라 흐르는 신호가 들려. 선착장에 가면 그 목소리를 만날 수 있을 거야.");
            Terminal(InteractionKind.MissionBoard, "메인 의뢰 / 다음 노선", 25, 0, -2);
            Terminal(InteractionKind.Signal, "옥상 안테나 수신", 115, 11, 3);
            Terminal(InteractionKind.Rest, "광장 쉼터", 44, 0, -2);
            ExitFrame(InteractionKind.HarborTravel, "강변 선착장 / E", new Vector3(54, 0, -3));
            Terminal(InteractionKind.Return, "기존 열차 캠페인 새로 시작", 4, 0, -4);
            Bench(6, 0, 2.8f);
            Bench(36, 0, -3);
            Bench(80, 6, -2);
            Bench(109, 11, 4);
            Anchor(21, 8, 2);
            Anchor(44, 10, 3);
            Anchor(69, 12, 0);
            Anchor(100, 16, 3);
            for (int i = 0; i < 12; i++)
            {
                float x = 5 + i * 9;
                Cylinder("Plaza planter", new Vector3(x, .55f, -5), new Vector3(1.25f, .55f, 1.25f), "Metal");
                for (int j = 0; j < 5; j++)
                {
                    var leaf = Box("Broad leaf", x + (j - 2) * .2f, 1.5f, -5, .2f, 1.7f, .7f, "Leaf");
                    leaf.transform.rotation = Quaternion.Euler(j * 12, 30 * j, j * 9);
                }
            }

            Sign("작업실 / 승강기", new Vector3(64, 10, 4), 7, .8f, "Amber");
            Sign("옥상 정원", new Vector3(108, 16, 6), 6, .8f);
            foreach (float x in new[]
            {
                103f,
                112f,
                118f
            }

            )
            {
                Box("Roof garden bed", x, 11.4f, 5.3f, 2.1f, .8f, 1.1f, "DistrictWarm");
                for (int j = 0; j < 5; j++)
                {
                    var leaf = Box("Roof foliage", x + (j - 2) * .3f, 12.2f, 5.3f, .35f, 1.2f, .6f, "Leaf");
                    leaf.transform.rotation = Quaternion.Euler(0, j * 23, (j - 2) * 14);
                }
            }

            foreach (float x in new[]
            {
                103f,
                119f
            }

            )
                Box("Garden pergola post", x, 13.6f, 5.8f, .18f, 5.2f, .18f, "DistrictWarm");
            Box("Garden pergola beam", 111, 16.2f, 5.8f, 16.4f, .2f, .3f, "DistrictWarm");
            for (int i = 0; i < 9; i++)
            {
                float x = 103 + i * 2;
                Box("Garden suspended lamp", x, 15.8f - Mathf.Sin(i * Mathf.PI / 8) * .5f, 5.6f, .2f, .27f, .2f, "DistrictLight");
            }

            LightAt(new Vector3(110, 14.5f, 2), new Color(1, .77f, .5f), 10, 12);
        }

        static void PolishDistrict(StageId stage)
        {
            foreach (var t in Object.FindObjectsByType<TextMesh>())
                if (t.name != "City lettering")
                    t.characterSize = Mathf.Min(t.characterSize, .13f);
            foreach (var light in Object.FindObjectsByType<Light>())
                if (light.type == LightType.Directional)
                {
                    light.color = CivicWorld.Interior(stage) ? new Color(.9f, .92f, .87f) : new Color(.53f, .77f, 1);
                    light.intensity = CivicWorld.Interior(stage) ? .85f : .65f;
                    light.shadowStrength = .4f;
                }

            RenderSettings.ambientSkyColor = new Color(.34f, .37f, .38f);
            RenderSettings.ambientEquatorColor = new Color(.24f, .27f, .29f);
            RenderSettings.ambientGroundColor = new Color(.14f, .17f, .19f);
            RenderSettings.fogDensity = .006f;
            Camera.main.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
            var profile = Object.FindAnyObjectByType<Volume>().sharedProfile;
            if (profile.TryGet<Bloom>(out var bloom))
                bloom.intensity.Override(.17f);
            if (profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.postExposure.Override(.24f);
                grade.saturation.Override(-8);
            }

            EditorUtility.SetDirty(profile);
            foreach (var sprite in Object.FindObjectsByType<SpriteRenderer>())
                if (!sprite.GetComponentInParent<PixelActor>() && !sprite.GetComponent<ContactShadow>())
                    sprite.gameObject.AddComponent<ContactShadow>();
        }
    }
}
