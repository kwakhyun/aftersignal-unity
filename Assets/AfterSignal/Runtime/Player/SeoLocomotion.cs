using UnityEngine;
namespace AfterSignal
{
    public sealed class SeoLocomotion:MonoBehaviour
    {
        public const float StrideLength=3.4f;
        Vector3 previous,heading=Vector3.forward;bool seeded;
        float phase,climbPhase,clock,lean;
        int sector=4;
        public int Sector=>sector;
        public bool Flip {get;private set;}
        public int FrameIndex{get;private set;}
        public float Cycle=>phase;
        public float Lean=>lean;
        public Vector3 Offset {get;private set;}
        public string State {get;private set;}="Idle";
        public Sprite Tick(PlayerMotor p,float dt)
        {
            Vector3 displacement=seeded?transform.position-previous:Vector3.zero;
            previous=transform.position;seeded=true;
            if(displacement.sqrMagnitude>25)displacement=Vector3.zero;
            Vector3 delta=Vector3.ProjectOnPlane(displacement,Vector3.up);
            float speed=new Vector2(p.Velocity.x,p.Velocity.z).magnitude;
            bool moving=delta.magnitude>.001f&&speed>.2f;
            if(moving)heading=delta.normalized;
            if(p.DashTime>0)heading=p.DashDirection;
            var right=p.Director.CameraRig.ViewRight;
            float angle=Mathf.Repeat(Mathf.Atan2(Vector3.Dot(heading,right),-Vector3.Dot(heading,Vector3.Cross(right,Vector3.up)))*Mathf.Rad2Deg,360);
            if(Mathf.Abs(Mathf.DeltaAngle(sector*45,angle))>27)sector=SeoKinetic.Direction(heading,right);
            clock+=dt;Offset=Vector3.zero;FrameIndex=0;
            float targetLean=0;
            Sprite result;
            if(p.Health<=0){State="Down";result=Action(sector,SeoMotionPose.Hurt);targetLean=86;}
            else if(p.HurtTime>0){State="Hurt";result=Action(sector,SeoMotionPose.Hurt);targetLean=-3;}
            else if(p.WallClimbing)
            {
                State="Climb";climbPhase+=displacement.magnitude/1.4f;
                int direction=SeoKinetic.Direction(-p.WallNormal,right);
                result=Action(direction,(Mathf.FloorToInt(climbPhase)%2)==0?SeoMotionPose.ClimbLeft:SeoMotionPose.ClimbRight);
            }
            else if(p.AttackTime>0||p.Guarding||p.Reloading)
            {
                State=p.AttackTime>0?"Attack":p.Reloading?"Reload":"Guard";
                result=SeoKinetic.Action(p,sector);Flip=SeoKinetic.Mirror(p,sector);
                if(p.Reloading)targetLean=Mathf.Sin(p.ReloadProgress*Mathf.PI)*2;
            }
            else if(p.DashTime>0){State="Dash";result=Action(sector,SeoMotionPose.Dash);}
            else if(p.Rope.Attached){State="Rope";result=Action(sector,SeoMotionPose.Rope);targetLean=Mathf.Clamp(-Vector3.Dot(p.Velocity,right)*.45f,-9,9);}
            else if(!p.Grounded){State=p.Velocity.y>1?"Rise":"Fall";result=Action(sector,p.Velocity.y>1?SeoMotionPose.Rise:SeoMotionPose.Fall);}
            else if(p.LandingTime>.07f){State="Land";result=Action(sector,SeoMotionPose.Land);}
            else if(moving)
            {
                State="Run";
                // One physical stride, shared by all eight directions and both footfall sounds.
                phase=Mathf.Repeat(phase+delta.magnitude/StrideLength,1);
                FrameIndex=Mathf.FloorToInt(phase*8)%8;
                result=SeoRefined.Run(sector,FrameIndex);Flip=SeoRefined.Flip(sector);
                targetLean=-Vector3.Dot(p.Velocity,right)*.17f;
            }
            else
            {
                State="Idle";result=SeoKinetic.Frame("Idle",sector);Flip=sector==2;
                Offset=Vector3.up*(Mathf.Sin(clock*2.4f)*.004f);
            }
            lean=Mathf.LerpAngle(lean,targetLean,1-Mathf.Exp(-dt*(p.Health<=0?8:18)));
            return result;
        }
        Sprite Action(int direction,SeoMotionPose pose)
        {
            FrameIndex=(int)pose;
            var sprite=SeoRefined.Pose(direction,pose,out var flip);Flip=flip;return sprite;
        }
    }
}
