using UnityEngine;

public class SlowZone : MonoBehaviour
{
    private SpriteRenderer sr;
    private float spawnTime;
    private float lifetime = 4f;

    private static Sprite cachedWhiteSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2; // sob o jogador e inimigos, mas acima da rua

        gameObject.name = "SlowZone_Puddle";

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        transform.localScale = new Vector3(2.5f, 2.5f, 1f); 

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

    private void GenerateVisuals()
    {
        if (sr == null) return;

        // Se não houver sprite definido no SpriteRenderer, usamos um sprite branco estático de fallback
        if (sr.sprite == null)
        {
            if (cachedWhiteSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();
                cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            }
            sr.sprite = cachedWhiteSprite;
        }

        // OTIMIZADO: Em vez de pintar pixels manualmente em loop for, aplica a cor translúcida de açúcar queimado diretamente
        sr.color = new Color(0.15f, 0.12f, 0.1f, 0.5f); 
    }
}
