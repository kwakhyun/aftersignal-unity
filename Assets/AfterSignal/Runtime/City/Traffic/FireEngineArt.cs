using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class FireEngineArt:MonoBehaviour
    {
        static Material paint;Transform root;
        IEnumerator Start()
        {
            yield return null;yield return null;
            foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            root=new GameObject("119 dedicated pump engine").transform;root.SetParent(transform,false);
            if(!paint){paint=new Material(Resources.Load<Material>("Materials/Metal"));paint.name="Fire engine red enamel";paint.color=new Color(.65f,.025f,.02f);}
            Box("Ladder chassis",new(0,.72f,0),new(10,.4f,3),"DarkMetal");
            Red("Forward crew cab",new(3.1f,1.7f,0),new(3.4f,1.6f,2.85f));
            Box("Windscreen",new(4.83f,2.35f,0),new(.04f,.75f,2.55f),"Glass");
            Box("Crew roof",new(3.1f,2.86f,0),new(3.5f,.16f,2.95f),"Metal");
            Red("Equipment body",new(-1.75f,1.85f,0),new(5.9f,2.1f,2.85f));
            for(int s=-1;s<=1;s+=2)
            {
                Box("Cab side glass",new(3.1f,2.32f,s*1.44f),new(2.7f,.75f,.04f),"Glass");
                Box("Reflective side stripe",new(-.1f,1.35f,s*1.49f),new(9,.14f,.035f),"DistrictIvory");
                for(int i=0;i<3;i++)
                {
                    Box("Equipment roller door",new(-3.55f+i*1.85f,1.93f,s*1.45f),new(1.65f,1.35f,.055f),"Metal");
                    for(int j=0;j<6;j++)Box("Roller slat",new(-3.55f+i*1.85f,1.42f+j*.19f,s*1.49f),new(1.6f,.025f,.02f),"Chrome");
                    Box("Locker handle",new(-3.55f+i*1.85f,1.46f,s*1.53f),new(.5f,.06f,.08f),"DarkMetal");
                }
                for(int i=0;i<3;i++){var wheel=WorldGeometry.Part(root,"Heavy response wheel",new(i==0?3.45f:-2.4f+(i-1)*1.7f,.72f,s*1.55f),new(1.4f,.22f,1.4f),"DarkMetal",PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(90,0,0);}
                Box("Roof ladder rail",new(-1.6f,3.2f,s*.52f),new(6.1f,.1f,.09f),"Chrome");
                Box("Front lamp",new(4.86f,1.32f,s*.9f),new(.04f,.23f,.55f),"DistrictIvory");
                Box("Rear warning lamp",new(-4.78f,1.25f,s*1.1f),new(.08f,.33f,.3f),"RedFX");
            }
            for(int i=0;i<13;i++)Box("Ladder rung",new(-4.4f+i*.46f,3.2f,0),new(.08f,.09f,1.1f),"Chrome");
            Box("Front impact bumper",new(4.94f,.9f,0),new(.23f,.36f,3.1f),"Chrome");
            Box("Grille",new(4.88f,1.8f,0),new(.07f,.46f,1.4f),"DarkMetal");
            Box("Roof water turret",new(1,3.3f,0),new(.65f,.65f,.65f),"Chrome");
            Box("Water cannon barrel",new(1.5f,3.65f,0),new(1.3f,.19f,.19f),"DarkMetal");
            Box("Emergency lightbar",new(3.2f,3.08f,0),new(.38f,.22f,2.3f),"RedFX");
            GetComponent<VehicleCabin>()?.SetCrew("Firefighter",2);
        }
        Transform Box(string n,Vector3 p,Vector3 s,string m)=>WorldGeometry.Part(root,n,p,s,m).transform;
        void Red(string n,Vector3 p,Vector3 s){var t=Box(n,p,s,"Metal");t.GetComponent<Renderer>().sharedMaterial=paint;}
    }
}

