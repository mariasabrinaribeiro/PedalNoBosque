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
    private DriverPro carDriver;

    // Mobile inputs state (for buttons mode)
    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isForwardPressed = false;
    private bool isReversePressed = false;

    // UI elements references
    private GameObject menuPanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;
    private GameObject victoryPanel;
    private GameObject joystickContainer;
    private RectTransform joystickKnob;
    private TextMeshProUGUI warningText;
    private float warningFadeTime = -1f;

    // HUD child references
    private Slider healthSlider;
    private TextMeshProUGUI cargoText;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI hpLabelText;

    private RawImage vignetteImage;
    private float nextHeartbeatTime = 0f;

    private GameObject missionPanel;
    private TextMeshProUGUI missionDescText;
    private TextMeshProUGUI missionProgressText;

    private GameObject bossHealthPanel;
    private Image bossHealthFillImage;
    private TextMeshProUGUI bossHealthText;

    // Navigation Pointer
    private GameObject navArrow;
    private SpriteRenderer navArrowSR;
    private List<GameObject> cachedDeliveryPoints = new List<GameObject>();
    private float lastTargetSearchTime = -1f;
    private float targetSearchInterval = 0.25f;
    private GameObject currentNavigationTarget = null;

    private int lastHealthValue = -1;
    private int lastCargoValue = -1;
    private int lastMaxCargoValue = -1;
    private int lastScoreValue = -1;
    private int lastSpeedValue = -1;

    private List<GameObject> mobileHUDButtons = new List<GameObject>();
    private Vector2 startTouchPos;
    private bool isDraggingJoystick = false;
    private int joystickFingerId = -1; // -1 means not dragging. -2 means mouse drag.

    private GameObject gameplayToggleBtnObj;
    private TextMeshProUGUI gameplayToggleTxt;

    private GameObject debugPanel;
    private TextMeshProUGUI sliderLabel;
    private TextMeshProUGUI logTextArea;
    private List<string> debugLogs = new List<string>();

    private float lastTapTime = 0f;
    private float lastClickTime = 0f;

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
        // Check if there is a player car in the scene. If so, this is a gameplay scene!
        DriverPro player = FindObjectOfType<DriverPro>();
        if (player == null) return;

        GameObject controllerObj = GameObject.Find("GameUIController");
        if (controllerObj == null)
        {
            controllerObj = new GameObject("GameUIController");
            controllerObj.AddComponent<GameUIController>();
            controllerObj.AddComponent<EnemySpawner>(); // Auto-attach spawner to manage enemies
            if (FindObjectOfType<MissionManager>() == null)
            {
                controllerObj.AddComponent<MissionManager>();
            }
        }

        // Garante que a Main Camera tenha GameCameraController e comece alinhada ao carro em Z = -10
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            if (mainCam.GetComponent<GameCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<GameCameraController>();
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
        
        if (hpLabelText != null && carDriver.health != lastHealthValue)
        {
            lastHealthValue = carDriver.health;
            hpLabelText.text = "VIDA: " + lastHealthValue + "%";
        }
        
        if (cargoText != null && (carDriver.cargo != lastCargoValue || carDriver.maxCargo != lastMaxCargoValue))
        {
            lastCargoValue = carDriver.cargo;
            lastMaxCargoValue = carDriver.maxCargo;
            cargoText.text = "SORVETES: " + lastCargoValue + " / " + lastMaxCargoValue;
        }
        
        int currentSpeedInt = Mathf.RoundToInt(carDriver.displaySpeed * 10f);
        if (scoreText != null && (carDriver.score != lastScoreValue || currentSpeedInt != lastSpeedValue))
        {
            lastScoreValue = carDriver.score;
            lastSpeedValue = currentSpeedInt;
            scoreText.text = "PONTOS: " + lastScoreValue.ToString("N0") + "   VEL: " + lastSpeedValue + " km/h";

            // Vitória ao atingir 10.000.000 (10 Milhões) ou mais pontos!
            if (lastScoreValue >= 10000000)
            {
                ShowVictory();
            }
        }

        // 2. Handle Vignette low-health pulse and heartbeat audio
        if (vignetteImage != null)
        {
            if (carDriver.health < 20)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                vignetteImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0.55f), new Color(0.65f, 0f, 0f, 0.45f), pulse);

                if (Time.time > nextHeartbeatTime)
                {
                    nextHeartbeatTime = Time.time + 0.8f;
                    if (GameAudioController.Instance != null)
                    {
                        GameAudioController.Instance.PlayHeartbeat();
                    }
                }
            }
            else
            {
                vignetteImage.color = new Color(0f, 0f, 0f, 0.55f);
            }
        }

        // 3. Handle Warning fadeout
        if (warningText != null && Time.unscaledTime > warningFadeTime)
        {
            warningText.text = "";
        }

        // 4. Handle Navigation Pointer
        UpdateNavigationPointer();

        // 5. Handle Mobile Touch/Swipe controls if enabled
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
        carDriver = DriverPro.Instance;
    }

    private void SetupUI()
    {
        // Dynamically attach GameCameraController to the Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindObjectOfType<Camera>();
        if (mainCam != null)
        {
            if (mainCam.GetComponent<GameCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<GameCameraController>();
            }
        }

        mainCanvas = FindObjectOfType<Canvas>();
        
        // Fix EventSystem StandaloneInputModule crash under New Input System
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        if (mainCanvas == null) return;

        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Resolução de referência mais compacta para deixar botões e textos maiores e mais legíveis no celular
        scaler.referenceResolution = Screen.width >= Screen.height ? new Vector2(1500, 844) : new Vector2(844, 1500);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Deactivate original scene UI elements (Panel and Pause) to prevent overlap
        Transform originalPanel = mainCanvas.transform.Find("Panel");
        if (originalPanel != null)
        {
            originalPanel.gameObject.SetActive(false);
        }
        Transform originalPause = mainCanvas.transform.Find("Pause");
        if (originalPause != null)
        {
            originalPause.gameObject.SetActive(false);
        }

        // Find standard buttons in scene
        playButton = mainCanvas.transform.Find("Play");
        pauseButton = mainCanvas.transform.Find("Pause");
        exitButton = mainCanvas.transform.Find("Exit");

        // Dynamically create standard buttons if they are missing (e.g. in new scenes like aula 3)
        if (playButton == null)
        {
            GameObject playObj = new GameObject("Play");
            playObj.transform.SetParent(mainCanvas.transform, false);
            playObj.AddComponent<RectTransform>();
            playButton = playObj.transform;
        }
        if (pauseButton == null)
        {
            GameObject pauseObj = new GameObject("Pause");
            pauseObj.transform.SetParent(mainCanvas.transform, false);
            pauseObj.AddComponent<RectTransform>();
            pauseButton = pauseObj.transform;
        }
        if (exitButton == null)
        {
            GameObject exitObj = new GameObject("Exit");
            exitObj.transform.SetParent(mainCanvas.transform, false);
            exitObj.AddComponent<RectTransform>();
            exitButton = exitObj.transform;
        }

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

        // Destroy the duplicate Canvas-based "Button" (Game over text) if it exists, to avoid overlapping
        if (mainCanvas != null)
        {
            Transform staticGameOverBtn = mainCanvas.transform.Find("Button");
            if (staticGameOverBtn != null)
            {
                Destroy(staticGameOverBtn.gameObject);
            }
        }

        // Create UI panels
        CreateMenuPanel();
        CreateHUDPanel();
        CreateGameOverPanel();
        CreateVictoryPanel();
        CreateJoystickUI();

        CreateVignetteOverlay();
        CreateMissionPanel();
        CreateBossHealthBarUI();

        // Attach TextJuice dynamically to HUD text counters
        if (cargoText != null && cargoText.GetComponent<TextJuice>() == null) cargoText.gameObject.AddComponent<TextJuice>();
        if (scoreText != null && scoreText.GetComponent<TextJuice>() == null) scoreText.gameObject.AddComponent<TextJuice>();
        if (hpLabelText != null && hpLabelText.GetComponent<TextJuice>() == null) hpLabelText.gameObject.AddComponent<TextJuice>();

        // Spawn mobile controls
        SpawnMobileHUD();

        // Create Debug Console Panel
        CreateDebugPanel();

        // Create Gameplay Control Toggle Button
        CreateGameplayControlToggle();
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

        // Reparent Play and Exit to this panel (side-by-side near bottom when cover art is active so logo & girl shine!)
        if (playButton != null)
        {
            playButton.SetParent(menuPanel.transform, false);
            RectTransform playRect = playButton.GetComponent<RectTransform>();
            playRect.anchoredPosition = coverSprite != null ? new Vector2(-175, -310) : new Vector2(0, 120);
            playRect.sizeDelta = coverSprite != null ? new Vector2(290, 82) : new Vector2(350, 95);
        }

        if (exitButton != null)
        {
            exitButton.SetParent(menuPanel.transform, false);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchoredPosition = coverSprite != null ? new Vector2(175, -310) : new Vector2(0, -60);
            exitRect.sizeDelta = coverSprite != null ? new Vector2(290, 82) : new Vector2(350, 95);
        }

        // Only add fallback Title Text if no illustrated cover art is present
        if (coverSprite == null)
        {
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
    }

    private void CreateHUDPanel()
    {
        hudPanel = new GameObject("HUDPanel");
        hudPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = hudPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -16);
        rect.sizeDelta = new Vector2(1040, 150);

        Image img = hudPanel.AddComponent<Image>();
        img.sprite = CreateRoundedRectSprite(1040, 150, 24, new Color(0.368f, 0.235f, 0.502f, 0.85f), candyPink, 3, shadowColor);
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
        hpLabelRect.anchoredPosition = new Vector2(0, 32);
        hpLabelRect.sizeDelta = new Vector2(280, 40);
        hpLabelText = hpLabelObj.AddComponent<TextMeshProUGUI>();
        hpLabelText.text = "VIDA: 100%";
        hpLabelText.font = GetTMProFont();
        hpLabelText.fontSize = 28;
        hpLabelText.fontStyle = FontStyles.Bold;
        hpLabelText.alignment = TextAlignmentOptions.Left;
        hpLabelText.color = Color.white;
        hpLabelText.outlineColor = candyPurple;
        hpLabelText.outlineWidth = 0.2f;

        // 2. Cargo Text
        GameObject cargoObj = new GameObject("TxtCargo");
        cargoObj.transform.SetParent(hudPanel.transform, false);
        RectTransform cargoRect = cargoObj.AddComponent<RectTransform>();
        cargoRect.anchorMin = new Vector2(0.5f, 0.5f);
        cargoRect.anchorMax = new Vector2(0.5f, 0.5f);
        cargoRect.pivot = new Vector2(0.5f, 0.5f);
        cargoRect.anchoredPosition = new Vector2(-45, 0);
        cargoRect.sizeDelta = new Vector2(330, 90);

        cargoText = cargoObj.AddComponent<TextMeshProUGUI>();
        cargoText.text = "SORVETES: 0 / 3";
        cargoText.font = GetTMProFont();
        cargoText.fontSize = 32;
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
        scoreRect.anchoredPosition = new Vector2(-35, 0);
        scoreRect.sizeDelta = new Vector2(360, 90);

        scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "PONTOS: 0";
        scoreText.font = GetTMProFont();
        scoreText.fontSize = 30;
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
        warnRect.sizeDelta = new Vector2(960, 170);

        warningText = warnObj.AddComponent<TextMeshProUGUI>();
        warningText.text = "";
        warningText.font = GetTMProFont();
        warningText.fontSize = 42;
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
        btnTrans.gameObject.SetActive(true); // Ensure it is active at runtime if disabled in scene editor

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
        if (btnObj != null && btnObj.GetComponent<ButtonJuice>() == null)
        {
            btnObj.AddComponent<ButtonJuice>();
        }
    }

    private void SetGameState(bool isPaused)
    {
        if (menuPanel != null) menuPanel.SetActive(isPaused);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(!isPaused);
        if (missionPanel != null) missionPanel.SetActive(!isPaused);
        if (gameplayToggleBtnObj != null) gameplayToggleBtnObj.SetActive(!isPaused);

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
                hudBtn.SetActive(false); // Hidden during clean gameplay
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
        if (missionPanel != null) missionPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        if (gameplayToggleBtnObj != null) gameplayToggleBtnObj.SetActive(false);

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

    private void CreateVictoryPanel()
    {
        victoryPanel = new GameObject("VictoryPanel");
        victoryPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = victoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600, 750);

        Image panelImage = victoryPanel.AddComponent<Image>();
        Color goldTheme = new Color(1f, 0.85f, 0f, 0.95f);
        panelImage.sprite = CreateRoundedRectSprite(400, 500, 32, new Color(0.2f, 0.15f, 0.3f, 0.95f), goldTheme, 4, shadowColor);
        panelImage.type = Image.Type.Sliced;

        // Victory Title Text
        GameObject titleObj = new GameObject("VictoryTitle");
        titleObj.transform.SetParent(victoryPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 240);
        titleRect.sizeDelta = new Vector2(500, 100);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "VITÓRIA!";
        titleText.font = GetTMProFont();
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = goldTheme;
        titleText.outlineColor = candyPurple;
        titleText.outlineWidth = 0.25f;

        // Subtitle Text
        GameObject subObj = new GameObject("VictorySubtitle");
        subObj.transform.SetParent(victoryPanel.transform, false);
        RectTransform subRect = subObj.AddComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 170);
        subRect.sizeDelta = new Vector2(500, 60);

        TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
        subText.text = "VOCÊ VENCEU O BARÃO!";
        subText.font = GetTMProFont();
        subText.fontSize = 20;
        subText.fontStyle = FontStyles.Bold;
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = Color.white;

        // Score Text Label
        GameObject scoreLabelObj = new GameObject("VictoryScore");
        scoreLabelObj.transform.SetParent(victoryPanel.transform, false);
        RectTransform scoreLabelRect = scoreLabelObj.AddComponent<RectTransform>();
        scoreLabelRect.anchoredPosition = new Vector2(0, 90);
        scoreLabelRect.sizeDelta = new Vector2(500, 80);

        TextMeshProUGUI scoreLabel = scoreLabelObj.AddComponent<TextMeshProUGUI>();
        scoreLabel.text = "PONTUAÇÃO: 0";
        scoreLabel.font = GetTMProFont();
        scoreLabel.fontSize = 28;
        scoreLabel.fontStyle = FontStyles.Bold;
        scoreLabel.alignment = TextAlignmentOptions.Center;
        scoreLabel.color = Color.white;

        // Restart button
        GameObject restartBtnObj = new GameObject("BtnVictoryRestart");
        restartBtnObj.transform.SetParent(victoryPanel.transform, false);
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
        restartTxt.text = "JOGAR DE NOVO";
        restartTxt.font = GetTMProFont();
        restartTxt.fontSize = 24;
        restartTxt.fontStyle = FontStyles.Bold;
        restartTxt.alignment = TextAlignmentOptions.Center;
        restartTxt.color = Color.white;

        restartBtn.onClick.AddListener(() => RestartLevel());
        AddJuicyAnimations(restartBtnObj);

        // Exit button
        GameObject exitBtnObj = new GameObject("BtnVictoryExit");
        exitBtnObj.transform.SetParent(victoryPanel.transform, false);
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

        exitBtn.onClick.AddListener(() => QuitGame());
        AddJuicyAnimations(exitBtnObj);

        victoryPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        Time.timeScale = 0f;

        if (hudPanel != null) hudPanel.SetActive(false);
        if (missionPanel != null) missionPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);
        if (gameplayToggleBtnObj != null) gameplayToggleBtnObj.SetActive(false);

        foreach (var hudBtn in mobileHUDButtons)
        {
            hudBtn.SetActive(false);
        }

        if (joystickContainer != null) joystickContainer.SetActive(false);

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            TextMeshProUGUI finalScoreText = victoryPanel.transform.Find("VictoryScore").GetComponent<TextMeshProUGUI>();
            if (finalScoreText != null && carDriver != null)
            {
                finalScoreText.text = "PONTUAÇÃO FINAL: " + carDriver.score.ToString("N0");
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
        // Spawns Left, Right, Accelerate, Reverse, and Shoot buttons (maiores para celular)
        GameObject leftBtn = CreateHUDButton("BtnLeft", "◀", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(85, 85), 145, 145);
        AddHUDEvent(leftBtn, (pressed) => isLeftPressed = pressed);
        mobileHUDButtons.Add(leftBtn);

        GameObject rightBtn = CreateHUDButton("BtnRight", "▶", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(255, 85), 145, 145);
        AddHUDEvent(rightBtn, (pressed) => isRightPressed = pressed);
        mobileHUDButtons.Add(rightBtn);

        GameObject accelBtn = CreateHUDButton("BtnAccelerate", "▲", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-255, 85), 145, 145);
        AddHUDEvent(accelBtn, (pressed) => isForwardPressed = pressed);
        mobileHUDButtons.Add(accelBtn);

        GameObject revBtn = CreateHUDButton("BtnReverse", "▼", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-85, 85), 145, 145);
        AddHUDEvent(revBtn, (pressed) => isReversePressed = pressed);
        mobileHUDButtons.Add(revBtn);

        // ATIRAR (Shoot) button placed on the right side above drive buttons
        GameObject shootBtn = CreateHUDButton("BtnShoot", "ATIRAR", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-165, 270), 170, 170);
        AddHUDShootEvent(shootBtn);
        mobileHUDButtons.Add(shootBtn);
    }

    private GameObject CreateHUDButton(string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, float width, float height)
    {
        // 1. Parent button captures events with a larger touch target hitbox (25% larger)
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(width * 1.25f, height * 1.25f);

        Image raycastImg = btnObj.AddComponent<Image>();
        raycastImg.color = Color.clear;

        // 2. Visual container holds the actual graphics and animates scale
        GameObject visualContainer = new GameObject("Visuals");
        visualContainer.transform.SetParent(btnObj.transform, false);
        RectTransform visualRect = visualContainer.AddComponent<RectTransform>();
        visualRect.anchoredPosition = Vector2.zero;
        visualRect.sizeDelta = new Vector2(width, height);

        Image visualImg = visualContainer.AddComponent<Image>();
        Color semiTransPink = new Color(candyPink.r, candyPink.g, candyPink.b, 0.65f);
        visualImg.sprite = CreateRoundedRectSprite((int)width, (int)height, 24, semiTransPink, candyPurple, 4, new Color(0,0,0,0));
        visualImg.type = Image.Type.Simple;

        // 3. Text label inside visuals
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(visualContainer.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(width, height);

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.font = GetTMProFont();
        txt.fontSize = text.Length > 2 ? 32 : 52;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.outlineColor = candyPurple;
        txt.outlineWidth = 0.2f;

        btnObj.AddComponent<CanvasGroup>();
        
        // Add ButtonJuice to parent (will animate "Visuals" child)
        btnObj.AddComponent<ButtonJuice>();

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

                            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                            {
                                if (Time.unscaledTime - lastTapTime < 0.35f)
                                {
                                    if (carDriver != null)
                                    {
                                        carDriver.Shoot();
                                    }
                                }
                                lastTapTime = Time.unscaledTime;
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
                        if (mouse.leftButton.wasPressedThisFrame)
                        {
                            if (Time.unscaledTime - lastClickTime < 0.35f)
                            {
                                if (carDriver != null)
                                {
                                    carDriver.Shoot();
                                }
                            }
                            lastClickTime = Time.unscaledTime;
                        }

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

        if (carDriver != null)
        {
            carDriver.SetDragging(true);
            carDriver.SetDragDisplacement(Vector2.zero);
        }
    }

    private void UpdateJoystickDrag(Vector2 pos, Vector2 delta, RectTransform baseRect)
    {
        Vector2 displacement = pos - startTouchPos;
        
        // Calculate max drag radius based on physical screen DPI for consistency across mobile and desktop
        float dpi = Screen.dpi;
        if (dpi <= 0) dpi = 96f; // Fallback for desktops / undefined DPIs
        float maxDrag = 0.6f * dpi; // Maximum physical drag is 0.6 inches
        if (maxDrag < 50f) maxDrag = 50f; // safety floor
        
        Vector2 clampedDelta = Vector2.ClampMagnitude(displacement, maxDrag);
        joystickKnob.anchoredPosition = clampedDelta;

        if (carDriver != null)
        {
            // Pass the normalized displacement (0 to 1 magnitude) to ensure identical speed on all devices
            carDriver.SetDragDisplacement(clampedDelta / maxDrag);
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
            carDriver.SetDragging(false);
            carDriver.SetDragDisplacement(Vector2.zero);
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
        cachedDeliveryPoints.Clear();
        cachedDeliveryPoints.AddRange(GameObject.FindGameObjectsWithTag("Customer"));
        cachedDeliveryPoints.AddRange(GameObject.FindGameObjectsWithTag("Pink House"));
        
        // Also look by name for "Descarga de sorvete"
        foreach (GameObject go in FindObjectsOfType<GameObject>())
        {
            if (go.name.Contains("Descarga de sorvete"))
            {
                if (!cachedDeliveryPoints.Contains(go)) cachedDeliveryPoints.Add(go);
            }
        }

        foreach (GameObject cust in cachedDeliveryPoints)
        {
            // If this delivery plaza already has our illustrated CandyDeliveryPlaza sprite & trigger, keep it crisp!
            SpriteRenderer existingSr = cust.GetComponent<SpriteRenderer>();
            if (existingSr != null && existingSr.sprite != null && existingSr.sprite.name.Contains("CandyDeliveryPlaza"))
                continue;

            // Do NOT change the customer sprite/scale to avoid blocking roads.
            // Just spawn a flat, glowing pink candy circle centered on the delivery spot!
            GameObject matObj = new GameObject("CandyDeliveryZoneMat");
            matObj.transform.position = cust.transform.position;
            matObj.transform.localScale = new Vector3(3.0f, 3.0f, 1f);

            SpriteRenderer matSR = matObj.AddComponent<SpriteRenderer>();
            Color semiPink = new Color(candyPink.r, candyPink.g, candyPink.b, 0.55f);
            
            // Create a perfect 128x128 circle by setting cornerRadius to 64
            matSR.sprite = CreateRoundedRectSprite(128, 128, 64, semiPink, candyPurple, 4, new Color(0, 0, 0, 0));
            matSR.sortingOrder = 5; // Render on top of road/grass layer

            // Name and collider: give the mat a delivery-friendly name and trigger collider
            // so delivery detection (by name search or trigger) works when the car drives over it.
            matObj.name = "Descarga de sorvete";
            CircleCollider2D matCollider = matObj.AddComponent<CircleCollider2D>();
            matCollider.isTrigger = true;
            // Set a reasonable radius that roughly matches the visual mat (adjustable)
            matCollider.radius = 1.5f;
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
        navArrowSR.sortingOrder = 25;
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

        // Only update the target selection at a lower frequency to save massive CPU performance
        if (Time.time - lastTargetSearchTime > targetSearchInterval)
        {
            lastTargetSearchTime = Time.time;
            currentNavigationTarget = GetNearestNavigationTarget();
        }

        if (currentNavigationTarget != null)
        {
            navArrow.SetActive(true);

            // Change arrow color based on cargo
            if (carDriver.cargo > 0)
            {
                navArrowSR.color = candyPink;
            }
            else
            {
                navArrowSR.color = new Color(1f, 0.9f, 0.2f, 1f);
            }

            Vector3 direction = currentNavigationTarget.transform.position - carDriver.transform.position;
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

    private GameObject GetNearestNavigationTarget()
    {
        if (carDriver.cargo > 0)
        {
            // If cached list is empty (e.g., scene reload), repopulate it
            if (cachedDeliveryPoints == null || cachedDeliveryPoints.Count == 0)
            {
                cachedDeliveryPoints = new List<GameObject>();
                cachedDeliveryPoints.AddRange(GameObject.FindGameObjectsWithTag("Customer"));
                cachedDeliveryPoints.AddRange(GameObject.FindGameObjectsWithTag("Pink House"));
                foreach (GameObject go in FindObjectsOfType<GameObject>())
                {
                    if (go.name.Contains("Descarga de sorvete"))
                    {
                        if (!cachedDeliveryPoints.Contains(go)) cachedDeliveryPoints.Add(go);
                    }
                }
            }

            // Clean up any destroyed objects from cache
            cachedDeliveryPoints.RemoveAll(item => item == null);

            return FindNearest(cachedDeliveryPoints.ToArray());
        }
        else
        {
            // Packages are collected and destroyed, so we find them dynamically
            GameObject[] packages = GameObject.FindGameObjectsWithTag("package");
            return FindNearest(packages);
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
        // 1. Create Debug Toggle Button (positioned below MissionPanel to avoid overlap)
        GameObject toggleBtnObj = CreateHUDButton("BtnDebugToggle", "DEBUG", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -330), 160, 62);
        Image toggleImg = toggleBtnObj.GetComponent<Image>();
        toggleImg.sprite = CreateRoundedRectSprite(160, 62, 12, new Color(0.2f, 0.15f, 0.25f, 0.85f), candyPurple, 2, new Color(0,0,0,0));
        
        Button btn = toggleBtnObj.GetComponent<Button>();
        if (btn == null) btn = toggleBtnObj.AddComponent<Button>();
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
        sliderLabel.text = "SENSIBILIDADE: 1.2";
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
        slider.value = 1.2f;

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

    private void CreateGameplayControlToggle()
    {
        // Create subtle Control Toggle Button in the top-right next to Pause button
        gameplayToggleBtnObj = new GameObject("BtnControlToggleGameplay");
        gameplayToggleBtnObj.transform.SetParent(mainCanvas.transform, false);

        RectTransform toggleRect = gameplayToggleBtnObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1, 1);
        toggleRect.anchorMax = new Vector2(1, 1);
        toggleRect.pivot = new Vector2(1, 1);
        toggleRect.anchoredPosition = new Vector2(-145, -24);
        toggleRect.sizeDelta = new Vector2(210, 74);

        Image img = gameplayToggleBtnObj.AddComponent<Image>();
        // Subtle dark color with bubblegum pink border
        img.sprite = CreateRoundedRectSprite(210, 74, 16, new Color(0.2f, 0.15f, 0.25f, 0.85f), candyPink, 3, shadowColor);
        img.type = Image.Type.Simple;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(gameplayToggleBtnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(210, 74);

        gameplayToggleTxt = txtObj.AddComponent<TextMeshProUGUI>();
        gameplayToggleTxt.text = useSwipeMode ? "ARRASTAR" : "BOTÕES";
        gameplayToggleTxt.font = GetTMProFont();
        gameplayToggleTxt.fontSize = 26;
        gameplayToggleTxt.fontStyle = FontStyles.Bold;
        gameplayToggleTxt.alignment = TextAlignmentOptions.Center;
        gameplayToggleTxt.color = Color.white;
        gameplayToggleTxt.outlineColor = candyPurple;
        gameplayToggleTxt.outlineWidth = 0.2f;

        Button btn = gameplayToggleBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            useSwipeMode = !useSwipeMode;
            gameplayToggleTxt.text = useSwipeMode ? "ARRASTAR" : "BOTÕES";
            SetGameState(false); // Refreshes active UI HUD layout immediately
        });

        AddJuicyAnimations(gameplayToggleBtnObj);
    }

    private void CreateVignetteOverlay()
    {
        GameObject vigObj = new GameObject("FullscreenVignette");
        vigObj.transform.SetParent(mainCanvas.transform, false);
        vigObj.transform.SetAsFirstSibling(); // Draw in background of other UI elements

        RectTransform rect = vigObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        vignetteImage = vigObj.AddComponent<RawImage>();
        vignetteImage.texture = CreateVignetteTexture(256, 256);
        vignetteImage.color = new Color(0.18f, 0.08f, 0.24f, 0.16f);
        vignetteImage.raycastTarget = false;
    }

    private Texture2D CreateVignetteTexture(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - w / 2f) / (w / 2f);
                float dy = (y - h / 2f) / (h / 2f);
                float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float alpha = Mathf.SmoothStep(0f, 1f, dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private void CreateMissionPanel()
    {
        missionPanel = new GameObject("MissionPanel");
        missionPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = missionPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20, -180);
        rect.sizeDelta = new Vector2(390, 132);

        Image bg = missionPanel.AddComponent<Image>();
        bg.sprite = CreateRoundedRectSprite(390, 132, 16, new Color(0.368f, 0.235f, 0.502f, 0.82f), candyPink, 2, new Color(0,0,0,0));

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(missionPanel.transform, false);
        RectTransform tRect = titleObj.AddComponent<RectTransform>();
        tRect.anchoredPosition = new Vector2(0, 34);
        tRect.sizeDelta = new Vector2(360, 36);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "MISSÃO ATIVA";
        title.font = GetTMProFont();
        title.fontSize = 24;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = candyPink;

        GameObject descObj = new GameObject("Desc");
        descObj.transform.SetParent(missionPanel.transform, false);
        RectTransform dRect = descObj.AddComponent<RectTransform>();
        dRect.anchoredPosition = new Vector2(0, -2);
        dRect.sizeDelta = new Vector2(360, 34);
        missionDescText = descObj.AddComponent<TextMeshProUGUI>();
        missionDescText.text = "Aguardando Missão...";
        missionDescText.font = GetTMProFont();
        missionDescText.fontSize = 20;
        missionDescText.fontStyle = FontStyles.Bold;
        missionDescText.alignment = TextAlignmentOptions.Center;
        missionDescText.color = Color.white;

        GameObject progObj = new GameObject("Progress");
        progObj.transform.SetParent(missionPanel.transform, false);
        RectTransform pRect = progObj.AddComponent<RectTransform>();
        pRect.anchoredPosition = new Vector2(0, -34);
        pRect.sizeDelta = new Vector2(360, 34);
        missionProgressText = progObj.AddComponent<TextMeshProUGUI>();
        missionProgressText.text = "0 / 0";
        missionProgressText.font = GetTMProFont();
        missionProgressText.fontSize = 22;
        missionProgressText.fontStyle = FontStyles.Bold;
        missionProgressText.alignment = TextAlignmentOptions.Center;
        missionProgressText.color = Color.yellow;
    }

    public void UpdateMissionPanel(string desc, string progress)
    {
        if (missionDescText != null) missionDescText.text = desc;
        if (missionProgressText != null) missionProgressText.text = progress;
    }

    private void CreateBossHealthBarUI()
    {
        bossHealthPanel = new GameObject("BossHealthBarPanel");
        bossHealthPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform rect = bossHealthPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -170);
        rect.sizeDelta = new Vector2(450, 60);

        Image bg = bossHealthPanel.AddComponent<Image>();
        bg.sprite = CreateRoundedRectSprite(450, 60, 12, new Color(0.12f, 0.12f, 0.12f, 0.9f), Color.grey, 2, new Color(0,0,0,0));

        GameObject sliderBg = new GameObject("SliderBg");
        sliderBg.transform.SetParent(bossHealthPanel.transform, false);
        RectTransform sBgRect = sliderBg.AddComponent<RectTransform>();
        sBgRect.anchoredPosition = new Vector2(0, -12);
        sBgRect.sizeDelta = new Vector2(410, 20);
        Image sBgImg = sliderBg.AddComponent<Image>();
        sBgImg.sprite = CreateRoundedRectSprite(410, 20, 6, new Color(0.2f, 0.2f, 0.2f, 1f), new Color(0.2f, 0.2f, 0.2f, 1f), 0, new Color(0,0,0,0));

        GameObject sliderFill = new GameObject("SliderFill");
        sliderFill.transform.SetParent(sliderBg.transform, false);
        RectTransform sFillRect = sliderFill.AddComponent<RectTransform>();
        sFillRect.anchorMin = new Vector2(0f, 0.5f);
        sFillRect.anchorMax = new Vector2(0f, 0.5f);
        sFillRect.pivot = new Vector2(0f, 0.5f);
        sFillRect.anchoredPosition = new Vector2(0f, 0f);
        sFillRect.sizeDelta = new Vector2(410, 20);
        bossHealthFillImage = sliderFill.AddComponent<Image>();
        bossHealthFillImage.sprite = CreateRoundedRectSprite(410, 20, 6, Color.red, Color.red, 0, new Color(0,0,0,0));
        bossHealthFillImage.type = Image.Type.Filled;
        bossHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
        bossHealthFillImage.fillAmount = 1f;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(bossHealthPanel.transform, false);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchoredPosition = new Vector2(0, 15);
        labelRect.sizeDelta = new Vector2(410, 25);
        bossHealthText = labelObj.AddComponent<TextMeshProUGUI>();
        bossHealthText.text = "BARÃO AMARGO";
        bossHealthText.font = GetTMProFont();
        bossHealthText.fontSize = 16;
        bossHealthText.fontStyle = FontStyles.Bold;
        bossHealthText.alignment = TextAlignmentOptions.Center;
        bossHealthText.color = Color.white;
        bossHealthText.outlineColor = Color.black;
        bossHealthText.outlineWidth = 0.22f;

        bossHealthPanel.SetActive(false);
    }

    private float bossMaxHp = 2500f;
    public void ShowBossHealthBar(float maxHp)
    {
        bossMaxHp = maxHp;
        if (bossHealthPanel != null) bossHealthPanel.SetActive(true);
        if (bossHealthFillImage != null) bossHealthFillImage.fillAmount = 1f;
    }

    public void UpdateBossHealthBar(float currentHp)
    {
        if (bossHealthFillImage != null)
        {
            bossHealthFillImage.fillAmount = Mathf.Clamp01(currentHp / bossMaxHp);
        }
    }

    public void HideBossHealthBar()
    {
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
    }
}
