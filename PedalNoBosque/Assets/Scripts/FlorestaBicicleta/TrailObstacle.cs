using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Obstáculo físico natural na trilha (Pedra, Raiz ou Tronco) em Y = WorldDepth.PlayableGroundY.
    /// Possui colisão física real contra penetração: a bicicleta NÃO atravessa o obstáculo pelo chão —
    /// é bloqueada/recuada suavemente até que a menina salte por cima (W / Pular)!
    /// </summary>
    public class TrailObstacle : MonoBehaviour
    {
        public static readonly List<TrailObstacle> ActiveObstacles = new List<TrailObstacle>();

        public enum ObstacleKind { Pedra, Tronco, Raiz }
        public ObstacleKind kind;
        public string DisplayName => kind == ObstacleKind.Pedra ? "pedra" : (kind == ObstacleKind.Tronco ? "tronco" : "raiz");
        public float HalfBlockWidth => kind == ObstacleKind.Tronco ? 0.68f : 0.56f;
        public float JumpClearanceHeight => 0.48f;

        private float lastHitTime = -3f;
        private bool rewardedJumpOver = false;

        private void OnEnable()
        {
            if (!ActiveObstacles.Contains(this)) ActiveObstacles.Add(this);
        }

        private void OnDisable()
        {
            ActiveObstacles.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveObstacles.Remove(this);
        }

        public void Setup(ObstacleKind obstacleKind)
        {
            kind = obstacleKind;
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateObstacleSprite(kind);
            sr.sortingOrder = WorldDepth.PlayableObstacle;

            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(HalfBlockWidth * 2f, 0.72f);
            col.offset = new Vector2(0f, 0.36f);
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
            int w = 160, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            void Ellipse(int cx, int cy, int rx, int ry, Color c)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    if (y < 0 || y >= h) continue;
                    for (int x = cx - rx; x <= cx + rx; x++)
                    {
                        if (x < 0 || x >= w) continue;
                        float nx = (x - cx) / (float)Mathf.Max(1, rx);
                        float ny = (y - cy) / (float)Mathf.Max(1, ry);
                        if (nx * nx + ny * ny <= 1f) tex.SetPixel(x, y, c);
                    }
                }
            }

            void Circle(int cx, int cy, int r, Color c) => Ellipse(cx, cy, r, r, c);

            void Rect(int x0, int y0, int x1, int y1, Color c)
            {
                int minX = Mathf.Max(0, x0), maxX = Mathf.Min(w - 1, x1);
                int minY = Mathf.Max(0, y0), maxY = Mathf.Min(h - 1, y1);
                for (int y = minY; y <= maxY; y++)
                    for (int x = minX; x <= maxX; x++)
                        tex.SetPixel(x, y, c);
            }

            Color ink = new Color(0.22f, 0.10f, 0.07f, 1f);
            Color mossDark = new Color(0.22f, 0.48f, 0.16f, 1f);
            Color mossLight = new Color(0.46f, 0.72f, 0.24f, 1f);
            Color contactShadow = new Color(0.06f, 0.12f, 0.08f, 0.34f);

            // Sombra elíptica suave na base (sem barra retangular dura!)
            Ellipse(80, 10, 56, 7, contactShadow);

            if (kind == ObstacleKind.Pedra)
            {
                Color stoneDark = new Color(0.46f, 0.44f, 0.42f, 1f);
                Color stoneMid = new Color(0.64f, 0.61f, 0.57f, 1f);
                Color stoneLight = new Color(0.82f, 0.79f, 0.74f, 1f);

                // Rocha principal esculpida com contorno #381A12 e musgo no topo
                Ellipse(76, 34, 42, 26, ink);
                Ellipse(76, 34, 38, 22, stoneDark);
                Ellipse(72, 37, 31, 17, stoneMid);
                Ellipse(66, 42, 20, 10, stoneLight);

                // Segunda pedra menor apoiada à direita
                Ellipse(112, 24, 24, 16, ink);
                Ellipse(112, 24, 20, 12, stoneDark);
                Ellipse(109, 26, 14, 8, stoneMid);

                // Tufos de musgo e florzinha silvestre na base da rocha
                Ellipse(52, 18, 20, 9, ink);
                Ellipse(52, 18, 17, 6, mossDark);
                Ellipse(50, 20, 11, 4, mossLight);
                Circle(44, 23, 5, ink);
                Circle(44, 23, 3, new Color(1f, 0.88f, 0.26f, 1f));
            }
            else if (kind == ObstacleKind.Tronco)
            {
                Color barkDark = new Color(0.44f, 0.23f, 0.11f, 1f);
                Color barkMid = new Color(0.64f, 0.37f, 0.17f, 1f);
                Color barkLight = new Color(0.80f, 0.52f, 0.25f, 1f);
                Color ringLight = new Color(0.92f, 0.74f, 0.46f, 1f);
                Color ringMid = new Color(0.76f, 0.52f, 0.28f, 1f);

                // Tronco cilíndrico de carvalho caído com textura de casca, musgo e anéis da madeira
                Rect(28, 12, 132, 54, ink);
                Rect(31, 16, 129, 50, barkDark);
                Rect(31, 24, 129, 48, barkMid);
                Rect(31, 38, 129, 47, barkLight);

                // Veios longitudinais da casca
                Rect(48, 22, 92, 25, ink);
                Rect(64, 34, 114, 37, ink);

                // Extremidade direita arredondada da casca
                Ellipse(129, 33, 14, 21, ink);
                Ellipse(128, 33, 10, 17, barkMid);

                // Corte transversal da madeira à esquerda com anéis concêntricos
                Ellipse(30, 33, 16, 21, ink);
                Ellipse(30, 33, 12, 17, ringLight);
                Ellipse(30, 33, 8, 11, ringMid);
                Ellipse(30, 33, 3, 5, barkDark);

                // Musgo verde e cogumelo pequeno sobre o tronco
                Ellipse(84, 51, 24, 8, ink);
                Ellipse(84, 51, 21, 5, mossDark);
                Ellipse(82, 52, 14, 3, mossLight);
                Circle(94, 58, 7, ink);
                Circle(94, 58, 5, new Color(0.92f, 0.30f, 0.28f, 1f));
                Circle(93, 59, 1, Color.white);
            }
            else // Raiz
            {
                Color rootDark = new Color(0.46f, 0.24f, 0.11f, 1f);
                Color rootMid = new Color(0.68f, 0.40f, 0.19f, 1f);
                Color rootLight = new Color(0.84f, 0.56f, 0.29f, 1f);

                // Raízes arqueadas entrelaçadas com contorno #381A12 e brotos floridos
                Ellipse(52, 24, 28, 15, ink);
                Ellipse(52, 24, 24, 11, rootDark);
                Ellipse(50, 26, 18, 7, rootMid);

                Ellipse(84, 30, 30, 18, ink);
                Ellipse(84, 30, 26, 14, rootMid);
                Ellipse(82, 34, 18, 8, rootLight);

                Ellipse(112, 22, 24, 13, ink);
                Ellipse(112, 22, 20, 9, rootDark);
                Ellipse(110, 24, 14, 6, rootMid);

                // Folhinhas e flor silvestre brotando da raiz
                Ellipse(78, 46, 12, 7, ink);
                Ellipse(78, 46, 9, 4, mossLight);
                Circle(84, 50, 6, ink);
                Circle(84, 50, 4, new Color(1f, 0.52f, 0.70f, 1f));
                Circle(84, 50, 1, Color.white);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 10f / (float)h), 92f);
        }
    }
}
