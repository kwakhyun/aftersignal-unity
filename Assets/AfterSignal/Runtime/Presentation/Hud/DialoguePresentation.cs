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
            if (!seoPortrait) seoPortrait=StoryPortraits.Bust("서하");
            image.sprite=seoPortrait; image.preserveAspect=true; image.raycastTarget=false;
            return image;
        }
        void BuildConversationPresentation()
        {
            conversationPortrait=Portrait(lifePanel.transform,"Seo conversation portrait");
            conversationLoading=Legible(Label(lifePanel.transform,"",270,242,1060,24,16,mint));
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
            CenterBottom(dialogueBox.GetComponent<RectTransform>(),24,1260,250);
            PlaceLifeElement(storyPortrait.rectTransform,14,-86,306,338);
            PlaceLifeElement(dialogueName.rectTransform,340,20,880,34);dialogueName.fontSize=21;
            PlaceLifeElement(dialogueText.rectTransform,340,64,890,130);dialogueText.fontSize=22;
            PlaceLifeElement(dialogueContinue.GetComponent<RectTransform>(),990,198,250,36);
        }
        void ConversationLayout(CityLife life)
        {
            if (!conversationPortrait) BuildConversationPresentation();
            bool talk=life.Mode=="talk"||life.Mode=="story";bool story=life.Mode=="story";
            conversationPortrait.sprite=story?StoryPortraits.Bust(life.StorySpeaker):seoPortrait;
            preparationTrack.gameObject.SetActive(life.Mode=="shift");
            PlaceLifeElement(preparationMarker.rectTransform,life.Preparation*960,-4,8,28);
            conversationPortrait.gameObject.SetActive(talk&&conversationPortrait.sprite);
            conversationLoading.gameObject.SetActive(talk && life.Sending);
            if(talk)
            {
                CenterBottom(lifePanel.GetComponent<RectTransform>(),22,1300,380);
                PlaceLifeElement(conversationPortrait.rectTransform,10,-80,350,428);
                PlaceLifeElement(lifeTitle.rectTransform,380,18,740,42);
                PlaceLifeElement(lifeClose.GetComponent<RectTransform>(),1150,20,132,38);
                PlaceLifeElement(dialogueScroll,380,72,900,story?236:176);
                if(conversationLayoutRevision!=life.Revision)
                {
                    // Measure wrapped text after applying the narrower text column.
                    PlaceLifeElement(lifeBody.rectTransform,0,0,884,story?236:176);
                    lifeBody.rectTransform.sizeDelta=new Vector2(884,Mathf.Max(story?236:176,lifeBody.preferredHeight+10));
                }
                conversationLayoutRevision=life.Revision;
                PlaceLifeElement(lifeInput.GetComponent<RectTransform>(),380,320,708,44);
                PlaceLifeElement(lifeInput.textComponent.rectTransform,13,9,682,28);
                PlaceLifeElement(((Text)lifeInput.placeholder).rectTransform,13,9,682,28);
                PlaceLifeElement(lifeSend.GetComponent<RectTransform>(),1104,320,176,44);
                for(int i=0;i<lifeOptions.Length;i++)
                    if(lifeOptions[i].gameObject.activeSelf) PlaceLifeElement(lifeOptions[i].GetComponent<RectTransform>(),380+i*300,story?320:266,290,40);
                PlaceLifeElement(conversationLoading.rectTransform,380,248,900,24);
                float age=Time.unscaledTime-life.SendingStarted;
                conversationLoading.text=life.Sending ? (age>10 ? "답변을 정리하고 있어요" : "이야기를 생각하고 있어요") + new string('.',1+(int)(Time.unscaledTime*2)%3) + "   "+age.ToString("0")+"초" : "";
                lifeSend.GetComponentInChildren<Text>().fontSize=16;
                PlaceLifeElement(lifeSend.GetComponentInChildren<Text>().rectTransform,8,0,160,44);
                ButtonText(lifeSend,life.Sending?(life.CanCancelReply?"답변 취소":"답변 기다리는 중"):"말하기  ↵");
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
