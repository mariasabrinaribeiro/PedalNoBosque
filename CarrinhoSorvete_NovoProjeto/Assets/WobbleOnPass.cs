using UnityEngine;

public class WobbleOnPass : MonoBehaviour
{
    private Transform playerTransform;
    private Quaternion originalRotation;

    private bool isWobbling = false;
    private float wobbleTime = 0f;

    [Header("Spring Settings")]
    [SerializeField] private float wobbleAmplitude = 18f;  
    [SerializeField] private float wobbleFrequency = 14f;  
    [SerializeField] private float wobbleDecay = 3.5f;     

    private float checkCooldown = 0.2f;
    private float lastCheckTime = 0f;

    void Start()
    {
        originalRotation = transform.localRotation;
        
        // OTIMIZADO: Usando o Singleton rápido da classe DriverPro em vez do lento FindObjectOfType
        DriverPro player = DriverPro.Instance;
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            DriverPro player = DriverPro.Instance;
            if (player != null) playerTransform = player.transform;
            return;
        }

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
            
            float angle = wobbleAmplitude * Mathf.Sin(wobbleFrequency * wobbleTime) * Mathf.Exp(-wobbleDecay * wobbleTime);
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

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
