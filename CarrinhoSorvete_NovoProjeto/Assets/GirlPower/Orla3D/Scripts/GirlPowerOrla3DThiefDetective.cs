using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Street Thieves vs. Detective Pipoca & Coastal Police System for Baía do Avesso:
/// - Street thieves periodically sneak into the market stalls & restaurants to rob them!
/// - Detective Pipoca immediately runs to the suspect and gives 5 expressive signals:
///   Latir (bark), Rolar (roll), Morder (bite to slow thief), Se Chacalhar (shake), Chorar (whine)!
/// - If the player & workers notice the signals and call the Police in advance ([L] or HUD button),
///   the Patrol Car & Officer rush in with siren, arrest the thief, award Detective Points + Coins,
///   make Pipoca super happy, and after 3 prevented robberies the player WINS THE GAME!
/// - If 3 robberies succeed before the police arrive, the player LOSES (Game Over)!
/// </summary>
public sealed class GirlPowerOrla3DThiefDetective : MonoBehaviour
{
    private enum RaidState
    {
        Calm,
        ThiefApproaching,
        ThiefStealing,
        PoliceResponding,
        Cooldown
    }

    [Header("Blender 3D Actors")]
    public Transform player;
    public Camera sceneCamera;
    public Transform thiefActor;
    public Transform policeOfficer;
    public Transform policeCar;
    public GirlPowerOrla3DPetCompanion pipocaDog;
    public Transform[] raidTargets; // Market stalls, Restaurant, Kiosk, Florist
    public Transform[] workerNpcs;  // Workers who shout when alerted

    [Header("Connected Systems")]
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;

    [Header("Balance & Rules")]
    public int arrestsToWin = 3;
    public int maxAllowedThefts = 3;
    public float firstRaidDelay = 16f;
    public float raidInterval = 22f;
    public float stealDurationSeconds = 20f;

    public int ArrestsCount { get; private set; }
    public int TheftsOccurred { get; private set; }
    public int DetectivePoints { get; private set; }
    public bool IsThiefActive => state == RaidState.ThiefApproaching || state == RaidState.ThiefStealing;

    private RaidState state = RaidState.Calm;
    private float stateTimer;
    private int currentTargetIndex = -1;
    private Transform currentTarget;
    private Vector3 targetStandPos;
    private float stealProgress; // 0..1
    private float signalAudioTimer;
    private GirlPowerOrla3DPetCompanion.DetectiveSignal lastSignal = GirlPowerOrla3DPetCompanion.DetectiveSignal.None;
    private string livePipocaSignalText = "";
    private bool policeCalledEarly;

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();

