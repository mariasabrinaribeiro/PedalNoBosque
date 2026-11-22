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
        private static Sprite cachedRealWalkingGirlSprite;
        private static Sprite[] cachedWalkingGirlFrames;
        private static Sprite cachedRealRunningPuppySprite;
        private static readonly System.Collections.Generic.Dictionary<int, Sprite[]> cachedPuppyStateFrames =
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
        /// Carrega bg_floresta.png preservando 100% da pintura original para a camada de montanhas e floresta distante.
        /// Quando combinada com espelhamento alternado (flipX = i % 2 != 0), as bordas esquerda/direita casam
        /// pixel a pixel sem nenhuma linha ou emenda vertical!
        /// </summary>
        public static Sprite LoadForestBackgroundSprite(float cameraWorldHeight)
        {
            if (cachedSeamlessBgSprite != null) return cachedSeamlessBgSprite;
            cachedSeamlessBgSprite = LoadResourceSprite("Floresta/bg_floresta", cameraWorldHeight, 0.5f);
            return cachedSeamlessBgSprite;
        }

        /// <summary>
        /// Extrai a faixa inferior da estrada e suas margens floridas de bg_floresta.png (linhas 0..310 de 1024)
        /// com transição de alfa suave no topo da grama superior, para que a ESTRADA se mova a 100% (1.0x)
        /// presa ao mundo físico da bicicleta, enquanto as montanhas e a floresta ao fundo se movem em parallax!
        /// </summary>
        public static Sprite LoadForestRoadStripSprite(float cameraWorldHeight)
        {
            if (cachedSeamlessRoadStripSprite != null) return cachedSeamlessRoadStripSprite;
            Texture2D src = GetReadableBgTexture();
            if (src == null) return null;

            int w = src.width;
            int stripH = Mathf.Clamp(Mathf.RoundToInt(src.height * 0.305f), 260, src.height);
            int featherRows = 28;
            Texture2D stripTex = new Texture2D(w, stripH, TextureFormat.RGBA32, false);
            stripTex.filterMode = FilterMode.Bilinear;
            stripTex.wrapMode = TextureWrapMode.Clamp;

            Color[] srcPixels = src.GetPixels(0, 0, w, stripH);
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

            // 1. Copia a cabeça, capacete azul, cabelo ao vento e torso de blusa listrada da menina (sy = 468..915)
            // removendo o antebraço esticado para o guidão (sx > 528 e sy < 610) para que ela não pareça segurar um guidão invisível!
            for (int sy = 468; sy <= cropY1; sy++)
            {
                for (int sx = cropX0; sx <= cropX1; sx++)
                {
                    if (sx > 528 && sy < 612) continue;
                    if (sx < 395 && sy < 495) continue;

                    Color c = src.GetPixel(sx, sy);
                    if (c.a < 0.08f) continue;
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
            // frame 1 = Caminhando (passo 1)
            // frame 2 = Caminhando (passo 2)
            int backFootOffset = (frameIndex == 1) ? -24 : (frameIndex == 2 ? 22 : -12);
            int frontFootOffset = (frameIndex == 1) ? 24 : (frameIndex == 2 ? -20 : 12);
            int armSwingX = (frameIndex == 1) ? -10 : (frameIndex == 2 ? 12 : 2);

            int hipCenterX = 456 - cropX0; // 256
            int hipY = 472 - cropY0;       // 332
            int ankleY = 186 - cropY0;     // 46

            // 2. Perna de trás (calça jeans sombreada + tênis rosa apoiado no chão)
            DrawStandingLeg(outTex, hipCenterX - 8, hipY, hipCenterX + backFootOffset, ankleY,
                ink, jeansDark, jeansMid, shoePink, shoeSole);

            // 3. Cintura / quadril da calça jeans unindo suavemente à blusa listrada
            DrawEllipse(outTex, hipCenterX, hipY + 4, 34, 24, ink);
            DrawEllipse(outTex, hipCenterX, hipY + 4, 30, 20, jeansMid);
            DrawEllipse(outTex, hipCenterX + 4, hipY + 6, 18, 12, jeansLight);

            // Barra inferior da blusa listrada sobre o cós da calça
            DrawEllipse(outTex, hipCenterX + 2, hipY + 22, 34, 12, ink);
            DrawEllipse(outTex, hipCenterX + 2, hipY + 22, 30, 9, shirtCoral);
            DrawEllipse(outTex, hipCenterX + 2, hipY + 25, 28, 5, shirtCream);

            // 4. Perna da frente (calça jeans iluminada + tênis rosa e branco original)
            DrawStandingLeg(outTex, hipCenterX + 6, hipY, hipCenterX + frontFootOffset, ankleY,
                ink, jeansMid, jeansLight, shoePink, shoeSole);

            // 5. Braço natural relaxado ao lado do corpo (balançando suavemente ao caminhar, nunca esticado como no guidão!)
            int shoulderX = 468 - cropX0;
            int shoulderY = 594 - cropY0;
            int handX = shoulderX + 12 + armSwingX;
            int handY = 464 - cropY0;

            // Manga listrada no ombro
            DrawEllipse(outTex, shoulderX, shoulderY - 8, 18, 22, ink);
            DrawEllipse(outTex, shoulderX, shoulderY - 8, 14, 18, shirtCoral);
            DrawEllipse(outTex, shoulderX, shoulderY - 5, 13, 7, shirtCream);

            // Antebraço e mãozinha
            int steps = 24;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int ax = Mathf.RoundToInt(Mathf.Lerp(shoulderX + 2, handX, t));
                int ay = Mathf.RoundToInt(Mathf.Lerp(shoulderY - 18, handY, t));
                DrawCircle(outTex, ax, ay, 11, ink);
                DrawCircle(outTex, ax, ay, 8, skin);
                DrawCircle(outTex, ax - 2, ay, 4, skinShadow);
            }
            DrawCircle(outTex, handX + 2, handY - 2, 11, ink);
            DrawCircle(outTex, handX + 2, handY - 2, 8, skin);

            outTex.Apply();
            float ppu = 1024f / 3.3f;
            return Sprite.Create(
                outTex,
                new Rect(0, 0, outW, outH),
                new Vector2(hipCenterX / (float)outW, 30f / outH),
                ppu);
        }

        private static void DrawStandingLeg(
            Texture2D tex, int hipX, int hipY, int footX, int ankleY,
            Color ink, Color jeansMain, Color jeansHigh, Color shoePink, Color shoeSole)
        {
            int kneeX = (hipX + footX) / 2 + 4;
            int kneeY = (hipY + ankleY) / 2 + 6;

            // Coxa (quadril -> joelho)
            int steps = 26;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(hipX, kneeX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(hipY, kneeY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(21f, 17f, t));
                DrawCircle(tex, lx, ly, rad + 3, ink);
                DrawCircle(tex, lx, ly, rad, jeansMain);
                DrawCircle(tex, lx + 3, ly, Mathf.Max(4, rad - 6), jeansHigh);
            }

            // Canela (joelho -> tornozelo)
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int lx = Mathf.RoundToInt(Mathf.Lerp(kneeX, footX, t));
                int ly = Mathf.RoundToInt(Mathf.Lerp(kneeY, ankleY, t));
                int rad = Mathf.RoundToInt(Mathf.Lerp(17f, 13f, t));
                DrawCircle(tex, lx, ly, rad + 3, ink);
                DrawCircle(tex, lx, ly, rad, jeansMain);
                DrawCircle(tex, lx + 2, ly, Mathf.Max(3, rad - 5), jeansHigh);
            }

            // Barra dobrada da calça jeans
            DrawEllipse(tex, footX, ankleY + 12, 17, 7, ink);
            DrawEllipse(tex, footX, ankleY + 12, 14, 5, jeansHigh);

            // Meia branca e tênis rosa/branco apoiado horizontalmente no chão (base em y = 30)
            DrawEllipse(tex, footX + 10, ankleY - 2, 26, 15, ink);
            DrawEllipse(tex, footX + 10, ankleY - 1, 22, 12, shoePink);
            DrawEllipse(tex, footX + 10, ankleY - 10, 24, 6, ink);
            DrawEllipse(tex, footX + 10, ankleY - 9, 21, 4, shoeSole);
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

            frames = new Sprite[2]
            {
                BuildExpressivePuppyFrame(src, state, 0),
                BuildExpressivePuppyFrame(src, state, 1)
            };
            cachedPuppyStateFrames[key] = frames;
            return frames;
        }

        private static Sprite BuildExpressivePuppyFrame(Texture2D src, PuppyAnimState state, int phase)
        {
            int outW = 240, outH = 225;
            Texture2D outTex = NewTex(outW, outH);

            // Paleta original exata do cachorrinho dourado de girl_bike.png
            Color ink = new Color(0.14f, 0.07f, 0.05f, 1f);
            Color furDark = new Color(0.776f, 0.545f, 0.325f, 1f);
            Color furMid = new Color(0.906f, 0.706f, 0.482f, 1f);
            Color furHigh = new Color(1.000f, 0.855f, 0.651f, 1f);
            Color collarRed = new Color(0.92f, 0.32f, 0.34f, 1f);
            Color medalGold = new Color(1f, 0.86f, 0.24f, 1f);
            Color mouthDark = new Color(0.38f, 0.12f, 0.14f, 1f);
            Color tonguePink = new Color(1.00f, 0.46f, 0.58f, 1f);
            Color tongueLight = new Color(1.00f, 0.68f, 0.76f, 1f);
            Color blushPink = new Color(0.98f, 0.55f, 0.58f, 0.85f);

            bool isResting = (state == PuppyAnimState.Descansando);
            bool isSniffing = (state == PuppyAnimState.Farejando);
            bool isRunning = (state == PuppyAnimState.Correndo);
            bool isPetted = (state == PuppyAnimState.RecebendoCarinho);
            bool isCurious = (state == PuppyAnimState.Curioso);
            bool isInBasket = (state == PuppyAnimState.NaCestinha);

            // No estado NaCestinha, desenhamos apenas a cabeça expressiva, orelhinhas e patinhas apoiadas na borda
            if (!isInBasket)
            {
                // 1. Rabinho dourado abanando com movimento suave entre phase 0 e phase 1
                int tailTipY = isResting ? 48 : (phase == 0 ? 88 : 74);
                int tailTipX = isResting ? 46 : (phase == 0 ? 48 : 54);
                DrawEllipse(outTex, tailTipX + 10, tailTipY - 6, 24, 12, ink);
                DrawEllipse(outTex, tailTipX + 10, tailTipY - 6, 20, 8, furMid);
                DrawEllipse(outTex, tailTipX + 8, tailTipY - 4, 14, 5, furHigh);

                // 2. Patinhas animadas conforme o estado (Correndo, Parado/Feliz, Farejando, Descansando)
                int bodyY = isResting ? 40 : (isSniffing ? 54 : (phase == 1 && isRunning ? 62 : 56));
                int[] pawX = isRunning
                    ? (phase == 0 ? new[] { 66, 94, 134, 162 } : new[] { 84, 106, 120, 144 })
                    : (isResting ? new[] { 74, 96, 134, 154 } : new[] { 76, 100, 128, 150 });
                int pawBaseY = isResting ? 20 : 28;

                for (int i = 0; i < pawX.Length; i++)
                {
                    int px = pawX[i];
                    int py = pawBaseY + ((isRunning && ((i + phase) % 2 == 0)) ? 6 : 0);
                    DrawEllipse(outTex, px, py, 14, isResting ? 10 : 17, ink);
                    DrawEllipse(outTex, px, py + 1, 10, isResting ? 7 : 13, (i % 2 == 0) ? furDark : furMid);
                    DrawEllipse(outTex, px + 3, py - (isResting ? 4 : 10), 13, 8, ink);
                    DrawEllipse(outTex, px + 3, py - (isResting ? 3 : 9), 9, 5, furHigh);
                }

                // 3. Tronco e barriguinha macia do cachorrinho
                DrawEllipse(outTex, 114, bodyY, 48, isResting ? 23 : 29, ink);
                DrawEllipse(outTex, 114, bodyY, 43, isResting ? 19 : 24, furMid);
                DrawEllipse(outTex, 118, bodyY - 4, 30, 15, furHigh);

                // 4. Coleirinha vermelha charmosa com medalhinha dourada
                int collarY = isSniffing ? bodyY + 8 : bodyY + 16;
                DrawEllipse(outTex, 132, collarY, 27, 9, ink);
                DrawEllipse(outTex, 132, collarY, 23, 6, collarRed);
                DrawCircle(outTex, 146, collarY - 6, 7, ink);
                DrawCircle(outTex, 146, collarY - 6, 5, medalGold);
            }

            // 5. Copia a cabeça original do cachorrinho de girl_bike.png (x=675..829, y=504..633)
            int srcX0 = 675, srcX1 = 829;
            int srcY0 = 504, srcY1 = 633;
            int destOffsetX = isSniffing ? 48 : (isCurious && phase == 1 ? 42 : 40);
            int destOffsetY = isInBasket ? 28 : (isResting ? 48 : (isSniffing ? 46 : (phase == 1 ? 72 : 68)));

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

            // Coordenadas faciais relativas à cabeça copiada (centro do rostinho do cachorrinho)
            int faceCenterX = destOffsetX + 86;
            int faceCenterY = destOffsetY + 66;

            // 6. Orelhinhas expressivas com movimento suave entre os quadros
            int earLift = (isCurious || isRunning) ? (phase == 0 ? 6 : 2) : (phase == 0 ? 3 : 0);
            DrawEllipse(outTex, faceCenterX - 36, faceCenterY + 6 + earLift, 16, 24, ink);
            DrawEllipse(outTex, faceCenterX - 36, faceCenterY + 6 + earLift, 12, 20, furDark);
            DrawEllipse(outTex, faceCenterX - 34, faceCenterY + 8 + earLift, 7, 13, furMid);

            // 7. Olhos vivos e expressivos (ou semicerrados de alegria ao receber carinho / descansar)
            int leftEyeX = faceCenterX - 10;
            int rightEyeX = faceCenterX + 18;
            int eyeY = faceCenterY + 8;

            // Limpa a região dos olhos com o pelo dourado claro antes de desenhar a expressão viva
            DrawEllipse(outTex, leftEyeX, eyeY, 8, 8, furHigh);
            DrawEllipse(outTex, rightEyeX, eyeY, 8, 8, furHigh);

            if (isPetted || isResting)
            {
                // Olhinhos semicerrados em arco feliz (^ ^)
                for (int ex = -6; ex <= 6; ex++)
                {
                    int archY = isResting ? -Mathf.Abs(ex) / 2 : (3 - Mathf.Abs(ex) / 2);
                    DrawCircle(outTex, leftEyeX + ex, eyeY + archY, 2, ink);
                    DrawCircle(outTex, rightEyeX + ex, eyeY + archY, 2, ink);
                }
            }
            else
            {
                // Olhos vivos, grandes e brilhantes com duplo ponto de luz
                DrawCircle(outTex, leftEyeX, eyeY, 6, ink);
                DrawCircle(outTex, leftEyeX - 2, eyeY + 2, 2, Color.white);
                outTex.SetPixel(leftEyeX + 2, eyeY - 1, Color.white);

                DrawCircle(outTex, rightEyeX, eyeY, 6, ink);
                DrawCircle(outTex, rightEyeX - 2, eyeY + 2, 2, Color.white);
                outTex.SetPixel(rightEyeX + 2, eyeY - 1, Color.white);
            }

            // Bochechas rosadas alegres e focinho escuro brilhante
            DrawEllipse(outTex, leftEyeX - 7, eyeY - 10, 6, 4, blushPink);
            DrawEllipse(outTex, rightEyeX + 6, eyeY - 10, 6, 4, blushPink);
            int noseX = faceCenterX + 6;
            int noseY = faceCenterY - 4;
            DrawEllipse(outTex, noseX, noseY, 7, 5, ink);
            DrawCircle(outTex, noseX - 2, noseY + 1, 2, Color.white);

            // 8. Boca aberta em um sorriso carismático + linguinha rosada para fora!
            int mouthX = faceCenterX + 5;
            int mouthY = faceCenterY - 15;
            if (!isResting)
            {
                // Sorriso aberto alegre
                DrawEllipse(outTex, mouthX, mouthY, 12, 8, ink);
                DrawEllipse(outTex, mouthX, mouthY + 1, 9, 6, mouthDark);

                // Linguinha rosada para fora (com leve movimento alegre entre phase 0 e phase 1)
                int tongueOffsetX = (phase == 0) ? 2 : 4;
                int tongueOffsetY = (phase == 0) ? -5 : -7;
                DrawEllipse(outTex, mouthX + tongueOffsetX, mouthY + tongueOffsetY, 8, 9, ink);
                DrawEllipse(outTex, mouthX + tongueOffsetX, mouthY + tongueOffsetY, 6, 7, tonguePink);
                DrawEllipse(outTex, mouthX + tongueOffsetX - 1, mouthY + tongueOffsetY + 1, 3, 4, tongueLight);
            }
            else
            {
                // Sorriso sereno descansando com pontinha da linguinha
                DrawEllipse(outTex, mouthX, mouthY + 2, 9, 4, ink);
                DrawEllipse(outTex, mouthX + 3, mouthY - 1, 5, 4, ink);
                DrawEllipse(outTex, mouthX + 3, mouthY - 1, 3, 3, tonguePink);
            }

            outTex.Apply();
            // Escala ligeiramente ampliada (265 PPU em vez de 310 PPU) para que o sorriso e a linguinha
            // rosada fiquem perfeitamente visíveis mesmo em telas pequenas, mantendo proporção natural com a menina!
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
