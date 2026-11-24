using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// High-resolution illustrated cartoon Candy World art generator & house background
/// remover for "Girl Power - Sorvete". Generates seamless candy meadows, biscuit roads
/// with bubblegum-pink curbs, delivery star plaza, berry pond, giant lollipops, candy canes,
/// streetlamps, and cleanly cuts out the user's cupcake houses into transparent PNGs.
/// </summary>
public static class CandyWorldArtGenerator
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

        public void Blend(int x, int y, Rgba c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H || c.a == 0) return;
            int idx = y * W + x;
            if (c.a == 255)
            {
                Px[idx] = c;
                return;
            }
            Rgba d = Px[idx];
            float sa = c.a / 255f;
            float da = d.a / 255f;
            float oa = sa + da * (1f - sa);
            if (oa <= 0.0001f) return;
            Px[idx] = new Rgba(
                (int)((c.r * sa + d.r * da * (1f - sa)) / oa),
                (int)((c.g * sa + d.g * da * (1f - sa)) / oa),
                (int)((c.b * sa + d.b * da * (1f - sa)) / oa),
                (int)(oa * 255f));
        }

        public void BlendWrap(int x, int y, Rgba c)
        {
            int wx = ((x % W) + W) % W;
            int wy = ((y % H) + H) % H;
            Blend(wx, wy, c);
        }

        public void Rect(int x, int y, int w, int h, Rgba c)
        {
            int x0 = Clamp(x, 0, W), y0 = Clamp(y, 0, H);
            int x1 = Clamp(x + w, 0, W), y1 = Clamp(y + h, 0, H);
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++)
                    Blend(px, py, c);
        }

        public void CircleAA(float cx, float cy, float r, Rgba c, bool wrap = false)
        {
            int x0 = (int)Math.Floor(cx - r - 2);
            int x1 = (int)Math.Ceiling(cx + r + 2);
            int y0 = (int)Math.Floor(cy - r - 2);
            int y1 = (int)Math.Ceiling(cy + r + 2);
            if (!wrap)
            {
                x0 = Clamp(x0, 0, W - 1); x1 = Clamp(x1, 0, W - 1);
                y0 = Clamp(y0, 0, H - 1); y1 = Clamp(y1, 0, H - 1);
            }
            for (int y = y0; y <= y1; y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float cov = Clamp01(r + 0.6f - dist);
                    if (cov <= 0f) continue;
                    Rgba sc = new Rgba(c.r, c.g, c.b, (int)(c.a * cov));
                    if (wrap) BlendWrap(x, y, sc); else Blend(x, y, sc);
                }
            }
        }

        public void EllipseAA(float cx, float cy, float rx, float ry, Rgba c, bool wrap = false)
        {
            int x0 = (int)Math.Floor(cx - rx - 2);
            int x1 = (int)Math.Ceiling(cx + rx + 2);
            int y0 = (int)Math.Floor(cy - ry - 2);
            int y1 = (int)Math.Ceiling(cy + ry + 2);
            if (!wrap)
            {
                x0 = Clamp(x0, 0, W - 1); x1 = Clamp(x1, 0, W - 1);
                y0 = Clamp(y0, 0, H - 1); y1 = Clamp(y1, 0, H - 1);
            }
            float minR = Math.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f - cy) / ry;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float cov = Clamp01((1f - d) * minR + 0.6f);
                    if (cov <= 0f) continue;
                    Rgba sc = new Rgba(c.r, c.g, c.b, (int)(c.a * cov));
                    if (wrap) BlendWrap(x, y, sc); else Blend(x, y, sc);
                }
            }
        }

        public void CapsuleAA(float x0, float y0, float x1, float y1, float r, Rgba c, bool wrap = false)
        {
            int bx0 = (int)Math.Floor(Math.Min(x0, x1) - r - 2);
            int bx1 = (int)Math.Ceiling(Math.Max(x0, x1) + r + 2);
            int by0 = (int)Math.Floor(Math.Min(y0, y1) - r - 2);
            int by1 = (int)Math.Ceiling(Math.Max(y0, y1) + r + 2);
            if (!wrap)
            {
                bx0 = Clamp(bx0, 0, W - 1); bx1 = Clamp(bx1, 0, W - 1);
                by0 = Clamp(by0, 0, H - 1); by1 = Clamp(by1, 0, H - 1);
            }
            float vx = x1 - x0, vy = y1 - y0;
            float lenSq = Math.Max(0.0001f, vx * vx + vy * vy);
            for (int y = by0; y <= by1; y++)
            {
                for (int x = bx0; x <= bx1; x++)
                {
                    float t = Clamp01(((x + 0.5f - x0) * vx + (y + 0.5f - y0) * vy) / lenSq);
                    float px = x0 + vx * t, py = y0 + vy * t;
                    float d = (float)Math.Sqrt((x + 0.5f - px) * (x + 0.5f - px) + (y + 0.5f - py) * (y + 0.5f - py));
                    float cov = Clamp01(r + 0.6f - d);
                    if (cov <= 0f) continue;
                    Rgba sc = new Rgba(c.r, c.g, c.b, (int)(c.a * cov));
                    if (wrap) BlendWrap(x, y, sc); else Blend(x, y, sc);
                }
            }
        }

        public void RoundedRectAA(float cx, float cy, float halfW, float halfH, float radius, Rgba fill, Rgba border, float borderW)
        {
            int x0 = Clamp((int)Math.Floor(cx - halfW - 2), 0, W - 1);
            int x1 = Clamp((int)Math.Ceiling(cx + halfW + 2), 0, W - 1);
            int y0 = Clamp((int)Math.Floor(cy - halfH - 2), 0, H - 1);
            int y1 = Clamp((int)Math.Ceiling(cy + halfH + 2), 0, H - 1);
            float r = Math.Min(radius, Math.Min(halfW, halfH));
            for (int y = y0; y <= y1; y++)
            {
                float dy = Math.Abs(y + 0.5f - cy) - (halfH - r);
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Math.Abs(x + 0.5f - cx) - (halfW - r);
                    float outDist = (float)Math.Sqrt(Math.Max(dx, 0f) * Math.Max(dx, 0f) + Math.Max(dy, 0f) * Math.Max(dy, 0f)) + Math.Min(Math.Max(dx, dy), 0f) - r;
                    float cov = Clamp01(0.6f - outDist);
                    if (cov <= 0f) continue;
                    Rgba col = (borderW > 0f && outDist > -borderW) ? border : fill;
                    Blend(x, y, new Rgba(col.r, col.g, col.b, (int)(col.a * cov)));
                }
            }
        }
    }

    // Vibrant Pastel Candy World Palette (harmonized with Girl Power Title Screen!)
    private static readonly Rgba Ink = new Rgba(64, 34, 68);
    private static readonly Rgba MintBase = new Rgba(158, 226, 178);
    private static readonly Rgba MintLight = new Rgba(184, 240, 194);
    private static readonly Rgba MintDeep = new Rgba(124, 202, 152);
    private static readonly Rgba BiscuitBase = new Rgba(252, 224, 176);
    private static readonly Rgba BiscuitLight = new Rgba(255, 238, 202);
    private static readonly Rgba BiscuitShade = new Rgba(234, 194, 140);
    private static readonly Rgba PinkCandy = new Rgba(255, 128, 192);
    private static readonly Rgba PinkLight = new Rgba(255, 178, 218);
    private static readonly Rgba PinkDeep = new Rgba(218, 78, 154);
    private static readonly Rgba Lavender = new Rgba(196, 154, 246);
    private static readonly Rgba AquaCandy = new Rgba(118, 226, 236);
    private static readonly Rgba LemonCandy = new Rgba(255, 232, 118);
    private static readonly Rgba Cream = new Rgba(255, 250, 238);
    private static readonly Rgba Choco = new Rgba(122, 74, 58);

    private static readonly Rgba[] SprinkleColors =
    {
        new Rgba(255, 112, 176),
        new Rgba(112, 214, 248),
        new Rgba(255, 218, 86),
        new Rgba(186, 134, 250),
        new Rgba(126, 234, 168),
        new Rgba(255, 154, 96)
    };

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

    public static void GenerateAllProcedural(Action<string, Canvas> writer)
    {
        writer("CandyMeadowTile.png", BuildCandyMeadowTile());
        writer("CandyRoadH.png", BuildCandyRoad(false));
        writer("CandyRoadV.png", BuildCandyRoad(true));
        writer("CandyRoadCross.png", BuildCandyRoadCross());
        writer("CandyRoadCorner.png", BuildCandyRoadCorner());
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
    }

    // =========================================================================
    // CLEAN HOUSE CUTOUT ALGORITHM (removes peach/tan JPEG bg + stray doodles!)
    // =========================================================================
    public static Canvas CutoutCupcakeHouse(Rgba[] srcPixels, int srcW, int srcH, int cropX, int cropY, int cropW, int cropH)
    {
        cropX = Clamp(cropX, 0, srcW - 1);
        cropY = Clamp(cropY, 0, srcH - 1);
        cropW = Clamp(cropW, 1, srcW - cropX);
        cropH = Clamp(cropH, 1, srcH - cropY);

        int n = cropW * cropH;
        Rgba[] sub = new Rgba[n];
        for (int y = 0; y < cropH; y++)
            for (int x = 0; x < cropW; x++)
                sub[y * cropW + x] = srcPixels[(cropY + y) * srcW + (cropX + x)];

        // Estimate background peach/tan color from corners of the crop
        Rgba bg = EstimateCornerBg(sub, cropW, cropH);

        bool[] isBg = new bool[n];
        Queue<int> q = new Queue<int>();

        bool MatchesBg(Rgba p)
        {
            // Dark cartoon outline ALWAYS stops the flood fill!
            int lum = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
            if (lum < 120) return false;
            int dr = p.r - bg.r;
            int dg = p.g - bg.g;
            int db = p.b - bg.b;
            int distSq = dr * dr + dg * dg + db * db;
            // Warm peach/tan hues close to background
            bool warmTan = (p.r >= p.g - 6) && (p.g >= p.b - 6) && (p.r - p.b >= 18);
            return distSq <= 48 * 48 || (warmTan && distSq <= 70 * 70);
        }

        // Seed border pixels
        for (int x = 0; x < cropW; x++)
        {
            int top = x;
            int bot = (cropH - 1) * cropW + x;
            if (!isBg[top] && MatchesBg(sub[top])) { isBg[top] = true; q.Enqueue(top); }
            if (!isBg[bot] && MatchesBg(sub[bot])) { isBg[bot] = true; q.Enqueue(bot); }
        }
        for (int y = 0; y < cropH; y++)
        {
            int left = y * cropW;
            int right = y * cropW + (cropW - 1);
            if (!isBg[left] && MatchesBg(sub[left])) { isBg[left] = true; q.Enqueue(left); }
            if (!isBg[right] && MatchesBg(sub[right])) { isBg[right] = true; q.Enqueue(right); }
        }

        int[] dxs = { -1, 1, 0, 0 };
        int[] dys = { 0, 0, -1, 1 };
        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            int cx = cur % cropW, cy = cur / cropW;
            for (int k = 0; k < 4; k++)
            {
                int nx = cx + dxs[k], ny = cy + dys[k];
                if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH) continue;
                int ni = ny * cropW + nx;
                if (isBg[ni]) continue;
                if (MatchesBg(sub[ni]))
                {
                    isBg[ni] = true;
                    q.Enqueue(ni);
                }
            }
        }

        // Keep ONLY the largest connected foreground component (erases all stray floating sprinkles & doodles!)
        int[] labels = new int[n];
        int bestLabel = 0, bestCount = 0, nextLabel = 1;
        for (int i = 0; i < n; i++)
        {
            if (isBg[i] || labels[i] != 0) continue;
            int count = 0;
            q.Clear();
            labels[i] = nextLabel;
            q.Enqueue(i);
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                count++;
                int cx = cur % cropW, cy = cur / cropW;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dxs[k], ny = cy + dys[k];
                    if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH) continue;
                    int ni = ny * cropW + nx;
                    if (isBg[ni] || labels[ni] != 0) continue;
                    labels[ni] = nextLabel;
                    q.Enqueue(ni);
                }
            }
            if (count > bestCount)
            {
                bestCount = count;
                bestLabel = nextLabel;
            }
            nextLabel++;
        }

        // Compute tight bounding box of the house + remove fringe tan halos
        int minX = cropW, minY = cropH, maxX = 0, maxY = 0;
        bool[] keep = new bool[n];
        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                int i = y * cropW + x;
                if (labels[i] == bestLabel)
                {
                    keep[i] = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX <= minX || maxY <= minY) return new Canvas(64, 64);

        // Trim into padded square-ish canvas with a subtle bottom shadow
        int pad = 16;
        int outW = (maxX - minX + 1) + pad * 2;
        int outH = (maxY - minY + 1) + pad * 2;
        Canvas outCanvas = new Canvas(outW, outH);

        // Soft purple-plum shadow beneath house base
        outCanvas.EllipseAA(outW * 0.5f, pad + 6f, (maxX - minX) * 0.42f, 14f, new Rgba(62, 34, 72, 72));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int i = y * cropW + x;
                if (!keep[i]) continue;
                Rgba p = sub[i];
                // Soften 1px outer fringe if it leans tan
                int bgNeighbors = 0;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dxs[k], ny = y + dys[k];
                    if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH || !keep[ny * cropW + nx])
                        bgNeighbors++;
                }
                int alpha = 255;
                if (bgNeighbors > 0)
                {
                    int lum = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                    if (lum > 150) alpha = bgNeighbors >= 2 ? 90 : 175;
                }
                outCanvas.Blend((x - minX) + pad, (y - minY) + pad, new Rgba(p.r, p.g, p.b, alpha));
            }
        }

        // Downscale neatly to max 512px so Unity memory stays fast & crisp!
        if (outW > 512 || outH > 512)
        {
            float scale = 512f / Math.Max(outW, outH);
            int nw = Math.Max(32, (int)(outW * scale));
            int nh = Math.Max(32, (int)(outH * scale));
            return DownscaleBilinear(outCanvas, nw, nh);
        }
        return outCanvas;
    }

    private static Rgba EstimateCornerBg(Rgba[] sub, int w, int h)
    {
        int r = 0, g = 0, b = 0, cnt = 0;
        int[,] pts = { { 4, 4 }, { w - 5, 4 }, { 4, h - 5 }, { w - 5, h - 5 }, { w / 2, 4 }, { 4, h / 2 }, { w - 5, h / 2 } };
        for (int i = 0; i < pts.GetLength(0); i++)
        {
            int x = Clamp(pts[i, 0], 0, w - 1);
            int y = Clamp(pts[i, 1], 0, h - 1);
            Rgba p = sub[y * w + x];
            r += p.r; g += p.g; b += p.b; cnt++;
        }
        return new Rgba(r / cnt, g / cnt, b / cnt, 255);
    }

    private static Canvas DownscaleBilinear(Canvas src, int nw, int nh)
    {
        Canvas dst = new Canvas(nw, nh);
        for (int y = 0; y < nh; y++)
        {
            float sy = (y + 0.5f) * src.H / nh - 0.5f;
            int y0 = Clamp((int)Math.Floor(sy), 0, src.H - 1);
            int y1 = Clamp(y0 + 1, 0, src.H - 1);
            float fy = Clamp01(sy - y0);
            for (int x = 0; x < nw; x++)
            {
                float sx = (x + 0.5f) * src.W / nw - 0.5f;
                int x0 = Clamp((int)Math.Floor(sx), 0, src.W - 1);
                int x1 = Clamp(x0 + 1, 0, src.W - 1);
                float fx = Clamp01(sx - x0);
                Rgba c00 = src.Px[y0 * src.W + x0];
                Rgba c10 = src.Px[y0 * src.W + x1];
                Rgba c01 = src.Px[y1 * src.W + x0];
                Rgba c11 = src.Px[y1 * src.W + x1];
                Rgba top = Lerp(c00, c10, fx);
                Rgba bot = Lerp(c01, c11, fx);
                dst.Px[y * nw + x] = Lerp(top, bot, fy);
            }
        }
        return dst;
    }

    // =========================================================================
    // 1. SEAMLESS CANDY MEADOW TILE (512 x 512)
    // =========================================================================
    public static Canvas BuildCandyMeadowTile()
    {
        const int S = 512;
        Canvas c = new Canvas(S, S);

        // Soft seamless undulating mint-pistachio meadow base
        for (int y = 0; y < S; y++)
        {
            double ay = y * (2.0 * Math.PI / S);
            for (int x = 0; x < S; x++)
            {
                double ax = x * (2.0 * Math.PI / S);
                float n = (float)(
                    0.50 +
                    0.22 * Math.Sin(ax * 2.0 + Math.Cos(ay * 2.0)) +
                    0.16 * Math.Cos(ay * 3.0 - Math.Sin(ax * 2.0)) +
                    0.12 * Math.Sin((ax + ay) * 4.0));
                Rgba col = n > 0.55f ? Lerp(MintBase, MintLight, (n - 0.55f) * 2.2f) : Lerp(MintDeep, MintBase, n / 0.55f);
                c.Px[y * S + x] = col;
            }
        }

        // Soft sunlit pastel grass patches (wrapped seamlessly)
        for (int i = 0; i < 18; i++)
        {
            float cx = Math.Abs(Hash(i, 11)) % S;
            float cy = Math.Abs(Hash(i, 23)) % S;
            float rx = 28 + (Math.Abs(Hash(i, 31)) % 34);
            float ry = 20 + (Math.Abs(Hash(i, 37)) % 24);
            Rgba patch = (i % 2 == 0) ? new Rgba(196, 246, 206, 95) : new Rgba(136, 212, 162, 75);
            c.EllipseAA(cx, cy, rx, ry, patch, true);
        }

        // Little cute cartoon grass blades
        for (int i = 0; i < 65; i++)
        {
            float gx = Math.Abs(Hash(i, 53)) % S;
            float gy = Math.Abs(Hash(i, 59)) % S;
            Rgba blade = (i % 2 == 0) ? new Rgba(94, 174, 128, 170) : new Rgba(110, 190, 142, 165);
            c.CapsuleAA(gx, gy, gx - 4, gy + 9, 1.5f, blade, true);
            c.CapsuleAA(gx, gy, gx + 1, gy + 11, 1.5f, blade, true);
            c.CapsuleAA(gx, gy, gx + 5, gy + 8, 1.4f, blade, true);
        }

        // Adorable tiny pastel sugar-blossom flowers
        Rgba[] petalColors = { PinkLight, Cream, LemonCandy, Lavender };
        for (int i = 0; i < 22; i++)
        {
            float fx = Math.Abs(Hash(i, 71)) % S;
            float fy = Math.Abs(Hash(i, 79)) % S;
            Rgba petal = petalColors[i % petalColors.Length];
            for (int p = 0; p < 5; p++)
            {
                double ang = p * Math.PI * 2.0 / 5.0;
                c.CircleAA(fx + (float)Math.Cos(ang) * 4.5f, fy + (float)Math.Sin(ang) * 4.5f, 3.2f, petal, true);
            }
            c.CircleAA(fx, fy, 2.5f, LemonCandy, true);
        }

        // Tiny scattered colorful candy sprinkles in the meadow
        for (int i = 0; i < 34; i++)
        {
            float sx = Math.Abs(Hash(i, 97)) % S;
            float sy = Math.Abs(Hash(i, 103)) % S;
            double ang = (Math.Abs(Hash(i, 107)) % 314) / 100.0;
            float dx = (float)Math.Cos(ang) * 4.5f;
            float dy = (float)Math.Sin(ang) * 4.5f;
            Rgba sp = SprinkleColors[i % SprinkleColors.Length];
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.1f, new Rgba(sp.r, sp.g, sp.b, 200), true);
        }

        return c;
    }

    // =========================================================================
    // 2. CARTOON CANDY BISCUIT ROADS (256 x 256 seamless horizontal/vertical/cross/corner)
    // =========================================================================
    public static Canvas BuildCandyRoad(bool vertical)
    {
        const int S = 256;
        Canvas c = new Canvas(S, S);

        // Build horizontal first, then transpose if vertical
        // Outer drop shadow on meadow (y 0..10 and 246..256)
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                int dy = Math.Min(y, S - 1 - y);
                if (dy < 6)
                {
                    int a = (int)(75 * (dy / 6f));
                    c.Blend(x, y, new Rgba(64, 34, 68, a));
                }
                else if (dy < 10)
                {
                    c.Blend(x, y, Ink);
                }
                else if (dy < 30)
                {
                    // Bubblegum-pink & lavender candy curb stones!
                    int stone = (x / 32) % 2;
                    Rgba baseCol = stone == 0 ? PinkCandy : Lavender;
                    Rgba hiCol = stone == 0 ? PinkLight : new Rgba(224, 194, 255);
                    bool seam = (x % 32) < 3;
                    if (seam) c.Blend(x, y, Ink);
                    else if (dy > 25) c.Blend(x, y, hiCol);
                    else c.Blend(x, y, baseCol);
                }
                else if (dy < 34)
                {
                    c.Blend(x, y, Ink);
                }
                else
                {
                    // Warm golden-vanilla biscuit road with subtle inner shade near curbs
                    float edgeFactor = Clamp01((dy - 34) / 22f);
                    Rgba roadCol = Lerp(BiscuitShade, BiscuitBase, edgeFactor);
                    // Gentle glossy center sheen
                    float centerFactor = 1f - Math.Abs(y - 128f) / 94f;
                    roadCol = Lerp(roadCol, BiscuitLight, Clamp01(centerFactor * 0.45f));
                    c.Blend(x, y, roadCol);
                }
            }
        }

        // Cream marshmallow center lane dashes
        c.RoundedRectAA(64, 128, 36, 7, 6, Cream, new Rgba(214, 174, 134), 2f);
        c.RoundedRectAA(192, 128, 36, 7, 6, Cream, new Rgba(214, 174, 134), 2f);

        // Cute colorful sugar sprinkles scattered along the biscuit road shoulders!
        for (int i = 0; i < 22; i++)
        {
            float sx = 12 + (Math.Abs(Hash(i, 131)) % 232);
            float sy = (i % 2 == 0) ? (44 + Math.Abs(Hash(i, 137)) % 36) : (176 + Math.Abs(Hash(i, 139)) % 36);
            double ang = (Math.Abs(Hash(i, 149)) % 314) / 100.0;
            float dx = (float)Math.Cos(ang) * 5.5f;
            float dy = (float)Math.Sin(ang) * 5.5f;
            Rgba sp = SprinkleColors[i % SprinkleColors.Length];
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 3.0f, Ink);
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.0f, sp);
        }

        return vertical ? Transpose(c) : c;
    }

    public static Canvas BuildCandyRoadCross()
    {
        const int S = 256;
        Canvas c = new Canvas(S, S);

        // Full warm biscuit intersection base
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - 128f) * (x - 128f) + (y - 128f) * (y - 128f)) / 150f;
                c.Px[y * S + x] = Lerp(BiscuitLight, BiscuitBase, Clamp01(d));
            }
        }

        // Rounded candy-stone corner curbs in all 4 corners
        int[,] corners = { { 0, 0 }, { S, 0 }, { 0, S }, { S, S } };
        for (int k = 0; k < 4; k++)
        {
            float cx = corners[k, 0], cy = corners[k, 1];
            c.CircleAA(cx, cy, 34, Ink);
            c.CircleAA(cx, cy, 30, PinkCandy);
            c.CircleAA(cx, cy, 24, PinkLight);
            c.CircleAA(cx, cy, 10, Ink);
            c.CircleAA(cx, cy, 6, new Rgba(0, 0, 0, 0));
        }

        // Cute marshmallow crosswalk bars on all 4 entrances
        for (int i = 0; i < 4; i++)
        {
            float off = 66 + i * 41;
            c.RoundedRectAA(off, 18, 12, 10, 5, Cream, BiscuitShade, 2f);
            c.RoundedRectAA(off, S - 18, 12, 10, 5, Cream, BiscuitShade, 2f);
            c.RoundedRectAA(18, off, 10, 12, 5, Cream, BiscuitShade, 2f);
            c.RoundedRectAA(S - 18, off, 10, 12, 5, Cream, BiscuitShade, 2f);
        }

        // Center star medallion inlay in the intersection
        c.CircleAA(128, 128, 32, new Rgba(255, 198, 224, 140));
        c.CircleAA(128, 128, 22, new Rgba(255, 244, 212, 160));
        DrawStar(c, 128, 128, 16, 7, PinkCandy, Ink);

        return c;
    }

    public static Canvas BuildCandyRoadCorner()
    {
        // Connects East (+X) and North (+Y) cleanly; rotate 90/180/270 for the other 3 corners!
        const int S = 256;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                // Distance from inner corner (S, S)
                float d = (float)Math.Sqrt((x - S) * (x - S) + (y - S) * (y - S));
                if (d < 30f || d > S - 6f)
                {
                    if (d >= S - 6f && d <= S) c.Blend(x, y, new Rgba(64, 34, 68, (int)(75 * (1f - (d - (S - 6f)) / 6f))));
                    continue;
                }
                if (d < 34f || d > S - 10f) { c.Blend(x, y, Ink); continue; }
                if (d > S - 30f || d < 54f)
                {
                    double ang = Math.Atan2(S - y, S - x);
                    int seg = (int)(ang * 10.0);
                    Rgba col = (seg % 2 == 0) ? PinkCandy : Lavender;
                    c.Blend(x, y, col);
                    continue;
                }
                if (d > S - 34f || d < 58f) { c.Blend(x, y, Ink); continue; }
                c.Blend(x, y, BiscuitBase);
            }
        }
        // Curved marshmallow dashes
        for (int i = 1; i <= 3; i++)
        {
            double ang = (i / 4.0) * (Math.PI * 0.5);
            float mx = S - (float)Math.Cos(ang) * 128f;
            float my = S - (float)Math.Sin(ang) * 128f;
            c.CircleAA(mx, my, 7.5f, BiscuitShade);
            c.CircleAA(mx, my, 5.5f, Cream);
        }
        return c;
    }

    // =========================================================================
    // 3. SHOW-STOPPING DELIVERY STAR PLAZA (512 x 512)
    // =========================================================================
    public static Canvas BuildDeliveryPlaza()
    {
        const int S = 512;
        Canvas c = new Canvas(S, S);
        float mid = S * 0.5f;

        // Soft outer shadow
        c.CircleAA(mid, mid - 6, 242, new Rgba(64, 34, 68, 70));

        // Scalloped marshmallow-stone outer wreath
        for (int i = 0; i < 20; i++)
        {
            double ang = i * Math.PI * 2.0 / 20.0;
            float sx = mid + (float)Math.Cos(ang) * 212f;
            float sy = mid + (float)Math.Sin(ang) * 212f;
            c.CircleAA(sx, sy, 30, Ink);
            c.CircleAA(sx, sy, 26, i % 2 == 0 ? PinkLight : Cream);
            c.CircleAA(sx - 4, sy + 5, 10, new Rgba(255, 255, 255, 120));
        }

        // Main frosting plaza floor
        c.CircleAA(mid, mid, 206, Ink);
        c.CircleAA(mid, mid, 200, new Rgba(255, 198, 226));
        c.CircleAA(mid, mid, 178, Cream);
        c.CircleAA(mid, mid, 152, PinkCandy);
        c.CircleAA(mid, mid, 134, new Rgba(255, 168, 214));

        // Sprinkles around the cream ring
        for (int i = 0; i < 28; i++)
        {
            double ang = i * Math.PI * 2.0 / 28.0;
            float r = 165f + ((i % 2) * 6f - 3f);
            float sx = mid + (float)Math.Cos(ang) * r;
            float sy = mid + (float)Math.Sin(ang) * r;
            float dx = (float)Math.Cos(ang + 0.8) * 6f;
            float dy = (float)Math.Sin(ang + 0.8) * 6f;
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 3.8f, Ink);
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.5f, SprinkleColors[i % SprinkleColors.Length]);
        }

        // Big golden glowing star in the center!
        DrawStar(c, mid, mid, 92, 42, LemonCandy, Ink);
        DrawStar(c, mid, mid, 68, 30, Cream, new Rgba(255, 210, 96));

        return c;
    }

    // =========================================================================
    // 4. SPARKLING BERRY-SODA POND (512 x 320)
    // =========================================================================
    public static Canvas BuildBerryPond()
    {
        const int W = 512, H = 320;
        Canvas c = new Canvas(W, H);

        // Creamy frosting shoreline bank
        c.EllipseAA(256, 156, 236, 136, new Rgba(64, 34, 68, 65));
        c.EllipseAA(256, 160, 232, 134, Ink);
        c.EllipseAA(256, 160, 225, 128, Cream);
        c.EllipseAA(170, 185, 126, 92, Cream);
        c.EllipseAA(345, 145, 128, 94, Cream);

        // Sparkling turquoise berry-soda water!
        c.EllipseAA(256, 160, 206, 110, Ink);
        c.EllipseAA(256, 160, 200, 105, new Rgba(88, 206, 238));
        c.EllipseAA(256, 152, 184, 92, AquaCandy);
        c.EllipseAA(240, 146, 140, 68, new Rgba(158, 242, 248));

        // Glossy soda bubbles & water ripples
        c.CapsuleAA(130, 185, 195, 185, 4f, new Rgba(255, 255, 255, 195));
        c.CapsuleAA(290, 125, 368, 125, 4f, new Rgba(255, 255, 255, 195));
        c.CapsuleAA(210, 158, 275, 158, 3.5f, new Rgba(255, 255, 255, 175));
        c.CircleAA(162, 132, 10, new Rgba(255, 255, 255, 170));
        c.CircleAA(338, 186, 8, new Rgba(255, 255, 255, 170));

        // Cute floating sugar lily pads with pink candy lotus blooms!
        DrawLilyPad(c, 155, 152, 24);
        DrawLilyPad(c, 348, 166, 22);
        DrawLilyPad(c, 265, 205, 18);

        return c;
    }

    private static void DrawLilyPad(Canvas c, float cx, float cy, float r)
    {
        c.CircleAA(cx, cy, r + 2.5f, Ink);
        c.CircleAA(cx, cy, r, new Rgba(116, 214, 146));
        c.CircleAA(cx - 3, cy + 3, r * 0.65f, MintLight);
        // Cute pink lotus blossom on lily pad
        for (int i = 0; i < 5; i++)
        {
            double a = i * Math.PI * 2.0 / 5.0;
            c.CircleAA(cx + (float)Math.Cos(a) * 6f, cy + (float)Math.Sin(a) * 6f, 5.5f, PinkCandy);
        }
        c.CircleAA(cx, cy, 4.5f, LemonCandy);
    }

    // =========================================================================
    // 5. ICONIC CANDY PROPS: GIANT LOLLIPOPS, CANDY CANES, LAMPS, FLOWERBEDS, MARSHMALLOWS
    // =========================================================================
    public static Canvas BuildGiantLollipop(bool rainbowPink)
    {
        const int W = 192, H = 256;
        Canvas c = new Canvas(W, H);

        // Ground shadow
        c.EllipseAA(96, 22, 42, 14, new Rgba(64, 34, 68, 75));

        // Glossy white lollipop stick
        c.CapsuleAA(96, 24, 96, 134, 11f, Ink);
        c.CapsuleAA(96, 24, 96, 134, 7.5f, Cream);
        c.CapsuleAA(93, 28, 93, 130, 2.5f, new Rgba(255, 255, 255, 200));

        // Giant swirled candy head
        float cx = 96f, cy = 166f, R = 68f;
        c.CircleAA(cx, cy, R + 5f, Ink);
        for (int y = (int)(cy - R - 1); y <= (int)(cy + R + 1); y++)
        {
            for (int x = (int)(cx - R - 1); x <= (int)(cx + R + 1); x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > R) continue;
                float angle = (float)Math.Atan2(dy, dx);
                float spiral = (angle + dist * 0.14f);
                int band = ((int)Math.Floor(spiral / 1.05f) % 4 + 4) % 4;
                Rgba col;
                if (rainbowPink)
                    col = band == 0 ? PinkCandy : (band == 1 ? Cream : (band == 2 ? LemonCandy : Lavender));
                else
                    col = band == 0 ? AquaCandy : (band == 1 ? Cream : (band == 2 ? PinkLight : MintLight));
                c.Blend(x, y, col);
            }
        }
        // Glossy cartoon specular arc
        c.EllipseAA(cx - 22, cy + 24, 18, 10, new Rgba(255, 255, 255, 165));
        c.CircleAA(cx - 34, cy + 12, 6, new Rgba(255, 255, 255, 185));

        return c;
    }

    public static Canvas BuildCandyCane()
    {
        const int W = 160, H = 256;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(66, 20, 36, 12, new Rgba(64, 34, 68, 75));

        // Draw curved candy cane centerline
        List<float> xs = new List<float>();
        List<float> ys = new List<float>();
        for (int i = 0; i <= 40; i++)
        {
            xs.Add(64f);
            ys.Add(22f + i * 4.1f);
        }
        for (int i = 1; i <= 36; i++)
        {
            double ang = Math.PI - (i / 36.0) * Math.PI;
            xs.Add(96f + (float)Math.Cos(ang) * 32f);
            ys.Add(186f + (float)Math.Sin(ang) * 32f);
        }
        for (int i = 1; i <= 8; i++)
        {
            xs.Add(128f);
            ys.Add(186f - i * 3.5f);
        }

        // Outline pass
        for (int i = 0; i < xs.Count; i++) c.CircleAA(xs[i], ys[i], 21f, Ink);
        // Striped cream & berry-red pass
        Rgba caneRed = new Rgba(244, 78, 124);
        for (int i = 0; i < xs.Count; i++)
        {
            Rgba col = ((i / 6) % 2 == 0) ? caneRed : Cream;
            c.CircleAA(xs[i], ys[i], 16f, col);
            c.CircleAA(xs[i] - 4f, ys[i] + 3f, 5f, new Rgba(255, 255, 255, 110));
        }
        return c;
    }

    public static Canvas BuildCandyLamp()
    {
        const int W = 140, H = 256;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(70, 18, 32, 11, new Rgba(64, 34, 68, 80));

        // Base pedestal
        c.RoundedRectAA(70, 26, 22, 12, 8, Lavender, Ink, 4f);
        // Pink-and-cream striped pole
        for (int y = 34; y < 186; y++)
        {
            Rgba col = ((y / 14) % 2 == 0) ? PinkCandy : Cream;
            c.Rect(61, y, 18, 1, Ink);
            c.Rect(65, y, 10, 1, col);
        }
        // Decorative collar
        c.RoundedRectAA(70, 184, 20, 8, 6, LemonCandy, Ink, 3.5f);
        // Glowing sugar-orb lantern on top
        c.CircleAA(70, 214, 30, Ink);
        c.CircleAA(70, 214, 25, LemonCandy);
        c.CircleAA(70, 214, 18, Cream);
        c.CircleAA(62, 224, 7, new Rgba(255, 255, 255, 220));
        return c;
    }

    public static Canvas BuildSugarFlowerBed()
    {
        const int W = 220, H = 140;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(110, 24, 96, 22, new Rgba(64, 34, 68, 70));

        // Fluffy layered mint & pastel-pink candy bush
        int[,] puffs = { { 56, 54, 38 }, { 164, 54, 38 }, { 110, 68, 48 }, { 82, 74, 36 }, { 138, 74, 36 } };
        for (int i = 0; i < puffs.GetLength(0); i++) c.CircleAA(puffs[i, 0], puffs[i, 1], puffs[i, 2] + 4, Ink);
        for (int i = 0; i < puffs.GetLength(0); i++) c.CircleAA(puffs[i, 0], puffs[i, 1], puffs[i, 2], i < 2 ? MintDeep : MintBase);
        c.CircleAA(110, 80, 32, MintLight);
        c.CircleAA(82, 82, 22, MintLight);
        c.CircleAA(138, 82, 22, MintLight);

        // Big cheerful sugar flowers & gumdrops!
        int[,] fl = { { 58, 62 }, { 108, 92 }, { 158, 64 }, { 86, 52 }, { 134, 54 } };
        Rgba[] cols = { PinkCandy, LemonCandy, Lavender, AquaCandy, PinkLight };
        for (int i = 0; i < fl.GetLength(0); i++)
        {
            float fx = fl[i, 0], fy = fl[i, 1];
            for (int p = 0; p < 5; p++)
            {
                double a = p * Math.PI * 2.0 / 5.0;
                c.CircleAA(fx + (float)Math.Cos(a) * 9f, fy + (float)Math.Sin(a) * 9f, 8.5f, Ink);
            }
            for (int p = 0; p < 5; p++)
            {
                double a = p * Math.PI * 2.0 / 5.0;
                c.CircleAA(fx + (float)Math.Cos(a) * 9f, fy + (float)Math.Sin(a) * 9f, 6.2f, cols[i]);
            }
            c.CircleAA(fx, fy, 5.5f, Cream);
        }
        return c;
    }

    public static Canvas BuildMarshmallowCluster()
    {
        const int W = 180, H = 120;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(90, 22, 74, 18, new Rgba(64, 34, 68, 70));

        // 3 plump pastel marshmallows
        DrawMarshmallow(c, 58, 48, 32, 24, PinkLight, PinkCandy);
        DrawMarshmallow(c, 124, 48, 32, 24, AquaCandy, new Rgba(88, 198, 218));
        DrawMarshmallow(c, 92, 72, 34, 26, Cream, new Rgba(242, 228, 214));
        return c;
    }

    private static void DrawMarshmallow(Canvas c, float cx, float cy, float rx, float ry, Rgba body, Rgba shade)
    {
        c.RoundedRectAA(cx, cy, rx, ry, 16f, body, Ink, 4f);
        c.EllipseAA(cx, cy + ry - 8f, rx - 6f, 8f, new Rgba(255, 255, 255, 145));
        c.EllipseAA(cx, cy - ry + 9f, rx - 8f, 6f, shade);
    }

    public static Canvas BuildCandySparkle()
    {
        const int S = 64;
        Canvas c = new Canvas(S, S);
        float mid = 31.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = Math.Abs(x - mid) / 28f;
                float dy = Math.Abs(y - mid) / 28f;
                float d = (float)Math.Pow(dx, 0.55) + (float)Math.Pow(dy, 0.55);
                if (d < 1f)
                {
                    float a = Clamp01((1f - d) * 1.4f);
                    Rgba col = Lerp(PinkLight, new Rgba(255, 255, 240), a);
                    c.Blend(x, y, new Rgba(col.r, col.g, col.b, (int)(a * 255)));
                }
            }
        }
        return c;
    }

    public static Canvas BuildCandyGlow()
    {
        const int S = 128;
        Canvas c = new Canvas(S, S);
        float mid = 63.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - mid) * (x - mid) + (y - mid) * (y - mid)) / 62f;
                if (d >= 1f) continue;
                float f = (1f - d) * (1f - d);
                c.Blend(x, y, new Rgba(255, 224, 184, (int)(f * 170)));
            }
        }
        return c;
    }

    public static Canvas BuildCandyShadow()
    {
        const int W = 128, H = 64;
        Canvas c = new Canvas(W, H);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                float dx = (x - 63.5f) / 60f;
                float dy = (y - 31.5f) / 28f;
                float d = dx * dx + dy * dy;
                if (d >= 1f) continue;
                float f = 1f - d;
                c.Blend(x, y, new Rgba(64, 34, 72, (int)(f * 125)));
            }
        }
        return c;
    }

    private static void DrawStar(Canvas c, float cx, float cy, float outerR, float innerR, Rgba fill, Rgba border)
    {
        int x0 = Clamp((int)(cx - outerR - 4), 0, c.W - 1);
        int x1 = Clamp((int)(cx + outerR + 4), 0, c.W - 1);
        int y0 = Clamp((int)(cy - outerR - 4), 0, c.H - 1);
        int y1 = Clamp((int)(cy + outerR + 4), 0, c.H - 1);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx) + Math.PI * 0.5;
                double sector = ((ang % (Math.PI * 2.0 / 5.0)) + Math.PI * 2.0 / 5.0) % (Math.PI * 2.0 / 5.0);
                double halfSec = Math.PI / 5.0;
                double t = Math.Abs(sector - halfSec) / halfSec;
                float rAtAng = (float)(innerR + (outerR - innerR) * t);
                if (dist <= rAtAng + 4f)
                    c.Blend(x, y, dist <= rAtAng ? fill : border);
            }
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

