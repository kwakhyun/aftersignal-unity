using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CityBusService:MonoBehaviour
    {
        public static CityBusService Instance{get;private set;}
        public const int Fare=30;
        public CityVehicle Riding{get;private set;}
        public readonly List<CityBusLine> Lines=new List<CityBusLine>();
        public string Prompt{get;private set;}="";
        public int PaidTrips{get;private set;}
        Renderer[] hero;
        bool[] heroVisible;
        bool requestedStop;
        GameDirector game;
        void Awake(){Instance=this;}
        IEnumerator Start()
        {
            game=GameDirector.Instance;
            while(!game||!game.Ready||!UrbanSimulation.Instance)yield return null;
            yield return null;
            if(game.stage!=StageId.UrbanCity)yield break;
            for(int row=0;row<5;row++)
            {
                var route=new List<Vector3>();
                float z=-280+row*140;
                for(int col=4;col>=0;col--)route.Add(new Vector3(100+col*140,.02f,z+5));
                route.Add(new Vector3(50,.02f,z+5));route.Add(new Vector3(40,.02f,z));
                route.Add(new Vector3(50,.02f,z-5));route.Add(new Vector3(730,.02f,z-5));
                route.Add(new Vector3(740,.02f,z));route.Add(new Vector3(730,.02f,z+5));
                var car=UrbanSimulation.Instance.Spawn(route[0]+Vector3.right*8,true,2);
                car.route=route.ToArray();car.waypoint=0;car.transform.rotation=Quaternion.Euler(0,180,0);
                var line=car.gameObject.AddComponent<CityBusLine>();line.row=row;Lines.Add(line);
            }
        }
        public void BeforeInput(ref ControlFrame input)
        {
            Prompt="";
            if(Riding)
            {
                var line=Riding.GetComponent<CityBusLine>();
                Prompt="시내버스 "+(line.row+101)+"번 · 다음 정류장 "+line.NextStopName+" · E 하차벨 / F 뛰어내리기";
                if(input.exit){JumpOff();input.exit=false;return;}
                if(input.interact){requestedStop=true;input.interact=false;game.Toast("하차벨을 눌렀습니다. 다음 정류장에서 내려요.");}
                if(line.Stopped&&requestedStop)Leave();
                if(Riding){input=ControlFrame.Empty;game.Player.transform.position=Riding.transform.position+Vector3.up*1.2f;}
                return;
            }
            if(UrbanSimulation.Instance&&UrbanSimulation.Instance.Driving)return;
            CityBusLine nearest=null;
            foreach(var line in Lines)
            {
                if(!line||!line.Stopped)continue;
                float d=Vector3.Distance(game.Player.transform.position,line.BoardPoint);
                if(d<7)nearest=line;
            }
            if(nearest)
            {
                Prompt="E · "+(nearest.row+101)+"번 버스 탑승 / "+Fare+" C";
                if(input.interact){input.interact=false;Board(nearest);}
            }
            else
            {
                foreach(var line in Lines)if(line)
                    for(int col=0;col<5;col++)
                        if(Vector3.Distance(game.Player.transform.position,new Vector3(100+col*140,0,-264+line.row*140))<6)
                            Prompt=(line.row+101)+"번 버스 정류장 · 운행 중 · 요금 "+Fare+" C";
            }
        }
        public bool Board(CityBusLine line)
        {
            if(!line||!line.Stopped||Riding||line.Car.Wrecked)return false;
            if(!LifeState.Spend(Fare)){game.Toast("버스비가 부족합니다. 요금 "+Fare+" C");return false;}
            Riding=line.Car;requestedStop=false;PaidTrips++;
            game.Player.Rope.Release();game.Player.Controller.enabled=false;
            hero=game.Player.GetComponentsInChildren<Renderer>();heroVisible=new bool[hero.Length];
            for(int i=0;i<hero.Length;i++){heroVisible[i]=hero[i].enabled;hero[i].enabled=false;}
            game.Player.transform.position=Riding.transform.position+Vector3.up*1.2f;game.CameraRig.Snap();
            return true;
        }
        public void JumpOff()
        {
            if(!Riding)return;var bus=Riding;var at=bus.transform.position+bus.transform.forward*(bus.HalfWidth+2)+Vector3.up;
            var velocity=bus.Forward*bus.speed+bus.transform.forward*3+Vector3.up*3;
            Riding=null;requestedStop=false;game.Player.Respawn(at,false);game.Player.Velocity=velocity;
            if(hero!=null)for(int i=0;i<hero.Length;i++)if(hero[i])hero[i].enabled=heroVisible[i];game.Toast("이동 중 버스에서 뛰어내렸습니다");
        }
        public void EmergencyLeave(CityVehicle car)
        {
            if(Riding!=car)return;
            Leave();
            var at=car.transform.position+car.transform.forward*(car.HalfWidth+2.5f);at.y=.15f;
            game.Player.Respawn(at,false);game.CameraRig.Snap();
            game.Toast("버스 비상 탈출 · 차량에서 멀리 피하세요");
        }
        public void Leave()
        {
            if(!Riding)return;
            var line=Riding.GetComponent<CityBusLine>();
            Vector3 at=line?line.BoardPoint:CityRoadNetwork.Sidewalk(Riding.transform.position);
            Riding=null;requestedStop=false;
            game.Player.Respawn(at,false);
            for(int i=0;i<hero.Length;i++)if(hero[i])hero[i].enabled=heroVisible[i];
            game.CameraRig.Snap();
        }
        void Update(){if(Riding&&Riding.Wrecked)Leave();}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
    public sealed class CityBusLine:MonoBehaviour
    {
        public int row;
        public CityVehicle Car=>GetComponent<CityVehicle>();
        public bool Stopped{get;private set;}
        public Vector3 BoardPoint=>new Vector3(Car.transform.position.x,.1f,-264+row*140);
        public string NextStopName=>"애프터라이트 "+(row+1)+"가 "+(5-Mathf.Min(Car.waypoint,4))+"정류장";
        public int StopCount{get;private set;}
        float hold;readonly List<GameObject> boardingExtras=new List<GameObject>();
        public void CancelStop(){Stopped=false;hold=0;StopAllCoroutines();foreach(var item in boardingExtras)if(item)Destroy(item);boardingExtras.Clear();}
        public bool Prepare(float dt)
        {
            if(Stopped)
            {
                Car.speed=0;hold-=dt;
                if(hold<=0){Stopped=false;Car.waypoint=(Car.waypoint+1)%Car.route.Length;}
                return true;
            }
            if(Car.waypoint<5&&Vector3.Distance(Car.transform.position,Car.route[Car.waypoint])<.25f)
            {
                Car.speed=0;Stopped=true;hold=12;StopCount++;
                StartCoroutine(ExchangePassengers());
                return true;
            }
            return false;
        }
        IEnumerator ExchangePassengers()
        {
            var cabin=Car.GetComponent<VehicleCabin>();
            int departing=Random.Range(1,4),arriving=Random.Range(2,5);
            if(cabin)cabin.SetPassengers(cabin.PassengerCount-departing);
            for(int i=0;i<departing;i++)
            {
                CityPopulation.Instance?.Eject(BoardPoint+Vector3.right*(i*.8f));
                yield return new WaitForSeconds(.55f);
            }
            int passengerSeed=Random.Range(0,PeopleArt.Citizens.Length);
            for(int i=0;i<arriving;i++)
            {
                var g=new GameObject("Boarding passenger",typeof(SpriteRenderer));
                boardingExtras.Add(g);var r=g.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get(PeopleArt.Citizens[(passengerSeed+i)%PeopleArt.Citizens.Length],2,1);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");
                g.transform.position=BoardPoint+Vector3.right*(2+i);g.transform.localScale=Vector3.one*.72f;
                float elapsed=0;var start=g.transform.position;var end=Car.transform.position+Vector3.forward*1.3f+Vector3.up*.9f;
                while(elapsed<.8f){elapsed+=Time.deltaTime;g.transform.position=Vector3.Lerp(start,end,elapsed/.8f);if(Camera.main)g.transform.rotation=Camera.main.transform.rotation;yield return null;}
                boardingExtras.Remove(g);Destroy(g);if(cabin)cabin.SetPassengers(cabin.PassengerCount+1);
            }
        }
    }
}
