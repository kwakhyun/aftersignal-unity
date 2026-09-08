using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class OceanLife : MonoBehaviour
    {
        public const float Surface=-.95f;
        public static bool Swimming {get;private set;}
        public static bool Submerged => Swimming && Camera.main && Camera.main.transform.position.y<Surface;
        public static float Shore(float x)
        {
            float[] xs={0,250,480,780,1050,1250,1450,2200},zs={-510,-545,-610,-655,-590,-580,-565,-565};
            for(int i=1;i<xs.Length;i++)if(x<=xs[i])return Mathf.Lerp(zs[i-1],zs[i],Mathf.InverseLerp(xs[i-1],xs[i],x));return -565;
        }
        public static bool Contains(Vector3 p)=>p.x>2&&p.x<2198&&p.z<Shore(p.x)-12&&p.z> -2160;
        public static float Bed(float x,float z)=>-3-Mathf.Min(42,(Shore(x)-z)*.065f)+Mathf.Sin(x*.033f)*Mathf.Sin(z*.022f)*1.5f;
        readonly List<MarineAnimal> fauna=new List<MarineAnimal>();float next;
        void Start()
        {
            var rng=new System.Random(3721);
            for(int i=0;i<180;i++)
            {
                float x=150+(float)rng.NextDouble()*1840,z=Shore(x)-65-(float)rng.NextDouble()*310;
                float y=Mathf.Lerp(Bed(x,z)+2,-2,(float)rng.NextDouble());
                var fish=new GameObject(i%23==0?"Reef manta":i%17==0?"Sea turtle":"Reef fish school").AddComponent<MarineAnimal>();
                fish.transform.SetParent(transform);fish.transform.position=new Vector3(x,y,z);fish.Initialize(i);fauna.Add(fish);
            }
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||Time.time<next)return;next=Time.time+.7f;
            foreach(var fish in fauna)if(fish)fish.gameObject.SetActive((fish.transform.position-g.Player.transform.position).sqrMagnitude<150*150);
        }
        public static bool Tick(PlayerMotor player,ControlFrame input,float dt)
        {
            if(player.Director.stage!=StageId.UrbanCity){Swimming=false;return false;}
            var at=player.transform.position;
            if(!Contains(at)||at.y>Surface+.5f){Swimming=false;return false;}
            Swimming=true;player.Rope.Release();
            var direction=player.Director.CameraRig.MoveDirection(input.move);
            float vertical=input.vertical;
            if(Mathf.Abs(vertical)<.1f)vertical=at.y<Surface-1?0:.15f;
            float speed=input.boost?7:4;
            player.Velocity=Vector3.MoveTowards(player.Velocity,direction*speed+Vector3.up*vertical*3.4f,dt*14);
            player.Controller.Move(player.Velocity*dt);
            at=player.transform.position;at.y=Mathf.Clamp(at.y,Bed(at.x,at.z)+.25f,Surface+.35f);player.transform.position=at;
            if(input.jump&&at.y>Surface-.3f){player.Velocity.y=6;player.Controller.Move(Vector3.up*.65f);Swimming=false;}
            player.GetComponent<PixelActor>().TickHero(player,dt);
            return true;
        }
        void LateUpdate()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||!Camera.main)return;
            if(Camera.main.transform.position.y<Surface&&Contains(Camera.main.transform.position))
            {
                RenderSettings.fog=true;RenderSettings.fogDensity=.035f;RenderSettings.fogColor=new Color(.025f,.19f,.24f);
                Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=RenderSettings.fogColor;
            }
            else if(g.stage==StageId.UrbanCity)Camera.main.clearFlags=CameraClearFlags.Skybox;
        }
        void OnDestroy(){Swimming=false;}
    }
    public sealed class MarineAnimal : MonoBehaviour
    {
        Vector3 home;Transform tail;float phase;int kind;
        static Material[] colors;
        public void Initialize(int serial)
        {
            home=transform.position;phase=serial*.83f;kind=serial%23==0?2:serial%17==0?1:0;
            if(colors==null)
            {
                colors=new Material[4];var t=Resources.Load<Material>("WorldAssets/Generated/Aluminium");
                Color[] c={new Color(.21f,.66f,.71f),new Color(.85f,.7f,.15f),new Color(.12f,.27f,.31f),new Color(.28f,.43f,.22f)};
                for(int i=0;i<4;i++){colors[i]=new Material(t);colors[i].color=c[i];}
            }
            Part("Body",Vector3.zero,kind==2?new Vector3(2,.18f,1.65f):kind==1?new Vector3(1.2f,.38f,.83f):new Vector3(.74f,.3f,.2f),kind==1?3:serial%3);
            tail=new GameObject("Swimming tail and fins").transform;tail.SetParent(transform,false);
            var mesh=new Mesh();mesh.vertices=kind==2?new[]{new Vector3(-.4f,0,-.7f),new Vector3(.45f,0,-.3f),new Vector3(-.7f,0,-2.8f),new Vector3(-.4f,0,.7f),new Vector3(.45f,0,.3f),new Vector3(-.7f,0,2.8f)}:new[]{new Vector3(-.28f,0,0),new Vector3(-.73f,.29f,0),new Vector3(-.65f,-.29f,0),new Vector3(.1f,.1f,0),new Vector3(-.25f,.37f,0),new Vector3(-.25f,.1f,0)};
            mesh.triangles=new[]{0,1,2,2,1,0,3,4,5,5,4,3};mesh.RecalculateNormals();tail.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;tail.gameObject.AddComponent<MeshRenderer>().sharedMaterial=colors[serial%3];
            for(int side=-1;side<=1;side+=2)Part("Eye",new Vector3(kind==1?.5f:.26f,.055f,side*.09f),Vector3.one*.065f,2);
            if(kind==1){Part("Head",new Vector3(.67f,0,0),new Vector3(.38f,.25f,.25f),3);for(int side=-1;side<=1;side+=2)Part("Flipper",new Vector3(0,-.08f,side*.6f),new Vector3(.7f,.07f,.55f),3);}
        }
        void Part(string n,Vector3 at,Vector3 scale,int color)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=n;o.transform.SetParent(transform,false);o.transform.localPosition=at;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=colors[color];Destroy(o.GetComponent<Collider>());}
        void Update()
        {
            if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            float t=Time.time*(kind==0?.2f:.065f)+phase;var p=home+new Vector3(Mathf.Cos(t)*11,Mathf.Sin(t*.8f)*1.1f,Mathf.Sin(t)*8);
            var heading=p-transform.position;transform.position=p;
            if(heading.sqrMagnitude>.0001f)transform.rotation=Quaternion.Euler(0,Mathf.Atan2(-heading.z,heading.x)*Mathf.Rad2Deg,0);
            if(tail)tail.localRotation=Quaternion.Euler(kind==2?Mathf.Sin(Time.time*2+phase)*12:0,Mathf.Sin(Time.time*7+phase)*20,0);
        }
    }
}
