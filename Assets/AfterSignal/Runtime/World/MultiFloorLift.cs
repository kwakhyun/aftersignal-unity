using UnityEngine;
namespace AfterSignal
{
    public sealed class MultiFloorLift : MonoBehaviour
    {
        public Transform platform;
        public int floors=3;
        public float floorHeight=4.2f;
        public float speed=2.2f;
        public string[] floorNames;
        public int Destination=>Mathf.Clamp(Mathf.RoundToInt((target-bottom)/floorHeight),0,floors-1);
        public int CurrentFloor=>platform?Mathf.Clamp(Mathf.RoundToInt((platform.position.y-bottom)/floorHeight),0,floors-1):0;
        float bottom,target;
        Collider[] cabinColliders;bool[] colliderStates;
        public bool Moving=>platform&&Mathf.Abs(platform.position.y-target)>.02f;
        public bool Aboard(PlayerMotor p)=>platform&&Mathf.Abs(p.transform.position.x-platform.position.x)<1.65f&&Mathf.Abs(p.transform.position.z-platform.position.z)<1.65f&&p.transform.position.y>platform.position.y-.2f&&p.transform.position.y<platform.position.y+.8f;
        void Awake(){bottom=platform?platform.position.y:transform.position.y;target=bottom;}
        void Start(){if(platform){cabinColliders=platform.GetComponentsInChildren<Collider>();colliderStates=new bool[cabinColliders.Length];}}
        public void Call(int landing)
        {
            var g=GameDirector.Instance;if(!g)return;
            if(Aboard(g.Player)){CityLife.Instance.LiftServices(this);return;}
            if(Moving){g.Toast("승강기가 이동 중입니다.");return;}
            target=bottom+landing*floorHeight;
            g.Toast("승강기 호출 · "+(landing+1)+"층");
        }
        public void Go(int floor){target=bottom+Mathf.Clamp(floor,0,floors-1)*floorHeight;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||!Moving)return;
            bool aboard=Aboard(g.Player);var delta=Vector3.up*(Mathf.MoveTowards(platform.position.y,target,speed*Time.deltaTime)-platform.position.y);
            // Move the passenger without sweeping against the cabin's newly displaced floor.
            // Otherwise a fast upward step can place that floor inside the character capsule.
            if(aboard&&cabinColliders!=null)for(int i=0;i<cabinColliders.Length;i++)if(cabinColliders[i]){colliderStates[i]=cabinColliders[i].enabled;cabinColliders[i].enabled=false;}
            if(aboard)g.Player.Carry(delta);
            platform.position+=delta;
            if(aboard&&cabinColliders!=null)for(int i=0;i<cabinColliders.Length;i++)if(cabinColliders[i])cabinColliders[i].enabled=colliderStates[i];
            Physics.SyncTransforms();
        }
    }
}
