using UnityEngine;

/// <summary>
/// Portão Temático de Entrega Veloz em Pixel Art (FilterMode.Point, PPU = 32):
/// - Tapete listrado de confeitos com setas de impulso integrado à largura exata da via (GroundDetails)
/// - Dois postes de bengala doce com estrelas douradas alinhados ao meio-fio (PropsAndBuildings)
/// - Fita luminosa pulsante suave na camada VFX (opacidade controlada)
/// Substitui completamente o antigo quadrado isolado com duas linhas rosas.
/// </summary>
public class SpeedDeliveryGate : MonoBehaviour
{
    public float laneWidth = 4.3125f;

    private GameObject matObj;
    private GameObject leftPost;
    private GameObject rightPost;
    private SpriteRenderer bannerSr;
    private BoxCollider2D col;

    private float pulseTime;

    private static Sprite cachedMatSprite;
    private static Sprite cachedPostSprite;
    private static Sprite cachedBannerSprite;
    private static Sprite cachedParticleSprite;

    private static Sprite GetMatSprite()
    {
        if (cachedMatSprite != null) return cachedMatSprite;
        const int W = 138; // 138 / 32 = 4.3125 world units (largura exata da pista de biscoito!)
        const int H = 42;  // 42 / 32 = 1.3125 world units de profundidade
        const int P = 3;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 ink = new Color32(64, 34, 68, 255);
        Color32 pink = new Color32(252, 134, 194, 240);
        Color32 cream = new Color32(255, 250, 236, 245);
        Color32 gold = new Color32(255, 218, 92, 255);
        Color32 lav = new Color32(198, 152, 246, 235);

        Color32[] px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        int gw = W / P, gh = H / P;
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                Color32 c;
                if (gy == 0 || gy == gh - 1 || gx == 0 || gx == gw - 1)
                {
                    c = ink;
                }
                else if (gy == 1 || gy == gh - 2)
                {
                    c = ((gx / 3) % 2 == 0) ? gold : cream;
                }
                else
                {
                    // Listras diagonais doces estilo faixa de largada + 3 chevrons dourados
                    int stripe = ((gx + gy) / 3) % 3;
                    c = (stripe == 0) ? pink : (stripe == 1 ? cream : lav);

                    int midY = gh / 2;
                    for (int ch = 0; ch < 3; ch++)
                    {
                        int cx = 11 + ch * 12;
                        int dy = Mathf.Abs(gy - midY);
                        if (gx == cx - dy || gx == cx - dy + 1) c = gold;
                    }
                }

                for (int py = 0; py < P; py++)
                    for (int pxo = 0; pxo < P; pxo++)
                        px[(gy * P + py) * W + (gx * P + pxo)] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedMatSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f);
        return cachedMatSprite;
    }

    private static Sprite GetPostSprite()
    {
        if (cachedPostSprite != null) return cachedPostSprite;
        const int W = 30;
        const int H = 54;
        const int P = 3;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 ink = new Color32(64, 34, 68, 255);
        Color32 pink = new Color32(252, 118, 186, 255);
        Color32 cream = new Color32(255, 250, 240, 255);
        Color32 gold = new Color32(255, 224, 96, 255);
        Color32 shadow = new Color32(64, 34, 68, 85);

        Color32[] px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        int gw = W / P, gh = H / P;
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                Color32 c = clear;
                // Sombra na base
                if (gy <= 1 && gx >= 2 && gx <= 7) c = shadow;
                // Base arredondada
                else if (gy == 2 && gx >= 2 && gx <= 7) c = ink;
                else if (gy == 3 && gx >= 3 && gx <= 6) c = pink;
                // Haste listrada de bengala doce
                else if (gy >= 4 && gy <= 12)
                {
                    if (gx == 3 || gx == 6) c = ink;
                    else if (gx == 4 || gx == 5) c = (((gy + gx) % 3 == 0) ? pink : cream);
                }
                // Estrela dourada no topo
                else if (gy >= 13 && gy <= 16)
                {
                    int dx = Mathf.Abs(gx - 4);
                    int dy = Mathf.Abs(gy - 15);
                    if (dx + dy <= 2) c = (dx + dy == 2) ? ink : gold;
                    else if (gx == 5 && dy <= 1) c = gold;
                }

                if (c.a == 0) continue;
                for (int py = 0; py < P; py++)
                    for (int pxo = 0; pxo < P; pxo++)
                        px[(gy * P + py) * W + (gx * P + pxo)] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedPostSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.15f), 32f);
        return cachedPostSprite;
    }

    private static Sprite GetBannerSprite()
    {
        if (cachedBannerSprite != null) return cachedBannerSprite;
        const int W = 138;
        const int H = 12;
        const int P = 3;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32 pink = new Color32(255, 130, 210, 220);
        Color32 gold = new Color32(255, 226, 110, 235);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32[] px = new Color32[W * H];

        int gw = W / P, gh = H / P;
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                Color32 c = (gy == 1 || gy == 2) ? (((gx / 3) % 2 == 0) ? pink : gold) : white;
                for (int py = 0; py < P; py++)
                    for (int pxo = 0; pxo < P; pxo++)
                        px[(gy * P + py) * W + (gx * P + pxo)] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedBannerSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 32f);
        return cachedBannerSprite;
    }

    private static Sprite GetParticleSprite()
    {
        if (cachedParticleSprite != null) return cachedParticleSprite;
        Texture2D tex = new Texture2D(6, 6, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < 6; y++)
            for (int x = 0; x < 6; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        cachedParticleSprite = Sprite.Create(tex, new Rect(0, 0, 6, 6), new Vector2(0.5f, 0.5f), 32f);
        return cachedParticleSprite;
    }

    void Awake()
    {
        // Limpa eventual LineRenderer antigo caso exista em prefabs/objetos legados
        LineRenderer oldLr = GetComponent<LineRenderer>();
        if (oldLr != null) oldLr.enabled = false;

        float halfW = laneWidth * 0.5f;
        int yOrder = Mathf.RoundToInt(-transform.position.y * 10f);

        // 1. Tapete listrado de confeitos integrado ao piso da pista (GroundDetails)
        matObj = new GameObject("CandyGateMat");
        matObj.transform.SetParent(transform, false);
        matObj.transform.localPosition = Vector3.zero;
        SpriteRenderer matSr = matObj.AddComponent<SpriteRenderer>();
        matSr.sprite = GetMatSprite();
        matSr.sortingLayerName = "GroundDetails";
        matSr.sortingOrder = 5;

        // 2. Postes de bengala doce alinhados aos dois meios-fios (PropsAndBuildings)
        Sprite postSp = GetPostSprite();
        leftPost = new GameObject("LeftCandyPost");
        leftPost.transform.SetParent(transform, false);
        leftPost.transform.localPosition = new Vector3(-halfW - 0.12f, 0f, 0f);
        SpriteRenderer srL = leftPost.AddComponent<SpriteRenderer>();
        srL.sprite = postSp;
        srL.sortingLayerName = "PropsAndBuildings";
        srL.sortingOrder = yOrder + 2;

        rightPost = new GameObject("RightCandyPost");
        rightPost.transform.SetParent(transform, false);
        rightPost.transform.localPosition = new Vector3(halfW + 0.12f, 0f, 0f);
        SpriteRenderer srR = rightPost.AddComponent<SpriteRenderer>();
        srR.sprite = postSp;
        srR.sortingLayerName = "PropsAndBuildings";
        srR.sortingOrder = yOrder + 2;

        // 3. Fita luminosa de arco suave no topo (VFX, opacidade <= 0.35)
        GameObject bannerObj = new GameObject("CandyGateRibbon");
        bannerObj.transform.SetParent(transform, false);
        bannerObj.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        bannerSr = bannerObj.AddComponent<SpriteRenderer>();
        bannerSr.sprite = GetBannerSprite();
        bannerSr.color = new Color(1f, 0.95f, 1f, 0.32f);
        bannerSr.sortingLayerName = "VFX";
        bannerSr.sortingOrder = 4;

        col = GetComponent<BoxCollider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(laneWidth, 1.1f);
        col.offset = Vector2.zero;
    }

    void OnEnable()
    {
        pulseTime = 0f;
        if (matObj != null) matObj.SetActive(true);
        if (leftPost != null) leftPost.SetActive(true);
        if (rightPost != null) rightPost.SetActive(true);
        if (bannerSr != null) bannerSr.enabled = true;
    }

    void Update()
    {
        pulseTime += Time.deltaTime * 4.5f;
        if (bannerSr != null)
        {
            float alpha = 0.22f + 0.13f * (0.5f + 0.5f * Mathf.Sin(pulseTime));
            bannerSr.color = new Color(1f, 0.95f, 1f, alpha);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DriverPro player = collision.GetComponent<DriverPro>();
        if (player == null) return;

        float currentSpeed = player.displaySpeed;
        if (currentSpeed >= 4.8f)
        {
            if (ControladorJogo.Instance != null) ControladorJogo.Instance.AdicionarPontos(ControladorJogo.PontosPorPortaoVelocidade);
            else player.score += ControladorJogo.PontosPorPortaoVelocidade;
            player.ActivateGirlPower(5f);

            GameUIController ui = GameUIController.Instance;
            if (ui != null)
                ui.ShowDeliveryMessage("ENTREGA VELOZ! +" + ControladorJogo.PontosPorPortaoVelocidade + " PTS • GIRL POWER!");

            if (GameAudioController.Instance != null)
                GameAudioController.Instance.PlayGirlPower();

            SpawnSuccessParticles();
            gameObject.SetActive(false);
        }
        else
        {
            GameUIController ui = GameUIController.Instance;
            if (ui != null)
                ui.ShowWarning("MUITO LENTO! CORRA MAIS PARA ATRAVESSAR O PORTÃO!");

            if (GameAudioController.Instance != null)
                GameAudioController.Instance.PlayWarning();
        }
    }

    private void SpawnSuccessParticles()
    {
        Color pinkColor = new Color(1f, 0.2f, 0.8f, 0.35f);
        Color goldColor = new Color(1f, 0.843f, 0f, 0.35f);
        Sprite particleSprite = GetParticleSprite();

        for (int i = 0; i < 7; i++)
        {
            GameObject p = new GameObject("GateParticle");
            p.transform.position = transform.position;

            float angle = i * (360f / 7f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            SpriteRenderer particleSr = p.AddComponent<SpriteRenderer>();
            particleSr.sprite = particleSprite;
            particleSr.color = (i % 2 == 0) ? pinkColor : goldColor;
            particleSr.sortingLayerName = "VFX";
            particleSr.sortingOrder = 6;

            ExplosionParticle script = p.AddComponent<ExplosionParticle>();
            script.direction = dir;
            script.speed = Random.Range(3f, 6.5f);
        }
    }
}
