using System;
using UnityEngine;
namespace AfterSignal
{
    public sealed class EmergencyAmbulance:MonoBehaviour
    {
        public CityVehicle Car{get;private set;}
        public WorldActor Patient{get;private set;}
        public int Phase{get;private set;}
        Action finished;Vector3 destination;float hold,life;Renderer lamp;bool loaded;GameObject medic;
        readonly PursuitPath walk=new PursuitPath();
        public static EmergencyAmbulance Create(WorldActor patient,Action done)
        {
            Vector3 hospital=UrbanCatalog.Door(4)+Vector3.back*10;
            var car=UrbanSimulation.Instance.Spawn(hospital,false,3);car.name="AMBULANCE / emergency transport";
            var a=car.gameObject.AddComponent<EmergencyAmbulance>();a.Car=car;a.Patient=patient;a.finished=done;car.occupied=true;
            foreach(var r in car.GetComponentsInChildren<MeshRenderer>())if(r.name.Contains("Cargo")||r.name.Contains("chassis"))r.sharedMaterial=Resources.Load<Material>("Materials/Enamel");
            for(int side=-1;side<=1;side+=2){WorldGeometry.Part(car.transform,"Medical cross",new Vector3(-.5f,1.9f,side*1.26f),new Vector3(.3f,1.2f,.07f),"RedFX");WorldGeometry.Part(car.transform,"Medical cross",new Vector3(-.5f,1.9f,side*1.26f),new Vector3(1.2f,.3f,.07f),"RedFX");}
            a.lamp=WorldGeometry.Part(car.transform,"Emergency beacon",new Vector3(2,2.5f,0),new Vector3(1,.2f,1),"RedFX").GetComponent<Renderer>();
            var road=CityRoadNetwork.NearestJunction(patient.transform.position);
            a.Route(new Vector3(patient.transform.position.x,.02f,road.z+5));return a;
        }
        void Route(Vector3 target)
        {
            destination=target;destination.y=.02f;
            var path=CityRoadNetwork.Navigation(transform.position,destination);Car.route=path.ToArray();Car.waypoint=1;Car.traffic=true;
        }
        void MedicalTeam()
        {
            medic=new GameObject("Paramedic attending injured citizen",typeof(SpriteRenderer));
            medic.transform.position=transform.position+transform.forward*(Car.HalfWidth+1)+Vector3.up*.08f;
            var r=medic.GetComponent<SpriteRenderer>();r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");r.sprite=PeopleArt.Get("Nurse",0);
            PeopleArt.Attach(medic,"Nurse");NpcSpeech.Say(medic.transform,"구급대입니다. 상태를 확인할게요.",4);
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||game.Blocked)return;
            life+=Time.deltaTime;
            if(lamp)lamp.enabled=Mathf.Repeat(Time.time,.6f)<.3f;
            if(Car.Wrecked||!Patient){Finish();return;}
            if(Phase==0&&(Vector3.Distance(transform.position,destination)<5||life>75))
            {Car.traffic=false;Car.speed=0;Phase=1;MedicalTeam();}
            if(Phase==1||Phase==2)
            {
                var goal=Phase==1?Patient.transform.position:transform.position+transform.forward*(Car.HalfWidth+1);
                Vector3 d=goal-medic.transform.position;d.y=0;
                if(d.magnitude>1.6f)medic.transform.position+=walk.Direction(medic.transform.position,goal)*Time.deltaTime*2.6f;
                else if(Phase==1)
                {
                    Phase=2;loaded=true;Patient.GetComponent<MedicalPending>().carried=true;
                    foreach(var c in Patient.GetComponents<Collider>())c.enabled=false;
                    NpcSpeech.Say(medic.transform,"병원으로 이송하겠습니다.",3);
                }
                else
                {
                    Phase=3;Destroy(medic);
                    foreach(var r in Patient.GetComponentsInChildren<Renderer>())r.enabled=false;
                    Route(UrbanCatalog.Door(4)+Vector3.back*10);
                }
                if(Phase==2&&medic)
                {
                    Patient.transform.position=medic.transform.position+Vector3.right*.9f+Vector3.up*.5f;
                    var art=Patient.GetComponent<DirectionalPerson>();if(art)art.Lying=true;
                }
            }
            if(Phase==3)
            {
                Patient.transform.position=transform.position;
                if(Vector3.Distance(transform.position,destination)<5)
                {
                    Phase=4;Car.traffic=false;Car.speed=0;
                    Patient.ResetHealth();var walker=Patient.GetComponent<CityPedestrian>();
                    if(walker)walker.ResetAt(UrbanCatalog.Door(4)+Vector3.right*4,UrbanCatalog.Door(4)+Vector3.right*10,walker.poses);
                    else Patient.transform.position=UrbanCatalog.Door(4)+Vector3.right*4;
                    var art=Patient.GetComponent<DirectionalPerson>();if(art)art.Lying=false;
                    foreach(var r in Patient.GetComponentsInChildren<SpriteRenderer>())r.enabled=true;
                    foreach(var c in Patient.GetComponents<Collider>())c.enabled=true;
                    Destroy(Patient.GetComponent<MedicalPending>());loaded=false;if(CitySafety.Instance)CitySafety.Instance.Transports++;hold=4;
                    NpcSpeech.Say(Patient,"치료받고 나니 한결 나아요.",4);
                }
            }
            if(Phase==4){hold-=Time.deltaTime;if(hold<=0)Finish();}
        }
        void Finish()
        {
            if(loaded&&Patient){Patient.transform.position=CityRoadNetwork.Sidewalk(transform.position);foreach(var r in Patient.GetComponentsInChildren<SpriteRenderer>())r.enabled=true;foreach(var c in Patient.GetComponents<Collider>())c.enabled=true;Destroy(Patient.GetComponent<MedicalPending>());}
            if(medic)Destroy(medic);finished?.Invoke();finished=null;Destroy(gameObject);
        }
    }
}
