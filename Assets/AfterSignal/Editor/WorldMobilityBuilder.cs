using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        [MenuItem("AFTERSIGNAL/World/Build transport, ocean and inhabited districts")]
        public static void BuildMobility()
        {
            AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            materials.Clear();boxes.Clear();meshId=106000;ImportMaterials();MakeMaterials();
            root=new GameObject("MOBILITY / inhabited outer districts").transform;
            ConnectorRoads();BuildSeabed();MilitaryBase();PrisonCampus();PassengerPier();InfillBlocks();
            CombinePresentation();SaveGeneratedMeshes();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Output+"MobilityDistricts.prefab");Object.DestroyImmediate(root.gameObject);
            OpenLegacyConnectors();UpgradeExistingCraft();AssetDatabase.SaveAssets();Debug.Log("MOBILITY: roads, 3-floor buildings, parking, ocean, military base and prison saved.");
        }
        public static void MobilityAndRelease(){BuildMobility();ProjectBuilder.BuildRelease();}
        static void ConnectorRoads()
        {
            var p=Group("Continuous city connector roads",Vector3.zero);
            for(int r=9;r<ExpansionRoads.Roads.Count;r++)
            {
                var path=ExpansionRoads.Roads[r];var road=Group("Connector route "+r,Vector3.zero,p);
                Ribbon(road,"Connector asphalt",path,22,0,.06f,"Asphalt",true);
                for(int s=-1;s<=1;s+=2){Ribbon(road,"Connector sidewalk",path,5.8f,s*14,.10f,"Pavement",true);Ribbon(road,"White edge line",path,.14f,s*9.5f,.075f,"PaintWhite");}
                for(int i=1;i<path.Length-1;i++)
                {
                    Vector3 d=(path[i+1]-path[i-1]).normalized;
                    if(i%2==0){var stripe=Box(road,"Centre road dash",path[i]+Vector3.up*.08f,new Vector3(.17f,.015f,4),"PaintAmber",false);stripe.transform.rotation=Quaternion.LookRotation(d);}
                    if(i%9==0)for(int side=-1;side<=1;side+=2)Model("street_lamp_01",road,path[i]+Vector3.Cross(Vector3.up,d)*side*14.5f,1.65f,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+side*90);
                }
            }
            for(int site=0;site<UrbanCatalog.SiteCount;site+=2)
            {
                var at=UrbanCatalog.Center(site)+new Vector3(0,.04f,-42);Parking(at,6,0,false);
            }
        }
        static void Parking(Vector3 at,int count,float yaw,bool roadside)
        {
            var p=Group(roadside?"Curbside parking":"Public parking lot",at);p.localRotation=Quaternion.Euler(0,yaw,0);
            float spacing=roadside?7:3.3f;
            Box(p,"Parking asphalt",new Vector3(0,-.065f,count>6?7:0),new Vector3(spacing*6+2,.12f,count>6?25:11),"Asphalt");
            var lot=p.gameObject.AddComponent<ParkingLot>();lot.spaces=count;lot.spacing=spacing;lot.roadside=roadside;
            for(int i=0;i<count;i++)
            {
                float x=(i%6-2.5f)*spacing,z=i/6*14;
                Box(p,"Bay divider",new Vector3(x-spacing*.5f,.007f,z),new Vector3(.09f,.015f,7),"PaintWhite",false);
                Box(p,"Wheel stop",new Vector3(x,.06f,z+3),new Vector3(2.1f,.12f,.18f),"Pavement");
                if(i%3==0)Text(p,(i+1).ToString("00"),new Vector3(x,.02f,z-2),.25f,"PaintWhite").localRotation=Quaternion.Euler(90,0,0);
            }
            Text(p,"P  /  PUBLIC PARKING",new Vector3(-spacing*3,2,-4),.24f,"NeonCyan",180);
        }
        static void BuildSeabed()
        {
            var p=Group("Ocean / reef and continental shelf",Vector3.zero);
            var v=new List<Vector3>();var tris=new List<int>();const int cols=110,rows=84;
            for(int x=0;x<=cols;x++)for(int z=0;z<=rows;z++){float xx=x*20,zz=Mathf.Lerp(OceanLife.Shore(xx)-4,-2190,z/(float)rows);v.Add(new Vector3(xx,OceanLife.Bed(xx,zz),zz));}
            for(int x=0;x<cols;x++)for(int z=0;z<rows;z++){int k=x*(rows+1)+z;tris.AddRange(new[]{k,k+rows+1,k+1,k+1,k+rows+1,k+rows+2});}
            MeshObject(p,"Swimmable seabed collision",v.ToArray(),tris.ToArray(),"Sand",true);
            var rng=new System.Random(519);
            for(int i=0;i<115;i++)
            {
                float x=160+(float)rng.NextDouble()*1820,z=OceanLife.Shore(x)-50-(float)rng.NextDouble()*280;var at=new Vector3(x,OceanLife.Bed(x,z),z);
                var reef=Group("Reef / coral and seagrass "+i,at,p);
                Box(reef,"Weathered reef boulder",new Vector3(0,.9f,0),new Vector3(3+i%3,2.1f,2.8f),"Pavement",true,PrimitiveType.Sphere);
                for(int j=0;j<7;j++)
                {
                    var a=new Vector3((j%3-1)*1.6f,0,(j/3-1)*1.4f);var tip=a+new Vector3(.2f,1+j%3*.55f,.3f);
                    Beam(reef,"Coral stem",a,tip,.1f,i%2==0?"CargoRed":"CargoGold");
                    for(int k=-1;k<=1;k+=2)Beam(reef,"Coral fan branch",a+Vector3.up*.6f,tip+new Vector3(k*.6f,.3f,0),.07f,i%2==0?"CargoRed":"CargoGold");
                    Beam(reef,"Seagrass blade",a+Vector3.right*2,a+new Vector3(2.3f,1.8f,0),.045f,"Leaf");
                }
            }
        }
        static void PassengerPier()
        {
            var p=Group("Bluewater passenger terminal",new Vector3(1250,0,-652));
            Box(p,"Ferry quay foundation",new Vector3(0,-1.5f,-10),new Vector3(120,3,62),"Pavement");
            OpenBuilding(p,"블루워터 여객터미널",new Vector3(0,0,0),30,22,2,4);
            for(int i=0;i<2;i++)
            {
                float x=i==0?-50:30;
                Box(p,"Boarding pier",new Vector3(x,.28f,-43),new Vector3(8,.5f,46),"Wood");
                for(int z=0;z<7;z++)for(int s=-1;s<=1;s+=2)Beam(p,"Pier pile",new Vector3(x+s*3.5f,-7,-22-z*6),new Vector3(x+s*3.5f,.6f,-22-z*6),.28f,"Steel",true);
                Text(p,i==0?"RENTAL / E 조종":"FERRY / G 승선",new Vector3(x,2.5f,-25),.25f,"NeonCyan",180);
            }
            Service(p,12,new Vector3(0,1,-21));Crowd(p,"여객선 부두",new Vector3(-10,.16f,-18),18,8,4,10);
        }
        static void Fence(Transform p,float w,float d,string title)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,title+" perimeter wall",new Vector3(side*w*.5f,2.5f,0),new Vector3(.5f,5,d),"Pavement");
                Box(p,title+" rear wall",new Vector3(side*(w/4+7),2.5f,d*.5f),new Vector3(w/2-14,5,.5f),"Pavement");
                Box(p,title+" entry wall",new Vector3(side*(w/4+7),2.5f,-d*.5f),new Vector3(w/2-14,5,.5f),"Pavement");
                for(int i=0;i<(int)(d/5);i++)Beam(p,"Perimeter anti-climb post",new Vector3(side*w*.5f,5,-d*.5f+i*5),new Vector3(side*(w*.5f+.3f),6,-d*.5f+i*5),.06f,"Steel");
            }
        }
        static void MilitaryBase()
        {
            var p=Group("LUMEN DEFENCE / air and armor garrison",new Vector3(420,0,835));
            Box(p,"Military base pavement",new Vector3(0,-.04f,10),new Vector3(480,.1f,250),"Pavement");Fence(p,470,240,"Military");
            OpenBuilding(p,"기지 본부 / 작전통제실",new Vector3(155,0,-65),50,30,3,5);
            OpenBuilding(p,"생활관 / 병영식당",new Vector3(155,0,23),48,34,3,6);
            OpenBuilding(p,"항공 정비동",new Vector3(-25,0,10),55,40,2,3);
            Box(p,"Military runway",new Vector3(-70,.035f,65),new Vector3(300,.08f,34),"Asphalt");
            for(int i=0;i<20;i++){Box(p,"Runway centre mark",new Vector3(-210+i*14,.085f,65),new Vector3(7,.02f,.35f),"PaintWhite",false);for(int s=-1;s<=1;s+=2)Box(p,"Runway edge lamp",new Vector3(-210+i*14,.14f,65+s*16),new Vector3(.25f,.15f,.25f),"NeonCyan",false);}
            Box(p,"Helipad",new Vector3(15,.04f,7),new Vector3(26,.1f,26),"Asphalt");
            Text(p,"H",new Vector3(15,.11f,7),3,"PaintWhite").localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<3;i++)
            {
                var h=Group("Armored vehicle maintenance bay",new Vector3(-165+i*50,0,-76),p);
                Box(h,"Hangar roof",new Vector3(0,9,0),new Vector3(38,.4f,28),"Steel");Box(h,"Hangar rear",new Vector3(0,4.5f,14),new Vector3(38,9,.3f),"Cladding");
                for(int s=-1;s<=1;s+=2)Box(h,"Hangar side",new Vector3(s*19,4.5f,0),new Vector3(.3f,9,28),"Cladding");
                for(int j=0;j<4;j++){Box(h,"Tool chest",new Vector3(-12+j*8,.65f,10),new Vector3(3,1.3f,1.4f),"CargoRed");for(int k=0;k<4;k++)Box(h,"Tool drawer handle",new Vector3(-12+j*8,.4f+k*.24f,9.27f),new Vector3(2.3f,.04f,.08f),"Aluminium",false);}
            }
            Text(p,"LUMEN DEFENCE COMMAND",new Vector3(0,7,-121),.9f,"NeonCyan",180);Service(p,10,new Vector3(0,1,-111));
            Crowd(p,"군부대 정비병",new Vector3(-100,.1f,-55),28,20,4,25);Crowd(p,"기지 지상 조업",new Vector3(100,.1f,-45),24,0,4,24);
            Parking(new Vector3(542,.05f,735),12,0,false);
        }
        static void PrisonCampus()
        {
            var p=Group("AFTERLIGHT CORRECTIONAL / custody and visitation",new Vector3(1180,0,825));
            Box(p,"Prison campus foundation",new Vector3(0,-.04f,0),new Vector3(196,.1f,172),"Pavement");Fence(p,190,166,"Prison");
            var building=OpenBuilding(p,"교정동 / 접견실 / 수용동",new Vector3(0,0,-20),82,52,3,7);
            for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)
            {
                var t=Group("Guard watchtower",new Vector3(s*87,0,z*74),p);Box(t,"Watchtower base",new Vector3(0,5,0),new Vector3(5,10,5),"Pavement");Box(t,"Guard booth",new Vector3(0,11,0),new Vector3(8,3,8),"Glazing");Box(t,"Tower roof",new Vector3(0,12.7f,0),new Vector3(9,.3f,9),"Steel");
            }
            for(int c=0;c<6;c++)
            {
                var cell=Group("Prison cell "+c,new Vector3(-24+c*8,0,-5),building);
                Box(cell,"Cell rear",new Vector3(0,1.8f,3),new Vector3(6,3.6f,.2f),"Pavement");
                for(int s=-1;s<=1;s+=2)Box(cell,"Cell side wall",new Vector3(s*3,1.8f,0),new Vector3(.2f,3.6f,6),"Pavement");
                for(int i=0;i<13;i++)Beam(cell,"Cell front bars",new Vector3(-3+i*.5f,0,-3),new Vector3(-3+i*.5f,3.6f,-3),.055f,"Steel",true);
                Bed(cell,new Vector3(-1,0,1),false);Box(cell,"Sanitary basin",new Vector3(2,.5f,1.8f),new Vector3(.55f,1,.65f),"Cladding",true,PrimitiveType.Sphere);
                Crowd(cell,"수감자",new Vector3(.5f,.1f,0),c==0?0:1,4+c%4,1,.7f);
            }
            Service(building,11,new Vector3(-24,1,-5));
            var yard=Group("Exercise yard",new Vector3(0,0,52),p);Box(yard,"Sports court",new Vector3(0,.01f,0),new Vector3(90,.04f,30),"CargoBlue");
            for(int i=0;i<6;i++)Model("painted_wooden_bench",yard,new Vector3(-38+i*15,0,-19),1.1f,0);
            Crowd(yard,"교도소 운동장",new Vector3(-16,.1f,0),22,4,4,16);Crowd(p,"교정 직원",new Vector3(-57,.1f,-45),12,3,1,12);
            Text(p,"AFTERLIGHT CORRECTIONAL",new Vector3(0,7,-83.4f),.7f,"NeonWarm",180);Service(p,11,new Vector3(0,1,-83));
        }
        static void InfillBlocks()
        {
            var old=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"AfterlightExpansion.prefab");var obstacles=old?Object.Instantiate(old):null;
            Physics.SyncTransforms();int serial=0;
            int[] routes={9,10,11,12,13,14,1,4,6,8};
            foreach(int r in routes)
            {
                var path=ExpansionRoads.Roads[r];
                for(int i=5;i<path.Length-4;i+=9)
                {
                    if(serial>=24)break;var d=(path[i+1]-path[i-1]).normalized;
                    for(int s=-1;s<=1;s+=2)
                    {
                        if(serial>=24)break;
                        var at=path[i]+Vector3.Cross(Vector3.up,d)*s*42;at.y=0;
                        if(!ExpansionRoads.Outside(at)||at.x<80||at.x>2120||at.z<OceanLife.Shore(at.x)+45||at.z>690||at.x<880&&Mathf.Abs(at.z)<425)continue;
                        if(Physics.CheckBox(at+Vector3.up*10,new Vector3(22,9.5f,20),Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                        float near=(ExpansionRoads.Nearest(at,out _,out _)-at).magnitude;if(near<36)continue;
                        string[] titles={"오로라 공동주택","LUMEN WORKS / 사무동","COAST CAFE / 베이커리","정비 공방 / 부품상가","해양 연구 진료소","CITY ARCHIVE / 자료관"};
                        var b=OpenBuilding(root,titles[serial%6],at,32,28,2+serial%3,serial%6);
                        // Each block has a public frontage and a service alley.
                        Box(root,"Neighborhood forecourt",at+new Vector3(0,-.04f,-20),new Vector3(42,.1f,12),"Pavement");
                        Parking(at+new Vector3(0,.04f,-29),6,0,false);
                        Crowd(b,titles[serial%6],new Vector3(-6,.12f,-5),8,serial%24,3,5);foreach(var seed in b.GetComponentsInChildren<FacilityCrowd>())ApplyRoomRoles(seed,serial%6==5?1:serial%6);
                        serial++;Physics.SyncTransforms();
                    }
                }
            }
            if(obstacles)Object.DestroyImmediate(obstacles);
            Debug.Log("MOBILITY INFILL BUILDINGS: "+serial);
        }
        static Transform OpenBuilding(Transform parent,string title,Vector3 at,float w,float d,int floors,int theme)
        {
            var p=Group(title+" / walk-in multistorey",at,parent);float h=floors*4.2f;
            Box(p,"Full ground foundation",new Vector3(0,-.14f,0),new Vector3(w,.28f,d),"TerminalFloor");
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,"Structural side return",new Vector3(side*w*.5f,h*.5f,0),new Vector3(.3f,h,d),"Cladding");
                WindowWall(p,new Vector3(side*(w*.25f+1.7f),1.95f,-d*.5f),w*.5f-3.4f,3.9f);
                Beam(p,"Entrance jamb",new Vector3(side*3.4f,0,-d*.5f-.03f),new Vector3(side*3.4f,3.8f,-d*.5f-.03f),.13f,"Steel");
            }
            Box(p,"Rear structural wall",new Vector3(0,h*.5f,d*.5f),new Vector3(w,h,.3f),"Cladding");
            var shaft=Group("Lift machinery",new Vector3(w*.5f-2.65f,0,d*.5f-6),p);var lift=shaft.gameObject.AddComponent<MultiFloorLift>();lift.floors=floors;
            var platform=Group("Lift cabin and platform",Vector3.zero,shaft);lift.platform=platform;
            Box(platform,"Elevator platform",new Vector3(0,-.11f,0),new Vector3(3.7f,.22f,4),"Steel");
            for(int side=-1;side<=1;side+=2)Box(platform,"Elevator side",new Vector3(side*1.85f,1.35f,0),new Vector3(.08f,2.7f,4),"Glazing");
            Box(platform,"Lift ceiling",new Vector3(0,2.8f,0),new Vector3(3.8f,.1f,4),"Steel");
            LiftButton(platform,Vector3.up*1.15f,lift,0,"E · 승강기 층 선택");
            for(int level=0;level<floors;level++)
            {
                float y=level*4.2f;
                if(level>0)
                {
                    Box(p,"Main upper floor",new Vector3(-6,y-.12f,0),new Vector3(w-12,.24f,d),"TerminalFloor");
                    Box(p,"Front upper landing",new Vector3(w*.5f-6,y-.12f,-d*.25f),new Vector3(12,.24f,d*.5f),"TerminalFloor");
                    Box(p,"Rear upper landing",new Vector3(w*.5f-6,y-.12f,d*.5f-2),new Vector3(12,.24f,4),"TerminalFloor");
                }
                Box(p,"Front facade floor belt",new Vector3(0,y+3.9f,-d*.5f),new Vector3(w+.6f,.3f,.8f),"Steel");
                if(level>0)WindowWall(p,new Vector3(0,y+1.9f,-d*.5f),w-.5f,3.7f);
                for(int column=0;column<Mathf.FloorToInt(w/6);column++)Box(p,"Facade vertical mullion",new Vector3(-w*.5f+column*6,y+2,-d*.5f-.06f),new Vector3(.11f,4,.14f),"Steel",false);
                Text(p,(level+1)+"F  /  "+title,new Vector3(-6,y+3.1f,0),.19f,"NeonCyan",180);
                LiftButton(p,new Vector3(w*.5f-2.65f,y+1.2f,d*.5f-2),lift,level,"승강기 호출 · "+(level+1)+"층");
                if(level<floors-1)
                {
                    float run=d*.5f-4;
                    for(int step=0;step<24;step++)Box(p,"Stair tread",new Vector3(w*.5f-8,y+(step+1)*4.2f/24*.5f,(step+.5f)*run/24),new Vector3(3.1f,(step+1)*4.2f/24,run/24+.012f),"Pavement",false);
                    var ramp=Box(p,"Continuous stair collision ramp",new Vector3(w*.5f-8,y+2.1f-.1f,run*.5f),new Vector3(3.1f,.2f,Mathf.Sqrt(run*run+4.2f*4.2f)),"Pavement");
                    ramp.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(4.2f,run)*Mathf.Rad2Deg,0,0);ramp.GetComponent<MeshRenderer>().enabled=false;
                    for(int side=-1;side<=1;side+=2)Beam(p,"Stair handrail",new Vector3(w*.5f-8+side*1.55f,y+1,0),new Vector3(w*.5f-8+side*1.55f,y+5.2f,run),.06f,"Aluminium");
                }
                Furnish(p,new Vector3(-6,y,-1),w-15,d-5,theme,level);
                Lamp(p,new Vector3(-7,y+3.3f,-3),new Color(.79f,.88f,1),16,18);
                Box(p,"Suspended linear light",new Vector3(-6,y+3.65f,-2),new Vector3(w*.4f,.08f,.3f),"NeonWarm",false);
            }
            Box(p,"Roof slab",new Vector3(0,h+.13f,0),new Vector3(w+.5f,.26f,d+.5f),"Steel");
            for(int side=-1;side<=1;side+=2)Box(p,"Roof parapet",new Vector3(side*w*.5f,h+.65f,0),new Vector3(.22f,1,d),"Cladding");
            Text(p,title,new Vector3(0,4.4f,-d*.5f-.5f),.36f,"NeonCyan",180);
            Box(p,"Entry canopy",new Vector3(0,3.7f,-d*.5f-1.4f),new Vector3(9,.15f,3.2f),"Steel");
            for(int side=-1;side<=1;side+=2){Model("painted_wooden_bench",p,new Vector3(side*(w*.5f-5),0,-d*.5f-2),1.15f,180);Planter(p,new Vector3(side*6,0,-d*.5f-2));}
            foreach(var population in p.GetComponentsInChildren<FacilityCrowd>())ApplyRoomRoles(population,theme);
            return p;
        }
        static void ApplyRoomRoles(FacilityCrowd seed,int theme)
        {
            seed.arts=theme==4?new[]{"Doctor","Nurse","PatientMan","PatientWoman"}:theme==5?new[]{"Soldier","OfficeMan","OfficeWoman"}:theme==6?new[]{"Soldier","Worker"}:theme==7?new[]{"Prisoner"}:theme==2?new[]{"Bartender","CivilianMan","CivilianWoman"}:theme==3?new[]{"Worker","OfficeMan"}:theme==1?new[]{"OfficeMan","OfficeWoman"}:new[]{"CivilianMan","CivilianWoman","ElderMan","ElderWoman"};
            seed.jobs=theme==4?new[]{"의사","간호사","환자","환자"}:theme==5?new[]{"기지 경계병","작전 장교","신호 분석관"}:theme==6?new[]{"기지 병사","정비병"}:theme==7?new[]{"수감자"}:theme==2?new[]{"바리스타","손님","손님"}:theme==3?new[]{"정비사","물류 담당자"}:theme==1?new[]{"회사원"}:new[]{"주민"};
        }
        static void LiftButton(Transform p,Vector3 at,MultiFloorLift lift,int level,string title)
        {
            var go=Group(title,at,p);var point=go.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.Lift;point.title=title;point.radius=2.8f;
            var button=go.gameObject.AddComponent<FloorAccess>();button.lift=lift;button.floor=level;
            Box(go,"Lift control panel",new Vector3(1.5f,0,0),new Vector3(.16f,.65f,.3f),"Steel",false);
        }
        static void Furnish(Transform p,Vector3 at,float w,float d,int theme,int floor)
        {
            var room=Group("Furnished room / "+theme+" / "+floor,at,p);
            for(int side=-1;side<=1;side+=2)
            {
                Box(room,"Room skirting",new Vector3(side*w*.5f,.12f,0),new Vector3(.08f,.24f,d),"Wood",false);
                Planter(room,new Vector3(side*(w*.5f-1),0,-d*.5f+1));
            }
            if(theme==0||theme==6)
            {
                for(int i=0;i<2;i++)Bed(room,new Vector3(-w*.25f+i*w*.5f,0,d*.25f),theme==6);
                Counter(room,new Vector3(-w*.25f,0,-d*.28f),true);
                Sofa(room,new Vector3(w*.24f,0,-d*.23f));
                Box(room,"Television wall screen",new Vector3(w*.24f,1.9f,0),new Vector3(2.4f,1.4f,.09f),"WindowDark");
                Box(room,"Media console",new Vector3(w*.24f,.35f,.2f),new Vector3(2.8f,.7f,.65f),"Wood");
            }
            else if(theme==2)
            {
                Counter(room,new Vector3(0,0,d*.3f),true);
                for(int i=0;i<6;i++)CafeTable(room,new Vector3((i%3-1)*Mathf.Min(5,w*.28f),0,-d*.26f+i/3*5));
            }
            else if(theme==4)
            {
                for(int i=0;i<4;i++)
                {
                    var a=new Vector3((i%2-.5f)*w*.5f,0,-d*.25f+i/2*d*.55f);Bed(room,a,false);
                    Box(room,"Patient monitor arm",a+new Vector3(1.5f,1.2f,1.2f),new Vector3(.08f,2.4f,.08f),"Aluminium");
                    Box(room,"Vitals monitor",a+new Vector3(1.5f,1.65f,1.2f),new Vector3(.7f,.48f,.14f),"WindowDark");
                    for(int j=0;j<4;j++)Box(room,"Monitor graph",a+new Vector3(1.5f,1.53f+j*.07f,1.115f),new Vector3(.5f,.025f,.015f),"NeonCyan",false);
                    Beam(room,"Privacy curtain rail",a+new Vector3(-1.8f,3,1.5f),a+new Vector3(1.8f,3,1.5f),.04f,"Aluminium");
                }
            }
            else if(theme==3)
            {
                for(int i=0;i<3;i++)
                {
                    var a=new Vector3((i-1)*w*.29f,0,d*.22f);Shelf(room,a,true);
                    Box(room,"Workshop bench",a+Vector3.back*d*.45f+Vector3.up*.92f,new Vector3(3,.16f,1.1f),"Wood");
                    for(int j=0;j<4;j++)Box(room,"Tool case",a+new Vector3(-1+j*.7f,1.1f,-d*.45f),new Vector3(.5f,.25f,.7f),j%2==0?"CargoGold":"CargoRed");
                }
            }
            else if(theme!=7)
            {
                for(int i=0;i<6;i++)Desk(room,new Vector3((i%3-1)*Mathf.Min(5,w*.28f),0,(i/3-.5f)*7),theme==5);
                Shelf(room,new Vector3(0,0,d*.4f),false);
            }
            if(theme!=7)Crowd(room,"실내 상주 직원",new Vector3(0,.1f,-d*.4f),3,theme==4?23:theme==5?3:4,theme==4||theme==5?1:4,2.5f);
        }
        static void Planter(Transform p,Vector3 at)
        {Box(p,"Ceramic planter",at+Vector3.up*.35f,new Vector3(.7f,.7f,.7f),"Cladding",true,PrimitiveType.Cylinder);for(int i=0;i<5;i++){float a=i*1.256f;var tip=at+new Vector3(Mathf.Sin(a)*.45f,1.6f,Mathf.Cos(a)*.45f);Beam(p,"Indoor plant stem",at+Vector3.up*.4f,tip,.018f,"Leaf");Box(p,"Indoor plant leaf",tip,new Vector3(.35f,.09f,.5f),"Leaf",false,PrimitiveType.Sphere);}}
        static void Bed(Transform p,Vector3 at,bool bunk)
        {
            for(int n=0;n<(bunk?2:1);n++){float h=n*1.35f;Box(p,"Bed frame",at+Vector3.up*(h+.45f),new Vector3(1.55f,.16f,2.65f),"Aluminium");Box(p,"Mattress",at+Vector3.up*(h+.65f),new Vector3(1.48f,.26f,2.55f),"Cladding");Box(p,"Quilt",at+new Vector3(0,h+.82f,-.35f),new Vector3(1.5f,.08f,1.65f),"CargoBlue");Box(p,"Pillow",at+new Vector3(0,h+.85f,.91f),new Vector3(.95f,.22f,.45f),"Cladding",false,PrimitiveType.Sphere);}
            for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)Beam(p,"Bed post",at+new Vector3(s*.72f,0,z*1.25f),at+new Vector3(s*.72f,bunk?2.3f:.55f,z*1.25f),.05f,"Aluminium");
        }
        static void Sofa(Transform p,Vector3 a)
        {Box(p,"Upholstered sofa",a+Vector3.up*.4f,new Vector3(2.5f,.55f,.95f),"CargoBlue");Box(p,"Sofa back",a+new Vector3(0,.87f,.4f),new Vector3(2.5f,.85f,.2f),"CargoBlue");for(int s=-1;s<=1;s+=2)Box(p,"Sofa arm",a+new Vector3(s*1.2f,.65f,0),new Vector3(.2f,.65f,1),"CargoBlue");}
        static void Desk(Transform p,Vector3 a,bool command)
        {
            Box(p,"Desk top",a+Vector3.up*.86f,new Vector3(2.7f,.12f,1.3f),"Wood");for(int s=-1;s<=1;s+=2)Box(p,"Desk pedestal",a+new Vector3(s*1.1f,.4f,0),new Vector3(.25f,.8f,1.1f),"Steel");
            Box(p,"Monitor",a+new Vector3(0,1.45f,.28f),new Vector3(command?1.8f:1.15f,.7f,.09f),"WindowDark");Box(p,"Display graph",a+new Vector3(0,1.48f,.23f),new Vector3(1,.025f,.01f),"NeonCyan",false);
            Box(p,"Monitor stand",a+new Vector3(0,1.06f,.3f),new Vector3(.12f,.4f,.14f),"Aluminium");Box(p,"Keyboard",a+new Vector3(0,.96f,-.25f),new Vector3(.8f,.055f,.26f),"Rubber");Chair(p,a+Vector3.back*1.2f);
        }
        static void Chair(Transform p,Vector3 a)
        {Box(p,"Chair cushion",a+Vector3.up*.48f,new Vector3(.65f,.12f,.65f),"CargoBlue");Box(p,"Chair curved back",a+new Vector3(0,.88f,-.28f),new Vector3(.68f,.75f,.1f),"CargoBlue");for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)Beam(p,"Chair leg",a+new Vector3(s*.26f,0,z*.26f),a+new Vector3(s*.23f,.45f,z*.23f),.04f,"Steel");}
        static void CafeTable(Transform p,Vector3 a)
        {Box(p,"Cafe round tabletop",a+Vector3.up*.85f,new Vector3(1.8f,.065f,1.8f),"Wood",true,PrimitiveType.Cylinder);Beam(p,"Table pedestal",a,a+Vector3.up*.83f,.1f,"Steel");for(int s=-1;s<=1;s+=2)Chair(p,a+new Vector3(s*1.25f,0,0));Box(p,"Ceramic cup",a+new Vector3(.3f,.98f,.1f),new Vector3(.14f,.13f,.14f),"Cladding",false,PrimitiveType.Cylinder);}
        static void Counter(Transform p,Vector3 a,bool kitchen)
        {
            Box(p,"Fitted counter",a+Vector3.up*.48f,new Vector3(5.2f,.96f,1.3f),"Wood");Box(p,"Stone worktop",a+Vector3.up*1.01f,new Vector3(5.4f,.12f,1.4f),"Cladding");
            for(int i=0;i<5;i++){Box(p,"Cabinet panel",a+new Vector3(-2+i,.5f,-.665f),new Vector3(.96f,.85f,.035f),"Wood");Box(p,"Metal handle",a+new Vector3(-2+i,.79f,-.7f),new Vector3(.5f,.03f,.07f),"Aluminium",false);}
            Box(p,"Espresso machine",a+new Vector3(1.2f,1.4f,0),new Vector3(1.25f,.7f,.85f),"Aluminium");Box(p,"Espresso black panel",a+new Vector3(1.2f,1.44f,-.44f),new Vector3(1,.4f,.03f),"Rubber");
            for(int s=-1;s<=1;s+=2){Beam(p,"Coffee group head",a+new Vector3(1.2f+s*.3f,1.5f,-.5f),a+new Vector3(1.2f+s*.3f,1.28f,-.5f),.06f,"Aluminium");Box(p,"Coffee cup",a+new Vector3(1.2f+s*.3f,1.18f,-.5f),new Vector3(.15f,.12f,.15f),"Cladding",false,PrimitiveType.Cylinder);}
            Box(p,"Stainless sink",a+new Vector3(-1.3f,1.08f,0),new Vector3(.9f,.04f,.65f),"Steel");Beam(p,"Sink tap",a+new Vector3(-1.3f,1.1f,.3f),a+new Vector3(-1.3f,1.5f,.3f),.025f,"Aluminium");
        }
        static void Shelf(Transform p,Vector3 a,bool industrial)
        {
            for(int s=-1;s<=1;s+=2)Box(p,"Shelf upright",a+new Vector3(s*1.5f,1.6f,0),new Vector3(.12f,3.2f,1),industrial?"Steel":"Wood");
            for(int level=0;level<4;level++){Box(p,"Shelf",a+new Vector3(0,.3f+level*.8f,0),new Vector3(3.1f,.09f,1),industrial?"Aluminium":"Wood");for(int i=0;i<6;i++)Box(p,industrial?"Stock bin":"Archive books",a+new Vector3(-1.2f+i*.47f,.58f+level*.8f,0),new Vector3(.32f,.48f,.65f),(i+level)%2==0?"CargoBlue":"CargoGold");}
        }
        static void OpenLegacyConnectors()
        {
            var scene=EditorSceneManager.OpenScene("Assets/AfterSignal/Scenes/24_OpenCity.unity");
            var manifest=Object.FindAnyObjectByType<SceneBatchManifest>();if(!manifest)return;
            if(manifest.sources!=null)foreach(var r in manifest.sources)if(r)r.enabled=true;
            if(manifest.generated!=null)foreach(var go in manifest.generated)if(go)Object.DestroyImmediate(go);
            bool Crosses(Bounds b)
            {
                foreach(var road in ExpansionRoads.Roads)foreach(var at in road)
                    if(at.x<910&&Mathf.Abs(at.z)<460&&at.x>=b.min.x-13&&at.x<=b.max.x+13&&at.z>=b.min.z-13&&at.z<=b.max.z+13)return true;
                return false;
            }
            int removed=0;
            foreach(var t in Object.FindObjectsByType<Transform>())
            {
                if(!t||!t.name.StartsWith("SKYLINE /"))continue;var renderers=t.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)continue;
                Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
                if(Crosses(b)){Object.DestroyImmediate(t.gameObject);removed++;}
            }
            foreach(var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if(r.name=="Distant city silhouette"&&Crosses(r.bounds))Object.DestroyImmediate(r.gameObject);
                else if(Mathf.Abs(r.transform.position.z-339.9f)<.03f&&Crosses(r.bounds))Object.DestroyImmediate(r.gameObject);
            }
            var groups=new Dictionary<string,List<MeshFilter>>();
            foreach(var f in Object.FindObjectsByType<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!f.sharedMesh||!f.sharedMesh.isReadable||r.sharedMaterials.Length!=1||!r.sharedMaterial||r.sharedMaterial.renderQueue>2500)continue;
                if(f.GetComponentInParent<CityBuildingCutaway>()||f.GetComponentInParent<CityVehicle>()||f.GetComponentInParent<CityTrafficSignal>()||f.GetComponentInParent<GrappleAnchor>()||f.GetComponentInParent<PixelActor>())continue;
                string key=Mathf.FloorToInt(r.bounds.center.x/30)+"_"+Mathf.FloorToInt(r.bounds.center.z/30)+"_"+r.sharedMaterial.name;
                if(!groups.ContainsKey(key))groups[key]=new List<MeshFilter>();groups[key].Add(f);
            }
            var sources=new List<MeshRenderer>();var batches=new List<GameObject>();int n=0;
            AssetDatabase.StartAssetEditing();
            try{foreach(var group in groups.Values)
            {
                if(group.Count<2)continue;var m=new Mesh{name=scene.name+"_batch_"+n,indexFormat=IndexFormat.UInt32};m.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray(),true,true);m.RecalculateBounds();
                string path="Assets/AfterSignal/Resources/Geometry/"+m.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);m=existing;}else AssetDatabase.CreateAsset(m,path);
                var go=new GameObject("BATCH / connector-ready / "+n++,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(manifest.transform,false);go.GetComponent<MeshFilter>().sharedMesh=m;go.GetComponent<MeshRenderer>().sharedMaterial=group[0].GetComponent<MeshRenderer>().sharedMaterial;batches.Add(go);
                foreach(var f in group){var r=f.GetComponent<MeshRenderer>();sources.Add(r);r.enabled=false;}
            }}finally{AssetDatabase.StopAssetEditing();}
            manifest.sources=sources.ToArray();manifest.generated=batches.ToArray();manifest.originalRenderers=sources.Count;manifest.batches=batches.Count;
            EditorSceneManager.SaveScene(scene);Debug.Log("MOBILITY CONNECTORS: cleared "+removed+" former perimeter towers and rebaked only city geometry.");
        }
    }
}
