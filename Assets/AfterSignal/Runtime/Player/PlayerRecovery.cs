using UnityEngine;
namespace AfterSignal
{
    public sealed partial class PlayerMotor
    {
        public const float RecoveryDelay=8,RecoveryPerSecond=2.5f;
        float recoveryWait;
        public bool Recovering=>Health>0&&Health<Tuning.maxHealth&&recoveryWait<=0;
        public void TickRecovery(float dt)
        {
            if(Health<=0||Director.Blocked)return;
            recoveryWait=Mathf.Max(0,recoveryWait-dt);
            if(Recovering&&!OceanLife.Submerged)Health=Mathf.Min(Tuning.maxHealth,Health+RecoveryPerSecond*dt);
        }
    }
}
