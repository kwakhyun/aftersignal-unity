using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class GrappleAnchor : MonoBehaviour
    {
        public static readonly List<GrappleAnchor> All = new List<GrappleAnchor>();
        public int capacitor = -1;
        public bool cityAnchor, hasLanding;
        public Vector3 landing;
        public string label = "ROPE";
        public bool Charged { get; set; }

        public Renderer ring;
        public bool Surface {get;private set;}
        public Collider SurfaceCollider {get;private set;}
        Vector3 localPoint;
        public bool Valid=>isActiveAndEnabled&&(!Surface||SurfaceCollider&&SurfaceCollider.enabled&&SurfaceCollider.gameObject.activeInHierarchy);
        public void SetSurface(Collider collider,Vector3 point)
        {
            Surface=true;SurfaceCollider=collider;localPoint=collider.transform.InverseTransformPoint(point);
            cityAnchor=true;hasLanding=false;label=collider.GetComponentInParent<CityVehicle>()?"차량":"표면";All.Remove(this);FollowSurface();
        }
        public void FollowSurface(){if(Surface&&SurfaceCollider)transform.position=SurfaceCollider.transform.TransformPoint(localPoint);}
        void OnEnable()
        {
            if (!Surface&&!All.Contains(this))
                All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        MaterialPropertyBlock tint;
        void Update()
        {
            if (ring && (!cityAnchor || GameDirector.Instance && (transform.position - GameDirector.Instance.Player.transform.position).sqrMagnitude < 1600))
            {
                ring.transform.Rotate(0, 0, 40 * Time.deltaTime);
                if (tint == null)
                    tint = new MaterialPropertyBlock();
                ring.GetPropertyBlock(tint);
                tint.SetColor("_EmissionColor", (Charged ? new Color(1, .68f, .12f) : new Color(.1f, .9f, 1)) * 2.5f);
                ring.SetPropertyBlock(tint);
            }
        }
    }
}
