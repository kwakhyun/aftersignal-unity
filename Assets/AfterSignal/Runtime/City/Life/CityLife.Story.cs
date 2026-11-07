using System;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public string StorySpeaker {get;private set;}
        StoryVoice storyVoice;
        bool voiceCatalogChecked;
        int storyFilter;
        const int StoryPage=4;
        static readonly string[] StoryFilters={"전체","진행 가능","완료"};
        public void StoryJournal()
        {
            var c=CityChronicle.Instance;if(!c)return;
            int mains=c.Quests.Count(q=>q.main),open=c.Votes(1),consent=c.Votes(2);
            Panel("journal","애프터라이트 사건 일지","지워진 열일곱 분 · 기억 유출 사건\n메인 "+c.CompletedMain+"/"+mains+" · 서브 "+c.Quests.Count(q=>!q.main&&c.Done(q))+"/"+(c.Quests.Length-mains)+(open+consent>0?" · 결정 공개 "+open+" / 동의·보호 "+consent:"")+"\n\n추적 중: "+(c.Tracked?.title??"없음")+"\n"+(c.CurrentStep?.label??"모든 단서 확인")+"\n\n주민 의뢰는 메인 사건의 진행과 앞선 의뢰에 따라 열립니다. [J / ESC 닫기]");
            Option("메인 이야기",()=>StoryList(true,0));Option("주민 연계 의뢰",()=>StoryList(false,0));
            if(FourCityCampaign.Instance)Option("네 도시의 증언 · 시설 내부 캠페인",FourCityJournal);
            if(c.Tracked!=null)Option("현재 의뢰 상세",()=>StoryDetails(c.Tracked,-1));
            Option("최근 대화 기록",()=>DialogueHistory());
            Option("닫기",Dismiss);
        }
        bool StoryShown(CityChronicle c,StoryQuest q)=>storyFilter==1?c.Available(q)&&!c.Done(q):storyFilter!=2||c.Done(q);
        void StoryList(bool main,int page)
        {
            var c=CityChronicle.Instance;var list=c.Quests.Where(q=>q.main==main&&StoryShown(c,q)).ToArray();int pages=Mathf.Max(1,(list.Length+StoryPage-1)/StoryPage);page=Mathf.Clamp(page,0,pages-1);int current=page;
            int arcs=c.Quests.Where(q=>!q.main).Select(q=>q.arc).Distinct().Count();
            Panel("journal",main?"메인 · 지워진 열일곱 분":"서브 · 도시의 이야기 "+arcs+"편","보기: "+StoryFilters[storyFilter]+" · "+list.Length+"건 · 목록 "+(page+1)+" / "+pages+"\n"+(list.Length==0?"이 보기에 해당하는 의뢰가 없습니다. 보기를 바꿔 보세요.":"의뢰를 선택하면 줄거리, 선행 조건, 진행 단계와 보상을 확인합니다."));
            foreach(var q in list.Skip(page*StoryPage).Take(StoryPage)){var selected=q;Option((c.Done(q)?"✓ 완료 · ":!c.Available(q)?"[잠김] 대기 · ":c.Tracked==q?"◆ 추적 중 · ":"")+" "+q.title,()=>StoryDetails(selected,current));}
            Option("보기 전환 → "+StoryFilters[(storyFilter+1)%StoryFilters.Length],()=>{storyFilter=(storyFilter+1)%StoryFilters.Length;StoryList(main,0);});
            Option("이전",()=>{if(current==0)StoryJournal();else StoryList(main,current-1);});
            Option(current+1<pages?"다음":"일지 처음으로",()=>{if(current+1<pages)StoryList(main,current+1);else StoryJournal();});
        }
        static string StoryKind(StoryStep s)=>s.kind=="meet"?"대화":s.kind=="scan"?"조사":s.kind=="deliver"?"전달":s.kind=="choice"?"결정":s.kind=="combat"?"교전":"전투";
        // page < 0: opened from the journal front page, so "목록" lands on the page that lists this quest.
        void StoryDetails(StoryQuest q,int page)
        {
            var c=CityChronicle.Instance;var e=c.Entry(q);
            string prerequisite=string.IsNullOrEmpty(q.after)?"없음":c.Quests.First(p=>p.id==q.after).title;
            string text=q.arc+"\n"+q.summary+"\n\n선행 의뢰: "+prerequisite+" · 보상 "+q.reward+" C\n";
            for(int i=0;i<q.steps.Length;i++){var s=q.steps[i];text+=(i<e.step?"✓ ":i==e.step?"→ ":"· ")+"["+StoryKind(s)+"] "+s.label+" / "+(s.world?FourCityCatalog.CityNames[FourCityCatalog.CityAt(s.position)]:UrbanCatalog.Name(s.site))+"\n";}
            if(e.choice!=0){var decision=q.steps.FirstOrDefault(s=>s.kind=="choice");text+="결정: "+(decision!=null?CityChronicle.ChoiceLabel(decision,e.choice):e.choice==1?"공개와 공동 감독":"당사자 동의와 보호")+"\n";}
            var last=q.steps[q.steps.Length-1];if(c.Done(q))text+="\n"+(q.finale?c.Ending(q):last.kind=="battle"&&!string.IsNullOrEmpty(last.outro)?last.outro:last.line);
            Panel("journal",q.title,text);
            if(!c.Done(q)&&c.Available(q))Option(e.accepted?"이 의뢰 추적":"수락하고 추적",()=>{c.Track(q);Dismiss();});
            Option("목록",()=>
            {
                if(page<0){if(!StoryShown(c,q))storyFilter=0;page=Mathf.Max(0,Array.IndexOf(c.Quests.Where(p=>p.main==q.main&&StoryShown(c,p)).ToArray(),q))/StoryPage;}
                StoryList(q.main,page);
            });
            Option("닫기",Dismiss);
        }
        public void StoryDialogue(string title,string speaker,string text,bool choice,Action<int> done,string optionA=null,string optionB=null)
        {
            StorySpeaker=speaker;
            Panel("story",title+" · "+speaker,text);
            if(!voiceCatalogChecked)
            {
                voiceCatalogChecked=true;
                storyVoice=StoryVoice.Create(game,Resources.Load<TextAsset>(StoryVoice.CatalogPath));
            }
            if(storyVoice)storyVoice.PlayLine(speaker,text);
            if(choice){Option(string.IsNullOrEmpty(optionA)?CityChronicle.DefaultChoiceA:optionA,()=>{Dismiss();done(1);});Option(string.IsNullOrEmpty(optionB)?CityChronicle.DefaultChoiceB:optionB,()=>{Dismiss();done(2);});}
            else Option("확인 · 다음 단계",()=>{Dismiss();done(0);});
        }
    }
}
