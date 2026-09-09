using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CombatRobot:MonoBehaviour
    {
        public WorldActor Body{get;private set;}public int Shots{get;private set;}
        CharacterController motor;Transform form;Vector3 home;readonly PursuitPath path=new();
        readonly List<Transform> joints=new();readonly List<Quaternion> rests=new();float scan,shot,clock,gravity,dead;WorldActor target;bool army;WorldActor attacker;float retaliate;
        public static CombatRobot Create(Vector3 at,bool military)
        {
            var go=new GameObject(military?"국방 전투 로봇 / ATLAS":"경찰 진압 로봇 / SENTINEL",typeof(CharacterController),typeof(WorldActor),typeof(CombatRobot));go.layer=9;go.transform.position=at;
            var r=go.GetComponent<CombatRobot>();r.army=military;r.home=at;r.Body=go.GetComponent<WorldActor>();r.Body.robot=true;r.Body.military=military;r.Body.police=!military;r.Body.health=military?12000:6500;
            r.motor=go.GetComponent<CharacterController>();r.motor.height=military?3.6f:3;r.motor.center=Vector3.up*r.motor.height*.5f;r.motor.radius=.6f;r.motor.stepOffset=.45f;
            r.form=new GameObject("Robot heading").transform;r.form.SetParent(go.transform,false);var prefab=Resources.Load<GameObject>("Security/"+(military?"MilitaryRobot":"PoliceRobot"));
            if(prefab){var model=Instantiate(prefab,r.form);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;SecurityMaterials.Apply(model);}
            foreach(var t in r.form.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Thigh_")||t.name.StartsWith("Shin_")||t.name.StartsWith("Arm_")||t.name=="Sensor"){r.joints.Add(t);r.rests.Add(t.localRotation);}
            NpcSpeech.Say(r,"시민은 안전 구역으로 이동하십시오. 위협 대상을 제압합니다.",4);return r;
        }
        public void Attacked(WorldActor source){attacker=source;retaliate=Time.time+25;scan=0;}
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);clock+=dt;shot-=dt;scan-=dt;
            if(!Body.Alive){motor.enabled=false;dead+=dt;form.localRotation=Quaternion.Slerp(form.localRotation,Quaternion.Euler(80,0,0),dt*2);if(dead>2){VehicleExplosion.Create(transform.position+Vector3.up,3);BlastDamage.Create(transform.position+Vector3.up,7,110,Body);WreckFragments.Shatter(gameObject,7);Destroy(gameObject);}return;}
            if(scan<=0){scan=.45f;target=attacker&&attacker.Alive&&Time.time<retaliate?attacker:FactionCombat.NearestOpponent(Body,180);}
            bool player=!target&&(Time.time<retaliate&&!attacker||!IncidentCommand.Emergency&&WantedSystem.Level>=3);
            Vector3 goal=target?target.Center:player?g.Player.Shoulder:IncidentCommand.Emergency?IncidentCommand.Position:home;
            var d=Vector3.ProjectOnPlane(goal-transform.position,Vector3.up);bool visible=(target||player)&&FactionCombat.Visible(Body.Center,goal,150);bool move=d.magnitude>(visible?army?34:24:3);
            gravity=motor.isGrounded?-2:Mathf.Max(-35,gravity-dt*24);motor.Move((move?path.Direction(transform.position,goal)*(army?3.6f:4.2f):Vector3.zero)*dt+Vector3.up*gravity*dt);
            if(d.sqrMagnitude>1)form.rotation=Quaternion.Slerp(form.rotation,Quaternion.LookRotation(d),dt*5);
            for(int i=0;i<joints.Count;i++){var t=joints[i];float side=t.name.EndsWith("-1")?-1:1;float swing=move?Mathf.Sin(clock*7)*side*25:0;t.localRotation=rests[i]*Quaternion.Euler(t.name.StartsWith("Thigh")?swing:t.name.StartsWith("Shin")?Mathf.Max(0,-swing):t.name.StartsWith("Arm")&&visible?-25:0,0,0);}
            if(visible&&shot<=0){shot=army?.19f:.28f;Shots++;var from=Body.Center+Vector3.up*.65f+form.forward*.95f;FactionCombat.Fire(Body,from,goal,170,army?38:22,army?SignalEffects.Gold:SignalEffects.Cyan,player);g.Audio.PlayGun(GunshotKind.Rifle,from,.65f);}
        }
    }
}
