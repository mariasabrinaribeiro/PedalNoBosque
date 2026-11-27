using UnityEngine;

/// <summary>
/// Physically coherent 3D Depth Horizon & Coastal Motion Controller for Orla das Fadas:
/// - Respects TRUE 3D perspective depth: ZERO artificial camera-follow sliding on islands, clouds,
///   or sailboats so nothing ever slides across the ocean, reverses direction, or jumps at wrap edges!
/// - Layer 1 (Sky & Horizon): Wide distant islands firmly rooted in the sea + soft high clouds drifting gently in wind.
/// - Layer 2 (Mid-Sea): Open-sea sailboats gliding smoothly with water-synced height and off-screen turnaround.
/// - Layer 3 (Shoreline): Subtle surf foam synced with the ocean tide plane so it never clips dry sand.
/// </summary>
public sealed class GirlPowerOrla3DParallaxHorizon : MonoBehaviour
{
    [Header("Scene References")]
    public Camera sceneCamera;
    public Transform oceanSurface;

    [Header("Layer 1: Distant Horizon Islands & Sky Clouds (Real 3D Depth)")]
    public Transform[] farHorizonIslands;
    public Transform[] farHorizonClouds;
    [Range(0f, 1f)] public float farLayerFollowFactor = 0.0f;

    [Header("Layer 2: Open-Sea Sailboats (Real 3D Depth, Zero Artificial Drag)")]
    public Transform[] midSeaSailboats;
    [Range(0f, 1f)] public float midLayerFollowFactor = 0.0f;

    [Header("Layer 3: World-Space Shoreline Foam (Synced to Ocean Tide)")]
    public Transform[] shorelineFoamRibbons;

    private Vector3 oceanOrigin;
    private bool hasOceanOrigin;
    private Vector3[] cloudBasePos;
    private Vector3[] boatBasePos;
    private Quaternion[] boatBaseRot;
    private Vector3[] foamBasePos;
    private Vector3[] foamBaseScale;

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (oceanSurface == null)
        {
            GameObject oceanObj = GameObject.Find("Mar Estilizado - Animado");
            if (oceanObj != null) oceanSurface = oceanObj.transform;
        }
        if (oceanSurface != null)
        {
            oceanOrigin = oceanSurface.position;
            hasOceanOrigin = true;
        }

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

        float t = Time.time;
        float tideDeltaY = 0f;
        float tideDeltaZ = 0f;
        if (oceanSurface != null)
        {
            if (!hasOceanOrigin)
            {
                oceanOrigin = oceanSurface.position;
                hasOceanOrigin = true;
            }
            tideDeltaY = oceanSurface.position.y - oceanOrigin.y;
            tideDeltaZ = oceanSurface.position.z - oceanOrigin.z;
        }

        // 1. Far Horizon Islands remain 100% anchored in real 3D world space (Z = 60..66m):
        //    real perspective depth provides natural, smooth parallax with zero sliding over the sea!

        // 2. High Horizon Clouds: gentle organic wind drift in world space (never jumping or clipping islands!)
        if (farHorizonClouds != null && cloudBasePos != null)
        {
            for (int i = 0; i < farHorizonClouds.Length; i++)
            {
                Transform tr = farHorizonClouds[i];
                if (tr == null) continue;
                float gentleDrift = Mathf.Sin(t * 0.06f + i * 1.35f) * 2.8f;
                float gentleLift = Mathf.Cos(t * 0.09f + i * 1.1f) * 0.14f;
                tr.position = new Vector3(
                    cloudBasePos[i].x + gentleDrift,
                    cloudBasePos[i].y + gentleLift,
                    cloudBasePos[i].z);
            }
        }

        // 3. Open-Sea Sailboats: true 3D water-seated sailing with smooth turnaround well off-screen (ZERO teleport pops!)
        if (midSeaSailboats != null && boatBasePos != null)
        {
            for (int i = 0; i < midSeaSailboats.Length; i++)
            {
                Transform tr = midSeaSailboats[i];
                if (tr == null) continue;
                float dir = (i % 2 == 0) ? 1f : -1f;
                float speed = 0.32f + (i % 3) * 0.10f;
                // Wide off-screen wrap span [-78 .. +104] ensures no sailboat ever pops inside the camera frustum!
                const float minSailX = -78f;
                const float sailSpan = 182f;
                float x = Mathf.Repeat((boatBasePos[i].x - minSailX) + t * speed * dir, sailSpan) + minSailX;
                float bob = Mathf.Sin(t * 1.30f + i * 1.7f) * 0.038f;
                float roll = Mathf.Sin(t * 1.10f + i * 1.3f) * 2.8f;

                tr.position = new Vector3(x, boatBasePos[i].y + tideDeltaY + bob, boatBasePos[i].z);
                if (boatBaseRot != null && i < boatBaseRot.Length)
                    tr.localRotation = boatBaseRot[i] * Quaternion.Euler(0f, 0f, roll);
            }
        }

        // 4. World-Space Shoreline Foam Ribbons synced cleanly with ocean tide (never sliding over dry sand!)
        if (shorelineFoamRibbons != null && foamBasePos != null)
        {
            for (int i = 0; i < shorelineFoamRibbons.Length; i++)
            {
                Transform tr = shorelineFoamRibbons[i];
                if (tr == null) continue;
                float phase = t * 1.05f + i * 0.85f;
                float wash = Mathf.Sin(phase);
                float zOffset = -wash * 0.14f;
                float xSway = Mathf.Cos(phase * 0.55f) * 0.14f;
                tr.position = new Vector3(
                    foamBasePos[i].x + xSway,
                    foamBasePos[i].y + tideDeltaY,
                    foamBasePos[i].z + tideDeltaZ * 0.55f + zOffset);
                if (foamBaseScale != null && i < foamBaseScale.Length)
                {
                    float widthPulse = 0.86f + 0.22f * (0.5f + 0.5f * wash);
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
