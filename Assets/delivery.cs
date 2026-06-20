using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] float delay = 0.2f;
    private driver driver;

    void Start()
    {
        driver = GetComponent<driver>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("package"))
        {
            if (driver != null)
            {
                if (driver.cargo < driver.maxCargo)
                {
                    driver.cargo++;
                    Debug.Log("Picked up package. Cargo: " + driver.cargo);

                    ParticleSystem ps = GetComponent<ParticleSystem>();
                    if (ps != null) ps.Play();

                    // Play procedural pick up sound if audio controller is ready
                    if (GameAudioController.Instance != null)
                    {
                        GameAudioController.Instance.PlayHit();
                    }

                    Destroy(collision.gameObject, delay);
                }
                else
                {
                    GameUIController ui = FindObjectOfType<GameUIController>();
                    if (ui != null)
                    {
                        ui.ShowWarning("CARGA CHEIA! VÁ ATÉ A ÁREA DE DESCARGA!");
                    }
                }
            }
        }
        else if (collision.CompareTag("Customer") || collision.CompareTag("Pink House") || 
                 collision.name.ToLower().Contains("descarga") || 
                 collision.name.ToLower().Contains("tapete"))
        {
            if (driver != null && driver.cargo > 0)
            {
                int itemsDelivered = driver.cargo;
                driver.score += itemsDelivered * 50;
                driver.cargo = 0;

                Debug.Log("Delivered " + itemsDelivered + " packages. Score: " + driver.score);

                // Play delivery particles
                ParticleSystem ps = GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                // 1. Reward the player with "Girl Power" boost for 7 seconds!
                driver.ActivateGirlPower(7f);

                // 2. Notify MissionManager of delivery progress
                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.OnIceCreamDelivered();
                }

                // Show delivery message in HUD
                GameUIController ui = FindObjectOfType<GameUIController>();
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("ENTREGUE! GIRL POWER ATIVADO! +" + (itemsDelivered * 50) + " PONTOS");
                }
            }
        }
    }
}
