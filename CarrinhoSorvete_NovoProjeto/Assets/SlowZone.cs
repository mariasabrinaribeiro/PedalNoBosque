using UnityEngine;

/// <summary>
/// Sinalização visual nítida em pixel art (Point, PPU 32) para zonas lentas de calda de chocolate/caramelo
/// renderizadas na camada GroundDetails (acima da Track e abaixo de EntitiesAndPlayer).
/// </summary>
public class SlowZone : MonoBehaviour
{
    [Header("Configuração")]
    public bool isPermanentZone = false;
    public float lifetime = 4f;

    private SpriteRenderer sr;
    private float spawnTime;
    private static Sprite cachedPuddleSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "GroundDetails";
        sr.sortingOrder = 2;

        if (!isPermanentZone) gameObject.name = "SlowZone_Puddle";

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.85f;

        if (transform.localScale == Vector3.one)
            transform.localScale = new Vector3(1.6f, 1.3f, 1f);

        GenerateVisuals();
    }

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (!isPermanentZone && Time.time - spawnTime > lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    private void GenerateVisuals()
    {
        if (sr == null) return;
        if (sr.sprite == null)
        {
            if (cachedPuddleSprite == null) cachedPuddleSprite = CreatePixelPuddleSprite();
            sr.sprite = cachedPuddleSprite;
        }
        sr.color = Color.white;
    }

    private static Sprite CreatePixelPuddleSprite()
    {
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color outline = new Color(0.24f, 0.12f, 0.10f, 0.95f);
        Color syrupDark = new Color(0.42f, 0.21f, 0.14f, 0.90f);
        Color syrupMid = new Color(0.56f, 0.30f, 0.18f, 0.90f);
        Color highlight = new Color(0.86f, 0.62f, 0.42f, 0.92f);

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x - 31.5f) / 26f;
                float dy = (y - 31.5f) / 20f;
                float wobble = 0.16f * Mathf.Sin(Mathf.Atan2(dy, dx) * 5f + 0.6f);
                float d = Mathf.Sqrt(dx * dx + dy * dy) + wobble;
                if (d > 1.0f) tex.SetPixel(x, y, clear);
                else if (d > 0.88f) tex.SetPixel(x, y, outline);
                else if (d > 0.68f) tex.SetPixel(x, y, syrupDark);
                else tex.SetPixel(x, y, syrupMid);
            }
        }
        // Glossy caramel specular bubbles for unmistakable SlowZone readability
        for (int y = 36; y <= 40; y++)
            for (int x = 18; x <= 26; x++)
                tex.SetPixel(x, y, highlight);
        for (int y = 22; y <= 25; y++)
            for (int x = 40; x <= 45; x++)
                tex.SetPixel(x, y, highlight);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
    }
}
