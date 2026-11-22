using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Carrega as artes 2D ilustradas de Assets/Resources/Floresta/
    /// (com fallback seguro caso algum arquivo ainda não tenha sido importado).
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

        private static Sprite cachedSeamlessBgSprite;
        private static Sprite cachedRealWalkingGirlSprite;
        private static Sprite cachedRealRunningPuppySprite;
        private static Sprite cachedRealParkedBikeSprite;
        private static Texture2D cachedReadableGirlBikeTex;

        private static Texture2D GetReadableTextureCopy(Texture2D source)
        {
            if (source == null) return null;
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.filterMode = FilterMode.Bilinear;
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static Texture2D GetReadableGirlBikeTexture()
        {
            if (cachedReadableGirlBikeTex != null) return cachedReadableGirlBikeTex;
            Texture2D raw = Resources.Load<Texture2D>("Floresta/girl_bike");
            if (raw == null) return null;
            cachedReadableGirlBikeTex = GetReadableTextureCopy(raw);
            return cachedReadableGirlBikeTex;
        }

        private static Sprite LoadResourceSprite(string resourcePath, float targetWorldHeight, float pivotY = 0f)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;

            float ppu = tex.height / Mathf.Max(0.1f, targetWorldHeight);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, pivotY), ppu);
        }

        /// <summary>
        /// Carrega bg_floresta.png e suaviza a transição horizontal entre as bordas esquerda e direita,
        /// eliminando 100% da linha vertical de recorte na estrada de terra, no gramado e no céu!
        /// </summary>
        public static Sprite LoadForestBackgroundSprite(float cameraWorldHeight)
        {
            if (cachedSeamlessBgSprite != null) return cachedSeamlessBgSprite;
            Texture2D raw = Resources.Load<Texture2D>("Floresta/bg_floresta");
            if (raw == null) return null;

            Texture2D seamless = GetReadableTextureCopy(raw);
            int w = seamless.width;
            int h = seamless.height;
            Color[] pixels = seamless.GetPixels();

            // Faixa de fusão horizontal contínua nas últimas 120 colunas para casar perfeitamente com a coluna 0 do próximo bloco
            const int blendWidth = 120;
            for (int y = 0; y < h; y++)
            {
                int rowStart = y * w;
                // Usa média suave das primeiras 4 colunas da esquerda como alvo da costura
                Color leftTarget = (pixels[rowStart] + pixels[rowStart + 1] + pixels[rowStart + 2] + pixels[rowStart + 3]) * 0.25f;
                for (int i = 0; i < blendWidth; i++)
                {
                    int x = (w - blendWidth) + i;
                    float t = (i + 1f) / blendWidth;
                    float smoothT = t * t * (3f - 2f * t);
                    pixels[rowStart + x] = Color.Lerp(pixels[rowStart + x], leftTarget, smoothT);
                }
            }

            seamless.SetPixels(pixels);
            seamless.Apply();
            float ppu = h / Mathf.Max(0.1f, cameraWorldHeight);
            cachedSeamlessBgSprite = Sprite.Create(seamless, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
            return cachedSeamlessBgSprite;
        }

        public static Sprite LoadGirlBikeSprite()
        {
            return LoadResourceSprite("Floresta/girl_bike", 3.3f, 0.05f);
        }

        /// <summary>
        /// Extrai a PRÓPRIA protagonista de girl_bike.png (1024x1024) para quando ela desce da bicicleta,
        /// mantendo exatamente o mesmo rosto, capacete, cabelo, blusa listrada, calça jeans e tênis!
        /// </summary>
        public static Sprite ExtractRealWalkingGirlSprite()
        {
            if (cachedRealWalkingGirlSprite != null) return cachedRealWalkingGirlSprite;
            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            int cropX0 = 200, cropX1 = 640;
            int cropY0 = 140, cropY1 = 915;
            int outW = cropX1 - cropX0 + 1;
            int outH = cropY1 - cropY0 + 1;
            Texture2D outTex = NewTex(outW, outH);

            // 1. Copia a parte superior e o tronco real da menina de girl_bike.png (unityY = 408..915)
            for (int sy = 408; sy <= cropY1; sy++)
            {
                for (int sx = cropX0; sx <= cropX1; sx++)
                {
                    // Remove apenas a ponta do guidão/cesta à direita (sx > 584 abaixo de sy = 562)
                    // e a ponta traseira do selim marrom à esquerda (sx < 406 abaixo de sy = 448)
                    if (sx > 584 && sy < 562) continue;
                    if (sx < 406 && sy < 448) continue;

                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.08f) continue;

                    // Filtra pixels cor-de-rosa do quadro da bicicleta abaixo da cintura (sy < 455)
                    if (sy < 455 && c.r > 0.72f && c.r > c.g + 0.12f && c.b > c.g + 0.04f)
                        continue;

                    outTex.SetPixel(sx - cropX0, sy - cropY0, c);
                }
            }

            // 2. Copia a perna de calça jeans real da menina (unityY = 256..407, sx = 426..518)
            // e duplica levemente deslocada atrás para formar as duas pernas em pé
            Color ink = new Color(0.14f, 0.07f, 0.05f, 1f);
            Color jeansDark = new Color(0.24f, 0.43f, 0.64f, 1f);
            for (int sy = 256; sy < 408; sy++)
            {
                for (int sx = 426; sx <= 518; sx++)
                {
                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.15f) continue;
                    bool isJeans = c.b > c.r + 0.05f;
                    bool isDarkOutline = c.r < 0.30f && c.g < 0.24f && c.b < 0.24f;
                    if (isJeans || isDarkOutline)
                    {
                        int dx = sx - cropX0;
                        int dy = sy - cropY0;
                        outTex.SetPixel(dx, dy, c);
                        // Perna de trás levemente sombreada
                        int backDx = dx - 28;
                        if (backDx >= 0 && outTex.GetPixel(backDx, dy).a < 0.1f)
                        {
                            Color backCol = isDarkOutline ? ink : Color.Lerp(c, jeansDark, 0.35f);
                            outTex.SetPixel(backDx, dy, backCol);
                        }
                    }
                }
            }

            // 3. Tênis rosa e branco originais da menina na base das duas pernas (sy = 168..260 => dy = 28..120)
            Color shoePink = new Color(0.92f, 0.56f, 0.67f, 1f);
            Color shoeSole = new Color(0.97f, 0.96f, 0.94f, 1f);
            int[] legCentersX = { 444 - cropX0, 474 - cropX0 };
            for (int i = 0; i < legCentersX.Length; i++)
            {
                int lx = legCentersX[i];
                DrawEllipse(outTex, lx + 6, 98, 28, 17, ink);
                DrawEllipse(outTex, lx + 6, 99, 23, 13, shoePink);
                DrawEllipse(outTex, lx + 8, 92, 24, 6, shoeSole);
                DrawCircle(outTex, lx + 12, 104, 4, Color.white);
            }

            outTex.Apply();
            float ppu = 1024f / 3.3f;
            cachedRealWalkingGirlSprite = Sprite.Create(
                outTex,
                new Rect(0, 0, outW, outH),
                new Vector2((465f - cropX0) / outW, 78f / outH),
                ppu);
            return cachedRealWalkingGirlSprite;
        }

        /// <summary>
        /// Extrai o PRÓPRIO cachorrinho dourado da cesta de girl_bike.png (1024x1024) e completa
        /// as patinhas e rabinho na mesma paleta exata, para ele correr feliz no chão ao lado da menina
        /// e também aparecer como cãozinho amigo pelo cenário!
        /// </summary>
        public static Sprite ExtractRealRunningPuppySprite()
        {
            if (cachedRealRunningPuppySprite != null) return cachedRealRunningPuppySprite;
            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            int outW = 220, outH = 210;
            Texture2D outTex = NewTex(outW, outH);

            // Paleta medida diretamente dos pixels do cachorrinho em girl_bike.png
            Color ink = new Color(0.14f, 0.07f, 0.05f, 1f);
            Color furDark = new Color(0.776f, 0.545f, 0.325f, 1f);
            Color furMid = new Color(0.906f, 0.706f, 0.482f, 1f);
            Color furHigh = new Color(1.000f, 0.855f, 0.651f, 1f);
            Color collarRed = new Color(0.92f, 0.32f, 0.34f, 1f);

            // 1. Corpinho fofo, rabinho abanando e patinhas na metade inferior (y = 14..88)
            // Rabinho dourado levantado abanando à esquerda
            DrawEllipse(outTex, 58, 72, 22, 11, ink);
            DrawEllipse(outTex, 58, 72, 18, 7, furMid);
            DrawEllipse(outTex, 56, 74, 12, 4, furHigh);

            // Patinhas traseiras e dianteiras com contorno #24120C
            int[] pawX = { 76, 100, 126, 148 };
            for (int i = 0; i < pawX.Length; i++)
            {
                int px = pawX[i];
                DrawEllipse(outTex, px, 28, 13, 16, ink);
                DrawEllipse(outTex, px, 29, 9, 12, (i % 2 == 0) ? furDark : furMid);
                DrawEllipse(outTex, px + 2, 18, 12, 7, ink);
                DrawEllipse(outTex, px + 2, 19, 8, 4, furHigh);
            }

            // Tronco e barriguinha macia do cachorrinho
            DrawEllipse(outTex, 112, 56, 46, 28, ink);
            DrawEllipse(outTex, 112, 56, 41, 23, furMid);
            DrawEllipse(outTex, 116, 52, 28, 15, furHigh);

            // Coleirinha vermelha charmosa com medalhinha dourada
            DrawEllipse(outTex, 128, 72, 26, 8, ink);
            DrawEllipse(outTex, 128, 72, 22, 5, collarRed);
            DrawCircle(outTex, 142, 66, 6, ink);
            DrawCircle(outTex, 142, 66, 4, new Color(1f, 0.86f, 0.24f, 1f));

            // 2. Copia a cabeça, orelhinhas caídas, focinho sorridente e patinhas originais de girl_bike.png (x=675..829, y=504..633)
            int srcX0 = 675, srcX1 = 829;
            int srcY0 = 504, srcY1 = 633;
            int destOffsetX = 36;
            int destOffsetY = 68;
            for (int sy = srcY0; sy <= srcY1; sy++)
            {
                for (int sx = srcX0; sx <= srcX1; sx++)
                {
                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.12f) continue;
                    int dx = (sx - srcX0) + destOffsetX;
                    int dy = (sy - srcY0) + destOffsetY;
                    if (dx >= 0 && dx < outW && dy >= 0 && dy < outH)
                    {
                        outTex.SetPixel(dx, dy, c);
                    }
                }
            }

            outTex.Apply();
            float ppu = 1024f / 3.3f;
            cachedRealRunningPuppySprite = Sprite.Create(
                outTex,
                new Rect(0, 0, outW, outH),
                new Vector2(0.52f, 12f / outH),
                ppu);
            return cachedRealRunningPuppySprite;
        }

        /// <summary>
        /// Extrai a PRÓPRIA bicicleta rosa com cesta de vime de girl_bike.png (1024x1024),
        /// removendo a menina e o cachorrinho para quando eles descem para explorar a pé!
        /// </summary>
        public static Sprite ExtractRealParkedBikeSprite()
        {
            if (cachedRealParkedBikeSprite != null) return cachedRealParkedBikeSprite;
            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            int w = src.width, h = src.height;
            Texture2D outTex = NewTex(w, h);

            for (int y = 70; y <= 630; y++)
            {
                for (int x = 115; x <= 905; x++)
                {
                    // Remove o cachorrinho acima da borda da cesta (x=672..834, y=502..635)
                    if (x >= 672 && x <= 834 && y >= 502) continue;

                    // Remove o tronco, braços e cabeça da menina acima do selim/quadro
                    if (x >= 210 && x <= 582 && y >= 435) continue;
                    if (x >= 555 && x <= 620 && y >= 545) continue;

                    Color c = src.GetPixel(x, y);
                    if (c.a < 0.08f) continue;

                    // Remove a perna de calça jeans azul da menina (x=415..555, y=245..445)
                    if (x >= 415 && x <= 555 && y >= 245 && y <= 445)
                    {
                        bool isBlueJeans = c.b > c.r + 0.04f;
                        if (isBlueJeans) continue;
                    }
                    // Remove o tênis da menina junto ao pedal (x=428..524, y=155..255) mantendo o aro e quadro
                    if (x >= 435 && x <= 520 && y >= 158 && y <= 238)
                    {
                        bool isWheelOrCrank = x < 445 || y > 210;
                        if (!isWheelOrCrank) continue;
                    }

                    outTex.SetPixel(x, y, c);
                }
            }

            outTex.Apply();
            float ppu = h / 3.3f;
            cachedRealParkedBikeSprite = Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), ppu);
            return cachedRealParkedBikeSprite;
        }

        public static Sprite LoadAnimalSprite(AnimalKind kind)
        {
            string fileName;
            float height;
            switch (kind)
            {
                case AnimalKind.Esquilo: fileName = "animal_esquilo"; height = 1.15f; break;
                case AnimalKind.Coelho:  fileName = "animal_coelho";  height = 1.15f; break;
                case AnimalKind.Gato:    fileName = "animal_gato";    height = 1.25f; break;
                case AnimalKind.Lobo:    fileName = "animal_lobo";    height = 2.1f;  break;
                case AnimalKind.Quati:   fileName = "animal_quati";   height = 1.45f; break;
                default:                 fileName = "animal_esquilo"; height = 1.15f; break;
            }
            return LoadResourceSprite($"Floresta/{fileName}", height, 0.05f);
        }

        // Ícones suaves de balão de comida e coração
        private static Texture2D NewTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
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
                    if (!(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0))))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        public static Sprite CreateFoodIconSprite()
        {
            Texture2D tex = NewTex(64, 64);
            DrawCircle(tex, 32, 32, 28, new Color(0.95f, 0.55f, 0.25f, 0.95f));
            DrawCircle(tex, 32, 32, 24, new Color(1f, 0.98f, 0.90f, 1f));
            DrawEllipse(tex, 32, 25, 14, 7, new Color(0.82f, 0.34f, 0.18f, 1f));
            DrawCircle(tex, 25, 35, 6, new Color(0.92f, 0.22f, 0.36f, 1f));
            DrawCircle(tex, 38, 35, 6, new Color(0.96f, 0.68f, 0.18f, 1f));
            DrawCircle(tex, 32, 40, 5, new Color(0.32f, 0.72f, 0.30f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateHeartIconSprite()
        {
            Texture2D tex = NewTex(64, 64);
            DrawCircle(tex, 32, 32, 28, new Color(1f, 0.95f, 0.97f, 0.92f));
            Color heart = new Color(0.98f, 0.24f, 0.46f, 1f);
            DrawCircle(tex, 24, 37, 10, heart);
            DrawCircle(tex, 40, 37, 10, heart);
            DrawTriangle(tex, 15, 33, 49, 33, 32, 14, heart);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
