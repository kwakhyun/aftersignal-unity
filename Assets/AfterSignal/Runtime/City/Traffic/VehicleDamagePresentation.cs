using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleDamagePresentation:MonoBehaviour
    {
        CityVehicle car;ParticleSystem smoke,fire;Light glow;float next;
        public int Stage {get;private set;}
        void Awake()=>car=GetComponent<CityVehicle>();
        void Update()
        {
            var g=GameDirector.Instance;if(!car||!g||g.Blocked||Time.time<next)return;next=Time.time+.3f;
            Stage=car.HealthFraction>.75f?0:car.HealthFraction>.5f?1:car.HealthFraction>.25f?2:car.Wrecked?4:3;
            bool near=(transform.position-g.Player.transform.position).sqrMagnitude<650*650;
            if(Stage>=2&&near&&!smoke)Build();
            Set(smoke,near&&Stage>=2,Stage>=3?16:7);
            bool quenched=GetComponent<ExtinguishedObject>();
            Set(fire,near&&Stage>=3&&!quenched,Stage>=4?20:12);
            if(Stage==3&&!quenched&&!GetComponent<BurningObject>())CityFireService.Ignite(gameObject,transform.position+Vector3.up*1.4f,car.IsHeavy?2:1);
            if(glow){glow.enabled=near&&Stage>=3&&!quenched;glow.intensity=1.5f+Mathf.PerlinNoise(Time.time*8,0);}
        }
        static void Set(ParticleSystem p,bool enabled,float rate){if(!p)return;var e=p.emission;e.enabled=enabled;e.rateOverTime=rate;}
        void Build()
        {
            float size=car.GetComponent<AuthoredCraft>()?9:car.IsAircraft?3:car.IsHeavy?1.5f:1;
            var at=new Vector3(car.HalfLength*(car.IsAircraft?-.25f:.6f),car.IsAircraft?2:1.2f,0);
            smoke=Emitter(transform,"Engine smoke",at,size,false);fire=Emitter(transform,"Engine fire",at,size,true);
            glow=fire.gameObject.AddComponent<Light>();glow.color=new Color(1,.23f,.025f);glow.range=5*size;glow.shadows=LightShadows.None;
        }
        public static ParticleSystem Emitter(Transform parent,string title,Vector3 at,float size,bool flame)
        {
            var go=new GameObject(title);go.transform.SetParent(parent,false);go.transform.localPosition=at;
            var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=flame?64:100;main.startLifetime=flame?.7f:4;main.startSpeed=(flame?1.2f:.8f)*size;main.startSize=size*(flame?.6f:1.2f);main.gravityModifier=-.07f;
            main.startColor=flame?new Color(1,.32f,.025f,.8f):new Color(.11f,.12f,.13f,.65f);
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18;shape.radius=size*.3f;go.transform.localRotation=Quaternion.Euler(-90,0,0);
            var fade=p.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(flame?new Color(1,.12f,0):Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.15f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var grow=p.sizeOverLifetime;grow.enabled=true;grow.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,flame?1:.4f,1,flame?.1f:2.5f));
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("Materials/VehicleSmoke");p.Play();return p;
        }
    }
}
