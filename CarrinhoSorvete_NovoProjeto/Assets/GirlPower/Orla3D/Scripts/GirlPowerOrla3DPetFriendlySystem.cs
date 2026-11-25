using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the harmonized Modular Archetypes, Pet-Friendly Restaurant, Dual Human+Pet Order Bubbles,
/// Interactive Street/Restaurant Pet Water Bowls (Reputation System), and Camponesa Orchard Harvester!
/// </summary>
public sealed class GirlPowerOrla3DPetFriendlySystem : MonoBehaviour
{
    [Serializable]
    public struct ArchetypeActorEntry
    {
        public Transform actor;
        public Transform petCompanion;
        public GirlPowerOrla3DArchetypeConfig.ArchetypeKind kind;
        public GirlPowerOrla3DArchetypeConfig.PetKind petKind;
    }

    [Serializable]
    public struct WaterBowlEntry
    {
        public string label;
        public Transform bowlRoot;
        public Transform liquidMesh;
    }

    [Header("References")]
    public Transform player;
    public Camera sceneCamera;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DFirstDelivery storyController;

    [Header("Logical Zones & Staff")]
    public Transform camponesaHarvester;
    public Transform[] orchardTreePoints;
    public Transform kitchenDropoffPoint;
    public Transform[] fairyHelpers;

    [Header("Modular Archetype Guests & Pet-Friendly Tables")]
    public ArchetypeActorEntry[] archetypeGuests;
    public Transform[] petFriendlyTables;

    [Header("Pet Water Bowls (Street & Restaurant Entrances)")]
    public WaterBowlEntry[] waterBowls;

    public int PetReputation { get; private set; } = 100;
    public int OrchardHarvestedTotal { get; private set; }
    public int PetOrdersServedTotal { get; private set; }

    private sealed class BowlRuntime
    {
        public WaterBowlEntry cfg;
        public float level = 1.0f; // 1 = Cheio, 0.5 = Metade, 0 = Vazio!
        public Renderer liqRenderer;
        public float emptyAlertCooldown;
        public float fairyRefillTimer;
    }

    private sealed class GuestRuntime
    {
        public ArchetypeActorEntry cfg;
        public GirlPowerOrla3DArchetypeConfig.ProfileData profile;
        public Vector3 homePos;
        public int assignedTable = -1;
        public bool waitingForOrder;
        public bool eating;
        public float stateTimer;
        public string humanOrder = "";
        public string petOrder = "";
        public Transform calopWingL;
        public Transform calopWingR;
        public Transform petHead;
        public Transform petTail;
        public Quaternion petHeadBase;
        public Quaternion petTailBase;
        public float walkPhase;
        public Vector3 patrolTarget;
        public float faceSign;
        public Vector3 prevWorldPos;
        public Quaternion lastValidRot;
    }

    private readonly List<BowlRuntime> bowls = new List<BowlRuntime>();
    private readonly List<GuestRuntime> guests = new List<GuestRuntime>();
    private readonly Dictionary<string, Texture2D> orderIconCache = new Dictionary<string, Texture2D>();
    private MaterialPropertyBlock bowlMpb;

    // Camponesa harvest cycle state
    private int orchardIdx;
    private bool camponesaCarryingToKitchen;
    private float camponesaWait;
    private float camponesaFaceSign = 1f;
    private Vector3 camponesaPrevPos;
    private Quaternion camponesaLastRot = Quaternion.identity;
    private static readonly string[] OrchardCrops = { "Laranja", "Limão", "Manjericão", "Maçã" };

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (audioController == null) audioController = FindObjectOfType<GirlPowerOrla3DAudio>();
        if (storyController == null) storyController = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();

        bowlMpb = new MaterialPropertyBlock();

        if (waterBowls != null)
        {
            for (int i = 0; i < waterBowls.Length; i++)
            {
                WaterBowlEntry w = waterBowls[i];
                if (w.bowlRoot == null) continue;
                Transform liq = w.liquidMesh != null ? w.liquidMesh : FindChildRec(w.bowlRoot, "WaterBowl_Liquid");
                bowls.Add(new BowlRuntime
                {
                    cfg = w,
                    level = (i == 1) ? 0.45f : 0.92f, // Start with one bowl already half-full so mechanic is immediately visible!
                    liqRenderer = liq != null ? liq.GetComponent<Renderer>() : null,
                    emptyAlertCooldown = 0f,
                    fairyRefillTimer = 18f + i * 7f
                });
            }
        }

