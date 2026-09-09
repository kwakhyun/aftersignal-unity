using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class CollapsibleBuilding:MonoBehaviour
    {
        public Bounds worldBounds;public bool cutBatches;
        Renderer[] presentation;
        int cutToken;GameObject rubble;WorldActor attacker;Vector3 originalPosition;Quaternion originalRotation;Collider[] solids;bool[] solidStates;Light[] lamps;bool[] lightStates;Renderer[] surfaces;bool[] surfaceStates;
        public float RestoreDelay=180;
        public Renderer[] Presentation=>presentation??=GetComponentsInChildren<Renderer>();
        public bool Collapsed{get;private set;}
        public static int Collapses{get;private set;}
        void Start(){if(!GetComponent<HighriseBuilding>())gameObject.AddComponent<HighriseBuilding>();if(!GetComponent<FacadeGlass>())gameObject.AddComponent<FacadeGlass>();}
        public void Collapse(Vector3 point,Vector3 direction,WorldActor source=null)
        {
            if(Collapsed)return;Collapsed=true;Collapses++;attacker=source;originalPosition=transform.position;originalRotation=transform.rotation;
            solids=GetComponentsInChildren<Collider>();solidStates=System.Array.ConvertAll(solids,x=>x.enabled);lamps=GetComponentsInChildren<Light>();lightStates=System.Array.ConvertAll(lamps,x=>x.enabled);surfaces=GetComponentsInChildren<Renderer>();surfaceStates=System.Array.ConvertAll(surfaces,x=>x.enabled);
            if(worldBounds.size.y<2){worldBounds=new Bounds(transform.position,Vector3.one);foreach(var r in GetComponentsInChildren<Renderer>())worldBounds.Encapsulate(r.bounds);}
            StartCoroutine(Fall(point,direction));
        }
        IEnumerator Fall(Vector3 impact,Vector3 direction)
        {
            var g=GameDirector.Instance;g?.Audio.Play("urban_explosion",impact,.7f,1);if(g&&(g.Player.transform.position-impact).sqrMagnitude<160*160)g.CameraRig.Kick(.18f);
            if(cutBatches)cutToken=DistrictGeometryCut.Remove(worldBounds);
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var l in GetComponentsInChildren<Light>())l.enabled=false;
            var start=transform.position;var rotation=transform.rotation;float age=0,nextBurst=0;float h=worldBounds.size.y;
            while(age<4.5f)
            {
                if(!(g&&g.Blocked))
                {
                    age+=Time.deltaTime;
                    if(!cutBatches){transform.position=start-Vector3.up*h*.75f*Mathf.Pow(age/4.5f,2);transform.rotation=Quaternion.AngleAxis(age*2,Vector3.Cross(Vector3.up,direction))*rotation;}
                    if(age>nextBurst){nextBurst+=.55f;var at=worldBounds.center+Vector3.up*(h*.45f-age*h*.18f);VehicleExplosion.Smoke(at,Mathf.Clamp(Mathf.RoundToInt(h*.08f),3,12));SignalEffects.Burst(at,SignalEffects.Gold,18,10);}
                }
                yield return null;
            }
            foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;
            var bottom=new Vector3(worldBounds.center.x,worldBounds.min.y+.3f,worldBounds.center.z);
            VehicleExplosion.Create(bottom,Mathf.Min(14,worldBounds.size.x*.35f));BlastDamage.Create(bottom,Mathf.Min(35,worldBounds.size.x),180,attacker?attacker:TrafficDamageSource.Environment);
            Rubble(bottom);g?.ToastNear("건물 붕괴 · 잔해 구역에서 벗어나세요",bottom,180,5);
            yield return new WaitForSeconds(RestoreDelay);
            while(g&&(worldBounds.Contains(g.Player.transform.position)||IncidentCommand.Monster(worldBounds.center,100)))yield return new WaitForSeconds(10);
            Restore();
        }
        public void Restore()
        {
            if(!Collapsed)return;StopAllCoroutines();if(cutToken!=0)DistrictGeometryCut.Restore(cutToken);cutToken=0;transform.SetPositionAndRotation(originalPosition,originalRotation);
            for(int i=0;i<solids.Length;i++)if(solids[i])solids[i].enabled=solidStates[i];for(int i=0;i<lamps.Length;i++)if(lamps[i])lamps[i].enabled=lightStates[i];for(int i=0;i<surfaces.Length;i++)if(surfaces[i])surfaces[i].enabled=surfaceStates[i];if(rubble)Destroy(rubble);Collapsed=false;
        }
        void Rubble(Vector3 at)
        {
            var root=new GameObject("Collapsed building rubble");rubble=root;
            for(int i=0;i<12;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Fractured concrete slab";go.transform.SetParent(root.transform,false);
                go.transform.position=at+new Vector3(Mathf.Sin(i*2.4f)*worldBounds.size.x*.35f,1+i%3*.6f,Mathf.Cos(i*2.4f)*worldBounds.size.z*.35f);
                go.transform.localScale=new Vector3(worldBounds.size.x*.23f,.6f,worldBounds.size.z*.18f);go.transform.rotation=Quaternion.Euler(i%3*9,i*37,i%4*6);go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("WorldAssets/Generated/NovaConcrete");
            }
        }
    }
}
