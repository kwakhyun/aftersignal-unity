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
        void OnEnable()
        {
            if (!All.Contains(this))
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
