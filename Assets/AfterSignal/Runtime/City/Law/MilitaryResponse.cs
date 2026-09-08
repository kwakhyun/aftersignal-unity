using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryResponse:MonoBehaviour
    {
        readonly List<CityVehicle> vehicles=new();readonly List<PoliceOfficer> soldiers=new();
        public int VehicleCount=>vehicles.Count;
        public int SoldierCount=>soldiers.Count;
        bool withdrawn,fullyDeployed;
        IEnumerator Start()
        {
            GameDirector.Instance.Toast("대규모 민간인 피해 · 군 긴급대응부대 출동",6);
            var kinds=new[]{CityVehicleType.Truck,CityVehicleType.Truck,CityVehicleType.Tank,CityVehicleType.CombatHelicopter,CityVehicleType.Fighter};
            for(int i=0;i<kinds.Length;i++)
            {
                if(withdrawn)yield break;
                var g=GameDirector.Instance;var target=WantedSystem.Instance.LastSeen;
                var at=target+new Vector3(-85-i*16,0,40+i*12);
                if(kinds[i]==CityVehicleType.CombatHelicopter)at.y=Mathf.Max(65,target.y+55);
                else if(kinds[i]==CityVehicleType.Fighter){at+=Vector3.left*160;at.y=Mathf.Max(150,target.y+130);}
                else if(CityGangWar.FindGround(at,out var safe))at=safe;else at=ExpansionRoads.Sidewalk(at);
                var car=UrbanSimulation.Instance.Spawn(at,false,(int)kinds[i]);car.name="군 긴급대응 / "+VehicleSeats.Title(kinds[i]);car.occupied=true;car.InitializeDurability();car.health=car.MaxHealth;
                var unit=car.gameObject.AddComponent<MilitaryVehicleAI>();unit.Initialize(car,this);vehicles.Add(car);
                yield return new WaitForSeconds(2.5f);
            }
            fullyDeployed=true;
        }
        public void Deploy(Vector3 at,int index)
        {
            if(!CityGangWar.FindGround(at,out var safe))return;
            var soldier=PoliceOfficer.Create(WantedSystem.Instance,safe,4,index);soldier.name="군 긴급대응 소총수";soldier.Body.military=true;PeopleArt.Attach(soldier.gameObject,"Soldier");soldiers.Add(soldier);WantedSystem.Instance.Officers.Add(soldier);
        }
        public void Withdraw()
        {
            withdrawn=true;StopAllCoroutines();foreach(var s in soldiers)if(s)s.Withdraw();
            foreach(var v in vehicles)if(v){var ai=v.GetComponent<MilitaryVehicleAI>();if(ai)Destroy(ai);if(!v.owned)Destroy(v.gameObject,8);}
            Destroy(this);
        }
        void Update(){if(!withdrawn&&WantedSystem.Level==0)Withdraw();else if(fullyDeployed&&!vehicles.Exists(v=>v&&!v.Wrecked)&&!soldiers.Exists(s=>s&&s.Body.Alive))Destroy(this);}
    }
}
