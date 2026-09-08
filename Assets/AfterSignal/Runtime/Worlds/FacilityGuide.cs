using System;
using UnityEngine;
namespace AfterSignal
{
    public static class FacilityGuide
    {
        public static string Rules(VenueKind kind)=>kind switch
        {
            VenueKind.Football=>"11명씩 출전, 전·후반 각 45분. 골·오프사이드·반칙·페널티킥을 판정하며 정규시간 동점은 무승부입니다. 경기 시계는 실제 시간의 18배로 진행됩니다.",
            VenueKind.Basketball=>"5명씩 출전, 10분씩 4쿼터. 2점·3점·자유투, 24초 공격 제한과 리바운드, 팀 파울을 적용합니다. 동점은 5분 연장전을 반복합니다. 경기 시계는 8배속입니다.",
            VenueKind.Baseball=>"9명씩 수비, 각 반이닝 3아웃. 4볼은 볼넷, 3스트라이크는 삼진이며 2스트라이크 이후 파울은 삼진이 아닙니다. 9회말 생략·끝내기·연장 승부치기를 적용합니다. 시간 제한이 없습니다.",
            VenueKind.Circuit=>"2개 팀, 팀당 3대가 8랩을 완주합니다. 결승선을 통과한 순서대로 10·8·6·4·2·1점을 부여하고 팀 합계로 승패를 정합니다. 동점은 배팅 원금을 돌려드립니다.",
            _=>"안내 직원에게 시설 이용 방법을 물어보세요."
        };
        public static string For(CityVenue v)=>v.Sport?Rules(v.kind):v.kind switch
        {
            VenueKind.Slum=>"시장 공동 단말 E에서 8 C 식사, 암시장 거래, 비공식 운송 의뢰를 이용하세요. 서하의 집은 저지대 서남쪽의 기존 새벽 주거동에 있습니다. 이곳 이웃은 서하와 함께 자랐으며 높은 친밀도로 시작합니다. 폭력은 신뢰를 떨어뜨립니다.",
            VenueKind.Island=>"부두 옆 보트는 E로 조종할 수 있습니다. 해협 순환 여객선이 정차하면 G로 승객 탑승하고 F로 하선합니다. 마을 안내 단말에서 18 C 식사를 이용하세요.",
            VenueKind.Bank=>"안내 단말 E에서 500 C씩 예금·인출할 수 있습니다. 영업장 안의 금고는 별도 상호작용 대상입니다. 현금 탈취를 목격한 직원은 경찰에 신고합니다.",
            VenueKind.Restaurant or VenueKind.Cafe=>"안내 단말 E에서 식사 또는 음료를 구매하거나 아르바이트를 시작하세요. 화면 주문에 따라 응대·서빙·음료 제조를 진행하면 보수를 받습니다.",
            VenueKind.FireStation=>"출동 베이와 훈련탑, 구조 장비실을 둘러볼 수 있습니다. 안내 단말 E에서 무료 응급처치 교육을 받으면 체력이 20 회복됩니다.",
            VenueKind.Police=>"정문은 민원 접수 구역입니다. 안내 단말 E에서 안전·투항 방법을 확인하세요. 경찰서 안에서 범죄가 발생하면 근무 중인 경찰이 대응합니다.",
            VenueKind.Military=>"지휘동과 장비고는 계단·승강기로 이동합니다. 노바 기지의 전차와 항공기는 차량 가까이에서 E로 조종할 수 있습니다. 에레보스 기지는 잠식체에 대응하는 격리대가 지킵니다.",
            VenueKind.Prison=>"정문 접수 구역과 교도관에게 면회 안내를 문의하세요. 철창이 있는 수용 구역은 출입을 제한합니다.",
            VenueKind.School=>"안내 단말 E에서 1시간 공개 수업에 참가할 수 있습니다. 교실은 계단이나 승강기를 이용해 둘러보세요.",
            VenueKind.Library=>"안내 단말 E에서 도시 연혁 자료를 열람할 수 있습니다. 열람석과 서가를 자유롭게 이용하고 사서에게 궁금한 점을 물어보세요.",
            VenueKind.Sinkhole=>"붕괴구 가장자리의 관측 단말 E로 관측 지점에 설 수 있습니다. 지면이 실제로 붕괴한 구역이므로 우회 도로와 난간 바깥으로 떨어지지 않도록 이동하세요.",
            VenueKind.Cinema=>"입구 안내 단말에 가까이 가서 E → 영화 관람(25 C)을 선택하세요. 상영관 좌석에서 원본 단편 영상을 볼 수 있습니다. 자유롭게 걸어서 나올 수 있습니다.",
            VenueKind.Amusement=>"안내 단말에서 대관람차(30 C), 코스터(40 C), 회전목마(15 C)를 선택하세요. 승강장 대기 후 운전원이 출발시킵니다. 탑승 중 F를 누르면 입구에서 내립니다.",
            VenueKind.Hotel=>"로비 안내 단말 E → 객실 취침(120 C)을 선택하면 8시간 쉬고 체력을 회복합니다. 승강기 앞에서 E로 호출한 후 내부에서 E로 층을 선택하세요. 계단으로도 층을 이동할 수 있습니다.",
            VenueKind.Hospital=>"접수 단말 E → 진료(80 C)를 선택하면 체력이 모두 회복됩니다. 입원 병동은 승강기나 계단으로 올라가세요.",
            VenueKind.Market=>"상점 안내 단말 E에서 도시락과 전력 충전을 구매할 수 있습니다. 비용과 회복량을 확인한 다음 선택하세요.",
            VenueKind.Monument=>"탑 옆 승강기 앞에서 E로 호출하세요. 객실에 타고 E를 눌러 전망층을 선택하면 실제 승강기가 올라갑니다. 전망층 다리로 걸어가 도시를 둘러볼 수 있습니다.",
            _=>"정문은 바로 걸어서 들어갈 수 있습니다. 전시와 업무실은 통로를 따라 둘러보세요. 다층 시설은 계단이나 승강기로 이동하며, 단말은 가까이에서 E로 이용합니다."
        };
        public static string Knowledge(CityNpc npc)
        {
            var v=FourCityCatalog.Nearest(npc.transform.position);
            var localVenue=npc.GetComponent<VenueActor>()?.venue;if(localVenue)v=localVenue.Definition;
            string local=v!=null&&(localVenue||Vector3.Distance(v.position,npc.transform.position)<180)?v.title+". "+For(v):"";
            return "시설 사용법은 실제 조작과 일치하게 설명한다. 상호작용 E, 지도 M, 무기 숫자키, 승객 탑승 G, 하차 F. "+local+" 공항/항만 교통편은 터미널 안내 단말에서 노선, 운임, 출발 정보를 확인하고 탑승 구역으로 이동한다. 네레이드 기밀 도로와 돔 내부는 호흡이 가능하다. 이용 가능하지 않은 기능이나 운임은 지어내지 않는다.";
        }
        public static bool TryAnswer(CityNpc npc,string question,out string answer)
        {
            answer=null;if(string.IsNullOrWhiteSpace(question))return false;
            bool asks=Has(question,"어떻게","방법","어디","이용","탑승","타려","타는","요금","가격","규칙","몇 시","관람","배팅","베팅","승강기","엘리베이터","호흡","숨","수영");if(!asks)return false;
            if(Has(question,"비행기","공항","항공")){answer="공항 터미널 안내 단말에 가까이 가서 E를 눌러 노선과 운임을 확인하세요. 안내된 탑승 구역에서 대기 중인 여객기에 G로 승객 탑승할 수 있어요. 조종석에 직접 타려면 E를 사용하고, 하차는 F예요. M 지도의 교통 항목에서 공항 위치와 노선을 찾을 수 있습니다.";return true;}
            if(Has(question,"배 타","배를","여객선","항구","항만","수상택시")){answer="항만 여객터미널 안내 단말에서 E로 노선과 운임을 확인하세요. 정박한 여객선 옆에서 G로 승객 탑승하고 F로 내릴 수 있어요. 수상택시는 승강장에 정차한 택시 옆에서 E로 운임을 내고 탑승합니다. M 지도에서 항만과 택시 승강장을 찾으세요.";return true;}
            if(Has(question,"버스")){answer="버스 정류장 표지 옆에서 E를 눌러 탑승 안내를 확인하세요. 버스가 정차하면 요금을 내고 탈 수 있어요. 탑승 중 정류장 안내를 확인하고 원하는 곳에서 하차하세요.";return true;}
            if(Has(question,"아르바이트","알바","서빙","카페","식당")){answer="식당이나 카페의 직원 또는 업무 단말 가까이에서 E를 눌러 이용 메뉴를 열어 보세요. 아르바이트를 시작하면 화면의 주문과 테이블 안내를 따라 손님 응대·음식 서빙·음료 만들기를 진행할 수 있습니다. 식사 구매 비용과 일한 뒤 받을 보수도 메뉴에 표시돼요.";return true;}
            if(Has(question,"무기","총을","검을","상점","구매")){answer="상점의 판매 단말 가까이에서 E를 누르면 상품과 가격이 표시돼요. 보유 크레딧을 확인하고 구매할 품목을 선택하세요. 무기 상점에서 구입한 장비는 숫자키로 전환할 수 있습니다.";return true;}
            if(Has(question,"차량 수리","카센터","튜닝")){answer="차량을 카센터 정비 구역에 세운 뒤 가까운 정비 단말에서 E를 눌러 주세요. 차량 수리와 외관 튜닝 항목에서 비용을 확인하고 선택할 수 있습니다.";return true;}
            if(Has(question,"수중","네레이드","숨","호흡")){answer="노바 동쪽의 기밀 해저 도로를 따라가면 네레이드로 이어집니다. 도로의 투명 터널과 도시 돔 안에서는 숨을 편하게 쉴 수 있어요. 돔 바깥 바닷속에서는 산소가 줄어들니 산소 표시를 확인하세요.";return true;}
            var v=FourCityCatalog.Nearest(npc.transform.position);
            VenueKind? requested=Has(question,"축구")?VenueKind.Football:Has(question,"야구")?VenueKind.Baseball:Has(question,"농구")?VenueKind.Basketball:Has(question,"레이싱","경주")?VenueKind.Circuit:Has(question,"영화")?VenueKind.Cinema:Has(question,"호텔","숙박")?VenueKind.Hotel:Has(question,"병원","치료")?VenueKind.Hospital:Has(question,"놀이공원","놀이기구")?VenueKind.Amusement:null;
            float nearest=float.MaxValue;foreach(var candidate in FourCityCatalog.Venues){if(Has(question,candidate.title)){v=candidate;break;}if(requested.HasValue&&candidate.kind==requested.Value){float d=(candidate.position-npc.transform.position).sqrMagnitude;if(d<nearest){nearest=d;v=candidate;}}}
            var local=npc.GetComponent<VenueActor>();if(local&&local.venue&&!requested.HasValue)v=local.venue.Definition;
            if(v!=null&&(local||(v.position-npc.transform.position).sqrMagnitude<220*220||Has(question,"축구","야구","농구","레이싱","영화","호텔","놀이공원"))){answer="네, 안내해 드릴게요. "+v.title+"에서는 "+For(v)+(v.Sport?" 매표소의 '오늘 경기'에서 점수와 경기 시작 시간을 확인하고, 시작 전 홈 또는 원정 팀 승리에 100 C를 예측할 수 있어요.":"");return true;}
            if(Has(question,"승강기","엘리베이터")){answer="승강기 앞 단말에 가까이 가서 E로 호출하세요. 문이 열리면 객실 안에 들어가 E를 누르고 목적 층을 선택하면 됩니다. 도착한 뒤 통로로 걸어 나오세요.";return true;}
            return false;
        }
        static bool Has(string text,params string[] words){foreach(var word in words)if(text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0)return true;return false;}
    }
}
