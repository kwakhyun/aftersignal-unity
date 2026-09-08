using UnityEngine;

namespace AfterSignal
{
    // A vehicle body is never a road. All probes share this filter, including recovery.
    public static class VehicleGround
    {
        public static bool Sample(CityVehicle car, Vector3 at, float rise, float drop, out RaycastHit floor)
        {
            floor = default;
            float best = float.PositiveInfinity;
            foreach (var h in Physics.RaycastAll(at + Vector3.up * rise, Vector3.down, rise + drop, 1, QueryTriggerInteraction.Ignore))
            {
                if (!h.collider || h.normal.y < .72f || h.collider.transform.IsChildOf(car.transform)
                    || h.collider.GetComponentInParent<CityVehicle>() || h.collider.attachedRigidbody) continue;
                if (h.distance < best) { best = h.distance; floor = h; }
            }
            return best < float.PositiveInfinity;
        }

        public static void Settle(CityVehicle car, float dt, bool recover = false)
        {
            if (car.IsAircraft || car.IsWatercraft) return;
            var p = car.transform.position;
            if (Sample(car, p, .65f, recover ? 2000 : 5, out var hit))
            {
                // Only a kerb-sized upward step is allowed. Roofs above the chassis cannot lift it.
                if (hit.point.y <= p.y + .38f)
                {
                    p.y = recover ? hit.point.y + .025f : Mathf.MoveTowards(p.y, hit.point.y + .025f, dt * 18);
                    car.transform.position = p;
                }
            }
            else if (p.y > -2)
            { p.y -= dt * 12; car.transform.position = p; }
        }
    }
}
