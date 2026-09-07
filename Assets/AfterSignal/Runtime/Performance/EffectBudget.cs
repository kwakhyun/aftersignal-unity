using UnityEngine;

namespace AfterSignal
{
    public sealed class EffectBudget : MonoBehaviour
    {
        public static int Active { get; private set; }
        public static int Peak { get; private set; }

        void Awake()
        {
            Active++;
            Peak = Mathf.Max(Active, Peak);
        }

        void OnDestroy()
        {
            Active = Mathf.Max(0, Active - 1);
        }
    }
}
