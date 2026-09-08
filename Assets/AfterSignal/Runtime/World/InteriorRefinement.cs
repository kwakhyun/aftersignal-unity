using System.Collections;
using UnityEngine;
namespace AfterSignal
{
    public sealed class InteriorRefinement:MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;yield return null;
            var g=GameDirector.Instance;if(!g||!CivicWorld.Interior(g.stage))yield break;
            // Preserve the established playable floor plan and routines; detail their actual furnishings.
            var parent=new GameObject("Interior / furniture hardware and architectural finishes").transform;
            var renderers=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            int count=0;
            foreach(var r in renderers)
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||count>170)continue;
                string n=r.name.ToLowerInvariant();var b=r.bounds;
                if(n.EndsWith(" floor")&&b.size.x>10&&b.size.z>10&&b.size.x<70&&b.size.z<40)
                {
                    RoomFinishes(parent,b,n);
                    continue;
                }
                if(b.size.x>12||b.size.z>9||b.size.y>4)continue;
                if(n.Contains("desk")||n.Contains("table top")||n.Contains("worktop"))
                {
                    var p=b.center+Vector3.up*(b.extents.y+.025f);count++;
                    Piece(parent,"Desktop organizer",p+new Vector3(b.extents.x*.5f,.08f,0),new Vector3(.28f,.16f,.2f),"Chrome");
                    Piece(parent,"Document stack",p+new Vector3(-.25f,.025f,0),new Vector3(.33f,.05f,.25f),"LightTile");
                    Piece(parent,"Table rim",new Vector3(b.center.x,b.max.y,b.min.z-.012f),new Vector3(b.size.x,.035f,.025f),"Chrome");
                }
                else if(n.Contains("bed")&&b.size.z>1.1f&&b.size.y<1.4f)
                {
                    count++;for(int s=-1;s<=1;s+=2)Piece(parent,"Bed safety rail",new Vector3(b.center.x+s*b.extents.x,b.max.y+.22f,b.center.z),new Vector3(.045f,.06f,b.size.z*.75f),"Chrome");
                    Piece(parent,"Folded linen",new Vector3(b.center.x,b.max.y+.08f,b.min.z+.45f),new Vector3(b.size.x*.8f,.15f,.48f),"SoftCloth");
                    if(n.Contains("frame")&&(g.stage==StageId.Clinic||g.stage==StageId.UrbanInterior&&UrbanCatalog.Kind(UrbanCatalog.Current)==4))
                    {
                        Vector3 at=new Vector3(b.max.x+.7f,.8f,b.max.z-.2f);
                        Piece(parent,"Vitals monitor arm",at+Vector3.up*.6f,new Vector3(.055f,1.2f,.055f),"Chrome");
                        Piece(parent,"Vitals monitor",at+Vector3.up*1.1f,new Vector3(.74f,.51f,.16f),"DistrictWindow");
                        for(int j=0;j<5;j++)Piece(parent,"Vitals ECG trace",at+new Vector3(0,1+j*.055f,-.09f),new Vector3(.6f,.015f,.02f),"CyanFX");
                        Piece(parent,"Mobile bedside cabinet",at+Vector3.down*.45f,new Vector3(.7f,.7f,.65f),"Enamel");
                    }
                }
                else if(n.Contains("cabinet")||n.Contains("locker"))
                {
                    count++;for(int i=0;i<3;i++)Piece(parent,"Cabinet drawer pull",new Vector3(b.center.x,b.min.y+b.size.y*(.25f+i*.25f),b.min.z-.04f),new Vector3(Mathf.Min(.45f,b.size.x*.4f),.035f,.07f),"Chrome");
                }
            }
            if(CommunityWorld.Instance)
            {
                foreach(var c in CommunityWorld.Instance.RoomCenters)
                {
                    Piece(parent,"Recessed linear ceiling luminaire",c+Vector3.up*3.7f,new Vector3(3,.08f,.28f),"DistrictLight");
                    Piece(parent,"Ceiling air grille",c+new Vector3(2.5f,3.7f,0),new Vector3(.8f,.06f,.8f),"Metal");
                    for(int i=0;i<6;i++)Piece(parent,"Grille louver",c+new Vector3(2.5f,3.66f,-.32f+i*.13f),new Vector3(.74f,.02f,.025f),"Chrome");
                }
            }
        }
        static void RoomFinishes(Transform p,Bounds floor,string title)
        {
            float y=floor.max.y;
            for(int side=-1;side<=1;side+=2)
            {
                float x=floor.center.x+side*(floor.extents.x-.16f);
                Piece(p,"Architectural skirting",new Vector3(x,y+.16f,floor.center.z),new Vector3(.1f,.3f,floor.size.z-.5f),"WarmWood");
                Piece(p,"Wall protection rail",new Vector3(x,y+.95f,floor.center.z),new Vector3(.12f,.11f,floor.size.z-.6f),"Chrome");
                Piece(p,"Ceiling perimeter coffer",new Vector3(x-side*.35f,y+3.7f,floor.center.z),new Vector3(.75f,.2f,floor.size.z-.6f),"Enamel");
                Piece(p,"Concealed warm cove light",new Vector3(x-side*.78f,y+3.66f,floor.center.z),new Vector3(.05f,.05f,floor.size.z-.8f),"DistrictLight");
                for(int j=0;j<Mathf.FloorToInt(floor.size.z/4);j++)
                    Piece(p,"Wall acoustic panel joint",new Vector3(x-side*.06f,y+2.1f,floor.min.z+2+j*4),new Vector3(.045f,2.4f,.035f),"Metal");
            }
            var end=new Vector3(floor.center.x,y+2.2f,floor.center.z+Mathf.Sign(floor.center.z)*(floor.extents.z-.3f));
            if(title.Contains("교실"))
            {
                Piece(p,"Classroom projection screen frame",end,new Vector3(8,2.6f,.13f),"Chrome");
                Piece(p,"Classroom projection surface",end+Vector3.back*.08f,new Vector3(7.7f,2.3f,.02f),"LightTile");
                Piece(p,"Classroom clock housing",end+new Vector3(6,.25f,-.1f),new Vector3(.6f,.6f,.09f),"Enamel");
            }
            else if(title.Contains("작전")||title.Contains("분석"))
            {
                for(int i=0;i<4;i++)
                {
                    var at=end+Vector3.right*(i-1.5f)*2.8f;
                    Piece(p,"Operations video wall bezel",at,new Vector3(2.75f,1.85f,.18f),"Metal");
                    Piece(p,"Operations live display",at+Vector3.back*.1f,new Vector3(2.58f,1.67f,.02f),"DistrictWindow");
                    for(int line=0;line<7;line++)Piece(p,"Signal telemetry",at+new Vector3(0,-.6f+line*.18f,-.125f),new Vector3(2.2f,.023f,.015f),"CyanFX");
                }
            }
        }
        static void Piece(Transform p,string name,Vector3 at,Vector3 size,string mat)=>WorldGeometry.Part(p,name,at,size,mat);
    }
}
