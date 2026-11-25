using UnityEngine;

/// <summary>
/// Active Mechanical Companion for Pipoca (Dog) & Mingau (Cat):
/// 1. Sniffs out hidden fresh ingredients & buried low-tide shells/treasures!
/// 2. Guards the cart & garden beds, barking to chase away sneaky crabs/seagulls!
/// 3. Performs adorable tricks (Sentar, Dar a Pata, Girar) to entertain impatient clients & earn tips!
/// 4. Supports equipping matching coastal Bandana prop (PetBandanaProp.fbx)!
/// </summary>
public sealed class GirlPowerOrla3DPetCompanion : MonoBehaviour
{
    public enum DetectiveSignal
    {
        None = 0,
        Latir = 1,
        Rolar = 2,
        Morder = 3,
        Chacalhar = 4,
        Chorar = 5,
        FelizComemorando = 6
    }

    public string petName = "Pipoca";
    public bool isCat;
    public Transform followTarget;
    public Transform guardPoint;
    public Transform bandanaProp;
    public Transform detectiveHatProp;
    public GirlPowerOrla3DAudio audioController;
    public Vector3 followOffset = new Vector3(-1.15f, 0f, -0.75f);
    public float followSpeed = 5.6f;
    public float stopDistance = 1.45f;
    public bool isFollowing;

    public bool HasBandanaEquipped { get; private set; }
    public DetectiveSignal CurrentDetectiveSignal { get; private set; } = DetectiveSignal.None;

    private Vector3 homePos;
    private float hopClock;
    private float happyTimer;
    private float trickTimer;
    private int trickMode; // 0 = Sentar, 1 = Dar a Pata, 2 = Girar 360
    private float guardCooldown;
    private Vector3 sniffTarget;
    private bool sniffingActive;

    // Detective mode state
    private Transform detectiveSuspect;
    private float detectiveAnimClock;
    private float detectiveHappyTimer;

    // Articulated Blender 5.3 joints for Pipoca (tail wag, floppy ears, galloping legs!)
    private Transform pHead;
    private Transform pTail;
    private Transform pEarL;
    private Transform pEarR;
    private Transform pLegFL;
    private Transform pLegFR;
    private Transform pLegBL;
    private Transform pLegBR;
    private Vector3 prevFramePos;

    private static readonly Vector3[] HiddenSpots =
    {
        new Vector3(4.5f, -0.15f, 7.2f),   // Low-tide shells near sand
        new Vector3(13.2f, 0f, 3.2f),      // Behind market stalls
        new Vector3(21.4f, -0.14f, 6.9f),  // Buried beach treasure
        new Vector3(-4.8f, 0f, 2.4f),      // Behind Sushi/Comida stall
        new Vector3(17.8f, -0.18f, 7.8f)   // Near Pavilion steps
    };
    private int sniffIndex;

