using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void FourCityJournal()
        {
            var c=FourCityCampaign.Instance;if(!c)return;
            Panel("journal","네 도시의 증언",c.Complete?"12개 장의 기록을 복원했습니다.\n선택: "+(c.Choice==1?"기록 공개와 공동 감독":"당사자 동의와 보호"):c.Current.title+"\n\n"+c.Current.objective+"\n시설: "+FourCityCatalog.Venues[c.Current.facility].title+" · "+(c.Current.floor+1)+"층\n\n도시를 오가며 건물 내부의 기록 단말을 조사하세요. 위협이 있는 곳에서는 잠식체를 먼저 제압해야 합니다.\n진행 "+c.Step+" / 12 · 남은 위협 "+c.Remaining);
            if(!c.Complete)Option("지도에서 목적지 표시",()=>{FourCityAtlasSelection.Select(FourCityCatalog.Venues[c.Current.facility],true);Dismiss();UrbanSimulation.Instance?.OpenMap();});
            Option("기존 사건 일지",StoryJournal);Option("닫기",Dismiss);
        }
    }
}
