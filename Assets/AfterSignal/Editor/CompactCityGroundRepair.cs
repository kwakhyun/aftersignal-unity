using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static partial class CompactCityBuilder
    {
        [Serializable]sealed class BatchOrigins{public List<BatchOrigin> items;}
        [Serializable]sealed class BatchOrigin{public string source,name,guid;}
        public static void RepairGroundAndBuild(){RepairGround();ProjectBuilder.BuildRelease();}
        public static void RepairGround()
        {
            _=FourCityCatalog.Venues;
            var origins=JsonUtility.FromJson<BatchOrigins>(File.ReadAllText("Artifacts/CompactCities/original-batches.json"));int repaired=0;
            foreach(string source in Sources)
            {
                var root=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/"+source));
                foreach(var f in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(!f.sharedMesh||!AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/CompactMeshes/"))continue;
                    var origin=origins.items.FirstOrDefault(o=>o.source==source&&o.name==f.name);if(origin==null)throw new InvalidOperationException("Missing original batch "+source+" / "+f.name);
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(origin.guid));if(!original)throw new InvalidOperationException("Missing source mesh "+origin.guid);
                    var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int floors=0;
                    void Emit(List<Vertex> poly)
                    {
                        int start=vertices.Count;foreach(var v in poly){vertices.Add(f.transform.InverseTransformPoint(v.p));normals.Add(f.transform.InverseTransformDirection(v.n));uv.Add(v.uv);}
                        for(int n=1;n<poly.Count-1;n++)triangles.AddRange(new[]{start,start+n,start+n+1});
                    }
                    void Copy(Mesh mesh,bool groundOnly)
                    {
                        var v=mesh.vertices;var n=mesh.normals;var u=mesh.uv;var t=mesh.triangles;
                        for(int i=0;i<t.Length;i+=3)
                        {
                            var poly=new List<Vertex>(3);for(int j=0;j<3;j++){int k=t[i+j];poly.Add(new(){p=f.transform.TransformPoint(v[k]),n=f.transform.TransformDirection(n.Length>k?n[k]:Vector3.up),uv=u.Length>k?u[k]:Vector2.zero});}
                            var b=new Bounds(poly[0].p,Vector3.zero);b.Encapsulate(poly[1].p);b.Encapsulate(poly[2].p);
                            bool flat=b.size.y<.03f&&Mathf.Abs(b.center.y)<.3f;
                            if(flat!=groundOnly)continue;
                            if(!groundOnly){Emit(poly);continue;}
                            floors++;var pieces=new List<List<Vertex>>{poly};
                            foreach(var r in CompactCityLayout.Reserved)
                            {
                                var parcel=new Rect(r.x+5,r.y+5,r.width-10,r.height-10);if(!parcel.Overlaps(Rectangle(b)))continue;
                                var next=new List<List<Vertex>>();foreach(var p in pieces)next.AddRange(Subtract(p,parcel));pieces=next;
                            }
                            foreach(var p in pieces)Emit(p);
                        }
                    }
                    Copy(f.sharedMesh,false);Copy(original,true);if(floors==0)continue;
                    var target=f.sharedMesh;target.Clear();target.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;target.SetVertices(vertices);target.SetNormals(normals);target.SetUVs(0,uv);target.SetTriangles(triangles,0);target.RecalculateBounds();EditorUtility.SetDirty(target);repaired++;
                }
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("Documentation/CompactCities/ground-repair.json","{\"groundBatchesRestored\":"+repaired+",\"reason\":\"Ground must stay in place when its overlying building moves\"}");
            Debug.Log("COMPACT ground repair / "+repaired+" batches restored outside redeveloped parcels");
        }
    }
}