    private void Start()
    {
        homePos = transform.position;
        if (bandanaProp != null)
        {
            bandanaProp.SetParent(transform, false);
            bandanaProp.localPosition = new Vector3(0f, 0.36f, -0.14f);
            bandanaProp.localScale = Vector3.one * 0.85f;
            bandanaProp.gameObject.SetActive(HasBandanaEquipped);
        }
        if (detectiveHatProp != null)
        {
            detectiveHatProp.SetParent(transform, false);
            detectiveHatProp.localPosition = new Vector3(0f, 0.72f, -0.18f);
            detectiveHatProp.localScale = Vector3.one * 0.85f;
            detectiveHatProp.gameObject.SetActive(false);
        }
        pHead = FindChildRec(transform, "Pipoca_Head");
        pTail = FindChildRec(transform, "Pipoca_Tail");
        pEarL = FindChildRec(transform, "Pipoca_EarL");
        pEarR = FindChildRec(transform, "Pipoca_EarR");
        pLegFL = FindChildRec(transform, "Pipoca_LegFL");
        pLegFR = FindChildRec(transform, "Pipoca_LegFR");
        pLegBL = FindChildRec(transform, "Pipoca_LegBL");
        pLegBR = FindChildRec(transform, "Pipoca_LegBR");
        prevFramePos = transform.position;
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

    private void LateUpdate()
    {
        if (isCat) return;
        float t = Time.time;
        float dt = Mathf.Max(0.001f, Time.deltaTime);
        float speed = new Vector2(transform.position.x - prevFramePos.x, transform.position.z - prevFramePos.z).magnitude / dt;
        prevFramePos = transform.position;

        bool excited = happyTimer > 0f || detectiveHappyTimer > 0f || detectiveSuspect != null || trickTimer > 0f;
        float wagRate = excited ? 20f : 9f;
        float wagAmp = excited ? 36f : 18f;
        if (pTail != null)
            pTail.localRotation = Quaternion.Euler(excited ? 18f : 0f, Mathf.Sin(t * wagRate) * wagAmp, 0f);

        float earFlop = speed > 0.35f ? Mathf.Abs(Mathf.Sin(t * 14f)) * 22f : Mathf.Sin(t * 3.5f) * 5f;
        if (pEarL != null) pEarL.localRotation = Quaternion.Euler(0f, 0f, earFlop);
        if (pEarR != null) pEarR.localRotation = Quaternion.Euler(0f, 0f, -earFlop);

        if (pHead != null)
        {
            float headTilt = CurrentDetectiveSignal == DetectiveSignal.Chorar ? 18f : (CurrentDetectiveSignal == DetectiveSignal.Latir ? -14f : Mathf.Sin(t * 2.5f) * 4f);
            pHead.localRotation = Quaternion.Euler(headTilt, 0f, excited ? Mathf.Sin(t * 6f) * 8f : 0f);
        }

        float stride = speed > 0.25f ? Mathf.Sin(t * 16f) * 34f : 0f;
        if (pLegFL != null) pLegFL.localRotation = Quaternion.Euler(stride, 0f, 0f);
        if (pLegFR != null) pLegFR.localRotation = Quaternion.Euler(-stride, 0f, 0f);
        if (pLegBL != null) pLegBL.localRotation = Quaternion.Euler(-stride, 0f, 0f);
        if (pLegBR != null) pLegBR.localRotation = Quaternion.Euler(stride, 0f, 0f);
    }

    public void SetDetectiveAlert(Transform suspect, DetectiveSignal signal)
    {
        detectiveSuspect = suspect;
        CurrentDetectiveSignal = signal;
        if (detectiveHatProp != null)
            detectiveHatProp.gameObject.SetActive(suspect != null || signal == DetectiveSignal.FelizComemorando);
    }

    public void ClearDetectiveAlert()
    {
        detectiveSuspect = null;
        CurrentDetectiveSignal = DetectiveSignal.None;
        if (detectiveHatProp != null)
            detectiveHatProp.gameObject.SetActive(false);
    }

    public void TriggerDetectiveHappyVictory()
    {
        detectiveSuspect = null;
        CurrentDetectiveSignal = DetectiveSignal.FelizComemorando;
        detectiveHappyTimer = 4.0f;
        if (detectiveHatProp != null) detectiveHatProp.gameObject.SetActive(true);
        if (audioController != null) audioController.PlayPetSound(false);
        GirlPowerOrla3DJuice.PopScale(transform, 1.28f, 0.42f);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(transform.position, "♥ Detetive Pipoca está SUPER FELIZ!", new Color(1f, 0.84f, 0.36f), 2.4f);
    }

    public void SetBandanaVisible(bool visible)
    {
        HasBandanaEquipped = visible;
        if (bandanaProp != null) bandanaProp.gameObject.SetActive(visible);
        TriggerHappyBounce();
        GirlPowerOrla3DJuice.PopScale(transform, 1.24f, 0.32f);
    }

    public void ToggleFollow()
    {
        isFollowing = !isFollowing;
        sniffingActive = false;
        TriggerHappyBounce();
    }

    public void TriggerHappyBounce()
    {
        happyTimer = 0.85f;
    }

    /// <summary>Sends Pipoca ahead to sniff out a hidden ingredient or low-tide treasure!</summary>
    public Vector3 TriggerSniffTreasure()
    {
        isFollowing = true;
        sniffingActive = true;
        sniffTarget = HiddenSpots[sniffIndex % HiddenSpots.Length];
        sniffIndex++;
        if (audioController != null) audioController.PlayPetSound(isCat);
        GirlPowerOrla3DJuice.PopScale(transform, 1.20f, 0.28f);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(transform.position, petName + " farejou algo fresco!", new Color(1f, 0.88f, 0.42f));
        return sniffTarget;
    }

    /// <summary>Performs a charming trick (Sentar, Dar a Pata, Girar) to entertain clients and earn tips!</summary>
    public string PerformTrick()
    {
        trickMode = (trickMode + 1) % 3;
        trickTimer = 1.25f;
        if (audioController != null) audioController.PlayPetSound(isCat);
        GirlPowerOrla3DJuice.PopScale(transform, 1.22f, 0.30f);

        string name = trickMode == 0 ? "Sentou direitinho!" : (trickMode == 1 ? "Deu a patinha!" : "Girou 360°!");
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(transform.position, "★ " + petName + ": " + name, new Color(1f, 0.74f, 0.86f));
        return name;
    }

    public void ResetHome()
    {
        isFollowing = false;
        sniffingActive = false;
        transform.position = homePos;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time;

        // 0. Detective Pipoca Happy Victory Dance!
        if (detectiveHappyTimer > 0f)
        {
            detectiveHappyTimer = Mathf.Max(0f, detectiveHappyTimer - dt);
            Vector3 cp = transform.position;
            float baseY = SampleCompanionGroundY(cp.x, cp.z);
            float joyHop = Mathf.Abs(Mathf.Sin(t * 11f)) * 0.38f;
            transform.position = new Vector3(cp.x, baseY + joyHop, cp.z);
            transform.Rotate(0f, 340f * dt, 0f, Space.World);
            if (detectiveHappyTimer <= 0f) ClearDetectiveAlert();
            return;
        }

        // 1. Active Detective Mode: Pipoca rushes to the Street Thief & performs the 5 alert signals!
        if (!isCat && detectiveSuspect != null && detectiveSuspect.gameObject.activeInHierarchy)
        {
            detectiveAnimClock += dt;
            Vector3 goalPos = detectiveSuspect.position + new Vector3(-1.1f, 0f, -0.85f);
            Vector3 curPos = transform.position;
            Vector2 dXZ = new Vector2(goalPos.x - curPos.x, goalPos.z - curPos.z);
            float distToSuspect = dXZ.magnitude;

            if (distToSuspect > 0.75f)
            {
                Vector2 dir = dXZ / distToSuspect;
                float step = Mathf.Min(distToSuspect, followSpeed * 1.45f * dt);
                float nx = curPos.x + dir.x * step;
                float nz = curPos.z + dir.y * step;
                hopClock += dt * 16f;
                float baseY = SampleCompanionGroundY(nx, nz);
                transform.position = new Vector3(nx, baseY + Mathf.Abs(Mathf.Sin(hopClock)) * 0.20f, nz);
                Quaternion lookRot = Quaternion.LookRotation(-new Vector3(dir.x, 0f, dir.y).normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, dt * 12f);
            }
            else
            {
                Vector3 toThief = detectiveSuspect.position - curPos;
                toThief.y = 0f;
                Quaternion faceThief = toThief.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(-toThief.normalized, Vector3.up)
                    : transform.rotation;
                float baseY = SampleCompanionGroundY(curPos.x, curPos.z);

                switch (CurrentDetectiveSignal)
                {
                    case DetectiveSignal.Latir:
                        transform.position = new Vector3(curPos.x, baseY + Mathf.Abs(Mathf.Sin(t * 14f)) * 0.24f, curPos.z);
                        transform.rotation = Quaternion.Slerp(transform.rotation, faceThief, dt * 10f);
                        break;
                    case DetectiveSignal.Rolar:
                        transform.position = new Vector3(curPos.x, baseY + 0.14f, curPos.z);
                        transform.rotation = faceThief * Quaternion.Euler(0f, 0f, t * 480f);
                        break;
                    case DetectiveSignal.Morder:
                        float lunge = Mathf.Abs(Mathf.Sin(t * 10f)) * 0.42f;
                        Vector3 fwd = toThief.sqrMagnitude > 0.01f ? toThief.normalized : Vector3.forward;
                        transform.position = new Vector3(curPos.x, baseY + 0.08f, curPos.z) + fwd * (lunge * dt * 2.5f);
                        transform.rotation = faceThief * Quaternion.Euler(-16f, Mathf.Sin(t * 28f) * 14f, 0f);
                        break;
                    case DetectiveSignal.Chacalhar:
                        transform.position = new Vector3(curPos.x, baseY + 0.04f, curPos.z);
                        transform.rotation = faceThief * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 42f) * 28f);
                        break;
                    case DetectiveSignal.Chorar:
                        transform.position = new Vector3(curPos.x, baseY - 0.04f + Mathf.Sin(t * 18f) * 0.02f, curPos.z);
                        transform.rotation = faceThief * Quaternion.Euler(18f, 0f, Mathf.Sin(t * 14f) * 6f);
                        break;
                }
            }
            return;
        }

