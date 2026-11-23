using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Complete, untimed first delivery loop for the isolated working scene.</summary>
public sealed class GirlPowerFirstDelivery : MonoBehaviour
{
    private enum Phase { AtCart, ReadyToLeave, Outbound, AtFlorist, Returning, Paid, Complete }

    [Header("Scene")]
    public Transform player;
    public Transform cart;
    public Transform florist;
    public Transform sign;
    public Camera sceneCamera;
    public SpriteRenderer playerSprite;
    public Sprite idle, walkA, walkB;
    public TextMeshPro signLabel;
    public Collider2D[] obstacles;

    [Header("Movement")]
    public float walkSpeed = 2.8f;
    public float runSpeed = 4.8f;
    public float interactionRadius = 2.4f;
    public int deliveryPayment = 12;
    public int signPrice = 12;

    private Phase phase = Phase.AtCart;
    private string selectedFlavor = "";
    private string shopName = "Sorvetes da Orla";
    private string message = "Fim de tarde na orla de Matinhos. A florista pediu uma casquinha de morango.";
    private string routeName = "";
    private int coins;
    private bool cartOpen;
    private bool deliveryClaimed;
    private bool routeChosen;
    private float animationClock;
    private bool frameB;
    private bool nameFieldFocused;
    private Vector2 touchMove;
    private float touchMoveSeconds;
    private bool touchRun;
    private bool keyboardConnected;
    private string lastInput = "";
    private const float DeliveryX = 24f;
    private const float ReturnX = 0.5f;

