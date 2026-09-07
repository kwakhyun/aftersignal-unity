using UnityEngine;

namespace AfterSignal
{
    public sealed class RopeMotor : MonoBehaviour
    {
        public GrappleAnchor Target { get; private set; }
        public GrappleAnchor Candidate { get; private set; }
        public bool Attached => Target;
        public float Length { get; private set; }
        public int AttachCount { get; private set; }

        PlayerMotor player;
        LineRenderer line;
        bool previousHeld;
        public float Range => player && CivicWorld.Exploration(player.Director.stage) ? 36 : player.Tuning.ropeRange;

        public void Initialize(PlayerMotor owner)
        {
            player = owner;
            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = 13;
            line.widthMultiplier = .037f;
            line.sharedMaterial = Resources.Load<Material>("Materials/CyanFX");
            line.numCapVertices = 3;
            line.startColor = Color.white;
            line.endColor = new Color(.1f, .8f, 1);
            line.enabled = false;
        }

        public GrappleAnchor Select(Vector2 pointer)
        {
            var cam = Camera.main;
            if (!cam)
                return null;
            float score = float.MaxValue;
            GrappleAnchor best = null;
            foreach (var anchor in GrappleAnchor.All)
            {
                if (!anchor || !anchor.isActiveAndEnabled)
                    continue;
                float dist = Vector3.Distance(player.Shoulder, anchor.transform.position);
                if (dist > Range || dist < 1)
                    continue;
                var screen = cam.WorldToScreenPoint(anchor.transform.position);
                if (screen.z <= 0)
                    continue;
                float pixels = Vector2.Distance(pointer, new Vector2(screen.x, screen.y));
                if (pixels > player.Tuning.aimAssistPixels * Screen.height / 900f)
                    continue;
                if (Physics.Linecast(player.Shoulder, anchor.transform.position, 1, QueryTriggerInteraction.Ignore))
                    continue;
                float next = pixels + dist * .8f;
                if (next < score)
                {
                    score = next;
                    best = anchor;
                }
            }

            return best;
        }

        public void Tick(ControlFrame input, float dt)
        {
            Candidate = Select(input.pointer);
            if (input.grapple && !previousHeld && !Attached && Candidate)
                Attach(Candidate);
            if (!input.grapple && previousHeld)
                Release();
            previousHeld = input.grapple;
            if (!Attached)
                return;
            if (Physics.Linecast(player.Shoulder, Target.transform.position, 1, QueryTriggerInteraction.Ignore))
            {
                Release();
                return;
            }

            Length = Mathf.Clamp(Length - (input.move.y * (Target.cityAnchor ? 17 : player.Tuning.ropeReelSpeed) + 1.6f) * dt, 1.3f, Range);
            Vector3 delta = Target.transform.position - player.Shoulder;
            var v = player.Velocity;
            v += delta.normalized * (Mathf.Max(0, delta.magnitude - Length) * 48f + 5f) * dt;
            v.x += input.move.x * 17f * dt;
            player.Velocity = Vector3.ClampMagnitude(v, Target.cityAnchor ? 32 : 24f);
            if (Target.cityAnchor && Target.hasLanding && input.move.y > .1f && Vector3.Distance(player.Shoulder, Target.transform.position) < 3.3f)
            {
                var landing = Target.landing;
                var d = landing - player.transform.position;
                if (d.magnitude < 4 && !Physics.CheckCapsule(landing + Vector3.up * .42f, landing + Vector3.up * 1.7f, .34f, 1, QueryTriggerInteraction.Ignore) && !Physics.Linecast(player.Shoulder, landing + Vector3.up * 1.2f, 1, QueryTriggerInteraction.Ignore))
                {
                    player.Controller.Move(d);
                    player.Velocity = Vector3.zero;
                    player.Carry(Vector3.zero);
                    Release();
                    return;
                }
            }

            if (input.jump)
            {
                player.Velocity += Vector3.up * 5.5f;
                Release();
            }
        }

        public void Attach(GrappleAnchor anchor)
        {
            if (!anchor || Vector3.Distance(player.Shoulder, anchor.transform.position) > Range)
                return;
            Target = anchor;
            Length = Vector3.Distance(player.Shoulder, anchor.transform.position) * .94f;
            AttachCount++;
            player.Velocity += (anchor.transform.position - player.Shoulder).normalized * 3f + Vector3.up * 5f;
            player.Director.OnAnchor(anchor);
            SignalEffects.Burst(anchor.transform.position, SignalEffects.Cyan, 14, 3);
            player.Director.Audio.Play("rope_attach", player.Shoulder, .3f, 2);
        }

        public void Constrain()
        {
            if (!Attached)
                return;
            Vector3 radial = player.Shoulder - Target.transform.position;
            float distance = radial.magnitude;
            if (distance > Length + .1f)
            {
                Vector3 outward = radial / distance;
                player.Controller.Move(-outward * Mathf.Min(distance - Length, .65f));
                float speed = Vector3.Dot(player.Velocity, outward);
                if (speed > 0)
                    player.Velocity -= outward * speed;
            }
        }

        public void Release()
        {
            if (Target && player && player.Director && player.Director.Audio)
                player.Director.Audio.Play("rope_release", player.Shoulder, .13f, 1);
            Target = null;
            if (line)
                line.enabled = false;
        }

        void LateUpdate()
        {
            if (!line)
                return;
            line.enabled = Attached;
            if (!Attached)
                return;
            for (int i = 0; i < 13; i++)
            {
                float t = i / 12f;
                var point = Vector3.Lerp(player.Shoulder, Target.transform.position, t);
                point.y -= Mathf.Sin(t * Mathf.PI) * .1f;
                line.SetPosition(i, point);
            }
        }

        void OnDisable()
        {
            Release();
            previousHeld = false;
        }
    }
}
