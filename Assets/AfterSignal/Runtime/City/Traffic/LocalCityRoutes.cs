using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class LocalCityRoutes
    {
        static List<Vector3[]> routes;
        static List<Vector3[]> Routes
        {
            get
            {
                if(routes!=null)return routes;routes=new();
                foreach(var source in ExpansionRoads.Roads)Add(source);
                foreach(var source in FourCityCatalog.Roads)Add(source);
                foreach(var island in RegionalCatalog.Islands){var line=new Vector3[97];for(int i=0;i<97;i++)line[i]=island+SmartIsland.Road(i/96f);routes.Add(line);}
                return routes;
            }
        }
        static void Add(Vector3[] line){var points=new List<Vector3>();for(int i=1;i<line.Length;i++){int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(line[i-1],line[i])/8));for(int k=0;k<n;k++)points.Add(Vector3.Lerp(line[i-1],line[i],k/(float)n));}points.Add(line[^1]);if(points.Count>2)routes.Add(points.ToArray());}
        public static Vector3 Nearest(Vector3 p,out int route,out int sample)
        {
            float best=float.MaxValue;route=sample=0;
            for(int r=0;r<Routes.Count;r++)for(int i=0;i<Routes[r].Length;i++){var q=Routes[r][i];float vertical=Mathf.Abs(p.y-q.y);float d=(new Vector2(p.x-q.x,p.z-q.z)).sqrMagnitude+vertical*vertical*8;if(d<best){best=d;route=r;sample=i;}}
            return Routes[route][sample];
        }
        public static Vector3 Sidewalk(Vector3 p,int advance=0)
        {Nearest(p,out var r,out var i);var line=Routes[r];i=Mathf.Clamp(i+advance,0,line.Length-1);var d=(line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(Vector3.up,d);float width=RegionalCatalog.Island(line[i])?7.5f:13;return line[i]+side*(Vector3.Dot(p-line[i],side)<0?-width:width)+Vector3.up*.08f;}
        public static Vector3[] Loop(Vector3 p)
        {
            Nearest(p,out var r,out var at);var path=Routes[r];var list=new List<Vector3>();bool closed=(path[0]-path[^1]).sqrMagnitude<4;
            // Limit open-route traffic to the local district so the population budget stays nearby.
            int first=closed?0:Mathf.Max(0,at-32),last=closed?path.Length-1:Mathf.Min(path.Length-1,at+32);float lane=RegionalCatalog.Island(p)?2.2f:4;
            for(int i=first;i<=last;i++){var d=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;list.Add(path[i]+Vector3.Cross(Vector3.up,d)*lane);}
            if(!closed)for(int i=last;i>=first;i--){var d=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;list.Add(path[i]-Vector3.Cross(Vector3.up,d)*lane);}
            return list.ToArray();
        }
    }
}
