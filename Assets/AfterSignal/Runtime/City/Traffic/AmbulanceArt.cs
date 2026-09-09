using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class AmbulanceArt:MonoBehaviour
    {
        CityVehicle car;Transform model;readonly List<Transform> wheels=new();readonly List<Transform> doors=new();readonly List<Quaternion> rests=new();
        static readonly Dictionary<string,Material> materials=new();
        IEnumerator Start()
        {
            car=GetComponent<CityVehicle>();yield return null;yield return null;
            car.GetComponent<VehicleCabin>()?.SetCrew("Doctor",1);
            var asset=Resources.Load<GameObject>("Response/Ambulance");if(!asset)yield break;
            foreach(var r in GetComponentsInChildren<MeshRenderer>())if(!r.name.StartsWith("Response "))r.enabled=false;
            model=Instantiate(asset,transform).transform;model.localRotation=Quaternion.Euler(0,180,0)*model.localRotation;
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {
                var slots=r.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    string key=slots[i]?slots[i].name.Split('.')[0]:"MedicalPearl";
                    if(!materials.TryGetValue(key,out var m))
                    {
                        bool glass=key.Contains("Glass");m=new Material(glass?Resources.Load<Shader>("Shaders/StructuralGlass"):Shader.Find("Universal Render Pipeline/Lit"));
                        var c=key.Contains("Pearl")?new Color(.86f,.89f,.86f):key.Contains("Orange")?new Color(1,.21f,.025f):key.Contains("Teal")?new Color(.025f,.3f,.32f):key.Contains("Steel")?new Color(.4f,.47f,.5f):key.Contains("Red")?new Color(.75f,.025f,.04f):key.Contains("Cyan")?new Color(.02f,.5f,.85f):new Color(.03f,.04f,.055f);
                        m.SetColor("_BaseColor",glass?new Color(.16f,.25f,.3f,.08f):c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.48f);m.enableInstancing=true;materials[key]=m;
                    }
                    slots[i]=m;
                }
                r.sharedMaterials=slots;
            }
            foreach(var t in model.GetComponentsInChildren<Transform>())
            {if(t.name.StartsWith("Wheel")&&t.childCount>0)wheels.Add(t);if(t.name.StartsWith("RearDoor")&&t.childCount>0){doors.Add(t);rests.Add(t.localRotation);}}
            if(!GetComponent<ResponseLightbar>())gameObject.AddComponent<ResponseLightbar>();
        }
        void LateUpdate()
        {
            if(!car||!model)return;foreach(var w in wheels)w.Rotate(0,car.speed*Time.deltaTime*90,0,Space.Self);
            var rescue=GetComponent<EmergencyAmbulance>();bool open=rescue&&rescue.Phase>=1&&rescue.Phase<=3;
            for(int i=0;i<doors.Count;i++)doors[i].localRotation=Quaternion.RotateTowards(doors[i].localRotation,rests[i]*Quaternion.Euler(0,0,open?(i%2==0?-105:105):0),Time.deltaTime*90);
        }
    }
}
