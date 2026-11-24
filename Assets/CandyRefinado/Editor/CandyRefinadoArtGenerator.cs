using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

/// <summary>
/// Generates the refined Candy World visual assets for "Girl Power - Cenario Refinado":
/// - Three clearly differentiated surfaces:
///     1. Smooth, low-contrast golden biscuit drivable path (highlights the pink cart & items)
///     2. Soft vanilla-cream borders/curbs with sprinkles concentrated along outer edges
///     3. Varied pastel decorative zones (mint meadow + rose, lavender & lemon organic yards)
/// - Clean transparent cutouts for ALL 14 confectionery/cafe cupcake houses from "a casa de mosntros"
///   (removing the opaque peach/tan JPEG rectangles and stray doodles).
/// </summary>
public static class CandyRefinadoArtGenerator
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

        public void Blend(int x, int y, Rgba c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H || c.a == 0) return;
            int idx = y * W + x;
            if (c.a == 255) { Px[idx] = c; return; }
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
            Blend(((x % W) + W) % W, ((y % H) + H) % H, c);
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
            int x0 = (int)Math.Floor(cx - r - 2), x1 = (int)Math.Ceiling(cx + r + 2);
            int y0 = (int)Math.Floor(cy - r - 2), y1 = (int)Math.Ceiling(cy + r + 2);
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
            int x0 = (int)Math.Floor(cx - rx - 2), x1 = (int)Math.Ceiling(cx + rx + 2);
            int y0 = (int)Math.Floor(cy - ry - 2), y1 = (int)Math.Ceiling(cy + ry + 2);
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
            int bx0 = (int)Math.Floor(Math.Min(x0, x1) - r - 2), bx1 = (int)Math.Ceiling(Math.Max(x0, x1) + r + 2);
            int by0 = (int)Math.Floor(Math.Min(y0, y1) - r - 2), by1 = (int)Math.Ceiling(Math.Max(y0, y1) + r + 2);
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

    // Harmonized 3-surface Candy World Palette (from Girl Power Title Screen!)
    // Surface 1: Smooth Golden Biscuit (low contrast in road center!)
    private static readonly Rgba BiscuitCenter = new Rgba(246, 218, 170);
    private static readonly Rgba BiscuitWarm = new Rgba(238, 204, 152);
    private static readonly Rgba BiscuitEdge = new Rgba(224, 184, 130);

    // Surface 2: Soft Vanilla Cream Borders
    private static readonly Rgba CreamBorder = new Rgba(255, 247, 232);
    private static readonly Rgba CreamShade = new Rgba(242, 226, 202);
    private static readonly Rgba CaramelTrim = new Rgba(168, 114, 86);

    // Surface 3: Varied Pastel Decorative Zones
    private static readonly Rgba PastelMint = new Rgba(174, 232, 194);
    private static readonly Rgba PastelMintLight = new Rgba(198, 244, 210);
    private static readonly Rgba PastelMintDeep = new Rgba(146, 212, 172);
    private static readonly Rgba PastelRose = new Rgba(252, 208, 228);
    private static readonly Rgba PastelLavender = new Rgba(220, 202, 248);
    private static readonly Rgba PastelLemon = new Rgba(254, 238, 184);

    // Crisp cartoon accents
    private static readonly Rgba Ink = new Rgba(74, 42, 76);
    private static readonly Rgba PinkCandy = new Rgba(255, 126, 190);
    private static readonly Rgba PinkLight = new Rgba(255, 182, 220);
    private static readonly Rgba Lavender = new Rgba(194, 152, 246);
    private static readonly Rgba AquaCandy = new Rgba(118, 226, 238);
    private static readonly Rgba LemonCandy = new Rgba(255, 230, 116);

    private static readonly Rgba[] Sprinkles =
    {
        new Rgba(255, 112, 176),
        new Rgba(108, 214, 248),
        new Rgba(255, 216, 86),
        new Rgba(184, 134, 250),
        new Rgba(124, 232, 166),
        new Rgba(255, 152, 98)
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
        // Surface 3: Varied pastel decorative ground & organic yard patches
        writer("PastelMintGround.png", BuildPastelMintGround());
        writer("PastelYardRose.png", BuildPastelYardPatch(PastelRose, new Rgba(255, 228, 240), PinkCandy));
        writer("PastelYardLavender.png", BuildPastelYardPatch(PastelLavender, new Rgba(236, 224, 255), Lavender));
        writer("PastelYardLemon.png", BuildPastelYardPatch(PastelLemon, new Rgba(255, 248, 214), new Rgba(255, 184, 108)));

        // Surfaces 1 & 2: Smooth Golden Biscuit Path + Soft Cream Borders
        writer("BiscuitRoadH.png", BuildBiscuitRoad(false));
        writer("BiscuitRoadV.png", BuildBiscuitRoad(true));
        writer("BiscuitRoadCross.png", BuildBiscuitCross());
        writer("BiscuitRoadCorner.png", BuildBiscuitCorner());
        writer("BiscuitRoadT.png", BuildBiscuitTJunction());

        // Side details, landmarks & props
        writer("SprinkleEdgeCluster.png", BuildSprinkleEdgeCluster());
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
    // CLEAN TRANSPARENT CUTOUT FOR ALL 14 CONFECTIONERY & CAFE HOUSES
    // =========================================================================
    public static Canvas CutoutHouse(Rgba[] srcPixels, int srcW, int srcH, int cropX, int cropY, int cropW, int cropH)
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
            int cur = q.Dequeue();
            int cx = cur % cropW, cy = cur / cropW;
            for (int k = 0; k < 4; k++)
            {
                int nx = cx + dxs[k], ny = cy + dys[k];
                if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH) continue;
                int ni = ny * cropW + nx;
                if (!isBg[ni] && MatchesBg(sub[ni])) { isBg[ni] = true; q.Enqueue(ni); }
            }
        }

        // Keep ONLY the largest connected component (removes all floating doodles & stray cookies!)
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

        int pad = 16;
        int outW = (maxX - minX + 1) + pad * 2;
        int outH = (maxY - minY + 1) + pad * 2;
        Canvas outC = new Canvas(outW, outH);
        outC.EllipseAA(outW * 0.5f, pad + 6f, (maxX - minX) * 0.42f, 14f, new Rgba(74, 42, 76, 68));

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int i = y * cropW + x;
                if (!keep[i]) continue;
                Rgba p = sub[i];
                int bgN = 0;
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + dxs[k], ny = y + dys[k];
                    if (nx < 0 || nx >= cropW || ny < 0 || ny >= cropH || !keep[ny * cropW + nx]) bgN++;
                }
                int a = 255;
                if (bgN > 0)
                {
                    int lum = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                    if (lum > 152) a = bgN >= 2 ? 85 : 170;
                }
                outC.Blend((x - minX) + pad, (y - minY) + pad, new Rgba(p.r, p.g, p.b, a));
            }

        if (outW > 512 || outH > 512)
        {
            float sc = 512f / Math.Max(outW, outH);
            return DownscaleBilinear(outC, Math.Max(32, (int)(outW * sc)), Math.Max(32, (int)(outH * sc)));
        }
        return outC;
    }

    private static Rgba EstimateCornerBg(Rgba[] sub, int w, int h)
    {
        int r = 0, g = 0, b = 0, cnt = 0;
        int[,] pts = { { 4, 4 }, { w - 5, 4 }, { 4, h - 5 }, { w - 5, h - 5 }, { w / 2, 4 }, { 4, h / 2 }, { w - 5, h / 2 } };
        for (int i = 0; i < pts.GetLength(0); i++)
        {
            Rgba p = sub[Clamp(pts[i, 1], 0, h - 1) * w + Clamp(pts[i, 0], 0, w - 1)];
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
            int y0 = Clamp((int)Math.Floor(sy), 0, src.H - 1), y1 = Clamp(y0 + 1, 0, src.H - 1);
            float fy = Clamp01(sy - y0);
            for (int x = 0; x < nw; x++)
            {
                float sx = (x + 0.5f) * src.W / nw - 0.5f;
                int x0 = Clamp((int)Math.Floor(sx), 0, src.W - 1), x1 = Clamp(x0 + 1, 0, src.W - 1);
                float fx = Clamp01(sx - x0);
                Rgba t = Lerp(src.Px[y0 * src.W + x0], src.Px[y0 * src.W + x1], fx);
                Rgba b = Lerp(src.Px[y1 * src.W + x0], src.Px[y1 * src.W + x1], fx);
                dst.Px[y * nw + x] = Lerp(t, b, fy);
            }
        }
        return dst;
    }

    // =========================================================================
    // SURFACE 3: CALM, NON-REPETITIVE PASTEL MEADOW & VARIED ORGANIC YARD PADS
    // =========================================================================
    public static Canvas BuildPastelMintGround()
    {
        const int S = 512;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
        {
            double ay = y * (2.0 * Math.PI / S);
            for (int x = 0; x < S; x++)
            {
                double ax = x * (2.0 * Math.PI / S);
                float n = (float)(0.50 + 0.24 * Math.Sin(ax + Math.Cos(ay)) + 0.16 * Math.Cos(ay * 2.0 - ax) + 0.10 * Math.Sin((ax + ay) * 3.0));
                c.Px[y * S + x] = n > 0.52f ? Lerp(PastelMint, PastelMintLight, (n - 0.52f) * 2.0f) : Lerp(PastelMintDeep, PastelMint, n / 0.52f);
            }
        }
        // Subtle soft sunlit grass waves (low contrast so yards & houses take center stage!)
        for (int i = 0; i < 12; i++)
        {
            float cx = Math.Abs(Hash(i, 17)) % S, cy = Math.Abs(Hash(i, 29)) % S;
            c.EllipseAA(cx, cy, 44 + (i % 3) * 14, 30 + (i % 2) * 14, new Rgba(212, 248, 222, 65), true);
        }
        // Gentle scattered grass blades & tiny pastel clovers
        for (int i = 0; i < 42; i++)
        {
            float gx = Math.Abs(Hash(i, 41)) % S, gy = Math.Abs(Hash(i, 47)) % S;
            Rgba bl = new Rgba(124, 194, 152, 130);
            c.CapsuleAA(gx, gy, gx - 3, gy + 8, 1.3f, bl, true);
            c.CapsuleAA(gx, gy, gx + 3, gy + 7, 1.3f, bl, true);
        }
        Rgba[] petals = { PinkLight, CreamBorder, PastelLemon, PastelLavender };
        for (int i = 0; i < 14; i++)
        {
            float fx = Math.Abs(Hash(i, 61)) % S, fy = Math.Abs(Hash(i, 67)) % S;
            Rgba p = petals[i % petals.Length];
            for (int k = 0; k < 5; k++)
            {
                double a = k * Math.PI * 2.0 / 5.0;
                c.CircleAA(fx + (float)Math.Cos(a) * 4f, fy + (float)Math.Sin(a) * 4f, 2.8f, new Rgba(p.r, p.g, p.b, 210), true);
            }
            c.CircleAA(fx, fy, 2.2f, LemonCandy, true);
        }
        return c;
    }

    public static Canvas BuildPastelYardPatch(Rgba mainCol, Rgba innerCol, Rgba accentFlower)
    {
        // 384 x 288 organic scalloped pastel lawn pad placed beneath each house cluster!
        const int W = 384, H = 288;
        Canvas c = new Canvas(W, H);
        // Soft organic multi-lobe cloud silhouette with gentle feathering
        c.EllipseAA(192, 144, 168, 118, new Rgba(mainCol.r, mainCol.g, mainCol.b, 140));
        c.EllipseAA(118, 154, 102, 86, new Rgba(mainCol.r, mainCol.g, mainCol.b, 185));
        c.EllipseAA(266, 148, 104, 88, new Rgba(mainCol.r, mainCol.g, mainCol.b, 185));
        c.EllipseAA(192, 144, 148, 100, mainCol);
        c.EllipseAA(192, 150, 118, 76, innerCol);

        // Concentrate tiny sugar sprinkles & blossoms around the yard's outer rim!
        for (int i = 0; i < 18; i++)
        {
            double ang = i * Math.PI * 2.0 / 18.0 + 0.15;
            float sx = 192f + (float)Math.Cos(ang) * (132f + (i % 3) * 8f);
            float sy = 144f + (float)Math.Sin(ang) * (88f + (i % 2) * 8f);
            if (i % 3 == 0)
            {
                for (int p = 0; p < 5; p++)
                {
                    double pa = p * Math.PI * 2.0 / 5.0;
                    c.CircleAA(sx + (float)Math.Cos(pa) * 4.5f, sy + (float)Math.Sin(pa) * 4.5f, 3.3f, accentFlower);
                }
                c.CircleAA(sx, sy, 2.6f, CreamBorder);
            }
            else
            {
                float dx = (float)Math.Cos(ang + 1.1) * 4.5f, dy = (float)Math.Sin(ang + 1.1) * 4.5f;
                Rgba sp = Sprinkles[i % Sprinkles.Length];
                c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.3f, new Rgba(sp.r, sp.g, sp.b, 210));
            }
        }
        return c;
    }

    // =========================================================================
    // SURFACES 1 & 2: SMOOTH GOLDEN BISCUIT PATH + SOFT CREAM BORDERS
    // Low-contrast center lane keeps the pink cart, items & obstacles 100% crisp!
    // =========================================================================
    public static Canvas BuildBiscuitRoad(bool vertical)
    {
        const int S = 256;
        Canvas c = new Canvas(S, S);

        for (int y = 0; y < S; y++)
        {
            int dy = Math.Min(y, S - 1 - y);
            for (int x = 0; x < S; x++)
            {
                if (dy < 6)
                {
                    c.Blend(x, y, new Rgba(74, 42, 76, (int)(55 * (dy / 6f))));
                }
                else if (dy < 9)
                {
                    c.Blend(x, y, CaramelTrim);
                }
                else if (dy < 30)
                {
                    // Surface 2: Soft Vanilla Cream Border with gentle scalloped stone rhythm
                    float t = (dy - 9) / 21f;
                    Rgba curb = Lerp(CreamShade, CreamBorder, t);
                    if ((x % 32) < 2) curb = Lerp(curb, CaramelTrim, 0.28f);
                    c.Blend(x, y, curb);
                }
                else if (dy < 33)
                {
                    c.Blend(x, y, new Rgba(206, 166, 118));
                }
                else
                {
                    // Surface 1: Smooth, Low-Contrast Golden Biscuit center!
                    float edgeT = Clamp01((dy - 33) / 28f);
                    Rgba col = Lerp(BiscuitEdge, BiscuitCenter, edgeT);
                    // Very subtle warm cookie grain (low amplitude so cart & donuts pop!)
                    float wave = (float)(Math.Sin(x * 0.049) * Math.Cos(y * 0.049)) * 0.06f;
                    col = Lerp(col, BiscuitWarm, Clamp01(0.5f + wave) * 0.25f);
                    c.Blend(x, y, col);
                }
            }
        }

        // Delicate low-contrast soft cream center guide dashes (never competes with cart!)
        c.RoundedRectAA(64, 128, 32, 4.5f, 4f, new Rgba(255, 246, 224, 175), new Rgba(226, 192, 146, 120), 1.5f);
        c.RoundedRectAA(192, 128, 32, 4.5f, 4f, new Rgba(255, 246, 224, 175), new Rgba(226, 192, 146, 120), 1.5f);

        // Concentrate delicate colorful sprinkles ONLY on the soft cream outer borders (NEVER in the middle!)
        for (int i = 0; i < 14; i++)
        {
            float sx = 14 + (Math.Abs(Hash(i, 151)) % 228);
            float sy = (i % 2 == 0) ? (16 + (Math.Abs(Hash(i, 157)) % 9)) : (S - 25 + (Math.Abs(Hash(i, 163)) % 9));
            double ang = (Math.Abs(Hash(i, 167)) % 314) / 100.0;
            float dx = (float)Math.Cos(ang) * 4.2f, dy = (float)Math.Sin(ang) * 4.2f;
            Rgba sp = Sprinkles[i % Sprinkles.Length];
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.2f, new Rgba(sp.r, sp.g, sp.b, 215));
        }

        return vertical ? Transpose(c) : c;
    }

    public static Canvas BuildBiscuitCross()
    {
        const int S = 256;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - 128f) * (x - 128f) + (y - 128f) * (y - 128f)) / 160f;
                c.Px[y * S + x] = Lerp(BiscuitCenter, BiscuitWarm, Clamp01(d * 0.55f));
            }

        // Smooth rounded cream corner curbs in all 4 corners
        int[,] corners = { { 0, 0 }, { S, 0 }, { 0, S }, { S, S } };
        for (int k = 0; k < 4; k++)
        {
            float cx = corners[k, 0], cy = corners[k, 1];
            c.CircleAA(cx, cy, 33, new Rgba(206, 166, 118));
            c.CircleAA(cx, cy, 30, CreamBorder);
            c.CircleAA(cx, cy, 18, CreamShade);
            c.CircleAA(cx, cy, 9, CaramelTrim);
            c.CircleAA(cx, cy, 6, new Rgba(0, 0, 0, 0));
        }

        // Subtle soft cream biscuit crosswalk bars
        for (int i = 0; i < 4; i++)
        {
            float off = 66 + i * 41;
            Rgba bar = new Rgba(255, 248, 232, 185);
            Rgba bdr = new Rgba(224, 190, 144, 130);
            c.RoundedRectAA(off, 16, 11, 8, 4, bar, bdr, 1.5f);
            c.RoundedRectAA(off, S - 16, 11, 8, 4, bar, bdr, 1.5f);
            c.RoundedRectAA(16, off, 8, 11, 4, bar, bdr, 1.5f);
            c.RoundedRectAA(S - 16, off, 8, 11, 4, bar, bdr, 1.5f);
        }
        return c;
    }

    public static Canvas BuildBiscuitTJunction()
    {
        // Connects West (-X), East (+X), and South (-Y); top (+Y) has continuous soft cream curb!
        const int S = 256;
        Canvas c = BuildBiscuitCross();
        // Paint continuous top cream border across y = S-33..S
        for (int y = S - 33; y < S; y++)
        {
            int dy = S - 1 - y;
            for (int x = 0; x < S; x++)
            {
                if (dy < 6) c.Px[y * S + x] = new Rgba(74, 42, 76, (int)(55 * (dy / 6f)));
                else if (dy < 9) c.Px[y * S + x] = CaramelTrim;
                else if (dy < 30) c.Px[y * S + x] = Lerp(CreamShade, CreamBorder, (dy - 9) / 21f);
                else c.Px[y * S + x] = new Rgba(206, 166, 118);
            }
        }
        return c;
    }

    public static Canvas BuildBiscuitCorner()
    {
        // Smooth curved corner connecting East (+X) and North (+Y)
        const int S = 256;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - S) * (x - S) + (y - S) * (y - S));
                if (d < 9f || d > S - 6f)
                {
                    if (d >= S - 6f && d <= S)
                        c.Blend(x, y, new Rgba(74, 42, 76, (int)(55 * (1f - (d - (S - 6f)) / 6f))));
                    continue;
                }
                if (d < 12f || d > S - 9f) { c.Blend(x, y, CaramelTrim); continue; }
                if (d < 31f || d > S - 30f) { c.Blend(x, y, CreamBorder); continue; }
                if (d < 34f || d > S - 33f) { c.Blend(x, y, new Rgba(206, 166, 118)); continue; }
                float centerProx = 1f - Math.Abs(d - 128f) / 96f;
                c.Blend(x, y, Lerp(BiscuitWarm, BiscuitCenter, Clamp01(centerProx)));
            }
        }
        // Soft cream curved dashes along lane center
        for (int i = 1; i <= 3; i++)
        {
            double ang = (i / 4.0) * (Math.PI * 0.5);
            float mx = S - (float)Math.Cos(ang) * 128f;
            float my = S - (float)Math.Sin(ang) * 128f;
            c.CircleAA(mx, my, 5.5f, new Rgba(255, 246, 224, 175));
        }
        return c;
    }

    public static Canvas BuildSprinkleEdgeCluster()
    {
        // 192 x 96 cluster of colorful sprinkles & tiny blossoms for road edges and house yards!
        const int W = 192, H = 96;
        Canvas c = new Canvas(W, H);
        for (int i = 0; i < 20; i++)
        {
            float sx = 18 + (Math.Abs(Hash(i, 191)) % (W - 36));
            float sy = 16 + (Math.Abs(Hash(i, 193)) % (H - 32));
            if (i % 5 == 0)
            {
                Rgba fl = Sprinkles[i % Sprinkles.Length];
                for (int p = 0; p < 5; p++)
                {
                    double a = p * Math.PI * 2.0 / 5.0;
                    c.CircleAA(sx + (float)Math.Cos(a) * 5f, sy + (float)Math.Sin(a) * 5f, 3.8f, fl);
                }
                c.CircleAA(sx, sy, 3.0f, CreamBorder);
            }
            else
            {
                double ang = (Math.Abs(Hash(i, 197)) % 314) / 100.0;
                float dx = (float)Math.Cos(ang) * 5.2f, dy = (float)Math.Sin(ang) * 5.2f;
                Rgba sp = Sprinkles[i % Sprinkles.Length];
                c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 3.2f, Ink);
                c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.2f, sp);
            }
        }
        return c;
    }

    // =========================================================================
    // LANDMARKS & PROPS (Delivery Plaza, Berry Pond, Lollipops, Canes, Lamps, Flowerbeds)
    // =========================================================================
    public static Canvas BuildDeliveryPlaza()
    {
        const int S = 512;
        Canvas c = new Canvas(S, S);
        float mid = S * 0.5f;
        c.CircleAA(mid, mid - 6, 240, new Rgba(74, 42, 76, 65));
        for (int i = 0; i < 20; i++)
        {
            double a = i * Math.PI * 2.0 / 20.0;
            float sx = mid + (float)Math.Cos(a) * 212f, sy = mid + (float)Math.Sin(a) * 212f;
            c.CircleAA(sx, sy, 29, Ink);
            c.CircleAA(sx, sy, 25, i % 2 == 0 ? PinkLight : CreamBorder);
        }
        c.CircleAA(mid, mid, 204, Ink);
        c.CircleAA(mid, mid, 198, new Rgba(255, 202, 228));
        c.CircleAA(mid, mid, 174, CreamBorder);
        c.CircleAA(mid, mid, 148, PinkCandy);
        c.CircleAA(mid, mid, 130, new Rgba(255, 168, 214));
        for (int i = 0; i < 24; i++)
        {
            double a = i * Math.PI * 2.0 / 24.0;
            float sx = mid + (float)Math.Cos(a) * 161f, sy = mid + (float)Math.Sin(a) * 161f;
            float dx = (float)Math.Cos(a + 0.8) * 5.5f, dy = (float)Math.Sin(a + 0.8) * 5.5f;
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 3.6f, Ink);
            c.CapsuleAA(sx - dx, sy - dy, sx + dx, sy + dy, 2.4f, Sprinkles[i % Sprinkles.Length]);
        }
        DrawStar(c, mid, mid, 88, 40, LemonCandy, Ink);
        DrawStar(c, mid, mid, 64, 28, CreamBorder, new Rgba(255, 210, 96));
        return c;
    }

    public static Canvas BuildBerryPond()
    {
        const int W = 512, H = 320;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(256, 156, 236, 136, new Rgba(74, 42, 76, 60));
        c.EllipseAA(256, 160, 230, 132, Ink);
        c.EllipseAA(256, 160, 223, 126, CreamBorder);
        c.EllipseAA(256, 160, 202, 108, Ink);
        c.EllipseAA(256, 160, 196, 102, new Rgba(92, 208, 238));
        c.EllipseAA(256, 152, 178, 88, AquaCandy);
        c.EllipseAA(240, 146, 134, 64, new Rgba(164, 244, 248));
        c.CapsuleAA(132, 184, 196, 184, 3.8f, new Rgba(255, 255, 255, 195));
        c.CapsuleAA(288, 126, 366, 126, 3.8f, new Rgba(255, 255, 255, 195));
        DrawLilyPad(c, 156, 152, 23);
        DrawLilyPad(c, 346, 166, 21);
        DrawLilyPad(c, 262, 202, 17);
        return c;
    }

    private static void DrawLilyPad(Canvas c, float cx, float cy, float r)
    {
        c.CircleAA(cx, cy, r + 2.5f, Ink);
        c.CircleAA(cx, cy, r, new Rgba(122, 218, 152));
        c.CircleAA(cx - 3, cy + 3, r * 0.65f, PastelMintLight);
        for (int i = 0; i < 5; i++)
        {
            double a = i * Math.PI * 2.0 / 5.0;
            c.CircleAA(cx + (float)Math.Cos(a) * 6f, cy + (float)Math.Sin(a) * 6f, 5.2f, PinkCandy);
        }
        c.CircleAA(cx, cy, 4.2f, LemonCandy);
    }

    public static Canvas BuildGiantLollipop(bool rainbowPink)
    {
        const int W = 192, H = 256;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(96, 22, 40, 13, new Rgba(74, 42, 76, 70));
        c.CapsuleAA(96, 24, 96, 134, 10.5f, Ink);
        c.CapsuleAA(96, 24, 96, 134, 7.2f, CreamBorder);
        float cx = 96f, cy = 166f, R = 66f;
        c.CircleAA(cx, cy, R + 5f, Ink);
        for (int y = (int)(cy - R - 1); y <= (int)(cy + R + 1); y++)
            for (int x = (int)(cx - R - 1); x <= (int)(cx + R + 1); x++)
            {
                float dx = x - cx, dy = y - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist > R) continue;
                int band = ((int)Math.Floor(((float)Math.Atan2(dy, dx) + dist * 0.14f) / 1.05f) % 4 + 4) % 4;
                Rgba col = rainbowPink
                    ? (band == 0 ? PinkCandy : (band == 1 ? CreamBorder : (band == 2 ? LemonCandy : Lavender)))
                    : (band == 0 ? AquaCandy : (band == 1 ? CreamBorder : (band == 2 ? PinkLight : PastelMintLight)));
                c.Blend(x, y, col);
            }
        c.EllipseAA(cx - 22, cy + 24, 17, 9, new Rgba(255, 255, 255, 165));
        return c;
    }

    public static Canvas BuildCandyCane()
    {
        const int W = 160, H = 256;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(66, 20, 34, 11, new Rgba(74, 42, 76, 70));
        List<float> xs = new List<float>(), ys = new List<float>();
        for (int i = 0; i <= 40; i++) { xs.Add(64f); ys.Add(22f + i * 4.1f); }
        for (int i = 1; i <= 36; i++)
        {
            double a = Math.PI - (i / 36.0) * Math.PI;
            xs.Add(96f + (float)Math.Cos(a) * 32f); ys.Add(186f + (float)Math.Sin(a) * 32f);
        }
        for (int i = 1; i <= 8; i++) { xs.Add(128f); ys.Add(186f - i * 3.5f); }
        for (int i = 0; i < xs.Count; i++) c.CircleAA(xs[i], ys[i], 20f, Ink);
        Rgba red = new Rgba(244, 82, 126);
        for (int i = 0; i < xs.Count; i++)
            c.CircleAA(xs[i], ys[i], 15.5f, ((i / 6) % 2 == 0) ? red : CreamBorder);
        return c;
    }

    public static Canvas BuildCandyLamp()
    {
        const int W = 140, H = 256;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(70, 18, 30, 10, new Rgba(74, 42, 76, 75));
        c.RoundedRectAA(70, 26, 21, 11, 8, Lavender, Ink, 3.8f);
        for (int y = 34; y < 186; y++)
        {
            c.Rect(61, y, 18, 1, Ink);
            c.Rect(65, y, 10, 1, ((y / 14) % 2 == 0) ? PinkCandy : CreamBorder);
        }
        c.RoundedRectAA(70, 184, 19, 8, 6, LemonCandy, Ink, 3.5f);
        c.CircleAA(70, 214, 29, Ink);
        c.CircleAA(70, 214, 24, LemonCandy);
        c.CircleAA(70, 214, 17, CreamBorder);
        return c;
    }

    public static Canvas BuildSugarFlowerBed()
    {
        const int W = 220, H = 140;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(110, 24, 94, 20, new Rgba(74, 42, 76, 65));
        int[,] puffs = { { 56, 54, 37 }, { 164, 54, 37 }, { 110, 68, 46 }, { 82, 74, 35 }, { 138, 74, 35 } };
        for (int i = 0; i < puffs.GetLength(0); i++) c.CircleAA(puffs[i, 0], puffs[i, 1], puffs[i, 2] + 4, Ink);
        for (int i = 0; i < puffs.GetLength(0); i++) c.CircleAA(puffs[i, 0], puffs[i, 1], puffs[i, 2], i < 2 ? PastelMintDeep : PastelMint);
        c.CircleAA(110, 80, 30, PastelMintLight);
        int[,] fl = { { 58, 62 }, { 108, 92 }, { 158, 64 }, { 86, 52 }, { 134, 54 } };
        Rgba[] cols = { PinkCandy, LemonCandy, Lavender, AquaCandy, PinkLight };
        for (int i = 0; i < fl.GetLength(0); i++)
        {
            float fx = fl[i, 0], fy = fl[i, 1];
            for (int p = 0; p < 5; p++) { double a = p * Math.PI * 2.0 / 5.0; c.CircleAA(fx + (float)Math.Cos(a) * 9f, fy + (float)Math.Sin(a) * 9f, 8.2f, Ink); }
            for (int p = 0; p < 5; p++) { double a = p * Math.PI * 2.0 / 5.0; c.CircleAA(fx + (float)Math.Cos(a) * 9f, fy + (float)Math.Sin(a) * 9f, 6.0f, cols[i]); }
            c.CircleAA(fx, fy, 5.2f, CreamBorder);
        }
        return c;
    }

    public static Canvas BuildMarshmallowCluster()
    {
        const int W = 180, H = 120;
        Canvas c = new Canvas(W, H);
        c.EllipseAA(90, 22, 72, 17, new Rgba(74, 42, 76, 65));
        c.RoundedRectAA(58, 48, 31, 23, 15, PinkLight, Ink, 3.8f);
        c.RoundedRectAA(124, 48, 31, 23, 15, AquaCandy, Ink, 3.8f);
        c.RoundedRectAA(92, 72, 33, 25, 16, CreamBorder, Ink, 3.8f);
        return c;
    }

    public static Canvas BuildCandySparkle()
    {
        const int S = 64;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Pow(Math.Abs(x - 31.5f) / 28f, 0.55) + (float)Math.Pow(Math.Abs(y - 31.5f) / 28f, 0.55);
                if (d < 1f)
                {
                    float a = Clamp01((1f - d) * 1.4f);
                    Rgba col = Lerp(PinkLight, new Rgba(255, 255, 240), a);
                    c.Blend(x, y, new Rgba(col.r, col.g, col.b, (int)(a * 255)));
                }
            }
        return c;
    }

    public static Canvas BuildCandyGlow()
    {
        const int S = 128;
        Canvas c = new Canvas(S, S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = (float)Math.Sqrt((x - 63.5f) * (x - 63.5f) + (y - 63.5f) * (y - 63.5f)) / 62f;
                if (d < 1f) c.Blend(x, y, new Rgba(255, 228, 188, (int)((1f - d) * (1f - d) * 165f)));
            }
        return c;
    }

    public static Canvas BuildCandyShadow()
    {
        const int W = 128, H = 64;
        Canvas c = new Canvas(W, H);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - 63.5f) / 60f, dy = (y - 31.5f) / 28f;
                float d = dx * dx + dy * dy;
                if (d < 1f) c.Blend(x, y, new Rgba(74, 42, 76, (int)((1f - d) * 120f)));
            }
        return c;
    }

    private static void DrawStar(Canvas c, float cx, float cy, float outerR, float innerR, Rgba fill, Rgba border)
    {
        int x0 = Clamp((int)(cx - outerR - 4), 0, c.W - 1), x1 = Clamp((int)(cx + outerR + 4), 0, c.W - 1);
        int y0 = Clamp((int)(cy - outerR - 4), 0, c.H - 1), y1 = Clamp((int)(cy + outerR + 4), 0, c.H - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                double ang = Math.Atan2(dy, dx) + Math.PI * 0.5;
                double sec = ((ang % (Math.PI * 2.0 / 5.0)) + Math.PI * 2.0 / 5.0) % (Math.PI * 2.0 / 5.0);
                float rAt = (float)(innerR + (outerR - innerR) * (Math.Abs(sec - Math.PI / 5.0) / (Math.PI / 5.0)));
                if (dist <= rAt + 4f) c.Blend(x, y, dist <= rAt ? fill : border);
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

        // Cut out ALL 14 confectionery & cafe houses from "a casa de mosntros"
        string srcDir = Path.Combine(Application.dataPath, "a casa de mosntros");
        ExtractSixGridUnity(Path.Combine(srcDir, "casa diversas 22.jpeg"), absOut,
            new[] { "House_Strawberry.png", "House_ChocoMint.png", "House_Rainbow.png" },
            new[] { "House_Nautical.png", "House_Galaxy.png", "House_Matcha.png" }, 0.49f, 0.44f);

        ExtractSixGridUnity(Path.Combine(srcDir, "casas de cafe.jpeg"), absOut,
            new[] { "House_Cappuccino.png", "House_Tiramisu.png", "House_TeaRose.png" },
            new[] { "House_Gingham.png", "House_Samovar.png", "House_CherryCake.png" }, 0.50f, 0.47f);

        ExtractSingleUnity(Path.Combine(srcDir, "House_in_nude_background_2K_202607221539.jpeg"), Path.Combine(absOut, "House_RocketBlue.png"));
        if (Directory.Exists(srcDir))
        {
            string[] bot = Directory.GetFiles(srcDir, "Marshmallow_Botanical*.jpeg");
            if (bot.Length > 0) ExtractSingleUnity(bot[0], Path.Combine(absOut, "House_Botanical.png"));
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureImporter(outAssetFolder + "/PastelMintGround.png", 64f, TextureWrapMode.Repeat, 1024);
        ConfigureImporter(outAssetFolder + "/BiscuitRoadH.png", 44f, TextureWrapMode.Repeat, 512);
        ConfigureImporter(outAssetFolder + "/BiscuitRoadV.png", 44f, TextureWrapMode.Repeat, 512);
        ConfigureImporter(outAssetFolder + "/BiscuitRoadCross.png", 44f, TextureWrapMode.Clamp, 512);
        ConfigureImporter(outAssetFolder + "/BiscuitRoadCorner.png", 44f, TextureWrapMode.Clamp, 512);
        ConfigureImporter(outAssetFolder + "/BiscuitRoadT.png", 44f, TextureWrapMode.Clamp, 512);
        ConfigureImporter(outAssetFolder + "/CandyDeliveryPlaza.png", 64f, TextureWrapMode.Clamp, 1024);
        ConfigureImporter(outAssetFolder + "/CandyBerryPond.png", 48f, TextureWrapMode.Clamp, 1024);

        foreach (string file in Directory.GetFiles(absOut, "*.png"))
        {
            string name = Path.GetFileName(file);
            if (name == "PastelMintGround.png" || name.StartsWith("BiscuitRoad") || name == "CandyDeliveryPlaza.png" || name == "CandyBerryPond.png")
                continue;
            ConfigureImporter(outAssetFolder + "/" + name, 64f, TextureWrapMode.Clamp, 1024);
        }

        // The houses are cutouts from painted source sheets. Import them as compact,
        // nearest-filtered sprites so they share the hard pixel edges of the road art.
        // The lower PPU keeps their world-space footprint close to the previous import.
        string[] houseNames =
        {
            "House_Botanical.png", "House_Cappuccino.png", "House_CherryCake.png",
            "House_ChocoMint.png", "House_Galaxy.png", "House_Gingham.png",
            "House_Matcha.png", "House_Nautical.png", "House_Rainbow.png",
            "House_RocketBlue.png", "House_Samovar.png", "House_Strawberry.png",
            "House_TeaRose.png", "House_Tiramisu.png"
        };
        foreach (string houseName in houseNames)
            ConfigureImporter(outAssetFolder + "/" + houseName, 32f, TextureWrapMode.Clamp, 256);

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ExtractSixGridUnity(string jpegPath, string outDir, string[] topNames, string[] botNames, float topYRatio, float topHRatio)
    {
        if (!File.Exists(jpegPath)) return;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(jpegPath))) { UnityEngine.Object.DestroyImmediate(tex); return; }
        int w = tex.width, h = tex.height;
        Color32[] cols = tex.GetPixels32();
        Rgba[] px = new Rgba[cols.Length];
        for (int i = 0; i < cols.Length; i++) px[i] = new Rgba(cols[i].r, cols[i].g, cols[i].b, cols[i].a);
        UnityEngine.Object.DestroyImmediate(tex);

        int cellW = w / 3;
        int topY = (int)(h * topYRatio), topH = (int)(h * topHRatio);
        int botY = (int)(h * 0.02f), botH = (int)(h * 0.48f);
        for (int i = 0; i < 3; i++)
        {
            SaveCanvasUnity(Path.Combine(outDir, topNames[i]), CutoutHouse(px, w, h, i * cellW, topY, cellW, topH));
            SaveCanvasUnity(Path.Combine(outDir, botNames[i]), CutoutHouse(px, w, h, i * cellW, botY, cellW, botH));
        }
    }

    private static void ExtractSingleUnity(string jpegPath, string outPngPath)
    {
        if (!File.Exists(jpegPath)) return;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(jpegPath))) { UnityEngine.Object.DestroyImmediate(tex); return; }
        int w = tex.width, h = tex.height;
        Color32[] cols = tex.GetPixels32();
        Rgba[] px = new Rgba[cols.Length];
        for (int i = 0; i < cols.Length; i++) px[i] = new Rgba(cols[i].r, cols[i].g, cols[i].b, cols[i].a);
        UnityEngine.Object.DestroyImmediate(tex);
        SaveCanvasUnity(outPngPath, CutoutHouse(px, w, h, (int)(w * 0.14f), (int)(h * 0.03f), (int)(w * 0.72f), (int)(h * 0.94f)));
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
        TextureImporterSettings s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }
#endif
}
