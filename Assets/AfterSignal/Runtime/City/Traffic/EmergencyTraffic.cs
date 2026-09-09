using UnityEngine;
namespace AfterSignal
{
    // Local drivable-corridor sampling supplements the road route, without disabling collisions.
    public static class EmergencyTraffic
    {
        static readonly RaycastHit[] hits=new RaycastHit[48];
        static readonly float[] angles={0,18,-18,34,-34,52,-52,72,-72};
        public static Vector3 Steer(CityVehicle car,Vector3 desired)
        {
            float look=Mathf.Clamp(Mathf.Abs(car.speed)*.65f+car.HalfLength+4,9,24);
            Vector3 from=car.transform.position+Vector3.up*1.1f,best=desired.normalized;float bestScore=-999;
            foreach(float angle in angles)
            {
                var direction=Quaternion.Euler(0,angle,0)*desired.normalized;
                // A tall, shallow box covers the chassis corridor without starting inside the road.
                float clearance=look;int n=Physics.BoxCastNonAlloc(from,new Vector3(car.HalfWidth+.22f,.65f,.18f),direction,hits,Quaternion.LookRotation(direction),look,1,QueryTriggerInteraction.Ignore);
                for(int i=0;i<n;i++)
                {
                    var h=hits[i];if(h.collider.transform.IsChildOf(car.transform)||h.normal.y>.65f)continue;
                    clearance=Mathf.Min(clearance,h.distance);
                    var other=h.collider.GetComponentInParent<CityVehicle>();
                    if(other&&other.traffic&&!other.owned&&!other.GetComponent<EmergencyAmbulance>())TrafficYield.Request(other,car);
                }
                var sample=car.transform.position+direction*Mathf.Min(look,Mathf.Max(4,clearance));
                if(!VehicleGround.Sample(car,sample,.8f,2,out var floor)||floor.normal.y<.75f||Mathf.Abs(floor.point.y-car.transform.position.y)>1.1f)continue;
                float score=clearance-Mathf.Abs(angle)*.035f-(clearance<look-.1f?3:0);if(score>bestScore){bestScore=score;best=direction;}
            }
            return best;
        }
    }
    public sealed class TrafficYield:MonoBehaviour
    {
        CityVehicle car;Vector3 away;float until;
        public static void Request(CityVehicle c,CityVehicle emergency)
        {
            var g=GameDirector.Instance;if(!c||c.Wrecked||!g||UrbanSimulation.Instance.Current==c)return;
            var y=c.GetComponent<TrafficYield>();if(!y)y=c.gameObject.AddComponent<TrafficYield>();y.car=c;y.until=Time.time+2.5f;
            var side=Vector3.Cross(emergency.Forward,Vector3.up);y.away=side*(Vector3.Dot(c.transform.position-emergency.transform.position,side)>=0?1:-1);
        }
        void Update()
        {
            if(!car||Time.time>until){Destroy(this);return;}if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            car.speed=Mathf.MoveTowards(car.speed,0,Time.deltaTime*16);
            var delta=away*Time.deltaTime*1.8f;var p=car.transform.position;
            int n=Physics.BoxCastNonAlloc(p+Vector3.up,new Vector3(car.HalfLength,.7f,car.HalfWidth),away,contacts,car.transform.rotation,delta.magnitude+.18f,1,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(!contacts[i].transform.IsChildOf(transform)&&contacts[i].normal.y<.65f)return;
            if(Physics.Raycast(p+delta+Vector3.up*1.4f,Vector3.down,out var floor,3,1,QueryTriggerInteraction.Ignore)&&floor.normal.y>.8f&&Mathf.Abs(floor.point.y-p.y)<.55f)car.transform.position=p+delta;
        }
        readonly RaycastHit[] contacts=new RaycastHit[24];
    }
}
