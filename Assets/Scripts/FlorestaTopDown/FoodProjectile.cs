using UnityEngine;

namespace FlorestaTopDown
{
    /// <summary>
    /// Alimento específico arremessado pela menina em arco até o animal.
    /// </summary>
    public class FoodProjectile : MonoBehaviour
    {
        public FoodType foodType { get; private set; }
        private Vector3 velocity;
        private float lifetime = 1.15f;
        private float elapsed = 0f;
        private System.Action<ParkAnimal, FoodType> onHitAnimal;

        public void Launch(FoodType type, Vector3 direction, float speed, Sprite sprite, System.Action<ParkAnimal, FoodType> hitCallback)
        {
            foodType = type;
            velocity = direction.normalized * speed;
            onHitAnimal = hitCallback;

            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 18;

            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            transform.Rotate(0f, 0f, -360f * Time.deltaTime);

            float t = Mathf.Clamp01(elapsed / lifetime);
            float arcScale = 0.85f + Mathf.Sin(t * Mathf.PI) * 0.55f;
            transform.localScale = new Vector3(arcScale, arcScale, 1f);

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ParkAnimal animal = other.GetComponent<ParkAnimal>();
            if (animal != null)
            {
                onHitAnimal?.Invoke(animal, foodType);
                Destroy(gameObject);
            }
        }
    }
}
