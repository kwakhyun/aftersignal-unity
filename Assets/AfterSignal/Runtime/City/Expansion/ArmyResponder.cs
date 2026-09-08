using UnityEngine;
namespace AfterSignal
{
    public sealed class ArmyResponder:MonoBehaviour
    {
        public WorldActor Body {get;private set;}public int Shots {get;private set;}
        CharacterController controller;RiftIncursion incident;PursuitPath path=new();float cooldown,gravity;int variation;
        public static ArmyResponder Create(Vector3 at,int variant,RiftIncursion incident)
        {
            var go=new GameObject("국방대응부 / "+(variant%3==0?"분대장":variant%3==1?"소총수":"지원병"),typeof(CharacterController),typeof(WorldActor),typeof(ArmyResponder));go.layer=9;go.transform.position=at;
            var a=go.GetComponent<ArmyResponder>();a.incident=incident;a.variation=variant;a.Body=go.GetComponent<WorldActor>();a.Body.military=true;a.Body.health=150;
            a.controller=go.GetComponent<CharacterController>();a.controller.radius=.34f;a.controller.height=2.1f;a.controller.center=Vector3.up*1.05f;a.controller.stepOffset=.3f;
            var visual=new GameObject("Uniform",typeof(SpriteRenderer));visual.transform.SetParent(go.transform,false);
            var r=visual.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get("Soldier",0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");PeopleArt.Attach(go,"Soldier");return a;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);
            if(!Body.Alive){GetComponentInChildren<SpriteRenderer>().transform.rotation=Quaternion.Euler(0,0,85);controller.enabled=false;enabled=false;Destroy(gameObject,12);return;}
            var target=FactionCombat.NearestOpponent(Body,100);cooldown-=dt;gravity=controller.isGrounded?-2:gravity-dt*23;
            Vector3 desired=incident?incident.Position+new Vector3((variation%3-1)*9,0,-14+variation/3*8):transform.position;
            if(target)desired=target.transform.position;
            float distance=Vector3.Distance(Body.Center,desired);bool visible=target&&FactionCombat.Visible(Body.Center,target.Center,90);
            controller.Move((distance>(target?21:2)&&!visible?path.Direction(transform.position,desired)*4:distance>29?path.Direction(transform.position,desired)*3:Vector3.zero)*dt+Vector3.up*gravity*dt);
            if(target)GetComponent<DirectionalPerson>()?.Face(target.Center,.4f);
            if(visible&&cooldown<=0)
            {cooldown=.22f+variation%3*.07f;Shots++;if(incident)incident.MilitaryShots++;FactionCombat.Fire(Body,Body.Center+Vector3.up*.25f,target.Center,100,18,SignalEffects.Gold,false);g.Audio.PlayGun(GunshotKind.Rifle,Body.Center,.75f);}
        }
    }
}
