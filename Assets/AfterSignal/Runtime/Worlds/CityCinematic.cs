using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class CityCinematic:MonoBehaviour
    {
        public static bool Active{get;private set;}
        public static int Completed{get;private set;}
        Canvas overlay;Text title,subtitle;Image fade;Action done;bool finished,skip;bool cameraEnabled;float fov;Vector3 restorePosition;Quaternion restoreRotation;
        readonly System.Collections.Generic.List<GameObject> apparitions=new();
        public static void Play(string title,string speaker,string line,Vector3 building,Vector3 objective,Action done)
        {if(Active)return;var go=new GameObject("Directed story sequence");var c=go.AddComponent<CityCinematic>();c.done=done;c.StartCoroutine(c.Sequence(title,speaker,line,building,objective));}
        void BuildUI()
        {
            var go=new GameObject("Cinematic letterbox",typeof(Canvas),typeof(CanvasScaler));overlay=go.GetComponent<Canvas>();overlay.renderMode=RenderMode.ScreenSpaceOverlay;overlay.sortingOrder=500;go.transform.SetParent(transform);
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1600,900);
            RectTransform Panel(string name,Vector2 min,Vector2 max,Color color){var p=new GameObject(name,typeof(RectTransform),typeof(Image));p.transform.SetParent(overlay.transform,false);var r=p.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;p.GetComponent<Image>().color=color;p.GetComponent<Image>().raycastTarget=false;return r;}
            Panel("Top cinema bar",new(0,.86f),Vector2.one,Color.black);Panel("Bottom cinema bar",Vector2.zero,new(1,.16f),Color.black);
            title=Label("Chapter",new(.06f,.88f),new(.85f,.95f),24);subtitle=Label("Dialogue",new(.13f,.025f),new(.87f,.145f),24);subtitle.alignment=TextAnchor.MiddleCenter;
            var hint=Label("Skip",new(.86f,.89f),new(.97f,.95f),16);hint.text="ESC  건너뛰기";
            fade=Panel("Shot fade",Vector2.zero,Vector2.one,Color.black).GetComponent<Image>();
        }
        Text Label(string name,Vector2 min,Vector2 max,int size)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(overlay.transform,false);var t=go.GetComponent<Text>();t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.fontSize=size;t.color=new(.82f,.95f,.97f);t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return t;}
        IEnumerator Sequence(string heading,string speaker,string line,Vector3 building,Vector3 objective)
        {
            Active=true;var game=GameDirector.Instance;cameraEnabled=game.CameraRig.enabled;game.CameraRig.enabled=false;var camera=Camera.main;fov=camera.fieldOfView;restorePosition=camera.transform.position;restoreRotation=camera.transform.rotation;
            game.Player.Rope.Release();FindAnyObjectByType<SignalHud>()?.CinematicVisibility(false);BuildUI();title.text=heading;
            // Establish the destination, then cut on the archive pulse into the testimony.
            Vector3[] eyes={building+new Vector3(-65,45,-90),objective+new Vector3(-4,2.5f,-6),objective+new Vector3(4,2,-3)};
            Vector3[] focus={building+Vector3.up*14,objective+new Vector3(0,1.35f,0),game.Player.Shoulder};
            string[] parts=Split(line);float[] duration={3.6f,6.2f,5.2f};
            for(int shot=0;shot<3&&!skip;shot++)
            {
                camera.fieldOfView=shot==0?52:shot==1?44:37;
                subtitle.text=shot==0?"":speaker+"\n"+parts[Mathf.Min(shot-1,parts.Length-1)];
                if(shot==1)MemoryFigures(objective);
                float time=0;
                while(time<duration[shot]&&!skip)
                {
                    if(game.Paused){yield return null;continue;}
                    time+=Time.unscaledDeltaTime;float u=Mathf.SmoothStep(0,1,time/duration[shot]);var eye=eyes[shot]+new Vector3(shot==0?u*11:u*.8f,shot==0?-u*3:0,u*(shot==0?13:1.3f));
                    // Short interior shots stay inside the room rather than through a wall.
                    if(shot>0&&Physics.Linecast(focus[shot],eye,out var hit,1,QueryTriggerInteraction.Ignore))eye=hit.point+(focus[shot]-hit.point).normalized*.25f;
                    camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(focus[shot]-eye));
                    float alpha=Mathf.Max(1-time/.6f,(time-duration[shot]+.45f)/.45f);fade.color=new Color(0,0,0,Mathf.Clamp01(alpha));
                    for(int witness=0;witness<apparitions.Count;witness++)if(apparitions[witness])
                    {
                        var apparition=apparitions[witness];apparition.transform.rotation=Quaternion.Euler(0,camera.transform.eulerAngles.y,0);
                        var sr=apparition.GetComponent<SpriteRenderer>();sr.sprite=PeopleArt.Get(witness==0?"AbyssEngineer":"AbyssCitizen",0,time%3.2f<2.3f?3:0);sr.color=new Color(.28f,.84f,1,.78f+Mathf.Sin(time*4+witness)*.08f);
                    }
                    yield return null;
                }
            }
            Finish();
        }
        static string[] Split(string text){int middle=text.Length/2,index=text.IndexOf('.',middle);if(index<0||index>text.Length-10)index=text.LastIndexOf('.',middle);if(index<0)return new[]{text,text};return new[]{text.Substring(0,index+1),text.Substring(index+1).Trim()};}
        void MemoryFigures(Vector3 at)
        {
            var projector=new GameObject("Memory projection lattice").transform;projector.SetParent(transform);projector.position=at;var geometry=new CityGeometry(projector);
            for(int side=-1;side<=1;side+=2){var p=new Vector3(side*1.8f,.03f,.8f);geometry.Ring(p,1,1,.035f,"NeonCyan",48);geometry.Ring(p,1.2f,1.2f,.02f,"NeonCyan",48);for(int ray=0;ray<6;ray++){float a=ray*Mathf.PI/3;geometry.Beam(p+new Vector3(Mathf.Cos(a)*1.2f,0,Mathf.Sin(a)*1.2f),p+Vector3.up*2.6f,.012f,"NeonCyan");}}
            geometry.Finish();
            for(int i=0;i<2;i++){var go=new GameObject("Projected witness",typeof(SpriteRenderer));go.transform.position=at+new Vector3(i==0?-1.8f:1.8f,.06f,.8f);go.transform.SetParent(transform);var sr=go.GetComponent<SpriteRenderer>();sr.sprite=PeopleArt.Get(i==0?"AbyssEngineer":"AbyssCitizen",0,3);sr.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");sr.color=new Color(.24f,.82f,1,.82f);apparitions.Add(go);}
        }
        public void Skip()=>skip=true;
        void Update(){if(Keyboard.current!=null&&(Keyboard.current.escapeKey.wasPressedThisFrame||Keyboard.current.spaceKey.wasPressedThisFrame))Skip();}
        void Finish()
        {if(finished)return;finished=true;var g=GameDirector.Instance;if(g&&g.CameraRig){g.CameraRig.enabled=cameraEnabled;Camera.main.fieldOfView=fov;Camera.main.transform.SetPositionAndRotation(restorePosition,restoreRotation);g.CameraRig.Snap();}FindAnyObjectByType<SignalHud>()?.CinematicVisibility(true);Active=false;Completed++;var callback=done;done=null;Destroy(gameObject);callback?.Invoke();}
        void OnDestroy(){Active=false;if(!finished){FindAnyObjectByType<SignalHud>()?.CinematicVisibility(true);var g=GameDirector.Instance;if(g&&g.CameraRig){g.CameraRig.enabled=cameraEnabled;if(Camera.main)Camera.main.fieldOfView=fov;}}}
    }
}
