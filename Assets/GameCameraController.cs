using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public static GameCameraController Instance { get; private set; }

    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private float lookAheadDistance = 2.2f;
    [SerializeField] private float lookAheadSpeed = 4f;

    [Header("Shake Settings")]
    private float shakeDuration = 0f;
    private float shakeStrength = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    private Vector3 currentLookAhead = Vector3.zero;
    private float targetZ;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        targetZ = transform.position.z;

        // Deactivate Cinemachine Virtual Camera in the scene to let us control the camera directly
        GameObject vcam = GameObject.Find("Virtual Camera");
        if (vcam != null)
        {
            vcam.SetActive(false);
        }

        // Disable CinemachineBrain if it is attached to the Main Camera
        var brain = GetComponent("CinemachineBrain");
        if (brain != null)
        {
            (brain as MonoBehaviour).enabled = false;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            driver driverInstance = driver.Instance;
            if (driverInstance != null) target = driverInstance.transform;
        }
    }

    private void FixedUpdate()
    {
        if (target == null) return;

        // 1. Calculate look-ahead target in front of the car (transform.up is the forward direction)
        Vector3 targetLookAhead = target.up * lookAheadDistance;
        // Interpolate the offset smoothly to prevent camera jerking
        currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.fixedDeltaTime);

        // 2. Target position is player position + look-ahead offset
        Vector3 desiredPosition = target.position + currentLookAhead;
        desiredPosition.z = targetZ; // Keep original Z distance

        // 3. Smoothly follow the desired position
        Vector3 newPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.fixedDeltaTime);

        // 4. Apply camera shake if active
        if (shakeDuration > 0)
        {
            shakeOffset = (Vector3)Random.insideUnitCircle * shakeStrength;
            shakeDuration -= Time.fixedDeltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = newPosition + shakeOffset;
    }

    public void TriggerShake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeStrength = strength;
    }
}
