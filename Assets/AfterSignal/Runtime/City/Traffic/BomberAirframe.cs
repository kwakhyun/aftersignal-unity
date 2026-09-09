using UnityEngine;
namespace AfterSignal
{
    public static class BomberAirframe
    {
        public static void Install(CityVehicle car)
        {
            foreach(var r in car.GetComponentsInChildren<Renderer>())r.enabled=false;
            var root=new GameObject("Detailed vehicle coachwork").transform;root.SetParent(car.transform,false);
            // Faceted flying wing, with swept leading edges and a serrated trailing edge.
            Vector3[] ring={new(14,1.1f,0),new(8,1.3f,-4),new(-5,.9f,-26),new(-10,.8f,-24),new(-6,1,-12),new(-11,1,-8),new(-7,1.3f,-4),new(-11,1.3f,0),new(-7,1.3f,4),new(-11,1,8),new(-6,1,12),new(-10,.8f,24),new(-5,.9f,26),new(8,1.3f,4)};
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            void Triangle(Vector3 a,Vector3 b,Vector3 c){int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
            for(int i=0;i<ring.Length;i++){var a=ring[i];var b=ring[(i+1)%ring.Length];Triangle(new(0,2.4f,0),a,b);Triangle(new(0,.6f,0),b-Vector3.up*.35f,a-Vector3.up*.35f);Triangle(a,a-Vector3.up*.35f,b);Triangle(b,a-Vector3.up*.35f,b-Vector3.up*.35f);}
            var mesh=new Mesh{name="OBSIDIAN swept wing shell"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var skin=new GameObject("Radar absorbent airframe",typeof(MeshFilter),typeof(MeshRenderer));skin.transform.SetParent(root,false);root.gameObject.AddComponent<CityMeshOwner>().meshes.Add(mesh);skin.GetComponent<MeshFilter>().sharedMesh=mesh;skin.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Materials/DarkMetal");
            WorldGeometry.Part(root,"Forward pressure fuselage",new(5,1.4f,0),new(14,2.6f,4.4f),"Metal",PrimitiveType.Sphere);
            WorldGeometry.Part(root,"Windshield and side windows",new(9,2.55f,0),new(3.8f,1.35f,3.2f),"Glass",PrimitiveType.Sphere);
            for(int s=-1;s<=1;s+=2)
            {
                for(int i=0;i<2;i++){float z=s*(3.6f+i*2.2f);WorldGeometry.Part(root,"Recessed jet intake",new(1.8f,2.05f,z),new(4,.65f,1.6f),"Rubber");WorldGeometry.Part(root,"Jet exhaust shroud",new(-6.4f,1.6f,z),new(3,.4f,1.45f),"Metal");WorldGeometry.Part(root,"Exhaust aperture",new(-8,1.6f,z),new(.08f,.25f,1.1f),"Rubber");}
                var door=WorldGeometry.Part(root,"Bomb bay door "+s,new(-1,.63f,s*1.2f),new(8,.13f,2.3f),"DefenseDeck");
                WorldGeometry.Part(root,"Navigation lens",new(-5,1,s*25.5f),new(.18f,.12f,.25f),s<0?"RedFX":"CyanFX");
                for(int i=0;i<3;i++){float z=s*(8+i*4.5f);var panel=WorldGeometry.Part(root,"Control surface seam",new(-5,1.15f,z),new(2.5f,.025f,.05f),"Chrome");panel.transform.localRotation=Quaternion.Euler(0,s*-30,0);}
                for(int i=0;i<2;i++){var tire=WorldGeometry.Part(root,"Landing wheel",new(i==0?9:-3,.45f,s*(i==0?.65f:3.1f)),new(.8f,.8f,.35f),"Rubber",PrimitiveType.Cylinder);tire.transform.localRotation=Quaternion.Euler(90,0,0);}
            }
            var hit=car.GetComponent<BoxCollider>();if(hit){hit.center=new(0,1.4f,0);hit.size=new(27,2.4f,7);}
            var wing=car.gameObject.AddComponent<BoxCollider>();wing.center=new(-5,1.05f,0);wing.size=new(7,1.2f,50);
            car.gameObject.AddComponent<BomberBay>();
        }
    }
    public sealed class BomberBay:MonoBehaviour
    {
        CityVehicle car;WorldActor gunner;float next,reload;int ripple;Vector3 runHeading;bool returning;Transform[] doors;
        public int Bombs {get;private set;}=96;public int Released {get;private set;}
        public bool Dropping=>ripple>0;public float ReloadRemaining=>reload;
        void Awake(){car=GetComponent<CityVehicle>();}
        void Start()=>car.GetComponent<VehicleCabin>()?.SetCrew("AirForceCrew",car.occupied?1:0);
        public void StartSalvo(){if(!car||!car.occupied||car.Wrecked||Dropping||Bombs==0||transform.position.y<15||reload>0)return;ripple=Mathf.Min(24,Bombs);}
        public void Tick(ControlFrame input,float dt)
        {
            if(input.secondaryFire||input.attack)StartSalvo();
            if(input.reload&&reload<=0&&!Dropping&&Bombs<96&&VehicleGround.Sample(car,transform.position,.3f,4,out _))reload=8;
            if(reload>0){reload-=dt;if(reload<=0)Bombs=96;}
        }
        public void FlyRun(Vector3 target,float dt)
        {
            if(runHeading.sqrMagnitude<.1f)runHeading=Vector3.ProjectOnPlane(target-transform.position,Vector3.up).normalized;
            float along=Vector3.Dot(transform.position-target,runHeading);
            if(!returning&&along>320)returning=true;
            var destination=target+runHeading*(returning?-520:500)+Vector3.up*(returning?155:115);
            var delta=destination-transform.position;var direction=Vector3.ProjectOnPlane(delta,Vector3.up);
            if(returning&&direction.magnitude<150){returning=false;runHeading=Vector3.ProjectOnPlane(target-transform.position,Vector3.up).normalized;}
            var input=ControlFrame.Empty;var dynamics=car.GetComponent<CraftDynamics>();
            input.move=new Vector2(Mathf.Clamp(Vector3.SignedAngle(car.Forward,direction,Vector3.up)/20,-1,1),Mathf.Clamp((.7f-(dynamics?dynamics.Throttle:0))*5,-1,1));input.vertical=Mathf.Clamp(delta.y/18,-1,1);car.Drive(input,dt);
            if(!LocalSimulation.Combat(target)||returning||Vector3.Distance(target,transform.position)>450)return;
            float time=Mathf.Sqrt(2*Mathf.Max(0,transform.position.y-target.y)/9.81f);
            var impact=transform.position+car.Forward*Mathf.Abs(car.speed)*time;impact.y=target.y;
            if((impact-target).sqrMagnitude<65*65&&car.speed>30)StartSalvo();
        }
        void OnGUI()
        {
            var g=GameDirector.Instance;var sim=UrbanSimulation.Instance;if(!g||g.Blocked||!sim||sim.Current!=car||sim.SeatIndex!=0)return;
            var style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSansKR"),fontSize=Mathf.RoundToInt(Screen.height/55f),normal={textColor=new Color(.65f,1,.87f)}};
            GUI.Label(new Rect(25,Screen.height*.62f,600,85),"OBSIDIAN · 폭탄 "+Bombs+" / 96\n"+(reload>0?"재보급 중 "+reload.ToString("0.0")+"초":Dropping?"연속 투하 중":"우클릭 · 24발 연속 투하 / 착륙 후 R 재보급"),style);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked||!car)return;
            if(!car.occupied||car.Wrecked||GetComponent<TitanGravitySnare>()){ripple=0;return;}
            if(doors==null){var list=new System.Collections.Generic.List<Transform>();foreach(var t in GetComponentsInChildren<Transform>())if(t.name.StartsWith("Bomb bay door"))list.Add(t);doors=list.ToArray();}
            foreach(var door in doors)door.localRotation=Quaternion.RotateTowards(door.localRotation,Quaternion.Euler(Dropping?(door.localPosition.z<0?75:-75):0,0,0),Time.deltaTime*140);
            if(ripple<=0||Time.time<next||FallingBomb.Active>=72)return;
            if(GetComponent<MilitaryVehicleAI>()&&!gunner){var go=new GameObject("Bomber crew");go.transform.SetParent(transform,false);gunner=go.AddComponent<WorldActor>();gunner.military=true;gunner.helicopter=true;gunner.enabled=false;}
            next=Time.time+.17f;ripple--;Bombs--;Released++;
            var at=transform.TransformPoint(new Vector3(-1,.1f,Released%2==0?-1.15f:1.15f));FallingBomb.Release(at,car.Forward*Mathf.Abs(car.speed)+transform.forward*(Released%2==0?-1.4f:1.4f),car,UrbanSimulation.Instance&&UrbanSimulation.Instance.Current==car?null:gunner);
            if(Released%6==1)g.Audio.Play("cannon",at,.12f,2);
        }
    }
    public sealed class FallingBomb:MonoBehaviour
    {
        public static int Active {get;private set;}public static int Detonations {get;private set;}
        Vector3 velocity;CityVehicle carrier;WorldActor source;float age;
        public static void Release(Vector3 at,Vector3 velocity,CityVehicle carrier,WorldActor source)
        {
            var bomb=new GameObject("OBSIDIAN / free-fall bomb").AddComponent<FallingBomb>();bomb.transform.position=at;bomb.velocity=velocity;bomb.carrier=carrier;bomb.source=source;Active++;
            WorldGeometry.Part(bomb.transform,"Bomb casing",Vector3.zero,new(.32f,.32f,1.15f),"Metal",PrimitiveType.Sphere);
            for(int i=0;i<2;i++){var fin=WorldGeometry.Part(bomb.transform,"Tail fins",new(0,0,-.5f),new(.7f,.045f,.35f),"Chrome");fin.transform.localRotation=Quaternion.Euler(0,0,i*90);}
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||g.Blocked)return;float dt=Mathf.Min(.05f,Time.deltaTime);age+=dt;velocity+=Vector3.down*9.81f*dt;var delta=velocity*dt;
            if(Ballistics.Cast(transform.position,delta.normalized,delta.magnitude+.2f,carrier?carrier.transform:null,out var hit,source!=null)){transform.position=hit.point;Detonate(hit.collider.GetComponentInParent<CityVehicle>());return;}
            transform.position+=delta;transform.rotation=Quaternion.LookRotation(velocity);
            if(OceanLife.Contains(transform.position)&&transform.position.y<OceanLife.Surface)Detonate(null);else if(age>18)Destroy(gameObject);
        }
        void Detonate(CityVehicle direct)
        {Detonations++;VehicleExplosion.Create(transform.position,4.2f);BlastDamage.Create(transform.position,17,320,source,carrier,BlastPayload.Missile,direct);GameDirector.Instance.Audio.Play("urban_explosion",transform.position,.55f,3);Destroy(gameObject);}
        void OnDestroy()=>Active=Mathf.Max(0,Active-1);
    }
}
