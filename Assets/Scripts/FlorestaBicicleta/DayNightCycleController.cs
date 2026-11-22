using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlorestaBicicleta
{
    public enum DayPeriod
    {
        Manha,
        Tarde,
        PorDoSol,
        Noite,
        Madrugada
    }

    /// <summary>
    /// Etapa 6 — Ciclo Completo de Dia e Noite em 5 Períodos (PEDAL NO BOSQUE):
    /// Manhã -> Tarde -> Pôr do Sol -> Noite (lua, estrelas, vaga-lumes, farol na bicicleta) -> Madrugada (encontros raros).
    /// Mantém sempre alta legibilidade para crianças e famílias, com transições suaves de livro ilustrado.
    /// </summary>
    public class DayNightCycleController : MonoBehaviour
    {
        private class FireflyItem
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float offsetX;
            public float baseY;
            public float phase;
            public float speed;
        }

        private class SkyStarItem
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float offsetX;
            public float y;
            public float phase;
        }

        [Header("Duração de cada período (segundos)")]
        public float secondsPerPeriod = 32f;

        public DayPeriod CurrentPeriod { get; private set; } = DayPeriod.Manha;
        public bool IsNightOrDawn => CurrentPeriod == DayPeriod.Noite || CurrentPeriod == DayPeriod.Madrugada;
        public event System.Action<DayPeriod> OnPeriodChanged;

        private Camera cam;
        private Transform bikeTransform;
        private Image screenTintOverlay;
        private BiomeType currentBiome = BiomeType.CampoFlorido;

        private GameObject bikeHeadlightObj;
        private SpriteRenderer headlightSR;
        private Transform moonTransform;
        private SpriteRenderer moonSR;

        private readonly List<FireflyItem> fireflies = new List<FireflyItem>();
        private readonly List<SkyStarItem> skyStars = new List<SkyStarItem>();

        private float cycleTimer = 0f;
        private float nightStrength = 0f; // 0 = dia pleno, 1 = noite/Bosque da Lua
        private Color currentOverlayColor = Color.clear;

        public void Initialize(Camera targetCamera, Transform playerBike, Image overlayImage)
        {
            cam = targetCamera;
            bikeTransform = playerBike;
            screenTintOverlay = overlayImage;

            BuildMoonAndStars();
            BuildBikeHeadlight();
            BuildFireflies();
        }

        public void SetBiome(BiomeType biome)
        {
            currentBiome = biome;
        }

        public static string GetPeriodDisplayName(DayPeriod period)
        {
            switch (period)
            {
                case DayPeriod.Manha:     return "Manhã";
                case DayPeriod.Tarde:     return "Tarde";
                case DayPeriod.PorDoSol:  return "Pôr do Sol";
                case DayPeriod.Noite:     return "Noite";
                case DayPeriod.Madrugada: return "Madrugada";
                default:                  return "Dia";
            }
        }

        public static string GetPeriodNotification(DayPeriod period)
        {
            switch (period)
            {
                case DayPeriod.Manha:
                    return "Bom dia! A manhã clareou no bosque com brisa fresca!";
                case DayPeriod.Tarde:
                    return "Tarde ensolarada! Um ótimo momento para explorar novas trilhas!";
                case DayPeriod.PorDoSol:
                    return "Pôr do Sol dourado pintando as montanhas do bosque!";
                case DayPeriod.Noite:
                    return "Anoiteceu! O farol da bicicleta acendeu e os vaga-lumes apareceram!";
                case DayPeriod.Madrugada:
                    return "Madrugada mágica! Hora de encontros raros e estrelas brilhantes!";
                default:
                    return "";
            }
        }

        private void BuildMoonAndStars()
        {
            GameObject moonObj = new GameObject("LuaBosque");
            moonObj.transform.SetParent(transform, false);
            moonTransform = moonObj.transform;
            moonSR = moonObj.AddComponent<SpriteRenderer>();
            moonSR.sprite = CreateCrescentMoonSprite();
            moonSR.sortingOrder = -17;
            moonSR.color = new Color(1f, 0.98f, 0.86f, 0f);

            Sprite starSp = CreateTwinkleStarSprite();
            for (int i = 0; i < 18; i++)
            {
                GameObject sObj = new GameObject($"EstrelaCeu_{i}");
                sObj.transform.SetParent(transform, false);
                float sc = Random.Range(0.28f, 0.56f);
                sObj.transform.localScale = new Vector3(sc, sc, 1f);
                SpriteRenderer sr = sObj.AddComponent<SpriteRenderer>();
                sr.sprite = starSp;
                sr.sortingOrder = -18;
                sr.color = new Color(1f, 0.98f, 0.88f, 0f);

                skyStars.Add(new SkyStarItem
                {
                    tr = sObj.transform,
                    sr = sr,
                    offsetX = Random.Range(-11.5f, 11.5f),
                    y = Random.Range(2.4f, 5.2f),
                    phase = Random.Range(0f, Mathf.PI * 2f)
                });
            }
        }

        private void BuildBikeHeadlight()
        {
            if (bikeTransform == null) return;
            bikeHeadlightObj = new GameObject("FarolBicicleta");
            bikeHeadlightObj.transform.SetParent(bikeTransform, false);
            bikeHeadlightObj.transform.localPosition = new Vector3(3.2f, 1.35f, 0f);
            headlightSR = bikeHeadlightObj.AddComponent<SpriteRenderer>();
            headlightSR.sprite = CreateHeadlightBeamSprite();
            headlightSR.sortingOrder = 11;
            headlightSR.color = new Color(1f, 0.95f, 0.68f, 0f);
        }

        private void BuildFireflies()
        {
            Sprite glowSp = CreateFireflySprite();
            for (int i = 0; i < 16; i++)
            {
                GameObject fObj = new GameObject($"VagaLume_{i}");
                fObj.transform.SetParent(transform, false);
                float sc = Random.Range(0.32f, 0.58f);
                fObj.transform.localScale = new Vector3(sc, sc, 1f);
                SpriteRenderer sr = fObj.AddComponent<SpriteRenderer>();
                sr.sprite = glowSp;
                sr.sortingOrder = 15;
                sr.color = new Color(1f, 0.96f, 0.46f, 0f);

                fireflies.Add(new FireflyItem
                {
                    tr = fObj.transform,
                    sr = sr,
                    offsetX = Random.Range(-11f, 11f),
                    baseY = Random.Range(-2.6f, 1.8f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    speed = Random.Range(0.8f, 1.8f)
                });
            }
        }

        private void LateUpdate()
        {
            cycleTimer += Time.deltaTime;
            float totalDay = secondsPerPeriod * 5f;
            float wrapped = Mathf.Repeat(cycleTimer, totalDay);
            DayPeriod nextPeriod = (DayPeriod)Mathf.Clamp(Mathf.FloorToInt(wrapped / secondsPerPeriod), 0, 4);

            if (nextPeriod != CurrentPeriod)
            {
                CurrentPeriod = nextPeriod;
                OnPeriodChanged?.Invoke(CurrentPeriod);
            }

            // Iluminação alvo (Bosque da Lua traz um toque noturno mágico mesmo de dia!)
            bool inMoonForest = (currentBiome == BiomeType.BosqueDaLua);
            Color targetOverlay = GetTargetOverlayColor(CurrentPeriod, inMoonForest);
            currentOverlayColor = Color.Lerp(currentOverlayColor, targetOverlay, 1.6f * Time.deltaTime);
            if (screenTintOverlay != null) screenTintOverlay.color = currentOverlayColor;

            float targetNight = (CurrentPeriod == DayPeriod.Noite) ? 1f
                              : (CurrentPeriod == DayPeriod.Madrugada ? 0.72f
                              : (inMoonForest ? 0.85f : 0f));
            nightStrength = Mathf.Lerp(nightStrength, targetNight, 1.8f * Time.deltaTime);

            float camX = cam != null ? cam.transform.position.x : 0f;

            // Lua e estrelas no céu noturno
            if (moonTransform != null && moonSR != null)
            {
                moonTransform.position = new Vector3(camX + 6.8f, 4.15f, 1f);
                moonSR.color = new Color(1f, 0.98f, 0.86f, nightStrength * 0.92f);
            }

            for (int i = 0; i < skyStars.Count; i++)
            {
                SkyStarItem s = skyStars[i];
                s.tr.position = new Vector3(camX + s.offsetX, s.y, 1f);
                float twinkle = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 2.2f + s.phase));
                s.sr.color = new Color(1f, 0.98f, 0.88f, nightStrength * twinkle * 0.88f);
            }

            // Farol acolhedor da bicicleta acende à noite/madrugada ou no Bosque da Lua
            if (headlightSR != null)
            {
                float pulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 5f);
                headlightSR.color = new Color(1f, 0.94f, 0.66f, nightStrength * 0.48f * pulse);
            }

            // Vaga-lumes piscando suavemente
            for (int i = 0; i < fireflies.Count; i++)
            {
                FireflyItem f = fireflies[i];
                f.offsetX += Mathf.Sin(Time.time * 0.5f + f.phase) * f.speed * 0.55f * Time.deltaTime;
                float x = camX + Mathf.Repeat(f.offsetX + 12f, 24f) - 12f;
                float y = f.baseY + Mathf.Sin(Time.time * 2.4f + f.phase) * 0.42f;
                f.tr.position = new Vector3(x, y, 0f);
                float blink = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(Time.time * 3.1f + f.phase));
                Color col = (i % 3 == 0) ? new Color(0.56f, 0.95f, 1f, 1f) : new Color(1f, 0.95f, 0.45f, 1f);
                col.a = nightStrength * blink * 0.90f;
                f.sr.color = col;
            }
        }

        private static Color GetTargetOverlayColor(DayPeriod period, bool inMoonForest)
        {
            if (inMoonForest && period != DayPeriod.Noite)
                return new Color(0.10f, 0.16f, 0.36f, 0.28f);

            switch (period)
            {
                case DayPeriod.Manha:     return new Color(1f, 0.95f, 0.82f, 0.08f);
                case DayPeriod.Tarde:     return new Color(1f, 0.94f, 0.74f, 0.06f);
                case DayPeriod.PorDoSol:  return new Color(0.98f, 0.52f, 0.24f, 0.18f);
                case DayPeriod.Noite:     return new Color(0.06f, 0.10f, 0.28f, 0.36f);
                case DayPeriod.Madrugada: return new Color(0.24f, 0.18f, 0.42f, 0.24f);
                default:                  return Color.clear;
            }
        }

        // ─── Sprites Procedurais Noturnos ────────────────────────────

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

        private static Sprite CreateCrescentMoonSprite()
        {
            int s = 80;
            Texture2D tex = Blank(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dMain = Mathf.Sqrt((x - 40f) * (x - 40f) + (y - 40f) * (y - 40f));
                    float dCut = Mathf.Sqrt((x - 52f) * (x - 52f) + (y - 46f) * (y - 46f));
                    if (dMain <= 28f && dCut >= 23f)
                        tex.SetPixel(x, y, new Color(1f, 0.98f, 0.82f, 1f));
                    else if (dMain <= 38f)
                    {
                        float halo = Mathf.Clamp01(1f - (dMain - 26f) / 12f) * 0.22f;
                        tex.SetPixel(x, y, new Color(1f, 0.98f, 0.82f, halo));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 42f);
        }

        private static Sprite CreateTwinkleStarSprite()
        {
            int s = 24;
            Texture2D tex = Blank(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - 12f);
                    float dy = Mathf.Abs(y - 12f);
                    if (dx + dy <= 4.5f || (dx <= 1.2f && dy <= 9f) || (dy <= 1.2f && dx <= 9f))
                        tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateFireflySprite()
        {
            int s = 28;
            Texture2D tex = Blank(s, s);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - 14f) * (x - 14f) + (y - 14f) * (y - 14f)) / 13f;
                    if (d <= 1f)
                    {
                        float a = Mathf.SmoothStep(1f, 0f, d * d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 40f);
        }

        private static Sprite CreateHeadlightBeamSprite()
        {
            int w = 160, h = 72;
            Texture2D tex = Blank(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float halfCone = Mathf.Lerp(6f, 34f, t);
                float falloffX = Mathf.SmoothStep(1f, 0f, t * t);
                for (int y = 0; y < h; y++)
                {
                    float dy = Mathf.Abs(y - h * 0.5f);
                    if (dy <= halfCone)
                    {
                        float falloffY = Mathf.SmoothStep(1f, 0f, (dy / halfCone) * (dy / halfCone));
                        tex.SetPixel(x, y, new Color(1f, 0.96f, 0.72f, falloffX * falloffY));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 28f);
        }
    }
}
