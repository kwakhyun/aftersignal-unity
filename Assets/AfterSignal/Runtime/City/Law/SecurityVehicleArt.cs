using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static class SecurityMaterials
    {
        static readonly Dictionary<string,Material> cache=new();
        public static void Apply(GameObject root)
        {
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                var mats=r.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    string n=mats[i]?mats[i].name.Split('.')[0]:"SecurityCarbon";
                    if(!cache.TryGetValue(n,out var m))
                    {
                        if(n.Contains("Glass"))m=new Material(Resources.Load<Shader>("Shaders/StructuralGlass"));
                        else m=new Material(Resources.Load<Material>("Materials/DarkMetal"));
                        m.name=n;Color c=n.Contains("Ceramic")?new Color(.64f,.72f,.76f):n.Contains("Armor")?new Color(.13f,.18f,.17f):n.Contains("Steel")?new Color(.32f,.39f,.43f):n.Contains("Red")?new Color(.44f,.035f,.065f):n.Contains("Cyan")?new Color(.05f,.65f,.8f):new Color(.028f,.036f,.045f);
                        if(n.Contains("Glass"))c=new Color(.1f,.2f,.24f,.08f);
                        m.SetColor("_BaseColor",c);if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",n.Contains("Rubber")?0:.5f);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",n.Contains("Rubber")?.15f:.5f);m.DisableKeyword("_EMISSION");if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);m.enableInstancing=true;cache[n]=m;
                    }
                    mats[i]=m;
                }
                r.sharedMaterials=mats;
            }
        }
    }
    public sealed class SecurityVehicleArt:MonoBehaviour
    {
        CityVehicle car;Transform model,ramp;readonly List<Transform> wheels=new();bool gang,openRamp;Quaternion rampRest;
        public bool Military=>!gang;
        public void OpenRear(){openRamp=true;}
        public static void Install(CityVehicle car,bool gang)
        {
            if(car.GetComponent<SecurityVehicleArt>())return;
            var art=car.gameObject.AddComponent<SecurityVehicleArt>();art.car=car;art.gang=gang;
        }
        IEnumerator Start()
        {
            yield return null;var prefab=Resources.Load<GameObject>("Security/"+(gang?"GangInterceptor":"MilitaryCarrier"));if(!prefab)yield break;
            foreach(var r in car.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            model=Instantiate(prefab,transform).transform;model.localPosition=Vector3.zero;model.localRotation=Quaternion.Euler(0,180,0)*model.localRotation;SecurityMaterials.Apply(model.gameObject);
            foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Wheel")&&t.childCount>0)wheels.Add(t);
            foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name=="RearRamp"){ramp=t;rampRest=t.localRotation;}
            if(!gang&&!GetComponent<ResponseLightbar>())gameObject.AddComponent<ResponseLightbar>();
        }
        void LateUpdate(){if(!car||!model)return;foreach(var w in wheels)w.Rotate(0,car.speed*Time.deltaTime*90,0,Space.Self);if(ramp&&openRamp)ramp.localRotation=Quaternion.RotateTowards(ramp.localRotation,rampRest*Quaternion.Euler(0,-88,0),Time.deltaTime*60);}
    }
}
