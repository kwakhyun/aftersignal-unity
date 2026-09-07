using UnityEngine;

namespace AfterSignal
{
    // Local foreground cutaway keeps real depth for actors, buildings and everything else.
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class FoliageOcclusion : MonoBehaviour
    {
        MeshRenderer surface;
        MaterialPropertyBlock properties;
        float visibility = 1;
        void Awake()
        {
            surface = GetComponent<MeshRenderer>();
            properties = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            var g = GameDirector.Instance;
            var c = Camera.main;
            if (!g || !g.Player || !c)
                return;
            var delta = g.Player.Shoulder - c.transform.position;
            bool blocking = surface.bounds.IntersectRay(new Ray(c.transform.position, delta.normalized), out float near) && near < delta.magnitude - .3f;
            visibility = Mathf.MoveTowards(visibility, blocking ? .16f : 1, Time.unscaledDeltaTime * 3);
            properties.SetColor("_BaseColor", new Color(1, 1, 1, visibility));
            properties.SetFloat("_Cutoff", .03f);
            surface.SetPropertyBlock(properties);
        }
    }
}
