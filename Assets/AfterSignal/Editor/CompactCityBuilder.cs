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
        static readonly string[] Sources={"AfterlightExpansion","MobilityDistricts","CivicRenewal","NeonHarbor"};
        sealed class Move {public Transform root;public Bounds oldBounds;public Vector3 offset;public string source;}
        static readonly List<Move> moves=new();
        static readonly List<Rect> occupied=new();
        static readonly List<GameObject> loaded=new();
        static bool Inside(Vector3 p,Rect r)=>r.Contains(new Vector2(p.x,p.z));
        static Rect Rectangle(Bounds b,float margin=0)=>new(b.min.x-margin,b.min.z-margin,b.size.x+margin*2,b.size.z+margin*2);
        static Bounds BoundsOf(Transform t){var b=new Bounds(t.position,Vector3.zero);foreach(var c in t.GetComponentsInChildren<Collider>())b.Encapsulate(c.bounds);foreach(var r in t.GetComponentsInChildren<Renderer>())b.Encapsulate(r.bounds);return b;}
        static bool Free(Vector3 p,float w,float d)
        {
            var r=new Rect(p.x-w*.5f-3,p.z-d*.5f-3,w+6,d+6);
            bool land=p.z< -2300?NeonHarbor.Land.Any(l=>l.Contains(r.min)&&l.Contains(r.max)):r.xMin>25&&r.xMax<2170&&r.yMin> -440&&r.yMax<1080&&!(r.xMin<800&&r.yMin<330&&r.yMax> -325);
            if(!land||CompactCityLayout.Occupied(p,w+8,d+8)||occupied.Any(o=>o.Overlaps(r)))return false;
            foreach(var line in ExpansionRoads.Roads)for(int i=1;i<line.Length;i++)
            {var q=FourCityCatalog.Closest(p,line[i-1],line[i]);if(Mathf.Abs(q.x-p.x)<w*.5f+19&&Mathf.Abs(q.z-p.z)<d*.5f+19)return false;}
            return true;
        }
        static Vector3 FindLot(Move move)
        {
            var from=move.oldBounds.center;var options=new List<Vector3>();
            for(float z=from.z< -2300?-4130:-420;z<(from.z< -2300?-2430:1060);z+=18)
            for(float x=40;x<2170;x+=18)options.Add(new(x,move.root.position.y,z));
            options.Sort((a,b)=>(a-from).sqrMagnitude.CompareTo((b-from).sqrMagnitude));
            foreach(var p in options)if(Free(p,move.oldBounds.size.x,move.oldBounds.size.z))return p;
            throw new InvalidOperationException("No existing-city relocation lot for "+move.root.name);
        }
        public static void PrepareAndBuild(){Prepare();ProjectBuilder.BuildRelease();}
        public static void Prepare()
        {
            _=FourCityCatalog.Venues;Directory.CreateDirectory("Artifacts/CompactCities");
            string stamp="Documentation/CompactCities/prefab-layout-v1.json";
            if(File.Exists(stamp)){Debug.Log("Compact infill prefabs already prepared");return;}
            moves.Clear();occupied.Clear();loaded.Clear();
            // Keep transport campuses and original civic landmarks fixed.
            foreach(var r in new[]{new Rect(0,-325,790,650),new Rect(160,710,520,270),new Rect(1060,730,235,200),new Rect(1300,540,870,410),new Rect(1310,-630,820,235),new Rect(775,830,140,85),new Rect(808,583,105,85),new Rect(838,45,90,75),new Rect(937,-238,119,101),new Rect(1770,-3950,105,88)})occupied.Add(r);
            foreach(var source in Sources)
            {
                var root=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/"+source));root.name=source;loaded.Add(root);Physics.SyncTransforms();
                foreach(var t in root.GetComponentsInChildren<Transform>())
                {
                    bool tower=t.GetComponent<CollapsibleBuilding>();bool small=t.name.Contains("walk-in multistorey")&&!t.name.StartsWith("Inhabited identity");
                    if(!tower&&!small)continue;
                    var b=BoundsOf(t);if(b.size.x<5)continue;
                    if(CompactCityLayout.Occupied(b.center,b.size.x,b.size.z))moves.Add(new Move{root=t,oldBounds=b,source=source});
                    else occupied.Add(Rectangle(b,2));
                }
            }
            foreach(var m in moves)
            {
                var lot=FindLot(m);m.offset=new Vector3(lot.x-m.oldBounds.center.x,0,lot.z-m.oldBounds.center.z);m.root.position+=m.offset;
                var moved=m.oldBounds;moved.center+=m.offset;occupied.Add(Rectangle(moved,2));
                var collapse=m.root.GetComponent<CollapsibleBuilding>();if(collapse)collapse.worldBounds=moved;
            }
            Physics.SyncTransforms();int changed=0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var root in loaded)
                {
                    // Civic furnishing layers are stored separately from their building shells.
                    foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Inhabited identity"))
                    {var m=moves.FirstOrDefault(m=>Rectangle(m.oldBounds,3).Contains(new Vector2(t.position.x,t.position.z)));if(m!=null)t.position+=m.offset;}
                    foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                    {
                        if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable||filter.GetComponentInParent<CollapsibleBuilding>())continue;
                        if(moves.Any(m=>m.root&&filter.transform.IsChildOf(m.root)))continue;
                        if(!filter.name.StartsWith("Batch "))continue;
                        changed+=Rewrite(filter,root.name);
                    }
                    foreach(var c in root.GetComponentsInChildren<Collider>())
                    {
                        if(!c||c.bounds.max.y<.3f||c.bounds.size.y<.25f||moves.Any(m=>m.root&&c.transform.IsChildOf(m.root)))continue;
                        if(CompactCityLayout.Occupied(c.bounds.center)&&c.bounds.size.x<100&&c.bounds.size.z<100)Object.DestroyImmediate(c);
                    }
                    foreach(var r in root.GetComponentsInChildren<Renderer>())
                    {
                        if(!r||r.name.StartsWith("Batch ")||moves.Any(m=>m.root&&r.transform.IsChildOf(m.root)))continue;
                        if(r.bounds.max.y>.3f&&r.bounds.size.x<90&&r.bounds.size.z<90&&CompactCityLayout.Occupied(r.bounds.center))r.enabled=false;
                    }
                    foreach(var seed in root.GetComponentsInChildren<FacilityCrowd>())
                    {
                        if(moves.Any(m=>m.root&&seed.transform.IsChildOf(m.root)))continue;
                        var m=moves.FirstOrDefault(m=>Rectangle(m.oldBounds,4).Contains(new Vector2(seed.transform.position.x,seed.transform.position.z)));if(m!=null)seed.transform.position+=m.offset;
                    }
                    foreach(var lod in root.GetComponentsInChildren<LODGroup>())lod.RecalculateBounds();
                    PrefabUtility.SaveAsPrefabAsset(root,"Assets/AfterSignal/Resources/WorldAssets/"+root.name+".prefab");
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            Directory.CreateDirectory("Documentation/CompactCities");
            var records=new SurveyData();foreach(var m in moves)records.buildings.Add(new Footprint{name=m.root.name,source=m.source,center=m.oldBounds.center+m.offset,size=m.oldBounds.size,movable=true});
            File.WriteAllText(stamp,JsonUtility.ToJson(records,true));AssetDatabase.SaveAssets();
            foreach(var root in loaded)Object.DestroyImmediate(root);loaded.Clear();
            Debug.Log("COMPACT CITY: relocated "+moves.Count+" buildings; rewrote "+changed+" presentation batches. No city land added.");
        }
        struct Vertex {public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new(){p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t),uv=Vector2.Lerp(a.uv,b.uv,t)};}
        static List<Vertex> Clip(List<Vertex> polygon,int axis,float edge,bool positive)
        {
            var result=new List<Vertex>();if(polygon.Count==0)return result;
            float D(Vertex v)=>(axis==0?v.p.x:v.p.z)-edge;
            var a=polygon[^1];float da=D(a);bool ia=positive?da>=0:da<=0;
            foreach(var b in polygon){float db=D(b);bool ib=positive?db>=0:db<=0;if(ia!=ib)result.Add(Vertex.Lerp(a,b,da/(da-db)));if(ib)result.Add(b);a=b;da=db;ia=ib;}return result;
        }
        static List<List<Vertex>> Subtract(List<Vertex> polygon,Rect rect)
        {
            var output=new List<List<Vertex>>();var inside=polygon;
            foreach(var edge in new[]{(0,rect.xMin,true),(0,rect.xMax,false),(1,rect.yMin,true),(1,rect.yMax,false)})
            {var outside=Clip(inside,edge.Item1,edge.Item2,!edge.Item3);if(outside.Count>2)output.Add(outside);inside=Clip(inside,edge.Item1,edge.Item2,edge.Item3);if(inside.Count<3)break;}
            return output;
        }
        static int Rewrite(MeshFilter filter,string source)
        {
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;var uvs=mesh.uv;var indices=mesh.triangles;
            var renderer=filter.GetComponent<Renderer>();if(!renderer)return 0;var bounds=renderer.bounds;
            if(!CompactCityLayout.Reserved.Any(r=>r.Overlaps(Rectangle(bounds)))&&!moves.Any(m=>Rectangle(m.oldBounds,3).Overlaps(Rectangle(bounds))))return 0;
            var outputV=new List<Vector3>();var outputN=new List<Vector3>();var outputUV=new List<Vector2>();var outputT=new List<int>();bool changed=false;
            void Emit(List<Vertex> poly){if(poly.Count<3)return;int first=outputV.Count;foreach(var v in poly){outputV.Add(filter.transform.InverseTransformPoint(v.p));outputN.Add(filter.transform.InverseTransformDirection(v.n));outputUV.Add(v.uv);}for(int j=1;j<poly.Count-1;j++)outputT.AddRange(new[]{first,first+j,first+j+1});}
            for(int i=0;i<indices.Length;i+=3)
            {
                var tri=new List<Vertex>(3);for(int j=0;j<3;j++){int k=indices[i+j];tri.Add(new Vertex{p=filter.transform.TransformPoint(vertices[k]),n=normals.Length>k?filter.transform.TransformDirection(normals[k]):Vector3.up,uv=uvs.Length>k?uvs[k]:Vector2.zero});}
                var center=(tri[0].p+tri[1].p+tri[2].p)/3;var b=new Bounds(tri[0].p,Vector3.zero);b.Encapsulate(tri[1].p);b.Encapsulate(tri[2].p);
                bool flat=b.size.y<.03f&&Mathf.Abs(center.y)<.3f;
                var move=flat?null:moves.FirstOrDefault(m=>Inside(center,Rectangle(m.oldBounds,2))&&center.y>=m.oldBounds.min.y-.2f&&center.y<=m.oldBounds.max.y+2&&b.size.x<m.oldBounds.size.x+8&&b.size.z<m.oldBounds.size.z+8);
                if(move!=null){for(int j=0;j<3;j++){var v=tri[j];v.p+=move.offset;tri[j]=v;}Emit(tri);changed=true;continue;}
                // Subtract parcel ground exactly, retaining every triangle outside the parcel boundary.
                if(flat)
                {
                    var pieces=new List<List<Vertex>>{tri};foreach(var reserved in CompactCityLayout.Reserved)
                    {var parcel=new Rect(reserved.x+5,reserved.y+5,reserved.width-10,reserved.height-10);if(!parcel.Overlaps(Rectangle(b)))continue;var next=new List<List<Vertex>>();foreach(var piece in pieces)next.AddRange(Subtract(piece,parcel));pieces=next;changed=true;}
                    foreach(var piece in pieces)Emit(piece);continue;
                }
                if(center.y>.15f&&CompactCityLayout.Occupied(center)&&b.size.x<100&&b.size.z<100){changed=true;continue;}
                Emit(tri);
            }
            if(!changed)return 0;
            var replacement=new Mesh{name="Compact infill / "+mesh.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};replacement.SetVertices(outputV);replacement.SetNormals(outputN);replacement.SetUVs(0,outputUV);replacement.SetTriangles(outputT,0);replacement.RecalculateBounds();
            string folder="Assets/AfterSignal/Resources/WorldAssets/CompactMeshes";Directory.CreateDirectory(folder);string file=folder+"/"+source+"-"+filter.name.Replace('/','_')+".asset";AssetDatabase.CreateAsset(replacement,file);filter.sharedMesh=replacement;var collider=filter.GetComponent<MeshCollider>();if(collider)collider.sharedMesh=replacement;return 1;
        }
        [Serializable] sealed class SurveyData { public List<Footprint> buildings=new(); public List<Road> roads=new(); }
        [Serializable] sealed class Footprint {public string name,source;public Vector3 center,size;public bool movable;}
        [Serializable] sealed class Road {public Vector3[] points;}
        public static void Survey()
        {
            var data=new SurveyData();
            foreach(var name in new[]{"AfterlightExpansion","MobilityDistricts","CivicRenewal","NeonHarbor"})
            {
                var p=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/"+name));Physics.SyncTransforms();
                foreach(var t in p.GetComponentsInChildren<Transform>())
                {
                    bool movable=t.GetComponent<CollapsibleBuilding>();
                    if(!movable&&!t.name.Contains("walk-in multistorey")&&!t.name.Contains("Airport")&&!t.name.Contains("Military")&&!t.name.Contains("Prison"))continue;
                    var renderers=t.GetComponentsInChildren<Renderer>();var colliders=t.GetComponentsInChildren<Collider>();
                    var bounds=new Bounds(t.position,Vector3.zero);foreach(var r in renderers)bounds.Encapsulate(r.bounds);foreach(var c in colliders)bounds.Encapsulate(c.bounds);
                    if(bounds.size.x<5)continue;data.buildings.Add(new Footprint{name=t.name,source=name,center=bounds.center,size=bounds.size,movable=movable});
                }
                Object.DestroyImmediate(p);
            }
            foreach(var line in ExpansionRoads.Roads)data.roads.Add(new Road{points=line});
            Directory.CreateDirectory("Artifacts/CompactCities");File.WriteAllText("Artifacts/CompactCities/survey.json",JsonUtility.ToJson(data));
        }
    }
}
