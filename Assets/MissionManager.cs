using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public enum MissionType { DeliverIceCreams, DefeatMonstersNoDamage, MaxSpeedDuration }
    public MissionType currentType;

    public int targetCount;
    public int currentProgress;
    private float speedTimer = 0f;

    private driver playerDriver;
    private GameUIController uiController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        playerDriver = FindObjectOfType<driver>();
        uiController = FindObjectOfType<GameUIController>();

        AssignNewMission();
    }

    void Update()
    {
        if (playerDriver == null)
        {
            playerDriver = FindObjectOfType<driver>();
            return;
        }

        // Track Speed Duration mission
        if (currentType == MissionType.MaxSpeedDuration)
        {
            Rigidbody2D rb = playerDriver.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Boost speed is 6.0f, normal speed is 3.5f. Threshold >= 5.0f
                float currentSpeed = rb.velocity.magnitude;
                if (currentSpeed >= 4.8f)
                {
                    speedTimer += Time.deltaTime;
                    int progressSeconds = Mathf.FloorToInt(speedTimer);
                    
                    if (progressSeconds != currentProgress)
                    {
                        currentProgress = progressSeconds;
                        UpdateHUD();

                        if (currentProgress >= targetCount)
                        {
                            CompleteMission();
                        }
                    }
                }
                else
                {
                    if (speedTimer > 0f)
                    {
                        speedTimer = 0f;
                        currentProgress = 0;
                        UpdateHUD();
                    }
                }
            }
        }
    }

    public void AssignNewMission()
    {
        // Randomly select mission type
        int rand = Random.Range(0, 3);
        currentType = (MissionType)rand;

        speedTimer = 0f;
        currentProgress = 0;

        switch (currentType)
        {
            case MissionType.DeliverIceCreams:
                targetCount = 3;
                break;
            case MissionType.DefeatMonstersNoDamage:
                targetCount = 5;
                break;
            case MissionType.MaxSpeedDuration:
                targetCount = 8; // 8 seconds of max speed
                break;
        }

        UpdateHUD();
    }

    public void OnIceCreamDelivered()
    {
        if (currentType == MissionType.DeliverIceCreams)
        {
            currentProgress++;
            UpdateHUD();

            if (currentProgress >= targetCount)
            {
                CompleteMission();
            }
        }
    }

    public void OnMonsterDefeated()
    {
        if (currentType == MissionType.DefeatMonstersNoDamage)
        {
            currentProgress++;
            UpdateHUD();

            if (currentProgress >= targetCount)
            {
                CompleteMission();
            }
        }
    }

    public void OnPlayerDamaged()
    {
        if (currentType == MissionType.DefeatMonstersNoDamage)
        {
            if (currentProgress > 0)
            {
                currentProgress = 0;
                UpdateHUD();
            }
        }
    }

    private void CompleteMission()
    {
        // Reward player
        if (playerDriver != null)
        {
            playerDriver.score += 100;
            playerDriver.ActivateGirlPower(5f); // 5 seconds of Girl Power!
        }

        if (uiController == null) uiController = FindObjectOfType<GameUIController>();
        if (uiController != null)
        {
            uiController.ShowDeliveryMessage("MISSÃO CONCLUÍDA! +100 PONTOS / GIRL POWER 5s!");
        }

        // Sound effect
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayPowerup();
        }

        // Assign a new mission
        AssignNewMission();
    }

    private void UpdateHUD()
    {
        if (uiController == null) uiController = FindObjectOfType<GameUIController>();
        if (uiController != null)
        {
            uiController.UpdateMissionPanel(GetMissionDescription(), GetMissionProgressText());
        }
    }

    private string GetMissionDescription()
    {
        switch (currentType)
        {
            case MissionType.DeliverIceCreams:
                return "Entregue 3 Sorvetes";
            case MissionType.DefeatMonstersNoDamage:
                return "Derrote 5 monstros sem sofrer dano";
            case MissionType.MaxSpeedDuration:
                return "Corra em velocidade máxima por 8s";
            default:
                return "";
        }
    }

    private string GetMissionProgressText()
    {
        if (currentType == MissionType.MaxSpeedDuration)
        {
            return $"{currentProgress}s / {targetCount}s";
        }
        return $"{currentProgress} / {targetCount}";
    }
}
