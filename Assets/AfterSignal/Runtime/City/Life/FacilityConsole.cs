using UnityEngine;
namespace AfterSignal
{
    public enum FacilityFunction {Registry,Archive,Clinic,Office,Home,Power,Freight,Hotel,School,Police,Fire,Market,Cafe,Bank,Bar,Garage,Airport,Ferry,Military}
    public sealed class FacilityConsole:MonoBehaviour
    {public FacilityFunction function;public string location;public void Open()=>CityLife.Instance?.FacilityServices(function,location);}
}
