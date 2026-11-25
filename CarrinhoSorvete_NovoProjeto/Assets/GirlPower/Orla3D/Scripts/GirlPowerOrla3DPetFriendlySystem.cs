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
        public float walkPhase;
        public Vector3 patrolTarget;
    }

    private readonly List<BowlRuntime> bowls = new List<BowlRuntime>();
    private readonly List<GuestRuntime> guests = new List<GuestRuntime>();
    private MaterialPropertyBlock bowlMpb;

    // Camponesa harvest cycle state
    private int orchardIdx;
    private bool camponesaCarryingToKitchen;
    private float camponesaWait;
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

        if (archetypeGuests != null)
        {
            for (int i = 0; i < archetypeGuests.Length; i++)
            {
                ArchetypeActorEntry a = archetypeGuests[i];
                if (a.actor == null) continue;
                var prof = GirlPowerOrla3DArchetypeConfig.GetDefaultProfile(a.kind);
                var gr = new GuestRuntime
                {
                    cfg = a,
                    profile = prof,
                    homePos = a.actor.position,
                    patrolTarget = a.actor.position + new Vector3((i % 2 == 0 ? 3.2f : -3.2f), 0f, 0f),
                    walkPhase = i * 1.4f,
                    stateTimer = 2f + i * 2.5f
                };
                if (a.petCompanion != null && a.petKind == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
                {
                    gr.calopWingL = FindChildRec(a.petCompanion, "Calop_WingL");
                    gr.calopWingR = FindChildRec(a.petCompanion, "Calop_WingR");
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
        camponesaHarvester.position = new Vector3(cur.x + dir.x * step, hop, cur.z + dir.y * step);
        camponesaHarvester.rotation = Quaternion.Slerp(camponesaHarvester.rotation, Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.y), Vector3.up), dt * 6f);
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
            g.cfg.actor.rotation = Quaternion.Euler(0f, -90f, 0f);
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
                    g.cfg.actor.position = new Vector3(cur.x + dir.x * step, g.homePos.y + hop, cur.z + dir.y * step);
                    g.cfg.actor.rotation = Quaternion.Slerp(g.cfg.actor.rotation, Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.y), Vector3.up), dt * 6f);
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
                    // Dog or Cat sits with clean spacing beside owner (never clipping chair!)
                    Vector3 petGoal = ownerPos + new Vector3(-0.78f, 0f, -0.55f);
                    petGoal.y = g.homePos.y + Mathf.Abs(Mathf.Sin(t * 5.5f + i)) * 0.05f;
                    g.cfg.petCompanion.position = Vector3.Lerp(g.cfg.petCompanion.position, petGoal, dt * 4.5f);
                    g.cfg.petCompanion.rotation = Quaternion.Slerp(g.cfg.petCompanion.rotation, g.cfg.actor.rotation, dt * 6f);
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
        // 1. Refill nearby thirsty/low water bowl
        int bowlIdx = FindNearestLowBowl(2.8f);
        if (bowlIdx >= 0)
        {
            RefillBowl(bowlIdx, false);
            return true;
        }
        // 2. Serve nearby Pet-Friendly dual-order table
        GuestRuntime g = FindNearestWaitingGuest(3.4f);
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
            if (bowls[i].level > 0.72f || bowls[i].cfg.bowlRoot == null) continue;
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

    // ------------------------------------------------------------------
    // ONGUI: Dual Human+Pet Order Bubbles, Empty Water Bowl Alerts & Unified [E] Action
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        if (sceneCamera == null) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        float s = Mathf.Clamp(Screen.width / 1600f, 0.68f, 1.15f);

        // 1. Compact Pet Reputation pill tucked neatly at top-right under the icon bar (never blocks center!)
        float rw = 192f * s;
        Rect repRect = new Rect(Screen.width - rw - 16f * s, 186f * s, rw, 30f * s);
        GUI.Box(repRect, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        GUI.Label(repRect, "🐾 Reputação Pet: " + PetReputation + "%",
            new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(11.5f * s) });

        // 2. Dual Order Bubbles (Human Order + Pet Order!) above seated Pet-Friendly guests
        for (int i = 0; i < guests.Count; i++)
        {
            GuestRuntime g = guests[i];
            if (!g.waitingForOrder || g.cfg.actor == null) continue;
            Vector3 sp = sceneCamera.WorldToScreenPoint(g.cfg.actor.position + Vector3.up * 2.1f);
            if (sp.z <= 0.2f || sp.z > 28f) continue;

            float cx = sp.x;
            float cy = Screen.height - sp.y;
            bool hasPet = g.cfg.petKind != GirlPowerOrla3DArchetypeConfig.PetKind.None;

            float bw = 152f * s;
            float bh = 38f * s;
            Rect humanBubble = new Rect(hasPet ? (cx - bw - 4f * s) : (cx - bw * 0.5f), cy - bh, bw, bh);
            GUI.Box(humanBubble, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            GUI.Label(humanBubble, "🍽 " + g.humanOrder,
                new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(10.5f * s), alignment = TextAnchor.MiddleCenter });

            if (hasPet)
            {
                Rect petBubble = new Rect(cx + 4f * s, cy - bh, bw, bh);
                GUI.Box(petBubble, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
                GUI.Label(petBubble, "🐾 " + g.petOrder,
                    new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(10.5f * s), alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.16f, 0.48f, 0.42f) } });
            }
        }

        // 3. Red Alert Icon above EMPTY street/restaurant Water Bowls!
        for (int i = 0; i < bowls.Count; i++)
        {
            if (bowls[i].level > 0.14f || bowls[i].cfg.bowlRoot == null) continue;
            Vector3 sp = sceneCamera.WorldToScreenPoint(bowls[i].cfg.bowlRoot.position + Vector3.up * 0.85f);
            if (sp.z <= 0.2f || sp.z > 26f) continue;
            float pw = 114f * s;
            Rect al = new Rect(sp.x - pw * 0.5f, (Screen.height - sp.y) - 28f * s, pw, 26f * s);
            GUI.Box(al, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
            GUI.Label(al, "💧 Pote Vazio!", new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(10.5f * s) });
        }

        // 4. Unified bottom-center [E] Primary Action Pill (only if story isn't showing an [E] prompt!)
        bool storyBusy = storyController != null && storyController.HasNearbyStoryInteractable;
        if (!storyBusy)
        {
            int lowBowl = FindNearestLowBowl(2.8f);
            if (lowBowl >= 0)
            {
                DrawUnifiedActionPill("[E] Reabastecer Pote de Água", s, () => RefillBowl(lowBowl, false));
                return;
            }
            GuestRuntime waiting = FindNearestWaitingGuest(3.4f);
            if (waiting != null)
            {
                DrawUnifiedActionPill("[E] Servir " + waiting.profile.displayName + " & Pet", s, () => ServePetFriendlyGuest(waiting));
            }
        }
    }

    private static void DrawUnifiedActionPill(string text, float s, Action onClick)
    {
        float w = Mathf.Clamp(92f * s + text.Length * 7.8f * s, 225f * s, 380f * s);
        float h = 46f * s;
        Rect pill = new Rect((Screen.width - w) * 0.5f, Screen.height - 118f * s, w, h);
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
