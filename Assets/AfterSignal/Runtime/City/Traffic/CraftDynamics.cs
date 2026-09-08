using UnityEngine;

namespace AfterSignal
{
    public sealed class CraftDynamics : MonoBehaviour
    {
        CityVehicle car;
        float throttle, verticalSpeed;
        Transform model, rotor,tailRotor;Quaternion modelRest=Quaternion.identity;float rotorSpeed;
        public float Throttle => throttle;
        public void Initialize(CityVehicle vehicle)
        {
            car=vehicle;model=transform.Find("Detailed vehicle coachwork");
            if(model)modelRest=model.localRotation;
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name.StartsWith("Main rotor"))rotor=t;
            foreach(var t in GetComponentsInChildren<Transform>())if(t.name=="Tail rotor")tailRotor=t;
        }
        public void Drive(ControlFrame input,float dt)
        {
            if(!car||car.Wrecked)return;
            bool helicopter=car.type==CityVehicleType.CombatHelicopter;
            float max=car.IsWatercraft?19:helicopter?52:car.type==CityVehicleType.Fighter?130:92;
            bool cargo=car.GetComponent<AuthoredCraft>();if(cargo)max=8;
            max*=input.boost?1.35f:1;
            if(car.IsWatercraft||helicopter)throttle=input.move.y;
            else throttle=Mathf.Clamp01(throttle+input.move.y*dt*.32f);
            if(car.fuel<=0)throttle=0;
            car.GetComponent<VehicleSoundscape>()?.SetThrottle(throttle);
            float target=throttle*max;
            if(car.IsWatercraft&&input.vertical>0)target=0;
            car.speed=Mathf.MoveTowards(car.speed,target,dt*(car.IsWatercraft?4:helicopter?12:18));
            float steering=input.move.x*(car.IsWatercraft?(cargo?3:28):helicopter?52:car.speed>25?28:14)*dt;
            transform.Rotate(0,steering,0,Space.World);
            var old=transform.position;var delta=car.Forward*car.speed*dt;
            if(car.IsAircraft)
            {
                float lift=helicopter?input.vertical*16:car.speed>26?input.vertical*22:Mathf.Min(0,input.vertical)*6;
                if(!helicopter&&car.speed<26)lift=-9;
                if(car.fuel<=0)lift=-15;
                verticalSpeed=Mathf.MoveTowards(verticalSpeed,lift,dt*12);
                delta+=Vector3.up*verticalSpeed*dt;
                if(VehicleGround.Sample(car,old,.2f,4,out var floor)&&old.y+delta.y<floor.point.y+.12f)
                {delta.y=floor.point.y+.12f-old.y;if(verticalSpeed< -10&&car.speed>38)car.Damage(-verticalSpeed*2,old);verticalSpeed=0;}
            }
            else
            {
                var next=old+delta;
                if(!OceanLife.Contains(next)){car.speed=Mathf.MoveTowards(car.speed,0,dt*40);delta=Vector3.zero;}
                delta.y=(cargo?0:OceanLife.Surface)+Mathf.Sin(Time.time*1.4f+old.x*.05f)*.08f-old.y;
            }
            float len=delta.magnitude;
            if(len>.001f)
            {
                foreach(var hit in Physics.BoxCastAll(old+Vector3.up*(car.IsWatercraft?1.5f:2),new Vector3(car.HalfLength*.85f,car.IsWatercraft?.8f:1,car.HalfWidth*.8f),delta/len,transform.rotation,len+.15f,1,QueryTriggerInteraction.Ignore))
                {
                    if(hit.collider.transform.IsChildOf(transform)||hit.normal.y>.7f)continue;
                    delta=delta.normalized*Mathf.Max(0,hit.distance-.2f);
                    if(Mathf.Abs(car.speed)>9)car.Damage(Mathf.Abs(car.speed)*.4f,hit.point);
                    car.speed=0;throttle=0;break;
                }
                var p=old+delta;p.x=Mathf.Clamp(p.x,8,2192);p.z=Mathf.Clamp(p.z,NeonHarbor.South+10,1088);p.y=Mathf.Clamp(p.y,-3,520);transform.position=p;
                car.fuel=Mathf.Max(0,car.fuel-(old-p).magnitude*.0018f);
            }
            if(model)model.localRotation=Quaternion.Euler(input.move.x*(car.IsAircraft?-12:2),0,car.IsAircraft?verticalSpeed*.5f:Mathf.Sin(Time.time)*.6f)*modelRest;
            GetComponent<VehicleArmament>()?.Tick(input,dt);
        }
        void Update()
        {
            if(!car||GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            rotorSpeed=Mathf.MoveTowards(rotorSpeed,!car.Wrecked&&car.occupied&&car.fuel>0?1550:0,Time.deltaTime*320);
            if(rotor)rotor.Rotate(car.transform.up,Time.deltaTime*rotorSpeed,Space.World);
            if(tailRotor)tailRotor.Rotate(car.transform.forward,Time.deltaTime*rotorSpeed*2.1f,Space.World);
        }
    }
}
