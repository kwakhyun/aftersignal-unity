using UnityEngine;
namespace AfterSignal
{
    public sealed class MovingLift:MonoBehaviour
    {
        public float bottom,top=6,speed=2.4f;public Transform platform;float target;
        public bool Moving=>platform&&Mathf.Abs(platform.position.y-target)>.01f;
        public float Height=>platform?platform.position.y:bottom;
        void Start(){target=platform?platform.position.y:bottom;}
        public void Use(GameDirector game){if(Moving)return;target=Height>(bottom+top)*.5f?bottom:top;game.Audio.Play("ui_confirm",transform.position,.2f,1);game.Toast(target==top?"승강기 상승 · 발판 위에서 기다리세요":"승강기 하강 · 발판 위에서 기다리세요");}
        void Update(){var g=GameDirector.Instance;if(!g||g.Blocked||!platform||!Moving)return;var p=g.Player;Vector3 old=platform.position;float y=Mathf.MoveTowards(old.y,target,speed*Time.deltaTime);Vector3 delta=Vector3.up*(y-old.y);bool rider=Mathf.Abs(p.transform.position.x-old.x)<1.7f&&Mathf.Abs(p.transform.position.z-old.z)<1.7f&&p.transform.position.y>=old.y-.1f&&p.transform.position.y<old.y+.5f&&p.Velocity.y<1;
            platform.position+=delta;Physics.SyncTransforms();if(rider){p.Controller.Move(delta);p.Velocity.y=-2;}}
    }
}
