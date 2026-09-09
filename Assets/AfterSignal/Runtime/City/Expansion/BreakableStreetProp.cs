using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class BreakableStreetProp:MonoBehaviour
    {
        public float breakEnergy=90000;public bool broken;public bool cutBatches;public Bounds worldBounds;
        int cut;Renderer[] renderers;bool[] states;Collider[] colliders;bool[] solid;Light[] lights;bool[] lit;
        public bool Strike(CityVehicle car,Vector3 point,float speed)
        {
            if(broken)return true;
            float energy=.5f*StructuralImpact.Mass(car)*speed*speed;
            if(energy<breakEnergy){car.Damage(Mathf.Min(28,speed*1.3f),point);return false;}
            broken=true;car.Damage(car.type==CityVehicleType.Tank?1:Mathf.Clamp(breakEnergy/StructuralImpact.Mass(car)*.025f,2,24),point);
            renderers=GetComponentsInChildren<Renderer>();states=System.Array.ConvertAll(renderers,r=>r.enabled);colliders=GetComponentsInChildren<Collider>();solid=System.Array.ConvertAll(colliders,c=>c.enabled);lights=GetComponentsInChildren<Light>();lit=System.Array.ConvertAll(lights,l=>l.enabled);
            if(cutBatches){cut=DistrictGeometryCut.Remove(worldBounds);foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=true;}
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var l in GetComponentsInChildren<Light>())l.enabled=false;
            SignalEffects.Burst(point,SignalEffects.Gold,20,6);
            StartCoroutine(Fall(car.Forward,Mathf.Max(4,speed*.3f)));return true;
        }
        IEnumerator Fall(Vector3 direction,float force)
        {
            var start=transform.rotation;var axis=Vector3.Cross(Vector3.up,direction).normalized;float t=0;
            while(t<1.5f){if(!(GameDirector.Instance&&GameDirector.Instance.Blocked)){t+=Time.deltaTime;transform.rotation=Quaternion.AngleAxis(Mathf.SmoothStep(0,88,t/1.5f),axis)*start;}yield return null;}
            yield return new WaitForSeconds(16);foreach(var r in renderers)if(r)r.enabled=false;yield return new WaitForSeconds(140);
            while(GameDirector.Instance&&(GameDirector.Instance.Player.transform.position-transform.position).sqrMagnitude<12*12)yield return new WaitForSeconds(8);
            if(cut!=0)DistrictGeometryCut.Restore(cut);cut=0;transform.rotation=start;
            for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=states[i];for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=solid[i];for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=lit[i];broken=false;
        }
    }
}