#if UNITY_EDITOR
    public static void GenerateAndImportAll(string outAssetFolder)
    {
        string absOut = Path.Combine(Application.dataPath, outAssetFolder.Replace("Assets/", "").TrimStart('/'));
        Directory.CreateDirectory(absOut);

        GenerateAllProcedural((name, canvas) => SaveCanvasUnity(Path.Combine(absOut, name), canvas));

        // Also cut out all 8 adorable cupcake houses from the user's "a casa de mosntros" folder!
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

        // ETAPA 3: Padroniza TODOS os sprites 2D da fase para FilterMode = Point e PPU = 32!
        foreach (string file in Directory.GetFiles(absOut, "*.png"))
        {
            string fileName = Path.GetFileName(file);
            TextureWrapMode wrap = (fileName == "CandyMeadowTile.png" || fileName == "CandyRoadH.png" || fileName == "CandyRoadV.png")
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp;
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

        // Unity Texture2D y=0 is bottom! Top row is y in [h*0.48..h*0.93], bottom row is [h*0.03..h*0.49]
        string[] topNames = { "CandyHouse_Strawberry.png", "CandyHouse_ChocoMint.png", "CandyHouse_Rainbow.png" };
        string[] botNames = { "CandyHouse_Nautical.png", "CandyHouse_Galaxy.png", "CandyHouse_Matcha.png" };
        int cellW = w / 3;
        int topY = (int)(h * 0.49f), topH = (int)(h * 0.44f);
        int botY = (int)(h * 0.03f), botH = (int)(h * 0.46f);
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
        for (int i = 0; i < cols.Length; i++)
        {
            Rgba p = canvas.Px[i];
            cols[i] = new Color32(p.r, p.g, p.b, p.a);
        }
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
        imp.filterMode = FilterMode.Point; // ETAPA 3: Point (no filter) padronizado!
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
