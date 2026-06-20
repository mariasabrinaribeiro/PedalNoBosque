using UnityEngine;

public class SlowZone : MonoBehaviour
{
    private SpriteRenderer sr;
    private float spawnTime;
    private float lifetime = 4f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2; // under player and enemies, but above roads

        gameObject.name = "SlowZone_Puddle";

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        transform.localScale = new Vector3(2.5f, 2.5f, 1f); // large puddle

        GenerateVisuals();
    }

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnTime > lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    private static Sprite cachedPuddleSprite;

    private void GenerateVisuals()
    {
        if (sr == null) return;

        if (cachedPuddleSprite == null)
        {
            int w = 32;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color puddleColor = new Color(0.15f, 0.12f, 0.1f, 0.5f); // translucent burnt-sugar brown
            Color edgeColor = new Color(0.1f, 0.08f, 0.05f, 0.75f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - w / 2f;
                    float dy = y - h / 2f;
                    float distSq = dx * dx + dy * dy;
                    float rOuter = w / 2f;
                    float rInner = rOuter - 3f;

                    if (distSq <= rOuter * rOuter)
                    {
                        if (distSq > rInner * rInner)
                        {
                            tex.SetPixel(x, y, edgeColor);
                        }
                        else
                        {
                            tex.SetPixel(x, y, puddleColor);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            cachedPuddleSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
        
        sr.sprite = cachedPuddleSprite;
    }
}
