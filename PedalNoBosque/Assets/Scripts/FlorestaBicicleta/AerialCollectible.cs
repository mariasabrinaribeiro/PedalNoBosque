using UnityEngine;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Estrela dourada flutuando no ar acima das rampas para recompensar saltos!
    /// </summary>
    public class AerialCollectible : MonoBehaviour
    {
        public int bonusPoints = 50;
        private Vector3 basePos;
        private float phase;
        private bool collected = false;

        public void Setup(int points = 50)
        {
            bonusPoints = points;
            basePos = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateStarSprite();
            sr.sortingOrder = 14;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * 3.8f + phase) * 0.18f;
            transform.position = basePos + new Vector3(0f, bob, 0f);
            transform.Rotate(0f, 0f, 90f * Time.deltaTime);
        }

        public bool Collect()
        {
            if (collected) return false;
            collected = true;
            gameObject.SetActive(false);
            return true;
        }

        private static Sprite cachedStar;
        private static Sprite CreateStarSprite()
        {
            if (cachedStar != null) return cachedStar;
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] clear = new Color[s * s];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            Color glow = new Color(1f, 0.94f, 0.40f, 0.40f);
            Color gold = new Color(1f, 0.84f, 0.16f, 1f);
            Color center = new Color(1f, 0.98f, 0.72f, 1f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x - 24f;
                    float dy = y - 24f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float starR = 12f + 7f * Mathf.Cos(ang * 5f);
                    if (r <= starR)
                        tex.SetPixel(x, y, r < 6f ? center : gold);
                    else if (r <= 22f)
                        tex.SetPixel(x, y, glow);
                }
            }
            tex.Apply();
            cachedStar = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 44f);
            return cachedStar;
        }
    }
}
