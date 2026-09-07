using UnityEngine;

namespace AfterSignal
{
    public sealed class CityClock : MonoBehaviour
    {
        GameDirector game;
        Light sun;
        Material sky;
        float next;
        public void Initialize(GameDirector owner)
        {
            game = owner;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }

            if (game.stage == StageId.UrbanCity || game.stage == StageId.Haven)
            {
                var template = Resources.Load<Material>("Materials/CitySky");
                if (template)
                {
                    sky = new Material(template);
                    RenderSettings.skybox = sky;
                    Camera.main.clearFlags = CameraClearFlags.Skybox;
                }
            }

            Apply();
        }

        void Update()
        {
            if (!game || !game.Ready)
                return;
            if (!game.Blocked)
                LifeState.Hours += Time.deltaTime / 75f;
            if (Time.unscaledTime >= next)
            {
                next = Time.unscaledTime + .25f;
                Apply();
            }
        }

        void Apply()
        {
            if (!sun || !game || !CivicWorld.Exploration(game.stage))
                return;
            float hour = LifeState.Hour, altitude = Mathf.Sin((hour - 6) / 24 * Mathf.PI * 2);
            float daylight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.12f, .32f, altitude));
            bool indoor = CivicWorld.Interior(game.stage);
            sun.color = Color.Lerp(new Color(.36f, .54f, .88f), Color.Lerp(new Color(1, .57f, .33f), new Color(1, .94f, .82f), Mathf.Clamp01(altitude * 2)), daylight);
            sun.intensity = indoor ? Mathf.Lerp(.65f, 1.25f, daylight) : Mathf.Lerp(.28f, 1.45f, daylight);
            sun.transform.rotation = Quaternion.Euler(altitude >= 0 ? (hour - 6) * 15 : Mathf.Repeat(hour + 6, 24) * 15, -32, 0);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(.14f, .2f, .34f), new Color(.54f, .65f, .75f), daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.09f, .14f, .23f), new Color(.4f, .47f, .53f), daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(.04f, .07f, .12f), new Color(.23f, .25f, .28f), daylight);
            if (!indoor)
            {
                float dusk = (1 - Mathf.Abs(altitude) * 3) * daylight;
                Color fog = Color.Lerp(new Color(.035f, .065f, .14f), new Color(.32f, .46f, .57f), daylight);
                fog = Color.Lerp(fog, new Color(.61f, .34f, .29f), Mathf.Clamp01(dusk) * .5f);
                RenderSettings.fogColor = fog;
                Camera.main.backgroundColor = fog;
                if (game.stage == StageId.UrbanCity)
                    RenderSettings.fogDensity = Mathf.Lerp(.00165f, .0009f, Mathf.InverseLerp(15, 95, game.Player.transform.position.y));
                if (sky)
                {
                    sky.SetColor("_Horizon", fog);
                    sky.SetColor("_Zenith", Color.Lerp(new Color(.009f, .017f, .065f), new Color(.14f, .31f, .57f), daylight));
                    sky.SetFloat("_Night", 1 - daylight);
                    sky.SetVector("_SunDirection", -sun.transform.forward);
                    sky.SetColor("_SunColor", sun.color);
                }
            }
        }

        void OnDestroy()
        {
            if (sky)
                Destroy(sky);
        }
    }
}
