using UnityEngine;

/// <summary>
/// Strictly-interior smooth roof fade controller for enterable buildings (Café das Fadas, Batata & Mar):
/// - Places an internal isTrigger BoxCollider inset by 0.5m from every exterior wall so walking along
///   the outside sidewalk or brushing an outer wall NEVER triggers roof fade!
/// - Uses smooth Mathf.Lerp alpha blending + hysteresis so the roof never flickers or pops.
/// </summary>
public sealed class GirlPowerOrla3DCafeRoofFade : MonoBehaviour
{
    public Transform player;
    public Transform roofObject;
    public Vector3 buildingCenter = new Vector3(5.6f, 0f, -8.2f);
    public Vector2 halfBoundsXZ = new Vector2(3.05f, 2.05f);
    [Tooltip("Strict interior inset margin from exterior walls (minimum 0.5m) so outer sidewalk never triggers fade.")]
    public float interiorWallMargin = 0.50f;
    public float fadeSpeed = 7.5f;

    private MeshRenderer[] roofRenderers;
    private BoxCollider interiorTrigger;
    private float currentAlpha = 1f;
    private bool isInsideInterior;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (roofObject != null)
            roofRenderers = roofObject.GetComponentsInChildren<MeshRenderer>(true);

        EnsureStrictInteriorTrigger();
    }

    private void EnsureStrictInteriorTrigger()
    {
        float innerHalfX = Mathf.Max(0.45f, halfBoundsXZ.x - Mathf.Max(0.50f, interiorWallMargin));
        float innerHalfZ = Mathf.Max(0.45f, halfBoundsXZ.y - Mathf.Max(0.50f, interiorWallMargin));

        interiorTrigger = GetComponent<BoxCollider>();
        if (interiorTrigger == null)
            interiorTrigger = gameObject.AddComponent<BoxCollider>();

        interiorTrigger.isTrigger = true;
        transform.position = new Vector3(buildingCenter.x, 0f, buildingCenter.z);
        interiorTrigger.center = new Vector3(0f, 1.5f, 0f);
        interiorTrigger.size = new Vector3(innerHalfX * 2f, 3.0f, innerHalfZ * 2f);
    }

    private void Update()
    {
        if (player == null || roofRenderers == null || roofRenderers.Length == 0) return;

        float margin = Mathf.Max(0.50f, interiorWallMargin);
        float innerHalfX = Mathf.Max(0.40f, halfBoundsXZ.x - margin);
        float innerHalfZ = Mathf.Max(0.40f, halfBoundsXZ.y - margin);

        // Hysteresis (0.14m) prevents rapid on/off flickering right at the interior threshold
        float hyst = isInsideInterior ? 0.14f : 0f;
        Vector3 p = player.position;
        isInsideInterior = Mathf.Abs(p.x - buildingCenter.x) <= (innerHalfX + hyst)
                        && Mathf.Abs(p.z - buildingCenter.z) <= (innerHalfZ + hyst);

        float targetAlpha = isInsideInterior ? 0.0f : 1.0f;
        // Smooth exponential Mathf.Lerp alpha transition (zero popping or flickering!)
        float t = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
        if (Mathf.Abs(currentAlpha - targetAlpha) < 0.005f)
            currentAlpha = targetAlpha;

        bool visible = currentAlpha > 0.02f;
        for (int i = 0; i < roofRenderers.Length; i++)
        {
            MeshRenderer mr = roofRenderers[i];
            if (mr == null) continue;
            if (mr.enabled != visible) mr.enabled = visible;
            if (visible)
            {
                mr.GetPropertyBlock(mpb);
                Color col = mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_Color")
                    ? mr.sharedMaterial.GetColor("_Color")
                    : Color.white;
                col.a = currentAlpha;
                mpb.SetColor("_Color", col);
                mr.SetPropertyBlock(mpb);
            }
        }
    }
}
