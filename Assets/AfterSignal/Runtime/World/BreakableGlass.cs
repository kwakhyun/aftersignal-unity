using UnityEngine;

namespace AfterSignal
{
    public sealed class BreakableGlass : MonoBehaviour
    {
        public float health = 64;
        public bool Broken { get; private set; }

        public void Hit(float amount)
        {
            if (Broken)
                return;
            health -= amount;
            if (health > 0)
            {
                SignalEffects.Burst(transform.position, new Color(.5f, .9f, 1), 9, 3);
                return;
            }

            Broken = true;
            GetComponent<Collider>().enabled = false;
            GetComponent<Renderer>().enabled = false;
            SignalEffects.Glass(transform.position, transform.localScale);
            var game = GameDirector.Instance;
            if (game)
            {
                game.GlassBroken();
                game.Audio.PlayCue(1300, .28f, .18f);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
        }
    }
}
