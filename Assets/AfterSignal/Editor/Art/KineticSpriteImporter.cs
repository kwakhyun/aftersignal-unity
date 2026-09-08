using System;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public sealed class KineticSpriteImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Resources/Art/SeoKinetic/")||assetPath.Contains("/Resources/Art/SeoRefined/")||assetPath.Contains("/Resources/Art/VehiclePortraits/");
        public override uint GetVersion()=>3;
        static bool Background(Color32 c)=>c.a<24||Mathf.Min(c.r,Mathf.Min(c.g,c.b))>218&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<18;
        void OnPreprocessTexture()
        {
            if(!Match)return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.alphaIsTransparency=true;importer.isReadable=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
            var platform=importer.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;importer.SetPlatformTextureSettings(platform);
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);tex.LoadImage(System.IO.File.ReadAllBytes(assetPath));
            var px=tex.GetPixels32();string name=System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool cabin=assetPath.Contains("/VehiclePortraits/");
            int rows=cabin?4:name.StartsWith("Combat")?3:name=="Traversal"?4:2,cols=4;
            var xs=Cuts(tex,px,true,cols,0,tex.height);
            var data=new SpriteMetaData[rows*cols];var sizes=new float[data.Length];
            for(int col=0;col<cols;col++)
            {
                var ys=Cuts(tex,px,false,rows,xs[col],xs[col+1]);
                for(int row=0;row<rows;row++)
                {
                    int n=row*cols+col,ox=xs[col],oy=ys[rows-row-1],w=xs[col+1]-ox,h=ys[rows-row]-oy;
                    int lo=h,hi=0,left=w,right=0,hairTop=0;float mid=0;int count=0;
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                    {
                        var c=px[(oy+y)*tex.width+ox+x];if(Background(c))continue;
                        lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);left=Mathf.Min(left,x);right=Mathf.Max(right,x);
                        if(c.b>c.g+5&&c.r>c.g+2&&c.r>90&&c.b>100&&y>h*.4f)hairTop=Mathf.Max(hairTop,y);
                    }
                    for(int y=lo+(hi-lo)*42/100;y<lo+(hi-lo)*60/100;y++)for(int x=w/4;x<w*3/4;x++)
                        if(!Background(px[(oy+y)*tex.width+ox+x])){mid+=x;count++;}
                    float anchor=count>0?mid/count:(left+right)*.5f;
                    sizes[n]=Mathf.Max(1,(hairTop>lo+30?hairTop:hi)-lo);
                    if(cabin)
                    {
                        int margin=2;int l=Mathf.Max(0,left-margin),b=Mathf.Max(0,lo-margin);
                        int rw=Mathf.Min(w,right+margin+1)-l,rh=Mathf.Min(h,hi+margin+1)-b;
                        data[n]=new SpriteMetaData{name=name+"-"+n.ToString("00"),rect=new Rect(ox+l,oy+b,rw,rh),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(.5f,0)};
                        continue;
                    }
                    data[n]=new SpriteMetaData{name=name+"-"+n.ToString("00"),rect=new Rect(ox,oy,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(anchor/w,(float)Mathf.Max(1,lo)/h)};
                }
            }
            Array.Sort(sizes);float stature=sizes[sizes.Length/2];
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit=stature/2.08f;settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=data;
#pragma warning restore CS0618
            UnityEngine.Object.DestroyImmediate(tex);
        }
        static int[] Cuts(Texture2D tex,Color32[] pixels,bool xAxis,int count,int from,int to)
        {
            int length=xAxis?tex.width:tex.height;var cuts=new int[count+1];cuts[count]=length;
            for(int i=1;i<count;i++)
            {
                int expected=length*i/count,reach=length/count/3,best=expected;float score=float.MaxValue;
                for(int p=expected-reach;p<=expected+reach;p++)
                {
                    int ink=0;for(int q=from;q<to;q++)if(!Background(pixels[xAxis?q*tex.width+p:p*tex.width+q]))ink++;
                    float rank=ink*1000+Mathf.Abs(p-expected);if(rank<score){score=rank;best=p;}
                }
                cuts[i]=best;
            }
            return cuts;
        }
        void OnPostprocessTexture(Texture2D tex)
        {
            if(!Match)return;
            var p=tex.GetPixels32();var queue=new int[p.Length];var seen=new bool[p.Length];int head=0,tail=0;
            for(int n=0;n<p.Length;n++)if((n<tex.width||n>=p.Length-tex.width||n%tex.width==0||n%tex.width==tex.width-1||p[n].a==0)&&Background(p[n])){seen[n]=true;queue[tail++]=n;}
            while(head<tail)
            {
                int n=queue[head++],x=n%tex.width,y=n/tex.width;p[n].a=0;
                for(int d=0;d<4;d++)
                {
                    int nx=x+(d==0?-1:d==1?1:0),ny=y+(d==2?-1:d==3?1:0);
                    if(nx<0||ny<0||nx>=tex.width||ny>=tex.height)continue;
                    int k=ny*tex.width+nx;if(seen[k]||!Background(p[k]))continue;seen[k]=true;queue[tail++]=k;
                }
            }
            SeoEdgeFinish.Apply(p,seen,tex.width,tex.height);
            tex.SetPixels32(p);tex.Apply(false,false);
        }
    }
}
