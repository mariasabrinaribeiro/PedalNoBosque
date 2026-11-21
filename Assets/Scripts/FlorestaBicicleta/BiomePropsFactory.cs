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

        public static Sprite LoadParkBenchSprite()
        {
            if (cachedBench != null) return cachedBench;
            Texture2D benchTex = Resources.Load<Texture2D>("Floresta/banco_praca");
            cachedBench = benchTex != null
                ? Sprite.Create(benchTex, new Rect(0, 0, benchTex.width, benchTex.height), new Vector2(0.5f, 0.05f), benchTex.height / 2.2f)
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
            FillEllipse(tex, 48, 12, 42, 10, grassDark);
            FillEllipse(tex, 32, 16, 22, 9, grassLight);
            FillEllipse(tex, 64, 15, 22, 9, grassLight);

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
                // Pétalas
                Color p = petals[(i + key * 2) % petals.Length];
                FillCircle(tex, fx[i] - 4, fy[i], 4, p);
                FillCircle(tex, fx[i] + 4, fy[i], 4, p);
                FillCircle(tex, fx[i], fy[i] + 4, 4, p);
                FillCircle(tex, fx[i], fy[i] - 4, 4, p);
                // Miolo dourado
                FillCircle(tex, fx[i], fy[i], 3, new Color(1f, 0.90f, 0.22f, 1f));
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

        /// <summary>Faixa de rio azul-turquesa com gradiente de profundidade e reflexos para o Vale das Águas.</summary>
        public static Sprite CreateRiverWaterSprite()
        {
            if (cachedRiver != null) return cachedRiver;
            int w = 240, h = 64;
            Texture2D tex = CreateBlankTex(w, h);

            Color deepBlue = new Color(0.10f, 0.44f, 0.72f, 0.96f);
            Color turquoise = new Color(0.22f, 0.68f, 0.88f, 0.96f);
            Color surfaceLight = new Color(0.56f, 0.88f, 0.96f, 0.95f);
            Color lilyGreen = new Color(0.20f, 0.62f, 0.28f, 1f);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)h;
                Color rowCol = t < 0.75f
                    ? Color.Lerp(deepBlue, turquoise, t / 0.75f)
                    : Color.Lerp(turquoise, surfaceLight, (t - 0.75f) / 0.25f);
                for (int x = 0; x < w; x++)
                {
                    // Borda orgânica nas extremidades do rio
                    float edgeFade = Mathf.Clamp01(Mathf.Min(x, w - 1 - x) / 16f);
                    Color c = rowCol;
                    c.a *= edgeFade;
                    tex.SetPixel(x, y, c);
                }
            }

            // Reflexos suaves na água e vitórias-régias discretas
            for (int i = 0; i < 12; i++)
            {
                int rx = 20 + (i * 19) % (w - 44);
                int ry = 10 + (i * 13) % (h - 22);
                FillRect(tex, rx, ry, rx + 16, ry + 1, new Color(0.88f, 0.98f, 1f, 0.55f));
            }
            FillEllipse(tex, 42, 46, 11, 4, lilyGreen);
            FillCircle(tex, 42, 48, 3, new Color(1f, 0.72f, 0.84f, 1f));
            FillEllipse(tex, 198, 44, 10, 4, lilyGreen);

            tex.Apply();
            cachedRiver = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 26f);
            return cachedRiver;
        }

        /// <summary>Camada de brilho móvel da correnteza do rio.</summary>
        public static Sprite CreateRiverShimmerSprite()
        {
            if (cachedRiverShimmer != null) return cachedRiverShimmer;
            int w = 220, h = 48;
            Texture2D tex = CreateBlankTex(w, h);
            Color foam = new Color(0.92f, 0.99f, 1f, 0.72f);
            for (int i = 0; i < 16; i++)
            {
                int rx = 12 + (i * 23) % (w - 34);
                int ry = 8 + (i * 11) % (h - 14);
                FillRect(tex, rx, ry, rx + 18, ry + 2, foam);
            }
            tex.Apply();
            cachedRiverShimmer = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 26f);
            return cachedRiverShimmer;
        }

        /// <summary>
        /// Ponte de madeira redesenhada: proporcional à bicicleta, madeira trabalhada,
        /// pilares ancorados na água e corrimão baixo/aberto que não esconde a paisagem!
        /// </summary>
        public static Sprite CreateWoodenBridgeSprite()
        {
            if (cachedBridge != null) return cachedBridge;
            int w = 240, h = 84;
            Texture2D tex = CreateBlankTex(w, h);

            Color woodDark = new Color(0.40f, 0.23f, 0.11f, 1f);
            Color woodMid = new Color(0.62f, 0.39f, 0.19f, 1f);
            Color woodLight = new Color(0.78f, 0.55f, 0.30f, 1f);
            Color rope = new Color(0.84f, 0.72f, 0.48f, 1f);
            Color stone = new Color(0.52f, 0.54f, 0.56f, 1f);
            Color stoneLight = new Color(0.68f, 0.70f, 0.72f, 1f);

            // Pilares de madeira mergulhando na água com base de pedra
            int[] pillars = { 34, 92, 148, 206 };
            for (int i = 0; i < pillars.Length; i++)
            {
                int px = pillars[i];
                FillRect(tex, px - 5, 2, px + 5, 42, woodDark);
                FillRect(tex, px - 2, 4, px + 2, 40, woodMid);
            }

            // Cabeceiras de pedra integrando a ponte às margens
            FillEllipse(tex, 12, 34, 12, 12, stone);
            FillEllipse(tex, 10, 38, 8, 7, stoneLight);
            FillEllipse(tex, w - 12, 34, 12, 12, stone);
            FillEllipse(tex, w - 10, 38, 8, 7, stoneLight);

            // Tablado suavemente arqueado (proporcional e delicado)
            for (int x = 6; x < w - 6; x++)
            {
                float t = (x - 6f) / (w - 12f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 5f);
                int yBot = 34 + arch;
                int yTop = 44 + arch;
                bool plankGap = (x % 14 == 0);
                FillRect(tex, x, yBot, x, yTop, plankGap ? woodDark : woodMid);
                if (!plankGap)
                {
                    FillRect(tex, x, yTop - 2, x, yTop, woodLight);
                    FillRect(tex, x, yBot, x, yBot + 1, woodDark);
                }
            }

            // Postes baixos e abertos do corrimão (não bloqueiam o cenário atrás!)
            for (int px = 14; px <= w - 14; px += 42)
            {
                float t = (px - 6f) / (w - 12f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 5f);
                FillRect(tex, px - 3, 42 + arch, px + 3, 68 + arch, woodDark);
                FillRect(tex, px - 1, 44 + arch, px + 1, 67 + arch, woodLight);
            }

            // Corrimão superior fino e corda/trave média delicada
            for (int x = 14; x <= w - 14; x++)
            {
                float t = (x - 6f) / (w - 12f);
                int arch = Mathf.RoundToInt(Mathf.Sin(t * Mathf.PI) * 5f);
                FillRect(tex, x, 64 + arch, x, 67 + arch, woodLight);
                FillRect(tex, x, 63 + arch, x, 63 + arch, woodDark);
                int ropeDip = Mathf.RoundToInt(Mathf.Sin(((x - 14) % 42) / 42f * Mathf.PI) * 3f);
                FillRect(tex, x, 54 + arch - ropeDip, x, 55 + arch - ropeDip, rope);
            }

            tex.Apply();
            cachedBridge = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.48f), 26f);
            return cachedBridge;
        }

        /// <summary>Margem natural do rio com pedras arredondadas, juncos aquáticos e flores.</summary>
        public static Sprite CreateRiverBankRocksAndReedsSprite()
        {
            if (cachedRiverBank != null) return cachedRiverBank;
            int w = 96, h = 72;
            Texture2D tex = CreateBlankTex(w, h);

            Color reed = new Color(0.18f, 0.54f, 0.24f, 1f);
            Color cattail = new Color(0.46f, 0.26f, 0.14f, 1f);
            Color rockDark = new Color(0.42f, 0.46f, 0.50f, 1f);
            Color rockLight = new Color(0.64f, 0.68f, 0.72f, 1f);

            // Juncos e taboas aquáticas
            int[] rx = { 22, 34, 48, 66, 76 };
            int[] rh = { 54, 62, 48, 58, 44 };
            for (int i = 0; i < rx.Length; i++)
            {
                FillRect(tex, rx[i] - 1, 12, rx[i] + 1, rh[i], reed);
                if (i % 2 == 0)
                    FillEllipse(tex, rx[i], rh[i] - 8, 3, 7, cattail);
            }

            // Pedras na margem
            FillEllipse(tex, 32, 18, 18, 12, rockDark);
            FillEllipse(tex, 29, 21, 12, 8, rockLight);
            FillEllipse(tex, 60, 16, 16, 11, rockDark);
            FillEllipse(tex, 58, 19, 10, 7, rockLight);

            // Florzinhas na margem
            FillCircle(tex, 46, 26, 4, new Color(1f, 0.86f, 0.24f, 1f));
            FillCircle(tex, 74, 24, 4, new Color(1f, 0.48f, 0.72f, 1f));

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
            int key = Mathf.Abs(variant) % 2;
            if (midTreeCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 180, h = 140;
            Texture2D tex = CreateBlankTex(w, h);
            Color trunk = new Color(0.32f, 0.24f, 0.18f, 0.85f);
            Color pineBack = new Color(0.16f, 0.44f, 0.36f, 0.82f);
            Color pineFront = new Color(0.22f, 0.54f, 0.38f, 0.88f);
            Color leafCrown = new Color(0.28f, 0.62f, 0.36f, 0.88f);

            if (key == 0)
            {
                // Pinheiros em camadas
                FillRect(tex, 46, 6, 54, 42, trunk);
                FillTriangle(tex, 18, 34, 82, 34, 50, 84, pineBack);
                FillTriangle(tex, 24, 62, 76, 62, 50, 114, pineBack);

                FillRect(tex, 114, 6, 124, 48, trunk);
                FillTriangle(tex, 80, 38, 158, 38, 119, 96, pineFront);
                FillTriangle(tex, 88, 72, 150, 72, 119, 130, pineFront);
            }
            else
            {
                // Árvores frondosas arredondadas de livro ilustrado
                FillRect(tex, 52, 6, 64, 58, trunk);
                FillCircle(tex, 44, 72, 26, pineBack);
                FillCircle(tex, 74, 74, 24, pineBack);
                FillCircle(tex, 58, 94, 28, leafCrown);

                FillRect(tex, 124, 6, 134, 48, trunk);
                FillCircle(tex, 118, 66, 22, pineFront);
                FillCircle(tex, 142, 66, 20, pineFront);
                FillCircle(tex, 130, 82, 24, leafCrown);
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
            midTreeCache[key] = sp;
            return sp;
        }

        /// <summary>Cachoeira orgânica pintada com paredão rochoso, múltiplos filetes de água, espuma e névoa suave.</summary>
        public static Sprite CreateDistantWaterfallSprite()
        {
            if (cachedWaterfall != null) return cachedWaterfall;
            int w = 160, h = 160;
            Texture2D tex = CreateBlankTex(w, h);

            Color cliffDark = new Color(0.28f, 0.36f, 0.40f, 0.88f);
            Color cliffMid = new Color(0.40f, 0.48f, 0.52f, 0.90f);
            Color mossDark = new Color(0.18f, 0.48f, 0.28f, 0.92f);
            Color mossLight = new Color(0.32f, 0.66f, 0.34f, 0.92f);
            Color aquaDeep = new Color(0.24f, 0.68f, 0.86f, 0.86f);
            Color aquaLight = new Color(0.56f, 0.90f, 0.98f, 0.88f);
            Color foam = new Color(0.94f, 0.99f, 1f, 0.92f);

            // Paredão rochoso orgânico em camadas
            FillEllipse(tex, 42, 68, 36, 62, cliffDark);
            FillEllipse(tex, 118, 62, 34, 56, cliffDark);
            FillEllipse(tex, 48, 76, 26, 46, cliffMid);
            FillEllipse(tex, 112, 70, 24, 42, cliffMid);
            FillEllipse(tex, 40, 122, 34, 18, mossDark);
            FillEllipse(tex, 118, 114, 30, 16, mossDark);
            FillEllipse(tex, 46, 128, 24, 12, mossLight);
            FillEllipse(tex, 114, 120, 20, 10, mossLight);

            // Filetes curvos orgânicos de queda d'água (nunca um retângulo chapado!)
            for (int y = 16; y <= 130; y++)
            {
                float t = (y - 16f) / 114f;
                int center = Mathf.RoundToInt(80f + Mathf.Sin(t * 2.6f) * 6f);
                int halfW = Mathf.RoundToInt(Mathf.Lerp(22f, 13f, t));
                FillRect(tex, center - halfW, y, center + halfW, y, aquaDeep);
                FillRect(tex, center - halfW + 5, y, center + halfW - 6, y, aquaLight);
                if (y % 7 < 4)
                {
                    FillRect(tex, center - 8, y, center - 3, y, foam);
                    FillRect(tex, center + 3, y, center + 7, y, foam);
                }
            }

            // Espuma volumosa no topo da queda e névoa na piscina da base
            FillEllipse(tex, 80, 128, 18, 7, foam);
            FillEllipse(tex, 80, 22, 44, 15, new Color(0.84f, 0.96f, 1f, 0.72f));
            FillEllipse(tex, 58, 18, 26, 11, new Color(1f, 1f, 1f, 0.62f));
            FillEllipse(tex, 104, 18, 26, 11, new Color(1f, 1f, 1f, 0.62f));

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
            if (cachedWillowTree != null) return cachedWillowTree;
            int w = 160, h = 160;
            Texture2D tex = CreateBlankTex(w, h);

            Color trunk = new Color(0.42f, 0.28f, 0.16f, 0.95f);
            Color trunkLight = new Color(0.54f, 0.38f, 0.22f, 0.95f);
            Color crownDark = new Color(0.18f, 0.50f, 0.28f, 0.92f);
            Color crownMid = new Color(0.28f, 0.64f, 0.34f, 0.92f);
            Color frondLight = new Color(0.42f, 0.76f, 0.38f, 0.88f);

            // Tronco elegante + galho lateral onde o passarinho pousa
            FillRect(tex, 72, 6, 88, 92, trunk);
            FillRect(tex, 76, 8, 82, 90, trunkLight);
            FillRect(tex, 84, 72, 122, 78, trunk); // Galho direito visível

            // Copa arredondada de salgueiro
            FillCircle(tex, 56, 108, 30, crownDark);
            FillCircle(tex, 104, 108, 28, crownDark);
            FillCircle(tex, 80, 124, 34, crownMid);

            // Ramos pendentes delicados de salgueiro
            int[] fx = { 34, 48, 62, 96, 112, 124 };
            int[] fl = { 54, 62, 48, 52, 60, 46 };
            for (int i = 0; i < fx.Length; i++)
            {
                FillRect(tex, fx[i] - 2, 104 - fl[i], fx[i] + 2, 104, frondLight);
                FillCircle(tex, fx[i], 104 - fl[i], 3, frondLight);
            }

            // Pequeno passarinho azul pousado discretamente no galho!
            Color birdBlue = new Color(0.26f, 0.70f, 0.98f, 1f);
            FillEllipse(tex, 112, 84, 7, 5, birdBlue);
            FillCircle(tex, 117, 89, 4, birdBlue);
            FillTriangle(tex, 120, 90, 120, 87, 125, 88, new Color(1f, 0.74f, 0.16f, 1f)); // Biquinho
            FillCircle(tex, 118, 90, 1, Color.black);

            tex.Apply();
            cachedWillowTree = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
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
            if (cachedFruitTree != null) return cachedFruitTree;
            int w = 144, h = 152;
            Texture2D tex = CreateBlankTex(w, h);

            FillRect(tex, 64, 6, 80, 72, new Color(0.46f, 0.30f, 0.16f, 0.96f));
            FillCircle(tex, 50, 96, 30, new Color(0.22f, 0.56f, 0.28f, 0.94f));
            FillCircle(tex, 94, 96, 28, new Color(0.22f, 0.56f, 0.28f, 0.94f));
            FillCircle(tex, 72, 114, 34, new Color(0.32f, 0.68f, 0.32f, 0.96f));

            // Maçãzinhas/frutas vermelhas delicadas na copa
            int[] ax = { 46, 68, 92, 58, 82 };
            int[] ay = { 96, 120, 98, 108, 110 };
            for (int i = 0; i < ax.Length; i++)
                FillCircle(tex, ax[i], ay[i], 4, new Color(0.92f, 0.26f, 0.26f, 1f));

            tex.Apply();
            cachedFruitTree = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 28f);
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

        /// <summary>
        /// 5 modelos distintos de árvores orgânicas (Cozy Storybook) para eliminar repetição artificial:
        /// 0 = Salgueiro com passarinho, 1 = Carvalho antigo volumoso, 2 = Pinheiro alto,
        /// 3 = Árvore frutífera florida, 4 = Bétula delicada de folhas douradas.
        /// </summary>
        public static Sprite CreateOrganicTreeSprite(int modelIndex)
        {
            int key = Mathf.Abs(modelIndex) % 5;
            if (key == 0) return CreateWillowTreeWithBirdSprite();
            if (key == 3) return CreateFruitTreeSprite();
            if (organicTreeCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 160, h = 168;
            Texture2D tex = CreateBlankTex(w, h);

            if (key == 1)
            {
                // Carvalho antigo volumoso com raízes, galhos e copa em 3 tons de luz
                Color bark = new Color(0.38f, 0.24f, 0.14f, 0.96f);
                Color barkLight = new Color(0.52f, 0.34f, 0.20f, 0.96f);
                FillEllipse(tex, 80, 12, 28, 10, bark); // Raízes na base
                FillRect(tex, 68, 10, 92, 84, bark);
                FillRect(tex, 74, 12, 82, 82, barkLight);
                FillCircle(tex, 48, 104, 34, new Color(0.16f, 0.44f, 0.22f, 0.95f));
                FillCircle(tex, 112, 102, 32, new Color(0.16f, 0.44f, 0.22f, 0.95f));
                FillCircle(tex, 80, 120, 40, new Color(0.24f, 0.58f, 0.28f, 0.96f));
                FillCircle(tex, 68, 134, 24, new Color(0.38f, 0.72f, 0.32f, 0.95f));
                FillCircle(tex, 96, 128, 22, new Color(0.38f, 0.72f, 0.32f, 0.95f));
            }
            else if (key == 2)
            {
                // Pinheiro alto orgânico com camadas assimétricas de folhagem
                FillRect(tex, 73, 6, 87, 46, new Color(0.36f, 0.22f, 0.12f, 0.96f));
                FillTriangle(tex, 28, 36, 132, 36, 80, 92, new Color(0.14f, 0.42f, 0.30f, 0.95f));
                FillTriangle(tex, 36, 68, 124, 68, 80, 124, new Color(0.18f, 0.50f, 0.34f, 0.96f));
                FillTriangle(tex, 46, 98, 114, 98, 80, 154, new Color(0.26f, 0.60f, 0.38f, 0.96f));
            }
            else
            {
                // Bétula delicada de tronco claro e folhas verde-douradas luminosas
                Color paleBark = new Color(0.86f, 0.84f, 0.78f, 0.96f);
                Color barkMark = new Color(0.36f, 0.34f, 0.32f, 0.85f);
                FillRect(tex, 74, 6, 86, 92, paleBark);
                for (int y = 18; y < 86; y += 14) FillRect(tex, 75, y, 82, y + 2, barkMark);
                FillCircle(tex, 56, 106, 26, new Color(0.46f, 0.72f, 0.28f, 0.92f));
                FillCircle(tex, 102, 104, 24, new Color(0.56f, 0.78f, 0.28f, 0.92f));
                FillCircle(tex, 80, 126, 30, new Color(0.74f, 0.84f, 0.30f, 0.94f));
                FillCircle(tex, 72, 136, 18, new Color(0.92f, 0.88f, 0.38f, 0.90f));
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.04f), 26f);
            organicTreeCache[key] = sp;
            return sp;
        }
    }
}
