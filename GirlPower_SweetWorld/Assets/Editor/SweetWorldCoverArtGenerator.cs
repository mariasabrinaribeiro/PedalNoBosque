using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// Gerador de ilustrações 2D cartoon anti-aliased com contornos roxo-escuro (#3B1854),
/// brilho glossy e paleta idêntica à capa "Girl Power - Sweet Treats Delivery":
/// - bg_sky_galaxy.png (1536x768, seamless): galáxia estrelada, lua crescente, montanhas listradas e castelos doces
/// - bg_candy_midground.png (1536x512, seamless, transparente): colinas doces, árvores algodão-doce rosa/azul, cupcakes e pirulitos
/// - hero_girl_power_cart.png (512x420): protagonista de cabelo rosa/óculos aviador dirigindo o carrinho Sweet Treats!
/// - gingerbread_house.png (384x384): casinha de gengibre com telhado de glacê e janela iluminada
/// - cupcake_collectible.png (256x256): cupcake gigante com cereja e confeitos
/// - lollipop_prop.png (256x320): pirulito espiral arco-íris brilhante
/// - jelly_green.png / jelly_pink.png / jelly_purple.png (256x256): os 3 monstrinhos de gelatina da capa!
/// </summary>
public static class SweetWorldCoverArtGenerator
{
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

    public sealed class ArtCanvas
    {
        public readonly int W, H;
        public readonly Rgba[] Px;
        public ArtCanvas(int w, int h) { W = w; H = h; Px = new Rgba[w * h]; }

        public void Blend(int x, int y, Rgba c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H || c.a == 0) return;
            int i = y * W + x;
            if (c.a == 255) { Px[i] = c; return; }
            Rgba d = Px[i];
            float sa = c.a / 255f, da = d.a / 255f, oa = sa + da * (1f - sa);
            if (oa <= 0.0001f) return;
            Px[i] = new Rgba(
                (int)((c.r * sa + d.r * da * (1f - sa)) / oa),
                (int)((c.g * sa + d.g * da * (1f - sa)) / oa),
                (int)((c.b * sa + d.b * da * (1f - sa)) / oa),
                (int)(oa * 255f));
        }

