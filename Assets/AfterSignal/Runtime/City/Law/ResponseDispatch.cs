using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Dispatch uses the actual road graph and never teleports a visible response unit.
    public static class ResponseDispatch
    {
        static readonly List<Vector3> nodes=new();
        static readonly List<HashSet<int>> edges=new();
        static readonly Dictionary<Vector3Int,int> keys=new();
        public static string LastSearch {get;private set;}
        static int Node(Vector3 p)
        {
            var key=new Vector3Int(Mathf.RoundToInt(p.x/12),Mathf.RoundToInt(p.y/15),Mathf.RoundToInt(p.z/12));
            if(keys.TryGetValue(key,out int i))return i;
            i=nodes.Count;keys[key]=i;nodes.Add(p);edges.Add(new());return i;
        }
        static void Road(Vector3[] points)
        {
            int previous=Node(points[0]);
            for(int i=1;i<points.Length;i++)
            {
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(points[i-1],points[i])/10));
                for(int j=1;j<=count;j++){int n=Node(Vector3.Lerp(points[i-1],points[i],j/(float)count));edges[previous].Add(n);edges[n].Add(previous);previous=n;}
            }
        }
        static void Build()
        {
            if(nodes.Count>0)return;
            foreach(var road in ExpansionRoads.Roads)Road(road);
            foreach(var road in FourCityCatalog.Roads)Road(road);
            for(int x=0;x<6;x++)Road(new[]{CityRoadNetwork.Junction(x,0),CityRoadNetwork.Junction(x,4)});
            for(int z=0;z<5;z++)Road(new[]{CityRoadNetwork.Junction(0,z),CityRoadNetwork.Junction(5,z)});
        }
        public static bool Hidden(Vector3 p,float size=12)
        {
            var camera=Camera.main;if(!camera)return true;
            var screen=camera.WorldToViewportPoint(p+Vector3.up*3);
            float margin=.08f+size/Mathf.Max(50,screen.z);
            if(screen.z<0||screen.x< -margin||screen.x>1+margin||screen.y< -margin||screen.y>1+margin)return true;
            return Physics.Linecast(camera.transform.position,p+Vector3.up*size,1,QueryTriggerInteraction.Ignore);
        }
        public static bool TryOrigin(Vector3 target,bool army,bool aircraft,int index,out Vector3 at)
        {
            Build();float min=aircraft?650:220,max=aircraft?1800:950;
            if(aircraft)
            {
                for(int i=0;i<16;i++){float angle=(i*22.5f+index*41)*Mathf.Deg2Rad;at=target+new Vector3(Mathf.Cos(angle)*(min+index*55),140,Mathf.Sin(angle)*(min+index*55));if(Hidden(at,35))return true;}
                at=default;return false;
            }
            Vector3 station=army?ExpansionWorld.Places[10]:UrbanCatalog.Door(1);
            float nearest=(station-target).sqrMagnitude;
            foreach(var venue in FourCityCatalog.Venues)
                if(venue.kind==(army?VenueKind.Military:VenueKind.Police)&&!venue.id.Contains("ruined")&&(venue.Entrance-target).sqrMagnitude<nearest){station=venue.Entrance;nearest=(station-target).sqrMagnitude;}
            float best=float.MaxValue;at=default;bool found=false;int distant=0,hidden=0,grounded=0,clear=0;
            for(int i=0;i<nodes.Count;i++)
            {
                var p=nodes[i];float distance=Vector3.Distance(p,target);
                if(distance<min||distance>max||Mathf.Abs(p.y-(target.y< -30?-62:0))>20||RegionalCatalog.InRift(p,20))continue;distant++;if(!Hidden(p))continue;hidden++;
                float score=distance+Mathf.Min(500,Vector3.Distance(p,station))*.8f+Mathf.Abs((i%11)-index)*4;
                if(score>=best||!CityGangWar.FindGround(p,out var safe)||Mathf.Abs(safe.y-p.y)>3)continue;grounded++;
                if(Physics.CheckBox(safe+Vector3.up*2,new Vector3(2,1.5f,2),Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                if(Vector3.Distance(safe,target)<min||!Hidden(safe))continue;
                clear++;at=safe;best=score;found=true;
            }
            LastSearch=$"nodes={nodes.Count}, distant={distant}, hidden={hidden}, grounded={grounded}, clear={clear}, target={target}";return found;
        }
        static int Nearest(Vector3 p){int n=0;float best=float.MaxValue;for(int i=0;i<nodes.Count;i++){float d=(nodes[i]-p).sqrMagnitude;if(d<best){best=d;n=i;}}return n;}
        public static List<Vector3> Route(Vector3 from,Vector3 to)
        {
            Build();int start=Nearest(from),end=Nearest(to);var open=new List<int>{start};var closed=new HashSet<int>();var cost=new Dictionary<int,float>{{start,0}};var previous=new Dictionary<int,int>();bool reached=false;
            for(int steps=0;open.Count>0&&steps<12000;steps++)
            {
                int slot=0;float best=float.MaxValue;
                for(int j=0;j<open.Count;j++){int n=open[j];float f=cost[n]+Vector3.Distance(nodes[n],nodes[end]);if(f<best){best=f;slot=j;}}
                int k=open[slot];open.RemoveAt(slot);if(k==end){reached=true;break;}closed.Add(k);
                foreach(int n in edges[k]){if(closed.Contains(n))continue;float g=cost[k]+Vector3.Distance(nodes[k],nodes[n]);if(cost.TryGetValue(n,out float old)&&g>=old)continue;cost[n]=g;previous[n]=k;if(!open.Contains(n))open.Add(n);}
            }
            var path=new List<Vector3>();if(!reached)return path;
            int walk=end;path.Add(nodes[walk]);while(walk!=start&&previous.TryGetValue(walk,out int parent)){walk=parent;path.Add(nodes[walk]);}path.Reverse();return path;
        }
    }
    public sealed class ResponseDrive
    {
        List<Vector3> route;int index;Vector3 lastTarget;float next,stuck,reverse;
        public void Drive(CityVehicle car,Vector3 destination,float dt,float stop=26)
        {
            var p=car.transform.position;var delta=destination-p;delta.y=0;
            if(delta.magnitude<stop){car.Drive(new ControlFrame{guard=true},dt);return;}
            if(Time.time>=next&&(route==null||Vector3.Distance(destination,lastTarget)>60))
            {next=Time.time+8;route=ResponseDispatch.Route(p,destination);index=0;lastTarget=destination;}
            while(route!=null&&index<route.Count&&Vector3.ProjectOnPlane(route[index]-p,Vector3.up).magnitude<11)index++;
            var goal=route!=null&&index<route.Count?route[index]:destination;
            var d=Vector3.ProjectOnPlane(goal-p,Vector3.up);float angle=Vector3.SignedAngle(car.Forward,d,Vector3.up);
            stuck=Mathf.Abs(car.speed)<.6f?stuck+dt:0;
            if(stuck>3){reverse=1.4f;stuck=0;}reverse-=dt;
            var input=ControlFrame.Empty;input.move=new Vector2(Mathf.Clamp(angle/28,-1,1)*(reverse>0?-1:1),reverse>0?-.35f:Mathf.Abs(angle)>65?.22f:.72f);car.Drive(input,dt);
        }
    }
}
