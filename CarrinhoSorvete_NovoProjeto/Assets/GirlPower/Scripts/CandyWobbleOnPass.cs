using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Feedback lúdico "WobbleOnPass" para casas, placas, toldos e bichinhos doces:
/// - Dá um pulinho elástico (squash & stretch) quando o carrinho passa perto.
/// - Comemora em grupo quando ocorre uma entrega, power-up ou passagem por portão.
/// - Solta confetes/estrelinhas leves em pixel art na camada VFX.
/// </summary>
public sealed class CandyWobbleOnPass : MonoBehaviour
{
    public static readonly List<CandyWobbleOnPass> All = new List<CandyWobbleOnPass>();

    public float triggerRadius = 4.6f;
    public float hopHeight = 0.22f;
    public float squashAmount = 0.14f;
    public float duration = 0.45f;
    public float cooldown = 2.0f;

    private Vector3 basePos;
    private Vector3 baseScale;
    private float wobbleTimer = -1f;
    private float lastTriggerTime = -10f;
    private float currentIntensity = 1f;

    private static Sprite cachedStarSprite;
    private static readonly Color[] ConfettiColors =
    {
        new Color(1.00f, 0.45f, 0.78f, 0.85f), // Morango
        new Color(1.00f, 0.88f, 0.35f, 0.85f), // Baunilha/Limão
        new Color(0.55f, 0.90f, 0.72f, 0.85f), // Menta
        new Color(0.75f, 0.56f, 0.98f, 0.85f), // Uva
        new Color(0.52f, 0.84f, 1.00f, 0.85f)  // Céu Doce
    };

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Start()
    {
        basePos = transform.position;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (wobbleTimer >= 0f)
        {
            wobbleTimer += Time.deltaTime;
            float t = Mathf.Clamp01(wobbleTimer / Mathf.Max(0.1f, duration));
            float arch = Mathf.Sin(t * Mathf.PI);
            float elastic = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);

            transform.position = new Vector3(basePos.x, basePos.y + arch * hopHeight * currentIntensity, basePos.z);
            float sx = 1f + elastic * squashAmount * currentIntensity;
            float sy = 1f - elastic * squashAmount * currentIntensity;
            transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

            if (t >= 1f)
            {
                wobbleTimer = -1f;
                transform.position = basePos;
                transform.localScale = baseScale;
            }
            return;
        }

        if (Time.time - lastTriggerTime < cooldown) return;
        DriverPro player = DriverPro.Instance;
        if (player == null) return;

        Vector2 delta = (Vector2)player.transform.position - (Vector2)basePos;
        if (delta.sqrMagnitude <= triggerRadius * triggerRadius)
        {
            TriggerWobble(0.85f);
        }
    }

    public void TriggerWobble(float intensity = 1f)
    {
        wobbleTimer = 0f;
        lastTriggerTime = Time.time;
        currentIntensity = Mathf.Clamp(intensity, 0.4f, 1.6f);
    }

    public static void CelebrateNearby(Vector3 center, float radius = 12f)
    {
        float r2 = radius * radius;
        for (int i = 0; i < All.Count; i++)
        {
            CandyWobbleOnPass w = All[i];
            if (w == null) continue;
            if (((Vector2)(w.transform.position - center)).sqrMagnitude <= r2)
                w.TriggerWobble(1.35f);
        }
    }

    public static void SpawnConfettiBurst(Vector3 pos, int count = 9)
    {
        Sprite star = GetStarSprite();
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("CandyConfetti");
            go.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.4f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.45f, 0.75f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = star;
            sr.color = ConfettiColors[i % ConfettiColors.Length];
            sr.sortingLayerName = "VFX";
            sr.sortingOrder = 12;

            float angle = (i * (360f / count) + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
            ConfettiPiece piece = go.AddComponent<ConfettiPiece>();
            piece.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.55f) * Random.Range(2.4f, 4.6f);
            piece.spinSpeed = Random.Range(-360f, 360f);
        }
    }

    private static Sprite GetStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;
        const int S = 12;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                int dx = Mathf.Abs(x - 5);
                int dy = Mathf.Abs(y - 5);
                bool star = (dx + dy <= 3) || (dx == 0 && dy <= 5) || (dy == 0 && dx <= 5);
                tex.SetPixel(x, y, star ? white : clear);
            }
        }
        tex.Apply(false, false);
        cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
        return cachedStarSprite;
    }

    private sealed class ConfettiPiece : MonoBehaviour
    {
        public Vector2 velocity;
        public float spinSpeed;
        private float age;
        private const float Lifetime = 0.72f;
        private SpriteRenderer sr;

        private void Awake() => sr = GetComponent<SpriteRenderer>();

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= Lifetime) { Destroy(gameObject); return; }

            velocity.y -= 5.2f * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.85f, 0f, age / Lifetime);
                sr.color = c;
            }
        }
    }
}
