using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Runtime for the first no-timer delivery: prepare, choose a route, deliver once, return, install a sign.</summary>
public sealed class GirlPowerFirstDelivery : MonoBehaviour
{
    private enum Phase { AtCart, ReadyToLeave, Outbound, AtFlorist, Returning, Paid, Complete }

    [Header("Scene references")]
    public Transform player;
    public Transform cart;
    public Transform florist;
    public Transform sign;
    public Camera sceneCamera;
    public SpriteRenderer playerSprite;
    public Sprite idle, walkA, walkB;
    public TextMeshPro signLabel;
    public Collider2D[] obstacles;

    [Header("Tuning")]
    public float walkSpeed = 2.8f;
    public float runSpeed = 4.8f;
    public float interactionRadius = 1.8f;
    public int deliveryPayment = 12;

    private Phase phase = Phase.AtCart;
    private string selectedFlavor = "";
    private string shopName = "Sorvetes da Orla";
    private string message = "Fim de tarde em Matinhos. A florista pediu uma casquinha de morango.\nChegue ao carrinho e pressione E.";
    private int coins;
    private bool cartOpen;
    private bool deliveryClaimed;
    private bool routeChosen;
    private string routeName = "";
    private float animationClock;
    private bool frameB;
    private Vector2 lastMove;
    private const float DeliveryX = 24f;
    private const float ReturnX = 0.5f;

    private Keyboard Keys => Keyboard.current;

