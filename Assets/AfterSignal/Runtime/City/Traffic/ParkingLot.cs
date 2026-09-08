using UnityEngine;
namespace AfterSignal
{
    public sealed class ParkingLot : MonoBehaviour
    {
        public int spaces=12;
        public float spacing=3.2f;
        public bool roadside;
        CityVehicle[] cars;bool[] used;float next;
        void Update()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;
            if(!g||!g.Ready||!sim||Time.time<next)return;next=Time.time+2;
            if(cars==null){cars=new CityVehicle[spaces];used=new bool[spaces];}
            float distance=Vector3.Distance(g.Player.transform.position,transform.position);
            if(distance<235)
            {
                for(int i=0;i<spaces;i++)
                {
                    if(cars[i]||used[i]||i%7==6)continue;
                    var at=transform.TransformPoint(new Vector3((i%6-2.5f)*spacing,0,(i/6)*14));
                    if(Physics.CheckBox(at+Vector3.up,.8f*Vector3.one,Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                    var car=sim.Spawn(at,false,i%8==0?4:i%6==0?5:i%5==0?1:0);car.transform.rotation=transform.rotation*Quaternion.Euler(0,roadside?0:90,0);
                    var assignment=car.gameObject.AddComponent<ParkingAssignment>();assignment.lot=this;assignment.slot=i;cars[i]=car;used[i]=true;
                }
            }
            else if(distance>390)
            {
                for(int i=0;i<spaces;i++)if(cars[i]&&!cars[i].owned&&!cars[i].occupied){Destroy(cars[i].gameObject);cars[i]=null;used[i]=false;}
            }
        }
    }
    public sealed class ParkingAssignment : MonoBehaviour
    {
        public ParkingLot lot;public int slot;
    }
}
