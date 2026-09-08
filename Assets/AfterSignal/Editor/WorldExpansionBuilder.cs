using System;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.Rendering;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        const string Output="Assets/AfterSignal/Resources/WorldAssets/";
        static Transform root;static int meshId;static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static readonly Dictionary<Vector3,Mesh> boxes=new Dictionary<Vector3,Mesh>();
        [Serializable]class Imported{public string id;public ImportedMaterial[] materials;}
        [Serializable]class ImportedMaterial{public string name;public string[] images;}
        [MenuItem("AFTERSIGNAL/World/Build coastal expansion")]
        public static void Build()
        {
            AssetDatabase.Refresh();materials.Clear();boxes.Clear();meshId=65000;Directory.CreateDirectory(Output+"Generated");
            ImportMaterials();
            root=new GameObject("AFTERLIGHT / coastal metropolitan expansion",typeof(ExpansionWorld)).transform;
            MakeMaterials();BuildTerrain();BuildRoads();BuildNeighborhoods();BuildAirport();BuildHarbor();BuildCoast();BuildServices();
            CombinePresentation();SaveGeneratedMeshes();
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Output+"AfterlightExpansion.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
            AssetDatabase.SaveAssets();Debug.Log("WORLD EXPANSION: prefab, roads, ten destinations and facility population authored.");
        }
        public static void BuildAndRelease(){Build();ProjectBuilder.BuildRelease();}
        public static void ReleasePresentation(){materials.Clear();ImportMaterials();MakeMaterials();AssetDatabase.SaveAssets();ProjectBuilder.BuildRelease();}
        static Material Mat(string name,string color,float metal=0,float smooth=.35f,float emission=0)
        {
            string path=Output+"Generated/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}ColorUtility.TryParseHtmlString(color,out var c);m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*emission);}materials[name]=m;return m;
        }
        static void MakeMaterials()
        {
            Mat("Asphalt","#20282b",.05f,.28f);Mat("Pavement","#727a79",.05f,.4f);Mat("Earth","#565e57",0,.15f);Mat("Sand","#b2a387",0,.15f);Mat("Steel","#30434b",.8f,.55f);Mat("Aluminium","#a8b8b9",.75f,.65f);Mat("Cladding","#d4dcda",.32f,.65f);Mat("Rubber","#10181d",0,.25f);Mat("PaintWhite","#e0d4ac",0,.4f);Mat("PaintAmber","#daa941",0,.4f);Mat("Wood","#66554a",0,.24f);Mat("Leaf","#31534e",0,.22f);Mat("Trunk","#4d453c",0,.28f);Mat("WindowDark","#17353e",.65f,.8f);Mat("CargoBlue","#325d67",.55f,.4f);Mat("CargoRed","#964f45",.5f,.4f);Mat("CargoGold","#a78c50",.5f,.4f);Mat("NeonCyan","#6debd8",.2f,.4f,.6f);Mat("NeonRose","#f474ac",.1f,.4f,.7f);Mat("NeonWarm","#ffc779",.1f,.4f,.65f);
            var glass=Mat("Glazing","#799a9a",.2f,.9f);glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.SetColor("_BaseColor",new Color(.35f,.62f,.65f,.24f));glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            var water=Mat("Ocean","#164757",.35f,.85f);water.shader=Shader.Find("AfterSignal/CoastalWater");
            foreach(var shader in new[]{"AfterSignal/ExplosionVolume","AfterSignal/SeveredSprite","AfterSignal/WorldLettering"}){string name=shader.Split('/')[1];string p=Output+"Generated/"+name+".mat";if(!AssetDatabase.LoadAssetAtPath<Material>(p))AssetDatabase.CreateAsset(new Material(Shader.Find(shader)),p);}
            Surface("Asphalt","asphalt_02");Surface("Pavement","concrete_pavement");Surface("Earth","aerial_grass_rock");Surface("Sand","aerial_sand");
            Mat("TerminalFloor","#899395",0,.18f);Mat("RoofInterior","#56646e",.1f,.2f);
            for(int i=0;i<8;i++)materials["UrbanFacade"+i]=Resources.Load<Material>("Materials/Urban-Facade-"+i.ToString("00"));
        }
        static void Surface(string name,string id)
        {
            var m=materials[name];m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.18f);
            foreach(var path in Directory.GetFiles(Output+"Surfaces/"+id,"*.jpg"))
            {
                string file=path.Replace('\\','/');var importer=AssetImporter.GetAtPath(file) as TextureImporter;
                if(importer&&file.Contains("nor_gl")&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
                var t=AssetDatabase.LoadAssetAtPath<Texture2D>(file);
                if(file.Contains("_diff_"))m.SetTexture("_BaseMap",t);
                if(file.Contains("nor_gl")){m.SetTexture("_BumpMap",t);m.EnableKeyword("_NORMALMAP");}
            }
        }
        static void ImportMaterials()
        {
            foreach(string path in Directory.GetFiles(Output,"materials.json",SearchOption.AllDirectories))
            {
                var info=JsonUtility.FromJson<Imported>(File.ReadAllText(path));string dir=Path.GetDirectoryName(path).Replace('\\','/');
                foreach(var src in info.materials)
                {
                    string dest=dir+"/"+src.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(dest);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,dest);}m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.38f);
                    foreach(string image in src.images)
                    {
                        string file=dir+"/"+image;var importer=AssetImporter.GetAtPath(file) as TextureImporter;
                        if(importer&&image.Contains("nor_gl")&&importer.textureType!=TextureImporterType.NormalMap){importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();}
                        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(file);if(!t)continue;
                        if(image.Contains("_diff_"))m.SetTexture("_BaseMap",t);
                        if(image.Contains("nor_gl")){m.SetTexture("_BumpMap",t);m.EnableKeyword("_NORMALMAP");}
                        if(image.Contains("emissive")){m.SetTexture("_EmissionMap",t);m.SetColor("_EmissionColor",Color.white*2);m.EnableKeyword("_EMISSION");}
                    }
                    materials[src.name]=m;
                }
            }
        }
        static Transform Group(string name,Vector3 at,Transform parent=null)
        {var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);t.localPosition=at;return t;}
        static void Lamp(Transform p,Vector3 at,Color color,float intensity,float range)
        {var l=Group("District area light",at,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
        static GameObject Box(Transform p,string name,Vector3 at,Vector3 size,string mat,bool solid=true,PrimitiveType type=PrimitiveType.Cube)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=materials[mat];if(!solid)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.isStatic=true;
            if(type==PrimitiveType.Cube&&(mat=="Asphalt"||mat=="Pavement"||mat=="Earth"||mat=="Sand"))
            {
                var filter=o.GetComponent<MeshFilter>();
                if(!boxes.TryGetValue(size,out var mesh)){mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="Surface UV / "+size;var points=mesh.vertices;var normals=mesh.normals;var coords=new Vector2[points.Length];for(int i=0;i<points.Length;i++){var v=Vector3.Scale(points[i],size);var n=normals[i];coords[i]=Mathf.Abs(n.y)>.5f?new Vector2(v.x,v.z)/4:Mathf.Abs(n.x)>.5f?new Vector2(v.z,v.y)/4:new Vector2(v.x,v.y)/4;}mesh.uv=coords;boxes[size]=mesh;}filter.sharedMesh=mesh;
            }
            return o;
        }
        static void Beam(Transform p,string name,Vector3 a,Vector3 b,float thickness,string mat,bool solid=false)
        {var o=Box(p,name,(a+b)*.5f,new Vector3(thickness,Vector3.Distance(a,b),thickness),mat,solid,PrimitiveType.Cylinder);o.transform.up=(b-a).normalized;o.transform.localScale=new Vector3(thickness,Vector3.Distance(a,b)*.5f,thickness);}
        static GameObject MeshObject(Transform p,string name,Vector3[] v,int[] tris,string material,bool solid=false,Vector2[] uv=null)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(p,false);var m=new Mesh{name=name};if(v.Length>65000)m.indexFormat=IndexFormat.UInt32;m.vertices=v;m.triangles=tris;if(uv==null)uv=v.Select(point=>new Vector2(point.x,point.z)/4).ToArray();m.uv=uv;m.RecalculateNormals();m.RecalculateBounds();o.GetComponent<MeshFilter>().sharedMesh=m;o.GetComponent<MeshRenderer>().sharedMaterial=materials[material];if(solid)o.AddComponent<MeshCollider>().sharedMesh=m;o.isStatic=true;return o;
        }
        static void Ribbon(Transform p,string name,Vector3[] path,float width,float offset,float y,string mat,bool solid=false)
        {
            var v=new Vector3[path.Length*2];var uv=new Vector2[v.Length];var tri=new List<int>();float distance=0;
            for(int i=0;i<path.Length;i++){var d=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;var n=Vector3.Cross(Vector3.up,d);var center=path[i]+Vector3.up*y+n*offset;v[i*2]=center-n*width*.5f;v[i*2+1]=center+n*width*.5f;if(i>0)distance+=Vector3.Distance(path[i],path[i-1]);uv[i*2]=new Vector2(0,distance/8);uv[i*2+1]=new Vector2(width/8,distance/8);if(i>0){int k=i*2;tri.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}}
            MeshObject(p,name,v,tri.ToArray(),mat,solid,uv);
        }
        static GameObject Model(string asset,Transform parent,Vector3 at,float scale=1,float yaw=0,string part=null)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+asset+"/"+asset+".fbx");if(!prefab)return null;
            GameObject source=prefab;if(part!=null){var f=prefab.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.name==part);if(!f)return null;source=f.gameObject;}
            var container=Group(asset+" / "+(part??"asset"),at,parent);container.localRotation=Quaternion.Euler(0,yaw,0);container.localScale=Vector3.one*scale;
            var model=UnityEngine.Object.Instantiate(source,container,false);model.transform.localPosition=Vector3.zero;
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>m&&materials.TryGetValue(m.name,out var replacement)?replacement:materials["Steel"]).ToArray();r.gameObject.isStatic=true;}
            var filters=model.GetComponentsInChildren<MeshFilter>();if(filters.Length>0){Bounds b=filters[0].sharedMesh.bounds;foreach(var f in filters)b.Encapsulate(f.sharedMesh.bounds);model.transform.localPosition=-new Vector3(b.center.x,b.min.y,b.center.z);}
            if(asset=="street_lamp_01")StreetBreakable(container,120000,.17f,7);
            return container.gameObject;
        }
        static Transform Text(Transform p,string words,Vector3 at,float size,string mat,float yaw=0)
        {var o=Group(words,at,p);o.localRotation=Quaternion.Euler(0,yaw+180,0);var t=o.gameObject.AddComponent<TextMesh>();t.text=words;t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.fontSize=64;t.characterSize=size*.28f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=materials[mat].color;if(t.font)o.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;o.gameObject.AddComponent<WorldSign>();return o;}
        static void Crowd(Transform p,string title,Vector3 at,int count,int role,int kinds,float radius)
        {var c=Group("Population / "+title,at,p).gameObject.AddComponent<FacilityCrowd>();c.title=title;c.count=count;c.firstRole=role;c.roleCount=kinds;c.radius=radius;}
        static void Service(Transform p,int id,Vector3 at)
        {var o=Group("Service / "+ExpansionWorld.Names[id],at,p);o.gameObject.AddComponent<ExpansionService>().facility=id;var point=o.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.LifeService;point.title=ExpansionWorld.Names[id]+" · 이용 안내";point.radius=4;}
        static void SaveGeneratedMeshes()
        {
            var saved=new Dictionary<Mesh,Mesh>();
            AssetDatabase.StartAssetEditing();
            try{foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&!AssetDatabase.Contains(f.sharedMesh)){var source=f.sharedMesh;if(saved.TryGetValue(source,out var copy)){f.sharedMesh=copy;}else{string p=Output+"Generated/Mesh"+(meshId++).ToString("00000")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(existing){EditorUtility.CopySerialized(source,existing);f.sharedMesh=existing;}else AssetDatabase.CreateAsset(source,p);saved[source]=f.sharedMesh;}var col=f.GetComponent<MeshCollider>();if(col)col.sharedMesh=f.sharedMesh;}}
            finally{AssetDatabase.StopAssetEditing();}
        }
        static void CombinePresentation()
        {
            var groups=new Dictionary<string,List<CombineInstance>>();var paints=new Dictionary<string,Material>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled||!f.sharedMesh||!f.sharedMesh.isReadable||f.GetComponentInParent<MultiFloorLift>()||f.GetComponentInParent<NeonTransit>()||f.GetComponentInParent<KelpCurrent>()||f.GetComponentInParent<CollapsibleBuilding>()||f.GetComponentInParent<BreakableStreetProp>())continue;
                if(f.GetComponentInParent<BreakableGlass>())continue;
                var usable=f.GetComponentInParent<UsableProp>();if(usable&&usable.use==PropUse.Television)continue;
                Vector3 c=r.bounds.center;string cell=Mathf.FloorToInt(c.x/100)+"_"+Mathf.FloorToInt(c.y/60)+"_"+Mathf.FloorToInt(c.z/100);
                for(int sub=0;sub<f.sharedMesh.subMeshCount;sub++)
                {var mat=r.sharedMaterials[Mathf.Min(sub,r.sharedMaterials.Length-1)];if(!mat)continue;string key=cell+"_"+mat.name;if(!groups.TryGetValue(key,out var list)){list=new List<CombineInstance>();groups[key]=list;paints[key]=mat;}list.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=sub,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});}
                r.enabled=false;
            }
            var parent=Group("Batched district geometry",Vector3.zero);
            foreach(var pair in groups)
            {
                var m=new Mesh{name="District batch "+pair.Key,indexFormat=IndexFormat.UInt32};m.CombineMeshes(pair.Value.ToArray(),true,true);m.RecalculateBounds();var o=Group("Batch "+pair.Key,Vector3.zero,parent);o.gameObject.AddComponent<MeshFilter>().sharedMesh=m;var r=o.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=paints[pair.Key];r.shadowCastingMode=ShadowCastingMode.On;
                var lod=o.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.014f,new Renderer[]{r})});lod.RecalculateBounds();o.gameObject.isStatic=true;
            }
        }
    }
    public sealed class ExpansionModelImporter:AssetPostprocessor
    {
        void OnPreprocessModel(){if(!assetPath.Contains("/WorldAssets/"))return;var i=(ModelImporter)assetImporter;i.isReadable=true;i.importAnimation=false;i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;i.meshCompression=ModelImporterMeshCompression.Off;}
    }
}
