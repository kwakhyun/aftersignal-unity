using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityLife
    {
        bool HasRegisteredCar=>SaveStore.GetInt(UrbanCatalog.Prefix+"Car",0)>0||UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned;
        public void Garage()
        {
            Panel("garage","애프터라이트 모터랩","차량 정비와 외형 커스텀 · 잔액 "+LifeState.Credits+" C\n정비와 커스텀은 등록된 내 차에만 적용됩니다.");
            string candidate=UrbanSimulation.RegistrationCandidate;int fee=GameTuning.Active.carRegistrationFee;
            if(!HasRegisteredCar)
            {
                Body=candidate!=null?"등록된 차량이 없습니다 · 타고 온 "+candidate+"을(를) 내 차로 등록할 수 있습니다.":"먼저 차량을 소유한 뒤 방문하세요.";
                if(candidate!=null)Option("이 차량을 내 차로 등록 · "+fee+" C / "+candidate,()=>RegisterCar(fee));
                Option("닫기",Dismiss);return;
            }
            // Version 0 marks CarHealth as a percentage, so 100 loads as full health; a live car's Repair() re-saves it as an absolute value.
            Option("차체 수리 · 150 C",()=>GarageBuy(150,()=>{SaveStore.SetInt(UrbanCatalog.Prefix+"CarDurabilityVersion",0);SaveStore.SetFloat(UrbanCatalog.Prefix+"CarHealth",100);if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned)UrbanSimulation.Instance.Owned.Repair();}));
            Option("차체 도색 · 200 C",GaragePaint);
            Option("스포츠 휠 · 350 C",()=>GarageBuy(350,()=>SaveStore.SetInt(UrbanCatalog.Prefix+"CarWheels",1)));
            Option("스포일러 장착 · 450 C",()=>GarageBuy(450,()=>SaveStore.SetInt(UrbanCatalog.Prefix+"CarSpoiler",1)));
            Option("스포일러 제거 · 무료",()=>{SaveStore.SetInt(UrbanCatalog.Prefix+"CarSpoiler",0);ApplyGarage();Garage();});
            if(candidate!=null)Option("이 차량을 내 차로 등록 · "+fee+" C / "+candidate,()=>RegisterCar(fee));
            Option("닫기",Dismiss);
        }
        void RegisterCar(int fee)
        {
            if(WantedSystem.Level>0){Body="수배 중에는 차량을 등록할 수 없습니다.";Revision++;return;}
            if(UrbanSimulation.RegistrationCandidate==null){Body="등록할 차량이 없습니다.";Revision++;return;}
            if(!LifeState.Spend(fee)){Body="등록비가 부족합니다 · "+fee+" C";Revision++;return;}
            string title=UrbanSimulation.RegistrationCandidate;
            if(!UrbanSimulation.RegisterCandidate()){LifeState.Earn(fee);Body="등록할 차량이 없습니다.";Revision++;return;}
            Garage();Body="등록 완료 · "+title+"이(가) 서하의 차가 되었습니다.\n이전 차량의 도색과 튜닝은 이전되지 않습니다.";Revision++;
            game.Toast("차량 등록 완료 · "+title,5);
        }
        void GaragePaint()
        {
            Panel("garage","차체 도색","색상을 고르면 200 C를 지불하고 도색합니다.");
            string[] labels={"미드나이트 블루","네온 핑크","민트","샴페인 골드","펄 화이트","카본 블랙"};
            for(int i=0;i<labels.Length;i++){int color=i;Option(labels[i],()=>GarageBuy(200,()=>SaveStore.SetInt(UrbanCatalog.Prefix+"CarColor",color)));}
            Option("돌아가기",Garage);
        }
        void GarageBuy(int price,System.Action effect)
        {
            if(!LifeState.Spend(price)){Body="잔액이 부족합니다.";Revision++;return;}
            effect();ApplyGarage();Garage();game.Toast("정비 완료 · "+price+" C");
        }
        void ApplyGarage()
        {
            SaveStore.Save();
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned)UrbanSimulation.Instance.Owned.ApplyCustomization();
        }
    }
}
