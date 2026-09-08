using UnityEngine;
namespace AfterSignal
{
    public sealed class CreditDrop:MonoBehaviour
    {
        public int Amount {get;private set;}
        public bool Collected {get;private set;}
        float born,vertical=3;Vector3 drift;Transform bundle;
        public static CreditDrop From(WorldActor actor)
        {
            if(actor.helicopter||actor.protectedResident||actor.monster||Random.value>.78f)return null;
            var npc=actor.GetComponent<CityNpc>();int cash=Random.Range(actor.police||actor.military?35:8,actor.gang?210:actor.police||actor.military?150:125);
            if(npc)cash=npc.TakeCash(cash);if(cash<=0)return null;
            return Spawn(actor.transform.position+Vector3.up*.5f,cash);
        }
        public static CreditDrop Spawn(Vector3 at,int amount)
        {
            var go=new GameObject("Dropped credits / "+amount+" C");go.transform.position=at;
            var d=go.AddComponent<CreditDrop>();d.Amount=Mathf.Max(1,amount);return d;
        }
        void Start()
        {
            born=Time.time;var scatter=Random.insideUnitCircle*.8f;drift=new Vector3(scatter.x,0,scatter.y);
            bundle=WorldGeometry.Part(transform,"Credit chips",Vector3.zero,new Vector3(.28f,.08f,.18f),"Chrome").transform;
            WorldGeometry.Part(bundle,"Credit stripe",Vector3.up*.51f,new Vector3(.25f,.06f,1.04f),"CyanFX");
            var ip=gameObject.AddComponent<InteractionPoint>();ip.title=Amount+" C 줍기";ip.kind=InteractionKind.LifeService;ip.radius=2.2f;
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;
            vertical-=Time.deltaTime*12;Vector3 p=transform.position+(drift+Vector3.up*vertical)*Time.deltaTime;
            foreach(var h in Physics.RaycastAll(transform.position+Vector3.up*.3f,Vector3.down,1.8f,1,QueryTriggerInteraction.Ignore))
                if(!h.collider.GetComponentInParent<CityVehicle>()&&h.normal.y>.65f&&p.y<h.point.y+.06f){p.y=h.point.y+.06f;vertical=0;drift=Vector3.zero;break;}
            transform.position=p;
            if(bundle)bundle.localRotation=Quaternion.Euler(0,(Time.time-born)*55,0);
            if(!Collected&&Time.time-born>.7f&&g.Player.Controller.enabled&&Vector3.Distance(g.Player.transform.position,p)<1.35f)Collect();
            if(Time.time-born>180||p.y< -60)Destroy(gameObject);
        }
        public void Collect()
        {
            if(Collected)return;Collected=true;LifeState.Earn(Amount);var g=GameDirector.Instance;
            g?.Toast("재화 습득  +"+Amount+" C",1.4f);g?.Audio.Play("ui_confirm",transform.position,.16f,1);Destroy(gameObject);
        }
    }
}
