using UnityEngine;

public class SpeedDeliveryGate : MonoBehaviour
{
    private GameObject leftPost;
    private GameObject rightPost;
    private LineRenderer energyLine;
    private BoxCollider2D col;

    private float pulseTime = 0f;

    void Awake()
    {
        gameObject.name = "SpeedDeliveryGate";

        // 1. Create left and right post child GameObjects
        leftPost = new GameObject("LeftPost");
        leftPost.transform.SetParent(transform);
        leftPost.transform.localPosition = new Vector3(-2.2f, 0f, 0f);

        rightPost = new GameObject("RightPost");
        rightPost.transform.SetParent(transform);
        rightPost.transform.localPosition = new Vector3(2.2f, 0f, 0f);

        // Add SpriteRenderers to posts
        SpriteRenderer srL = leftPost.AddComponent<SpriteRenderer>();
        SpriteRenderer srR = rightPost.AddComponent<SpriteRenderer>();

        Sprite postSprite = CreatePostSprite(16, 48);
        srL.sprite = postSprite;
        srR.sprite = postSprite;
        srL.sortingOrder = 3;
        srR.sortingOrder = 3;

        // 2. Set up BoxCollider2D trigger zone between posts
        col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4.0f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        // 3. Set up LineRenderer for the glowing neon energy line
        energyLine = GetComponent<LineRenderer>();
        if (energyLine == null) energyLine = gameObject.AddComponent<LineRenderer>();
        
        energyLine.positionCount = 2;
        energyLine.startWidth = 0.18f;
        energyLine.endWidth = 0.18f;
        energyLine.useWorldSpace = false;
        
        // Connect the top of the posts
        energyLine.SetPosition(0, new Vector3(-2.2f, 0.8f, 0f));
        energyLine.SetPosition(1, new Vector3(2.2f, 0.8f, 0f));
        energyLine.sortingOrder = 4;

        // Assign unlit color shader to line
        Shader unlitShader = Shader.Find("Sprites/Default");
        if (unlitShader != null)
        {
            energyLine.material = new Material(unlitShader);
        }

        ConfigureColors();
    }

    void OnEnable()
    {
        pulseTime = 0f;
        if (energyLine != null) energyLine.enabled = true;
        if (leftPost != null) leftPost.SetActive(true);
        if (rightPost != null) rightPost.SetActive(true);
    }

    private void ConfigureColors()
    {
        Color pinkGlow = new Color(1f, 0.2f, 0.8f, 0.75f);
        energyLine.startColor = pinkGlow;
        energyLine.endColor = pinkGlow;
    }

    void Update()
    {
        // Pulsate energy line width and opacity to represent high-energy glow
        pulseTime += Time.deltaTime * 6f;
        float pulse = Mathf.PingPong(pulseTime, 0.1f) + 0.12f;
        if (energyLine != null)
        {
            energyLine.startWidth = pulse;
            energyLine.endWidth = pulse;
            
            // oscilate color intensity
            float alpha = Mathf.PingPong(pulseTime * 0.5f, 0.3f) + 0.5f;
            Color c = new Color(1f, 0.2f, 0.8f, alpha);
            energyLine.startColor = c;
            energyLine.endColor = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        driverpro player = collision.GetComponent<driverpro>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float currentSpeed = player.displaySpeed;
                
                // Speed threshold: 4.8f (fast, requires boost or full speed steering)
                if (currentSpeed >= 4.8f)
                {
                    // Success!
                    player.score += 100;
                    player.ActivateGirlPower(5f); // 5s Girl Power!

                    // Alert HUD
                    GameUIController ui = FindObjectOfType<GameUIController>();
                    if (ui != null)
                    {
                        ui.ShowDeliveryMessage("ENTREGA DE ALTA VELOCIDADE! +100 PONTOS / GIRL POWER!");
                    }

                    // Play arpeggio sound
                    if (GameAudioController.Instance != null)
                    {
                        GameAudioController.Instance.PlayGirlPower();
                    }

                    // Particle splash
                    SpawnSuccessParticles();

                    // Deactivate speed gate back to pool
                    gameObject.SetActive(false);
                }
                else
                {
                    // Too slow!
                    GameUIController ui = FindObjectOfType<GameUIController>();
                    if (ui != null)
                    {
                        ui.ShowWarning("MUITO LENTO! CORRA MAIS PARA ENTREGAR!");
                    }

                    // Play warning SFX
                    if (GameAudioController.Instance != null)
                    {
                        GameAudioController.Instance.PlayWarning();
                    }
                }
            }
        }
    }

    private void SpawnSuccessParticles()
    {
        Color pinkColor = new Color(1f, 0.2f, 0.8f, 1f);
        Color goldColor = new Color(1f, 0.843f, 0f, 1f);

        for (int i = 0; i < 12; i++)
        {
            GameObject p = new GameObject("GateParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 12f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = CreateTinySprite(6, 6, (i % 2 == 0) ? pinkColor : goldColor);
            particleSr.sortingOrder = 6;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(3f, 7f);
        }
    }

    private Sprite CreatePostSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color pink = new Color(1f, 0.45f, 0.75f, 1f);
        Color white = Color.white;

        for (int y = 0; y < h; y++)
        {
            // Pink and white stripes pattern based on y coordinates
            Color c = ((y / 6) % 2 == 0) ? pink : white;
            for (int x = 0; x < w; x++)
            {
                // Rounded top
                if (y >= h - 4)
                {
                    float dx = x - w / 2f;
                    float dy = y - (h - 4);
                    if (dx * dx + dy * dy > (w / 2f) * (w / 2f))
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
    }

    private Sprite CreateTinySprite(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, color);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }
}