        ResetSession();
    }

    public void ResetSession()
    {
        ArrestsCount = 0;
        TheftsOccurred = 0;
        DetectivePoints = 0;
        state = RaidState.Calm;
        stateTimer = firstRaidDelay;
        stealProgress = 0f;
        policeCalledEarly = false;
        livePipocaSignalText = "";
        lastSignal = GirlPowerOrla3DPetCompanion.DetectiveSignal.None;

        if (thiefActor != null) thiefActor.gameObject.SetActive(false);
        if (policeOfficer != null) policeOfficer.gameObject.SetActive(false);
        if (policeCar != null) policeCar.gameObject.SetActive(false);
        if (pipocaDog != null) pipocaDog.ClearDetectiveAlert();
    }

    private void Update()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        float dt = Time.deltaTime;

        // Player hotkey [L] to call the Police in advance!
        Keyboard keys = Keyboard.current;
        if (keys != null && keys.lKey.wasPressedThisFrame && IsThiefActive)
            CallPoliceNow();

        switch (state)
        {
            case RaidState.Calm:
            case RaidState.Cooldown:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                    SpawnNextThiefRaid();
                break;

            case RaidState.ThiefApproaching:
                UpdateThiefApproach(dt);
                break;

            case RaidState.ThiefStealing:
                UpdateThiefStealing(dt);
                break;

            case RaidState.PoliceResponding:
                UpdatePoliceArrestSequence(dt);
                break;
        }
    }

    private void SpawnNextThiefRaid()
    {
        if (thiefActor == null || raidTargets == null || raidTargets.Length == 0)
        {
            stateTimer = 15f;
            return;
        }

        // Pick a valid fair stall / restaurant / kiosk target
        for (int tries = 0; tries < 8; tries++)
        {
            int idx = Random.Range(0, raidTargets.Length);
            if (raidTargets[idx] != null)
            {
                currentTargetIndex = idx;
                currentTarget = raidTargets[idx];
                break;
            }
        }
        if (currentTarget == null) return;

        // Spawn thief sneaking from far edge of the promenade
        float spawnX = currentTarget.position.x > 10f ? currentTarget.position.x + 12f : currentTarget.position.x - 12f;
        spawnX = Mathf.Clamp(spawnX, -20f, 40f);
        thiefActor.position = new Vector3(spawnX, 0f, -2.1f);
        thiefActor.gameObject.SetActive(true);

        targetStandPos = new Vector3(currentTarget.position.x, 0f, Mathf.Clamp(currentTarget.position.z - 1.55f, -3.2f, 2.2f));
        stealProgress = 0f;
        policeCalledEarly = false;
        signalAudioTimer = 0.5f;
        state = RaidState.ThiefApproaching;

        if (audioController != null) audioController.PlayThiefAlert();
        UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Latir, "Pipoca está LATINDO! Suspeito indo para " + currentTarget.name + "!");
    }

    private void UpdateThiefApproach(float dt)
    {
        Vector3 cur = thiefActor.position;
        Vector3 delta = targetStandPos - cur;
        delta.y = 0f;
        float dist = delta.magnitude;

        if (dist > 0.35f)
        {
            Vector3 dir = delta / dist;
            float creepHop = Mathf.Abs(Mathf.Sin(Time.time * 7.5f)) * 0.08f;
            thiefActor.position = new Vector3(cur.x + dir.x * 2.55f * dt, creepHop, cur.z + dir.z * 2.55f * dt);
            thiefActor.rotation = Quaternion.Slerp(thiefActor.rotation, Quaternion.LookRotation(-dir, Vector3.up), dt * 8f);
        }
        else
        {
            state = RaidState.ThiefStealing;
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Rolar, "Pipoca está ROLANDO no chão! O ladrão chegou na barraca!");
        }

        TickPipocaAlertSounds(dt);
    }

    private void UpdateThiefStealing(float dt)
    {
        // Pipoca biting the thief slows the robbery down by 45% so the humans have time to call the police!
        bool pipocaBiting = lastSignal == GirlPowerOrla3DPetCompanion.DetectiveSignal.Morder;
        float rate = (1f / Mathf.Max(8f, stealDurationSeconds)) * (pipocaBiting ? 0.55f : 1.0f);
        stealProgress += dt * rate;

        // Sneaky nervous stealing animation on thief
        float wiggle = Mathf.Sin(Time.time * 12f) * 8f;
        thiefActor.position = new Vector3(targetStandPos.x, Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.06f, targetStandPos.z);
        thiefActor.rotation = Quaternion.Euler(14f, 0f, wiggle);

        // Cycle Pipoca through ALL 5 detective warning signals as danger rises!
        if (stealProgress < 0.20f)
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Latir, "Pipoca está LATINDO! Suspeito na feira!");
        else if (stealProgress < 0.40f)
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Rolar, "Pipoca está ROLANDO no chão avisando os trabalhadores!");
        else if (stealProgress < 0.62f)
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Morder, "Pipoca está MORDENDO o ladrão para atrasar o furto!");
        else if (stealProgress < 0.78f)
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Chacalhar, "Pipoca está SE CHACOALHANDO de alerta! Chame a Polícia [L]!");
        else
            UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal.Chorar, "Pipoca está CHORANDO! Furto quase acontecendo — Chame a Polícia [L]!");

        TickPipocaAlertSounds(dt);

        // Robbery succeeded -> Player loses a Bay Security heart/shield!
        if (stealProgress >= 1f)
            OnRobberySucceeded();
    }

    private void UpdatePipocaSignal(GirlPowerOrla3DPetCompanion.DetectiveSignal sig, string desc)
    {
        livePipocaSignalText = desc;
        if (pipocaDog != null)
            pipocaDog.SetDetectiveAlert(thiefActor, sig);

        if (sig != lastSignal)
        {
            lastSignal = sig;
            if (pipocaDog != null)
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pipocaDog.transform.position, "🐾 " + SignalShortLabel(sig), new Color(1f, 0.84f, 0.32f), 1.6f);

            if (audioController != null)
            {
                if (sig == GirlPowerOrla3DPetCompanion.DetectiveSignal.Latir) audioController.PlayPetSound(false);
                else if (sig == GirlPowerOrla3DPetCompanion.DetectiveSignal.Morder) audioController.PlayPipocaBite();
                else if (sig == GirlPowerOrla3DPetCompanion.DetectiveSignal.Chorar) audioController.PlayPipocaWhine();
            }
        }
    }

    private void TickPipocaAlertSounds(float dt)
    {
        signalAudioTimer -= dt;
        if (signalAudioTimer > 0f || audioController == null) return;
        signalAudioTimer = 2.6f;

        if (lastSignal == GirlPowerOrla3DPetCompanion.DetectiveSignal.Chorar)
            audioController.PlayPipocaWhine();
        else if (lastSignal == GirlPowerOrla3DPetCompanion.DetectiveSignal.Morder)
            audioController.PlayPipocaBite();
        else
            audioController.PlayPetSound(false);
    }

    /// <summary>Called when the player / workers notice Pipoca's signals and call the Police in advance!</summary>
    public void CallPoliceNow()
    {
        if (!IsThiefActive || policeCalledEarly) return;
        policeCalledEarly = true;
        state = RaidState.PoliceResponding;
        stateTimer = 2.4f;

        if (audioController != null) audioController.PlayPoliceSiren();
        GirlPowerOrla3DJuice.ShakeCamera(0.10f, 0.16f);

        // Workers notice and shout in support!
        if (workerNpcs != null)
        {
            for (int i = 0; i < workerNpcs.Length; i++)
            {
                if (workerNpcs[i] != null && workerNpcs[i].gameObject.activeInHierarchy)
                {
                    GirlPowerOrla3DJuice.PopScale(workerNpcs[i], 1.18f, 0.30f);
                    if (i == 0)
                        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(workerNpcs[i].position, "Trabalhadores: Polícia chamada a tempo!", new Color(0.56f, 0.90f, 1f), 2.0f);
                }
            }
        }

        // Dispatch Coastal Police Patrol Car + Officer rushing toward the thief!
        if (policeCar != null && thiefActor != null)
        {
            float startX = thiefActor.position.x - 14f;
            policeCar.position = new Vector3(startX, 0f, -4.4f);
            policeCar.rotation = Quaternion.Euler(0f, -90f, 0f);
            policeCar.gameObject.SetActive(true);
        }
        if (policeOfficer != null && thiefActor != null)
        {
            policeOfficer.position = thiefActor.position + new Vector3(-2.6f, 0f, -1.6f);
            policeOfficer.gameObject.SetActive(true);
        }

        livePipocaSignalText = "🚓 Polícia a caminho! Detetive Pipoca salvou a feira!";
    }

    private void UpdatePoliceArrestSequence(float dt)
    {
        stateTimer -= dt;

        if (policeCar != null && thiefActor != null)
        {
            Vector3 carGoal = new Vector3(thiefActor.position.x - 1.2f, 0f, -4.4f);
            policeCar.position = Vector3.Lerp(policeCar.position, carGoal, 1f - Mathf.Exp(-4.5f * dt));
        }

        if (policeOfficer != null && thiefActor != null)
        {
            Vector3 offGoal = thiefActor.position + new Vector3(-0.95f, 0f, -0.35f);
            policeOfficer.position = Vector3.Lerp(policeOfficer.position, offGoal, 1f - Mathf.Exp(-5.5f * dt));
            Vector3 look = thiefActor.position - policeOfficer.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                policeOfficer.rotation = Quaternion.LookRotation(-look.normalized, Vector3.up);
        }

        // Thief trembles in surrender!
        if (thiefActor != null)
            thiefActor.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 28f) * 18f, 0f);

        if (stateTimer <= 0f)
            CompletePoliceArrest();
    }

    private void CompletePoliceArrest()
    {
        ArrestsCount++;
        int earlyBonus = Mathf.RoundToInt((1f - Mathf.Clamp01(stealProgress)) * 60f);
        int pts = 100 + earlyBonus;
        int coinsReward = 25;
        DetectivePoints += pts;

        Vector3 arrestPos = thiefActor != null ? thiefActor.position : (player != null ? player.position : Vector3.zero);

        if (thiefActor != null) thiefActor.gameObject.SetActive(false);
        if (policeOfficer != null) policeOfficer.gameObject.SetActive(false);
        if (policeCar != null) policeCar.gameObject.SetActive(false);

        if (lifeActivities != null) lifeActivities.AddCoins(coinsReward);
        if (megaSystems != null) megaSystems.AddPrestige(15, arrestPos);
        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        if (pipocaDog != null) pipocaDog.TriggerDetectiveHappyVictory();
        if (audioController != null) audioController.PlayVictoryFanfare();

        GirlPowerOrla3DJuice.SpawnFlyingCoins(arrestPos, 9);
        GirlPowerOrla3DJuice.ShakeCamera(0.14f, 0.20f);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(arrestPos,
            "🚓 LADRÃO PRESO! +" + pts + " pts  (+" + coinsReward + " moedas)",
            new Color(0.54f, 0.96f, 0.86f), 2.5f);

        // WIN THE GAME when 3 robberies are prevented in advance!
        if (ArrestsCount >= arrestsToWin)
        {
            state = RaidState.Calm;
            stateTimer = 999f;
            if (gameFlow != null)
                gameFlow.TriggerDetectiveVictory(DetectivePoints, ArrestsCount, ArrestsCount * coinsReward);
            return;
        }

        state = RaidState.Cooldown;
        stateTimer = raidInterval;
        livePipocaSignalText = "";
    }

    private void OnRobberySucceeded()
    {
        TheftsOccurred++;
        Vector3 theftPos = thiefActor != null ? thiefActor.position : Vector3.zero;
        if (thiefActor != null) thiefActor.gameObject.SetActive(false);
        if (pipocaDog != null) pipocaDog.ClearDetectiveAlert();
        if (audioController != null) audioController.PlayFailChime();
        GirlPowerOrla3DJuice.ShakeCamera(0.20f, 0.28f);

        if (lifeActivities != null) lifeActivities.AddCoins(-10);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(theftPos,
            "⚠ FURTO NA BAÍA! (" + TheftsOccurred + "/" + maxAllowedThefts + ")",
            new Color(1f, 0.36f, 0.40f), 2.5f);

        // GAME OVER when street thieves rob the fairs/restaurants 3 times!
        if (TheftsOccurred >= maxAllowedThefts)
        {
            state = RaidState.Calm;
            if (gameFlow != null)
                gameFlow.TriggerGameOver("Os ladrões de rua furtaram as feiras e restaurantes da Baía do Avesso! Observe os sinais do Detetive Pipoca e chame a Polícia com antecedência [L]!");
            return;
        }

        state = RaidState.Cooldown;
        stateTimer = raidInterval * 0.85f;
        livePipocaSignalText = "";
    }

    private static string SignalShortLabel(GirlPowerOrla3DPetCompanion.DetectiveSignal sig)
    {
        switch (sig)
        {
            case GirlPowerOrla3DPetCompanion.DetectiveSignal.Latir: return "AU! AU! (Latindo)";
            case GirlPowerOrla3DPetCompanion.DetectiveSignal.Rolar: return "Rolando no chão!";
            case GirlPowerOrla3DPetCompanion.DetectiveSignal.Morder: return "Mordendo o ladrão!";
            case GirlPowerOrla3DPetCompanion.DetectiveSignal.Chacalhar: return "Se chacoalhando!";
            case GirlPowerOrla3DPetCompanion.DetectiveSignal.Chorar: return "Chorando! Socorro!";
            default: return "Alerta!";
        }
    }

    // ------------------------------------------------------------------
    // DETECTIVE SECURITY HUD & CALL POLICE BUTTON
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        float s = Mathf.Clamp(Screen.width / 1600f, 0.68f, 1.15f);

        // Compact Detective Security Badge below top-left Order Card
        float bw = 326f * s;
        Rect badge = new Rect(18f * s, 134f * s, bw, 44f * s);
        GUI.Box(badge, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        GUI.DrawTexture(new Rect(badge.x + 8f * s, badge.y + 8f * s, 28f * s, 28f * s), GirlPowerOrla3DUITheme.IconPaw);

        string secText = "Detetive Pipoca: " + ArrestsCount + "/" + arrestsToWin + "  •  " + DetectivePoints + " pts  •  Furtos: " + TheftsOccurred + "/" + maxAllowedThefts;
        GUI.Label(new Rect(badge.x + 40f * s, badge.y + 4f * s, bw - 48f * s, 36f * s), secText,
            new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { fontSize = Mathf.RoundToInt(11.5f * s), alignment = TextAnchor.MiddleLeft });

        // Active Thief Alert Banner + One-Click Call Police [L] Button!
        if (IsThiefActive || state == RaidState.PoliceResponding)
        {
            float aw = 510f * s;
            float ah = IsThiefActive ? 104f * s : 56f * s;
            Rect alert = new Rect((Screen.width - aw) * 0.5f, 82f * s, aw, ah);
            GUI.Box(alert, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

            GUIStyle warnSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(14f * s),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.66f, 0.14f, 0.22f) }
            };
            GUI.Label(new Rect(alert.x + 14f * s, alert.y + 6f * s, aw - 28f * s, 30f * s), "🐾  " + livePipocaSignalText, warnSt);

            if (IsThiefActive)
            {
                // Steal danger progress bar
                Color barCol = stealProgress < 0.55f ? new Color(0.98f, 0.72f, 0.22f) : new Color(0.92f, 0.24f, 0.30f);
                GirlPowerOrla3DUITheme.DrawProgressBar(new Rect(alert.x + 18f * s, alert.y + 38f * s, aw - 36f * s, 12f * s), stealProgress, barCol);

                // Big glowing Call Police button!
                if (GUI.Button(new Rect(alert.x + (aw - 280f * s) * 0.5f, alert.y + 56f * s, 280f * s, 40f * s), "🚓  CHAMAR POLÍCIA (190)  [L]", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                    CallPoliceNow();

                // Floating world alert directly above the Street Thief!
                if (sceneCamera != null && thiefActor != null)
                {
                    Vector3 sp = sceneCamera.WorldToScreenPoint(thiefActor.position + Vector3.up * 2.15f);
                    if (sp.z > 0.1f)
                    {
                        float fw = 178f * s;
                        float fx = Mathf.Clamp(sp.x - fw * 0.5f, 16f * s, Screen.width - fw - 16f * s);
                        float fy = Mathf.Clamp((Screen.height - sp.y) - 38f * s, 188f * s, Screen.height - 140f * s);
                        if (GUI.Button(new Rect(fx, fy, fw, 36f * s), "⚠ LADRÃO! [L] Polícia", GirlPowerOrla3DUITheme.BtnDangerStyle))
                            CallPoliceNow();
                    }
                }
            }
        }
    }
}
