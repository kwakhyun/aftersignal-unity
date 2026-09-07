using UnityEngine;

namespace AfterSignal
{
    public sealed class CameraRig : MonoBehaviour
    {
        public GameDirector director;
        public bool ReducedMotion { get; set; }

        Vector3 velocity, impulse, basePosition;
        float shake, lead = 2;
        readonly RaycastHit[] walls = new RaycastHit[24];
        bool Driving => UrbanSimulation.Instance && UrbanSimulation.Instance.Driving;

        bool Viewing => CityLife.Instance && CityLife.Instance.Panorama;

        bool Stairwell => director.stage == StageId.Residence && director.Player.transform.position.x > 54;

        public void Kick(float amount)
        {
            Impact(Vector3.right, amount * .65f);
        }

        public void Impact(Vector3 direction, float amount)
        {
            if (ReducedMotion || PresentationSettings.Motion < .01f)
                return;
            impulse = Vector3.ClampMagnitude(impulse + new Vector3(direction.x, .35f, 0) * amount, .16f) * PresentationSettings.Motion;
            shake = .13f;
        }

        Quaternion Angle => Viewing ? Quaternion.LookRotation(director.Player.Shoulder - basePosition) : Driving ? Quaternion.LookRotation(UrbanSimulation.Instance.Current.transform.position + UrbanSimulation.Instance.Current.Forward * 5 + Vector3.up * 1.5f - basePosition, Vector3.up) : Stairwell ? Quaternion.LookRotation(director.Player.transform.position + Vector3.up * 1.1f - basePosition, Vector3.up) : director.stage == StageId.UrbanCity || director.stage == StageId.UrbanInterior ? Quaternion.Euler(20, -8, 0) : CivicWorld.Interior(director.stage) ? Quaternion.Euler(14, -5, 0) : CivicWorld.Exploration(director.stage) ? Quaternion.Euler(16, -8, 0) : Quaternion.Euler(10, 0, 0);

        public void Snap()
        {
            basePosition = Target();
            velocity = Vector3.zero;
            transform.position = basePosition;
            transform.rotation = Angle;
        }

        Vector3 Target()
        {
            var p = director.Player;
            if (Viewing)
            {
                var focus = p.Shoulder;
                var desired = focus + Quaternion.Euler(CityLife.Instance.PanoramaPitch, CityLife.Instance.PanoramaYaw, 0) * Vector3.back * 32;
                var ray = desired - focus;
                float length = ray.magnitude;
                int n = Physics.SphereCastNonAlloc(focus, .5f, ray.normalized, walls, length, 1, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = walls[i];
                    if (h.collider && h.collider.bounds.max.y > p.transform.position.y + 1.5f)
                        length = Mathf.Min(length, Mathf.Max(4, h.distance - .7f));
                }

                return focus + ray.normalized * length;
            }

            // A low sightline passes beneath the alternating stair flights.
            // Only the view changes; the real decks and collisions remain in place.
            if (Stairwell)
                return p.transform.position + new Vector3(.7f, 2.35f, -10);
            if (Driving)
            {
                var car = UrbanSimulation.Instance.Current;
                var focus = car.transform.position + Vector3.up * 1.6f;
                float chaseDistance = car.HalfLength + 8 + Mathf.Abs(car.speed) * .06f;
                var desired = car.transform.position - car.Forward * chaseDistance + Vector3.up * 5.3f;
                var ray = desired - focus;
                float length = ray.magnitude;
                int n = Physics.SphereCastNonAlloc(focus, .35f, ray.normalized, walls, length, 1, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var h = walls[i];
                    if (!h.collider || h.collider.transform.IsChildOf(car.transform) || h.collider.GetComponentInParent<CityBuildingCutaway>())
                        continue;
                    length = Mathf.Min(length, Mathf.Max(3, h.distance - .2f));
                }

                return focus + ray.normalized * length;
            }

            if (director.stage == StageId.UrbanInterior)
                return p.transform.position + new Vector3(2.2f, 6.2f, -15);
            if (director.stage == StageId.UrbanCity)
                return p.transform.position + new Vector3(2.4f, 7, -17);
            float distance = director.stage == StageId.Roof ? 23 : 21;
            bool explore = CivicWorld.Exploration(director.stage), inside = CivicWorld.Interior(director.stage);
            if (explore)
                distance = inside ? 14 : 18;
            float rise = inside ? 4 : explore ? 5.6f : 4.5f;
            return new Vector3(Mathf.Clamp(p.transform.position.x + lead + (inside ? 1.2f : explore ? 2 : 0), inside ? 7 : 10, director.stageLength - 9), Mathf.Max(rise, p.transform.position.y + rise), -distance + p.transform.position.z * (explore ? 1 : .22f));
        }

        void LateUpdate()
        {
            if (!director || !director.Player || director.Blocked)
                return;
            lead = Mathf.Lerp(lead, Mathf.Clamp(director.Player.Velocity.x * .22f + director.Player.Facing * .65f, -2.4f, 2.4f), 1 - Mathf.Exp(-Time.deltaTime * 4));
            basePosition = Vector3.SmoothDamp(basePosition, Target(), ref velocity, Driving ? .22f : .14f);
            shake = Mathf.Max(0, shake - Time.unscaledDeltaTime);
            impulse = Vector3.Lerp(impulse, Vector3.zero, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 24));
            transform.position = basePosition + (ReducedMotion ? Vector3.zero : impulse * Mathf.Cos((.13f - shake) * 46));
            transform.rotation = Quaternion.Slerp(transform.rotation, Angle, 1 - Mathf.Exp(-Time.deltaTime * (Driving ? 9 : 14)));
            var camera = GetComponent<Camera>();
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, Viewing ? 62 : Driving ? 56 : 45, 1 - Mathf.Exp(-Time.deltaTime * 5));
        }
    }
}
