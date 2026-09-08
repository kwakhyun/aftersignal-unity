using UnityEngine;
namespace AfterSignal
{
    public sealed class KelpCurrent:MonoBehaviour
    {
        public float phase;
        void Update(){var g=GameDirector.Instance;if(!g||g.Blocked||(g.Player.transform.position-transform.position).sqrMagnitude>140*140)return;transform.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*.7f+phase)*5,0,Mathf.Sin(Time.time*.43f+phase)*8);}
    }
}