    private void Update()
    {
        if (player == null || Keys == null) return;
        Keyboard k = Keys;
        Vector2 input = Vector2.zero;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) input.x -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) input.x += 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) input.y += 1f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) input.y -= 1f;
        input = input.normalized;
        lastMove = input;

        if (k.eKey.wasPressedThisFrame) Interact();
        if (phase == Phase.AtCart && cartOpen)
        {
            if (k.digit1Key.wasPressedThisFrame) SelectFlavor("Morango");
            if (k.digit2Key.wasPressedThisFrame) SelectFlavor("Baunilha");
            if (k.digit3Key.wasPressedThisFrame) SelectFlavor("Chocolate");
        }

        if (phase == Phase.ReadyToLeave || phase == Phase.Outbound || phase == Phase.Returning || phase == Phase.Paid)
            Move(input, k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
        UpdateMission();
        Animate(input);
        FollowCamera();
    }

    private void Move(Vector2 input, bool running)
    {
        if (input.sqrMagnitude < 0.001f) return;
        Vector2 step = input * (running ? runSpeed : walkSpeed) * Time.deltaTime;
        Vector2 current = player.position;
        Vector2 next = current + step;
        next.x = Mathf.Clamp(next.x, -2.5f, DeliveryX + 2.5f);
        next.y = Mathf.Clamp(next.y, -3.0f, 3.4f);
        Vector2 delta = next - current;
        float radius = 0.22f;
        RaycastHit2D hit = Physics2D.CircleCast(current, radius, delta.normalized, delta.magnitude, LayerMask.GetMask("Default"));
        if (!hit.collider || hit.collider.isTrigger) player.position = new Vector3(next.x, next.y, player.position.z);
        else if (!hit.collider.isTrigger)
        {
            Vector2 slide = new Vector2(delta.x, 0f);
            if (Mathf.Abs(slide.x) > 0.001f && !Physics2D.CircleCast(current, radius, slide.normalized, slide.magnitude, LayerMask.GetMask("Default")))
                player.position = new Vector3(current.x + slide.x, current.y, player.position.z);
        }
    }

    private void Interact()
    {
        if (phase == Phase.AtCart && Near(cart))
        {
            if (!cartOpen)
            {
                cartOpen = true;
                message = "Carrinho aberto. Prepare a casquinha pedida: 1 Morango, 2 Baunilha, 3 Chocolate.";
            }
            else if (selectedFlavor == "Morango")
            {
                phase = Phase.ReadyToLeave;
                cartOpen = false;
                message = "Casquinha de morango pronta. Corra até a bifurcação e escolha a rota pela altura do caminho.";
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
                message = "Entrega concluída! Você recebeu 12 moedas uma única vez. Volte ao carrinho para instalar a placa.";
            }
            return;
        }
        if (phase == Phase.Paid && Near(cart))
        {
            phase = Phase.Complete;
            if (sign != null) sign.gameObject.SetActive(true);
            if (signLabel != null) signLabel.text = shopName.ToUpperInvariant();
            message = "Placa instalada! A primeira entrega inaugurou " + shopName + ".";
        }
    }

    private void SelectFlavor(string flavor)
    {
        selectedFlavor = flavor;
        message = flavor == "Morango" ? "Morango escolhido. Pressione E perto do carrinho para montar e fechar a casquinha." : "A florista pediu morango. Escolha 1 para atender ao pedido.";
    }

    private void UpdateMission()
    {
        float x = player.position.x;
        if ((phase == Phase.ReadyToLeave || phase == Phase.Outbound) && x > 5.5f && !routeChosen)
        {
            if (player.position.y < -0.25f)
            {
                routeChosen = true; routeName = "Orla"; phase = Phase.Outbound;
                message = "Rota da orla: calçada ampla e livre. Siga até a florista.";
            }
            else if (player.position.y > 0.55f)
            {
                routeChosen = true; routeName = "Feira"; phase = Phase.Outbound;
                message = "Atalho da feira: contorne as caixas e os pedestres.";
            }
        }
        if (phase == Phase.Outbound && x > DeliveryX - 1.2f)
        {
            phase = Phase.AtFlorist;
            message = "A florista está aqui. Pressione E para entregar a casquinha de morango.";
        }
        if ((phase == Phase.Paid || phase == Phase.Returning) && x < ReturnX + 1.1f)
        {
            phase = Phase.Paid;
            message = "Pagamento recebido: 12 moedas. Escolha o nome na placa abaixo e pressione E perto do carrinho.";
        }
        if ((phase == Phase.ReadyToLeave || phase == Phase.Outbound) && x > 2f && phase == Phase.ReadyToLeave)
            phase = Phase.Outbound;
        if (phase == Phase.AtFlorist && x < DeliveryX - 2f) phase = Phase.Outbound;
        if (phase == Phase.Paid && x > ReturnX + 1.1f) phase = Phase.Returning;
    }

    private bool Near(Transform target) => target != null && Vector2.Distance(player.position, target.position) <= interactionRadius;

    private void Animate(Vector2 input)
    {
        if (playerSprite == null) return;
        if (Mathf.Abs(input.x) > 0.1f) playerSprite.flipX = input.x < 0;
        if (input.sqrMagnitude < 0.01f) { playerSprite.sprite = idle; animationClock = 0; return; }
        animationClock += Time.deltaTime * ((Keys != null && (Keys.leftShiftKey.isPressed || Keys.rightShiftKey.isPressed)) ? 9 : 6);
        if (animationClock > 0.14f) { frameB = !frameB; animationClock = 0; }
        playerSprite.sprite = frameB ? walkA : walkB;
    }

    private void FollowCamera()
    {
        if (sceneCamera == null) return;
        Vector3 target = new Vector3(player.position.x + 1.2f, 0.2f, sceneCamera.transform.position.z);
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, target, 1 - Mathf.Exp(-5f * Time.deltaTime));
    }

    private void OnGUI()
    {
        GUI.skin.box.fontSize = 16;
        GUI.skin.label.fontSize = 16;
        GUI.skin.button.fontSize = 15;
        GUI.Box(new Rect(16, 14, 440, 106), "GIRL POWER — PRIMEIRA ENTREGA\n" + message + "\nSem cronômetro  •  WASD/setas: andar  •  Shift: correr  •  E: interagir");
        if (coins > 0 || phase == Phase.Complete) GUI.Label(new Rect(20, 126, 300, 28), "Pagamento: " + coins + " moedas   |   Rota: " + (routeName == "" ? "a escolher" : routeName));
        if (phase == Phase.Paid)
        {
            GUI.Label(new Rect(20, 160, 130, 28), "Nome da sorveteria:");
            shopName = GUI.TextField(new Rect(152, 160, 230, 26), shopName, 24);
        }
        if (phase == Phase.AtCart && cartOpen)
        {
            if (GUI.Button(new Rect(16, 130, 125, 38), "1 · Morango")) SelectFlavor("Morango");
            if (GUI.Button(new Rect(146, 130, 125, 38), "2 · Baunilha")) SelectFlavor("Baunilha");
            if (GUI.Button(new Rect(276, 130, 125, 38), "3 · Chocolate")) SelectFlavor("Chocolate");
        }
    }

}
