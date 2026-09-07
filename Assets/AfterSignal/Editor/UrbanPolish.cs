using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void UrbanSurfaceMaterials()
        {
            for(int i=0;i<6;i++){
                string texPath=resourceRoot+"Art/Urban/Surface-"+i.ToString("00")+".png";var importer=AssetImporter.GetAtPath(texPath) as TextureImporter;if(importer){importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.SaveAndReimport();}
                string name=i==0?"UrbanRoad":i==1?"UrbanWalk":"UrbanSurface"+i;var m=CreateMaterial(name,"#ffffff",0,.18f);m.shader=Shader.Find("AfterSignal/Urban Surface");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));m.SetFloat("_Density",i==1?.24f:i==4?.35f:.2f);EditorUtility.SetDirty(m);
            }
        }
        static void UrbanStreetLife()
        {
            for(int row=0;row<5;row++)for(int column=0;column<5;column++){
                float x=100+column*140,z=-260+row*140;
                Box("Bus shelter rear",x,1.6f,z,7.4f,3.2f,.12f,"DistrictWindow");Box("Shelter roof",x,3.3f,z-1.1f,8,.15f,3,"Metal");foreach(float dx in new[]{-3.6f,3.6f})Box("Shelter post",x+dx,1.6f,z,.1f,3.2f,.1f,"Chrome");Bench(x,0,z-1);UrbanPlate((row+column)%2==0?15:7,new Vector3(x+6,2,z),4.3f,.75f);
                Box("Street bollard",x+10,.5f,z-1,.35f,1,.35f,"Metal",true);Box("Bollard reflector",x+10,.8f,z-1,.38f,.08f,.38f,"Amber");
                Box("Planter trough",x-10,.5f,z,3.4f,1,2.6f,"UrbanBrick",true);Cylinder("Tree trunk",new Vector3(x-10,2,z),new Vector3(.25f,1.6f,.25f),"WarmWood");
                var leaf=GameObject.CreatePrimitive(PrimitiveType.Quad);leaf.name="City tree / authored foliage";leaf.transform.SetParent(world);leaf.transform.position=new Vector3(x-10,3.8f,z);leaf.transform.localScale=new Vector3(6.2f,5,1);Object.DestroyImmediate(leaf.GetComponent<Collider>());leaf.GetComponent<Renderer>().sharedMaterial=Mat("CivicFoliage");leaf.AddComponent<FoliageOcclusion>();
                Box("Roadside neon channel",x,.07f,z-3.4f,16,.03f,.06f,column%2==0?"NeonHoney":"NeonAzure");
            }
        }
        static void PolishUrbanInterior(int kind)
        {
            Box("Authored interior floor",29,.018f,-4,66,.03f,46,kind==4?"UrbanSurface5":kind==0||kind==5||kind==9||kind==15?"UrbanSurface4":"UrbanWalk");
            Box("Plaster wall finish",29,4.4f,18.72f,65,8.7f,.06f,"UrbanSurface2");
            for(int i=0;i<6;i++){float x=2+i*11;Box("Wall pilaster",x,4.4f,18.5f,.25f,8.8f,.4f,"CivicBronze");Box("Ceiling beam",x,8.4f,1,.25f,.25f,35,"DarkMetal");}
            Box("Wall skirting",29,.2f,18.5f,65,.4f,.2f,"WarmWood");
            foreach(var renderer in world.GetComponentsInChildren<MeshRenderer>()){
                if(renderer.name=="Reception counter"||renderer.name=="Desk top"||renderer.name=="Shop shelf")renderer.sharedMaterial=Mat("WarmWood");
                if(renderer.name=="Pillow"||renderer.name=="Mattress")renderer.sharedMaterial=Mat("Enamel");
            }
            if(kind==0){FurnitureFace("Wardrobe",new Vector3(53,1.6f,11.95f),2.9f,3.1f,Quaternion.identity);FurnitureFace("Refrigerator",new Vector3(48,1.4f,12.15f),1.9f,2.7f,Quaternion.identity);FurnitureFace("Quilt",new Vector3(26.5f,.9f,11),2.2f,2.15f,Quaternion.Euler(90,0,0));}
            if(kind==5){for(int i=0;i<4;i++){float x=22+i*8;Box("Library bookcase",x,2,16,4,4,1.5f,"WarmWood",true);for(int shelf=0;shelf<4;shelf++)for(int b=0;b<9;b++)Box("Library book",x-1.7f+b*.4f,.7f+shelf*.85f,15.15f,.3f,.65f,.35f,(b+shelf)%3==0?"DistrictRed":"Enamel");}}
            foreach(float x in new[]{2f,12f,46f,56f}){Box("Room window frame",x,4.7f,18.45f,6,4.4f,.2f,"WarmWood");Box("Room window",x,4.7f,18.32f,5.6f,4.05f,.04f,"DistrictWindow");Box("Window mullion",x,4.7f,18.26f,.08f,4.1f,.07f,"Chrome");}
            foreach(float x in new[]{18f,40f})LightAt(new Vector3(x,5,-3),kind==4?new Color(.77f,.9f,1):new Color(1,.74f,.44f),38,16);
        }
    }
}
