using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class UrbanSimulation:MonoBehaviour
    {
        public static UrbanSimulation Instance {get;private set;}
        public CityVehicle vehiclePrefab;public CityVehicle[] vehiclePrefabs;public CityVehicle Current {get;private set;}public CityVehicle Owned {get;private set;}
        public readonly List<CityVehicle> Cars=new List<CityVehicle>();
        public bool Driving=>Current;public bool Refueling {get;private set;}public int Hijacks {get;private set;}public int Entries {get;private set;}
        public string Prompt {get;private set;}public bool MapOpen {get;private set;}
        GameDirector game;Renderer[] playerRenderers;bool[] rendererStates;ContactShadow contact;float spawnClock,saveClock;CityVehicle nearest;
        readonly System.Random random=new System.Random();
        void Awake(){Instance=this;}
        System.Collections.IEnumerator Start(){game=GameDirector.Instance;while(!game.Ready)yield return null;Cars.AddRange(FindObjectsByType<CityVehicle>());foreach(var c in Cars)c.occupied=false;
            if(PlayerPrefs.GetInt(UrbanCatalog.Prefix+"Car",0)>0&&PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarStage",23)==(int)game.stage){Owned=Spawn(new Vector3(PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarX"),.02f,PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarZ")),false,PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarType",0));Owned.transform.rotation=Quaternion.Euler(0,PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarYaw"),0);Owned.fuel=PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarFuel",45);Owned.health=PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarHealth",100);Owned.owned=true;}
            if(game.stage==StageId.UrbanCity){var parking=new Vector3(73,.02f,-254);if(!Owned||Vector3.Distance(Owned.transform.position,parking)>8)Spawn(parking,false);for(int i=0;i<12;i++)SpawnTraffic();}
        }
        public CityVehicle Spawn(Vector3 position,bool ai,int variant=-1){var prefab=variant<0?vehiclePrefab:vehiclePrefabs!=null&&vehiclePrefabs.Length>variant?vehiclePrefabs[variant]:vehiclePrefab;var c=Instantiate(prefab,position,Quaternion.identity);c.gameObject.SetActive(true);c.occupied=c.traffic=ai;c.owned=false;Cars.Add(c);var paints=new[]{"SedanIvory","SedanRed","DistrictBlue","DistrictWarm"};string paint=paints[random.Next(paints.Length)];foreach(var mesh in c.GetComponentsInChildren<MeshRenderer>())if(c.type==CityVehicleType.Sedan&&(mesh.name=="Sculpted chassis"||mesh.name=="Sedan roof"))mesh.sharedMaterial=Resources.Load<Material>("Materials/"+paint);return c;}
        void SpawnTraffic(){if(!game||!vehiclePrefab)return;var p=game.Player.transform.position;int col=Mathf.Clamp(Mathf.RoundToInt((p.x-40)/140)+(random.Next(3)-1),0,4),row=Mathf.Clamp(Mathf.RoundToInt((p.z+280)/140)+(random.Next(3)-1),0,3);float x=40+col*140,z=-280+row*140;
            var route=CityRoadNetwork.TrafficLoop(col,row);int start=random.Next(4)*7+6;int next=(start+1)%route.Length;var position=Vector3.Lerp(route[start],route[next],.25f+(float)random.NextDouble()*.5f);
            foreach(var existing in Cars)if(existing&&Vector3.Distance(existing.transform.position,position)<18)return;
            var c=Spawn(position,true,random.Next(4));c.route=route;c.waypoint=next;c.speed=4;c.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(-(route[next]-position).z,(route[next]-position).x)*Mathf.Rad2Deg,0);}
        public void BeforeInput(ref ControlFrame input,float dt)
        {
            if(input.map)MapOpen=!MapOpen;
            nearest=null;Prompt="";
            if(Current){
                bool pump=NearPump(Current.transform.position);Prompt=Refueling?"주유 중 · E 중단 / 출발하면 자동 중단":pump&&Mathf.Abs(Current.speed)<.7f?"E 주유 시작   ·   F 하차":"E 하차   ·   WASD 운전 / SPACE 제동";
                if(input.interact){input.interact=false;if(pump&&Mathf.Abs(Current.speed)<.7f)Refueling=!Refueling;else Exit();}
                if(input.exit){Exit();}
                if(Current&&Refueling){if(Mathf.Abs(input.move.y)>.1f||Mathf.Abs(Current.speed)>.8f)Refueling=false;else{Current.fuel=Mathf.Min(CityVehicle.Capacity,Current.fuel+10*dt);if(Current.fuel>=CityVehicle.Capacity){Refueling=false;game.Toast("주유 완료 · 안전 운전하세요");} }}
                input.attack=input.grapple=input.dash=input.skill=input.reload=false;
            }else{
                float best=4.3f;foreach(var c in Cars)if(c&&!c.Wrecked){float d=Vector3.Distance(c.transform.position,game.Player.transform.position);if(d<best){best=d;nearest=c;}}
                if(nearest){Prompt=nearest.occupied?"E 운전자 내리게 하고 탑승":"E 차량 탑승";if(input.interact){input.interact=false;Enter(nearest);}}
            }
            if(MapOpen){input.move=Vector2.zero;input.guard=true;input.attack=input.grapple=input.jump=input.dash=input.skill=false;}
            spawnClock-=dt;saveClock-=dt;
            if(game.stage==StageId.UrbanCity&&spawnClock<=0){spawnClock=2;int count=0;for(int i=Cars.Count-1;i>=0;i--){var c=Cars[i];if(!c){Cars.RemoveAt(i);continue;}if(c!=Owned&&Vector3.Distance(c.transform.position,game.Player.transform.position)>290){Destroy(c.gameObject);Cars.RemoveAt(i);}else if(c.traffic)count++;}if(count<14)SpawnTraffic();int parked=0;foreach(var car in Cars)if(car&&!car.traffic&&!car.owned)parked++;if(parked<12){int id=random.Next(UrbanCatalog.SiteCount);var p=UrbanCatalog.Center(id)+new Vector3(-12,.02f,-44);if(Vector3.Distance(p,game.Player.transform.position)<210&&Vector3.Distance(p,game.Player.transform.position)>45)Spawn(p,false,random.Next(4));}}
            foreach(var c in Cars)if(c&&c!=Current)c.TickTraffic(dt);
            if(saveClock<=0){saveClock=8;SaveCar();}
        }
        public void Tick(ControlFrame input,float dt){if(!Current)return;Current.Drive(input,dt);game.Player.transform.position=Current.transform.position+Vector3.up*.15f;}
        public bool Enter(CityVehicle car)
        {
            if(!car||Current||car.Wrecked)return false;
            if(Mathf.Abs(car.speed)>12){game.Toast("차가 너무 빠릅니다 · 속도가 줄었을 때 접근하세요");return false;}
            if(car.occupied){Hijacks++;CityPopulation.Instance?.Eject(car.transform.position-car.transform.forward*2);game.Toast("운전자가 하차했습니다");}
            if(Owned)Owned.owned=false;Owned=Current=car;car.owned=car.occupied=true;car.traffic=false;car.speed=0;Entries++;
            game.Player.Rope.Release();game.Player.Respawn(game.Player.transform.position,false);game.Player.Controller.enabled=false;
            playerRenderers=game.Player.GetComponentsInChildren<Renderer>();rendererStates=new bool[playerRenderers.Length];for(int i=0;i<playerRenderers.Length;i++){rendererStates[i]=playerRenderers[i].enabled;playerRenderers[i].enabled=false;}
            contact=game.Player.GetComponent<ContactShadow>();if(contact)contact.enabled=false;game.Audio.Play("urban_door",car.transform.position,.25f,1);SaveCar();return true;
        }
        public bool Exit()
        {
            if(!Current)return false;if(Mathf.Abs(Current.speed)>2){game.Toast("먼저 SPACE로 정차하세요");return false;}
            Vector3 point=Vector3.zero;bool found=false;
            foreach(var offset in new[]{Current.transform.forward*(Current.HalfWidth+1.5f),-Current.transform.forward*(Current.HalfWidth+1.5f),-Current.Forward*(Current.HalfLength+1.5f)}){var p=Current.transform.position+offset; if(Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.7f,.34f,1,QueryTriggerInteraction.Ignore))continue;point=p+Vector3.up*.15f;found=true;break;}
            if(!found){game.Toast("문 옆 공간이 좁습니다 · 넓은 곳에 정차하세요");return false;}
            Current.speed=0;Current.occupied=false;game.Audio.Play("urban_door",point,.25f,1);Current=null;Refueling=false;game.Player.Respawn(point,false);
            for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=rendererStates[i];if(contact)contact.enabled=true;SaveCar();return true;
        }
        public bool NearPump(Vector3 p){if(game.stage!=StageId.UrbanCity)return false;for(int i=0;i<UrbanCatalog.SiteCount;i++)if(UrbanCatalog.Kind(i)==11&&Vector3.Distance(p,UrbanCatalog.Pump(i))<8)return true;return false;}
        public void SaveCar(){if(!Owned||!game)return;string p=UrbanCatalog.Prefix;PlayerPrefs.SetInt(p+"Car",1);PlayerPrefs.SetInt(p+"CarStage",(int)game.stage);PlayerPrefs.SetFloat(p+"CarX",Owned.transform.position.x);PlayerPrefs.SetFloat(p+"CarZ",Owned.transform.position.z);PlayerPrefs.SetFloat(p+"CarYaw",Owned.transform.eulerAngles.y);PlayerPrefs.SetFloat(p+"CarFuel",Owned.fuel);PlayerPrefs.SetFloat(p+"CarHealth",Owned.health);PlayerPrefs.SetInt(p+"CarType",(int)Owned.type);PlayerPrefs.Save();}
        public void EmergencyExit(CityVehicle car){if(Current!=car)return;if(!Exit()){var point=CityRoadNetwork.Sidewalk(car.transform.position);Current.occupied=false;Current=null;Refueling=false;game.Player.Respawn(point,false);for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=rendererStates[i];if(contact)contact.enabled=true;}SaveCar();}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
