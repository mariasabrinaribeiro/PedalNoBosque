using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool forceMobileUI = true; // Enabled by default in Editor for testing
    private bool useSwipeMode = true; // Toggle between swipe/drag and on-screen buttons

    [Header("Candy Palette Colors")]
    public Color candyPink = new Color(1f, 0.482f, 0.773f, 1f); // #ff7bc5 (Bubblegum Pink)
    public Color candyPurple = new Color(0.368f, 0.235f, 0.502f, 1f); // #5e3c80 (Dark Theme Purple)
    public Color candyLightPurple = new Color(0.545f, 0.373f, 0.663f, 1f); // #8b5fa9 (Light Purple)
    public Color shadowColor = new Color(0.2f, 0.118f, 0.294f, 0.5f); // Deep semi-transparent shadow
    public Color candyGreen = new Color(0.4f, 0.9f, 0.6f, 1f); // For score rewards

    private Transform playButton;
    private Transform pauseButton;
    private Transform exitButton;
    private Canvas mainCanvas;
    private driverpro carDriver;

    // Mobile inputs state (for buttons mode)
    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isForwardPressed = false;
    private bool isReversePressed = false;

    // UI elements references
    private GameObject menuPanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;
    private GameObject joystickContainer;
    private RectTransform joystickKnob;
    private TextMeshProUGUI warningText;
    private float warningFadeTime = -1f;

    // HUD child references
    private Slider healthSlider;
    private TextMeshProUGUI cargoText;
    private TextMeshProUGUI scoreText;

    // Navigation Pointer
    private GameObject navArrow;
    private SpriteRenderer navArrowSR;

    private List<GameObject> mobileHUDButtons = new List<GameObject>();
    private Vector2 startTouchPos;
    private bool isDraggingJoystick = false;
    private int joystickFingerId = -1; // -1 means not dragging. -2 means mouse drag.

    private GameObject debugPanel;
    private TextMeshProUGUI sliderLabel;
    private TextMeshProUGUI logTextArea;
    private List<string> debugLogs = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        // Subscribe to sceneLoaded event so it runs on every reload/scene change
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedCallback;

        // Run initially for the active scene
        InitializeControllerForActiveScene();
    }

    private static void OnSceneLoadedCallback(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        InitializeControllerForActiveScene();
    }

    private static void InitializeControllerForActiveScene()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Check if this is the gameplay scene (has Play, Pause, or Exit buttons)
        Transform play = canvas.transform.Find("Play");
        Transform pause = canvas.transform.Find("Pause");
        Transform exit = canvas.transform.Find("Exit");

        if (play != null || pause != null || exit != null)
        {
            GameObject controllerObj = GameObject.Find("GameUIController");
            if (controllerObj == null)
            {
                controllerObj = new GameObject("GameUIController");
                controllerObj.AddComponent<GameUIController>();
                controllerObj.AddComponent<EnemySpawner>(); // Auto-attach spawner to manage enemies
            }
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Copy sprites to Resources dynamically in editor
#if UNITY_EDITOR
        string resourcesDir = Application.dataPath + "/Resources";
        if (!System.IO.Directory.Exists(resourcesDir))
        {
            System.IO.Directory.CreateDirectory(resourcesDir);
        }
        // Copy pink house sprite
        string srcHouse = Application.dataPath + "/GameDev.tv - Delivery Dash Assets/Houses/house3frontpink.png";
        string destHouse = resourcesDir + "/house3frontpink.png";
        if (System.IO.File.Exists(srcHouse) && !System.IO.File.Exists(destHouse))
        {
            System.IO.File.Copy(srcHouse, destHouse);
        }
#endif

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        SetupUI();
    }

    private void Start()
    {
        FindDriver();
        EnhanceDeliveryZone();
        CreateNavigationArrow();
        DisableNonInteractiveRaycasts();

        // Start game in main menu state (paused)
        SetGameState(true);
    }

    private void Update()
    {
        if (carDriver == null) return;

        // 1. Update HUD stats
        if (healthSlider != null) healthSlider.value = carDriver.health;
        if (cargoText != null) cargoText.text = "SORVETES: " + carDriver.cargo + " / " + carDriver.maxCargo;
        if (scoreText != null) scoreText.text = "PONTOS: " + carDriver.score;

        // 2. Handle Warning fadeout
        if (warningText != null && Time.unscaledTime > warningFadeTime)
        {
            warningText.text = "";
        }

        // 3. Handle Navigation Pointer
        UpdateNavigationPointer();

        // 4. Handle Mobile Touch/Swipe controls if enabled
        if (Time.timeScale > 0.01f)
        {
            if (useSwipeMode)
            {
                HandleSwipeControls();
            }
            else
            {
                // Buttons mode logic
                float steer = (isLeftPressed ? 1f : 0f) + (isRightPressed ? -1f : 0f);
                float move = (isForwardPressed ? 1f : 0f) + (isReversePressed ? -1f : 0f);
                carDriver.SetMobileSteer(steer);
                carDriver.SetMobileMove(move);
            }
        }
    }

    private void FindDriver()
    {
        carDriver = FindObjectOfType<driverpro>();
    }

    private void SetupUI()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null) return;

        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Find standard buttons in scene
        playButton = mainCanvas.transform.Find("Play");
        pauseButton = mainCanvas.transform.Find("Pause");
        exitButton = mainCanvas.transform.Find("Exit");

        // Style buttons
        StyleButton(playButton, "JOGAR", candyPink);
        StyleButton(pauseButton, "||", candyPink);
        StyleButton(exitButton, "SAIR", candyPink);

        // Remove the buggy "Game over" button parented to tree in the scene if it exists!
        // This stops it from showing in world space. We find it by name.
        GameObject buggyGameOver = GameObject.Find("tree2 (10)/Button");
        if (buggyGameOver != null)
        {
            Destroy(buggyGameOver);
        }
        else
        {
            // Try to find any other world space Button named "Button"
            GameObject worldButton = GameObject.Find("Button");
            if (worldButton != null && worldButton.transform.parent != null && !worldButton.transform.parent.name.Contains("Canvas") && !worldButton.transform.parent.name.Contains("Panel"))
            {
                Destroy(worldButton);
            }
        }

        // Create UI panels
        CreateMenuPanel();
        CreateHUDPanel();
        CreateGameOverPanel();
        CreateJoystickUI();

        // Spawn mobile controls
        SpawnMobileHUD();

        // Create Debug Console Panel
        CreateDebugPanel();
    }

    private void CreateMenuPanel()
    {
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        Image panelImage = menuPanel.AddComponent<Image>();

        Sprite coverSprite = Resources.Load<Sprite>("art/title_screen_cover");
        if (coverSprite != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = Vector2.zero; // Fullscreen

            panelImage.sprite = coverSprite;
            panelImage.type = Image.Type.Simple;
        }
        else
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(600, 750);

            panelImage.sprite = CreateRoundedRectSprite(400, 500, 32, new Color(0.368f, 0.235f, 0.502f, 0.85f), candyPink, 4, shadowColor);
            panelImage.type = Image.Type.Sliced;
        }

        // Reparent Play and Exit to this panel
        if (playButton != null)
        {
            playButton.SetParent(menuPanel.transform, false);
            RectTransform playRect = playButton.GetComponent<RectTransform>();
            playRect.anchoredPosition = new Vector2(0, 120);
            playRect.sizeDelta = new Vector2(350, 95);
        }

        if (exitButton != null)
        {
            exitButton.SetParent(menuPanel.transform, false);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchoredPosition = new Vector2(0, -220);
            exitRect.sizeDelta = new Vector2(350, 95);
        }

        // Add Control mode toggle button
        GameObject toggleBtnObj = new GameObject("BtnControlToggle");
        toggleBtnObj.transform.SetParent(menuPanel.transform, false);
        RectTransform toggleRect = toggleBtnObj.AddComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(0, -50);
        toggleRect.sizeDelta = new Vector2(350, 95);

        Button toggleBtn = toggleBtnObj.AddComponent<Button>();
        Image toggleImg = toggleBtnObj.AddComponent<Image>();
        toggleImg.sprite = CreateRoundedRectSprite(350, 95, 20, candyLightPurple, candyPurple, 4, shadowColor);
        toggleImg.type = Image.Type.Simple;

        GameObject toggleTxtObj = new GameObject("Text");
        toggleTxtObj.transform.SetParent(toggleBtnObj.transform, false);
        RectTransform txtRect = toggleTxtObj.AddComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(350, 95);
        txtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI toggleTxt = toggleTxtObj.AddComponent<TextMeshProUGUI>();
        toggleTxt.text = useSwipeMode ? "CONTROLE: DESLIZAR" : "CONTROLE: BOTÕES";
        toggleTxt.font = GetTMProFont();
        toggleTxt.fontSize = 22;
        toggleTxt.fontStyle = FontStyles.Bold;
        toggleTxt.alignment = TextAlignmentOptions.Center;
        toggleTxt.color = Color.white;
        toggleTxt.outlineColor = candyPurple;
        toggleTxt.outlineWidth = 0.2f;

        toggleBtn.onClick.AddListener(() => {
            useSwipeMode = !useSwipeMode;
            toggleTxt.text = useSwipeMode ? "CONTROLE: DESLIZAR" : "CONTROLE: BOTÕES";
            // Refresh HUD layout
            SetGameState(true);
        });

        AddJuicyAnimations(toggleBtnObj);

        // Add Title Text
        GameObject titleObj = new GameObject("MenuTitle");
        titleObj.transform.SetParent(menuPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 260);
        titleRect.sizeDelta = new Vector2(500, 100);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "GIRL POWER";
        titleText.font = GetTMProFont();
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.outlineColor = candyPurple;
        titleText.outlineWidth = 0.25f;
    }

    private void CreateHUDPanel()
    {
        hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = hudPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(950, 130);

        Image img = hudPanel.AddComponent<Image>();
        img.sprite = CreateRoundedRectSprite(950, 130, 24, new Color(0.368f, 0.235f, 0.502f, 0.8f), candyPink, 3, shadowColor);
        img.type = Image.Type.Sliced;

        // 1. Health slider (Slider de Vida)
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(hudPanel.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(50, 0);
        sliderRect.sizeDelta = new Vector2(250, 30);

        healthSlider = sliderObj.AddComponent<Slider>();

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = Vector2.zero; // Stretch
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = CreateRoundedRectSprite(250, 30, 8, candyPurple, candyPurple, 0, new Color(0,0,0,0));

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = CreateRoundedRectSprite(250, 30, 8, candyPink, candyPink, 0, new Color(0,0,0,0));

        healthSlider.fillRect = fillRect;
        healthSlider.minValue = 0;
        healthSlider.maxValue = 100;
        healthSlider.value = 100;

        // Health text label
        GameObject hpLabelObj = new GameObject("Label");
        hpLabelObj.transform.SetParent(sliderObj.transform, false);
        RectTransform hpLabelRect = hpLabelObj.AddComponent<RectTransform>();
        hpLabelRect.anchoredPosition = new Vector2(0, 35);
        hpLabelRect.sizeDelta = new Vector2(250, 40);
        TextMeshProUGUI hpLabel = hpLabelObj.AddComponent<TextMeshProUGUI>();
        hpLabel.text = "VIDA";
        hpLabel.font = GetTMProFont();
        hpLabel.fontSize = 20;
        hpLabel.fontStyle = FontStyles.Bold;
        hpLabel.alignment = TextAlignmentOptions.Left;
        hpLabel.color = Color.white;
        hpLabel.outlineColor = candyPurple;
        hpLabel.outlineWidth = 0.2f;

        // 2. Cargo Text
        GameObject cargoObj = new GameObject("TxtCargo");
        cargoObj.transform.SetParent(hudPanel.transform, false);
        RectTransform cargoRect = cargoObj.AddComponent<RectTransform>();
        cargoRect.anchorMin = new Vector2(0.5f, 0.5f);
        cargoRect.anchorMax = new Vector2(0.5f, 0.5f);
        cargoRect.pivot = new Vector2(0.5f, 0.5f);
        cargoRect.anchoredPosition = new Vector2(-60, 0);
        cargoRect.sizeDelta = new Vector2(300, 80);

        cargoText = cargoObj.AddComponent<TextMeshProUGUI>();
        cargoText.text = "SORVETES: 0 / 3";
        cargoText.font = GetTMProFont();
        cargoText.fontSize = 24;
        cargoText.fontStyle = FontStyles.Bold;
        cargoText.alignment = TextAlignmentOptions.Center;
        cargoText.color = Color.white;
        cargoText.outlineColor = candyPurple;
        cargoText.outlineWidth = 0.2f;

        // 3. Score Text
        GameObject scoreObj = new GameObject("TxtScore");
        scoreObj.transform.SetParent(hudPanel.transform, false);
        RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(1f, 0.5f);
        scoreRect.anchorMax = new Vector2(1f, 0.5f);
        scoreRect.pivot = new Vector2(1f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(-50, 0);
        scoreRect.sizeDelta = new Vector2(250, 80);

        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "PONTOS: 0";
        scoreText.font = GetTMProFont();
        scoreText.fontSize = 24;
        scoreText.fontStyle = FontStyles.Bold;
        scoreText.alignment = TextAlignmentOptions.Right;
        scoreText.color = Color.white;
        scoreText.outlineColor = candyPurple;
        scoreText.outlineWidth = 0.2f;

        // 4. Center Screen Warning Text
        GameObject warnObj = new GameObject("TxtWarning");
        warnObj.transform.SetParent(mainCanvas.transform, false);
        RectTransform warnRect = warnObj.AddComponent<RectTransform>();
        warnRect.anchorMin = new Vector2(0.5f, 0.7f);
        warnRect.anchorMax = new Vector2(0.5f, 0.7f);
        warnRect.pivot = new Vector2(0.5f, 0.5f);
        warnRect.anchoredPosition = Vector2.zero;
        warnRect.sizeDelta = new Vector2(900, 150);

        warningText = warnObj.AddComponent<TextMeshProUGUI>();
        warningText.text = "";
        warningText.font = GetTMProFont();
        warningText.fontSize = 32;
        warningText.fontStyle = FontStyles.Bold;
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.color = candyPink;
        warningText.outlineColor = candyPurple;
        warningText.outlineWidth = 0.25f;
    }

    private void CreateGameOverPanel()
    {
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600, 750);

        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.sprite = CreateRoundedRectSprite(400, 500, 32, new Color(0.368f, 0.235f, 0.502f, 0.95f), candyPink, 4, shadowColor);
        panelImage.type = Image.Type.Sliced;

        // GameOver Title Text
        GameObject titleObj = new GameObject("GameOverTitle");
        titleObj.transform.SetParent(gameOverPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 240);
        titleRect.sizeDelta = new Vector2(500, 100);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "FIM DE JOGO";
        titleText.font = GetTMProFont();
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = candyPink;
        titleText.outlineColor = candyPurple;
        titleText.outlineWidth = 0.25f;

        // Score Text Label
        GameObject scoreLabelObj = new GameObject("GameOverScore");
        scoreLabelObj.transform.SetParent(gameOverPanel.transform, false);
        RectTransform scoreLabelRect = scoreLabelObj.AddComponent<RectTransform>();
        scoreLabelRect.anchoredPosition = new Vector2(0, 110);
        scoreLabelRect.sizeDelta = new Vector2(500, 80);

        TextMeshProUGUI scoreLabel = scoreLabelObj.AddComponent<TextMeshProUGUI>();
        scoreLabel.text = "PONTUAÇÃO: 0";
        scoreLabel.font = GetTMProFont();
        scoreLabel.fontSize = 28;
        scoreLabel.fontStyle = FontStyles.Bold;
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.color = Color.white;
        scoreLabel.outlineColor = candyPurple;
        scoreLabel.outlineWidth = 0.2f;

        // Restart button (RECOMEÇAR)
        GameObject restartBtnObj = new GameObject("BtnRestart");
        restartBtnObj.transform.SetParent(gameOverPanel.transform, false);
        RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
        restartRect.anchoredPosition = new Vector2(0, -60);
        restartRect.sizeDelta = new Vector2(350, 95);

        Button restartBtn = restartBtnObj.AddComponent<Button>();
        Image restartImg = restartBtnObj.AddComponent<Image>();
        restartImg.sprite = CreateRoundedRectSprite(350, 95, 20, candyPink, candyPurple, 4, shadowColor);
        restartImg.type = Image.Type.Simple;

        GameObject restartTxtObj = new GameObject("Text");
        restartTxtObj.transform.SetParent(restartBtnObj.transform, false);
        RectTransform restartTxtRect = restartTxtObj.AddComponent<RectTransform>();
        restartTxtRect.sizeDelta = new Vector2(350, 95);
        restartTxtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI restartTxt = restartTxtObj.AddComponent<TextMeshProUGUI>();
        restartTxt.text = "RECOMEÇAR";
        restartTxt.font = GetTMProFont();
        restartTxt.fontSize = 24;
        restartTxt.fontStyle = FontStyles.Bold;
        restartTxt.alignment = TextAlignmentOptions.Center;
        restartTxt.color = Color.white;
        restartTxt.outlineColor = candyPurple;
        restartTxt.outlineWidth = 0.2f;

        restartBtn.onClick.AddListener(() => RestartLevel());
        AddJuicyAnimations(restartBtnObj);

        // Exit button (SAIR)
        GameObject exitBtnObj = new GameObject("BtnGameOverExit");
        exitBtnObj.transform.SetParent(gameOverPanel.transform, false);
        RectTransform exitRect = exitBtnObj.AddComponent<RectTransform>();
        exitRect.anchoredPosition = new Vector2(0, -200);
        exitRect.sizeDelta = new Vector2(350, 95);

        Button exitBtn = exitBtnObj.AddComponent<Button>();
        Image exitImg = exitBtnObj.AddComponent<Image>();
        exitImg.sprite = CreateRoundedRectSprite(350, 95, 20, candyLightPurple, candyPurple, 4, shadowColor);
        exitImg.type = Image.Type.Simple;

        GameObject exitTxtObj = new GameObject("Text");
        exitTxtObj.transform.SetParent(exitBtnObj.transform, false);
        RectTransform exitTxtRect = exitTxtObj.AddComponent<RectTransform>();
        exitTxtRect.sizeDelta = new Vector2(350, 95);
        exitTxtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI exitTxt = exitTxtObj.AddComponent<TextMeshProUGUI>();
        exitTxt.text = "SAIR";
        exitTxt.font = GetTMProFont();
        exitTxt.fontSize = 24;
        exitTxt.fontStyle = FontStyles.Bold;
        exitTxt.alignment = TextAlignmentOptions.Center;
        exitTxt.color = Color.white;
        exitTxt.outlineColor = candyPurple;
        exitTxt.outlineWidth = 0.2f;

        exitBtn.onClick.AddListener(() => QuitGame());
        AddJuicyAnimations(exitBtnObj);

        gameOverPanel.SetActive(false);
    }

    private void CreateJoystickUI()
    {
        joystickContainer = new GameObject("JoystickContainer");
        joystickContainer.transform.SetParent(mainCanvas.transform, false);

        RectTransform containerRect = joystickContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero; // Fullscreen touch capture area

        // Visual Joystick Base
        GameObject baseObj = new GameObject("Base");
        baseObj.transform.SetParent(joystickContainer.transform, false);
        RectTransform baseRect = baseObj.AddComponent<RectTransform>();
        baseRect.sizeDelta = new Vector2(180, 180);
        Image baseImg = baseObj.AddComponent<Image>();
        baseImg.sprite = CreateRoundedRectSprite(180, 180, 90, new Color(0.368f, 0.235f, 0.502f, 0.4f), candyPink, 3, new Color(0,0,0,0));

        // Visual Joystick Knob
        GameObject knobObj = new GameObject("Knob");
        knobObj.transform.SetParent(baseObj.transform, false);
        joystickKnob = knobObj.AddComponent<RectTransform>();
        joystickKnob.sizeDelta = new Vector2(80, 80);
        joystickKnob.anchoredPosition = Vector2.zero;
        Image knobImg = knobObj.AddComponent<Image>();
        knobImg.sprite = CreateRoundedRectSprite(80, 80, 40, candyPink, candyPurple, 2, new Color(0,0,0,0));

        joystickContainer.SetActive(false);
    }

    private void StyleButton(Transform btnTrans, string labelText, Color buttonColor)
    {
        if (btnTrans == null) return;

        Button button = btnTrans.GetComponent<Button>();
        if (button == null) button = btnTrans.gameObject.AddComponent<Button>();

        Image img = btnTrans.GetComponent<Image>();
        if (img == null) img = btnTrans.gameObject.AddComponent<Image>();

        img.sprite = CreateRoundedRectSprite(300, 90, 20, buttonColor, candyPurple, 4, shadowColor);
        img.type = Image.Type.Simple;

        button.transition = Button.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        Text txt = btnTrans.GetComponentInChildren<Text>();
        if (txt != null) Destroy(txt.gameObject); // replace with TMP

        TextMeshProUGUI tmpTxt = btnTrans.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpTxt == null)
        {
            GameObject tObj = new GameObject("TextTMP");
            tObj.transform.SetParent(btnTrans, false);
            RectTransform tr = tObj.AddComponent<RectTransform>();
            tr.sizeDelta = btnTrans.GetComponent<RectTransform>().sizeDelta;
            tr.anchoredPosition = Vector2.zero;

            tmpTxt = tObj.AddComponent<TextMeshProUGUI>();
        }

        tmpTxt.text = labelText;
        tmpTxt.fontSize = 24;
        tmpTxt.color = Color.white;
        tmpTxt.alignment = TextAlignmentOptions.Center;
        tmpTxt.fontStyle = FontStyles.Bold;
        tmpTxt.font = GetTMProFont();
        tmpTxt.outlineColor = candyPurple;
        tmpTxt.outlineWidth = 0.2f;

        // Click listeners
        button.onClick.RemoveAllListeners();
        if (btnTrans.name == "Play")
        {
            button.onClick.AddListener(() => SetGameState(false));
        }
        else if (btnTrans.name == "Pause")
        {
            button.onClick.AddListener(() => SetGameState(true));
        }
        else if (btnTrans.name == "Exit")
        {
            button.onClick.AddListener(() => QuitGame());
        }

        AddJuicyAnimations(btnTrans.gameObject);
    }

    private void AddJuicyAnimations(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 1.08f; });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one; });
        trigger.triggers.Add(exit);

        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 0.95f; });
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry();
        up.eventID = EventTriggerType.PointerUp;
        up.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 1.08f; });
        trigger.triggers.Add(up);
    }

    private void SetGameState(bool isPaused)
    {
        if (menuPanel != null) menuPanel.SetActive(isPaused);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(!isPaused);

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!isPaused);
            RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1, 1);
            pauseRect.anchorMax = new Vector2(1, 1);
            pauseRect.pivot = new Vector2(1, 1);
            pauseRect.anchoredPosition = new Vector2(-40, -40);
            pauseRect.sizeDelta = new Vector2(80, 80);
        }

        // Handle mobile HUD visibility
        bool isMobile = Application.isMobilePlatform || forceMobileUI;
        bool showButtonsHUD = isMobile && !useSwipeMode && !isPaused;

        foreach (var hudBtn in mobileHUDButtons)
        {
            if (hudBtn.name == "BtnShoot")
            {
                hudBtn.SetActive(isMobile && !isPaused);
            }
            else if (hudBtn.name == "BtnDebugToggle")
            {
                hudBtn.SetActive(!isPaused);
            }
            else
            {
                hudBtn.SetActive(showButtonsHUD);
            }
        }

        // Hide joystick when paused
        if (joystickContainer != null)
        {
            joystickContainer.SetActive(false);
            isDraggingJoystick = false;
        }

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0f;

        if (hudPanel != null) hudPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        foreach (var hudBtn in mobileHUDButtons)
        {
            hudBtn.SetActive(false);
        }

        if (joystickContainer != null) joystickContainer.SetActive(false);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            TextMeshProUGUI finalScoreText = gameOverPanel.transform.Find("GameOverScore").GetComponent<TextMeshProUGUI>();
            if (finalScoreText != null && carDriver != null)
            {
                finalScoreText.text = "PONTUAÇÃO FINAL: " + carDriver.score;
            }
        }
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowWarning(string msg)
    {
        if (warningText != null)
        {
            warningText.text = msg;
            warningText.color = candyPink;
            warningFadeTime = Time.unscaledTime + 2f;
        }
    }

    public void ShowDeliveryMessage(string msg)
    {
        if (warningText != null)
        {
            warningText.text = msg;
            warningText.color = candyGreen;
            warningFadeTime = Time.unscaledTime + 2.5f;
        }
    }

    private void SpawnMobileHUD()
    {
        // Spawns Left, Right, Accelerate, Reverse, and Shoot buttons
        GameObject leftBtn = CreateHUDButton("BtnLeft", "◀", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 80), 120, 120);
        AddHUDEvent(leftBtn, (pressed) => isLeftPressed = pressed);
        mobileHUDButtons.Add(leftBtn);

        GameObject rightBtn = CreateHUDButton("BtnRight", "▶", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(220, 80), 120, 120);
        AddHUDEvent(rightBtn, (pressed) => isRightPressed = pressed);
        mobileHUDButtons.Add(rightBtn);

        GameObject accelBtn = CreateHUDButton("BtnAccelerate", "▲", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-220, 80), 120, 120);
        AddHUDEvent(accelBtn, (pressed) => isForwardPressed = pressed);
        mobileHUDButtons.Add(accelBtn);

        GameObject revBtn = CreateHUDButton("BtnReverse", "▼", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 80), 120, 120);
        AddHUDEvent(revBtn, (pressed) => isReversePressed = pressed);
        mobileHUDButtons.Add(revBtn);

        // ATIRAR (Shoot) button placed on the right side above drive buttons
        GameObject shootBtn = CreateHUDButton("BtnShoot", "ATIRAR", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, 230), 140, 140);
        AddHUDShootEvent(shootBtn);
        mobileHUDButtons.Add(shootBtn);
    }

    private GameObject CreateHUDButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, float width, float height)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width, height);

        Image img = btnObj.AddComponent<Image>();
        Color semiTransPink = new Color(candyPink.r, candyPink.g, candyPink.b, 0.65f);
        img.sprite = CreateRoundedRectSprite((int)width, (int)height, 24, semiTransPink, candyPurple, 4, new Color(0,0,0,0));
        img.type = Image.Type.Simple;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(width, height);

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.font = GetTMProFont();
        txt.fontSize = text.Length > 2 ? 24 : 40;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.outlineColor = candyPurple;
        txt.outlineWidth = 0.2f;

        btnObj.AddComponent<CanvasGroup>();
        AddJuicyAnimations(btnObj);

        return btnObj;
    }

    private void AddHUDEvent(GameObject btnObj, Action<bool> action)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener((data) => { action(true); });
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry();
        up.eventID = EventTriggerType.PointerUp;
        up.callback.AddListener((data) => { action(false); });
        trigger.triggers.Add(up);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => { action(false); });
        trigger.triggers.Add(exit);
    }

    private void AddHUDShootEvent(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener((data) => { if (carDriver != null) carDriver.SetMobileShoot(true); });
        trigger.triggers.Add(down);

        EventTrigger.Entry up = new EventTrigger.Entry();
        up.eventID = EventTriggerType.PointerUp;
        up.callback.AddListener((data) => { if (carDriver != null) carDriver.SetMobileShoot(false); });
        trigger.triggers.Add(up);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => { if (carDriver != null) carDriver.SetMobileShoot(false); });
        trigger.triggers.Add(exit);
    }

    private void HandleSwipeControls()
    {
        if (!joystickContainer.activeSelf)
        {
            joystickContainer.SetActive(true);
        }

        RectTransform baseRect = joystickContainer.transform.Find("Base").GetComponent<RectTransform>();

        // Check touches for multi-touch support using new Input System's Touchscreen
        bool processedTouch = false;
        Touchscreen touchscreen = Touchscreen.current;

        if (touchscreen != null)
        {
            // Check if there are any active touches (phase is not None)
            bool hasActiveTouch = false;
            for (int i = 0; i < touchscreen.touches.Count; i++)
            {
                if (touchscreen.touches[i].phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.None)
                {
                    hasActiveTouch = true;
                    break;
                }
            }

            if (hasActiveTouch)
            {
                // 1. If we are already dragging, check if this touch is our joystick touch
                if (isDraggingJoystick && joystickFingerId >= 0)
                {
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var t = touchscreen.touches[i];
                        var phase = t.phase.ReadValue();
                        
                        if (phase == UnityEngine.InputSystem.TouchPhase.None)
                        {
                            continue;
                        }

                        int tid = t.touchId.ReadValue();
                        if (tid == joystickFingerId)
                        {
                            if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                            {
                                StopJoystickDrag(baseRect);
                            }
                            else
                            {
                                UpdateJoystickDrag(t.position.ReadValue(), t.delta.ReadValue(), baseRect);
                            }
                            processedTouch = true;
                            break;
                        }
                    }

                    if (!processedTouch)
                    {
                        StopJoystickDrag(baseRect);
                    }
                }
                // 2. If we are NOT dragging, look for a new touch that is not over UI
                else if (!isDraggingJoystick)
                {
                    for (int i = 0; i < touchscreen.touches.Count; i++)
                    {
                        var t = touchscreen.touches[i];
                        var phase = t.phase.ReadValue();
                        
                        if (phase == UnityEngine.InputSystem.TouchPhase.Began || phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                        {
                            Vector2 touchPos = t.position.ReadValue();
                            
                            // Check if touch is over UI buttons
                            if (IsPointerOverUI(touchPos))
                            {
                                continue;
                            }

                            int tid = t.touchId.ReadValue();
                            StartJoystickDrag(touchPos, tid, baseRect);
                            processedTouch = true;
                            break;
                        }
                    }
                }
            }
        }

        // If no touchscreen touch was processed, fall back to Mouse/Trackpad input
        if (!processedTouch)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                bool leftPressed = mouse.leftButton.isPressed;
                Vector2 mousePos = mouse.position.ReadValue();

                if (isDraggingJoystick && joystickFingerId == -2)
                {
                    if (leftPressed)
                    {
                        UpdateJoystickDrag(mousePos, mouse.delta.ReadValue(), baseRect);
                    }
                    else
                    {
                        StopJoystickDrag(baseRect);
                    }
                }
                else if (!isDraggingJoystick && leftPressed)
                {
                    if (IsPointerOverUI(mousePos))
                    {
                        // Over UI, do nothing
                    }
                    else
                    {
                        StartJoystickDrag(mousePos, -2, baseRect);
                    }
                }
                else if (isDraggingJoystick)
                {
                    StopJoystickDrag(baseRect);
                }
            }
            else if (isDraggingJoystick)
            {
                StopJoystickDrag(baseRect);
            }
        }
    }

    private void StartJoystickDrag(Vector2 pos, int fingerId, RectTransform baseRect)
    {
        isDraggingJoystick = true;
        joystickFingerId = fingerId;
        startTouchPos = pos;
        
        baseRect.position = pos;
        joystickKnob.anchoredPosition = Vector2.zero;
        baseRect.gameObject.SetActive(true);
    }

    private void UpdateJoystickDrag(Vector2 pos, Vector2 delta, RectTransform baseRect)
    {
        Vector2 displacement = pos - startTouchPos;
        float maxDrag = 100f; // maximum displacement radius
        Vector2 clampedDelta = Vector2.ClampMagnitude(displacement, maxDrag);
        joystickKnob.anchoredPosition = clampedDelta;

        if (carDriver != null)
        {
            carDriver.ApplySwipeDelta(delta);
        }
    }

    private void StopJoystickDrag(RectTransform baseRect)
    {
        isDraggingJoystick = false;
        joystickFingerId = -1;
        
        if (baseRect != null)
        {
            baseRect.gameObject.SetActive(false);
        }
        
        if (carDriver != null)
        {
            carDriver.SetMobileSteer(0f);
            carDriver.SetMobileMove(0f);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        
        foreach (var result in results)
        {
            if (result.gameObject != null)
            {
                if (joystickContainer != null && (result.gameObject == joystickContainer || result.gameObject.transform.IsChildOf(joystickContainer.transform)))
                {
                    continue;
                }

                // Check if the object or any of its parents has a Button or EventTrigger component
                Button btn = result.gameObject.GetComponentInParent<Button>();
                EventTrigger trigger = result.gameObject.GetComponentInParent<EventTrigger>();
                
                if (btn != null || trigger != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void EnhanceDeliveryZone()
    {
        // Find all delivery spots
        GameObject[] customers = GameObject.FindGameObjectsWithTag("Customer");

        foreach (GameObject cust in customers)
        {
            // Do NOT change the customer sprite/scale to avoid blocking roads.
            // Just spawn a flat, glowing pink candy circle centered on the delivery spot!
            GameObject matObj = new GameObject("CandyDeliveryZoneMat");
            matObj.transform.position = cust.transform.position;
            matObj.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            SpriteRenderer matSR = matObj.AddComponent<SpriteRenderer>();
            Color semiPink = new Color(candyPink.r, candyPink.g, candyPink.b, 0.55f);
            
            // Create a perfect 128x128 circle by setting cornerRadius to 64
            matSR.sprite = CreateRoundedRectSprite(128, 128, 64, semiPink, candyPurple, 4, new Color(0, 0, 0, 0));
            matSR.sortingOrder = 1; // Under the car (which is sorting order 2 or higher)
        }
    }

    private void CreateNavigationArrow()
    {
        if (carDriver == null) return;

        navArrow = new GameObject("NavPointerArrow");
        navArrow.transform.localScale = Vector3.one * 0.7f;

        navArrowSR = navArrow.AddComponent<SpriteRenderer>();
        // Draw pixel-art arrow pointing UP
        navArrowSR.sprite = CreateArrowSprite(48, 48, candyPink, candyPurple, 4);
        navArrowSR.sortingOrder = 6;
        navArrow.SetActive(false);
    }

    private void OnDestroy()
    {
        if (navArrow != null)
        {
            Destroy(navArrow);
        }
    }

    private void UpdateNavigationPointer()
    {
        if (carDriver == null || navArrow == null || Time.timeScale < 0.01f)
        {
            if (navArrow != null) navArrow.SetActive(false);
            return;
        }

        GameObject target = null;

        // If carrying cargo, point to the nearest delivery customer. Else point to nearest package.
        if (carDriver.cargo > 0)
        {
            GameObject[] customers = GameObject.FindGameObjectsWithTag("Customer");
            target = FindNearest(customers);
            
            // Set glowing hot pink for delivery pointer
            navArrowSR.color = candyPink;
        }
        else
        {
            GameObject[] packages = GameObject.FindGameObjectsWithTag("package");
            target = FindNearest(packages);
            
            // Set glowing yellow for pickup pointer
            navArrowSR.color = new Color(1f, 0.9f, 0.2f, 1f);
        }

        if (target != null)
        {
            navArrow.SetActive(true);

            Vector3 direction = target.transform.position - carDriver.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // Subtract 90 since arrow sprite points UP

            // Orbit positioning in world space
            navArrow.transform.position = carDriver.transform.position + direction.normalized * 1.5f;
            navArrow.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            navArrow.SetActive(false);
        }
    }

    private GameObject FindNearest(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0) return null;

        GameObject nearest = null;
        float minDist = Mathf.Infinity;
        Vector3 playerPos = carDriver.transform.position;

        foreach (GameObject t in targets)
        {
            if (t == null || t == carDriver.gameObject) continue; // EXCLUDE the player car itself!
            float dist = Vector3.Distance(t.transform.position, playerPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t;
            }
        }
        return nearest;
    }

    // Dynamic sprite generators
    private Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius, Color baseColor, Color borderColor, int borderWidth, Color shadowColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        int shadowOffset = 6;
        if (shadowColor.a > 0.01f)
        {
            DrawRoundedRect(tex, shadowOffset, -shadowOffset, width, height, cornerRadius, shadowColor, shadowColor, 0);
        }

        DrawRoundedRect(tex, 0, 0, width, height, cornerRadius, baseColor, borderColor, borderWidth);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void DrawRoundedRect(Texture2D tex, int offsetX, int offsetY, int w, int h, int r, Color fillColor, Color borderColor, int borderWidth)
    {
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int targetX = x + offsetX;
                int targetY = y + offsetY;

                if (targetX < 0 || targetX >= tex.width || targetY < 0 || targetY >= tex.height)
                    continue;

                bool inside = true;
                if (x < r && y < r)
                    inside = (x - r) * (x - r) + (y - r) * (y - r) <= r * r;
                else if (x >= w - r && y < r)
                    inside = (x - (w - r)) * (x - (w - r)) + (y - r) * (y - r) <= r * r;
                else if (x < r && y >= h - r)
                    inside = (x - r) * (x - r) + (y - (h - r)) * (y - (h - r)) <= r * r;
                else if (x >= w - r && y >= h - r)
                    inside = (x - (w - r)) * (x - (w - r)) + (y - (h - r)) * (y - (h - r)) <= r * r;

                if (inside)
                {
                    bool isBorder = false;
                    if (borderWidth > 0)
                    {
                        if (x < borderWidth || x >= w - borderWidth || y < borderWidth || y >= h - borderWidth)
                        {
                            isBorder = true;
                        }
                        else if (x < r || x >= w - r || y < r || y >= h - r)
                        {
                            int innerR = r - borderWidth;
                            bool insideInner = true;
                            if (x < r && y < r)
                                insideInner = (x - r) * (x - r) + (y - r) * (y - r) <= innerR * innerR;
                            else if (x >= w - r && y < r)
                                insideInner = (x - (w - r)) * (x - (w - r)) + (y - r) * (y - r) <= innerR * innerR;
                            else if (x < r && y >= h - r)
                                insideInner = (x - r) * (x - r) + (y - (h - r)) * (y - (h - r)) <= innerR * innerR;
                            else if (x >= w - r && y >= h - r)
                                insideInner = (x - (w - r)) * (x - (w - r)) + (y - (h - r)) * (y - (h - r)) <= innerR * innerR;

                            if (!insideInner) isBorder = true;
                        }
                    }
                    tex.SetPixel(targetX, targetY, isBorder ? borderColor : fillColor);
                }
            }
        }
    }

    private Sprite CreateArrowSprite(int w, int h, Color fillColor, Color borderColor, int borderWidth)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        // Draw pixel arrow pointing UP
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inside = false;
                // Triangle top head
                if (y >= h / 2)
                {
                    float ratio = (y - h / 2f) / (h / 2f);
                    int halfW = Mathf.RoundToInt((1f - ratio) * (w / 2f));
                    if (Mathf.Abs(x - w / 2) <= halfW)
                    {
                        inside = true;
                    }
                }
                // Rect bottom stem
                else
                {
                    int stemW = w / 6;
                    if (Mathf.Abs(x - w / 2) <= stemW)
                    {
                        inside = true;
                    }
                }

                if (inside)
                {
                    // Basic border outline check
                    bool isBorder = false;
                    if (borderWidth > 0)
                    {
                        if (y < borderWidth || y >= h - borderWidth || x < borderWidth || x >= w - borderWidth)
                        {
                            isBorder = true;
                        }
                        else
                        {
                            // Check stem boundaries
                            int stemW = w / 6;
                            if (y < h / 2 && (Mathf.Abs(x - w / 2) >= stemW - borderWidth))
                            {
                                isBorder = true;
                            }
                            // Check triangle boundaries
                            else if (y >= h / 2)
                            {
                                float ratio = (y - h / 2f) / (h / 2f);
                                int halfW = Mathf.RoundToInt((1f - ratio) * (w / 2f));
                                if (Mathf.Abs(x - w / 2) >= halfW - borderWidth)
                                {
                                    isBorder = true;
                                }
                            }
                        }
                    }
                    tex.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    private TMP_FontAsset GetTMProFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }

    private void DisableNonInteractiveRaycasts()
    {
        Graphic[] graphics = FindObjectsOfType<Graphic>(true);
        foreach (var graphic in graphics)
        {
            // Determine if the graphic is inside MenuPanel, GameOverPanel, or DebugPanel
            bool isInsideMenuOrGameOver = false;
            Transform parent = graphic.transform;
            while (parent != null)
            {
                if (parent.name == "MenuPanel" || parent.name == "GameOverPanel" || parent.name == "DebugPanel")
                {
                    isInsideMenuOrGameOver = true;
                    break;
                }
                parent = parent.parent;
            }

            if (!isInsideMenuOrGameOver &&
                graphic.GetComponentInParent<Button>() == null &&
                graphic.GetComponentInParent<EventTrigger>() == null &&
                graphic.GetComponentInParent<TMP_InputField>() == null &&
                !graphic.name.Contains("Joystick") &&
                !graphic.name.Contains("Knob") &&
                !graphic.name.Contains("Base"))
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string color = "white";
        if (type == LogType.Error || type == LogType.Exception) color = "#ff5555";
        else if (type == LogType.Warning) color = "#ffcc00";

        debugLogs.Add($"<color={color}>[{type}] {logString}</color>");
        if (debugLogs.Count > 18)
        {
            debugLogs.RemoveAt(0);
        }

        UpdateLogText();
    }

    private void UpdateLogText()
    {
        if (logTextArea != null)
        {
            logTextArea.text = string.Join("\n", debugLogs);
        }
    }

    private void CreateDebugPanel()
    {
        // 1. Create Debug Toggle Button
        GameObject toggleBtnObj = CreateHUDButton("BtnDebugToggle", "DEBUG", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -180), 120, 60);
        Image toggleImg = toggleBtnObj.GetComponent<Image>();
        toggleImg.sprite = CreateRoundedRectSprite(120, 60, 12, new Color(0.2f, 0.15f, 0.25f, 0.85f), candyPurple, 2, new Color(0,0,0,0));
        
        Button btn = toggleBtnObj.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => {
            if (debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
                if (debugPanel.activeSelf && carDriver != null)
                {
                    Slider s = debugPanel.transform.Find("SensitivitySlider").GetComponent<Slider>();
                    s.value = carDriver.GetSwipeSpeedMultiplier();
                    sliderLabel.text = "SENSIBILIDADE: " + s.value.ToString("F1");
                }
            }
        });
        mobileHUDButtons.Add(toggleBtnObj);

        // 2. Create Debug Panel (on the left side of the screen)
        debugPanel = new GameObject("DebugPanel");
        debugPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.5f);
        panelRect.anchorMax = new Vector2(0, 0.5f);
        panelRect.pivot = new Vector2(0, 0.5f);
        panelRect.anchoredPosition = new Vector2(20, 0);
        panelRect.sizeDelta = new Vector2(480, 800);

        Image panelImg = debugPanel.AddComponent<Image>();
        panelImg.sprite = CreateRoundedRectSprite(480, 800, 24, new Color(0.15f, 0.1f, 0.2f, 0.95f), candyPink, 3, shadowColor);
        panelImg.type = Image.Type.Sliced;

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(debugPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 350);
        titleRect.sizeDelta = new Vector2(440, 50);

        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "PAINEL DE DEPURAÇÃO";
        titleTxt.font = GetTMProFont();
        titleTxt.fontSize = 24;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = Color.white;

        // Sensitivity Label
        GameObject labelObj = new GameObject("SensitivityLabel");
        labelObj.transform.SetParent(debugPanel.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 280);
        labelRect.sizeDelta = new Vector2(440, 40);

        sliderLabel = labelObj.AddComponent<TextMeshProUGUI>();
        sliderLabel.text = "SENSIBILIDADE: 6.5";
        sliderLabel.font = GetTMProFont();
        sliderLabel.fontSize = 20;
        sliderLabel.fontStyle = FontStyles.Bold;
        sliderLabel.alignment = TextAlignmentOptions.Center;
        sliderLabel.color = candyPink;

        // Slider
        GameObject sliderObj = new GameObject("SensitivitySlider");
        sliderObj.transform.SetParent(debugPanel.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(0, 230);
        sliderRect.sizeDelta = new Vector2(400, 30);

        Slider slider = sliderObj.AddComponent<Slider>();

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = CreateRoundedRectSprite(400, 30, 8, candyPurple, candyPurple, 0, new Color(0,0,0,0));

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = CreateRoundedRectSprite(400, 30, 8, candyPink, candyPink, 0, new Color(0,0,0,0));

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = Vector2.zero;

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(40, 40);
        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.sprite = CreateRoundedRectSprite(40, 40, 20, Color.white, candyPurple, 2, new Color(0,0,0,0));

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 1f;
        slider.maxValue = 30f;
        slider.value = 6.5f;

        slider.onValueChanged.AddListener((val) => {
            if (carDriver != null)
            {
                carDriver.SetSwipeSpeedMultiplier(val);
            }
            sliderLabel.text = "SENSIBILIDADE: " + val.ToString("F1");
        });

        // Console area title
        GameObject consoleTitleObj = new GameObject("ConsoleTitle");
        consoleTitleObj.transform.SetParent(debugPanel.transform, false);
        RectTransform consoleTitleRect = consoleTitleObj.AddComponent<RectTransform>();
        consoleTitleRect.anchoredPosition = new Vector2(0, 160);
        consoleTitleRect.sizeDelta = new Vector2(440, 40);

        TextMeshProUGUI consoleTitleTxt = consoleTitleObj.AddComponent<TextMeshProUGUI>();
        consoleTitleTxt.text = "LOGS DO CONSOLE (ERROS):";
        consoleTitleTxt.font = GetTMProFont();
        consoleTitleTxt.fontSize = 18;
        consoleTitleTxt.fontStyle = FontStyles.Bold;
        consoleTitleTxt.alignment = TextAlignmentOptions.Left;
        consoleTitleTxt.color = Color.white;

        // Console background panel
        GameObject consoleBgObj = new GameObject("ConsoleBackground");
        consoleBgObj.transform.SetParent(debugPanel.transform, false);
        RectTransform consoleBgRect = consoleBgObj.AddComponent<RectTransform>();
        consoleBgRect.anchoredPosition = new Vector2(0, -110);
        consoleBgRect.sizeDelta = new Vector2(440, 460);

        Image consoleBgImg = consoleBgObj.AddComponent<Image>();
        consoleBgImg.sprite = CreateRoundedRectSprite(440, 460, 12, new Color(0.08f, 0.05f, 0.12f, 0.9f), candyPurple, 2, new Color(0,0,0,0));

        // Console Text
        GameObject consoleTxtObj = new GameObject("ConsoleText");
        consoleTxtObj.transform.SetParent(consoleBgObj.transform, false);
        RectTransform consoleTxtRect = consoleTxtObj.AddComponent<RectTransform>();
        consoleTxtRect.anchorMin = Vector2.zero;
        consoleTxtRect.anchorMax = Vector2.one;
        consoleTxtRect.sizeDelta = new Vector2(-20, -20);
        consoleTxtRect.anchoredPosition = Vector2.zero;

        logTextArea = consoleTxtObj.AddComponent<TextMeshProUGUI>();
        logTextArea.font = GetTMProFont();
        logTextArea.fontSize = 14;
        logTextArea.alignment = TextAlignmentOptions.TopLeft;
        logTextArea.color = Color.white;
        logTextArea.overflowMode = TextOverflowModes.Masking;
        logTextArea.enableWordWrapping = true;

        // Initialize logs view
        UpdateLogText();

        // Start closed
        debugPanel.SetActive(false);
    }
}