        if (camponesaHarvester != null)
        {
            GirlPowerOrla3DCharacterFacing.DisableRootMotion(camponesaHarvester);
            camponesaFaceSign = GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign(camponesaHarvester, 1f);
            camponesaPrevPos = camponesaHarvester.position;
            camponesaLastRot = camponesaHarvester.rotation;
        }

        if (archetypeGuests != null)
        {
            for (int i = 0; i < archetypeGuests.Length; i++)
            {
                ArchetypeActorEntry a = archetypeGuests[i];
                if (a.actor == null) continue;
                GirlPowerOrla3DCharacterFacing.DisableRootMotion(a.actor);
                var prof = GirlPowerOrla3DArchetypeConfig.GetDefaultProfile(a.kind);
                var gr = new GuestRuntime
                {
                    cfg = a,
                    profile = prof,
                    homePos = a.actor.position,
                    patrolTarget = a.actor.position + new Vector3((i % 2 == 0 ? 3.2f : -3.2f), 0f, 0f),
                    walkPhase = i * 1.4f,
                    stateTimer = 2f + i * 2.5f,
                    faceSign = GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign(a.actor, 1f),
                    prevWorldPos = a.actor.position,
                    lastValidRot = a.actor.rotation
                };
                if (a.petCompanion != null)
                {
                    if (a.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
                    {
                        gr.calopWingL = FindChildRec(a.petCompanion, "Calop_WingL");
                        gr.calopWingR = FindChildRec(a.petCompanion, "Calop_WingR");
                    }
                    else
                    {
                        gr.petHead = FindChildRec(a.petCompanion, a.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Gato ? "Cat_Head" : "Pipoca_Head");
                        gr.petTail = FindChildRec(a.petCompanion, a.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Gato ? "Cat_Tail" : "Pipoca_Tail");
                        if (gr.petHead != null) gr.petHeadBase = gr.petHead.localRotation;
                        if (gr.petTail != null) gr.petTailBase = gr.petTail.localRotation;
                    }
                }
                guests.Add(gr);
            }
        }

        // Seat initial 2 diverse pet-friendly guests cleanly at the restaurant tables so dual bubbles shine right away!
        SeatNextAvailableGuest(0);
        SeatNextAvailableGuest(1);
    }

    private void Update()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        float dt = Time.deltaTime;
        float t = Time.time;

        UpdateCamponesaHarvester(dt, t);
        UpdateWaterBowls(dt, t);
        UpdateArchetypeGuestsAndPets(dt, t);

