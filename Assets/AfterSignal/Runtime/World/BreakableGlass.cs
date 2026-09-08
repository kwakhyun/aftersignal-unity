using UnityEngine;

namespace AfterSignal
{
    public sealed class BreakableGlass : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<BreakableGlass> All=new();
        public float health = 64;
        public bool campaign=true;
        public bool Broken { get; private set; }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}

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
            var hit=GetComponent<Collider>();if(hit)hit.enabled=false;
            var visual=GetComponent<Renderer>();if(visual)visual.enabled=false;
            SignalEffects.Glass(transform.position, transform.lossyScale);
            var game = GameDirector.Instance;
            if (game)
            {
                if(campaign)game.GlassBroken();
                game.Audio.PlayCue(1300, .28f, .18f);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
        }
    }
}
