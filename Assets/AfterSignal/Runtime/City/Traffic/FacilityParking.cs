using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public enum ServiceParkingKind { None, Police, Fire, Medical }

    // A small local fleet, shared by authored parking and emergency dispatch.
    public sealed class FacilityParking : MonoBehaviour
    {
        sealed class Station
        {
            public Vector3 door; public ServiceParkingKind kind;
            public readonly CityVehicle[] cars = new CityVehicle[2];
            public readonly float[] refill = new float[2];
        }
        readonly List<Station> stations = new();
        float next; bool built;
        public static ServiceParkingKind SiteKind(int site) => UrbanCatalog.Kind(site) switch
        { 1 => ServiceParkingKind.Police, 2 => ServiceParkingKind.Fire, 4 => ServiceParkingKind.Medical, _ => ServiceParkingKind.None };
        public static ServiceParkingKind VenueType(VenueKind kind) => kind switch
        { VenueKind.Police => ServiceParkingKind.Police, VenueKind.FireStation => ServiceParkingKind.Fire, VenueKind.Hospital => ServiceParkingKind.Medical, _ => ServiceParkingKind.None };
        static bool Forecourt(Vector3 p, Vector3 door) => Mathf.Abs(p.y-door.y)<5 && Mathf.Abs(p.x-door.x)<25 && p.z>door.z-32 && p.z<door.z+10;
        public static ServiceParkingKind Reserved(Vector3 at)
        {
            for(int i=0;i<UrbanCatalog.SiteCount;i++) { var kind=SiteKind(i); if(kind!=ServiceParkingKind.None&&Forecourt(at,UrbanCatalog.Door(i)))return kind; }
            foreach(var v in FourCityCatalog.Venues) { var kind=VenueType(v.kind); if(kind!=ServiceParkingKind.None&&Forecourt(at,v.Entrance))return kind; }
            return ServiceParkingKind.None;
        }
        public static bool Matches(CityVehicle car, ServiceParkingKind kind) => kind switch
        { ServiceParkingKind.Fire => car.GetComponent<FireEngineArt>(), ServiceParkingKind.Medical => car.GetComponent<AmbulanceArt>(), ServiceParkingKind.Police => car.GetComponent<PoliceCar>(), _ => true };
        public static bool ClearBay(Vector3 desired, out Vector3 at)
        {
            at=desired;
            if(!NpcGroundSupport.Floor(desired,desired.y+3,6,out float floor))return false;
            at.y=floor+.08f;
            return !Physics.CheckBox(at+Vector3.up*1.7f,new Vector3(5.5f,1.35f,1.9f),Quaternion.identity,1,QueryTriggerInteraction.Ignore);
        }
        public static CityVehicle Create(ServiceParkingKind kind, Vector3 at)
        {
            CityVehicle car;
            if(kind==ServiceParkingKind.Police)
            {
                var police=PoliceCar.Create(WantedSystem.Instance,at);police.enabled=false;car=police.Vehicle;
                foreach(var audio in car.GetComponentsInChildren<AudioSource>())audio.Stop();
                car.health=car.MaxHealth;
            }
            else
            {
                car=UrbanSimulation.Instance.Spawn(at,false,(int)CityVehicleType.Truck);
                if(kind==ServiceParkingKind.Fire)car.gameObject.AddComponent<FireEngineArt>();else car.gameObject.AddComponent<AmbulanceArt>();
            }
            car.name=kind==ServiceParkingKind.Fire?"119 / 소방서 대기 소방차":kind==ServiceParkingKind.Medical?"119 / 병원 대기 구급차":"POLICE / 경찰서 대기 차량";
            car.occupied=car.traffic=false;car.gameObject.AddComponent<FacilityParked>().kind=kind;
            return car;
        }
        public static CityVehicle TakeFireEngine(Vector3 station)
        {
            foreach(var car in UrbanSimulation.Instance.Cars)
            {
                if(!car||car.Wrecked||car.owned||car.occupied||car.GetComponent<FireEngine>()||(car.transform.position-station).sqrMagnitude>65*65)continue;
                var slot=car.GetComponent<FacilityParked>();if(!slot||slot.kind!=ServiceParkingKind.Fire)continue;
                Destroy(slot);return car;
            }
            return null;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||!UrbanSimulation.Instance||Time.time<next)return;next=Time.time+2;
            if(!built)
            {
                if(!FourCityWorld.Instance||!FourCityWorld.Instance.Built)return;
                for(int i=0;i<UrbanCatalog.SiteCount;i++)if(SiteKind(i)!=ServiceParkingKind.None)stations.Add(new Station{door=UrbanCatalog.Door(i),kind=SiteKind(i)});
                foreach(var v in FourCityCatalog.Venues)if(VenueType(v.kind)!=ServiceParkingKind.None&&v.city!=2)stations.Add(new Station{door=v.Entrance,kind=VenueType(v.kind)});
                built=true;
            }
            foreach(var station in stations)
            {
                float distance=(g.Player.transform.position-station.door).sqrMagnitude;
                for(int i=0;i<2;i++)
                {
                    var car=station.cars[i];
                    if(car&&(car.owned||car.occupied||(car.transform.position-station.door).sqrMagnitude>70*70)) { station.cars[i]=null;station.refill[i]=Time.time+90;continue; }
                    if(distance>400*400) { if(car)Destroy(car.gameObject);station.cars[i]=null;continue; }
                    if(car||distance>280*280||Time.time<station.refill[i])continue;
                    for(int row=0;row<3;row++)if(ClearBay(station.door+new Vector3(i==0?-14:14,0,-8-row*7),out var at)){station.cars[i]=Create(station.kind,at);break;}
                }
            }
            // Remove only unattended ambient cars; never delete a player's or active response vehicle.
            foreach(var car in UrbanSimulation.Instance.Cars)
            {
                if(!car||car.owned||car.occupied||car.traffic||car.IsSpecial||car.GetComponent<FacilityParked>()||car.GetComponent<FireEngine>()||car.GetComponent<EmergencyAmbulance>()||car.GetComponent<BurningObject>()||car.GetComponent<VehicleFailure>())continue;
                var kind=Reserved(car.transform.position);if(kind!=ServiceParkingKind.None&&!Matches(car,kind))Destroy(car.gameObject);
            }
        }
    }
    public sealed class FacilityParked : MonoBehaviour
    {
        public ServiceParkingKind kind;
        void LateUpdate()
        {
            var car=GetComponent<CityVehicle>();if(!car||car.owned||car.occupied){Destroy(this);return;}
            var lights=GetComponent<ResponseLightbar>();if(lights)lights.enabled=false;
        }
    }
}
