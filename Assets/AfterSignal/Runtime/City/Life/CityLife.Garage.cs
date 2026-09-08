using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void Garage()
        {
            Panel("garage","애프터라이트 모터랩","차량 정비와 외형 커스텀 · 잔액 "+LifeState.Credits+" C");
            if(PlayerPrefs.GetInt(UrbanCatalog.Prefix+"Car",0)==0){Body="먼저 차량을 소유한 뒤 방문하세요.";Option("닫기",Dismiss);return;}
            Option("차체 수리 · 150 C",()=>GarageBuy(150,()=>{PlayerPrefs.SetFloat(UrbanCatalog.Prefix+"CarHealth",100);if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned)UrbanSimulation.Instance.Owned.Repair();}));
            Option("차체 도색 · 200 C",GaragePaint);
            Option("스포츠 휠 · 350 C",()=>GarageBuy(350,()=>PlayerPrefs.SetInt(UrbanCatalog.Prefix+"CarWheels",1)));
            Option("스포일러 장착 · 450 C",()=>GarageBuy(450,()=>PlayerPrefs.SetInt(UrbanCatalog.Prefix+"CarSpoiler",1)));
            Option("스포일러 제거 · 무료",()=>{PlayerPrefs.SetInt(UrbanCatalog.Prefix+"CarSpoiler",0);ApplyGarage();Garage();});
            Option("닫기",Dismiss);
        }
        void GaragePaint()
        {
            Panel("garage","차체 도색","색상을 고르면 200 C를 지불하고 도색합니다.");
            string[] labels={"미드나이트 블루","네온 핑크","민트","샴페인 골드","펄 화이트","카본 블랙"};
            for(int i=0;i<labels.Length;i++){int color=i;Option(labels[i],()=>GarageBuy(200,()=>PlayerPrefs.SetInt(UrbanCatalog.Prefix+"CarColor",color)));}
            Option("돌아가기",Garage);
        }
        void GarageBuy(int price,System.Action effect)
        {
            if(!LifeState.Spend(price)){Body="잔액이 부족합니다.";Revision++;return;}
            effect();ApplyGarage();Garage();game.Toast("정비 완료 · "+price+" C");
        }
        void ApplyGarage()
        {
            PlayerPrefs.Save();
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned)UrbanSimulation.Instance.Owned.ApplyCustomization();
        }
    }
}
