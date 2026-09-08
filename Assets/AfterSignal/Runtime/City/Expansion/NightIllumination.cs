using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace AfterSignal
{
    public sealed class NightIllumination:MonoBehaviour
    {
        readonly List<Vector3> fixtures=new();readonly List<Material> clones=new();readonly List<Color> emissions=new();
        Light[] pool;float next;public float Night {get;private set;}public int LitFixtures {get;private set;}
        IEnumerator Start()
        {
            yield return null;yield return null;
            var replacements=new Dictionary<Material,Material>();
            foreach(var r in FindObjectsByType<MeshRenderer>())
            {
                if(r.GetComponentInParent<CityVehicle>())continue;
                var mats=r.sharedMaterials;bool changed=false;
                for(int i=0;i<mats.Length;i++)
                {
                    var m=mats[i];if(!m||!m.HasProperty("_EmissionColor")||m.shader.name=="AfterSignal/Architectural Glass")continue;
                    string n=m.name.ToLowerInvariant();bool fixture=n.Contains("neon")||n.Contains("districtlight");
                    bool neon=fixture||m.IsKeywordEnabled("_EMISSION")&&m.GetColor("_EmissionColor").maxColorComponent>.01f&&(n.Contains("window")||n.Contains("sign"));
                    if(!neon)continue;
                    if(!replacements.TryGetValue(m,out var copy))
                    {
                        copy=new Material(m);copy.name=m.name+" / timed night glow";copy.EnableKeyword("_EMISSION");copy.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        var baseColor=m.GetColor("_EmissionColor");if(baseColor.maxColorComponent<.01f)baseColor=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.cyan;
                        if(baseColor.maxColorComponent>.01f)baseColor/=baseColor.maxColorComponent;
                        baseColor*=n.Contains("window")?.3f:n.Contains("districtlight")?.48f:1.8f;replacements[m]=copy;clones.Add(copy);emissions.Add(baseColor);
                    }
                    mats[i]=copy;changed=true;
                }
                if(changed)r.sharedMaterials=mats;
            }
            foreach(var t in FindObjectsByType<Transform>())
            {
                string n=t.name.ToLowerInvariant();
                if(n.StartsWith("street_lamp_01 /")||n=="street lamp post"||n=="lamp post"||n=="street lamp pole"||n=="road light arm")
                {var at=t.position;at.y=n.StartsWith("street_lamp")?at.y+6.3f:Mathf.Max(at.y,5.7f);if(!fixtures.Any(p=>(p-at).sqrMagnitude<9))fixtures.Add(at);}
            }
            if(GameDirector.Instance.stage==StageId.UrbanCity)
                foreach(var road in ExpansionRoads.Roads)for(int i=0;i<road.Length;i+=12){var d=(road[Mathf.Min(i+1,road.Length-1)]-road[Mathf.Max(0,i-1)]).normalized;var p=road[i]+Vector3.Cross(Vector3.up,d)*14+Vector3.up*6.1f;if(!fixtures.Any(a=>(a-p).sqrMagnitude<64))fixtures.Add(p);}
            pool=new Light[40];for(int i=0;i<pool.Length;i++){var go=new GameObject("Road lighting pool "+i);go.transform.SetParent(transform,false);var l=pool[i]=go.AddComponent<Light>();l.type=LightType.Point;l.range=18;l.shadows=LightShadows.None;l.color=i%4==0?new Color(.38f,.76f,1):new Color(1,.73f,.42f);l.enabled=false;}
        }
        void Update()
        {
            if(pool==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.5f;
            Night=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.1f,.25f,Mathf.Sin((LifeState.Hour-6)/24*Mathf.PI*2)));
            Shader.SetGlobalFloat("_CityNightGlow",Night);
            for(int i=0;i<clones.Count;i++)clones[i].SetColor("_EmissionColor",emissions[i]*Mathf.Lerp(.13f,1.7f,Night));
            var g=GameDirector.Instance;if(!g)return;var eye=g.Player.transform.position;
            var close=fixtures.Where(p=>(p-eye).sqrMagnitude<140*140).OrderBy(p=>(p-eye).sqrMagnitude).Take(pool.Length).ToArray();LitFixtures=Night>.08f?close.Length:0;
            for(int i=0;i<pool.Length;i++){pool[i].enabled=i<close.Length&&Night>.08f;if(i<close.Length){pool[i].transform.position=close[i];pool[i].intensity=8*Night;}}
        }
        void OnDestroy(){foreach(var m in clones)if(m)Destroy(m);Shader.SetGlobalFloat("_CityNightGlow",0);}
    }
}
