using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class AtlasLabels:MonoBehaviour
    {
        AtlasViewport map;readonly List<Text> labels=new();float next;int count;readonly List<Rect> occupied=new();
        public void Initialize(AtlasViewport viewport){map=viewport;var mask=gameObject.AddComponent<RectMask2D>();}
        Text Label(){if(count>=labels.Count){var go=new GameObject("Readable map label",typeof(RectTransform),typeof(Text),typeof(Outline));go.transform.SetParent(transform,false);var t=go.GetComponent<Text>();t.font=Resources.Load<Font>("Fonts/NotoSansKR");t.fontSize=13;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;go.GetComponent<Outline>().effectColor=new Color(.015f,.045f,.06f,.95f);go.GetComponent<Outline>().effectDistance=new(1,-1);labels.Add(t);}return labels[count++];}
        void Add(Vector3 p,string text,Color color,int size=13,bool priority=false)
        {var xy=map.Project(p);var rect=map.rectTransform.rect;if(!rect.Contains(xy)||xy.y>rect.yMax-18||xy.y<rect.yMin+18)return;float width=Mathf.Min(245,text.Length*size*.73f+12);var box=new Rect(xy.x-width/2,xy.y-20,width,36);foreach(var used in occupied)if(!priority&&box.Overlaps(used))return;occupied.Add(box);var t=Label();t.text=text;t.color=color;t.fontSize=size;var r=t.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new(width,40);r.anchoredPosition=xy-rect.center+Vector2.down*17;}
        void Update()
        {
            if(!map||map.mini||Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;count=0;occupied.Clear();
            if(FourCityAtlasSelection.Venue!=null){var v=FourCityAtlasSelection.Venue;Add(v.position,"◆ "+v.title,new Color(1,.88f,.45f),16,true);}
            if(map.Span>2600){for(int i=0;i<4;i++)Add(FourCityCatalog.Centers[i],FourCityCatalog.CityNames[i],new Color(.7f,.88f,.94f),21,true);}
            else
            {foreach(var v in FourCityCatalog.Venues)if(map.Visible(v)&&v!=FourCityAtlasSelection.Venue)Add(v.position,FourCityAtlasSelection.Icon(v.kind)+" · "+v.title,FourCityAtlasSelection.Color(v.kind),14);
                for(int i=0;i<ExpansionWorld.Places.Length;i++)Add(ExpansionWorld.Places[i],ExpansionWorld.Names[i],new Color(.65f,.79f,.82f));
                if(map.Span<800)for(int i=0;i<UrbanCatalog.SiteCount;i++)Add(UrbanCatalog.Center(i),UrbanCatalog.Name(i),new Color(.55f,.71f,.72f),12);}
            for(int i=count;i<labels.Count;i++)labels[i].gameObject.SetActive(false);for(int i=0;i<count;i++)labels[i].gameObject.SetActive(true);
        }
    }
}
