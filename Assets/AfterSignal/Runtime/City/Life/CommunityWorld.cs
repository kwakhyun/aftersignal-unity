using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CommunityWorld:MonoBehaviour
    {
        public static CommunityWorld Instance{get;private set;}
        public readonly List<CivicRoutine> People=new List<CivicRoutine>();
        public readonly List<Vector3> RoomCenters=new List<Vector3>();
        GameDirector game;Transform root;int serial;
        public bool School{get;private set;}
        public static GameObject Box(Transform parent,string name,Vector3 at,Vector3 size,string material,bool solid=true)=>ResidentialWorld.Box(parent,name,at,size,material,solid);
        public static void Sign(Transform root,string text,Vector3 p,float width=7)
        {
            Box(root,"Sign backing",p,new Vector3(width,1,.12f),"Metal",false);
            var g=new GameObject(text,typeof(TextMesh));g.transform.SetParent(root,false);g.transform.localPosition=p+Vector3.back*.09f;
            var t=g.GetComponent<TextMesh>();t.text=text;t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.GetComponent<Renderer>().sharedMaterial=t.font.material;t.fontSize=64;t.characterSize=.07f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.75f,.94f,1);
        }
        IEnumerator Start()
        {
            Instance=this;game=GameDirector.Instance;
            yield return null;
            if(!game)yield break;
            root=new GameObject("Community spaces and routines").transform;root.SetParent(transform,false);
            if(game.stage==StageId.UrbanCity){CityExteriors();yield break;}
            if(game.stage==StageId.Residence){Neighbors();yield break;}
            if(!CivicWorld.Interior(game.stage))yield break;
            if(game.stage==StageId.UrbanInterior&&ResidentialWorld.VisitHome>=0){HomeRoutines();yield break;}
            int type=game.stage==StageId.School?5:game.stage==StageId.Clinic?4:game.stage==StageId.Headquarters?14:UrbanCatalog.Kind(UrbanCatalog.Current);
            School=type==5;
            if(game.stage==StageId.UrbanInterior&&type!=1&&type!=4&&type!=5&&type!=14&&!UrbanCatalog.IsGarage(UrbanCatalog.Current)&&!UrbanCatalog.IsBar(UrbanCatalog.Current))
            {PopulateExistingService(type);yield break;}
            ReplaceArchitecture();
            game.stageLength=School?182:type==4?116:type==14?100:78;game.halfDepth=School?45:30;
            Box(root,"Continuous facility floor",new Vector3(game.stageLength/2,-.08f,0),new Vector3(game.stageLength,.16f,game.halfDepth*2),"HomeWood2");
            Box(root,"Rear facility wall",new Vector3(game.stageLength/2,2.5f,game.halfDepth),new Vector3(game.stageLength,5,.25f),"Enamel");
            Box(root,"West facility wall",new Vector3(0,2.5f,0),new Vector3(.25f,5,game.halfDepth*2),"Enamel");
            Box(root,"East facility wall",new Vector3(game.stageLength,2.5f,0),new Vector3(.25f,5,game.halfDepth*2),"Enamel");
            if(School)BuildSchool();
            else if(type==4)BuildClinic();
            else if(type==14)BuildHeadquarters();
            else BuildService(type);
            var point=new GameObject("Facility services",typeof(InteractionPoint));point.transform.SetParent(root,false);point.transform.position=new Vector3(10,1.2f,3);
            point.GetComponent<InteractionPoint>().kind=InteractionKind.LifeService;point.GetComponent<InteractionPoint>().title="시설 서비스";point.GetComponent<InteractionPoint>().radius=3;
            var exit=new GameObject("Community exit",typeof(InteractionPoint));exit.transform.SetParent(root,false);exit.transform.position=new Vector3(4,1.2f,0);
            var ep=exit.GetComponent<InteractionPoint>();ep.kind=game.stage==StageId.UrbanInterior?InteractionKind.UrbanExit:InteractionKind.FacilityTravel;ep.destination=StageId.Haven;ep.title="출입문 · 시내로";ep.radius=3;
            Sign(root,"출입문 E",new Vector3(4,3,-1),6);
            game.spawn=game.checkpoint=new Vector3(6,.12f,0);
            Physics.SyncTransforms();game.Player.Respawn(game.spawn,false);game.CameraRig.Snap();
        }
        void PopulateExistingService(int type)
        {
            foreach(var npc in FindObjectsByType<CityNpc>())
            {
                if(npc.fixedQuest)continue;
                var behavior=npc.GetComponent<CivicRoutine>();
                if(!behavior){behavior=npc.gameObject.AddComponent<CivicRoutine>();behavior.Initialize(npc.occupation,npc.transform.position);}
                behavior.meal=new Vector3(38,.1f,-5);behavior.toilet=new Vector3(51,.1f,-7);behavior.social=new Vector3(27,.1f,0);People.Add(behavior);
            }
            for(int i=0;i<6;i++)
            {
                string art=type==9||type==15?i<2?"Bartender":PeopleArt.Citizens[i]:type==3?i%2==0?"OfficeMan":"OfficeWoman":PeopleArt.Citizens[(type+i)%12];
                var p=Person(art,"\uBC29\uBB38\uAC1D "+(i+1),i<2?"\uC2DC\uC124 \uC9C1\uC6D0":"\uBC29\uBB38\uAC1D",new Vector3(16+i*6,.1f,-4));
                p.work=new Vector3(15+i*6,.1f,-3);p.meal=new Vector3(19+i*4,.1f,-8);p.toilet=new Vector3(53,.1f,-6);p.social=new Vector3(20+i*4,.1f,1);
            }
        }
        void ReplaceArchitecture()
        {
            var old=GameObject.Find("WORLD / editable architecture");
            var baked=GameObject.Find("BAKED / geometry manifest");if(baked)baked.SetActive(false);
            if(old)
            {
                foreach(var r in old.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
                foreach(var c in old.GetComponentsInChildren<Collider>())if(!c.GetComponentInParent<CityNpc>())c.enabled=false;
            }
            foreach(var npc in FindObjectsByType<CityNpc>())
            {
                if(npc.fixedQuest){npc.transform.SetParent(root,true);npc.transform.position=new Vector3(14+serial++*3,.1f,-4);if(npc.point){npc.point.transform.SetParent(root,true);npc.point.transform.position=npc.transform.position+Vector3.up*1.2f;}continue;}
                npc.gameObject.SetActive(false);
            }
            int console=0;
            foreach(var p in new List<InteractionPoint>(InteractionPoint.All))
            {
                if(!p||p.npc||!(p.kind==InteractionKind.FirstRail||p.kind==InteractionKind.MissionBoard||p.kind==InteractionKind.BreachMission))continue;
                p.transform.SetParent(root,true);p.transform.position=new Vector3(28+console++*8,1.2f,-4);
                Box(root,"Campaign mission terminal",p.transform.position-Vector3.up*.55f,new Vector3(1.4f,1.3f,.8f),"Metal");
                Sign(root,p.title,p.transform.position+Vector3.up*1.8f,7);
            }
            if(old)old.SetActive(false);
            var themes=FindAnyObjectByType<UrbanInterior>();if(themes)themes.gameObject.SetActive(false);
            var points=new List<InteractionPoint>(InteractionPoint.All);
            foreach(var p in points)if(p&&!p.npc&&(p.kind==InteractionKind.Furniture||p.kind==InteractionKind.Rest||p.kind==InteractionKind.UrbanService||p.kind==InteractionKind.LifeService||p.kind==InteractionKind.UrbanExit||p.kind==InteractionKind.FacilityTravel))p.gameObject.SetActive(false);
        }
        void Room(string name,Vector3 center,Vector2 size)
        {
            RoomCenters.Add(center);
            Box(root,name+" floor",center+Vector3.down*.005f,new Vector3(size.x,.03f,size.y),"LightTile",false);
            for(int s=-1;s<=1;s+=2)Box(root,name+" side",center+new Vector3(s*size.x/2,1.8f,0),new Vector3(.16f,3.6f,size.y),"Enamel");
            float doorZ=center.z>0?center.z-size.y/2:center.z+size.y/2;
            for(int s=-1;s<=1;s+=2)Box(root,name+" doorway wall",new Vector3(center.x+s*(size.x/4+1),1.8f,doorZ),new Vector3(size.x/2-2,3.6f,.15f),"Enamel");
            Sign(root,name,new Vector3(center.x,3.25f,doorZ-.1f),8);
        }
        void Desk(Vector3 p,bool chair=true)
        {
            Box(root,"Desk top",p+Vector3.up*.86f,new Vector3(2.1f,.13f,1.2f),"WarmWood");
            for(int s=-1;s<=1;s+=2)Box(root,"Desk leg",p+new Vector3(s*.8f,.43f,0),new Vector3(.1f,.86f,.8f),"Metal");
            if(chair){Box(root,"Chair seat",p+new Vector3(0,.45f,-1),new Vector3(.8f,.12f,.75f),"SoftCloth");Box(root,"Chair back",p+new Vector3(0,.95f,-1.35f),new Vector3(.8f,1,.12f),"SoftCloth");}
        }
        Vector3 Bed(Vector3 p)
        {
            Box(root,"Bed frame",p+Vector3.up*.35f,new Vector3(1.6f,.7f,3.2f),"Metal");
            Box(root,"Bed mattress",p+Vector3.up*.78f,new Vector3(1.55f,.2f,3.1f),"Enamel");
            Box(root,"Bed blanket",p+new Vector3(0,.91f,-.5f),new Vector3(1.55f,.08f,1.9f),"SoftCloth",false);
            Box(root,"Pillow",p+new Vector3(0,.96f,1.1f),new Vector3(1.2f,.2f,.6f),"Enamel",false);
            return p+Vector3.up;
        }
        void Toilet(Vector3 p)
        {
            for(int i=0;i<3;i++){var at=p+Vector3.right*i*2.8f;Box(root,"WC privacy divider",at+new Vector3(-1,1,0),new Vector3(.12f,2,3),"Enamel");Box(root,"WC pedestal",at+Vector3.up*.25f,new Vector3(.5f,.5f,.6f),"Enamel");Box(root,"WC seat",at+Vector3.up*.55f,new Vector3(.85f,.13f,1.05f),"Enamel");Box(root,"WC cistern",at+new Vector3(0,.8f,.7f),new Vector3(.8f,1,.25f),"Enamel");}
        }
        CivicRoutine Person(string art,string name,string job,Vector3 p)
        {
            var go=new GameObject(name,typeof(SpriteRenderer),typeof(CityNpc));go.transform.SetParent(root,false);go.transform.position=p;
            go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            var npc=go.GetComponent<CityNpc>();npc.Configure(4000+serial++,job,name+" · "+job);
            PeopleArt.Attach(go,art);
            var routine=go.AddComponent<CivicRoutine>();routine.Initialize(job,p);People.Add(routine);return routine;
        }
        void BuildSchool()
        {
            string[] titles={"1학년 교실","2학년 교실","3학년 교실","4학년 교실","도서실","행정실","교장실","보건실","화장실","급식실"};
            for(int n=0;n<10;n++)
            {
                var c=new Vector3(22+n%5*34,0,n<5?26:-26);Room(titles[n],c,new Vector2(30,30));
                if(n<4)
                {
                    for(int k=0;k<8;k++)
                    {
                        var at=c+new Vector3((k%4-1.5f)*4,0,(k/4)*5-3);Desk(at);
                        var p=Person(k%2==0?"StudentBoy":"StudentGirl",new[]{"지우","서윤","민준","하린","도윤","수아","예준","채원"}[k]+(n+1),"학생",at+Vector3.back*2);
                        p.student=true;p.meal=new Vector3(145+k%4*3,.1f,-22);p.toilet=new Vector3(122+k%3*2,.1f,-23);p.social=new Vector3(16+n*32+k,.1f,0);
                    }
                    Box(root,"Classroom blackboard",c+new Vector3(0,2,11),new Vector3(15,2.8f,.15f),"DistrictBlue",false);
                    var t=Person(n%2==0?"TeacherMan":"TeacherWoman","담임 "+(n+1),"교사",c+new Vector3(0,.1f,7));t.meal=new Vector3(152,.1f,-27);t.toilet=new Vector3(123,.1f,-20);t.social=new Vector3(48,.1f,-23);
                }
                else if(n==7){for(int i=0;i<3;i++)Bed(c+new Vector3(i*4-4,0,4));Person("Nurse","보건 담당 유진","보건교사",c+Vector3.back*4);}
                else if(n==8)Toilet(c+new Vector3(-7,0,4));
                else if(n==9){for(int i=0;i<8;i++)Desk(c+new Vector3(i%4*5-7,0,i/4*7-3));Person("Worker","급식 담당 정우","급식 직원",c+Vector3.forward*9);}
                else{for(int i=0;i<4;i++)Desk(c+new Vector3(i*5-8,0,4));Person(n==6?"TeacherMan":"OfficeWoman",n==6?"교장 선우":"학교 직원 "+n,n==6?"교장":"학교 직원",c);}
            }
        }
        void BuildClinic()
        {
            Room("접수 · 외래 진료",new Vector3(24,0,18),new Vector2(37,22));
            Room("입원 병동",new Vector3(73,0,18),new Vector2(52,22));
            Room("식당 · 휴게실",new Vector3(27,0,-18),new Vector2(38,22));
            Room("화장실",new Vector3(77,0,-18),new Vector2(27,22));Toilet(new Vector3(71,0,-20));
            for(int i=0;i<8;i++)
            {
                Vector3 bed=Bed(new Vector3(53+i%4*11,0,13+i/4*10));
                var p=Person(i%2==0?"PatientMan":"PatientWoman","입원 환자 "+(i+1),"환자",new Vector3(bed.x-2,.1f,bed.z));p.patient=true;p.rest=bed;p.meal=new Vector3(18+i*2,.1f,-16);p.toilet=new Vector3(74+i%3*2,.1f,-19);p.social=new Vector3(56+i*3,.1f,4);
            }
            for(int i=0;i<6;i++)
            {
                var p=Person(i%2==0?"Doctor":"Nurse",i%2==0?"의사 "+(i/2+1):"간호사 "+(i/2+1),i%2==0?"의사":"간호사",new Vector3(15+i*6,.1f,16));
                p.social=new Vector3(51+i*8,.1f,12);p.meal=new Vector3(14+i*3,.1f,-19);p.toilet=new Vector3(73,.1f,-21);
            }
            for(int i=0;i<5;i++){Desk(new Vector3(14+i*5,0,-18));Desk(new Vector3(12+i*5,0,22));}
        }
        void BuildHeadquarters()
        {
            Room("작전실",new Vector3(25,0,18),new Vector2(38,22));Room("분석실",new Vector3(73,0,18),new Vector2(38,22));
            Room("직원 휴게실",new Vector3(25,0,-18),new Vector2(38,22));Room("화장실",new Vector3(74,0,-18),new Vector2(28,22));Toilet(new Vector3(70,0,-20));
            for(int i=0;i<12;i++)
            {
                Vector3 at=new Vector3(12+i%6*13,0,13+i/6*10);Desk(at);Box(root,"Workstation display",at+new Vector3(0,1.5f,.3f),new Vector3(1.5f,1,.1f),"DistrictWindow",false);
                var p=Person(i%2==0?"OfficeMan":"OfficeWoman","본부 직원 "+(i+1),i%3==0?"신호 분석가":i%3==1?"현장 지원관":"작전 담당",at+Vector3.back*2);p.meal=new Vector3(15+i%5*4,.1f,-18);p.toilet=new Vector3(73,.1f,-20);p.social=new Vector3(28+i%4*3,.1f,0);
            }
        }
        void BuildService(int type)
        {
            if(UrbanCatalog.IsGarage(UrbanCatalog.Current)){BuildGarage();return;}
            if(UrbanCatalog.IsBar(UrbanCatalog.Current)){BuildBar();return;}
            Room(type==1?"민원실":type==3?"은행 창구":"영업 공간",new Vector3(23,0,18),new Vector2(36,22));
            Room(type==1?"유치장":"직원 휴게실",new Vector3(61,0,18),new Vector2(28,22));
            Room("식사 · 휴게 공간",new Vector3(24,0,-18),new Vector2(34,22));Room("화장실",new Vector3(62,0,-18),new Vector2(25,22));Toilet(new Vector3(58,0,-20));
            for(int i=0;i<8;i++)
            {
                Vector3 at=new Vector3(12+i%4*8,.1f,10+i/4*12);Desk(at);
                string art=type==1?"Police":type==9||type==15?i%2==0?"Bartender":"CivilianMan":PeopleArt.Citizens[i%PeopleArt.Citizens.Length];
                var p=Person(art,"직원·방문객 "+(i+1),i<3?"시설 직원":"방문객",at+Vector3.back*2);p.meal=new Vector3(13+i*3,.1f,-16);p.toilet=new Vector3(60,.1f,-20);p.social=new Vector3(20+i*4,.1f,0);
            }
            if(type==1)
            {
                for(int i=0;i<3;i++)
                {
                    float x=51+i*8;for(int bar=0;bar<9;bar++)Box(root,"Custody steel bars",new Vector3(x-3.5f+bar*.85f,1.5f,8),new Vector3(.07f,3,.08f),"VehicleAlloy");
                    Bed(new Vector3(x,0,24));var p=Person("Worker","수감자 "+(i+1),"수감자",new Vector3(x,.1f,17));p.prisoner=true;p.rest=new Vector3(x,.1f,22);p.work=new Vector3(x,.1f,13);
                }
                Sign(root,"경찰 유치장",new Vector3(61,3.8f,8),13);
            }
        }
        void BuildGarage()
        {
            Sign(root,"AFTERLIGHT MOTOR LAB",new Vector3(35,4,20),25);
            for(int i=0;i<3;i++){float x=18+i*19;Box(root,"Repair bay floor",new Vector3(x,.06f,8),new Vector3(12,.08f,18),"Metal",false);for(int s=-1;s<=1;s+=2)Box(root,"Hydraulic lift pillar",new Vector3(x+s*5,2.5f,8),new Vector3(.6f,5,.8f),"DistrictBlue");}
            for(int i=0;i<5;i++)Person("Worker","정비사 "+(i+1),"자동차 정비사",new Vector3(12+i*12,.1f,3));
            Sign(root,"수리 · 도색 · 휠 · 스포일러 / E",new Vector3(18,3,0),15);
        }
        void BuildBar()
        {
            Sign(root,"NEON AFTERHOURS",new Vector3(35,4,20),24);
            Box(root,"Neon bar counter",new Vector3(35,1.1f,13),new Vector3(43,2.2f,2),"Metal");
            Box(root,"Magenta bar strip",new Vector3(35,1.9f,11.95f),new Vector3(43,.08f,.08f),"NeonRose",false);
            for(int i=0;i<18;i++)Box(root,"Bottle",new Vector3(16+i*2.3f,2.5f,13),new Vector3(.25f,.75f,.25f),i%2==0?"NeonAzure":"NeonRose",false);
            for(int i=0;i<10;i++){Desk(new Vector3(13+i%5*12,0,-10+i/5*12));var p=Person(i<2?"Bartender":PeopleArt.Citizens[i%12],"바 손님 "+(i+1),i<2?"바텐더":"단골 손님",new Vector3(14+i*5,.1f,i<2?16:4));p.meal=p.work;p.social=new Vector3(15+i%5*10,.1f,-8);}
        }
        void CityExteriors()
        {
            foreach(int site in new[]{38,39})
            {
                var at=UrbanCatalog.Door(site);Sign(root,UrbanCatalog.Name(site),at+new Vector3(0,4,-.4f),20);
                if(site==38){for(int i=0;i<3;i++)Box(root,"Garage tool cabinet",at+new Vector3(-9+i*3,1,-1),new Vector3(2,2,.9f),"DistrictBlue");}
            }
        }
        void Neighbors()
        {
            for(int floor=1;floor<=4;floor++)
            {
                float y=floor*4.4f;
                Box(root,"Neighbor floor corridor",new Vector3(28,y-.18f,-4),new Vector3(50,.36f,7),"HomeWood2");
                float landing=floor%2==1?80:58;
                Box(root,"Neighbor corridor connection",new Vector3((53+landing)/2,y-.18f,-4),new Vector3(landing-53+2,.36f,3),"HomeWood2");
                Box(root,"Landing threshold",new Vector3(landing,y-.18f,-1.25f),new Vector3(2.2f,.36f,3.5f),"HomeWood2");
                for(int unit=0;unit<3;unit++)
                {
                    int resident=(floor-1)*3+unit;var p=new Vector3(12+unit*18,y,-.5f);
                    Box(root,"Neighbor front door",p+Vector3.up*1.3f,new Vector3(2.2f,2.6f,.2f),"WarmWood");
                    Sign(root,(floor+1)+"0"+(unit+1)+" · "+ResidentialWorld.Names[resident],p+Vector3.up*3,7);
                    var door=new GameObject("Neighbor home entrance",typeof(InteractionPoint),typeof(NeighborEntrance));door.transform.SetParent(root,false);door.transform.position=p+Vector3.back*1.4f+Vector3.up;
                    door.GetComponent<InteractionPoint>().kind=InteractionKind.Furniture;door.GetComponent<InteractionPoint>().title="이웃집 방문";door.GetComponent<InteractionPoint>().radius=3;
                    door.GetComponent<NeighborEntrance>().resident=resident;
                    if(unit<2){var npc=Person(PeopleArt.Citizens[resident%12],ResidentialWorld.Names[resident],"아파트 주민",p+Vector3.back*3);npc.work=p+new Vector3(5,0,-3);npc.social=p+new Vector3(-5,0,-3);npc.homeResident=true;npc.gameObject.AddComponent<NeighborPresence>();npc.GetComponent<CityNpc>().identity="resident-"+(2000+resident);}
                }
            }
        }
        void HomeRoutines()
        {
            foreach(var resident in FindObjectsByType<ResidentialCitizen>())
            {
                if(!resident.indoors)continue;
                var p=resident.gameObject.AddComponent<CivicRoutine>();p.Initialize("주민",resident.transform.position);p.homeResident=true;p.rest=new Vector3(47,1.4f,10);p.meal=new Vector3(23,.1f,-2);p.toilet=new Vector3(55,.1f,-6);p.social=new Vector3(26,.1f,2);People.Add(p);
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class NeighborEntrance:MonoBehaviour{public int resident;}
    public sealed class NeighborPresence:MonoBehaviour
    {
        void LateUpdate()
        {
            bool present=LifeState.Hour>=7&&LifeState.Hour<9||LifeState.Hour>=18&&LifeState.Hour<22;
            var r=GetComponent<SpriteRenderer>();if(r)r.enabled=present;
            var n=GetComponent<CityNpc>();if(n&&n.point)n.point.gameObject.SetActive(present);
            var c=GetComponent<Collider>();if(c)c.enabled=present;
        }
    }
}
