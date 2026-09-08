namespace AfterSignal {public sealed class FacilityWorkPoint:UnityEngine.MonoBehaviour {public FacilityOperation owner;public void Use(){if(owner)owner.CompleteStep();}}}
