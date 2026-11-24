using UnityEngine;

public class SpeedDeliveryGate : MonoBehaviour
{
    private GameObject leftPost;
    private GameObject rightPost;
    private LineRenderer energyLine;
    private BoxCollider2D col;

    private float pulseTime = 0f;

    // Cache estático para evitar vazamento de texturas e picos de GC
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

        // ASSINALA O SPRITE BRANCO ESTÁTICO PARA AS CORES TINTAREM CORRETAMENTE
        Sprite whiteSprite = GetWhiteSprite();
        srL.sprite = whiteSprite;
        srR.sprite = whiteSprite;

        // OTIMIZADO: Define os postes visualmente usando tintas nativas sem loops geradores de imagem
        srL.color = new Color(1f, 0.45f, 0.75f, 1f);
        srR.color = new Color(1f, 0.45f, 0.75f, 1f);
        leftPost.transform.localScale = new Vector3(0.2f, 1f, 1f);
        rightPost.transform.localScale = new Vector3(0.2f, 1f, 1f);
        
        srL.sortingLayerName = "PropsAndBuildings";
        srR.sortingLayerName = "PropsAndBuildings";
        srL.sortingOrder = 5;
        srR.sortingOrder = 5;

        col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4.0f, 0.6f);
        col.offset = new Vector2(0f, 0.2f);

        energyLine = GetComponent<LineRenderer>();
        if (energyLine == null) energyLine = gameObject.AddComponent<LineRenderer>();

        energyLine.positionCount = 2;
        energyLine.startWidth = 0.20f;
        energyLine.endWidth = 0.20f;
        energyLine.useWorldSpace = false;

        energyLine.SetPosition(0, new Vector3(-2.2f, 0.8f, 0f));
        energyLine.SetPosition(1, new Vector3(2.2f, 0.8f, 0f));
        energyLine.sortingLayerName = "VFX";
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
        // CORRIGIDO: de driver para DriverPro
        DriverPro player = collision.GetComponent<DriverPro>(); 
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float currentSpeed = player.displaySpeed;
                
                if (currentSpeed >= 4.8f)
                {
                    // ETAPA 4: Pontuação legível e balanceada (+150 pts no Portão de Velocidade)
                    if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(ControladorJogo.PontosPorPortaoVelocidade);
                    else player.score += ControladorJogo.PontosPorPortaoVelocidade;
                    player.ActivateGirlPower(5f);

                    GameUIController ui = GameUIController.Instance;
                    if (ui != null)
                    {
                        ui.ShowDeliveryMessage("ENTREGA VELOZ! +" + ControladorJogo.PontosPorPortaoVelocidade + " PTS • GIRL POWER!");
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
        // ETAPA 4: -40% emissão (12 -> 7) e opacidade máxima 35% nas partículas rosas na camada VFX
        Color pinkColor = new Color(1f, 0.2f, 0.8f, 0.35f);
        Color goldColor = new Color(1f, 0.843f, 0f, 0.35f);

        Sprite whiteSprite = GetWhiteSprite();

        for (int i = 0; i < 7; i++)
        {
            GameObject p = new GameObject("GateParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 7f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = whiteSprite;
            particleSr.color = (i % 2 == 0) ? pinkColor : goldColor;
            particleSr.sortingLayerName = "VFX";
            particleSr.sortingOrder = 6;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(3f, 6.5f);
        }
    }
}
