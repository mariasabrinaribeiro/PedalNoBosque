using UnityEngine;

/// <summary>
/// Entidades interativas do universo Girl Power Sweet World:
/// - MagicBubbleProjectile: bolha mágica rosa brilhante igual à da capa!
/// - SweetJellyEnemy: monstrinhos de gelatina (verde, rosa, roxo) quicando na estrada!
/// - SweetCollectible: cupcakes gigantes com cereja para coletar e entregar!
/// - GingerbreadDeliveryStop: casinha de biscoito de gengibre com cone gigante e comemoração!
/// - SweetRoadZone: faixas turbo arco-íris (SpeedGate) e poças pegajosas de caramelo (SlowZone)!
/// - SweetConfettiBurst: explosões leves de confetes estrelados!
/// </summary>
public sealed class MagicBubbleProjectile : MonoBehaviour
{
    public Vector2 velocity = new Vector2(15f, 0f);
    public float lifetime = 2.2f;
    private float age;

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime || transform.position.x < -11.5f || transform.position.x > 11.5f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += (Vector3)(velocity * Time.deltaTime);
        float pulse = 1f + 0.12f * Mathf.Sin(age * 24f);
        transform.localScale = Vector3.one * (0.62f * pulse);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SweetJellyEnemy enemy = other.GetComponent<SweetJellyEnemy>();
        if (enemy != null)
        {
            enemy.PopByBubble();
            SweetConfettiBurst.Spawn(transform.position, 8, new Color(1f, 0.35f, 0.82f, 1f));
            Destroy(gameObject);
        }
    }
}

public sealed class SweetJellyEnemy : MonoBehaviour
{
    public int hp = 1;
    public float extraSpeed = 1.6f;
    public Color splashColor = new Color(0.32f, 0.92f, 0.38f, 1f);
    private float baseY;
    private float phase;
    private Vector3 baseScale;
    private SpriteRenderer sr;
    private bool popped;

    private void Start()
    {
        baseY = transform.position.y;
        baseScale = transform.localScale;
        phase = Random.Range(0f, 6.28f);
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float scroll = SweetWorldGameManager.Instance != null ? SweetWorldGameManager.Instance.CurrentScrollSpeed : 6f;
        float x = transform.position.x + (scroll + extraSpeed) * Time.deltaTime;
        float hop = Mathf.Abs(Mathf.Sin(Time.time * 5.2f + phase));
        float y = baseY + hop * 0.42f;
        transform.position = new Vector3(x, y, 0f);

        // Squash & stretch fofo de gelatina!
        float squash = 1f + (0.5f - hop) * 0.16f;
        transform.localScale = new Vector3(baseScale.x * squash, baseScale.y / squash, 1f);

        if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-baseY * 100f);
        if (x > 12.5f) Destroy(gameObject);
    }

    public void PopByBubble()
    {
        if (popped) return;
        hp--;
        if (hp > 0)
        {
            if (sr != null) sr.color = new Color(1f, 0.65f, 0.85f, 1f);
            return;
        }
        popped = true;
        if (SweetWorldGameManager.Instance != null)
            SweetWorldGameManager.Instance.AddScore(50, "GELATINA ESTOURADA! +50", false);
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayPop();
        SweetConfettiBurst.Spawn(transform.position, 12, splashColor);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (popped) return;
        SweetCartController cart = other.GetComponent<SweetCartController>();
        if (cart != null)
        {
            popped = true;
            cart.TakeDamage(15);
            SweetConfettiBurst.Spawn(transform.position, 10, splashColor);
            Destroy(gameObject);
        }
    }
}

public sealed class SweetCollectible : MonoBehaviour
{
    public bool isTurboStar;
    private float baseY;
    private float phase;
    private SpriteRenderer sr;
    private bool collected;

    private void Start()
    {
        baseY = transform.position.y;
        phase = Random.Range(0f, 6.28f);
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        float scroll = SweetWorldGameManager.Instance != null ? SweetWorldGameManager.Instance.CurrentScrollSpeed : 6f;
        float x = transform.position.x + scroll * Time.deltaTime;
        float y = baseY + Mathf.Sin(Time.time * 4.2f + phase) * 0.18f;
        transform.position = new Vector3(x, y, 0f);
        if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-baseY * 100f);
        if (x > 12.5f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        SweetCartController cart = other.GetComponent<SweetCartController>();
        if (cart == null) return;
        collected = true;

        if (isTurboStar)
        {
            cart.ActivateGirlPower(6f);
            if (SweetWorldGameManager.Instance != null)
                SweetWorldGameManager.Instance.AddScore(100, "ESTRELA MÁGICA! GIRL POWER TURBO!", true);
            if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
        }
        else
        {
            if (SweetWorldGameManager.Instance != null)
                SweetWorldGameManager.Instance.CollectIceCream();
            if (SweetAudio.Instance != null) SweetAudio.Instance.PlayPickup();
        }

        SweetConfettiBurst.Spawn(transform.position, 9, new Color(1f, 0.85f, 0.35f, 1f));
        Destroy(gameObject);
    }
}

