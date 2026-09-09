using UnityEngine;
namespace AfterSignal.Editor
{
    // Import-time cleanup only: no extra textures or per-frame work in the player.
    static class SpriteSilhouetteFinish
    {
        public static void Apply(Color32[] pixels,int width,int height,bool darkCostume=false)
        {
            var seen=new bool[pixels.Length];var queue=new int[pixels.Length];
            bool Paper(Color32 c)=>c.a>16&&Mathf.Min(c.r,Mathf.Min(c.g,c.b))>220&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<16;
            if(darkCostume)for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start]||!Paper(pixels[start]))continue;
                int head=0,count=1;bool hair=false;queue[0]=start;seen[start]=true;
                while(head<count)
                {
                    int n=queue[head++],x=n%width,y=n/width;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int xx=x+dx,yy=y+dy;if(xx<0||xx>=width||yy<0||yy>=height)continue;int k=yy*width+xx;var c=pixels[k];
                        if(c.a>32&&c.b>c.g+18&&c.r>c.g+8&&c.r>145&&c.g>110&&c.b>175)hair=true;
                        if(!seen[k]&&Paper(c)){seen[k]=true;queue[count++]=k;}
                    }
                }
                // Neutral paper islands only; preserve regions adjoining lilac hair.
                if(count>=64&&!hair)for(int i=0;i<count;i++)pixels[queue[i]].a=0;
            }
            System.Array.Clear(seen,0,seen.Length);int minimum=Mathf.Max(24,width*height/50000);
            for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start]||pixels[start].a<16)continue;int head=0,count=1;queue[0]=start;seen[start]=true;
                while(head<count)
                {
                    int n=queue[head++],x=n%width,y=n/width;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||xx>=width||yy<0||yy>=height)continue;int k=yy*width+xx;if(!seen[k]&&pixels[k].a>=16){seen[k]=true;queue[count++]=k;}}
                }
                if(count<minimum)for(int i=0;i<count;i++)pixels[queue[i]].a=0;
            }
        }
    }
}
