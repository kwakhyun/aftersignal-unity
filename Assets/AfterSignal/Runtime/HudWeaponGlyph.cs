using UnityEngine;
using UnityEngine.UI;
namespace AfterSignal
{
    public sealed class HudWeaponGlyph:MaskableGraphic
    {
        WeaponId weapon;public void SetWeapon(WeaponId value){if(weapon==value)return;weapon=value;SetVerticesDirty();}
        void Quad(VertexHelper vh,float x,float y,float w,float h){int i=vh.currentVertCount;Point(vh,x,y);Point(vh,x,y+h);Point(vh,x+w,y+h);Point(vh,x+w,y);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i+2,i+3,i);}
        void Point(VertexHelper vh,float x,float y){var r=rectTransform.rect;vh.AddVert(new Vector3(r.x+x*r.width,r.y+y*r.height),color,Vector2.zero);}
        protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();if(weapon==WeaponId.Pistol){Quad(vh,.16f,.49f,.69f,.24f);Quad(vh,.16f,.16f,.21f,.36f);Quad(vh,.72f,.58f,.19f,.12f);Quad(vh,.39f,.3f,.05f,.24f);return;}
            Point(vh,.24f,.34f);Point(vh,.3f,weapon==WeaponId.Greatsword?.78f:.52f);Point(vh,.88f,.84f);Point(vh,.75f,weapon==WeaponId.Greatsword?.42f:.5f);vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);Quad(vh,.17f,.18f,.09f,.31f);Quad(vh,.12f,.38f,.26f,.08f);
        }
    }
}
