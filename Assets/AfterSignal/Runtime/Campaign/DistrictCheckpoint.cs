using UnityEngine;

namespace AfterSignal
{
    public sealed class DistrictCheckpoint : MonoBehaviour
    {
        float last;
        void Update()
        {
            var g = GameDirector.Instance;
            if (!g || g.Blocked || !g.Player.Grounded)
                return;
            var p = g.Player.transform.position;
            if (p.x < last + 18)
                return;
            if (Physics.Raycast(p + Vector3.up * .2f, Vector3.down, .7f, 1, QueryTriggerInteraction.Ignore))
            {
                last = p.x;
                g.checkpoint = p + Vector3.up * .15f;
            }
        }
    }
}
