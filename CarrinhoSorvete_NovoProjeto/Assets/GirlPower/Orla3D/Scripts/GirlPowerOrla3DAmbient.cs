using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Brings Baía do Avesso to life with balanced cozy lighting, coastal wildlife,
/// bobbing anchored boats & buoys, falling leaves, ocean tide cycle, and time presets.
/// </summary>
public sealed class GirlPowerOrla3DAmbient : MonoBehaviour
{
    public enum TimePreset { FimDeTarde, ManhaTropical, NoiteNaOrla }

    [Header("Animated Foliage & Lights")]
    public Transform[] swayingPalms;
    public Transform[] swayingFestoons;
    public Transform[] idleCharacters;
    public Light[] warmLanternLights;

    [Header("Living World Wildlife, Sea & Sky")]
    public Transform[] clouds;
    public Transform[] cloudShadows;
    public Transform[] seagulls;
    public Transform[] butterflies;
    public Transform[] beachCrabs;
    public Transform[] fireflies;
    public Transform[] breezeCurtains;
    public Transform[] floatingBoats;
    public Transform[] floatingBuoys;
    public Transform[] ambientLeaves;
    public Transform[] waterSparkles;

    [Header("Baia do Avesso Tide & Villager Routine")]
    public Transform oceanSurface;
    public Transform floristRoutineTarget;
    public Vector3 floristMorningPos = new Vector3(24.0f, 0f, 1.35f);
    public Vector3 floristEveningPos = new Vector3(24.0f, 0f, 0.20f);
    public bool isHighTide;
    public bool autoTideCycle = true;
    public float tideCycleSeconds = 54f;
    public bool windGustActive;
    public bool festivalModeActive;
    public Transform[] windPetals;
    public Transform[] collectibleCards;
    public Light[] pavilionLights;

    public Transform playerTransform;

    [Header("Lighting & Atmosphere")]
    public Light sunLight;
    public Camera targetCamera;
    public GirlPowerOrla3DCameraPost cameraPost;
    public TimePreset currentPreset = TimePreset.FimDeTarde;

    private Vector3 oceanLowTidePos;
    private float tideTimer;
    private float tideBlend;
    private Vector3[] petalBasePos;
    private Vector3[] cardBasePos;
    private Vector3[] boatBasePos;
    private Quaternion[] boatBaseRot;
    private Vector3[] buoyBasePos;
    private Vector3[] leafBasePos;
    private Vector3[] sparkleBasePos;

    private Quaternion[] palmBaseRot;
    private Quaternion[] festoonBaseRot;
    private Vector3[] charBasePos;
    private float[] lightBaseIntensity;
    private Vector3[] cloudBasePos;
    private Vector3[] cloudShadowBasePos;
    private Vector3[] gullBasePos;
    private Transform[] gullWingL;
    private Transform[] gullWingR;
    private Vector3[] bfBasePos;
    private Transform[] bfWingL;
    private Transform[] bfWingR;
    private Vector3[] crabBasePos;
    private Vector3[] fireflyBasePos;
    private float lanternScale = 1.0f;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (cameraPost == null && targetCamera != null)
            cameraPost = targetCamera.GetComponent<GirlPowerOrla3DCameraPost>();

        palmBaseRot = CaptureRotations(swayingPalms);
        festoonBaseRot = CaptureRotations(swayingFestoons);
        charBasePos = CapturePositions(idleCharacters);
        cloudBasePos = CapturePositions(clouds);
        cloudShadowBasePos = CapturePositions(cloudShadows);
        gullBasePos = CapturePositions(seagulls);
        bfBasePos = CapturePositions(butterflies);
        crabBasePos = CapturePositions(beachCrabs);
        fireflyBasePos = CapturePositions(fireflies);
        boatBasePos = CapturePositions(floatingBoats);
        boatBaseRot = CaptureRotations(floatingBoats);
        buoyBasePos = CapturePositions(floatingBuoys);
        leafBasePos = CapturePositions(ambientLeaves);
        sparkleBasePos = CapturePositions(waterSparkles);

        CacheWings(seagulls, out gullWingL, out gullWingR);
        CacheWings(butterflies, out bfWingL, out bfWingR);

