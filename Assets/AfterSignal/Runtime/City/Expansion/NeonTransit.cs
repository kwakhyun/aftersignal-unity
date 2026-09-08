using UnityEngine;
namespace AfterSignal
{
    public sealed class NeonTransit:MonoBehaviour
    {
        public Vector3[] path;int stop=1;float wait=8;
        void Start(){if(path!=null&&path.Length>1)transform.position=path[0];foreach(var t in GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;}
        void Update()
        {
            if(path==null||path.Length<2||GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            if(wait>0){wait-=Time.deltaTime;return;}
            var d=path[stop]-transform.position;
            if(d.magnitude<.3f){stop=(stop+1)%path.Length;wait=12;return;}
            transform.position=Vector3.MoveTowards(transform.position,path[stop],Time.deltaTime*Mathf.Min(32,Mathf.Max(3,Mathf.Sqrt(d.magnitude*10))));
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*32);
        }
    }
}
