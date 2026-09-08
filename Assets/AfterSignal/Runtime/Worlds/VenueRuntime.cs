using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace AfterSignal
{
    public sealed class VenueRuntime:MonoBehaviour
    {
        public CityVenue Definition{get;private set;}
        public int Index{get;private set;}
        public MultiFloorLift Lift{get;private set;}
        public readonly List<Vector3> staffPoints=new(),activityPoints=new(),seatPoints=new();
        public Vector3 viewPoint,lookPoint,screenPosition;
        public Vector2 screenSize;
        public float roomWidth,roomDepth,floorHeight;
        public int floorCount;
        public int Admissions{get;private set;}
        public TextMesh scoreboard;
        public int ActiveVisitors=>crowd?crowd.transform.childCount:0;
        public int StaffOnDuty{get;private set;}
        public bool FilmPlaying=>film&&film.isPlaying;
        public readonly List<VenueRide> rides=new();
        GameObject crowd;float next;bool spawned;readonly List<VenueActor> players=new();
        readonly List<Transform> raceCars=new();Transform ball;Vector3 ballFrom,ballTo;float ballAge;int lastEvent=-1;
        VideoPlayer film;RenderTexture filmTexture;Material screenMaterial;
        public void Initialize(int index){Index=index;Definition=FourCityCatalog.Venues[index];FourCityArchitecture.Build(this);if(Definition.Sport)BuildSports();if(Definition.kind==VenueKind.Cinema)BuildCinema();}
        public void CreateLift(Vector3 at,int floors,float step)
        {
            var shaft=new GameObject("Venue panoramic lift").transform;shaft.SetParent(transform,false);shaft.localPosition=at;
            Lift=shaft.gameObject.AddComponent<MultiFloorLift>();Lift.floors=floors;Lift.floorHeight=step;Lift.speed=Definition.kind==VenueKind.Monument?10:3.5f;
            Lift.floorNames=new string[floors];for(int f=0;f<floors;f++)Lift.floorNames[f]=f==0?"안내 로비":f==floors-1?"전망 / 라운지":Definition.kind==VenueKind.Hotel?"객실 "+(f+1)+"00":"시설 "+(f+1)+"층";
            var cabin=new GameObject("Moving cabin").transform;cabin.SetParent(shaft,false);Lift.platform=cabin;var g=new CityGeometry(cabin);
            g.Box("Lift floor",new(0,-.12f,0),new(3.5f,.24f,3.5f),"FutureSilver",true);g.Box("Lift ceiling",new(0,3,0),new(3.5f,.15f,3.5f),"FutureSilver");
            g.Box("Lift back",new(0,1.4f,1.7f),new(3.5f,2.8f,.1f),"Glass",true);for(int s=-1;s<=1;s+=2)g.Box("Lift cabin side",new(s*1.7f,1.4f,0),new(.1f,2.8f,3.5f),"Glass",true);
            g.Sign("E  층 선택",new(0,1.8f,1.6f),.16f);g.Finish();VenueService.Add(cabin,new(0,1,0),Index,"승강기 · 층 선택","lift");
            for(int f=0;f<floors;f++)
            {VenueService.Add(transform,at+new Vector3(0,f*step+1,-2.6f),Index,(f+1)+"층 · 승강기 호출","lift",f);var gates=new CityGeometry(transform);var gate=gates.Box("Lift landing safety gate",at+new Vector3(0,f*step+1.1f,-1.9f),new(3.5f,2.2f,.1f),"Glass",true);gate.AddComponent<HighriseLanding>().Initialize(Lift,transform.position.y+f*step);gates.Finish();}
        }
        void BuildSports()
        {
            var v=Definition;if(v.kind==VenueKind.Circuit)
            {
                var prefab=Resources.Load<GameObject>("WorldAssets/FutureSportsCar/FutureSportsCar");
                for(int i=0;i<6;i++){GameObject car=prefab?Instantiate(prefab,transform):new GameObject("Race car");car.name="Race team "+(i%2)+" / car "+(i+1);car.transform.SetParent(transform,false);foreach(var c in car.GetComponentsInChildren<MonoBehaviour>())Destroy(c);foreach(var c in car.GetComponentsInChildren<Collider>())Destroy(c);car.transform.localScale=Vector3.one;foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>()){var mats=renderer.sharedMaterials;for(int n=0;n<mats.Length;n++){string key=mats[n]?mats[n].name:"";mats[n]=CityGeometry.Material(key.Contains("Glazing")?"Glass":key.Contains("Rubber")?"Rubber":key.Contains("Body")?(i%2==0?"SeatCoral":"SeatBlue"):key.Contains("lamp")?"NeonCyan":"FutureSilver");}renderer.sharedMaterials=mats;}raceCars.Add(car.transform);}
                return;
            }
            int perTeam=v.kind==VenueKind.Football?11:v.kind==VenueKind.Baseball?9:5;
            string art=v.kind==VenueKind.Football?"FootballPlayer":v.kind==VenueKind.Baseball?"BaseballPlayer":"BasketballPlayer";
            for(int i=0;i<perTeam*2;i++){var a=VenueActor.Create(this,30000+Index*100+i,i<perTeam?"홈 팀 선수":"원정 팀 선수",art,Vector3.zero);a.athlete=true;a.team=i/perTeam;a.slot=i%perTeam;a.TakeStartingPosition(FourCitySports.Instance.Get(v.id));a.gameObject.SetActive(false);players.Add(a);}
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Live match ball";go.transform.SetParent(transform,false);Destroy(go.GetComponent<Collider>());go.transform.localScale=Vector3.one*(v.kind==VenueKind.Baseball?.18f:.35f);go.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material(v.kind==VenueKind.Basketball?"SeatCoral":"PaintWhite");ball=go.transform;
        }
        void BuildCinema()
        {
            var screen=GameObject.CreatePrimitive(PrimitiveType.Quad);screen.name="Original film screen";screen.transform.SetParent(transform,false);screen.transform.localPosition=screenPosition;screen.transform.localScale=new(screenSize.x,screenSize.y,1);Destroy(screen.GetComponent<Collider>());
            filmTexture=new RenderTexture(1280,720,0);filmTexture.name="Cinema presentation";screenMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));screenMaterial.SetTexture("_BaseMap",filmTexture);screen.GetComponent<Renderer>().sharedMaterial=screenMaterial;
            film=screen.AddComponent<VideoPlayer>();film.playOnAwake=false;film.source=VideoSource.Url;film.url=System.IO.Path.Combine(Application.streamingAssetsPath,"Cinema","SignalTide.mp4");film.renderMode=VideoRenderMode.RenderTexture;film.targetTexture=filmTexture;film.isLooping=true;film.waitForFirstFrame=true;film.skipOnDrop=true;
            var audio=screen.AddComponent<AudioSource>();audio.spatialBlend=1;audio.minDistance=7;audio.maxDistance=70;audio.rolloffMode=AudioRolloffMode.Linear;audio.volume=.28f;film.audioOutputMode=VideoAudioOutputMode.AudioSource;film.SetTargetAudioSource(0,audio);
            film.errorReceived+=(p,error)=>{Debug.LogWarning("Cinema playback: "+error);GameDirector.Instance?.Toast("상영 파일을 불러오지 못했습니다.");};
        }
        void SpawnCrowd()
        {
            spawned=true;crowd=new GameObject("Facility residents / staff / spectators");crowd.transform.SetParent(transform,false);
            int count=Definition.visitors;
            for(int i=0;i<count;i++)
            {
                bool staff=i<Mathf.Min(12,Mathf.Max(3,count/7));string role=Role(staff,i),art=Art(staff,i);
                var points=staff&&staffPoints.Count>0?staffPoints:activityPoints;Vector3 p=points.Count>0?points[i%points.Count]:new Vector3((i%7-3)*2,.08f,-Definition.size.y*.5f+16+i/7*2);
                if(Definition.kind==VenueKind.Prison&&!staff){int n=i%8,f=(i/8)%Mathf.Max(1,floorCount);p=new Vector3((n<4?-1:1)*roomWidth*.21f-1,f*floorHeight+.1f,-roomDepth*.17f+n%4*6);}
                bool spectator=!staff&&seatPoints.Count>0&&i%4!=0;if(spectator)p=seatPoints[i%seatPoints.Count];
                var npc=VenueActor.Create(this,40000+Index*200+i,role,art,p);npc.transform.SetParent(crowd.transform,false);npc.transform.localPosition=p;npc.origin=p;npc.staff=staff;npc.spectator=spectator;npc.serial=i;RegionalResidents.Apply(npc,this,staff,i);if(staff)StaffOnDuty++;
            }
            Admissions+=count;
        }
        string Art(bool staff,int i)
        {var regional=RegionalResidents.Art(Definition,staff,i);if(regional!=null)return regional;if(Definition.kind==VenueKind.Circuit&&staff&&i%2==0)return "RacingDriver";if(Definition.city==3)return staff?(Definition.kind==VenueKind.Hospital?"AbyssMedic":"AbyssEngineer"):i%3==0?"AbyssEngineer":i%3==1?"AbyssCitizen":"AbyssMedic";if(Definition.city==2)return "Soldier";return staff?Definition.kind==VenueKind.Hospital?(i%2==0?"Doctor":"Nurse"):i%3==0?"Worker":i%3==1?"OfficeWoman":"Bartender":PeopleArt.Citizens[(i+Index)%PeopleArt.Citizens.Length];}
        string Role(bool staff,int i)
        {var regional=RegionalResidents.Role(Definition,staff,i);if(regional!=null)return regional;if(!staff)return Definition.Sport?"경기 관람객":Definition.kind==VenueKind.Hotel?"호텔 투숙객":Definition.city==3?"수중 도시 주민":"문화 시설 방문객";return Definition.kind switch{VenueKind.Football or VenueKind.Baseball or VenueKind.Basketball=>i%3==0?"경기장 운영 직원":i%3==1?"경기 심판":"경기장 매점 직원",VenueKind.Circuit=>i%2==0?"레이스 정비사":"경기 진행 요원",VenueKind.Amusement=>i%2==0?"놀이기구 운전원":"놀이공원 안내 직원",VenueKind.Hospital=>i%2==0?"담당 의사":"간호사",VenueKind.Hotel=>i%3==0?"프런트 직원":i%3==1?"객실 관리 직원":"호텔 요리사",VenueKind.Cinema=>i%2==0?"영사 기사":"영화관 안내 직원",VenueKind.Research=>"심해 연구원",VenueKind.Garden=>"정원 해설사",_=>"시설 안내 직원"};}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready)return;float distance=(g.Player.transform.position-transform.position).sqrMagnitude;if(Definition.kind==VenueKind.Slum||Definition.kind==VenueKind.Island)distance=new Bounds(transform.position,new Vector3(Definition.size.x,40,Definition.size.y)).SqrDistance(g.Player.transform.position);
            if(Time.time>next){next=Time.time+.45f;if(distance<260*260&&!spawned)SpawnCrowd();if(crowd)crowd.SetActive(distance<420*420);foreach(var p in players)if(p)p.gameObject.SetActive(distance<260*260);
                if(film){if(distance<100*100&&!film.isPlaying&&!film.isPrepared)film.Prepare();if(distance<100*100&&film.isPrepared&&!film.isPlaying)film.Play();else if(distance>140*140&&film.isPlaying)film.Pause();}}
            if(Definition.Sport)Sports(distance<420*420);
        }
        void Sports(bool close)
        {
            var league=FourCitySports.Instance;if(!league)return;var m=league.Get(Definition.id);if(m==null)return;
            if(scoreboard)scoreboard.text=m.home+"   "+m.homeScore+" : "+m.awayScore+"   "+m.away+"\n"+m.Status+"\n"+m.lastEvent;
            if(!close)return;
            if(Definition.kind==VenueKind.Circuit){for(int i=0;i<raceCars.Count;i++){float t=m.raceDistance[i]/1500;var p=FourCityArchitecture.Track(t);var tangent=FourCityArchitecture.Track(t+.001f)-p;var side=Vector3.Cross(tangent.normalized,Vector3.up);raceCars[i].localPosition=p+side*((i%3-1)*2.7f);raceCars[i].localRotation=Quaternion.LookRotation(-Vector3.Cross(tangent,Vector3.up));}return;}
            if(m.eventNumber!=lastEvent){lastEvent=m.eventNumber;ballFrom=Definition.kind==VenueKind.Baseball?new Vector3(0,1.6f,-25.56f):ball.localPosition;ballTo=new(m.ballX,.25f,m.ballZ);ballAge=0;foreach(var p in players)p.Play(m);}
            if(GameDirector.Instance.Paused)return;ballAge+=Time.deltaTime;float duration=Definition.kind==VenueKind.Baseball?1.2f:.8f,tween=Mathf.Clamp01(ballAge/duration);ball.localPosition=Vector3.Lerp(ballFrom,ballTo,tween)+Vector3.up*Mathf.Sin(tween*Mathf.PI)*Mathf.Max(.25f,m.ballHeight);
        }
        public void Observe(){var g=GameDirector.Instance;g.Player.Respawn(transform.TransformPoint(viewPoint),false);g.CameraRig.SetView(transform.TransformPoint(lookPoint));g.Toast(Definition.Sport?"경기를 관람합니다 · 매표소에서 점수와 승부예측 확인":"마우스로 자유롭게 둘러보세요",5);}
        static VenueRuntime cinemaView;
        public static bool ViewingCinema=>cinemaView&&GameDirector.Instance&&GameDirector.Instance.Ready&&!GameDirector.Instance.Paused&&!GameDirector.Instance.Dialogue&&(GameDirector.Instance.Player.transform.position-cinemaView.transform.TransformPoint(cinemaView.viewPoint)).sqrMagnitude<2.25f;
        public void WatchFilm(){Admissions++;Observe();cinemaView=this;GameDirector.Instance.CameraRig.Snap();if(film){film.Play();GameDirector.Instance.Toast("SIGNAL / 심해의 빛 · 원본 단편 상영 중",6);}}
        public void Rest(){var g=GameDirector.Instance;if(floorCount>1)g.Player.Respawn(transform.TransformPoint(new Vector3(3,floorHeight+.15f,0)),false);g.Toast("8시간 취침 · 체력 회복");}
        public void CreateRides(){for(int i=0;i<3;i++){var go=new GameObject(i==0?"Tide Ferris wheel":i==1?"Orbital coaster":"Signal carousel");go.transform.SetParent(transform,false);var ride=go.AddComponent<VenueRide>();ride.Initialize(this,i);rides.Add(ride);}}
        public void Ride(int index){if(index>=0&&index<rides.Count)rides[index].Board();}
        void OnDestroy(){if(filmTexture){filmTexture.Release();Destroy(filmTexture);}if(screenMaterial)Destroy(screenMaterial);}
    }
}
