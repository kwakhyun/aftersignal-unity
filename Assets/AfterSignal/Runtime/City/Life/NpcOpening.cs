using UnityEngine;

namespace AfterSignal
{
    public static class NpcOpening
    {
        static readonly string[] Hurt={"으… 아까 다친 데가 욱신거려요. 서하 씨, 근처에 병원이 어디 있죠?","괜찮아요, 조금 다쳤을 뿐이에요. 그래도 잠깐 옆에 있어 줄래요?","아야… 서하 씨, 혹시 구급상자 같은 거 있어요?"};
        static readonly string[] Busy={"잠깐만요, 생각 좀 정리할게요. 방금 한 말, 한 번만 다시 해 줄래요?","하나씩 천천히 이야기해요. 조금 있다가 다시 물어봐 줄래요?"};
        static readonly string[] Noise={"잠깐만요, 주변 신호에 잡음이 심하네요.","미안해요, 방금은 잘 못 들었어요.","음… 머릿속이 잠깐 하얘졌어요."};
        static readonly string[] Tired={"오늘은 이야기를 꽤 많이 나눴네요.","이야기가 길어졌네요. 나머지는 다음에 해요."};
        static string Pick(string[] lines)=>lines[Random.Range(0,lines.Length)];
        static bool NearFire(CityNpc npc)
        {
            var fires=CityFireService.Instance;
            if(fires)foreach(var fire in fires.Fires)if(fire&&fire.Heat>0&&(fire.Position-npc.transform.position).sqrMagnitude<90*90)return true;
            return false;
        }
        static bool Urgent(CityNpc npc){var actor=npc.GetComponent<WorldActor>();return actor&&(actor.Downed||actor.health<60)||npc.Fleeing||NearFire(npc);}
        static string Situation(CityNpc npc)
        {
            var actor=npc.GetComponent<WorldActor>();
            if(actor&&actor.Downed)return "중상을 입고 구조를 기다리는 중";
            if(actor&&actor.health<60)return "다쳐서 치료가 필요한 상태";
            if(npc.Fleeing)return "근처 위험에서 대피하는 중";
            if(NearFire(npc))return "근처 화재를 목격한 상태";
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
        // Situational line from the local banks (injury, danger, role, time of day). Never a paid request.
        public static string Fallback(CityNpc npc)
        {
            if(!npc)return NpcDialogueBank.Line(null,"ambient");
            var actor=npc.GetComponent<WorldActor>();
            if(actor&&actor.Downed)return NpcDialogueBank.Line(npc,"down");
            if(actor&&actor.health<60)return Pick(Hurt);
            if(npc.Fleeing||NearFire(npc))return NpcDialogueBank.Line(npc,"danger");
            return NpcDialogueBank.Line(npc,"ambient");
        }
        // The NPC always speaks first with this local line; the AI is asked only after Seoha says something.
        public static string Line(CityNpc npc)
        {
            if(!npc||Urgent(npc))return Fallback(npc);
            string group=NpcPersona.Group(npc);var bond=npc.GetComponent<NeighborBond>();
            if(group=="child"||group=="police"||group=="military"||group=="gang"||group=="terrorist"||bond&&bond.Affinity<60)return Fallback(npc);
            float chance=bond?0.7f:group=="elder"||group=="student"||group=="medical"?0.6f:0.4f;
            return Random.value<chance?NpcDialogueBank.Line(npc,"greeting"):Fallback(npc);
        }
        // Local stand-in reply when no AI answer is available: reason "busy" (rate limited), "budget" (cap reached), otherwise offline/noise.
        public static string Reply(CityNpc npc,string reason)
        {
            if(npc&&Urgent(npc))return Fallback(npc);
            return reason=="busy"?Pick(Busy):(reason=="budget"?Pick(Tired):Pick(Noise))+" "+Fallback(npc);
        }
    }
}
