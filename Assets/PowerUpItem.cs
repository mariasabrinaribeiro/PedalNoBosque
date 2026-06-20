using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public enum PowerUpType { Magnet, Shield, TripleShot }
    public PowerUpType type;

    private SpriteRenderer sr;
    private Vector3 basePosition;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 4;

        basePosition = transform.position;

        // Ensure collider is configured
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        ConfigureVisuals();
    }

    public void Initialize(PowerUpType itemType, Vector3 pos)
    {
        type = itemType;
        transform.position = pos;
        basePosition = pos;
        ConfigureVisuals();
    }

    private void ConfigureVisuals()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        Color mainColor = Color.white;
        Color outlineColor = Color.white;

        switch (type)
        {
            case PowerUpType.Magnet:
                mainColor = new Color(1f, 0.843f, 0f, 1f); // Gold
                outlineColor = new Color(0.6f, 0.4f, 0f, 1f);
                gameObject.name = "PowerUp_Magnet";
                break;
            case PowerUpType.Shield:
                mainColor = new Color(0f, 0.9f, 1f, 1f); // Cyan
                outlineColor = new Color(0f, 0.5f, 0.8f, 1f);
                gameObject.name = "PowerUp_Shield";
                break;
            case PowerUpType.TripleShot:
                mainColor = new Color(1f, 0.2f, 0.8f, 1f); // Magenta
                outlineColor = new Color(0.5f, 0f, 0.4f, 1f);
                gameObject.name = "PowerUp_TripleShot";
                break;
        }

        sr.sprite = CreatePowerUpSprite(mainColor, outlineColor);
    }

    void Update()
    {
        // Gentle bobbing up and down
        float bob = Mathf.Sin(Time.time * 4f) * 0.12f;
        transform.position = basePosition + new Vector3(0f, bob, 0f);

        // Gentle scale pulsing to look active
        float pulse = Mathf.PingPong(Time.time * 2f, 0.15f) + 0.9f;
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        driver player = collision.GetComponent<driver>();
        if (player != null)
        {
            GameUIController ui = GameUIController.Instance;
            string msg = "";

            switch (type)
            {
                case PowerUpType.Magnet:
                    player.ActivateMagnet(5f);
                    msg = "IMÃ COLETADO! ATRAI ITENS POR 5s";
                    break;
                case PowerUpType.Shield:
                    player.ActivateShield();
                    msg = "ESCUDO COLETADO! BLOQUEIA 1 DANO";
                    break;
                case PowerUpType.TripleShot:
                    player.ActivateTripleShot(5f);
                    msg = "TIRO TRIPLO COLETADO POR 5s!";
                    break;
            }

            if (ui != null)
            {
                ui.ShowDeliveryMessage(msg);
            }

            // Play procedural powerup SFX
            if (GameAudioController.Instance != null)
            {
                GameAudioController.Instance.PlayPowerup();
            }

            // Spawn colored particle burst
            SpawnCollectParticles();

            // Destroy item (using pooled logic or destroy if dynamically instantiated)
            Destroy(gameObject);
        }
    }

    private void SpawnCollectParticles()
    {
        Color particleColor = Color.white;
        if (type == PowerUpType.Magnet) particleColor = Color.yellow;
        else if (type == PowerUpType.Shield) particleColor = Color.cyan;
        else if (type == PowerUpType.TripleShot) particleColor = Color.magenta;

        for (int i = 0; i < 8; i++)
        {
            GameObject p = new GameObject("PowerUpParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 8f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateParticleSprite(6, 6, particleColor);
            particleSr.sortingOrder = 6;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(3f, 5f);
        }
    }

    private Sprite CreatePowerUpSprite(Color mainColor, Color outlineColor)
    {
        int w = 24;
        int h = 24;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - w / 2f;
                float dy = y - h / 2f;
                float distSq = dx * dx + dy * dy;
                float radiusOuter = w / 2f;
                float radiusInner = radiusOuter - 3f;

                if (distSq <= radiusOuter * radiusOuter)
                {
                    if (distSq > radiusInner * radiusInner)
                    {
                        tex.SetPixel(x, y, outlineColor);
                    }
                    else
                    {
                        // Emulate Bloom glow: soft white center
                        if (distSq <= 2f * 2f)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            tex.SetPixel(x, y, mainColor);
                        }
                    }
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateParticleSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, color);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
