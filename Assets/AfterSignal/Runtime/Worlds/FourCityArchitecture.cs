using UnityEngine;
namespace AfterSignal
{
    public static partial class FourCityArchitecture
    {
        public static void Build(VenueRuntime venue)
        {
            var v=venue.Definition;var g=new CityGeometry(venue.transform);float w=v.size.x,d=v.size.y;
            if(v.kind==VenueKind.Sinkhole){RegionalTerrain.Sinkhole(g,venue);g.Finish();return;}
            if(v.kind==VenueKind.Slum){RegionalSettlements.Slum(g,venue);g.Finish();return;}
            if(v.kind==VenueKind.Island){RegionalSettlements.Island(g,venue);g.Finish();return;}
            g.Box("Venue plaza",new(0,-.12f,0),new(w+.5f,.16f,d+.5f),v.city==3?"DeepDeck":"Pavement",true);
            for(int s=-1;s<=1;s+=2){g.Box("Entry wayfinding strip",new(s*4,.025f,-d*.5f+10),new(.3f,.03f,25),"NeonCyan");g.Beam(new(s*10,0,-d*.5f+5),new(s*10,6,-d*.5f+5),.4f,"FutureCopper");}
            g.Sign(v.title,new(0,5,-d*.5f+4),.45f);
            if(Regional(v))RegionalBuilding(g,venue);
            else if(v.Sport&&v.kind!=VenueKind.Circuit)Stadium(g,venue);
            else if(v.kind==VenueKind.Circuit)Circuit(g,venue);
            else if(v.kind==VenueKind.Amusement)Park(g,venue);
            else if(v.kind==VenueKind.Garden)Garden(g,venue);
            else if(v.kind==VenueKind.Monument)Monument(g,venue);
            else Building(g,venue);
            // Public forecourt furniture has clear walking aisles and a staffed service counter.
            Counter(g,new(15,0,-d*.5f+12));venue.staffPoints.Add(new(15,.08f,-d*.5f+13.8f));
            VenueService.Add(venue.transform,new(15,1,-d*.5f+10),venue.Index,v.title+" · 안내 / 이용");
            for(int n=0;n<8;n++){float x=(n<4?-1:1)*(w*.5f-8),z=-d*.38f+(n%4)*d*.22f;Planter(g,new(x,0,z));Bench(g,new(x+(n<4?3:-3),0,z));venue.activityPoints.Add(new(x+(n<4?4:-4),.08f,z));}
            g.Finish();
        }
        public static void Tower(CityGeometry g,float w,float d,float h,int profile,bool ruined=false)
        {
            if(!ruined){DistrictTower.Build(g,w,d,h,profile);return;}
            string stone=ruined?"NovaObsidian":profile%3==0?"FutureCeramic":"FutureCarbon";
            int floors=Mathf.Max(3,(int)(h/4));
            for(int f=0;f<floors;f++)
            {float taper=1-f/(float)floors*(profile%3==0?.48f:.22f);var p=new Vector3(Mathf.Sin(f*(profile%2==0?.2f:0)+profile)*w*.12f,f*4,Mathf.Cos(f*.2f+profile)*d*.08f);
                if(ruined&&f>floors*.65f&&f%3==0)continue;
                g.Box("Sculpted floor plate",p,new(w*taper,.5f,d*taper),stone);
                for(int s=-1;s<=1;s+=2){if(!ruined||f<3||((f+profile+s)%4==0)){g.Box("Curtain glazing",p+new Vector3(s*w*taper*.48f,1.8f,0),new(.25f,3.3f,d*taper),"NovaWindow");g.Box("Curtain glazing",p+new Vector3(0,1.8f,s*d*taper*.48f),new(w*taper,3.3f,.25f),"NovaWindow");}else g.Beam(p+new Vector3(s*w*taper*.45f,0,-d*.45f),p+new Vector3(s*w*taper*.45f,3.8f,d*.45f),.35f,"Steel");}
                if(f%4==0)g.Box("Cantilever garden",p+new Vector3(w*.18f,0,d*.12f),new(w*taper+4,.65f,d*taper+4),stone);
            }
            g.Box("Building collision",new(0,h*(ruined?.19f:.5f),0),new(w*(ruined?.35f:.7f),h*(ruined?.38f:1),d*(ruined?.35f:.7f)),stone,true);
            for(int s=-1;s<=1;s+=2)g.Beam(new(s*w*.45f,0,-d*.5f),new(-s*w*.2f,h,d*.32f),1.1f,"FutureCopper");
            g.Ring(new(0,h+1,0),w*.35f,d*.35f,.45f,ruined?"NovaNeonViolet":"NeonCyan",32);
        }
        static void Stadium(CityGeometry g,VenueRuntime venue)
        {
            var v=venue.Definition;bool basket=v.kind==VenueKind.Basketball,baseball=v.kind==VenueKind.Baseball;
            float rx=basket?24:baseball?83:72,rz=basket?16:baseball?82:50;int tiers=basket?10:16;
            for(int tier=0;tier<tiers;tier++)
            {
                float ax=rx+tier*1.65f,az=rz+tier*1.65f,y=.8f+tier*.68f;
                for(int n=0;n<72;n++)
                {
                    float a=n*Mathf.PI*2/72,b=(n+1)*Mathf.PI*2/72,mid=(a+b)*.5f;
                    // Four radial aisles stay open. Seats are independent of the stepped concrete bowl.
                    var pa=new Vector3(Mathf.Cos(a)*ax,y,Mathf.Sin(a)*az);var pb=new Vector3(Mathf.Cos(b)*ax,y,Mathf.Sin(b)*az);var q=Quaternion.LookRotation(pb-pa);
                    g.Box("Stand tread",(pa+pb)*.5f-Vector3.up*.2f,new(1.75f,.4f,Vector3.Distance(pa,pb)+.1f),"NovaConcrete",true,q);
                    if(n%18==0)continue;int seats=Mathf.Max(2,(int)(Vector3.Distance(pa,pb)/.8f));
                    for(int k=0;k<seats;k++){var p=Vector3.Lerp(pa,pb,(k+.5f)/seats);string c=(n/18+tier/4)%2==0?"SeatBlue":"SeatCoral";g.Box("Moulded stadium seat",p+Vector3.up*.35f,new(.54f,.1f,.55f),c,false,Quaternion.Euler(0,-mid*Mathf.Rad2Deg+90,0));g.Box("Stadium seat back",p+new Vector3(Mathf.Cos(mid)*.24f,.63f,Mathf.Sin(mid)*.24f),new(.52f,.58f,.07f),c,false,Quaternion.Euler(0,90-mid*Mathf.Rad2Deg,0));
                        if(tier%3==0&&n%3==0&&k==0)venue.seatPoints.Add(p+Vector3.up*.08f);}
                }
            }
            float outerX=rx+tiers*1.65f+5,outerZ=rz+tiers*1.65f+5;
            for(int i=0;i<72;i++)
            {float a=i*5*Mathf.Deg2Rad,b=(i+1)*5*Mathf.Deg2Rad;var lo=new Vector3(Mathf.Cos(a)*outerX,1,Mathf.Sin(a)*outerZ);var hi=lo+Vector3.up*(basket?19:24);
                g.Beam(lo,hi,.38f,"Steel");g.Beam(lo,hi+new Vector3(-Mathf.Sin(a)*5,0,Mathf.Cos(a)*5),.2f,"FutureSilver");
                var next=new Vector3(Mathf.Cos(b)*outerX,1,Mathf.Sin(b)*outerZ);g.Quad(lo+Vector3.up*5,hi,next+Vector3.up*(basket?19:24),next+Vector3.up*5,"Glazing",true);
                g.Quad(hi,hi-new Vector3(Mathf.Cos(a)*20,2,Mathf.Sin(a)*20),next+Vector3.up*(basket?17:22)-new Vector3(Mathf.Cos(b)*20,0,Mathf.Sin(b)*20),next+Vector3.up*(basket?19:24),"FutureSilver",true);
            }
            g.Ring(new(0,basket?20:25,0),outerX,outerZ,.5f,"NeonCyan");
            g.Stairs(new(0,0,-outerZ-1),5,11,26,"NovaConcrete");
            g.Box("Accessible spectator deck",new(0,10.9f,-rz-9),new(15,.25f,7),"NovaConcrete",true);
            venue.viewPoint=new(0,11.15f,-rz-9);venue.lookPoint=new(0,1,0);
            g.Box("Scoreboard housing",new(0,18,rz+10),new(basket?20:34,8,1),"FutureCarbon");
            venue.scoreboard=g.Sign("경기 준비 중",new(0,18,rz+9.4f),basket?.15f:.24f);
            for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2){var p=new Vector3(s*(outerX-5),0,t*(outerZ-5));g.Beam(p,p+Vector3.up*34,.6f,"Steel");g.Box("Stadium floodlight array",p+Vector3.up*34,new(8,2,.5f),"NeonWarm");}
            if(basket)BasketballCourt(g);else if(baseball)BaseballField(g);else FootballField(g);
            // Clubrooms below the west stand, with doors and continuous floors.
            for(int r=0;r<3;r++){var at=new Vector3(-outerX+4,0,-14+r*14);g.Box("Clubroom floor",at,new(12,.2f,12),"TerminalFloor",true);g.Box("Clubroom divider",at+new Vector3(0,2.5f,6),new(12,5,.2f),"FutureCeramic",true);g.Sign(r==0?"선수 라커룸":r==1?"응급실":"클럽 매장",at+new Vector3(6,2.6f,0),.17f,90);for(int n=0;n<5;n++)g.Box("Player locker",at+new Vector3(-4+n*1.6f,1.2f,4.8f),new(1.2f,2.4f,.65f),"SeatBlue",true);venue.staffPoints.Add(at+Vector3.up*.12f);}
        }
        static void Line(CityGeometry g,Vector3 a,Vector3 b,float width=.13f,string color="PaintWhite")=>g.Box("Regulation field marking",(a+b)*.5f+Vector3.up*.035f,new(width,.025f,Vector3.Distance(a,b)),color,false,Quaternion.LookRotation(b-a));
        static void Rectangle(CityGeometry g,float x,float z,float w,float d){Line(g,new(x-w/2,0,z-d/2),new(x+w/2,0,z-d/2));Line(g,new(x-w/2,0,z+d/2),new(x+w/2,0,z+d/2));Line(g,new(x-w/2,0,z-d/2),new(x-w/2,0,z+d/2));Line(g,new(x+w/2,0,z-d/2),new(x+w/2,0,z+d/2));}
        static void FootballField(CityGeometry g)
        {
            for(int i=0;i<14;i++)g.Box("Striped football turf",new(-52.5f+(i+.5f)*7.5f,.012f,0),new(7.5f,.025f,68),i%2==0?"Pitch":"PitchLight");Rectangle(g,0,0,105,68);Line(g,new(0,0,-34),new(0,0,34));g.Ring(new(0,.035f,0),9.15f,9.15f,.13f,"PaintWhite",64);
            for(int s=-1;s<=1;s+=2){Rectangle(g,s*44.25f,0,16.5f,40.32f);Rectangle(g,s*49.75f,0,5.5f,18.32f);g.Cylinder(new(s*41.5f,.045f,0),.14f,.025f,"PaintWhite",12);var a=new Vector3(s*52.5f,0,-3.66f);var b=new Vector3(s*52.5f,0,3.66f);g.Beam(a,a+Vector3.up*2.44f,.12f,"PaintWhite");g.Beam(b,b+Vector3.up*2.44f,.12f,"PaintWhite");g.Beam(a+Vector3.up*2.44f,b+Vector3.up*2.44f,.12f,"PaintWhite");for(float z=-3.66f;z<3.67f;z+=.4f)g.Beam(new(s*54.5f,0,z),new(s*54.5f,2.44f,z),.018f,"PaintWhite");for(float y=.2f;y<2.5f;y+=.4f)g.Beam(new(s*54.5f,y,-3.66f),new(s*54.5f,y,3.66f),.018f,"PaintWhite");}
        }
        static void BasketballCourt(CityGeometry g)
        {
            g.Box("Maple basketball floor",new(0,.014f,0),new(32,.03f,19),"Court");Rectangle(g,0,0,28,15);Line(g,new(0,0,-7.5f),new(0,0,7.5f));g.Ring(new(0,.04f,0),1.8f,1.8f,.05f,"PaintWhite",48);
            for(int s=-1;s<=1;s+=2){Rectangle(g,s*11.1f,0,5.8f,4.9f);g.Ring(new(s*12.425f,.045f,0),6.75f,6.75f,.06f,"PaintWhite",64,s>0?90:-90,s>0?270:90);g.Beam(new(s*15.8f,0,0),new(s*15.8f,4.1f,0),.35f,"FutureCarbon");g.Beam(new(s*15.8f,3.7f,0),new(s*12.8f,3.7f,0),.25f,"FutureCarbon");g.Box("Backboard",new(s*12.8f,3.95f,0),new(.06f,1.05f,1.8f),"Glass");g.Ring(new(s*12.425f,3.05f,0),.225f,.225f,.035f,"SeatCoral",24);for(int n=0;n<12;n++){float a=n*Mathf.PI/6;g.Beam(new(s*12.425f+Mathf.Cos(a)*.22f,3.05f,Mathf.Sin(a)*.22f),new(s*12.425f+Mathf.Cos(a)*.15f,2.65f,Mathf.Sin(a)*.15f),.014f,"PaintWhite");}}
        }
        static void BaseballField(CityGeometry g)
        {
            g.Box("Outfield turf",new(0,.015f,12),new(145,.025f,132),"Pitch");g.Box("Infield clay diamond",new(0,.03f,-24.6f),new(38.8f,.035f,38.8f),"Clay",false,Quaternion.Euler(0,45,0));
            Vector3[] bases={new(0,.065f,-44),new(19.4f,.065f,-24.6f),new(0,.065f,-5.2f),new(-19.4f,.065f,-24.6f)};
            for(int i=0;i<4;i++){g.Box("Base",bases[i],new(.45f,.05f,.45f),"PaintWhite");Line(g,bases[i],bases[(i+1)%4]);}
            Line(g,bases[0],new(70,0,26));Line(g,bases[0],new(-70,0,26));g.Cylinder(new(0,.025f,-25.56f),2.75f,.25f,"Clay",32,1.5f);g.Box("Pitching rubber",new(0,.3f,-25.56f),new(.61f,.02f,.15f),"PaintWhite");
            for(int s=-1;s<=1;s+=2){g.Box("Dugout sunken bench",new(s*28,.45f,-36),new(11,.9f,2),"Wood",true);g.Box("Dugout roof",new(s*28,3,-36),new(14,.25f,5),"FutureSilver");g.Beam(new(s*71,0,27),new(s*71,25,27),.25f,"PaintAmber");}
        }
        public static void Bench(CityGeometry g,Vector3 p){g.Box("Bench seat",p+Vector3.up*.5f,new(2.6f,.16f,.65f),"Wood",true);g.Box("Bench back",p+new Vector3(0,.9f,.3f),new(2.6f,.7f,.12f),"Wood");for(int s=-1;s<=1;s+=2)g.Box("Bench foot",p+new Vector3(s*.9f,.23f,0),new(.12f,.46f,.6f),"Steel");}
        public static void Planter(CityGeometry g,Vector3 p){g.Cylinder(p,1.4f,.65f,"FutureCeramic",16,1.55f);g.Cylinder(p+Vector3.up*.65f,1.35f,.2f,"GardenSoil",16);Tree(g,p+Vector3.up*.85f,.65f,Mathf.RoundToInt(p.x+p.z));}
        public static void Counter(CityGeometry g,Vector3 p){g.Box("Staff counter",p+Vector3.up*.55f,new(5,1.1f,1.4f),"FutureCeramic",true);g.Box("Counter worktop",p+Vector3.up*1.13f,new(5.2f,.1f,1.55f),"FutureCopper");g.Box("Payment terminal",p+new Vector3(1,1.45f,0),new(.6f,.5f,.12f),"FutureCarbon");g.Box("Touch screen",p+new Vector3(1,1.45f,-.075f),new(.52f,.42f,.02f),"NeonCyan");}
    }
}
