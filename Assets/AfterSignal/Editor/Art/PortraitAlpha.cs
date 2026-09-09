using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    // Convert imagegen's chroma-key delivery into real PNG alpha at import time.
    // Flood only the panel background, preserving enclosed green props and every black garment.
    public static class PortraitAlpha
    {
        public static bool Apply(Texture2D image,int columns,int rows)
        {
            var p=image.GetPixels32();int w=image.width,h=image.height;var mask=new bool[p.Length];var queue=new Queue<int>();
            bool Green(int i)=>p[i].g>100&&p[i].g-p[i].r>35&&p[i].g-p[i].b>30&&p[i].a>0;
            void Add(int x,int y){int i=y*w+x;if(!mask[i]&&Green(i)){mask[i]=true;queue.Enqueue(i);}}
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                int x0=col*w/columns,x1=(col+1)*w/columns-1,y0=row*h/rows,y1=(row+1)*h/rows-1;
                for(int x=x0;x<=x1;x++){Add(x,y0);Add(x,y1);}for(int y=y0;y<=y1;y++){Add(x0,y);Add(x1,y);}
            }
            // Seed pure-key enclosed spaces as well, then flood their antialiased edges.
            // Merely clearing the pure pixels left green fringes inside loops of hair.
            for(int i=0;i<p.Length;i++)if(!mask[i]&&p[i].a>0&&p[i].g>205&&p[i].r<35&&p[i].b<35){mask[i]=true;queue.Enqueue(i);}
            int count=queue.Count;if(count<20)return false;
            while(queue.Count>0){int i=queue.Dequeue(),x=i%w,y=i/w;if(x>0)Add(x-1,y);if(x<w-1)Add(x+1,y);if(y>0)Add(x,y-1);if(y<h-1)Add(x,y+1);}
            // Enclosed pure-key gaps (between a hand and the torso) use the exact saturated key,
            // unlike the muted, outlined green glass props in the world portraits.
            for(int i=0;i<p.Length;i++)
            {
                var c=p[i];bool pure=c.g>205&&c.r<35&&c.b<35;
                if(mask[i]||pure){c.a=0;c.g=(byte)Mathf.Max(c.r,c.b);p[i]=c;continue;}
                int x=i%w,y=i/w;bool edge=x>0&&mask[i-1]||x<w-1&&mask[i+1]||y>0&&mask[i-w]||y<h-1&&mask[i+w];
                if(edge&&c.g>Mathf.Max(c.r,c.b)+12){float key=(c.g-Mathf.Max(c.r,c.b))/255f;c.a=(byte)Mathf.RoundToInt(255*(1-Mathf.Clamp01(key*1.35f)));c.g=(byte)Mathf.Min(c.g,Mathf.Max(c.r,c.b)+8);p[i]=c;}
            }
            if(image.format!=TextureFormat.RGBA32)image.Reinitialize(w,h,TextureFormat.RGBA32,false);
            image.SetPixels32(p);image.Apply(false,false);return true;
        }
        public static void SaveSources()
        {
            foreach(var name in new[]{"Portraits/SeoDialogue","StoryCast/CoreCast","StoryCast/WorldCast"})
            {
                string path="Assets/AfterSignal/Resources/Art/"+name+".png";var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(System.IO.File.ReadAllBytes(path));
                if(Apply(image,name.Contains("StoryCast")?4:1,name.Contains("CoreCast")?2:name.Contains("WorldCast")?3:1)){System.IO.File.WriteAllBytes(path,image.EncodeToPNG());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
                Object.DestroyImmediate(image);
            }
        }
        public static void BuildRelease(){SaveSources();ProjectBuilder.BuildRelease();}
    }
}
