using UnityEngine;
namespace AfterSignal
{
    public sealed class MilitaryVehicleAI:MonoBehaviour
    {
        CityVehicle car;MilitaryResponse response;WorldActor gunner;float clock,shot,disembark;int deployed;
        public void Initialize(CityVehicle c,MilitaryResponse r)
        {
            car=c;response=r;
            var source=new GameObject("Military vehicle gunner");source.transform.SetParent(transform,false);gunner=source.AddComponent<WorldActor>();gunner.military=true;gunner.helicopter=true;gunner.enabled=false;
        }
        void Start(){foreach(var renderer in GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Sculpted chassis")renderer.sharedMaterial=Resources.Load<Material>("Materials/DarkMetal");car.GetComponent<VehicleCabin>()?.SetPassengers(car.type==CityVehicleType.Truck?6:0);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car||car.Wrecked||!response)return;
            if(UrbanSimulation.Instance.Current==car){enabled=false;return;}
            float dt=Mathf.Min(.05f,Time.deltaTime);clock+=dt;shot-=dt;disembark-=dt;
            var target=WantedSystem.Instance.LastSeen;var delta=target-transform.position;
            var input=ControlFrame.Empty;
            if(car.IsAircraft)
            {
                var orbit=target+new Vector3(Mathf.Cos(clock*.13f)*120,car.type==CityVehicleType.Fighter?140:55,Mathf.Sin(clock*.13f)*120);
                delta=orbit-transform.position;input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(car.Forward,Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.up)/28,-1,1),car.type==CityVehicleType.Fighter?.25f:.7f);input.vertical=Mathf.Clamp(delta.y/15,-1,1);
            }
            else {delta.y=0;input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(car.Forward,delta,Vector3.up)/30,-1,1),delta.magnitude<32?0:.5f);input.guard=delta.magnitude<32;}
            // Never forward player attack/aim to NPC vehicle armaments.
            car.Drive(input,dt);
            if(car.type==CityVehicleType.Truck)
            {
                if((delta.magnitude<38||clock>18)&&deployed<6&&disembark<=0){car.speed=0;response.Deploy(transform.position-car.Forward*(car.HalfLength+2)+transform.forward*(deployed%2==0?-1:1),deployed);deployed++;disembark=.7f;car.GetComponent<VehicleCabin>()?.SetPassengers(6-deployed);}return;
            }
            Vector3 aim=UrbanSimulation.Instance.Current?UrbanSimulation.Instance.Current.transform.position+Vector3.up: g.Player.Shoulder;
            var mount=transform.position+Vector3.up*(car.type==CityVehicleType.Tank?3:1);
            if(shot>0||(aim-mount).sqrMagnitude>700*700)return;
            if(Physics.Linecast(mount,aim,out var wall,1,QueryTriggerInteraction.Ignore)&&!(UrbanSimulation.Instance.Current&&wall.transform.IsChildOf(UrbanSimulation.Instance.Current.transform))&&!wall.transform.IsChildOf(transform))return;
            if(car.type==CityVehicleType.Tank)
            {
                shot=5;SignalEffects.Beam(mount,aim,SignalEffects.Gold,.09f,.16f);
                BlastDamage.Create(aim,6,45,gunner,car);VehicleExplosion.Create(aim,1.8f);g.Audio.Play("cannon",mount,.3f,2);
                foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Turret assembly"){var d=aim-t.position;d.y=0;t.rotation=Quaternion.Euler(0,Vector3.SignedAngle(Vector3.right,d,Vector3.up),0);break;}
            }
            else{shot=car.type==CityVehicleType.Fighter?1.2f:.55f;FactionCombat.Fire(gunner,mount,aim,700,car.type==CityVehicleType.Fighter?24:16,SignalEffects.Gold,true,transform);g.Audio.PlayGun(GunshotKind.Automatic,mount,.8f);}
        }
    }
}
