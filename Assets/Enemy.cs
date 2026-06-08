using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;

    [SerializeField] private int collisionDamage = 20;
    [SerializeField] private int scoreReward = 10;
    [SerializeField] private int healthReward = 15;

    private Transform playerTransform;
    [SerializeField] private float chaseSpeed = 1.5f;

    void Start()
    {
        currentHealth = maxHealth;

        // Tint enemy red/orange so it stands out immediately
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        driverpro player = FindObjectOfType<driverpro>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Ensure it has a CircleCollider2D set as trigger
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = true;
    }

    void Update()
    {
        if (Time.timeScale < 0.01f) return;

        // 1. Slowly chase the player's car
        if (playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * chaseSpeed * Time.deltaTime;
        }

        // 2. Pulsate scale to indicate it is alive
        float pulse = Mathf.PingPong(Time.time * 3.5f, 0.2f) + 0.9f;
        transform.localScale = new Vector3(pulse, pulse, 1f);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check collision with the car
        driverpro player = collision.GetComponent<driverpro>();
        if (player != null)
        {
            player.TakeDamage(collisionDamage);
            
            // Spawn a visual effect or message
            GameUIController ui = FindObjectOfType<GameUIController>();
            if (ui != null)
            {
                ui.ShowWarning("BATEU NO MONSTRO! -20 VIDA");
            }

            Die(false); // destroy without rewards
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die(true); // destroy with rewards
        }
    }

    private void Die(bool giveRewards)
    {
        if (giveRewards)
        {
            driverpro player = FindObjectOfType<driverpro>();
            if (player != null)
            {
                player.score += scoreReward;
                player.health = Mathf.Min(player.health + healthReward, player.maxHealth);
                
                GameUIController ui = FindObjectOfType<GameUIController>();
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("MONSTRO DESTRUÍDO! +10 PTS / +15 VIDA");
                }
            }
        }

        // Play candy-themed explosion particles
        SpawnExplosionEffect(transform.position);

        Destroy(gameObject);
    }

    private void SpawnExplosionEffect(Vector3 pos)
    {
        Color candyPink = new Color(1f, 0.482f, 0.773f, 1f);
        Color candyPurple = new Color(0.368f, 0.235f, 0.502f, 1f);

        // Spawn 8 tiny candy particles flying outward
        for (int i = 0; i < 8; i++)
        {
            GameObject p = new GameObject("ExplosionParticle");
            p.transform.position = pos;
            
            float angle = i * (360f / 8f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            
            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = CreateParticleSprite(8, 8, (i % 2 == 0) ? candyPink : candyPurple);
            sr.sortingOrder = 5;
            
            ExplosionParticle particleScript = p.AddComponent<ExplosionParticle>();
            particleScript.direction = dir;
            particleScript.speed = Random.Range(3f, 6f);
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
