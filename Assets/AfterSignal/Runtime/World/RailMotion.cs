using UnityEngine;

namespace AfterSignal
{
    public sealed class RailMotion : MonoBehaviour
    {
        public float parallax = .6f;
        public float span = 160;
        public float baseX;
        public bool arrivalTrain;
        public bool door;
        public float direction = 1;
        Vector3 initial;
        void Start()
        {
            initial = transform.position;
            baseX = initial.x;
        }

        void Update()
        {
            var game = GameDirector.Instance;
            if (!game || game.Blocked)
                return;
            if (arrivalTrain)
            {
                transform.position = initial + Vector3.right * (1 - Mathf.SmoothStep(0, 1, game.Arrival)) * 85;
                return;
            }

            if (door)
            {
                transform.position = initial + Vector3.right * direction * Mathf.Clamp01((game.Arrival - .8f) * 5) * 1.5f;
                return;
            }

            float x = baseX - Mathf.Repeat(game.TravelDistance * parallax, span);
            if (x < game.Player.transform.position.x - span * .55f)
                x += span;
            transform.position = new Vector3(x, initial.y, initial.z);
        }
    }
}
