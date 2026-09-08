using UnityEngine;
namespace AfterSignal
{
    public sealed class GovernmentCampus:MonoBehaviour
    {
        public static GovernmentCampus Instance {get;private set;}
        public Vector3 Entrance=>transform.position+new Vector3(0,.1f,-46);
        void Awake(){Instance=this;if(ExpansionWorld.Places.Length>13)ExpansionWorld.Places[13]=Entrance;}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
