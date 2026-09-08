using System.Collections.Generic;using UnityEngine;
namespace AfterSignal
{
    public sealed class ExpansionWorld:MonoBehaviour
    {
        public static ExpansionWorld Instance{get;private set;}
        public static readonly string[] Names={"루멘 해변","블루워터 항만","애프터라이트 국제공항","홍련 야시장","환승역 광장","오로라 전망공원","동부 물류센터","해변 호텔","항만 진료소","동부 변전소","루멘 방위기지","애프터라이트 교도소","해양 여객터미널","도시기억관리청","노바 해협도시","크로마 수로시장","펠라직 해양연구소","오벨리스크 기록금고","스카이워드 시민의회","에코 수중 관측소","노바 공항","조수 발전 관제소"};
        public static readonly Vector3[] Places={new Vector3(570,0,-545),new Vector3(1730,0,-475),new Vector3(1730,0,525),new Vector3(980,0,-225),new Vector3(880,0,70),new Vector3(860,0,630),new Vector3(1360,0,-276),new Vector3(1100,0,-421),new Vector3(1910,0,-310),new Vector3(1370,0,-88),new Vector3(420,0,720),new Vector3(1180,0,737),new Vector3(1250,0,-673),new Vector3(1270,0,284),new Vector3(920,0,-2460),new Vector3(610,0,-2810),new Vector3(1360,0,-2810),new Vector3(1610,0,-3470),new Vector3(680,0,-3700),new Vector3(980,-28,-2180),new Vector3(1790,0,-3940),new Vector3(470,0,-4010)};
        public static int Selected=-1;
        static ExpansionWorld(){for(int i=0;i<NeonHarbor.Sites.Length;i++)Places[14+i]=NeonHarbor.Sites[i];}
        public static void Install(GameDirector game)
        {
            if(game.stage!=StageId.UrbanCity)return;game.stageLength=ExpansionRoads.Width;game.halfDepth=-NeonHarbor.South;
            var prefab=Resources.Load<GameObject>("WorldAssets/AfterlightExpansion");var expanded=prefab?Instantiate(prefab):null;
            var addon=Resources.Load<GameObject>("WorldAssets/MobilityDistricts");if(addon)Instantiate(addon,expanded?expanded.transform:null);
            var renewal=Resources.Load<GameObject>("WorldAssets/CivicRenewal");if(renewal)Instantiate(renewal,expanded?expanded.transform:null);
            var harbor=Resources.Load<GameObject>("WorldAssets/NeonHarbor");if(harbor)Instantiate(harbor,expanded?expanded.transform:null);
            var terminals=Resources.Load<GameObject>("WorldAssets/TransitFacilities");if(terminals)Instantiate(terminals,expanded?expanded.transform:null);
            game.gameObject.AddComponent<VehicleFleet>();game.gameObject.AddComponent<OceanLife>();game.gameObject.AddComponent<PrisonSystem>();game.gameObject.AddComponent<TaxiNetwork>();
            Camera.main.farClipPlane=6400;
            game.stageLength=FourCityCatalog.East;game.halfDepth=-FourCityCatalog.South;
            new GameObject("Four cities / living world").AddComponent<FourCityWorld>();
        }
        public int Population{get;private set;}
        sealed class DistrictResidents
        {
            public FacilityCrowd seed;public int first;public readonly List<FacilityCitizen> live=new();public float[] health;public int created;
        }
        readonly List<DistrictResidents> districts=new();float next;Material actorMaterial;
        public int ResidentObjects{get;private set;}
        public int ActiveResidents{get;private set;}
        void Awake(){Instance=this;}
        void Start()
        {
            actorMaterial=Resources.Load<Material>("Materials/PixelActor");
            foreach(var seed in GetComponentsInChildren<FacilityCrowd>())
            {districts.Add(new DistrictResidents{seed=seed,first=Population,health=new float[seed.count]});Population+=seed.count;}
        }
        FacilityCitizen SpawnResident(DistrictResidents district,int i)
        {
            if(district.health[i]<0)return null;
            var seed=district.seed;var go=new GameObject("Citizen / "+seed.title+" / "+i,typeof(SpriteRenderer),typeof(CityNpc),typeof(FacilityCitizen));go.transform.SetParent(seed.transform,false);
            int role=(seed.firstRole+i%Mathf.Max(1,seed.roleCount))%FacilityPeople.Jobs.Length;
            string art=seed.arts!=null&&seed.arts.Length>0?seed.arts[i%seed.arts.Length]:FacilityPeople.Key(role);
            string job=seed.jobs!=null&&seed.jobs.Length>0?seed.jobs[i%seed.jobs.Length]:FacilityPeople.Jobs[role];
            bool prisoner=job.Contains("수감자")&&seed.title.Contains("수감자");
            bool soldier=job.Contains("기지")||job.Contains("정비병")||job.Contains("작전 장교");
            if(prisoner)art="Prisoner";else if(soldier)art="Soldier";else if(art=="Prisoner")art="Worker";
            go.transform.position=SpawnPosition(seed,i);var sr=go.GetComponent<SpriteRenderer>();sr.sharedMaterial=actorMaterial;sr.sprite=PeopleArt.Get(art,0);
            var npc=go.GetComponent<CityNpc>();npc.Configure(6000+district.first+i,job,null,seed.title+"에서 생활한다. 주변 시설과 교통편을 잘 안다. 실제 위치와 직업에 맞게 대화한다.");
            PeopleArt.Attach(go,art);var c=go.GetComponent<FacilityCitizen>();c.origin=go.transform.position;c.radius=seed.radius;c.district=seed.title;c.serial=district.first+i;
            if(soldier)go.GetComponent<WorldActor>().military=true;
            if(prisoner){var routine=go.AddComponent<CivicRoutine>();routine.Initialize(job,go.transform.position);routine.prisoner=true;routine.work=c.origin;routine.rest=c.origin+Vector3.right*.4f;c.enabled=false;}
            if(district.health[i]<0)go.GetComponent<WorldActor>().health=0;
            else if(district.health[i]>0)go.GetComponent<WorldActor>().health=district.health[i];
            return c;
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
            var g=GameDirector.Instance;if(!g||!g.Ready||Time.time<next)return;next=Time.time+.2f;
            int budget=20;ResidentObjects=ActiveResidents=0;var player=g.Player.transform.position;
            foreach(var district in districts)
            {
                if(!district.seed)continue;float distance=(district.seed.transform.position-player).sqrMagnitude;
                if(distance>600*600&&district.live.Count>0)
                {
                    for(int i=0;i<district.live.Count;i++)if(district.live[i]){var body=district.live[i].GetComponent<WorldActor>();district.health[i]=body&&body.Alive?body.health:-1;Destroy(district.live[i].gameObject);}
                    district.live.Clear();district.created=0;continue;
                }
                if(distance<250*250)while(district.created<district.seed.count&&budget>0)
                {district.live.Add(SpawnResident(district,district.created++));budget--;}
                foreach(var c in district.live)if(c){bool active=(c.transform.position-player).sqrMagnitude<210*210;if(c.gameObject.activeSelf!=active)c.gameObject.SetActive(active);ResidentObjects++;if(active)ActiveResidents++;}
            }
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
