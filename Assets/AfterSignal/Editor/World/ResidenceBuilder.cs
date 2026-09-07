using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/Residence/Build playable city")]
        public static void BuildResidence()
        {
            AssetDatabase.Refresh();
            DistrictMaterials();
            InteriorMaterials();
            CityMaterials();
            UrbanMaterials();
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(resourceRoot + "GameTuning.asset");
            foreach (StageId s in new[]
            {
                StageId.Haven,
                StageId.Residence,
                StageId.School,
                StageId.Clinic,
                StageId.Headquarters,
                StageId.Breach
            }

            )
                BuildScene(s, tuning);
            var scenes = new EditorBuildSettingsScene[CampaignRules.Scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                scenes[i] = new EditorBuildSettingsScene("Assets/AfterSignal/Scenes/" + CampaignRules.Scenes[i] + ".unity", true);
            EditorBuildSettings.scenes = scenes;
            for (int i = 0; i < 23; i++)
            {
                var scene = EditorSceneManager.OpenScene(scenes[i].path);
                world = GameObject.Find("WORLD / editable architecture").transform;
                var game = Object.FindAnyObjectByType<GameDirector>();
                InstallBackdrop((StageId)i, game.stageLength);
                NeonArchitecture((StageId)i, game.stageLength);
                if (i == 3 || i >= 18)
                    PolishDistrict((StageId)i);
                if (i == 3)
                    InstallUrbanHaven();
                ReplaceSignImages();
                EditorSceneManager.SaveScene(scene);
            }

            OptimizeAllScenes();
            AssetDatabase.SaveAssets();
        }

        public static void ResidenceAndBuild()
        {
            BuildResidence();
            BuildWindows();
        }

        public static void ResidenceAndRelease()
        {
            BuildResidence();
            BuildRelease();
        }

        static void ArtPlate(StageId stage, Vector3 p, float width, float height, string suffix = "")
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot + "Art/Environment/" + stage + ".png");
            if (!tex)
                return;
            string path = resourceRoot + "Materials/Backdrop_" + stage + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ART / " + stage + suffix;
            go.transform.SetParent(world, false);
            go.transform.position = p;
            go.transform.localScale = new Vector3(width, height, 1);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        static void InstallBackdrop(StageId stage, float length)
        {
            if (stage == StageId.UrbanCity || stage == StageId.UrbanInterior)
                return;
            for (int i = world.childCount - 1; i >= 0; i--)
                if (world.GetChild(i).name.StartsWith("ART / "))
                    Object.DestroyImmediate(world.GetChild(i).gameObject);
            if (stage == StageId.Residence)
            {
                ArtPlate(stage, new Vector3(13, 26.9f, 5.85f), 28, 9.8f);
                return;
            }

            if (stage == StageId.Breach)
            {
                ArtPlate(stage, new Vector3(53, 15.5f, 5.07f), 59, 39);
                ArtPlate(StageId.Archive, new Vector3(115, 30, 5.05f), 58, 13);
                return;
            }

            if (CivicWorld.Interior(stage))
            {
                for (int i = 0; i < 2; i++)
                    ArtPlate(stage, new Vector3(12 + i * 26, 5.8f, 9.7f), 26, 11.6f, " / rear wall");
                return;
            }

            if (stage == StageId.Haven)
            {
                for (int i = 0; i < 3; i++)
                    ArtPlate(stage, new Vector3(-60 + i * 155, 34, 65), 158, 79, " / skyline");
                return;
            }

            bool inside = stage == StageId.Station || stage == StageId.Arcade || stage == StageId.Lab || stage == StageId.Archive || stage == StageId.Origin || stage == StageId.Foundry;
            float y = CampaignCatalog.Get(stage)?.EndHeight ?? 0;
            for (int i = 0; i < Mathf.CeilToInt(length / 36); i++)
                ArtPlate(stage, new Vector3(16 + i * 36, y + (inside ? 8 : 18), inside ? 7.05f : 50), inside ? 36 : 74, inside ? 22 : 49, " / district");
        // Textured wall bays sit behind walkable space; foreground architecture keeps real depth.
        }

        static void Furnish(string name, float x, float y, float z, InteractionKind kind = InteractionKind.Furniture, string text = "", bool heal = false)
        {
            var p = Interact(kind, name, new Vector3(x, y + 1.2f, z - 1.2f), text);
            p.restoresHealth = heal;
        }

        static void Table(float x, float y, float z, float w = 3, float d = 1.4f)
        {
            Box("Desk top", x, y + .85f, z, w, .15f, d, "DistrictWarm", true);
            foreach (float dx in new[]
            {
                -w * .4f,
                w * .4f
            }

            )
                foreach (float dz in new[]
                {
                    -d * .35f,
                    d * .35f
                }

                )
                    Box("Desk leg", x + dx, y + .4f, z + dz, .1f, .8f, .1f, "Metal");
        }

        static void Monitor(float x, float y, float z)
        {
            Box("Monitor foot", x, y + .08f, z, .7f, .1f, .45f, "Metal");
            Box("Monitor neck", x, y + .4f, z, .11f, .6f, .1f, "Chrome");
            Box("Monitor case", x, y + .8f, z, 1.4f, .9f, .16f, "DarkMetal");
            Box("Monitor active display", x, y + .8f, z - .09f, 1.22f, .72f, .025f, "DistrictBlue");
            for (int i = 0; i < 4; i++)
                Box("Terminal text line", x - .12f, y + .96f - i * .12f, z - .11f, .83f - i * .13f, .035f, .01f, "DistrictLight");
        }

        static void Bed(float x, float y, float z, bool hospital = false)
        {
            Box("Bed base", x, y + .28f, z, 3.3f, .55f, 2.2f, "DarkMetal", true);
            Box("Mattress", x, y + .64f, z, 3.25f, .25f, 2.16f, "DistrictIvory");
            Box("Folded blanket", x + .5f, y + .81f, z, 2.2f, .15f, 2.18f, hospital ? "DistrictBlue" : "Seat");
            Box("Pillow", x - 1.03f, y + .87f, z, .7f, .23f, 1.6f, "LightTile");
            Box("Bed headboard", x - 1.75f, y + .9f, z, .16f, 1.8f, 2.3f, "DistrictWarm");
            if (hospital)
            {
                Cylinder("Drip stand", new Vector3(x + 1.9f, y + 1.2f, z + .8f), new Vector3(.07f, 1.2f, .07f), "Chrome");
                Box("IV bag", x + 2, y + 2.1f, z + .8f, .3f, .6f, .12f, "DistrictLight");
            }
        }

        static HingedDoor Door(string name, Vector3 hinge, float width, bool alongX)
        {
            var root = new GameObject("DOOR / " + name);
            root.transform.SetParent(world);
            root.transform.position = hinge;
            var door = root.AddComponent<HingedDoor>();
            door.angle = alongX ? -100 : 100;
            var panel = Box("Hinged panel", hinge.x + (alongX ? width / 2 : 0), hinge.y + 1.5f, hinge.z + (alongX ? 0 : width / 2), alongX ? width : .16f, 3, alongX ? .16f : width, "DistrictWarm", true, root.transform);
            var pivot = new GameObject("Door pivot");
            pivot.transform.SetParent(root.transform, false);
            panel.transform.SetParent(pivot.transform, true);
            door.panel = pivot.transform;
            Box("Door handle", hinge.x + (alongX ? width * .82f : -.12f), hinge.y + 1.25f, hinge.z + (alongX ? -.12f : width * .82f), .12f, .13f, .25f, "Gold", false, pivot.transform);
            var p = Interact(InteractionKind.HomeDoor, name, hinge + new Vector3(alongX ? width / 2 : 0, 1.25f, alongX ? 0 : width / 2));
            p.door = door;
            p.radius = 3.1f;
            return door;
        }

        static void Stair(float a, float b, float low, float high, float z)
        {
            float len = Mathf.Sqrt((b - a) * (b - a) + (high - low) * (high - low));
            float angle = Mathf.Atan2(high - low, b - a) * Mathf.Rad2Deg;
            var r = Box("Emergency stair ramp", (a + b) / 2, (low + high) / 2 - .16f, z, len, .3f, 3.4f, "Concrete", true);
            r.transform.rotation = Quaternion.Euler(0, 0, angle);
            for (int i = 0; i < 38; i++)
            {
                float t = i / 37f;
                Box("Stair tread", Mathf.Lerp(a, b, t), Mathf.Lerp(low, high, t), z, .62f, .06f, 3.4f, "DistrictIvory");
            }

            foreach (float side in new[]
            {
                -1.8f,
                1.8f
            }

            )
            {
                var rail = Box("Stair rail", (a + b) / 2, (low + high) / 2 + 1.05f, z + side, len, .09f, .09f, "Gold");
                rail.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        static void Residence()
        {
            Deck(-3, 28, 22, 0, 12);
            Deck(28, 40.2f, 22, 0, 5);
            Deck(43.8f, 56, 22, 0, 5);
            Deck(25, 90, 0, 2, 18);
            // The bridge passes beside the final stair flight, leaving standing headroom below it.
            Deck(54, 81.1f, 22, -3.2f, 2.4f);
            Deck(78.9f, 81.1f, 22, -1, 2);
            Box("Top corridor guard rail", 67, 23, -4.5f, 28, .09f, .1f, "Gold");
            Box("Room rear wall", 12, 26, 6.1f, 32, 8, .4f, "DistrictStone", true);
            Box("Room left wall", -3, 25, 0, .4f, 6, 12, "DistrictStone", true);
            Box("Door wall rear", 28, 24.1f, 4, .35f, 4.2f, 4, "DistrictStone", true);
            Box("Door wall front", 28, 24.1f, -4, .35f, 4.2f, 4, "DistrictStone", true);
            Box("Door header", 28, 25.5f, 0, .4f, 1, 4, "DistrictStone");
            Door("현관문 열기 / 닫기", new Vector3(28, 22, -2), 4, false);
            Bed(6, 22, 3.3f);
            Furnish("서하의 침대 · 휴식", 6, 22, 2.6f, text: "낡은 담요에는 햇빛 냄새가 남아 있다. 밤마다 떠오르는 낯선 기억은 누구의 것일까.", heal: true);
            Box("Wardrobe", -1, 23.7f, 3.7f, 2.5f, 3.4f, 1.5f, "DistrictWarm", true);
            foreach (float x in new[]
            {
                -1.65f,
                -.35f
            }

            )
            {
                Box("Wardrobe door", x, 23.7f, 2.91f, 1.19f, 3.15f, .09f, "Seat");
                Box("Wardrobe grip", x + (x < -1 ? .35f : -.35f), 23.6f, 2.82f, .07f, .6f, .12f, "Gold");
            }

            Furnish("옷장", 0, 22, 2.8f, text: "수선한 재킷과 빨간 스카프. 서하가 이 도시에서 지켜 온 작은 흔적들이다.");
            Table(14, 22, 3.7f, 4);
            Monitor(14, 22.94f, 3.8f);
            Box("Keyboard", 14, 22.96f, 3.25f, 1.2f, .05f, .36f, "DarkMetal");
            Bench(14, 22, 1.8f);
            Furnish("컴퓨터 · 노아의 메시지", 14, 22, 2.1f, text: "노아: 오늘 밤 중앙역에서 만나. 먼저 본부에 들러 남겨 둔 승차권을 확인해 줘. 기억을 돌려줄 방법을 찾았어.");
            Box("Refrigerator body", 23, 23.45f, 4.3f, 1.8f, 2.9f, 1.4f, "DistrictIvory", true);
            Box("Fridge freezer", 23, 24.4f, 3.57f, 1.65f, .88f, .12f, "LightTile");
            Box("Fridge door", 23, 23, 3.57f, 1.65f, 1.8f, .12f, "DistrictIvory");
            Box("Fridge handle", 22.45f, 23.7f, 3.45f, .07f, .7f, .13f, "Chrome");
            Furnish("냉장고 · 식사", 23, 22, 3.1f, text: "따뜻하게 데운 식사를 먹었다. 길을 나서기 전에 잠시 숨을 돌린다.", heal: true);
            Box("Kitchen counter", 19, 22.6f, 4.4f, 4, 1.2f, 1.5f, "Seat", true);
            Box("Kitchen worktop", 19, 23.25f, 4.4f, 4.1f, .12f, 1.6f, "DistrictIvory");
            Cylinder("Kettle", new Vector3(18, 23.58f, 4.4f), new Vector3(.45f, .3f, .45f), "Chrome");
            Box("Apartment rug", 10, 22.035f, -1, 8, .045f, 4, "DistrictRed");
            for (int i = 0; i < 6; i++)
                Box("Rug weave", 6.5f + i * 1.4f, 22.061f, -1, .06f, .012f, 3.7f, "DistrictWarm");
            Sign("SEO / 0607", new Vector3(26, 24.8f, 4.1f), 2.7f, .45f, "Amber");
            LightAt(new Vector3(9, 25, 2), new Color(1, .68f, .36f), 12, 11);
            LightAt(new Vector3(21, 25, -1), new Color(.56f, .76f, .78f), 5, 9);
            var lift = Lift(42, 0, 22);
            lift.platform.position = new Vector3(42, 22, 0);
            lift.speed = 5;
            for (int floor = 0; floor < 5; floor++)
            {
                float lo = floor * 4.4f;
                bool right = floor % 2 == 0;
                Stair(right ? 58 : 80, right ? 80 : 58, lo, lo + 4.4f, right ? 2 : 8);
                Deck((right ? 80 : 58) - 1.1f, (right ? 80 : 58) + 1.1f, lo + 4.4f, 5, 10);
                Sign((floor + 2) + "F / 비상계단", new Vector3(right ? 80 : 58, lo + 6.6f, 10.2f), 3, .6f, "Amber");
            }

            for (int i = 0; i < 6; i++)
            {
                float y = i * 4.4f;
                Box("Stairwell rear masonry", 69, y + 2, 11, 28, 4.3f, .5f, "DistrictStone", true);
                Box("Stairwell window", 69, y + 2.2f, 10.71f, 6, 2.1f, .04f, "DistrictWindow");
                LightAt(new Vector3(70, y + 3, 5), new Color(.75f, .8f, .72f), 4, 9);
            }

            CivicNpc("경비원 해솔", "concierge", new Vector3(32, .05f, 3), "0607호 서하 씨군요. 승강기는 정상이에요. 비상계단도 이용할 수 있어요. 본부는 큰길 동쪽입니다.");
            var exit = Interact(InteractionKind.ReturnTown, "현관 · 마을로 외출", new Vector3(34, 1.2f, -3));
            exit.radius = 2.4f;
            Sign("1F → 애프터라이트", new Vector3(34, 3.5f, -2), 7, .7f, "Amber");
            Bench(49, 0, 6);
            Sign("6F / 주거층", new Vector3(35, 26, 2.5f), 5, .7f);
            Sign("비상계단 →", new Vector3(53, 25, 2.5f), 5, .7f, "Amber");
            ResidenceDetail();
        }

        static void CivicNpc(string name, string role, Vector3 pos, string dialogue, InteractionKind kind = InteractionKind.Citizen, bool heal = false)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(ResidentWalker), typeof(ContactShadow));
            go.transform.SetParent(world);
            go.transform.position = pos;
            go.GetComponent<SpriteRenderer>().sharedMaterial = Mat("PixelActor");
            go.GetComponent<ResidentWalker>().role = role;
            go.GetComponent<ResidentWalker>().walking = false;
            ShadowProxy(go.transform, 2.1f);
            var p = Interact(kind, name, pos + Vector3.up * 1.2f, dialogue);
            p.restoresHealth = heal;
        }

        static void TownBuilding(string name, float x, float z, float w, string color, StageId interior)
        {
            Box(name + " / building mass", x, 13, z, w, 26, 14, color, true);
            Box("Storefront reveal", x, 2.1f, z - 7.12f, w - 1, 4.2f, .2f, "DarkMetal");
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 5; c++)
                {
                    float px = x - w * .38f + c * w * .19f, py = 6 + r * 3.6f;
                    Box("Window frame", px, py, z - 7.23f, w * .14f, 2.4f, .16f, "Metal");
                    Box("Occupied window", px, py, z - 7.33f, w * .12f, 2.17f, .035f, (r + c) % 4 == 0 ? "DistrictLight" : "DistrictWindow");
                }

            Box("Entry recess", x, 1.6f, z - 7.26f, 3.5f, 3.2f, .1f, "DistrictBlue");
            foreach (float dx in new[]
            {
                -1.8f,
                1.8f
            }

            )
                Box("Entry jamb", x + dx, 1.7f, z - 7.5f, .15f, 3.4f, .35f, "Gold");
            Box("Entrance canopy", x, 3.5f, z - 8, 7, .2f, 2.7f, "DistrictWarm");
            Sign(name, new Vector3(x, 4.7f, z - 7.55f), w * .65f, 1.2f, "Amber");
            var p = Interact(InteractionKind.FacilityTravel, name + " · 들어가기", new Vector3(x, 1.2f, z - 10));
            p.destination = interior;
            p.radius = 3;
            if (interior == StageId.Residence)
            {
                p.hasArrival = true;
                p.arrival = new Vector3(34, .15f, -1);
            }

            LightAt(new Vector3(x, 4, z - 9), new Color(1, .77f, .48f), 7, 11);
        }

        static void CivicTown()
        {
            Deck(126, 234, 0, 0, 15);
            Deck(-10, 234, 0, 25, 35);
            Deck(-10, 234, 0, -18, 21);
            foreach (float z in new[]
            {
                13f,
                -12f
            }

            )
            {
                Box("Avenue / asphalt", 111, .035f, z, 242, .04f, 7, "DarkMetal");
                for (int i = 0; i < 59; i++)
                    Box("Road dash", -6 + i * 4, .065f, z, 1.6f, .025f, .12f, "DistrictIvory");
            }

            foreach (float x in new[]
            {
                42f,
                94f,
                138f,
                204f
            }

            )
            {
                Box("Cross street", x, .07f, 10, 7, .04f, 64, "DistrictStone");
                for (int i = 0; i < 8; i++)
                    Box("Crosswalk", x - 3 + i * .85f, .11f, 5, .42f, .025f, 3.4f, "DistrictIvory");
            }

            TownBuilding("새벽 주거동", 16, 26, 26, "DistrictWarm", StageId.Residence);
            TownBuilding("애프터라이트 학교", 64, 30, 29, "DistrictStone", StageId.School);
            TownBuilding("시민 병원", 113, 30, 27, "DistrictIvory", StageId.Clinic);
            TownBuilding("신호 복원 본부", 178, 30, 32, "DistrictBlue", StageId.Headquarters);
            for (int i = 0; i < 10; i++)
            {
                float x = 5 + i * 23;
                Box("Pavement planter", x, .5f, 8, 2.7f, 1, 2.1f, "DistrictWarm", true);
                Cylinder("Tree trunk", new Vector3(x, 2.2f, 8), new Vector3(.26f, 1.5f, .26f), "DistrictWarm");
                for (int j = 0; j < 5; j++)
                {
                    var leaf = Box("Angular autumn canopy", x + (j % 3 - 1) * .65f, 4 + (j % 2) * .55f, 8 + (j - 2) * .35f, 1.8f, 1.5f, 1.8f, j % 2 == 0 ? "DistrictRed" : "Leaf");
                    leaf.transform.rotation = Quaternion.Euler(j * 7, j * 31, 15);
                }
            }

            for (int i = 0; i < 12; i++)
            {
                float x = 8 + i * 19;
                Box("Street lamp post", x, 3.2f, 17, .12f, 6.4f, .12f, "Metal");
                Box("Street lamp head", x + .7f, 6.3f, 17, 1.7f, .18f, .5f, "DistrictLight");
                Bench(x, 0, 22);
            }

            for (int i = 0; i < 8; i++)
            {
                float x = 126 + i * 12;
                Box("Market kiosk", x, 1.3f, -22, 5, 2.6f, 3, "DistrictBlue", true);
                Box("Kiosk awning", x, 3, -21, 5.6f, .14f, 4.5f, i % 2 == 0 ? "DistrictWarm" : "DistrictRed");
                Sign(i % 2 == 0 ? "도시 식탁" : "REPAIR / 수선", new Vector3(x, 2.7f, -23.6f), 4, .65f, "Amber");
            }

            CivicNpc("교사 유진", "teacher", new Vector3(61, .05f, 18), "아이들이 기억을 잃어도 다시 배울 수 있도록 학교 문을 열었어요. 안에 들어와 보세요.");
            CivicNpc("의료원 소은", "medic", new Vector3(110, .05f, 18), "기억 회복에는 휴식도 필요해요. 병원 안에서 진료받을 수 있어요.");
            CivicNpc("연락원 태오", "commander", new Vector3(171, .05f, 17), "본부의 지안 대장이 기다립니다. 중앙역 임무와 외벽 침투 기록을 확인할 수 있어요.");
            for (int i = 0; i < 20; i++)
            {
                var go = new GameObject("Resident / walking " + i, typeof(SpriteRenderer), typeof(ResidentWalker), typeof(ContactShadow));
                go.transform.SetParent(world);
                go.GetComponent<SpriteRenderer>().sharedMaterial = Mat("PixelActor");
                var r = go.GetComponent<ResidentWalker>();
                r.role = new[]
                {
                    "teacher",
                    "medic",
                    "concierge",
                    "commander"
                }[i % 4];
                r.walking = true;
                r.from = new Vector3(8 + i * 11, .06f, i % 2 == 0 ? 10 : -8);
                r.to = r.from + Vector3.right * (i % 2 == 0 ? 13 : -8);
                r.speed = .7f + (i % 3) * .12f;
                go.transform.position = r.from;
            }

            Sign("← 주거동   학교 · 병원 →   본부", new Vector3(88, 7, 20), 15, 1, "Amber");
            Box("River beyond promenade", 111, -2, -39, 255, .2f, 18, "DistrictWater");
            CityDetail();
        }

        static void Facility(StageId stage)
        {
            Deck(-5, 58, 0, 1, 22);
            Box("Facility rear wall", 26, 5, 10.1f, 64, 10, .4f, "DistrictIvory", true);
            Box("Facility side wall", -5, 3, 1, .4f, 6, 22, "DistrictStone", true);
            Box("Facility far wall", 58, 3, 1, .4f, 6, 22, "DistrictStone", true);
            Sign(CivicWorld.Title(stage), new Vector3(8, 4.7f, 8.9f), 10, 1, "Amber");
            var exit = Interact(InteractionKind.ReturnTown, "출입문 · 마을로", new Vector3(3, 1.2f, -4));
            exit.radius = 2.6f;
            Sign("E / 나가기", new Vector3(3, 3, -3.6f), 3, .6f);
            foreach (float x in new[]
            {
                10f,
                28f,
                46f
            }

            )
            {
                Box("Ceiling practical", x, 7, 3, 8, .12f, .4f, "DistrictLight");
                LightAt(new Vector3(x, 5, 1), stage == StageId.Clinic ? new Color(.78f, .9f, .87f) : new Color(1, .8f, .57f), 8, 14);
            }

            Box("Reception desk", 9, .65f, 4, 6, 1.3f, 1.6f, "DistrictWarm", true);
            Monitor(8, 1.35f, 4);
            Bench(9, 0, 1);
            if (stage == StageId.School)
            {
                for (int row = 0; row < 3; row++)
                    for (int c = 0; c < 4; c++)
                    {
                        float x = 21 + c * 7, z = -4 + row * 4.5f;
                        Table(x, 0, z, 2.8f, 1.6f);
                        CivicChair(x, 0, z - 1.15f);
                        Box("Notebook", x + .4f, .96f, z, .65f, .045f, .8f, "DistrictIvory");
                    }

                CivicNpc("유진 선생님", "teacher", new Vector3(15, .05f, 2), "이곳은 이름을 다시 배우는 교실이에요. 네가 가져온 기록으로 아이들에게 도시의 역사를 들려줄 수 있겠죠.");
                Furnish("교실 기록 · 오늘의 수업", 47, 0, 6, text: "오늘의 질문: 기억은 누가 소유할 수 있을까? 칠판 아래에는 학생들의 서로 다른 답이 붙어 있다.");
            }
            else if (stage == StageId.Clinic)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = 22 + i * 8;
                    Bed(x, 0, 4, true);
                    Monitor(x + 2.8f, .9f, 5.5f);
                    Box("Privacy divider", x + 3.5f, 1.3f, 4, .12f, 2.6f, 4, "DistrictBlue", true);
                }

                CivicNpc("소은 의료원", "medic", new Vector3(13, .05f, 1), "맥박은 안정적이에요. 기억이 돌아오는 통증이 심해지면 언제든 찾아와요.", InteractionKind.Furniture, true);
                Furnish("치료실 · 회복", 21, 0, 1, text: "검사를 마치고 체력을 회복했다. 서하의 기억 신호는 점차 안정되고 있다.", heal: true);
            }
            else
            {
                Table(27, 0, 1, 9, 4);
                Box("Operations map", 27, .96f, 1, 8, .08f, 3.2f, "DistrictBlue");
                for (int i = 0; i < 6; i++)
                    Box("Map route", 24 + i * 1.2f, 1.02f, 1, .045f, .03f, 2.8f, "DistrictLight");
                for (int i = 0; i < 4; i++)
                {
                    Table(21 + i * 8, 0, 7, 4);
                    Monitor(21 + i * 8, .95f, 7);
                }

                CivicNpc("지안 대장", "commander", new Vector3(15, .05f, 1), "노아가 중앙역 승차권을 맡겼어. 열차의 코어를 되찾으면 서하의 주거동에 관한 고층 기록도 조사할 수 있어.");
                Terminal(InteractionKind.FirstRail, "중앙역 · 첫 번째 임무", 35, 0, -4);
                Terminal(InteractionKind.BreachMission, "외벽 침투 · 스물네 번째 창", 46, 0, -4);
                Terminal(InteractionKind.MissionBoard, "도시 복원 · 다음 캠페인", 18, 0, -4);
            }

            FacilityDetail(stage);
        }

        static void FacadeRoute(DistrictSpec s)
        {
            Deck(-8, 24, 0);
            Deck(28, 39, 6);
            Deck(45, 56, 12);
            Deck(62, 74, 18);
            Deck(82, s.length + 7, 24);
            Anchor(27, 10, 0);
            Anchor(45, 17, 0);
            Anchor(62, 24, 0);
            Anchor(80, 31, 0);
            for (int i = 0; i < 6; i++)
            {
                float x = 25 + i * 11;
                Box("Facade / vertical tower", x, 13, 7, 10.8f, 34, 3, "DistrictStone");
                for (int row = 0; row < 7; row++)
                {
                    Box("Facade window", x, 2 + row * 4.5f, 5.46f, 7, 3, .06f, "DistrictWindow");
                    Box("Facade sill", x, 3.65f + row * 4.5f, 5.3f, 8, .18f, .55f, "Metal");
                }

                Box("Facade service riser", x + 4.7f, 14, 5.2f, .22f, 34, .3f, "Gold");
            }

            for (int i = 0; i < 4; i++)
            {
                float x = 31 + i * 17, y = 6 + i * 6;
                Sign("RMB + W ↑ · SPACE →", new Vector3(x, y + 3, 5), 5, .65f, "Amber");
            }

            Box("24F entry glass", 86, 25.9f, 0, .12f, 3.8f, 7.6f, "Glass", true).AddComponent<BreakableGlass>();
            foreach (float z in new[]
            {
                -3.7f,
                3.7f
            }

            )
                Box("Window entry jamb", 86, 26, z, .35f, 4.1f, .2f, "Gold");
            Sign("24F · 창문을 공격", new Vector3(87, 29.2f, 4.1f), 6, .75f, "Amber");
            for (int i = 0; i < 7; i++)
            {
                float x = 93 + i * 7;
                Box("Records cabinet", x, 25.5f, 4, 3, 3, 1.3f, "DistrictIvory");
                for (int r = 0; r < 5; r++)
                    Box("File drawer", x, 24.4f + r * .55f, 3.3f, 2.7f, .44f, .1f, "DistrictWarm");
            }
        }
    }
}
