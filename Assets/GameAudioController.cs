using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    public static GameAudioController Instance { get; private set; }

    private AudioSource sfxSource;

    private AudioClip shootClip;
    private AudioClip hitClip;
    private AudioClip explosionClip;
    private AudioClip powerupClip;
    private AudioClip girlPowerClip;
    private AudioClip warningClip;
    private AudioClip heartbeatClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        GenerateClips();
    }

    private void GenerateClips()
    {
        shootClip = CreateShootClip();
        hitClip = CreateHitClip();
        explosionClip = CreateExplosionClip();
        powerupClip = CreatePowerUpClip();
        girlPowerClip = CreateGirlPowerClip();
        warningClip = CreateWarningClip();
        heartbeatClip = CreateHeartbeatClip();
    }

    public void PlayShoot() => sfxSource.PlayOneShot(shootClip, 0.45f);
    public void PlayHit() => sfxSource.PlayOneShot(hitClip, 0.35f);
    public void PlayExplosion() => sfxSource.PlayOneShot(explosionClip, 0.7f);
    public void PlayPowerup() => sfxSource.PlayOneShot(powerupClip, 0.55f);
    public void PlayGirlPower() => sfxSource.PlayOneShot(girlPowerClip, 0.65f);
    public void PlayWarning() => sfxSource.PlayOneShot(warningClip, 0.6f);
    public void PlayHeartbeat() => sfxSource.PlayOneShot(heartbeatClip, 0.5f);

    private AudioClip CreateShootClip()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Bubble gun frequency sweep (high to mid)
            float freq = Mathf.Lerp(1500f, 350f, t);
            phase += (freq / sampleRate) * 2f * Mathf.PI;
            
            // Pop envelope
            float envelope = Mathf.Sin(t * Mathf.PI) * (1f - t);
            samples[i] = Mathf.Sin(phase) * envelope * 0.22f;
        }

        AudioClip clip = AudioClip.Create("ShootProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHitClip()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // A bit of pinkish noise (filtered noise)
            float noise = Random.Range(-1f, 1f);
            
            // Soft sine drop for the "splat" body
            float freq = Mathf.Lerp(300f, 80f, t);
            phase += (freq / sampleRate) * 2f * Mathf.PI;
            float sine = Mathf.Sin(phase);
            
            // Mix noise (crunch) and sine (splat)
            float mix = (sine * 0.45f + noise * 0.55f) * (1f - t) * (1f - t);
            samples[i] = mix * 0.2f; // low volume, safe for speakers
        }

        AudioClip clip = AudioClip.Create("HitProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateExplosionClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // Soft noise combined with low rumble frequency
            float noise = Random.Range(-1.0f, 1.0f);
            float rumble = Mathf.Sin(t * duration * 80f * 2f * Mathf.PI); // low 80Hz rumble
            
            float mix = (noise * 0.7f + rumble * 0.3f) * (1f - t) * (1f - t);
            samples[i] = mix * 0.35f;
        }

        AudioClip clip = AudioClip.Create("ExplosionProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreatePowerUpClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase1 = 0f;
        float phase2 = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // Magic bell sweep: fundamental + 3rd harmonic (bell tone)
            float freq1 = Mathf.Lerp(700f, 950f, t * t);
            float freq2 = freq1 * 3.1f; // metallic/chime ring
            
            phase1 += (freq1 / sampleRate) * 2f * Mathf.PI;
            phase2 += (freq2 / sampleRate) * 2f * Mathf.PI;
            
            float envelope = Mathf.Exp(-4.5f * t); // exponential bell decay
            float sample = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.35f) * envelope;
            
            samples[i] = sample * 0.2f; // clean, soft ding
        }

        AudioClip clip = AudioClip.Create("PowerUpProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateGirlPowerClip()
    {
        int sampleRate = 44100;
        float duration = 0.65f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // Clean arpeggio sweep using phase accumulation to avoid clicks
            float freq = 380f;
            if (t > 0.2f && t <= 0.4f) freq = 520f;
            else if (t > 0.4f && t <= 0.6f) freq = 680f;
            else if (t > 0.6f) freq = 1000f;

            phase += (freq / sampleRate) * 2f * Mathf.PI;
            samples[i] = Mathf.Sin(phase) * (1f - t) * 0.25f;
        }

        AudioClip clip = AudioClip.Create("GirlPowerProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateWarningClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // Smooth warning sweep using phase accumulation to avoid clicks
            float freq = 450f + Mathf.Sin(t * 10f * Mathf.PI) * 120f;
            phase += (freq / sampleRate) * 2f * Mathf.PI;
            
            float envelope = Mathf.Sin(t * Mathf.PI);
            samples[i] = Mathf.Sin(phase) * envelope * 0.22f;
        }

        AudioClip clip = AudioClip.Create("WarningProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHeartbeatClip()
    {
        int sampleRate = 44100;
        float duration = 0.3f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            
            // Low thumping frequency (55Hz)
            float freq = 55f;
            phase += (freq / sampleRate) * 2f * Mathf.PI;
            
            float envelope = 0f;
            // Double beat envelope shape
            if (t < 0.35f) envelope = Mathf.Sin(t * Mathf.PI / 0.35f);
            else if (t > 0.45f) envelope = Mathf.Sin((t - 0.45f) * Mathf.PI / 0.55f) * 0.65f;

            samples[i] = Mathf.Sin(phase) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("HeartbeatProcedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
