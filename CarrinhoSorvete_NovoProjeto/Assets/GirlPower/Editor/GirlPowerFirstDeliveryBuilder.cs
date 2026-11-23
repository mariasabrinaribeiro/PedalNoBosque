using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the first delivery only in its copied scene; source and reference scenes remain untouched.</summary>
public static class GirlPowerFirstDeliveryBuilder
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Primeira Entrega.unity";
    private const string ArtPath = "Assets/GirlPower/Art/";
    private const string BackdropPath = ArtPath + "MatinhosEveningPixelArt.png";
    private const int ArtWidth = 1200;
    private const int ArtHeight = 256;
    private static Sprite pixel;

    [MenuItem("Girl Power/Build First Delivery (working copy)")]
    public static void Build()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "GirlPower/Art/PanelWhite.png")))
        {
            Debug.LogError("PanelWhite.png is missing; source scenes and artwork were not changed.");
            return;
        }

        CreateMatinhosArtwork();
        AssetDatabase.Refresh();
        ConfigureBackdropImporter();
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "PanelWhite.png");
        Sprite backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
        if (pixel == null || backdrop == null)
        {
            Debug.LogError("Could not load the pixel art for the working scene.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects()) root.SetActive(false);

        GameObject world = new GameObject("Primeira Entrega - mundo de jogo");
        CreateCamera(world.transform);
        CreateBackdrop(world.transform, backdrop);
        CreateRoutes(world.transform);
        CreateCoastalDetails(world.transform);
        CreateShopfront(world.transform);
        CreateFlorist(world.transform);
        CreateCharacters(world.transform);
        CreateMission(world.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: cena de trabalho montada e salva; missão sem cronômetro, rotas da orla e feira, pagamento único e placa instalável.");
    }

    private static void CreateMatinhosArtwork()
    {
        string fullPath = Path.Combine(Application.dataPath, "GirlPower/Art/MatinhosEveningPixelArt.png");
        Color32[] pixels = new Color32[ArtWidth * ArtHeight];
        Color32 skyTop = new Color32(80, 79, 119, 255);
        Color32 skyHorizon = new Color32(244, 139, 126, 255);
        Color32 seaTop = new Color32(52, 150, 169, 255);
        Color32 seaBottom = new Color32(47, 111, 145, 255);
        Color32 sandTop = new Color32(239, 199, 145, 255);
        Color32 sandBottom = new Color32(214, 163, 113, 255);

        for (int y = 0; y < ArtHeight; y++)
        {
            Color32 row;
            if (y >= 180) row = Blend(skyHorizon, skyTop, Mathf.InverseLerp(180, ArtHeight, y));
            else if (y >= 140) row = Blend(new Color32(57, 143, 164, 255), seaTop, Mathf.InverseLerp(140, 180, y));
            else if (y >= 102) row = Blend(sandBottom, sandTop, Mathf.InverseLerp(102, 140, y));
            else if (y >= 74) row = Blend(new Color32(75, 117, 83, 255), new Color32(117, 151, 94, 255), Mathf.InverseLerp(74, 102, y));
            else row = Blend(new Color32(77, 82, 94, 255), new Color32(124, 116, 111, 255), Mathf.InverseLerp(0, 74, y));
            for (int x = 0; x < ArtWidth; x++) pixels[y * ArtWidth + x] = row;
        }

        // Warm sky, stepped clouds and a low sun over the ocean.
        DrawPixelRect(pixels, 54, 222, 106, 7, new Color32(255, 194, 164, 255));
        DrawPixelRect(pixels, 72, 229, 68, 5, new Color32(255, 194, 164, 255));
        DrawPixelRect(pixels, 307, 211, 126, 8, new Color32(250, 172, 154, 255));
        DrawPixelRect(pixels, 330, 219, 74, 5, new Color32(250, 172, 154, 255));
        DrawPixelRect(pixels, 970, 232, 130, 7, new Color32(255, 193, 159, 255));
        DrawPixelRect(pixels, 990, 239, 78, 5, new Color32(255, 193, 159, 255));
        DrawSteppedCircle(pixels, 828, 183, 22, new Color32(255, 191, 117, 255));
        DrawSteppedCircle(pixels, 828, 183, 15, new Color32(255, 221, 151, 255));
        DrawPixelRect(pixels, 0, 177, ArtWidth, 3, new Color32(255, 199, 139, 255));

        // Distant Matinhos skyline and low-rise beachfront buildings.
        for (int i = 0; i < 22; i++)
        {
            int x = i * 59 + 11;
            int w = 25 + (i * 13 % 23);
            int h = 10 + (i * 17 % 30);
            Color32 building = i % 3 == 0 ? new Color32(132, 110, 117, 255) : new Color32(171, 121, 121, 255);
            DrawPixelRect(pixels, x, 178, w, h, building);
            for (int wy = 181; wy < 178 + h - 2; wy += 8)
                for (int wx = x + 4; wx < x + w - 3; wx += 7)
                    DrawPixelRect(pixels, wx, wy, 3, 3, new Color32(255, 211, 151, 255));
        }

        // Small wave bands, foam and beach texture.
        for (int x = 0; x < ArtWidth; x += 47)
        {
            DrawPixelRect(pixels, x + (x % 3) * 4, 151 + (x % 7), 18 + x % 13, 2, new Color32(112, 196, 197, 255));
            DrawPixelRect(pixels, x + 15, 144 + (x % 5), 9, 2, new Color32(80, 173, 186, 255));
            DrawPixelRect(pixels, x + 5, 128 + (x % 11), 8, 2, new Color32(252, 220, 172, 255));
            DrawPixelRect(pixels, x + 27, 111 + (x % 9), 11, 2, new Color32(241, 207, 160, 255));
        }

        // Beach umbrellas, palms and planted edge; all details share a compact pixel grid.
        DrawUmbrella(pixels, 185, 104, new Color32(213, 93, 105, 255));
        DrawUmbrella(pixels, 592, 113, new Color32(231, 179, 99, 255));
        DrawUmbrella(pixels, 1032, 101, new Color32(201, 103, 119, 255));
        DrawPalm(pixels, 73, 79, 59);
        DrawPalm(pixels, 467, 78, 66);
        DrawPalm(pixels, 902, 80, 57);
        DrawPalm(pixels, 1150, 75, 63);
        for (int x = 0; x < ArtWidth; x += 31)
        {
            int h = 4 + (x * 7 % 7);
            DrawPixelRect(pixels, x, 74, 14, h, x % 2 == 0 ? new Color32(71, 119, 79, 255) : new Color32(88, 135, 86, 255));
        }

        // A banded promenade below the playable route adds continuity beyond the camera frame.
        DrawPixelRect(pixels, 0, 48, ArtWidth, 2, new Color32(215, 175, 129, 255));
        for (int x = 0; x < ArtWidth; x += 80)
        {
            DrawPixelRect(pixels, x + 10, 30, 35, 2, new Color32(150, 145, 141, 255));
            DrawPixelRect(pixels, x + 53, 30, 16, 2, new Color32(150, 145, 141, 255));
            DrawPixelRect(pixels, x + 22, 58, 2, 9, new Color32(219, 192, 156, 255));
            DrawPixelRect(pixels, x + 42, 62, 2, 5, new Color32(230, 204, 168, 255));
        }

        Texture2D texture = new Texture2D(ArtWidth, ArtHeight, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void ConfigureBackdropImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    private static Color32 Blend(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), 255);
    }

    private static void DrawPixelRect(Color32[] pixels, int x, int y, int width, int height, Color32 color)
    {
        int x0 = Mathf.Clamp(x, 0, ArtWidth);
        int y0 = Mathf.Clamp(y, 0, ArtHeight);
        int x1 = Mathf.Clamp(x + width, 0, ArtWidth);
        int y1 = Mathf.Clamp(y + height, 0, ArtHeight);
        for (int py = y0; py < y1; py++)
            for (int px = x0; px < x1; px++) pixels[py * ArtWidth + px] = color;
    }

    private static void DrawSteppedCircle(Color32[] pixels, int cx, int cy, int radius, Color32 color)
    {
        for (int y = -radius; y <= radius; y += 3)
            for (int x = -radius; x <= radius; x += 3)
                if (x * x + y * y <= radius * radius) DrawPixelRect(pixels, cx + x, cy + y, 3, 3, color);
    }

    private static void DrawPalm(Color32[] pixels, int x, int y, int height)
    {
        DrawPixelRect(pixels, x, y, 5, height, new Color32(116, 84, 64, 255));
        DrawPixelRect(pixels, x + 5, y + height - 7, 5, 4, new Color32(116, 84, 64, 255));
        Color32 leaf = new Color32(59, 104, 78, 255);
        DrawPixelRect(pixels, x - 21, y + height - 4, 24, 4, leaf);
        DrawPixelRect(pixels, x - 17, y + height + 1, 18, 4, leaf);
        DrawPixelRect(pixels, x + 4, y + height + 1, 23, 4, leaf);
        DrawPixelRect(pixels, x + 7, y + height - 5, 20, 4, leaf);
        DrawPixelRect(pixels, x - 11, y + height + 4, 15, 3, new Color32(77, 126, 87, 255));
    }

    private static void DrawUmbrella(Color32[] pixels, int x, int y, Color32 color)
    {
        DrawPixelRect(pixels, x - 16, y, 32, 4, color);
        DrawPixelRect(pixels, x - 12, y + 4, 24, 4, color);
        DrawPixelRect(pixels, x - 6, y + 8, 12, 18, new Color32(112, 83, 67, 255));
        DrawPixelRect(pixels, x - 23, y - 3, 8, 3, new Color32(245, 222, 181, 255));
        DrawPixelRect(pixels, x + 15, y - 3, 8, 3, new Color32(245, 222, 181, 255));
    }

    private static void CreateCamera(Transform root)
    {
        GameObject cameraObject = new GameObject("Delivery Camera");
        cameraObject.transform.SetParent(root, false);
        cameraObject.transform.position = new Vector3(-0.2f, 0.15f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color32(239, 166, 147, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateBackdrop(Transform root, Sprite backdrop)
    {
        GameObject back = new GameObject("Arte de fundo - orla de Matinhos ao entardecer");
        back.transform.SetParent(root, false);
        back.transform.position = new Vector3(12f, 0.15f, 0.9f);
        back.transform.localScale = new Vector3(4.2f, 4.2f, 1f);
        SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
        sr.sprite = backdrop;
        sr.sortingOrder = -30;

        CreateRect(root, "Calçada contínua do passeio", new Vector2(12f, -3.05f), new Vector2(50f, 0.88f), new Color32(198, 188, 166, 255), -14, false, false);
        CreateRect(root, "Meio-fio claro do passeio", new Vector2(12f, -2.58f), new Vector2(50f, 0.12f), new Color32(239, 225, 198, 255), -13, false, false);
        CreateRect(root, "Rua costeira ao fundo", new Vector2(12f, -4.35f), new Vector2(50f, 1.45f), new Color32(92, 101, 111, 255), -16, false, false);
    }

    private static void CreateRoutes(Transform root)
    {
        CreateRect(root, "Rota ampla - calçada contínua da orla", new Vector2(8f, -1.65f), new Vector2(38f, 1.75f), new Color32(203, 194, 173, 255), -10, false, false);
        CreateRect(root, "Atalho - travessa da feira", new Vector2(18f, 1.1f), new Vector2(22f, 1.3f), new Color32(187, 159, 130, 255), -9, false, false);
        CreateRect(root, "Bifurcação - ligação entre os caminhos", new Vector2(6.8f, -0.2f), new Vector2(4.2f, 3.45f), new Color32(211, 199, 175, 255), -11, false, false);
        for (int i = 0; i < 21; i++)
        {
            float x = -10.5f + i * 1.8f;
            CreateRect(root, "Junta da calçada da orla", new Vector2(x, -1.65f), new Vector2(0.035f, 1.67f), new Color32(176, 167, 147, 255), -8, false, false);
            if (x >= 7f) CreateRect(root, "Junta da travessa da feira", new Vector2(x, 1.1f), new Vector2(0.035f, 1.22f), new Color32(151, 125, 102, 255), -7, false, false);
        }
        for (int i = 0; i < 13; i++)
        {
            float x = -9f + i * 2.8f;
            CreateRect(root, "Junta transversal da calçada", new Vector2(x, -1.65f), new Vector2(1.62f, 0.025f), new Color32(188, 180, 161, 255), -8, false, false);
            if (x >= 7f) CreateRect(root, "Junta transversal da feira", new Vector2(x, 1.1f), new Vector2(1.1f, 0.025f), new Color32(164, 139, 117, 255), -6, false, false);
        }
        CreateRouteMarker(root, "ORLA  →  CAMINHO AMPLO", new Vector2(4.8f, -2.24f), new Color32(67, 117, 125, 255));
        CreateRouteMarker(root, "FEIRA  →  ATALHO COM MOVIMENTO", new Vector2(6.7f, 1.98f), new Color32(133, 87, 74, 255));

        CreateObstacle(root, "Caixa de laranjas", new Vector2(10.2f, 1.05f), new Vector2(0.78f, 0.72f), new Color32(185, 111, 58, 255));
        CreateObstacle(root, "Caixa de flores", new Vector2(14.2f, 0.76f), new Vector2(0.85f, 0.84f), new Color32(151, 100, 69, 255));
        CreateObstacle(root, "Pedestre azul", new Vector2(17.2f, 1.34f), new Vector2(0.54f, 0.76f), new Color32(80, 119, 155, 255));
        CreateObstacle(root, "Pedestre coral", new Vector2(19.6f, 0.88f), new Vector2(0.54f, 0.76f), new Color32(204, 107, 104, 255));

        for (int i = 0; i < 5; i++)
        {
            float x = 4f + i * 4.7f;
            CreateRect(root, "Banco baixo da orla", new Vector2(x, -3.05f), new Vector2(0.9f, 0.17f), new Color32(131, 87, 67, 255), -1, false, false);
            CreateRect(root, "Sombra do banco", new Vector2(x, -3.17f), new Vector2(1.15f, 0.08f), new Color32(111, 105, 99, 120), -2, false, false);
        }
    }

    private static void CreateCoastalDetails(Transform root)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = -2f + i * 4.7f;
            CreateRect(root, "Poste de luz - haste", new Vector2(x, 2.05f), new Vector2(0.08f, 1.75f), new Color32(93, 83, 84, 255), 0, false, false);
            CreateRect(root, "Poste de luz - braço", new Vector2(x + 0.22f, 2.84f), new Vector2(0.55f, 0.08f), new Color32(93, 83, 84, 255), 0, false, false);
            CreateRect(root, "Poste de luz - luminária", new Vector2(x + 0.48f, 2.73f), new Vector2(0.22f, 0.17f), new Color32(255, 218, 151, 255), 1, false, false);
        }
        CreateRect(root, "Borda do jardim costeiro", new Vector2(12f, 0.05f), new Vector2(50f, 0.17f), new Color32(129, 154, 117, 255), -12, false, false);
    }

    private static void CreateShopfront(Transform root)
    {
        CreateRect(root, "Quiosque baixo - balcão aberto", new Vector2(0f, 2.42f), new Vector2(3.5f, 0.5f), new Color32(113, 124, 110, 255), 2, false, false);
        CreateRect(root, "Quiosque - painel de madeira", new Vector2(0f, 2.82f), new Vector2(3.1f, 0.53f), new Color32(149, 103, 76, 255), 3, false, false);
        CreateRect(root, "Quiosque - toldo baixo", new Vector2(0f, 3.38f), new Vector2(4.1f, 0.42f), new Color32(191, 89, 125, 255), 5, false, false);
        for (int i = 0; i < 7; i++)
            CreateRect(root, "Toldo - faixa de tecido", new Vector2(-1.75f + i * 0.58f, 3.38f), new Vector2(0.13f, 0.40f), new Color32(255, 229, 193, 255), 6, false, false);
        CreateRect(root, "Placa base GIRL POWER", new Vector2(0f, 2.91f), new Vector2(2.2f, 0.34f), new Color32(80, 57, 78, 255), 7, false, false);
        MakeText(root, "Nome da sorveteria", new Vector2(0f, 2.91f), "GIRL POWER", 3.2f, new Color32(255, 238, 213, 255), 8);
        CreateRect(root, "Quiosque - poste esquerdo", new Vector2(-1.55f, 2.2f), new Vector2(0.11f, 1.2f), new Color32(116, 76, 60, 255), 1, false, false);
        CreateRect(root, "Quiosque - poste direito", new Vector2(1.55f, 2.2f), new Vector2(0.11f, 1.2f), new Color32(116, 76, 60, 255), 1, false, false);

        GameObject sign = CreateRect(root, "Placa nova - fica guardada até ser instalada", new Vector2(0f, 1.96f), new Vector2(2.8f, 0.42f), new Color32(72, 53, 77, 255), 10, false, false);
        sign.SetActive(false);
        TextMeshPro signText = MakeText(sign.transform, "Nome instalado", Vector2.zero, "", 2.7f, new Color32(255, 235, 203, 255), 11);
        signText.rectTransform.sizeDelta = new Vector2(2.8f, 0.42f);
        signText.transform.localScale = new Vector3(1f / 2.8f, 2.38f, 1f);
    }

    private static void CreateFlorist(Transform root)
    {
        CreateRect(root, "Banca da florista - balcão", new Vector2(24f, 2.32f), new Vector2(3.3f, 0.46f), new Color32(99, 116, 87, 255), 2, false, false);
        CreateRect(root, "Banca da florista - toldo", new Vector2(24f, 3.06f), new Vector2(3.8f, 0.38f), new Color32(218, 135, 117, 255), 3, false, false);
        for (int i = 0; i < 5; i++)
        {
            Color32 flower = i % 2 == 0 ? new Color32(237, 118, 159, 255) : new Color32(247, 198, 101, 255);
            CreateRect(root, "Vaso da florista", new Vector2(22.8f + i * 0.58f, 2.61f), new Vector2(0.32f, 0.28f), flower, 5, false, false);
            CreateRect(root, "Folhas do vaso", new Vector2(22.8f + i * 0.58f, 2.78f), new Vector2(0.18f, 0.12f), new Color32(99, 145, 94, 255), 6, false, false);
        }
        MakeText(root, "Identificação da florista", new Vector2(24f, 3.53f), "FLORISTA  •  FLORES DA PRAÇA", 2.7f, new Color32(255, 245, 219, 255), 5);
    }

    private static void CreateCharacters(Transform root)
    {
        Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Idle.png");
        Sprite walkA = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_A.png");
        Sprite walkB = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_B.png");

        GameObject cart = new GameObject("Carrinho de sorvete - posição e colisão próprias");
        cart.transform.SetParent(root, false);
        cart.transform.localPosition = new Vector3(0.8f, -1.78f, 0f);
        cart.transform.localScale = Vector3.one * 0.84f;
        CreateRect(cart.transform, "Carrinho - rodas", new Vector2(0f, -0.43f), new Vector2(1.66f, 0.24f), new Color32(72, 57, 65, 255), 3, false, false);
        CreateRect(cart.transform, "Carrinho - corpo turquesa", new Vector2(0f, 0f), new Vector2(1.92f, 0.78f), new Color32(87, 179, 168, 255), 4, false, false);
        CreateRect(cart.transform, "Carrinho - frente coral", new Vector2(0f, -0.19f), new Vector2(1.82f, 0.24f), new Color32(222, 104, 139, 255), 5, false, false);
        CreateRect(cart.transform, "Carrinho - tampo creme", new Vector2(0f, 0.44f), new Vector2(2.13f, 0.22f), new Color32(255, 228, 184, 255), 6, false, false);
        CreateRect(cart.transform, "Carrinho - sorvete casquinha", new Vector2(0.02f, 0.79f), new Vector2(0.25f, 0.3f), new Color32(196, 139, 77, 255), 7, false, false);
        CreateRect(cart.transform, "Carrinho - sorvete morango", new Vector2(0.02f, 0.99f), new Vector2(0.4f, 0.22f), new Color32(239, 120, 161, 255), 8, false, false);
        BoxCollider2D cartCollider = cart.AddComponent<BoxCollider2D>();
        cartCollider.size = new Vector2(1.8f, 1.15f);
        cartCollider.isTrigger = true;

        GameObject player = new GameObject("Protagonista - personagem independente");
        player.transform.SetParent(root, false);
        player.transform.localPosition = new Vector3(-1.25f, -1.68f, 0f);
        player.transform.localScale = Vector3.one * 0.86f;
        SpriteRenderer avatar = player.AddComponent<SpriteRenderer>();
        avatar.sprite = idle;
        avatar.sortingOrder = 8;
        CircleCollider2D body = player.AddComponent<CircleCollider2D>();
        body.radius = 0.24f;

        GameObject florist = new GameObject("Florista - cliente da primeira entrega");
        florist.transform.SetParent(root, false);
        florist.transform.localPosition = new Vector3(24f, -0.15f, 0f);
        florist.transform.localScale = Vector3.one * 0.72f;
        SpriteRenderer npc = florist.AddComponent<SpriteRenderer>();
        npc.sprite = idle;
        npc.color = new Color32(158, 102, 82, 255);
        npc.sortingOrder = 7;
        CapsuleCollider2D npcCollider = florist.AddComponent<CapsuleCollider2D>();
        npcCollider.size = new Vector2(0.65f, 0.8f);
        npcCollider.isTrigger = true;
    }

    private static void CreateMission(Transform root)
    {
        GameObject missionObject = new GameObject("First Delivery Mission - no timer");
        missionObject.transform.SetParent(root, false);
        GirlPowerFirstDelivery mission = missionObject.AddComponent<GirlPowerFirstDelivery>();
        mission.player = root.Find("Protagonista - personagem independente");
        mission.cart = root.Find("Carrinho de sorvete - posição e colisão próprias");
        mission.florist = root.Find("Florista - cliente da primeira entrega");
        mission.sign = root.Find("Placa nova - fica guardada até ser instalada");
        mission.sceneCamera = root.Find("Delivery Camera").GetComponent<Camera>();
        mission.playerSprite = mission.player.GetComponent<SpriteRenderer>();
        mission.idle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Idle.png");
        mission.walkA = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_A.png");
        mission.walkB = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_B.png");
        mission.signLabel = mission.sign.GetComponentInChildren<TextMeshPro>(true);
        mission.obstacles = Object.FindObjectsOfType<Collider2D>();
    }

    private static void CreateObstacle(Transform root, string name, Vector2 position, Vector2 size, Color32 color)
    {
        GameObject obstacle = CreateRect(root, name, position, size, color, 5, true, true);
        CreateRect(root, name + " - brilho", position + new Vector2(-size.x * 0.22f, size.y * 0.20f), new Vector2(size.x * 0.22f, 0.06f), new Color32(245, 195, 126, 255), 6, false, false);
        if (name.Contains("Pedestre"))
        {
            CreateRect(root, name + " - cabeça", position + new Vector2(0f, size.y * 0.58f), new Vector2(0.38f, 0.36f), new Color32(195, 145, 113, 255), 7, false, false);
            CreateRect(root, name + " - sombra", position + new Vector2(0f, -size.y * 0.6f), new Vector2(0.65f, 0.12f), new Color32(110, 99, 86, 150), 1, false, false);
        }
        obstacle.name = name;
    }

    private static void CreateRouteMarker(Transform root, string label, Vector2 position, Color32 color)
    {
        CreateRect(root, "Sinalização da bifurcação", position, new Vector2(4.9f, 0.28f), color, 0, false, false);
        MakeText(root, "Direção da rota", position, label, 2.4f, new Color32(255, 245, 223, 255), 1);
    }

    private static TextMeshPro MakeText(Transform parent, string name, Vector2 position, string content, float fontSize, Color color, int order)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.rectTransform.sizeDelta = new Vector2(5.5f, 0.5f);
        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = order;
        return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size, Color32 color, int order, bool collider, bool block)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (collider)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.isTrigger = !block;
        }
        return go;
    }
}
