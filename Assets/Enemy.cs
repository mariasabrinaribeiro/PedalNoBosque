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

    [SerializeField] private int collisionDamage = 20;
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

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        DriverPro player = FindObjectOfType<DriverPro>();
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

        DriverPro player = FindObjectOfType<DriverPro>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Configure stats, scale, and color tints for each variation
        switch (enemyType)
        {
            case EnemyType.Standard:
                maxHealth = 50;
                chaseSpeed = 1.5f;
                scoreReward = 2;
                healthReward = 15;
                collisionDamage = 20;
                baseScale = 1.0f;
                if (sr != null) sr.color = new Color(1f, 0.35f, 0.35f, 1f); // Red
                break;
            case EnemyType.Slow:
                maxHealth = 150;
                chaseSpeed = 0.6f;
                scoreReward = 5;
                healthReward = 25;
                collisionDamage = 30;
                baseScale = 1.5f;
                if (sr != null) sr.color = new Color(0.4f, 0.15f, 0.8f, 1f); // Deep Violet
                break;
            case EnemyType.Fast:
                maxHealth = 15;
                chaseSpeed = 3.2f;
                scoreReward = 2;
                healthReward = 10;
                collisionDamage = 15;
                baseScale = 0.7f;
                if (sr != null) sr.color = new Color(0.2f, 0.9f, 0.2f, 1f); // Neon Green
                break;
            case EnemyType.Explosive:
                maxHealth = 40;
                chaseSpeed = 2.0f;
                scoreReward = 2;
                healthReward = 15;
                collisionDamage = 35; // high damage on self-destruct
                baseScale = 1.0f;
                if (sr != null) sr.color = new Color(1f, 0.5f, 0.1f, 1f); // Orange-Red
                break;
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
                    
                    GameUIController ui = FindObjectOfType<GameUIController>();
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
            GameUIController ui = FindObjectOfType<GameUIController>();
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
        Color candyColor = new Color(1f, 0.482f, 0.773f, 1f);
        Vector2 splashDir = -hitDirection.normalized;

        for (int i = 0; i < 6; i++)
        {
            GameObject p = new GameObject("HitParticle");
            p.transform.position = pos;

            float spreadAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            Vector2 particleDir = RotateVector(splashDir, spreadAngle);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateParticleSprite(6, 6, (i % 2 == 0) ? candyColor : Color.white);
            particleSr.sortingOrder = 6;

            ExplosionParticle particleScript = p.AddComponent<ExplosionParticle>();
            particleScript.direction = particleDir;
            particleScript.speed = Random.Range(4f, 8f);
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
            DriverPro player = FindObjectOfType<DriverPro>();
            if (player != null)
            {
                player.score += scoreReward;
                player.health = Mathf.Min(player.health + healthReward, player.maxHealth);
                
                GameUIController ui = FindObjectOfType<GameUIController>();
                if (ui != null)
                {
                    ui.ShowDeliveryMessage($"MONSTRO DESTRUÍDO! +{scoreReward} PTS / +{healthReward} VIDA");
                }

                // Notify mission manager of kill
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.OnMonsterDefeated();
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
        Color candyPink = new Color(1f, 0.482f, 0.773f, 1f);
        Color candyPurple = new Color(0.368f, 0.235f, 0.502f, 1f);
        int count = large ? 16 : 8;

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
            particleSr.sortingOrder = 5;
            
            ExplosionParticle particleScript = p.AddComponent<ExplosionParticle>();
            particleScript.direction = dir;
            particleScript.speed = Random.Range(3f, large ? 8f : 6f);
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

