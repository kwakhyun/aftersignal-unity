using UnityEngine;
namespace AfterSignal
{
    public sealed class RiftCreature:MonoBehaviour
    {
        public WorldActor Body {get;private set;}
        CharacterController controller;Transform form;readonly PursuitPath path=new();Transform[] limbs;int kind;float cooldown,phase,gravity;
        static Material shell,vein;
        public static RiftCreature Create(Vector3 at,int kind)
        {
            var go=new GameObject("잔향체 / "+new[]{"기억을 더듬는 자","기록 포식자","공허의 목격자"}[kind],typeof(CharacterController),typeof(WorldActor),typeof(RiftCreature));go.layer=9;go.transform.position=at;
            var c=go.GetComponent<RiftCreature>();c.kind=kind;c.Body=go.GetComponent<WorldActor>();c.Body.monster=true;c.Body.health=150+kind*70;
            c.controller=go.GetComponent<CharacterController>();c.controller.radius=kind==1?.7f:.5f;c.controller.height=2.7f;c.controller.center=Vector3.up*1.35f;c.controller.stepOffset=.3f;
            c.Build();return c;
        }
        void Build()
        {
            if(!shell){shell=new Material(Resources.Load<Material>("Materials/DarkMetal"));shell.SetColor("_BaseColor",new Color(.025f,.025f,.05f));shell.SetFloat("_Metallic",.75f);shell.SetFloat("_Smoothness",.65f);vein=new Material(Resources.Load<Material>("Materials/CyanFX"));vein.SetColor("_BaseColor",new Color(.56f,.1f,1));vein.EnableKeyword("_EMISSION");vein.SetColor("_EmissionColor",new Color(1.3f,.15f,3.3f));}
            form=new GameObject("Fractured organic chassis").transform;form.SetParent(transform,false);
            Shape("Obsidian rib cage",new Vector3(0,1.5f,0),new Vector3(kind==1?1.8f:.9f,1.8f,.7f),shell,PrimitiveType.Capsule);
            Shape("Featureless memory mask",new Vector3(0,2.7f,.25f),new Vector3(.58f,.8f,.42f),shell,PrimitiveType.Sphere);
            Shape("Broken signal eye",new Vector3(0,2.77f,.47f),new Vector3(.46f,.055f,.06f),vein);
            Shape("Rupture core",new Vector3(0,1.67f,.43f),new Vector3(.22f,.6f,.12f),vein,PrimitiveType.Sphere);
            for(int i=0;i<8;i++)
            {var spine=Shape("Crystalline spinal plate",new Vector3(0,.85f+i*.21f,-.3f),new Vector3(.65f+i*.035f,.055f,.7f),i%3==0?vein:shell);spine.localRotation=Quaternion.Euler(-24,0,(i%2==0?1:-1)*15);}
            limbs=new Transform[kind==2?6:4];
            for(int i=0;i<limbs.Length;i++)
            {
                int side=i%2==0?-1:1;var p=new GameObject("Articulated shard limb "+i).transform;p.SetParent(form,false);p.localPosition=new Vector3(side*.45f,i<2?1.9f:.65f,i/2*.2f-.2f);limbs[i]=p;
                var upper=Shape("Tapered appendage",new Vector3(side*.42f,-.33f,0),new Vector3(.19f,1.05f,.21f),shell,PrimitiveType.Capsule,p);upper.localRotation=Quaternion.Euler(0,0,side*48);
                var lower=Shape("Hooked talon",new Vector3(side*.83f,-.92f,.2f),new Vector3(.09f,.95f,.14f),shell,PrimitiveType.Capsule,p);lower.localRotation=Quaternion.Euler(-22,0,-side*15);
                Shape("Nerve filament",new Vector3(side*.7f,-.51f,.11f),new Vector3(.05f,.75f,.055f),vein,PrimitiveType.Capsule,p);
            }
        }
        Transform Shape(string name,Vector3 at,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube,Transform parent=null)
        {var p=GameObject.CreatePrimitive(type);p.name=name;p.transform.SetParent(parent?parent:form,false);p.transform.localPosition=at;p.transform.localScale=size;Destroy(p.GetComponent<Collider>());p.GetComponent<Renderer>().sharedMaterial=material;return p.transform;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.05f,Time.deltaTime);phase+=dt;cooldown-=dt;
            if(!Body.Alive){form.localScale=Vector3.Lerp(form.localScale,Vector3.zero,dt*1.8f);return;}
            var target=FactionCombat.NearestOpponent(Body,65);Vector3 goal=target?target.Center:g.Player.Shoulder;
            if(target&&(goal-Body.Center).sqrMagnitude>(g.Player.Shoulder-Body.Center).sqrMagnitude*1.8f)goal=g.Player.Shoulder;
            var d=goal-Body.Center;d.y=0;bool near=d.magnitude<2.7f;
            gravity=controller.isGrounded?-2:gravity-dt*22;
            controller.Move((near?Vector3.zero:path.Direction(transform.position,goal)*(kind==1?2.3f:3.5f))*dt+Vector3.up*gravity*dt);
            if(d.sqrMagnitude>.1f)form.rotation=Quaternion.Slerp(form.rotation,Quaternion.LookRotation(d),dt*6);
            for(int i=0;i<limbs.Length;i++)limbs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase*7+i*2)*24,0,Mathf.Sin(phase*4+i)*12+(near&&cooldown<.3f?35:0));
            form.localPosition=Vector3.up*(kind==2?.4f+Mathf.Sin(phase*2)*.18f:Mathf.Sin(phase*7)*.04f);
            if(near&&cooldown<=0&&FactionCombat.Visible(Body.Center,goal,4))
            {
                cooldown=kind==1?1.1f:.75f;SignalEffects.Slash(Body.Center,d.normalized,2.3f,new Color(.66f,.15f,1),kind);
                if(target&&Vector3.Distance(goal,target.Center)<.4f)target.Damage(kind==1?25:16,d.normalized*6,Body);
                else g.Player.ReceiveDamage(kind==1?23:14,transform.position);
                g.Audio.Play("heavy_hit",Body.Center,.32f,2);
            }
        }
    }
}
