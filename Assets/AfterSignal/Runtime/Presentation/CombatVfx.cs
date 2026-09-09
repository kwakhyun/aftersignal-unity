using UnityEngine;
namespace AfterSignal
{
    public static class CombatVfx
    {
        static Material material;static float flashAt;
        public static int Impacts{get;private set;}public static int Muzzles{get;private set;}
        public static Material ParticleMaterial=>material?material:material=new Material(Resources.Load<Shader>("Shaders/CombatParticle"));
        static bool Near(Vector3 at,float range=180)=>PresentationSettings.Effects>0&&EffectBudget.Active<90&&(!Camera.main||(Camera.main.transform.position-at).sqrMagnitude<range*range);
        public static ParticleSystem Emit(string name,Vector3 at,Vector3 direction,Color color,int count,float speed,float size,float life,bool smoke=false)
        {
            if(!Near(at))return null;
            var go=new GameObject(name,typeof(EffectBudget),typeof(ParticleSystem));go.transform.position=at;go.transform.rotation=Quaternion.LookRotation(direction.sqrMagnitude>.001f?direction:Vector3.up);
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=.1f;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.55f,life);main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.35f,speed);main.startSize=new ParticleSystem.MinMaxCurve(size*.35f,size);main.startColor=color;main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=smoke?-.03f:.7f;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=smoke?35:24;shape.radius=.06f;
            var emission=ps.emission;emission.enabled=false;
            var fade=ps.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(smoke?Color.white:new Color(.6f,.28f,.08f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            var grow=ps.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(1,smoke?3.5f:.2f)));
            if(smoke){var noise=ps.noise;noise.enabled=true;noise.strength=.35f;noise.frequency=.9f;noise.scrollSpeed=.45f;}
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=ParticleMaterial;renderer.renderMode=smoke?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;renderer.lengthScale=smoke?1:1.8f;renderer.velocityScale=.04f;
            ps.Emit(Mathf.CeilToInt(count*Mathf.Max(.3f,PresentationSettings.Effects)));Object.Destroy(go,life+.4f);return ps;
        }
        public static Vector3 Heading(Vector3 muzzle)
        {
            var g=GameDirector.Instance;if(!g)return Vector3.forward;
            if((muzzle-g.Player.Muzzle).sqrMagnitude<1.5f)return(g.Player.Aim-muzzle).normalized;
            WorldActor source=null;float closest=12;
            foreach(var a in WorldActor.All){if(!a||!a.Alive)continue;float d=(a.Center-muzzle).sqrMagnitude;if(d<closest){closest=d;source=a;}}
            if(source){var gang=source.GetComponent<GangMember>();var officer=source.GetComponent<PoliceOfficer>();var opponent=gang?gang.Target:officer?officer.GangTarget:FactionCombat.NearestOpponent(source,160);if(opponent)return(opponent.Center-muzzle).normalized;if(source.gang||WantedSystem.Level>0)return(g.Player.Shoulder-muzzle).normalized;}
            return Camera.main?Camera.main.transform.forward:Vector3.forward;
        }
        public static void Muzzle(Vector3 at,GunshotKind kind)
        {
            if(!Near(at))return;
            Muzzles++;var direction=Heading(at);float scale=kind==GunshotKind.Shotgun?1.3f:kind==GunshotKind.Automatic?1.1f:kind==GunshotKind.Rifle?.8f:.55f;
            Emit("Muzzle / hot gas petals",at,direction,new Color(1,.7f,.25f),8,5,.32f*scale,.055f);
            Emit("Muzzle / residual smoke",at,direction,new Color(.48f,.5f,.53f,.3f),3,.5f,.18f*scale,.7f,true);
            Emit("Ejected brass",at-Vector3.up*.06f,Quaternion.Euler(0,85,0)*direction,new Color(.66f,.45f,.18f),1,2.5f,.065f,.48f);
            if(Near(at,35)&&Time.time>flashAt){flashAt=Time.time+.035f;var go=new GameObject("Muzzle light",typeof(Light));go.transform.position=at;var l=go.GetComponent<Light>();l.range=4*scale;l.intensity=2.5f;l.color=new Color(1,.6f,.25f);l.shadows=LightShadows.None;Object.Destroy(go,.045f);}
            var g=GameDirector.Instance;if(g&&(at-g.Player.Muzzle).sqrMagnitude<1.5f)CombatCameraImpulse.Recoil(kind);
        }
        public static void Tracer(Vector3 from,Vector3 to,Color color)
        {
            if(!Near(from,250))return;var go=new GameObject("Moving ballistic tracer",typeof(LineRenderer),typeof(CombatTracer),typeof(EffectBudget));
            var line=go.GetComponent<LineRenderer>();line.sharedMaterial=Resources.Load<Material>("Materials/GoldFX");line.positionCount=2;line.startColor=new Color(1,.8f,.4f,.8f);line.endColor=new Color(1,.38f,.08f,0);line.widthMultiplier=.025f;line.numCapVertices=2;go.GetComponent<CombatTracer>().Initialize(from,to);
        }
        public static void Hit(RaycastHit hit,Vector3 direction)
        {
            Impacts++;bool person=hit.collider.GetComponentInParent<WorldActor>()||hit.collider.GetComponentInParent<PlayerMotor>();bool metal=hit.collider.GetComponentInParent<CityVehicle>()||hit.collider.GetComponentInParent<CombatRobot>();bool glass=hit.collider.GetComponentInParent<FacadeGlass>()||hit.collider.GetComponentInParent<BreakableGlass>();
            Emit(metal?"Armor ricochet":person?"Flesh impact":"Surface impact",hit.point+hit.normal*.025f,hit.normal,metal?new Color(1,.7f,.22f):person?new Color(.45f,.015f,.025f):glass?new Color(.65f,.88f,.94f):new Color(.58f,.52f,.43f),metal?18:9,metal?7:3,person?.095f:.07f,.4f);
            if(!person)Emit("Impact dust",hit.point,hit.normal,new Color(.4f,.4f,.38f,.45f),5,.9f,.18f,.8f,true);
            GameDirector.Instance?.Audio.Play(metal?"impact_metal":person?"impact_flesh":glass?"impact_glass":"impact_concrete",hit.point,.24f,3);
        }
        public static void Explosion(Vector3 at,float radius)
        {
            Emit("Blast / ground pressure dust",at+Vector3.up*.2f,Vector3.up,new Color(.35f,.31f,.25f,.55f),45,radius*4,radius*.35f,2.5f,true);
            var debris=Emit("Blast / incandescent fragments",at+Vector3.up,Vector3.up,new Color(1,.57f,.13f),32,radius*5,.15f,1.5f);
            if(debris){var shape=debris.shape;shape.angle=80;var trails=debris.trails;trails.enabled=true;trails.ratio=.35f;trails.lifetime=.18f;trails.widthOverTrail=new ParticleSystem.MinMaxCurve(.035f);debris.GetComponent<ParticleSystemRenderer>().trailMaterial=ParticleMaterial;}
            CombatCameraImpulse.Blast(at,radius);
            GameDirector.Instance?.Audio.Play("blast_pressure",at,.42f,4);
        }
    }
    public sealed class CombatTracer:MonoBehaviour
    {
        Vector3 start,end;float age,distance;LineRenderer line;
        public void Initialize(Vector3 a,Vector3 b){start=a;end=b;distance=Vector3.Distance(a,b);line=GetComponent<LineRenderer>();line.SetPosition(0,a);line.SetPosition(1,a);}
        void Update(){if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;age+=Time.deltaTime;float front=Mathf.Min(distance,age*900);var direction=(end-start).normalized;line.SetPosition(0,start+direction*Mathf.Max(0,front-9));line.SetPosition(1,start+direction*front);if(age>Mathf.Max(.055f,distance/900))Destroy(gameObject);}
    }
    [DefaultExecutionOrder(1250)]
    public sealed class CombatCameraImpulse:MonoBehaviour
    {
        Vector2 recoil,velocity;float shock;Vector3 origin;float clock;
        static CombatCameraImpulse Get(){var c=Camera.main;return c?c.GetComponent<CombatCameraImpulse>()??c.gameObject.AddComponent<CombatCameraImpulse>():null;}
        public static void Recoil(GunshotKind kind){var c=Get();if(!c)return;float force=kind==GunshotKind.Shotgun?2.4f:kind==GunshotKind.Rifle?.7f:kind==GunshotKind.Automatic?.55f:1.25f;c.recoil+=new Vector2(-force,Random.Range(-.3f,.3f)*force);c.recoil=Vector2.ClampMagnitude(c.recoil,5);}
        public static void Blast(Vector3 at,float radius){var c=Get();if(!c)return;float distance=Vector3.Distance(c.transform.position,at);c.shock=Mathf.Min(1.5f,c.shock+Mathf.Clamp01(1-distance/(radius*28))*radius*.12f);c.origin=at;}
        void LateUpdate()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);clock+=dt;recoil=Vector2.SmoothDamp(recoil,Vector2.zero,ref velocity,.14f,100,dt);shock=Mathf.MoveTowards(shock,0,dt*1.4f);
            float gain=PresentationSettings.Motion;if(g.CameraRig.ReducedMotion||gain<=0)return;
            var wave=new Vector3(Mathf.Sin(clock*37),Mathf.Sin(clock*29)*.65f,Mathf.Sin(clock*19)*.45f)*shock*gain;
            transform.rotation*=Quaternion.Euler(recoil.x*gain+wave.x,recoil.y*gain+wave.y,wave.z);
            transform.position+=Vector3.ClampMagnitude((transform.position-origin).normalized*shock*.08f+transform.up*wave.y*.025f,.12f);
        }
    }
}
