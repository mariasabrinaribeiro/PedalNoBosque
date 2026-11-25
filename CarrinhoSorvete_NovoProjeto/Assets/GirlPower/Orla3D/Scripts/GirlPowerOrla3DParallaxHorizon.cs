using UnityEngine;

/// <summary>
/// Clean 2D/3D Hybrid Ocean & Horizon Parallax Controller for Orla das Fadas:
/// - Restricts decorative horizon parallax strictly to subtle LATERAL (X) movement (ZERO depth Z drift!)
///   so distant islands, 2D horizon clouds, and far sails stay locked inside the sea bounds at every zoom!
/// - Layer 1 (Far 2D Horizon): Wide coastal islands & calm cloud bands closing the horizon end-to-end.
/// - Layer 2 (Mid-Sea): Gentle distant sails staying strictly inside the visible ocean band.
/// - Layer 3 (Shoreline): 100% world-space foam ribbons anchored cleanly to the sand edge.
/// </summary>
public sealed class GirlPowerOrla3DParallaxHorizon : MonoBehaviour
{
    [Header("Camera Reference")]
    public Camera sceneCamera;

    [Header("Layer 1: Far 2D Horizon Islands & Clouds (Lateral-Only Subtle Drift)")]
    public Transform[] farHorizonIslands;
    public Transform[] farHorizonClouds;
    [Range(0f, 1f)] public float farLayerFollowFactor = 0.85f;

    [Header("Layer 2: Distant Decorative Sails (Clamped Inside Sea Area)")]
    public Transform[] midSeaSailboats;
    [Range(0f, 1f)] public float midLayerFollowFactor = 0.35f;

    [Header("Layer 3: World-Space Shoreline Foam")]
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
        // Lateral-only camera delta (ZERO depth Z follow so horizon & sails never slide over the shoreline or out of the sea!)
        float camDx = sceneCamera.transform.position.x - camOrigin.x;

        // 1. Far 2D Horizon Islands (pure lateral parallax, fixed world Z!)
        if (farHorizonIslands != null && islandBasePos != null)
        {
            float px = camDx * farLayerFollowFactor;
            for (int i = 0; i < farHorizonIslands.Length; i++)
            {
                Transform tr = farHorizonIslands[i];
                if (tr == null) continue;
                tr.position = new Vector3(
                    islandBasePos[i].x + px,
                    islandBasePos[i].y,
                    islandBasePos[i].z);
            }
        }

        // 2. Wide 2D Horizon Cloud Bands (gentle slow wind drift + lateral-only follow, fixed world Z!)
        if (farHorizonClouds != null && cloudBasePos != null)
        {
            float px = camDx * farLayerFollowFactor;
            for (int i = 0; i < farHorizonClouds.Length; i++)
            {
                Transform tr = farHorizonClouds[i];
                if (tr == null) continue;
                float gentleDrift = Mathf.Sin(t * 0.08f + i * 1.4f) * 3.8f;
                tr.position = new Vector3(
                    cloudBasePos[i].x + px + gentleDrift,
                    cloudBasePos[i].y,
                    cloudBasePos[i].z);
            }
        }

        // 3. Distant Decorative Sails (gentle motion strictly inside the sea visual band, fixed world Z!)
        if (midSeaSailboats != null && boatBasePos != null)
        {
            float px = camDx * midLayerFollowFactor;
            for (int i = 0; i < midSeaSailboats.Length; i++)
            {
                Transform tr = midSeaSailboats[i];
                if (tr == null) continue;
                float dir = (i % 2 == 0) ? 1f : -1f;
                float speed = 0.45f + (i % 3) * 0.14f;
                float span = 88f;
                float localSail = Mathf.Repeat((boatBasePos[i].x + 34f) + t * speed * dir, span) - 34f;
                float bob = Mathf.Sin(t * 1.35f + i * 1.7f) * 0.08f;
                float roll = Mathf.Sin(t * 1.15f + i * 1.3f) * 3.5f;

                tr.position = new Vector3(localSail + px, boatBasePos[i].y + bob, boatBasePos[i].z);
                if (boatBaseRot != null && i < boatBaseRot.Length)
                    tr.localRotation = boatBaseRot[i] * Quaternion.Euler(0f, 0f, roll);
            }
        }

        // 4. World-Space Shoreline Foam Ribbons anchored strictly to the sand edge
        if (shorelineFoamRibbons != null && foamBasePos != null)
        {
            for (int i = 0; i < shorelineFoamRibbons.Length; i++)
            {
                Transform tr = shorelineFoamRibbons[i];
                if (tr == null) continue;
                float phase = t * 1.15f + i * 0.85f;
                float wash = Mathf.Sin(phase);
                float zOffset = -wash * 0.55f;
                float xSway = Mathf.Cos(phase * 0.6f) * 0.22f;
                tr.position = new Vector3(foamBasePos[i].x + xSway, foamBasePos[i].y, foamBasePos[i].z + zOffset);
                if (foamBaseScale != null && i < foamBaseScale.Length)
                {
                    float widthPulse = 0.78f + 0.30f * (0.5f + 0.5f * wash);
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
