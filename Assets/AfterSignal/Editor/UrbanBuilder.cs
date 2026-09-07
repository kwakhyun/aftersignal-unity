using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/Urban/Build city and release")]
        public static void UrbanAndRelease(){BuildUrban();BuildRelease();}
        public static void CityOnlyAndRelease(){AssetDatabase.Refresh();BuildScene(StageId.UrbanCity,AssetDatabase.LoadAssetAtPath<GameTuning>(resourceRoot+"GameTuning.asset"));OptimizeAllScenes();AssetDatabase.SaveAssets();BuildRelease();}
        public static void BuildUrban()
        {
            AssetDatabase.Refresh();DistrictMaterials();InteriorMaterials();CityMaterials();UrbanMaterials();
            var tuning=AssetDatabase.LoadAssetAtPath<GameTuning>(resourceRoot+"GameTuning.asset");
            BuildScene(StageId.Haven,tuning);NeonArchitecture(StageId.Haven,226);InstallUrbanHaven();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            BuildScene(StageId.UrbanCity,tuning);BuildScene(StageId.UrbanInterior,tuning);
            var scenes=new EditorBuildSettingsScene[CampaignRules.Scenes.Length];for(int i=0;i<scenes.Length;i++)scenes[i]=new EditorBuildSettingsScene("Assets/AfterSignal/Scenes/"+CampaignRules.Scenes[i]+".unity",true);EditorBuildSettings.scenes=scenes;
            for(int i=0;i<23;i++){var scene=EditorSceneManager.OpenScene(scenes[i].path);world=GameObject.Find("WORLD / editable architecture").transform;ReplaceSignImages();if(i!=3)NeonArchitecture((StageId)i,Object.FindAnyObjectByType<GameDirector>().stageLength);EditorSceneManager.SaveScene(scene);}
            OptimizeAllScenes();AssetDatabase.SaveAssets();Debug.Log("URBAN READY: 40 enterable sites / 16 interior types / 25 preserved and appended scenes.");
        }
        static void UrbanMaterials()
        {
            Directory.CreateDirectory(resourceRoot+"Geometry");
            for(int i=0;i<24;i++){bool sign=i<16;string name=(sign?"Sign-":"Facade-")+(sign?i:i-16).ToString("00");string texPath=resourceRoot+"Art/Urban/"+name+".png";
                var importer=AssetImporter.GetAtPath(texPath) as TextureImporter;if(importer!=null){importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();}
                string path=resourceRoot+"Materials/Urban-"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.14f);m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",tex);m.SetColor("_EmissionColor",Color.white*(sign?.85f:.24f));EditorUtility.SetDirty(m);
            }
            CreateMaterial("UrbanRoad","#18272c",.12f,.27f);CreateMaterial("UrbanWalk","#515459",.06f,.16f);CreateMaterial("UrbanWall","#26333d",.1f,.2f);CreateMaterial("UrbanBrick","#483743",.03f,.12f);
            UrbanSurfaceMaterials();RoadMarkMaterials();
            CreateMaterial("NeonAzure","#35cfea",0,.22f,2);CreateMaterial("NeonRose","#ed4dbe",0,.22f,1.8f);CreateMaterial("NeonHoney","#efb452",0,.22f,1.6f);
        }
        static int UrbanSignIndex(string title)
        {
            if(title.Contains("←")||title.Contains("→")||title.Contains(" E")||title.Contains("E /"))return -1;
            if(title.Contains("주유"))return 11;if(title.Contains("소방"))return 2;if(title.Contains("경찰"))return 1;if(title.Contains("은행"))return 3;if(title.Contains("백화"))return 6;if(title.Contains("마트")||title.Contains("MARKET")||title.Contains("시장"))return 7;
            if(title.Contains("옷")||title.Contains("수선"))return 8;if(title.Contains("식탁")||title.Contains("식당"))return 9;if(title.Contains("병원")||title.Contains("온유")||title.Contains("CARE"))return 4;if(title.Contains("학교")||title.Contains("SCHOOL"))return 5;
            if(title.Contains("본부")||title.Contains("AFTER / SIGNAL"))return 14;if(title.Contains("주거")||title.Contains("아파트"))return 0;if(title.Contains("중앙역")||title.Contains("CENTRAL"))return 10;if(title.Contains("주차"))return 12;if(title.Contains("상회")||title.Contains("카페"))return 15;return -1;
        }
        static void UrbanPlate(int index,Vector3 p,float w,float h,Transform parent=null)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="IMAGE SIGN / "+UrbanCatalog.Names[index];go.transform.SetParent(parent?parent:world,false);go.transform.position=p+Vector3.back*.02f;go.transform.localScale=new Vector3(w,h,1);Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Mat("Urban-Sign-"+index.ToString("00"));go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        static void ReplaceSignImages()
        {
            foreach(var point in Object.FindObjectsByType<InteractionPoint>()){point.title=point.title.Replace("새벽 주거동","새벽아파트").Replace("애프터라이트 학교","새봄초등학교").Replace("시민 병원","온유병원").Replace("신호 복원 본부","신호복원본부").Replace("의료원 소은","간호사 소은").Replace("소은 의료원","간호사 소은");}
            foreach(var text in Object.FindObjectsByType<TextMesh>()){int kind=UrbanSignIndex(text.text);if(kind<0)continue;var p=text.transform.position;float w=Mathf.Clamp(text.GetComponent<Renderer>().bounds.size.x+1,3,15);UrbanPlate(kind,p,w,w/5.7f,text.transform.parent);Object.DestroyImmediate(text.gameObject);}
        }
        static void InstallUrbanHaven()
        {
            foreach(var walker in Object.FindObjectsByType<ResidentWalker>())if(walker.walking)Object.DestroyImmediate(walker.gameObject);
            var simulation=new GameObject("URBAN / traffic and population").AddComponent<UrbanSimulation>();simulation.vehiclePrefab=MakeVehiclePrefab();simulation.vehiclePrefabs=MakeVehicleVariants(simulation.vehiclePrefab);simulation.gameObject.AddComponent<CityPopulation>();
            var point=Interact(InteractionKind.FacilityTravel,"도시대로 · 애프터라이트 시내",new Vector3(215,1.2f,-7));point.destination=StageId.UrbanCity;point.radius=3;
            Sign("도시대로 →  E",new Vector3(216,3.7f,-5),8,1,"Amber");
        }
        static CityVehicle MakeVehiclePrefab()
        {
            var cars=new List<Transform>();foreach(Transform t in world)if(t.name=="Parked city sedan")cars.Add(t);
            if(cars.Count==0){ParkedSedans();foreach(Transform t in world)if(t.name=="Parked city sedan")cars.Add(t);}
            foreach(var root in cars){foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial==Mat("Chrome"))renderer.sharedMaterial=Mat("VehicleAlloy");if(!root.GetComponent<CityVehicle>())root.gameObject.AddComponent<CityVehicle>();var rb=root.GetComponent<Rigidbody>();if(!rb)rb=root.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;}
            var prefab=PrefabUtility.SaveAsPrefabAsset(cars[0].gameObject,"Assets/AfterSignal/Prefabs/CitySedan.prefab");return prefab.GetComponent<CityVehicle>();
        }
        static void UrbanFace(int index,Vector3 p,float w,float h,Quaternion rotation,Transform parent)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Facade / authored surface";go.transform.SetParent(parent,false);go.transform.position=p;go.transform.rotation=rotation;go.transform.localScale=new Vector3(w,h,1);Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<Renderer>();r.sharedMaterial=Mat("Urban-Facade-"+index.ToString("00"));r.shadowCastingMode=ShadowCastingMode.Off;}
        static void BuildUrbanCity()
        {
            Box("CITY / continuous ground",395,-.25f,0,980,.5f,840,"UrbanWalk",true);
            BuildRoadNetwork();
            for(int id=0;id<UrbanCatalog.SiteCount;id++)BuildUrbanSite(id);UrbanStreetLife();BuildDenseSkyline();
            for(int c=0;c<6;c++)for(int r=0;r<9;r++){float x=23+c*140,z=-305+r*72;Box("Road light pole",x,4.6f,z,.16f,9.2f,.16f,"Metal");Box("Road light arm",x+1.8f,9,z,3.8f,.2f,.35f,"DarkMetal");Box("Road light diffuser",x+2.6f,8.86f,z,2,.08f,.35f,"DistrictLight");LightAt(new Vector3(x+2.5f,8.5f,z),new Color(1,.75f,.46f),18,24);}
            // Background towers are outside the traversable perimeter; the 40 street-level sites are enterable.
            for(int i=0;i<14;i++){float x=-30+i*65,h=45+(i%5)*16;Box("Distant city silhouette",x,h*.5f,355,46,h,30,"UrbanWall");UrbanFace(0,new Vector3(x,h*.5f,339.9f),46,h,Quaternion.identity,world);}
            var sim=new GameObject("URBAN / traffic and population").AddComponent<UrbanSimulation>();sim.vehiclePrefab=MakeVehiclePrefab();sim.vehiclePrefabs=MakeVehicleVariants(sim.vehiclePrefab);sim.gameObject.AddComponent<CityPopulation>();
            foreach(var c in Object.FindObjectsByType<CityVehicle>())if(c.transform.position.z==-12.5f)Object.DestroyImmediate(c.gameObject);
            Interact(InteractionKind.UrbanTown,"주거지 연결로 · 기존 마을로",new Vector3(30,1.2f,-299));Sign("주거지 ←  E",new Vector3(28,4,-296),9,1.2f,"Amber");
            CivicNpc("교통 안내원 나린","concierge",new Vector3(46,.06f,-290),"시내에서는 차 옆에서 E로 탑승할 수 있어요. WASD로 운전하고 SPACE로 멈추세요. M을 누르면 시설과 주유소가 표시됩니다. 건물 현관의 E 표시를 따라 들어가 보세요.");
        }
        static void BuildUrbanSite(int id)
        {
            int kind=UrbanCatalog.Kind(id);var p=UrbanCatalog.Center(id);float h=kind==0?34:kind==6?24:kind==3?22:kind==12?8:12;
            int facade=kind==0?0:kind==6||kind==8?1:kind==7?2:kind==9||kind==15?3:kind==1||kind==2?4:kind==3||kind==14?5:kind==10?6:kind==4||kind==5?7:2;
            var building=new GameObject("SITE / "+id+" / "+UrbanCatalog.Name(id)).transform;building.SetParent(world);
            Box("Ground floor mass",p.x,2,p.z,40,4,54,"UrbanWall",true,building);
            var upper=new GameObject("Upper floors / camera cutaway").transform;upper.SetParent(building);var cut=upper.gameObject.AddComponent<CityBuildingCutaway>();cut.center=p;cut.footprint=new Vector2(40,54);
            Box("Upper building volume",p.x,4+(h-4)*.5f,p.z,40,h-4,54,"UrbanSurface2",true,upper);
            UrbanFace(facade,new Vector3(p.x,h*.5f,p.z-27.05f),40,h,Quaternion.identity,upper);
            UrbanFace(facade,new Vector3(p.x-20.05f,h*.5f,p.z),54,h,Quaternion.Euler(0,90,0),upper);
            UrbanFace(facade,new Vector3(p.x+20.05f,h*.5f,p.z),54,h,Quaternion.Euler(0,-90,0),upper);
            UrbanFace(facade,new Vector3(p.x,h*.5f,p.z+27.05f),40,h,Quaternion.Euler(0,180,0),upper);
            Box("Roof parapet",p.x,h+.25f,p.z,41,.5f,55,"UrbanSurface3",false,upper);Box("Roof circuit strip",p.x,h+.53f,p.z-27.6f,39,.08f,.1f,id%3==0?"NeonRose":"NeonAzure",false,upper);
            for(int i=0;i<3;i++){Box("Rooftop air handling",p.x-12+i*10,h+1,p.z+8,4,2,7,"Chrome",false,upper);for(int j=0;j<4;j++)Box("Vent louvers",p.x-12+i*10,h+2.02f,p.z+5+j*1.8f,3.5f,.05f,.12f,"DarkMetal",false,upper);}
            cut.upper=upper.GetComponentsInChildren<Renderer>();
            Box("Entry dark reveal",p.x,1.7f,p.z-27.12f,4.4f,3.4f,.08f,"DistrictWindow",false,building);foreach(float dx in new[]{-2.3f,2.3f})Box("Entry illuminated jamb",p.x+dx,1.8f,p.z-27.3f,.12f,3.6f,.14f,"Cyan",false,building);
            Box("Store awning",p.x,4.2f,p.z-29,24,.2f,4,"UrbanBrick",false,building);UrbanPlate(kind,new Vector3(p.x,5.8f,p.z-29.2f),23,4,building);
            var enter=Interact(InteractionKind.UrbanEnter,UrbanCatalog.Name(id)+" · 들어가기",UrbanCatalog.Door(id)+Vector3.up);enter.siteId=id;enter.radius=3.6f;
            Box("Door approach marker",p.x,.07f,p.z-32,3,.03f,1.2f,"NeonAzure");LightAt(new Vector3(p.x,4,p.z-31),new Color(.5f,.8f,1),9,13);
            for(int i=0;i<3;i++){float x=p.x-14+i*14;Box("Parking bay end",x,.06f,p.z-42,5.8f,.025f,.1f,"DistrictIvory");Box("Parking bay divider",x-3,.06f,p.z-45,.1f,.025f,6,"DistrictIvory");}
            Box("Alley drainage",p.x+23,.08f,p.z,1,.03f,55,"Metal");for(int i=0;i<3;i++){Box("Alley refuse container",p.x+23,.65f,p.z+i*5,2,1.3f,2.4f,"UrbanBrick",true);Box("Container lid",p.x+23,1.35f,p.z+i*5,2.1f,.13f,2.5f,"Metal");}
            if(kind==11){var q=UrbanCatalog.Pump(id);var roof=Box("Fuel forecourt roof",q.x,5,q.z,20,.35f,12,"DistrictIvory");var roofCut=roof.AddComponent<CityBuildingCutaway>();roofCut.center=q;roofCut.footprint=new Vector2(20,12);roofCut.upper=new[]{roof.GetComponent<Renderer>()};foreach(float dx in new[]{-7f,7f}){Box("Fuel canopy pillar",q.x+dx,2.5f,q.z,.35f,5,.35f,"Chrome",true);Box("Fuel dispenser",q.x+dx,1,q.z,1.2f,2,.8f,"DistrictRed",true);Box("Fuel meter",q.x+dx,1.5f,q.z-.43f,.8f,.55f,.04f,"DarkMetal");Box("Fuel nozzle",q.x+dx+.7f,1,q.z,.1f,.9f,.15f,"Rubber");}UrbanPlate(11,new Vector3(q.x,5.2f,q.z-6.05f),18,3);}
            if(kind==12){for(int i=0;i<5;i++)Box("Parking stall",p.x-16+i*8,.075f,p.z-39,.12f,.025f,8,"DistrictIvory");}
        }
        static void BuildUrbanInterior()
        {
            Deck(-3,62,0,1,38);Box("Interior rear masonry",29,4,19,66,8,.4f,"DistrictIvory",true);Box("Interior left wall",-3,3,1,.3f,6,36,"UrbanWall",true);Box("Interior right wall",62,3,1,.3f,6,36,"UrbanWall",true);
            foreach(float x in new[]{8f,28f,48f}){LightAt(new Vector3(x,6,0),new Color(.95f,.8f,.6f),12,18);Box("Interior ceiling light",x,7,3,13,.12f,.5f,"DistrictLight");}
            var exit=Interact(InteractionKind.UrbanExit,"출입문 · 시내로 나가기",new Vector3(5,1.2f,-10));exit.radius=3;Sign("출입문   E",new Vector3(5,3,-8),5,.8f,"Amber");
            var root=new GameObject("INTERIORS / site themes").AddComponent<UrbanInterior>();root.themes=new GameObject[16];var original=world;
            for(int type=0;type<16;type++){
                var theme=new GameObject("Interior / "+UrbanCatalog.Names[type]);theme.transform.SetParent(root.transform);root.themes[type]=theme;world=theme.transform;
                UrbanPlate(type,new Vector3(28,5.8f,18.65f),20,3.5f);UrbanFace(type==0?0:type==1||type==2?4:type==6?1:type==10?6:7,new Vector3(45,4.4f,18.7f),27,8.6f,Quaternion.identity,world);
                Box("Reception counter",12,.65f,8,7,1.3f,2,"DistrictWarm",true);Monitor(12,1.4f,8);CivicNpc(type==1?"민원 담당 이안":type==2?"소방대원 도윤":type==4?"간호사 소은":type==5?"교사 유진":type==14?"연락원 태오":"직원 해솔",type==4?"medic":type==5?"teacher":type==14||type==1?"commander":"concierge",new Vector3(10,.05f,5),UrbanCatalog.Descriptions[type],InteractionKind.UrbanService);
                if(type==0){Bed(26,0,11);Table(41,0,11,5);Monitor(41,1,11);Box("Apartment wardrobe",53,1.6f,13,3,3.2f,2,"DistrictWarm",true);Box("Fridge",48,1.4f,13,2,2.8f,1.6f,"DistrictIvory",true);Box("Living rug",32,.04f,0,16,.03f,7,"Seat");Bench(30,0,3);Table(31,0,0,4);}
                else if(type==4){for(int i=0;i<3;i++){Bed(26+i*11,0,10,true);Monitor(29+i*11,1,13);Box("Medical divider",30+i*11,1.4f,10,.1f,2.8f,5,"DistrictBlue",true);}}
                else if(type==5){for(int i=0;i<9;i++){Table(25+(i%3)*10,0,-2+(i/3)*6,3);CivicChair(25+(i%3)*10,0,-3.5f+(i/3)*6);}Box("Classroom blackboard",38,2.6f,18.6f,14,2.8f,.08f,"Leaf");}
                else if(type==9||type==15){for(int i=0;i<8;i++){float x=25+(i%4)*8,z=-2+i/4*10;Table(x,0,z,3,2);CivicChair(x-1,0,z-2);CivicChair(x+1,0,z+2);Cylinder("Tableware",new Vector3(x,.98f,z),new Vector3(.42f,.06f,.42f),"DistrictIvory");}Box("Restaurant kitchen",42,1,15,25,2,3,"Chrome",true);}
                else if(type==10){for(int i=0;i<4;i++)Bench(25+i*8,0,5);Box("Platform rail",40,-.3f,14,35,.2f,.14f,"Chrome");Box("Subway carriage",42,1.9f,16,28,3.8f,4,"DistrictIvory",true);for(int i=0;i<7;i++)Box("Carriage window",30+i*3.8f,2.1f,13.97f,2.6f,1.4f,.04f,"DistrictWindow");Terminal(InteractionKind.FirstRail,"중앙역 조사 · 열차 임무 시작",53,0,-5);}
                else if(type==14){Table(35,0,3,14,5);Box("Operations screen",35,1.05f,3,12,.07f,4,"DistrictBlue");for(int i=0;i<4;i++)Monitor(24+i*8,.95f,13);Terminal(InteractionKind.FirstRail,"중앙역 조사",28,0,-7);Terminal(InteractionKind.MissionBoard,"도시 복원 임무",40,0,-7);Terminal(InteractionKind.BreachMission,"외벽 기록 회수",51,0,-7);}
                else if(type==1||type==3){for(int i=0;i<4;i++){Table(26+i*8,0,11,5);Monitor(26+i*8,.95f,11);CivicChair(26+i*8,0,9);}Box("Records vault",52,2,15,6,4,4,"Metal",true);for(int i=0;i<4;i++)Box("Queue rail",23+i*8,1,0,.12f,2,5,"Gold");}
                else if(type==2||type==12){for(int i=0;i<4;i++){Box("Garage equipment",25+i*8,1.1f,13,5,2.2f,3,"DistrictRed",true);for(int j=0;j<4;j++)Box("Garage drawer",25+i*8,.5f+j*.45f,11.4f,4,.1f,.06f,"Chrome");}Box("Garage lane",38,.04f,0,32,.025f,8,"RoadSurface");}
                else {for(int aisle=0;aisle<3;aisle++)for(int bay=0;bay<4;bay++){float x=25+bay*8,z=-2+aisle*7;Box("Shop shelf",x,1.1f,z,5,2.2f,1.7f,"DistrictWarm",true);for(int s=0;s<3;s++)for(int k=0;k<4;k++)Box(type==8?"Folded clothing":"Packaged goods",x-1.8f+k*1.2f,.4f+s*.7f,z-.92f,.8f,.4f,.5f,(k+s+type)%3==0?"DistrictRed":(k+s)%2==0?"DistrictIvory":"DistrictBlue");}}
                PolishUrbanInterior(type);theme.SetActive(type==0);
            }world=original;
        }
    }
}
