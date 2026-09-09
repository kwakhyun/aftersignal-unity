using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class FourCityNavigation
    {
        sealed class Segment {public Vector3 a,b;public readonly List<float> cuts=new(){0,1};public Segment(Vector3 a,Vector3 b){this.a=a;this.b=b;}}
        static List<Vector3> nodes;static List<List<int>> links;
        public static int NodeCount=>nodes?.Count??0;
        static void Build()
        {
            var segments=new List<Segment>();void Add(Vector3[] p){for(int i=1;i<p.Length;i++)if((p[i]-p[i-1]).sqrMagnitude>.1f)segments.Add(new(p[i-1],p[i]));}
            foreach(var p in ExpansionRoads.Roads)Add(p);foreach(var p in FourCityCatalog.Roads)Add(p);
            for(int x=0;x<6;x++)for(int z=0;z<5;z++){var a=CityRoadNetwork.Junction(x,z);if(x<5)Add(new[]{a,CityRoadNetwork.Junction(x+1,z)});if(z<4)Add(new[]{a,CityRoadNetwork.Junction(x,z+1)});}
            Add(new[]{new Vector3(1250,.03f,-666),NeonHarbor.OldDock,NeonHarbor.NewDock,new Vector3(920,.03f,-2412)});
            // Split every crossing before creating the graph. Grade-separated roads never join.
            for(int i=0;i<segments.Count;i++)for(int j=i+1;j<segments.Count;j++)
            {
                var a=segments[i];var b=segments[j];Vector2 p=new(a.a.x,a.a.z),r=new(a.b.x-a.a.x,a.b.z-a.a.z),q=new(b.a.x,b.a.z),s=new(b.b.x-b.a.x,b.b.z-b.a.z);
                float cross=r.x*s.y-r.y*s.x;if(Mathf.Abs(cross)>.0001f){var d=q-p;float t=(d.x*s.y-d.y*s.x)/cross,u=(d.x*r.y-d.y*r.x)/cross;if(t>0&&t<1&&u>0&&u<1&&Mathf.Abs(Mathf.Lerp(a.a.y,a.b.y,t)-Mathf.Lerp(b.a.y,b.b.y,u))<1.5f){a.cuts.Add(t);b.cuts.Add(u);}}
            }
            // Connect authored road endpoints to nearby centre lines, including old/new district seams.
            var connectors=new List<Segment>();
            for(int i=0;i<segments.Count;i++)foreach(var end in new[]{segments[i].a,segments[i].b})
            {float best=35*35;int selected=-1;float cut=0;Vector3 target=default;for(int j=0;j<segments.Count;j++){if(i==j)continue;var s=segments[j];float t=Mathf.Clamp01(Vector3.Dot(end-s.a,s.b-s.a)/(s.b-s.a).sqrMagnitude);var p=Vector3.Lerp(s.a,s.b,t);float d=(p-end).sqrMagnitude;if(d<best&&Mathf.Abs(end.y-p.y)<1){best=d;selected=j;cut=t;target=p;}}if(selected>=0){segments[selected].cuts.Add(cut);if(best>.1f)connectors.Add(new(end,target));}}
            segments.AddRange(connectors);nodes=new();links=new();var index=new Dictionary<Vector3Int,int>();
            int Node(Vector3 p){var key=new Vector3Int(Mathf.RoundToInt(p.x*2),Mathf.RoundToInt(p.y*2),Mathf.RoundToInt(p.z*2));if(index.TryGetValue(key,out var n))return n;n=nodes.Count;nodes.Add(p);links.Add(new());index.Add(key,n);return n;}
            foreach(var s in segments){s.cuts.Sort();for(int n=1;n<s.cuts.Count;n++){int a=Node(Vector3.Lerp(s.a,s.b,s.cuts[n-1])),b=Node(Vector3.Lerp(s.a,s.b,s.cuts[n]));if(a!=b){links[a].Add(b);links[b].Add(a);}}}
        }
        public static List<Vector3> Route(Vector3 from,Vector3 to)
            =>Route(from,to,out _);
        public static List<Vector3> Route(Vector3 from,Vector3 to,out bool connected)
        {
            connected=true;
            if(nodes==null)Build();
            if(nodes.Count==0){connected=false;return new List<Vector3>{from,to};}
            // Attach to road segments, not distant junctions; a goal halfway down the
            // same street must not send the player back to the previous intersection.
            (int a,int b,Vector3 p) Attach(Vector3 point)
            {
                float best=float.PositiveInfinity;var result=(a:0,b:0,p:nodes[0]);
                for(int i=0;i<nodes.Count;i++)foreach(int j in links[i])if(j>i)
                {
                    var delta=nodes[j]-nodes[i];float t=Mathf.Clamp01(Vector3.Dot(point-nodes[i],delta)/delta.sqrMagnitude);
                    var projected=nodes[i]+delta*t;float d=(projected-point).sqrMagnitude;
                    if(d<best){best=d;result=(i,j,projected);}
                }
                return result;
            }
            var source=Attach(from);var destination=Attach(to);int start=nodes.Count,end=start+1,count=end+1;
            Vector3 Position(int i)=>i==start?source.p:i==end?destination.p:nodes[i];
            bool sameEdge=source.a==destination.a&&source.b==destination.b;
            IEnumerable<int> Neighbours(int i)
            {
                if(i==start){yield return source.a;yield return source.b;if(sameEdge)yield return end;yield break;}
                if(i==end)yield break;
                foreach(int n in links[i])yield return n;
                if(i==destination.a||i==destination.b)yield return end;
            }
            var dist=new float[count];var prev=new int[count];var closed=new bool[count];Array.Fill(dist,float.PositiveInfinity);Array.Fill(prev,-1);dist[start]=0;
            var heap=new VehicleNavigator.MinHeap();heap.Push(start,0);
            while(heap.Count>0)
            {
                int n=heap.Pop();if(closed[n])continue;if(n==end)break;closed[n]=true;
                foreach(int k in Neighbours(n)){float score=dist[n]+Vector3.Distance(Position(n),Position(k));if(score>=dist[k])continue;dist[k]=score;prev[k]=n;heap.Push(k,score+Vector3.Distance(Position(k),destination.p));}
            }
            if(float.IsInfinity(dist[end])){connected=false;return new List<Vector3>{from,to};}
            var path=new List<Vector3>{to};for(int n=end;n>=0;n=prev[n]){path.Add(Position(n));if(n==start)break;}path.Add(from);path.Reverse();
            for(int i=path.Count-2;i>=0;i--)if((path[i+1]-path[i]).sqrMagnitude<.04f)path.RemoveAt(i+1);
            if(path.Count==1)path.Add(to);return path;
        }
    }
}
