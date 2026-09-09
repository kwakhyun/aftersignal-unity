using UnityEngine;
namespace AfterSignal
{
    public sealed class ArmyResponder:MonoBehaviour
    {
        public WorldActor Body {get;private set;}public int Shots {get;private set;}
        CharacterController controller;RiftIncursion incident;PursuitPath path=new();float cooldown,gravity,scan;int variation;WorldActor target;
        public string Decision {get;private set;}="현장 경계";
        public static ArmyResponder Create(Vector3 at,int variant,RiftIncursion incident)
        {
            var go=new GameObject("국방대응부 / "+(variant%3==0?"분대장":variant%3==1?"소총수":"지원병"),typeof(CharacterController),typeof(WorldActor),typeof(ArmyResponder));go.layer=9;go.transform.position=at;
            var a=go.GetComponent<ArmyResponder>();a.incident=incident;a.variation=variant;a.Body=go.GetComponent<WorldActor>();a.Body.military=true;a.Body.health=150;
            a.controller=go.GetComponent<CharacterController>();a.controller.radius=.34f;a.controller.height=2.1f;a.controller.center=Vector3.up*1.05f;a.controller.stepOffset=.3f;
            var visual=new GameObject("Uniform",typeof(SpriteRenderer));visual.transform.SetParent(go.transform,false);
            var r=visual.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get("Soldier",0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");NpcPersona.Ensure(a.Body,"Soldier","방위군 소총수");return a;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);
            if(!Body.Alive){GetComponentInChildren<SpriteRenderer>().transform.rotation=Quaternion.Euler(0,0,85);controller.enabled=false;enabled=false;Destroy(gameObject,12);return;}
            if(Body.Downed||!controller.enabled||CivilianImpact.Active(this))return;
            if(!LocalSimulation.Combat(transform.position)){target=null;Decision="원거리 대기";return;}
            if(Time.time>=scan||target&&!TacticalJudgment.Opponent(Body,target)){scan=Time.time+.35f;target=FactionCombat.NearestOpponent(Body,100);}
            cooldown-=dt;gravity=controller.isGrounded?-2:gravity-dt*23;
            Vector3 desired=incident?incident.Position+new Vector3((variation%3-1)*9,0,-14+variation/3*8):transform.position;
            if(target)desired=target.transform.position;
            float distance=Vector3.Distance(Body.Center,desired);bool visible=target&&TacticalJudgment.ClearShot(Body,target,target.Center,false,100);
            Decision=!target?"현장 확보":!visible?"사선 확보":Body.health<Body.MaxHealth*.35f?"엄폐 / 지원 요청":"확인된 위협 제압";
            bool flanking=target&&(!visible&&distance<28||Body.health<Body.MaxHealth*.35f);
            if(flanking)desired=TacticalJudgment.Flank(Body,target.Center,5);
            controller.Move((flanking?path.Direction(transform.position,desired)*2.6f:distance>(target?21:2)&&!visible?path.Direction(transform.position,desired)*4:distance>29?path.Direction(transform.position,desired)*3:Vector3.zero)*dt+Vector3.up*gravity*dt);
            if(target)GetComponent<DirectionalPerson>()?.Face(target.Center,.4f);
            if(visible&&cooldown<=0)
            {cooldown=Shots%3==2?1.1f:.24f+variation%3*.07f;Shots++;if(incident)incident.MilitaryShots++;FactionCombat.Fire(Body,Body.Center+Vector3.up*.25f,target.Center,100,18,SignalEffects.Gold,false);g.Audio.PlayGun(GunshotKind.Rifle,Body.Center,.75f);}
        }
    }
}
