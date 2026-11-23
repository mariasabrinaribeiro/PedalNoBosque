using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class GirlPowerStreetController : MonoBehaviour
{
    [Header("Referências")]
    public SpriteRenderer avatarRenderer;
    public Sprite idleSprite;
    public Sprite walkSpriteA;
    public Sprite walkSpriteB;
    public Transform cart;
    public Camera gameplayCamera;
    public TextMeshProUGUI controlsLabel;
    public TextMeshProUGUI cartPrompt;

    [Header("Movimento")]
    public float walkSpeed = 2.8f;
    public float runSpeed = 4.8f;
    public float cartInteractionDistance = 2.2f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private bool isRunning;
    private bool cartOpen;
    private float animationClock;
    private bool animationFrame;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.useFullKinematicContacts = true;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        moveInput = new Vector2(x, y).normalized;
        isRunning = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        UpdateAvatar();
        UpdateCartPrompt(keyboard);
    }

    private void FixedUpdate()
    {
        if (body == null || moveInput.sqrMagnitude < 0.001f) return;
        float speed = isRunning ? runSpeed : walkSpeed;
        Vector2 next = body.position + moveInput * speed * Time.fixedDeltaTime;
        next.x = Mathf.Clamp(next.x, -30f, 30f);
        next.y = Mathf.Clamp(next.y, -14f, 10f);
        body.MovePosition(next);
    }

    private void LateUpdate()
    {
        if (gameplayCamera == null) return;

        Vector3 playerPosition = transform.position;
        Vector3 target = playerPosition + new Vector3(moveInput.x * 1.6f, moveInput.y * 0.9f, 0f);
        bool nearCart = cart != null && Vector2.Distance(transform.position, cart.position) < 4.2f;
        if (nearCart)
        {
            target = Vector3.Lerp(playerPosition, cart.position, 0.42f);
            target.z = playerPosition.z;
        }

        target.z = gameplayCamera.transform.position.z;
        gameplayCamera.transform.position = Vector3.Lerp(
            gameplayCamera.transform.position, target, 1f - Mathf.Exp(-6f * Time.deltaTime));

        float targetSize = nearCart ? 5.8f : 5.2f;
        gameplayCamera.orthographicSize = Mathf.Lerp(
            gameplayCamera.orthographicSize, targetSize, 1f - Mathf.Exp(-4f * Time.deltaTime));
    }

    private void UpdateAvatar()
    {
        if (avatarRenderer == null) return;
        if (Mathf.Abs(moveInput.x) > 0.1f) avatarRenderer.flipX = moveInput.x < 0f;

        if (moveInput.sqrMagnitude < 0.001f)
        {
            avatarRenderer.sprite = idleSprite;
            animationClock = 0f;
            return;
        }

        animationClock += Time.deltaTime * (isRunning ? 9f : 6f);
        if (animationClock >= 1f)
        {
            animationClock = 0f;
            animationFrame = !animationFrame;
        }
        avatarRenderer.sprite = animationFrame ? walkSpriteA : walkSpriteB;
    }

    private void UpdateCartPrompt(Keyboard keyboard)
    {
        if (cartPrompt == null || cart == null) return;
        bool nearby = Vector2.Distance(transform.position, cart.position) <= cartInteractionDistance;
        if (!nearby)
        {
            cartPrompt.text = string.Empty;
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame) cartOpen = !cartOpen;
        cartPrompt.text = cartOpen ? "CARRINHO ABERTO" : "E  •  ABRIR CARRINHO";
    }
}
