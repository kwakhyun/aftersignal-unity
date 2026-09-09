using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RiftCreature:MonoBehaviour
    {
        public WorldActor Body {get;private set;}
        public Vector3 AimCenter=>transform.position+Vector3.up*(kind==0?6.7f:4.7f);
        public float Armour=>windup>0||vulnerable>0?.82f:.32f;
        public int LaserShots {get;private set;}
        public int Shockwaves {get;private set;}
        public bool Campaign;
        public float MaximumHealth{get;private set;}
        public string AttackWarning=>windup>0?skill==3?"전방위 소각 광선 · 즉시 엄폐 / 거리 이탈":skill==2?"집중 광선 · 엄폐하세요":skill==1?"광역 지반 붕괴 · 범위 이탈":"충격파 · 거리를 벌리세요":"";
        public WorldActor PriorityTarget=>target;
        struct Threat{public WorldActor actor;public float damage,time;}
        readonly Dictionary<int,Threat> threats=new();float playerThreat,playerThreatAt=-100;
        public void Attacked(WorldActor source,float damage)
        {
            if(source&&source.environmental)return;
            if(!source){playerThreat+=damage;playerThreatAt=Time.time;}else{int id=source.GetInstanceID();threats.TryGetValue(id,out var t);t.actor=source;t.damage=Time.time-t.time>18?damage:t.damage+damage;t.time=Time.time;threats[id]=t;}scan=0;
        }
        CharacterController controller;Transform form;readonly List<Transform> limbs=new();readonly List<Quaternion> rests=new();Transform head,thorax;
        int kind,skill;float cooldown=4,phase,gravity,scan,windup,vulnerable,laserAt=8,barrageAt=18,deathAge;Vector3 goal,locked,origin,home;WorldActor target;CityVehicle airTarget;
        static readonly Dictionary<string,Material> materials=new();
        void Start(){MaximumHealth=Body.health;SecurityResponse.Request(Body,true);}
        public static RiftCreature Create(Vector3 at,int kind)
        {
            kind=Mathf.Abs(kind)%3;var go=new GameObject("잠식 거신 / "+new[]{"잔향의 도살자","철갑 포식자","공허의 예언자"}[kind],typeof(CharacterController),typeof(WorldActor),typeof(RiftCreature));go.layer=9;go.transform.position=at;
            var c=go.GetComponent<RiftCreature>();c.kind=kind;c.home=at;c.Body=go.GetComponent<WorldActor>();c.Body.monster=true;c.Body.health=new[]{14000,21000,16000}[kind];
            c.controller=go.GetComponent<CharacterController>();c.controller.radius=2.8f;c.controller.height=kind==0?10.5f:7.2f;c.controller.center=Vector3.up*c.controller.height*.5f;c.controller.stepOffset=.5f;c.controller.slopeLimit=45;
            c.MaximumHealth=c.Body.health;c.Body.Titan=c;c.Build();go.AddComponent<TitanHealthDisplay>();return c;
        }
        void Build()
        {
            var prefab=Resources.Load<GameObject>("Creatures/"+new[]{"RiftReaver","RiftBehemoth","RiftOracle"}[kind]);
            if(!prefab){Debug.LogError("Missing authored rift titan model");return;}
            form=new GameObject("Titan facing and articulation").transform;form.SetParent(transform,false);
            var model=Instantiate(prefab,form).transform;model.localPosition=Vector3.zero;
            // Preserve Blender Z-up to Unity Y-up conversion on the imported model root.
            model.localRotation=Quaternion.Euler(0,180,0)*model.localRotation;
            foreach(var r in form.GetComponentsInChildren<Renderer>())
            {
                var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)
                {
                    string key=slots[i]?slots[i].name.Split('.')[0]:"RiftPlate";
                    if(!materials.TryGetValue(key,out var mat))
                    {
                        mat=new Material(Resources.Load<Material>("Materials/DarkMetal"));mat.name=key;bool core=key.Contains("Core");
                        var color=core?new Color(.38f,.035f,.6f):key.Contains("Bone")?new Color(.55f,.47f,.38f):key.Contains("Hide")?new Color(.14f,.09f,.15f):new Color(.19f,.26f,.29f);
                        mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",core?.1f:.35f);mat.SetFloat("_Smoothness",key.Contains("Hide")?.25f:.42f);mat.enableInstancing=true;
                        if(core){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*.8f);}else{mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}materials[key]=mat;
                    }
                    slots[i]=mat;
                }
                r.sharedMaterials=slots;
            }
            foreach(var t in form.GetComponentsInChildren<Transform>())
            {if(t.name.StartsWith("Limb_")||t.name.StartsWith("Arm_")||t.name=="Tail"){limbs.Add(t);rests.Add(t.localRotation);}if(t.name=="Cranium")head=t;if(t.name=="Thorax")thorax=t;}
        }
        void ChooseTargets(GameDirector g)
        {
            target=null;float best=230*230;
            foreach(var a in WorldActor.All)
            {
                if(!a||!a.Alive||a.Downed||a.monster||a.environmental||!a.enabled)continue;
                float d=(a.Center-AimCenter).sqrMagnitude*(a.police||a.military?1.4f:.65f);
                if(d<best&&BlastDamage.Exposed(AimCenter,a.Center,a.transform)){target=a;best=d;}
            }
            goal=target?target.transform.position:(g.Player.transform.position-transform.position).sqrMagnitude<180*180?g.Player.transform.position:home;
            airTarget=null;var sim=UrbanSimulation.Instance;
            if(sim)foreach(var car in sim.Cars)
                if(car&&!car.Wrecked&&car.IsAircraft&&car.transform.position.y-transform.position.y>14&&(car.transform.position-AimCenter).sqrMagnitude<480*480&&(car.occupied||car==sim.Current)&&(!airTarget||(car.transform.position-AimCenter).sqrMagnitude<(airTarget.transform.position-AimCenter).sqrMagnitude))airTarget=car;
            float score=Time.time-playerThreatAt<18?playerThreat/(1+(Time.time-playerThreatAt)*.2f):0;
            bool player=score>0;WorldActor aggressor=null;
            foreach(var pair in threats)
            {
                var t=pair.Value;if(!t.actor||!t.actor.Alive||t.actor.Downed||Time.time-t.time>18||(t.actor.Center-AimCenter).sqrMagnitude>650*650)continue;
                float value=t.damage/(1+(Time.time-t.time)*.2f);if(value>score){score=value;aggressor=t.actor;player=false;}
            }
            if(player){target=null;goal=g.Player.transform.position;airTarget=sim&&sim.Current&&sim.Current.IsAircraft?sim.Current:null;}
            else if(aggressor){target=aggressor;goal=aggressor.transform.position;airTarget=aggressor.GetComponentInParent<CityVehicle>();if(airTarget&&!airTarget.IsAircraft)airTarget=null;}
            if(threats.Count>128)threats.Clear();
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!form)return;float dt=Mathf.Min(.05f,Time.deltaTime);phase+=dt;cooldown-=dt;laserAt-=dt;barrageAt-=dt;scan-=dt;vulnerable-=dt;
            if(!Body.Alive){controller.enabled=false;deathAge+=dt;form.localRotation=Quaternion.Slerp(form.localRotation,Quaternion.Euler(0,form.localEulerAngles.y,78),dt*.8f);form.localPosition=Vector3.down*Mathf.Min(2,deathAge*.3f);if(deathAge>12)Destroy(gameObject);return;}
            if(scan<=0){scan=.6f;ChooseTargets(g);}
            var d=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up);bool moving=d.magnitude>10&&windup<=0;
            gravity=controller.isGrounded?-2:Mathf.Max(-38,gravity-dt*20);
            Vector3 direction=d.normalized;
            if(moving&&Physics.SphereCast(transform.position+Vector3.up*3,2.5f,direction,out var wall,8,1,QueryTriggerInteraction.Ignore))direction=Vector3.ProjectOnPlane(direction,wall.normal).normalized;
            controller.Move((moving?direction*(kind==1?5.5f:8):Vector3.zero)*dt+Vector3.up*gravity*dt);
            if(d.sqrMagnitude>1&&windup<=0)form.rotation=Quaternion.Slerp(form.rotation,Quaternion.LookRotation(d),dt*2);
            for(int i=0;i<limbs.Count;i++)limbs[i].localRotation=rests[i]*Quaternion.Euler(moving?Mathf.Sin(phase*4.6f+i*Mathf.PI)*19:Mathf.Sin(phase+i)*3,0,windup>0?Mathf.Sin((2.2f-windup)*2)*14:0);
            if(windup>0)
            {
                windup-=dt;
                if(skill==2){origin=head?head.position+form.forward*2:AimCenter+form.forward*3;SignalEffects.Beam(origin,locked,new Color(.75f,.1f,1,.5f),.035f,.07f);}
                if(windup<=0){vulnerable=3;if(skill==3)TitanBarrage.Fire(this);else if(skill==2)FireLaser();else Shockwave(skill==1?25:17,skill==1?100:80);cooldown=4.5f;}
                return;
            }
            if(barrageAt<=0&&d.magnitude<65){skill=3;windup=2.8f;barrageAt=27;SignalEffects.Ring(transform.position+Vector3.up*.2f,new Color(.85f,.1f,1),48,windup);g.ToastNear("잠식체 전방위 소각 예고 · 48m 밖으로 이탈하거나 건물 뒤로 엄폐!",transform.position,100,3);g.Audio.Play("charge",AimCenter,.5f,3);return;}
            if(laserAt<=0&&(airTarget||target&&target.helicopter||d.magnitude>26&&d.magnitude<180))
            {skill=2;windup=2.2f;laserAt=kind==2?9:14;locked=airTarget?airTarget.transform.position:target?target.Center:g.Player.Shoulder;g.Audio.Play("charge",AimCenter,.5f,3);return;}
            if(cooldown<=0&&d.magnitude<26)
            {skill=Random.value<.4f?1:0;windup=skill==1?1.7f:1.05f;SignalEffects.Ring(transform.position+Vector3.up*.15f,new Color(.9f,.13f,.36f),skill==1?25:17,windup);g.Audio.Play("heavy_hit",AimCenter,.42f,3);}
        }
        public void FireLaser()
        {
            LaserShots++;Vector3 from=head?head.position+form.forward*2:AimCenter;var ray=locked-from;var end=locked;
            if(Ballistics.Cast(from,ray.normalized,Mathf.Min(500,ray.magnitude+8),transform,out var hit,true))
            {
                end=hit.point;var vehicle=hit.collider.GetComponentInParent<CityVehicle>();var actor=hit.collider.GetComponentInParent<WorldActor>();
                if(vehicle)vehicle.Damage(WarheadDamage.Against(vehicle,BlastPayload.Missile,1400),end,Body);
                if(actor&&!actor.monster)actor.Damage(actor.helicopter?2800:Mathf.Max(220,actor.MaxHealth*2),ray.normalized*10,Body);
                hit.collider.GetComponentInParent<PlayerMotor>()?.ReceiveDamage(36,from);
            }
            SignalEffects.Beam(from,end,new Color(.58f,.17f,1),.52f,.65f);SignalEffects.Beam(from,end,new Color(.88f,.7f,1),.17f,.65f);VehicleExplosion.Create(end,2.3f);TitanImpact.Create(end,9);TitanDemolition.Strike(end,12,Body);GameDirector.Instance.Audio.Play("urban_explosion",end,.38f,3);
        }
        public void Shockwave(float radius,float damage)
        {
            TitanDemolition.Strike(transform.position+Vector3.up*5,radius+12,Body);
            Shockwaves++;var at=transform.position;TitanImpact.Create(at,radius);SignalEffects.Ring(at+Vector3.up*.3f,new Color(.75f,.17f,1),radius,.8f);VehicleExplosion.Create(at+Vector3.up,2.4f);
            foreach(var a in WorldActor.All.ToArray())
            {
                if(!a||!a.Alive||a.monster||a.helicopter||(a.transform.position-at).sqrMagnitude>radius*radius||!BlastDamage.Exposed(at+Vector3.up*3,a.Center,a.transform))continue;
                var d=(a.Center-AimCenter).normalized;float falloff=Mathf.InverseLerp(radius,0,Vector3.Distance(a.transform.position,at));a.Damage(falloff>.6f?Mathf.Max(damage,a.MaxHealth*2):damage*Mathf.Lerp(.18f,.8f,falloff),d*22,Body);if(!a.protectedResident)CivilianImpact.Launch(a,d,20);
                NpcSpeech.Say(a,NpcDialogueBank.Line(a.GetComponent<CityNpc>(),"monster"),4,8);
            }
            var g=GameDirector.Instance;if((g.Player.transform.position-at).sqrMagnitude<radius*radius&&BlastDamage.Exposed(at+Vector3.up*3,g.Player.Shoulder,g.Player.transform))g.Player.ReceiveDamage(28,at);
            var sim=UrbanSimulation.Instance;if(sim)foreach(var car in sim.Cars)if(car&&!car.Wrecked&&Vector3.Distance(WarheadDamage.HullPoint(car,at),at)<radius)car.Damage(car.MaxHealth*.13f,at,Body);
        }
    }
}
