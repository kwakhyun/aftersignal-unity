using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static readonly string[] futureForms={"Helix","Cascade","Prism","Oval","Cantilever","Lantern","TwinGate","Orbital"};
        static void FutureMaterials()
        {
            Mat("FutureSilver","#819ba3",.78f,.58f);Mat("FutureCeramic","#c7d4cc",.26f,.64f);Mat("FutureCarbon","#18282e",.55f,.55f);Mat("FutureCopper","#927057",.7f,.52f);
            Mat("FutureLight","#83f8e8",.2f,.6f,1.5f);Mat("FutureRose","#cf79ed",.2f,.6f,1.5f);
            var glass=Mat("FutureGlass","#315c70",.75f,.88f,.16f);glass.shader=Shader.Find("AfterSignal/NovaWindows");
            foreach(var m in materials.Values)if(m)m.enableInstancing=true;
        }
        static Transform FutureTower(Transform parent,Vector3 at,float w,float d,float h,int serial)
        {
            if(!materials.ContainsKey("FutureSilver"))FutureMaterials();
            string profile=futureForms[Mathf.Abs(serial)%futureForms.Length];
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"FutureArchitecture/"+profile+".fbx");if(!asset)throw new Exception("Missing future architecture: "+profile);
            var p=Group("Future "+profile+" / "+serial,at,parent);p.gameObject.AddComponent<CollapsibleBuilding>();
            var model=Object.Instantiate(asset,p,false);model.name="Authored architecture";model.transform.localScale=new Vector3(w,h,d);model.transform.localRotation=Quaternion.Euler(0,serial%3==0?12:0,0);
            var near=new System.Collections.Generic.List<Renderer>();var far=new System.Collections.Generic.List<Renderer>();
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                string source=r.sharedMaterial?r.sharedMaterial.name:"";bool collision=r.name.StartsWith("Collision");
                if(collision){r.enabled=false;r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;continue;}
                string key=source.StartsWith("FutureGlass")?"FutureGlass":source.StartsWith("FutureNeon")?serial%2==0?"FutureLight":"FutureRose":source.StartsWith("FutureMetal")?"FutureSilver":source.StartsWith("FutureLeaf")?"Leaf":new[]{"FutureCarbon","FutureCeramic","FutureCopper","FutureSilver"}[serial%4];
                r.sharedMaterial=materials[key];if(r.name.StartsWith("Far"))far.Add(r);else near.Add(r);
                r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
            }
            var lod=p.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.065f,near.ToArray()),new LOD(.007f,far.ToArray())});lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();
            foreach(var t in p.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
            return p;
        }
        static void StreetBreakable(Transform p,float energy,float radius=.18f,float height=7)
        {
            var component=p.gameObject.AddComponent<BreakableStreetProp>();component.breakEnergy=energy;
            if(!p.GetComponentsInChildren<Collider>().Any()){var c=p.gameObject.AddComponent<CapsuleCollider>();c.center=Vector3.up*height*.5f;c.height=height;c.radius=radius;}
            foreach(var t in p.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
        }
        static void FutureCore()
        {
            var scene=EditorSceneManager.OpenScene("Assets/AfterSignal/Scenes/24_OpenCity.unity",OpenSceneMode.Single);
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("SITE / ")).ToArray())
            {
                var upper=t.GetComponentsInChildren<Transform>().FirstOrDefault(x=>x.name=="Upper floors / camera cutaway");if(!upper)continue;
                var collider=upper.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c=>c.name=="Upper building volume");if(!collider)continue;
                var b=collider.bounds;int id=int.Parse(t.name.Split('/')[1]);
                foreach(Transform child in upper.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                FutureTower(upper,new Vector3(b.center.x,b.min.y,b.center.z),b.size.x,b.size.z,b.size.y,id);
            }
            // Group each original pole with its adjacent fixture so the whole lamp falls together.
            var all=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach(var t in all.Where(t=>t.name=="Road light pole"||t.name=="Tree trunk").ToArray())
            {
                if(t.GetComponentInParent<BreakableStreetProp>())continue;bool tree=t.name=="Tree trunk";
                var at=t.position;at.y=0;var holder=new GameObject(tree?"Breakable city tree":"Breakable street lamp").transform;holder.SetParent(t.parent);holder.position=at;t.SetParent(holder,true);
                foreach(var other in all)if(other&&other!=t&&(tree?other.name.Contains("authored foliage"):other.name.Contains("Road light"))&&Vector2.Distance(new Vector2(other.position.x,other.position.z),new Vector2(at.x,at.z))<3)other.SetParent(holder,true);
                StreetBreakable(holder,tree?420000:120000,tree?.3f:.18f,tree?5:9.2f);
            }
            EditorSceneManager.SaveScene(scene);
        }
        static void TransitFacilities()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);root=new GameObject("Intercity passenger infrastructure").transform;meshId=94000;
            foreach(bool air in new[]{false,true})foreach(bool nova in new[]{false,true})
            {
                Vector3 at=air?(nova?new(1855,.2f,-3975):new(1394,.2f,780)):nova?new(927.4f,.1f,-2404):new(1250,.1f,-672);
                var p=Group((air?"Airport gate":"Ferry gate")+(nova?" / Nova":" / Afterlight"),at);
                Box(p,"Terminal platform",new(0,-.12f,0),new(15,.24f,13),"Pavement");
                Box(p,"Cantilever terminal roof",new(0,4.2f,0),new(16,.18f,14),"FutureCarbon",false);
                for(int s=-1;s<=1;s+=2)Beam(p,"V-shaped canopy leg",new(s*6,0,4),new(s*7,4.2f,-4),.15f,"FutureSilver",true);
                Box(p,"Gate information display",new(5,2.1f,2),new(.2f,2,3.5f),"FutureCarbon");
                Text(p,air?"LUMEN AIR / LA207\nAFTERLIGHT <> NOVA":"BLUEWATER / BW12\nAFTERLIGHT <> NOVA",new(0,3.45f,-5.5f),.23f,"FutureLight",180);
                for(int i=0;i<3;i++){Box(p,"Boarding lounge bench",new(-4+i*4,.55f,4),new(2.7f,.2f,.65f),"FutureSilver");Box(p,"Seat back",new(-4+i*4,.95f,4.3f),new(2.7f,.7f,.1f),"FutureCarbon");}
                var gate=air?(nova?new Vector3(1868,.2f,-3996):new Vector3(1408,.2f,794)):nova?new Vector3(927.4f,.1f,-2390):new Vector3(1250,.1f,-664);
                var door=air?(nova?new Vector3(1868,2.2f,-3993.4f):new Vector3(1408,2.2f,796.6f)):nova?new Vector3(923.3f,.45f,-2379):new Vector3(1275.8f,.45f,-708);
                Ribbon(root,"Passenger boarding walkway",new[]{at,gate,door},3.6f,0,-.08f,"Pavement",true);
                for(int s=-1;s<=1;s+=2)Ribbon(root,"Gangway safety rail",new[]{gate,door},.07f,s*1.65f,1,"FutureSilver");
                Console(p,new Vector3(3,0,-2),air?FacilityFunction.Airport:FacilityFunction.Ferry,air?"루멘 에어 출발 게이트":"블루워터 여객선 승선 게이트");
            }
            CombinePresentation();StripNovaSourceMeshes();SaveGeneratedMeshes();PrefabUtility.SaveAsPrefabAsset(root.gameObject,Output+"TransitFacilities.prefab");Object.DestroyImmediate(root.gameObject);
        }
        static void RetrofitStreetProps()
        {
            string file=Output+"MobilityDistricts.prefab";var prefab=PrefabUtility.LoadPrefabContents(file);
            foreach(var t in prefab.GetComponentsInChildren<Transform>().Where(t=>t.name=="street_lamp_01 / asset").ToArray())
            {
                if(t.GetComponent<BreakableStreetProp>())continue;StreetBreakable(t,120000,.18f,7);
                var component=t.GetComponent<BreakableStreetProp>();component.cutBatches=true;component.worldBounds=new Bounds(t.position+Vector3.up*4,new Vector3(5,9,5));
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>())component.worldBounds.Encapsulate(r.bounds);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab,file);PrefabUtility.UnloadPrefabContents(prefab);
        }
        public static void FinalizeFutureAssets()
        {
            AssetDatabase.Refresh();
            materials.Clear();ImportMaterials();MakeMaterials();FutureMaterials();FutureCore();
            foreach(string name in new[]{"AfterlightExpansion","MobilityDistricts","NeonHarbor"})
            {
                string path=Output+name+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);root=prefab.transform;
                foreach(var prop in prefab.GetComponentsInChildren<BreakableStreetProp>())
                {
                    if(prop.GetComponent<LODGroup>())continue;var renderers=prop.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
                    if(renderers.Length==0)continue;var lod=prop.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.035f,renderers)});lod.RecalculateBounds();
                }
                if(name=="AfterlightExpansion")StripNovaSourceMeshes();
                PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("AFTERSIGNAL/World/Build future city and player")]
        public static void BuildFutureAndRelease()
        {
            Build();UpgradeExistingCraft();BuildNeonHarbor();FutureMaterials();TransitFacilities();RetrofitStreetProps();FutureCore();AssetDatabase.SaveAssets();ProjectBuilder.BuildRelease();
        }
    }
}
