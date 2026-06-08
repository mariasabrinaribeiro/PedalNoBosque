using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int currentHealth;

    [SerializeField] private int collisionDamage = 20;
    [SerializeField] private int scoreReward = 10;
    [SerializeField] private int healthReward = 15;

    void Start()
    {
        currentHealth = maxHealth;

        // Ensure it has a CircleCollider2D set as trigger
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check collision with the car
        driverpro player = collision.GetComponent<driverpro>();
        if (player != null)
        {
            player.TakeDamage(collisionDamage);
            
            // Spawn a visual effect or message
            GameUIController ui = FindObjectOfType<GameUIController>();
            if (ui != null)
            {
                ui.ShowWarning("BATEU NO MONSTRO! -20 VIDA");
            }

            Die(false); // destroy without rewards
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die(true); // destroy with rewards
        }
    }

    private void Die(bool giveRewards)
    {
        if (giveRewards)
        {
            driverpro player = FindObjectOfType<driverpro>();
            if (player != null)
            {
                player.score += scoreReward;
                player.health = Mathf.Min(player.health + healthReward, player.maxHealth);
                
                GameUIController ui = FindObjectOfType<GameUIController>();
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("MONSTRO DESTRUÍDO! +10 PTS / +15 VIDA");
                }
            }
        }

        // Play explosion particles if possible
        ParticleSystem parentPS = FindObjectOfType<ParticleSystem>(); // fallback
        if (parentPS != null)
        {
            // Instantiate particles at our position
            ParticleSystem ps = Instantiate(parentPS, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }

        Destroy(gameObject);
    }
}
