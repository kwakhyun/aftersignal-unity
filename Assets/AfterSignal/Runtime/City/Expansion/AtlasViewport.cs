using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AfterSignal
{
    // All layers use one metres-to-pixels transform, so roads, routes and pins stay aligned.
    public sealed class AtlasViewport : MaskableGraphic, IBeginDragHandler, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        public Vector2 Center { get; private set; }
        public float Span { get; private set; } = 820;
        public bool mini, follow = true;
        public int filter;
        public Action<int, bool> Select;
        public Action<Vector3> Waypoint;
        List<Vector3> route;
        float next;
        Vector2 dragStart;
        bool dragged;
        static readonly Color roadColor = new(.28f,.55f,.61f);
        public Vector2 Project(Vector3 p) => new Vector2((p.x-Center.x)/Span, (p.z-Center.y)/Span)*rectTransform.rect.width + rectTransform.rect.center;
        public Vector3 Unproject(Vector2 p) { p=(p-rectTransform.rect.center)/rectTransform.rect.width*Span+Center;return new Vector3(p.x,0,p.y); }
        public void Focus(Vector3 p, float span = -1) { Center=new Vector2(p.x,p.z);if(span>0)Span=Mathf.Clamp(span,160,6500);SetVerticesDirty(); }
        public void Recenter() { follow=true;var g=GameDirector.Instance;if(g&&g.Ready)Focus(g.Player.transform.position,mini?360:820); }
        public void SetRoute(List<Vector3> p) {route=p;SetVerticesDirty();}
        public void Zoom(float factor,Vector2 anchor) {var before=Unproject(anchor);Span=Mathf.Clamp(Span*factor,160,6500);var after=Unproject(anchor);Center+=new Vector2(before.x-after.x,before.z-after.z);follow=false;SetVerticesDirty();}
        public void OnScroll(PointerEventData e) {if(mini)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);Zoom(Mathf.Pow(.83f,e.scrollDelta.y),p);}
        public void OnBeginDrag(PointerEventData e) {dragStart=e.position;dragged=false;}
        public void OnDrag(PointerEventData e) {if(mini)return;dragged|=(e.position-dragStart).sqrMagnitude>16;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position-e.delta,e.pressEventCamera,out var old);Center-=(p-old)*Span/rectTransform.rect.width;Center=new Vector2(Mathf.Clamp(Center.x,-300,2500),Mathf.Clamp(Center.y,-4850,1400));follow=false;SetVerticesDirty();}
        bool VisibleLandmark(int i)=>filter!=2||i==1||i==2||i==4||i==12||i==14||i==20;
        public void OnPointerClick(PointerEventData e)
        {
            if(mini||dragged)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);
            float best=24;int id=-1;bool expanded=false;
            for(int i=0;i<UrbanCatalog.SiteCount;i++){if(filter==2)continue;float d=Vector2.Distance(p,Project(UrbanCatalog.Center(i)));if(d<best){best=d;id=i;}}
            for(int i=0;i<ExpansionWorld.Places.Length;i++){if(!VisibleLandmark(i))continue;float d=Vector2.Distance(p,Project(ExpansionWorld.Places[i]));if(d<best){best=d;id=i;expanded=true;}}
            if(id>=0)Select?.Invoke(id,expanded);else Waypoint?.Invoke(Unproject(p));
        }
        void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+(mini?.12f:.08f);
            var g=GameDirector.Instance;if(!g||!g.Ready)return;if(follow)Center=new Vector2(g.Player.transform.position.x,g.Player.transform.position.z);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Quad(vh,rectTransform.rect,new Color(.025f,.085f,.12f));
            Land(vh,new Rect(0,-660,2200,1760),new Color(.07f,.15f,.17f));
            foreach(var land in NeonHarbor.Land)Land(vh,land,new Color(.085f,.17f,.2f));
            float grid=Span<1000?100:500;var lo=Unproject(rectTransform.rect.min);var hi=Unproject(rectTransform.rect.max);
            for(float x=Mathf.Floor(lo.x/grid)*grid;x<hi.x;x+=grid)Line(vh,new(x,0,lo.z),new(x,0,hi.z),new Color(.12f,.23f,.26f),.4f);
            for(float z=Mathf.Floor(lo.z/grid)*grid;z<hi.z;z+=grid)Line(vh,new(lo.x,0,z),new(hi.x,0,z),new Color(.12f,.23f,.26f),.4f);
            foreach(var road in ExpansionRoads.Roads)for(int i=1;i<road.Length;i++)Line(vh,road[i-1],road[i],roadColor,mini?1:1.7f);
            for(int x=0;x<6;x++)Line(vh,CityRoadNetwork.Junction(x,0),CityRoadNetwork.Junction(x,4),roadColor,mini?1:1.7f);
            for(int z=0;z<5;z++)Line(vh,CityRoadNetwork.Junction(0,z),CityRoadNetwork.Junction(5,z),roadColor,mini?1:1.7f);
            if(filter!=1){Line(vh,NeonHarbor.OldDock,NeonHarbor.NewDock,new Color(.25f,.54f,.84f),.8f);Line(vh,new(1400,0,800),new(1860,0,-3990),new Color(.5f,.43f,.73f),.8f);}
            if(route!=null)for(int i=1;i<route.Count;i++)Line(vh,route[i-1],route[i],new Color(1,.72f,.28f),mini?1.4f:2.4f);
            if(filter!=2)for(int i=0;i<UrbanCatalog.SiteCount;i++)Pin(vh,UrbanCatalog.Center(i),mini?2:4,new Color(.55f,.77f,.78f));
            for(int i=0;i<ExpansionWorld.Places.Length;i++)if(VisibleLandmark(i))Pin(vh,ExpansionWorld.Places[i],mini?3:6,new Color(.34f,.77f,.96f));
            var sim=UrbanSimulation.Instance;if(!mini&&sim)foreach(var c in sim.Cars)if(c&&c.GetComponent<IntercityService>())Pin(vh,c.transform.position,5,new Color(.83f,.55f,1));
            var g=GameDirector.Instance;if(g&&g.Ready){var p=Project(g.Player.transform.position);if(rectTransform.rect.Contains(p)){Pin(vh,g.Player.transform.position,mini?5:9,new Color(1,.86f,.36f));var f=g.CameraRig.ViewRight;Line(vh,g.Player.transform.position,g.Player.transform.position+Vector3.Cross(f,Vector3.up)*Span*.035f,new Color(1,.86f,.36f),1.5f);}}
        }
        void Land(VertexHelper vh,Rect r,Color c) {var a=Project(new(r.xMin,0,r.yMin));var b=Project(new(r.xMax,0,r.yMax));Quad(vh,Rect.MinMaxRect(a.x,a.y,b.x,b.y),c);}
        void Pin(VertexHelper vh,Vector3 p,float radius,Color c) {var a=Project(p);Quad(vh,new Rect(a.x-radius,a.y-radius,radius*2,radius*2),c);}
        void Quad(VertexHelper vh,Rect r,Color c)
        {
            var b=rectTransform.rect;r=Rect.MinMaxRect(Mathf.Max(r.xMin,b.xMin),Mathf.Max(r.yMin,b.yMin),Mathf.Min(r.xMax,b.xMax),Mathf.Min(r.yMax,b.yMax));if(r.width<=0||r.height<=0)return;
            int k=vh.currentVertCount;vh.AddVert(new(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new(r.xMin,r.yMax),c,Vector2.zero);vh.AddVert(new(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new(r.xMax,r.yMin),c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
        void Line(VertexHelper vh,Vector3 aa,Vector3 bb,Color c,float width)
        {
            var a=Project(aa);var b=Project(bb);var d=b-a;float t0=0,t1=1;var r=rectTransform.rect;
            if(!Clip(-d.x,a.x-r.xMin,ref t0,ref t1)||!Clip(d.x,r.xMax-a.x,ref t0,ref t1)||!Clip(-d.y,a.y-r.yMin,ref t0,ref t1)||!Clip(d.y,r.yMax-a.y,ref t0,ref t1))return;
            b=a+d*t1;a+=d*t0;var n=new Vector2(-d.y,d.x).normalized*width;int k=vh.currentVertCount;vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
        static bool Clip(float p,float q,ref float a,ref float b){if(Mathf.Abs(p)<.00001f)return q>=0;float t=q/p;if(p<0){if(t>b)return false;a=Mathf.Max(a,t);}else{if(t<a)return false;b=Mathf.Min(b,t);}return true;}
    }
}
