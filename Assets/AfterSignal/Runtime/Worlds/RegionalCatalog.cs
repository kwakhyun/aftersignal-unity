using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class RegionalCatalog
    {
        public static readonly Vector3 HomeQuarter=new(430,0,2670), RiftCenter=new(3850,0,-1030),PressureAnnex=new(5320,-62,-4500);
        public static readonly Vector3[] Islands={new(390,.05f,-1320),new(2350,.05f,-1840),new(3500,.05f,-2740),new(5050,.05f,-2890)};
        public static readonly string[] IslandNames={"솔바람 어촌","러스티 조선섬","오키드 생태섬","유리등대 해상마을"};
        public static bool Slum(Vector3 p)=>p.z>2450&&p.z<3440&&p.x>170&&p.x<2050;
        public static bool Island(Vector3 p){foreach(var i in Islands){var d=p-i;if(d.x*d.x/(210*210)+d.z*d.z/(165*165)<1)return true;}return false;}
        public static bool Dry(Vector3 p){var d=p-PressureAnnex;return d.y>=-.5f&&d.y<50&&d.x*d.x/(720*720)+d.z*d.z/(720*720)+d.y*d.y/2500<1;}
        public static bool InRift(Vector3 p,float margin=0)=>Vector2.Distance(new(p.x,p.z),new(RiftCenter.x,RiftCenter.z))<210+margin;
        public static void Append(List<CityVenue> venues)
        {
            void Add(string id,string title,int city,VenueKind kind,float x,float z,float w,float d,int n,string text)=>venues.Add(new(id,title,city,kind,x,city==3?-62:0,z,w,d,n,text));
            Add("dawn-lowlands","새벽 저지대 · 서하의 고향",0,VenueKind.Slum,1100,2940,1800,940,180,"고층 부촌의 방벽 아래 자리한 애프터라이트 외곽. 새벽 주거동, 이웃의 골목, 공동 식당, 고물상, 암시장이 이어진다. 서하는 이곳에서 자랐으며 주민들과 오랜 신뢰를 나눈다.");
            Add("nova-fort","노바 해협 방위사령부",1,VenueKind.Military,480,-5900,370,270,36,"항공 격납고와 계단식 지휘동, 방파제 훈련장, 차량 정비고를 갖춘 노바 독립 방위기지.");
            Add("nova-prison","노바 링 교정시설",1,VenueKind.Prison,1900,-5920,330,270,42,"원형 감시 회랑과 중정 운동장, 면회·수용·교정 교육 구역이 분리된 교도소.");
            Add("nova-cityhall","노바 조수 시청",1,VenueKind.CityHall,1180,-6230,190,175,65,"층마다 뒤로 물러나는 유리 의회, 공개 민원홀과 항만을 내려다보는 시민 테라스.");
            Add("nova-police","노바 해양경찰본서",1,VenueKind.Police,1660,-6340,140,125,32,"개방된 민원 중정과 별도의 수사·유치장·순찰차 출동 구역.");
            Add("nova-fire","노바 구조소방본부",1,VenueKind.FireStation,2100,-6410,160,120,25,"긴 쐐기형 캐노피 아래 구조차 출동 베이, 훈련탑과 해상 구조 장비실.");
            Add("nova-bank","노바 크레센트 은행",1,VenueKind.Bank,750,-6290,140,110,38,"곡면 석재 외피와 이중 높이 영업홀, 상담실, 강화 금고를 갖춘 해협 금융점.");
            Add("nova-restaurant","노바 선셋 다이닝",1,VenueKind.Restaurant,390,-6460,110,95,38,"항만 시장의 어획물을 사용하는 테라스 식당. 주방과 손님 테이블을 오가며 영업한다.");
            Add("nova-cafe","노바 필터웨이브",1,VenueKind.Cafe,660,-6780,110,95,30,"목재 루버와 떠 있는 차양, 원두 로스터와 독서 테라스를 갖춘 카페.");
            Add("nova-school","노바 해협학원",1,VenueKind.School,1130,-6780,250,190,90,"중정 운동장을 감싸는 교실동과 행정실, 보건실, 과학실, 급식실.");
            Add("nova-library","노바 파도 중앙도서관",1,VenueKind.Library,1560,-6780,175,155,65,"목재 아치와 다이아몬드 창틀, 계단식 열람실과 어린이 자료실.");
            Add("nova-medical","노바 코럴 대학병원",1,VenueKind.Hospital,1980,-6860,230,195,80,"응급차 진입로와 감염 분리 동선, 입원 병동, 치료실과 회복 정원.");
            Add("nova-plaza","노바 해협 시민광장",1,VenueKind.Plaza,1090,-5840,210,160,95,"방사형 포장과 반사 수로, 공연 계단과 노점이 모이는 시민 광장.");
            string[] ids={"police","fire","bank","restaurant","cafe","school","library","plaza"};
            string[] names={"네레이드 압력치안국","네레이드 심해구조소방서","네레이드 펄 리저브 은행","네레이드 켈프 키친","네레이드 버블 카페","네레이드 해양학교","네레이드 기억산호 도서관","네레이드 생명의 광장"};
            VenueKind[] kinds={VenueKind.Police,VenueKind.FireStation,VenueKind.Bank,VenueKind.Restaurant,VenueKind.Cafe,VenueKind.School,VenueKind.Library,VenueKind.Plaza};
            Vector2[] p={new(5030,-4130),new(5450,-4120),new(5810,-4290),new(4920,-4640),new(5780,-4680),new(5180,-4890),new(5560,-4910),new(5340,-4500)};
            for(int i=0;i<ids.Length;i++)Add("nereid-"+ids[i],names[i],3,kinds[i],p[i].x,p[i].y,i==5?165:135,i==5?130:105,35+i*5,"해수 압력을 견디는 리브 쉘과 기밀 출입구, 산호빛 안내 조명, 독립 공기 순환 설비를 갖춘 수중 도시 전용 시설.");
            Add("erebos-base","에레보스 제로 특수작전기지",2,VenueKind.Military,2940,-350,210,180,28,"잠식 경계선을 지키는 특수부대의 콘크리트 방호 기지. 방역 에어록, 작전통제실과 장비고.");
            Add("erebos-institute","에레보스 잔향 연구기관",2,VenueKind.Research,3330,-40,180,150,30,"방호 유리 관찰실과 샘플 분류 구역에서 잠식 현상을 조사하는 연구원과 경비대.");
            Add("erebos-lab","에레보스 격리 실험실",2,VenueKind.Laboratory,4570,-490,180,150,20,"독립 격리 캡슐과 실험대, 관찰 회랑, 비상 세척실을 갖춘 생체 신호 실험시설.");
            Add("erebos-ruined-fort","잠식된 제7군 주둔지",2,VenueKind.Military,2890,-1580,230,210,0,"무너진 격납고와 버려진 전차, 떠오른 철근 잔해. 내부에는 잠식체가 남아 있다.");
            Add("erebos-ruined-prison","파괴된 에레보스 교도소",2,VenueKind.Prison,4600,-1750,260,230,0,"뜯겨 나간 감시탑과 끊어진 수용동, 비어 있는 운동장에 잠식된 수감자들이 배회한다.");
            Add("erebos-ruined-hall","붕괴한 에레보스 시청",2,VenueKind.CityHall,4210,-1870,180,150,0,"갈라진 의회 지붕과 공중에 떠 있는 기록실. 파괴 전 도시 행정의 흔적.");
            Add("erebos-sinkhole","영점 싱크홀",2,VenueKind.Sinkhole,3850,-1030,480,480,0,"직경 420m의 거대한 붕괴구. 끊어진 도로와 부유 잔해 아래에서 정체불명의 신호가 올라온다.");
            for(int i=0;i<Islands.Length;i++)Add("strait-island-"+i,IslandNames[i],i==0?0:1,VenueKind.Island,Islands[i].x,Islands[i].z,420,330,48+i*9,i==0?"목조 수상가옥과 그물 건조장, 어시장과 작은 방파제 마을.":i==1?"재활용 선체와 크레인, 용접 작업장과 컨테이너 주거가 섞인 조선섬.":i==2?"수경 재배 온실과 풍력 발전, 순환 자원을 사용하는 녹색 공동체.":"해상 등대를 중심으로 유리 지붕 주거와 관광 카페가 모인 항해자 마을.");
        }
        public static void AddRoad(List<Vector3[]> roads,params Vector3[] points)
        {
            var result=new List<Vector3>{points[0]};
            for(int i=1;i<points.Length;i++)
            {
                var a=points[i-1];var b=points[i];var closest=FourCityCatalog.Closest(RiftCenter,a,b);
                if(a.x>2650&&a.z> -2200&&b.z> -2200&&InRift(closest,36))
                {
                    float aa=Mathf.Atan2(a.z-RiftCenter.z,a.x-RiftCenter.x),bb=Mathf.Atan2(b.z-RiftCenter.z,b.x-RiftCenter.x);
                    float arc=Mathf.DeltaAngle(aa*Mathf.Rad2Deg,bb*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                    result.Add(RiftCenter+new Vector3(Mathf.Cos(aa)*275,0,Mathf.Sin(aa)*275));
                    int n=Mathf.CeilToInt(Mathf.Abs(arc)/.13f);for(int j=1;j<=n;j++){float t=aa+arc*j/n;result.Add(RiftCenter+new Vector3(Mathf.Cos(t)*275,0,Mathf.Sin(t)*275));}
                }
                result.Add(b);
            }
            roads.Add(result.ToArray());
        }
        public static void Roads(List<Vector3[]> roads)
        {
            AddRoad(roads,new(650,0,2240),new(650,0,2480),new(260,0,2540),new(260,0,3360),new(1940,0,3360),new(1940,0,2500),new(1830,0,2240));
            AddRoad(roads,new(260,0,2870),new(950,0,2860),new(1420,0,2990),new(1940,0,3010));
            AddRoad(roads,new(950,0,2500),new(950,0,3360));
            AddRoad(roads,new(240,0,-5580),new(220,0,-5720),new(250,0,-6200),new(220,0,-6700),new(340,0,-7080),new(1740,0,-7080),new(2330,0,-7000),new(2350,0,-5750),new(2000,0,-5580));
            AddRoad(roads,new(250,0,-6110),new(1030,0,-6070),new(1490,0,-6130),new(2350,0,-6150));
            AddRoad(roads,new(220,0,-6640),new(840,0,-6590),new(1440,0,-6590),new(2350,0,-6590));
            AddRoad(roads,new(1490,0,-5580),new(1490,0,-6130),new(1440,0,-6590),new(1440,0,-7080));
            AddRoad(roads,new(4620,-62,-4390),new(4790,-62,-4480),new(4860,-62,-4480),new(5020,-62,-4380),new(5700,-62,-4380),new(5980,-62,-4500),new(5700,-62,-4780),new(5000,-62,-4780),new(4860,-62,-4480));
        }
    }
}
