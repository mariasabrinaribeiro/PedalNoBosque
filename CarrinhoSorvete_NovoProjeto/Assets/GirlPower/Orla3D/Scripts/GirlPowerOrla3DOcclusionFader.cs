using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zero-artifact occlusion fader:
/// - Every stall, table, cart, kiosk, and house stays 100% OPAQUE (ZWrite On, OrlaACNHCozy) by default,
///   completely eliminating translucent rectangle artifacts, alpha-sorting glitches, and mobile overdraw!
/// - Only when an elevated overhead cover (*CanopyFade, *RoofFade, HouseRoof) actually intersects the
///   camera-to-player line of sight does this controller temporarily swap THAT single renderer to a smooth
///   fade material, and immediately restores its crisp opaque material once the protagonist steps clear.
/// </summary>
public sealed class GirlPowerOrla3DOcclusionFader : MonoBehaviour
{
    public Transform player;
    public Camera targetCamera;
    public Transform[] occluderRoots;
    public float cutoutRadius = 1.45f;
    public float fadedAlpha = 0.20f;
    public float fadeSpeed = 10.0f;

    private static readonly int PropPlayerPos = Shader.PropertyToID("_BaiaPlayerPos");
    private static readonly int PropCamPos = Shader.PropertyToID("_BaiaCamPos");
    private static readonly int PropActive = Shader.PropertyToID("_BaiaOccluderActive");
    private static readonly int PropEnabled = Shader.PropertyToID("_OccluderEnabled");
    private static readonly int PropRadius = Shader.PropertyToID("_OccluderRadius");
    private static readonly int PropColor = Shader.PropertyToID("_Color");

    private sealed class CoverEntry
    {
        public MeshRenderer renderer;
        public Material[] opaqueMats;
        public Material[] fadeMats;
        public float alpha = 1f;
        public bool usingFade;
        public bool toggleOnly;
    }

