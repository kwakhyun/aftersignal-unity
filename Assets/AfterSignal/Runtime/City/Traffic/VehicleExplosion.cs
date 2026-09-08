using UnityEngine;

namespace AfterSignal
{
    public static class VehicleExplosion
    {
        public static void Create(Vector3 p, float radius)
        {
            ExplosionPresentation.Detonate(p,radius);
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
