using UnityEngine;
namespace AfterSignal
{
    public enum PropUse {Bench,Vending,Water,ATM,Phone,Television,Locker,Books,Medical,Power,Trash,Workshop}
    public sealed class UsableProp:MonoBehaviour
    {
        public PropUse use;float next;bool on;public int Uses {get;private set;}
        public void Use(GameDirector g)
        {
            if(Time.time<next){g.Toast("잠시 후 다시 이용하세요.");return;}
            if(use==PropUse.ATM){CityLife.Instance.FacilityServices(FacilityFunction.Bank,"거리 ATM");return;}
            if(use==PropUse.Vending&&!LifeState.Spend(25)){g.Toast("25 C가 필요합니다.");return;}
            next=Time.time+(use==PropUse.Books||use==PropUse.Water?15:use==PropUse.Trash?45:2);Uses++;
            switch(use)
            {
                case PropUse.Bench:g.Player.Heal(5);g.Player.RestoreEnergy(15);g.Toast("벤치에서 숨을 골랐습니다. 체력 +5 / 에너지 +15");break;
                case PropUse.Vending:g.Player.Heal(10);g.Player.RestoreEnergy(40);g.Toast("음료를 꺼냈습니다. 25 C 사용");break;
                case PropUse.Water:g.Player.RestoreEnergy(12);g.Toast("깨끗한 물을 마셨습니다. 에너지 +12");break;
                case PropUse.Phone:CitySafety.Alarm(g.Player.transform.position,null);g.Toast("긴급 상황을 신고했습니다. 인근 순찰대에 전달합니다.");break;
                case PropUse.Television:on=!on;g.Toast(on?"뉴스: 신호 균열 인근 통제. 국방대응부가 현장에 출동했습니다.":"TV 전원을 껐습니다.");foreach(var r in GetComponentsInChildren<Renderer>())if(r.sharedMaterial&&r.sharedMaterial.HasProperty("_EmissionColor")){var b=new MaterialPropertyBlock();b.SetColor("_EmissionColor",on?new Color(.4f,1.2f,1.5f):Color.black);r.SetPropertyBlock(b);}break;
                case PropUse.Locker:CityLife.Instance.FacilityServices(FacilityFunction.Home,"개인 보관함");break;
                case PropUse.Books:g.Player.RestoreEnergy(8);g.Toast("기록: 잔향체는 지워진 시민의 기억이 신호 균열에서 다시 뭉친 형상이다.",6);break;
                case PropUse.Medical:CityLife.Instance.FacilityServices(FacilityFunction.Clinic,"의료 단말");break;
                case PropUse.Power:FacilityOperation.Begin("grid",240,1);break;
                case PropUse.Trash:if(Random.value<.3f)CreditDrop.Spawn(transform.position+Vector3.up*.2f,Random.Range(3,18));g.Toast("분리수거함을 정리했습니다.");break;
                case PropUse.Workshop:FacilityOperation.Begin("repair",230,1);break;
            }
            g.Audio.Play("ui_confirm",transform.position,.15f,1);
        }
    }
}
