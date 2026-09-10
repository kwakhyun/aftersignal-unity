using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class GarrisonArchitecture
    {
        struct Room{public Vector3 Center,Exit;public Vector2 Half;}
        static readonly List<Room> rooms=new();
        public static bool Outdoors(Vector3 at)=>!Physics.Raycast(at+Vector3.up*2,Vector3.up,16,1,QueryTriggerInteraction.Ignore);
        public static Vector3 ExitFor(Vector3 p,Vector3 fallback)
        {
            foreach(var r in rooms)if(Mathf.Abs(p.x-r.Center.x)<r.Half.x&&Mathf.Abs(p.z-r.Center.z)<r.Half.y)return r.Exit;
            if(p.x>548&&p.x<602&&p.z>750&&p.z<786)return new(575,.1f,748);
            if(p.x>550&&p.x<602&&p.z>839&&p.z<878)return new(575,.1f,836);
            if(p.x>365&&p.x<425&&p.z>824&&p.z<866)return new(395,.1f,820);
            return Outdoors(p)?p:fallback;
        }
        public static IEnumerator BuildAll(Transform parent)
        {
            rooms.Clear();var main=MilitaryBaseOperations.Bases[0];
            var center=new Vector3(580,0,915);
            yield return RoomBuilding(parent,main,center+new Vector3(-17,0,-11),"작전 통신실",0);
            yield return RoomBuilding(parent,main,center+new Vector3(17,0,-11),"차량 정비 지원실",1);
            yield return RoomBuilding(parent,main,center+new Vector3(-17,0,13),"병영 생활관",2);
            yield return RoomBuilding(parent,main,center+new Vector3(17,0,13),"급양 · 응급 의무실",3);
            var supply=new GameObject("Garrison logistics and motor pool").transform;supply.SetParent(parent,false);supply.position=new(481,0,775);var b=new CityGeometry(supply);
            for(int i=0;i<10;i++)
            {
                var at=supply.position+new Vector3(i%5*12,.1f,i/5*21);var truck=UrbanSimulation.Instance.Spawn(at,false,(int)CityVehicleType.Truck);truck.name="KESTREL / 주둔 기관총 장갑차 "+(i+1);truck.transform.rotation=Quaternion.Euler(0,90,0);MilitaryGunTruck.Install(truck,i);main.Vehicles.Add(truck);
                b.Box("Parking bay end",new(i%5*12,.075f,i/5*21-5.5f),new(6,.025f,.14f),"DistrictIvory");
                for(int side=-1;side<=1;side+=2)b.Box("Vehicle service bay",new(i%5*12+side*3,.075f,i/5*21),new(.12f,.025f,11),"DistrictIvory");yield return null;
            }
            for(int i=0;i<5;i++)
            {
                var p=new Vector3(i*8,0,43);b.Box("Pallet platform",p+Vector3.up*.12f,new(3,.24f,2.5f),"WarmWood",true);
                for(int j=0;j<3;j++){var at=p+new Vector3(j%2*1.25f-.65f,.4f+j/2*.9f,0);b.Box("Rugged field equipment crate",at+Vector3.up*.4f,new(1.15f,.8f,1.9f),"DefenseDeck",true);for(int s=-1;s<=1;s+=2)b.Box("Crate restraint strap",at+new Vector3(s*.42f,.82f,0),new(.09f,.025f,1.95f),"Metal");}
            }
            b.Sign("MOTOR POOL / 정비 · 연료 · 탄약",new(14,3.2f,43),.25f);
            for(int i=0;i<3;i++){b.Cylinder(new(-8+i*4,0,43),1.45f,3.4f,"Metal",20);b.Box("Fuel pump",new(-8+i*4,1,39),new(.8f,2,.65f),"DefenseDeck",true);b.Beam(new(-8+i*4,1.6f,38.6f),new(-7+i*4,.2f,37.8f),.09f,"DarkMetal");}
            var service=new GameObject("기지 차량 정비·재보급",typeof(InteractionPoint),typeof(GarrisonService));service.transform.SetParent(supply,false);service.transform.localPosition=new(12,.2f,38);service.GetComponent<InteractionPoint>().radius=6;service.GetComponent<InteractionPoint>().title="차량 정비 · 연료/탄약 보급";
            b.Finish();
            // Each parked weapon has a real nearby crew member before any alarm starts.
            foreach(var site in MilitaryBaseOperations.Bases)
            {
                foreach(var vehicle in UrbanSimulation.Instance.Cars.ToArray())
                {
                    if(!vehicle||!site.Contains(vehicle.transform.position)||vehicle.occupied||!(vehicle.GetComponent<MilitaryGunTruck>()||vehicle.type==CityVehicleType.Tank||vehicle.type==CityVehicleType.CombatHelicopter||vehicle.type==CityVehicleType.Fighter||vehicle.type==CityVehicleType.Bomber))continue;
                    if(!site.Vehicles.Contains(vehicle))site.Vehicles.Add(vehicle);if(!vehicle.GetComponent<RegionalParked>())vehicle.gameObject.AddComponent<RegionalParked>();
                    var desired=VehicleSeats.Door(vehicle)+Vector3.back*2;var at=desired;if(CityGangWar.FindGround(desired,out var grounded))at=grounded;
                    var unit=ArmyResponder.Create(at,site.Personnel.Count,null);unit.name=site.Name+" 장비 대기조";unit.transform.SetParent(parent,true);MilitaryBaseOperations.Enlist(unit.Body,site,at);yield return null;
                }
                if(site!=main){var detail=new GameObject(site.Name+" / field logistics").transform;detail.SetParent(parent,false);detail.position=site.Gate+new Vector3(-12,0,5);var kit=new CityGeometry(detail);for(int i=0;i<4;i++){kit.Box("Field generator",new(i*2.3f,1,0),new(1.8f,2,1.4f),"DefenseDeck",true);kit.Box("Vent grille",new(i*2.3f,1,-.71f),new(1.3f,1,.04f),"DarkMetal");}kit.Finish();}
            }
        }
        static IEnumerator RoomBuilding(Transform parent,MilitaryBaseOperations.Base site,Vector3 at,string title,int kind)
        {
            var root=new GameObject("기지 내부시설 / "+title).transform;root.SetParent(parent,false);root.position=at;var b=new CityGeometry(root);
            var exit=at+new Vector3(0,.1f,-11);rooms.Add(new Room{Center=at,Exit=exit,Half=new(14,10)});
            b.Box("Raised insulated floor",new(0,-.08f,0),new(28,.3f,20),"DefenseDeck",true);
            for(int side=-1;side<=1;side+=2){b.Box("Side insulation wall",new(side*14,2.4f,0),new(.35f,4.8f,20),"Metal",true);b.Box("Entry wall",new(side*9,2.4f,-10),new(10,4.8f,.3f),"DefenseDeck",true);b.Box("Front window",new(side*9,2.5f,-10.18f),new(7.5f,1.45f,.04f),"Glass");}
            b.Box("Rear wall",new(0,2.4f,10),new(28,4.8f,.35f),"Metal",true);b.Box("Weather roof",new(0,4.9f,0),new(29,.24f,21),"Metal",true);
            b.Box("Entry canopy",new(0,3.3f,-11),new(8,.17f,3),"DarkMetal");b.Sign(title,new(0,4,-10.3f),.26f);
            for(int i=0;i<7;i++){b.Box("Roof rib",new(-12+i*4,5.08f,0),new(.16f,.1f,21),"Chrome");b.Box("Ventilation duct",new(-12+i*4,4.5f,7),new(.6f,.5f,3),"DarkMetal");}
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<4;i++){var p=new Vector3(side*10,0,-6+i*4);
                    if(kind==2){for(int level=0;level<2;level++){b.Box("Bunk bed frame",p+Vector3.up*(.5f+level*1.65f),new(2.2f,.14f,3),"Metal",true);b.Box("Bunk mattress",p+Vector3.up*(.68f+level*1.65f),new(2,.18f,2.85f),"SeatBlue");b.Box("Pillow",p+new Vector3(0,.84f+level*1.65f,1),new(1.5f,.15f,.65f),"DistrictIvory");}for(int k=-1;k<=1;k+=2)b.Box("Bunk upright",p+new Vector3(k,1.5f,1.4f),new(.08f,3,.08f),"Metal");}
                    else if(kind==0){b.Box("Communications console",p+Vector3.up,new(3,.16f,2.2f),"DarkMetal",true);b.Box("Data display",p+new Vector3(side*.8f,1.8f,0),new(.12f,1.25f,1.8f),"DistrictBlue");b.Box("Operator chair",p+new Vector3(-side*1.8f,.65f,0),new(.85f,1.3f,.8f),"SeatBlue",true);}
                    else if(kind==1){b.Box("Repair workstation",p+Vector3.up,new(3,.18f,2.4f),"Metal",true);b.Box("Tool drawer",p+Vector3.up*.45f,new(2.6f,.9f,2),"DefenseDeck",true);for(int j=0;j<5;j++)b.Box("Drawer handle",p+new Vector3(-side*1.33f,.2f+j*.16f,0),new(.06f,.05f,.7f),"Chrome");b.Cylinder(p+Vector3.up*1.12f,.65f,.35f,"DarkMetal",14);}
                    else{b.Box(side<0?"Medical cot":"Mess table",p+Vector3.up*.8f,new(2.7f,.16f,2),side<0?"DistrictIvory":"Metal",true);if(side<0){b.Box("Medical pillow",p+new Vector3(0,1, .65f),new(1.5f,.18f,.6f),"DistrictIvory");b.Beam(p+new Vector3(-1.2f,0,.8f),p+new Vector3(-1.2f,2.5f,.8f),.06f,"Chrome");}else for(int j=0;j<3;j++)b.Cylinder(p+new Vector3(-.8f+j*.8f,.92f,0),.22f,.04f,"Chrome",12);}
                }
            }
            for(int i=0;i<5;i++){b.Box("Individual equipment locker",new(-8+i*4,1.6f,8.6f),new(2.1f,3.2f,1),"DefenseDeck",true);b.Box("Locker handle",new(-7.3f+i*4,1.5f,8.07f),new(.06f,.4f,.07f),"Chrome");}
            b.Box("Ceiling light",new(0,4.65f,0),new(14,.06f,.35f),"DistrictIvory");
            if(kind==0){b.Box("Operations table",new(0,1.1f,3.4f),new(6,.2f,3.3f),"Metal",true);b.Box("Tactical map surface",new(0,1.22f,3.4f),new(5.5f,.035f,2.9f),"DistrictBlue");for(int i=0;i<12;i++)b.Box("Map sector",new(-2.2f+i%4*1.4f,1.25f,2.4f+i/4),new(.7f,.07f,.45f),i%3==0?"NeonCyan":"Metal");}
            b.Finish();Physics.SyncTransforms();
            for(int i=0;i<6;i++){var pos=at+new Vector3(i%2==0?-4.5f:4.5f,.15f,-6+i/2*4);var unit=ArmyResponder.Create(pos,i,null);unit.name=title+" 근무 군인";unit.transform.SetParent(root,true);MilitaryBaseOperations.Enlist(unit.Body,site,exit);yield return null;}
        }
    }
    public sealed class GarrisonService:MonoBehaviour
    {
        public void Use()
        {
            var g=GameDirector.Instance;CityVehicle best=null;float distance=28*28;
            foreach(var car in UrbanSimulation.Instance.Cars){if(!car||car.Wrecked)continue;float d=(car.transform.position-transform.position).sqrMagnitude;if(d<distance){best=car;distance=d;}}
            if(!best){g.Toast("정비 구역 가까이에 차량을 세워 주세요.",3);return;}
            best.Repair();best.fuel=CityVehicle.Capacity;best.GetComponent<VehicleArmament>()?.Resupply();best.GetComponent<MilitaryGunTruck>()?.Resupply();g.Toast("차량 정비 · 연료 및 탄약 보급 완료",3);
        }
    }
}
