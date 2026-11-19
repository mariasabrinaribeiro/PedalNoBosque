using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Rampa / lombada na pista que impulsiona a bicicleta para um salto suave no ar!
    /// </summary>
    public class JumpRamp : MonoBehaviour
    {
        public float launchVelocity = 9.2f;
        private float lastTriggerTime = -2f;

        public void Setup()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRampSprite();
            sr.sortingOrder = 4;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 1.2f);
            col.offset = new Vector2(0f, 0.3f);
        }

        public bool CanTrigger()
        {
            if (Time.time - lastTriggerTime < 0.6f) return false;
            lastTriggerTime = Time.time;
            return true;
        }

        private static Sprite cachedSprite;
        private static Sprite CreateRampSprite()
        {
            if (cachedSprite != null) return cachedSprite;
            int w = 96, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color woodTop = new Color(0.84f, 0.58f, 0.30f, 1f);
            Color woodSide = new Color(0.62f, 0.38f, 0.18f, 1f);
            Color stripe = new Color(1f, 0.84f, 0.24f, 1f);

            for (int x = 4; x < w - 4; x++)
            {
                float t = (x - 4f) / (w - 8f);
                int maxY = Mathf.RoundToInt(4 + t * (h - 12));
                for (int y = 4; y <= maxY; y++)
                {
                    if (y >= maxY - 5)
                        tex.SetPixel(x, y, (x % 14 < 6) ? stripe : woodTop);
                    else
                        tex.SetPixel(x, y, woodSide);
                }
            }
            tex.Apply();
            cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.05f), 32f);
            return cachedSprite;
        }
    }
}
