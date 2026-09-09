using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace AfterSignal.Editor
{
    public static class SpriteQualityReview
    {
        [Serializable]sealed class Sheet{public string path;public int frames;public float minHeight,maxHeight;public int empty;}
        [Serializable]sealed class Review{public List<Sheet> sheets=new();public List<string> pages=new();}
        public static void Export()
        {
            var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-sprite-review-output");string output=arg>=0?args[arg+1]:"Artifacts/SpriteQuality/Before";Directory.CreateDirectory(output);
            var report=new Review();var names=new List<string>();Texture2D page=null;int row=0,pageIndex=0;
            var paths=new[]{"NpcDirections","NpcPolished","CivicForces","CyberSecurity","SeoKinetic","SeoRefined","SeoSwimming","VehiclePortraits"}.Where(folder=>Directory.Exists("Assets/AfterSignal/Resources/Art/"+folder)).SelectMany(folder=>Directory.GetFiles("Assets/AfterSignal/Resources/Art/"+folder,"*.png")).OrderBy(p=>p).ToArray();
            foreach(var path in paths)
            {
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();if(sprites.Length==0)continue;
                if(row==0){page=new Texture2D(1536,1280,TextureFormat.RGBA32,false);page.SetPixels(Enumerable.Repeat(new Color(.06f,.105f,.14f,1),1536*1280).ToArray());names.Clear();}
                names.Add(path);var sheet=new Sheet{path=path,frames=sprites.Length,minHeight=float.MaxValue};report.sheets.Add(sheet);
                var source=sprites[0].texture;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(source,rt);var previous=RenderTexture.active;RenderTexture.active=rt;var readable=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);readable.ReadPixels(new Rect(0,0,source.width,source.height),0,0);readable.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);var pixels=readable.GetPixels32();
                foreach(var sprite in sprites){var rect=sprite.rect;int lo=(int)rect.yMax,hi=(int)rect.y;for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*source.width+x].a>100){lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}if(hi<lo)sheet.empty++;else{float height=(hi-lo+1)/sprite.pixelsPerUnit;sheet.minHeight=Mathf.Min(sheet.minHeight,height);sheet.maxHeight=Mathf.Max(sheet.maxHeight,height);}}
                for(int col=0;col<8;col++)
                {
                    int index=sprites.Length==24?new[]{0,1,2,3,4,5,6,12}[col]:sprites.Length==16?new[]{0,1,2,3,4,8,12,15}[col]:Mathf.Min(col,sprites.Length-1);var s=sprites[index];var r=s.rect;float scale=Mathf.Min(175/r.width,148/r.height);
                    for(int y=0;y<r.height*scale;y++)for(int x=0;x<r.width*scale;x++){var c=(Color)pixels[((int)r.y+Mathf.Min((int)(y/scale),(int)r.height-1))*source.width+(int)r.x+Mathf.Min((int)(x/scale),(int)r.width-1)];int px=col*192+8+x,py=1280-(row+1)*160+6+y;page.SetPixel(px,py,Color.Lerp(new Color(.06f,.105f,.14f,1),c,c.a));}
                }
                UnityEngine.Object.DestroyImmediate(readable);row++;
                if(row==8||path==paths.Last()){string name="sheet-"+pageIndex++.ToString("00");page.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),page.EncodeToPNG());File.WriteAllLines(Path.Combine(output,name+".txt"),names);report.pages.Add(name);UnityEngine.Object.DestroyImmediate(page);row=0;}
            }
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(report,true));
        }
        public static void ExportAndBuild(){Export();ProjectBuilder.BuildRelease();}
    }
}
