using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class InteriorDistinct:MonoBehaviour
    {
        public int UsableObjects {get;private set;}
        Transform details;int kind,site;Material accent;
        IEnumerator Start()
        {
            yield return null;yield return null;yield return null;
            var g=GameDirector.Instance;site=g.stage==StageId.UrbanInterior?UrbanCatalog.Current:(int)g.stage;
            if(g.stage==StageId.Residence)yield break;
            kind=g.stage==StageId.Clinic?4:g.stage==StageId.School?5:g.stage==StageId.Headquarters?14:g.stage==StageId.Residence?0:UrbanCatalog.Kind(site);
            details=new GameObject("Interior identity / "+site+" / "+kind).transform;
            accent=new Material(Resources.Load<Material>("Materials/Enamel"));accent.SetColor("_BaseColor",Color.HSVToRGB(Mathf.Repeat(kind*.087f+site*.018f,1),.2f+site%3*.08f,.53f+site%4*.07f));
            var objects=FindObjectsByType<Transform>();
            foreach(var t in objects)
            {
                if(t.GetComponentInParent<CityNpc>()||t.GetComponentInParent<PlayerMotor>()||t.GetComponent<InteractionPoint>()||t.GetComponent<UsableProp>()||UsableObjects>=45)continue;
                string name=t.name.ToLowerInvariant();PropUse? use=name.Contains("bench")||name.Contains("sofa")?PropUse.Bench:name.Contains("water cooler")||name.Contains("sink tap")?PropUse.Water:name.Contains("vending")?PropUse.Vending:name.Contains("television")?PropUse.Television:name.Contains("books")||name.Contains("archive shelf")?PropUse.Books:name.Contains("bed")?PropUse.Medical:name.Contains("locker")?PropUse.Locker:name.Contains("workbench")?PropUse.Workshop:(PropUse?)null;
                if(use==null)continue;var prop=t.gameObject.AddComponent<UsableProp>();prop.use=use.Value;
                var point=t.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title=use==PropUse.Books?"기록 읽기":use==PropUse.Bench?"잠시 쉬기":use==PropUse.Medical?"의료·휴식 안내":"시설 이용 · "+t.name;point.radius=2.5f;UsableObjects++;
            }
            if(CommunityWorld.Instance)
            {
                int serial=0;foreach(var center in CommunityWorld.Instance.RoomCenters){Room(center,serial++);if(serial>=24)break;}
            }
            else Room(new Vector3(g.stageLength*.5f,.1f,0),0);
        }
        Transform Part(string n,Vector3 at,Vector3 size,string mat="Enamel")=>WorldGeometry.Part(details,n,at,size,mat).transform;
        void Room(Vector3 center,int serial)
        {
            // A different arrangement and material rhythm per room, without moving existing doors or floors.
            var a=center+new Vector3(serial%2==0?-3:3,0,3);
            if(!Physics.Raycast(a+Vector3.up*1.8f,Vector3.down,out var floor,3,1,QueryTriggerInteraction.Ignore))return;
            a.y=floor.point.y;
            if(Physics.CheckCapsule(a+Vector3.up*.5f,a+Vector3.up*2,.45f,1,QueryTriggerInteraction.Ignore))return;
            var panel=Part("Room accent screen",a+Vector3.up*1.6f,new Vector3(2.5f,1.9f,.14f));panel.GetComponent<Renderer>().sharedMaterial=accent;
            for(int s=-1;s<=1;s+=2)Part("Display screen leg",a+new Vector3(s,0.55f,0),new Vector3(.065f,1.1f,.065f),"Chrome");
            if(kind==4)
            {
                Part("Medical cart top",a+new Vector3(0,.95f,-.8f),new Vector3(1.4f,.09f,.65f),"Chrome");
                for(int i=0;i<5;i++)Part("Sterile instrument tray",a+new Vector3(-.5f+i*.25f,1.03f,-.8f),new Vector3(.1f,.045f,.4f),"LightTile");
                Part("IV pole",a+new Vector3(1.7f,1.25f,0),new Vector3(.04f,2.5f,.04f),"Chrome");Part("IV bag",a+new Vector3(1.8f,2.1f,0),new Vector3(.25f,.38f,.12f),"Glass");
            }
            else if(kind==5)
            {
                for(int i=0;i<6;i++)Part("Student cubby",a+new Vector3((i%3-1)*.68f,.4f+i/3*.7f,-.4f),new Vector3(.6f,.62f,.55f),"WarmWood");
                Part("Learning display",a+new Vector3(0,1.8f,-.09f),new Vector3(2.2f,1,.03f),"DistrictWindow");
            }
            else if(kind==1||kind==3||kind==14)
            {
                Part(kind==3?"Bank secure document safe":"Secure evidence pedestal",a+new Vector3(0,.6f,-.6f),new Vector3(1.6f,1.2f,.8f),"DarkMetal");
                for(int i=0;i<8;i++)Part("Document drawer rail",a+new Vector3(0,.2f+i*.13f,-1.02f),new Vector3(1.4f,.025f,.035f),"Chrome");
                for(int i=0;i<5;i++)Part("Live information row",a+new Vector3(0,1.5f+i*.14f,-.09f),new Vector3(1.9f-i*.16f,.03f,.02f),"CyanFX");
            }
            else if(kind==2||kind==11||kind==13)
            {
                for(int i=0;i<5;i++){Part("Hanging workshop tool",a+new Vector3(-.8f+i*.4f,1.7f,-.13f),new Vector3(.07f,.8f,.08f),"Chrome");Part("Tool grip",a+new Vector3(-.8f+i*.4f,1.37f,-.13f),new Vector3(.13f,.28f,.1f),"Rubber");}
            }
            else
            {
                for(int i=0;i<7;i++){var slat=Part("Acoustic timber strip",a+new Vector3(-1+i*.33f,1.6f,-.09f),new Vector3(.08f,1.7f,.06f),"WarmWood");}
                Part("Hospitality side table",a+new Vector3(0,.7f,-.8f),new Vector3(1.2f,.08f,.7f),"WarmWood");
                Part("Flower vase",a+new Vector3(0,.92f,-.8f),new Vector3(.18f,.36f,.18f),"Enamel");
            }
            var console=panel.gameObject.AddComponent<FacilityConsole>();console.function=kind==4?FacilityFunction.Clinic:kind==5?FacilityFunction.School:kind==1?FacilityFunction.Police:kind==2?FacilityFunction.Fire:kind==3?FacilityFunction.Bank:kind==13?FacilityFunction.Freight:kind==14?FacilityFunction.Archive:kind==0?FacilityFunction.Home:kind==15?FacilityFunction.Cafe:FacilityFunction.Office;console.location="실내 시설 "+(serial+1);
            var point=panel.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title="시설 서비스";point.radius=2.8f;UsableObjects++;
        }
        void OnDestroy(){if(details)Destroy(details.gameObject);if(accent)Destroy(accent);}
    }
}
