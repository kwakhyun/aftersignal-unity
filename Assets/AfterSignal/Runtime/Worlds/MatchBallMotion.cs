using UnityEngine;
namespace AfterSignal
{
    // Match events drive distinct passes, shots and pitch/contact phases.
    public sealed class MatchBallMotion:MonoBehaviour
    {
        public VenueRuntime Venue;int sequence=-1;Vector3 from,to;float age,duration,arc;bool contact,shot;
        void Update()
        {
            var g=GameDirector.Instance;var m=Venue?FourCitySports.Instance?.Get(Venue.Definition.id):null;
            if(!g||g.Blocked||m==null||VenueSafety.IsSuspended(m.venue))return;
            if(sequence!=m.eventNumber)
            {
                sequence=m.eventNumber;from=transform.localPosition;to=new Vector3(m.ballX,.25f,m.ballZ);age=0;shot=m.lastEvent.Contains("슛")||m.lastEvent.Contains("리바운드");
                contact=m.sport==VenueKind.Baseball&&(Mathf.Abs(m.ballX)>1||m.ballZ> -40);
                if(m.sport==VenueKind.Baseball){from=new Vector3(0,1.7f,-25.56f);duration=contact?1.65f:.48f;arc=contact?Mathf.Max(2,m.ballHeight):.07f;}
                else if(m.sport==VenueKind.Basketball){duration=shot?.95f:.48f;arc=shot?2.8f:.25f;if(shot)to=new Vector3(m.ballX>=0?12.45f:-12.45f,3.05f,0);}
                else {duration=Mathf.Clamp(Vector3.Distance(from,to)/21,.35f,1.7f);arc=Mathf.Max(.08f,m.ballHeight);}
            }
            if(m.phase!=MatchPhase.Playing)return;age+=Mathf.Min(Time.deltaTime,.06f);float t=Mathf.Clamp01(age/duration);Vector3 p;
            if(m.sport==VenueKind.Baseball&&contact)
            {
                var plate=new Vector3(0,.85f,-44);float pitch=.3f;
                if(age<pitch)p=Vector3.Lerp(from,plate,age/pitch);
                else{float hit=Mathf.Clamp01((age-pitch)/(duration-pitch));p=Vector3.Lerp(plate,to,hit)+Vector3.up*Mathf.Sin(hit*Mathf.PI)*arc;}
            }
            else p=Vector3.Lerp(from,to,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*arc;
            if(m.sport==VenueKind.Basketball&&t>=1&&!shot)p.y=.22f+Mathf.Abs(Mathf.Sin(age*11))*.85f;
            transform.localPosition=p;transform.Rotate(Vector3.right,Time.deltaTime*(m.sport==VenueKind.Baseball?1300:460),Space.Self);
        }
    }
}
