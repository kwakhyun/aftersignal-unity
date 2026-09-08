using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Every hero pose comes from the new illustrated identity.
    public static class SeoSpriteSet
    {
        static readonly Dictionary<string, Sprite[]> sheets = new Dictionary<string, Sprite[]>();
        public static Sprite Frame(string sheet, int frame)
        {
            if (!sheets.TryGetValue(sheet, out var sprites))
            {
                sprites = Resources.LoadAll<Sprite>("Art/SeoIllustrated/" + sheet);
                Array.Sort(sprites, (a,b) => string.CompareOrdinal(a.name,b.name));
                sheets[sheet] = sprites;
            }
            return sprites.Length == 0 ? null : sprites[Mathf.Clamp(frame, 0, sprites.Length-1)];
        }
        public static Sprite Pose(PlayerMotor p, float clock, out bool flip)
        {
            var right = p.Director.CameraRig.ViewRight;
            float side = Vector3.Dot(p.Velocity, right);
            float depth = Vector3.Dot(p.Velocity, Vector3.Cross(right,Vector3.up));
            bool back = depth > 0;
            bool vertical = Mathf.Abs(depth) > Mathf.Abs(side) * 1.15f;
            flip = p.Facing < 0;
            if (p.Health <= 0) return Frame("Context", 5);
            if (p.HurtTime > 0) return Frame("Context", vertical ? 4 : 5);
            if (p.Rope.Attached) return Frame("Rope", Mathf.Abs(p.Velocity.y) > 7 ? 4 : (int)(clock*6)%3+1);
            if (p.DashTime > 0 && p.AttackTime <= 0)
            {
                float d = Vector3.Dot(p.DashDirection,Vector3.Cross(right,Vector3.up));
                if (Mathf.Abs(d) > .6f) { flip = false; return Frame("Context", (d > 0 ? 2 : 0) + (p.DashTime < .1f ? 1 : 0)); }
                return Frame("Katana",7);
            }
            string weapon = p.Weapon.ToString();
            if (p.Reloading) return Frame("Pistol",p.ReloadProgress < .5f ? 3 : 4);
            if (p.AttackTime > 0)
            {
                float t = 1-p.AttackTime/Mathf.Max(.01f,p.AttackDuration);
                int f = p.Weapon == WeaponId.Pistol ? (t < .2f ? 0 : t < .55f ? 1 : 2) :
                    p.Action == WeaponAction.Skill ? 5 : p.Action == WeaponAction.Dash ? 7 : !p.Grounded ? 6 :
                    t < .25f ? 1 : t < .65f ? 2 + p.Combo%3 : 3;
                return Frame(weapon,f);
            }
            if (p.Guarding) return Frame(weapon,p.Weapon == WeaponId.Pistol ? 5 : 0);
            if (!p.Grounded) return Frame("Base",p.Velocity.y > 3 ? 4 : p.Velocity.y < -3 ? 6 : 5);
            if (p.LandingTime > 0) return Frame("Base",7);
            if (new Vector2(side,depth).magnitude > .3f)
            {
                string direction = vertical ? back ? "Back" : "Front" : side < 0 ? "Left" : "Right";
                flip = false;
                return Frame("Move"+direction,(p.Running ? 4 : 0)+(int)(clock*(p.Running?11:6))%4);
            }
            flip = false;
            return Frame("Base",p.LastMoveDirection == 0 ? 0 : p.LastMoveDirection == 2 ? 2 : p.Facing < 0 ? 3 : 1);
        }
    }
}
