using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public static class VehiclePortraits
    {
        static readonly Dictionary<string,Sprite[]> sheets=new();
        static readonly Dictionary<string,Sprite> passengers=new();
        static readonly string[][] roles={
            new[]{"CivilianMan","CivilianWoman","OfficeMan","OfficeWoman"},
            new[]{"Worker","ElderMan","ElderWoman","TeacherMan"},
            new[]{"TeacherWoman","Doctor","Nurse","Bartender"},
            new[]{"PatientMan","PatientWoman","Police","Swat"}};
        static readonly string[] atlases={"CabinCitizens","CabinCommunity","CabinServices","CabinSpecialists"};
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
            if(role=="Worker"||role=="Soldier"||role=="Prisoner"||role.StartsWith("Facility"))return Passenger(role,direction);
            for(int group=0;group<roles.Length;group++)
            {
                int row=Array.IndexOf(roles[group],role);
                if(row<0)continue;
                var frames=Sheet(atlases[group]);int n=row*4+Mathf.Clamp(direction,0,3);
                if(n<frames.Length)return frames[n];
            }
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
