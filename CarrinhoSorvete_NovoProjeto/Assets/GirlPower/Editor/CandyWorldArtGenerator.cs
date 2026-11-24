using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// Crisp pixel-art Candy World asset generator (FilterMode = Point, PPU = 32)
/// for "CarrinhoSorvete_NovoProjeto".
/// - 192x192 seamless pixel-art tiles (6.0 x 6.0 world units at PPU = 32):
///   * Clean, low-noise pastel mint pixel-art meadow (ZERO blurry blobs!)
///   * Crisp golden-biscuit/wafer road with bubblegum-pink & lavender candy curbs,
///     sprinkles on edges, and soft cream center dashes (ZERO grey asphalt!)
/// - Clean transparent cutouts of ALL 8 cupcake houses from "a casa de mosntros"
///   (ZERO beige/brown rectangles behind houses!)
/// - Thematic pixel-art SlowZone puddle & SpeedDeliveryGate striped candy arch/mat!
/// </summary>
public static class CandyWorldArtGenerator
{
    public const int TilePx = 192; // 192 / 32 PPU = 6.0 world units per Tilemap cell!

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
            if (x < 0 || x >= W || y < 0 || y >= H || c.a == 0) return;
            int idx = y * W + x;
            if (c.a == 255) { Px[idx] = c; return; }
            Rgba d = Px[idx];
            float sa = c.a / 255f, da = d.a / 255f, oa = sa + da * (1f - sa);
            if (oa <= 0.0001f) return;
            Px[idx] = new Rgba(
                (int)((c.r * sa + d.r * da * (1f - sa)) / oa),
                (int)((c.g * sa + d.g * da * (1f - sa)) / oa),
                (int)((c.b * sa + d.b * da * (1f - sa)) / oa),
                (int)(oa * 255f));
        }

        public void Rect(int x, int y, int w, int h, Rgba c)
        {
            int x0 = Clamp(x, 0, W), y0 = Clamp(y, 0, H);
            int x1 = Clamp(x + w, 0, W), y1 = Clamp(y + h, 0, H);
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++)
                    Set(px, py, c);
        }

        public void BlockRect(int bx, int by, int bw, int bh, int blockSize, Rgba c)
        {
            Rect(bx * blockSize, by * blockSize, bw * blockSize, bh * blockSize, c);
        }

        public void CircleHard(float cx, float cy, float r, Rgba c)
        {
            int x0 = Clamp((int)Math.Floor(cx - r), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + r), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - r), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + r), 0, H - 1);
            float r2 = r * r;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if ((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy) <= r2)
                        Set(x, y, c);
        }

        public void EllipseHard(float cx, float cy, float rx, float ry, Rgba c)
        {
            int x0 = Clamp((int)Math.Floor(cx - rx), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + rx), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - ry), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + ry), 0, H - 1);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f - cy) / ry;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
            }
        }

        public void LineHard(int x0, int y0, int x1, int y1, int thick, Rgba c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int r = thick / 2;
            while (true)
            {
                Rect(x0 - r, y0 - r, thick, thick, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
    }

    // Unified Crisp Pixel-Art Candy Palette
    private static readonly Rgba Ink = new Rgba(64, 34, 68);
    private static readonly Rgba MintBase = new Rgba(166, 224, 186);
    private static readonly Rgba MintLight = new Rgba(184, 236, 198);
    private static readonly Rgba MintShade = new Rgba(146, 208, 168);
    private static readonly Rgba BiscuitBase = new Rgba(246, 218, 170);
    private static readonly Rgba BiscuitLight = new Rgba(252, 230, 188);
    private static readonly Rgba BiscuitShade = new Rgba(226, 190, 140);
    private static readonly Rgba Cream = new Rgba(255, 248, 232);
    private static readonly Rgba PinkCandy = new Rgba(252, 126, 188);
    private static readonly Rgba PinkLight = new Rgba(255, 182, 220);
    private static readonly Rgba Lavender = new Rgba(196, 156, 246);
    private static readonly Rgba AquaCandy = new Rgba(116, 224, 236);
    private static readonly Rgba LemonCandy = new Rgba(255, 228, 114);

    private static readonly Rgba[] Sprinkles =
    {
        new Rgba(255, 114, 178),
        new Rgba(106, 212, 246),
        new Rgba(255, 216, 86),
        new Rgba(186, 134, 250),
        new Rgba(124, 230, 166)
    };

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    private static Rgba Lerp(Rgba a, Rgba b, float t)
    {
        t = Clamp01(t);
        return new Rgba((int)(a.r + (b.r - a.r) * t), (int)(a.g + (b.g - a.g) * t), (int)(a.b + (b.b - a.b) * t), (int)(a.a + (b.a - a.a) * t));
    }
    private static int Hash(int x, int y)
    {
        unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return h ^ (h >> 16); }
    }

    public static void GenerateAllProcedural(Action<string, Canvas> writer)
    {
        writer("CandyMeadowTile.png", BuildCrispPixelMeadowTile());
        writer("CandyRoadH.png", BuildCrispPixelRoad(false));
        writer("CandyRoadV.png", BuildCrispPixelRoad(true));
        writer("CandyRoadCross.png", BuildCrispPixelCross());
        writer("CandyRoadTee.png", BuildCrispPixelTee());
        writer("CandyRoadCorner.png", BuildCrispPixelCorner());
        writer("CandyDeliveryPlaza.png", BuildDeliveryPlaza());
        writer("CandyBerryPond.png", BuildBerryPond());
        writer("GiantLollipopA.png", BuildGiantLollipop(true));
        writer("GiantLollipopB.png", BuildGiantLollipop(false));
        writer("CandyCanePost.png", BuildCandyCane());
        writer("CandyLampPost.png", BuildCandyLamp());
        writer("SugarFlowerBed.png", BuildSugarFlowerBed());
        writer("MarshmallowCluster.png", BuildMarshmallowCluster());
        writer("CandySparkle.png", BuildCandySparkle());
        writer("CandyGlow.png", BuildCandyGlow());
        writer("CandyShadow.png", BuildCandyShadow());
        writer("ParallaxCloudWisp.png", BuildParallaxCloudWisp());
        writer("CandyIceCreamSign.png", BuildIceCreamSign());
        writer("CandyStripedAwning.png", BuildStripedAwning());
        writer("CandyCritterGummy.png", BuildCandyCritter(true));
        writer("CandyCritterMarshmallow.png", BuildCandyCritter(false));
        writer("CandyDeliveryConeBeacon.png", BuildDeliveryConeBeacon());
        writer("CandyBuntingFence.png", BuildBuntingFence());
        writer("CandyCottonBush.png", BuildCottonBush());
        writer("CandyConfettiStar.png", BuildConfettiStar());
    }

    // =========================================================================
    // 1. CRISP PIXEL-ART PASTEL MEADOW (192x192, 3px pixel grid, LOW NOISE!)
    //    Replaces blurry green floor with clean, calm, sharp pixel art!
    // =========================================================================
    public static Canvas BuildCrispPixelMeadowTile()
    {
        Canvas c = new Canvas(TilePx, TilePx);
        const int P = 3; // crisp 3x3 pixel-art texel grid (64x64 logical pixels)
        c.Clear(MintBase);

        // Low-contrast, broad patches avoid the repeated diagonal noise of the earlier tile.
        int[,] patches = { { 7, 9 }, { 39, 13 }, { 20, 32 }, { 52, 43 }, { 8, 54 } };
        for (int i = 0; i < patches.GetLength(0); i++)
        {
            int px = patches[i, 0], py = patches[i, 1];
            c.BlockRect(px, py, 4, 2, P, MintLight);
            c.BlockRect(px + 1, py + 2, 3, 1, P, MintShade);
        }

        // A few small grass tufts keep the ground alive without competing with the route.
        int[,] tufts = { { 12, 18 }, { 45, 30 }, { 28, 51 }, { 56, 8 } };
        for (int i = 0; i < tufts.GetLength(0); i++)
        {
            int tx = tufts[i, 0], ty = tufts[i, 1];
            c.BlockRect(tx, ty, 1, 2, P, MintShade);
            c.BlockRect(tx + 2, ty, 1, 2, P, MintShade);
            c.BlockRect(tx + 1, ty + 1, 1, 2, P, MintLight);
        }

        // Sparse little flowers; the road edge and delivery markers keep the stronger colors.
        int[,] fl = { { 16, 22 }, { 48, 46 } };
        Rgba[] cols = { PinkLight, Cream };
        for (int i = 0; i < fl.GetLength(0); i++)
        {
            int fx = fl[i, 0], fy = fl[i, 1];
            Rgba col = cols[i % cols.Length];
            c.BlockRect(fx - 1, fy, 1, 1, P, col);
            c.BlockRect(fx + 1, fy, 1, 1, P, col);
            c.BlockRect(fx, fy - 1, 1, 1, P, col);
            c.BlockRect(fx, fy + 1, 1, 1, P, col);
            c.BlockRect(fx, fy, 1, 1, P, LemonCandy);
        }
        return c;
    }

    // =========================================================================
    // 2. CRISP PIXEL-ART GOLDEN BISCUIT & CANDY-CURB TRACK (192x192, PPU=32 -> 6.0 units!)
    //    Drivable center lane: y in [27..165] (138px = 4.31 units wide!)
    //    Zero grey asphalt! Sharp pixel-art outlines, pink/lavender candy curbs,
    //    sprinkles concentrated on the curbs, smooth low-contrast biscuit center!
    // =========================================================================
    public static Canvas BuildCrispPixelRoad(bool vertical)
    {
        Canvas c = new Canvas(TilePx, TilePx);
        const int P = 3;
        int G = TilePx / P; // 64 logical pixels

        for (int gy = 0; gy < G; gy++)
        {
            int dy = Math.Min(gy, G - 1 - gy);
            for (int gx = 0; gx < G; gx++)
            {
                if (dy == 0)
                {
                    c.BlockRect(gx, gy, 1, 1, P, new Rgba(64, 34, 68, 70)); // crisp shadow edge
                }
                else if (dy == 1)
                {
                    c.BlockRect(gx, gy, 1, 1, P, Ink);
                }
                else if (dy <= 7)
                {
                    // Bubblegum-pink & lavender pixel-art candy curb stones!
                    bool seam = (gx % 12 == 0);
                    int stone = (gx / 12) % 3;
                    Rgba col = seam ? new Rgba(176, 126, 112) :
                        (stone == 0 ? new Rgba(246, 222, 191) :
                        (stone == 1 ? new Rgba(255, 242, 220) : new Rgba(239, 211, 225)));
                    c.BlockRect(gx, gy, 1, 1, P, col);
                }
                else if (dy == 8)
                {
                    c.BlockRect(gx, gy, 1, 1, P, Ink);
                }
                else
                {
                    // Smooth low-contrast golden biscuit center lane!
                    Rgba roadCol = (dy <= 10) ? BiscuitShade : ((dy >= 24 && dy <= 40) ? BiscuitLight : BiscuitBase);
                    c.BlockRect(gx, gy, 1, 1, P, roadCol);
                }
            }
        }

        // Crisp cream dashed lane markings in center (gy = 31..32)
        c.BlockRect(8, 31, 16, 2, P, Cream);
        c.BlockRect(40, 31, 16, 2, P, Cream);

        // Crisp pixel sprinkles ONLY along the inner curb shoulder (never cluttering center!)
        int[] sx = { 5, 18, 31, 46, 58 };
        for (int i = 0; i < sx.Length; i++)
        {
            Rgba sp = Sprinkles[i % Sprinkles.Length];
            c.BlockRect(sx[i], 10 + (i % 2), 3, 1, P, sp);
            c.BlockRect((sx[i] + 7) % 60, G - 12 - (i % 2), 3, 1, P, Sprinkles[(i + 2) % Sprinkles.Length]);
        }

        return vertical ? Transpose(c) : c;
    }

    public static Canvas BuildCrispPixelCross()
    {
        Canvas c = new Canvas(TilePx, TilePx);
        const int P = 3;
        int G = TilePx / P; // 64
        c.Clear(BiscuitBase);
        c.BlockRect(10, 10, G - 20, G - 20, P, BiscuitLight);

        // Crisp rounded candy-stone corner curbs in all 4 corners (exact match to road curb dy<=8!)
        int[,] corners = { { 0, 0 }, { G - 1, 0 }, { 0, G - 1 }, { G - 1, G - 1 } };
        for (int k = 0; k < 4; k++)
        {
            int cx = corners[k, 0], cy = corners[k, 1];
            for (int gy = 0; gy < G; gy++)
            {
                for (int gx = 0; gx < G; gx++)
                {
                    float d = (float)Math.Sqrt((gx - cx) * (gx - cx) + (gy - cy) * (gy - cy));
                    if (d <= 1.2f) c.BlockRect(gx, gy, 1, 1, P, new Rgba(0, 0, 0, 0));
                    else if (d <= 2.2f) c.BlockRect(gx, gy, 1, 1, P, Ink);
            else if (d <= 7.8f) c.BlockRect(gx, gy, 1, 1, P, ((gx + gy) / 5) % 2 == 0 ? new Rgba(239, 211, 225) : new Rgba(255, 242, 220));
                    else if (d <= 8.8f) c.BlockRect(gx, gy, 1, 1, P, Ink);
                }
            }
        }

        // Cream pixel crosswalk dashes at all 4 entrances
        for (int i = 0; i < 4; i++)
        {
            int off = 14 + i * 11;
            c.BlockRect(off, 3, 5, 3, P, Cream);
            c.BlockRect(off, G - 6, 5, 3, P, Cream);
            c.BlockRect(3, off, 3, 5, P, Cream);
            c.BlockRect(G - 6, off, 3, 5, P, Cream);
        }
        return c;
    }

    public static Canvas BuildCrispPixelTee()
    {
        // Closed continuous candy curb along North (+Y); open to West, East, and South (-Y).
        // Rotations: 0 = North T (0,+3), 90 = West T (-3,0), 180 = South T (0,-3), 270 = East T (+3,0).
        Canvas c = new Canvas(TilePx, TilePx);
        const int P = 3;
        int G = TilePx / P; // 64
        c.Clear(BiscuitBase);
        c.BlockRect(0, 10, G, G - 20, P, BiscuitLight);
        c.BlockRect(10, 0, G - 20, G - 10, P, BiscuitLight);

        // Continuous North curb (gy in [G-9 .. G-1]) matching BuildCrispPixelRoad!
        for (int gy = G - 9; gy < G; gy++)
        {
            int dy = G - 1 - gy;
            for (int gx = 0; gx < G; gx++)
            {
                if (dy == 0) c.BlockRect(gx, gy, 1, 1, P, new Rgba(64, 34, 68, 70));
                else if (dy == 1) c.BlockRect(gx, gy, 1, 1, P, Ink);
                else if (dy <= 7)
                {
                    bool seam = (gx % 12 == 0);
                    int stone = (gx / 12) % 3;
                    Rgba col = seam ? new Rgba(176, 126, 112) :
                        (stone == 0 ? new Rgba(246, 222, 191) :
                        (stone == 1 ? new Rgba(255, 242, 220) : new Rgba(239, 211, 225)));
                    c.BlockRect(gx, gy, 1, 1, P, col);
                }
                else if (dy == 8) c.BlockRect(gx, gy, 1, 1, P, Ink);
            }
        }
        // Subtle shade right inside North curb
        c.BlockRect(0, G - 11, G, 2, P, BiscuitShade);

        // Rounded South-West and South-East inner corner curbs matching road curb dy<=8
        int[,] southCorners = { { 0, 0 }, { G - 1, 0 } };
        for (int k = 0; k < 2; k++)
        {
            int cx = southCorners[k, 0], cy = southCorners[k, 1];
            for (int gy = 0; gy < 12; gy++)
            {
                for (int gx = 0; gx < G; gx++)
                {
                    float d = (float)Math.Sqrt((gx - cx) * (gx - cx) + (gy - cy) * (gy - cy));
                    if (d <= 1.2f) c.BlockRect(gx, gy, 1, 1, P, new Rgba(0, 0, 0, 0));
                    else if (d <= 2.2f) c.BlockRect(gx, gy, 1, 1, P, Ink);
                    else if (d <= 7.8f) c.BlockRect(gx, gy, 1, 1, P, ((gx + gy) / 4 % 2 == 0) ? new Rgba(239, 211, 225) : new Rgba(255, 242, 220));
                    else if (d <= 8.8f) c.BlockRect(gx, gy, 1, 1, P, Ink);
                }
            }
        }

        // Cream dashes on West, East, and South entrances
        for (int i = 0; i < 4; i++)
        {
            int off = 14 + i * 11;
            c.BlockRect(off, 3, 5, 3, P, Cream);
            c.BlockRect(3, off, 3, 5, P, Cream);
            c.BlockRect(G - 6, off, 3, 5, P, Cream);
        }
        return c;
    }

    public static Canvas BuildCrispPixelCorner()
    {
        // Connects East (+X) and North (+Y) cleanly at 0 deg; rotate 90/180/270 for other 3 corners!
        Canvas c = new Canvas(TilePx, TilePx);
        const int P = 3;
        int G = TilePx / P; // 64
        for (int gy = 0; gy < G; gy++)
        {
            for (int gx = 0; gx < G; gx++)
            {
                float d = (float)Math.Sqrt((gx - (G - 1)) * (gx - (G - 1)) + (gy - (G - 1)) * (gy - (G - 1)));
                if (d < 1.2f || d > G - 1.0f) continue;
                if (d < 2.2f || d > G - 2.0f) { c.BlockRect(gx, gy, 1, 1, P, Ink); continue; }
                if (d < 7.8f || d > G - 8.0f)
                {
                    int seg = (int)(Math.Atan2(G - 1 - gy, G - 1 - gx) * 6.0);
                    c.BlockRect(gx, gy, 1, 1, P, (seg % 2 == 0) ? new Rgba(239, 211, 225) : new Rgba(255, 242, 220));
                    continue;
                }
                if (d < 8.8f || d > G - 9.0f) { c.BlockRect(gx, gy, 1, 1, P, Ink); continue; }
                bool center = Math.Abs(d - 32f) < 9f;
                c.BlockRect(gx, gy, 1, 1, P, center ? BiscuitLight : BiscuitBase);
            }
        }
        // Cream curved center dashes
        for (int i = 1; i <= 3; i++)
        {
            double ang = (i / 4.0) * (Math.PI * 0.5);
            int mx = (G - 1) - (int)Math.Round(Math.Cos(ang) * 32.0);
            int my = (G - 1) - (int)Math.Round(Math.Sin(ang) * 32.0);
            c.BlockRect(mx - 1, my - 1, 3, 3, P, Cream);
        }
        return c;
    }

    // =========================================================================
    // 3. PIXEL-ART DELIVERY PLAZA, BERRY POND, PROPS & PARALLAX CLOUDS
    // =========================================================================
    public static Canvas BuildDeliveryPlaza()
    {
        const int S = 192; // Exact match to 6.0x6.0 intersection!
        Canvas c = new Canvas(S, S);
        float mid = S * 0.5f;
        c.CircleHard(mid, mid - 2, 92, new Rgba(64, 34, 68, 70));
        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.PI * 2.0 / 16.0;
            float sx = mid + (float)Math.Cos(a) * 80f, sy = mid + (float)Math.Sin(a) * 80f;
            c.CircleHard(sx, sy, 12, Ink);
            c.CircleHard(sx, sy, 9.5f, i % 2 == 0 ? PinkLight : Cream);
        }
        c.CircleHard(mid, mid, 76, Ink);
        c.CircleHard(mid, mid, 72, new Rgba(255, 198, 226));
        c.CircleHard(mid, mid, 62, Cream);
        c.CircleHard(mid, mid, 52, PinkCandy);
        c.CircleHard(mid, mid, 44, PinkLight);
        DrawStarHard(c, mid, mid, 32, 14, LemonCandy, Ink);
        DrawStarHard(c, mid, mid, 22, 9, Cream, new Rgba(255, 206, 88));
        return c;
    }

    public static Canvas BuildBerryPond()
    {
        const int W = 192, H = 128;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(96, 62, 90, 54, Ink);
        c.EllipseHard(96, 62, 86, 50, Cream);
        c.EllipseHard(96, 62, 76, 42, Ink);
        c.EllipseHard(96, 62, 72, 38, AquaCandy);
        c.EllipseHard(90, 58, 52, 26, new Rgba(162, 244, 248));
        c.Rect(46, 72, 28, 3, Cream);
        c.Rect(114, 48, 30, 3, Cream);
        // Cute lily pads
        c.CircleHard(62, 60, 11, Ink); c.CircleHard(62, 60, 9, MintBase); c.CircleHard(62, 60, 4, PinkCandy);
        c.CircleHard(132, 66, 10, Ink); c.CircleHard(132, 66, 8, MintBase); c.CircleHard(132, 66, 4, LemonCandy);
        return c;
    }

    public static Canvas BuildGiantLollipop(bool rainbowPink)
    {
        const int W = 64, H = 96;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(32, 8, 14, 5, new Rgba(64, 34, 68, 80));
        c.Rect(28, 8, 8, 42, Ink);
        c.Rect(30, 10, 4, 38, Cream);
        float cx = 32f, cy = 62f, R = 24f;
        c.CircleHard(cx, cy, R + 3f, Ink);
        for (int y = (int)(cy - R); y <= (int)(cy + R); y++)
            for (int x = (int)(cx - R); x <= (int)(cx + R); x++)
            {
                float dx = x - cx, dy = y - cy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > R) continue;
                int band = ((int)Math.Floor(((float)Math.Atan2(dy, dx) + d * 0.34f) / 1.05f) % 4 + 4) % 4;
                Rgba col = rainbowPink
                    ? (band == 0 ? PinkCandy : (band == 1 ? Cream : (band == 2 ? LemonCandy : Lavender)))
                    : (band == 0 ? AquaCandy : (band == 1 ? Cream : (band == 2 ? PinkLight : MintLight)));
                c.Set(x, y, col);
            }
        c.Rect(22, 68, 6, 4, new Rgba(255, 255, 255, 210));
        return c;
    }

    public static Canvas BuildCandyCane()
    {
        const int W = 64, H = 96;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(26, 8, 12, 4, new Rgba(64, 34, 68, 80));
        List<float> xs = new List<float>(), ys = new List<float>();
        for (int i = 0; i <= 26; i++) { xs.Add(24f); ys.Add(10f + i * 2.3f); }
        for (int i = 1; i <= 20; i++)
        {
            double a = Math.PI - (i / 20.0) * Math.PI;
            xs.Add(37f + (float)Math.Cos(a) * 13f); ys.Add(70f + (float)Math.Sin(a) * 13f);
        }
        for (int i = 0; i < xs.Count; i++) c.CircleHard(xs[i], ys[i], 8f, Ink);
        Rgba red = new Rgba(244, 78, 124);
        for (int i = 0; i < xs.Count; i++) c.CircleHard(xs[i], ys[i], 5.8f, ((i / 4) % 2 == 0) ? red : Cream);
        return c;
    }

    public static Canvas BuildCandyLamp()
    {
        const int W = 48, H = 96;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(24, 7, 11, 4, new Rgba(64, 34, 68, 80));
        c.Rect(16, 8, 16, 8, Ink); c.Rect(18, 10, 12, 4, Lavender);
        for (int y = 16; y < 68; y++)
        {
            c.Rect(20, y, 8, 1, Ink);
            c.Rect(22, y, 4, 1, ((y / 6) % 2 == 0) ? PinkCandy : Cream);
        }
        c.CircleHard(24, 78, 12, Ink);
        c.CircleHard(24, 78, 9, LemonCandy);
        c.CircleHard(24, 78, 6, Cream);
        return c;
    }

    public static Canvas BuildSugarFlowerBed()
    {
        const int W = 96, H = 64;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(48, 12, 40, 10, new Rgba(64, 34, 68, 70));
        c.CircleHard(26, 26, 18, Ink); c.CircleHard(70, 26, 18, Ink); c.CircleHard(48, 34, 22, Ink);
        c.CircleHard(26, 26, 15, MintShade); c.CircleHard(70, 26, 15, MintShade); c.CircleHard(48, 34, 19, MintBase);
        c.CircleHard(48, 40, 12, MintLight);
        c.CircleHard(28, 30, 5, PinkCandy); c.CircleHard(48, 42, 6, LemonCandy); c.CircleHard(68, 30, 5, Lavender);
        c.CircleHard(28, 30, 2, Cream); c.CircleHard(48, 42, 2, Cream); c.CircleHard(68, 30, 2, Cream);
        return c;
    }

    public static Canvas BuildMarshmallowCluster()
    {
        const int W = 80, H = 56;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(40, 10, 32, 8, new Rgba(64, 34, 68, 70));
        c.Rect(12, 12, 26, 20, Ink); c.Rect(15, 15, 20, 14, PinkLight);
        c.Rect(42, 12, 26, 20, Ink); c.Rect(45, 15, 20, 14, AquaCandy);
        c.Rect(27, 24, 26, 22, Ink); c.Rect(30, 27, 20, 16, Cream);
        return c;
    }

    public static Canvas BuildCandySparkle()
    {
        const int S = 32;
        Canvas c = new Canvas(S, S);
        c.Rect(14, 4, 4, 24, new Rgba(255, 244, 210, 230));
        c.Rect(4, 14, 24, 4, new Rgba(255, 244, 210, 230));
        c.Rect(12, 12, 8, 8, new Rgba(255, 255, 255, 255));
        return c;
    }

    public static Canvas BuildCandyGlow()
    {
        const int S = 64;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 30f;
                if (d < 1f) c.Set(x, y, new Rgba(255, 224, 180, (int)((1f - d) * (1f - d) * 140f)));
            }
        return c;
    }

    public static Canvas BuildCandyShadow()
    {
        const int W = 64, H = 32;
        Canvas c = new Canvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - 31.5f) / 29f, dy = (y - 15.5f) / 13f;
                float d = dx * dx + dy * dy;
                if (d < 1f) c.Set(x, y, new Rgba(64, 34, 72, (int)((1f - d) * 130f)));
            }
        return c;
    }

    public static Canvas BuildParallaxCloudWisp()
    {
        // Soft pastel cotton-candy sky wisp for gentle top-down parallax depth!
        const int W = 128, H = 64;
        Canvas c = new Canvas(W, H);
        Rgba wisp = new Rgba(255, 244, 252, 48);
        Rgba wispCore = new Rgba(255, 250, 255, 68);
        c.EllipseHard(64, 32, 46, 18, wisp);
        c.EllipseHard(44, 34, 26, 16, wisp);
        c.EllipseHard(84, 30, 28, 16, wisp);
        c.EllipseHard(64, 34, 32, 12, wispCore);
        return c;
    }

    // =========================================================================
    // 4. EXPRESSIVE CANDY THEME-PARK PROPS (SIGNS, AWNINGS, CRITTERS, BEACON, PARK FENCE)
    // =========================================================================
    public static Canvas BuildIceCreamSign()
    {
        // Cute illustrated ice-cream cone sign on a wooden wafer stand (64x80)
        const int W = 64, H = 80;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(32, 8, 16, 5, new Rgba(64, 34, 68, 78));
        // Stand post
        c.Rect(27, 8, 10, 22, Ink);
        c.Rect(29, 10, 6, 18, new Rgba(214, 162, 108));
        // Scalloped biscuit signboard frame
        c.Rect(8, 26, 48, 46, Ink);
        c.Rect(11, 29, 42, 40, PinkCandy);
        c.Rect(14, 32, 36, 34, Cream);
        // Cute ice-cream cone inside sign!
        for (int y = 35; y < 50; y++)
        {
            float t = (y - 35) / 15f;
            int half = (int)(2 + t * 9);
            c.Rect(32 - half - 1, y, (half + 1) * 2, 1, Ink);
            c.Rect(32 - half, y, half * 2, 1, new Rgba(234, 178, 114));
        }
        // Strawberry + Vanilla double scoop!
        c.CircleHard(26, 53, 8, Ink); c.CircleHard(26, 53, 6, PinkCandy);
        c.CircleHard(38, 53, 8, Ink); c.CircleHard(38, 53, 6, MintBase);
        c.CircleHard(32, 58, 9, Ink); c.CircleHard(32, 58, 7, PinkLight);
        // Cherry on top + happy eyes!
        c.CircleHard(32, 66, 4, Ink); c.CircleHard(32, 66, 2.5f, new Rgba(248, 76, 128));
        c.Rect(29, 57, 2, 2, Ink); c.Rect(34, 57, 2, 2, Ink);
        c.Rect(31, 55, 3, 1, Ink);
        return c;
    }

    public static Canvas BuildStripedAwning()
    {
        // Festive strawberry-and-vanilla striped scalloped awning (96x48)
        const int W = 96, H = 48;
        Canvas c = new Canvas(W, H);
        c.Rect(4, 12, 88, 28, Ink);
        for (int i = 0; i < 8; i++)
        {
            int sx = 6 + i * 10;
            Rgba col = (i % 2 == 0) ? PinkCandy : Cream;
            Rgba shade = (i % 2 == 0) ? new Rgba(230, 96, 166) : BiscuitLight;
            c.Rect(sx, 14, 10, 24, col);
            c.Rect(sx, 14, 10, 5, shade);
            // Scallop loops at bottom
            c.CircleHard(sx + 5, 13, 6.5f, Ink);
            c.CircleHard(sx + 5, 13, 4.5f, col);
        }
        // Top decorative rail
        c.Rect(4, 37, 88, 5, Ink);
        c.Rect(6, 38, 84, 3, LemonCandy);
        return c;
    }

    public static Canvas BuildCandyCritter(bool gummyBear)
    {
        // Cute joyful candy critters cheering near sidewalks & delivery points (48x48)
        const int S = 48;
        Canvas c = new Canvas(S, S);
        c.EllipseHard(24, 7, 13, 4.5f, new Rgba(64, 34, 68, 75));
        if (gummyBear)
        {
            // Strawberry gummy bear with raised cheering paws!
            Rgba body = new Rgba(255, 118, 178);
            Rgba hi = new Rgba(255, 182, 218);
            c.CircleHard(14, 38, 6, Ink); c.CircleHard(14, 38, 4, body); // Left ear
            c.CircleHard(34, 38, 6, Ink); c.CircleHard(34, 38, 4, body); // Right ear
            c.CircleHard(10, 24, 5, Ink); c.CircleHard(10, 24, 3, body); // Left raised paw
            c.CircleHard(38, 24, 5, Ink); c.CircleHard(38, 24, 3, body); // Right raised paw
            c.EllipseHard(24, 18, 12, 11, Ink); c.EllipseHard(24, 18, 10, 9, body); // Belly
            c.CircleHard(24, 30, 12, Ink); c.CircleHard(24, 30, 10, body); // Head
            c.EllipseHard(24, 17, 6, 5, hi); // Cute tummy highlight
            c.CircleHard(19, 34, 2.5f, Cream); // Gloss
            // Sparkling happy eyes, rosy cheeks & big smile!
            c.Rect(18, 30, 3, 4, Ink); c.Rect(19, 32, 1, 1, Cream);
            c.Rect(27, 30, 3, 4, Ink); c.Rect(28, 32, 1, 1, Cream);
            c.Rect(14, 27, 4, 2, new Rgba(255, 86, 146));
            c.Rect(30, 27, 4, 2, new Rgba(255, 86, 146));
            c.Rect(21, 26, 6, 2, Ink);
        }
        else
        {
            // Cheerful vanilla marshmallow buddy with lavender bow!
            c.Rect(11, 10, 26, 26, Ink);
            c.Rect(13, 12, 22, 22, Cream);
            c.EllipseHard(24, 35, 13, 5, Ink); c.EllipseHard(24, 35, 11, 3.5f, new Rgba(255, 255, 255));
            c.EllipseHard(24, 11, 13, 5, Ink); c.EllipseHard(24, 11, 11, 3.5f, new Rgba(242, 232, 248));
            // Cute lavender bow on top!
            c.CircleHard(19, 40, 4, Ink); c.CircleHard(19, 40, 2.5f, Lavender);
            c.CircleHard(29, 40, 4, Ink); c.CircleHard(29, 40, 2.5f, Lavender);
            c.CircleHard(24, 40, 3, Ink); c.CircleHard(24, 40, 1.8f, PinkCandy);
            // Happy eyes, blush & smile
            c.Rect(17, 24, 3, 4, Ink); c.Rect(18, 26, 1, 1, Cream);
            c.Rect(28, 24, 3, 4, Ink); c.Rect(29, 26, 1, 1, Cream);
            c.Rect(14, 21, 3, 2, PinkCandy);
            c.Rect(31, 21, 3, 2, PinkCandy);
            c.Rect(21, 19, 6, 2, Ink);
            c.Rect(20, 20, 2, 2, Ink); c.Rect(26, 20, 2, 2, Ink);
        }
        return c;
    }

    public static Canvas BuildDeliveryConeBeacon()
    {
        // Unmistakable giant ice-cream cone + downward golden arrow for kid-friendly delivery beacons (64x96)
        const int W = 64, H = 96;
        Canvas c = new Canvas(W, H);
        // Bright golden downward arrow at bottom!
        for (int y = 6; y < 28; y++)
        {
            int half = (y - 6) * 16 / 22;
            c.Rect(32 - half - 2, y, (half + 2) * 2, 1, Ink);
            if (half > 1) c.Rect(32 - half, y, half * 2, 1, LemonCandy);
        }
        c.Rect(25, 26, 14, 10, Ink);
        c.Rect(27, 28, 10, 6, LemonCandy);
        // Giant waffle cone above arrow
        for (int y = 38; y < 64; y++)
        {
            float t = (y - 38) / 26f;
            int half = (int)(3 + t * 14);
            c.Rect(32 - half - 2, y, (half + 2) * 2, 1, Ink);
            Rgba wCol = ((y / 4) % 2 == 0) ? new Rgba(240, 186, 118) : new Rgba(222, 162, 96);
            c.Rect(32 - half, y, half * 2, 1, wCol);
        }
        // Triple pastel ice-cream scoops!
        c.CircleHard(23, 66, 11, Ink); c.CircleHard(23, 66, 8.5f, MintBase);
        c.CircleHard(41, 66, 11, Ink); c.CircleHard(41, 66, 8.5f, Lavender);
        c.CircleHard(32, 74, 13, Ink); c.CircleHard(32, 74, 10.5f, PinkCandy);
        c.CircleHard(28, 78, 3.5f, PinkLight);
        // Cherry + sparkle on top!
        c.CircleHard(32, 87, 5.5f, Ink); c.CircleHard(32, 87, 3.8f, new Rgba(248, 72, 124));
        c.CircleHard(30, 89, 1.5f, Cream);
        // Tiny colorful sprinkles on scoop
        c.Rect(26, 72, 4, 2, LemonCandy);
        c.Rect(35, 74, 4, 2, AquaCandy);
        c.Rect(30, 67, 4, 2, Cream);
        return c;
    }

    public static Canvas BuildBuntingFence()
    {
        // Festive candy theme-park fence with peppermint posts & colorful bunting flags (96x64)
        const int W = 96, H = 64;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(48, 8, 42, 6, new Rgba(64, 34, 68, 65));
        // Horizontal cream rails
        c.Rect(8, 16, 80, 9, Ink); c.Rect(10, 18, 76, 5, Cream);
        c.Rect(8, 34, 80, 9, Ink); c.Rect(10, 36, 76, 5, PinkLight);
        // Three peppermint-striped posts with candy ball tops
        int[] px = { 12, 48, 84 };
        for (int i = 0; i < px.Length; i++)
        {
            int x = px[i];
            c.Rect(x - 6, 8, 12, 42, Ink);
            for (int y = 10; y < 48; y++)
                c.Rect(x - 4, y, 8, 1, ((y / 5) % 2 == 0) ? PinkCandy : Cream);
            c.CircleHard(x, 52, 7, Ink);
            c.CircleHard(x, 52, 5, (i == 1) ? LemonCandy : Lavender);
            c.CircleHard(x - 2, 54, 1.8f, Cream);
        }
        // Festive bunting triangles hanging along top rail!
        Rgba[] flags = { PinkCandy, LemonCandy, AquaCandy, Lavender, MintBase, PinkCandy };
        for (int f = 0; f < 6; f++)
        {
            int fx = 19 + f * 11;
            for (int dy = 0; dy < 10; dy++)
            {
                int half = (10 - dy) / 2;
                c.Rect(fx - half - 1, 35 - dy, (half + 1) * 2, 1, Ink);
                if (half > 0) c.Rect(fx - half, 35 - dy, half * 2, 1, flags[f]);
            }
        }
        return c;
    }

    public static Canvas BuildCottonBush()
    {
        // Fluffy cotton-candy hedge bush with little sugar flowers (80x64)
        const int W = 80, H = 64;
        Canvas c = new Canvas(W, H);
        c.EllipseHard(40, 11, 32, 8, new Rgba(64, 34, 68, 70));
        Rgba puffA = new Rgba(255, 164, 212);
        Rgba puffB = new Rgba(214, 174, 250);
        Rgba puffHi = new Rgba(255, 224, 242);
        c.CircleHard(22, 26, 17, Ink);
        c.CircleHard(58, 26, 17, Ink);
        c.CircleHard(40, 34, 21, Ink);
        c.CircleHard(22, 26, 14, puffB);
        c.CircleHard(58, 26, 14, puffB);
        c.CircleHard(40, 34, 18, puffA);
        c.CircleHard(34, 41, 9, puffHi);
        c.CircleHard(52, 31, 6, puffHi);
        // Cute sugar blossoms on bush
        int[,] fl = { { 22, 28 }, { 44, 34 }, { 56, 26 }, { 34, 22 } };
        for (int i = 0; i < 4; i++)
        {
            c.CircleHard(fl[i, 0], fl[i, 1], 4, Ink);
            c.CircleHard(fl[i, 0], fl[i, 1], 2.5f, (i % 2 == 0) ? LemonCandy : Cream);
        }
        return c;
    }

    public static Canvas BuildConfettiStar()
    {
        const int S = 24;
        Canvas c = new Canvas(S, S);
        DrawStarHard(c, 12, 12, 10, 4.5f, Cream, Ink);
        return c;
    }

    private static void DrawStarHard(Canvas c, float cx, float cy, float outerR, float innerR, Rgba fill, Rgba border)
    {
        int x0 = Clamp((int)(cx - outerR - 3), 0, c.W - 1), x1 = Clamp((int)(cx + outerR + 3), 0, c.W - 1);
        int y0 = Clamp((int)(cy - outerR - 3), 0, c.H - 1), y1 = Clamp((int)(cy + outerR + 3), 0, c.H - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx) + Math.PI * 0.5;
                double sec = ((ang % (Math.PI * 2.0 / 5.0)) + Math.PI * 2.0 / 5.0) % (Math.PI * 2.0 / 5.0);
                float rAt = (float)(innerR + (outerR - innerR) * (Math.Abs(sec - Math.PI / 5.0) / (Math.PI / 5.0)));
                if (dist <= rAt + 2.5f) c.Set(x, y, dist <= rAt ? fill : border);
            }
    }

    private static Canvas Transpose(Canvas src)
    {
        Canvas dst = new Canvas(src.H, src.W);
        for (int y = 0; y < src.H; y++)
            for (int x = 0; x < src.W; x++)
                dst.Px[x * dst.W + y] = src.Px[y * src.W + x];
        return dst;
    }

    // =========================================================================
    // CLEAN TRANSPARENT CUTOUT OF ALL 8 CUPCAKE HOUSES (NO BEIGE RECTANGLES!)
    // =========================================================================
    public static Canvas CutoutCupcakeHouse(Rgba[] srcPixels, int srcW, int srcH, int cropX, int cropY, int cropW, int cropH)
    {
        cropX = Clamp(cropX, 0, srcW - 1); cropY = Clamp(cropY, 0, srcH - 1);
        cropW = Clamp(cropW, 1, srcW - cropX); cropH = Clamp(cropH, 1, srcH - cropY);
        int n = cropW * cropH;
        Rgba[] sub = new Rgba[n];
        for (int y = 0; y < cropH; y++)
            for (int x = 0; x < cropW; x++)
                sub[y * cropW + x] = srcPixels[(cropY + y) * srcW + (cropX + x)];

        Rgba bg = EstimateCornerBg(sub, cropW, cropH);
        bool[] isBg = new bool[n];
        Queue<int> q = new Queue<int>();

        bool MatchesBg(Rgba p)
        {
            int lum = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
            if (lum < 118) return false;
            int dr = p.r - bg.r, dg = p.g - bg.g, db = p.b - bg.b;
            int distSq = dr * dr + dg * dg + db * db;
            bool warmTan = (p.r >= p.g - 6) && (p.g >= p.b - 6) && (p.r - p.b >= 16);
            return distSq <= 48 * 48 || (warmTan && distSq <= 72 * 72);
        }

        for (int x = 0; x < cropW; x++)
        {
            int t = x, b = (cropH - 1) * cropW + x;
            if (!isBg[t] && MatchesBg(sub[t])) { isBg[t] = true; q.Enqueue(t); }
            if (!isBg[b] && MatchesBg(sub[b])) { isBg[b] = true; q.Enqueue(b); }
        }
        for (int y = 0; y < cropH; y++)
        {
            int l = y * cropW, r = y * cropW + (cropW - 1);
            if (!isBg[l] && MatchesBg(sub[l])) { isBg[l] = true; q.Enqueue(l); }
            if (!isBg[r] && MatchesBg(sub[r])) { isBg[r] = true; q.Enqueue(r); }
        }

        int[] dxs = { -1, 1, 0, 0 }, dys = { 0, 0, -1, 1 };
        while (q.Count > 0)
        {
            int cur = q.Dequeue(); int cx = cur % cropW, cy = cur / cropW;
            for (int k = 0; k < 4; k++)
            {
                int nx = cx + dxs[k], ny = cy + dys[k];
                if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH) continue;
                int ni = ny * cropW + nx;
                if (!isBg[ni] && MatchesBg(sub[ni])) { isBg[ni] = true; q.Enqueue(ni); }
            }
        }

        int[] labels = new int[n];
        int bestLabel = 0, bestCount = 0, nextLabel = 1;
        for (int i = 0; i < n; i++)
        {
            if (isBg[i] || labels[i] != 0) continue;
            int count = 0; q.Clear(); labels[i] = nextLabel; q.Enqueue(i);
            while (q.Count > 0)
            {
                int cur = q.Dequeue(); count++; int cx = cur % cropW, cy = cur / cropW;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dxs[k], ny = cy + dys[k];
                    if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH) continue;
                    int ni = ny * cropW + nx;
                    if (!isBg[ni] && labels[ni] == 0) { labels[ni] = nextLabel; q.Enqueue(ni); }
                }
            }
            if (count > bestCount) { bestCount = count; bestLabel = nextLabel; }
            nextLabel++;
        }

        int minX = cropW, minY = cropH, maxX = 0, maxY = 0;
        bool[] keep = new bool[n];
        for (int y = 0; y < cropH; y++)
            for (int x = 0; x < cropW; x++)
            {
                int i = y * cropW + x;
                if (labels[i] == bestLabel)
                {
                    keep[i] = true;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }

        if (maxX <= minX || maxY <= minY) return new Canvas(64, 64);

        int pad = 12;
        int outW = (maxX - minX + 1) + pad * 2, outH = (maxY - minY + 1) + pad * 2;
        Canvas outC = new Canvas(outW, outH);
        outC.EllipseHard(outW * 0.5f, pad + 5f, (maxX - minX) * 0.42f, 12f, new Rgba(64, 34, 72, 72));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                if (keep[y * cropW + x]) outC.Set((x - minX) + pad, (y - minY) + pad, sub[y * cropW + x]);

        // Downscale to crisp 192px pixel-art density matching our PPU=32 Tilemap!
        if (outW > 192 || outH > 192)
        {
            float sc = 192f / Math.Max(outW, outH);
            return DownscaleNearest(outC, Math.Max(32, (int)(outW * sc)), Math.Max(32, (int)(outH * sc)));
        }
        return outC;
    }

    private static Rgba EstimateCornerBg(Rgba[] sub, int w, int h)
    {
        int r = 0, g = 0, b = 0, cnt = 0;
        int[,] pts = { { 4, 4 }, { w - 5, 4 }, { 4, h - 5 }, { w - 5, h - 5 }, { w / 2, 4 } };
        for (int i = 0; i < pts.GetLength(0); i++)
        {
            Rgba p = sub[Clamp(pts[i, 1], 0, h - 1) * w + Clamp(pts[i, 0], 0, w - 1)];
            r += p.r; g += p.g; b += p.b; cnt++;
        }
        return new Rgba(r / cnt, g / cnt, b / cnt, 255);
    }

    private static Canvas DownscaleNearest(Canvas src, int nw, int nh)
    {
        Canvas dst = new Canvas(nw, nh);
        for (int y = 0; y < nh; y++)
        {
            int sy = Clamp((int)((y + 0.5f) * src.H / nh), 0, src.H - 1);
            for (int x = 0; x < nw; x++)
            {
                int sx = Clamp((int)((x + 0.5f) * src.W / nw), 0, src.W - 1);
                dst.Px[y * nw + x] = src.Px[sy * src.W + sx];
            }
        }
        return dst;
    }

