using System.Collections;
using UnityEngine;

public class BossBitterBaron : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 2500;
    public int currentHealth;

    private Transform playerTransform;
    private driver playerDriver; 
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private bool isDead = false;

    private float lastAttackTime = 0f;
    private float attackInterval = 2.5f;

    private float lastSummonTime = 0f;
    private float summonInterval = 6.0f;

    // Singletons / Static Cache to prevent GC spikes and memory leak
    private static Sprite cachedBossSprite;
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

    private static Sprite GetBossSprite()
    {
        if (cachedBossSprite == null)
        {
            int w = 64;
            int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color darkGrey = new Color(0.12f, 0.12f, 0.12f, 1f); 
            Color metallicEdge = new Color(0.35f, 0.35f, 0.35f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x <= 4 || x >= w - 5 || y <= 4 || y >= h - 5)
                    {
                        tex.SetPixel(x, y, metallicEdge);
                    }
                    else
                    {
                        tex.SetPixel(x, y, darkGrey);
                    }
                }
            }
            tex.Apply();
            cachedBossSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
        return cachedBossSprite;
    }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 3;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        gameObject.name = "Barão Amargo";

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.8f);

        transform.localScale = new Vector3(2.8f, 2.8f, 1f);

        driver driverInstance = driver.Instance;
        if (driverInstance != null)
        {
            playerTransform = driverInstance.transform;
            playerDriver = driverInstance;
        }

        ConfigureVisuals();

        GameUIController ui = GameUIController.Instance;
        if (ui != null)
        {
            ui.ShowBossHealthBar(maxHealth);
            ui.UpdateBossHealthBar(maxHealth);
            ui.ShowWarning("O BARÃO AMARGO APARECEU!");
        }

        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayWarning();
        }

        lastAttackTime = Time.time + 1f; 
        lastSummonTime = Time.time + 2f;
    }

    private void ConfigureVisuals()
    {
        if (sr != null)
        {
            sr.sprite = GetBossSprite();
            sr.color = Color.white;
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        Vector3 targetCenter = playerTransform.position + playerTransform.up * 7f;
        float sway = Mathf.Sin(Time.time * 1.5f) * 3.5f;
        Vector3 targetPos = targetCenter + playerTransform.right * sway;

        transform.position = Vector3.Lerp(transform.position, targetPos, 4f * Time.deltaTime);
        transform.rotation = playerTransform.rotation;

        if (Time.time - lastAttackTime > attackInterval)
        {
            lastAttackTime = Time.time;
            ThrowSugarBomb();
        }

        if (Time.time - lastSummonTime > summonInterval)
        {
            lastSummonTime = Time.time;
            SummonMinions();
        }
    }

    private void ThrowSugarBomb()
    {
        GameObject bomb = new GameObject("SugarBombProjectable");
        bomb.transform.position = transform.position;
        bomb.transform.rotation = transform.rotation;

        SpriteRenderer bombSr = bomb.AddComponent<SpriteRenderer>();
        bombSr.sprite = GetWhiteSprite();
        bombSr.color = new Color(0.2f, 0.15f, 0.12f, 1f);
        bombSr.sortingOrder = 5;

        SugarBombMovement movement = bomb.AddComponent<SugarBombMovement>();
        movement.targetPos = playerTransform.position;
        movement.speed = 8f;
    }

    private void SummonMinions()
    {
        if (ObjectPoolManager.Instance == null) return;

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject minion = ObjectPoolManager.Instance.GetMonster();
            Vector3 spawnPos = transform.position + playerTransform.right * (i * 2f) - playerTransform.up * 1f;
            minion.transform.position = spawnPos;

            Enemy script = minion.GetComponent<Enemy>();
            if (script != null)
            {
                script.Initialize(i == -1 ? Enemy.EnemyType.Fast : Enemy.EnemyType.Standard);
            }
            minion.SetActive(true);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        GameUIController ui = GameUIController.Instance;
        if (ui != null)
        {
            ui.UpdateBossHealthBar(currentHealth);
        }

        StartCoroutine(FlashWhiteCoroutine());

        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayHit();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (playerDriver != null)
        {
            playerDriver.score += 200;
            playerDriver.ActivateGirlPower(10f);
        }

        GameUIController ui = GameUIController.Instance;
        if (ui != null)
        {
            ui.HideBossHealthBar();
            ui.ShowDeliveryMessage("BARÃO AMARGO DERROTADO! +200 PTS / GIRL POWER 10s!");
            ui.ShowVictory();
        }

        if (GameCameraController.Instance != null)
        {
            GameCameraController.Instance.TriggerShake(0.6f, 0.35f);
        }

        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayExplosion();
        }

        SpawnDefeatExplosion();

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnBossDefeated();
        }

        Destroy(gameObject);
    }

    private void SpawnDefeatExplosion()
    {
        Color greyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        Color orangeColor = new Color(1f, 0.5f, 0f, 1f);

        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 24; i++)
        {
            GameObject p = new GameObject("BossExplosionParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 24f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = whiteSprite;
            particleSr.color = (i % 2 == 0) ? greyColor : orangeColor;
            particleSr.sortingOrder = 5;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(4f, 9f);
        }
    }

    private IEnumerator FlashWhiteCoroutine()
    {
        if (sr == null) yield break;

        Color originalColor = sr.color;
        sr.color = Color.red; 

        yield return new WaitForSeconds(0.1f);

        if (sr != null)
        {
            sr.color = originalColor;
        }
    }
}

public class SugarBombMovement : MonoBehaviour
{
    public Vector3 targetPos;
    public float speed;
    private float flightDuration = 0.8f;
    private float spawnTime;

    // Static Cache for white sprite
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
        spawnTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - spawnTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        transform.position = Vector3.Lerp(transform.position, targetPos, t);

        if (t >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        GameObject puddle = new GameObject("SlowZonePuddle");
        puddle.transform.position = transform.position;
        puddle.AddComponent<SlowZone>();

        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 6; i++)
        {
            GameObject p = new GameObject("SplashParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 6f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = whiteSprite;
            particleSr.color = new Color(0.2f, 0.15f, 0.12f, 1f);
            particleSr.sortingOrder = 5;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(2f, 4f);
        }

        Destroy(gameObject);
    }
}
