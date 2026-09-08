using System;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    // Sheet gutters are detected from pixels; generated sheets rarely have perfectly equal cells.
    public sealed class DirectionalSheetImporter : AssetPostprocessor
    {
        bool Match => assetPath.Contains("/Resources/Art/SeoMotion/") || assetPath.Contains("/Resources/Art/NpcDirections/");
        public override uint GetVersion() => 3;
        static bool Background(Color32 c) => c.a < 32 || Mathf.Min(c.r, Mathf.Min(c.g,c.b)) > 235 && Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b)) < 14;
        void OnPreprocessTexture()
        {
            if (!Match) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.mipmapEnabled=false; importer.filterMode=FilterMode.Point; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=4096; importer.npotScale=TextureImporterNPOTScale.None;
            var platform=importer.GetDefaultPlatformTextureSettings(); platform.format=TextureImporterFormat.RGBA32; importer.SetPlatformTextureSettings(platform);
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false); tex.LoadImage(System.IO.File.ReadAllBytes(assetPath));
            var pixels=tex.GetPixels32(); int columns=Columns(tex,pixels);
            int[] xs=Cuts(tex,pixels,true,columns,0,tex.height);
            var data=new SpriteMetaData[16]; float height=0;
            for(int n=0;n<16;n++)
            {
                int source=columns==5?(n<8?0:10)+Mathf.FloorToInt(n%8*10/8f):n;
                int col=source%columns,row=3-source/columns;
                int[] ys=Cuts(tex,pixels,false,4,xs[col],xs[col+1]);
                int ox=xs[col],oy=ys[row],w=xs[col+1]-ox,h=ys[row+1]-oy;
                int lo=h,hi=0,left=w,right=0; float torso=0; int count=0;
                for(int y=0;y<h;y++) for(int x=0;x<w;x++)
                {
                    if(Background(pixels[(oy+y)*tex.width+ox+x]))continue;
                    lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);left=Mathf.Min(left,x);right=Mathf.Max(right,x);
                }
                for(int y=lo+(hi-lo)*55/100;y<lo+(hi-lo)*85/100;y++) for(int x=left;x<=right;x++)
                {if(!Background(pixels[(oy+y)*tex.width+ox+x])){torso+=x;count++;}}
                if(n<8)height+=Mathf.Max(1,hi-lo)/8f;
                data[n]=new SpriteMetaData{name=System.IO.Path.GetFileNameWithoutExtension(assetPath)+"-"+n.ToString("00"),rect=new Rect(ox,oy,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(count>0?torso/count/w:.5f,(float)Mathf.Max(1,lo)/h)};
            }
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit=height/(assetPath.Contains("Student")?1.45f:2.08f);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=data;
#pragma warning restore CS0618
            UnityEngine.Object.DestroyImmediate(tex);
        }
        static int[] Cuts(Texture2D tex,Color32[] pixels,bool horizontal,int count,int from,int to)
        {
            int length=horizontal?tex.width:tex.height;
            var cuts=new int[count+1];cuts[count]=length;
            for(int i=1;i<count;i++)
            {
                int expected=length*i/count,reach=length/count/3,best=expected;float score=float.MaxValue;
                for(int p=expected-reach;p<=expected+reach;p++)
                {
                    int ink=0;for(int q=from;q<to;q++)
                    {var c=pixels[horizontal?q*tex.width+p:p*tex.width+q];if(!Background(c))ink++;}
                    float rank=ink*1000+Mathf.Abs(p-expected);
                    if(rank<score){score=rank;best=p;}
                }
                cuts[i]=best;
            }
            return cuts;
        }
        static int Columns(Texture2D tex,Color32[] pixels)
        {
            int spans=0,start=-1;
            for(int x=0;x<=tex.width;x++)
            {
                int ink=0;if(x<tex.width)for(int y=0;y<tex.height;y++)if(!Background(pixels[y*tex.width+x]))ink++;
                bool filled=ink>tex.height*.015f;
                if(filled&&start<0)start=x;
                if(!filled&&start>=0){if(x-start>tex.width*.04f)spans++;start=-1;}
            }
            return spans==5?5:4;
        }
        void OnPostprocessTexture(Texture2D tex)
        {
            if(!Match)return;
            var p=tex.GetPixels32();var queue=new int[p.Length];var visited=new bool[p.Length];int head=0,tail=0;
            // Seed only the outer boundary. A grid line can cross white clothing or pale hair.
            for(int n=0;n<p.Length;n++)if((p[n].a==0||n%tex.width==0||n%tex.width==tex.width-1||n<tex.width||n>=p.Length-tex.width)&&Background(p[n]))
            {visited[n]=true;queue[tail++]=n;}
            while(head<tail)
            {
                int n=queue[head++],x=n%tex.width,y=n/tex.width;p[n].a=0;
                for(int dir=0;dir<4;dir++)
                {
                    int nx=x+(dir==0?-1:dir==1?1:0),ny=y+(dir==2?-1:dir==3?1:0);
                    if(nx<0||ny<0||nx>=tex.width||ny>=tex.height)continue;
                    int k=ny*tex.width+nx;if(visited[k]||!Background(p[k]))continue;
                    visited[k]=true;queue[tail++]=k;
                }
            }
            tex.SetPixels32(p);tex.Apply(false,false);
        }
    }
}
