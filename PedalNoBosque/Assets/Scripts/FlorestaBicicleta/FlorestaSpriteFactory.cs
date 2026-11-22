using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Carrega as artes 2D ilustradas de Assets/Resources/Floresta/
    /// (com fallback seguro caso algum arquivo ainda não tenha sido importado).
    /// </summary>
    public static class FlorestaSpriteFactory
    {
        public enum AnimalKind
        {
            Esquilo,
            Coelho,
            Gato,
            Lobo,
            Quati
        }

        private static Sprite LoadResourceSprite(string resourcePath, float targetWorldHeight, float pivotY = 0f)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;

            float ppu = tex.height / Mathf.Max(0.1f, targetWorldHeight);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, pivotY), ppu);
        }

        public static Sprite LoadForestBackgroundSprite(float cameraWorldHeight)
        {
            return LoadResourceSprite("Floresta/bg_floresta", cameraWorldHeight, 0.5f);
        }

        public static Sprite LoadGirlBikeSprite()
        {
            return LoadResourceSprite("Floresta/girl_bike", 3.3f, 0.05f);
        }

        public static Sprite LoadAnimalSprite(AnimalKind kind)
        {
            string fileName;
            float height;
            switch (kind)
            {
                case AnimalKind.Esquilo: fileName = "animal_esquilo"; height = 1.15f; break;
                case AnimalKind.Coelho:  fileName = "animal_coelho";  height = 1.15f; break;
                case AnimalKind.Gato:    fileName = "animal_gato";    height = 1.25f; break;
                case AnimalKind.Lobo:    fileName = "animal_lobo";    height = 2.1f;  break;
                case AnimalKind.Quati:   fileName = "animal_quati";   height = 1.45f; break;
                default:                 fileName = "animal_esquilo"; height = 1.15f; break;
            }
            return LoadResourceSprite($"Floresta/{fileName}", height, 0.05f);
        }

        // Ícones suaves de balão de comida e coração
        private static Texture2D NewTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color col)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    float nx = (x - cx) / (float)Mathf.Max(1, rx);
                    float ny = (y - cy) / (float)Mathf.Max(1, ry);
                    if (nx * nx + ny * ny <= 1f)
                        tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color col)
        {
            int minX = Mathf.Max(0, Mathf.Min(x0, Mathf.Min(x1, x2)));
            int maxX = Mathf.Min(tex.width - 1, Mathf.Max(x0, Mathf.Max(x1, x2)));
            int minY = Mathf.Max(0, Mathf.Min(y0, Mathf.Min(y1, y2)));
            int maxY = Mathf.Min(tex.height - 1, Mathf.Max(y0, Mathf.Max(y1, y2)));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float d1 = (x - x1) * (y0 - y1) - (x0 - x1) * (y - y1);
                    float d2 = (x - x2) * (y1 - y2) - (x1 - x2) * (y - y2);
                    float d3 = (x - x0) * (y2 - y0) - (x2 - x0) * (y - y0);
                    if (!(((d1 < 0) || (d2 < 0) || (d3 < 0)) && ((d1 > 0) || (d2 > 0) || (d3 > 0))))
                        tex.SetPixel(x, y, col);
                }
            }
        }

        public static Sprite CreateFoodIconSprite()
        {
            Texture2D tex = NewTex(64, 64);
            DrawCircle(tex, 32, 32, 28, new Color(0.95f, 0.55f, 0.25f, 0.95f));
            DrawCircle(tex, 32, 32, 24, new Color(1f, 0.98f, 0.90f, 1f));
            DrawEllipse(tex, 32, 25, 14, 7, new Color(0.82f, 0.34f, 0.18f, 1f));
            DrawCircle(tex, 25, 35, 6, new Color(0.92f, 0.22f, 0.36f, 1f));
            DrawCircle(tex, 38, 35, 6, new Color(0.96f, 0.68f, 0.18f, 1f));
            DrawCircle(tex, 32, 40, 5, new Color(0.32f, 0.72f, 0.30f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        public static Sprite CreateHeartIconSprite()
        {
            Texture2D tex = NewTex(64, 64);
            DrawCircle(tex, 32, 32, 28, new Color(1f, 0.95f, 0.97f, 0.92f));
            Color heart = new Color(0.98f, 0.24f, 0.46f, 1f);
            DrawCircle(tex, 24, 37, 10, heart);
            DrawCircle(tex, 40, 37, 10, heart);
            DrawTriangle(tex, 15, 33, 49, 33, 32, 14, heart);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
