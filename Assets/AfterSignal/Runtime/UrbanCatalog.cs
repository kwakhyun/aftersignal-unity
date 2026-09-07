using UnityEngine;
namespace AfterSignal
{
    public static class UrbanCatalog
    {
        public const string Prefix="AFTERSIGNAL.Unity.Urban.";
        public const int SiteCount=40;
        public static readonly string[] Names={"새벽아파트","동부경찰서","애프터라이트 소방서","한빛은행","온유병원","새봄초등학교","오름백화점","새벽마트","모아옷장","달빛식당","중앙역","애프터 주유소","공영주차장","항구물류센터","신호복원본부","밤길카페"};
        public static readonly string[] Descriptions={"관리실과 공동 거실, 주민의 방을 둘러볼 수 있습니다.","분실물 신고와 지역 안전 안내를 받는 민원실입니다.","소방대원들이 교대 근무를 준비하는 차고와 휴게실입니다.","은행 창구와 상담 공간입니다. 기억 기록도 소중한 자산이지요.","도시 주민을 위한 진료실입니다. 잠시 쉬며 체력을 회복하세요.","학생들의 기록을 보관하는 교실과 도서 코너입니다.","생활용품과 의류 매장을 연결하는 백화점입니다.","늦은 밤에도 필요한 식료품을 구할 수 있는 마트입니다.","수선한 옷에도 새로운 이야기가 깃듭니다. 진열대를 둘러보세요.","따뜻한 식사를 먹고 체력을 회복할 수 있습니다.","대합실과 승강장입니다. 본부에서 받은 중앙역 임무도 시작할 수 있습니다.","차를 주유기 옆에 세우고 E를 누르세요. 멈춘 동안 연료가 채워집니다.","주차된 차량은 E로 탑승할 수 있습니다. 출차할 때 보행자를 살펴주세요.","도시의 생활 물자를 보관하는 물류 창고입니다.","중앙역 조사와 도시 복원 임무를 진행하는 작전실입니다.","밤길을 걷는 이들이 잠시 머무르는 카페입니다."};
        public static int Kind(int site)=>Mathf.Clamp(site,0,SiteCount-1)%16;
        public static Vector3 Center(int site){int block=site/2;return new Vector3(85+(block%5)*140+(site%2)*50,0,-210+(block/5)*140);}
        public static Vector3 Door(int site)=>Center(site)+new Vector3(0,.12f,-32);
        public static Vector3 Pump(int site)=>Center(site)+new Vector3(0,.02f,-49);
        public static int Current=>Mathf.Clamp(PlayerPrefs.GetInt(Prefix+"Site",0),0,SiteCount-1);
        public static string Name(int site)=>Names[Kind(site)]+(site>=16?" · "+(site/16+1)+"구역":"");
        public static void Enter(GameDirector game,int site){PlayerPrefs.SetInt(Prefix+"Site",site);UrbanSimulation.Instance?.SaveCar();game.Travel(StageId.UrbanInterior);}
        public static void Exit(GameDirector game){CivicWorld.Travel(game,StageId.UrbanCity,Door(Current)+Vector3.back*2);}
        public static void Reset(){foreach(string s in new[]{"Site","Car","CarX","CarZ","CarYaw","CarFuel","CarStage","CarHealth","CarType"})PlayerPrefs.DeleteKey(Prefix+s);}
    }
}
