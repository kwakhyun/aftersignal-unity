using UnityEngine;

namespace AfterSignal
{
    public static class VehicleExplosion
    {
        public static void Create(Vector3 p, float radius)
        {
            if (PresentationSettings.Effects <= 0)
                return;
            SignalEffects.Burst(p + Vector3.up, SignalEffects.Gold, 32, 12);
            SignalEffects.Burst(p + Vector3.up, SignalEffects.Red, 20, 8);
            Smoke(p + Vector3.up, 3);
            Flame(p + Vector3.up);
            for (int i = 0; i < 8; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Vehicle fragment";
                Object.Destroy(go.GetComponent<Collider>());
                go.transform.position = p + Vector3.up;
                go.transform.localScale = new Vector3(.15f, .12f, .35f);
                go.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/DarkMetal");
                var fx = go.AddComponent<TransientEffect>();
                fx.life = 1.8f;
                fx.fall = true;
                fx.velocity = new Vector3(Random.Range(-6f, 6f), Random.Range(4f, 9f), Random.Range(-6f, 6f));
            }

            SignalEffects.Ring(p + Vector3.up, SignalEffects.Gold, radius, .24f);
        }

        static void Flame(Vector3 p)
        {
            var go = new GameObject("Vehicle fire burst");
            go.transform.position = p;
            go.AddComponent<EffectBudget>();
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .55f);
            main.startSpeed = 4;
            main.startSize = new ParticleSystem.MinMaxCurve(.8f, 2f);
            main.startColor = new Color(1, .6f, .12f, .9f);
            main.maxParticles = 20;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.8f, .2f, .06f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .7f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Materials/VehicleSmoke");
            ps.Emit(16);
            Object.Destroy(go, 1);
        }

        public static void Smoke(Vector3 p, int size)
        {
            if (PresentationSettings.Effects <= 0 || EffectBudget.Active >= 40)
                return;
            var go = new GameObject("Vehicle smoke");
            go.transform.position = p;
            go.AddComponent<EffectBudget>();
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = 2;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.8f, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .5f), new Keyframe(1, 1.8f)));
            main.startSpeed = .8f;
            main.startSize = .6f * size;
            main.startColor = new Color(.13f, .15f, .16f, .6f);
            main.maxParticles = 16;
            main.gravityModifier = -.04f;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .25f;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load<Material>("Materials/VehicleSmoke");
            ps.Emit(size * 3);
            Object.Destroy(go, 2.5f);
        }
    }
}
