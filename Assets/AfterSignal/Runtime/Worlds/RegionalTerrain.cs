using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class RegionalTerrain
    {
        public static void IslandShore(CityGeometry g,int seed)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3 P(float a,float scale,float y){float r=1+.045f*Mathf.Sin(a*5+seed)+.04f*Mathf.Cos(a*9);return new Vector3(Mathf.Cos(a)*210*scale*r,y,Mathf.Sin(a)*165*scale*r);}
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,string material){g.Quad(a,b,c,d,material);int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                Face(Vector3.zero,P(b,.85f,0),P(a,.85f,0),Vector3.zero,seed==1?"Concrete":"NovaConcrete");
                Face(P(a,.85f,0),P(b,.85f,0),P(b,1,-1.4f),P(a,1,-1.4f),"IslandSand");
                Face(P(a,1,-1.4f),P(b,1,-1.4f),P(b,1.07f,-12),P(a,1.07f,-12),"NovaObsidian");
                if(i%3==0){var p=P(a,.92f,-.9f);g.Dome(p,new Vector3(4+i%5,2+i%3,3+i%4),"Concrete",8,3,false);}
            }
            var collider=new GameObject("Island continuous irregular shoreline");collider.transform.SetParent(g.root,false);var mesh=new Mesh{name="Island land and beach collision"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateNormals();collider.AddComponent<MeshCollider>().sharedMesh=mesh;collider.AddComponent<CityMeshOwner>().meshes.Add(mesh);
        }
        public static bool Foundation(Rect land,Transform root)
        {
            var center=RegionalCatalog.RiftCenter;if(!land.Contains(new Vector2(center.x,center.z)))return false;
            var g=new CityGeometry(root);const float outer=248;
            void Slab(float x,float z,float w,float d){g.Box("Continuous land around collapse",new Vector3(x,-2,z)-root.position,new(w,4,d),"NovaConcrete",true);}
            Slab((land.xMin+center.x-outer)/2,land.center.y,center.x-outer-land.xMin,land.height);
            Slab((land.xMax+center.x+outer)/2,land.center.y,land.xMax-center.x-outer,land.height);
            Slab(center.x,(land.yMin+center.z-outer)/2,outer*2,center.z-outer-land.yMin);
            Slab(center.x,(land.yMax+center.z+outer)/2,outer*2,land.yMax-center.z-outer);
            var verts=new List<Vector3>();var tri=new List<int>();
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                Vector3 P(float angle,float radius)=>center-root.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                float ra=outer/Mathf.Max(Mathf.Abs(Mathf.Cos(a)),Mathf.Abs(Mathf.Sin(a))),rb=outer/Mathf.Max(Mathf.Abs(Mathf.Cos(b)),Mathf.Abs(Mathf.Sin(b)));
                var p=P(a,210);var q=P(b,210);var r=P(b,rb);var s=P(a,ra);g.Quad(p,q,r,s,"NovaConcrete");int start=verts.Count;verts.AddRange(new[]{p,q,r,s});tri.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            g.Finish();var mesh=new Mesh{name="Walkable sinkhole rim"};mesh.SetVertices(verts);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();root.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;root.GetComponent<CityMeshOwner>().meshes.Add(mesh);return true;
        }
        public static void Sinkhole(CityGeometry g,VenueRuntime venue)
        {
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                for(int level=0;level<6;level++)
                {
                    float upper=-level*23,lower=-(level+1)*23,r=210-level*18,nr=210-(level+1)*18;
                    Vector3 P(float t,float radius,float y)=>new(Mathf.Cos(t)*radius,y,Mathf.Sin(t)*radius);
                    g.Quad(P(a,r,upper),P(a,nr,lower),P(b,nr,lower),P(b,r,upper),level%2==0?"NovaObsidian":"Concrete",true);
                }
                if(i%3==0){var p=new Vector3(Mathf.Cos(a)*221,0,Mathf.Sin(a)*221);g.Box("Collapsed barrier",p+Vector3.up*.6f,new(7,1.2f,1.1f),"Concrete",true,Quaternion.Euler(0,-a*Mathf.Rad2Deg,0));}
            }
            g.Box("Rift bottom",new(0,-140,0),new(260,2,260),"NovaObsidian",true);
            g.Ring(new(0,-131,0),75,75,.6f,"NovaNeonViolet",96);
            for(int i=0;i<34;i++)
            {
                var debris=new GameObject("Suspended collapse fragment").transform;debris.SetParent(venue.transform,false);float a=i*2.39996f;debris.localPosition=new(Mathf.Cos(a)*(65+i%5*24),8+i%7*9,Mathf.Sin(a)*(65+i%5*24));var d=new CityGeometry(debris);d.Box("Broken reinforced slab",Vector3.zero,new(10+i%5*4,1.4f,8+i%4*3),"NovaConcrete");for(int bar=0;bar<5;bar++)d.Beam(new(-8,0,bar-2),new(8+i%5*3,2,bar-2),.08f,"Steel");d.Finish();debris.gameObject.AddComponent<FloatingRemnant>().phase=i;
            }
            venue.viewPoint=new(0,.12f,-230);venue.lookPoint=new(0,-55,0);
            VenueService.Add(venue.transform,new(0,1,-231),venue.Index,"붕괴구 관측 단말");
        }
    }
}
