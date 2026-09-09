using UnityEngine;

namespace AfterSignal
{
    public static class NpcOpening
    {
        static string Situation(CityNpc npc)
        {
            var actor=npc.GetComponent<WorldActor>();
            if(actor&&actor.Downed)return "중상을 입고 구조를 기다리는 중";
            if(actor&&actor.health<60)return "다쳐서 치료가 필요한 상태";
            if(npc.Fleeing)return "근처 위험에서 대피하는 중";
            var fires=CityFireService.Instance;
            if(fires)foreach(var fire in fires.Fires)if(fire&&fire.Heat>0&&(fire.Position-npc.transform.position).sqrMagnitude<90*90)return "근처 화재를 목격한 상태";
            return LifeState.Hour>=6&&LifeState.Hour<9?"아침 출근·등교 시간":LifeState.Hour>=17&&LifeState.Hour<21?"저녁 퇴근·휴식 시간":LifeState.Hour>=21||LifeState.Hour<6?"한밤중": "낮 일과 시간";
        }
        public static string Place(CityNpc npc)
        {
            var game=GameDirector.Instance;
            if(game.stage==StageId.UrbanInterior)return UrbanCatalog.Name(UrbanCatalog.Current);
            if(game.stage!=StageId.UrbanCity)return CivicWorld.Title(game.stage);
            string place=new[]{"애프터라이트","노바 시티","에레보스","네레이드"}[FourCityCatalog.CityAt(npc.transform.position)];
            float best=120*120;
            foreach(var venue in FourCityCatalog.Venues){float d=(venue.Entrance-npc.transform.position).sqrMagnitude;if(d<best){best=d;place=venue.title;}}
            for(int i=0;i<UrbanCatalog.SiteCount;i++){float d=(UrbanCatalog.Door(i)-npc.transform.position).sqrMagnitude;if(d<best){best=d;place=UrbanCatalog.Name(i);}}
            return place;
        }
        public static string Context(CityNpc npc) => $"현재 상황: {Situation(npc)}. 장소: {Place(npc)}. 서하는 다가와 말을 기다리고 있다. 직업: {npc.occupation}.";
        public static string Fallback(CityNpc npc)
        {
            var actor=npc.GetComponent<WorldActor>();
            return NpcDialogueBank.Line(npc,actor&&actor.Downed?"down":actor&&actor.health<60?"hurt":npc.Fleeing?"panic":"ambient");
        }
    }
}
