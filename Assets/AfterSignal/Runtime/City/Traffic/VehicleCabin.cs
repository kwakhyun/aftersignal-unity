using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal
{
    public sealed class VehicleCabin : MonoBehaviour
    {
        CityVehicle car;
        Material glass;
        readonly List<SpriteRenderer> occupants=new List<SpriteRenderer>();
        readonly List<Sprite> people=new List<Sprite>();
        public int PassengerCount {get;private set;}
        public float OccupantInjury {get;private set;}
        public void InjureOccupants(float speed){OccupantInjury=Mathf.Clamp(OccupantInjury+VehicleDurability.OccupantDamage(speed,false),0,50);}
        static readonly string[] passengerRoles={"CivilianMan","CivilianWoman","OfficeMan","OfficeWoman","Worker","ElderMan","ElderWoman","TeacherMan","TeacherWoman","Doctor","Nurse","Bartender","PatientMan","PatientWoman"};
        readonly List<string> identities=new List<string>();
        int boardingSerial;float visualClock;
        public string IdentityAt(int seat)=>car&&car.GetComponent<GangConvoy>()?"GangCrimson":seat>=0&&seat<identities.Count?identities[seat]:"CivilianMan";
        public WorldActor EjectDriver()
        {
            var convoy=car.GetComponent<GangConvoy>();if(convoy)return convoy.EjectDriver();
            var stolen=car.GetComponent<StolenVehicle>();
            if(stolen&&stolen.Driver){var driver=stolen.Driver;driver.GetComponent<GangCrime>()?.EjectFromWreck(car,false);return driver.Body;}
            return VehicleOccupant.Create(car,IdentityAt(0),0,false,GameDirector.Instance.Player.transform.position);
        }
        
        public void Initialize(CityVehicle owner)
        {
            if(car) return;
            car=owner;
            glass=new Material(Resources.Load<Shader>("Shaders/StructuralGlass"));
            glass.name="Transparent vehicle glazing";
            glass.SetColor("_BaseColor",new Color(.10f,.20f,.24f,.14f));glass.renderQueue=3000;
            foreach(var mesh in GetComponentsInChildren<MeshRenderer>())
            {
                string n=mesh.name.ToLowerInvariant();
                if((n.Contains("window")&&!n.Contains("pillar")&&!n.Contains("frame"))||n.Contains("windshield")||n=="sculpted cabin"||n=="navigation bridge") mesh.sharedMaterial=glass;
                if(n=="bus saloon") mesh.enabled=false;
            }
            bool bus=car.type==CityVehicleType.Bus;
            if(bus&&!Resources.Load<GameObject>("WorldAssets/FutureBus/FutureBus"))
            {
                Detail("Saloon floor",new Vector3(0,1,0),new Vector3(8.6f,.12f,2.25f),"WarmWood");
                for(int side=-1;side<=1;side+=2)
                {
                    Detail("Lower saloon side",new Vector3(0,1.35f,side*1.18f),new Vector3(8.8f,.65f,.12f),"BusPaint");
                    for(int i=0;i<9;i++) Detail("Window pillar",new Vector3(-4+i,2.2f,side*1.18f),new Vector3(.07f,1.8f,.09f),"VehicleAlloy");
                }
                for(int i=0;i<14;i++)
                {
                    var seat=new Vector3(2.3f-(i/2)*.91f,1.2f,i%2==0?-.72f:.72f);
                    Detail("Passenger seat",seat,new Vector3(.6f,.18f,.5f),"DistrictBlue");
                    Detail("Seat back",seat+Vector3.left*.25f+Vector3.up*.35f,new Vector3(.12f,.7f,.5f),"DistrictBlue");
                }
            }
            int count=VehicleSeats.Count(car);
            for(int i=0;i<count;i++)
            {
                var g=new GameObject("Occupant / "+VehicleSeats.Name(car,i),typeof(SpriteRenderer));
                g.transform.SetParent(transform,false);
                g.transform.localPosition=VehicleSeats.Local(car,i);
                g.transform.localScale=Vector3.one;
                var r=g.GetComponent<SpriteRenderer>();r.sprite=Resources.LoadAll<Sprite>("Art/NPC/Civic/"+new[]{"concierge","teacher","medic","commander"}[i%4])[0];
                r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                string identity=passengerRoles[(i+Mathf.Abs(car.GetInstanceID())%passengerRoles.Length)%passengerRoles.Length];
                if(i==0&&car.GetComponent<EmergencyAmbulance>())identity="Doctor";
                if(car.GetComponent<PoliceCar>())identity="CyberPolice";
                if(car.GetComponent<TacticalTransport>()&&!car.GetComponent<PoliceCar>())identity="CyberPolice";
                if(car.GetComponent<MilitaryVehicleAI>())identity="Soldier";
                if(car.IsAircraft)identity=i==0?FacilityPeople.Key(0):FacilityPeople.Key(4+i%4);
                if(car.IsWatercraft)identity=i==0?FacilityPeople.Key(10):passengerRoles[i%passengerRoles.Length];
                identities.Add(identity);
                var directional=i==0?VehiclePortraits.Driver(identity,1):VehiclePortraits.Passenger(identity,1);if(directional)r.sprite=directional;
                occupants.Add(r);people.Add(r.sprite);
            }
            SetPassengers(bus?9:car.occupied?Mathf.Abs(car.GetInstanceID())%3:0);
        }
        void Detail(string name,Vector3 at,Vector3 scale,string material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(transform,false);
            g.transform.localPosition=at;g.transform.localScale=scale;Destroy(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/"+material);
        }
        public void SetPassengers(int count)
        {
            int next=Mathf.Clamp(count,0,Mathf.Max(0,occupants.Count-1));
            for(int i=PassengerCount+1;i<=next;i++)identities[i]=car.GetComponent<TacticalTransport>()?"Swat":car.GetComponent<MilitaryVehicleAI>()?"Soldier":car.GetComponent<PoliceCar>()?"Police":car.IsAircraft?FacilityPeople.Key(4+(boardingSerial++ + i)%4):passengerRoles[(boardingSerial++ + i)%passengerRoles.Length];
            PassengerCount=next;
        }
        public void SetManifest(List<string> manifest)
        {
            PassengerCount=Mathf.Min(manifest.Count,occupants.Count-2);
            for(int i=0;i<PassengerCount;i++)identities[i+1]=manifest[i];
        }
        public void SetCrew(string identity,int count){for(int i=0;i<identities.Count;i++)identities[i]=identity;PassengerCount=Mathf.Clamp(count,0,occupants.Count-1);}
        public List<string> ReleaseOccupants(bool playerDriver)
        {
            var result=new List<string>();
            var taxi=car.GetComponent<CityTaxiService>();
            if(car.occupied&&!playerDriver&&identities.Count>0&&!(taxi&&taxi.Air))result.Add(identities[0]);
            for(int i=1;i<=PassengerCount;i++)result.Add(identities[i]);
            SetPassengers(0);return result;
        }
        void LateUpdate()
        {
            if(!car)return;
            bool cockpit=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car&&GameDirector.Instance.CameraRig.FirstPersonVehicle;
            if(glass)glass.SetColor("_BaseColor",cockpit?new Color(.05f,.1f,.12f,.025f):new Color(.10f,.20f,.24f,.14f));
            var camera=Camera.main;bool nearby=camera&&(transform.position-camera.transform.position).sqrMagnitude<180*180;
            if(Time.time<visualClock)return;visualClock=Time.time+(cockpit?0:nearby?.08f:.75f);
            bool seo=UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car;
            bool rider=CityBusService.Instance&&CityBusService.Instance.Riding==car;
            for(int i=0;i<occupants.Count;i++)
            {
                var r=occupants[i];
                r.transform.localPosition=VehicleSeats.Local(car,i);
                bool isSeo=seo&&i==UrbanSimulation.Instance.SeatIndex||rider&&i==occupants.Count-1;
                r.enabled=(nearby||seo||rider)&&(!car.Wrecked||car.GetComponent<VehicleFailure>())&&(isSeo||(i==0?car.occupied:i<=PassengerCount));
                var taxi=car.GetComponent<CityTaxiService>();if(taxi&&taxi.Air&&i==0&&!isSeo)r.enabled=false;
                if(!nearby&&!isSeo)continue;
                if(isSeo&&GameDirector.Instance&&GameDirector.Instance.CameraRig.FirstPersonVehicle)r.enabled=false;
                if(!r.enabled)continue;
                string identity=identities[i];
                if(car.GetComponent<GangConvoy>())identity="GangCrimson";
                var stolen=car.GetComponent<StolenVehicle>();
                if(i==0&&stolen&&stolen.Driver)
                {
                    var person=stolen.Driver.GetComponent<DirectionalPerson>();
                    if(person)identity=person.art;
                }
                int view=PeopleArt.Direction(car.Forward);
                r.sprite=isSeo?VehiclePortraits.Seo(SeoKinetic.Direction(car.Forward,Camera.main?Camera.main.transform.right:Vector3.right),i==0)
                    :i==0?VehiclePortraits.Driver(identity,view):VehiclePortraits.Passenger(identity,view);
                if(!r.sprite)r.sprite=people[i];
                float height=car.IsSpecial?1.05f:car.type==CityVehicleType.Bus?1.06f:car.type==CityVehicleType.Truck?.98f:car.type==CityVehicleType.Motorcycle?.9f:.72f;
                float scale=height/Mathf.Max(.1f,r.sprite.bounds.size.y);
                if(car.type!=CityVehicleType.Motorcycle)scale=Mathf.Min(scale,(car.IsHeavy?1.05f:.76f)/Mathf.Max(.1f,r.sprite.bounds.size.x));
                r.transform.localScale=Vector3.one*scale;
                r.flipX=false;
                if(Camera.main)r.transform.rotation=Quaternion.Euler(0,Camera.main.transform.eulerAngles.y,0);
            }
        }
        void OnDestroy(){if(glass)Destroy(glass);}
    }
}

