using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GirlPowerStreetPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/Girl power 1- Sorvete.unity";
    private const string ArtFolder = "Assets/GirlPower/Art";
    private static Sprite whiteSprite;

    [MenuItem("Girl Power/Build Street Prototype")]
    public static void BuildStreetPrototype()
    {
        EnsureArtAssets();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject mainCameraObject = null;
        GameObject eventSystem = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Main Camera") mainCameraObject = root;
            else if (root.name == "EventSystem") eventSystem = root;
            else if (root.name == "GirlPower_StreetPrototype") Object.DestroyImmediate(root);
            else root.SetActive(false);
        }

        if (mainCameraObject == null) mainCameraObject = new GameObject("Main Camera");
        Camera camera = mainCameraObject.GetComponent<Camera>();
        if (camera == null) camera = mainCameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.55f, 0.78f, 0.79f, 1f);
        camera.transform.position = new Vector3(-1f, -2.2f, -10f);
        foreach (Transform child in mainCameraObject.transform)
            child.gameObject.SetActive(false);
        foreach (MonoBehaviour behaviour in mainCameraObject.GetComponents<MonoBehaviour>())
            behaviour.enabled = false;
        Behaviour brain = mainCameraObject.GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null) brain.enabled = false;

        if (eventSystem != null) eventSystem.SetActive(true);

        GameObject prototype = new GameObject("GirlPower_StreetPrototype");
        CreateStreetGround(prototype.transform);
        CreateLandmark(prototype.transform);
        CreateCartAndPlayer(prototype.transform, camera);
        CreateControlsUI(prototype.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: trecho contínuo de rua criado. Controles: WASD/setas para andar, Shift para correr, E perto do carrinho para abrir.");
    }

    private static void EnsureArtAssets()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GirlPower/Art"));
        WriteGroundTexture();
        WritePanelTexture();
        WriteSunTexture();
        WriteCloudTexture();
        WriteGirlTexture("Girl_Idle.png", 0);
        WriteGirlTexture("Girl_Walk_A.png", -1);
        WriteGirlTexture("Girl_Walk_B.png", 1);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureSpriteTexture(ArtFolder + "/MatinhosStreet.png", 8);
        ConfigureSpriteTexture(ArtFolder + "/PanelWhite.png", 8);
        ConfigureSpriteTexture(ArtFolder + "/Sun.png", 32);
        ConfigureSpriteTexture(ArtFolder + "/Cloud.png", 32);
        ConfigureSpriteTexture(ArtFolder + "/Girl_Idle.png", 32);
        ConfigureSpriteTexture(ArtFolder + "/Girl_Walk_A.png", 32);
        ConfigureSpriteTexture(ArtFolder + "/Girl_Walk_B.png", 32);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/PanelWhite.png");
    }

    private static void WritePanelTexture()
    {
        const int size = 8;
        Color32[] px = new Color32[size * size];
        Fill(px, size, size, new Color32(255, 255, 255, 255));
        SavePng("PanelWhite.png", size, size, px);
    }

    private static void WriteSunTexture()
    {
        const int size = 48;
        Color32[] px = new Color32[size * size];
        Fill(px, size, size, new Color32(0, 0, 0, 0));
        FillEllipse(px, size, size, 3, 3, 42, 42, new Color32(255, 221, 143, 255));
        FillEllipse(px, size, size, 7, 7, 34, 34, new Color32(255, 232, 169, 255));
        SavePng("Sun.png", size, size, px);
    }

    private static void WriteCloudTexture()
    {
        const int w = 96, h = 48;
        Color32[] px = new Color32[w * h];
        Fill(px, w, h, new Color32(0, 0, 0, 0));
        Color32 cloud = new Color32(229, 242, 229, 245);
        FillEllipse(px, w, h, 8, 6, 55, 29, cloud);
        FillEllipse(px, w, h, 26, 18, 38, 24, cloud);
        FillEllipse(px, w, h, 52, 12, 36, 28, cloud);
        FillRect(px, w, h, 19, 7, 60, 18, cloud);
        SavePng("Cloud.png", w, h, px);
    }

    private static void WriteGroundTexture()
    {
        const int w = 512, h = 256;
        Color32[] px = new Color32[w * h];
        Fill(px, w, h, new Color32(145, 207, 210, 255));
        FillRect(px, w, h, 0, 134, w, 14, new Color32(61, 157, 173, 255));
        FillRect(px, w, h, 0, 130, w, 4, new Color32(111, 192, 190, 255));
        FillRect(px, w, h, 0, 127, w, 3, new Color32(226, 218, 177, 255));
        FillRect(px, w, h, 0, 120, w, 7, new Color32(222, 203, 158, 255));
        FillRect(px, w, h, 0, 117, w, 3, new Color32(103, 156, 126, 255));
        FillRect(px, w, h, 0, 92, w, 25, new Color32(219, 211, 190, 255));
        FillRect(px, w, h, 0, 89, w, 3, new Color32(191, 153, 119, 255));
        FillRect(px, w, h, 0, 0, w, 89, new Color32(82, 94, 102, 255));

        // Distant clouds, sea glints, and a low coastal tree line.
        FillRect(px, w, h, 35, 160, 58, 5, new Color32(225, 239, 226, 255));
        FillRect(px, w, h, 45, 155, 42, 5, new Color32(225, 239, 226, 255));
        FillRect(px, w, h, 266, 165, 75, 5, new Color32(229, 240, 226, 255));
        FillRect(px, w, h, 279, 160, 50, 5, new Color32(229, 240, 226, 255));
        FillRect(px, w, h, 0, 146, w, 2, new Color32(119, 189, 194, 255));
        for (int x = 7; x < w; x += 41)
        {
            int y = 137 + (x % 3);
            FillRect(px, w, h, x, y, 20, 1, new Color32(147, 211, 203, 255));
            FillRect(px, w, h, x + 12, y - 4, 13, 1, new Color32(43, 139, 160, 255));
        }
        for (int x = 0; x < w; x += 28)
            FillRect(px, w, h, x, 117, 14, 2, new Color32(94, 148, 118, 255));

        // Subtle paving joints keep the walking lane readable without a blank slab.
        for (int y = 96; y < 117; y += 7)
            FillRect(px, w, h, 0, y, w, 1, new Color32(204, 196, 176, 255));
        for (int y = 93; y < 117; y += 7)
            for (int x = ((y / 7) % 2) * 22; x < w; x += 44)
                FillRect(px, w, h, x, y, 1, 4, new Color32(204, 196, 176, 255));
        FillRect(px, w, h, 0, 90, w, 2, new Color32(233, 218, 187, 255));
        for (int x = 8; x < w; x += 104)
            FillRect(px, w, h, x, 33, 35, 2, new Color32(222, 202, 151, 255));
        FillRect(px, w, h, 0, 1, w, 2, new Color32(67, 77, 84, 255));
        SavePng("MatinhosStreet.png", w, h, px);
    }
    private static void WriteGirlTexture(string fileName, int step)
    {
        const int w = 32, h = 48;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 hair = new Color32(104, 48, 107, 255);
        Color32 hairLight = new Color32(177, 94, 161, 255);
        Color32 skin = new Color32(184, 119, 83, 255);
        Color32 shirt = new Color32(242, 89, 155, 255);
        Color32 shirtLight = new Color32(255, 171, 203, 255);
        Color32 pants = new Color32(64, 93, 126, 255);
        Color32 shoes = new Color32(70, 51, 64, 255);
        Color32[] px = new Color32[w * h];
        Fill(px, w, h, clear);

        FillRect(px, w, h, 9, 2 + step, 5, 5, shoes);
        FillRect(px, w, h, 19, 2 - step, 5, 5, shoes);
        FillRect(px, w, h, 10, 7 + step, 4, 10, pants);
        FillRect(px, w, h, 18, 7 - step, 4, 10, pants);
        FillRect(px, w, h, 8, 16, 16, 13, shirt);
        FillRect(px, w, h, 6, 18, 4, 9, shirtLight);
        FillRect(px, w, h, 22, 18, 4, 9, shirtLight);
        FillRect(px, w, h, 10, 26, 12, 12, skin);
        FillRect(px, w, h, 7, 32, 4, 8, hair);
        FillRect(px, w, h, 21, 32, 4, 8, hair);
        FillRect(px, w, h, 8, 36, 16, 9, hair);
        FillRect(px, w, h, 10, 38, 12, 5, hairLight);
        FillRect(px, w, h, 12, 31, 2, 2, new Color32(56, 44, 53, 255));
        FillRect(px, w, h, 18, 31, 2, 2, new Color32(56, 44, 53, 255));
        FillRect(px, w, h, 14, 20, 4, 4, new Color32(255, 226, 117, 255));

        SavePng(fileName, w, h, px);
    }

    private static void SavePng(string fileName, int width, int height, Color32[] pixels)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        string path = Path.Combine(Application.dataPath, "GirlPower/Art/" + fileName);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void ConfigureSpriteTexture(string assetPath, float ppu)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void Fill(Color32[] pixels, int width, int height, Color32 color)
    {
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
    }

    private static void FillRect(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
    {
        int x0 = Mathf.Clamp(x, 0, width);
        int y0 = Mathf.Clamp(y, 0, height);
        int x1 = Mathf.Clamp(x + w, 0, width);
        int y1 = Mathf.Clamp(y + h, 0, height);
        for (int iy = y0; iy < y1; iy++)
            for (int ix = x0; ix < x1; ix++) pixels[iy * width + ix] = color;
    }

    private static void FillEllipse(Color32[] pixels, int width, int height, int x, int y, int w, int h, Color32 color)
    {
        float rx = Mathf.Max(1f, w * 0.5f);
        float ry = Mathf.Max(1f, h * 0.5f);
        float cx = x + rx;
        float cy = y + ry;
        int x0 = Mathf.Clamp(x, 0, width);
        int y0 = Mathf.Clamp(y, 0, height);
        int x1 = Mathf.Clamp(x + w, 0, width);
        int y1 = Mathf.Clamp(y + h, 0, height);
        for (int iy = y0; iy < y1; iy++)
            for (int ix = x0; ix < x1; ix++)
            {
                float dx = (ix + 0.5f - cx) / rx;
                float dy = (iy + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    pixels[iy * width + ix] = color;
            }
    }

    private static void CreateStreetGround(Transform parent)
    {
        Sprite street = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/MatinhosStreet.png");
        GameObject ground = new GameObject("Rua contínua — asfalto, meio-fio, calçada, areia e mar");
        ground.transform.SetParent(parent, false);
        ground.transform.position = Vector3.zero;
        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = street;
        renderer.sortingOrder = -20;

        Sprite sun = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Sun.png");
        Sprite cloud = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Cloud.png");
        CreateBackdropSprite(parent, "Sol de fim de tarde", sun, new Vector2(-8.5f, 2.7f), 0.85f, -16);
        CreateBackdropSprite(parent, "Nuvem costeira esquerda", cloud, new Vector2(-6.2f, 3.25f), 0.68f, -15);
        CreateBackdropSprite(parent, "Nuvem costeira direita", cloud, new Vector2(3.0f, 3.1f), 0.52f, -15);

        CreatePalm(parent, -8.8f, 1.7f);
        CreatePalm(parent, 10.2f, 1.8f);
        CreatePalm(parent, 23f, 2.3f);
    }

    private static void CreateBackdropSprite(Transform parent, string objectName, Sprite sprite, Vector2 position, float scale, int order)
    {
        if (sprite == null) return;
        GameObject item = new GameObject(objectName);
        item.transform.SetParent(parent, false);
        item.transform.position = new Vector3(position.x, position.y, 0.1f);
        item.transform.localScale = Vector3.one * scale;
        SpriteRenderer spriteRenderer = item.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = order;
    }

    private static void CreatePalm(Transform parent, float x, float y)
    {
        GameObject trunk = CreateRect(parent, "Coqueiro — tronco", new Vector2(x, y), new Vector2(0.34f, 1.25f), new Color(0.47f, 0.34f, 0.25f), -3, false);
        GameObject frondA = CreateRect(parent, "Coqueiro - folha", new Vector2(x - 0.42f, y + 1.18f), new Vector2(1.18f, 0.22f), new Color(0.38f, 0.55f, 0.43f), -2, false);
        frondA.transform.rotation = Quaternion.Euler(0f, 0f, 28f);
        GameObject frondB = CreateRect(parent, "Coqueiro - folha", new Vector2(x + 0.42f, y + 1.18f), new Vector2(1.18f, 0.22f), new Color(0.38f, 0.55f, 0.43f), -2, false);
        frondB.transform.rotation = Quaternion.Euler(0f, 0f, -28f);
        GameObject frondC = CreateRect(parent, "Coqueiro - folha", new Vector2(x, y + 1.5f), new Vector2(0.22f, 1.12f), new Color(0.46f, 0.63f, 0.49f), -1, false);
        trunk.transform.position = new Vector3(trunk.transform.position.x, trunk.transform.position.y, 0f);
    }

    private static void CreateLandmark(Transform parent)
    {
        Vector2 center = new Vector2(5f, -0.45f);
        // Low open-sided pavilion: the sidewalk remains clear and the counter faces the player.
        CreateRect(parent, "Kiosque roof beam", center + new Vector2(0f, 0.92f), new Vector2(3.8f, 0.20f), new Color(0.48f, 0.30f, 0.23f), 1, false);
        CreateRect(parent, "Kiosque awning", center + new Vector2(0f, 0.72f), new Vector2(4.1f, 0.48f), new Color(0.88f, 0.31f, 0.56f), 2, false);
        for (int i = -2; i <= 2; i++)
            CreateRect(parent, "Awning stripe", center + new Vector2(i * 0.78f, 0.72f), new Vector2(0.34f, 0.48f), new Color(1f, 0.88f, 0.74f), 3, false);
        CreateRect(parent, "Kiosque left post", center + new Vector2(-1.55f, -0.12f), new Vector2(0.12f, 1.55f), new Color(0.55f, 0.34f, 0.25f), 1, false);
        CreateRect(parent, "Kiosque right post", center + new Vector2(1.55f, -0.12f), new Vector2(0.12f, 1.55f), new Color(0.55f, 0.34f, 0.25f), 1, false);
        CreateRect(parent, "Kiosque counter", center + new Vector2(0f, -0.78f), new Vector2(3.25f, 0.32f), new Color(0.30f, 0.50f, 0.47f), 4, false);
        CreateRect(parent, "Kiosque counter front", center + new Vector2(0f, -1.00f), new Vector2(3.05f, 0.16f), new Color(0.96f, 0.77f, 0.56f), 3, false);
        CreateRect(parent, "Girl Power signboard", center + new Vector2(0f, 1.22f), new Vector2(2.5f, 0.34f), new Color(0.34f, 0.24f, 0.43f), 5, false);

        GameObject sign = new GameObject("Sales point name");
        sign.transform.SetParent(parent, false);
        sign.transform.position = new Vector3(center.x, center.y + 1.14f, -0.2f);
        TextMeshPro text = sign.AddComponent<TextMeshPro>();
        text.text = "GIRL POWER";
        text.fontSize = 2.35f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.89f, 0.75f);
        text.rectTransform.sizeDelta = new Vector2(2.5f, 0.34f);
        MeshRenderer signRenderer = sign.GetComponent<MeshRenderer>();
        if (signRenderer != null) signRenderer.sortingOrder = 6;
    }
    private static void CreateCartAndPlayer(Transform parent, Camera camera)
    {
        Sprite cartSprite = Resources.Load<Sprite>("icecreamvan");
        GameObject cart = new GameObject("Carrinho de sorvete — ponto inicial");
        cart.transform.SetParent(parent, false);
        cart.transform.position = new Vector3(1.6f, -2.45f, -0.1f);
        cart.transform.localScale = Vector3.one * 0.82f;
        SpriteRenderer cartRenderer = cart.AddComponent<SpriteRenderer>();
        cartRenderer.sprite = cartSprite;
        cartRenderer.sortingOrder = 4;
        BoxCollider2D cartCollider = cart.AddComponent<BoxCollider2D>();
        cartCollider.size = new Vector2(2.2f, 1.55f);
        cartCollider.offset = new Vector2(0f, -0.15f);

        GameObject player = new GameObject("Protagonista — exploração a pé");
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(-0.3f, -2.35f, -0.2f);
        SpriteRenderer avatar = player.AddComponent<SpriteRenderer>();
        avatar.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Girl_Idle.png");
        avatar.sortingOrder = 8;
        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.size = new Vector2(0.62f, 0.75f);
        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        GirlPowerStreetController controller = player.AddComponent<GirlPowerStreetController>();
        controller.avatarRenderer = avatar;
        controller.idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Girl_Idle.png");
        controller.walkSpriteA = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Girl_Walk_A.png");
        controller.walkSpriteB = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/Girl_Walk_B.png");
        controller.cart = cart.transform;
        controller.gameplayCamera = camera;
    }

    private static void CreateControlsUI(Transform parent)
    {
        GameObject canvasObject = new GameObject("Exploração — controles essenciais", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject pill = new GameObject("Faixa de controles", typeof(RectTransform), typeof(Image));
        pill.transform.SetParent(canvasObject.transform, false);
        RectTransform pillRect = pill.GetComponent<RectTransform>();
        pillRect.anchorMin = new Vector2(0.03f, 0.035f);
        pillRect.anchorMax = new Vector2(0.34f, 0.12f);
        pillRect.offsetMin = Vector2.zero;
        pillRect.offsetMax = Vector2.zero;
        pill.GetComponent<Image>().color = new Color(0.25f, 0.18f, 0.33f, 0.82f);

        TextMeshProUGUI controls = CreateUiText(pill.transform, "Instruções de exploração", new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 28f, "WASD / SETAS  ANDAR     SHIFT  CORRER");

        GameObject promptObject = new GameObject("Aviso de interação com o carrinho", typeof(RectTransform));
        promptObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI prompt = promptObject.AddComponent<TextMeshProUGUI>();
        prompt.rectTransform.anchorMin = new Vector2(0.34f, 0.14f);
        prompt.rectTransform.anchorMax = new Vector2(0.66f, 0.21f);
        prompt.rectTransform.offsetMin = Vector2.zero;
        prompt.rectTransform.offsetMax = Vector2.zero;
        prompt.fontSize = 30f;
        prompt.fontStyle = FontStyles.Bold;
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.color = new Color(0.31f, 0.22f, 0.39f, 1f);

        GirlPowerStreetController controller = Object.FindObjectOfType<GirlPowerStreetController>();
        if (controller != null)
        {
            controller.controlsLabel = controls;
            controller.cartPrompt = prompt;
        }
    }

    private static TextMeshProUGUI CreateUiText(Transform parent, string objectName, Vector2 min, Vector2 max, float size, string content)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = min;
        text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = content;
        return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order, bool collider)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = whiteSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (collider)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
        }
        return go;
    }
}

