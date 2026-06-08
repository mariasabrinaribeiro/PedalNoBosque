using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 6;
    [SerializeField] private float minSpawnDist = 8f;
    [SerializeField] private float maxSpawnDist = 16f;

    private driverpro player;
    private float nextSpawnTime;
    private Sprite enemySprite;

    private void Awake()
    {
        // Copy the monster sprite to Resources folder dynamically in editor if not present
#if UNITY_EDITOR
        string resourcesDir = Application.dataPath + "/Resources";
        if (!System.IO.Directory.Exists(resourcesDir))
        {
            System.IO.Directory.CreateDirectory(resourcesDir);
        }
        string srcPath = Application.dataPath + "/GameDev.tv - Delivery Dash Assets/Props & Stuff/monster.png";
        string destPath = resourcesDir + "/monster.png";
        if (System.IO.File.Exists(srcPath) && !System.IO.File.Exists(destPath))
        {
            System.IO.File.Copy(srcPath, destPath);
            AssetDatabase.Refresh();
        }
#endif
    }

    void Start()
    {
        player = FindObjectOfType<driverpro>();
        nextSpawnTime = Time.time + 1f;

        // Load the sprite from Resources
        enemySprite = Resources.Load<Sprite>("monster");
        if (enemySprite == null)
        {
            Debug.LogWarning("Monster sprite not found in Resources. Will generate a placeholder slime color.");
        }
    }

    void Update()
    {
        if (Time.timeScale < 0.01f || player == null) return;

        // Count current active enemies
        int activeEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // Spawn enemy if below limit
        if (activeEnemies < maxEnemies && Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            SpawnEnemyAhead();
        }
    }

    private void SpawnEnemyAhead()
    {
        // Calculate spawn position ahead of the player in the direction they are facing
        float distance = Random.Range(minSpawnDist, maxSpawnDist);
        float sideOffset = Random.Range(-4f, 4f); // Spawn randomly across road width

        Vector3 spawnPos = player.transform.position + player.transform.up * distance + player.transform.right * sideOffset;

        // Check if overlaps with obstacles or other colliders
        Collider2D overlap = Physics2D.OverlapCircle(spawnPos, 1f);
        if (overlap != null && !overlap.CompareTag("Player") && !overlap.CompareTag("package") && !overlap.CompareTag("Customer"))
        {
            // If it spawned inside a building or rock, skip this spawn to prevent getting stuck
            return;
        }

        // Spawn enemy GameObject
        GameObject enemyObj = new GameObject("MonsterEnemy");
        enemyObj.tag = "Enemy"; // Set tag to count them
        enemyObj.transform.position = spawnPos;

        SpriteRenderer sr = enemyObj.AddComponent<SpriteRenderer>();
        if (enemySprite != null)
        {
            sr.sprite = enemySprite;
        }
        else
        {
            // Fallback placeholder
            sr.sprite = CreatePlaceholderSprite(32, 32, new Color(0.5f, 0.1f, 0.5f, 1f));
        }
        sr.sortingOrder = 3;

        // Add 2D collider
        CircleCollider2D col = enemyObj.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        // Add Enemy script
        enemyObj.AddComponent<Enemy>();
    }

    private Sprite CreatePlaceholderSprite(int w, int h, Color color)
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
