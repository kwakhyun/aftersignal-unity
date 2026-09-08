using UnityEngine;
namespace AfterSignal
{
    public static class SmartIsland
    {
        public static Vector3 Road(float t)=>new(Mathf.Cos(t*Mathf.PI*2)*97,.08f,Mathf.Sin(t*Mathf.PI*2)*70);
        public static void Build(CityGeometry g,VenueRuntime venue)
        {
            int id=int.Parse(venue.Definition.id[^1].ToString());RegionalTerrain.IslandShore(g,id);
            for(int i=0;i<36;i++)
            {
                bool inner=i<12;float a=(i-(inner?0:12))*(Mathf.PI*2/(inner?12:24));var p=new Vector3(Mathf.Cos(a)*(inner?48:139),0,Mathf.Sin(a)*(inner?35:103));if(p.z< -70&&Mathf.Abs(p.x)<22)continue;
                var root=new GameObject("Advanced coastal habitat "+i).transform;root.SetParent(venue.transform,false);root.localPosition=p;root.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);
                var h=new CityGeometry(root);float w=12+i%3*2,d=10,height=5+i%3*2;
                h.Box("Ceramic habitat deck",new(0,-.08f,0),new(w+3,.16f,d+4),"FutureCeramic",true);
                for(int s=-1;s<=1;s+=2){h.Box("Carbon flank",new(s*w*.5f,height*.5f,0),new(.22f,height,d),"FutureCarbon",true);if(s>0)h.Box("Rear panoramic wall",new(0,height*.5f,d*.5f),new(w,height,.12f),"Glass",true);h.Box("Open entry glazing",new(s*(w*.25f+1),height*.5f,-d*.5f),new(w*.5f-2,height,.12f),"Glass",true);h.Beam(new(s*w*.5f,0,-d*.5f),new(s*(w*.5f-1),height,-d*.5f),.26f,"FutureSilver");}
                h.Box("Solar cantilever",new(0,height+.15f,0),new(w+4,.26f,d+4),id%2==0?"FutureSilver":"FutureCopper",true,Quaternion.Euler(0,0,i%2==0?4:-4));
                for(int n=0;n<7;n++)h.Box("Photovoltaic strip",new((n-3)*w/7,height+.5f,0),new(w/8,.04f,d),"NovaWindow");
                h.Box("Habitat light ribbon",new(0,height-.2f,-d*.5f-.08f),new(w,.12f,.08f),id%2==0?"NeonCyan":"NovaNeonViolet");
                h.Cylinder(new(w*.5f+1,0,3),.6f,3,"FutureCopper",16);h.Ring(new(w*.5f+1,2.8f,3),.7f,.7f,.08f,"NeonCyan",20);
                h.Box("Resident kitchen",new(-w*.3f,1,3),new(3,2,1),"FutureCeramic",true);h.Box("Rest pod",new(w*.28f,.4f,2),new(2,.8f,3),"SeatBlue",true);FourCityArchitecture.Bench(h,new(0,0,-1));
                if(i%4==0){h.Dome(new(0,height+.5f,0),new(w*.45f,3,4),"Glass",16,6);h.Sign("BIO / LIVING",new(0,3,-d*.5f-.2f),.18f);}
                h.Finish();venue.activityPoints.Add(p+root.localRotation*new Vector3(0,.1f,-8));
                var near=Road(a/(Mathf.PI*2));var walk=Vector3.Lerp(p,near,.78f);var start=p+root.localRotation*new Vector3(0,0,-7);var delta=walk-start;
                g.Box("Habitat pedestrian deck",(start+walk)*.5f+Vector3.up*.02f,new(5,.03f,delta.magnitude),"FutureCeramic",false,Quaternion.LookRotation(delta));
            }
            for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI/24;var p=new Vector3(Mathf.Cos(a)*166,0,Mathf.Sin(a)*122);var q=new Vector3(Mathf.Cos(a+Mathf.PI/24)*166,0,Mathf.Sin(a+Mathf.PI/24)*122);
                g.Box("Coastal service walk",(p+q)*.5f+Vector3.up*.025f,new(6,.035f,(q-p).magnitude+.03f),"FutureSilver",false,Quaternion.LookRotation(q-p));
                if(i%3==0){g.Cylinder(p+Vector3.up*.1f,2.4f,.7f,"FutureCarbon",16);g.Dome(p+Vector3.up*.8f,new(2.2f,1.8f,2.2f),"Glass",12,5);g.Ring(p+Vector3.up*.82f,2.3f,2.3f,.06f,"NeonCyan",16);}
                if(i%8==0){g.Beam(p,p+Vector3.up*9,.2f,"FutureSilver",true);g.Ring(p+Vector3.up*8,2,2,.16f,"NeonCyan",20);g.Box("Autonomous service node",p+new Vector3(2,1,0),new(1.2f,2,.8f),"FutureCeramic",true);}
            }
            for(int i=0;i<96;i++)
            {
                var p=Road(i/96f);var q=Road((i+1)/96f);g.Box("Electric mobility ring",(p+q)*.5f-Vector3.up*.08f,new(10,.12f,(q-p).magnitude+.02f),"Asphalt",true,Quaternion.LookRotation(q-p));
                if(i%8==0){var l=p*1.08f;g.Beam(l,l+Vector3.up*5,.12f,"FutureSilver",true);g.Box("Island LED luminaire",l+Vector3.up*5,new(2,.08f,.4f),"NeonCyan");venue.activityPoints.Add(l+Vector3.right*3);}
            }
            g.Box("Harbour promenade",new(0,-.04f,-131),new(16,.16f,112),"FutureSilver",true);g.Box("Autonomous ferry landing",new(0,-.02f,-195),new(20,.24f,42),"FutureCeramic",true);
            for(int s=-1;s<=1;s+=2){g.Beam(new(s*8,0,-173),new(s*8,8,-183),.35f,"FutureCopper",true);g.Beam(new(s*8,8,-183),new(s*8,5,-210),.35f,"FutureCopper");g.Box("Berth guidance",new(s*8,.14f,-195),new(.12f,.08f,40),"NeonCyan");}
            g.Dome(new(0,5,-185),new(14,5,22),"Glass",24,7);g.Cylinder(Vector3.zero,12,.3f,"FutureSilver",40);g.Dome(Vector3.up*7,new(20,8,17),"Glass",32,8);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var p=new Vector3(Mathf.Cos(a)*13,0,Mathf.Sin(a)*13);g.Beam(p,p*.8f+Vector3.up*11,.25f,"FutureCopper",true);g.Cylinder(p,.9f,5,id==2?"CanopyLeaf":"FutureCeramic",16);}
            g.Sign(venue.Definition.title+"\nSMART HARBOR / ZERO EMISSION",new(0,4,-164),.27f);venue.viewPoint=new(0,.2f,-173);venue.lookPoint=new(0,6,0);venue.staffPoints.Add(new(4,.1f,-183));
            VenueService.Add(venue.transform,new(3,1,-180),venue.Index,"스마트 섬 · 자율운항 항로 안내");
        }
    }
}
