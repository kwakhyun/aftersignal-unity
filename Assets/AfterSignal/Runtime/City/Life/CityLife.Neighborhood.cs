namespace AfterSignal
{
    public sealed partial class CityLife
    {
        public void ApartmentDirectory(int home,int page=0)
        {
            Panel("neighbors",home==0?"북문아파트 · 주민 안내":"북문아파트 2지구 · 주민 안내","주민의 집을 방문할 수 있습니다. 낮에는 직장에, 밤에는 집에 있습니다.");
            int shown=0,matched=0;
            for(int i=0;i<ResidentialWorld.ResidentCount;i++)
            {
                if(ResidentialWorld.Home(i)!=home)continue;
                if(matched++<page*5)continue;
                int resident=i;
                if(shown++>=5)break;
                Option(ResidentialWorld.Names[i]+" · "+ResidentialWorld.Address(i)+" · "+ResidentialWorld.Activity(i,LifeState.Hour),()=>{Dismiss();ResidentialWorld.EnterHome(home,resident);});
            }
            if(page==0)Option("\uB2E4\uC74C \uD638\uC2E4 \u2192",()=>ApartmentDirectory(home,1));else Option("\u2190 \uC774\uC804 \uD638\uC2E4",()=>ApartmentDirectory(home,0));
            Option("돌아가기",Dismiss);
        }
    }
}
