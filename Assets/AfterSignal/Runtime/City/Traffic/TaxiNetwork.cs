using System.Collections;using UnityEngine;
namespace AfterSignal
{
    public sealed class TaxiNetwork:MonoBehaviour
    {
        IEnumerator Start()
        {
            while(!GameDirector.Instance||!GameDirector.Instance.Ready||!UrbanSimulation.Instance)yield return null;
            for(int i=0;i<56;i++)
            {
                bool air=i<24;int id=air?i:i-24;var c=UrbanSimulation.Instance.Spawn(air?CityTaxiService.AirStops[0]:CityTaxiService.WaterStops[0],false,(int)(air?CityVehicleType.CombatHelicopter:CityVehicleType.Boat));
                c.name=(air?"SKYLINE autonomous taxi ":"BLUEWAY water taxi ")+id;var service=c.gameObject.AddComponent<CityTaxiService>();service.Initialize(c,air,id);
                yield return null;
            }
        }
    }
}
