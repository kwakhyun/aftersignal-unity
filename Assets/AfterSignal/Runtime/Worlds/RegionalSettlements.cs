using UnityEngine;
namespace AfterSignal
{
    public static class RegionalSettlements
    {
        public static int Houses{get;private set;}
        static readonly string[] Shops={"순이네 밥집","새벽 수선방","신호 수리소","골목 진료실","중고 부품","야간 전당포","충전소","공동 세탁소"};
        public static void Slum(CityGeometry g,VenueRuntime venue)
        {
            Houses=0;
            g.Box("Continuous compact village pavement",new(0,-.12f,0),new(280,.16f,220),"Pavement",true);
            foreach(var road in ExpansionRoads.Roads)for(int k=1;k<road.Length;k++)
            {
                var a=road[k-1]-venue.transform.position;var b=road[k]-venue.transform.position;
                if(Mathf.Abs((a.x+b.x)*.5f)>140||Mathf.Abs((a.z+b.z)*.5f)>110)continue;
                g.Box("Existing road through village",(a+b)*.5f-Vector3.up*.08f,new(22,.12f,(b-a).magnitude+.015f),"Asphalt",true,Quaternion.LookRotation(b-a));
            }
            for(int row=0;row<12;row++)for(int col=0;col<18;col++)
            {
                int id=row*18+col;var p=new Vector3(-126+col*14.8f+(row%2)*1.1f,0,-94+row*17);var world=venue.transform.TransformPoint(p);
                if(Mathf.Abs(world.x-RegionalCatalog.HomeQuarter.x)<20&&Mathf.Abs(world.z-RegionalCatalog.HomeQuarter.z)<25)continue;
                bool clear=true;foreach(var road in ExpansionRoads.Roads)for(int k=1;k<road.Length;k++)if(Vector3.Distance(world,FourCityCatalog.Closest(world,road[k-1],road[k]))<21)clear=false;
                if(!clear)continue;float w=8+id%4*.95f,d=8+id%3*.8f;
                Cottage(g,p,id,w,d,id%4==0);Houses++;venue.activityPoints.Add(p+new Vector3(0,.08f,-d*.5f-2.5f));
                if(id%14==0){var at=p+new Vector3(0,0,-d*.5f-2.7f);Stall(g,at,id);venue.staffPoints.Add(at+Vector3.forward*1.3f);g.Sign(Shops[id/14%8],p+new Vector3(0,2.9f,-d*.5f-.2f),.17f);}
                if(id%3==0){var a=p+new Vector3(w*.5f+1,0,0);g.Beam(a,a+Vector3.up*6,.12f,"Steel",true);for(int n=0;n<7;n++)g.Beam(a+new Vector3(n*3,6-Mathf.Sin(n/6f*Mathf.PI)*.7f,0),a+new Vector3((n+1)*3,6-Mathf.Sin((n+1)/6f*Mathf.PI)*.7f,0),.023f,"Steel");}
            }
            var home=RegionalCatalog.HomeQuarter-venue.transform.position;
            Cottage(g,home,501,12,10,false);Houses++;g.Sign("서하의 집",home+new Vector3(0,2.8f,-5.2f),.19f);
            var door=new GameObject("Seoha small house entrance").transform;door.SetParent(venue.transform,false);door.localPosition=home+new Vector3(0,1,-6.5f);
            var point=door.gameObject.AddComponent<InteractionPoint>();point.kind=InteractionKind.FacilityTravel;point.destination=StageId.Residence;point.hasArrival=true;point.arrival=CompactHome.Spawn;point.title="서하의 집 · E 들어가기";point.radius=2.7f;
            foreach(int side in new[]{-1,1}){var at=home+new Vector3(side*17,0,-14);Stall(g,at,side+2);venue.staffPoints.Add(at+Vector3.forward*1.4f);}
            for(int i=0;i<5;i++)venue.activityPoints.Add(home+new Vector3(-12+i*6,.08f,-16));
            g.Sign("새벽 골목\n오늘도 불을 끄지 않는다",new(0,5,-107),.28f);
            VenueService.Add(venue.transform,home+new Vector3(-17,1,-16),venue.Index,"새벽 골목 · 공동식사 / 암시장 / 이웃 의뢰");
            venue.viewPoint=home+new Vector3(0,.15f,-13);venue.lookPoint=home+new Vector3(0,3,0);venue.gameObject.AddComponent<RegionalDistrict>().Initialize(venue);
        }
        public static void Cottage(CityGeometry g,Vector3 p,int seed,float w,float d,bool upper)
        {
            float h=3.3f+(seed%5)*.32f;string wall=seed%4==0?"SlumRust":seed%4==1?"SlumPlaster":seed%4==2?"SlumPatina":"Concrete";
            g.Box("House foundation",p+new Vector3(0,-.08f,0),new(w+.7f,.16f,d+.7f),"Concrete",true);
            for(int side=-1;side<=1;side+=2)
            {
                g.Box("Patched party wall",p+new Vector3(side*w*.5f,h*.5f,0),new(.22f,h,d),wall,true);
                g.Box("Recessed doorway flank",p+new Vector3(side*(w*.25f+.55f),h*.5f,-d*.5f),new(w*.5f-1.1f,h,.22f),wall,true);
                g.Box("Window frame",p+new Vector3(side*w*.3f,1.9f,-d*.5f-.16f),new(2,1.3f,.12f),"Steel");
                g.Box("Living window",p+new Vector3(side*w*.3f,1.9f,-d*.5f-.24f),new(1.8f,1.1f,.03f),"Glass");
                for(int bar=0;bar<4;bar++)g.Beam(p+new Vector3(side*w*.3f-.8f+bar*.53f,1.3f,-d*.5f-.28f),p+new Vector3(side*w*.3f-.8f+bar*.53f,2.5f,-d*.5f-.28f),.035f,"Steel");
            }
            g.Box("Back wall",p+new Vector3(0,h*.5f,d*.5f),new(w,h,.22f),wall,true);g.Box("Door lintel",p+new Vector3(0,h-.35f,-d*.5f),new(2.2f,.7f,.24f),wall,true);
            for(int i=0;i<10;i++){float x=(i-4.5f)*(w+1)/10;g.Box("Corrugated metal seam",p+new Vector3(x,h+.09f,0),new((w+1)/10-.03f,.13f,d+1),seed%2==0?"SlumRust":"Steel",true,Quaternion.Euler(seed%3==0?5:0,0,0));g.Beam(p+new Vector3(x,h+.18f,-d*.5f),p+new Vector3(x,h+.18f,d*.5f),.035f,"FutureSilver");}
            g.Box("Door canopy",p+new Vector3(0,2.8f,-d*.5f-.8f),new(3.6f,.1f,2),seed%3==0?"SlumTarpaulin":"SlumRust",false,Quaternion.Euler(7,0,0));
            g.Box("Power meter",p+new Vector3(w*.5f+.2f,1.6f,-2),new(.35f,.7f,.55f),"Steel",true);
            g.Beam(p+new Vector3(w*.5f+.22f,1.9f,-2),p+new Vector3(w*.5f+.22f,h+.3f,-2),.035f,"FutureCopper");
            g.Box("Cooling unit",p+new Vector3(-w*.5f-.4f,2.1f,1),new(.7f,.75f,1.1f),"FutureSilver",true);
            for(int s=0;s<6;s++)g.Box("Cooling vent",p+new Vector3(-w*.5f-.77f,1.84f+s*.1f,1),new(.03f,.035f,.85f),"FutureCarbon");
            g.Box("Entry light",p+new Vector3(.9f,2.65f,-d*.5f-.19f),new(.14f,.18f,.08f),seed%2==0?"NeonWarm":"NeonCyan");
            g.Box("Salvaged glowing sign",p+new Vector3(-w*.4f,2.55f,-d*.5f-.3f),new(.22f,.8f,.12f),seed%3==0?"NeonRose":"NeonCyan");
            if(seed%3==1)for(int strip=0;strip<4;strip++)g.Box("Riveted repair plate",p+new Vector3(w*.5f+.13f,.8f+strip*.6f,1.2f),new(.04f,.5f,2.3f),"SlumRust");
            g.Box("Kitchen",p+new Vector3(-w*.28f,.88f,d*.32f),new(2.6f,.12f,1),"FutureSilver",true);
            g.Box("Dining table",p+new Vector3(-w*.2f,.72f,0),new(1.8f,.12f,1),"Wood",true);FourCityArchitecture.Bench(g,p+new Vector3(-w*.2f,0,-1));
            g.Box("Bed base",p+new Vector3(w*.28f,.28f,d*.22f),new(1.6f,.56f,2.7f),"SlumPatina",true);g.Box("Worn blanket",p+new Vector3(w*.28f,.62f,d*.22f),new(1.5f,.12f,2.6f),"SeatBlue");
            if(upper){var top=p+new Vector3(-w*.08f,h+.3f,d*.1f);g.Box("Rooftop annex",top+Vector3.up*1.3f,new(w*.64f,2.6f,d*.6f),"SlumPatina",true);g.Box("Annex roof",top+Vector3.up*2.7f,new(w*.7f,.12f,d*.65f),"Steel");g.Stairs(p+new Vector3(w*.5f+1.1f,0,-d*.5f),1.5f,h,6,"Steel");}
            else if(seed%3==0){g.Cylinder(p+new Vector3(w*.26f,h+.2f,d*.2f),.75f,1.6f,"SlumPatina",16);g.Beam(p+new Vector3(w*.26f,h+2,d*.2f),p+new Vector3(w*.26f,h+4,d*.2f),.035f,"Steel");}
            if(seed%2==0){g.Beam(p+new Vector3(-w*.5f,3.8f,-d*.5f-2),p+new Vector3(w*.5f,3.8f,-d*.5f-2),.02f,"Steel");for(int c=0;c<4;c++)g.Box("Laundry",p+new Vector3(c*1.3f-2,3.35f,-d*.5f-2),new(.8f,.9f,.02f),c%2==0?"SeatCoral":"SeatBlue");}
        }
        static void Stall(CityGeometry g,Vector3 p,int id)
        {
            g.Box("Reclaimed counter",p+Vector3.up*.5f,new(3.3f,1,1.3f),"SlumPatina",true);
            for(int s=-1;s<=1;s+=2)g.Beam(p+new Vector3(s*1.9f,0,1),p+new Vector3(s*1.9f,2.7f,1),.06f,"Steel");
            g.Box("Patched canopy",p+new Vector3(0,2.7f,0),new(4.3f,.06f,3),id%2==0?"SlumTarpaulin":"SlumRust",false,Quaternion.Euler(8,0,0));
            for(int i=0;i<6;i++)g.Box("Reconditioned stock",p+new Vector3(-1.3f+i*.5f,1.12f,0),new(.35f,.23f,.45f),id%2==0?"FutureSilver":"CanopyLeaf");
        }
        public static void Island(CityGeometry g,VenueRuntime venue)=>SmartIsland.Build(g,venue);
    }
}
