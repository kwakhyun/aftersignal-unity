using UnityEngine;
namespace AfterSignal { public sealed class RouteBeacon:MonoBehaviour {
    public Transform ring;public bool districtExit=true;
    void Update(){var g=GameDirector.Instance;if(!g||!ring)return;bool ready=!districtExit||g.Power&&g.Cleared;ring.localRotation=Quaternion.Euler(0,0,ready?Time.time*25:0);ring.localScale=Vector3.one*(ready?1+Mathf.Sin(Time.time*3)*.035f:.88f);}
} }
