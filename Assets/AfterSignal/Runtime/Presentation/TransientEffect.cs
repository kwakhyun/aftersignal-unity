using UnityEngine;

namespace AfterSignal
{
    public sealed class TransientEffect : MonoBehaviour
    {
        public float life = .3f;
        public LineRenderer line;
        public Vector3 velocity;
        public bool fall;
        public bool expand;
        public Vector3 expansionCenter;
        Vector3[] points;
        float age, width;
        void Start()
        {
            if (line)
            {
                width = line.widthMultiplier;
                if (expand)
                {
                    points = new Vector3[line.positionCount];
                    line.GetPositions(points);
                }
            }
        }

        void Update()
        {
            if (GameDirector.Instance && GameDirector.Instance.Blocked)
                return;
            float dt = Time.deltaTime;
            age += dt;
            if (age >= life)
            {
                Destroy(gameObject);
                return;
            }

            if (line)
                line.widthMultiplier = width * (1 - age / life);
            if (expand && line && points != null)
                for (int i = 0; i < points.Length; i++)
                    line.SetPosition(i, expansionCenter + (points[i] - expansionCenter) * Mathf.Lerp(.25f, 1, 1 - Mathf.Pow(1 - age / life, 3)));
            if (fall)
            {
                velocity.y -= 15 * dt;
                transform.position += velocity * dt;
                transform.Rotate(new Vector3(90, 180, 45) * dt);
            }
        }
    }
}
