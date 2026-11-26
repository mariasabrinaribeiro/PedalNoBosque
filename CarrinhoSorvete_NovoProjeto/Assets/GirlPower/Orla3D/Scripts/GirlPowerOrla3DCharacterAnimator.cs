using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public float baseWalkSpeed = 4.0f;
    public float walkStepRate = 9.2f;
    public float runStepRate = 13.8f;

    private Animator cachedAnimator;
    private NavMeshAgent cachedAgent;

    public LandMode CurrentLandMode { get; private set; } = LandMode.Walk;
    public WaterMode CurrentWaterMode { get; private set; } = WaterMode.Swim;
    public bool IsInWater => inWater;
    public float WaterSurfaceY { get; set; } = -0.20f;
    public bool HasHatEquipped { get; private set; }
    /// <summary>Auto-detected local Z sign of the protagonist's face (+1 = Unity standard +Z forward).</summary>
    public float FaceForwardSign { get; private set; } = 1f;

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
    private float lastActualSpeed;

    // Articulated Blender 5.3 limb, torso & face joints (auto-discovered at Start!)
    private Transform jointTorso;
    private Transform jointHead;
    private Transform jointEyeL;
    private Transform jointEyeR;
    private Transform jointArmL;
    private Transform jointArmR;
    private Transform jointLegL;
    private Transform jointLegR;
    private Quaternion baseTorsoRot = Quaternion.identity;
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
    private Transform waterRippleA;
    private Transform waterRippleB;
    private Material waterRippleMatA;
    private Material waterRippleMatB;

    private void Start()
    {
        if (visualRoot == null) visualRoot = transform;
        // Disable Root Motion on all child Animators so movement is owned strictly by the controller (zero sliding!)
        GirlPowerOrla3DCharacterFacing.DisableRootMotion(transform);
        cachedAnimator = visualRoot.GetComponentInChildren<Animator>(true);
        cachedAgent = GetComponent<NavMeshAgent>();
        visualBaseLocalPos = visualRoot.localPosition;
        visualRoot.localRotation = Quaternion.Euler(0f, visualRoot.localEulerAngles.y, 0f);
        prevYaw = visualRoot.localEulerAngles.y;
        CacheArticulatedJoints();
        FaceForwardSign = GirlPowerOrla3DCharacterFacing.DetectFaceForwardSign(visualRoot, 1f);
        SetupProps();
        SetupVfxPool();
        RefreshEquippedProps();
    }

    private void CacheArticulatedJoints()
    {
        if (visualRoot == null) return;
        jointTorso = FindRecursive(visualRoot, "Protag_Torso");
        jointHead = FindRecursive(visualRoot, "Protag_Head");
        jointEyeL = FindRecursive(visualRoot, "Protag_EyeL");
        jointEyeR = FindRecursive(visualRoot, "Protag_EyeR");
        jointArmL = FindRecursive(visualRoot, "Protag_ArmL");
        jointArmR = FindRecursive(visualRoot, "Protag_ArmR");
        jointLegL = FindRecursive(visualRoot, "Protag_LegL");
        jointLegR = FindRecursive(visualRoot, "Protag_LegR");

        if (jointTorso != null) baseTorsoRot = jointTorso.localRotation;
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

        float armL = 0f, armR = 0f, legL = 0f, legR = 0f, headPitch = 0f, headYaw = 0f;
        float armLRoll = 0f, armRRoll = 0f;
        float torsoPitch = 0f, torsoYaw = 0f, torsoRoll = 0f;

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
                if (moving)
                {
                    // Energetic alternating freestyle swimming strokes + flutter kicks!
                    float stroke = Mathf.Sin(t * 8.4f);
                    float strokeCos = Mathf.Cos(t * 8.4f);
                    armL = -110f + stroke * 58f;
                    armR = -110f - stroke * 58f;
                    armLRoll = 28f + strokeCos * 18f;
                    armRRoll = -28f - strokeCos * 18f;
                    legL = stroke * 36f;
                    legR = -stroke * 36f;
                    torsoRoll = stroke * 12f;
                    headPitch = -36f; // Keep head & face lifted cleanly above water!
                }
                else
                {
                    // Gentle treading water with calm sculling arms & head up above waves
                    float tread = Mathf.Sin(t * 3.4f);
                    armL = -42f + tread * 18f;
                    armR = -42f - tread * 18f;
                    armLRoll = 38f + tread * 14f;
                    armRRoll = -38f - tread * 14f;
                    legL = tread * 20f;
                    legR = -tread * 20f;
                    headPitch = -8f;
                }
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
            // Scale stride swing smoothly by actual ground speed — calibrated for +15% longer legs (natural stride arc!)
            float speedScale = Mathf.Clamp01(lastActualSpeed / 1.35f);
            float amp = (running ? 46f : 31f) * speedScale;
            float s = Mathf.Sin(walkPhase);
            float frontSign = FaceForwardSign >= 0f ? -1f : 1f;
            legL = s * amp * frontSign;
            legR = -s * amp * frontSign;
            armL = -s * (amp * 0.82f) * frontSign;
            armR = holdingCone ? (-52f * frontSign) : (s * (amp * 0.82f) * frontSign);
            // Natural Secondary Motion: counter-rotating shoulders/torso opposite to leg swing + gentle weight shift!
            torsoYaw = -s * (running ? 11.5f : 7.5f) * speedScale;
            torsoRoll = s * (running ? 3.5f : 2.2f) * speedScale;
            torsoPitch = (running ? -5.5f : -2.0f) * frontSign * speedScale;
            headPitch = (running ? -4f : -1.5f) * frontSign * speedScale;
            headYaw = s * (running ? 5.0f : 3.2f) * speedScale;
        }
        else
        {
            float idle = Mathf.Sin(t * 2.4f) * 3.5f;
            armL = idle;
            armR = holdingCone ? -50f : -idle;
            torsoRoll = Mathf.Sin(t * 1.4f) * 1.2f;
            headPitch = Mathf.Sin(t * 1.6f) * 2.5f;
        }

        float k = 1f - Mathf.Exp(-14f * dt);
        if (jointTorso != null) jointTorso.localRotation = Quaternion.Slerp(jointTorso.localRotation, baseTorsoRot * Quaternion.Euler(torsoPitch, torsoYaw, torsoRoll), k);
        if (jointArmL != null) jointArmL.localRotation = Quaternion.Slerp(jointArmL.localRotation, baseArmLRot * Quaternion.Euler(armL, 0f, armLRoll), k);
        if (jointArmR != null) jointArmR.localRotation = Quaternion.Slerp(jointArmR.localRotation, baseArmRRot * Quaternion.Euler(armR, 0f, armRRoll), k);
        if (jointLegL != null) jointLegL.localRotation = Quaternion.Slerp(jointLegL.localRotation, baseLegLRot * Quaternion.Euler(legL, 0f, 0f), k);
        if (jointLegR != null) jointLegR.localRotation = Quaternion.Slerp(jointLegR.localRotation, baseLegRRot * Quaternion.Euler(legR, 0f, 0f), k);
        if (jointHead != null) jointHead.localRotation = Quaternion.Slerp(jointHead.localRotation, baseHeadRot * Quaternion.Euler(headPitch, headYaw, 0f), k);
    }

    private void SetupProps()
    {
        float fz = FaceForwardSign >= 0f ? 1f : -1f;
        // Calibrated for +18% longer legs & -7% refined head so carried Lemon+Basil cone reads unmistakably from every angle!
        AttachProp(heldIceCreamCone, new Vector3(0.36f, 0.94f, 0.27f * fz), Quaternion.Euler(14f * fz, 0f, -10f), 1.18f, holdingCone);
        AttachProp(surfboardProp, new Vector3(0f, 0.02f, 0f), Quaternion.identity, 1.05f, false);
        AttachProp(skateboardProp, new Vector3(0f, 0.01f, 0f), Quaternion.identity, 0.92f, false);
        AttachProp(scooterProp, new Vector3(0f, 0.01f, 0.08f * fz), Quaternion.identity, 0.90f, false);
        AttachProp(rollerSkatesProp, new Vector3(0f, 0.0f, 0f), Quaternion.identity, 0.95f, false);
        AttachProp(hawaiianCanoeProp, new Vector3(0f, -0.10f, 0f), Quaternion.identity, 0.85f, false);
        AttachProp(wardrobeHatProp, new Vector3(0f, 1.54f, 0f), Quaternion.identity, 0.86f, HasHatEquipped);
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

        CreateWaterRippleRings();
    }

    private void CreateWaterRippleRings()
    {
        if (vfxContainer == null) return;
        Shader ringSh = Shader.Find("GirlPower/OrlaWaterRipple");
        if (ringSh == null) ringSh = Shader.Find("Sprites/Default");
        if (ringSh == null) ringSh = Shader.Find("Unlit/Transparent");
        Texture2D ringTex = BuildWaterRingTexture(128);

        waterRippleA = BuildRippleQuad("Protagonista_Anel_Agua_1", ringSh, ringTex, out waterRippleMatA);
        waterRippleB = BuildRippleQuad("Protagonista_Anel_Agua_2", ringSh, ringTex, out waterRippleMatB);
        if (waterRippleA != null) waterRippleA.gameObject.SetActive(false);
        if (waterRippleB != null) waterRippleB.gameObject.SetActive(false);
    }

    private Transform BuildRippleQuad(string name, Shader sh, Texture2D tex, out Material mat)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        q.transform.SetParent(vfxContainer, false);
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Collider c = q.GetComponent<Collider>();
        if (c != null) Destroy(c);
        mat = new Material(sh) { mainTexture = tex, color = new Color(0.95f, 0.99f, 1.0f, 0.88f) };
        if (mat.HasProperty("_CurveStrength"))
            mat.SetFloat("_CurveStrength", 0.0008f);
        Renderer r = q.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q.transform;
    }

    private static Texture2D BuildWaterRingTexture(int sz)
    {
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[sz * sz];
        float half = sz * 0.5f;
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                // Crisp outer white foam ring + soft inner turquoise halo
                float outerRing = Mathf.Exp(-Mathf.Pow((r - 0.80f) / 0.10f, 2f));
                float innerRing = Mathf.Exp(-Mathf.Pow((r - 0.52f) / 0.08f, 2f)) * 0.55f;
                float a = Mathf.Clamp01(outerRing + innerRing) * (r <= 1.0f ? 1f : 0f);
                byte ba = (byte)Mathf.RoundToInt(a * 255f);
                px[y * sz + x] = new Color32(235, 252, 255, ba);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
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

        // Drive the gait strictly from real ground displacement (zero foot sliding when blocked or slowing!).
        lastActualSpeed = actualPlanarSpeed;
        if (cachedAgent != null && cachedAgent.enabled && cachedAgent.isOnNavMesh && cachedAgent.velocity.sqrMagnitude > 0.01f)
            lastActualSpeed = cachedAgent.velocity.magnitude;
        else if (lastActualSpeed <= 0.01f && hasPrevWorldPos && dt > 0.0001f)
            lastActualSpeed = new Vector2(worldPos.x - prevWorldPos.x, worldPos.z - prevWorldPos.z).magnitude / dt;

        bool moving = lastActualSpeed > 0.22f;
        UpdateArticulatedLimbs(moving, running, false, dt, t);

        if (inWater)
        {
            AnimateWaterModes(moveInput, moving, yaw, turnTilt, worldPos, t, dt);
            UpdateWaterRipples(worldPos, moving, t);
            UpdateVfx(dt);
            return;
        }
        if (waterRippleA != null && waterRippleA.gameObject.activeSelf) waterRippleA.gameObject.SetActive(false);
        if (waterRippleB != null && waterRippleB.gameObject.activeSelf) waterRippleB.gameObject.SetActive(false);

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
        float actualSpeed = lastActualSpeed;
        prevWorldPos = worldPos;
        hasPrevWorldPos = true;

        // Strictly synchronize Animator.speed = actualSpeed / baseWalkSpeed when an Animator is present!
        float walkSpeedRatio = actualSpeed / Mathf.Max(0.5f, baseWalkSpeed);
        if (cachedAnimator != null)
            cachedAnimator.speed = moving ? Mathf.Clamp(walkSpeedRatio, 0.35f, 2.25f) : 1.0f;

        if (moving && actualSpeed > 0.22f)
        {
            // Sync footstep cadence strictly to real ground displacement — calibrated for +15% longer legs (1.70m full stride!)
            float stepMeters = actualSpeed * dt;
            walkPhase += stepMeters * (Mathf.PI * 2f / 1.70f);
            float s = Mathf.Sin(walkPhase);
            float cadenceWeight = Mathf.Clamp01(actualSpeed / 3.2f);
            float hop = Mathf.Abs(s) * (running ? 0.135f : 0.085f) * cadenceWeight;
            float waddle = s * (running ? 4.2f : 2.6f) * cadenceWeight + turnTilt * 0.40f;
            float frontSign = FaceForwardSign >= 0f ? -1f : 1f;
            float forwardLean = (running ? -9.0f : -3.2f) * frontSign;

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

    private void UpdateWaterRipples(Vector3 worldPos, bool moving, float t)
    {
        if (waterRippleA == null || waterRippleB == null) return;
        if (!waterRippleA.gameObject.activeSelf) waterRippleA.gameObject.SetActive(true);
        if (!waterRippleB.gameObject.activeSelf) waterRippleB.gameObject.SetActive(true);

        float speed = moving ? 1.45f : 0.72f;
        float pA = Mathf.Repeat(t * speed, 1f);
        float pB = Mathf.Repeat(t * speed + 0.5f, 1f);

        float baseScale = CurrentWaterMode == WaterMode.Swim ? 0.85f : 1.15f;
        float maxScale = CurrentWaterMode == WaterMode.Swim ? (moving ? 2.35f : 1.85f) : (moving ? 2.85f : 2.15f);
        float ySurface = WaterSurfaceY + 0.03f;

        float scA = Mathf.Lerp(baseScale, maxScale, pA);
        float scB = Mathf.Lerp(baseScale, maxScale, pB);
        waterRippleA.position = new Vector3(worldPos.x, ySurface, worldPos.z);
        waterRippleB.position = new Vector3(worldPos.x, ySurface + 0.005f, worldPos.z);
        waterRippleA.localScale = new Vector3(scA, scA, 1f);
        waterRippleB.localScale = new Vector3(scB, scB, 1f);

        if (waterRippleMatA != null)
            waterRippleMatA.color = new Color(0.92f, 0.99f, 1.0f, (1f - pA) * (moving ? 0.88f : 0.68f));
        if (waterRippleMatB != null)
            waterRippleMatB.color = new Color(0.84f, 0.98f, 1.0f, (1f - pB) * (moving ? 0.88f : 0.68f));
    }

    private void AnimateWaterModes(Vector2 moveInput, bool moving, float yaw, float turnTilt, Vector3 worldPos, float t, float dt)
    {
        if (CurrentWaterMode == WaterMode.Swim)
        {
            // Chest-deep swimming: head, shoulders & stroking arms stay cleanly above the turquoise waves while waist/legs are submerged!
            float strokeRoll = moving ? Mathf.Sin(t * 8.4f) * 14f : Mathf.Sin(t * 2.6f) * 4f;
            float swimPitch = moving ? (FaceForwardSign >= 0f ? 42f : -42f) : (FaceForwardSign >= 0f ? 8f : -8f);
            float floatY = (moving ? -0.48f : -0.56f) + Mathf.Sin(t * 3.4f) * 0.05f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, floatY, 0f), dt * 9f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(swimPitch, yaw, strokeRoll), dt * 8f);
        }
        else if (CurrentWaterMode == WaterMode.Canoe)
        {
            // Paddling the Hawaiian Outrigger Canoe smoothly across the waves!
            float paddleRock = moving ? Mathf.Sin(t * 5.2f) * 7f : Mathf.Sin(t * 2.0f) * 2.5f;
            float waveBob = 0.20f + Mathf.Sin(t * 3.0f) * 0.09f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, waveBob, 0f), dt * 8f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(-2f, yaw, paddleRock + turnTilt * 0.5f), dt * 7f);
        }
        else
        {
            // Surfing on the tropical surfboard!
            float waveBob = Mathf.Sin(t * 3.8f) * 0.14f + 0.14f;
            float carveRoll = -moveInput.x * 16f + turnTilt * 0.7f + Mathf.Sin(t * 2.8f) * 4f;
            float pitch = -moveInput.y * 8f - 4f;
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPos + new Vector3(0f, waveBob, 0f), dt * 10f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, Quaternion.Euler(pitch, yaw, carveRoll), dt * 8f);
        }

        effectClock += dt;
        if (effectClock >= (moving ? 0.055f : 0.20f))
        {
            effectClock = 0f;
            Vector3 backDir = visualRoot != null ? -visualRoot.forward * FaceForwardSign : Vector3.back;
            Vector3 rightDir = visualRoot != null ? visualRoot.right : Vector3.right;
            float ySurf = WaterSurfaceY + 0.02f;
            // Clean white foamy wake right at the water surface behind the swimmer / surfboard / canoe!
            Vector3 wakeCenter = new Vector3(worldPos.x, ySurf, worldPos.z) + backDir * 0.42f;
            SpawnPuff(wakeCenter + rightDir * 0.24f, Vector3.zero, moving ? 0.26f : 0.16f, 0.40f, 0.95f, new Color(0.94f, 0.99f, 1.0f), true);
            SpawnPuff(wakeCenter - rightDir * 0.24f, Vector3.zero, moving ? 0.26f : 0.16f, 0.40f, 0.95f, new Color(0.94f, 0.99f, 1.0f), true);

            if (moving)
            {
                int drops = CurrentWaterMode == WaterMode.Swim ? 3 : 2;
                for (int d = 0; d < drops; d++)
                {
                    Vector3 sprayPos = new Vector3(worldPos.x, ySurf + 0.08f, worldPos.z) + backDir * 0.28f + rightDir * Random.Range(-0.28f, 0.28f);
                    Vector3 vel = new Vector3(Random.Range(-1.05f, 1.05f), Random.Range(0.75f, 1.55f), Random.Range(-1.05f, 1.05f));
                    SpawnPuff(sprayPos, vel, 0.11f, 0.20f, 0.40f, new Color(0.88f, 0.98f, 1.0f), false);
                }
                if (audioController != null && Random.value > 0.62f)
                    audioController.PlayFootstep(2);
            }
        }
    }

    private void OnFootstep(Vector3 pos, bool running, float sideOffset)
    {
        Vector3 footPos = pos + visualRoot.right * sideOffset + Vector3.up * 0.05f;
        bool inSurf = pos.z >= 7.8f;
        bool onBeach = pos.z >= 4.05f && pos.z < 7.8f;

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
