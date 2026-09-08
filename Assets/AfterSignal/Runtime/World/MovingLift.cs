using UnityEngine;
namespace AfterSignal
{
    public sealed class MovingLift : MonoBehaviour
    {
        public float bottom,top=6,speed=2.4f;
        public Transform platform;
        float target;bool initialized;
        public bool Moving=>platform&&Mathf.Abs(platform.position.y-target)>.01f;
        public float Height=>platform?platform.position.y:bottom;
        public bool HasRider {get;private set;}
        void Start(){if(!initialized){target=Height;initialized=true;}}
        bool Aboard(PlayerMotor p,Vector3 at)=>Mathf.Abs(p.transform.position.x-at.x)<1.65f&&Mathf.Abs(p.transform.position.z-at.z)<1.65f&&p.transform.position.y>=at.y-.15f&&p.transform.position.y<at.y+.75f&&p.Velocity.y<1;
        public void Use(GameDirector game)
        {
            if(!platform)return;
            if(!initialized){target=Height;initialized=true;}
            if(Moving){game.Toast("승강기가 이동 중입니다.");return;}
            bool aboard=Aboard(game.Player,platform.position);
            // A call from the opposite landing summons the platform instead of toggling blindly.
            float floor=Mathf.Abs(game.Player.transform.position.y-top)<Mathf.Abs(game.Player.transform.position.y-bottom)?top:bottom;
            target=!aboard&&Mathf.Abs(Height-floor)>.5f?floor:Height>(bottom+top)*.5f?bottom:top;
            game.Audio.Play("ui_confirm",transform.position,.2f,1);
            game.Toast(aboard?(target==top?"승강기 상승 중":"승강기 하강 중"):"승강기를 호출했습니다. 도착하면 탑승하세요.");
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||!platform)return;
            HasRider=Aboard(g.Player,platform.position);
            if(!Moving)return;
            var delta=Vector3.up*(Mathf.MoveTowards(Height,target,speed*Time.deltaTime)-Height);
            platform.position+=delta;Physics.SyncTransforms();
            if(HasRider)g.Player.Carry(delta);
        }
    }
}