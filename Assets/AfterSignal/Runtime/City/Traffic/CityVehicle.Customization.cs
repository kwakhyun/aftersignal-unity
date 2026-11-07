using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityVehicle
    {
        int customColor=-1;Material stockWheel;
        static readonly Color[] colors={new Color(.06f,.16f,.3f),new Color(.64f,.06f,.32f),new Color(.12f,.54f,.46f),new Color(.68f,.5f,.23f),new Color(.8f,.83f,.8f),new Color(.04f,.045f,.05f)};
        // Paid Motor Lab cosmetics belong to the registered car only; any other car shows its stock look.
        public bool Registered=>UrbanSimulation.Instance&&UrbanSimulation.Instance.Owned==this;
        public void Repair(){if(Wrecked)return;InitializeDurability();health=MaxHealth;exploded=false;ApplyDamageLook();UrbanSimulation.Instance?.SaveCar();}
        public void ApplyCustomization()
        {
            if(IsSpecial||type==CityVehicleType.Tank||type==CityVehicleType.Motorcycle)return;
            bool mine=Registered;
            customColor=mine?PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarColor",-1):-1;
            ApplyDamageLook();
            bool wheels=mine&&PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarWheels",0)>0;
            foreach(var mesh in GetComponentsInChildren<MeshRenderer>())
                if(mesh.name=="Wheel alloy")
                {
                    if(wheels){if(!stockWheel)stockWheel=mesh.sharedMaterial;mesh.sharedMaterial=Resources.Load<Material>("Materials/Gold");}
                    else if(stockWheel)mesh.sharedMaterial=stockWheel;
                }
            var spoiler=transform.Find("Custom spoiler");
            bool show=mine&&PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarSpoiler",0)>0;
            if(show&&!spoiler)
            {
                var part=new GameObject("Custom spoiler");part.transform.SetParent(transform,false);spoiler=part.transform;
                WorldGeometry.Part(spoiler,"Aero wing",new Vector3(-HalfLength*.65f,1.7f,0),new Vector3(.65f,.12f,2.4f),"Metal");
                for(int i=-1;i<=1;i+=2)WorldGeometry.Part(spoiler,"Wing mount",new Vector3(-HalfLength*.65f,1.4f,i*.7f),new Vector3(.12f,.6f,.13f),"Metal");
            }
            if(spoiler)spoiler.gameObject.SetActive(show);
        }
    }
}
