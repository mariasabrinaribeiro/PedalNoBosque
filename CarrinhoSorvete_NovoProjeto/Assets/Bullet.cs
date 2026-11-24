using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 2f;
    public Vector3 direction;
    public bool isGirlPower = false;
    public bool isFragment = false;

    private float spawnTime;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
    }

    void OnEnable()
    {
        spawnTime = Time.time;
    }

    public void Initialize(Vector3 dir, float spd, bool girlPower)
    {
        direction = dir.normalized;
        speed = spd;
        isGirlPower = girlPower;
        isFragment = false;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (isGirlPower)
            {
                // Glowing golden star
                sr.sprite = CreateStarSprite(24, 24, new Color(1f, 0.85f, 0.1f, 1f));
                transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            }
            else
            {
                // Candy pink circle
                sr.sprite = CreateBulletSprite(16, 16, new Color(1f, 0.482f, 0.773f, 1f));
                transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            }
        }
    }

    void Update()
    {
        // Homing behavior for bubble fragments
        if (isFragment && (Time.time - spawnTime >= 0.25f)) // 0.25s of spreading out first
        {
            Enemy nearest = FindNearestEnemy();
            if (nearest != null)
            {
                Vector3 targetDir = (nearest.transform.position - transform.position).normalized;
                // Rotate direction towards targetDir smoothly
                float homingStrength = 8f; // controls how sharp it turns
                direction = Vector3.RotateTowards(direction, targetDir, homingStrength * Time.deltaTime, 0f).normalized;
            }
        }

        // Move forward in 2D space
        transform.position += direction * speed * Time.deltaTime;

        // Return to pool after lifetime expires
        if (Time.time - spawnTime >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    private Enemy FindNearestEnemy()
    {
        Enemy nearest = null;
        float minDist = float.MaxValue;
        Vector3 myPos = transform.position;

        for (int i = Enemy.ActiveEnemies.Count - 1; i >= 0; i--)
        {
            Enemy e = Enemy.ActiveEnemies[i];
            if (e == null)
            {
                Enemy.ActiveEnemies.RemoveAt(i);
                continue;
            }
            if (e.gameObject.activeInHierarchy)
            {
                float dist = Vector3.Distance(myPos, e.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = e;
                }
            }
        }
        return nearest;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Check if hit Boss
        BossBitterBaron boss = collision.GetComponent<BossBitterBaron>();
        if (boss != null)
        {
            int damage = isGirlPower ? 500 : 250;

            // 30% chance of critical hit
            bool isCritical = Random.value < 0.3f;
            if (isCritical)
            {
                damage *= 2;
                if (GameCameraController.Instance != null)
                {
                    GameCameraController.Instance.TriggerShake(0.15f, 0.25f);
                }
                GameUIController ui = GameUIController.Instance;
                if (ui != null)
                {
                    ui.ShowWarning("TIRO CRÍTICO!");
                }
            }

            boss.TakeDamage(damage);
            gameObject.SetActive(false); // return to pool
            return;
        }

        // 2. Check if hit enemy
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            // Rifle mode: Instant kill
            int damage = isFragment ? 500 : 1000;

            enemy.TakeDamage(damage, direction); // deal damage to enemy and apply knockback

            // Spawn bubble fragments if it is a main bullet
            if (!isFragment)
            {
                SpawnFragments(transform.position);
            }

            gameObject.SetActive(false); // return bullet to pool
        }
        else if (collision.CompareTag("Obstacle") || collision.name.Contains("fence") || collision.name.Contains("tree") || collision.name.Contains("rock"))
        {
            // Return bullet to pool on obstacle impact
            gameObject.SetActive(false);
        }
    }

    private Sprite CreateBulletSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - w / 2f;
                float dy = y - h / 2f;
                float distSq = dx * dx + dy * dy;
                float rOuter = w / 2f;
                float rInner = rOuter - 2f;

                if (distSq <= rOuter * rOuter)
                {
                    if (distSq > rInner * rInner)
                    {
                        tex.SetPixel(x, y, new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f));
                    }
                    else
                    {
                        // soft white highlight in center for emulated bloom
                        if (distSq <= 1.5f * 1.5f)
                        {
                            tex.SetPixel(x, y, Color.white);
                        }
                        else
                        {
                            tex.SetPixel(x, y, color);
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

    private Sprite CreateStarSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w / 2f);
                float dy = Mathf.Abs(y - h / 2f);

                // Diamond/star cross outline
                if (dx + dy <= w / 2f + 1f)
                {
                    // soft white glow center
                    if (dx + dy <= 3f)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        tex.SetPixel(x, y, color);
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

    public void InitializeFragment(Vector3 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        isGirlPower = false;
        isFragment = true;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "VFX";
            sr.sortingOrder = 5;
            // Smaller light-blue bubble sprite
            sr.sprite = CreateBulletSprite(10, 10, new Color(0.4f, 0.75f, 1f, 0.35f));
            transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
    }

    private void SpawnFragments(Vector3 pos)
    {
        if (ObjectPoolManager.Instance == null) return;

        // ETAPA 4: -40% emissão de fragmentos (6 -> 4)
        int numFragments = 4;
        for (int i = 0; i < numFragments; i++)
        {
            GameObject bulletGo = ObjectPoolManager.Instance.GetBullet();
            if (bulletGo == null) continue;

            bulletGo.transform.position = pos;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 fragDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            Bullet bulletScript = bulletGo.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.InitializeFragment(fragDir, speed * 0.8f);
            }

            bulletGo.SetActive(true);
        }
    }
}
