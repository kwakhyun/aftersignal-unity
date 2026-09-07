using UnityEngine;
namespace AfterSignal
{
    // Hide only upper volumes when their footprint blocks the camera-to-player sightline.
    // Ground-floor colliders and door geometry remain intact.
    public sealed class CityBuildingCutaway:MonoBehaviour
    {
        public Renderer[] upper;public Vector3 center;public Vector2 footprint=new Vector2(40,54);float next;
        void LateUpdate(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;var g=GameDirector.Instance;if(!g||!Camera.main)return;
            Vector3 a=Camera.main.transform.position,b=g.Player.Shoulder,d=b-a;bool hide=false;
            var box=new Bounds(center+Vector3.up*61,new Vector3(footprint.x+2,116,footprint.y+2));
            hide=box.IntersectRay(new Ray(a,d.normalized),out float distance)&&distance<d.magnitude;
            foreach(var r in upper)if(r)r.enabled=!hide;
        }
    }
}
