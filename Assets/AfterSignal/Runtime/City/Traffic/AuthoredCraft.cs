using UnityEngine;
namespace AfterSignal
{
    public sealed class AuthoredCraft:MonoBehaviour
    {
        public Vector3 dock=new Vector3(1730,0,-615);
        public Vector3 quay=new Vector3(1730,.15f,-554);
        public Vector3 helm=new Vector3(185,24.4f,0);
        public Vector3 BoardingPoint=>Vector3.Distance(transform.position,dock)<12?quay:transform.TransformPoint(new Vector3(0,5,20));
    }
}
