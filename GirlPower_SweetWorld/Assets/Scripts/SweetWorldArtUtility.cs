using UnityEngine;

/// <summary>
/// Gerador procedural de assets brilhantes estilo ilustração cartoon (mesma paleta da capa Girl Power):
/// - Estrada de creme dourado com bordas de balas de goma arco-íris e confeitos coloridos
/// - Bolha mágica rosa cintilante (projétil idêntico ao da capa!)
/// - Sombra de contato suave, brilho mágico, estrelinha de confete, portão arco-íris e poça de caramelo
/// - Sprites 9-slice arredondados para UI brilhante
/// </summary>
public static class SweetWorldArtUtility
{
    private static Sprite cachedRoadSprite;
    private static Sprite cachedBubbleSprite;
    private static Sprite cachedShadowSprite;
    private static Sprite cachedGlowSprite;
    private static Sprite cachedStarSprite;
    private static Sprite cachedSlowPuddleSprite;
    private static Sprite cachedSpeedMatSprite;
    private static Sprite cachedConeBeaconSprite;
    private static Sprite cachedUiPillSprite;

    public static Sprite GetCoverCandyRoadSprite()
    {
        if (cachedRoadSprite != null) return cachedRoadSprite;
        const int W = 1024;
        const int H = 420;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color32[] px = new Color32[W * H];
        Color32 ink = new Color32(58, 22, 86, 255);
        Color32 roadLight = new Color32(255, 243, 198, 255);
        Color32 roadBase = new Color32(252, 226, 152, 255);
        Color32 roadShade = new Color32(240, 196, 118, 255);
        Color32 grassTop = new Color32(114, 214, 158, 255);
        Color32 grassDark = new Color32(66, 168, 128, 255);

        Color32[] gumdrops =
        {
            new Color32(255, 92, 168, 255),  // Morango
            new Color32(255, 214, 72, 255),  // Limão
            new Color32(106, 218, 255, 255), // Céu
            new Color32(182, 114, 255, 255), // Uva
            new Color32(104, 232, 156, 255)  // Menta
        };

        // 1. Base de creme dourado suave (mais claro no centro, quente nas bordas) + faixa de grama superior
        for (int y = 0; y < H; y++)
        {
            float ny = y / (float)(H - 1);
            for (int x = 0; x < W; x++)
            {
                Color32 c;
                if (y >= H - 22)
                {
                    c = (y > H - 8) ? new Color32(0, 0, 0, 0) : Color32.Lerp(grassDark, grassTop, (y - (H - 22)) / 14f);
                }
                else if (y <= 44 || y >= H - 64)
                {
                    c = roadShade;
                }
                else
                {
                    float distFromCenter = Mathf.Abs(ny - 0.46f) / 0.36f;
                    c = Color32.Lerp(roadLight, roadBase, Mathf.Clamp01(distFromCenter));
                    // Faixas sutis de 3 pistas em creme clarinho (guia visual sem poluir!)
                    if (Mathf.Abs(y - 148) <= 3 || Mathf.Abs(y - 252) <= 3)
                    {
                        if ((x % 128) < 68) c = new Color32(255, 251, 230, 255);
                    }
                }
                px[y * W + x] = c;
            }
        }

        void DrawRoundedGumdrop(float cx, float cy, float rx, float ry, Color32 fill)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 5));
            int x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + rx + 5));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 5));
            int y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + ry + 5));
            Color32 hi = Color32.Lerp(fill, new Color32(255, 255, 255, 255), 0.55f);
            Color32 sh = Color32.Lerp(fill, ink, 0.28f);

            for (int y = y0; y <= y1; y++)
            {
                float dy = (y - cy) / ry;
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / rx;
                    float d = dx * dx * dx * dx + dy * dy * dy * dy; // Squircle gordinho igual bala de goma!
                    if (d <= 1.38f)
                    {
                        if (d > 0.92f) px[y * W + x] = ink;
                        else
                        {
                            Color32 col = (dy < -0.25f) ? sh : fill;
                            if (dy > 0.22f && dy < 0.62f && Mathf.Abs(dx + 0.15f) < 0.45f) col = hi;
                            px[y * W + x] = col;
                        }
                    }
                }
            }
        }

        // 2. Bordas de balas de goma arco-íris (superior e inferior — marca registrada da capa!)
        const int stoneCount = 16;
        float step = W / (float)stoneCount;
        for (int i = 0; i < stoneCount; i++)
        {
            float cx = (i + 0.5f) * step;
            DrawRoundedGumdrop(cx, 24f, step * 0.46f, 20f, gumdrops[i % gumdrops.Length]);
            DrawRoundedGumdrop(cx, H - 38f, step * 0.46f, 16f, gumdrops[(i + 2) % gumdrops.Length]);
        }

        // 3. Confeitos coloridos espalhados com bom gosto pela pista (com contorninho escuro nítido!)
        void DrawSprinkle(int cx, int cy, float angleRad, Color32 col)
        {
            float ca = Mathf.Cos(angleRad), sa = Mathf.Sin(angleRad);
            for (int dy = -12; dy <= 12; dy++)
            {
                for (int dx = -12; dx <= 12; dx++)
                {
                    float lx = dx * ca + dy * sa;
                    float ly = -dx * sa + dy * ca;
                    float clx = Mathf.Clamp(lx, -7f, 7f);
                    float d = Mathf.Sqrt((lx - clx) * (lx - clx) + ly * ly);
                    int tx = (cx + dx + W) % W;
                    int ty = cy + dy;
                    if (ty < 46 || ty > H - 58) continue;
                    if (d <= 4.4f) px[ty * W + tx] = (d > 2.8f) ? ink : col;
                }
            }
        }

        for (int i = 0; i < 38; i++)
        {
            int sx = (i * 197 + 41) % W;
            // Concentra mais confeitos perto das margens e alguns esparsos na pista
            int sy = (i % 3 == 0) ? (56 + (i * 19) % 42) : ((i % 3 == 1) ? (H - 96 + (i * 13) % 30) : (108 + (i * 47) % 185));
            float ang = ((i * 53) % 314) * 0.01f;
            DrawSprinkle(sx, sy, ang, gumdrops[i % gumdrops.Length]);
        }

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedRoadSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        return cachedRoadSprite;
    }

    public static Sprite GetMagicBubbleSprite()
    {
        if (cachedBubbleSprite != null) return cachedBubbleSprite;
        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] px = new Color32[S * S];
        float mid = (S - 1) * 0.5f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x - mid) / 56f;
                float dy = (y - mid) / 56f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 1.12f) { px[y * S + x] = new Color32(0, 0, 0, 0); continue; }

                if (r > 0.85f)
                {
                    // Halo externo rosa mágico luminoso
                    float halo = Mathf.Clamp01(1f - (r - 0.85f) / 0.27f);
                    px[y * S + x] = new Color32(255, 110, 224, (byte)(halo * halo * 165));
                }
                else
                {
                    // Esfera rosa brilhante + reflexo branco + estrelinhas internas!
                    float rim = Mathf.Pow(r / 0.85f, 2.2f);
                    Color core = Color.Lerp(new Color(1f, 0.62f, 0.94f, 0.95f), new Color(0.95f, 0.14f, 0.72f, 1f), rim);
                    if (r > 0.77f) core = Color.Lerp(core, Color.white, 0.75f);

                    // Reflexo principal superior esquerdo
                    float hx = (dx + 0.30f) / 0.24f, hy = (dy - 0.30f) / 0.18f;
                    float spec = Mathf.Clamp01(1f - (hx * hx + hy * hy));
                    core = Color.Lerp(core, Color.white, spec * 0.92f);

                    // Brilhos cintilantes internos
                    float s1 = Mathf.Clamp01(1f - ((dx - 0.22f) * (dx - 0.22f) + (dy + 0.18f) * (dy + 0.18f)) * 45f);
                    float s2 = Mathf.Clamp01(1f - ((dx + 0.05f) * (dx + 0.05f) + (dy - 0.05f) * (dy - 0.05f)) * 60f);
                    core = Color.Lerp(core, Color.white, Mathf.Max(s1, s2));

                    px[y * S + x] = core;
                }
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedBubbleSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return cachedBubbleSprite;
    }

    public static Sprite GetSoftShadowSprite()
    {
        if (cachedShadowSprite != null) return cachedShadowSprite;
        const int W = 128, H = 64;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - 63.5f) / 58f, dy = (y - 31.5f) / 26f;
                float d = dx * dx + dy * dy;
                byte a = d < 1f ? (byte)((1f - d) * (1f - d) * 145f) : (byte)0;
                px[y * W + x] = new Color32(48, 18, 72, a);
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedShadowSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        return cachedShadowSprite;
    }

    public static Sprite GetGlowSprite()
    {
        if (cachedGlowSprite != null) return cachedGlowSprite;
        const int S = 128;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x - 63.5f) / 60f, dy = (y - 63.5f) / 60f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = r < 1f ? (byte)(Mathf.Pow(1f - r, 2.2f) * 210f) : (byte)0;
                px[y * S + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return cachedGlowSprite;
    }

    public static Sprite GetStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;
        const int S = 64;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[S * S];
        Color32 ink = new Color32(58, 22, 86, 255);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                float sec = ((ang % (Mathf.PI * 2f / 5f)) + Mathf.PI * 2f / 5f) % (Mathf.PI * 2f / 5f);
                float rAt = 11f + 16f * (Mathf.Abs(sec - Mathf.PI / 5f) / (Mathf.PI / 5f));
                if (dist <= rAt + 3f) px[y * S + x] = dist <= rAt ? new Color32(255, 255, 255, 255) : ink;
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        return cachedStarSprite;
    }

    public static Sprite GetCaramelPuddleSprite()
    {
        if (cachedSlowPuddleSprite != null) return cachedSlowPuddleSprite;
        const int W = 192, H = 96;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[W * H];
        Color32 ink = new Color32(62, 24, 34, 255);
        Color32 caramel = new Color32(158, 76, 36, 235);
        Color32 hi = new Color32(246, 182, 92, 255);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - 96f) / 82f, dy = (y - 48f) / 36f;
                float ang = Mathf.Atan2(dy, dx);
                float wobble = 0.86f + 0.14f * Mathf.Sin(ang * 5f + 0.4f);
                float r = Mathf.Sqrt(dx * dx + dy * dy) / wobble;
                if (r > 1.06f) continue;
                Color32 c = (r > 0.92f) ? ink : caramel;
                if (r < 0.72f && dy > 0.15f && Mathf.Abs(dx + 0.25f) < 0.35f) c = hi;
                px[y * W + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedSlowPuddleSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        return cachedSlowPuddleSprite;
    }

    public static Sprite GetSpeedBoostMatSprite()
    {
        if (cachedSpeedMatSprite != null) return cachedSpeedMatSprite;
        const int W = 128, H = 160;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[W * H];
        Color32 ink = new Color32(58, 22, 86, 255);
        Color32 pink = new Color32(255, 96, 192, 235);
        Color32 cyan = new Color32(96, 226, 255, 235);
        Color32 gold = new Color32(255, 234, 86, 255);

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (x < 5 || x >= W - 5 || y < 5 || y >= H - 5) { px[y * W + x] = ink; continue; }
                Color32 c = (((x + y) / 18) % 2 == 0) ? pink : cyan;
                int dy = Mathf.Abs(y - H / 2);
                // Chevrons apontando para a DIREITA (+X = turbo!)
                if (Mathf.Abs((x - 42) + dy / 2) < 10 || Mathf.Abs((x - 86) + dy / 2) < 10)
                    c = gold;
                px[y * W + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedSpeedMatSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        return cachedSpeedMatSprite;
    }

    public static Sprite GetDeliveryConeBeaconSprite()
    {
        if (cachedConeBeaconSprite != null) return cachedConeBeaconSprite;
        const int W = 128, H = 160;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[W * H];
        Color32 ink = new Color32(58, 22, 86, 255);
        Color32 gold = new Color32(255, 224, 68, 255);
        Color32 waffle = new Color32(238, 178, 108, 255);
        Color32 pink = new Color32(255, 118, 190, 255);
        Color32 mint = new Color32(138, 234, 186, 255);
        Color32 cherry = new Color32(250, 56, 112, 255);

        void Circle(float cx, float cy, float r, Color32 col)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * W + x] = col;
        }

        // Seta dourada apontando para baixo
        for (int y = 10; y < 48; y++)
        {
            int half = (y - 10) * 28 / 38;
            for (int x = 64 - half - 4; x <= 64 + half + 4; x++)
                if (x >= 0 && x < W) px[y * W + x] = (Mathf.Abs(x - 64) > half) ? ink : gold;
        }
        // Cone de sorvete
        for (int y = 56; y < 104; y++)
        {
            float t = (y - 56) / 48f;
            int half = (int)(5 + t * 24);
            for (int x = 64 - half - 4; x <= 64 + half + 4; x++)
                if (x >= 0 && x < W) px[y * W + x] = (Mathf.Abs(x - 64) > half) ? ink : waffle;
        }
        Circle(48, 108, 20, ink); Circle(48, 108, 16, mint);
        Circle(80, 108, 20, ink); Circle(80, 108, 16, new Color32(194, 146, 255, 255));
        Circle(64, 122, 24, ink); Circle(64, 122, 20, pink);
        Circle(64, 146, 10, ink); Circle(64, 146, 7, cherry);

        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedConeBeaconSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        return cachedConeBeaconSprite;
    }

    public static Sprite GetUiPillSprite()
    {
        if (cachedUiPillSprite != null) return cachedUiPillSprite;
        const int S = 64, R = 20;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float cx = Mathf.Clamp(x, R, S - 1 - R);
                float cy = Mathf.Clamp(y, R, S - 1 - R);
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > R) continue;
                Color c = Color.white;
                if (d > R - 3f) c = new Color(1f, 1f, 1f, 0.96f); // Borda branca brilhante
                else if (y > S * 0.58f) c = new Color(1f, 1f, 1f, 0.98f); // Brilho glossy no topo
                px[y * S + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        cachedUiPillSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        return cachedUiPillSprite;
    }
}
