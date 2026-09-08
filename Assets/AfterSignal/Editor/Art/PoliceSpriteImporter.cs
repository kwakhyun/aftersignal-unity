using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AfterSignal.Editor
{
    // Keep the generated source PNG intact. Import-time alpha keying and sprite
    // metadata provide consistent feet/scale without changing the painted artwork.
    public sealed class PoliceSpriteImporter : AssetPostprocessor
    {
        const string Folder = "/Resources/Art/LawEnforcement/";
        public override uint GetVersion() => 8;
        bool ActorSheet => assetPath.Contains(Folder) || assetPath.Contains("/Resources/Art/Gangs/") || Seo;
        bool Seo => assetPath.Contains("/Resources/Art/SeoIllustrated/");
        static bool Paper(Color32 c) => c.r > 218 && c.g > 218 && c.b > 218
            && Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) < 28;
        static bool Ink(Color32 c) => c.a >= 32 && !Paper(c);

        void OnPreprocessTexture()
        {
            if (!ActorSheet || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!image.LoadImage(File.ReadAllBytes(assetPath))) throw new InvalidDataException(assetPath);
                var pixels = image.GetPixels32();
                var figures = FindFigures(pixels, image.width, image.height);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var platform = importer.GetDefaultPlatformTextureSettings();
                platform.format = TextureImporterFormat.RGBA32;
                importer.SetPlatformTextureSettings(platform);
                importer.maxTextureSize = 4096;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                float height = Seo && assetPath.EndsWith("/Katana.png") ? 2.55f : Seo && assetPath.EndsWith("/Greatsword.png") ? 2.8f : Seo && assetPath.EndsWith("/Rope.png") ? 2.6f : 2.1f;
                settings.spritePixelsPerUnit = figures[0].height / height;
                settings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(settings);
                string name = Path.GetFileNameWithoutExtension(assetPath);
                var sprites = new SpriteMetaData[PoliceSpriteCatalog.FrameCount];
                for (int i = 0; i < sprites.Length; i++)
                {
                    var figure = figures[i];
                    float feet = !Seo && i == 7 ? figure.center.x : FeetCenter(pixels, image.width, figure);
                    var rect = Rect.MinMaxRect(Mathf.Max(0, figure.xMin - 2), Mathf.Max(0, figure.yMin - 2), Mathf.Min(image.width, figure.xMax + 2), Mathf.Min(image.height, figure.yMax + 2));
                    sprites[i] = new SpriteMetaData
                    {
                        name = name + "-" + i.ToString("00"), rect = rect,
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2((feet - rect.xMin) / rect.width, (figure.yMin - rect.yMin) / rect.height)
                    };
                }
#pragma warning disable CS0618
                importer.spritesheet = sprites;
#pragma warning restore CS0618
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        void OnPostprocessTexture(Texture2D image)
        {
            if (!ActorSheet) return;
            if(Seo){SpriteMatte.Apply(image);return;}
            var pixels = image.GetPixels32();
            var queue = new int[pixels.Length];
            int head = 0, tail = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (Seo ? pixels[i].r > 220 && pixels[i].g > 220 && pixels[i].b > 220 && Mathf.Max(pixels[i].r,Mathf.Max(pixels[i].g,pixels[i].b))-Mathf.Min(pixels[i].r,Mathf.Min(pixels[i].g,pixels[i].b)) < 13 : Paper(pixels[i])) { pixels[i].a = 0; queue[tail++] = i; }
            // Remove the pale antialias fringe connected to the white matte, keeping
            // enclosed metallic highlights and the colored muzzle flashes intact.
            while (head < tail)
            {
                int index = queue[head++], x = index % image.width, y = index / image.width;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= image.width || ny < 0 || ny >= image.height) continue;
                    int next = ny * image.width + nx;
                    var color = pixels[next];
                    if (color.a == 0 || color.r <= (Seo ? 185 : 150) || color.g <= (Seo ? 185 : 150) || color.b <= (Seo ? 185 : 150)
                        || Mathf.Max(color.r, Mathf.Max(color.g, color.b)) - Mathf.Min(color.r, Mathf.Min(color.g, color.b)) >= (Seo ? 25 : 50)) continue;
                    pixels[next].a = 0;
                    queue[tail++] = next;
                }
            }
            image.SetPixels32(pixels);
            image.Apply(false, false);
        }


        // A rectangular sprite can include a neighbouring sword/limb when the
        // authored figure crosses a grid cell. Restrict geometry to its own ink.
        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            if(!Seo)return;
            var pixels=texture.GetPixels32();int w=texture.width,h=texture.height;
            var labels=new int[pixels.Length];var queue=new int[pixels.Length];
            var parts=new List<Component>();
            for(int seed=0;seed<pixels.Length;seed++)
            {
                if(labels[seed]!=0||pixels[seed].a<32)continue;
                int id=parts.Count+1,head=0,tail=1,minX=seed%w,maxX=minX,minY=seed/w,maxY=minY;
                labels[seed]=id;queue[0]=seed;
                while(head<tail)
                {
                    int k=queue[head++],x=k%w,y=k/w;
                    minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,ny=y+dy;
                        if(nx<0||nx>=w||ny<0||ny>=h)continue;
                        int n=ny*w+nx;if(labels[n]!=0||pixels[n].a<32)continue;
                        labels[n]=id;queue[tail++]=n;
                    }
                }
                parts.Add(new Component{area=tail,bounds=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1)});
            }
            var main=parts.OrderByDescending(p=>p.area).Take(8).OrderByDescending(p=>p.bounds.center.y).ToArray();
            if(main.Length!=8)return;
            main=main.Take(4).OrderBy(p=>p.bounds.center.x).Concat(main.Skip(4).OrderBy(p=>p.bounds.center.x)).ToArray();
            var owner=new int[parts.Count+1];owner[0]=-1;
            for(int i=0;i<parts.Count;i++)
            {
                var part=parts[i];
                int own=Array.IndexOf(main,part);
                if(own>=0){owner[i+1]=own;continue;}
                int best=0;float gap=float.MaxValue;
                for(int n=0;n<8;n++){float d=Distance(main[n].bounds,part.bounds);if(d<gap){gap=d;best=n;}}
                owner[i+1]=best;
            }
            Array.Sort(sprites,(a,b)=>string.CompareOrdinal(a.name,b.name));
            for(int frame=0;frame<sprites.Length;frame++)
            {
                var sprite=sprites[frame];var rect=sprite.rect;
                var vertices=new List<Vector2>();var triangles=new List<ushort>();
                // OverrideGeometry consumes pixel coordinates; Unity applies the pivot and PPU.
                for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)
                {
                    int x=(int)rect.xMin;
                    while(x<(int)rect.xMax)
                    {
                        if(owner[labels[y*w+x]]!=frame){x++;continue;}
                        int start=x;while(x<(int)rect.xMax&&owner[labels[y*w+x]]==frame)x++;
                        if(vertices.Count>65000)break;
                        ushort v=(ushort)vertices.Count;
                        vertices.Add(new Vector2(start-rect.x,y-rect.y));
                        vertices.Add(new Vector2(x-rect.x,y-rect.y));
                        vertices.Add(new Vector2(x-rect.x,y+1-rect.y));
                        vertices.Add(new Vector2(start-rect.x,y+1-rect.y));
                        triangles.Add(v);triangles.Add((ushort)(v+2));triangles.Add((ushort)(v+1));
                        triangles.Add(v);triangles.Add((ushort)(v+3));triangles.Add((ushort)(v+2));
                    }
                }
                if(vertices.Count>0)sprite.OverrideGeometry(vertices.ToArray(),triangles.ToArray());
            }
        }

        sealed class Component
        {
            public int area;
            public RectInt bounds;
        }

        static RectInt[] FindFigures(Color32[] pixels, int width, int height)
        {
            var seen = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            var components = new List<Component>();
            for (int seed = 0; seed < pixels.Length; seed++)
            {
                if (seen[seed] || !Ink(pixels[seed])) continue;
                int head = 0, tail = 1, minX = seed % width, maxX = minX, minY = seed / width, maxY = minY;
                queue[0] = seed; seen[seed] = true;
                while (head < tail)
                {
                    int index = queue[head++], x = index % width, y = index / width;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int next = ny * width + nx;
                        if (seen[next] || !Ink(pixels[next])) continue;
                        seen[next] = true; queue[tail++] = next;
                    }
                }
                if (tail >= 5) components.Add(new Component { area = tail, bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1) });
            }
            var bodies = components.OrderByDescending(c => c.area).Take(8).ToArray();
            if (bodies.Length != 8 || bodies.Any(c => c.area < width * height / 1000)) throw new InvalidDataException("Actor art must contain eight separate full-body figures.");
            var byRow = bodies.OrderByDescending(c => c.bounds.center.y).ToArray();
            var ordered = byRow.Take(4).OrderBy(c => c.bounds.center.x).Concat(byRow.Skip(4).OrderBy(c => c.bounds.center.x)).ToArray();
            // Detached casings and muzzle flashes belong to the nearest body, not a new animation frame.
            foreach (var part in components.Except(bodies))
            {
                var nearest = ordered.OrderBy(c => Distance(c.bounds, part.bounds)).First();
                if (Distance(nearest.bounds, part.bounds) > height * .06f) continue;
                var a = nearest.bounds; var b = part.bounds;
                nearest.bounds = new RectInt(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax) - Mathf.Min(a.xMin, b.xMin), Mathf.Max(a.yMax, b.yMax) - Mathf.Min(a.yMin, b.yMin));
            }
            return ordered.Select(c => c.bounds).ToArray();
        }

        static float Distance(RectInt a, RectInt b)
        {
            float x = Mathf.Max(0, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
            float y = Mathf.Max(0, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
            return Mathf.Sqrt(x * x + y * y);
        }

        static float FeetCenter(Color32[] pixels, int width, RectInt bounds)
        {
            int left = bounds.xMax, right = bounds.xMin;
            for (int y = bounds.yMin; y < bounds.yMin + Mathf.Max(1, bounds.height / 8); y++)
                for (int x = bounds.xMin; x < bounds.xMax; x++) if (Ink(pixels[y * width + x])) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
            return (left + right) * .5f;
        }
    }
}
