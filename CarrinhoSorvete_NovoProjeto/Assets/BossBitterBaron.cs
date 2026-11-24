using System.Collections;
using UnityEngine;

public class BossBitterBaron : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 2500;
    public int currentHealth;

    private Transform playerTransform;
    private DriverPro playerDriver; 
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
        if (cachedBossSprite != null) return cachedBossSprite;
        // Chefão caricaturado "Barão Amargo": trufa de chocolate amargo gigante com coroa dourada,
        // sobrancelhas grossas emburradas, olhos verde-azedos e bigode rabugento!
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 ink = new Color32(36, 18, 40, 255);
        Color32 choco = new Color32(82, 46, 56, 255);
        Color32 chocoHi = new Color32(126, 78, 88, 255);
        Color32 gold = new Color32(255, 212, 68, 255);
        Color32 ruby = new Color32(236, 56, 102, 255);
        Color32 sourEye = new Color32(196, 255, 84, 255);
        Color32 stache = new Color32(52, 26, 36, 255);

        Color32[] px = new Color32[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        void SetPx(int x, int y, Color32 c) { if (x >= 0 && x < S && y >= 0 && y < S) px[y * S + x] = c; }
        void FillCircle(float cx, float cy, float r, Color32 c)
        {
            float r2 = r * r;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) SetPx(x, y, c);
        }

        // Corpo redondo de trufa amarga com capa escura
        FillCircle(32, 26, 23f, ink);
        FillCircle(32, 26, 20f, choco);
        FillCircle(24, 34, 6f, chocoHi);

        // Coroa dourada torta de vilão no topo!
        for (int y = 45; y <= 57; y++)
        {
            for (int x = 18; x <= 46; x++)
            {
                bool spire = (x <= 24 && y <= 57 - (x - 18)) ||
                             (x >= 28 && x <= 36 && y <= 58 - Mathf.Abs(x - 32)) ||
                             (x >= 40 && y <= 57 - (46 - x)) ||
                             (y <= 50);
                if (!spire) continue;
                bool border = (y == 45 || x == 18 || x == 46 || y >= 55);
                SetPx(x, y, border ? ink : gold);
            }
        }
        FillCircle(32, 49, 2.4f, ruby);

        // Olhos verde-azedos brilhantes + pupilas zangadas
        for (int y = 26; y <= 33; y++)
        {
            for (int x = 18; x <= 26; x++) SetPx(x, y, sourEye);
            for (int x = 38; x <= 46; x++) SetPx(x, y, sourEye);
        }
        for (int y = 28; y <= 31; y++)
        {
            for (int x = 22; x <= 25; x++) SetPx(x, y, ink);
            for (int x = 39; x <= 42; x++) SetPx(x, y, ink);
        }

        // Sobrancelhas grossas emburradas em V
        for (int i = 0; i < 11; i++)
        {
            int y = 37 - i / 2;
            SetPx(16 + i, y, ink); SetPx(16 + i, y - 1, ink); SetPx(16 + i, y - 2, ink);
            SetPx(48 - i, y, ink); SetPx(48 - i, y - 1, ink); SetPx(48 - i, y - 2, ink);
        }

        // Bigodão rabugento curvado + boca emburrada
        for (int x = 14; x <= 50; x++)
        {
            int dx = Mathf.Abs(x - 32);
            int my = 18 + (dx < 8 ? 3 : (dx < 15 ? 1 : 3));
            for (int dy = -2; dy <= 2; dy++) SetPx(x, my + dy, (Mathf.Abs(dy) == 2) ? ink : stache);
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedBossSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
        return cachedBossSprite;
    }

    void Start()
    {
        currentHealth = maxHealth;
        isDead = false;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "EntitiesAndPlayer";
        sr.sortingOrder = 8;

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        gameObject.name = "Barão Amargo";

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 1.6f);

        transform.localScale = new Vector3(1.85f, 1.85f, 1f);

        DriverPro driverInstance = DriverPro.Instance;
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
            if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(ControladorJogo.PontosPorChefe);
            else playerDriver.score += ControladorJogo.PontosPorChefe;
            playerDriver.ActivateGirlPower(10f);
        }

        GameUIController ui = GameUIController.Instance;
        if (ui != null)
        {
            ui.HideBossHealthBar();
            ui.ShowDeliveryMessage("BARÃO AMARGO DERROTADO! +" + ControladorJogo.PontosPorChefe + " PTS • GIRL POWER!");
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
        Color greyColor = new Color(0.2f, 0.2f, 0.2f, 0.35f);
        Color orangeColor = new Color(1f, 0.5f, 0f, 0.35f);

        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 14; i++)
        {
            GameObject p = new GameObject("BossExplosionParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 14f) * Mathf.Deg2Rad;
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
