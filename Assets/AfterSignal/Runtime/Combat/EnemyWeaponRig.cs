using UnityEngine;

namespace AfterSignal
{
    public sealed class EnemyWeaponRig : MonoBehaviour
    {
        Transform grip;
        bool blade, heavy;
        public void Initialize(string kind)
        {
            blade = kind == "blade" || kind == "stalker" || kind == "shield";
            heavy = kind == "shield";
            grip = new GameObject("Weapon / articulated grip").transform;
            grip.SetParent(transform, false);
            grip.localPosition = Vector3.up * 1.3f;
            if (blade)
            {
                WorldGeometry.Part(grip, "Blade tang", new Vector3(.28f, 0, 0), new Vector3(.6f, .1f, .12f), "DarkMetal");
                WorldGeometry.Part(grip, "Hand guard", new Vector3(.52f, 0, 0), new Vector3(.09f, .38f, .2f), "Gold");
                WorldGeometry.Part(grip, "Forged blade", new Vector3(heavy ? 1.4f : 1.1f, 0, 0), new Vector3(heavy ? 1.7f : 1.25f, heavy ? .23f : .1f, .075f), "Chrome");
            }
            else
            {
                float length = kind == "pistol" ? .55f : kind == "shotgun" ? 1.25f : 1.1f;
                WorldGeometry.Part(grip, "Receiver", new Vector3(.45f, .02f, 0), new Vector3(length, .2f, .17f), "DarkMetal");
                WorldGeometry.Part(grip, "Barrel", new Vector3(.5f + length * .55f, .05f, 0), new Vector3(length * .7f, .07f, .08f), "Chrome");
                WorldGeometry.Part(grip, "Pistol grip", new Vector3(.3f, -.17f, 0), new Vector3(.15f, .3f, .14f), "Rubber");
                if (kind != "pistol")
                    WorldGeometry.Part(grip, "Shoulder stock", new Vector3(-.1f, -.02f, 0), new Vector3(.5f, .18f, .16f), "Metal");
            }
        }

        public void Present(Vector3 direction, float phase, bool braced = false)
        {
            if (!grip)
                return;
            if (direction.sqrMagnitude < .1f)
                direction = Vector3.left;
            if (blade)
            {
                float facing = direction.x >= 0 ? 1 : -1;
                float angle = phase <= 0 ? -65 : phase < .5f ? Mathf.Lerp(-65, 110, phase * 2) : Mathf.Lerp(110, -100, (phase - .5f) * 2);
                grip.rotation = Quaternion.Euler(0, facing > 0 ? 0 : 180, angle);
                grip.localPosition = new Vector3(facing * .25f, 1.25f, -.18f);
            }
            else
            {
                grip.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
                grip.localPosition = Vector3.up * (braced ? 1.4f : 1.25f) - direction.normalized * (phase > .5f ? .09f : 0);
            }
        }
    }
}
