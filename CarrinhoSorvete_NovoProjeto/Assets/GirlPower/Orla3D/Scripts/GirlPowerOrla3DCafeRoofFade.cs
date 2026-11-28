using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strictly-interior smooth roof fade controller for enterable buildings (Café das Fadas):
/// - Keeps the roof 100% OPAQUE (ZWrite On) whenever the player is outside — ZERO translucent artifacts!
/// - Uses an internal isTrigger BoxCollider inset by 0.5m from every exterior wall so walking along
///   the outside sidewalk never triggers roof fade, and swaps to fade materials ONLY during interior entry.
/// </summary>
public sealed class GirlPowerOrla3DCafeRoofFade : MonoBehaviour
{
    public Transform player;
    public Transform roofObject;
    public Camera targetCamera;
    public Vector3 buildingCenter = new Vector3(6.4f, 0f, -14.2f);
    public Vector2 halfBoundsXZ = new Vector2(3.05f, 2.05f);
    [Tooltip("Strict interior inset margin from exterior walls (minimum 0.5m) so outer sidewalk never triggers fade.")]
    public float interiorWallMargin = 0.50f;
    public float fadeSpeed = 8.0f;

    private sealed class RoofSlot
    {
        public MeshRenderer renderer;
        public Material[] opaqueMats;
        public Material[] fadeMats;
        public bool usingFade;
        public bool toggleOnly;
    }

    private readonly List<RoofSlot> slots = new List<RoofSlot>();
    private readonly Dictionary<Material, Material> fadeCache = new Dictionary<Material, Material>();
    private BoxCollider interiorTrigger;
    private float currentAlpha = 1f;
    private bool isInsideInterior;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        Shader fadeShader = Shader.Find("GirlPower/OrlaACNHCozyFade");
        if (roofObject != null)
        {
            foreach (MeshRenderer mr in roofObject.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;
                bool isTmp = mr.GetComponent<TMPro.TMP_Text>() != null;
                if (isTmp)
                {
                    slots.Add(new RoofSlot { renderer = mr, opaqueMats = mr.sharedMaterials, fadeMats = mr.sharedMaterials, usingFade = false, toggleOnly = true });
                    continue;
                }
                Material[] opaque = mr.sharedMaterials;
                Material[] fade = new Material[opaque.Length];
                for (int i = 0; i < opaque.Length; i++)
                {
                    if (opaque[i] == null) continue;
                    if (fadeShader != null && fadeShader.isSupported)
                    {
                        if (!fadeCache.TryGetValue(opaque[i], out Material fm))
                        {
                            fm = new Material(opaque[i]);
                            fm.shader = fadeShader;
                            if (fm.HasProperty("_OccluderEnabled")) fm.SetFloat("_OccluderEnabled", 0f);
                            fadeCache[opaque[i]] = fm;
                        }
                        fade[i] = fm;
                    }
                    else
                    {
                        fade[i] = opaque[i];
                    }
                }
                slots.Add(new RoofSlot { renderer = mr, opaqueMats = opaque, fadeMats = fade, usingFade = false, toggleOnly = false });
            }
        }

        EnsureStrictInteriorTrigger();
    }

    private void OnDestroy()
    {
        foreach (var kv in fadeCache)
            if (kv.Value != null) Destroy(kv.Value);
        fadeCache.Clear();
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
        if (player == null || slots.Count == 0) return;

        float margin = Mathf.Max(0.50f, interiorWallMargin);
        float innerHalfX = Mathf.Max(0.40f, halfBoundsXZ.x - margin);
        float innerHalfZ = Mathf.Max(0.40f, halfBoundsXZ.y - margin);

        float hyst = isInsideInterior ? 0.14f : 0f;
        Vector3 p = player.position;
        isInsideInterior = Mathf.Abs(p.x - buildingCenter.x) <= (innerHalfX + hyst)
                        && Mathf.Abs(p.z - buildingCenter.z) <= (innerHalfZ + hyst);

        // Also fade roof smoothly if it sits physically between the camera and the protagonist
        // (e.g. when walking right outside the Lanchonete doorway or between nearby stalls!).
        bool blockingCameraView = false;
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (!isInsideInterior && cam != null && roofObject != null)
        {
            Vector3 camPos = cam.transform.position;
            Vector3 focus = p + Vector3.up * 1.05f;
            Vector3 seg = focus - camPos;
            float segLen = seg.magnitude;
            if (segLen > 0.6f)
            {
                Ray ray = new Ray(camPos, seg / segLen);
                Bounds roofBounds = new Bounds(
                    new Vector3(buildingCenter.x, buildingCenter.y + 2.4f, buildingCenter.z),
                    new Vector3((halfBoundsXZ.x + 1.0f) * 2f, 3.6f, (halfBoundsXZ.y + 1.0f) * 2f));
                if (roofBounds.IntersectRay(ray, out float hitDist) && hitDist > 0.35f && hitDist < segLen - 0.35f)
                    blockingCameraView = true;
            }
        }

        float targetAlpha = isInsideInterior ? 0.0f : (blockingCameraView ? 0.14f : 1.0f);
        float t = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
        if (Mathf.Abs(currentAlpha - targetAlpha) < 0.01f)
            currentAlpha = targetAlpha;

        bool visible = currentAlpha > 0.02f;
        bool needFade = currentAlpha < 0.985f;

        for (int i = 0; i < slots.Count; i++)
        {
            RoofSlot s = slots[i];
            if (s.renderer == null) continue;
            if (s.toggleOnly)
            {
                bool showText = currentAlpha > 0.35f;
                if (s.renderer.enabled != showText) s.renderer.enabled = showText;
                continue;
            }
            if (s.renderer.enabled != visible) s.renderer.enabled = visible;
            if (!visible) continue;

            if (needFade)
            {
                if (!s.usingFade)
                {
                    s.renderer.sharedMaterials = s.fadeMats;
                    s.usingFade = true;
                }
                s.renderer.GetPropertyBlock(mpb);
                Color col = s.opaqueMats.Length > 0 && s.opaqueMats[0] != null && s.opaqueMats[0].HasProperty("_Color")
                    ? s.opaqueMats[0].GetColor("_Color")
                    : Color.white;
                col.a = currentAlpha;
                mpb.SetColor("_Color", col);
                s.renderer.SetPropertyBlock(mpb);
            }
            else if (s.usingFade)
            {
                s.renderer.sharedMaterials = s.opaqueMats;
                s.renderer.SetPropertyBlock(null);
                s.usingFade = false;
            }
        }
    }
}
