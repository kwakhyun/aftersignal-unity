using UnityEngine;
using UnityEngine.UI;

namespace AfterSignal
{
    public sealed partial class SignalHud
    {
        GameObject noaRadio;
        Text noaLine,noaStatus;
        Image noaProgress;
        string lastNoaLine;
        int lastNoaSecond=-1;
        bool lastNoaClearing;

        void UpdateNoaRadio()
        {
            var support=WantedSystem.Instance?.NoaSupport;
            bool show=support!=null&&support.Visible&&!game.Blocked&&!(UrbanSimulation.Instance&&UrbanSimulation.Instance.MapOpen);
            if(!show){if(noaRadio)noaRadio.SetActive(false);return;}
            if(!noaRadio)
            {
                noaRadio=Panel(root,"Noa pursuit support",0,0,1360,220,ink).gameObject;
                CenterBottom(noaRadio.GetComponent<RectTransform>(),28,1360,220);
                var portrait=Portrait(noaRadio.transform,"Noa radio portrait");portrait.sprite=StoryPortraits.Bust("노아");
                PlaceLifeElement(portrait.rectTransform,12,-160,330,364);
                Label(noaRadio.transform,"노아  /  보안망 연결",366,20,940,32,22,mint,FontStyle.Bold);
                noaLine=Label(noaRadio.transform,"",366,68,952,88,26,white);
                noaStatus=Label(noaRadio.transform,"",366,164,952,26,18,mint);
                var track=Panel(noaRadio.transform,"Record deletion track",366,204,952,3,new Color(.18f,.29f,.33f));
                noaProgress=Panel(track.transform,"Record deletion progress",0,0,952,3,mint);
            }
            noaRadio.SetActive(true);
            int second=Mathf.CeilToInt(support.Remaining);
            if(lastNoaLine!=support.Line){lastNoaLine=support.Line;noaLine.text=lastNoaLine;}
            if(second!=lastNoaSecond||support.Clearing!=lastNoaClearing)
            {
                lastNoaSecond=second;lastNoaClearing=support.Clearing;
                noaStatus.text=support.Clearing?$"수배 기록 삭제 중…  {second}초":"수배 해제 완료  ·  추적 중단";
            }
            noaProgress.rectTransform.sizeDelta=new Vector2(952*(support.Clearing?1-support.Remaining/NoaWantedSupport.ClearDelay:1),3);
        }
    }
}
