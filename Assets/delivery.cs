using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] float delay = 0.2f;
    private driverpro driver;

    void Start()
    {
        driver = GetComponent<driverpro>();
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

                    Destroy(collision.gameObject, delay);
                }
                else
                {
                    GameUIController ui = FindObjectOfType<GameUIController>();
                    if (ui != null)
                    {
                        ui.ShowWarning("CARGA CHEIA! VÁ ATÉ A CASA ROSA!");
                    }
                }
            }
        }
        else if (collision.CompareTag("Customer"))
        {
            if (driver != null && driver.cargo > 0)
            {
                int itemsDelivered = driver.cargo;
                driver.score += itemsDelivered * 50;
                driver.cargo = 0;

                Debug.Log("Delivered " + itemsDelivered + " packages. Score: " + driver.score);

                ParticleSystem ps = GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                // Show delivery message in HUD
                GameUIController ui = FindObjectOfType<GameUIController>();
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("ENTREGUE! +" + (itemsDelivered * 50) + " PONTOS");
                }
            }
        }
    }
}
