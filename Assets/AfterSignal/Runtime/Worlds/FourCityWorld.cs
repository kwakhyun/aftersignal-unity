using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class FourCityWorld:MonoBehaviour
    {
        public static FourCityWorld Instance{get;private set;}
        public bool Built{get;private set;}
        public readonly List<VenueRuntime> Facilities=new();
        sealed class Chunk
        {
            public Transform root; public Renderer[] renderers; public Bounds bounds;
            public bool culled;
        }
        readonly List<Chunk> chunks=new();
        float next;Vector3 lastVisibility;int announcedCity=-1;
        void Awake(){Instance=this;gameObject.AddComponent<FourCitySports>();gameObject.AddComponent<FourCityCampaign>();gameObject.AddComponent<FourCityAtmosphere>();gameObject.AddComponent<VenuePracticalLights>();}
        public VenueRuntime Find(string id)=>Facilities.Find(v=>v.Definition.id==id);
        Transform Root(string name,Vector3 at){var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=at;return go.transform;}
        IEnumerator Start()
        {
            BuildGround();yield return null;BuildConnections();yield return null;
            for(int i=0;i<FourCityCatalog.Venues.Length;i++)
            {var v=FourCityCatalog.Venues[i];var r=Root(v.title,v.position);var venue=r.gameObject.AddComponent<VenueRuntime>();venue.Initialize(i);Facilities.Add(venue);Track(r);yield return null;}
            for(int city=0;city<4;city++){BuildNeighborhood(city);yield return null;}
            BuildUnderseaShell();gameObject.AddComponent<RegionalWorld>();Physics.SyncTransforms();Built=true;
            FourCityCampaign.Instance?.BuildObjectives();
        }
        void Track(Transform root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            chunks.Add(new Chunk{root=root,renderers=renderers,bounds=bounds});
        }
        void BuildGround()
        {
            foreach(var r in FourCityCatalog.Land){var root=Root("Expanded city foundation",new(r.center.x,0,r.center.y));if(RegionalTerrain.Foundation(r,root))continue;var g=new CityGeometry(root);g.Box("Continuous city ground",new(0,-2.08f,0),new(r.width,4,r.height),"NovaConcrete",true);g.Finish();}
            var seabed=Root("Nereid basin",new(3900,-110,-4470));var bed=new CityGeometry(seabed);bed.Box("Deep basin",Vector3.zero,new(2850,4,2950),"NovaSeabed",true);bed.Finish();
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cylinder);deck.name="Nereid sealed continuous foundation";deck.transform.SetParent(transform);deck.transform.position=FourCityCatalog.Centers[3]-Vector3.up*.37f;deck.transform.localScale=new Vector3(2060,.25f,1860);Destroy(deck.GetComponent<Collider>());deck.AddComponent<MeshCollider>().sharedMesh=deck.GetComponent<MeshFilter>().sharedMesh;deck.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material("DeepDeck");
            Water(new Rect(2199,FourCityCatalog.South,FourCityCatalog.East-2199,400-FourCityCatalog.South));Water(new Rect(0,FourCityCatalog.South,2200,-4549-FourCityCatalog.South));
        }
        void Water(Rect r)
        {var root=Root("Extended sea surface",new(r.center.x,OceanLife.Surface,r.center.y));var g=new CityGeometry(root);const float tile=80;for(float x=-r.width*.5f;x<r.width*.5f;x+=tile)for(float z=-r.height*.5f;z<r.height*.5f;z+=tile){float xx=Mathf.Min(x+tile,r.width*.5f),zz=Mathf.Min(z+tile,r.height*.5f);if(!RegionalCatalog.InRift(root.TransformPoint(new Vector3((x+xx)*.5f,0,(z+zz)*.5f)),58))g.Quad(new(x,0,z),new(x,0,zz),new(xx,0,zz),new(xx,0,z),"Ocean");}g.Finish();}
        void BuildConnections()
        {
            int id=0;
            foreach(var road in FourCityCatalog.Roads)
            {
                var root=Root("Connector road "+id++,road[0]);var g=new CityGeometry(root);
                for(int i=1;i<road.Length;i++)
                {
                    var a=road[i-1]-root.position;var b=road[i]-root.position;var d=b-a;float length=d.magnitude;var q=Quaternion.LookRotation(d);
                    g.Box("Continuous road collider",(a+b)*.5f-Vector3.up*.05f,new(18,.24f,length),"Asphalt",true,q);
                    for(int s=-1;s<=1;s+=2){g.Box("Raised pavement",(a+b)*.5f+q*new Vector3(s*11,.05f,0),new(4,.1f,length),"Pavement",true,q);g.Box("Road edge",(a+b)*.5f+q*new Vector3(s*8.7f,.018f,0),new(.13f,.02f,length),"PaintWhite");}
                    for(float t=4;t<length;t+=12)g.Box("Centre dash",a+d*t/length+Vector3.up*.025f,new(.12f,.025f,5),"PaintAmber",false,q);
                    if(road[i].y<-.5f&&(road[i-1].y> -61||road[i].y> -61))
                    {
                        for(float t=0;t<length;t+=18){var p=Vector3.Lerp(a,b,t/length);for(int s=-1;s<=1;s+=2)g.Beam(p+q*new Vector3(s*13,0,0),p+q*new Vector3(s*13,8,0),.28f,"FutureSilver");g.Beam(p+q*new Vector3(-13,8,0),p+q*new Vector3(13,8,0),.3f,"FutureSilver");}
                        g.Box("Pressure roof",(a+b)*.5f+q*new Vector3(0,8.3f,0),new(27,.18f,length+.2f),"Glass",true,q);
                        for(int s=-1;s<=1;s+=2){g.Box("Pressure sidewall",(a+b)*.5f+q*new Vector3(s*13,4,0),new(.2f,8,length+.2f),"Glass",true,q);g.Box("Pressure tunnel guidance",(a+b)*.5f+q*new Vector3(s*12.8f,1,0),new(.08f,.08f,length),"NeonCyan");}
                    }
                    else for(float t=20;t<length;t+=48)
                    {var p=Vector3.Lerp(a,b,t/length)+q*new Vector3(11,0,0);g.Beam(p,p+Vector3.up*8,.19f,"Steel",true);g.Beam(p+Vector3.up*8,p+Vector3.up*8-q*Vector3.right*3,.16f,"Steel");g.Box("Road luminaire",p+Vector3.up*7.9f-q*Vector3.right*2,new(2,.13f,.4f),"NeonWarm");}
                }
                g.Finish();
            }
        }
        void BuildUnderseaShell()
        {
            var root=Root("Nereid pressure canopy",FourCityCatalog.Centers[3]);var g=new CityGeometry(root);
            g.Dome(Vector3.zero,new(1030,50,930),"PressureGlass",64,12,false);
            for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;var a=new Vector3(Mathf.Cos(angle)*1018,0,Mathf.Sin(angle)*918);var b=new Vector3(Mathf.Cos(angle)*700,37,Mathf.Sin(angle)*630);g.Beam(a,b,.9f,"FutureSilver");}
            g.Ring(Vector3.up*37,700,630,1.1f,"NeonCyan");g.Finish();
            for(int i=0;i<55;i++){var fish=new GameObject("Nereid exterior marine life").AddComponent<MarineAnimal>();fish.transform.SetParent(transform);float a=i*2.39996f;fish.transform.position=FourCityCatalog.Centers[3]+new Vector3(Mathf.Cos(a)*(1050+i%5*10),10+i%7*5,Mathf.Sin(a)*(950+i%5*10));fish.Initialize(480+i);}
        }
        void BuildNeighborhood(int city)
        {
            if(city<2)return; // Existing authored city blocks are relocated into free infill lots by CompactCityBuilder.
            var rng=new System.Random(9171+city*76);int count=city==2?1300:city==3?700:1200;var occupied=new List<Rect>();
            for(int i=0;i<count;i++)
            {
                float x,z,y=city==3?-62:0;
                if(city==2){x=2770+(float)rng.NextDouble()*2250;z=-1970+(float)rng.NextDouble()*2250;}
                else if(city==3){float a=i*2.39996f,r=300+(float)rng.NextDouble()*470;x=3900+Mathf.Cos(a)*r;z=-4480+Mathf.Sin(a)*r*.85f;}
                else {x=200+(float)rng.NextDouble()*1800;z=city==0?1200+(float)rng.NextDouble()*1040:-5500+(float)rng.NextDouble()*1250;}
                var p=new Vector3(x,y,z);float w=18+i%5*5,d=18+i%4*5;
                bool clear=!RegionalCatalog.InRift(p,80);foreach(var v in FourCityCatalog.Venues)if(Mathf.Abs(v.position.y-y)<5&&Mathf.Abs(x-v.position.x)<v.size.x*.5f+w&&Mathf.Abs(z-v.position.z)<v.size.y*.5f+d){clear=false;break;}
                if(!clear)continue;foreach(var road in FourCityCatalog.Roads)for(int n=1;n<road.Length;n++)if(Vector3.Distance(p,FourCityCatalog.Closest(p,road[n-1],road[n]))<Mathf.Max(w,d)+18)clear=false;
                var footprint=new Rect(x-w*.5f-10,z-d*.5f-10,w+20,d+20);foreach(var rect in occupied)if(rect.Overlaps(footprint)){clear=false;break;}
                if(!clear)continue;occupied.Add(footprint);var root=Root(city==2?"Fractured Erebos block":"Mixed use city block",p);var g=new CityGeometry(root);float h=city==3?12+i%5*4:city==2?20+i%8*14:18+i%8*11;
                FourCityArchitecture.Tower(g,w,d,h,i,city==2);FourCityArchitecture.Streetscape(g,w,d,i,city==2);g.Finish();Track(root);
                if(city!=2&&city!=3){var collapse=root.gameObject.AddComponent<CollapsibleBuilding>();collapse.worldBounds=new Bounds(p+Vector3.up*h*.5f,new(w,h,d));}
                if(city!=2)root.gameObject.AddComponent<HighriseBuilding>();
                if(city==2)
                {var debris=Root("Suspended fractured facade",p+new Vector3(12,70+i%6*18,0));var dg=new CityGeometry(debris);for(int n=0;n<4;n++)dg.Box("Floating concrete floor",new(n*4,n*2,0),new(14,.65f,11),"NovaConcrete",false,Quaternion.Euler(n*7,i*13,n*9));dg.Finish();debris.gameObject.AddComponent<FloatingRemnant>().phase=i;Track(debris);}
            }
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready)return;var p=game.Player.transform.position;if(Time.time<next&&(p-lastVisibility).sqrMagnitude<100*100)return;next=Time.time+.4f;lastVisibility=p;
            int city=FourCityCatalog.CityAt(p);if(city!=announcedCity&&!game.Blocked){announcedCity=city;if(city==2)game.Toast("EREBOS / 에레보스 잠식도시\n격리 관문 · 붕괴된 기억 · 잠식체 출몰 지역",5);if(city==3)game.Toast("NEREID / 네레이드 수중도시\n기밀 생활 돔 · 심해 연구원 · 블루 아카이브",5);}
            // Distance culling owns only forceRenderingOff. Renderer.enabled belongs to
            // interiors, destruction and LOD presentation; never resurrect those objects.
            var camera=Camera.main;var eye=camera?camera.transform.position:p;
            float range=Mathf.Lerp(2200,5000,Mathf.InverseLerp(25,180,p.y));
            foreach(var c in chunks)
            {
                if(!c.root)continue;
                float limit=range+(c.culled?0:180); // Hysteresis prevents boundary flicker.
                bool hide=Mathf.Min(c.bounds.SqrDistance(p),c.bounds.SqrDistance(eye))>limit*limit;
                if(hide==c.culled)continue;c.culled=hide;
                foreach(var r in c.renderers)if(r)r.forceRenderingOff=hide;
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class FloatingRemnant:MonoBehaviour
    {public float phase;Vector3 origin;void Start()=>origin=transform.position;void Update(){if(GameDirector.Instance&&GameDirector.Instance.Paused)return;transform.position=origin+Vector3.up*Mathf.Sin(Time.time*.24f+phase)*2.3f;transform.rotation=Quaternion.Euler(6*Mathf.Sin(Time.time*.1f+phase),phase*13+Time.time*.4f,phase%5*7);}}
}
