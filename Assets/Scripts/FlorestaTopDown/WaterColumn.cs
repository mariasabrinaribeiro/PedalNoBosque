using UnityEngine;
using FlorestaBicicleta;

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
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.05f), tex.height / 2.05f);
            }
            else
            {
                sr.sprite = ParkSpriteFactory.CreateWaterColumnSprite();
            }
            sr.sortingOrder = 5;

            // Sombra de contato suave na base de pedra
            BiomePropsFactory.AddContactShadow(transform, new Vector3(0f, 0.02f, 0f), new Vector2(0.95f, 0.25f), 4);

            GameObject icon = new GameObject("WaterIcon");
            icon.transform.SetParent(transform);
            icon.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            icon.transform.localScale = new Vector3(0.48f, 0.48f, 1f);
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
                float bob = Mathf.Sin(Time.time * 2.8f) * 0.08f;
                iconTransform.localPosition = new Vector3(0f, 2.15f + bob, 0f);
            }
        }
    }
}
