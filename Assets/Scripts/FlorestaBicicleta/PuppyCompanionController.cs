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
        private Sprite iconEarsUp;
        private bool isFaroMode;

        private float idleTime = 0f;
        private float timedMoodRemaining = 0f;

        public void Initialize(BikeGirlController controller)
        {
            bike = controller;
            iconHeart = FlorestaSpriteFactory.CreateHeartIconSprite();
            iconWater = StorybookUISpriteFactory.GetIcon(StorybookUISpriteFactory.UIIconKind.WaterDrop);
            iconSniff = CreateSniffIconSprite();
            iconSleep = CreateSleepZzzSprite();
            iconEarsUp = CreateEarsUpSprite();

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

        public void SetFaroMode(bool active)
        {
            isFaroMode = active;
            if (active) TriggerCurious(float.MaxValue);
            else if (CurrentMood == PuppyMood.Curioso)
            {
                timedMoodRemaining = 0f;
                CurrentMood = PuppyMood.Neutro;
            }
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
            if (isFaroMode)
                displayMood = PuppyMood.Curioso;
            else if (displayMood == PuppyMood.Neutro)
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
                case PuppyMood.Curioso:  moodBubbleSR.sprite = isFaroMode ? iconEarsUp : iconSniff; break;
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

        private static Sprite CreateEarsUpSprite()
        {
            const int s = 64;
            Texture2D tex = Blank(s);
            Color outer = new Color(0.43f, 0.25f, 0.15f, 1f);
            Color inner = new Color(0.96f, 0.54f, 0.48f, 1f);
            for (int y = 14; y < 55; y++)
            {
                float t = (y - 14f) / 41f;
                int half = Mathf.RoundToInt(3f + t * 10f);
                int leftCenter = Mathf.RoundToInt(19f - t * 2f);
                int rightCenter = Mathf.RoundToInt(45f + t * 2f);
                for (int x = leftCenter - half; x <= leftCenter + half; x++)
                    if (x >= 0 && x < s) tex.SetPixel(x, y, outer);
                for (int x = rightCenter - half; x <= rightCenter + half; x++)
                    if (x >= 0 && x < s) tex.SetPixel(x, y, outer);

                int innerHalf = Mathf.Max(1, half - 4);
                for (int x = leftCenter - innerHalf; x <= leftCenter + innerHalf; x++)
                    if (x >= 0 && x < s && y > 20) tex.SetPixel(x, y, inner);
                for (int x = rightCenter - innerHalf; x <= rightCenter + innerHalf; x++)
                    if (x >= 0 && x < s && y > 20) tex.SetPixel(x, y, inner);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
