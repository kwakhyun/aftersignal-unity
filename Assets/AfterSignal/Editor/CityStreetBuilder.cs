using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void RoadMarkMaterials()
        {
            CreateMaterial("VehicleAlloy","#8b9a9b",.15f,.28f);
            CreateMaterial("TrafficRed","#ef4145",0,.2f,2);CreateMaterial("TrafficAmber","#efb643",0,.2f,2);CreateMaterial("TrafficGreen","#38ddba",0,.2f,2);
            foreach(string kind in new[]{"Crosswalk","Line","Arrow"}){
                var tex=new Texture2D(256,128,TextureFormat.RGBA32,true);for(int y=0;y<128;y++)for(int x=0;x<256;x++){bool white=kind=="Crosswalk"?x%26<14:kind=="Line"?y>48&&y<80:(y>52&&y<76&&x>24&&x<205)||(x>=148&&x<=230&&Mathf.Abs(y-64)<(230-x)*.58f);tex.SetPixel(x,y,white?new Color(.84f,.87f,.82f,1):Color.clear);}tex.Apply();string path=resourceRoot+"Art/Urban/Mark-"+kind+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
                var m=CreateMaterial("RoadMark"+kind,"#ffffff",0,.1f);m.shader=Shader.Find("Universal Render Pipeline/Unlit");m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;
            }
            var smoke=CreateMaterial("VehicleSmoke","#383e40",0,0);smoke.shader=Shader.Find("AfterSignal/Vehicle Smoke");smoke.SetFloat("_Surface",1);smoke.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);smoke.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);smoke.SetFloat("_ZWrite",0);smoke.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");smoke.renderQueue=3000;
        }
        static void RoadDecal(string kind,Vector3 p,float w,float h,float yaw=0)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="ROAD IMAGE / "+kind;go.transform.SetParent(world);go.transform.position=p;go.transform.rotation=Quaternion.Euler(90,yaw,0);go.transform.localScale=new Vector3(w,h,1);Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<Renderer>();r.sharedMaterial=Mat("RoadMark"+kind);r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        static void BuildRoadNetwork()
        {
            for(int c=0;c<6;c++){float x=40+c*140;Box("North south avenue",x,.016f,0,22,.03f,640,"UrbanRoad");
                for(int r=0;r<4;r++){float z=-210+r*140;foreach(float dx in new[]{-13f,13f})Box("Continuous sidewalk kerb",x+dx,.11f,z,.22f,.18f,100,"Concrete");
                    for(int k=0;k<11;k++)RoadDecal("Line",new Vector3(x,.09f,z-45+k*9),4,.4f,90);RoadDecal("Arrow",new Vector3(x+5,.095f,z),4,2,-90);RoadDecal("Arrow",new Vector3(x-5,.095f,z),4,2,90);}
            }
            for(int r=0;r<5;r++){float z=-280+r*140;Box("East west avenue",390,.035f,z,746,.03f,22,"UrbanRoad");
                for(int c=0;c<5;c++){float x=110+c*140;foreach(float dz in new[]{-13f,13f})Box("Continuous sidewalk kerb",x,.11f,z+dz,100,.18f,.22f,"Concrete");
                    for(int k=0;k<11;k++)RoadDecal("Line",new Vector3(x-45+k*9,.09f,z),4,.4f);RoadDecal("Arrow",new Vector3(x,.095f,z-5),4,2);RoadDecal("Arrow",new Vector3(x,.095f,z+5),4,2,180);}
                for(int c=0;c<6;c++){float x=40+c*140;foreach(float sign in new[]{-1f,1f}){RoadDecal("Crosswalk",new Vector3(x,.105f,z+sign*16),22,4);RoadDecal("Crosswalk",new Vector3(x+sign*16,.105f,z),22,4,90);
                        RoadDecal("Line",new Vector3(x+sign*5.5f,.11f,z-sign*21),10,.8f);RoadDecal("Line",new Vector3(x-sign*21,.11f,z-sign*5.5f),10,.8f,90);}
                    TrafficSignalAt(x,z);}
            }
        }
        static void TrafficSignalAt(float x,float z)
        {
            var root=new GameObject("SIGNAL / protected crossing").transform;root.SetParent(world);var controller=root.gameObject.AddComponent<CityTrafficSignal>();var ew=new List<Renderer>();var ns=new List<Renderer>();
            for(int side=0;side<4;side++){float yaw=side*90;var rot=Quaternion.Euler(0,yaw,0);var corner=new Vector3(x,0,z)+rot*new Vector3(18,0,-19);
                Box("Signal mast",corner.x,3,corner.z,.17f,6,.17f,"Metal",false,root);var board=Box("Traffic signal housing",corner.x,5.8f,corner.z,.6f,1.9f,.4f,"DarkMetal",false,root);board.transform.rotation=rot;
                for(int lamp=0;lamp<3;lamp++){var p=corner+Vector3.up*(6.4f-lamp*.55f)+rot*Vector3.back*.23f;var bulb=Box("Signal aspect",p.x,p.y,p.z,.34f,.34f,.05f,"Rubber",false,root);bulb.transform.rotation=rot;(side%2==0?ns:ew).Add(bulb.GetComponent<Renderer>());}
            }
            controller.eastWest=ew.ToArray();controller.northSouth=ns.ToArray();var walk=Box("Pedestrian signal",x-18,2.5f,z-19,.6f,.65f,.1f,"TrafficRed",false,root);controller.walkLamp=walk.GetComponent<Renderer>();
            var label=new GameObject("Pedestrian state",typeof(TextMesh));label.transform.SetParent(root);label.transform.position=new Vector3(x-18,3.2f,z-19.1f);var text=label.GetComponent<TextMesh>();text.anchor=TextAnchor.MiddleCenter;text.characterSize=.12f;text.fontSize=36;text.color=new Color(.65f,.9f,.86f);controller.walkLabel=text;
        }
        static void BuildDenseSkyline()
        {
            // Forty rear towers complete every block while keeping forecourts and mission doors intact.
            for(int id=0;id<40;id++){var p=UrbanCatalog.Center(id)+Vector3.forward*43;Tower(p,34,18,48+(id*17%65),id,true);}
            // All four horizon edges: layered silhouettes, illuminated facades and rooftop crowns.
            for(int layer=0;layer<2;layer++){
                float edge=330+layer*58;
                for(int i=0;i<18;i++){Tower(new Vector3(-70+i*54,0,edge),42,30,65+(i*29+layer*17)%95,i+layer*18,false);Tower(new Vector3(-70+i*54,0,-edge),42,30,55+(i*23+layer*21)%90,i+7,false);}
                for(int i=0;i<13;i++){Tower(new Vector3(-layer*58,0,-315+i*54),32,42,70+(i*31)%100,i+3,false);Tower(new Vector3(790+layer*58,0,-315+i*54),32,42,65+(i*27)%90,i+10,false);}
            }
        }
        static void Tower(Vector3 p,float width,float depth,float height,int style,bool solid)
        {
            var root=new GameObject("SKYLINE / tower "+style).transform;root.SetParent(world);
            bool blocks=solid||(p.x+width*.5f>1&&p.x-width*.5f<789&&p.z+depth*.5f>-329&&p.z-depth*.5f<329);
            Box("Tower podium",p.x,3,p.z,width+1,6,depth+1,"UrbanWall",blocks,root);
            Box("Tower core",p.x,height*.5f,p.z,width,height,depth,"UrbanWall",blocks,root);
            UrbanFace(style%2==0?2:3,new Vector3(p.x,3,p.z-depth*.5f-.54f),width,6,Quaternion.identity,root);
            UrbanFace(style%2==0?3:2,new Vector3(p.x+width*.5f+.54f,3,p.z),depth,6,Quaternion.Euler(0,-90,0),root);
            UrbanFace(style%2==0?2:3,new Vector3(p.x-width*.5f-.54f,3,p.z),depth,6,Quaternion.Euler(0,90,0),root);
            UrbanFace(style%2==0?3:2,new Vector3(p.x,3,p.z+depth*.5f+.54f),width,6,Quaternion.Euler(0,180,0),root);
            for(int tier=0;tier<Mathf.CeilToInt(height/13);tier++){float y=6+tier*13,h=Mathf.Min(13,height-y);if(h<=0)break;
                UrbanFace(style%2==0?0:5,new Vector3(p.x,y+h*.5f,p.z-depth*.5f-.03f),width,h,Quaternion.identity,root);
                UrbanFace(style%2==0?5:0,new Vector3(p.x-width*.5f-.03f,y+h*.5f,p.z),depth,h,Quaternion.Euler(0,90,0),root);
                UrbanFace(style%2==0?0:5,new Vector3(p.x+width*.5f+.03f,y+h*.5f,p.z),depth,h,Quaternion.Euler(0,-90,0),root);
                UrbanFace(style%2==0?5:0,new Vector3(p.x,y+h*.5f,p.z+depth*.5f+.03f),width,h,Quaternion.Euler(0,180,0),root);
                Box("Floor belt",p.x,y,p.z,width+.3f,.35f,depth+.3f,"DarkMetal",false,root);
            }
            string accent=style%3==0?"NeonHoney":style%3==1?"NeonAzure":"NeonRose";
            foreach(float side in new[]{-1f,1f})Box("Vertical light seam",p.x+side*(width*.5f-.4f),height*.5f,p.z-depth*.5f-.07f,.12f,height-2,.1f,accent,false,root);
            Box("Crown setback",p.x,height+2,p.z,width*.7f,4,depth*.72f,"DarkMetal",false,root);Box("Crown light",p.x,height+4.1f,p.z,width*.72f,.15f,depth*.74f,accent,false,root);
            Box("Aerial",p.x+width*.2f,height+7,p.z,.1f,6,.1f,"Metal",false,root);
            if(solid){var cut=root.gameObject.AddComponent<CityBuildingCutaway>();cut.center=p;cut.footprint=new Vector2(width,depth);cut.upper=root.GetComponentsInChildren<Renderer>();}
        }
    }
}
