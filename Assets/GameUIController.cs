using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool forceMobileUI = true; // Enabled by default in Editor for testing

    [Header("Candy Palette Colors")]
    public Color candyPink = new Color(1f, 0.482f, 0.773f, 1f); // #ff7bc5 (Bubblegum Pink)
    public Color candyPurple = new Color(0.368f, 0.235f, 0.502f, 1f); // #5e3c80 (Dark Theme Purple)
    public Color candyLightPurple = new Color(0.545f, 0.373f, 0.663f, 1f); // #8b5fa9 (Light Purple)
    public Color shadowColor = new Color(0.2f, 0.118f, 0.294f, 0.5f); // Deep semi-transparent shadow

    private Transform playButton;
    private Transform pauseButton;
    private Transform exitButton;
    private Canvas mainCanvas;
    private driverpro carDriver;

    // Mobile inputs state
    private bool isLeftPressed = false;
    private bool isRightPressed = false;
    private bool isForwardPressed = false;
    private bool isReversePressed = false;

    private List<GameObject> mobileHUDButtons = new List<GameObject>();
    private GameObject menuPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        // Find if there is a Canvas in the scene
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Check if this is the scene containing the menu buttons
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
            }
        }
    }

    private void Awake()
    {
        // Ensure EventSystem exists so buttons are clickable
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
        // Start game in main menu state (paused)
        SetGameState(true);
    }

    private void Update()
    {
        // Update driver inputs from mobile buttons
        if (carDriver != null)
        {
            float steer = (isLeftPressed ? 1f : 0f) + (isRightPressed ? -1f : 0f);
            float move = (isForwardPressed ? 1f : 0f) + (isReversePressed ? -1f : 0f);
            
            carDriver.SetMobileSteer(steer);
            carDriver.SetMobileMove(move);
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

        // Force CanvasScaler to scale with screen size for responsive mobile layout
        CanvasScaler scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Portrait or adaptive landscape
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Find buttons
        playButton = mainCanvas.transform.Find("Play");
        pauseButton = mainCanvas.transform.Find("Pause");
        exitButton = mainCanvas.transform.Find("Exit");

        // Style existing buttons dynamically
        StyleButton(playButton, "JOGAR", candyPink);
        StyleButton(pauseButton, "||", candyPink);
        StyleButton(exitButton, "SAIR", candyPink);

        // Group Play and Exit in a centered menu panel
        CreateMenuPanel();

        // Spawn mobile control HUD if on mobile platform or forced in editor
        bool isMobile = Application.isMobilePlatform || forceMobileUI;
        if (isMobile)
        {
            SpawnMobileHUD();
        }
    }

    private void CreateMenuPanel()
    {
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(mainCanvas.transform, false);

        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(500, 600);

        // Add a beautiful semi-transparent purple panel background
        Image panelImage = menuPanel.AddComponent<Image>();
        panelImage.sprite = CreateRoundedRectSprite(400, 500, 32, new Color(0.368f, 0.235f, 0.502f, 0.85f), candyPink, 4, shadowColor);
        panelImage.type = Image.Type.Sliced;

        // Reparent Play and Exit to this panel
        if (playButton != null)
        {
            playButton.SetParent(menuPanel.transform, false);
            RectTransform playRect = playButton.GetComponent<RectTransform>();
            playRect.anchoredPosition = new Vector2(0, 80);
            playRect.sizeDelta = new Vector2(300, 90);
        }

        if (exitButton != null)
        {
            exitButton.SetParent(menuPanel.transform, false);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            exitRect.anchoredPosition = new Vector2(0, -80);
            exitRect.sizeDelta = new Vector2(300, 90);
        }

        // Add a Title text to the menu
        GameObject titleObj = new GameObject("MenuTitle");
        titleObj.transform.SetParent(menuPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 200);
        titleRect.sizeDelta = new Vector2(400, 100);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CARRINHO DE SORVETE";
        titleText.font = GetTMProFont();
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        titleText.outlineColor = candyPurple;
        titleText.outlineWidth = 0.2f;
    }

    private void StyleButton(Transform btnTrans, string labelText, Color buttonColor)
    {
        if (btnTrans == null) return;

        Button button = btnTrans.GetComponent<Button>();
        if (button == null) button = btnTrans.gameObject.AddComponent<Button>();

        Image img = btnTrans.GetComponent<Image>();
        if (img == null) img = btnTrans.gameObject.AddComponent<Image>();

        // Apply our custom generated procedural sprite
        img.sprite = CreateRoundedRectSprite(300, 90, 20, buttonColor, candyPurple, 4, shadowColor);
        img.type = Image.Type.Simple;

        // Configure button transitions (juicy tinting)
        button.transition = Button.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        // Dynamic Text styling
        Text txt = btnTrans.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = labelText;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            // Add drop shadow
            Outline outline = txt.gameObject.GetComponent<Outline>();
            if (outline == null) outline = txt.gameObject.AddComponent<Outline>();
            outline.effectColor = candyPurple;
            outline.effectDistance = new Vector2(2, -2);
        }
        else
        {
            TMP_Text tmpTxt = btnTrans.GetComponentInChildren<TMP_Text>();
            if (tmpTxt != null)
            {
                tmpTxt.text = labelText;
                tmpTxt.fontSize = 24;
                tmpTxt.color = Color.white;
                tmpTxt.alignment = TextAlignmentOptions.Center;
                tmpTxt.fontStyle = FontStyles.Bold;
            }
        }

        // Setup click events
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

        // Add micro-animations (bounce on hover and press)
        AddJuicyAnimations(btnTrans.gameObject);
    }

    private void AddJuicyAnimations(GameObject btnObj)
    {
        EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

        // On pointer enter, scale up
        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 1.08f; });
        trigger.triggers.Add(enter);

        // On pointer exit, scale back to normal
        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one; });
        trigger.triggers.Add(exit);

        // On pointer down, squeeze a bit
        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 0.95f; });
        trigger.triggers.Add(down);

        // On pointer up, restore hover scale
        EventTrigger.Entry up = new EventTrigger.Entry();
        up.eventID = EventTriggerType.PointerUp;
        up.callback.AddListener((data) => { btnObj.transform.localScale = Vector3.one * 1.08f; });
        trigger.triggers.Add(up);
    }

    private void SetGameState(bool isPaused)
    {
        // Manage active states
        if (menuPanel != null) menuPanel.SetActive(isPaused);
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!isPaused);
            
            // Re-anchor pause button to top-right corner, out of way
            RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1, 1);
            pauseRect.anchorMax = new Vector2(1, 1);
            pauseRect.pivot = new Vector2(1, 1);
            pauseRect.anchoredPosition = new Vector2(-40, -40);
            pauseRect.sizeDelta = new Vector2(80, 80);
        }

        // Show/hide mobile HUD
        foreach (var hudBtn in mobileHUDButtons)
        {
            hudBtn.SetActive(!isPaused);
        }

        // Set timescale
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void QuitGame()
    {
        Debug.Log("Exiting Game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SpawnMobileHUD()
    {
        // Create Left button
        GameObject leftBtn = CreateHUDButton("BtnLeft", "◀", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 80), 120, 120);
        AddHUDEvent(leftBtn, (pressed) => isLeftPressed = pressed);
        mobileHUDButtons.Add(leftBtn);

        // Create Right button
        GameObject rightBtn = CreateHUDButton("BtnRight", "▶", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(220, 80), 120, 120);
        AddHUDEvent(rightBtn, (pressed) => isRightPressed = pressed);
        mobileHUDButtons.Add(rightBtn);

        // Create Accelerate button
        GameObject accelBtn = CreateHUDButton("BtnAccelerate", "▲", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-220, 80), 120, 120);
        AddHUDEvent(accelBtn, (pressed) => isForwardPressed = pressed);
        mobileHUDButtons.Add(accelBtn);

        // Create Reverse button
        GameObject revBtn = CreateHUDButton("BtnReverse", "▼", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 80), 120, 120);
        AddHUDEvent(revBtn, (pressed) => isReversePressed = pressed);
        mobileHUDButtons.Add(revBtn);
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
        Color semiTransPink = new Color(candyPink.r, candyPink.g, candyPink.b, 0.6f); // Semi-transparent for overlay
        img.sprite = CreateRoundedRectSprite((int)width, (int)height, 24, semiTransPink, candyPurple, 4, new Color(0, 0, 0, 0));
        img.type = Image.Type.Simple;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchoredPosition = Vector2.zero;
        txtRect.sizeDelta = new Vector2(width, height);

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.font = GetTMProFont();
        txt.fontSize = 40;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;

        txt.outlineColor = candyPurple;
        txt.outlineWidth = 0.2f;

        btnObj.AddComponent<CanvasGroup>(); // Required for pointer handling in Canvas

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

    // Mathematical rounded rect texture generator
    private Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius, Color baseColor, Color borderColor, int borderWidth, Color shadowColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Keep pixel boundaries sharp!

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        int shadowOffset = 6;
        
        // 1. Draw shadow if color is not transparent
        if (shadowColor.a > 0.01f)
        {
            DrawRoundedRect(tex, shadowOffset, -shadowOffset, width, height, cornerRadius, shadowColor, shadowColor, 0);
        }

        // 2. Draw main button rounded rectangle with border
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

                // Check rounded corner bounds
                bool inside = true;
                if (x < r && y < r) // bottom-left
                    inside = (x - r) * (x - r) + (y - r) * (y - r) <= r * r;
                else if (x >= w - r && y < r) // bottom-right
                    inside = (x - (w - r)) * (x - (w - r)) + (y - r) * (y - r) <= r * r;
                else if (x < r && y >= h - r) // top-left
                    inside = (x - r) * (x - r) + (y - (h - r)) * (y - (h - r)) <= r * r;
                else if (x >= w - r && y >= h - r) // top-right
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

    private TMP_FontAsset GetTMProFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            font = TMP_Settings.defaultFontAsset;
        }
        return font;
    }
}
