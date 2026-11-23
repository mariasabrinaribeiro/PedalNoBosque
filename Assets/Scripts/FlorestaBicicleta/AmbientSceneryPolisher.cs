using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Sistema de Composição em 4 Planos com Paralaxe Independente e Vida Ambiental (PEDAL NO BOSQUE):
    /// - Plano 1 (Fundo Distante): nuvens macias em paralaxe lenta (combinado com as montanhas de fundo a 0.25x);
    /// - Plano 2 (Fundo Intermediário — 0.48x): bosques de pinheiros, árvores frondosas, colinas e cachoeiras distantes;
    /// - Plano 3 (Plano de Interação — 1.00x): estrada, ponte integrada, animais, moradores e a menina;
    /// - Plano 4 (Primeiro Plano — 1.28x): samambaias, cogumelos, flores grandes e pedras/raízes passando perto da câmera;
    /// - Vida e Movimento: pássaros voando no céu, borboletas coloridas e pétalas/folhas ao vento!
    /// </summary>
    public class AmbientSceneryPolisher : MonoBehaviour
    {
        private class ParallaxProp
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float screenBaseX; // Posição base em espaço de tela (uniformemente espaçada dentro de screenSpan)
            public float y;
            public float parallax;    // Deslocamento aparente relativo à câmera (0.06 céu .. 0.52 árvores .. 1.18 primeiro plano)
            public float screenSpan;  // Largura total da janela de reciclagem em espaço de tela (sem lacunas e sem acúmulo!)
            public int variantIndex;
            public bool isTreeLayer;
            public int lastCycle;
        }

        private class CloudItem
        {
            public Transform tr;
            public float baseScreenX;
            public float windOffset;
            public float speed;
            public float y;
        }

        private class BirdItem
        {
            public Transform tr;
            public float speed;
            public float baseY;
            public float phase;
            public float baseScale;
        }

        private class ButterflyItem
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float offsetX;
            public float baseY;
            public float phase;
            public float speed;
        }

        private class MoteItem
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 velocity;
            public float phase;
            public float baseScale;
        }

        private Camera cam;
        private readonly List<CloudItem> clouds = new List<CloudItem>();
        private readonly List<ParallaxProp> distantForestProps = new List<ParallaxProp>();
        private readonly List<ParallaxProp> midgroundProps = new List<ParallaxProp>();
        private readonly List<ParallaxProp> nearTreeProps = new List<ParallaxProp>();
        private readonly List<ParallaxProp> foregroundProps = new List<ParallaxProp>();
        private readonly List<BirdItem> birds = new List<BirdItem>();
        private readonly List<ButterflyItem> butterflies = new List<ButterflyItem>();
        private readonly List<MoteItem> motes = new List<MoteItem>();

        private Sprite cloudSprite;
        private Sprite birdSprite;
        private Sprite butterflySprite;
        private Sprite moteSprite;
        private Sprite petalSprite;

        private BiomeType activeBiome = BiomeType.CampoFlorido;
        private Color currentMoteColor = new Color(1f, 0.96f, 0.72f, 0.72f);

        public void Initialize(Camera targetCamera)
        {
            cam = targetCamera;
            cloudSprite = CreateCloudSprite();
            birdSprite = CreateBirdSprite();
            butterflySprite = CreateButterflySprite();
            moteSprite = CreateGlowMoteSprite();
            petalSprite = CreatePetalSprite();

            BuildSkyClouds();
            BuildDistantForestLayer();
            BuildMidgroundLayer();
            BuildNearTreesLayer();
            BuildForegroundLayer();
            BuildFlyingBirds();
            BuildButterflies();
            BuildAtmosphericMotes();
        }

        public void SetBiome(BiomeType biome)
        {
            activeBiome = biome;
        }

        private void BuildSkyClouds()
        {
            const float cloudSpan = 42f;
            for (int i = 0; i < 6; i++)
            {
                GameObject cObj = new GameObject($"NuvemDistante_{i}");
                cObj.transform.SetParent(transform, false);
                float startX = -cloudSpan * 0.5f + i * (cloudSpan / 6f) + Random.Range(-1.0f, 1.0f);
                float y = Random.Range(3.4f, 5.1f);
                cObj.transform.position = new Vector3(startX, y, 2f);
                float sc = Random.Range(0.85f, 1.45f);
                cObj.transform.localScale = new Vector3(sc, sc * 0.88f, 1f);

                SpriteRenderer sr = cObj.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprite;
                sr.color = new Color(1f, 0.99f, 0.97f, Random.Range(0.65f, 0.85f));
                sr.sortingOrder = WorldDepth.SkyClouds;

                clouds.Add(new CloudItem
                {
                    tr = cObj.transform,
                    baseScreenX = startX,
                    windOffset = 0f,
                    speed = Random.Range(0.08f, 0.16f),
                    y = y
                });
            }
        }

        /// <summary>Plano 1.5 — Floresta de Fundo (paralaxe 0.30x): silhuetas de bosques distantes com névoa atmosférica à frente das montanhas (12%).</summary>
        private void BuildDistantForestLayer()
        {
            const int count = 8;
            const float screenSpacing = 7.8f;
            float totalScreenSpan = count * screenSpacing; // 62.4f
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"FlorestaDistante_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BiomePropsFactory.CreateMidgroundTreeClusterSprite(i);
                sr.flipX = (i % 2 == 1);
                sr.sortingOrder = WorldDepth.DistantForest; // Entre montanhas distantes (-20) e árvores intermediárias (-10)
                // Névoa atmosférica azul-esverdeada suave para profundidade natural sem competir com as montanhas
                sr.color = new Color(0.58f, 0.76f, 0.74f, 0.56f);
                float y = -2.15f + (i % 3) * 0.22f;
                float scale = 0.28f + (i % 3) * 0.04f;
                go.transform.localScale = new Vector3(scale * 1.15f, scale, 1f);
                distantForestProps.Add(new ParallaxProp
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -totalScreenSpan * 0.5f + i * screenSpacing,
                    y = y,
                    parallax = WorldDepth.ParallaxDistantForest,
                    screenSpan = totalScreenSpan,
                    variantIndex = i,
                    isTreeLayer = false,
                    lastCycle = 0
                });
            }
        }

        /// <summary>Plano 2 — Árvores Intermediárias (paralaxe 0.55x): bosques e árvores variadas atrás da estrada.</summary>
        private void BuildMidgroundLayer()
        {
            const int count = 7;
            const float screenSpacing = 9.0f;
            float totalScreenSpan = count * screenSpacing; // 63.0f
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"FundoIntermediario_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 2 == 1)
                    ? BiomePropsFactory.CreateMidgroundTreeClusterSprite(i)
                    : BiomePropsFactory.CreateOrganicTreeSprite(i + 1);
                sr.sortingOrder = WorldDepth.MidgroundTrees;
                sr.color = new Color(0.78f, 0.87f, 0.83f, 0.76f);
                float y = -2.42f + 0.18f * (i % 3);
                float scale = (i % 2 == 1) ? 0.38f + (i % 3) * 0.05f : 0.34f + (i % 4) * 0.055f;
                go.transform.localScale = new Vector3(scale, scale * (0.94f + (i % 2) * 0.08f), 1f);
                midgroundProps.Add(new ParallaxProp
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -totalScreenSpan * 0.5f + i * screenSpacing,
                    y = y,
                    parallax = WorldDepth.ParallaxMidgroundTrees,
                    screenSpan = totalScreenSpan,
                    variantIndex = i + 1,
                    isTreeLayer = true,
                    lastCycle = 0
                });
            }
        }

        /// <summary>Plano 2.5 — Árvores Próximas (paralaxe 0.85x): carvalhos ilustrados ancorados na grama atrás do meio-fio da estrada.</summary>
        private void BuildNearTreesLayer()
        {
            float[] screenOffsets = { -27f, -15f, -2f, 11f, 23f };
            const float totalScreenSpan = 60f;

            for (int i = 0; i < screenOffsets.Length; i++)
            {
                GameObject go = new GameObject($"ArvoreOrganica_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BiomePropsFactory.LoadOakTreeSprite();
                sr.flipX = (i % 2 == 1);
                sr.sortingOrder = WorldDepth.NearParallaxTrees;
                sr.color = (i % 2 == 0) ? Color.white : new Color(0.96f, 0.99f, 0.95f, 1f);

                float sc = 0.64f + (i % 3) * 0.07f;
                go.transform.localScale = new Vector3(sc, sc * (0.95f + (i % 2) * 0.06f), 1f);

                // Sombra de contato suave na grama atrás do meio-fio (nunca invade a estrada)
                BiomePropsFactory.AddContactShadow(go.transform, new Vector3(0f, 0.02f, 0f), new Vector2(1.85f, 0.32f), WorldDepth.NearParallaxShadow);

                GentleSway sway = go.AddComponent<GentleSway>();
                sway.maxAngleDeg = 1.2f;
                sway.speed = Random.Range(0.85f, 1.25f);

                nearTreeProps.Add(new ParallaxProp
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = screenOffsets[i],
                    y = -2.30f + (i % 2) * 0.06f,
                    parallax = WorldDepth.ParallaxNearTrees,
                    screenSpan = totalScreenSpan,
                    variantIndex = i,
                    isTreeLayer = true,
                    lastCycle = 0
                });
            }
        }

        /// <summary>Plano 4 — Vegetação de Primeiro Plano (paralaxe 1.18x): grama florida e samambaias na borda inferior mais próxima da câmera.</summary>
        private void BuildForegroundLayer()
        {
            // Vegetação decorativa rente à borda inferior da lente (Y = -4.34f, abaixo do meio-fio inferior da estrada),
            // deslocando-se a 118% da câmera: sensação nítida e natural de velocidade sem escorregar sobre o terreno!
            const int count = 12;
            const float screenSpacing = 5.0f;
            float totalScreenSpan = count * screenSpacing; // 60.0f

            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"PrimeiroPlano_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = WorldDepth.ForegroundBase;

                if (i % 3 == 0)
                    sr.sprite = BiomePropsFactory.CreateForegroundFernSprite();
                else
                    sr.sprite = BiomePropsFactory.CreateFlowerPatchSprite(i);

                float sc = (i % 3 == 0) ? (0.48f + (i % 2) * 0.08f) : (0.90f + (i % 3) * 0.09f);
                go.transform.localScale = new Vector3(sc, sc, 1f);
                sr.color = new Color(0.94f, 0.98f, 0.90f, 0.96f);

                GentleSway sway = go.AddComponent<GentleSway>();
                sway.maxAngleDeg = 2.4f;
                sway.speed = 1.25f + (i % 3) * 0.22f;

                foregroundProps.Add(new ParallaxProp
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -totalScreenSpan * 0.5f + i * screenSpacing,
                    y = WorldDepth.ForegroundEdgeY + (i % 2) * 0.04f,
                    parallax = WorldDepth.ParallaxForeground,
                    screenSpan = totalScreenSpan,
                    variantIndex = i,
                    isTreeLayer = false,
                    lastCycle = 0
                });
            }
        }

        private void BuildFlyingBirds()
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject bObj = new GameObject($"PassaroCeu_{i}");
                bObj.transform.SetParent(transform, false);
                float y = Random.Range(2.2f, 4.5f);
                bObj.transform.position = new Vector3(-11f + i * 4.5f, y, 0f);
                float sc = Random.Range(0.52f, 0.88f);
                bObj.transform.localScale = new Vector3(sc, sc, 1f);

                SpriteRenderer sr = bObj.AddComponent<SpriteRenderer>();
                sr.sprite = birdSprite;
                sr.color = new Color(0.24f, 0.30f, 0.38f, 0.80f);
                sr.sortingOrder = -12;

                birds.Add(new BirdItem
                {
                    tr = bObj.transform,
                    speed = Random.Range(1.2f, 2.1f),
                    baseY = y,
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseScale = sc
                });
            }
        }

        private void BuildButterflies()
        {
            Color[] colors =
            {
                new Color(1f, 0.76f, 0.24f, 0.95f),
                new Color(1f, 0.46f, 0.76f, 0.95f),
                new Color(0.46f, 0.84f, 1f, 0.95f),
                new Color(0.98f, 0.56f, 0.28f, 0.95f)
            };

            for (int i = 0; i < 6; i++)
            {
                GameObject bfObj = new GameObject($"Borboleta_{i}");
                bfObj.transform.SetParent(transform, false);
                float sc = 0.48f + (i % 3) * 0.07f;
                bfObj.transform.localScale = new Vector3(sc, sc, 1f);

                SpriteRenderer sr = bfObj.AddComponent<SpriteRenderer>();
                sr.sprite = butterflySprite;
                sr.color = colors[i % colors.Length];
                sr.sortingOrder = 6;

                butterflies.Add(new ButterflyItem
                {
                    tr = bfObj.transform,
                    sr = sr,
                    offsetX = -8f + i * 4.6f,
                    baseY = Random.Range(-1.7f, -0.55f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    speed = Random.Range(0.6f, 1.15f)
                });
            }
        }

        private void BuildAtmosphericMotes()
        {
            // Reduzido para 8 partículas leves para manter a cena limpa e destacar a protagonista
            for (int i = 0; i < 8; i++)
            {
                bool isPetal = (i % 2 == 0);
                GameObject mObj = new GameObject($"Atmosfera_{i}");
                mObj.transform.SetParent(transform, false);
                // Confinadas ao céu/copa das árvores (Y de 0.8 a 4.6), sem poluir a faixa da bicicleta
                mObj.transform.position = new Vector3(Random.Range(-10f, 12f), Random.Range(0.8f, 4.6f), 0f);
                float sc = isPetal ? Random.Range(0.24f, 0.40f) : Random.Range(0.18f, 0.32f);
                mObj.transform.localScale = new Vector3(sc, sc, 1f);

                SpriteRenderer sr = mObj.AddComponent<SpriteRenderer>();
                sr.sprite = isPetal ? petalSprite : moteSprite;
                sr.sortingOrder = 4; // Atrás da protagonista (camada 10), garantindo nitidez total
                sr.color = currentMoteColor;

                motes.Add(new MoteItem
                {
                    tr = mObj.transform,
                    sr = sr,
                    velocity = new Vector2(Random.Range(-1.0f, -0.35f), Random.Range(-0.2f, 0.12f)),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseScale = sc
                });
            }
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float camX = cam.transform.position.x;

            Color targetTint = GetBiomeParticleColor(activeBiome);
            currentMoteColor = Color.Lerp(currentMoteColor, targetTint, 2.5f * Time.deltaTime);

            // Plano 1: Nuvens suaves
            // Céu / Nuvens distantes: paralaxe mínima de 6% (0.06x) + brisa suave contínua (nunca dispara na frente das montanhas!)
            const float cloudSpan = 42f;
            const float cloudHalf = cloudSpan * 0.5f;
            for (int i = 0; i < clouds.Count; i++)
            {
                CloudItem c = clouds[i];
                c.windOffset += c.speed * Time.deltaTime;
                float rawCloud = c.baseScreenX - camX * 0.06f - c.windOffset;
                float screenOffset = Mathf.Repeat(rawCloud + cloudHalf, cloudSpan) - cloudHalf;
                float y = c.y + Mathf.Sin(Time.time * 0.4f + i) * 0.12f;
                c.tr.position = new Vector3(camX + screenOffset, y, 2f);
            }

            // Planos de Parallax unificados em espaço de tela (Mathf.Repeat O(1): zero lacunas, zero deriva acumulada,
            // zero deslizamento com a bicicleta parada, resposta imediata ao pedalar para frente ou para trás!)
            UpdateParallaxList(distantForestProps, camX);
            UpdateParallaxList(midgroundProps, camX);
            UpdateParallaxList(nearTreeProps, camX);
            UpdateParallaxList(foregroundProps, camX);

            // Pássaros voando no céu com batida de asas suave
            for (int i = 0; i < birds.Count; i++)
            {
                BirdItem b = birds[i];
                Vector3 p = b.tr.position;
                p.x += b.speed * Time.deltaTime;
                if (p.x > camX + 18f) p.x = camX - 16f - Random.Range(0f, 4f);
                if (p.x < camX - 20f) p.x = camX + 15f;
                p.y = b.baseY + Mathf.Sin(Time.time * 2.2f + b.phase) * 0.28f;
                b.tr.position = p;

                float wingFlap = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 7.5f + b.phase));
                b.tr.localScale = new Vector3(b.baseScale, b.baseScale * wingFlap, 1f);
            }

            // Borboletas dançando perto das flores
            for (int i = 0; i < butterflies.Count; i++)
            {
                ButterflyItem bf = butterflies[i];
                bf.offsetX += Mathf.Sin(Time.time * 0.6f + bf.phase) * bf.speed * Time.deltaTime;
                float x = camX + Mathf.Repeat(bf.offsetX + 12f, 24f) - 12f;
                float y = bf.baseY + Mathf.Sin(Time.time * 3.8f + bf.phase) * 0.45f;
                bf.tr.position = new Vector3(x, y, 0f);
                float flap = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.time * 13f + bf.phase));
                bf.tr.localScale = new Vector3(0.65f * flap, 0.65f, 1f);
            }

            // Partículas atmosféricas (pétalas, folhas ao vento, brilhos)
            float windBoost = (activeBiome == BiomeType.Colinas) ? 2.1f : 1.0f;
            for (int i = 0; i < motes.Count; i++)
            {
                MoteItem m = motes[i];
                Vector3 p = m.tr.position;
                p.x += m.velocity.x * windBoost * Time.deltaTime;
                p.y += (m.velocity.y + Mathf.Sin(Time.time * 2.2f + m.phase) * 0.35f) * Time.deltaTime;

                if (p.x < camX - 13f) p.x = camX + 13f + Random.Range(0f, 3f);
                if (p.x > camX + 16f) p.x = camX - 12f;
                if (p.y < 0.6f) p.y = 4.6f;
                if (p.y > 5.2f) p.y = 0.9f;

                m.tr.position = p;
                m.tr.Rotate(0f, 0f, 42f * Time.deltaTime);

                float pulse = 0.85f + 0.25f * Mathf.Sin(Time.time * 3.0f + m.phase);
                m.tr.localScale = new Vector3(m.baseScale * pulse, m.baseScale * pulse, 1f);
                m.sr.color = currentMoteColor;
            }
        }

        private void UpdateParallaxList(List<ParallaxProp> list, float camX)
        {
            // Sistema único de paralaxe relativa à câmera (sem deriva acumulada e 100% reversível):
            // - Céu/Nuvens: 6% (0.06x)
            // - Montanhas e floresta distante (InfiniteTrackGenerator): 16% (0.16x)
            // - Árvores intermediárias: 52% (0.52x)
            // - Árvores próximas: 80% (0.80x)
            // - Estrada, pontes e objetos físicos (InfiniteTrackGenerator): 100% (1.00x fixos no mundo)
            // - Vegetação na borda inferior de primeiro plano: 118% (1.18x)
            for (int i = 0; i < list.Count; i++)
            {
                ParallaxProp item = list[i];
                float span = Mathf.Max(20f, item.screenSpan);
                float half = span * 0.5f;
                float raw = item.screenBaseX - camX * item.parallax;
                int cycle = Mathf.FloorToInt((raw + half) / span);
                float screenOffset = Mathf.Repeat(raw + half, span) - half;

                if (cycle != item.lastCycle)
                {
                    item.lastCycle = cycle;
                    if (item.isTreeLayer && item.sr != null)
                    {
                        item.sr.sprite = BiomePropsFactory.GetTreeForBiome(activeBiome, item.variantIndex);
                        item.sr.color = GetTreeColorForBiome(activeBiome, item.parallax);
                    }
                }

                item.tr.position = new Vector3(camX + screenOffset, item.y, 0f);
            }
        }

        private static Color GetTreeColorForBiome(BiomeType biome, float parallax)
        {
            bool isNear = parallax > 0.6f;
            if (biome == BiomeType.BosqueDaLua)
            {
                return isNear
                    ? new Color(0.85f, 0.90f, 1f, 1f)
                    : new Color(0.68f, 0.76f, 0.92f, 0.85f);
            }
            if (biome == BiomeType.Colinas)
            {
                return isNear
                    ? new Color(0.96f, 0.99f, 0.94f, 1f)
                    : new Color(0.82f, 0.90f, 0.85f, 0.85f);
            }
            return isNear
                ? Color.white
                : new Color(0.78f, 0.87f, 0.83f, 0.85f);
        }

        private static Color GetBiomeParticleColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.CampoFlorido:     return new Color(1f, 0.74f, 0.86f, 0.82f);
                case BiomeType.Colinas:          return new Color(0.88f, 0.98f, 0.62f, 0.80f);
                case BiomeType.RegiaoDoRio:      return new Color(0.68f, 0.94f, 1f, 0.78f);
                case BiomeType.TrilhaDaFloresta: return new Color(0.96f, 0.94f, 0.56f, 0.82f);
                case BiomeType.PequenaVila:      return new Color(1f, 0.88f, 0.52f, 0.82f);
                case BiomeType.BosqueDaLua:      return new Color(0.58f, 0.92f, 1f, 0.88f);
                default:                         return new Color(1f, 0.96f, 0.72f, 0.75f);
            }
        }

        // ─── Sprites Procedurais Suaves ──────────────────────────────

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

        private static void SoftCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / Mathf.Max(1f, r);
                    if (d <= 1f)
                    {
                        float a = col.a * Mathf.SmoothStep(1f, 0f, d * d);
                        Color existing = tex.GetPixel(x, y);
                        float outA = Mathf.Max(existing.a, a);
                        Color blended = Color.Lerp(existing, new Color(col.r, col.g, col.b, outA), a);
                        blended.a = outA;
                        tex.SetPixel(x, y, blended);
                    }
                }
            }
        }

        private static Sprite CreateCloudSprite()
        {
            int w = 160, h = 72;
            Texture2D tex = Blank(w, h);
            Color shade = new Color(0.88f, 0.94f, 1f, 0.82f);
            Color white = new Color(1f, 1f, 1f, 0.95f);

            SoftCircle(tex, 42, 26, 22, shade);
            SoftCircle(tex, 80, 28, 28, shade);
            SoftCircle(tex, 118, 25, 21, shade);
            SoftCircle(tex, 54, 38, 22, white);
            SoftCircle(tex, 86, 42, 26, white);
            SoftCircle(tex, 112, 34, 18, white);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 34f);
        }

        private static Sprite CreateBirdSprite()
        {
            int w = 48, h = 24;
            Texture2D tex = Blank(w, h);
            Color col = Color.white;
            for (int x = 4; x < 24; x++)
            {
                float t = (x - 4f) / 20f;
                int y = Mathf.RoundToInt(10f + Mathf.Sin(t * Mathf.PI) * 9f);
                for (int dy = -2; dy <= 1; dy++)
                    if (y + dy >= 0 && y + dy < h) tex.SetPixel(x, y + dy, col);
            }
            for (int x = 24; x < 44; x++)
            {
                float t = (x - 24f) / 20f;
                int y = Mathf.RoundToInt(10f + Mathf.Sin(t * Mathf.PI) * 9f);
                for (int dy = -2; dy <= 1; dy++)
                    if (y + dy >= 0 && y + dy < h) tex.SetPixel(x, y + dy, col);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        }

        private static Sprite CreateButterflySprite()
        {
            int s = 36;
            Texture2D tex = Blank(s, s);
            SoftCircle(tex, 11, 22, 8, Color.white);
            SoftCircle(tex, 25, 22, 8, Color.white);
            SoftCircle(tex, 13, 13, 6, new Color(1f, 0.92f, 0.84f, 0.95f));
            SoftCircle(tex, 23, 13, 6, new Color(1f, 0.92f, 0.84f, 0.95f));
            for (int y = 10; y <= 26; y++)
            {
                tex.SetPixel(17, y, new Color(0.20f, 0.14f, 0.12f, 1f));
                tex.SetPixel(18, y, new Color(0.20f, 0.14f, 0.12f, 1f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 44f);
        }

        private static Sprite CreateGlowMoteSprite()
        {
            int s = 32;
            Texture2D tex = Blank(s, s);
            SoftCircle(tex, 16, 16, 14, new Color(1f, 1f, 1f, 0.85f));
            SoftCircle(tex, 16, 16, 6, new Color(1f, 1f, 1f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 42f);
        }

        private static Sprite CreatePetalSprite()
        {
            int s = 32;
            Texture2D tex = Blank(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float nx = (x - 16f) / 10f;
                    float ny = (y - 16f) / 6f;
                    if (nx * nx + ny * ny <= 1f)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.92f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
