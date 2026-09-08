using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class SeoSwimming
    {
        static readonly Dictionary<string,Sprite[]> sheets=new();
        public static bool Apply(PlayerMotor player,SpriteRenderer visual,float clock)
        {
            if(!OceanLife.Swimming)return false;
            var velocity=player.Velocity;var camera=Camera.main;if(!camera)return false;
            float side=Vector3.Dot(velocity,camera.transform.right),depth=Vector3.Dot(velocity,Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized);
            bool idle=Vector3.ProjectOnPlane(velocity,Vector3.up).magnitude<.6f;
            string key=idle?"Tread":Mathf.Abs(depth)>Mathf.Abs(side)*.65f?(depth>0?"SwimBack":"SwimFront"):"SwimRight";
            if(!sheets.TryGetValue(key,out var frames)){frames=Resources.LoadAll<Sprite>("Art/SeoSwimming/"+key);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));sheets[key]=frames;}
            if(frames.Length<8)return false;
            float rate=idle?6:Mathf.Lerp(7,11,Mathf.Clamp01(velocity.magnitude/7));visual.sprite=frames[(int)(clock*rate)%8];visual.flipX=side<-.1f;visual.color=Color.white;
            float tilt=idle?0:Mathf.Clamp(velocity.y*7,-28,28)*(visual.flipX?-1:1);
            visual.transform.rotation=Quaternion.Euler(camera.transform.eulerAngles.x*.55f,camera.transform.eulerAngles.y,tilt);
            visual.transform.position=player.transform.position+Vector3.up*(idle?.4f:.1f);visual.transform.localScale=Vector3.one;return true;
        }
    }
}
