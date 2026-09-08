using UnityEngine;
namespace AfterSignal
{
    public sealed class HighriseDoor:MonoBehaviour
    {
        public HighriseBuilding building;public bool exit;public int floor;
        public void Use(){if(exit)HighriseInterior.Active?.Leave();else if(building){building.Enter();if(floor>0)HighriseInterior.Active?.ArriveAt(floor);}}
    }
}
