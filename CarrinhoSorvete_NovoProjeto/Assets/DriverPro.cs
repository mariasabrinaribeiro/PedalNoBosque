using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class DriverPro : MonoBehaviour
{
    [SerializeField] float currentSpeed = 3.5f;
    [SerializeField] float steerSpeed = 200f;
    [SerializeField] float boostSpeed = 6.0f;
    [SerializeField] float regularSpeed = 3.5f;
    public float RegularSpeed => regularSpeed;
    [SerializeField] float swipeSpeedMultiplier = 1.2f; 
    [SerializeField] float swipeLerpFactor = 8f; 
    [SerializeField] float bulletSpawnOffset = 0.8f;

    [SerializeField] TMP_Text boostText;

    private float moveInput = 0f;
    private float steerInput = 0f;

    [Header("Gameplay Stats")]
    public int health = 100;
    public int maxHealth = 100;
    public int score = 0;
    public int cargo = 0;
    public int maxCargo = 5;

    [Header("Shooting Config")]
    [SerializeField] private float shootCooldown = 0.08f;
    private float lastShootTime = -1f;

    public float magnetTimer = 0f;
    public float tripleShotTimer = 0f;
    public float girlPowerTimer = 0f;
    public bool hasShield = false;
    public float displaySpeed { get; private set; }
    private GameObject shieldVisualInstance;

    [SerializeField] private Transform bulletSpawnPoint;
    private TrailRenderer speedTrail;
    private Sprite cachedShieldSprite;
    private Rigidbody2D rb;

    private bool isDragging = false;
    private Vector2 dragDisplacement = Vector2.zero;

    private float mobileMoveInput = 0f;
    private float mobileSteerInput = 0f;
    private bool mobileShootPressed = false;

    public static DriverPro Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.None; 
            rb.gravityScale = 0f;
        }

        speedTrail = gameObject.AddComponent<TrailRenderer>();
        speedTrail.time = 0f;
        speedTrail.startWidth = 0.28f;
        speedTrail.endWidth = 0f;

        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null) speedTrail.material = new Material(spriteShader);

        // ETAPA 4: Opacidade máxima de partículas/rastro rosa limitada a 35% na camada VFX
        speedTrail.startColor = new Color(1f, 0.482f, 0.773f, 0.35f);
        speedTrail.endColor = new Color(1f, 0.482f, 0.773f, 0f);
        speedTrail.sortingLayerName = "VFX";
        speedTrail.sortingOrder = 2;

        // ETAPA 4: Reduz a nuvem magenta (tamanho, emissão -40% e opacidade <= 30%) na camada VFX
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var emission = ps.emission;
            emission.rateOverTimeMultiplier = Mathf.Min(emission.rateOverTimeMultiplier * 0.60f, 8f);
            var main = ps.main;
            main.startSizeMultiplier = Mathf.Min(main.startSizeMultiplier, 0.42f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.52f, 0.82f, 0.28f),
                new Color(1f, 0.88f, 0.55f, 0.26f));
            ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sortingLayerName = "VFX";
                psr.sortingOrder = 1;
            }
        }

        cachedShieldSprite = CreateShieldVisualSprite();
    }

    public void SetMobileMove(float val) => mobileMoveInput = val;
    public void SetMobileSteer(float val) => mobileSteerInput = val;
    public void SetMobileShoot(bool val) => mobileShootPressed = val;
    public void SetDragging(bool dragging) => isDragging = dragging;
    public void SetDragDisplacement(Vector2 displacement) => dragDisplacement = displacement;

    void Update()
    {
        if (Time.timeScale < 0.01f) return;

        if (magnetTimer > 0f)
        {
            magnetTimer -= Time.deltaTime;
            AttractPackages();
        }

        if (tripleShotTimer > 0f) tripleShotTimer -= Time.deltaTime;

        if (girlPowerTimer > 0f)
        {
            girlPowerTimer -= Time.deltaTime;
            float hue = (Time.time * 2.2f) % 1f;
            Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
            if (speedTrail != null)
            {
                speedTrail.startColor = new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, 0.35f);
                speedTrail.endColor = new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, 0f);
            }
        }
        else
        {
            if (speedTrail != null)
            {
                speedTrail.startColor = new Color(1f, 0.482f, 0.773f, 0.35f);
                speedTrail.endColor = new Color(1f, 0.482f, 0.773f, 0f);
            }
        }

        float keyboardMove = 0f;
        float keyboardSteer = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyboardMove = 1f;
            else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyboardMove = -1f;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardSteer = 1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardSteer = -1f;
        }

        moveInput = Mathf.Clamp(keyboardMove + mobileMoveInput, -1f, 1f);
        steerInput = Mathf.Clamp(keyboardSteer + mobileSteerInput, -1f, 1f);

        if ((Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || mobileShootPressed)
        {
            Shoot();
        }

        // Report observed rigidbody motion instead of the requested drag/input magnitude.
        // A short smoothing step avoids the HUD snapping between zero and peak speed.
        float measuredSpeed = rb != null ? rb.velocity.magnitude : 0f;
        displaySpeed = Mathf.MoveTowards(displaySpeed, measuredSpeed, 18f * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (Time.timeScale < 0.01f) return;

        if (isDragging)
        {
            MoveTowardsDragTarget();
        }
        else
        {
            float speedToUse = girlPowerTimer > 0f ? boostSpeed : currentSpeed;
            
            // CORREÇÃO DA DIREÇÃO DA FÍSICA: Usa o transform.up real do carro atualizado pela rotação física
            Vector2 forwardDirection = transform.up;
            rb.velocity = forwardDirection * (moveInput * speedToUse);

            // CORREÇÃO DA ROTAÇÃO DA FÍSICA: Gira o Rigidbody usando o input horizontal
            float rotationAmount = -steerInput * steerSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation + rotationAmount);

        }
    }

    private void MoveTowardsDragTarget()
    {
        float speedToUse = girlPowerTimer > 0f ? boostSpeed : currentSpeed;
        float dragMagnitude = Mathf.Clamp01(dragDisplacement.magnitude);

        if (dragMagnitude > 0.05f)
        {
            Vector2 moveDirection = dragDisplacement.normalized;
            float speed = Mathf.Clamp(dragMagnitude * speedToUse * swipeSpeedMultiplier, 0f, boostSpeed * 1.5f);
            rb.velocity = moveDirection * speed;

            float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            float smoothAngle = Mathf.LerpAngle(rb.rotation, targetAngle, swipeLerpFactor * Time.fixedDeltaTime);
            rb.MoveRotation(smoothAngle);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void Shoot()
    {
        float currentCooldown = (girlPowerTimer > 0f) ? 0.04f : shootCooldown;
        if (Time.time - lastShootTime < currentCooldown) return;
        lastShootTime = Time.time;

        bool gp = (girlPowerTimer > 0f);
        Vector3 forwardDir = bulletSpawnPoint != null ? bulletSpawnPoint.up : transform.up;

        if (tripleShotTimer > 0f)
        {
            SpawnBullet(forwardDir, transform.rotation, gp);
            
            Quaternion leftRot = Quaternion.Euler(0f, 0f, 15f);
            SpawnBullet(leftRot * forwardDir, transform.rotation * leftRot, gp);

            Quaternion rightRot = Quaternion.Euler(0f, 0f, -15f);
            SpawnBullet(rightRot * forwardDir, transform.rotation * rightRot, gp);
        }
        else
        {
            SpawnBullet(forwardDir, transform.rotation, gp);
        }

        if (AudioManager.Instance != null) AudioManager.PlaySFX("Tiro");
        else if (GameAudioController.Instance != null) GameAudioController.Instance.PlayShoot();
    }

    private void SpawnBullet(Vector3 dir, Quaternion rot, bool gp)
    {
        if (ObjectPoolManager.Instance == null) return;

        GameObject bulletGo = ObjectPoolManager.Instance.GetBullet();
        
        // CORREÇÃO DO LOCAL DO TIRO: Garante o spawn na ponta do cano
        if (bulletSpawnPoint != null)
        {
            bulletGo.transform.position = bulletSpawnPoint.position;
            bulletGo.transform.rotation = bulletSpawnPoint.rotation;
        }
        else
        {
            bulletGo.transform.position = transform.position + transform.up * bulletSpawnOffset;
            bulletGo.transform.rotation = rot;
        }

        Bullet bulletScript = bulletGo.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(dir, 32f, gp);
        }

        bulletGo.SetActive(true);
    }

    private void AttractPackages()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 6.0f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("package"))
            {
                col.transform.position = Vector3.MoveTowards(col.transform.position, transform.position, 8f * Time.deltaTime);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (hasShield)
        {
            hasShield = false;
            if (shieldVisualInstance != null) Destroy(shieldVisualInstance);
            if (GameUIController.Instance != null) GameUIController.Instance.ShowWarning("ESCUDO BLOQUEOU O DANO!");
            return;
        }

        health = Mathf.Max(health - damage, 0);
        if (MissionManager.Instance != null) MissionManager.Instance.OnPlayerDamaged();

        if (health <= 0)
        {
            if (ControladorJogo.Instance != null) ControladorJogo.Instance.GameOver();
            if (GameUIController.Instance != null) GameUIController.Instance.ShowGameOver();
        }
    }

    public void ActivateMagnet(float duration) => magnetTimer = Mathf.Max(magnetTimer, duration);
    public void ActivateTripleShot(float duration) => tripleShotTimer = Mathf.Max(tripleShotTimer, duration);
    public void ActivateGirlPower(float duration) => girlPowerTimer = Mathf.Max(girlPowerTimer, duration);

    public void ActivateShield()
    {
        hasShield = true;
        if (shieldVisualInstance != null) Destroy(shieldVisualInstance);
        shieldVisualInstance = new GameObject("ShieldHaloVisual");
        shieldVisualInstance.transform.SetParent(transform);
        shieldVisualInstance.transform.localPosition = Vector3.zero;
        shieldVisualInstance.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        SpriteRenderer sr = shieldVisualInstance.AddComponent<SpriteRenderer>();
        sr.sprite = cachedShieldSprite;
        sr.sortingLayerName = "VFX";
        sr.sortingOrder = 4;
    }

    private Sprite CreateShieldVisualSprite()
    {
        int w = 48; int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color cyanAlpha = new Color(0f, 0.8f, 1f, 0.25f);
        Color whiteAlpha = new Color(1f, 1f, 1f, 0.65f);
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float dx = x - w / 2f; float dy = y - h / 2f;
                float distSq = dx * dx + dy * dy;
                float rOuter = w / 2f; float rInner = rOuter - 4f;
                if (distSq <= rOuter * rOuter && distSq >= (rInner - 3f) * (rInner - 3f)) {
                    if (distSq > rInner * rInner) tex.SetPixel(x, y, whiteAlpha);
                    else tex.SetPixel(x, y, cyanAlpha);
                } else tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public float GetSwipeSpeedMultiplier() => swipeSpeedMultiplier;
    public void SetSwipeSpeedMultiplier(float val) => swipeSpeedMultiplier = val;
}
