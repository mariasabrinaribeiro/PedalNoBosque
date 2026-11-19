using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Coluna de água / bebedouro no parque para hidratar o cachorrinho da cesta.
    /// </summary>
    public class WaterColumn : MonoBehaviour
    {
        private Sprite waterIcon;
        private Transform iconTransform;

        public void Setup()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = Resources.Load<Texture2D>("Floresta/coluna_agua");
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.05f), tex.height / 2.6f);
            }
            else
            {
                sr.sprite = ParkSpriteFactory.CreateWaterColumnSprite();
            }
            sr.sortingOrder = 5;

            GameObject icon = new GameObject("WaterIcon");
            icon.transform.SetParent(transform);
            icon.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            icon.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            iconTransform = icon.transform;

            SpriteRenderer iconSR = icon.AddComponent<SpriteRenderer>();
            waterIcon = ParkSpriteFactory.CreateEmotionIcon("water");
            iconSR.sprite = waterIcon;
            iconSR.sortingOrder = 12;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.0f;
        }

        private void Update()
        {
            if (iconTransform != null)
            {
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.12f;
                iconTransform.localPosition = new Vector3(0f, 2.85f + bob, 0f);
            }
        }
    }
}
