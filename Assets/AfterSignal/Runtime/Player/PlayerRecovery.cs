using UnityEngine;
namespace AfterSignal
{
    public sealed partial class PlayerMotor
    {
        public float RecoveryDelay=>Tuning?Tuning.recoveryDelay:8;
        public float RecoveryPerSecond=>Tuning?Tuning.recoveryPerSecond:2.5f;
        // Natural regeneration only patches Seoha up to this level; food, clinics, rest and paid terminals heal above it.
        public float RecoveryCap=>Tuning?Tuning.maxHealth*Mathf.Clamp01(Tuning.recoveryCap):60;
        float recoveryWait;
        public bool Recovering=>Health>0&&Health<RecoveryCap&&recoveryWait<=0;
        public void TickRecovery(float dt)
        {
            LifeState.SettleDay();
            if(Health<=0||Director.Blocked)return;
            recoveryWait=Mathf.Max(0,recoveryWait-dt);
            if(Recovering&&!OceanLife.Submerged)Health=Mathf.Min(RecoveryCap,Health+RecoveryPerSecond*dt);
            TrackSafeGround(dt);
        }
        public void HealTo(float target){if(Health>0&&Health<target)Health=Mathf.Min(Tuning.maxHealth,target);}

        // Last position where Seoha stood on static, open ground in this scene. Falling out of the
        // world and the pause-menu "unstuck" return here instead of the scene checkpoint.
        public Vector3 SafePosition{get;private set;}
        public bool HasSafePosition{get;private set;}
        float safeClock;
        void TrackSafeGround(float dt)
        {
            safeClock-=dt;if(safeClock>0)return;safeClock=.35f;
            if(!Grounded||Rope.Attached||WallClimbing||DashTime>0||Time.time<supportedUntil||OceanLife.Swimming||!Controller||!Controller.enabled)return;
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Current||VenueRide.Riding||CityBusService.Instance&&CityBusService.Instance.Riding)return;
            var p=transform.position;
            if(Director.stage==StageId.UrbanCity&&!FourCityCatalog.Dry(p)&&p.y<OceanLife.Surface+.6f)return;
            if(!Physics.Raycast(p+Vector3.up*.5f,Vector3.down,out var floor,1.1f,1,QueryTriggerInteraction.Ignore)||floor.normal.y<.7f)return;
            var c=floor.collider;
            if(c.attachedRigidbody||c.GetComponentInParent<CityVehicle>()||c.GetComponentInParent<MovingLift>()||c.GetComponentInParent<MultiFloorLift>()||c.GetComponentInParent<EscalatorSurface>()||c.GetComponentInParent<RailMotion>()||c.GetComponentInParent<WorldActor>())return;
            if(Physics.CheckCapsule(floor.point+Vector3.up*.45f,floor.point+Vector3.up*1.7f,.3f,1,QueryTriggerInteraction.Ignore))return;
            SafePosition=floor.point+Vector3.up*.06f;HasSafePosition=true;
        }
        public Vector3 RecoveryPoint=>HasSafePosition?SafePosition:Director.checkpoint;

        static float unstuckReady;
        public static float UnstuckWait=>Mathf.Max(0,unstuckReady-Time.unscaledTime);
        public static void Unstuck(GameDirector game)
        {
            if(!game||!game.Ready||!game.Player||game.Dead||game.Title||game.Transition||game.Player.Health<=0)return;
            if(UnstuckWait>0){game.Toast("잠시 후 다시 시도하세요 · "+Mathf.CeilToInt(UnstuckWait)+"초",2);return;}
            unstuckReady=Time.unscaledTime+GameTuning.Active.unstuckCooldown;
            var sim=UrbanSimulation.Instance;if(sim&&sim.Current)sim.EmergencyExit(sim.Current);
            if(game.Paused)game.SetPaused(false);
            var p=game.Player;p.Rope.Release();
            // A jailed player is returned to the cell, so this never doubles as an escape.
            bool jailed=game.stage==StageId.UrbanCity&&PrisonSystem.Instance&&PrisonSystem.Instance.Jailed;
            p.Respawn(jailed?PrisonSystem.Cell:p.RecoveryPoint,false);game.CameraRig.Snap();
            game.Toast(jailed?"수감 구역으로 돌아왔습니다":"끼임 해제 · 마지막 안전 지점으로 이동했습니다",3);
        }
    }
    // Pause-menu entry point: PlayerRecovery.Unstuck(game).
    public static class PlayerRecovery
    {
        public static void Unstuck(GameDirector game)=>PlayerMotor.Unstuck(game);
        public static float Cooldown=>PlayerMotor.UnstuckWait;
    }
}
