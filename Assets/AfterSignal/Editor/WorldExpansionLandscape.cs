using System.Collections.Generic;using UnityEngine;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static float Shore(float x)
        {float[] xs={0,250,480,780,1050,1250,1450,2200};float[] zs={-510,-545,-610,-655,-590,-580,-565,-565};for(int i=1;i<xs.Length;i++)if(x<=xs[i])return Mathf.Lerp(zs[i-1],zs[i],Mathf.InverseLerp(xs[i-1],xs[i],x));return -565;}
        static void BuildTerrain()
        {
            var p=Group("Coastline / continuous terrain",Vector3.zero);var v=new List<Vector3>();var tris=new List<int>();
            const int bands=64;
            for(int i=0;i<=110;i++)for(int j=0;j<=bands;j++){float x=i*20;v.Add(new Vector3(x,-.12f,Mathf.Lerp(Shore(x),1100,j/(float)bands)));}
            for(int i=0;i<110;i++)for(int j=0;j<bands;j++){int k=i*(bands+1)+j;tris.AddRange(new[]{k,k+1,k+bands+1,k+1,k+bands+2,k+bands+1});}
            MeshObject(p,"Coastal ground",v.ToArray(),tris.ToArray(),"Earth",true);
            var shore=new List<Vector3>();for(int i=0;i<=110;i++)shore.Add(new Vector3(i*20,-.06f,Shore(i*20)+20));Ribbon(p,"Sloping sand strand",shore.ToArray(),48,0,0,"Sand",true);
            v.Clear();tris.Clear();int cols=100,rows=45;for(int z=0;z<=rows;z++)for(int x=0;x<=cols;x++)v.Add(new Vector3(-900+x*40,-.95f,-2250+z*40));for(int z=0;z<rows;z++)for(int x=0;x<cols;x++){int k=z*(cols+1)+x;tris.AddRange(new[]{k,k+cols+1,k+1,k+1,k+cols+1,k+cols+2});}MeshObject(p,"Open sea / animated waves",v.ToArray(),tris.ToArray(),"Ocean");
            // Pavement connects every original eastern avenue to the new terrain without a lip.
            Box(p,"Eastern city transition",new Vector3(790,-.16f,0),new Vector3(90,.1f,620),"Pavement");
        }
        static void BuildRoads()
        {
            var p=Group("Roads / waterfront, ring roads and diagonal connectors",Vector3.zero);
            for(int r=0;r<ExpansionRoads.Roads.Count;r++)
            {
                var path=ExpansionRoads.Roads[r];var road=Group("Route "+r,Vector3.zero,p);
                Ribbon(road,"Asphalt carriageway",path,22,0,0,"Asphalt",true);
                Ribbon(road,"Continuous pavement left",path,6,14,.07f,"Pavement",true);Ribbon(road,"Continuous pavement right",path,6,-14,.07f,"Pavement",true);
                Ribbon(road,"Kerb left",path,.22f,11.05f,.12f,"PaintWhite");Ribbon(road,"Kerb right",path,.22f,-11.05f,.12f,"PaintWhite");
                Ribbon(road,"Lane edge left",path,.13f,9.3f,.008f,"PaintWhite");Ribbon(road,"Lane edge right",path,.13f,-9.3f,.008f,"PaintWhite");
                for(int i=1;i<path.Length-1;i++)
                {
                    var d=(path[i+1]-path[i-1]).normalized;var side=Vector3.Cross(Vector3.up,d);
                    if(i%2==0){var stripe=Box(road,"Centre dash",path[i]+Vector3.up*.015f,new Vector3(.16f,.018f,4),"PaintAmber",false);stripe.transform.rotation=Quaternion.LookRotation(d);}
                    if(i%7==2)
                    {
                        for(int s=-1;s<=1;s+=2){var at=path[i]+side*s*14.5f;Model("street_lamp_01",road,at,1.8f,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+90*s);Box(road,"Pavement light",at+Vector3.up*.05f,new Vector3(.22f,.04f,1.2f),"NeonCyan",false);}
                    }
                }
            }
            // Portal signs and sculpted retaining walls mark the transition from the old core.
            foreach(var at in new[]{new Vector3(795,0,0),new Vector3(773,0,320),new Vector3(755,0,-150)})
            {Beam(p,"Sign support",at+new Vector3(0,0,-13),at+new Vector3(0,8,-13),.28f,"Steel");Box(p,"Direction gantry",at+new Vector3(0,7.4f,0),new Vector3(.3f,2,25),"Steel",false);Text(p,"COAST  ←   /   AIRPORT  →",at+new Vector3(-.2f,7.5f,0),.16f,"NeonCyan",-90);}
        }
        static bool Free(Vector3 at,float radius)
        {if(!ExpansionRoads.Outside(at)||at.z<Shore(at.x)+75)return false;var near=ExpansionRoads.Nearest(at,out _,out _);return Vector3.Distance(near,at)>radius+24;}
        static void BuildNeighborhoods()
        {
            var district=Group("District / neon terraces and mixed skyline",Vector3.zero);
            int serial=0;for(int ix=0;ix<6;ix++)for(int iz=0;iz<5;iz++)
            {
                var at=new Vector3(835+ix*80,0,-80+iz*87);float width=24+(ix+iz)%3*5;if(!Free(at,width*.7f))continue;
                Tower(district,at,width,27+(iz%3)*8,28+(ix*17+iz*11)%95,serial++);
            }
            for(int i=0;i<7;i++){var at=new Vector3(165+110*i,0,370+(i%2)*40);if(Free(at,22))Tower(district,at,28,30,55+i*10,serial++);}
            for(int i=0;i<30;i++)
            {
                var at=new Vector3(1190+(i%6)*143+(i/6%2)*27,0,-130+(i/6)*104);
                if((at-new Vector3(1370,0,-60)).sqrMagnitude<95*95||!Free(at,26))continue;
                Tower(district,at,22+i%3*5,24+i%4*4,20+(i*19)%65,serial++);
            }
            for(int i=0;i<10;i++)
            {
                float x=310+i*68;var at=new Vector3(x,0,Shore(x)+128);
                if(at.z> -435||!Free(at,19))continue;
                Tower(district,at,30,22,12+i%4*6,serial++);
                Tree(district,at+new Vector3(-21,0,-6),6);Tree(district,at+new Vector3(21,0,6),7);
            }
            // Metro plaza: raised roof, entry stairs, kiosks, shared forecourt.
            var metro=Group("Lumen interchange / public concourse",new Vector3(880,0,80));
            Box(metro,"Station plinth",new Vector3(0,-.03f,0),new Vector3(54,.12f,48),"Pavement");
            Box(metro,"Concourse roof",new Vector3(0,6,0),new Vector3(36,.4f,22),"Cladding");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Beam(metro,"Tapered station column",new Vector3(x*14,0,z*8),new Vector3(x*16,6,z*8),.38f,"Steel",true);
            for(int i=0;i<5;i++){Box(metro,"Ticket gate",new Vector3(-8+i*4,.65f,-2),new Vector3(.55f,1.3f,2.8f),"Aluminium");Box(metro,"Ticket reader",new Vector3(-8+i*4,1.34f,-3),new Vector3(.42f,.06f,.5f),"NeonCyan",false);}
            Text(metro,"LUMEN  /  TRANSIT",new Vector3(0,5.5f,-11.3f),.48f,"NeonCyan",180);
            Crowd(metro,"환승역",new Vector3(-8,.08f,12),24,20,4,15);Service(metro,4,new Vector3(0,1,14));
            // Night market stalls form narrow lanes and a central court, not square blocks.
            var market=Group("Hongryeon / night market alleys",new Vector3(995,0,-185));market.localRotation=Quaternion.Euler(0,-18,0);
            Box(market,"Market courtyard",new Vector3(0,-.03f,0),new Vector3(76,.1f,65),"Pavement");
            for(int i=0;i<12;i++){var stall=Group("Vendor "+i,new Vector3((i%2==0?-1:1)*25,0,(i/2)*9-24),market);Stall(stall,i);}
            for(int i=0;i<4;i++){Beam(market,"Overhead cable",new Vector3(-30,6,-24+i*16),new Vector3(30,6,-24+i*16),.045f,"Steel");for(int j=0;j<9;j++)Box(market,"Lantern",new Vector3(-26+j*6.5f,5.8f,-24+i*16),new Vector3(.45f,.7f,.45f),i%2==0?"NeonRose":"NeonWarm",false,PrimitiveType.Sphere);}
            for(int i=0;i<6;i++)Lamp(market,new Vector3(i%2==0?-19:19,4.3f,-20+i/2*20),i%2==0?new Color(1,.15f,.38f):new Color(.12f,.85f,1),16,19);
            Crowd(market,"홍련 야시장",new Vector3(-7,.08f,-10),36,16,4,18);Service(market,3,new Vector3(-15,1,-36));
            var park=Group("Aurora / skyline observation park",new Vector3(860,0,625));Box(park,"Observation deck",new Vector3(0,0,0),new Vector3(88,.2f,50),"Wood");
            for(int i=0;i<10;i++){Tree(park,new Vector3(-40+i*9,0,24),6+i%3);Model("painted_wooden_bench",park,new Vector3(-32+i*7,0,-17),1.3f,180);}
            Text(park,"AURORA  /  CITY OVERLOOK",new Vector3(0,4,20),.55f,"NeonCyan",180);Crowd(park,"오로라 전망공원",new Vector3(-12,.15f,0),20,4,4,23);Service(park,5,new Vector3(0,1,-20));
        }
        static void Tower(Transform parent,Vector3 at,float w,float d,float height,int serial)
        {
            var p=Group("Mixed-use tower "+serial,at,parent);p.localRotation=Quaternion.Euler(0,serial%3==0?14:serial%3==1?-9:0,0);
            Box(p,"Structural core",new Vector3(0,height/2,0),new Vector3(w,height,d),"WindowDark");
            for(int face=0;face<4;face++)
            {
                bool front=face<2;float side=face%2==0?-1:1;
                var panel=Box(p,"Textured recessed facade",front?new Vector3(0,height/2,side*(d/2+.02f)):new Vector3(side*(w/2+.02f),height/2,0),new Vector3(front?w:d,height,1),"UrbanFacade"+(serial%8),false,PrimitiveType.Quad);
                panel.transform.localRotation=Quaternion.Euler(0,front?(side<0?0:180):(side<0?90:-90),0);
            }
            Box(p,"Roof parapet",new Vector3(0,height+.5f,0),new Vector3(w+1,1,d+1),"Steel");
            for(int floor=0;floor<Mathf.Min(16,(int)height/4);floor++)
            {
                float y=floor*4+1;
                Box(p,"Floor slab front",new Vector3(0,y,d*.5f+.1f),new Vector3(w+.8f,.25f,.45f),"Aluminium",false);Box(p,"Floor slab rear",new Vector3(0,y,-d*.5f-.1f),new Vector3(w+.8f,.25f,.45f),"Aluminium",false);
                for(int j=0;j<4;j++)
                {
                    float x=(j-1.5f)*(w/4);
                    Model("modular_urban_apartments_facade",p,new Vector3(x,y,-d*.5f-.18f),1.5f,180,"window_centered_large_01");
                    Box(p,"Office warm glazing",new Vector3(x,y+1.3f,d*.5f+.04f),new Vector3(w/4-.6f,2.2f,.04f),(floor+j+serial)%5==0?"NeonWarm":"WindowDark",false);
                }
                for(int s=-1;s<=1;s+=2){if(floor==0)Box(p,"Vertical facade fin",new Vector3(s*w*.5f, height*.5f,0),new Vector3(.32f,height,d+.7f),"Steel",false);if(floor%3==0){Box(p,"Balcony",new Vector3(s*(w*.5f+1),y,0),new Vector3(2,.22f,d*.5f),"Cladding");for(int rail=0;rail<5;rail++)Beam(p,"Balcony railing",new Vector3(s*(w*.5f+1.9f),y,rail*3-d*.23f),new Vector3(s*(w*.5f+1.9f),y+1.1f,rail*3-d*.23f),.07f,"Aluminium");}}
            }
            for(int i=0;i<3;i++){Box(p,"Rooftop plant enclosure",new Vector3(-w*.25f+i*5,height+1.2f,0),new Vector3(3,2.2f,4),"Aluminium");for(int k=0;k<5;k++)Box(p,"Vent louvers",new Vector3(-w*.25f+i*5,height+.4f+k*.3f,-2.02f),new Vector3(2.6f,.09f,.1f),"Steel",false);}
            Beam(p,"Antenna mast",new Vector3(w*.3f,height,0),new Vector3(w*.3f,height+10,0),.15f,"Steel");Box(p,"Obstruction beacon",new Vector3(w*.3f,height+10,0),Vector3.one*.4f,"NeonRose",false,PrimitiveType.Sphere);
            for(int s=-1;s<=1;s+=2)Box(p,"Vertical neon edge",new Vector3(s*w*.49f,height*.6f,-d*.5f-.4f),new Vector3(.14f,height*.75f,.08f),serial%2==0?"NeonCyan":"NeonRose",false);
            Text(p,new[]{"LUMEN","SIGNAL","AURORA","NEON WORKS","AFTERLIGHT"}[serial%5],new Vector3(0,5.8f,-d*.5f-.45f),.44f,"NeonCyan",180);
            Model("modular_urban_apartments_facade",p,new Vector3(0,0,-d*.5f-.3f),1.5f,180,"door_centered_large_01");
            for(int s=-1;s<=1;s+=2){Model("industrial_wall_lamp",p,new Vector3(s*5,3,-d*.5f-.5f),2,180);Model("painted_wooden_bench",p,new Vector3(s*7,0,-d*.5f-3),1.3f,180);}
            if(serial%3==0)Lamp(p,new Vector3(0,4,-d*.5f-2),serial%2==0?new Color(.1f,.85f,1):new Color(1,.13f,.4f),8,14);
        }
        static void Tree(Transform p,Vector3 at,float height)
        {Beam(p,"Tree trunk",at,at+Vector3.up*height,.24f,"Trunk",true);for(int i=0;i<5;i++){float a=i*1.256f;var b=at+new Vector3(Mathf.Cos(a)*2,height*.85f,Mathf.Sin(a)*2);Beam(p,"Branch",at+Vector3.up*height*.65f,b,.12f,"Trunk");Box(p,"Tree crown",b,new Vector3(3.2f,2.6f,3.4f),"Leaf",false,PrimitiveType.Sphere);}}
        static void Stall(Transform p,int i)
        {Box(p,"Counter",new Vector3(0,.6f,0),new Vector3(7,1.2f,3),"Steel");Box(p,"Canvas awning",new Vector3(0,3.4f,0),new Vector3(8,.14f,6),i%2==0?"CargoRed":"CargoBlue",false);for(int s=-1;s<=1;s+=2)Beam(p,"Awning pole",new Vector3(s*3.5f,0,1.2f),new Vector3(s*3.5f,3.4f,1.2f),.06f,"Aluminium");for(int j=0;j<5;j++){Box(p,"Bowl",new Vector3(j*1.2f-2.4f,1.3f,-.7f),new Vector3(.55f,.15f,.55f),"Cladding",false,PrimitiveType.Sphere);Box(p,"Food pot",new Vector3(j*1.2f-2.4f,1.35f,.55f),new Vector3(.45f,.22f,.45f),"Aluminium",false,PrimitiveType.Cylinder);}Text(p,new[]{"NOODLES","NIGHT CAFE","RAMEN","SPARE PARTS"}[i%4],new Vector3(0,2.8f,-2.4f),.24f,"NeonWarm",180);}
    }
}
