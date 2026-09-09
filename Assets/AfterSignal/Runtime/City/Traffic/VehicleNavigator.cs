using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Bounded A* searches the actual vehicle clearance, then retains the detour.
    // Only one search can run per frame; waiting vehicles brake instead of hitting the wall.
    public sealed class VehicleNavigator
    {
        const int Side=49, Count=Side*Side, Middle=Side/2;
        static int searchFrame=-1;
        static readonly Collider[] overlaps=new Collider[64];
        static readonly RaycastHit[] hits=new RaycastHit[48];
        readonly List<Vector3> path=new();int waypoint;float scan,retry;Vector3 destination;
        public bool Detouring=>waypoint<path.Count;
        public bool Waiting {get;private set;}
        public IReadOnlyList<Vector3> Path=>path;
        public static bool Clear(CityVehicle car,Vector3 a,Vector3 b)
        {
            var d=b-a;d.y=0;float length=d.magnitude;if(length<.02f)return true;
            int n=Physics.BoxCastNonAlloc(a+Vector3.up*1.15f,new Vector3(car.HalfWidth+.32f,.57f,.15f),d/length,hits,Quaternion.LookRotation(d),length,1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(Obstacle(car,hits[i].collider)&&hits[i].normal.y<.68f)return false;
            return true;
        }
        static bool Obstacle(CityVehicle car,Collider c)=>c&&!c.transform.IsChildOf(car.transform)&&!c.GetComponentInParent<CityVehicle>()&&!c.GetComponentInParent<WorldActor>()&&!c.GetComponentInParent<PlayerMotor>();
        public static bool Open(CityVehicle car,Vector3 at,float radius=0)
        {
            if(!VehicleGround.Sample(car,at,.65f,1.1f,out var floor)||Mathf.Abs(floor.point.y-at.y)>.48f)return false;
            radius=Mathf.Max(car.HalfWidth+.35f,radius);
            int n=Physics.OverlapBoxNonAlloc(at+Vector3.up*1.15f,new Vector3(radius,.57f,radius),overlaps,Quaternion.identity,1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(Obstacle(car,overlaps[i]))return false;
            return n<overlaps.Length;
        }
        public Vector3 Direction(CityVehicle car,Vector3 goal,float stop=1)
        {
            var p=car.transform.position;goal.y=p.y;var direct=goal-p;
            if((goal-destination).sqrMagnitude>100){path.Clear();waypoint=0;scan=retry=0;Waiting=false;destination=goal;}
            if(Detouring)
            {
                while(waypoint<path.Count&&(path[waypoint]-p).sqrMagnitude<9)waypoint++;
                if(Detouring)
                {
                    if(Time.time>=scan){scan=Time.time+.6f;if(!Clear(car,p,path[waypoint])){path.Clear();waypoint=0;retry=0;} }
                    if(Detouring){Waiting=false;return (path[waypoint]-p).normalized;}
                }
            }
            if(Time.time<scan&&!Waiting)return direct.normalized;
            scan=Time.time+.4f;
            float look=Mathf.Min(direct.magnitude,car.HalfLength+Mathf.Max(12,Mathf.Abs(car.speed)*1.5f));
            if(Clear(car,p,p+direct.normalized*look)){Waiting=false;return direct.normalized;}
            Waiting=true;
            if(Time.time>=retry&&searchFrame!=Time.frameCount)
            {searchFrame=Time.frameCount;retry=Time.time+2;Find(car,goal,stop);if(Detouring){Waiting=false;return (path[0]-p).normalized;}}
            return direct.normalized;
        }
        public bool Find(CityVehicle car,Vector3 goal,float stop=1)
        {
            path.Clear();waypoint=0;var origin=car.transform.position;goal.y=origin.y;destination=goal;
            float step=Mathf.Max(4,car.HalfWidth*2+.8f);var target=origin+Vector3.ClampMagnitude(goal-origin,(Middle-3)*step);
            var costs=new float[Count];var parents=new int[Count];var state=new byte[Count];var terrain=new byte[Count];
            var open=new MinHeap();int start=Middle*Side+Middle,end=-1,best=start;float bestH=Vector3.Distance(origin,target);
            open.Push(start,bestH);costs[start]=0;state[start]=1;
            Vector3 Position(int id)=>origin+new Vector3(id%Side-Middle,0,id/Side-Middle)*step;
            bool Walkable(int id){if(terrain[id]==0)terrain[id]=(byte)(Open(car,Position(id),Mathf.Min(car.HalfLength*.65f,3))?1:2);return terrain[id]==1;}
            for(int expanded=0;open.Count>0&&expanded<850;expanded++)
            {
                int id=open.Pop();if(state[id]==2){expanded--;continue;}state[id]=2;var at=Position(id);float h=Vector3.Distance(at,target);
                if(h<bestH){best=id;bestH=h;}
                if(h<Mathf.Max(step*.85f,stop)&&Clear(car,at,target)){end=id;break;}
                int x=id%Side,z=id/Side;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dz==0||x+dx<0||x+dx>=Side||z+dz<0||z+dz>=Side)continue;
                    int next=(z+dz)*Side+x+dx;if(state[next]==2||!Walkable(next))continue;
                    if(dx!=0&&dz!=0&&(!Walkable(z*Side+x+dx)||!Walkable((z+dz)*Side+x)))continue;
                    float cost=costs[id]+step*(dx!=0&&dz!=0?1.414214f:1);
                    if(state[next]!=0&&cost>=costs[next])continue;
                    if(!Clear(car,at,Position(next)))continue;
                    state[next]=1;costs[next]=cost;parents[next]=id;open.Push(next,cost+Vector3.Distance(Position(next),target));
                }
            }
            if(end<0){if(best==start||bestH>Vector3.Distance(origin,target)-step*2)return false;end=best;}
            for(int id=end;id!=start;id=parents[id])path.Add(Position(id));path.Reverse();
            // Only remove corners when the whole chassis corridor remains clear.
            for(int i=0;i+2<path.Count;)if(Clear(car,i==0?origin:path[i-1],path[i+1]))path.RemoveAt(i);else i++;
            scan=Time.time+.6f;return path.Count>0;
        }
        internal sealed class MinHeap
        {
            readonly List<(int id,float score)> data=new();public int Count=>data.Count;
            public void Push(int id,float score){int i=data.Count;data.Add((id,score));while(i>0){int p=(i-1)/2;if(data[p].score<=score)break;data[i]=data[p];i=p;}data[i]=(id,score);}
            public int Pop(){int result=data[0].id;var last=data[data.Count-1];data.RemoveAt(data.Count-1);if(data.Count==0)return result;int i=0;while(i*2+1<data.Count){int c=i*2+1;if(c+1<data.Count&&data[c+1].score<data[c].score)c++;if(data[c].score>=last.score)break;data[i]=data[c];i=c;}data[i]=last;return result;}
        }
    }
}
