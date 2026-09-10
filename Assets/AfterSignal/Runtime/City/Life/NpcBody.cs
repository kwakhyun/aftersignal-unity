using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(1000)]
    public sealed class NpcBody:MonoBehaviour
    {
        WorldActor actor;Collider solid,restingHit;Transform shell;float next,vehicleCheck;static float nextAudible;
        public int Bumps {get;private set;}

        void Start()
        {
            actor=GetComponent<WorldActor>();
            var cc=GetComponent<CharacterController>();
            if(cc){solid=cc;return;}
            shell=new GameObject("Physical personal space").transform;shell.SetParent(transform,false);shell.gameObject.layer=9;
            var c=shell.gameObject.AddComponent<CapsuleCollider>();c.radius=actor.monster?.65f:.34f;c.height=1.9f;c.center=Vector3.up*.98f;solid=c;
        }
        public void Bump(PlayerMotor player)
        {
            if(!actor||!actor.Alive||Time.time<next||player.Velocity.sqrMagnitude<.4f)return;
            next=Time.time+6;Bumps++;
            if(Time.time<nextAudible||actor.monster)return;nextAudible=Time.time+.8f;
            var npc=GetComponent<CityNpc>();int seed=npc?npc.variation:Mathf.Abs(GetInstanceID());
            NpcSpeech.Say(this,NpcDialogueBank.Line(npc,"bump"),3.2f,2);
            GetComponent<DirectionalPerson>()?.Face(player.transform.position,1.5f);
            if(npc)npc.SocialUntil=Mathf.Max(npc.SocialUntil,Time.time+.7f);
            player.Director.Audio.Play("urban_impact",player.Shoulder,.09f,0);
        }
        void LateUpdate()
        {
            if(!actor||!solid)return;
            if(GetComponent<GarrisonPassenger>()||GetComponent<GarrisonLiftRide>()?.Riding==true){solid.enabled=false;return;}
            var pose=GetComponent<DirectionalPerson>();var walker=GetComponent<CityPedestrian>();bool lying=pose&&pose.Lying||walker&&walker.struck;
            solid.enabled=actor.Alive&&!actor.Downed&&!lying&&!GetComponentInParent<CityVehicle>()&&!(actor.GetComponent<MedicalPending>()&&actor.GetComponent<MedicalPending>().carried);
            if(lying&&!actor.Downed&&!restingHit&&!GetComponentInChildren<ProneHitVolume>())restingHit=ProneHitVolume.Create(actor);
            if(!lying&&restingHit){Destroy(restingHit.gameObject);restingHit=null;}
            // Root billboard rotation must not rotate a second upright trigger across the street.
            if(shell&&GetComponent<SpriteRenderer>()){var trigger=GetComponent<CapsuleCollider>();if(trigger&&trigger.isTrigger)trigger.enabled=false;}
            if(shell){shell.rotation=Quaternion.identity;var s=transform.lossyScale;shell.localScale=new Vector3(1/Mathf.Max(.01f,Mathf.Abs(s.x)),1/Mathf.Max(.01f,Mathf.Abs(s.y)),1/Mathf.Max(.01f,Mathf.Abs(s.z)));}
            var g=GameDirector.Instance;if(actor.Downed)return;
            if(actor.Alive&&Time.time>vehicleCheck){vehicleCheck=Time.time+.24f;if(ActorWorkBudget.DistanceSquared(this)<100*100)PushFromVehicles();}
            if(!actor.Alive||!g||g.Blocked||!g.Player.Controller.enabled)return;
            var player=g.Player;var d=transform.position-player.transform.position;
            if(Mathf.Abs(d.y)>1.6f)return;d.y=0;float distance=d.magnitude;
            // Transform-driven pedestrians cannot occupy a stationary character's body.
            if(distance>.05f&&distance<.67f)
            {var shift=d/distance*(.68f-distance);if(!Physics.SphereCast(transform.position+Vector3.up,.32f,shift.normalized,out _,shift.magnitude+.02f,1,QueryTriggerInteraction.Ignore))transform.position+=shift;Bump(player);}
        }
        void PushFromVehicles()
        {
            var sim=UrbanSimulation.Instance;if(!sim||actor.helicopter||actor.monster||GetComponent<StolenVehicle>()||GetComponent<MedicalPending>())return;
            var crime=GetComponent<GangCrime>();if(crime&&crime.State=="차량 절도")return;
            foreach(var car in sim.Cars)
            {
                if(!car||car.IsAircraft||car.IsWatercraft||Mathf.Abs(car.speed)>.8f||(car.transform.position-transform.position).sqrMagnitude>100)continue;
                var local=car.transform.InverseTransformPoint(transform.position);if(Mathf.Abs(local.y)>1.7f||Mathf.Abs(local.x)>car.HalfLength+.3f||Mathf.Abs(local.z)>car.HalfWidth+.33f)continue;
                local.z=(local.z>=0?1:-1)*(car.HalfWidth+.37f);var desired=car.transform.TransformPoint(local);var shift=desired-transform.position;shift.y=0;
                if(!Physics.SphereCast(transform.position+Vector3.up,.3f,shift.normalized,out var hit,shift.magnitude,1,QueryTriggerInteraction.Ignore)||hit.collider.transform.IsChildOf(car.transform))transform.position+=shift;
            }
        }
        void OnDestroy(){if(shell)Destroy(shell.gameObject);}
    }
    public sealed partial class PlayerMotor
    {
        void OnControllerColliderHit(ControllerColliderHit hit){hit.collider.GetComponentInParent<NpcBody>()?.Bump(this);}
    }
}
