using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class NpcSpeech:MonoBehaviour
    {
        Transform bubble;Text words;float until;int priority;
        public string CurrentLine=>words?words.text:"";
        public static void Say(Component actor,string line,float seconds=3.5f,int importance=0)
        {
            if(!actor)return;
            var speech=actor.GetComponent<NpcSpeech>();if(!speech)speech=actor.gameObject.AddComponent<NpcSpeech>();
            speech.Show(FactionVoice.Filter(actor,line,importance),seconds,importance);
        }
        void Show(string line,float seconds,int importance)
        {
            if(Time.time<until&&importance<priority)return;priority=importance;
            if(!bubble)
            {
                var go=new GameObject("NPC speech bubble",typeof(RectTransform),typeof(Canvas));bubble=go.transform;bubble.SetParent(transform,false);bubble.localScale=Vector3.one*.012f;
                var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=150;
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(260,72);
                var panel=new GameObject("Bubble",typeof(RectTransform),typeof(Image));panel.transform.SetParent(bubble,false);
                var pr=panel.GetComponent<RectTransform>();pr.sizeDelta=rect.sizeDelta;
                panel.GetComponent<Image>().color=new Color(.025f,.055f,.085f,.94f);
                var text=new GameObject("Speech",typeof(RectTransform),typeof(Text));text.transform.SetParent(bubble,false);
                text.GetComponent<RectTransform>().sizeDelta=new Vector2(244,64);
                words=text.GetComponent<Text>();words.font=Resources.Load<Font>("Fonts/NotoSansKR");words.fontSize=22;words.alignment=TextAnchor.MiddleCenter;words.color=Color.white;
            }
            words.text=line;until=Time.time+seconds;bubble.gameObject.SetActive(true);
        }
        void LateUpdate()
        {
            if(!bubble)return;
            bubble.position=transform.position+Vector3.up*2.65f;
            var camera=Camera.main;
            float depth=camera?Vector3.Dot(bubble.position-camera.transform.position,camera.transform.forward):10;
            bubble.gameObject.SetActive(Time.time<until&&depth>.8f);
            if(camera)
            {
                bubble.rotation=camera.transform.rotation;
                // Keep nearby speech readable without covering the room or inheriting actor scale.
                float pixelLimit=260f*Mathf.Max(.65f,Screen.height/900f);
                float worldScale=Mathf.Min(.008f,pixelLimit*2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/(Mathf.Max(1,Screen.height)*260));
                var inherited=transform.lossyScale;
                bubble.localScale=new Vector3(worldScale/Mathf.Max(.001f,Mathf.Abs(inherited.x)),worldScale/Mathf.Max(.001f,Mathf.Abs(inherited.y)),worldScale/Mathf.Max(.001f,Mathf.Abs(inherited.z)));
            }
        }
        void OnDestroy(){if(bubble)Destroy(bubble.gameObject);}
    }
}
