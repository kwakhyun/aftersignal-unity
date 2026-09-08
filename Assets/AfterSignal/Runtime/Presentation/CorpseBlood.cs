using System.Collections.Generic;
using UnityEngine;

namespace AfterSignal
{
    // The pool belongs to the corpse lifecycle, while its geometry stays on the floor.
    public sealed class CorpseBlood : MonoBehaviour
    {
        public static readonly List<CorpseBlood> All=new List<CorpseBlood>();
        public bool HasPool=>pool&&pool.activeSelf;
        public Vector3 FloorPoint=>pool?pool.transform.position:transform.position;
        GameObject pool;Mesh mesh;WorldActor body;EnemyBrain enemy;Renderer corpse;
        static Material material;
        static readonly RaycastHit[] floorHits=new RaycastHit[16];
        float age;
        public static CorpseBlood Attach(GameObject owner)
        {
            var existing=owner.GetComponent<CorpseBlood>();return existing?existing:owner.AddComponent<CorpseBlood>();
        }
        void Awake(){body=GetComponent<WorldActor>();enemy=GetComponent<EnemyBrain>();corpse=GetComponentInChildren<Renderer>();}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void LateUpdate()
        {
            if(body&&(body.Alive||body.helicopter)||enemy&&enemy.Alive){Clear();Destroy(this);return;}
            if(!corpse||!corpse.enabled||!corpse.gameObject.activeInHierarchy){Clear();return;}
            if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;
            // Wait for falling/ejected bodies to contact their actual road, roof or interior floor.
            Vector3 center=corpse.bounds.center;center.y=transform.position.y;
            int count=Physics.RaycastNonAlloc(center+Vector3.up*1.1f,Vector3.down,floorHits,2.1f,1,QueryTriggerInteraction.Ignore);
            RaycastHit ground=default;float nearest=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var hit=floorHits[i];
                if(hit.normal.y<.55f||hit.point.y>transform.position.y+.25f||hit.collider.GetComponentInParent<CityVehicle>())continue;
                if(hit.distance<nearest){nearest=hit.distance;ground=hit;}
            }
            if(!ground.collider||transform.position.y-ground.point.y>.65f){if(pool)pool.SetActive(false);return;}
            if(!pool)Build();
            pool.SetActive(true);age+=Time.deltaTime;
            pool.transform.SetPositionAndRotation(ground.point+ground.normal*.012f,Quaternion.FromToRotation(Vector3.up,ground.normal));
            float growth=Mathf.Lerp(.38f,1,Mathf.SmoothStep(0,1,age/.65f));
            pool.transform.localScale=Vector3.one*growth;
        }
        void Build()
        {
            if(!material)
            {
                var shader=Resources.Load<Shader>("Shaders/CorpseBlood");
                material=new Material(shader){name="Corpse blood / shared"};material.hideFlags=HideFlags.DontSave;
            }
            pool=new GameObject("Blood pool / "+name,typeof(MeshFilter),typeof(MeshRenderer));
            var renderer=pool.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var points=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            var random=new System.Random(gameObject.GetInstanceID());
            void Patch(Vector2 center,float rx,float rz,int sides,float opacity)
            {
                int first=points.Count;
                points.Add(new Vector3(center.x,0,center.y));colors.Add(new Color(.27f,.012f,.023f,opacity));
                float rotation=(float)random.NextDouble()*Mathf.PI*2;
                for(int i=0;i<=sides;i++)
                {
                    float angle=rotation+i*Mathf.PI*2/sides;
                    float variation=.84f+(float)random.NextDouble()*.2f;
                    var edge=new Vector3(Mathf.Cos(angle)*rx*variation,0,Mathf.Sin(angle)*rz*variation);
                    points.Add(new Vector3(center.x,0,center.y)+edge*.77f);colors.Add(new Color(.34f,.015f,.026f,opacity*.94f));
                    points.Add(new Vector3(center.x,0,center.y)+edge);colors.Add(new Color(.23f,.008f,.014f,0));
                    if(i==sides)continue;
                    int k=first+1+i*2;triangles.Add(first);triangles.Add(k+2);triangles.Add(k);
                    triangles.Add(k);triangles.Add(k+2);triangles.Add(k+1);
                    triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+3);
                }
            }
            Patch(Vector2.zero,.73f,.46f,22,.94f);
            for(int i=0;i<11;i++)
            {
                float a=(float)random.NextDouble()*Mathf.PI*2,r=.55f+(float)random.NextDouble()*.4f;
                float size=.024f+(float)random.NextDouble()*.058f;
                Patch(new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.7f),size,size*.8f,7,.7f);
            }
            mesh=new Mesh{name="Irregular blood surface"};mesh.SetVertices(points);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            pool.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        void Clear(){if(pool)Destroy(pool);if(mesh)Destroy(mesh);pool=null;mesh=null;age=0;}
        void OnDisable(){All.Remove(this);Clear();}
        void OnDestroy(){All.Remove(this);Clear();}
    }
}
