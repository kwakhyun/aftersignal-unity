using System.Collections;
using UnityEngine;

namespace AfterSignal
{
    public sealed class VehicleFleet : MonoBehaviour
    {
        public static string ModelName(CityVehicleType type) => (int)type < 4 ? "DetailedSedan" : type.ToString();
        public static bool Persistent(CityVehicle c) => c.IsSpecial || c.GetComponent<ParkingAssignment>() || c.type == CityVehicleType.Tank;
        public static void Configure(CityVehicle c, int variant)
        {
            if (variant < 4) return;
            c.type = (CityVehicleType)Mathf.Clamp(variant, 4, 10);
            foreach (var col in c.GetComponentsInChildren<Collider>()) Object.Destroy(col);
            var collider = c.gameObject.AddComponent<BoxCollider>();
            float height = c.type == CityVehicleType.Airliner ? 4.8f : c.type == CityVehicleType.CombatHelicopter ? 2.5f : c.type == CityVehicleType.Boat ? 3.5f : 1.45f;
            collider.center = Vector3.up * (height * .5f + .3f);
            collider.size = new Vector3(c.HalfLength * 2, height, c.HalfWidth * 2);
            if (c.type == CityVehicleType.Tank || c.type == CityVehicleType.Fighter || c.type == CityVehicleType.CombatHelicopter)
                c.gameObject.AddComponent<VehicleArmament>();
        }

        IEnumerator Start()
        {
            while (!GameDirector.Instance || !GameDirector.Instance.Ready || !UrbanSimulation.Instance) yield return null;
            yield return null;
            var sim = UrbanSimulation.Instance;
            Add(CityVehicleType.SportsCar, new Vector3(78,.05f,-254), 90);
            Add(CityVehicleType.Motorcycle, new Vector3(83,.05f,-254), 90);
            Add(CityVehicleType.Boat, new Vector3(1192.5f,-.9f,-700), 90);
            var ferry = Add(CityVehicleType.Boat, new Vector3(1272.5f,-.9f,-708), 90);
            var plane = Add(CityVehicleType.Airliner, new Vector3(1400,.15f,800), 0);
            Add(CityVehicleType.Airliner, new Vector3(1480,.15f,800), 180);
            Add(CityVehicleType.CombatHelicopter, new Vector3(435,.15f,842), 0);
            Add(CityVehicleType.Fighter, new Vector3(300,.15f,900), 0);
            Add(CityVehicleType.Tank, new Vector3(340,.15f,777), 0);
            foreach(var marker in FindObjectsByType<CraftSpawn>())Add(marker.type,marker.transform.position,marker.transform.eulerAngles.y);
            var cargoPrefab=Resources.Load<GameObject>("WorldAssets/ContainerShip");
            CityVehicle cargo=null;
            if(cargoPrefab){var vessel=Instantiate(cargoPrefab);cargo=vessel.GetComponent<CityVehicle>();cargo.transform.position=new Vector3(1730,0,-615);sim.Cars.Add(cargo);foreach(var t in vessel.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;}
            yield return null;
            if(cargo)cargo.gameObject.AddComponent<PassengerRoute>().Initialize(cargo,false);
            ferry.gameObject.AddComponent<PassengerRoute>().Initialize(ferry, false);
            plane.gameObject.AddComponent<PassengerRoute>().Initialize(plane, true);
        }
        CityVehicle Add(CityVehicleType kind, Vector3 at, float yaw)
        {
            var c = UrbanSimulation.Instance.Spawn(at, false, (int)kind);
            c.name = "Transport / " + VehicleSeats.Title(kind); c.transform.rotation = Quaternion.Euler(0,yaw,0);
            return c;
        }
    }

    public static class VehicleSeats
    {
        public static int Count(CityVehicle c) => c.type == CityVehicleType.Bus ? 15 : c.type == CityVehicleType.Airliner ? 26 : c.type == CityVehicleType.Boat ? 15 : c.type == CityVehicleType.Motorcycle || c.type == CityVehicleType.Fighter || c.type == CityVehicleType.Tank ? 1 : c.type == CityVehicleType.CombatHelicopter ? 4 : c.type == CityVehicleType.Truck ? 2 : 4;
        public static Vector3 Local(CityVehicle c, int seat)
        {
            var authored=c.GetComponent<AuthoredCraft>();if(authored)return authored.helm+new Vector3(seat==0?0:-8+seat*1.1f,0,seat==0?0:seat%2==0?-7:7);
            if (c.type == CityVehicleType.Airliner) return seat < 2 ? new Vector3(10,2.3f,seat==0?-.7f:.7f) : new Vector3(6-(seat-2)/4*2.4f,1.8f,((seat-2)%4-1.5f)*.8f);
            if (c.type == CityVehicleType.Boat) return seat == 0 ? new Vector3(3.5f,2.5f,-.7f) : new Vector3(1-(seat-1)/2*1.1f,1.55f,seat%2==0?-1.2f:1.2f);
            if (c.type == CityVehicleType.CombatHelicopter) return new Vector3(seat<2?2:-.6f,1.25f,seat%2==0?-.6f:.6f);
            if (c.type == CityVehicleType.Fighter) return new Vector3(3,1.45f,0);
            if (c.type == CityVehicleType.Tank) return new Vector3(.5f,1.25f,0);
            if (c.type == CityVehicleType.Motorcycle) return new Vector3(-.15f,.92f,0);
            if (c.type == CityVehicleType.Bus) return seat==0?new Vector3(3.5f,1.55f,-.55f):new Vector3(2.3f-((seat-1)/2)*.91f,1.55f,(seat-1)%2==0?-.72f:.72f);
            if (c.type == CityVehicleType.Truck) return new Vector3(2.8f,1.52f,seat==0?-.53f:.53f);
            return new Vector3(seat<2?.3f:-.68f,c.type==CityVehicleType.SportsCar?.66f:.78f,seat%2==0?-.43f:.43f);
        }
        public static Vector3 Door(CityVehicle c) => c.GetComponent<AuthoredCraft>()?c.GetComponent<AuthoredCraft>().BoardingPoint:c.transform.TransformPoint(c.type==CityVehicleType.Airliner?new Vector3(8,0,-3.4f):c.type==CityVehicleType.Boat?new Vector3(0,1.4f,3.3f):new Vector3(0,0,-c.HalfWidth-1));
        public static string Name(CityVehicle c,int seat) => seat==0 ? c.IsAircraft ? "조종석" : c.IsWatercraft ? "선장석" : "운전석" : c.IsSpecial || c.type==CityVehicleType.Bus ? "승객석 "+seat : seat==1?"조수석":seat==2?"뒷좌석 왼쪽":"뒷좌석 오른쪽";
        public static string Title(CityVehicleType t) => new[]{"세단","택시","시내버스","트럭","루멘 바이크","오로라 스포츠카","블루워터 여객선","루멘 에어 여객기","레이븐 전투헬기","스펙터 전투기","아이언 전차"}[(int)t];
        public static string Controls(CityVehicle c) => c.IsAircraft ? "W/S 추력 · A/D 선회 · SPACE 상승 / CTRL 하강 · SHIFT 가속 · F 하차" : c.IsWatercraft ? "W/S 추진 · A/D 키 · SHIFT 가속 · SPACE 제동 · F 하선" : "W/S 가속·후진 · A/D 조향 · SHIFT 가속 · SPACE 제동 · E 하차";
    }
}
