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
            if (!g || !g.Player || surface.bounds.SqrDistance(g.Player.Shoulder) > 80 * 80 && visibility >= 1)
                return;
            var c = Camera.main;
            if (!c)
                return;
            var delta = g.Player.Shoulder - c.transform.position;
            bool blocking = surface.bounds.IntersectRay(new Ray(c.transform.position, delta.normalized), out float near) && near < delta.magnitude - .3f;
            float previous = visibility;
            visibility = Mathf.MoveTowards(visibility, blocking ? .16f : 1, Time.unscaledDeltaTime * 3);
            if (Mathf.Approximately(previous, visibility)) return;
            properties.SetColor("_BaseColor", new Color(1, 1, 1, visibility));
            properties.SetFloat("_Cutoff", .03f);
            surface.SetPropertyBlock(properties);
        }
    }
}
