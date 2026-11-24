using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controlador principal do jogo "Girl Power - Sweet World":
/// - Tela inicial idêntica à capa (imagem completa + JOGAR e SAIR no canto inferior direito, HUD oculto)
/// - Ondas dinâmicas de cupcakes, monstrinhos de gelatina (verde, rosa, roxo), faixas turbo,
///   poças de caramelo e casinhas de gengibre para entrega festiva!
/// - HUD limpo, legível e sem sobreposições (CanvasScaler 1920x1080 Match 0.5)
/// - Telas de Pausa, Vitória (2000 pts) e Fim de Jogo com reinício completo.
/// </summary>
public sealed class SweetWorldGameManager : MonoBehaviour
{
    public static SweetWorldGameManager Instance { get; private set; }

    [Header("Referências da Cena")]
    public SweetCartController cart;
    public Sprite enemyGreenSprite;
    public Sprite enemyPinkSprite;
    public Sprite enemyPurpleSprite;
    public Sprite cupcakeSprite;
    public Sprite gingerbreadHouseSprite;
    public Sprite lollipopSprite;

    public int Score { get; private set; }
    public int CargoCount { get; private set; }
    public const int MaxCargo = 5;
    public const int VictoryScore = 2000;

    public bool IsPlaying => state == GameState.Playing;
    public float CurrentScrollSpeed
    {
        get
        {
            if (state == GameState.Menu) return 2.2f; // Mundo vivo ao fundo mesmo no menu!
            if (state != GameState.Playing) return 0f;
            float baseSpd = 6.4f + Mathf.Min(3.4f, Score * 0.0012f);
            if (cart != null && cart.IsGirlPowerActive) baseSpd *= 1.38f;
            if (cart != null && cart.SlowTimer > 0f) baseSpd *= 0.58f;
            return baseSpd;
        }
    }

    private enum GameState { Menu, Playing, Paused, GameOver, Victory }
    private GameState state = GameState.Menu;

    private float nextEnemyTime;
    private float nextCupcakeTime;
    private float nextHouseTime;
    private float nextZoneTime;
    private bool endlessMode;

    // UI References (criados limpos em runtime para zero dependência quebrada!)
    private GameObject menuPanel;
    private GameObject hudRoot;
    private GameObject pausePanel;
    private GameObject endPanel;
    private GameObject bannerBox;
    private Text txtLife;
    private Image imgLifeFill;
    private Text txtCargo;
    private Text txtScore;
    private Text txtSpeed;
    private Text txtBanner;
    private Text txtEndTitle;
    private Text txtEndBody;
    private Button btnContinueEndless;
    private float bannerTimer;

    private readonly float[] roadLanes = { -1.45f, -2.55f, -3.55f };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (GetComponent<SweetAudio>() == null) gameObject.AddComponent<SweetAudio>();
    }

    private void Start()
    {
        BuildCleanUI();
        EnterMenu();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && (state == GameState.Playing || state == GameState.Paused))
            TogglePause();
#else
        if (Input.GetKeyDown(KeyCode.Escape) && (state == GameState.Playing || state == GameState.Paused))
            TogglePause();