        public void FillCircleAA(float cx, float cy, float r, Rgba c)
        {
            int x0 = Clamp((int)Math.Floor(cx - r - 2), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + r + 2), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - r - 2), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + r + 2), 0, H - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = (float)Math.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float cov = Clamp01(r + 0.75f - d);
                    if (cov > 0f) Blend(x, y, new Rgba(c.r, c.g, c.b, (int)(c.a * cov)));
                }
        }

        public void FillEllipseAA(float cx, float cy, float rx, float ry, Rgba c)
        {
            int x0 = Clamp((int)Math.Floor(cx - rx - 2), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + rx + 2), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - ry - 2), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + ry + 2), 0, H - 1);
            float scale = Math.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float cov = Clamp01((1f - d) * scale + 0.75f);
                    if (cov > 0f) Blend(x, y, new Rgba(c.r, c.g, c.b, (int)(c.a * cov)));
                }
        }

        public void FillRoundRectAA(float x, float y, float w, float h, float r, Rgba c)
        {
            float cx0 = x + r, cx1 = x + w - r, cy0 = y + r, cy1 = y + h - r;
            int px0 = Clamp((int)Math.Floor(x - 2), 0, W - 1), px1 = Clamp((int)Math.Ceiling(x + w + 2), 0, W - 1);
            int py0 = Clamp((int)Math.Floor(y - 2), 0, H - 1), py1 = Clamp((int)Math.Ceiling(y + h + 2), 0, H - 1);
            for (int py = py0; py <= py1; py++)
                for (int px = px0; px <= px1; px++)
                {
                    float qx = ClampF(px + 0.5f, cx0, cx1), qy = ClampF(py + 0.5f, cy0, cy1);
                    float d = (float)Math.Sqrt((px + 0.5f - qx) * (px + 0.5f - qx) + (py + 0.5f - qy) * (py + 0.5f - qy));
                    float cov = Clamp01(r + 0.75f - d);
                    if (cov > 0f) Blend(px, py, new Rgba(c.r, c.g, c.b, (int)(c.a * cov)));
                }
        }

        public void CapsuleAA(float x0, float y0, float x1, float y1, float r, Rgba c)
        {
            int bx0 = Clamp((int)Math.Floor(Math.Min(x0, x1) - r - 2), 0, W - 1);
            int bx1 = Clamp((int)Math.Ceiling(Math.Max(x0, x1) + r + 2), 0, W - 1);
            int by0 = Clamp((int)Math.Floor(Math.Min(y0, y1) - r - 2), 0, H - 1);
            int by1 = Clamp((int)Math.Ceiling(Math.Max(y0, y1) + r + 2), 0, H - 1);
            float vx = x1 - x0, vy = y1 - y0, len2 = Math.Max(0.0001f, vx * vx + vy * vy);
            for (int py = by0; py <= by1; py++)
                for (int px = bx0; px <= bx1; px++)
                {
                    float t = Clamp01(((px + 0.5f - x0) * vx + (py + 0.5f - y0) * vy) / len2);
                    float qx = x0 + vx * t, qy = y0 + vy * t;
                    float d = (float)Math.Sqrt((px + 0.5f - qx) * (px + 0.5f - qx) + (py + 0.5f - qy) * (py + 0.5f - qy));
                    float cov = Clamp01(r + 0.75f - d);
                    if (cov > 0f) Blend(px, py, new Rgba(c.r, c.g, c.b, (int)(c.a * cov)));
                }
        }

        public void SoftGlow(float cx, float cy, float r, Rgba c)
        {
            int x0 = Clamp((int)(cx - r), 0, W - 1), x1 = Clamp((int)(cx + r), 0, W - 1);
            int y0 = Clamp((int)(cy - r), 0, H - 1), y1 = Clamp((int)(cy + r), 0, H - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d < 1f) Blend(x, y, new Rgba(c.r, c.g, c.b, (int)(c.a * (1f - d) * (1f - d))));
                }
        }

        public void Sparkle4(float cx, float cy, float r, Rgba c)
        {
            int x0 = Clamp((int)(cx - r), 0, W - 1), x1 = Clamp((int)(cx + r), 0, W - 1);
            int y0 = Clamp((int)(cy - r), 0, H - 1), y1 = Clamp((int)(cy + r), 0, H - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Math.Abs(x - cx) / r, dy = Math.Abs(y - cy) / r;
                    float s = (float)(Math.Pow(dx, 0.45) + Math.Pow(dy, 0.45));
                    if (s < 1f) Blend(x, y, new Rgba(c.r, c.g, c.b, (int)(c.a * (1f - s))));
                }
        }
    }

    // Exact Cover Palette (sampled directly from Girl Power cover art!)
    private static readonly Rgba Ink = new Rgba(56, 20, 84);
    private static readonly Rgba White = new Rgba(255, 255, 255);
    private static readonly Rgba Cream = new Rgba(255, 248, 224);
    private static readonly Rgba MoonGold = new Rgba(255, 242, 166);
    private static readonly Rgba PinkBright = new Rgba(255, 96, 186);
    private static readonly Rgba PinkSoft = new Rgba(255, 172, 222);
    private static readonly Rgba PinkDeep = new Rgba(214, 48, 142);
    private static readonly Rgba CyanBright = new Rgba(92, 218, 255);
    private static readonly Rgba CyanLight = new Rgba(168, 238, 255);
    private static readonly Rgba PurpleMid = new Rgba(144, 72, 224);
    private static readonly Rgba PurpleLight = new Rgba(206, 154, 255);
    private static readonly Rgba MintHill = new Rgba(96, 218, 162);
    private static readonly Rgba MintDark = new Rgba(52, 162, 126);
    private static readonly Rgba Ginger = new Rgba(216, 132, 66);
    private static readonly Rgba GingerShade = new Rgba(172, 92, 42);
    private static readonly Rgba PeppermintRed = new Rgba(246, 76, 126);

    private static int Clamp(int v, int a, int b) => v < a ? a : (v > b ? b : v);
    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    private static float ClampF(float v, float a, float b) => v < a ? a : (v > b ? b : v);
    private static Rgba Lerp(Rgba a, Rgba b, float t)
    {
        t = Clamp01(t);
        return new Rgba((int)(a.r + (b.r - a.r) * t), (int)(a.g + (b.g - a.g) * t), (int)(a.b + (b.b - a.b) * t), (int)(a.a + (b.a - a.a) * t));
    }

    public static void GenerateAll(Action<string, ArtCanvas> writer)
    {
        writer("bg_sky_galaxy.png", BuildGalaxySkyBackdrop());
        writer("bg_candy_midground.png", BuildCandyMidgroundHills());
        writer("hero_girl_power_cart.png", BuildHeroGirlPowerCart());
        writer("gingerbread_house.png", BuildGingerbreadHouse());
        writer("cupcake_collectible.png", BuildGiantCupcake());
        writer("lollipop_prop.png", BuildSwirlLollipop());
        writer("jelly_green.png", BuildJellyMonster(0));
        writer("jelly_pink.png", BuildJellyMonster(1));
        writer("jelly_purple.png", BuildJellyMonster(2));
    }

    // =========================================================================
    // 1. SEAMLESS GALAXY SKY + CRESCENT MOON + PEPPERMINT MOUNTAINS & CASTLES
    // =========================================================================
    public static ArtCanvas BuildGalaxySkyBackdrop()
    {
        const int W = 1536, H = 768;
        ArtCanvas c = new ArtCanvas(W, H);

        // Rich purple-indigo-magenta twilight galaxy sky gradient
        Rgba topCol = new Rgba(34, 14, 84);
        Rgba midCol = new Rgba(94, 42, 166);
        Rgba botCol = new Rgba(198, 128, 240);
        for (int y = 0; y < H; y++)
        {
            float ny = y / (float)(H - 1);
            Rgba rowCol = ny > 0.52f ? Lerp(midCol, topCol, (ny - 0.52f) / 0.48f) : Lerp(botCol, midCol, ny / 0.52f);
            for (int x = 0; x < W; x++) c.Px[y * W + x] = rowCol;
        }

        // Swirling Milky Way nebula bands (seamless modulo X!)
        for (int i = 0; i < 18; i++)
        {
            float nx = (i * 110f) % W;
            float ny = 520f + (float)Math.Sin(i * 0.7f) * 120f;
            c.SoftGlow(nx, ny, 165f, new Rgba(214, 118, 255, 42));
            c.SoftGlow(nx + 40f, ny + 25f, 110f, new Rgba(120, 210, 255, 34));
        }

        // Twinkling starry sky + 4-point sparkles
        for (int i = 0; i < 180; i++)
        {
            int sx = (i * 293 + 37) % W;
            int sy = 260 + (i * 179 + 53) % (H - 280);
            float r = (i % 7 == 0) ? 3.2f : ((i % 3 == 0) ? 2.1f : 1.3f);
            Rgba scol = (i % 5 == 0) ? MoonGold : ((i % 4 == 0) ? PinkSoft : White);
            c.FillCircleAA(sx, sy, r, scol);
            if (i % 11 == 0) c.Sparkle4(sx, sy, 14f, new Rgba(255, 248, 224, 210));
        }

        // Iconic Glowing Golden Crescent Moon (upper right, just like cover!)
        float mx = 1220f, my = 620f;
        c.SoftGlow(mx, my, 150f, new Rgba(255, 240, 170, 90));
        for (int y = (int)(my - 80); y <= (int)(my + 80); y++)
            for (int x = (int)(mx - 80); x <= (int)(mx + 80); x++)
            {
                float dOuter = (float)Math.Sqrt((x - mx) * (x - mx) + (y - my) * (y - my));
                float dInner = (float)Math.Sqrt((x - (mx - 28f)) * (x - (mx - 28f)) + (y - (my + 16f)) * (y - (my + 16f)));
                if (dOuter <= 66f && dInner >= 54f)
                {
                    float t = Clamp01((dOuter - 20f) / 46f);
                    c.Blend(x, y, Lerp(White, MoonGold, t));
                }
            }

        // Soft glowing lavender horizon clouds (seamless!)
        for (int i = 0; i < 16; i++)
        {
            float cx = (i * 96f + 30f) % W;
            float cy = 270f + (float)Math.Sin(i * 1.3f) * 35f;
            float cr = 75f + (i % 3) * 24f;
            c.FillCircleAA(cx, cy, cr, new Rgba(184, 126, 232, 130));
            if (cx + cr > W) c.FillCircleAA(cx - W, cy, cr, new Rgba(184, 126, 232, 130));
            if (cx - cr < 0) c.FillCircleAA(cx + W, cy, cr, new Rgba(184, 126, 232, 130));
        }

        // Giant Peppermint-Striped Candy Mountains with Snow Icing Caps (just like right side of cover!)
        void DrawPeppermintMountain(float peakX, float peakY, float halfW, float baseY)
        {
            int y0 = Clamp((int)baseY, 0, H - 1), y1 = Clamp((int)peakY + 4, 0, H - 1);
            for (int y = y0; y <= y1; y++)
            {
                float t = (y - baseY) / Math.Max(1f, peakY - baseY);
                float hw = halfW * (1f - t * 0.92f);
                int x0 = (int)(peakX - hw - 4), x1 = (int)(peakX + hw + 4);
                for (int x = x0; x <= x1; x++)
                {
                    int wx = ((x % W) + W) % W;
                    float dx = Math.Abs(x - peakX);
                    if (dx > hw + 3.5f) continue;
                    if (dx > hw - 1.5f) { c.Blend(wx, y, Ink); continue; }

                    // Snow icing cap on top 32% with wavy drips!
                    float dripLine = 0.68f + 0.05f * (float)Math.Sin((x - peakX) * 0.14f);
                    if (t >= dripLine)
                    {
                        c.Blend(wx, y, (t < dripLine + 0.025f) ? Ink : Lerp(new Rgba(235, 224, 255), White, (t - dripLine) / 0.32f));
                    }
                    else
                    {
                        bool stripe = (((int)((x - peakX) + y * 0.55f) + 2000) / 36) % 2 == 0;
                        c.Blend(wx, y, stripe ? PeppermintRed : Cream);
                    }
                }
            }
        }

        DrawPeppermintMountain(340f, 440f, 220f, 110f);
        DrawPeppermintMountain(1310f, 495f, 255f, 110f);

        // Distant Pastel Candy Castle Towers (just like cover!)
        void DrawCandyTower(float cx, float baseY, float w, float h, Rgba wall, Rgba roof)
        {
            c.FillRoundRectAA(cx - w * 0.5f - 4, baseY, w + 8, h + 4, 8, Ink);
            c.FillRoundRectAA(cx - w * 0.5f, baseY, w, h, 6, wall);
            // Conical roof
            for (int y = (int)(baseY + h); y < (int)(baseY + h + w * 0.95f); y++)
            {
                float t = (y - (baseY + h)) / (w * 0.95f);
                float hw = w * 0.66f * (1f - t);
                for (int x = (int)(cx - hw - 4); x <= (int)(cx + hw + 4); x++)
                    c.Blend(((x % W) + W) % W, y, Math.Abs(x - cx) > hw ? Ink : roof);
            }
            // Glowing yellow arched window
            c.FillRoundRectAA(cx - 12, baseY + h * 0.52f, 24, 34, 10, Ink);
            c.FillRoundRectAA(cx - 8, baseY + h * 0.52f + 3, 16, 28, 7, MoonGold);
        }

        DrawCandyTower(860f, 150f, 76f, 185f, CyanLight, CyanBright);
        DrawCandyTower(940f, 150f, 64f, 225f, PinkSoft, PinkBright);
        DrawCandyTower(580f, 145f, 68f, 165f, PurpleLight, PurpleMid);

        // Giant Background Candy Canes
        void DrawArchCane(float cx, float cy)
        {
            c.CapsuleAA(cx + 26, cy - 110, cx + 26, cy + 12, 17, Ink);
            c.CapsuleAA(cx + 26, cy - 110, cx + 26, cy + 12, 12, Cream);
            for (int y = (int)(cy - 105); y < (int)(cy + 8); y += 22)
                c.CapsuleAA(cx + 16, y, cx + 36, y + 10, 5, PeppermintRed);
            for (int a = 0; a <= 180; a += 6)
            {
                float rad = a * (float)Math.PI / 180f;
                float px = cx + (float)Math.Cos(rad) * 26f, py = cy + 12f + (float)Math.Sin(rad) * 26f;
                c.FillCircleAA(px, py, 17, Ink);
            }
            for (int a = 0; a <= 180; a += 6)
            {
                float rad = a * (float)Math.PI / 180f;
                float px = cx + (float)Math.Cos(rad) * 26f, py = cy + 12f + (float)Math.Sin(rad) * 26f;
                c.FillCircleAA(px, py, 12, ((a / 24) % 2 == 0) ? PeppermintRed : Cream);
            }
        }
        DrawArchCane(1110f, 360f);
        DrawArchCane(140f, 320f);

        // Seamless lavender mist foothills at bottom
        for (int x = 0; x < W; x++)
        {
            float th = (x / (float)W) * (float)(Math.PI * 2.0);
            int hillTop = 185 + (int)(Math.Sin(th * 2.0) * 32 + Math.Cos(th * 3.0 + 0.8) * 18);
            for (int y = 0; y < hillTop; y++)
                c.Blend(x, y, y > hillTop - 5 ? Ink : Lerp(new Rgba(142, 86, 206), new Rgba(214, 152, 248), y / (float)hillTop));
        }

        return c;
    }

    // =========================================================================
    // 2. SEAMLESS MIDGROUND CANDY HILLS + COTTON-CANDY TREES + CUPCAKES + LOLLIPOPS
    // =========================================================================
    public static ArtCanvas BuildCandyMidgroundHills()
    {
        const int W = 1536, H = 512;
        ArtCanvas c = new ArtCanvas(W, H);

        // Seamless rolling pastel-mint candy hills
        for (int x = 0; x < W; x++)
        {
            float th = (x / (float)W) * (float)(Math.PI * 2.0);
            int yTop = 165 + (int)(Math.Sin(th * 2.0 + 0.5) * 44 + Math.Sin(th * 4.0) * 18);
            for (int y = 0; y <= yTop; y++)
            {
                Rgba col = (y >= yTop - 5) ? Ink : Lerp(MintDark, MintHill, y / (float)yTop);
                c.Px[y * W + x] = col;
            }
        }

        // Giant Fluffy Pink & Sky-Blue Cotton-Candy Trees (signature look from left side of cover!)
        void DrawCottonTree(float cx, float baseY, float r, Rgba mainCol, Rgba hiCol, Rgba shadeCol)
        {
            // Striped candy-stick trunk
            c.FillRoundRectAA(cx - 14, baseY - 10, 28, 130, 8, Ink);
            c.FillRoundRectAA(cx - 10, baseY - 6, 20, 122, 6, Cream);
            for (int sy = (int)baseY; sy < (int)(baseY + 112); sy += 22)
                c.CapsuleAA(cx - 9, sy, cx + 9, sy + 10, 4.5f, PinkBright);

            // Fluffy cloud canopy with dark-purple outline
            float[,] puffs = { { 0, 140, 1.0f }, { -0.58f, 118, 0.72f }, { 0.58f, 118, 0.72f }, { -0.35f, 168, 0.65f }, { 0.35f, 168, 0.65f }, { 0, 185, 0.58f } };
            for (int i = 0; i < puffs.GetLength(0); i++)
                c.FillCircleAA(cx + puffs[i, 0] * r, baseY + puffs[i, 1] * (r / 88f), r * puffs[i, 2] + 5f, Ink);
            for (int i = 0; i < puffs.GetLength(0); i++)
                c.FillCircleAA(cx + puffs[i, 0] * r, baseY + puffs[i, 1] * (r / 88f), r * puffs[i, 2], shadeCol);
            for (int i = 0; i < puffs.GetLength(0); i++)
                c.FillCircleAA(cx + puffs[i, 0] * r - 4, baseY + puffs[i, 1] * (r / 88f) + 6, r * puffs[i, 2] * 0.86f, mainCol);
            c.FillCircleAA(cx - r * 0.22f, baseY + 162 * (r / 88f), r * 0.36f, hiCol);
            c.FillCircleAA(cx + r * 0.22f, baseY + 150 * (r / 88f), r * 0.22f, White);
        }

        DrawCottonTree(210f, 120f, 92f, PinkSoft, new Rgba(255, 218, 240), new Rgba(238, 124, 192));
        DrawCottonTree(425f, 110f, 74f, CyanLight, new Rgba(222, 250, 255), CyanBright);
        DrawCottonTree(1040f, 118f, 88f, PinkSoft, new Rgba(255, 218, 240), new Rgba(238, 124, 192));
        DrawCottonTree(1240f, 112f, 70f, CyanLight, new Rgba(222, 250, 255), CyanBright);

        // Embedded Mini Cupcakes, Swirl Lollipops & Marshmallows along the hills!
        ArtCanvas cup = BuildGiantCupcake();
        ArtCanvas lol = BuildSwirlLollipop();
        BlitScaled(c, cup, 30, 80, 170, 170);
        BlitScaled(c, cup, 640, 105, 165, 165);
        BlitScaled(c, cup, 1330, 85, 170, 170);
        BlitScaled(c, lol, 535, 95, 105, 130);
        BlitScaled(c, lol, 855, 105, 110, 138);
        BlitScaled(c, lol, 1150, 95, 98, 124);

        // Marshmallow boulders along bottom edge of hills
        float[] mx = { 330f, 365f, 795f, 830f, 1455f, 1490f };
        for (int i = 0; i < mx.Length; i++)
        {
            float cy = 88f + (i % 2) * 16f;
            c.FillRoundRectAA(mx[i] - 24, cy - 22, 48, 44, 14, Ink);
            c.FillRoundRectAA(mx[i] - 20, cy - 18, 40, 36, 11, Cream);
            c.FillEllipseAA(mx[i], cy + 12, 16, 6, White);
        }

        return c;
    }

    // =========================================================================
    // 3. HEROINE GIRL POWER & HER SMILING "SWEET TREATS" ICE CREAM CART (FACING RIGHT!)
    // =========================================================================
    public static ArtCanvas BuildHeroGirlPowerCart()
    {
        const int W = 512, H = 420;
        ArtCanvas c = new ArtCanvas(W, H);

        // --- A. HEROINE (Seated in open cockpit, facing RIGHT, pink hair & scarf flying left!) ---
        // Flowing lavender scarf streaming behind her!
        c.CapsuleAA(138, 226, 54, 242, 17, Ink);
        c.CapsuleAA(138, 226, 54, 242, 12, PurpleLight);
        c.CapsuleAA(126, 224, 62, 238, 5, PinkBright);

        // Flowing bright pink ponytail streaming back!
        c.FillEllipseAA(128, 294, 56, 32, Ink);
        c.FillEllipseAA(92, 304, 42, 22, Ink);
        c.FillEllipseAA(128, 294, 50, 26, PinkBright);
        c.FillEllipseAA(94, 304, 36, 17, PinkBright);
        c.FillEllipseAA(118, 302, 34, 12, PinkSoft);

        // Pink GP jacket torso & arm holding steering wheel!
        c.FillRoundRectAA(154, 174, 92, 92, 26, Ink);
        c.FillRoundRectAA(160, 180, 80, 80, 22, PinkBright);
        c.FillRoundRectAA(172, 188, 52, 62, 16, PinkSoft);
        // Steering column & wheel
        c.CapsuleAA(274, 175, 260, 232, 8, Ink);
        c.CapsuleAA(274, 175, 260, 232, 4, PurpleMid);
        c.FillEllipseAA(258, 235, 16, 26, Ink);
        c.FillEllipseAA(258, 235, 10, 18, new Rgba(92, 72, 120));
        // Outstretched arm + hand on wheel
        c.CapsuleAA(206, 222, 256, 234, 16, Ink);
        c.CapsuleAA(206, 222, 256, 234, 11, PinkBright);
        c.FillCircleAA(258, 234, 13, Ink);
        c.FillCircleAA(258, 234, 9, new Rgba(255, 212, 186));

        // Cheerful Head + Hair + Aviator Goggles!
        float hx = 204f, hy = 296f;
        c.FillCircleAA(hx, hy, 54, Ink);
        c.FillCircleAA(hx, hy, 48, new Rgba(255, 214, 188)); // Warm peach skin
        // Rosy cheek & big joyful open smile!
        c.FillEllipseAA(hx + 22, hy - 8, 10, 6, new Rgba(255, 134, 162, 180));
        c.FillEllipseAA(hx + 22, hy - 19, 16, 12, Ink);
        c.FillEllipseAA(hx + 22, hy - 21, 12, 8, new Rgba(238, 72, 112));
        c.FillRoundRectAA(hx + 12, hy - 14, 19, 6, 3, White); // Bright smile teeth!
        // Sparkling happy eyes (facing right!)
        c.FillEllipseAA(hx + 14, hy + 4, 10, 12, Ink);
        c.FillEllipseAA(hx + 14, hy + 4, 7, 9, White);
        c.FillCircleAA(hx + 16, hy + 3, 5, new Rgba(52, 176, 138)); // Emerald eyes like cover!
        c.FillCircleAA(hx + 17, hy + 4, 3, Ink);
        c.FillCircleAA(hx + 14, hy + 7, 2.4f, White);
        c.FillEllipseAA(hx + 34, hy + 4, 8, 10, Ink);
        c.FillEllipseAA(hx + 34, hy + 4, 5.5f, 7.5f, White);
        c.FillCircleAA(hx + 35, hy + 3, 4, new Rgba(52, 176, 138));
        c.FillCircleAA(hx + 36, hy + 4, 2.4f, Ink);

        // Pink bangs & top hair dome
        c.FillEllipseAA(hx - 6, hy + 32, 52, 28, Ink);
        c.FillEllipseAA(hx - 6, hy + 32, 46, 23, PinkBright);
        c.FillEllipseAA(hx - 14, hy + 38, 28, 12, PinkSoft);
        c.FillCircleAA(hx - 34, hy + 10, 22, Ink);
        c.FillCircleAA(hx - 34, hy + 10, 17, PinkBright);

        // Iconic Golden-Brown Aviator Goggles on Forehead!
        c.CapsuleAA(hx - 44, hy + 28, hx + 44, hy + 32, 9, Ink);
        c.CapsuleAA(hx - 44, hy + 28, hx + 44, hy + 32, 5, new Rgba(158, 92, 48));
        c.FillCircleAA(hx + 6, hy + 34, 18, Ink);
        c.FillCircleAA(hx + 6, hy + 34, 14, new Rgba(218, 152, 68));
        c.FillCircleAA(hx + 6, hy + 34, 10, CyanBright);
        c.FillCircleAA(hx + 3, hy + 37, 4, White);
        c.FillCircleAA(hx + 34, hy + 33, 15, Ink);
        c.FillCircleAA(hx + 34, hy + 33, 11.5f, new Rgba(218, 152, 68));
        c.FillCircleAA(hx + 34, hy + 33, 8, CyanBright);
        c.FillCircleAA(hx + 31, hy + 36, 3, White);

        // --- B. SWEET TREATS DELIVERY CART (Pink, Cyan & Purple glossy body!) ---
        // Rear push handle bar
        c.CapsuleAA(86, 198, 42, 214, 9, Ink);
        c.CapsuleAA(86, 198, 42, 214, 5, PurpleLight);
        // Rear backrest & main cart chassis
        c.FillRoundRectAA(84, 86, 344, 128, 24, Ink);
        c.FillRoundRectAA(90, 92, 332, 116, 20, CyanBright);
        // Lower purple trim strip
        c.FillRoundRectAA(90, 92, 332, 38, 14, PurpleMid);
        // Front hood + smiling bumper!
        c.FillRoundRectAA(258, 126, 166, 86, 18, Ink);
        c.FillRoundRectAA(264, 132, 154, 74, 14, PinkSoft);
        c.FillRoundRectAA(264, 156, 154, 48, 12, PinkBright);
        // Glossy pink frosting roof canopy on front hood!
        c.FillRoundRectAA(246, 198, 188, 30, 14, Ink);
        c.FillRoundRectAA(252, 204, 176, 18, 10, PinkBright);
        c.FillRoundRectAA(266, 212, 142, 7, 4, PinkSoft);
        // Cute scalloped frosting drips under hood rim!
        for (int i = 0; i < 6; i++)
        {
            float dx = 264 + i * 28f;
            c.FillCircleAA(dx, 198, 13, Ink);
            c.FillCircleAA(dx, 198, 9, PinkBright);
        }

        // Cute smiling front headlights & bumper grin on right nose of cart!
        c.FillCircleAA(412, 144, 18, Ink);
        c.FillCircleAA(412, 144, 13, MoonGold);
        c.FillCircleAA(410, 148, 5, White);
        // Happy bumper smile
        c.CapsuleAA(374, 116, 404, 120, 6, Ink);
        c.CapsuleAA(376, 116, 402, 120, 3, PinkSoft);

        // Ice-cream cone emblem painted on the cyan side panel (exact cover detail!)
        float ex = 212f, ey = 148f;
        c.CapsuleAA(ex - 10, ey - 32, ex + 8, ey + 6, 14, Ink);
        c.CapsuleAA(ex - 10, ey - 32, ex + 8, ey + 6, 9, Ginger);
        c.FillCircleAA(ex + 10, ey + 14, 18, Ink);
        c.FillCircleAA(ex + 10, ey + 14, 13, PurpleLight);
        c.FillCircleAA(ex + 18, ey + 24, 14, Ink);
        c.FillCircleAA(ex + 18, ey + 24, 10, PinkBright);
        c.FillCircleAA(ex + 15, ey + 28, 3.5f, White);

        // Colorful sprinkles on side panel!
        c.CapsuleAA(132, 152, 146, 160, 4, PinkBright);
        c.CapsuleAA(154, 172, 166, 166, 4, MoonGold);
        c.CapsuleAA(310, 168, 326, 174, 4, CyanLight);
        c.CapsuleAA(338, 150, 352, 158, 4, MoonGold);

        // Glossy Purple Spoked Wheels (front & back!)
        void DrawWheel(float wx, float wy, float wr)
        {
            c.FillCircleAA(wx, wy, wr + 5, Ink);
            c.FillCircleAA(wx, wy, wr, new Rgba(74, 38, 106));
            c.FillCircleAA(wx, wy, wr * 0.66f, Ink);
            c.FillCircleAA(wx, wy, wr * 0.56f, PurpleLight);
            c.FillCircleAA(wx, wy, wr * 0.28f, Ink);
            c.FillCircleAA(wx, wy, wr * 0.20f, PinkSoft);
            c.FillCircleAA(wx - wr * 0.25f, wy + wr * 0.30f, wr * 0.14f, White);
        }
        DrawWheel(156f, 84f, 44f);
        DrawWheel(352f, 84f, 46f);

        // Golden turbo flame streaks behind rear wheel!
        c.CapsuleAA(102, 72, 48, 66, 9, new Rgba(255, 198, 48, 220));
        c.CapsuleAA(96, 86, 58, 86, 6, new Rgba(255, 246, 152, 220));

        return c;
    }

    // =========================================================================
    // 4. COZY GINGERBREAD DELIVERY COTTAGE (MATCHING RIGHT SIDE OF COVER!)
    // =========================================================================
    public static ArtCanvas BuildGingerbreadHouse()
    {
        const int S = 384;
        ArtCanvas c = new ArtCanvas(S, S);

        // Frosty chimney
        c.FillRoundRectAA(258, 250, 46, 84, 8, Ink);
        c.FillRoundRectAA(264, 256, 34, 72, 5, Ginger);
        c.FillEllipseAA(281, 330, 28, 14, Ink);
        c.FillEllipseAA(281, 330, 22, 9, White);

        // Gingerbread walls
        c.FillRoundRectAA(68, 44, 248, 178, 18, Ink);
        c.FillRoundRectAA(76, 52, 232, 162, 14, Ginger);
        c.FillRoundRectAA(76, 52, 232, 34, 10, GingerShade);

        // Pitched Roof with crisscross icing grid & frosting eaves!
        for (int y = 198; y <= 342; y++)
        {
            float t = (y - 198f) / 144f;
            float hw = 158f * (1f - t * 0.94f);
            for (int x = (int)(192 - hw - 6); x <= (int)(192 + hw + 6); x++)
            {
                if (x < 0 || x >= S) continue;
                float dx = Math.Abs(x - 192f);
                if (dx > hw + 5) continue;
                if (dx > hw - 1) { c.Blend(x, y, Ink); continue; }
                bool grid = ((x + y) % 28 < 5) || ((x - y + 400) % 28 < 5);
                c.Blend(x, y, grid ? Cream : GingerShade);
            }
        }
        // Thick glossy white icing roof trim + scallops!
        c.CapsuleAA(38, 202, 192, 340, 14, Ink);
        c.CapsuleAA(346, 202, 192, 340, 14, Ink);
        c.CapsuleAA(38, 202, 192, 340, 9, White);
        c.CapsuleAA(346, 202, 192, 340, 9, White);
        c.FillCircleAA(192, 344, 16, Ink);
        c.FillCircleAA(192, 344, 11, PinkBright);
        for (int i = 0; i < 8; i++)
        {
            float sx = 64 + i * 36.5f;
            c.FillCircleAA(sx, 202, 18, Ink);
            c.FillCircleAA(sx, 202, 13, White);
        }

        // Glowing warm arched doorway with peppermint candy arch!
        c.FillRoundRectAA(154, 48, 76, 104, 34, Ink);
        c.FillRoundRectAA(162, 52, 60, 92, 28, PinkBright);
        c.FillRoundRectAA(170, 52, 44, 80, 22, Ink);
        c.FillRoundRectAA(175, 56, 34, 72, 18, MoonGold);
        c.SoftGlow(192, 92, 54, new Rgba(255, 226, 110, 85));

        // Glowing attic round window + side windows!
        c.FillCircleAA(192, 246, 28, Ink);
        c.FillCircleAA(192, 246, 22, MoonGold);
        c.CapsuleAA(170, 246, 214, 246, 3, Ink);
        c.CapsuleAA(192, 224, 192, 268, 3, Ink);

        c.FillCircleAA(114, 126, 22, Ink); c.FillCircleAA(114, 126, 16, MoonGold);
        c.FillCircleAA(270, 126, 22, Ink); c.FillCircleAA(270, 126, 16, MoonGold);

        // Gumdrop gems along eaves!
        Rgba[] gems = { PinkBright, CyanBright, MoonGold, MintHill, PurpleLight };
        for (int i = 0; i < 5; i++)
        {
            c.FillCircleAA(96 + i * 48f, 184, 9, Ink);
            c.FillCircleAA(96 + i * 48f, 184, 6, gems[i]);
        }
        return c;
    }

    // =========================================================================
    // 5. GIANT FROSTED CHERRY CUPCAKE (MATCHING LEFT SIDE OF COVER!)
    // =========================================================================
    public static ArtCanvas BuildGiantCupcake()
    {
        const int S = 256;
        ArtCanvas c = new ArtCanvas(S, S);

        // Golden fluted paper cup
        for (int y = 26; y <= 122; y++)
        {
            float t = (y - 26f) / 96f;
            float hw = 54f + t * 28f;
            for (int x = (int)(128 - hw - 5); x <= (int)(128 + hw + 5); x++)
            {
                float dx = Math.Abs(x - 128f);
                if (dx > hw + 4) continue;
                if (dx > hw - 1 || y < 31) { c.Blend(x, y, Ink); continue; }
                bool pleat = ((int)((x - 128) * 0.25f) + 20) % 2 == 0;
                c.Blend(x, y, pleat ? new Rgba(248, 188, 92) : new Rgba(226, 152, 68));
            }
        }

        // Lush swirled pink & vanilla icing tiers!
        c.FillEllipseAA(128, 134, 92, 38, Ink);
        c.FillEllipseAA(128, 134, 85, 31, PinkBright);
        c.FillEllipseAA(128, 162, 74, 34, Ink);
        c.FillEllipseAA(128, 162, 67, 27, PinkSoft);
        c.FillEllipseAA(128, 188, 48, 26, Ink);
        c.FillEllipseAA(128, 188, 41, 20, Cream);
        c.FillCircleAA(112, 194, 12, White);

        // Colorful sprinkles on icing
        c.CapsuleAA(80, 136, 94, 142, 4, CyanBright);
        c.CapsuleAA(154, 138, 168, 130, 4, MoonGold);
        c.CapsuleAA(108, 162, 122, 168, 4, MintHill);
        c.CapsuleAA(142, 166, 156, 160, 4, PurpleLight);

        // Glossy Red Cherry & Green Stem on top!
        c.CapsuleAA(128, 224, 148, 246, 5, Ink);
        c.CapsuleAA(128, 224, 148, 246, 2.5f, MintHill);
        c.FillCircleAA(128, 214, 22, Ink);
        c.FillCircleAA(128, 214, 17, PeppermintRed);
        c.FillCircleAA(122, 220, 6, White);
        return c;
    }

    // =========================================================================
    // 6. GIANT RAINBOW SWIRL LOLLIPOP (MATCHING BOTTOM RIGHT OF COVER!)
    // =========================================================================
    public static ArtCanvas BuildSwirlLollipop()
    {
        const int W = 256, H = 320;
        ArtCanvas c = new ArtCanvas(W, H);
        c.FillRoundRectAA(118, 16, 20, 154, 8, Ink);
        c.FillRoundRectAA(122, 20, 12, 146, 5, White);

        float cx = 128f, cy = 212f, R = 84f;
        Rgba[] cols = { PinkBright, MoonGold, CyanBright, PurpleLight };
        for (int y = (int)(cy - R - 6); y <= (int)(cy + R + 6); y++)
            for (int x = (int)(cx - R - 6); x <= (int)(cx + R + 6); x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                if (r > R + 5) continue;
                if (r > R - 1) { c.Blend(x, y, Ink); continue; }
                float ang = (float)Math.Atan2(dy, dx) + r * 0.085f;
                int band = (((int)Math.Floor((ang / (Math.PI * 2.0)) * 4.0) % 4) + 4) % 4;
                c.Blend(x, y, cols[band]);
            }
        c.FillEllipseAA(cx - 28, cy + 32, 22, 12, new Rgba(255, 255, 255, 190));
        return c;
    }

    // =========================================================================
    // 7. THE 3 GLOSSY JELLY MONSTERS FROM THE COVER (GREEN, PINK, PURPLE!)
    // =========================================================================
    public static ArtCanvas BuildJellyMonster(int kind)
    {
        const int S = 256;
        ArtCanvas c = new ArtCanvas(S, S);
        Rgba body, hi, shade;
        if (kind == 0) { body = new Rgba(88, 226, 82); hi = new Rgba(184, 255, 158); shade = new Rgba(46, 168, 56); }
        else if (kind == 1) { body = new Rgba(255, 92, 152); hi = new Rgba(255, 184, 214); shade = new Rgba(204, 42, 108); }
        else { body = new Rgba(158, 92, 242); hi = new Rgba(218, 174, 255); shade = new Rgba(106, 48, 182); }

        // Cute horns/antennae on the Purple jelly monster (just like bottom right of cover!)
        if (kind == 2)
        {
            c.CapsuleAA(92, 180, 74, 224, 15, Ink); c.CapsuleAA(92, 180, 74, 224, 9, body);
            c.CapsuleAA(164, 180, 182, 224, 15, Ink); c.CapsuleAA(164, 180, 182, 224, 9, body);
        }

        // Translucent squishy dome body!
        c.FillRoundRectAA(46, 44, 164, 154, 72, Ink);
        c.FillRoundRectAA(54, 52, 148, 138, 66, shade);
        c.FillRoundRectAA(54, 62, 148, 128, 64, body);
        // Glossy jelly specular highlight!
        c.FillEllipseAA(96, 158, 28, 14, hi);
        c.FillCircleAA(132, 168, 8, White);

        // Expressive cartoon eyes & mouth!
        if (kind == 0)
        {
            // Cheeky Green Jelly: happy wide grin + shiny dark eyes!
            c.FillCircleAA(98, 128, 12, Ink); c.FillCircleAA(95, 132, 4, White);
            c.FillCircleAA(158, 128, 12, Ink); c.FillCircleAA(155, 132, 4, White);
            c.FillEllipseAA(128, 96, 26, 16, Ink);
            c.FillEllipseAA(128, 92, 18, 9, new Rgba(242, 74, 118));
        }
        else if (kind == 1)
        {
            // Mischievous Pink Jelly: big bright cartoon eyes + silly grin!
            c.FillCircleAA(102, 132, 18, Ink); c.FillCircleAA(102, 132, 13, White); c.FillCircleAA(105, 130, 7, Ink); c.FillCircleAA(102, 134, 3, White);
            c.FillCircleAA(154, 132, 18, Ink); c.FillCircleAA(154, 132, 13, White); c.FillCircleAA(157, 130, 7, Ink); c.FillCircleAA(154, 134, 3, White);
            c.FillEllipseAA(128, 94, 22, 12, Ink);
            c.FillRoundRectAA(114, 96, 28, 7, 3, White);
        }
        else
        {
            // Grumpy Purple Jelly: big angry V-eyebrows + glowing eyes + scowl!
            c.FillCircleAA(98, 124, 18, Ink); c.FillCircleAA(98, 124, 13, MoonGold); c.FillCircleAA(101, 122, 6, Ink);
            c.FillCircleAA(158, 124, 18, Ink); c.FillCircleAA(158, 124, 13, MoonGold); c.FillCircleAA(155, 122, 6, Ink);
            c.CapsuleAA(78, 148, 116, 134, 7, Ink);
            c.CapsuleAA(178, 148, 140, 134, 7, Ink);
            c.CapsuleAA(104, 86, 128, 96, 6, Ink);
            c.CapsuleAA(128, 96, 152, 86, 6, Ink);
        }

        return c;
    }

    private static void BlitScaled(ArtCanvas dst, ArtCanvas src, int dx, int dy, int dw, int dh)
    {
        for (int y = 0; y < dh; y++)
        {
            int sy = Clamp((int)((y + 0.5f) * src.H / dh), 0, src.H - 1);
            for (int x = 0; x < dw; x++)
            {
                int sx = Clamp((int)((x + 0.5f) * src.W / dw), 0, src.W - 1);
                dst.Blend(dx + x, dy + y, src.Px[sy * src.W + sx]);
            }
        }
    }

#if UNITY_EDITOR
    public static void GenerateAndImportAll(string assetFolder)
    {
        string absDir = Path.Combine(Application.dataPath, assetFolder.Replace("Assets/", "").TrimStart('/'));
        Directory.CreateDirectory(absDir);

        GenerateAll((name, canvas) =>
        {
            Texture2D tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
            Color32[] cols = new Color32[canvas.Px.Length];
            for (int i = 0; i < cols.Length; i++) { Rgba p = canvas.Px[i]; cols[i] = new Color32(p.r, p.g, p.b, p.a); }
            tex.SetPixels32(cols);
            tex.Apply(false, false);
            File.WriteAllBytes(Path.Combine(absDir, name), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        });

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string file in Directory.GetFiles(absDir, "*.png"))
        {
            string relPath = assetFolder + "/" + Path.GetFileName(file);
            TextureImporter imp = AssetImporter.GetAtPath(relPath) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = Path.GetFileName(file).StartsWith("bg_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }
#endif
}
