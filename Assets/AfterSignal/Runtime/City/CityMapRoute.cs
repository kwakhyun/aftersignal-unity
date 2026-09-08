using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed class CityMapRoute : MaskableGraphic
    {
        public bool large;
        List<Vector3> route;
        public void SetRoute(List<Vector3> points)
        {
            route = points;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (route == null)
                return;
            for (int i = 1; i < route.Count; i++)
            {
                Vector2 a = Point(route[i - 1]), b = Point(route[i]);
                if ((b - a).sqrMagnitude < .01f)
                    continue;
                Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * (large ? 2 : 1);
                int k = vh.currentVertCount;
                vh.AddVert(a + n, color, Vector2.zero);
                vh.AddVert(a - n, color, Vector2.zero);
                vh.AddVert(b - n, color, Vector2.zero);
                vh.AddVert(b + n, color, Vector2.zero);
                vh.AddTriangle(k, k + 1, k + 2);
                vh.AddTriangle(k, k + 2, k + 3);
            }
        }

        Vector2 Point(Vector3 p){var map=ExpansionRoads.Map(p);return new Vector2(map.x*rectTransform.rect.width,-(1-map.y)*rectTransform.rect.height);}
    }
}