        Keyboard keys = Keyboard.current;
        if (keys != null && keys.eKey.wasPressedThisFrame)
            TryInteractNearPlayer();
    }

    // ------------------------------------------------------------------
    // 1. CAMPONESA HARVESTER (Fenced Orchard -> Kitchen Restock!)
    // ------------------------------------------------------------------
    private void UpdateCamponesaHarvester(float dt, float t)
    {
        if (camponesaHarvester == null || orchardTreePoints == null || orchardTreePoints.Length == 0) return;

        if (camponesaWait > 0f)
        {
            camponesaWait -= dt;
            // Harvesting crouch/reach animation
            float pickBob = Mathf.Abs(Mathf.Sin(t * 6.5f)) * 0.08f;
            Vector3 p = camponesaHarvester.position;
            camponesaHarvester.position = new Vector3(p.x, pickBob, p.z);
            return;
        }

        Transform goalT = camponesaCarryingToKitchen && kitchenDropoffPoint != null
            ? kitchenDropoffPoint
            : orchardTreePoints[orchardIdx % orchardTreePoints.Length];
        if (goalT == null) return;

        Vector3 cur = camponesaHarvester.position;
        Vector2 delta = new Vector2(goalT.position.x - cur.x, goalT.position.z - cur.z);
        float dist = delta.magnitude;

        if (dist < 1.15f)
        {
            if (!camponesaCarryingToKitchen)
            {
                string crop = OrchardCrops[orchardIdx % OrchardCrops.Length];
                camponesaCarryingToKitchen = true;
                camponesaWait = 2.6f;
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cur + Vector3.up * 1.4f, "🧺 Camponesa colheu " + crop + "!", new Color(0.72f, 0.96f, 0.62f), 1.6f);
            }
            else
            {
                camponesaCarryingToKitchen = false;
                orchardIdx = (orchardIdx + 1) % orchardTreePoints.Length;
                camponesaWait = 2.2f;
                OrchardHarvestedTotal++;
                if (megaSystems != null) megaSystems.Data.organicHerbs++;
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(cur + Vector3.up * 1.4f, "✓ Cozinha abastecida pela Horta!", new Color(1f, 0.90f, 0.45f), 1.6f);
            }
            return;
        }

        Vector2 dir = delta / dist;
        float step = Mathf.Min(dist, 2.0f * dt);
        float hop = Mathf.Abs(Mathf.Sin(t * 7.8f)) * 0.07f;
        Vector3 nextPos = new Vector3(cur.x + dir.x * step, hop, cur.z + dir.y * step);
        camponesaHarvester.position = nextPos;
        Vector3 realMove = new Vector3(nextPos.x - camponesaPrevPos.x, 0f, nextPos.z - camponesaPrevPos.z);
        float realSpeed = dt > 0.0001f ? (realMove.magnitude / dt) : 0f;
        camponesaPrevPos = nextPos;
        if (realSpeed > 0.25f && GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realMove, camponesaFaceSign, 0.00001f, out Quaternion cRot))
            camponesaLastRot = cRot;
        camponesaHarvester.rotation = Quaternion.Slerp(camponesaHarvester.rotation, camponesaLastRot, dt * 7f);
    }

    // ------------------------------------------------------------------
    // 2. INTERACTIVE PET WATER BOWLS & REPUTATION SYSTEM
    // ------------------------------------------------------------------
    private void UpdateWaterBowls(float dt, float t)
    {
        for (int i = 0; i < bowls.Count; i++)
        {
            BowlRuntime b = bowls[i];
            // Gradual drinking/evaporation along the sunny beach promenade (~65 seconds from full to empty)
            b.level = Mathf.Max(0f, b.level - dt * (0.012f + (i % 2) * 0.004f));

            // Update visual water level & color (Full=Turquoise, Half=Amber, Empty=Red pulse!)
            if (b.liqRenderer != null)
            {
                Transform lt = b.liqRenderer.transform;
                lt.localScale = new Vector3(1f, Mathf.Lerp(0.18f, 1.0f, b.level), 1f);
                Color col = b.level > 0.55f
                    ? new Color(0.24f, 0.84f, 0.88f, 1f)
                    : (b.level > 0.12f
                        ? new Color(0.95f, 0.72f, 0.24f, 1f)
                        : Color.Lerp(new Color(0.88f, 0.22f, 0.26f, 1f), new Color(1f, 0.55f, 0.55f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 7f)));
                b.liqRenderer.GetPropertyBlock(bowlMpb);
                bowlMpb.SetColor("_Color", col);
                b.liqRenderer.SetPropertyBlock(bowlMpb);
            }

            if (b.emptyAlertCooldown > 0f) b.emptyAlertCooldown -= dt;

            // Check if any client with a pet walks near an EMPTY bowl -> immediate Reputation loss!
            if (b.level <= 0.05f && b.emptyAlertCooldown <= 0f)
            {
                for (int g = 0; g < guests.Count; g++)
                {
                    if (guests[g].cfg.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.None || guests[g].cfg.actor == null) continue;
                    float d = Vector2.Distance(
                        new Vector2(guests[g].cfg.actor.position.x, guests[g].cfg.actor.position.z),
                        new Vector2(b.cfg.bowlRoot.position.x, b.cfg.bowlRoot.position.z));
                    if (d < 4.5f)
                    {
                        b.emptyAlertCooldown = 14f;
                        if (gameFlow == null || !gameFlow.FreePlayKidsMode)
                            PetReputation = Mathf.Max(0, PetReputation - 5);
                        if (audioController != null) audioController.PlayFailChime();
                        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(b.cfg.bowlRoot.position + Vector3.up * 0.6f, "💧 Pote Vazio! Pet com sede (-5 Rep)", new Color(1f, 0.42f, 0.44f), 2.2f);
                        break;
                    }
                }

                // Fairy Waitress autonomous rescue if player is busy elsewhere!
                if (fairyHelpers != null && fairyHelpers.Length > 0)
                {
                    b.fairyRefillTimer -= dt;
                    if (b.fairyRefillTimer <= 0f)
                    {
                        b.fairyRefillTimer = 26f;
                        RefillBowl(i, true);
                    }
                }
            }
        }
    }

    public void RefillBowl(int index, bool byFairy)
    {
        if (index < 0 || index >= bowls.Count) return;
        BowlRuntime b = bowls[index];
        b.level = 1.0f;
        b.emptyAlertCooldown = 4f;
        PetReputation = Mathf.Min(100, PetReputation + (byFairy ? 3 : 5));
        if (audioController != null) audioController.PlayChime();
        GirlPowerOrla3DJuice.PopScale(b.cfg.bowlRoot, 1.24f, 0.30f);
        string msg = byFairy ? "✨ Fada reabasteceu o pote! (+3 Rep)" : "💧 Pote cheio! (+5 Reputação)";
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(b.cfg.bowlRoot.position + Vector3.up * 0.5f, msg, new Color(0.45f, 0.94f, 0.96f), 1.8f);
    }

    // ------------------------------------------------------------------
    // 3. ARCHETYPE GUESTS, PETS (DOG / CAT / FLYING CALOPSITA) & DUAL ORDERS
    // ------------------------------------------------------------------
    private void SeatNextAvailableGuest(int tableIdx)
    {
        if (petFriendlyTables == null || tableIdx < 0 || tableIdx >= petFriendlyTables.Length || petFriendlyTables[tableIdx] == null) return;

        for (int i = 0; i < guests.Count; i++)
        {
            GuestRuntime g = guests[i];
            if (g.profile.isStaff || g.assignedTable >= 0 || g.profile.homeZone == GirlPowerOrla3DArchetypeConfig.MapZone.BeachAndSea)
                continue;
            AssignGuestToTable(g, tableIdx);
            return;
        }
    }

    private void AssignGuestToTable(GuestRuntime g, int tableIdx)
    {
        g.assignedTable = tableIdx;
        g.waitingForOrder = true;
        g.eating = false;
        g.stateTimer = 0f;
        string[] favs = g.profile.favoriteOrders;
        g.humanOrder = (favs != null && favs.Length > 0) ? favs[UnityEngine.Random.Range(0, favs.Length)] : "Suco Natural";
        g.petOrder = GirlPowerOrla3DArchetypeConfig.GetPetOrderLabel(g.cfg.petKind);

        // Seat guest cleanly beside the wide pet-friendly table with generous pet clearance!
        Transform tbl = petFriendlyTables[tableIdx];
        if (tbl != null && g.cfg.actor != null)
        {
            g.cfg.actor.position = tbl.position + new Vector3(-1.35f, 0f, -0.35f);
            g.cfg.actor.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
    }

    private void UpdateArchetypeGuestsAndPets(float dt, float t)
    {
        for (int i = 0; i < guests.Count; i++)
        {
            GuestRuntime g = guests[i];
            if (g.cfg.actor == null) continue;

            if (g.assignedTable >= 0)
            {
                if (g.eating)
                {
                    g.stateTimer -= dt;
                    float eatBob = Mathf.Abs(Mathf.Sin(t * 5.2f + i)) * 0.04f;
                    Vector3 p = g.cfg.actor.position;
                    g.cfg.actor.position = new Vector3(p.x, eatBob, p.z);
                    if (g.stateTimer <= 0f)
                    {
                        int finishedTable = g.assignedTable;
                        g.assignedTable = -1;
                        g.eating = false;
                        g.waitingForOrder = false;
                        g.patrolTarget = g.homePos;
                        SeatNextAvailableGuest(finishedTable);
                    }
                }
                else
                {
                    // Gently bob waiting for human + pet order
                    float waitBob = Mathf.Sin(t * 2.6f + i) * 0.025f;
                    Vector3 p = g.cfg.actor.position;
                    g.cfg.actor.position = new Vector3(p.x, Mathf.Max(0f, waitBob), p.z);
                }
            }
            else
            {
                // Zone-coherent strolling (Promenade tourists stay on promenade; Beach/Sports stay on sand!)
                Vector3 cur = g.cfg.actor.position;
                Vector2 delta = new Vector2(g.patrolTarget.x - cur.x, g.patrolTarget.z - cur.z);
                float dist = delta.magnitude;
                if (dist < 0.4f)
                {
                    g.stateTimer -= dt;
                    if (g.stateTimer <= 0f)
                    {
                        g.stateTimer = 4.5f + (i % 3) * 1.2f;
                        float offset = (Mathf.Abs(cur.x - g.homePos.x) < 1.2f) ? ((i % 2 == 0) ? 4.2f : -4.2f) : 0f;
                        g.patrolTarget = g.homePos + new Vector3(offset, 0f, 0f);
                    }
                }
                else
                {
                    Vector2 dir = delta / dist;
                    float spd = (g.cfg.kind == GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Vovo) ? 1.15f : 1.75f;
                    float step = Mathf.Min(dist, spd * dt);
                    float hop = Mathf.Abs(Mathf.Sin(t * 7.0f + g.walkPhase)) * 0.06f;
                    Vector3 nextPos = new Vector3(cur.x + dir.x * step, g.homePos.y + hop, cur.z + dir.y * step);
                    g.cfg.actor.position = nextPos;
                    Vector3 realMove = new Vector3(nextPos.x - g.prevWorldPos.x, 0f, nextPos.z - g.prevWorldPos.z);
                    float realSpeed = dt > 0.0001f ? (realMove.magnitude / dt) : 0f;
                    g.prevWorldPos = nextPos;
                    if (realSpeed > 0.25f && GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(realMove, g.faceSign, 0.00001f, out Quaternion gRot))
                        g.lastValidRot = gRot;
                    g.cfg.actor.rotation = Quaternion.Slerp(g.cfg.actor.rotation, g.lastValidRot, dt * 7f);
                }
            }

            // Animate owner's Pet Companion (Cachorro, Gato, or flying/perched Calopsita!)
            if (g.cfg.petCompanion != null)
            {
                Vector3 ownerPos = g.cfg.actor.position;
                if (g.cfg.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
                {
                    // Calopsita flutters beside owner's shoulder or hovers gently!
                    float hoverY = 1.28f + Mathf.Sin(t * 4.5f + i) * 0.14f;
                    Vector3 birdGoal = ownerPos + new Vector3(0.42f, hoverY, -0.18f);
                    g.cfg.petCompanion.position = Vector3.Lerp(g.cfg.petCompanion.position, birdGoal, dt * 6f);
                    g.cfg.petCompanion.rotation = Quaternion.Slerp(g.cfg.petCompanion.rotation, g.cfg.actor.rotation, dt * 6f);
                    float flap = Mathf.Sin(t * 22f + i) * 32f;
                    if (g.calopWingL != null) g.calopWingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                    if (g.calopWingR != null) g.calopWingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
                }
                else
                {
                    // Dog or Cat sits/trots cleanly beside owner (outside table collider!) with sniffing & varied gait rhythm!
                    float sniffWave = Mathf.Sin(t * 0.9f + i * 2.3f);
                    bool sniffingGround = sniffWave > 0.55f;
                    float paceMult = sniffingGround ? 0.45f : (sniffWave < -0.3f ? 1.28f : 0.95f);
                    Vector3 petGoal = ownerPos + new Vector3(-0.55f, 0f, 0.85f);
                    petGoal.y = Mathf.Max(0f, g.homePos.y) + (sniffingGround ? 0.01f : Mathf.Abs(Mathf.Sin(t * (6.2f * paceMult) + i)) * 0.06f);
                    g.cfg.petCompanion.position = Vector3.Lerp(g.cfg.petCompanion.position, petGoal, dt * (4.5f * paceMult));
                    Quaternion flatOwnerRot = Quaternion.Euler(0f, g.cfg.actor.eulerAngles.y, 0f);
                    g.cfg.petCompanion.rotation = Quaternion.Slerp(g.cfg.petCompanion.rotation, flatOwnerRot, dt * 6f);
                    if (g.petHead != null)
                    {
                        Quaternion headTarget = sniffingGround
                            ? g.petHeadBase * Quaternion.Euler(28f + Mathf.Sin(t * 16f) * 4f, Mathf.Sin(t * 20f) * 11f, 0f)
                            : g.petHeadBase * Quaternion.Euler(Mathf.Sin(t * 2.6f + i) * 4f, 0f, 0f);
                        g.petHead.localRotation = Quaternion.Slerp(g.petHead.localRotation, headTarget, dt * 10f);
                    }
                    if (g.petTail != null)
                        g.petTail.localRotation = g.petTailBase * Quaternion.Euler(0f, Mathf.Sin(t * (sniffingGround ? 14f : 8f) + i) * 24f, 0f);
                }
            }
        }
    }

    private void ServePetFriendlyGuest(GuestRuntime g)
    {
        if (g == null || !g.waitingForOrder) return;
        g.waitingForOrder = false;
        g.eating = true;
        g.stateTimer = Mathf.Max(4f, g.profile.eatDurationSec);
        PetOrdersServedTotal++;

        int coins = Mathf.RoundToInt(g.profile.baseCoins * g.profile.tipMultiplier);
        if (g.cfg.petKind != GirlPowerOrla3DArchetypeConfig.PetKind.None) coins += 4;
        PetReputation = Mathf.Min(100, PetReputation + 4);

        if (lifeActivities != null) lifeActivities.AddCoins(coins);
        if (audioController != null) audioController.PlayFanfare();
        GirlPowerOrla3DJuice.SpawnFlyingCoins(g.cfg.actor.position, 5);
        GirlPowerOrla3DJuice.PopScale(g.cfg.actor, 1.18f, 0.32f);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(
            g.cfg.actor.position + Vector3.up * 1.5f,
            "★ " + g.profile.displayName + " + Pet servidos! (+" + coins + "m  +4 Rep)",
            new Color(1f, 0.88f, 0.36f), 2.2f);
    }

    private bool TryInteractNearPlayer()
    {
        if (player == null) return false;
        // 1. Refill nearby low water bowl (only within clear reach <= 3.4m!)
        int bowlIdx = FindNearestLowBowl(3.4f);
        if (bowlIdx >= 0)
        {
            RefillBowl(bowlIdx, false);
            return true;
        }
        // 2. Serve nearby Pet-Friendly dual-order table (within clear reach <= 3.8m!)
        GuestRuntime g = FindNearestWaitingGuest(3.8f);
        if (g != null)
        {
            ServePetFriendlyGuest(g);
            return true;
        }
        return false;
    }

    private int FindNearestLowBowl(float maxDist)
    {
        if (player == null) return -1;
        int best = -1;
        float bestD = maxDist;
        for (int i = 0; i < bowls.Count; i++)
        {
            if (bowls[i].level > 0.85f || bowls[i].cfg.bowlRoot == null) continue;
            float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(bowls[i].cfg.bowlRoot.position.x, bowls[i].cfg.bowlRoot.position.z));
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private GuestRuntime FindNearestWaitingGuest(float maxDist)
    {
        if (player == null) return null;
        GuestRuntime best = null;
        float bestD = maxDist;
        for (int i = 0; i < guests.Count; i++)
        {
            if (!guests[i].waitingForOrder || guests[i].cfg.actor == null) continue;
            float d = Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(guests[i].cfg.actor.position.x, guests[i].cfg.actor.position.z));
            if (d < bestD) { bestD = d; best = guests[i]; }
        }
        return best;
    }

    public bool TryServeTableByIndex(int tableIndex, bool byFairy)
    {
        bool servedAny = false;
        for (int i = 0; i < guests.Count; i++)
        {
            if (guests[i].assignedTable == tableIndex && guests[i].waitingForOrder)
            {
                ServePetFriendlyGuest(guests[i]);
                servedAny = true;
                break;
            }
        }
        if (megaSystems != null && megaSystems.TryQuickServeTable(tableIndex))
            servedAny = true;
        return servedAny;
    }

    private Texture2D GetIllustratedOrderIcon(string orderName)
    {
        string key = "icon_juice_lemon";
        if (!string.IsNullOrEmpty(orderName))
        {
            string low = orderName.ToLowerInvariant();
            if (low.Contains("sushi") || low.Contains("combinado") || low.Contains("salm")) key = "icon_sushi_combo";
            else if (low.Contains("temaki")) key = "icon_temaki";
            else if (low.Contains("hot") || low.Contains("roll")) key = "icon_hotroll";
            else if (low.Contains("churrasc") || low.Contains("espet") || low.Contains("hamb") || low.Contains("prato")) key = "icon_burger";
            else if (low.Contains("batata")) key = "icon_fries";
            else if (low.Contains("peixe") || low.Contains("mar")) key = "icon_fish";
            else if (low.Contains("pastel")) key = "icon_pastel";
            else if (low.Contains("açaí") || low.Contains("acai")) key = "icon_acai";
            else if (low.Contains("bolo") || low.Contains("torta") || low.Contains("cupcake") || low.Contains("sobrem")) key = "icon_cupcake";
            else if (low.Contains("doce") || low.Contains("brigadeiro")) key = "icon_brigadeiro";
            else if (low.Contains("sorvete") || low.Contains("morango")) key = "icon_ice_berry";
            else if (low.Contains("rosa") || low.Contains("suco")) key = "icon_juice_berry";
            else if (low.Contains("coco")) key = "icon_coconut";
            else if (low.Contains("chá") || low.Contains("cha") || low.Contains("chimarr")) key = "icon_lemonade";
            else if (low.Contains("café") || low.Contains("cafe") || low.Contains("energ")) key = "icon_guarana";
            else if (low.Contains("água") || low.Contains("agua")) key = "icon_water";
        }
        if (orderIconCache.TryGetValue(key, out Texture2D cached)) return cached;
        Texture2D loaded = Resources.Load<Texture2D>("CardapioIcons/" + key);
        if (loaded == null) loaded = GirlPowerOrla3DUITheme.IconIceCreamLemonBasil;
        orderIconCache[key] = loaded;
        return loaded;
    }

    private Texture2D GetPetOrderIcon(GirlPowerOrla3DArchetypeConfig.PetKind petKind)
    {
        if (petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Gato)
        {
            if (!orderIconCache.TryGetValue("icon_fish", out Texture2D fish))
            {
                fish = Resources.Load<Texture2D>("CardapioIcons/icon_fish");
                orderIconCache["icon_fish"] = fish;
            }
            if (fish != null) return fish;
        }
        if (petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
            return GirlPowerOrla3DUITheme.IconBasil;
        return GirlPowerOrla3DUITheme.IconPetTreat;
    }

    // ------------------------------------------------------------------
    // ONGUI: Focused Single-Target World Icon + Reach-Only Action Pill
    // (Never crowds the screen or covers the protagonist / 3D models!)
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        if (sceneCamera == null || player == null) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        float s = Mathf.Clamp(Screen.width / 1600f, 0.72f, 1.15f);
        bool touchPrimary = storyController != null && storyController.IsTouchPrimary;

        // Pick ONLY the single closest actionable target within approach range (<= 7.0m) so icons stay calm and legible!
        const float IconFocusRange = 7.0f;
        const float BowlReachRange = 3.4f;
        const float TableReachRange = 3.8f;

        Vector2 playerXZ = new Vector2(player.position.x, player.position.z);
        int nearestBowlIdx = -1;
        float nearestBowlDist = IconFocusRange;
        for (int i = 0; i < bowls.Count; i++)
        {
            if (bowls[i].level > 0.55f || bowls[i].cfg.bowlRoot == null) continue;
            float d = Vector2.Distance(playerXZ, new Vector2(bowls[i].cfg.bowlRoot.position.x, bowls[i].cfg.bowlRoot.position.z));
            if (d < nearestBowlDist) { nearestBowlDist = d; nearestBowlIdx = i; }
        }

        GuestRuntime nearestGuest = null;
        float nearestGuestDist = IconFocusRange;
        for (int i = 0; i < guests.Count; i++)
        {
            GuestRuntime g = guests[i];
            if (!g.waitingForOrder || g.cfg.actor == null) continue;
            float d = Vector2.Distance(playerXZ, new Vector2(g.cfg.actor.position.x, g.cfg.actor.position.z));
            if (d < nearestGuestDist) { nearestGuestDist = d; nearestGuest = g; }
        }

        bool focusIsBowl = nearestBowlIdx >= 0 && (nearestGuest == null || nearestBowlDist <= nearestGuestDist);

        // 1. Anchored world-space icon strictly above the single nearest actionable target!
        if (focusIsBowl)
        {
            Transform bowlT = bowls[nearestBowlIdx].cfg.bowlRoot;
            Vector3 sp = sceneCamera.WorldToScreenPoint(bowlT.position + Vector3.up * 1.18f);
            if (sp.z > 0.2f && sp.z < 26f)
            {
                float pulse = 1.0f + Mathf.Sin(Time.time * 5.2f) * 0.08f;
                float floatY = Mathf.Sin(Time.time * 2.8f) * 3.0f * s;
                float sz = 44f * s * pulse;
                Rect badge = new Rect(sp.x - sz * 0.5f, (Screen.height - sp.y) - sz + floatY, sz, sz);
                if (GUI.Button(badge, GUIContent.none, GUIStyle.none))
                    QuickRefillBowl(nearestBowlIdx);
                GUI.DrawTexture(badge, GirlPowerOrla3DUITheme.CircleGlassTex);
                float ip = 7f * s * pulse;
                GUI.DrawTexture(new Rect(badge.x + ip, badge.y + ip, sz - ip * 2f, sz - ip * 2f), GirlPowerOrla3DUITheme.IconWaterDrop);
            }
        }
        else if (nearestGuest != null && nearestGuest.cfg.actor != null)
        {
            Vector3 sp = sceneCamera.WorldToScreenPoint(nearestGuest.cfg.actor.position + Vector3.up * 2.12f);
            if (sp.z > 0.2f && sp.z < 26f)
            {
                float cx = sp.x;
                float cy = Screen.height - sp.y;
                float bob = Mathf.Sin(Time.time * 2.8f) * 2.8f * s;
                bool hasPet = nearestGuest.cfg.petKind != GirlPowerOrla3DArchetypeConfig.PetKind.None;

                float bSz = 50f * s;
                float pad = 6f * s;
                float totalW = hasPet ? (bSz + 42f * s + 4f * s) : bSz;
                Rect hitBubble = new Rect(cx - totalW * 0.5f, cy - bSz + bob, totalW, bSz);
                if (GUI.Button(hitBubble, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle))
                    QuickServeGuest(nearestGuest);
                Texture2D foodIcon = GetIllustratedOrderIcon(nearestGuest.humanOrder);
                if (foodIcon != null)
                    GUI.DrawTexture(new Rect(hitBubble.x + pad, hitBubble.y + pad, bSz - pad * 2f, bSz - pad * 2f), foodIcon);

                if (hasPet)
                {
                    float pSz = 40f * s;
                    Texture2D petIcon = GetPetOrderIcon(nearestGuest.cfg.petKind);
                    if (petIcon != null)
                        GUI.DrawTexture(new Rect(hitBubble.x + bSz + 1f * s, hitBubble.y + (bSz - pSz) * 0.5f, pSz - 4f * s, pSz - 4f * s), petIcon);
                }
            }
        }

        // 2. Show primary action pill ONLY when the protagonist is actually within reach of the visible target!
        bool storyBusy = storyController != null && storyController.HasNearbyStoryInteractable;
        if (!storyBusy)
        {
            if (focusIsBowl && nearestBowlDist <= BowlReachRange)
            {
                string lbl = touchPrimary
                    ? GirlPowerOrla3DLocalization.T("Encher pote", "Refill bowl", "Llenar pote")
                    : GirlPowerOrla3DLocalization.T("[E] Encher pote", "[E] Refill bowl", "[E] Llenar pote");
                DrawUnifiedActionPill(lbl, s, () => RefillBowl(nearestBowlIdx, false));
                return;
            }
            if (!focusIsBowl && nearestGuest != null && nearestGuestDist <= TableReachRange)
            {
                string lbl = touchPrimary
                    ? GirlPowerOrla3DLocalization.T("Servir mesa", "Serve table", "Servir mesa")
                    : GirlPowerOrla3DLocalization.T("[E] Servir mesa", "[E] Serve table", "[E] Servir mesa");
                DrawUnifiedActionPill(lbl, s, () => ServePetFriendlyGuest(nearestGuest));
            }
        }
    }

    private void QuickServeGuest(GuestRuntime g)
    {
        if (g == null || !g.waitingForOrder || g.cfg.actor == null) return;
        if (player != null && Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(g.cfg.actor.position.x, g.cfg.actor.position.z)) <= 5.2f)
        {
            ServePetFriendlyGuest(g);
            return;
        }
        var npcAi = FindObjectOfType<GirlPowerOrla3DNpcAI>();
        if (npcAi != null && fairyHelpers != null && fairyHelpers.Length > 0 && fairyHelpers[0] != null)
        {
            npcAi.DispatchFairyTo(fairyHelpers[0], g.cfg.actor.position, () => ServePetFriendlyGuest(g));
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(g.cfg.actor.position + Vector3.up * 1.3f, "✨ Fada indo servir!", new Color(1f, 0.80f, 0.94f), 1.4f);
            return;
        }
        if (storyController != null)
            storyController.SetTapDestination(g.cfg.actor.position, () => ServePetFriendlyGuest(g));
    }

    private void QuickRefillBowl(int bowlIdx)
    {
        if (bowlIdx < 0 || bowlIdx >= bowls.Count || bowls[bowlIdx].cfg.bowlRoot == null) return;
        Vector3 bPos = bowls[bowlIdx].cfg.bowlRoot.position;
        if (player != null && Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(bPos.x, bPos.z)) <= 4.6f)
        {
            RefillBowl(bowlIdx, false);
            return;
        }
        var npcAi = FindObjectOfType<GirlPowerOrla3DNpcAI>();
        if (npcAi != null && fairyHelpers != null && fairyHelpers.Length > 0 && fairyHelpers[0] != null)
        {
            npcAi.DispatchFairyTo(fairyHelpers[0], bPos, () => RefillBowl(bowlIdx, true));
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(bPos + Vector3.up * 0.6f, "✨ Fada enchendo água!", new Color(0.58f, 0.94f, 1f), 1.4f);
            return;
        }
        if (storyController != null)
            storyController.SetTapDestination(bPos, () => RefillBowl(bowlIdx, false));
    }

    private static void DrawUnifiedActionPill(string text, float s, Action onClick)
    {
        Rect sa = Screen.safeArea;
        float safeTop = Screen.height - (sa.y + sa.height);
        float safeBottom = safeTop + sa.height;
        float w = Mathf.Clamp(86f * s + text.Length * 7.8f * s, 200f * s, Mathf.Min(340f * s, sa.width - 28f * s));
        float h = 46f * s;
        Rect pill = new Rect(sa.x + (sa.width - w) * 0.5f, safeBottom - 60f * s, w, h);
        if (GUI.Button(pill, text, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            onClick?.Invoke();
    }

    private static Transform FindChildRec(Transform root, string n)
    {
        if (root.name == n) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindChildRec(root.GetChild(i), n);
            if (hit != null) return hit;
        }
        return null;
    }
}
