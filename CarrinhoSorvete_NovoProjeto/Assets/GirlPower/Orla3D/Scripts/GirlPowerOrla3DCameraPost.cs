using UnityEngine;

/// <summary>
/// Lightweight built-in Post-Processing for Baía do Avesso:
/// Controls exposure, ACES filmic tonemapping (eliminating overexposed whites),
/// cozy contrast/saturation, and subtle vignette without requiring external packages.
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public sealed class GirlPowerOrla3DCameraPost : MonoBehaviour
{
    [Range(0.55f, 1.45f)] public float exposure = 0.90f;
    [Range(0.85f, 1.30f)] public float contrast = 1.08f;
    [Range(0.75f, 1.35f)] public float saturation = 1.12f;
    [Range(0.0f, 0.35f)] public float vignetteStrength = 0.16f;
    public Color warmTint = new Color(1.02f, 0.98f, 0.94f, 1f);
    public bool postEnabled = true;

    private Material postMat;

    private void OnEnable()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
            cam.depthTextureMode |= DepthTextureMode.Depth;
        EnsureMaterial();
    }

    private void OnDisable()
    {
        if (postMat != null)
        {
            if (Application.isPlaying) Destroy(postMat);
            else DestroyImmediate(postMat);
            postMat = null;
        }
    }

    public void ApplyAtmosphere(float exp, float sat, float vig, Color tint)
    {
        exposure = exp;
        saturation = sat;
        vignetteStrength = vig;
        warmTint = tint;
    }

    private void EnsureMaterial()
    {
        if (postMat != null) return;
        Shader sh = Shader.Find("Hidden/GirlPower/OrlaPostTone");
        if (sh != null && sh.isSupported)
        {
            postMat = new Material(sh);
            postMat.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!postEnabled)
        {
            Graphics.Blit(src, dst);
            return;
        }

        EnsureMaterial();
        if (postMat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }

        postMat.SetFloat("_Exposure", exposure);
        postMat.SetFloat("_Contrast", contrast);
        postMat.SetFloat("_Saturation", saturation);
        postMat.SetFloat("_VignetteStrength", vignetteStrength);
        postMat.SetColor("_WarmTint", warmTint);
        Graphics.Blit(src, dst, postMat);
    }
}
