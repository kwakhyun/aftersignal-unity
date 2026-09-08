using UnityEngine;

namespace AfterSignal
{
    // The avenue entrance works on foot and inside a vehicle.
    public sealed class AutomaticCityEntrance : MonoBehaviour
    {
        static bool continueDriving;
        GameDirector game;
        InteractionPoint entrance;
        void Start()
        {
            game = GameDirector.Instance;
            if(game&&game.stage==StageId.UrbanCity&&continueDriving){StartCoroutine(ResumeCar());return;}
            if (!game || game.stage != StageId.Haven) { enabled = false; return; }
            foreach (var point in InteractionPoint.All)
                if (point && point.kind == InteractionKind.FacilityTravel && point.destination == StageId.UrbanCity) { entrance = point; break; }
        }
        System.Collections.IEnumerator ResumeCar()
        {
            continueDriving=false;
            float until=Time.realtimeSinceStartup+8;
            while((!UrbanSimulation.Instance||!UrbanSimulation.Instance.Owned)&&Time.realtimeSinceStartup<until)yield return null;
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned)
            {
                UrbanSimulation.Instance.Owned.occupied=false;
                UrbanSimulation.Instance.Enter(UrbanSimulation.Instance.Owned);
            }
            enabled=false;
        }
        void Update()
        {
            if (!game || !entrance || !game.Ready || game.Blocked) return;
            var player = game.Player.transform.position;
            var gate = entrance.transform.position;
            var car = UrbanSimulation.Instance ? UrbanSimulation.Instance.Current : null;
            float reach = car ? car.HalfLength + 2 : 3;
            if (Mathf.Abs(player.x - gate.x) > reach || Mathf.Abs(player.z - gate.z) > 7 || Mathf.Abs(player.y - gate.y) > 5) return;
            UrbanSimulation.Instance?.SaveCar();
            if(car)
            {
                continueDriving=true;
                PlayerPrefs.SetInt(UrbanCatalog.Prefix+"CarStage",(int)StageId.UrbanCity);
                PlayerPrefs.SetFloat(UrbanCatalog.Prefix+"CarX",73);
                PlayerPrefs.SetFloat(UrbanCatalog.Prefix+"CarZ",-254);
            }
            game.Travel(StageId.UrbanCity);
        }
    }
}
