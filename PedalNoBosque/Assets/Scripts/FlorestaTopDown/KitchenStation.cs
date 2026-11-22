using UnityEngine;
using FlorestaBicicleta;

namespace FlorestaTopDown
{
    /// <summary>
    /// Barraca / Cozinha do Parque onde a menina pega lanches prontos
    /// (Refrigerante, Suco, Coxinha, Doces, Pipoca) e prepara Sanduíche e Hot Dog!
    /// </summary>
    public class KitchenStation : MonoBehaviour
    {
        public bool playerInside { get; private set; } = false;

        public void Setup()
        {
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            Texture2D tex = Resources.Load<Texture2D>("Floresta/barraca_cozinha");
            if (tex != null)
            {
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.1396f), tex.height / 3.65f);
            }
            else
            {
                sr.sprite = ParkSpriteFactory.CreateKitchenStallSprite();
            }
            sr.sortingOrder = 5;

            BiomePropsFactory.AddContactShadow(transform, new Vector3(0f, 0.02f, 0f), new Vector2(2.75f, 0.34f), 4);

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 3.2f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<TopDownBikeController>() != null)
                playerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<TopDownBikeController>() != null)
                playerInside = false;
        }
    }
}
