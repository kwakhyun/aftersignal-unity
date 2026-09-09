using UnityEngine;
namespace AfterSignal
{
    // A world-aligned child stays shootable when billboard art rotates or a controller is disabled.
    [DefaultExecutionOrder(1200)]
    public sealed class ProneHitVolume:MonoBehaviour
    {
        WorldActor body;BoxCollider hit;
        public static Collider Create(WorldActor actor)
        {
            var go=new GameObject("Prone body / damage volume",typeof(BoxCollider),typeof(ProneHitVolume));go.layer=9;go.transform.SetParent(actor.transform,false);
            var v=go.GetComponent<ProneHitVolume>();v.body=actor;v.hit=go.GetComponent<BoxCollider>();v.hit.isTrigger=true;v.hit.size=new Vector3(2.1f,.62f,.85f);v.Align();return v.hit;
        }
        void Align(){transform.position=body.transform.position+Vector3.up*.32f;transform.rotation=Quaternion.identity;var s=body.transform.lossyScale;transform.localScale=new Vector3(1/Mathf.Max(.01f,Mathf.Abs(s.x)),1/Mathf.Max(.01f,Mathf.Abs(s.y)),1/Mathf.Max(.01f,Mathf.Abs(s.z)));}
        void LateUpdate(){if(!body)return;Align();var p=body.GetComponent<MedicalPending>();hit.enabled=!(p&&p.carried);}
    }
}
