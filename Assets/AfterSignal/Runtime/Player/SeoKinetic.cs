using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class SeoKinetic
    {
        public static readonly string[] Directions={"Front","FrontRight","Right","BackRight","Back","BackLeft","Left","FrontLeft"};
        static readonly Dictionary<string,Sprite[]> sheets=new Dictionary<string,Sprite[]>();
        public static Sprite[] Sheet(string name)
        {
            if(!sheets.TryGetValue(name,out var s)){s=Resources.LoadAll<Sprite>("Art/SeoKinetic/"+name);Array.Sort(s,(a,b)=>string.CompareOrdinal(a.name,b.name));sheets[name]=s;}return s;
        }
        public static Sprite Frame(string name,int n){var s=Sheet(name);return s.Length>0?s[Mathf.Clamp(n,0,s.Length-1)]:null;}
        public static int Direction(Vector3 heading,Vector3 right)
        {
            float angle=Mathf.Repeat(Mathf.Atan2(Vector3.Dot(heading,right),-Vector3.Dot(heading,Vector3.Cross(right,Vector3.up)))*Mathf.Rad2Deg,360);
            return Mathf.RoundToInt(angle/45)%8;
        }
        public static bool RunFlip(int direction)=>direction==6||direction==7;
        public static bool Mirror(PlayerMotor p,int direction)
        {
            if(p.AttackTime>0||p.Guarding||p.Reloading)return Direction(p.AttackHeading,p.Director.CameraRig.ViewRight)==7;
            if(p.WallClimbing&&p.Health>0&&p.HurtTime<=0)return Direction(-p.WallNormal,p.Director.CameraRig.ViewRight)==7;
            return direction==2&&(p.Health<=0||p.HurtTime>0||p.Grounded&&p.AttackTime<=0&&!p.Guarding&&!p.Reloading&&p.DashTime<=0&&!p.Rope.Attached);
        }
        public static Sprite Action(PlayerMotor p,int idleDirection)
        {
            int sector=Direction(p.AttackHeading,p.Director.CameraRig.ViewRight);
            if(p.Health<=0||p.HurtTime>0)return Frame("Idle",idleDirection);
            if(p.WallClimbing){int d=Direction(-p.WallNormal,p.Director.CameraRig.ViewRight);return Frame("Traversal",8+(d==7?1:d));}
            if(p.AttackTime>0)
            {
                float t=p.AttackDuration-p.AttackTime;
                var timing=p.ActiveTiming;
                int phase=t<timing.contact*.4f?0:t<timing.contact?1:t<timing.activeEnd?2:3;
                if(p.Weapon==WeaponId.Pistol)phase=t<timing.contact?0:t<timing.contact+.07f?1:t<timing.contact+.18f?2:3;
                return Frame("Combat"+Directions[sector],(int)p.Weapon*4+phase);
            }
            if(p.Guarding||p.Reloading)return Frame("Combat"+Directions[sector],(int)p.Weapon*4);
            if(p.Rope.Attached||!p.Grounded||p.DashTime>0)return Frame("Traversal",idleDirection);
            return Frame("Idle",idleDirection);
        }
    }
}