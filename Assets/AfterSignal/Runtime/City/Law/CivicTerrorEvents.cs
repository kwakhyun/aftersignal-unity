using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Fictional city incidents, deliberately separate from gang membership and army dispatch.
    public sealed class CivicTerrorEvents:MonoBehaviour
    {
        public static CivicTerrorEvents Instance{get;private set;}public readonly List<TerrorSuspect> Suspects=new();float next;
        void Awake(){Instance=this;next=Time.time+170;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||g.stage!=StageId.UrbanCity||Time.time<next)return;next=Time.time+Random.Range(170,280);
            Suspects.RemoveAll(x=>!x||!x.Body.Alive||x.Body.Downed);if(CityEventGate.Busy||Suspects.Count>0||CampaignBattle.Active||FourCityCatalog.CityAt(g.Player.transform.position)>1)return;
            Vector3 at=LocalCityRoutes.Sidewalk(g.Player.transform.position+Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*Random.Range(80,140));
            foreach(var v in FindObjectsByType<VenueRuntime>(FindObjectsSortMode.None))if(v.Definition.city<2&&(v.Definition.Entrance-g.Player.transform.position).sqrMagnitude<230*230&&Random.value<.4f){at=v.Definition.Entrance;break;}
            Spawn(at);
        }
        public bool Spawn(Vector3 at)
        {
            if(!CityEventGate.Begin(this,CityEventKind.Terror))return false;
            for(int i=0;i<5;i++)if(CrowdFlow.Place(at,i,out var p,20)){var a=TerrorSuspect.Create(p,i);Suspects.Add(a);CityEventGate.Enroll(a.Body);}
            if(Suspects.Count==0){CityEventGate.Cancel(this);return false;}CitySafety.Alarm(at,Suspects[0].Body);return true;
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class TerrorSuspect:MonoBehaviour
    {
        public WorldActor Body{get;private set;}public WorldActor Target{get;private set;}public bool Firing=>Time.time<shotPose;public Vector3 AimTarget{get;private set;}float shotPose;
        CharacterController motor;readonly PursuitPath path=new();float shot,bomb,scan,life,dead;int index;Vector3 home;float gravity;
        public static TerrorSuspect Create(Vector3 at,int index)
        {
            var go=new GameObject("무명단 / 독립 테러 용의자",typeof(SpriteRenderer),typeof(WorldActor),typeof(CharacterController),typeof(TerrorSuspect));go.transform.position=at;go.layer=9;
            var a=go.GetComponent<TerrorSuspect>();a.Body=go.GetComponent<WorldActor>();a.Body.terrorist=true;a.Body.health=180;a.index=index;a.home=at;a.motor=go.GetComponent<CharacterController>();a.motor.radius=.32f;a.motor.height=2;a.motor.center=Vector3.up;a.motor.stepOffset=.65f;
            go.GetComponent<SpriteRenderer>().sharedMaterial=Resources.Load<Material>("Materials/PixelActor");NpcPersona.Ensure(a.Body,"NullCell","독립 테러 용의자");a.bomb=Time.time+12+index*4;a.shot=Time.time+3+index;return a;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;life+=Time.deltaTime;
            if(!Body.Alive||Body.Downed){if(!Body.Alive){dead+=Time.deltaTime;if(dead>45)Destroy(gameObject);}return;}
            if(Time.time>scan){scan=Time.time+.55f;float nearest=90*90;Target=null;foreach(var a in WorldActor.All)if(a&&a!=Body&&a.Alive&&!a.Downed&&!a.terrorist&&!a.military&&!a.monster&&!a.environmental&&!a.helicopter){float d=(a.Center-Body.Center).sqrMagnitude;if(a.police)d*=.65f;if(d<nearest){nearest=d;Target=a;}}}
            bool playerThreat=(Time.time-Body.LastPlayerHit<18||!Target)&&(g.Player.transform.position-transform.position).sqrMagnitude<70*70;var target=playerThreat?g.Player.Shoulder:Target?Target.Center:home;AimTarget=target;Vector3 delta=target-transform.position;float dt=Mathf.Min(.06f,Time.deltaTime);gravity=motor.isGrounded?-2:Mathf.Max(-28,gravity-dt*20);
            var direction=path.Direction(transform.position,target);motor.Move((delta.magnitude>14?direction*2.8f:Vector3.zero)*dt+Vector3.up*gravity*dt);if(Firing||delta.magnitude<14)GetComponent<DirectionalPerson>()?.Face(target,.25f);
            if((Target||playerThreat)&&Time.time>shot&&FactionCombat.Visible(Body.Center,target,65)){shot=Time.time+.55f+Random.value*.6f;shotPose=Time.time+.3f;FactionCombat.Fire(Body,Body.Center+Vector3.up*.3f,target,70,14,SignalEffects.Red,true);g.Audio.PlayGun(GunshotKind.Rifle,Body.Center,.7f);CitySafety.Alarm(Body.Center,Body,Target?Target.GetComponent<CityNpc>():null);}
            if((Target||playerThreat)&&Time.time>bomb&&delta.magnitude<24){bomb=Time.time+35;CivicBomb.Create(playerThreat?g.Player.transform.position:Target.transform.position,Body);}
            if(life>240&&(g.Player.transform.position-transform.position).sqrMagnitude>220*220)Destroy(gameObject);
        }
    }
    public sealed class CivicBomb:MonoBehaviour
    {
        public static int Active{get;private set;}void OnEnable()=>Active++;void OnDisable()=>Active--;
        public WorldActor Source;float fuse=4;WorldActor device;Transform light;
        public static CivicBomb Create(Vector3 at,WorldActor source)
        {
            var go=new GameObject("수상한 폭발 장치",typeof(WorldActor),typeof(CivicBomb));go.transform.position=at;var b=go.GetComponent<CivicBomb>();b.Source=source;b.device=go.GetComponent<WorldActor>();b.device.environmental=true;b.device.helicopter=true;b.device.health=20;
            var box=CommunityWorld.Box(go.transform,"Fictional hazard",Vector3.up*.2f,new Vector3(.55f,.4f,.4f),"DarkMetal");b.light=WorldGeometry.Part(go.transform,"Warning beacon",Vector3.up*.43f,new Vector3(.1f,.06f,.1f),"RedFX").transform;
            foreach(var a in WorldActor.All)if(a&&a.Alive&&(a.transform.position-at).sqrMagnitude<30*30){a.GetComponent<CityNpc>()?.Panic(at,15);NpcSpeech.Say(a,a.police?"폭발물! 시민을 엄폐물 뒤로 대피시켜!":"폭탄이야! 뒤로 물러나!",4,9);}
            return b;
        }
        void Update()
        {
            if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            if(!device.Alive){GameDirector.Instance?.ToastNear("폭발 장치를 파괴했습니다",transform.position,120,3);Destroy(gameObject);return;}
            fuse-=Time.deltaTime;if(light)light.localScale=Vector3.one*(1+Mathf.Sin(Time.time*18)*.3f);
            if(fuse<=0){var at=transform.position;VehicleExplosion.Create(at,4);BlastDamage.Create(at,13,120,Source?Source:TrafficDamageSource.Environment);CitySafety.Shock(at);Destroy(gameObject);}
        }
    }
}
