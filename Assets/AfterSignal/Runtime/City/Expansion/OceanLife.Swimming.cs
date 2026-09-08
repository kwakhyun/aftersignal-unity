using UnityEngine;
namespace AfterSignal
{
    public sealed partial class OceanLife
    {
        public static float Oxygen{get;private set;}=90;
        static float originalHeight;static Vector3 originalCenter;static float strokeClock,hurtClock;
        AudioSource underwater;ParticleSystem bubbles;Transform waterVeil;Material waterVeilMaterial;
        static void EnterWater(PlayerMotor p)
        {
            if(Swimming)return;Swimming=true;originalHeight=p.Controller.height;originalCenter=p.Controller.center;
            p.Controller.height=.72f;p.Controller.center=Vector3.up*.1f;p.Director.Audio.Play("waterstroke",p.transform.position,.23f,1);
        }
        static void ExitWater(PlayerMotor p)
        {
            if(!Swimming)return;Swimming=false;
            if(p.Controller){p.Controller.height=originalHeight;p.Controller.center=originalCenter;}
        }
        static void WaterSurvival(PlayerMotor p,ControlFrame input,float dt)
        {
            var at=p.transform.position;bool deep=at.y<Surface-.8f;
            Oxygen=Mathf.Clamp(Oxygen+(deep?-dt:dt*14),0,90);
            hurtClock-=dt;if(Oxygen<=0&&hurtClock<=0){hurtClock=1.2f;p.ReceiveDamage(7,at,true);}
            strokeClock-=dt;if(strokeClock<=0&&p.Velocity.magnitude>.8f){strokeClock=input.boost?.55f:.85f;p.Director.Audio.Play("waterstroke",at,deep?.06f:.15f,0);}
            // Swim to an edge and hold Space to climb onto a clear quay or beach.
            if(input.vertical>0&&at.y>Surface-.5f)
            {
                var d=p.Director.CameraRig.MoveDirection(input.move);if(d.sqrMagnitude<.1f)d=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized;
                var cast=at+d.normalized*1.7f+Vector3.up*3.5f;
                if(Physics.Raycast(cast,Vector3.down,out var hit,4,1,QueryTriggerInteraction.Ignore)&&hit.normal.y>.75f&&!hit.collider.GetComponentInParent<CityVehicle>()&&hit.point.y>Surface+.1f)
                {
                    var landing=hit.point+Vector3.up*.1f;
                    if(!Physics.CheckCapsule(landing+Vector3.up*.4f,landing+Vector3.up*1.6f,.32f,1,QueryTriggerInteraction.Ignore))
                    {ExitWater(p);p.Controller.enabled=false;p.transform.position=landing;p.Controller.enabled=true;p.Velocity=Vector3.zero;}
                }
            }
        }
        void InitializeWaterAudio()
        {
            if(Camera.main){var data=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.requiresDepthTexture=true;data.requiresColorTexture=true;}
            var veil=GameObject.CreatePrimitive(PrimitiveType.Quad);veil.name="Underwater distance absorption";veil.layer=2;Destroy(veil.GetComponent<Collider>());waterVeil=veil.transform;waterVeil.SetParent(Camera.main.transform,false);
            waterVeilMaterial=new Material(Resources.Load<Shader>("Shaders/UnderwaterHaze"));var vr=veil.GetComponent<MeshRenderer>();vr.sharedMaterial=waterVeilMaterial;vr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;vr.receiveShadows=false;veil.SetActive(false);
            underwater=gameObject.AddComponent<AudioSource>();underwater.clip=Resources.Load<AudioClip>("Audio/Transport/underwater");underwater.loop=true;underwater.spatialBlend=0;underwater.volume=0;if(underwater.clip)underwater.Play();
            var go=new GameObject("Underwater exhalation bubbles");go.transform.SetParent(transform,false);bubbles=go.AddComponent<ParticleSystem>();bubbles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=bubbles.main;main.startLifetime=2.8f;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.09f);main.startSpeed=.3f;main.gravityModifier=-.045f;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=100;main.startColor=new Color(.5f,.88f,.94f,.42f);
            var emission=bubbles.emission;emission.rateOverTime=0;var shape=bubbles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.1f;
            var render=bubbles.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=Resources.Load<Material>("Materials/CyanFX");bubbles.Play();
        }
        void SetWaterVeil(bool deep)
        {
            if(!waterVeil)return;waterVeil.gameObject.SetActive(deep);if(!deep)return;
            var cam=Camera.main;float near=cam.nearClipPlane+.08f,half=Mathf.Tan(cam.fieldOfView*.5f*Mathf.Deg2Rad)*near;
            waterVeil.localPosition=Vector3.forward*near;waterVeil.localRotation=Quaternion.identity;waterVeil.localScale=new Vector3(half*2*cam.aspect,half*2,1);
        }
        void WaterAudioAndEffects()
        {
            var g=GameDirector.Instance;bool deep=Camera.main&&Camera.main.transform.position.y<Surface&&Contains(Camera.main.transform.position);
            if(underwater)underwater.volume=deep&&!g.Blocked?.14f*g.Audio.Volume*g.Audio.SfxVolume:0;
            if(bubbles){bubbles.transform.position=g.Player.transform.position+Vector3.up*.35f;var e=bubbles.emission;e.rateOverTime=Swimming&&g.Player.transform.position.y<Surface-.8f&&!g.Blocked?5:0;}
            Shader.SetGlobalFloat("_UnderwaterTime",Time.time);
        }
        void OnGUI()
        {
            var g=GameDirector.Instance;if(!Swimming||!g||g.Blocked)return;
            var style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSansKR"),fontSize=16,alignment=TextAnchor.MiddleCenter,normal={textColor=Oxygen<20?new Color(1,.4f,.3f):new Color(.6f,1,.95f)}};
            GUI.Label(new Rect(Screen.width*.25f,Screen.height*.81f,Screen.width*.5f,65),$"수심 {Mathf.Max(0,Surface-g.Player.transform.position.y):0.0} m · 호흡 {Oxygen:0}초\nWASD 수영 · SHIFT 빠르게 · SPACE 상승 / 턱 오르기 · CTRL 잠수",style);
        }
    }
}
