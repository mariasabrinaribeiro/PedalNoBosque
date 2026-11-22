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
            float bob = Mathf.Sin(Time.time * 3.8f + phase) * 0.14f;
            transform.position = basePos + new Vector3(0f, bob, 0f);
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
            cachedStar = StorybookUISpriteFactory.GetIcon(StorybookUISpriteFactory.UIIconKind.Star);
            return cachedStar;
        }
    }
}
