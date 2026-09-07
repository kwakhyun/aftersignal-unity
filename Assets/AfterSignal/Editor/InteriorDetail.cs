using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void InteriorMaterials()
        {
            CreateMaterial("Enamel","#afb5a6",.08f,.27f);CreateMaterial("WarmWood","#604832",.02f,.22f);CreateMaterial("SoftCloth","#426068",0,.05f);CreateMaterial("Plaster","#514b3e",0,.16f);CreateMaterial("WindowBounce","#c7a263",0,.1f,.25f);
            for(int i=0;i<4;i++)CreateMaterial("HomeWood"+i,new[]{"#584636","#62513e","#6a5744","#514437"}[i],0,.15f);
            string cookiePath=resourceRoot+"Materials/WindowBlinds.png";
            if(!System.IO.File.Exists(cookiePath)){var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++)pixels[y*64+x]=x>5&&x<58&&y>5&&y<58&&y%7<4?Color.white:Color.black;tex.SetPixels(pixels);tex.Apply();System.IO.File.WriteAllBytes(cookiePath,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(cookiePath);}
            foreach(string name in new[]{"Wardrobe","Refrigerator","Quilt","Cabinet"}){
                string path=resourceRoot+"Materials/Furniture_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Art/Environment/Furniture/"+name+".png"));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.15f);m.SetFloat("_Metallic",0);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.085f,.074f,.059f));m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));EditorUtility.SetDirty(m);
            }
        }
        static void FurnitureFace(string material,Vector3 pos,float w,float h,Quaternion rotation)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Surface / "+material;go.transform.SetParent(world,false);go.transform.position=pos;go.transform.rotation=rotation;go.transform.localScale=new Vector3(w,h,1);Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Mat("Furniture_"+material);r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void ResidenceDetail()
        {
            // Extending the visible floor removes the empty black band without moving simulation coordinates.
            Box("Room foreground floor",12.4f,21.7f,-10,31,.6f,8,"WarmWood",true);Box("Door wall foreground",28,24.1f,-10,.35f,4.2f,8,"Plaster",true);Box("Room left return",-3,25,-9,.4f,6,6,"Plaster",true);
            Box("Room ceiling cutaway",12.3f,30,4,31,.25f,5,"DarkMetal");
            foreach(var r in world.GetComponentsInChildren<MeshRenderer>()){
                string n=r.name;if(n.StartsWith("Wardrobe")||n=="Bed headboard"||n=="Desk top"||n=="Kitchen counter")r.sharedMaterial=Mat("WarmWood");if(n.StartsWith("Fridge")||n=="Refrigerator body"||n=="Kitchen worktop")r.sharedMaterial=Mat("Enamel");if(n=="Folded blanket")r.sharedMaterial=Mat("SoftCloth");
            }
            for(int row=0;row<38;row++)for(int col=0;col<10;col++)Box("Apartment oak plank",-1.4f+col*3.1f,22.016f,-13.5f+row*.5f,3.085f,.024f,.488f,"HomeWood"+((row*7+col*3)%4));
            for(int i=world.childCount-1;i>=0;i--){var t=world.GetChild(i);if(t.name=="Path edge"&&t.position.x<28&&t.position.y>21)Object.DestroyImmediate(t.gameObject);}
            FurnitureFace("Wardrobe",new Vector3(-1,23.7f,2.75f),2.38f,3.22f,Quaternion.identity);FurnitureFace("Refrigerator",new Vector3(23,23.45f,3.36f),1.65f,2.8f,Quaternion.identity);
            FurnitureFace("Quilt",new Vector3(6.5f,22.897f,3.3f),2.18f,2.15f,Quaternion.Euler(90,0,0));FurnitureFace("Quilt",new Vector3(6.5f,22.72f,2.18f),2.18f,.32f,Quaternion.identity);
            Box("Desk drawer pedestal",15.25f,22.42f,3.7f,1.2f,.84f,1.2f,"WarmWood",true);FurnitureFace("Cabinet",new Vector3(15.25f,22.44f,3.08f),1.17f,.8f,Quaternion.identity);
            Box("Bedside cabinet",3,22.42f,3.2f,1.4f,.84f,1.25f,"WarmWood",true);FurnitureFace("Cabinet",new Vector3(3,22.43f,2.55f),1.36f,.81f,Quaternion.identity);
            Cylinder("Bedside lamp base",new Vector3(3,22.94f,3.2f),new Vector3(.48f,.06f,.48f),"Gold");Cylinder("Bedside lamp stem",new Vector3(3,23.27f,3.2f),new Vector3(.065f,.28f,.065f),"Gold");Cylinder("Bedside lampshade",new Vector3(3,23.62f,3.2f),new Vector3(.7f,.25f,.7f),"DistrictLight");LightAt(new Vector3(3,23.5f,2.7f),new Color(1,.62f,.3f),12,5);
            Box("Shelving upright",10,24.1f,5.35f,.13f,4.2f,.9f,"WarmWood");Box("Shelving upright",12,24.1f,5.35f,.13f,4.2f,.9f,"WarmWood");for(int row=0;row<4;row++){float y=22.5f+row*1.05f;Box("Book shelf",11,y,5.35f,2.2f,.09f,1,"WarmWood");for(int i=0;i<7;i++){float h=.45f+(i%3)*.13f;Box("Personal book",10.22f+i*.23f,y+h*.5f+.07f,5.25f,.18f,h,.65f,i%3==0?"DistrictRed":i%3==1?"Seat":"Gold");}}
            for(int i=0;i<5;i++){float x=17+i*.45f;Box("Kitchen utensil hook",x,24.3f,5.3f,.05f,.32f,.07f,"Chrome");Cylinder("Hanging pan",new Vector3(x,23.94f,5.25f),new Vector3(.35f,.035f,.35f),"DarkMetal").transform.rotation=Quaternion.Euler(90,0,0);}
            Box("Cooktop",20,23.34f,4.3f,1.2f,.08f,.85f,"DarkMetal");Cylinder("Cooking pot",new Vector3(20,23.61f,4.3f),new Vector3(.58f,.23f,.58f),"Chrome");Box("Chopping board",18.5f,23.35f,4.15f,.7f,.05f,.85f,"WarmWood");
            Box("Floor storage trunk",19.5f,22.38f,-4.5f,3,.75f,1.4f,"Seat",true);FurnitureFace("Cabinet",new Vector3(19.5f,22.4f,-5.22f),2.9f,.7f,Quaternion.identity);
            Table(6,22,-7.8f,4,1.8f);Box("Open journal",5.6f,22.96f,-7.8f,.8f,.06f,.65f,"LightTile");Cylinder("Tea cup",new Vector3(7,23.12f,-7.8f),new Vector3(.24f,.17f,.24f),"Enamel");
            // Directional window light is represented by a real spot and restrained floor bounce strips.
            var go=new GameObject("Window / warm key");go.transform.SetParent(world);go.transform.position=new Vector3(7,27,4.8f);go.transform.rotation=Quaternion.LookRotation(new Vector3(.12f,-.8f,-1));var light=go.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.73f,.43f);light.intensity=95;light.range=16;light.spotAngle=85;light.innerSpotAngle=50;light.shadows=LightShadows.Soft;light.shadowStrength=.45f;
            light.cookie=AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Materials/WindowBlinds.png");
            LightAt(new Vector3(14,25,-2),new Color(1,.8f,.56f),35,13);LightAt(new Vector3(23,25,-1),new Color(.69f,.8f,.84f),22,10);
        }
    }
}
