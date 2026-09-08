using System.Collections.Generic;
using UnityEngine;
namespace AfterSignal
{
    public static partial class FourCityArchitecture
    {
        static void Building(CityGeometry g,VenueRuntime venue)
        {
            var v=venue.Definition;float w=Mathf.Min(96,v.size.x*.72f),d=Mathf.Min(88,v.size.y*.66f);bool cinema=v.kind==VenueKind.Cinema,hotel=v.kind==VenueKind.Hotel;
            int floors=cinema?1:hotel?6:v.city==2?3:3;float step=cinema?12:5;float height=floors*step;
            venue.roomWidth=w;venue.roomDepth=d;venue.floorHeight=step;venue.floorCount=floors;
            for(int floor=0;floor<floors;floor++)
            {
                float y=floor*step;
                Floor(g,w,d,y,floor==0);if(floor<floors-1)g.Stairs(new(-w*.5f+7,y,-5),4,step,10,"TerminalFloor");
                for(int side=-1;side<=1;side+=2)
                {
                    for(float x=-w*.5f;x<w*.5f;x+=4)
                    {if(floor==0&&side<0&&Mathf.Abs(x)<8)continue;
                        g.Box("Facade mullion",new(x,y+step*.5f,side*d*.5f),new(.2f,step,.35f),"FutureCopper");g.Box("Clear outer glazing",new(x+1.9f,y+step*.47f,side*d*.5f),new(3.7f,step*.87f,.12f),"Glass",true);}
                    g.Box("Side wall",new(side*w*.5f,y+step*.5f,0),new(.22f,step,d),"Glass",true);
                    g.Box("Floor cornice",new(0,y+.15f,side*(d*.5f+1)),new(w+2,.4f,2),"FutureSilver");
                    for(int bay=-1;bay<=1;bay+=2)g.Box("Facade insulated spandrel",new(bay*(w*.25f+4),y+.65f,side*d*.5f),new(w*.5f-8,1.3f,.35f),v.city==3?"DeepDeck":"FutureCarbon",true);
                    g.Box("Side facade spandrel",new(side*w*.5f,y+.6f,0),new(.4f,1.2f,d),"FutureCarbon",true);
                }
                if(!cinema)
                {
                    Rooms(g,venue,floor,w,d,step);
                    foreach(var lamp in VenuePracticalLights.Positions(w,d,y+step))
                    {g.Box("Recessed luminaire housing",lamp+Vector3.up*.03f,new(2.8f,.1f,.4f),"FutureSilver");g.Box("Linear ceiling diffuser",lamp-Vector3.up*.035f,new(2.65f,.025f,.25f),"NeonWarm");}
                }
            }
            if(floors>1)venue.CreateLift(new(w*.5f-3,0,d*.5f-3),floors,step);
            if(cinema){Cinema(g,venue,w,d);}
            else
            {
                g.Box("Insulated building roof",new(0,height-.12f,0),new(w,.25f,d),"FutureCarbon",true);
                // Transverse shell varies by building function; exposed ribs give the silhouette depth.
                int ribs=hotel?18:26;float roof=height+.5f;
                for(int i=0;i<ribs;i++)
                {float x=-w*.58f+i*w*1.16f/(ribs-1);float crown=(v.kind==VenueKind.Museum?12:v.kind==VenueKind.Research?8:4)*Mathf.Sin(i*Mathf.PI/(ribs-1));
                    g.Beam(new(x,roof,-d*.6f),new(x,roof+crown,0),.42f,"FutureSilver");g.Beam(new(x,roof+crown,0),new(x,roof,d*.6f),.42f,"FutureSilver");
                    if(i<ribs-1){float nx=x+w*1.16f/(ribs-1),nc=(v.kind==VenueKind.Museum?12:8)*Mathf.Sin((i+1)*Mathf.PI/(ribs-1));g.Quad(new(x,roof,-d*.6f),new(x,roof+crown,0),new(nx,roof+nc,0),new(nx,roof,-d*.6f),"Glass",true);}
                }
                g.Box("Roof lightwell rim",new(0,height+.25f,0),new(w*.35f,.3f,d*.35f),"Glass");
                for(int unit=0;unit<4;unit++)StreetKit.Place("ClimateUnit",venue.transform,new Vector3(w*.36f,height,-d*.28f+unit*2.8f),Quaternion.identity);
            }
            if(hotel)
            {
                for(int s=-1;s<=1;s+=2){var wing=new GameObject("Hotel curved tower wing").transform;wing.SetParent(venue.transform,false);wing.localPosition=new Vector3(s*(w*.5f+10),0,12);var wg=new CityGeometry(wing);Tower(wg,16,d*.65f,v.city==3?28:68,s+7);wg.Finish();}
                g.Box("Sky pool bridge",new(0,height+1,8),new(w+40,1.2f,12),"FutureCeramic",true);g.Box("Sky pool water",new(0,height+1.65f,8),new(w+32,.04f,8),"Glass");
            }
            if(!cinema){venue.viewPoint=new(0,.14f,-d*.5f+8);venue.lookPoint=new(0,3,0);}
        }
        static void Floor(CityGeometry g,float w,float d,float y,bool ground)
        {
            if(ground){g.Box("Ground lobby floor",new(0,y-.07f,0),new(w,.2f,d),"TerminalFloor",true);return;}
            Rect stair=new(-w*.5f+4,-5.1f,6,10.2f),lift=new(w*.5f-6,d*.5f-6,6,6);
            var xs=new List<float>{-w*.5f,stair.xMin,stair.xMax,lift.xMin,w*.5f};var zs=new List<float>{-d*.5f,stair.yMin,stair.yMax,lift.yMin,d*.5f};xs.Sort();zs.Sort();
            for(int a=1;a<xs.Count;a++)for(int b=1;b<zs.Count;b++){var center=new Vector2((xs[a-1]+xs[a])/2,(zs[b-1]+zs[b])/2);if(stair.Contains(center)||lift.Contains(center))continue;g.Box("Unobstructed upper floor",new(center.x,y-.1f,center.y),new(xs[a]-xs[a-1],.2f,zs[b]-zs[b-1]),"TerminalFloor",true);}
            for(int s=-1;s<=1;s+=2)g.Beam(new(stair.center.x+s*3,y+1,stair.yMin),new(stair.center.x+s*3,y+1,stair.yMax),.07f,"Steel");
        }
        static void Rooms(CityGeometry g,VenueRuntime venue,int floor,float w,float d,float step)
        {
            var v=venue.Definition;float y=floor*step;bool hotel=v.kind==VenueKind.Hotel,clinic=v.kind==VenueKind.Hospital,research=v.kind==VenueKind.Research||v.kind==VenueKind.Reactor,archive=v.kind==VenueKind.Archive||v.kind==VenueKind.Museum;
            g.Sign((floor+1)+"F  "+(hotel?floor==0?"LOBBY / RESTAURANT":"GUEST ROOMS":clinic?floor==0?"RECEPTION":"WARD / TREATMENT":research?"RESEARCH / CONTROL":archive?"ARCHIVE / EXHIBITION":"CIVIC SERVICES"),new(0,y+3.2f,-d*.5f+.5f),.21f);
            for(int row=0;row<3;row++)for(int side=-1;side<=1;side+=2)
            {
                var at=new Vector3(side*(w*.26f),y,-d*.3f+row*d*.28f);float roomW=w*.28f;
                g.Box("Room partition",at+new Vector3(0,step*.45f,d*.12f),new(roomW,step*.9f,.18f),"FutureCeramic",true);
                if(hotel&&floor>0||clinic&&floor>0)
                {
                    g.Box("Bed frame",at+Vector3.up*.3f,new(2.2f,.6f,3.4f),"Wood",true);g.Box("Bed mattress",at+Vector3.up*.7f,new(2.1f,.28f,3.3f),"PaintWhite");g.Box("Bed cover",at+new Vector3(0,.88f,-.4f),new(2.13f,.08f,2.4f),"SeatBlue");g.Box("Pillow",at+new Vector3(0,.94f,1),new(1.7f,.24f,.55f),"PaintWhite");g.Box("Bedside table",at+new Vector3(2,.4f,1),new(.9f,.8f,.8f),"Wood",true);venue.activityPoints.Add(at+new Vector3(2,.08f,-1));
                    if(hotel){g.Box("Wall television",at+new Vector3(0,1.8f,-d*.12f),new(3.2f,1.8f,.12f),"FutureCarbon");g.Box("Television panel",at+new Vector3(0,1.8f,-d*.12f+.075f),new(3,1.6f,.02f),"NeonCyan");}
                }
                else if(archive)
                {for(int n=0;n<4;n++){var p=at+new Vector3((n-1.5f)*3,0,1);g.Box("Exhibit plinth",p+Vector3.up*.6f,new(1.4f,1.2f,1.4f),"FutureCeramic",true);g.Cylinder(p+Vector3.up*1.2f,.55f,1.7f,"Glass",16);g.Cylinder(p+Vector3.up*1.3f,.18f,1.1f,"NeonCyan",8,.38f);}venue.activityPoints.Add(at+new Vector3(0,.08f,-2));}
                else
                {for(int n=0;n<3;n++){var p=at+new Vector3((n-1)*3.7f,0,0);g.Box("Work desk",p+Vector3.up*.78f,new(2.8f,.12f,1.25f),"Wood",true);g.Box("Desk base",p+Vector3.up*.38f,new(2,.76f,.8f),"FutureCarbon");g.Box("Workstation screen",p+new Vector3(0,1.3f,.36f),new(1.5f,.8f,.09f),"FutureCarbon");g.Box("Workstation display",p+new Vector3(0,1.3f,.3f),new(1.4f,.7f,.02f),research?"NeonCyan":"NovaNeonAmber");Bench(g,p+new Vector3(0,0,-1.1f));venue.staffPoints.Add(p+new Vector3(0,.08f,-1.1f));}}
                if(research){g.Cylinder(at+new Vector3(roomW*.4f,.1f,0),1,2.6f,"Glass",24);g.Cylinder(at+new Vector3(roomW*.4f,.1f,0),.35f,2.6f,"NeonCyan",16);}
            }
            // Bathrooms and pantry fit behind the public corridor, outside stair/lift apertures.
            for(int n=0;n<3;n++){var p=new Vector3(-8+n*5,y,d*.5f-5);g.Box("Restroom partition",p+new Vector3(2,1.4f,0),new(.12f,2.8f,5),"FutureCeramic",true);g.Cylinder(p,.45f,.5f,"PaintWhite",16);g.Box("WC cistern",p+new Vector3(0,.75f,.6f),new(.8f,1,.3f),"PaintWhite");}
            Counter(g,new(8,y,d*.5f-5));venue.staffPoints.Add(new(8,y+.08f,d*.5f-3));
        }
        static void Cinema(CityGeometry g,VenueRuntime venue,float w,float d)
        {
            g.Box("Sound insulated auditorium roof",new(0,14,0),new(w+3,.5f,d+3),"FutureCarbon",true);
            for(int s=-1;s<=1;s+=2){g.Box("Acoustic side wall",new(s*21.5f,6.8f,0),new(.4f,13.6f,d*.85f),"FutureCarbon",true);for(int n=0;n<16;n++)g.Box("Acoustic wall fin",new(s*21.2f,7,-d*.4f+n*d*.8f/15),new(.3f,11,.15f),n%4==0?"NeonRose":"SeatBlue");}
            float screenZ=d*.32f;g.Box("Cinema acoustic wall",new(0,6,screenZ+1),new(32,12,1),"FutureCarbon",true);g.Box("Screen border",new(0,5.8f,screenZ),new(24,13.5f,.35f),"FutureCarbon");venue.screenPosition=new(0,5.8f,screenZ-.2f);venue.screenSize=new(22.4f,12.6f);
            for(int row=0;row<12;row++)
            {float z=screenZ-10-row*2.7f,y=row*.37f;g.Box("Auditorium stepped floor",new(0,y-.2f,z),new(40,.4f,2.7f),"FutureCarbon",true);
                for(int col=0;col<16;col++){float x=(col-7.5f)*1.25f;if(Mathf.Abs(x)<1.3f)continue;var p=new Vector3(x,y,z);g.Box("Upholstered cinema seat",p+Vector3.up*.55f,new(.9f,.3f,.85f),"SeatCoral",true);g.Box("Cinema backrest",p+new Vector3(0,1.1f,-.38f),new(.9f,1.2f,.2f),"SeatCoral");venue.seatPoints.Add(p+new Vector3(0,.3f,.7f));}g.Box("Aisle guidance",new(-18,y+.04f,z),new(.1f,.04f,2.7f),"NeonCyan");}
            var aisle=new GameObject("Cinema accessible stair aisle").transform;aisle.SetParent(venue.transform,false);aisle.localPosition=new(-17,0,screenZ-10);aisle.localRotation=Quaternion.Euler(0,180,0);var ramp=new CityGeometry(aisle);ramp.Stairs(Vector3.zero,3,4.1f,29.7f,"FutureCarbon");ramp.Finish();
            for(int n=0;n<20;n++){float x=(n-9.5f)*w/20;g.Beam(new(x,0,-d*.5f-1),new(x,14+Mathf.Sin(n*.4f)*2,-d*.5f-1),.6f,n%3==0?"NeonRose":"FutureSilver");}
            g.Sign("SIGNAL / 심해의 빛\nORIGINAL SHORT FILM · 60 SEC",new(0,11,-d*.5f-1.5f),.4f);
            venue.viewPoint=new(0,2.03f,screenZ-23.5f);venue.lookPoint=venue.screenPosition;
        }
        static void Garden(CityGeometry g,VenueRuntime venue)
        {
            float rx=venue.Definition.size.x*.42f,rz=venue.Definition.size.y*.41f;float h=venue.Definition.city==3?28:38;
            g.Dome(Vector3.zero,new(rx,h,rz),"Glass",40,10);
            g.Ring(new(0,.1f,0),rx*.65f,rz*.65f,5,"TerminalFloor");
            GardenBeds(g,rx,rz,venue.Definition.city==3);
            for(int i=0;i<36;i++){float a=i*Mathf.PI*2/36;var p=new Vector3(Mathf.Cos(a)*rx*.75f,0,Mathf.Sin(a)*rz*.75f);Planter(g,p);venue.activityPoints.Add(p+new Vector3(-3,.08f,0));}
            g.Cylinder(new(0,.04f,0),12,.3f,"FutureCeramic",64);g.Cylinder(new(0,.38f,0),11,.06f,"Glass",64);g.Cylinder(new(0,.4f,0),2.1f,h*.78f,"Glass",40,3.2f);g.Ring(new(0,h*.78f,0),4,4,.65f,"NeonCyan");
            for(int s=-1;s<=1;s+=2){g.Stairs(new(s*rx*.35f,0,-20),4,7,18,"TerminalFloor");g.Box("Canopy pedestrian bridge",new(s*rx*.35f,6.9f,16),new(5,.3f,36),"TerminalFloor",true);g.Beam(new(s*rx*.35f-2.5f,8,-2),new(s*rx*.35f-2.5f,8,34),.1f,"FutureCopper");}
            venue.viewPoint=new(rx*.35f,7.15f,8);venue.lookPoint=new(0,12,0);
        }
        static void Monument(CityGeometry g,VenueRuntime venue)
        {
            float height=venue.Definition.city==0?100:85;
            g.Cylinder(Vector3.zero,13,height,"FutureCarbon",64,5);
            for(int i=0;i<128;i++){float a=i*.12f,b=(i+1)*.12f;g.Beam(new(Mathf.Cos(a)*(18-i*.07f),i*height/128,Mathf.Sin(a)*(18-i*.07f)),new(Mathf.Cos(b)*(18-(i+1)*.07f),(i+1)*height/128,Mathf.Sin(b)*(18-(i+1)*.07f)),1.7f,"FutureSilver");}
            g.Cylinder(new(0,height,0),24,1,"FutureCeramic",64);g.Cylinder(new(0,height+1,0),24,4,"Glass",64);g.Ring(new(0,height+5,0),24,24,.7f,"NeonCyan");
            // A panoramic cabin runs beside the core and opens onto a genuine upper deck.
            g.Box("Observation deck floor",new(0,height-.2f,0),new(43,.4f,43),"TerminalFloor",true);
            venue.CreateLift(new(27,0,0),2,height);g.Box("Observation lift landing bridge",new(24,height-.15f,0),new(7,.3f,5),"TerminalFloor",true);
            venue.viewPoint=new(20,height+.12f,-8);venue.lookPoint=new(0,height+8,0);venue.activityPoints.Add(venue.viewPoint);
        }
        public static Vector3 Track(float progress)
        {float a=progress*Mathf.PI*2;return new Vector3(Mathf.Cos(a)*(211+16*Mathf.Sin(a*3)),.08f,Mathf.Sin(a)*(119+9*Mathf.Cos(a*2)));}
        static void Circuit(CityGeometry g,VenueRuntime venue)
        {
            for(int i=0;i<192;i++)
            {var a=Track(i/192f);var b=Track((i+1)/192f);var q=Quaternion.LookRotation(b-a);g.Box("Racing asphalt",(a+b)*.5f,new(18,.1f,Vector3.Distance(a,b)+.1f),"Asphalt",true,q);for(int s=-1;s<=1;s+=2){g.Box("Kerb",(a+b)*.5f+q*new Vector3(s*9,.08f,0),new(1.2f,.1f,Vector3.Distance(a,b)),i%2==0?"PaintWhite":"CargoRed",false,q);g.Box("Race safety barrier",(a+b)*.5f+q*new Vector3(s*11,1,0),new(.5f,2,Vector3.Distance(a,b)+.1f),"FutureSilver",true,q);}}
            for(int i=0;i<12;i++){var p=new Vector3(-100+i*17,0,-77);g.Box("Pit garage back",p+new Vector3(0,3,9),new(16,6,.4f),"FutureCarbon",true);g.Box("Pit garage partition",p+new Vector3(8,3,0),new(.4f,6,18),"FutureCeramic",true);g.Box("Pit garage canopy",p+Vector3.up*6,new(16,.4f,18),"FutureSilver");g.Sign("PIT "+(i+1),p+new Vector3(0,5,-9),.23f);venue.staffPoints.Add(p+Vector3.up*.1f);}
            for(int i=0;i<16;i++)g.Box("Grid chequer",Track(0)+new Vector3((i%2-.5f)*2,.12f,(i/2-3.5f)*2),new(2,.025f,2),i%2==i/2%2?"PaintWhite":"FutureCarbon");
            g.Box("Race control podium",new(0,3,-104),new(42,6,9),"FutureCarbon",true);g.Stairs(new(-26,0,-127),4,6,14,"NovaConcrete");g.Box("Race viewing deck",new(0,5.9f,-109),new(60,.2f,19),"TerminalFloor",true);venue.viewPoint=new(0,6.15f,-109);venue.lookPoint=new(160,1,0);
            g.Box("Race scoreboard housing",new(0,12,80),new(48,11,1),"FutureCarbon");venue.scoreboard=g.Sign("8 LAP / TEAM CUP",new(0,12,79.4f),.65f);
        }
        static void Park(CityGeometry g,VenueRuntime venue)
        {
            g.Ring(Vector3.up*.04f,155,95,5,"TerminalFloor");
            for(int garden=0;garden<6;garden++)
            {
                var center=new Vector3(-150+garden*60,0,83);g.Cylinder(center,18,.18f,"FutureCopper",32);g.Cylinder(center+Vector3.up*.18f,17.8f,.04f,"GardenSoil",32);
                for(int tree=0;tree<8;tree++){float angle=tree*2.399f;Tree(g,center+new Vector3(Mathf.Cos(angle)*(5+tree),.22f,Mathf.Sin(angle)*(5+tree)),1.3f+tree%3*.2f,garden*9+tree);}
                Bench(g,center+new Vector3(0,0,-22));venue.activityPoints.Add(center+new Vector3(0,.08f,-24));
            }
            g.Box("Park central pedestrian avenue",new(-45,.02f,4),new(8,.04f,220),"TerminalFloor");g.Box("Garden promenade",new(0,.025f,57),new(325,.05f,8),"TerminalFloor");
            for(int side=-1;side<=1;side+=2)for(int light=0;light<12;light++){var p=new Vector3(-45+side*5,0,-101+light*18);g.Beam(p,p+Vector3.up*4,.12f,"FutureSilver");g.Cylinder(p+Vector3.up*4,.4f,.16f,"NeonWarm",12);}
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var p=new Vector3(Mathf.Cos(a)*153,0,Mathf.Sin(a)*99);Planter(g,p);Bench(g,p+new Vector3(0,0,4));venue.activityPoints.Add(p+Vector3.up*.08f);}
            for(int n=0;n<6;n++){var p=new Vector3(-100+n*40,0,-106);g.Box("Park shop",p+Vector3.up*2,new(26,4,12),"FutureCeramic",true);g.Box("Park shop awning",p+new Vector3(0,4,-7),new(28,.3f,5),n%2==0?"SeatCoral":"SeatBlue");Counter(g,p+new Vector3(0,0,-8));venue.staffPoints.Add(p+new Vector3(0,.08f,-6));}
            venue.CreateRides();venue.viewPoint=new(-130,.15f,-70);venue.lookPoint=new(-105,26,10);
        }
    }
}
