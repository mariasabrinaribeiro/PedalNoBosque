using UnityEngine;
using FlorestaBicicleta;

namespace FlorestaTopDown
{
    /// <summary>
    /// Coluna de água / bebedouro no parque para hidratar o cachorrinho da cesta.
    /// </summary>
    public class WaterColumn : MonoBehaviour
    {
        public void Setup()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = Resources.Load<Texture2D>("Floresta/coluna_agua");
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.0758f), tex.height / 2.25f);
            }
            else
            {
                sr.sprite = ParkSpriteFactory.CreateWaterColumnSprite();
            }
            sr.sortingOrder = 5;

            // Sombra de contato suave diretamente sob a base de pedra
            BiomePropsFactory.AddContactShadow(transform, new Vector3(0f, 0.01f, 0f), new Vector2(1.08f, 0.24f), 4);

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.0f;
        }
    }
}
