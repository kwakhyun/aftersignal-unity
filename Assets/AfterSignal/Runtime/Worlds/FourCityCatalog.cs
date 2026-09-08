using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    public enum VenueKind { Garden, Monument, Football, Baseball, Basketball, Circuit, Cinema, Hotel, Amusement, Museum, Research, Civic, Market, Hospital, Archive, Reactor, Police, FireStation, Bank, Restaurant, Cafe, School, Library, Military, Prison, Slum, Island, Laboratory, CityHall, Plaza, Sinkhole }
    [Serializable] public sealed class CityVenue
    {
        public string id, title, district, description;
        public int city;
        public VenueKind kind;
        public Vector3 position;
        public Vector2 size;
        public int visitors;
        public CityVenue(string id,string title,int city,VenueKind kind,float x,float y,float z,float w,float d,int people,string description)
        {this.id=id;this.title=title;this.city=city;this.kind=kind;position=new(x,y,z);size=new(w,d);visitors=people;this.description=description;district=FourCityCatalog.CityNames[city];}
        public Vector3 Entrance => position+new Vector3(0,.12f,-size.y*.5f-5);
        public bool Sport => kind==VenueKind.Football||kind==VenueKind.Baseball||kind==VenueKind.Basketball||kind==VenueKind.Circuit;
    }
    public static class FourCityCatalog
    {
        public const float East=6200, North=3600, South=-7300;
        public static readonly string[] CityNames={"애프터라이트","노바 시티","에레보스 · 잠식도시","네레이드 · 수중도시"};
        public static readonly Vector3[] Centers={new(1120,0,1650),new(1110,0,-5020),new(3850,0,-900),new(3900,-62,-4480)};
        public static readonly Rect[] Land={new(0,1095,2200,1255),new(180,-5650,1860,1470),new(2700,-2100,2500,2500),new(100,2349,2050,1160),new(70,-7200,2380,1621)};
        public static readonly CityVenue[] Venues=CreateVenues();
        static CityVenue[] CreateVenues(){var list=new List<CityVenue>{
            new("lumen-garden","루멘 크라운 식물원",0,VenueKind.Garden,350,0,1450,260,250,42,"유리 아치 아래 열대 정원과 빛의 폭포, 공중 산책로를 잇는 도심 온실."),
            new("signal-spire","세븐 시그널 전망탑",0,VenueKind.Monument,760,0,1430,130,160,28,"일곱 신호를 기리는 나선형 전망대. 승강기로 하늘 정원에 오르세요."),
            new("dawn-stadium","던 유나이티드 축구장",0,VenueKind.Football,1170,0,1440,245,200,110,"전·후반 45분, 11인제 도시 리그. 관람석과 선수 통로, 매표소, 클럽 매장."),
            new("aurora-ballpark","오로라 야구장",0,VenueKind.Baseball,1650,0,1450,260,250,95,"9이닝 정규 경기와 동점 연장. 다이아몬드, 더그아웃, 불펜, 관중석."),
            new("pulse-arena","펄스 농구 아레나",0,VenueKind.Basketball,430,0,1990,155,150,74,"FIBA 방식 4쿼터와 연장전. 코트 사이드 좌석과 선수 라커룸."),
            new("apex-circuit","에이펙스 스트리트 서킷",0,VenueKind.Circuit,1270,0,1980,650,410,88,"8랩 도시 투어링카 경기. 2개 팀, 6대 출전. 피트와 통제탑을 갖춘 폐쇄 서킷."),
            new("prism-cinema","프리즘 시네마",0,VenueKind.Cinema,1940,0,1960,120,140,48,"직접 제작한 원본 단편 상영관. 매표·매점·계단식 좌석·영사실."),
            new("nova-skyhotel","노바 아크 그랜드호텔",1,VenueKind.Hotel,480,0,-4520,165,140,48,"세 개의 곡선 타워와 연결 스카이풀. 객실·레스토랑·전망 라운지."),
            new("nova-wonder","타이드 원더파크",1,VenueKind.Amusement,1050,0,-4550,470,330,130,"대관람차·궤도 코스터·회전목마, 대기열과 운전원, 휴식 정원."),
            new("nova-museum","기억의 파도 박물관",1,VenueKind.Museum,1730,0,-4520,180,160,50,"구부러진 리브 지붕 아래 도시의 기억을 전시하는 문화 공간."),
            new("nova-football","트라이던트 스타디움",1,VenueKind.Football,480,0,-5150,245,200,110,"노바 홈 팀과 애프터라이트 원정 팀의 11인제 해협 리그."),
            new("nova-baseball","해협 문라이트 볼파크",1,VenueKind.Baseball,930,0,-5150,260,250,95,"수로 쪽으로 열린 외야와 야간 조명을 갖춘 프로 야구장."),
            new("nova-basketball","오비탈 실내체육관",1,VenueKind.Basketball,1370,0,-5090,155,150,74,"노바의 농구 클럽 경기와 시민 스포츠를 위한 실내 경기장."),
            new("nova-cinema","홀로웨이브 시네마",1,VenueKind.Cinema,1730,0,-5130,120,140,48,"항구의 밤을 밝히는 디지털 영화관과 카페."),
            new("nova-observatory","해협 빛의 등대",1,VenueKind.Monument,1730,0,-5490,110,120,22,"수중 도시로 향하는 빛을 비추는 전망 등대."),
            new("erebos-gate","에레보스 격리 관문",2,VenueKind.Civic,2860,0,40,110,90,12,"민간인 출입 금지. 격리대원이 도시 진입 장비와 작전 정보를 제공합니다."),
            new("erebos-broadcast","침묵한 방송국",2,VenueKind.Archive,3250,0,-540,130,110,0,"송출실 안에서 실종 당일의 음성 기록을 찾으세요. 잠식체가 내부를 배회합니다."),
            new("erebos-hospital","정지된 기억병원",2,VenueKind.Hospital,4120,0,-440,150,120,0,"병동과 수술실에 남은 피난 기록은 네레이드의 설계 비밀을 가리킵니다."),
            new("erebos-vault","역중력 기록성당",2,VenueKind.Archive,4510,0,-1160,190,180,0,"부유 잔해 사이에 남은 기록실. 신호의 발원과 도시 붕괴의 진실을 보관합니다."),
            new("erebos-reactor","영점 공명로",2,VenueKind.Reactor,3670,0,-1600,200,180,0,"잠식의 중심. 코어에 도달해 도시를 고립시킬지 기억을 복원할지 결정합니다."),
            new("nereid-port","네레이드 감압 관문",3,VenueKind.Civic,3010,-62,-4350,110,110,45,"기밀 해저 도로와 중앙 생활 돔을 잇는 관문. 돔 안에서는 자유롭게 숨 쉴 수 있습니다."),
            new("nereid-garden","펄라이트 산호 정원",3,VenueKind.Garden,3640,-62,-4050,230,190,68,"해양 생물 관찰 갤러리와 실내 숲, 수경 재배 연구실."),
            new("nereid-research","아틀라스 해저 연구원",3,VenueKind.Research,4210,-62,-4130,170,140,72,"생물학·압력 제어·기억 보관 연구원들이 근무하는 최첨단 연구 시설."),
            new("nereid-forum","네레이드 시민 시청",3,VenueKind.CityHall,3960,-62,-4560,160,130,70,"민원·교통 안내·도시 운영 제어를 제공하는 공개 행정 광장."),
            new("nereid-market","블루펄 생활시장",3,VenueKind.Market,3470,-62,-4660,200,140,95,"해조 식당·의류점·생활 도구점이 연결된 기밀 상업가."),
            new("nereid-hospital","심해 종합의료원",3,VenueKind.Hospital,4390,-62,-4640,150,130,65,"감압 치료·진료·입원·응급 복원 기능을 제공하는 의료원."),
            new("nereid-hotel","아비스 그랜드 레지던스",3,VenueKind.Hotel,3960,-62,-5020,165,130,82,"심해 전망 객실과 식당, 주민 공동 거실을 갖춘 주거 호텔."),
            new("nereid-archive","블루 아카이브",3,VenueKind.Archive,4410,-62,-5020,150,130,60,"잠식 이전의 기록을 보존한 도서관. 에레보스 사건을 연결하는 핵심 장소.")
        };RegionalCatalog.Append(list);return list.ToArray();}
        public static readonly Vector3[][] Roads=MakeRoads();
        static Vector3[][] MakeRoads()
        {
            var r=new List<Vector3[]>();
            void Add(params Vector3[] p)=>RegionalCatalog.AddRoad(r,p);
            Add(new(880,0,1050),new(880,0,1200),new(2100,0,1200));
            foreach(float x in new[]{160f,650,930,2100})Add(new(x,0,1200),new(x,0,2240));
            Add(new(1450,0,1200),new(1450,0,1720),new(1830,0,1720),new(1830,0,2240));
            foreach(float z in new[]{1720f,2240})Add(new(160,0,z),new(2100,0,z));
            Add(new(2000,0,-4050),new(2090,0,-4270),new(2000,0,-4800),new(2000,0,-5580));
            Add(new(240,0,-4150),new(240,0,-5580));
            foreach(float z in new[]{-4290f,-4810,-5580})Add(new(240,0,z),new(2000,0,z));
            Add(new(1490,0,-4290),new(1490,0,-5580));
            Add(new(2120,0,60),new(2380,0,130),new(2700,0,130),new(3000,0,130),new(4970,0,130));
            foreach(float x in new[]{3010f,3510,3970,4720,5000})Add(new(x,0,130),new(x,0,-1950));
            foreach(float z in new[]{-220f,-820,-1400,-1950})Add(new(2770,0,z),new(5000,0,z));
            // Gradients remain below 4%. A continuous floor and pressure shell follow the same polyline.
            Add(new(2000,.02f,-4050),new(2240,-7,-4070),new(2500,-18,-4160),new(2770,-32,-4340),new(2970,-44,-4520),new(3190,-62,-4560));
            Add(new(3190,-62,-4560),new(3280,-62,-4390),new(4550,-62,-4390));
            Add(new(3250,-62,-4880),new(4550,-62,-4880));
            foreach(float x in new[]{3280f,3810,4620})Add(new(x,-62,-3900),new(x,-62,-5200));
            RegionalCatalog.Roads(r);
            var mainRoads=r.ToArray();
            foreach(var v in Venues)
            {
                if(v.kind==VenueKind.Island||v.kind==VenueKind.Sinkhole||v.kind==VenueKind.Slum)continue;
                var a=v.Entrance;a.y=v.position.y;Vector3 best=a;float distance=float.MaxValue;
                foreach(var road in mainRoads)for(int i=1;i<road.Length;i++)
                {
                    var p=Closest(a,road[i-1],road[i]);
                    foreach(var candidate in new[]{p,road[i-1],road[i]})
                    {
                        if(Mathf.Abs(candidate.y-a.y)>1)continue;bool clear=true;
                        foreach(var other in Venues)
                        {
                            if(other.city!=v.city)continue;
                            float width=other.Sport||other.kind==VenueKind.Amusement?other.size.x*.49f:Mathf.Min(96,other.size.x*.72f)*.5f;
                            float depth=other.Sport||other.kind==VenueKind.Amusement?other.size.y*.49f:Mathf.Min(88,other.size.y*.66f)*.5f;
                            float margin=other==v?2:12;var rect=new Rect(other.position.x-width-margin,other.position.z-depth-margin,width*2+margin*2,depth*2+margin*2);
                            for(int sample=0;sample<=30;sample++){var q=Vector3.Lerp(a,candidate,sample/30f);if(rect.Contains(new Vector2(q.x,q.z))){clear=false;break;}}
                            if(!clear)break;
                        }
                        float length=(candidate-a).sqrMagnitude;if(clear&&length<distance){best=candidate;distance=length;}
                    }
                }
                if(distance<float.MaxValue)Add(best,a);
            }
            return r.ToArray();
        }
        public static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b)=>a+(b-a)*Mathf.Clamp01(Vector3.Dot(p-a,b-a)/Mathf.Max(.001f,(b-a).sqrMagnitude));
        public static int CityAt(Vector3 p)=>p.x>2700?(p.z< -3200?3:p.z> -2200?2:1):p.z< -2200?1:0;
        public static CityVenue Nearest(Vector3 p){CityVenue best=null;float d=float.MaxValue;foreach(var v in Venues){float n=(v.position-p).sqrMagnitude;if(n<d){d=n;best=v;}}return best;}
        public static bool OnNewLand(Vector3 p){if(RegionalCatalog.Island(p))return true;foreach(var r in Land)if(r.Contains(new Vector2(p.x,p.z)))return true;return false;}
        public static bool Dry(Vector3 p)
        {
            if(RegionalCatalog.Dry(p))return true;
            var d=p-Centers[3];if(d.y>=-.5f&&d.y<50&&d.x*d.x/(1030*1030)+d.z*d.z/(930*930)+d.y*d.y/2500<1)return true;
            foreach(var road in Roads)for(int i=1;i<road.Length;i++)if(road[i].y<-.5f){var q=Closest(p,road[i-1],road[i]);if(Mathf.Abs(p.x-q.x)<12&&Mathf.Abs(p.z-q.z)<12&&p.y>=q.y-.5f&&p.y<q.y+8)return true;}
            return false;
        }
    }
}
