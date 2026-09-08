using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class ResidentialWorld:MonoBehaviour
    {
        public static ResidentialWorld Instance{get;private set;}
        public static int VisitHome=-1, VisitResident=-1;
        public static Vector3 ReturnPoint;
        public static StageId ReturnStage=StageId.UrbanCity;
        public const int ResidentCount=24;
        GameDirector game;
        readonly List<ResidentialCitizen> residents=new List<ResidentialCitizen>();
        public static readonly string[] Names={"보라","민준","해솔","리안","수진","지우","은서","도윤","나래","태오","유진","시온","하린","서준","가온","예린","현우","다은","주원","다솔","윤서","이준","소민","채원"};
        public static int Home(int person)=>person<19?person%2:2+(person-19);
        public static Vector3 Door(int home)=>home<2?UrbanCatalog.Door(home==0?0:16):new Vector3(100+(home-2)*140,.12f,303);
        public static string Address(int person)=>Home(person)<2?(Home(person)==0?"북문아파트":"북문아파트 2지구")+" "+(201+person/2)+"호":"북쪽 주택가 "+(Home(person)-1)+"번지";
        public static string Activity(int person,float hour)=>hour<7||hour>=21?"집에서 휴식":hour<9?"출근":hour<18?"직장에서 일하는 중":hour<20?"카페에서 이웃과 교류":"귀가";
        public static string VisitTitle => VisitResident>=0 ? Names[VisitResident]+"의 집 · "+Address(VisitResident) : "주민 주택";
        public static bool AtHome(float hour)=>hour<7||hour>=21;
        public static int WorkSite(int person)=>new[]{9,15,3,4,5,14,7,13}[person%8];
        public static Vector3 DayPlace(int person)=>UrbanCatalog.Door(WorkSite(person));
        void Awake(){Instance=this;}
        void Start()
        {
            game=GameDirector.Instance;
            if(game.stage==StageId.UrbanCity)
            {
                VisitHome=-1;
                for(int i=0;i<5;i++) BuildHouse(i);
                for(int i=0;i<2;i++) AddApartmentBalconies(i);
                for(int i=0;i<ResidentCount;i++)
                {
                    var citizen=MakeResident(i,AtHome(LifeState.Hour)?Door(Home(i)):DayPlace(i));
                    residents.Add(citizen);
                }
            }
            else if(game.stage==StageId.UrbanInterior)
            {
                if(VisitHome>=0)BuildHomeInterior(VisitHome);
                else for(int i=0;i<ResidentCount;i++)if(WorkSite(i)==UrbanCatalog.Current)
                {
                    var person=MakeResident(i,new Vector3(20+i%4*6,.1f,4+i%2*5));
                    person.indoors=true;person.workplace=true;residents.Add(person);
                }
            }
        }
        public static GameObject Box(Transform parent,string name,Vector3 at,Vector3 scale,string material,bool solid=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=at;g.transform.localScale=scale;
            var m=Resources.Load<Material>("Materials/"+material);if(m)g.GetComponent<Renderer>().sharedMaterial=m;
            if(!solid){g.GetComponent<Collider>().enabled=false;Destroy(g.GetComponent<Collider>());}
            return g;
        }
        static void Plant(Transform root,Vector3 p)
        {
            Box(root,"Ceramic planter",p+Vector3.up*.35f,new Vector3(.8f,.7f,.8f),"DistrictIvory");
            for(int i=0;i<5;i++)
            {
                var leaf=Box(root,"Sculpted foliage",p+new Vector3(Mathf.Sin(i*2)*.3f,1.05f+i*.13f,Mathf.Cos(i*2)*.3f),new Vector3(.7f,.12f,.4f),"Leaf",false);
                leaf.transform.localRotation=Quaternion.Euler(25,i*72,20);
            }
        }
        void BuildHouse(int index)
        {
            var root=new GameObject("Visit home / "+(index+1)).transform;root.SetParent(transform,false);
            root.position=new Vector3(100+index*140,0,316);
            Box(root,"Stone foundation",new Vector3(0,.1f,0),new Vector3(25,.2f,25),"UrbanSurface2");
            Box(root,"Home masonry",new Vector3(0,3.2f,0),new Vector3(22,6.2f,21),index%2==0?"UrbanBrick":"UrbanWall");
            Box(root,"Recessed front door",new Vector3(0,1.55f,-10.6f),new Vector3(2.4f,3.1f,.2f),"WarmWood",false);
            Box(root,"Door handle",new Vector3(.8f,1.4f,-10.8f),new Vector3(.12f,.5f,.12f),"VehicleAlloy",false);
            Box(root,"Entrance canopy",new Vector3(0,3.6f,-11.7f),new Vector3(5,.18f,3.6f),"Metal");
            for(int side=-1;side<=1;side+=2)
            {
                var roof=Box(root,"Sloped slate roof",new Vector3(side*5.7f,7.5f,0),new Vector3(12.3f,.3f,24),"WornSteel");
                roof.transform.localRotation=Quaternion.Euler(0,0,-side*18);
                for(int w=0;w<2;w++)
                {
                    float x=side*(4+w*3.4f);
                    Box(root,"Window frame",new Vector3(x,2.9f,-10.6f),new Vector3(2.8f,2.7f,.15f),"DistrictIvory",false);
                    Box(root,"Warm recessed glass",new Vector3(x,2.9f,-10.71f),new Vector3(2.5f,2.4f,.1f),"WarmWindow",false);
                    Box(root,"Window mullion",new Vector3(x,2.9f,-10.79f),new Vector3(.07f,2.5f,.06f),"WarmWood",false);
                }
            }
            for(int x=-10;x<=10;x+=2)Box(root,"Porch slat",new Vector3(x,.18f,-11.4f),new Vector3(1.9f,.06f,2),"WarmWood",false);
            Plant(root,new Vector3(-3,.2f,-11.4f));Plant(root,new Vector3(3,.2f,-11.4f));
            var entry=new GameObject("Home door");entry.transform.SetParent(transform,false);entry.transform.position=Door(index+2)+Vector3.up;
            var point=entry.AddComponent<InteractionPoint>();point.kind=InteractionKind.Furniture;point.title=Names[19+index]+"의 집 방문";point.radius=3.5f;
            entry.AddComponent<ResidentialDoor>().home=index+2;
        }
        void AddApartmentBalconies(int index)
        {
            var center=UrbanCatalog.Center(index==0?0:16);
            var root=new GameObject("Apartment balconies and planters").transform;root.SetParent(transform,false);
            for(int floor=0;floor<4;floor++)for(int col=-1;col<=1;col++)
            {
                var p=center+new Vector3(col*6,4+floor*3.8f,-27.6f);
                Box(root,"Balcony slab",p,new Vector3(5.5f,.18f,2),"DistrictIvory",false);
                Box(root,"Balcony rail",p+new Vector3(0,.8f,-.9f),new Vector3(5.3f,.07f,.08f),"VehicleAlloy",false);
                for(int bar=-2;bar<=2;bar++)Box(root,"Balcony vertical rail",p+new Vector3(bar,.4f,-.9f),new Vector3(.05f,.8f,.05f),"VehicleAlloy",false);
            }
        }
        public ResidentialCitizen MakeResident(int id,Vector3 at)
        {
            var go=new GameObject("Resident / "+Names[id],typeof(SpriteRenderer),typeof(CityPedestrian),typeof(ResidentialCitizen));
            go.transform.SetParent(transform,false);go.transform.position=at;
            var sprite=go.GetComponent<SpriteRenderer>();sprite.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            var frames=Resources.LoadAll<Sprite>("Art/NPC/Civic/"+new[]{"teacher","medic","concierge","commander"}[id%4]);
            System.Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
            go.GetComponent<CityPedestrian>().ResetAt(at,at,frames);
            var npc=go.AddComponent<CityNpc>();npc.Configure(2000+id,null,Names[id]);
            var resident=go.GetComponent<ResidentialCitizen>();resident.identity=id;return resident;
        }
        void BuildHomeInterior(int home)
        {
            var theme=FindAnyObjectByType<UrbanInterior>();
            if(theme)foreach(var t in theme.themes)if(t)t.SetActive(false);
            var root=new GameObject("Furnished resident home").transform;root.SetParent(transform,false);
            for(int row=0;row<35;row++)for(int col=0;col<4;col++)
                Box(root,"Oak floorboards",new Vector3(6.25f+col*14.5f,.025f,-15+row*.92f),new Vector3(14.48f,.04f,.91f),"HomeWood"+((row+col*3)%4),false);
            Box(root,"Warm plaster backdrop",new Vector3(29,4,18.65f),new Vector3(65,8,.12f),"Enamel",false);
            for(int bay=0;bay<3;bay++)
            {
                float x=9+bay*20;
                Box(root,"Window oak surround",new Vector3(x,4.7f,18.5f),new Vector3(8,4.8f,.18f),"WarmWood",false);
                Box(root,"Night window glazing",new Vector3(x,4.7f,18.38f),new Vector3(7.6f,4.4f,.1f),"DistrictWindow",false);
                Box(root,"Window vertical mullion",new Vector3(x,4.7f,18.26f),new Vector3(.09f,4.4f,.08f),"Enamel",false);
                Box(root,"Window sill",new Vector3(x,2.3f,18.2f),new Vector3(8.3f,.18f,.8f),"WarmWood",false);
                for(int side=-1;side<=1;side+=2)
                    Box(root,"Linen curtain",new Vector3(x+side*4.3f,4.6f,18),new Vector3(1,5.1f,.25f),"SoftCloth",false);
            }
            Box(root,"Bedroom partition",new Vector3(39,1.5f,11),new Vector3(.2f,3,13),"Enamel");
            Box(root,"Bedroom entry jamb",new Vector3(39,2.4f,2),new Vector3(.2f,1.2f,5),"Enamel");
            Box(root,"Living woven rug",new Vector3(25,.06f,0),new Vector3(14,.04f,10),"Seat",false);
            Box(root,"Sofa base",new Vector3(25,.5f,4),new Vector3(8,1,2.5f),"DistrictBlue");
            Box(root,"Sofa back",new Vector3(25,1.25f,5),new Vector3(8,1.2f,.5f),"DistrictBlue");
            for(int i=0;i<4;i++)Box(root,"Sofa cushion",new Vector3(22+i*2,1.1f,4),new Vector3(1.9f,.25f,1.8f),"Enamel",false);
            Box(root,"Coffee table",new Vector3(25,.65f,-1),new Vector3(5,.18f,3),"WarmWood");
            for(int side=-1;side<=1;side+=2)Box(root,"Table leg",new Vector3(25+side*2,.32f,-1),new Vector3(.15f,.65f,2.4f),"Metal");
            for(int i=0;i<4;i++)Box(root,"Books and magazines",new Vector3(24+i*.36f,.84f,-1),new Vector3(.3f,.12f,1.2f),i%2==0?"DistrictBlue":"DistrictWarm",false);
            Box(root,"Media console",new Vector3(11,.6f,-6),new Vector3(5.5f,1.2f,1.4f),"WarmWood");
            Box(root,"Television frame",new Vector3(11,1.95f,-6),new Vector3(4.3f,2.3f,.16f),"Metal",false);
            Box(root,"Television screen",new Vector3(11,1.95f,-5.89f),new Vector3(4.05f,2.05f,.03f),"DistrictWindow",false);
            for(int i=0;i<6;i++)
            {
                Box(root,"Kitchen cabinet",new Vector3(10+i*3.5f,1,16),new Vector3(3.4f,2,2.5f),"Enamel");
                Box(root,"Cabinet handle",new Vector3(10+i*3.5f,1.4f,14.68f),new Vector3(.8f,.07f,.08f),"VehicleAlloy",false);
            }
            Box(root,"Stone counter",new Vector3(19,2.1f,16),new Vector3(22,.18f,2.8f),"CeramicWall");
            Box(root,"Sink",new Vector3(15,2.2f,16),new Vector3(2,.08f,1.4f),"VehicleAlloy",false);
            Box(root,"Tap",new Vector3(15,2.65f,16.6f),new Vector3(.1f,.9f,.1f),"VehicleAlloy",false);
            for(int i=0;i<4;i++)Box(root,"Induction hob",new Vector3(22+i%2*.8f,2.23f,15.5f+i/2*.8f),new Vector3(.6f,.03f,.6f),"Metal",false);
            Box(root,"Refrigerator",new Vector3(6,1.8f,16),new Vector3(2.8f,3.6f,2.5f),"Enamel");
            Box(root,"Bed frame",new Vector3(49,.45f,10),new Vector3(7,.9f,8),"WarmWood");
            Box(root,"Mattress",new Vector3(49,1,10),new Vector3(6.8f,.4f,7.8f),"Enamel");
            Box(root,"Folded duvet",new Vector3(49,1.3f,8.8f),new Vector3(6.7f,.23f,5.2f),home%2==0?"DistrictBlue":"DistrictWarm",false);
            for(int i=-1;i<=1;i+=2)Box(root,"Pillow",new Vector3(49+i*1.6f,1.4f,12.8f),new Vector3(2.7f,.4f,1.4f),"Enamel",false);
            Box(root,"Wardrobe",new Vector3(58,2,12),new Vector3(3.5f,4,5),"WarmWood");
            for(int i=0;i<4;i++)Box(root,"Wardrobe door inlay",new Vector3(56.18f,2,10.2f+i*1.2f),new Vector3(.08f,3.8f,1.1f),"Enamel",false);
            Plant(root,new Vector3(35,0,14));Plant(root,new Vector3(13,0,-7));
            for(int i=0;i<ResidentCount;i++)if(i==VisitResident || VisitResident<0&&Home(i)==home&&i>=19)
            {
                var resident=MakeResident(i,new Vector3(19+(i%4)*5,.1f,3+(i%2)*5));
                resident.indoors=true;residents.Add(resident);
            }
            foreach(float x in new[]{18f,34f,49f})
            {
                var lamp=new GameObject("Warm home ceiling lamp",typeof(Light));lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(x,4.5f,5);
                var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.78f,.54f);light.intensity=14;light.range=25;light.shadows=LightShadows.None;
                Box(root,"Pendant shade",new Vector3(x,4.7f,5),new Vector3(2,.2f,1.3f),"DistrictLight",false);
            }
            Physics.SyncTransforms();
            game.Player.Respawn(CivicWorld.SafeSpawn(game.stage,new Vector3(25,.1f,-6)),false);
            game.CameraRig.Snap();
        }
        public static void EnterHome(int home,int resident=-1)
        {
            ReturnStage=StageId.UrbanCity;VisitHome=home;VisitResident=resident<0&&home>=2?19+home-2:resident;ReturnPoint=Door(home);
            GameDirector.Instance.Travel(StageId.UrbanInterior);
        }
        public static bool ExitHome()
        {
            if(VisitHome<0)return false;
            VisitHome=-1;
            CivicWorld.Travel(GameDirector.Instance,ReturnStage,ReturnPoint+Vector3.back*2);
            return true;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class ResidentialDoor:MonoBehaviour{public int home;}
    public sealed class ResidentialCitizen:MonoBehaviour
    {
        public int identity;
        public bool indoors,workplace;
        CityNpc npc;CityPedestrian walker;
        readonly Queue<Vector3> route=new Queue<Vector3>();
        string activity="";
        float refresh;
        void Start(){npc=GetComponent<CityNpc>();walker=GetComponent<CityPedestrian>();walker.speed=1.9f;}
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked)return;
            if(npc.Fleeing){walker.Tick(Mathf.Min(.06f,Time.deltaTime));return;}
            if(Time.time>=refresh)
            {
                refresh=Time.time+1;
                string next=ResidentialWorld.Activity(identity,LifeState.Hour);
                npc.context="주소: "+ResidentialWorld.Address(identity)+". 현재 일과: "+next+". 낮에는 직장과 카페에서 이웃을 만나고 밤에는 집에서 쉰다.";
                if(next!=activity)
                {
                    activity=next;
                    if(!indoors)
                    {
                        bool home=ResidentialWorld.AtHome(LifeState.Hour)||next=="귀가";
                        Vector3 target=home?ResidentialWorld.Door(ResidentialWorld.Home(identity)):next.Contains("교류")?UrbanCatalog.Door(15):ResidentialWorld.DayPlace(identity);
                        BuildRoute(target);
                    }
                }
            }
            if(indoors)
            {
                bool present=workplace ? LifeState.Hour>=9&&LifeState.Hour<18 : ResidentialWorld.AtHome(LifeState.Hour);
                GetComponent<SpriteRenderer>().enabled=present;
                if(npc.point)npc.point.gameObject.SetActive(present);
                GetComponent<Collider>().enabled=present;
                if(!present||GetComponent<CivicRoutine>())return;
                if(Vector3.Distance(walker.target,transform.position)<.3f)walker.WalkTo(new Vector3(18+(identity%4)*5,.1f,identity%2==0?2:7),false);
            }
            else
            {
                bool resting=route.Count==0 && (ResidentialWorld.AtHome(LifeState.Hour)&&Vector3.Distance(transform.position,ResidentialWorld.Door(ResidentialWorld.Home(identity)))<2 || LifeState.Hour>=9&&LifeState.Hour<18&&Vector3.Distance(transform.position,ResidentialWorld.DayPlace(identity))<2);
                GetComponent<SpriteRenderer>().enabled=!resting;
                if(npc.point)npc.point.gameObject.SetActive(!resting);
                GetComponent<Collider>().enabled=!resting;
                if(resting)return;
                if(Vector3.Distance(transform.position,walker.target)<.3f&&route.Count>0)
                {
                    var next=route.Dequeue();bool cross=Vector3.Distance(transform.position,next)<35;
                    walker.WalkTo(next,cross);
                }
            }
            walker.Tick(Mathf.Min(.06f,Time.deltaTime));
        }
        void BuildRoute(Vector3 target)
        {
            route.Clear();
            if(Vector3.Distance(transform.position,target)<1){walker.WalkTo(target,false);return;}
            var start=CityRoadNetwork.Sidewalk(transform.position);var goal=CityRoadNetwork.Sidewalk(target);
            var queue=new Queue<Vector3>();var previous=new Dictionary<Vector3,Vector3>();
            queue.Enqueue(start);previous[start]=start;
            while(queue.Count>0&&!previous.ContainsKey(goal))
            {
                var at=queue.Dequeue();
                for(int i=0;i<4;i++)
                {
                    var next=CityRoadNetwork.NextWalk(at,i,out _);
                    if(previous.ContainsKey(next))continue;
                    previous[next]=at;queue.Enqueue(next);
                }
            }
            var reverse=new List<Vector3>();
            if(previous.ContainsKey(goal)){var at=goal;while(at!=start){reverse.Add(at);at=previous[at];}}
            reverse.Reverse();route.Enqueue(start);foreach(var at in reverse)route.Enqueue(at);route.Enqueue(target);
            walker.WalkTo(transform.position,false);
        }
    }
}
