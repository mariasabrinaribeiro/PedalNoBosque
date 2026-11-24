using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador da protagonista Girl Power e seu carrinho "Sweet Treats Delivery":
/// - Movimento suave em 2.5D pela estrada de creme dourado (teclado WASD/Setas + toque/arraste no celular)
/// - Tiro de bolhas mágicas rosa cintilantes (Espaço / Clique / Botão ATIRAR)
/// - Modo GIRL POWER TURBO com disparo triplo de bolhas, brilho mágico e rastro estrelado!
/// </summary>
public sealed class SweetCartController : MonoBehaviour
{
    [Header("Limites da Estrada 2.5D")]
    public float minX = -6.6f;
    public float maxX = -0.8f;
    public float minY = -3.85f;
    public float maxY = -1.10f;

    [Header("Movimento & Tiro")]
    public float moveSpeedX = 6.2f;
    public float moveSpeedY = 5.6f;
    public float shootCooldown = 0.24f;

    public Transform visualRoot;
    public SpriteRenderer cartRenderer;
    public SpriteRenderer shadowRenderer;
    public SpriteRenderer turboAuraRenderer;
    public Transform cannonMuzzle;

    public int MaxHealth { get; } = 100;
    public int Health { get; private set; } = 100;
    public float GirlPowerTimer { get; private set; }
    public float SlowTimer { get; private set; }
    public bool IsGirlPowerActive => GirlPowerTimer > 0f;

    private float nextShootTime;
    private float hitFlashTimer;
    private float mobileShootHeldTimer;
    private Vector2 dragVel;
    private Vector3 baseVisualScale = Vector3.one;
    private float wheelBobClock;

    private void Start()
    {
        if (visualRoot != null) baseVisualScale = visualRoot.localScale;
        ResetCart();
    }

    public void ResetCart()
    {
        Health = MaxHealth;
        GirlPowerTimer = 0f;
        SlowTimer = 0f;
        hitFlashTimer = 0f;
        transform.position = new Vector3(-4.4f, -2.55f, 0f);
        if (cartRenderer != null) cartRenderer.color = Color.white;
    }

    public void TriggerMobileShoot() => mobileShootHeldTimer = 0.16f;

    public void ActivateGirlPower(float duration)
    {
        GirlPowerTimer = Mathf.Max(GirlPowerTimer, duration);
    }

    public void ApplySlow(float duration)
    {
        SlowTimer = Mathf.Max(SlowTimer, duration);
    }

    public void Heal(int amount)
    {
        Health = Mathf.Min(MaxHealth, Health + amount);
    }

    public void TakeDamage(int dmg)
    {
        if (IsGirlPowerActive || hitFlashTimer > 0f) return;
        Health = Mathf.Max(0, Health - dmg);
        hitFlashTimer = 0.55f;
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayHit();
        if (Health <= 0 && SweetWorldGameManager.Instance != null)
            SweetWorldGameManager.Instance.TriggerGameOver();
    }

    private void Update()
    {
        if (SweetWorldGameManager.Instance == null || !SweetWorldGameManager.Instance.IsPlaying)
            return;

        if (GirlPowerTimer > 0f) GirlPowerTimer = Mathf.Max(0f, GirlPowerTimer - Time.deltaTime);
        if (SlowTimer > 0f) SlowTimer = Mathf.Max(0f, SlowTimer - Time.deltaTime);
        if (hitFlashTimer > 0f) hitFlashTimer = Mathf.Max(0f, hitFlashTimer - Time.deltaTime);
        if (mobileShootHeldTimer > 0f) mobileShootHeldTimer -= Time.deltaTime;

        Vector2 input = ReadMoveInput();
        float slowMul = (SlowTimer > 0f) ? 0.52f : 1f;
        float gpMul = IsGirlPowerActive ? 1.25f : 1f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x + input.x * moveSpeedX * slowMul * gpMul * Time.deltaTime, minX, maxX);
        pos.y = Mathf.Clamp(pos.y + input.y * moveSpeedY * slowMul * gpMul * Time.deltaTime, minY, maxY);
        transform.position = pos;

        // Y-sorting dinâmico com inimigos e cupcakes na estrada
        int sortOrder = Mathf.RoundToInt(-pos.y * 100f) + 5;
        if (cartRenderer != null) cartRenderer.sortingOrder = sortOrder;

        // Balanço alegre da suspensão e leve inclinação ao trocar de faixa!
        wheelBobClock += Time.deltaTime * (IsGirlPowerActive ? 18f : 12f);
        if (visualRoot != null)
        {
            float bob = Mathf.Sin(wheelBobClock) * 0.055f;
            float tilt = input.y * 7.5f - (IsGirlPowerActive ? 2.5f : 0f);
            visualRoot.localPosition = new Vector3(0f, bob, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            float gpPulse = IsGirlPowerActive ? (1f + 0.04f * Mathf.Sin(Time.time * 16f)) : 1f;
            visualRoot.localScale = baseVisualScale * gpPulse;
        }

        if (cartRenderer != null)
        {
            if (hitFlashTimer > 0f)
                cartRenderer.color = ((int)(hitFlashTimer * 18f) % 2 == 0) ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
            else if (SlowTimer > 0f)
                cartRenderer.color = new Color(0.95f, 0.80f, 0.65f, 1f);
            else
                cartRenderer.color = Color.white;
        }

        if (turboAuraRenderer != null)
        {
            turboAuraRenderer.enabled = IsGirlPowerActive;
            if (IsGirlPowerActive)
            {
                float a = 0.24f + 0.14f * Mathf.Sin(Time.time * 12f);
                turboAuraRenderer.color = new Color(1f, 0.42f, 0.88f, a);
            }
        }

        if (ReadShootInput() && Time.time >= nextShootTime)
        {
            FireMagicBubbles();
            nextShootTime = Time.time + (IsGirlPowerActive ? shootCooldown * 0.68f : shootCooldown);
        }
    }

    private void FireMagicBubbles()
    {
        Vector3 spawn = cannonMuzzle != null ? cannonMuzzle.position : (transform.position + new Vector3(1.55f, -0.10f, 0f));
        SpawnSingleBubble(spawn, new Vector2(15.5f, 0f));
        if (IsGirlPowerActive)
        {
            SpawnSingleBubble(spawn + new Vector3(0f, 0.20f, 0f), new Vector2(15.0f, 2.4f));
            SpawnSingleBubble(spawn + new Vector3(0f, -0.20f, 0f), new Vector2(15.0f, -2.4f));
        }
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayShoot();
    }

    private static void SpawnSingleBubble(Vector3 pos, Vector2 velocity)
    {
        GameObject go = new GameObject("MagicBubble");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.62f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SweetWorldArtUtility.GetMagicBubbleSprite();
        sr.sortingLayerName = "VFX";
        sr.sortingOrder = 20;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.50f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        MagicBubbleProjectile proj = go.AddComponent<MagicBubbleProjectile>();
        proj.velocity = velocity;
    }

    private Vector2 ReadMoveInput()
    {
        float x = 0f, y = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
#endif
        return new Vector2(x, y).normalized;
    }

    private bool ReadShootInput()
    {
        if (mobileShootHeldTimer > 0f) return true;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.isPressed || kb.zKey.isPressed || kb.jKey.isPressed)) return true;
#else
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Z)) return true;
#endif
        return false;
    }
}
