using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador cooperativo em tempo real das 3 amigas de "O Avesso das Horas - A Corrida da Meia-Noite":
/// - Íria (1): Dirige o carrinho na pista inferior e salta pela rampa do pirulito para a Pista Elevada!
/// - Nara (2): Pilota a mariposa mecânica até a engrenagem do pirulito para baixar a haste!
/// - Suri (3): Grava o movimento de descida da haste e o repete (tecla R ou botão) na rampa e na cancela!
/// Todas permanecem fisicamente ativas no cenário ao mesmo tempo!
/// </summary>
public sealed class AvessoTrioController : MonoBehaviour
{
    public static AvessoTrioController Instance { get; private set; }

    public enum Friend { Iria = 0, Nara = 1, Suri = 2 }
    public Friend ActiveFriend { get; private set; } = Friend.Iria;

    [Header("Personagens & Objetos da Cena")]
    public Transform iriaCart;
    public Transform naraMoth;
    public Transform suriMarker;
    public LollipopRampMechanism lollipopRamp;
    public ElevatedTrackAndGate elevatedSystem;
    public Camera mainCam;

    [Header("Parallax da Pista")]
    public Transform skyA;
    public Transform skyB;
    public Transform midA;
    public Transform midB;
    public Transform roadA;
    public Transform roadB;
    public float tileWidth = 18.6f;

    public int LapsCompleted { get; private set; } = 1;
    public bool IsParkedAtShoulder { get; private set; }
    public bool IsOnElevatedTrack { get; private set; }
    public bool SuriRecordedDownMotion { get; private set; }
    public bool ChapterCompleted { get; private set; }

    public string IriaStatus { get; private set; } = "Dirigindo (Volta 1)";
    public string NaraStatus { get; private set; } = "Aguardando no carrinho";
    public string SuriStatus { get; private set; } = "Observando o parque";
    public string StoryBanner { get; private set; } = "Volta 1: Dirija com Íria pela pista da meia-noite!";

