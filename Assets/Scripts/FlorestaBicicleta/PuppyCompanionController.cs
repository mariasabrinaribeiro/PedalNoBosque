using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 10 — Cachorrinho Companheiro Inteligente na Cestinha:
    /// Expressa emoções próprias acima da cesta (Feliz, Curioso farejando segredos,
    /// Dormindo na cestinha durante pausas calmas e Com Sede) sem caracteres quebrados!
    /// </summary>
    public class PuppyCompanionController : MonoBehaviour
    {
        public enum PuppyMood { Neutro, Feliz, Curioso, Dormindo, Sede }

        public PuppyMood CurrentMood { get; private set; } = PuppyMood.Neutro;

        private BikeGirlController bike;
        private SpriteRenderer moodBubbleSR;
        private Transform moodBubbleTr;

        private Sprite iconHeart;
        private Sprite iconSniff;
        private Sprite iconSleep;
        private Sprite iconWater;

        private float idleTime = 0f;
        private float timedMoodRemaining = 0f;

        public void Initialize(BikeGirlController controller)
        {
            bike = controller;
            iconHeart = FlorestaSpriteFactory.CreateHeartIconSprite();
            iconWater = StorybookUISpriteFactory.GetIcon(StorybookUISpriteFactory.UIIconKind.WaterDrop);
            iconSniff = CreateSniffIconSprite();
            iconSleep = CreateSleepZzzSprite();

            GameObject bubbleObj = new GameObject("HumorCachorrinho");
            bubbleObj.transform.SetParent(bike.transform, false);
            bubbleObj.transform.localPosition = new Vector3(0.95f, 2.55f, 0f);
            bubbleObj.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            moodBubbleTr = bubbleObj.transform;

            moodBubbleSR = bubbleObj.AddComponent<SpriteRenderer>();
            moodBubbleSR.sortingOrder = 15;
            moodBubbleSR.enabled = false;
        }

        public void TriggerHappy(float duration = 2.6f)
        {
            CurrentMood = PuppyMood.Feliz;
            timedMoodRemaining = duration;
        }

        public void TriggerCurious(float duration = 3.2f)
        {
            CurrentMood = PuppyMood.Curioso;
            timedMoodRemaining = duration;
        }

        private void Update()
        {
            if (bike == null || moodBubbleSR == null) return;

            bool isStopped = Mathf.Abs(bike.currentSpeed) < 0.12f;
            if (isStopped)
                idleTime += Time.deltaTime;
            else
                idleTime = 0f;

            if (timedMoodRemaining > 0f)
            {
                timedMoodRemaining -= Time.deltaTime;
                if (timedMoodRemaining <= 0f) CurrentMood = PuppyMood.Neutro;
            }

            // Prioridade de estados emocionais do cachorrinho
            PuppyMood displayMood = CurrentMood;
            if (displayMood == PuppyMood.Neutro)
            {
                if (bike.waterLevel < 25f) displayMood = PuppyMood.Sede;
                else if (idleTime > 7.5f) displayMood = PuppyMood.Dormindo;
            }

            if (displayMood == PuppyMood.Neutro)
            {
                moodBubbleSR.enabled = false;
                return;
            }

            moodBubbleSR.enabled = true;
            switch (displayMood)
            {
                case PuppyMood.Feliz:    moodBubbleSR.sprite = iconHeart; break;
                case PuppyMood.Curioso:  moodBubbleSR.sprite = iconSniff; break;
                case PuppyMood.Dormindo: moodBubbleSR.sprite = iconSleep; break;
                case PuppyMood.Sede:     moodBubbleSR.sprite = iconWater; break;
            }

            float bob = Mathf.Sin(Time.time * (displayMood == PuppyMood.Dormindo ? 1.8f : 5.2f)) * 0.10f;
            moodBubbleTr.localPosition = new Vector3(0.95f, 2.55f + bob, 0f);
        }

        private static Texture2D Blank(int s)
        {
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[s * s];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static Sprite CreateSniffIconSprite()
        {
            int s = 48;
            Texture2D tex = Blank(s);
            Color bg = new Color(1f, 0.96f, 0.72f, 0.95f);
            Color exc = new Color(0.90f, 0.42f, 0.16f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    if ((x - 24) * (x - 24) + (y - 24) * (y - 24) <= 20 * 20) tex.SetPixel(x, y, bg);

            // Ponto de exclamação simpático ("Farejei algo!")
            for (int y = 20; y <= 36; y++)
                for (int x = 21; x <= 27; x++)
                    tex.SetPixel(x, y, exc);
            for (int y = 11; y <= 16; y++)
                for (int x = 21; x <= 27; x++)
                    tex.SetPixel(x, y, exc);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }

        private static Sprite CreateSleepZzzSprite()
        {
            int s = 48;
            Texture2D tex = Blank(s);
            Color bg = new Color(0.86f, 0.92f, 1f, 0.90f);
            Color zCol = new Color(0.26f, 0.40f, 0.72f, 1f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    if ((x - 24) * (x - 24) + (y - 24) * (y - 24) <= 20 * 20) tex.SetPixel(x, y, bg);

            // Letra "Z" fofa de cochilo
            for (int x = 15; x <= 33; x++) { tex.SetPixel(x, 33, zCol); tex.SetPixel(x, 32, zCol); tex.SetPixel(x, 15, zCol); tex.SetPixel(x, 14, zCol); }
            for (int i = 0; i <= 18; i++) { tex.SetPixel(15 + i, 15 + i, zCol); tex.SetPixel(16 + i, 15 + i, zCol); }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
