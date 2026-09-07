using UnityEngine;
using System.Collections.Generic;
namespace AfterSignal
{
    public sealed class TravellingCut:MonoBehaviour
    {
        GameDirector game;Vector3 direction;float damage,travel,clock;readonly HashSet<EnemyBrain> struck=new HashSet<EnemyBrain>();readonly HashSet<BreakableGlass> glassHit=new HashSet<BreakableGlass>();
        public void Initialize(GameDirector director,Vector3 forward,float amount){game=director;direction=forward;damage=amount;}
        void Update(){
            if(!game){Destroy(gameObject);return;}if(game.Blocked)return;
            float step=23*Time.deltaTime;
            if(Physics.Raycast(transform.position,direction,out var wall,step,1<<0,QueryTriggerInteraction.Ignore)&&!wall.collider.GetComponentInParent<BreakableGlass>()){Destroy(gameObject);return;}
            transform.position+=direction*step;travel+=step;clock-=Time.deltaTime;
            if(clock<=0){clock=.045f;SignalEffects.Slash(transform.position,direction.x,1.7f,SignalEffects.Cyan,1);}
            foreach(var enemy in game.Enemies)if(enemy&&enemy.Alive&&!struck.Contains(enemy)){var d=enemy.transform.position+Vector3.up*1.2f-transform.position;if(Mathf.Abs(d.x)<1.2f+step&&Mathf.Abs(d.z)<1.15f&&Mathf.Abs(d.y)<1.8f){struck.Add(enemy);enemy.Damage(damage,direction*4);enemy.React(.3f,.035f,direction);SignalEffects.Impact(enemy.transform.position+Vector3.up,direction,SignalEffects.Cyan,.9f);game.Audio.Play("blade_hit",enemy.transform.position,.34f,3);}}
            foreach(var glass in game.Glass)if(glass&&!glass.Broken&&!glassHit.Contains(glass)&&Vector3.Distance(glass.transform.position,transform.position)<2){glassHit.Add(glass);glass.Hit(damage);}
            if(travel>11)Destroy(gameObject);
        }
    }
}