    public bool LollipopLockedForPuzzle { get; private set; }
    private float lollipopLoopX = -10.5f;
    private float cartCruiseSpeed = 4.6f;
    private bool isJumpingToElevated;
    private float jumpTimer;
    private Vector3 jumpStartPos;
    private Vector3 jumpEndPos;
    private float softBounceTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (GetComponent<SweetAudio>() == null) gameObject.AddComponent<SweetAudio>();
    }

    private void Start()
    {
        ResetSceneState();
    }

    public void ResetSceneState()
    {
        ActiveFriend = Friend.Iria;
        LapsCompleted = 1;
        IsParkedAtShoulder = false;
        IsOnElevatedTrack = false;
        isJumpingToElevated = false;
        SuriRecordedDownMotion = false;
        ChapterCompleted = false;
        LollipopLockedForPuzzle = false;
        lollipopLoopX = -9.5f;

        if (iriaCart != null) iriaCart.position = new Vector3(4.2f, -2.55f, 0f);
        if (naraMoth != null) naraMoth.position = new Vector3(5.0f, -1.65f, 0f);
        if (suriMarker != null) suriMarker.position = new Vector3(5.6f, -2.35f, 0f);
        if (lollipopRamp != null)
        {
            lollipopRamp.ResetMechanism();
            lollipopRamp.transform.position = new Vector3(lollipopLoopX, -0.82f, 0f);
        }
        if (elevatedSystem != null) elevatedSystem.ResetElevated();

        IriaStatus = "Dirigindo (Volta 1)";
        NaraStatus = "Pronta para voar";
        SuriStatus = "Pronta para gravar";
        StoryBanner = "Volta 1: Dirija com Íria pela pista da meia-noite e observe o Pirulito Gigante!";
    }

    public void SelectFriend(Friend f)
    {
        ActiveFriend = f;
        if ((f == Friend.Nara || f == Friend.Suri) && !LollipopLockedForPuzzle && lollipopRamp != null)
        {
            // Permite à criança começar a cooperar a qualquer momento trazendo o pirulito ao centro!
            LollipopLockedForPuzzle = true;
            lollipopLoopX = -0.35f;
            lollipopRamp.transform.position = new Vector3(-0.35f, -0.82f, 0f);
        }
        if (f == Friend.Nara && !IsOnElevatedTrack)
        {
            IsParkedAtShoulder = true;
            IriaStatus = "No acostamento seguro";
            StoryBanner = "Nara: Voe com a mariposa até a engrenagem dourada do Pirulito e aperte ESPAÇO!";
        }
        else if (f == Friend.Suri)
        {
            StoryBanner = SuriRecordedDownMotion
                ? "Suri: Movimento gravado! Pressione R (ou o botão) perto da rampa ou cancela!"
                : "Suri: Aproxime-se do Pirulito enquanto a haste desce e grave o movimento!";
        }
        else if (f == Friend.Iria)
        {
            IsParkedAtShoulder = false;
            IriaStatus = IsOnElevatedTrack ? "Na Pista Elevada!" : "Dirigindo (Volta " + LapsCompleted + ")";
        }
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayPickup();
    }

    public string GetContextualActionLabel(out Color buttonColor)
    {
        if (ActiveFriend == Friend.Nara)
        {
            bool nearGear = naraMoth != null && lollipopRamp != null &&
                            Vector2.Distance(naraMoth.position, lollipopRamp.GearWorldPosition) < 2.3f;
            if (nearGear)
            {
                buttonColor = new Color(0.22f, 0.80f, 0.74f, 1f);
                return lollipopRamp.NaraWorkingHere ? "SOLTAR ENGRENAGEM" : "GIRAR ENGRENAGEM";
            }
            buttonColor = new Color(0.28f, 0.62f, 0.68f, 0.92f);
            return "VOAR ATÉ A ENGRENAGEM";
        }

        if (ActiveFriend == Friend.Suri)
        {
            if (!SuriRecordedDownMotion)
            {
                buttonColor = new Color(0.62f, 0.34f, 0.92f, 1f);
                return (lollipopRamp != null && lollipopRamp.LowerProgress > 0.25f)
                    ? "GRAVAR DESCIDA!"
                    : "ESPERE NARA BAIXAR A HASTE";
            }
            buttonColor = new Color(0.76f, 0.36f, 0.96f, 1f);
            return IsOnElevatedTrack ? "BAIXAR CANCELA (R)" : "REPETIR RAMPA (R)";
        }

        // Íria ativa:
        if (SuriRecordedDownMotion)
        {
            buttonColor = new Color(0.78f, 0.36f, 0.96f, 1f);
            return IsOnElevatedTrack ? "SURI: BAIXAR CANCELA (R)" : "SURI: REPETIR RAMPA (R)";
        }

        buttonColor = new Color(1f, 0.36f, 0.74f, 1f);
        return IsParkedAtShoulder ? "VOLTAR A DIRIGIR" : "PARAR NO ACOSTAMENTO";
    }

    public void PerformContextualAction()
    {
        if (ActiveFriend == Friend.Nara)
        {
            if (naraMoth != null && lollipopRamp != null &&
                Vector2.Distance(naraMoth.position, lollipopRamp.GearWorldPosition) < 2.3f)
            {
                bool next = !lollipopRamp.NaraWorkingHere;
                lollipopRamp.SetNaraWorking(next);
                NaraStatus = next ? "Girando engrenagem!" : "Voando livre";
                if (next)
                    StoryBanner = "Nara baixou a haste! Troque para Suri (3) para gravar o movimento ou salte com Íria (1)!";
            }
            else if (naraMoth != null && lollipopRamp != null)
            {
                // Ajuda amigável para crianças: aproxima a mariposa suavemente na direção da engrenagem!
                naraMoth.position = Vector3.Lerp(naraMoth.position, lollipopRamp.GearWorldPosition, 0.55f);
            }
            return;
        }

        if (ActiveFriend == Friend.Suri)
        {
            if (!SuriRecordedDownMotion)
            {
                if (lollipopRamp != null && lollipopRamp.LowerProgress > 0.25f)
                {
                    SuriRecordedDownMotion = true;
                    SuriStatus = "Descida gravada!";
                    StoryBanner = "Suri gravou a descida! Volte para Íria (1) e aperte R (ou o botão) na hora do salto!";
                    if (SweetAudio.Instance != null) SweetAudio.Instance.PlayDelivery();
                    SweetConfettiBurst.Spawn(suriMarker != null ? suriMarker.position : Vector3.zero, 14, new Color(0.78f, 0.48f, 1f, 1f));
                }
                else
                {
                    StoryBanner = "Dica: Use Nara (2) para girar a engrenagem do pirulito primeiro!";
                }
            }
            else
            {
                TriggerSuriRecordedMotion();
            }
            return;
        }

        // Íria ativa:
        if (SuriRecordedDownMotion)
        {
            TriggerSuriRecordedMotion();
        }
        else
        {
            IsParkedAtShoulder = !IsParkedAtShoulder;
            IriaStatus = IsParkedAtShoulder ? "Estacionada no acostamento" : "Dirigindo (Volta " + LapsCompleted + ")";
            if (IsParkedAtShoulder)
                StoryBanner = "Carrinho no acostamento! Clique em Nara (2) para pilotar a mariposa até o pirulito!";
        }
    }

    public void TriggerSuriRecordedMotion()
    {
        if (!SuriRecordedDownMotion) return;
        if (IsOnElevatedTrack && elevatedSystem != null)
        {
            elevatedSystem.LowerBarrierBySuri();
            SuriStatus = "Cancelou o vagão!";
            StoryBanner = "Suri reproduziu a descida na cancela mágica!";
        }
        else if (lollipopRamp != null)
        {
            lollipopRamp.TriggerSuriReplay(6.0f);
            SuriStatus = "Repetindo rampa!";
            StoryBanner = "Rampa brilhando! Passe com Íria pela faixa de cima do pirulito para saltar!";
        }
    }

    public void OnWagonSafelyStopped()
    {
        ChapterCompleted = true;
        StoryBanner = "INCRÍVEL! As três detiveram o vagão! Mas... quem acenou no outro carrinho lá embaixo?";
    }

    private void Update()
    {
        HandleHotkeys();
        Vector2 move = ReadMoveInput();

        // 1. Movimento da pista de fundo (somente quando Íria está dirigindo, sem emenda visível!)
        float bgSpeed = (ActiveFriend == Friend.Iria && !IsParkedAtShoulder && !isJumpingToElevated)
            ? (cartCruiseSpeed + Mathf.Max(-2.0f, -move.x * 2.4f))
            : (IsOnElevatedTrack ? 1.4f : 0.6f);

        ScrollBand(skyA, skyB, bgSpeed * 0.08f);
        ScrollBand(midA, midB, bgSpeed * 0.28f);
        ScrollBand(roadA, roadB, bgSpeed * 1.00f);

        // 2. Passagem real pelo mesmo Pirulito Gigante nas Voltas 1 e 2; na 3ª passagem ele gira ANTES de chegarmos!
        if (!IsOnElevatedTrack && !LollipopLockedForPuzzle && !IsParkedAtShoulder && !isJumpingToElevated && lollipopRamp != null)
        {
            lollipopLoopX += bgSpeed * Time.deltaTime;
            if (LapsCompleted >= 3 && lollipopLoopX >= -0.35f)
            {
                lollipopLoopX = -0.35f;
                LollipopLockedForPuzzle = true;
                lollipopRamp.TriggerMysteryPreSpin();
                IsParkedAtShoulder = true;
                IriaStatus = "Volta 3 — Acostamento!";
                StoryBanner = "Volta 3: O pirulito girou ANTES de chegarmos! Troque para Nara (2) e gire a engrenagem!";
            }
            else if (lollipopLoopX > 11.2f)
            {
                lollipopLoopX = -11.2f;
                LapsCompleted++;
                if (LapsCompleted == 2)
                {
                    IriaStatus = "Dirigindo (Volta 2)";
                    StoryBanner = "Volta 2: Ei... nós já passamos por este mesmo pirulito! A pista está se repetindo!";
                }
                else if (LapsCompleted == 3)
                {
                    IriaStatus = "Volta 3 — Olhe o pirulito!";
                    StoryBanner = "Volta 3: Lá vem o mesmo pirulito de novo...";
                }
            }
            lollipopRamp.transform.position = new Vector3(lollipopLoopX, -0.82f, 0f);
        }

        // 3. Atualiza Íria (salto parabólico para o 2º nível ou direção na pista!)
        UpdateIria(move);

        // 4. Atualiza Nara (voo livre da mariposa mecânica ou trabalhando na engrenagem!)
        UpdateNara(move);

        // 5. Atualiza Suri (move-se ou acompanha o carrinho / prepara gravação!)
        UpdateSuri(move);

        // 6. Câmera suave acompanhando o 1º e o 2º nível (mostrando o Eco do Tempo lá embaixo!)
        if (mainCam != null)
        {
            float targetCamY = IsOnElevatedTrack ? 0.85f : 0f;
            float targetSize = IsOnElevatedTrack ? 5.55f : 5.0f;
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, new Vector3(0f, targetCamY, -10f), Time.deltaTime * 3.2f);
            mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, targetSize, Time.deltaTime * 3.2f);
        }
    }

    private void UpdateIria(Vector2 move)
    {
        if (iriaCart == null) return;

        if (isJumpingToElevated)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / 1.05f);
            Vector3 p = Vector3.Lerp(jumpStartPos, jumpEndPos, t);
            p.y += Mathf.Sin(t * Mathf.PI) * 2.15f;
            iriaCart.position = p;
            iriaCart.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -14f);

            if (t >= 1f)
            {
                isJumpingToElevated = false;
                IsOnElevatedTrack = true;
                iriaCart.localRotation = Quaternion.identity;
                IriaStatus = "Na Pista Elevada!";
                StoryBanner = "SEGUNDO NÍVEL! Olhe lá embaixo: nosso outro carrinho passou e acenou! Baixe a cancela (R)!";
                if (elevatedSystem != null) elevatedSystem.TriggerTimeEchoPass();
                if (SweetAudio.Instance != null) SweetAudio.Instance.PlayDelivery();
                SweetConfettiBurst.Spawn(iriaCart.position, 18, new Color(1f, 0.45f, 0.88f, 1f));
            }
            return;
        }

        if (ActiveFriend == Friend.Iria && !IsParkedAtShoulder)
        {
            Vector3 pos = iriaCart.position;
            pos.x = Mathf.Clamp(pos.x + move.x * 5.2f * Time.deltaTime, -6.8f, 6.8f);
            if (IsOnElevatedTrack)
            {
                pos.y = Mathf.Lerp(pos.y, 2.38f, Time.deltaTime * 8f);
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y + move.y * 4.5f * Time.deltaTime, -3.75f, -1.25f);
            }
            iriaCart.position = pos;

            // Detecta chegada na marca do salto do Pirulito Gigante!
            if (!IsOnElevatedTrack && lollipopRamp != null && Mathf.Abs(pos.x - lollipopRamp.transform.position.x) < 1.25f && pos.y > -2.15f)
            {
                if (lollipopRamp.IsRampReady)
                {
                    isJumpingToElevated = true;
                    jumpTimer = 0f;
                    jumpStartPos = pos;
                    jumpEndPos = new Vector3(-2.6f, 2.38f, 0f);
                    if (SweetAudio.Instance != null) SweetAudio.Instance.PlayTurbo();
                }
                else if (softBounceTimer <= 0f)
                {
                    // Sem frustração para crianças: empurrãozinho fofo e dica amigável!
                    softBounceTimer = 1.6f;
                    StoryBanner = "A haste do pirulito ainda está em pé! Use Nara (2) na engrenagem e Suri (3) para gravar!";
                }
            }
        }

        if (softBounceTimer > 0f) softBounceTimer -= Time.deltaTime;
    }

    private void UpdateNara(Vector2 move)
    {
        if (naraMoth == null) return;
        float flutter = Mathf.Sin(Time.time * 18f) * 0.12f;
        naraMoth.localScale = new Vector3(0.58f, 0.58f + flutter * 0.25f, 1f);

        if (ActiveFriend == Friend.Nara)
        {
            Vector3 pos = naraMoth.position;
            pos.x = Mathf.Clamp(pos.x + move.x * 6.4f * Time.deltaTime, -8.0f, 8.0f);
            pos.y = Mathf.Clamp(pos.y + move.y * 6.0f * Time.deltaTime, -4.0f, 4.2f);
            naraMoth.position = pos;
        }
        else if (lollipopRamp != null && lollipopRamp.NaraWorkingHere)
        {
            // Nara permanece fisicamente pousada na engrenagem enquanto você joga com Íria ou Suri!
            Vector3 target = lollipopRamp.GearWorldPosition + new Vector3(0f, 0.25f + Mathf.Sin(Time.time * 8f) * 0.08f, 0f);
            naraMoth.position = Vector3.Lerp(naraMoth.position, target, Time.deltaTime * 6f);
        }
        else if (iriaCart != null)
        {
            Vector3 follow = iriaCart.position + new Vector3(1.1f, 0.95f + Mathf.Sin(Time.time * 4f) * 0.12f, 0f);
            naraMoth.position = Vector3.Lerp(naraMoth.position, follow, Time.deltaTime * 3.5f);
        }
    }

    private void UpdateSuri(Vector2 move)
    {
        if (suriMarker == null) return;
        if (ActiveFriend == Friend.Suri)
        {
            Vector3 pos = suriMarker.position;
            pos.x = Mathf.Clamp(pos.x + move.x * 5.4f * Time.deltaTime, -7.8f, 7.8f);
            pos.y = Mathf.Clamp(pos.y + move.y * 4.8f * Time.deltaTime, -3.9f, 3.2f);
            suriMarker.position = pos;
        }
        else if (iriaCart != null)
        {
            Vector3 companion = iriaCart.position + new Vector3(1.55f, 0.25f + Mathf.Sin(Time.time * 3.2f) * 0.08f, 0f);
            suriMarker.position = Vector3.Lerp(suriMarker.position, companion, Time.deltaTime * 4f);
        }
    }

    private void ScrollBand(Transform a, Transform b, float speed)
    {
        if (a == null || b == null) return;
        float dx = speed * Time.deltaTime;
        // Rola para a DIREITA (+X) porque Íria está olhando e dirigindo para a ESQUERDA (-X), igual à capa!
        a.position += Vector3.right * dx;
        b.position += Vector3.right * dx;
        if (a.position.x >= tileWidth) a.position = new Vector3(b.position.x - tileWidth, a.position.y, a.position.z);
        if (b.position.x >= tileWidth) b.position = new Vector3(a.position.x - tileWidth, b.position.y, b.position.z);
    }

    private void HandleHotkeys()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) SelectFriend(Friend.Iria);
        else if (kb.digit2Key.wasPressedThisFrame) SelectFriend(Friend.Nara);
        else if (kb.digit3Key.wasPressedThisFrame) SelectFriend(Friend.Suri);
        else if (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) PerformContextualAction();
        else if (kb.rKey.wasPressedThisFrame) TriggerSuriRecordedMotion();
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectFriend(Friend.Iria);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectFriend(Friend.Nara);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectFriend(Friend.Suri);
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)) PerformContextualAction();
        else if (Input.GetKeyDown(KeyCode.R)) TriggerSuriRecordedMotion();
#endif
    }

    private static Vector2 ReadMoveInput()
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
}
