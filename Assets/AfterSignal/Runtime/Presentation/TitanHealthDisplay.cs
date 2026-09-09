using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class TitanHealthDisplay:MonoBehaviour
    {
        RiftCreature titan;Canvas canvas;RectTransform panel;Image fill;Text title,warning;
        void Start()
        {
            titan=GetComponent<RiftCreature>();var go=new GameObject("Titan health",typeof(Canvas));go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=25;
            var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            var p=new GameObject("Threat status",typeof(RectTransform),typeof(Image));p.transform.SetParent(go.transform,false);panel=p.GetComponent<RectTransform>();panel.anchorMin=panel.anchorMax=new Vector2(.5f,.78f);panel.sizeDelta=new Vector2(440,72);p.GetComponent<Image>().color=new Color(.025f,.015f,.045f,.88f);
            title=Label(panel,new Vector2(0,21),16);warning=Label(panel,new Vector2(0,-22),13);warning.color=new Color(1,.64f,.4f);
            var bar=new GameObject("Health remaining",typeof(RectTransform),typeof(Image));bar.transform.SetParent(panel,false);var rect=bar.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(16,0);rect.sizeDelta=new Vector2(408,7);fill=bar.GetComponent<Image>();fill.color=new Color(.83f,.14f,.36f);
        }
        static Text Label(Transform parent,Vector2 at,int size)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.rectTransform.sizeDelta=new Vector2(420,24);t.rectTransform.anchoredPosition=at;t.font=Resources.Load<Font>("Fonts/NotoSansKR");if(!t.font)t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;return t;
        }
        void LateUpdate()
        {
            if(!titan||!canvas)return;var cam=Camera.main;var g=GameDirector.Instance;bool show=cam&&g&&titan.Body.Alive&&!g.Blocked&&(titan.AimCenter-cam.transform.position).sqrMagnitude<500*500;
            int index=0;foreach(var body in WorldActor.All)if(body&&body.monster&&body.Alive&&body.Titan){if(body==titan.Body)break;if(cam&&(body.Center-cam.transform.position).sqrMagnitude<500*500)index++;}
            canvas.enabled=show&&index<3;if(!canvas.enabled)return;panel.anchoredPosition=new Vector2(0,-index*80);
            float health=Mathf.Clamp01(titan.Body.health/titan.MaximumHealth);fill.rectTransform.sizeDelta=new Vector2(408*health,7);title.text=titan.name.Replace("잠식 거신 / ","")+"  "+Mathf.CeilToInt(titan.Body.health).ToString("N0");warning.text=titan.AttackWarning.Length>0?titan.AttackWarning:"경찰·방위군 합동 대응 · 발광 기관을 노리세요";
        }
    }
    public sealed class TitanImpact:MonoBehaviour
    {
        Transform[] debris;Vector3[] velocities;float life;Vector3 center;float radius;
        public static void Create(Vector3 at,float radius)
        {
            var fx=new GameObject("Titan ground rupture").AddComponent<TitanImpact>();fx.center=at;fx.radius=radius;fx.debris=new Transform[22];fx.velocities=new Vector3[22];
            for(int i=0;i<22;i++)
            {
                float angle=i*Mathf.PI*2/22;var d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var p=at+d*Random.Range(2,radius*.75f);
                if(Physics.Raycast(p+Vector3.up*4,Vector3.down,out var floor,8,1,QueryTriggerInteraction.Ignore))p=floor.point+Vector3.up*.2f;
                var bit=WorldGeometry.Part(fx.transform,"Fractured pavement",p,new Vector3(Random.Range(.25f,1),.25f,Random.Range(.3f,1.4f)),"DarkMetal");fx.debris[i]=bit.transform;fx.velocities[i]=d*Random.Range(2,7)+Vector3.up*Random.Range(4,10);
                SignalEffects.Beam(p,p+d*Random.Range(2,5),new Color(.62f,.11f,1),.1f,2.1f);
            }
            SignalEffects.Burst(at+Vector3.up,SignalEffects.Gold,32,10);Destroy(fx.gameObject,3);
        }
        void Update()
        {
            float dt=Time.deltaTime;life+=dt;for(int i=0;i<debris.Length;i++){velocities[i]+=Vector3.down*19*dt;debris[i].position+=velocities[i]*dt;debris[i].Rotate(77*dt,54*dt,20*dt);if(life>1.5f)debris[i].localScale*=Mathf.Exp(-dt*3);}
            var g=GameDirector.Instance;if(g&&life<.8f){float d=Vector3.Distance(g.Player.transform.position,center);g.CameraRig.Kick(Mathf.Clamp01(1-d/(radius*4))*.22f);}
        }
    }
}
