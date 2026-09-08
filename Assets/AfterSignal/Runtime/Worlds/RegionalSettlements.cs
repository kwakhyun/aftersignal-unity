using UnityEngine;
namespace AfterSignal
{
    public static class RegionalSettlements
    {
        public static int Houses{get;private set;}
        public static void Slum(CityGeometry g,VenueRuntime venue)
        {
            Houses=0;
            for(int row=0;row<16;row++)for(int col=0;col<23;col++)
            {
                int id=row*23+col;var p=new Vector3(-790+col*70+(row%2)*9,0,-360+row*46);var world=venue.transform.TransformPoint(p);
                if(world.x>400&&world.x<700&&world.z>2540&&world.z<2760)continue;
                bool road=false;foreach(var line in FourCityCatalog.Roads)for(int k=1;k<line.Length;k++)if((world-FourCityCatalog.Closest(world,line[k-1],line[k])).sqrMagnitude<38*38)road=true;
                if(road)continue;House(g,p,id,0);Houses++;venue.activityPoints.Add(p+new Vector3(0,.1f,-19));
                if(id%6==0){House(g,p+Vector3.up*3.9f,id+999,0);Houses++;}
                var annex=p+new Vector3(35,0,id%2==0?5:-5);var annexWorld=venue.transform.TransformPoint(annex);bool clear=!(annexWorld.x>380&&annexWorld.x<720&&annexWorld.z>2520&&annexWorld.z<2780);
                foreach(var line in FourCityCatalog.Roads)for(int k=1;k<line.Length;k++)if((annexWorld-FourCityCatalog.Closest(annexWorld,line[k-1],line[k])).sqrMagnitude<36*36)clear=false;
                if(clear&&col<22&&id%3!=0){House(g,annex,id+2001,0);Houses++;}
                if(id%9==0){g.Beam(p+new Vector3(20,0,-20),p+new Vector3(20,9,-20),.15f,"Wood",true);g.Beam(p+new Vector3(20,9,-20),p+new Vector3(82,8,-20),.035f,"Steel");}
                if(id%7==0){MarketStall(g,p+new Vector3(22,0,-10),id);venue.staffPoints.Add(p+new Vector3(22,.1f,-8));}
            }
            for(int n=0;n<20;n++){var p=new Vector3(-690+n*70,0,405);g.Box("Flood retaining buttress",p+Vector3.up*9,new(14,18,8),"Concrete",true);g.Beam(p+new Vector3(0,19,0),p+new Vector3(0,36,0),.3f,"Steel");}
            g.Sign("새벽 저지대\n우리는 여전히 여기 살고 있다",new(-145,7,-440),.65f);
            var old=Resources.Load<GameObject>("WorldAssets/HavenQuarter");
            if(old)
            {
                var quarter=Object.Instantiate(old,venue.transform);quarter.name="Original Haven hometown";quarter.transform.position=RegionalCatalog.HomeQuarter;
                foreach(var walker in quarter.GetComponentsInChildren<ResidentWalker>()){walker.from+=RegionalCatalog.HomeQuarter;walker.to+=RegionalCatalog.HomeQuarter;}
                foreach(var point in quarter.GetComponentsInChildren<InteractionPoint>())if(point.kind==InteractionKind.UrbanTown){point.kind=InteractionKind.Furniture;point.title="새벽 저지대 / 애프터라이트 시내";}
            }
            // Main thoroughfare and informal businesses flank the original hometown.
            for(int n=0;n<14;n++){var p=new Vector3(-250+n*37,0,-83);MarketStall(g,p,n);venue.staffPoints.Add(p+new Vector3(0,.1f,2));}
            venue.viewPoint=RegionalCatalog.HomeQuarter-venue.transform.position+new Vector3(20,.15f,-10);venue.lookPoint=venue.viewPoint+new Vector3(25,3,20);
            VenueService.Add(venue.transform,new Vector3(-250,1,-88),venue.Index,"새벽 저지대 · 공동식사 / 암시장 / 이웃 의뢰");
            var runtime=venue.gameObject.AddComponent<RegionalDistrict>();runtime.Initialize(venue);
        }
        static void House(CityGeometry g,Vector3 p,int seed,int style)
        {
            float w=22+seed%3*5,d=26,h=style==0&&seed%6==0?3.8f:3.5f;string wall=style==0?(seed%3==0?"SlumRust":seed%3==1?"SlumPlaster":"SlumPatina"):style==1?"Wood":style==2?"SlumRust":"FutureCeramic";
            g.Box("Habitable house floor",p+Vector3.up*.02f,new(w,.15f,d),"Concrete",true);
            for(int side=-1;side<=1;side+=2){g.Box("House side wall",p+new Vector3(side*w*.5f,h*.5f,0),new(.2f,h,d),wall,true);g.Box("Front doorway flank",p+new Vector3(side*(w*.25f+1.4f),h*.5f,-d*.5f),new(w*.5f-2.8f,h,.25f),wall,true);}
            g.Box("House back wall",p+new Vector3(0,h*.5f,d*.5f),new(w,h,.24f),wall,true);
            for(int panel=0;panel<8;panel++){float x=(panel-3.5f)*(w+2)/8;g.Box("Corrugated roof panel",p+new Vector3(x,h+.18f,0),new((w+2)/8-.025f,.13f,d+2),seed%2==0?"SlumRust":"Steel",true,Quaternion.Euler(0,0,seed%2==0?2:-2));}
            for(int n=0;n<7;n++){var a=p+new Vector3(-w*.45f+n*w*.9f/6,h+.31f,-d*.52f);g.Beam(a,a+Vector3.forward*d*1.04f,.07f,"FutureSilver");}
            for(int side=-1;side<=1;side+=2){g.Box("House window",p+new Vector3(side*w*.27f,2,-d*.5f-.17f),new(3.4f,1.2f,.05f),"Glass");g.Box("Window shutter",p+new Vector3(side*w*.27f+2,2,-d*.5f-.24f),new(.55f,1.5f,.12f),"Wood");}
            g.Box("Shared dining table",p+new Vector3(-w*.2f,.78f,2),new(3,.13f,1.7f),"Wood",true);FourCityArchitecture.Bench(g,p+new Vector3(-w*.2f,0,-.1f));
            g.Box("House bed",p+new Vector3(w*.28f,.4f,6),new(2.3f,.7f,3.5f),"Wood",true);g.Box("Worn bedding",p+new Vector3(w*.28f,.82f,6),new(2.2f,.17f,3.4f),"SeatBlue");
            g.Box("Kitchen cupboard",p+new Vector3(-w*.28f,1,9),new(5,2,1.3f),wall,true);g.Box("Old television",p+new Vector3(w*.25f,1.8f,10),new(2,1.1f,.25f),"FutureCarbon");
            if(seed%4==0){g.Cylinder(p+new Vector3(w*.3f,h+.4f,5),1.25f,2,"SlumPatina",16);g.Beam(p+new Vector3(w*.3f,h+.6f,7),p+new Vector3(w*.3f,.3f,13.2f),.075f,"Steel");}
            if(seed%3==0){g.Stairs(p+new Vector3(-w*.5f-2,0,-8),2,h,10,"Steel");g.Box("Roof deck landing",p+new Vector3(-w*.5f-2,h,3),new(4,.2f,3),"Steel",true);}
            if(style==0)
            {
                g.Beam(p+new Vector3(-w*.5f-2,5,-14),p+new Vector3(w*.5f+2,5,-14),.026f,"Steel");for(int cloth=0;cloth<6;cloth++)g.Box("Laundry on line",p+new Vector3(-6+cloth*2,4.5f,-14),new(1,.9f,.035f),cloth%2==0?"SeatCoral":"SeatBlue");
                for(int pile=0;pile<3;pile++)g.Box("Reclaimed storage crate",p+new Vector3(w*.5f+2,.5f+pile*.7f,7),new(2,1,2),wall,true);
            }
        }
        static void MarketStall(CityGeometry g,Vector3 p,int id)
        {
            g.Box("Market counter",p+Vector3.up*.55f,new(5.5f,1.1f,2),"Wood",true);
            for(int s=-1;s<=1;s+=2)g.Beam(p+new Vector3(s*3,0,1),p+new Vector3(s*3,3.2f,1),.085f,"Steel");
            g.Box("Patched market awning",p+new Vector3(0,3.2f,-.2f),new(6.5f,.09f,5),id%2==0?"SlumTarpaulin":"SeatCoral",false,Quaternion.Euler(7,0,0));
            for(int n=0;n<7;n++)g.Box("Market stock",p+new Vector3(-2.2f+n*.72f,1.25f,0),new(.5f,.28f,.7f),id%3==0?"FutureSilver":id%3==1?"CanopyLeaf":"DistrictIvory");
        }
        public static void Island(CityGeometry g,VenueRuntime venue)
        {
            int id=int.Parse(venue.Definition.id.Substring(venue.Definition.id.Length-1));
            RegionalTerrain.IslandShore(g,id);
            for(int i=0;i<28;i++)
            {
                float angle=i*2.39996f,radius=35+i%4*31;var p=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius*.75f);
                if(p.z< -40&&Mathf.Abs(p.x)<30)continue;House(g,p,900+i,id==0?1:id==1?2:3);venue.activityPoints.Add(p+new Vector3(0,.12f,-16));
                if(i%3==0){MarketStall(g,p+Vector3.right*17,i);venue.staffPoints.Add(p+new Vector3(17,.12f,2));}
            }
            g.Box("Village harbour road",new(0,.05f,-88),new(16,.1f,145),"Pavement",true);
            g.Box("Landing pier",new(0,.05f,-187),new(15,.3f,78),"Wood",true);for(int s=-1;s<=1;s+=2)for(int n=0;n<8;n++)g.Beam(new(s*7,-6,-151-n*10),new(s*7,1,-151-n*10),.25f,"Wood",true);
            if(id==0)for(int n=0;n<14;n++){g.Beam(new(-65+n*6,0,-115),new(-65+n*6,4,-115),.1f,"Wood");g.Beam(new(-65+n*6,4,-115),new(-62+n*6,4,-125),.035f,"Steel");}
            if(id==1)for(int i=0;i<4;i++){var p=new Vector3(-100+i*55,0,100);g.Beam(p,p+Vector3.up*28,.8f,"Steel",true);g.Beam(p+Vector3.up*28,p+new Vector3(25,28,-15),.6f,"PaintAmber");g.Beam(p+new Vector3(22,28,-13),p+new Vector3(22,3,-13),.045f,"Steel");}
            if(id==2)for(int i=0;i<5;i++){var p=new Vector3(-120+i*52,0,100);g.Dome(p+Vector3.up*4,new(18,9,24),"Glass",16,5);g.Box("Hydroponic bed",p+Vector3.up*.8f,new(25,1.6f,30),"CanopyLeaf",true);}
            if(id==3){g.Cylinder(new(0,0,95),7,38,"FutureCeramic",32,4);g.Cylinder(new(0,38,95),5,5,"Glass",32);g.Ring(new(0,40,95),5.5f,5.5f,.24f,"NeonWarm",32);}
            g.Sign(venue.Definition.title,new(0,4,-142),.45f);venue.viewPoint=new(0,.2f,-173);venue.lookPoint=new(0,5,0);
            VenueService.Add(venue.transform,new(3,1,-180),venue.Index,"섬 마을 항로 안내");
        }
    }
}
