namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void Armory(int category=-1)
        {
            Panel("service","BLACKLINE / 무기 상점","잔액 "+LifeState.Credits.ToString("N0")+" C · 보유 장비는 무료로 다시 장착할 수 있습니다.\n1 검 · 2 대검 · 3 권총 · 4 소총 · 5 수류탄 · 6 바주카 · 7 산탄총");
            if(category<0)
            {
                string[] names={"검 · 와키자시 / 카타나","대검","권총","소총","수류탄","바주카포","산탄총"};
                for(int i=0;i<7;i++){int group=i;Option(names[i],()=>Armory(group));}return;
            }
            for(int i=0;i<ArmoryInventory.Items.Length;i++)if(ArmoryInventory.Items[i].slot==category)
            {int id=i;var item=ArmoryInventory.Items[id];Option(item.name+" · "+(ArmoryInventory.Owns(id)&&category!=4?"보유":item.price+" C"),()=>ArmoryDetails(id));}
            Option("다른 종류 보기",()=>Armory());
        }
        void ArmoryDetails(int id)
        {
            var item=ArmoryInventory.Items[id];Panel("service",item.name,item.description+"\n잔액 "+LifeState.Credits.ToString("N0")+" C"+(item.slot>=3?"\n보유 탄약 "+ArmoryInventory.Rounds(id)+" / 예비탄 "+ArmoryInventory.Reserve(id):""));
            Option(ArmoryInventory.Owns(id)&&item.slot!=4?"장착":"구매 / "+item.price+" C",()=>
            {
                if(!ArmoryInventory.Buy(id)){Body="잔액이 부족합니다.";Revision++;return;}
                game.Player.Equipment.Select(item.slot);ArmoryDetails(id);Body+="\n구매·장착 완료. 숫자 "+(item.slot+1)+" 키로 선택하세요.";Revision++;
                game.Audio.Play("ui_confirm",game.Player.Shoulder,.18f,1);
            });
            if(item.slot>=3&&ArmoryInventory.Owns(id))Option("탄약 보충 / "+(item.slot==5?180:item.slot==4?120:75)+" C",()=>{bool ok=ArmoryInventory.BuyAmmo(id);ArmoryDetails(id);Body+=ok?"\n탄약 보충 완료":"\n잔액이 부족합니다.";Revision++;});
            Option("목록으로",()=>Armory(item.slot));
        }
    }
}
