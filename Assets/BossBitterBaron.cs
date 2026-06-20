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

        driver driverInstance = FindObjectOfType<driver>();
        if (driverInstance != null)
        {
            playerTransform = driverInstance.transform;
            playerDriver = driverInstance;
        }

        ConfigureVisuals();

        // Show Boss Health Bar in HUD
        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null)
        {
            ui.ShowBossHealthBar(maxHealth);
            ui.UpdateBossHealthBar(maxHealth);
            ui.ShowWarning("O BARÃO AMARGO APARECEU!");
        }

        // Play Warning sound
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayWarning();
        }

        lastAttackTime = Time.time + 1f; // delay first attack
        lastSummonTime = Time.time + 2f;
    }

    private void ConfigureVisuals()
    {
        if (sr != null)
        {
            sr.sprite = CreateBossSprite();
            sr.color = Color.white;
        }
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // 1. Follow player position but stay 7 units ahead, swaying side-to-side
        Vector3 targetCenter = playerTransform.position + playerTransform.up * 7f;
        float sway = Mathf.Sin(Time.time * 1.5f) * 3.5f;
        Vector3 targetPos = targetCenter + playerTransform.right * sway;

        // Smoothly interpolate position to prevent jerking
        transform.position = Vector3.Lerp(transform.position, targetPos, 4f * Time.deltaTime);
        transform.rotation = playerTransform.rotation;

        // 2. Attack: Throw Sugar Bomb
        if (Time.time - lastAttackTime > attackInterval)
        {
            lastAttackTime = Time.time;
            ThrowSugarBomb();
        }

        // 3. Summon: Spawn helper minions
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
        bombSr.sprite = CreateBombSprite(16, 16, new Color(0.2f, 0.15f, 0.12f, 1f));
        bombSr.sortingOrder = 5;

        SugarBombMovement movement = bomb.AddComponent<SugarBombMovement>();
        movement.targetPos = playerTransform.position;
        movement.speed = 8f;
    }

    private void SummonMinions()
    {
        if (ObjectPoolManager.Instance == null) return;

        // Spawn 2 minions at sides
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject minion = ObjectPoolManager.Instance.GetMonster();
            // Position near boss sides
            Vector3 spawnPos = transform.position + playerTransform.right * (i * 2f) - playerTransform.up * 1f;
            minion.transform.position = spawnPos;

            Enemy script = minion.GetComponent<Enemy>();
            if (script != null)
            {
                // Alternates standard and fast
                script.Initialize(i == -1 ? Enemy.EnemyType.Fast : Enemy.EnemyType.Standard);
            }
            minion.SetActive(true);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        // Update health bar in HUD
        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null)
        {
            ui.UpdateBossHealthBar(currentHealth);
        }

        // Damage flash
        StartCoroutine(FlashWhiteCoroutine());

        // Sound feedback
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

        // Give player rewards
        if (playerDriver != null)
        {
            playerDriver.score += 200;
            // Activate Girl Power for 10 seconds!
            playerDriver.ActivateGirlPower(10f);
        }

        // Hide Boss Health Bar
        GameUIController ui = FindObjectOfType<GameUIController>();
        if (ui != null)
        {
            ui.HideBossHealthBar();
            ui.ShowDeliveryMessage("BARÃO AMARGO DERROTADO! +200 PTS / GIRL POWER 10s!");
            ui.ShowVictory();
        }

        // Trigger heavy camera shake
        if (GameCameraController.Instance != null)
        {
            GameCameraController.Instance.TriggerShake(0.6f, 0.35f);
        }

        // Trigger massive explosion sound
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayExplosion();
        }

        // Spawn massive particle burst
        SpawnDefeatExplosion();

        // Report to spawner that boss is dead
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

        for (int i = 0; i < 24; i++)
        {
            GameObject p = new GameObject("BossExplosionParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 24f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateBombSprite(8, 8, (i % 2 == 0) ? greyColor : orangeColor);
            particleSr.sortingOrder = 5;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(4f, 9f);
        }
    }

    private IEnumerator FlashWhiteCoroutine()
    {
        if (sr == null) yield break;

        Material originalMaterial = sr.material;
        Color originalColor = sr.color;

        Shader guiTextShader = Shader.Find("GUI/Text Shader");
        if (guiTextShader != null)
        {
            sr.material = new Material(guiTextShader);
        }
        sr.color = Color.white;

        yield return new WaitForSeconds(0.1f);

        if (sr != null)
        {
            sr.material = originalMaterial;
            sr.color = originalColor;
        }
    }

    private Sprite CreateBossSprite()
    {
        int w = 64;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color darkGrey = new Color(0.12f, 0.12f, 0.12f, 1f); // Black/Licorice
        Color metallicEdge = new Color(0.35f, 0.35f, 0.35f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Draw a metallic bevelled dark square
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
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateBombSprite(int w, int h, Color color)
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

public class SugarBombMovement : MonoBehaviour
{
    public Vector3 targetPos;
    public float speed;
    private float flightDuration = 0.8f;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - spawnTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        // Move to target using lerp
        transform.position = Vector3.Lerp(transform.position, targetPos, t);

        if (t >= 1.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Spawn slow zone puddle
        GameObject puddle = new GameObject("SlowZonePuddle");
        puddle.transform.position = transform.position;
        puddle.AddComponent<SlowZone>();

        // Spawn a burst of dark splash particles
        for (int i = 0; i < 6; i++)
        {
            GameObject p = new GameObject("SplashParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 6f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateTinySprite(4, 4, new Color(0.2f, 0.15f, 0.12f, 1f));
            particleSr.sortingOrder = 5;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(2f, 4f);
        }

        Destroy(gameObject);
    }

    private Sprite CreateTinySprite(int w, int h, Color color)
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
