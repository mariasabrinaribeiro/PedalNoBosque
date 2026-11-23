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

        public enum PuppyAnimState
        {
            Feliz,
            Correndo,
            Parado,
            Farejando,
            Curioso,
            RecebendoCarinho,
            NaCestinha,
            Descansando
        }

        private static Sprite cachedSeamlessBgSprite;
        private static Sprite cachedSeamlessRoadStripSprite;
        private static Sprite cachedGirlBikeNoPuppySprite;
        private static Sprite cachedRealWalkingGirlSprite;
        private static Sprite[] cachedWalkingGirlFrames;
        private static Sprite cachedRealRunningPuppySprite;
        private static readonly System.Collections.Generic.Dictionary<int, Sprite[]> cachedPuppyStateFrames =
            new System.Collections.Generic.Dictionary<int, Sprite[]>();
        private static readonly System.Collections.Generic.Dictionary<int, Sprite[]> cachedBasketPuppyStateFrames =
            new System.Collections.Generic.Dictionary<int, Sprite[]>();
        private static Sprite cachedRealParkedBikeSprite;
        private static Texture2D cachedReadableGirlBikeTex;
        private static Texture2D cachedReadableBgTex;

        private static Texture2D GetReadableBgTexture()
        {
            if (cachedReadableBgTex != null) return cachedReadableBgTex;
            Texture2D raw = Resources.Load<Texture2D>("Floresta/bg_floresta");
            if (raw == null) return null;
            cachedReadableBgTex = GetReadableTextureCopy(raw);
            return cachedReadableBgTex;
        }

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
        /// Carrega bg_floresta.png preservando 100% da pintura original para a camada de montanhas e floresta distante,
        /// aplicando suavização sutil apenas nos 5px extremos das bordas para emenda espelhada (flipX) 100% invisível.
        /// </summary>
        public static Sprite LoadForestBackgroundSprite(float cameraWorldHeight)
        {
            if (cachedSeamlessBgSprite != null) return cachedSeamlessBgSprite;
            Texture2D src = GetReadableBgTexture();
            if (src == null)
            {
                cachedSeamlessBgSprite = LoadResourceSprite("Floresta/bg_floresta", cameraWorldHeight, 0.5f);
                return cachedSeamlessBgSprite;
            }

            int w = src.width;
            int h = src.height;
            Texture2D seamlessTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            seamlessTex.filterMode = FilterMode.Bilinear;
            seamlessTex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = src.GetPixels();
            // Suavização espelhada nos 5px mais externos para eliminar qualquer variação brusca na emenda flipX
            const int edgeSmooth = 5;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                Color leftAnchor = pixels[row + edgeSmooth];
                Color rightAnchor = pixels[row + (w - 1 - edgeSmooth)];
                for (int dx = 0; dx < edgeSmooth; dx++)
                {
                    float t = dx / (float)edgeSmooth;
                    pixels[row + dx] = Color.Lerp(leftAnchor, pixels[row + dx], t * 0.5f + 0.5f);
                    pixels[row + (w - 1 - dx)] = Color.Lerp(rightAnchor, pixels[row + (w - 1 - dx)], t * 0.5f + 0.5f);
                }
            }

            seamlessTex.SetPixels(pixels);
            seamlessTex.Apply();

            float ppu = h / Mathf.Max(0.1f, cameraWorldHeight);
            cachedSeamlessBgSprite = Sprite.Create(seamlessTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
            return cachedSeamlessBgSprite;
        }

        /// <summary>
        /// Extrai APENAS a faixa da estrada e suas margens imediatas de grama de bg_floresta.png (linhas 0..164 de 768)
        /// com equalização suave da luminosidade da estrada nas bordas e transição alfa suave no topo,
        /// para que a ESTRADA + meio-fio se movam a 100% (1.0x) presos ao mundo físico sem nenhuma emenda vertical!
        /// </summary>
        public static Sprite LoadForestRoadStripSprite(float cameraWorldHeight)
        {
            if (cachedSeamlessRoadStripSprite != null) return cachedSeamlessRoadStripSprite;
            Texture2D src = GetReadableBgTexture();
            if (src == null) return null;

            int w = src.width;
            // ~21.4% de 768px = 164px: cobre a grama inferior (0..48), a estrada inteira (48..124)
            // e a borda imediata do meio-fio superior (124..164), mantendo a campina superior no parallax distante!
            int stripH = Mathf.Clamp(Mathf.RoundToInt(src.height * 0.214f), 148, src.height);
            int featherRows = 26;
            Texture2D stripTex = new Texture2D(w, stripH, TextureFormat.RGBA32, false);
            stripTex.filterMode = FilterMode.Bilinear;
            stripTex.wrapMode = TextureWrapMode.Clamp;

            Color[] srcPixels = src.GetPixels(0, 0, w, stripH);

            // Equaliza o gradiente de iluminação esquerda/direita na faixa de terra batida da estrada (44..128)
            // para eliminar qualquer degrau de tonalidade (claro/escuro) entre segmentos consecutivos da estrada!
            int roadY0 = Mathf.Clamp(Mathf.RoundToInt(src.height * 0.058f), 0, stripH - 1);
            int roadY1 = Mathf.Clamp(Mathf.RoundToInt(src.height * 0.166f), roadY0 + 1, stripH - 1);
            int blendSpan = Mathf.Min(180, w / 4);
            for (int y = roadY0; y <= roadY1; y++)
            {
                int rowOffset = y * w;
                Color midRef = srcPixels[rowOffset + w / 2];
                for (int dx = 0; dx < blendSpan; dx++)
                {
                    float u = 1f - (dx / (float)blendSpan);
                    float wBlend = u * u * (3f - 2f * u) * 0.65f;

                    Color cL = srcPixels[rowOffset + dx];
                    float lumL = Mathf.Max(0.05f, cL.r * 0.299f + cL.g * 0.587f + cL.b * 0.114f);
                    float lumMid = Mathf.Max(0.05f, midRef.r * 0.299f + midRef.g * 0.587f + midRef.b * 0.114f);
                    float ratioL = Mathf.Clamp(lumMid / lumL, 0.85f, 1.20f);
                    Color normL = new Color(
                        Mathf.Clamp01(cL.r * Mathf.Lerp(1f, ratioL, wBlend)),
                        Mathf.Clamp01(cL.g * Mathf.Lerp(1f, ratioL, wBlend)),
                        Mathf.Clamp01(cL.b * Mathf.Lerp(1f, ratioL, wBlend)),
                        cL.a);
                    srcPixels[rowOffset + dx] = Color.Lerp(normL, midRef, wBlend * 0.25f);

                    int rx = w - 1 - dx;
                    Color cR = srcPixels[rowOffset + rx];
                    float lumR = Mathf.Max(0.05f, cR.r * 0.299f + cR.g * 0.587f + cR.b * 0.114f);
                    float ratioR = Mathf.Clamp(lumMid / lumR, 0.85f, 1.20f);
                    Color normR = new Color(
                        Mathf.Clamp01(cR.r * Mathf.Lerp(1f, ratioR, wBlend)),
                        Mathf.Clamp01(cR.g * Mathf.Lerp(1f, ratioR, wBlend)),
                        Mathf.Clamp01(cR.b * Mathf.Lerp(1f, ratioR, wBlend)),
                        cR.a);
                    srcPixels[rowOffset + rx] = Color.Lerp(normR, midRef, wBlend * 0.25f);
                }
            }

            // Transição alfa suave no topo da borda gramada superior (sem corte duro)
            for (int y = stripH - featherRows; y < stripH; y++)
            {
                float alpha = 1f - (y - (stripH - featherRows)) / (float)featherRows;
                alpha = alpha * alpha * (3f - 2f * alpha); // smoothstep
                int rowOffset = y * w;
                for (int x = 0; x < w; x++)
                {
                    Color c = srcPixels[rowOffset + x];
                    c.a *= alpha;
                    srcPixels[rowOffset + x] = c;
                }
            }

            stripTex.SetPixels(srcPixels);
            stripTex.Apply();

            float ppu = src.height / Mathf.Max(0.1f, cameraWorldHeight);
            cachedSeamlessRoadStripSprite = Sprite.Create(
                stripTex,
                new Rect(0, 0, w, stripH),
                new Vector2(0.5f, 0f),
                ppu);
            return cachedSeamlessRoadStripSprite;
        }

        public static Sprite LoadGirlBikeSprite()
        {
            // Pivô Y = 83/1024 (0.08105f) corresponde exatamente à base inferior dos pneus em girl_bike.png
            return LoadResourceSprite("Floresta/girl_bike", 3.3f, 83f / 1024f);
        }

        /// <summary>
        /// Cópia em memória de girl_bike.png removendo apenas a cabecinha estática do cachorrinho acima da borda
        /// da cestinha de vime (x=682..832, y=510..645) para que o cachorrinho expressivo animado (basketPuppySR)
        /// se encaixe perfeitamente na cestinha sem nenhuma duplicação! O arquivo original em disco permanece intacto.
        /// </summary>
        public static Sprite LoadGirlBikeWithoutBasketPuppySprite()
        {
            if (cachedGirlBikeNoPuppySprite != null) return cachedGirlBikeNoPuppySprite;
            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return LoadGirlBikeSprite();

            int w = src.width, h = src.height;
            Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.filterMode = FilterMode.Bilinear;
            copy.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = src.GetPixels();

            for (int y = 510; y <= 645 && y < h; y++)
            {
                int row = y * w;
                int minX = (y >= 542) ? 674 : 684;
                for (int x = minX; x <= 834 && x < w; x++)
                {
                    pixels[row + x] = Color.clear;
                }
            }

            copy.SetPixels(pixels);
            copy.Apply();
            float ppu = h / 3.3f;
            cachedGirlBikeNoPuppySprite = Sprite.Create(copy, new Rect(0, 0, w, h), new Vector2(0.5f, 83f / 1024f), ppu);
            return cachedGirlBikeNoPuppySprite;
        }

        /// <summary>
        /// Extrai a PRÓPRIA protagonista de girl_bike.png (1024x1024) para quando ela desce da bicicleta,
        /// removendo a pose sentada de pedalar e desenhando as pernas, braços e pés em postura natural
        /// em pé e caminhando (3 quadros animados: Parada, Passo Direito, Passo Esquerdo).
        /// </summary>
        public static Sprite ExtractRealWalkingGirlSprite()
        {
            if (cachedRealWalkingGirlSprite != null) return cachedRealWalkingGirlSprite;
            Sprite[] frames = GetWalkingGirlSprites();
            cachedRealWalkingGirlSprite = (frames != null && frames.Length > 0) ? frames[0] : null;
            return cachedRealWalkingGirlSprite;
        }

        public static Sprite[] GetWalkingGirlSprites()
        {
            if (cachedWalkingGirlFrames != null && cachedWalkingGirlFrames.Length == 3 && cachedWalkingGirlFrames[0] != null)
                return cachedWalkingGirlFrames;

            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            cachedWalkingGirlFrames = new Sprite[3];
            for (int f = 0; f < 3; f++)
            {
                cachedWalkingGirlFrames[f] = BuildWalkingGirlFrameFromSource(src, f);
            }
            cachedRealWalkingGirlSprite = cachedWalkingGirlFrames[0];
            return cachedWalkingGirlFrames;
        }

        private static Sprite BuildWalkingGirlFrameFromSource(Texture2D src, int frameIndex)
        {
            int cropX0 = 200, cropX1 = 640;
            int cropY0 = 140, cropY1 = 915;
            int outW = cropX1 - cropX0 + 1;
            int outH = cropY1 - cropY0 + 1;
            Texture2D outTex = NewTex(outW, outH);

            // 1. Copia a cabeça, capacete azul, cabelo ao vento e torso de blusa listrada da menina (sy = 496..915)
            // removendo completamente o braço esticado de pedalar (sx > 488 e sy < 602) e a calça dobrada sentada,
            // preservando 100% da identidade original sem deixar nenhum resto de guidão ou cotovelo esticado!
            for (int sy = 496; sy <= cropY1; sy++)
            {
                for (int sx = cropX0; sx <= cropX1; sx++)
                {
                    if (sx > 488 && sy < 602) continue;
                    if (sx > 504 && sy < 626) continue;

                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.08f) continue;

                    // Remove restos azuis da calça dobrada na base do recorte (abaixo da barra da camisa)
                    if (sy < 516 && c.b > c.r + 0.06f) continue;

                    outTex.SetPixel(sx - cropX0, sy - cropY0, c);
                }
            }

            Color ink = new Color(0.16f, 0.08f, 0.06f, 1f);
            Color skin = new Color(0.98f, 0.78f, 0.64f, 1f);
            Color skinShadow = new Color(0.90f, 0.67f, 0.53f, 1f);
            Color shirtCoral = new Color(0.93f, 0.49f, 0.37f, 1f);
            Color shirtCream = new Color(0.99f, 0.93f, 0.84f, 1f);
            Color jeansDark = new Color(0.22f, 0.40f, 0.60f, 1f);
            Color jeansMid = new Color(0.30f, 0.52f, 0.74f, 1f);
            Color jeansLight = new Color(0.42f, 0.64f, 0.85f, 1f);
            Color shoePink = new Color(0.92f, 0.56f, 0.67f, 1f);
            Color shoeSole = new Color(0.97f, 0.96f, 0.94f, 1f);

            // Deslocamento das duas pernas e do braço conforme o quadro:
            // frame 0 = Parada em pé ao lado da bicicleta
            // frame 1 = Caminhando (passo direito à frente)
            // frame 2 = Caminhando (passo esquerdo à frente)
            int backFootOffset = (frameIndex == 1) ? -36 : (frameIndex == 2 ? 32 : -14);
            int frontFootOffset = (frameIndex == 1) ? 36 : (frameIndex == 2 ? -30 : 14);
            int armSwingX = (frameIndex == 1) ? -22 : (frameIndex == 2 ? 26 : 4);

            // Proporção Cozy Storybook fiel a girl_bike.png (pernas proporcionais, não alongadas!)
            int hipCenterX = 428 - cropX0; // 228
            int hipY = 494 - cropY0;       // 354
            int ankleY = 268 - cropY0;     // 128

            // 2. Perna de trás (dois passes: contorno liso primeiro, preenchimento jeans depois — ZERO listras!)
            DrawStandingLeg(outTex, hipCenterX - 10, hipY, hipCenterX + backFootOffset, ankleY,
                ink, jeansDark, jeansMid, shoePink, shoeSole);

            // 3. Cintura / quadril da calça jeans unindo suavemente à blusa listrada
            DrawEllipse(outTex, hipCenterX, hipY + 4, 42, 24, ink);
            DrawEllipse(outTex, hipCenterX, hipY + 4, 38, 20, jeansMid);
            DrawEllipse(outTex, hipCenterX + 4, hipY + 6, 22, 12, jeansLight);

            // Barra inferior da blusa listrada sobre o cós da calça (cobre suavemente o corte inferior)
            DrawEllipse(outTex, hipCenterX + 2, hipY + 20, 44, 14, ink);
            DrawEllipse(outTex, hipCenterX + 2, hipY + 20, 40, 11, shirtCoral);
            DrawEllipse(outTex, hipCenterX + 2, hipY + 23, 36, 5, shirtCream);

            // 4. Perna da frente (dois passes lisos + tênis rosa e branco)
            DrawStandingLeg(outTex, hipCenterX + 9, hipY, hipCenterX + frontFootOffset, ankleY,
                ink, jeansMid, jeansLight, shoePink, shoeSole);

            // 5. Braço natural relaxado ao lado do corpo em DOIS PASSES (contorno liso + pele lisa, ZERO anéis!)
            int shoulderX = 462 - cropX0;
            int shoulderY = 588 - cropY0;
            int handX = shoulderX + 6 + armSwingX;
            int handY = 470 - cropY0;

            // Passe 1: contorno ink contínuo do braço e mão
            int steps = 28;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int ax = Mathf.RoundToInt(Mathf.Lerp(shoulderX + 2, handX, t));
                int ay = Mathf.RoundToInt(Mathf.Lerp(shoulderY - 16, handY, t));
                DrawCircle(outTex, ax, ay, 13, ink);
            }
            DrawCircle(outTex, handX + 1, handY - 2, 13, ink);

            // Passe 2: preenchimento suave de pele e sombra do braço e mão
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int ax = Mathf.RoundToInt(Mathf.Lerp(shoulderX + 2, handX, t));
                int ay = Mathf.RoundToInt(Mathf.Lerp(shoulderY - 16, handY, t));
                DrawCircle(outTex, ax, ay, 9, skin);
                DrawCircle(outTex, ax - 3, ay, 4, skinShadow);
            }
            DrawCircle(outTex, handX + 1, handY - 2, 9, skin);

            // Manga listrada no ombro por cima da junção do braço (cobre suavemente o corte do ombro)
            DrawEllipse(outTex, shoulderX, shoulderY - 6, 22, 24, ink);
            DrawEllipse(outTex, shoulderX, shoulderY - 6, 18, 20, shirtCoral);
            DrawEllipse(outTex, shoulderX, shoulderY - 2, 17, 7, shirtCream);
            DrawEllipse(outTex, shoulderX, shoulderY - 14, 15, 5, shirtCream);

            outTex.Apply();
            float ppu = 1024f / 3.3f;
            return Sprite.Create(
                outTex,
                new Rect(0, 0, outW, outH),
                new Vector2(hipCenterX / (float)outW, (ankleY - 16f) / outH),
                ppu);
        }

        private static void DrawStandingLeg(
            Texture2D tex, int hipX, int hipY, int footX, int ankleY,
            Color ink, Color jeansMain, Color jeansHigh, Color shoePink, Color shoeSole)
        {
            int kneeX = (hipX + footX) / 2 + 5;
            int kneeY = (hipY + ankleY) / 2 + 4;
            int steps = 28;

            // Passe 1: contorno ink contínuo da coxa + canela (zero anéis internos!)
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(hipX, kneeX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(hipY, kneeY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(26f, 21f, t));
                DrawCircle(tex, lx, ly, rad + 4, ink);
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(kneeX, footX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(kneeY, ankleY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(21f, 16f, t));
                DrawCircle(tex, lx, ly, rad + 4, ink);
            }

            // Passe 2: preenchimento jeans contínuo e liso
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(hipX, kneeX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(hipY, kneeY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(26f, 21f, t));
                DrawCircle(tex, lx, ly, rad, jeansMain);
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(kneeX, footX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(kneeY, ankleY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(21f, 16f, t));
                DrawCircle(tex, lx, ly, rad, jeansMain);
            }

            // Passe 3: brilho suave na frente da calça jeans
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(hipX, kneeX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(hipY, kneeY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(14f, 10f, t));
                DrawCircle(tex, lx + 5, ly, rad, jeansHigh);
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(kneeX, footX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(kneeY, ankleY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(10f, 7f, t));
                DrawCircle(tex, lx + 4, ly, rad, jeansHigh);
            }

            // Barra dobrada da calça jeans
            DrawEllipse(tex, footX, ankleY + 12, 20, 8, ink);
            DrawEllipse(tex, footX, ankleY + 12, 16, 5, jeansHigh);

            // Meia branca e tênis rosa/branco apoiado horizontalmente no chão
            DrawEllipse(tex, footX + 10, ankleY - 3, 29, 16, ink);
            DrawEllipse(tex, footX + 10, ankleY - 2, 25, 13, shoePink);
            DrawEllipse(tex, footX + 10, ankleY - 12, 27, 6, ink);
            DrawEllipse(tex, footX + 10, ankleY - 11, 24, 4, shoeSole);
            DrawCircle(tex, footX + 14, ankleY + 3, 4, Color.white);
        }

        /// <summary>
        /// Extrai o PRÓPRIO cachorrinho dourado de girl_bike.png (1024x1024) e gera os 8 estados animados
        /// em estilo Cozy Storybook 2D com olhos vivos e expressivos, boca aberta em um sorriso alegre,
        /// linguinha rosada para fora, orelhas com movimento suave e rabinho abanando!
        /// </summary>
        public static Sprite ExtractRealRunningPuppySprite()
        {
            if (cachedRealRunningPuppySprite != null) return cachedRealRunningPuppySprite;
            Sprite[] happyFrames = GetPuppyStateFrames(PuppyAnimState.Feliz);
            cachedRealRunningPuppySprite = (happyFrames != null && happyFrames.Length > 0) ? happyFrames[0] : null;
            return cachedRealRunningPuppySprite;
        }

        public static Sprite[] GetPuppyStateFrames(PuppyAnimState state)
        {
            int key = (int)state;
            if (cachedPuppyStateFrames.TryGetValue(key, out Sprite[] frames) && frames != null && frames.Length > 0 && frames[0] != null)
                return frames;

            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            bool inBasket = (state == PuppyAnimState.NaCestinha);
            frames = new Sprite[2]
            {
                BuildExpressivePuppyFrame(src, state, 0, inBasket),
                BuildExpressivePuppyFrame(src, state, 1, inBasket)
            };
            cachedPuppyStateFrames[key] = frames;
            return frames;
        }

        /// <summary>
        /// Gera quadros animados do cachorrinho NA CESTINHA para qualquer expressão (Feliz, Curioso,
        /// Farejando, RecebendoCarinho, Descansando), alinhados ao pixel exato da cestinha de girl_bike.png!
        /// </summary>
        public static Sprite[] GetBasketPuppyStateFrames(PuppyAnimState state)
        {
            int key = (int)state;
            if (cachedBasketPuppyStateFrames.TryGetValue(key, out Sprite[] frames) && frames != null && frames.Length > 0 && frames[0] != null)
                return frames;

            Texture2D src = GetReadableGirlBikeTexture();
            if (src == null) return null;

            frames = new Sprite[2]
            {
                BuildExpressivePuppyFrame(src, state, 0, true),
                BuildExpressivePuppyFrame(src, state, 1, true)
            };
            cachedBasketPuppyStateFrames[key] = frames;
            return frames;
        }

        private static Sprite BuildExpressivePuppyFrame(Texture2D src, PuppyAnimState state, int phase, bool isInBasket)
        {
            int outW = 240, outH = 225;
            Texture2D outTex = NewTex(outW, outH);

            // Paleta original exata do cachorrinho dourado de girl_bike.png
            Color ink = new Color(0.14f, 0.07f, 0.05f, 1f);
            Color furDark = new Color(0.776f, 0.545f, 0.325f, 1f);
            Color furMid = new Color(0.906f, 0.706f, 0.482f, 1f);
            Color furHigh = new Color(0.955f, 0.805f, 0.565f, 1f);
            Color collarRed = new Color(0.92f, 0.32f, 0.34f, 1f);
            Color medalGold = new Color(1f, 0.86f, 0.24f, 1f);
            Color mouthDark = new Color(0.38f, 0.12f, 0.14f, 1f);
            Color tonguePink = new Color(1.00f, 0.46f, 0.58f, 1f);
            Color tongueLight = new Color(1.00f, 0.72f, 0.80f, 1f);
            Color blushPink = new Color(0.98f, 0.55f, 0.58f, 0.82f);

            bool isResting = (state == PuppyAnimState.Descansando);
            bool isSniffing = (state == PuppyAnimState.Farejando);
            bool isRunning = (state == PuppyAnimState.Correndo);
            bool isPetted = (state == PuppyAnimState.RecebendoCarinho);
            bool isCurious = (state == PuppyAnimState.Curioso);
            bool isHappy = (state == PuppyAnimState.Feliz || state == PuppyAnimState.NaCestinha);

            // Quando fora da cestinha (a pé), desenhamos corpinho, rabinho abanando, patinhas animadas e coleirinha
            if (!isInBasket)
            {
                // 1. Rabinho dourado abanando com movimento visível entre phase 0 e phase 1
                int tailTipY = isResting ? 44 : (isPetted || isHappy || isRunning ? (phase == 0 ? 94 : 74) : (phase == 0 ? 84 : 76));
                int tailTipX = isResting ? 44 : (phase == 0 ? 44 : 56);
                DrawEllipse(outTex, tailTipX + 12, tailTipY - 6, 25, 12, ink);
                DrawEllipse(outTex, tailTipX + 12, tailTipY - 6, 21, 8, furMid);
                DrawEllipse(outTex, tailTipX + 10, tailTipY - 4, 15, 5, furHigh);

                // 2. Patinhas animadas conforme o estado (Correndo, Parado/Feliz, Farejando, Descansando)
                int bodyY = isResting ? 38 : (isSniffing ? 50 : (phase == 1 && isRunning ? 62 : 56));
                int[] pawX = isRunning
                    ? (phase == 0 ? new[] { 62, 92, 136, 166 } : new[] { 86, 108, 120, 144 })
                    : (isResting ? new[] { 72, 96, 136, 158 } : new[] { 76, 100, 128, 150 });
                int pawBaseY = isResting ? 19 : 28;

                for (int i = 0; i < pawX.Length; i++)
                {
                    int px = pawX[i];
                    int py = pawBaseY + ((isRunning && ((i + phase) % 2 == 0)) ? 7 : 0);
                    DrawEllipse(outTex, px, py, 14, isResting ? 9 : 17, ink);
                    DrawEllipse(outTex, px, py + 1, 10, isResting ? 6 : 13, (i % 2 == 0) ? furDark : furMid);
                    DrawEllipse(outTex, px + 3, py - (isResting ? 4 : 10), 13, 8, ink);
                    DrawEllipse(outTex, px + 3, py - (isResting ? 3 : 9), 9, 5, furHigh);
                }

                // 3. Tronco e barriguinha macia do cachorrinho
                DrawEllipse(outTex, 114, bodyY, 48, isResting ? 22 : 29, ink);
                DrawEllipse(outTex, 114, bodyY, 43, isResting ? 18 : 24, furMid);
                DrawEllipse(outTex, 118, bodyY - 4, 30, 15, furHigh);

                // 4. Coleirinha vermelha charmosa com medalhinha dourada
                int collarY = isSniffing ? bodyY + 6 : bodyY + 16;
                DrawEllipse(outTex, 132, collarY, 27, 9, ink);
                DrawEllipse(outTex, 132, collarY, 23, 6, collarRed);
                DrawCircle(outTex, 146, collarY - 6, 7, ink);
                DrawCircle(outTex, 146, collarY - 6, 5, medalGold);
            }

            // 5. Copia a cabeça original do cachorrinho de girl_bike.png (x=675..830, y=504..638)
            // Quando fora da cestinha (!isInBasket), recorta apenas a cabeça/pescoço (sy >= 532)
            // removendo as patinhas apoiadas na cesta e a borda de vime para um corpo 100% limpo!
            int srcX0 = 675, srcX1 = 830;
            int srcY0 = 504, srcY1 = 638;
            int copyMinY = isInBasket ? 504 : 532;
            int destOffsetX = isInBasket ? 40 : (isSniffing ? 50 : (isCurious && phase == 1 ? 42 : 40));
            int destOffsetY = isInBasket
                ? (isResting ? 24 : (phase == 1 ? 30 : 28))
                : (isResting ? 40 : (isSniffing ? 36 : (phase == 1 ? 66 : 62)));
            int earLift = (isCurious || isRunning || isPetted) ? (phase == 0 ? 5 : 1) : (isResting ? -2 : (phase == 0 ? 3 : 0));

            for (int sy = copyMinY; sy <= srcY1; sy++)
            {
                for (int sx = srcX0; sx <= srcX1; sx++)
                {
                    // Remove pontinha do guidão/rabo da cesta no canto inferior esquerdo
                    if (sx < 698 && sy < 544) continue;
                    // Quando a pé, remove a patinha direita apoiada na borda da cesta (sx > 792 && sy < 546)
                    if (!isInBasket && sx > 790 && sy < 548) continue;

                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.12f) continue;

                    bool isLeftEar = (sx <= 722 && sy >= 542 && sy <= 616);
                    int dx = (sx - srcX0) + destOffsetX;
                    int dy = (sy - srcY0) + destOffsetY + (isLeftEar ? earLift : 0);
                    if (dx >= 0 && dx < outW && dy >= 0 && dy < outH)
                    {
                        outTex.SetPixel(dx, dy, c);
                    }
                }
            }

            // Coordenadas faciais EXATAS medidas em girl_bike.png (1024x1024):
            // Olho esquerdo: (752, 580) | Olho direito: (793, 586)
            // Focinho: (783, 569)       | Boca + linguinha: (781, 547)
            int leftEyeX = destOffsetX + (752 - srcX0);   // +77
            int leftEyeY = destOffsetY + (580 - srcY0);   // +76
            int rightEyeX = destOffsetX + (793 - srcX0);  // +118
            int rightEyeY = destOffsetY + (586 - srcY0);  // +82
            int mouthX = destOffsetX + (781 - srcX0);     // +106
            int mouthY = destOffsetY + (547 - srcY0);     // +43

            // 6. Olhos expressivos no local exato dos olhinhos originais (ZERO olhos duplicados!)
            if (isPetted || isResting)
            {
                // Cobre exatamente os olhos originais com o pelo dourado claro antes de desenhar o arco feliz/sereno
                DrawEllipse(outTex, leftEyeX, leftEyeY, 9, 10, furHigh);
                DrawEllipse(outTex, rightEyeX, rightEyeY, 9, 10, furHigh);

                // Olhinhos semicerrados em arco contente (^ ^) ou tranquilos descansando
                for (int ex = -7; ex <= 7; ex++)
                {
                    int archY = isResting ? -Mathf.Abs(ex) / 2 : (4 - Mathf.Abs(ex) / 2);
                    DrawCircle(outTex, leftEyeX + ex, leftEyeY + archY, 2, ink);
                    DrawCircle(outTex, rightEyeX + ex, rightEyeY + archY, 2, ink);
                }
            }
            else
            {
                // Mantém os olhos originais nítidos e reforça o brilho vivo de alegria com duplo ponto de luz
                DrawCircle(outTex, leftEyeX - 2, leftEyeY + 3, 2, Color.white);
                DrawCircle(outTex, rightEyeX - 2, rightEyeY + 3, 2, Color.white);
                if (phase == 0 && (isHappy || isRunning || isCurious))
                {
                    outTex.SetPixel(leftEyeX + 2, leftEyeY - 2, Color.white);
                    outTex.SetPixel(rightEyeX + 2, rightEyeY - 2, Color.white);
                }
            }

            // Bochechinhas rosadas alegres sob os olhos (no ponto exato!)
            DrawEllipse(outTex, leftEyeX - 8, leftEyeY - 14, 6, 4, blushPink);
            DrawEllipse(outTex, rightEyeX + 5, rightEyeY - 14, 5, 3, blushPink);

            // 7. Sorriso alegre + linguinha rosada bem visível e animada no local EXATO da boca (781, 547)!
            if (!isResting)
            {
                // Abertura alegre da boca
                DrawEllipse(outTex, mouthX, mouthY + 2, 11, 7, ink);
                DrawEllipse(outTex, mouthX, mouthY + 2, 8, 5, mouthDark);

                // Linguinha rosada vibrante para fora (balançando suavemente entre phase 0 e phase 1!)
                int tongueOffsetX = (phase == 0) ? 1 : 3;
                int tongueOffsetY = (phase == 0) ? -4 : -7;
                DrawEllipse(outTex, mouthX + tongueOffsetX, mouthY + tongueOffsetY, 8, 9, ink);
                DrawEllipse(outTex, mouthX + tongueOffsetX, mouthY + tongueOffsetY, 6, 7, tonguePink);
                DrawEllipse(outTex, mouthX + tongueOffsetX - 1, mouthY + tongueOffsetY + 2, 3, 4, tongueLight);
            }
            else
            {
                // Sorriso calmo e tranquilo descansando (cobre a boca aberta original e desenha sorriso sereno)
                DrawEllipse(outTex, mouthX, mouthY, 13, 10, furHigh);
                for (int mx = -8; mx <= 8; mx++)
                {
                    int sy = -Mathf.Abs(mx) / 3;
                    DrawCircle(outTex, mouthX + mx, mouthY + 2 + sy, 2, ink);
                }
                DrawEllipse(outTex, mouthX + 3, mouthY - 2, 4, 4, ink);
                DrawEllipse(outTex, mouthX + 3, mouthY - 2, 2, 3, tonguePink);
            }

            outTex.Apply();
            // Na cestinha: PPU idêntico ao de girl_bike.png (1024 / 3.3 = 310.303) para encaixe perfeito!
            // A pé: 265 PPU (~17% maior) para o rosto, sorriso e linguinha rosada brilharem mesmo em telas pequenas!
            float ppu = isInBasket ? (1024f / 3.3f) : 265f;
            float pivotY = isInBasket ? (28f / outH) : (12f / outH);
            return Sprite.Create(
                outTex,
                new Rect(0, 0, outW, outH),
                new Vector2(0.52f, pivotY),
                ppu);
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
            cachedRealParkedBikeSprite = Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0.5f, 83f / 1024f), ppu);
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
