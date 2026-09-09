using System;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public string StorySpeaker {get;private set;}
        public void StoryJournal()
        {
            var c=CityChronicle.Instance;if(!c)return;
            Panel("journal","애프터라이트 사건 일지","지워진 열일곱 분 · 기억 유출 사건\n메인 "+c.CompletedMain+"/24 · 서브 "+c.Quests.Count(q=>!q.main&&c.Done(q))+"/18\n\n추적 중: "+(c.Tracked?.title??"없음")+"\n"+(c.CurrentStep?.label??"모든 단서 확인")+"\n\n주민 의뢰는 메인 사건의 진행과 앞선 의뢰에 따라 열립니다. [J / ESC 닫기]");
            Option("메인 이야기",()=>StoryList(true,0));Option("주민 연계 의뢰",()=>StoryList(false,0));
            if(FourCityCampaign.Instance)Option("네 도시의 증언 · 시설 내부 캠페인",FourCityJournal);
            if(c.Tracked!=null)Option("현재 의뢰 상세",()=>StoryDetails(c.Tracked));
            Option("닫기",Dismiss);
        }
        void StoryList(bool main,int page)
        {
            var c=CityChronicle.Instance;var list=c.Quests.Where(q=>q.main==main).ToArray();int pages=(list.Length+4)/5;page=Mathf.Clamp(page,0,pages-1);int current=page;
            Panel("journal",main?"메인 · 지워진 열일곱 분":"서브 · 도시의 여섯 이야기","목록 "+(page+1)+" / "+pages+"\n의뢰를 선택하면 줄거리, 선행 조건, 진행 단계와 보상을 확인합니다.");
            foreach(var q in list.Skip(page*5).Take(5)){var selected=q;Option((c.Done(q)?"✓ 완료 · ":!c.Available(q)?"[잠김] 대기 · ":"")+" "+q.title,()=>StoryDetails(selected));}
            Option("이전",()=>{if(current==0)StoryJournal();else StoryList(main,current-1);});
            Option(current+1<pages?"다음":"일지 처음으로",()=>{if(current+1<pages)StoryList(main,current+1);else StoryJournal();});
        }
        void StoryDetails(StoryQuest q)
        {
            var c=CityChronicle.Instance;var e=c.Entry(q);
            string prerequisite=string.IsNullOrEmpty(q.after)?"없음":c.Quests.First(p=>p.id==q.after).title;
            string text=q.arc+"\n"+q.summary+"\n\n선행 의뢰: "+prerequisite+" · 보상 "+q.reward+" C\n";
            for(int i=0;i<q.steps.Length;i++)text+=(i<e.step?"✓ ":i==e.step?"→ ":"· ")+q.steps[i].label+" / "+UrbanCatalog.Name(q.steps[i].site)+"\n";
            if(e.choice!=0)text+="결정: "+(e.choice==1?"공개와 공동 감독":"당사자 동의와 보호")+"\n";
            if(c.Done(q))text+="\n"+q.steps[q.steps.Length-1].line;
            Panel("journal",q.title,text);
            if(!c.Done(q)&&c.Available(q))Option(e.accepted?"이 의뢰 추적":"수락하고 추적",()=>{c.Track(q);Dismiss();});
            Option("목록",()=>StoryList(q.main,0));Option("닫기",Dismiss);
        }
        public void StoryDialogue(string title,string speaker,string text,bool choice,Action<int> done)
        {
            StorySpeaker=speaker;
            Panel("story",title+" · "+speaker,text);
            if(choice){Option("기록 공개 · 공동 감독",()=>{Dismiss();done(1);});Option("당사자 동의 · 보호 우선",()=>{Dismiss();done(2);});}
            else Option("확인 · 다음 단계",()=>{Dismiss();done(0);});
        }
    }
}
