using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class ExplosionPresentation:MonoBehaviour
    {
        Light flash;float age,size,nextFire;static Material fire,smoke;
        public static int Detonations{get;private set;}
        public static void Detonate(Vector3 p,float radius)
        {
            Detonations++;CitySafety.Shock(p,"explosion");CombatVfx.Explosion(p,radius);
            if(PresentationSettings.Effects<=0)return;
            var go=new GameObject("DETONATION / fireball, pressure, smoke");go.transform.position=p;
            var fx=go.AddComponent<ExplosionPresentation>();fx.size=Mathf.Clamp(radius,2,7);
            fx.flash=go.AddComponent<Light>();fx.flash.type=LightType.Point;fx.flash.color=new Color(1,.55f,.15f);fx.flash.range=Mathf.Min(38,radius*7);fx.flash.intensity=14;fx.flash.shadows=LightShadows.None;
            var shader=Shader.Find("AfterSignal/ExplosionVolume");
            if(!fire){fire=new Material(shader);smoke=new Material(shader);smoke.SetFloat("_Smoke",1);}
            fx.Cloud("Primary fireball",false,42,1.1f,radius*1.1f,radius*1.8f,14);
            fx.Cloud("Rolling black smoke",true,42,7,radius*.9f,radius*1.4f,6);
            SignalEffects.Burst(p+Vector3.up,SignalEffects.Gold,65,22);
            SignalEffects.Ring(p+Vector3.up*.12f,new Color(1,.75f,.4f,.65f),radius*3.5f,.48f);
            SignalEffects.Dust(p,Vector3.up,radius);
            if(GameDirector.Instance&&Vector3.Distance(p,GameDirector.Instance.Player.transform.position)<45)GameDirector.Instance.CameraRig.Kick(.23f);
            Destroy(go,8);
        }
        void Cloud(string name,bool dark,int count,float life,float scale,float speed,float up)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=.5f;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.6f,life);main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.3f,speed);main.startSize=new ParticleSystem.MinMaxCurve(scale*.6f,scale*1.2f);main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=dark?-.025f:0;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=size*.35f;shape.rotation=new Vector3(-90,0,0);
            var emission=ps.emission;emission.enabled=false;
            var vel=ps.velocityOverLifetime;vel.enabled=true;vel.x=new ParticleSystem.MinMaxCurve(0,0);vel.y=new ParticleSystem.MinMaxCurve(up*.1f,up*.3f);vel.z=new ParticleSystem.MinMaxCurve(0,0);
            var fade=ps.colorOverLifetime;fade.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(dark?0:1,0),new GradientAlphaKey(dark?.72f:1,.18f),new GradientAlphaKey(0,1)});fade.color=g;
            var grow=ps.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.22f,1),new Keyframe(1,dark?3.4f:1.6f)));
            var limit=ps.limitVelocityOverLifetime;limit.enabled=true;limit.limit=dark?3:9;limit.dampen=.2f;
            var rend=ps.GetComponent<ParticleSystemRenderer>();rend.sharedMaterial=dark?smoke:fire;rend.sortMode=ParticleSystemSortMode.Distance;
            ps.Emit(count);Destroy(go,life+.5f);
        }
        void Update(){if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;age+=Time.deltaTime;if(flash)flash.intensity=14*Mathf.Exp(-age*10);
            if(age>nextFire&&age<3.2f){nextFire=age+.4f;Cloud("Secondary fuel ignition",false,6,.65f,size*.42f,1,5);}}
    }
    public static class WreckFragments
    {
        public static int Spawned{get;private set;}
        public static void Shatter(GameObject original,float force)
        {
            int count=0;var colliders=new List<Collider>();
            foreach(var filter in original.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
                var mesh=filter.sharedMesh;if(!mesh.isReadable)continue;
                // Split the large chassis along its longitudinal cells; wheels and trim detach intact.
                int slices=renderer.bounds.size.magnitude>3?4:1;
                var vertices=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;
                for(int slice=0;slice<slices&&count<42;slice++)
                {
                    var part=new Mesh{name="Fractured "+filter.name};part.vertices=vertices;if(uv.Length==vertices.Length)part.uv=uv;if(normals.Length==vertices.Length)part.normals=normals;part.subMeshCount=mesh.subMeshCount;
                    int total=0;
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {
                        var tris=mesh.GetTriangles(sub);var kept=new List<int>();bool useX=mesh.bounds.size.x>mesh.bounds.size.z;
                        for(int i=0;i<tris.Length;i+=3){var c=(vertices[tris[i]]+vertices[tris[i+1]]+vertices[tris[i+2]])/3;float t=useX?Mathf.InverseLerp(mesh.bounds.min.x,mesh.bounds.max.x,c.x):Mathf.InverseLerp(mesh.bounds.min.z,mesh.bounds.max.z,c.z);if(Mathf.Min(slices-1,(int)(t*slices))!=slice)continue;kept.Add(tris[i]);kept.Add(tris[i+1]);kept.Add(tris[i+2]);}part.SetTriangles(kept,sub);total+=kept.Count;
                    }
                    if(total==0){Object.Destroy(part);continue;}
                    var remap=new Dictionary<int,int>();var compact=new List<Vector3>();var compactUv=new List<Vector2>();var compactNormals=new List<Vector3>();var faces=new List<int[]>();
                    for(int sub=0;sub<part.subMeshCount;sub++){var indices=part.GetTriangles(sub);for(int k=0;k<indices.Length;k++){int old=indices[k];if(!remap.TryGetValue(old,out int index)){index=compact.Count;remap[old]=index;compact.Add(vertices[old]);if(uv.Length==vertices.Length)compactUv.Add(uv[old]);if(normals.Length==vertices.Length)compactNormals.Add(normals[old]);}indices[k]=index;}faces.Add(indices);}
                    part.Clear();part.SetVertices(compact);if(compactUv.Count>0)part.SetUVs(0,compactUv);if(compactNormals.Count>0)part.SetNormals(compactNormals);part.subMeshCount=faces.Count;for(int sub=0;sub<faces.Count;sub++)part.SetTriangles(faces[sub],sub);part.RecalculateBounds();
                    var go=new GameObject("WRECK / "+filter.name,typeof(MeshFilter),typeof(MeshRenderer),typeof(WreckDebris));go.layer=2;go.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);go.transform.localScale=filter.transform.lossyScale;go.GetComponent<MeshFilter>().sharedMesh=part;go.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                    var box=go.AddComponent<BoxCollider>();box.center=part.bounds.center;box.size=Vector3.Max(part.bounds.size,Vector3.one*.08f);
                    foreach(var c in colliders)Physics.IgnoreCollision(c,box);colliders.Add(box);
                    var rb=go.AddComponent<Rigidbody>();rb.mass=12;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.linearVelocity=(renderer.bounds.center-original.transform.position).normalized*force+Random.insideUnitSphere*3+Vector3.up*Random.Range(4f,9f);rb.angularVelocity=Random.insideUnitSphere*8;
                    count++;Spawned++;
                }
            }
            foreach(var r in original.GetComponentsInChildren<Renderer>())r.enabled=false;
            foreach(var c in original.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var l in original.GetComponentsInChildren<Light>())l.enabled=false;
        }
    }
    public sealed class WreckDebris:MonoBehaviour
    {
        float age;Vector3 scale;void Start(){scale=transform.localScale;}
        void Update(){if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;age+=Time.deltaTime;if(age>5.5f)transform.localScale=scale*Mathf.Clamp01((7.5f-age)/2);if(age>7.5f)Destroy(gameObject);}
        void OnDestroy(){var f=GetComponent<MeshFilter>();if(f&&f.sharedMesh)Destroy(f.sharedMesh);}
    }
}
