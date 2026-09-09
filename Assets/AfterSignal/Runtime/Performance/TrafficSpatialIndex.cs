using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // One fleet scan per 0.15 s, shared by every car's headway query.
    public static class TrafficSpatialIndex
    {
        static UrbanSimulation owner;static float next;
        static readonly Dictionary<Vector2Int,List<CityVehicle>> grid=new();
        static readonly Stack<List<CityVehicle>> pool=new();
        static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/40),Mathf.FloorToInt(p.z/40));
        public static void Nearby(Vector3 at,List<CityVehicle> result)
        {
            result.Clear();var sim=UrbanSimulation.Instance;if(!sim)return;
            if(owner!=sim||Time.time>=next)
            {
                owner=sim;next=Time.time+.15f;foreach(var bucket in grid.Values){bucket.Clear();pool.Push(bucket);}grid.Clear();
                foreach(var car in sim.Cars){if(!car||!car.gameObject.activeInHierarchy||car.IsSpecial)continue;var key=Cell(car.transform.position);if(!grid.TryGetValue(key,out var bucket))grid[key]=bucket=pool.Count>0?pool.Pop():new List<CityVehicle>(12);bucket.Add(car);}
            }
            var cell=Cell(at);for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++)if(grid.TryGetValue(cell+new Vector2Int(x,z),out var bucket))foreach(var car in bucket)
                if(car&&car.gameObject.activeInHierarchy&&Mathf.Abs(car.transform.position.y-at.y)<3&&(car.transform.position-at).sqrMagnitude<65*65)result.Add(car);
        }
    }
}
