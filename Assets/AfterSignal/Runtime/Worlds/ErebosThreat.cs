using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class ErebosThreat:MonoBehaviour
    {
        public WorldActor Body{get;private set;}
        Vector3 home;float hit,phase;bool large;Transform head;readonly List<Transform> legs=new();
        public static WorldActor Spawn(Vector3 p,bool large,int id,Transform parent)
        {
            var go=new GameObject(large?"Unknown resonance sentinel":"Corrupted citizen "+id,typeof(WorldActor),typeof(ErebosThreat));go.transform.SetParent(parent);go.transform.position=p;go.layer=9;
            var body=go.GetComponent<WorldActor>();body.monster=true;body.health=large?420:85;
            var hit=go.AddComponent<CapsuleCollider>();hit.isTrigger=true;hit.height=large?4.5f:2.1f;hit.radius=large?1.25f:.38f;hit.center=Vector3.up*hit.height*.5f;
            var threat=go.GetComponent<ErebosThreat>();threat.Body=body;threat.home=p;threat.large=large;threat.phase=id*.73f;
            if(!large){var sr=go.AddComponent<SpriteRenderer>();sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.sprite=PeopleArt.Get("CorruptedCitizen",0);PeopleArt.Attach(go,"CorruptedCitizen");}
            else threat.BuildSentinel();
            return body;
        }
        void BuildSentinel()
        {
            head=new GameObject("Suspended resonance core").transform;head.SetParent(transform,false);head.localPosition=Vector3.up*3;
            var g=new CityGeometry(head);g.Dome(Vector3.zero,new(1.6f,1.2f,1.6f),"NovaObsidian",16,6);g.Ring(Vector3.up*.3f,1.8f,1.8f,.16f,"NovaNeonViolet",32);g.Cylinder(new(0,-.7f,0),.5f,1.8f,"NovaNeonViolet",16,.1f);g.Finish();
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var leg=new GameObject("Articulated limb "+i).transform;leg.SetParent(transform,false);var l=new CityGeometry(leg);Vector3 root=new(Mathf.Cos(a),2.8f,Mathf.Sin(a)),knee=new(Mathf.Cos(a)*2.3f,1.9f,Mathf.Sin(a)*2.3f),foot=new(Mathf.Cos(a)*2.8f,.1f,Mathf.Sin(a)*2.8f);l.Beam(root,knee,.35f,"FutureCarbon");l.Beam(knee,foot,.18f,"FutureSilver");l.Beam(root+Vector3.up*.1f,knee+Vector3.up*.1f,.04f,"NovaNeonViolet");l.Finish();legs.Add(leg);}
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||game.Blocked||!Body)return;
            if(!Body.Alive){if(large){transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*.15f,Time.deltaTime*2);head.localPosition=Vector3.Lerp(head.localPosition,Vector3.up*.3f,Time.deltaTime*2);}else{var sr=GetComponent<SpriteRenderer>();if(sr)sr.transform.rotation=Quaternion.Euler(0,Camera.main.transform.eulerAngles.y,88);}return;}
            var player=game.Player;var opponent=FactionCombat.NearestOpponent(Body,64);var target=opponent?opponent.transform.position:player.transform.position;var aim=opponent?opponent.Center:player.Shoulder;var delta=target-transform.position;float distance=delta.magnitude;
            if(distance>64||Mathf.Abs(delta.y)>2.5f)return;
            delta.y=0;var dir=delta.normalized;
            if(distance>(large?3.2f:1.4f))
            {
                var next=transform.position+dir*Time.deltaTime*(large?2.9f:2.25f);
                if(!Physics.Raycast(transform.position+Vector3.up,dir,large?1.5f:.6f,1,QueryTriggerInteraction.Ignore)&&NpcGroundSupport.Floor(next,next.y+.5f,1.7f,out float floor))
                {next.y=floor+.08f;transform.position=next;}
            }
            else if(Time.time>hit&&!Physics.Linecast(transform.position+Vector3.up,aim,1,QueryTriggerInteraction.Ignore))
            {hit=Time.time+(large?1.4f:1.1f);if(opponent)opponent.Damage(large?26:13,dir*3,Body);else player.ReceiveDamage(large?18:7,transform.position);GetComponent<DirectionalPerson>()?.Face(target,.5f);SignalEffects.Impact(aim,-dir,SignalEffects.Cyan,.5f);game.Audio.Play("urban_impact",aim,.3f,2);}
            if(large){head.localPosition=Vector3.up*(3+Mathf.Sin(Time.time*2+phase)*.25f);head.Rotate(0,Time.deltaTime*28,0);for(int i=0;i<legs.Count;i++)legs[i].localRotation=Quaternion.Euler(0,Mathf.Sin(Time.time*4+i)*8,0);}
        }
    }
    public sealed class ErebosPopulation:MonoBehaviour
    {
        readonly List<WorldActor> active=new();float next;int serial;
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||game.Blocked||Time.time<next)return;next=Time.time+3;
            var p=game.Player.transform.position;if(FourCityCatalog.CityAt(p)!=2)return;
            // Interior encounters own their population. Street threats remain at ground level.
            if(p.y>2)return;
            active.RemoveAll(a=>!a);int nearby=0;foreach(var a in active)if(a&&a.Alive&&Vector3.Distance(a.transform.position,p)<90)nearby++;
            if(nearby>=18)return;
            for(int attempt=0;attempt<8;attempt++)
            {float a=(serial+attempt)*2.39996f;var at=p+new Vector3(Mathf.Cos(a)*48,0,Mathf.Sin(a)*48);if(at.x<3040||at.x>5030||at.z>80||at.z< -1930)continue;
                if(Physics.Raycast(at+Vector3.up*3,Vector3.down,out var ground,5,1,QueryTriggerInteraction.Ignore)&&ground.normal.y>.8f&&!Physics.CheckCapsule(ground.point+Vector3.up*.5f,ground.point+Vector3.up*1.5f,.5f,1,QueryTriggerInteraction.Ignore))
                {var threat=ErebosThreat.Spawn(ground.point+Vector3.up*.1f,serial%13==12,75000+serial++,transform);active.Add(threat);break;}}
            for(int i=active.Count-1;i>=0;i--)if(active[i]&&Vector3.Distance(active[i].transform.position,p)>240){Destroy(active[i].gameObject);active.RemoveAt(i);}
        }
    }
}
