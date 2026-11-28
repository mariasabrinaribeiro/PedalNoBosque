using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Exclusive 3-Layer Mobile-First Touch & Mouse Drag Controller for Baía do Avesso:
/// - Layer 3 (Highest Priority): Direct Drag & Drop on interactive +40% hitboxes:
///     * Drag from a Fairy Waitress to a waiting Table or Empty Water Bowl -> glowing guide arc dispatches Fairy!
///     * Drag from Ice Cream Cart / Kitchen directly to a waiting Table -> instant service!
///     * Tap an Empty Water Bowl / Table / Stall / NPC -> protagonist walks there and interacts cleanly!
/// - Layer 1 (Drag on Empty Ground/Sea/Street): Smooth Camera Pan with inertia & clamped scenic bounds + 2-finger Pinch Zoom!
/// - Layer 2 (Single Tap on Open Ground): Tap-to-Move Protagonist with expanding circular ground ring indicator!
/// Never mixes camera panning with object dragging or movement tapping.
/// </summary>
public sealed class GirlPowerOrla3DMobileTouchController : MonoBehaviour
{
    private enum GestureState
    {
        Idle = 0,
        PendingGround = 1,
        PanningCamera = 2,
        DraggingInteractive = 3,
        PinchingZoom = 4
    }

    [Header("References")]
    public Camera sceneCamera;
    public GirlPowerOrla3DFirstDelivery storyController;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DDialogue dialogueController;
    public GirlPowerOrla3DPhotoOverlay photoOverlay;
    public GirlPowerOrla3DCardapioUI cardapioUI;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DFairBusinesses fairBusinesses;
    public GirlPowerOrla3DPetFriendlySystem petFriendlySystem;
    public GirlPowerOrla3DNpcAI npcAI;

    [Header("Mobile Tuning")]
    public float dragThresholdPx = 15f;
    public float maxTapDurationSec = 0.42f;
    public float panSensitivity = 0.024f;
    public float pinchZoomSensitivity = 0.0028f;

    private GestureState state = GestureState.Idle;
    private Vector2 pointerDownScreen;
    private Vector2 pointerPrevScreen;
    private Vector2 pointerCurrScreen;
    private float pointerDownTime;
    private Vector2 panInertiaVelXZ;
    private float prevPinchDist;

    private GirlPowerOrla3DTouchTarget draggedTarget;
    private GirlPowerOrla3DTouchTarget hoveredDropTarget;
    private Vector3 dragWorldEnd;

    // Expanding circular ground tap indicator visual
    private Transform tapRing;
    private Renderer tapRingRenderer;
    private Material tapRingMat;
    private float tapRingTimer;
    private const float TapRingDuration = 0.68f;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (storyController == null) storyController = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (dialogueController == null) dialogueController = FindObjectOfType<GirlPowerOrla3DDialogue>();
        if (photoOverlay == null) photoOverlay = FindObjectOfType<GirlPowerOrla3DPhotoOverlay>();
        if (cardapioUI == null) cardapioUI = FindObjectOfType<GirlPowerOrla3DCardapioUI>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (fairBusinesses == null) fairBusinesses = FindObjectOfType<GirlPowerOrla3DFairBusinesses>();
        if (petFriendlySystem == null) petFriendlySystem = FindObjectOfType<GirlPowerOrla3DPetFriendlySystem>();
        if (npcAI == null) npcAI = FindObjectOfType<GirlPowerOrla3DNpcAI>();