    private readonly List<CoverEntry> covers = new List<CoverEntry>();
    private readonly Dictionary<Material, Material> fadeCache = new Dictionary<Material, Material>();
    private Shader fadeShader;
    private MaterialPropertyBlock mpb;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        fadeShader = Shader.Find("GirlPower/OrlaACNHCozyFade");
        mpb = new MaterialPropertyBlock();
        CollectOverheadCovers();
        Shader.SetGlobalFloat(PropActive, 0f);
    }

    private void OnDisable()
    {
        RestoreAllOpaque();
        Shader.SetGlobalFloat(PropActive, 0f);
    }

    private void OnDestroy()
    {
        RestoreAllOpaque();
        foreach (var kv in fadeCache)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        fadeCache.Clear();
    }

    private void CollectOverheadCovers()
    {
        covers.Clear();
        if (occluderRoots == null || fadeShader == null || !fadeShader.isSupported) return;

        for (int i = 0; i < occluderRoots.Length; i++)
        {
            Transform root = occluderRoots[i];
            if (root == null) continue;
            foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;
                bool isTmp = mr.GetComponent<TMPro.TMP_Text>() != null;
                Material[] shared = mr.sharedMaterials;
                if (shared == null || shared.Length == 0) continue;

                if (isTmp)
                {
                    covers.Add(new CoverEntry
                    {
                        renderer = mr,
                        opaqueMats = shared,
                        fadeMats = shared,
                        alpha = 1f,
                        usingFade = false,
                        toggleOnly = true
                    });
                    continue;
                }

                // Fade ANY commercial stall, lanchonete wall, umbrella, canopy, or building standing
                // directly between the camera and the protagonist — while keeping everything 100% opaque otherwise!
                Material[] fadeList = new Material[shared.Length];
                for (int m = 0; m < shared.Length; m++)
                {
                    Material src = shared[m];
                    fadeList[m] = src != null ? GetOrCreateFadeMat(src) : null;
                }

                covers.Add(new CoverEntry
                {
                    renderer = mr,
                    opaqueMats = shared,
                    fadeMats = fadeList,
                    alpha = 1f,
                    usingFade = false,
                    toggleOnly = false
                });
            }
        }
    }

    private Material GetOrCreateFadeMat(Material source)
    {
        if (fadeCache.TryGetValue(source, out Material existing)) return existing;
        Material fm = new Material(source);
        fm.name = source.name + "_RuntimeFade";
        fm.shader = fadeShader;
        if (fm.HasProperty(PropEnabled)) fm.SetFloat(PropEnabled, 0f);
        fadeCache[source] = fm;
        return fm;
    }

    private void RestoreAllOpaque()
    {
        for (int i = 0; i < covers.Count; i++)
        {
            CoverEntry c = covers[i];
            if (c.renderer != null && c.usingFade)
            {
                c.renderer.sharedMaterials = c.opaqueMats;
                c.renderer.SetPropertyBlock(null);
                c.usingFade = false;
                c.alpha = 1f;
            }
        }
    }

    private void LateUpdate()
    {
        if (player == null || targetCamera == null || covers.Count == 0) return;

        Vector3 camPos = targetCamera.transform.position;
        Vector3 focusMid = player.position + Vector3.up * 0.90f;
        Vector3 focusHead = player.position + Vector3.up * 1.48f;
        Vector3 segMid = focusMid - camPos;
        float segMidLen = segMid.magnitude;
        if (segMidLen < 0.5f) return;
        Vector3 rayMidDir = segMid / segMidLen;
        Vector3 segHead = focusHead - camPos;
        float segHeadLen = segHead.magnitude;
        Vector3 rayHeadDir = segHeadLen > 0.1f ? (segHead / segHeadLen) : rayMidDir;

        Shader.SetGlobalVector(PropPlayerPos, new Vector4(focusMid.x, focusMid.y, focusMid.z, 1f));
        Shader.SetGlobalVector(PropCamPos, new Vector4(camPos.x, camPos.y, camPos.z, 1f));
        Shader.SetGlobalFloat(PropActive, 0f);

        float blend = 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime);

        for (int i = 0; i < covers.Count; i++)
        {
            CoverEntry c = covers[i];
            if (c.renderer == null || !c.renderer.gameObject.activeInHierarchy) continue;

            Bounds b = c.renderer.bounds;
            bool blocking = IsBoundsBlockingRay(b, camPos, rayMidDir, segMidLen, cutoutRadius)
                         || IsBoundsBlockingRay(b, camPos, rayHeadDir, segHeadLen, cutoutRadius * 0.85f);
            float targetAlpha = blocking ? fadedAlpha : 1f;
            c.alpha = Mathf.Lerp(c.alpha, targetAlpha, blend);
            if (Mathf.Abs(c.alpha - targetAlpha) < 0.015f)
                c.alpha = targetAlpha;

            if (c.toggleOnly)
            {
                bool showSign = c.alpha > 0.55f;
                if (c.renderer.enabled != showSign) c.renderer.enabled = showSign;
                continue;
            }

            if (c.alpha < 0.985f)
            {
                if (!c.usingFade)
                {
                    c.renderer.sharedMaterials = c.fadeMats;
                    c.usingFade = true;
                }
                c.renderer.GetPropertyBlock(mpb);
                Color baseCol = c.opaqueMats.Length > 0 && c.opaqueMats[0] != null && c.opaqueMats[0].HasProperty(PropColor)
                    ? c.opaqueMats[0].GetColor(PropColor)
                    : Color.white;
                baseCol.a = c.alpha;
                mpb.SetColor(PropColor, baseCol);
                mpb.SetFloat(PropEnabled, 0f);
                mpb.SetFloat(PropRadius, cutoutRadius);
                c.renderer.SetPropertyBlock(mpb);
            }
            else if (c.usingFade)
            {
                // Restore 100% solid opaque ZWrite material the instant the cover clears the camera line!
                c.renderer.sharedMaterials = c.opaqueMats;
                c.renderer.SetPropertyBlock(null);
                c.usingFade = false;
            }
        }
    }

    private static bool IsBoundsBlockingRay(Bounds b, Vector3 origin, Vector3 dir, float maxLen, float pad)
    {
        // Exact Ray-vs-Expanded-AABB intersection strictly between camera and protagonist!
        Bounds expanded = new Bounds(b.center, b.size + new Vector3(pad * 0.55f, 0.30f, pad * 0.55f));
        Ray ray = new Ray(origin, dir);
        if (!expanded.IntersectRay(ray, out float hitDist)) return false;
        return hitDist >= 0.35f && hitDist < (maxLen - 0.55f);
    }
}
