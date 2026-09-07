using UnityEngine;
namespace AfterSignal { public sealed class EscalatorSurface:MonoBehaviour {
    public Vector3 from,to;public float speed=1.5f;
    void Update(){var g=GameDirector.Instance;if(!g||g.Blocked)return;var p=g.Player;Vector3 axis=to-from;float t=Vector3.Dot(p.transform.position-from,axis)/axis.sqrMagnitude;if(t<.01f||t>.99f)return;var on=from+axis*t;if(Mathf.Abs(p.transform.position.z-from.z)<1.2f&&Mathf.Abs(p.transform.position.y-on.y)<.28f&&p.Velocity.y<1)p.Controller.Move(axis.normalized*speed*Time.deltaTime);}
} }
