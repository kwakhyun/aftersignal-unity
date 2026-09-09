using UnityEngine;
namespace AfterSignal
{
    public static class VehicleOccupant
    {
        public static WorldActor Create(CityVehicle car,string identity,int seat,bool fallen,Vector3 danger)
        {
            Vector3 desired=car.transform.position+car.transform.forward*(car.HalfWidth+1.2f)*(seat%2==0?-1:1)+car.Forward*(seat/2*.8f);
            if(!CityGangWar.FindGround(desired,out var at))at=VehicleSeats.Door(car);
            if(!fallen&&(identity=="CoastGuard"||identity=="NavyCrew"||identity=="AirForceCrew"||identity=="SeaRaider"))
            {
                WorldActor unit;
                if(identity=="CoastGuard"){var officer=PoliceOfficer.Create(WantedSystem.Instance,at,2,seat);officer.Ambient=true;unit=officer.Body;}
                else if(identity=="SeaRaider"){var pirate=GangMember.Create(at,0,seat);unit=pirate.Body;}
                else unit=ArmyResponder.Create(at,seat,RiftIncursion.Instance).Body;
                PeopleArt.Attach(unit.gameObject,identity);unit.gameObject.AddComponent<RegionalUniform>().art=identity;var profile=unit.GetComponent<CityNpc>();if(profile)profile.occupation=identity=="CoastGuard"?"해양경찰":identity=="NavyCrew"?"해군 승조원":identity=="AirForceCrew"?"공군 조종사":"해적 조직원";return unit;
            }
            if(!fallen&&(identity.Contains("Police")||identity=="Swat")){var officer=PoliceOfficer.Create(WantedSystem.Instance,at,identity=="Swat"?4:2,seat);officer.Ambient=true;return officer.Body;}
            if(!fallen&&identity=="Soldier")return ArmyResponder.Create(at,seat,RiftIncursion.Instance).Body;
            if(!fallen&&identity.StartsWith("Gang")){var member=GangMember.Create(at,0,seat);member.gameObject.AddComponent<GangCrime>();return member.Body;}
            var go=new GameObject("Vehicle occupant / "+identity,typeof(SpriteRenderer));go.transform.position=at;
            var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get(identity,0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
            var npc=go.AddComponent<CityNpc>();npc.Configure(Mathf.Abs(car.GetInstanceID())+seat,NpcPersona.Job(identity),null,"차량 탑승 전후 외형이 같은 시민. 방금 차량에서 내렸다.");PeopleArt.Attach(go,identity);
            var body=go.GetComponent<WorldActor>();body.police=identity.Contains("Police")||identity=="Swat"||identity=="CoastGuard";body.military=identity=="Soldier"||identity=="NavyCrew"||identity=="AirForceCrew";body.gang=identity=="SeaRaider";
            go.AddComponent<VehicleSurvivor>().Initialize(fallen,danger,car.transform.position);Object.Destroy(go,fallen?90:120);return body;
        }
    }
}
