using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Baía do Avesso — 4-Chapter Story Controller, Refined Cozy HUD,
/// Diegetic World Prompts & Waypoint Arrow, and Smart Multiplatform Floating Virtual Joystick.
/// </summary>
public sealed class GirlPowerOrla3DFirstDelivery : MonoBehaviour
{
    private enum StoryPhase
    {
        Ch1_AtCart = 0,
        Ch1_Delivering = 1,
        Ch1_ReturningForUpgrade = 2,
        Ch1_ChooseUpgrade = 3,
        Ch2_HarvestBasil = 4,
        Ch2_CollectCards = 5,
        Ch2_InviteChoice = 6,
        Ch2_DeliverInviteToMarina = 7,
        Ch3_PrepareFestivalTreat = 8,
        Ch3_RestorePavilion = 9,
        Ch4_FestivalFinale = 10
    }

    [Header("Characters & Core Props")]
    public Transform player;
    public Transform playerVisual;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;
    public Transform cart;
    public Transform friendNina;
    public Transform friendBia;
    public Transform florist;
    public Transform sisterMarina;
    public Transform pavilion;
    public Transform basilPot;
    public Transform pedestrianBlue;
    public Transform pedestrianCoral;

    [Header("Visible World Transformations")]
    public GameObject kioskAwningUpgrade;
    public GameObject cartPaintedSignUpgrade;
    public GameObject[] scatteredCards;
    public GameObject pavilionFestivalTable;
    public GameObject pavilionFestivalLights;
    public GameObject pavilionFestivalDishes;
    public TextMeshPro pavilionFadedText;
    public TextMeshPro pavilionRestoredText;
    public Transform customSignboard;
    public TextMeshPro customSignLabel;

    [Header("Systems")]
    public Camera sceneCamera;
    public GirlPowerOrla3DAmbient ambientController;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DDialogue dialogueController;
    public GirlPowerOrla3DPhotoOverlay photoOverlay;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DCardapioUI cardapioUI;
    public GirlPowerOrla3DPetFriendlySystem petFriendlySystem;
    public BoxCollider[] blockingObstacles;

    [Header("Movement & Feel")]
    public float walkSpeed = 4.0f;
    public float runSpeed = 6.4f;
    public float surfSpeed = 7.9f;
    public float turnSpeed = 7.8f;
    public float angularSpeedDeg = 420f;
    public float interactionRadius = 3.8f;

    [Header("Open Spacious Horizon Camera (Wide Beach, Fairies & Town Horizon)")]
    public Vector3 cameraOffset = new Vector3(0f, 10.6f, -16.4f);
    public float cameraPitch = 31.5f;
    public float cameraSmoothTime = 0.22f;
    public float minZoom = 0.72f;
    public float maxZoom = 1.65f;

    [Header("World Exploration Bounds (Wide Promenade, Enterable Cafe & Open Beach!)")]
    public float minWorldX = -17.5f;
    public float maxWorldX = 36.8f;
    public float minWorldZ = -10.8f;
    public float maxWorldZ = 21.5f;

    private Vector2 currentMoveVel;
    private float actualPlanarSpeed;
    private float lastValidFacingYaw;
    private bool hasValidFacingYaw;
    private Vector3 cameraVelRef;
    private float craftProgressTimer;
    private float craftProgressDuration;
    private string craftProgressLabel = "";
    private System.Action onCraftComplete;

    public int BasilLeavesCount => basilLeaves;
    public bool HasChoiceMenuOpen => cartOpen || inviteMenuOpen;
    public bool HasNearbyStoryInteractable => TryGetNearbyInteractable(out _, out _);

    public void AddBasilLeaves(int amount)
    {
        basilLeaves = Mathf.Max(0, basilLeaves + amount);
        SaveProgress();
    }

    public string GetMatchingTimeoutFailReason(bool english)
    {
        switch (phase)
        {
            case StoryPhase.Ch1_AtCart:
            case StoryPhase.Ch1_Delivering:
                return english
                    ? "Sunset arrived and the ice cream melted before you delivered it to Florist Luna!"
                    : "O expediente acabou e o sorvete derreteu antes de você entregar para a Florista Luna!";
            case StoryPhase.Ch1_ReturningForUpgrade:
            case StoryPhase.Ch1_ChooseUpgrade:
                return english
                    ? "Sunset arrived before you chose your kiosk upgrade with Nina!"
                    : "O expediente acabou antes de você escolher a melhoria do seu quiosque com a Nina!";
            case StoryPhase.Ch2_HarvestBasil:
            case StoryPhase.Ch2_CollectCards:
            case StoryPhase.Ch2_InviteChoice:
            case StoryPhase.Ch2_DeliverInviteToMarina:
                return english
                    ? "The coastal gust blew the florist's postcards away before the sisters reunited!"
                    : "A ventania levou os cartões da florista antes do reencontro das irmãs!";
            default:
                return english
                    ? "Night fell over the bay before the Pavilion lights could be restored!"
                    : "A noite chegou sobre a baía antes de você acender o Pavilhão da Baía!";
        }
    }

    private StoryPhase phase = StoryPhase.Ch1_AtCart;
    private string selectedFlavor = "";
    private string chosenUpgrade = "";
    private string inviteApproach = "";
    private string routeName = "";
    private int basilLeaves;
    private int collectedCardsMask;
    private bool cartOpen;
    private bool inviteMenuOpen;
    private bool routeChosen;
    private bool biaHintShown;
    private float zoomFactor = 1f;

    // Smart multiplatform touch/keyboard detection & 3-layer mobile tap-to-move / camera pan
    private bool touchDetected;
    private bool keyboardUsedRecently;
    private bool joyDragging;
    private Vector2 joyCenterGui;
    private Vector2 joyVector;
    private bool touchSprint;
    private bool hasTapDest;
    private Vector3 tapDestWorld;
    private System.Action tapArriveCallback;
    private List<Vector3> tapRoute = new List<Vector3>();
    private int tapRouteIndex;
    private Vector2 cameraPanOffsetXZ;
    private Rect topLeftHudRect;
    private Rect topRightHudRect;
    private Rect bottomPanelHudRect;
    private float floristReactionTimer;
    private string floristReactionText = "";

    public float CurrentZoomFactor => zoomFactor;

    public void SetTapDestination(Vector3 worldPos, System.Action onArrive = null)
    {
        Vector3 requested = new Vector3(
            Mathf.Clamp(worldPos.x, minWorldX + 0.35f, maxWorldX - 0.35f),
            0f,
            Mathf.Clamp(worldPos.z, minWorldZ + 0.35f, maxWorldZ - 0.35f));
        tapArriveCallback = onArrive;
        tapRoute.Clear();
        GirlPowerOrla3DPathfinder pathfinder = EnsurePathfinder();
        if (pathfinder != null && player != null)
            pathfinder.FindPath(player.position, requested, tapRoute, PlayerRadius + 0.06f);
        else
            tapRoute.Add(requested);
        tapRouteIndex = 0;
        hasTapDest = tapRoute.Count > 0;
        if (hasTapDest) tapDestWorld = tapRoute[tapRoute.Count - 1];
    }

    private GirlPowerOrla3DPathfinder EnsurePathfinder()
    {
        if (GirlPowerOrla3DPathfinder.Instance != null) return GirlPowerOrla3DPathfinder.Instance;
        var pf = FindObjectOfType<GirlPowerOrla3DPathfinder>();
        if (pf == null)
        {
            pf = gameObject.AddComponent<GirlPowerOrla3DPathfinder>();
            pf.blockingObstacles = blockingObstacles;
            pf.minX = minWorldX;
            pf.maxX = maxWorldX;
            pf.minZ = minWorldZ;
            pf.maxZ = maxWorldZ;
            pf.RebuildGrid();
        }
        return pf;
    }

    public void ClearTapDestination()
    {
        hasTapDest = false;
        tapArriveCallback = null;
        tapRoute.Clear();
        tapRouteIndex = 0;
    }

    public void AddCameraPanOffset(Vector2 deltaXZ)
    {
        cameraPanOffsetXZ += deltaXZ;
        if (player != null)
        {
            float clampedX = Mathf.Clamp(player.position.x + cameraPanOffsetXZ.x, minWorldX + 2.5f, maxWorldX - 2.5f);
            float clampedZ = Mathf.Clamp(player.position.z + cameraPanOffsetXZ.y, minWorldZ + 1.2f, maxWorldZ - 2.0f);
            cameraPanOffsetXZ = new Vector2(clampedX - player.position.x, clampedZ - player.position.z);
        }
    }

    public void AdjustCameraZoom(float deltaZoom)
    {
        zoomFactor = Mathf.Clamp(zoomFactor + deltaZoom, Mathf.Max(0.75f, minZoom), Mathf.Min(1.62f, maxZoom));
    }

    public void GuideAndPerformActiveObjective()
    {
        Transform target = GetActiveObjectiveTarget();
        if (target == null || player == null) return;
        if (NearXZ(target, interactionRadius))
        {
            Interact();
            return;
        }
        SetTapDestination(target.position, () => Interact());
    }

    public bool IsUiPointBlocked(Vector2 guiPos)
    {
        return topLeftHudRect.Contains(guiPos) || topRightHudRect.Contains(guiPos) || bottomPanelHudRect.Contains(guiPos);
    }

    public bool IsTouchPrimary => touchDetected && !keyboardUsedRecently;

    private static Rect GetGuiSafeArea()
    {
        Rect sa = Screen.safeArea;
        if (sa.width < 100f || sa.height < 100f)
            return new Rect(0f, 0f, Screen.width, Screen.height);
        // Convert Unity bottom-left safeArea to OnGUI top-left coordinates
        return new Rect(sa.x, Screen.height - (sa.y + sa.height), sa.width, sa.height);
    }

    // Diegetic 3D Ground Beacon & Invisible Chapter 1 Onboarding Camera Pan
    private Transform groundBeacon;
    private Renderer groundBeaconRend;
    private float onboardingTimer;

    private Vector3 ninaHomePos, biaHomePos, floristHomePos, betoHomePos, claraHomePos;
    private const float ReturnX = 1.2f;
    private const float PlayerRadius = 0.34f;
    private const string SavePrefix = "BaiaDoAvesso_v4_";

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (cardapioUI == null) cardapioUI = FindObjectOfType<GirlPowerOrla3DCardapioUI>();
        if (petFriendlySystem == null) petFriendlySystem = FindObjectOfType<GirlPowerOrla3DPetFriendlySystem>();

