using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AfterSignal
{
    public sealed class PixelActor : MonoBehaviour
    {
        static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
        public string art = "Hero";
        public float bodyScale = 1f;
        [SerializeField]
        SpriteRenderer visual;
        static readonly int[] heavyGround =
        {
            38,
            38,
            51,
            55
        }, heavyAir =
        {
            48,
            52,
            50,
            51
        };
        Sprite[] frames;
        float clock;
        SeoLocomotion locomotion;
        Sprite[] katanaFrames;
        readonly Sprite[][] actionFrames = new Sprite[3][];
        readonly Sprite[][] depthFrames = new Sprite[3][];
        int depthFacing;
        [System.Serializable]
        class GunPoints
        {
            public Vector2[] points;
        }

        GunPoints gunPoints;
        int actionIndex = -1;
        bool pistolPose;
        int lastFrame = -1;
        public Transform Visual => visual.transform;

        public void Initialize()
        {
            if (visual && frames != null)
                return;
            if (!cache.TryGetValue(art, out frames))
            {
                if (art.StartsWith("Enemies/"))
                {
                    string kind = art.Substring(8);
                    frames = System.Array.FindAll(Resources.LoadAll<Sprite>("Art/Enemies"), s => s.name.StartsWith(kind + "-"));
                }
                else
                {
                    frames = Resources.LoadAll<Sprite>("Art/" + art);
                    if (art == "Hero")
                        frames = System.Array.FindAll(frames, s => s.name.StartsWith("seo-"));
                }

                System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                cache[art] = frames;
            }

            if (!visual)
            {
                var child = new GameObject("Pixel silhouette");
                child.transform.SetParent(transform, false);
                visual = child.AddComponent<SpriteRenderer>();
            }

            visual.sharedMaterial = Resources.Load<Material>("Materials/PixelActor");
            visual.shadowCastingMode = ShadowCastingMode.Off;
            visual.receiveShadows = false;
            visual.transform.localScale = Vector3.one * bodyScale;
            if (Application.isPlaying && !GetComponent<ContactShadow>())
                gameObject.AddComponent<ContactShadow>();
            if (frames.Length > 0)
                visual.sprite = frames[0];
            if (art == "Hero")
            {
                katanaFrames = Resources.LoadAll<Sprite>("Art/Hero/Quality");
                System.Array.Sort(katanaFrames, (a, b) => string.CompareOrdinal(a.name, b.name));
            }

            if (art == "Hero")
            {
                for (int i = 0; i < 3; i++)
                {
                    actionFrames[i] = Resources.LoadAll<Sprite>("Art/Hero/Actions/" + ((WeaponId)i));
                    System.Array.Sort(actionFrames[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                    depthFrames[i] = Resources.LoadAll<Sprite>("Art/Hero/Depth/" + ((WeaponId)i));
                    System.Array.Sort(depthFrames[i], (a, b) => string.CompareOrdinal(a.name, b.name));
                }

                var data = Resources.Load<TextAsset>("Art/Hero/Actions/gun-points");
                if (data)
                    gunPoints = JsonUtility.FromJson<GunPoints>(data.text);
            }
        }

        public void Pose(int frame, float facing, float tint = 0, float lean = 0)
        {
            Initialize();
            pistolPose = false;
            actionIndex = -1;
            if (frames.Length == 0)
                return;
            frame = Mathf.Clamp(frame, 0, frames.Length - 1);
            if (frame != lastFrame)
            {
                visual.sprite = frames[frame];
                lastFrame = frame;
            }

            visual.flipX = facing < 0;
            visual.color = Color.Lerp(Color.white, new Color(1f, .3f, .3f), tint);
            var cam = Camera.main;
            if (cam)
                visual.transform.rotation = Quaternion.Euler(cam.transform.eulerAngles.x, cam.transform.eulerAngles.y, lean);
            if (cam && Application.isPlaying)
            {
                var screen = cam.WorldToScreenPoint(transform.position);
                screen.x = Mathf.Round(screen.x);
                screen.y = Mathf.Round(screen.y);
                visual.transform.position = cam.ScreenToWorldPoint(screen);
            }
        }

        public void TickHero(PlayerMotor p, float dt)
        {
            Initialize();
            bool illustratedFlip;
            var illustrated = SeoSpriteSet.Pose(p, clock, out illustratedFlip);
            if(!locomotion)locomotion=gameObject.AddComponent<SeoLocomotion>();
            var gait=locomotion.Tick(p,dt);
            if(gait)
            {illustrated=gait;illustratedFlip=locomotion.Flip;}
            if (illustrated)
            {
                clock += dt;
                Pose(0, illustratedFlip ? -1 : 1,p.HurtTime>0?.5f:0,locomotion.Lean);
                visual.sprite = illustrated; lastFrame = -1;
                // Illustrated sprites move smoothly in the 3D camera; pixel snapping caused visible judder.
                visual.transform.position = transform.position + locomotion.Offset;
                visual.transform.localScale = Vector3.one * bodyScale;
                return;
            }
            visual.transform.localScale = new Vector3(1 + (p.LandingTime > 0 ? .035f : 0), 1 - (p.LandingTime > 0 ? .035f : 0), 1) * bodyScale;
            float speed = new Vector2(p.Velocity.x, p.Velocity.z).magnitude;
            clock += dt * (speed > .5f ? Mathf.Lerp(.45f, 1, Mathf.Clamp01(speed / 7.6f)) : 1);
            var right = p.Director.CameraRig.ViewRight;
            float sideSpeed = Vector3.Dot(p.Velocity, right), depthSpeed = Vector3.Dot(p.Velocity, Vector3.Cross(right, Vector3.up));
            if (speed > .5f)
                depthFacing = Mathf.Abs(depthSpeed) > Mathf.Abs(sideSpeed) * 1.2f ? (depthSpeed > 0 ? 1 : -1) : 0;
            if (p.HurtTime <= 0 && !p.Reloading && p.AttackTime <= 0 && (p.DashTime <= 0 || Mathf.Abs(Vector3.Dot(p.DashDirection, Vector3.Cross(right, Vector3.up))) > .5f) && p.Grounded && !p.Guarding && depthFacing != 0 && depthFrames[(int)p.Weapon].Length == 8)
            {
                int index = (depthFacing > 0 ? 4 : 0) + (speed > .5f ? (int)(clock * 7) % 4 : 0);
                Pose(0, 1);
                visual.sprite = depthFrames[(int)p.Weapon][index];
                lastFrame = -1;
                return;
            }

            int frame;
            if (p.HurtTime > 0)
                frame = 13;
            else if (p.Reloading && ActionPose(p, 16 + Mathf.Min(3, (int)(p.ReloadProgress * 4))))
                return;
            else if (p.AttackTime > 0 && TryAction(p))
                return;
            else if (p.DashTime > 0 && ActionPose(p, 12))
                return;
            else if (p.DashTime > 0)
                frame = 12;
            else if (p.AttackTime > 0 && p.Weapon == WeaponId.Katana && p.Grounded && !p.SkillPose && katanaFrames != null && katanaFrames.Length == 6)
            {
                float t = p.AttackDuration - p.AttackTime;
                int index = t < .04f ? 0 : t < .09f ? 1 : t < .12f ? 2 : t < .195f ? 3 : t < .275f ? 4 : 5;
                Pose(0, p.Facing);
                visual.sprite = katanaFrames[index];
                lastFrame = -1;
                return;
            }
            else if (p.Rope.Attached)
                frame = 91 + (int)(clock * 4) % 2;
            else if (p.Guarding && p.Weapon == WeaponId.Pistol && ActionPose(p, 4))
                return;
            else if (p.Guarding)
                frame = p.Weapon == WeaponId.Greatsword ? 84 + (int)(clock * 4) % 2 : 80 + (int)(clock * 4) % 2;
            else if (p.AttackTime > 0)
            {
                float t = 1 - p.AttackTime / p.AttackDuration;
                if (p.Weapon == WeaponId.Pistol)
                    frame = (p.Grounded ? 24 : 46) + Mathf.Min(1, (int)(t * 2));
                else if (p.Weapon == WeaponId.Greatsword)
                    frame = (p.Grounded ? heavyGround : heavyAir)[Mathf.Min(3, (int)(t * 4))];
                else
                    frame = (p.Grounded ? (p.Combo == 0 ? 16 : 19) : 40) + Mathf.Min(2, (int)(t * 3));
            }
            else if (!p.Grounded && p.Weapon == WeaponId.Pistol && ActionPose(p, 14))
                return;
            else if (!p.Grounded)
                frame = p.Weapon == WeaponId.Greatsword ? 48 : p.Velocity.y > 0 ? 9 : 10;
            else if (new Vector2(p.Velocity.x, p.Velocity.z).magnitude > .5f)
            {
                if (p.Weapon == WeaponId.Pistol && ActionPose(p, 20 + (int)(clock * 7) % 4))
                    return;
                frame = p.Velocity.x * p.Facing < -.6f ? 34 + (int)(clock * 7) % 4 : (p.Weapon == WeaponId.Greatsword ? 68 : 56) + (int)(clock * 12) % 12;
            }
            else
            {
                if (p.Weapon == WeaponId.Pistol && ActionPose(p, 0))
                    return;
                frame = p.Weapon == WeaponId.Greatsword ? 38 : (int)(clock * 3) % 2;
            }

            Pose(frame, p.Facing, p.HurtTime > 0 ? .7f : 0, p.Rope.Attached ? Mathf.Clamp(-p.Velocity.x * 1.3f, -16, 16) : 0);
        }

        bool TryAction(PlayerMotor p)
        {
            // Preserve the authored airborne silhouettes instead of reusing planted combo feet in midair.
            if (!p.Grounded && p.Action == WeaponAction.Combo && p.Weapon != WeaponId.Pistol)
                return false;
            var timing = p.ActiveTiming;
            float t = p.AttackDuration - p.AttackTime;
            if (p.Action == WeaponAction.Dash)
                return ActionPose(p, t < timing.contact ? 12 : 13);
            if (p.Action == WeaponAction.Skill)
                return ActionPose(p, t < timing.contact ? 14 : 15);
            int pose = t < Mathf.Max(.015f, timing.contact - .035f) ? 0 : t < timing.activeEnd ? 1 : t < Mathf.Lerp(timing.activeEnd, timing.duration, .6f) ? 2 : 3;
            return ActionPose(p, p.Combo * 4 + pose);
        }

        bool ActionPose(PlayerMotor p, int index)
        {
            var set = actionFrames[(int)p.Weapon];
            if (set == null || index >= set.Length)
                return false;
            Pose(0, p.Facing);
            visual.sprite = set[index];
            lastFrame = -1;
            actionIndex = index;
            pistolPose = p.Weapon == WeaponId.Pistol;
            return true;
        }

        public bool TryMuzzle(float facing, out Vector3 point)
        {
            point = Vector3.zero;
            if(art=="Hero" && visual && visual.sprite && visual.sprite.name.StartsWith("Pistol-"))
            {
                var b=SeoSpriteSet.Frame("Pistol",0).bounds;
                point=visual.transform.TransformPoint(new Vector3(facing*b.max.x,b.min.y+b.size.y*.8f,0));return true;
            }
            if (!pistolPose || gunPoints == null || actionIndex < 0 || actionIndex >= gunPoints.points.Length)
                return false;
            var pixel = gunPoints.points[actionIndex];
            point = visual.transform.TransformPoint(new Vector3((pixel.x - 144) / 49f * facing, (272 - pixel.y) / 49f, 0));
            return true;
        }
    }
}
