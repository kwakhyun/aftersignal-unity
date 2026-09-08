using UnityEngine;
namespace AfterSignal
{
    public static partial class FourCityArchitecture
    {
        public static void Tree(CityGeometry g,Vector3 p,float scale,int seed)
        {
            if(BotanicalTree.Place(g.root,p,scale,seed))return;
            g.Cylinder(p,.22f*scale,4.6f*scale,"Trunk",9,.09f*scale);
            for(int b=0;b<5;b++)
            {
                float a=seed*.71f+b*2.399f;var tip=p+new Vector3(Mathf.Cos(a)*1.25f,3.5f+b*.48f,Mathf.Sin(a)*1.25f)*scale;
                g.Beam(p+Vector3.up*2.7f*scale,tip,.12f*scale,"Trunk");
                g.Dome(tip-Vector3.up*.4f*scale,new Vector3(2.3f,2.3f,2.1f)*scale,b%2==0?"CanopyLeaf":"CanopyLight",9,4,false);
                g.Cylinder(tip-Vector3.up*.8f*scale,1.5f*scale,.8f*scale,b%2==0?"CanopyLeaf":"CanopyLight",9,2.2f*scale);
            }
        }
        static void GardenBeds(CityGeometry g,float rx,float rz,bool marine)
        {
            for(int sector=0;sector<12;sector++)
            {
                float angle=sector*Mathf.PI/6;var at=new Vector3(Mathf.Cos(angle)*rx*.43f,0,Mathf.Sin(angle)*rz*.43f);
                g.Cylinder(at+Vector3.up*.02f,13,.25f,"FutureCopper",32);g.Cylinder(at+Vector3.up*.28f,12.7f,.08f,"GardenSoil",32);
                for(int n=0;n<7;n++){float a=n*2.399f+sector;var p=at+new Vector3(Mathf.Cos(a)*(3+n),.35f,Mathf.Sin(a)*(3+n));if(Mathf.Abs(Mathf.Abs(p.x)-rx*.35f)<11&&p.z> -30&&p.z<50)continue;Tree(g,p,n==0?2.1f:1.1f+(n%3)*.25f,sector*7+n);}
                for(int n=0;n<16;n++){float a=n*Mathf.PI/8;var p=at+new Vector3(Mathf.Cos(a)*12,.4f,Mathf.Sin(a)*12);g.Dome(p,new(1.3f,.7f,1.1f),n%3==0?"SeatCoral":"CanopyLeaf",8,3,false);}
                if(sector%3==0){g.Sign(marine?"BIO / CORAL CONSERVATORY":"TROPICAL / LIVING CANOPY",at+new Vector3(0,1.8f,-14),.12f);Bench(g,at+new Vector3(0,0,-16));}
            }
        }
        public static void Streetscape(CityGeometry g,float w,float d,int seed,bool ruined)
        {
            g.Box("Block pedestrian podium",new(0,-.04f,0),new(w+14,.12f,d+14),ruined?"DeepDeck":"Pavement",true);
            for(int s=-1;s<=1;s+=2)
            {
                if(!ruined){Tree(g,new(s*(w*.5f+4),0,-d*.4f),1.2f,seed+s);Bench(g,new(s*(w*.5f+3),0,d*.3f));}
                else for(int i=0;i<7;i++){var p=new Vector3(s*(w*.5f+3+i%3),.8f+(i%3)*.4f,(i-3)*d/7);g.Box("Collapsed masonry",p,new(3+i%3,1.4f,2.5f),"NovaConcrete",true,Quaternion.Euler(i*8,seed*7+i*19,14+i*7));}
            }
            if(!ruined)
            {
                for(int s=-1;s<=1;s+=2){g.Box("Shopfront canopy",new(s*w*.26f,3.7f,-d*.5f-2),new(w*.42f,.28f,4),seed%2==0?"FutureCopper":"SeatBlue");g.Sign(seed%3==0?"ORBIT / CAFE":seed%3==1?"NIGHT / BOOKS":"NEO / MARKET",new(s*w*.26f,2.7f,-d*.5f-.8f),.10f);}
            }
        }
    }
}
