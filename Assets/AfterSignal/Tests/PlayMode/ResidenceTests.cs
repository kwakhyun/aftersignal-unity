using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace AfterSignal.Tests
{
    public partial class GameplayTests
    {
        [UnityTest] public IEnumerator LeftTravelFacesLeftRegardlessOfIdleMousePosition()
        {
            var c=Controls(game.Player.Shoulder+Vector3.right*10);c.move=Vector2.left;yield return Drive(c,.2f);Assert.AreEqual(-1,game.Player.Facing);
            c.move=Vector2.right;yield return Drive(c,.2f);Assert.AreEqual(1,game.Player.Facing);
        }
        [UnityTest] public IEnumerator MagazineReloadAndWeaponChangeHaveNoFreeRounds()
        {
            var c=Controls(game.Player.Shoulder+Vector3.right*12);c.weapon=2;yield return Drive(c,.05f);c.weapon=-1;c.attack=true;yield return Drive(c,.1f);c.attack=false;yield return Drive(c,.4f);
            Assert.AreEqual(11,game.Player.Ammo);Assert.AreEqual(1,game.Player.ShotsFired);
            c.reload=true;yield return Drive(c,.03f);c.reload=false;yield return Drive(c,.35f);Assert.IsTrue(game.Player.Reloading);Assert.AreEqual(11,game.Player.Ammo);
            c.weapon=0;yield return Drive(c,.1f);Assert.IsFalse(game.Player.Reloading);Assert.AreEqual(11,game.Player.Ammo);
            c.weapon=2;yield return Drive(c,.06f);c.weapon=-1;c.reload=true;yield return Drive(c,.02f);c.reload=false;yield return Drive(c,1.2f);Assert.AreEqual(12,game.Player.Ammo);Assert.AreEqual(1,game.Player.Reloads);
        }
        [UnityTest] public IEnumerator WheelCyclesOncePerInputAndWaitsForRecovery()
        {
            var c=Controls(game.Player.Shoulder+Vector3.right*12);c.weaponCycle=1;game.Input.ExternalFrame=c;yield return null;game.Input.ExternalFrame=ControlFrame.Empty;yield return null;
            Assert.AreEqual(WeaponId.Greatsword,game.Player.Weapon);
            c.weaponCycle=0;c.attack=true;yield return Drive(c,.03f);c.attack=false;c.weaponCycle=1;game.Input.ExternalFrame=c;yield return null;game.Input.ExternalFrame=ControlFrame.Empty;
            Assert.AreEqual(WeaponId.Greatsword,game.Player.Weapon);yield return new WaitForSeconds(.9f);Assert.AreEqual(WeaponId.Pistol,game.Player.Weapon);
        }
        [UnityTest] public IEnumerator PistolDashBurstCrossesLowFrameRateContactWithoutLostShots()
        {
            var c=Controls(game.Player.Shoulder+Vector3.right*20);c.weapon=2;yield return Drive(c,.03f);game.enabled=false;c.weapon=-1;c.dash=true;c.attack=true;game.Player.Tick(c,1f/15);c.dash=c.attack=false;
            for(int i=0;i<7;i++)game.Player.Tick(c,1f/15);
            Assert.AreEqual(WeaponAction.Dash,game.Player.Action);Assert.AreEqual(3,game.Player.ShotsFired);Assert.AreEqual(9,game.Player.Ammo);game.enabled=true;
        }
        [UnityTest] public IEnumerator EveryWeaponUsesItsOwnDashAndSkillTiming()
        {
            foreach(WeaponId w in System.Enum.GetValues(typeof(WeaponId))){
                game.Player.Respawn(game.spawn);var c=Controls(game.Player.Shoulder+Vector3.right*12);c.weapon=(int)w;yield return Drive(c,.05f);c.weapon=-1;c.dash=true;c.attack=true;game.Input.ExternalFrame=c;yield return null;c.dash=c.attack=false;game.Input.ExternalFrame=c;yield return new WaitForSeconds(.05f);
                Assert.AreEqual(WeaponAction.Dash,game.Player.Action);Assert.AreEqual(w,game.Player.Weapon);yield return Drive(c,1.0f);int skills=game.Player.SkillsUsed;c.skill=true;game.Input.ExternalFrame=c;yield return null;c.skill=false;game.Input.ExternalFrame=c;yield return new WaitForSeconds(.1f);Assert.AreEqual(skills+1,game.Player.SkillsUsed);Assert.AreEqual(WeaponAction.Skill,game.Player.Action);yield return Drive(c,4.2f);
            }
        }
        [UnityTest] public IEnumerator ResidenceDoorAndRiderSurviveSerializedSceneReload()
        {
            yield return LoadDistrict(StageId.Residence);Assert.That(game.Player.transform.position.y,Is.InRange(21.9f,22.4f));
            var door=Object.FindAnyObjectByType<HingedDoor>();Assert.NotNull(door);Assert.NotNull(door.panel);Assert.IsFalse(door.Open);
            game.Player.Respawn(new Vector3(26,22.15f,0));var c=Controls(new Vector3(35,23,0));c.move=Vector2.right;yield return Drive(c,.55f);Assert.Less(game.Player.transform.position.x,28);
            door.Toggle(game);yield return new WaitForSeconds(.65f);yield return Drive(c,1);Assert.Greater(game.Player.transform.position.x,29);
            var lift=Object.FindAnyObjectByType<MovingLift>();Assert.That(lift.Height,Is.EqualTo(22).Within(.03));game.Player.Respawn(new Vector3(42,22.15f,0));yield return new WaitForSeconds(.2f);lift.Use(game);yield return new WaitForSeconds(4.7f);Assert.That(game.Player.transform.position.y,Is.InRange(-.1f,.4f));
        }
        [UnityTest] public IEnumerator HeadquartersMainQuestStartsRailWithoutGrantingCompletion()
        {
            PlayerPrefs.SetInt("AFTERSIGNAL.Unity.Completed",0);yield return LoadDistrict(StageId.Headquarters);game.Player.Respawn(new Vector3(35,.15f,-4));yield return new WaitForSeconds(.2f);Assert.NotNull(game.Nearby);Assert.AreEqual(InteractionKind.FirstRail,game.Nearby.kind);var c=Controls(game.Player.Shoulder);c.interact=true;game.Input.ExternalFrame=c;yield return null;yield return new WaitForSeconds(1.5f);game=GameDirector.Instance;Assert.AreEqual(StageId.Station,game.stage);Assert.AreEqual(0,PlayerPrefs.GetInt("AFTERSIGNAL.Unity.Completed"));
        }
    }
}