        // Passive thief defense when near cart/garden!
        if (!isCat && guardPoint != null)
        {
            guardCooldown -= dt;
            if (guardCooldown <= 0f)
            {
                float dGuard = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(guardPoint.position.x, guardPoint.position.z));
                if (dGuard < 5.5f && Random.value < 0.08f)
                {
                    guardCooldown = 26f;
                    TriggerHappyBounce();
                    if (audioController != null) audioController.PlayPetSound(false);
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(transform.position, "Au! " + petName + " espantou uma gaivota ladra!", new Color(0.62f, 0.96f, 0.86f));
                }
                else
                {
                    guardCooldown = 5f;
                }
            }
        }

        if (trickTimer > 0f)
        {
            trickTimer = Mathf.Max(0f, trickTimer - dt);
            Vector3 p = transform.position;
            float baseY = SampleCompanionGroundY(p.x, p.z);
            if (trickMode == 2) // Spin 360
                transform.Rotate(0f, 560f * dt, 0f, Space.World);
            else if (trickMode == 1) // Paw bounce
                transform.position = new Vector3(p.x, baseY + Mathf.Abs(Mathf.Sin(trickTimer * 12f)) * 0.24f, p.z);
            else // Sit squat
                transform.position = new Vector3(p.x, baseY - 0.06f, p.z);
            return;
        }

        if (happyTimer > 0f)
        {
            happyTimer = Mathf.Max(0f, happyTimer - dt);
            float h = Mathf.Abs(Mathf.Sin((0.85f - happyTimer) * 14f)) * 0.36f;
            Vector3 p = transform.position;
            p.y = SampleCompanionGroundY(p.x, p.z) + h;
            transform.position = p;
            return;
        }

        if (!isFollowing || followTarget == null)
        {
            Vector3 cur = transform.position;
            float baseY = SampleCompanionGroundY(cur.x, cur.z);
            float bob = Mathf.Abs(Mathf.Sin(t * (isCat ? 2.1f : 3.2f))) * 0.04f;
            transform.position = new Vector3(cur.x, baseY + bob, cur.z);
            return;
        }

        Vector3 goal = sniffingActive ? sniffTarget : (followTarget.position + followOffset);
        goal.z = Mathf.Min(goal.z, 9.2f);
        goal.x = Mathf.Clamp(goal.x, -22f, 44f);

        Vector3 current = transform.position;
        Vector2 deltaXZ = new Vector2(goal.x - current.x, goal.z - current.z);
        float dist = deltaXZ.magnitude;

        if (sniffingActive && dist < 0.85f)
        {
            sniffingActive = false;
            TriggerHappyBounce();
            if (audioController != null) audioController.PlayFanfare();
            GirlPowerOrla3DJuice.SpawnFlyingCoins(transform.position, 4);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(transform.position, "★ Tesouro achado! (+8 moedas + 1 Concha)", new Color(1f, 0.88f, 0.34f));
            var mega = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
            if (mega != null) mega.OnPipocaFoundTreasure();
            return;
        }

        if (dist > stopDistance * 0.55f)
        {
            Vector2 dir = deltaXZ / Mathf.Max(0.001f, dist);
            float spd = sniffingActive ? followSpeed * 1.25f : followSpeed;
            float step = Mathf.Min(dist, spd * dt);
            float nx = current.x + dir.x * step;
            float nz = current.z + dir.y * step;
            hopClock += dt * (isCat ? 10.5f : 13.5f);
            float hop = Mathf.Abs(Mathf.Sin(hopClock)) * (isCat ? 0.14f : 0.18f);
            float baseY = SampleCompanionGroundY(nx, nz);
            transform.position = new Vector3(nx, baseY + hop, nz);

            Vector3 look = new Vector3(dir.x, 0f, dir.y);
            if (look.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-look.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-10f * dt));
            }
        }
        else
        {
            float baseY = SampleCompanionGroundY(current.x, current.z);
            float idle = Mathf.Abs(Mathf.Sin(t * 3.0f)) * 0.035f;
            transform.position = Vector3.Lerp(current, new Vector3(current.x, baseY + idle, current.z), dt * 10f);
        }
    }

    private static float SampleCompanionGroundY(float x, float z)
    {
        if (x >= 12.6f && x <= 18.6f && z >= 2.6f && z <= 10.2f)
        {
            if (z < 3.4f) return Mathf.Lerp(0f, 0.36f, (z - 2.6f) / 0.8f);
            if (z <= 7.4f) return 0.36f;
            return Mathf.Lerp(0.36f, -0.24f, Mathf.Clamp01((z - 7.4f) / 2.2f));
        }
        if (z < -3.75f) return -0.08f;
        if (z <= 3.35f) return 0f;
        if (z < 4.10f) return Mathf.Sin(((z - 3.35f) / 0.75f) * Mathf.PI) * 0.48f;
        return Mathf.Lerp(-0.04f, -0.34f, Mathf.Clamp01((z - 4.10f) / 6.40f));
    }
}
