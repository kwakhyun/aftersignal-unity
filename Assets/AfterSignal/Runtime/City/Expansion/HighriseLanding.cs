using UnityEngine;
namespace AfterSignal
{
    public sealed class HighriseLanding:MonoBehaviour
    {
        MultiFloorLift lift;float landing;Collider barrier;Renderer panel;
        public void Initialize(MultiFloorLift value,float y){lift=value;landing=y;barrier=GetComponent<Collider>();panel=GetComponent<Renderer>();}
        void Update()
        {
            bool closed=!lift||!lift.platform||Mathf.Abs(lift.platform.position.y-landing)>.12f||lift.Moving;
            if(barrier)barrier.enabled=closed;if(panel)panel.enabled=closed;
        }
    }
}
