using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static Mesh VehicleHull(string name, Vector4[] rings)
        {
            string path = resourceRoot + "Geometry/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            var v = new List<Vector3>();
            var t = new List<int>();
            foreach (var r in rings)
            {
                v.Add(new Vector3(r.x, r.y, -r.w));
                v.Add(new Vector3(r.x, r.z, -r.w));
                v.Add(new Vector3(r.x, r.z, r.w));
                v.Add(new Vector3(r.x, r.y, r.w));
            }

            for (int j = 0; j < rings.Length - 1; j++)
                for (int k = 0; k < 4; k++)
                {
                    int a = j * 4 + k, b = j * 4 + (k + 1) % 4, c = b + 4, d = a + 4;
                    t.AddRange(new[] { a, b, c, c, d, a });
                }

            t.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
            int last = v.Count - 4;
            t.AddRange(new[] { last, last + 1, last + 2, last, last + 2, last + 3 });
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static void ParkedSedans()
        {
            var body = VehicleHull("CivicSedanBody", new[] { new Vector4(-2.7f, .42f, .6f, .84f), new Vector4(-2.35f, .32f, .88f, 1.09f), new Vector4(1.5f, .32f, .86f, 1.09f), new Vector4(2.7f, .42f, .66f, .87f) });
            var cabin = VehicleHull("CivicSedanCabin", new[] { new Vector4(-1.7f, .82f, .84f, .98f), new Vector4(-.95f, .82f, 1.55f, .85f), new Vector4(.75f, .82f, 1.55f, .85f), new Vector4(1.65f, .82f, .85f, .98f) });
            CreateMaterial("SedanIvory", "#9cabaf", .4f, .45f);
            CreateMaterial("SedanRed", "#742d45", .45f, .4f);
            int i = 0;
            foreach (float x in new[]
            {
                14f,
                78f,
                146f,
                225f
            }

            )
            {
                var root = new GameObject("Parked city sedan").transform;
                root.SetParent(world);
                root.position = new Vector3(x, .02f, -12.5f);
                foreach (bool glass in new[]
                {
                    false,
                    true
                }

                )
                {
                    var go = new GameObject(glass ? "Sculpted cabin" : "Sculpted chassis", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(root, false);
                    go.GetComponent<MeshFilter>().sharedMesh = glass ? cabin : body;
                    go.GetComponent<MeshRenderer>().sharedMaterial = Mat(glass ? "DistrictWindow" : i % 2 == 0 ? "SedanIvory" : "SedanRed");
                }

                Box("Sedan roof", x - .1f, 1.59f, -12.5f, 1.8f, .08f, 1.68f, i % 2 == 0 ? "SedanIvory" : "SedanRed", false, root);
                foreach (float side in new[]
                {
                    -1.12f,
                    1.12f
                }

                )
                {
                    foreach (float axle in new[]
                    {
                        -1.6f,
                        1.7f
                    }

                    )
                    {
                        var wheel = Cylinder("Wheel tire", new Vector3(x + axle, .43f, -12.5f + side), new Vector3(.8f, .13f, .8f), "Rubber", root);
                        wheel.transform.rotation = Quaternion.Euler(90, 0, 0);
                        var hub = Cylinder("Wheel alloy", new Vector3(x + axle, .43f, -12.5f + side * 1.17f), new Vector3(.48f, .035f, .48f), "Chrome", root);
                        hub.transform.rotation = Quaternion.Euler(90, 0, 0);
                    }

                    Box("Side sill trim", x, .45f, -12.5f + side, 3.6f, .065f, .06f, "Chrome", false, root);
                    Box("Door handle", x + .2f, .94f, -12.5f + side * .87f, .32f, .065f, .07f, "Chrome", false, root);
                }

                foreach (float z in new[]
                {
                    -.6f,
                    .6f
                }

                )
                {
                    Box("Headlamp", x + 2.61f, .63f, -12.5f + z, .08f, .15f, .35f, "DistrictLight", false, root);
                    Box("Tail lamp", x - 2.55f, .61f, -12.5f + z, .08f, .14f, .32f, "NeonRose", false, root);
                }

                Box("Parked body collision", x, .56f, -12.5f, 5.1f, .68f, 2, "DarkMetal", true, root).GetComponent<MeshRenderer>().enabled = false;
                i++;
            }
        }
    }
}