        CreateTapRingIndicator();
    }

    private void CreateTapRingIndicator()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Indicador_Toque_Chao";
        Collider c = ring.GetComponent<Collider>();
        if (c != null) Destroy(c);
        ring.transform.localScale = new Vector3(0.9f, 0.018f, 0.9f);

        Shader sh = Shader.Find("GirlPower/OrlaACNHCozy");
        if (sh == null) sh = Shader.Find("Standard");
        tapRingMat = new Material(sh);
        // Dominant Coastal Turquoise (#2AC6CC) with soft bright sheen
        tapRingMat.SetColor("_Color", new Color(0.165f, 0.776f, 0.80f, 1f));
        if (tapRingMat.HasProperty("_EmissionColor"))
            tapRingMat.SetColor("_EmissionColor", new Color(0.24f, 0.88f, 0.90f, 1f) * 0.72f);
        if (tapRingMat.HasProperty("_CurveStrength"))
            tapRingMat.SetFloat("_CurveStrength", 0.0008f);

        tapRingRenderer = ring.GetComponent<Renderer>();
        tapRingRenderer.sharedMaterial = tapRingMat;
        tapRingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tapRingRenderer.receiveShadows = false;
        ring.SetActive(false);
        tapRing = ring.transform;
    }

    private void SpawnTapRing(Vector3 worldPos)
    {
        if (tapRing == null) return;
        tapRing.position = new Vector3(worldPos.x, worldPos.y + 0.05f, worldPos.z);
        tapRing.localScale = new Vector3(0.32f, 0.02f, 0.32f);
        tapRing.gameObject.SetActive(true);
        tapRingTimer = TapRingDuration;
    }

    private void UpdateTapRing(float dt)
    {
        if (tapRing == null || tapRingTimer <= 0f) return;
        tapRingTimer -= dt;
        if (tapRingTimer <= 0f)
        {
            tapRing.gameObject.SetActive(false);
            return;
        }
        float p = 1f - (tapRingTimer / TapRingDuration);
        float ease = 1f - Mathf.Pow(1f - p, 2.5f);
        float rad = Mathf.Lerp(0.32f, 1.38f, ease);
        float yScale = Mathf.Lerp(0.026f, 0.004f, p);
        tapRing.localScale = new Vector3(rad, yScale, rad);
    }

    private bool IsGameplayInputBlocked()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return true;
        if (dialogueController != null && dialogueController.IsOpen) return true;
        if (photoOverlay != null && photoOverlay.IsShowing) return true;
        if (cardapioUI != null && cardapioUI.IsOpen) return true;
        if (megaSystems != null && megaSystems.IsAnyModalOpen) return true;
        if (lifeActivities != null && lifeActivities.IsShopMenuOpen) return true;
        if (fairBusinesses != null && fairBusinesses.IsModalOpen) return true;
        if (storyController != null && storyController.HasChoiceMenuOpen) return true;
        return false;
    }

    private bool IsOverCompactHudCorner(Vector2 screenPos)
    {
        if (GUIUtility.hotControl != 0) return true;
        // Convert Unity screen coords (bottom-left origin) to GUI coords (top-left origin)
        Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        if (storyController != null && storyController.IsUiPointBlocked(guiPos))
            return true;

        // Protect the unified bottom-right thumb action button so tapping Interact NEVER leaks into tap-to-move!
        if (GirlPowerOrla3DInteractionHub.Instance != null)
        {
            Rect hubRect = GirlPowerOrla3DInteractionHub.Instance.ActiveButtonGuiRect;
            if (hubRect.width > 1f && hubRect.height > 1f)
            {
                Rect paddedHub = new Rect(hubRect.x - 10f, hubRect.y - 10f, hubRect.width + 20f, hubRect.height + 20f);
                if (paddedHub.Contains(guiPos))
                    return true;
            }
        }

        // Safe fallback guard around top-left (mission + 3-step stepper) and top-right compact HUD corners
        float s = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 810f), 0.68f, 1.15f);
        Rect topLeftGuard = new Rect(0f, 0f, 348f * s, 142f * s);
        Rect topRightGuard = new Rect(Screen.width - 360f * s, 0f, 360f * s, 152f * s);
        if (topLeftGuard.Contains(guiPos) || topRightGuard.Contains(guiPos))
            return true;

        // Protect contextual bottom-right action pill when player is next to an interactable
        if (storyController != null && storyController.HasNearbyStoryInteractable)
        {
            Rect actionPillGuard = new Rect(Screen.width - 368f * s, Screen.height - 96f * s, 368f * s, 96f * s);
            if (actionPillGuard.Contains(guiPos))
                return true;
        }
        return false;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        UpdateTapRing(dt);

        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera == null || storyController == null) return;

        if (IsGameplayInputBlocked())
        {
            CancelActiveGesture();
            return;
        }

        // Apply smooth camera pan inertia when no finger is actively dragging
        if (state != GestureState.PanningCamera && panInertiaVelXZ.sqrMagnitude > 0.0004f)
        {
            storyController.AddCameraPanOffset(panInertiaVelXZ * dt);
            panInertiaVelXZ = Vector2.Lerp(panInertiaVelXZ, Vector2.zero, 1f - Mathf.Exp(-6.2f * dt));
        }

        // 1. MULTI-TOUCH (2+ fingers -> Layer 1 Safe Pinch Zoom, exclusive!)
        var touches = Touch.activeTouches;
        if (touches.Count >= 2)
        {
            Vector2 p0 = touches[0].screenPosition;
            Vector2 p1 = touches[1].screenPosition;
            float dist = Vector2.Distance(p0, p1);
            if (state != GestureState.PinchingZoom)
            {
                state = GestureState.PinchingZoom;
                draggedTarget = null;
                hoveredDropTarget = null;
                prevPinchDist = dist;
                panInertiaVelXZ = Vector2.zero;
            }
            else
            {
                float delta = dist - prevPinchDist;
                prevPinchDist = dist;
                if (Mathf.Abs(delta) > 0.5f)
                    storyController.AdjustCameraZoom(-delta * pinchZoomSensitivity);
            }
            return;
        }
        else if (state == GestureState.PinchingZoom)
        {
            state = GestureState.Idle;
            return;
        }

        // 2. SINGLE POINTER (Touch #0 OR Left Mouse Button for seamless Editor/WebGL/Mobile parity!)
        bool began = false, moved = false, ended = false;
        Vector2 screenPos = Vector2.zero;

        if (touches.Count == 1)
        {
            Touch t0 = touches[0];
            screenPos = t0.screenPosition;
            var phase = t0.phase;
            began = phase == UnityEngine.InputSystem.TouchPhase.Began;
            moved = phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary;
            ended = phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
        }
        else
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                began = mouse.leftButton.wasPressedThisFrame;
                moved = mouse.leftButton.isPressed && !began;
                ended = mouse.leftButton.wasReleasedThisFrame;
            }
        }

        if (began)
        {
            if (IsOverCompactHudCorner(screenPos))
                return;

            pointerDownScreen = screenPos;
            pointerPrevScreen = screenPos;
            pointerCurrScreen = screenPos;
            pointerDownTime = Time.unscaledTime;
            panInertiaVelXZ = Vector2.zero;

            // Priority Layer 3: Check oversized (+40%) interactive touch targets first!
            GirlPowerOrla3DTouchTarget hitTarget = RaycastTouchTarget(screenPos);
            if (hitTarget != null)
            {
                draggedTarget = hitTarget;
                hoveredDropTarget = null;
                dragWorldEnd = hitTarget.Anchor.position;
                state = GestureState.DraggingInteractive;
                GirlPowerOrla3DJuice.PopScale(hitTarget.Anchor, 1.12f, 0.22f);
            }
            else
            {
                // Layer 1 or Layer 2 candidate (disambiguated cleanly by drag distance!)
                draggedTarget = null;
                state = GestureState.PendingGround;
            }
        }
        else if (moved && state != GestureState.Idle)
        {
            Vector2 frameDelta = screenPos - pointerPrevScreen;
            pointerPrevScreen = screenPos;
            pointerCurrScreen = screenPos;

            if (state == GestureState.PendingGround)
            {
                if (Vector2.Distance(screenPos, pointerDownScreen) >= dragThresholdPx)
                    state = GestureState.PanningCamera;
            }

            if (state == GestureState.PanningCamera)
            {
                // Layer 1: Smooth Camera Pan across Beach / Promenade / Street!
                float zoomScale = storyController.CurrentZoomFactor;
                Vector2 worldDelta = new Vector2(-frameDelta.x, -frameDelta.y) * (panSensitivity * zoomScale);
                storyController.AddCameraPanOffset(worldDelta);
                if (dt > 0.0001f)
                    panInertiaVelXZ = Vector2.Lerp(panInertiaVelXZ, worldDelta / dt, 0.45f);
            }
            else if (state == GestureState.DraggingInteractive && draggedTarget != null)
            {
                // Layer 3: Update guide line endpoint & highlight valid drop target!
                if (TryRaycastGround(screenPos, out Vector3 groundPt))
                    dragWorldEnd = groundPt;
                hoveredDropTarget = FindBestDropTarget(screenPos, dragWorldEnd, draggedTarget);
            }
        }
        else if (ended && state != GestureState.Idle)
        {
            pointerCurrScreen = screenPos;
            float dragDist = Vector2.Distance(screenPos, pointerDownScreen);
            float heldSec = Time.unscaledTime - pointerDownTime;

            if (state == GestureState.PendingGround)
            {
                // Layer 2: Clean Single Tap on Open Ground -> Tap-to-Move Protagonist + Expanding Ground Ring!
                if (dragDist < dragThresholdPx && heldSec <= maxTapDurationSec && TryRaycastGround(screenPos, out Vector3 tapWorld))
                {
                    tapWorld.x = Mathf.Clamp(tapWorld.x, storyController.minWorldX + 0.3f, storyController.maxWorldX - 0.3f);
                    tapWorld.z = Mathf.Clamp(tapWorld.z, storyController.minWorldZ + 0.3f, storyController.maxWorldZ - 0.3f);
                    storyController.SetTapDestination(tapWorld, null);
                    SpawnTapRing(tapWorld);
                }
            }
            else if (state == GestureState.DraggingInteractive && draggedTarget != null)
            {
                ResolveInteractiveRelease(draggedTarget, hoveredDropTarget, dragDist);
            }

            state = GestureState.Idle;
            draggedTarget = null;
            hoveredDropTarget = null;
        }
    }

    private void CancelActiveGesture()
    {
        state = GestureState.Idle;
        draggedTarget = null;
        hoveredDropTarget = null;
        panInertiaVelXZ = Vector2.zero;
    }

    private void ResolveInteractiveRelease(GirlPowerOrla3DTouchTarget source, GirlPowerOrla3DTouchTarget dropTarget, float dragDistPx)
    {
        if (source == null) return;

        // A. Direct Drag & Drop from Fairy Waitress -> Table or Water Bowl!
        if (source.role == GirlPowerOrla3DTouchTarget.TargetRole.FairyWaitress && dragDistPx >= dragThresholdPx)
        {
            if (dropTarget != null && npcAI != null)
            {
                if (dropTarget.role == GirlPowerOrla3DTouchTarget.TargetRole.WaterBowl && petFriendlySystem != null)
                {
                    int bIdx = dropTarget.index;
                    Vector3 dest = dropTarget.Anchor.position;
                    npcAI.DispatchFairyTo(source.Anchor, dest, () => petFriendlySystem.RefillBowl(bIdx, true));
                    SpawnTapRing(dest);
                    return;
                }
                if (dropTarget.role == GirlPowerOrla3DTouchTarget.TargetRole.PetTable && petFriendlySystem != null)
                {
                    int tIdx = dropTarget.index;
                    Vector3 dest = dropTarget.Anchor.position;
                    npcAI.DispatchFairyTo(source.Anchor, dest, () => petFriendlySystem.TryServeTableByIndex(tIdx, true));
                    SpawnTapRing(dest);
                    return;
                }
            }
            // Dropped anywhere else -> Fairy glides smoothly to that spot!
            if (npcAI != null)
            {
                npcAI.DispatchFairyTo(source.Anchor, dragWorldEnd, null);
                SpawnTapRing(dragWorldEnd);
            }
            return;
        }

        // B. Direct Drag & Drop from Ice Cream Cart / Kitchen -> Waiting Table!
        if (source.role == GirlPowerOrla3DTouchTarget.TargetRole.CartOrKitchen && dragDistPx >= dragThresholdPx && dropTarget != null)
        {
            if (dropTarget.role == GirlPowerOrla3DTouchTarget.TargetRole.PetTable && petFriendlySystem != null)
            {
                if (petFriendlySystem.TryServeTableByIndex(dropTarget.index, false))
                {
                    SpawnTapRing(dropTarget.Anchor.position);
                    return;
                }
            }
        }

        // C. Quick Tap on Interactive Object -> Walk Protagonist cleanly to the walkable FRONT service spot & auto-interact!
        Vector3 destPos = storyController != null
            ? storyController.GetWalkableServicePoint(source.Anchor)
            : source.Anchor.position;
        SpawnTapRing(destPos);

        if (source.role == GirlPowerOrla3DTouchTarget.TargetRole.WaterBowl && petFriendlySystem != null)
        {
            int bIdx = source.index;
            if (storyController.player != null && Vector2.Distance(
                new Vector2(storyController.player.position.x, storyController.player.position.z),
                new Vector2(destPos.x, destPos.z)) <= 3.3f)
            {
                petFriendlySystem.RefillBowl(bIdx, false);
            }
            else
            {
                storyController.SetTapDestination(destPos, () => petFriendlySystem.RefillBowl(bIdx, false));
            }
            return;
        }

        if (source.role == GirlPowerOrla3DTouchTarget.TargetRole.PetTable && petFriendlySystem != null)
        {
            int tIdx = source.index;
            if (storyController.player != null && Vector2.Distance(
                new Vector2(storyController.player.position.x, storyController.player.position.z),
                new Vector2(destPos.x, destPos.z)) <= 3.6f)
            {
                petFriendlySystem.TryServeTableByIndex(tIdx, false);
            }
            else
            {
                storyController.SetTapDestination(destPos, () => petFriendlySystem.TryServeTableByIndex(tIdx, false));
            }
            return;
        }

        if (source.role == GirlPowerOrla3DTouchTarget.TargetRole.ShopStall && lifeActivities != null)
        {
            int sIdx = source.index;
            storyController.SetTapDestination(destPos + new Vector3(0f, 0f, -1.6f), () => lifeActivities.OpenShopAsCardapio(sIdx));
            return;
        }

        // Default (Cart, Kitchen, Story NPC, Orchard): Walk close and trigger primary Interact()!
        storyController.SetTapDestination(destPos, () => storyController.Interact());
    }

    private GirlPowerOrla3DTouchTarget RaycastTouchTarget(Vector2 screenPos)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 90f, ~0, QueryTriggerInteraction.Collide);
        GirlPowerOrla3DTouchTarget best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            GirlPowerOrla3DTouchTarget t = hits[i].collider.GetComponent<GirlPowerOrla3DTouchTarget>();
            if (t == null) t = hits[i].collider.GetComponentInParent<GirlPowerOrla3DTouchTarget>();
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            // Prefer Fairies and Empty Water Bowls slightly if overlapping
            float bias = (t.role == GirlPowerOrla3DTouchTarget.TargetRole.FairyWaitress) ? -1.5f : 0f;
            float score = hits[i].distance + bias;
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }
        return best;
    }

    private GirlPowerOrla3DTouchTarget FindBestDropTarget(Vector2 screenPos, Vector3 groundPt, GirlPowerOrla3DTouchTarget source)
    {
        // First check direct raycast hit
        GirlPowerOrla3DTouchTarget direct = RaycastTouchTarget(screenPos);
        if (direct != null && direct != source && IsValidDropPair(source, direct))
            return direct;

        // Generous magnetic snap (2.4m XZ) so mobile fingertips lock effortlessly onto bowls or tables!
        GirlPowerOrla3DTouchTarget[] all = FindObjectsOfType<GirlPowerOrla3DTouchTarget>();
        GirlPowerOrla3DTouchTarget best = null;
        float bestDist = 2.45f;
        for (int i = 0; i < all.Length; i++)
        {
            GirlPowerOrla3DTouchTarget cand = all[i];
            if (cand == null || cand == source || !cand.gameObject.activeInHierarchy) continue;
            if (!IsValidDropPair(source, cand)) continue;
            float d = Vector2.Distance(
                new Vector2(groundPt.x, groundPt.z),
                new Vector2(cand.Anchor.position.x, cand.Anchor.position.z));
            if (d < bestDist)
            {
                bestDist = d;
                best = cand;
            }
        }
        return best;
    }

    private static bool IsValidDropPair(GirlPowerOrla3DTouchTarget src, GirlPowerOrla3DTouchTarget dst)
    {
        if (src.role == GirlPowerOrla3DTouchTarget.TargetRole.FairyWaitress)
            return dst.role == GirlPowerOrla3DTouchTarget.TargetRole.PetTable
                || dst.role == GirlPowerOrla3DTouchTarget.TargetRole.WaterBowl;
        if (src.role == GirlPowerOrla3DTouchTarget.TargetRole.CartOrKitchen)
            return dst.role == GirlPowerOrla3DTouchTarget.TargetRole.PetTable;
        return false;
    }

    private bool TryRaycastGround(Vector2 screenPos, out Vector3 worldPoint)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter) && enter > 0.1f && enter < 140f)
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }
        worldPoint = Vector3.zero;
        return false;
    }

    // ------------------------------------------------------------------
    // DIEGETIC GUIDE ARC (Drawn cleanly when dragging Fairy or Serving!)
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (state != GestureState.DraggingInteractive || draggedTarget == null || sceneCamera == null) return;
        if (Vector2.Distance(pointerCurrScreen, pointerDownScreen) < dragThresholdPx) return;

        GirlPowerOrla3DUITheme.EnsureInitialized();

        Vector3 startW = draggedTarget.Anchor.position + Vector3.up * 0.95f;
        Vector3 endW = hoveredDropTarget != null ? (hoveredDropTarget.Anchor.position + Vector3.up * 0.85f) : (dragWorldEnd + Vector3.up * 0.25f);
        Vector3 sp0 = sceneCamera.WorldToScreenPoint(startW);
        Vector3 sp1 = sceneCamera.WorldToScreenPoint(endW);
        if (sp0.z <= 0.1f || sp1.z <= 0.1f) return;

        Vector2 g0 = new Vector2(sp0.x, Screen.height - sp0.y);
        Vector2 g1 = new Vector2(sp1.x, Screen.height - sp1.y);
        Vector2 mid = (g0 + g1) * 0.5f + new Vector2(0f, -Mathf.Clamp(Vector2.Distance(g0, g1) * 0.22f, 24f, 95f));

        bool isFairy = draggedTarget.role == GirlPowerOrla3DTouchTarget.TargetRole.FairyWaitress;
        // 3-Color Cohesive Palette: Fairy Pink-Lilac (#EC6FA4) vs. Dominant Coastal Turquoise (#2AC6CC)
        Color lineCol = isFairy
            ? new Color(0.925f, 0.435f, 0.643f, 0.95f)
            : new Color(0.165f, 0.776f, 0.80f, 0.95f);

        Color prev = GUI.color;
        int dots = 14;
        float animShift = Mathf.Repeat(Time.time * 1.4f, 1f / dots);
        for (int i = 0; i <= dots; i++)
        {
            float t = Mathf.Clamp01((float)i / dots + animShift);
            // Quadratic Bezier arc from source to finger/drop target!
            Vector2 pt = (1f - t) * (1f - t) * g0 + 2f * (1f - t) * t * mid + t * t * g1;
            float sz = Mathf.Lerp(10f, 15f, t);
            GUI.color = new Color(lineCol.r, lineCol.g, lineCol.b, Mathf.Lerp(0.55f, 1f, t));
            GUI.DrawTexture(new Rect(pt.x - sz * 0.5f, pt.y - sz * 0.5f, sz, sz), GirlPowerOrla3DUITheme.CircleWhiteTex);
        }

        // Pulsing lock-on halo over valid drop target (Table or Empty Water Bowl)!
        float haloSz = hoveredDropTarget != null ? (54f + Mathf.Sin(Time.time * 9f) * 7f) : 32f;
        GUI.color = hoveredDropTarget != null ? new Color(1f, 0.92f, 0.48f, 0.92f) : lineCol;
        GUI.DrawTexture(new Rect(g1.x - haloSz * 0.5f, g1.y - haloSz * 0.5f, haloSz, haloSz), GirlPowerOrla3DUITheme.CircleWhiteTex);
        GUI.color = prev;
    }
}
