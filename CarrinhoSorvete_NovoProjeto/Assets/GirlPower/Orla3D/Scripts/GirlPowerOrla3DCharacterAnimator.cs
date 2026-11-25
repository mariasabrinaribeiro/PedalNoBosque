using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Expressive 3D Protagonist Animator supporting ALL land & water sports and wardrobe props:
/// - Land Mobility: Walk, Skateboard, Scooter (Patinete), Roller Skates (Patins no Pé)
/// - Water Sports: Surfboard, Swimming (Nadar), Hawaiian Outrigger Canoe (Canoa Havaiana)
/// - Wardrobe: Dynamic outfit colors, Straw Sun Hat & Sunglasses prop
/// - Juice: Turn banking/tilt, running dust clouds, wet sand footprints, water splashes & sparkle trails!
/// </summary>
public sealed class GirlPowerOrla3DCharacterAnimator : MonoBehaviour
{
    public enum LandMode { Walk = 0, Skate = 1, Scooter = 2, RollerSkates = 3 }
    public enum WaterMode { Surf = 0, Swim = 1, Canoe = 2 }

    [Header("Visuals & Blender 3D Props")]
    public Transform visualRoot;
    public Transform heldIceCreamCone;
    public Transform surfboardProp;
    public Transform skateboardProp;
    public Transform scooterProp;
    public Transform rollerSkatesProp;
    public Transform hawaiianCanoeProp;
    public Transform wardrobeHatProp;
    public GirlPowerOrla3DAudio audioController;

    [Header("Feel")]
    public float walkStepRate = 9.2f;
    public float runStepRate = 13.8f;

    public LandMode CurrentLandMode { get; private set; } = LandMode.Walk;
    public WaterMode CurrentWaterMode { get; private set; } = WaterMode.Surf;
    public bool IsInWater => inWater;
    public bool HasHatEquipped { get; private set; }

    private Vector3 visualBaseLocalPos;
    private float walkPhase;
    private int lastStepHalf = -1;
    private float celebrateTimer;
    private bool holdingCone;
    private bool inWater;
    private float effectClock;
    private float prevYaw;
    private Vector3 prevWorldPos;
    private bool hasPrevWorldPos;

    // Articulated Blender 5.3 limb & face joints (auto-discovered at Start!)
    private Transform jointHead;
    private Transform jointEyeL;
    private Transform jointEyeR;
    private Transform jointArmL;
    private Transform jointArmR;
    private Transform jointLegL;
    private Transform jointLegR;
    private Quaternion baseHeadRot = Quaternion.identity;
    private Quaternion baseArmLRot = Quaternion.identity;
    private Quaternion baseArmRRot = Quaternion.identity;
    private Quaternion baseLegLRot = Quaternion.identity;
    private Quaternion baseLegRRot = Quaternion.identity;
    private float blinkClock = 2.4f;

    private sealed class VfxItem
    {
        public GameObject go;
        public Renderer rend;
        public Vector3 vel;
        public float age;
        public float lifetime;
        public float startScale;
        public float endScale;
        public bool isFootprint;
    }

    private readonly List<VfxItem> activeVfx = new List<VfxItem>(64);
    private readonly Stack<VfxItem> vfxPool = new Stack<VfxItem>(64);
    private Material puffMat;
    private Transform vfxContainer;

    private void Start()
    {
        if (visualRoot == null) visualRoot = transform;
        visualBaseLocalPos = visualRoot.localPosition;
        visualRoot.localRotation = Quaternion.Euler(0f, visualRoot.localEulerAngles.y, 0f);
        prevYaw = visualRoot.localEulerAngles.y;
        SetupProps();
        CacheArticulatedJoints();
        SetupVfxPool();
        RefreshEquippedProps();
    }

