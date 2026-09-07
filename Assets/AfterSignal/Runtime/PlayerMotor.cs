using UnityEngine;
using System.Collections.Generic;

namespace AfterSignal
{
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerMotor : MonoBehaviour
    {
        public GameTuning Tuning { get; private set; }
        public GameDirector Director { get; private set; }
        public CharacterController Controller { get; private set; }
        public RopeMotor Rope { get; private set; }
        public Vector3 Velocity;
        public Vector3 Shoulder => transform.position+Vector3.up*1.45f;
        public Vector3 Muzzle {
            get {
                if(actor&&actor.TryMuzzle(Facing,out var point))return point;
                bool late=AttackTime>0&&AttackTime<AttackDuration*.5f;
                Vector2 pixel=Grounded?(late?new Vector2(178,117):new Vector2(184,123)):(late?new Vector2(169,132):new Vector2(168,137));
                return transform.position+Vector3.right*((pixel.x-112)/49f*Facing)+(Camera.main?Camera.main.transform.up:Vector3.up)*((212-pixel.y)/49f);
            }
        }
        public Vector3 Aim { get; private set; }
        public float Facing { get; private set; }=1;
        public bool Grounded { get; private set; }
        public bool Guarding { get; private set; }
        public float Health { get; private set; }=100;
        public float Energy { get; private set; }=100;
        public float HurtTime { get; private set; }
        public float DashTime { get; private set; }
        public float AttackTime { get; private set; }
        public float AttackDuration { get; private set; }
        public WeaponId Weapon { get; private set; }
        public int Combo { get; private set; }
        public int Attacks { get; private set; }
        public int HitsTaken { get; private set; }
        public int ConfirmedHits { get; private set; }
        public float HitStopRemaining=>Mathf.Max(0,freezeUntil-Time.unscaledTime);
        public float LandingTime {get;private set;}
        public bool SkillPose {get;private set;}
        readonly HashSet<EnemyBrain> struck=new HashSet<EnemyBrain>();
        readonly HashSet<BreakableGlass> shattered=new HashSet<BreakableGlass>();
        float freezeUntil,attackBuffer,stepDistance,trailClock;
        bool resolving,swingStarted;
        float attackCooldown, dashCooldown, skillCooldown, invincible, coyote, comboWindow, jumpBuffer;
        int jumps;
        PixelActor actor;
        public void Initialize(GameDirector director)
        {
            Director=director;Tuning=director.tuning;Health=Tuning.maxHealth;
            Controller=GetComponent<CharacterController>();Controller.height=2.08f;Controller.radius=.34f;Controller.center=Vector3.up*1.06f;Controller.stepOffset=.36f;Controller.skinWidth=.035f;
            actor=GetComponent<PixelActor>();actor.Initialize();Rope=gameObject.AddComponent<RopeMotor>();Rope.Initialize(this);gameObject.layer=8;
            Weapon=WeaponId.Katana;Ammo=Tuning.magazineSize;
        }
        public void Tick(ControlFrame input,float dt)
        {
            if(Health<=0)return;
            attackBuffer=input.attack?Tuning.Timing(Weapon).buffer:Mathf.Max(0,attackBuffer-dt/Mathf.Max(.01f,Time.timeScale));
            if(HitStopRemaining>0)return;
            UpdateArsenal(input,dt);
            LandingTime=Mathf.Max(0,LandingTime-dt);
            float previousAttack=AttackTime;
            HurtTime=Mathf.Max(0,HurtTime-dt);invincible=Mathf.Max(0,invincible-dt);DashTime=Mathf.Max(0,DashTime-dt);
            AttackTime=Mathf.Max(0,AttackTime-dt);attackCooldown-=dt;dashCooldown-=dt;skillCooldown-=dt;comboWindow-=dt;
            Energy=Mathf.Min(100,Energy+dt*4f);
            var camera=Camera.main;var ray=camera.ScreenPointToRay(input.pointer);
            var plane=new Plane(Vector3.forward,new Vector3(0,0,transform.position.z));
            Aim=plane.Raycast(ray,out float distance)?ray.GetPoint(distance):Shoulder+Vector3.right*Facing;
            // The visible enemy can be on another depth lane; use the camera ray to acquire its actual 3D collider.
            if(Physics.Raycast(ray,out var aimHit,200,(1<<0)|(1<<9),QueryTriggerInteraction.Collide)&&aimHit.collider.GetComponentInParent<EnemyBrain>())Aim=aimHit.point;
            if(AttackTime<=0){if(Mathf.Abs(input.move.x)>.15f&&!input.guard)Facing=Mathf.Sign(input.move.x);else if(input.guard&&Mathf.Abs(Aim.x-transform.position.x)>.15f)Facing=Mathf.Sign(Aim.x-transform.position.x);}
            Guarding=input.guard&&Grounded&&!Rope.Attached&&AttackTime<=0;
            bool wasGrounded=Grounded;Grounded=Controller.isGrounded;
            if(Grounded&&!wasGrounded&&Velocity.y<-3){LandingTime=.13f;Director.Audio.Play("land",transform.position,.3f,1);SignalEffects.Dust(transform.position,Vector3.up,.55f);}
            if(Grounded){coyote=.11f;jumps=0;}else coyote-=dt;
            if(input.jump)jumpBuffer=.12f;else jumpBuffer-=dt;
            bool wasRope=Rope.Attached;Rope.Tick(input,dt);
            if(jumpBuffer>0&&!wasRope&&(coyote>0||jumps<1)){
                Velocity.y=Tuning.jumpSpeed;jumpBuffer=0;coyote=0;jumps++;
                SignalEffects.Burst(transform.position+Vector3.up*.1f,SignalEffects.Cyan,8,2);
                Director.Audio.Play("jump",transform.position,.18f,1);
            }
            if(input.dash&&dashCooldown<=0&&!Guarding){
                DashTime=.2f;dashCooldown=.85f;invincible=.27f;Facing=Mathf.Abs(input.move.x)>.2f?Mathf.Sign(input.move.x):Facing;
                dashAttackWindow=.27f;CancelReload();
                Velocity.x=Facing*Tuning.dashSpeed;Velocity.y=Mathf.Max(Velocity.y,0);resolving=false;AttackTime=0;Director.Audio.Play("dash",transform.position,.3f,2);SignalEffects.Dust(transform.position,Vector3.left*Facing,.6f);
            }
            if(DashTime<=0){
                if(!Rope.Attached){
                    float target=input.move.x*Tuning.moveSpeed*(Guarding?.36f:1)*(AttackTime>0&&Weapon!=WeaponId.Pistol?.45f:1);
                    Velocity.x=Mathf.MoveTowards(Velocity.x,target,(Grounded?Tuning.acceleration:13f)*dt);
                }
                Velocity.z=Mathf.MoveTowards(Velocity.z,Rope.Attached?0:input.move.y*Tuning.moveSpeed*.64f,Tuning.acceleration*dt);
                Velocity.y-= (Rope.Attached?Tuning.ropeGravity:Tuning.gravity)*dt;
                if(Grounded&&Velocity.y<0)Velocity.y=-2f;
            }
            Controller.Move(Velocity*dt);Rope.Constrain();
            if((Controller.collisionFlags&CollisionFlags.Above)!=0&&Velocity.y>0)Velocity.y=0;
            Vector3 position=transform.position;position.x=Mathf.Clamp(position.x,1,Director.stageLength-1);position.z=Mathf.Clamp(position.z,-Director.halfDepth,Director.halfDepth);transform.position=position;
            if(position.y<-9){ReceiveDamage(20,Vector3.zero,true);Respawn(Director.checkpoint,false);}
            if(resolving)ResolveSwing(previousAttack);
            if(attackBuffer>0&&!Guarding&&HurtTime<=0&&attackCooldown<=0&&!Reloading){Attack();attackBuffer=0;}
            if(input.skill&&!Guarding&&HurtTime<=0&&!Reloading&&AttackTime<=0&&Energy>=Tuning.skillCost&&skillCooldown<=0)Skill();
            actor.TickHero(this,dt);
            if(Grounded&&DashTime<=0&&AttackTime<=0){stepDistance+=new Vector2(Velocity.x,Velocity.z).magnitude*dt;if(stepDistance>2.25f){stepDistance=0;Director.Audio.Play(Director.stage==StageId.Carriage||Director.stage==StageId.Roof?"step_metal":"step_tile",transform.position,.12f,0);}}
            trailClock-=dt;if(DashTime>0&&trailClock<=0){trailClock=.055f;SignalEffects.Afterimage(actor,.13f);}
        }
        public void Attack()
        {
            if(Reloading)return;if(Weapon==WeaponId.Pistol&&Ammo<=0){BeginReload();return;}
            if(Mathf.Abs(Aim.x-transform.position.x)>.15f)Facing=Mathf.Sign(Aim.x-transform.position.x);
            Combo=comboWindow>0?(Combo+1)%3:0;comboWindow=1.05f;Attacks++;
            Action=dashAttackWindow>0?WeaponAction.Dash:WeaponAction.Combo;dashAttackWindow=0;if(Action==WeaponAction.Dash)DashAttacks++;
            currentTiming=Action==WeaponAction.Dash?(Weapon==WeaponId.Katana?Tuning.katanaDash:Weapon==WeaponId.Greatsword?Tuning.heavyDash:Tuning.pistolDash):Tuning.ComboTiming(Weapon,Combo);
            SkillPose=false;AttackDuration=currentTiming.duration;shotsInAction=0;
            AttackTime=AttackDuration;attackCooldown=AttackDuration;
            struck.Clear();shattered.Clear();resolving=true;swingStarted=false;
            if(Weapon==WeaponId.Pistol&&currentTiming.contact<=0)ResolveSwing(AttackDuration);
        }
        void ResolveSwing(float previousAttack)
        {
            var timing=ActiveTiming;float elapsed=AttackDuration-AttackTime,previous=AttackDuration-previousAttack;
            if(Action==WeaponAction.Skill){ResolveSkill(elapsed);return;}
            if(elapsed<timing.contact)return;
            if(previous>timing.activeEnd){resolving=false;return;}
            float damage=CampaignRules.Damage(Weapon,Combo,Tuning)*(Action==WeaponAction.Dash?Weapon==WeaponId.Greatsword?1.3f:1.15f:1);
            if(Weapon==WeaponId.Pistol){
                int count=Action==WeaponAction.Dash?3:1;
                while(shotsInAction<count&&elapsed>=timing.contact+shotsInAction*.075f){if(Ammo<=0)break;actor.TickHero(this,0);FirePistol(damage,false);shotsInAction++;}
                if(shotsInAction>=count||Ammo<=0)resolving=false;
            }else{
                float range=(Weapon==WeaponId.Greatsword?3.4f:2.6f)+(Action==WeaponAction.Dash?.8f:0);
                if(!swingStarted){swingStarted=true;SignalEffects.Slash(Shoulder,Facing,range,Weapon==WeaponId.Greatsword?SignalEffects.Gold:SignalEffects.Cyan,Combo);Director.Audio.Play(Weapon==WeaponId.Greatsword?"heavy_swing":"blade_swing",Shoulder,.22f,1);}
                foreach(var enemy in Director.Enemies)if(enemy&&enemy.Alive&&!struck.Contains(enemy)){
                    Vector3 d=enemy.transform.position-transform.position;
                    if(Mathf.Abs(d.z)<1.35f&&Mathf.Abs(d.y)<2.9f&&d.x*Facing>-.55f&&d.x*Facing<range){struck.Add(enemy);enemy.Damage(damage,new Vector3(Facing*(Weapon==WeaponId.Greatsword?5.5f:3.5f),0,0));OnContact(enemy,enemy.transform.position+new Vector3(-Facing*.22f,1.22f,-.08f),Vector3.right*Facing);}
                }
                foreach(var glass in Director.Glass)if(glass&&!glass.Broken&&!shattered.Contains(glass)&&Vector3.Distance(Shoulder,glass.transform.position)<range+1.1f){shattered.Add(glass);glass.Hit(damage);}
                Director.TryCoreStrike(this,range);
            }
            if(elapsed>=timing.activeEnd)resolving=false;
        }
        void OnContact(EnemyBrain enemy,Vector3 position,Vector3 direction)
        {
            ConfirmedHits++;bool armored=enemy.boss&&Director.ExposeTimer<=0;bool heavy=Weapon==WeaponId.Greatsword;
            SignalEffects.Impact(position,direction,armored?SignalEffects.Gold:SignalEffects.Cyan,heavy?1.1f:.7f);
            Director.Audio.Play(armored?"guard":heavy?"heavy_hit":"blade_hit",position,heavy?.5f:.36f,3);
            if(armored)return;float stop=ActiveTiming.hitStop+(!enemy.Alive?.018f:0);freezeUntil=Mathf.Max(freezeUntil,Time.unscaledTime+stop);enemy.React(ActiveTiming.stagger,stop,direction);
            Director.CameraRig.Impact(direction,heavy?.1f:.045f);
        }
        public void Skill()
        {
            if(Energy<Tuning.skillCost||Reloading)return;
            if(Mathf.Abs(Aim.x-transform.position.x)>.15f)Facing=Mathf.Sign(Aim.x-transform.position.x);
            Energy-=Tuning.skillCost;skillCooldown=4f;SkillsUsed++;Action=WeaponAction.Skill;
            currentTiming=Weapon==WeaponId.Katana?Tuning.katanaSkill:Weapon==WeaponId.Greatsword?Tuning.heavySkill:Tuning.pistolSkill;
            AttackDuration=currentTiming.duration;AttackTime=attackCooldown=AttackDuration;SkillPose=true;resolving=true;swingStarted=false;shotsInAction=0;struck.Clear();shattered.Clear();
        }
        public void ReceiveDamage(float amount,Vector3 source,bool ignoreInvulnerability=false)
        {
            if(Health<=0||(!ignoreInvulnerability&&invincible>0))return;
            bool blocked=Guarding&&(source.x-transform.position.x)*Facing>=0;
            if(blocked){amount*=.15f;Energy=Mathf.Max(0,Energy-8);SignalEffects.Ring(Shoulder+Vector3.right*Facing*.55f,SignalEffects.Gold,1.2f,.2f);}
            else {HurtTime=.28f;Rope.Release();resolving=false;AttackTime=0;CancelReload();Director.CameraRig.Impact((transform.position-source).normalized,.09f);Velocity.x=(transform.position.x>=source.x?1:-1)*4.5f;}
            Health=Mathf.Max(0,Health-amount);invincible=.65f;HitsTaken++;Director.Audio.Play(blocked?"guard":"hurt",Shoulder,.42f,3);
            if(Health<=0)Director.Die();
        }
        public void Heal(float amount){Health=Mathf.Min(Tuning.maxHealth,Health+amount);Energy=Mathf.Min(100,Energy+12);}
        public void Respawn(Vector3 point,bool fullHeal=true)
        {
            Rope.Release();Controller.enabled=false;transform.position=point;Controller.enabled=true;Velocity=Vector3.zero;
            if(fullHeal){Health=Tuning.maxHealth;Ammo=Tuning.magazineSize;}invincible=1.5f;HurtTime=0;AttackTime=0;resolving=false;freezeUntil=attackBuffer=0;CancelReload();pendingWeapon=-1;dashAttackWindow=0;
        }
    }
}
