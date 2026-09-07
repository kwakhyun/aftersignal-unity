using UnityEngine;

namespace AfterSignal
{
    // Hide only upper volumes when their footprint blocks the camera-to-player sightline.
    // Ground-floor colliders and door geometry remain intact.
    public sealed class CityBuildingCutaway : MonoBehaviour
    {
        public Renderer[] upper;
        public Vector3 center;
        public Vector2 footprint = new Vector2(40, 54);
        public bool visualOnly;
        public static readonly System.Collections.Generic.List<CityBuildingCutaway> All = new System.Collections.Generic.List<CityBuildingCutaway>();
        void OnEnable()
        {
            if (!All.Contains(this))
                All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }
    }
}
