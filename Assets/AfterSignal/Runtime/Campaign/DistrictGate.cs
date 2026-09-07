using UnityEngine;

namespace AfterSignal
{
    public sealed class DistrictGate : MonoBehaviour
    {
        Vector3 start;
        Collider barrier;
        Renderer panel;
        void Start()
        {
            start = transform.position;
            barrier = GetComponent<Collider>();
            panel = GetComponent<Renderer>();
        }

        void Update()
        {
            var g = GameDirector.Instance;
            if (!g)
                return;
            bool open = g.Power && g.Cleared;
            barrier.enabled = !open;
            transform.position = Vector3.Lerp(transform.position, start + Vector3.up * (open ? 4.5f : 0), 1 - Mathf.Exp(-Time.deltaTime * 4));
            if (panel)
                panel.enabled = !open || transform.position.y < start.y + 4.2f;
        }
    }
}
