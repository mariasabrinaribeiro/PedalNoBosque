using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class driverpro : MonoBehaviour
{
    [SerializeField] float currentSpeed = 5f;
    [SerializeField] float steerSpeed = 200f;
    [SerializeField] float boostSpeed = 10f;
    [SerializeField] float regularSpeed = 5f;
    [SerializeField] float swipeSpeedMultiplier = 6.5f; // Sensitivity for finger/mouse swipe controls

    [SerializeField] TMP_Text boostText;

    [Header("Gameplay Stats")]
    public int health = 100;
    public int maxHealth = 100;
    public int score = 0;
    public int cargo = 0;
    public int maxCargo = 3;

    [Header("Shooting Config")]
    [SerializeField] private float shootCooldown = 0.3f;
    private float lastShootTime = -1f;

    void Start()
    {
        if (boostText != null)
        {
            boostText.gameObject.SetActive(false);
        }
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
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        currentSpeed = regularSpeed;
    }

    // Mobile inputs set by UI touch buttons
    private float mobileMoveInput = 0f;
    private float mobileSteerInput = 0f;
    private bool mobileShootPressed = false;

    public void SetMobileMove(float val)
    {
        mobileMoveInput = val;
    }

    public void SetMobileSteer(float val)
    {
        mobileSteerInput = val;
    }

    public void SetMobileShoot(bool val)
    {
        mobileShootPressed = val;
    }

    public void ApplySwipeDelta(Vector2 screenDelta)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        // Convert screen delta to world delta for orthographic camera
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        Vector3 worldDelta = new Vector3(
            (screenDelta.x / Screen.width) * worldWidth * swipeSpeedMultiplier,
            (screenDelta.y / Screen.height) * worldHeight * swipeSpeedMultiplier,
            0f
        );

        // Move the car directly (bypassing forces/gravity)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // reset velocity to isolate from other forces
            rb.angularVelocity = 0f;
            rb.MovePosition(rb.position + (Vector2)worldDelta);
        }
        else
        {
            transform.position += worldDelta;
        }

        // Rotate the car to face the drag direction
        if (worldDelta.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(worldDelta.y, worldDelta.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void TakeDamage(int damage)
    {
        health = Mathf.Max(health - damage, 0);
        if (health <= 0)
        {
            GameUIController uiController = FindObjectOfType<GameUIController>();
            if (uiController != null)
            {
                uiController.ShowGameOver();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Don't move or shoot if the game is paused (Time.timeScale == 0)
        if (Time.timeScale < 0.01f) return;

        float steer = 0f;
        float move = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
            {
                move = 1f;
            }
            else if (Keyboard.current.sKey.isPressed)
            {
                move = -1f;
            }

            if (Keyboard.current.aKey.isPressed)
            {
                steer = 1f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                steer = -1f;
            }
        }

        // Combine inputs and clamp them
        move = Mathf.Clamp(move + mobileMoveInput, -1f, 1f);
        steer = Mathf.Clamp(steer + mobileSteerInput, -1f, 1f);

        float moveAmount = move * currentSpeed * Time.deltaTime;
        float steerAmount = steer * steerSpeed * Time.deltaTime;

        // Translate moves the object, rotate rotates it
        transform.Translate(0, moveAmount, 0);
        transform.Rotate(0, 0, steerAmount);

        // Shooting logic
        bool isShooting = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || mobileShootPressed;
        if (isShooting && Time.time - lastShootTime >= shootCooldown)
        {
            lastShootTime = Time.time;
            Shoot();
        }
    }

    private void Shoot()
    {
        GameObject bulletGo = new GameObject("Bullet");
        bulletGo.transform.position = transform.position + transform.up * 0.8f; // spawn slightly ahead of the cart
        bulletGo.transform.rotation = transform.rotation;

        SpriteRenderer sr = bulletGo.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBulletSprite(16, 16, candyPinkColor());
        sr.sortingOrder = 5; // render on top of roads and tiles

        CircleCollider2D col = bulletGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        Rigidbody2D rb = bulletGo.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        Bullet bulletScript = bulletGo.AddComponent<Bullet>();
        bulletScript.direction = transform.up;
        bulletScript.speed = 12f;
    }

    private Color candyPinkColor()
    {
        return new Color(1f, 0.482f, 0.773f, 1f); // matching candy pink theme
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
