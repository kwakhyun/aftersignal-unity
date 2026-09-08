using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        [MenuItem("AFTERSIGNAL/World/Build civic renewal and government campus")]
        public static void BuildCivicRenewal()
        {
            AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);materials.Clear();boxes.Clear();meshId=9000;ImportMaterials();MakeMaterials();
            string[] palettes={"#cfbaa1","#294553","#823934","#aa8751","#b4d6c6","#9e6841","#455269","#6f8a6a","#a87786","#775143","#c0baba","#416875","#69747b","#a9864c","#303f59","#47695d"};
            for(int i=0;i<palettes.Length;i++)Mat("CivicFacade"+i,palettes[i],i%3==0?.25f:.08f,.32f);
            Mat("CivicBrass","#c0a05d",.78f,.48f);Mat("CivicStone","#c4c0b4",.05f,.28f);Mat("CivicTeal","#173a42",.48f,.5f);Mat("CivicFloor","#444a4b",.18f,.45f);
            root=new GameObject("CIVIC RENEWAL / distinct facades, usable places and government").transform;
            for(int i=0;i<UrbanCatalog.SiteCount;i++)DistinctLegacyFacade(i);
            var original=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/AfterlightExpansion"));
            var mobility=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/MobilityDistricts"));Physics.SyncTransforms();
            int serial=0;
            foreach(var lift in mobility.GetComponentsInChildren<MultiFloorLift>())
            {
                var building=lift.transform.parent;var at=building.position;
                if(building.name.Contains("walk-in"))DistinctInfill(building,serial++);
            }
            var candidates=new[]{new Vector3(1270,0,330),new Vector3(1770,0,20),new Vector3(1920,0,90),new Vector3(2040,0,150),new Vector3(840,0,870)};
            var chosen=candidates.FirstOrDefault(p=>!Physics.CheckBox(p+Vector3.up*10,new Vector3(96,8,70),Quaternion.identity,1,QueryTriggerInteraction.Ignore));
            if(chosen==Vector3.zero)chosen=new Vector3(1920,0,40);
            Object.DestroyImmediate(original);Object.DestroyImmediate(mobility);
            Government(chosen);WeaponStore(new Vector3(950,0,260));
            CombinePresentation();SaveGeneratedMeshes();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Output+"CivicRenewal.prefab");Object.DestroyImmediate(root.gameObject);AssetDatabase.SaveAssets();
            Debug.Log("CIVIC RENEWAL: government "+chosen+", 40 distinct original sites, "+serial+" inhabited building identities, armory and functional props.");
        }
        public static void CivicAndRelease(){BuildCivicRenewal();ProjectBuilder.BuildRelease();}
        static FacilityFunction Function(int kind)=>new[]{FacilityFunction.Home,FacilityFunction.Police,FacilityFunction.Fire,FacilityFunction.Bank,FacilityFunction.Clinic,FacilityFunction.School,FacilityFunction.Market,FacilityFunction.Market,FacilityFunction.Market,FacilityFunction.Cafe,FacilityFunction.Airport,FacilityFunction.Garage,FacilityFunction.Garage,FacilityFunction.Freight,FacilityFunction.Archive,FacilityFunction.Cafe}[Mathf.Clamp(kind,0,15)];
        static void Console(Transform p,Vector3 at,FacilityFunction function,string title)
        {
            var g=Group("Operating facility / "+title,at,p);var c=g.gameObject.AddComponent<FacilityConsole>();c.function=function;c.location=title;
            var point=g.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title=title+" · 서비스 단말";point.radius=2.8f;
            Box(g,"Brushed console pedestal",new Vector3(0,.45f,0),new Vector3(.64f,.9f,.5f),"Steel");Box(g,"Angled information terminal",new Vector3(0,1.16f,0),new Vector3(.9f,.5f,.1f),"WindowDark");
            for(int i=0;i<4;i++)Box(g,"Console interface line",new Vector3(-.07f,1.29f-i*.085f,-.059f),new Vector3(i%2==0?.6f:.43f,.025f,.01f),"NeonCyan",false);
            Text(g,"E  /  "+title,new Vector3(0,1.76f,0),.15f,"NeonWarm",180);
        }
        static void Prop(Transform p,Vector3 at,PropUse use)
        {
            var g=Group("Usable / "+use,at,p);g.gameObject.AddComponent<UsableProp>().use=use;
            var ip=g.gameObject.AddComponent<InteractionPoint>();ip.kind=InteractionKind.LifeService;ip.title=new[]{"벤치에서 쉬기","음료 자판기 / 25 C","정수기","ATM","긴급 신고 전화","TV 전원","보관함","기억 기록 읽기","의료 단말","배전반 점검","분리수거함","공구 작업대"}[(int)use];ip.radius=2.6f;
            if(use==PropUse.Bench){Model("painted_wooden_bench",g,Vector3.zero,1.1f,180);return;}
            if(use==PropUse.Water){Box(g,"Water cooler",Vector3.up*.52f,new Vector3(.55f,1.04f,.5f),"CivicStone");Box(g,"Water tank",Vector3.up*1.28f,new Vector3(.4f,.58f,.4f),"Glazing",false,PrimitiveType.Cylinder);return;}
            if(use==PropUse.Books){Shelf(g,Vector3.zero,false);return;}
            Box(g,use+" enclosure",new Vector3(0,.87f,0),new Vector3(1,1.74f,.7f),use==PropUse.Trash?"CargoBlue":"CivicTeal");Box(g,"Inset front panel",new Vector3(0,1.04f,-.361f),new Vector3(.81f,1.1f,.055f),"WindowDark");
            for(int i=0;i<5;i++)Box(g,"Machined ventilation fin",new Vector3(0,.2f+i*.055f,-.38f),new Vector3(.62f,.014f,.015f),"Aluminium",false);
            Box(g,"Illuminated service strip",new Vector3(0,1.72f,-.38f),new Vector3(.85f,.055f,.03f),"NeonCyan",false);
            if(use==PropUse.Vending)for(int row=0;row<3;row++)for(int col=0;col<4;col++)Box(g,"Drink can",new Vector3(-.3f+col*.2f,.66f+row*.3f,-.406f),new Vector3(.105f,.22f,.08f),(col+row)%2==0?"CargoGold":"CargoRed",false,PrimitiveType.Cylinder);
            else for(int i=0;i<4;i++)Box(g,"Display data",new Vector3(-.04f,1.38f-i*.14f,-.398f),new Vector3(.63f-i*.07f,.028f,.015f),"NeonCyan",false);
        }
        static void DistinctLegacyFacade(int id)
        {
            int kind=UrbanCatalog.Kind(id);var at=UrbanCatalog.Center(id);float h=kind==0?34:kind==6?24:kind==3?22:kind==12?8:12;
            var p=Group("Facade identity / "+id+" / "+UrbanCatalog.Name(id),at);string color="CivicFacade"+kind;
            int modules=3+id%4;
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<modules;i++)
                {
                    float z=-23+i*46f/Mathf.Max(1,modules-1);
                    Box(p,"Side facade rib",new Vector3(side*20.18f,h*.5f,z),new Vector3(.42f,h-.3f,1.2f+id%3*.3f),color,false);
                    if(kind==0||kind==8||kind==15)for(int floor=1;floor<h/4.3f;floor++)
                    {Box(p,"Balcony slab",new Vector3(side*21.3f,floor*4.3f,z),new Vector3(2.5f,.18f,4),"CivicStone",false);Box(p,"Balcony balustrade",new Vector3(side*22.5f,floor*4.3f+.6f,z),new Vector3(.1f,1.2f,4),"Glazing",false);}
                }
                Box(p,"Entry facade surround",new Vector3(side*15,4,-27.58f),new Vector3(8,7.7f,.65f),color,false);
                for(int slit=0;slit<4;slit++)Box(p,"Entry wall groove",new Vector3(side*15,1+slit*1.65f,-27.94f),new Vector3(7.3f,.075f,.08f),"CivicBrass",false);
            }
            if(kind==3||kind==14||kind==1)
                for(int i=0;i<6;i++){float x=-17+i*6.8f;if(Mathf.Abs(x)<4)continue;Beam(p,"Monumental entrance column",new Vector3(x,0,-29),new Vector3(x,8.5f,-29),.6f,"CivicStone",true);Box(p,"Capital",new Vector3(x,8.5f,-29),new Vector3(1.2f,.4f,1.2f),"CivicBrass",false);}
            else if(kind==9||kind==15||kind==7)
            {
                for(int i=0;i<12;i++)Box(p,"Striped fabric awning",new Vector3(-11+i*2,4.5f,-29),new Vector3(1.95f,.2f,4.6f),i%2==0?color:"CivicStone",false);
                for(int side=-1;side<=1;side+=2)CafeTable(p,new Vector3(side*14,0,-32));
                Beam(p,"Kitchen flue",new Vector3(18,6,20),new Vector3(18,h+4,20),1.4f,"Aluminium",false);
            }
            else if(kind==2||kind==13||kind==11)
                for(int bay=0;bay<2;bay++)for(int slat=0;slat<14;slat++)Box(p,"Garage roller slat",new Vector3(bay==0?-12:12,.4f+slat*.28f,-28.03f),new Vector3(8,.23f,.13f),"Aluminium",false);
            else
                for(int panel=0;panel<9;panel++){var b=Box(p,"Angled solar facade blade",new Vector3(-18+panel*4,h-1.7f,-27.8f),new Vector3(.3f,3.4f,1.2f),color,false);b.transform.localRotation=Quaternion.Euler(0,18+id%3*12,0);}
            if(kind==4){Box(p,"Medical cross horizontal",new Vector3(-15,h-1.7f,-28.2f),new Vector3(3,.75f,.16f),"NeonCyan",false);Box(p,"Medical cross vertical",new Vector3(-15,h-1.7f,-28.2f),new Vector3(.75f,3,.16f),"NeonCyan",false);}
            for(int i=0;i<2+id%3;i++){var panel=Box(p,"Roof solar bank",new Vector3(-10+i*7,h+.65f,-8+id%3*3),new Vector3(5,.15f,7),"WindowDark",false);panel.transform.localRotation=Quaternion.Euler(12+id%4*3,0,0);}
            Text(p,"DISTRICT "+(id/16+1)+"  /  "+(id+1).ToString("00"),new Vector3(15,1.9f,-28.2f),.21f,"NeonWarm",180);
            Console(p,new Vector3(7,0,-32),UrbanCatalog.IsGarage(id)?FacilityFunction.Garage:UrbanCatalog.IsBar(id)?FacilityFunction.Bar:UrbanCatalog.IsHotel(id)?FacilityFunction.Hotel:Function(kind),UrbanCatalog.Name(id));
            Prop(p,new Vector3(-7,0,-32),kind==3?PropUse.ATM:kind==4?PropUse.Medical:kind==5?PropUse.Water:kind==14?PropUse.Power:PropUse.Vending);
        }
        static void DistinctInfill(Transform source,int serial)
        {
            var p=Group("Inhabited identity / "+source.name,source.position);var lift=source.GetComponentInChildren<MultiFloorLift>();float height=lift.floors*4.2f;
            bool large=source.name.Contains("교정");float w=large?82:source.name.Contains("청사")?40:32,d=large?52:28;
            string color="CivicFacade"+(serial%16);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<5;i++)Box(p,"Profiled facade fins",new Vector3(side*(w*.5f+.22f),height*.5f,-d*.4f+i*d*.2f),new Vector3(.35f,height,1.0f+serial%3*.5f),color,false);
                for(int floor=0;floor<lift.floors;floor++)Box(p,"Contrasting facade cassette",new Vector3(side*(w*.5f-4),floor*4.2f+2,-d*.5f-.1f),new Vector3(6,3.5f,.2f),color,false);
                Planter(p,new Vector3(side*(w*.5f-4),0,-d*.5f-3));
            }
            var canopy=Box(p,"Individual entrance profile",new Vector3(0,3.9f,-d*.5f-2),new Vector3(10+serial%4*1.4f,.22f,4+serial%3),color,false);canopy.transform.localRotation=Quaternion.Euler(serial%2==0?0:5,0,0);
            for(int j=0;j<3+serial%3;j++)Box(p,"Roof garden planter",new Vector3(-8+j*4,height+.6f,7),new Vector3(2.6f,1,2.6f),color,false);
            FacilityFunction type=source.name.Contains("진료")?FacilityFunction.Clinic:source.name.Contains("CAFE")?FacilityFunction.Cafe:source.name.Contains("공방")||source.name.Contains("정비")?FacilityFunction.Garage:source.name.Contains("ARCHIVE")?FacilityFunction.Archive:source.name.Contains("공동")?FacilityFunction.Home:source.name.Contains("여객")?FacilityFunction.Ferry:source.name.Contains("병영")||source.name.Contains("기지")?FacilityFunction.Military:FacilityFunction.Office;
            for(int floor=0;floor<lift.floors;floor++)
            {
                float y=floor*4.2f;
                Console(p,new Vector3(-w*.5f+3,y,-d*.5f+2.7f),type,source.name.Split('/')[0]+" · "+(floor+1)+"층");
                Prop(p,new Vector3(-w*.5f+6,y,-d*.5f+2.5f),floor%2==0?PropUse.Water:PropUse.Vending);
                for(int i=0;i<5;i++)Box(p,"Interior timber acoustic slat",new Vector3(-w*.5f+.23f,y+1.9f,-5+i*2),new Vector3(.12f,3.2f,.25f),serial%2==0?"Wood":"CivicBrass",false);
                Box(p,"Stone floor border",new Vector3(-w*.5f+1,y+.025f,0),new Vector3(.5f,.02f,d-1),color,false);
            }
        }
        static void Government(Vector3 at)
        {
            var campus=Group("도시기억관리청 / government campus",at);campus.gameObject.AddComponent<GovernmentCampus>();
            Box(campus,"Government plaza",new Vector3(0,-.12f,-12),new Vector3(198,.24f,150),"Pavement");
            var building=OpenBuilding(campus,"도시기억관리청",Vector3.zero,116,64,6,1);
            GovernmentRooms(building);
            for(int side=-1;side<=1;side+=2)
            {
                var tower=Group("Archive data tower "+side,new Vector3(side*76,0,8),campus);
                Box(tower,"Archive vault tower",new Vector3(0,30,0),new Vector3(28,60,44),"CivicTeal");
                for(int f=0;f<15;f++){Box(tower,"Tower floor band",new Vector3(0,1.5f+f*4,0),new Vector3(28.6f,.25f,44.6f),"CivicStone",false);for(int i=0;i<6;i++)Box(tower,"Archive slit window",new Vector3(-11+i*4.4f,3+f*4,-22.1f),new Vector3(2.2f,1.7f,.12f),f%3==0?"NeonCyan":"WindowDark",false);}
                Box(tower,"Cantilever crown",new Vector3(0,61,0),new Vector3(31,2,47),"CivicBrass",false);
                for(int i=0;i<5;i++)Beam(campus,"Public colonnade",new Vector3(side*(10+i*9),0,-39),new Vector3(side*(10+i*9),9,-39),.9f,"CivicStone",true);
                for(int i=0;i<3;i++){Planter(campus,new Vector3(side*(20+i*17),0,-62));Prop(campus,new Vector3(side*(20+i*17),0,-56),PropUse.Bench);}
            }
            Box(campus,"Grand entrance entablature",new Vector3(0,9.2f,-39),new Vector3(110,.8f,10),"CivicBrass",false);
            Text(campus,"AFTERLIGHT  /  CIVIC MEMORY AUTHORITY",new Vector3(0,10.4f,-44.1f),.75f,"NeonWarm",180);
            for(int f=0;f<6;f++){Console(building,new Vector3(-28,f*4.2f,-20),f==0?FacilityFunction.Registry:f==1?FacilityFunction.Archive:f==2?FacilityFunction.Clinic:f==3?FacilityFunction.Military:FacilityFunction.Office,new[]{"시민 민원실","기억 기록공개실","의료복지과","국방대응부 연락실","도시기반시설과","중앙 복원 관제실"}[f]);Prop(building,new Vector3(25,f*4.2f,-20),f%2==0?PropUse.Books:PropUse.Television);}
            var fountain=Group("Memory prism fountain",new Vector3(0,0,-67),campus);Box(fountain,"Fountain basin",Vector3.up*.35f,new Vector3(11,.7f,7),"CivicStone");Box(fountain,"Reflecting water",Vector3.up*.74f,new Vector3(10,.02f,6),"Ocean",false);
            var prism=Box(fountain,"Suspended memory prism",Vector3.up*4,new Vector3(2.8f,5,2.8f),"Glazing",false);prism.transform.localRotation=Quaternion.Euler(20,45,15);Beam(fountain,"Memorial light",Vector3.up*.8f,Vector3.up*7,.18f,"NeonCyan",false);
            Console(campus,new Vector3(8,0,-47),FacilityFunction.Registry,"도시기억관리청 안내");
            Service(campus,13,new Vector3(0,1,-48));
            var road=ExpansionRoads.Nearest(at+Vector3.back*88,out _,out _);var path=new[]{road,Vector3.Lerp(road,at+Vector3.back*86,.5f),at+Vector3.back*86};Ribbon(root,"Government approach road",path,16,0,.07f,"Asphalt",true);
            for(int s=-1;s<=1;s+=2)Ribbon(root,"Government approach sidewalk",path,3,s*10,.12f,"Pavement",true);
            Parking(at+new Vector3(-76,.04f,-64),12,0,false);Parking(at+new Vector3(76,.04f,-64),12,0,false);
            Crowd(campus,"정부청사 시민 민원",new Vector3(0,.12f,-48),28,5,4,20);foreach(var c in building.GetComponentsInChildren<FacilityCrowd>()){c.arts=new[]{"OfficeMan","OfficeWoman","CivilianMan","CivilianWoman"};c.jobs=new[]{"공무원","기록 연구원","민원인","방문객"};}
        }
        static void WeaponStore(Vector3 at)
        {
            var p=OpenBuilding(root,"BLACKLINE / 무기 상점",at,32,28,2,3);
            foreach(var room in p.Cast<Transform>().Where(t=>t.name.StartsWith("Furnished room")&&t.localPosition.y<1).ToArray())Object.DestroyImmediate(room.gameObject);
            Box(p,"Armory checkout plinth",new Vector3(-3,.5f,-8),new Vector3(7,1,1.4f),"CivicTeal");
            Box(p,"Armory glass worktop",new Vector3(-3,1.08f,-8),new Vector3(7.3f,.14f,1.65f),"Steel");
            Box(p,"Counter neon ribbon",new Vector3(-3,.81f,-8.72f),new Vector3(6.7f,.08f,.035f),"NeonCyan",false);
            Desk(p,new Vector3(-11,0,-5),false);Prop(p,new Vector3(-13,0,1),PropUse.Workshop);
            Crowd(p,"무기 상점 직원",new Vector3(-4,.1f,-5),2,4,2,1.5f);
            for(int i=0;i<3;i++){Box(p,"Sealed ammunition crate",new Vector3(-11+i*4,.4f,3),new Vector3(2,.8f,1.4f),"Steel");for(int j=0;j<3;j++)Box(p,"Ammunition box",new Vector3(-11+i*4+(j-1)*.5f,.92f,3),new Vector3(.4f,.2f,.65f),"CargoGold",false);}
            var counter=Group("Blackline armory counter",new Vector3(0,1,-8),p);counter.gameObject.AddComponent<ArmoryCounter>();var ip=counter.gameObject.AddComponent<InteractionPoint>();ip.kind=InteractionKind.LifeService;ip.title="BLACKLINE · 무기 구매 / 탄약 보충";ip.radius=4;
            for(int i=0;i<7;i++)
            {
                float x=-12+i*3.3f;Box(p,"Weapon display backboard",new Vector3(x,1.8f,10),new Vector3(2.6f,2.6f,.22f),"CivicTeal");
                Box(p,"Weapon case glass",new Vector3(x,1.8f,9.45f),new Vector3(2.5f,2.5f,.05f),"Glazing",false);
                Text(p,new[]{"KATANA","GREATSWORD","PISTOL","RIFLE","GRENADE","LAUNCHER","SHOTGUN"}[i],new Vector3(x,3.4f,9.5f),.19f,"NeonWarm",180);
                if(i<2){Beam(p,"Displayed blade",new Vector3(x-.5f,1.1f,9.7f),new Vector3(x+.5f,2.8f,9.7f),i==0?.07f:.2f,"Aluminium");Beam(p,"Sword grip",new Vector3(x-.66f,.82f,9.7f),new Vector3(x-.48f,1.13f,9.7f),.13f,"Rubber");}
                else if(i==4){for(int n=0;n<3;n++)Box(p,"Grenade display",new Vector3(x+(n-1)*.5f,1.9f,9.7f),new Vector3(.25f,.4f,.25f),"CargoGold",false,PrimitiveType.Capsule);}
                else {Box(p,"Receiver",new Vector3(x,2,9.7f),new Vector3(i==2?.65f:1.2f,.22f,.18f),"Steel",false);Beam(p,"Barrel",new Vector3(x+.3f,2,9.7f),new Vector3(x+(i==2?.65f:1),2,9.7f),i==5?.24f:.08f,"Aluminium");Box(p,"Pistol grip",new Vector3(x-.2f,1.76f,9.7f),new Vector3(.16f,.4f,.17f),"Rubber",false);}
            }
            Text(p,"BLACKLINE  /  ARMS & SUPPLY",new Vector3(0,5,-14.7f),.5f,"NeonRose",180);
            Console(p,new Vector3(-10,0,-10),FacilityFunction.Market,"보급품 카운터");
        }
        static void GovernmentRooms(Transform building)
        {
            foreach(var room in building.Cast<Transform>().Where(t=>t.name.StartsWith("Furnished room")).ToArray())Object.DestroyImmediate(room.gameObject);
            // The first three levels share a genuine atrium; galleries keep solid floors and guardrails.
            foreach(var floor in building.Cast<Transform>().Where(t=>t.name=="Main upper floor"&&t.localPosition.y<9).ToArray())
            {
                float y=floor.localPosition.y;Object.DestroyImmediate(floor.gameObject);
                Box(building,"Atrium west gallery",new Vector3(-35,y,0),new Vector3(46,.24f,64),"TerminalFloor");
                Box(building,"Atrium east gallery",new Vector3(29,y,0),new Vector3(34,.24f,64),"TerminalFloor");
                Box(building,"Atrium front bridge",new Vector3(0,y,-24),new Vector3(24,.24f,16),"TerminalFloor");
                Box(building,"Atrium rear bridge",new Vector3(0,y,20),new Vector3(24,.24f,24),"TerminalFloor");
                for(int side=-1;side<=1;side+=2)
                {
                    Box(building,"Atrium glass guard",new Vector3(side*12,y+.77f,-4),new Vector3(.075f,1.5f,24),"Glazing");
                    Beam(building,"Atrium handrail",new Vector3(side*12,y+1.58f,-16),new Vector3(side*12,y+1.58f,8),.065f,"CivicBrass");
                }
                foreach(float z in new[]{-16f,8f}){Box(building,"Bridge glass guard",new Vector3(0,y+.77f,z),new Vector3(24,1.5f,.075f),"Glazing");Beam(building,"Bridge handrail",new Vector3(-12,y+1.58f,z),new Vector3(12,y+1.58f,z),.065f,"CivicBrass");}
            }
            foreach(var light in building.Cast<Transform>().Where(t=>t.name=="Suspended linear light"&&t.localPosition.y<12).ToArray())Object.DestroyImmediate(light.gameObject);
            string[] departments={"시민등록 · 민원접수","기억 기록 · 자료복원","의료복지 · 상담","국방대응부 · 연락","기반시설 · 전력관리","중앙복원 · 관제"};
            FacilityFunction[] functions={FacilityFunction.Registry,FacilityFunction.Archive,FacilityFunction.Clinic,FacilityFunction.Military,FacilityFunction.Power,FacilityFunction.Office};
            for(int f=0;f<6;f++)
            {
                float y=f*4.2f;
                for(int s=0;s<2;s++)for(int row=0;row<2;row++)
                {
                    var room=Group(departments[f]+" / "+(s*2+row+1),new Vector3(s==0?-35:30,y,row==0?-17:11),building);room.localRotation=Quaternion.Euler(0,s==0?-90:90,0);
                    for(int side=-1;side<=1;side+=2)
                    {
                        Box(room,"Department side partition",new Vector3(side*11,1.65f,0),new Vector3(.15f,3.3f,20),"CivicStone");
                        Box(room,"Department glass front",new Vector3(side*6.6f,1.8f,-10),new Vector3(8.8f,3.6f,.12f),"Glazing");
                        Box(room,"Door brass jamb",new Vector3(side*2.2f,1.8f,-10),new Vector3(.08f,3.6f,.18f),"CivicBrass",false);
                    }
                    Box(room,"Department rear partition",new Vector3(0,1.65f,10),new Vector3(22,3.3f,.16f),"CivicTeal");
                    Box(room,"Public department carpet",new Vector3(0,.012f,0),new Vector3(20,.024f,18),f%2==0?"CivicFacade6":"CivicFacade11",false);
                    Text(room,(f+1)+"F / "+departments[f]+" "+(s*2+row+1),new Vector3(0,3.5f,-10.2f),.22f,"NeonWarm",180);
                    Console(room,new Vector3(0,0,-8),functions[f],departments[f]);
                    if(f==2){Bed(room,new Vector3(-6,0,4),false);Desk(room,new Vector3(5,0,4),false);Prop(room,new Vector3(-7,0,-5),PropUse.Medical);}
                    else if(f==1){for(int shelf=0;shelf<3;shelf++)Shelf(room,new Vector3((shelf-1)*5.5f,0,6),false);Desk(room,new Vector3(0,0,0),false);Prop(room,new Vector3(-7,0,-5),PropUse.Books);}
                    else if(f>=3){for(int desk=0;desk<4;desk++)Desk(room,new Vector3((desk%2-.5f)*10,0,desk/2*7),true);for(int i=0;i<3;i++){Box(room,"Control room status screen",new Vector3((i-1)*5,2.1f,9.86f),new Vector3(4,1.65f,.05f),"WindowDark",false);for(int line=0;line<5;line++)Box(room,"Live telemetry",new Vector3((i-1)*5,1.55f+line*.24f,9.82f),new Vector3(3.4f-line*.22f,.03f,.025f),"NeonCyan",false);}}
                    else{for(int desk=0;desk<3;desk++)Desk(room,new Vector3((desk-1)*6,0,4),false);for(int chair=0;chair<6;chair++)Chair(room,new Vector3((chair%3-1)*1.6f,0,-3+chair/3*1.8f));Prop(room,new Vector3(-7,0,-5),PropUse.Water);}
                    Planter(room,new Vector3(8,0,-7));
                    Box(room,"Department ceiling luminaire",new Vector3(0,3.7f,0),new Vector3(16,.05f,.25f),"NeonWarm",false);Lamp(room,new Vector3(0,3.4f,0),new Color(.76f,.9f,1),20,16);
                    Crowd(room,departments[f],new Vector3(0,.1f,-6),3,f==2?23:5,2,2);
                }
                if(f<3)for(int side=-1;side<=1;side+=2)Box(building,"Atrium gallery edge light",new Vector3(side*12,y+1.2f,-4),new Vector3(.04f,.04f,24),"NeonCyan",false);
            }
            Box(building,"Reception desk base",new Vector3(0,.55f,4),new Vector3(12,1.1f,1.8f),"CivicTeal");Box(building,"Reception stone worktop",new Vector3(0,1.14f,4),new Vector3(12.3f,.15f,2),"CivicStone");
            Console(building,new Vector3(0,0,2),FacilityFunction.Registry,"청사 종합 안내");
            for(int i=0;i<4;i++){Box(building,"Lobby overhead pendant",new Vector3((i-1.5f)*4,9.7f,-4),new Vector3(.1f,3.3f,.1f),"NeonWarm",false);Beam(building,"Pendant suspension",new Vector3((i-1.5f)*4,11.4f,-4),new Vector3((i-1.5f)*4,12.4f,-4),.025f,"Steel",false);}
            for(int s=-1;s<=1;s+=2){Prop(building,new Vector3(s*8,0,-8),PropUse.Bench);Planter(building,new Vector3(s*9,0,-14));Box(building,"Lobby guidance inlay",new Vector3(s*10,.026f,-8),new Vector3(.12f,.025f,40),"CivicBrass",false);}
            Text(building,"기억을 지키는 도시 / AFTERLIGHT",new Vector3(0,8,8),.42f,"NeonCyan",180);
        }
    }
}
