using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Complete Game State Machine & Screen Architecture for Baía do Avesso:
/// - TitleMenu (Live real-time camera orbiting the bay + Logo + Play/Continue + Chapters + Settings + Credits + Quit)
/// - Playing (Active chapter timer/patience & gameplay HUD)
/// - Paused (60% dim overlay, Time.timeScale = 0, Continue, Restart Chapter, Controls Guide, Settings, Main Menu)
/// - ChapterVictory (Animated confetti, 1-3 stars, coins/deliveries/decoration bonus summary, Next/Replay/Menu)
/// - GameOver (Themed closed-cart fail screen, clear reason, zero-friction Retry Chapter / Menu)
/// </summary>
public sealed class GirlPowerOrla3DGameFlow : MonoBehaviour
{
    public enum GameState
    {
        TitleMenu,
        Playing,
        Paused,
        ChapterVictory,
        GameOver
    }

    public enum TouchControlMode
    {
        Auto = 0,
        AlwaysHide = 1,
        AlwaysShow = 2
    }

    private enum SubModal
    {
        None,
        Chapters,
        Settings,
        ControlsGuide,
        Credits
    }

    private sealed class ConfettiPiece
    {
        public float x;
        public float y;
        public float vx;
        public float vy;
        public float size;
        public Color color;
    }

    [Header("Connected Systems")]
    public Camera sceneCamera;
    public GirlPowerOrla3DFirstDelivery storyController;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DCardapioUI cardapioUI;
    public GirlPowerOrla3DThiefDetective thiefDetective;
    public GirlPowerOrla3DAmbient ambientController;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DDialogue dialogueController;
    public GirlPowerOrla3DPhotoOverlay photoOverlay;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;

    [Header("Chapter Timer Settings")]
    public bool challengeTimerEnabled = true;
    public float chapter1TimeLimit = 150f;
    public float chapter2TimeLimit = 165f;
    public float chapter3TimeLimit = 150f;

    public GameState CurrentState { get; private set; } = GameState.TitleMenu;
    public TouchControlMode ControlMode { get; private set; } = TouchControlMode.Auto;
    public int LanguageIndex { get; private set; } // 0=PT, 1=EN, 2=ES
    public bool IsEnglish => LanguageIndex == 1;
    public bool IsSpanish => LanguageIndex == 2;
    public float ChapterTimeRemaining { get; private set; }
    public float ChapterTimeMax { get; private set; }
    public bool HasActiveTimer => challengeTimerEnabled && ChapterTimeMax > 1f && CurrentState == GameState.Playing;

    public bool IsPlaying => CurrentState == GameState.Playing;

    private SubModal activeModal = SubModal.None;
    private float menuOrbitClock;
    private Vector3 previousPlayerPosition;
    private float cameraLeadX = 1f;
    private float cameraLeadZ;
    private bool hasPreviousPlayerPosition;
    private int chapterCoinsStart;
    private int chapterPaintStart;

    // Victory / Game Over data
    private int summaryChapter = 1;
    private string summaryTitle = "";
    private string summaryFailReason = "";
    private int summaryStars = 3;
    private int summaryCoinsEarned;
    private int summaryDeliveries;
    private int summaryDecorBonus;
    private float summaryTimeLeft;
    private readonly List<ConfettiPiece> confetti = new List<ConfettiPiece>(70);
    private Texture2D whitePx;
    private string quitFeedback = "";
    private float quitFeedbackTimer;

    private const string PrefPrefix = "BaiaFlow_v1_";

