using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public enum SeoMotionPose { Rise, Fall, Land, Hurt, Dash, Rope, ClimbLeft, ClimbRight }
    public static class SeoRefined
    {
        static readonly Dictionary<string,Sprite[]> cache=new();
        static readonly int[] canonical={0,1,2,3,4,3,2,1};
        public static bool Flip(int direction)=>direction>=5;
        public static Sprite[] Sheet(string name)
        {
            if(cache.TryGetValue(name,out var frames))return frames;
            frames=Resources.LoadAll<Sprite>("Art/SeoRefined/"+name);
            Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
            cache[name]=frames;
            if(frames.Length==0||!name.StartsWith("Action"))return frames;
            var run=Sheet("Sprint"+name.Substring(6));
            if(run.Length==0)return frames;
            // Match the shared illustration canvas scale, never normalize a crouch or raised arm by its bounds.
            float ppu=run[0].pixelsPerUnit*frames[0].texture.width/run[0].texture.width;
            for(int i=0;i<frames.Length;i++)
            {
                var source=frames[i];
                var sprite=Sprite.Create(source.texture,source.rect,new Vector2(source.pivot.x/source.rect.width,source.pivot.y/source.rect.height),ppu,0,SpriteMeshType.FullRect);
                sprite.name=source.name;frames[i]=sprite;
            }
            return frames;
        }
        public static Sprite Run(int direction,int phase)
        {
            var s=Sheet("Sprint"+SeoKinetic.Directions[canonical[direction]]);
            return s.Length==8?s[phase%8]:SeoKinetic.Frame("Run"+SeoKinetic.Directions[direction],phase);
        }
        public static Sprite Pose(int direction,SeoMotionPose pose,out bool flip)
        {
            int d=canonical[direction];flip=Flip(direction);
            // Two painted climbing cells face left; correct their view while alternating the reach.
            if(pose==SeoMotionPose.ClimbRight&&(d==1||d==2))flip=!flip;
            var s=Sheet("Action"+SeoKinetic.Directions[d]);
            return s.Length==8?s[(int)pose]:SeoKinetic.Frame("Traversal",direction);
        }
    }
}
