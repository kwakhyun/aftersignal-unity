using System.IO;
using UnityEngine;
using UnityEditor;
namespace AfterSignal.Editor
{
    public sealed class SwimmingSpriteImporter:AssetPostprocessor
    {
        bool Match=>assetPath.Contains("/Art/SeoSwimming/");public override uint GetVersion()=>7;
        static bool Blank(Color32 c)=>c.a<16||Mathf.Min(c.r,Mathf.Min(c.g,c.b))>220&&Mathf.Max(c.r,Mathf.Max(c.g,c.b))-Mathf.Min(c.r,Mathf.Min(c.g,c.b))<20;
        void OnPreprocessTexture()
        {
            if(!Match)return;var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Multiple;t.alphaIsTransparency=true;t.mipmapEnabled=false;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.Uncompressed;t.isReadable=true;t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;
            // RGB source sheets acquire their alpha mask below, so the imported texture must retain it.
            var platform=t.GetDefaultPlatformTextureSettings();platform.format=TextureImporterFormat.RGBA32;t.SetPlatformTextureSettings(platform);
            var temp=new Texture2D(2,2);temp.LoadImage(File.ReadAllBytes(assetPath));OnPostprocessTexture(temp);int w=temp.width/4,h=temp.height/2;var pixels=temp.GetPixels32();var frames=new SpriteMetaData[8];float extent=0;
            for(int i=0;i<8;i++)
            {
                int ox=i%4*w,oy=(1-i/4)*h;int minX=w,minY=h,maxX=0,maxY=0;
                for(int y=2;y<h-2;y++)for(int x=2;x<w-2;x++)if(!Blank(pixels[(oy+y)*temp.width+ox+x])){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                extent+=Mathf.Max(maxX-minX,maxY-minY)/8f;
                frames[i]=new SpriteMetaData{name=Path.GetFileNameWithoutExtension(assetPath)+"-"+i.ToString("00"),rect=new Rect(ox,oy,w,h),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2((minX+maxX)*.5f/w,(minY+maxY)*.5f/h)};
            }
            var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spritePixelsPerUnit=extent/(assetPath.Contains("Tread")?1.65f:2.5f);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;t.SetTextureSettings(settings);
#pragma warning disable CS0618
            t.spritesheet=frames;
#pragma warning restore CS0618
            Object.DestroyImmediate(temp);
        }
        void OnPostprocessTexture(Texture2D texture)
        {
            if(!Match)return;var p=texture.GetPixels32();int w=texture.width,h=texture.height;var seen=new bool[p.Length];var queue=new int[p.Length];int begin=0,end=0;
            // Source poses may cross nominal grid boundaries; only seed the outer matte.
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(x==0||y==0||x==w-1||y==h-1){int n=y*w+x;if(!seen[n]&&Blank(p[n])){seen[n]=true;queue[end++]=n;}}
            while(begin<end){int k=queue[begin++],x=k%w,y=k/w;p[k].a=0;for(int d=0;d<4;d++){int xx=x+(d==0?-1:d==1?1:0),yy=y+(d==2?-1:d==3?1:0);if(xx<0||xx>=w||yy<0||yy>=h)continue;int n=yy*w+xx;if(!seen[n]&&Blank(p[n])){seen[n]=true;queue[end++]=n;}}}
            SpriteSilhouetteFinish.Apply(p,w,h,true);
            PackPoses(p,w,h);
            texture.SetPixels32(p);texture.Apply(false,false);
        }
        sealed class Pose{public int id,count,left,right,bottom,top;}
        static void PackPoses(Color32[] pixels,int width,int height)
        {
            var labels=new int[pixels.Length];var queue=new int[pixels.Length];var poses=new System.Collections.Generic.List<Pose>();int next=0;
            for(int start=0;start<labels.Length;start++)
            {
                if(labels[start]!=0||pixels[start].a<16)continue;
                var pose=new Pose{id=++next,left=width,bottom=height};int head=0,count=1;queue[0]=start;labels[start]=pose.id;
                while(head<count)
                {
                    int n=queue[head++],x=n%width,y=n/width;pose.left=Mathf.Min(pose.left,x);pose.right=Mathf.Max(pose.right,x);pose.bottom=Mathf.Min(pose.bottom,y);pose.top=Mathf.Max(pose.top,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||xx>=width||yy<0||yy>=height)continue;int k=yy*width+xx;if(labels[k]==0&&pixels[k].a>=16){labels[k]=pose.id;queue[count++]=k;}}
                }
                pose.count=count;if(count>pixels.Length/1000)poses.Add(pose);
            }
            // Segment before slicing so outstretched hands and boots cannot enter the next frame.
            if(poses.Count!=8){Debug.LogWarning("Swimming atlas needs eight separate silhouettes; found "+poses.Count);return;}
            poses.Sort((a,b)=>(b.top+b.bottom).CompareTo(a.top+a.bottom));
            var horizontal=System.Collections.Generic.Comparer<Pose>.Create((a,b)=>(a.left+a.right).CompareTo(b.left+b.right));poses.Sort(0,4,horizontal);poses.Sort(4,4,horizontal);
            int cellWidth=width/4,cellHeight=height/2;float scale=1;
            foreach(var pose in poses)scale=Mathf.Min(scale,(cellWidth-12f)/(pose.right-pose.left+1),(cellHeight-12f)/(pose.top-pose.bottom+1));
            var packed=new Color32[pixels.Length];
            for(int i=0;i<8;i++)
            {
                var pose=poses[i];int sw=pose.right-pose.left+1,sh=pose.top-pose.bottom+1,dw=Mathf.Max(1,Mathf.RoundToInt(sw*scale)),dh=Mathf.Max(1,Mathf.RoundToInt(sh*scale));int ox=i%4*cellWidth+(cellWidth-dw)/2,oy=(1-i/4)*cellHeight+(cellHeight-dh)/2;
                for(int y=0;y<dh;y++)for(int x=0;x<dw;x++){int sx=pose.left+Mathf.Min(sw-1,Mathf.FloorToInt(x/scale)),sy=pose.bottom+Mathf.Min(sh-1,Mathf.FloorToInt(y/scale)),n=sy*width+sx;if(labels[n]==pose.id)packed[(oy+y)*width+ox+x]=pixels[n];}
            }
            System.Array.Copy(packed,pixels,pixels.Length);
        }
    }
}
