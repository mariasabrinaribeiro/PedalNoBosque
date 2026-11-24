using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public enum PowerUpType { Magnet, Shield, TripleShot, Bomb }
    public PowerUpType type;

    private SpriteRenderer sr;
    private Vector3 basePosition;

    // Static Cache for sprites to prevent GC spikes and memory leak
    private static Sprite cachedMagnetSprite;
    private static Sprite cachedShieldSprite;
    private static Sprite cachedTripleShotSprite;
    private static Sprite cachedBombSprite;
    private static Sprite cachedWhiteSprite;

    private static Sprite GetWhiteSprite()
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
        return cachedWhiteSprite;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "EntitiesAndPlayer";
        sr.sortingOrder = 6;

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
                if (cachedMagnetSprite == null)
                {
                    mainColor = new Color(1f, 0.843f, 0f, 1f); // Gold
                    outlineColor = new Color(0.6f, 0.4f, 0f, 1f);
                    cachedMagnetSprite = CreatePowerUpSprite(mainColor, outlineColor);
                }
                sr.sprite = cachedMagnetSprite;
                gameObject.name = "PowerUp_Magnet";
                break;
            case PowerUpType.Shield:
                if (cachedShieldSprite == null)
                {
                    mainColor = new Color(0f, 0.9f, 1f, 1f); // Cyan
                    outlineColor = new Color(0f, 0.5f, 0.8f, 1f);
                    cachedShieldSprite = CreatePowerUpSprite(mainColor, outlineColor);
                }
                sr.sprite = cachedShieldSprite;
                gameObject.name = "PowerUp_Shield";
                break;
            case PowerUpType.TripleShot:
                if (cachedTripleShotSprite == null)
                {
                    mainColor = new Color(1f, 0.2f, 0.8f, 1f); // Magenta
                    outlineColor = new Color(0.5f, 0f, 0.4f, 1f);
                    cachedTripleShotSprite = CreatePowerUpSprite(mainColor, outlineColor);
                }
                sr.sprite = cachedTripleShotSprite;
                gameObject.name = "PowerUp_TripleShot";
                break;
            case PowerUpType.Bomb:
                if (cachedBombSprite == null)
                {
                    mainColor = new Color(0.9f, 0.1f, 0.1f, 1f); // Vibrant Red
                    outlineColor = new Color(0.4f, 0.05f, 0.05f, 1f);
                    cachedBombSprite = CreatePowerUpSprite(mainColor, outlineColor);
                }
                sr.sprite = cachedBombSprite;
                gameObject.name = "PowerUp_Bomb";
                break;
        }
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
        DriverPro player = collision.GetComponent<DriverPro>();
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
                case PowerUpType.Bomb:
                    TriggerScreenClearBomb();
                    msg = "BOMBA DE CHOCOLATE! TODOS OS MONSTROS DESTRUÍDOS!";
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

            // Spawn colored particle burst & joyful star confetti!
            SpawnCollectParticles();
            CandyWobbleOnPass.SpawnConfettiBurst(transform.position, 9);
            CandyWobbleOnPass.CelebrateNearby(transform.position, 10f);

            // Destroy item
            Destroy(gameObject);
        }
    }

    private void TriggerScreenClearBomb()
    {
        // Cria uma cópia da lista de inimigos ativos para evitar erro de concorrência ao desativar
        var enemiesToKill = new System.Collections.Generic.List<Enemy>(Enemy.ActiveEnemies);
        foreach (var enemy in enemiesToKill)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                // Causa dano igual à vida máxima do inimigo para derrotá-lo e computar pontos
                enemy.TakeDamage(enemy.maxHealth);
            }
        }

        // Desativa permanentemente o gerador de inimigos
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.enabled = false;
        }

        // Tremor de tela para dar impacto visual
        if (GameCameraController.Instance != null)
        {
            GameCameraController.Instance.TriggerShake(0.35f, 0.5f);
        }

        // Toca o som de explosão
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayExplosion();
        }
    }

    private void SpawnCollectParticles()
    {
        Color particleColor = Color.white;
        if (type == PowerUpType.Magnet) particleColor = Color.yellow;
        else if (type == PowerUpType.Shield) particleColor = Color.cyan;
        else if (type == PowerUpType.TripleShot) particleColor = Color.magenta;
        else if (type == PowerUpType.Bomb) particleColor = Color.red;

        // ETAPA 4: Opacidade máxima 35%, -40% emissão (8 -> 5), camada VFX
        particleColor.a = 0.35f;
        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 5; i++)
        {
            GameObject p = new GameObject("PowerUpParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 5f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = whiteSprite;
            particleSr.color = particleColor;
            particleSr.sortingLayerName = "VFX";
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
}
