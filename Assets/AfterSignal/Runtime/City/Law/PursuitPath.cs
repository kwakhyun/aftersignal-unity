using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // Small local A* search around actual scene colliders. Rebuilt only when the target moves.
    public sealed class PursuitPath
    {
        readonly List<Vector3> route = new List<Vector3>();
        int index;
        float next;
        Vector3 last;
        public Vector3 Direction(Vector3 from, Vector3 target)
        {
            target.y = from.y;
            if (Time.time >= next && (route.Count == 0 || Vector3.Distance(last, target) > 4 || index >= route.Count))
            {
                next = Time.time + 1.5f;
                last = target;
                Build(from, target);
            }

            while (index < route.Count && Vector3.Distance(from, route[index]) < .75f)
                index++;
            var d = (index < route.Count ? route[index] : target) - from;
            d.y = 0;
            return d.normalized;
        }

        void Build(Vector3 start, Vector3 target)
        {
            route.Clear();
            index = 0;
            float y = start.y;
            if (!Physics.SphereCast(start + Vector3.up, .38f, (target - start).normalized, out _, Vector3.Distance(start, target), 1, QueryTriggerInteraction.Ignore))
            {
                route.Add(target);
                return;
            }

            Vector2Int begin = new Vector2Int(Mathf.RoundToInt(start.x / 2), Mathf.RoundToInt(start.z / 2)), end = new Vector2Int(Mathf.RoundToInt(target.x / 2), Mathf.RoundToInt(target.z / 2));
            var open = new List<Vector2Int>
            {
                begin
            };
            var visited = new HashSet<Vector2Int>();
            var cost = new Dictionary<Vector2Int, float>
            {
                {
                    begin,
                    0
                }
            };
            var parent = new Dictionary<Vector2Int, Vector2Int>();
            Vector2Int best = begin;
            float nearest = Vector2Int.Distance(begin, end);
            Vector2Int[] axes =
            {
                Vector2Int.up,
                Vector2Int.right,
                Vector2Int.down,
                Vector2Int.left
            };
            for (int count = 0; count < 420 && open.Count > 0; count++)
            {
                int selected = 0;
                float score = float.MaxValue;
                for (int j = 0; j < open.Count; j++)
                {
                    float f = cost[open[j]] + Vector2Int.Distance(open[j], end);
                    if (f < score)
                    {
                        score = f;
                        selected = j;
                    }
                }

                var current = open[selected];
                open.RemoveAt(selected);
                visited.Add(current);
                float distance = Vector2Int.Distance(current, end);
                if (distance < nearest)
                {
                    nearest = distance;
                    best = current;
                }

                if (current == end)
                    break;
                foreach (var axis in axes)
                {
                    var cell = current + axis;
                    if (visited.Contains(cell) || Mathf.Abs(cell.x - begin.x) > 35 || Mathf.Abs(cell.y - begin.y) > 35)
                        continue;
                    Vector3 p = new Vector3(cell.x * 2, y, cell.y * 2), a = new Vector3(current.x * 2, y + 1, current.y * 2);
                    if (Physics.CheckCapsule(p + Vector3.up * .5f, p + Vector3.up * 1.6f, .38f, 1, QueryTriggerInteraction.Ignore) || Physics.SphereCast(a, .38f, (p + Vector3.up - a).normalized, out _, 2, 1, QueryTriggerInteraction.Ignore) || !Physics.Raycast(p + Vector3.up * .5f, Vector3.down, 1.3f, 1, QueryTriggerInteraction.Ignore))
                        continue;
                    float c = cost[current] + 1;
                    if (cost.TryGetValue(cell, out float old) && old <= c)
                        continue;
                    cost[cell] = c;
                    parent[cell] = current;
                    if (!open.Contains(cell))
                        open.Add(cell);
                }
            }

            var cursor = best;
            for (int i = 0; i < 200 && cursor != begin; i++)
            {
                route.Add(new Vector3(cursor.x * 2, y, cursor.y * 2));
                if (!parent.TryGetValue(cursor, out cursor))
                    break;
            }

            route.Reverse();
        }
    }
}
