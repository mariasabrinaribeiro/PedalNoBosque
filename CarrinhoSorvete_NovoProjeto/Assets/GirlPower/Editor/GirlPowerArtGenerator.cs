using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// Procedural pixel-art generator for the Matinhos golden-hour coastal scenery
/// of "Girl Power - Primeira Entrega". Pure C# pixel kernel so it can run inside
/// Unity Editor or be previewed standalone without external dependencies.
/// </summary>
public static class GirlPowerArtGenerator
{
    public const int BackdropWidth = 1200;
    public const int BackdropHeight = 256;

    public struct Rgba
    {
        public byte r, g, b, a;
        public Rgba(int r, int g, int b, int a = 255)
        {
            this.r = (byte)Clamp(r, 0, 255);
            this.g = (byte)Clamp(g, 0, 255);
            this.b = (byte)Clamp(b, 0, 255);
            this.a = (byte)Clamp(a, 0, 255);
        }
    }

    public sealed class Canvas
    {
        public readonly int W;
        public readonly int H;
        public readonly Rgba[] Px;

        public Canvas(int w, int h)
        {
            W = w;
            H = h;
            Px = new Rgba[w * h];
        }

        public void Clear(Rgba c)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public void Set(int x, int y, Rgba c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            if (c.a == 0) return;
            if (c.a == 255)
            {
                Px[y * W + x] = c;
                return;
            }
            Rgba dst = Px[y * W + x];
            float sa = c.a / 255f;
            float da = dst.a / 255f;
            float outA = sa + da * (1f - sa);
            if (outA <= 0.0001f) return;
            int r = (int)((c.r * sa + dst.r * da * (1f - sa)) / outA);
            int g = (int)((c.g * sa + dst.g * da * (1f - sa)) / outA);
            int b = (int)((c.b * sa + dst.b * da * (1f - sa)) / outA);
            Px[y * W + x] = new Rgba(r, g, b, (int)(outA * 255f));
        }

        public Rgba Get(int x, int y)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return new Rgba(0, 0, 0, 0);
            return Px[y * W + x];
        }

        public void Rect(int x, int y, int w, int h, Rgba c)
        {
            int x0 = Clamp(x, 0, W);
            int y0 = Clamp(y, 0, H);
            int x1 = Clamp(x + w, 0, W);
            int y1 = Clamp(y + h, 0, H);
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++)
                    Set(px, py, c);
        }

        public void OutlineRect(int x, int y, int w, int h, Rgba fill, Rgba border)
        {
            Rect(x, y, w, h, border);
            if (w > 2 && h > 2) Rect(x + 1, y + 1, w - 2, h - 2, fill);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Rgba c)
        {
            int x0 = Clamp((int)Math.Floor(cx - rx - 1), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + rx + 1), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - ry - 1), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + ry + 1), 0, H - 1);
            float invRx = 1f / Math.Max(0.5f, rx);
            float invRy = 1f / Math.Max(0.5f, ry);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f - cy) * invRy;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) * invRx;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
            }
        }

        public void Circle(float cx, float cy, float r, Rgba c) => Ellipse(cx, cy, r, r, c);

        public void Line(int x0, int y0, int x1, int y1, Rgba c, int thickness = 1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int r = thickness / 2;
            while (true)
            {
                if (thickness <= 1) Set(x0, y0, c);
                else Rect(x0 - r, y0 - r, thickness, thickness, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void DitheredGradientY(int yStart, int yEnd, Rgba bottom, Rgba top)
        {
            int len = Math.Max(1, yEnd - yStart);
            for (int y = yStart; y < yEnd; y++)
            {
                float t = (y - yStart) / (float)len;
                for (int x = 0; x < W; x++)
                {
                    float dither = ((((x & 1) ^ (y & 1)) * 2) - 1) * 0.018f;
                    Set(x, y, Lerp(bottom, top, t + dither));
                }
            }
        }

        public void OutlineSprite(Rgba outlineColor)
        {
            Rgba[] copy = new Rgba[Px.Length];
            Array.Copy(Px, copy, Px.Length);
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a > 20) continue;
                    bool neighbor =
                        (x > 0 && copy[y * W + (x - 1)].a > 128) ||
                        (x < W - 1 && copy[y * W + (x + 1)].a > 128) ||
                        (y > 0 && copy[(y - 1) * W + x].a > 128) ||
                        (y < H - 1 && copy[(y + 1) * W + x].a > 128);
                    if (neighbor) Px[y * W + x] = outlineColor;
                }
            }
        }

        public byte[] ToRgbaBytes()
        {
            byte[] data = new byte[W * H * 4];
            for (int i = 0; i < Px.Length; i++)
            {
                data[i * 4 + 0] = Px[i].r;
                data[i * 4 + 1] = Px[i].g;
                data[i * 4 + 2] = Px[i].b;
                data[i * 4 + 3] = Px[i].a;
            }
            return data;
        }
    }

    // Shared warm golden-hour pixel-art palette
    private static readonly Rgba Outline = new Rgba(56, 38, 52);
    private static readonly Rgba WoodDark = new Rgba(94, 58, 44);
    private static readonly Rgba WoodMid = new Rgba(142, 92, 64);
    private static readonly Rgba WoodLight = new Rgba(184, 130, 88);
    private static readonly Rgba Cream = new Rgba(255, 240, 212);
    private static readonly Rgba CreamShade = new Rgba(228, 204, 170);
    private static readonly Rgba Coral = new Rgba(226, 98, 126);
    private static readonly Rgba CoralDark = new Rgba(178, 66, 96);
    private static readonly Rgba CoralLight = new Rgba(248, 146, 166);
    private static readonly Rgba Turquoise = new Rgba(82, 184, 174);
    private static readonly Rgba TurquoiseDark = new Rgba(52, 138, 136);
    private static readonly Rgba TurquoiseLight = new Rgba(142, 218, 208);
    private static readonly Rgba LeafDark = new Rgba(46, 92, 66);
    private static readonly Rgba LeafMid = new Rgba(74, 130, 82);
    private static readonly Rgba LeafLight = new Rgba(124, 172, 98);
    private static readonly Rgba Gold = new Rgba(255, 214, 118);
    private static readonly Rgba GoldWarm = new Rgba(242, 166, 78);

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    private static Rgba Lerp(Rgba a, Rgba b, float t)
    {
        t = Clamp01(t);
        return new Rgba(
            (int)(a.r + (b.r - a.r) * t),
            (int)(a.g + (b.g - a.g) * t),
            (int)(a.b + (b.b - a.b) * t),
            (int)(a.a + (b.a - a.a) * t));
    }

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }
    }

    public static void GenerateAll(Action<string, Canvas> writer)
    {
        writer("MatinhosEveningPixelArt.png", BuildMatinhosBackdrop());
        writer("OrlaSidewalkTile.png", BuildOrlaSidewalkTile());
        writer("FeiraCobbleTile.png", BuildFeiraCobbleTile());
        writer("PlazaTile.png", BuildPlazaTile());
        writer("KioskGirlPower.png", BuildKiosk());
        writer("SignboardNew.png", BuildSignboardNew());
        writer("FloristStand.png", BuildFloristStand());
        writer("FloristNPC.png", BuildFloristNpc());
        writer("IceCreamCart.png", BuildIceCreamCart());
        writer("MarketStallA.png", BuildMarketStall(true));
        writer("MarketStallB.png", BuildMarketStall(false));
        writer("FestoonLights.png", BuildFestoonLights());
        writer("PalmTree.png", BuildPalmTree());
        writer("StreetLamp.png", BuildStreetLamp());
        writer("CoastalBench.png", BuildCoastalBench());
        writer("PlanterBush.png", BuildPlanterBush());
        writer("CrateOranges.png", BuildCrateOranges());
        writer("CrateFlowers.png", BuildCrateFlowers());
        writer("PedestrianBlue.png", BuildPedestrian(true));
        writer("PedestrianCoral.png", BuildPedestrian(false));
        writer("RouteSignpost.png", BuildRouteSignpost());
        writer("SeagullA.png", BuildSeagull(false));
        writer("SeagullB.png", BuildSeagull(true));
        writer("CloudEvening.png", BuildCloudEvening());
        writer("WaterShimmer.png", BuildWaterShimmer());
        writer("WarmGlow.png", BuildWarmGlow());
        writer("ShadowOval.png", BuildShadowOval());
    }

