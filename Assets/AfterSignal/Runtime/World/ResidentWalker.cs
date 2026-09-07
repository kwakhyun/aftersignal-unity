using UnityEngine;

namespace AfterSignal
{
    public sealed class ResidentWalker : MonoBehaviour
    {
        public string role = "teacher";
        public Vector3 from, to;
        public float speed = 1.2f;
        public bool walking = true;
        SpriteRenderer visual;
        Sprite[] poses;
        bool outward = true;
        float phase;
        void Start()
        {
            visual = GetComponent<SpriteRenderer>();
            poses = Resources.LoadAll<Sprite>("Art/NPC/Civic/" + role);
            System.Array.Sort(poses, (a, b) => string.CompareOrdinal(a.name, b.name));
        }

        void Update()
        {
            var g = GameDirector.Instance;
            if (!g || g.Blocked || !visual)
                return;
            var target = outward ? to : from;
            var delta = target - transform.position;
            if (walking)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                if (delta.magnitude < .05f)
                    outward = !outward;
                if (Mathf.Abs(delta.x) > .01f)
                    visual.flipX = delta.x < 0;
                phase += Time.deltaTime * 5;
            }

            if (poses != null && poses.Length > 0)
                visual.sprite = poses[walking ? 1 + (int)phase % Mathf.Max(1, poses.Length - 1) : 0];
        }

        void LateUpdate()
        {
            if (visual && Camera.main)
                transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0);
        }
    }
}
