using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class UrbanDetailStreaming:MonoBehaviour
    {
        struct Site {public Vector3 p;public Quaternion q;public int kind;}
        readonly List<Site> sites=new();readonly Dictionary<int,GameObject> live=new();readonly List<int> remove=new();
        readonly HashSet<Vector2Int> cells=new();float next;
        public int ActiveSites=>live.Count;
        IEnumerator Start()
        {
            while(!FourCityWorld.Instance||!FourCityWorld.Instance.Built)yield return null;
            for(int x=0;x<6;x++)for(int z=0;z<5;z++)
            {var p=CityRoadNetwork.Junction(x,z);Add(p+new Vector3(42,0,19),Quaternion.identity);Add(p+new Vector3(-19,0,67),Quaternion.Euler(0,90,0));}
            Roads(ExpansionRoads.Roads);Roads(NeonHarbor.Roads);Roads(FourCityCatalog.Roads);
        }
        void Roads(IEnumerable<Vector3[]> roads)
        {
            foreach(var line in roads)
            {
                float accumulated=0;
                for(int i=1;i<line.Length;i++)
                {
                    var d=line[i]-line[i-1];float length=d.magnitude;if(Mathf.Abs(d.y)>.3f)continue;accumulated+=length;
                    if(accumulated<70)continue;accumulated=0;var q=Quaternion.LookRotation(d);
                    Add(line[i]+q*new Vector3(15,0,0),q*Quaternion.Euler(0,90,0));
                }
            }
        }
        void Add(Vector3 p,Quaternion q)
        {
            if(FourCityCatalog.CityAt(p)==2)return;var cell=new Vector2Int(Mathf.RoundToInt(p.x/22),Mathf.RoundToInt(p.z/22));if(!cells.Add(cell))return;
            sites.Add(new Site{p=p,q=q,kind=sites.Count%5});
        }
        void Update()
        {
            var game=GameDirector.Instance;if(!game||!game.Ready||Time.unscaledTime<next)return;next=Time.unscaledTime+.35f;var eye=game.Player.transform.position;
            remove.Clear();foreach(var pair in live)if(!pair.Value||(sites[pair.Key].p-eye).sqrMagnitude>230*230)remove.Add(pair.Key);
            foreach(int id in remove){if(live[id])Destroy(live[id]);live.Remove(id);}
            int made=0;
            for(int i=0;i<sites.Count&&live.Count<48&&made<3;i++)
            {
                var site=sites[i];if(live.ContainsKey(i)||(site.p-eye).sqrMagnitude>170*170)continue;
                if(!NpcGroundSupport.Floor(site.p,site.p.y+1.2f,3,out float floor)||Mathf.Abs(floor-site.p.y)>.6f)continue;
                var p=new Vector3(site.p.x,floor+.025f,site.p.z);
                // Exclude buildings/roads with a broad standing-volume query; keep clear pavement aisles.
                if(Physics.CheckBox(p+Vector3.up*1.5f,new Vector3(2.8f,1.1f,1.4f),site.q,1,QueryTriggerInteraction.Ignore))continue;
                var root=new GameObject("Local street amenity");root.transform.SetParent(transform,false);root.transform.SetPositionAndRotation(p,site.q);
                string kit=site.kind==0?"TransitShelter":site.kind==1?"CivicKiosk":site.kind==2?"CoastalPalm":site.kind==3?"PromenadeBench":"ChargePoint";
                var prop=StreetKit.Place(kit,root.transform,Vector3.zero,Quaternion.identity);
                if(site.kind==0||site.kind==1)for(int side=-1;side<=1;side+=2)StreetKit.Place("SmartBollard",root.transform,new Vector3(side*3.2f,0,-1.1f),Quaternion.identity);
                if(prop&&(site.kind==0||site.kind==1||site.kind==4)){var point=prop.AddComponent<InteractionPoint>();point.kind=InteractionKind.Furniture;point.title=site.kind==0?"교통 안내":site.kind==1?"도시 안내 지도":"차량 충전";point.radius=3;prop.AddComponent<StreetAmenity>().kind=site.kind;}
                if(site.kind==0||site.kind==1){var glow=new GameObject("Display spill light");glow.transform.SetParent(root.transform,false);glow.transform.localPosition=new(0,1.7f,-.8f);var light=glow.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=8;light.color=new(.08f,.65f,1);light.shadows=LightShadows.None;}
                live[i]=root;made++;
            }
        }
    }
    public sealed class StreetAmenity:MonoBehaviour
    {
        public int kind;
        public void Use(GameDirector game)
        {
            if(kind==1){UrbanSimulation.Instance.OpenMap();return;}
            if(kind==0){game.ShowDialogue("교통 안내","시내버스는 정류장의 탑승 안내에서 버스비를 지불하고 이용하세요.\n공항·항구에서는 G로 여객편을 예약할 수 있습니다.\nM 지도의 교통 필터에서 정류장과 터미널을 찾을 수 있습니다.");return;}
            CityVehicle nearest=null;float distance=12;
            foreach(var car in Object.FindObjectsByType<CityVehicle>()){float d=Vector3.Distance(car.transform.position,transform.position);if(!car.Wrecked&&d<distance){distance=d;nearest=car;}}
            if(!nearest){game.Toast("차량을 충전기에서 12m 이내에 세워 주세요.");return;}
            if(nearest.fuel>=CityVehicle.Capacity-.01f){game.Toast("이미 충전이 완료된 차량입니다.");return;}
            if(!LifeState.Spend(60)){game.Toast("충전에 60 C가 필요합니다.");return;}
            nearest.fuel=Mathf.Min(CityVehicle.Capacity,nearest.fuel+30);game.Toast("60 C 결제 · 차량 에너지 보충");
        }
    }
}