#endif

        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            if (bannerTimer <= 0f && bannerBox != null) bannerBox.SetActive(false);
        }

        if (state != GameState.Playing) return;

        UpdateHUD();

        // Spawners ritmados na estrada
        if (Time.time >= nextCupcakeTime)
        {
            SpawnCupcakeOrStar();
            nextCupcakeTime = Time.time + Random.Range(1.45f, 2.35f);
        }
        if (Time.time >= nextEnemyTime)
        {
            SpawnJellyEnemy();
            float pace = Mathf.Max(1.05f, 2.25f - Score * 0.00045f);
            nextEnemyTime = Time.time + Random.Range(pace * 0.8f, pace * 1.25f);
        }
        if (Time.time >= nextZoneTime)
        {
            SpawnRoadZone();
            nextZoneTime = Time.time + Random.Range(6.5f, 9.5f);
        }
        if (Time.time >= nextHouseTime)
        {
            SpawnGingerbreadDeliveryHouse();
            nextHouseTime = Time.time + Random.Range(12.5f, 16.0f);
        }
    }

    public void StartGame()
    {
        ClearDynamicWorldObjects();
        Score = 0;
        CargoCount = 0;
        endlessMode = false;
        if (cart != null) cart.ResetCart();

        state = GameState.Playing;
        Time.timeScale = 1f;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (hudRoot != null) hudRoot.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);

        nextCupcakeTime = Time.time + 0.8f;
        nextEnemyTime = Time.time + 2.2f;
        nextZoneTime = Time.time + 5.5f;
        nextHouseTime = Time.time + 8.5f;

        ShowBanner("COLETE CUPCAKES E ENTREGUE NAS CASINHAS DE GENGIBRE!");
        UpdateHUD();
    }

    public void EnterMenu()
    {
        ClearDynamicWorldObjects();
        state = GameState.Menu;
        Time.timeScale = 1f;
        if (menuPanel != null) menuPanel.SetActive(true);
        if (hudRoot != null) hudRoot.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);
        if (bannerBox != null) bannerBox.SetActive(false);
    }

    public void TogglePause()
    {
        if (state == GameState.Playing)
        {
            state = GameState.Paused;
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
        }
        else if (state == GameState.Paused)
        {
            state = GameState.Playing;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }

    public void TriggerGameOver()
    {
        if (state == GameState.GameOver) return;
        state = GameState.GameOver;
        Time.timeScale = 0f;
        if (endPanel != null) endPanel.SetActive(true);
        if (txtEndTitle != null) txtEndTitle.text = "FIM DE JOGO!";
        if (txtEndBody != null) txtEndBody.text = "PONTUAÇÃO FINAL: " + Score + " PTS\nTente novamente e leve ainda mais sorvetes!";
        if (btnContinueEndless != null) btnContinueEndless.gameObject.SetActive(false);
    }

    private void TriggerVictory()
    {
        state = GameState.Victory;
        Time.timeScale = 0f;
        if (endPanel != null) endPanel.SetActive(true);
        if (txtEndTitle != null) txtEndTitle.text = "VITÓRIA DOCE!";
        if (txtEndBody != null) txtEndBody.text = "VOCÊ ALCANÇOU " + Score + " PONTOS!\nO Mundo de Doces está em festa!";
        if (btnContinueEndless != null) btnContinueEndless.gameObject.SetActive(true);
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayDelivery();
    }

    public void ContinueEndless()
    {
        endlessMode = true;
        state = GameState.Playing;
        Time.timeScale = 1f;
        if (endPanel != null) endPanel.SetActive(false);
        ShowBanner("MODO INFINITO! BUSQUE SEU RECORDE!");
    }

    public void CollectIceCream()
    {
        if (CargoCount < MaxCargo)
        {
            CargoCount++;
            AddScore(25, "CUPCAKE COLETADO! (" + CargoCount + "/" + MaxCargo + ")", false);
        }
        else
        {
            AddScore(35, "CARGA CHEIA! ENTREGUE NA PRÓXIMA CASINHA!", true);
        }
    }

    public void DeliverCargo(SweetCartController playerCart)
    {
        int delivered = CargoCount;
        CargoCount = 0;
        int pts = delivered * 150;
        playerCart.Heal(15);
        playerCart.ActivateGirlPower(6.5f);
        if (SweetAudio.Instance != null) SweetAudio.Instance.PlayDelivery();
        AddScore(pts, "ENTREGA PERFEITA! +" + pts + " PTS • GIRL POWER!", true);
    }

    public void AddScore(int pts, string bannerMsg, bool forceBanner)
    {
        Score += pts;
        UpdateHUD();
        if (forceBanner || bannerTimer <= 0f) ShowBanner(bannerMsg);
        if (!endlessMode && Score >= VictoryScore && state == GameState.Playing)
            TriggerVictory();
    }

    public void ShowBanner(string message)
    {
        if (bannerBox == null || txtBanner == null) return;
        txtBanner.text = message;
        bannerBox.SetActive(true);
        bannerTimer = 2.6f;
    }

    private void UpdateHUD()
    {
        if (cart != null)
        {
            if (txtLife != null) txtLife.text = "VIDA: " + cart.Health + "%";
            if (imgLifeFill != null) imgLifeFill.fillAmount = cart.Health / (float)cart.MaxHealth;
        }
        if (txtCargo != null) txtCargo.text = "SORVETES: " + CargoCount + " / " + MaxCargo;
        if (txtScore != null) txtScore.text = "PONTOS: " + Score;
        if (txtSpeed != null)
        {
            int kmh = Mathf.RoundToInt(CurrentScrollSpeed * 9.5f);
            txtSpeed.text = "VEL: " + kmh + " km/h";
        }
    }

    private void SpawnCupcakeOrStar()
    {
        bool star = Random.value < 0.14f;
        float y = roadLanes[Random.Range(0, roadLanes.Length)];
        GameObject go = new GameObject(star ? "MagicStar" : "CupcakePickup");
        go.transform.position = new Vector3(11.5f, y, 0f);
        go.transform.localScale = Vector3.one * (star ? 0.95f : 1.05f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = star ? SweetWorldArtUtility.GetStarSprite() : (cupcakeSprite != null ? cupcakeSprite : SweetWorldArtUtility.GetStarSprite());
        sr.color = star ? new Color(1f, 0.90f, 0.25f, 1f) : Color.white;
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = Mathf.RoundToInt(-y * 100f);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.62f;

        SweetCollectible c = go.AddComponent<SweetCollectible>();
        c.isTurboStar = star;
    }

    private void SpawnJellyEnemy()
    {
        int kind = Random.Range(0, 3);
        float y = roadLanes[Random.Range(0, roadLanes.Length)];
        GameObject go = new GameObject("JellyEnemy_" + kind);
        go.transform.position = new Vector3(11.8f, y, 0f);
        go.transform.localScale = Vector3.one * (kind == 2 ? 1.25f : 1.08f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = kind == 0 ? enemyGreenSprite : (kind == 1 ? enemyPinkSprite : enemyPurpleSprite);
        if (sr.sprite == null) sr.sprite = SweetWorldArtUtility.GetMagicBubbleSprite();
        sr.sortingLayerName = "Entities";
        sr.sortingOrder = Mathf.RoundToInt(-y * 100f);

        // Sombra suave no chão
        GameObject sh = new GameObject("Shadow");
        sh.transform.SetParent(go.transform, false);
        sh.transform.localPosition = new Vector3(0f, -0.58f, 0f);
        sh.transform.localScale = new Vector3(1.1f, 0.55f, 1f);
        SpriteRenderer shSr = sh.AddComponent<SpriteRenderer>();
        shSr.sprite = SweetWorldArtUtility.GetSoftShadowSprite();
        shSr.sortingLayerName = "RoadDetails";
        shSr.sortingOrder = 5;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.58f;

        SweetJellyEnemy e = go.AddComponent<SweetJellyEnemy>();
        e.hp = (kind == 2) ? 2 : 1;
        e.extraSpeed = (kind == 1) ? 2.6f : (kind == 2 ? 1.0f : 1.7f);
        e.splashColor = kind == 0 ? new Color(0.32f, 0.94f, 0.36f, 1f) : (kind == 1 ? new Color(1f, 0.32f, 0.62f, 1f) : new Color(0.68f, 0.34f, 0.98f, 1f));
    }

    private void SpawnRoadZone()
    {
        bool boost = Random.value < 0.62f;
        float y = roadLanes[Random.Range(0, roadLanes.Length)];
        GameObject go = new GameObject(boost ? "SpeedBoostZone" : "CaramelSlowZone");
        go.transform.position = new Vector3(12.2f, y, 0f);
        go.transform.localScale = boost ? new Vector3(1.25f, 0.85f, 1f) : new Vector3(1.15f, 0.92f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = boost ? SweetWorldArtUtility.GetSpeedBoostMatSprite() : SweetWorldArtUtility.GetCaramelPuddleSprite();
        sr.sortingLayerName = "RoadDetails";
        sr.sortingOrder = 4;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.4f, 0.85f);

        SweetRoadZone z = go.AddComponent<SweetRoadZone>();
        z.isSpeedBoost = boost;
    }

    private void SpawnGingerbreadDeliveryHouse()
    {
        GameObject root = new GameObject("GingerbreadDeliveryStop");
        root.transform.position = new Vector3(13.2f, -1.55f, 0f);

        // Tapete brilhante de entrega na pista superior!
        SpriteRenderer mat = root.AddComponent<SpriteRenderer>();
        mat.sprite = SweetWorldArtUtility.GetGlowSprite();
        mat.color = new Color(1f, 0.52f, 0.88f, 0.62f);
        mat.sortingLayerName = "RoadDetails";
        mat.sortingOrder = 6;

        BoxCollider2D trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(2.8f, 2.4f);

        // Casinha de Gengibre na calçada superior!
        GameObject house = new GameObject("GingerbreadCottage");
        house.transform.SetParent(root.transform, false);
        house.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        house.transform.localScale = Vector3.one * 1.45f;
        SpriteRenderer hSr = house.AddComponent<SpriteRenderer>();
        hSr.sprite = gingerbreadHouseSprite;
        hSr.sortingLayerName = "Entities";
        hSr.sortingOrder = 20;

        // Cone gigante flutuante indicando ENTREGA sem precisar ler!
        GameObject beacon = new GameObject("DeliveryConeBeacon");
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 3.25f, 0f);
        beacon.transform.localScale = Vector3.one * 0.92f;
        SpriteRenderer bSr = beacon.AddComponent<SpriteRenderer>();
        bSr.sprite = SweetWorldArtUtility.GetDeliveryConeBeaconSprite();
        bSr.sortingLayerName = "VFX";
        bSr.sortingOrder = 30;

        GingerbreadDeliveryStop stop = root.AddComponent<GingerbreadDeliveryStop>();
        stop.houseVisual = house.transform;
        stop.coneBeacon = beacon.transform;

        if (CargoCount > 0) ShowBanner("CASINHA DE GENGIBRE À FRENTE! ENCOSTE NA FAIXA SUPERIOR!");
    }

    private static void ClearDynamicWorldObjects()
    {
        foreach (var e in FindObjectsOfType<SweetJellyEnemy>()) Destroy(e.gameObject);
        foreach (var c in FindObjectsOfType<SweetCollectible>()) Destroy(c.gameObject);
        foreach (var h in FindObjectsOfType<GingerbreadDeliveryStop>()) Destroy(h.gameObject);
        foreach (var z in FindObjectsOfType<SweetRoadZone>()) Destroy(z.gameObject);
        foreach (var b in FindObjectsOfType<MagicBubbleProjectile>()) Destroy(b.gameObject);
    }

    // =========================================================================
    // UI COMPLETA E LIMPA (CAPA EXATA + HUD 1920x1080 SEM COLISÕES + FIM DE JOGO)
    // =========================================================================
    private void BuildCleanUI()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        GameObject canvasObj = new GameObject("SweetWorldCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Sprite pill = SweetWorldArtUtility.GetUiPillSprite();

        // 1. MENU INICIAL (CAPA EXATA + JOGAR E SAIR NO CANTO INFERIOR DIREITO!)
        menuPanel = CreatePanel(canvasObj.transform, "MenuInicial", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, null);
        Texture2D coverTex = Resources.Load<Texture2D>("title_screen_cover");
        if (coverTex != null)
        {
            Image bg = menuPanel.GetComponent<Image>();
            bg.sprite = Sprite.Create(coverTex, new Rect(0, 0, coverTex.width, coverTex.height), new Vector2(0.5f, 0.5f));
            bg.preserveAspect = false;
        }
        else menuPanel.GetComponent<Image>().color = new Color(0.18f, 0.06f, 0.36f, 1f);

        CreateGlossyButton(menuPanel.transform, "BtnJogar", "JOGAR", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-215f, 205f), new Vector2(340f, 92f),
            new Color(1f, 0.34f, 0.72f, 1f), pill, uiFont, StartGame);
        CreateGlossyButton(menuPanel.transform, "BtnSair", "SAIR", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-215f, 95f), new Vector2(340f, 82f),
            new Color(0.38f, 0.24f, 0.64f, 0.96f), pill, uiFont, () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

        // 2. HUD IN-GAME (ZERO SOBREPOSIÇÃO, LETRA GRANDE E LEGÍVEL!)
        hudRoot = CreatePanel(canvasObj.transform, "HUD_InGame", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0), null);
        hudRoot.GetComponent<Image>().raycastTarget = false;

        GameObject topBar = CreatePanel(hudRoot.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(1420f, 82f),
            new Color(0.18f, 0.07f, 0.34f, 0.84f), pill);

        txtLife = CreateText(topBar.transform, "TxtLife", "VIDA: 100%", uiFont, 28, TextAnchor.MiddleLeft, new Vector2(-560f, 14f), new Vector2(240f, 36f));
        GameObject lifeBg = CreatePanel(topBar.transform, "LifeBarBg", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-555f, -18f), new Vector2(230f, 18f),
            new Color(0.32f, 0.16f, 0.48f, 1f), pill);
        GameObject lifeFillObj = CreatePanel(lifeBg.transform, "LifeFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(1f, 0.38f, 0.76f, 1f), pill);
        imgLifeFill = lifeFillObj.GetComponent<Image>();
        imgLifeFill.type = Image.Type.Filled;
        imgLifeFill.fillMethod = Image.FillMethod.Horizontal;

        txtCargo = CreateText(topBar.transform, "TxtCargo", "SORVETES: 0 / 5", uiFont, 32, TextAnchor.MiddleCenter, new Vector2(-175f, 0f), new Vector2(360f, 56f));
        txtScore = CreateText(topBar.transform, "TxtScore", "PONTOS: 0", uiFont, 34, TextAnchor.MiddleCenter, new Vector2(190f, 0f), new Vector2(320f, 56f));
        txtScore.color = new Color(1f, 0.90f, 0.36f, 1f);
        txtSpeed = CreateText(topBar.transform, "TxtSpeed", "VEL: 60 km/h", uiFont, 30, TextAnchor.MiddleRight, new Vector2(530f, 0f), new Vector2(280f, 56f));
        txtSpeed.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Botão Pausa canto superior direito (bem separado da TopBar!)
        CreateGlossyButton(hudRoot.transform, "BtnPausa", "||", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-72f, -54f), new Vector2(78f, 78f),
            new Color(1f, 0.38f, 0.76f, 1f), pill, uiFont, TogglePause);

        // Banner central escuro (#1A0B2E 85% opacidade) para avisos alegres sem atrapalhar a visão!
        bannerBox = CreatePanel(hudRoot.transform, "BannerAvisos", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(960f, 64f),
            new Color(0.102f, 0.043f, 0.180f, 0.86f), pill);
        txtBanner = CreateText(bannerBox.transform, "TxtBanner", "", uiFont, 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900f, 54f));
        txtBanner.color = new Color(1f, 0.92f, 0.45f, 1f);
        bannerBox.SetActive(false);

        // Botão ATIRAR grande no canto inferior direito + dica de controles sutil no inferior esquerdo
        CreateGlossyButton(hudRoot.transform, "BtnAtirar", "ATIRAR", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-145f, 105f), new Vector2(220f, 120f),
            new Color(1f, 0.34f, 0.74f, 0.95f), pill, uiFont, () => { if (cart != null) cart.TriggerMobileShoot(); });

        GameObject hintBox = CreatePanel(hudRoot.transform, "HintControles", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(285f, 44f), new Vector2(520f, 44f),
            new Color(0.10f, 0.04f, 0.20f, 0.72f), pill);
        CreateText(hintBox.transform, "TxtHint", "W/S ou SETAS: Dirigir   •   ESPAÇO: Bolhas Mágicas", uiFont, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 36f));

        // 3. PAINEL PAUSA
        pausePanel = CreatePanel(canvasObj.transform, "PainelPausa", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.09f, 0.03f, 0.18f, 0.84f), null);
        GameObject pCard = CreatePanel(pausePanel.transform, "CardPausa", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 420f),
            new Color(0.20f, 0.08f, 0.38f, 0.95f), pill);
        CreateText(pCard.transform, "TitlePausa", "JOGO PAUSADO", uiFont, 48, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(560f, 70f));
        CreateGlossyButton(pCard.transform, "BtnContinuar", "CONTINUAR", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(360f, 82f),
            new Color(1f, 0.34f, 0.72f, 1f), pill, uiFont, TogglePause);
        CreateGlossyButton(pCard.transform, "BtnMenuPausa", "MENU INICIAL", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(360f, 76f),
            new Color(0.42f, 0.24f, 0.68f, 1f), pill, uiFont, EnterMenu);
        pausePanel.SetActive(false);

        // 4. PAINEL FIM DE JOGO / VITÓRIA (ALTO CONTRASTE 92% OPACIDADE)
        endPanel = CreatePanel(canvasObj.transform, "PainelFim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.03f, 0.16f, 0.90f), null);
        GameObject eCard = CreatePanel(endPanel.transform, "CardFim", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(740f, 520f),
            new Color(0.22f, 0.08f, 0.42f, 0.95f), pill);
        txtEndTitle = CreateText(eCard.transform, "TxtEndTitle", "FIM DE JOGO!", uiFont, 54, TextAnchor.MiddleCenter, new Vector2(0f, 165f), new Vector2(680f, 80f));
        txtEndTitle.color = new Color(1f, 0.88f, 0.32f, 1f);
        txtEndBody = CreateText(eCard.transform, "TxtEndBody", "", uiFont, 30, TextAnchor.MiddleCenter, new Vector2(0f, 55f), new Vector2(660f, 110f));
        btnContinueEndless = CreateGlossyButton(eCard.transform, "BtnInfinito", "CONTINUAR MODO INFINITO", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(460f, 76f),
            new Color(0.32f, 0.78f, 0.54f, 1f), pill, uiFont, ContinueEndless);
        CreateGlossyButton(eCard.transform, "BtnRecomecar", "JOGAR NOVAMENTE", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -142f), new Vector2(460f, 82f),
            new Color(1f, 0.34f, 0.72f, 1f), pill, uiFont, StartGame);
        CreateGlossyButton(eCard.transform, "BtnMenuEnd", "MENU INICIAL", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -222f), new Vector2(360f, 64f),
            new Color(0.40f, 0.22f, 0.66f, 1f), pill, uiFont, EnterMenu);
        endPanel.SetActive(false);
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color col, Sprite spr)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = col;
        if (spr != null) { img.sprite = spr; img.type = Image.Type.Sliced; }
        return go;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, TextAnchor align, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = align;
        t.color = Color.white;
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.18f, 0.05f, 0.28f, 0.92f);
        ol.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    private static Button CreateGlossyButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color bgCol, Sprite pill, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreatePanel(parent, name, aMin, aMax, pos, size, bgCol, pill);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        CreateText(go.transform, "Label", label, font, 34, TextAnchor.MiddleCenter, Vector2.zero, size);
        return btn;
    }
}
