using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 6f;
    [SerializeField] private int maxEnemies = 3;
    [SerializeField] private float minSpawnDist = 8f;
    [SerializeField] private float maxSpawnDist = 16f;

    private DriverPro player;
    private float nextSpawnTime;
    private Sprite enemySprite;

    // Power-up, Gate, and Boss spawn tracking
    private float powerUpSpawnInterval = 12f;
    private float nextPowerUpSpawnTime;

    private float gateSpawnInterval = 18f;
    private float nextGateSpawnTime;

    [HideInInspector] public bool bossActive = false;
    [HideInInspector] public int scoreForNextBoss = 1500;
    private GameObject activeBossInstance;

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

        // Dynamically instantiate ObjectPoolManager if it does not exist
        if (ObjectPoolManager.Instance == null)
        {
            GameObject poolObj = new GameObject("ObjectPoolManager");
            poolObj.AddComponent<ObjectPoolManager>();
        }
    }

    void Start()
    {
        player = FindObjectOfType<DriverPro>();
        nextSpawnTime = Time.time + 1.5f;

        nextPowerUpSpawnTime = Time.time + 5.0f;
        nextGateSpawnTime = Time.time + 9.0f;
        scoreForNextBoss = 1500;
        bossActive = false;

        // Load the sprite from Resources
        enemySprite = Resources.Load<Sprite>("monster");
    }

    void Update()
    {
        if (Time.timeScale < 0.01f || player == null) return;

        // 1. Boss Trigger Check - DISABLED to make game lighter
        /*
        if (player.score >= scoreForNextBoss && !bossActive)
        {
            SpawnBossFight();
        }
        */

        if (bossActive)
        {
            // Pause all normal spawns during boss fight
            return;
        }

        // 2. Spawn standard enemies
        int activeEnemies = 0;
        Enemy[] pooledMonsters = FindObjectsOfType<Enemy>();
        foreach (var m in pooledMonsters)
        {
            if (m.gameObject.activeInHierarchy) activeEnemies++;
        }

        if (activeEnemies < maxEnemies && Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            SpawnEnemyAhead();
        }

        // 3. Spawn power-ups on timer
        if (Time.time >= nextPowerUpSpawnTime)
        {
            nextPowerUpSpawnTime = Time.time + powerUpSpawnInterval;
            SpawnPowerUpAhead();
        }

        // 4. Spawn speed delivery gates on timer
        if (Time.time >= nextGateSpawnTime)
        {
            nextGateSpawnTime = Time.time + gateSpawnInterval;
            SpawnGateAhead();
        }
    }

    private void SpawnEnemyAhead()
    {
        if (ObjectPoolManager.Instance == null) return;

        Vector3 spawnPos = CalculateSpawnPosition(true); // random offset
        if (spawnPos == Vector3.zero) return;

        // Retrieve from pool
        GameObject enemyObj = ObjectPoolManager.Instance.GetMonster();
        enemyObj.transform.position = spawnPos;

        // Assign sprite renderer if standard sprite is present
        SpriteRenderer sr = enemyObj.GetComponent<SpriteRenderer>();
        if (sr != null && enemySprite != null)
        {
            sr.sprite = enemySprite;
        }

        // Decide random variation: Standard (40%), Slow (20%), Fast (20%), Explosive (20%)
        Enemy.EnemyType chosenType = Enemy.EnemyType.Standard;
        float rand = Random.value;
        if (rand < 0.4f) chosenType = Enemy.EnemyType.Standard;
        else if (rand < 0.6f) chosenType = Enemy.EnemyType.Slow;
        else if (rand < 0.8f) chosenType = Enemy.EnemyType.Fast;
        else chosenType = Enemy.EnemyType.Explosive;

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.Initialize(chosenType);
        }

        enemyObj.SetActive(true);
    }

    private void SpawnPowerUpAhead()
    {
        Vector3 spawnPos = CalculateSpawnPosition(true);
        if (spawnPos == Vector3.zero) return;

        GameObject pUpObj = new GameObject("PowerUpItem");
        PowerUpItem script = pUpObj.AddComponent<PowerUpItem>();
        
        // Randomly select type (Magnet, Shield, or TripleShot)
        int randType = Random.Range(0, 3);
        script.Initialize((PowerUpItem.PowerUpType)randType, spawnPos);
    }

    private void SpawnGateAhead()
    {
        if (ObjectPoolManager.Instance == null) return;

        Vector3 spawnPos = CalculateSpawnPosition(false); // No side offset (center on road)
        if (spawnPos == Vector3.zero) return;

        GameObject gateObj = ObjectPoolManager.Instance.GetGate();
        gateObj.transform.position = spawnPos;
        gateObj.transform.rotation = player.transform.rotation;
        gateObj.SetActive(true);
    }

    private void SpawnBossFight()
    {
        bossActive = true;

        // Clear all active enemies on screen
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (var e in enemies)
        {
            e.gameObject.SetActive(false);
        }

        // Clear active gates
        SpeedDeliveryGate[] gates = FindObjectsOfType<SpeedDeliveryGate>();
        foreach (var g in gates)
        {
            g.gameObject.SetActive(false);
        }

        // Instantiate Boss
        GameObject bossObj = new GameObject("Boss_BitterBaron");
        bossObj.transform.position = player.transform.position + player.transform.up * 8f;
        bossObj.transform.rotation = player.transform.rotation;
        bossObj.AddComponent<BossBitterBaron>();
        activeBossInstance = bossObj;
    }

    public void OnBossDefeated()
    {
        bossActive = false;
        // Schedule next boss fight after another 1500 points
        scoreForNextBoss = player.score + 1500;

        nextSpawnTime = Time.time + 3.0f;
        nextPowerUpSpawnTime = Time.time + 6.0f;
        nextGateSpawnTime = Time.time + 10.0f;
    }

    private Vector3 CalculateSpawnPosition(bool randomOffset)
    {
        float distance = Random.Range(minSpawnDist, maxSpawnDist);
        float sideOffset = randomOffset ? Random.Range(-3.5f, 3.5f) : 0f;

        Vector3 spawnPos = player.transform.position + player.transform.up * distance + player.transform.right * sideOffset;

        // Safety overlap check
        Collider2D overlap = Physics2D.OverlapCircle(spawnPos, 0.8f);
        if (overlap != null && !overlap.CompareTag("Player") && !overlap.CompareTag("package") && !overlap.CompareTag("Customer"))
        {
            // spawned inside obstacle, discard
            return Vector3.zero;
        }

        return spawnPos;
    }
}
