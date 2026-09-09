using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public static class VehiclePortraits
    {
        static readonly Dictionary<string,Sprite[]> sheets=new();
        static readonly Dictionary<string,Sprite> passengers=new();
        static readonly int[] seoDirections={0,8,1,9,2,10,3,11};
        public static Sprite[] Sheet(string key)
        {
            if(!sheets.TryGetValue(key,out var frames))
            {
                frames=Resources.LoadAll<Sprite>("Art/VehiclePortraits/"+key);
                Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));sheets[key]=frames;
            }
            return frames;
        }
        public static Sprite Seo(int direction,bool driver)
        {
            var frames=Sheet("CabinSeo");
            int n=seoDirections[Mathf.Clamp(direction,0,7)]+(driver?0:4);
            return n<frames.Length?frames[n]:null;
        }
        public static Sprite Driver(string role,int direction)
        {
            // The driver's face and clothing must come from the same identity as the person who exits.
            return Passenger(role,direction);
        }
        public static Sprite Passenger(string role,int direction)
        {
            string key=role+"/"+direction;
            if(passengers.TryGetValue(key,out var cached))return cached;
            var source=PeopleArt.Get(role,direction);
            if(!source)source=PeopleArt.Get("CivilianMan",direction);
            if(!source)return null;
            // Preserve the individual passenger's face and outfit; only the seated torso is visible.
            var rect=source.rect;
            // The importer's foot pivot and authored stature are available without CPU-readable pixels.
            float stature=role.StartsWith("Student")?1.45f:2.08f;
            float waist=Mathf.Clamp(source.pivot.y+source.pixelsPerUnit*stature*.49f,1,rect.height-1);
            rect.y+=waist;rect.height-=waist;
            var sprite=Sprite.Create(source.texture,rect,new Vector2(source.pivot.x/source.rect.width,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            sprite.name="Seated-"+key;passengers[key]=sprite;return sprite;
        }
    }
}
