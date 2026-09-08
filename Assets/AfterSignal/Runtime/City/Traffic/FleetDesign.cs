using UnityEngine;
namespace AfterSignal
{
    public static class FleetDesign
    {
        public static readonly string[] Cars={"MetroSedan","CivicHatch","TrailSUV","EstateTourer"};
        public static readonly string[] Supercars={"VelaR","RosaM","ChimeraC"};
        public static readonly string[] Bikes={"ApexRR","NomadGS","ClassicTwin"};
        public static void Configure(CityVehicle c)
        {
            if(c.designVariant>=0)return;
            var p=c.transform.position;c.designVariant=Mathf.Abs(Mathf.RoundToInt(p.x*11+p.z*7));
        }
        public static string Model(CityVehicle c)
        {
            Configure(c);
            if(c.GetComponent<PoliceCar>())return Cars[0];
            if(c.type==CityVehicleType.Sedan||c.type==CityVehicleType.Taxi)return Cars[c.designVariant%Cars.Length];
            if(c.type==CityVehicleType.SportsCar)return Supercars[c.designVariant%Supercars.Length];
            if(c.type==CityVehicleType.Motorcycle)return Bikes[c.designVariant%Bikes.Length];
            return VehicleFleet.ModelName(c.type);
        }
        public static string Title(CityVehicle c)=>c.type==CityVehicleType.Sedan||c.type==CityVehicleType.Taxi||c.type==CityVehicleType.SportsCar||c.type==CityVehicleType.Motorcycle?Model(c):VehicleSeats.Title(c.type);
    }
}
