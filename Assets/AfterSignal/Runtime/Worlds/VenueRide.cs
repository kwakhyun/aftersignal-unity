using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class VenueRide:MonoBehaviour
    {
        public static VenueRide Riding{get;private set;}
        public VenueRuntime Venue{get;private set;}
        public int Kind{get;private set;}
        public int Trips{get;private set;}
        public string Status=>phase<10?"탑승 대기":phase<68?"운행 중":"하차 안내";
        readonly List<Transform> cabins=new();readonly List<VenueActor> guests=new();Transform wheel;
        readonly List<Collider> cabinCollision=new();
        float phase,angle;Vector3 lastSeat;bool boarded;int playerSeat;
        public void Initialize(VenueRuntime venue,int kind)
        {
            Venue=venue;Kind=kind;transform.localPosition=kind==0?new(-115,0,12):kind==1?new(80,0,5):new(-15,0,-78);
            var g=new CityGeometry(transform);
            if(kind==0)
            {
                for(int s=-1;s<=1;s+=2){g.Beam(new(s*15,0,-5),new(0,28,0),1.6f,"FutureSilver",true);g.Beam(new(s*15,0,5),new(0,28,0),1.6f,"FutureSilver",true);}
                wheel=new GameObject("Rotating wheel structure").transform;wheel.SetParent(transform,false);wheel.localPosition=Vector3.up*28;wheel.localRotation=Quaternion.Euler(90,0,0);var wg=new CityGeometry(wheel);wg.Ring(Vector3.zero,25,25,.65f,"NeonCyan");wg.Ring(Vector3.zero,23.7f,23.7f,.5f,"FutureSilver");for(int n=0;n<16;n++){float a=n*Mathf.PI/8;wg.Beam(Vector3.zero,new Vector3(Mathf.Cos(a)*25,0,Mathf.Sin(a)*25),.3f,"FutureSilver");}wg.Finish();
                for(int n=0;n<12;n++)Cabin(n,false);
            }
            else if(kind==1)
            {
                for(int n=0;n<160;n++){var a=Coaster(n/160f);var b=Coaster((n+1)/160f);var side=Vector3.Cross((b-a).normalized,Vector3.up);g.Beam(a+side*.85f,b+side*.85f,.14f,"NeonRose");g.Beam(a-side*.85f,b-side*.85f,.14f,"NeonRose");g.Beam(a+side,a-side,.17f,"FutureSilver");if(n%5==0)g.Beam(new(a.x,0,a.z),a-Vector3.up*.5f,.65f,"FutureSilver",true);}
                for(int n=0;n<4;n++)Cabin(n,true);
            }
            else
            {
                g.Cylinder(Vector3.zero,13,.3f,"FutureCeramic",48);g.Cylinder(Vector3.up*.3f,1.2f,7,"FutureCopper",20);g.Cylinder(Vector3.up*6.8f,14,.7f,"SeatCoral",48,2);g.Ring(Vector3.up*6.8f,14,14,.4f,"NeonWarm");
                for(int n=0;n<10;n++)Cabin(n,true);
            }
            g.Box("Ride boarding platform",new(0,.1f,-16),new(15,.2f,9),"TerminalFloor",true);
            for(int n=0;n<4;n++)g.Box("Queue railing",new(-6+n*4,.7f,-25),new(.1f,1.4f,12),"FutureSilver",true);
            g.Sign(kind==0?"TIDE WHEEL":kind==1?"ORBIT COASTER":"SIGNAL CAROUSEL",new(0,4,-19),.32f);
            FourCityArchitecture.Counter(g,new(10,0,-17));g.Finish();
            var op=VenueActor.Create(venue,61000+kind,"놀이기구 운전원","Worker",venue.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(10,.08f,-15))));op.staff=true;op.serial=kind;
            phase=kind*3;
        }
        void Cabin(int n,bool open)
        {
            var root=new GameObject("Passenger cabin "+n).transform;root.SetParent(transform,false);var g=new CityGeometry(root);
            g.Box("Cabin floor",new(0,-.1f,0),new(2.7f,.2f,2),"FutureCarbon",true);g.Box("Passenger bench",new(0,.4f,.4f),new(2.3f,.4f,.65f),n%2==0?"SeatBlue":"SeatCoral");
            for(int s=-1;s<=1;s+=2){g.Box("Cabin side",new(s*1.3f,.5f,0),new(.13f,1,2),"FutureSilver");if(!open)g.Box("Cabin glazing",new(s*1.3f,1.45f,0),new(.05f,1,2),"Glass");}
            g.Beam(new(-1.2f,.85f,-.65f),new(1.2f,.85f,-.65f),.1f,"Steel");if(!open)g.Box("Cabin roof",new(0,2.05f,0),new(2.8f,.18f,2.1f),"FutureCeramic");g.Finish();cabins.Add(root);cabinCollision.AddRange(root.GetComponentsInChildren<Collider>());
        }
        static Vector3 Coaster(float phase)
        {float a=phase*2*Mathf.PI;return new Vector3(Mathf.Sin(a)*72,2+Mathf.Pow(Mathf.Sin(a*.5f),2)*26+Mathf.Pow(Mathf.Sin(a*2),2)*7,Mathf.Cos(a)*53-53);}
        public void Board()
        {
            var game=GameDirector.Instance;if(Riding||!game||!game.Ready)return;
            if(!LifeState.Spend(Kind==0?30:Kind==1?40:15)){game.Toast("보유 크레딧이 부족합니다.");return;}
            Riding=this;boarded=false;game.Player.Respawn(transform.TransformPoint(new Vector3(2,.15f,-18)),false);game.Toast("승강장에서 기다립니다 · 운전원이 탑승을 안내합니다. F 취소",6);
        }
        void BoardPassengers()
        {
            for(int n=0;n<Mathf.Min(cabins.Count,6);n++)
            {
                if(n>=guests.Count){var npc=VenueActor.Create(Venue,63000+Kind*30+n,"놀이기구 탑승객",PeopleArt.Citizens[(n+Kind*3)%PeopleArt.Citizens.Length],Vector3.zero);npc.enabled=false;guests.Add(npc);}
                var guest=guests[n];guest.transform.SetParent(cabins[n],false);guest.transform.localPosition=new Vector3(-.55f,.18f,.1f);guest.GetComponent<DirectionalPerson>().Sitting=true;guest.gameObject.SetActive(true);
            }
            if(Riding==this&&!boarded){playerSeat=0;boarded=true;var g=GameDirector.Instance;g.Player.Respawn(cabins[0].TransformPoint(new Vector3(.55f,.15f,.1f)),false);lastSeat=cabins[0].TransformPoint(new Vector3(.55f,.15f,.1f));g.Toast("탑승 완료 · F 하차",4);}
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||game.Paused)return;
            phase+=Time.deltaTime;
            if(phase>=10&&phase-Time.deltaTime<10)BoardPassengers();
            if(phase>74){phase=0;Trips++;if(Riding==this)Exit();foreach(var guest in guests)if(guest){guest.transform.SetParent(Venue.transform);guest.transform.position=transform.TransformPoint(new Vector3(4+guest.GetInstanceID()%3,.12f,-19));guest.GetComponent<DirectionalPerson>().Sitting=false;guest.gameObject.SetActive(false);}}
            float u=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-10)/58));angle=u*360;
            if(Riding==this&&boarded)foreach(var collider in cabinCollision)collider.enabled=false;
            if(wheel)wheel.localRotation=Quaternion.Euler(90,0,angle);
            for(int i=0;i<cabins.Count;i++)
            {
                if(Kind==0){float a=(angle+i*360f/cabins.Count-90)*Mathf.Deg2Rad;cabins[i].localPosition=new(Mathf.Cos(a)*25,28+Mathf.Sin(a)*25,0);cabins[i].rotation=Quaternion.identity;}
                else if(Kind==1){float t=u-i*.006f;var p=Coaster(t);cabins[i].localPosition=p;cabins[i].localRotation=Quaternion.LookRotation(Coaster(t+.001f)-p);}
                else{float a=(angle*3+i*36)*Mathf.Deg2Rad;cabins[i].localPosition=new(Mathf.Cos(a)*8,1+Mathf.Sin(a*2)*.4f,Mathf.Sin(a)*8);cabins[i].localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            }
            if(Riding==this&&boarded){var seat=cabins[playerSeat].TransformPoint(new Vector3(.55f,.15f,.1f));game.Player.Carry(seat-lastSeat);lastSeat=seat;}
            foreach(var collider in cabinCollision)collider.enabled=true;
        }
        public static void BeforeInput(ref ControlFrame input)
        {if(!Riding)return;if(input.exit){Riding.Exit();input.exit=false;}input.move=Vector2.zero;input.jump=input.dash=input.attack=input.interact=input.passenger=false;}
        public void Exit(){if(Riding!=this)return;Riding=null;boarded=false;var game=GameDirector.Instance;if(game&&game.Player){game.Player.Respawn(transform.TransformPoint(new Vector3(5,.15f,-20)),false);game.Toast("놀이기구에서 내렸습니다.");}}
        void OnDestroy(){if(Riding==this)Riding=null;}
    }
}
