using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single unified Interaction Coordinator for Orla das Fadas:
/// - Collects all in-reach interactable candidates from Story, MegaSystems, PetFriendly & LifeActivities.
/// - Picks EXACTLY ONE active target (prioritizing the active story mission when in reach, otherwise closest target with smooth hysteresis).
/// - Draws a compact diegetic marker directly over the active target so the player always knows WHICH object/NPC will respond.
/// - Draws ONE large, legible action button in the bottom-right thumb zone that disappears immediately upon leaving reach.
/// </summary>
[DefaultExecutionOrder(45)]
public sealed class GirlPowerOrla3DInteractionHub : MonoBehaviour
{
    public struct Candidate
    {
        public string id;
        public Transform target;
        public string buttonLabel;
        public string targetBadgeLabel;
        public float distance;
        public float markerHeight;
        public bool isPrimaryMission;
        public Action onExecute;
    }

    public static GirlPowerOrla3DInteractionHub Instance { get; private set; }

    public Camera sceneCamera;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DFirstDelivery storyController;
    public GirlPowerOrla3DDialogue dialogueController;
    public GirlPowerOrla3DPhotoOverlay photoOverlay;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DFairBusinesses fairBusinesses;

    private readonly List<Candidate> frameCandidates = new List<Candidate>(16);
    private Candidate activeCandidate;
    private bool hasActiveCandidate;
    private string stickyId = "";
    private Rect activeButtonGuiRect;

