using UnityEngine;

public class EngineSound : MonoBehaviour
{
    private float phase = 0f;
    private float sampleRate = 44100f;
    public float baseFrequency = 50f; 
    public float frequencyMultiplier = 1f;
    public float volume = 0.05f;

    private Rigidbody2D rb;
    private DriverPro driver;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        driver = GetComponent<DriverPro>();

        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        // Assign a dummy blank clip to trigger OnAudioFilterRead callback
        audioSource.clip = AudioClip.Create("EngineHumDummy", 44100, 1, 44100, false);
        audioSource.Play();
    }

    void Update()
    {
        if (rb != null)
        {
            float currentSpeed = rb.velocity.magnitude;
            // Max speed is around 6.0f (boost) or 3.5f (regular)
            float speedRatio = Mathf.Clamp01(currentSpeed / 6.5f);
            
            // pitch changes between 1x and 2.5x base frequency
            frequencyMultiplier = 1f + speedRatio * 1.5f;
            volume = 0.012f + speedRatio * 0.015f; // extremely soft background hum
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float freq = baseFrequency * frequencyMultiplier;
        for (int i = 0; i < data.Length; i += channels)
        {
            phase += freq / sampleRate;
            if (phase > 1f) phase -= 1f;

            // Pure electric vehicle hum (sine combination, no saw buzz)
            float sine1 = Mathf.Sin(phase * 2f * Mathf.PI);
            float sine2 = Mathf.Sin(phase * 4f * Mathf.PI); // octave above
            float sample = sine1 * 0.75f + sine2 * 0.25f;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * volume;
            }
        }
    }
}
