using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public sealed class ExpansionWorld:MonoBehaviour
    {
        public static ExpansionWorld Instance{get;private set;}
        public static readonly string[] Names={"루멘 해변","블루워터 항만","애프터라이트 국제공항","홍련 야시장","환승역 광장","오로라 전망공원","동부 물류센터","해변 호텔","항만 진료소","동부 변전소","루멘 방위기지","애프터라이트 교도소","해양 여객터미널","도시기억관리청","노바 해협도시"};
        public static readonly Vector3[] Places={new Vector3(570,0,-545),new Vector3(1730,0,-475),new Vector3(1730,0,525),new Vector3(980,0,-225),new Vector3(880,0,70),new Vector3(860,0,630),new Vector3(1360,0,-276),new Vector3(1100,0,-421),new Vector3(1910,0,-310),new Vector3(1370,0,-88),new Vector3(420,0,720),new Vector3(1180,0,737),new Vector3(1250,0,-673),new Vector3(1270,0,284),new Vector3(920,0,-2460)};
        public static int Selected=-1;
        public static void Install(GameDirector game)
        {
            if(game.stage!=StageId.UrbanCity)return;game.stageLength=ExpansionRoads.Width;game.halfDepth=-NeonHarbor.South;
            var prefab=Resources.Load<GameObject>("WorldAssets/AfterlightExpansion");var expanded=prefab?Instantiate(prefab):null;
            var addon=Resources.Load<GameObject>("WorldAssets/MobilityDistricts");if(addon)Instantiate(addon,expanded?expanded.transform:null);
            var renewal=Resources.Load<GameObject>("WorldAssets/CivicRenewal");if(renewal)Instantiate(renewal,expanded?expanded.transform:null);
            var harbor=Resources.Load<GameObject>("WorldAssets/NeonHarbor");if(harbor)Instantiate(harbor,expanded?expanded.transform:null);
            game.gameObject.AddComponent<VehicleFleet>();game.gameObject.AddComponent<OceanLife>();game.gameObject.AddComponent<PrisonSystem>();
            Camera.main.farClipPlane=5400;
        }
        public int Population{get;private set;}
        readonly List<FacilityCitizen> people=new List<FacilityCitizen>();float next;
        void Awake(){Instance=this;}
        void Start()
        {
            foreach(var seed in GetComponentsInChildren<FacilityCrowd>())
            {
                for(int i=0;i<seed.count;i++)
                {
                    var go=new GameObject("Citizen / "+seed.title+" / "+i,typeof(SpriteRenderer),typeof(CityNpc),typeof(FacilityCitizen));go.transform.SetParent(seed.transform,false);
                    int role=(seed.firstRole+i%seed.roleCount)%FacilityPeople.Jobs.Length;
                    string art=seed.arts!=null&&seed.arts.Length>0?seed.arts[i%seed.arts.Length]:FacilityPeople.Key(role);
                    string job=seed.jobs!=null&&seed.jobs.Length>0?seed.jobs[i%seed.jobs.Length]:FacilityPeople.Jobs[role];
                    go.transform.position=SpawnPosition(seed,i);
                    var sr=go.GetComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.sprite=PeopleArt.Get(art,0);
                    var npc=go.GetComponent<CityNpc>();npc.Configure(6000+Population,job,null,seed.title+"에서 생활한다. 주변 시설과 교통편을 잘 안다. 실제 위치와 직업에 맞게 대화한다.");
                    PeopleArt.Attach(go,art);var c=go.GetComponent<FacilityCitizen>();c.origin=go.transform.position;c.radius=seed.radius;c.district=seed.title;c.serial=Population;
                    people.Add(c);Population++;
                }
            }
        }
        static Vector3 SpawnPosition(FacilityCrowd seed,int index)
        {
            for(int attempt=0;attempt<64;attempt++)
            {
                float angle=(index+attempt*7)*2.399963f;
                float spread=Mathf.Sqrt(((index+attempt*13)%seed.count+.5f)/seed.count)*seed.radius*.82f;
                var p=seed.transform.TransformPoint(new Vector3(Mathf.Cos(angle)*spread,0,Mathf.Sin(angle)*spread));
                if(Physics.Raycast(p+Vector3.up*2,Vector3.down,out var ground,5,1,QueryTriggerInteraction.Ignore)&&ground.normal.y>.8f&&Mathf.Abs(ground.point.y-seed.transform.position.y)<.28f)
                {p=ground.point+Vector3.up*.05f;if(!Physics.CheckCapsule(p+Vector3.up*.45f,p+Vector3.up*1.5f,.3f,1,QueryTriggerInteraction.Ignore))return p;}
            }
            return seed.transform.position+Vector3.up*.12f;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||Time.time<next)return;next=Time.time+1;
            foreach(var p in people)if(p){bool nearby=(p.transform.position-g.Player.transform.position).sqrMagnitude<210*210;p.gameObject.SetActive(nearby);}
        }
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class FacilityCitizen:MonoBehaviour
    {
        public Vector3 origin;public float radius;public string district;public int serial;
        Vector3 target;float next,chat;WorldActor body;CityNpc npc;
        void Start(){body=GetComponent<WorldActor>();npc=GetComponent<CityNpc>();target=origin;chat=Time.time+Random.Range(8f,28f);}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!body||!body.Alive||npc.Fleeing||CivilianImpact.Active(this)||CivilianDefense.Active(this)||Time.time<npc.SocialUntil)return;
            if(Time.time>next){next=Time.time+Random.Range(8f,19f);var d=Random.insideUnitCircle*radius;var p=origin+new Vector3(d.x,0,d.y);if(Physics.Raycast(p+Vector3.up*3,Vector3.down,out var ground,5,1,QueryTriggerInteraction.Ignore)&&ground.normal.y>.8f&&Mathf.Abs(ground.point.y-origin.y)<.3f)target=ground.point+Vector3.up*.04f;}
            Vector3 delta=target-transform.position;delta.y=0;if(delta.magnitude>.4f&&!Physics.Raycast(transform.position+Vector3.up,delta.normalized,.7f,1,QueryTriggerInteraction.Ignore))transform.position+=delta.normalized*Time.deltaTime*(1.05f+serial%4*.17f);
            if(Time.time>chat&&(transform.position-g.Player.transform.position).sqrMagnitude<30*30){chat=Time.time+Random.Range(22f,45f);NpcSpeech.Say(npc,NpcDialogueBank.Line(npc,"ambient"),4);}
        }
    }
}
