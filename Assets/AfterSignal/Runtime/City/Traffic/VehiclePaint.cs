using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class VehiclePaint
    {
        static readonly Dictionary<CityVehicleType,Material> paints=new();
        static Material headlamp,taillamp;
        public static Material Lens(bool rear)
        {
            if(rear&&taillamp)return taillamp;if(!rear&&headlamp)return headlamp;
            var m=new Material(Resources.Load<Material>("Materials/SedanIvory"));
            var color=rear?new Color(.75f,.008f,.004f):new Color(.75f,.87f,.94f);
            m.name=rear?"Red rear lens":"White front lens";m.SetColor("_BaseColor",color);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*(rear?.9f:.7f));
            if(rear)taillamp=m;else headlamp=m;return m;
        }
        public static Material For(CityVehicleType type,Material fallback)
        {
            if((int)type<4)return fallback;
            if(paints.TryGetValue(type,out var cached))return cached;
            Color c=type==CityVehicleType.Motorcycle?new Color(.88f,.42f,.09f):type==CityVehicleType.SportsCar?new Color(.08f,.44f,.5f):type==CityVehicleType.Tank||type==CityVehicleType.CombatHelicopter?new Color(.19f,.25f,.19f):type==CityVehicleType.Fighter?new Color(.23f,.29f,.34f):new Color(.82f,.87f,.86f);
            var m=new Material(fallback);m.name=type+" enamel";m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",type==CityVehicleType.SportsCar?.68f:.38f);m.SetFloat("_Smoothness",type==CityVehicleType.SportsCar?.76f:.4f);paints[type]=m;return m;
        }
    }
}
