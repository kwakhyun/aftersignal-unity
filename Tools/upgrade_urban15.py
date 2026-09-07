"""Historical one-time 1.4 to 1.5 source migration; already applied.

Do not rerun on 1.5. Rebuild current scenes with ProjectBuilder.CityOnlyAndRelease
or the AFTERSIGNAL Urban editor menu instead.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]/'Assets/AfterSignal'
def change(file, pairs):
 p=ROOT/file;s=p.read_text(encoding='utf-8')
 for a,b in pairs:
  assert a in s,(file,a[:100]);s=s.replace(a,b)
 p.write_text(s,encoding='utf-8')
change('Runtime/CityVehicle.cs',[('type>=CityVehicleType.Bus','(int)type>=2')])
change('Runtime/UrbanSimulation.cs',[
 ('public CityVehicle vehiclePrefab;','public CityVehicle vehiclePrefab;public CityVehicle[] vehiclePrefabs;'),
 ('PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarZ")),false)','PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarZ")),false,PlayerPrefs.GetInt(UrbanCatalog.Prefix+"CarType",0))'),
 ('Owned.owned=true;','Owned.health=PlayerPrefs.GetFloat(UrbanCatalog.Prefix+"CarHealth",100);Owned.owned=true;'),
 ('if(c){float d=','if(c&&!c.Wrecked){float d='),
 ('if(!car||Current)return false;','if(!car||Current||car.Wrecked)return false;'),
 ('Current.transform.forward*2.5f,-Current.transform.forward*2.5f,-Current.Forward*3.5f','Current.transform.forward*(Current.HalfWidth+1.5f),-Current.transform.forward*(Current.HalfWidth+1.5f),-Current.Forward*(Current.HalfLength+1.5f)'),
 ('PlayerPrefs.SetFloat(p+"CarFuel",Owned.fuel);','PlayerPrefs.SetFloat(p+"CarFuel",Owned.fuel);PlayerPrefs.SetFloat(p+"CarHealth",Owned.health);PlayerPrefs.SetInt(p+"CarType",(int)Owned.type);'),
 ('public CityVehicle Spawn(Vector3 position,bool ai){','public CityVehicle Spawn(Vector3 position,bool ai,int variant=-1){'),
 ('var c=Instantiate(vehiclePrefab,position,Quaternion.identity);','var prefab=variant<0?vehiclePrefab:vehiclePrefabs!=null&&vehiclePrefabs.Length>variant?vehiclePrefabs[variant]:vehiclePrefab;var c=Instantiate(prefab,position,Quaternion.identity);'),
 ('foreach(var mesh in c.GetComponentsInChildren<MeshRenderer>())','string paint=paints[random.Next(paints.Length)];foreach(var mesh in c.GetComponentsInChildren<MeshRenderer>())'),
 ('if(mesh.name=="Sculpted chassis"||mesh.name=="Sedan roof")','if(c.type==CityVehicleType.Sedan&&(mesh.name=="Sculpted chassis"||mesh.name=="Sedan roof"))'),
 ('paints[random.Next(paints.Length)]);return c;','paint);return c;'),
 ('Spawn(p,false);','Spawn(p,false,random.Next(4));'),
 ('void OnDestroy(){','public void EmergencyExit(CityVehicle car){if(Current!=car)return;if(!Exit()){var point=CityRoadNetwork.Sidewalk(car.transform.position);Current.occupied=false;Current=null;Refueling=false;game.Player.Respawn(point,false);for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i])playerRenderers[i].enabled=rendererStates[i];if(contact)contact.enabled=true;}SaveCar();}\n        void OnDestroy(){')
])
p=ROOT/'Runtime/UrbanSimulation.cs';s=p.read_text(encoding='utf-8');a=s.index('            var route=new[]{');b=s.index('        public void BeforeInput',a)
s=s[:a]+'''            var route=CityRoadNetwork.TrafficLoop(col,row);int start=random.Next(4)*7+6;int next=(start+1)%route.Length;var position=Vector3.Lerp(route[start],route[next],.25f+(float)random.NextDouble()*.5f);
            foreach(var existing in Cars)if(existing&&Vector3.Distance(existing.transform.position,position)<18)return;
            var c=Spawn(position,true,random.Next(4));c.route=route;c.waypoint=next;c.speed=4;c.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(-(route[next]-position).z,(route[next]-position).x)*Mathf.Rad2Deg,0);}
'''+s[b:];p.write_text(s,encoding='utf-8')
for file in ['Runtime/UrbanCatalog.cs','Runtime/UrbanSmoke.cs','Runtime/UrbanManualProbe.cs','Tests/PlayMode/GameplayTests.cs']:
 p=ROOT/file;s=p.read_text(encoding='utf-8');s=s.replace('"CarFuel","CarStage"','"CarFuel","CarStage","CarHealth","CarType"')
 s=s.replace('suffix=="CarStage"?','suffix=="CarStage"||suffix=="CarType"?')
 s=s.replace('EndsWith("Fuel")','EndsWith("Fuel")||kv.Key.EndsWith("Health")') if 'kv.Key.EndsWith("Fuel")' in s else s
 p.write_text(s,encoding='utf-8')
change('Runtime/CityPedestrian.cs',[
 ('SpriteRenderer visual;float phase,stun;','SpriteRenderer visual;float phase,stun;public bool crossing,enteredCrossing,waiting;\n        public void WalkTo(Vector3 p,bool cross){target=p;crossing=cross;enteredCrossing=waiting=false;}'),
 ('struck=dead=false;','struck=dead=crossing=enteredCrossing=waiting=false;'),
 ('else{var delta=target-transform.position;','else{var delta=target-transform.position;waiting=crossing&&!enteredCrossing&&!CityRoadNetwork.CanStartCrossing(delta.magnitude/speed);if(waiting){if(poses!=null&&poses.Length>0)visual.sprite=poses[0];return;}if(crossing)enteredCrossing=true;'),
 ('if(!dead&&age>stun)struck=false;','if(!dead&&age>stun){struck=false;if(GameDirector.Instance.stage==StageId.UrbanCity){transform.position=CityRoadNetwork.Sidewalk(transform.position);target=transform.position;crossing=enteredCrossing=false;}}')
])
p=ROOT/'Runtime/CityPopulation.cs';s=p.read_text(encoding='utf-8')
s=s.replace('if(Physics.CheckCapsule(p+', 'if(game.stage==StageId.UrbanCity)p=CityRoadNetwork.Sidewalk(p);\n            if(Physics.CheckCapsule(p+')
s=s.replace('c.ResetAt(p,Destination(p),sprites[Random.Range(0,4)]);c.speed=Random.Range(.9f,1.65f);','c.ResetAt(p,p,sprites[Random.Range(0,4)]);c.speed=Random.Range(1.8f,2f);SetDestination(c);')
a=s.index('        Vector3 Destination(');b=s.index('        void Update()',a)
s=s[:a]+'''        void SetDestination(CityPedestrian c){if(game.stage==StageId.Haven){c.WalkTo(new Vector3(Mathf.Clamp(c.transform.position.x+Random.Range(-24,24),5,222),.06f,c.transform.position.z),false);return;}bool crossing;var target=CityRoadNetwork.NextWalk(c.transform.position,Random.Range(0,4),out crossing);c.WalkTo(target,crossing);}
'''+s[b:]
s=s.replace('c.target=Destination(c.transform.position);','SetDestination(c);')
s=s.replace('Mathf.Abs(local.x)<2.9f&&Mathf.Abs(local.z)<1.35f','Mathf.Abs(local.x)<car.HalfLength+.25f&&Mathf.Abs(local.z)<car.HalfWidth+.3f')
s=s.replace('forward>3&&forward<10','forward>car.HalfLength&&forward<car.HalfLength+14')
s=s.replace('if(c)c.ResetAt(p,new Vector3(p.x+8,.06f,p.z+8),sprites[2]);','if(c){var safe=game.stage==StageId.UrbanCity?CityRoadNetwork.Sidewalk(p):p+Vector3.forward*3;c.ResetAt(p,safe,sprites[2]);}')
p.write_text(s,encoding='utf-8')
change('Editor/ProjectBuilder.cs',[('"1.4.0"','"1.5.0"'),('DistrictMaterials();InteriorMaterials();CityMaterials();for','DistrictMaterials();InteriorMaterials();CityMaterials();UrbanMaterials();for'),('for(int i=0;i<buildScenes.Length;i++){var scene=','for(int i=0;i<23;i++){var scene='),('if(i>=3)PolishDistrict((StageId)i);','if(i>=3)PolishDistrict((StageId)i);if(i==3)InstallUrbanHaven();ReplaceSignImages();'),('23 URP scenes','25 URP scenes')])
change('Editor/UrbanBuilder.cs',[
 ('if(title.Contains("주유"))','if(title.Contains("←")||title.Contains("→")||title.Contains(" E")||title.Contains("E /"))return -1;\n            if(title.Contains("주유"))'),
 ('simulation.vehiclePrefab=MakeVehiclePrefab();','simulation.vehiclePrefab=MakeVehiclePrefab();simulation.vehiclePrefabs=MakeVehicleVariants(simulation.vehiclePrefab);'),
 ('sim.vehiclePrefab=MakeVehiclePrefab();','sim.vehiclePrefab=MakeVehiclePrefab();sim.vehiclePrefabs=MakeVehicleVariants(sim.vehiclePrefab);'),
 ('for(int id=0;id<UrbanCatalog.SiteCount;id++)BuildUrbanSite(id);UrbanStreetLife();','for(int id=0;id<UrbanCatalog.SiteCount;id++)BuildUrbanSite(id);UrbanStreetLife();BuildDenseSkyline();'),
 ('UrbanSurfaceMaterials();','UrbanSurfaceMaterials();RoadMarkMaterials();')
])
print('Urban15 integration changes applied')
