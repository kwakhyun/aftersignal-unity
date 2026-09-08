using UnityEngine;
namespace AfterSignal
{
    public static class DistrictTower
    {
        public static void Build(CityGeometry g,float w,float d,float h,int seed)
        {
            int city=FourCityCatalog.CityAt(g.root.position),style=seed%6;
            string shell=city==3?"FutureCeramic":style%3==0?"FutureCopper":style%3==1?"FutureCeramic":"FutureCarbon";
            string accent=city==1?"NovaNeonViolet":city==3?"NeonCyan":"NeonWarm";
            float podium=5.2f;
            Prism(g,Vector3.up*2.5f,w,d,5,"FutureCarbon",1.2f);
            for(int side=-1;side<=1;side+=2)
            {
                for(float x=-w*.5f+3;x<w*.5f-1;x+=3.5f)
                {g.Box("Retail storefront",new(x,2.3f,side*(d*.5f+.02f)),new(3,3.6f,.15f),"NovaWindow");g.Beam(new(x-1.65f,0,side*d*.5f),new(x-1.65f,5.2f,side*d*.5f),.16f,"FutureSilver");}
                g.Box("Podium canopy",new(0,4.6f,side*(d*.5f+1)),new(w+1,.18f,2.4f),shell);
                g.Box("Recessed fascia light",new(0,4.53f,side*(d*.5f+1.8f)),new(w-.8f,.055f,.055f),accent);
            }
            int floors=Mathf.Max(2,Mathf.FloorToInt((h-podium)/4));
            for(int f=0;f<floors;f++)
            {
                float t=f/(float)floors,y=podium+f*4;float ww=w*.94f,dd=d*.91f;Vector3 shift=Vector3.zero;
                if(style==0){float setback=Mathf.Floor(t*4)*.10f;ww*=1-setback;dd*=1-setback;}
                if(style==2){ww*=.63f+.37f*Mathf.SmoothStep(0,1,t);shift.x=w*.14f*t;}
                if(style==3){ww*=1-t*.32f;dd*=1-t*.18f;shift.x=Mathf.Sin(t*2)*w*.09f;}
                if(style==5){ww*=1-Mathf.Floor(t*3)*.13f;shift.z=Mathf.Sin(t*2.4f)*d*.10f;}
                if(style==1&&h>35)
                {
                    for(int s=-1;s<=1;s+=2){var p=new Vector3(s*w*.28f,y+1.9f,0);Prism(g,p,w*.37f,dd,3.6f,"NovaWindow",1);Prism(g,p+Vector3.up*1.9f,w*.39f,dd+.3f,.22f,shell,1);}
                    if(f==floors-2||f==floors/2)Prism(g,new(0,y+1.8f,0),w*.42f,dd*.6f,3.5f,shell,1);
                }
                else
                {
                    var p=shift+Vector3.up*(y+1.8f);Prism(g,p,ww,dd,3.6f,"NovaWindow",1.1f);
                    Prism(g,p+Vector3.up*1.88f,ww+.35f,dd+.35f,.23f,shell,1.2f);
                    if(style==0||style==5)for(int s=-1;s<=1;s+=2)g.Box("Terrace planted edge",shift+new Vector3(s*(ww*.5f-.4f),y+3.97f,0),new(.5f,.3f,dd-2),"CanopyLeaf");
                    if(style==4)for(float x=-ww*.5f+1;x<ww*.5f;x+=3.7f)for(int s=-1;s<=1;s+=2)g.Box("Vertical solar fin",shift+new Vector3(x,y+1.8f,s*(dd*.5f+.35f)),new(.18f,3.9f,.85f),"FutureCeramic");
                }
            }
            float roof=podium+floors*4;
            Prism(g,new(0,roof+.4f,0),w*.60f,d*.64f,.65f,shell,1.4f);
            g.Box("Lift overrun",new(0,roof+1.7f,0),new(w*.23f,2.6f,d*.25f),"FutureCarbon");
            for(int s=-1;s<=1;s+=2)
            {
                g.Box("Rooftop ventilation bank",new(s*w*.20f,roof+1.1f,0),new(w*.13f,1.6f,d*.35f),"FutureSilver");
                for(int n=0;n<6;n++)g.Box("Cooling louvre",new(s*w*.20f,roof+1.97f,-d*.15f+n*d*.06f),new(w*.11f,.06f,.13f),"FutureCarbon");
            }
            if(style==1||style==3)for(int s=-1;s<=1;s+=2)for(int f=0;f<floors-2;f+=3)
            {
                float y=podium+f*4;g.Beam(new(s*w*.43f,y,-d*.47f),new(-s*w*.28f,Mathf.Min(y+12,roof),-d*.47f),.55f,"FutureCopper");
            }
            // Broad, stable hidden collision has no decorative ledges to launch vehicles.
            g.Box("Building structural core",new(0,h*.5f,0),new(w*.62f,h,d*.62f),"FutureCarbon",true);
        }
        static void Prism(CityGeometry g,Vector3 center,float w,float d,float height,string mat,float bevel)
        {
            float x=w*.5f,z=d*.5f,b=Mathf.Min(bevel,Mathf.Min(x,z)*.25f);
            Vector3[] p={new(-x+b,0,-z),new(x-b,0,-z),new(x,0,-z+b),new(x,0,z-b),new(x-b,0,z),new(-x+b,0,z),new(-x,0,z-b),new(-x,0,-z+b)};
            for(int i=0;i<8;i++)
            {var a=center+p[i]-Vector3.up*height*.5f;var c=center+p[(i+1)%8]-Vector3.up*height*.5f;g.Quad(a,a+Vector3.up*height,c+Vector3.up*height,c,mat);g.Quad(center+Vector3.up*height*.5f,c+Vector3.up*height,a+Vector3.up*height,center+Vector3.up*height*.5f,mat);}
        }
    }
}
