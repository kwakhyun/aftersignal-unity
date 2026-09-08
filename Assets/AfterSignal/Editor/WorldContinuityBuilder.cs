using System;using System.Linq;using UnityEngine;using UnityEditor;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static void WindowWall(Transform parent,Vector3 center,float width,float height)
        {
            int count=Mathf.CeilToInt(width/2.8f);float paneWidth=width/count;
            for(int i=0;i<count;i++)
            {
                var pane=Box(parent,"Breakable facade glazing",center+Vector3.right*(-width*.5f+(i+.5f)*paneWidth),new Vector3(paneWidth-.1f,height,.08f),"Glazing");
                var glass=pane.AddComponent<BreakableGlass>();glass.health=35;glass.campaign=false;
                var lod=pane.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.003f,new[]{pane.GetComponent<Renderer>()})});lod.RecalculateBounds();
            }
        }
        static bool RoadClear(Vector3 center,float w,float d)
        {
            var rect=new Rect(center.x-w*.5f-19,center.z-d*.5f-19,w+38,d+38);
            foreach(var road in NeonHarbor.Roads)foreach(var point in road)if(Mathf.Abs(point.y-center.y)<12&&rect.Contains(new Vector2(point.x,point.z)))return false;
            return true;
        }
        static Vector3 ClearRoadFootprint(Vector3 at,float w,float d)
        {
            if(RoadClear(at,w,d))return at;
            for(int radius=10;radius<=220;radius+=10)for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;var p=at+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                if(!NeonHarbor.OnIsland(p-new Vector3(w*.5f,0,d*.5f))||!NeonHarbor.OnIsland(p+new Vector3(w*.5f,0,d*.5f)))continue;
                if(RoadClear(p,w,d)){Debug.Log("ROAD CLEARANCE: "+at+" -> "+p);return p;}
            }
            throw new Exception("No safe building footprint found at "+at);
        }
        public static void BuildContinuityDistricts()
        {
            BuildMobility();BuildNeonHarbor();AssetDatabase.SaveAssets();
        }
    }
}
