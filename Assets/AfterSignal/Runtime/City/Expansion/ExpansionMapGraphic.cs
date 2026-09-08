using UnityEngine;using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class ExpansionMapGraphic:MaskableGraphic
    {
        Vector2 Point(Vector3 p){var m=ExpansionRoads.Map(p);return new Vector2(m.x*rectTransform.rect.width,-(1-m.y)*rectTransform.rect.height);}
        void Line(VertexHelper vh,Vector3 aa,Vector3 bb,Color c,float width)
        {
            var a=Point(aa);var b=Point(bb);var n=new Vector2(-(b-a).y,(b-a).x).normalized*width;int k=vh.currentVertCount;vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float w=rectTransform.rect.width>500?2:1;
            foreach(var road in ExpansionRoads.Roads)for(int i=1;i<road.Length;i++)Line(vh,road[i-1],road[i],new Color(.31f,.56f,.62f),w);
            for(int x=0;x<6;x++)Line(vh,CityRoadNetwork.Junction(x,0),CityRoadNetwork.Junction(x,4),new Color(.23f,.4f,.44f),w*.75f);
            for(int z=0;z<5;z++)Line(vh,CityRoadNetwork.Junction(0,z),CityRoadNetwork.Junction(5,z),new Color(.23f,.4f,.44f),w*.75f);
            Line(vh,new Vector3(1315,0,910),new Vector3(2145,0,910),new Color(.65f,.66f,.51f),w*2);
            Line(vh,new Vector3(1370,0,-560),new Vector3(2080,0,-560),new Color(.3f,.65f,.72f),w*2);
            for(int i=0;i<ExpansionWorld.Places.Length;i++){var p=ExpansionWorld.Places[i];Line(vh,p-Vector3.right*5,p+Vector3.right*5,new Color(.9f,.64f,.32f),w*2);}
        }
    }
}
