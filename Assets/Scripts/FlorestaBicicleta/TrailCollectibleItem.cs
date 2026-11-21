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

            int s = 56;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
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

            // Aura suave atrás do colecionável
            Circle(28, 28, 22, new Color(1f, 0.98f, 0.80f, 0.30f));

            switch (kind)
            {
                case CollectibleKind.Estrelas:
                    Circle(28, 28, 12, new Color(1f, 0.86f, 0.22f, 1f));
                    Circle(28, 28, 6, new Color(1f, 0.98f, 0.78f, 1f));
                    break;
                case CollectibleKind.Flores:
                    Circle(28, 36, 7, new Color(1f, 0.48f, 0.72f, 1f));
                    Circle(28, 20, 7, new Color(1f, 0.48f, 0.72f, 1f));
                    Circle(20, 28, 7, new Color(1f, 0.48f, 0.72f, 1f));
                    Circle(36, 28, 7, new Color(1f, 0.48f, 0.72f, 1f));
                    Circle(28, 28, 6, new Color(1f, 0.88f, 0.22f, 1f));
                    break;
                case CollectibleKind.Sementes:
                    Circle(28, 24, 12, new Color(0.28f, 0.70f, 0.30f, 1f));
                    Circle(23, 29, 5, new Color(0.36f, 0.28f, 0.20f, 1f));
                    Circle(33, 29, 5, new Color(0.36f, 0.28f, 0.20f, 1f));
                    Circle(28, 35, 5, new Color(0.46f, 0.36f, 0.24f, 1f));
                    break;
                case CollectibleKind.FolhasEspeciais:
                    Circle(24, 28, 11, new Color(0.96f, 0.68f, 0.16f, 1f));
                    Circle(32, 32, 10, new Color(1f, 0.82f, 0.24f, 1f));
                    Circle(20, 22, 5, new Color(0.54f, 0.32f, 0.14f, 1f));
                    break;
                case CollectibleKind.Cogumelos:
                    Circle(28, 20, 7, new Color(0.96f, 0.92f, 0.82f, 1f));
                    Circle(28, 32, 13, new Color(0.88f, 0.24f, 0.30f, 1f));
                    Circle(23, 35, 3, Color.white);
                    Circle(33, 33, 3, Color.white);
                    break;
                case CollectibleKind.PedrasBrilhantes:
                    Circle(28, 28, 13, new Color(0.32f, 0.80f, 0.98f, 1f));
                    Circle(24, 32, 6, new Color(0.76f, 0.96f, 1f, 1f));
                    Circle(32, 24, 4, new Color(0.56f, 0.44f, 0.98f, 1f));
                    break;
                case CollectibleKind.TesourosSecretos:
                    Circle(28, 28, 14, new Color(0.98f, 0.78f, 0.20f, 1f));
                    Circle(28, 28, 9, new Color(0.42f, 0.92f, 1f, 1f));
                    Circle(25, 31, 4, Color.white);
                    break;
            }

            tex.Apply();
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 46f);
            iconCache[kind] = sp;
            return sp;
        }
    }
}
