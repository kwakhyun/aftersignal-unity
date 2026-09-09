using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public enum InjuryGrade { None, Wounded, Critical, Dead }
    public sealed class MedicalState:MonoBehaviour
    {
        public InjuryGrade Grade {get;private set;}
        public bool Incapacitated=>Grade==InjuryGrade.Critical;
        public bool Stabilized {get;private set;}
        public bool Bandaged {get;private set;}
        public bool Reported {get;private set;}
        public bool NeedsRescue=>body&&body.Alive&&(Grade==InjuryGrade.Wounded||Incapacitated);
        WorldActor body;float expires,callAt;Collider prone;
        readonly List<Behaviour> suspended=new();readonly List<Collider> colliders=new();
        public void Report(){if(!NeedsRescue||Reported)return;Reported=true;CitySafety.Instance?.RequestRescue(body);}
        public void Wound(WorldActor actor,float before,float damage)
        {
            body=actor;Bandaged=false;
            // Lethal high-energy hits and follow-up shots never resurrect an incapacitated person.
            if(body.health<=0&&(Incapacitated||damage>=body.MaxHealth*.9f||before<=body.MaxHealth*.2f))
            {Grade=InjuryGrade.Dead;return;}
            if(Incapacitated){if(body.health<=0)Grade=InjuryGrade.Dead;return;}
            if(body.health<=body.MaxHealth*.2f)
            {
                body.health=Mathf.Max(1,body.health);Grade=InjuryGrade.Critical;expires=Time.time+180;
                foreach(var component in GetComponents<MonoBehaviour>())
                {
                    string n=component.GetType().Name;
                    if(component.enabled&&(n=="PoliceOfficer"||n=="ArmyResponder"||n=="GangMember"||n=="CityPedestrian"||n=="CityNpc"||n=="CivicRoutine"||n=="VenueActor"||n=="GangCrime"||n=="ResidentWalker"||n=="RegionalGuard"||n=="StationDefender"||n=="CivilianDefense"||n=="VehicleSurvivor"||n=="TerrorSuspect"))
                    {suspended.Add(component);component.enabled=false;}
                }
                foreach(var c in GetComponents<Collider>())if(c.enabled){colliders.Add(c);c.enabled=false;}
                prone=ProneHitVolume.Create(body);
                var art=GetComponent<DirectionalPerson>();if(art)art.Lying=true;
            }
            else if(damage>=body.MaxHealth*.08f||body.health<body.MaxHealth*.7f)Grade=InjuryGrade.Wounded;
            if(Grade!=InjuryGrade.None&&!GetComponent<InjuryPresentation>())gameObject.AddComponent<InjuryPresentation>();
            if(NeedsRescue)Call();
        }
        void Call()
        {
            if(Time.time<callAt)return;callAt=Time.time+24;
            NpcSpeech.Say(body,body.gang?"젠장, 맞았어! 엄호해!":body.military?"대원 부상! 의무 지원 요청한다!":body.police?"대원 부상! 구급 지원 바란다.":Incapacitated?"움직일 수 없어요… 구조대를 불러주세요!":"다쳤어요! 응급처치가 필요해요!",5,10);
            // Conscious people and radios can report; unconscious civilians need a witness/player.
            if(!Incapacitated||body.police||body.military)Report();
        }
        void Update()
        {
            if(!body||!body.Alive)return;if(NeedsRescue)Call();
            if(Incapacitated&&!Stabilized&&Time.time>expires){body.health=0;Grade=InjuryGrade.Dead;GetComponent<DirectionalPerson>()?.PreserveAppearance();Invoke(nameof(Retire),90);}
        }
        void Retire(){if(GetComponent<CityPedestrian>())gameObject.SetActive(false);else Destroy(gameObject);}
        void RestoreComponents()
        {
            if(prone){prone.enabled=false;Destroy(prone.gameObject);prone=null;}
            foreach(var c in colliders)if(c)c.enabled=true;colliders.Clear();
            foreach(var b in suspended)if(b)b.enabled=true;suspended.Clear();
            var art=GetComponent<DirectionalPerson>();if(art)art.Lying=false;
        }
        public void ResetForReuse(){Grade=InjuryGrade.None;Stabilized=Bandaged=Reported=false;CancelInvoke();RestoreComponents();}
        public void Stabilize(){if(!body||!body.Alive)return;Stabilized=Bandaged=true;}
        public bool FirstAid()
        {
            if(!NeedsRescue||Incapacitated)return false;
            body.health=Mathf.Max(body.health,body.MaxHealth*.85f);Grade=InjuryGrade.None;Stabilized=false;Bandaged=true;Reported=false;return true;
        }
        public void Recover(Vector3 at)
        {
            if(!body||!body.Alive)return;
            transform.position=at;body.health=Mathf.Max(45,body.MaxHealth*.85f);Grade=InjuryGrade.None;Stabilized=false;Bandaged=true;Reported=false;RestoreComponents();
            var walker=GetComponent<CityPedestrian>();if(walker)walker.ResetAt(at,at+Vector3.right*8,walker.poses);
            foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=true;
        }
    }
}
