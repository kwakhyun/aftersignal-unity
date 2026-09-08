using UnityEngine;
namespace AfterSignal.Editor
{
    // Remove only neutral matte connected to the image boundary; highlights inside the silhouette stay opaque.
    static class SpriteMatte
    {
        static bool Paper(Color32 c)=>c.a<16||Mathf.Min(c.r,Mathf.Min(c.g,c.b))>235&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<14;
        public static void Apply(Texture2D image)
        {
            var p=image.GetPixels32();var seen=new bool[p.Length];var q=new int[p.Length];int head=0,tail=0,w=image.width,h=image.height;
            for(int n=0;n<p.Length;n++)if((n<w||n>=p.Length-w||n%w==0||n%w==w-1)&&Paper(p[n])){seen[n]=true;q[tail++]=n;}
            while(head<tail)
            {
                int n=q[head++],x=n%w,y=n/w;p[n].a=0;
                for(int d=0;d<4;d++){int nx=x+(d==0?-1:d==1?1:0),ny=y+(d==2?-1:d==3?1:0);if(nx<0||nx>=w||ny<0||ny>=h)continue;int k=ny*w+nx;if(seen[k]||!Paper(p[k]))continue;seen[k]=true;q[tail++]=k;}
            }
            image.SetPixels32(p);image.Apply(false,false);
        }
    }
}
