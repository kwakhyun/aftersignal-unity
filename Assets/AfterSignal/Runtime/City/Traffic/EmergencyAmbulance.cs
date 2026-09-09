using System;
using UnityEngine;
namespace AfterSignal
{
    public sealed class EmergencyAmbulance:MonoBehaviour
    {
        public CityVehicle Car{get;private set;}
        public WorldActor Patient{get;private set;}
        public int Phase{get;private set;}
        public Transform Stretcher{get;private set;}
        Action finished;Vector3 hospital;float hold,life;bool loaded;Transform[] medics;MedicalState injury;
        readonly ResponseDrive drive=new();readonly PursuitPath walk=new();
        public static Vector3 Hospital(Vector3 at)
        {
            Vector3 best=UrbanCatalog.Door(4);float d=(best-at).sqrMagnitude;
            foreach(var v in FourCityCatalog.Venues)if(v.kind==VenueKind.Hospital&&v.city!=2){float n=(v.Entrance-at).sqrMagnitude;if(n<d){d=n;best=v.Entrance;}}
            return best;
        }
        public static EmergencyAmbulance Create(WorldActor patient,Action done)
        {
            var hospital=Hospital(patient.transform.position);Vector3 origin=hospital+Vector3.back*12+Vector3.right*((CitySafety.Instance?CitySafety.Instance.ActiveAmbulances:0)%6*6);
            if(!CityGangWar.FindGround(origin,out origin)&&!ResponseDispatch.TryOrigin(patient.transform.position,false,false,0,out origin)){done?.Invoke();Destroy(patient.GetComponent<MedicalPending>());return null;}
            var car=UrbanSimulation.Instance.Spawn(origin,false,3);car.name="119 / 응급 구조·환자 이송";car.occupied=true;car.gameObject.AddComponent<AmbulanceArt>();
            var a=car.gameObject.AddComponent<EmergencyAmbulance>();a.Car=car;a.Patient=patient;a.finished=done;a.hospital=hospital;a.injury=patient.GetComponent<MedicalState>();
            car.gameObject.AddComponent<ResponseLightbar>();
            return a;
        }
        void Team()
        {
            medics=new Transform[2];
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"구급대 응급구조사":"구급대 이송 대원",typeof(SpriteRenderer),typeof(WorldActor),typeof(RescueMedic));medics[i]=go.transform;medics[i].position=Rear+transform.forward*(i*1.5f-.75f);
                var r=go.GetComponent<SpriteRenderer>();r.sprite=PeopleArt.Get(i==0?"Doctor":"Nurse",0);r.sharedMaterial=Resources.Load<Material>("Materials/PixelActor");NpcPersona.Ensure(go.GetComponent<WorldActor>(),i==0?"Doctor":"Nurse",i==0?"응급구조사":"구급 간호사");
            }
            Stretcher=new GameObject("Two-person medical stretcher").transform;Stretcher.position=Rear;
            WorldGeometry.Part(Stretcher,"Stretcher frame",new Vector3(0,.65f,0),new Vector3(.75f,.08f,2.45f),"Chrome");
            WorldGeometry.Part(Stretcher,"Canvas mattress",new Vector3(0,.76f,0),new Vector3(.68f,.14f,1.86f),"DistrictBlue");
            for(int s=-1;s<=1;s+=2){WorldGeometry.Part(Stretcher,"Carry handle",new Vector3(s*.38f,.72f,0),new Vector3(.055f,.055f,2.8f),"Chrome");WorldGeometry.Part(Stretcher,"Collapsible leg",new Vector3(s*.25f,.42f,0),new Vector3(.04f,.55f,1.4f),"Chrome");}
            NpcSpeech.Say(medics[0],"구급대입니다! 사격을 피해 부상자에게 접근합니다.",4);Phase=1;
        }
        Vector3 Rear=>transform.position-Car.Forward*(Car.HalfLength+2.8f);
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.06f,Time.deltaTime);life+=dt;
            if(!Car||Car.Wrecked||!Patient||!Patient.Alive||life>420){Finish();return;}
            if(medics!=null)foreach(var m in medics)if(m&&(!m.GetComponent<WorldActor>().Alive||m.GetComponent<WorldActor>().Downed)){Finish();return;}
            if(Phase==0)
            {
                drive.Drive(Car,Patient.transform.position,dt,19);
                if(Vector3.ProjectOnPlane(Patient.transform.position-transform.position,Vector3.up).magnitude<22&&Mathf.Abs(Patient.transform.position.y-transform.position.y)<3){Car.speed=0;Team();}return;
            }
            if(Phase==1)
            {
                var delta=Patient.transform.position-Stretcher.position;delta.y=0;MoveTeam(Patient.transform.position,3.2f,dt);
                if(delta.magnitude<1.5f){Phase=2;hold=3.2f;NpcSpeech.Say(medics[0],"출혈을 막겠습니다. 의식 확인, 들것 준비!",4);}
            }
            else if(Phase==2)
            {
                hold-=dt;if(hold<=0){if(injury&&injury.FirstAid()){if(CitySafety.Instance)CitySafety.Instance.FieldTreatments++;Destroy(Patient.GetComponent<MedicalPending>());Phase=5;hold=5;NpcSpeech.Say(medics[0],"응급처치 완료. 움직일 수 있습니다. 안전한 곳으로 이동하세요.",5);DestroyTeam();return;}injury?.Stabilize();loaded=true;var pending=Patient.GetComponent<MedicalPending>();if(pending)pending.carried=true;foreach(var c in Patient.GetComponents<Collider>())c.enabled=false;var impact=Patient.GetComponent<CivilianImpact>();if(impact)Destroy(impact);Phase=3;NpcSpeech.Say(medics[1],"하나, 둘, 셋! 들어 올립니다. 병원으로 이송!",4);}
            }
            else if(Phase==3)
            {
                MoveTeam(Rear,2.6f,dt);Patient.transform.position=Stretcher.position+Vector3.up*.85f;var art=Patient.GetComponent<DirectionalPerson>();if(art)art.Lying=true;
                if(Vector3.ProjectOnPlane(Rear-Stretcher.position,Vector3.up).magnitude<1.7f){Phase=4;foreach(var r in Patient.GetComponentsInChildren<Renderer>())r.enabled=false;DestroyTeam();}
            }
            else if(Phase==4)
            {
                Patient.transform.position=transform.position;drive.Drive(Car,hospital,dt,17);
                if(Vector3.ProjectOnPlane(transform.position-hospital,Vector3.up).magnitude<20)
                {
                    Car.speed=0;Phase=5;hold=5;Vector3 safe;if(!CityGangWar.FindGround(hospital+Vector3.right*4,out safe))safe=Rear;
                    injury?.Recover(safe);loaded=false;Destroy(Patient.GetComponent<MedicalPending>());if(CitySafety.Instance)CitySafety.Instance.Transports++;
                    NpcSpeech.Say(Patient,Patient.police||Patient.military?"치료 완료. 복귀 명령을 기다리겠습니다.":"구해 주셔서 고마워요. 정말 큰일 날 뻔했어요.",4);
                }
            }
            else if(Phase==5){hold-=dt;if(hold<=0)Finish();}
        }
        void MoveTeam(Vector3 goal,float speed,float dt)
        {
            var d=walk.Direction(Stretcher.position,goal);Stretcher.position+=d*speed*dt;if(CityGangWar.FindGround(Stretcher.position,out var at))Stretcher.position=at;
            if(d.sqrMagnitude>.01f)Stretcher.rotation=Quaternion.Slerp(Stretcher.rotation,Quaternion.LookRotation(d),dt*7);
            for(int i=0;i<2;i++)medics[i].position=Stretcher.position+Stretcher.forward*(i==0?1.65f:-1.65f);
        }
        void DestroyTeam(){if(Stretcher)Destroy(Stretcher.gameObject);if(medics!=null)foreach(var m in medics)if(m){Destroy(m.GetComponent<RescueMedic>());if(m.GetComponent<WorldActor>().Alive&&!m.GetComponent<WorldActor>().Downed)Destroy(m.gameObject);}medics=null;}
        void Finish()
        {
            if(Patient){if(loaded){if(CityGangWar.FindGround(Rear,out var safe))Patient.transform.position=safe;foreach(var r in Patient.GetComponentsInChildren<SpriteRenderer>())r.enabled=true;}Destroy(Patient.GetComponent<MedicalPending>());}
            DestroyTeam();finished?.Invoke();finished=null;Destroy(gameObject);
        }
        void OnDestroy(){if(Patient){Destroy(Patient.GetComponent<MedicalPending>());if(loaded){if(CityGangWar.FindGround(Rear,out var safe))Patient.transform.position=safe;foreach(var r in Patient.GetComponentsInChildren<SpriteRenderer>())r.enabled=true;}}DestroyTeam();finished?.Invoke();finished=null;}
    }
    public sealed class RescueMedic:MonoBehaviour{}
}
