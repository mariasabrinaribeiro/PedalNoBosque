using UnityEngine;

/// <summary>
/// Ambiente vivo e paralaxe top-down sutil para o Mundo de Doces em "Girl Power - Sorvete":
/// - Paralaxe aérea de nuvens de algodão-doce translúcidas acompanhando a câmera com profundidade real
/// - Balanço suave de brisa em árvores e pirulitos
/// - Brilhos pulsantes em praças e postes
/// - Partículas cintilantes e donuts flutuantes
/// </summary>
public sealed class CandyWorldAmbient : MonoBehaviour
{
    public Transform[] swayingProps;
    public SpriteRenderer[] pulsingGlows;
    public SpriteRenderer[] floatingSparkles;
    public Transform[] bobbingPickups;
    public Transform[] parallaxClouds;

    [Header("Paralaxe Top-Down")]
    public float cloudParallaxFactor = 0.22f;
    public Vector2 cloudDriftSpeed = new Vector2(0.35f, 0.08f);
    public Vector2 cloudWrapBounds = new Vector2(36f, 26f);

    private Vector3[] sparkleOrigins;
    private Vector3[] pickupOrigins;
    private Vector3[] cloudOrigins;
    private Color[] glowBaseColors;
    private Vector3[] glowBaseScales;
    private Transform camTransform;
    private Vector3 camStartPos;

    private void Start()
    {
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
            camStartPos = camTransform.position;
        }

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
        if (parallaxClouds != null)
        {
            cloudOrigins = new Vector3[parallaxClouds.Length];
            for (int i = 0; i < parallaxClouds.Length; i++)
                if (parallaxClouds[i] != null) cloudOrigins[i] = parallaxClouds[i].transform.position;
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
                float angle = Mathf.Sin(t * 1.4f + i * 1.15f) * 2.0f;
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
                c.a = Mathf.Min(0.35f, glowBaseColors[i].a * pulse);
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
                float dx = Mathf.Sin(t * 0.9f + i * 2.1f) * 0.42f;
                float dy = Mathf.Cos(t * 1.1f + i * 1.7f) * 0.32f;
                sr.transform.position = new Vector3(sparkleOrigins[i].x + dx, sparkleOrigins[i].y + dy, sparkleOrigins[i].z);
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.8f + i) * 18f);
                Color c = sr.color;
                c.a = 0.12f + 0.22f * (0.5f + 0.5f * Mathf.Sin(t * 3.1f + i * 1.9f));
                sr.color = c;
            }
        }

        if (bobbingPickups != null && pickupOrigins != null)
        {
            for (int i = 0; i < bobbingPickups.Length; i++)
            {
                Transform tr = bobbingPickups[i];
                if (tr == null) continue;
                float bob = Mathf.Sin(t * 3.2f + i * 1.3f) * 0.12f;
                tr.position = new Vector3(pickupOrigins[i].x, pickupOrigins[i].y + bob, pickupOrigins[i].z);
                tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f + i) * 7f);
            }
        }
    }

    private void LateUpdate()
    {
        if (parallaxClouds == null || cloudOrigins == null) return;
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
            camStartPos = camTransform.position;
        }
        if (camTransform == null) return;

        Vector3 camDelta = camTransform.position - camStartPos;
        float t = Time.time;

        for (int i = 0; i < parallaxClouds.Length; i++)
        {
            Transform tr = parallaxClouds[i];
            if (tr == null) continue;

            // Camadas mais altas se deslocam mais rápido em relação à câmera (efeito paralaxe 2.5D top-down)
            float layerMul = 0.75f + (i % 3) * 0.25f;
            float px = cloudOrigins[i].x - camDelta.x * cloudParallaxFactor * layerMul + t * cloudDriftSpeed.x * layerMul;
            float py = cloudOrigins[i].y - camDelta.y * cloudParallaxFactor * layerMul + t * cloudDriftSpeed.y * layerMul;

            // Mantém as nuvens sempre distribuídas ao redor da câmera sem desaparecer do mapa
            float dx = Mathf.Repeat(px - camTransform.position.x + cloudWrapBounds.x, cloudWrapBounds.x * 2f) - cloudWrapBounds.x;
            float dy = Mathf.Repeat(py - camTransform.position.y + cloudWrapBounds.y, cloudWrapBounds.y * 2f) - cloudWrapBounds.y;

            tr.position = new Vector3(camTransform.position.x + dx, camTransform.position.y + dy, 0f);
        }
    }
}
