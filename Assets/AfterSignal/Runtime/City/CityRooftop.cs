using UnityEngine;

namespace AfterSignal
{
    public sealed class CityRooftop : MonoBehaviour
    {
        public int site = -1;
        public Vector3 landing;
        public static Vector3 Destination(int id)
        {
            foreach (var roof in Object.FindObjectsByType<CityRooftop>(FindObjectsSortMode.None))
                if (roof.site == id)
                    return roof.landing;
            return UrbanCatalog.Door(id);
        }
    }
}
