using UnityEngine;

namespace AfterSignal
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        public string kind="blade";
        public float maxHealth=72;
        public bool boss;
        public float activateAt;
        public float Health { get; private set; }
        public bool Alive => Health>0;
        public bool Active { get; private set; }
        public float Telegraph { get; private set; }
        public float Facing { get; private set; }=-1;
        public float Exposed { get; private set; }
        GameDirector director;
        PixelActor actor;
        CharacterController controller;
        Vector3 knockback;
        float cooldown=1.6f,hurt,clock,gravity,leapTime;
        bool fired;
        float freezeUntil,deathTime,impactLean;
        public void React(float stagger,float stop,Vector3 direction){if(boss)return;hurt=Mathf.Max(hurt,stagger);freezeUntil=Mathf.Max(freezeUntil,Time.unscaledTime+stop);impactLean=-Mathf.Sign(direction.x)*13;Telegraph=0;cooldown=Mathf.Max(cooldown,.38f);}
        public void Initialize(GameDirector owner)
        {
            director=owner;Health=maxHealth;actor=GetComponent<PixelActor>();actor.Initialize();
            controller=GetComponent<CharacterController>();gameObject.layer=9;
            Active=false;
        }
        public void Tick(float dt)
        {
            if(!Alive){deathTime+=dt;actor.Pose(deathTime<.13f?6:7,Facing,0,Mathf.Lerp(impactLean,Mathf.Sign(impactLean)*35,Mathf.Clamp01(deathTime/.32f)));if(deathTime>.38f)gameObject.SetActive(false);return;}
            if(Time.unscaledTime<freezeUntil)return;
            clock+=dt;hurt=Mathf.Max(0,hurt-dt);Exposed=Mathf.Max(0,Exposed-dt);
            var player=director.Player;Vector3 delta=player.transform.position-transform.position;
            Active=player.transform.position.x>=activateAt;
            if(delta.sqrMagnitude>.1f)Facing=Mathf.Sign(delta.x);
            int frame=0;
            if(Active&&!boss){
                cooldown-=dt;
                if(Telegraph>0){
                    Telegraph-=dt;frame=3;
                    if(Telegraph<=0&&!fired){
                        fired=true;cooldown=kind=="gunner"?1.7f:1.15f;
                        if(kind=="gunner"){
                            Vector3 start=transform.position+new Vector3(Facing*.6f,1.25f,0);
                            Vector3 end=player.Shoulder;
                            SignalEffects.Beam(start,end,SignalEffects.Red,.035f,.2f);
                            if(Mathf.Abs(delta.z)<1.2f&&Mathf.Abs(delta.y)<2.5f&&Mathf.Abs(delta.x)<14&&!Physics.Linecast(start,end,1,QueryTriggerInteraction.Ignore))player.ReceiveDamage(10,transform.position);
                        }else if(Mathf.Abs(delta.x)<2.3f&&Mathf.Abs(delta.z)<1.2f&&Mathf.Abs(delta.y)<2.6f){SignalEffects.Slash(transform.position+Vector3.up,Facing,1.5f,SignalEffects.Red,0);player.ReceiveDamage(12,transform.position);}
                    }
                }else if(leapTime>0){leapTime-=dt;controller.Move(Vector3.right*Facing*5.2f*dt);frame=4;}
                else if(hurt<=0){
                    float desired=kind=="gunner"?8:1.65f;
                    if(Mathf.Abs(delta.x)>desired||Mathf.Abs(delta.z)>.7f){
                        Vector3 move=new Vector3(Mathf.Abs(delta.x)>desired?Facing*2.5f:0,0,Mathf.Clamp(delta.z,-1,1)*1.8f);
                        if(controller.isGrounded&&Mathf.Abs(move.x)>.1f&&!Physics.Raycast(transform.position+Vector3.up+Vector3.right*Facing*.75f,Vector3.down,2.1f,1,QueryTriggerInteraction.Ignore)){gravity=11;leapTime=.82f;}
                        controller.Move(move*dt);frame=1+(int)(clock*7)%2;
                    }else if(cooldown<=0){Telegraph=kind=="gunner"?.8f:.65f;fired=false;}
                }
            }
            if(!boss){
                gravity=controller.isGrounded&&leapTime<=0?-2:gravity-30*dt;
                controller.Move((knockback+Vector3.up*gravity)*dt);knockback=Vector3.MoveTowards(knockback,Vector3.zero,22*dt);
                var pos=transform.position;pos.z=Mathf.Clamp(pos.z,-director.halfDepth,director.halfDepth);transform.position=pos;
                if(pos.y<-8){Damage(Health,Vector3.zero);return;}
            }
            if(hurt>0)frame=5;else if(boss)frame=Exposed>0?5:Telegraph>0?3:(int)(clock*3)%2;
            impactLean=Mathf.MoveTowards(impactLean,0,55*dt);actor.Pose(frame,Facing,hurt>0?.28f:0,hurt>0?impactLean:0);
        }
        public void SetTelegraph(float seconds){Telegraph=seconds;}
        public void SetExposed(float seconds){Exposed=seconds;Telegraph=0;}
        public void Damage(float amount,Vector3 force,bool core=false)
        {
            if(!Alive)return;
            if(boss&&!core){amount*=.035f;director.Toast("장갑이 공격을 흡수합니다 · 양쪽 축전기에 로프를 연결하세요",1.8f);}
            Health=Mathf.Max(0,Health-amount);hurt=.2f;knockback=force;
            if(core)SignalEffects.Impact(transform.position+Vector3.up*1.8f,Vector3.right,SignalEffects.Gold,1.6f);
            director.DamageNumber(transform.position+Vector3.up*2.5f,Mathf.CeilToInt(amount),core);
            if(Health<=0){director.EnemyDied(this);controller.enabled=false;deathTime=0;impactLean=-Mathf.Sign(force.x)*13;actor.Pose(6,Facing);director.Audio.Play("death",transform.position,.27f,2);SignalEffects.Dust(transform.position,Vector3.up,.7f);}
        }
    }
}
