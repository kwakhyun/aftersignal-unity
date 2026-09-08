using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed partial class HighriseInterior:MonoBehaviour
    {
        public static HighriseInterior Active{get;private set;}
        public HighriseBuilding Building{get;private set;}
        public MultiFloorLift Lift{get;private set;}
        public int LoadedFloors=>loaded.Count;
        readonly Dictionary<int,GameObject> loaded=new();readonly Dictionary<int,float[]> health=new();
        readonly Dictionary<int,WorldActor[]> staff=new();
        float width,depth,height;Vector3 origin;Material glass;int last=-1;float next;
        static readonly string[] floors={"안내 로비 · 카페","고객 상담실","개발 사무실","회의실 · 자료실","연구실","직원 식당","공유 업무 공간","관리 사무실"};
        public static void Open(HighriseBuilding building)
        {
            if(Active)Active.Leave();
            var go=new GameObject("Interior / "+building.Title);Active=go.AddComponent<HighriseInterior>();Active.Initialize(building);
        }
        void Initialize(HighriseBuilding building)
        {
            Building=building;building.ShowExterior(false);
            width=Mathf.Clamp(building.Bounds.size.x*.82f,18,46);depth=Mathf.Clamp(building.Bounds.size.z*.82f,18,38);
            height=Mathf.Max(3.7f,building.Bounds.size.y/building.Floors);origin=new Vector3(building.Bounds.center.x,building.Bounds.min.y,building.Bounds.center.z);
            transform.position=origin;
            glass=new Material(Resources.Load<Material>("Materials/Glass"));glass.name="Highrise transparent glazing";
            if(glass.HasProperty("_BaseColor"))glass.SetColor("_BaseColor",new Color(.19f,.49f,.57f,.2f));
            var shaft=new GameObject("Panoramic lift").transform;shaft.SetParent(transform,false);shaft.localPosition=new Vector3(width*.5f-2,0,depth*.5f-2);
            Lift=shaft.gameObject.AddComponent<MultiFloorLift>();Lift.floors=building.Floors;Lift.floorHeight=height;Lift.speed=7;Lift.floorNames=new string[building.Floors];
            for(int f=0;f<building.Floors;f++)Lift.floorNames[f]=FloorName(f);
            var cabin=new GameObject("Lift cabin").transform;cabin.SetParent(shaft,false);Lift.platform=cabin;
            Box(cabin,"Lift floor",new(0,-.1f,0),new(3.5f,.2f,3.5f),"Steel");
            Box(cabin,"Lift ceiling",new(0,2.8f,0),new(3.5f,.1f,3.5f),"Steel");
            for(int s=-1;s<=1;s+=2)Box(cabin,"Lift glass side",new(s*1.75f,1.3f,0),new(.07f,2.6f,3.5f),"Glass");
            Box(cabin,"Lift back",new(0,1.3f,1.75f),new(3.5f,2.6f,.07f),"Glass");
            Button(cabin,new(0,1,-.9f),0);Load(0);Load(1);
            Physics.SyncTransforms();GameDirector.Instance.Player.Respawn(origin+new Vector3(0,.12f,-depth*.5f+3),false);
            GameDirector.Instance.Toast(building.Title+" · 1층\n승강기에서 1–"+building.Floors+"층을 선택할 수 있습니다.",5);
        }
        string FloorName(int f)=>f==Building.Floors-1?"스카이라운지 · 전망 테라스":f==0?floors[0]:floors[1+(f+Building.Identity)%7];
        public void ArriveAt(int floor){floor=Mathf.Clamp(floor,0,Building.Floors-1);Load(floor);Physics.SyncTransforms();GameDirector.Instance.Player.Respawn(origin+new Vector3(0,floor*height+.12f,0),false);Lift.Go(floor);}
        void Load(int floor)
        {
            if(floor<0||floor>=Building.Floors||loaded.ContainsKey(floor))return;
            var root=new GameObject((floor+1)+"F / "+FloorName(floor));root.transform.SetParent(transform,false);root.transform.localPosition=Vector3.up*floor*height;loaded[floor]=root;
            var p=root.transform;float w=width,d=depth;
            // The rear-right 4x4 m opening is reserved for the lift through every storey.
            Box(p,"Continuous office floor",new(-2,-.12f,0),new(w-4,.24f,d),"TerminalFloor");
            Box(p,"Lift lobby floor",new(w*.5f-2,-.12f,-2),new(4,.24f,d-4),"TerminalFloor");
            Box(p,"Ceiling",new(-2,height-.12f,0),new(w-4,.16f,d),"Cladding");
            Box(p,"Ceiling lobby",new(w*.5f-2,height-.12f,-2),new(4,.16f,d-4),"Cladding");
            for(int side=-1;side<=1;side+=2)
            {
                for(int n=0;n<Mathf.CeilToInt(w/3);n++)
                {
                    float span=w/Mathf.CeilToInt(w/3),x=-w/2+span*(n+.5f);
                    if(floor==0&&side<0&&Mathf.Abs(x)<3)continue;
                    Pane(p,new(x,height*.45f,side*d*.5f),new(span-.12f,height*.8f,.07f));
                    Box(p,"Curtain wall mullion",new(x-span*.5f,height*.5f,side*d*.5f),new(.1f,height,.16f),"Steel");
                }
                for(int n=0;n<Mathf.CeilToInt(d/3);n++)
                {float span=d/Mathf.CeilToInt(d/3),z=-d/2+span*(n+.5f);Pane(p,new(side*w*.5f,height*.45f,z),new(.07f,height*.8f,span-.12f));}
            }
            Button(p,new(w*.5f-2,1,d*.5f-4.8f),floor);
            var gate=Box(p,"Lift landing safety gate",new(w*.5f-2,1.1f,d*.5f-4),new(3.7f,2.2f,.12f),"Glass");
            gate.AddComponent<HighriseLanding>().Initialize(Lift,origin.y+floor*height);
            Sign(p,(floor+1)+"F  "+FloorName(floor),new(0,2.6f,-d*.5f+1),.14f);
            if(floor==0)
            {
                var door=new GameObject("Street exit");door.transform.SetParent(p,false);door.transform.localPosition=new Vector3(0,1,-d*.5f+1);
                var point=door.AddComponent<InteractionPoint>();point.title="건물 밖으로";point.radius=3;point.kind=InteractionKind.Furniture;door.AddComponent<HighriseDoor>().exit=true;
            }
            // Clear central aisle and lift lobby; rooms occupy the western two thirds.
            int theme=floor==0?0:floor==Building.Floors-1?7:(floor+Building.Identity)%7;
            var deskPositions=new List<Vector3>();
            int rows=Mathf.Clamp(Mathf.FloorToInt((d-8)/5.5f),2,5),cols=Mathf.Clamp(Mathf.FloorToInt((w-12)/6),1,4);
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {
                Vector3 at=new(-w*.5f+4+col*5.5f,0,-d*.5f+5+row*5.5f);deskPositions.Add(at);
                bool lounge=theme==0||theme==5||theme==7;
                Desk(p,at,lounge,theme);
            }
            for(int s=-1;s<=1;s+=2)
            {
                var at=new Vector3(s*(w*.5f-2),0,-d*.5f+2);Box(p,"Planter",at+Vector3.up*.35f,new(1,.7f,1),"FutureCeramic");
                Box(p,"Indoor foliage",at+Vector3.up*1.1f,new(.8f,1.2f,.8f),"Leaf",false);
            }
            Box(p,"Tea counter",new(-w*.5f+3,.55f,d*.5f-2.3f),new(4,1.1f,1.2f),"Wood");
            Box(p,"Coffee machine",new(-w*.5f+2.4f,1.45f,d*.5f-2.3f),new(.6f,.7f,.55f),"Steel");
            Box(p,"Storage shelving",new(-w*.5f+.5f,1.3f,0),new(.7f,2.6f,5),"Wood");
            for(int n=0;n<4;n++)Box(p,"Shelf",new(-w*.5f+.1f,.3f+n*.65f,0),new(1,.07f,5),"Steel");
            for(int n=0;n<3;n++)Box(p,"Linear ceiling light",new(-w*.2f,height-.25f,-d*.3f+n*d*.3f),new(w*.45f,.07f,.18f),"NeonWarm",false);
            int count=Mathf.Min(10,deskPositions.Count);if(!health.ContainsKey(floor))health[floor]=new float[count];
            staff[floor]=new WorldActor[count];Amenities(p,w,d,height);
            for(int n=0;n<count;n++)
            {
                if(health[floor][n]<0)continue;
                var go=new GameObject("Employee "+n,typeof(SpriteRenderer),typeof(CityNpc));go.transform.SetParent(p,false);go.transform.localPosition=deskPositions[n]+new Vector3(0,.04f,-1.1f);
                string art=theme==4?(n%2==0?"Doctor":"Nurse"):theme==5?(n%3==0?"Bartender":"OfficeWoman"):n%2==0?"OfficeMan":"OfficeWoman";
                var sr=go.GetComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.sprite=PeopleArt.Get(art,2);
                go.GetComponent<CityNpc>().Configure(Building.Identity*31+floor*16+n,theme==4?"연구원":theme==5&&n%3==0?"구내식당 직원":"사무 직원",null,Building.Title+" "+(floor+1)+"층 "+FloorName(floor)+"에서 근무한다. 동료와 회의하거나 업무를 처리하고 휴게실에서 쉰다.");
                PeopleArt.Attach(go,art);var routine=go.AddComponent<CivicRoutine>();routine.Initialize("사무",go.transform.position);
                staff[floor][n]=go.GetComponent<WorldActor>();
                routine.work=go.transform.position;routine.social=p.TransformPoint(new Vector3(w*.15f,.04f,0));routine.meal=p.TransformPoint(new Vector3(-w*.5f+3,.04f,d*.5f-4));routine.toilet=p.TransformPoint(new Vector3(w*.2f,.04f,-d*.5f+3));
                if(health[floor][n]>0)go.GetComponent<WorldActor>().health=health[floor][n];
            }
        }
        void Desk(Transform p,Vector3 at,bool lounge,int theme)
        {
            Box(p,lounge?"Cafe tabletop":"Work desk",at+Vector3.up*.82f,new(2.6f,.12f,1.25f),lounge?"Wood":"FutureCeramic");
            for(int s=-1;s<=1;s+=2)Box(p,"Desk pedestal",at+new Vector3(s*1,.4f,0),new(.16f,.8f,.9f),"Steel");
            Box(p,"Ergonomic chair seat",at+new Vector3(0,.48f,-1.1f),new(.75f,.16f,.7f),"FutureCarbon");
            Box(p,"Chair back",at+new Vector3(0,.92f,-1.45f),new(.75f,.85f,.12f),"FutureCarbon");
            if(!lounge){Box(p,"Monitor stand",at+Vector3.up*1.08f,new(.12f,.42f,.16f),"Steel");Box(p,"Monitor",at+new Vector3(0,1.43f,.22f),new(1.18f,.66f,.07f),"FutureCarbon");Box(p,"Active data screen",at+new Vector3(0,1.43f,.175f),new(1.05f,.52f,.012f),"NeonCyan",false);Box(p,"Keyboard",at+new Vector3(0,.92f,-.3f),new(.75f,.05f,.25f),"Steel");}
            else Box(p,"Cup",at+new Vector3(.5f,1.02f,0),new(.16f,.26f,.16f),"FutureCeramic",false);
        }
        void Button(Transform p,Vector3 at,int floor)
        {
            var go=new GameObject("Lift selection");go.transform.SetParent(p,false);go.transform.localPosition=at;
            var point=go.AddComponent<InteractionPoint>();point.kind=InteractionKind.Lift;point.title="승강기 · "+Building.Floors+"개 층 선택";point.radius=2.7f;
            var access=go.AddComponent<FloorAccess>();access.lift=Lift;access.floor=floor;
        }
        public GameObject Box(Transform p,string title,Vector3 at,Vector3 size,string material,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=title;go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material=="Glass"?glass:Resources.Load<Material>("WorldAssets/Generated/"+material)??Resources.Load<Material>("Materials/"+material);
            if(!collision)Destroy(go.GetComponent<Collider>());return go;
        }
        void Pane(Transform p,Vector3 at,Vector3 size){var pane=Box(p,"Breakable office window",at,size,"Glass");var glass=pane.AddComponent<BreakableGlass>();glass.health=35;glass.campaign=false;}
        void Sign(Transform p,string words,Vector3 at,float size)
        {
            var go=new GameObject("Floor directory",typeof(TextMesh));go.transform.SetParent(p,false);go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=go.GetComponent<TextMesh>();text.text=words;text.characterSize=size;text.fontSize=48;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.65f,1,.94f);
            text.font=Resources.Load<Font>("Fonts/NotoSansKR");if(text.font)text.GetComponent<Renderer>().sharedMaterial=text.font.material;
            go.AddComponent<WorldSign>();
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||!Lift)return;
            int floor=Lift.Aboard(g.Player)?Lift.CurrentFloor:Mathf.Clamp(Mathf.RoundToInt((g.Player.transform.position.y-origin.y)/height),0,Building.Floors-1);
            if(floor==last&&Time.time<next)return;last=floor;next=Time.time+.5f;
            Load(floor);Load(floor+1);Load(floor-1);Load(Lift.Destination);
            foreach(var pair in new List<KeyValuePair<int,GameObject>>(loaded))
                if(Mathf.Abs(pair.Key-floor)>1&&pair.Key!=Lift.Destination)
                {
                    var actors=staff[pair.Key];
                    for(int i=0;i<actors.Length;i++)health[pair.Key][i]=actors[i]&&actors[i].Alive?actors[i].health:-1;
                    staff.Remove(pair.Key);
                    Destroy(pair.Value);loaded.Remove(pair.Key);
                }
            if(g.Player.transform.position.y<origin.y-2)g.Player.Respawn(origin+new Vector3(0,.1f,-depth*.5f+3),false);
        }
        public void Leave()
        {
            var g=GameDirector.Instance;if(Building){Building.ShowExterior(true);if(g)g.Player.Respawn(Building.Door+Vector3.back*1.5f,false);}
            Active=null;Destroy(gameObject);
        }
        void OnDestroy(){if(Active==this){Active=null;if(Building)Building.ShowExterior(true);}if(glass)Destroy(glass);}
    }
}