    private Collider2D playerCollider;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        if (player != null) playerCollider = player.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (player == null) return;
        Keyboard keys = Keyboard.current;
        keyboardConnected = keys != null;
        Vector2 input = Vector2.zero;
        if (keys != null)
        {
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) input.x -= 1f;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) input.x += 1f;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) input.y += 1f;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) input.y -= 1f;
            if (keys.eKey.wasPressedThisFrame) { lastInput = "E"; Interact(); }
            if (phase == Phase.AtCart && cartOpen)
            {
                if (keys.digit1Key.wasPressedThisFrame) SelectFlavor("Morango");
                if (keys.digit2Key.wasPressedThisFrame) SelectFlavor("Baunilha");
                if (keys.digit3Key.wasPressedThisFrame) SelectFlavor("Chocolate");
            }
        }
        input = input.normalized;
        if (input.sqrMagnitude > 0.01f) lastInput = "MOVIMENTO";

        bool running = keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed);

        if (touchMoveSeconds > 0f)
        {
            input = touchMove;
            running = touchRun;
            touchMoveSeconds -= Time.deltaTime;
        }
        if (phase == Phase.ReadyToLeave || phase == Phase.Outbound || phase == Phase.Returning || phase == Phase.Paid)
            Move(input, running);
        UpdateMission();
        Animate(input, running);
        FollowCamera(input);
    }

    private void Move(Vector2 input, bool running)
    {
        if (input.sqrMagnitude < 0.001f) return;
        float speed = running ? runSpeed : walkSpeed;
        Vector2 current = player.position;
        Vector2 delta = input * speed * Time.deltaTime;
        Vector2 next = current;
        next = TryMoveAxis(next, new Vector2(delta.x, 0f));
        next = TryMoveAxis(next, new Vector2(0f, delta.y));
        next.x = Mathf.Clamp(next.x, -3.2f, DeliveryX + 3f);
        next.y = Mathf.Clamp(next.y, -3.2f, 3.2f);
        player.position = new Vector3(next.x, next.y, player.position.z);
    }

    private Vector2 TryMoveAxis(Vector2 origin, Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f) return origin;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, 0.22f, delta.normalized, delta.magnitude, LayerMask.GetMask("Default"));
        float nearest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit == playerCollider || hit.isTrigger) continue;
            if (hits[i].distance < nearest) nearest = hits[i].distance;
        }
        if (nearest < float.MaxValue)
        {
            float allowed = Mathf.Max(0f, nearest - 0.025f);
            return origin + delta.normalized * allowed;
        }
        return origin + delta;
    }

    private void Interact()
    {
        if (phase == Phase.AtCart && Near(cart))
        {
            if (!cartOpen)
            {
                cartOpen = true;
                message = "Carrinho aberto. Monte a casquinha de morango pedida pela florista.";
            }
            else if (selectedFlavor == "Morango")
            {
                cartOpen = false;
                phase = Phase.ReadyToLeave;
                message = "Casquinha pronta. Na bifurcação, escolha a calçada ampla da orla ou o atalho da feira.";
            }
            else
            {
                message = "A florista pediu morango. Escolha essa bola para concluir a casquinha.";
            }
            return;
        }

        if (phase == Phase.AtFlorist && Near(florist))
        {
            if (!deliveryClaimed)
            {
                deliveryClaimed = true;
                coins += deliveryPayment;
                phase = Phase.Paid;
                message = "Casquinha entregue. Você recebeu 12 moedas. Volte ao carrinho para instalar sua placa.";
            }
            return;
        }

        if (phase == Phase.Paid && Near(cart))
        {
            if (coins < signPrice)
            {
                message = "Ainda faltam moedas para instalar a placa.";
                return;
            }
            coins -= signPrice;
            phase = Phase.Complete;
            if (sign != null) sign.gameObject.SetActive(true);
            if (signLabel != null) signLabel.text = shopName.Trim().ToUpperInvariant();
            message = "Placa instalada. A primeira entrega inaugurou sua sorveteria.";
        }
    }

    private void SelectFlavor(string flavor)
    {
        selectedFlavor = flavor;
        message = flavor == "Morango"
            ? "Morango escolhido. Pressione E novamente para montar a casquinha."
            : "A florista pediu morango. Escolha a bola de morango para seguir o pedido.";
    }

    private void UpdateMission()
    {
        float x = player.position.x;
        float y = player.position.y;

        if ((phase == Phase.ReadyToLeave || phase == Phase.Outbound) && !routeChosen && x >= 5.5f)
        {
            if (y <= -0.25f)
            {
                routeChosen = true;
                routeName = "Orla";
                phase = Phase.Outbound;
                message = "Rota da orla: siga pela calçada ampla e livre até a florista.";
            }
            else if (y >= 0.55f)
            {
                routeChosen = true;
                routeName = "Feira";
                phase = Phase.Outbound;
                message = "Atalho da feira: desvie das caixas e dos pedestres no caminho.";
            }
        }

        if (phase == Phase.ReadyToLeave && x > 2f) phase = Phase.Outbound;

        if (phase == Phase.Outbound && x >= DeliveryX - 1.4f)
        {
            phase = Phase.AtFlorist;
            message = "Você chegou à florista. Pressione E perto dela para entregar a casquinha.";
        }
        else if (phase == Phase.AtFlorist && x < DeliveryX - 2.5f)
        {
            phase = Phase.Outbound;
        }

        if ((phase == Phase.Paid || phase == Phase.Returning) && x > ReturnX + 1.1f) phase = Phase.Returning;
        if (phase == Phase.Returning && x <= ReturnX + 1.1f)
        {
            phase = Phase.Paid;
            message = "Pagamento: 12 moedas. Escolha o nome da sorveteria e pressione E perto do carrinho.";
        }
    }

    private bool Near(Transform target) => target != null && Vector2.Distance(player.position, target.position) <= interactionRadius;

    private void Animate(Vector2 input, bool running)
    {
        if (playerSprite == null) return;
        if (Mathf.Abs(input.x) > 0.1f) playerSprite.flipX = input.x < 0f;
        if (input.sqrMagnitude < 0.01f)
        {
            playerSprite.sprite = idle;
            animationClock = 0f;
            return;
        }
        animationClock += Time.deltaTime * (running ? 9f : 6f);
        if (animationClock >= 0.14f)
        {
            frameB = !frameB;
            animationClock = 0f;
        }
        playerSprite.sprite = frameB ? walkA : walkB;
    }

    private void FollowCamera(Vector2 input)
    {
        if (sceneCamera == null) return;
        Vector3 target = new Vector3(player.position.x + Mathf.Clamp(input.x, -1f, 1f) * 1.0f, 0.15f, sceneCamera.transform.position.z);
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, target, 1f - Mathf.Exp(-5f * Time.deltaTime));
    }

    private void OnGUI()
    {
        EnsureStyles();
        float scale = Mathf.Clamp(Screen.width / 1600f, 0.72f, 1.18f);
        float panelWidth = 690f * scale;
        float panelHeight = 166f * scale;
        Rect panel = new Rect(28f * scale, 24f * scale, panelWidth, panelHeight);
        GUI.color = new Color(0.13f, 0.16f, 0.22f, 0.94f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 22f * scale, panel.y + 14f * scale, panelWidth - 44f * scale, 32f * scale), "GIRL POWER  /  PRIMEIRA ENTREGA", titleStyle);
        GUI.Label(new Rect(panel.x + 22f * scale, panel.y + 52f * scale, panelWidth - 44f * scale, 50f * scale), message, bodyStyle);
        GUI.Label(new Rect(panel.x + 22f * scale, panel.y + 122f * scale, panelWidth - 44f * scale, 28f * scale), "SEM CRONÔMETRO     WASD / SETAS: ANDAR     SHIFT: CORRER     E: INTERAGIR", bodyStyle);

        if (phase == Phase.AtCart && cartOpen)
        {
            float y = panel.yMax + 12f * scale;
            DrawFlavorButton(new Rect(panel.x, y, 176f * scale, 42f * scale), "1   MORANGO", "Morango");
            DrawFlavorButton(new Rect(panel.x + 188f * scale, y, 176f * scale, 42f * scale), "2   BAUNILHA", "Baunilha");
            DrawFlavorButton(new Rect(panel.x + 376f * scale, y, 176f * scale, 42f * scale), "3   CHOCOLATE", "Chocolate");
        }

        if (phase == Phase.Paid)
        {
            float y = panel.yMax + 14f * scale;
            GUI.color = new Color(0.13f, 0.16f, 0.22f, 0.94f);
            GUI.DrawTexture(new Rect(panel.x, y, 520f * scale, 70f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 16f * scale, y + 6f * scale, 220f * scale, 28f * scale), "NOME DA SORVETERIA", bodyStyle);
            GUI.SetNextControlName("shop-name");
            shopName = GUI.TextField(new Rect(panel.x + 236f * scale, y + 8f * scale, 264f * scale, 34f * scale), shopName, 24, buttonStyle);
            GUI.Label(new Rect(panel.x + 16f * scale, y + 40f * scale, 490f * scale, 22f * scale), "Placa: 12 moedas  |  saldo após instalar: " + Mathf.Max(0, coins - signPrice), bodyStyle);
        }

        if (coins > 0 || phase == Phase.Complete)
        {
            GUI.Label(new Rect(Screen.width - 380f * scale, 30f * scale, 350f * scale, 42f * scale),
                "MOEDAS  " + coins.ToString("00") + "     ROTA  " + (routeName == "" ? "—" : routeName.ToUpperInvariant()), titleStyle);
        }

        DrawTouchControls(scale);
    }

    private void DrawTouchControls(float scale)
    {
        float size = 68f * scale;
        float gap = 7f * scale;
        float x = Screen.width - 220f * scale;
        float y = Screen.height - 220f * scale;
        GUIStyle dpad = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold };
        if (GUI.Button(new Rect(x + size + gap, y, size, size), "W", dpad)) QueueTouchMove(Vector2.up);
        if (GUI.Button(new Rect(x, y + size + gap, size, size), "A", dpad)) QueueTouchMove(Vector2.left);
        if (GUI.Button(new Rect(x + size + gap, y + size + gap, size, size), "S", dpad)) QueueTouchMove(Vector2.down);
        if (GUI.Button(new Rect(x + (size + gap) * 2f, y + size + gap, size, size), "D", dpad)) QueueTouchMove(Vector2.right);
        if (GUI.Button(new Rect(x + (size + gap) * 2f, y, size, size), touchRun ? "RUN" : "WALK", dpad)) touchRun = !touchRun;
        if (GUI.Button(new Rect(x + size + gap, y + (size + gap) * 2f, size, 42f * scale), "E", dpad)) Interact();
    }

    private void QueueTouchMove(Vector2 direction)
    {
        touchMove = direction;
        touchMoveSeconds = 0.38f;
    }

    private void DrawFlavorButton(Rect rect, string label, string flavor)
    {
        GUI.color = new Color(0.78f, 0.36f, 0.48f, 0.98f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (GUI.Button(rect, label, buttonStyle)) SelectFlavor(flavor);
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.88f, 0.73f) } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, normal = { textColor = new Color(0.98f, 0.96f, 0.91f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        panelStyle = new GUIStyle(GUI.skin.box);
    }
}
