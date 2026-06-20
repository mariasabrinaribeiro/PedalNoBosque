using UnityEngine;

public class SpeedDeliveryGate : MonoBehaviour
{
    private GameObject leftPost;
    private GameObject rightPost;
    private LineRenderer energyLine;
    private BoxCollider2D col;

    private float pulseTime = 0f;

    // Static Cache to prevent GC spikes and memory leaks
    private static Sprite cachedPostSprite;
    private static Sprite cachedWhiteSprite;

    private static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite == null)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();
            cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        }
        return cachedWhiteSprite;
    }

    private static Sprite GetPostSprite()
    {
        if (cachedPostSprite == null)
        {
            int w = 16;
            int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color pink = new Color(1f, 0.45f, 0.75f, 1f);
            Color white = Color.white;

            for (int y = 0; y < h; y++)
            {
                Color c = ((y / 6) % 2 == 0) ? pink : white;
                for (int x = 0; x < w; x++)
                {
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
            cachedPostSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
        }
        return cachedPostSprite;
    }

    void Awake()
    {
        gameObject.name = "SpeedDeliveryGate";

        leftPost = new GameObject("LeftPost");
        leftPost.transform.SetParent(transform);
        leftPost.transform.localPosition = new Vector3(-2.2f, 0f, 0f);

        rightPost = new GameObject("RightPost");
        rightPost.transform.SetParent(transform);
        rightPost.transform.localPosition = new Vector3(2.2f, 0f, 0f);

        SpriteRenderer srL = leftPost.AddComponent<SpriteRenderer>();
        SpriteRenderer srR = rightPost.AddComponent<SpriteRenderer>();

        srL.sprite = GetPostSprite();
        srR.sprite = GetPostSprite();
        srL.sortingOrder = 3;
        srR.sortingOrder = 3;

        col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4.0f, 0.4f);
        col.offset = new Vector2(0f, 0.2f);

        energyLine = GetComponent<LineRenderer>();
        if (energyLine == null) energyLine = gameObject.AddComponent<LineRenderer>();
        
        energyLine.positionCount = 2;
        energyLine.startWidth = 0.18f;
        energyLine.endWidth = 0.18f;
        energyLine.useWorldSpace = false;
        
        energyLine.SetPosition(0, new Vector3(-2.2f, 0.8f, 0f));
        energyLine.SetPosition(1, new Vector3(2.2f, 0.8f, 0f));
        energyLine.sortingOrder = 4;

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
        pulseTime += Time.deltaTime * 6f;
        float pulse = Mathf.PingPong(pulseTime, 0.1f) + 0.12f;
        if (energyLine != null)
        {
            energyLine.startWidth = pulse;
            energyLine.endWidth = pulse;
            
            float alpha = Mathf.PingPong(pulseTime * 0.5f, 0.3f) + 0.5f;
            Color c = new Color(1f, 0.2f, 0.8f, alpha);
            energyLine.startColor = c;
            energyLine.endColor = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        driver player = collision.GetComponent<driver>();
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float currentSpeed = player.displaySpeed;
                
                if (currentSpeed >= 4.8f)
                {
                    player.score += 100;
                    player.ActivateGirlPower(5f); 

                    GameUIController ui = GameUIController.Instance;
                    if (ui != null)
                    {
                        ui.ShowDeliveryMessage("ENTREGA DE ALTA VELOCIDADE! +100 PONTOS / GIRL POWER!");
                    }

                    if (GameAudioController.Instance != null)
                    {
                        GameAudioController.Instance.PlayGirlPower();
                    }

                    SpawnSuccessParticles();
                    gameObject.SetActive(false);
                }
                else
                {
                    GameUIController ui = GameUIController.Instance;
                    if (ui != null)
                    {
                        ui.ShowWarning("MUITO LENTO! CORRA MAIS PARA ENTREGAR!");
                    }

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

        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 12; i++)
        {
            GameObject p = new GameObject("GateParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 12f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = whiteSprite;
            particleSr.color = (i % 2 == 0) ? pinkColor : goldColor; 
            particleSr.sortingOrder = 6;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(3f, 7f);
        }
    }
}
