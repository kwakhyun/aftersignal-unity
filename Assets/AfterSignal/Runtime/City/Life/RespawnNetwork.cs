using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class RespawnNetwork:MonoBehaviour
    {
        const string Key="AFTERSIGNAL.Unity.RespawnFacility";
        public static readonly Vector3[] Points=new Vector3[4];
        public static readonly string[] Names={"애프터라이트 회복센터","노바 응급의료센터","에레보스 전진기지 의무실","네레이드 생명지원센터"};
        public static bool SuppressSave;
        static int transient=-1,pendingCity=-1;
        public static bool Arriving=>pendingCity>=0;
        public static int Selected=>Mathf.Clamp(SuppressSave?transient:PlayerPrefs.GetInt(Key,-1),-1,3);
        public static string Destination=>"서하의 집";
        public static void ResetHome(){if(SuppressSave)transient=-1;else PlayerPrefs.DeleteKey(Key);}
        public static void Register(int city){city=Mathf.Clamp(city,0,3);ResetHome();GameDirector.Instance?.Player.Heal(100);GameDirector.Instance?.Toast(Names[city]+" · 체력 회복 / 사망 화면에서 리스폰 도시 선택",5);}
        public static void Respawn(GameDirector game)=>Respawn(game,0);
        public static void Respawn(GameDirector game,int city)
        {
            ResetHome();ResidentialWorld.VisitHome=-1;CivicWorld.ClearArrival();
            city=Mathf.Clamp(city,0,3);pendingCity=city==0?-1:city;
            if(city==0)CivicWorld.Travel(game,StageId.Residence,CompactHome.Spawn);
            else{Resolve();CivicWorld.Travel(game,StageId.UrbanCity,Points[city]);}
        }
        public static void Resolve()
        {
            Points[0]=UrbanCatalog.Door(4)+Vector3.back*5;
            for(int city=1;city<4;city++)
            {
                Vector3 best=FourCityCatalog.Centers[city];float distance=float.MaxValue;
                foreach(var v in FourCityCatalog.Venues)
                {
                    if(v.city!=city||v.id.Contains("ruined")||!(v.kind==VenueKind.Hospital||city==2&&v.kind==VenueKind.Military))continue;
                    float d=(v.position-FourCityCatalog.Centers[city]).sqrMagnitude;if(d<distance){distance=d;best=v.Entrance+Vector3.back*4;}
                }
                Points[city]=best;
            }
        }
        IEnumerator Start()
        {
            var game=GameDirector.Instance;if(!game||game.stage!=StageId.UrbanCity)yield break;
            if(Arriving)
            {
                while(!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
                Resolve();Physics.SyncTransforms();var at=Points[pendingCity];
                if(CrowdFlow.Place(at,pendingCity,out var safe,24))at=safe;
                game.Player.Respawn(at);game.checkpoint=at;game.CameraRig.Snap();pendingCity=-1;
            }
            yield return new WaitForSeconds(3);Resolve();
            for(int i=0;i<4;i++)
            {
                Vector3 at=Points[i];if(CrowdFlow.Place(at,i,out var safe,12))at=safe;Points[i]=at;
                var root=new GameObject(Names[i],typeof(RespawnTerminal),typeof(InteractionPoint));root.transform.position=at;
                root.GetComponent<RespawnTerminal>().City=i;
                var point=root.GetComponent<InteractionPoint>();point.title=Names[i]+" · 체력 회복";point.radius=4;
                WorldGeometry.Part(root.transform,"Medical terminal pedestal",Vector3.up*.6f,new Vector3(.8f,1.2f,.7f),"DarkMetal");
                WorldGeometry.Part(root.transform,"Recovery screen",new Vector3(0,1.35f,-.05f),new Vector3(.72f,.45f,.15f),"DistrictBlue");
                WorldGeometry.Part(root.transform,"Recovery symbol",new Vector3(0,1.36f,-.145f),new Vector3(.3f,.07f,.025f),"CyanFX");
                WorldGeometry.Part(root.transform,"Recovery symbol",new Vector3(0,1.36f,-.146f),new Vector3(.07f,.3f,.025f),"CyanFX");
            }
        }
    }
    public sealed class RespawnTerminal:MonoBehaviour { public int City; public void Use()=>RespawnNetwork.Register(City); }
}
