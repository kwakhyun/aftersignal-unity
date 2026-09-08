using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class FacilitySecurity:MonoBehaviour
    {
        public static FacilitySecurity Instance{get;private set;}
        public bool Alerted{get;private set;}
        static bool Station=>GameDirector.Instance&&GameDirector.Instance.stage==StageId.UrbanInterior&&UrbanCatalog.Kind(UrbanCatalog.Current)==1;
        void Awake()=>Instance=this;
        IEnumerator Start(){yield return new WaitForSeconds(2);if(Station)Register();}
        void Register()
        {
            foreach(var npc in FindObjectsByType<CityNpc>())
            {
                var look=npc.GetComponent<DirectionalPerson>();string role=npc.occupation??"";
                if(look&&look.art=="Prisoner"||role.Contains("죄수")||role.Contains("민원")||role.Contains("방문"))continue;
                var body=npc.GetComponent<WorldActor>();if(!body||!body.Alive)continue;
                body.police=true;body.protectedResident=false;
                if(!npc.GetComponent<StationDefender>())npc.gameObject.AddComponent<StationDefender>();
                PeopleArt.Attach(npc.gameObject,"Police");
            }
        }
        public static void Alert(Vector3 at)
        {
            RegionalGuard.Alert(at);
            if(!Instance||!Station)return;Instance.Register();Instance.Alerted=true;
            foreach(var guard in FindObjectsByType<StationDefender>())guard.Engage();
        }
        void Update(){if(Alerted&&WantedSystem.Level==0&&CrimeObservation.Instance&&CrimeObservation.Instance.PendingCalls==0)Alerted=false;}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
