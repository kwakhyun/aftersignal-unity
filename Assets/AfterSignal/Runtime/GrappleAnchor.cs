using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public sealed class GrappleAnchor : MonoBehaviour
    {
        public static readonly List<GrappleAnchor> All = new List<GrappleAnchor>();
        public int capacitor=-1;
        public string label="ROPE";
        public bool Charged { get; set; }
        public Renderer ring;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        void Update(){ if(ring) {ring.transform.Rotate(0,0,40*Time.deltaTime); ring.material.SetColor("_EmissionColor",(Charged?new Color(1,.68f,.12f):new Color(.1f,.9f,1))*2.5f); } }
    }
}
