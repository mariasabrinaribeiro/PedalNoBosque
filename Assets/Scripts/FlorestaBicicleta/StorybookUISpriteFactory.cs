using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Ordem de Serviço UI/UX — Fábrica de sprites ilustrados e arredondados (Cozy Storybook 2D):
    /// Substitui retângulos padrão do Unity e caracteres especiais Unicode (que viram quadrados no TMP)
    /// por painéis orgânicos arredondados (9-slice) e ícones reais de alta nitidez.
    /// </summary>
    public static class StorybookUISpriteFactory
    {
        public enum UIIconKind
        {
            Star,
            WaterDrop,
            BackArrow,
            Stroll,
            Pedal,
            Jump,
            Basket,
            Book,
            Workshop,
            Bell,
            CloseX
        }

        private static Sprite cachedRoundedPill;
        private static Sprite cachedRoundedCard;
        private static readonly Dictionary<UIIconKind, Sprite> iconCache = new Dictionary<UIIconKind, Sprite>();

        private static Texture2D Blank(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void Circle(Texture2D tex, float cx, float cy, float r, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + r + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + r + 1));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d <= r)
                    {
                        float aa = Mathf.Clamp01((r - d) * 1.6f);
                        Color c = col;
                        c.a *= aa;
                        BlendPixel(tex, x, y, c);
                    }
                }
            }
        }

        private static void Line(Texture2D tex, float x0, float y0, float x1, float y1, float thickness, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Circle(tex, Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), thickness * 0.5f, col);
            }
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color src)
        {
            Color dst = tex.GetPixel(x, y);
            float outA = src.a + dst.a * (1f - src.a);
            if (outA <= 0.0001f) return;
            Color outC = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
            outC.a = outA;
            tex.SetPixel(x, y, outC);
        }

        /// <summary>Botão / barra arredondada 9-slice com borda delicada e brilho superior suave.</summary>
        public static Sprite GetRoundedPillSprite()
        {
            if (cachedRoundedPill != null) return cachedRoundedPill;
            int w = 64, h = 64, r = 20;
            Texture2D tex = Blank(w, h);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - (w - 1) * 0.5f) - (w * 0.5f - r - 2));
                    float dy = Mathf.Max(0f, Mathf.Abs(y - (h - 1) * 0.5f) - (h * 0.5f - r - 2));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        float aa = Mathf.Clamp01((r - dist) * 1.4f);
                        bool isBorder = dist >= r - 2.2f;
                        float grad = Mathf.Lerp(0.88f, 1.0f, y / (float)h);
                        Color c = isBorder
                            ? new Color(1f, 0.98f, 0.90f, 0.95f * aa)
                            : new Color(grad, grad, grad, aa);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            cachedRoundedPill = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(22, 22, 22, 22));
            return cachedRoundedPill;
        }

        /// <summary>Cartão de livro ilustrado arredondado 9-slice com moldura suave.</summary>
        public static Sprite GetRoundedCardSprite()
        {
            if (cachedRoundedCard != null) return cachedRoundedCard;
            int w = 64, h = 64, r = 14;
            Texture2D tex = Blank(w, h);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - (w - 1) * 0.5f) - (w * 0.5f - r - 2));
                    float dy = Mathf.Max(0f, Mathf.Abs(y - (h - 1) * 0.5f) - (h * 0.5f - r - 2));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        float aa = Mathf.Clamp01((r - dist) * 1.4f);
                        bool isBorder = dist >= r - 2.4f;
                        Color c = isBorder
                            ? new Color(0.82f, 0.74f, 0.58f, 0.96f * aa)
                            : new Color(1f, 1f, 1f, aa);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            cachedRoundedCard = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            return cachedRoundedCard;
        }

        /// <summary>Ícones ilustrados reais (substituem símbolos Unicode quebrados).</summary>
        public static Sprite GetIcon(UIIconKind kind)
        {
            if (iconCache.TryGetValue(kind, out Sprite cached) && cached != null) return cached;

            int s = 48;
            Texture2D tex = Blank(s, s);
            Color white = Color.white;

            switch (kind)
            {
                case UIIconKind.Star:
                    for (int y = 0; y < s; y++)
                    {
                        for (int x = 0; x < s; x++)
                        {
                            float dx = x - 24f, dy = y - 24f;
                            float rad = Mathf.Sqrt(dx * dx + dy * dy);
                            float ang = Mathf.Atan2(dy, dx);
                            float starR = 11.5f + 6.5f * Mathf.Cos(ang * 5f - Mathf.PI * 0.5f);
                            if (rad <= starR)
                                tex.SetPixel(x, y, rad < 6f ? new Color(1f, 0.98f, 0.78f, 1f) : new Color(1f, 0.84f, 0.18f, 1f));
                        }
                    }
                    break;

                case UIIconKind.WaterDrop:
                    Circle(tex, 24, 18, 11, new Color(0.30f, 0.82f, 1f, 1f));
                    for (int y = 20; y < 42; y++)
                    {
                        float t = (y - 20f) / 22f;
                        float halfW = (1f - t) * 10.5f;
                        for (int x = Mathf.FloorToInt(24 - halfW); x <= Mathf.CeilToInt(24 + halfW); x++)
                            tex.SetPixel(x, y, new Color(0.30f, 0.82f, 1f, 1f));
                    }
                    Circle(tex, 20, 20, 3.2f, new Color(0.88f, 0.98f, 1f, 0.95f));
                    break;

                case UIIconKind.BackArrow:
                    Line(tex, 36, 24, 13, 24, 5.5f, white);
                    Line(tex, 13, 24, 23, 35, 5.5f, white);
                    Line(tex, 13, 24, 23, 13, 5.5f, white);
                    break;

                case UIIconKind.Stroll:
                    Line(tex, 12, 24, 34, 24, 5.5f, white);
                    Line(tex, 34, 24, 24, 34, 5.5f, white);
                    Line(tex, 34, 24, 24, 14, 5.5f, white);
                    break;

                case UIIconKind.Pedal:
                    // Duas rodas de bicicleta + quadro estilizado!
                    Circle(tex, 14, 16, 7, white);
                    Circle(tex, 14, 16, 4, new Color(0.2f, 0.2f, 0.2f, 0.6f));
                    Circle(tex, 34, 16, 7, white);
                    Circle(tex, 34, 16, 4, new Color(0.2f, 0.2f, 0.2f, 0.6f));
                    Line(tex, 14, 16, 24, 28, 3.5f, white);
                    Line(tex, 24, 28, 34, 16, 3.5f, white);
                    Line(tex, 20, 32, 27, 32, 3.5f, white);
                    Line(tex, 30, 30, 35, 32, 3.5f, white);
                    break;

                case UIIconKind.Jump:
                    Line(tex, 24, 10, 24, 36, 5.5f, white);
                    Line(tex, 24, 36, 13, 25, 5.5f, white);
                    Line(tex, 24, 36, 35, 25, 5.5f, white);
                    Line(tex, 14, 10, 34, 10, 4.0f, new Color(1f, 1f, 1f, 0.75f));
                    break;

                case UIIconKind.Basket:
                    // Cesta de vime com frutinha
                    for (int y = 10; y <= 24; y++)
                        for (int x = 11; x <= 37; x++)
                            tex.SetPixel(x, y, new Color(0.96f, 0.84f, 0.56f, 1f));
                    Line(tex, 14, 24, 24, 38, 3.8f, white);
                    Line(tex, 34, 24, 24, 38, 3.8f, white);
                    Circle(tex, 20, 27, 5, new Color(1f, 0.44f, 0.46f, 1f));
                    Circle(tex, 29, 27, 5, new Color(0.42f, 0.86f, 0.44f, 1f));
                    break;

                case UIIconKind.Book:
                    // Livro aberto
                    for (int y = 12; y <= 34; y++)
                    {
                        for (int x = 8; x <= 22; x++) tex.SetPixel(x, y, new Color(1f, 0.96f, 0.86f, 1f));
                        for (int x = 26; x <= 40; x++) tex.SetPixel(x, y, new Color(1f, 0.96f, 0.86f, 1f));
                    }
                    Line(tex, 24, 11, 24, 35, 3f, new Color(0.70f, 0.56f, 0.36f, 1f));
                    break;

                case UIIconKind.Workshop:
                    // Engrenagem dourada elegante, perfeitamente simétrica e centralizada em (24, 24)
                    Color gearGold = new Color(1f, 0.92f, 0.48f, 1f);
                    for (int a = 0; a < 8; a++)
                    {
                        float rad = a * Mathf.PI / 4f;
                        int gx = Mathf.RoundToInt(24f + Mathf.Cos(rad) * 12f);
                        int gy = Mathf.RoundToInt(24f + Mathf.Sin(rad) * 12f);
                        Circle(tex, gx, gy, 4, gearGold);
                    }
                    Circle(tex, 24, 24, 11, gearGold);
                    Circle(tex, 24, 24, 6, new Color(0.44f, 0.28f, 0.58f, 1f));
                    Circle(tex, 24, 24, 3, white);
                    break;

                case UIIconKind.Bell:
                    // Sino dourado fofo com badalo e brilho
                    Color bellGold = new Color(1f, 0.88f, 0.32f, 1f);
                    Color bellDark = new Color(0.82f, 0.60f, 0.16f, 1f);
                    for (int y = 16; y <= 34; y++)
                    {
                        float t = (y - 16f) / 18f;
                        int halfW = Mathf.RoundToInt(Mathf.Lerp(12f, 4f, t));
                        for (int x = 24 - halfW; x <= 24 + halfW; x++)
                            tex.SetPixel(x, y, (x < 23) ? bellDark : bellGold);
                    }
                    for (int x = 11; x <= 37; x++)
                    {
                        tex.SetPixel(x, 15, bellDark);
                        tex.SetPixel(x, 16, bellGold);
                    }
                    Circle(tex, 24, 12, 3, new Color(0.65f, 0.45f, 0.12f, 1f));
                    Circle(tex, 27, 28, 2, white);
                    break;

                case UIIconKind.CloseX:
                    Line(tex, 14, 14, 34, 34, 5f, white);
                    Line(tex, 34, 14, 14, 34, 5f, white);
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
            iconCache[kind] = sp;
            return sp;
        }
    }
}
