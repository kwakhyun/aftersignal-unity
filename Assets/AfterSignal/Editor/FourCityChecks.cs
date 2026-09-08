using System;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEngine;
namespace AfterSignal.Editor
{
    public static class FourCityChecks
    {
        [Serializable]sealed class Report{public List<string> passed=new(),failed=new();}
        public static void Rules()
        {
            var r=new Report();void Check(bool ok,string name){(ok?r.passed:r.failed).Add(name);}
            SportsMatch Match(VenueKind sport){var m=new SportsMatch(FourCityCatalog.Venues.First(v=>v.kind==sport));m.Begin();if(sport==VenueKind.Baseball)m.battingTeam=1;return m;}
            var b=Match(VenueKind.Baseball);b.strikes=2;b.ApplyPitch(PitchOutcome.Foul);Check(b.strikes==2&&b.outs==0,"Two-strike foul is not a strikeout");b.ApplyPitch(PitchOutcome.Strike);Check(b.outs==1&&b.strikes==0,"Third strike records an out and resets count");
            b.bases=7;b.balls=3;b.ApplyPitch(PitchOutcome.Ball);Check(b.awayScore==1&&b.bases==7,"Bases-loaded walk forces exactly one run");b.ApplyPitch(PitchOutcome.HomeRun);Check(b.awayScore==5&&b.bases==0,"Grand slam scores all runners and batter");
            b=Match(VenueKind.Baseball);b.inning=9;b.homeScore=1;b.outs=2;b.ApplyPitch(PitchOutcome.Out);Check(b.phase==MatchPhase.Final,"Leading home team skips bottom ninth");
            b=Match(VenueKind.Baseball);b.inning=9;b.battingTeam=0;b.ApplyPitch(PitchOutcome.HomeRun);Check(b.phase==MatchPhase.Final&&b.Winner==0,"Bottom-ninth walkoff ends immediately");
            b=Match(VenueKind.Baseball);b.inning=9;b.battingTeam=0;b.outs=2;b.ApplyPitch(PitchOutcome.Out);Check(b.inning==10&&b.battingTeam==1&&b.bases==2,"Tied ninth enters extra innings with runner on second");
            var basket=Match(VenueKind.Basketball);basket.period=4;basket.clock=599.99f;basket.eventClock=10;basket.Tick(.01f);Check(basket.period==5&&basket.phase==MatchPhase.Interval,"Tied fourth quarter enters overtime");basket.wait=0;basket.Tick(.01f);basket.homeScore=1;basket.clock=299.99f;basket.eventClock=10;basket.Tick(.01f);Check(basket.phase==MatchPhase.Final&&basket.Winner==0,"Overtime ends only with unequal scores");
            basket=Match(VenueKind.Basketball);basket.eventClock=10;basket.shotClock=.01f;basket.Tick(.01f);Check(basket.possessingTeam==1&&basket.shotClock==24,"Shot clock violation transfers possession");
            var f=Match(VenueKind.Football);f.clock=2699.99f;f.eventClock=10;f.Tick(.01f);Check(f.period==2&&f.phase==MatchPhase.Interval,"Football halftime after 45 minutes");f.wait=0;f.Tick(.01f);f.clock=2699.99f;f.eventClock=10;f.Tick(.01f);Check(f.phase==MatchPhase.Final&&f.Winner==-1,"Football draw preserved after 90 minutes");
            Check(SportsMatch.Offside(42,30,39,1)&&!SportsMatch.Offside(42,43,39,1)&&!SportsMatch.Offside(-10,-20,-15,1)&&!SportsMatch.Offside(42,30,39,1,true),"Offside checks ball, defender, opposing half and restart exemption");
            Check(f.AttackDirection(0)==-1&&f.AttackDirection(1)==1,"Football teams exchange ends after halftime");
            b=Match(VenueKind.Baseball);int batter=b.Batter;b.ApplyPitch(PitchOutcome.Ball);b.ApplyPitch(PitchOutcome.Strike);Check(b.Batter==batter,"Batter remains at plate between pitches");b.ApplyPitch(PitchOutcome.Single);Check(b.Batter==(batter+1)%9,"Batting order advances only after completed plate appearance");
            var race=Match(VenueKind.Circuit);for(int i=0;i<5000&&race.phase!=MatchPhase.Final;i++)race.Tick(.1f);Check(race.finishCount==6&&race.raceFinish.Distinct().Count()==6&&race.homeScore+race.awayScore==31,"All six race cars complete eight laps with unique places and team points");
            foreach(var kind in new[]{VenueKind.Football,VenueKind.Baseball,VenueKind.Basketball}){var m=Match(kind);for(int i=0;i<90000&&m.phase!=MatchPhase.Final;i++)m.Tick(.1f);Check(m.phase==MatchPhase.Final&&m.homeScore>=0&&m.awayScore>=0,kind+" complete simulated match");}
            Check(FourCityCatalog.Venues.Length==28&&FourCityCatalog.Venues.Select(v=>v.id).Distinct().Count()==28,"28 uniquely addressable facilities across four cities");
            Check(FourCityCatalog.Dry(FourCityCatalog.Centers[3]+Vector3.up)&&!OceanLife.Contains(FourCityCatalog.Centers[3]+Vector3.up),"Underwater city is a breathable dry volume");
            Check(!FourCityCatalog.Dry(new(5300,-50,-4400))&&OceanLife.Contains(new(5300,-50,-4400)),"Outside the pressure dome remains underwater");
            var path=FourCityNavigation.Route(new(880,.1f,1040),FourCityCatalog.Venues[2].Entrance);Check(path.Count>4&&path.First().x==880&&Vector3.Distance(path.Last(),FourCityCatalog.Venues[2].Entrance)<.1f,"Road graph connects old city to sports district");
            var underwater=FourCityNavigation.Route(new(2000,.1f,-4050),FourCityCatalog.Venues[23].Entrance);Check(underwater.Count>4&&underwater.Any(p=>p.y< -40)&&underwater.Zip(underwater.Skip(1),(a,z)=>Vector3.Distance(a,z)).Max()<650,"Underwater route follows graded road instead of a direct ocean shortcut");
            Directory.CreateDirectory("Artifacts/FourCities");File.WriteAllText("Artifacts/FourCities/rules.json",JsonUtility.ToJson(r,true));if(r.failed.Count>0)throw new Exception(string.Join("\n",r.failed));Debug.Log("Four-city essential rule checks: "+r.passed.Count+" passed");
        }
        public static void RulesAndBuild(){Rules();ProjectBuilder.BuildRelease();}
    }
}