        if (warmLanternLights != null)
        {
            lightBaseIntensity = new float[warmLanternLights.Length];
            for (int i = 0; i < warmLanternLights.Length; i++)
                lightBaseIntensity[i] = warmLanternLights[i] != null ? warmLanternLights[i].intensity : 1.3f;
        }

        SanitizeLegacySceneVisualsAndOcean();

        if (oceanSurface != null)
            oceanLowTidePos = oceanSurface.position;

        petalBasePos = CapturePositions(windPetals);
        cardBasePos = CapturePositions(collectibleCards);
        SetArrayActive(windPetals, windGustActive);
        SetPavilionLightsActive(false);

        ApplyTimePreset(currentPreset);
    }

    private void SanitizeLegacySceneVisualsAndOcean()
    {
        // 1. Eliminate any legacy cloud shadow ground quads (giant translucent rectangles causing vertical bands on sand/boardwalk!)
        if (cloudShadows != null)
        {
            for (int i = 0; i < cloudShadows.Length; i++)
                if (cloudShadows[i] != null) cloudShadows[i].gameObject.SetActive(false);
        }
        foreach (Transform tr in FindObjectsOfType<Transform>())
        {
            if (tr != null && tr.name.StartsWith("Sombra_Nuvem_Movel"))
                tr.gameObject.SetActive(false);
            else if (tr != null && tr.name == "Terreno Costeiro - Orla das Fadas")
            {
                foreach (Renderer r in tr.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                }
            }
        }

        // 2. Guarantee visible, shoreline-aligned animated ocean plane (shoreline z = 8.2m, y = -0.20m) & Water Zone even on older scene builds!
        if (oceanSurface == null)
        {
            GameObject foundOcean = GameObject.Find("Mar Estilizado - Animado");
            if (foundOcean != null) oceanSurface = foundOcean.transform;
        }
        if (oceanSurface != null)
        {
            MeshFilter mf = oceanSurface.GetComponent<MeshFilter>();
            bool needsMeshUpgrade = mf == null || mf.sharedMesh == null || mf.sharedMesh.bounds.extents.z < 30f;
            if (needsMeshUpgrade)
            {
                if (mf == null) mf = oceanSurface.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = BuildRuntimeOceanMesh(260f, 76f, 84, 34);
                oceanSurface.position = new Vector3(12f, -0.20f, 46.2f);
            }
            else
            {
                if (mf.sharedMesh.bounds.extents.y < 5f)
                    mf.sharedMesh.bounds = new Bounds(Vector3.zero, new Vector3(260f, 40f, 76f));
                if (oceanSurface.position.y < -0.24f || oceanSurface.position.z > 47.0f)
                    oceanSurface.position = new Vector3(oceanSurface.position.x, -0.20f, 46.2f);
            }

            // Ensure rich visible turquoise material properties
            Renderer mr = oceanSurface.GetComponent<Renderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                Material mat = mr.sharedMaterial;
                if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", new Color(0.20f, 0.82f, 0.86f, 0.86f));
                if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", new Color(0.06f, 0.42f, 0.62f, 0.97f));
            }

            // Ensure explicit Water Surf/Swim Zone collider exists and is linked to Story Controller
            Transform wzTr = oceanSurface.Find("Zona_Agua_Mar_Surf");
            BoxCollider wzCol = wzTr != null ? wzTr.GetComponent<BoxCollider>() : null;
            if (wzCol == null)
            {
                GameObject wzObj = new GameObject("Zona_Agua_Mar_Surf");
                wzObj.transform.SetParent(oceanSurface, false);
                wzObj.transform.localPosition = Vector3.zero;
                wzCol = wzObj.AddComponent<BoxCollider>();
                wzCol.isTrigger = true;
            }
            // Ocean mesh spans local Z -38..+38 (world Z = 8.2 .. 84.2). Water zone begins at local Z = -37.55 (world Z = 8.65).
            wzCol.center = new Vector3(0f, 0.5f, 0.225f);
            wzCol.size = new Vector3(260f, 4.0f, 75.55f);

            GirlPowerOrla3DFirstDelivery story = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
            if (story != null && story.waterSurfZone == null)
                story.waterSurfZone = wzCol;
        }
        // 3. Ensure anchored boats & buoys sit cleanly ON TOP of the water surface (y = -0.14f)
        if (floatingBoats != null && boatBasePos != null)
        {
            for (int i = 0; i < floatingBoats.Length; i++)
            {
                if (floatingBoats[i] == null) continue;
                if (boatBasePos[i].y < -0.18f)
                {
                    boatBasePos[i].y = -0.14f;
                    floatingBoats[i].position = boatBasePos[i];
                }
            }
        }
        if (floatingBuoys != null && buoyBasePos != null)
        {
            for (int i = 0; i < floatingBuoys.Length; i++)
            {
                if (floatingBuoys[i] == null) continue;
                if (buoyBasePos[i].y < -0.18f)
                {
                    buoyBasePos[i].y = -0.14f;
                    floatingBuoys[i].position = buoyBasePos[i];
                }
            }
        }
    }

    private static Mesh BuildRuntimeOceanMesh(float sizeX, float sizeZ, int segsX, int segsZ)
    {
        Mesh mesh = new Mesh { name = "OrlaOceanGrid_Runtime" };
        Vector3[] verts = new Vector3[(segsX + 1) * (segsZ + 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[segsX * segsZ * 6];
        int v = 0;
        for (int z = 0; z <= segsZ; z++)
        {
            float tz = (float)z / segsZ;
            for (int x = 0; x <= segsX; x++)
            {
                float tx = (float)x / segsX;
                verts[v] = new Vector3((tx - 0.5f) * sizeX, 0f, (tz - 0.5f) * sizeZ);
                uvs[v] = new Vector2(tx, tz);
                v++;
            }
        }
        int ti = 0;
        for (int z = 0; z < segsZ; z++)
        {
            for (int x = 0; x < segsX; x++)
            {
                int i0 = z * (segsX + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (segsX + 1);
                int i3 = i2 + 1;
                tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1;
                tris[ti++] = i1; tris[ti++] = i2; tris[ti++] = i3;
            }
        }
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        // Generous vertical bounds so horizon curvature never triggers frustum culling!
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(sizeX, 40f, sizeZ));
        return mesh;
    }

    private void Update()
    {
        if (Time.timeScale > 0.01f)
        {
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.tKey.wasPressedThisFrame)
                    CycleTimePreset();
                if (keys.rKey.wasPressedThisFrame)
                    ToggleTide();
            }
        }

        float dt = Time.deltaTime;
        float t = Time.time;
        UpdateTide(dt);
        UpdateFloristRoutine(dt);
        AnimatePalms(t);
        AnimateFestoons(t);
        AnimateCurtains(t);
        AnimateIdleCharacters(t);
        AnimateLanterns(t);
        AnimateClouds(t);
        AnimateCloudShadows(t);
        AnimateSeagulls(t);
        AnimateButterflies(t);
        AnimateCrabs(t);
        AnimateFireflies(t);
        AnimateBoatsAndBuoys(t);
        AnimateAmbientLeaves(t, dt);
        AnimateWaterSparkles(t);
        AnimateWindPetals(t, dt);
        AnimateCollectibleCards(t, dt);
    }

    public void SetWindGust(bool active)
    {
        windGustActive = active;
        SetArrayActive(windPetals, active);
    }

    public void SetFestivalLights(bool active)
    {
        festivalModeActive = active;
        SetPavilionLightsActive(active);
    }

    private void SetPavilionLightsActive(bool active)
    {
        if (pavilionLights == null) return;
        for (int i = 0; i < pavilionLights.Length; i++)
            if (pavilionLights[i] != null) pavilionLights[i].enabled = active;
    }

    public void SnapIdleBasePositions()
    {
        charBasePos = CapturePositions(idleCharacters);
    }

    public void ToggleTide()
    {
        isHighTide = !isHighTide;
        tideTimer = 0f;
    }

    public string GetTideDisplayName()
    {
        return isHighTide ? "Maré Alta" : "Maré Baixa";
    }

    public float GetCurrentMaxWalkableZ()
    {
        return Mathf.Lerp(10.8f, 8.2f, tideBlend);
    }

    private void UpdateTide(float dt)
    {
        if (autoTideCycle && tideCycleSeconds > 5f)
        {
            tideTimer += dt;
            if (tideTimer >= tideCycleSeconds)
            {
                tideTimer = 0f;
                isHighTide = !isHighTide;
            }
        }
        float target = isHighTide ? 1f : 0f;
        tideBlend = Mathf.MoveTowards(tideBlend, target, dt * 0.45f);
        if (oceanSurface != null)
        {
            // Keep high tide strictly within the wet shoreline (never invading dry sand, sidewalk, or street!)
            Vector3 highPos = oceanLowTidePos + new Vector3(0f, 0.06f, -0.95f);
            oceanSurface.position = Vector3.Lerp(oceanLowTidePos, highPos, Mathf.SmoothStep(0f, 1f, tideBlend));
        }
    }

    private void UpdateFloristRoutine(float dt)
    {
        if (floristRoutineTarget == null || festivalModeActive) return;
        Vector3 goal = currentPreset == TimePreset.ManhaTropical ? floristMorningPos : floristEveningPos;
        Vector3 cur = floristRoutineTarget.position;
        Vector3 target = new Vector3(goal.x, cur.y, goal.z);
        Vector3 next = Vector3.Lerp(cur, target, dt * 2.2f);
        floristRoutineTarget.position = next;
        if (idleCharacters != null && charBasePos != null)
        {
            for (int i = 0; i < idleCharacters.Length; i++)
            {
                if (idleCharacters[i] == floristRoutineTarget)
                    charBasePos[i] = new Vector3(next.x, goal.y, next.z);
            }
        }
    }

    public void CycleTimePreset()
    {
        int next = ((int)currentPreset + 1) % 3;
        ApplyTimePreset((TimePreset)next);
    }

    public string GetPresetDisplayName()
    {
        switch (currentPreset)
        {
            case TimePreset.ManhaTropical: return "Manhã";
            case TimePreset.NoiteNaOrla: return "Noite";
            default: return "Fim de Tarde";
        }
    }

    public void ApplyTimePreset(TimePreset preset)
    {
        currentPreset = preset;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        // Distant gentle horizon mist only (58m..135m) so ocean & sky stay crisp and clearly separated!
        RenderSettings.fogStartDistance = 58f;
        RenderSettings.fogEndDistance = 135f;

        // Warm coastal lighting hierarchy: elevated soft shadows (zero long dark streaks across the walkway!)
        switch (preset)
        {
            case TimePreset.ManhaTropical:
                if (sunLight != null)
                {
                    sunLight.color = new Color(1.0f, 0.94f, 0.82f);
                    sunLight.intensity = 0.88f;
                    sunLight.shadowStrength = 0.34f;
                    sunLight.transform.rotation = Quaternion.Euler(54f, -28f, 0f);
                }
                RenderSettings.ambientSkyColor = new Color(0.46f, 0.66f, 0.82f);
                RenderSettings.ambientEquatorColor = new Color(0.64f, 0.60f, 0.54f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.32f, 0.28f);
                RenderSettings.fogColor = new Color(0.46f, 0.76f, 0.92f);
                if (targetCamera != null) targetCamera.backgroundColor = new Color(0.30f, 0.66f, 0.90f);
                if (cameraPost != null) cameraPost.ApplyAtmosphere(0.94f, 1.09f, 0.08f, new Color(1.01f, 1.0f, 0.97f, 1f));
                lanternScale = 0.22f;
                SetArrayActive(butterflies, true);
                SetArrayActive(fireflies, true);
                break;

            case TimePreset.NoiteNaOrla:
                if (sunLight != null)
                {
                    sunLight.color = new Color(0.54f, 0.68f, 0.96f);
                    sunLight.intensity = 0.44f;
                    sunLight.shadowStrength = 0.38f;
                    sunLight.transform.rotation = Quaternion.Euler(46f, 32f, 0f);
                }
                RenderSettings.ambientSkyColor = new Color(0.14f, 0.20f, 0.38f);
                RenderSettings.ambientEquatorColor = new Color(0.18f, 0.22f, 0.38f);
                RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.22f);
                RenderSettings.fogColor = new Color(0.11f, 0.17f, 0.30f);
                if (targetCamera != null) targetCamera.backgroundColor = new Color(0.09f, 0.15f, 0.30f);
                if (cameraPost != null) cameraPost.ApplyAtmosphere(0.95f, 1.12f, 0.16f, new Color(0.94f, 0.97f, 1.05f, 1f));
                lanternScale = 1.65f;
                SetArrayActive(butterflies, false);
                SetArrayActive(fireflies, true);
                break;

            default: // FimDeTarde (Bright Golden Coastal Afternoon + Defined Azure Sky & Deep Turquoise Sea!)
                if (sunLight != null)
                {
                    sunLight.color = new Color(1.0f, 0.88f, 0.72f);
                    sunLight.intensity = 0.86f;
                    sunLight.shadowStrength = 0.36f;
                    sunLight.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
                }
                RenderSettings.ambientSkyColor = new Color(0.46f, 0.62f, 0.78f);
                RenderSettings.ambientEquatorColor = new Color(0.68f, 0.60f, 0.52f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.30f, 0.30f);
                RenderSettings.fogColor = new Color(0.48f, 0.74f, 0.88f);
                if (targetCamera != null) targetCamera.backgroundColor = new Color(0.36f, 0.68f, 0.88f);
                if (cameraPost != null) cameraPost.ApplyAtmosphere(0.93f, 1.09f, 0.08f, new Color(1.02f, 0.99f, 0.94f, 1f));
                lanternScale = 0.85f;
                SetArrayActive(butterflies, true);
                SetArrayActive(fireflies, true);
                break;
        }
    }

    private void AnimateBoatsAndBuoys(float t)
    {
        if (floatingBoats != null && boatBasePos != null)
        {
            float tideY = tideBlend * 0.24f;
            float tideZ = -tideBlend * 1.4f;
            for (int i = 0; i < floatingBoats.Length; i++)
            {
                Transform tr = floatingBoats[i];
                if (tr == null) continue;
                float phase = i * 1.85f;
                float bob = Mathf.Sin(t * 1.55f + phase) * 0.11f;
                float roll = Mathf.Sin(t * 1.25f + phase) * 4.5f;
                float pitch = Mathf.Cos(t * 1.05f + phase * 1.3f) * 3.0f;
                tr.position = boatBasePos[i] + new Vector3(Mathf.Sin(t * 0.45f + phase) * 0.22f, bob + tideY, tideZ);
                tr.localRotation = boatBaseRot[i] * Quaternion.Euler(pitch, 0f, roll);
            }
        }

        if (floatingBuoys != null && buoyBasePos != null)
        {
            float tideY = tideBlend * 0.24f;
            for (int i = 0; i < floatingBuoys.Length; i++)
            {
                Transform tr = floatingBuoys[i];
                if (tr == null) continue;
                float bob = Mathf.Sin(t * 2.1f + i * 1.4f) * 0.09f;
                tr.position = buoyBasePos[i] + new Vector3(0f, bob + tideY, 0f);
                tr.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.8f + i) * 9f, 0f, Mathf.Cos(t * 1.6f + i) * 11f);
            }
        }
    }

    private void AnimateAmbientLeaves(float t, float dt)
    {
        if (ambientLeaves == null || leafBasePos == null) return;
        for (int i = 0; i < ambientLeaves.Length; i++)
        {
            Transform tr = ambientLeaves[i];
            if (tr == null) continue;
            float cycle = Mathf.Repeat(t * (0.14f + (i % 4) * 0.03f) + i * 0.27f, 1f);
            float y = Mathf.Lerp(5.4f, 0.18f, cycle);
            float swayX = Mathf.Sin(t * 1.7f + i * 1.9f) * 0.85f + cycle * 1.4f;
            float swayZ = Mathf.Cos(t * 1.3f + i * 1.3f) * 0.55f;
            tr.position = new Vector3(leafBasePos[i].x + swayX, y, leafBasePos[i].z + swayZ);
            tr.Rotate(55f * dt, 85f * dt, 40f * dt);
        }
    }

    private void AnimateWaterSparkles(float t)
    {
        if (waterSparkles == null || sparkleBasePos == null) return;
        float tideY = tideBlend * 0.24f;
        for (int i = 0; i < waterSparkles.Length; i++)
        {
            Transform tr = waterSparkles[i];
            if (tr == null) continue;
            float twinkle = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * 3.4f + i * 2.17f), 4f);
            tr.position = sparkleBasePos[i] + new Vector3(Mathf.Sin(t * 0.6f + i) * 0.3f, tideY, 0f);
            tr.localScale = Vector3.one * (0.04f + twinkle * 0.15f);
        }
    }

    private void AnimateCurtains(float t)
    {
        if (breezeCurtains == null) return;
        float mult = windGustActive ? 2.6f : 1.0f;
        for (int i = 0; i < breezeCurtains.Length; i++)
        {
            if (breezeCurtains[i] == null) continue;
            float sway = (Mathf.Sin(t * 1.9f * mult + i * 1.4f) * 8.5f + Mathf.Sin(t * 3.4f + i) * 2.5f) * mult;
            breezeCurtains[i].localRotation = Quaternion.Euler(sway, 0f, sway * 0.25f);
        }
    }

    private void AnimateWindPetals(float t, float dt)
    {
        if (!windGustActive || windPetals == null || petalBasePos == null) return;
        for (int i = 0; i < windPetals.Length; i++)
        {
            Transform tr = windPetals[i];
            if (tr == null) continue;
            float speed = 5.2f + (i % 4) * 1.1f;
            float x = Mathf.Repeat((petalBasePos[i].x + 12f) - t * speed, 48f) - 12f;
            float y = petalBasePos[i].y + Mathf.Sin(t * 3.5f + i * 1.7f) * 0.65f;
            float z = petalBasePos[i].z + Mathf.Cos(t * 2.2f + i) * 0.9f;
            tr.position = new Vector3(x, y, z);
            tr.Rotate(140f * dt, 190f * dt, 90f * dt);
        }
    }

    private void AnimateCollectibleCards(float t, float dt)
    {
        if (collectibleCards == null || cardBasePos == null) return;
        for (int i = 0; i < collectibleCards.Length; i++)
        {
            Transform tr = collectibleCards[i];
            if (tr == null || !tr.gameObject.activeSelf) continue;
            float bob = Mathf.Sin(t * 3.2f + i * 1.5f) * 0.14f;
            tr.position = cardBasePos[i] + new Vector3(0f, bob, 0f);
            tr.Rotate(0f, 52f * dt, 0f, Space.World);
        }
    }

    private void AnimatePalms(float t)
    {
        if (swayingPalms == null || palmBaseRot == null) return;
        float gust = windGustActive ? 2.6f : 1.0f;
        for (int i = 0; i < swayingPalms.Length; i++)
        {
            if (swayingPalms[i] == null) continue;
            float swayZ = (Mathf.Sin(t * 1.25f * gust + i * 1.4f) * 2.1f + Mathf.Sin(t * 2.6f + i) * 0.55f) * gust;
            float swayX = Mathf.Cos(t * 1.05f * gust + i * 1.7f) * 1.15f * gust;
            swayingPalms[i].localRotation = palmBaseRot[i] * Quaternion.Euler(swayX, 0f, swayZ);
        }
    }

    private void AnimateFestoons(float t)
    {
        if (swayingFestoons == null || festoonBaseRot == null) return;
        for (int i = 0; i < swayingFestoons.Length; i++)
        {
            if (swayingFestoons[i] == null) continue;
            float swing = Mathf.Sin(t * 1.65f + i * 1.2f) * 3.5f;
            swayingFestoons[i].localRotation = festoonBaseRot[i] * Quaternion.Euler(swing, 0f, 0f);
        }
    }

    private void AnimateIdleCharacters(float t)
    {
        if (idleCharacters == null || charBasePos == null) return;
        float dt = Time.deltaTime;
        for (int i = 0; i < idleCharacters.Length; i++)
        {
            Transform npc = idleCharacters[i];
            if (npc == null) continue;

            // Gentle cozy breathing + subtle idle sway
            float bob = Mathf.Abs(Mathf.Sin(t * 2.2f + i * 1.5f)) * 0.04f;
            npc.position = charBasePos[i] + new Vector3(0f, bob, 0f);
            float breath = 1f + Mathf.Sin(t * 2.6f + i) * 0.022f;
            npc.localScale = new Vector3(1f, breath, 1f);

            // Smoothly look toward the player when the player approaches!
            if (playerTransform != null)
            {
                Vector3 toPlayer = playerTransform.position - npc.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude < 16f && toPlayer.sqrMagnitude > 0.05f)
                {
                    Quaternion look = Quaternion.LookRotation(-toPlayer.normalized, Vector3.up);
                    npc.rotation = Quaternion.Slerp(npc.rotation, look, dt * 4.5f);
                }
            }
        }
    }

    private void AnimateLanterns(float t)
    {
        if (warmLanternLights == null || lightBaseIntensity == null) return;
        for (int i = 0; i < warmLanternLights.Length; i++)
        {
            if (warmLanternLights[i] == null) continue;
            float pulse = 0.90f + 0.10f * Mathf.Sin(t * 2.4f + i * 1.3f);
            warmLanternLights[i].intensity = lightBaseIntensity[i] * lanternScale * pulse;
        }
    }

    private void AnimateClouds(float t)
    {
        if (clouds == null || cloudBasePos == null) return;
        for (int i = 0; i < clouds.Length; i++)
        {
            Transform tr = clouds[i];
            if (tr == null) continue;
            float speed = 0.28f + (i % 3) * 0.10f;
            float span = 76f;
            float x = Mathf.Repeat((cloudBasePos[i].x + 24f) + t * speed, span) - 24f;
            float y = cloudBasePos[i].y + Mathf.Sin(t * 0.45f + i * 1.3f) * 0.22f;
            tr.position = new Vector3(x, y, cloudBasePos[i].z);
        }
    }

    private void AnimateCloudShadows(float t)
    {
        if (cloudShadows == null || cloudShadowBasePos == null) return;
        for (int i = 0; i < cloudShadows.Length; i++)
        {
            Transform sh = cloudShadows[i];
            if (sh == null) continue;
            float speed = 0.52f + (i % 2) * 0.18f;
            float span = 68f;
            float x = Mathf.Repeat((cloudShadowBasePos[i].x + 22f) + t * speed, span) - 22f;
            float zDrift = Mathf.Sin(t * 0.18f + i * 1.7f) * 1.1f;
            sh.position = new Vector3(x, cloudShadowBasePos[i].y, cloudShadowBasePos[i].z + zDrift);
        }
    }

    private void AnimateSeagulls(float t)
    {
        if (seagulls == null || gullBasePos == null) return;
        for (int i = 0; i < seagulls.Length; i++)
        {
            Transform tr = seagulls[i];
            if (tr == null) continue;
            float speed = 0.42f + (i % 3) * 0.11f;
            float ang = t * speed + i * 2.1f;
            float rx = 6.2f + (i % 2) * 2.2f;
            float rz = 2.8f;
            Vector3 pos = gullBasePos[i] + new Vector3(Mathf.Cos(ang) * rx, Mathf.Sin(t * 1.3f + i) * 0.42f, Mathf.Sin(ang) * rz);
            Vector3 vel = new Vector3(-Mathf.Sin(ang) * rx, 0f, Mathf.Cos(ang) * rz);
            tr.position = pos;
            if (vel.sqrMagnitude > 0.001f)
                tr.rotation = Quaternion.LookRotation(-vel.normalized, Vector3.up);

            float flap = Mathf.Sin(t * 6.5f + i * 1.7f) * 28f;
            if (gullWingL != null && gullWingL[i] != null)
                gullWingL[i].localRotation = Quaternion.Euler(0f, 0f, flap);
            if (gullWingR != null && gullWingR[i] != null)
                gullWingR[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
    }

    private void AnimateButterflies(float t)
    {
        if (butterflies == null || bfBasePos == null) return;
        for (int i = 0; i < butterflies.Length; i++)
        {
            Transform tr = butterflies[i];
            if (tr == null || !tr.gameObject.activeSelf) continue;
            float ang = t * (1.15f + (i % 2) * 0.35f) + i * 1.8f;
            Vector3 offset = new Vector3(
                Mathf.Cos(ang) * 1.15f,
                Mathf.Sin(t * 2.8f + i * 1.4f) * 0.32f,
                Mathf.Sin(ang * 1.3f) * 0.85f
            );
            Vector3 next = bfBasePos[i] + offset;
            Vector3 delta = next - tr.position;
            tr.position = next;
            Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
            if (flatDelta.sqrMagnitude > 0.0001f)
                tr.rotation = Quaternion.LookRotation(-flatDelta.normalized, Vector3.up);

            float flutter = Mathf.Sin(t * 22f + i * 2.1f) * 46f;
            if (bfWingL != null && bfWingL[i] != null)
                bfWingL[i].localRotation = Quaternion.Euler(0f, 0f, flutter);
            if (bfWingR != null && bfWingR[i] != null)
                bfWingR[i].localRotation = Quaternion.Euler(0f, 0f, -flutter);
        }
    }

    private void AnimateCrabs(float t)
    {
        if (beachCrabs == null || crabBasePos == null) return;
        for (int i = 0; i < beachCrabs.Length; i++)
        {
            Transform tr = beachCrabs[i];
            if (tr == null) continue;
            // Short lateral bursts along the shoreline foam with natural resting pauses!
            float phase = Mathf.Repeat(t * (0.28f + i * 0.05f) + i * 0.37f, 1f);
            float lateral;
            bool scuttling = false;
            if (phase < 0.25f) { lateral = Mathf.SmoothStep(-0.9f, 0.9f, phase / 0.25f); scuttling = true; }
            else if (phase < 0.55f) { lateral = 0.9f; }
            else if (phase < 0.80f) { lateral = Mathf.SmoothStep(0.9f, -0.9f, (phase - 0.55f) / 0.25f); scuttling = true; }
            else { lateral = -0.9f; }
            float hop = scuttling ? Mathf.Abs(Mathf.Sin(t * 14f + i)) * 0.032f : 0f;
            float roll = scuttling ? Mathf.Sin(t * 16f + i) * 7f : 0f;
            tr.position = crabBasePos[i] + new Vector3(lateral, hop, Mathf.Sin(t * 0.6f + i) * 0.12f);
            tr.localRotation = Quaternion.Euler(0f, 0f, roll);
        }
    }

    private void AnimateFireflies(float t)
    {
        if (fireflies == null || fireflyBasePos == null) return;
        for (int i = 0; i < fireflies.Length; i++)
        {
            Transform tr = fireflies[i];
            if (tr == null || !tr.gameObject.activeSelf) continue;
            Vector3 drift = new Vector3(
                Mathf.Sin(t * 0.85f + i * 1.9f) * 0.65f,
                Mathf.Cos(t * 1.15f + i * 2.3f) * 0.35f,
                Mathf.Sin(t * 0.72f + i * 1.3f) * 0.55f
            );
            tr.position = fireflyBasePos[i] + drift;
            float pulse = 0.55f + 0.55f * Mathf.Sin(t * 3.4f + i * 1.7f);
            tr.localScale = Vector3.one * (0.10f * pulse);
        }
    }

    private static void CacheWings(Transform[] roots, out Transform[] lefts, out Transform[] rights)
    {
        if (roots == null)
        {
            lefts = null;
            rights = null;
            return;
        }
        lefts = new Transform[roots.Length];
        rights = new Transform[roots.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            lefts[i] = FindChildRecursive(roots[i], "WingL");
            rights[i] = FindChildRecursive(roots[i], "WingR");
        }
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform f = FindChildRecursive(root.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    private static void SetArrayActive(Transform[] arr, bool active)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) arr[i].gameObject.SetActive(active);
    }

    private static Quaternion[] CaptureRotations(Transform[] items)
    {
        if (items == null) return null;
        Quaternion[] res = new Quaternion[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) res[i] = items[i].localRotation;
        return res;
    }

    private static Vector3[] CapturePositions(Transform[] items)
    {
        if (items == null) return null;
        Vector3[] res = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) res[i] = items[i].position;
        return res;
    }
}
