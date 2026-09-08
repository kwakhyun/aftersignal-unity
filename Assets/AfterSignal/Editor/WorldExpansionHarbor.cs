using System.Collections.Generic;using UnityEngine;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static void Container(Transform p,Vector3 at,int id,float length=12)
        {
            var c=Group("ISO freight container "+id,at,p);string mat=id%3==0?"CargoBlue":id%3==1?"CargoRed":"CargoGold";
            Box(c,"Corrugated steel shell",new Vector3(0,1.45f,0),new Vector3(length,2.9f,2.45f),mat);
            for(int i=0;i<Mathf.RoundToInt(length/.45f);i++)for(int side=-1;side<=1;side+=2)Box(c,"Pressed corrugation",new Vector3(-length*.48f+i*.45f,1.43f,side*1.24f),new Vector3(.09f,2.68f,.045f),mat,false);
            for(int s=-1;s<=1;s+=2){Box(c,"Corner post",new Vector3(s*length*.49f,1.45f,0),new Vector3(.14f,2.94f,2.52f),"Steel",false);for(int door=-1;door<=1;door+=2)Beam(c,"Door locking rod",new Vector3(s*(length*.5f+.025f),.25f,door*.62f),new Vector3(s*(length*.5f+.025f),2.65f,door*.62f),.055f,"Aluminium");}
            if(id%4==0)Text(c,"LUMEN  /  "+(40000+id),new Vector3(0,1.8f,-1.28f),.12f,"PaintWhite",180);
        }
        static void BuildHarbor()
        {
            var harbor=Group("Bluewater / working container terminal",Vector3.zero);
            Box(harbor,"Reinforced quay",new Vector3(1720,-.1f,-515),new Vector3(710,.4f,92),"Pavement");Box(harbor,"Quay retaining wall",new Vector3(1720,-2,-561),new Vector3(710,4,1.5f),"Steel");
            for(int i=0;i<18;i++){float x=1380+i*39;Box(harbor,"Quay bollard",new Vector3(x,.45f,-554),new Vector3(.65f,.8f,.8f),"PaintAmber",true,PrimitiveType.Cylinder);Box(harbor,"Rubber fender",new Vector3(x,-.3f,-562),new Vector3(1.3f,2.5f,.4f),"Rubber",false);}
            for(int x=0;x<10;x++)for(int z=0;z<5;z++)for(int tier=0;tier<2+(x+z)%2;tier++)Container(harbor,new Vector3(1480+x*31,tier*2.95f,-360+z*8),(x*15+z*3+tier));
            // Vehicle lanes, rail connection, inspection bays, reefer power pedestals.
            Box(harbor,"Container yard paving",new Vector3(1680,-.045f,-340),new Vector3(450,.12f,112),"Asphalt");
            for(int lane=0;lane<4;lane++)Box(harbor,"Yard lane marking",new Vector3(1680,.02f,-384+lane*28),new Vector3(450,.015f,.14f),"PaintAmber",false);
            for(int s=-1;s<=1;s+=2)Box(harbor,"Freight rail",new Vector3(1760,.08f,-247+s*.72f),new Vector3(680,.12f,.12f),"Steel",false);
            for(int i=0;i<170;i++)Box(harbor,"Railway sleeper",new Vector3(1420+i*4,.015f,-247),new Vector3(.25f,.1f,2.8f),"Wood",false);
            for(int i=0;i<4;i++)Crane(harbor,new Vector3(1490+i*160,0,-525),i);
            Ship(harbor,new Vector3(1730,0,-615));
            for(int i=0;i<3;i++)
            {
                var warehouse=Group("Freight warehouse "+i,new Vector3(1330+i*95,0,-220),harbor);Box(warehouse,"Warehouse shell",new Vector3(0,9,0),new Vector3(65,18,35),"Cladding");
                for(int j=0;j<6;j++){Box(warehouse,"Roller shutter",new Vector3(-26+j*10,3.5f,-17.6f),new Vector3(7,7,.2f),"Steel",false);Box(warehouse,"Loading dock",new Vector3(-26+j*10,.7f,-20),new Vector3(7,1.4f,5),"Pavement");for(int k=0;k<12;k++)Box(warehouse,"Shutter slat",new Vector3(-26+j*10,.35f+k*.57f,-17.76f),new Vector3(7,.06f,.08f),"Aluminium",false);}
                Text(warehouse,"BLUEWATER  /  LOGISTICS "+(i+1),new Vector3(0,12,-17.8f),.65f,"NeonWarm",180);
                for(int j=0;j<14;j++)Beam(warehouse,"Portal roof frame",new Vector3(-32+j*5,18,-18),new Vector3(-32+j*5,23,0),.14f,"Steel");
                Box(warehouse,"Roof surface",new Vector3(0,18.5f,0),new Vector3(68,1,39),"Steel");
            }
            var customs=Group("Customs / inspection office",new Vector3(1870,0,-355),harbor);Box(customs,"Office core",new Vector3(0,4.5f,0),new Vector3(30,9,26),"Cladding");for(int j=0;j<5;j++)Box(customs,"Inspection window",new Vector3(-12+j*6,5,-13.1f),new Vector3(4,3,.12f),"WindowDark",false);Text(customs,"CUSTOMS  /  세관",new Vector3(0,8,-13.3f),.5f,"NeonCyan",180);
            Crowd(harbor,"블루워터 항만",new Vector3(1710,.12f,-497),30,8,4,18);Crowd(harbor,"화물 검사 구역",new Vector3(1870,.08f,-385),18,8,4,12);Crowd(harbor,"물류센터",new Vector3(1335,.08f,-260),24,8,4,16);
            Service(harbor,1,new Vector3(1730,1,-475));Service(harbor,6,new Vector3(1360,1,-276));
        }
        static void Crane(Transform parent,Vector3 at,int id)
        {
            var p=Group("Ship-to-shore gantry crane "+id,at,parent);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 a=new Vector3(x*13,0,z*13),b=new Vector3(x*9,49,z*9);Beam(p,"Main lattice leg",a,b,.85f,"PaintAmber",true);
                for(int y=0;y<5;y++){float t=y/5f,u=(y+1)/5f;Vector3 lo=Vector3.Lerp(a,b,t),hi=Vector3.Lerp(a,b,u);Beam(p,"Diagonal lattice brace",lo,hi+new Vector3(x*2.4f,0,0),.16f,"Steel");}
                Box(p,"Crane bogie",a+Vector3.up*.5f,new Vector3(3,1.1f,7),"Steel");for(int k=0;k<4;k++){var wheel=Box(p,"Bogie wheel",a+new Vector3(0,.45f,-2.3f+k*1.5f),new Vector3(1.1f,.65f,1.1f),"Rubber",false,PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
            }
            for(int x=-1;x<=1;x+=2)
            {Beam(p,"Crane boom lower chord",new Vector3(x*3.5f,49,26),new Vector3(x*3.5f,49,-84),.5f,"PaintAmber");Beam(p,"Crane boom upper chord",new Vector3(x*3.5f,54,26),new Vector3(x*3.5f,54,-84),.45f,"PaintAmber");for(int i=0;i<22;i++)Beam(p,"Triangulated boom web",new Vector3(x*3.5f,49,26-i*5),new Vector3(x*3.5f,54,21-i*5),.14f,"Steel");}
            Box(p,"Machinery house",new Vector3(0,52,18),new Vector3(15,8,16),"Cladding");Box(p,"Trolley cab",new Vector3(0,45,-48),new Vector3(5,4,5),"WindowDark");
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Beam(p,"Hoist cable",new Vector3(x*2.5f,48,-48+z*1.2f),new Vector3(x*2.5f,15,-48+z*1.2f),.05f,"Steel");
            Box(p,"Container spreader",new Vector3(0,15,-48),new Vector3(12,1,3),"PaintAmber");Container(p,new Vector3(0,11.6f,-48),500+id);
            Beam(p,"A-frame mast",new Vector3(0,50,0),new Vector3(0,73,5),.65f,"PaintAmber");Beam(p,"Boom suspension cable",new Vector3(0,73,5),new Vector3(0,54,-78),.12f,"Steel");
            Text(p,"BW - "+(id+1).ToString("02"),new Vector3(0,53,26.1f),1.1f,"NeonCyan");
        }
        static void Ship(Transform parent,Vector3 at)
        {
            var ship=Group("MV AFTERGLOW / container vessel",at,parent);var v=new List<Vector3>();var tris=new List<int>();
            float[] xs={-235,-223,-190,160,213,232};float[] widths={2,13,19,19,12,1};
            for(int i=0;i<xs.Length;i++){float x=xs[i],w=widths[i];v.Add(new Vector3(x,-7,-w*.6f));v.Add(new Vector3(x,-7,w*.6f));v.Add(new Vector3(x,5,w));v.Add(new Vector3(x,5,-w));}
            for(int i=1;i<xs.Length;i++)for(int s=0;s<4;s++){int a=(i-1)*4+s,b=(i-1)*4+(s+1)%4,c=i*4+s,d=i*4+(s+1)%4;tris.AddRange(new[]{a,c,b,b,c,d});}MeshObject(ship,"Ship shaped hull",v.ToArray(),tris.ToArray(),"CargoBlue",true);
            Box(ship,"Weather deck",new Vector3(0,4.9f,0),new Vector3(440,.35f,35),"CargoRed");
            for(int x=0;x<14;x++)for(int z=0;z<7;z++)for(int tier=0;tier<2+(x%3==0?1:0);tier++)Container(ship,new Vector3(-185+x*24,5.1f+tier*2.95f,-8+z*2.6f),800+x*21+z*3+tier);
            Box(ship,"Accommodation block",new Vector3(185,15,0),new Vector3(32,20,30),"Cladding");Box(ship,"Navigation bridge",new Vector3(185,26,0),new Vector3(42,4,34),"WindowDark");Box(ship,"Bridge canopy",new Vector3(185,28.3f,0),new Vector3(44,.6f,36),"Cladding");
            for(int deck=0;deck<5;deck++)for(int window=0;window<8;window++)Box(ship,"Cabin porthole",new Vector3(173+window*3.4f,7+deck*3.5f,-15.1f),new Vector3(1.4f,1.4f,.06f),"WindowDark",false);
            Box(ship,"Exhaust funnel",new Vector3(204,31,0),new Vector3(9,12,8),"CargoRed");Beam(ship,"Bridge mast",new Vector3(181,28,0),new Vector3(181,40,0),.3f,"Steel");
            Text(ship,"AFTERGLOW",new Vector3(-182,3,-17),1.2f,"PaintWhite",180);
        }
        static void BuildCoast()
        {
            var coast=Group("Lumen coast / boardwalk and public beach",Vector3.zero);
            var path=new List<Vector3>();for(int i=0;i<=85;i++){float x=180+i*12;path.Add(new Vector3(x,.06f,Shore(x)+45));}Ribbon(coast,"Timber beach promenade",path.ToArray(),12,0,0,"Wood",true);
            for(int i=0;i<22;i++)
            {
                float x=215+i*44,z=Shore(x)+46;Model("painted_wooden_bench",coast,new Vector3(x,.08f,z+3.8f),1.3f);Model("street_lamp_01",coast,new Vector3(x+7,.08f,z+5),1.6f);
                if(i%2==0){var umbrella=Group("Beach shade",new Vector3(x,0,z-25),coast);Beam(umbrella,"Umbrella pole",Vector3.zero,Vector3.up*3.1f,.06f,"Aluminium");var vv=new List<Vector3>{new Vector3(0,3.8f,0)};for(int a=0;a<13;a++){float t=a*Mathf.PI/6;vv.Add(new Vector3(Mathf.Cos(t)*2.8f,3,Mathf.Sin(t)*2.8f));}var tt=new List<int>();for(int a=1;a<=12;a++)tt.AddRange(new[]{0,a+1,a});MeshObject(umbrella,"Fabric umbrella canopy",vv.ToArray(),tt.ToArray(),i%4==0?"CargoRed":"CargoBlue");for(int chair=0;chair<2;chair++){var c=Box(umbrella,"Beach lounger",new Vector3(chair*2-1,.25f,0),new Vector3(.8f,.18f,2.3f),"Cladding");Box(umbrella,"Lounger raised back",new Vector3(chair*2-1,.55f,.8f),new Vector3(.8f,.8f,.1f),"Cladding",false).transform.localRotation=Quaternion.Euler(-25,0,0);}}
            }
            var rescue=Group("Lifeguard tower",new Vector3(585,0,-584),coast);for(int s=-1;s<=1;s+=2)for(int z=-1;z<=1;z+=2)Beam(rescue,"Tower piling",new Vector3(s*2,0,z*2),new Vector3(s*2,4,z*2),.18f,"Wood",true);Box(rescue,"Guard deck",new Vector3(0,3.7f,0),new Vector3(6,.25f,5.5f),"Wood");Box(rescue,"Guard hut",new Vector3(0,5,1),new Vector3(4,2.4f,2.8f),"Cladding");Box(rescue,"Hut window",new Vector3(0,5.3f,-.42f),new Vector3(3.5f,1.1f,.08f),"WindowDark",false);for(int i=0;i<12;i++)Box(rescue,"Stair tread",new Vector3(3.8f-i*.23f,.17f+i*.3f,-2),new Vector3(.5f,.18f,1.1f),"Wood");Text(rescue,"LIFEGUARD",new Vector3(0,6,-.5f),.25f,"CargoRed",180);
            var pier=Group("Fisherman's pier",new Vector3(1050,0,Shore(1050)+30),coast);Box(pier,"Timber pier deck",new Vector3(0,.35f,-52),new Vector3(10,.5f,130),"Wood");for(int i=0;i<13;i++)for(int s=-1;s<=1;s+=2){Beam(pier,"Pier timber pile",new Vector3(s*4,-4,-i*10),new Vector3(s*4,.4f,-i*10),.3f,"Wood");Beam(pier,"Pier balustrade",new Vector3(s*4.8f,.6f,-i*10),new Vector3(s*4.8f,1.8f,-i*10),.07f,"Steel");if(i<12)Beam(pier,"Pier handrail",new Vector3(s*4.8f,1.8f,-i*10),new Vector3(s*4.8f,1.8f,-(i+1)*10),.06f,"Aluminium");}
            var hotel=Group("Seabreeze hotel / terraces",new Vector3(1100,0,-400),coast);Tower(hotel,Vector3.zero,46,34,42,40);Box(hotel,"Hotel pool surround",new Vector3(0,.1f,-40),new Vector3(56,.25f,35),"Cladding");Box(hotel,"Reflecting pool",new Vector3(0,.25f,-40),new Vector3(24,.025f,15),"Ocean",false);for(int i=0;i<8;i++)Tree(hotel,new Vector3(-29+i*8,0,-58),5);Service(hotel,7,new Vector3(0,1,-21));
            Crowd(coast,"루멘 해변",new Vector3(550,.08f,-551),30,12,4,16);Crowd(coast,"해변 산책로",new Vector3(790,.1f,-609),24,12,4,13);Crowd(pier,"낚시 부두",new Vector3(-2,.65f,-78),12,12,4,2.5f);Service(coast,0,new Vector3(570,1,-545));
        }
        static void BuildServices()
        {
            var clinic=Group("Port emergency clinic",new Vector3(1910,0,-340));Box(clinic,"Clinic shell",new Vector3(0,4.5f,0),new Vector3(34,9,24),"Cladding");Box(clinic,"Clinic windows",new Vector3(0,5,12.1f),new Vector3(29,3,.07f),"WindowDark",false);Text(clinic,"+  PORT MEDICAL",new Vector3(0,8,12.3f),.6f,"NeonCyan");Crowd(clinic,"항만 진료소",new Vector3(-8,.05f,22),12,20,4,9);Service(clinic,8,new Vector3(0,1,30));
            var station=Group("East electrical substation",new Vector3(1370,0,-60));Box(station,"Substation platform",new Vector3(0,0,0),new Vector3(70,.2f,50),"Pavement");
            for(int i=0;i<5;i++){Box(station,"Power transformer",new Vector3(-26+i*13,2,0),new Vector3(7,4,8),"Steel");for(int j=0;j<3;j++){Beam(station,"Ceramic bushing",new Vector3(-28+i*13+j*2,4,0),new Vector3(-28+i*13+j*2,7,0),.2f,"Cladding");for(int k=0;k<6;k++)Box(station,"Insulator rib",new Vector3(-28+i*13+j*2,4.4f+k*.4f,0),new Vector3(.65f,.08f,.65f),"Cladding",false,PrimitiveType.Cylinder);}for(int k=0;k<10;k++)Box(station,"Cooling fin",new Vector3(-29.5f+i*13,1+k*.25f,0),new Vector3(.35f,.08f,7),"Aluminium",false);}
            for(int s=-1;s<=1;s+=2){Beam(station,"High voltage pylon",new Vector3(s*30,0,17),new Vector3(s*30,19,17),.5f,"Steel");Beam(station,"Power bus",new Vector3(-30,18,17),new Vector3(30,18,17),.15f,"Aluminium");}Text(station,"EAST GRID  /  66 kV",new Vector3(0,3,-26),.65f,"NeonWarm",180);Crowd(station,"동부 전력 정비소",new Vector3(-10,.13f,-18),12,20,4,8);Service(station,9,new Vector3(0,1,-28));
        }
    }
}
