using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public sealed class HighriseBuilding:MonoBehaviour
    {
        public static readonly List<HighriseBuilding> All=new();
        public Bounds Bounds{get;private set;}
        public int Floors{get;private set;}
        public int Identity{get;private set;}
        public string Title{get;private set;}
        public Vector3 Door{get;private set;}
        Renderer[] exterior;Collider[] solids;bool[] visible,colliding;GameObject entry;
        static readonly string[] names={"아르카디아 테크","루멘 미디어","해협 금융센터","오로라 바이오","노바 디자인랩","시그널 물류","메모리 네트웍스","크로마 레지던스"};
        void Start()
        {
            exterior=GetComponentsInChildren<Renderer>();solids=GetComponentsInChildren<Collider>();
            bool found=false;Bounds b=default;
            foreach(var r in exterior)if(r.bounds.size.y>5){if(!found){b=r.bounds;found=true;}else b.Encapsulate(r.bounds);}
            bool undersea=FourCityCatalog.CityAt(transform.position)==3;
            if(!found||b.size.y<(undersea?12:30)){enabled=false;return;}
            Bounds=b;Floors=Mathf.Clamp(Mathf.RoundToInt(b.size.y/4.2f),undersea?3:10,undersea?8:30);
            Identity=Mathf.Abs(Mathf.RoundToInt(b.center.x)*7381+Mathf.RoundToInt(b.center.z)*193);
            Title=names[Identity%names.Length]+" "+(Identity%900+100)+"동";
            Door=new Vector3(b.center.x,b.min.y+.05f,b.min.z-1.3f);
            if(NpcGroundSupport.Floor(Door,b.min.y+1.5f,8,out var y))Door=new Vector3(Door.x,y+.08f,Door.z);
            entry=new GameObject("E · "+Title+" / "+Floors+"층");entry.transform.SetParent(transform,true);entry.transform.position=Door+Vector3.up;
            var point=entry.AddComponent<InteractionPoint>();point.kind=InteractionKind.Furniture;point.title=Title+" 입장 · "+Floors+"개 층";point.radius=3.2f;
            entry.AddComponent<HighriseDoor>().building=this;
            All.Add(this);
        }
        public void Enter()
        {
            var collapse=GetComponent<CollapsibleBuilding>();if(collapse&&collapse.Collapsed)return;
            HighriseInterior.Open(this);
        }
        public void Relocate(Vector3 delta){transform.position+=delta;var b=Bounds;b.center+=delta;Bounds=b;Door+=delta;var collapse=GetComponent<CollapsibleBuilding>();if(collapse){var bounds=collapse.worldBounds;bounds.center+=delta;collapse.worldBounds=bounds;}exterior=GetComponentsInChildren<Renderer>();}
        public void ShowExterior(bool show)
        {
            if(!show)
            {
                visible=new bool[exterior.Length];colliding=new bool[solids.Length];
                for(int i=0;i<exterior.Length;i++)if(exterior[i]){visible[i]=exterior[i].enabled;exterior[i].enabled=false;}
                for(int i=0;i<solids.Length;i++)if(solids[i]){colliding[i]=solids[i].enabled;solids[i].enabled=false;}
                var lod=GetComponent<LODGroup>();if(lod)lod.enabled=false;
            }
            else if(visible!=null)
            {
                for(int i=0;i<exterior.Length;i++)if(exterior[i])exterior[i].enabled=visible[i];
                for(int i=0;i<solids.Length;i++)if(solids[i])solids[i].enabled=colliding[i];
                var lod=GetComponent<LODGroup>();if(lod)lod.enabled=true;
            }
            if(entry)entry.SetActive(show);
        }
        void OnDestroy(){All.Remove(this);}
    }
}
