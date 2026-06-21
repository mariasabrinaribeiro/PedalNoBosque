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
    [SerializeField] float swipeSpeedMultiplier = 1.2f; 
    [SerializeField] float swipeLerpFactor = 8f; 
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] float bulletSpawnOffset = 0.8f;
    [SerializeField] float spriteRotationOffset = 90f;

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

    // Power-up durations and statuses
    public float magnetTimer = 0f;
    public float tripleShotTimer = 0f;
    public float girlPowerTimer = 0f;
    public bool hasShield = false;
    public float displaySpeed { get; private set; }
    private GameObject shieldVisualInstance;

    // Speed trails and continuous sound
    private TrailRenderer speedTrail;
    private Sprite cachedShieldSprite;

    // Cached Components
    private Rigidbody2D rb;

    // Input state
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
        // Verificar se o carrinho foi acidentalmente arrastado para dentro de um Canvas de UI
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.LogError($"[ALERTA IMPORTANTE] O objeto '{gameObject.name}' está dentro do Canvas de UI '{parentCanvas.name}'. Isso tira o carrinho da pista e deixa a tela azul! Por favor, arraste o '{gameObject.name}' para fora do Canvas na janela Hierarchy.");
        }


        // Check if there is a child GameObject named "CarroSprite" or containing "Sprite"
        Transform customSpriteChild = null;
        foreach (Transform child in transform)
        {
            string cName = child.name.ToLower();
            if (cName.Contains("sprite") && !cName.Contains("tiro") && !cName.Contains("point") && !cName.Contains("spawn"))
            {
                customSpriteChild = child;
                break;
            }
        }

        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();

        // SE O SPRITE RENDERER ESTIVER NO OBJETO PAI (sem objeto filho de visual):
        // Criamos o objeto filho de visual automaticamente e transferimos as propriedades do SpriteRenderer
        if (customSpriteChild == null && playerSr != null && playerSr.sprite != null)
        {
            GameObject autoSpriteObj = new GameObject("CarroSprite_Auto");
            autoSpriteObj.transform.SetParent(transform);
            autoSpriteObj.transform.localPosition = Vector3.zero;
            autoSpriteObj.transform.localScale = Vector3.one;

            SpriteRenderer childSr = autoSpriteObj.AddComponent<SpriteRenderer>();
            childSr.sprite = playerSr.sprite;
            childSr.material = playerSr.material;
            childSr.color = playerSr.color;
            childSr.sortingLayerID = playerSr.sortingLayerID;
            childSr.sortingOrder = playerSr.sortingOrder;
            childSr.flipX = playerSr.flipX;
            childSr.flipY = playerSr.flipY;

            playerSr.enabled = false; // desativa o SpriteRenderer do pai para evitar renderização dupla
            customSpriteChild = autoSpriteObj.transform;
        }

        if (customSpriteChild != null)
        {
            // Aplica a rotação de alinhamento visual configurada no Inspector do Unity
            customSpriteChild.localRotation = Quaternion.Euler(0f, 0f, spriteRotationOffset);

            // Se o bulletSpawnPoint for filho do jogador, rotacionamos sua posição local pelo mesmo offset para alinhar com a nova rotação do sprite
            if (bulletSpawnPoint != null && bulletSpawnPoint.parent == transform)
            {
                bulletSpawnPoint.localPosition = Quaternion.Euler(0f, 0f, spriteRotationOffset) * bulletSpawnPoint.localPosition;
            }

            // Automatically hide old visual parts (Square, wheels/rodas, window/vidro)
            foreach (Transform child in transform)
            {
                string cName = child.name.ToLower();
                if (child != customSpriteChild && !cName.Contains("tiro") && !cName.Contains("point") && !cName.Contains("spawn"))
                {
                    if (cName.Contains("square") || cName.Contains("roda") || cName.Contains("vidro"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            // Default sprite loading fallback
            if (playerSr != null && playerSr.sprite == null)
            {
                Sprite vanSprite = Resources.Load<Sprite>("icecreamvan");
                if (vanSprite != null)
                {
                    playerSr.sprite = vanSprite;
                    playerSr.color = Color.white;
                }
            }
        }

        if (boostText != null)
        {
            boostText.gameObject.SetActive(false);
        }

        // 1. Enable physics interpolation to prevent camera tracking stutters
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            // Prevent jitter from unwanted rotation by freezing rotation on the car's rigidbody.
            rb.constraints = RigidbodyConstraints2D.None; // Permite rotação livre para o carro girar
        }

        // 2. Add speed trail dynamically
        speedTrail = gameObject.AddComponent<TrailRenderer>();
        speedTrail.time = 0f; // start hidden
        speedTrail.startWidth = 0.35f;
        speedTrail.endWidth = 0f;
        
        Shader spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null)
        {
            speedTrail.material = new Material(spriteShader);
        }
        
        // Default pink color
        speedTrail.startColor = new Color(1f, 0.482f, 0.773f, 0.55f);
        speedTrail.endColor = new Color(1f, 0.482f, 0.773f, 0f);
        speedTrail.sortingOrder = 2; // draw just behind car

        // 3. Add engine sound loop synthesizer - DISABLED per user request to make game lighter
        // gameObject.AddComponent<EngineSound>();

        // 4. Hook up environment scanner - DISABLED per user request to make game lighter
        // GameObject scanner = new GameObject("DynamicEnvironmentScanner");
        // scanner.AddComponent<DynamicEnvironmentInitializer>();
        
        // Add ambient sugar dust particles - DISABLED per user request to make game lighter
        /*
        if (Camera.main != null)
        {
            Camera.main.gameObject.AddComponent<AmbientGlitterManager>();
        }
        else
        {
            Debug.LogWarning("DriverPro: Camera.main not found. AmbientGlitterManager was not added.");
        }
        */

        // Cache procedural sprites to prevent memory leaks and GC spikes
        // cachedStarSprite = CreateStarSparkSprite(8, 8, new Color(1f, 0.85f, 0f, 0.8f)); // Disabled to save memory
        cachedShieldSprite = CreateShieldVisualSprite();

        // Enforce a minimum cargo capacity of 5 - REMOVED per user request to allow custom values like 3
        /*
        if (maxCargo < 5)
        {
            maxCargo = 5;
        }
        */
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boost item"))
        {
            currentSpeed = boostSpeed;
            Destroy(collision.gameObject);
            if (boostText != null)
            {
                boostText.gameObject.SetActive(true);
            }
            if (GameAudioController.Instance != null)
            {
                GameAudioController.Instance.PlayPowerup();
            }
        }
        else if (collision.GetComponent<SlowZone>() != null)
        {
            // Speed reduction disabled per user request
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<SlowZone>() != null)
        {
            // Speed reduction disabled per user request
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Reset boost speed on obstacle impact
        currentSpeed = regularSpeed;

        if (GameCameraController.Instance != null)
        {
            GameCameraController.Instance.TriggerShake(0.18f, 0.14f);
        }
    }

    public void SetMobileMove(float val) => mobileMoveInput = val;
    public void SetMobileSteer(float val) => mobileSteerInput = val;
    public void SetMobileShoot(bool val) => mobileShootPressed = val;
    public void SetDragging(bool dragging) => isDragging = dragging;
    public void SetDragDisplacement(Vector2 displacement) => dragDisplacement = displacement;

    private void MoveTowardsDragTarget()
    {
        float speedToUse = currentSpeed;
        if (girlPowerTimer > 0f) speedToUse = boostSpeed;
        // Speed reduction from isSlowed completely disabled for free movement

        float dragMagnitude = Mathf.Clamp01(dragDisplacement.magnitude);

        if (dragMagnitude > 0.05f)
        {
            Vector2 moveDirection = dragDisplacement.normalized;
            // Garantir que a velocidade de arraste seja limitada para evitar que o carrinho voe ou saia do cenário
            float speed = Mathf.Clamp(dragMagnitude * speedToUse * swipeSpeedMultiplier, 0f, boostSpeed * 1.5f);
            displaySpeed = speed;

            Vector2 nextPos = (Vector2)transform.position + moveDirection * (speed * Time.fixedDeltaTime);

            if (rb != null)
            {
                // Obtain current velocity in local space
                Vector2 localCurrent = transform.InverseTransformDirection(rb.velocity);
                // Convert desired drag direction/speed to local space
                Vector2 desiredVelocity = moveDirection * speed;
                Vector2 localDesired = transform.InverseTransformDirection(desiredVelocity);

                // Set forward velocity and preserve/blend sliding sideways component to allow wall sliding
                localCurrent.y = localDesired.y;
                localCurrent.x = (localCurrent.x * 0.15f) + (localDesired.x * 0.85f);

                rb.velocity = transform.TransformDirection(localCurrent);
                rb.angularVelocity = 0f;
            }
            else
            {
                transform.position = nextPos;
            }

            float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            float currentAngle = rb != null ? rb.rotation : transform.rotation.eulerAngles.z;
            float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, swipeLerpFactor * Time.fixedDeltaTime);
            
            if (rb != null)
            {
                rb.MoveRotation(smoothAngle);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
            }
        }
        else
        {
            displaySpeed = 0f;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (hasShield)
        {
            hasShield = false;
            if (shieldVisualInstance != null) Destroy(shieldVisualInstance);

            GameUIController uiController = GameUIController.Instance;
            if (uiController != null)
            {
                uiController.ShowWarning("ESCUDO BLOQUEOU O DANO!");
            }
            if (GameAudioController.Instance != null)
            {
                GameAudioController.Instance.PlayWarning();
            }
            return; // Block damage completely
        }

        health = Mathf.Max(health - damage, 0);

        // Notify mission manager of damage (resets survival streak)
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnPlayerDamaged();
        }

        if (health <= 0)
        {
            // Controla os sons de game over
            if (ControladorJogo.Instance != null)
            {
                ControladorJogo.Instance.GameOver();
            }

            GameUIController uiController = GameUIController.Instance;
            if (uiController != null)
            {
                uiController.ShowGameOver();
            }
        }
    }

    void Update()
    {
        if (Time.timeScale < 0.01f) return;

        // 1. Process Power-up Timers
        if (magnetTimer > 0f)
        {
            magnetTimer -= Time.deltaTime;
            AttractPackages();
        }

        if (tripleShotTimer > 0f)
        {
            tripleShotTimer -= Time.deltaTime;
        }

        if (girlPowerTimer > 0f)
        {
            girlPowerTimer -= Time.deltaTime;
            
            // Cycle speed trail color like a glowing rainbow
            float hue = (Time.time * 2.2f) % 1f;
            Color rainbowColor = Color.HSVToRGB(hue, 1f, 1f);
            if (speedTrail != null)
            {
                speedTrail.startColor = new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, 0.75f);
                speedTrail.endColor = new Color(rainbowColor.r, rainbowColor.g, rainbowColor.b, 0f);
            }

            // Emit sparkly stars randomly - DISABLED to make game lighter
            /*
            if (Random.value < 0.15f)
            {
                SpawnGoldStarSpark();
            }
            */
        }
        else
        {
            // Reset to default pink speed trail
            if (speedTrail != null)
            {
                speedTrail.startColor = new Color(1f, 0.482f, 0.773f, 0.55f);
                speedTrail.endColor = new Color(1f, 0.482f, 0.773f, 0f);
            }
        }

        // 2. Adjust Speed Trail intensity based on car velocity
        if (rb != null && speedTrail != null)
        {
            float speedRatio = Mathf.Clamp01(rb.velocity.magnitude / 6.5f);
            speedTrail.time = Mathf.Lerp(0.02f, 0.35f, speedRatio);
            speedTrail.startWidth = Mathf.Lerp(0.12f, 0.45f, speedRatio);
        }

        // 3. Process normal Inputs
        float keyboardMove = 0f;
        float keyboardSteer = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) keyboardMove = 1f;
            else if (Keyboard.current.sKey.isPressed) keyboardMove = -1f;

            if (Keyboard.current.aKey.isPressed) keyboardSteer = 1f;
            else if (Keyboard.current.dKey.isPressed) keyboardSteer = -1f;
        }

        moveInput = Mathf.Clamp(keyboardMove + mobileMoveInput, -1f, 1f);
        steerInput = Mathf.Clamp(keyboardSteer + mobileSteerInput, -1f, 1f);

        bool isShooting = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || mobileShootPressed;
        if (isShooting)
        {
            Shoot();
        }
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
            float speedToUse = currentSpeed;
            if (girlPowerTimer > 0f) speedToUse = boostSpeed;
            // Speed reduction from isSlowed completely disabled for free movement

            float moveAmount = moveInput * speedToUse;
            float steerAmount = steerInput * steerSpeed * Time.fixedDeltaTime;

            displaySpeed = Mathf.Abs(moveInput) * speedToUse;

            if (rb != null)
            {
                // Convert current velocity to local space
                Vector2 localVelocity = transform.InverseTransformDirection(rb.velocity);
                
                // Set the forward component to the desired moveAmount
                localVelocity.y = moveAmount;
                
                // Zera a velocidade lateral completamente para eliminar o deslizamento
                localVelocity.x = 0f;
                
                // Reapply in world coordinates
                rb.velocity = transform.TransformDirection(localVelocity);
                rb.angularVelocity = 0f;

                float nextRotation = rb.rotation + steerAmount;
                rb.MoveRotation(nextRotation);
            }
            else
            {
                transform.Translate(0, moveAmount * Time.fixedDeltaTime, 0);
                transform.Rotate(0, 0, steerAmount);
            }
        }

        // Limita a velocidade física máxima para evitar que o carrinho voe ou saia do cenário por bugs físicos/colisão
        if (rb != null)
        {
            float maxVelocityLimit = boostSpeed * 1.5f;
            if (rb.velocity.magnitude > maxVelocityLimit)
            {
                rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxVelocityLimit);
            }
        }
    }

    private void AttractPackages()
    {
        // Usar física local OverlapCircleAll para encontrar pacotes próximos (raio 6.0f)
        // Isso evita varrer toda a cena com FindGameObjectsWithTag a cada frame.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 6.0f);
        foreach (var col in colliders)
        {
            if (col.CompareTag("package"))
            {
                // Puxa o pacote suavemente para o centro do carrinho
                col.transform.position = Vector3.MoveTowards(col.transform.position, transform.position, 8f * Time.deltaTime);
            }
        }
    }

    /*
    private void SpawnGoldStarSpark()
    {
        GameObject spark = new GameObject("StarSpark");
        spark.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.4f;

        SpriteRenderer sparkSr = spark.AddComponent<SpriteRenderer>();
        sparkSr.sprite = cachedStarSprite;
        sparkSr.sortingOrder = 4;

        ExplosionParticle p = spark.AddComponent<ExplosionParticle>();
        p.direction = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f).normalized;
        p.speed = Random.Range(1f, 3f);
    }
    */

    public void ActivateMagnet(float duration)
    {
        magnetTimer = Mathf.Max(magnetTimer, duration);
    }

    public void ActivateTripleShot(float duration)
    {
        tripleShotTimer = Mathf.Max(tripleShotTimer, duration);
    }

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
        sr.sortingOrder = 4; // render above car
    }

    public void ActivateGirlPower(float duration)
    {
        girlPowerTimer = Mathf.Max(girlPowerTimer, duration);

        // Sound trigger
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayGirlPower();
        }

        // Particle burst - DISABLED to make game lighter
        /*
        for (int i = 0; i < 12; i++)
        {
            SpawnGoldStarSpark();
        }
        */
    }

    public void Shoot()
    {
        // Adjust shooting speed cooldown under Girl Power (shoots twice as fast at 0.04s)
        float currentCooldown = (girlPowerTimer > 0f) ? 0.04f : shootCooldown;

        if (Time.time - lastShootTime < currentCooldown) return;
        lastShootTime = Time.time;

        bool gp = (girlPowerTimer > 0f);

        // Usamos o bulletSpawnPoint como referência se configurado e posicionado, senão o próprio transform do jogador (utilizando o eixo Y / transform.up)
        Transform refTransform = (bulletSpawnPoint != null && bulletSpawnPoint.localPosition != Vector3.zero) ? bulletSpawnPoint : transform;
        Vector3 baseUp = refTransform.up;
        Quaternion baseRot = refTransform.rotation;

        if (tripleShotTimer > 0f)
        {
            // Center
            SpawnBullet(baseUp, baseRot, gp);
            
            // Left 15 degrees
            Quaternion leftRot = baseRot * Quaternion.Euler(0f, 0f, 15f);
            Vector3 leftDir = leftRot * Vector3.up;
            SpawnBullet(leftDir, leftRot, gp);

            // Right 15 degrees
            Quaternion rightRot = baseRot * Quaternion.Euler(0f, 0f, -15f);
            Vector3 rightDir = rightRot * Vector3.up;
            SpawnBullet(rightDir, rightRot, gp);
        }
        else
        {
            SpawnBullet(baseUp, baseRot, gp);
        }

        // Play shoot audio
        if (AudioManager.Instance != null)
        {
            AudioManager.PlaySFX("Tiro");
        }
        else if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayShoot();
        }
    }

    private void SpawnBullet(Vector3 dir, Quaternion rot, bool gp)
    {
        if (ObjectPoolManager.Instance == null) return;

        GameObject bulletGo = ObjectPoolManager.Instance.GetBullet();
        
        // Se o bulletSpawnPoint estiver assinalado e configurado, usa sua posição exata, senão calcula a partir do transform.up (seta verde)
        Vector3 spawnPos = transform.position + transform.up * bulletSpawnOffset;
        if (bulletSpawnPoint != null && bulletSpawnPoint.localPosition != Vector3.zero)
        {
            spawnPos = bulletSpawnPoint.position;
        }
        bulletGo.transform.position = spawnPos;
        bulletGo.transform.rotation = rot;

        Bullet bulletScript = bulletGo.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(dir, 32f, gp);
        }

        bulletGo.SetActive(true);
    }

    private Sprite CreateShieldVisualSprite()
    {
        int w = 48;
        int h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color cyanAlpha = new Color(0f, 0.8f, 1f, 0.25f);
        Color whiteAlpha = new Color(1f, 1f, 1f, 0.65f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - w / 2f;
                float dy = y - h / 2f;
                float distSq = dx * dx + dy * dy;
                float rOuter = w / 2f;
                float rInner = rOuter - 4f;

                if (distSq <= rOuter * rOuter && distSq >= (rInner - 3f) * (rInner - 3f))
                {
                    if (distSq > rInner * rInner)
                    {
                        tex.SetPixel(x, y, whiteAlpha);
                    }
                    else
                    {
                        tex.SetPixel(x, y, cyanAlpha);
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

    private Sprite CreateStarSparkSprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w / 2f);
                float dy = Mathf.Abs(y - h / 2f);
                if (dx + dy <= w / 2f)
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

    public float GetSwipeSpeedMultiplier() => swipeSpeedMultiplier;
    public void SetSwipeSpeedMultiplier(float val) => swipeSpeedMultiplier = val;
}
