using UnityEngine;

public class Delivery : MonoBehaviour
{
    [SerializeField] float delay = 0.2f;
    private DriverPro driver;

    void Start()
    {
        driver = GetComponent<DriverPro>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.collider);
    }

    private void HandleCollision(Collider2D collision)
    {
        if (collision.CompareTag("package"))
        {
            if (driver != null)
            {
                if (driver.cargo < driver.maxCargo)
                {
                    collision.enabled = false; // Evita coleta duplicada durante o delay de destruição
                    driver.cargo++;
                    // ETAPA 4: Pontuação balanceada e legível (+25 pts por coleta)
                    if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(ControladorJogo.PontosPorColeta);
                    else driver.score += ControladorJogo.PontosPorColeta;
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
                    GameUIController ui = GameUIController.Instance;
                    if (ui != null)
                    {
                        ui.ShowWarning("CARGA CHEIA! VÁ ATÉ A ÁREA DE DESCARGA!");
                    }
                }
            }
        }
        else if (IsDeliveryPoint(collision))
        {
            if (driver != null && driver.cargo > 0)
            {
                int itemsDelivered = driver.cargo;
                int pontosEntrega = itemsDelivered * ControladorJogo.PontosPorEntrega; // ETAPA 4: 100 pts por entrega!
                if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(pontosEntrega);
                else driver.score += pontosEntrega;
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
                    MissionManager.Instance.OnIceCreamDelivered(itemsDelivered);
                }

                // Show delivery message in HUD
                GameUIController ui = GameUIController.Instance;
                if (ui != null)
                {
                    ui.ShowDeliveryMessage("ENTREGUE! GIRL POWER ATIVADO! +" + pontosEntrega + " PTS");
                }
            }
        }
    }

    private bool IsDeliveryPoint(Collider2D collision)
    {
        if (collision.CompareTag("Customer") || collision.CompareTag("Pink House"))
            return true;

        Transform current = collision.transform;
        while (current != null)
        {
            string nameLower = current.name.ToLower();
            if (nameLower.Contains("descarga") || nameLower.Contains("tapete"))
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }
}
