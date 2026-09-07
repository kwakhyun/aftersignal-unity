using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static CityVehicle[] MakeVehicleVariants(CityVehicle sedan)
        {
            CreateMaterial("TaxiPaint", "#daa532", .35f, .32f);
            CreateMaterial("BusPaint", "#286878", .25f, .3f);
            CreateMaterial("TruckPaint", "#c3b6a1", .2f, .25f);
            var fleet = new CityVehicle[4];
            fleet[0] = sedan;
            for (int kind = 1; kind < 4; kind++)
            {
                var root = new GameObject("City " + (CityVehicleType)kind).transform;
                root.SetParent(world);
                if (kind == 1)
                {
                    var clone = (GameObject)PrefabUtility.InstantiatePrefab(sedan.gameObject);
                    PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    clone.transform.SetParent(root, false);
                    clone.transform.localPosition = Vector3.zero;
                    Object.DestroyImmediate(clone.GetComponent<CityVehicle>());
                    Object.DestroyImmediate(clone.GetComponent<Rigidbody>());
                    foreach (var r in clone.GetComponentsInChildren<MeshRenderer>())
                        if (r.name == "Sculpted chassis" || r.name == "Sedan roof")
                            r.sharedMaterial = Mat("TaxiPaint");
                    var children = new List<Transform>();
                    foreach (Transform t in clone.transform)
                        children.Add(t);
                    foreach (var t in children)
                        t.SetParent(root, true);
                    Object.DestroyImmediate(clone);
                    Box("Taxi roof light", 0, 1.78f, 0, .8f, .28f, .45f, "DistrictLight", false, root);
                    Box("Taxi inset", 0, 1.78f, -.23f, .68f, .18f, .02f, "DarkMetal", false, root);
                    foreach (float z in new[]
                    {
                        -1.095f,
                        1.095f
                    }

                    )
                        for (int x = 0; x < 10; x++)
                            Box("Taxi checker", -.9f + x * .19f, .66f, z, .1f, .1f, .02f, x % 2 == 0 ? "DarkMetal" : "DistrictIvory", false, root);
                }
                else
                {
                    bool bus = kind == 2;
                    float length = bus ? 4.6f : 3.9f;
                    string paint = bus ? "BusPaint" : "TruckPaint";
                    var hull = VehicleHull(bus ? "CityBusBody" : "CityTruckBody", new[] { new Vector4(-length, .35f, .65f, 1.1f), new Vector4(-length + .25f, .35f, 1.05f, 1.25f), new Vector4(length - .3f, .35f, 1.05f, 1.25f), new Vector4(length, .45f, .9f, 1.05f) });
                    var go = new GameObject("Sculpted chassis", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(root, false);
                    go.GetComponent<MeshFilter>().sharedMesh = hull;
                    go.GetComponent<MeshRenderer>().sharedMaterial = Mat(paint);
                    if (bus)
                    {
                        Box("Bus saloon", 0, 1.95f, 0, 8.9f, 2.2f, 2.4f, paint, false, root);
                        Box("Bus roof", 0, 3.15f, 0, 9, .2f, 2.5f, "DistrictIvory", false, root);
                        foreach (float z in new[]
                        {
                            -1.22f,
                            1.22f
                        }

                        )
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                Box("Passenger window", -3.7f + i * 1.04f, 2.2f, z, .88f, 1.05f, .04f, "DistrictWindow", false, root);
                                Box("Window sill", -3.7f + i * 1.04f, 1.64f, z, .9f, .06f, .05f, "VehicleAlloy", false, root);
                            }

                            Box("Bus route ribbon", 0, 1.2f, z, 8.6f, .2f, .04f, "DistrictIvory", false, root);
                        }

                        Box("Bus windshield", 4.51f, 2.13f, 0, .04f, 1.25f, 2.15f, "DistrictWindow", false, root);
                        Box("Destination display", 4.54f, 2.93f, 0, .04f, .26f, 1.75f, "Amber", false, root);
                        for (int i = 0; i < 2; i++)
                            Box("Folding passenger door", 3.3f + i * .45f, 1.55f, -1.26f, .4f, 1.95f, .04f, "DarkMetal", false, root);
                        Box("Roof air conditioner", -1.5f, 3.4f, 0, 2, .4f, 1.8f, "Metal", false, root);
                    }
                    else
                    {
                        Box("Cargo shell", -1, 2.12f, 0, 5.4f, 2.35f, 2.5f, "DistrictIvory", false, root);
                        Box("Truck cab", 2.6f, 1.75f, 0, 2.1f, 1.6f, 2.35f, paint, false, root);
                        Box("Cab windshield", 3.67f, 2.15f, 0, .04f, .65f, 2.03f, "DistrictWindow", false, root);
                        foreach (float z in new[]
                        {
                            -1.265f,
                            1.265f
                        }

                        )
                        {
                            for (int i = 0; i < 15; i++)
                                Box("Cargo corrugation", -3.5f + i * .34f, 2.12f, z, .04f, 2.2f, .03f, "VehicleAlloy", false, root);
                            Box("Cab door window", 2.7f, 2.15f, z * .94f, 1.4f, .7f, .04f, "DistrictWindow", false, root);
                        }

                        foreach (float z in new[]
                        {
                            -.62f,
                            .62f
                        }

                        )
                            Box("Cargo rear door", -3.73f, 2.1f, z, .03f, 2.2f, 1.2f, "TruckPaint", false, root);
                    }

                    foreach (float z in new[]
                    {
                        -1.28f,
                        1.28f
                    }

                    )
                        foreach (float x in new[]
                        {
                            -length + 1,
                            -length + 2.1f,
                            length - 1
                        }

                        )
                        {
                            var tire = Cylinder("Wheel tire", new Vector3(x, .53f, z), new Vector3(1, .2f, 1), "Rubber", root);
                            tire.transform.rotation = Quaternion.Euler(90, 0, 0);
                            var alloy = Cylinder("Wheel alloy", new Vector3(x, .53f, z * 1.22f), new Vector3(.58f, .035f, .58f), "VehicleAlloy", root);
                            alloy.transform.rotation = Quaternion.Euler(90, 0, 0);
                        }

                    foreach (float z in new[]
                    {
                        -.85f,
                        .85f
                    }

                    )
                    {
                        Box("Headlamp", length + .02f, .8f, z, .08f, .2f, .35f, "DistrictLight", false, root);
                        Box("Tail lamp", -length - .02f, .8f, z, .08f, .2f, .3f, "NeonRose", false, root);
                    }

                    var collider = root.gameObject.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0, 1.45f, 0);
                    collider.size = new Vector3(length * 2, 2.5f, 2.5f);
                }

                var vehicle = root.gameObject.AddComponent<CityVehicle>();
                vehicle.type = (CityVehicleType)kind;
                var rb = root.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                fleet[kind] = PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/AfterSignal/Prefabs/City" + (CityVehicleType)kind + ".prefab").GetComponent<CityVehicle>();
                Object.DestroyImmediate(root.gameObject);
            }

            return fleet;
        }
    }
}
