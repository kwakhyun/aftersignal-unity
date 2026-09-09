using UnityEngine;
namespace AfterSignal
{
    public sealed class ResponseLightbar:MonoBehaviour
    {
        Renderer[] lenses=new Renderer[2];Light[] lights=new Light[2];CityVehicle car;
        static Material[] tint;
        void Start()
        {
            car=GetComponent<CityVehicle>();float height=GetComponent<EmergencyAmbulance>()?3.3f:GetComponent<TacticalTransport>()?3.05f:2.15f;
            if(tint==null){tint=new Material[2];for(int i=0;i<2;i++){var c=i==0?new Color(.8f,.015f,.025f):new Color(.015f,.08f,.9f);tint[i]=new Material(Shader.Find("Universal Render Pipeline/Lit"));tint[i].SetColor("_BaseColor",c);tint[i].EnableKeyword("_EMISSION");tint[i].SetColor("_EmissionColor",c*.6f);tint[i].enableInstancing=true;}}
            for(int i=0;i<2;i++)
            {var p=WorldGeometry.Part(transform,i==0?"Response red lens":"Response blue lens",new Vector3(.6f,height,i==0?-.6f:.6f),new Vector3(.5f,.2f,.65f),i==0?"RedFX":"CyanFX");lenses[i]=p.GetComponent<Renderer>();lenses[i].sharedMaterial=tint[i];lights[i]=p.AddComponent<Light>();lights[i].color=i==0?new Color(1,.02f,.01f):new Color(.01f,.12f,1);lights[i].range=13;lights[i].intensity=5;lights[i].shadows=LightShadows.None;}
        }
        void Update(){bool alive=car&&!car.Wrecked;for(int i=0;i<2;i++){bool on=alive&&(Mathf.Repeat(Time.time,.65f)<.325f)==(i==0);if(lenses[i])lenses[i].enabled=on;if(lights[i])lights[i].enabled=on;}}
        void OnDisable(){for(int i=0;i<2;i++){if(lights[i])lights[i].enabled=false;if(lenses[i])lenses[i].enabled=false;}}
    }
}
