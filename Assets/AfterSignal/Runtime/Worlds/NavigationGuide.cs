using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class NavigationGuide
    {
        public List<Vector3> Route {get;private set;}
        public bool Connected {get;private set;}
        public bool Arrived {get;private set;}
        public float Remaining {get;private set;}
        public float TurnDistance {get;private set;}
        public string Instruction {get;private set;}="경로 탐색 중";
        public string Arrow {get;private set;}="↑";
        public int Recalculations {get;private set;}
        public Vector3 Next {get;private set;}
        Vector3 lastGoal,calculatedAt;float nextRepath;int segment;
        public void Reset(){Route=null;Arrived=false;nextRepath=0;}
        public bool Update(Vector3 at,Vector3 goal,Vector3 forward)
        {
            float deviation=Route==null?float.MaxValue:Nearest(at,out _);bool changed=false;
            if(Route==null||(lastGoal-goal).sqrMagnitude>1||Time.unscaledTime>nextRepath&&(deviation>18||(at-calculatedAt).sqrMagnitude>45*45))
            {
                Route=FourCityNavigation.Route(at,goal,out bool connected);Connected=connected;lastGoal=goal;calculatedAt=at;nextRepath=Time.unscaledTime+2;segment=0;Recalculations++;changed=true;
            }
            Arrived=Vector3.Distance(at,goal)<7;
            Nearest(at,out var projection);Remaining=Vector3.Distance(at,projection);
            for(int i=segment+1;i<Route.Count;i++){Remaining+=Vector3.Distance(projection,Route[i]);projection=Route[i];}
            Next=Route[Mathf.Min(segment+1,Route.Count-1)];TurnDistance=Vector3.Distance(at,Next);Arrow="↑";Instruction="도로를 따라 이동";
            if(Arrived){Instruction="목적지 도착";Arrow="◆";Remaining=0;}
            else if(!Connected){Instruction="도로 연결 없음 · 교통편 확인";Arrow="◇";Remaining=Vector3.Distance(at,goal);}
            else if(Mathf.Abs(goal.y-at.y)>5&&Vector2.Distance(new(at.x,at.z),new(goal.x,goal.z))<18){Instruction=goal.y>at.y?"승강기 / 계단으로 위층 이동":"승강기 / 계단으로 아래층 이동";Arrow=goal.y>at.y?"↑":"↓";}
            else
            {
                var heading=Vector3.ProjectOnPlane(Next-at,Vector3.up);
                float entryTurn=Vector3.SignedAngle(forward,heading,Vector3.up);
                if(heading.sqrMagnitude>16&&Vector3.Dot(heading.normalized,forward.normalized)<-.6f){Arrow="↶";Instruction="반대 방향으로 돌아 이동";}
                else if(heading.sqrMagnitude>16&&Mathf.Abs(entryTurn)>55){Arrow=entryTurn>0?"↱":"↰";Instruction=entryTurn>0?"오른쪽 경로로 진입":"왼쪽 경로로 진입";TurnDistance=0;}
                else
                {
                    float along=Vector3.Distance(at,Next);
                    for(int i=segment+1;i<Route.Count-1;i++)
                    {
                        var a=Route[i]-Route[i-1];var b=Route[i+1]-Route[i];a.y=b.y=0;
                        if(a.sqrMagnitude>.1f&&b.sqrMagnitude>.1f){float turn=Vector3.SignedAngle(a,b,Vector3.up);if(Mathf.Abs(turn)>28){TurnDistance=along;Arrow=turn>0?"↱":"↰";Instruction=turn>0?"우회전":"좌회전";break;}}
                        along+=Vector3.Distance(Route[i],Route[i+1]);
                    }
                }
                if(segment>=Route.Count-2){Arrow="◆";Instruction="목적지 입구로 이동";}
                for(int i=segment;i<Mathf.Min(segment+2,Route.Count-1);i++)if(Vector3.Distance(Route[i],NeonHarbor.OldDock)<45&&Vector3.Distance(Route[i+1],NeonHarbor.NewDock)<45||Vector3.Distance(Route[i],NeonHarbor.NewDock)<45&&Vector3.Distance(Route[i+1],NeonHarbor.OldDock)<45){Arrow="≈";Instruction="항구에서 도시 간 선박 이용";}
            }
            return changed;
        }
        float Nearest(Vector3 at,out Vector3 closest)
        {
            closest=at;if(Route==null||Route.Count<2)return float.MaxValue;float best=float.MaxValue;int selected=segment;
            for(int i=Mathf.Max(0,segment-1);i<Route.Count-1;i++)
            {
                var d=Route[i+1]-Route[i];float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(at-Route[i],d)/d.sqrMagnitude):0;var p=Route[i]+d*t;float n=(p-at).sqrMagnitude;
                if(n<best){best=n;closest=p;selected=i;}
            }
            segment=selected;return Mathf.Sqrt(best);
        }
        public static string Bearing(Vector3 forward)
        {float angle=Mathf.Repeat(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,360);string[] labels={"N 북","NE 북동","E 동","SE 남동","S 남","SW 남서","W 서","NW 북서"};return labels[Mathf.RoundToInt(angle/45)%8]+"  "+Mathf.RoundToInt(angle).ToString("000")+"°";}
    }
}
