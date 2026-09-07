using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/City life/Upgrade existing scenes and build release")]
        public static void CityLifeAndRelease()
        {
            UpgradeCityLife();
            OptimizeAllScenes();
            BuildRelease();
        }

        public static void UpgradeCityLife()
        {
            AssetDatabase.Refresh();
            LifeMaterial("BuildingFade", "AfterSignal/Building Fade");
            LifeMaterial("CitySky", "AfterSignal/City Sky");
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            foreach (var entry in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                var g = Object.FindAnyObjectByType<GameDirector>();
                if (!g)
                    continue;
                world = GameObject.Find("WORLD / editable architecture").transform;
                var manifest = Object.FindAnyObjectByType<SceneBatchManifest>();
                if (manifest)
                {
                    if (manifest.sources != null)
                        foreach (var r in manifest.sources)
                            if (r)
                                r.enabled = true;
                    if (manifest.generated != null)
                        foreach (var item in manifest.generated)
                            if (item)
                                Object.DestroyImmediate(item);
                    manifest.sources = null;
                    manifest.generated = null;
                }

                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t && (t.name == "CITY LIFE / additions" || t.name == "CITY LIFE / rooftop" || t.name == "CITY LIFE / room population" || t.name == "NEON / district retrofit"))
                        Object.DestroyImmediate(t.gameObject);
                CleanArchitecture(g);
                var root = new GameObject("CITY LIFE / additions").transform;
                root.SetParent(world, false);
                if (g.stage == StageId.UrbanCity)
                {
                    UpgradeRoofs(root);
                    StreetAnchors(root);
                }

                if (g.stage == StageId.Haven)
                {
                    StreetAnchors(root);
                    UpgradeTownRoofs(root);
                }

                if (CivicWorld.Interior(g.stage))
                    UpgradeInteriors(g, root);
                MarkOccluders(g);
                Physics.SyncTransforms();
                g.spawn = CivicWorld.SafeSpawn(g.stage, g.spawn);
                g.checkpoint = g.spawn;
                EditorUtility.SetDirty(g);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("CITY LIFE 1.6: existing scenes upgraded; authored mission ids retained.");
        }

        static void LifeMaterial(string name, string shader)
        {
            string path = resourceRoot + "Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(mat, path);
            }
            else
                mat.shader = Shader.Find(shader);
            EditorUtility.SetDirty(mat);
        }

        static void CleanArchitecture(GameDirector game)
        {
            bool interior = CivicWorld.Interior(game.stage);
            bool train = game.stage == StageId.Carriage || game.stage == StageId.Roof;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!t)
                    continue;
                string name = t.name;
                bool wallPicture = name.StartsWith("ART / ") && (interior || train || game.stage == StageId.Station || game.stage == StageId.Lab || game.stage == StageId.Archive || game.stage == StageId.Origin || game.stage == StageId.Foundry);
                bool sign = interior && (name.StartsWith("IMAGE SIGN /") || name.StartsWith("Sign / ") || name == "Sign rim" || name.StartsWith("Lettering / ") || name == "Facade / authored surface");
                bool furniture = interior && (name == "Surface / Wardrobe" || name == "Surface / Refrigerator" || name == "Surface / Cabinet");
                if (wallPicture || sign || furniture)
                {
                    Object.DestroyImmediate(t.gameObject);
                    continue;
                }

                if (game.stage == StageId.UrbanCity && name == "Facade / authored surface")
                {
                    // Flat storefront photos contained unrelated businesses; the structural shells get real glazing below.
                    var m = t.GetComponent<Renderer>().sharedMaterial;
                    bool ground = m && (m.name == "Urban-Facade-02" || m.name == "Urban-Facade-03" || m.name == "Urban-Facade-04" || m.name == "Urban-Facade-07");
                    if (t.GetComponentInParent<CityBuildingCutaway>() || ground)
                        Object.DestroyImmediate(t.gameObject);
                }
            }

            if (train)
            {
                foreach (var sign in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (sign && sign.name.StartsWith("IMAGE SIGN /") && !sign.GetComponentInParent<RailMotion>())
                        Object.DestroyImmediate(sign.gameObject);
            }

            if (game.stage == StageId.UrbanCity)
            {
                foreach (var point in Object.FindObjectsByType<InteractionPoint>(FindObjectsSortMode.None))
                    if (point.kind == InteractionKind.UrbanEnter)
                        point.title = UrbanCatalog.Name(point.siteId) + " · 들어가기";
            }
        }

        static void UpgradeRoofs(Transform additions)
        {
            var groups = Object.FindObjectsByType<CityBuildingCutaway>(FindObjectsSortMode.None);
            int index = 0;
            foreach (var cut in groups)
            {
                var core = cut.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c => c.name == "Tower core" || c.name == "Upper building volume");
                if (!core || core.transform == cut.transform)
                    continue;
                var bounds = core.bounds;
                int site = -1;
                var siteRoot = cut.transform.parent;
                if (siteRoot && siteRoot.name.StartsWith("SITE / "))
                {
                    var pieces = siteRoot.name.Split('/');
                    int.TryParse(pieces[1].Trim(), out site);
                }

                // Roof geometry uses world dimensions, independent of the source building's scale.
                var root = new GameObject("CITY LIFE / rooftop").transform;
                root.SetParent(cut.transform, true);
                var roof = root.gameObject.AddComponent<CityRooftop>();
                roof.site = site;
                float top = bounds.max.y + .56f;
                Vector3 center = bounds.center;
                float w = bounds.size.x, d = bounds.size.z, z = bounds.min.z;
                RoofRoute(root, center, w, d, top, roof, index++);
                if (site >= 0)
                {
                    foreach (var sign in siteRoot.GetComponentsInChildren<Transform>())
                        if (sign.name.StartsWith("IMAGE SIGN /"))
                        {
                            sign.position = new Vector3(center.x, 5.8f, z - .22f);
                            Box("Mounted sign backing", center.x, 5.8f, z - .08f, 23.2f, 4.2f, .22f, "DarkMetal", false, root);
                            if (UrbanCatalog.IsHotel(site))
                            {
                                Object.DestroyImmediate(sign.gameObject);
                                Sign("AFTERVIEW / 호텔", new Vector3(center.x, 5.8f, z - .23f), 19, 1.8f, "Amber", root);
                            }
                        }
                // Ground-level service sign is mounted on the actual facade, above its canopy.
                }

                if (UrbanCatalog.IsHotel(site) && !root.GetComponentsInChildren<TextMesh>().Any(t => t.text.Contains("AFTERVIEW")))
                {
                    Box("Hotel sign backing", center.x, 5.8f, z - .08f, 20, 2, .22f, "DarkMetal", false, root);
                    Sign("AFTERVIEW / 호텔", new Vector3(center.x, 5.8f, z - .23f), 19, 1.8f, "Amber", root);
                }

                CombineLifeDetails(root, "roof-" + (index - 1));
                cut.upper = cut.GetComponentsInChildren<Renderer>(true);
            }
        }

        static void RoofRoute(Transform root, Vector3 center, float width, float depth, float top, CityRooftop roof, int style)
        {
            float front = center.z - depth * .5f;
            float routeX = center.x - width * .32f;
            Box("Walkable roof terrace", center.x, top - .18f, center.z, width + .8f, .36f, depth + .8f, "UrbanWalk", true, root);
            // Front parapet has a landing opening; side and rear rails protect the lookout.
            foreach (float side in new[]
            {
                -1f,
                1f
            }

            )
            {
                Box("Roof side parapet", center.x + side * (width * .5f + .3f), top + .5f, center.z, .25f, 1, depth + .8f, "Metal", true, root);
                Box("Roof edge light", center.x + side * (width * .5f + .31f), top + 1.04f, center.z, .12f, .08f, depth + .8f, "NeonAzure", false, root);
            }

            Box("Roof rear parapet", center.x, top + .5f, center.z + depth * .5f + .3f, width, 1, .25f, "Metal", true, root);
            for (float level = 5; level < top - 6.5f; level += 8)
                FacadeLanding(root, new Vector3(routeX, level, front - 1.25f));
            var landing = new Vector3(routeX, top + .06f, front - 1.1f);
            FacadeLanding(root, new Vector3(routeX, top, front - 1.1f));
            roof.landing = landing;
            Box("Roof landing bridge", routeX, top - .18f, front + .15f, 4, .36f, 3, "Chrome", true, root);
            var view = Interact(InteractionKind.Viewpoint, "옥상 전망 · 도시 바라보기", new Vector3(center.x + width * .18f, top + 1.2f, front + 2.4f));
            view.transform.SetParent(root, true);
            var original = world;
            world = root;
            Bench(center.x + width * .2f, top, front + 3.5f);
            Table(center.x + width * .25f, top, front + 1.5f, 2);
            foreach (float x in new[]
            {
                center.x - width * .25f,
                center.x + width * .25f
            }

            )
            {
                Box("Rooftop garden planter", x, top + .45f, center.z + depth * .26f, 4, .9f, 2, "DistrictWarm", true);
                Box("Roof garden foliage", x, top + 1, center.z + depth * .26f, 3.8f, .45f, 1.8f, "Leaf");
            }

            world = original;
            // Insets, sills and ribs are real volumes; no storefront photography is used on these walls.
            for (float y = 8; y < top - 2; y += 4.8f)
                for (int col = 0; col < 5; col++)
                {
                    float x = center.x - width * .4f + col * width * .2f;
                    Box("Recessed 3D window", x, y, front - .08f, width * .13f, 2.7f, .18f, (col + (int)y + style) % 5 == 0 ? "DistrictLight" : "DistrictWindow", false, root);
                    Box("Projecting window sill", x, y - 1.4f, front - .21f, width * .14f, .12f, .48f, "Chrome", false, root);
                }

            for (int col = 0; col < 6; col++)
            {
                float x = center.x - width * .5f + col * width * .2f;
                Box("Facade vertical rib", x, top * .5f, front - .2f, .2f, top, .38f, "Metal", false, root);
            }

            foreach (float side in new[]
            {
                -1f,
                1f
            }

            )
                for (float y = 8; y < top - 2; y += 5.5f)
                    for (int row = 0; row < 3; row++)
                    {
                        float z = center.z - depth * .32f + row * depth * .32f;
                        Box("Side glazing", center.x + side * (width * .5f + .08f), y, z, .16f, 3, depth * .2f, "DistrictWindow", false, root);
                    }
        }

        static void FacadeLanding(Transform parent, Vector3 p)
        {
            Box("Facade service platform", p.x, p.y - .15f, p.z, 4, .3f, 2.6f, "Metal", true, parent);
            Box("Cantilever support", p.x, p.y - .65f, p.z + .85f, 3.4f, 1, 1, "DarkMetal", false, parent);
            Box("Grapple bracket post", p.x + 1.25f, p.y + 2.5f, p.z, .12f, 5, .12f, "Chrome", false, parent);
            Box("Grapple bracket cantilever", p.x + 1.25f, p.y + 5, p.z - 1.6f, .12f, .12f, 3.3f, "Chrome", false, parent);
            Box("Grapple bracket arm", p.x + .6f, p.y + 5, p.z - 3.2f, 1.4f, .12f, .12f, "Chrome", false, parent);
            LifeAnchor(parent, p + Vector3.up * 5 + Vector3.back * 3.2f, p + Vector3.up * .06f + Vector3.back * .55f, true);
        }

        static void LifeAnchor(Transform parent, Vector3 at, Vector3 landing, bool mount)
        {
            var go = new GameObject("CITY ROPE / structural anchor");
            go.transform.SetParent(parent, true);
            go.transform.position = at;
            go.layer = 10;
            var anchor = go.AddComponent<GrappleAnchor>();
            anchor.cityAnchor = true;
            anchor.hasLanding = mount;
            anchor.landing = landing;
            anchor.label = mount ? "외벽 발판 / W로 올라서기" : "도시 로프";
            var ring = Box("Grapple attachment", at.x, at.y, at.z, .36f, .36f, .3f, "Cyan", false, go.transform);
            ring.transform.rotation = Quaternion.Euler(0, 0, 45);
            anchor.ring = ring.GetComponent<Renderer>();
        }

        static void StreetAnchors(Transform root)
        {
            foreach (var pole in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (pole && (pole.name == "Road light arm" || pole.name == "Signal mast" || pole.name == "Street lamp pole" || pole.name == "Lamp post"))
                {
                    var r = pole.GetComponent<Renderer>();
                    if (!r)
                        continue;
                    LifeAnchor(root, new Vector3(r.bounds.center.x, r.bounds.max.y + .35f, r.bounds.center.z - .35f), Vector3.zero, false);
                }
        }

        static void UpgradeTownRoofs(Transform root)
        {
            int index = 0;
            foreach (var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                if (c.name.EndsWith(" / building mass"))
                {
                    var bounds = c.bounds;
                    var deck = new GameObject("CITY LIFE / rooftop").transform;
                    deck.SetParent(root, false);
                    var roof = deck.gameObject.AddComponent<CityRooftop>();
                    RoofRoute(deck, bounds.center, bounds.size.x, bounds.size.z, bounds.max.y + .1f, roof, index);
                    CombineLifeDetails(deck, "town-" + index++);
                }
        }

        static void UpgradeInteriors(GameDirector g, Transform additions)
        {
            if (g.stage == StageId.UrbanInterior)
            {
                foreach (var floor in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                    if (floor.name == "Walkable deck")
                    {
                        floor.transform.position = new Vector3(29.5f, -.3f, -4);
                        floor.transform.localScale = new Vector3(65, .6f, 46);
                    }

                var themes = Object.FindAnyObjectByType<UrbanInterior>();
                for (int type = 0; type < themes.themes.Length; type++)
                {
                    var root = new GameObject("CITY LIFE / room population").transform;
                    root.SetParent(themes.themes[type].transform, false);
                    var original = world;
                    world = root;
                    string[][] roles =
                    {
                        new[]
                        {
                            "주민",
                            "관리사",
                            "방문객",
                            "입주 상담사",
                            "야간 근무자"
                        },
                        new[]
                        {
                            "순경",
                            "민원인",
                            "교통 조사관",
                            "분실물 담당자",
                            "변호사"
                        },
                        new[]
                        {
                            "소방관",
                            "구급대원",
                            "정비사",
                            "안전 강사",
                            "구조대원"
                        },
                        new[]
                        {
                            "은행원",
                            "상담사",
                            "보안 직원",
                            "사업자",
                            "고객"
                        },
                        new[]
                        {
                            "의사",
                            "간호사",
                            "환자",
                            "보호자",
                            "약사",
                            "접수 직원"
                        },
                        new[]
                        {
                            "교사",
                            "사서",
                            "학부모",
                            "교무 직원",
                            "봉사자"
                        },
                        new[]
                        {
                            "판매원",
                            "구매 고객",
                            "매장 매니저",
                            "진열 담당자",
                            "디자이너"
                        },
                        new[]
                        {
                            "점원",
                            "야간 근무자",
                            "동네 주민",
                            "배달원",
                            "물류 직원"
                        },
                        new[]
                        {
                            "재단사",
                            "스타일리스트",
                            "손님",
                            "디자이너",
                            "수선사"
                        },
                        new[]
                        {
                            "요리사",
                            "서빙 직원",
                            "식사 손님",
                            "단골 손님",
                            "배달원"
                        },
                        new[]
                        {
                            "역무원",
                            "출근 승객",
                            "여행객",
                            "정비사",
                            "안내원"
                        },
                        new[]
                        {
                            "주유 직원",
                            "운전자",
                            "정비사",
                            "배달원",
                            "택시 기사"
                        },
                        new[]
                        {
                            "관리인",
                            "운전자",
                            "정비사",
                            "예약 고객",
                            "안내원"
                        },
                        new[]
                        {
                            "물류 관리자",
                            "창고 직원",
                            "화물 기사",
                            "검수 직원",
                            "택배 기사"
                        },
                        new[]
                        {
                            "신호 기술자",
                            "분석관",
                            "복원대원",
                            "기록관",
                            "연락원"
                        },
                        new[]
                        {
                            "바리스타",
                            "카페 단골",
                            "작가",
                            "음악가",
                            "여행객"
                        }
                    };
                    string[] names =
                    {
                        "가람",
                        "윤슬",
                        "민재",
                        "예림",
                        "서율",
                        "은재"
                    };
                    for (int i = 0; i < roles[type].Length; i++)
                    {
                        var pos = new Vector3(19 + i * 7, .06f, i % 2 == 0 ? -10.5f : -13.5f);
                        string art = type == 4 ? "medic" : type == 5 ? "teacher" : type == 1 || type == 14 ? "commander" : i % 3 == 0 ? "teacher" : "concierge";
                        CivicNpc(names[i] + " · " + roles[type][i], art, pos, UrbanCatalog.Names[type] + "에서 " + roles[type][i] + "로 생활한다. " + UrbanCatalog.Descriptions[type]);
                    }

                    if (type == 0)
                    {
                        var bed = Interact(InteractionKind.Sleep, "객실 침대 · 휴식", new Vector3(25, 1.2f, 7));
                        bed.restoresHealth = true;
                        Interact(InteractionKind.Wardrobe, "객실 옷장", new Vector3(53, 1.2f, 10));
                    }

                    // Tangible furniture details replace photographed cabinet fronts.
                    for (int i = 0; i < 4; i++)
                    {
                        Box("Reception drawer", 9.4f + i * 1.7f, .6f, 6.93f, 1.5f, .8f, .1f, "WarmWood");
                        Box("Drawer handle", 9.4f + i * 1.7f, .75f, 6.8f, .5f, .07f, .16f, "Chrome");
                    }

                    world = original;
                }

                Box("Interior front safety curb", 29.5f, .6f, -19.35f, 65, 1.2f, .25f, "WarmWood", true, additions);
            }
            else
            {
                var original = world;
                world = additions;
                if (g.stage == StageId.Residence)
                {
                    CivicNpc("우편 배달원 나린", "teacher", new Vector3(39, .05f, 6), "새벽아파트의 편지를 배달하는 주민이다.");
                    CivicNpc("주민 서진", "concierge", new Vector3(48, .05f, -3), "0607호 이웃이며 도시 정원 관리에 관심이 많다.");
                    Box("Room front safety wall", 12, 22.6f, -6.1f, 31, 1.2f, .25f, "WarmWood", true);
                    Box("Room right rear sill", 12, 24.4f, 5.6f, 25, .15f, .6f, "WarmWood");
                }
                else
                {
                    string baseRole = g.stage == StageId.Clinic ? "medic" : g.stage == StageId.School ? "teacher" : "commander";
                    string[] people = g.stage == StageId.Clinic ? new[]
                    {
                        "의사 도윤",
                        "외래 환자 예림",
                        "보호자 은재",
                        "약사 윤슬"
                    }

                    : g.stage == StageId.School ? new[]
                    {
                        "사서 은재",
                        "학부모 가람",
                        "교사 서율",
                        "행정 직원 예림"
                    }

                    : new[]
                    {
                        "신호 기술자 민재",
                        "복원대원 나린",
                        "기록관 예림",
                        "연락원 은재"
                    };
                    for (int i = 0; i < people.Length; i++)
                        CivicNpc(people[i], baseRole, new Vector3(20 + i * 8, .05f, -6), CivicWorld.Title(g.stage) + "에서 생활하며 주민의 기억 복원을 돕는다.");
                    Interact(InteractionKind.LifeService, "시설 접수 · 서비스", new Vector3(9, 1.2f, -3));
                }

                world = original;
            }
        }

        static void MarkOccluders(GameDirector game)
        {
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r && !r.GetComponentInParent<CityBuildingCutaway>() && (r.name == "Structural fascia" || r.name == "Top corridor guard rail" || r.name == "Door header" || r.name == "Store awning" || r.name == "Shelter roof" || r.name == "Interior ceiling light"))
                {
                    var cut = r.gameObject.AddComponent<CityBuildingCutaway>();
                    cut.visualOnly = true;
                    cut.upper = new[]
                    {
                        r
                    };
                    cut.center = r.bounds.center;
                    cut.footprint = new Vector2(r.bounds.size.x, r.bounds.size.z);
                }

            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.isTrigger || c.GetComponentInParent<CityVehicle>() || c.GetComponentInParent<PixelActor>() || c.GetComponentInParent<MovingLift>() || c.GetComponentInParent<CityBuildingCutaway>() || c.GetComponentInParent<BreakableGlass>())
                    continue;
                var r = c.GetComponent<Renderer>();
                if (!r || c.bounds.max.y < 2 || c.bounds.size.x > 300)
                    continue;
                var cut = c.gameObject.AddComponent<CityBuildingCutaway>();
                cut.center = c.bounds.center;
                cut.footprint = new Vector2(c.bounds.size.x, c.bounds.size.z);
                cut.upper = new[]
                {
                    r
                };
            }
        }

        static void CombineLifeDetails(Transform root, string id)
        {
            var meshes = root.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<MeshRenderer>() && !f.GetComponentInParent<GrappleAnchor>() && !f.GetComponent<Collider>()).GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial).ToArray();
            int index = 0;
            foreach (var group in meshes)
            {
                var parts = group.ToArray();
                if (parts.Length < 2)
                    continue;
                var mesh = new Mesh
                {
                    name = "CityLife-" + id + "-" + index,
                    indexFormat = IndexFormat.UInt32
                };
                mesh.CombineMeshes(parts.Select(f => new CombineInstance { mesh = f.sharedMesh, transform = root.worldToLocalMatrix * f.transform.localToWorldMatrix }).ToArray(), true, true);
                string path = resourceRoot + "Geometry/" + mesh.name + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (asset)
                {
                    EditorUtility.CopySerialized(mesh, asset);
                    Object.DestroyImmediate(mesh);
                    mesh = asset;
                }
                else
                    AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject("3D facade details / " + index++, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = group.Key;
                foreach (var p in parts)
                    Object.DestroyImmediate(p.gameObject);
            }
        }
    }
}