#if UNITY_EDITOR
    public static void GenerateAndImportAll(string artAssetFolder)
    {
        string absFolder = Path.Combine(Application.dataPath, artAssetFolder.Replace("Assets/", "").TrimStart('/'));
        Directory.CreateDirectory(absFolder);

        GenerateAll((fileName, canvas) =>
        {
            Texture2D tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            Color32[] colors = new Color32[canvas.Px.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Rgba p = canvas.Px[i];
                colors[i] = new Color32(p.r, p.g, p.b, p.a);
            }
            tex.SetPixels32(colors);
            tex.Apply(false, false);
            File.WriteAllBytes(Path.Combine(absFolder, fileName), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        });

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureImporter(artAssetFolder + "/MatinhosEveningPixelArt.png", 100f, FilterMode.Point, TextureWrapMode.Clamp, 4096);
        ConfigureImporter(artAssetFolder + "/OrlaSidewalkTile.png", 32f, FilterMode.Point, TextureWrapMode.Repeat, 512);
        ConfigureImporter(artAssetFolder + "/FeiraCobbleTile.png", 32f, FilterMode.Point, TextureWrapMode.Repeat, 512);
        ConfigureImporter(artAssetFolder + "/PlazaTile.png", 32f, FilterMode.Point, TextureWrapMode.Repeat, 512);

        string[] propSprites =
        {
            "KioskGirlPower.png", "SignboardNew.png", "FloristStand.png", "FloristNPC.png",
            "IceCreamCart.png", "MarketStallA.png", "MarketStallB.png", "FestoonLights.png",
            "PalmTree.png", "StreetLamp.png", "CoastalBench.png", "PlanterBush.png",
            "CrateOranges.png", "CrateFlowers.png", "PedestrianBlue.png", "PedestrianCoral.png",
            "RouteSignpost.png", "SeagullA.png", "SeagullB.png", "CloudEvening.png", "WaterShimmer.png"
        };
        foreach (string s in propSprites)
            ConfigureImporter(artAssetFolder + "/" + s, 32f, FilterMode.Point, TextureWrapMode.Clamp, 1024);

        ConfigureImporter(artAssetFolder + "/WarmGlow.png", 32f, FilterMode.Bilinear, TextureWrapMode.Clamp, 256);
        ConfigureImporter(artAssetFolder + "/ShadowOval.png", 32f, FilterMode.Bilinear, TextureWrapMode.Clamp, 256);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ConfigureImporter(string path, float ppu, FilterMode filter, TextureWrapMode wrap, int maxSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = filter;
        imp.wrapMode = wrap;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = maxSize;
        TextureImporterSettings settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();
    }
#endif

    // =========================================================================
    // 1. MATINHOS EVENING BACKDROP (1200 x 256, aligned to world Y [-5.23..+5.53])
    //    worldY(py) = 0.15 + (py - 128) * 0.042
    //    py 212..255 => +3.68..+5.48 : Sunset Sky, Sun, Clouds, Morro do Boi, Skyline
    //    py 190..212 => +2.75..+3.68 : Atlantic Ocean & Golden Sun Shimmer
    //    py 166..190 => +1.75..+2.75 : Golden Beach Sand, Umbrellas, Lifeguard Post
    //    py 158..166 => +1.41..+1.75 : Beachfront Stone/Timber Mureta & Restinga
    //    py 104..158 => -0.86..+1.41 : Coastal Parkway Grass & Flowers
    //    py  40..104 => -3.55..-0.86 : Orla Boardwalk & Curb Base
    //    py   0.. 40 => -5.23..-3.55 : Avenida Atlantica Asphalt Road
    // =========================================================================
    public static Canvas BuildMatinhosBackdrop()
    {
        Canvas c = new Canvas(BackdropWidth, BackdropHeight);

        // --- SKY (py 212..256) ---
        c.DitheredGradientY(238, 256, new Rgba(136, 86, 134), new Rgba(64, 62, 114));
        c.DitheredGradientY(224, 238, new Rgba(226, 128, 122), new Rgba(136, 86, 134));
        c.DitheredGradientY(212, 224, new Rgba(255, 198, 136), new Rgba(226, 128, 122));

        // Soft twilight stars near zenith
        int[] starXs = { 44, 118, 212, 336, 488, 610, 742, 916, 1044, 1142 };
        int[] starYs = { 250, 246, 252, 248, 251, 245, 250, 247, 252, 248 };
        for (int i = 0; i < starXs.Length; i++)
            c.Set(starXs[i], starYs[i], new Rgba(255, 236, 206, 175));

        // Setting sun over the ocean bay (centered around x=660 so visible from the middle/orla!)
        int sunX = 640;
        int sunY = 221;
        c.Circle(sunX, sunY, 26, new Rgba(255, 188, 128, 70));
        c.Circle(sunX, sunY, 18, new Rgba(255, 208, 140, 120));
        c.Circle(sunX, sunY, 12, new Rgba(255, 226, 158, 225));
        c.Circle(sunX, sunY, 8, new Rgba(255, 246, 206, 255));

        // Secondary warm glow over start area horizon (x=280) for balanced golden light
        c.Ellipse(280, 216, 65, 14, new Rgba(255, 204, 144, 48));
        c.Ellipse(980, 216, 70, 14, new Rgba(255, 196, 142, 42));

        // Layered sunset cumulus clouds (warm sunlit bottoms, rosy lavender tops)
        DrawBackdropCloud(c, 92, 242, 54, 9);
        DrawBackdropCloud(c, 265, 236, 68, 11);
        DrawBackdropCloud(c, 472, 244, 58, 9);
        DrawBackdropCloud(c, 780, 240, 74, 11);
        DrawBackdropCloud(c, 1020, 243, 62, 10);

        // Distant Serra do Mar / Morro do Boi headland silhouettes on the horizon
        for (int x = 0; x < BackdropWidth; x++)
        {
            // Gentle coastal mountain profile on left & far right (like Matinhos/Caioba bay)
            double hFar =
                11.0 * Math.Exp(-Math.Pow((x - 140) / 115.0, 2.0)) +
                8.0 * Math.Exp(-Math.Pow((x - 310) / 95.0, 2.0)) +
                13.0 * Math.Exp(-Math.Pow((x - 1030) / 135.0, 2.0)) +
                6.5 * Math.Exp(-Math.Pow((x - 865) / 85.0, 2.0)) +
                2.0 * Math.Sin(x * 0.025) + 1.5;
            int hillTop = 212 + (int)Math.Max(0, hFar);
            for (int y = 212; y < hillTop; y++)
                c.Set(x, y, new Rgba(136, 94, 126, 235));

            double hNear =
                7.5 * Math.Exp(-Math.Pow((x - 95) / 80.0, 2.0)) +
                9.5 * Math.Exp(-Math.Pow((x - 1090) / 95.0, 2.0)) +
                4.5 * Math.Exp(-Math.Pow((x - 415) / 70.0, 2.0));
            int nearTop = 212 + (int)Math.Max(0, hNear);
            for (int y = 212; y < nearTop; y++)
                c.Set(x, y, new Rgba(104, 78, 110, 245));
        }

        // Subtle coastal Matinhos low-rise beachfront buildings clustered on the far left & right headlands
        // (leaving the central ocean bay open for the glowing sunset over the water!)
        int[] bldgXs = { 22, 54, 86, 124, 158, 198, 236, 360, 396, 898, 938, 976, 1018, 1056, 1094, 1134, 1168 };
        for (int i = 0; i < bldgXs.Length; i++)
        {
            int bx = bldgXs[i];
            int bw = 20 + (Math.Abs(Hash(i, 3)) % 14);
            int bh = 7 + (Math.Abs(Hash(i, 7)) % 14);
            Rgba body = (i % 2 == 0) ? new Rgba(118, 86, 108) : new Rgba(144, 100, 116);
            Rgba roof = new Rgba(166, 98, 102);
            c.Rect(bx, 212, bw, bh, body);
            c.Rect(bx - 1, 212 + bh, bw + 2, 2, roof);
            // Warm lit windows (organic, not every window lit)
            for (int wy = 214; wy < 212 + bh - 2; wy += 4)
            {
                for (int wx = bx + 3; wx < bx + bw - 3; wx += 5)
                {
                    if ((Math.Abs(Hash(wx, wy)) % 3) != 0)
                        c.Rect(wx, wy, 2, 2, new Rgba(255, 216, 146));
                }
            }
        }

        // Golden horizon shimmer line
        c.Rect(0, 211, BackdropWidth, 2, new Rgba(255, 206, 146, 220));

        // --- ATLANTIC OCEAN (py 190..212) ---
        c.DitheredGradientY(193, 212, new Rgba(66, 166, 178), new Rgba(46, 114, 146));
        c.DitheredGradientY(189, 194, new Rgba(112, 198, 196), new Rgba(66, 166, 178));

        // Golden sun reflection column rippling down the ocean
        for (int y = 192; y < 211; y++)
        {
            float distFromHorizon = (211 - y) / 19f;
            int halfW = (int)(12 + distFromHorizon * 28);
            int wobble = (int)(Math.Sin(y * 1.3) * 4);
            Rgba col = Lerp(new Rgba(255, 232, 170, 210), new Rgba(255, 190, 128, 125), distFromHorizon);
            c.Rect(sunX - halfW + wobble, y, halfW * 2, 1, col);
            if (y % 2 == 0)
                c.Rect(sunX - halfW / 2 - wobble, y, halfW, 1, new Rgba(255, 246, 204, 220));
        }

        // Distant sailboats on the sunset bay
        DrawSailboat(c, 520, 206);
        DrawSailboat(c, 775, 208);
        DrawSailboat(c, 290, 207);

        // Layered horizontal wave glints across the sea
        for (int x = 6; x < BackdropWidth; x += 29)
        {
            int wy = 195 + (Math.Abs(Hash(x, 11)) % 14);
            int wl = 9 + (Math.Abs(Hash(x, 17)) % 14);
            c.Rect(x, wy, wl, 1, new Rgba(148, 218, 216, 180));
            c.Rect(x + 4, wy - 1, wl / 2, 1, new Rgba(42, 120, 148, 160));
        }

        // Wavy frothy surf line lapping onto the wet sand (py 189..193)
        for (int x = 0; x < BackdropWidth; x++)
        {
            int wave = (int)Math.Round(Math.Sin(x * 0.045) * 1.6 + Math.Sin(x * 0.11) * 0.7);
            int foamY = 190 + wave;
            c.Set(x, foamY + 1, new Rgba(246, 240, 228, 235));
            c.Set(x, foamY, new Rgba(255, 248, 236, 255));
            c.Set(x, foamY - 1, new Rgba(196, 224, 216, 190));
            // Wet sand reflection strip right below foam
            c.Rect(x, foamY - 4, 1, 3, new Rgba(204, 158, 120, 255));
        }

        // --- GOLDEN BEACH SAND (py 166..189) ---
        c.DitheredGradientY(166, 188, new Rgba(226, 182, 130), new Rgba(242, 206, 156));
        // Subtle sand grain & scattered shells/pebbles
        for (int y = 168; y < 186; y++)
        {
            for (int x = 0; x < BackdropWidth; x++)
            {
                int h = Hash(x, y);
                if ((h & 63) == 0) c.Set(x, y, new Rgba(252, 224, 178, 180));
                else if ((h & 63) == 1) c.Set(x, y, new Rgba(204, 158, 112, 150));
            }
        }

        // Properly oriented colorful beach umbrellas (canopy on TOP, pole planted DOWN into sand!)
        DrawBeachUmbrella(c, 160, 173, new Rgba(224, 88, 112), Cream);
        DrawBeachUmbrella(c, 345, 176, new Rgba(242, 180, 82), Cream);
        DrawBeachUmbrella(c, 560, 174, new Rgba(74, 174, 168), Cream);
        DrawBeachUmbrella(c, 748, 177, new Rgba(228, 108, 96), Cream);
        DrawBeachUmbrella(c, 950, 174, new Rgba(236, 186, 88), Cream);
        DrawBeachUmbrella(c, 1110, 175, new Rgba(206, 94, 134), Cream);

        // Wooden lifeguard tower (Posto Salva-Vidas) on the beach!
        DrawLifeguardTower(c, 456, 172);
        DrawLifeguardTower(c, 855, 172);

        // --- COASTAL RESTINGA HEDGE & STONE MURETA (py 158..170) ---
        // Warm low stone retaining wall (mureta da orla)
        c.Rect(0, 158, BackdropWidth, 7, new Rgba(188, 174, 154));
        c.Rect(0, 164, BackdropWidth, 2, new Rgba(236, 224, 198)); // sunlit stone cap
        c.Rect(0, 158, BackdropWidth, 1, new Rgba(124, 108, 94));  // base shadow
        for (int x = 0; x < BackdropWidth; x += 18)
            c.Rect(x, 159, 1, 5, new Rgba(156, 142, 124));

        // Lush organic restinga clusters & sea-oats along the beach edge above the wall
        for (int x = 0; x < BackdropWidth; x += 22)
        {
            int h = Math.Abs(Hash(x, 23));
            if ((h % 5) == 0) continue; // natural openings showing the sunlit stone wall & beach
            int clusterW = 8 + (h % 10);
            int cx = x + (h % 6);
            c.Ellipse(cx, 166, clusterW * 0.55f, 3.8f, LeafDark);
            c.Ellipse(cx - 2, 167, clusterW * 0.42f, 3.0f, LeafMid);
            c.Ellipse(cx + 3, 167, clusterW * 0.36f, 2.8f, LeafMid);
            c.Ellipse(cx - 1, 168.5f, clusterW * 0.25f, 1.8f, LeafLight);
            if ((h % 3) == 0)
                c.Set(cx - 1, 169, (h % 2 == 0) ? CoralLight : Gold);
            if ((h % 4) == 0)
                c.Set(cx + 3, 168, Gold);
        }

        // --- COASTAL PARKWAY GARDEN STRIP (py 104..158) ---
        c.DitheredGradientY(104, 158, new Rgba(68, 114, 74), new Rgba(96, 144, 86));
        for (int y = 106; y < 156; y += 3)
        {
            for (int x = 0; x < BackdropWidth; x += 5)
            {
                int h = Math.Abs(Hash(x, y));
                if ((h % 5) == 0) c.Rect(x, y, 2, 1, new Rgba(118, 162, 96, 170));
                else if ((h % 7) == 0) c.Rect(x, y, 2, 1, new Rgba(54, 96, 66, 160));
                if ((h % 41) == 0) c.Set(x, y, (h % 2 == 0) ? Gold : CoralLight);
            }
        }

        // --- ORLA PROMENADE & LOWER CURB BASE (py 40..104) ---
        c.DitheredGradientY(54, 104, new Rgba(210, 200, 178), new Rgba(224, 214, 190));
        c.DitheredGradientY(42, 54, new Rgba(192, 182, 162), new Rgba(212, 202, 180));
        // Stone slab paving joints along the lower bench terrace (py 43..58)
        c.Rect(0, 52, BackdropWidth, 1, new Rgba(172, 162, 144));
        for (int x = 0; x < BackdropWidth; x += 22)
        {
            c.Rect(x, 43, 1, 15, new Rgba(174, 164, 146, 170));
            c.Rect(x + 1, 43, 1, 15, new Rgba(234, 224, 202, 90));
        }
        // Sunlit curb edge separating promenade from Avenida Atlantica
        c.Rect(0, 40, BackdropWidth, 3, new Rgba(238, 224, 198));
        c.Rect(0, 39, BackdropWidth, 1, new Rgba(112, 104, 98));
        for (int x = 0; x < BackdropWidth; x += 22)
            c.Rect(x, 40, 1, 3, new Rgba(176, 164, 142));

        // --- AVENIDA ATLANTICA COASTAL ASPHALT ROAD (py 0..39) ---
        c.DitheredGradientY(0, 39, new Rgba(58, 64, 78), new Rgba(78, 84, 98));
        // Subtle asphalt pebble texture
        for (int y = 1; y < 38; y++)
        {
            for (int x = 0; x < BackdropWidth; x++)
            {
                int h = Hash(x, y);
                if ((h & 31) == 0) c.Set(x, y, new Rgba(94, 98, 112, 120));
                else if ((h & 31) == 1) c.Set(x, y, new Rgba(48, 54, 68, 120));
            }
        }
        // Curb gutter shadow
        c.Rect(0, 36, BackdropWidth, 3, new Rgba(48, 52, 66, 160));
        // Warm golden-cream coastal road dashed center lane markings
        for (int x = 14; x < BackdropWidth; x += 64)
        {
            c.Rect(x, 18, 32, 2, new Rgba(226, 202, 150, 210));
            c.Rect(x, 17, 32, 1, new Rgba(168, 148, 112, 180));
        }

        return c;
    }

    private static void DrawBackdropCloud(Canvas c, int cx, int cy, int w, int h)
    {
        Rgba top = new Rgba(248, 186, 164, 210);
        Rgba mid = new Rgba(255, 208, 172, 230);
        Rgba bot = new Rgba(255, 228, 178, 240);
        c.Ellipse(cx, cy, w * 0.45f, h * 0.48f, mid);
        c.Ellipse(cx - w * 0.28f, cy - 1, w * 0.28f, h * 0.36f, mid);
        c.Ellipse(cx + w * 0.28f, cy - 1, w * 0.30f, h * 0.38f, mid);
        c.Ellipse(cx - w * 0.08f, cy + h * 0.22f, w * 0.24f, h * 0.34f, top);
        c.Rect(cx - (int)(w * 0.42f), cy - (int)(h * 0.35f), (int)(w * 0.84f), 2, bot);
    }

    private static void DrawSailboat(Canvas c, int x, int y)
    {
        c.Rect(x - 4, y, 9, 2, new Rgba(88, 66, 76));
        c.Line(x, y + 2, x, y + 9, new Rgba(124, 96, 98));
        // White-apricot sunset sail
        for (int dy = 0; dy < 7; dy++)
            c.Rect(x + 1, y + 2 + dy, Math.Max(1, 5 - dy), 1, new Rgba(255, 236, 206));
        // Tiny water reflection
        c.Rect(x - 3, y - 1, 7, 1, new Rgba(54, 118, 140, 170));
    }

    private static void DrawBeachUmbrella(Canvas c, int cx, int groundY, Rgba stripeA, Rgba stripeB)
    {
        // Cast sand shadow below
        c.Ellipse(cx + 3, groundY, 9, 2, new Rgba(176, 134, 96, 170));
        // Beach towel beside umbrella
        c.Rect(cx - 11, groundY - 1, 8, 2, stripeB);
        c.Rect(cx - 10, groundY - 1, 6, 1, stripeA);
        // Wooden pole going UP from groundY to canopy
        c.Rect(cx, groundY, 2, 10, WoodDark);
        // Arching dome canopy ON TOP (groundY + 7 .. groundY + 13)
        for (int dx = -10; dx <= 10; dx++)
        {
            int arch = (int)Math.Round((1.0 - (dx * dx) / 110.0) * 5.0);
            Rgba col = (Math.Abs(dx) / 3) % 2 == 0 ? stripeA : stripeB;
            for (int dy = 0; dy <= arch; dy++)
                c.Set(cx + dx, groundY + 8 + dy, col);
        }
        // Subtle top knob
        c.Rect(cx, groundY + 14, 2, 1, Gold);
    }

    private static void DrawLifeguardTower(Canvas c, int cx, int groundY)
    {
        // Shadow
        c.Ellipse(cx, groundY, 10, 2, new Rgba(176, 134, 96, 165));
        // Wooden stilts
        c.Rect(cx - 6, groundY, 2, 8, WoodDark);
        c.Rect(cx + 5, groundY, 2, 8, WoodDark);
        c.Line(cx - 5, groundY + 1, cx + 5, groundY + 7, WoodMid);
        c.Line(cx + 5, groundY + 1, cx - 5, groundY + 7, WoodMid);
        // Deck & cabin
        c.Rect(cx - 8, groundY + 8, 17, 2, WoodLight);
        c.Rect(cx - 6, groundY + 10, 13, 7, Cream);
        c.Rect(cx - 4, groundY + 12, 9, 4, new Rgba(88, 136, 152)); // window
        // Red-coral pitched roof
        c.Rect(cx - 8, groundY + 17, 17, 2, Coral);
        c.Rect(cx - 6, groundY + 19, 13, 2, CoralDark);
    }

    // =========================================================================
    // 2. TILED PAVING TEXTURES (Portuguese Stone Orla, Feira Cobbles, Plazas)
    // =========================================================================
    public static Canvas BuildOrlaSidewalkTile()
    {
        // 128 x 64 seamless horizontal tile (PPU 32 -> 4 x 2 world units)
        // Iconic Brazilian beachfront petit-pave (Portuguese stone waves)
        Canvas c = new Canvas(128, 64);
        Rgba lightStone = new Rgba(238, 226, 200);
        Rgba lightAlt = new Rgba(228, 216, 190);
        Rgba darkStone = new Rgba(188, 176, 156);
        Rgba darkAlt = new Rgba(174, 162, 144);
        Rgba grout = new Rgba(158, 146, 130);

        c.Clear(grout);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                // Top and bottom clean stone curb strips
                if (y >= 59)
                {
                    c.Set(x, y, (x % 16 == 0) ? grout : new Rgba(248, 238, 214));
                    continue;
                }
                if (y <= 4)
                {
                    c.Set(x, y, (x % 16 == 0) ? grout : (y == 4 ? new Rgba(246, 234, 210) : new Rgba(212, 198, 174)));
                    continue;
                }

                // Portuguese stone wave pattern!
                double waveCenter1 = 22.0 + Math.Sin(x * (2.0 * Math.PI / 64.0)) * 7.5;
                double waveCenter2 = 44.0 + Math.Sin(x * (2.0 * Math.PI / 64.0)) * 7.5;
                bool inWave = Math.Abs(y - waveCenter1) < 4.4 || Math.Abs(y - waveCenter2) < 4.4;

                // Small 4x4 cobblestone tesserae with subtle mortar & tone variation
                int tx = x / 4;
                int ty = y / 4;
                bool mortar = (x % 4 == 0 && (y % 4 != 2)) || (y % 4 == 0 && (x % 4 != 2));
                if (mortar && ((Hash(tx, ty) & 1) == 0))
                {
                    c.Set(x, y, grout);
                    continue;
                }

                int h = Math.Abs(Hash(tx, ty));
                if (inWave)
                    c.Set(x, y, (h % 3 == 0) ? darkAlt : darkStone);
                else
                    c.Set(x, y, (h % 3 == 0) ? lightAlt : lightStone);
            }
        }
        return c;
    }

    public static Canvas BuildFeiraCobbleTile()
    {
        // 96 x 48 seamless horizontal tile (PPU 32 -> 3 x 1.5 world units)
        // Warm terracotta / ochre rustic cobblestones for the Feira alley
        Canvas c = new Canvas(96, 48);
        Rgba grout = new Rgba(124, 92, 74);
        c.Clear(grout);

        Rgba[] tones =
        {
            new Rgba(212, 168, 132),
            new Rgba(198, 152, 118),
            new Rgba(184, 138, 106),
            new Rgba(222, 180, 142)
        };

        // Top & bottom warm stone edging
        c.Rect(0, 44, 96, 4, new Rgba(186, 148, 118));
        c.Rect(0, 47, 96, 1, new Rgba(236, 206, 174));
        c.Rect(0, 0, 96, 4, new Rgba(172, 134, 106));
        for (int x = 0; x < 96; x += 12)
        {
            c.Rect(x, 44, 1, 4, grout);
            c.Rect(x, 0, 1, 4, grout);
        }

        // Running-bond cobble bricks in the middle (y 4..44)
        for (int row = 0; row < 5; row++)
        {
            int y = 5 + row * 8;
            int offset = (row % 2) * 6;
            for (int col = -1; col < 9; col++)
            {
                int x = col * 12 + offset;
                int h = Math.Abs(Hash(col + 10, row + 20));
                Rgba baseCol = tones[h % tones.Length];
                Rgba hi = Lerp(baseCol, Cream, 0.22f);
                for (int px = 0; px < 11; px++)
                {
                    int wx = ((x + px) % 96 + 96) % 96;
                    for (int py = 0; py < 7; py++)
                        c.Set(wx, y + py, py == 6 ? hi : baseCol);
                }
            }
        }
        return c;
    }

    public static Canvas BuildPlazaTile()
    {
        // 64 x 64 seamless plaza stone tile (PPU 32 -> 2 x 2 world units)
        Canvas c = new Canvas(64, 64);
        Rgba grout = new Rgba(162, 144, 124);
        Rgba creamStone = new Rgba(234, 218, 190);
        Rgba warmStone = new Rgba(222, 202, 174);
        Rgba accent = new Rgba(206, 152, 126);

        c.Clear(grout);
        for (int ty = 0; ty < 4; ty++)
        {
            for (int tx = 0; tx < 4; tx++)
            {
                int x = tx * 16;
                int y = ty * 16;
                bool checker = ((tx + ty) % 2) == 0;
                Rgba col = checker ? creamStone : warmStone;
                c.Rect(x + 1, y + 1, 14, 14, col);
                c.Rect(x + 1, y + 14, 14, 1, new Rgba(248, 236, 212));
                // Small terracotta diamond inlay at intersections
                if (checker)
                    c.Rect(x + 6, y + 6, 4, 4, accent);
            }
        }
        return c;
    }

    // =========================================================================
    // 3. HERO LANDMARKS: GIRL POWER KIOSK, SIGNBOARD, FLORIST STAND, ICE CREAM CART
    // =========================================================================
    public static Canvas BuildKiosk()
    {
        // 160 x 104 (PPU 32 -> 5.0 x 3.25 world units)
        Canvas c = new Canvas(160, 104);

        // Ground deck & shadow
        c.Ellipse(80, 7, 74, 6, new Rgba(56, 40, 52, 110));
        c.OutlineRect(10, 6, 140, 10, WoodLight, Outline);
        for (int x = 18; x < 148; x += 12) c.Rect(x, 7, 1, 8, WoodDark);

        // Back interior wall (warm cozy evening glow inside kiosk!)
        c.Rect(20, 16, 120, 52, new Rgba(92, 68, 86));
        c.Rect(24, 24, 112, 42, new Rgba(136, 98, 106));
        // Warm interior menu shelf & ice cream tubs glow
        c.Rect(30, 44, 42, 16, new Rgba(56, 46, 62)); // chalkboard menu
        c.Rect(32, 55, 22, 2, Cream);
        c.Rect(32, 51, 32, 1, CoralLight);
        c.Rect(32, 48, 28, 1, Gold);
        // Shelf of cones & syrup bottles on right
        c.Rect(82, 44, 48, 3, WoodLight);
        for (int i = 0; i < 5; i++)
        {
            Rgba bottle = i % 3 == 0 ? Coral : (i % 3 == 1 ? Turquoise : Gold);
            c.Rect(86 + i * 9, 47, 5, 9, bottle);
            c.Rect(87 + i * 9, 56, 3, 2, Cream);
        }

        // Open wooden support posts
        c.OutlineRect(16, 14, 10, 58, WoodMid, Outline);
        c.Rect(18, 15, 3, 56, WoodLight);
        c.OutlineRect(134, 14, 10, 58, WoodMid, Outline);
        c.Rect(136, 15, 3, 56, WoodLight);

        // Front counter (coastal turquoise wood slats + warm cream marble counter top)
        c.OutlineRect(22, 14, 116, 24, Turquoise, Outline);
        for (int x = 28; x < 136; x += 8)
            c.Rect(x, 15, 1, 22, TurquoiseDark);
        // Coral decorative waist band
        c.Rect(23, 22, 114, 5, Coral);
        c.Rect(23, 26, 114, 1, CoralLight);
        // Cream counter top overhang
        c.OutlineRect(18, 36, 124, 7, Cream, Outline);
        c.Rect(20, 40, 120, 2, new Rgba(255, 250, 236));

        // Glass ice-cream display case on counter!
        c.OutlineRect(46, 43, 68, 14, new Rgba(184, 232, 236, 215), Outline);
        // Colorful ice cream tubs inside display case (morango, baunilha, chocolate, menta)
        c.Rect(50, 44, 13, 7, CoralLight);
        c.Rect(65, 44, 13, 7, Cream);
        c.Rect(80, 44, 13, 7, new Rgba(124, 78, 58));
        c.Rect(95, 44, 15, 7, TurquoiseLight);
        // Glass specular glare
        c.Line(52, 54, 62, 46, new Rgba(255, 255, 255, 165));
        c.Line(86, 54, 96, 46, new Rgba(255, 255, 255, 165));

        // Potted coastal plants flanking the kiosk
        DrawKioskPlant(c, 12, 14);
        DrawKioskPlant(c, 148, 14);

        // Awning cast shadow across top of interior
        c.Rect(18, 63, 124, 5, new Rgba(56, 38, 52, 110));

        // Warm hanging fairy bulbs under the awning
        for (int x = 28; x <= 132; x += 13)
        {
            c.Circle(x, 64, 4, new Rgba(255, 220, 138, 90));
            c.Rect(x - 1, 63, 2, 2, Gold);
        }

        // Scalloped striped coastal awning (coral pink & warm cream)
        c.OutlineRect(8, 67, 144, 18, Coral, Outline);
        for (int i = 0; i < 12; i++)
        {
            int sx = 9 + i * 12;
            Rgba col = (i % 2 == 0) ? Coral : Cream;
            Rgba shade = (i % 2 == 0) ? CoralDark : CreamShade;
            c.Rect(sx, 68, 12, 16, col);
            c.Rect(sx, 68, 12, 3, shade);
            // Scalloped bottom valance
            c.Ellipse(sx + 6, 67, 6, 3, col);
            c.Rect(sx, 65, 12, 1, Outline);
        }
        // Timber roof ridge above awning
        c.OutlineRect(12, 84, 136, 5, WoodMid, Outline);
        c.Rect(14, 87, 132, 1, WoodLight);

        // Ornate header signboard plaque for "GIRL POWER" (top center y 87..102)
        c.OutlineRect(38, 86, 84, 16, new Rgba(74, 50, 78), Outline);
        c.Rect(40, 88, 80, 12, new Rgba(92, 62, 96));
        c.Rect(41, 99, 78, 1, Gold);
        c.Rect(41, 88, 78, 1, GoldWarm);
        // Cute mini ice-cream cone emblem on left & right of header plaque
        DrawMiniCone(c, 46, 90);
        DrawMiniCone(c, 114, 90);

        return c;
    }

    private static void DrawKioskPlant(Canvas c, int cx, int y)
    {
        c.OutlineRect(cx - 6, y, 12, 9, new Rgba(186, 102, 74), Outline);
        c.Rect(cx - 7, y + 7, 14, 3, new Rgba(212, 122, 88));
        c.Circle(cx, y + 14, 7, LeafDark);
        c.Circle(cx - 2, y + 16, 5, LeafMid);
        c.Circle(cx + 3, y + 15, 5, LeafLight);
        c.Set(cx - 2, y + 17, CoralLight);
        c.Set(cx + 3, y + 16, Gold);
    }

    private static void DrawMiniCone(Canvas c, int cx, int y)
    {
        c.Rect(cx - 1, y, 2, 3, GoldWarm);
        c.Rect(cx - 2, y + 3, 4, 2, GoldWarm);
        c.Circle(cx, y + 6, 2.6f, CoralLight);
        c.Set(cx, y + 8, Cream);
    }

    public static Canvas BuildSignboardNew()
    {
        // 112 x 28 (PPU 32 -> 3.5 x 0.875 world units)
        Canvas c = new Canvas(112, 28);
        c.OutlineRect(2, 2, 108, 24, new Rgba(72, 48, 78), Outline);
        c.Rect(4, 4, 104, 20, new Rgba(90, 62, 96));
        // Double gold decorative frame
        c.Rect(5, 22, 102, 1, Gold);
        c.Rect(5, 5, 102, 1, GoldWarm);
        c.Rect(5, 5, 1, 18, Gold);
        c.Rect(106, 5, 1, 18, Gold);
        // Corner glowing star rivets
        c.Rect(6, 21, 2, 2, Cream);
        c.Rect(104, 21, 2, 2, Cream);
        c.Rect(6, 6, 2, 2, Cream);
        c.Rect(104, 6, 2, 2, Cream);
        return c;
    }

    public static Canvas BuildFloristStand()
    {
        // 152 x 104 (PPU 32 -> 4.75 x 3.25 world units)
        Canvas c = new Canvas(152, 104);

        // Ground shadow & wooden platform
        c.Ellipse(76, 7, 70, 6, new Rgba(56, 40, 52, 110));
        c.OutlineRect(12, 6, 128, 8, WoodLight, Outline);

        // Rustic timber back trellis with climbing coastal vines
        c.Rect(20, 14, 112, 54, new Rgba(116, 84, 66));
        for (int x = 24; x < 130; x += 10) c.Rect(x, 15, 2, 52, WoodDark);
        for (int y = 20; y < 66; y += 10) c.Rect(21, y, 110, 2, WoodDark);
        // Climbing vines & blooms on trellis
        for (int i = 0; i < 16; i++)
        {
            int vx = 24 + (i * 19) % 104;
            int vy = 42 + (i * 11) % 24;
            c.Circle(vx, vy, 4, LeafMid);
            c.Circle(vx + 1, vy + 1, 2, LeafLight);
            if (i % 2 == 0) c.Rect(vx, vy + 1, 2, 2, i % 4 == 0 ? CoralLight : Gold);
        }

        // Timber roof posts
        c.OutlineRect(16, 12, 9, 60, WoodMid, Outline);
        c.OutlineRect(127, 12, 9, 60, WoodMid, Outline);

        // Front sage-green wooden flower counter
        c.OutlineRect(20, 12, 112, 24, new Rgba(92, 126, 90), Outline);
        for (int x = 26; x < 130; x += 8) c.Rect(x, 13, 1, 22, LeafDark);
        c.OutlineRect(17, 34, 118, 6, WoodLight, Outline);

        // Overflowing buckets & terracotta pots of vibrant flowers along the counter!
        Rgba[] flowerColors =
        {
            new Rgba(244, 116, 156), // roses
            new Rgba(255, 206, 86),  // sunflowers
            new Rgba(174, 124, 218), // lavender
            new Rgba(252, 132, 102), // coral hibiscus
            new Rgba(255, 236, 142), // daisies
            new Rgba(236, 98, 142)   // bougainvillea
        };
        for (int i = 0; i < 6; i++)
        {
            int fx = 28 + i * 19;
            // Rustic bucket/pot
            c.OutlineRect(fx - 6, 38, 12, 9, i % 2 == 0 ? new Rgba(192, 110, 78) : new Rgba(136, 152, 162), Outline);
            // Lush foliage base
            c.Circle(fx, 50, 7, LeafDark);
            c.Circle(fx, 51, 6, LeafMid);
            // Blossoms
            Rgba fl = flowerColors[i];
            c.Circle(fx - 3, 53, 3, fl);
            c.Circle(fx + 3, 52, 3, fl);
            c.Circle(fx, 56, 3.2f, fl);
            c.Set(fx, 56, Cream);
            c.Set(fx - 3, 53, Gold);
        }

        // Hanging flower baskets under the awning!
        int[] basketX = { 34, 118 };
        foreach (int bx in basketX)
        {
            c.Line(bx, 68, bx, 60, WoodDark);
            c.OutlineRect(bx - 6, 56, 12, 5, WoodMid, Outline);
            c.Circle(bx, 62, 5, LeafMid);
            c.Circle(bx - 3, 63, 2, CoralLight);
            c.Circle(bx + 2, 64, 2, Gold);
        }

        // Warm terracotta & cream striped scalloped awning
        c.OutlineRect(8, 68, 136, 18, new Rgba(214, 118, 96), Outline);
        for (int i = 0; i < 11; i++)
        {
            int sx = 9 + i * 12;
            int sw = Math.Min(12, 143 - sx);
            Rgba col = (i % 2 == 0) ? new Rgba(218, 122, 98) : Cream;
            Rgba shade = (i % 2 == 0) ? new Rgba(174, 86, 68) : CreamShade;
            c.Rect(sx, 69, sw, 16, col);
            c.Rect(sx, 69, sw, 3, shade);
            c.Ellipse(sx + sw / 2f, 68, sw / 2f, 3, col);
            c.Rect(sx, 66, sw, 1, Outline);
        }
        c.OutlineRect(12, 85, 128, 5, WoodMid, Outline);

        // Cozy wooden signboard header
        c.OutlineRect(30, 88, 92, 14, new Rgba(74, 98, 72), Outline);
        c.Rect(32, 90, 88, 10, new Rgba(92, 124, 86));
        c.Rect(33, 99, 86, 1, Gold);
        // Flower emblems on left & right
        c.Circle(38, 95, 3, CoralLight); c.Set(38, 95, Gold);
        c.Circle(114, 95, 3, CoralLight); c.Set(114, 95, Gold);

        return c;
    }

    public static Canvas BuildFloristNpc()
    {
        // 32 x 48 pixel-art florist with straw sun hat, floral green apron, holding bouquet
        Canvas c = new Canvas(32, 48);
        Rgba skin = new Rgba(214, 158, 122);
        Rgba hair = new Rgba(92, 56, 44);
        Rgba hat = new Rgba(242, 206, 132);
        Rgba hatShade = new Rgba(206, 164, 94);
        Rgba dress = new Rgba(248, 232, 206);
        Rgba apron = new Rgba(84, 144, 94);
        Rgba shoes = new Rgba(114, 72, 56);

        // Shoes & legs
        c.Rect(10, 2, 4, 4, shoes);
        c.Rect(18, 2, 4, 4, shoes);
        c.Rect(11, 6, 3, 6, skin);
        c.Rect(18, 6, 3, 6, skin);

        // Dress & sage-green florist apron
        c.Rect(9, 11, 14, 16, dress);
        c.Rect(11, 12, 10, 14, apron);
        c.Rect(12, 25, 2, 3, apron);
        c.Rect(18, 25, 2, 3, apron);
        c.Rect(14, 15, 4, 4, new Rgba(112, 168, 114)); // apron pocket
        // Arms
        c.Rect(6, 17, 3, 8, skin);
        c.Rect(23, 17, 3, 8, skin);

        // Holding a small colorful flower bouquet in right hand!
        c.Rect(23, 19, 4, 4, LeafMid);
        c.Set(24, 23, CoralLight);
        c.Set(26, 22, Gold);
        c.Set(25, 24, new Rgba(196, 138, 226));

        // Head & hair
        c.Rect(10, 27, 12, 11, skin);
        c.Rect(8, 29, 3, 9, hair);
        c.Rect(21, 29, 3, 9, hair);
        c.Rect(12, 32, 2, 2, Outline); // eyes
        c.Rect(18, 32, 2, 2, Outline);
        c.Rect(14, 29, 4, 1, new Rgba(196, 102, 102)); // warm smile

        // Wide woven straw sun hat with coral ribbon!
        c.Rect(5, 37, 22, 3, hat);
        c.Rect(6, 37, 20, 1, hatShade);
        c.Rect(9, 40, 14, 5, hat);
        c.Rect(9, 40, 14, 2, Coral); // hat ribbon
        c.Rect(20, 41, 3, 2, Gold);  // flower tucked in hat

        c.OutlineSprite(Outline);
        return c;
    }

    public static Canvas BuildIceCreamCart()
    {
        // 84 x 68 (PPU 32 -> 2.625 x 2.125 world units)
        // Handcrafted vintage beach ice-cream cart with spoked wheels, turquoise/coral body,
        // cream top, giant cone ornament, push handle, and striped beach parasol!
        Canvas c = new Canvas(84, 68);

        // Ground shadow
        c.Ellipse(42, 6, 32, 5, new Rgba(56, 40, 52, 110));

        // Left & right vintage spoked wheels
        DrawSpokedWheel(c, 22, 11, 7);
        DrawSpokedWheel(c, 58, 11, 7);
        // Small front caster wheel
        c.Circle(68, 8, 3, Outline);
        c.Circle(68, 8, 1.5f, CreamShade);

        // Wooden push handle on left side
        c.Line(8, 26, 16, 22, WoodDark, 2);
        c.Line(8, 26, 16, 23, WoodLight, 1);
        c.Rect(5, 25, 4, 3, Coral);

        // Cart chassis & body (pastel turquoise with coral trim & cream marble counter)
        c.OutlineRect(16, 12, 50, 22, Turquoise, Outline);
        // Vertical vintage wood panel grooves
        for (int x = 22; x < 64; x += 6) c.Rect(x, 14, 1, 18, TurquoiseDark);
        // Coral lower stripe + sunlit cream highlight
        c.Rect(17, 15, 48, 6, Coral);
        c.Rect(17, 20, 48, 1, CoralLight);
        // Cute pastel ice-cream decal badge on front center of cart
        c.Circle(41, 23, 6, Cream);
        DrawMiniCone(c, 41, 19);

        // Cream marble freezer lid & chrome handles
        c.OutlineRect(14, 32, 54, 6, Cream, Outline);
        c.Rect(16, 36, 50, 1, new Rgba(255, 252, 240));
        c.Rect(24, 38, 10, 2, new Rgba(172, 188, 198));
        c.Rect(46, 38, 10, 2, new Rgba(172, 188, 198));

        // Giant sculpted ice-cream cone sign on the right corner of cart!
        c.Line(58, 38, 61, 45, GoldWarm, 3);
        c.Line(64, 38, 61, 45, GoldWarm, 3);
        c.Rect(59, 38, 5, 5, GoldWarm);
        c.Circle(61, 46, 4.5f, CoralLight);
        c.Circle(59, 49, 3.2f, Cream);
        c.Set(61, 51, CoralDark); // cherry on top!

        // Umbrella mast rising from center of cart
        c.Rect(40, 38, 3, 18, WoodDark);
        c.Rect(41, 38, 1, 18, WoodLight);

        // Striped pink-and-cream beach parasol umbrella arching over the cart!
        for (int dx = -24; dx <= 24; dx++)
        {
            int arch = (int)Math.Round((1.0 - (dx * dx) / 620.0) * 10.0);
            Rgba col = ((dx + 24) / 6) % 2 == 0 ? Coral : Cream;
            Rgba trim = ((dx + 24) / 6) % 2 == 0 ? CoralDark : CreamShade;
            for (int dy = 0; dy <= arch; dy++)
                c.Set(41 + dx, 54 + dy, dy == 0 ? trim : col);
        }
        // Parasol outline & golden finial
        c.Rect(40, 65, 3, 2, Gold);

        return c;
    }

    private static void DrawSpokedWheel(Canvas c, int cx, int cy, int r)
    {
        c.Circle(cx, cy, r, Outline);
        c.Circle(cx, cy, r - 2, new Rgba(188, 178, 166));
        c.Circle(cx, cy, r - 3, new Rgba(0, 0, 0, 0));
        // Spokes
        c.Line(cx - (r - 2), cy, cx + (r - 2), cy, WoodDark);
        c.Line(cx, cy - (r - 2), cx, cy + (r - 2), WoodDark);
        c.Line(cx - 4, cy - 4, cx + 4, cy + 4, WoodDark);
        c.Line(cx - 4, cy + 4, cx + 4, cy - 4, WoodDark);
        c.Circle(cx, cy, 2, Gold);
    }

    // =========================================================================
    // 4. FEIRINHA DA ORLA STALLS & FESTOON LIGHTS
    // =========================================================================
    public static Canvas BuildMarketStall(bool isCoconutFruitStall)
    {
        // 112 x 76 (PPU 32 -> 3.5 x 2.375 world units)
        Canvas c = new Canvas(112, 76);

        c.Ellipse(56, 6, 50, 5, new Rgba(56, 40, 52, 95));

        // Wooden frame & legs
        c.OutlineRect(12, 8, 6, 42, WoodMid, Outline);
        c.OutlineRect(94, 8, 6, 42, WoodMid, Outline);
        c.Rect(18, 22, 76, 26, new Rgba(88, 66, 72, 210)); // cozy shaded interior

        // Front stall table & cloth
        Rgba cloth = isCoconutFruitStall ? new Rgba(74, 146, 118) : new Rgba(214, 142, 86);
        Rgba clothDark = isCoconutFruitStall ? new Rgba(52, 112, 88) : new Rgba(172, 106, 62);
        c.OutlineRect(14, 8, 84, 18, cloth, Outline);
        for (int x = 22; x < 94; x += 10) c.Rect(x, 9, 2, 16, clothDark);
        c.OutlineRect(12, 24, 88, 5, WoodLight, Outline);

        if (isCoconutFruitStall)
        {
            // Piles of green coconuts, pineapples & tropical fruit crates
            for (int i = 0; i < 6; i++)
            {
                c.Circle(24 + i * 7, 32, 3.5f, LeafMid);
                c.Circle(23 + i * 7, 33, 2, LeafLight);
            }
            for (int i = 0; i < 4; i++)
            {
                c.Circle(72 + i * 6, 32, 3, GoldWarm);
                c.Rect(71 + i * 6, 35, 2, 3, LeafLight);
            }
        }
        else
        {
            // Colorful hanging beach wraps (cangas) & woven straw hats!
            Rgba[] wraps = { Coral, Turquoise, Gold, new Rgba(184, 122, 206) };
            for (int i = 0; i < 4; i++)
            {
                int wx = 24 + i * 18;
                c.OutlineRect(wx, 30, 12, 16, wraps[i], Outline);
                c.Rect(wx + 2, 36, 8, 2, Cream);
            }
        }

        // Striped awning
        Rgba stripeA = isCoconutFruitStall ? new Rgba(68, 152, 124) : new Rgba(222, 118, 88);
        c.OutlineRect(6, 48, 100, 18, stripeA, Outline);
        for (int i = 0; i < 10; i++)
        {
            int sx = 7 + i * 10;
            Rgba col = (i % 2 == 0) ? stripeA : Cream;
            c.Rect(sx, 49, 10, 16, col);
            c.Ellipse(sx + 5, 48, 5, 2.5f, col);
        }
        c.OutlineRect(10, 65, 92, 4, WoodMid, Outline);

        return c;
    }

    public static Canvas BuildFestoonLights()
    {
        // 160 x 36 (PPU 32 -> 5.0 x 1.125 world units)
        // Draped string cable with colorful coastal pennants (bandeirinhas) & glowing bulbs
        Canvas c = new Canvas(160, 36);
        Rgba wire = new Rgba(74, 58, 66);
        Rgba[] flags = { Coral, Gold, Turquoise, Cream, new Rgba(244, 138, 102) };

        for (int x = 0; x < 160; x++)
        {
            float t = (x - 80f) / 80f;
            int y = (int)Math.Round(16 + t * t * 16);
            c.Set(x, y, wire);
            c.Set(x, y + 1, wire);
        }

        for (int i = 0; i < 11; i++)
        {
            int x = 10 + i * 14;
            float t = (x - 80f) / 80f;
            int wy = (int)Math.Round(16 + t * t * 16);

            // Triangle pennant hanging below wire
            Rgba flag = flags[i % flags.Length];
            for (int dy = 0; dy < 7; dy++)
            {
                int half = Math.Max(0, 3 - dy / 2);
                c.Rect(x - half, wy - 1 - dy, half * 2 + 1, 1, flag);
            }
            // Glowing evening fairy bulb between pennants
            if (i < 10)
            {
                int bx = x + 7;
                float bt = (bx - 80f) / 80f;
                int by = (int)Math.Round(16 + bt * bt * 16) - 2;
                c.Circle(bx, by, 4, new Rgba(255, 218, 134, 80));
                c.Rect(bx - 1, by - 1, 2, 2, Gold);
                c.Set(bx, by, Cream);
            }
        }
        return c;
    }

    // =========================================================================
    // 5. PALM TREES, STREET LAMPS, BENCHES, PLANTERS
    // =========================================================================
    public static Canvas BuildPalmTree()
    {
        // 76 x 112 (PPU 32 -> 2.375 x 3.5 world units)
        // Lush curving coastal coconut palm with textured bark rings, coconuts & arching fronds!
        Canvas c = new Canvas(76, 112);

        // Ground shadow
        c.Ellipse(34, 6, 16, 4, new Rgba(56, 40, 52, 95));

        // Curving trunk from (34, 7) up to crown at (42, 82)
        for (int y = 6; y <= 82; y++)
        {
            float t = (y - 6f) / 76f;
            float cx = 33f + (float)Math.Sin(t * Math.PI * 0.65f) * 9f;
            float halfW = 4.6f - t * 1.9f;
            int x0 = (int)Math.Round(cx - halfW);
            int w = Math.Max(3, (int)Math.Round(halfW * 2));
            bool ring = (y % 5) == 0;
            c.Rect(x0, y, w, 1, ring ? WoodDark : WoodMid);
            c.Rect(x0 + w - 2, y, 2, 1, ring ? WoodMid : WoodLight); // sunlit rim
        }

        // Cluster of coconuts nestled at crown
        int crownX = 41, crownY = 82;
        c.Circle(crownX - 3, crownY - 2, 3, new Rgba(114, 72, 48));
        c.Circle(crownX + 3, crownY - 2, 3, new Rgba(128, 82, 54));
        c.Circle(crownX, crownY - 4, 3, new Rgba(102, 62, 42));

        // 7 rich arching coconut palm fronds!
        DrawPalmFrond(c, crownX, crownY, -32, -10, 12, LeafDark, LeafMid);
        DrawPalmFrond(c, crownX, crownY, -28, 12, 14, LeafMid, LeafLight);
        DrawPalmFrond(c, crownX, crownY, -16, 22, 11, LeafMid, LeafLight);
        DrawPalmFrond(c, crownX, crownY, 32, -12, 12, LeafDark, LeafMid);
        DrawPalmFrond(c, crownX, crownY, 29, 10, 14, LeafMid, LeafLight);
        DrawPalmFrond(c, crownX, crownY, 17, 22, 11, LeafMid, LeafLight);
        DrawPalmFrond(c, crownX, crownY, 2, 24, 9, LeafLight, new Rgba(158, 198, 112));

        c.OutlineSprite(Outline);
        return c;
    }

    private static void DrawPalmFrond(Canvas c, int ox, int oy, float dx, float dy, float archLift, Rgba baseCol, Rgba hiCol)
    {
        const int steps = 28;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = ox + dx * t;
            float y = oy + dy * t + (float)Math.Sin(t * Math.PI) * archLift;
            // Rib
            c.Set((int)x, (int)y, hiCol);
            c.Set((int)x, (int)y + 1, hiCol);
            // Drooping leaflets hanging beneath rib
            int droop = (int)(Math.Sin(t * Math.PI) * 7f + 2f);
            if (i % 2 == 0 && i > 2)
            {
                for (int d = 1; d <= droop; d++)
                    c.Set((int)(x + (dx > 0 ? d * 0.15f : -d * 0.15f)), (int)y - d, d <= 2 ? hiCol : baseCol);
            }
        }
    }

    public static Canvas BuildStreetLamp()
    {
        // 32 x 84 (PPU 32 -> 1.0 x 2.625 world units)
        // Classic coastal wrought-iron evening lamp post with glowing warm lantern!
        Canvas c = new Canvas(32, 84);
        Rgba iron = new Rgba(74, 68, 84);
        Rgba ironHi = new Rgba(114, 108, 126);

        c.Ellipse(10, 4, 8, 3, new Rgba(56, 40, 52, 110));
        // Ornate cast-iron base pedestal
        c.OutlineRect(5, 4, 10, 8, iron, Outline);
        c.Rect(7, 12, 6, 6, iron);
        // Slim vertical post
        c.Rect(8, 18, 4, 54, iron);
        c.Rect(10, 18, 1, 54, ironHi);
        // Decorative collar rings
        c.Rect(7, 36, 6, 2, ironHi);
        c.Rect(7, 62, 6, 2, ironHi);
        // Curved shepherd-crook arm extending right
        c.Rect(8, 72, 14, 3, iron);
        c.Rect(20, 68, 3, 6, iron);
        c.Line(12, 64, 18, 72, ironHi); // scroll brace
        // Hanging glass lantern housing
        c.OutlineRect(17, 58, 9, 10, new Rgba(255, 224, 148), Outline);
        c.Rect(19, 60, 5, 6, new Rgba(255, 246, 206));
        c.Rect(16, 67, 11, 2, iron); // lantern cap
        c.Rect(19, 69, 5, 2, iron);

        return c;
    }

    public static Canvas BuildCoastalBench()
    {
        // 52 x 26 (PPU 32 -> 1.625 x 0.81 world units)
        Canvas c = new Canvas(52, 26);
        Rgba iron = new Rgba(72, 64, 78);

        // Ground shadow
        c.Ellipse(26, 4, 24, 3.5f, new Rgba(56, 40, 52, 115));
        // Wrought-iron legs
        c.Rect(8, 4, 3, 10, iron);
        c.Rect(41, 4, 3, 10, iron);
        // Wooden seat slats
        c.OutlineRect(4, 10, 44, 5, WoodLight, Outline);
        c.Rect(6, 13, 40, 1, CreamShade);
        // Wooden backrest slats
        c.OutlineRect(5, 16, 42, 4, WoodMid, Outline);
        c.OutlineRect(5, 20, 42, 4, WoodLight, Outline);
        // Iron armrests
        c.Rect(5, 11, 2, 11, iron);
        c.Rect(45, 11, 2, 11, iron);
        return c;
    }

    public static Canvas BuildPlanterBush()
    {
        // 48 x 32 (PPU 32 -> 1.5 x 1.0 world units)
        Canvas c = new Canvas(48, 32);
        c.Ellipse(24, 5, 21, 4, new Rgba(56, 40, 52, 95));
        // Low stone coastal planter curb
        c.OutlineRect(5, 5, 38, 8, new Rgba(212, 198, 174), Outline);
        c.Rect(7, 11, 34, 1, Cream);
        // Lush layered coastal shrub foliage
        c.Circle(15, 17, 9, LeafDark);
        c.Circle(33, 17, 9, LeafDark);
        c.Circle(24, 20, 10, LeafMid);
        c.Circle(16, 19, 7, LeafMid);
        c.Circle(32, 19, 7, LeafMid);
        c.Circle(23, 23, 6, LeafLight);
        c.Circle(14, 21, 4, LeafLight);
        // Hibiscus & bougainvillea blooms
        c.Circle(13, 20, 2.2f, CoralLight); c.Set(13, 20, Gold);
        c.Circle(25, 24, 2.4f, Coral);      c.Set(25, 24, Gold);
        c.Circle(34, 19, 2.2f, Gold);       c.Set(34, 19, Cream);
        c.Circle(20, 16, 2.0f, CoralLight);
        return c;
    }

    // =========================================================================
    // 6. FEIRA OBSTACLES (CRATES & PEDESTRIANS) + SIGNPOST
    // =========================================================================
    public static Canvas BuildCrateOranges()
    {
        // 36 x 34 (PPU 32 -> 1.125 x 1.06 world units)
        Canvas c = new Canvas(36, 34);
        c.Ellipse(18, 4, 15, 3.5f, new Rgba(56, 40, 52, 115));

        // Slatted wooden market crate
        c.OutlineRect(5, 5, 26, 17, WoodMid, Outline);
        c.Rect(6, 10, 24, 1, WoodDark);
        c.Rect(6, 16, 24, 1, WoodDark);
        c.Rect(6, 6, 3, 15, WoodLight);
        c.Rect(27, 6, 3, 15, WoodLight);

        // Mound of glossy oranges + green leaves piled on top!
        Rgba orange = new Rgba(244, 142, 48);
        Rgba orangeHi = new Rgba(255, 196, 92);
        Rgba orangeShade = new Rgba(198, 96, 32);
        int[,] pos = { { 11, 23 }, { 17, 23 }, { 23, 23 }, { 14, 27 }, { 20, 27 }, { 17, 30 } };
        for (int i = 0; i < pos.GetLength(0); i++)
        {
            int ox = pos[i, 0], oy = pos[i, 1];
            c.Circle(ox, oy, 3.4f, orangeShade);
            c.Circle(ox, oy + 0.5f, 2.8f, orange);
            c.Set(ox - 1, oy + 2, orangeHi);
        }
        c.Rect(10, 26, 3, 2, LeafMid);
        c.Rect(23, 26, 3, 2, LeafLight);
        c.OutlineSprite(Outline);
        return c;
    }

    public static Canvas BuildCrateFlowers()
    {
        // 38 x 36 (PPU 32 -> 1.18 x 1.12 world units)
        Canvas c = new Canvas(38, 36);
        c.Ellipse(19, 4, 16, 3.5f, new Rgba(56, 40, 52, 115));

        // Rustic wooden flower crate
        c.OutlineRect(5, 5, 28, 16, WoodLight, Outline);
        c.Rect(6, 10, 26, 1, WoodDark);
        c.Rect(6, 15, 26, 1, WoodDark);
        c.Rect(6, 6, 3, 14, WoodMid);
        c.Rect(29, 6, 3, 14, WoodMid);

        // Lush mound of blooms
        c.Circle(13, 23, 6, LeafDark);
        c.Circle(25, 23, 6, LeafDark);
        c.Circle(19, 25, 7, LeafMid);
        c.Circle(12, 25, 3, CoralLight); c.Set(12, 25, Gold);
        c.Circle(19, 29, 3.5f, Gold);    c.Set(19, 29, Cream);
        c.Circle(26, 25, 3, new Rgba(188, 128, 224)); c.Set(26, 25, Cream);
        c.Circle(16, 23, 2.5f, Coral);
        c.Circle(22, 23, 2.5f, Cream);
        c.OutlineSprite(Outline);
        return c;
    }

    public static Canvas BuildPedestrian(bool blueVariant)
    {
        // 32 x 48 charming beach-fair shopper
        Canvas c = new Canvas(32, 48);
        Rgba skin = blueVariant ? new Rgba(204, 146, 112) : new Rgba(168, 108, 78);
        Rgba hair = blueVariant ? new Rgba(66, 52, 62) : new Rgba(74, 44, 42);
        Rgba top = blueVariant ? new Rgba(72, 134, 178) : new Rgba(224, 104, 104);
        Rgba topHi = blueVariant ? new Rgba(118, 174, 212) : new Rgba(246, 152, 146);
        Rgba bottom = blueVariant ? new Rgba(224, 204, 168) : new Rgba(72, 92, 124);
        Rgba bag = blueVariant ? GoldWarm : Cream;

        // Shoes & legs
        c.Rect(10, 2, 4, 4, new Rgba(88, 66, 60));
        c.Rect(18, 2, 4, 4, new Rgba(88, 66, 60));
        c.Rect(10, 6, 4, 9, bottom);
        c.Rect(18, 6, 4, 9, bottom);

        // Torso & arms
        c.Rect(9, 14, 14, 13, top);
        c.Rect(11, 21, 10, 5, topHi);
        c.Rect(6, 16, 3, 9, skin);
        c.Rect(23, 16, 3, 9, skin);

        // Woven beach tote bag
        c.Rect(4, 12, 5, 7, bag);
        c.Rect(5, 18, 3, 2, WoodDark);

        // Head & hair
        c.Rect(10, 27, 12, 11, skin);
        c.Rect(12, 32, 2, 2, Outline);
        c.Rect(18, 32, 2, 2, Outline);
        if (blueVariant)
        {
            // Cap / visor
            c.Rect(9, 36, 14, 5, new Rgba(56, 102, 144));
            c.Rect(11, 35, 13, 2, Cream);
        }
        else
        {
            // Sun hat & flowing hair
            c.Rect(8, 29, 3, 9, hair);
            c.Rect(21, 29, 3, 9, hair);
            c.Rect(6, 37, 20, 3, new Rgba(242, 204, 134));
            c.Rect(9, 40, 14, 4, new Rgba(242, 204, 134));
            c.Rect(9, 40, 14, 1, Turquoise);
        }

        c.OutlineSprite(Outline);
        return c;
    }

    public static Canvas BuildRouteSignpost()
    {
        // 120 x 24 wooden coastal route plaque
        Canvas c = new Canvas(120, 24);
        c.OutlineRect(2, 2, 116, 20, new Rgba(74, 58, 72, 235), Outline);
        c.Rect(4, 4, 112, 16, new Rgba(92, 74, 88, 235));
        c.Rect(5, 19, 110, 1, GoldWarm);
        c.Rect(5, 4, 110, 1, WoodDark);
        return c;
    }

    // =========================================================================
    // 7. AMBIENT SKY, SEA & ATMOSPHERE SPRITES
    // =========================================================================
    public static Canvas BuildSeagull(bool wingsDown)
    {
        // 24 x 16 two-frame pixel seagull
        Canvas c = new Canvas(24, 16);
        Rgba body = new Rgba(255, 244, 228);
        Rgba wingTip = new Rgba(134, 120, 136);
        c.Rect(9, 7, 6, 3, body);
        c.Set(15, 8, GoldWarm); // beak
        if (!wingsDown)
        {
            c.Line(3, 12, 9, 9, body, 2);
            c.Line(14, 9, 20, 13, body, 2);
            c.Set(2, 13, wingTip);
            c.Set(21, 13, wingTip);
        }
        else
        {
            c.Line(4, 5, 9, 8, body, 2);
            c.Line(14, 8, 19, 5, body, 2);
            c.Set(3, 4, wingTip);
            c.Set(20, 4, wingTip);
        }
        return c;
    }

    public static Canvas BuildCloudEvening()
    {
        // 96 x 32 warm sunset drifting cloud
        Canvas c = new Canvas(96, 32);
        Rgba top = new Rgba(248, 184, 168, 190);
        Rgba mid = new Rgba(255, 212, 178, 215);
        Rgba bot = new Rgba(255, 232, 184, 225);
        c.Ellipse(48, 14, 30, 10, mid);
        c.Ellipse(26, 12, 18, 8, mid);
        c.Ellipse(70, 12, 20, 8, mid);
        c.Ellipse(42, 19, 18, 8, top);
        c.Ellipse(60, 18, 14, 7, top);
        c.Rect(16, 7, 64, 3, bot);
        return c;
    }

    public static Canvas BuildWaterShimmer()
    {
        // 64 x 8 sparkling ocean wave highlight strip
        Canvas c = new Canvas(64, 8);
        c.Rect(4, 4, 22, 2, new Rgba(255, 236, 188, 180));
        c.Rect(10, 5, 10, 1, new Rgba(255, 250, 226, 220));
        c.Rect(34, 2, 24, 2, new Rgba(188, 236, 232, 175));
        c.Rect(40, 3, 12, 1, new Rgba(248, 252, 244, 210));
        return c;
    }

    public static Canvas BuildWarmGlow()
    {
        // 64 x 64 soft radial golden-hour atmospheric halo
        Canvas c = new Canvas(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d >= 1f) continue;
                float falloff = (1f - d) * (1f - d);
                int alpha = (int)(falloff * 185f);
                c.Set(x, y, new Rgba(255, 216, 142, alpha));
            }
        }
        return c;
    }

    public static Canvas BuildShadowOval()
    {
        // 32 x 12 soft contact shadow oval
        Canvas c = new Canvas(32, 12);
        for (int y = 0; y < 12; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15f;
                float dy = (y - 5.5f) / 5.2f;
                float d = dx * dx + dy * dy;
                if (d >= 1f) continue;
                float falloff = (1f - d);
                c.Set(x, y, new Rgba(44, 30, 44, (int)(falloff * 135f)));
            }
        }
        return c;
    }
}
