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
            building.Collapse(point,car.Forward);car.Damage(10000,point);return true;
        }
        public static bool CheckCraft(CityVehicle car,Vector3 delta)
        {
            float len=delta.magnitude;if(len<.001f)return false;
            int count=Physics.BoxCastNonAlloc(car.transform.position+Vector3.up*2,new Vector3(car.HalfLength*.8f,1,car.HalfWidth*.8f),delta/len,hits,car.transform.rotation,len+.2f,1,QueryTriggerInteraction.Ignore);
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<count;i++){var hit=hits[i];if(!hit.collider||hit.collider.transform.IsChildOf(car.transform)||hit.normal.y>.7f)continue;if(hit.distance<distance){best=i;distance=hit.distance;}}
            if(best<0)return false;var h=hits[best];Hit(car,h.collider,h.point,Mathf.Abs(car.speed));
            if(!car.Wrecked)car.Damage(Mathf.Max(5,Mathf.Abs(car.speed)*.5f),h.point);car.speed=0;return true;
        }
    }


    // Compatibility for legacy meshes that were combined across multiple buildings.
    public static class DistrictGeometryCut
    {
        public static void Remove(Bounds bounds)
        {
            bounds.Expand(new Vector3(1,.2f,1));
            foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!r.bounds.Intersects(bounds)||f.GetComponentInParent<CityVehicle>())continue;
                var mesh=f.sharedMesh;if(!mesh||!mesh.isReadable)continue;
                var vertices=mesh.vertices;var groups=new List<int[]>();bool changed=false;
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var source=mesh.GetTriangles(sub);var kept=new List<int>(source.Length);
                    for(int i=0;i<source.Length;i+=3){var p=f.transform.TransformPoint((vertices[source[i]]+vertices[source[i+1]]+vertices[source[i+2]])/3);if(bounds.Contains(p)&&p.y>bounds.min.y+.15f){changed=true;continue;}kept.Add(source[i]);kept.Add(source[i+1]);kept.Add(source[i+2]);}groups.Add(kept.ToArray());
                }
                if(!changed)continue;var copy=Object.Instantiate(mesh);copy.name="Structural cut / "+mesh.name;for(int i=0;i<groups.Count;i++)copy.SetTriangles(groups[i],i);copy.RecalculateBounds();
                f.sharedMesh=copy;f.gameObject.AddComponent<RuntimeMeshOwner>().mesh=copy;
            }
        }
    }

}
