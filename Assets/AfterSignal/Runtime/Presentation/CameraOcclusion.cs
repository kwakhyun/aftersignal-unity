using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    [DefaultExecutionOrder(200)]
    public sealed class CameraOcclusion : MonoBehaviour
    {
        sealed class Surface
        {
            public Material[] originals, faded;
            public float opacity = 1;
            public MaterialPropertyBlock block = new MaterialPropertyBlock();
        }

        readonly Dictionary<Renderer, Surface> surfaces = new Dictionary<Renderer, Surface>();
        readonly Dictionary<Material, Material> materials = new Dictionary<Material, Material>();
        readonly HashSet<Renderer> blocking = new HashSet<Renderer>();
        readonly RaycastHit[] hits = new RaycastHit[96];
        float next;
        readonly Collider[] overlaps = new Collider[32];
        readonly List<Renderer> roofStructures = new List<Renderer>();
        void Awake()
        {
            foreach (var renderer in FindObjectsByType<MeshRenderer>())
                if (renderer.name == "Crown setback" || renderer.name == "Crown light")
                    roofStructures.Add(renderer);
        }

        void LateUpdate()
        {
            var g = GameDirector.Instance;
            if (!g || !g.Player)
                return;
            if (Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + .055f;
                blocking.Clear();
                Vector3 start = transform.position, end = g.Player.Shoulder;
                var line = end - start;
                int count = Physics.SphereCastNonAlloc(start, .38f, line.normalized, hits, Mathf.Max(0, line.magnitude - .6f), 1, QueryTriggerInteraction.Ignore);
                int inside = Physics.OverlapSphereNonAlloc(start, .55f, overlaps, 1, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < inside; i++)
                {
                    var c = overlaps[i];
                    if (!c || c.GetComponentInParent<CityVehicle>())
                        continue;
                    var structure=c.GetComponentInParent<CollapsibleBuilding>();
                    if(structure){foreach(var r in structure.Presentation)Mark(r);continue;}
                    var group = c.GetComponentInParent<CityBuildingCutaway>();
                    if (group && group.upper != null)
                    {
                        foreach (var r in group.upper)
                            Mark(r);
                    }
                    else
                        Mark(c.GetComponent<Renderer>());
                }

                for (int i = 0; i < count; i++)
                {
                    var c = hits[i].collider;
                    if (!c || c.GetComponentInParent<CityVehicle>() || c.GetComponentInParent<BreakableGlass>())
                        continue;
                    if (c.bounds.max.y < g.Player.transform.position.y + .12f)
                        continue;
                    var structure=c.GetComponentInParent<CollapsibleBuilding>();
                    if(structure){foreach(var r in structure.Presentation)Mark(r);continue;}
                    var group = c.GetComponentInParent<CityBuildingCutaway>();
                    if (group && group.upper != null)
                    {
                        foreach (var r in group.upper)
                            Mark(r);
                    }
                    else
                        Mark(c.GetComponent<Renderer>());
                }

                foreach (var renderer in roofStructures)
                    MarkVisualObstruction(renderer, start, line, g.Player.transform.position.y);
                foreach (var group in CityBuildingCutaway.All)
                    if (group && group.visualOnly && group.upper != null)
                        foreach (var r in group.upper)
                        {
                            MarkVisualObstruction(r, start, line, g.Player.transform.position.y);
                        }
            }

            foreach (var pair in surfaces)
            {
                var r = pair.Key;
                if (!r)
                    continue;
                var surface = pair.Value;
                float target = blocking.Contains(r) ? .14f : 1;
                if (target == 1 && surface.opacity >= 1)
                    continue;
                surface.opacity = Mathf.MoveTowards(surface.opacity, target, Time.unscaledDeltaTime * 5);
                if (surface.opacity < .999f)
                {
                    r.sharedMaterials = surface.faded;
                    r.GetPropertyBlock(surface.block);
                    surface.block.SetFloat("_Opacity", surface.opacity);
                    r.SetPropertyBlock(surface.block);
                }
                else
                    r.sharedMaterials = surface.originals;
            }
        }

        void MarkVisualObstruction(Renderer renderer, Vector3 start, Vector3 line, float playerHeight)
        {
            if (!renderer || renderer.bounds.max.y < playerHeight + .15f)
                return;
            var bounds = renderer.bounds;
            bounds.Expand(.3f);
            if (bounds.IntersectRay(new Ray(start, line.normalized), out float distance) && distance < line.magnitude - .5f)
                Mark(renderer);
        }

        void Mark(Renderer r)
        {
            if (!r || !r.enabled || !r.gameObject.activeInHierarchy)
                return;
            blocking.Add(r);
            if (surfaces.ContainsKey(r))
                return;
            var template = Resources.Load<Material>("Materials/BuildingFade");
            if (!template)
                return;
            var surface = new Surface
            {
                originals = r.sharedMaterials
            };
            surface.faded = new Material[surface.originals.Length];
            for (int i = 0; i < surface.originals.Length; i++)
            {
                var source = surface.originals[i];
                if (!source)
                {
                    surface.faded[i] = template;
                    continue;
                }

                if (!materials.TryGetValue(source, out var faded))
                {
                    faded = new Material(template);
                    faded.name = source.name + " / sightline";
                    faded.CopyPropertiesFromMaterial(source);
                    faded.renderQueue = 3000;
                    faded.SetFloat("_Triplanar", source.shader.name.Contains("Urban Surface") ? 1 : 0);
                    materials[source] = faded;
                }

                surface.faded[i] = faded;
            }

            surfaces.Add(r, surface);
        }

        void OnDisable()
        {
            foreach (var pair in surfaces)
                if (pair.Key)
                    pair.Key.sharedMaterials = pair.Value.originals;
        }

        void OnDestroy()
        {
            OnDisable();
            foreach (var mat in materials.Values)
                if (mat)
                    Destroy(mat);
        }
    }
}
