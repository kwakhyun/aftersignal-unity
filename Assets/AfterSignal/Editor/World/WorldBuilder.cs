using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static Transform world;
        static void BuildScene(StageId stage, GameTuning tuning)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject("WORLD / editable architecture").transform;
            var game = new GameObject("CAMPAIGN / " + stage).AddComponent<GameDirector>();
            game.stage = stage;
            game.tuning = tuning;
            var spec = CampaignCatalog.Get(stage);
            game.stageLength = spec != null ? spec.length : stage == StageId.Station ? 58 : stage == StageId.Carriage ? 62 : stage == StageId.Roof ? 76 : 118;
            game.halfDepth = spec != null ? 3.4f : stage == StageId.Station ? 3 : stage == StageId.Haven ? 6 : 1.9f;
            game.spawn = stage == StageId.Station ? new Vector3(4, 5.1f, -.5f) : new Vector3(4, .15f, 0);
            if (stage == StageId.Haven)
            {
                game.stageLength = 226;
                game.halfDepth = 42;
            }

            if (CivicWorld.Interior(stage))
            {
                game.stageLength = stage == StageId.Residence ? 94 : 52;
                game.halfDepth = 14;
                if (stage == StageId.Residence)
                    game.spawn = new Vector3(8, 22.15f, 0);
            }

            if (stage == StageId.UrbanCity)
            {
                game.stageLength = 790;
                game.halfDepth = 330;
                game.spawn = new Vector3(50, .15f, -295);
                BuildUrbanCity();
            }
            else if (stage == StageId.UrbanInterior)
            {
                game.stageLength = 58;
                game.halfDepth = 19;
                game.spawn = new Vector3(5, .15f, -10);
                BuildUrbanInterior();
            }
            else if (stage == StageId.Residence)
                Residence();
            else if (CivicWorld.Interior(stage))
                Facility(stage);
            else if (spec != null)
                District(spec);
            else if (stage == StageId.Station)
                Station();
            else if (stage == StageId.Carriage)
                Carriage();
            else if (stage == StageId.Roof)
                Roof();
            else
            {
                ExpandedHaven();
                CivicTown();
            }

            var hero = new GameObject("Seo / player", typeof(CharacterController), typeof(PixelActor), typeof(PlayerMotor));
            hero.transform.position = game.spawn;
            hero.GetComponent<PixelActor>().art = "Hero";
            hero.GetComponent<PixelActor>().Initialize();
            ShadowProxy(hero.transform, 2.1f);
            var controller = hero.GetComponent<CharacterController>();
            controller.height = 2.08f;
            controller.center = Vector3.up * 1.06f;
            controller.radius = .34f;
            hero.layer = 8;
            PrefabUtility.SaveAsPrefabAsset(hero, "Assets/AfterSignal/Prefabs/Seo.prefab");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraRig));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 43;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 240;
            camera.allowHDR = true;
            if (stage == StageId.UrbanCity)
                camera.fieldOfView = 48;
            camera.backgroundColor = new Color(.025f, .04f, .09f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            if (stage == StageId.UrbanCity)
            {
                camera.farClipPlane = 1100;
                camera.backgroundColor = new Color(.12f, .22f, .29f);
            }

            cameraObject.transform.position = new Vector3(11, game.spawn.y + 4.5f, -21);
            cameraObject.transform.rotation = Quaternion.Euler(10, 0, 0);
            var camData = camera.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var sun = new GameObject("Moon / key light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(.6f, .76f, 1);
            sun.intensity = .95f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42, -26, 0);
            if (stage == StageId.UrbanCity || stage == StageId.UrbanInterior)
            {
                sun.intensity = 1.5f;
                sun.color = new Color(.83f, .84f, .95f);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.19f, .25f, .39f);
            RenderSettings.ambientEquatorColor = new Color(.11f, .17f, .22f);
            RenderSettings.ambientGroundColor = new Color(.05f, .07f, .11f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.033f, .063f, .12f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = stage == StageId.Roof ? .007f : .005f;
            if (stage == StageId.UrbanCity)
            {
                RenderSettings.fogColor = new Color(.08f, .16f, .21f);
                RenderSettings.fogDensity = .0023f;
                RenderSettings.ambientSkyColor = new Color(.29f, .39f, .5f);
                RenderSettings.ambientEquatorColor = new Color(.22f, .29f, .34f);
            }

            var volume = new GameObject("Color / atmosphere").AddComponent<Volume>();
            volume.isGlobal = true;
            string profilePath = "Assets/Settings/AfterSignal_" + stage + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }

            bloom.intensity.Override(.36f);
            bloom.threshold.Override(1.1f);
            bloom.scatter.Override(.64f);
            if (!profile.TryGet<Tonemapping>(out var tone))
            {
                tone = profile.Add<Tonemapping>(true);
                AssetDatabase.AddObjectToAsset(tone, profile);
            }

            tone.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade = profile.Add<ColorAdjustments>(true);
                AssetDatabase.AddObjectToAsset(grade, profile);
            }

            grade.postExposure.Override(stage == StageId.UrbanCity ? .55f : .35f);
            grade.contrast.Override(stage == StageId.UrbanCity ? 4 : 12);
            grade.saturation.Override(-7);
            EditorUtility.SetDirty(grade);
            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
                AssetDatabase.AddObjectToAsset(vignette, profile);
            }

            vignette.intensity.Override(.22f);
            vignette.smoothness.Override(.5f);
            volume.sharedProfile = profile;
            InstallBackdrop(stage, game.stageLength);
            EditorSceneManager.SaveScene(scene, "Assets/AfterSignal/Scenes/" + CampaignRules.Scene(stage) + ".unity");
        }

        static GameObject Box(string name, float x, float y, float z, float w, float h, float d, string material, bool solid = false, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent ? parent : world, false);
            go.transform.position = new Vector3(x, y, z);
            go.transform.localScale = new Vector3(w, h, d);
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            if (!solid)
                Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Cylinder(string name, Vector3 p, Vector3 scale, string material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent ? parent : world, false);
            go.transform.position = p;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void ShadowProxy(Transform actor, float height)
        {
            var proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            proxy.name = "Contact shadow proxy";
            proxy.transform.SetParent(actor, false);
            proxy.transform.localPosition = Vector3.up * height * .5f;
            proxy.transform.localScale = new Vector3(height * .27f, height * .5f, height * .27f);
            Object.DestroyImmediate(proxy.GetComponent<Collider>());
            var render = proxy.GetComponent<Renderer>();
            render.sharedMaterial = Mat("DarkMetal");
            render.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        static void Sign(string title, Vector3 pos, float width, float height, string glow = "Cyan", Transform parent = null)
        {
            int image = UrbanSignIndex(title);
            if (image >= 0 && AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot + "Art/Urban/Sign-" + image.ToString("00") + ".png"))
            {
                UrbanPlate(image, pos, width, Mathf.Max(height, width / 5.7f), parent);
                return;
            }

            Box("Sign / " + title, pos.x, pos.y, pos.z, width, height, .15f, "DarkMetal", false, parent);
            Box("Sign rim", pos.x, pos.y - height / 2, pos.z - .1f, width, .035f, .07f, glow, false, parent);
            var go = new GameObject("Lettering / " + title, typeof(TextMesh));
            go.transform.SetParent(parent ? parent : world, false);
            go.transform.position = pos + new Vector3(0, 0, -.1f);
            var text = go.GetComponent<TextMesh>();
            text.text = title;
            text.font = AssetDatabase.LoadAssetAtPath<Font>(resourceRoot + "Fonts/NotoSansKR.ttf");
            text.fontSize = 72;
            text.characterSize = Mathf.Min(height * .17f, width / (Mathf.Max(1, title.Length) * .42f));
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = glow == "Amber" ? new Color(1, .78f, .43f) : new Color(.58f, .96f, .91f);
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            text.font.RequestCharactersInTexture(title, 72, FontStyle.Normal);
            float line = 0, widest = 1;
            int rows = 1;
            foreach (char c in title)
            {
                if (c == '\n')
                {
                    widest = Mathf.Max(widest, line);
                    line = 0;
                    rows++;
                    continue;
                }

                line += text.font.GetCharacterInfo(c, out var glyph, 72) ? glyph.advance : 72;
            }

            widest = Mathf.Max(widest, line);
            text.characterSize = Mathf.Min(height * .78f / (7.2f * rows), width * .9f / (widest * .1f));
        }

        static void LightAt(Vector3 pos, Color color, float intensity = 5, float range = 10)
        {
            var go = new GameObject("Practical neon light");
            go.transform.SetParent(world);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        static void Anchor(float x, float y, float z, int capacitor = -1)
        {
            var root = new GameObject(capacitor >= 0 ? "CAPACITOR / " + capacitor : "GRAPPLE / " + x);
            root.transform.SetParent(world);
            root.transform.position = new Vector3(x, y, z);
            root.layer = 10;
            var anchor = root.AddComponent<GrappleAnchor>();
            anchor.capacitor = capacitor;
            var ring = Cylinder("Anchor housing", new Vector3(x, y, z), new Vector3(.65f, .09f, .65f), "Metal", root.transform);
            ring.transform.rotation = Quaternion.Euler(90, 0, 0);
            var core = Box("Rotating signal", x, y, z - .12f, .38f, .38f, .14f, "Cyan", false, root.transform);
            core.transform.rotation = Quaternion.Euler(0, 0, 45);
            anchor.ring = core.GetComponent<Renderer>();
            Box("Support", x, y + 1, z, .09f, 1.9f, .09f, "Chrome", false, root.transform);
            Sign(capacitor >= 0 ? "CAP 0" + (capacitor + 1) : "RMB", new Vector3(x, y + .7f, z - .1f), 1.2f, .4f, "Cyan", root.transform);
        }

        static InteractionPoint Interact(InteractionKind kind, string title, Vector3 p, string dialogue = "")
        {
            var go = new GameObject("INTERACT / " + title);
            go.transform.SetParent(world);
            go.transform.position = p;
            var point = go.AddComponent<InteractionPoint>();
            point.kind = kind;
            point.title = title;
            point.dialogue = dialogue;
            return point;
        }

        static void Terminal(InteractionKind kind, string title, float x, float y, float z)
        {
            Box("Terminal stand", x, y + .6f, z, .8f, 1.2f, .55f, "Metal", false);
            Box("Terminal display", x, y + 1.5f, z - .05f, 1.1f, .8f, .18f, "DarkMetal");
            Box("Active screen", x, y + 1.5f, z - .16f, .86f, .52f, .035f, "Cyan");
            Sign("E / LINK", new Vector3(x, y + 2.15f, z), 1.7f, .42f);
            Interact(kind, title, new Vector3(x, y + 1.25f, z));
        }

        static void Npc(string name, string art, Vector3 pos, string dialogue, InteractionKind kind = InteractionKind.Citizen)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(world);
            go.transform.position = pos;
            var sprite = go.GetComponent<SpriteRenderer>();
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(resourceRoot + "Art/NPC/" + art + ".png");
            sprite.sharedMaterial = Mat("PixelActor");
            if (sprite.sprite)
            {
                var source = new Texture2D(2, 2);
                source.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.sprite)));
                var pixels = source.GetPixels32();
                int low = source.height, high = 0;
                for (int y = 0; y < source.height; y++)
                    for (int x = 0; x < source.width; x++)
                        if (pixels[y * source.width + x].a > 64)
                        {
                            low = Mathf.Min(low, y);
                            high = Mathf.Max(high, y);
                        }

                go.transform.localScale = Vector3.one * (2.15f * sprite.sprite.pixelsPerUnit / Mathf.Max(1, high - low + 1));
                Object.DestroyImmediate(source);
            }

            go.transform.rotation = Quaternion.Euler(10, 0, 0);
            ShadowProxy(go.transform, 2.1f / go.transform.localScale.x);
            Interact(kind, name, pos + Vector3.up * 1.3f, dialogue);
        }

        static void Enemy(string kind, float x, float z, float activate, float hp = 72, bool boss = false)
        {
            var go = new GameObject(boss ? "CONDUCTOR / shielded core" : "Guard / " + kind, typeof(CharacterController), typeof(PixelActor), typeof(EnemyBrain));
            go.transform.SetParent(world);
            go.transform.position = new Vector3(x, .1f, z);
            go.layer = 9;
            var c = go.GetComponent<CharacterController>();
            c.height = boss ? 3.8f : 2.1f;
            c.radius = boss ? .8f : .32f;
            c.center = Vector3.up * c.height / 2;
            c.stepOffset = .3f;
            var actor = go.GetComponent<PixelActor>();
            actor.art = "Enemies/" + kind;
            actor.bodyScale = boss ? 1.65f : 1;
            actor.Initialize();
            ShadowProxy(go.transform, boss ? 3.8f : 2.1f);
            var e = go.GetComponent<EnemyBrain>();
            e.kind = kind;
            e.maxHealth = hp;
            e.boss = boss;
            e.activateAt = activate;
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/AfterSignal/Prefabs/" + (boss ? "Conductor" : kind) + ".prefab");
        }

        static void Bench(float x, float y, float z, Transform parent = null)
        {
            Box("Bench seat", x, y + .55f, z, 3.4f, .18f, .7f, "Seat", false, parent);
            Box("Bench back", x, y + 1, z + .3f, 3.4f, .8f, .15f, "Seat", false, parent);
            foreach (float dx in new[]
            {
                -1.25f,
                1.25f
            }

            )
            {
                Box("Bench leg", x + dx, y + .25f, z, .12f, .5f, .5f, "Chrome", false, parent);
                Box("Bench arm", x + dx, y + .88f, z, .1f, .1f, .6f, "Chrome", false, parent);
            }

            for (int i = 0; i < 4; i++)
                Box("Seat joint", x - 1.3f + i * .88f, y + .65f, z, .025f, .025f, .72f, "DarkMetal", false, parent);
        }

        static void Station()
        {
            Box("Lower platform", 29, -.35f, 0, 60, .7f, 8, "Tile", true);
            Box("Upper concourse", 10, 4.6f, 0, 21, .8f, 7.8f, "Concrete", true);
            Box("Upper floor inlay", 10, 5.015f, 0, 21, .03f, 7.7f, "Tile");
            Box("Platform foundation", 30, -1.7f, 0, 60, 2, 8, "Concrete");
            Box("Tunnel rear wall", 29, 6.5f, 9.5f, 62, 17, .65f, "Concrete");
            for (int i = 0; i < 14; i++)
            {
                float x = i * 4.5f;
                Box("Recessed wall bay", x, 6, 9.05f, 4.15f, 10, .22f, "DarkMetal");
                Box("Ceramic wall panel", x, 3.6f, 8.88f, 3.8f, 4.6f, .12f, "Tile");
                Box("Turquoise route strip", x, 6.2f, 8.73f, 4.1f, .24f, .07f, "Cyan");
                Box("Copper sill", x, 1.25f, 8.7f, 4.1f, .11f, .11f, "Gold");
            }

            for (int i = 0; i < 8; i++)
            {
                float x = 2 + i * 8;
                Box("Structural column", x, 5.6f, 4.4f, .65f, 12, .75f, "Metal");
                Box("Column base", x, .4f, 4.4f, 1.05f, .8f, 1.1f, "Concrete");
                Box("Column capital", x, 10.8f, 4.4f, 1.25f, .55f, 1.35f, "Chrome");
                Box("Ceiling cross beam", x, 11.6f, 2, .42f, .65f, 15, "Metal");
                Box("Ceiling lamp", x, 11.19f, 1, .22f, .12f, 5, "Amber");
                Box("Ceiling trunk", x, 12.1f, 5, 7.7f, .35f, .8f, "DarkMetal");
                LightAt(new Vector3(x, 7, 1), i % 2 == 0 ? new Color(.22f, .8f, .86f) : new Color(1, .65f, .35f), 6, 12);
            }

            for (int i = 0; i < 45; i++)
            {
                float x = i * 1.35f;
                Box("Tactile warning", x, .035f, -3.45f, 1.2f, .04f, .42f, "Gold");
                Box("Platform edge", x, .04f, 3.75f, 1.2f, .04f, .3f, "Amber");
            }

            // A continuous collision ramp under the visible escalator treads prevents stair jitter.
            var ramp = Box("Escalator collision", 26, 2.36f, -.2f, 13.3f, .3f, 3.1f, "Metal", true);
            ramp.transform.rotation = Quaternion.Euler(0, 0, -22.62f);
            for (int i = 0; i < 30; i++)
            {
                float t = i / 29f;
                Box("Escalator tread", 20 + t * 12, 4.95f - t * 5, -.2f, .44f, .13f, 2.8f, "Chrome");
            }

            foreach (float z in new[]
            {
                -1.9f,
                1.5f
            }

            )
            {
                var side = Box("Escalator glass side", 26, 3.08f, z, 13.1f, 1.05f, .16f, "Glass");
                side.transform.rotation = Quaternion.Euler(0, 0, -22.62f);
                var rail = Box("Escalator handrail", 26, 3.67f, z, 13.1f, .11f, .12f, "Rubber");
                rail.transform.rotation = Quaternion.Euler(0, 0, -22.62f);
            }

            Sign("중앙역   CENTRAL / 04", new Vector3(12, 9.3f, 7), 13, 1.6f);
            Sign("NIGHT LINE  →", new Vector3(41, 7.6f, 5), 10, 1.1f, "Amber");
            Sign("기억을 잇는 마지막 열차", new Vector3(46, 3.6f, 8.6f), 8, 1.15f);
            Npc("NOA / 노아", "noa", new Vector3(7, 5.05f, 1.1f), "", InteractionKind.Noa);
            Terminal(InteractionKind.Power, "승강장 전력 복구", 16, 5, -.4f);
            Bench(11, 5, 2.8f);
            Bench(38, 0, 2.6f);
            Bench(47, 0, 2.6f);
            for (int i = 0; i < 3; i++)
            {
                Box("Ticket machine", 2 + i * 1.1f, 6.1f, 3.1f, .88f, 2.2f, .6f, "Gold");
                Box("Ticket screen", 2 + i * 1.1f, 6.5f, 2.77f, .65f, .7f, .04f, "Cyan");
            }

            Anchor(12, 10, -.5f);
            Anchor(26, 10, -.5f);
            Anchor(37, 7, 0);
            Anchor(48, 7, 0);
            Enemy("blade", 33, -.5f, 23, 72);
            Enemy("gunner", 38, 1, 25, 64);
            Enemy("blade", 40, -1, 25, 72);
            Enemy("stalker", 45, .3f, 36, 60);
            Enemy("blade", 48, -.6f, 36, 72);
            Enemy("gunner", 51, .8f, 36, 64);
            Interact(InteractionKind.Memory, "잃어버린 승차권", new Vector3(10, 6.2f, -2.4f), "승차권에는 목적지 대신 이름이 적혀 있다. '노아'. 열차는 장소가 아니라 기억으로 향한다.");
            Box("Memory fragment", 10, 5.7f, -2.4f, .22f, .48f, .18f, "Amber");
            var train = new GameObject("ARRIVING TRAIN / animated");
            train.transform.SetParent(world);
            TrainCar(47, 0, 6.4f, 18, train.transform);
            var motion = train.AddComponent<RailMotion>();
            motion.arrivalTrain = true;
            foreach (float direction in new[]
            {
                -1f,
                1f
            }

            )
            {
                var door = Box("Boarding sliding door", 53 + direction * .65f, 1.7f, 4.15f, 1.25f, 3.3f, .12f, "Metal");
                door.AddComponent<RailMotion>().door = true;
                door.GetComponent<RailMotion>().direction = direction;
            }

            Sign("E / BOARD", new Vector3(53, 4.1f, 3.95f), 3.2f, .7f, "Amber");
            Interact(InteractionKind.Board, "유령 열차 탑승", new Vector3(53, 1.3f, 2.2f));
            for (int i = 0; i < 47; i++)
            {
                Box("Rail sleeper", i * 1.4f, -.7f, 6.5f, .3f, .25f, 4, "Rubber");
            }

            Box("Rail", 29, -.4f, 5.4f, 70, .22f, .14f, "Chrome");
            Box("Rail", 29, -.4f, 7.4f, 70, .22f, .14f, "Chrome");
        }

        static void TrainCar(float center, float y, float z, float length, Transform parent)
        {
            Box("Car underbody", center, y - .6f, z, length, .75f, 4.6f, "DarkMetal", false, parent);
            Box("Car floor", center, y - .13f, z, length, .25f, 4.5f, "Metal", true, parent);
            Box("Far side lower shell", center, y + .65f, z + 2.25f, length, 1.3f, .18f, "LightTile", false, parent);
            Box("Window roof lintel", center, y + 3.5f, z + 2.25f, length, .6f, .22f, "LightTile", false, parent);
            Box("Copper route band", center, y + 1.32f, z + 2.12f, length, .12f, .035f, "Gold", false, parent);
            Box("Ceiling shell", center, y + 4.04f, z, length, .2f, 4.7f, "Metal", true, parent);
            Box("Ceiling lining", center, y + 3.91f, z, length - .2f, .06f, 4.35f, "LightTile", false, parent);
            Box("Near cutaway lintel", center, y + 3.63f, z - 2.25f, length, .7f, .18f, "LightTile", false, parent);
            foreach (float side in new[]
            {
                -1.65f,
                1.65f
            }

            )
                Box("Ceiling light", center, y + 3.84f, z + side, length - .7f, .08f, .13f, "Amber", false, parent);
            foreach (float end in new[]
            {
                center - length / 2,
                center + length / 2
            }

            )
            {
                Box("Doorway arch", end, y + 3.5f, z, .22f, 1f, 4.65f, "Chrome", false, parent);
                Box("Near door jamb", end, y + 1.8f, z - 2.26f, .2f, 3.6f, .2f, "Chrome", false, parent);
            }

            for (float x = center - length / 2 + .35f; x < center + length / 2; x += 3.6f)
            {
                Box("Window mullion", x, y + 2.35f, z + 2.23f, .21f, 2.3f, .23f, "Chrome", false, parent);
                Box("Window sill", x + 1.65f, y + 1.43f, z + 2.18f, 3.3f, .1f, .2f, "Chrome", false, parent);
                Box("Window glazing", x + 1.65f, y + 2.36f, z + 2.24f, 3.25f, 1.75f, .025f, "Glass", false, parent);
                Bench(x + 1.5f, y, z + 1.48f, parent);
                Cylinder("Grab pole", new Vector3(x + .5f, y + 1.85f, z - .15f), new Vector3(.06f, 1.85f, .06f), "Chrome", parent);
                for (int k = 0; k < 3; k++)
                {
                    float dx = x + k * .8f;
                    Box("Handle strap", dx, y + 3.35f, z + .4f, .035f, .68f, .035f, "Rubber", false, parent);
                    var h = Cylinder("Handle", new Vector3(dx, y + 2.94f, z + .4f), new Vector3(.21f, .025f, .21f), "Gold", parent);
                    h.transform.rotation = Quaternion.Euler(90, 0, 0);
                }
            }

            foreach (float dx in new[]
            {
                -length * .34f,
                length * .34f
            }

            )
                foreach (float side in new[]
                {
                    -1.8f,
                    1.8f
                }

                )
                {
                    var wheel = Cylinder("Bogie wheel", new Vector3(center + dx, y - .8f, z + side), new Vector3(1.05f, .21f, 1.05f), "Rubber", parent);
                    wheel.transform.rotation = Quaternion.Euler(90, 0, 0);
                }

            Box("Near camera edge", center, y + .02f, z - 2.15f, length, .12f, .16f, "Gold", false, parent);
        }

        static void Carriage()
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 10.5f + i * 20;
                TrainCar(x, 0, 0, 19.2f, world);
                Sign("04   NIGHT LINE   /   " + (i + 1) + "号車", new Vector3(x, 3.38f, 2.06f), 6, .45f, "Amber");
                LightAt(new Vector3(x, 3, 0), new Color(.92f, .68f, .43f), 3.3f, 11);
                Box("Coupler floor", x + 10, -.12f, 0, 1.2f, .25f, 4.5f, "Rubber", true);
                for (int j = 0; j < 5; j++)
                    Box("Accordion seal", x + 9.8f + j * .12f, 1.8f, 2.3f, .065f, 3.6f, .3f, "Rubber");
            }

            Box("Glass divider", 28, 1.9f, 0, .12f, 3.8f, 4.4f, "Glass", true).AddComponent<BreakableGlass>();
            foreach (float x in new[]
            {
                10f,
                23f,
                36f,
                49f
            }

            )
                Anchor(x, 2.95f, -.25f);
            for (int i = 0; i < 10; i++)
                Enemy(i % 3 == 1 ? "gunner" : "blade", 12 + i * 4.4f, i % 2 == 0 ? -.6f : .7f, Mathf.Max(6, 7 + (i / 3) * 13), i % 3 == 1 ? 64 : 72);
            Terminal(InteractionKind.Release, "지붕 해치 잠금 해제", 48, 0, -.9f);
            Interact(InteractionKind.Memory, "객실 기록 읽기", new Vector3(31, 1.2f, 1.2f), "기억을 지우는 것은 치료가 아니다. 기업은 도시의 고통을 수익으로 바꿨다. 열차의 코어가 그 증거다.");
            Box("Memory cartridge", 31, .9f, 1.2f, .25f, .5f, .2f, "Amber");
            for (int i = 0; i < 8; i++)
                Box("Roof ladder rung", 58, .35f + i * .43f, 1.6f, 1.2f, .09f, .18f, "Chrome");
            Box("Ladder upright", 57.35f, 1.8f, 1.6f, .12f, 3.7f, .15f, "Chrome");
            Box("Ladder upright", 58.65f, 1.8f, 1.6f, .12f, 3.7f, .15f, "Chrome");
            Sign("E / ROOF ACCESS", new Vector3(58, 4.3f, 1.5f), 4, .55f);
            Interact(InteractionKind.Hatch, "사다리로 열차 지붕 진입", new Vector3(58, 1.35f, .9f));
            City(true);
        }

        static void Roof()
        {
            foreach (var section in new[]
            {
                new Vector2(0, 20),
                new Vector2(23, 43),
                new Vector2(46, 77)
            }

            )
            {
                float center = (section.x + section.y) / 2, length = section.y - section.x;
                Box("Roof collision", center, -.25f, 0, length, .5f, 4.8f, "Metal", true);
                Box("Roof centre panel", center, .025f, 0, length, .06f, 2.8f, "Tile");
                foreach (float side in new[]
                {
                    -2.15f,
                    2.15f
                }

                )
                    Box("Roof safety stripe", center, .075f, side, length, .035f, .13f, "Gold");
                Box("Car body visible", center, -1.8f, 0, length, 2.8f, 4.4f, "LightTile");
                for (float x = section.x + 1; x < section.y; x += 2)
                {
                    Box("Roof seam", x, .07f, 0, .035f, .02f, 4.5f, "Chrome");
                    Box("Side window", x, -1.5f, -2.23f, 1.6f, 1.4f, .04f, "Window");
                }
            }

            foreach (float x in new[]
            {
                14f,
                34f
            }

            )
            {
                Box("Rooftop AC unit", x, .45f, 1.25f, 2.2f, .8f, 1.05f, "DarkMetal");
                for (int i = 0; i < 9; i++)
                    Box("AC fin", x - .9f + i * .22f, .87f, 1.25f, .07f, .07f, .9f, "Chrome");
            }

            Anchor(18, 7, -.1f);
            Anchor(27, 8, -.1f);
            Anchor(40, 7.5f, -.1f);
            Anchor(49, 8, -.1f);
            Anchor(52, 8, 0, 0);
            Anchor(68, 8, 0, 1);
            for (int i = 0; i < 6; i++)
                Enemy(i % 3 == 1 ? "gunner" : "stalker", 10 + i * 6.1f, i % 2 == 0 ? -.6f : .65f, Mathf.Max(5, 7 + (i / 2) * 10), i % 3 == 1 ? 64 : 60);
            Enemy("boss", 61, 0, 46, 1080, true);
            foreach (float x in new[]
            {
                52f,
                68f
            }

            )
            {
                Box("Capacitor pedestal", x, .5f, 1.4f, 1, .9f, 1, "Metal");
                Cylinder("Capacitor cell", new Vector3(x, 1.4f, 1.4f), new Vector3(.6f, .6f, .6f), "Cyan");
                Box("Capacitor mast", x, 4.5f, 2.2f, .17f, 8.7f, .17f, "Chrome");
                LightAt(new Vector3(x, 3.5f, 0), SignalEffects.Cyan, 4, 10);
            }

            Sign("CONDUCTOR / 코어 장갑", new Vector3(61, 5.8f, 3), 8, .7f, "Amber");
            Terminal(InteractionKind.Core, "기억 코어 회수", 73, 0, 0);
            City(false);
            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            moon.transform.SetParent(world);
            moon.transform.position = new Vector3(51, 29, 95);
            moon.transform.localScale = Vector3.one * 6;
            moon.GetComponent<Renderer>().sharedMaterial = Mat("Amber");
            Object.DestroyImmediate(moon.GetComponent<Collider>());
        }

        static void City(bool interior)
        {
            var random = new System.Random(214);
            for (int layer = 0; layer < 3; layer++)
                for (int i = 0; i < 14; i++)
                {
                    float x = -35 + i * 12.5f, z = 14 + layer * 24;
                    float height = 8 + (float)random.NextDouble() * 24;
                    var block = new GameObject("Moving skyline / layer " + layer);
                    block.transform.SetParent(world);
                    block.transform.position = new Vector3(x, 0, z);
                    Box("Tower volume", x, height / 2 - 7, z, 7.5f, height, 6, "Building", false, block.transform);
                    Box("Tower setback", x, height - 5.6f, z, 5.6f, 2.8f, 4.8f, "Metal", false, block.transform);
                    Box("Roof trim", x, height - 4.1f, z, 5.8f, .14f, 4.9f, layer % 2 == 0 ? "Cyan" : "Pink", false, block.transform);
                    for (int row = 0; row < Mathf.Min(12, height / 1.5f); row++)
                        for (int col = 0; col < 4; col++)
                        {
                            if (random.NextDouble() < .36)
                                continue;
                            Box("Lit window", x - 2.9f + col * 1.8f, row * 1.5f - 5, z - 3.03f, .76f, .6f, .04f, (row + col) % 4 == 0 ? "Cyan" : "Amber", false, block.transform);
                        }

                    if (i % 4 == 0)
                        Sign(i % 3 == 0 ? "기억상회" : i % 3 == 1 ? "夜市" : "AFTERLIGHT", new Vector3(x, height * .5f, z - 3.1f), 6, 1.6f, i % 2 == 0 ? "Cyan" : "Amber", block.transform);
                    var motion = block.AddComponent<RailMotion>();
                    motion.parallax = layer == 0 ? .7f : layer == 1 ? .28f : .1f;
                    motion.span = 175;
                }

            for (int i = 0; i < 15; i++)
            {
                float x = -22 + i * 10;
                var support = new GameObject("Viaduct support / moving");
                support.transform.SetParent(world);
                support.transform.position = new Vector3(x, 0, 8);
                Box("Viaduct pillar", x, -7, 7.8f, .8f, 15, .8f, "Concrete", false, support.transform);
                Box("Viaduct lamp", x, 3, 7.8f, .2f, 5, .2f, "Metal", false, support.transform);
                Box("Viaduct lamp head", x, 5.4f, 6.8f, .22f, .16f, 2.2f, "Cyan", false, support.transform);
                var m = support.AddComponent<RailMotion>();
                m.parallax = 1;
                m.span = 150;
            }

            Box("Night haze horizon", 40, -9, 65, 240, .4f, 100, "DarkMetal");
        }

        static void Haven()
        {
            Box("Plaza", 22, -.3f, 0, 46, .6f, 13, "Tile", true);
            Box("Raised promenade", 30, 2.5f, 4, 18, 5, 3, "Concrete", true);
            var ramp = Box("Promenade ramp", 17, 2.25f, 4, 12, .4f, 3, "Tile", true);
            ramp.transform.rotation = Quaternion.Euler(0, 0, 24.6f);
            for (int i = 0; i < 6; i++)
            {
                float x = 3 + i * 8;
                Box("Townhouse", x, 6.5f, 9, 7, 13, 5, "Building");
                Box("Storefront frame", x, 2.2f, 6.35f, 6.6f, 4.4f, .4f, "Metal");
                for (int j = 0; j < 3; j++)
                {
                    Box("Shop glazing", x - 2 + j * 2, 2.25f, 6.09f, 1.7f, 3.1f, .06f, "Window");
                    Box("Shop interior glow", x - 2 + j * 2, 2.9f, 6.04f, 1.3f, .08f, .03f, i % 2 == 0 ? "Amber" : "Cyan");
                }

                Sign(new[] { "기억상회", "AFTERLIGHT", "노아의 작업실", "밤의 식탁", "月光 MARKET", "기억을 잇다" }[i], new Vector3(x, 5.3f, 6), 6.7f, 1.1f, i % 2 == 0 ? "Cyan" : "Amber");
                Box("Awning", x, 4.45f, 5.9f, 6.8f, .17f, 1.1f, "Gold");
                LightAt(new Vector3(x, 3.6f, 2), i % 2 == 0 ? SignalEffects.Cyan : SignalEffects.Gold, 4, 10);
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 3; col++)
                        Box("Apartment window", x - 2 + col * 2, 7 + row * 1.8f, 6.44f, 1.1f, 1.2f, .04f, row % 2 == 0 ? "Amber" : "Window");
            }

            Npc("NOA / 노아", "noa", new Vector3(10, .05f, .5f), "돌아왔구나, 서하. 열차에 갇혔던 기억들이 도시로 돌아오고 있어. 오늘은 네가 이 도시의 신호야.");
            Npc("MIN / 민", "min", new Vector3(19, .05f, 1), "아까 창밖으로 열차 지붕 위의 빛을 봤어. 설마 그게 너였어? 뜨거운 식사 한 그릇은 내가 살게.");
            Npc("YUN / 윤", "yun", new Vector3(32, 5.05f, 4), "위에서 보면 이 도시도 숨을 쉬는 게 보여. 언젠가 다음 노선도 함께 조사하자.");
            Terminal(InteractionKind.Return, "새 캠페인 시작", 40, 0, -1);
            Bench(24, 0, -2.5f);
            Bench(6, 0, 2.8f);
            Anchor(21, 8, 2);
            Anchor(31, 10, 3.5f);
            for (int i = 0; i < 5; i++)
            {
                float x = 4 + i * 8;
                Cylinder("Planter", new Vector3(x, .55f, -4), new Vector3(1.1f, .55f, 1.1f), "Metal");
                for (int j = 0; j < 4; j++)
                {
                    var leaf = Box("Plant", x + (j - 1.5f) * .2f, 1.35f, -4, .14f, 1.1f, .5f, "Leaf");
                    leaf.transform.rotation = Quaternion.Euler(j * 12, 30 * j, j * 9);
                }
            }

            Sign("AFTERLIGHT / 돌아온 기억", new Vector3(23, 11, 6), 16, 1.5f);
        }
    }
}
