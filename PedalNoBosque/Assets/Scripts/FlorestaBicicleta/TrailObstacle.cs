using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Obstáculo natural na trilha (Pedra, Raiz ou Tronco).
    /// Se a menina saltar por cima, passa limpa; se bater no chão, perde velocidade e pode derrubar 1 lanche.
    /// </summary>
    public class TrailObstacle : MonoBehaviour
    {
        public enum ObstacleKind { Pedra, Tronco, Raiz }
        public ObstacleKind kind;
        public string DisplayName => kind == ObstacleKind.Pedra ? "pedra" : (kind == ObstacleKind.Tronco ? "tronco" : "raiz");

        private float lastHitTime = -3f;
        private bool rewardedJumpOver = false;

        public void Setup(ObstacleKind obstacleKind)
        {
            kind = obstacleKind;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateObstacleSprite(kind);
            sr.sortingOrder = 5;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.45f, 1.0f);
            col.offset = new Vector2(0f, 0.35f);
        }

        public bool TryHit()
        {
            if (Time.time - lastHitTime < 1.5f) return false;
            lastHitTime = Time.time;
            return true;
        }

        public bool TryRewardJumpOver()
        {
            if (rewardedJumpOver) return false;
            rewardedJumpOver = true;
            return true;
        }

        private static Sprite CreateObstacleSprite(ObstacleKind kind)
        {
            int w = 64, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            void Circle(int cx, int cy, int r, Color c)
            {
                int r2 = r * r;
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (y < 0 || y >= h) continue;
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) tex.SetPixel(x, y, c);
                    }
                }
            }

            // Sombra de contato suave integrando o obstáculo ao chão da trilha
            Color contactShadow = new Color(0.10f, 0.14f, 0.08f, 0.26f);
            for (int y = 1; y <= 8; y++)
                for (int x = 8; x < w - 8; x++)
                    tex.SetPixel(x, y, contactShadow);

            if (kind == ObstacleKind.Pedra)
            {
                Circle(32, 16, 16, new Color(0.46f, 0.49f, 0.54f, 1f));
                Circle(25, 19, 11, new Color(0.60f, 0.63f, 0.68f, 1f));
                Circle(41, 14, 10, new Color(0.38f, 0.41f, 0.46f, 1f));
                Circle(24, 24, 5, new Color(0.76f, 0.79f, 0.84f, 1f));
            }
            else if (kind == ObstacleKind.Tronco)
            {
                for (int y = 6; y < 26; y++)
                    for (int x = 8; x < 54; x++)
                        tex.SetPixel(x, y, y > 20 ? new Color(0.58f, 0.36f, 0.18f, 1f) : new Color(0.42f, 0.25f, 0.12f, 1f));
                Circle(12, 16, 10, new Color(0.76f, 0.56f, 0.34f, 1f));
                Circle(12, 16, 5, new Color(0.58f, 0.38f, 0.20f, 1f));
            }
            else // Raiz
            {
                Circle(20, 12, 10, new Color(0.46f, 0.28f, 0.14f, 1f));
                Circle(34, 16, 11, new Color(0.52f, 0.32f, 0.16f, 1f));
                Circle(46, 11, 9, new Color(0.42f, 0.25f, 0.12f, 1f));
                Circle(34, 24, 5, new Color(0.24f, 0.66f, 0.28f, 1f)); // Broto verde
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.08f), 36f);
        }
    }
}