#if UNITY_EDITOR
    public static void GenerateAndImportAll(string outAssetFolder)
    {
        string absOut = Path.Combine(Application.dataPath, outAssetFolder.Replace("Assets/", "").TrimStart('/'));
        Directory.CreateDirectory(absOut);

        GenerateAllProcedural((name, canvas) => SaveCanvasUnity(Path.Combine(absOut, name), canvas));

        string housesSrcDir = Path.Combine(Application.dataPath, "a casa de mosntros");
        ExtractSixHousesUnity(Path.Combine(housesSrcDir, "casa diversas 22.jpeg"), absOut);
        ExtractSingleHouseUnity(Path.Combine(housesSrcDir, "House_in_nude_background_2K_202607221539.jpeg"), Path.Combine(absOut, "CandyHouse_RocketBlue.png"));
        if (Directory.Exists(housesSrcDir))
        {
            string[] botFiles = Directory.GetFiles(housesSrcDir, "Marshmallow_Botanical*.jpeg");
            if (botFiles.Length > 0)
                ExtractSingleHouseUnity(botFiles[0], Path.Combine(absOut, "CandyHouse_Botanical.png"));
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (string file in Directory.GetFiles(absOut, "*.png"))
        {
            string fileName = Path.GetFileName(file);
            TextureWrapMode wrap = (fileName == "CandyMeadowTile.png" || fileName == "CandyRoadH.png" || fileName == "CandyRoadV.png")
                ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ConfigureImporter(outAssetFolder + "/" + fileName, 32f, wrap, 1024);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ExtractSixHousesUnity(string jpegPath, string outDir)
    {
        if (!File.Exists(jpegPath)) return;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(jpegPath))) { UnityEngine.Object.DestroyImmediate(tex); return; }
        int w = tex.width, h = tex.height;
        Color32[] cols = tex.GetPixels32();
        Rgba[] px = new Rgba[cols.Length];
        for (int i = 0; i < cols.Length; i++) px[i] = new Rgba(cols[i].r, cols[i].g, cols[i].b, cols[i].a);
        UnityEngine.Object.DestroyImmediate(tex);

        string[] topNames = { "CandyHouse_Strawberry.png", "CandyHouse_ChocoMint.png", "CandyHouse_Rainbow.png" };
        string[] botNames = { "CandyHouse_Nautical.png", "CandyHouse_Galaxy.png", "CandyHouse_Matcha.png" };
        int cellW = w / 3, topY = (int)(h * 0.49f), topH = (int)(h * 0.44f), botY = (int)(h * 0.03f), botH = (int)(h * 0.46f);
        for (int i = 0; i < 3; i++)
        {
            SaveCanvasUnity(Path.Combine(outDir, topNames[i]), CutoutCupcakeHouse(px, w, h, i * cellW, topY, cellW, topH));
            SaveCanvasUnity(Path.Combine(outDir, botNames[i]), CutoutCupcakeHouse(px, w, h, i * cellW, botY, cellW, botH));
        }
    }

    private static void ExtractSingleHouseUnity(string jpegPath, string outPngPath)
    {
        if (!File.Exists(jpegPath)) return;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(jpegPath))) { UnityEngine.Object.DestroyImmediate(tex); return; }
        int w = tex.width, h = tex.height;
        Color32[] cols = tex.GetPixels32();
        Rgba[] px = new Rgba[cols.Length];
        for (int i = 0; i < cols.Length; i++) px[i] = new Rgba(cols[i].r, cols[i].g, cols[i].b, cols[i].a);
        UnityEngine.Object.DestroyImmediate(tex);
        SaveCanvasUnity(outPngPath, CutoutCupcakeHouse(px, w, h, (int)(w * 0.14f), (int)(h * 0.03f), (int)(w * 0.72f), (int)(h * 0.94f)));
    }

    private static void SaveCanvasUnity(string path, Canvas canvas)
    {
        Texture2D tex = new Texture2D(canvas.W, canvas.H, TextureFormat.RGBA32, false);
        Color32[] cols = new Color32[canvas.Px.Length];
        for (int i = 0; i < cols.Length; i++) { Rgba p = canvas.Px[i]; cols[i] = new Color32(p.r, p.g, p.b, p.a); }
        tex.SetPixels32(cols);
        tex.Apply(false, false);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void ConfigureImporter(string assetPath, float ppu, TextureWrapMode wrap, int maxSize)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
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
}
