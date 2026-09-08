using UnityEngine;
namespace AfterSignal
{
    public sealed partial class UrbanSimulation
    {
        public CityVehicle PersonalBike{get;private set;}
        public static bool BikeDeliveryPending;
        float bikeCooldown;
        public static void RequestBike()
        {
            var g=GameDirector.Instance;if(!g||g.Dead||g.Transition)return;
            if(Instance&&g.stage==StageId.UrbanCity){Instance.SummonBike();return;}
            BikeDeliveryPending=true;g.Toast("바이크 호출 접수 · 건물 밖으로 나가면 가까운 곳에 배달됩니다.",5);
        }
        public bool SummonBike()
        {
            if(!game||!game.Ready||Time.unscaledTime<bikeCooldown)return false;
            if(Current==PersonalBike&&PersonalBike){game.Toast("이미 서하의 바이크에 탑승 중입니다.");return true;}
            Vector3 from=game.Player.transform.position;var preferred=from+game.Player.transform.right*5;
            if(Mathf.Abs(from.y)>8&&!FourCityCatalog.Dry(from))preferred=LocalCityRoutes.Nearest(new Vector3(from.x,0,from.z),out _,out _);
            Vector3 safe=default;bool found=false;
            for(int n=0;n<160;n++)
            {
                var center=n<80?preferred:LocalCityRoutes.Sidewalk(from);float angle=n*2.399963f,radius=2+(n%80)/8f;
                var p=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,5,1,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f||hit.collider.GetComponentInParent<CityVehicle>())continue;
                p=hit.point+Vector3.up*.08f;
                if(Physics.CheckBox(p+Vector3.up*1,new Vector3(1.5f,.8f,.75f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                if((p-from).sqrMagnitude<12)continue;safe=p;found=true;break;
            }
            if(!found){BikeDeliveryPending=true;game.Toast("바이크가 접근 중입니다 · 가까운 평지에 도착하면 배달됩니다.",4);bikeCooldown=Time.unscaledTime+2;return false;}
            if(PersonalBike&&PersonalBike.occupied&&!PersonalBike.owned){game.Toast("바이크가 사용 중입니다.");return false;}
            if(PersonalBike){Cars.Remove(PersonalBike);if(Owned==PersonalBike)Owned=null;Destroy(PersonalBike.gameObject);}
            PersonalBike=Spawn(safe,false,(int)CityVehicleType.Motorcycle);PersonalBike.name="서하의 루멘 바이크";PersonalBike.owned=true;PersonalBike.gameObject.AddComponent<PersonalMotorcycle>();
            PersonalBike.transform.rotation=Quaternion.Euler(0,game.Player.transform.eulerAngles.y,0);PersonalBike.InitializeDurability();PersonalBike.health=PersonalBike.MaxHealth;PersonalBike.fuel=CityVehicle.Capacity;Owned=PersonalBike;
            BikeDeliveryPending=false;bikeCooldown=Time.unscaledTime+3;SaveCar();game.Toast("서하의 바이크 도착 · "+Mathf.RoundToInt(Vector3.Distance(from,safe))+" m · E 탑승",5);return true;
        }
    }
    public sealed class PersonalMotorcycle:MonoBehaviour{}
}
