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
        public int ActiveAmbulances=>ambulances;public int RescueReports{get;private set;}public int FieldTreatments{get;set;}
        public void RequestRescue(WorldActor body){if(body&&body.Alive)RescueReports++;}
        public static int PlayerReport()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return 0;int count=0;foreach(var a in WorldActor.All){if(!a||!a.Alive||(a.Center-g.Player.Shoulder).sqrMagnitude>130*130)continue;var m=a.GetComponent<MedicalState>();if(m&&m.NeedsRescue){m.Report();count++;}}g.Toast(count>0?$"119 구조 신고 접수 · 부상자 {count}명, 구급대 출동 요청":"주변에 구조가 필요한 부상자가 없습니다.",5);return count;
        }
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
            // Player reports belong to CrimeObservation; accidents have no suspect.
            if(!suspect||suspect.environmental||!LocalSimulation.Combat(at))return;
            if(suspect.monster){SecurityResponse.Request(suspect,true);return;}
            int id=suspect?suspect.GetInstanceID():-1;
            if(reported.Contains(id))return;
            bool witness=victim&&victim.GetComponent<WorldActor>().Alive;
            foreach(var npc in FindObjectsByType<CityNpc>())
            {
                if(!npc.gameObject.activeInHierarchy||Vector3.Distance(npc.transform.position,at)>34)continue;
                var body=npc.GetComponent<WorldActor>();if(!body||!body.Alive||body==suspect||body.gang||body.terrorist||body.military||body.monster)continue;
                if(body.police){witness=true;NpcSpeech.Say(npc,"무장 용의자 확인. 현장 지원 요청!",4);continue;}
                if(npc==victim)NpcSpeech.Say(npc,suspect.terrorist?"테러예요! 경찰과 구급대를 보내 주세요!":"도와주세요! 강도예요!",4);
                else{npc.Panic(at,8);NpcSpeech.Say(npc,witness?"여기서 벗어나요!":suspect.terrorist?"경찰이죠? 무차별 공격이에요! 특수대응팀을 보내 주세요!":"경찰이죠? 강도 사건이에요!",4);witness=true;}
            }
            if(!witness)return;
            reported.Add(id);Reports++;
            if(suspect)StartCoroutine(Respond(at,suspect,3));
        }
        IEnumerator Respond(Vector3 at,WorldActor suspect,float delay)
        {
            yield return new WaitForSeconds(delay);
            if(!suspect||!suspect.Alive||!LocalSimulation.Combat(at))yield break;
            Dispatches++;
            foreach(var officer in Patrol)if(officer&&officer.Body.Alive&&!officer.Body.military){officer.Dispatch(suspect);NpcSpeech.Say(officer,"신고 접수. 현장으로 이동!",3);}
            SecurityResponse.Request(suspect,false);
        }
        IEnumerator InteriorResponse()
        {
            yield return new WaitForSeconds(8);
            for(int i=0;i<2;i++){var o=PoliceOfficer.Create(WantedSystem.Instance,new Vector3(5+i*2,.12f,0),2,i);o.Ambient=true;Patrol.Add(o);NpcSpeech.Say(o,"경찰입니다. 무기를 내려놓으세요!",4);}
        }
        public static void Shock(Vector3 at,string cause="danger")
        {
            if(!LocalSimulation.Combat(at))return;
            foreach(var npc in FindObjectsByType<CityNpc>())
                if(npc&&Vector3.Distance(npc.transform.position,at)<25&&npc.GetComponent<WorldActor>().Alive)
                {npc.Panic(at,7);NpcSpeech.Say(npc,NpcDialogueBank.Line(npc,cause),3);}
        }
        void Update()
        {
            if(!game||!game.Ready||game.Blocked)return;
            if(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame)PlayerReport();
            if(Time.time<next)return;next=Time.time+1;
            if(game.stage!=StageId.UrbanCity||!UrbanSimulation.Instance)return;
            const int capacity=5;int launched=0;
            if(ambulances>=capacity)return;
            ActorSpatialIndex.Nearby(game.Player.transform.position,LocalSimulation.CombatRadius,patients);patients.Sort((a,b)=>(b&&b.Downed?1:0).CompareTo(a&&a.Downed?1:0));
            foreach(var body in patients)
            {
                if(!body||!body.Alive||body.robot||body.monster||body.helicopter||body.GetComponent<MedicalPending>())continue;
                var injury=body.GetComponent<MedicalState>();if(!injury||!injury.NeedsRescue)continue;
                if(!injury.Reported)foreach(var witness in WorldActor.All){if(!witness||witness==body||!witness.Alive||witness.Downed||witness.gang||witness.monster||witness.environmental||witness.helicopter||witness.terrorist)continue;if((witness.Center-body.Center).sqrMagnitude<48*48&&FactionCombat.Visible(witness.Center,body.Center,48)){NpcSpeech.Say(witness,"119죠? 여기 사람이 다쳤어요! 구조대를 보내주세요!",5,9);injury.Report();break;}}
                if(!injury.Reported||ambulances>=capacity||launched>=1)continue;
                body.gameObject.AddComponent<MedicalPending>();ambulances++;launched++;
                EmergencyAmbulance.Create(body,()=>ambulances=Mathf.Max(0,ambulances-1));
            }
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
        readonly List<WorldActor> patients=new();
    }
}
