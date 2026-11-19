using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Gera sprites 2D nítidos (estilo pixel-art/cartoon com fundo transparente)
    /// para o cenário da floresta, a menina na bicicleta com o cachorrinho e os 5 animais.
    /// </summary>
    public static class FlorestaSpriteFactory
    {
        public enum AnimalKind
        {
            Esquilo,
            Coelho,
            Gato,
            Lobo,
            Quati
        }

        private static Texture2D NewTex(int w, int h, FilterMode filter = FilterMode.Point)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = filter;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static Sprite Finish(Texture2D tex, float ppu = 32f, float pivotY = 0.5f)
        {
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, pivotY), ppu);
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
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
                    if (dx * dx + dy * dy <= r2)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawRect(Texture2D tex, int x0, int y0, int w, int h, Color col)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = x0; x < x0 + w; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int thickness, Color col)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) + 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                if (thickness <= 1)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                        tex.SetPixel(x, y, col);
                }
                else
                {
                    DrawCircle(tex, x, y, thickness / 2, col);
                }
            }
        }

        private static void DrawTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color col)
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
                    bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
                    bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
                    if (!(hasNeg && hasPos))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        // ─── Cenário ─────────────────────────────────────────────────

        public static Sprite CreateSkyGradient()
        {
            Texture2D tex = NewTex(4, 128, FilterMode.Bilinear);
            Color top = new Color(0.24f, 0.62f, 0.96f, 1f);
            Color mid = new Color(0.50f, 0.80f, 0.99f, 1f);
            Color bot = new Color(0.80f, 0.94f, 1f, 1f);
            for (int y = 0; y < 128; y++)
            {
                float t = y / 127f;
                Color c = t < 0.5f ? Color.Lerp(bot, mid, t * 2f) : Color.Lerp(mid, top, (t - 0.5f) * 2f);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
            }
            return Finish(tex, 16f);
        }

        public static Sprite CreateSunSprite()
        {
            Texture2D tex = NewTex(64, 64, FilterMode.Bilinear);
            DrawCircle(tex, 32, 32, 30, new Color(1f, 0.94f, 0.45f, 0.25f));
            DrawCircle(tex, 32, 32, 23, new Color(1f, 0.92f, 0.35f, 0.55f));
            DrawCircle(tex, 32, 32, 17, new Color(1f, 0.88f, 0.22f, 1f));
            DrawCircle(tex, 30, 34, 12, new Color(1f, 0.96f, 0.58f, 1f));
            return Finish(tex, 24f);
        }

        public static Sprite CreateCloudSprite()
        {
            Texture2D tex = NewTex(96, 48, FilterMode.Bilinear);
            Color c = new Color(1f, 1f, 1f, 0.92f);
            Color shadow = new Color(0.88f, 0.94f, 1f, 0.92f);
            DrawEllipse(tex, 48, 16, 34, 10, shadow);
            DrawCircle(tex, 28, 22, 13, c);
            DrawCircle(tex, 46, 28, 16, c);
            DrawCircle(tex, 64, 22, 12, c);
            DrawEllipse(tex, 48, 19, 32, 9, c);
            return Finish(tex, 24f);
        }

        public static Sprite CreateMountainRangeSprite(Color bodyColor, Color snowColor, int seed)
        {
            int w = 256;
            int h = 128;
            Texture2D tex = NewTex(w, h, FilterMode.Point);

            int[] heights = new int[w];
            for (int x = 0; x < w; x++)
            {
                float p1 = Mathf.Sin((x + seed * 37) * 0.032f) * 32f + 56f;
                float p2 = Mathf.Sin((x + seed * 19) * 0.075f) * 18f;
                float ridge = Mathf.Abs(Mathf.Sin((x + seed * 11) * 0.025f)) * 22f;
                heights[x] = Mathf.Clamp(Mathf.RoundToInt(p1 + p2 + ridge), 16, h - 8);
            }

            for (int x = 0; x < w; x++)
            {
                int topY = heights[x];
                for (int y = 0; y <= topY; y++)
                {
                    if (y > topY - 12 && topY > 82)
                        tex.SetPixel(x, y, snowColor);
                    else
                        tex.SetPixel(x, y, bodyColor);
                }
            }
            return Finish(tex, 24f, 0f);
        }

        public static Sprite CreatePineTreeSprite(int variant = 0)
        {
            Texture2D tex = NewTex(48, 84);
            Color trunk = new Color(0.42f, 0.25f, 0.14f, 1f);
            Color leafDark = variant == 0 ? new Color(0.11f, 0.42f, 0.20f, 1f) : new Color(0.15f, 0.48f, 0.22f, 1f);
            Color leafLight = variant == 0 ? new Color(0.18f, 0.56f, 0.27f, 1f) : new Color(0.24f, 0.62f, 0.30f, 1f);

            DrawRect(tex, 20, 0, 8, 22, trunk);
            DrawTriangle(tex, 4, 18, 44, 18, 24, 46, leafDark);
            DrawTriangle(tex, 8, 34, 40, 34, 24, 62, leafLight);
            DrawTriangle(tex, 12, 50, 36, 50, 24, 78, leafDark);
            DrawTriangle(tex, 15, 52, 30, 52, 24, 78, leafLight);
            return Finish(tex, 24f, 0f);
        }

        public static Sprite CreateLeafyTreeSprite()
        {
            Texture2D tex = NewTex(64, 84);
            Color trunk = new Color(0.46f, 0.28f, 0.15f, 1f);
            Color leafDark = new Color(0.15f, 0.49f, 0.19f, 1f);
            Color leafMid = new Color(0.22f, 0.62f, 0.25f, 1f);
            Color leafLight = new Color(0.34f, 0.74f, 0.32f, 1f);

            DrawRect(tex, 27, 0, 10, 30, trunk);
            DrawCircle(tex, 22, 42, 16, leafDark);
            DrawCircle(tex, 42, 42, 16, leafDark);
            DrawCircle(tex, 32, 56, 18, leafMid);
            DrawCircle(tex, 26, 48, 12, leafMid);
            DrawCircle(tex, 36, 60, 10, leafLight);
            return Finish(tex, 24f, 0f);
        }

        public static Sprite CreateRoadTileSprite()
        {
            int w = 128;
            int h = 64;
            Texture2D tex = NewTex(w, h);
            Color dirtBottom = new Color(0.36f, 0.26f, 0.18f, 1f);
            Color asphalt = new Color(0.32f, 0.34f, 0.38f, 1f);
            Color asphaltTop = new Color(0.38f, 0.40f, 0.44f, 1f);
            Color grass = new Color(0.24f, 0.64f, 0.26f, 1f);
            Color grassEdge = new Color(0.32f, 0.76f, 0.32f, 1f);
            Color stripe = new Color(0.98f, 0.86f, 0.34f, 1f);

            DrawRect(tex, 0, 0, w, 14, dirtBottom);
            DrawRect(tex, 0, 14, w, 34, asphalt);
            DrawRect(tex, 0, 44, w, 4, asphaltTop);
            DrawRect(tex, 0, 48, w, 16, grass);
            DrawRect(tex, 0, 48, w, 3, grassEdge);

            // Faixas tracejadas no meio da pista
            DrawRect(tex, 12, 29, 36, 4, stripe);
            DrawRect(tex, 76, 29, 36, 4, stripe);

            // Pequenas flores na grama
            DrawCircle(tex, 20, 56, 2, Color.white);
            DrawCircle(tex, 58, 54, 2, new Color(1f, 0.55f, 0.78f, 1f));
            DrawCircle(tex, 102, 58, 2, Color.yellow);

            return Finish(tex, 32f, 0f);
        }

        // ─── Menina na Bicicleta com Cachorrinho na Cesta ────────────

        public static Sprite CreateWheelSprite()
        {
            Texture2D tex = NewTex(28, 28);
            Color tire = new Color(0.14f, 0.14f, 0.16f, 1f);
            Color rim = new Color(0.84f, 0.86f, 0.90f, 1f);
            Color hub = new Color(0.95f, 0.36f, 0.62f, 1f);

            DrawCircle(tex, 14, 14, 13, tire);
            DrawCircle(tex, 14, 14, 9, Color.clear);
            DrawCircle(tex, 14, 14, 9, rim);
            DrawCircle(tex, 14, 14, 7, Color.clear);

            // Raios para visualizar o giro
            DrawLine(tex, 14, 5, 14, 23, 1, rim);
            DrawLine(tex, 5, 14, 23, 14, 1, rim);
            DrawLine(tex, 8, 8, 20, 20, 1, rim);
            DrawLine(tex, 8, 20, 20, 8, 1, rim);
            DrawCircle(tex, 14, 14, 3, hub);

            return Finish(tex, 32f, 0.5f);
        }

        public static Sprite CreateBikeGirlWithPuppySprite()
        {
            Texture2D tex = NewTex(96, 88);

            Color framePink = new Color(0.96f, 0.34f, 0.62f, 1f);
            Color metal = new Color(0.75f, 0.78f, 0.84f, 1f);
            Color dark = new Color(0.18f, 0.16f, 0.20f, 1f);
            Color skin = new Color(0.98f, 0.80f, 0.66f, 1f);
            Color hair = new Color(0.32f, 0.18f, 0.10f, 1f);
            Color shirt = new Color(0.30f, 0.72f, 0.98f, 1f);
            Color skirt = new Color(0.98f, 0.45f, 0.68f, 1f);
            Color helmet = new Color(1f, 0.85f, 0.25f, 1f);
            Color basket = new Color(0.76f, 0.52f, 0.26f, 1f);
            Color basketDark = new Color(0.58f, 0.38f, 0.18f, 1f);
            Color puppy = new Color(0.88f, 0.64f, 0.35f, 1f);
            Color puppyEar = new Color(0.55f, 0.34f, 0.16f, 1f);

            // Quadro da bicicleta (eixos das rodas ficam em x=24, y=16 e x=68, y=16)
            DrawLine(tex, 24, 16, 42, 18, 3, framePink);
            DrawLine(tex, 42, 18, 36, 40, 3, framePink);
            DrawLine(tex, 24, 16, 36, 40, 3, framePink);
            DrawLine(tex, 36, 40, 58, 42, 3, framePink);
            DrawLine(tex, 42, 18, 58, 42, 3, framePink);
            DrawLine(tex, 58, 42, 68, 16, 3, framePink);
            // Guidão e selim
            DrawLine(tex, 58, 42, 56, 52, 2, metal);
            DrawLine(tex, 56, 52, 50, 52, 2, dark);
            DrawRect(tex, 31, 41, 10, 4, dark);

            // Cesta na frente do guidão
            DrawRect(tex, 60, 40, 15, 11, basket);
            DrawRect(tex, 60, 44, 15, 2, basketDark);
            DrawRect(tex, 60, 49, 15, 2, basketDark);

            // Cachorrinho dentro da cesta!
            DrawCircle(tex, 68, 55, 6, puppy);
            DrawEllipse(tex, 64, 54, 2, 4, puppyEar); // Orelha caída
            DrawEllipse(tex, 72, 54, 2, 4, puppyEar);
            DrawCircle(tex, 71, 56, 1, dark); // Olho
            DrawCircle(tex, 74, 54, 1, dark); // Focinho
            DrawCircle(tex, 67, 53, 1, new Color(0.95f, 0.3f, 0.4f, 1f)); // Coleira

            // Menina pedalando
            // Pernas
            DrawLine(tex, 36, 40, 44, 28, 3, skin);
            DrawLine(tex, 44, 28, 42, 19, 3, skin);
            DrawRect(tex, 40, 17, 6, 3, dark); // Tênis
            // Saia e tronco
            DrawTriangle(tex, 29, 39, 43, 39, 36, 48, skirt);
            DrawRect(tex, 32, 45, 9, 13, shirt);
            // Braço segurando guidão
            DrawLine(tex, 39, 55, 51, 52, 3, skin);
            // Cabeça, cabelo e capacete
            DrawCircle(tex, 38, 65, 7, skin);
            DrawCircle(tex, 33, 64, 5, hair); // Rabo de cavalo / cabelo
            DrawLine(tex, 31, 63, 24, 58, 4, hair);
            DrawCircle(tex, 41, 66, 1, dark); // Olho
            DrawEllipse(tex, 38, 70, 8, 5, helmet); // Capacete

            return Finish(tex, 32f, 0f);
        }

        // ─── Os 5 Animais da Floresta ────────────────────────────────

        public static Sprite CreateAnimalSprite(AnimalKind kind)
        {
            Texture2D tex = NewTex(64, 64);
            Color black = new Color(0.12f, 0.12f, 0.14f, 1f);
            Color pink = new Color(0.98f, 0.65f, 0.75f, 1f);

            switch (kind)
            {
                case AnimalKind.Esquilo:
                {
                    Color fur = new Color(0.76f, 0.42f, 0.18f, 1f);
                    Color belly = new Color(0.95f, 0.80f, 0.62f, 1f);
                    // Cauda fofa enrolada
                    DrawEllipse(tex, 18, 28, 10, 16, fur);
                    DrawCircle(tex, 16, 40, 8, fur);
                    // Corpo e barriga
                    DrawEllipse(tex, 34, 20, 10, 12, fur);
                    DrawEllipse(tex, 37, 19, 6, 8, belly);
                    // Cabeça e orelhas
                    DrawCircle(tex, 38, 36, 9, fur);
                    DrawTriangle(tex, 33, 42, 37, 42, 34, 50, fur);
                    DrawTriangle(tex, 39, 42, 43, 42, 42, 50, fur);
                    DrawCircle(tex, 42, 38, 2, black);
                    DrawCircle(tex, 46, 35, 2, black);
                    DrawRect(tex, 28, 8, 14, 4, fur); // Patinhas
                    break;
                }
                case AnimalKind.Coelho:
                {
                    Color fur = new Color(0.94f, 0.94f, 0.96f, 1f);
                    Color shadow = new Color(0.82f, 0.82f, 0.86f, 1f);
                    // Rabinho pompom
                    DrawCircle(tex, 22, 16, 5, Color.white);
                    // Corpo
                    DrawEllipse(tex, 32, 18, 11, 10, fur);
                    // Cabeça
                    DrawCircle(tex, 40, 31, 9, fur);
                    // Orelhas longas
                    DrawEllipse(tex, 36, 46, 3, 11, fur);
                    DrawEllipse(tex, 36, 46, 1, 8, pink);
                    DrawEllipse(tex, 42, 45, 3, 10, shadow);
                    // Olho e nariz
                    DrawCircle(tex, 43, 33, 2, black);
                    DrawCircle(tex, 48, 30, 2, pink);
                    break;
                }
                case AnimalKind.Gato:
                {
                    Color fur = new Color(0.96f, 0.62f, 0.24f, 1f);
                    Color stripe = new Color(0.75f, 0.38f, 0.10f, 1f);
                    // Cauda curvada
                    DrawLine(tex, 22, 14, 14, 30, 4, fur);
                    // Corpo
                    DrawEllipse(tex, 32, 18, 11, 10, fur);
                    DrawRect(tex, 28, 16, 3, 8, stripe);
                    DrawRect(tex, 34, 16, 3, 8, stripe);
                    // Cabeça e orelhas pontudas
                    DrawCircle(tex, 41, 32, 9, fur);
                    DrawTriangle(tex, 34, 38, 39, 38, 35, 48, fur);
                    DrawTriangle(tex, 42, 38, 47, 38, 46, 48, fur);
                    DrawCircle(tex, 44, 33, 2, black);
                    DrawCircle(tex, 49, 31, 1, pink);
                    break;
                }
                case AnimalKind.Lobo:
                {
                    Color fur = new Color(0.46f, 0.50f, 0.58f, 1f);
                    Color chest = new Color(0.78f, 0.80f, 0.85f, 1f);
                    // Cauda peluda
                    DrawLine(tex, 18, 18, 8, 28, 6, fur);
                    // Corpo
                    DrawEllipse(tex, 30, 20, 14, 10, fur);
                    DrawEllipse(tex, 39, 21, 6, 8, chest);
                    // Patas
                    DrawRect(tex, 21, 6, 4, 10, fur);
                    DrawRect(tex, 36, 6, 4, 10, fur);
                    // Cabeça e focinho alongado
                    DrawCircle(tex, 42, 34, 9, fur);
                    DrawEllipse(tex, 50, 32, 6, 4, chest);
                    DrawTriangle(tex, 36, 40, 40, 40, 37, 50, fur);
                    DrawTriangle(tex, 42, 40, 46, 40, 45, 50, fur);
                    DrawCircle(tex, 44, 36, 2, new Color(1f, 0.82f, 0.25f, 1f)); // Olho dourado amigável
                    DrawCircle(tex, 55, 33, 2, black);
                    break;
                }
                case AnimalKind.Quati:
                {
                    Color fur = new Color(0.54f, 0.36f, 0.22f, 1f);
                    Color darkBand = new Color(0.22f, 0.14f, 0.08f, 1f);
                    Color snout = new Color(0.84f, 0.72f, 0.60f, 1f);
                    // Cauda longa erguida com anéis listrados (marca registrada do quati!)
                    DrawLine(tex, 20, 18, 10, 46, 5, fur);
                    DrawCircle(tex, 17, 26, 3, darkBand);
                    DrawCircle(tex, 14, 34, 3, darkBand);
                    DrawCircle(tex, 11, 42, 3, darkBand);
                    // Corpo
                    DrawEllipse(tex, 30, 17, 12, 9, fur);
                    DrawRect(tex, 22, 6, 4, 8, darkBand);
                    DrawRect(tex, 35, 6, 4, 8, darkBand);
                    // Cabeça e focinho comprido apontando pra frente
                    DrawCircle(tex, 41, 28, 8, fur);
                    DrawEllipse(tex, 51, 27, 7, 3, snout);
                    DrawCircle(tex, 43, 30, 2, black);
                    DrawCircle(tex, 57, 27, 2, black);
                    DrawCircle(tex, 38, 36, 3, fur); // Orelha redonda
                    break;
                }
            }

            return Finish(tex, 32f, 0f);
        }

        public static Sprite CreateFoodIconSprite()
        {
            Texture2D tex = NewTex(32, 32);
            // Balão branco com tigela de comida / frutinha
            DrawCircle(tex, 16, 16, 13, new Color(1f, 1f, 1f, 0.95f));
            DrawCircle(tex, 16, 16, 11, new Color(1f, 0.95f, 0.75f, 1f));
            DrawEllipse(tex, 16, 13, 7, 4, new Color(0.85f, 0.35f, 0.20f, 1f));
            DrawCircle(tex, 13, 18, 3, new Color(0.92f, 0.22f, 0.36f, 1f));
            DrawCircle(tex, 19, 18, 3, new Color(0.95f, 0.65f, 0.18f, 1f));
            return Finish(tex, 32f);
        }

        public static Sprite CreateHeartIconSprite()
        {
            Texture2D tex = NewTex(32, 32);
            Color heart = new Color(0.98f, 0.25f, 0.45f, 1f);
            DrawCircle(tex, 11, 19, 6, heart);
            DrawCircle(tex, 21, 19, 6, heart);
            DrawTriangle(tex, 5, 17, 27, 17, 16, 6, heart);
            return Finish(tex, 32f);
        }
    }
}
