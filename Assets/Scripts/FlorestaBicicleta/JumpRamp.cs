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
            int w = 112, h = 52;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            // Rampa natural de terra batida com grama, pedrinhas e sombra de contato (Cozy Storybook)
            Color shadow = new Color(0.10f, 0.14f, 0.08f, 0.28f);
            Color dirtDark = new Color(0.50f, 0.34f, 0.20f, 1f);
            Color dirtMid = new Color(0.68f, 0.49f, 0.30f, 1f);
            Color dirtLight = new Color(0.80f, 0.64f, 0.42f, 1f);
            Color grass = new Color(0.28f, 0.64f, 0.26f, 1f);
            Color grassTip = new Color(0.46f, 0.78f, 0.32f, 1f);
            Color woodEdge = new Color(0.46f, 0.28f, 0.14f, 1f);

            // Sombra suave na base
            for (int x = 4; x < w - 4; x++)
                for (int y = 0; y <= 5; y++)
                    tex.SetPixel(x, y, shadow);

            // Corpo curvo natural de terra + travessa de madeira na ponta
            for (int x = 6; x < w - 8; x++)
            {
                float t = (x - 6f) / (w - 14f);
                float curve = Mathf.Pow(t, 1.35f);
                int maxY = Mathf.RoundToInt(6 + curve * (h - 14));
                for (int y = 4; y <= maxY; y++)
                {
                    if (x >= w - 16)
                        tex.SetPixel(x, y, (y % 5 == 0) ? woodEdge : dirtDark);
                    else if (y >= maxY - 3)
                        tex.SetPixel(x, y, dirtLight);
                    else if (y <= 11)
                        tex.SetPixel(x, y, dirtDark);
                    else
                        tex.SetPixel(x, y, dirtMid);
                }

                // Tufinhos delicados de grama na base da lombada
                if (x % 11 < 4 && x < w - 18)
                {
                    tex.SetPixel(x, 6, grass);
                    tex.SetPixel(x, 7, grass);
                    tex.SetPixel(x, 8, grassTip);
                }
            }

            tex.Apply();
            cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 34f);
            return cachedSprite;
        }
    }
}
