using UnityEngine;
namespace AfterSignal
{
    public static class PoliceVehicleArt
    {
        public static void Apply(CityVehicle car)
        {
            var root=new GameObject("Dedicated police bodywork").transform;root.SetParent(car.transform,false);
            foreach(var r in car.GetComponentsInChildren<MeshRenderer>())
            {
                if(r.name=="Sculpted chassis"||r.name=="Sedan roof")r.sharedMaterial=Resources.Load<Material>("Materials/DistrictIvory");
            }
            WorldGeometry.Part(root,"Black patrol hood",new Vector3(1.6f,1.1f,0),new Vector3(1.3f,.1f,1.9f),"DarkMetal");
            WorldGeometry.Part(root,"Black rear deck",new Vector3(-1.9f,1.05f,0),new Vector3(.8f,.1f,1.9f),"DarkMetal");
            WorldGeometry.Part(root,"Push bumper",new Vector3(2.68f,.66f,0),new Vector3(.15f,.65f,1.7f),"DarkMetal");
            for(int side=-1;side<=1;side+=2)
            {
                WorldGeometry.Part(root,"Push bar upright",new Vector3(2.72f,.85f,side*.55f),new Vector3(.13f,.95f,.13f),"DarkMetal");
                WorldGeometry.Part(root,"Reflective blue door band",new Vector3(-.15f,.92f,side*1.08f),new Vector3(3.9f,.29f,.035f),"DistrictBlue");
                WorldGeometry.Part(root,"Police door shield",new Vector3(.4f,1.11f,side*1.105f),new Vector3(.42f,.43f,.035f),"Gold",PrimitiveType.Cube);
                Text(root,"POLICE",new Vector3(-.55f,1.12f,side*1.13f),Quaternion.Euler(0,side>0?180:0,0),.08f);
                Text(root,"112",new Vector3(-1.8f,1.25f,side*1.13f),Quaternion.Euler(0,side>0?180:0,0),.05f);
            }
            WorldGeometry.Part(root,"Lightbar mounting bridge",new Vector3(0,2.09f,0),new Vector3(.5f,.09f,1.7f),"DarkMetal");
            WorldGeometry.Part(root,"Radio aerial",new Vector3(-.8f,2.48f,.35f),new Vector3(.03f,1.1f,.03f),"DarkMetal",PrimitiveType.Cylinder);
            Text(root,"POLICE 112",new Vector3(1.65f,1.17f,0),Quaternion.Euler(90,90,0),.07f);
        }
        static void Text(Transform parent,string text,Vector3 at,Quaternion rotation,float size)
        {
            var go=new GameObject(text,typeof(TextMesh));go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localRotation=rotation;
            var mesh=go.GetComponent<TextMesh>();mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontSize=72;mesh.characterSize=size;mesh.color=Color.white;
        }
    }
}