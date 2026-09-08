using UnityEngine;
namespace AfterSignal
{
    public sealed class FloorAccess:MonoBehaviour
    {
        public MultiFloorLift lift;public int floor;
        public void Open(){if(lift)lift.Call(floor);}
    }
}