    private void CacheArticulatedJoints()
    {
        if (visualRoot == null) return;
        jointHead = FindRecursive(visualRoot, "Protag_Head");
        jointEyeL = FindRecursive(visualRoot, "Protag_EyeL");
        jointEyeR = FindRecursive(visualRoot, "Protag_EyeR");
        jointArmL = FindRecursive(visualRoot, "Protag_ArmL");
        jointArmR = FindRecursive(visualRoot, "Protag_ArmR");
        jointLegL = FindRecursive(visualRoot, "Protag_LegL");
        jointLegR = FindRecursive(visualRoot, "Protag_LegR");

        if (jointHead != null)
        {
            if (jointEyeL != null && jointEyeL.parent != jointHead) jointEyeL.SetParent(jointHead, true);
            if (jointEyeR != null && jointEyeR.parent != jointHead) jointEyeR.SetParent(jointHead, true);
            baseHeadRot = jointHead.localRotation;
        }
        if (jointArmL != null) baseArmLRot = jointArmL.localRotation;
        if (jointArmR != null) baseArmRRot = jointArmR.localRotation;
        if (jointLegL != null) baseLegLRot = jointLegL.localRotation;
        if (jointLegR != null) baseLegRRot = jointLegR.localRotation;
    }

    private static Transform FindRecursive(Transform root, string targetName)
    {
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindRecursive(root.GetChild(i), targetName);
            if (hit != null) return hit;
        }
        return null;
    }

    private void UpdateArticulatedLimbs(bool moving, bool running, bool celebrating, float dt, float t)
    {
        // Natural eye blink every ~3.2-4.6 seconds
        if (jointEyeL != null && jointEyeR != null)
        {
            blinkClock -= dt;
            float eyeScaleY = 1f;
            if (blinkClock <= 0f)
            {
                eyeScaleY = 0.12f;
                if (blinkClock <= -0.11f)
                    blinkClock = Random.Range(3.0f, 4.8f);
            }
            Vector3 es = new Vector3(1f, eyeScaleY, 1f);
            jointEyeL.localScale = es;
            jointEyeR.localScale = es;
        }

        float armL = 0f, armR = 0f, legL = 0f, legR = 0f, headPitch = 0f;
        float armLRoll = 0f, armRRoll = 0f;

        if (celebrating)
        {
            float cheer = Mathf.Sin(t * 14f) * 14f;
            armL = -135f + cheer;
            armR = -135f - cheer;
            armLRoll = 22f;
            armRRoll = -22f;
            headPitch = -8f;
        }
        else if (inWater)
        {
            if (CurrentWaterMode == WaterMode.Swim)
            {
                float s = moving ? Mathf.Sin(t * 7.5f) : Mathf.Sin(t * 2.5f) * 0.35f;
                armL = -70f + s * 42f;
                armR = -70f - s * 42f;
                legL = s * 28f;
                legR = -s * 28f;
            }
            else if (CurrentWaterMode == WaterMode.Canoe)
            {
                float p = moving ? Mathf.Sin(t * 5.2f) : Mathf.Sin(t * 2.0f) * 0.2f;
                armL = -48f + p * 26f;
                armR = -48f - p * 26f;
            }
            else
            {
                // Surf balance arms out!
                float bal = Mathf.Sin(t * 3.8f) * 10f;
                armLRoll = 58f + bal;
                armRRoll = -58f + bal;
            }
        }
        else if (CurrentLandMode != LandMode.Walk)
        {
            float kickCycle = moving ? Mathf.Repeat(t * 0.85f, 1f) : 0f;
            float kick = (moving && CurrentLandMode != LandMode.RollerSkates && kickCycle < 0.28f)
                ? Mathf.Sin((kickCycle / 0.28f) * Mathf.PI) * 38f
                : 0f;
            if (CurrentLandMode == LandMode.Scooter)
            {
                armL = -46f;
                armR = -46f;
                legR = kick;
            }
            else if (CurrentLandMode == LandMode.Skate)
            {
                armLRoll = 34f;
                armRRoll = -34f;
                legR = kick;
            }
            else
            {
                float gl = moving ? Mathf.Sin(t * 9.5f) * 28f : 0f;
                armL = gl;
                armR = -gl;
                legL = -gl;
                legR = gl;
            }
        }
        else if (moving)
        {
            float amp = running ? 42f : 26f;
            float s = Mathf.Sin(walkPhase);
            legL = s * amp;
            legR = -s * amp;
            armL = -s * (amp * 0.85f);
            armR = holdingCone ? -52f : (s * (amp * 0.85f));
            headPitch = running ? -5f : -2f;
        }
        else
        {
            float idle = Mathf.Sin(t * 2.4f) * 3.5f;
            armL = idle;
            armR = holdingCone ? -50f : -idle;
            headPitch = Mathf.Sin(t * 1.6f) * 2.5f;
        }

        float k = 1f - Mathf.Exp(-14f * dt);
        if (jointArmL != null) jointArmL.localRotation = Quaternion.Slerp(jointArmL.localRotation, baseArmLRot * Quaternion.Euler(armL, 0f, armLRoll), k);
        if (jointArmR != null) jointArmR.localRotation = Quaternion.Slerp(jointArmR.localRotation, baseArmRRot * Quaternion.Euler(armR, 0f, armRRoll), k);
        if (jointLegL != null) jointLegL.localRotation = Quaternion.Slerp(jointLegL.localRotation, baseLegLRot * Quaternion.Euler(legL, 0f, 0f), k);
        if (jointLegR != null) jointLegR.localRotation = Quaternion.Slerp(jointLegR.localRotation, baseLegRRot * Quaternion.Euler(legR, 0f, 0f), k);
        if (jointHead != null) jointHead.localRotation = Quaternion.Slerp(jointHead.localRotation, baseHeadRot * Quaternion.Euler(headPitch, 0f, 0f), k);
    }

    private void SetupProps()
    {
        AttachProp(heldIceCreamCone, new Vector3(0.36f, 0.72f, -0.22f), Quaternion.Euler(-12f, 0f, -10f), 1.05f, holdingCone);
        AttachProp(surfboardProp, new Vector3(0f, 0.02f, 0f), Quaternion.identity, 1.05f, false);
        AttachProp(skateboardProp, new Vector3(0f, 0.01f, 0f), Quaternion.identity, 0.92f, false);
        AttachProp(scooterProp, new Vector3(0f, 0.01f, -0.08f), Quaternion.identity, 0.90f, false);
        AttachProp(rollerSkatesProp, new Vector3(0f, 0.0f, 0f), Quaternion.identity, 0.95f, false);
        AttachProp(hawaiianCanoeProp, new Vector3(0f, -0.10f, 0f), Quaternion.identity, 0.85f, false);
        AttachProp(wardrobeHatProp, new Vector3(0f, 1.38f, 0f), Quaternion.identity, 0.92f, HasHatEquipped);
    }

    private void AttachProp(Transform prop, Vector3 pos, Quaternion rot, float scale, bool active)
    {
        if (prop == null) return;
        prop.SetParent(visualRoot, false);
        prop.localPosition = pos;
        prop.localRotation = rot;
        prop.localScale = Vector3.one * scale;
        prop.gameObject.SetActive(active);
    }

    private void SetupVfxPool()
    {
        GameObject containerObj = new GameObject("Protagonista_VFX_Pool");
        vfxContainer = containerObj.transform;
        Shader sh = Shader.Find("GirlPower/OrlaACNHCozy");
        if (sh == null) sh = Shader.Find("Standard");
        puffMat = new Material(sh);
        if (puffMat.HasProperty("_ShadowColor"))
            puffMat.SetColor("_ShadowColor", new Color(0.55f, 0.52f, 0.68f, 1f));
        if (puffMat.HasProperty("_CurveStrength"))
            puffMat.SetFloat("_CurveStrength", 0.0008f);
    }

    public void SetHoldingCone(bool holding)
    {
        holdingCone = holding;
        if (heldIceCreamCone != null)
            heldIceCreamCone.gameObject.SetActive(holding);
    }

    public void SetSurfing(bool isInOcean)
    {
        if (inWater == isInOcean) return;
        inWater = isInOcean;
        RefreshEquippedProps();
    }

    public void CycleLandMode()
    {
        CurrentLandMode = (LandMode)(((int)CurrentLandMode + 1) % 4);
        RefreshEquippedProps();
        GirlPowerOrla3DJuice.PopScale(visualRoot, 1.16f, 0.25f);
    }

    public void SetLandMode(LandMode mode)
    {
        CurrentLandMode = mode;
        RefreshEquippedProps();
        GirlPowerOrla3DJuice.PopScale(visualRoot, 1.16f, 0.25f);
    }

    public void CycleWaterMode()
    {
        CurrentWaterMode = (WaterMode)(((int)CurrentWaterMode + 1) % 3);
        RefreshEquippedProps();
        GirlPowerOrla3DJuice.PopScale(visualRoot, 1.16f, 0.25f);
    }

    public void SetWaterMode(WaterMode mode)
    {
        CurrentWaterMode = mode;
        RefreshEquippedProps();
    }

    public float GetMobilitySpeedMultiplier()
    {
        if (inWater)
        {
            switch (CurrentWaterMode)
            {
                case WaterMode.Swim: return 0.68f;
                case WaterMode.Canoe: return 1.18f;
                default: return 1.0f;
            }
        }
        switch (CurrentLandMode)
        {
            case LandMode.Skate: return 1.42f;
            case LandMode.Scooter: return 1.36f;
            case LandMode.RollerSkates: return 1.30f;
            default: return 1.0f;
        }
    }

    public string GetActiveMobilityName()
    {
        if (inWater)
        {
            switch (CurrentWaterMode)
            {
                case WaterMode.Swim: return "Nadando";
                case WaterMode.Canoe: return "Canoa Havaiana";
                default: return "Surfando";
            }
        }
        switch (CurrentLandMode)
        {
            case LandMode.Skate: return "Skate";
            case LandMode.Scooter: return "Patinete";
            case LandMode.RollerSkates: return "Patins";
            default: return "A Pé";
        }
    }

    private void RefreshEquippedProps()
    {
        if (skateboardProp != null) skateboardProp.gameObject.SetActive(!inWater && CurrentLandMode == LandMode.Skate);
        if (scooterProp != null) scooterProp.gameObject.SetActive(!inWater && CurrentLandMode == LandMode.Scooter);
        if (rollerSkatesProp != null) rollerSkatesProp.gameObject.SetActive(!inWater && CurrentLandMode == LandMode.RollerSkates);
        if (surfboardProp != null) surfboardProp.gameObject.SetActive(inWater && CurrentWaterMode == WaterMode.Surf);
        if (hawaiianCanoeProp != null) hawaiianCanoeProp.gameObject.SetActive(inWater && CurrentWaterMode == WaterMode.Canoe);
    }

    public void SetWardrobeHatVisible(bool visible)
    {
        HasHatEquipped = visible;
        if (wardrobeHatProp != null)
            wardrobeHatProp.gameObject.SetActive(visible);
        GirlPowerOrla3DJuice.PopScale(visualRoot, 1.15f, 0.28f);
    }

    public void SetOutfitColor(Color color)
    {
        if (visualRoot == null) return;
        foreach (MeshRenderer mr in visualRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (IsPropChild(mr.transform)) continue;
            Material[] mats = mr.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && (mats[i].name.StartsWith("AC_ProtagShirt") || mats[i].name.StartsWith("AC_PinkBerry")))
                    mats[i].SetColor("_Color", color);
            }
            mr.materials = mats;
        }
        TriggerCelebration();
    }

    private bool IsPropChild(Transform t)
    {
        return (surfboardProp != null && t.IsChildOf(surfboardProp))
            || (skateboardProp != null && t.IsChildOf(skateboardProp))
            || (scooterProp != null && t.IsChildOf(scooterProp))
            || (rollerSkatesProp != null && t.IsChildOf(rollerSkatesProp))
            || (hawaiianCanoeProp != null && t.IsChildOf(hawaiianCanoeProp))
            || (wardrobeHatProp != null && t.IsChildOf(wardrobeHatProp))
            || (heldIceCreamCone != null && t.IsChildOf(heldIceCreamCone));
    }

    public void TriggerCelebration()
    {
        celebrateTimer = 1.05f;
        SpawnCelebrationSparkles(transform.position + Vector3.up * 1.1f);
        GirlPowerOrla3DJuice.PopScale(visualRoot, 1.18f, 0.34f);
    }

    public void SpawnHeartBurst(Vector3 pos)
    {
        Color[] hearts = { new Color(1.0f, 0.42f, 0.62f), new Color(1.0f, 0.72f, 0.84f), new Color(1.0f, 0.86f, 0.42f) };
        for (int i = 0; i < 8; i++)
        {
            float ang = (i / 8f) * Mathf.PI * 2f;
            Vector3 vel = new Vector3(Mathf.Cos(ang) * 1.1f, Random.Range(1.6f, 2.5f), Mathf.Sin(ang) * 1.1f);
            SpawnPuff(pos + Vector3.up * 1.1f, vel, 0.14f, 0.03f, 0.72f, hearts[i % hearts.Length], false);
        }
    }

    public void SpawnLeafBurst(Vector3 pos)
    {
        Color[] cols = { new Color(0.38f, 0.84f, 0.44f), new Color(0.62f, 0.94f, 0.56f), new Color(1.0f, 0.88f, 0.38f) };
        for (int i = 0; i < 9; i++)
        {
            float ang = (i / 9f) * Mathf.PI * 2f;
            Vector3 vel = new Vector3(Mathf.Cos(ang) * 1.3f, Random.Range(1.5f, 2.6f), Mathf.Sin(ang) * 1.3f);
            SpawnPuff(pos + Vector3.up * 0.75f, vel, 0.13f, 0.02f, 0.65f, cols[i % cols.Length], false);
        }
    }

    public void UpdateAnimation(Vector2 moveInput, bool running, bool talking, Vector3 worldPos, float actualPlanarSpeed)
    {
        float dt = Time.deltaTime;
        float t = Time.time;
        float yaw = visualRoot.localEulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(prevYaw, yaw);
        prevYaw = yaw;
        float turnTilt = Mathf.Clamp(-yawDelta * 1.6f, -15f, 15f);

        if (celebrateTimer > 0f)
        {
            celebrateTimer = Mathf.Max(0f, celebrateTimer - dt);
            float p = 1f - (celebrateTimer / 1.05f);
            float h = Mathf.Max(0f, Mathf.Sin(p * Mathf.PI * 2f)) * 0.42f;
            visualRoot.localPosition = visualBaseLocalPos + new Vector3(0f, h, 0f);
            visualRoot.localRotation = Quaternion.Euler(0f, yaw, Mathf.Sin(p * Mathf.PI * 6f) * 9f);
            UpdateArticulatedLimbs(false, false, true, dt, t);
            UpdateVfx(dt);
            return;
        }

        // Drive the gait from displacement, not input. This prevents fast feet while accelerating,
        // sliding against obstacles, or releasing the joystick.
        float referenceSpeed = running ? 6.4f : 4.0f;
        float speed01 = Mathf.Clamp01(actualPlanarSpeed / referenceSpeed);
        bool moving = actualPlanarSpeed > 0.12f;
        UpdateArticulatedLimbs(moving, running, false, dt, t);

        if (inWater)
        {
            AnimateWaterModes(moveInput, moving, yaw, turnTilt, worldPos, t, dt);
            UpdateVfx(dt);
            return;
        }

        // Wheeled land sports (Skateboard, Scooter, RollerSkates) with push-off kick, turn banking & wheel particles!
        if (CurrentLandMode != LandMode.Walk)
        {
            float lift = CurrentLandMode == LandMode.RollerSkates ? 0.08f : 0.14f;
            float speedMag = Mathf.Clamp01(moveInput.magnitude);
            // Rhythmic foot push-off kick every ~1.25s while accelerating on Scooter or Skateboard!
            float kickCycle = moving ? Mathf.Repeat(t * 0.85f, 1f) : 0f;
            float kickPulse = (moving && CurrentLandMode != LandMode.RollerSkates && kickCycle < 0.28f)
                ? Mathf.Sin((kickCycle / 0.28f) * Mathf.PI)
                : 0f;

            float stride = moving
                ? (CurrentLandMode == LandMode.RollerSkates
                    ? Mathf.Sin(t * 10.5f) * 11f
                    : kickPulse * 8.5f + Mathf.Sin(t * 5.2f) * 2.5f)
                : 0f;
            float bob = moving
                ? (kickPulse * 0.085f + Mathf.Abs(Mathf.Sin(t * 6.5f)) * 0.03f)
                : Mathf.Sin(t * 2.4f) * 0.02f;
            float leanPitch = moving ? (-5.5f * speedMag - kickPulse * 7.5f - (running ? 4f : 0f)) : 0f;
            float carveBank = Mathf.Clamp(turnTilt * 1.45f, -20f, 20f);

            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, lift + bob, 0f), dt * 12f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(leanPitch, yaw, carveBank + stride), dt * 11f);

            // Subtle dynamic handlebar/board banking lean on the prop itself!
            if (scooterProp != null && scooterProp.gameObject.activeSelf)
                scooterProp.localRotation = Quaternion.Slerp(scooterProp.localRotation, Quaternion.Euler(kickPulse * -4f, Mathf.Clamp(-turnTilt * 0.9f, -18f, 18f), carveBank * 0.45f), dt * 12f);
            if (skateboardProp != null && skateboardProp.gameObject.activeSelf)
                skateboardProp.localRotation = Quaternion.Slerp(skateboardProp.localRotation, Quaternion.Euler(kickPulse * -6f, 0f, carveBank * 0.55f), dt * 12f);

            if (moving)
            {
                effectClock += dt;
                if (effectClock >= (running ? 0.08f : 0.14f))
                {
                    effectClock = 0f;
                    if (audioController != null) audioController.PlayWheelGlide();
                    bool onSand = worldPos.z >= 3.95f;
                    Color trailCol = onSand
                        ? new Color(0.92f, 0.80f, 0.58f)
                        : (CurrentLandMode == LandMode.RollerSkates
                            ? new Color(1.0f, 0.80f, 0.94f)
                            : (CurrentLandMode == LandMode.Scooter ? new Color(0.72f, 0.96f, 0.90f) : new Color(1.0f, 0.92f, 0.72f)));
                    Vector3 vel = new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(0.45f, 1.05f), Random.Range(-0.55f, 0.55f));
                    SpawnPuff(worldPos + Vector3.up * 0.08f, vel, 0.11f, 0.24f, 0.40f, trailCol, false);
                }
            }
            UpdateVfx(dt);
            return;
        }

        // Ground-speed synchronized walk/run cycle (eliminates foot sliding!) + vertical bobbing & forward sprint lean!
        float actualSpeed = 0f;
        if (hasPrevWorldPos && dt > 0.0001f)
            actualSpeed = new Vector2(worldPos.x - prevWorldPos.x, worldPos.z - prevWorldPos.z).magnitude / dt;
        prevWorldPos = worldPos;
        hasPrevWorldPos = true;

        if (moving && actualSpeed > 0.12f)
        {
            // Sync footstep cadence strictly to real ground displacement (2.55 rad per meter walked!)
            float syncRate = Mathf.Clamp(actualSpeed * 2.55f, 4.8f, 17.5f);
            walkPhase += dt * syncRate;
            float s = Mathf.Sin(walkPhase);
            float cadenceWeight = Mathf.Clamp01(actualSpeed / 3.2f);
            float hop = Mathf.Abs(s) * (running ? 0.135f : 0.085f) * cadenceWeight;
            float waddle = s * (running ? 4.2f : 2.6f) * cadenceWeight + turnTilt * 0.40f;
            float forwardLean = running ? -9.0f : -3.2f;

            float poseBlend = 1f - Mathf.Exp(-15f * dt);
            Vector3 walkPose = visualBaseLocalPos + new Vector3(0f, hop, 0f);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, walkPose, poseBlend);
            Quaternion walkRotation = Quaternion.Euler(forwardLean * cadenceWeight, yaw, waddle);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, walkRotation, poseBlend);

            int stepHalf = Mathf.FloorToInt(walkPhase / Mathf.PI);
            if (stepHalf != lastStepHalf)
            {
                lastStepHalf = stepHalf;
                OnFootstep(worldPos, running, (stepHalf % 2 == 0) ? -0.14f : 0.14f);
            }
        }
        else
        {
            float breathe = Mathf.Sin(t * 2.5f) * 0.025f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, breathe, 0f), dt * 10f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(0f, yaw, 0f), dt * 10f);
        }

        UpdateVfx(dt);
    }

    private void AnimateWaterModes(Vector2 moveInput, bool moving, float yaw, float turnTilt, Vector3 worldPos, float t, float dt)
    {
        if (CurrentWaterMode == WaterMode.Swim)
        {
            // Swimming low in the turquoise water with gentle breaststroke bob & rings!
            float stroke = moving ? Mathf.Sin(t * 7.5f) * 12f : Mathf.Sin(t * 2.4f) * 3f;
            float floatY = -0.34f + Mathf.Sin(t * 3.2f) * 0.06f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, floatY, 0f), dt * 8f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(moving ? 22f : 4f, yaw, stroke), dt * 8f);
        }
        else if (CurrentWaterMode == WaterMode.Canoe)
        {
            // Paddling the Hawaiian Outrigger Canoe smoothly across the waves!
            float paddleRock = moving ? Mathf.Sin(t * 5.2f) * 7f : Mathf.Sin(t * 2.0f) * 2.5f;
            float waveBob = 0.22f + Mathf.Sin(t * 3.0f) * 0.09f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, waveBob, 0f), dt * 8f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(-2f, yaw, paddleRock + turnTilt * 0.5f), dt * 7f);
        }
        else
        {
            // Surfing on the tropical surfboard!
            float waveBob = Mathf.Sin(t * 3.8f) * 0.14f + 0.14f;
            float carveRoll = -moveInput.x * 16f + turnTilt * 0.7f + Mathf.Sin(t * 2.8f) * 4f;
            float pitch = -moveInput.y * 8f - 4f;
            visualRoot.localPosition = visualBaseLocalPos + new Vector3(0f, waveBob, 0f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(pitch, yaw, carveRoll), dt * 8f);
        }

        effectClock += dt;
        if (effectClock >= (moving ? 0.065f : 0.22f))
        {
            effectClock = 0f;
            Vector3 backDir = visualRoot != null ? visualRoot.forward : Vector3.back;
            Vector3 rightDir = visualRoot != null ? visualRoot.right : Vector3.right;
            // Clean white wake ribbon right behind the surfboard / canoe so height & shore direction read clearly!
            Vector3 wakeCenter = new Vector3(worldPos.x, -0.25f, worldPos.z) + backDir * 0.45f;
            SpawnPuff(wakeCenter + rightDir * 0.22f, Vector3.zero, moving ? 0.24f : 0.16f, 0.36f, 0.95f, new Color(0.93f, 0.99f, 1.0f), true);
            SpawnPuff(wakeCenter - rightDir * 0.22f, Vector3.zero, moving ? 0.24f : 0.16f, 0.36f, 0.95f, new Color(0.93f, 0.99f, 1.0f), true);

            if (moving)
            {
                Vector3 sprayPos = worldPos + Vector3.up * 0.06f + backDir * 0.35f;
                Vector3 vel = new Vector3(Random.Range(-0.85f, 0.85f), Random.Range(0.55f, 1.25f), Random.Range(-0.85f, 0.85f));
                SpawnPuff(sprayPos, vel, 0.12f, 0.22f, 0.38f, new Color(0.95f, 0.99f, 1.0f), false);
                if (audioController != null && Random.value > 0.68f)
                    audioController.PlayFootstep(2);
            }
        }
    }

    private void OnFootstep(Vector3 pos, bool running, float sideOffset)
    {
        Vector3 footPos = pos + visualRoot.right * sideOffset + Vector3.up * 0.05f;
        bool inSurf = pos.z >= 9.5f;
        bool onBeach = pos.z >= 4.05f && pos.z < 9.5f;

        if (audioController != null)
        {
            if (inSurf) audioController.PlayFootstep(2);
            else if (onBeach) audioController.PlayFootstep(1);
            else audioController.PlayFootstep(0);
        }

        if (inSurf)
        {
            int drops = running ? 4 : 2;
            for (int i = 0; i < drops; i++)
            {
                Vector3 vel = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(1.4f, 2.5f), Random.Range(-1.1f, 1.1f));
                SpawnPuff(footPos, vel, 0.09f, 0.17f, 0.36f, new Color(0.82f, 0.98f, 1.0f), false);
            }
        }
        else if (onBeach)
        {
            SpawnPuff(new Vector3(footPos.x, pos.y + 0.02f, footPos.z), Vector3.zero, 0.17f, 0.14f, 2.6f, new Color(0.72f, 0.56f, 0.38f), true);
            if (running || Random.value > 0.35f)
            {
                Vector3 vel = new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(0.7f, 1.3f), Random.Range(-0.55f, 0.55f));
                SpawnPuff(footPos, vel, 0.11f, 0.24f, 0.40f, new Color(0.95f, 0.84f, 0.64f), false);
            }
        }
        else if (running)
        {
            Vector3 vel = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(0.65f, 1.2f), Random.Range(-0.45f, 0.45f));
            SpawnPuff(footPos, vel, 0.13f, 0.30f, 0.40f, new Color(0.98f, 0.94f, 0.86f), false);
        }
    }

    private void SpawnCelebrationSparkles(Vector3 center)
    {
        Color[] colors =
        {
            new Color(1.0f, 0.86f, 0.38f),
            new Color(1.0f, 0.52f, 0.72f),
            new Color(0.45f, 0.92f, 0.86f),
            new Color(1.0f, 0.98f, 0.88f)
        };
        for (int i = 0; i < 12; i++)
        {
            float ang = (i / 12f) * Mathf.PI * 2f;
            Vector3 vel = new Vector3(Mathf.Cos(ang) * 1.7f, Random.Range(1.8f, 3.0f), Mathf.Sin(ang) * 1.7f);
            SpawnPuff(center, vel, 0.16f, 0.02f, 0.75f, colors[i % colors.Length], false);
        }
    }

    private void SpawnPuff(Vector3 pos, Vector3 vel, float startScale, float endScale, float lifetime, Color color, bool isFootprint)
    {
        VfxItem item = vfxPool.Count > 0 ? vfxPool.Pop() : CreateVfxItem();
        item.go.SetActive(true);
        item.go.transform.position = pos;
        item.go.transform.localScale = isFootprint
            ? new Vector3(startScale, 0.025f, startScale * 1.25f)
            : Vector3.one * startScale;
        item.vel = vel;
        item.age = 0f;
        item.lifetime = lifetime;
        item.startScale = startScale;
        item.endScale = endScale;
        item.isFootprint = isFootprint;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", color);
        item.rend.SetPropertyBlock(mpb);
        activeVfx.Add(item);
    }

    private VfxItem CreateVfxItem()
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "FootstepPuff";
        s.transform.SetParent(vfxContainer, false);
        Collider col = s.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Renderer r = s.GetComponent<Renderer>();
        r.sharedMaterial = puffMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return new VfxItem { go = s, rend = r };
    }

    private void UpdateVfx(float dt)
    {
        for (int i = activeVfx.Count - 1; i >= 0; i--)
        {
            VfxItem item = activeVfx[i];
            item.age += dt;
            if (item.age >= item.lifetime)
            {
                item.go.SetActive(false);
                vfxPool.Push(item);
                activeVfx.RemoveAt(i);
                continue;
            }

            float t = item.age / item.lifetime;
            if (!item.isFootprint)
            {
                item.vel.y -= 3.2f * dt;
                item.go.transform.position += item.vel * dt;
                float sc = Mathf.Lerp(item.startScale, item.endScale, t) * (1f - t * 0.65f);
                item.go.transform.localScale = Vector3.one * Mathf.Max(0.01f, sc);
            }
            else
            {
                float fade = 1f - Mathf.Clamp01((t - 0.55f) / 0.45f);
                float sc = item.startScale * fade;
                item.go.transform.localScale = new Vector3(sc, 0.02f, sc * 1.25f);
            }
        }
    }
}
