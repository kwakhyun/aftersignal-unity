using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AfterSignal.Tests
{
    public partial class GameplayTests
    {
        [UnityTest]
        public IEnumerator VehicleVariantsHaveCollisionAndDestructionIsIdempotent()
        {
            yield return LoadDistrict(StageId.UrbanCity);
            yield return null;
            var sim = UrbanSimulation.Instance;
            for (int i = 0; i < 4; i++)
            {
                var car = sim.Spawn(new Vector3(210 + i * 16, .02f, -285), false, i);
                yield return null;
                Assert.AreEqual((CityVehicleType)i, car.type);
                Assert.NotNull(car.transform.Find("Sculpted chassis"));
                Assert.NotNull(car.GetComponentInChildren<Collider>());
                car.Damage(40, car.transform.position);
                Assert.That(car.health, Is.EqualTo(60));
                car.Damage(90, car.transform.position);
                car.Damage(100, car.transform.position);
                Assert.AreEqual(1, car.Explosions);
                Assert.IsFalse(sim.Enter(car));
            }
        }

        [UnityTest]
        public IEnumerator PedestrianWaitsBeforeEnteringRedCrosswalk()
        {
            yield return LoadDistrict(StageId.UrbanCity);
            yield return null;
            yield return null;
            var population = CityPopulation.Instance;
            Assert.IsFalse(CityRoadNetwork.Walk);
            var citizen = population.Citizens[0];
            var pos = CityRoadNetwork.CornerPoint(1, 1, -1, -1);
            citizen.ResetAt(pos, pos, citizen.poses);
            citizen.WalkTo(CityRoadNetwork.CornerPoint(1, 1, 1, -1), true);
            citizen.Tick(.5f);
            Assert.IsTrue(citizen.waiting);
            Assert.That(Vector3.Distance(pos, citizen.transform.position), Is.LessThan(.001f));
            Assert.IsFalse(citizen.enteredCrossing);
        }

        [UnityTest]
        public IEnumerator EveryCitySiteHasAReachableDoorWithoutChangingOldIds()
        {
            yield return LoadDistrict(StageId.UrbanCity);
            yield return null;
            Assert.AreEqual(22, (int)StageId.Breach);
            Assert.AreEqual(23, (int)StageId.UrbanCity);
            var doors = Object.FindObjectsByType<InteractionPoint>();
            for (int i = 0; i < 40; i++)
            {
                int id = i;
                var p = System.Array.Find(doors, v => v.kind == InteractionKind.UrbanEnter && v.siteId == id);
                Assert.NotNull(p, "Site " + i);
                var q = UrbanCatalog.Door(i);
                Assert.IsFalse(Physics.CheckCapsule(q + Vector3.up * .4f, q + Vector3.up * 1.7f, .3f, 1, QueryTriggerInteraction.Ignore), "Blocked door " + i);
            }
        }

        [UnityTest]
        public IEnumerator DrivingBlocksFastExitAndEmptyTankCannotAccelerate()
        {
            yield return LoadDistrict(StageId.UrbanCity);
            yield return null;
            var sim = UrbanSimulation.Instance;
            var car = sim.Spawn(new Vector3(100, .02f, -283), false);
            game.Player.Respawn(car.transform.position + Vector3.back * 3, false);
            Assert.IsTrue(sim.Enter(car));
            car.speed = 12;
            Assert.IsFalse(sim.Exit());
            Assert.IsFalse(game.Player.Controller.enabled);
            car.fuel = 0;
            car.speed = 0;
            var c = ControlFrame.Empty;
            c.move = Vector2.up;
            yield return Drive(c, .5f);
            Assert.That(car.speed, Is.EqualTo(0).Within(.01));
            Assert.IsTrue(sim.Exit());
            Assert.IsTrue(game.Player.Controller.enabled);
        }
    }
}
