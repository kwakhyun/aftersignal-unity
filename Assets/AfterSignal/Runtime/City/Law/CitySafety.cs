using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class MedicalPending:MonoBehaviour{public bool carried;}
    public sealed class CitySafety:MonoBehaviour
    {
        public static CitySafety Instance{get;private set;}
        public readonly List<PoliceOfficer> Patrol=new List<PoliceOfficer>();
        public int Reports{get;private set;}public int Dispatches{get;private set;}public int Transports{get;set;}
        GameDirector game;float next;int ambulances;
        readonly HashSet<int> reported=new HashSet<int>();
        void Awake(){Instance=this;}
        IEnumerator Start()
        {
            game=GameDirector.Instance;yield return new WaitForSeconds(1);
            if(game.stage==StageId.UrbanCity)
            {
                for(int i=0;i<4;i++)
                {
                    Vector3 desired=CityRoadNetwork.Sidewalk(game.Player.transform.position+new Vector3((i-1)*42,0,i%2==0?35:-35));
                    if(CityGangWar.FindGround(desired,out var at)){var o=PoliceOfficer.Create(WantedSystem.Instance,at,1,i);o.Ambient=true;Patrol.Add(o);}
                }
            }
            else if(CivicWorld.Interior(game.stage)&&UrbanCrime.InsideRobbery)StartCoroutine(InteriorResponse());
        }
        public static void Alarm(Vector3 at,WorldActor suspect=null,CityNpc victim=null)
        {
            if(!Instance)return;
            Instance.Report(at,suspect,victim);
        }
        void Report(Vector3 at,WorldActor suspect,CityNpc victim)
        {
            int id=suspect?suspect.GetInstanceID():-1;
            if(reported.Contains(id))return;
            reported.Add(id);Reports++;
            bool witness=victim&&victim.GetComponent<WorldActor>().Alive;
            foreach(var npc in FindObjectsByType<CityNpc>())
            {
                if(!npc.gameObject.activeInHierarchy||Vector3.Distance(npc.transform.position,at)>34)continue;
                var body=npc.GetComponent<WorldActor>();if(body&&!body.Alive)continue;
                if(npc==victim)NpcSpeech.Say(npc,"도와주세요! 강도예요!",4);
                else{npc.Panic(at,8);NpcSpeech.Say(npc,witness?"여기서 벗어나요!":"경찰이죠? 강도 사건이에요!",4);witness=true;}
            }
            if(suspect)StartCoroutine(Respond(at,suspect,witness?3:7));
        }
        IEnumerator Respond(Vector3 at,WorldActor suspect,float delay)
        {
            yield return new WaitForSeconds(delay);
            if(!suspect||!suspect.Alive)yield break;
            Dispatches++;
            foreach(var officer in Patrol)if(officer&&officer.Body.Alive){officer.Dispatch(suspect);NpcSpeech.Say(officer,"신고 접수. 현장으로 이동!",3);}
            int count=Patrol.FindAll(o=>o&&o.Body.Alive).Count;
            if(count<2)for(int i=0;i<2;i++)
            {
                Vector3 spawn=at+new Vector3(14+i*2,0,-9);
                if(game.stage==StageId.UrbanCity)spawn=CityRoadNetwork.Sidewalk(spawn);
                if(CityGangWar.FindGround(spawn,out var safe)){var o=PoliceOfficer.Create(WantedSystem.Instance,safe,2,i);o.Ambient=true;o.Dispatch(suspect);Patrol.Add(o);}
            }
        }
        IEnumerator InteriorResponse()
        {
            yield return new WaitForSeconds(8);
            for(int i=0;i<2;i++){var o=PoliceOfficer.Create(WantedSystem.Instance,new Vector3(5+i*2,.12f,0),2,i);o.Ambient=true;Patrol.Add(o);NpcSpeech.Say(o,"경찰입니다. 무기를 내려놓으세요!",4);}
        }
        public static void Shock(Vector3 at,string cause="danger")
        {
            foreach(var npc in FindObjectsByType<CityNpc>())
                if(npc&&Vector3.Distance(npc.transform.position,at)<25&&npc.GetComponent<WorldActor>().Alive)
                {npc.Panic(at,7);NpcSpeech.Say(npc,NpcDialogueBank.Line(npc,cause),3);}
        }
        void Update()
        {
            if(!game||!game.Ready||game.Blocked||Time.time<next)return;next=Time.time+2;
            if(game.stage!=StageId.UrbanCity||ambulances>=2||!UrbanSimulation.Instance)return;
            foreach(var body in WorldActor.All)
            {
                if(!body||body.police||body.gang||body.protectedResident||!body.GetComponent<CityNpc>()||body.health>40||body.GetComponent<MedicalPending>())continue;
                if(Vector3.Distance(body.transform.position,game.Player.transform.position)>220)continue;
                body.gameObject.AddComponent<MedicalPending>();ambulances++;
                EmergencyAmbulance.Create(body,()=>ambulances--);break;
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
