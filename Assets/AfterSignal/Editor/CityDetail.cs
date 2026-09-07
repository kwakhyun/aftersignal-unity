using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void CityMaterials()
        {
            var ghost=Mat("GhostFX");if(ghost){ghost.SetFloat("_SpriteMode",1);EditorUtility.SetDirty(ghost);}
            CreateMaterial("CivicStone","#77786a",.04f,.24f);CreateMaterial("CivicBronze","#69533c",.4f,.3f);CreateMaterial("RoadSurface","#293331",.05f,.2f);
            foreach(string name in new[]{"Residence","School","Clinic","Headquarters"}){
                string path=resourceRoot+"Materials/Facade_"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Art/Environment/Facades/"+name+".png");m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.13f);m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",tex);m.SetColor("_EmissionColor",new Color(.24f,.22f,.18f));EditorUtility.SetDirty(m);
            }
        }
        static void FacadeSurface(string name,Vector3 p,float w,float h,Quaternion rotation)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Facade detail / "+name;go.transform.SetParent(world,false);go.transform.position=p;go.transform.rotation=rotation;go.transform.localScale=new Vector3(w,h,1);Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Mat("Facade_"+name);r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void CityDetail()
        {
            // Remove only presentation geometry that obscured the entry lane; collision routes are retained.
            for(int i=world.childCount-1;i>=0;i--){var t=world.GetChild(i);if(t.name=="Broad leaf"||t.name=="Plaza planter"||t.name=="Angular autumn canopy"||t.name=="Occupied window"||t.name=="Window frame"||t.name=="Storefront reveal")Object.DestroyImmediate(t.gameObject);}
            Deck(-42,-10,0,9,66);Deck(234,270,0,9,66);
            var colors=new[]{"Residence","School","Clinic","Headquarters"};float[] xs={16,64,113,178},zs={26,30,30,30},ws={26,29,27,32};
            for(int b=0;b<4;b++){
                float x=xs[b],z=zs[b],w=ws[b];FacadeSurface(colors[b],new Vector3(x,13,z-7.38f),w,26,Quaternion.identity);
                FacadeSurface(colors[b],new Vector3(x-w*.5f-.02f,13,z),14,26,Quaternion.Euler(0,90,0));
                FacadeSurface(colors[b],new Vector3(x+w*.5f+.02f,13,z),14,26,Quaternion.Euler(0,-90,0));
                for(int r=0;r<7;r++){Box("Masonry belt course",x,4.2f+r*3.6f,z-7.48f,w,.16f,.43f,"CivicStone");}
                foreach(float dx in new[]{-w*.48f,w*.48f}){Box("Masonry corner pier",x+dx,13,z-7.5f,.5f,26,.5f,"CivicStone");Cylinder("Copper drainpipe",new Vector3(x+dx*.92f,9,z-7.85f),new Vector3(.15f,9,.15f),"CivicBronze");}
                for(int r=0;r<3;r++){float py=7+r*7.2f;Box("Service AC cabinet",x-w*.32f,py,z-7.96f,1.5f,.9f,.8f,"Enamel");for(int f=0;f<6;f++)Box("AC louver",x-w*.32f,py-.32f+f*.13f,z-8.39f,1.28f,.04f,.04f,"DarkMetal");}
                for(int s=-1;s<=1;s+=2){float px=x+s*w*.32f;Box("Store canopy",px,3.6f,z-8,6,.2f,2.3f,s<0?"Seat":"DistrictWarm");Sign(b==0?(s<0?"AFTERLIGHT / 24":"도시 식탁"):b==1?(s<0?"배움의 집":"기억 도서관"):b==2?(s<0?"시민 진료소":"24H / CARE"):(s<0?"기록 보관소":"SIGNAL / HQ"),new Vector3(px,3.85f,z-8.3f),6,.55f,"Amber");
                    Box("Shop entry step",px,.09f,z-8.3f,5,.18f,1.1f,"CivicStone");Box("Shop door rail",px,1.6f,z-7.55f,.08f,3,.13f,"CivicBronze");
                }
                for(int j=0;j<3;j++){float bx=x-w*.28f+j*.75f;Box("Wall mailboxes",bx,1.5f,z-7.8f,.6f,.86f,.3f,"CivicBronze");Box("Mailbox slot",bx,1.7f,z-7.96f,.39f,.07f,.02f,"DarkMetal");}
                LightAt(new Vector3(x,7,z-12),new Color(1,.78f,.51f),42,22);
            }
            // At the far edge, architecture frames the depth streets without closing their entrances.
            for(int i=0;i<8;i++){float x=-25+i*43;Box("Distant civic building",x,17,48,26,34,10,i%2==0?"DistrictStone":"DistrictWarm");FacadeSurface(colors[i%4],new Vector3(x,17,42.97f),26,34,Quaternion.identity);}
            foreach(float z in new[]{9.4f,16.6f,-8.4f,-15.6f}){Box("Avenue curb",111,.12f,z,242,.24f,.26f,"CivicStone");Box("Drain edge",111,.04f,z+.24f,242,.025f,.09f,"CivicBronze");}
            foreach(var r in world.GetComponentsInChildren<MeshRenderer>())if(r.name=="Avenue / asphalt")r.sharedMaterial=Mat("RoadSurface");
            for(int i=0;i<31;i++){float x=-4+i*7.4f;Box("Sidewalk expansion seam",x,.021f,3,.035f,.01f,10,"DarkMetal");if(i%3==0){Box("Street drain",x,.024f,8.9f,1.4f,.012f,.6f,"DarkMetal");for(int j=0;j<9;j++)Box("Drain grate",x-.6f+j*.15f,.037f,8.9f,.065f,.012f,.52f,"CivicBronze");}}
            // Sparse hanging fixtures make the old raised walkways read as working platforms.
            foreach(float x in new[]{26f,36f,47f}){Box("Workshop deck fascia",x,4.45f,2.3f,9,1,.18f,"CivicBronze");Box("Workshop light strip",x,4.7f,2.13f,6,.07f,.035f,"DistrictLight");}
            foreach(float x in new[]{28f,84f,145f,219f}){Cylinder("Bollard",new Vector3(x,.5f,7.8f),new Vector3(.32f,.5f,.32f),"CivicBronze");Box("Bollard marker",x,.86f,7.62f,.24f,.1f,.035f,"DistrictLight");}
            for(int i=0;i<10;i++){float x=5+i*23;TreeCrown(x,8);}
            LightAt(new Vector3(3,7,-1),new Color(1,.77f,.48f),50,24);
        }
        static void TreeCrown(float x,float z)
        {
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(resourceRoot+"Art/Environment/Foliage.png");if(!tex)return;
            string path=resourceRoot+"Materials/CivicFoliage.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",Color.white);m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.35f);m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",.1f);m.SetFloat("_Surface",1);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.renderQueue=3000;
            foreach(float angle in new[]{0f,72f}){var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Autumn / cutout canopy";go.transform.SetParent(world,false);go.transform.position=new Vector3(x,4.2f,z);go.transform.rotation=Quaternion.Euler(0,angle,0);go.transform.localScale=new Vector3(5.2f,4.2f,1);Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=m;go.AddComponent<FoliageOcclusion>();}
            EditorUtility.SetDirty(m);
        }
    }
}
