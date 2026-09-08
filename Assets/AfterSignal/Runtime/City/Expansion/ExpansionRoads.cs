using System;using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public static class ExpansionRoads
    {
        public const float Width=2200,Depth=5650,South=NeonHarbor.South;
        public static bool Outside(Vector3 p)=>p.x>778||p.z>315||p.z< -315;
        public static Vector2 Map(Vector3 p)=>new Vector2(Mathf.Clamp01(p.x/Width),Mathf.Clamp01((p.z-South)/Depth));
        static Vector3 P(float x,float z)=>new Vector3(x,.035f,z);
        public static readonly Vector3[][] Controls={
            new[]{P(40,-280),P(40,-380),P(260,-450),P(480,-510),P(780,-560),P(1030,-500),P(1210,-470),P(1380,-450),P(1580,-450),P(1830,-450),P(2080,-450)},
            new[]{P(740,0),P(900,10),P(1120,120),P(1300,170),P(1500,180),P(1760,250),P(1940,390)},
            new[]{P(740,280),P(850,400),P(1060,550),P(1280,540),P(1500,450),P(1740,440),P(1940,390),P(2100,470),P(2040,330),P(1760,250)},
            new[]{P(740,-140),P(890,-190),P(1070,-300),P(1210,-470),P(1350,-320),P(1580,-300),P(1830,-300),P(2080,-450)},
            new[]{P(1120,120),P(1140,-70),P(1070,-300)},
            new[]{P(740,280),P(690,420),P(760,610),P(920,700),P(1060,550)},
            new[]{P(1500,180),P(1500,-80),P(1580,-300)},
            new[]{P(780,-560),P(830,-400),P(890,-190)},
            new[]{P(1500,450),P(1530,500),P(1700,510),P(1900,510),P(1940,390)},
            new[]{P(740,140),P(820,140),P(930,140),P(1120,120)},
            new[]{P(740,-280),P(815,-280),P(870,-252),P(890,-190)},
            new[]{P(180,-280),P(180,-370),P(260,-450)},
            new[]{P(460,-280),P(460,-390),P(480,-510)},
            new[]{P(180,280),P(180,450),P(270,620),P(420,700),P(690,710),P(920,700)},
            new[]{P(460,280),P(460,415),P(560,580),P(690,710)},
            new[]{P(1060,550),P(1160,640),P(1180,700),P(1200,730)},
            new[]{P(1210,-470),P(1270,-550),P(1250,-637),P(1250,-666)}
        };
        static List<Vector3[]> roads;
        public static List<Vector3[]> Roads
        {
            get{if(roads!=null)return roads;roads=new List<Vector3[]>();foreach(var c in Controls){var r=new List<Vector3>();for(int i=0;i<c.Length-1;i++){var a=c[Mathf.Max(0,i-1)];var b=c[i];var d=c[i+1];var e=c[Mathf.Min(c.Length-1,i+2)];int n=Mathf.CeilToInt(Vector3.Distance(b,d)/10);for(int j=0;j<n;j++){float t=j/(float)n;var q=.5f*((2*b)+(-a+d)*t+(2*a-5*b+4*d-e)*t*t+(-a+3*b-3*d+e)*t*t*t);q.y=.035f;r.Add(q);}}r.Add(c[c.Length-1]);roads.Add(r.ToArray());}roads.AddRange(NeonHarbor.Roads);return roads;}
        }
        public static Vector3 Nearest(Vector3 point,out int road,out int sample)
        {
            float best=float.MaxValue;road=sample=0;for(int r=0;r<Roads.Count;r++)for(int i=0;i<Roads[r].Length;i++){float d=(point-Roads[r][i]).sqrMagnitude;if(d<best){best=d;road=r;sample=i;}}return Roads[road][sample];
        }
        public static Vector3 Sidewalk(Vector3 p,int advance=0)
        {
            Nearest(p,out int r,out int i);var path=Roads[r];i=Mathf.Clamp(i+advance,0,path.Length-1);var tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(Vector3.up,tangent);float sign=Vector3.Dot(p-path[i],side)>=0?1:-1;return path[i]+side*sign*15+Vector3.up*.06f;
        }
        public static Vector3[] TrafficLoop(Vector3 p)
        {
            Nearest(p,out int r,out _);var path=Roads[r];var list=new List<Vector3>();
            for(int i=0;i<path.Length;i++){var d=path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)];list.Add(path[i]+Vector3.Cross(Vector3.up,d.normalized)*4);}
            for(int i=path.Length-1;i>=0;i--){var d=path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)];list.Add(path[i]-Vector3.Cross(Vector3.up,d.normalized)*4);}return list.ToArray();
        }
        public static List<Vector3> Navigation(Vector3 from,Vector3 to)
        {
            var points=new List<Vector3>();var edges=new List<List<int>>();var keys=new Dictionary<Vector2Int,int>();
            int Node(Vector3 p){var key=new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.z));if(keys.TryGetValue(key,out int n))return n;n=points.Count;points.Add(p);edges.Add(new List<int>());keys[key]=n;return n;}
            void Link(Vector3 a,Vector3 b){int x=Node(a),y=Node(b);edges[x].Add(y);edges[y].Add(x);}
            foreach(var road in Roads)for(int i=1;i<road.Length;i++)Link(road[i-1],road[i]);
            for(int x=0;x<6;x++)for(int z=0;z<5;z++){var a=CityRoadNetwork.Junction(x,z);if(x<5)Link(a,CityRoadNetwork.Junction(x+1,z));if(z<4)Link(a,CityRoadNetwork.Junction(x,z+1));}
            int NearestNode(Vector3 p){int best=0;float d=float.MaxValue;for(int i=0;i<points.Count;i++){float v=(points[i]-p).sqrMagnitude;if(v<d){best=i;d=v;}}return best;}
            Link(NeonHarbor.OldDock,NeonHarbor.NewDock);Link(NeonHarbor.OldDock,new Vector3(1250,.035f,-666));Link(NeonHarbor.NewDock,new Vector3(920,.035f,-2412));int start=NearestNode(from),end=NearestNode(to);var dist=new float[points.Count];var prev=new int[points.Count];var used=new bool[points.Count];Array.Fill(dist,float.PositiveInfinity);Array.Fill(prev,-1);dist[start]=0;
            for(int step=0;step<points.Count;step++){int k=-1;for(int i=0;i<points.Count;i++)if(!used[i]&&(k<0||dist[i]<dist[k]))k=i;if(k<0||float.IsInfinity(dist[k]))break;if(k==end)break;used[k]=true;foreach(int n in edges[k]){float d=dist[k]+Vector3.Distance(points[k],points[n]);if(d<dist[n]){dist[n]=d;prev[n]=k;}}}
            var result=new List<Vector3>{to};int walk=end;while(walk>=0){result.Add(points[walk]);if(walk==start)break;walk=prev[walk];}result.Add(from);result.Reverse();return result;
        }
    }
}
