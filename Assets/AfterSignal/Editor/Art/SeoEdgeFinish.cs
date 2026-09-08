using UnityEngine;
namespace AfterSignal.Editor
{
    // Colour decontamination is restricted to the cutout boundary; interior lilac hair stays intact.
    static class SeoEdgeFinish
    {
        public static void Apply(Color32[] pixels,bool[] exterior,int width,int height)
        {
            var original=(Color32[])pixels.Clone();
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++)
            {
                int n=y*width+x;if(exterior[n])continue;var c=original[n];
                bool edge=exterior[n-1]||exterior[n+1]||exterior[n-width]||exterior[n+width];if(!edge)continue;
                int min=Mathf.Min(c.r,Mathf.Min(c.g,c.b)),max=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                if(max-min>18||min<115)continue;
                float best=1000;
                for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++)
                {
                    int nx=x+ox,ny=y+oy;if(nx<1||ny<1||nx>=width-1||ny>=height-1)continue;int k=ny*width+nx;
                    if(exterior[k]||exterior[k-1]||exterior[k+1]||exterior[k-width]||exterior[k+width])continue;
                    var p=original[k];float value=Mathf.Max(p.r,Mathf.Max(p.g,p.b));if(value<best)best=value;
                }
                if(best>170)continue;
                float a=Mathf.Clamp01((255-max)/Mathf.Max(1,255-best));
                a=Mathf.Clamp(a,.08f,1);var color=(Color)c;
                color.r=Mathf.Clamp01((color.r-(1-a))/a);color.g=Mathf.Clamp01((color.g-(1-a))/a);color.b=Mathf.Clamp01((color.b-(1-a))/a);color.a=a*c.a/255f;pixels[n]=color;
            }
            // Extrude only RGB into transparent gutters to avoid white bilinear fringes.
            var finished=(Color32[])pixels.Clone();
            for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++)
            {
                int n=y*width+x;if(!exterior[n])continue;
                for(int d=0;d<4;d++){int k=n+(d==0?-1:d==1?1:d==2?-width:width);if(finished[k].a>32){var c=finished[k];c.a=0;pixels[n]=c;break;}}
            }
        }
    }
}
