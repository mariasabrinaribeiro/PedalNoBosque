using UnityEngine;

public class WobbleOnPass : MonoBehaviour
{
    private Transform playerTransform;
    private Quaternion originalRotation;

    private bool isWobbling = false;
    private float wobbleTime = 0f;

    [Header("Spring Settings")]
    [SerializeField] private float wobbleAmplitude = 18f;  // starting tilt angle
    [SerializeField] private float wobbleFrequency = 14f;  // wobble speed
    [SerializeField] private float wobbleDecay = 3.5f;     // how fast it dampens to stop

    private float checkCooldown = 0.2f;
    private float lastCheckTime = 0f;

    void Start()
    {
        originalRotation = transform.localRotation;
        
        driverpro player = FindObjectOfType<driverpro>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            // Try to find player if not set
            driverpro player = FindObjectOfType<driverpro>();
            if (player != null) playerTransform = player.transform;
            return;
        }

        // Proximity check with cooldown to avoid CPU spikes
        if (!isWobbling && Time.time - lastCheckTime > checkCooldown)
        {
            lastCheckTime = Time.time;
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < 2.5f)
            {
                TriggerWobble();
            }
        }

        if (isWobbling)
        {
            wobbleTime += Time.deltaTime;
            
            // Damped spring oscillation equation: Amplitude * sin(freq * t) * exp(-decay * t)
            float angle = wobbleAmplitude * Mathf.Sin(wobbleFrequency * wobbleTime) * Mathf.Exp(-wobbleDecay * wobbleTime);
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

            // Stop when oscillation becomes negligible
            if (wobbleTime > 2.0f)
            {
                isWobbling = false;
                transform.localRotation = originalRotation;
            }
        }
    }

    public void TriggerWobble()
    {
        isWobbling = true;
        wobbleTime = 0f;
    }
}
