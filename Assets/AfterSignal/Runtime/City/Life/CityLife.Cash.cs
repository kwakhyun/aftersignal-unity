namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void CashMenu(CashContainer container)
        {
            if(!container)return;
            Panel("cash",container.Home?"서하의 개인 금고":container.Bank?"은행 현금 금고":"매장 계산대",container.Home?"소지금 "+LifeState.Credits.ToString("N0")+" C\n금고 보관금 "+LifeState.HomeCash.ToString("N0")+" C":container.Empty?"오늘의 현금은 이미 회수되었습니다.":"현금을 강제로 가져가면 직원과 목격자가 신고할 수 있습니다.");
            if(container.Home)
            {
                foreach(int value in new[]{500,5000}){int amount=value;Option(amount.ToString("N0")+" C 넣기",()=>{LifeState.StoreHomeCash(amount);CashMenu(container);});Option(amount.ToString("N0")+" C 꺼내기",()=>{LifeState.TakeHomeCash(amount);CashMenu(container);});}
                Option("소지금 모두 보관",()=>{LifeState.StoreHomeCash(LifeState.Credits);CashMenu(container);});
                Option("보관금 모두 꺼내기",()=>{LifeState.TakeHomeCash(LifeState.HomeCash);CashMenu(container);});
            }
            else if(!container.Empty)Option(container.Bank?"금고 강탈 · 12초":"계산대 현금 탈취 · 4초",container.StartRobbery);
        }
    }
}
