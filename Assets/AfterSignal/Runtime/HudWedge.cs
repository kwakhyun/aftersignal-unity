using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class HudWedge:Image
    {
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;float bevel=Mathf.Min(12,r.width*.3f);vh.AddVert(new Vector3(r.xMin+bevel,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax-bevel,r.yMin),color,Vector2.zero);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);}
    }
}
