using UnityEngine;
namespace AfterSignal
{
    [DefaultExecutionOrder(1100)]
    public sealed class HeldArmory:MonoBehaviour
    {
        PlayerMotor player;Transform model;int item=-1;
        void LateUpdate()
        {
            if(!player)player=GetComponent<PlayerMotor>();if(!player||!player.Equipment)return;
            var equipment=player.Equipment;
            bool visible=equipment.Extended&&player.Controller.enabled&&player.Health>0;
            if(!visible){if(model)model.gameObject.SetActive(false);return;}
            if(item!=equipment.ItemId){item=equipment.ItemId;Build(equipment.Slot);}
            if(!model)return;model.gameObject.SetActive(true);
            var heading=(player.Aim-player.Shoulder).normalized;if(heading.sqrMagnitude<.1f)heading=Vector3.forward;
            model.position=player.Shoulder-Vector3.up*.17f+heading*.25f+Vector3.Cross(Vector3.up,heading)*.18f;
            model.rotation=Quaternion.LookRotation(heading,Vector3.up)*Quaternion.Euler(equipment.Reloading?24:0,0,equipment.Reloading?-18:0);
            if(player.AttackTime>0)model.position-=heading*.035f*(player.AttackTime/Mathf.Max(.01f,player.AttackDuration));
        }
        void Build(int slot)
        {
            if(model)Destroy(model.gameObject);model=new GameObject("Held / "+ArmoryInventory.Items[item].name).transform;model.SetParent(transform,false);
            if(slot==4){Part("Fragmentation casing",new Vector3(0,0,.12f),new Vector3(.13f,.18f,.13f),"DarkMetal",PrimitiveType.Capsule);Part("Safety lever",new Vector3(0,.1f,.12f),new Vector3(.025f,.05f,.15f),"Chrome");return;}
            if(slot==5)
            {
                var tube=Part("Launcher tube",new Vector3(0,.04f,.15f),new Vector3(.19f,.55f,.19f),"DarkMetal",PrimitiveType.Cylinder);tube.localRotation=Quaternion.Euler(90,0,0);
                var collar=Part("Muzzle collar",new Vector3(0,.04f,.73f),new Vector3(.24f,.035f,.24f),"Chrome",PrimitiveType.Cylinder);collar.localRotation=Quaternion.Euler(90,0,0);
                Part("Trigger grip",new Vector3(0,-.12f,.12f),new Vector3(.075f,.2f,.1f),"DarkMetal");Part("Optical sight",new Vector3(.15f,.17f,.25f),new Vector3(.08f,.09f,.26f),"Chrome");return;
            }
            bool shotgun=slot==6;
            Part("Receiver",new Vector3(0,0,.15f),new Vector3(.11f,.15f,.4f),"DarkMetal");
            Part("Stock",new Vector3(0,-.025f,-.17f),new Vector3(.075f,.18f,.28f),"DarkMetal");
            Part("Shoulder pad",new Vector3(0,-.025f,-.32f),new Vector3(.1f,.2f,.035f),"Chrome");
            Part("Grip",new Vector3(0,-.14f,.06f),new Vector3(.07f,.17f,.1f),"DarkMetal");
            Part(shotgun?"Pump fore-end":"Ventilated handguard",new Vector3(0,0,.45f),new Vector3(.12f,.12f,.26f),"Chrome");
            var barrel=Part("Barrel",new Vector3(0,.025f,.62f),new Vector3(.035f,.23f,.035f),"DarkMetal",PrimitiveType.Cylinder);barrel.localRotation=Quaternion.Euler(90,0,0);
            if(!shotgun){var mag=Part("Detachable magazine",new Vector3(0,-.16f,.24f),new Vector3(.065f,.22f,.12f),"Chrome");mag.localRotation=Quaternion.Euler(-12,0,0);}
            Part("Top sight rail",new Vector3(0,.1f,.2f),new Vector3(.06f,.035f,.3f),"Chrome");
            for(int i=0;i<5;i++)Part("Vent slot",new Vector3(.061f,.018f,.36f+i*.04f),new Vector3(.006f,.045f,.02f),"DarkMetal");
            if(item==9){var scope=Part("Precision optic",new Vector3(0,.17f,.21f),new Vector3(.055f,.14f,.055f),"DarkMetal",PrimitiveType.Cylinder);scope.localRotation=Quaternion.Euler(90,0,0);}
        }
        Transform Part(string name,Vector3 at,Vector3 size,string material,PrimitiveType shape=PrimitiveType.Cube)=>WorldGeometry.Part(model,name,at,size,material,shape).transform;
    }
}
