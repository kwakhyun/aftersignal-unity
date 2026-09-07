using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        Text energyReadout,chainText,archiveReadout;HudWeaponGlyph weaponGlyph;int previousHits,chain;float chainUntil;
        Image Wedge(Transform parent,string title,float x,float y,float w,float h,Color color){var go=new GameObject(title,typeof(RectTransform),typeof(HudWedge));go.transform.SetParent(parent,false);var image=go.GetComponent<HudWedge>();image.color=color;image.raycastTarget=false;Rect(image.rectTransform,x,y,w,h);return image;}
        RectTransform BuildReferenceHud()
        {
            var red=new Color(.95f,.35f,.29f);var pale=new Color(.93f,.86f,.62f);
            health=Label(root,"100 / 100",42,27,220,35,25,pale);Label(root,"SEO / LINK",235,44,110,18,10,muted);
            Wedge(root,"Health backing",33,67,296,13,new Color(.02f,.05f,.065f,.9f));hpTrail=Wedge(root,"Damage lag",39,69,278,9,pale);hpBar=Wedge(root,"Vital signal",39,69,278,9,red);
            energyReadout=Label(root,"◇ 100",42,91,115,27,18,new Color(.28f,.78f,.92f));Panel(root,"Energy track",144,107,174,3,new Color(.02f,.07f,.09f));energyBar=Panel(root,"Energy",144,107,174,3,new Color(.27f,.76f,.89f));
            chainText=Label(root,"",42,139,240,58,34,pale,FontStyle.Italic);Label(root,"AFTER / SIGNAL",42,121,240,15,9,muted);
            var inventory=Panel(root,"Record ledger",0,0,240,54,new Color(.02f,.055f,.07f,.38f));Bottom(inventory.rectTransform,38,43,240,54);archiveReadout=Label(inventory.transform,"",12,10,216,32,16,pale);
            var status=Panel(root,"Weapon bay",0,0,290,127,new Color(.02f,.045f,.07f,.55f)).rectTransform;status.anchorMin=status.anchorMax=new Vector2(1,0);status.pivot=new Vector2(1,0);status.anchoredPosition=new Vector2(-32,32);status.sizeDelta=new Vector2(290,127);
            weapon=Label(status,"",15,8,265,23,12,pale,FontStyle.Bold);Wedge(status,"Weapon signal",17,36,257,3,new Color(.18f,.9f,.86f));
            weapon.rectTransform.sizeDelta=new Vector2(178,23);var glyph=new GameObject("Equipped silhouette",typeof(RectTransform),typeof(HudWeaponGlyph));glyph.transform.SetParent(status,false);weaponGlyph=glyph.GetComponent<HudWeaponGlyph>();weaponGlyph.color=new Color(.22f,.91f,.88f);weaponGlyph.raycastTarget=false;Rect(weaponGlyph.rectTransform,192,4,85,29);
            var objectivePanel=Panel(root,"Route objective",0,0,282,92,new Color(.025f,.06f,.075f,.48f)).rectTransform;Right(objectivePanel,28,202,282,92);
            area=Label(objectivePanel,"",11,6,260,17,10,muted,FontStyle.Bold);objective=Label(objectivePanel,"",11,28,260,52,14,white);rail=Label(root,"",0,0,1,1,1,Color.clear);
            var guide=Label(root,"WASD 이동   ·   E 상호작용   ·   ESC 조작 / 설정",0,0,650,22,11,muted);CenterBottom(guide.rectTransform,22,650,22);guide.alignment=TextAnchor.MiddleCenter;
            notice=Label(root,"",0,200,850,34,17,mint,FontStyle.Bold);CenterTop(notice.rectTransform,200,850,34);notice.alignment=TextAnchor.MiddleCenter;
            prompt=Label(root,"",0,0,820,38,18,pale,FontStyle.Bold);CenterBottom(prompt.rectTransform,118,820,38);prompt.alignment=TextAnchor.MiddleCenter;
            anchorHint=Label(root,"",0,0,840,26,12,muted);CenterBottom(anchorHint.rectTransform,88,840,26);anchorHint.alignment=TextAnchor.MiddleCenter;
            cursor=Label(root,"+",0,0,30,30,24,mint).rectTransform;candidate=Label(root,"◇",0,0,60,60,41,mint,FontStyle.Bold).rectTransform;safeMarker=Label(root,"▼ SAFE",0,0,150,42,22,pale,FontStyle.Bold).rectTransform;
            bossPanel=Panel(root,"Conductor",0,0,700,66,new Color(.025f,.045f,.07f,.65f)).gameObject;CenterBottom(bossPanel.GetComponent<RectTransform>(),180,700,66);
            bossText=Label(bossPanel.transform,"",16,7,668,32,16,white,FontStyle.Bold);Panel(bossPanel.transform,"Boss track",16,48,668,6,new Color(.25f,.13f,.19f));bossBar=Panel(bossPanel.transform,"Boss signal",16,48,668,6,red);
            BuildCivicHud(objectivePanel);
            arsenalPanel.transform.SetParent(status,false);Rect(arsenalPanel.GetComponent<RectTransform>(),0,43,290,79);arsenalPanel.GetComponent<Image>().color=Color.clear;
            return objectivePanel;
        }
        void UpdateReferenceHud()
        {
            var p=game.Player;weaponGlyph.SetWeapon(p.Weapon);energyReadout.text="◇ "+Mathf.CeilToInt(p.Energy);archiveReadout.text="◇  ARCHIVE  "+game.Memories.ToString("00");
            if(p.ConfirmedHits>previousHits){chain+=p.ConfirmedHits-previousHits;chainUntil=Time.unscaledTime+2.1f;}previousHits=p.ConfirmedHits;if(Time.unscaledTime>chainUntil)chain=0;chainText.text=chain>1?chain+"  CHAIN":"";
        }
        void BuildRouteMinimap(Transform map)
        {
            Label(map,"SIGNAL MAP  /  고도",14,8,290,20,12,mint,FontStyle.Bold);
            Panel(map,"Map upper trim",5,3,310,1,new Color(.3f,.62f,.69f));Panel(map,"Map lower trim",5,162,310,1,new Color(.3f,.62f,.69f));
            foreach(var c in FindObjectsByType<BoxCollider>()){
                var b=c.bounds;if(b.size.y>1.3f||b.size.x<3||b.max.x<0||b.min.x>game.stageLength)continue;
                float x=Mathf.Clamp(b.min.x,0,game.stageLength)/game.stageLength*292,w=Mathf.Min(b.max.x,game.stageLength)/game.stageLength*292-x;
                Panel(map,"Walkable map segment",14+x,140-Mathf.Clamp(b.max.y,0,36)*2.6f,Mathf.Max(2,w),4,new Color(.25f,.46f,.56f));
            }
            foreach(var a in GrappleAnchor.All)if(a)Label(map,"◇",9+a.transform.position.x/game.stageLength*292,130-Mathf.Clamp(a.transform.position.y,0,36)*2.6f,18,18,12,new Color(1,.73f,.34f));
            mapPlayer=Label(map,"▼",0,0,18,20,16,new Color(1,.82f,.32f),FontStyle.Bold).rectTransform;
        }
    }
}
