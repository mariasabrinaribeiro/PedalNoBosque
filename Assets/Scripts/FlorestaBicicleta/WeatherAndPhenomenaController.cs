using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    public enum WeatherState
    {
        Ensolarado,
        ChuvaLeve,
        ArcoIris,
        Neblina
    }

    /// <summary>
    /// Etapa 10 — Fenômenos Naturais do PEDAL NO BOSQUE (100% com Object Pooling / zero GC):
    /// - Chuva leve acolhedora;
    /// - Arco-íris delicado que surge após a chuva ou no Vale das Águas;
    /// - Neblina suave na Madrugada e Floresta dos Segredos;
    /// - Estrela cadente atravessando o céu durante a Noite / Bosque da Lua.
    /// </summary>
    public class WeatherAndPhenomenaController : MonoBehaviour
    {
        private class RainDrop
        {
            public Transform tr;
            public SpriteRenderer sr;
            public float speed;
        }

        public WeatherState CurrentWeather { get; private set; } = WeatherState.Ensolarado;
        public event System.Action<string, Color> OnWeatherMessage;

        private Camera cam;
        private DayNightCycleController dayNight;
        private BiomeType currentBiome = BiomeType.CampoFlorido;

        private readonly List<RainDrop> rainPool = new List<RainDrop>();
        private Transform rainbowTr;
        private SpriteRenderer rainbowSR;
        private Transform mistTr;
        private SpriteRenderer mistSR;

        // Estrela cadente em pool
        private Transform shootingStarTr;
        private SpriteRenderer shootingStarSR;
        private bool shootingStarActive = false;
        private float shootingStarT = 0f;
        private float shootingStarCooldown = 8f;
        private Vector3 shootingStart;

        private float weatherTimer = 42f;
        private float rainAlpha = 0f;
        private float rainbowAlpha = 0f;
        private float mistAlpha = 0f;

        public void Initialize(Camera targetCamera, DayNightCycleController cycle)
        {
            cam = targetCamera;
            dayNight = cycle;

            BuildRainPool();
            BuildRainbow();
            BuildMist();
            BuildShootingStar();
        }

        public void SetBiome(BiomeType biome)
        {
            currentBiome = biome;
        }

        private void BuildRainPool()
        {
            Sprite dropSp = CreateRainStreakSprite();
            for (int i = 0; i < 18; i++)
            {
                GameObject go = new GameObject($"GotaChuva_{i}");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(Random.Range(-12f, 12f), Random.Range(-3f, 6f), 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dropSp;
                sr.sortingOrder = 15;
                sr.color = new Color(0.76f, 0.90f, 1f, 0f);
                rainPool.Add(new RainDrop { tr = go.transform, sr = sr, speed = Random.Range(7.5f, 10.5f) });
            }
        }

        private void BuildRainbow()
        {
            GameObject go = new GameObject("ArcoIrisBosque");
            go.transform.SetParent(transform, false);
            rainbowTr = go.transform;
            rainbowTr.localScale = new Vector3(1.55f, 1.35f, 1f);
            rainbowSR = go.AddComponent<SpriteRenderer>();
            rainbowSR.sprite = CreateRainbowSprite();
            rainbowSR.sortingOrder = -15;
            rainbowSR.color = new Color(1f, 1f, 1f, 0f);
        }

        private void BuildMist()
        {
            GameObject go = new GameObject("NeblinaSuave");
            go.transform.SetParent(transform, false);
            mistTr = go.transform;
            mistTr.localScale = new Vector3(2.8f, 1.1f, 1f);
            mistSR = go.AddComponent<SpriteRenderer>();
            mistSR.sprite = CreateMistSprite();
            mistSR.sortingOrder = 13;
            mistSR.color = new Color(0.92f, 0.96f, 1f, 0f);
        }

        private void BuildShootingStar()
        {
            GameObject go = new GameObject("EstrelaCadente");
            go.transform.SetParent(transform, false);
            shootingStarTr = go.transform;
            shootingStarSR = go.AddComponent<SpriteRenderer>();
            shootingStarSR.sprite = CreateShootingStarSprite();
            shootingStarSR.sortingOrder = -14;
            shootingStarSR.color = Color.clear;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float camX = cam.transform.position.x;

            // Ciclo climático suave e natural
            weatherTimer -= Time.deltaTime;
            if (weatherTimer <= 0f)
            {
                AdvanceWeather();
            }

            bool wantRain = (CurrentWeather == WeatherState.ChuvaLeve);
            bool wantRainbow = (CurrentWeather == WeatherState.ArcoIris)
                            && (dayNight == null || !dayNight.IsNightOrDawn);
            bool wantMist = (CurrentWeather == WeatherState.Neblina)
                         || (currentBiome == BiomeType.TrilhaDaFloresta)
                         || (dayNight != null && dayNight.CurrentPeriod == DayPeriod.Madrugada);

            rainAlpha = Mathf.Lerp(rainAlpha, wantRain ? 0.52f : 0f, 1.6f * Time.deltaTime);
            rainbowAlpha = Mathf.Lerp(rainbowAlpha, wantRainbow ? 0.58f : 0f, 1.2f * Time.deltaTime);
            mistAlpha = Mathf.Lerp(mistAlpha, wantMist ? 0.24f : 0f, 1.2f * Time.deltaTime);

            // Atualiza gotinhas de chuva (pool)
            for (int i = 0; i < rainPool.Count; i++)
            {
                RainDrop r = rainPool[i];
                if (rainAlpha > 0.02f)
                {
                    Vector3 p = r.tr.position;
                    p.x -= r.speed * 0.22f * Time.deltaTime;
                    p.y -= r.speed * Time.deltaTime;
                    if (p.y < -4.2f)
                    {
                        p.y = Random.Range(4.8f, 6.2f);
                        p.x = camX + Random.Range(-11f, 13f);
                    }
                    r.tr.position = p;
                }
                r.sr.color = new Color(0.76f, 0.90f, 1f, rainAlpha);
            }

            // Arco-íris suave no fundo
            if (rainbowTr != null && rainbowSR != null)
            {
                rainbowTr.position = new Vector3(camX + 2.5f, 2.2f, 2f);
                rainbowSR.color = new Color(1f, 1f, 1f, rainbowAlpha);
            }

            // Neblina delicada deslizando rente ao solo
            if (mistTr != null && mistSR != null)
            {
                float drift = Mathf.Sin(Time.time * 0.4f) * 1.4f;
                mistTr.position = new Vector3(camX + drift, -2.5f, 0f);
                mistSR.color = new Color(0.92f, 0.96f, 1f, mistAlpha);
            }

            // Estrela cadente durante a Noite, Madrugada ou Bosque da Lua
            bool canShootStar = (dayNight != null && dayNight.IsNightOrDawn) || currentBiome == BiomeType.BosqueDaLua;
            if (shootingStarActive)
            {
                shootingStarT += Time.deltaTime * 1.25f;
                float t = Mathf.Clamp01(shootingStarT);
                shootingStarTr.position = shootingStart + new Vector3(-7.5f * t, -2.4f * t, 0f);
                float a = Mathf.Sin(t * Mathf.PI);
                shootingStarSR.color = new Color(1f, 0.98f, 0.80f, a * 0.95f);
                if (t >= 1f)
                {
                    shootingStarActive = false;
                    shootingStarCooldown = Random.Range(10f, 18f);
                }
            }
            else if (canShootStar)
            {
                shootingStarCooldown -= Time.deltaTime;
                if (shootingStarCooldown <= 0f)
                {
                    shootingStarActive = true;
                    shootingStarT = 0f;
                    shootingStart = new Vector3(camX + Random.Range(4f, 10f), Random.Range(3.6f, 5.0f), 1f);
                }
            }
        }

        private void AdvanceWeather()
        {
            if (CurrentWeather == WeatherState.Ensolarado)
            {
                CurrentWeather = WeatherState.ChuvaLeve;
                weatherTimer = 22f;
                OnWeatherMessage?.Invoke("Comecou uma chuva leve e fresquinha no bosque!", new Color(0.72f, 0.92f, 1f, 1f));
            }
            else if (CurrentWeather == WeatherState.ChuvaLeve)
            {
                CurrentWeather = WeatherState.ArcoIris;
                weatherTimer = 26f;
                OnWeatherMessage?.Invoke("A chuva passou e um Arco-Iris colorido surgiu no ceu!", new Color(1f, 0.92f, 0.62f, 1f));
            }
            else if (CurrentWeather == WeatherState.ArcoIris)
            {
                CurrentWeather = WeatherState.Neblina;
                weatherTimer = 24f;
            }
            else
            {
                CurrentWeather = WeatherState.Ensolarado;
                weatherTimer = 38f;
            }
        }

        // ─── Sprites Suaves de Fenômenos Naturais ────────────────────

        private static Texture2D Blank(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static Sprite CreateRainStreakSprite()
        {
            Texture2D tex = Blank(6, 26);
            for (int y = 0; y < 26; y++)
            {
                float a = Mathf.Sin((y / 25f) * Mathf.PI);
                tex.SetPixel(2, y, new Color(1f, 1f, 1f, a * 0.85f));
                tex.SetPixel(3, y, new Color(1f, 1f, 1f, a * 0.85f));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 6, 26), new Vector2(0.5f, 0.5f), 40f);
        }

        private static Sprite CreateRainbowSprite()
        {
            int w = 220, h = 110;
            Texture2D tex = Blank(w, h);
            Color[] bands =
            {
                new Color(1f, 0.42f, 0.46f, 0.72f),
                new Color(1f, 0.70f, 0.30f, 0.72f),
                new Color(1f, 0.92f, 0.38f, 0.72f),
                new Color(0.46f, 0.86f, 0.52f, 0.72f),
                new Color(0.42f, 0.78f, 1f, 0.72f),
                new Color(0.72f, 0.56f, 1f, 0.72f)
            };

            float cx = w * 0.5f;
            float cy = 4f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (r >= 62f && r <= 104f)
                    {
                        float t = (104f - r) / 42f;
                        int idx = Mathf.Clamp(Mathf.FloorToInt(t * bands.Length), 0, bands.Length - 1);
                        float fadeY = Mathf.Clamp01((y - 6f) / 24f);
                        Color c = bands[idx];
                        c.a *= fadeY;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.2f), 26f);
        }

        private static Sprite CreateMistSprite()
        {
            int w = 180, h = 44;
            Texture2D tex = Blank(w, h);
            for (int y = 0; y < h; y++)
            {
                float fy = Mathf.Sin((y / (float)(h - 1)) * Mathf.PI);
                for (int x = 0; x < w; x++)
                {
                    float fx = Mathf.Sin((x / (float)(w - 1)) * Mathf.PI);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, fx * fy * 0.70f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite CreateShootingStarSprite()
        {
            int w = 80, h = 16;
            Texture2D tex = Blank(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = 1f - (x / (float)(w - 1));
                for (int y = 5; y <= 10; y++)
                {
                    float fy = 1f - Mathf.Abs(y - 7.5f) / 3.5f;
                    tex.SetPixel(x, y, new Color(1f, 0.98f, 0.80f, t * t * fy));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.15f, 0.5f), 36f);
        }
    }
}
