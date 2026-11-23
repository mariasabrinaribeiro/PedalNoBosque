using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlorestaBicicleta
{
    /// <summary>
    /// CENÁRIO-PILOTO CINEMATOGRÁFICO — VALE DAS FLORES (Cozy Storybook 2D):
    /// Eleva o acabamento artístico do trecho representativo do Vale das Flores (BiomeType.CampoFlorido)
    /// inspirado na arte oficial de abertura (abertura_bosque.png), 100% compatível com o Built-in 2D Pipeline,
    /// leve para mobile, não destrutivo e reversível em tempo real pela tecla V (Antes vs. Depois):
    ///
    /// - Camada 1-2 (Névoa de Montanha & Halo Solar, 6%..18%): névoa azulada/dourada separando montanhas e vale;
    /// - Camada 3-4 (Ipês Floridos Dourado e Roxo no Vale, 42%): árvores brasileiras floridas ao fundo com balanço suave;
    /// - Iluminação Cinematográfica (Raios Solares Volumétricos Diagonais + Sombras de Copa na Trilha):
    ///   integrados dinamicamente ao DayNightCycleController (Manhã, Tarde, Pôr do Sol, Noite e Madrugada);
    /// - Camada 6 (Moldura de Primeiro Plano, 122%): copa superior com galhos, cipós e folhas iluminadas nas bordas +
    ///   samambaias tropicais e flores silvestres na margem inferior sem obstruir a pista nem a HUD;
    /// - Preserva integralmente os outros 5 biomas (desvanece suavemente fora do Vale das Flores até aprovação!).
    /// </summary>
    public class ValeDasFloresCinematicPilot : MonoBehaviour
    {
        private class ParallaxSprite
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float screenBaseX;
            public float baseY;
            public float parallax;
            public float screenSpan;
            public Color baseColor;
            public float phase;
        }

        public bool PilotEnabled { get; private set; } = true;
        public System.Action<string> OnToggleFeedback;

        private Camera cam;
        private DayNightCycleController dayNight;
        private BiomeType activeBiome = BiomeType.CampoFlorido;

        private float pilotWeight = 1f; // 1 = ativo no Vale das Flores, 0 = desativado ou outro bioma

        private Transform sunGlowTr;
        private SpriteRenderer sunGlowSR;

        private readonly List<ParallaxSprite> valleyMist = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> floweringTrees = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> godRays = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> trailShadows = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> canopyFraming = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> lowerFerns = new List<ParallaxSprite>();
        private readonly List<ParallaxSprite> goldenMotes = new List<ParallaxSprite>();

        // Sprites cacheados uma única vez (zero alocação em runtime)
        private static Sprite cachedSunBloomSprite;
        private static Sprite cachedMistRibbonSprite;
        private static Sprite cachedYellowIpeSprite;
        private static Sprite cachedPurpleIpeSprite;
        private static Sprite cachedGodRaySprite;
        private static Sprite cachedTrailShadowSprite;
        private static Sprite cachedCanopyBoughSprite;
        private static Sprite cachedHangingVineSprite;
        private static Sprite cachedTropicalFernSprite;
        private static Sprite cachedSoftMoteSprite;

        public void Initialize(Camera targetCamera, DayNightCycleController dayNightController)
        {
            cam = targetCamera;
            dayNight = dayNightController;

            EnsureSprites();
            BuildSunBloom();
            BuildValleyMistLayer();
            BuildFloweringValleyTrees();
            BuildVolumetricGodRays();
            BuildTrailCanopyShadows();
            BuildUpperCanopyAndVines();
            BuildLowerTropicalFerns();
            BuildGoldenSunMotes();
        }

        public void SetBiome(BiomeType biome)
        {
            activeBiome = biome;
        }

        public void TogglePilot()
        {
            PilotEnabled = !PilotEnabled;
            OnToggleFeedback?.Invoke(PilotEnabled
                ? "Piloto Cinematográfico (Vale das Flores): ATIVADO — aperte V para comparar Antes/Depois."
                : "Piloto Cinematográfico: DESATIVADO (visual original) — aperte V para reativar.");
        }

        private void EnsureSprites()
        {
            if (cachedSunBloomSprite == null) cachedSunBloomSprite = CreateSunBloomSprite();
            if (cachedMistRibbonSprite == null) cachedMistRibbonSprite = CreateMistRibbonSprite();
            if (cachedYellowIpeSprite == null) cachedYellowIpeSprite = CreateFloweringIpeSprite(isYellow: true);
            if (cachedPurpleIpeSprite == null) cachedPurpleIpeSprite = CreateFloweringIpeSprite(isYellow: false);
            if (cachedGodRaySprite == null) cachedGodRaySprite = CreateGodRaySprite();
            if (cachedTrailShadowSprite == null) cachedTrailShadowSprite = CreateTrailShadowSprite();
            if (cachedCanopyBoughSprite == null) cachedCanopyBoughSprite = CreateCanopyBoughSprite();
            if (cachedHangingVineSprite == null) cachedHangingVineSprite = CreateHangingVineSprite();
            if (cachedTropicalFernSprite == null) cachedTropicalFernSprite = CreateTropicalFernSprite();
            if (cachedSoftMoteSprite == null) cachedSoftMoteSprite = CreateSoftMoteSprite();
        }

        // ─── Construção das Camadas do Piloto ────────────────────────

        private void BuildSunBloom()
        {
            GameObject go = new GameObject("Piloto_HaloSolar");
            go.transform.SetParent(transform, false);
            sunGlowTr = go.transform;
            sunGlowTr.localScale = new Vector3(3.6f, 3.6f, 1f);
            sunGlowSR = go.AddComponent<SpriteRenderer>();
            sunGlowSR.sprite = cachedSunBloomSprite;
            sunGlowSR.sortingOrder = -15;
            sunGlowSR.color = new Color(1f, 0.90f, 0.62f, 0.42f);
        }

        private void BuildValleyMistLayer()
        {
            const int count = 5;
            const float span = 54f;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Piloto_NevoaVale_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cachedMistRibbonSprite;
                sr.sortingOrder = -14; // Entre montanhas (-20) e floresta distante (-13)
                sr.flipX = (i % 2 == 1);
                float sc = 1.15f + (i % 3) * 0.18f;
                go.transform.localScale = new Vector3(sc * 1.45f, sc * 0.85f, 1f);

                valleyMist.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -span * 0.5f + i * (span / count),
                    baseY = 0.65f + (i % 2) * 0.42f,
                    parallax = 0.18f,
                    screenSpan = span,
                    baseColor = new Color(0.92f, 0.95f, 1f, 0.34f),
                    phase = i * 1.3f
                });
            }
        }

        private void BuildFloweringValleyTrees()
        {
            // Ipês-amarelos e Ipês-roxos (inspirados diretamente na arte de referência!)
            // distribuídos de forma orgânica no vale intermediário (42% de paralaxe)
            float[] offsets = { -28f, -15.5f, -3.5f, 10.5f, 22.5f };
            const float span = 64f;
            for (int i = 0; i < offsets.Length; i++)
            {
                bool isYellow = (i % 2 == 0);
                GameObject go = new GameObject(isYellow ? $"Piloto_IpeAmarelo_{i}" : $"Piloto_IpeRoxo_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isYellow ? cachedYellowIpeSprite : cachedPurpleIpeSprite;
                sr.flipX = (i % 3 == 1);
                sr.sortingOrder = -9; // Acima da floresta distante (-13) e árvores intermediárias (-10), atrás da estrada (-6)
                float sc = 0.52f + (i % 3) * 0.08f;
                go.transform.localScale = new Vector3(sc, sc, 1f);

                GentleSway sway = go.AddComponent<GentleSway>();
                sway.maxAngleDeg = 1.1f;
                sway.speed = 0.9f + (i % 3) * 0.18f;

                floweringTrees.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = offsets[i],
                    baseY = -2.26f + (i % 2) * 0.14f,
                    parallax = 0.42f,
                    screenSpan = span,
                    baseColor = new Color(0.95f, 0.97f, 0.94f, 0.92f),
                    phase = i * 0.9f
                });
            }
        }

        private void BuildVolumetricGodRays()
        {
            // 4 feixes diagonais suaves de luz solar atravessando as copas das árvores
            float[] offsets = { -16f, -5.5f, 4.5f, 14.5f };
            const float span = 44f;
            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject go = new GameObject($"Piloto_RaioSolar_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cachedGodRaySprite;
                sr.sortingOrder = 14; // Acima dos personagens com transparência bem sutil (nunca ofusca!)
                float sx = 1.05f + (i % 2) * 0.35f;
                go.transform.localScale = new Vector3(sx, 1.45f, 1f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -24f + (i % 2) * 4f);

                godRays.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = offsets[i],
                    baseY = 1.35f,
                    parallax = 0.24f,
                    screenSpan = span,
                    baseColor = new Color(1f, 0.93f, 0.68f, 0.13f),
                    phase = i * 1.7f
                });
            }
        }

        private void BuildTrailCanopyShadows()
        {
            // Sombras suaves projetadas pelas copas sobre a trilha (fixas no terreno = 1.00x!)
            const int count = 6;
            const float span = 48f;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Piloto_SombraCopaTrilha_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cachedTrailShadowSprite;
                sr.sortingOrder = -5; // Logo acima de roadTiles (-6), abaixo dos animais (6) e bicicleta (10)
                float sx = 1.25f + (i % 3) * 0.32f;
                go.transform.localScale = new Vector3(sx, 0.95f, 1f);

                trailShadows.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -span * 0.5f + i * (span / count),
                    baseY = -3.02f + (i % 2) * 0.08f,
                    parallax = 1.00f, // 100% preso ao terreno físico!
                    screenSpan = span,
                    baseColor = new Color(0.18f, 0.26f, 0.14f, 0.22f),
                    phase = i * 1.1f
                });
            }
        }

        private void BuildUpperCanopyAndVines()
        {
            // Galhos frondosos de copa e cipós pendentes na borda superior da tela (Camada 6 — Primeiro Plano 122%)
            // Posicionados em Y = 5.45..5.95 para emoldurar cinematograficamente SEM cobrir a pista nem a barra de topo!
            const int count = 6;
            const float span = 56f;
            for (int i = 0; i < count; i++)
            {
                bool isVine = (i % 2 == 1);
                GameObject go = new GameObject(isVine ? $"Piloto_CipoSuperior_{i}" : $"Piloto_CopaSuperior_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isVine ? cachedHangingVineSprite : cachedCanopyBoughSprite;
                sr.flipX = (i % 3 == 0);
                sr.sortingOrder = 17;
                float sc = isVine ? (0.72f + (i % 2) * 0.14f) : (0.95f + (i % 3) * 0.12f);
                go.transform.localScale = new Vector3(sc, sc, 1f);

                GentleSway sway = go.AddComponent<GentleSway>();
                sway.maxAngleDeg = isVine ? 3.2f : 1.6f;
                sway.speed = isVine ? 1.35f : 1.0f;

                canopyFraming.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -span * 0.5f + i * (span / count),
                    baseY = isVine ? 5.95f : 5.75f,
                    parallax = 1.22f,
                    screenSpan = span,
                    baseColor = new Color(0.96f, 0.99f, 0.92f, 0.90f),
                    phase = i * 0.8f
                });
            }
        }

        private void BuildLowerTropicalFerns()
        {
            // Samambaias tropicais detalhadas e flores de primeiro plano (Camada 6 — 122%, rente à borda inferior)
            const int count = 8;
            const float span = 58f;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Piloto_SamambaiaPrimeiroPlano_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cachedTropicalFernSprite;
                sr.flipX = (i % 2 == 1);
                sr.sortingOrder = 17;
                float sc = 0.68f + (i % 3) * 0.12f;
                go.transform.localScale = new Vector3(sc, sc, 1f);

                GentleSway sway = go.AddComponent<GentleSway>();
                sway.maxAngleDeg = 2.6f;
                sway.speed = 1.2f + (i % 3) * 0.25f;

                lowerFerns.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -span * 0.5f + i * (span / count) + 1.8f,
                    baseY = -4.42f + (i % 2) * 0.06f,
                    parallax = 1.22f,
                    screenSpan = span,
                    baseColor = new Color(0.94f, 0.99f, 0.90f, 0.94f),
                    phase = i * 1.4f
                });
            }
        }

        private void BuildGoldenSunMotes()
        {
            // Partículas de pólen dourado dançando nos raios de sol
            const int count = 16;
            const float span = 28f;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Piloto_ParticulaLuz_{i}");
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cachedSoftMoteSprite;
                sr.sortingOrder = 15;
                float sc = 0.18f + (i % 4) * 0.08f;
                go.transform.localScale = new Vector3(sc, sc, 1f);

                goldenMotes.Add(new ParallaxSprite
                {
                    tr = go.transform,
                    sr = sr,
                    screenBaseX = -span * 0.5f + i * (span / count),
                    baseY = -1.8f + (i % 5) * 1.25f,
                    parallax = 0.68f + (i % 3) * 0.14f,
                    screenSpan = span,
                    baseColor = new Color(1f, 0.94f, 0.62f, 0.72f),
                    phase = i * 1.9f
                });
            }
        }

        // ─── Atualização Coordenada + Ciclo Dia/Noite ────────────────

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                TogglePilot();
            }
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            bool shouldBeActive = PilotEnabled && (activeBiome == BiomeType.CampoFlorido);
            float targetWeight = shouldBeActive ? 1f : 0f;
            pilotWeight = Mathf.MoveTowards(pilotWeight, targetWeight, 2.5f * Time.deltaTime);

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;
            float camYDelta = camY - 0.65f;

            DayPeriod period = (dayNight != null) ? dayNight.CurrentPeriod : DayPeriod.Manha;
            GetPeriodLighting(period, out Color sunCol, out Color mistCol, out Color rayCol, out float shadowAlpha, out Color canopyTint);

            // 1. Halo Solar cinematográfico suave (acompanha a câmera com mínimo deslocamento)
            if (sunGlowTr != null && sunGlowSR != null)
            {
                float sunPulse = 0.94f + 0.06f * Mathf.Sin(Time.time * 1.4f);
                sunGlowTr.position = new Vector3(camX + 5.8f - Mathf.Repeat(camX * 0.03f, 3f), 3.75f + camYDelta * 0.85f, 3f);
                sunGlowTr.localScale = new Vector3(3.6f * sunPulse, 3.6f * sunPulse, 1f);
                Color c = sunCol;
                c.a *= pilotWeight;
                sunGlowSR.color = c;
            }

            // 2. Névoa atmosférica do vale (18%)
            UpdateParallaxGroup(valleyMist, camX, camYDelta * 0.6f, mistCol, pilotWeight, breatheAlpha: true);

            // 3. Ipês Floridos Dourado e Roxo no vale (42%)
            UpdateParallaxGroup(floweringTrees, camX, 0f, canopyTint, pilotWeight, breatheAlpha: false);

            // 4. Raios Solares Volumétricos Diagonais (24%)
            UpdateParallaxGroup(godRays, camX, camYDelta * 0.5f, rayCol, pilotWeight, breatheAlpha: true);

            // 5. Sombras de copa na trilha (100% fixas no terreno!)
            Color shCol = new Color(0.18f, 0.26f, 0.14f, shadowAlpha);
            UpdateParallaxGroup(trailShadows, camX, 0f, shCol, pilotWeight, breatheAlpha: false);

            // 6. Moldura superior de copas/cipós e samambaias tropicais inferiores (122%)
            UpdateParallaxGroup(canopyFraming, camX, camYDelta, canopyTint, pilotWeight, breatheAlpha: false);
            UpdateParallaxGroup(lowerFerns, camX, 0f, canopyTint, pilotWeight, breatheAlpha: false);

            // 7. Partículas de luz dourada flutuando suavemente
            for (int i = 0; i < goldenMotes.Count; i++)
            {
                ParallaxSprite m = goldenMotes[i];
                float half = m.screenSpan * 0.5f;
                float drift = Time.time * 0.25f * (i % 2 == 0 ? 1f : -0.6f);
                float raw = m.screenBaseX - camX * m.parallax + drift;
                float screenOffset = Mathf.Repeat(raw + half, m.screenSpan) - half;
                float y = m.baseY + Mathf.Sin(Time.time * 1.4f + m.phase) * 0.35f;
                m.tr.position = new Vector3(camX + screenOffset, y, 0f);

                float twinkle = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 1.8f + m.phase));
                Color mc = (period == DayPeriod.Noite || period == DayPeriod.Madrugada)
                    ? new Color(0.75f, 0.96f, 1f, 0.65f)
                    : new Color(1f, 0.94f, 0.64f, 0.72f);
                mc.a *= twinkle * pilotWeight;
                m.sr.color = mc;
                m.sr.enabled = mc.a > 0.01f;
            }
        }

        private static void UpdateParallaxGroup(
            List<ParallaxSprite> list, float camX, float yOffset, Color tint, float weight, bool breatheAlpha)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ParallaxSprite item = list[i];
                float span = Mathf.Max(16f, item.screenSpan);
                float half = span * 0.5f;
                float raw = item.screenBaseX - camX * item.parallax;
                float screenOffset = Mathf.Repeat(raw + half, span) - half;
                item.tr.position = new Vector3(camX + screenOffset, item.baseY + yOffset, 0f);

                float pulse = breatheAlpha ? (0.82f + 0.18f * Mathf.Sin(Time.time * 0.9f + item.phase)) : 1f;
                Color c = new Color(
                    item.baseColor.r * tint.r,
                    item.baseColor.g * tint.g,
                    item.baseColor.b * tint.b,
                    item.baseColor.a * tint.a * pulse * weight);
                item.sr.color = c;
                item.sr.enabled = c.a > 0.008f;
            }
        }

        private static void GetPeriodLighting(
            DayPeriod period,
            out Color sunCol,
            out Color mistCol,
            out Color rayCol,
            out float shadowAlpha,
            out Color canopyTint)
        {
            switch (period)
            {
                case DayPeriod.Manha:
                    sunCol      = new Color(1f, 0.94f, 0.72f, 0.44f);
                    mistCol     = new Color(0.95f, 0.96f, 1.00f, 1.00f);
                    rayCol      = new Color(1.00f, 0.95f, 0.76f, 1.05f);
                    shadowAlpha = 0.22f;
                    canopyTint  = new Color(1.00f, 1.00f, 0.97f, 1f);
                    break;
                case DayPeriod.Tarde:
                    sunCol      = new Color(1f, 0.88f, 0.54f, 0.50f);
                    mistCol     = new Color(1.00f, 0.96f, 0.86f, 0.95f);
                    rayCol      = new Color(1.00f, 0.88f, 0.56f, 1.25f);
                    shadowAlpha = 0.26f;
                    canopyTint  = new Color(1.00f, 0.98f, 0.90f, 1f);
                    break;
                case DayPeriod.PorDoSol:
                    sunCol      = new Color(1f, 0.66f, 0.34f, 0.62f);
                    mistCol     = new Color(1.00f, 0.80f, 0.64f, 1.08f);
                    rayCol      = new Color(1.00f, 0.72f, 0.40f, 1.45f);
                    shadowAlpha = 0.30f;
                    canopyTint  = new Color(1.00f, 0.88f, 0.78f, 1f);
                    break;
                case DayPeriod.Noite:
                    sunCol      = new Color(0.70f, 0.84f, 1.00f, 0.24f);
                    mistCol     = new Color(0.62f, 0.74f, 0.96f, 0.85f);
                    rayCol      = new Color(0.68f, 0.82f, 1.00f, 0.55f);
                    shadowAlpha = 0.16f;
                    canopyTint  = new Color(0.72f, 0.78f, 0.92f, 1f);
                    break;
                case DayPeriod.Madrugada:
                default:
                    sunCol      = new Color(0.98f, 0.76f, 0.86f, 0.36f);
                    mistCol     = new Color(0.92f, 0.84f, 0.98f, 1.05f);
                    rayCol      = new Color(0.98f, 0.80f, 0.88f, 0.80f);
                    shadowAlpha = 0.18f;
                    canopyTint  = new Color(0.90f, 0.86f, 0.96f, 1f);
                    break;
            }
        }

        // ─── Geradores de Arte Cozy Storybook Cinematográfica ────────

        private static Texture2D BlankTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void BlendCircle(Texture2D tex, int cx, int cy, int r, Color col, float softness = 0.35f)
        {
            int w = tex.width, h = tex.height;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= h) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= w) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / Mathf.Max(1f, r);
                    if (d > 1f) continue;
                    float edge = (softness > 0.01f) ? (1f - TerrainHeightProfile.SmoothRange(1f - softness, 1f, d)) : 1f;
                    float a = col.a * edge;
                    Color dst = tex.GetPixel(x, y);
                    float outA = a + dst.a * (1f - a);
                    if (outA > 0.001f)
                    {
                        Color res = new Color(
                            (col.r * a + dst.r * dst.a * (1f - a)) / outA,
                            (col.g * a + dst.g * dst.a * (1f - a)) / outA,
                            (col.b * a + dst.b * dst.a * (1f - a)) / outA,
                            outA);
                        tex.SetPixel(x, y, res);
                    }
                }
            }
        }

        private static void PaintEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            int w = tex.width, h = tex.height;
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= h) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= w) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static Sprite CreateSunBloomSprite()
        {
            const int s = 192;
            Texture2D tex = BlankTex(s, s);
            float half = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    if (d >= 1f) continue;
                    float core = Mathf.Pow(1f - d, 3.2f);
                    float halo = Mathf.Pow(1f - d, 1.5f) * 0.42f;
                    float a = Mathf.Clamp01(core + halo);
                    Color c = Color.Lerp(new Color(1f, 0.82f, 0.45f, a), new Color(1f, 0.99f, 0.90f, a), core);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite CreateMistRibbonSprite()
        {
            const int w = 256, h = 96;
            Texture2D tex = BlankTex(w, h);
            Color mist = new Color(1f, 1f, 1f, 0.42f);
            BlendCircle(tex, 56, 44, 42, mist, 0.95f);
            BlendCircle(tex, 112, 52, 48, mist, 0.95f);
            BlendCircle(tex, 168, 46, 44, mist, 0.95f);
            BlendCircle(tex, 208, 40, 36, mist, 0.95f);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateGodRaySprite()
        {
            const int w = 160, h = 384;
            Texture2D tex = BlankTex(w, h);
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1); // 0 = base (chão), 1 = topo (sol)
                float vFade = TerrainHeightProfile.SmoothRange(0f, 0.35f, v) * TerrainHeightProfile.SmoothRange(1f, 0.78f, v);
                float widthHere = Mathf.Lerp(0.48f, 0.22f, v);
                for (int x = 0; x < w; x++)
                {
                    float u = (x / (float)(w - 1) - 0.5f) / widthHere;
                    float radial = Mathf.Clamp01(1f - Mathf.Abs(u));
                    radial = radial * radial * (3f - 2f * radial);
                    // Sub-raios sutis dentro do feixe
                    float streak = 0.78f + 0.22f * Mathf.Sin(u * 9f + 0.6f);
                    float a = vFade * radial * streak;
                    if (a > 0.002f)
                        tex.SetPixel(x, y, new Color(1f, 0.98f, 0.88f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
        }

        private static Sprite CreateTrailShadowSprite()
        {
            const int w = 220, h = 64;
            Texture2D tex = BlankTex(w, h);
            Color sh = new Color(1f, 1f, 1f, 0.55f);
            BlendCircle(tex, 64, 30, 34, sh, 0.92f);
            BlendCircle(tex, 112, 34, 38, sh, 0.92f);
            BlendCircle(tex, 158, 28, 32, sh, 0.92f);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 42f);
        }

        private static Texture2D GetReadableOakTexture()
        {
            Texture2D raw = Resources.Load<Texture2D>("Floresta/carvalho_bosque");
            if (raw == null) return null;
            if (raw.isReadable) return raw;
            RenderTexture rt = RenderTexture.GetTemporary(raw.width, raw.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Graphics.Blit(raw, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(raw.width, raw.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, raw.width, raw.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        /// <summary>
        /// Reaproveita a arte pintada à mão de carvalho_bosque.png (tronco orgânico + pinceladas reais de folhagem)
        /// remapeando apenas a matiz das folhas verdes para flores douradas (Ipê-Amarelo) ou lilases (Ipê-Roxo/Jacarandá),
        /// alcançando 100% de acabamento de livro ilustrado sem nenhum círculo artificial!
        /// </summary>
        private static Sprite CreateFloweringIpeSprite(bool isYellow)
        {
            Texture2D oak = GetReadableOakTexture();
            if (oak != null)
            {
                int w = oak.width, h = oak.height;
                Color[] pixels = oak.GetPixels();
                Color deepCol = isYellow ? new Color(0.74f, 0.44f, 0.06f, 1f) : new Color(0.38f, 0.18f, 0.56f, 1f);
                Color midCol  = isYellow ? new Color(0.96f, 0.74f, 0.14f, 1f) : new Color(0.68f, 0.40f, 0.86f, 1f);
                Color hiCol   = isYellow ? new Color(1.00f, 0.94f, 0.46f, 1f) : new Color(0.92f, 0.76f, 1.00f, 1f);

                for (int y = 0; y < h; y++)
                {
                    float yFrac = y / (float)h;
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        Color c = pixels[row + x];
                        if (c.a < 0.08f) continue;

                        // Identifica folhas verdes/amareladas da copa (mantendo o tronco marrom 100% original!)
                        float greenness = (c.g - c.b) + (c.g - c.r) * 0.65f;
                        if (yFrac > 0.30f && greenness > 0.08f)
                        {
                            float lum = Mathf.Clamp01(c.r * 0.299f + c.g * 0.587f + c.b * 0.114f);
                            float t = Mathf.Clamp01((lum - 0.14f) / 0.58f);
                            Color flower = (t < 0.55f)
                                ? Color.Lerp(deepCol, midCol, t / 0.55f)
                                : Color.Lerp(midCol, hiCol, (t - 0.55f) / 0.45f);
                            float blend = TerrainHeightProfile.SmoothRange(0.08f, 0.22f, greenness) * TerrainHeightProfile.SmoothRange(0.30f, 0.42f, yFrac);
                            Color res = Color.Lerp(c, flower, blend * 0.92f);
                            res.a = c.a;
                            pixels[row + x] = res;
                        }
                    }
                }

                Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                outTex.filterMode = FilterMode.Bilinear;
                outTex.wrapMode = TextureWrapMode.Clamp;
                outTex.SetPixels(pixels);
                outTex.Apply();
                return Sprite.Create(outTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.012f), 205f);
            }

            // Fallback seguro caso a textura ainda não esteja carregada
            return BiomePropsFactory.LoadOakTreeSprite();
        }

        /// <summary>
        /// Reaproveita a copa pintada à mão de carvalho_bosque.png (terço superior com folhas orgânicas detalhadas),
        /// invertida e suavemente aquecida pelo sol para emoldurar cinematograficamente o topo da tela (Camada 6)!
        /// </summary>
        private static Sprite CreateCanopyBoughSprite()
        {
            Texture2D oak = GetReadableOakTexture();
            if (oak != null)
            {
                int w = oak.width;
                int startY = Mathf.RoundToInt(oak.height * 0.56f);
                int cropH = oak.height - startY;
                Color[] srcPx = oak.GetPixels(0, startY, w, cropH);
                Color[] outPx = new Color[w * cropH];

                for (int y = 0; y < cropH; y++)
                {
                    // Inverte verticalmente para que a silhueta orgânica das folhas pendam do topo da tela!
                    int srcRow = (cropH - 1 - y) * w;
                    int dstRow = y * w;
                    float topAnchorFade = TerrainHeightProfile.SmoothRange(0f, 0.14f, y / (float)cropH);
                    for (int x = 0; x < w; x++)
                    {
                        Color c = srcPx[srcRow + x];
                        if (c.a < 0.08f) { outPx[dstRow + x] = Color.clear; continue; }
                        // Profundidade rica de primeiro plano com toque dourado nas folhas mais claras
                        float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                        Color deep = new Color(c.r * 0.72f, c.g * 0.82f, c.b * 0.68f, c.a * topAnchorFade);
                        Color sunRim = new Color(Mathf.Clamp01(c.r * 1.12f), Mathf.Clamp01(c.g * 1.10f), c.b * 0.78f, c.a * topAnchorFade);
                        outPx[dstRow + x] = Color.Lerp(deep, sunRim, TerrainHeightProfile.SmoothRange(0.38f, 0.68f, lum));
                    }
                }

                Texture2D outTex = new Texture2D(w, cropH, TextureFormat.RGBA32, false);
                outTex.filterMode = FilterMode.Bilinear;
                outTex.wrapMode = TextureWrapMode.Clamp;
                outTex.SetPixels(outPx);
                outTex.Apply();
                return Sprite.Create(outTex, new Rect(0, 0, w, cropH), new Vector2(0.5f, 0.96f), 175f);
            }

            const int fw = 320, fh = 140;
            Texture2D tex = BlankTex(fw, fh);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0.92f), 52f);
        }

        private static Sprite CreateHangingVineSprite()
        {
            const int w = 96, h = 180;
            Texture2D tex = BlankTex(w, h);
            Color stem = new Color(0.18f, 0.38f, 0.15f, 0.94f);
            Color leafDark = new Color(0.14f, 0.36f, 0.15f, 0.95f);
            Color leafSun  = new Color(0.46f, 0.74f, 0.22f, 0.95f);
            Color blossom  = new Color(0.98f, 0.54f, 0.26f, 0.96f); // Cachinho de flor alaranjada da referência!

            for (int y = 26; y < h - 4; y++)
            {
                float t = (y - 26f) / (float)(h - 30);
                int vx = Mathf.RoundToInt(48f + Mathf.Sin(t * 7.5f) * 9f);
                PaintEllipse(tex, vx, y, 2, 2, stem);
                if (y % 18 == 0)
                {
                    int side = ((y / 18) % 2 == 0) ? 1 : -1;
                    PaintEllipse(tex, vx + side * 11, y - 2, 10, 5, leafDark);
                    PaintEllipse(tex, vx + side * 12, y - 1, 7, 3, leafSun);
                }
            }
            // Pequeno cacho floral vibrante pendente na ponta (como o detalhe da árvore na referência!)
            for (int k = 0; k < 4; k++)
            {
                BlendCircle(tex, 46 + (k % 2 == 0 ? -4 : 4), 24 + k * 7, 6, blossom, 0.25f);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.96f), 54f);
        }

        private static Sprite CreateTropicalFernSprite()
        {
            const int w = 180, h = 110;
            Texture2D tex = BlankTex(w, h);
            Color fernDeep = new Color(0.12f, 0.34f, 0.15f, 0.95f);
            Color fernMid  = new Color(0.24f, 0.56f, 0.21f, 0.95f);
            Color fernSun  = new Color(0.56f, 0.80f, 0.26f, 0.95f);
            Color flowerViolet = new Color(0.68f, 0.44f, 0.90f, 0.96f);
            Color flowerWhite  = new Color(0.99f, 0.98f, 0.92f, 0.96f);

            // 5 frondes arqueadas de samambaia tropical
            float[] angles = { -0.82f, -0.42f, 0f, 0.42f, 0.82f };
            for (int f = 0; f < angles.Length; f++)
            {
                float ang = angles[f];
                int len = (f == 2) ? 74 : (f == 1 || f == 3 ? 66 : 56);
                for (int s = 6; s <= len; s += 4)
                {
                    float frac = s / (float)len;
                    int fx = Mathf.RoundToInt(90f + Mathf.Sin(ang) * s * 1.15f);
                    int fy = Mathf.RoundToInt(10f + Mathf.Cos(ang) * s - frac * frac * 14f);
                    int leafR = Mathf.Max(3, Mathf.RoundToInt(Mathf.Lerp(11f, 3f, frac)));
                    Color col = Color.Lerp(fernDeep, frac > 0.6f ? fernSun : fernMid, frac);
                    BlendCircle(tex, fx, fy, leafR, col, 0.20f);
                }
            }
            // Flores lilases e margaridas brancas no canto (ecoando o canto inferior direito da referência!)
            BlendCircle(tex, 42, 34, 7, flowerViolet, 0.22f);
            BlendCircle(tex, 54, 46, 6, flowerViolet, 0.22f);
            BlendCircle(tex, 134, 38, 7, flowerWhite, 0.22f);
            BlendCircle(tex, 134, 38, 3, new Color(1f, 0.86f, 0.22f, 1f), 0.20f);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 62f);
        }

        private static Sprite CreateSoftMoteSprite()
        {
            const int s = 32;
            Texture2D tex = BlankTex(s, s);
            BlendCircle(tex, 16, 16, 14, new Color(1f, 0.98f, 0.80f, 0.95f), 0.95f);
            BlendCircle(tex, 16, 16, 5, Color.white, 0.50f);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 40f);
        }
    }
}