public sealed class GingerbreadDeliveryStop : MonoBehaviour
{
    public Transform houseVisual;
    public Transform coneBeacon;
    private bool delivered;
    private float wobbleTime = -1f;
    private Vector3 houseBaseScale;

    private void Start()
    {
        if (houseVisual != null) houseBaseScale = houseVisual.localScale;
    }

    private void Update()
    {
        float scroll = SweetWorldGameManager.Instance != null ? SweetWorldGameManager.Instance.CurrentScrollSpeed : 6f;
        transform.position += Vector3.right * (scroll * Time.deltaTime);

        if (coneBeacon != null)
            coneBeacon.localPosition = new Vector3(0f, 3.25f + Mathf.Sin(Time.time * 4.5f) * 0.25f, 0f);

        if (wobbleTime >= 0f && houseVisual != null)
        {
            wobbleTime += Time.deltaTime;
            float t = Mathf.Clamp01(wobbleTime / 0.55f);
            float bounce = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 0.22f;
            houseVisual.localScale = new Vector3(houseBaseScale.x * (1f + bounce), houseBaseScale.y * (1f - bounce), 1f);
            if (t >= 1f) { wobbleTime = -1f; houseVisual.localScale = houseBaseScale; }
        }

        if (transform.position.x > 14f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (delivered) return;
        SweetCartController cart = other.GetComponent<SweetCartController>();
        if (cart == null || SweetWorldGameManager.Instance == null) return;

        if (SweetWorldGameManager.Instance.CargoCount > 0)
        {
            delivered = true;
            wobbleTime = 0f;
            SweetWorldGameManager.Instance.DeliverCargo(cart);
            SweetConfettiBurst.Spawn(transform.position + Vector3.up * 1.4f, 18, new Color(1f, 0.45f, 0.85f, 1f));
            if (coneBeacon != null) coneBeacon.gameObject.SetActive(false);
        }
        else
        {
            SweetWorldGameManager.Instance.ShowBanner("PEGUE CUPCAKES NA ESTRADA PARA ENTREGAR AQUI!");
        }
    }
}

public sealed class SweetRoadZone : MonoBehaviour
{
    public bool isSpeedBoost = true;
    private bool triggered;

    private void Update()
    {
        float scroll = SweetWorldGameManager.Instance != null ? SweetWorldGameManager.Instance.CurrentScrollSpeed : 6f;
        transform.position += Vector3.right * (scroll * Time.deltaTime);
        if (transform.position.x > 13f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        SweetCartController cart = other.GetComponent<SweetCartController>();
        if (cart == null) return;
        triggered = true;

        if (isSpeedBoost)
        {
            cart.ActivateGirlPower(4.5f);
            if (SweetWorldGameManager.Instance != null)
                SweetWorldGameManager.Instance.AddScore(100, "ARCO TURBO! +100 PTS!", true);
            if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
            SweetConfettiBurst.Spawn(transform.position, 14, new Color(1f, 0.86f, 0.25f, 1f));
        }
        else
        {
            cart.ApplySlow(1.8f);
            if (SweetWorldGameManager.Instance != null)
                SweetWorldGameManager.Instance.ShowBanner("CALDA PEGAJOSA! DESVIE DO CARAMELO!");
            if (SweetAudio.Instance != null) SweetAudio.Instance.PlayHit();
        }
    }
}

public sealed class SweetConfettiBurst : MonoBehaviour
{
    private Vector2 vel;
    private float spin;
    private float age;
    private SpriteRenderer sr;
    private Color baseCol;

    private static readonly Color[] Palette =
    {
        new Color(1f, 0.38f, 0.78f, 1f),
        new Color(1f, 0.88f, 0.28f, 1f),
        new Color(0.40f, 0.88f, 1f, 1f),
        new Color(0.76f, 0.48f, 1f, 1f),
        new Color(0.45f, 0.94f, 0.65f, 1f)
    };

    public static void Spawn(Vector3 pos, int count, Color tint)
    {
        Sprite star = SweetWorldArtUtility.GetStarSprite();
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("SweetConfetti");
            go.transform.position = pos + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.2f, 0.35f), 0f);
            go.transform.localScale = Vector3.one * Random.Range(0.28f, 0.52f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = star;
            sr.sortingLayerName = "VFX";
            sr.sortingOrder = 50;

            Color c = (i % 2 == 0) ? tint : Palette[i % Palette.Length];
            sr.color = c;

            SweetConfettiBurst b = go.AddComponent<SweetConfettiBurst>();
            float ang = (i * (360f / count) + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
            b.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) + 0.55f) * Random.Range(3.2f, 6.2f);
            b.spin = Random.Range(-420f, 420f);
            b.sr = sr;
            b.baseCol = c;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= 0.70f) { Destroy(gameObject); return; }
        vel.y -= 8.5f * Time.deltaTime;
        transform.position += (Vector3)(vel * Time.deltaTime);
        transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (sr != null) sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(1f, 0f, age / 0.70f));
    }
}
