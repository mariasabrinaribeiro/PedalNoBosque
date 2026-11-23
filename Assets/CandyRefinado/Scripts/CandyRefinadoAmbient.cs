using UnityEngine;

/// <summary>
/// Gentle ambient animation for "Girl Power - Cenario Refinado":
/// subtle breeze on cotton-candy trees & lollipops, warm pulse on streetlamps & plazas,
/// floating pastel sugar sparkles, and gentle bob on collectible donuts.
/// </summary>
public sealed class CandyRefinadoAmbient : MonoBehaviour
{
    public Transform[] swayingProps;
    public SpriteRenderer[] pulsingGlows;
    public SpriteRenderer[] floatingSparkles;
    public Transform[] bobbingPickups;

    private Vector3[] sparkleOrigins;
    private Vector3[] pickupOrigins;
    private Color[] glowBaseColors;
    private Vector3[] glowBaseScales;

    private void Start()
    {
        if (floatingSparkles != null)
        {
            sparkleOrigins = new Vector3[floatingSparkles.Length];
            for (int i = 0; i < floatingSparkles.Length; i++)
                if (floatingSparkles[i] != null) sparkleOrigins[i] = floatingSparkles[i].transform.position;
        }
        if (bobbingPickups != null)
        {
            pickupOrigins = new Vector3[bobbingPickups.Length];
            for (int i = 0; i < bobbingPickups.Length; i++)
                if (bobbingPickups[i] != null) pickupOrigins[i] = bobbingPickups[i].transform.position;
        }
        if (pulsingGlows != null)
        {
            glowBaseColors = new Color[pulsingGlows.Length];
            glowBaseScales = new Vector3[pulsingGlows.Length];
            for (int i = 0; i < pulsingGlows.Length; i++)
            {
                if (pulsingGlows[i] == null) continue;
                glowBaseColors[i] = pulsingGlows[i].color;
                glowBaseScales[i] = pulsingGlows[i].transform.localScale;
            }
        }
    }

    private void Update()
    {
        float t = Time.time;
        if (swayingProps != null)
        {
            for (int i = 0; i < swayingProps.Length; i++)
            {
                Transform tr = swayingProps[i];
                if (tr != null) tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.35f + i * 1.1f) * 1.9f);
            }
        }
        if (pulsingGlows != null && glowBaseColors != null)
        {
            for (int i = 0; i < pulsingGlows.Length; i++)
            {
                SpriteRenderer sr = pulsingGlows[i];
                if (sr == null) continue;
                float p = 0.87f + 0.13f * Mathf.Sin(t * 2.2f + i * 1.4f);
                Color c = glowBaseColors[i]; c.a = glowBaseColors[i].a * p;
                sr.color = c;
                sr.transform.localScale = glowBaseScales[i] * (0.95f + 0.06f * p);
            }
        }
        if (floatingSparkles != null && sparkleOrigins != null)
        {
            for (int i = 0; i < floatingSparkles.Length; i++)
            {
                SpriteRenderer sr = floatingSparkles[i];
                if (sr == null) continue;
                sr.transform.position = new Vector3(
                    sparkleOrigins[i].x + Mathf.Sin(t * 0.85f + i * 1.9f) * 0.4f,
                    sparkleOrigins[i].y + Mathf.Cos(t * 1.05f + i * 1.5f) * 0.3f,
                    sparkleOrigins[i].z);
                Color c = sr.color;
                c.a = 0.25f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 2.8f + i * 1.7f));
                sr.color = c;
            }
        }
        if (bobbingPickups != null && pickupOrigins != null)
        {
            for (int i = 0; i < bobbingPickups.Length; i++)
            {
                Transform tr = bobbingPickups[i];
                if (tr == null) continue;
                tr.position = new Vector3(pickupOrigins[i].x, pickupOrigins[i].y + Mathf.Sin(t * 3.0f + i * 1.2f) * 0.12f, pickupOrigins[i].z);
            }
        }
    }
}
