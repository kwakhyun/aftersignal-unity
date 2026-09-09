using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class BlastDamage:MonoBehaviour
    {
        public static int Detonations {get;private set;}
        float radius,damage;WorldActor source;CityVehicle excluded,direct;BlastPayload payload;bool done;
        public static void Create(Vector3 at,float radius,float damage,WorldActor source=null,CityVehicle excluded=null,BlastPayload payload=BlastPayload.Conventional,CityVehicle direct=null)
        {
            var go=new GameObject("Blast damage wave");go.transform.position=at;var b=go.AddComponent<BlastDamage>();b.radius=radius;b.damage=damage;b.source=source;b.excluded=excluded;b.payload=payload;b.direct=direct;
        }
        public static bool Exposed(Vector3 origin,Vector3 target,Transform victim,CityVehicle excluded=null)
        {
            Vector3 d=target-origin;
            foreach(var hit in Physics.RaycastAll(origin,d.normalized,d.magnitude,1,QueryTriggerInteraction.Ignore))
            {if(hit.distance<.08f||hit.normal.y>.8f||hit.collider.attachedRigidbody||excluded&&hit.transform.IsChildOf(excluded.transform)||victim&&hit.transform.IsChildOf(victim))continue;return false;}
            return true;
        }
        void Update()
        {
            if(done)return;done=true;Detonations++;Vector3 origin=transform.position+Vector3.up*.8f;
            foreach(var actor in WorldActor.All.ToArray())
            {
                if(!actor||!actor.Alive)continue;float distance=Vector3.Distance(origin,actor.Center);
                if(distance>radius||!Exposed(origin,actor.Center,actor.transform,excluded))continue;
                var d=(actor.Center-origin).normalized;float amount=damage*(actor.monster&&payload!=BlastPayload.Conventional?8:1)*Mathf.Lerp(1,.08f,distance/radius);
                if(payload!=BlastPayload.Conventional&&!actor.monster&&distance<radius*.32f)amount=Mathf.Max(amount,actor.MaxHealth*2.6f);
                actor.Damage(amount,d*12,source);
                if(!actor.helicopter&&!actor.monster)CivilianImpact.Launch(actor,d,Mathf.Lerp(22,7,distance/radius));
            }
            var g=GameDirector.Instance;
            if(g)
            {
                float d=Vector3.Distance(origin,g.Player.Shoulder);if(d<radius&&Exposed(origin,g.Player.Shoulder,g.Player.transform,excluded)){g.Player.ReceiveDamage(damage*Mathf.Lerp(1,.2f,d/radius),origin);g.CameraRig.Kick(.14f*(1-d/radius));}
                foreach(var e in g.Enemies)if(e&&e.Alive&&Vector3.Distance(origin,e.transform.position)<radius&&Exposed(origin,e.transform.position+Vector3.up,e.transform))e.Damage(damage,(e.transform.position-origin).normalized*8);
            }
            var sim=UrbanSimulation.Instance;
            if(sim)foreach(var car in new List<CityVehicle>(sim.Cars))
            {if(!car||car==excluded||car.Wrecked)continue;var point=WarheadDamage.HullPoint(car,origin);float d=Vector3.Distance(origin,point);if(car==direct||(d<radius&&Exposed(origin,point,car.transform,excluded)))car.Damage(WarheadDamage.Against(car,payload,damage)*(car==direct?1:Mathf.Lerp(.85f,.15f,Mathf.Clamp01(d/radius))),point,source);}
            CityFireService.IgniteBlast(origin,radius,damage);
            Destroy(gameObject);
        }
    }
}
