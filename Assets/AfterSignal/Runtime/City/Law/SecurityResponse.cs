using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class SecurityResponse:MonoBehaviour
    {
        static readonly HashSet<int> requested=new();WorldActor target;int key;bool disaster;
        static readonly List<SecurityResponse> commands=new();
        public static void Request(WorldActor suspect,bool monster)
        {
            if(suspect&&!suspect.terrorist&&MilitaryBaseOperations.Handles(suspect.transform.position))return;
            if(!suspect||!LocalSimulation.Combat(suspect.transform.position)||!UrbanSimulation.Instance||!WantedSystem.Instance)return;
            foreach(var c in commands)if(c&&c.target&&c.target.Alive&&(c.target.transform.position-suspect.transform.position).sqrMagnitude<320*320){if(monster){c.disaster=true;c.target=suspect;}return;}
            if(commands.Count>=3)return;
            int id=suspect.GetInstanceID();if(!requested.Add(id))return;
            var r=new GameObject("현장 통합 대응 / 순찰·특수대응팀").AddComponent<SecurityResponse>();r.target=suspect;r.key=id;r.disaster=monster;
            commands.Add(r);
        }
        IEnumerator Start()
        {
            yield return new WaitForSeconds(disaster?12:8);
            for(int i=0;i<(disaster?5:3)&&target&&target.Alive&&!target.Downed;i++)
            {
                int attempts=0;Vector3 origin;
                while(target&&target.Alive&&!ResponseDispatch.TryOrigin(target.transform.position,false,false,i,out _)&&attempts++<20)yield return new WaitForSeconds(3);
                if(!target||!target.Alive||!LocalSimulation.Combat(target.transform.position))break;
                if(!target.terrorist&&MilitaryBaseOperations.Handles(target.transform.position))continue;
                if(ResponseDispatch.TryOrigin(target.transform.position,false,false,i,out origin))
                {var car=TacticalTransport.Create(WantedSystem.Instance,origin,i<2?2:5,i<2?2:4);car.AssignIncident(target);}
                yield return new WaitForSeconds(disaster?6:10);
            }
            while(target&&target.Alive&&!target.Downed&&LocalSimulation.Combat(target.transform.position))yield return new WaitForSeconds(8);
            Destroy(gameObject);
        }
        void OnDestroy(){requested.Remove(key);commands.Remove(this);}
    }
}
