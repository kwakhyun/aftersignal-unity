using UnityEngine;
namespace AfterSignal
{
    public sealed partial class PlayerMotor
    {
        public bool WallClimbing {get;private set;}
        public Vector3 WallNormal {get;private set;}
        public int JumpsUsed=>jumps;
        float wallRelease;
        public Vector3 AttackHeading
        {
            get
            {
                var d=Vector3.ProjectOnPlane(Aim-Shoulder,Vector3.up);
                return d.sqrMagnitude>.01f?d.normalized:Director.CameraRig.MoveDirection(Vector2.up);
            }
        }
        void StrikeVehicles(float range,float damage)
        {
            var sim=UrbanSimulation.Instance;if(!sim)return;
            foreach(var car in sim.Cars)
            {
                if(!car||car.Wrecked||struckCars.Contains(car))continue;
                var center=car.transform.position+Vector3.up;
                var local=car.transform.InverseTransformPoint(Shoulder);
                local.x=Mathf.Clamp(local.x,-car.HalfLength,car.HalfLength);local.z=Mathf.Clamp(local.z,-car.HalfWidth,car.HalfWidth);local.y=Mathf.Clamp(local.y,.3f,1.8f);
                var surface=car.transform.TransformPoint(local);var delta=surface-Shoulder;
                if(delta.magnitude>range||Vector3.Dot(delta.normalized,AttackHeading)<.1f)continue;
                if(Physics.Linecast(Shoulder,surface,out var wall,1,QueryTriggerInteraction.Ignore)&&wall.collider.GetComponentInParent<CityVehicle>()!=car)continue;
                bool occupied=car.occupied||car.GetComponent<VehicleCabin>()&&car.GetComponent<VehicleCabin>().PassengerCount>0;struckCars.Add(car);car.Damage(damage,surface);
                if(occupied||car.GetComponent<PoliceCar>())WantedSystem.Report(car.GetComponent<PoliceCar>()?12:7,center);
            }
        }
        float wallStepDistance;
        bool Climbable(RaycastHit hit)
        {
            if(Mathf.Abs(hit.normal.y)>.22f||hit.collider.GetComponentInParent<CityVehicle>()||hit.collider.GetComponentInParent<MovingLift>())return false;
            return hit.collider.bounds.size.y>2.5f && CivicWorld.Exploration(Director.stage);
        }
        void Traverse(ControlFrame input,Vector3 move,float dt)
        {
            wallRelease=Mathf.Max(0,wallRelease-dt);
            if(Health<=0||HurtTime>0||Rope.Attached||DashTime>0||input.guard||wallRelease>0)
            {WallClimbing=false;return;}
            if(WallClimbing&&input.jump)
            {
                WallClimbing=false;wallRelease=.45f;Velocity=WallNormal*6+Vector3.up*Tuning.jumpSpeed;
                jumps=1;Grounded=false;supportedUntil=0;jumpBuffer=0;Director.Audio.Play("jump",Shoulder,.24f,1);return;
            }
            if(WallClimbing&&input.move.y<-.3f)
            {WallClimbing=false;wallRelease=.4f;Velocity=WallNormal*2+Vector3.down*2;return;}
            var direction=WallClimbing?-WallNormal:move;
            if(direction.sqrMagnitude<.1f||(!WallClimbing&&(Grounded||input.move.y<=.1f)))return;
            if(Physics.SphereCast(Shoulder,.22f,direction.normalized,out var wall,.8f,1,QueryTriggerInteraction.Ignore)&&Climbable(wall))
            {
                if(Energy<1){WallClimbing=false;wallRelease=1;return;}
                if(!WallClimbing){Director.Audio.Play("land",Shoulder,.16f,1);wallStepDistance=0;}
                WallClimbing=true;WallNormal=wall.normal;Grounded=false;
                float upward=input.move.y>.1f?3.9f:0;
                var tangent=Vector3.Cross(Vector3.up,wall.normal);
                if(Vector3.Dot(tangent,Director.CameraRig.ViewRight)<0)tangent=-tangent;
                Velocity=Vector3.up*upward+tangent*input.move.x*2.4f-wall.normal*1.4f;
                Energy=Mathf.Max(0,Energy-dt*9);jumps=1;Rope.Release();
                wallStepDistance+=(upward+Mathf.Abs(input.move.x)*2.4f)*dt;
                if(wallStepDistance>1.4f){wallStepDistance-=1.4f;Director.Audio.Play("step_tile",Shoulder,.1f,0);}
            }
            else if(WallClimbing)
            {
                // Mantle only when both the landing floor and body clearance are available.
                var over=transform.position-WallNormal*.95f+Vector3.up*2.8f;
                if(Physics.Raycast(over,Vector3.down,out var ledge,3,1,QueryTriggerInteraction.Ignore)&&ledge.normal.y>.65f)
                {
                    var landing=ledge.point+Vector3.up*.08f;
                    if(!Physics.CheckCapsule(landing+Vector3.up*.4f,landing+Vector3.up*1.8f,.32f,1,QueryTriggerInteraction.Ignore))
                    {Controller.enabled=false;transform.position=landing;Controller.enabled=true;Velocity=Vector3.zero;jumps=0;Grounded=true;}
                }
                WallClimbing=false;wallRelease=.3f;
            }
        }
    }
}