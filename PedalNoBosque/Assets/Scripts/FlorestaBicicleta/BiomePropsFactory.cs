using System.Collections.Generic;
using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Fábrica procedural com cache para os elementos visuais dos 5 biomas da Etapa 2:
    /// Campo Florido, Colinas, Região do Rio (água + pontes de madeira),
    /// Trilha da Floresta e Pequena Vila (casinhas, postes, placas de km).
    /// </summary>
    public static class BiomePropsFactory
    {
        private static readonly Dictionary<int, Sprite> flowerCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> cottageCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> midTreeCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> fgFlowerCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Sprite> organicTreeCache = new Dictionary<int, Sprite>();
        private static Sprite cachedHill;
        private static Sprite cachedRiver;
        private static Sprite cachedRiverShimmer;
        private static Sprite cachedBridge;
        private static Sprite cachedRiverBank;
        private static Sprite cachedFish;
        private static Sprite cachedWaterfall;
        private static Sprite cachedBush;
        private static Sprite cachedLamp;
        private static Sprite cachedSign;
        private static Sprite cachedBench;
        private static Sprite cachedBunting;
        private static Sprite cachedFgFern;
        private static Sprite cachedFgMushroom;
        private static Sprite cachedFgStoneRoot;
        private static Sprite cachedWillowTree;
        private static Sprite cachedRiverIslet;
        private static Sprite cachedFruitTree;
        private static Sprite cachedMoonFlower;
        private static Sprite cachedGlowMushroom;
        private static Sprite cachedOakTree;
        private static Sprite cachedMoonTree;
        private static Sprite cachedAncientConifer;
        private static Sprite cachedContactShadow;

        /// <summary>
        /// Sombra de contato suave elíptica no chão para ancoragem realista de personagens, banco, barraca e árvores.
        /// </summary>
        public static Sprite CreateContactShadowSprite()
        {
            if (cachedContactShadow != null) return cachedContactShadow;
            int w = 64, h = 24;
            Texture2D tex = CreateBlankTex(w, h);
            float rx = w * 0.48f;
            float ry = h * 0.44f;
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            Color shadowCol = new Color(0.04f, 0.12f, 0.08f, 0.52f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= 1f)
                    {
                        float falloff = 1f - Mathf.Sqrt(d2);
                        float a = Mathf.Pow(falloff, 0.65f) * shadowCol.a;
                        tex.SetPixel(x, y, new Color(shadowCol.r, shadowCol.g, shadowCol.b, a));
                    }
                }
            }
            tex.Apply();
            cachedContactShadow = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
            return cachedContactShadow;
        }

        public static GameObject AddContactShadow(Transform parent, Vector3 localOffset, Vector2 scale, int sortingOrder = -1)
        {
            GameObject shadow = new GameObject("ContactShadow");
            shadow.transform.SetParent(parent, false);
            shadow.transform.localPosition = localOffset;
            shadow.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = CreateContactShadowSprite();
            sr.sortingOrder = sortingOrder;
            return shadow;
        }

        private static Texture2D CreateBlankTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            int minX = Mathf.Max(0, x0);
            int maxX = Mathf.Min(tex.width - 1, x1);
            int minY = Mathf.Max(0, y0);
            int maxY = Mathf.Min(tex.height - 1, y1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    tex.SetPixel(x, y, col);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2) tex.SetPixel(x, y, col);
                }
            }
        }

        private static void FillEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f) tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawCurvedBranch(
            Texture2D tex, Vector2 start, Vector2 control, Vector2 end,
            float startRadius, float endRadius, Color shadow, Color light)
        {
            const int steps = 28;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float oneMinusT = 1f - t;
                Vector2 point = oneMinusT * oneMinusT * start
                    + 2f * oneMinusT * t * control
                    + t * t * end;
                int radius = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(startRadius, endRadius, t)));
                int x = Mathf.RoundToInt(point.x);
                int y = Mathf.RoundToInt(point.y);
                FillCircle(tex, x, y, radius, shadow);
                if (radius > 2)
                    FillCircle(tex, x - 1, y + 1, radius - 2, light);
            }
        }

        private static void ScatterCanopyHighlights(
            Texture2D tex, int centerX, int centerY, int radiusX, int radiusY,
            int seed, Color leafLight, Color leafShadow)
        {
            System.Random random = new System.Random(seed);
            int placed = 0;
            for (int attempt = 0; attempt < 100 && placed < 34; attempt++)
            {
                int x = centerX + random.Next(-radiusX, radiusX + 1);
                int y = centerY + random.Next(-radiusY, radiusY + 1);
                float nx = (x - centerX) / (float)radiusX;
                float ny = (y - centerY) / (float)radiusY;
                if (nx * nx + ny * ny > 0.76f) continue;

                bool sunlit = y > centerY || (placed % 4 == 0);
                Color mark = sunlit ? leafLight : leafShadow;
                int rx = random.Next(2, 5);
                int ry = random.Next(2, 4);
                FillEllipse(tex, x, y, rx, ry, mark);
                placed++;
            }
        }

        private static void ScatterPineNeedles(Texture2D tex, int seed, Color light, Color shadow)
        {
            int[] centersY = { 50, 78, 102, 124, 144, 160 };
            int[] spreads = { 54, 46, 36, 26, 16, 9 };
            System.Random random = new System.Random(seed);
            for (int tier = 0; tier < centersY.Length; tier++)
            {
                int count = 12 + tier * 2;
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)(random.NextDouble() * Mathf.PI);
                    float r = (float)random.NextDouble() * spreads[tier] * 0.75f;
                    int x = 90 + Mathf.RoundToInt(Mathf.Cos(angle) * r);
                    int y = centersY[tier] + Mathf.RoundToInt(Mathf.Sin(angle) * (spreads[tier] * 0.28f)) - random.Next(0, 6);
                    Color col = (random.Next(0, 4) == 0) ? shadow : light;
                    FillCircle(tex, x, y, random.Next(1, 3), col);
                }
            }
        }

        private static void FillTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color col)
        {
            int minX = Mathf.Max(0, Mathf.Min(x0, Mathf.Min(x1, x2)));
            int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x0, Mathf.Max(x1, x2)));
            int minY = Mathf.Max(0, Mathf.Min(y0, Mathf.Min(y1, y2)));
            int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y0, Mathf.Max(y1, y2)));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d1 = (x - x1) * (y0 - y1) - (x0 - x1) * (y - y1);
                    float d2 = (x - x2) * (y1 - y2) - (x1 - x2) * (y - y2);
                    float d3 = (x - x0) * (y2 - y0) - (x2 - x0) * (y - y0);
                    if (!(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0))))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawOrganicFoliageLobe(Texture2D tex, int cx, int cy, int rx, int ry, Color baseCol, Color highCol, Color shadowCol)
        {
            for (int y = cy - ry - 2; y <= cy + ry + 2; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx - 2; x <= cx + rx + 2; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float dx = (x - cx) / (float)Mathf.Max(1, rx);
                    float dy = (y - cy) / (float)Mathf.Max(1, ry);
                    float angle = Mathf.Atan2(dy, dx);
                    float radiusNoise = 1f + Mathf.Sin(angle * 7f + cx * 0.1f) * 0.08f + Mathf.Cos(angle * 4f) * 0.06f;
                    float d2 = (dx * dx + dy * dy) / (radiusNoise * radiusNoise);
                    if (d2 <= 1f)
                    {
                        float tHeight = Mathf.Clamp01((y - (cy - ry)) / (float)(ry * 2));
                        float lightFactor = tHeight * 0.68f + (1f - (dx + 1f) * 0.5f) * 0.32f;
                        Color col;
                        if (lightFactor > 0.62f)
                            col = Color.Lerp(baseCol, highCol, (lightFactor - 0.62f) / 0.38f);
                        else
                            col = Color.Lerp(shadowCol, baseCol, lightFactor / 0.62f);

                        if ((x * 7 + y * 13) % 9 == 0)
                            col = Color.Lerp(col, highCol, 0.22f);
                        else if ((x * 11 + y * 5) % 11 == 0)
                            col = Color.Lerp(col, shadowCol, 0.28f);

                        if (d2 > 0.82f)
                        {
                            float edgeA = Mathf.Clamp01((1f - Mathf.Sqrt(d2)) / 0.18f);
                            col.a *= edgeA;
                        }
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static void DrawPineBranchTier(Texture2D tex, int cx, int cy, int spreadW, int height, Color darkNeedle, Color midNeedle, Color lightNeedle)
        {
            // Copa orgânica de pinheiro com silhueta cônica curvada e tufos de agulhas suaves
            int yBottom = cy - Mathf.RoundToInt(height * 0.65f);
            int yTop = cy + Mathf.RoundToInt(height * 0.35f);

            // Preenchimento de massa orgânica com bordas atenuadas (sem linhas de varredura nem degraus rígidos)
            for (int y = yBottom; y <= yTop; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                float yt = (float)(y - yBottom) / Mathf.Max(1, yTop - yBottom);
                // Silhueta arqueada com pontas caídas
                float widthFactor = Mathf.Pow(1f - yt, 0.75f);
                float halfW = spreadW * widthFactor;

                for (int x = Mathf.RoundToInt(cx - halfW); x <= Mathf.RoundToInt(cx + halfW); x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / Mathf.Max(1f, halfW);
                    float edgeDist = 1f - Mathf.Abs(nx);

                    // Sombra na base, luz suave na parte superior exposta ao sol
                    float lightT = Mathf.Clamp01(yt * 0.7f + (1f - Mathf.Abs(nx)) * 0.3f);
                    Color baseCol = Color.Lerp(darkNeedle, midNeedle, lightT);
                    if (yt > 0.65f && edgeDist > 0.25f)
                        baseCol = Color.Lerp(baseCol, lightNeedle, (yt - 0.65f) / 0.35f);

                    tex.SetPixel(x, y, baseCol);
                }
            }

            // Tufos de agulhas pendentes nas pontas para contorno vivo e natural
            int numTips = Mathf.Clamp(spreadW / 6, 6, 12);
            for (int i = 0; i <= numTips; i++)
            {
                float t = i / (float)numTips;
                float xOffset = Mathf.Lerp(-spreadW, spreadW, t);
                int tipX = Mathf.RoundToInt(cx + xOffset);
                float droop = (1f - (xOffset * xOffset) / (spreadW * spreadW * 1.05f)) * (height * 0.35f);
                int tipY = Mathf.RoundToInt(yBottom - droop * 0.6f);

                FillEllipse(tex, tipX, tipY, Mathf.Max(2, spreadW / 14), Mathf.Max(2, height / 10), midNeedle);
                FillCircle(tex, tipX, tipY - 1, 1, darkNeedle);
            }
        }

        public static Sprite LoadParkBenchSprite()
        {
            if (cachedBench != null) return cachedBench;
            Texture2D benchTex = Resources.Load<Texture2D>("Floresta/banco_praca");
            cachedBench = benchTex != null
                ? Sprite.Create(benchTex, new Rect(0, 0, benchTex.width, benchTex.height), new Vector2(0.5f, 0.2288f), benchTex.height / 2.65f)
                : ParkSpriteFactory.CreateParkBenchSprite();
            return cachedBench;
        }

        /// <summary>Canteiro colorido de flores silvestres para o Campo Florido.</summary>
        public static Sprite CreateFlowerPatchSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 3;
            if (flowerCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 96, h = 48;
            Texture2D tex = CreateBlankTex(w, h);

            Color grassDark = new Color(0.18f, 0.56f, 0.24f, 1f);
            Color grassLight = new Color(0.32f, 0.72f, 0.30f, 1f);
            Color[] petals =
            {
                new Color(1f, 0.42f, 0.68f, 1f),
                new Color(1f, 0.86f, 0.24f, 1f),
                new Color(0.68f, 0.48f, 1f, 1f),
                new Color(1f, 0.96f, 0.98f, 1f),
                new Color(1f, 0.58f, 0.24f, 1f)
            };

            int[] fx = { 18, 34, 50, 66, 80 };
            int[] fy = { 24, 30, 26, 31, 23 };
            for (int i = 0; i < fx.Length; i++)
            {
                // Caule
                FillRect(tex, fx[i] - 1, 12, fx[i] + 1, fy[i] - 3, grassDark);
                    FillEllipse(tex, fx[i] + 2, 16, 3, 1, grassLight);
                // Pétalas
                Color p = petals[(i + key * 2) % petals.Length];
                FillCircle(tex, fx[i] - 2, fy[i], 2, p);
                FillCircle(tex, fx[i] + 2, fy[i], 2, p);
                FillCircle(tex, fx[i], fy[i] + 2, 2, p);
                FillCircle(tex, fx[i], fy[i] - 2, 2, p);
                // Miolo dourado
                FillCircle(tex, fx[i], fy[i], 1, new Color(1f, 0.90f, 0.22f, 1f));
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 42f);
            flowerCache[key] = sp;
            return sp;
        }

        /// <summary>Silhueta suave de colina verde para o bioma Colinas.</summary>
        public static Sprite CreateRollingHillSprite()
        {
            if (cachedHill != null) return cachedHill;
            int w = 192, h = 96;
            Texture2D tex = CreateBlankTex(w, h);

            FillEllipse(tex, 68, 8, 66, 60, new Color(0.26f, 0.62f, 0.30f, 0.88f));
            FillEllipse(tex, 132, 4, 62, 50, new Color(0.34f, 0.72f, 0.34f, 0.92f));
            FillEllipse(tex, 96, 0, 88, 34, new Color(0.42f, 0.78f, 0.36f, 0.95f));

            tex.Apply();
            cachedHill = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.02f), 26f);
            return cachedHill;
        }

        /// <summary>Faixa de rio cristalino estilo livro ilustrado (mesma paleta de água de coluna_agua.png e margens de bg_floresta.png).</summary>
        public static Sprite CreateRiverWaterSprite()
        {
            if (cachedRiver != null) return cachedRiver;
            int w = 640, h = 152;
            Texture2D tex = CreateBlankTex(w, h);

            Color inkOutline = new Color(0.24f, 0.14f, 0.10f, 0.92f);
            Color deepChannel = new Color(0.16f, 0.52f, 0.78f, 0.97f);
            Color midWater = new Color(0.28f, 0.72f, 0.94f, 0.96f);
            Color shallowCyan = new Color(0.50f, 0.86f, 0.98f, 0.95f);
            Color foamWhite = new Color(0.92f, 0.99f, 1f, 0.92f);
            Color bankGrassDark = new Color(0.28f, 0.48f, 0.16f, 0.98f);
            Color bankGrassLight = new Color(0.48f, 0.72f, 0.24f, 0.98f);
            Color stoneDark = new Color(0.62f, 0.58f, 0.52f, 1f);
            Color stoneLight = new Color(0.84f, 0.80f, 0.72f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = y / (float)(h - 1);
                // Margens curvas naturais que integram o rio à estrada
                float curveShift = Mathf.Sin(ny * Mathf.PI) * 18f + Mathf.Sin(ny * 7f) * 4f;
                float leftBank = 38f + curveShift;
                float rightBank = (w - 39f) - curveShift;

                // Gradiente vertical rico com sombra sob o arco da ponte
                Color waterRow = ny < 0.55f
                    ? Color.Lerp(deepChannel, midWater, ny / 0.55f)
                    : Color.Lerp(midWater, shallowCyan, (ny - 0.55f) / 0.45f);

                float vEdgeFade = Mathf.Clamp01(Mathf.Min(y, h - 1 - y) / 10f);

                for (int x = 0; x < w; x++)
                {
                    float distLeft = x - leftBank;
                    float distRight = rightBank - x;
                    float minBankDist = Mathf.Min(distLeft, distRight);

                    if (minBankDist < -14f)
                    {
                        continue;
                    }
                    if (minBankDist < 0f)
                    {
                        // Talude de grama e terra na borda da estrada
                        float bt = Mathf.Clamp01((minBankDist + 14f) / 14f);
                        Color bc = Color.Lerp(bankGrassLight, bankGrassDark, bt);
                        bc.a *= bt * vEdgeFade;
                        tex.SetPixel(x, y, bc);
                    }
                    else if (minBankDist < 6f)
                    {
                        // Espuma alva na margem d'água
                        Color fc = Color.Lerp(foamWhite, shallowCyan, minBankDist / 6f);
                        fc.a *= vEdgeFade;
                        tex.SetPixel(x, y, fc);
                    }
                    else
                    {
                        // Ondulação suave de aquarela no canal
                        float centerFactor = Mathf.Clamp01(minBankDist / 64f);
                        float ripple = Mathf.Sin(x * 0.055f + y * 0.14f) * 0.035f;
                        Color c = Color.Lerp(shallowCyan, waterRow, centerFactor);
                        c.r = Mathf.Clamp01(c.r + ripple);
                        c.g = Mathf.Clamp01(c.g + ripple);
                        c.b = Mathf.Clamp01(c.b + ripple);
                        c.a *= vEdgeFade;
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Fitas curvas de correnteza luminosa (estilo coluna_agua.png)
            for (int i = 0; i < 18; i++)
            {
                int rx = 74 + (i * 43) % (w - 180);
                int ry = 18 + (i * 19) % (h - 36);
                int len = 28 + (i * 9) % 34;
                for (int lx = 0; lx < len; lx++)
                {
                    int px = rx + lx;
                    int py = ry + Mathf.RoundToInt(Mathf.Sin(lx * 0.22f + i) * 2.2f);
                    if (px > 64 && px < w - 64 && py > 8 && py < h - 8)
                    {
                        Color cur = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(cur, foamWhite, 0.72f));
                        if (py + 1 < h - 8)
                            tex.SetPixel(px, py + 1, Color.Lerp(tex.GetPixel(px, py + 1), foamWhite, 0.45f));
                    }
                }
            }

            // Pedras polidas de rio contornadas nas duas margens
            int[] sx = { 54, 72, 48, w - 54, w - 72, w - 48 };
            int[] sy = { 34, 62, 108, 38, 68, 104 };
            int[] srx = { 16, 13, 15, 16, 13, 15 };
            int[] sry = { 10, 8, 9, 10, 8, 9 };
            for (int i = 0; i < sx.Length; i++)
            {
                FillEllipse(tex, sx[i], sy[i], srx[i] + 2, sry[i] + 2, inkOutline);
                FillEllipse(tex, sx[i], sy[i], srx[i], sry[i], stoneDark);
                FillEllipse(tex, sx[i] - 2, sy[i] + 2, srx[i] - 3, sry[i] - 2, stoneLight);
            }

            // Vitórias-régias delicadas com flor de lótus
            int[] lxArr = { 138, w - 142 };
            int[] lyArr = { 44, 48 };
            for (int i = 0; i < lxArr.Length; i++)
            {
                FillEllipse(tex, lxArr[i], lyArr[i], 18, 8, inkOutline);
                FillEllipse(tex, lxArr[i], lyArr[i], 16, 6, new Color(0.26f, 0.64f, 0.30f, 1f));
                FillEllipse(tex, lxArr[i] - 3, lyArr[i] + 1, 10, 4, new Color(0.42f, 0.78f, 0.36f, 1f));
                FillCircle(tex, lxArr[i] + 2, lyArr[i] + 3, 5, new Color(1f, 0.76f, 0.86f, 1f));
                FillCircle(tex, lxArr[i] + 2, lyArr[i] + 3, 2, new Color(1f, 0.94f, 0.42f, 1f));
            }

            tex.Apply();
            cachedRiver = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 72f);
            return cachedRiver;
        }

        /// <summary>Camada de brilho móvel e reflexos cristalinos da correnteza do rio.</summary>
        public static Sprite CreateRiverShimmerSprite()
        {
            if (cachedRiverShimmer != null) return cachedRiverShimmer;
            int w = 520, h = 110;
            Texture2D tex = CreateBlankTex(w, h);
            Color foam = new Color(0.95f, 0.99f, 1f, 0.78f);
            for (int i = 0; i < 20; i++)
            {
                int rx = 32 + (i * 41) % (w - 96);
                int ry = 14 + (i * 17) % (h - 28);
                int span = 22 + (i * 9) % 26;
                for (int sx = 0; sx < span; sx++)
                {
                    int px = rx + sx;
                    if (px >= w - 16) break;
                    int py = ry + Mathf.RoundToInt(Mathf.Sin(sx * 0.25f) * 1.8f);
                    if (py >= 2 && py < h - 2)
                    {
                        tex.SetPixel(px, py, foam);
                        tex.SetPixel(px, py + 1, new Color(1f, 1f, 1f, 0.45f));
                    }
                }
            }
            tex.Apply();
            cachedRiverShimmer = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 72f);
            return cachedRiverShimmer;
        }

        /// <summary>
        /// Ponte de madeira ilustrada em alta resolução (mesmo traço, contorno escuro e paleta de madeira
        /// de banco_praca.png e barraca_cozinha.png), perfeitamente alinhada ao leito da estrada.
        /// </summary>
        public static Sprite CreateWoodenBridgeSprite()
        {
            if (cachedBridge != null) return cachedBridge;
            int w = 640, h = 196;
            Texture2D tex = CreateBlankTex(w, h);

            // Paleta idêntica a banco_praca.png e barraca_cozinha.png
            Color ink = new Color(0.23f, 0.11f, 0.08f, 1f);
            Color woodShadow = new Color(0.46f, 0.24f, 0.11f, 1f);
            Color woodDark = new Color(0.65f, 0.37f, 0.17f, 1f);
            Color woodMid = new Color(0.81f, 0.51f, 0.25f, 1f);
            Color woodLight = new Color(0.91f, 0.65f, 0.36f, 1f);
            Color woodDeck = new Color(0.86f, 0.60f, 0.33f, 1f);
            Color ironDark = new Color(0.20f, 0.18f, 0.24f, 1f);
            Color ironLight = new Color(0.46f, 0.44f, 0.52f, 1f);
            Color stoneDark = new Color(0.62f, 0.57f, 0.50f, 1f);
            Color stoneMid = new Color(0.78f, 0.74f, 0.66f, 1f);
            Color stoneLight = new Color(0.91f, 0.87f, 0.80f, 1f);
            Color grassDark = new Color(0.26f, 0.48f, 0.16f, 1f);
            Color grassLight = new Color(0.48f, 0.72f, 0.24f, 1f);

            // 1. Arco estrutural inferior de madeira sob o tablado
            for (int x = 56; x <= w - 56; x++)
            {
                float t = (x - 56f) / (w - 112f);
                int subArch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 32f);
                int yBot = 36 + subArch;
                int yTop = 50 + subArch;
                FillRect(tex, x, yBot - 2, x, yTop + 2, ink);
                FillRect(tex, x, yBot, x, yTop, woodDark);
                FillRect(tex, x, yTop - 4, x, yTop, woodMid);
            }

            // 2. Quatro pilares robustos de carvalho mergulhando na água com ondulações na base
            int[] pillars = { 104, 236, 404, 536 };
            for (int i = 0; i < pillars.Length; i++)
            {
                int px = pillars[i];
                float t = (px - 24f) / (w - 48f);
                int deckY = 74 + Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 14f);

                FillRect(tex, px - 12, 10, px + 12, deckY, ink);
                FillRect(tex, px - 10, 12, px + 10, deckY - 1, woodDark);
                FillRect(tex, px - 6, 12, px + 4, deckY - 1, woodMid);
                FillRect(tex, px - 3, 12, px + 1, deckY - 1, woodLight);
                // Sombra molhada junto à água e anel de espuma branca
                FillRect(tex, px - 10, 12, px + 10, 24, woodShadow);
                FillEllipse(tex, px, 12, 16, 4, new Color(0.92f, 0.99f, 1f, 0.85f));
            }

            // 3. Tablado arqueado principal (com superfície de rolagem e viga lateral esculpida)
            for (int x = 18; x < w - 18; x++)
            {
                float t = (x - 18f) / (w - 36f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 14f);
                int yFasciaBot = 68 + arch;
                int yFasciaTop = 90 + arch;
                int yDeckTop = 104 + arch;

                // Contornos escuros de livro ilustrado
                FillRect(tex, x, yFasciaBot - 2, x, yDeckTop + 2, ink);

                // Piso superior da passarela onde a bicicleta pedala
                bool deckPlankSep = (x % 22 == 0);
                for (int y = yFasciaTop + 1; y <= yDeckTop; y++)
                {
                    if (deckPlankSep)
                    {
                        tex.SetPixel(x, y, woodShadow);
                    }
                    else
                    {
                        float dt = (y - yFasciaTop) / (float)(yDeckTop - yFasciaTop);
                        Color dc = Color.Lerp(woodDeck, woodLight, dt * 0.65f);
                        tex.SetPixel(x, y, dc);
                    }
                }
                FillRect(tex, x, yFasciaTop - 1, x, yFasciaTop + 1, ink);

                // Viga lateral de madeira com veios orgânicos
                bool fasciaSep = (x % 44 == 0);
                for (int y = yFasciaBot; y < yFasciaTop; y++)
                {
                    if (fasciaSep)
                    {
                        tex.SetPixel(x, y, ink);
                    }
                    else
                    {
                        float grain = Mathf.Sin(x * 0.06f + y * 0.45f);
                        Color wc = (y > yFasciaTop - 5) ? woodLight : ((y < yFasciaBot + 4 || grain < -0.55f) ? woodDark : woodMid);
                        tex.SetPixel(x, y, wc);
                    }
                }
            }

            // 4. Postes verticais trabalhados com ferragens pretas (estilo banco_praca.png)
            for (int px = 48; px <= w - 48; px += 90)
            {
                float t = (px - 18f) / (w - 36f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 14f);
                int pBot = 92 + arch;
                int pTop = 160 + arch;

                FillRect(tex, px - 8, pBot, px + 8, pTop, ink);
                FillRect(tex, px - 6, pBot + 2, px + 6, pTop - 2, woodDark);
                FillRect(tex, px - 4, pBot + 2, px + 3, pTop - 2, woodMid);
                FillRect(tex, px - 2, pBot + 2, px + 1, pTop - 2, woodLight);

                // Braçadeira de ferro forjado na base do poste
                FillRect(tex, px - 8, pBot, px + 8, pBot + 10, ink);
                FillRect(tex, px - 6, pBot + 2, px + 6, pBot + 8, ironDark);
                FillCircle(tex, px, pBot + 5, 2, ironLight);

                // Topo arredondado esculpido
                FillCircle(tex, px, pTop + 3, 8, ink);
                FillCircle(tex, px, pTop + 3, 6, woodMid);
                FillCircle(tex, px - 1, pTop + 5, 3, woodLight);
            }

            // 5. Corrimãos horizontais curvos com veios de madeira e contorno escuro
            for (int x = 36; x <= w - 36; x++)
            {
                float t = (x - 18f) / (w - 36f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 14f);

                // Corrimão superior principal
                int r1Bot = 148 + arch;
                int r1Top = 158 + arch;
                FillRect(tex, x, r1Bot - 2, x, r1Top + 2, ink);
                FillRect(tex, x, r1Bot, x, r1Top, woodMid);
                FillRect(tex, x, r1Top - 3, x, r1Top, woodLight);
                FillRect(tex, x, r1Bot, x, r1Bot + 2, woodDark);

                // Travessa intermediária
                int r2Bot = 122 + arch;
                int r2Top = 129 + arch;
                FillRect(tex, x, r2Bot - 2, x, r2Top + 2, ink);
                FillRect(tex, x, r2Bot, x, r2Top, woodDark);
                FillRect(tex, x, r2Top - 2, x, r2Top, woodMid);
            }

            // 6. Cabeceiras de pedra esculpida e tufos de grama integrando às margens
            for (int side = 0; side < 2; side++)
            {
                int cx = side == 0 ? 32 : w - 32;
                FillEllipse(tex, cx, 78, 28, 22, ink);
                FillEllipse(tex, cx, 78, 25, 19, stoneDark);
                FillEllipse(tex, cx, 81, 21, 15, stoneMid);
                FillEllipse(tex, cx + (side == 0 ? -4 : 4), 85, 13, 9, stoneLight);

                // Tufos de grama na base da cabeceira
                FillEllipse(tex, cx, 64, 24, 9, ink);
                FillEllipse(tex, cx, 64, 22, 7, grassDark);
                FillEllipse(tex, cx, 66, 16, 5, grassLight);
            }

            tex.Apply();
            cachedBridge = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.44f), 72f);
            return cachedBridge;
        }

        /// <summary>Margem natural do rio com pedras polidas sombreadas, taboas aveludadas e flores.</summary>
        public static Sprite CreateRiverBankRocksAndReedsSprite()
        {
            if (cachedRiverBank != null) return cachedRiverBank;
            int w = 128, h = 84;
            Texture2D tex = CreateBlankTex(w, h);

            Color reedDark = new Color(0.16f, 0.44f, 0.20f, 1f);
            Color reedLight = new Color(0.32f, 0.68f, 0.28f, 1f);
            Color cattailDark = new Color(0.36f, 0.20f, 0.10f, 1f);
            Color cattailMid = new Color(0.52f, 0.30f, 0.16f, 1f);
            Color rockDark = new Color(0.38f, 0.42f, 0.46f, 1f);
            Color rockMid = new Color(0.56f, 0.60f, 0.64f, 1f);
            Color rockLight = new Color(0.74f, 0.78f, 0.82f, 1f);

            // Juncos ondulantes e taboas com folhagem suave
            int[] rx = { 26, 42, 58, 82, 102 };
            int[] rh = { 64, 74, 58, 70, 52 };
            for (int i = 0; i < rx.Length; i++)
            {
                int bx = rx[i];
                int topH = rh[i];
                DrawCurvedBranch(tex, new Vector2(bx, 10), new Vector2(bx + (i % 2 == 0 ? 3 : -3), topH * 0.5f),
                    new Vector2(bx + (i % 2 == 0 ? 5 : -4), topH), 2.5f, 1f, reedDark, reedLight);
                if (i % 2 == 0)
                {
                    // Espiga aveludada de taboa
                    FillEllipse(tex, bx + 4, topH - 10, 4, 9, cattailDark);
                    FillEllipse(tex, bx + 3, topH - 10, 2, 7, cattailMid);
                }
            }

            // Pedras de rio polidas com volume 3D suave
            FillEllipse(tex, 44, 22, 22, 14, rockDark);
            FillEllipse(tex, 42, 25, 17, 10, rockMid);
            FillEllipse(tex, 39, 29, 10, 6, rockLight);

            FillEllipse(tex, 84, 18, 19, 12, rockDark);
            FillEllipse(tex, 82, 21, 14, 8, rockMid);
            FillEllipse(tex, 79, 24, 8, 5, rockLight);

            // Florzinhas delicadas não-me-esqueças às margens d'água
            FillCircle(tex, 60, 32, 4, new Color(0.36f, 0.72f, 0.98f, 1f));
            FillCircle(tex, 60, 32, 2, new Color(1f, 0.92f, 0.35f, 1f));

            FillCircle(tex, 98, 28, 4, new Color(1f, 0.62f, 0.78f, 1f));
            FillCircle(tex, 98, 28, 2, new Color(1f, 0.92f, 0.35f, 1f));

            tex.Apply();
            cachedRiverBank = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 32f);
            return cachedRiverBank;
        }

        /// <summary>Pequeno peixe saltitante do rio.</summary>
        public static Sprite CreateFishSprite()
        {
            if (cachedFish != null) return cachedFish;
            int w = 40, h = 24;
            Texture2D tex = CreateBlankTex(w, h);
            Color body = new Color(0.98f, 0.62f, 0.22f, 1f);
            Color fin = new Color(1f, 0.84f, 0.36f, 1f);
            FillEllipse(tex, 22, 12, 11, 6, body);
            FillTriangle(tex, 4, 18, 4, 6, 13, 12, fin);
            FillCircle(tex, 28, 14, 2, Color.white);
            FillCircle(tex, 29, 14, 1, Color.black);
            tex.Apply();
            cachedFish = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
            return cachedFish;
        }

        /// <summary>Arbusto denso com samambaias para a Trilha da Floresta.</summary>
        public static Sprite CreateForestBushSprite()
        {
            if (cachedBush != null) return cachedBush;
            int w = 96, h = 64;
            Texture2D tex = CreateBlankTex(w, h);

            FillCircle(tex, 30, 22, 20, new Color(0.12f, 0.40f, 0.18f, 1f));
            FillCircle(tex, 64, 22, 20, new Color(0.14f, 0.44f, 0.20f, 1f));
            FillCircle(tex, 48, 30, 22, new Color(0.18f, 0.52f, 0.24f, 1f));
            FillCircle(tex, 42, 34, 12, new Color(0.28f, 0.64f, 0.28f, 1f));
            // Frutinhas silvestres no arbusto
            FillCircle(tex, 32, 28, 3, new Color(0.95f, 0.25f, 0.35f, 1f));
            FillCircle(tex, 58, 30, 3, new Color(0.95f, 0.25f, 0.35f, 1f));
            FillCircle(tex, 46, 20, 3, new Color(0.98f, 0.80f, 0.20f, 1f));

            tex.Apply();
            cachedBush = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 34f);
            return cachedBush;
        }

        /// <summary>Casinha charmosa ilustrada para a Pequena Vila.</summary>
        public static Sprite CreateVillageCottageSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 3;
            if (cottageCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 144, h = 128;
            Texture2D tex = CreateBlankTex(w, h);

            Color[] wallColors =
            {
                new Color(0.98f, 0.93f, 0.82f, 1f),
                new Color(0.96f, 0.88f, 0.90f, 1f),
                new Color(0.86f, 0.94f, 0.96f, 1f)
            };
            Color[] roofColors =
            {
                new Color(0.84f, 0.30f, 0.24f, 1f),
                new Color(0.28f, 0.50f, 0.78f, 1f),
                new Color(0.76f, 0.44f, 0.22f, 1f)
            };

            Color wall = wallColors[key];
            Color roof = roofColors[key];
            Color wood = new Color(0.52f, 0.32f, 0.16f, 1f);
            Color windowGlow = new Color(1f, 0.92f, 0.58f, 1f);

            // Chaminé
            FillRect(tex, 96, 82, 114, 118, new Color(0.68f, 0.36f, 0.28f, 1f));
            FillRect(tex, 93, 114, 117, 120, wood);

            // Paredes
            FillRect(tex, 20, 6, 124, 72, wall);
            // Base de pedra
            FillRect(tex, 18, 4, 126, 14, new Color(0.58f, 0.60f, 0.64f, 1f));

            // Telhado triangular
            FillTriangle(tex, 8, 68, 136, 68, 72, 122, roof);
            // Contorno/beiral
            FillRect(tex, 12, 66, 132, 71, wood);

            // Porta
            FillRect(tex, 58, 8, 86, 48, wood);
            FillCircle(tex, 81, 28, 3, new Color(1f, 0.84f, 0.25f, 1f));

            // Janelas com moldura e floreiras coloridas embaixo!
            FillRect(tex, 28, 34, 50, 56, wood);
            FillRect(tex, 31, 37, 47, 53, windowGlow);
            FillRect(tex, 38, 37, 40, 53, wood);
            FillRect(tex, 31, 44, 47, 46, wood);
            FillRect(tex, 26, 28, 52, 34, new Color(0.62f, 0.36f, 0.18f, 1f));
            FillCircle(tex, 32, 35, 4, new Color(1f, 0.42f, 0.66f, 1f));
            FillCircle(tex, 40, 36, 4, new Color(1f, 0.86f, 0.24f, 1f));
            FillCircle(tex, 47, 35, 4, new Color(0.28f, 0.74f, 0.30f, 1f));

            FillRect(tex, 94, 34, 116, 56, wood);
            FillRect(tex, 97, 37, 113, 53, windowGlow);
            FillRect(tex, 104, 37, 106, 53, wood);
            FillRect(tex, 97, 44, 113, 46, wood);
            FillRect(tex, 92, 28, 118, 34, new Color(0.62f, 0.36f, 0.18f, 1f));
            FillCircle(tex, 98, 35, 4, new Color(0.28f, 0.74f, 0.30f, 1f));
            FillCircle(tex, 106, 36, 4, new Color(1f, 0.42f, 0.66f, 1f));
            FillCircle(tex, 113, 35, 4, new Color(1f, 0.86f, 0.24f, 1f));

            // Fumaça fofa saindo da chaminé
            FillCircle(tex, 106, 123, 5, new Color(1f, 1f, 1f, 0.65f));
            FillCircle(tex, 114, 125, 4, new Color(1f, 1f, 1f, 0.45f));

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 28f);
            cottageCache[key] = sp;
            return sp;
        }

        /// <summary>Poste aconchegante com lanterna para a Pequena Vila.</summary>
        public static Sprite CreateVillageLampSprite()
        {
            if (cachedLamp != null) return cachedLamp;
            int w = 48, h = 112;
            Texture2D tex = CreateBlankTex(w, h);

            Color iron = new Color(0.22f, 0.25f, 0.30f, 1f);
            Color glow = new Color(1f, 0.90f, 0.45f, 0.42f);
            Color bulb = new Color(1f, 0.96f, 0.72f, 1f);

            FillRect(tex, 18, 4, 30, 12, iron);
            FillRect(tex, 22, 10, 26, 94, iron);
            FillRect(tex, 14, 92, 34, 98, iron);
            FillCircle(tex, 24, 86, 14, glow);
            FillRect(tex, 18, 78, 30, 92, bulb);
            FillTriangle(tex, 14, 96, 34, 96, 24, 108, iron);

            tex.Apply();
            cachedLamp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 32f);
            return cachedLamp;
        }

        /// <summary>Placa de madeira de marco de distância (km) na beira da trilha.</summary>
        public static Sprite CreateDistanceSignSprite()
        {
            if (cachedSign != null) return cachedSign;
            int w = 80, h = 96;
            Texture2D tex = CreateBlankTex(w, h);

            Color post = new Color(0.48f, 0.28f, 0.14f, 1f);
            Color board = new Color(0.84f, 0.62f, 0.34f, 1f);
            Color border = new Color(0.42f, 0.24f, 0.10f, 1f);

            FillRect(tex, 36, 4, 44, 62, post);
            FillRect(tex, 6, 48, 74, 86, border);
            FillRect(tex, 10, 52, 70, 82, board);

            tex.Apply();
            cachedSign = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 38f);
            return cachedSign;
        }

        /// <summary>Varal festivo de bandeirinhas coloridas para a Vila dos Viajantes.</summary>
        public static Sprite CreateVillageBuntingSprite()
        {
            if (cachedBunting != null) return cachedBunting;
            int w = 220, h = 56;
            Texture2D tex = CreateBlankTex(w, h);

            Color rope = new Color(0.45f, 0.30f, 0.16f, 0.95f);
            Color[] flagColors =
            {
                new Color(1f, 0.38f, 0.52f, 0.96f),
                new Color(1f, 0.82f, 0.22f, 0.96f),
                new Color(0.28f, 0.78f, 0.96f, 0.96f),
                new Color(0.42f, 0.84f, 0.44f, 0.96f),
                new Color(0.76f, 0.50f, 1f, 0.96f)
            };

            // Corda em curva suave
            for (int x = 4; x < w - 4; x++)
            {
                float t = (x - 4f) / (w - 8f);
                int y = Mathf.RoundToInt(48f - Mathf.Sin(t * Mathf.PI) * 14f);
                FillRect(tex, x, y - 1, x, y + 1, rope);
            }

            // 9 bandeirinhas triangulares coloridas
            for (int i = 0; i < 9; i++)
            {
                int cx = 18 + i * 23;
                float t = (cx - 4f) / (w - 8f);
                int topY = Mathf.RoundToInt(48f - Mathf.Sin(t * Mathf.PI) * 14f);
                FillTriangle(tex, cx - 9, topY - 1, cx + 9, topY - 1, cx, topY - 22, flagColors[i % flagColors.Length]);
            }

            tex.Apply();
            cachedBunting = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 26f);
            return cachedBunting;
        }

        // ─── Plano 2 (Fundo Intermediário) e Plano 4 (Primeiro Plano) ─

        /// <summary>Bosque intermediário de pinheiros e árvores com tom atmosférico de profundidade.</summary>
        public static Sprite CreateMidgroundTreeClusterSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 4;
            if (midTreeCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 180, h = 150;
            Texture2D tex = CreateBlankTex(w, h);
            Color trunkDark = new Color(0.28f, 0.20f, 0.15f, 0.95f);
            Color trunkMid = new Color(0.40f, 0.28f, 0.20f, 0.95f);

            if (key == 0)
            {
                // Dois Pinheiros Orgânicos com galhos jagged e agulhas em camadas
                Color pineDark1 = new Color(0.12f, 0.36f, 0.28f, 0.95f);
                Color pineMid1 = new Color(0.18f, 0.48f, 0.34f, 0.95f);
                Color pineLight1 = new Color(0.26f, 0.58f, 0.40f, 0.95f);
                FillRect(tex, 46, 6, 52, 54, trunkDark);
                DrawPineBranchTier(tex, 49, 44, 38, 22, pineDark1, pineMid1, pineLight1);
                DrawPineBranchTier(tex, 49, 64, 32, 20, pineDark1, pineMid1, pineLight1);
                DrawPineBranchTier(tex, 49, 82, 24, 18, pineDark1, pineMid1, pineLight1);
                DrawPineBranchTier(tex, 49, 98, 16, 16, pineDark1, pineMid1, pineLight1);
                DrawPineBranchTier(tex, 49, 112, 10, 14, pineDark1, pineMid1, pineLight1);

                Color pineDark2 = new Color(0.15f, 0.42f, 0.30f, 0.98f);
                Color pineMid2 = new Color(0.22f, 0.54f, 0.38f, 0.98f);
                Color pineLight2 = new Color(0.32f, 0.68f, 0.46f, 0.98f);
                FillRect(tex, 116, 6, 124, 60, trunkMid);
                DrawPineBranchTier(tex, 120, 50, 46, 26, pineDark2, pineMid2, pineLight2);
                DrawPineBranchTier(tex, 120, 74, 38, 24, pineDark2, pineMid2, pineLight2);
                DrawPineBranchTier(tex, 120, 96, 28, 22, pineDark2, pineMid2, pineLight2);
                DrawPineBranchTier(tex, 120, 116, 20, 18, pineDark2, pineMid2, pineLight2);
                DrawPineBranchTier(tex, 120, 132, 12, 15, pineDark2, pineMid2, pineLight2);
            }
            else if (key == 1)
            {
                // Duas Árvores Frondosas de Bosque (Carvalho e Bétula agrupados)
                Color leafDark1 = new Color(0.16f, 0.40f, 0.22f, 0.95f);
                Color leafMid1 = new Color(0.26f, 0.56f, 0.30f, 0.95f);
                Color leafLight1 = new Color(0.42f, 0.72f, 0.36f, 0.95f);

                Color leafDark2 = new Color(0.35f, 0.48f, 0.18f, 0.98f);
                Color leafMid2 = new Color(0.56f, 0.68f, 0.22f, 0.98f);
                Color leafLight2 = new Color(0.78f, 0.84f, 0.32f, 0.98f);

                FillRect(tex, 52, 6, 62, 65, trunkDark);
                DrawOrganicFoliageLobe(tex, 36, 75, 26, 20, leafMid1, leafLight1, leafDark1);
                DrawOrganicFoliageLobe(tex, 76, 78, 28, 22, leafMid1, leafLight1, leafDark1);
                DrawOrganicFoliageLobe(tex, 56, 98, 32, 26, leafMid1, leafLight1, leafDark1);

                FillRect(tex, 124, 6, 132, 58, new Color(0.88f, 0.88f, 0.84f, 0.95f));
                DrawOrganicFoliageLobe(tex, 104, 70, 24, 20, leafMid2, leafLight2, leafDark2);
                DrawOrganicFoliageLobe(tex, 146, 72, 26, 22, leafMid2, leafLight2, leafDark2);
                DrawOrganicFoliageLobe(tex, 125, 92, 34, 26, leafMid2, leafLight2, leafDark2);
                DrawOrganicFoliageLobe(tex, 122, 114, 26, 18, leafLight2, new Color(0.92f, 0.90f, 0.46f, 1f), leafMid2);
            }
            else if (key == 2)
            {
                // Bosque de Salgueiros Ribeirinhos
                Color sTrunk = new Color(0.38f, 0.26f, 0.16f, 0.95f);
                Color wDark = new Color(0.16f, 0.44f, 0.24f, 0.92f);
                Color wMid = new Color(0.24f, 0.58f, 0.30f, 0.92f);
                Color wLight = new Color(0.38f, 0.72f, 0.34f, 0.92f);

                FillRect(tex, 58, 6, 66, 60, sTrunk);
                DrawOrganicFoliageLobe(tex, 62, 85, 34, 26, wMid, wLight, wDark);
                FillRect(tex, 120, 6, 127, 52, sTrunk);
                DrawOrganicFoliageLobe(tex, 123, 76, 28, 22, wMid, wLight, wDark);

                // Ramos pendentes
                int[] wx = { 38, 48, 76, 86, 104, 114, 136, 144 };
                int[] wy = { 35, 42, 38, 32, 36, 40, 34, 38 };
                for (int i = 0; i < wx.Length; i++)
                {
                    DrawCurvedBranch(tex, new Vector2(wx[i], 80), new Vector2(wx[i] + (i % 2 == 0 ? -4 : 4), 60), new Vector2(wx[i], wy[i]), 2f, 1f, wDark, wLight);
                }
            }
            else
            {
                // Bosque Lunar Noturno com árvores místicas azul-violeta
                Color lTrunk = new Color(0.28f, 0.24f, 0.42f, 0.95f);
                Color mDark = new Color(0.12f, 0.22f, 0.46f, 0.95f);
                Color mMid = new Color(0.18f, 0.52f, 0.66f, 0.95f);
                Color mLight = new Color(0.52f, 0.88f, 0.92f, 0.98f);

                FillRect(tex, 54, 6, 62, 58, lTrunk);
                DrawOrganicFoliageLobe(tex, 58, 86, 32, 26, mMid, mLight, mDark);
                FillRect(tex, 118, 6, 125, 66, lTrunk);
                DrawOrganicFoliageLobe(tex, 122, 94, 36, 28, mMid, mLight, mDark);
                ScatterCanopyHighlights(tex, 90, 88, 72, 38, 59, mLight, mDark);
            }

            // Névoa suave na base do bosque para fusão atmosférica
            Color mistCol = (key == 3) ? new Color(0.24f, 0.32f, 0.54f, 0.32f) : new Color(0.72f, 0.84f, 0.78f, 0.28f);
            for (int y = 6; y <= 24; y++)
            {
                float a = (1f - (y - 6f) / 18f) * mistCol.a;
                for (int x = 12; x < w - 12; x++)
                {
                    Color cur = tex.GetPixel(x, y);
                    if (cur.a > 0.05f)
                    {
                        Color blended = Color.Lerp(cur, mistCol, a);
                        blended.a = cur.a;
                        tex.SetPixel(x, y, blended);
                    }
                }
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
            midTreeCache[key] = sp;
            return sp;
        }

        /// <summary>Cachoeira orgânica pintada com paredão rochoso estratificado, cortinas de água, espuma e lago de impacto ondulado.</summary>
        public static Sprite CreateDistantWaterfallSprite()
        {
            if (cachedWaterfall != null) return cachedWaterfall;
            int w = 180, h = 180;
            Texture2D tex = CreateBlankTex(w, h);

            Color rockDark = new Color(0.22f, 0.28f, 0.32f, 0.95f);
            Color rockMid = new Color(0.35f, 0.42f, 0.46f, 0.95f);
            Color rockLight = new Color(0.48f, 0.56f, 0.60f, 0.95f);
            Color mossDark = new Color(0.16f, 0.38f, 0.22f, 0.95f);
            Color mossLight = new Color(0.32f, 0.58f, 0.30f, 0.95f);

            Color waterDeep = new Color(0.20f, 0.58f, 0.82f, 0.90f);
            Color waterMid = new Color(0.38f, 0.78f, 0.92f, 0.92f);
            Color waterLight = new Color(0.68f, 0.92f, 0.98f, 0.95f);
            Color foamWhite = new Color(0.96f, 0.99f, 1f, 0.98f);
            Color mistCol = new Color(0.85f, 0.95f, 1f, 0.45f);

            // 1. Paredões rochosos com estratos naturais e fendas
            for (int y = 14; y < h - 14; y++)
            {
                float yt = y / (float)h;
                int leftCliffEnd = Mathf.RoundToInt(52f + Mathf.Sin(y * 0.12f) * 8f + Mathf.Cos(y * 0.25f) * 4f - yt * 12f);
                for (int x = 6; x <= leftCliffEnd; x++)
                {
                    Color c = ((x + y) % 7 == 0 || y % 11 == 0) ? rockDark : (x > leftCliffEnd - 6 ? rockLight : rockMid);
                    if (y > h - 35 || (y % 19 < 4)) c = Color.Lerp(c, mossDark, 0.6f);
                    tex.SetPixel(x, y, c);
                }
                int rightCliffStart = Mathf.RoundToInt(128f - Mathf.Sin(y * 0.14f) * 7f - Mathf.Cos(y * 0.22f) * 5f + yt * 10f);
                for (int x = rightCliffStart; x < w - 6; x++)
                {
                    Color c = ((x - y) % 7 == 0 || y % 13 == 0) ? rockDark : (x < rightCliffStart + 6 ? rockLight : rockMid);
                    if (y > h - 35 || (y % 23 < 4)) c = Color.Lerp(c, mossDark, 0.6f);
                    tex.SetPixel(x, y, c);
                }
            }

            // Lóbulos de musgo e vegetação sobre as rochas
            DrawOrganicFoliageLobe(tex, 36, h - 22, 26, 12, mossDark, mossLight, rockDark);
            DrawOrganicFoliageLobe(tex, 144, h - 24, 24, 11, mossDark, mossLight, rockDark);
            DrawOrganicFoliageLobe(tex, 42, 92, 16, 8, mossDark, mossLight, rockDark);
            DrawOrganicFoliageLobe(tex, 136, 84, 18, 9, mossDark, mossLight, rockDark);

            // 2. Queda d'água com filamentos e linhas de velocidade
            for (int y = 24; y <= h - 20; y++)
            {
                float t = (y - 24f) / (h - 44f);
                float center = 90f + Mathf.Sin(t * 3.14f * 1.5f) * 6f;
                float streamW = Mathf.Lerp(28f, 18f, t);

                int leftX = Mathf.RoundToInt(center - streamW);
                int rightX = Mathf.RoundToInt(center + streamW);

                for (int x = leftX; x <= rightX; x++)
                {
                    float normX = (x - leftX) / (float)Mathf.Max(1, rightX - leftX);
                    Color wc;
                    if (normX < 0.15f || normX > 0.85f)
                        wc = waterDeep;
                    else if (normX > 0.35f && normX < 0.65f)
                        wc = ((x + y * 2) % 5 == 0) ? foamWhite : waterLight;
                    else
                        wc = waterMid;

                    if ((x % 6 == 0) && (y % 9 < 5)) wc = foamWhite;
                    tex.SetPixel(x, y, wc);
                }
            }

            // 3. Crista de água no topo
            for (int x = 74; x <= 106; x++)
            {
                for (int y = h - 26; y <= h - 20; y++)
                {
                    Color fc = (y == h - 20 || (x % 3 == 0)) ? foamWhite : waterLight;
                    tex.SetPixel(x, y, fc);
                }
            }

            // 4. Lago de impacto com ondulações naturais e espuma dinâmica (sem círculos estáticos chapados)
            for (int y = 6; y <= 32; y++)
            {
                float py = (y - 18f) / 13f;
                for (int x = 32; x <= 148; x++)
                {
                    float px = (x - 90f) / 54f;
                    float r2 = px * px + py * py;
                    if (r2 <= 1f)
                    {
                        float ripple = Mathf.Sin(Mathf.Sqrt(r2) * 12f - y * 0.4f);
                        Color poolCol;
                        if (r2 < 0.28f)
                            poolCol = (ripple > 0.2f) ? foamWhite : waterLight;
                        else if (ripple > 0.5f)
                            poolCol = foamWhite;
                        else if (ripple > 0f)
                            poolCol = waterMid;
                        else
                            poolCol = waterDeep;

                        if (r2 > 0.82f)
                        {
                            float a = Mathf.Clamp01((1f - Mathf.Sqrt(r2)) / 0.18f);
                            poolCol.a *= a;
                        }
                        tex.SetPixel(x, y, poolCol);
                    }
                }
            }

            // 5. Névoa e microgotículas suspensas na base
            for (int my = 20; my <= 48; my += 3)
            {
                for (int mx = 55; mx <= 125; mx += 4)
                {
                    if ((mx * 3 + my * 7) % 5 == 0)
                    {
                        float dCenter = Mathf.Abs(mx - 90f) / 35f;
                        Color mc = mistCol;
                        mc.a = (1f - dCenter) * 0.42f * (1f - (my - 20f) / 28f);
                        tex.SetPixel(mx, my, mc);
                        if (mx + 1 < w) tex.SetPixel(mx + 1, my, mc);
                    }
                }
            }

            tex.Apply();
            cachedWaterfall = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 26f);
            return cachedWaterfall;
        }

        /// <summary>Samambaia exuberante de primeiro plano (Plano 4).</summary>
        public static Sprite CreateForegroundFernSprite()
        {
            if (cachedFgFern != null) return cachedFgFern;
            int w = 128, h = 88;
            Texture2D tex = CreateBlankTex(w, h);
            Color dark = new Color(0.10f, 0.36f, 0.16f, 0.95f);
            Color mid = new Color(0.18f, 0.52f, 0.22f, 0.96f);
            Color light = new Color(0.32f, 0.68f, 0.28f, 0.96f);

            for (int frond = -2; frond <= 2; frond++)
            {
                for (int s = 0; s < 16; s++)
                {
                    float t = s / 15f;
                    int x = 64 + Mathf.RoundToInt(frond * 24f * t + frond * 8f * t * t);
                    int y = 6 + Mathf.RoundToInt(64f * t - 16f * t * t * Mathf.Abs(frond) * 0.45f);
                    int r = Mathf.Max(3, Mathf.RoundToInt((1f - t * 0.65f) * 11f));
                    Color c = t < 0.35f ? dark : (t < 0.75f ? mid : light);
                    FillEllipse(tex, x, y, r + 3, r, c);
                }
            }

            tex.Apply();
            cachedFgFern = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 42f);
            return cachedFgFern;
        }

        /// <summary>Cogumelos ilustrados de primeiro plano com musgo (Plano 4).</summary>
        public static Sprite CreateForegroundMushroomClusterSprite()
        {
            if (cachedFgMushroom != null) return cachedFgMushroom;
            int w = 96, h = 76;
            Texture2D tex = CreateBlankTex(w, h);
            Color moss = new Color(0.16f, 0.48f, 0.20f, 0.96f);
            Color stem = new Color(0.96f, 0.92f, 0.80f, 1f);
            Color capRed = new Color(0.88f, 0.22f, 0.26f, 1f);
            Color capWarm = new Color(0.96f, 0.48f, 0.22f, 1f);

            FillEllipse(tex, 48, 10, 36, 9, moss);

            // Cogumelo maior
            FillRect(tex, 34, 12, 46, 40, stem);
            FillEllipse(tex, 40, 44, 22, 14, capRed);
            FillCircle(tex, 31, 47, 4, Color.white);
            FillCircle(tex, 44, 51, 3, Color.white);
            FillCircle(tex, 52, 43, 3, Color.white);

            // Cogumelo menor ao lado
            FillRect(tex, 62, 12, 70, 30, stem);
            FillEllipse(tex, 66, 33, 14, 9, capWarm);
            FillCircle(tex, 62, 35, 2, Color.white);
            FillCircle(tex, 70, 34, 2, Color.white);

            tex.Apply();
            cachedFgMushroom = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 44f);
            return cachedFgMushroom;
        }

        /// <summary>Flores grandes e delicadas de primeiro plano (Plano 4).</summary>
        public static Sprite CreateForegroundBigFlowerSprite(int variant)
        {
            int key = Mathf.Abs(variant) % 2;
            if (fgFlowerCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 96, h = 84;
            Texture2D tex = CreateBlankTex(w, h);
            Color leaf = new Color(0.16f, 0.50f, 0.22f, 0.96f);
            Color petal = key == 0 ? new Color(1f, 0.50f, 0.72f, 0.98f) : new Color(0.76f, 0.56f, 1f, 0.98f);
            Color petal2 = key == 0 ? new Color(1f, 0.88f, 0.28f, 0.98f) : new Color(1f, 0.96f, 0.98f, 0.98f);

            FillEllipse(tex, 48, 12, 34, 10, leaf);
            FillRect(tex, 32, 12, 36, 48, leaf);
            FillRect(tex, 62, 12, 66, 40, leaf);

            // Flor 1 grande
            for (int a = 0; a < 5; a++)
            {
                float ang = a * Mathf.PI * 2f / 5f;
                FillCircle(tex, 34 + Mathf.RoundToInt(Mathf.Cos(ang) * 10f), 54 + Mathf.RoundToInt(Mathf.Sin(ang) * 10f), 8, petal);
            }
            FillCircle(tex, 34, 54, 6, new Color(1f, 0.88f, 0.22f, 1f));

            // Flor 2 complementar
            for (int a = 0; a < 5; a++)
            {
                float ang = a * Mathf.PI * 2f / 5f;
                FillCircle(tex, 64 + Mathf.RoundToInt(Mathf.Cos(ang) * 8f), 44 + Mathf.RoundToInt(Mathf.Sin(ang) * 8f), 6, petal2);
            }
            FillCircle(tex, 64, 44, 5, new Color(1f, 0.66f, 0.16f, 1f));

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 44f);
            fgFlowerCache[key] = sp;
            return sp;
        }

        /// <summary>Pedra com musgo e raiz de primeiro plano (Plano 4).</summary>
        public static Sprite CreateForegroundStoneRootSprite()
        {
            if (cachedFgStoneRoot != null) return cachedFgStoneRoot;
            int w = 112, h = 68;
            Texture2D tex = CreateBlankTex(w, h);

            FillEllipse(tex, 46, 22, 30, 20, new Color(0.36f, 0.40f, 0.44f, 0.96f));
            FillEllipse(tex, 40, 26, 20, 13, new Color(0.52f, 0.56f, 0.60f, 0.96f));
            FillEllipse(tex, 44, 38, 22, 7, new Color(0.20f, 0.56f, 0.24f, 0.96f)); // Musgo no topo
            FillEllipse(tex, 80, 16, 22, 10, new Color(0.44f, 0.26f, 0.14f, 0.96f)); // Raiz

            tex.Apply();
            cachedFgStoneRoot = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 44f);
            return cachedFgStoneRoot;
        }

        /// <summary>Salgueiro ribeirinho delicado com um passarinho azul pousado no galho (Vale das Águas).</summary>
        public static Sprite CreateWillowTreeWithBirdSprite()
        {
            Sprite oak = LoadOakTreeSprite();
            if (oak != null) return oak;
            if (cachedWillowTree != null) return cachedWillowTree;
            int w = 320, h = 320;
            Texture2D tex = CreateBlankTex(w, h);

            Color trunkDark = new Color(0.36f, 0.24f, 0.14f, 0.98f);
            Color trunkMid = new Color(0.50f, 0.34f, 0.20f, 0.98f);
            Color trunkLight = new Color(0.66f, 0.48f, 0.30f, 0.98f);
            Color crownDark = new Color(0.14f, 0.42f, 0.22f, 0.94f);
            Color crownMid = new Color(0.24f, 0.58f, 0.30f, 0.94f);
            Color crownLight = new Color(0.40f, 0.74f, 0.36f, 0.90f);
            Color frondSun = new Color(0.58f, 0.86f, 0.42f, 0.88f);

            // Raízes e tronco gracioso de salgueiro
            DrawCurvedBranch(tex, new Vector2(160, 10), new Vector2(132, 6), new Vector2(106, 5), 10f, 3f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(162, 10), new Vector2(192, 6), new Vector2(218, 5), 10f, 3f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(160, 8), new Vector2(150, 95), new Vector2(162, 185), 18f, 10f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(158, 140), new Vector2(115, 180), new Vector2(88, 226), 11f, 4f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(164, 145), new Vector2(208, 164), new Vector2(242, 152), 10f, 4f, trunkDark, trunkLight);

            // Copa de salgueiro em nuvens suaves de folhagem
            DrawOrganicFoliageLobe(tex, 108, 214, 58, 52, crownDark, crownMid, new Color(0.08f, 0.30f, 0.14f, 0.95f));
            DrawOrganicFoliageLobe(tex, 212, 214, 56, 50, crownDark, crownMid, new Color(0.08f, 0.30f, 0.14f, 0.95f));
            DrawOrganicFoliageLobe(tex, 160, 246, 72, 62, crownMid, crownLight, crownDark);
            DrawOrganicFoliageLobe(tex, 134, 268, 46, 36, crownMid, crownLight, crownDark);
            DrawOrganicFoliageLobe(tex, 190, 266, 44, 34, crownMid, crownLight, crownDark);
            ScatterCanopyHighlights(tex, 160, 234, 98, 68, 36, frondSun, crownDark);

            // Ramos pendentes curvos de salgueiro chorão com folhas fluidas em cascata
            int[] fxStart = { 68, 96, 124, 148, 176, 198, 224, 248 };
            int[] fl =      { 102, 118, 98, 84, 88, 98, 114, 96 };
            int[] swayDx =  { -20, -12, -8, -4, 6, 10, 16, 24 };
            for (int i = 0; i < fxStart.Length; i++)
            {
                Vector2 p0 = new Vector2(fxStart[i], 212);
                Vector2 p1 = new Vector2(fxStart[i] + swayDx[i] * 0.45f, 212 - fl[i] * 0.5f);
                Vector2 p2 = new Vector2(fxStart[i] + swayDx[i], 212 - fl[i]);
                DrawCurvedBranch(tex, p0, p1, p2, 4.5f, 1.8f, crownDark, crownLight);
                for (int s = 6; s <= fl[i]; s += 8)
                {
                    float t = s / (float)fl[i];
                    float omt = 1f - t;
                    Vector2 pt = omt * omt * p0 + 2f * omt * t * p1 + t * t * p2;
                    int px = Mathf.RoundToInt(pt.x);
                    int py = Mathf.RoundToInt(pt.y);
                    Color leafC = (s % 16 == 0) ? frondSun : crownMid;
                    FillEllipse(tex, px, py, 5, 3, leafC);
                }
            }

            // Pequeno passarinho azul pousado no galho (storybook acolhedor)
            Color birdBlue = new Color(0.24f, 0.65f, 0.96f, 1f);
            Color birdChest = new Color(0.88f, 0.96f, 1f, 1f);
            Color birdBeak = new Color(1f, 0.76f, 0.18f, 1f);
            FillEllipse(tex, 224, 166, 13, 9, birdBlue);
            FillEllipse(tex, 222, 164, 7, 5, birdChest);
            FillCircle(tex, 234, 175, 7, birdBlue);
            FillTriangle(tex, 240, 177, 240, 171, 248, 174, birdBeak);
            FillCircle(tex, 236, 176, 1, Color.black);

            tex.Apply();
            cachedWillowTree = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 52f);
            return cachedWillowTree;
        }

        /// <summary>Pequena ilha ribeirinha com musgo, pedras lisas e flores aquáticas (Vale das Águas).</summary>
        public static Sprite CreateRiverIsletSprite()
        {
            if (cachedRiverIslet != null) return cachedRiverIslet;
            int w = 128, h = 56;
            Texture2D tex = CreateBlankTex(w, h);

            FillEllipse(tex, 64, 14, 52, 12, new Color(0.38f, 0.44f, 0.46f, 0.92f));
            FillEllipse(tex, 64, 18, 44, 11, new Color(0.24f, 0.60f, 0.28f, 0.96f));
            FillEllipse(tex, 54, 22, 22, 8, new Color(0.36f, 0.72f, 0.34f, 0.96f));
            // Juncos e florzinha branca na ilhota
            FillRect(tex, 74, 20, 76, 44, new Color(0.18f, 0.54f, 0.24f, 1f));
            FillRect(tex, 82, 20, 84, 48, new Color(0.18f, 0.54f, 0.24f, 1f));
            FillCircle(tex, 48, 28, 4, Color.white);
            FillCircle(tex, 48, 28, 2, new Color(1f, 0.86f, 0.22f, 1f));
            FillCircle(tex, 62, 26, 4, new Color(1f, 0.62f, 0.80f, 1f));

            tex.Apply();
            cachedRiverIslet = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 32f);
            return cachedRiverIslet;
        }

        /// <summary>Árvore frutífera acolhedora de livro ilustrado para profundidade próxima (Plano 2.5).</summary>
        public static Sprite CreateFruitTreeSprite()
        {
            Sprite oak = LoadOakTreeSprite();
            if (oak != null) return oak;
            if (cachedFruitTree != null) return cachedFruitTree;
            int w = 320, h = 320;
            Texture2D tex = CreateBlankTex(w, h);

            Color trunkDark = new Color(0.32f, 0.18f, 0.10f, 0.98f);
            Color trunkMid = new Color(0.46f, 0.28f, 0.16f, 0.98f);
            Color trunkLight = new Color(0.60f, 0.38f, 0.22f, 0.98f);

            Color fDark = new Color(0.14f, 0.38f, 0.18f, 0.98f);
            Color fMid = new Color(0.24f, 0.56f, 0.24f, 0.98f);
            Color fLight = new Color(0.40f, 0.72f, 0.30f, 0.98f);
            Color fSun = new Color(0.58f, 0.86f, 0.38f, 0.98f);

            // Tronco orgânico esculpido com raízes de suporte
            DrawCurvedBranch(tex, new Vector2(160, 10), new Vector2(134, 6), new Vector2(110, 4), 9f, 3f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(162, 10), new Vector2(188, 6), new Vector2(212, 4), 9f, 3f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(160, 8), new Vector2(152, 85), new Vector2(160, 164), 18f, 10f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(158, 124), new Vector2(116, 164), new Vector2(92, 204), 10f, 4f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(164, 124), new Vector2(206, 160), new Vector2(234, 202), 10f, 4f, trunkDark, trunkLight);

            // Copa frondosa em camadas orgânicas sombreadas
            DrawOrganicFoliageLobe(tex, 108, 188, 64, 48, fMid, fLight, fDark);
            DrawOrganicFoliageLobe(tex, 212, 188, 64, 48, fMid, fLight, fDark);
            DrawOrganicFoliageLobe(tex, 160, 228, 76, 56, fMid, fLight, fDark);
            DrawOrganicFoliageLobe(tex, 160, 268, 52, 36, fLight, fSun, fMid);
            ScatterCanopyHighlights(tex, 160, 216, 96, 78, 38,
                new Color(0.68f, 0.88f, 0.46f, 0.94f), new Color(0.12f, 0.32f, 0.16f, 0.96f));

            // Maçãs volumosas sombreadas com degradê esférico suave e folhinha
            int[] ax = { 100, 144, 192, 124, 176, 216, 160 };
            int[] ay = { 184, 236, 192, 216, 224, 208, 252 };
            for (int i = 0; i < ax.Length; i++)
            {
                int mx = ax[i];
                int my = ay[i];
                // Sombra de folha atrás da maçã
                FillEllipse(tex, mx - 2, my + 7, 3, 2, fDark);
                // Maçã vermelha com degradê suave
                FillCircle(tex, mx, my, 7, new Color(0.78f, 0.14f, 0.16f, 1f));
                FillCircle(tex, mx, my + 1, 5, new Color(0.92f, 0.24f, 0.22f, 1f));
                FillCircle(tex, mx - 2, my + 3, 3, new Color(1f, 0.44f, 0.36f, 1f));
                FillCircle(tex, mx - 2, my + 3, 1, new Color(1f, 0.92f, 0.85f, 1f)); // Ponto de luz
                // Cabinho de madeira
                FillRect(tex, mx - 1, my + 7, mx, my + 9, trunkDark);
                // Folhinha verde no cabinho
                FillEllipse(tex, mx + 2, my + 9, 3, 2, fSun);
            }

            tex.Apply();
            cachedFruitTree = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 52f);
            return cachedFruitTree;
        }

        /// <summary>Flores luminosas azul-prateadas do Bosque da Lua (Etapa 6).</summary>
        public static Sprite CreateMoonFlowerSprite()
        {
            if (cachedMoonFlower != null) return cachedMoonFlower;
            int w = 96, h = 64;
            Texture2D tex = CreateBlankTex(w, h);

            Color stem = new Color(0.16f, 0.36f, 0.48f, 1f);
            Color glow = new Color(0.46f, 0.84f, 1f, 0.32f);
            Color petal = new Color(0.70f, 0.92f, 1f, 0.98f);
            Color core = new Color(1f, 0.96f, 0.68f, 1f);

            int[] fx = { 24, 48, 72 };
            int[] fy = { 34, 44, 32 };
            for (int i = 0; i < fx.Length; i++)
            {
                FillRect(tex, fx[i] - 1, 8, fx[i] + 1, fy[i] - 4, stem);
                FillCircle(tex, fx[i], fy[i], 14, glow);
                FillCircle(tex, fx[i] - 5, fy[i], 5, petal);
                FillCircle(tex, fx[i] + 5, fy[i], 5, petal);
                FillCircle(tex, fx[i], fy[i] + 5, 5, petal);
                FillCircle(tex, fx[i], fy[i] - 5, 5, petal);
                FillCircle(tex, fx[i], fy[i], 4, core);
            }

            tex.Apply();
            cachedMoonFlower = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 40f);
            return cachedMoonFlower;
        }

        /// <summary>Cogumelos luminosos encantados para o Bosque da Lua e Floresta dos Segredos.</summary>
        public static Sprite CreateGlowingMushroomSprite()
        {
            if (cachedGlowMushroom != null) return cachedGlowMushroom;
            int w = 88, h = 72;
            Texture2D tex = CreateBlankTex(w, h);

            Color aura = new Color(0.42f, 0.76f, 1f, 0.28f);
            Color stem = new Color(0.84f, 0.94f, 1f, 0.96f);
            Color cap = new Color(0.38f, 0.56f, 0.98f, 0.98f);
            Color spot = new Color(0.76f, 0.98f, 1f, 1f);

            FillCircle(tex, 42, 42, 26, aura);
            FillRect(tex, 34, 8, 44, 36, stem);
            FillEllipse(tex, 39, 40, 20, 12, cap);
            FillCircle(tex, 31, 42, 3, spot);
            FillCircle(tex, 44, 44, 3, spot);

            FillRect(tex, 58, 8, 65, 26, stem);
            FillEllipse(tex, 62, 28, 12, 8, new Color(0.62f, 0.48f, 0.98f, 0.98f));
            FillCircle(tex, 60, 30, 2, spot);

            tex.Apply();
            cachedGlowMushroom = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.06f), 42f);
            return cachedGlowMushroom;
        }

        /// <summary>Carrega o carvalho ilustrado clássico (Resources/Floresta/carvalho_bosque) diretamente como Sprite com fallback seguro.</summary>
        public static Sprite LoadOakTreeSprite()
        {
            if (cachedOakTree != null) return cachedOakTree;
            Sprite sp = Resources.Load<Sprite>("Floresta/carvalho_bosque");
            if (sp != null)
            {
                cachedOakTree = sp;
                return cachedOakTree;
            }
            Texture2D tex = Resources.Load<Texture2D>("Floresta/carvalho_bosque");
            if (tex != null)
            {
                Texture2D solidTex = (tex.isReadable) ? RepairAlphaHolesInMemory(tex) : tex;
                cachedOakTree = Sprite.Create(solidTex, new Rect(0, 0, solidTex.width, solidTex.height), new Vector2(0.5f, 0.012f), 205f);
                return cachedOakTree;
            }
            return null;
        }

        private static Texture2D RepairAlphaHolesInMemory(Texture2D source)
        {
            if (source == null || !source.isReadable) return source;
            try
            {
                int w = source.width;
                int h = source.height;
                Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color[] pixels = source.GetPixels();
                Color barkFill = new Color(0.36f, 0.22f, 0.12f, 1f);
                Color canopyFill = new Color(0.22f, 0.46f, 0.20f, 1f);

                int[] leftBound = new int[h];
                int[] rightBound = new int[h];
                for (int y = 0; y < h; y++)
                {
                    leftBound[y] = -1;
                    rightBound[y] = -1;
                    for (int x = 0; x < w; x++)
                    {
                        if (pixels[y * w + x].a > 0.08f)
                        {
                            if (leftBound[y] == -1) leftBound[y] = x;
                            rightBound[y] = x;
                        }
                    }
                }

                for (int y = 0; y < h; y++)
                {
                    int minX = leftBound[y];
                    int maxX = rightBound[y];
                    if (minX == -1 || maxX == -1 || maxX - minX < 8) continue;

                    for (int x = minX + 2; x <= maxX - 2; x++)
                    {
                        int idx = y * w + x;
                        if (pixels[idx].a < 0.15f)
                        {
                            Color fill = (y < h * 0.42f) ? barkFill : canopyFill;
                            pixels[idx] = fill;
                        }
                    }
                }

                result.SetPixels(pixels);
                result.Apply();
                return result;
            }
            catch
            {
                return source;
            }
        }

        /// <summary>Árvore Lunar Encantada com tronco prateado-violeta e folhagem bioluminescente (Bosque da Lua).</summary>
        public static Sprite CreateMoonTreeSprite()
        {
            if (cachedMoonTree != null) return cachedMoonTree;
            int w = 180, h = 180;
            Texture2D tex = CreateBlankTex(w, h);

            Color trunkDark = new Color(0.20f, 0.16f, 0.32f, 0.98f);
            Color trunkMid = new Color(0.38f, 0.34f, 0.54f, 0.98f);
            Color trunkLight = new Color(0.68f, 0.64f, 0.86f, 0.98f);
            Color trunkRune = new Color(0.50f, 0.92f, 1f, 0.95f);

            // Raízes e tronco curvo mágico
            DrawCurvedBranch(tex, new Vector2(90, 8), new Vector2(66, 6), new Vector2(48, 4), 6f, 2f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(92, 8), new Vector2(114, 6), new Vector2(132, 4), 6f, 2f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(90, 6), new Vector2(82, 48), new Vector2(90, 96), 11f, 5f, trunkDark, trunkMid);

            // Galhos serpenteantes
            DrawCurvedBranch(tex, new Vector2(88, 76), new Vector2(62, 102), new Vector2(44, 128), 6f, 2f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(92, 80), new Vector2(118, 102), new Vector2(136, 126), 6f, 2f, trunkDark, trunkLight);
            DrawCurvedBranch(tex, new Vector2(90, 84), new Vector2(88, 115), new Vector2(86, 142), 6f, 2f, trunkDark, trunkLight);

            // Detalhes bioluminescentes no tronco
            FillEllipse(tex, 87, 36, 2, 9, trunkRune);
            FillEllipse(tex, 90, 62, 2, 7, trunkRune);

            Color moonDark = new Color(0.12f, 0.18f, 0.38f, 0.98f);
            Color moonMid = new Color(0.18f, 0.54f, 0.68f, 0.98f);
            Color moonLight = new Color(0.48f, 0.88f, 0.88f, 0.98f);
            Color moonGlow = new Color(0.82f, 0.96f, 1f, 0.98f);

            // Copas arredondadas místicas com bioluminescência
            DrawOrganicFoliageLobe(tex, 50, 102, 36, 28, moonMid, moonLight, moonDark);
            DrawOrganicFoliageLobe(tex, 130, 106, 36, 28, moonMid, moonLight, moonDark);
            DrawOrganicFoliageLobe(tex, 90, 120, 44, 32, moonMid, moonLight, moonDark);
            DrawOrganicFoliageLobe(tex, 70, 142, 30, 24, moonLight, moonGlow, moonMid);
            DrawOrganicFoliageLobe(tex, 110, 138, 28, 22, moonLight, moonGlow, moonMid);
            DrawOrganicFoliageLobe(tex, 90, 156, 24, 18, moonLight, moonGlow, moonMid);

            // Brilhos e esporos luminosos cintilantes
            ScatterCanopyHighlights(tex, 90, 122, 74, 50, 42,
                new Color(0.78f, 0.96f, 1f, 0.96f), new Color(0.14f, 0.34f, 0.54f, 0.96f));

            // Ramos pendentes com esporos bioluminescentes azuis
            int[] mx = { 38, 56, 72, 108, 124, 140 };
            int[] my = { 76, 84, 88, 86, 82, 74 };
            for (int i = 0; i < mx.Length; i++)
            {
                FillCircle(tex, mx[i], my[i], 3, moonGlow);
                FillCircle(tex, mx[i], my[i], 7, new Color(0.48f, 0.88f, 1f, 0.28f));
                FillCircle(tex, mx[i] + (i % 2 == 0 ? -2 : 2), my[i] - 6, 2, moonLight);
            }

            tex.Apply();
            cachedMoonTree = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
            return cachedMoonTree;
        }

        /// <summary>Abeto / Pinheiro Ancião majestoso de mata fechada com líquens e agulhas esmeralda (Trilha da Floresta).</summary>
        public static Sprite CreateAncientConiferSprite()
        {
            if (cachedAncientConifer != null) return cachedAncientConifer;
            int w = 180, h = 195;
            Texture2D tex = CreateBlankTex(w, h);

            Color trunkDark = new Color(0.24f, 0.16f, 0.10f, 0.98f);
            Color trunkMid = new Color(0.38f, 0.24f, 0.14f, 0.98f);
            Color mossGreen = new Color(0.36f, 0.54f, 0.24f, 0.95f);

            // Raízes e tronco forte
            DrawCurvedBranch(tex, new Vector2(90, 6), new Vector2(68, 4), new Vector2(48, 3), 7f, 2f, trunkDark, trunkMid);
            DrawCurvedBranch(tex, new Vector2(90, 6), new Vector2(112, 4), new Vector2(132, 3), 7f, 2f, trunkDark, trunkMid);
            FillRect(tex, 85, 6, 95, 68, trunkDark);
            FillRect(tex, 88, 6, 92, 66, trunkMid);

            // Manchas de musgo no tronco
            FillEllipse(tex, 87, 26, 3, 10, mossGreen);
            FillEllipse(tex, 93, 44, 3, 8, mossGreen);

            Color pineDark = new Color(0.08f, 0.28f, 0.20f, 0.98f);
            Color pineMid = new Color(0.14f, 0.44f, 0.28f, 0.98f);
            Color pineLight = new Color(0.24f, 0.60f, 0.36f, 0.98f);

            // 7 níveis de ramagem densa
            DrawPineBranchTier(tex, 90, 48, 62, 30, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 75, 54, 28, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 102, 44, 26, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 126, 34, 22, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 148, 24, 20, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 166, 16, 16, pineDark, pineMid, pineLight);
            DrawPineBranchTier(tex, 90, 180, 10, 12, pineDark, pineMid, pineLight);

            ScatterPineNeedles(tex, 77, new Color(0.38f, 0.72f, 0.42f, 0.98f), pineDark);

            // Pequenas pinhas pendentes
            int[] cx = { 64, 116, 72, 108, 80, 100 };
            int[] cy = { 46, 48, 74, 76, 100, 102 };
            Color coneCol = new Color(0.32f, 0.20f, 0.12f, 1f);
            for (int i = 0; i < cx.Length; i++)
            {
                FillEllipse(tex, cx[i], cy[i], 3, 5, coneCol);
            }

            tex.Apply();
            cachedAncientConifer = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.035f), 28f);
            return cachedAncientConifer;
        }

        /// <summary>Retorna a espécie de árvore mais característica para cada bioma da jornada.</summary>
        public static Sprite GetTreeForBiome(BiomeType biome, int variant)
        {
            int v = Mathf.Abs(variant);
            switch (biome)
            {
                case BiomeType.CampoFlorido:
                    int[] campoPool = { 1, 4, 3, 1 };
                    return CreateOrganicTreeSprite(campoPool[v % campoPool.Length]);
                case BiomeType.Colinas:
                    int[] colinaPool = { 2, 6, 1, 2 };
                    return CreateOrganicTreeSprite(colinaPool[v % colinaPool.Length]);
                case BiomeType.RegiaoDoRio:
                    int[] rioPool = { 0, 0, 4, 1 };
                    return CreateOrganicTreeSprite(rioPool[v % rioPool.Length]);
                case BiomeType.TrilhaDaFloresta:
                    int[] mataPool = { 1, 6, 2, 6 };
                    return CreateOrganicTreeSprite(mataPool[v % mataPool.Length]);
                case BiomeType.PequenaVila:
                    int[] vilaPool = { 3, 4, 1, 3 };
                    return CreateOrganicTreeSprite(vilaPool[v % vilaPool.Length]);
                case BiomeType.BosqueDaLua:
                    int[] luaPool = { 5, 5, 0, 5 };
                    return CreateOrganicTreeSprite(luaPool[v % luaPool.Length]);
                default:
                    return CreateOrganicTreeSprite(v);
            }
        }

        /// <summary>
        /// 7 modelos distintos de árvores orgânicas (Cozy Storybook) para eliminar repetição artificial:
        /// 0 = Salgueiro com passarinho (Vale das Águas),
        /// 1 = Carvalho ilustrado clássico (ou Carvalho antigo volumoso),
        /// 2 = Pinheiro da montanha alto,
        /// 3 = Árvore frutífera florida com maçãs vermelhas,
        /// 4 = Bétula dourada com tronco prateado,
        /// 5 = Árvore Lunar Encantada bioluminescente (Bosque da Lua),
        /// 6 = Abeto / Pinheiro Ancião majestoso de mata fechada.
        /// </summary>
        public static Sprite CreateOrganicTreeSprite(int modelIndex)
        {
            Sprite oakGlobal = LoadOakTreeSprite();
            if (oakGlobal != null) return oakGlobal;
            int key = Mathf.Abs(modelIndex) % 7;
            if (key == 0) return CreateWillowTreeWithBirdSprite();
            if (key == 1)
            {
                Sprite oak = LoadOakTreeSprite();
                if (oak != null) return oak;
            }
            if (key == 3) return CreateFruitTreeSprite();
            if (key == 5) return CreateMoonTreeSprite();
            if (key == 6) return CreateAncientConiferSprite();

            if (organicTreeCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 180, h = 180;
            Texture2D tex = CreateBlankTex(w, h);

            if (key == 1)
            {
                // Carvalho Nobre e Frondoso com tronco estriado e copas orgânicas shaded
                Color trunkShadow = new Color(0.30f, 0.18f, 0.10f, 0.98f);
                Color trunkMid = new Color(0.44f, 0.28f, 0.16f, 0.98f);
                Color trunkLight = new Color(0.58f, 0.38f, 0.22f, 0.98f);

                DrawCurvedBranch(tex, new Vector2(90, 10), new Vector2(66, 6), new Vector2(46, 4), 6f, 2f, trunkShadow, trunkMid);
                DrawCurvedBranch(tex, new Vector2(90, 10), new Vector2(114, 6), new Vector2(134, 4), 6f, 2f, trunkShadow, trunkMid);

                FillEllipse(tex, 90, 10, 36, 8, trunkShadow);
                FillEllipse(tex, 72, 12, 16, 6, trunkMid);
                FillEllipse(tex, 108, 12, 16, 6, trunkMid);

                for (int y = 10; y <= 90; y++)
                {
                    float t = (y - 10f) / 80f;
                    int halfW = Mathf.RoundToInt(Mathf.Lerp(18f, 12f, t));
                    for (int x = 90 - halfW; x <= 90 + halfW; x++)
                    {
                        Color tc = (x < 86) ? trunkShadow : ((x > 94) ? trunkLight : trunkMid);
                        if (x % 5 == 0) tc = Color.Lerp(tc, trunkShadow, 0.25f);
                        tex.SetPixel(x, y, tc);
                    }
                }
                FillEllipse(tex, 68, 82, 22, 7, trunkMid);
                FillEllipse(tex, 114, 86, 24, 7, trunkMid);

                // Galhos curvos visíveis entre os volumes da copa.
                DrawCurvedBranch(tex, new Vector2(90, 78), new Vector2(66, 101), new Vector2(48, 126), 8f, 3f, trunkShadow, trunkLight);
                DrawCurvedBranch(tex, new Vector2(92, 82), new Vector2(116, 101), new Vector2(135, 125), 8f, 3f, trunkShadow, trunkLight);
                DrawCurvedBranch(tex, new Vector2(90, 84), new Vector2(86, 111), new Vector2(82, 137), 7f, 3f, trunkShadow, trunkLight);

                Color oakDark = new Color(0.15f, 0.38f, 0.18f, 0.98f);
                Color oakMid = new Color(0.26f, 0.55f, 0.26f, 0.98f);
                Color oakLight = new Color(0.42f, 0.74f, 0.32f, 0.98f);
                Color oakSun = new Color(0.58f, 0.84f, 0.38f, 0.98f);

                DrawOrganicFoliageLobe(tex, 52, 98, 36, 28, oakMid, oakLight, oakDark);
                DrawOrganicFoliageLobe(tex, 128, 102, 36, 28, oakMid, oakLight, oakDark);
                DrawOrganicFoliageLobe(tex, 90, 118, 44, 32, oakMid, oakLight, oakDark);
                DrawOrganicFoliageLobe(tex, 72, 140, 32, 24, oakLight, oakSun, oakMid);
                DrawOrganicFoliageLobe(tex, 108, 136, 30, 22, oakLight, oakSun, oakMid);
                DrawOrganicFoliageLobe(tex, 90, 154, 24, 18, oakLight, oakSun, oakMid);
                ScatterCanopyHighlights(tex, 90, 120, 78, 53, 11,
                    new Color(0.68f, 0.88f, 0.43f, 0.96f), new Color(0.13f, 0.35f, 0.17f, 0.96f));
            }
            else if (key == 2)
            {
                // Pinheiro da Montanha Alto com boughs detalhados e silhueta serrilhada
                Color barkD = new Color(0.32f, 0.20f, 0.12f, 0.98f);
                Color barkM = new Color(0.46f, 0.28f, 0.16f, 0.98f);
                DrawCurvedBranch(tex, new Vector2(90, 8), new Vector2(72, 6), new Vector2(56, 4), 5f, 2f, barkD, barkM);
                DrawCurvedBranch(tex, new Vector2(90, 8), new Vector2(108, 6), new Vector2(124, 4), 5f, 2f, barkD, barkM);
                FillRect(tex, 86, 6, 94, 52, barkD);
                FillRect(tex, 89, 6, 92, 52, barkM);

                Color pDark = new Color(0.12f, 0.38f, 0.26f, 0.98f);
                Color pMid = new Color(0.18f, 0.50f, 0.32f, 0.98f);
                Color pLight = new Color(0.28f, 0.65f, 0.42f, 0.98f);

                DrawPineBranchTier(tex, 90, 48, 56, 28, pDark, pMid, pLight);
                DrawPineBranchTier(tex, 90, 75, 48, 26, pDark, pMid, pLight);
                DrawPineBranchTier(tex, 90, 100, 38, 24, pDark, pMid, pLight);
                DrawPineBranchTier(tex, 90, 122, 28, 20, pDark, pMid, pLight);
                DrawPineBranchTier(tex, 90, 142, 18, 18, pDark, pMid, pLight);
                DrawPineBranchTier(tex, 90, 158, 10, 14, pDark, pMid, pLight);
                ScatterPineNeedles(tex, 43, new Color(0.44f, 0.76f, 0.48f, 0.98f),
                    new Color(0.10f, 0.32f, 0.22f, 0.98f));
            }
            else
            {
                // Bétula Dourada com tronco prateado manchado e folhagem suave
                Color birchWhite = new Color(0.92f, 0.91f, 0.88f, 0.98f);
                Color birchMark = new Color(0.25f, 0.24f, 0.24f, 0.92f);
                Color birchShadow = new Color(0.72f, 0.70f, 0.66f, 0.98f);

                DrawCurvedBranch(tex, new Vector2(90, 8), new Vector2(76, 5), new Vector2(62, 4), 4f, 1.5f, birchShadow, birchWhite);
                DrawCurvedBranch(tex, new Vector2(90, 8), new Vector2(104, 5), new Vector2(118, 4), 4f, 1.5f, birchShadow, birchWhite);
                FillRect(tex, 86, 6, 94, 95, birchWhite);
                FillRect(tex, 86, 6, 88, 95, birchShadow);
                // Manchas orgânicas da bétula (lenticelas suaves e espaçadas, sem barras horizontais duras)
                int[] markYs = { 18, 30, 44, 58, 70, 82 };
                int[] markOffsets = { -1, 1, -2, 1, -1, 0 };
                for (int m = 0; m < markYs.Length; m++)
                {
                    int my = markYs[m];
                    int mx = 89 + markOffsets[m];
                    FillEllipse(tex, mx, my, 3, 1, birchMark);
                    FillCircle(tex, mx - 2, my, 1, birchShadow);
                }

                DrawCurvedBranch(tex, new Vector2(90, 78), new Vector2(68, 101), new Vector2(52, 126), 6f, 2f, birchShadow, birchWhite);
                DrawCurvedBranch(tex, new Vector2(90, 82), new Vector2(111, 103), new Vector2(128, 127), 6f, 2f, birchShadow, birchWhite);

                Color birchDark = new Color(0.42f, 0.65f, 0.22f, 0.98f);
                Color birchMid = new Color(0.62f, 0.78f, 0.24f, 0.98f);
                Color birchGold = new Color(0.88f, 0.86f, 0.32f, 0.98f);

                DrawOrganicFoliageLobe(tex, 62, 104, 30, 24, birchMid, birchGold, birchDark);
                DrawOrganicFoliageLobe(tex, 118, 108, 30, 24, birchMid, birchGold, birchDark);
                DrawOrganicFoliageLobe(tex, 90, 126, 36, 28, birchMid, birchGold, birchDark);
                DrawOrganicFoliageLobe(tex, 80, 146, 26, 20, birchMid, birchGold, birchDark);
                DrawOrganicFoliageLobe(tex, 104, 142, 24, 18, birchMid, birchGold, birchDark);
                ScatterCanopyHighlights(tex, 90, 123, 66, 47, 29,
                    new Color(0.96f, 0.91f, 0.46f, 0.96f), new Color(0.35f, 0.56f, 0.20f, 0.96f));
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
            organicTreeCache[key] = sp;
            return sp;
        }


    }
}
