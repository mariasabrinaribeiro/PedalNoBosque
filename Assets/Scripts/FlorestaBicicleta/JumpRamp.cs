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
            int w = 220, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            // Rampa de madeira esculpida e terra compactada com contorno #381A12 e tufos de grama (Cozy Storybook)
            Color ink = new Color(0.22f, 0.10f, 0.07f, 1f);
            Color woodDark = new Color(0.50f, 0.27f, 0.13f, 1f);
            Color woodMid = new Color(0.72f, 0.44f, 0.21f, 1f);
            Color woodLight = new Color(0.88f, 0.62f, 0.34f, 1f);
            Color grassDark = new Color(0.24f, 0.48f, 0.16f, 1f);
            Color grassLight = new Color(0.46f, 0.72f, 0.24f, 1f);

            for (int x = 14; x < w - 16; x++)
            {
                float t = (x - 14f) / (w - 30f);
                float curve = Mathf.Pow(t, 1.28f);
                int maxY = Mathf.RoundToInt(14 + curve * (h - 30));
                bool isPlankSep = ((x - 14) % 28 == 0);

                for (int y = 10; y <= maxY + 3; y++)
                {
                    if (y <= 12 || y >= maxY + 1 || x <= 16 || x >= w - 19 || isPlankSep)
                    {
                        tex.SetPixel(x, y, ink);
                    }
                    else if (y >= maxY - 5)
                    {
                        tex.SetPixel(x, y, woodLight);
                    }
                    else if (y <= 20)
                    {
                        tex.SetPixel(x, y, woodDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, woodMid);
                    }
                }
            }

            // Tufos de grama contornados na base da rampa
            int[] gx = { 34, 92, 162 };
            for (int i = 0; i < gx.Length; i++)
            {
                int cx = gx[i];
                for (int y = 6; y <= 20; y++)
                {
                    for (int x = cx - 16; x <= cx + 16; x++)
                    {
                        float nx = (x - cx) / 16f;
                        float ny = (y - 13f) / 7f;
                        float d2 = nx * nx + ny * ny;
                        if (d2 <= 1f)
                            tex.SetPixel(x, y, d2 > 0.68f ? ink : (y > 13 ? grassLight : grassDark));
                    }
                }
            }

            tex.Apply();
            cachedSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 10f / (float)h), 88f);
            return cachedSprite;
        }
    }
}
