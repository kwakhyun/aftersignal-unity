using UnityEngine;
namespace AfterSignal
{
    public sealed class TitanBarrage:MonoBehaviour
    {
        const float Radius=48;RiftCreature titan;float age,pulse,scorch;LineRenderer[] beams,cores;int hits;
        public int VictimsHit=>hits;
        public static void Fire(RiftCreature source){if(source.GetComponent<TitanBarrage>())return;source.gameObject.AddComponent<TitanBarrage>().titan=source;}
        void Start()
        {
            beams=new LineRenderer[12];cores=new LineRenderer[12];
            for(int i=0;i<beams.Length;i++)
            {
                var go=new GameObject("Titan radial incineration beam",typeof(LineRenderer));go.transform.SetParent(transform,false);var line=beams[i]=go.GetComponent<LineRenderer>();line.sharedMaterial=Resources.Load<Material>("Materials/CyanFX");line.startColor=new Color(.7f,.3f,1);line.endColor=new Color(.3f,.04f,.8f);line.positionCount=2;line.widthMultiplier=.42f;line.numCapVertices=3;
                var core=new GameObject("Incineration / hot white core",typeof(LineRenderer));core.transform.SetParent(transform,false);var bright=cores[i]=core.GetComponent<LineRenderer>();bright.sharedMaterial=line.sharedMaterial;bright.startColor=new Color(1,.92f,1);bright.endColor=new Color(.9f,.68f,1);bright.positionCount=2;bright.widthMultiplier=.16f;bright.numCapVertices=4;
            }
            GameDirector.Instance?.Audio.Play("titan_barrage",transform.position,.55f,4);CombatCameraImpulse.Blast(transform.position,5);
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked)return;if(!titan||!titan.Body.Alive){Destroy(this);return;}
            age+=Time.deltaTime;var from=titan.AimCenter;
            bool sear=age>=scorch;if(sear)scorch=age+.45f;
            for(int i=0;i<beams.Length;i++)
            {
                float angle=i*Mathf.PI*2/beams.Length+age*1.7f;var endpoint=transform.position+new Vector3(Mathf.Cos(angle)*Radius,1.2f+Mathf.Sin(age*4+i)*1.1f,Mathf.Sin(angle)*Radius);
                var delta=endpoint-from;if(Ballistics.Cast(from,delta.normalized,delta.magnitude,transform,out var hit,true))endpoint=hit.point;
                beams[i].enabled=PresentationSettings.Effects>0;beams[i].SetPosition(0,from);beams[i].SetPosition(1,endpoint);
                beams[i].widthMultiplier=.68f+Mathf.Sin(age*35+i)*.16f;
                cores[i].enabled=beams[i].enabled;cores[i].SetPosition(0,from);cores[i].SetPosition(1,endpoint);
                if(sear)CombatVfx.Emit("Incineration / hot contact spray",endpoint,Vector3.up,new Color(1,.48f,.12f),9,6,.2f,.55f);
            }
            if(age>=pulse){pulse=age+.35f;Burn(from);}
            if(age>2.6f){TitanDemolition.Strike(from,Radius,titan.Body);Destroy(this);}
        }
        void Burn(Vector3 from)
        {
            foreach(var a in WorldActor.All.ToArray())
            {
                if(!a||a==titan.Body||!a.Alive||a.monster||(a.Center-from).sqrMagnitude>Radius*Radius||!BlastDamage.Exposed(from,a.Center,a.transform))continue;
                float distance=Vector3.Distance(a.Center,from);float damage=a.MaxHealth*(distance<Radius*.55f?1.45f:.3f);a.Damage(damage,(a.Center-from).normalized*3,titan.Body);hits++;
                CombatVfx.Emit("Scorched target smoke",a.Center,Vector3.up,new Color(.14f,.13f,.15f,.7f),5,1,.5f,2,true);
            }
            var sim=UrbanSimulation.Instance;if(sim)foreach(var car in sim.Cars.ToArray())
            {
                if(!car||car.Wrecked)continue;var at=WarheadDamage.HullPoint(car,from);if((at-from).sqrMagnitude>Radius*Radius||!BlastDamage.Exposed(from,at,car.transform))continue;
                car.Damage(car.MaxHealth*.38f,at,titan.Body);hits++;CombatVfx.Emit("Vehicle thermal ignition",at,Vector3.up,new Color(1,.3f,.04f),10,2,.5f,.55f);
            }
            var g=GameDirector.Instance;if(g&&(g.Player.Shoulder-from).sqrMagnitude<Radius*Radius&&BlastDamage.Exposed(from,g.Player.Shoulder,g.Player.transform))g.Player.ReceiveDamage(Vector3.Distance(g.Player.Shoulder,from)<Radius*.55f?120:24,from);
        }
        void OnDestroy(){if(beams!=null)foreach(var beam in beams)if(beam)Destroy(beam.gameObject);if(cores!=null)foreach(var core in cores)if(core)Destroy(core.gameObject);}
    }
}
