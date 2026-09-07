using UnityEngine;

namespace AfterSignal
{
    public sealed class SceneLightBudget : MonoBehaviour
    {
        Light[] lights;
        float next;
        void Start()
        {
            lights = FindObjectsByType<Light>();
        }

        void Update()
        {
            if (Time.unscaledTime < next)
                return;
            next = Time.unscaledTime + .25f;
            var cam = Camera.main;
            if (!cam)
                return;
            foreach (var light in lights)
                if (light && light.type != LightType.Directional)
                {
                    if (light.GetComponentInParent<CityVehicle>())
                        continue;
                    var g = GameDirector.Instance;
                    bool city = g && g.stage == StageId.UrbanCity;
                    light.enabled = city ? (light.transform.position - cam.transform.position).sqrMagnitude < 48 * 48 : Mathf.Abs(light.transform.position.x - cam.transform.position.x) < 24 && Mathf.Abs(light.transform.position.y - cam.transform.position.y) < 16;
                }
        }

        void OnDestroy()
        {
            if (lights != null)
                foreach (var light in lights)
                    if (light)
                        light.enabled = true;
        }
    }
}
