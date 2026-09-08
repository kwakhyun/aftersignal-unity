using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(1000)]
    public sealed class NpcBody:MonoBehaviour
    {
        WorldActor actor;Collider solid;Transform shell;float next;static float nextAudible;
        public int Bumps {get;private set;}
        static readonly string[] civilian={"똑바로 보고 다녀!","앗, 앞 좀 봐 주세요.","어이쿠! 괜찮으세요?","급해도 조심해야죠.","왜 밀고 그래요?","아야, 어깨 부딪혔잖아요.","잠깐만요, 지나갈게요.","거 참, 길 좀 보고 가요.","서하 씨, 깜짝 놀랐어요!","휴대폰 말고 앞을 봐요.","아이 참, 커피 쏟을 뻔했네.","괜찮아요. 다음엔 조심해요."};
        void Start()
        {
            actor=GetComponent<WorldActor>();
            var cc=GetComponent<CharacterController>();
            if(cc){solid=cc;return;}
            shell=new GameObject("Physical personal space").transform;shell.SetParent(transform,false);shell.gameObject.layer=9;
            var c=shell.gameObject.AddComponent<CapsuleCollider>();c.radius=actor.monster?.65f:.34f;c.height=1.9f;c.center=Vector3.up*.98f;solid=c;
        }
        public void Bump(PlayerMotor player)
        {
            if(!actor||!actor.Alive||Time.time<next||player.Velocity.sqrMagnitude<.4f)return;
            next=Time.time+6;Bumps++;
            if(Time.time<nextAudible||actor.monster)return;nextAudible=Time.time+.8f;
            var npc=GetComponent<CityNpc>();int seed=npc?npc.variation:Mathf.Abs(GetInstanceID());
            NpcSpeech.Say(this,actor.military?"통제 구역입니다. 거리를 유지하세요.":actor.police?"경찰입니다. 밀지 말고 천천히 이동하세요.":actor.gang?"어딜 들이받아? 조심해.":civilian[(seed+Bumps)%civilian.Length],3.2f,2);
            GetComponent<DirectionalPerson>()?.Face(player.transform.position,1.5f);
            if(npc)npc.SocialUntil=Mathf.Max(npc.SocialUntil,Time.time+.7f);
            player.Director.Audio.Play("urban_impact",player.Shoulder,.09f,0);
        }
        void LateUpdate()
        {
            if(!actor||!solid)return;
            solid.enabled=actor.Alive;
            if(shell){shell.rotation=Quaternion.identity;var s=transform.lossyScale;shell.localScale=new Vector3(1/Mathf.Max(.01f,Mathf.Abs(s.x)),1/Mathf.Max(.01f,Mathf.Abs(s.y)),1/Mathf.Max(.01f,Mathf.Abs(s.z)));}
            var g=GameDirector.Instance;if(!actor.Alive||!g||g.Blocked||!g.Player.Controller.enabled)return;
            var player=g.Player;var d=transform.position-player.transform.position;
            if(Mathf.Abs(d.y)>1.6f)return;d.y=0;float distance=d.magnitude;
            // Transform-driven pedestrians cannot occupy a stationary character's body.
            if(distance>.05f&&distance<.67f)
            {var shift=d/distance*(.68f-distance);if(!Physics.SphereCast(transform.position+Vector3.up,.32f,shift.normalized,out _,shift.magnitude+.02f,1,QueryTriggerInteraction.Ignore))transform.position+=shift;Bump(player);}
        }
        void OnDestroy(){if(shell)Destroy(shell.gameObject);}
    }
    public sealed partial class PlayerMotor
    {
        void OnControllerColliderHit(ControllerColliderHit hit){hit.collider.GetComponentInParent<NpcBody>()?.Bump(this);}
    }
}
