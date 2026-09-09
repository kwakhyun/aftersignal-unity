using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(700)]
    public sealed class SeaTraffic:MonoBehaviour
    {
        static readonly List<SeaTraffic> fleet=new();static int frame=-1;
        CityVehicle car;Vector3 previous;Quaternion rotation;bool initialized;
        public static int ContactCorrections {get;private set;}
        void Start(){car=GetComponent<CityVehicle>();previous=transform.position;rotation=transform.rotation;initialized=true;fleet.Add(this);}
        void OnDestroy()=>fleet.Remove(this);
        public static Vector3 Steer(CityVehicle c,Vector3 desired)
        {
            desired.y=0;if(desired.sqrMagnitude<.001f)return desired;desired.Normalize();var side=Vector3.Cross(desired,Vector3.up);var result=desired;
            foreach(var other in fleet)
            {
                if(!other||!other.car||other.car==c||Mathf.Abs(other.transform.position.y-c.transform.position.y)>10)continue;
                var d=other.transform.position-c.transform.position;d.y=0;
                float along=Vector3.Dot(d,desired),lateral=Vector3.Dot(d,side);
                float longitudinal=c.HalfLength+Projected(other.car,desired)+Mathf.Max(12,Mathf.Abs(c.speed)*5);
                float clearance=c.HalfWidth+Projected(other.car,side)+5;
                if(along< -c.HalfLength||along>longitudinal||Mathf.Abs(lateral)>clearance)continue;
                // Both vessels keep starboard in a head-on encounter. A parked ship yields no steering.
                float turn=lateral>clearance*.45f?-1:1;
                result+=side*turn*(1-Mathf.Clamp01(Mathf.Max(0,along)/longitudinal))*2.6f;
            }
            return result.normalized;
        }
        public static Vector3 Move(CityVehicle c,Vector3 next)
        {
            if(!c.IsWatercraft)return next;var delta=next-c.transform.position;float y=next.y;
            var candidate=c.transform.position+Steer(c,delta)*Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;candidate.y=y;
            return OceanLife.Contains(candidate)?candidate:c.transform.position;
        }
        static float Projected(CityVehicle c,Vector3 axis)=>Mathf.Abs(Vector3.Dot(c.Forward,axis))*c.HalfLength+Mathf.Abs(Vector3.Dot(c.transform.forward,axis))*c.HalfWidth;
        public static bool Overlap(CityVehicle a,CityVehicle b,out Vector3 correction,float padding=.35f)
        {
            correction=Vector3.zero;var d=a.transform.position-b.transform.position;d.y=0;float min=float.MaxValue;
            for(int i=0;i<4;i++)
            {
                var axis=Vector3.ProjectOnPlane(i==0?a.Forward:i==1?a.transform.forward:i==2?b.Forward:b.transform.forward,Vector3.up).normalized;
                float overlap=Projected(a,axis)+Projected(b,axis)+padding-Mathf.Abs(Vector3.Dot(d,axis));if(overlap<=0)return false;
                if(overlap<min){min=overlap;float sign=Vector3.Dot(d,axis);correction=axis*(sign<0?-1:1)*(overlap+.03f);}
            }
            return true;
        }
        static float Sweep(SeaTraffic a,SeaTraffic b)
        {
            var d=a.previous-b.previous;var velocity=(a.transform.position-a.previous)-(b.transform.position-b.previous);d.y=velocity.y=0;
            float enter=0,exit=1;
            for(int i=0;i<4;i++)
            {
                var axis=Vector3.ProjectOnPlane(i==0?a.car.Forward:i==1?a.transform.forward:i==2?b.car.Forward:b.transform.forward,Vector3.up).normalized;
                float r=Projected(a.car,axis)+Projected(b.car,axis)+.35f,p=Vector3.Dot(d,axis),v=Vector3.Dot(velocity,axis);
                if(Mathf.Abs(v)<.0001f){if(Mathf.Abs(p)>=r)return 1;continue;}
                float t0=(-r-p)/v,t1=(r-p)/v;if(t0>t1){float t=t0;t0=t1;t1=t;}
                enter=Mathf.Max(enter,t0);exit=Mathf.Min(exit,t1);if(enter>exit)return 1;
            }
            return enter>0&&enter<1?Mathf.Max(0,enter-.01f):1;
        }
        void LateUpdate()
        {
            if(!initialized||frame==Time.frameCount)return;frame=Time.frameCount;
            if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            for(int i=0;i<fleet.Count;i++)
            {
                var a=fleet[i];if(!a||!a.car)continue;
                for(int j=i+1;j<fleet.Count;j++)
                {
                    var b=fleet[j];if(!b||!b.car||Mathf.Abs(a.transform.position.y-b.transform.position.y)>10)continue;
                    float bound=a.car.HalfLength+b.car.HalfLength+a.car.HalfWidth+b.car.HalfWidth+40;
                    if((a.transform.position-b.transform.position).sqrMagnitude>bound*bound)continue;
                    float t=Sweep(a,b);
                    if(t<1){a.transform.position=Vector3.Lerp(a.previous,a.transform.position,t);b.transform.position=Vector3.Lerp(b.previous,b.transform.position,t);a.car.speed*=.5f;b.car.speed*=.5f;ContactCorrections++;}
                    if(!Overlap(a.car,b.car,out var push))continue;
                    // Undo a turn into an adjacent hull before separating the remaining contact.
                    a.transform.rotation=a.rotation;b.transform.rotation=b.rotation;
                    if(!Overlap(a.car,b.car,out push))continue;
                    float massA=a.car.HalfLength*a.car.HalfWidth,massB=b.car.HalfLength*b.car.HalfWidth;
                    float share=massB/(massA+massB);var pa=a.transform.position+push*share;var pb=b.transform.position-push*(1-share);
                    if(OceanLife.Contains(pa)&&OceanLife.Contains(pb)){a.transform.position=pa;b.transform.position=pb;}
                    else if(OceanLife.Contains(a.transform.position+push))a.transform.position+=push;
                    else if(OceanLife.Contains(b.transform.position-push))b.transform.position-=push;
                    a.car.speed=Mathf.Min(a.car.speed,2);b.car.speed=Mathf.Min(b.car.speed,2);ContactCorrections++;
                }
            }
            foreach(var boat in fleet)if(boat){boat.previous=boat.transform.position;boat.rotation=boat.transform.rotation;}
        }
    }
}
