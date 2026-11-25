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
    public BoxCollider waterSurfZone;

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
    public bool IsCraftingNow => craftProgressTimer > 0f;
    public bool HasNearbyStoryInteractable => TryGetNearbyInteractable(out _, out _);
    public bool IsAssemblingOrder => phase == StoryPhase.Ch1_AtCart || phase == StoryPhase.Ch3_PrepareFestivalTreat || craftProgressTimer > 0f;

    public bool TryResetMeltedDeliveryOrder()
    {
        if (phase != StoryPhase.Ch1_Delivering) return false;
        phase = StoryPhase.Ch1_AtCart;
        selectedFlavor = "Limão com Manjericão";
        selectedBaseFlavor = "Limão";
        selectedTopping = "Manjericão";
        if (characterAnimator != null) characterAnimator.SetHoldingCone(false);
        SaveProgress();
        Vector3 popPos = player != null ? player.position : (cart != null ? cart.position : Vector3.zero);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(
            popPos + Vector3.up * 1.4f,
            GirlPowerOrla3DLocalization.T(
                "O sorvete derreteu! Refaça no carrinho sem perder progresso.",
                "The ice cream melted! Remake it at the cart — progress kept.",
                "¡El helado se derritió! Prepáralo de nuevo en el carrito."),
            new Color(1f, 0.78f, 0.45f), 2.8f);
        return true;
    }

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
                return english
                    ? "Time ran out before the ice cream was assembled at the cart!"
                    : "O tempo acabou antes de montar o pedido no carrinho!";
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
    private string selectedBaseFlavor = "";
    private string selectedTopping = "";
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
    private float discreteNoticeTimer;
    private string discreteNoticeText = "";

    public void ShowDiscreteNotice(string text, float duration = 2.6f)
    {
        if (string.IsNullOrEmpty(text)) return;
        discreteNoticeText = text;
        discreteNoticeTimer = Mathf.Max(1.2f, duration);
    }

    public float CurrentZoomFactor => zoomFactor;

    public bool SetTapDestination(Vector3 worldPos, System.Action onArrive = null, bool requireReachable = false)
    {
        Vector3 requested = new Vector3(
            Mathf.Clamp(worldPos.x, minWorldX + 0.35f, maxWorldX - 0.35f),
            0f,
            Mathf.Clamp(worldPos.z, minWorldZ + 0.35f, maxWorldZ - 0.35f));
        tapArriveCallback = onArrive;
        tapRoute.Clear();
        GirlPowerOrla3DPathfinder pathfinder = EnsurePathfinder();
        if (pathfinder != null && player != null)
        {
            Vector3 safeRequested = pathfinder.FindNearestWalkablePoint(requested, Vector3.back, PlayerRadius + 0.08f);
            pathfinder.FindPath(player.position, safeRequested, tapRoute, PlayerRadius + 0.06f);
            if (requireReachable && tapRoute.Count > 0)
            {
                Vector3 endPt = tapRoute[tapRoute.Count - 1];
                if (Vector2.Distance(new Vector2(endPt.x, endPt.z), new Vector2(safeRequested.x, safeRequested.z)) > 1.85f)
                {
                    tapRoute.Clear();
                }
            }
        }
        else
        {
            tapRoute.Add(requested);
        }
        tapRouteIndex = 0;
        hasTapDest = tapRoute.Count > 0;
        if (hasTapDest) tapDestWorld = tapRoute[tapRoute.Count - 1];
        else tapArriveCallback = null;
        return hasTapDest;
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

    /// <summary>
    /// Computes an explicit walkable service position strictly IN FRONT of the target's counter/NPC
    /// (verified outside all OBB colliders so approaching from beach, fair, or behind the stand always arrives cleanly!).
    /// </summary>
    public Vector3 GetWalkableServicePoint(Transform target)
    {
        if (target == null) return player != null ? player.position : Vector3.zero;
        Vector3 desired;
        Vector3 frontDir = Vector3.back; // Counters face -Z toward camera/promenade

        if (target == cart)
        {
            desired = new Vector3(cart.position.x, 0f, cart.position.z - 1.48f);
            frontDir = Vector3.back;
        }
        else if (target == florist)
        {
            // Luna stands on the customer side (-Z) of FloristStand; stop cleanly in front of her!
            desired = new Vector3(florist.position.x, 0f, florist.position.z - 1.15f);
            frontDir = Vector3.back;
        }
        else if (target == basilPot)
        {
            desired = new Vector3(basilPot.position.x, 0f, basilPot.position.z - 1.05f);
            frontDir = Vector3.back;
        }
        else if (target == pavilion)
        {
            desired = new Vector3(pavilion.position.x, 0f, pavilion.position.z + 2.25f);
            frontDir = Vector3.forward;
        }
        else
        {
            Vector3 f = -target.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.01f) f = Vector3.back;
            frontDir = f.normalized;
            desired = target.position + frontDir * 1.45f;
            desired.y = 0f;
        }

        GirlPowerOrla3DPathfinder pf = EnsurePathfinder();
        if (pf != null)
            return pf.FindNearestWalkablePoint(desired, frontDir, PlayerRadius + 0.08f);
        return desired;
    }

    public bool IsPointInWaterZone(Vector3 worldPos)
    {
        if (waterSurfZone != null && waterSurfZone.enabled && waterSurfZone.gameObject.activeInHierarchy)
        {
            Vector3 local = waterSurfZone.transform.InverseTransformPoint(new Vector3(worldPos.x, waterSurfZone.transform.position.y + waterSurfZone.center.y, worldPos.z));
            Vector3 half = waterSurfZone.size * 0.5f;
            Vector3 c = waterSurfZone.center;
            return Mathf.Abs(local.x - c.x) <= half.x && Mathf.Abs(local.z - c.z) <= half.z;
        }
        // Safe fallback aligned to visible ocean shoreline mesh when running an older scene build
        if (ambientController != null && ambientController.oceanSurface != null)
            return worldPos.z >= ambientController.oceanSurface.position.z - 15.1f;
        return worldPos.z >= 10.9f;
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
        Vector3 frontPoint = GetWalkableServicePoint(target);
        if (!SetTapDestination(frontPoint, () => Interact(), true))
        {
            // Retry with a slightly wider front offset so the player still reaches the usable side cleanly
            SetTapDestination(frontPoint + new Vector3(0f, 0f, -0.8f), () => Interact(), false);
        }
    }

    public bool IsUiPointBlocked(Vector2 guiPos)
    {
        bool blockedByHub = GirlPowerOrla3DInteractionHub.Instance != null
            && GirlPowerOrla3DInteractionHub.Instance.ActiveButtonGuiRect.Contains(guiPos);
        return blockedByHub || topLeftHudRect.Contains(guiPos) || topRightHudRect.Contains(guiPos) || bottomPanelHudRect.Contains(guiPos);
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
        selectedBaseFlavor = "";
        selectedTopping = "";

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
                onboardingTimer = 2.15f; // Brief focus showing Florista Luna -> Cart
                // Start cleanly in front of the strawberry-pink Ice Cream Cart so BOTH Protagonist & Cart shine in frame!
                if (player != null) player.position = new Vector3(-0.9f, 0f, -2.05f);
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
                if (keys.eKey.wasPressedThisFrame)
                {
                    keyboardUsedRecently = true;
                    if (!cartOpen && !inviteMenuOpen && GirlPowerOrla3DInteractionHub.Instance != null && GirlPowerOrla3DInteractionHub.Instance.HasActiveCandidate)
                        GirlPowerOrla3DInteractionHub.Instance.TryExecuteActiveCandidate();
                    else
                        Interact();
                }
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
                    if (keys.digit1Key.wasPressedThisFrame) ToggleBaseIngredient("Limão");
                    if (keys.digit2Key.wasPressedThisFrame) ToggleBaseIngredient("Morango");
                    if (keys.digit3Key.wasPressedThisFrame) ToggleBaseIngredient("Baunilha");
                    if (keys.digit4Key.wasPressedThisFrame) ToggleToppingIngredient("Manjericão");
                    if (keys.digit5Key.wasPressedThisFrame) ToggleToppingIngredient("Hortelã");
                    if (keys.digit6Key.wasPressedThisFrame) ToggleToppingIngredient("Calda Berry");
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
        SubmitStoryInteractionCandidates();
        if (floristReactionTimer > 0f)
            floristReactionTimer = Mathf.Max(0f, floristReactionTimer - Time.deltaTime);
        if (discreteNoticeTimer > 0f)
            discreteNoticeTimer = Mathf.Max(0f, discreteNoticeTimer - Time.deltaTime);

        if (characterAnimator != null)
            characterAnimator.UpdateAnimation(input, running, talking, player.position, actualPlanarSpeed);

        FollowAnimalCrossingCamera(input);
    }

    private float DistToPlayerXZ(Transform target)
    {
        if (target == null || player == null) return float.MaxValue;
        return Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(target.position.x, target.position.z));
    }

    private void SubmitStoryInteractionCandidates()
    {
        var hub = GirlPowerOrla3DInteractionHub.Instance;
        if (hub == null || player == null || cartOpen || inviteMenuOpen) return;
        Transform activeObj = GetActiveObjectiveTarget();

        if (phase == StoryPhase.Ch2_CollectCards && scatteredCards != null)
        {
            for (int i = 0; i < scatteredCards.Length; i++)
            {
                if (scatteredCards[i] != null && scatteredCards[i].activeSelf)
                {
                    float d = DistToPlayerXZ(scatteredCards[i].transform);
                    if (d <= 2.3f)
                    {
                        int cardIdx = i;
                        hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                        {
                            id = "story_card_" + cardIdx, target = scatteredCards[cardIdx].transform,
                            buttonLabel = "Recolher Cartão", targetBadgeLabel = "Cartão",
                            distance = d, markerHeight = 1.25f, isPrimaryMission = true,
                            onExecute = () => CollectCard(cardIdx)
                        });
                    }
                }
            }
        }

        if (basilPot != null && basilPot.gameObject.activeSelf)
        {
            float d = DistToPlayerXZ(basilPot);
            if (d <= 2.3f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "story_basil", target = basilPot,
                    buttonLabel = "Colher Manjericão", targetBadgeLabel = "Manjericão",
                    distance = d, markerHeight = 1.35f, isPrimaryMission = (activeObj == basilPot),
                    onExecute = Interact
                });
            }
        }

        if (cart != null)
        {
            float d = DistToPlayerXZ(cart);
            if (d <= interactionRadius)
            {
                string cartLbl = null;
                if (phase == StoryPhase.Ch1_AtCart) cartLbl = "Montar sorvete";
                else if (phase == StoryPhase.Ch1_ChooseUpgrade) cartLbl = "Escolher melhoria";
                else if (phase == StoryPhase.Ch3_PrepareFestivalTreat) cartLbl = "Preparar Trio";
                if (!string.IsNullOrEmpty(cartLbl))
                {
                    hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                    {
                        id = "story_cart", target = cart,
                        buttonLabel = cartLbl, targetBadgeLabel = "Carrinho",
                        distance = d, markerHeight = 1.85f, isPrimaryMission = (activeObj == cart),
                        onExecute = Interact
                    });
                }
            }
        }

        if (sisterMarina != null && sisterMarina.gameObject.activeSelf)
        {
            float d = DistToPlayerXZ(sisterMarina);
            if (d <= 2.8f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "story_marina", target = sisterMarina,
                    buttonLabel = phase == StoryPhase.Ch2_DeliverInviteToMarina ? "Entregar Convite" : "Falar com Marina",
                    targetBadgeLabel = "Marina",
                    distance = d, markerHeight = 2.05f, isPrimaryMission = (activeObj == sisterMarina),
                    onExecute = Interact
                });
            }
        }

        if (florist != null)
        {
            float d = DistToPlayerXZ(florist);
            if (d <= interactionRadius)
            {
                string flLbl = phase == StoryPhase.Ch1_Delivering ? "Entregar Sorvete"
                    : (phase == StoryPhase.Ch2_InviteChoice ? "Mostrar Convite" : "Falar com Luna");
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "story_luna", target = florist,
                    buttonLabel = flLbl, targetBadgeLabel = "Florista Luna",
                    distance = d, markerHeight = 2.15f, isPrimaryMission = (activeObj == florist),
                    onExecute = Interact
                });
            }
        }

        if (pavilion != null)
        {
            float d = DistToPlayerXZ(pavilion);
            if (d <= 3.6f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "story_pavilion", target = pavilion,
                    buttonLabel = phase == StoryPhase.Ch3_RestorePavilion ? "Acender Pavilhão" : "Ver Pavilhão",
                    targetBadgeLabel = "Pavilhão",
                    distance = d, markerHeight = 2.40f, isPrimaryMission = (activeObj == pavilion),
                    onExecute = Interact
                });
            }
        }

        SubmitNpcCandidate(hub, "story_nina", friendNina, "Falar com Nina", "Nina", 2.4f);
        SubmitNpcCandidate(hub, "story_bia", friendBia, "Falar com Bia", "Bia", 2.5f);
        SubmitNpcCandidate(hub, "story_beto", pedestrianBlue, "Falar com Beto", "Beto", 2.5f);
        SubmitNpcCandidate(hub, "story_clara", pedestrianCoral, "Falar com Clara", "Clara", 2.5f);
    }

    private void SubmitNpcCandidate(GirlPowerOrla3DInteractionHub hub, string id, Transform npc, string btnLabel, string badgeLabel, float maxDist)
    {
        if (npc == null || !npc.gameObject.activeInHierarchy) return;
        float d = DistToPlayerXZ(npc);
        if (d > maxDist) return;
        hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
        {
            id = id, target = npc, buttonLabel = btnLabel, targetBadgeLabel = badgeLabel,
            distance = d, markerHeight = 2.02f, isPrimaryMission = false, onExecute = Interact
        });
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
        // Place the ground ring on the walkable FRONT service point (never hidden underneath the Cart or Florist stand!)
        Vector3 tp = GetWalkableServicePoint(target);
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
        bool isSurfingNow = IsPointInWaterZone(player.position);

        if (characterAnimator != null)
            characterAnimator.SetSurfing(isSurfingNow);

        float mobMult = characterAnimator != null ? characterAnimator.GetMobilitySpeedMultiplier() : 1f;
        float upgMult = megaSystems != null ? megaSystems.GetSpeedUpgradeMultiplier() : 1f;

        // Terrain physics: Scooter/Skate/Patins are faster on paved promenade/street & drag realistically on soft beach sand!
        bool onSoftSand = player.position.z >= 3.95f && !isSurfingNow;
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

        // Keep player safely inside scenic bounds; wheeled vehicles smoothly switch off at the explicit water shoreline!
        next.x = Mathf.Clamp(next.x, minWorldX, maxWorldX);
        next.z = Mathf.Clamp(next.z, minWorldZ, maxWorldZ);
        bool nextInWater = IsPointInWaterZone(next);
        if (isWheeled && nextInWater)
        {
            // Auto-dismount scooter/skate cleanly at the visible shoreline when entering the ocean!
            characterAnimator.SetLandMode(GirlPowerOrla3DCharacterAnimator.LandMode.Walk);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(next, "Entrando no mar! [V: Surf / Nadar / Canoa]", new Color(0.58f, 0.94f, 1f), 1.4f);
        }

        next.y = SampleGroundHeight(next.x, next.z);
        player.position = next;
        Vector3 realDisp = new Vector3(next.x - current.x, 0f, next.z - current.z);
        actualPlanarSpeed = realDisp.magnitude / dt;

        if (characterAnimator != null)
            characterAnimator.SetSurfing(nextInWater);

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

    private float SampleGroundHeight(float x, float z)
    {
        if (IsPointInWaterZone(new Vector3(x, 0f, z)))
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
        bool talking = dialogueController != null && dialogueController.IsOpen;
        bool closeUpFocus = !inWater && (cartOpen || inviteMenuOpen || talking);

        // Contextual Camera Modes:
        // 1. Close-Up Focus when assembling ice cream at Cart or conversing with Luna/NPCs!
        // 2. Stable Exploration framing showing feet + next walkway exit (default on land)
        // 3. Water framing when surfing/swimming!
        Vector3 baseOff = closeUpFocus
            ? new Vector3(0f, 5.5f, -7.2f)
            : (inWater ? new Vector3(0f, 9.6f, -13.6f) : cameraOffset);
        float targetPitch = closeUpFocus ? 32.5f : (inWater ? 36.0f : cameraPitch);
        float targetFov = closeUpFocus ? 40f : 44f;

        // Smart framing pull when approaching the Cart or Florist so BOTH protagonist and station stay well inside the frame!
        float arrivalLiftY = 0f;
        Vector2 servicePullXZ = Vector2.zero;
        if (!inWater)
        {
            Transform focusService = null;
            float cartFocusRange = (phase == StoryPhase.Ch1_AtCart || phase == StoryPhase.Ch1_ChooseUpgrade || phase == StoryPhase.Ch3_PrepareFestivalTreat) ? 8.2f : 5.0f;
            float floristFocusRange = (phase == StoryPhase.Ch1_Delivering || phase == StoryPhase.Ch2_InviteChoice) ? 7.2f : 4.8f;
            float bestRatio = 1f;

            if (cart != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(cart.position.x, cart.position.z));
                float r = d / cartFocusRange;
                if (r < bestRatio) { bestRatio = r; focusService = cart; }
            }
            if (florist != null)
            {
                float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(florist.position.x, florist.position.z));
                float r = d / floristFocusRange;
                if (r < bestRatio) { bestRatio = r; focusService = florist; }
            }
            if (focusService != null)
            {
                float blend = closeUpFocus ? 1f : Mathf.SmoothStep(0f, 1f, 1f - bestRatio);
                arrivalLiftY = closeUpFocus ? 0f : (blend * 0.35f);
                float pullWeight = closeUpFocus ? 0.42f : 0.34f;
                servicePullXZ = new Vector2(focusService.position.x - player.position.x, focusService.position.z - player.position.z) * (blend * pullWeight);
            }
        }

        Vector3 scaledOffset = baseOff * (closeUpFocus ? 1f : zoomFactor);
        float lookAheadX = closeUpFocus ? 0f : Mathf.Clamp(currentMoveVel.x * 0.24f, -1.4f, 1.4f);
        float lookAheadZ = closeUpFocus ? 0.15f : (Mathf.Clamp(currentMoveVel.y * 0.16f, -0.8f, 0.8f) + (inWater ? -0.35f : 0.35f));
        float focusX = Mathf.Clamp(player.position.x + lookAheadX + servicePullXZ.x + (closeUpFocus ? 0f : cameraPanOffsetXZ.x), minWorldX + 2.5f, maxWorldX - 2.5f);
        float focusZ = Mathf.Clamp(player.position.z + lookAheadZ + servicePullXZ.y + (closeUpFocus ? 0f : cameraPanOffsetXZ.y), minWorldZ + 1.2f, maxWorldZ - 2.0f);
        float desiredX = focusX;
        float desiredZ = focusZ + scaledOffset.z;
        float desiredY = player.position.y + scaledOffset.y + arrivalLiftY;
        Vector3 shake = GirlPowerOrla3DJuice.Instance != null ? GirlPowerOrla3DJuice.Instance.GetCameraShakeOffset() : Vector3.zero;
        Vector3 target = new Vector3(desiredX, desiredY, desiredZ) + shake;
        sceneCamera.transform.position = Vector3.SmoothDamp(sceneCamera.transform.position, target, ref cameraVelRef, cameraSmoothTime);
        sceneCamera.transform.rotation = Quaternion.Slerp(sceneCamera.transform.rotation, Quaternion.Euler(targetPitch, 0f, 0f), 1f - Mathf.Exp(-5.5f * Time.deltaTime));
        sceneCamera.fieldOfView = Mathf.Lerp(sceneCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-5.0f * Time.deltaTime));
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
                    if (audioController != null) audioController.PlayChime();
                }
                else
                {
                    ConfirmConeAssembly();
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

    private void ToggleBaseIngredient(string baseName)
    {
        selectedBaseFlavor = (selectedBaseFlavor == baseName) ? "" : baseName;
        SyncSelectedFlavorLabel();
        if (audioController != null) audioController.PlayChime();
    }

    private void ToggleToppingIngredient(string toppingName)
    {
        selectedTopping = (selectedTopping == toppingName) ? "" : toppingName;
        SyncSelectedFlavorLabel();
        if (audioController != null) audioController.PlayChime();
    }

    private void SyncSelectedFlavorLabel()
    {
        if (selectedBaseFlavor == "Limão" && selectedTopping == "Manjericão")
            selectedFlavor = "Limão com Manjericão";
        else if (!string.IsNullOrEmpty(selectedBaseFlavor) && !string.IsNullOrEmpty(selectedTopping))
            selectedFlavor = selectedBaseFlavor + " + " + selectedTopping;
        else
            selectedFlavor = !string.IsNullOrEmpty(selectedBaseFlavor) ? selectedBaseFlavor : selectedTopping;
    }

    private void ConfirmConeAssembly()
    {
        if (selectedBaseFlavor == "Limão" && selectedTopping == "Manjericão")
        {
            selectedFlavor = "Limão com Manjericão";
            cartOpen = false;
            craftProgressDuration = 0.60f;
            craftProgressTimer = craftProgressDuration;
            craftProgressLabel = GirlPowerOrla3DLocalization.T("Montando Limão + Manjericão...", "Building Lemon + Basil...", "Armando Limón + Albahaca...");
            if (audioController != null) audioController.PlayChime();
            if (characterAnimator != null && cart != null) characterAnimator.SpawnLeafBurst(cart.position);
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
                if (cart != null)
                {
                    GirlPowerOrla3DJuice.PopScale(cart, 1.16f, 0.34f);
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart.position, "★ Limão com Manjericão pronto!", new Color(0.84f, 0.98f, 0.54f));
                }
            };
            return;
        }

        string hint;
        if (string.IsNullOrEmpty(selectedBaseFlavor) || string.IsNullOrEmpty(selectedTopping))
            hint = GirlPowerOrla3DLocalization.T("Escolha o sabor (Limão) e o toque (Manjericão)!", "Pick flavor (Lemon) and topping (Basil)!", "¡Elige sabor (Limón) y toque (Albahaca)!");
        else if (selectedBaseFlavor != "Limão")
            hint = GirlPowerOrla3DLocalization.T("Troque o sabor para ◆ Limão!", "Switch the base to ◆ Lemon!", "¡Cambia el sabor a ◆ Limón!");
        else
            hint = GirlPowerOrla3DLocalization.T("Troque o toque para ♣ Manjericão!", "Switch the topping to ♣ Basil!", "¡Cambia el toque a ♣ Albahaca!");
        if (audioController != null) audioController.PlayFailChime();
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cart != null ? cart.position : player.position, hint, new Color(1f, 0.80f, 0.45f), 2.0f);
    }

    private void SelectFlavor(string flavor)
    {
        selectedFlavor = flavor;
        if (flavor == "Limão com Manjericão") { selectedBaseFlavor = "Limão"; selectedTopping = "Manjericão"; }
        else { selectedBaseFlavor = flavor; selectedTopping = ""; }
        if (audioController != null) audioController.PlayChime();
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

        // 4. Explicit world-space marker on the active mission point (shows where to assemble/deliver & lights up when in reach!)
        if (!dialogueOpen && !photoShowing && !shopOpen && !megaOpen && !cartOpen && !inviteMenuOpen)
            DrawActiveMissionTargetBadge(s);

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
        float maxW = Mathf.Min(318f * s, safe.width * 0.54f);
        float cw = Mathf.Max(238f * s, maxW);
        float ch = (gameFlow != null && gameFlow.HasActiveTimer) ? 98f * s : 82f * s;
        Rect card = new Rect(safe.x + 12f * s, safe.y + 10f * s, cw, ch);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        GetOrderVisuals(out Texture2D clientIcon, out Texture2D itemIcon, out string headline, out string subline, out string counterBadge);

        // Order item / portrait visual on left (high clarity on cream card)
        float iconSz = 50f * s;
        GUI.DrawTexture(new Rect(card.x + 10f * s, card.y + 12f * s, iconSz, iconSz), itemIcon);
        float miniSz = 24f * s;
        GUI.DrawTexture(new Rect(card.x + 38f * s, card.y + 38f * s, miniSz, miniSz), clientIcon);

        // Optional 1-tap "▶ IR / OK" pill tucked cleanly on top-right of the card
        Transform objTarget = GetActiveObjectiveTarget();
        float rightReserve = 12f * s;
        if (objTarget != null)
        {
            float goW = 58f * s;
            Rect goRect = new Rect(card.xMax - goW - 8f * s, card.y + 8f * s, goW, 26f * s);
            string goLbl = NearXZ(objTarget, interactionRadius)
                ? GirlPowerOrla3DLocalization.T("▶ OK", "▶ DO", "▶ YA")
                : GirlPowerOrla3DLocalization.T("▶ IR", "▶ GO", "▶ IR");
            if (GUI.Button(goRect, goLbl, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                GuideAndPerformActiveObjective();
            rightReserve = goW + 14f * s;
        }

        // LINE 1: Prominent bold dark headline (e.g. "Monte o sorvete")
        float textX = card.x + 68f * s;
        float textW = cw - 72f * s - rightReserve;
        GUIStyle headSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            fontSize = Mathf.RoundToInt(17.5f * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false
        };
        headSt.normal.textColor = new Color(0.16f, 0.10f, 0.08f, 1f);
        GUI.Label(new Rect(textX, card.y + 9f * s, textW, 26f * s), headline, headSt);

        // LINE 2: Clear dark subtitle directly below (e.g. "Limão + manjericão") + optional counter pill
        GUIStyle subSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            fontSize = Mathf.RoundToInt(14.5f * s),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        subSt.normal.textColor = new Color(0.28f, 0.20f, 0.16f, 1f);
        float subW = cw - 78f * s - (!string.IsNullOrEmpty(counterBadge) ? 56f * s : 0f);
        GUI.Label(new Rect(textX, card.y + 37f * s, subW, 34f * s), subline, subSt);

        if (!string.IsNullOrEmpty(counterBadge))
        {
            Rect cntRect = new Rect(card.xMax - 56f * s, card.y + 39f * s, 48f * s, 22f * s);
            GUI.Box(cntRect, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
            GUIStyle badgeSt = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(13f * s) };
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(cntRect, counterBadge, badgeSt, Color.white, new Color(0.08f, 0.12f, 0.14f, 0.9f), 1.0f);
        }

        // Countdown timer bar ONLY during optional timed challenges (hidden on free first delivery!)
        if (gameFlow != null && gameFlow.HasActiveTimer)
        {
            float ratio = Mathf.Clamp01(gameFlow.ChapterTimeRemaining / Mathf.Max(1f, gameFlow.ChapterTimeMax));
            Color barCol = ratio > 0.45f
                ? new Color(0.208f, 0.659f, 0.706f)
                : (ratio > 0.22f ? new Color(0.95f, 0.76f, 0.34f) : new Color(0.925f, 0.353f, 0.557f));
            Rect barRect = new Rect(card.x + 12f * s, card.yMax - 15f * s, cw - 74f * s, 9f * s);
            GirlPowerOrla3DUITheme.DrawProgressBar(barRect, ratio, barCol);

            GUIStyle tmSt = new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.RoundToInt(14f * s),
                normal = { textColor = ratio <= 0.22f ? new Color(0.84f, 0.16f, 0.26f) : new Color(0.24f, 0.16f, 0.12f) }
            };
            GUI.Label(new Rect(card.xMax - 60f * s, card.yMax - 21f * s, 50f * s, 20f * s), GirlPowerOrla3DGameFlow.FormatSeconds(gameFlow.ChapterTimeRemaining), tmSt);
        }

        topLeftHudRect = new Rect(card.x - 6f, card.y - 6f, cw + 12f, ch + 12f);

        // Discrete background notice strip directly beneath the Mission Card (away from stalls & walkway!)
        if (discreteNoticeTimer > 0f && !string.IsNullOrEmpty(discreteNoticeText))
        {
            Rect noticeRect = new Rect(card.x, card.yMax + 6f * s, cw, 30f * s);
            GUI.Box(noticeRect, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            GUIStyle nSt = new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle)
            {
                fontSize = Mathf.RoundToInt(13f * s),
                alignment = TextAnchor.MiddleLeft
            };
            nSt.normal.textColor = new Color(0.22f, 0.34f, 0.24f, 1f);
            GUI.Label(new Rect(noticeRect.x + 10f * s, noticeRect.y + 2f * s, noticeRect.width - 18f * s, 26f * s), discreteNoticeText, nSt);
        }

        // Short, non-blocking Florist delivery reaction banner
        if (floristReactionTimer > 0f && !string.IsNullOrEmpty(floristReactionText))
        {
            float bw = Mathf.Min(480f * s, safe.width - 28f * s);
            float bh = 62f * s;
            float bx = safe.x + (safe.width - bw) * 0.5f;
            float by = safe.y + 10f * s;
            if (bx < topLeftHudRect.xMax + 8f) by = topLeftHudRect.yMax + 8f;
            Rect toast = new Rect(bx, by, bw, bh);
            GUI.Box(toast, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            GUI.DrawTexture(new Rect(toast.x + 10f * s, toast.y + 8f * s, 44f * s, 44f * s), GirlPowerOrla3DUITheme.IconPortraitLuna);
            GUIStyle reactSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(15f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            GUI.Label(new Rect(toast.x + 62f * s, toast.y + 4f * s, bw - 74f * s, bh - 8f * s), floristReactionText, reactSt);
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
                tag = GirlPowerOrla3DLocalization.T("Monte o sorvete", "Make the ice cream", "Prepara el helado");
                task = GirlPowerOrla3DLocalization.T("Limão + manjericão", "Lemon + basil", "Limón + albahaca");
                counter = (cartOpen || craftProgressTimer > 0f) ? "0/1" : "";
                break;
            case StoryPhase.Ch1_Delivering:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconIceCreamLemonBasil;
                tag = GirlPowerOrla3DLocalization.T("Entregue para Luna", "Deliver to Luna", "Entrega a Luna");
                task = GirlPowerOrla3DLocalization.T("Limão + manjericão • Floricultura", "Lemon + basil • Florist", "Limón + albahaca • Floristería");
                counter = "0/1";
                break;
            case StoryPhase.Ch1_ReturningForUpgrade:
            case StoryPhase.Ch1_ChooseUpgrade:
                client = GirlPowerOrla3DUITheme.IconPortraitNina;
                item = GirlPowerOrla3DUITheme.IconBrush;
                tag = GirlPowerOrla3DLocalization.T("Volte ao carrinho", "Return to cart", "Vuelve al carrito");
                task = GirlPowerOrla3DLocalization.T("Escolha sua melhoria", "Choose your upgrade", "Elige tu mejora");
                counter = "1/1";
                break;
            case StoryPhase.Ch2_HarvestBasil:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconBasil;
                tag = GirlPowerOrla3DLocalization.T("Colha manjericão", "Harvest basil", "Cosecha albahaca");
                task = GirlPowerOrla3DLocalization.T("Vaso ao lado do carrinho", "Pot beside the cart", "Maceta junto al carrito");
                counter = "0/1";
                break;
            case StoryPhase.Ch2_CollectCards:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconPostcard;
                tag = GirlPowerOrla3DLocalization.T("Recolha os cartões", "Collect postcards", "Recoge las tarjetas");
                task = GirlPowerOrla3DLocalization.T("Espalhados pela orla", "Scattered along the bay", "Esparcidas por la costa");
                counter = CountCollectedCards() + "/4";
                break;
            case StoryPhase.Ch2_InviteChoice:
                client = GirlPowerOrla3DUITheme.IconPortraitLuna;
                item = GirlPowerOrla3DUITheme.IconPostcard;
                tag = GirlPowerOrla3DLocalization.T("Mostre o convite", "Show invitation", "Muestra la invitación");
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
        // Compact Top-Right corner: Coins counter + Menu Book + Pause -> leaves maximum focus for Mission & Scene!
        Rect safe = GetGuiSafeArea();
        float btnSz = 40f * s;
        float gap = 6f * s;
        float coinPillW = 76f * s;
        float barW = coinPillW + gap + 2 * btnSz + gap + 16f * s;
        float barH = btnSz + 12f * s;
        Rect strip = new Rect(safe.xMax - barW - 12f * s, safe.y + 10f * s, barW, barH);
        GUI.Box(strip, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
        topRightHudRect = new Rect(strip.x - 6f, strip.y - 6f, strip.width + 12f, strip.height + 16f * s);

        float x = strip.x + 8f * s;
        float y = strip.y + 6f * s;

        // 1. Compact Coins Counter
        int coins = lifeActivities != null ? lifeActivities.Coins : 0;
        GUI.DrawTexture(new Rect(x, y + 8f * s, 24f * s, 24f * s), GirlPowerOrla3DUITheme.IconCoin);
        GUIStyle coinSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            fontSize = Mathf.RoundToInt(17f * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        coinSt.normal.textColor = new Color(0.18f, 0.12f, 0.08f, 1f);
        GUI.Label(new Rect(x + 28f * s, y + 2f * s, coinPillW - 28f * s, btnSz - 4f * s), coins.ToString(), coinSt);
        x += coinPillW + gap;

        // 2. Cardápio / Shop Book [TAB]
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconMenuBook, s))
            if (cardapioUI != null) cardapioUI.Toggle();
        x += btnSz + gap;

        // 3. Pause / Options (||)
        if (DrawIconButton(new Rect(x, y, btnSz, btnSz), GirlPowerOrla3DUITheme.IconPause, s))
            if (gameFlow != null) gameFlow.TogglePause();
    }

    private void DrawActiveMissionTargetBadge(float s)
    {
        if (sceneCamera == null || player == null) return;
        Transform target = GetActiveObjectiveTarget();
        if (target == null) return;

        bool inReach = NearXZ(target, interactionRadius);
        // When in reach, the unified InteractionHub displays the active target marker + "Montar sorvete" / "Entregar Sorvete" button!
        if (inReach && GirlPowerOrla3DInteractionHub.Instance != null) return;

        float heightOffset = (target == cart) ? 1.85f : 2.15f;
        Vector3 sp = sceneCamera.WorldToScreenPoint(target.position + Vector3.up * heightOffset);
        Rect safe = GetGuiSafeArea();

        bool clearlyOnScreen = sp.z > 0.3f && sp.z < 30f
            && sp.x > safe.x + 96f * s && sp.x < safe.xMax - 96f * s
            && (Screen.height - sp.y) > safe.y + 92f * s && (Screen.height - sp.y) < safe.yMax - 92f * s;

        string shortDest;
        switch (phase)
        {
            case StoryPhase.Ch1_AtCart:
                shortDest = GirlPowerOrla3DLocalization.T("Carrinho", "Cart", "Carrito");
                break;
            case StoryPhase.Ch1_Delivering:
                shortDest = GirlPowerOrla3DLocalization.T("Luna", "Luna", "Luna");
                break;
            case StoryPhase.Ch2_HarvestBasil:
                shortDest = GirlPowerOrla3DLocalization.T("Manjericão", "Basil", "Albahaca");
                break;
            default:
                shortDest = GirlPowerOrla3DLocalization.T("Destino", "Target", "Destino");
                break;
        }

        // If the target (e.g. Cart or Luna) is OFF-SCREEN or near the edge, draw a discreet edge arrow instead of clamping a label!
        if (!clearlyOnScreen)
        {
            Vector3 toTarget = target.position - player.position;
            float distM = new Vector2(toTarget.x, toTarget.z).magnitude;
            bool onRight = toTarget.x >= 0f;
            string arrow = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.z) * 0.45f
                ? (onRight ? "→" : "←")
                : (toTarget.z >= 0f ? "↑" : "↓");
            string edgeText = onRight
                ? (shortDest + " " + Mathf.RoundToInt(distM) + "m  " + arrow)
                : (arrow + "  " + shortDest + " " + Mathf.RoundToInt(distM) + "m");

            float ew = Mathf.Clamp(62f * s + edgeText.Length * 7.0f * s, 124f * s, 186f * s);
            float eh = 34f * s;
            float ex = onRight ? (safe.xMax - ew - 14f * s) : (safe.x + 14f * s);
            float ey = Mathf.Clamp(safe.y + safe.height * 0.44f, safe.y + 112f * s, safe.yMax - 120f * s);
            Rect edgeRect = new Rect(ex, ey, ew, eh);
            if (GUI.Button(edgeRect, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle))
                GuideAndPerformActiveObjective();
            GUIStyle eSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(14.5f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            eSt.normal.textColor = new Color(0.84f, 0.24f, 0.46f, 1f);
            GUI.Label(edgeRect, edgeText, eSt);
            return;
        }

        // Target is cleanly visible on screen (still approaching): show a compact world anchor tag right over it!
        float bob = Mathf.Sin(Time.time * 3.0f) * 2.6f * s;
        string onScreenLabel = phase == StoryPhase.Ch1_AtCart
            ? GirlPowerOrla3DLocalization.T("🍦 Carrinho", "🍦 Cart", "🍦 Carrito")
            : (phase == StoryPhase.Ch1_Delivering
                ? GirlPowerOrla3DLocalization.T("💐 Florista Luna", "💐 Florist Luna", "💐 Florista Luna")
                : ("★ " + shortDest));
        float bw = Mathf.Clamp(44f * s + onScreenLabel.Length * 7.2f * s, 114f * s, 186f * s);
        float bh = 28f * s;
        Rect r = new Rect(sp.x - bw * 0.5f, (Screen.height - sp.y) - bh + bob, bw, bh);
        if (GUI.Button(r, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle))
            GuideAndPerformActiveObjective();
        GUIStyle lbl = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(13.5f * s) };
        GirlPowerOrla3DUITheme.DrawOutlinedLabel(r, onScreenLabel, lbl, Color.white, new Color(0.08f, 0.08f, 0.12f, 0.92f), 1.1f);
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
            // DEDICATED ICE CREAM PREPARATION SCREEN:
            // - Featured cone visual on left showing live base + topping
            // - Current selection slots with 1-tap remove/fix
            // - Large illustrated ingredient buttons with big readable names
            // - Prominent "Confirmar" button for clear review before finishing!
            float pw = Mathf.Min(780f * s, safe.width - 20f * s);
            float ph = Mathf.Min(420f * s, safe.height - 28f * s);
            float px = safe.x + (safe.width - pw) * 0.5f;
            float py = safe.y + (safe.height - ph) * 0.5f;
            Rect box = new Rect(px, py, pw, ph);
            bottomPanelHudRect = box;
            GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

            // 1. Header Bar: Florist's Order always clearly visible
            Rect orderBar = new Rect(box.x + 14f * s, box.y + 12f * s, pw - 28f * s, 50f * s);
            GUI.Box(orderBar, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
            GUI.DrawTexture(new Rect(orderBar.x + 8f * s, orderBar.y + 5f * s, 40f * s, 40f * s), GirlPowerOrla3DUITheme.IconPortraitLuna);
            string orderText = GirlPowerOrla3DLocalization.T(
                "PREPARAR SORVETE  •  Pedido da Luna: ◆ Limão + ♣ Manjericão",
                "MAKE ICE CREAM  •  Luna's Order: ◆ Lemon + ♣ Basil",
                "PREPARAR HELADO  •  Pedido de Luna: ◆ Limón + ♣ Albahaca");
            GUIStyle orderSt = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle)
            {
                fontSize = Mathf.RoundToInt(17.5f * s),
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            GirlPowerOrla3DUITheme.DrawOutlinedLabel(
                new Rect(orderBar.x + 56f * s, orderBar.y + 2f * s, orderBar.width - 68f * s, orderBar.height - 4f * s),
                orderText, orderSt, Color.white, new Color(0.08f, 0.16f, 0.18f, 0.92f), 1.2f);

            float bodyTop = orderBar.yMax + 12f * s;
            float footerH = 54f * s;
            float bodyH = Mathf.Max(190f * s, box.yMax - footerH - 22f * s - bodyTop);
            float leftW = Mathf.Clamp(pw * 0.34f, 200f * s, 260f * s);
            float rightX = box.x + 14f * s + leftW + 12f * s;
            float rightW = box.xMax - 14f * s - rightX;

            // 2. LEFT COLUMN: Featured Live Cone Preview + 1-Tap Editable Current Selection
            Rect coneCard = new Rect(box.x + 14f * s, bodyTop, leftW, bodyH);
            GUI.Box(coneCard, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            bool hasBase = !string.IsNullOrEmpty(selectedBaseFlavor);
            bool hasTop = !string.IsNullOrEmpty(selectedTopping);
            bool matchesOrder = selectedBaseFlavor == "Limão" && selectedTopping == "Manjericão";

            // Large visual cone with dynamic tint & basil/topping badge
            float coneSz = Mathf.Min(108f * s, bodyH * 0.42f);
            Rect coneImgRect = new Rect(coneCard.x + (leftW - coneSz) * 0.5f, coneCard.y + 10f * s, coneSz, coneSz);
            Color prevC = GUI.color;
            if (!hasBase) GUI.color = new Color(0.86f, 0.82f, 0.78f, 0.55f);
            else if (selectedBaseFlavor == "Morango") GUI.color = new Color(1.0f, 0.68f, 0.80f, 1f);
            else if (selectedBaseFlavor == "Baunilha") GUI.color = new Color(1.0f, 0.96f, 0.84f, 1f);
            else GUI.color = new Color(0.96f, 1.0f, 0.74f, 1f); // Limão
            GUI.DrawTexture(coneImgRect, GirlPowerOrla3DUITheme.IconIceCreamLemonBasil);
            GUI.color = prevC;

            if (hasTop)
            {
                float topSz = 34f * s;
                Rect topBadge = new Rect(coneImgRect.xMax - topSz * 0.75f, coneImgRect.y + 2f * s, topSz, topSz);
                if (selectedTopping == "Manjericão")
                    GUI.DrawTexture(topBadge, GirlPowerOrla3DUITheme.IconBasil);
                else if (selectedTopping == "Hortelã")
                {
                    GUI.color = new Color(0.45f, 0.95f, 0.78f, 1f);
                    GUI.DrawTexture(topBadge, GirlPowerOrla3DUITheme.IconBasil);
                    GUI.color = prevC;
                }
                else
                {
                    GUI.color = new Color(0.96f, 0.36f, 0.62f, 1f);
                    GUI.DrawTexture(topBadge, GirlPowerOrla3DUITheme.CircleCoralTex);
                    GUI.color = prevC;
                }
            }

            // Current Selection Slots (1 tap clears/fixes either ingredient immediately!)
            float slotY = coneImgRect.yMax + 8f * s;
            float slotH = 38f * s;
            GUIStyle slotTitleSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle)
            {
                fontSize = Mathf.RoundToInt(13.5f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(coneCard.x + 6f * s, slotY - 4f * s, leftW - 12f * s, 20f * s),
                GirlPowerOrla3DLocalization.T("Seleção atual (toque p/ corrigir):", "Selected (tap to fix):", "Selección (toca p/ cambiar):"), slotTitleSt);
            slotY += 18f * s;

            Rect baseSlot = new Rect(coneCard.x + 10f * s, slotY, leftW - 20f * s, slotH);
            string baseSlotTxt = hasBase
                ? (GirlPowerOrla3DLocalization.T("1. Sabor: ", "1. Base: ", "1. Base: ") + selectedBaseFlavor + "   ✕")
                : GirlPowerOrla3DLocalization.T("1. Sabor: (vazio)", "1. Base: (empty)", "1. Base: (vacío)");
            if (GUI.Button(baseSlot, baseSlotTxt, hasBase ? GirlPowerOrla3DUITheme.BtnTileStyle : GirlPowerOrla3DUITheme.BtnSecondaryStyle) && hasBase)
                ToggleBaseIngredient(selectedBaseFlavor);

            Rect topSlot = new Rect(coneCard.x + 10f * s, slotY + slotH + 6f * s, leftW - 20f * s, slotH);
            string topSlotTxt = hasTop
                ? (GirlPowerOrla3DLocalization.T("2. Toque: ", "2. Top: ", "2. Toque: ") + selectedTopping + "   ✕")
                : GirlPowerOrla3DLocalization.T("2. Toque: (vazio)", "2. Top: (empty)", "2. Toque: (vacío)");
            if (GUI.Button(topSlot, topSlotTxt, hasTop ? GirlPowerOrla3DUITheme.BtnTileStyle : GirlPowerOrla3DUITheme.BtnSecondaryStyle) && hasTop)
                ToggleToppingIngredient(selectedTopping);

            // 3. RIGHT COLUMN: Large Named Ingredient Tiles (Row 1 = Base Flavor, Row 2 = Special Topping)
            GUIStyle secHeadSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(16.5f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            secHeadSt.normal.textColor = new Color(0.18f, 0.12f, 0.09f, 1f);

            float gap = 8f * s;
            float tileW = (rightW - gap * 2f) / 3f;
            float tileH = Mathf.Clamp((bodyH - 64f * s) * 0.5f, 68f * s, 96f * s);

            // Group 1: Base Flavors
            GUI.Label(new Rect(rightX, bodyTop, rightW, 24f * s),
                GirlPowerOrla3DLocalization.T("1. Escolha o sabor da bola:", "1. Pick scoop flavor:", "1. Elige el sabor:"), secHeadSt);

            var bases = new (string key, string label, Color tint)[]
            {
                ("Limão",    GirlPowerOrla3DLocalization.T("◆ Limão",    "◆ Lemon",      "◆ Limón"),   new Color(0.95f, 0.98f, 0.52f)),
                ("Morango",  GirlPowerOrla3DLocalization.T("● Morango",  "● Strawberry", "● Fresa"),   new Color(1.00f, 0.62f, 0.78f)),
                ("Baunilha", GirlPowerOrla3DLocalization.T("▲ Baunilha", "▲ Vanilla",    "▲ Vainilla"),new Color(1.00f, 0.94f, 0.76f))
            };
            float row1Y = bodyTop + 26f * s;
            DrawIngredientTileRow(bases, selectedBaseFlavor, rightX, row1Y, tileW, tileH, gap, s, GirlPowerOrla3DUITheme.IconIceCreamLemonBasil, ToggleBaseIngredient);

            // Group 2: Toppings
            float row2HeadY = row1Y + tileH + 12f * s;
            GUI.Label(new Rect(rightX, row2HeadY, rightW, 24f * s),
                GirlPowerOrla3DLocalization.T("2. Escolha o toque especial:", "2. Pick special topping:", "2. Elige el toque:"), secHeadSt);

            var toppings = new (string key, string label, Color tint)[]
            {
                ("Manjericão",  GirlPowerOrla3DLocalization.T("♣ Manjericão", "♣ Basil",       "♣ Albahaca"), new Color(0.56f, 0.96f, 0.58f)),
                ("Hortelã",     GirlPowerOrla3DLocalization.T("✿ Hortelã",    "✿ Mint",        "✿ Menta"),    new Color(0.46f, 0.94f, 0.86f)),
                ("Calda Berry", GirlPowerOrla3DLocalization.T("♥ Calda",      "♥ Berry Syrup", "♥ Salsa"),    new Color(0.98f, 0.52f, 0.74f))
            };
            DrawIngredientTileRow(toppings, selectedTopping, rightX, row2HeadY + 26f * s, tileW, tileH, gap, s, GirlPowerOrla3DUITheme.IconBasil, ToggleToppingIngredient);

            // 4. FIXED FOOTER BAR: Close + Large "Confirmar" Review Button
            float btnY = box.yMax - footerH - 12f * s;
            float closeW = Mathf.Min(138f * s, (pw - 36f * s) * 0.26f);
            float confirmW = (pw - 36f * s) - closeW;

            GUIStyle footSecSt = new GUIStyle(GirlPowerOrla3DUITheme.BtnSecondaryStyle) { fontSize = Mathf.RoundToInt(17.5f * s) };
            if (GUI.Button(new Rect(box.x + 14f * s, btnY, closeW, footerH), GirlPowerOrla3DLocalization.T("Voltar", "Back", "Volver"), footSecSt))
                cartOpen = false;

            bool readyForReview = hasBase && hasTop;
            string confirmTxt = matchesOrder
                ? GirlPowerOrla3DLocalization.T("✓  CONFIRMAR SORVETE", "✓  CONFIRM ICE CREAM", "✓  CONFIRMAR HELADO")
                : (readyForReview
                    ? GirlPowerOrla3DLocalization.T("CONFIRMAR", "CONFIRM", "CONFIRMAR")
                    : GirlPowerOrla3DLocalization.T("Escolha sabor e toque...", "Pick base & topping...", "Elige sabor y toque..."));
            GUIStyle footMainSt = new GUIStyle(readyForReview ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle)
            {
                fontSize = Mathf.RoundToInt(19.5f * s)
            };
            if (GUI.Button(new Rect(box.x + 14f * s + closeW + 10f * s, btnY, confirmW, footerH), confirmTxt, footMainSt))
                ConfirmConeAssembly();
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

    private static void DrawIngredientTileRow(
        (string key, string label, Color tint)[] items,
        string activeKey,
        float startX,
        float y,
        float tileW,
        float tileH,
        float gap,
        float s,
        Texture2D iconTex,
        System.Action<string> onTap)
    {
        GUIStyle lblSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            fontSize = Mathf.RoundToInt(16f * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        for (int i = 0; i < items.Length; i++)
        {
            bool isSel = activeKey == items[i].key;
            Rect tr = new Rect(startX + i * (tileW + gap), y, tileW, tileH);
            if (GUI.Button(tr, GUIContent.none, isSel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
                onTap?.Invoke(items[i].key);

            float icSz = Mathf.Min(38f * s, tileH * 0.48f);
            Color prev = GUI.color;
            GUI.color = items[i].tint;
            GUI.DrawTexture(new Rect(tr.x + (tileW - icSz) * 0.5f, tr.y + 6f * s, icSz, icSz), iconTex);
            GUI.color = prev;

            lblSt.normal.textColor = isSel ? Color.white : new Color(0.18f, 0.12f, 0.08f, 1f);
            string txt = (isSel ? "✓ " : "") + items[i].label;
            GUI.Label(new Rect(tr.x + 4f * s, tr.y + icSz + 6f * s, tileW - 8f * s, tileH - icSz - 10f * s), txt, lblSt);
        }
    }

    private void DrawWorldSpaceInteractPrompt(float s)
    {
        if (GirlPowerOrla3DInteractionHub.Instance != null) return;
        if (!TryGetNearbyInteractable(out Transform target, out string actionLabel)) return;
        if (target == null) return;

        Rect safe = GetGuiSafeArea();
        bool isMainMissionAction = (target == GetActiveObjectiveTarget());
        string text = IsTouchPrimary ? actionLabel : (actionLabel + "   [E]");
        float w = Mathf.Clamp(88f * s + text.Length * 8.0f * s, isMainMissionAction ? 215f * s : 184f * s, Mathf.Min(330f * s, safe.width - 36f * s));
        float h = isMainMissionAction ? 54f * s : 46f * s;
        float x = safe.xMax - w - 18f * s;
        float y = safe.yMax - h - 18f * s;

        Rect pill = new Rect(x, y, w, h);
        bottomPanelHudRect = pill;
        // Vivid strawberry pink reserved EXCLUSIVELY for the active main story mission action; calm secondary style for casual NPC chats!
        GUIStyle btnStyle = isMainMissionAction ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnSecondaryStyle;
        if (GUI.Button(pill, text, btnStyle))
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
            if (phase == StoryPhase.Ch1_AtCart) { label = "Montar sorvete"; return true; }
            if (phase == StoryPhase.Ch1_ChooseUpgrade) { label = "Escolher melhoria"; return true; }
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
