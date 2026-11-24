using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dynamic Coastal Soundscape & Adaptive Audio Synthesizer for Baía do Avesso:
/// - Distance-reactive ocean surf (louder near the beach/sea, gentler near stalls & houses)
/// - Periodic distant seagull ambience
/// - Adaptive cozy bossa/marimba music that speeds up softly when a chapter timer runs low
/// - Varied SFX: stone/sand/water steps, coin clink, paper flip, paint roller, fry sizzle,
///   skate/roller glide, beach tennis hit, fishing splash, dog bark, cat meow, victory & fail chimes!
/// </summary>
public sealed class GirlPowerOrla3DAudio : MonoBehaviour
{
    [Header("Volumes")]
    [Range(0f, 1f)] public float oceanVolume = 0.22f;
    [Range(0f, 1f)] public float musicVolume = 0.14f;
    [Range(0f, 1f)] public float sfxVolume = 0.45f;

    public bool IsMuted { get; private set; }

    private AudioSource oceanSource;
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip stoneStepClip;
    private AudioClip sandStepClip;
    private AudioClip waterStepClip;
    private AudioClip chimeClip;
    private AudioClip fanfareClip;
    private AudioClip victoryClip;
    private AudioClip failClip;
    private AudioClip voiceBlipClip;
    private AudioClip dogBarkClip;
    private AudioClip catMeowClip;
    private AudioClip coinClip;
    private AudioClip paperClip;
    private AudioClip brushClip;
    private AudioClip sizzleClip;
    private AudioClip seagullClip;
    private AudioClip tennisClip;
    private AudioClip wheelClip;

    private float oceanProximityFactor = 0.7f;
    private float targetMusicPitch = 1.0f;
    private float seagullCooldown = 9f;

    private const int SampleRate = 22050;

    private void Start()
    {
        musicVolume = PlayerPrefs.GetFloat("BaiaVol_Music", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("BaiaVol_Sfx", sfxVolume);
        oceanVolume = PlayerPrefs.GetFloat("BaiaVol_Ocean", oceanVolume);

        oceanSource = CreateLoopSource("OceanSurf_Audio", GenerateOceanSurfClip(), oceanVolume);
        musicSource = CreateLoopSource("CozyBeachMusic_Audio", GenerateCozyMarimbaLoop(), musicVolume);

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        stoneStepClip = GenerateStepClip(0);
        sandStepClip = GenerateStepClip(1);
        waterStepClip = GenerateStepClip(2);
        chimeClip = GenerateChimeClip(new[] { 523.25f, 659.25f, 783.99f }, 0.11f);
        fanfareClip = GenerateChimeClip(new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.13f);
        victoryClip = GenerateChimeClip(new[] { 523.25f, 659.25f, 783.99f, 987.77f, 1046.50f, 1318.51f }, 0.11f);
        failClip = GenerateChimeClip(new[] { 392.00f, 349.23f, 311.13f, 261.63f }, 0.17f);
        voiceBlipClip = GenerateVoiceBlipClip();
        dogBarkClip = GeneratePetSound(false);
        catMeowClip = GeneratePetSound(true);
        coinClip = GenerateChimeClip(new[] { 1046.5f, 1567.98f }, 0.045f);
        paperClip = GenerateNoiseBurstClip(0.11f, 0.35f, 301);
        brushClip = GenerateNoiseBurstClip(0.16f, 0.22f, 302);
        sizzleClip = GenerateNoiseBurstClip(0.28f, 0.55f, 303);
        seagullClip = GenerateSeagullClip();
        tennisClip = GeneratePopToneClip(320f, 180f, 0.09f);
        wheelClip = GenerateNoiseBurstClip(0.09f, 0.18f, 304);
    }

    private void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys != null && keys.mKey.wasPressedThisFrame)
            ToggleMute();

        if (oceanSource != null)
            oceanSource.volume = Mathf.Lerp(oceanSource.volume, oceanVolume * oceanProximityFactor, Time.unscaledDeltaTime * 3f);

