using UnityEngine;
namespace AfterSignal
{
    public static class MilitaryArmory
    {
        public static void Build(Transform parent,Vector3 at)
        {
            var root=new GameObject("방위부 무기고 / FIELD ARMORY").transform;root.SetParent(parent,false);root.position=at;var b=new CityGeometry(root);
            b.Box("Armory deck",new(0,-.12f,0),new(14,.24f,17),"DefenseDeck",true);
            b.Box("Armory rear wall",new(0,2.6f,8.5f),new(14,5.2f,.4f),"DarkMetal",true);
            for(int side=-1;side<=1;side+=2){b.Box("Armory side",new(side*7,2.6f,0),new(.4f,5.2f,17),"Metal",true);b.Box("Armory entry wing",new(side*5,2.6f,-8.5f),new(4,5.2f,.4f),"Metal",true);}
            b.Box("Armory roof",new(0,5.3f,0),new(14.5f,.3f,17.5f),"Metal",true);b.Sign("FIELD ARMORY / 무기·탄약 보급",new(0,4.25f,-8.8f),.22f);
            for(int i=0;i<ArmoryInventory.Items.Length;i++)
            {
                var item=ArmoryInventory.Items[i];var p=new Vector3(i<7?-5.2f:5.2f,0,-6.7f+(i<7?i:i-7)*2.1f);
                var rack=new GameObject(item.name,typeof(InteractionPoint),typeof(MilitaryWeaponRack));rack.transform.SetParent(root,false);rack.transform.localPosition=p;
                rack.GetComponent<MilitaryWeaponRack>().Item=i;var interact=rack.GetComponent<InteractionPoint>();interact.title=item.name+" · 보급 / 장착";interact.radius=2.4f;
                b.Box("Equipment bench",p+Vector3.up*.9f,new(2.2f,.15f,1.45f),"Metal",true);
                b.Box("Ammunition locker",p+Vector3.up*.4f,new(1.6f,.7f,1.1f),"DefenseDeck",true);
                b.Box("Rack status",p+new Vector3(0,1.12f,-.68f),new(1.7f,.05f,.04f),"NeonCyan");
                Draw(rack.transform,item.slot);b.Sign(item.name,p+new Vector3(0,1.85f,0),.1f);
            }
            b.Finish();
        }
        static void Draw(Transform p,int slot)
        {
            if(slot<2){WorldGeometry.Part(p,"Forged blade",new(.15f,1.15f,0),new(slot==1?1.5f:1.2f,.05f,slot==1?.22f:.09f),"Chrome");WorldGeometry.Part(p,"Wrapped grip",new(-.7f,1.15f,0),new(.42f,.09f,.13f),"DarkMetal");WorldGeometry.Part(p,"Guard",new(-.45f,1.15f,0),new(.06f,.08f,.3f),"Metal");return;}
            if(slot==4){for(int j=0;j<4;j++){WorldGeometry.Part(p,"Fragmentation grenade",new(-.6f+j*.4f,1.22f,0),new(.25f,.4f,.25f),"Metal",PrimitiveType.Capsule);WorldGeometry.Part(p,"Safety lever",new(-.6f+j*.4f,1.43f,0),new(.17f,.04f,.07f),"Chrome");}return;}
            if(slot==5){var tube=WorldGeometry.Part(p,"Launcher tube",new(0,1.35f,0),new(.26f,.87f,.26f),"DefenseDeck",PrimitiveType.Cylinder);tube.transform.localRotation=Quaternion.Euler(0,0,90);WorldGeometry.Part(p,"Optical sight",new(.1f,1.58f,0),new(.3f,.12f,.11f),"DarkMetal");return;}
            float scale=slot==2?.65f:1;WorldGeometry.Part(p,"Weapon receiver",new(0,1.15f,0),new(.65f*scale,.17f,.12f),"DarkMetal");WorldGeometry.Part(p,"Barrel",new(.64f*scale,1.16f,0),new(.7f*scale,.07f,.07f),"Chrome");WorldGeometry.Part(p,"Magazine",new(-.12f,1.1f,.17f),new(.15f,.12f,.3f),"Metal");WorldGeometry.Part(p,"Stock",new(-.58f*scale,1.15f,0),new(.46f*scale,.1f,.2f),"DefenseDeck");WorldGeometry.Part(p,"Scope",new(.03f,1.33f,0),new(.35f*scale,.09f,.09f),"Metal");
        }
    }
    public sealed class MilitaryWeaponRack:MonoBehaviour
    {
        public int Item;
        public void Use(){if(!ArmoryInventory.Issue(Item))return;var g=GameDirector.Instance;g.Player.Equipment.Select(ArmoryInventory.Items[Item].slot);g.Toast(ArmoryInventory.Items[Item].name+" 장착 · 탄약 보급 완료",4);g.Audio.Play("ui_confirm",transform.position,.2f,1);}
    }
    public sealed partial class CityLife
    {
        public void MilitarySupply(int category=-1)
        {
            Panel("service","FIELD ARMORY / 군부대 무기고","등록된 방위 장비를 장착하고 탄약을 보급받습니다.");
            if(category<0){string[] labels={"검","대검","권총","소총","수류탄","로켓 발사기","산탄총"};for(int i=0;i<7;i++){int slot=i;Option(labels[i],()=>MilitarySupply(slot));}return;}
            for(int i=0;i<ArmoryInventory.Items.Length;i++)if(ArmoryInventory.Items[i].slot==category){int id=i;Option(ArmoryInventory.Items[i].name+" · 장착 / 탄약 보급",()=>{ArmoryInventory.Issue(id);game.Player.Equipment.Select(category);Body=ArmoryInventory.Items[id].name+" 장착 및 보급 완료";Revision++;});}
            Option("다른 장비 보기",()=>MilitarySupply());
        }
    }
}
