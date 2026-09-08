using System;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    /// <summary>Infill parcels share the existing city land and preserve stable facility IDs.</summary>
    public static class CompactCityLayout
    {
        [Serializable] public sealed class Parcel {public string id;public float x,z,w,d;public int city;}
        [Serializable] sealed class Plan {public Parcel[] parcels;}
        public static readonly Dictionary<string,Vector3> Previous=new();
        public static readonly List<Rect> Reserved=new();
        public static void Apply(List<CityVenue> venues)
        {
            var asset=Resources.Load<TextAsset>("WorldAssets/CompactCityLayout");
            if(!asset)throw new InvalidOperationException("Compact city parcel plan is missing");
            foreach(var p in JsonUtility.FromJson<Plan>(asset.text).parcels)
            {
                var v=venues.Find(x=>x.id==p.id);if(v==null)throw new InvalidOperationException("Unknown compact parcel "+p.id);
                Previous[v.id]=v.position;v.position=new(p.x,v.position.y,p.z);v.size=new(p.w,p.d);
                if(v.city<2&&v.kind!=VenueKind.Island)Reserved.Add(Footprint(v,5));
            }
        }
        public static Rect Footprint(CityVenue v,float margin=0)=>new(v.position.x-v.size.x*.5f-margin,v.position.z-v.size.y*.5f-margin,v.size.x+margin*2,v.size.y+margin*2);
        public static bool Occupied(Vector3 p,float w=0,float d=0)
        {foreach(var r in Reserved)if(r.Overlaps(new Rect(p.x-w*.5f,p.z-d*.5f,Mathf.Max(.1f,w),Mathf.Max(.1f,d))))return true;return false;}
        public static Vector3 Migrate(Vector3 p)
        {
            // Only former outlying districts are remapped; unrelated old downtown coordinates remain valid.
            if(p.x<2300&&p.z>2350)return RegionalCatalog.HomeQuarter+new Vector3(0,.15f,-14);
            CityVenue nearest=null;float distance=float.MaxValue;
            foreach(var v in FourCityCatalog.Venues)if(Previous.TryGetValue(v.id,out var old))
            {float d=(p-old).sqrMagnitude;if(d<distance){nearest=v;distance=d;}}
            if(nearest!=null&&((p.x<2500&&(p.z>1100||p.z< -4180))||(p.x>4800&&p.z< -3800)))return nearest.Entrance+Vector3.back*3;
            return p;
        }
    }
}
