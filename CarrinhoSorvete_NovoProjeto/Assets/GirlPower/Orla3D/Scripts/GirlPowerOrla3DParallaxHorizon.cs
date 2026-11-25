using UnityEngine;

/// <summary>
/// Cinematic 3-Layer Ocean & Horizon Parallax Controller for Orla das Fadas:
/// - Layer 1 (Far Horizon — 10% relative camera speed): Distant tropical islands/mountains & fluffy clouds
///   tracking 90% of camera lateral movement so they glide majestically at 10% apparent parallax speed!
/// - Layer 2 (Mid-Sea — 30% relative camera speed): Stylized sailboats & yachts sailing continuously
///   across the turquoise bay while tracking 70% of camera lateral movement (30% parallax depth!).
/// - Layer 3 (Shoreline — 100% world space): Soft looping wave-foam ribbons washing smoothly onto the wet sand!
/// </summary>
public sealed class GirlPowerOrla3DParallaxHorizon : MonoBehaviour
{
    [Header("Camera Reference")]
    public Camera sceneCamera;

    [Header("Layer 1: Far Horizon Islands & Clouds (10% camera speed)")]
    public Transform[] farHorizonIslands;
    public Transform[] farHorizonClouds;
    [Range(0f, 1f)] public float farLayerFollowFactor = 0.90f; // 10% relative screen drift!

    [Header("Layer 2: Mid-Sea Sailing Boats (30% camera speed)")]
    public Transform[] midSeaSailboats;
    [Range(0f, 1f)] public float midLayerFollowFactor = 0.70f; // 30% relative screen drift!

    [Header("Layer 3: Shoreline Looping Wave Foam")]
    public Transform[] shorelineFoamRibbons;

    private Vector3 camOrigin;
    private bool hasCamOrigin;
    private Vector3[] islandBasePos;
    private Vector3[] cloudBasePos;
    private Vector3[] boatBasePos;
    private Quaternion[] boatBaseRot;
    private Vector3[] foamBasePos;
    private Vector3[] foamBaseScale;

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera != null)
        {
            camOrigin = sceneCamera.transform.position;
            hasCamOrigin = true;
        }

        islandBasePos = CapturePositions(farHorizonIslands);
        cloudBasePos = CapturePositions(farHorizonClouds);
        boatBasePos = CapturePositions(midSeaSailboats);
        boatBaseRot = CaptureRotations(midSeaSailboats);
        foamBasePos = CapturePositions(shorelineFoamRibbons);
        foamBaseScale = CaptureScales(shorelineFoamRibbons);
    }

    private void LateUpdate()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera == null) return;
        if (!hasCamOrigin)
        {
            camOrigin = sceneCamera.transform.position;
            hasCamOrigin = true;
        }

        float t = Time.time;
        Vector3 camDelta = sceneCamera.transform.position - camOrigin;
        float camDx = camDelta.x;
        float camDz = camDelta.z;

        // ------------------------------------------------------------------
        // LAYER 1 (10% apparent speed): Far Tropical Islands & Horizon Clouds
        // ------------------------------------------------------------------
        if (farHorizonIslands != null && islandBasePos != null)
        {
            float px = camDx * farLayerFollowFactor;
            float pz = camDz * (farLayerFollowFactor * 0.45f);
            for (int i = 0; i < farHorizonIslands.Length; i++)
            {
                Transform tr = farHorizonIslands[i];
                if (tr == null) continue;
                tr.position = new Vector3(
                    islandBasePos[i].x + px,
                    islandBasePos[i].y,
                    islandBasePos[i].z + pz);
            }
        }

        if (farHorizonClouds != null && cloudBasePos != null)
        {
            float px = camDx * farLayerFollowFactor;
            for (int i = 0; i < farHorizonClouds.Length; i++)
            {
                Transform tr = farHorizonClouds[i];
                if (tr == null) continue;
                float windDrift = Mathf.Repeat((t * (0.35f + (i % 3) * 0.12f)) + i * 14f, 84f) - 42f;
                float bobY = Mathf.Sin(t * 0.42f + i * 1.5f) * 0.25f;
                tr.position = new Vector3(
                    cloudBasePos[i].x + px + windDrift,
                    cloudBasePos[i].y + bobY,
                    cloudBasePos[i].z);
            }
        }

        // ------------------------------------------------------------------
        // LAYER 2 (30% apparent speed): Mid-Sea Sailboats gliding smoothly!
        // ------------------------------------------------------------------
        if (midSeaSailboats != null && boatBasePos != null)
        {
            float px = camDx * midLayerFollowFactor;
            float pz = camDz * (midLayerFollowFactor * 0.28f);
            for (int i = 0; i < midSeaSailboats.Length; i++)
            {
                Transform tr = midSeaSailboats[i];
                if (tr == null) continue;
                float dir = (i % 2 == 0) ? 1f : -1f;
                float speed = 0.72f + (i % 3) * 0.24f;
                float span = 68f;
                float localSail = Mathf.Repeat((boatBasePos[i].x + 28f) + t * speed * dir, span) - 28f;
                float bob = Mathf.Sin(t * 1.55f + i * 1.7f) * 0.11f;
                float roll = Mathf.Sin(t * 1.35f + i * 1.3f) * 4.8f;
                float pitch = Mathf.Cos(t * 1.10f + i * 1.9f) * 2.6f;

                tr.position = new Vector3(localSail + px, boatBasePos[i].y + bob, boatBasePos[i].z + pz);
                if (boatBaseRot != null && i < boatBaseRot.Length)
                    tr.localRotation = boatBaseRot[i] * Quaternion.Euler(pitch, 0f, roll);
            }
        }

        // ------------------------------------------------------------------
        // LAYER 3 (100% world space): Looping Shoreline Wave-Foam on the Sand!
        // ------------------------------------------------------------------
        if (shorelineFoamRibbons != null && foamBasePos != null)
        {
            for (int i = 0; i < shorelineFoamRibbons.Length; i++)
            {
                Transform tr = shorelineFoamRibbons[i];
                if (tr == null) continue;
                // Smooth rhythmic surf wash onto the sand and gently back to sea
                float phase = t * 1.15f + i * 0.85f;
                float wash = Mathf.Sin(phase); // -1 (recede) .. +1 (wash onto sand)
                float zOffset = -wash * 0.62f;
                float xSway = Mathf.Cos(phase * 0.6f) * 0.25f;
                tr.position = new Vector3(foamBasePos[i].x + xSway, foamBasePos[i].y, foamBasePos[i].z + zOffset);
                if (foamBaseScale != null && i < foamBaseScale.Length)
                {
                    float widthPulse = 0.76f + 0.34f * (0.5f + 0.5f * wash);
                    tr.localScale = new Vector3(foamBaseScale[i].x, foamBaseScale[i].y, foamBaseScale[i].z * widthPulse);
                }
            }
        }
    }

    private static Vector3[] CapturePositions(Transform[] items)
    {
        if (items == null) return null;
        Vector3[] res = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) res[i] = items[i].position;
        return res;
    }

    private static Quaternion[] CaptureRotations(Transform[] items)
    {
        if (items == null) return null;
        Quaternion[] res = new Quaternion[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) res[i] = items[i].localRotation;
        return res;
    }

    private static Vector3[] CaptureScales(Transform[] items)
    {
        if (items == null) return null;
        Vector3[] res = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) res[i] = items[i].localScale;
        return res;
    }
}
