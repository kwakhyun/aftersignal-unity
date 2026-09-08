using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class BreakableStreetProp:MonoBehaviour
    {
        public float breakEnergy=90000;public bool broken;public bool cutBatches;public Bounds worldBounds;
        public bool Strike(CityVehicle car,Vector3 point,float speed)
        {
            if(broken)return true;
            float energy=.5f*StructuralImpact.Mass(car)*speed*speed;
            if(energy<breakEnergy){car.Damage(Mathf.Min(28,speed*1.3f),point);return false;}
            broken=true;car.Damage(car.type==CityVehicleType.Tank?1:Mathf.Clamp(breakEnergy/StructuralImpact.Mass(car)*.025f,2,24),point);
            if(cutBatches){DistrictGeometryCut.Remove(worldBounds);foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=true;}
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var l in GetComponentsInChildren<Light>())l.enabled=false;
            SignalEffects.Burst(point,SignalEffects.Gold,20,6);
            StartCoroutine(Fall(car.Forward,Mathf.Max(4,speed*.3f)));return true;
        }
        IEnumerator Fall(Vector3 direction,float force)
        {
            var start=transform.rotation;var axis=Vector3.Cross(Vector3.up,direction).normalized;float t=0;
            while(t<1.5f){if(!(GameDirector.Instance&&GameDirector.Instance.Blocked)){t+=Time.deltaTime;transform.rotation=Quaternion.AngleAxis(Mathf.SmoothStep(0,88,t/1.5f),axis)*start;}yield return null;}
            yield return new WaitForSeconds(16);Destroy(gameObject);
        }
    }
}