    private void Awake()
    {
        LoadUserPreferences();
    }

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (characterAnimator == null) characterAnimator = FindObjectOfType<GirlPowerOrla3DCharacterAnimator>();
        if (sceneCamera != null)
        {
            sceneCamera.fieldOfView = 48f;
            GirlPowerOrla3DCameraPost cameraPost = sceneCamera.GetComponent<GirlPowerOrla3DCameraPost>();
            if (cameraPost != null)
            {
                cameraPost.contrast = 1.04f;
                cameraPost.saturation = 1.02f;
                cameraPost.vignetteStrength = 0.10f;
            }
        }
        GirlPowerOrla3DOcclusionFader occlusionFader = FindObjectOfType<GirlPowerOrla3DOcclusionFader>();
        if (occlusionFader != null) occlusionFader.cutoutRadius = 1.35f;
        if (characterAnimator != null)
        {
            previousPlayerPosition = characterAnimator.transform.position;
            hasPreviousPlayerPosition = true;
        }
        EnterTitleMenu();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void LoadUserPreferences()
    {
        ControlMode = (TouchControlMode)Mathf.Clamp(PlayerPrefs.GetInt(PrefPrefix + "TouchMode", 0), 0, 2);
        challengeTimerEnabled = PlayerPrefs.GetInt(PrefPrefix + "TimerOn", 1) == 1;
        LanguageIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefPrefix + "LangIdx", 0), 0, 2);
    }

    private void SaveUserPreferences()
    {
        PlayerPrefs.SetInt(PrefPrefix + "TouchMode", (int)ControlMode);
        PlayerPrefs.SetInt(PrefPrefix + "TimerOn", challengeTimerEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PrefPrefix + "LangIdx", LanguageIndex);
        PlayerPrefs.Save();
    }

    public int GetChapterBestStars(int chapter)
    {
        return PlayerPrefs.GetInt(PrefPrefix + "Stars_Ch" + chapter, 0);
    }

    private void SaveChapterBestStars(int chapter, int stars)
    {
        int prev = GetChapterBestStars(chapter);
        if (stars > prev)
        {
            PlayerPrefs.SetInt(PrefPrefix + "Stars_Ch" + chapter, stars);
            PlayerPrefs.Save();
        }
    }

    public void EnterTitleMenu()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.TitleMenu;
        activeModal = SubModal.None;
        if (dialogueController != null) dialogueController.CloseImmediate();
        if (photoOverlay != null) photoOverlay.Close();
        if (ambientController != null)
            ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.FimDeTarde);
    }

    public void StartOrContinueGame()
    {
        activeModal = SubModal.None;
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        int chap = storyController != null ? storyController.GetCurrentChapterNumber() : 1;
        BeginChapterTimer(chap);
        if (audioController != null) audioController.PlayChime();
    }

    public void StartChapter(int chapterNumber)
    {
        activeModal = SubModal.None;
        Time.timeScale = 1f;
        if (storyController != null)
            storyController.JumpToChapter(chapterNumber);
        if (thiefDetective != null)
            thiefDetective.ResetSession();
        CurrentState = GameState.Playing;
        BeginChapterTimer(chapterNumber);
        if (audioController != null) audioController.PlayChime();
    }

    public void RestartCurrentChapter()
    {
        // Restore coins cleanly to what the player had at the start of the chapter attempt!
        if (lifeActivities != null) lifeActivities.SetCoinsDirect(chapterCoinsStart);
        if (megaSystems != null)
        {
            megaSystems.Data.coins = chapterCoinsStart;
            megaSystems.SaveJson();
        }
        int chap = storyController != null ? storyController.GetCurrentChapterNumber() : summaryChapter;
        StartChapter(Mathf.Clamp(chap, 1, 4));
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            activeModal = SubModal.None;
            Time.timeScale = 0f;
            if (audioController != null) audioController.PlayChime();
        }
        else if (CurrentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        activeModal = SubModal.None;
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        if (audioController != null) audioController.PlayChime();
    }

    public void BeginChapterTimer(int chapterNumber)
    {
        chapterCoinsStart = lifeActivities != null ? lifeActivities.Coins : 0;
        chapterPaintStart = (lifeActivities != null && lifeActivities.paintSystem != null)
            ? lifeActivities.paintSystem.StreetTilesPainted
            : 0;

        switch (chapterNumber)
        {
            case 1: ChapterTimeMax = chapter1TimeLimit; break;
            case 2: ChapterTimeMax = chapter2TimeLimit; break;
            case 3: ChapterTimeMax = chapter3TimeLimit; break;
            default: ChapterTimeMax = 0f; break; // Chapter 4 Finale is free cozy celebration
        }
        ChapterTimeRemaining = ChapterTimeMax;
    }

    public void TriggerChapterVictory(int completedChapter, string chapterTitle, int deliveriesCount, int decorBonus)
    {
        if (CurrentState != GameState.Playing) return;

        Time.timeScale = 1f;
        CurrentState = GameState.ChapterVictory;
        activeModal = SubModal.None;

        summaryChapter = completedChapter;
        summaryTitle = chapterTitle;
        summaryDeliveries = deliveriesCount;

        int currentCoins = lifeActivities != null ? lifeActivities.Coins : 0;
        int currentPaint = (lifeActivities != null && lifeActivities.paintSystem != null)
            ? lifeActivities.paintSystem.StreetTilesPainted
            : 0;
        int paintDiff = Mathf.Max(0, currentPaint - chapterPaintStart);

        summaryDecorBonus = decorBonus + paintDiff * 2;
        int rewardCoins = 20 + completedChapter * 10 + summaryDecorBonus;
        if (lifeActivities != null) lifeActivities.AddCoins(rewardCoins);
        summaryCoinsEarned = Mathf.Max(rewardCoins, (currentCoins + rewardCoins) - chapterCoinsStart);
        summaryTimeLeft = ChapterTimeRemaining;

        // Calculate 1 to 3 stars based on remaining time ratio + decoration bonus
        if (ChapterTimeMax > 1f && challengeTimerEnabled)
        {
            float ratio = ChapterTimeRemaining / ChapterTimeMax;
            if (ratio >= 0.42f || (ratio >= 0.30f && summaryDecorBonus >= 10)) summaryStars = 3;
            else if (ratio >= 0.18f) summaryStars = 2;
            else summaryStars = 1;
        }
        else
        {
            summaryStars = 3;
        }

        SaveChapterBestStars(completedChapter, summaryStars);
        SpawnVictoryConfetti();
        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        if (audioController != null) audioController.PlayVictoryFanfare();
    }

    public void TriggerDetectiveVictory(int detectivePoints, int arrestsPrevented, int coinsEarned)
    {
        if (CurrentState != GameState.Playing) return;

        Time.timeScale = 1f;
        CurrentState = GameState.ChapterVictory;
        activeModal = SubModal.None;

        summaryChapter = 4;
        summaryTitle = "VENCEDOR DA BAÍA! Detetive Pipoca está Super Feliz! (" + detectivePoints + " pts)";
        summaryDeliveries = arrestsPrevented;
        summaryDecorBonus = detectivePoints;
        summaryCoinsEarned = coinsEarned;
        summaryTimeLeft = ChapterTimeRemaining;
        summaryStars = 3;

        SaveChapterBestStars(4, 3);
        SpawnVictoryConfetti();
        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        if (audioController != null) audioController.PlayVictoryFanfare();
    }

    public void TriggerGameOver(string reason)
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.GameOver;
        activeModal = SubModal.None;
        summaryChapter = storyController != null ? storyController.GetCurrentChapterNumber() : 1;
        summaryFailReason = reason;
        Time.timeScale = 0f;

        if (dialogueController != null) dialogueController.CloseImmediate();
        if (ambientController != null)
            ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.NoiteNaOrla);
        if (audioController != null) audioController.PlayFailChime();
    }

    private void Update()
    {
        if (quitFeedbackTimer > 0f)
            quitFeedbackTimer = Mathf.Max(0f, quitFeedbackTimer - Time.unscaledDeltaTime);

        Keyboard keys = Keyboard.current;
        if (keys != null && keys.escapeKey.wasPressedThisFrame)
        {
            if (activeModal != SubModal.None)
            {
                activeModal = SubModal.None;
            }
            else if (CurrentState == GameState.Playing)
            {
                bool blockingPopup = (dialogueController != null && dialogueController.IsOpen)
                    || (photoOverlay != null && photoOverlay.IsShowing)
                    || (lifeActivities != null && lifeActivities.IsShopMenuOpen)
                    || (cardapioUI != null && cardapioUI.IsOpen)
                    || (megaSystems != null && megaSystems.IsAnyModalOpen)
                    || (storyController != null && storyController.HasChoiceMenuOpen);
                if (!blockingPopup)
                    TogglePause();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        if (CurrentState == GameState.TitleMenu)
        {
            UpdateTitleOrbitCamera(Time.deltaTime);
        }
        else if (CurrentState == GameState.Playing)
        {
            UpdateGameplayCamera(Time.deltaTime);
            UpdateChapterTimer(Time.deltaTime);
        }
        else if (CurrentState == GameState.ChapterVictory)
        {
            UpdateVictoryConfetti(Time.unscaledDeltaTime);
        }
    }

    private void UpdateTitleOrbitCamera(float dt)
    {
        if (sceneCamera == null) return;
        menuOrbitClock += dt * 0.20f;
        float cx = 11.5f + Mathf.Sin(menuOrbitClock) * 10.5f;
        float cz = -9.2f + Mathf.Cos(menuOrbitClock * 0.7f) * 2.2f;
        float cy = 7.8f + Mathf.Sin(menuOrbitClock * 1.1f) * 0.75f;
        Vector3 targetPos = new Vector3(cx, cy, cz);
        Vector3 lookAt = new Vector3(11.5f + Mathf.Sin(menuOrbitClock * 0.5f) * 4.5f, 1.0f, 3.2f);
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, targetPos, 1f - Mathf.Exp(-2.2f * dt));
        Quaternion targetRot = Quaternion.LookRotation((lookAt - sceneCamera.transform.position).normalized, Vector3.up);
        sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, targetRot, 1f - Mathf.Exp(-2.4f * dt));
    }

    private void UpdateGameplayCamera(float dt)
    {
        if (sceneCamera == null || characterAnimator == null) return;

        Transform player = characterAnimator.transform;
        Vector3 playerPosition = player.position;
        if (hasPreviousPlayerPosition)
        {
            Vector3 movement = playerPosition - previousPlayerPosition;
            if (movement.sqrMagnitude > 0.0004f)
            {
                if (Mathf.Abs(movement.x) > Mathf.Abs(movement.z) * 0.72f)
                {
                    cameraLeadX = Mathf.Sign(movement.x);
                    cameraLeadZ = 0f;
                }
                else
                {
                    cameraLeadZ = Mathf.Sign(movement.z);
                    cameraLeadX = 0f;
                }
            }
        }
        previousPlayerPosition = playerPosition;
        hasPreviousPlayerPosition = true;

        // Keep the avatar in the lower half of the frame and reveal the route ahead.
        Vector3 lookAhead = new Vector3(cameraLeadX * 2.8f, 0f, cameraLeadZ * 2.2f);
        Vector3 focus = playerPosition + lookAhead;
        Vector3 targetPosition = focus + new Vector3(0f, 9.6f, -10.5f);
        float positionBlend = 1f - Mathf.Exp(-4.0f * Mathf.Max(0f, dt));
        sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, targetPosition, positionBlend);

        Vector3 lookAt = playerPosition + Vector3.up * 0.8f + lookAhead * 0.72f;
        Quaternion targetRotation = Quaternion.LookRotation(lookAt - sceneCamera.transform.position, Vector3.up);
        sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, targetRotation, 1f - Mathf.Exp(-5.0f * Mathf.Max(0f, dt)));
    }

    private void UpdateChapterTimer(float dt)
    {
        if (!challengeTimerEnabled || ChapterTimeMax <= 1f) return;
        // Pause timer gently while reading story dialogues or viewing the Polaroid photo
        if (dialogueController != null && dialogueController.IsOpen) return;
        if (photoOverlay != null && photoOverlay.IsShowing) return;

        ChapterTimeRemaining -= dt;
        if (ChapterTimeRemaining <= 0f)
        {
            ChapterTimeRemaining = 0f;
            string reason = storyController != null
                ? storyController.GetMatchingTimeoutFailReason(IsEnglish)
                : (IsEnglish
                    ? "Sunset arrived before you completed the current objective!"
                    : "O expediente acabou antes de você concluir o objetivo atual!");

            TriggerGameOver(reason);
        }
    }

    private void SpawnVictoryConfetti()
    {
        confetti.Clear();
        Color[] cols =
        {
            new Color(1.00f, 0.82f, 0.28f),
            new Color(0.95f, 0.42f, 0.58f),
            new Color(0.32f, 0.82f, 0.74f),
            new Color(0.50f, 0.78f, 0.98f),
            new Color(0.98f, 0.96f, 0.86f)
        };
        for (int i = 0; i < 65; i++)
        {
            confetti.Add(new ConfettiPiece
            {
                x = Random.Range(0f, Screen.width),
                y = Random.Range(-Screen.height * 0.5f, 0f),
                vx = Random.Range(-45f, 45f),
                vy = Random.Range(110f, 240f),
                size = Random.Range(8f, 15f),
                color = cols[i % cols.Length]
            });
        }
    }

    private void UpdateVictoryConfetti(float dt)
    {
        for (int i = 0; i < confetti.Count; i++)
        {
            ConfettiPiece c = confetti[i];
            c.x += c.vx * dt + Mathf.Sin(Time.unscaledTime * 3.5f + i) * 28f * dt;
            c.y += c.vy * dt;
            if (c.y > Screen.height + 20f)
            {
                c.y = -20f;
                c.x = Random.Range(0f, Screen.width);
            }
        }
    }

    private void OnGUI()
    {
        GirlPowerOrla3DUITheme.EnsureInitialized();
        if (whitePx == null)
        {
            whitePx = new Texture2D(2, 2);
            whitePx.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            whitePx.Apply();
        }

        float s = Mathf.Clamp(Screen.width / 1600f, 0.68f, 1.15f);
        GUI.depth = -30;

        switch (CurrentState)
        {
            case GameState.TitleMenu:
                DrawTitleMenu(s);
                break;
            case GameState.Paused:
                DrawPauseOverlay(s);
                break;
            case GameState.ChapterVictory:
                DrawVictoryScreen(s);
                break;
            case GameState.GameOver:
                DrawGameOverScreen(s);
                break;
        }

        if (activeModal != SubModal.None)
            DrawSubModal(s);
    }

    // ------------------------------------------------------------------
    // A. TITLE SCREEN / MAIN MENU (Inspired by "Pedal no Bosque" Reference!)
    // ------------------------------------------------------------------
    private GUIStyle pedalGoldBtn;
    private GUIStyle pedalMintBtn;
    private GUIStyle pedalDarkBtn;
    private GUIStyle pedalRoseBtn;
    private Texture2D leftScrimTex;

    private void EnsurePedalTitleStyles()
    {
        if (pedalGoldBtn != null) return;
        pedalGoldBtn = MakePedalPill(new Color(0.95f, 0.74f, 0.36f, 0.98f), new Color(1.0f, 0.88f, 0.58f, 1f), new Color(0.14f, 0.12f, 0.10f), 20);
        pedalMintBtn = MakePedalPill(new Color(0.42f, 0.78f, 0.56f, 0.98f), new Color(0.68f, 0.92f, 0.76f, 1f), new Color(0.12f, 0.18f, 0.14f), 18);
        pedalDarkBtn = MakePedalPill(new Color(0.15f, 0.32f, 0.28f, 0.94f), new Color(0.32f, 0.54f, 0.48f, 0.9f), new Color(0.96f, 0.98f, 0.95f), 14);
        pedalRoseBtn = MakePedalPill(new Color(0.56f, 0.24f, 0.26f, 0.96f), new Color(0.76f, 0.42f, 0.44f, 0.9f), new Color(0.98f, 0.95f, 0.95f), 14);

        // Soft left-side horizontal gradient scrim so text pops over the live 3D bay!
        leftScrimTex = new Texture2D(64, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[128];
        for (int x = 0; x < 64; x++)
        {
            float t = x / 63f;
            float a = Mathf.Pow(1f - t, 1.8f) * 0.62f;
            Color c = new Color(0.06f, 0.18f, 0.18f, a);
            px[x] = c;
            px[x + 64] = c;
        }
        leftScrimTex.SetPixels(px);
        leftScrimTex.Apply();
    }

    private static GUIStyle MakePedalPill(Color fill, Color border, Color txt, int fontSize)
    {
        Texture2D n = MakePillTex(fill, border);
        Texture2D h = MakePillTex(Color.Lerp(fill, Color.white, 0.15f), Color.white);
        return new GUIStyle(GUI.skin.button)
        {
            normal = { background = n, textColor = txt },
            hover = { background = h, textColor = txt },
            active = { background = n, textColor = txt },
            border = new RectOffset(22, 22, 16, 16),
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private static Texture2D MakePillTex(Color fill, Color border)
    {
        int w = 64, h = 44, r = 20;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r - 1f);
                float qy = Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r - 1f);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                        + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                float cov = Mathf.Clamp01(0.5f - d);
                float inCov = Mathf.Clamp01(0.5f - (d + 2f));
                Color c = Color.Lerp(border, fill, inCov);
                c.a *= cov;
                px[y * w + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private void DrawTitleMenu(float s)
    {
        EnsurePedalTitleStyles();

        // Subtle left-side cozy atmospheric gradient over the live 3D orbiting Baía do Avesso!
        GUI.DrawTexture(new Rect(0, 0, Screen.width * 0.56f, Screen.height), leftScrimTex);

        float lx = 76f * s;
        float colW = 350f * s;
        float topY = Screen.height * 0.14f;

        // Stacked bold cream title ("BAÍA / DO AVESSO") matching Pedal no Bosque
        GUIStyle bigTitle = new GUIStyle(GirlPowerOrla3DUITheme.TitleLogoStyle)
        {
            fontSize = Mathf.RoundToInt(54f * s),
            alignment = TextAnchor.MiddleCenter
        };
        GirlPowerOrla3DUITheme.DrawShadowedLabel(new Rect(lx - 20f * s, topY, colW + 40f * s, 60f * s), "BAÍA", bigTitle, new Color(1f, 0.98f, 0.92f), new Color(0.06f, 0.14f, 0.14f, 0.85f), 3f * s);
        GirlPowerOrla3DUITheme.DrawShadowedLabel(new Rect(lx - 20f * s, topY + 56f * s, colW + 40f * s, 60f * s), "DO AVESSO", bigTitle, new Color(1f, 0.98f, 0.92f), new Color(0.06f, 0.14f, 0.14f, 0.85f), 3f * s);

        string tagline = IsEnglish
            ? "Small deliveries. Big coastal discoveries."
            : (IsSpanish ? "Pequeñas entregas. Grandes descubrimientos." : "Pequenas entregas. Grandes descobertas.");
        GUIStyle subSt = new GUIStyle(GirlPowerOrla3DUITheme.SubtitleStyle)
        {
            fontSize = Mathf.RoundToInt(16f * s),
            alignment = TextAnchor.MiddleCenter
        };
        GirlPowerOrla3DUITheme.DrawShadowedLabel(new Rect(lx - 18f * s, topY + 124f * s, colW + 36f * s, 28f * s), tagline, subSt, new Color(1f, 0.95f, 0.84f), new Color(0.06f, 0.14f, 0.14f, 0.8f), 2f * s);

        if (activeModal != SubModal.None) return;

        float bx = lx + 18f * s;
        float bw = colW - 36f * s;
        float by = topY + 172f * s;

        // Golden-amber Primary Pill: ▶ Jogar
        string playLbl = IsEnglish ? "▶   Play" : (IsSpanish ? "▶   Jugar" : "▶   Jogar");
        if (GUI.Button(new Rect(bx, by, bw, 56f * s), playLbl, pedalGoldBtn))
            StartChapter(1);
        by += 70f * s;

        // Soft Mint-Green Pill: Continuar
        string contLbl = IsEnglish ? "Continue" : (IsSpanish ? "Continuar" : "Continuar");
        if (GUI.Button(new Rect(bx, by, bw, 52f * s), contLbl, pedalMintBtn))
            StartOrContinueGame();
        by += 68f * s;

        // Compact 2-Column Pills: Configurações (Dark Green) & Sair (Burgundy)
        float halfW = (bw - 14f * s) * 0.5f;
        string cfgLbl = IsEnglish ? "Settings" : (IsSpanish ? "Ajustes" : "Configurações");
        if (GUI.Button(new Rect(bx, by, halfW, 42f * s), cfgLbl, pedalDarkBtn))
            activeModal = SubModal.Settings;

        string quitLbl = IsEnglish ? "Quit" : (IsSpanish ? "Salir" : "Sair");
        if (GUI.Button(new Rect(bx + halfW + 14f * s, by, halfW, 42f * s), quitLbl, pedalRoseBtn))
        {
            Application.Quit();
            quitFeedback = IsEnglish ? "Close the browser tab or stop Play Mode to exit." : "Feche a aba do navegador ou pare o Play Mode para sair.";
            quitFeedbackTimer = 3.5f;
        }
        by += 56f * s;

        // Language Pill Chips: PT | EN | ES
        float chipW = 64f * s;
        float chipGap = 12f * s;
        float chipStart = bx + (bw - (chipW * 3f + chipGap * 2f)) * 0.5f;
        string[] langs = { "PT", "EN", "ES" };
        for (int i = 0; i < 3; i++)
        {
            bool sel = LanguageIndex == i;
            if (GUI.Button(new Rect(chipStart + i * (chipW + chipGap), by, chipW, 32f * s), langs[i], sel ? pedalMintBtn : pedalDarkBtn))
            {
                LanguageIndex = i;
                SaveUserPreferences();
            }
        }
        by += 44f * s;

        // Chapters & Credits compact dark pills
        string chapLbl = IsEnglish ? "Chapters" : (IsSpanish ? "Capítulos" : "Capítulos");
        string credLbl = IsEnglish ? "Credits" : (IsSpanish ? "Créditos" : "Créditos");
        if (GUI.Button(new Rect(bx, by, halfW, 36f * s), chapLbl, pedalDarkBtn))
            activeModal = SubModal.Chapters;
        if (GUI.Button(new Rect(bx + halfW + 14f * s, by, halfW, 36f * s), credLbl, pedalDarkBtn))
            activeModal = SubModal.Credits;
        by += 52f * s;

        // Bottom live stats line matching Pedal no Bosque!
        int coins = lifeActivities != null ? lifeActivities.Coins : (megaSystems != null ? megaSystems.Data.coins : 60);
        int deliv = megaSystems != null ? megaSystems.Data.deliveriesTotal : 0;
        int stars = GetChapterBestStars(1) + GetChapterBestStars(2) + GetChapterBestStars(3) + GetChapterBestStars(4);
        int prest = megaSystems != null ? megaSystems.Data.prestige : 10;
        string statsLine = "Moedas: " + coins + "  |  Entregas: " + deliv + "  |  Estrelas: " + stars + "  |  Prestígio: " + prest;
        GUIStyle statSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle)
        {
            fontSize = Mathf.RoundToInt(14f * s),
            alignment = TextAnchor.MiddleCenter
        };
        GirlPowerOrla3DUITheme.DrawShadowedLabel(new Rect(lx - 26f * s, by, colW + 52f * s, 28f * s), statsLine, statSt, new Color(1f, 0.96f, 0.86f), new Color(0.05f, 0.12f, 0.12f, 0.85f), 2f);

        if (quitFeedbackTimer > 0f && !string.IsNullOrEmpty(quitFeedback))
        {
            Rect fb = new Rect(lx, by + 34f * s, colW, 34f * s);
            GUI.Box(fb, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUI.Label(fb, quitFeedback, GirlPowerOrla3DUITheme.BodyLightStyle);
        }
    }

    // ------------------------------------------------------------------
    // C. PAUSE OVERLAY (60% Dim + Time.timeScale = 0)
    // ------------------------------------------------------------------
    private void DrawPauseOverlay(float s)
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);
        if (activeModal != SubModal.None) return;

        float w = 420f * s;
        float h = 428f * s;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect header = new Rect(card.x + 56f * s, card.y - 20f * s, w - 112f * s, 44f * s);
        GUI.Box(header, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
        GUI.Label(header, IsEnglish ? "PAUSED" : "PAUSA", GirlPowerOrla3DUITheme.BadgeTextStyle);

        float bx = card.x + 36f * s;
        float bw = w - 72f * s;
        float by = card.y + 44f * s;

        if (GUI.Button(new Rect(bx, by, bw, 52f * s), IsEnglish ? "▶  RESUME" : "▶  CONTINUAR", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            ResumeGame();
        by += 64f * s;

        if (GUI.Button(new Rect(bx, by, bw, 46f * s), IsEnglish ? "RESTART CHAPTER" : "REINICIAR CAPÍTULO", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            RestartCurrentChapter();
        by += 58f * s;

        if (GUI.Button(new Rect(bx, by, bw, 46f * s), IsEnglish ? "CONTROLS GUIDE" : "GUIA DE CONTROLES", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            activeModal = SubModal.ControlsGuide;
        by += 58f * s;

        if (GUI.Button(new Rect(bx, by, bw, 46f * s), IsEnglish ? "AUDIO & SETTINGS" : "ÁUDIO E CONFIGURAÇÕES", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            activeModal = SubModal.Settings;
        by += 62f * s;

        if (GUI.Button(new Rect(bx, by, bw, 44f * s), IsEnglish ? "MAIN MENU" : "VOLTAR AO MENU PRINCIPAL", GirlPowerOrla3DUITheme.BtnDangerStyle))
            EnterTitleMenu();
    }

    // ------------------------------------------------------------------
    // D. VICTORY / CHAPTER SUMMARY SCREEN
    // ------------------------------------------------------------------
    private void DrawVictoryScreen(float s)
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);

        // Animated celebratory confetti particles
        Color prev = GUI.color;
        for (int i = 0; i < confetti.Count; i++)
        {
            ConfettiPiece c = confetti[i];
            GUI.color = c.color;
            GUI.DrawTexture(new Rect(c.x, c.y, c.size * s, c.size * 0.65f * s), whitePx);
        }
        GUI.color = prev;

        float w = 540f * s;
        float h = 470f * s;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect ribbon = new Rect(card.x + 64f * s, card.y - 22f * s, w - 128f * s, 48f * s);
        GUI.Box(ribbon, GUIContent.none, GirlPowerOrla3DUITheme.PillGoldStyle);
        string vHead = summaryChapter >= 4
            ? (IsEnglish ? "BAY FESTIVAL COMPLETE!" : "FESTA DA BAÍA CONCLUÍDA!")
            : (IsEnglish ? ("CHAPTER " + summaryChapter + " COMPLETE!") : ("CAPÍTULO " + summaryChapter + " CONCLUÍDO!"));
        GirlPowerOrla3DUITheme.DrawShadowedLabel(ribbon, vHead, GirlPowerOrla3DUITheme.BadgeTextStyle, Color.white, new Color(0.45f, 0.28f, 0.04f, 0.8f), 1.5f);

        GirlPowerOrla3DUITheme.HeadingStyle.fontSize = Mathf.RoundToInt(20f * s);
        GUI.Label(new Rect(card.x + 24f * s, card.y + 38f * s, w - 48f * s, 32f * s), summaryTitle, GirlPowerOrla3DUITheme.HeadingStyle);

        // 1 to 3 Golden Stars!
        float starSz = 64f * s;
        float totalStarW = starSz * 3f + 24f * s;
        float sx = card.x + (w - totalStarW) * 0.5f;
        float sy = card.y + 82f * s;
        for (int i = 0; i < 3; i++)
        {
            Texture2D icon = i < summaryStars ? GirlPowerOrla3DUITheme.IconStarFilled : GirlPowerOrla3DUITheme.IconStarEmpty;
            float lift = (i == 1) ? -8f * s : 0f;
            GUI.DrawTexture(new Rect(sx + i * (starSz + 12f * s), sy + lift, starSz, starSz), icon);
        }

        // Numeric summary rows inside cozy recessed panel
        Rect statsBox = new Rect(card.x + 36f * s, card.y + 164f * s, w - 72f * s, 162f * s);
        GUI.Box(statsBox, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

        DrawStatRow(statsBox, 0, s, IsEnglish ? "Coins Earned" : "Moedas Ganhas", "+" + summaryCoinsEarned, new Color(1f, 0.86f, 0.36f));
        DrawStatRow(statsBox, 1, s, IsEnglish ? "Deliveries & Milestones" : "Pedidos & Marcos Entregues", summaryDeliveries.ToString(), new Color(0.58f, 0.96f, 0.88f));
        DrawStatRow(statsBox, 2, s, IsEnglish ? "Decoration & Painting Bonus" : "Bônus de Decoração / Pintura", "+" + summaryDecorBonus, new Color(1f, 0.72f, 0.82f));
        DrawStatRow(statsBox, 3, s, IsEnglish ? "Time Remaining" : "Tempo Restante", FormatSeconds(summaryTimeLeft), new Color(0.95f, 0.96f, 1f));

        // Action Buttons: Next Chapter (highlight), Replay, Menu
        float btnY = card.y + 344f * s;
        float bx = card.x + 36f * s;
        float bw = w - 72f * s;

        string nextTxt = summaryChapter < 4
            ? (IsEnglish ? ("▶  NEXT CHAPTER (CH. " + (summaryChapter + 1) + ")") : ("▶  PRÓXIMO CAPÍTULO (CAP. " + (summaryChapter + 1) + ")"))
            : (IsEnglish ? "▶  EXPLORE FESTIVAL" : "▶  EXPLORAR A FESTA");

        if (GUI.Button(new Rect(bx, btnY, bw, 52f * s), nextTxt, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            if (summaryChapter < 4) StartChapter(summaryChapter + 1);
            else
            {
                CurrentState = GameState.Playing;
                BeginChapterTimer(4);
            }
        }

        float half = (bw - 14f * s) * 0.5f;
        if (GUI.Button(new Rect(bx, btnY + 62f * s, half, 42f * s), IsEnglish ? "REPLAY" : "REPETIR", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            StartChapter(summaryChapter);

        if (GUI.Button(new Rect(bx + half + 14f * s, btnY + 62f * s, half, 42f * s), IsEnglish ? "MAIN MENU" : "MENU", GirlPowerOrla3DUITheme.BtnTileStyle))
            EnterTitleMenu();
    }

    // ------------------------------------------------------------------
    // E. GAME OVER / FAIL STATE SCREEN
    // ------------------------------------------------------------------
    private void DrawGameOverScreen(float s)
    {
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);

        float w = 520f * s;
        float h = 440f * s;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect ribbon = new Rect(card.x + 74f * s, card.y - 22f * s, w - 148f * s, 46f * s);
        GUI.Box(ribbon, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
        GUI.Label(ribbon, IsEnglish ? "DAY ENDED" : "FIM DO EXPEDIENTE", GirlPowerOrla3DUITheme.BadgeTextStyle);

        // Closed cart twilight illustration
        float illuSz = 104f * s;
        GUI.DrawTexture(new Rect(card.x + (w - illuSz) * 0.5f, card.y + 36f * s, illuSz, illuSz), GirlPowerOrla3DUITheme.IconCartClosedFail);

        // Always non-empty, high-contrast failure reason inside dedicated readable area
        string safeReason = string.IsNullOrEmpty(summaryFailReason)
            ? (IsEnglish ? "Time ran out before completing the chapter objective!" : "O tempo acabou antes de concluir o objetivo do capítulo!")
            : summaryFailReason;
        GUIStyle centerBody = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(17f * s),
            normal = { textColor = new Color(0.24f, 0.16f, 0.12f, 1f) }
        };
        GUI.Label(new Rect(card.x + 32f * s, card.y + 150f * s, w - 64f * s, 96f * s), safeReason, centerBody);

        float bx = card.x + 42f * s;
        float bw = w - 84f * s;
        if (GUI.Button(new Rect(bx, card.y + 272f * s, bw, 56f * s), IsEnglish ? "↻  TRY AGAIN NOW" : "↻  TENTAR NOVAMENTE", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            RestartCurrentChapter();

        if (GUI.Button(new Rect(bx, card.y + 342f * s, bw, 46f * s), IsEnglish ? "BACK TO MENU" : "VOLTAR AO MENU", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            EnterTitleMenu();
    }

    // ------------------------------------------------------------------
    // SUB-MODALS (Chapters, Settings, Controls Guide, Credits)
    // ------------------------------------------------------------------
    private void DrawSubModal(float s)
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);

        float w = 560f * s;
        float h = 470f * s;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        string title = "";
        switch (activeModal)
        {
            case SubModal.Chapters: title = IsEnglish ? "CHAPTER SELECT" : "SELEÇÃO DE CAPÍTULOS"; break;
            case SubModal.Settings: title = IsEnglish ? "SETTINGS" : "CONFIGURAÇÕES"; break;
            case SubModal.ControlsGuide: title = IsEnglish ? "CONTROLS GUIDE" : "GUIA DE CONTROLES"; break;
            case SubModal.Credits: title = IsEnglish ? "CREDITS" : "CRÉDITOS"; break;
        }

        Rect head = new Rect(card.x + 68f * s, card.y - 20f * s, w - 136f * s, 44f * s);
        GUI.Box(head, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
        GUI.Label(head, title, GirlPowerOrla3DUITheme.BadgeTextStyle);

        if (activeModal == SubModal.Chapters)
            DrawChaptersModalContent(card, s);
        else if (activeModal == SubModal.Settings)
            DrawSettingsModalContent(card, s);
        else if (activeModal == SubModal.ControlsGuide)
            DrawControlsModalContent(card, s);
        else if (activeModal == SubModal.Credits)
            DrawCreditsModalContent(card, s);

        if (GUI.Button(new Rect(card.x + (w - 190f * s) * 0.5f, card.yMax - 58f * s, 190f * s, 42f * s), IsEnglish ? "BACK" : "VOLTAR", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            activeModal = SubModal.None;
    }

    private void DrawChaptersModalContent(Rect card, float s)
    {
        string[] names = IsEnglish
            ? new[] { "Chapter 1 • The Florist's Order", "Chapter 2 • The Postcard Gust", "Chapter 3 • The Bay Pavilion", "Chapter 4 • Grand Bay Festival" }
            : new[] { "Capítulo 1 • A Encomenda da Florista", "Capítulo 2 • A Ventania dos Cartões", "Capítulo 3 • O Pavilhão da Baía", "Capítulo 4 • A Festa da Baía do Avesso" };
        string[] descs = IsEnglish
            ? new[] { "Craft Lemon & Basil ice cream, deliver to Luna, and upgrade your kiosk.", "Harvest basil, rescue 4 windblown cards, and reunite the sisters.", "Prepare the Festival Trio and illuminate the Pavilion at low tide.", "Free celebration with every neighbor gathered at the long table!" }
            : new[] { "Monte o sorvete de Limão com Manjericão, entregue à Luna e melhore o quiosque.", "Colha manjericão, recolha 4 cartões na ventania e reaproxime as irmãs.", "Prepare o Trio da Festa e restaure as luzes do Pavilhão na maré baixa.", "Celebração livre com toda a vizinhança reunida na mesa comprida!" };

        float y = card.y + 40f * s;
        for (int i = 0; i < 4; i++)
        {
            int chapNum = i + 1;
            Rect row = new Rect(card.x + 26f * s, y, card.width - 52f * s, 82f * s);
            GUI.Box(row, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 150f * s, 26f * s), names[i], new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(15f * s) });
            GUI.Label(new Rect(row.x + 14f * s, row.y + 34f * s, row.width - 150f * s, 40f * s), descs[i], new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle) { normal = { textColor = new Color(0.88f, 0.92f, 0.95f) }, fontSize = Mathf.RoundToInt(12f * s) });

            // Saved stars display
            int stars = GetChapterBestStars(chapNum);
            for (int st = 0; st < 3; st++)
                GUI.DrawTexture(new Rect(row.xMax - 122f * s + st * 22f * s, row.y + 10f * s, 20f * s, 20f * s),
                    st < stars ? GirlPowerOrla3DUITheme.IconStarFilled : GirlPowerOrla3DUITheme.IconStarEmpty);

            if (GUI.Button(new Rect(row.xMax - 124f * s, row.y + 38f * s, 110f * s, 34f * s), IsEnglish ? "PLAY" : "JOGAR", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                StartChapter(chapNum);

            y += 90f * s;
        }
    }

    private void DrawSettingsModalContent(Rect card, float s)
    {
        float x = card.x + 36f * s;
        float w = card.width - 72f * s;
        float y = card.y + 46f * s;

        // Volume sliders
        float musicVol = audioController != null ? audioController.musicVolume : 0.14f;
        float sfxVol = audioController != null ? audioController.sfxVolume : 0.45f;
        float oceanVol = audioController != null ? audioController.oceanVolume : 0.22f;

        GUI.Label(new Rect(x, y, 220f * s, 28f * s), (IsEnglish ? "Music Volume: " : "Volume da Música: ") + Mathf.RoundToInt(musicVol * 100f) + "%", GirlPowerOrla3DUITheme.BodyDarkStyle);
        float newMusic = GUI.HorizontalSlider(new Rect(x + 230f * s, y + 8f * s, w - 230f * s, 22f * s), musicVol, 0f, 0.6f);
        if (Mathf.Abs(newMusic - musicVol) > 0.005f && audioController != null) audioController.SetMusicVolume(newMusic);
        y += 42f * s;

        GUI.Label(new Rect(x, y, 220f * s, 28f * s), (IsEnglish ? "SFX Volume: " : "Efeitos Sonoros: ") + Mathf.RoundToInt(sfxVol * 100f) + "%", GirlPowerOrla3DUITheme.BodyDarkStyle);
        float newSfx = GUI.HorizontalSlider(new Rect(x + 230f * s, y + 8f * s, w - 230f * s, 22f * s), sfxVol, 0f, 1f);
        if (Mathf.Abs(newSfx - sfxVol) > 0.005f && audioController != null) audioController.SetSfxVolume(newSfx);
        y += 42f * s;

        GUI.Label(new Rect(x, y, 220f * s, 28f * s), (IsEnglish ? "Ocean Ambience: " : "Som do Mar: ") + Mathf.RoundToInt(oceanVol * 100f) + "%", GirlPowerOrla3DUITheme.BodyDarkStyle);
        float newOcean = GUI.HorizontalSlider(new Rect(x + 230f * s, y + 8f * s, w - 230f * s, 22f * s), oceanVol, 0f, 0.7f);
        if (Mathf.Abs(newOcean - oceanVol) > 0.005f && audioController != null) audioController.SetOceanVolume(newOcean);
        y += 50f * s;

        // Quality selector
        GUI.Label(new Rect(x, y, 200f * s, 36f * s), IsEnglish ? "Graphics Quality" : "Qualidade Gráfica", GirlPowerOrla3DUITheme.BodyDarkStyle);
        string[] qNames = IsEnglish ? new[] { "Low", "Medium", "High" } : new[] { "Baixa", "Média", "Alta" };
        int[] qIndices = { 1, 2, Mathf.Min(4, QualitySettings.names.Length - 1) };
        float qw = (w - 205f * s) / 3f - 6f * s;
        for (int i = 0; i < 3; i++)
        {
            bool sel = QualitySettings.GetQualityLevel() == qIndices[i];
            if (GUI.Button(new Rect(x + 205f * s + i * (qw + 8f * s), y, qw, 36f * s), qNames[i], sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
                QualitySettings.SetQualityLevel(qIndices[i], true);
        }
        y += 50f * s;

        // Touch Joystick mode
        GUI.Label(new Rect(x, y, 200f * s, 36f * s), IsEnglish ? "On-Screen Joystick" : "Joystick Virtual", GirlPowerOrla3DUITheme.BodyDarkStyle);
        string[] mNames = IsEnglish ? new[] { "Auto", "Off", "On" } : new[] { "Auto", "Oculto", "Visível" };
        for (int i = 0; i < 3; i++)
        {
            bool sel = (int)ControlMode == i;
            if (GUI.Button(new Rect(x + 205f * s + i * (qw + 8f * s), y, qw, 36f * s), mNames[i], sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            {
                ControlMode = (TouchControlMode)i;
                SaveUserPreferences();
            }
        }
        y += 50f * s;

        // Challenge Timer toggle + Language toggle
        string timerLbl = (IsEnglish ? "Chapter Timer: " : "Tempo de Desafio: ") + (challengeTimerEnabled ? "ON" : "OFF");
        if (GUI.Button(new Rect(x, y, (w - 14f * s) * 0.5f, 40f * s), timerLbl, challengeTimerEnabled ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
        {
            challengeTimerEnabled = !challengeTimerEnabled;
            SaveUserPreferences();
        }

        string langLbl = LanguageIndex == 1 ? "Language: EN" : (LanguageIndex == 2 ? "Idioma: ES" : "Idioma: PT-BR");
        if (GUI.Button(new Rect(x + (w + 14f * s) * 0.5f, y, (w - 14f * s) * 0.5f, 40f * s), langLbl, GirlPowerOrla3DUITheme.BtnTileStyle))
        {
            LanguageIndex = (LanguageIndex + 1) % 3;
            SaveUserPreferences();
        }
    }

    private void DrawControlsModalContent(Rect card, float s)
    {
        float x = card.x + 34f * s;
        float w = card.width - 68f * s;
        float y = card.y + 44f * s;

        var rows = IsEnglish
            ? new (string key, string desc)[]
            {
                ("W A S D / Arrows", "Walk around the promenade, fair, beach & surf the sea"),
                ("SHIFT", "Sprint faster along the coast"),
                ("E / Click", "Interact with cart, shops, basil pot & neighbors"),
                ("P  /  C", "Paint houses or street art  /  Change paint color"),
                ("B  /  G", "Call pets (Pipoca & Mingau)  /  Gift & befriend"),
                ("T  /  R", "Switch Time of Day  /  Switch High & Low Tide"),
                ("Scroll / ESC", "Camera Zoom  /  Pause Menu")
            }
            : new (string key, string desc)[]
            {
                ("W A S D / Setas", "Caminhar pela orla, feira, praia e surfar no mar"),
                ("SHIFT", "Correr mais rápido pelo calçadão"),
                ("E / Clique", "Interagir com carrinho, lojas, manjericão e vizinhos"),
                ("P  /  C", "Pintar casas (rolo manual)  /  Trocar cor da tinta"),
                ("B  /  K  /  L", "Chamar/Farejar Pipoca  /  Truques  /  Chamar Polícia (190)"),
                ("V  /  TAB", "Skate, Patinete, Patins, Canoa  /  Abrir Cardápio"),
                ("T  /  R  /  ESC", "Horário  /  Maré Alta e Baixa  /  Pausa")
            };

        for (int i = 0; i < rows.Length; i++)
        {
            Rect r = new Rect(x, y, w, 42f * s);
            GUI.Box(r, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            Rect keyBadge = new Rect(r.x + 8f * s, r.y + 6f * s, 148f * s, 30f * s);
            GUI.Box(keyBadge, GUIContent.none, GirlPowerOrla3DUITheme.PillGoldStyle);
            GUI.Label(keyBadge, rows[i].key, new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { normal = { textColor = new Color(0.28f, 0.18f, 0.10f) }, fontSize = Mathf.RoundToInt(12f * s) });

            GUI.Label(new Rect(r.x + 168f * s, r.y + 4f * s, r.width - 178f * s, 34f * s), rows[i].desc, new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(13f * s) });
            y += 48f * s;
        }
    }

    private void DrawCreditsModalContent(Rect card, float s)
    {
        float y = card.y + 56f * s;
        GUI.DrawTexture(new Rect(card.x + (card.width - 84f * s) * 0.5f, y, 84f * s, 84f * s), GirlPowerOrla3DUITheme.IconLogoEmblem);
        y += 98f * s;

        GUIStyle center = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = Mathf.RoundToInt(16f * s)
        };
        string text = IsEnglish
            ? "Baía do Avesso — Girl Power Orla 3D\n\nA cozy coastal adventure about ice cream, friendship,\ncreative neighborhood painting, and restoring the bay.\n\nProcedural 3D art, custom stylized shaders,\nand coastal synthesizer audio crafted in Unity."
            : "Baía do Avesso — Girl Power Orla 3D\n\nUma aventura costeira aconchegante sobre sorvetes,\namizade, pintura criativa e a restauração da orla.\n\nArte 3D estilizada, shaders customizados\ne trilha costeira procedural criados na Unity.";
        GUI.Label(new Rect(card.x + 42f * s, y, card.width - 84f * s, 210f * s), text, center);
    }

    private void DrawStatRow(Rect box, int row, float s, string label, string val, Color valCol)
    {
        float ry = box.y + 12f * s + row * 36f * s;
        GUIStyle leftSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(15f * s) };
        GUIStyle rightSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(17f * s), normal = { textColor = valCol } };
        GUI.Label(new Rect(box.x + 18f * s, ry, box.width * 0.65f, 30f * s), label, leftSt);
        GUI.Label(new Rect(box.x + box.width * 0.65f, ry, box.width * 0.35f - 18f * s, 30f * s), val, rightSt);
    }

    public static string FormatSeconds(float sec)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(sec));
        return (total / 60).ToString("0") + ":" + (total % 60).ToString("00");
    }
}