        if (musicSource != null)
            musicSource.pitch = Mathf.Lerp(musicSource.pitch, targetMusicPitch, Time.unscaledDeltaTime * 2.5f);

        if (!IsMuted && Time.timeScale > 0.01f)
        {
            seagullCooldown -= Time.deltaTime;
            if (seagullCooldown <= 0f)
            {
                seagullCooldown = Random.Range(11f, 22f);
                if (sfxSource != null && seagullClip != null)
                {
                    sfxSource.pitch = Random.Range(0.92f, 1.12f);
                    sfxSource.PlayOneShot(seagullClip, oceanVolume * 0.45f * oceanProximityFactor);
                }
            }
        }
    }

    /// <summary>Scales ocean wave loudness smoothly based on player's Z position relative to shoreline.</summary>
    public void UpdateOceanProximity(float playerZ)
    {
        float t = Mathf.InverseLerp(-8f, 12f, playerZ);
        oceanProximityFactor = Mathf.Lerp(0.35f, 1.35f, t);
    }

    /// <summary>Adapts music speed gently when chapter timer gets low (urgency 0..1).</summary>
    public void SetTimerUrgency(float urgency01)
    {
        targetMusicPitch = Mathf.Lerp(1.0f, 1.16f, Mathf.Clamp01(urgency01));
    }

    public void ToggleMute()
    {
        IsMuted = !IsMuted;
        if (oceanSource != null) oceanSource.mute = IsMuted;
        if (musicSource != null) musicSource.mute = IsMuted;
        if (sfxSource != null) sfxSource.mute = IsMuted;
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("BaiaVol_Music", musicVolume);
    }

    public void SetSfxVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("BaiaVol_Sfx", sfxVolume);
    }

    public void SetOceanVolume(float vol)
    {
        oceanVolume = Mathf.Clamp01(vol);
        if (oceanSource != null) oceanSource.volume = oceanVolume * oceanProximityFactor;
        PlayerPrefs.SetFloat("BaiaVol_Ocean", oceanVolume);
    }

    public void PlayFootstep(int surfaceType)
    {
        if (IsMuted || sfxSource == null) return;
        AudioClip clip = surfaceType == 2 ? waterStepClip : (surfaceType == 1 ? sandStepClip : stoneStepClip);
        if (clip == null) return;
        sfxSource.pitch = Random.Range(0.90f, 1.12f);
        sfxSource.PlayOneShot(clip, sfxVolume * (surfaceType == 2 ? 0.65f : 0.42f));
    }

    public void PlayWheelGlide()
    {
        if (IsMuted || sfxSource == null || wheelClip == null) return;
        sfxSource.pitch = Random.Range(0.95f, 1.15f);
        sfxSource.PlayOneShot(wheelClip, sfxVolume * 0.28f);
    }

    public void PlayCoinClink(float pitch = 1.0f)
    {
        if (IsMuted || sfxSource == null || coinClip == null) return;
        sfxSource.pitch = Mathf.Clamp(pitch, 0.8f, 1.7f);
        sfxSource.PlayOneShot(coinClip, sfxVolume * 0.52f);
    }

    public void PlayPaperFlip()
    {
        if (IsMuted || sfxSource == null || paperClip == null) return;
        sfxSource.pitch = Random.Range(0.95f, 1.10f);
        sfxSource.PlayOneShot(paperClip, sfxVolume * 0.48f);
    }

    public void PlayPaintBrush()
    {
        if (IsMuted || sfxSource == null || brushClip == null) return;
        sfxSource.pitch = Random.Range(0.88f, 1.16f);
        sfxSource.PlayOneShot(brushClip, sfxVolume * 0.52f);
    }

    public void PlaySizzle()
    {
        if (IsMuted || sfxSource == null || sizzleClip == null) return;
        sfxSource.pitch = Random.Range(0.92f, 1.08f);
        sfxSource.PlayOneShot(sizzleClip, sfxVolume * 0.50f);
    }

    public void PlayTennisHit()
    {
        if (IsMuted || sfxSource == null || tennisClip == null) return;
        sfxSource.pitch = Random.Range(0.92f, 1.14f);
        sfxSource.PlayOneShot(tennisClip, sfxVolume * 0.62f);
    }

    public void PlayVoiceBlip(float basePitch = 1.0f)
    {
        if (IsMuted || sfxSource == null || voiceBlipClip == null) return;
        sfxSource.pitch = basePitch * Random.Range(0.92f, 1.15f);
        sfxSource.PlayOneShot(voiceBlipClip, sfxVolume * 0.36f);
    }

    public void PlayChime()
    {
        if (IsMuted || sfxSource == null || chimeClip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(chimeClip, sfxVolume * 0.68f);
    }

    public void PlayFanfare()
    {
        if (IsMuted || sfxSource == null || fanfareClip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(fanfareClip, sfxVolume * 0.78f);
    }

    public void PlayVictoryFanfare()
    {
        if (IsMuted || sfxSource == null || victoryClip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(victoryClip, sfxVolume * 0.85f);
    }

    public void PlayFailChime()
    {
        if (IsMuted || sfxSource == null || failClip == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(failClip, sfxVolume * 0.72f);
    }

    public void PlayPetSound(bool isCat)
    {
        if (IsMuted || sfxSource == null) return;
        AudioClip clip = isCat ? catMeowClip : dogBarkClip;
        if (clip == null) return;
        sfxSource.pitch = Random.Range(0.95f, 1.08f);
        sfxSource.PlayOneShot(clip, sfxVolume * 0.62f);
    }

    private AudioSource CreateLoopSource(string name, AudioClip clip, float vol)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.volume = vol;
        src.spatialBlend = 0f;
        src.Play();
        return src;
    }

    private static AudioClip GenerateNoiseBurstClip(float dur, float cutoff, int seed)
    {
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        System.Random rng = new System.Random(seed);
        float lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = Mathf.Sin(t * Mathf.PI);
            float n = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += cutoff * (n - lp);
            data[i] = lp * env * 0.55f;
        }
        AudioClip c = AudioClip.Create("Noise_" + seed, count, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }

    private static AudioClip GeneratePopToneClip(float fStart, float fEnd, float dur)
    {
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / dur;
            float f = Mathf.Lerp(fStart, fEnd, u);
            float env = Mathf.Exp(-u * 5f);
            data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.6f;
        }
        AudioClip c = AudioClip.Create("PopTone", count, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }

    private static AudioClip GenerateSeagullClip()
    {
        float dur = 0.52f;
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / dur;
            float env = Mathf.Sin(u * Mathf.PI) * Mathf.Exp(-u * 1.4f);
            float f = Mathf.Lerp(1480f, 920f, u * u) + Mathf.Sin(t * 45f) * 35f;
            data[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.65f + Mathf.Sin(2f * Mathf.PI * f * 1.98f * t) * 0.25f) * env * 0.35f;
        }
        AudioClip c = AudioClip.Create("SeagullCry", count, 1, SampleRate, false);
        c.SetData(data, 0);
        return c;
    }

    private static AudioClip GeneratePetSound(bool isCat)
    {
        float dur = isCat ? 0.28f : 0.16f;
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float u = t / dur;
            float env = Mathf.Sin(u * Mathf.PI);
            float f = isCat
                ? Mathf.Lerp(620f, 920f, Mathf.Sin(u * Mathf.PI)) - u * 180f
                : Mathf.Lerp(420f, 220f, u);
            float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.72f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.24f;
            data[i] = tone * env * 0.55f;
        }
        AudioClip clip = AudioClip.Create(isCat ? "CatMeow" : "DogBark", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateOceanSurfClip()
    {
        float duration = 8.0f;
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[count];
        System.Random rng = new System.Random(101);

        float lp1 = 0f, lp2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float waveEnv = 0.45f + 0.55f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin((t / duration) * Mathf.PI * 2f), 1.6f);
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            float cutoff = Mathf.Lerp(0.035f, 0.14f, waveEnv);
            lp1 += cutoff * (white - lp1);
            lp2 += cutoff * (lp1 - lp2);
            data[i] = lp2 * waveEnv * 0.65f;
        }

        AudioClip clip = AudioClip.Create("OceanSurfLoop", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateCozyMarimbaLoop()
    {
        float stepDur = 0.5f;
        int steps = 16;
        int count = Mathf.RoundToInt(SampleRate * stepDur * steps);
        float[] data = new float[count];

        float[] melody =
        {
            523.25f, 0f, 659.25f, 587.33f,
            440.00f, 0f, 523.25f, 392.00f,
            349.23f, 0f, 440.00f, 523.25f,
            587.33f, 523.25f, 392.00f, 0f
        };
        float[] bass =
        {
            130.81f, 0f, 0f, 0f,
            110.00f, 0f, 0f, 0f,
            174.61f, 0f, 0f, 0f,
            196.00f, 0f, 130.81f, 0f
        };

        for (int s = 0; s < steps; s++)
        {
            int start = Mathf.RoundToInt(s * stepDur * SampleRate);
            AddWarmNote(data, start, melody[s], 0.48f, 0.24f);
            AddWarmNote(data, start, bass[s], 0.92f, 0.18f);
        }

        AudioClip clip = AudioClip.Create("CozyBeachLoop", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void AddWarmNote(float[] buffer, int startIdx, float freq, float dur, float amp)
    {
        if (freq <= 1f) return;
        int len = Mathf.RoundToInt(dur * SampleRate);
        for (int i = 0; i < len; i++)
        {
            int idx = (startIdx + i) % buffer.Length;
            float t = (float)i / SampleRate;
            float attack = Mathf.Clamp01(t / 0.015f);
            float decay = Mathf.Exp(-t * 5.2f);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.78f
                       + Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.18f * Mathf.Exp(-t * 9f);
            buffer[idx] += wave * attack * decay * amp;
        }
    }

    private static AudioClip GenerateStepClip(int surfaceType)
    {
        float dur = surfaceType == 2 ? 0.14f : 0.08f;
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        System.Random rng = new System.Random(200 + surfaceType);
        float lp = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-t * (surfaceType == 0 ? 45f : (surfaceType == 1 ? 26f : 18f)));
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            if (surfaceType == 0)
            {
                float thud = Mathf.Sin(2f * Mathf.PI * 190f * t * (1f - t * 3f));
                data[i] = (thud * 0.65f + noise * 0.25f) * env * 0.55f;
            }
            else if (surfaceType == 1)
            {
                lp += 0.28f * (noise - lp);
                data[i] = lp * env * 0.55f;
            }
            else
            {
                lp += 0.45f * (noise - lp);
                float bubble = Mathf.Sin(2f * Mathf.PI * (420f + t * 900f) * t) * 0.3f;
                data[i] = (lp * 0.7f + bubble) * env * 0.6f;
            }
        }
        AudioClip clip = AudioClip.Create("Step_" + surfaceType, count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateVoiceBlipClip()
    {
        float dur = 0.068f;
        int count = Mathf.RoundToInt(SampleRate * dur);
        float[] data = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Sin((t / dur) * Mathf.PI);
            float f = 540f + Mathf.Sin(t * 90f) * 55f;
            float tone = Mathf.Sin(2f * Mathf.PI * f * t) * 0.72f + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.22f;
            data[i] = tone * env * 0.5f;
        }
        AudioClip clip = AudioClip.Create("AnimaleseBlip", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip GenerateChimeClip(float[] freqs, float spacing)
    {
        float total = freqs.Length * spacing + 0.42f;
        int count = Mathf.RoundToInt(SampleRate * total);
        float[] data = new float[count];
        for (int n = 0; n < freqs.Length; n++)
            AddWarmNote(data, Mathf.RoundToInt(n * spacing * SampleRate), freqs[n], 0.42f, 0.34f);

        AudioClip clip = AudioClip.Create("CozyChime", count, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
