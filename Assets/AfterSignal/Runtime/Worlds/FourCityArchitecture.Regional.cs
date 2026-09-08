using UnityEngine;
namespace AfterSignal
{
    public static partial class FourCityArchitecture
    {
        public static bool Regional(CityVenue v)=>v.kind>=VenueKind.Police||v.id=="nereid-forum"||v.id=="nereid-hospital"||v.id=="nova-medical"||v.id=="erebos-institute";
        static void RegionalBuilding(CityGeometry g,VenueRuntime venue)
        {
            var v=venue.Definition;bool deep=v.city==3,ruin=v.id.Contains("ruined"),food=v.kind==VenueKind.Cafe||v.kind==VenueKind.Restaurant;
            if(v.kind==VenueKind.Plaza){RegionalPlaza(g,venue);return;}
            float w=Mathf.Min(v.size.x*.63f,110),d=Mathf.Min(v.size.y*.61f,86),step=5;
            int floors=food?1:v.kind==VenueKind.CityHall||v.id=="nereid-forum"?4:v.kind==VenueKind.School||v.kind==VenueKind.Library||v.kind==VenueKind.Hospital?3:2;
            venue.floorCount=floors;venue.floorHeight=step;venue.roomWidth=w;venue.roomDepth=d;venue.viewPoint=new(0,.15f,-d*.5f+7);venue.lookPoint=new(0,3,0);
            string wall=ruin?"NovaObsidian":deep?"FutureCeramic":v.kind==VenueKind.Bank?"DistrictStone":v.kind==VenueKind.FireStation?"Concrete":"FutureCarbon";
            for(int f=0;f<floors;f++)
            {
                float y=f*step;Floor(g,w,d,y,f==0);
                if(f<floors-1)g.Stairs(new(-w*.5f+7,y,-5),4,step,10,"TerminalFloor");
                for(int side=-1;side<=1;side+=2)
                {
                    g.Box("Insulated side enclosure",new(side*w*.5f,y+2.5f,0),new(.3f,5,d),wall,true);
                    for(float x=-w*.5f;x<w*.5f-1;x+=5)
                    {
                        if(side<0&&Mathf.Abs(x+2.5f)<7)continue;
                        bool broken=ruin&&(Mathf.RoundToInt(x)+f)%4==0;
                        g.Box("Facade pier",new(x,y+2.5f,side*d*.5f),new(.35f,5,.45f),wall,true);
                        if(!broken){g.Box("Facade safety glass",new(x+2.45f,y+2.55f,side*d*.5f),new(4.55f,4.5f,.12f),"Glass",true);g.Box("Facade sill",new(x+2.45f,y+.4f,side*d*.5f),new(4.7f,.8f,.35f),wall,true);}
                        else g.Beam(new(x,y,side*d*.5f),new(x+3,y+4,side*d*.5f),.12f,"Steel");
                    }
                    g.Box("Projecting floor cornice",new(0,y-.12f,side*(d*.5f+1.2f)),new(w+4,.3f,2.6f),deep?"FutureCopper":"FutureSilver");
                }
                RegionalRooms(g,venue,f,w,d,step);
                for(int n=0;n<6;n++)g.Box("Task lighting",new((n%3-1)*w*.22f,y+4.72f,(n/3==0?-1:1)*d*.22f),new(3,.05f,.25f),deep?"NeonCyan":"NeonWarm");
            }
            if(floors>1)venue.CreateLift(new(w*.5f-3,0,d*.5f-3),floors,step);
            float h=floors*step;g.Box("Weather roof",new(0,h-.12f,0),new(w+2,.24f,d+2),wall,true);
            if(deep)
            {
                g.Dome(new(0,h,0),new(w*.58f,8,d*.6f),"Glass",32,6);
                for(int s=-1;s<=1;s+=2){g.Cylinder(new(s*(w*.5f+5),0,d*.2f),2.2f,h+4,"FutureSilver",16);g.Ring(new(s*(w*.5f+5),h+2,d*.2f),2.3f,2.3f,.16f,"NeonCyan",24);}
                for(int n=0;n<8;n++){float x=(n-3.5f)*1.7f;g.Beam(new(x,0,-d*.5f-5),new(x,5.7f,-d*.5f-5),.16f,"FutureCopper");}
                g.Box("Sealed entrance canopy",new(0,5.8f,-d*.5f-5),new(14,.25f,12),"Glass");
                g.Sign("기밀 구역 / 압력 정상",new(0,3.7f,-d*.5f-.3f),.22f);
                DeepCivicProfile(g,venue,w,d,h);
            }
            else if(v.kind==VenueKind.CityHall||v.id=="nereid-forum")
            {
                for(int f=0;f<9;f++){float yy=h+f*2.4f;g.Ring(new(f*.65f,yy,5),w*.33f-f*.65f,d*.34f-f*.35f,.38f,"FutureSilver",48);
                    if(f<8)for(int n=0;n<48;n++){float a=n*Mathf.PI*2/48,b=(n+1)*Mathf.PI*2/48;Vector3 P(float angle,int tier)=>new(tier*.65f+Mathf.Cos(angle)*(w*.33f-tier*.65f),h+tier*2.4f,5+Mathf.Sin(angle)*(d*.34f-tier*.35f));g.Quad(P(a,f),P(a,f+1),P(b,f+1),P(b,f),"Glass");}}
                g.Dome(new(5.2f,h+19.2f,5),new(w*.33f-5.2f,3,d*.34f-2.8f),"Glass",48,5);
                for(int i=0;i<36;i++){float a=i*Mathf.PI*2/36;g.Beam(new(Mathf.Cos(a)*w*.33f,h,5+Mathf.Sin(a)*d*.34f),new(5+Mathf.Cos(a)*(w*.33f-5),h+19,5+Mathf.Sin(a)*(d*.34f-3)),.24f,ruin?"Steel":"FutureCopper");}
            }
            else if(v.kind==VenueKind.FireStation)
            {
                for(int i=0;i<4;i++){var p=new Vector3(-w*.33f+i*w*.22f,0,-d*.5f-16);g.Box("Emergency vehicle bay apron",p,new(10,.12f,25),"Asphalt",true);g.Box("Sloping dispatch canopy",p+Vector3.up*6,new(12,.4f,31),"Concrete",false,Quaternion.Euler(0,0,-7));g.Sign("0"+(i+1),p+new Vector3(0,4.7f,0),.4f);}
                g.Box("Training tower",new(w*.5f+9,13,10),new(10,26,13),"Concrete",true);for(int f=0;f<5;f++)g.Box("Training tower opening",new(w*.5f+9,3+f*4.5f,3.4f),new(6,2.7f,.1f),"FutureCarbon");
            }
            else if(v.kind==VenueKind.Library)
            {
                for(int i=0;i<19;i++){float x=(i-9)*w/18;float rise=8*Mathf.Sin(i*Mathf.PI/18);g.Beam(new(x,h,-d*.56f),new(x,h+rise,0),.36f,"Wood");g.Beam(new(x,h+rise,0),new(x,h,d*.56f),.36f,"Wood");}
                for(int n=0;n<12;n++){float x=(n-5.5f)*w/12;g.Beam(new(x,1,-d*.5f-.5f),new(x+4,h,-d*.5f-.5f),.2f,"FutureCopper");}
            }
            else if(v.kind==VenueKind.School)
            {
                g.Box("School courtyard track",new(0,.04f,d*.5f+17),new(w,.08f,20),"Court",true);for(int n=0;n<6;n++)g.Box("Running lane",new(0,.09f,d*.5f+10+n*2.2f),new(w-.5f,.015f,.1f),"PaintWhite");
            }
            else
            {
                for(int s=-1;s<=1;s+=2)for(int i=0;i<14;i++){var p=new Vector3(s*(w*.5f+.6f),h*.5f,-d*.5f+i*d/13);g.Box("Deep exterior sun fin",p,new(1.2f,h, .16f),food?"Wood":"FutureSilver",false,Quaternion.Euler(0,s*18,0));}
                g.Box("Cantilever entrance",new(0,5.2f,-d*.5f-4),new(w*.65f,.4f,11),wall);
            }
            if(v.kind==VenueKind.Military||v.kind==VenueKind.Prison)SecureYard(g,venue,w,d,ruin);
            if(ruin)for(int n=0;n<16;n++){float a=n*2.39996f;g.Box("Collapsed concrete and twisted slabs",new(Mathf.Cos(a)*w*.64f,1+n%3,Mathf.Sin(a)*d*.64f),new(8,1.1f,4),"Concrete",true,Quaternion.Euler(n*9,n*33,n*17));}
        }
        static void DeepCivicProfile(CityGeometry g,VenueRuntime venue,float w,float d,float h)
        {
            var kind=venue.Definition.kind;
            if(kind==VenueKind.Bank||kind==VenueKind.CityHall)
            {
                g.Dome(new(0,h+1,0),new(w*.31f,kind==VenueKind.CityHall?12:7,d*.28f),kind==VenueKind.Bank?"FutureCopper":"Glass",32,9);
                for(int n=0;n<7;n++)g.Ring(new(0,h+n*1.15f,0),w*.33f-n*1.8f,d*.3f-n*1.4f,.2f,"FutureCopper",48);
            }
            else if(kind==VenueKind.FireStation||kind==VenueKind.Police)
            {
                for(int i=0;i<4;i++){var p=new Vector3((i-1.5f)*w*.18f,0,d*.5f+9);g.Cylinder(p,3,h+3,"FutureCeramic",24);g.Ring(p+Vector3.up*(h+2),3.25f,3.25f,.22f,kind==VenueKind.Police?"NeonCyan":"NeonWarm",24);g.Beam(p+Vector3.up*3,p+new Vector3(0,3,-12),1.2f,"FutureCopper");}
                g.Sign(kind==VenueKind.Police?"압력 순찰 / 격리":"심해 구조 / 산소 보급",new(0,4,-d*.5f-5),.27f);
            }
            else if(kind==VenueKind.Library)
            {
                for(int s=-1;s<=1;s+=2)for(int n=0;n<9;n++){float x=s*(w*.36f+n*.5f);g.Beam(new(x,0,-d*.5f),new(x+s*8,h+5,0),.45f,"FutureCopper");g.Beam(new(x+s*8,h+5,0),new(x,0,d*.5f),.45f,"FutureCopper");}
            }
            else if(kind==VenueKind.Cafe||kind==VenueKind.Restaurant)
            {
                for(int n=0;n<7;n++){float x=(n-3)*w*.13f;g.Beam(new(x,0,-d*.5f-13),new(x+2,6,-d*.5f-13),.14f,"FutureCopper");g.Dome(new(x+2,6,-d*.5f-13),new(8,2,8),n%2==0?"CanopyLeaf":"Glass",12,4);Bench(g,new(x,0,-d*.5f-10));}
            }
            else if(kind==VenueKind.School)
            {
                for(int i=0;i<3;i++){var p=new Vector3((i-1)*w*.3f,h,0);g.Dome(p,new(12,9,15),"Glass",16,7);g.Ring(p+Vector3.up*3,12,15,.25f,"NeonCyan",32);}
            }
            else if(kind==VenueKind.Hospital)
            {
                for(int s=-1;s<=1;s+=2){var p=new Vector3(s*(w*.5f+8),2,0);g.Dome(p,new(7,8,13),"Glass",24,7);g.Box("Medical cross upright",p+new Vector3(0,8,-14),new(.8f,4,.25f),"PaintWhite");g.Box("Medical cross arms",p+new Vector3(0,8,-14),new(3,.8f,.25f),"PaintWhite");}
            }
        }
        static void RegionalRooms(CityGeometry g,VenueRuntime venue,int f,float w,float d,float step)
        {
            var kind=venue.Definition.kind;float y=f*step;
            for(int s=-1;s<=1;s+=2)
            {
                var at=new Vector3(s*w*.21f,y,-d*.17f);
                if(kind==VenueKind.Restaurant||kind==VenueKind.Cafe)
                {
                    for(int n=0;n<8;n++){var p=at+new Vector3((n%2-.5f)*6,0,n/2*5);g.Cylinder(p,.9f,.8f,"Wood",16);for(int side=-1;side<=1;side+=2)Bench(g,p+Vector3.right*side*1.8f);venue.seatPoints.Add(p+new Vector3(1.8f,.1f,0));}
                    Counter(g,new(s*w*.2f,y,d*.34f));g.Box("Commercial kitchen equipment",new(s*w*.2f,y+1.05f,d*.4f),new(8,2.1f,2),"FutureSilver",true);g.Sign(s==1?"주방 / 직원 전용":"주문 / 픽업",new(s*w*.2f,y+3,d*.29f),.2f);
                }
                else if(kind==VenueKind.Library)
                {
                    for(int n=0;n<7;n++){var p=at+new Vector3((n%3-1)*6,0,n/3*7);g.Box("Library shelving",p+Vector3.up*1.2f,new(3.5f,2.4f,.7f),"Wood",true);for(int shelf=0;shelf<4;shelf++)for(int book=0;book<12;book++)g.Box("Book spine",p+new Vector3(-1.5f+book*.27f,.3f+shelf*.55f,.38f),new(.19f,.35f,.11f),(book+shelf)%3==0?"SeatCoral":(book+shelf)%3==1?"SeatBlue":"DistrictIvory");venue.activityPoints.Add(p+new Vector3(0,.12f,-1.2f));}
                }
                else if(kind==VenueKind.Hospital)
                {
                    for(int n=0;n<6;n++){var p=at+new Vector3((n%2-.5f)*6,0,n/2*6);g.Box("Adjustable hospital bed",p+Vector3.up*.5f,new(2,.65f,3.1f),"FutureSilver",true);g.Box("Clinical mattress",p+Vector3.up*.9f,new(1.9f,.25f,3),"PaintWhite");g.Box("Pillow",p+new Vector3(0,1.12f,1),new(1.3f,.2f,.6f),"PaintWhite");g.Box("Vital sign monitor",p+new Vector3(1.8f,1.5f,1),new(1,.65f,.15f),"NeonCyan");venue.activityPoints.Add(p+new Vector3(2.5f,.1f,-1));}
                }
                else if(kind==VenueKind.School)
                {
                    for(int n=0;n<12;n++){var p=at+new Vector3((n%3-1)*3,0,n/3*3.2f);g.Box("Student desk",p+Vector3.up*.75f,new(1.65f,.12f,1.1f),"Wood",true);g.Box("Desk legs",p+Vector3.up*.35f,new(.85f,.7f,.7f),"Steel");Bench(g,p+Vector3.back*1.05f);venue.seatPoints.Add(p+new Vector3(0,.1f,-1));}
                    g.Box("Teaching board",at+new Vector3(0,2.1f,14),new(7,2.4f,.16f),"SeatBlue");g.Sign(f==0?"새로운 해협 / 과학과 공동체":f==1?"수업 시간표":"도시의 기억",at+new Vector3(0,2.1f,13.9f),.22f);venue.staffPoints.Add(at+new Vector3(0,.1f,12));
                }
                else if(kind==VenueKind.Prison)
                {
                    for(int n=0;n<4;n++){var p=at+new Vector3(0,0,n*6);g.Box("Cell bed",p+new Vector3(1,.5f,0),new(2,.7f,3),"Steel",true);g.Box("Cell mattress",p+new Vector3(1,.9f,0),new(1.9f,.15f,2.8f),"SeatBlue");for(int bar=0;bar<12;bar++)g.Beam(p+new Vector3(-3+bar*.55f,0,-2),p+new Vector3(-3+bar*.55f,4.4f,-2),.065f,"Steel",true);g.Box("Cell partition",p+new Vector3(-3,2.1f,0),new(.18f,4.2f,5.8f),"Concrete",true);venue.activityPoints.Add(p+new Vector3(-1,.1f,0));}
                }
                else
                {
                    for(int n=0;n<6;n++){var p=at+new Vector3((n%2-.5f)*6,0,n/2*6);g.Box("Office laboratory desk",p+Vector3.up*.82f,new(3,.15f,1.4f),"FutureSilver",true);g.Box("Cabinet under desk",p+new Vector3(1,.4f,0),new(.7f,.8f,1.1f),"FutureCarbon",true);g.Box("Information screen",p+new Vector3(0,1.45f,.4f),new(1.35f,.75f,.1f),"NeonCyan");Bench(g,p+Vector3.back*1.3f);venue.staffPoints.Add(p+new Vector3(0,.1f,-1.3f));}
                    if(kind==VenueKind.Laboratory||kind==VenueKind.Research)for(int n=0;n<3;n++){var p=at+new Vector3(n*3-3,0,18);g.Cylinder(p,1.2f,3.3f,"Glass",24);g.Cylinder(p+Vector3.up*.1f,.32f,2.6f,"NovaNeonViolet",12);g.Ring(p+Vector3.up*3.3f,1.25f,1.25f,.15f,"FutureSilver",24);}
                }
            }
            for(int n=0;n<3;n++){var p=new Vector3(-9+n*6,y,d*.5f-5);g.Box("Utility room partition",p+new Vector3(2,1.6f,0),new(.18f,3.2f,5),"FutureCeramic",true);g.Cylinder(p,.4f,.55f,"PaintWhite",16);g.Box("Wash basin",p+new Vector3(0,1,-1.5f),new(1,.15f,.7f),"FutureCeramic");}
            g.Sign(f==0?"접수 / 안내  ·  계단 / 승강기 →":f==1?"업무실  ·  휴게실  ·  화장실": "자료실  ·  연구 / 학습 공간",new(0,y+3.2f,-d*.5f+1),.19f);
            venue.activityPoints.Add(new(0,y+.12f,-d*.3f));venue.activityPoints.Add(new(0,y+.12f,d*.25f));
        }
        static void SecureYard(CityGeometry g,VenueRuntime venue,float w,float d,bool ruin)
        {
            float x=venue.Definition.size.x*.46f,z=venue.Definition.size.y*.46f;
            for(int side=-1;side<=1;side+=2)
            {
                g.Box("Secure perimeter wall",new(side*x,3,0),new(.6f,6,z*2),"Concrete",true);
                for(int half=-1;half<=1;half+=2)g.Box("Gate flank",new(half*(x*.5f+7),3,side*z),new(x-14,6,.6f),"Concrete",true);
                for(int end=-1;end<=1;end+=2){var p=new Vector3(side*(x-6),0,end*(z-6));g.Box("Guard tower column",p+Vector3.up*5,new(3,10,3),"Concrete",true);g.Box("Guard tower cabin",p+Vector3.up*10,new(7,2.8f,7),"Glass");g.Box("Guard tower roof",p+Vector3.up*12,new(8,.3f,8),"FutureCarbon");}
            }
            if(venue.Definition.kind==VenueKind.Military)for(int i=0;i<3;i++){var p=new Vector3(-x+32+i*40,0,z-25);g.Box("Hangar side pier",p+Vector3.up*5,new(2,10,32),"Concrete",true);g.Dome(p+new Vector3(15,10,0),new(17,8,18),"FutureCarbon",20,6);g.Sign(ruin?"격납고 / 격리":"정비 / 항공작전",p+new Vector3(15,7,-19),.23f);}
        }
        static void RegionalPlaza(CityGeometry g,VenueRuntime venue)
        {
            float w=venue.Definition.size.x,d=venue.Definition.size.y;
            for(int i=0;i<5;i++)g.Ring(new(0,.03f,0),10+i*9,10+i*7,.16f,i%2==0?"FutureCopper":"NeonCyan",64);
            g.Cylinder(Vector3.zero,6,.35f,"FutureSilver",40);g.Cylinder(Vector3.up*.38f,5.6f,.04f,"Glass",40);
            for(int n=0;n<18;n++){float a=n*Mathf.PI*2/18;var p=new Vector3(Mathf.Cos(a)*w*.34f,0,Mathf.Sin(a)*d*.34f);Bench(g,p);Planter(g,p+new Vector3(3,0,3));venue.activityPoints.Add(p+new Vector3(0,.12f,-2));}
            g.Box("Public performance stage",new(0,.45f,d*.34f),new(20,.9f,12),"Wood",true);g.Stairs(new(0,0,d*.34f-8),6,.9f,2,"Wood");venue.viewPoint=new(0,.12f,-15);venue.lookPoint=new(0,3,0);
        }
    }
}
