using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class TacticalTransport:MonoBehaviour
    {
        static readonly List<TacticalTransport> all=new();
        public static bool Pending=>all.Exists(t=>t&&t.remaining>0&&t.car&&!t.car.Wrecked);
        public int Deployed{get;private set;}
        CityVehicle car;WantedSystem owner;int remaining;float elapsed,release;Transform ramp;bool withdrawing;
        public static TacticalTransport Create(WantedSystem system,Vector3 at,int count)
        {
            var vehicle=UrbanSimulation.Instance.Spawn(at,false,3);vehicle.name="특수대응팀 장갑차";
            var t=vehicle.gameObject.AddComponent<TacticalTransport>();t.car=vehicle;t.owner=system;t.remaining=count;
            vehicle.InitializeDurability();vehicle.health=vehicle.MaxHealth;vehicle.occupied=true;return t;
        }
        void OnEnable()=>all.Add(this);
        void OnDisable()=>all.Remove(this);
        IEnumerator Start()
        {
            yield return null;
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Sculpted chassis"||renderer.name.Contains("Cargo")||renderer.name.Contains("roof"))renderer.enabled=false;
            var root=new GameObject("Tactical armoured body").transform;root.SetParent(transform,false);
            WorldGeometry.Part(root,"Armoured troop compartment",new Vector3(-1,1.65f,0),new Vector3(5,2.5f,2.65f),"DarkMetal");
            WorldGeometry.Part(root,"Armoured cab",new Vector3(2.2f,1.65f,0),new Vector3(1.4f,2,2.6f),"DarkMetal");
            for(int s=-1;s<=1;s+=2)
            {
                for(int i=0;i<5;i++){var plate=WorldGeometry.Part(root,"Sloped composite armour",new Vector3(-2.8f+i,.95f,s*1.4f),new Vector3(.9f,1.4f,.17f),"Metal");plate.transform.localRotation=Quaternion.Euler(s*12,0,0);}
                WorldGeometry.Part(root,"Ballistic side glazing",new Vector3(2.1f,2.1f,s*1.32f),new Vector3(.8f,.45f,.035f),"Glass");
                WorldGeometry.Part(root,"Side step",new Vector3(.2f,.55f,s*1.6f),new Vector3(5.8f,.13f,.35f),"DarkMetal");
            }
            var windshield=WorldGeometry.Part(root,"Armoured windshield",new Vector3(2.95f,2.05f,0),new Vector3(.05f,.55f,2),"Glass");
            windshield.GetComponent<Renderer>().sharedMaterial=new Material(Resources.Load<Shader>("Shaders/StructuralGlass")){name="Tactical safety glass"};
            ramp=new GameObject("Rear deployment ramp").transform;ramp.SetParent(root,false);ramp.localPosition=new Vector3(-3.6f,.45f,0);
            WorldGeometry.Part(ramp,"Rear ramp panel",new Vector3(0,1,0),new Vector3(.17f,2,2.25f),"Metal");
            gameObject.AddComponent<ResponseLightbar>();
            car.GetComponent<VehicleCabin>()?.SetPassengers(remaining);
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!car||!game||game.Blocked)return;
            if(car.Wrecked){remaining=0;return;}
            if(withdrawing){if(!car.owned)Destroy(gameObject,8);enabled=false;return;}
            if(UrbanSimulation.Instance.Current==car){remaining=0;return;}
            elapsed+=Time.deltaTime;
            var delta=owner.LastSeen-transform.position;delta.y=0;
            if(delta.magnitude>24&&elapsed<18&&remaining>0)
            {var input=ControlFrame.Empty;input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(car.Forward,delta,Vector3.up)/30,-1,1),.55f);car.Drive(input,Mathf.Min(.05f,Time.deltaTime));return;}
            car.speed=0;if(ramp)ramp.localRotation=Quaternion.RotateTowards(ramp.localRotation,Quaternion.Euler(0,0,88),Time.deltaTime*70);
            if(remaining<=0||elapsed<2)return;
            release-=Time.deltaTime;if(release>0)return;release=.65f;
            Vector3 door=transform.position-car.Forward*(car.HalfLength+1.7f)+transform.forward*((Deployed%2==0?1:-1)*.65f);
            if(!CityGangWar.FindGround(door,out var ground))return;
            var officer=PoliceOfficer.Create(owner,ground,4,Deployed);owner.Officers.Add(officer);NpcSpeech.Say(officer,"하차! 엄폐하고 용의자를 제압해!",3);
            remaining--;Deployed++;car.GetComponent<VehicleCabin>()?.SetPassengers(remaining);
        }
        public void Withdraw(){remaining=0;withdrawing=true;var lights=GetComponent<ResponseLightbar>();if(lights)lights.enabled=false;}
    }
}
