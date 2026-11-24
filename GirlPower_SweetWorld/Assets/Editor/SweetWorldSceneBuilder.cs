using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construtor automático da cena completa "GirlPower_SweetWorld.unity" no novo projeto Unity 2022.3:
/// - Registra Sorting Layers na ordem exata: Sky -> Midground -> Road -> RoadDetails -> Entities -> Foreground -> VFX -> UI
/// - Gera e importa toda a arte cartoon idêntica à capa
/// - Monta a cena 2.5D com 4 camadas de paralaxe, estrada de creme dourado com balas de goma arco-íris,
///   protagonista Girl Power + carrinho Sweet Treats e SweetWorldGameManager configurado!
/// - Também pode ser executado em batchmode via `-executeMethod SweetWorldSceneBuilder.BuildSceneBatch`!
/// </summary>
[InitializeOnLoad]
public static class SweetWorldSceneBuilder
{
    private const string ArtFolder = "Assets/Art/Processed";
    private const string ScenePath = "Assets/Scenes/GirlPower_SweetWorld.unity";

    static SweetWorldSceneBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string absScene = Path.Combine(Application.dataPath, "Scenes/GirlPower_SweetWorld.unity");
            if (!File.Exists(absScene))
            {
                BuildScene();
            }
        };
    }

    [MenuItem("Girl Power Sweet World/Build Scene (Mundo Igual à Capa)")]
    public static void BuildScene()
    {
        EnsureSortingLayers("Sky", "Midground", "Road", "RoadDetails", "Entities", "Foreground", "VFX", "UI");
        SweetWorldCoverArtGenerator.GenerateAndImportAll(ArtFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera (16:9 orthoSize = 5.0 -> altura 10u, largura 17.78u!)
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.0f;
        cam.backgroundColor = new Color32(34, 14, 84, 255);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObj.AddComponent<AudioListener>();

        // 2. Sistema de Paralaxe em 4 Camadas (Céu Galáxia -> Colinas Algodão-Doce -> Estrada Bala de Goma -> Primeiro Plano)
        GameObject worldRoot = new GameObject("SweetWorld_Parallax");
        SweetWorldParallax parallax = worldRoot.AddComponent<SweetWorldParallax>();
        List<SweetWorldParallax.ParallaxBand> bands = new List<SweetWorldParallax.ParallaxBand>();

        Sprite skySp = LoadArt("bg_sky_galaxy.png");
        if (skySp != null)
            bands.Add(CreateParallaxBand(worldRoot.transform, "Layer0_GalaxySky", skySp, 1.55f, 18.6f, "Sky", 0, 0.08f));

        Sprite midSp = LoadArt("bg_candy_midground.png");
        if (midSp != null)
            bands.Add(CreateParallaxBand(worldRoot.transform, "Layer1_CandyHills", midSp, 0.35f, 18.6f, "Midground", 0, 0.30f));

        Sprite roadSp = SweetWorldArtUtility.GetCoverCandyRoadSprite();
        // Salva também a estrada como PNG em Art/Processed para persistir serializada na cena!
        SaveSpriteTextureAsPng(roadSp, ArtFolder + "/candy_road_surface.png", TextureWrapMode.Repeat);
        Sprite persistentRoadSp = LoadArt("candy_road_surface.png") ?? roadSp;
        bands.Add(CreateParallaxBand(worldRoot.transform, "Layer2_CandyRoad", persistentRoadSp, -2.55f, 18.6f, "Road", 0, 1.00f));

        // Estrelas cintilantes no céu galáxia
        Sprite starSp = SweetWorldArtUtility.GetStarSprite();
        SaveSpriteTextureAsPng(starSp, ArtFolder + "/star_sparkle.png", TextureWrapMode.Clamp);
        Sprite persistentStar = LoadArt("star_sparkle.png") ?? starSp;
        List<Transform> stars = new List<Transform>();
        Vector2[] starSpots = { new Vector2(-7.2f, 4.1f), new Vector2(-4.5f, 3.6f), new Vector2(-1.4f, 4.3f), new Vector2(2.3f, 3.8f), new Vector2(5.4f, 4.4f), new Vector2(7.6f, 3.5f) };
        for (int i = 0; i < starSpots.Length; i++)
        {
            GameObject st = new GameObject("TwinkleStar_" + (i + 1));
            st.transform.SetParent(worldRoot.transform, false);
            st.transform.position = new Vector3(starSpots[i].x, starSpots[i].y, 0f);
            st.transform.localScale = Vector3.one * 0.32f;
            SpriteRenderer sr = st.AddComponent<SpriteRenderer>();
            sr.sprite = persistentStar;
            sr.color = new Color(1f, 0.96f, 0.78f, 0.85f);
            sr.sortingLayerName = "Sky";
            sr.sortingOrder = 5;
            stars.Add(st.transform);
        }

        // Primeiro plano suave (pirulito e cupcake passando na borda inferior para profundidade 2.5D!)
        Sprite lolliSp = LoadArt("lollipop_prop.png");
        if (lolliSp != null)
        {
            GameObject fgA = CreateForegroundDecor(worldRoot.transform, "FG_Lollipop_A", lolliSp, new Vector2(-6.5f, -4.65f), 0.78f);
            GameObject fgB = CreateForegroundDecor(worldRoot.transform, "FG_Lollipop_B", lolliSp, new Vector2(12.1f, -4.65f), 0.78f);
            bands.Add(new SweetWorldParallax.ParallaxBand { tileA = fgA.transform, tileB = fgB.transform, tileWidth = 18.6f, speedMultiplier = 1.28f });
        }

        parallax.bands = bands.ToArray();
        parallax.twinklingStars = stars.ToArray();

        // 3. Protagonista Girl Power + Carrinho Sweet Treats Delivery!
        GameObject cartRoot = new GameObject("GirlPower_SweetCart");
        cartRoot.transform.position = new Vector3(-4.4f, -2.55f, 0f);
        Rigidbody2D cartRb = cartRoot.AddComponent<Rigidbody2D>();
        cartRb.isKinematic = true;
        BoxCollider2D cartCol = cartRoot.AddComponent<BoxCollider2D>();
        cartCol.isTrigger = true;
        cartCol.size = new Vector2(2.35f, 1.05f);
        cartCol.offset = new Vector2(0f, -0.35f);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(cartRoot.transform, false);
        visual.transform.localScale = Vector3.one * 0.72f; // Presença marcante na pista de creme dourado!
        SpriteRenderer cartSr = visual.AddComponent<SpriteRenderer>();
        cartSr.sprite = LoadArt("hero_girl_power_cart.png");
        cartSr.sortingLayerName = "Entities";
        cartSr.sortingOrder = 300;

        // Sombra de contato nítida sob as rodas
        Sprite shadowSp = SweetWorldArtUtility.GetSoftShadowSprite();
        SaveSpriteTextureAsPng(shadowSp, ArtFolder + "/soft_shadow.png", TextureWrapMode.Clamp);
        Sprite persistentShadow = LoadArt("soft_shadow.png") ?? shadowSp;
        GameObject shadowObj = new GameObject("CartShadow");
        shadowObj.transform.SetParent(cartRoot.transform, false);
        shadowObj.transform.localPosition = new Vector3(0.05f, -1.18f, 0f);
        shadowObj.transform.localScale = new Vector3(2.45f, 0.75f, 1f);
        SpriteRenderer shSr = shadowObj.AddComponent<SpriteRenderer>();
        shSr.sprite = persistentShadow;
        shSr.sortingLayerName = "RoadDetails";
        shSr.sortingOrder = 10;

        // Aura Girl Power Turbo (invisível até ativar turbo!)
        Sprite glowSp = SweetWorldArtUtility.GetGlowSprite();
        SaveSpriteTextureAsPng(glowSp, ArtFolder + "/magic_glow.png", TextureWrapMode.Clamp);
        Sprite persistentGlow = LoadArt("magic_glow.png") ?? glowSp;
        GameObject auraObj = new GameObject("TurboAura");
        auraObj.transform.SetParent(visual.transform, false);
        auraObj.transform.localPosition = Vector3.zero;
        auraObj.transform.localScale = Vector3.one * 3.8f;
        SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
        auraSr.sprite = persistentGlow;
        auraSr.color = new Color(1f, 0.42f, 0.88f, 0.30f);
        auraSr.sortingLayerName = "VFX";
        auraSr.sortingOrder = 2;
        auraSr.enabled = false;

        GameObject muzzle = new GameObject("BubbleMuzzle");
        muzzle.transform.SetParent(visual.transform, false);
        muzzle.transform.localPosition = new Vector3(2.05f, -0.25f, 0f);

        SweetCartController cartCtrl = cartRoot.AddComponent<SweetCartController>();
        cartCtrl.visualRoot = visual.transform;
        cartCtrl.cartRenderer = cartSr;
        cartCtrl.shadowRenderer = shSr;
        cartCtrl.turboAuraRenderer = auraSr;
        cartCtrl.cannonMuzzle = muzzle.transform;

        // 4. GameManager com todos os sprites da capa conectados!
        GameObject gmObj = new GameObject("SweetWorldGameManager");
        SweetWorldGameManager gm = gmObj.AddComponent<SweetWorldGameManager>();
        gm.cart = cartCtrl;
        gm.enemyGreenSprite = LoadArt("jelly_green.png");
        gm.enemyPinkSprite = LoadArt("jelly_pink.png");
        gm.enemyPurpleSprite = LoadArt("jelly_purple.png");
        gm.cupcakeSprite = LoadArt("cupcake_collectible.png");
        gm.gingerbreadHouseSprite = LoadArt("gingerbread_house.png");
        gm.lollipopSprite = lolliSp;

        // Salva cena e registra nas Build Settings!
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("GirlPower_SweetWorld: Cena construída e salva com sucesso em " + ScenePath);
    }

    public static void BuildSceneBatch()
    {
        BuildScene();
        EditorApplication.Exit(0);
    }

    private static SweetWorldParallax.ParallaxBand CreateParallaxBand(
        Transform parent, string name, Sprite sprite, float posY, float targetWidth, string layer, int order, float speedMul)
    {
        float scaleX = targetWidth / Mathf.Max(0.01f, sprite.bounds.size.x);
        float scaleY = scaleX;
        if (layer == "Road") scaleY = 4.95f / Mathf.Max(0.01f, sprite.bounds.size.y);

        Transform a = MakeBandPiece(parent, name + "_A", sprite, new Vector3(0f, posY, 0f), new Vector3(scaleX, scaleY, 1f), layer, order);
        Transform b = MakeBandPiece(parent, name + "_B", sprite, new Vector3(targetWidth, posY, 0f), new Vector3(scaleX, scaleY, 1f), layer, order);

        return new SweetWorldParallax.ParallaxBand { tileA = a, tileB = b, tileWidth = targetWidth, speedMultiplier = speedMul };
    }

    private static Transform MakeBandPiece(Transform parent, string name, Sprite sp, Vector3 pos, Vector3 scale, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go.transform;
    }

    private static GameObject CreateForegroundDecor(Transform parent, string name, Sprite sp, Vector2 pos, float scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder = 10;
        return go;
    }

    private static void SaveSpriteTextureAsPng(Sprite sp, string assetPath, TextureWrapMode wrap)
    {
        if (sp == null || sp.texture == null) return;
        string abs = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", "").TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllBytes(abs, sp.texture.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = wrap;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    private static Sprite LoadArt(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);

    private static void EnsureSortingLayers(params string[] layers)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tm = new SerializedObject(assets[0]);
        SerializedProperty sp = tm.FindProperty("m_SortingLayers");
        if (sp == null) return;
        while (sp.arraySize > 1) sp.DeleteArrayElementAtIndex(sp.arraySize - 1);
        for (int i = 0; i < layers.Length; i++)
        {
            int idx = sp.arraySize;
            sp.InsertArrayElementAtIndex(idx);
            SerializedProperty e = sp.GetArrayElementAtIndex(idx);
            e.FindPropertyRelative("name").stringValue = layers[i];
            e.FindPropertyRelative("uniqueID").intValue = 2000 + i * 23;
        }
        tm.ApplyModifiedProperties();
    }
}
