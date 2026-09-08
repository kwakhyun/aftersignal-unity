using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static int novaBuildings,novaInteriors;static readonly List<Vector3> novaOccupied=new();
        [MenuItem("AFTERSIGNAL/World/Build Nova Strait expansion")]
        public static void BuildNeonHarbor()
        {
            AssetDatabase.Refresh();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);materials.Clear();boxes.Clear();meshId=75000;ImportMaterials();MakeMaterials();
            Mat("NovaObsidian","#14212d",.6f,.45f);Mat("NovaPorcelain","#b9c9c5",.35f,.4f);Mat("NovaCopper","#ac6f55",.68f,.5f);Mat("NovaViolet","#39334f",.48f,.38f);Mat("NovaTeal","#31575e",.52f,.43f);Mat("NovaConcrete","#6a7279",.08f,.3f);
            Mat("NovaNeonViolet","#b85cff",.1f,.4f,2);Mat("NovaNeonIce","#81ffe7",.1f,.4f,2);Mat("NovaNeonAmber","#ffab48",.1f,.4f,1.6f);Mat("NovaWindow","#376676",.5f,.78f,.22f);
            FinishNovaMaterials();
            Mat("CivicStone","#c4c0b4",.05f,.28f);Mat("CivicTeal","#173a42",.48f,.5f);Mat("CivicBrass","#c0a05d",.78f,.48f);
            var seabed=Mat("NovaSeabed","#a8b6a3");seabed.shader=Shader.Find("AfterSignal/PelagicFloor");seabed.SetTexture("_BaseMap",materials["Sand"].GetTexture("_BaseMap"));
            root=new GameObject("NOVA STRAIT / four islands, canals and vertical city",typeof(NeonHarbor)).transform;novaBuildings=novaInteriors=0;novaOccupied.Clear();
            NovaTerrain();NovaRoads();NovaLandmarks();NovaDistricts();NovaSkyRail();NovaOldCityInfill();NovaReefs();
            CombinePresentation();StripNovaSourceMeshes();SaveGeneratedMeshes();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Output+"NeonHarbor.prefab");Object.DestroyImmediate(root.gameObject);AssetDatabase.SaveAssets();
            System.IO.Directory.CreateDirectory("Documentation/NeonHarbor");System.IO.File.WriteAllText("Documentation/NeonHarbor/world-counts.json","{\"buildings\":"+novaBuildings+",\"walk_in_buildings\":"+novaInteriors+",\"road_routes\":"+NeonHarbor.Roads.Count+"}");
            Debug.Log("NOVA STRAIT: "+novaBuildings+" buildings, "+novaInteriors+" walk-in interiors, 4 islands, 11 road routes, skyrail and sea crossing.");
        }
        public static void NeonAndRelease(){BuildNeonHarbor();ProjectBuilder.BuildRelease();}
        public static void PolishNeonAndRelease()
        {
            AssetDatabase.Refresh();materials.Clear();ImportMaterials();
            foreach(var name in new[]{"NovaConcrete","NovaWindow"})materials[name]=AssetDatabase.LoadAssetAtPath<Material>(Output+"Generated/"+name+".mat");
            FinishNovaMaterials();AssetDatabase.SaveAssets();ProjectBuilder.BuildRelease();
        }
        static void FinishNovaMaterials()
        {
            var concrete=materials["NovaConcrete"];concrete.shader=Shader.Find("AfterSignal/NovaSurface");concrete.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Material>(Output+"Generated/Pavement.mat").GetTexture("_BaseMap"));EditorUtility.SetDirty(concrete);
            var windows=materials["NovaWindow"];windows.shader=Shader.Find("AfterSignal/NovaWindows");EditorUtility.SetDirty(windows);
        }
        static void StripNovaSourceMeshes()
        {
            // The combined meshes are the presentation. Retain collision and interactive objects,
            // remove the duplicated disabled source renderers and their empty leaf transforms.
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(!renderer||renderer.enabled)continue;
                var go=filter.gameObject;Object.DestroyImmediate(renderer);if(!go.GetComponent<MeshCollider>())Object.DestroyImmediate(filter);
                if(go.transform.childCount==0&&go.GetComponents<Component>().Length==1)Object.DestroyImmediate(go);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>())if(t.GetComponentInParent<NeonTransit>()||t.GetComponentInParent<KelpCurrent>())t.gameObject.isStatic=false;
        }
        static void NovaTerrain()
        {
            foreach(var r in NeonHarbor.Land)
            {
                Box(root,"Island foundation",new Vector3(r.center.x,-2,r.center.y),new Vector3(r.width,4,r.height),"NovaConcrete");
                for(int s=-1;s<=1;s+=2)
                {
                    Box(root,"Quayside coping",new Vector3(r.center.x,.12f,r.center.y+s*r.height*.5f),new Vector3(r.width,.24f,2.2f),"NovaPorcelain");
                    Box(root,"Canal retaining wall",new Vector3(r.center.x+s*r.width*.5f,-2.8f,r.center.y),new Vector3(1.8f,5.6f,r.height),"NovaConcrete");
                }
            }
            Box(root,"Open strait water",new Vector3(1100,OceanLife.Surface,-3380),new Vector3(2200,.02f,2380),"Ocean",false);
            // The old ocean ends at -2200. Continue its seabed with a sampled surface.
            var v=new List<Vector3>();var tri=new List<int>();int nx=56,nz=60;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){float xx=x*2200f/nx,zz=-2100-z*2450f/nz;v.Add(new Vector3(xx,OceanLife.Bed(xx,zz),zz));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int k=z*(nx+1)+x;tri.AddRange(new[]{k,k+1,k+nx+1,k+1,k+nx+2,k+nx+1});}
            MeshObject(root,"Continental shelf and deep strait",v.ToArray(),tri.ToArray(),"NovaSeabed",true);
            var dock=Group("Nova passenger quay",new Vector3(927.4f,0,-2412));
            Box(dock,"Boarding pier",new Vector3(0,-.12f,15),new Vector3(9,.24f,36),"Pavement");
            // Unrailed gangway faces the ferry door; both sides remain physically reachable.
            for(int s=-1;s<=1;s+=2)for(int i=0;i<5;i++)Beam(dock,"Mooring pile",new Vector3(s*5.3f,-7,i*7),new Vector3(s*5.3f,.65f,i*7),.35f,"Steel",true);
            Text(dock,"NOVA  /  STRAIT FERRY",new Vector3(0,4,0),.5f,"NovaNeonIce",180);
            for(int i=0;i<8;i++)
            {
                var buoy=Group("Navigation buoy",new Vector3(1030+i%2*110,OceanLife.Surface,-850-i*180));
                Box(buoy,"Floating buoy",Vector3.zero,new Vector3(2,1.3f,2),"CargoRed",false,PrimitiveType.Cylinder);Beam(buoy,"Buoy light mast",Vector3.zero,Vector3.up*4,.14f,"Steel");Box(buoy,"Navigation light",Vector3.up*4,Vector3.one*.5f,"NovaNeonIce",false,PrimitiveType.Sphere);
            }
        }
        static void NovaRoads()
        {
            foreach(var road in NeonHarbor.Roads)
            {
                Ribbon(root,"Nova boulevard",road,20,0,.025f,"Asphalt",true);
                for(int s=-1;s<=1;s+=2){Ribbon(root,"Accessible street pavement",road,4,s*12,.06f,"Pavement",true);Ribbon(root,"Street curb",road,.18f,s*10,.08f,"NovaPorcelain");Ribbon(root,"Road edge stripe",road,.14f,s*9.4f,.06f,"PaintWhite");}
                for(int i=0;i<road.Length-3;i+=3)Ribbon(root,"Lane dash",new[]{road[i],road[i+1]},.14f,0,.065f,"PaintAmber");
                for(int i=3;i<road.Length-3;i+=5)
                {
                    var tangent=(road[i+1]-road[i-1]).normalized;var side=Vector3.Cross(Vector3.up,tangent);
                    for(int s=-1;s<=1;s+=2)
                    {
                        var at=road[i]+side*s*15;
                        if(at.y>1){Beam(root,"Bridge support",at+Vector3.down*(at.y+7),at,.65f,"NovaConcrete",true);continue;}
                        var pole=Group("Nova street furnishing",at);StreetBreakable(pole,120000,.17f,7);Beam(pole,"Street lamp pole",Vector3.zero,Vector3.up*7,.13f,"Steel");Beam(pole,"Road light arm",Vector3.up*7,Vector3.up*7-side*s*2.6f,.12f,"Steel");Box(pole,"LED fixture",Vector3.up*6.95f-side*s*2.5f,new Vector3(.5f,.12f,1.2f),i%2==0?"NovaNeonIce":"NovaNeonViolet",false);
                        if(i%15==3){Prop(pole,new Vector3(2,0,0),PropUse.Vending);Box(pole,"Public information pillar",new Vector3(-2,1.4f,0),new Vector3(.5f,2.8f,1.2f),"NovaObsidian");Box(pole,"Route panel",new Vector3(-2.26f,1.6f,0),new Vector3(.03f,1.8f,.9f),"NovaNeonIce",false);}
                    }
                    if(i%10==3&&NeonHarbor.OnIsland(road[i]+side*18)&&road[i].y<1)Crowd(root,"노바 대로 시민",road[i]+side*18,6,24+i%24,4,6);
                }
            }
            for(int i=0;i<3;i++)
            {
                float z=-2650-i*510;var bridge=new[]{new Vector3(940,.05f,z),new Vector3(1000,8,z),new Vector3(1130,8,z),new Vector3(1190,.05f,z)};
                Ribbon(root,"Canal pedestrian bridge",bridge,6,0,0,"NovaPorcelain",true);
                for(int s=-1;s<=1;s+=2)Ribbon(root,"Lit bridge rail",bridge,.14f,s*3,1.1f,"NovaNeonIce");
                for(int j=0;j<5;j++){float x=1010+j*25;Beam(root,"Suspension bridge cable",new Vector3(x,8,z),new Vector3(1065,21,z),.06f,"Aluminium");}
            }
        }
        static void NovaLandmarks()
        {
            int[] themes={1,2,4,1,1,3,1,3};var functions=new[]{FacilityFunction.Ferry,FacilityFunction.Market,FacilityFunction.Clinic,FacilityFunction.Archive,FacilityFunction.Registry,FacilityFunction.Power,FacilityFunction.Airport,FacilityFunction.Power};
            for(int i=0;i<8;i++)
            {
                if(i==5)continue;
                var at=NeonHarbor.Sites[i]+new Vector3(0,-.05f,28);var p=NovaPublic(NeonHarbor.Names[i],at,78,48,i==3?7:i==4?5:3,themes[i],functions[i],24+i*3%24);novaOccupied.Add(at);
                if(i==0){var f=p.gameObject.AddComponent<ExpansionService>();f.facility=14;}
                if(i==3||i==4)
                {
                    for(int s=-1;s<=1;s+=2)NovaTower(at+new Vector3(s*65,0,25),38,42,i==3?196:118,i+1);
                    Box(p,"Landmark crown",new Vector3(0,(i==3?7:5)*4.2f+4,0),new Vector3(88,4,50),"NovaPorcelain",false);
                }
                if(i==1)for(int j=0;j<14;j++)NovaMarket(at+new Vector3(-70+j%7*21,0,-60-j/7*20),j);
                if(i==6)
                {
                    Box(root,"Nova runway",new Vector3(1680,.07f,-4070),new Vector3(520,.14f,32),"Asphalt");for(int n=0;n<20;n++)Box(root,"Runway center light",new Vector3(1420+n*26,.16f,-4070),new Vector3(8,.02f,.3f),"NovaNeonIce",false);
                    var spawn=Group("Nova jet apron",new Vector3(1860,.2f,-3990));spawn.gameObject.AddComponent<CraftSpawn>().type=CityVehicleType.Airliner;
                }
            }
        }
        static Transform NovaPublic(string name,Vector3 at,float w,float d,int floors,int theme,FacilityFunction function,int role)
        {
            var p=OpenBuilding(root,name,at,w,d,floors,theme);novaBuildings++;novaInteriors++;
            for(int f=0;f<floors;f++)
            {
                Console(p,new Vector3(-w*.5f+4,f*4.2f,-d*.5f+4),function,name+" "+(f+1)+"F");
                Prop(p,new Vector3(-w*.5f+8,f*4.2f,-d*.5f+3),f%3==0?PropUse.Television:f%3==1?PropUse.Water:PropUse.Books);
                for(int j=0;j<6;j++)Box(p,"Layered acoustic wall finish",new Vector3(-w*.5f+.2f,f*4.2f+2,-d*.3f+j*d*.1f),new Vector3(.16f,3.2f,.32f),f%2==0?"NovaCopper":"NovaTeal",false);
            }
            foreach(var seed in p.GetComponentsInChildren<FacilityCrowd>()){seed.arts=System.Array.Empty<string>();seed.jobs=System.Array.Empty<string>();seed.firstRole=role;seed.roleCount=4;}
            for(int s=-1;s<=1;s+=2)
            {
                for(int j=0;j<5;j++)Box(p,"External sun screen fins",new Vector3(s*(w*.5f+.5f),floors*2.1f,-d*.4f+j*d*.2f),new Vector3(.22f,floors*4.2f,1.5f),"NovaPorcelain",false);
                Box(p,"Facade address light",new Vector3(s*(w*.5f-2),floors*2.1f,-d*.5f-.15f),new Vector3(.15f,floors*4.2f,.12f),"NovaNeonViolet",false);
            }
            Crowd(p,name+" 안내광장",new Vector3(-w*.3f,.1f,-d*.5f-5),10,role,4,5);
            return p;
        }
        static void NovaDistricts()
        {
            var random=new System.Random(9128);int serial=0;
            foreach(var land in NeonHarbor.Land)
            {
                for(float z=land.yMin+48;z<land.yMax-45;z+=57)for(float x=land.xMin+42;x<land.xMax-40;x+=55)
                {
                    var at=new Vector3(x+(float)random.NextDouble()*12-6,0,z+(float)random.NextDouble()*12-6);
                    if(novaOccupied.Any(p=>Vector3.Distance(p,at)<100)||at.x>1390&&at.z< -3930)continue;
                    var nearest=ExpansionRoads.Nearest(at,out _,out _);float road=Vector3.Distance(nearest,at);if(road<39)continue;
                    float w=24+random.Next(14),d=24+random.Next(10);serial++;
                    if(serial%19==0&&novaInteriors<34)
                    {
                        int k=serial%6;string[] labels={"CANAL HOUSE / 주거동","NOVA CLINIC / 진료소","LANTERN / 식당","HORIZON / 기술 공방","ECHO / 시민 자료관","VIOLET / 음악 바"};
                        var functions=new[]{FacilityFunction.Home,FacilityFunction.Clinic,FacilityFunction.Cafe,FacilityFunction.Garage,FacilityFunction.Archive,FacilityFunction.Bar};
                        NovaPublic(labels[k],at,34,30,2+serial%3,k==1?4:k==2?2:k==0?0:1,functions[k],24+serial%24);
                        var a=at+Vector3.back*18;var b=ExpansionRoads.Nearest(a,out _,out _);Ribbon(root,"Facility access lane",new[]{a,b},5,0,.065f,"Pavement",true);continue;
                    }
                    float h=serial%11==0?155+random.Next(95):serial%4==0?70+random.Next(60):20+random.Next(46);
                    NovaTower(at,w,d,h,serial);
                    if(serial%4==0)Crowd(root,"노바 골목 주민",at+new Vector3(-w*.5f-3,.1f,0),5,24+serial%24,4,7);
                }
            }
            Debug.Log("NOVA DISTRICT FOOTPRINTS "+serial);
        }
        static void NovaTower(Vector3 at,float w,float d,float h,int serial)
        {
            var p=FutureTower(root,at,w,d,h,serial);p.name="Nova building "+serial+" / "+futureForms[serial%8];novaBuildings++;
        }
        static void NovaMarket(Vector3 at,int serial)
        {
            var p=Group("Canal market stall",at);Box(p,"Shop deck",new Vector3(0,.12f,0),new Vector3(9,.24f,7),"NovaConcrete");
            Box(p,"Vendor back wall",new Vector3(0,1.8f,3.3f),new Vector3(9,3.6f,.2f),"NovaTeal");
            for(int s=-1;s<=1;s+=2)Beam(p,"Market awning support",new Vector3(s*4,0,-3),new Vector3(s*4,3.7f,-3),.1f,"Steel");
            Box(p,"Sloped market canopy",new Vector3(0,3.8f,0),new Vector3(10,.15f,8),serial%2==0?"NovaViolet":"NovaCopper",false);
            Counter(p,new Vector3(0,0,1),serial%2==0);Console(p,new Vector3(-3,0,-2),serial%2==0?FacilityFunction.Cafe:FacilityFunction.Market,"크로마 시장 "+(serial+1));
            for(int j=0;j<4;j++){Box(p,"Produce crate",new Vector3(-3+j*2,.8f,-.8f),new Vector3(1.4f,.35f,1),"Wood");for(int n=0;n<3;n++)Box(p,"Produce",new Vector3(-3+j*2+n*.3f,.99f,-.8f),Vector3.one*.3f,serial%2==0?"PaintAmber":"Leaf",false,PrimitiveType.Sphere);}
            Text(p,serial%2==0?"CHROMA / NIGHT FOOD":"MARINE / SUPPLY",new Vector3(0,3.1f,-3.1f),.25f,"NovaNeonAmber",180);Crowd(p,"크로마 시장",new Vector3(0,.1f,-5),4,28+serial%4,4,3);
        }
        static void NovaSkyRail()
        {
            var loop=new[]{new Vector3(350,18,-2540),new Vector3(1820,18,-2540),new Vector3(1820,18,-3820),new Vector3(350,18,-3820),new Vector3(350,18,-2540)};
            Ribbon(root,"Skyrail guideway",loop,5,0,0,"NovaConcrete",true);
            for(int s=-1;s<=1;s+=2)Ribbon(root,"Skyrail running rail",loop,.18f,s*1.5f,.18f,"Aluminium");
            for(int j=1;j<loop.Length;j++)for(float t=0;t<1;t+=.035f){var at=Vector3.Lerp(loop[j-1],loop[j],t);if(NeonHarbor.OnIsland(at))Beam(root,"Skyrail pillar",new Vector3(at.x,0,at.z),at,.85f,"NovaConcrete",true);}
            var g=Group("Automated elevated transit",Vector3.zero);var train=g.gameObject.AddComponent<NeonTransit>();train.path=loop;
            for(int i=0;i<3;i++){var carriage=Group("Rail carriage",new Vector3(0,0,-i*11),g);Box(carriage,"Car shell",Vector3.up*1.7f,new Vector3(3.4f,2.8f,9.8f),"NovaPorcelain",false);for(int s=-1;s<=1;s+=2){Box(carriage,"Continuous passenger window",new Vector3(s*1.72f,2,0),new Vector3(.08f,1.1f,8.4f),"NovaWindow",false);Box(carriage,"Train accent light",new Vector3(s*1.73f,.8f,0),new Vector3(.07f,.1f,9),"NovaNeonViolet",false);}}
            for(int i=0;i<4;i++)
            {
                var p=Group("Skyrail station "+i,loop[i]);Box(p,"Raised station platform",new Vector3(5,-.12f,0),new Vector3(6,.24f,34),"NovaPorcelain");Box(p,"Station roof",new Vector3(4,5,0),new Vector3(11,.2f,36),"NovaTeal",false);
                var tower=OpenBuilding(root,"스카이레일 환승탑",new Vector3(loop[i].x+24,0,loop[i].z),28,28,5,1);novaInteriors++;novaBuildings++;
                Ribbon(root,"Upper station footbridge",new[]{tower.position+new Vector3(-14,16.8f,-7),loop[i]+new Vector3(7,0,-7)},4,0,0,"NovaPorcelain",true);Console(tower,new Vector3(-5,0,-8),FacilityFunction.Office,"교통 안내 및 업무");
            }
        }
        static void NovaOldCityInfill()
        {
            var old=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/AfterlightExpansion"));var mobility=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/MobilityDistricts"));var civic=Object.Instantiate(Resources.Load<GameObject>("WorldAssets/CivicRenewal"));Physics.SyncTransforms();int n=0;
            for(int roadIndex=0;roadIndex<17;roadIndex++)
            {
                var road=ExpansionRoads.Roads[roadIndex];for(int i=5;i<road.Length-5;i+=7)
                {
                    var tangent=(road[i+1]-road[i-1]).normalized;
                    for(int s=-1;s<=1;s+=2)
                    {
                        var at=road[i]+Vector3.Cross(Vector3.up,tangent)*s*44;at.y=0;
                        if(at.x<805||at.x>2130||at.z>695||at.z<OceanLife.Shore(at.x)+35||Physics.CheckBox(at+Vector3.up*12,new Vector3(18,11,18),Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                        if(Vector3.Distance(ExpansionRoads.Nearest(at,out _,out _),at)<34)continue;
                        NovaTower(at,26,27,18+n%6*7,1000+n);if(n%5==0)Crowd(root,"애프터라이트 확장 시민",at+new Vector3(0,.1f,-17),6,24+n%24,4,5);n++;Physics.SyncTransforms();
                    }
                }
            }
            Object.DestroyImmediate(old);Object.DestroyImmediate(mobility);Object.DestroyImmediate(civic);Debug.Log("AFTERLIGHT DENSITY INFILL "+n);
        }
        static void NovaReefs()
        {
            // Hand-authored exploration landmarks surrounded by sampled reefs and kelp.
            var p=Group("ECHO / submerged relay archive",new Vector3(980,-28,-2180));Box(p,"Archive ballast platform",new Vector3(0,-1,0),new Vector3(28,2,20),"NovaConcrete");
            for(int s=-1;s<=1;s+=2){Box(p,"Flooded server wall",new Vector3(s*10,2,0),new Vector3(3,6,17),"Steel");for(int i=0;i<6;i++)Box(p,"Server status lights",new Vector3(s*8.4f,2,-6+i*2.4f),new Vector3(.1f,.3f,.8f),"NovaNeonIce",false);}
            for(int i=0;i<6;i++)Beam(p,"Broken observation arch",new Vector3(-8,0,-8+i*3),new Vector3(-8,8,-8+i*3),.24f,"Steel");
            var random=new System.Random(2109);
            for(int i=0;i<300;i++)
            {
                float x=140+(float)random.NextDouble()*1900,z=-1100-(float)random.NextDouble()*3200;if(NeonHarbor.OnIsland(new Vector3(x,0,z)))continue;
                float y=OceanLife.Bed(x,z);var reef=Group("Pelagic reef colony",new Vector3(x,y,z));
                for(int n=0;n<4;n++)
                {
                    var at=new Vector3(n*1.4f,1+(float)random.NextDouble()*2,0);Box(reef,"Coral limestone",at,new Vector3(3,2.3f,2),n%2==0?"NovaConcrete":"CargoRed",false,PrimitiveType.Sphere);
                    for(int k=0;k<3;k++)Beam(reef,"Branching coral",at,new Vector3(at.x+(k-1)*1.3f,at.y+2.8f,1),.13f,"CargoGold");
                }
                if(i%2==0)
                {
                    var kelp=Group("Kelp current animation",Vector3.zero,reef);kelp.gameObject.AddComponent<KelpCurrent>().phase=i*.7f;
                    for(int j=0;j<5;j++){float h=4+j*.8f;Beam(kelp,"Kelp stem",new Vector3(j*.6f,0,0),new Vector3(j*.6f,h,0),.05f,"Leaf");for(int k=1;k<6;k++)Box(kelp,"Kelp frond",new Vector3(j*.6f+Mathf.Sin(k)*.5f,h*k/6,0),new Vector3(1.2f,.08f,.45f),"Leaf",false,PrimitiveType.Sphere);}
                }
            }
        }
    }
}
