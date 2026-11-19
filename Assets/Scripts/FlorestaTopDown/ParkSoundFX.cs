using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Efeitos sonoros leves sintetizados via código para o parque (comer, corações, choro, água, cavar e diamantes).
    /// </summary>
    public class ParkSoundFX : MonoBehaviour
    {
        public static ParkSoundFX Instance { get; private set; }
        private AudioSource source;

        private AudioClip clipThrow;
        private AudioClip clipHappy;
        private AudioClip clipCry;
        private AudioClip clipWater;
        private AudioClip clipDig;
        private AudioClip clipDiamond;
        private AudioClip clipWrong;

        private void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.35f;

            clipThrow   = MakeTone(420f, 620f, 0.10f, 0.4f);
            clipHappy   = MakeArpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.08f);
            clipCry     = MakeTone(480f, 240f, 0.32f, 0.5f);
            clipWater   = MakeArpeggio(new[] { 587f, 740f, 880f }, 0.07f);
            clipDig     = MakeTone(190f, 110f, 0.12f, 0.6f);
            clipDiamond = MakeArpeggio(new[] { 784f, 987f, 1174f, 1568f, 2093f }, 0.09f);
            clipWrong   = MakeTone(260f, 180f, 0.18f, 0.4f);
        }

        public void PlayThrow()   { if (source != null) source.PlayOneShot(clipThrow); }
        public void PlayHappy()   { if (source != null) source.PlayOneShot(clipHappy); }
        public void PlayCry()     { if (source != null) source.PlayOneShot(clipCry); }
        public void PlayWater()   { if (source != null) source.PlayOneShot(clipWater); }
        public void PlayDig()     { if (source != null) source.PlayOneShot(clipDig); }
        public void PlayDiamond() { if (source != null) source.PlayOneShot(clipDiamond); }
        public void PlayWrong()   { if (source != null) source.PlayOneShot(clipWrong); }

        private AudioClip MakeTone(float startFreq, float endFreq, float duration, float amp)
        {
            int sr = 22050;
            int samples = Mathf.Max(1, Mathf.FloorToInt(sr * duration));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sr;
                float env = Mathf.Sin(t * Mathf.PI);
                data[i] = Mathf.Sin(phase) * env * amp;
            }
            AudioClip clip = AudioClip.Create("Tone", samples, 1, sr, false);
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
                    float env = Mathf.Sin(t * Mathf.PI);
                    data[n * noteSamples + i] = Mathf.Sin(phase) * env * 0.45f;
                }
            }
            AudioClip clip = AudioClip.Create("Arp", total, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
