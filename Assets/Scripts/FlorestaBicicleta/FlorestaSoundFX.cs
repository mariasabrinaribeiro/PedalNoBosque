using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Sons sintetizados para todos os animais (latido, miado, uivo, esquilo, coelho, quati, raposa, cervo),
    /// além de som de sede ofegante do cachorrinho, campainha da bicicleta, mastigar, choro, água, cavar e diamantes!
    /// </summary>
    public class FlorestaSoundFX : MonoBehaviour
    {
        public static FlorestaSoundFX Instance { get; private set; }
        private AudioSource source;

        private AudioClip clipDogBark;
        private AudioClip clipThirstPant;
        private AudioClip clipCatMeow;
        private AudioClip clipWolfHowl;
        private AudioClip clipSquirrelChirp;
        private AudioClip clipRabbitSniff;
        private AudioClip clipCoatiChitter;
        private AudioClip clipFoxYip;
        private AudioClip clipDeerCall;

        private AudioClip clipBell;
        private AudioClip clipThrow;
        private AudioClip clipHappy;
        private AudioClip clipWrong;
        private AudioClip clipCry;
        private AudioClip clipWater;
        private AudioClip clipDig;
        private AudioClip clipDiamond;
        private AudioClip clipJump;
        private AudioClip clipBump;
        private AudioClip clipStar;

        private void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.42f;

            // Sons dos animais
            clipDogBark       = MakeDoubleBark(360f, 540f, 220f, 0.09f, 0.06f);      // Au-Au!
            clipThirstPant    = MakePantSound();                                       // Ofegante de sede
            clipCatMeow       = MakeCurveTone(new[] { 540f, 820f, 680f, 460f }, 0.36f, 0.45f); // Miaaau!
            clipWolfHowl      = MakeCurveTone(new[] { 260f, 440f, 520f, 490f, 340f }, 0.72f, 0.50f); // Auuuuu!
            clipSquirrelChirp = MakeTripleChirp(1150f, 1550f, 0.055f);                 // Tchi-tchi-tchi!
            clipRabbitSniff   = MakeTripleChirp(680f, 820f, 0.045f);                   // Fungadinha fofa
            clipCoatiChitter  = MakeDoubleBark(720f, 980f, 620f, 0.07f, 0.04f);        // Quati alegre
            clipFoxYip        = MakeDoubleBark(820f, 1180f, 640f, 0.08f, 0.05f);       // Yip-yip!
            clipDeerCall      = MakeCurveTone(new[] { 310f, 380f, 290f }, 0.34f, 0.45f);

            // Efeitos da aventura
            clipBell    = MakeArpeggio(new[] { 1318f, 1760f, 1318f, 1760f }, 0.055f); // Trim-trim!
            clipThrow   = MakeCurveTone(new[] { 400f, 680f }, 0.11f, 0.38f);
            clipHappy   = MakeArpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.075f);
            clipWrong   = MakeCurveTone(new[] { 280f, 180f }, 0.20f, 0.40f);
            clipCry     = MakeCurveTone(new[] { 520f, 380f, 450f, 260f }, 0.42f, 0.48f);
            clipWater   = MakeArpeggio(new[] { 587f, 740f, 880f, 1174f }, 0.065f);
            clipDig     = MakeCurveTone(new[] { 190f, 105f }, 0.13f, 0.55f);
            clipDiamond = MakeArpeggio(new[] { 784f, 987f, 1174f, 1568f, 2093f }, 0.085f);
            clipJump    = MakeCurveTone(new[] { 320f, 760f }, 0.18f, 0.42f);
            clipBump    = MakeCurveTone(new[] { 170f, 85f }, 0.16f, 0.55f);
            clipStar    = MakeArpeggio(new[] { 880f, 1174f, 1568f }, 0.06f);
        }

        public void PlayAnimalVoice(ParkSpriteFactory.AnimalKind kind)
        {
            if (source == null) return;
            AudioClip c = kind switch
            {
                ParkSpriteFactory.AnimalKind.Gato    => clipCatMeow,
                ParkSpriteFactory.AnimalKind.Lobo    => clipWolfHowl,
                ParkSpriteFactory.AnimalKind.Esquilo => clipSquirrelChirp,
                ParkSpriteFactory.AnimalKind.Coelho  => clipRabbitSniff,
                ParkSpriteFactory.AnimalKind.Quati   => clipCoatiChitter,
                ParkSpriteFactory.AnimalKind.Raposa  => clipFoxYip,
                ParkSpriteFactory.AnimalKind.Cervo   => clipDeerCall,
                _ => clipDogBark
            };
            source.PlayOneShot(c);
        }

        public void PlayDogBark()   { if (source != null) source.PlayOneShot(clipDogBark); }
        public void PlayThirst()    { if (source != null) source.PlayOneShot(clipThirstPant); }
        public void PlayBell()      { if (source != null) source.PlayOneShot(clipBell); }
        public void PlayThrow()     { if (source != null) source.PlayOneShot(clipThrow); }
        public void PlayHappy()     { if (source != null) source.PlayOneShot(clipHappy); }
        public void PlayWrong()     { if (source != null) source.PlayOneShot(clipWrong); }
        public void PlayCry()       { if (source != null) source.PlayOneShot(clipCry); }
        public void PlayWater()     { if (source != null) source.PlayOneShot(clipWater); }
        public void PlayDig()       { if (source != null) source.PlayOneShot(clipDig); }
        public void PlayDiamond()   { if (source != null) source.PlayOneShot(clipDiamond); }
        public void PlayJump()      { if (source != null) source.PlayOneShot(clipJump); }
        public void PlayBump()      { if (source != null) source.PlayOneShot(clipBump); }
        public void PlayStar()      { if (source != null) source.PlayOneShot(clipStar); }

        // ─── Síntese Procedural ──────────────────────────────────────

        private AudioClip MakeCurveTone(float[] pitchPoints, float duration, float amp)
        {
            int sr = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sr * duration));
            float[] data = new float[samples];
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)(samples - 1);
                float scaled = t * (pitchPoints.Length - 1);
                int idx = Mathf.Min(pitchPoints.Length - 2, Mathf.FloorToInt(scaled));
                float frac = scaled - idx;
                float freq = Mathf.Lerp(pitchPoints[idx], pitchPoints[idx + 1], frac);

                phase += 2f * Mathf.PI * freq / sr;
                float env = Mathf.Sin(t * Mathf.PI);
                // Mistura seno + harmônico suave para timbre mais expressivo
                float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;
                data[i] = wave * env * amp;
            }

            AudioClip clip = AudioClip.Create("Voice", samples, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeDoubleBark(float startF, float peakF, float endF, float barkDur, float gapDur)
        {
            int sr = 22050;
            int bSamp = Mathf.FloorToInt(sr * barkDur);
            int gSamp = Mathf.FloorToInt(sr * gapDur);
            int total = bSamp * 2 + gSamp;
            float[] data = new float[total];

            for (int b = 0; b < 2; b++)
            {
                int offset = b * (bSamp + gSamp);
                float phase = 0f;
                for (int i = 0; i < bSamp; i++)
                {
                    float t = i / (float)bSamp;
                    float f = t < 0.35f ? Mathf.Lerp(startF, peakF, t / 0.35f) : Mathf.Lerp(peakF, endF, (t - 0.35f) / 0.65f);
                    phase += 2f * Mathf.PI * f / sr;
                    float env = Mathf.Sin(t * Mathf.PI);
                    float wave = Mathf.Sin(phase) * 0.65f + Mathf.Sin(phase * 2.5f) * 0.35f;
                    data[offset + i] = wave * env * 0.5f;
                }
            }

            AudioClip clip = AudioClip.Create("Bark", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeTripleChirp(float f1, float f2, float chirpDur)
        {
            int sr = 22050;
            int cSamp = Mathf.FloorToInt(sr * chirpDur);
            int gSamp = Mathf.FloorToInt(sr * 0.03f);
            int total = cSamp * 3 + gSamp * 2;
            float[] data = new float[total];

            for (int c = 0; c < 3; c++)
            {
                int offset = c * (cSamp + gSamp);
                float phase = 0f;
                for (int i = 0; i < cSamp; i++)
                {
                    float t = i / (float)cSamp;
                    float f = Mathf.Lerp(f1, f2, t);
                    phase += 2f * Mathf.PI * f / sr;
                    data[offset + i] = Mathf.Sin(phase) * Mathf.Sin(t * Mathf.PI) * 0.42f;
                }
            }

            AudioClip clip = AudioClip.Create("Chirp", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakePantSound()
        {
            int sr = 22050;
            int burst = Mathf.FloorToInt(sr * 0.11f);
            int gap = Mathf.FloorToInt(sr * 0.07f);
            int total = burst * 3 + gap * 2;
            float[] data = new float[total];
            System.Random rng = new System.Random(42);

            for (int b = 0; b < 3; b++)
            {
                int offset = b * (burst + gap);
                for (int i = 0; i < burst; i++)
                {
                    float t = i / (float)burst;
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float whine = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(520f, 380f, t) * (i / (float)sr)) * 0.25f;
                    data[offset + i] = (noise * 0.22f + whine) * Mathf.Sin(t * Mathf.PI);
                }
            }

            AudioClip clip = AudioClip.Create("Thirst", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeArpeggio(float[] freqs, float noteDuration)
        {
            int sr = 22050;
            int noteSamples = Mathf.FloorToInt(sr * noteDuration);
            int total = noteSamples * freqs.Length;
            float[] data = new float[total];
            for (int n = 0; n < freqs.Length; n++)
            {
                float phase = 0f;
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = i / (float)noteSamples;
                    phase += 2f * Mathf.PI * freqs[n] / sr;
                    data[n * noteSamples + i] = Mathf.Sin(phase) * Mathf.Sin(t * Mathf.PI) * 0.42f;
                }
            }
            AudioClip clip = AudioClip.Create("Arp", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
