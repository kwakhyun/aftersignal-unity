using UnityEngine;
namespace AfterSignal
{
    public sealed class RaceDriverPresentation:MonoBehaviour
    {
        public VenueRuntime Venue;public int Serial;WorldActor chassis;VenueActor driver;bool evacuated;Vector3 previous;Transform[] parts;
        void Start()
        {
            chassis=gameObject.AddComponent<WorldActor>();chassis.helicopter=true;chassis.environmental=true;chassis.health=900;
            var hit=gameObject.AddComponent<BoxCollider>();hit.center=Vector3.up*.65f;hit.size=new Vector3(4.7f,1.3f,2);
            previous=transform.position;parts=GetComponentsInChildren<Transform>();
        }
        void Update()
        {
            var safety=Venue.GetComponent<VenueSafety>();
            if(safety&&safety.Emergency&&!evacuated)
            {
                evacuated=true;var at=transform.position+transform.forward*3;
                if(CrowdFlow.Place(at,Serial,out var safe,8))at=safe;
                driver=VenueActor.Create(Venue,61000+Venue.Index*10+Serial,"레이스 선수","RacingDriver",Venue.transform.InverseTransformPoint(at));driver.athlete=true;driver.enabled=true;
                NpcSpeech.Say(driver,chassis.Alive?"적기야! 레이스 중단! 피트 쪽으로 대피한다!":"차량에 피격! 의료진을 보내 줘!",5,8);
            }
            if(evacuated&&safety&&!safety.Suspended){if(driver)Destroy(driver.gameObject);evacuated=false;chassis.health=900;}
            float travel=(transform.position-previous).magnitude;previous=transform.position;
            if(travel<.001f)return;foreach(var p in parts)if(p&&p.name.ToLowerInvariant().Contains("wheel")&&p.GetComponent<MeshFilter>())p.Rotate(Vector3.forward,travel*160,Space.Self);
        }
    }
}
