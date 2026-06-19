using UnityEngine;

public class DynamicEnvironmentInitializer : MonoBehaviour
{
    void Start()
    {
        // Find all GameObjects in the active scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        int treeCount = 0;
        int houseCount = 0;

        foreach (GameObject go in allObjects)
        {
            string lowerName = go.name.ToLower();

            // 1. Identify trees, lollipops, or street props and attach WobbleOnPass
            if (lowerName.Contains("tree") || lowerName.Contains("arvore") || 
                lowerName.Contains("lollipop") || lowerName.Contains("pirulito") || 
                lowerName.Contains("obstaculo") || lowerName.Contains("fence") || 
                lowerName.Contains("rock") || lowerName.Contains("pedra"))
            {
                // Only attach if it has a collider (interacts with player)
                if (go.GetComponent<Collider2D>() != null && go.GetComponent<WobbleOnPass>() == null)
                {
                    go.AddComponent<WobbleOnPass>();
                    treeCount++;
                }
            }

            // 2. Identify houses or residential buildings and attach HouseFlasher
            if (lowerName.Contains("house") || lowerName.Contains("casa") || 
                lowerName.Contains("building") || lowerName.Contains("customer"))
            {
                if (go.GetComponent<SpriteRenderer>() != null && go.GetComponent<HouseFlasher>() == null)
                {
                    go.AddComponent<HouseFlasher>();
                    houseCount++;
                }
            }
        }

        Debug.Log($"Dynamic Environment Initialized: Hooked {treeCount} sways on props and {houseCount} glows on houses.");
    }
}

public class HouseFlasher : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;
    private float pulseSpeed;
    private float pulseIntensity;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalColor = sr.color;
        }
        pulseSpeed = Random.Range(0.8f, 1.8f);
        pulseIntensity = Random.Range(0.08f, 0.16f);
    }

    void Update()
    {
        if (sr != null)
        {
            // Pulsate the color tint slightly towards a warm glowing yellow
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, pulseIntensity);
            sr.color = Color.Lerp(originalColor, new Color(1f, 0.95f, 0.75f, 1f), pulse);
        }
    }
}
