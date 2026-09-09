using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class StructuralImpact
    {
        static readonly RaycastHit[] hits=new RaycastHit[64];
        public static float Mass(CityVehicle c)=>c.type==CityVehicleType.Tank?42000:c.type==CityVehicleType.Truck?7000:c.type==CityVehicleType.Bus?12000:c.type==CityVehicleType.Motorcycle?260:c.IsAircraft?22000:1550;
        public static bool Hit(CityVehicle car,Collider col,Vector3 point,float speed)
        {
            var prop=col.GetComponentInParent<BreakableStreetProp>();if(prop)return prop.Strike(car,point,speed);
            if(!car.IsAircraft||speed<38)return false;
            var building=col.GetComponentInParent<CollapsibleBuilding>();
            if(!building && col.bounds.size.y>9 && col.bounds.size.x>3 && col.bounds.size.z>3)
            {building=col.gameObject.AddComponent<CollapsibleBuilding>();building.worldBounds=col.bounds;building.cutBatches=true;}
            if(!building)return false;
            building.Collapse(point,car.Forward);
            bool player=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car;
            car.Damage(car.MaxHealth*2,point,player?null:TrafficDamageSource.Environment,false);return true;
        }
        public static bool CheckCraft(CityVehicle car,Vector3 delta)
        {
            float len=delta.magnitude;if(len<.001f)return false;
            int count=Physics.BoxCastNonAlloc(car.transform.position+Vector3.up*2,new Vector3(car.HalfLength*.8f,1,car.HalfWidth*.8f),delta/len,hits,car.transform.rotation,len+.2f,1,QueryTriggerInteraction.Ignore);
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<count;i++){var hit=hits[i];if(!hit.collider||hit.collider.transform.IsChildOf(car.transform)||hit.normal.y>.7f)continue;if(hit.distance<distance){best=i;distance=hit.distance;}}
            if(best<0)return false;var h=hits[best];Hit(car,h.collider,h.point,Mathf.Abs(car.speed));
            if(!car.Wrecked)car.CollisionDamage(Mathf.Abs(car.speed),h.point);car.speed=0;return true;
        }
    }


    // Compatibility for legacy meshes that were combined across multiple buildings.
    public static class DistrictGeometryCut
    {
        static int serial;static readonly Dictionary<int,Bounds> cuts=new();
        public static void Reset(){cuts.Clear();serial=0;}
        public static int Remove(Bounds bounds)
        {
            // Foundation colliders can extend below street level. Never let their
            // bounds cut a shared road/ground triangle when a building is removed.
            var minimum=bounds.min;
            if(NpcGroundSupport.Floor(bounds.center,minimum.y+4,8,out var ground))minimum.y=Mathf.Max(minimum.y,ground+.24f);
            bounds.SetMinMax(minimum,bounds.max);
            bounds.Expand(new Vector3(1,.2f,1));
            int id=++serial;cuts[id]=bounds;
            foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!r.bounds.Intersects(bounds)||f.GetComponentInParent<CityVehicle>())continue;
                var mesh=f.sharedMesh;if(!mesh||!mesh.isReadable)continue;var owner=f.GetComponent<ReversibleMeshCut>();if(!owner)owner=f.gameObject.AddComponent<ReversibleMeshCut>();owner.Apply(cuts);
            }
            return id;
        }
        public static void Restore(int id){if(!cuts.Remove(id))return;foreach(var owner in Object.FindObjectsByType<ReversibleMeshCut>(FindObjectsSortMode.None))owner.Apply(cuts);}
    }
    public sealed class ReversibleMeshCut:MonoBehaviour
    {
        Mesh original,copy;MeshFilter filter;
        public void Apply(Dictionary<int,Bounds> cuts)
        {
            if(!filter){filter=GetComponent<MeshFilter>();original=filter.sharedMesh;}if(copy)Destroy(copy);
            if(cuts.Count==0){filter.sharedMesh=original;copy=null;return;}
            copy=Instantiate(original);var vertices=original.vertices;
            for(int sub=0;sub<original.subMeshCount;sub++)
            {
                var source=original.GetTriangles(sub);var kept=new List<int>();
                for(int i=0;i<source.Length;i+=3){var p=transform.TransformPoint((vertices[source[i]]+vertices[source[i+1]]+vertices[source[i+2]])/3);bool cut=false;foreach(var bounds in cuts.Values)if(bounds.Contains(p)&&p.y>bounds.min.y+.2f){cut=true;break;}if(!cut){kept.Add(source[i]);kept.Add(source[i+1]);kept.Add(source[i+2]);}}
                copy.SetTriangles(kept,sub);
            }
            copy.RecalculateBounds();filter.sharedMesh=copy;
        }
        void OnDestroy(){if(copy)Destroy(copy);}
    }

}
