using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(1150)]
    public sealed class AthleteMotion:MonoBehaviour
    {
        VenueActor athlete;SpriteRenderer source,display;Transform pose;float phase,action;Vector3 previous;int last=-1;
        void Start(){athlete=GetComponent<VenueActor>();source=GetComponent<SpriteRenderer>();var go=new GameObject("Athlete action presentation",typeof(SpriteRenderer));go.transform.SetParent(transform,false);pose=go.transform;display=go.GetComponent<SpriteRenderer>();display.sharedMaterial=source.sharedMaterial;previous=transform.position;}
        void LateUpdate()
        {
            if(!source||!athlete||!athlete.venue)return;var body=GetComponent<WorldActor>();if(!body||!body.Alive||body.Downed){source.forceRenderingOff=false;display.enabled=false;return;}
            source.forceRenderingOff=true;display.enabled=source.enabled;display.sprite=source.sprite;display.flipX=source.flipX;display.color=source.color;
            var m=FourCitySports.Instance?.Get(athlete.venue.Definition.id);if(m==null)return;
            var movement=transform.position-previous;previous=transform.position;phase+=movement.magnitude*4;
            var ball=athlete.venue.transform.TransformPoint(new Vector3(m.ballX,0,m.ballZ));
            if(m.eventNumber!=last){last=m.eventNumber;if((ball-transform.position).sqrMagnitude<18*18&&(athlete.team==m.possessingTeam||m.sport==VenueKind.Baseball))action=.72f;}
            bool running=movement.sqrMagnitude>.00005f;action=Mathf.Max(0,action-Time.deltaTime);bool safe=VenueSafety.IsSuspended(m.venue);float t=1-action/.72f;
            float jump=!safe&&action>0&&m.sport==VenueKind.Basketball?Mathf.Sin(t*Mathf.PI)*.58f:0;
            pose.localPosition=new Vector3(0,jump+(running?Mathf.Abs(Mathf.Sin(phase))*.045f:0),0);
            pose.localRotation=Quaternion.Euler(0,0,!safe&&action>0?(m.sport==VenueKind.Baseball?Mathf.Sin(t*Mathf.PI*2)*12:m.sport==VenueKind.Football?Mathf.Sin(t*Mathf.PI)*9:0):running?Mathf.Sin(phase)*2:0);
        }
        void OnDisable(){if(source)source.forceRenderingOff=false;}
    }
}
