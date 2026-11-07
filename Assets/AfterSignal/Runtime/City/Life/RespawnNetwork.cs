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
        public static int Selected=>Mathf.Clamp(SuppressSave?transient:SaveStore.GetInt(Key,-1),-1,3);
        public static string Destination=>"서하의 집";
        public static void ResetHome(){if(SuppressSave)transient=-1;else SaveStore.DeleteKey(Key);}
        static string notice;
        public static int LastFee{get;private set;}
        // Hospital fee for dying: a share of carried credits only. Savings and the home safe are never charged.
        public static int HospitalFee{get{LifeState.Load();var t=GameTuning.Active;int fee=Mathf.RoundToInt(Mathf.Clamp(LifeState.Credits*t.hospitalFeeRate,t.hospitalFeeMin,t.hospitalFeeMax)*DifficultySettings.Penalty);return Mathf.Clamp(fee,0,LifeState.Credits);}}
        public static void ChargeHospital(GameDirector game)
        {
            LastFee=0;if(!game||!CivicWorld.Exploration(game.stage))return;
            int fee=HospitalFee;if(fee>0&&LifeState.Spend(fee))LastFee=fee;
        }
        public static void Register(int city)
        {
            city=Mathf.Clamp(city,0,3);ResetHome();var g=GameDirector.Instance;if(!g)return;
            var p=g.Player;int fee=GameTuning.Active.terminalHealFee;string where=Names[city]+" · 사망 화면에서 리스폰 도시 선택";
            if(p.Health>=p.Tuning.maxHealth-.5f){g.Toast(where,5);return;}
            // Full treatment costs a small fee; without it the terminal only stabilizes to the natural recovery cap.
            if(LifeState.Spend(fee)){p.Heal(p.Tuning.maxHealth);g.Toast(where+" · 체력 완전 회복 / "+fee+" C",5);}
            else{p.HealTo(p.RecoveryCap);g.Toast(where+" · 잔액 부족 · 응급 안정 처치만 받았습니다 (완전 회복 "+fee+" C)",6);}
        }
        public static void Respawn(GameDirector game)=>Respawn(game,0);
        public static void Respawn(GameDirector game,int city)
        {
            ResetHome();ResidentialWorld.VisitHome=-1;CivicWorld.ClearArrival();
            notice=LastFee>0?"응급 이송 · 치료비 "+LastFee.ToString("N0")+" C 청구 · 예금과 집 금고의 돈은 안전합니다":null;LastFee=0;
            // An unserved sentence continues: the clinic hands Seoha back to the prison cell.
            if(PrisonSystem.Sentenced){pendingCity=-1;notice=(notice!=null?notice+" · ":"")+"남은 형기를 교도소에서 이어갑니다";CivicWorld.Travel(game,StageId.UrbanCity,PrisonSystem.Cell);return;}
            city=Mathf.Clamp(city,0,3);pendingCity=city==0?-1:city;
            if(city==0)CivicWorld.Travel(game,StageId.Residence,CompactHome.Spawn);
            else{Resolve();CivicWorld.Travel(game,StageId.UrbanCity,Points[city]);}
        }
        IEnumerator ShowNotice()
        {
            var game=GameDirector.Instance;
            while(game&&(!game.Ready||game.Blocked))yield return null;
            yield return new WaitForSeconds(1.2f);
            if(game&&notice!=null)game.Toast(notice,6);
            notice=null;
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
            if(notice!=null)StartCoroutine(ShowNotice());
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
