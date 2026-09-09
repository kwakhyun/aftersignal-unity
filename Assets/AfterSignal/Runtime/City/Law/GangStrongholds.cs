using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    // Small compounds occupy clear parcels in the existing cities; no new terrain is created.
    public sealed class GangStrongholds:MonoBehaviour
    {
        public static GangStrongholds Instance{get;private set;}
        public readonly List<Vector3> Bases=new();readonly List<List<GangMember>> garrisons=new();float next,convoyAt;
        IEnumerator Start()
        {
            Instance=this;while(!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;yield return new WaitForSeconds(1);
            Vector3[] preferred={new(490,0,180),new(1320,0,110),new(1050,0,-3040)};
            for(int i=0;i<preferred.Length;i++)
            {
                bool found=false;Vector3 at=default;
                for(int ring=0;ring<20&&!found;ring++)for(int j=0;j<16&&!found;j++)
                {
                    float angle=j*Mathf.PI*2/16;var p=preferred[i]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*18;
                    if(!CityGangWar.FindGround(p,out at)||FourCityCatalog.CityAt(at)!=(i==2?1:0))continue;
                    if(Physics.CheckBox(at+Vector3.up*5,new Vector3(13,4.8f,16),Quaternion.identity,1,QueryTriggerInteraction.Ignore))continue;
                    if(!CityGangWar.FindGround(at+new Vector3(12,0,15),out _)||!CityGangWar.FindGround(at-new Vector3(12,0,15),out _))continue;
                    bool roadway=false;foreach(var road in ExpansionRoads.Roads)for(int k=1;k<road.Length;k++)if(Distance(at,road[k-1],road[k])<31)roadway=true;
                    foreach(var road in FourCityCatalog.Roads)for(int k=1;k<road.Length;k++)if(Distance(at,road[k-1],road[k])<31)roadway=true;
                    var junction=CityRoadNetwork.NearestJunction(at);if(i<2&&at.x<780&&(Mathf.Abs(at.x-junction.x)<32||Mathf.Abs(at.z-junction.z)<32))roadway=true;
                    if(!roadway)found=true;
                }
                if(!found){Debug.LogWarning("No clear gang parcel "+i);continue;}
                Bases.Add(at);garrisons.Add(new());Build(at,i);yield return null;
            }
            var story=CityChronicle.Instance;if(story){for(int i=0;i<story.Quests.Length;i++){var q=story.Quests[i];if(!q.id.StartsWith("gang-main-"))continue;if(int.TryParse(q.id.Substring(10),out int b)&&b<Bases.Count)foreach(var s in q.steps)s.position=Bases[b]+new Vector3(0,.1f,-11);}story.Refresh();}
            convoyAt=Time.time+20;
        }
        static float Distance(Vector3 p,Vector3 a,Vector3 b){if(Mathf.Abs(p.y-a.y)>5)return 9999;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
        void Build(Vector3 at,int index)
        {
            var root=new GameObject(GangMember.CrewName(index)+" / 무기 공방·통신 아지트").transform;root.SetParent(transform);root.position=at;var g=new CityGeometry(root);
            g.Box("Compound deck",new(0,.12f,0),new(24,.2f,28),"Concrete",true);
            for(int s=-1;s<=1;s+=2){g.Box("Armoured facade",new(s*11.7f,4,0),new(.5f,8,28),"DarkMetal",true);g.Box("Front gate pier",new(s*8,3.5f,-13.8f),new(8,7,.5f),"Steel",true);}
            g.Box("Rear service wall",new(0,4,13.7f),new(24,8,.5f),"DarkMetal",true);
            g.Box("Loft office floor",new(-5,4.1f,4),new(13,.25f,19),"Steel",true);
            g.Box("Roof east canopy",new(7.5f,8.1f,0),new(9,.25f,28),"DarkMetal",true);
            g.Box("Roof rear canopy",new(-4.5f,8.1f,9),new(15,.25f,10),"Steel",true);
            for(int i=0;i<18;i++)g.Box("Accessible stair",new(4.1f,.14f+i*.225f,-7+i*.62f),new(2.1f,.25f,.65f),"Steel",true);
            for(int i=0;i<5;i++)
            {
                g.Box("Ammunition case",new(-8+(i%2)*2,.7f,-8+i*3),new(1.7f,1,1.3f),"Metal",true);
                g.Box("Server cabinet",new(-9+i*2.3f,5.7f,11),new(1.5f,2.7f,1.2f),"DarkMetal",true);
                for(int k=0;k<5;k++)g.Box("Server status",new(-9+i*2.3f,4.7f+k*.4f,10.36f),new(.8f,.055f,.025f),"NeonRose");
            }
            for(int i=0;i<3;i++){g.Box("Armor maintenance bench",new(8,.85f,-5+i*6),new(4,1.4f,2.3f),"Steel",true);g.Box("Workshop screen",new(9,2.2f,-5+i*6),new(.12f,1.3f,1.8f),"NeonCyan");}
            for(int i=0;i<8;i++){g.Beam(new(-11,7,-12+i*3.5f),new(11,7,-12+i*3.5f),.16f,"Steel");g.Box("Red security strip",new(-11.36f,3.5f,-11+i*3),new(.06f,.12f,1.8f),"NeonRose");}
            g.Finish();
            for(int i=0;i<3;i++){var lamp=new GameObject("Workshop practical light").AddComponent<Light>();lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(i==0?0:i==1?-6:7,5,i==0?-12:5);lamp.type=LightType.Point;lamp.range=18;lamp.intensity=3.5f;lamp.color=i==2?new Color(.12f,.7f,1):new Color(1,.17f,.28f);lamp.shadows=LightShadows.None;}
            var sign=new GameObject("Crew identity",typeof(TextMesh));sign.transform.SetParent(root,false);sign.transform.localPosition=new Vector3(0,6.1f,-14.1f);var text=sign.GetComponent<TextMesh>();text.text=GangMember.CrewName(index)+"\nACCESS RESTRICTED";text.font=Resources.Load<Font>("Fonts/NotoSansKR");text.GetComponent<Renderer>().sharedMaterial=text.font.material;text.fontSize=42;text.characterSize=.06f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.25f,.33f);
        }
        void Update()
        {
            var g=GameDirector.Instance;if(!g||!g.Ready||g.Blocked||Time.time<next)return;next=Time.time+3;
            for(int b=0;b<Bases.Count;b++)
            {
                float d=Vector3.Distance(g.Player.transform.position,Bases[b]);var units=garrisons[b];units.RemoveAll(u=>!u);
                if(d<230&&units.Count==0)for(int i=0;i<8;i++){var p=Bases[b]+new Vector3(i%4*3-5,.25f,i/4*6-5);if(CityGangWar.FindGround(p,out var at)){var member=GangMember.Create(at,b,i);member.gameObject.AddComponent<GangCrime>();units.Add(member);}}
                if(d>480)foreach(var u in units)if(u&&!u.Body.Downed)Destroy(u.gameObject);
            }
            if(!CityEventGate.Busy&&!CampaignBattle.Active&&Time.time>convoyAt&&GangConvoy.Active<1&&FourCityCatalog.CityAt(g.Player.transform.position)!=2)
            {convoyAt=Time.time+(ResidentialWorld.AtHome(LifeState.Hour)?28:55);GangConvoy.Dispatch(g.Player.transform.position);}
        }
        void OnDestroy(){foreach(var group in garrisons)foreach(var m in group)if(m)Destroy(m.gameObject);if(Instance==this)Instance=null;}
    }
}
