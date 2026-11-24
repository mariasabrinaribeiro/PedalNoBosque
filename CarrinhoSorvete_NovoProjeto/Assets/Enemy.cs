using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType { Standard, Slow, Fast, Explosive }
    public EnemyType enemyType = EnemyType.Standard;

    [Header("Stats")]
    public int maxHealth = 50;
    public int currentHealth;
    public float chaseSpeed = 1.5f;

    [SerializeField] private int collisionDamage = 10; // Reduzido de 20 para 10 para tornar o jogo mais fácil
    [SerializeField] private int scoreReward = 10;
    [SerializeField] private int healthReward = 15;

    private Transform playerTransform;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private float baseScale = 1.0f;
    private bool isKnockedBack = false; 
    private bool isExploding = false;
    private static Sprite cachedPinkParticleSprite;
    private static Sprite cachedPurpleParticleSprite;
    private static Sprite[] cachedSourSprites;

    private static Sprite GetSourCandySprite(EnemyType type)
    {
        if (cachedSourSprites == null) cachedSourSprites = new Sprite[4];
        int idx = (int)type;
        if (cachedSourSprites[idx] != null) return cachedSourSprites[idx];

        const int S = 48;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 ink = new Color32(48, 22, 54, 255);
        Color32 white = new Color32(255, 252, 245, 255);
        Color32 yellow = new Color32(255, 236, 92, 255);
        Color32 body, hi, wrapper;

        switch (type)
        {
            case EnemyType.Slow:
                body = new Color32(118, 52, 168, 255);    // Ameixa Amarga
                hi = new Color32(168, 102, 214, 255);
                wrapper = new Color32(84, 34, 128, 255);
                break;
            case EnemyType.Fast:
                body = new Color32(132, 218, 58, 255);    // Limão Azedo Veloz
                hi = new Color32(204, 248, 114, 255);
                wrapper = new Color32(72, 162, 42, 255);
                break;
            case EnemyType.Explosive:
                body = new Color32(246, 108, 42, 255);    // Bala Pimenta Explosiva
                hi = new Color32(255, 178, 96, 255);
                wrapper = new Color32(204, 52, 34, 255);
                break;
            default:
                body = new Color32(236, 68, 104, 255);    // Framboesa Azeda
                hi = new Color32(255, 138, 164, 255);
                wrapper = new Color32(182, 36, 78, 255);
                break;
        }

        Color32[] px = new Color32[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        void SetPx(int x, int y, Color32 c)
        {
            if (x >= 0 && x < S && y >= 0 && y < S) px[y * S + x] = c;
        }

        void FillCircle(float cx, float cy, float r, Color32 c)
        {
            float r2 = r * r;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) SetPx(x, y, c);
        }

        // Pontas de papel de bala torcido nas laterais!
        for (int y = 14; y <= 34; y++)
        {
            int dy = Mathf.Abs(y - 24);
            for (int x = 2; x <= 10; x++)
                if (dy <= (10 - x) + 3) SetPx(x, y, (x == 2 || dy == (10 - x) + 3) ? ink : wrapper);
            for (int x = 38; x <= 45; x++)
                if (dy <= (x - 38) + 3) SetPx(x, y, (x == 45 || dy == (x - 38) + 3) ? ink : wrapper);
        }

        // Corpo gordinho da bala azeda com contorno pixel-art e brilho
        FillCircle(24, 23, 16.5f, ink);
        FillCircle(24, 23, 14.0f, body);
        FillCircle(19, 30, 4.2f, hi);

        // Pavio aceso se for Explosivo, ou topete azedo nos outros!
        if (type == EnemyType.Explosive)
        {
            for (int y = 38; y <= 44; y++) { SetPx(23, y, ink); SetPx(24, y, ink); }
            FillCircle(24, 44, 3.2f, yellow);
        }
        else
        {
            FillCircle(24, 39, 3.5f, ink);
            FillCircle(24, 39, 2.0f, hi);
        }

        // Olhos brancos com pupila zangada
        for (int y = 22; y <= 27; y++)
        {
            for (int x = 14; x <= 20; x++) SetPx(x, y, white);
            for (int x = 28; x <= 34; x++) SetPx(x, y, white);
        }
        for (int y = 23; y <= 25; y++)
        {
            for (int x = 17; x <= 19; x++) SetPx(x, y, ink);
            for (int x = 29; x <= 31; x++) SetPx(x, y, ink);
        }

        // Sobrancelhas grossas emburradas em V (deixa claro na hora que é o vilão!)
        for (int i = 0; i < 8; i++)
        {
            int yL = 30 - i / 2;
            SetPx(13 + i, yL, ink); SetPx(13 + i, yL - 1, ink);
            SetPx(35 - i, yL, ink); SetPx(35 - i, yL - 1, ink);
        }

        // Boca azeda emburrada em zigue-zague!
        for (int x = 16; x <= 32; x++)
        {
            int zig = ((x / 3) % 2 == 0) ? 15 : 17;
            SetPx(x, zig, ink);
            SetPx(x, zig - 1, ink);
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedSourSprites[idx] = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 32f);
        return cachedSourSprites[idx];
    }

    public static readonly System.Collections.Generic.List<Enemy> ActiveEnemies = new System.Collections.Generic.List<Enemy>();

    void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    void OnDestroy()
    {
        ActiveEnemies.Remove(this);
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "EntitiesAndPlayer";
            sr.sortingOrder = 5;
        }
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        DriverPro player = DriverPro.Instance;
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Configure default standard stats if spawned manually without Initialize
        if (currentHealth <= 0)
        {
            Initialize(EnemyType.Standard);
        }
    }

    public void Initialize(EnemyType type)
    {
        enemyType = type;
        isKnockedBack = false;
        isExploding = false;

        StopAllCoroutines();

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        DriverPro player = DriverPro.Instance;
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Visual caricaturado de doce azedo/amargo com cara emburrada (contraste imediato com o carrinho fofo!)
        switch (enemyType)
        {
            case EnemyType.Standard:
                maxHealth = 50;
                chaseSpeed = 1.5f;
                scoreReward = 2;
                healthReward = 15;
                collisionDamage = 20;
                baseScale = 1.05f;
                break;
            case EnemyType.Slow:
                maxHealth = 150;
                chaseSpeed = 0.6f;
                scoreReward = 5;
                healthReward = 25;
                collisionDamage = 30;
                baseScale = 1.32f;
                break;
            case EnemyType.Fast:
                maxHealth = 15;
                chaseSpeed = 3.2f;
                scoreReward = 2;
                healthReward = 10;
                collisionDamage = 15;
                baseScale = 0.88f;
                break;
            case EnemyType.Explosive:
                maxHealth = 40;
                chaseSpeed = 2.0f;
                scoreReward = 2;
                healthReward = 15;
                collisionDamage = 35;
                baseScale = 1.08f;
                break;
        }

        if (sr != null)
        {
            sr.sprite = GetSourCandySprite(enemyType);
            sr.color = Color.white;
            sr.sortingLayerName = "EntitiesAndPlayer";
            sr.sortingOrder = 6;
        }

        currentHealth = maxHealth;
        transform.localScale = new Vector3(baseScale, baseScale, 1f);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.drag = 5f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void Update()
    {
        if (Time.timeScale < 0.01f) return;

        // Pulsate scale dynamically based on base scale to represent life
        float pulse = Mathf.PingPong(Time.time * 3.5f, 0.2f) + 0.9f;
        transform.localScale = new Vector3(pulse * baseScale, pulse * baseScale, 1f);

        // Explosive monster proximity checks
        if (enemyType == EnemyType.Explosive && !isExploding && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            // Trigger countdown when within 1.3 units of player car
            if (dist < 1.3f)
            {
                StartCoroutine(ExplosionSequenceCoroutine());
            }
        }
    }

    void FixedUpdate()
    {
        if (Time.timeScale < 0.01f) return;

        if (rb != null && playerTransform != null)
        {
            // Only chase if not knocked back and not currently exploding/fuse counting
            if (!isKnockedBack && !isExploding)
            {
                Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                rb.velocity = direction * chaseSpeed;
            }
        }
    }

    private IEnumerator ExplosionSequenceCoroutine()
    {
        isExploding = true;

        if (rb != null)
        {
            rb.velocity = Vector2.zero; // Freeze in place during countdown
        }

        float duration = 0.45f;
        float elapsed = 0f;
        bool toggleColor = false;

        // Flash orange-red and yellow rapidly to warn player
        while (elapsed < duration)
        {
            if (sr != null)
            {
                sr.color = toggleColor ? Color.yellow : new Color(1f, 0.2f, 0f, 1f);
            }
            toggleColor = !toggleColor;
            
            float waitTime = 0.05f;
            elapsed += waitTime;
            yield return new WaitForSeconds(waitTime);
        }

        // Detonate!
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            // Check if player is inside the area-of-effect radius (2.5 units)
            if (dist <= 2.5f)
            {
                DriverPro playerScript = playerTransform.GetComponent<DriverPro>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(collisionDamage);
                    
                    GameUIController ui = GameUIController.Instance;
                    if (ui != null)
                    {
                        ui.ShowWarning($"EXPLOSÃO DO MONSTRO! -{collisionDamage} VIDA");
                    }
                }
            }
        }

        // Trigger explosion effects
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayExplosion();
        }

        if (GameCameraController.Instance != null)
        {
            GameCameraController.Instance.TriggerShake(0.25f, 0.2f);
        }

        SpawnExplosionEffect(transform.position, true); // large burst
        
        // Return to pool
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isExploding) return;

        // Check collision with the car
        DriverPro player = collision.GetComponent<DriverPro>();
        if (player != null && gameObject.activeInHierarchy)
        {
            player.TakeDamage(collisionDamage);
            
            // Spawn warning text on HUD
            GameUIController ui = GameUIController.Instance;
            if (ui != null)
            {
                ui.ShowWarning($"BATEU NO MONSTRO! -{collisionDamage} VIDA");
            }

            Die(false); // return to pool without rewards
        }
    }

    public void TakeDamage(int damage)
    {
        Vector2 defaultDir = Vector2.zero;
        if (playerTransform != null)
        {
            defaultDir = (transform.position - playerTransform.position).normalized;
        }
        TakeDamage(damage, defaultDir);
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isExploding) return;

        currentHealth -= damage;

        if (rb != null)
        {
            // Zero velocity to avoid slides
            rb.velocity = Vector2.zero;

            // Trigger knockback coroutine
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(KnockbackCoroutine(rb, hitDirection));
            }
        }

        // Damage flash
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashWhiteCoroutine());
        }

        // Particle splash
        if (hitDirection != Vector2.zero)
        {
            SpawnHitParticles(transform.position, hitDirection);
        }

        if (currentHealth <= 0)
        {
            Die(true); // destroy with rewards
        }
    }

    private IEnumerator KnockbackCoroutine(Rigidbody2D rb, Vector2 hitDirection)
    {
        isKnockedBack = true;

        if (hitDirection == Vector2.zero && playerTransform != null)
        {
            hitDirection = (transform.position - playerTransform.position).normalized;
        }

        float knockbackForce = 6.5f;
        rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.15f);

        if (rb != null)
        {
            rb.velocity = Vector2.zero; // stop physically
        }

        isKnockedBack = false;
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

    private void SpawnHitParticles(Vector3 pos, Vector2 hitDirection)
    {
        // ETAPA 4: Redução de 40% na emissão (6 -> 4) e opacidade máxima 35% nas partículas rosas
        Color candyColor = new Color(1f, 0.482f, 0.773f, 0.35f);
        Color softWhite = new Color(1f, 1f, 1f, 0.35f);
        Vector2 splashDir = -hitDirection.normalized;

        for (int i = 0; i < 4; i++)
        {
            GameObject p = new GameObject("HitParticle");
            p.transform.position = pos;

            float spreadAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            Vector2 particleDir = RotateVector(splashDir, spreadAngle);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateParticleSprite(6, 6, (i % 2 == 0) ? candyColor : softWhite);
            particleSr.sortingLayerName = "VFX";
            particleSr.sortingOrder = 6;

            ExplosionParticle particleScript = p.AddComponent<ExplosionParticle>();
            particleScript.direction = particleDir;
            particleScript.speed = Random.Range(4f, 7f);
        }
    }

    private Vector2 RotateVector(Vector2 v, float delta)
    {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    private void Die(bool giveRewards)
    {
        if (giveRewards)
        {
            DriverPro player = DriverPro.Instance;
            if (player != null)
            {
                // ETAPA 4: Pontuação balanceada (+50 pts por inimigo derrotado)
                if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(ControladorJogo.PontosPorInimigo);
                else player.score += ControladorJogo.PontosPorInimigo;
                player.health = Mathf.Min(player.health + healthReward, player.maxHealth);

                GameUIController ui = GameUIController.Instance;
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("MONSTRO DESTRUÍDO! +" + ControladorJogo.PontosPorInimigo + " PTS • +" + healthReward + " VIDA");
                }

                // Notify mission manager of kill
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.OnMonsterDefeated();
                }

                // Toda vez que mata um inimigo, aumenta a chance de ganhar dando 20% de chance de dropar um PowerUp (incluindo a Bomba!)
                if (Random.value < 0.20f)
                {
                    GameObject pUpObj = new GameObject("PowerUpItem");
                    PowerUpItem script = pUpObj.AddComponent<PowerUpItem>();
                    int randType = Random.Range(0, 4); // Escolhe entre Magnet, Shield, TripleShot e Bomb
                    script.Initialize((PowerUpItem.PowerUpType)randType, transform.position);
                }
            }
        }

        // Sound feedback
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayExplosion();
        }

        SpawnExplosionEffect(transform.position, false);
        
        // Return to pool instead of destroying
        gameObject.SetActive(false);
    }

    private void SpawnExplosionEffect(Vector3 pos, bool large)
    {
        // ETAPA 4: Redução de 40% na emissão (16->10 / 8->5) e opacidade máxima 35% nas partículas rosas/roxas
        Color candyPink = new Color(1f, 0.482f, 0.773f, 0.35f);
        Color candyPurple = new Color(0.368f, 0.235f, 0.502f, 0.35f);
        int count = large ? 10 : 5;

        if (cachedPinkParticleSprite == null)
        {
            cachedPinkParticleSprite = CreateParticleSprite(8, 8, candyPink);
        }
        if (cachedPurpleParticleSprite == null)
        {
            cachedPurpleParticleSprite = CreateParticleSprite(8, 8, candyPurple);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject p = new GameObject("ExplosionParticle");
            p.transform.position = pos;

            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = (i % 2 == 0) ? cachedPinkParticleSprite : cachedPurpleParticleSprite;
            particleSr.sortingLayerName = "VFX";
            particleSr.sortingOrder = 5;

            ExplosionParticle particleScript = p.AddComponent<ExplosionParticle>();
            particleScript.direction = dir;
            particleScript.speed = Random.Range(3f, large ? 7f : 5.5f);
        }
    }

    private Sprite CreateParticleSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - w / 2f;
                float dy = y - h / 2f;
                if (dx * dx + dy * dy <= (w / 2f) * (w / 2f))
                {
                    tex.SetPixel(x, y, color);
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

public class ExplosionParticle : MonoBehaviour
{
    public Vector3 direction;
    public float speed;
    private float fadeSpeed = 3f;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        Destroy(gameObject, 0.5f); // auto destroy after half a second
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        
        // Fade out
        if (sr != null)
        {
            Color c = sr.color;
            c.a -= fadeSpeed * Time.deltaTime;
            sr.color = c;
        }
    }
}

