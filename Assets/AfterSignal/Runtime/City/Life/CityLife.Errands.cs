using UnityEngine;

namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void InstallErrandMarker()
        {
            if (game.stage != StageId.UrbanCity)
                return;
            var q = LifeState.Errand;
            if (q == null || !q.accepted || q.completed)
                return;
            var old = GameObject.Find("Hidden errand destination");
            if (old)
                Destroy(old);
            var go = new GameObject("Hidden errand destination");
            Vector3 p = UrbanCatalog.Door(q.site);
            if (q.kind == "rooftop")
                p = CityRooftop.Destination(q.site);
            go.transform.position = p + Vector3.up * 1.2f;
            var point = go.AddComponent<InteractionPoint>();
            point.kind = InteractionKind.HiddenErrand;
            point.title = q.title + " · 확인";
            point.radius = 4;
            var glyph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glyph.transform.SetParent(go.transform, false);
            glyph.transform.localScale = Vector3.one * .4f;
            Destroy(glyph.GetComponent<Collider>());
            glyph.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Materials/GoldFX");
        }

        void CompleteErrand()
        {
            var q = LifeState.Errand;
            if (q == null || !q.accepted || q.completed)
                return;
            q.completed = true;
            LifeState.Earn(Mathf.Clamp(q.reward, 120, 350));
            game.Toast("의뢰 완료 · " + q.title + " +" + q.reward + " C", 5);
            if (Mode.Length > 0)
                Dismiss();
        }
    }
}
