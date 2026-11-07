using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class FacilityOperation:MonoBehaviour
    {
        // Quick: one E press. Hold: E, then stay at the station while the task runs. Carry: E picks up
        // the parts, a second E at the drop-off point finishes the step.
        enum Task{Quick,Hold,Carry}
        const float HoldSeconds=4,HoldRadius=3.4f;
        public static FacilityOperation Current {get;private set;}
        public int Completed {get;private set;}public Vector3 Target=>node?node.transform.position:transform.position;
        public string Label {get;private set;}
        public bool Carrying=>carrying;public bool Holding=>holding;public float HoldProgress=>holding?Mathf.Clamp01(held/HoldSeconds):0;
        string id,stamp,step;int reward;float hours,held;bool carrying,holding;GameObject node;readonly List<Vector3> stations=new();readonly Task[] plan=new Task[3];
        // One key per job id holding "campaignSerial:day" (bounded) instead of one key per job per day.
        // The in-memory copy (the only record while saving is suppressed) belongs to one slot generation: another slot or a New Game starts clean.
        static readonly Dictionary<string,string> done=new();static int doneGeneration=-1;
        static Dictionary<string,string> Done{get{if(doneGeneration!=SaveStore.Generation){done.Clear();doneGeneration=SaveStore.Generation;}return done;}}
        static string Key(string id)=>"AFTERSIGNAL.Unity.FacilityJobDay."+id;
        static bool DoneToday(string id,string stamp)
        {
            if(Done.TryGetValue(id,out var day)&&day==stamp)return true;
            return SaveStore.GetString(Key(id),"")==stamp||SaveStore.GetInt("AFTERSIGNAL.Unity.FacilityJob."+LifeState.CampaignSerial+"-"+id+"-"+LifeState.Day,0)>0;
        }
        public static bool Begin(string id,int reward,float hours)
        {
            var g=GameDirector.Instance;if(!g)return false;
            if(Current){g.Toast("진행 중인 현장 업무를 먼저 마무리하세요.");return false;}
            string stamp=LifeState.CampaignSerial+":"+LifeState.Day;
            if(DoneToday(id,stamp)){g.Toast("오늘 이 업무는 이미 완료했습니다.");return false;}
            if(WantedSystem.Level>0){g.Toast("수배 중에는 업무를 받을 수 없습니다.");return false;}
            var go=new GameObject("Facility work / "+id);var op=go.AddComponent<FacilityOperation>();Current=op;op.id=id;op.stamp=stamp;op.reward=Mathf.RoundToInt(reward*DifficultySettings.Payout);op.hours=hours;
            var origin=g.Player.transform.position;
            for(int i=0;i<96&&op.stations.Count<4;i++)
            {
                float angle=i*2.39996f;var p=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(3+i%6*1.5f);
                if(Physics.Raycast(p+Vector3.up,Vector3.down,out var h,2.1f,1,QueryTriggerInteraction.Ignore)&&h.normal.y>.8f&&Mathf.Abs(h.point.y-origin.y)<.4f&&!Physics.CheckCapsule(h.point+Vector3.up*.45f,h.point+Vector3.up*1.7f,.4f,1,QueryTriggerInteraction.Ignore)&&op.stations.TrueForAll(v=>(v-h.point).sqrMagnitude>5))op.stations.Add(h.point+Vector3.up*.05f);
            }
            if(op.stations.Count<3){Destroy(go);Current=null;g.Toast("업무 공간이 부족합니다. 시설 안의 넓은 곳에서 다시 시작하세요.");return false;}
            int seed=LifeState.Day;foreach(char c in id)seed=seed*31+c;seed&=int.MaxValue;
            // The fourth free spot is the drop-off; without it the middle task becomes a timed hold.
            op.plan[0]=Task.Quick;op.plan[1]=op.stations.Count>3&&seed%2==0?Task.Carry:Task.Hold;op.plan[2]=seed%3==1?Task.Hold:Task.Quick;
            g.CloseDialogue();op.Next();g.Toast("현장 업무 시작 · 표시된 작업 지점 3곳에서 E · 운반·자리 유지 작업이 섞여 있습니다",5);return true;
        }
        void Next()
        {
            if(node)Destroy(node);carrying=holding=false;held=0;
            if(Completed>=3)
            {
                Done[id]=stamp;if(!LifeState.SuppressSave){SaveStore.SetString(Key(id),stamp);SaveStore.Save();}
                int pay=SaveStore.GetInt("AFTERSIGNAL.Unity.CivicPermit",0)>0?Mathf.RoundToInt(reward*1.1f):reward;LifeState.Hours+=hours;LifeState.Earn(pay);GameDirector.Instance.Toast("현장 업무 완료 · +"+pay+" C",5);Destroy(gameObject);return;
            }
            step=ActionName(id,Completed);
            Station(stations[Completed],plan[Completed]==Task.Carry?step+" · 물품 들기":plan[Completed]==Task.Hold?step+" · E 후 "+HoldSeconds.ToString("0")+"초 자리 유지":step);
        }
        void Station(Vector3 at,string label)
        {
            Label=label;node=new GameObject("Work station / "+Label);node.transform.position=at;
            WorldGeometry.Part(node.transform,"Task equipment",Vector3.up*.46f,new Vector3(.9f,.9f,.7f),"DarkMetal");WorldGeometry.Part(node.transform,"Status screen",new Vector3(0,1.05f,0),new Vector3(.66f,.38f,.08f),carrying?"DistrictBlue":"CyanFX");
            var point=node.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title="["+(Completed+1)+"/3] "+Label;point.radius=2.7f;node.AddComponent<FacilityWorkPoint>().owner=this;
            SignalEffects.Ring(node.transform.position+Vector3.up*.04f,carrying?SignalEffects.Gold:SignalEffects.Cyan,1.4f,.9f);
        }
        static string ActionName(string id,int n)
        {
            string[] stages=id.Contains("fire")?new[]{"소화전 압력 확인","소방 호스 연결 점검","비상 경보 시험"}:id.Contains("grid")||id.Contains("power")?new[]{"배전반 전압 측정","손상 퓨즈 교체","전력 공급 재연결"}:id.Contains("school")?new[]{"반납 도서 분류","교실 기자재 정리","보건 물품 보충"}:id.Contains("medical")?new[]{"의약품 유효기간 확인","응급 물품 재고 확인","진료 기록 전달"}:id.Contains("registry")||id.Contains("archive")?new[]{"신원·기억 기록 대조","기록 오류 정정","복원망으로 자료 전송"}:id.Contains("military")?new[]{"전술 통신 주파수 동기화","보급품 수량 확인","격납고 무장 상태 확인"}:id.Contains("repair")?new[]{"고장 진단","교체 부품 조립","정비 장비 시험"}:new[]{"작업 물품 분류","수량·상태 검수","처리 결과 등록"};return stages[n];
        }
        public void CompleteStep()
        {
            var g=GameDirector.Instance;if(!node||Vector3.Distance(g.Player.transform.position,node.transform.position)>3.2f)return;
            var task=plan[Completed];
            if(task==Task.Hold){if(!holding){holding=true;held=0;g.Toast(step+" · 작업 중 · 작업 지점을 벗어나지 마세요",2);}return;}
            if(task==Task.Carry&&!carrying)
            {
                carrying=true;Destroy(node);Station(stations[3],step+" · 하역 지점으로 운반");
                g.Audio.Play("ui_confirm",g.Player.Shoulder,.16f,1);g.Toast("물품을 들었습니다 · 표시된 하역 지점에서 E",3);return;
            }
            Completed++;g.Audio.Play("ui_confirm",node.transform.position,.2f,1);Next();
        }
        void Update()
        {
            if(!holding||!node)return;
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            if(Vector3.Distance(g.Player.transform.position,node.transform.position)>HoldRadius){holding=false;held=0;Label=step+" · E 후 "+HoldSeconds.ToString("0")+"초 자리 유지";g.Toast("작업 지점을 벗어나 중단됐습니다 · 다시 E",3);return;}
            held+=Time.deltaTime;Label=step+" · 작업 중 "+Mathf.CeilToInt(Mathf.Max(0,HoldSeconds-held))+"초";
            if(held>=HoldSeconds){Completed++;g.Audio.Play("ui_confirm",node.transform.position,.2f,1);Next();}
        }
        void OnDestroy(){if(node)Destroy(node);if(Current==this)Current=null;}
    }
}
