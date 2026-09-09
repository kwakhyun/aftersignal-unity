using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal
{
    public static class HarborParcelRepair
    {
        public static int Relocated{get;private set;}
        static int Port(Bounds b)=>b.center.x>1250&&b.center.x<2150&&b.center.z> -650&&b.center.z< -390?1:b.center.x>740&&b.center.x<1080&&b.center.z> -2600&&b.center.z< -2360?2:0;
        static bool Suitable(Bounds b)=>Port(b)>0&&b.size.y>24&&b.size.x>7&&b.size.z>7&&b.size.x<90&&b.size.z<90;
        public static void Apply()
        {
            Relocated=0;var candidates=new List<(Transform root,Bounds bounds,HighriseBuilding building)>();var roots=new HashSet<Transform>();
            foreach(var building in HighriseBuilding.All)if(building&&Suitable(building.Bounds)){candidates.Add((building.transform,building.Bounds,building));roots.Add(building.transform);}
            foreach(var c in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                if(c&&!c.isTrigger&&Suitable(c.bounds)&&!c.GetComponentInParent<HighriseBuilding>()&&!c.GetComponentInParent<CityVehicle>()&&!c.GetComponentInParent<VenueRuntime>()&&roots.Add(c.transform))candidates.Add((c.transform,c.bounds,null));
            MeshFilter[] filters=null;int slot=0;
            foreach(var item in candidates)
            {
                var bounds=item.bounds;var building=item.building;bool old=Port(bounds)==1;
                Vector3 destination=default;bool found=false;
                for(int i=0;i<180;i++)
                {
                    var at=old?new Vector3(1100+(i%15)*73,bounds.min.y,-280+i/15*73):new Vector3(300+(i%18)*91,bounds.min.y,-2770-i/18*100);
                    if(Port(new Bounds(at,bounds.size))>0||!NpcGroundSupport.Floor(at,at.y+2,5,out var y))continue;at.y=y;
                    var check=new Bounds(at+Vector3.up*(bounds.size.y*.5f+1),bounds.size+new Vector3(6,-2,6));
                    if(Physics.CheckBox(check.center,check.extents,Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                    float clearance=Mathf.Max(bounds.extents.x,bounds.extents.z)+12; if((LocalCityRoutes.Sidewalk(at)-at).sqrMagnitude<clearance*clearance)continue;destination=at+Vector3.up*bounds.size.y*.5f;found=true;break;
                }
                if(!found)continue;var delta=destination-bounds.center;
                // Move dedicated models directly; restored batch cuts must not duplicate them.
                if(item.root.GetComponentsInChildren<MeshFilter>().Length>0){if(building)building.Relocate(delta);else item.root.position+=delta;Relocated++;Physics.SyncTransforms();continue;}
                float floorLevel=bounds.min.y;if(NpcGroundSupport.Floor(bounds.center,bounds.min.y+4,8,out var street))floorLevel=Mathf.Max(floorLevel,street);
                filters??=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);var root=new GameObject("Relocated harbor building / "+slot++);root.transform.position=destination;bool copied=false;
                foreach(var filter in filters)
                {
                    if(!filter||filter.GetComponentInParent<CityVehicle>())continue;var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;if(!renderer||!renderer.bounds.Intersects(bounds)||!mesh||!mesh.isReadable)continue;
                    var original=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var vertices=new List<Vector3>();var texture=new List<Vector2>();var normal=new List<Vector3>();var map=new Dictionary<int,int>();var triangles=new List<int[]>();bool any=false;
                    int Vertex(int index){if(map.TryGetValue(index,out int n))return n;n=vertices.Count;map[index]=n;vertices.Add(filter.transform.TransformPoint(original[index])-bounds.center);texture.Add(uv.Length>index?uv[index]:Vector2.zero);normal.Add(normals.Length>index?filter.transform.TransformDirection(normals[index]):Vector3.up);return n;}
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var src=mesh.GetTriangles(sub);var keep=new List<int>();for(int t=0;t<src.Length;t+=3){var p=filter.transform.TransformPoint((original[src[t]]+original[src[t+1]]+original[src[t+2]])/3);if(!bounds.Contains(p)||p.y<floorLevel+.3f)continue;keep.Add(Vertex(src[t]));keep.Add(Vertex(src[t+1]));keep.Add(Vertex(src[t+2]));any=true;}triangles.Add(keep.ToArray());
                    }
                    if(!any)continue;var part=new GameObject("Relocated facade",typeof(MeshFilter),typeof(MeshRenderer),typeof(RuntimeMeshOwner));part.transform.SetParent(root.transform,false);var copy=new Mesh{name="Harbor parcel facade",indexFormat=IndexFormat.UInt32};copy.SetVertices(vertices);copy.SetNormals(normal);copy.SetUVs(0,texture);copy.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)copy.SetTriangles(triangles[i],i);copy.RecalculateBounds();part.GetComponent<MeshFilter>().sharedMesh=copy;part.GetComponent<RuntimeMeshOwner>().mesh=copy;part.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;copied=true;
                }
                if(!copied){Object.Destroy(root);continue;}DistrictGeometryCut.Remove(bounds);if(building)building.Relocate(delta);else item.root.position+=delta;root.transform.SetParent(item.root,true);if(building)building.Relocate(Vector3.zero);else item.root.gameObject.AddComponent<HighriseBuilding>();Relocated++;Physics.SyncTransforms();
            }
        }
    }
}
