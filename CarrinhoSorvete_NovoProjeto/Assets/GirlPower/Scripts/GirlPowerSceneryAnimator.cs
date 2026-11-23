using UnityEngine;

/// <summary>
/// Lightweight ambient animation controller for the Matinhos golden-hour coastal scenery:
/// gentle palm breeze, cloud parallax drift, gliding seagulls, ocean sun-shimmer,
/// pulsing evening lamp halos, floating golden motes, and subtle NPC idle motion.
/// </summary>
public sealed class GirlPowerSceneryAnimator : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;

    [Header("Sky & Sea")]
    public Transform[] clouds;
    public SpriteRenderer[] seagulls;
    public Sprite seagullFrameA;
    public Sprite seagullFrameB;
    public SpriteRenderer[] waterShimmers;

    [Header("Foliage & Lights")]
    public Transform[] swayingPalms;
    public Transform[] swayingFestoons;
    public SpriteRenderer[] warmGlows;
    public SpriteRenderer[] goldenMotes;

    [Header("Living World")]
    public Transform[] idleCharacters;

    private Vector3[] cloudOrigins;
    private Vector3[] seagullOrigins;
    private Vector3[] shimmerOrigins;
    private Vector3[] moteOrigins;
    private Vector3[] charOrigins;
    private Color[] glowBaseColors;
    private Vector3[] glowBaseScales;
    private float cameraOriginX;

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        cameraOriginX = targetCamera != null ? targetCamera.transform.position.x : 0f;

        cloudOrigins = CapturePositions(clouds);
        seagullOrigins = CapturePositions(seagulls);
        shimmerOrigins = CapturePositions(waterShimmers);
        moteOrigins = CapturePositions(goldenMotes);
        charOrigins = CapturePositions(idleCharacters);

        if (warmGlows != null)
        {
            glowBaseColors = new Color[warmGlows.Length];
            glowBaseScales = new Vector3[warmGlows.Length];
            for (int i = 0; i < warmGlows.Length; i++)
            {
                if (warmGlows[i] == null) continue;
                glowBaseColors[i] = warmGlows[i].color;
                glowBaseScales[i] = warmGlows[i].transform.localScale;
            }
        }
    }

    private void Update()
    {
        float t = Time.time;
        float camDeltaX = targetCamera != null ? (targetCamera.transform.position.x - cameraOriginX) : 0f;

        UpdateClouds(t, camDeltaX);
        UpdateSeagulls(t);
        UpdateWaterShimmers(t);
        UpdatePalms(t);
        UpdateFestoons(t);
        UpdateWarmGlows(t);
        UpdateGoldenMotes(t);
        UpdateIdleCharacters(t);
    }

    private void UpdateClouds(float t, float camDeltaX)
    {
        if (clouds == null || cloudOrigins == null) return;
        for (int i = 0; i < clouds.Length; i++)
        {
            Transform tr = clouds[i];
            if (tr == null) continue;
            float speed = 0.08f + (i % 3) * 0.035f;
            float span = 44f;
            float wrapped = Mathf.Repeat((cloudOrigins[i].x + 12f) + t * speed, span) - 12f;
            float parallax = camDeltaX * 0.14f;
            float bob = Mathf.Sin(t * 0.35f + i * 1.4f) * 0.05f;
            tr.position = new Vector3(wrapped + parallax, cloudOrigins[i].y + bob, cloudOrigins[i].z);
        }
    }

    private void UpdateSeagulls(float t)
    {
        if (seagulls == null || seagullOrigins == null) return;
        for (int i = 0; i < seagulls.Length; i++)
        {
            SpriteRenderer sr = seagulls[i];
            if (sr == null) continue;
            float dir = (i % 2 == 0) ? 1f : -1f;
            float speed = 0.55f + (i % 3) * 0.18f;
            float span = 42f;
            float x = Mathf.Repeat((seagullOrigins[i].x + 10f) + t * speed * dir, span) - 10f;
            float y = seagullOrigins[i].y + Mathf.Sin(t * 1.4f + i * 2.1f) * 0.16f;
            sr.transform.position = new Vector3(x, y, seagullOrigins[i].z);
            sr.flipX = dir < 0f;
            if (seagullFrameA != null && seagullFrameB != null)
            {
                bool down = Mathf.Sin(t * 5.2f + i * 1.9f) > 0.1f;
                sr.sprite = down ? seagullFrameB : seagullFrameA;
            }
        }
    }

    private void UpdateWaterShimmers(float t)
    {
        if (waterShimmers == null || shimmerOrigins == null) return;
        for (int i = 0; i < waterShimmers.Length; i++)
        {
            SpriteRenderer sr = waterShimmers[i];
            if (sr == null) continue;
            float drift = Mathf.Sin(t * 0.85f + i * 1.3f) * 0.28f;
            float bob = Mathf.Cos(t * 1.1f + i * 1.7f) * 0.03f;
            sr.transform.position = new Vector3(shimmerOrigins[i].x + drift, shimmerOrigins[i].y + bob, shimmerOrigins[i].z);
            Color c = sr.color;
            c.a = 0.42f + 0.36f * (0.5f + 0.5f * Mathf.Sin(t * 1.9f + i * 2.3f));
            sr.color = c;
        }
    }

    private void UpdatePalms(float t)
    {
        if (swayingPalms == null) return;
        for (int i = 0; i < swayingPalms.Length; i++)
        {
            Transform tr = swayingPalms[i];
            if (tr == null) continue;
            float angle = Mathf.Sin(t * 1.15f + i * 1.35f) * 1.6f + Mathf.Sin(t * 2.4f + i * 0.7f) * 0.45f;
            tr.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void UpdateFestoons(float t)
    {
        if (swayingFestoons == null) return;
        for (int i = 0; i < swayingFestoons.Length; i++)
        {
            Transform tr = swayingFestoons[i];
            if (tr == null) continue;
            float angle = Mathf.Sin(t * 1.5f + i * 1.1f) * 0.9f;
            tr.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void UpdateWarmGlows(float t)
    {
        if (warmGlows == null || glowBaseColors == null) return;
        for (int i = 0; i < warmGlows.Length; i++)
        {
            SpriteRenderer sr = warmGlows[i];
            if (sr == null) continue;
            float pulse = 0.88f + 0.12f * Mathf.Sin(t * 2.1f + i * 1.7f);
            Color c = glowBaseColors[i];
            c.a = glowBaseColors[i].a * pulse;
            sr.color = c;
            sr.transform.localScale = glowBaseScales[i] * (0.96f + 0.04f * pulse);
        }
    }

    private void UpdateGoldenMotes(float t)
    {
        if (goldenMotes == null || moteOrigins == null) return;
        for (int i = 0; i < goldenMotes.Length; i++)
        {
            SpriteRenderer sr = goldenMotes[i];
            if (sr == null) continue;
            float dx = Mathf.Sin(t * 0.75f + i * 1.9f) * 0.35f;
            float dy = Mathf.Cos(t * 0.95f + i * 2.4f) * 0.22f;
            sr.transform.position = new Vector3(moteOrigins[i].x + dx, moteOrigins[i].y + dy, moteOrigins[i].z);
            Color c = sr.color;
            c.a = 0.25f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 2.6f + i * 1.5f));
            sr.color = c;
        }
    }

    private void UpdateIdleCharacters(float t)
    {
        if (idleCharacters == null || charOrigins == null) return;
        for (int i = 0; i < idleCharacters.Length; i++)
        {
            Transform tr = idleCharacters[i];
            if (tr == null) continue;
            float bob = Mathf.Sin(t * 2.4f + i * 1.6f) * 0.025f;
            tr.position = new Vector3(charOrigins[i].x, charOrigins[i].y + bob, charOrigins[i].z);
        }
    }

    private static Vector3[] CapturePositions(Transform[] items)
    {
        if (items == null) return null;
        Vector3[] result = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) result[i] = items[i].position;
        return result;
    }

    private static Vector3[] CapturePositions(SpriteRenderer[] items)
    {
        if (items == null) return null;
        Vector3[] result = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
            if (items[i] != null) result[i] = items[i].transform.position;
        return result;
    }
}
