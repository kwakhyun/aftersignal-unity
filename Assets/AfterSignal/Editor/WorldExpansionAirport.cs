using System.Collections.Generic;using UnityEngine;
namespace AfterSignal.Editor
{
    public static partial class WorldExpansionBuilder
    {
        static void BuildAirport()
        {
            var airport=Group("AFTERLIGHT INTERNATIONAL / terminal and airside",new Vector3(1730,0,610));
            Box(airport,"Airport apron",new Vector3(0,-.03f,125),new Vector3(830,.12f,350),"Pavement");
            Box(airport,"Runway 09/27",new Vector3(0,.055f,300),new Vector3(830,.08f,48),"Asphalt");
            for(int i=0;i<62;i++)
            {
                float x=-405+i*13;Box(airport,"Runway centreline",new Vector3(x,.102f,300),new Vector3(6,.015f,.4f),"PaintWhite",false);
                for(int side=-1;side<=1;side+=2){Box(airport,"Runway edge marker",new Vector3(x,.12f,300+side*23),new Vector3(.3f,.14f,.3f),"NeonWarm",false);if(i%3==0)Box(airport,"Apron guidance stripe",new Vector3(x,.04f,190),new Vector3(6,.02f,.18f),"PaintAmber",false);}
            }
            for(int side=-1;side<=1;side+=2)for(int j=0;j<8;j++)Box(airport,"Threshold piano keys",new Vector3(side*380,.105f,284+j*4.4f),new Vector3(25,.02f,1.1f),"PaintWhite",false);
            Text(airport,"09",new Vector3(-343,.14f,300),2.8f,"PaintWhite").localRotation=Quaternion.Euler(90,90,0);
            Text(airport,"27",new Vector3(343,.14f,300),2.8f,"PaintWhite").localRotation=Quaternion.Euler(90,-90,0);
            for(int i=0;i<20;i++){var at=new Vector3(-190+i*20,0,-121);Model("concrete_road_barrier",airport,at,1.3f,90);}
            var terminal=Group("Terminal A / accessible departures hall",Vector3.zero,airport);
            Box(terminal,"Concourse floor",new Vector3(0,.04f,0),new Vector3(320,.2f,120),"TerminalFloor");
            // A double-curved barrel roof with repeated steel arches and clerestory strips.
            var v=new List<Vector3>();var tris=new List<int>();for(int x=0;x<=32;x++)for(int z=0;z<=24;z++){float xx=-168+x*10.5f,zz=-66+z*5.5f;float yy=21+17*Mathf.Cos(zz/66*Mathf.PI*.5f)+Mathf.Sin(xx*.016f)*2;v.Add(new Vector3(xx,yy,zz));}
            for(int x=0;x<32;x++)for(int z=0;z<24;z++){int k=x*25+z;tris.AddRange(new[]{k,k+1,k+25,k+1,k+26,k+25});}MeshObject(terminal,"Swept aluminium terminal roof",v.ToArray(),tris.ToArray(),"Cladding",true);
            var ceiling=v.ToArray();for(int n=0;n<ceiling.Length;n++)ceiling[n]-=Vector3.up*.35f;var underside=tris.ToArray();System.Array.Reverse(underside);MeshObject(terminal,"Acoustic roof underside",ceiling,underside,"RoofInterior");
            for(int i=0;i<8;i++){float x=-140+i*40;Box(terminal,"Suspended ceiling light",new Vector3(x,16,0),new Vector3(14,.1f,.4f),"NeonWarm",false);Lamp(terminal,new Vector3(x,12,0),new Color(.72f,.88f,1),28,36);}
            for(int i=-8;i<=8;i++)
            {
                float x=i*20;
                for(int z=-1;z<=1;z+=2){Beam(terminal,"Terminal branching column",new Vector3(x,0,z*52),new Vector3(x,21,z*59),.48f,"Steel",true);Beam(terminal,"Column roof fork",new Vector3(x,15,z*55),new Vector3(x+7,24,z*57),.23f,"Steel");}
                for(int j=0;j<12;j++){float a=-60+j*10,b=a+10;Beam(terminal,"Exposed roof rib",new Vector3(x,20+17*Mathf.Cos(a/66*Mathf.PI*.5f),a),new Vector3(x,20+17*Mathf.Cos(b/66*Mathf.PI*.5f),b),.22f,"Steel");}
            }
            for(int i=-16;i<16;i++)
            {
                float x=i*10+5;for(int z=-1;z<=1;z+=2){if(z==-1&&Mathf.Abs(x)<18)continue;Box(terminal,"Curtain wall glazing",new Vector3(x,10,z*59),new Vector3(9.8f,20,.08f),"Glazing",true);Beam(terminal,"Facade mullion",new Vector3(x-5,0,z*59),new Vector3(x-5,21,z*59),.13f,"Aluminium");Beam(terminal,"Glazing transom",new Vector3(x-5,10,z*59),new Vector3(x+5,10,z*59),.12f,"Aluminium");}
            }
            for(int s=-1;s<=1;s+=2)Box(terminal,"Terminal end wall",new Vector3(s*160,10,0),new Vector3(.25f,20,120),"Glazing");
            Text(terminal,"AFTERLIGHT  INTERNATIONAL",new Vector3(0,24,-66),1.0f,"NeonCyan",180);
            Text(terminal,"DEPARTURES   /   TERMINAL A",new Vector3(0,14,-60),.55f,"NeonWarm",180);
            for(int i=0;i<12;i++)
            {
                float x=-125+i*22;Box(terminal,"Check-in desk",new Vector3(x,.65f,-15),new Vector3(5,1.3f,2.2f),"Steel");Box(terminal,"Counter screen",new Vector3(x,1.8f,-14.5f),new Vector3(1,.65f,.12f),"NeonCyan",false);
                Box(terminal,"Bag conveyor",new Vector3(x+4,.4f,-9),new Vector3(1.3f,.7f,12),"Rubber");Text(terminal,(i+1).ToString("00"),new Vector3(x,3.4f,-15),.28f,"NeonWarm",180);
                for(int j=0;j<4;j++)Model("painted_wooden_bench",terminal,new Vector3(x+j%2*4-2,0,15+(j/2)*8),1.15f,180);
            }
            for(int s=-1;s<=1;s+=2)
            {
                var carousel=Group("Baggage reclaim island",new Vector3(s*120,0,43),terminal);Box(carousel,"Belt housing",new Vector3(0,.4f,0),new Vector3(28,.8f,9),"Aluminium");Box(carousel,"Moving belt",new Vector3(0,.83f,0),new Vector3(27,.04f,8),"Rubber",false);
                for(int b=0;b<9;b++)Box(carousel,"Checked baggage",new Vector3(-11+b*2.7f,1.25f,b%2==0?2.5f:-2.5f),new Vector3(1.3f,.8f,.7f),b%2==0?"CargoRed":"CargoBlue");
            }
            for(int gate=0;gate<4;gate++)
            {
                float x=-115+gate*77;var bridge=Group("Gate A"+(gate+1)+" / passenger boarding bridge",new Vector3(x,0,60),airport);
                Box(bridge,"Enclosed bridge floor",new Vector3(0,4.4f,17),new Vector3(4,.35f,34),"Steel");Box(bridge,"Enclosed bridge roof",new Vector3(0,7.4f,17),new Vector3(4,.25f,34),"Cladding");
                for(int s=-1;s<=1;s+=2)Box(bridge,"Jetbridge glass",new Vector3(s*1.94f,5.9f,17),new Vector3(.08f,2.8f,34),"Glazing");
                Beam(bridge,"Bridge support",new Vector3(0,0,26),new Vector3(0,4.3f,26),.65f,"Steel",true);Box(bridge,"Aircraft dock sleeve",new Vector3(0,5.6f,34),new Vector3(4.5f,3,2.6f),"Rubber");
                Text(terminal,"A"+(gate+1)+"  →",new Vector3(x,8,48),.6f,"NeonWarm",180);
                Aircraft(airport,new Vector3(x+9,0,121),1,gate);
                Box(airport,"Gate stop line",new Vector3(x+9,.043f,91),new Vector3(12,.02f,.2f),"PaintAmber",false);
            }
            var tower=Group("Air traffic control / panoramic cab",new Vector3(340,0,-50),airport);Box(tower,"Control tower shaft",new Vector3(0,31,0),new Vector3(12,62,12),"Cladding");Box(tower,"Control cabin",new Vector3(0,66,0),new Vector3(25,8,22),"WindowDark");Box(tower,"Cabin roof",new Vector3(0,70.4f,0),new Vector3(28,1,25),"Steel");
            for(int i=0;i<4;i++)Box(tower,"Cabin glazing strip",new Vector3(0,64+i*1.4f,-11.1f),new Vector3(24,.9f,.03f),"NeonCyan",false);Beam(tower,"Radar mast",new Vector3(0,70,0),new Vector3(0,79,0),.2f,"Steel");Box(tower,"Rotating surveillance antenna",new Vector3(0,78,0),new Vector3(12,1,1.5f),"Aluminium",false);
            for(int i=0;i<6;i++){var vehicle=Group("Ground support tug",new Vector3(-140+i*53,0,74),airport);Box(vehicle,"Baggage tractor",new Vector3(0,.65f,0),new Vector3(3,1.3f,1.4f),"PaintAmber");for(int j=0;j<3;j++)Box(vehicle,"Baggage cart",new Vector3(-4-j*3.2f,.45f,0),new Vector3(2.8f,.65f,1.6f),"Aluminium");}
            Crowd(terminal,"공항 출국장",new Vector3(-32,.16f,-42),36,4,4,22);Crowd(terminal,"공항 직원",new Vector3(30,.16f,-2),24,0,4,24);Crowd(terminal,"탑승 대합실",new Vector3(-45,.16f,35),24,4,4,18);Crowd(airport,"공항 지상 조업 구역",new Vector3(-125,.15f,88),18,0,4,10);
            Service(airport,2,new Vector3(0,1,-82));
        }
        static void Aerofoil(Transform p,string name,Vector3[] top,float depth,string mat)
        {
            var v=new Vector3[8];Vector3 center=Vector3.zero;for(int i=0;i<4;i++){v[i]=top[i];v[i+4]=top[i]-Vector3.up*depth;center+=v[i]+v[i+4];}center/=8;
            var tri=new[]{0,1,2,0,2,3,4,6,5,4,7,6,0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0};
            for(int i=0;i<tri.Length;i+=3){var a=v[tri[i]];var b=v[tri[i+1]];var c=v[tri[i+2]];if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3-center)<0){int t=tri[i+1];tri[i+1]=tri[i+2];tri[i+2]=t;}}
            MeshObject(p,name,v,tri,mat,true);
        }
        static void Aircraft(Transform p,Vector3 at,float scale,int serial)
        {
            var plane=Group("Lumen Air / regional jet "+serial,at,p);plane.localScale=Vector3.one*scale;
            var v=new List<Vector3>();var tris=new List<int>();float[] zs={-24,-22,-17,10,17,23,25};float[] rs={.1f,1.2f,2.15f,2.2f,1.8f,.6f,.04f};
            for(int i=0;i<zs.Length;i++)for(int a=0;a<32;a++){float theta=a*Mathf.PI*2/32;v.Add(new Vector3(Mathf.Cos(theta)*rs[i],5+Mathf.Sin(theta)*rs[i],zs[i]));}
            for(int i=1;i<zs.Length;i++)for(int a=0;a<32;a++){int b=(a+1)%32,k=(i-1)*32,j=i*32;tris.AddRange(new[]{k+a,k+b,j+a,k+b,j+b,j+a});}MeshObject(plane,"Aircraft tapered fuselage",v.ToArray(),tris.ToArray(),"Cladding",true);
            for(int s=-1;s<=1;s+=2)
            {
                Aerofoil(plane,"Swept wing",new[]{new Vector3(s*1.3f,4.4f,-3),new Vector3(s*22,4.4f,8),new Vector3(s*22,4.7f,11),new Vector3(s*1.3f,4.6f,6)},.18f,"Aluminium");
                Aerofoil(plane,"Tailplane",new[]{new Vector3(0,6,17),new Vector3(s*8,6.5f,21),new Vector3(s*8,6.7f,23),new Vector3(0,6,22)},.12f,"CargoBlue");
                var engine=Box(plane,"Turbofan nacelle",new Vector3(s*7,2.9f,1),new Vector3(3.1f,2.5f,3.1f),"Aluminium",true,PrimitiveType.Cylinder);engine.transform.localRotation=Quaternion.Euler(90,0,0);
                var intake=Box(plane,"Engine fan inlet",new Vector3(s*7,2.9f,-1.53f),new Vector3(2.65f,.07f,2.65f),"Rubber",false,PrimitiveType.Cylinder);intake.transform.localRotation=Quaternion.Euler(90,0,0);
                for(int i=0;i<20;i++)Box(plane,"Passenger porthole",new Vector3(s*2.1f,5.9f,-15+i*1.4f),new Vector3(.05f,.45f,.3f),"WindowDark",false);
                Box(plane,"Livery stripe",new Vector3(s*2.17f,4.8f,0),new Vector3(.025f,.2f,32),"CargoBlue",false);
                for(int j=0;j<2;j++){Beam(plane,"Landing gear",new Vector3(s*3,0,-2+j*10),new Vector3(s*3,4,-2+j*10),.2f,"Steel");var wheel=Box(plane,"Landing tyre",new Vector3(s*3,.8f,-2+j*10),new Vector3(1.2f,.5f,1.2f),"Rubber",false,PrimitiveType.Cylinder);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
                Box(plane,"Navigation light",new Vector3(s*22,4.8f,10),Vector3.one*.18f,s<0?"NeonRose":"NeonCyan",false,PrimitiveType.Sphere);
            }
            MeshObject(plane,"Vertical stabilizer",new[]{new Vector3(-.25f,6.5f,16),new Vector3(-.25f,14,21),new Vector3(-.25f,14,24),new Vector3(-.25f,6.5f,23),new Vector3(.25f,6.5f,16),new Vector3(.25f,14,21),new Vector3(.25f,14,24),new Vector3(.25f,6.5f,23)},new[]{0,1,2,0,2,3,4,6,5,4,7,6,0,4,5,0,5,1,1,5,6,1,6,2},"CargoBlue",true);
            for(int s=-1;s<=1;s+=2)Box(plane,"Cockpit window",new Vector3(s*.72f,5.85f,-21.4f),new Vector3(1.1f,.65f,.12f),"WindowDark",false);
        }
    }
}
