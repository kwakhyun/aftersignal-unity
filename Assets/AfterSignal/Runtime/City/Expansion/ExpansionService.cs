using UnityEngine;
namespace AfterSignal
{
    public sealed class ExpansionService:MonoBehaviour
    {
        public int facility;
        public void Open()=>CityLife.Instance?.ExpansionServices(facility);
    }
}
