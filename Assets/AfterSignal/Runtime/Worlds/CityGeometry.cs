using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal
{
    // Authored parametric architecture: one mesh batch per material, independent walkable collision.
    public sealed class CityGeometry
    {
        sealed class Batch {public readonly List<Vector3> v=new(),n=new();public readonly List<Vector2> uv=new();public readonly List<int> t=new();}
        readonly Dictionary<string,Batch> batches=new();
        readonly List<Vector3> solidVertices=new();readonly List<int> solidTriangles=new();
        public readonly Transform root;
        static readonly Dictionary<string,Material> materials=new();
        static Material depthText;
        public CityGeometry(Transform root){this.root=root;}
        public static Material Material(string key)
        {
            if(materials.TryGetValue(key,out var mat)&&mat)return mat;
            if(key=="Glass"||key=="PressureGlass")
            {
                mat=new Material(Resources.Load<Shader>("Shaders/StructuralGlass")){name=key};
                mat.SetColor("_BaseColor",key=="PressureGlass"?new Color(.025f,.14f,.19f,.08f):new Color(.10f,.28f,.32f,.12f));
                mat.renderQueue=3000;mat.enableInstancing=true;materials[key]=mat;return mat;
            }
            mat=Resources.Load<Material>("WorldAssets/Generated/"+key);
            if(!mat)mat=Resources.Load<Material>("Materials/"+key);
            if(!mat&&key=="DefenseDeck")
            {mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=key};mat.SetColor("_BaseColor",new Color(.38f,.42f,.44f));mat.SetFloat("_Smoothness",.16f);mat.enableInstancing=true;materials[key]=mat;return mat;}
            if(!mat&&key=="DeepDeck")
            {
                var template=Resources.Load<Material>("WorldAssets/Generated/Pavement");
                if(template){mat=new Material(template){name=key};mat.SetColor("_BaseColor",new Color(.29f,.40f,.43f));mat.SetFloat("_RainResponse",0);materials[key]=mat;return mat;}
            }
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=key;
                Color c=key=="IslandSand"?new(.68f,.58f,.39f):key=="SlumRust"?new(.38f,.19f,.13f):key=="SlumPlaster"?new(.51f,.46f,.35f):key=="SlumPatina"?new(.17f,.31f,.28f):key=="SlumTarpaulin"?new(.09f,.24f,.39f):key=="Pitch"?new(.12f,.32f,.18f):key=="PitchLight"?new(.16f,.39f,.23f):key=="Clay"?new(.52f,.28f,.16f):key=="Court"?new(.59f,.34f,.19f):key=="SeatBlue"?new(.035f,.29f,.43f):key=="SeatCoral"?new(.63f,.20f,.14f):key=="GardenSoil"?new(.08f,.12f,.09f):key=="CanopyLeaf"?new(.035f,.24f,.12f):key=="CanopyLight"?new(.16f,.38f,.18f):key=="DeepDeck"?new(.095f,.15f,.18f):new(.15f,.2f,.24f);
                mat.color=c;mat.SetFloat("_Smoothness",key=="Court"?.55f:.2f);}
            materials[key]=mat;return mat;
        }
        Batch Get(string key){if(!batches.TryGetValue(key,out var b)){b=new Batch();batches[key]=b;}return b;}
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,string mat,bool both=false)
        {
            var m=Get(mat);int k=m.v.Count;Vector3 n=Vector3.Cross(b-a,c-a).normalized;float w=Vector3.Distance(a,b)*.25f,h=Vector3.Distance(b,c)*.25f;
            m.v.AddRange(new[]{a,b,c,d});for(int i=0;i<4;i++)m.n.Add(n);m.uv.AddRange(new[]{Vector2.zero,new Vector2(w,0),new Vector2(w,h),new Vector2(0,h)});m.t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});if(both)m.t.AddRange(new[]{k+2,k+1,k,k+3,k+2,k});
        }
        public GameObject Box(string name,Vector3 p,Vector3 size,string mat,bool collision=false,Quaternion? rotation=null)
        {
            var q=rotation??Quaternion.identity;Vector3 h=size*.5f;
            Vector3 V(float x,float y,float z)=>p+q*Vector3.Scale(h,new(x,y,z));
            Quad(V(-1,-1,-1),V(-1,1,-1),V(1,1,-1),V(1,-1,-1),mat);
            Quad(V(1,-1,1),V(1,1,1),V(-1,1,1),V(-1,-1,1),mat);
            Quad(V(-1,-1,1),V(-1,1,1),V(-1,1,-1),V(-1,-1,-1),mat);
            Quad(V(1,-1,-1),V(1,1,-1),V(1,1,1),V(1,-1,1),mat);
            Quad(V(-1,1,-1),V(-1,1,1),V(1,1,1),V(1,1,-1),mat);
            Quad(V(-1,-1,1),V(-1,-1,-1),V(1,-1,-1),V(1,-1,1),mat);
            if(!collision)return null;var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localRotation=q;go.AddComponent<BoxCollider>().size=size;return go;
        }
        public void Beam(Vector3 a,Vector3 b,float width,string mat,bool collision=false)=>Box("Structural beam",(a+b)*.5f,new(width,Vector3.Distance(a,b),width),mat,collision,Quaternion.FromToRotation(Vector3.up,b-a));
        public void Cylinder(Vector3 p,float radius,float height,string mat,int segments=32,float top=-1)
        {
            if(top<0)top=radius;for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;var lo=p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);var hi=p+new Vector3(Mathf.Cos(a)*top,height,Mathf.Sin(a)*top);var ln=p+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);var hn=p+new Vector3(Mathf.Cos(b)*top,height,Mathf.Sin(b)*top);Quad(lo,hi,hn,ln,mat);Quad(p+Vector3.up*height,hn,hi,p+Vector3.up*height,mat);}
        }
        public void Ring(Vector3 p,float rx,float rz,float thickness,string mat,int segments=96,float from=0,float to=360)
        {
            for(int i=0;i<segments;i++){float a=Mathf.Lerp(from,to,(float)i/segments)*Mathf.Deg2Rad,b=Mathf.Lerp(from,to,(float)(i+1)/segments)*Mathf.Deg2Rad;
                var x=p+new Vector3(Mathf.Cos(a)*rx,0,Mathf.Sin(a)*rz);var y=p+new Vector3(Mathf.Cos(b)*rx,0,Mathf.Sin(b)*rz);Beam(x,y,thickness,mat);}
        }
        public void Dome(Vector3 p,Vector3 radius,string glass,int meridians=32,int parallels=10,bool ribs=true)
        {
            Vector3 P(float a,float b)=>p+Vector3.Scale(radius,new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b)));
            for(int i=0;i<meridians;i++)for(int j=0;j<parallels;j++)
            {float a=i*2*Mathf.PI/meridians,b=(i+1)*2*Mathf.PI/meridians,t=j*Mathf.PI*.5f/parallels,u=(j+1)*Mathf.PI*.5f/parallels;
                // Glass uses Cull Off: a reversed copy blended the exact same pane
                // twice, increasing opacity and transparent fill cost across the dome.
                Quad(P(a,t),P(a,u),P(b,u),P(b,t),glass);if(ribs){Beam(P(a,t),P(a,u),.25f,"FutureSilver");if(j%2==0)Beam(P(a,t),P(b,t),.2f,"FutureSilver");}}
        }
        public void Stairs(Vector3 bottom,float width,float rise,float run,string mat)
        {
            int n=Mathf.CeilToInt(rise/.18f);for(int i=0;i<n;i++)Box("Stair tread",bottom+new Vector3(0,(i+.5f)*rise/n,(i+.5f)*run/n),new(width,rise/n,run/n+.02f),mat);
            // Smooth collision ramp, independent from the visual treads. No overhead blocker.
            float angle=Mathf.Atan2(rise,run)*Mathf.Rad2Deg;
            Box("Walkable stair ramp",bottom+new Vector3(0,rise*.5f-.11f,run*.5f),new(width,.22f,Mathf.Sqrt(rise*rise+run*run)),mat,true,Quaternion.Euler(-angle,0,0));
            for(int s=-1;s<=1;s+=2)Beam(bottom+new Vector3(s*width*.5f,1,0),bottom+new Vector3(s*width*.5f,rise+1,run),.07f,"Steel");
        }
        public TextMesh Sign(string text,Vector3 p,float size=.3f,float yaw=0)
        {var go=new GameObject(text);go.transform.SetParent(root,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,yaw,0);var t=go.AddComponent<TextMesh>();t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.text=text;t.fontSize=60;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.65f,.96f,1);var r=t.GetComponent<MeshRenderer>();if(!depthText){depthText=new Material(Resources.Load<Shader>("Shaders/CityDepthText"));depthText.mainTexture=t.font.material.mainTexture;Font.textureRebuilt+=f=>{if(depthText)depthText.mainTexture=f.material.mainTexture;};}r.sharedMaterial=depthText;r.shadowCastingMode=ShadowCastingMode.Off;return t;}
        // One low-poly collision mesh follows the real volumes, including setbacks and twin-tower gaps.
        public void SolidPrism(Vector3 center,Vector3[] outline,float height)
        {
            int start=solidVertices.Count,n=outline.Length;
            for(int i=0;i<n;i++){solidVertices.Add(center+outline[i]-Vector3.up*height*.5f);solidVertices.Add(center+outline[i]+Vector3.up*height*.5f);}
            for(int i=0;i<n;i++)
            {
                int a=start+i*2,b=start+((i+1)%n)*2;
                solidTriangles.AddRange(new[]{a,a+1,b+1,a,b+1,b});
                if(i>0&&i<n-1){solidTriangles.AddRange(new[]{start+1,b+1,a+1,start,a,b});}
            }
        }
        public void Finish()
        {
            var owner=root.GetComponent<CityMeshOwner>();if(!owner)owner=root.gameObject.AddComponent<CityMeshOwner>();
            foreach(var pair in batches){var b=pair.Value;var mesh=new Mesh{name=root.name+" / "+pair.Key,indexFormat=IndexFormat.UInt32};mesh.SetVertices(b.v);mesh.SetNormals(b.n);mesh.SetUVs(0,b.uv);mesh.SetTriangles(b.t,0);mesh.RecalculateBounds();owner.meshes.Add(mesh);
                var go=new GameObject("Architecture / "+pair.Key,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=Material(pair.Key);renderer.shadowCastingMode=pair.Key.Contains("Glass")?ShadowCastingMode.Off:ShadowCastingMode.On;}
            batches.Clear();
            if(solidVertices.Count>0)
            {
                var mesh=new Mesh{name=root.name+" / exterior collision",indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(solidVertices);mesh.SetTriangles(solidTriangles,0);mesh.RecalculateBounds();owner.meshes.Add(mesh);
                var shell=new GameObject("Facade collision shell",typeof(MeshCollider));shell.transform.SetParent(root,false);shell.GetComponent<MeshCollider>().sharedMesh=mesh;
                solidVertices.Clear();solidTriangles.Clear();
            }
        }
    }
    public sealed class CityMeshOwner:MonoBehaviour
    {
        public readonly List<Mesh> meshes=new();
        void OnDestroy(){foreach(var m in meshes)if(m)Destroy(m);}
    }
}
