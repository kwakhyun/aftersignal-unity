using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class EscortFollower:MonoBehaviour
    {
        public bool Following{get;private set;}public int Recoveries{get;private set;}
        readonly PursuitPath path=new();readonly List<Vector3> breadcrumbs=new();float sample,stuck;Vector3 previous;
        public void Tick(PlayerMotor player,Vector3 extraction,float dt)
        {
            var at=transform.position;float distance=Vector3.Distance(at,player.transform.position);
            if(distance<18)Following=true;if(!Following)return;
            if(Time.time>sample){sample=Time.time+.5f;if(PedestrianGround.Stand(player.transform.position,player.transform.position.y,.8f,out var safe)){breadcrumbs.Add(safe);if(breadcrumbs.Count>80)breadcrumbs.RemoveAt(0);}}
            Vector3 goal=Vector3.Distance(player.transform.position,extraction)<6?extraction:player.transform.position;
            if(distance<2&&Vector3.Distance(goal,extraction)>1){stuck=0;return;}
            Vector3 dir=path.Direction(at,goal);float step=Mathf.Min(dt*(distance>7?5:3.6f),Vector3.Distance(at,goal));
            if(PedestrianGround.Step(at,at+dir*step,out var next,1.05f))transform.position=next;
            GetComponent<DirectionalPerson>()?.Face(goal,.15f);
            stuck=(transform.position-previous).sqrMagnitude<dt*dt*.2f?stuck+dt:0;previous=transform.position;
            if(stuck>1.4f)path.Invalidate();
            // Recover only on previously visited, supported ground, never inside a wall or another floor.
            if(stuck>5||distance>32)
            {
                for(int i=breadcrumbs.Count-1;i>=0;i--){var p=breadcrumbs[i];float d=Vector3.Distance(p,player.transform.position);if(d<3||d>12||!CrowdFlow.Vacant(p,.8f)||!PedestrianGround.Stand(p,p.y,.5f,out var safe))continue;transform.position=safe;Recoveries++;stuck=0;path.Invalidate();NpcSpeech.Say(this,"돌아가는 길을 찾았어요. 계속 따라갈게요!",3,3);break;}
            }
        }
    }
}
