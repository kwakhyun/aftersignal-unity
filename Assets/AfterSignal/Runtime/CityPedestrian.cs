using UnityEngine;
namespace AfterSignal
{
    public sealed class CityPedestrian:MonoBehaviour
    {
        public Vector3 target,velocity;public float age,speed=1.2f;public bool struck,dead;public Sprite[] poses;
        SpriteRenderer visual;float phase,stun;public bool crossing,enteredCrossing,waiting;
        public void WalkTo(Vector3 p,bool cross){target=p;crossing=cross;enteredCrossing=waiting=false;}
        public void ResetAt(Vector3 p,Vector3 destination,Sprite[] frames){transform.position=p;target=destination;poses=frames;visual=GetComponent<SpriteRenderer>();visual.color=Color.white;visual.enabled=true;struck=dead=crossing=enteredCrossing=waiting=false;velocity=Vector3.zero;age=phase=stun=0;gameObject.SetActive(true);}
        public void Hit(Vector3 direction,float force){if(struck||dead)return;struck=true;dead=force>=12;age=0;stun=dead?12:3.2f;velocity=direction.normalized*Mathf.Clamp(force*.8f,3,18)+Vector3.up*Mathf.Clamp(force*.4f,2,9);}
        public void Tick(float dt)
        {
            age+=dt;phase+=dt*5;
            if(struck){velocity.y-=20*dt;transform.position+=velocity*dt;if(transform.position.y<.04f){var p=transform.position;p.y=.04f;transform.position=p;velocity=Vector3.Lerp(velocity,Vector3.zero,dt*9);velocity.y=0;}if(!dead&&age>stun){struck=false;if(GameDirector.Instance.stage==StageId.UrbanCity){transform.position=CityRoadNetwork.Sidewalk(transform.position);target=transform.position;crossing=enteredCrossing=false;}}}
            else{var delta=target-transform.position;waiting=crossing&&!enteredCrossing&&!CityRoadNetwork.CanStartCrossing(delta.magnitude/speed);if(waiting){if(poses!=null&&poses.Length>0)visual.sprite=poses[0];return;}if(crossing)enteredCrossing=true;delta.y=0;transform.position=Vector3.MoveTowards(transform.position,target,speed*dt);if(delta.x!=0)visual.flipX=delta.x<0;}
            if(poses!=null&&poses.Length>0)visual.sprite=poses[struck?0:1+(int)phase%Mathf.Max(1,poses.Length-1)];
        }
        void LateUpdate(){if(visual&&Camera.main)transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,struck?(dead?86:Mathf.Sin(age*4)*40):0);}
    }
}
