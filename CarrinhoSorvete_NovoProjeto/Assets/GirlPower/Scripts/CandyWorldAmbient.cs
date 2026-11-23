using UnityEngine;

/// <summary>
/// Lightweight ambient animation for the Candy World in "Girl Power - Sorvete":
/// gentle breeze sway on cotton-candy trees & giant lollipops, pulsing candy halos
/// on delivery plazas & streetlamps, bobbing donut/ice-cream pickups, and floating sparkles.
/// </summary>
public sealed class CandyWorldAmbient : MonoBehaviour
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
                if (tr == null) continue;
                float angle = Mathf.Sin(t * 1.4f + i * 1.15f) * 2.1f;
                tr.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        if (pulsingGlows != null && glowBaseColors != null)
        {
            for (int i = 0; i < pulsingGlows.Length; i++)
            {
                SpriteRenderer sr = pulsingGlows[i];
                if (sr == null) continue;
                float pulse = 0.86f + 0.14f * Mathf.Sin(t * 2.4f + i * 1.5f);
                Color c = glowBaseColors[i];
                c.a = glowBaseColors[i].a * pulse;
                sr.color = c;
                sr.transform.localScale = glowBaseScales[i] * (0.94f + 0.08f * pulse);
            }
        }

        if (floatingSparkles != null && sparkleOrigins != null)
        {
            for (int i = 0; i < floatingSparkles.Length; i++)
            {
                SpriteRenderer sr = floatingSparkles[i];
                if (sr == null) continue;
                float dx = Mathf.Sin(t * 0.9f + i * 2.1f) * 0.45f;
                float dy = Mathf.Cos(t * 1.1f + i * 1.7f) * 0.35f;
                sr.transform.position = new Vector3(sparkleOrigins[i].x + dx, sparkleOrigins[i].y + dy, sparkleOrigins[i].z);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f + i) * 20f);
                Color c = sr.color;
                c.a = 0.25f + 0.70f * (0.5f + 0.5f * Mathf.Sin(t * 3.1f + i * 1.9f));
                sr.color = c;
            }
        }

        if (bobbingPickups != null && pickupOrigins != null)
        {
            for (int i = 0; i < bobbingPickups.Length; i++)
            {
                Transform tr = bobbingPickups[i];
                if (tr == null) continue;
                float bob = Mathf.Sin(t * 3.2f + i * 1.3f) * 0.14f;
                tr.position = new Vector3(pickupOrigins[i].x, pickupOrigins[i].y + bob, pickupOrigins[i].z);
                tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f + i) * 8f);
            }
        }
    }
}
