using UnityEngine;

namespace AfterSignal
{
    public static class CivicWorld
    {
        static StageId arrivalStage;
        static Vector3 arrival;
        static bool pending;
        public static bool Interior(StageId stage) => stage == StageId.Residence || stage == StageId.School || stage == StageId.Clinic || stage == StageId.Headquarters || stage == StageId.UrbanInterior;
        public static bool Exploration(StageId stage) => stage == StageId.Haven || stage == StageId.UrbanCity || Interior(stage);
        public static string Title(StageId stage) => stage == StageId.Residence ? "서하의 집 / AFTERLIGHT 0607" : stage == StageId.School ? "새봄초등학교" : stage == StageId.Clinic ? "온유병원" : stage == StageId.Headquarters ? "신호복원본부" : stage == StageId.UrbanInterior ? (ResidentialWorld.VisitHome>=0 ? ResidentialWorld.VisitTitle : UrbanCatalog.Name(UrbanCatalog.Current)) : "애프터라이트";
        public static Vector3 TownDoor(StageId stage) => stage == StageId.Residence ? new Vector3(16, .15f, 14) : stage == StageId.School ? new Vector3(64, .15f, 19) : stage == StageId.Clinic ? new Vector3(113, .15f, 19) : new Vector3(178, .15f, 19);
        public static void Travel(GameDirector game, StageId destination, Vector3 point)
        {
            arrivalStage = destination;
            arrival = point;
            pending = true;
            game.Travel(destination);
        }

        public static Vector3 Spawn(StageId stage, Vector3 fallback)
        {
            if (!pending || arrivalStage != stage)
                return fallback;
            pending = false;
            return arrival;
        }

        public static void ClearArrival()
        {
            pending = false;
        }

        public static Vector3 SafeSpawn(StageId stage, Vector3 desired)
        {
            if (!Interior(stage))
                return desired;
            Physics.SyncTransforms();
            for (int i = 0; i < 25; i++)
            {
                var p = desired + new Vector3((i % 5 - 2) * .7f, 0, (i / 5 - 2) * .7f);
                if (i == 0)
                    p = desired;
                if (!Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var floor, 5, 1, QueryTriggerInteraction.Ignore) || floor.normal.y < .65f || floor.point.y > desired.y + .4f)
                    continue;
                p.y = floor.point.y + .06f;
                if (!Physics.CheckCapsule(p + Vector3.up * .42f, p + Vector3.up * 1.7f, .34f, 1, QueryTriggerInteraction.Ignore))
                    return p;
            }

            return desired;
        }
    }
}
