using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal
{
    // A single small mesh replaces an unbroken emissive line across the entire world.
    public sealed class RouteRibbon:MonoBehaviour
    {
        Mesh mesh;MeshRenderer view;Material material;
        readonly List<Vector3> vertices=new();readonly List<int> indices=new();
        public int ChevronCount {get;private set;}
        void Awake()
        {
            mesh=new Mesh{name="Local navigation chevrons"};mesh.MarkDynamic();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default"));material.SetColor("_BaseColor",new Color(.32f,.82f,.73f));material.color=new Color(.32f,.82f,.73f);material.SetFloat("_Cull",0);view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
        public void Show(bool show){if(view)view.enabled=show;}
        public void Draw(List<Vector3> route,Vector3 player,bool connected)
        {
            vertices.Clear();indices.Clear();ChevronCount=0;
            if(connected&&route!=null)
            for(int i=1;i<route.Count&&ChevronCount<28;i++)
            {
                var start=route[i-1];var d=route[i]-start;float length=d.magnitude;if(length<.05f)continue;var f=Vector3.ProjectOnPlane(d,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,f);
                // Sample only the nearby portion of a segment, including very long airport roads.
                float projection=Mathf.Clamp(Vector3.Dot(player-start,d/length),0,length);
                for(float n=Mathf.Max(0,Mathf.Floor((projection-90)/4.5f)*4.5f);n<Mathf.Min(length,projection+90)&&ChevronCount<28;n+=4.5f)
                {
                    var p=start+d*(n/length);if((p-player).sqrMagnitude>90*90||Mathf.Abs(p.y-player.y)>6)continue;
                    if(!NpcGroundSupport.Floor(p,p.y+2.5f,5,out var y)||Mathf.Abs(y-p.y)>2)continue;p.y=y+.075f;
                    Strip(p-right*.55f-f*.35f,p+f*.25f,.1f);Strip(p+right*.55f-f*.35f,p+f*.25f,.1f);ChevronCount++;
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        }
        void Strip(Vector3 a,Vector3 b,float width){var n=Vector3.Cross(Vector3.up,b-a).normalized*width;int i=vertices.Count;vertices.Add(a+n);vertices.Add(a-n);vertices.Add(b-n);vertices.Add(b+n);indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
