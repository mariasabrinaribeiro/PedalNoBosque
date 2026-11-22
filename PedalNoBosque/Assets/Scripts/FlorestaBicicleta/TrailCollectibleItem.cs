using System.Collections.Generic;
using UnityEngine;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 7 — Colecionável natural na trilha (Flores, Sementes, Folhas Especiais,
    /// Cogumelos, Pedras Brilhantes e Tesouros Secretos) com sprite próprio e trava anti-duplicação.
    /// </summary>
    public class TrailCollectibleItem : MonoBehaviour
    {
        public CollectibleKind kind { get; private set; }
        public int bonusPoints { get; private set; } = 30;
        private Vector3 basePos;
        private float phase;
        private bool collected = false;

        private static readonly Dictionary<CollectibleKind, Sprite> iconCache =
            new Dictionary<CollectibleKind, Sprite>();

        public void Setup(CollectibleKind collectibleKind)
        {
            kind = collectibleKind;
            bonusPoints = (kind == CollectibleKind.TesourosSecretos) ? 90 : 30;
            basePos = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSprite(kind);
            sr.sortingOrder = 13;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * 3.1f + phase) * 0.14f;
            transform.position = basePos + new Vector3(0f, bob, 0f);
        }

        public bool TryCollect()
        {
            if (collected) return false;
            collected = true;
            gameObject.SetActive(false);
            return true;
        }

        public static Sprite GetOrCreateSprite(CollectibleKind kind)
        {
            if (iconCache.TryGetValue(kind, out Sprite cached) && cached != null)
                return cached;

            int s = 96;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] clear = new Color[s * s];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);

            void Circle(int cx, int cy, int r, Color c)
            {
                int r2 = r * r;
                for (int y = cy - r; y <= cy + r; y++)
                {
                    if (y < 0 || y >= s) continue;
                    for (int x = cx - r; x <= cx + r; x++)
                    {
                        if (x < 0 || x >= s) continue;
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2) tex.SetPixel(x, y, c);
                    }
                }
            }

            Color ink = new Color(0.22f, 0.10f, 0.07f, 1f);

            switch (kind)
            {
                case CollectibleKind.Estrelas:
                    Circle(48, 48, 22, ink);
                    Circle(48, 48, 18, new Color(1f, 0.86f, 0.22f, 1f));
                    Circle(45, 51, 9, new Color(1f, 0.98f, 0.78f, 1f));
                    break;
                case CollectibleKind.Flores:
                    for (int a = 0; a < 5; a++)
                    {
                        float ang = a * Mathf.PI * 2f / 5f;
                        int px = 48 + Mathf.RoundToInt(Mathf.Cos(ang) * 14f);
                        int py = 48 + Mathf.RoundToInt(Mathf.Sin(ang) * 14f);
                        Circle(px, py, 12, ink);
                        Circle(px, py, 9, new Color(1f, 0.52f, 0.72f, 1f));
                    }
                    Circle(48, 48, 10, ink);
                    Circle(48, 48, 7, new Color(1f, 0.88f, 0.24f, 1f));
                    break;
                case CollectibleKind.Sementes:
                    Circle(48, 44, 20, ink);
                    Circle(48, 44, 16, new Color(0.76f, 0.48f, 0.22f, 1f));
                    Circle(48, 56, 14, ink);
                    Circle(48, 56, 11, new Color(0.46f, 0.26f, 0.12f, 1f));
                    break;
                case CollectibleKind.FolhasEspeciais:
                    Circle(44, 46, 18, ink);
                    Circle(44, 46, 14, new Color(0.96f, 0.68f, 0.18f, 1f));
                    Circle(54, 52, 15, ink);
                    Circle(54, 52, 12, new Color(1f, 0.84f, 0.28f, 1f));
                    break;
                case CollectibleKind.Cogumelos:
                    Circle(48, 34, 13, ink);
                    Circle(48, 34, 10, new Color(0.98f, 0.94f, 0.84f, 1f));
                    Circle(48, 54, 22, ink);
                    Circle(48, 54, 18, new Color(0.90f, 0.28f, 0.30f, 1f));
                    Circle(40, 58, 4, Color.white);
                    Circle(55, 55, 4, Color.white);
                    break;
                case CollectibleKind.PedrasBrilhantes:
                case CollectibleKind.TesourosSecretos:
                default:
                    Circle(48, 48, 22, ink);
                    Circle(48, 48, 18, new Color(0.36f, 0.84f, 0.98f, 1f));
                    Circle(43, 53, 8, new Color(0.84f, 0.98f, 1f, 1f));
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 115f);
            iconCache[kind] = sp;
            return sp;
        }
    }
}
