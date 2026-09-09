using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Occupant state belongs to the cabin; reactions remain independent of route spawning.
    public sealed class VehicleEmergency : MonoBehaviour
    {
        public enum Reaction { Calm, Escaping, Evacuated, Destroyed }
        public Reaction State { get; private set; }
        public int Ejected { get; private set; }
        CityVehicle car; float expires, blocked;
        public bool Escaping => State == Reaction.Escaping;
        public static void Hit(CityVehicle vehicle, Vector3 contact, bool fatal)
        {
            var response=vehicle.GetComponent<VehicleEmergency>();
            if(!response)response=vehicle.gameObject.AddComponent<VehicleEmergency>();
            response.car=vehicle;response.React(contact,fatal);
        }
        void React(Vector3 danger,bool fatal)
        {
            if(fatal){if(State==Reaction.Destroyed)return;Evacuate(true,danger);State=Reaction.Destroyed;return;}
            if(State==Reaction.Evacuated||State==Reaction.Destroyed)return;
            if(car.GetComponent<EmergencyAmbulance>())return;
            if(!car.occupied&&(!car.GetComponent<VehicleCabin>()||car.GetComponent<VehicleCabin>().PassengerCount==0)||car.GetComponent<PoliceCar>()||car.GetComponent<TacticalTransport>()||car.GetComponent<MilitaryVehicleAI>()||car.GetComponent<SeaCombat>()||car.GetComponent<GangConvoy>()||car.GetComponent<StolenVehicle>()||UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car)return;
            var intercity=car.GetComponent<IntercityService>();if(intercity&&!intercity.Boarding)return;
            CitySafety.Shock(car.transform.position);
            if(car.HealthFraction<.25f||Mathf.Abs(car.speed)<2.5f||!car.traffic||car.route==null||car.route.Length<2)
                Evacuate(false,danger);
            else
            {
                State=Reaction.Escaping;expires=Time.time+14;
                car.GetComponent<CityBusLine>()?.CancelStop();
            }
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked||!car||!Escaping)return;
            blocked=car.speed<1?blocked+Time.deltaTime:0;
            if(blocked>1.8f||car.HealthFraction<.25f)Evacuate(false,game.Player.transform.position);
            else if(Time.time>expires)State=Reaction.Calm;
        }
        void Evacuate(bool fallen,Vector3 danger)
        {
            car.GetComponent<GangConvoy>()?.PrepareEvacuation();
            var cabin=car.GetComponent<VehicleCabin>();
            var service=car.GetComponent<IntercityService>();if(service&&fallen){service.ReleaseAfterCrash();cabin?.SetPassengers(0);}
            var stolen=car.GetComponent<StolenVehicle>();
            bool realDriver=stolen&&stolen.Driver;
            if(realDriver&&fallen){stolen.Driver.GetComponent<GangCrime>()?.EjectFromWreck(car);Ejected++;}
            bool playerDriver=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car;
            foreach(var role in cabin?cabin.ReleaseOccupants(playerDriver||realDriver):new List<string>())
            {
                int i=Ejected++;float side=i%2==0?1:-1;
                var at=car.transform.position+car.transform.forward*side*(car.HalfWidth+.75f)+car.Forward*((i/2)%7-3)*.65f;
                if(Physics.Raycast(at+Vector3.up*4,Vector3.down,out var floor,12,1,QueryTriggerInteraction.Ignore))at.y=floor.point.y+.05f;else at.y=.05f;
                var person=new GameObject(fallen?"Thrown vehicle occupant":"Escaping vehicle occupant",typeof(SpriteRenderer));
                person.transform.position=at;
                var visual=person.GetComponent<SpriteRenderer>();visual.sprite=PeopleArt.Get(role,0);visual.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                var npc=person.AddComponent<CityNpc>();npc.Configure(Mathf.Abs(car.GetInstanceID())+i,"승객",null,"차량 공격에서 탈출한 애프터라이트 시민.");
                PeopleArt.Attach(person,role);
                var body=person.GetComponent<WorldActor>();body.police=role.Contains("Police")||role=="Swat";body.military=role=="Soldier";body.gang=role.StartsWith("Gang");npc.occupation=NpcPersona.Job(role);
                if(!fallen)person.GetComponent<WorldActor>().health=Mathf.Max(20,70-(cabin?cabin.OccupantInjury:0));
                person.AddComponent<VehicleSurvivor>().Initialize(fallen,danger,car.transform.position);
                Destroy(person,fallen?90:60);
            }
            car.GetComponent<CityBusLine>()?.CancelStop();
            if(CityBusService.Instance&&CityBusService.Instance.Riding==car)
            {
                CityBusService.Instance.EmergencyLeave(car);
                if(fallen){GameDirector.Instance.Player.ProtectVehicleImpact();GameDirector.Instance.Player.ReceiveDamage(30,car.transform.position,true);}
            }
            if(!playerDriver){car.occupied=false;car.traffic=false;car.speed=0;}
            State=fallen?Reaction.Destroyed:Reaction.Evacuated;
        }
    }
    public sealed class VehicleSurvivor : MonoBehaviour
    {
        bool fallen;Vector3 velocity;float floorHeight;
        public void Initialize(bool down,Vector3 danger,Vector3 origin)
        {
            fallen=down;floorHeight=transform.position.y;
            var npc=GetComponent<CityNpc>();
            if(down)
            {
                GetComponent<WorldActor>().health=0;
                npc.enabled=false;npc.point.enabled=false;
                GetComponent<Collider>().enabled=false;
                var look=GetComponent<DirectionalPerson>();if(look)look.enabled=false;
                velocity=Vector3.ProjectOnPlane(transform.position-origin,Vector3.up).normalized*3+Vector3.up*3;
            }
            else {npc.Panic(danger,18);NpcSpeech.Say(npc,NpcDialogueBank.Line(npc,"evacuate"),4,8);}
        }
        void Update()
        {
            if(!fallen||GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            velocity.y-=18*Time.deltaTime;
            var next=transform.position+velocity*Time.deltaTime;
            if(Physics.Raycast(next+Vector3.up*2,Vector3.down,out var floor,4,1,QueryTriggerInteraction.Ignore))floorHeight=floor.point.y+.06f;
            if(next.y<floorHeight){next.y=floorHeight;velocity=Vector3.zero;}
            transform.position=next;
            if(Camera.main)transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,86);
        }
    }
}
