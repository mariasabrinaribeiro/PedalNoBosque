using UnityEngine;

/// <summary>
/// Efeitos sonoros procedurais alegres (sem depender de arquivos externos de áudio):
/// disparo de bolha mágica, estouro de gelatina, coleta de cupcake, entrega festiva, turbo e alerta.
/// </summary>
public sealed class SweetAudio : MonoBehaviour
{
    public static SweetAudio Instance { get; private set; }

    private AudioSource sfx;
    private AudioClip clipShoot;
    private AudioClip clipPop;
    private AudioClip clipPickup;
    private AudioClip clipDelivery;
    private AudioClip clipTurbo;
    private AudioClip clipHit;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        clipShoot = MakeSweep(0.11f, 520f, 980f, 0.25f);
        clipPop = MakeSweep(0.14f, 680f, 220f, 0.30f);
        clipPickup = MakeChord(0.18f, new[] { 523.25f, 659.25f, 783.99f }, 0.24f);
        clipDelivery = MakeChord(0.45f, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.30f);
        clipTurbo = MakeSweep(0.32f, 440f, 1320f, 0.26f);
        clipHit = MakeSweep(0.22f, 240f, 95f, 0.32f);
    }

    public void PlayShoot() { if (sfx != null) sfx.PlayOneShot(clipShoot, 0.65f); }
    public void PlayPop() { if (sfx != null) sfx.PlayOneShot(clipPop, 0.75f); }
    public void PlayPickup() { if (sfx != null) sfx.PlayOneShot(clipPickup, 0.75f); }
    public void PlayDelivery() { if (sfx != null) sfx.PlayOneShot(clipDelivery, 0.90f); }
    public void PlayTurbo() { if (sfx != null) sfx.PlayOneShot(clipTurbo, 0.80f); }
    public void PlayHit() { if (sfx != null) sfx.PlayOneShot(clipHit, 0.80f); }

    private static AudioClip MakeSweep(float dur, float f0, float f1, float vol)
    {
        int sr = 22050;
        int len = Mathf.Max(256, Mathf.RoundToInt(sr * dur));
        float[] data = new float[len];
        float phase = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)len;
            float f = Mathf.Lerp(f0, f1, t);
            phase += 2f * Mathf.PI * f / sr;
            float env = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Sin(phase) * env * vol;
        }
        AudioClip c = AudioClip.Create("SweetSweep", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }

    private static AudioClip MakeChord(float dur, float[] freqs, float vol)
    {
        int sr = 22050;
        int len = Mathf.Max(256, Mathf.RoundToInt(sr * dur));
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)len;
            float env = Mathf.Pow(1f - t, 1.3f) * Mathf.Min(1f, t * 25f);
            float sum = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                float noteStart = f * 0.08f;
                if (t >= noteStart)
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * (i / (float)sr));
            }
            data[i] = (sum / freqs.Length) * env * vol;
        }
        AudioClip c = AudioClip.Create("SweetChord", len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }
}
