using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 2f;
    public Vector3 direction;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move forward in 2D space
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if hit enemy
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(50); // deal damage to enemy
            Destroy(gameObject); // destroy bullet on hit
        }
        else if (collision.CompareTag("Obstacle") || collision.name.Contains("fence") || collision.name.Contains("tree") || collision.name.Contains("rock"))
        {
            // Destroy bullet on wall/obstacle impact
            Destroy(gameObject);
        }
    }
}
