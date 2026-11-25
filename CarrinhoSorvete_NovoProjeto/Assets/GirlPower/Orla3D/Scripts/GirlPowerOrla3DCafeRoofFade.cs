using UnityEngine;

/// <summary>
/// Seamless no-loading-screen interior entry for Café das Fadas (or any enterable building):
/// When the player walks through the open doorway into the building footprint,
/// smoothly fades out the roof mesh and hides it cleanly so the decorated interior is 100% visible!
/// </summary>
public sealed class GirlPowerOrla3DCafeRoofFade : MonoBehaviour
{
    public Transform player;
    public Transform roofObject;
    public Vector3 buildingCenter = new Vector3(5.6f, 0f, -8.2f);
    public Vector2 halfBoundsXZ = new Vector2(3.6f, 2.8f);
    public float fadeSpeed = 6.5f;

    private MeshRenderer[] roofRenderers;
    private float currentAlpha = 1f;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (roofObject != null)
            roofRenderers = roofObject.GetComponentsInChildren<MeshRenderer>(true);
    }

    private void Update()
    {
        if (player == null || roofRenderers == null || roofRenderers.Length == 0) return;

        Vector3 p = player.position;
        bool inside = Mathf.Abs(p.x - buildingCenter.x) <= halfBoundsXZ.x
                   && Mathf.Abs(p.z - buildingCenter.z) <= halfBoundsXZ.y;

        float target = inside ? 0f : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, target, Time.deltaTime * fadeSpeed);
        bool visible = currentAlpha > 0.04f;

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
