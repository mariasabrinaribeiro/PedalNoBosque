using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager _instance;
    public static ObjectPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectPoolManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ObjectPoolManager");
                    _instance = go.AddComponent<ObjectPoolManager>();
                }
            }
            return _instance;
        }
    }

    private List<GameObject> bulletPool = new List<GameObject>();
    private List<GameObject> monsterPool = new List<GameObject>();
    private List<GameObject> gatePool = new List<GameObject>();

    private int bulletPoolSize = 25;
    private int monsterPoolSize = 12;
    private int gatePoolSize = 3;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        PreWarmPools();
    }

    private void PreWarmPools()
    {
        // 1. Pre-warm bullet pool
        for (int i = 0; i < bulletPoolSize; i++)
        {
            GameObject bullet = CreateNewBulletInstance();
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }

        // 2. Pre-warm monster pool
        for (int i = 0; i < monsterPoolSize; i++)
        {
            GameObject monster = CreateNewMonsterInstance();
            monster.SetActive(false);
            monsterPool.Add(monster);
        }

        // 3. Pre-warm gate pool
        for (int i = 0; i < gatePoolSize; i++)
        {
            GameObject gate = CreateNewGateInstance();
            gate.SetActive(false);
            gatePool.Add(gate);
        }
    }

    private GameObject CreateNewBulletInstance()
    {
        GameObject bullet = new GameObject("Bullet_Pooled");
        bullet.transform.SetParent(transform);

        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "VFX";
        sr.sortingOrder = 5;

        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.15f;
        col.isTrigger = true;

        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        bullet.AddComponent<Bullet>();
        return bullet;
    }

    private GameObject CreateNewMonsterInstance()
    {
        GameObject monster = new GameObject("Monster_Pooled");
        monster.transform.SetParent(transform);
        monster.tag = "Enemy";

        SpriteRenderer sr = monster.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "EntitiesAndPlayer";
        sr.sortingOrder = 5;

        Rigidbody2D rb = monster.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.drag = 5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D col = monster.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;
        col.isTrigger = true;

        monster.AddComponent<Enemy>();
        return monster;
    }

    private GameObject CreateNewGateInstance()
    {
        GameObject gate = new GameObject("SpeedGate_Pooled");
        gate.transform.SetParent(transform);

        // Visual components are set up by SpeedDeliveryGate script
        gate.AddComponent<SpeedDeliveryGate>();
        return gate;
    }

    public GameObject GetBullet()
    {
        foreach (var b in bulletPool)
        {
            if (!b.activeInHierarchy) return b;
        }
        GameObject newB = CreateNewBulletInstance();
        newB.SetActive(false);
        bulletPool.Add(newB);
        return newB;
    }

    public GameObject GetMonster()
    {
        foreach (var m in monsterPool)
        {
            if (!m.activeInHierarchy) return m;
        }
        GameObject newM = CreateNewMonsterInstance();
        newM.SetActive(false);
        monsterPool.Add(newM);
        return newM;
    }

    public GameObject GetGate()
    {
        foreach (var g in gatePool)
        {
            if (!g.activeInHierarchy) return g;
        }
        GameObject newG = CreateNewGateInstance();
        newG.SetActive(false);
        gatePool.Add(newG);
        return newG;
    }
}
