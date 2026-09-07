using UnityEngine;
namespace AfterSignal
{
    public sealed class CityTrafficSignal:MonoBehaviour
    {
        public Renderer[] eastWest,northSouth;public Renderer walkLamp;public TextMesh walkLabel;int previous=-1;
        void Update(){int a=CityRoadNetwork.Signal(true),b=CityRoadNetwork.Signal(false),w=CityRoadNetwork.Walk?1:0,code=a+b*3+w*9;if(code==previous)return;previous=code;Show(eastWest,a);Show(northSouth,b);if(walkLamp)walkLamp.sharedMaterial=Resources.Load<Material>(w==1?"Materials/TrafficGreen":"Materials/TrafficRed");if(walkLabel)walkLabel.text=w==1?"WALK":"WAIT";}
        void LateUpdate(){if(walkLabel&&Camera.main)walkLabel.transform.rotation=Camera.main.transform.rotation;}
        static void Show(Renderer[] lamps,int active){for(int i=0;i<lamps.Length;i++)if(lamps[i])lamps[i].sharedMaterial=Resources.Load<Material>(i%3==active?"Materials/Traffic"+(active==0?"Red":active==1?"Amber":"Green"):"Materials/Rubber");}
    }
}
