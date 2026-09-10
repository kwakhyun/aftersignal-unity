using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    // Original armored 6x6 silhouette: +X is the vehicle's forward axis everywhere.
    public sealed class MilitaryGunTruck:MonoBehaviour
    {
        public int Variant;public int Shots{get;private set;}public int Ammunition{get;private set;}=240;
        CityVehicle car;Transform turret,gun,muzzle;float nextShot,reload;public bool Built{get;private set;}
        public Vector3 Muzzle=>muzzle?muzzle.position:transform.position+Vector3.up*3.5f;
        public static MilitaryGunTruck Install(CityVehicle car,int variant)
        {var art=car.gameObject.AddComponent<MilitaryGunTruck>();art.Variant=variant;car.gameObject.AddComponent<RegionalParked>();return art;}
        IEnumerator Start()
        {
            car=GetComponent<CityVehicle>();yield return null;yield return null;
            foreach(var r in GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            foreach(var c in GetComponentsInChildren<Collider>())Destroy(c);var hull=gameObject.AddComponent<BoxCollider>();hull.center=new(.15f,1.4f,0);hull.size=new(9.7f,2.7f,3.25f);
            var details=GetComponent<VehicleDetails>();if(details)details.enabled=false;
            var root=new GameObject("Kestrel 6x6 armored weapons carrier").transform;root.SetParent(transform,false);
            GameObject P(string n,Vector3 at,Vector3 size,string mat,PrimitiveType shape=PrimitiveType.Cube){var part=WorldGeometry.Part(root,n,at,size,mat,shape);part.GetComponent<Renderer>().sharedMaterial=CityGeometry.Material(mat);return part;}
            P("Protected V hull",new(0,.85f,0),new(8.5f,.65f,2.75f),"DefenseDeck");
            P("Chassis rail",new(-.1f,.53f,0),new(9,.3f,2),"DarkMetal");
            P("Sloped engine armor",new(3.85f,1.4f,0),new(2.15f,.7f,2.6f),"Metal").transform.localRotation=Quaternion.Euler(0,0,-9);
            P("Front impact bumper",new(4.8f,.85f,0),new(.28f,.45f,3.05f),"DarkMetal");
            P("Crew cabin floor",new(1.75f,1.16f,0),new(2.8f,.25f,2.6f),"DefenseDeck");
            P("Cabin armored roof",new(1.72f,2.75f,0),new(3.2f,.18f,2.8f),"Metal");
            for(int side=-1;side<=1;side+=2)
            {
                P("Ballistic lower door",new(1.75f,1.55f,side*1.3f),new(2.8f,.7f,.15f),"DefenseDeck");
                for(int i=0;i<3;i++)P("Door pillar",new(.4f+i*1.38f,2.28f,side*1.3f),new(.13f,1,.13f),"Metal");
                P("Door step",new(1.8f,.75f,side*1.52f),new(2.5f,.12f,.45f),"DarkMetal");
                P("Mirror arm",new(3.05f,2.3f,side*1.62f),new(.1f,.1f,.55f),"Metal");
                P("Mirror",new(3.05f,2.3f,side*1.9f),new(.14f,.4f,.25f),"Chrome");
                P("Armored bed side",new(-2.1f,1.5f,side*1.3f),new(4.5f,.9f,.18f),"DefenseDeck");
                P("Tail lamp",new(-4.42f,1.15f,side*1.03f),new(.08f,.2f,.28f),"NeonPink");
                P("Headlamp surround",new(4.9f,1.15f,side*1.02f),new(.12f,.28f,.55f),"DarkMetal");
                P("LED headlamp",new(4.98f,1.15f,side*1.02f),new(.025f,.11f,.4f),"DistrictIvory");
                for(int axle=0;axle<3;axle++)
                {
                    float x=axle==0?3.1f:axle==1?-1.85f:-3.3f;
                    var wheel=P("Run flat road wheel",new(x,.7f,side*1.48f),new(1.4f,.3f,1.4f),"DarkMetal",PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                    var hub=P("Armored hub",new(x,.7f,side*1.8f),new(.68f,.025f,.68f),"Metal",PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(90,0,0);
                    var spin=wheel.AddComponent<GarrisonWheel>();spin.Car=car;spin.Side=side;
                    P("Wheel arch",new(x,1.45f,side*1.45f),new(1.7f,.12f,.7f),"Metal");
                }
            }
            for(int i=0;i<9;i++)P("Radiator grille",new(4.85f,1.4f,-.7f+i*.175f),new(.06f,.46f,.065f),"DarkMetal");
            P("Winch housing",new(4.98f,.78f,0),new(.4f,.35f,.75f),"Metal");
            P("Rear tailgate",new(-4.4f,1.52f,0),new(.16f,.85f,2.7f),"Metal");
            P("Fuel protection box",new(-.15f,.82f,1.2f),new(1.4f,.65f,.65f),"DarkMetal");
            P("Communications mast",new(-3.8f,2.5f,1.05f),new(.05f,3.2f,.05f),"Metal");
            for(int i=0;i<4;i++){P("Secured ammunition case",new(-3.4f+i%2*.95f,1.55f,i<2?-.66f:.66f),new(.8f,.55f,.9f),"DefenseDeck");P("Case fastening",new(-3.4f+i%2*.95f,1.84f,i<2?-.66f:.66f),new(.11f,.035f,.92f),"Metal");}
            if(Variant%2==1)for(int side=-1;side<=1;side+=2){P("Cage armor",new(-2,2.25f,side*1.45f),new(4.2f,.12f,.08f),"Metal");for(int i=0;i<8;i++)P("Spaced cage slat",new(-3.8f+i*.5f,1.9f,side*1.45f),new(.055f,.7f,.07f),"Metal");}
            turret=new GameObject("Remote weapon station azimuth").transform;turret.SetParent(root,false);turret.localPosition=new(-.65f,2.05f,0);
            WorldGeometry.Part(turret,"Bearing ring",Vector3.zero,new(1.2f,.2f,1.2f),"Metal",PrimitiveType.Cylinder);
            WorldGeometry.Part(turret,"Turret shield",new(.05f,.55f,0),new(.7f,1.05f,1),"DefenseDeck");
            gun=new GameObject("Weapon elevation").transform;gun.SetParent(turret,false);gun.localPosition=new(.3f,.85f,0);
            WorldGeometry.Part(gun,"Heavy machine gun receiver",new(.3f,0,0),new(.95f,.22f,.25f),"DarkMetal");
            WorldGeometry.Part(gun,"Machine gun barrel",new(1.35f,0,0),new(1.25f,.075f,.075f),"Metal");
            WorldGeometry.Part(gun,"Thermal optic",new(.18f,.19f,-.25f),new(.32f,.24f,.23f),"DarkMetal");
            WorldGeometry.Part(gun,"Ammo feed box",new(-.05f,-.1f,.48f),new(.65f,.5f,.55f),"DefenseDeck");
            muzzle=new GameObject("Actual barrel muzzle").transform;muzzle.SetParent(gun,false);muzzle.localPosition=new(2.03f,0,0);
            var shader=Resources.Load<Shader>("Shaders/StructuralGlass");if(shader){var glass=new Material(shader);glass.SetColor("_BaseColor",new Color(.10f,.22f,.26f,.08f));
                var w=P("Clear armored windshield",new(3.17f,2.25f,0),new(.06f,.84f,2.37f),"Glass");w.GetComponent<Renderer>().sharedMaterial=glass;
                for(int side=-1;side<=1;side+=2)P("Clear side glazing",new(1.75f,2.25f,side*1.3f),new(2.55f,.84f,.055f),"Glass").GetComponent<Renderer>().sharedMaterial=glass;ownedGlass=glass;}
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(!renderer.sharedMaterial)renderer.sharedMaterial=CityGeometry.Material("Metal");
            BatchHull(root,turret);car.GetComponent<VehicleCabin>()?.SetCrew("Soldier",0);Built=true;
        }
        static void BatchHull(Transform root,Transform movingTurret)
        {
            var groups=new System.Collections.Generic.Dictionary<Material,System.Collections.Generic.List<CombineInstance>>();
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.transform.IsChildOf(movingTurret)||renderer.GetComponent<GarrisonWheel>())continue;var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||!renderer.sharedMaterial)continue;
                if(!groups.TryGetValue(renderer.sharedMaterial,out var list)){list=new();groups.Add(renderer.sharedMaterial,list);}list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*renderer.transform.localToWorldMatrix});renderer.enabled=false;
            }
            var owner=root.gameObject.AddComponent<CityMeshOwner>();
            foreach(var group in groups){var mesh=new Mesh{name="KESTREL batched hull",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,true);owner.meshes.Add(mesh);var go=new GameObject("Batched hull / "+group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=group.Key;}
        }
        Material ownedGlass;
        public void AimAt(Vector3 aim,float dt)
        {
            if(!turret||!gun)return;var d=transform.InverseTransformDirection(aim-turret.position);float yaw=Mathf.Atan2(-d.z,d.x)*Mathf.Rad2Deg;
            turret.localRotation=Quaternion.RotateTowards(turret.localRotation,Quaternion.Euler(0,yaw,0),150*dt);
            float pitch=Mathf.Clamp(Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg,-18,65);gun.localRotation=Quaternion.RotateTowards(gun.localRotation,Quaternion.Euler(0,0,pitch),100*dt);
        }
        public void TickPlayer(ControlFrame input,float dt)
        {
            if(!Built||!car||car.Wrecked||!Camera.main)return;var aim=Ballistics.AimPoint(Camera.main.ViewportPointToRay(new(.5f,.5f)),transform);AimAt(aim,dt);
            if(reload>0){reload-=dt;if(reload<=0)Ammunition=240;return;}if(input.reload&&Ammunition<240){reload=3;return;}
            if(!input.attack||Time.time<nextShot||Ammunition<=0)return;nextShot=Time.time+.11f;Ammunition--;Shots++;
            var d=(aim-Muzzle).normalized;var end=Muzzle+d*1000;
            if(Ballistics.Cast(Muzzle,d,1000,transform,out var hit)){end=hit.point;CombatVfx.Hit(hit,d);hit.collider.GetComponentInParent<WorldActor>()?.Damage(30,d*4);hit.collider.GetComponentInParent<CityVehicle>()?.Damage(24,end);}
            CombatVfx.Tracer(Muzzle,end,SignalEffects.Gold);GameDirector.Instance.Audio.PlayGun(GunshotKind.Automatic,Muzzle,.75f);
        }
        public void Resupply()=>Ammunition=240;
        void OnGUI(){if(!car||!UrbanSimulation.Instance||UrbanSimulation.Instance.Current!=car||GameDirector.Instance.Blocked)return;var style=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("Fonts/NotoSansKR"),fontSize=17,normal={textColor=Color.white}};GUI.Label(new Rect(26,Screen.height*.65f,480,45),reload>0?"기관총 재장전 "+reload.ToString("0.0"):"기관총 "+Ammunition+" / 240 · 좌클릭 발사 · R 재장전",style);}
        void OnDestroy(){if(ownedGlass)Destroy(ownedGlass);}
    }
    public sealed class GarrisonWheel:MonoBehaviour{public CityVehicle Car;public int Side;void Update(){if(Car&&GameDirector.Instance&&!GameDirector.Instance.Blocked&&LocalSimulation.Within(transform.position,180))transform.Rotate(Vector3.up,Car.speed*Time.deltaTime*82,Space.Self);}}
}
