using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central Game Feel ("Juice") & Micro-Interaction Engine for Baía do Avesso:
/// - Elastic scale-pop tweens (120% -> 100%) on harvest, painting, building & serving
/// - Subtle camera screen-shake on rewarding actions
/// - Flying physical coins arcing from world NPCs/tables directly into the HUD coin counter
///   with escalating metallic chimes on arrival!
/// </summary>
public sealed class GirlPowerOrla3DJuice : MonoBehaviour
{
    public static GirlPowerOrla3DJuice Instance { get; private set; }

    public Camera sceneCamera;
    public GirlPowerOrla3DAudio audioController;

    private sealed class ScalePop
    {
        public Transform target;
        public Vector3 baseScale;
        public float peak;
        public float age;
        public float duration;
    }

    private sealed class FlyingCoin
    {
        public Vector2 startGui;
        public Vector2 endGui;
        public Vector2 ctrlGui;
        public float delay;
        public float age;
        public float duration;
        public int index;
    }

    private readonly List<ScalePop> pops = new List<ScalePop>(16);
    private readonly List<FlyingCoin> coins = new List<FlyingCoin>(32);

    private float shakeTimer;
    private float shakeDuration;
    private float shakeIntensity;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
    }

    public static void PopScale(Transform target, float peak = 1.22f, float duration = 0.36f)
    {
        if (Instance == null || target == null) return;
        for (int i = 0; i < Instance.pops.Count; i++)
        {
            if (Instance.pops[i].target == target)
            {
                Instance.pops[i].age = 0f;
                Instance.pops[i].peak = peak;
                Instance.pops[i].duration = duration;
                return;
            }
        }
        Instance.pops.Add(new ScalePop
        {
            target = target,
            baseScale = target.localScale,
            peak = peak,
            age = 0f,
            duration = duration
        });
    }

    public static void ShakeCamera(float intensity = 0.14f, float duration = 0.18f)
    {
        if (Instance == null) return;
        Instance.shakeIntensity = Mathf.Max(Instance.shakeIntensity, intensity);
        Instance.shakeDuration = Mathf.Max(0.05f, duration);
        Instance.shakeTimer = Instance.shakeDuration;
    }

    public static void SpawnFlyingCoins(Vector3 worldOrigin, int visualCount = 6)
    {
        if (Instance == null) return;
        Camera cam = Instance.sceneCamera != null ? Instance.sceneCamera : Camera.main;
        Vector2 startGui = new Vector2(Screen.width * 0.5f, Screen.height * 0.45f);
        if (cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(worldOrigin + Vector3.up * 1.1f);
            if (sp.z > 0.1f)
                startGui = new Vector2(sp.x, Screen.height - sp.y);
        }

        float s = Mathf.Clamp(Screen.width / 1600f, 0.68f, 1.15f);
        Vector2 endGui = new Vector2(68f * s, Screen.height - 46f * s);
        int count = Mathf.Clamp(visualCount, 1, 14);

        for (int i = 0; i < count; i++)
        {
            Vector2 jitter = new Vector2(Random.Range(-42f, 42f), Random.Range(-36f, 24f));
            Vector2 mid = Vector2.Lerp(startGui, endGui, 0.45f) + new Vector2(Random.Range(-90f, 90f), -Random.Range(60f, 140f));
            Instance.coins.Add(new FlyingCoin
            {
                startGui = startGui + jitter,
                endGui = endGui,
                ctrlGui = mid,
                delay = i * 0.065f,
                age = 0f,
                duration = 0.58f + i * 0.02f,
                index = i
            });
        }
    }

    public Vector3 GetCameraShakeOffset()
    {
        if (shakeTimer <= 0f) return Vector3.zero;
        float k = shakeTimer / Mathf.Max(0.01f, shakeDuration);
        float amp = shakeIntensity * k;
        return new Vector3(
            (Mathf.PerlinNoise(Time.unscaledTime * 38f, 0.3f) - 0.5f) * 2f * amp,
            (Mathf.PerlinNoise(0.7f, Time.unscaledTime * 42f) - 0.5f) * 2f * amp,
            0f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (shakeTimer > 0f)
            shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);

        for (int i = pops.Count - 1; i >= 0; i--)
        {
            ScalePop p = pops[i];
            if (p.target == null) { pops.RemoveAt(i); continue; }
            p.age += dt;
            if (p.age >= p.duration)
            {
                p.target.localScale = p.baseScale;
                pops.RemoveAt(i);
                continue;
            }
            float t = p.age / p.duration;
            // Elastic overshoot: grows to peak (122%), dips subtly, settles at 100%
            float env = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 2.2f);
            float factor = 1f + (p.peak - 1f) * env * 1.45f;
            p.target.localScale = p.baseScale * factor;
        }

        float udt = Time.unscaledDeltaTime;
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            FlyingCoin c = coins[i];
            if (c.delay > 0f) { c.delay -= udt; continue; }
            c.age += udt;
            if (c.age >= c.duration)
            {
                if (audioController != null)
                    audioController.PlayCoinClink(1.0f + c.index * 0.065f);
                coins.RemoveAt(i);
            }
        }
    }

    private void OnGUI()
    {
        if (coins.Count == 0) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        GUI.depth = -25;
        float sz = 28f * Mathf.Clamp(Screen.width / 1600f, 0.7f, 1.15f);

        for (int i = 0; i < coins.Count; i++)
        {
            FlyingCoin c = coins[i];
            if (c.delay > 0f) continue;
            float t = Mathf.Clamp01(c.age / Mathf.Max(0.01f, c.duration));
            float ease = t * t * (3f - 2f * t);
            Vector2 a = Vector2.Lerp(c.startGui, c.ctrlGui, ease);
            Vector2 b = Vector2.Lerp(c.ctrlGui, c.endGui, ease);
            Vector2 pos = Vector2.Lerp(a, b, ease);
            float sc = Mathf.Lerp(1.15f, 0.85f, ease);
            float w = sz * sc;
            GUI.DrawTexture(new Rect(pos.x - w * 0.5f, pos.y - w * 0.5f, w, w), GirlPowerOrla3DUITheme.IconCoin);
        }
    }
}
