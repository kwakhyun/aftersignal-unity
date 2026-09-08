using System;

namespace AfterSignal
{
    public enum MatchPhase { Scheduled, Playing, Interval, Final }
    public enum PitchOutcome { Ball, Strike, Foul, Out, Single, Double, Triple, HomeRun }
    /// <summary>Serializable rules state. Presentation consumes events; it never assigns a winner.</summary>
    [Serializable] public sealed class SportsMatch
    {
        public string venue, home="애프터라이트 루멘", away="노바 트라이던트", lastEvent="경기 시작 전 · 승부예측 접수 중";
        public VenueKind sport;
        public MatchPhase phase;
        public int homeScore,awayScore,period=1,inning=1,outs,balls,strikes,bases,battingTeam,possessingTeam;
        public int homeBatter,awayBatter;
        public int homeFouls,awayFouls,homeCards,awayCards,round=1,eventNumber,passes;
        public float clock,wait=70,eventClock,shotClock=24,finalSeconds;
        public int stake,selection=-1,payout;
        public bool settled;
        public uint random=732451;
        public float[] raceDistance=new float[6];
        public int[] raceFinish=new int[6];
        public int finishCount;
        public float[] racePace=new float[6];
        public float ballX,ballZ,ballHeight;
        public int Winner => phase!=MatchPhase.Final?-2:homeScore==awayScore?-1:homeScore>awayScore?0:1;
        public bool CanBet => phase==MatchPhase.Scheduled&&wait>0&&stake==0;
        public int AttackDirection(int team)=>(team==0?1:-1)*(sport==VenueKind.Football&&period>1||sport==VenueKind.Basketball&&period>2?-1:1);
        public int Batter=>battingTeam==0?homeBatter:awayBatter;
        public float Roll(){random^=random<<13;random^=random>>17;random^=random<<5;return (random&0xFFFFFF)/16777216f;}
        public SportsMatch(){}
        public SportsMatch(CityVenue v){venue=v.id;sport=v.kind;random=(uint)(13491+Array.IndexOf(FourCityCatalog.Venues,v)*8723);for(int i=0;i<6;i++)racePace[i]=(Roll()-.5f)*4;if(v.city==1){home="노바 트라이던트";away="애프터라이트 루멘";} }
        public static bool Offside(float receiverX,float ballAtPass,float secondLastDefenderX,int direction,bool exemptRestart=false)=>!exemptRestart&&receiverX*direction>0&&receiverX*direction>ballAtPass*direction&&receiverX*direction>secondLastDefenderX*direction;
        public void Begin(){phase=MatchPhase.Playing;wait=0;clock=0;eventClock=0;lastEvent="경기 시작";eventNumber++;}
        public void Tick(float dt)
        {
            if(dt<=0)return;
            if(phase==MatchPhase.Scheduled){wait-=dt;if(wait<=0)Begin();return;}
            if(phase==MatchPhase.Final){finalSeconds+=dt;return;}
            if(phase==MatchPhase.Interval){wait-=dt;if(wait<=0){phase=MatchPhase.Playing;clock=0;shotClock=24;lastEvent="경기 재개";}return;}
            if(sport==VenueKind.Circuit){Race(dt);return;}
            clock+=dt*(sport==VenueKind.Football?18:sport==VenueKind.Basketball?8:1);
            if(sport==VenueKind.Basketball){shotClock-=dt*8;if(shotClock<=0){Turnover("24초 공격 제한 위반");}}
            eventClock-=dt;
            if(eventClock<=0){eventClock=sport==VenueKind.Baseball?1.8f:sport==VenueKind.Basketball?1.2f:2.8f;
                if(sport==VenueKind.Baseball)SimulatePitch();else if(sport==VenueKind.Basketball)BasketballPossession();else FootballPlay();}
            if(sport==VenueKind.Football&&clock>=2700){if(period==1){period=2;Interval(14,"하프타임 · 진영 교대");}else Finish("정규시간 종료");}
            if(sport==VenueKind.Basketball&&clock>=(period<=4?600:300))
            {
                if(period>=4&&homeScore!=awayScore){Finish("경기 종료");return;}
                period++;homeFouls=awayFouls=0;Interval(period==3?14:8,period>4?"동점 · 5분 연장전":"쿼터 종료");
            }
        }
        void Event(string line){lastEvent=line;eventNumber++;}
        void Interval(float duration,string line){phase=MatchPhase.Interval;wait=duration;Event(line);}
        public void Finish(string line){phase=MatchPhase.Final;finalSeconds=0;Event(line+(homeScore==awayScore?" · 무승부":homeScore>awayScore?" · 홈 팀 승리":" · 원정 팀 승리"));}
        void Score(int team,int points){if(team==0)homeScore+=points;else awayScore+=points;}
        void Turnover(string line){possessingTeam=1-possessingTeam;shotClock=24;passes=0;Event(line);}
        void FootballPlay()
        {
            float r=Roll();passes++;
            int direction=AttackDirection(possessingTeam);float previous=ballX;float receiver=System.Math.Clamp(ballX+direction*(6+Roll()*12),-51,51);bool offside=Offside(receiver,previous,direction*(32+Roll()*15),direction);ballX=receiver;ballZ=(Roll()-.5f)*55;ballHeight=Roll()<.2f?2.5f:0;
            if(r<.10f){Turnover("수비 인터셉트 · 역습");return;}
            if(offside){Turnover("오프사이드 · 간접 프리킥");ballX*=.65f;return;}
            if(r<.24f)
            {
                int defending=1-possessingTeam;bool caution=Roll()<.2f;if(caution){if(defending==0)homeCards++;else awayCards++;}
                Event(caution?"반칙 · 경고, 프리킥":"반칙 · 프리킥");
                if(Math.Abs(ballX)>36&&Math.Abs(ballZ)<20&&Roll()<.24f){if(Roll()<.76f){Score(possessingTeam,1);Event("페널티킥 득점!");}else Event("페널티킥 · 골키퍼 선방");ballX=ballZ=0;possessingTeam=1-possessingTeam;}
                return;
            }
            if(Math.Abs(ballX)>40&&passes>2)
            {if(Roll()<.12f){Score(possessingTeam,1);Event("골! 중앙에서 킥오프");ballX=ballZ=0;possessingTeam=1-possessingTeam;passes=0;}else if(Roll()<.45f){ballX=direction*50;ballZ=32;Event("수비 굴절 · 코너킥");}else{Turnover("슈팅 · 골키퍼 선방 / 골킥");ballX=direction*40;} }
            else Event(passes%3==0?"측면 전환 · 침투 패스":"중원 패스 연결");
        }
        void BasketballPossession()
        {
            float r=Roll();int team=possessingTeam;ballX=AttackDirection(team)*(6+Roll()*6);ballZ=(Roll()-.5f)*11;
            if(r<.13f){Turnover("스틸 · 공격권 전환");return;}
            if(r<.23f)
            {
                int defending=1-team;if(defending==0)homeFouls++;else awayFouls++;
                int fouls=defending==0?homeFouls:awayFouls;
                if(fouls>=5||Roll()<.55f){int made=0;for(int i=0;i<2;i++)if(Roll()<.76f)made++;Score(team,made);Event("파울 · 자유투 "+made+"/2 성공");Turnover(lastEvent);}else {shotClock=Math.Max(14,shotClock);Event("수비 파울 · 사이드라인 스로인");}return;
            }
            if(r<.38f&&shotClock>8){passes++;Event("패스 · 공격 전개");return;}
            bool three=Roll()<.37f;ballX=AttackDirection(team)*(three?5.5f:11.5f);ballHeight=3.05f;
            if(Roll()<(three?.35f:.53f)){Score(team,three?3:2);Turnover(three?"3점 슛 성공!":"2점 슛 성공!");}
            else if(Roll()<.27f){shotClock=14;Event("공격 리바운드 · 14초 재설정");}else Turnover("수비 리바운드");
        }
        void SimulatePitch()
        {
            float r=Roll();ApplyPitch(r<.30f?PitchOutcome.Ball:r<.53f?PitchOutcome.Strike:r<.65f?PitchOutcome.Foul:r<.86f?PitchOutcome.Out:r<.95f?PitchOutcome.Single:r<.982f?PitchOutcome.Double:r<.991f?PitchOutcome.Triple:PitchOutcome.HomeRun);
        }
        public void ApplyPitch(PitchOutcome result)
        {
            if(phase!=MatchPhase.Playing||sport!=VenueKind.Baseball)return;
            bool plateComplete=result==PitchOutcome.Out||(int)result>=(int)PitchOutcome.Single||result==PitchOutcome.Ball&&balls==3||result==PitchOutcome.Strike&&strikes==2;
            ballX=0;ballZ=-44;ballHeight=.7f;
            if(result==PitchOutcome.Ball){balls++;Event("볼 "+balls+" · 스트라이크 "+strikes);if(balls>=4){Walk();ResetCount();Event("볼넷 · 1루 출루");}}
            else if(result==PitchOutcome.Strike){strikes++;Event("스트라이크 "+strikes);if(strikes>=3){outs++;ResetCount();Event("삼진 아웃");}}
            else if(result==PitchOutcome.Foul){if(strikes<2)strikes++;Event("파울 · "+balls+"볼 "+strikes+"스트라이크");}
            else if(result==PitchOutcome.Out){outs++;ResetCount();Event("타구 처리 · 아웃");}
            else
            {
                ballX=(Roll()-.5f)*70;ballZ=5+Roll()*52;
                int advance=result==PitchOutcome.Single?1:result==PitchOutcome.Double?2:result==PitchOutcome.Triple?3:4;
                int next=0,runs=advance==4?1:0;
                for(int b=0;b<3;b++)if((bases&(1<<b))!=0){int target=b+advance;if(target>=3)runs++;else next|=1<<target;}
                if(advance<4)next|=1<<(advance-1);bases=next;Score(battingTeam,runs);ResetCount();ballHeight=advance>=2?8:1;
                Event(advance==4?"홈런! "+runs+"점":advance+"루타 · "+runs+"점");
            }
            if(plateComplete){if(battingTeam==0)homeBatter=(homeBatter+1)%9;else awayBatter=(awayBatter+1)%9;}
            if(inning>=9&&battingTeam==0&&homeScore>awayScore){Finish("끝내기");return;}
            if(outs>=3)
            {
                bases=outs=0;ResetCount();
                if(battingTeam==1){if(inning>=9&&homeScore>awayScore){Finish("9회초 종료 · 홈 팀 승리");return;}battingTeam=0;}
                else {if(inning>=9&&homeScore!=awayScore){Finish("이닝 종료");return;}inning++;battingTeam=1;}
                if(inning>9)bases=2; // MLB regular-season automatic runner at second base.
                Event(inning+"회 "+(battingTeam==1?"초":"말")+" · 공수 교대");
            }
        }
        void Walk(){if((bases&1)!=0){if((bases&2)!=0){if((bases&4)!=0)Score(battingTeam,1);bases|=4;}bases|=2;}bases|=1;}
        void ResetCount(){balls=strikes=0;}
        void Race(float dt)
        {
            clock+=dt;const float length=1500;
            for(int i=0;i<6;i++)
            {if(raceFinish[i]>0)continue;float speed=42+racePace[i]+(float)Math.Sin(clock*.17f+i*1.73f)*3;raceDistance[i]+=speed*dt;if(raceDistance[i]>=length*8){raceDistance[i]=length*8;raceFinish[i]=++finishCount;Score(i%2,new[]{10,8,6,4,2,1}[finishCount-1]);Event((i+1)+"번 차량 체커기 · "+finishCount+"위");}}
            if(finishCount==6)Finish("8랩 종료 · 팀 합산 포인트 확정");
        }
        public string Status=>phase==MatchPhase.Scheduled?"시작까지 "+(int)Math.Max(0,wait)+"초":phase==MatchPhase.Final?"최종 결과":sport==VenueKind.Baseball?inning+"회 "+(battingTeam==1?"초":"말")+"  "+outs+" OUT  "+balls+"B "+strikes+"S":sport==VenueKind.Circuit?"8랩 · 피니시 "+finishCount+" / 6":sport==VenueKind.Football?(period==1?"전반 ":"후반 ")+((int)(clock/60)+(period==2?45:0))+":"+((int)clock%60).ToString("00"):(period<=4?period+"Q":"OT "+(period-4))+"  "+(int)(Math.Max(0,(period<=4?600:300)-clock)/60)+":"+((int)Math.Max(0,(period<=4?600:300)-clock)%60).ToString("00");
    }
}
