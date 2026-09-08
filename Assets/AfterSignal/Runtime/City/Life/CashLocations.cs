using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CashLocations:MonoBehaviour
    {
        public static CashLocations Instance{get;private set;}
        public CashContainer Counter{get;private set;}
        void Awake()=>Instance=this;
        IEnumerator Start()
        {
            yield return new WaitForSeconds(1.5f);var g=GameDirector.Instance;
            if(g.stage==StageId.Residence){Make(new Vector3(3,22.1f,3.7f),true,false);yield break;}
            if(!CivicWorld.Interior(g.stage))yield break;
            int kind=g.stage==StageId.Clinic?4:g.stage==StageId.UrbanInterior?UrbanCatalog.Kind(UrbanCatalog.Current):-1;
            if(kind!=3&&kind!=4&&kind!=7&&kind!=9&&kind!=15&&kind!=0)yield break;
            Vector3 preferred=new Vector3(18,.1f,4);
            foreach(var npc in FindObjectsByType<CityNpc>())if(npc.point&&npc.point.kind==InteractionKind.UrbanService){preferred=npc.transform.position+Vector3.right*4;break;}
            bool bank=kind==3;Vector3 place=FindSpace(preferred,bank?1.6f:.7f,g);
            Counter=Make(place,false,bank);
        }
        static Vector3 FindSpace(Vector3 preferred,float radius,GameDirector g)
        {
            for(int i=0;i<180;i++)
            {
                var at=i<25?preferred+new Vector3((i%5-2)*2,0,(i/5-2)*2):new Vector3(5+(i%15)*(g.stageLength-10)/15,preferred.y,-g.halfDepth+3+(i/15)*(g.halfDepth*2-6)/12);
                if(!Physics.Raycast(at+Vector3.up*1.5f,Vector3.down,out var floor,3,1,QueryTriggerInteraction.Ignore)||floor.normal.y<.8f)continue;
                at.y=floor.point.y+.05f;
                if(!Physics.CheckBox(at+Vector3.up*1.45f,new Vector3(radius,1.35f,radius),Quaternion.identity,1,QueryTriggerInteraction.Ignore))return at;
            }
            return preferred;
        }
        public static CashContainer Make(Vector3 at,bool home,bool bank)
        {
            var root=new GameObject(home?"서하의 개인 금고":bank?"은행 현금 금고":"매장 현금 보관함");root.transform.position=at;
            var cash=root.AddComponent<CashContainer>();cash.Home=home;cash.Bank=bank;
            float w=bank?3.2f:home?1.2f:1,h=bank?2.7f:home?1.5f:1.05f,d=bank?2:home?.9f:.7f;
            foreach(float side in new[]{-1f,1f})ResidentialWorld.Box(root.transform,"Reinforced safe side",new Vector3(side*w*.48f,h*.5f,0),new Vector3(.12f,h,d),"DarkMetal");
            ResidentialWorld.Box(root.transform,"Safe back",new Vector3(0,h*.5f,d*.48f),new Vector3(w,h,.12f),"DarkMetal");
            ResidentialWorld.Box(root.transform,"Safe floor",new Vector3(0,.08f,0),new Vector3(w,.16f,d),"DarkMetal");
            ResidentialWorld.Box(root.transform,"Safe ceiling",new Vector3(0,h-.08f,0),new Vector3(w,.16f,d),"DarkMetal");
            cash.Door=new GameObject("Hinged cash safe door").transform;cash.Door.SetParent(root.transform,false);cash.Door.localPosition=new Vector3(-w*.48f,0,-d*.55f);
            WorldGeometry.Part(cash.Door,"Steel safe door",new Vector3(w*.48f,h*.5f,0),new Vector3(w*.93f,h*.92f,.15f),"Chrome");
            WorldGeometry.Part(cash.Door,"Keypad",new Vector3(w*.72f,h*.65f,-.13f),new Vector3(.2f,.24f,.04f),"DistrictBlue");
            for(int i=0;i<6;i++)WorldGeometry.Part(cash.Door,"Keypad button",new Vector3(w*.66f+i%3*.05f,h*.68f-i/3*.07f,-.16f),new Vector3(.03f,.03f,.02f),"CyanFX");
            cash.Cash=new GameObject("Stored credit bundles").transform;cash.Cash.SetParent(root.transform,false);
            for(int i=0;i<(bank?24:6);i++)WorldGeometry.Part(cash.Cash,"Credit bundle",new Vector3((i%4-1.5f)*w*.17f,.3f+i/4*.13f,-d*.15f),new Vector3(w*.16f,.1f,.3f),"DistrictIvory");
            var point=root.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title=root.name;point.radius=bank?4.5f:3;
            return cash;
        }
        public static void ReturnHome()
        {
            var g=GameDirector.Instance;if(!g)return;
            if(g.Dead||WantedSystem.Level>0||PrisonSystem.Instance&&PrisonSystem.Instance.Jailed||CrimeObservation.Instance&&CrimeObservation.Instance.PendingCalls>0){g.SetPaused(false);g.Toast("추격·신고·수감 상태에서는 집으로 복귀할 수 없습니다.",4);return;}
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current)UrbanSimulation.Instance.EmergencyExit(UrbanSimulation.Instance.Current);
            CityLife.Instance?.Dismiss();g.SetPaused(false);ResidentialWorld.VisitHome=-1;ResidentialWorld.VisitResident=-1;
            CivicWorld.Travel(g,StageId.Residence,new Vector3(8,22.15f,-1));
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
