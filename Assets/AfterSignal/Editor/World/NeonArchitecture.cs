using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void NeonArchitecture(StageId stage, float length)
        {
            for (int i = world.childCount - 1; i >= 0; i--)
                if (world.GetChild(i).name == "NEON / district retrofit")
                    Object.DestroyImmediate(world.GetChild(i).gameObject);
            CreateMaterial("NeonAzure", "#35cfea", 0, .22f, 3);
            CreateMaterial("NeonRose", "#ed4dbe", 0, .22f, 2.5f);
            CreateMaterial("NeonLime", "#a4e966", 0, .22f, 2);
            CreateMaterial("NeonHoney", "#efb452", 0, .22f, 2.5f);
            var root = new GameObject("NEON / district retrofit").transform;
            root.SetParent(world, false);
            if (CivicWorld.Interior(stage))
            {
                if (stage == StageId.Residence)
                {
                    Box("Home signal display", 14, 24.35f, 5.45f, 2.7f, .07f, .04f, "NeonAzure", false, root);
                    return;
                }

                foreach (float x in new[]
                {
                    7f,
                    29f,
                    49f
                }

                )
                {
                    Box("Facility circuit channel", x, 5.8f, 9.35f, 10, .06f, .08f, "NeonAzure", false, root);
                    Sign(stage == StageId.Clinic ? "온유 / CARE" : stage == StageId.School ? "MEMORY / SCHOOL" : "AFTER / SIGNAL", new Vector3(x, 4.9f, 9.25f), 7, .8f, "Cyan", root);
                }

                return;
            }

            bool town = stage == StageId.Haven;
            float baseY = CampaignCatalog.Get(stage)?.EndHeight ?? 0;
            string[] titles =
            {
                "기억\n시장",
                "N\nE\nO\nN",
                "온\n유",
                "S\nI\nG\nN\nA\nL",
                "夜\n行",
                "CITY\nLINK"
            };
            string[] colors =
            {
                "NeonAzure",
                "NeonRose",
                "NeonLime",
                "NeonHoney"
            };
            if (stage == StageId.Breach || stage == StageId.Lab || stage == StageId.Archive || stage == StageId.Foundry || stage == StageId.Origin || stage == StageId.Tower)
                titles = new[]
                {
                    "기억\n보관",
                    "C\nO\nR\nE",
                    "관\n측",
                    "S\nI\nG\nN\nA\nL",
                    "記\n録",
                    "DATA\nLINK"
                };
            int count = town ? 18 : Mathf.Clamp(Mathf.CeilToInt(length / 17), 3, 10);
            for (int i = 0; i < count; i++)
            {
                float x = town ? -1 + i * 13.5f : 7 + i * 16;
                float z = town ? 18.2f : 5.7f;
                float y = town ? 8 + i % 3 * 2.6f : baseY + 7 + i % 3 * 2.2f;
                if (stage == StageId.Breach)
                {
                    y = Mathf.Min(34, 6 + x * .22f);
                    z = 5;
                }

                float w = 9.5f, h = 1.7f;
                Box("Cantilever sign frame", x, y, z, w + .3f, h + .3f, .45f, "DarkMetal", false, root);
                Box("Luminous sign rim", x, y - h * .5f, z - .26f, w, .06f, .08f, colors[i % 4], false, root);
                UrbanPlate(town ? new[] { 7, 15, 9, 8, 0, 14 }[i % 6] : new[] { 13, 14, 10, 3 }[i % 4], new Vector3(x, y, z - .29f), w, h, root);
                Box("Sign attachment arm", x, y + 2, z + 1, .14f, .15f, 2.3f, "Metal", false, root);
                for (int k = 0; k < 3; k++)
                    Box("Facade luminous window ribbon", x + 4, y - 2.1f + k * .22f, z + .35f, 4.7f, .085f, .055f, i % 3 == 0 ? "NeonHoney" : "NeonAzure", false, root);
                if (i % 3 == 0)
                {
                    LightAt(new Vector3(x, y - 2, z - 4), i % 2 == 0 ? new Color(.16f, .67f, 1) : new Color(1, .22f, .48f), 32, 15);
                    world.GetChild(world.childCount - 1).SetParent(root, true);
                }
            }

            if (town)
            {
                ParkedSedans();
                Box("River quay curb", 111, .4f, -28.3f, 244, .8f, .35f, "CivicStone", true, root);
                Box("Waterfront rail", 111, 1.25f, -28.3f, 244, .08f, .09f, "Chrome", false, root);
                for (int i = 0; i < 41; i++)
                    Box("Waterfront railing upright", -9 + i * 6, 1, -28.3f, .09f, .8f, .09f, "Chrome", false, root);
                foreach (float x in new[]
                {
                    42f,
                    94f,
                    138f,
                    204f
                }

                )
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var beam = Box("Overhead cable", x, 12 + i * .55f, 25, 40, .055f, .055f, "DarkMetal", false, root);
                        beam.transform.rotation = Quaternion.Euler(0, 90, -3 + i * 3);
                    }

                    Box("Street crossing glow", x, .15f, 5, 6, .035f, .12f, "NeonAzure", false, root);
                }
            }
        }
    }
}
