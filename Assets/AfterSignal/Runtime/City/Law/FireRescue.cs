using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // The rescue crew claims one person at a time and hands critical casualties to EMS in safety.
    public sealed class FireRescue:MonoBehaviour
    {
        static readonly HashSet<WorldActor> claimed=new();
        readonly List<Behaviour> suspended=new();readonly List<Collider> colliders=new();
        readonly PursuitPath path=new();WorldActor patient;MedicalPending pending;MedicalState medical;
        Vector3 safe,fireOrigin;float scan,age,stuck;Vector3 previous;bool carrying,ownsPending;DirectionalPerson art;FireRescueClaim claim;
        public bool Busy=>patient;
        public int Rescued {get;private set;}
        public void Cancel()=>Release(false);
        public string Decision=>carrying?"시민을 안전지대로 구조":"위험에 처한 시민에게 접근";
        public bool Tick(FireEngine truck,BurningObject fire,WorldActor rescuer,float dt,bool canSearch)
        {
            if(!rescuer||!rescuer.Alive||rescuer.Downed){Release(false);return false;}
            if(!patient&&canSearch&&fire&&Time.time>=scan)
            {
                scan=Time.time+1.3f;float best=float.MaxValue;
                foreach(var a in WorldActor.All)
                {
                    if(!a||!a.Alive||a.environmental||a.robot||a.monster||a.helicopter||a.gang||a.GetComponent<Firefighter>()||a.GetComponent<MedicalPending>()&&a.GetComponent<MedicalPending>().carried||claimed.Contains(a))continue;
                    var m=a.GetComponent<MedicalState>();bool hurt=m&&m.NeedsRescue;
                    if((a.transform.position-fire.Position).sqrMagnitude>(hurt?30*30:12*12)||Mathf.Abs(a.transform.position.y-fire.Position.y)>8||!hurt&&(a.police||a.military))continue;
                    float score=(a.transform.position-transform.position).sqrMagnitude*(hurt?.25f:1);
                    if(score<best){best=score;patient=a;medical=m;}
                }
                if(patient)
                {
                    claimed.Add(patient);fireOrigin=fire.Position;age=stuck=0;previous=transform.position;
                    pending=patient.GetComponent<MedicalPending>();ownsPending=!pending;if(!pending)pending=patient.gameObject.AddComponent<MedicalPending>();claim=patient.gameObject.AddComponent<FireRescueClaim>();
                    var away=Vector3.ProjectOnPlane(patient.transform.position-fireOrigin,Vector3.up).normalized;
                    if(away.sqrMagnitude<.1f)away=Vector3.ProjectOnPlane(truck.transform.position-fireOrigin,Vector3.up).normalized;
                    safe=fireOrigin+away*30;safe.y=patient.transform.position.y;
                    if(!NpcGroundSupport.Floor(safe,safe.y+3,8,out float floor))safe=truck.transform.position-truck.Car.Forward*10;
                    else safe.y=floor+.08f;
                    NpcSpeech.Say(this,medical&&medical.Incapacitated?"움직이지 마세요. 제가 안전한 곳으로 옮기겠습니다!":"소방대입니다. 제 옆으로 오세요! 안전한 곳으로 안내할게요.",5,5);
                }
            }
            if(!patient)return false;
            if(!patient.Alive||age>45){Release(false);return false;}age+=dt;
            var goal=carrying?safe:patient.transform.position;
            if(!carrying&&(patient.transform.position-transform.position).sqrMagnitude<2.6f*2.6f)
            {
                carrying=true;pending.carried=true;medical?.Stabilize();art=patient.GetComponent<DirectionalPerson>();if(art)art.Lying=medical&&medical.Incapacitated;
                foreach(var b in patient.GetComponents<MonoBehaviour>())
                {
                    string n=b.GetType().Name;
                    if(b.enabled&&(n=="CityNpc"||n=="CityPedestrian"||n=="CivicRoutine"||n=="ResidentWalker"||n=="VehicleSurvivor"||n=="CivilianDefense"||n=="VenueActor"||n=="PoliceOfficer"||n=="ArmyResponder")){suspended.Add(b);b.enabled=false;}
                }
                foreach(var c in patient.GetComponentsInChildren<Collider>())if(c.enabled){colliders.Add(c);c.enabled=false;}
                goal=safe;
            }
            float speed=carrying&&medical&&medical.Incapacitated?2.6f:3.6f;
            var d=path.Direction(transform.position,goal);PedestrianSteering.For(this).Move(transform.position+d*3,dt*speed);
            if((transform.position-previous).sqrMagnitude<.0002f)stuck+=dt;else stuck=0;previous=transform.position;
            if(stuck>5){Release(false);scan=Time.time+8;return false;}
            if(carrying)
            {
                bool critical=medical&&medical.Incapacitated;
                patient.transform.position=transform.position+(critical?Vector3.up*1.1f:Vector3.Cross(Vector3.up,d).normalized*.65f);
                if((transform.position-safe).sqrMagnitude<9){Release(true);return false;}
            }
            return true;
        }
        void Release(bool completed)
        {
            if(patient)
            {
                if(carrying)
                {
                    var p=transform.position;var d=Vector3.ProjectOnPlane(p-fireOrigin,Vector3.up).normalized;p+=d*1.2f;
                    if(NpcGroundSupport.Floor(p,p.y+2,8,out float floor))p.y=floor+.12f;
                    patient.transform.position=p;
                }
                if(art)art.Lying=medical&&medical.Incapacitated;
                foreach(var c in colliders)if(c)c.enabled=true;
                foreach(var b in suspended)if(b)b.enabled=true;
                if(completed)
                {
                    Rescued++;
                    if(medical&&medical.NeedsRescue){if(medical.Incapacitated){medical.Stabilize();medical.Report();}else medical.FirstAid();}
                    NpcSpeech.Say(this,medical&&medical.Incapacitated?"중상자 확보! 안전지대에서 구급대에 인계합니다.":"이제 안전합니다. 응급처치 후 이곳에서 쉬세요.",5,4);
                }
                claimed.Remove(patient);
            }
            if(pending){pending.carried=false;if(ownsPending)Destroy(pending);}if(claim)Destroy(claim);claim=null;pending=null;patient=null;medical=null;art=null;carrying=false;colliders.Clear();suspended.Clear();
        }
        void OnDestroy()=>Release(false);
    }
    public sealed class FireRescueClaim:MonoBehaviour{}
}
