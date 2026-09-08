using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        Image conversationPortrait, storyPortrait, preparationTrack, preparationMarker;
        Text conversationLoading;
        RectTransform dialogueScroll;
        Sprite seoPortrait;
        int conversationLayoutRevision=-1;
        Image Portrait(Transform parent,string name)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(parent,false);
            var image = go.GetComponent<Image>();
            if (!seoPortrait) seoPortrait=Resources.Load<Sprite>("Art/Portraits/SeoDialogue");
            image.sprite=seoPortrait; image.preserveAspect=true; image.raycastTarget=false;
            return image;
        }
        void BuildConversationPresentation()
        {
            conversationPortrait=Portrait(lifePanel.transform,"Seo conversation portrait");
            conversationLoading=Label(lifePanel.transform,"",270,242,1060,24,16,mint);
            var viewport = Panel(lifePanel.transform,"Conversation viewport",270,72,1150,150,Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            dialogueScroll=viewport.rectTransform;
            viewport.raycastTarget=true;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();
            lifeBody.transform.SetParent(viewport.transform,false);
            PlaceLifeElement(lifeBody.rectTransform,0,0,1140,150);
            scroll.viewport=viewport.rectTransform; scroll.content=lifeBody.rectTransform;
            scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            preparationTrack=Panel(lifePanel.transform,"Preparation timing",36,335,968,20,new Color(.18f,.25f,.28f));
            Panel(preparationTrack.transform,"Perfect timing",340,0,290,20,new Color(.28f,.68f,.49f));
            preparationMarker=Panel(preparationTrack.transform,"Timing marker",0,-4,8,28,new Color(1,.82f,.35f));
            storyPortrait=Portrait(dialogueBox.transform,"Seo story portrait");
            CenterBottom(dialogueBox.GetComponent<RectTransform>(),28,1400,260);
            PlaceLifeElement(storyPortrait.rectTransform,12,8,220,244);
            PlaceLifeElement(dialogueName.rectTransform,250,24,1100,30);
            PlaceLifeElement(dialogueText.rectTransform,250,70,1100,130);
        }
        void ConversationLayout(CityLife life)
        {
            if (!conversationPortrait) BuildConversationPresentation();
            bool talk=life.Mode=="talk"||life.Mode=="story";bool story=life.Mode=="story";
            preparationTrack.gameObject.SetActive(life.Mode=="shift");
            PlaceLifeElement(preparationMarker.rectTransform,life.Preparation*960,-4,8,28);
            conversationPortrait.gameObject.SetActive(talk);
            conversationLoading.gameObject.SetActive(talk && life.Sending);
            if(talk)
            {
                CenterBottom(lifePanel.GetComponent<RectTransform>(),22,1480,370);
                PlaceLifeElement(conversationPortrait.rectTransform,8,8,246,354);
                PlaceLifeElement(lifeTitle.rectTransform,270,20,990,42);
                PlaceLifeElement(lifeClose.GetComponent<RectTransform>(),1320,20,132,38);
                PlaceLifeElement(dialogueScroll,270,74,1160,story?224:152);
                if(conversationLayoutRevision!=life.Revision)PlaceLifeElement(lifeBody.rectTransform,0,0,1140,Mathf.Max(story?224:152,lifeBody.preferredHeight+10));
                conversationLayoutRevision=life.Revision;
                PlaceLifeElement(lifeInput.GetComponent<RectTransform>(),270,304,990,44);
                PlaceLifeElement(lifeInput.textComponent.rectTransform,13,9,960,28);
                PlaceLifeElement(((Text)lifeInput.placeholder).rectTransform,13,9,960,28);
                PlaceLifeElement(lifeSend.GetComponent<RectTransform>(),1276,304,176,44);
                for(int i=0;i<lifeOptions.Length;i++)
                    if(lifeOptions[i].gameObject.activeSelf) PlaceLifeElement(lifeOptions[i].GetComponent<RectTransform>(),270+i*360,story?306:254,345,36);
                float age=Time.unscaledTime-life.SendingStarted;
                conversationLoading.text=life.Sending ? (age>10 ? "답변을 정리하고 있어요" : "이야기를 생각하고 있어요") + new string('.',1+(int)(Time.unscaledTime*2)%3) + "   "+age.ToString("0")+"초" : "";
                lifeSend.GetComponentInChildren<Text>().fontSize=16;
                PlaceLifeElement(lifeSend.GetComponentInChildren<Text>().rectTransform,8,0,160,44);
                ButtonText(lifeSend,life.Sending?"답변 기다리는 중":"말하기  ↵");
            }
            else
            {
                Center(lifePanel.GetComponent<RectTransform>(),1040,690);
                PlaceLifeElement(lifeTitle.rectTransform,36,26,820,44);
                PlaceLifeElement(lifeClose.GetComponent<RectTransform>(),870,28,132,38);
                PlaceLifeElement(dialogueScroll,36,91,968,life.Options.Exists(o=>ShopItemArt.Get(o.image))?78:320);
                PlaceLifeElement(lifeBody.rectTransform,0,0,968,Mathf.Max(dialogueScroll.sizeDelta.y,lifeBody.preferredHeight+10));
            }
        }
    }
}
