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
        WriteGirlTexture("Girl_Idle.png", 0);
        WriteGirlTexture("Girl_Walk_A.png", -1);
        WriteGirlTexture("Girl_Walk_B.png", 1);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureSpriteTexture(ArtFolder + "/MatinhosStreet.png", 8);
        ConfigureSpriteTexture(ArtFolder + "/PanelWhite.png", 8);
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

    private static void WriteGroundTexture()
    {
        const int w = 512, h = 256;
        Color32[] px = new Color32[w * h];
        Fill(px, w, h, new Color32(229, 207, 171, 255));

        FillRect(px, w, h, 0, 154, w, 102, new Color32(140, 202, 203, 255));
        FillRect(px, w, h, 0, 151, w, 3, new Color32(108, 176, 183, 255));
        for (int y = 164; y < 254; y += 19)
        {
            int start = (y * 7) % 51;
            for (int x = start; x < w; x += 67)
                FillRect(px, w, h, x, y, 23, 2, new Color32(174, 220, 214, 255));
        }

        FillRect(px, w, h, 0, 130, w, 21, new Color32(231, 207, 168, 255));
        FillRect(px, w, h, 0, 120, w, 10, new Color32(132, 164, 126, 255));
        FillRect(px, w, h, 0, 86, w, 34, new Color32(226, 216, 196, 255));
        FillRect(px, w, h, 0, 80, w, 6, new Color32(190, 151, 125, 255));
        FillRect(px, w, h, 0, 0, w, 80, new Color32(78, 86, 91, 255));

        for (int x = 0; x < w; x += 64)
            FillRect(px, w, h, x, 88, 1, 30, new Color32(205, 196, 179, 170));
        for (int x = 10; x < w; x += 76)
            FillRect(px, w, h, x, 38, 36, 3, new Color32(232, 211, 155, 230));
        FillRect(px, w, h, 0, 1, w, 2, new Color32(66, 73, 80, 255));

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

    private static void CreateStreetGround(Transform parent)
    {
        Sprite street = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/MatinhosStreet.png");
        GameObject ground = new GameObject("Rua contínua — asfalto, meio-fio, calçada, areia e mar");
        ground.transform.SetParent(parent, false);
        ground.transform.position = Vector3.zero;
        SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = street;
        renderer.sortingOrder = -20;

        CreatePalm(parent, -12f, 2.5f);
        CreatePalm(parent, 17f, 2.7f);
        CreatePalm(parent, 25f, 2.3f);
    }

    private static void CreatePalm(Transform parent, float x, float y)
    {
        GameObject trunk = CreateRect(parent, "Coqueiro — tronco", new Vector2(x, y), new Vector2(0.34f, 1.25f), new Color(0.47f, 0.34f, 0.25f), -3, false);
        CreateRect(parent, "Coqueiro — copa", new Vector2(x, y + 0.7f), new Vector2(1.55f, 0.44f), new Color(0.38f, 0.55f, 0.43f), -2, false);
        CreateRect(parent, "Coqueiro — copa", new Vector2(x, y + 0.7f), new Vector2(0.48f, 1.7f), new Color(0.46f, 0.63f, 0.49f), -1, false);
        trunk.transform.position = new Vector3(trunk.transform.position.x, trunk.transform.position.y, 0f);
    }

    private static void CreateLandmark(Transform parent)
    {
        Vector2 center = new Vector2(5f, -1.5f);
        CreateRect(parent, "Quiosque — corpo", center, new Vector2(3.4f, 2.0f), new Color(0.98f, 0.87f, 0.72f), 1, true);
        CreateRect(parent, "Quiosque — balcão", center + new Vector2(0f, -0.45f), new Vector2(3.55f, 0.36f), new Color(0.34f, 0.54f, 0.51f), 2, false);
        CreateRect(parent, "Quiosque — toldo", center + new Vector2(0f, 0.84f), new Vector2(4.1f, 0.62f), new Color(0.91f, 0.36f, 0.62f), 3, false);
        for (int i = -2; i <= 2; i++)
            CreateRect(parent, "Faixa do toldo", center + new Vector2(i * 0.78f, 0.84f), new Vector2(0.36f, 0.62f), new Color(1f, 0.88f, 0.76f), 4, false);
        CreateRect(parent, "Placa Girl Power", center + new Vector2(0f, 1.35f), new Vector2(2.7f, 0.42f), new Color(0.34f, 0.24f, 0.43f), 5, false);

        GameObject sign = new GameObject("Nome do ponto de venda");
        sign.transform.SetParent(parent, false);
        sign.transform.position = new Vector3(center.x, center.y + 1.27f, -0.2f);
        TextMeshPro text = sign.AddComponent<TextMeshPro>();
        text.text = "GIRL POWER";
        text.fontSize = 3.1f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.89f, 0.75f);
        text.rectTransform.sizeDelta = new Vector2(2.7f, 0.45f);
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