        touchDetected = Input.touchSupported && (Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld);
        CaptureHomePositions();
        EnsurePathfinder();
        CreateDiegeticWaypointBeacon();
        LoadProgress();
        ApplyWorldStateFromPhase(false);
    }

    private void CaptureHomePositions()
    {
        if (friendNina != null) ninaHomePos = friendNina.position;
        if (friendBia != null) biaHomePos = friendBia.position;
        if (florist != null) floristHomePos = florist.position;
        if (pedestrianBlue != null) betoHomePos = pedestrianBlue.position;
        if (pedestrianCoral != null) claraHomePos = pedestrianCoral.position;
    }

    private void CreateDiegeticWaypointBeacon()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Marcador_Objetivo_Diegetico";
        Collider col = ring.GetComponent<Collider>();
        if (col != null) Destroy(col);
        ring.transform.localScale = new Vector3(1.15f, 0.025f, 1.15f);

        Shader sh = Shader.Find("GirlPower/OrlaACNHCozy");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.SetColor("_Color", new Color(1.0f, 0.85f, 0.32f, 1f));
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(1.0f, 0.78f, 0.25f, 1f) * 0.65f);
        if (mat.HasProperty("_CurveStrength")) mat.SetFloat("_CurveStrength", 0.0008f);

        groundBeaconRend = ring.GetComponent<Renderer>();
        groundBeaconRend.sharedMaterial = mat;
        groundBeaconRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        groundBeacon = ring.transform;
    }

    public int GetCurrentChapterNumber()
    {
        if (phase <= StoryPhase.Ch1_ChooseUpgrade) return 1;
        if (phase <= StoryPhase.Ch2_DeliverInviteToMarina) return 2;
        if (phase <= StoryPhase.Ch3_RestorePavilion) return 3;
        return 4;
    }

    public bool HasSavedProgress()
    {
        return PlayerPrefs.HasKey(SavePrefix + "Phase") && PlayerPrefs.GetInt(SavePrefix + "Phase", 0) > 0;
    }

    public void JumpToChapter(int chapter)
    {
        currentMoveVel = Vector2.zero;
        craftProgressTimer = 0f;
        onCraftComplete = null;
        if (characterAnimator != null)
        {
            characterAnimator.SetSurfing(false);
            characterAnimator.SetLandMode(GirlPowerOrla3DCharacterAnimator.LandMode.Walk);
        }
        cartOpen = false;
        inviteMenuOpen = false;
        routeChosen = false;
        biaHintShown = false;
        selectedFlavor = "";

        if (dialogueController != null) dialogueController.CloseImmediate();
        if (photoOverlay != null) photoOverlay.Close();

        switch (chapter)
        {
            case 1:
                phase = StoryPhase.Ch1_AtCart;
                chosenUpgrade = "";
                inviteApproach = "";
                basilLeaves = 0;
                collectedCardsMask = 0;
                onboardingTimer = 2.35f; // Invisible Onboarding: 2s pan showing Florista Luna (!) then Cart
                if (player != null) player.position = new Vector3(-1.2f, 0f, 0.25f);
                if (ambientController != null) ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.FimDeTarde);
                break;
            case 2:
                phase = StoryPhase.Ch2_HarvestBasil;
                if (string.IsNullOrEmpty(chosenUpgrade)) chosenUpgrade = "Toldo Listrado";
                collectedCardsMask = 0;
                if (player != null) player.position = new Vector3(1.6f, 0f, -1.8f);
                if (ambientController != null) ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.ManhaTropical);
                break;
            case 3:
                phase = StoryPhase.Ch3_PrepareFestivalTreat;
                if (string.IsNullOrEmpty(chosenUpgrade)) chosenUpgrade = "Toldo Listrado";
                if (string.IsNullOrEmpty(inviteApproach)) inviteApproach = "Pessoalmente";
                basilLeaves = Mathf.Max(1, basilLeaves);
                if (player != null) player.position = new Vector3(0.4f, 0f, -1.8f);
                if (ambientController != null) ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.FimDeTarde);
                break;
            default:
                phase = StoryPhase.Ch4_FestivalFinale;
                if (string.IsNullOrEmpty(chosenUpgrade)) chosenUpgrade = "Toldo Listrado";
                if (string.IsNullOrEmpty(inviteApproach)) inviteApproach = "Pessoalmente";
                if (player != null)
                {
                    Vector3 pavBase = pavilion != null ? pavilion.position : new Vector3(26.0f, 0f, -10.0f);
                    player.position = pavBase + new Vector3(0f, 0.36f, 2.2f);
                }
                if (ambientController != null) ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.FimDeTarde);
                break;
        }

        SaveProgress();
        ApplyWorldStateFromPhase(false);
        if (sceneCamera != null && player != null)
        {
            sceneCamera.transform.position = player.position + cameraOffset * zoomFactor;
            sceneCamera.transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (gameFlow != null && !gameFlow.IsPlaying)
        {
            if (groundBeacon != null) groundBeacon.gameObject.SetActive(false);
            return;
        }

        bool talking = dialogueController != null && dialogueController.IsOpen;
        bool viewingPhoto = photoOverlay != null && photoOverlay.IsShowing && !talking;
        bool modalBlocked = (cardapioUI != null && cardapioUI.IsOpen) || (megaSystems != null && megaSystems.IsAnyModalOpen);
        Keyboard keys = Keyboard.current;
        Vector2 input = Vector2.zero;

        // Crafting progress bar update (smooth visual prep before cone appears in hand!)
        if (craftProgressTimer > 0f)
        {
            craftProgressTimer = Mathf.Max(0f, craftProgressTimer - Time.deltaTime);
            if (craftProgressTimer <= 0f)
            {
                System.Action cb = onCraftComplete;
                onCraftComplete = null;
                cb?.Invoke();
            }
            FollowAnimalCrossingCamera(Vector2.zero);
            return;
        }

        // Invisible Onboarding Camera Pan (2.3s silent focus on Florista Luna -> Cart)
        if (onboardingTimer > 0f)
        {
            onboardingTimer = Mathf.Max(0f, onboardingTimer - Time.deltaTime);
            Transform focus = onboardingTimer > 1.0f ? florist : cart;
            if (focus != null && sceneCamera != null)
            {
                Vector3 goal = focus.position + new Vector3(0f, 6.2f, -7.8f);
                sceneCamera.transform.position = Vector3.Lerp(sceneCamera.transform.position, goal, 1f - Mathf.Exp(-3.4f * Time.deltaTime));
                sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, Quaternion.Euler(36f, 0f, 0f), 1f - Mathf.Exp(-3.4f * Time.deltaTime));
            }
            if (keys != null && (keys.spaceKey.wasPressedThisFrame || keys.eKey.wasPressedThisFrame))
                onboardingTimer = 0f;
            return;
        }

        // Dynamic Soundscape proximity + adaptive timer urgency!
        if (audioController != null)
        {
            audioController.UpdateOceanProximity(player.position.z);
            float urgency = (gameFlow != null && gameFlow.HasActiveTimer && gameFlow.ChapterTimeMax > 1f)
                ? Mathf.InverseLerp(0.32f, 0.05f, gameFlow.ChapterTimeRemaining / gameFlow.ChapterTimeMax)
                : 0f;
            audioController.SetTimerUrgency(urgency);
        }

        actualPlanarSpeed = 0f;
        if (!talking && !viewingPhoto && !modalBlocked)
        {
            if (keys != null)
            {
                if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) { input.x -= 1f; keyboardUsedRecently = true; }
                if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) { input.x += 1f; keyboardUsedRecently = true; }
                if (keys.wKey.isPressed || keys.upArrowKey.isPressed) { input.y += 1f; keyboardUsedRecently = true; }
                if (keys.sKey.isPressed || keys.downArrowKey.isPressed) { input.y -= 1f; keyboardUsedRecently = true; }
                if (keys.eKey.wasPressedThisFrame) { keyboardUsedRecently = true; Interact(); }
                if (keys.vKey.wasPressedThisFrame && characterAnimator != null)
                {
                    if (characterAnimator.IsInWater) characterAnimator.CycleWaterMode();
                    else characterAnimator.CycleLandMode();
                    if (audioController != null) audioController.PlayChime();
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "Modo: " + characterAnimator.GetActiveMobilityName(), new Color(0.64f, 0.96f, 0.90f), 1.2f);
                }
                if (keys.escapeKey.wasPressedThisFrame)
                {
                    cartOpen = false;
                    inviteMenuOpen = false;
                }

                if (phase == StoryPhase.Ch1_AtCart && cartOpen)
                {
                    if (keys.digit1Key.wasPressedThisFrame) SelectFlavor("Limão com Manjericão");
                    if (keys.digit2Key.wasPressedThisFrame) SelectFlavor("Morango");
                    if (keys.digit3Key.wasPressedThisFrame) SelectFlavor("Baunilha");
                }
                else if (phase == StoryPhase.Ch1_ChooseUpgrade && cartOpen)
                {
                    if (keys.digit1Key.wasPressedThisFrame) ApplyUpgradeChoice("Toldo Listrado");
                    if (keys.digit2Key.wasPressedThisFrame) ApplyUpgradeChoice("Placa Pintada");
                }
                else if (phase == StoryPhase.Ch2_InviteChoice && inviteMenuOpen)
                {
                    if (keys.digit1Key.wasPressedThisFrame) ChooseInviteApproach("Pessoalmente");
                    if (keys.digit2Key.wasPressedThisFrame) ChooseInviteApproach("Coragem");
                }
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) input += stick;
                if (pad.buttonSouth.wasPressedThisFrame) Interact();
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    zoomFactor = Mathf.Clamp(zoomFactor - Mathf.Sign(scroll) * 0.08f, minZoom, maxZoom);
            }

            if (Input.touchCount > 0)
            {
                touchDetected = true;
                keyboardUsedRecently = false;
            }
        }

        if (joyDragging && joyVector.sqrMagnitude > 0.01f && !talking && !viewingPhoto)
            input = joyVector;

        // Convert manual stick/keyboard input into camera-relative ground XZ movement
        // so Right/Up ALWAYS move in the exact direction seen on screen!
        Vector2 worldMoveInput = Vector2.zero;
        if (input.sqrMagnitude > 0.01f)
        {
            ClearTapDestination();
            cameraPanOffsetXZ = Vector2.Lerp(cameraPanOffsetXZ, Vector2.zero, 1f - Mathf.Exp(-3.2f * Time.deltaTime));
            Vector3 camFwd = Vector3.forward;
            Vector3 camRight = Vector3.right;
            if (sceneCamera != null)
            {
                camFwd = sceneCamera.transform.forward;
                camFwd.y = 0f;
                if (camFwd.sqrMagnitude > 0.001f) camFwd.Normalize(); else camFwd = Vector3.forward;
                camRight = sceneCamera.transform.right;
                camRight.y = 0f;
                if (camRight.sqrMagnitude > 0.001f) camRight.Normalize(); else camRight = Vector3.right;
            }
            Vector3 wDir = camRight * input.x + camFwd * input.y;
            worldMoveInput = new Vector2(wDir.x, wDir.z);
        }
        else if (hasTapDest && !talking && !viewingPhoto && !modalBlocked)
        {
            while (tapRouteIndex < tapRoute.Count)
            {
                bool isFinalWp = tapRouteIndex == tapRoute.Count - 1;
                float reachSq = isFinalWp ? (0.36f * 0.36f) : (0.50f * 0.50f);
                if (DistanceSqXZ(player.position, tapRoute[tapRouteIndex]) <= reachSq)
                    tapRouteIndex++;
                else
                    break;
            }
            if (tapRouteIndex >= tapRoute.Count)
            {
                System.Action cb = tapArriveCallback;
                ClearTapDestination();
                cb?.Invoke();
            }
            else
            {
                Vector3 next = tapRoute[tapRouteIndex];
                Vector2 delta = new Vector2(next.x - player.position.x, next.z - player.position.z);
                float dist = delta.magnitude;
                bool isFinalWp = tapRouteIndex == tapRoute.Count - 1;
                float ease = isFinalWp ? Mathf.Clamp01(dist / 0.85f) : 1f;
                worldMoveInput = dist > 0.001f ? (delta / dist) * Mathf.Max(0.42f, ease) : Vector2.zero;
                cameraPanOffsetXZ = Vector2.Lerp(cameraPanOffsetXZ, Vector2.zero, 1f - Mathf.Exp(-2.0f * Time.deltaTime));
            }
        }

        input = worldMoveInput;
        if (input.sqrMagnitude > 1f) input.Normalize();
        bool running = !talking && !viewingPhoto && (
            (keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed))
            || (Gamepad.current != null && Gamepad.current.buttonEast.isPressed)
            || (joyDragging && touchSprint));

        if (!talking && !viewingPhoto && !modalBlocked)
        {
            MovePlayer(input, running);
            if (megaSystems != null) megaSystems.UpdateTrayBalance(input.sqrMagnitude > 0.01f, running, Time.deltaTime);
            if (cartOpen && !NearXZ(cart, interactionRadius + 0.8f)) cartOpen = false;
            if (inviteMenuOpen && !NearXZ(florist, interactionRadius + 0.8f)) inviteMenuOpen = false;
            TryAutoPickupNearbyCard();
        }

        UpdateStoryTriggers();
        UpdateDiegeticBeacon();
        if (floristReactionTimer > 0f)
            floristReactionTimer = Mathf.Max(0f, floristReactionTimer - Time.deltaTime);

        if (characterAnimator != null)
            characterAnimator.UpdateAnimation(input, running, talking, player.position, actualPlanarSpeed);

        FollowAnimalCrossingCamera(input);
    }

    private void UpdateDiegeticBeacon()
    {
        if (groundBeacon == null) return;
        Transform target = GetActiveObjectiveTarget();
        if (target == null || !target.gameObject.activeInHierarchy || NearXZ(target, 2.2f))
        {
            groundBeacon.gameObject.SetActive(false);
            return;
        }

        groundBeacon.gameObject.SetActive(true);
        Vector3 tp = target.position;
        float gy = SampleGroundHeight(tp.x, tp.z);
        float pulse = 1.0f + Mathf.Sin(Time.time * 4.2f) * 0.16f;
        groundBeacon.position = new Vector3(tp.x, gy + 0.05f, tp.z);
        groundBeacon.localScale = new Vector3(1.15f * pulse, 0.025f, 1.15f * pulse);
    }

    private Transform GetActiveObjectiveTarget()
    {
        switch (phase)
        {
            case StoryPhase.Ch1_AtCart:
            case StoryPhase.Ch1_ReturningForUpgrade:
            case StoryPhase.Ch1_ChooseUpgrade:
            case StoryPhase.Ch3_PrepareFestivalTreat:
                return cart;
            case StoryPhase.Ch1_Delivering:
            case StoryPhase.Ch2_InviteChoice:
                return florist;
            case StoryPhase.Ch2_HarvestBasil:
                return basilPot;
            case StoryPhase.Ch2_CollectCards:
                if (scatteredCards != null)
                {
                    Transform best = null;
                    float bestD = float.MaxValue;
                    for (int i = 0; i < scatteredCards.Length; i++)
                    {
                        if (scatteredCards[i] != null && scatteredCards[i].activeSelf)
                        {
                            float d = Vector3.Distance(player.position, scatteredCards[i].transform.position);
                            if (d < bestD) { bestD = d; best = scatteredCards[i].transform; }
                        }
                    }
                    return best;
                }
                return null;
            case StoryPhase.Ch2_DeliverInviteToMarina:
                return sisterMarina;
            case StoryPhase.Ch3_RestorePavilion:
                return pavilion;
            default:
                return null;
        }
    }

    private void MovePlayer(Vector2 input, bool running)
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        bool isWheeled = characterAnimator != null && characterAnimator.CurrentLandMode != GirlPowerOrla3DCharacterAnimator.LandMode.Walk;
        bool isSurfingNow = player.position.z >= 9.6f;

        if (characterAnimator != null)
            characterAnimator.SetSurfing(isSurfingNow);

        float mobMult = characterAnimator != null ? characterAnimator.GetMobilitySpeedMultiplier() : 1f;
        float upgMult = megaSystems != null ? megaSystems.GetSpeedUpgradeMultiplier() : 1f;

        // Terrain physics: Scooter/Skate/Patins are faster on paved promenade/street & drag realistically on soft beach sand!
        bool onSoftSand = player.position.z >= 3.95f && player.position.z < 9.6f;
        float terrainMult = (onSoftSand && isWheeled) ? 0.54f : 1.0f;

        float maxSpeed = (isSurfingNow ? surfSpeed : (running ? runSpeed : walkSpeed)) * mobMult * upgMult * terrainMult;
        Vector2 desiredVel = input * maxSpeed;

        // Smooth acceleration & natural deceleration (no rigid snapping!)
        float accelRate = input.sqrMagnitude > 0.01f ? (isWheeled ? 10.5f : 15f) : (isWheeled ? 6.5f : 14f);
        currentMoveVel = Vector2.MoveTowards(currentMoveVel, desiredVel, maxSpeed * accelRate * dt);

        if (currentMoveVel.sqrMagnitude < 0.0005f)
        {
            actualPlanarSpeed = 0f;
            if (isSurfingNow)
            {
                Vector3 p = player.position;
                p.y = SampleGroundHeight(p.x, p.z);
                player.position = p;
            }
            return;
        }

        Vector3 current = player.position;
        Vector3 move = new Vector3(currentMoveVel.x, 0f, currentMoveVel.y) * dt;

        Vector3 next = TryMoveAxis(current, new Vector3(move.x, 0f, 0f));
        next = TryMoveAxis(next, new Vector3(0f, 0f, move.z));

        // Keep player safely inside scenic bounds; wheeled vehicles smoothly switch off at the ocean's edge!
        next.x = Mathf.Clamp(next.x, minWorldX, maxWorldX);
        next.z = Mathf.Clamp(next.z, minWorldZ, maxWorldZ);
        if (isWheeled && next.z >= 9.4f)
        {
            // Auto-dismount scooter/skate cleanly at the water's edge when entering the ocean!
            characterAnimator.SetLandMode(GirlPowerOrla3DCharacterAnimator.LandMode.Walk);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(next, "Entrando no mar! [V: Surf / Nadar / Canoa]", new Color(0.58f, 0.94f, 1f), 1.4f);
        }

        next.y = SampleGroundHeight(next.x, next.z);
        player.position = next;
        Vector3 realDisp = new Vector3(next.x - current.x, 0f, next.z - current.z);
        actualPlanarSpeed = realDisp.magnitude / dt;

        if (characterAnimator != null)
            characterAnimator.SetSurfing(next.z >= 9.6f);

        // Rotate visual strictly by REAL post-collision displacement + deadzone tolerance:
        // preserves lastValidFacingYaw whenever stopped or pressed against a wall (zero flipping / zero wall jitter!).
        if (playerVisual != null)
        {
            if (!hasValidFacingYaw)
            {
                lastValidFacingYaw = playerVisual.localEulerAngles.y;
                hasValidFacingYaw = true;
            }

            float faceSign = characterAnimator != null ? characterAnimator.FaceForwardSign : 1f;
            if (input.sqrMagnitude > 0.04f && actualPlanarSpeed > 0.35f
                && GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realDisp, faceSign, 0.00001f, out Quaternion targetRot))
            {
                lastValidFacingYaw = targetRot.eulerAngles.y;
            }

            Vector3 curE = playerVisual.localEulerAngles;
            Quaternion flatTarget = Quaternion.Euler(0f, lastValidFacingYaw, 0f);
            Quaternion currentFlat = Quaternion.Euler(0f, curE.y, 0f);
            Quaternion slerpStep = Quaternion.Slerp(currentFlat, flatTarget, 1f - Mathf.Exp(-turnSpeed * dt));
            Quaternion nextFlat = Quaternion.RotateTowards(currentFlat, slerpStep, angularSpeedDeg * dt);
            playerVisual.localRotation = Quaternion.Euler(curE.x, nextFlat.eulerAngles.y, curE.z);
        }
    }

    private static float SampleGroundHeight(float x, float z)
    {
        if (z >= 9.8f)
        {
            float wave = Mathf.Sin(x * 0.55f + Time.time * 1.6f) * 0.06f + Mathf.Cos(z * 0.8f - Time.time * 1.2f) * 0.05f;
            return -0.24f + wave;
        }
        // Pavilion deck height cleanly synced to PavilionBay at (26.0, -0.02, -10.0)
        if (x >= 23.2f && x <= 28.8f && z >= -11.9f && z <= -7.6f)
        {
            if (z > -8.3f) return Mathf.Lerp(-0.04f, 0.34f, (-7.6f - z) / 0.7f);
            return 0.34f;
        }
        if (z < -3.75f) return -0.05f;
        if (z < -3.45f) return Mathf.Lerp(-0.05f, 0f, (z + 3.75f) / 0.30f);
        if (z <= 3.35f) return 0f;
        if (z < 4.10f)
        {
            float t = (z - 3.35f) / 0.75f;
            return Mathf.Sin(t * Mathf.PI) * 0.32f;
        }
        float beachT = Mathf.Clamp01((z - 4.10f) / 5.70f);
        return Mathf.Lerp(-0.04f, -0.30f, beachT);
    }

    private Vector3 TryMoveAxis(Vector3 origin, Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.000001f) return origin;
        Vector3 candidate = origin + delta;
        Vector3 candProbe = new Vector3(candidate.x, 0.5f, candidate.z);
        Vector3 origProbe = new Vector3(origin.x, 0.5f, origin.z);
        float minSq = PlayerRadius * PlayerRadius;

        if (blockingObstacles != null)
        {
            for (int i = 0; i < blockingObstacles.Length; i++)
            {
                BoxCollider box = blockingObstacles[i];
                if (box == null || !box.enabled || box.isTrigger || !box.gameObject.activeInHierarchy) continue;
                float candDistSq = GirlPowerOrla3DPathfinder.DistanceSqToOrientedBoxXZ(candProbe, box);
                if (candDistSq < minSq)
                {
                    float origDistSq = GirlPowerOrla3DPathfinder.DistanceSqToOrientedBoxXZ(origProbe, box);
                    if (candDistSq <= origDistSq)
                        return origin;
                }
            }
        }
        return candidate;
    }

    private static float DistanceSqXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    private void FollowAnimalCrossingCamera(Vector2 input)
    {
        if (sceneCamera == null) return;
        bool inWater = characterAnimator != null && characterAnimator.IsInWater;
        Vector3 baseOff = inWater ? new Vector3(0f, 9.8f, -15.4f) : cameraOffset;
        float targetPitch = inWater ? 34.0f : cameraPitch;

        // Smart arrival framing: gently lift and center on the Cart or Florist pocket so roofs never crowd the view!
        float arrivalLiftY = 0f;
        Vector2 servicePullXZ = Vector2.zero;
        if (!inWater)
        {
            Transform focusService = null;
            float bestDist = 4.8f;
            if (cart != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(cart.position.x, cart.position.z));
                if (d < bestDist) { bestDist = d; focusService = cart; }
            }
            if (florist != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(florist.position.x, florist.position.z));
                if (d < bestDist) { bestDist = d; focusService = florist; }
            }
            if (focusService != null)
            {
                float blend = Mathf.SmoothStep(0f, 1f, 1f - (bestDist / 4.8f));
                arrivalLiftY = blend * 0.85f;
                servicePullXZ = new Vector2(focusService.position.x - player.position.x, focusService.position.z - player.position.z) * (blend * 0.28f);
            }
        }

        Vector3 scaledOffset = baseOff * zoomFactor;
        float lookAheadX = Mathf.Clamp(currentMoveVel.x * 0.24f, -1.4f, 1.4f);
        // Balanced forward bias (+0.35f Z) on land so the protagonist's feet AND the walkway ahead stay clearly framed!
        float lookAheadZ = Mathf.Clamp(currentMoveVel.y * 0.16f, -0.8f, 0.8f) + (inWater ? -0.4f : 0.35f);
        float focusX = Mathf.Clamp(player.position.x + lookAheadX + servicePullXZ.x + cameraPanOffsetXZ.x, minWorldX + 2.5f, maxWorldX - 2.5f);
        float focusZ = Mathf.Clamp(player.position.z + lookAheadZ + servicePullXZ.y + cameraPanOffsetXZ.y, minWorldZ + 1.2f, maxWorldZ - 2.0f);
        float desiredX = focusX;
        float desiredZ = focusZ + scaledOffset.z;
        float desiredY = player.position.y + scaledOffset.y + arrivalLiftY;
        Vector3 shake = GirlPowerOrla3DJuice.Instance != null ? GirlPowerOrla3DJuice.Instance.GetCameraShakeOffset() : Vector3.zero;
        Vector3 target = new Vector3(desiredX, desiredY, desiredZ) + shake;
        sceneCamera.transform.position = Vector3.SmoothDamp(sceneCamera.transform.position, target, ref cameraVelRef, cameraSmoothTime);
        sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, Quaternion.Euler(targetPitch, 0f, 0f), 1f - Mathf.Exp(-5.5f * Time.deltaTime));
    }

    private void TryAutoPickupNearbyCard()
    {
        if (phase != StoryPhase.Ch2_CollectCards || scatteredCards == null) return;
        for (int i = 0; i < scatteredCards.Length; i++)
        {
            GameObject c = scatteredCards[i];
            if (c != null && c.activeSelf && NearXZ(c.transform, 1.35f))
            {
                CollectCard(i);
                return;
            }
        }
    }

    private void CollectCard(int idx)
    {
        if (scatteredCards == null || idx < 0 || idx >= scatteredCards.Length) return;
        if ((collectedCardsMask & (1 << idx)) != 0) return;

        Vector3 cardPos = scatteredCards[idx] != null ? scatteredCards[idx].transform.position : player.position;
        collectedCardsMask |= (1 << idx);
        if (scatteredCards[idx] != null) scatteredCards[idx].SetActive(false);
        if (audioController != null) audioController.PlayChime();

        int count = CountCollectedCards();
        int total = scatteredCards.Length;
        if (lifeActivities != null) lifeActivities.AddCoins(5);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cardPos, "★ Cartão " + count + "/" + total + "  (+5 moedas)", new Color(1f, 0.88f, 0.36f));
        SaveProgress();

        if (count >= total)
        {
            phase = StoryPhase.Ch2_InviteChoice;
            SaveProgress();
            if (characterAnimator != null) characterAnimator.TriggerCelebration();
            if (audioController != null) audioController.PlayFanfare();
            if (dialogueController != null)
            {
                dialogueController.ShowDialogue(
                    "Cartão Especial Encontrado!",
                    new Color(0.76f, 0.44f, 0.72f, 1f),
                    1.15f,
                    new[]
                    {
                        "Espera... este último cartão tem um selo de flor e está endereçado: \"Para Marina — Venha ver o pôr do sol no Pavilhão da Baía do Avesso\".",
                        "A Florista escreveu um convite para a irmã, mas ainda não teve coragem de enviar! Vamos conversar com a Florista Luna!"
                    }
                );
            }
        }
    }

    private int CountCollectedCards()
    {
        int n = 0;
        int len = scatteredCards != null ? scatteredCards.Length : 4;
        for (int i = 0; i < len; i++)
            if ((collectedCardsMask & (1 << i)) != 0) n++;
        return n;
    }

    public void Interact()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;

        // 0. Card pickup
        if (phase == StoryPhase.Ch2_CollectCards && scatteredCards != null)
        {
            for (int i = 0; i < scatteredCards.Length; i++)
            {
                if (scatteredCards[i] != null && scatteredCards[i].activeSelf && NearXZ(scatteredCards[i].transform, 2.3f))
                {
                    CollectCard(i);
                    return;
                }
            }
        }

        // 1. Basil Pot harvesting
        if (basilPot != null && basilPot.gameObject.activeSelf && NearXZ(basilPot, 2.3f))
        {
            basilLeaves++;
            if (audioController != null) audioController.PlayChime();
            if (characterAnimator != null)
            {
                characterAnimator.TriggerCelebration();
                characterAnimator.SpawnLeafBurst(basilPot.position);
            }
            GirlPowerOrla3DJuice.PopScale(basilPot, 1.25f, 0.34f);
            GirlPowerOrla3DJuice.ShakeCamera(0.08f, 0.12f);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(basilPot.position, "+1 Manjericão Fresco!", new Color(0.54f, 0.96f, 0.56f));

            if (phase == StoryPhase.Ch2_HarvestBasil)
            {
                phase = StoryPhase.Ch2_CollectCards;
                collectedCardsMask = 0;
                ApplyWorldStateFromPhase(true);
                SaveProgress();
                if (dialogueController != null)
                {
                    dialogueController.ShowDialogue(
                        "Capítulo 2 — A Ventania!",
                        new Color(0.32f, 0.66f, 0.78f, 1f),
                        1.05f,
                        new[]
                        {
                            "Você colheu manjericão fresquinho! De repente, uma ventania forte sopra pela Baía do Avesso!",
                            "O vento espalhou 4 cartões da Florista pela orla! Vamos recolher todos antes que se percam!"
                        }
                    );
                }
            }
            else
            {
                SaveProgress();
            }
            return;
        }

        // 2. Cart & Kiosk interactions
        if (NearXZ(cart, interactionRadius))
        {
            if (phase == StoryPhase.Ch1_AtCart)
            {
                if (!cartOpen)
                {
                    cartOpen = true;
                    // Pre-select the Florist's requested flavor so 1 tap confirms immediately (player can still switch flavors!)
                    if (string.IsNullOrEmpty(selectedFlavor))
                        selectedFlavor = "Limão com Manjericão";
                    if (audioController != null) audioController.PlayChime();
                }
                else if (selectedFlavor == "Limão com Manjericão")
                {
                    cartOpen = false;
                    craftProgressDuration = 0.65f;
                    craftProgressTimer = craftProgressDuration;
                    craftProgressLabel = GirlPowerOrla3DLocalization.T("Montando Limão + Manjericão...", "Building Lemon + Basil...", "Armando Limón + Albahaca...");
                    if (audioController != null) audioController.PlayChime();
                    if (characterAnimator != null) characterAnimator.SpawnLeafBurst(cart.position);
                    onCraftComplete = () =>
                    {
                        phase = StoryPhase.Ch1_Delivering;
                        SaveProgress();
                        if (characterAnimator != null)
                        {
                            characterAnimator.SetHoldingCone(true);
                            characterAnimator.TriggerCelebration();
                        }
                        if (audioController != null) audioController.PlayFanfare();
                        GirlPowerOrla3DJuice.PopScale(cart, 1.16f, 0.34f);
                        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "★ Limão com Manjericão pronto!", new Color(0.84f, 0.98f, 0.54f));
                    };
                }
                else
                {
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "A Florista pediu Limão com Manjericão!", new Color(1f, 0.78f, 0.45f));
                }
                return;
            }

            if (phase == StoryPhase.Ch1_ChooseUpgrade)
            {
                cartOpen = true;
                if (audioController != null) audioController.PlayChime();
                return;
            }

            if (phase == StoryPhase.Ch3_PrepareFestivalTreat)
            {
                if (basilLeaves < 1)
                {
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(basilPot != null ? basilPot.position : cart.position, "Colha 1 Manjericão primeiro!", new Color(1f, 0.78f, 0.45f));
                    return;
                }
                basilLeaves--;
                phase = StoryPhase.Ch3_RestorePavilion;
                SaveProgress();
                if (characterAnimator != null)
                {
                    characterAnimator.SetHoldingCone(true);
                    characterAnimator.TriggerCelebration();
                }
                if (audioController != null) audioController.PlayFanfare();
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "★ Trio da Festa preparado!", new Color(1f, 0.88f, 0.40f));
                if (dialogueController != null)
                {
                    dialogueController.ShowDialogue(
                        "Nina (Quiosque)",
                        new Color(0.24f, 0.70f, 0.62f, 1f),
                        1.18f,
                        new[]
                        {
                            "Que aroma maravilhoso!! Usar o manjericão que a florista nos deu fechou a receita com chave de ouro!",
                            "Agora leve para o Pavilhão da Baía na Maré Baixa! Marina já está lá com a chave dourada!"
                        }
                    );
                }
                return;
            }
        }

        // 3. Sister Marina
        if (sisterMarina != null && sisterMarina.gameObject.activeSelf && NearXZ(sisterMarina, 2.8f))
        {
            if (phase == StoryPhase.Ch2_DeliverInviteToMarina)
            {
                CompleteChapter2Reunion("Pessoalmente");
                return;
            }
            if (dialogueController != null)
            {
                string[] mLines = phase == StoryPhase.Ch4_FestivalFinale
                    ? new[]
                    {
                        "Olha só esse letreiro aceso de novo e toda a Baía do Avesso reunida na mesa comprida!",
                        "Obrigada por nos lembrar que esse lugar só precisava de alguém que cuidasse dele com carinho!"
                    }
                    : new[]
                    {
                        "Eu guardei a chave do Pavilhão todos esses anos esperando o dia de acender esse letreiro de novo!",
                        "Prepara o sorvete especial com o manjericão lá no quiosque e traz pro Pavilhão na maré baixa!"
                    };
                dialogueController.ShowDialogue("Marina (Irmã da Florista)", new Color(0.92f, 0.46f, 0.38f, 1f), 1.12f, mLines);
            }
            return;
        }

        // 4. Florista Luna
        if (NearXZ(florist, interactionRadius))
        {
            if (phase == StoryPhase.Ch1_Delivering)
            {
                // Short, joyous, non-blocking Florist reaction: updates objective & returns movement control immediately without covering the path!
                phase = StoryPhase.Ch1_ReturningForUpgrade;
                SaveProgress();
                if (megaSystems != null) megaSystems.RecordDelivery(15, florist.position);
                else if (lifeActivities != null) lifeActivities.AddCoins(15);
                GirlPowerOrla3DJuice.PopScale(florist, 1.24f, 0.36f);
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(florist.position + Vector3.up * 0.4f, "+15 ★", new Color(1f, 0.86f, 0.36f), 2.0f);
                if (characterAnimator != null)
                {
                    characterAnimator.SetHoldingCone(false);
                    characterAnimator.TriggerCelebration();
                }
                if (audioController != null) audioController.PlayFanfare();
                if (photoOverlay != null) photoOverlay.UnlockSilent();

                floristReactionText = GirlPowerOrla3DLocalization.T(
                    "Florista Luna: \"♥ Limão com manjericão! Que delícia! Volte ao carrinho para escolher sua melhoria!\"",
                    "Florist Luna: \"♥ Lemon & basil! Delicious! Head back to the cart to pick your upgrade!\"",
                    "Florista Luna: \"♥ ¡Limón con albahaca! ¡Qué rico! ¡Vuelve al carrito para elegir tu mejora!\"");
                floristReactionTimer = 4.2f;
                return;
            }

            if (phase == StoryPhase.Ch2_InviteChoice)
            {
                inviteMenuOpen = true;
                if (audioController != null) audioController.PlayChime();
                return;
            }

            if (dialogueController != null)
            {
                if (phase == StoryPhase.Ch4_FestivalFinale)
                {
                    string how = inviteApproach == "Coragem"
                        ? "Nunca vou esquecer você me ajudando a criar coragem para chamar a Marina!"
                        : "Quando você foi entregar o convite pessoalmente para a Marina, mudou tudo pra nós duas!";
                    dialogueController.ShowDialogue("Florista Luna", new Color(0.76f, 0.44f, 0.72f, 1f), 1.20f, new[]
                    {
                        how,
                        "Trouxe meus vasinhos de flores mais bonitos pra enfeitar a mesa comprida!"
                    });
                }
                else
                {
                    bool isMorning = ambientController != null && ambientController.currentPreset == GirlPowerOrla3DAmbient.TimePreset.ManhaTropical;
                    string routineLine = isMorning
                        ? "De manhã cedinho eu rego todas as mudas aqui na banca antes do sol esquentar."
                        : "No fim da tarde eu trago os vasinhos pra calçada para perfumar toda a Baía do Avesso!";
                    dialogueController.ShowDialogue("Florista Luna", new Color(0.76f, 0.44f, 0.72f, 1f), 1.20f, new[] { routineLine });
                }
            }
            return;
        }

        // 5. Pavilion Restoration
        if (NearXZ(pavilion, 4.0f))
        {
            if (phase == StoryPhase.Ch3_RestorePavilion)
            {
                bool highTide = ambientController != null && ambientController.isHighTide;
                if (highTide)
                {
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pavilion.position, "Abaixe a maré [R] para abrir o Pavilhão!", new Color(0.56f, 0.90f, 1f));
                    if (audioController != null) audioController.PlayChime();
                    return;
                }

                phase = StoryPhase.Ch4_FestivalFinale;
                ApplyWorldStateFromPhase(true);
                SaveProgress();
                if (characterAnimator != null)
                {
                    characterAnimator.SetHoldingCone(false);
                    characterAnimator.TriggerCelebration();
                }
                if (audioController != null) audioController.PlayFanfare();
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pavilion.position, "★ Pavilhão Restaurado! (+30 moedas)", new Color(1f, 0.88f, 0.36f));

                if (dialogueController != null)
                {
                    dialogueController.ShowDialogue(
                        "Capítulo Final — A Festa da Baía!",
                        new Color(0.95f, 0.64f, 0.28f, 1f),
                        1.14f,
                        new[]
                        {
                            "A chave dourada girou!! As luzes do Pavilhão se acenderam, o letreiro voltou a brilhar e a mesa comprida foi montada!",
                            "Todos os moradores chegaram trazendo pratos, vasinhos e sorrisos. A Baía do Avesso ganhou vida nova!"
                        },
                        () =>
                        {
                            if (gameFlow != null)
                                gameFlow.TriggerChapterVictory(3, "O Pavilhão da Baía Restaurado!", 3, 25);
                        }
                    );
                }
                else if (gameFlow != null)
                {
                    gameFlow.TriggerChapterVictory(3, "O Pavilhão da Baía Restaurado!", 3, 25);
                }
                return;
            }

            if (dialogueController != null)
            {
                bool highTide = ambientController != null && ambientController.isHighTide;
                string[] lines = phase == StoryPhase.Ch4_FestivalFinale
                    ? new[] { "O Pavilhão da Baía brilha com luzes quentes, bandeirinhas e a mesa comprida cheia de quitutes!" }
                    : new[]
                    {
                        "Um pavilhão de telhado baixo com mosaico pastel gasto e um letreiro apagado...",
                        highTide ? "Na maré alta a espuma cobre os degraus. Alterne a maré no topo direito ou aperte R!"
                                 : "Na maré baixa a escadinha de pedra aparece inteira levando ao mosaico!"
                    };
                dialogueController.ShowDialogue("Pavilhão da Baía do Avesso", new Color(0.32f, 0.62f, 0.68f, 1f), 0.96f, lines);
            }
            return;
        }

        // 6. Neighbors
        if (NearXZ(friendNina, 2.5f) && dialogueController != null)
        {
            string[] lines = phase == StoryPhase.Ch4_FestivalFinale
                ? new[] { "Nós conseguimos!! Trouxe sorvete de limão com manjericão pra mesa comprida!" }
                : new[] { "Eu fico cuidando do quiosque enquanto você explora e transforma a Baía do Avesso!" };
            dialogueController.ShowDialogue("Nina (Amiga do Quiosque)", new Color(0.24f, 0.70f, 0.62f, 1f), 1.18f, lines);
            return;
        }

        if (NearXZ(friendBia, 2.7f) && dialogueController != null)
        {
            string[] lines = phase == StoryPhase.Ch4_FestivalFinale
                ? new[] { "Festa no Pavilhão!! Pendurei bandeirinhas coloridas e trouxe bolo de frutas!" }
                : new[] { "Pela praça da feira aqui em cima o caminho até a florista é mais curto! Pelo calçadão você tem vista aberta pro mar!" };
            dialogueController.ShowDialogue("Bia (Amiga da Praça)", new Color(0.62f, 0.44f, 0.84f, 1f), 1.24f, lines);
            return;
        }

        if (NearXZ(pedestrianBlue, 2.6f) && dialogueController != null)
        {
            string[] lines = phase == StoryPhase.Ch4_FestivalFinale
                ? new[] { "Trouxe frutas fresquinhas da feirinha pra mesa comprida! Viva a Baía do Avesso!" }
                : new[] { "E aí, sou o Beto! Na maré baixa a escadinha do Pavilhão aparece inteirinha!" };
            dialogueController.ShowDialogue("Beto Praiano", new Color(0.28f, 0.64f, 0.86f, 1f), 0.88f, lines);
            return;
        }

        if (NearXZ(pedestrianCoral, 2.6f) && dialogueController != null)
        {
            string[] lines = phase == StoryPhase.Ch4_FestivalFinale
                ? new[] { "Que noite inesquecível!! Trouxe lanterninhas da feira pra iluminar a festa!" }
                : new[] { "Oiê, sou a Clara! A feirinha da orla está cheia de lojinhas pra comprar presentes!" };
            dialogueController.ShowDialogue("Clara da Feira", new Color(0.92f, 0.48f, 0.38f, 1f), 1.18f, lines);
        }
    }

    private void SelectFlavor(string flavor)
    {
        selectedFlavor = flavor;
        if (audioController != null) audioController.PlayChime();
        if (flavor == "Limão com Manjericão")
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "✓ Limão com Manjericão selecionado!", new Color(0.64f, 0.98f, 0.60f));
        else
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "A florista pediu Limão com Manjericão!", new Color(1f, 0.80f, 0.45f));
    }

    private void ApplyUpgradeChoice(string upgradeName)
    {
        chosenUpgrade = upgradeName;
        cartOpen = false;
        phase = StoryPhase.Ch2_HarvestBasil;
        ApplyWorldStateFromPhase(true);
        SaveProgress();

        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        if (audioController != null) audioController.PlayFanfare();
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "★ " + upgradeName + " instalado!", new Color(1f, 0.88f, 0.36f));

        if (dialogueController != null)
        {
            dialogueController.ShowDialogue(
                "Nina (Quiosque)",
                new Color(0.24f, 0.70f, 0.62f, 1f),
                1.18f,
                new[]
                {
                    "Uauuu!! O " + upgradeName + " apareceu na hora e deixou o nosso cantinho lindo demais!",
                    "A Florista Luna também mandou um Vaso de Manjericão de presente bem ao lado do carrinho!"
                },
                () =>
                {
                    if (gameFlow != null)
                        gameFlow.TriggerChapterVictory(1, "A Encomenda da Florista", 1, 15);
                }
            );
        }
        else if (gameFlow != null)
        {
            gameFlow.TriggerChapterVictory(1, "A Encomenda da Florista", 1, 15);
        }
    }

    private void ChooseInviteApproach(string approach)
    {
        inviteApproach = approach;
        inviteMenuOpen = false;
        if (audioController != null) audioController.PlayChime();

        if (approach == "Pessoalmente")
        {
            phase = StoryPhase.Ch2_DeliverInviteToMarina;
            ApplyWorldStateFromPhase(true);
            SaveProgress();
            if (dialogueController != null)
            {
                dialogueController.ShowDialogue(
                    "Florista Luna",
                    new Color(0.76f, 0.44f, 0.72f, 1f),
                    1.20f,
                    new[]
                    {
                        "Você vai mesmo entregar o convite pessoalmente para ela?! Muito obrigada!",
                        "Marina costuma caminhar lá no canto esquerdo do calçadão, olhando o mar. Leve o convite até ela!"
                    }
                );
            }
        }
        else
        {
            CompleteChapter2Reunion("Coragem");
        }
    }

    private void CompleteChapter2Reunion(string approach)
    {
        inviteApproach = approach;
        phase = StoryPhase.Ch3_PrepareFestivalTreat;
        ApplyWorldStateFromPhase(true);
        SaveProgress();
        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        if (audioController != null) audioController.PlayFanfare();
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "♥ Reencontro das Irmãs!", new Color(1f, 0.62f, 0.78f));

        if (dialogueController != null)
        {
            string[] lines = approach == "Coragem"
                ? new[]
                {
                    "Florista Luna respira fundo com o seu apoio e acena para Marina... as duas se abraçam emocionadas!",
                    "Marina mostra a chave dourada do Pavilhão: \"Vamos reabrir o lugar com uma festa!\""
                }
                : new[]
                {
                    "Marina lê o convite com os olhos brilhando: \"Minha irmã lembrou do nosso pôr do sol no Pavilhão!\"",
                    "Ela ergue a chave dourada: \"Vamos juntas reabrir o Pavilhão com o Sorvete Especial de Manjericão!\""
                };
            dialogueController.ShowDialogue("Reencontro das Irmãs!", new Color(0.92f, 0.46f, 0.38f, 1f), 1.14f, lines, () =>
            {
                if (gameFlow != null)
                    gameFlow.TriggerChapterVictory(2, "A Ventania dos Cartões", 4, 20);
            });
        }
        else if (gameFlow != null)
        {
            gameFlow.TriggerChapterVictory(2, "A Ventania dos Cartões", 4, 20);
        }
    }

    private void UpdateStoryTriggers()
    {
        float x = player.position.x;
        float z = player.position.z;

        if (phase == StoryPhase.Ch1_Delivering && !biaHintShown && x >= 4.2f && x <= 8.2f)
        {
            biaHintShown = true;
            if (friendBia != null)
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(friendBia.position, "Atalho pela Feira ↑ ou Calçadão ↓!", new Color(0.86f, 0.76f, 1f), 2.4f);
        }

        if (phase == StoryPhase.Ch1_Delivering && !routeChosen && x >= 7.2f)
        {
            if (z <= -0.55f) { routeChosen = true; routeName = "Calçadão"; }
            else if (z >= 0.45f) { routeChosen = true; routeName = "Praça da Feira"; }
        }

        if (phase == StoryPhase.Ch1_ReturningForUpgrade && x <= ReturnX + 2.8f)
        {
            phase = StoryPhase.Ch1_ChooseUpgrade;
            cartOpen = true;
            SaveProgress();
        }
    }

    private void ApplyWorldStateFromPhase(bool animateTransition)
    {
        bool hasUpgrade = phase >= StoryPhase.Ch2_HarvestBasil;
        if (kioskAwningUpgrade != null)
            kioskAwningUpgrade.SetActive(hasUpgrade && chosenUpgrade == "Toldo Listrado");
        if (cartPaintedSignUpgrade != null)
            cartPaintedSignUpgrade.SetActive(hasUpgrade && chosenUpgrade == "Placa Pintada");
        if (basilPot != null)
            basilPot.gameObject.SetActive(hasUpgrade);

        bool windActive = phase == StoryPhase.Ch2_CollectCards;
        if (ambientController != null) ambientController.SetWindGust(windActive);

        if (scatteredCards != null)
        {
            for (int i = 0; i < scatteredCards.Length; i++)
            {
                if (scatteredCards[i] == null) continue;
                bool showCard = windActive && ((collectedCardsMask & (1 << i)) == 0);
                scatteredCards[i].SetActive(showCard);
            }
        }

        if (sisterMarina != null)
        {
            Vector3 pavBase = pavilion != null ? pavilion.position : new Vector3(26.0f, 0f, -10.0f);
            if (phase == StoryPhase.Ch2_DeliverInviteToMarina)
            {
                sisterMarina.gameObject.SetActive(true);
                sisterMarina.position = new Vector3(-7.6f, 0f, -1.65f);
                sisterMarina.rotation = Quaternion.Euler(0f, -25f, 0f);
            }
            else if (phase == StoryPhase.Ch3_PrepareFestivalTreat || phase == StoryPhase.Ch3_RestorePavilion)
            {
                sisterMarina.gameObject.SetActive(true);
                sisterMarina.position = pavBase + new Vector3(-1.8f, 0.36f, 1.4f);
                sisterMarina.rotation = Quaternion.Euler(0f, 15f, 0f);
            }
            else if (phase == StoryPhase.Ch4_FestivalFinale)
            {
                sisterMarina.gameObject.SetActive(true);
                sisterMarina.position = pavBase + new Vector3(-1.3f, 0.36f, 1.35f);
                sisterMarina.rotation = Quaternion.Euler(0f, 18f, 0f);
            }
            else
            {
                sisterMarina.gameObject.SetActive(false);
            }
        }

        bool finale = phase == StoryPhase.Ch4_FestivalFinale;
        if (pavilionFestivalTable != null) pavilionFestivalTable.SetActive(finale);
        if (pavilionFestivalLights != null) pavilionFestivalLights.SetActive(finale);
        if (pavilionFestivalDishes != null) pavilionFestivalDishes.SetActive(finale);
        if (pavilionFadedText != null) pavilionFadedText.gameObject.SetActive(!finale);
        if (pavilionRestoredText != null) pavilionRestoredText.gameObject.SetActive(finale);
        if (ambientController != null) ambientController.SetFestivalLights(finale);

        if (finale)
        {
            Vector3 pavBase = pavilion != null ? pavilion.position : new Vector3(26.0f, 0f, -10.0f);
            MoveNpcTo(friendNina, pavBase + new Vector3(-2.1f, 0.36f, 1.9f), -35f);
            MoveNpcTo(friendBia, pavBase + new Vector3(2.0f, 0.36f, 1.8f), 38f);
            MoveNpcTo(florist, pavBase + new Vector3(1.1f, 0.36f, 1.35f), -18f);
            MoveNpcTo(pedestrianBlue, pavBase + new Vector3(-1.0f, 0.36f, 2.8f), 175f);
            MoveNpcTo(pedestrianCoral, pavBase + new Vector3(1.0f, 0.36f, 2.8f), -170f);
            if (ambientController != null)
            {
                ambientController.SnapIdleBasePositions();
                if (animateTransition) ambientController.ApplyTimePreset(GirlPowerOrla3DAmbient.TimePreset.FimDeTarde);
            }
        }
        else
        {
            MoveNpcTo(friendNina, ninaHomePos, -18f);
            MoveNpcTo(friendBia, biaHomePos, 14f);
            MoveNpcTo(florist, floristHomePos, 15f);
            MoveNpcTo(pedestrianBlue, betoHomePos, 25f);
            MoveNpcTo(pedestrianCoral, claraHomePos, -22f);
            if (ambientController != null) ambientController.SnapIdleBasePositions();
        }

        if (photoOverlay != null && phase >= StoryPhase.Ch1_ReturningForUpgrade && !photoOverlay.IsUnlocked)
        {
            photoOverlay.UnlockAndShow();
            photoOverlay.Close();
        }

        if (characterAnimator != null)
            characterAnimator.SetHoldingCone(phase == StoryPhase.Ch1_Delivering || phase == StoryPhase.Ch3_RestorePavilion);
    }

    private static void MoveNpcTo(Transform npc, Vector3 pos, float rotY)
    {
        if (npc == null || pos == Vector3.zero) return;
        npc.position = pos;
        npc.rotation = Quaternion.Euler(0f, rotY, 0f);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(SavePrefix + "Phase", (int)phase);
        PlayerPrefs.SetString(SavePrefix + "Upgrade", chosenUpgrade);
        PlayerPrefs.SetString(SavePrefix + "Invite", inviteApproach);
        PlayerPrefs.SetString(SavePrefix + "Route", routeName);
        PlayerPrefs.SetInt(SavePrefix + "Basil", basilLeaves);
        PlayerPrefs.SetInt(SavePrefix + "Cards", collectedCardsMask);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        phase = (StoryPhase)Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "Phase", 0), 0, (int)StoryPhase.Ch4_FestivalFinale);
        chosenUpgrade = PlayerPrefs.GetString(SavePrefix + "Upgrade", "");
        inviteApproach = PlayerPrefs.GetString(SavePrefix + "Invite", "");
        routeName = PlayerPrefs.GetString(SavePrefix + "Route", "");
        basilLeaves = PlayerPrefs.GetInt(SavePrefix + "Basil", 0);
        collectedCardsMask = PlayerPrefs.GetInt(SavePrefix + "Cards", 0);
    }

    private bool NearXZ(Transform target, float radius)
    {
        if (target == null || player == null) return false;
        Vector2 a = new Vector2(player.position.x, player.position.z);
        Vector2 b = new Vector2(target.position.x, target.position.z);
        return Vector2.Distance(a, b) <= radius;
    }

    // ------------------------------------------------------------------
    // REFINED COZY GAMEPLAY HUD (Clean, Visual, Illustrated, Diegetic)
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();

        float s = Mathf.Clamp(Screen.width / 1440f, 0.82f, 1.15f);

        // Floating diegetic numbers (+10 moedas, +1 Manjericão...)
        GirlPowerOrla3DUITheme.UpdateAndDrawFloatingFeedback(sceneCamera, Time.deltaTime, s);

        // 1. TOP-LEFT: Compact Illustrated Client Order Card (NO static WASD wall of text!)
        DrawCompactOrderCard(s);

        // 2. TOP-RIGHT: Icon-Only Environment Strip (Sun/Moon + Wave + Photo + Pause ||)
        DrawTopRightIconBar(s);

        // 3. STORY CHOICE MODALS (Clean rounded panels when choosing flavor / upgrade / invite)
        DrawStoryChoicePanels(s);

        bool dialogueOpen = dialogueController != null && dialogueController.IsOpen;
        bool photoShowing = photoOverlay != null && photoOverlay.IsShowing;
        bool shopOpen = lifeActivities != null && lifeActivities.IsShopMenuOpen;
        bool megaOpen = megaSystems != null && megaSystems.IsAnyModalOpen;

        // Floating '!' icon over Florista Luna during Invisible Onboarding pan!
        if (onboardingTimer > 1.0f && florist != null && sceneCamera != null)
        {
            Vector3 sp = sceneCamera.WorldToScreenPoint(florist.position + Vector3.up * 2.25f);
            if (sp.z > 0.1f)
            {
                float sz = 44f * s;
                Rect ex = new Rect(sp.x - sz * 0.5f, (Screen.height - sp.y) - sz, sz, sz);
                GUI.DrawTexture(ex, GirlPowerOrla3DUITheme.CircleCoralTex);
                GUI.Label(ex, "!", new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(24f * s) });
            }
        }

        if (!dialogueOpen && !photoShowing && !shopOpen && !megaOpen)
        {
            // Preparation progress bar above player while crafting ice cream!
            if (craftProgressTimer > 0f && sceneCamera != null)
            {
                Vector3 sp = sceneCamera.WorldToScreenPoint(player.position + Vector3.up * 2.25f);
                if (sp.z > 0.1f)
                {
                    float pw = 240f * s;
                    float ph = 46f * s;
                    Rect cr = new Rect(Mathf.Clamp(sp.x - pw * 0.5f, 16f, Screen.width - pw - 16f), Mathf.Clamp((Screen.height - sp.y) - ph, 80f, Screen.height - 140f), pw, ph);
                    GUI.Box(cr, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
                    GUI.Label(new Rect(cr.x + 10f * s, cr.y + 4f * s, pw - 20f * s, 20f * s), craftProgressLabel, new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle) { alignment = TextAnchor.MiddleCenter });
                    float prog = 1f - (craftProgressTimer / Mathf.Max(0.01f, craftProgressDuration));
                    GirlPowerOrla3DUITheme.DrawProgressBar(new Rect(cr.x + 14f * s, cr.y + 26f * s, pw - 28f * s, 12f * s), prog, new Color(0.32f, 0.84f, 0.62f));
                }
            }
            else
            {
                // 4. DIEGETIC FLOATING [E] PROMPT ABOVE NEARBY OBJECT
                DrawWorldSpaceInteractPrompt(s);
            }

            // 5. SMART FLOATING ANALOG JOYSTICK (Hidden on PC/WebGL with Keyboard!)
            if (ShouldShowVirtualJoystick())
                DrawFloatingAnalogJoystick(s);
        }
    }

    private void DrawCompactOrderCard(float s)
    {
        Rect safe = GetGuiSafeArea();
        float maxW = Mathf.Min(356f * s, safe.width * 0.54f);
        float cw = Mathf.Max(230f * s, maxW);
        float ch = (gameFlow != null && gameFlow.HasActiveTimer) ? 104f * s : 88f * s;
        Rect card = new Rect(safe.x + 12f * s, safe.y + 12f * s, cw, ch);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        GetOrderVisuals(out Texture2D clientIcon, out Texture2D itemIcon, out string chapterTag, out string shortTask, out string counterBadge);

        // Client portrait badge on left + overlapping order item mini-icon
        float iconSz = 54f * s;
        GUI.DrawTexture(new Rect(card.x + 10f * s, card.y + 10f * s, iconSz, iconSz), clientIcon);
        float miniSz = 28f * s;
        GUI.DrawTexture(new Rect(card.x + 36f * s, card.y + 36f * s, miniSz, miniSz), itemIcon);

        // Concise directional distance + progress counter (accessible shape+text badges with strong contrast over bright ocean!)
        Transform objTarget = GetActiveObjectiveTarget();
        string distStr = "";
        if (objTarget != null && player != null && !NearXZ(objTarget, 3.0f))
        {
            Vector3 d = objTarget.position - player.position;
            float m = new Vector2(d.x, d.z).magnitude;
            string arr = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? (d.x > 0f ? "→" : "←") : (d.z > 0f ? "↑" : "↓");
            distStr = arr + " " + Mathf.RoundToInt(m) + "m";
        }

        Color darkOutline = new Color(0.10f, 0.08f, 0.12f, 0.94f);
        GUIStyle badgeSt = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(16.5f * s) };
        if (!string.IsNullOrEmpty(counterBadge))
        {
            Rect cntRect = new Rect(card.x + 72f * s, card.y + 8f * s, 72f * s, 26f * s);
            GUI.Box(cntRect, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(cntRect, counterBadge, badgeSt, Color.white, darkOutline, 1.2f);
        }
        if (!string.IsNullOrEmpty(distStr))
        {
            Rect distRect = new Rect(card.x + 150f * s, card.y + 8f * s, Mathf.Min(96f * s, cw - 224f * s), 26f * s);
            if (distRect.width > 42f * s)
            {
                GUI.Box(distRect, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
                GirlPowerOrla3DUITheme.DrawOutlinedLabel(distRect, distStr, badgeSt, Color.white, darkOutline, 1.2f);
            }
        }

        // One-tap "▶ IR / FAZER" quick-action button on the Objective Card -> auto-walks via A* and executes the task!
        if (objTarget != null)
        {
            float goW = 68f * s;
            Rect goRect = new Rect(card.xMax - goW - 8f * s, card.y + 7f * s, goW, 28f * s);
            string goLbl = NearXZ(objTarget, interactionRadius)
                ? GirlPowerOrla3DLocalization.T("▶ OK", "▶ DO", "▶ YA")
                : GirlPowerOrla3DLocalization.T("▶ IR", "▶ GO", "▶ IR");
            if (GUI.Button(goRect, goLbl, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                GuideAndPerformActiveObjective();
        }

        // Wrapped localized objective text (supports longer PT/EN/ES strings cleanly without truncation!)
        GUIStyle taskSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            fontSize = Mathf.RoundToInt(16.5f * s),
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        GirlPowerOrla3DUITheme.DrawShadowedLabel(new Rect(card.x + 72f * s, card.y + 38f * s, cw - 82f * s, 44f * s), shortTask, taskSt, new Color(0.18f, 0.11f, 0.08f), new Color(1f, 0.98f, 0.92f, 0.92f), 1.0f);

        // Sleek Daylight/Patience Progress Bar tucked at bottom edge
        if (gameFlow != null && gameFlow.HasActiveTimer)
        {
            float ratio = Mathf.Clamp01(gameFlow.ChapterTimeRemaining / Mathf.Max(1f, gameFlow.ChapterTimeMax));
            Color barCol = ratio > 0.45f
                ? new Color(0.165f, 0.776f, 0.80f)
                : (ratio > 0.22f ? new Color(0.95f, 0.76f, 0.34f) : new Color(0.925f, 0.435f, 0.643f));
            Rect barRect = new Rect(card.x + 12f * s, card.yMax - 18f * s, cw - 84f * s, 10f * s);
            GirlPowerOrla3DUITheme.DrawProgressBar(barRect, ratio, barCol);

            GUIStyle tmSt = new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.RoundToInt(15.5f * s),
                normal = { textColor = ratio <= 0.22f ? new Color(0.84f, 0.16f, 0.26f) : new Color(0.24f, 0.16f, 0.12f) }
            };
            GUI.Label(new Rect(card.xMax - 70f * s, card.yMax - 24f * s, 58f * s, 22f * s), GirlPowerOrla3DGameFlow.FormatSeconds(gameFlow.ChapterTimeRemaining), tmSt);
        }

        // Compact Icon-Only Resource Strip directly below (High-contrast Coins + 5 Pet Stars + Herbs!)
        Rect resStrip = new Rect(card.x, card.yMax + 6f * s, cw, 42f * s);
        GUI.Box(resStrip, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        int coins = lifeActivities != null ? lifeActivities.Coins : 0;
        int petRep = petFriendlySystem != null ? petFriendlySystem.PetReputation : 100;
        int petStars = Mathf.Clamp(Mathf.RoundToInt(petRep / 20f), 0, 5);
        int herbs = basilLeaves + (megaSystems != null ? megaSystems.Data.organicHerbs : 0);

        GUIStyle numSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(19f * s), alignment = TextAnchor.MiddleLeft };
        float rx = resStrip.x + 10f * s;
        float ry = resStrip.y + 7f * s;
        float isz = 26f * s;

        GUI.DrawTexture(new Rect(rx, ry, isz, isz), GirlPowerOrla3DUITheme.IconCoin);
        GirlPowerOrla3DUITheme.DrawOutlinedLabel(new Rect(rx + isz + 4f * s, resStrip.y + 4f * s, 54f * s, 34f * s), coins.ToString(), numSt, new Color(1f, 0.94f, 0.68f), darkOutline, 1.3f);
        rx += 82f * s;

        GUI.DrawTexture(new Rect(rx, ry + 1f * s, 22f * s, 22f * s), GirlPowerOrla3DUITheme.IconPaw);
        rx += 26f * s;
        float starSz = Mathf.Min(20f * s, (resStrip.xMax - rx - 74f * s) / 5.4f);
        for (int st = 0; st < 5; st++)
        {
            Texture2D starTex = (st < petStars) ? GirlPowerOrla3DUITheme.IconStarFilled : GirlPowerOrla3DUITheme.IconStarEmpty;
            GUI.DrawTexture(new Rect(rx + st * (starSz + 2f * s), resStrip.y + 11f * s, starSz, starSz), starTex);
        }
        rx += 5 * (starSz + 2f * s) + 10f * s;

        if (rx + 54f * s <= resStrip.xMax)
        {
            GUI.DrawTexture(new Rect(rx, ry + 1f * s, 24f * s, 24f * s), GirlPowerOrla3DUITheme.IconBasil);
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(new Rect(rx + 26f * s, resStrip.y + 4f * s, 38f * s, 34f * s), herbs.ToString(), numSt, new Color(0.84f, 1f, 0.78f), darkOutline, 1.3f);
        }

        topLeftHudRect = new Rect(card.x - 6f, card.y - 6f, cw + 12f, (resStrip.yMax - card.y) + 12f);

        // Short, non-blocking Florist delivery reaction banner (never blocks movement or covers the walkway!)
        if (floristReactionTimer > 0f && !string.IsNullOrEmpty(floristReactionText))
        {
            float bw = Mathf.Min(540f * s, safe.width - 28f * s);
            float bh = 68f * s;
            float bx = safe.x + (safe.width - bw) * 0.5f;
            float by = safe.y + 12f * s;
            if (bx < topLeftHudRect.xMax + 8f) by = topLeftHudRect.yMax + 8f;
            Rect toast = new Rect(bx, by, bw, bh);
            GUI.Box(toast, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            GUI.DrawTexture(new Rect(toast.x + 10f * s, toast.y + 10f * s, 48f * s, 48f * s), GirlPowerOrla3DUITheme.IconPortraitLuna);
            GUIStyle reactSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(16f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(toast.x + 66f * s, toast.y + 6f * s, bw - 78f * s, bh - 12f * s), floristReactionText, reactSt);
        }
    }

    private void GetOrderVisuals(out Texture2D client, out Texture2D item, out string tag, out string task, out string counter)
    {
        counter = "";
        switch (phase)
        {
            case StoryPhase.Ch1_AtCart:
                client = GirlPowerOrla3DUITheme.IconPortraitNina;
                item = GirlPowerOrla3DUITheme.IconIceCreamLemonBasil;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 1", "CHAPTER 1", "CAPÍTULO 1");
                task = GirlPowerOrla3DLocalization.T(
                    "Montar Limão + Manjericão no carrinho",
                    "Build Lemon + Basil cone at the cart",
                    "Armar Limón + Albahaca en el carrito");
                counter = "0/1";
                break;
            case StoryPhase.Ch1_Delivering:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconIceCreamLemonBasil;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 1", "CHAPTER 1", "CAPÍTULO 1");
                task = GirlPowerOrla3DLocalization.T(
                    "Entregar sorvete à Florista Luna",
                    "Deliver ice cream to Florist Luna",
                    "Entregar helado a la Florista Luna");
                counter = "0/1";
                break;
            case StoryPhase.Ch1_ReturningForUpgrade:
            case StoryPhase.Ch1_ChooseUpgrade:
                client = GirlPowerOrla3DUITheme.IconPortraitNina;
                item = GirlPowerOrla3DUITheme.IconBrush;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 1", "CHAPTER 1", "CAPÍTULO 1");
                task = GirlPowerOrla3DLocalization.T(
                    "Voltar ao carrinho e escolher melhoria",
                    "Return to the cart and pick an upgrade",
                    "Volver al carrito y elegir una mejora");
                counter = "★ 1/1";
                break;
            case StoryPhase.Ch2_HarvestBasil:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconBasil;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 2", "CHAPTER 2", "CAPÍTULO 2");
                task = GirlPowerOrla3DLocalization.T(
                    "Colher Manjericão ao lado do carrinho",
                    "Harvest Basil beside the cart",
                    "Cosechar Albahaca junto al carrito");
                counter = "0/1";
                break;
            case StoryPhase.Ch2_CollectCards:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconPostcard;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 2", "CHAPTER 2", "CAPÍTULO 2");
                task = GirlPowerOrla3DLocalization.T(
                    "Recolher cartões levados pelo vento",
                    "Collect postcards blown by the wind",
                    "Recoger tarjetas llevadas por el viento");
                counter = CountCollectedCards() + "/4";
                break;
            case StoryPhase.Ch2_InviteChoice:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconPostcard;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 2", "CHAPTER 2", "CAPÍTULO 2");
                task = GirlPowerOrla3DLocalization.T(
                    "Falar com Luna sobre o convite",
                    "Talk to Luna about the invitation",
                    "Hablar con Luna sobre la invitación");
                counter = "4/4";
                break;
            case StoryPhase.Ch2_DeliverInviteToMarina:
                client = GirlPowerOrla3DUITheme.IconPortraitMarina;
                item = GirlPowerOrla3DUITheme.IconPostcard;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 2", "CHAPTER 2", "CAPÍTULO 2");
                task = GirlPowerOrla3DLocalization.T(
                    "Entregar convite à Marina no calçadão",
                    "Deliver invitation to Marina on the boardwalk",
                    "Entregar invitación a Marina en el paseo");
                counter = "♥";
                break;
            case StoryPhase.Ch3_PrepareFestivalTreat:
                client = GirlPowerOrla3DUITheme.IconPortraitNina;
                item = GirlPowerOrla3DUITheme.IconFestivalTrio;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 3", "CHAPTER 3", "CAPÍTULO 3");
                task = GirlPowerOrla3DLocalization.T(
                    "Preparar Trio da Festa com Manjericão",
                    "Prepare Festival Trio with Basil",
                    "Preparar Trío de Fiesta con Albahaca");
                counter = basilLeaves > 0 ? "OK" : "0/1";
                break;
            case StoryPhase.Ch3_RestorePavilion:
                client = GirlPowerOrla3DUITheme.IconPortraitPavilion;
                item = GirlPowerOrla3DUITheme.IconGoldenKey;
                tag = GirlPowerOrla3DLocalization.T("CAPÍTULO 3", "CHAPTER 3", "CAPÍTULO 3");
                task = GirlPowerOrla3DLocalization.T(
                    "Acender o Pavilhão na Maré Baixa",
                    "Light up the Pavilion at Low Tide",
                    "Encender el Pabellón en Marea Baja");
                counter = (ambientController != null && ambientController.isHighTide)
                    ? GirlPowerOrla3DLocalization.T("MARÉ!", "TIDE!", "¡MAREA!")
                    : GirlPowerOrla3DLocalization.T("PRONTO", "READY", "LISTO");
                break;
            default:
                client = GirlPowerOrla3DUITheme.IconPortraitPavilion;
                item = GirlPowerOrla3DUITheme.IconFestivalTrio;
                tag = GirlPowerOrla3DLocalization.T("FESTA FINAL", "FINAL FESTIVAL", "FIESTA FINAL");
                task = GirlPowerOrla3DLocalization.T(
                    "Orla das Fadas viva! Explore e celebre",
                    "Orla das Fadas alive! Explore & celebrate",
                    "¡Orla das Fadas viva! Explora y celebra");
                counter = "★★★";
                break;
        }
    }

    private void DrawTopRightIconBar(float s)
    {
        // Safe-area anchored Top-Right frosted-glass bar (scales cleanly for narrow & wide mobile screens!)
        Rect safe = GetGuiSafeArea();
        bool hasPhoto = photoOverlay != null && photoOverlay.IsUnlocked;
        int count = hasPhoto ? 7 : 6;
        float maxBarW = Mathf.Max(220f * s, safe.width * 0.42f);
        float btnSz = Mathf.Min(42f * s, (maxBarW - 14f * s - (count - 1) * 6f * s) / count);
        float gap = 6f * s;
        float barW = count * btnSz + (count - 1) * gap + 14f * s;
        float barH = btnSz + 12f * s;
        Rect strip = new Rect(safe.xMax - barW - 12f * s, safe.y + 12f * s, barW, barH);
        GUI.Box(strip, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        topRightHudRect = new Rect(strip.x - 6f, strip.y - 6f, strip.width + 12f, strip.height + 54f * s);

        float x = strip.x + 7f * s;
        float y = strip.y + 6f * s;

        // 1. Cardápio / Shop Book Icon [TAB]
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconMenuBook, s))
            if (cardapioUI != null) cardapioUI.Toggle();
        x += btnSz + gap;

        // 2. Mobility / Sports Switcher Icon [V] (Walk/Skate/Scooter/Skates or Surf/Swim/Canoe)
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconMobility, s))
        {
            if (characterAnimator != null)
            {
                if (characterAnimator.IsInWater) characterAnimator.CycleWaterMode();
                else characterAnimator.CycleLandMode();
                if (audioController != null) audioController.PlayChime();
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, characterAnimator.GetActiveMobilityName(), new Color(0.165f, 0.776f, 0.80f), 1.1f);
            }
        }
        x += btnSz + gap;

        // 3. Pipoca Companion Paw Icon [B]
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconPaw, s))
            if (lifeActivities != null) lifeActivities.TriggerPetAction();
        x += btnSz + gap;

        // 4. Time-of-Day Icon Button (Sun / Sunset / Moon)
        Texture2D timeIcon = GirlPowerOrla3DUITheme.IconSunset;
        if (ambientController != null)
        {
            if (ambientController.currentPreset == GirlPowerOrla3DAmbient.TimePreset.ManhaTropical) timeIcon = GirlPowerOrla3DUITheme.IconSun;
            else if (ambientController.currentPreset == GirlPowerOrla3DAmbient.TimePreset.NoiteNaOrla) timeIcon = GirlPowerOrla3DUITheme.IconMoon;
        }
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), timeIcon, s))
            if (ambientController != null) ambientController.CycleTimePreset();
        x += btnSz + gap;

        // 5. Tide Wave Icon Button (Low / High)
        Texture2D waveIcon = (ambientController != null && ambientController.isHighTide)
            ? GirlPowerOrla3DUITheme.IconWaveHigh
            : GirlPowerOrla3DUITheme.IconWaveLow;
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), waveIcon, s))
            if (ambientController != null) ambientController.ToggleTide();
        x += btnSz + gap;

        // 6. Polaroid Photo Icon (only when unlocked)
        if (hasPhoto)
        {
            if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconPhoto, s))
                photoOverlay.ToggleShow();
            x += btnSz + gap;
        }

        // 7. Pause / System Menu Button (||)
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconPause, s))
            if (gameFlow != null) gameFlow.TogglePause();
    }

    private static bool DrawIconButton(Rect r, Texture2D icon, float s)
    {
        bool clicked = GUI.Button(r, GUIContent.none, GirlPowerOrla3DUITheme.BtnGlassIconStyle);
        float pad = 8f * s;
        if (icon != null)
            GUI.DrawTexture(new Rect(r.x + pad, r.y + pad, r.width - pad * 2f, r.height - pad * 2f), icon);
        return clicked;
    }

    private void DrawStoryChoicePanels(float s)
    {
        Rect safe = GetGuiSafeArea();
        bottomPanelHudRect = Rect.zero;

        if (phase == StoryPhase.Ch1_AtCart && cartOpen)
        {
            // BOTTOM-DOCKED ONE-HANDED ASSEMBLY PANEL:
            // - Florist's order always visible at top
            // - Assembled cone preview at center
            // - Illustrated & named ingredients with shape markers (never color-only!)
            // - "Confirmar pedido" button locked in the EXACT SAME position throughout assembly!
            float pw = Mathf.Min(680f * s, safe.width - 20f * s);
            float ph = Mathf.Min(292f * s, safe.height * 0.44f);
            float px = safe.x + (safe.width - pw) * 0.5f;
            float py = safe.yMax - ph - 10f * s;
            Rect box = new Rect(px, py, pw, ph);
            bottomPanelHudRect = box;
            GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

            // 1. Florist Order Header Bar (always visible while assembling!)
            Rect orderBar = new Rect(box.x + 12f * s, box.y + 10f * s, pw - 24f * s, 48f * s);
            GUI.Box(orderBar, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
            GUI.DrawTexture(new Rect(orderBar.x + 8f * s, orderBar.y + 5f * s, 38f * s, 38f * s), GirlPowerOrla3DUITheme.IconPortraitLuna);
            GUI.DrawTexture(new Rect(orderBar.xMax - 46f * s, orderBar.y + 5f * s, 38f * s, 38f * s), GirlPowerOrla3DUITheme.IconIceCreamLemonBasil);
            string orderText = GirlPowerOrla3DLocalization.T(
                "PEDIDO DA FLORISTA:  ◆ Limão + ♣ Manjericão",
                "FLORIST'S ORDER:  ◆ Lemon + ♣ Basil",
                "PEDIDO DE LA FLORISTA:  ◆ Limón + ♣ Albahaca");
            GUIStyle orderSt = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle)
            {
                fontSize = Mathf.RoundToInt(16f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(new Rect(orderBar.x + 50f * s, orderBar.y + 2f * s, orderBar.width - 100f * s, orderBar.height - 4f * s),
                orderText, orderSt, Color.white, new Color(0.08f, 0.16f, 0.18f, 0.92f), 1.2f);

            // 2. Center Cone Preview Area + Status Label
            float previewH = 74f * s;
            float previewY = orderBar.yMax + 8f * s;
            Rect previewBox = new Rect(box.x + 14f * s, previewY, pw - 28f * s, previewH);
            GUI.Box(previewBox, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            bool matchesOrder = selectedFlavor == "Limão com Manjericão";
            float coneSz = 58f * s;
            Rect coneRect = new Rect(previewBox.x + (previewBox.width - coneSz) * 0.5f - 78f * s, previewBox.y + (previewH - coneSz) * 0.5f, coneSz, coneSz);
            Color prevGuiCol = GUI.color;
            GUI.color = selectedFlavor == "Morango"
                ? new Color(1f, 0.72f, 0.82f)
                : (selectedFlavor == "Baunilha" ? new Color(1f, 0.96f, 0.82f) : Color.white);
            GUI.DrawTexture(coneRect, GirlPowerOrla3DUITheme.IconIceCreamLemonBasil);
            GUI.color = prevGuiCol;
            if (matchesOrder)
                GUI.DrawTexture(new Rect(coneRect.xMax - 20f * s, coneRect.y - 2f * s, 24f * s, 24f * s), GirlPowerOrla3DUITheme.IconBasil);

            string currentConeLabel = string.IsNullOrEmpty(selectedFlavor)
                ? GirlPowerOrla3DLocalization.T("Casquinha pronta — toque no sabor abaixo", "Cone ready — tap a flavor below", "Cono listo — toca un sabor abajo")
                : (matchesOrder
                    ? GirlPowerOrla3DLocalization.T("✓ Casquinha: ◆ Limão + ♣ Manjericão (Pronto!)", "✓ Cone: ◆ Lemon + ♣ Basil (Ready!)", "✓ Cono: ◆ Limón + ♣ Albahaca (¡Listo!)")
                    : GirlPowerOrla3DLocalization.T("Casquinha: " + selectedFlavor + " (troque para Limão + Manjericão)", "Cone: " + selectedFlavor + " (switch to Lemon + Basil)", "Cono: " + selectedFlavor + " (cambia a Limón + Albahaca)"));
            GUIStyle prevSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle)
            {
                fontSize = Mathf.RoundToInt(15.5f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(new Rect(coneRect.xMax + 12f * s, previewBox.y + 4f * s, previewBox.xMax - (coneRect.xMax + 20f * s), previewH - 8f * s),
                currentConeLabel, prevSt, matchesOrder ? new Color(0.86f, 1f, 0.82f) : Color.white, new Color(0.10f, 0.08f, 0.14f, 0.90f), 1.1f);

            // 3. Illustrated & Named Ingredient Tiles (accessible shape markers + clear selection check!)
            float rowY = previewBox.yMax + 8f * s;
            float gap = 8f * s;
            float tileW = (pw - 28f * s - gap * 2f) / 3f;
            float tileH = 66f * s;

            var options = new (string key, string label, Texture2D icon, Color tint)[]
            {
                ("Limão com Manjericão", GirlPowerOrla3DLocalization.T("◆ Limão +\n♣ Manjericão", "◆ Lemon +\n♣ Basil", "◆ Limón +\n♣ Albahaca"), GirlPowerOrla3DUITheme.IconIceCreamLemonBasil, Color.white),
                ("Morango", GirlPowerOrla3DLocalization.T("● Morango", "● Strawberry", "● Fresa"), GirlPowerOrla3DUITheme.IconIceCreamLemonBasil, new Color(1f, 0.70f, 0.82f)),
                ("Baunilha", GirlPowerOrla3DLocalization.T("▲ Baunilha", "▲ Vanilla", "▲ Vainilla"), GirlPowerOrla3DUITheme.IconIceCreamLemonBasil, new Color(1f, 0.95f, 0.80f))
            };

            GUIStyle tileLblSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(14.5f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            for (int i = 0; i < options.Length; i++)
            {
                bool isSel = selectedFlavor == options[i].key;
                Rect tr = new Rect(box.x + 14f * s + i * (tileW + gap), rowY, tileW, tileH);
                if (GUI.Button(tr, GUIContent.none, isSel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
                    SelectFlavor(options[i].key);
                float ic = 34f * s;
                Color oldC = GUI.color;
                GUI.color = options[i].tint;
                GUI.DrawTexture(new Rect(tr.x + 8f * s, tr.y + (tileH - ic) * 0.5f, ic, ic), options[i].icon);
                GUI.color = oldC;
                string txt = (isSel ? "✓ " : "") + options[i].label;
                Color tc = isSel ? Color.white : new Color(0.20f, 0.13f, 0.09f);
                tileLblSt.normal.textColor = tc;
                GUI.Label(new Rect(tr.x + ic + 14f * s, tr.y + 4f * s, tr.width - ic - 18f * s, tileH - 8f * s), txt, tileLblSt);
            }

            // 4. FIXED-POSITION BOTTOM ACTION BAR ("Confirmar pedido" stays in the EXACT SAME spot throughout assembly!)
            float btnY = rowY + tileH + 10f * s;
            float btnH = Mathf.Max(42f * s, box.yMax - btnY - 10f * s);
            float closeW = Mathf.Min(120f * s, (pw - 36f * s) * 0.28f);
            float confirmW = (pw - 36f * s) - closeW;

            string closeTxt = GirlPowerOrla3DLocalization.T("Fechar", "Close", "Cerrar");
            if (GUI.Button(new Rect(box.x + 14f * s, btnY, closeW, btnH), closeTxt, GirlPowerOrla3DUITheme.BtnSecondaryStyle))
                cartOpen = false;

            string confirmTxt = matchesOrder
                ? (IsTouchPrimary
                    ? GirlPowerOrla3DLocalization.T("✓  CONFIRMAR PEDIDO", "✓  CONFIRM ORDER", "✓  CONFIRMAR PEDIDO")
                    : GirlPowerOrla3DLocalization.T("✓  CONFIRMAR PEDIDO [E]", "✓  CONFIRM ORDER [E]", "✓  CONFIRMAR PEDIDO [E]"))
                : GirlPowerOrla3DLocalization.T("CONFIRMAR PEDIDO", "CONFIRM ORDER", "CONFIRMAR PEDIDO");
            Rect confirmRect = new Rect(box.x + 14f * s + closeW + 8f * s, btnY, confirmW, btnH);
            if (GUI.Button(confirmRect, confirmTxt, matchesOrder ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            {
                if (matchesOrder)
                    Interact();
                else
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart != null ? cart.position : player.position,
                        GirlPowerOrla3DLocalization.T("Escolha ◆ Limão + ♣ Manjericão!", "Pick ◆ Lemon + ♣ Basil!", "¡Elige ◆ Limón + ♣ Albahaca!"),
                        new Color(1f, 0.85f, 0.42f), 1.6f);
            }
        }
        else if (phase == StoryPhase.Ch1_ChooseUpgrade && cartOpen)
        {
            float pw = Mathf.Min(580f * s, safe.width - 24f * s);
            float ph = 128f * s;
            Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.yMax - ph - 14f * s, pw, ph);
            bottomPanelHudRect = box;
            GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            string upgTitle = GirlPowerOrla3DLocalization.T("ESCOLHA A MELHORIA DO QUIOSQUE:", "CHOOSE YOUR KIOSK UPGRADE:", "ELIGE LA MEJORA DEL QUIOSCO:");
            GUI.Label(new Rect(box.x + 18f * s, box.y + 12f * s, pw - 36f * s, 30f * s), upgTitle, GirlPowerOrla3DUITheme.BodyDarkStyle);
            float bw = (pw - 48f * s) * 0.5f;
            if (GUI.Button(new Rect(box.x + 18f * s, box.y + 56f * s, bw, 52f * s), GirlPowerOrla3DLocalization.T("1  Toldo Listrado", "1  Striped Awning", "1  Toldo Rayado"), GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                ApplyUpgradeChoice("Toldo Listrado");
            if (GUI.Button(new Rect(box.x + 30f * s + bw, box.y + 56f * s, bw, 52f * s), GirlPowerOrla3DLocalization.T("2  Placa Pintada", "2  Painted Sign", "2  Letrero Pintado"), GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                ApplyUpgradeChoice("Placa Pintada");
        }
        else if (phase == StoryPhase.Ch2_InviteChoice && inviteMenuOpen)
        {
            float pw = Mathf.Min(620f * s, safe.width - 24f * s);
            float ph = 128f * s;
            Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.yMax - ph - 14f * s, pw, ph);
            bottomPanelHudRect = box;
            GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            string invTitle = GirlPowerOrla3DLocalization.T("CONVITE PARA MARINA — COMO AJUDAR LUNA?", "INVITE FOR MARINA — HOW TO HELP LUNA?", "INVITACIÓN PARA MARINA — ¿CÓMO AYUDAR A LUNA?");
            GUI.Label(new Rect(box.x + 18f * s, box.y + 12f * s, pw - 36f * s, 30f * s), invTitle, GirlPowerOrla3DUITheme.BodyDarkStyle);
            float bw = (pw - 48f * s) * 0.5f;
            if (GUI.Button(new Rect(box.x + 18f * s, box.y + 56f * s, bw, 52f * s), GirlPowerOrla3DLocalization.T("1  Entregar a Marina", "1  Deliver to Marina", "1  Entregar a Marina"), GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                ChooseInviteApproach("Pessoalmente");
            if (GUI.Button(new Rect(box.x + 30f * s + bw, box.y + 56f * s, bw, 52f * s), GirlPowerOrla3DLocalization.T("2  Dar Coragem a Luna", "2  Encourage Luna", "2  Dar Ánimo a Luna"), GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                ChooseInviteApproach("Coragem");
        }
    }

    private void DrawWorldSpaceInteractPrompt(float s)
    {
        if (!TryGetNearbyInteractable(out Transform target, out string actionLabel)) return;
        if (target == null) return;

        Rect safe = GetGuiSafeArea();
        string text = IsTouchPrimary ? actionLabel : ("[E]  " + actionLabel);
        float w = Mathf.Clamp(86f * s + text.Length * 7.8f * s, 200f * s, Mathf.Min(340f * s, safe.width - 28f * s));
        float h = 46f * s;
        float x = safe.x + (safe.width - w) * 0.5f;
        float y = safe.yMax - 60f * s;

        Rect pill = new Rect(x, y, w, h);
        bottomPanelHudRect = pill;
        if (GUI.Button(pill, text, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            Interact();
    }

    private bool TryGetNearbyInteractable(out Transform target, out string label)
    {
        target = null;
        label = "";
        if (phase == StoryPhase.Ch2_CollectCards && scatteredCards != null)
        {
            for (int i = 0; i < scatteredCards.Length; i++)
                if (scatteredCards[i] != null && scatteredCards[i].activeSelf && NearXZ(scatteredCards[i].transform, 2.3f))
                { target = scatteredCards[i].transform; label = "Recolher Cartão"; return true; }
        }
        if (basilPot != null && basilPot.gameObject.activeSelf && NearXZ(basilPot, 2.3f))
        { target = basilPot; label = "Colher Manjericão"; return true; }
        if (NearXZ(cart, interactionRadius))
        {
            target = cart;
            if (phase == StoryPhase.Ch1_AtCart) { label = cartOpen ? "Montar Sorvete" : "Abrir Carrinho"; return true; }
            if (phase == StoryPhase.Ch1_ChooseUpgrade) { label = "Escolher Melhoria"; return true; }
            if (phase == StoryPhase.Ch3_PrepareFestivalTreat) { label = "Preparar Trio"; return true; }
        }
        if (sisterMarina != null && sisterMarina.gameObject.activeSelf && NearXZ(sisterMarina, 2.8f))
        { target = sisterMarina; label = phase == StoryPhase.Ch2_DeliverInviteToMarina ? "Entregar Convite" : "Falar com Marina"; return true; }
        if (NearXZ(florist, interactionRadius))
        {
            target = florist;
            if (phase == StoryPhase.Ch1_Delivering) { label = "Entregar Sorvete"; return true; }
            if (phase == StoryPhase.Ch2_InviteChoice) { label = "Mostrar Convite"; return true; }
            label = "Falar com Luna"; return true;
        }
        if (NearXZ(pavilion, 4.0f))
        { target = pavilion; label = phase == StoryPhase.Ch3_RestorePavilion ? "Acender Pavilhão" : "Ver Pavilhão"; return true; }
        if (NearXZ(friendNina, 2.5f)) { target = friendNina; label = "Falar com Nina"; return true; }
        if (NearXZ(friendBia, 2.7f)) { target = friendBia; label = "Falar com Bia"; return true; }
        if (NearXZ(pedestrianBlue, 2.6f)) { target = pedestrianBlue; label = "Falar com Beto"; return true; }
        if (NearXZ(pedestrianCoral, 2.6f)) { target = pedestrianCoral; label = "Falar com Clara"; return true; }
        return false;
    }

    private void DrawDirectionalObjectiveIndicator(float s)
    {
        Transform target = GetActiveObjectiveTarget();
        if (target == null || sceneCamera == null || NearXZ(target, 3.8f)) return;

        Vector3 sp = sceneCamera.WorldToScreenPoint(target.position + Vector3.up * 1.4f);
        bool onScreen = sp.z > 0f && sp.x > 60f && sp.x < Screen.width - 60f && sp.y > 90f && sp.y < Screen.height - 90f;
        if (onScreen) return;

        Vector3 dir = target.position - player.position;
        float distMeters = new Vector2(dir.x, dir.z).magnitude;
        string arrow = Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
            ? (dir.x > 0f ? "→" : "←")
            : (dir.z > 0f ? "↑" : "↓");

        float pw = 132f * s;
        float ph = 34f * s;
        float px = dir.x > 0f ? Screen.width - pw - 22f * s : 22f * s;
        float py = Screen.height * 0.46f;

        Rect ind = new Rect(px, py, pw, ph);
        GUI.Box(ind, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
        GUIStyle st = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(13f * s) };
        GUI.Label(ind, arrow + "  Destino " + Mathf.RoundToInt(distMeters) + "m", st);
    }

    // ------------------------------------------------------------------
    // SMART MULTIPLATFORM FLOATING ANALOG JOYSTICK
    // ------------------------------------------------------------------
    private bool ShouldShowVirtualJoystick()
    {
        // Mobile-first 3-layer touch (Pan + Tap-to-Move + Direct Drag & Drop) keeps the screen 100% clean;
        // only show the legacy floating joystick if explicitly requested in Settings -> AlwaysShow.
        return gameFlow != null
            && gameFlow.ControlMode == GirlPowerOrla3DGameFlow.TouchControlMode.AlwaysShow
            && (!keyboardUsedRecently || touchDetected);
    }

    private void DrawFloatingAnalogJoystick(float s)
    {
        float baseRadius = 58f * s;
        float knobRadius = 24f * s;
        Vector2 restingCenter = new Vector2(104f * s, Screen.height - 155f * s);
        Rect touchZone = new Rect(14f * s, Screen.height - 270f * s, 240f * s, 210f * s);

        Event ev = Event.current;
        if (ev.type == EventType.MouseDown && touchZone.Contains(ev.mousePosition))
        {
            joyDragging = true;
            joyCenterGui = ev.mousePosition;
            joyVector = Vector2.zero;
            ev.Use();
        }
        else if (joyDragging && (ev.type == EventType.MouseDrag || ev.type == EventType.MouseMove))
        {
            Vector2 delta = ev.mousePosition - joyCenterGui;
            Vector2 clamped = Vector2.ClampMagnitude(delta, baseRadius);
            joyVector = new Vector2(clamped.x / baseRadius, -clamped.y / baseRadius);
            ev.Use();
        }
        else if (joyDragging && ev.type == EventType.MouseUp)
        {
            joyDragging = false;
            joyVector = Vector2.zero;
            ev.Use();
        }

        Vector2 center = joyDragging ? joyCenterGui : restingCenter;
        GUI.DrawTexture(new Rect(center.x - baseRadius, center.y - baseRadius, baseRadius * 2f, baseRadius * 2f), GirlPowerOrla3DUITheme.CircleGlassTex);

        Vector2 knobPos = center + new Vector2(joyVector.x * baseRadius, -joyVector.y * baseRadius);
        GUI.DrawTexture(new Rect(knobPos.x - knobRadius, knobPos.y - knobRadius, knobRadius * 2f, knobRadius * 2f), GirlPowerOrla3DUITheme.CircleCoralTex);

        // Right-side clean circular Interact & Sprint buttons for touch players
        float actSz = 66f * s;
        Rect actRect = new Rect(Screen.width - actSz - 28f * s, Screen.height - actSz - 28f * s, actSz, actSz);
        GUI.DrawTexture(actRect, GirlPowerOrla3DUITheme.CircleCoralTex);
        if (GUI.Button(actRect, "E", GirlPowerOrla3DUITheme.BadgeTextStyle))
            Interact();

        float sprSz = 48f * s;
        Rect sprRect = new Rect(actRect.x - sprSz - 16f * s, actRect.y + 14f * s, sprSz, sprSz);
        GUI.DrawTexture(sprRect, touchSprint ? GirlPowerOrla3DUITheme.CircleCoralTex : GirlPowerOrla3DUITheme.CircleGlassTex);
        if (GUI.Button(sprRect, touchSprint ? "RUN" : "»", new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(12f * s) }))
            touchSprint = !touchSprint;
    }
}
