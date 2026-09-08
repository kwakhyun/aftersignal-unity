using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public bool Working {get;private set;}
        public float Preparation => preparing ? Mathf.PingPong((Time.unscaledTime-preparationStart)*.65f,1):0;
        bool cafeShift,preparing,carrying;
        int selectedTable=-1,served,wage,quality;
        readonly int[] orders=new int[3];
        readonly float[] patience=new float[3];
        float preparationStart,shiftRefresh;
        string[] Recipes=>cafeShift?new[]{"아메리카노","카페라테","바닐라라테"}:new[]{"따뜻한 정식","오늘의 파스타","구운 채소 덮밥"};
        public void BeginShift(bool cafe)
        {
            if(WantedSystem.Level>0){game.Toast("수배 중에는 근무를 시작할 수 없습니다.");return;}
            cafeShift=cafe;Working=true;served=wage=0;selectedTable=-1;preparing=carrying=false;
            for(int i=0;i<3;i++){orders[i]=(i+LifeState.Day)%3;patience[i]=95+i*18;}
            RefreshShift();
        }
        void RefreshShift()
        {
            if(!Working)return;
            Panel("shift",cafeShift?"바람카페 · 바리스타 아르바이트":"달빛식당 · 홀 서빙 아르바이트","");
            Body="완료 "+served+"/5명 · 받은 급여 "+wage+" C\n";
            for(int i=0;i<3;i++) Body+=(i+1)+"번 테이블: "+Recipes[orders[i]]+" · "+Mathf.CeilToInt(patience[i])+"초 남음\n";
            if(preparing)
            {
                Body+="\n"+(cafeShift?"에스프레소 추출 / 우유 데우기":"음식 조리")+" · 표시가 중앙에 올 때 멈추세요.";
                Option("지금 멈추기",FinishPreparation);
            }
            else if(carrying)
            {
                Body+="\n쟁반에 "+Recipes[orders[selectedTable]]+" · 주문한 테이블에 서빙하세요.";
                for(int i=0;i<3;i++){int table=i;Option((i+1)+"번 테이블에 서빙",()=>ServeTable(table));}
            }
            else if(selectedTable>=0)
            {
                Body+="\n"+(selectedTable+1)+"번 손님 주문을 확인하고 알맞은 "+(cafeShift?"음료":"음식")+"를 선택하세요.";
                for(int i=0;i<3;i++){int recipe=i;Option(Recipes[i]+(cafeShift?" 만들기":" 조리"),()=>PrepareOrder(recipe));}
            }
            else for(int i=0;i<3;i++){int table=i;Option((i+1)+"번 손님 응대 / 주문 받기",()=>{selectedTable=table;RefreshShift();});}
            Option("근무 마치기",EndShift);
            Revision++;
        }
        public void PrepareOrder(int recipe)
        {
            if(selectedTable<0||preparing||carrying)return;
            if(recipe!=orders[selectedTable]){patience[selectedTable]=Mathf.Max(3,patience[selectedTable]-15);game.Toast("주문과 다릅니다. 손님의 주문을 확인하세요.");RefreshShift();return;}
            preparing=true;preparationStart=Time.unscaledTime;RefreshShift();
        }
        public void FinishPreparation()
        {
            if(!preparing)return;
            quality=Mathf.Abs(Preparation-.5f)<.15f?25:Mathf.Abs(Preparation-.5f)<.3f?10:0;
            preparing=false;carrying=true;RefreshShift();
        }
        public void ServeTable(int table)
        {
            if(!carrying||selectedTable<0)return;
            if(table!=selectedTable){game.Toast("다른 손님의 주문입니다. 테이블 번호를 확인하세요.");return;}
            int pay=(cafeShift?40:55)+quality;
            LifeState.Earn(pay);wage+=pay;served++;
            LifeState.Hours+=.25f;LifeState.Save();
            patience[table]=110;orders[table]=(orders[table]+1)%3;selectedTable=-1;carrying=false;
            game.Audio.Play("ui_confirm",game.Player.Shoulder,.2f,1);
            if(served>=5){EndShift();game.Toast("근무 완료 · 급여 "+wage+" C",6);}
            else RefreshShift();
        }
        void UpdateShift()
        {
            if(!Working)return;
            if(Mode!="shift"||!game.Dialogue){Working=false;return;}
            if(game.Paused||game.Dead)return;
            for(int i=0;i<3;i++)
            {
                patience[i]-=Time.unscaledDeltaTime;
                if(patience[i]<=0)
                {
                    if(selectedTable==i){selectedTable=-1;preparing=carrying=false;}
                    orders[i]=(orders[i]+1)%3;patience[i]=110;
                    game.Toast((i+1)+"번 손님이 떠났습니다. 새 손님을 응대하세요.");
                }
            }
            if(Time.unscaledTime>shiftRefresh){shiftRefresh=Time.unscaledTime+.2f;RefreshShift();}
        }
        void EndShift(){Working=false;preparing=carrying=false;selectedTable=-1;Dismiss();game.Toast("근무 종료 · 받은 급여 "+wage+" C",5);}
    }
}
