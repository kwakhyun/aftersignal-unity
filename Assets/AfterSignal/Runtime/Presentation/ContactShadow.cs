using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal
{
    public sealed class ContactShadow : MonoBehaviour
    {
        Transform shadow;
        MeshRenderer visual;
        MaterialPropertyBlock properties;
        static readonly int Tint = Shader.PropertyToID("_BaseColor");
        float next;
        void Start()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Ground contact / visual only";
            Destroy(go.GetComponent<Collider>());
            shadow = go.transform;
            visual = go.GetComponent<MeshRenderer>();
            visual.sharedMaterial = Resources.Load<Material>("Materials/ContactShadow");
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            properties = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (!shadow)
                return;
            if(Time.time<next)return;next=Time.time+.06f;
            if(ActorWorkBudget.DistanceSquared(this)>75*75){visual.enabled=false;next=Time.time+.3f;return;}
            if (Physics.Raycast(transform.position + Vector3.up * .4f, Vector3.down, out var hit, 16, 1, QueryTriggerInteraction.Ignore))
            {
                float height = Mathf.Max(0, hit.distance - .4f);
                visual.enabled = height < 12;
                shadow.position = hit.point + hit.normal * .025f;
                shadow.rotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
                float scale = 1 + Mathf.Min(height, 8) * .055f;
                shadow.localScale = new Vector3(1.12f, .64f, 1) * scale;
                properties.SetColor(Tint, new Color(.01f, .025f, .03f, Mathf.Lerp(.52f, .12f, Mathf.Clamp01(height / 7))));
                visual.SetPropertyBlock(properties);
            }
            else
                visual.enabled = false;
        }

        void OnDisable()
        {
            if (visual)
                visual.enabled = false;
        }

        void OnDestroy()
        {
            if (shadow)
                Destroy(shadow.gameObject);
        }
    }
}