    public bool HasActiveCandidate => hasActiveCandidate;
    public Transform ActiveTarget => hasActiveCandidate ? activeCandidate.target : null;
    public Rect ActiveButtonGuiRect => activeButtonGuiRect;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (storyController == null) storyController = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
        if (dialogueController == null) dialogueController = FindObjectOfType<GirlPowerOrla3DDialogue>();
        if (photoOverlay == null) photoOverlay = FindObjectOfType<GirlPowerOrla3DPhotoOverlay>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (fairBusinesses == null) fairBusinesses = FindObjectOfType<GirlPowerOrla3DFairBusinesses>();
    }

    public void SubmitCandidate(Candidate candidate)
    {
        if (candidate.target == null || candidate.onExecute == null || string.IsNullOrEmpty(candidate.buttonLabel))
            return;
        frameCandidates.Add(candidate);
    }

    public bool TryExecuteActiveCandidate()
    {
        if (!hasActiveCandidate || activeCandidate.onExecute == null) return false;
        activeCandidate.onExecute.Invoke();
        return true;
    }

    private void LateUpdate()
    {
        ResolveBestCandidate();
        frameCandidates.Clear();
    }

    private void ResolveBestCandidate()
    {
        if (frameCandidates.Count == 0)
        {
            hasActiveCandidate = false;
            stickyId = "";
            activeButtonGuiRect = Rect.zero;
            return;
        }

        int bestIdx = -1;
        float bestScore = float.MaxValue;

        for (int i = 0; i < frameCandidates.Count; i++)
        {
            Candidate c = frameCandidates[i];
            // Active story mission always wins when inside its interaction reach;
            // otherwise prefer the closest target with slight 0.45m hysteresis so adjacent targets never flicker.
            float score = c.distance;
            if (c.isPrimaryMission) score -= 25f;
            if (!string.IsNullOrEmpty(stickyId) && c.id == stickyId) score -= 0.45f;

            if (score < bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0)
        {
            activeCandidate = frameCandidates[bestIdx];
            hasActiveCandidate = true;
            stickyId = activeCandidate.id;
        }
        else
        {
            hasActiveCandidate = false;
            stickyId = "";
            activeButtonGuiRect = Rect.zero;
        }
    }

    private bool ShouldHidePromptUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return true;
        if (dialogueController != null && dialogueController.IsOpen) return true;
        if (photoOverlay != null && photoOverlay.IsShowing) return true;
        if (lifeActivities != null && lifeActivities.IsShopMenuOpen) return true;
        if (megaSystems != null && megaSystems.IsAnyModalOpen) return true;
        if (fairBusinesses != null && fairBusinesses.IsModalOpen) return true;
        if (storyController != null && (storyController.HasChoiceMenuOpen || storyController.IsCraftingNow)) return true;
        return false;
    }

    private void OnGUI()
    {
        activeButtonGuiRect = Rect.zero;
        if (!hasActiveCandidate || activeCandidate.target == null) return;
        if (ShouldHidePromptUI()) return;

        GirlPowerOrla3DUITheme.EnsureInitialized();
        float s = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 810f), 0.68f, 1.15f);
        bool touchPrimary = storyController != null && storyController.IsTouchPrimary;

        // 1. Contextual command anchored DIRECTLY NEAR the character/object in reach!
        //    - Desktop/Keyboard: "E — Conversar com Luna" right next to the target (never stranded far away in a corner!)
        //    - Mobile/Touch: clean target tag over character + matching ergonomic thumb touch button below!
        DrawActiveTargetPrompt(s, touchPrimary);

        if (touchPrimary)
        {
            Rect sa = Screen.safeArea;
            float safeTop = Screen.height - (sa.y + sa.height);
            Rect safe = new Rect(sa.x, safeTop, sa.width, sa.height);

            string btnText = activeCandidate.buttonLabel;
            float w = Mathf.Clamp(
                84f * s + btnText.Length * 8.4f * s,
                activeCandidate.isPrimaryMission ? 216f * s : 192f * s,
                Mathf.Min(336f * s, safe.width - 32f * s));
            float h = activeCandidate.isPrimaryMission ? 56f * s : 50f * s;
            Rect pill = new Rect(safe.xMax - w - 18f * s, safe.yMax - h - 18f * s, w, h);
            activeButtonGuiRect = pill;

            GUIStyle style = activeCandidate.isPrimaryMission
                ? GirlPowerOrla3DUITheme.BtnPrimaryStyle
                : GirlPowerOrla3DUITheme.BtnSecondaryStyle;
            GUIStyle sized = new GUIStyle(style)
            {
                fontSize = Mathf.RoundToInt((activeCandidate.isPrimaryMission ? 18.5f : 17f) * s)
            };
            if (GUI.Button(pill, btnText, sized))
                activeCandidate.onExecute?.Invoke();
        }
    }

    private void DrawActiveTargetPrompt(float s, bool touchPrimary)
    {
        if (sceneCamera == null || activeCandidate.target == null) return;
        float hOff = activeCandidate.markerHeight > 0.1f ? activeCandidate.markerHeight : 1.95f;
        Vector3 sp = sceneCamera.WorldToScreenPoint(activeCandidate.target.position + Vector3.up * hOff);
        if (sp.z <= 0.25f || sp.z > 28f) return;

        string promptText = touchPrimary
            ? ("● " + activeCandidate.buttonLabel)
            : ("E  —  " + activeCandidate.buttonLabel);

        float bob = Mathf.Sin(Time.time * 3.8f) * 2.5f * s;
        GUIStyle lbl = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle)
        {
            fontSize = Mathf.RoundToInt((touchPrimary ? 14f : 15.5f) * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        float measuredW = lbl.CalcSize(new GUIContent(promptText)).x + 30f * s;
        float mw = Mathf.Clamp(measuredW, 136f * s, 290f * s);
        float mh = (touchPrimary ? 30f : 38f) * s;
        float mx = Mathf.Clamp(sp.x - mw * 0.5f, 14f * s, Screen.width - mw - 14f * s);
        float my = Mathf.Clamp((Screen.height - sp.y) - mh - 12f * s + bob, 92f * s, Screen.height - 96f * s);

        Rect badgeRect = new Rect(mx, my, mw, mh);
        if (!touchPrimary) activeButtonGuiRect = badgeRect;

        GUIStyle pillStyle = activeCandidate.isPrimaryMission
            ? GirlPowerOrla3DUITheme.PillCoralStyle
            : GirlPowerOrla3DUITheme.PillTealStyle;
        if (GUI.Button(badgeRect, GUIContent.none, pillStyle))
            activeCandidate.onExecute?.Invoke();

        GirlPowerOrla3DUITheme.DrawOutlinedLabel(badgeRect, promptText, lbl, Color.white, new Color(0.10f, 0.06f, 0.12f, 0.92f), 1.15f);

        // Small downward anchor dot pointing straight to the character/object below
        float dotSz = 9f * s;
        Rect dotRect = new Rect(Mathf.Clamp(sp.x - dotSz * 0.5f, badgeRect.x + 12f * s, badgeRect.xMax - 12f * s - dotSz), badgeRect.yMax + 2f * s, dotSz, dotSz);
        GUI.DrawTexture(dotRect, activeCandidate.isPrimaryMission ? GirlPowerOrla3DUITheme.CircleCoralTex : GirlPowerOrla3DUITheme.CircleWhiteTex);
    }
}
