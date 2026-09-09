using UnityEngine;
namespace AfterSignal
{
    // Keep the patient's original face/clothes. Dedicated wound/gauze images are layered on it.
    [DefaultExecutionOrder(1050)]
    public sealed class InjuryPresentation:MonoBehaviour
    {
        SpriteRenderer original,patch,proneView;MedicalState injury;WorldActor body;
        static Sprite[] atlas;
        void Start()
        {
            body=GetComponent<WorldActor>();injury=GetComponent<MedicalState>();
            original=GetComponent<SpriteRenderer>();if(!original)original=GetComponentInChildren<SpriteRenderer>();
            if(atlas==null){atlas=Resources.LoadAll<Sprite>("Art/Medical/InjuryLayers");System.Array.Sort(atlas,(a,b)=>string.CompareOrdinal(a.name,b.name));}
            if(!original||atlas.Length<8)return;
            var go=new GameObject("Injury / treatment sprite",typeof(SpriteRenderer));go.transform.SetParent(original.transform,false);patch=go.GetComponent<SpriteRenderer>();patch.sharedMaterial=original.sharedMaterial;patch.sortingOrder=original.sortingOrder+1;
        }
        void LateUpdate()
        {
            if(!original||!patch)return;
            bool carried=body&&body.GetComponent<MedicalPending>()&&body.GetComponent<MedicalPending>().carried;
            bool prone=body&&injury&&(injury.Incapacitated||!body.Alive&&proneView);
            if(prone&&!carried)
            {
                if(!proneView){var go=new GameObject("Grounded patient appearance",typeof(SpriteRenderer));go.transform.SetParent(transform,false);proneView=go.GetComponent<SpriteRenderer>();proneView.sharedMaterial=original.sharedMaterial;proneView.sprite=original.sprite;}
                original.enabled=false;proneView.enabled=true;proneView.color=Color.white;
                var rotation=Quaternion.Euler(90,0,90);proneView.transform.rotation=rotation;proneView.transform.localScale=Vector3.one;
                proneView.transform.position=transform.position+Vector3.up*.16f-rotation*proneView.sprite.bounds.center;
                patch.enabled=false;return;
            }
            if(proneView){proneView.enabled=false;if(!carried){original.enabled=true;Destroy(proneView.gameObject);proneView=null;}}
            bool active=body&&body.Alive&&injury&&(injury.NeedsRescue||injury.Bandaged);
            patch.enabled=active&&original.enabled;if(!active)return;
            patch.sprite=atlas[injury.Bandaged?(injury.Incapacitated?6:4+Mathf.Abs(body.GetInstanceID())%4):body.police||body.military?3:Mathf.Abs(body.GetInstanceID())%3];
            var bounds=original.sprite?original.sprite.bounds:new Bounds(Vector3.up,Vector3.one*2);
            patch.transform.localPosition=new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*.61f,-.025f);
            float width=body.police||body.military?.35f:.26f;patch.transform.localScale=Vector3.one*(width/Mathf.Max(.01f,patch.sprite.bounds.size.x));
            if(!injury.Incapacitated&&!CivilianImpact.Active(this)&&Camera.main)original.transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,Mathf.Sin(Time.time*1.6f)*2-6);
        }
        void OnDestroy(){if(patch)Destroy(patch.gameObject);if(proneView)Destroy(proneView.gameObject);}
    }
}
