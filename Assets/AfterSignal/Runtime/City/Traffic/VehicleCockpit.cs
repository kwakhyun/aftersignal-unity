using UnityEngine;

namespace AfterSignal
{
    // Local instruments are fitted below the eye line; exterior meshes keep their existing silhouette.
    public sealed class VehicleCockpit : MonoBehaviour
    {
        CityVehicle car;
        Transform instruments, wheel, speedBar;
        void Awake() => car=GetComponent<CityVehicle>();

        void LateUpdate()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;
            bool visible=g&&sim&&sim.Current==car&&sim.SeatIndex==0&&g.CameraRig.FirstPersonVehicle;
            if(visible&&!instruments)Build(g.CameraRig);
            if(!instruments)return;
            instruments.gameObject.SetActive(visible);
            if(!visible)return;
            if(wheel)wheel.localRotation=Quaternion.AngleAxis(-car.Steering*95,Vector3.right);
            if(speedBar){var size=speedBar.localScale;size.z=Mathf.Lerp(.025f,.24f,Mathf.Clamp01(Mathf.Abs(car.speed)/(car.IsAircraft?130:car.IsWatercraft?25:car.TopSpeed)));speedBar.localScale=size;}
        }

        void Build(CameraRig rig)
        {
            instruments=new GameObject("Driver instrument station").transform;
            instruments.SetParent(transform,false);
            instruments.localPosition=transform.InverseTransformPoint(rig.CockpitPosition(car))+Vector3.up*.24f;
            bool tank=car.type==CityVehicleType.Tank,bike=car.type==CityVehicleType.Motorcycle;
            float width=bike?.55f:car.IsAircraft?1.25f:car.IsWatercraft?1.5f:1.05f;
            float forward=tank?.7f:.55f;
            Part(instruments,"Instrument cowl",new Vector3(forward,-.51f,0),new Vector3(.24f,.27f,width),"Rubber");
            int screens=bike?1:tank?2:3;
            for(int i=0;i<screens;i++)
            {
                float z=(i-(screens-1)*.5f)*(width/screens);
                Part(instruments,"Recessed display bezel",new Vector3(forward-.126f,-.45f,z),new Vector3(.03f,.2f,width/screens*.83f),"DarkMetal");
                Part(instruments,"Navigation display glass",new Vector3(forward-.145f,-.45f,z),new Vector3(.015f,.16f,width/screens*.72f),"DistrictBlue");
                for(int row=0;row<3;row++)Part(instruments,"Telemetry line",new Vector3(forward-.16f,-.405f-row*.035f,z),new Vector3(.008f,.008f,width/screens*(row==0?.58f:.36f)),"CyanFX");
                if(i==0)speedBar=Part(instruments,"RPM indicator",new Vector3(forward-.166f,-.5f,z),new Vector3(.008f,.013f,.12f),"CyanFX").transform;
                for(int b=0;b<3;b++)Part(instruments,"Panel switch",new Vector3(forward-.143f,-.57f,z+(b-1)*.06f),new Vector3(.026f,.018f,.028f),b==0?"RedFX":"Chrome");
            }
            if(tank)return;
            if(car.IsAircraft||bike)
            {
                float z=car.IsAircraft?.26f:0;
                Part(instruments,"Control column",new Vector3(.27f,-.67f,z),new Vector3(.045f,.32f,.055f),"Chrome");
                Part(instruments,bike?"Handlebar":"Flight control grip",new Vector3(.27f,-.52f,z),new Vector3(.08f,.055f,bike?.75f:.27f),"Rubber");
            }
            else
            {
                wheel=new GameObject("Steering wheel").transform;wheel.SetParent(instruments,false);wheel.localPosition=new Vector3(.23f,-.50f,0);
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI*2/16;
                    var rim=Part(wheel,"Leather steering rim",new Vector3(0,Mathf.Sin(a)*.185f,Mathf.Cos(a)*.185f),new Vector3(.037f,.037f,.08f),"Rubber");
                    rim.transform.localRotation=Quaternion.AngleAxis(-a*Mathf.Rad2Deg+90,Vector3.right);
                }
                Part(wheel,"Wheel hub",Vector3.zero,new Vector3(.06f,.085f,.09f),"DarkMetal");
                for(int s=-1;s<=1;s+=2)Part(wheel,"Wheel spoke",new Vector3(0,-.018f,s*.105f),new Vector3(.03f,.036f,.15f),"Chrome");
            }
        }

        static GameObject Part(Transform p,string n,Vector3 at,Vector3 size,string material)=>WorldGeometry.Part(p,n,at,size,material);
    }
}
