using UnityEngine;

namespace AfterSignal
{
    // Per-renderer cloth palette preserves source skin, hair, weapon and animation frames.
    public sealed class ActorWardrobe : MonoBehaviour
    {
        public bool citizen;
        public int variant;
        SpriteRenderer visual;
        MaterialPropertyBlock block;
        int applied = -99;
        Vector3 initialScale;
        bool accessories;
        public static readonly string[] Names =
        {
            "신호복원 재킷",
            "새벽 청록 재킷",
            "야간 순찰 코트",
            "도시 여행 코트"
        };
        static readonly Color[] Colors =
        {
            Color.white,
            new Color(.12f, .72f, .68f),
            new Color(.2f, .3f, .62f),
            new Color(.76f, .44f, .22f),
            new Color(.61f, .31f, .53f),
            new Color(.48f, .64f, .34f)
        };
        void LateUpdate()
        {
            if (!visual)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
                if (visual)
                    initialScale = visual.transform.localScale;
            }

            if (!visual)
                return;
            if(citizen&&GetComponent<DirectionalPerson>())return;
            int id = citizen ? variant + 1 : LifeState.Outfit;
            if (applied == id)
                return;
            applied = id;
            if (block == null)
                block = new MaterialPropertyBlock();
            visual.GetPropertyBlock(block);
            block.SetColor("_ClothColor", Colors[Mathf.Abs(id) % Colors.Length]);
            block.SetFloat("_ClothAmount", !citizen && id == 0 ? 0 : .85f);
            visual.SetPropertyBlock(block);
            if (citizen)
            {
                visual.transform.localScale = Vector3.Scale(initialScale, new Vector3(1 + (variant % 3 - 1) * .09f, 1 + (variant % 5 - 2) * .035f, 1));
                if (!accessories)
                {
                    accessories = true;
                    if (variant % 4 == 0)
                    {
                        WorldGeometry.Part(visual.transform, "Citizen cap", new Vector3(0, 1.97f, 0), new Vector3(.72f, .08f, .48f), "DistrictBlue", PrimitiveType.Cylinder);
                    }

                    if (variant % 4 == 1)
                        WorldGeometry.Part(visual.transform, "Messenger bag", new Vector3(.37f, .85f, -.03f), new Vector3(.38f, .47f, .22f), "DistrictWarm");
                    if (variant % 4 == 2)
                        WorldGeometry.Part(visual.transform, "Work satchel", new Vector3(-.42f, .5f, -.02f), new Vector3(.38f, .45f, .16f), "Metal");
                }
            }
        }
    }
}
