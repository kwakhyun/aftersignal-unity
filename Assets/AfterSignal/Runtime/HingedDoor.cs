using UnityEngine;
namespace AfterSignal
{
    public sealed class HingedDoor:MonoBehaviour
    {
        public Transform panel;public bool Open {get;private set;}public float angle=-100;
        public void Toggle(GameDirector game){if(Open&&Vector3.Distance(game.Player.Shoulder,transform.position)<1.1f){game.Toast("문에서 한 걸음 물러나세요");return;}Open=!Open;game.Audio.Play("door",transform.position,.25f,1);}
        void Update(){if(panel)panel.localRotation=Quaternion.Slerp(panel.localRotation,Quaternion.Euler(0,Open?angle:0,0),1-Mathf.Exp(-Time.deltaTime*7));}
    }
}
