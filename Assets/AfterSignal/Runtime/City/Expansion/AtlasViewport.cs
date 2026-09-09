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
        public int category;
        public Action<CityVenue> SelectVenue;
        public Action<int, bool> Select;
        public Action<Vector3> Waypoint;
        public Action ClearWaypoint;
        List<Vector3> route;
        float next;
        Vector2 dragStart;
        bool dragged;
        static readonly Color roadColor = new(.28f,.55f,.61f);
        public Vector2 Project(Vector3 p) => new Vector2((p.x-Center.x)/Span, (p.z-Center.y)/Span)*rectTransform.rect.width + rectTransform.rect.center;
        public Vector3 Unproject(Vector2 p) { p=(p-rectTransform.rect.center)/rectTransform.rect.width*Span+Center;return new Vector3(p.x,0,p.y); }
        public void Focus(Vector3 p, float span = -1) { Center=new Vector2(p.x,p.z);if(span>0)Span=Mathf.Clamp(span,120,9800);SetVerticesDirty(); }
        public void Recenter() { follow=true;var g=GameDirector.Instance;if(g&&g.Ready)Focus(g.Player.transform.position,mini?360:820); }
        public void SetRoute(List<Vector3> p) {route=p;SetVerticesDirty();}
        public void Zoom(float factor,Vector2 anchor) {var before=Unproject(anchor);Span=Mathf.Clamp(Span*factor,120,9800);var after=Unproject(anchor);Center+=new Vector2(before.x-after.x,before.z-after.z);follow=false;SetVerticesDirty();}
        public void OnScroll(PointerEventData e) {if(mini)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);Zoom(Mathf.Pow(.83f,e.scrollDelta.y),p);}
        public void OnBeginDrag(PointerEventData e) {dragStart=e.position;dragged=false;}
        public void OnDrag(PointerEventData e) {if(mini)return;dragged|=(e.position-dragStart).sqrMagnitude>16;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position-e.delta,e.pressEventCamera,out var old);Center-=(p-old)*Span/rectTransform.rect.width;Center=new Vector2(Mathf.Clamp(Center.x,-300,5700),Mathf.Clamp(Center.y,-6500,2650));follow=false;SetVerticesDirty();}
        public bool Visible(CityVenue v)=>category==0||category==1&&v.Sport||category==2&&(v.kind is VenueKind.Garden or VenueKind.Monument or VenueKind.Cinema or VenueKind.Museum or VenueKind.Amusement)||category==3&&v.kind==VenueKind.Hotel||category==5&&FourCityCampaign.Contains(Array.IndexOf(FourCityCatalog.Venues,v))||category==6&&(v.kind is VenueKind.Hospital or VenueKind.Market or VenueKind.Civic or VenueKind.Research);
        bool VisibleLandmark(int i)=>filter!=2||i==1||i==2||i==4||i==12||i==14||i==20;
        public void OnPointerClick(PointerEventData e)
        {
            if(!mini&&e.button==PointerEventData.InputButton.Right){ClearWaypoint?.Invoke();return;}
            if(mini||dragged)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);
            float best=24;int id=-1;bool expanded=false;
            foreach(var incident in CityIncidentBoard.Active)if(Vector2.Distance(p,Project(incident.position))<18){Waypoint?.Invoke(incident.position);GameDirector.Instance?.Toast(incident.title+" · 현장 지원 시 기여 보상",5);return;}
            CityVenue selected=null;foreach(var venue in FourCityCatalog.Venues)if(Visible(venue)){float d=Vector2.Distance(p,Project(venue.position));if(d<best){best=d;selected=venue;}}
            if(selected!=null){SelectVenue?.Invoke(selected);return;}
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
            for(int i=0;i<FourCityCatalog.Land.Length;i++)Land(vh,FourCityCatalog.Land[i],FourCityCatalog.Land[i].xMin>2600?new Color(.17f,.105f,.20f):new Color(.08f,.17f,.18f));
            Disc(vh,FourCityCatalog.Centers[3],1030,930,new Color(.06f,.21f,.29f),64);
            if(!mini&&Span<1900)foreach(var building in HighriseBuilding.All)if(building){var b=building.Bounds;Land(vh,new Rect(b.min.x,b.min.z,b.size.x,b.size.z),new Color(.17f,.28f,.31f));}
            foreach(var venue in FourCityCatalog.Venues){var color=FourCityAtlasSelection.Color(venue.kind)*.28f;color.a=1;Land(vh,new Rect(venue.position.x-venue.size.x*.5f,venue.position.z-venue.size.y*.5f,venue.size.x,venue.size.y),color);if(venue.Sport)Disc(vh,venue.position,venue.size.x*.32f,venue.size.y*.32f,new Color(.1f,.28f,.22f),24);}
            float grid=Span<1000?100:500;var lo=Unproject(rectTransform.rect.min);var hi=Unproject(rectTransform.rect.max);
            for(float x=Mathf.Floor(lo.x/grid)*grid;x<hi.x;x+=grid)Line(vh,new(x,0,lo.z),new(x,0,hi.z),new Color(.12f,.23f,.26f),.4f);
            for(float z=Mathf.Floor(lo.z/grid)*grid;z<hi.z;z+=grid)Line(vh,new(lo.x,0,z),new(hi.x,0,z),new Color(.12f,.23f,.26f),.4f);
            foreach(var road in ExpansionRoads.Roads)for(int i=1;i<road.Length;i++)Line(vh,road[i-1],road[i],roadColor,mini?1:1.7f);
            foreach(var road in FourCityCatalog.Roads)for(int i=1;i<road.Length;i++)Line(vh,road[i-1],road[i],road[i].y<0?new Color(.3f,.8f,.88f):roadColor,mini?1:2.1f);
            for(int x=0;x<6;x++)Line(vh,CityRoadNetwork.Junction(x,0),CityRoadNetwork.Junction(x,4),roadColor,mini?1:1.7f);
            for(int z=0;z<5;z++)Line(vh,CityRoadNetwork.Junction(0,z),CityRoadNetwork.Junction(5,z),roadColor,mini?1:1.7f);
            if(filter!=1){Line(vh,NeonHarbor.OldDock,NeonHarbor.NewDock,new Color(.25f,.54f,.84f),.8f);Line(vh,new(1400,0,800),new(1860,0,-3990),new Color(.5f,.43f,.73f),.8f);}
            if(route!=null)
            {
                for(int i=1;i<route.Count;i++){Line(vh,route[i-1],route[i],new Color(.02f,.09f,.11f),mini?3.2f:5);Line(vh,route[i-1],route[i],new Color(.29f,.88f,.77f),mini?1.7f:2.8f);}
                if(route.Count>0){var p=route[^1];var xy=Project(p);if(rectTransform.rect.Contains(xy)){Disc(vh,p,Span*.012f,Span*.012f,new Color(1,.76f,.3f,.25f),20);Diamond(vh,xy,mini?6:11,new Color(1,.81f,.34f));Diamond(vh,xy,mini?2:4,new Color(.04f,.09f,.12f));}}
                if(!mini)for(int i=1;i<route.Count;i++){var a=Project(route[i-1]);var b=Project(route[i]);float length=(b-a).magnitude;for(float n=34;n<length;n+=60){var p=Vector2.Lerp(a,b,n/length);if(rectTransform.rect.Contains(p))Arrow(vh,p,(b-a).normalized,5,new Color(.88f,1,.96f));}}
            }
            if(filter!=2)for(int i=0;i<UrbanCatalog.SiteCount;i++)Pin(vh,UrbanCatalog.Center(i),mini?2:4,new Color(.55f,.77f,.78f));
            for(int i=0;i<ExpansionWorld.Places.Length;i++)if(VisibleLandmark(i))Pin(vh,ExpansionWorld.Places[i],mini?3:6,new Color(.34f,.77f,.96f));
            foreach(var venue in FourCityCatalog.Venues)if(Visible(venue)){Pin(vh,venue.position,mini?4:9,FourCityAtlasSelection.Color(venue.kind));if(FourCityAtlasSelection.Venue==venue)Disc(vh,venue.position,Span*.014f,Span*.014f,new Color(1,.87f,.42f,.35f),24);}
            var sim=UrbanSimulation.Instance;if(!mini&&sim)foreach(var c in sim.Cars)if(c&&c.GetComponent<IntercityService>())Pin(vh,c.transform.position,5,new Color(.83f,.55f,1));
            foreach(var incident in CityIncidentBoard.Active){var tint=incident.color;tint.a=.18f;Disc(vh,incident.position,incident.radius,incident.radius,tint,20);Pin(vh,incident.position,mini?5:9,incident.color);}
            var g=GameDirector.Instance;if(g&&g.Ready){var p=Project(g.Player.transform.position);if(rectTransform.rect.Contains(p)){var f=g.CameraRig.LookForward;var d=new Vector2(f.x,f.z);Arrow(vh,p,d,mini?10:16,new Color(.015f,.045f,.06f));Arrow(vh,p,d,mini?7:12,new Color(1,.9f,.55f));}}
        }
        void Diamond(VertexHelper vh,Vector2 p,float size,Color color){int k=vh.currentVertCount;vh.AddVert(p+Vector2.up*size,color,Vector2.zero);vh.AddVert(p+Vector2.right*size,color,Vector2.zero);vh.AddVert(p+Vector2.down*size,color,Vector2.zero);vh.AddVert(p+Vector2.left*size,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);}
        void Arrow(VertexHelper vh,Vector2 p,Vector2 d,float size,Color color){d.Normalize();var r=new Vector2(-d.y,d.x);int k=vh.currentVertCount;vh.AddVert(p+d*size,color,Vector2.zero);vh.AddVert(p-d*size*.7f+r*size*.6f,color,Vector2.zero);vh.AddVert(p-d*size*.35f,color,Vector2.zero);vh.AddVert(p-d*size*.7f-r*size*.6f,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);}
        void Land(VertexHelper vh,Rect r,Color c) {var a=Project(new(r.xMin,0,r.yMin));var b=Project(new(r.xMax,0,r.yMax));Quad(vh,Rect.MinMaxRect(a.x,a.y,b.x,b.y),c);}
        void Disc(VertexHelper vh,Vector3 center,float rx,float rz,Color color,int segments)
        {
            var p=Project(center);float sx=rx/Span*rectTransform.rect.width,sy=rz/Span*rectTransform.rect.width;var rect=rectTransform.rect;
            // Clipped horizontal strips retain the land layer when the ellipse centre is offscreen.
            float low=Mathf.Max(rect.yMin,p.y-sy),high=Mathf.Min(rect.yMax,p.y+sy),step=Mathf.Max(1,(high-low)/128);
            for(float y=low;y<high;y+=step){float t=((y+Mathf.Min(step,high-y)*.5f)-p.y)/sy;float extent=sx*Mathf.Sqrt(Mathf.Max(0,1-t*t));Quad(vh,Rect.MinMaxRect(p.x-extent,y,p.x+extent,Mathf.Min(y+step,high)),color);}
        }
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
