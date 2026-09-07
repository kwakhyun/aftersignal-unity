using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // One coordinate contract for lane meshes, traffic, crossings and navigation.
    public static class CityRoadNetwork
    {
        public const float Spacing = 140, Lane = 5, Corner = 16, StopLine = 21, Cycle = 66;
        public static float Clock => Mathf.Repeat(Time.timeSinceLevelLoad, Cycle);

        public static int Signal(bool eastWest) => SignalAt(Clock, eastWest);
        public static int SignalAt(float seconds, bool eastWest)
        {
            float t = Mathf.Repeat(seconds, Cycle);
            return eastWest ? (t < 11 ? 2 : t < 13 ? 1 : 0) : (t >= 20 && t < 31 ? 2 : t >= 31 && t < 33 ? 1 : 0);
        }

        public static bool WalkAt(float seconds)
        {
            float t = Mathf.Repeat(seconds, Cycle);
            return t >= 42 && t < 64;
        }

        public static bool Walk => WalkAt(Clock);

        public static bool CanStartCrossing(float seconds) => Walk && 64 - Clock > seconds + 1;
        public static Vector3 Junction(int col, int row) => new Vector3(40 + col * Spacing, .02f, -280 + row * Spacing);
        public static Vector3 NearestJunction(Vector3 p) => Junction(Mathf.Clamp(Mathf.RoundToInt((p.x - 40) / Spacing), 0, 5), Mathf.Clamp(Mathf.RoundToInt((p.z + 280) / Spacing), 0, 4));
        public static Vector3 CornerPoint(int col, int row, int sx, int sz) => Junction(col, row) + new Vector3(sx * Corner, .04f, sz * Corner);
        public static Vector3 Sidewalk(Vector3 p)
        {
            var j = NearestJunction(p);
            return j + new Vector3(p.x >= j.x ? Corner : -Corner, .04f, p.z >= j.z ? Corner : -Corner);
        }

        public static Vector3 NextWalk(Vector3 p, int choice, out bool crossing)
        {
            var j = NearestJunction(p);
            int col = Mathf.RoundToInt((j.x - 40) / Spacing), row = Mathf.RoundToInt((j.z + 280) / Spacing), sx = p.x >= j.x ? 1 : -1, sz = p.z >= j.z ? 1 : -1;
            crossing = choice < 2;
            if (choice == 0)
                return CornerPoint(col, row, -sx, sz);
            if (choice == 1)
                return CornerPoint(col, row, sx, -sz);
            if (choice == 2 && col + sx >= 0 && col + sx <= 5)
                return CornerPoint(col + sx, row, -sx, sz);
            if (choice == 3 && row + sz >= 0 && row + sz <= 4)
                return CornerPoint(col, row + sz, sx, -sz);
            crossing = true;
            return CornerPoint(col, row, -sx, sz);
        }

        public static Vector3[] TrafficLoop(int col, int row)
        {
            float l = 40 + col * Spacing, r = l + Spacing, b = -280 + row * Spacing, t = b + Spacing;
            var points = new List<Vector3>();
            // Clockwise, right turns only. The centreline is five metres right of road centre.
            Arc(points, new Vector2(l + 10, t - 10), 180, 90);
            Arc(points, new Vector2(r - 10, t - 10), 90, 0);
            Arc(points, new Vector2(r - 10, b + 10), 0, -90);
            Arc(points, new Vector2(l + 10, b + 10), -90, -180);
            return points.ToArray();
        }

        static void Arc(List<Vector3> list, Vector2 center, float a, float b)
        {
            for (int i = 0; i <= 6; i++)
            {
                float rad = Mathf.Lerp(a, b, i / 6f) * Mathf.Deg2Rad;
                list.Add(new Vector3(center.x + Mathf.Cos(rad) * 5, .02f, center.y + Mathf.Sin(rad) * 5));
            }
        }

        public static float StopDistance(Vector3 p, Vector3 forward, float nose)
        {
            bool ew = Mathf.Abs(forward.x) > .97f, ns = Mathf.Abs(forward.z) > .97f;
            if (!ew && !ns)
                return float.PositiveInfinity;
            if (Signal(ew) == 2)
                return float.PositiveInfinity;
            float best = float.PositiveInfinity;
            for (int c = 0; c < 6; c++)
                for (int r = 0; r < 5; r++)
                {
                    var d = Junction(c, r) - p;
                    float along = Vector3.Dot(d, forward), side = Mathf.Abs(ew ? d.z : d.x);
                    // Vehicles already beyond the stop line finish clearing the junction.
                    if (side < 9 && along >= StopLine + nose - .25f && along < 135)
                        best = Mathf.Min(best, along - StopLine - nose);
                }

            return best;
        }

        public static List<Vector3> Navigation(Vector3 from, Vector3 to)
        {
            var result = new List<Vector3>
            {
                from
            };
            var a = NearestJunction(from);
            var b = NearestJunction(to);
            // Orthogonal road route; approach the nearest street from a facility forecourt.
            var start = new Vector3(from.x, .15f, a.z);
            result.Add(start);
            result.Add(new Vector3(b.x, .15f, a.z));
            result.Add(new Vector3(b.x, .15f, b.z));
            result.Add(new Vector3(to.x, .15f, b.z));
            result.Add(to);
            return result;
        }
    }
}
