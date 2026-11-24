using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construtor revisado e limpo da cena "Avesso das Horas - Corrida da Meia-Noite.unity":
/// - ZERO texturas embutidas em memória: usa 100% PNGs persistentes de Assets/Art/Processed!
/// - Sorting Layers unificadas com GirlPower_SweetWorld:
///   Sky -> ElevatedTrack -> Midground -> Road -> RoadDetails -> Entities -> Foreground -> VFX -> UI
///   (Pista Elevada Estrelada passa atrás dos telhados das casas, ganhando profundidade real!)
/// - Pirulito Gigante articulado corretamente: haste listrada fixa no pivô da base + cabeça espiral
///   que gira isolada no topo (nunca mais gira a haste como hélice!) + colisores reais!
/// - Personagem Suri ilustrada em pé (suri_character.png) e emenda zero nos tiles da pista (+0.08f overlap)!
/// </summary>
public static class AvessoSceneBuilder
{
    private const string ArtFolder = "Assets/Art/Processed";
    private const string SweetScenePath = "Assets/Scenes/GirlPower_SweetWorld.unity";
    private const string AvessoScenePath = "Assets/Scenes/Avesso das Horas - Corrida da Meia-Noite.unity";

    [MenuItem("O Avesso das Horas/Criar Cena (Corrida da Meia-Noite)")]
    public static void BuildAvessoScene()
    {
        EnsureUnifiedSortingLayers();
        ReimportRealProcessedArt();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.0f;
        cam.backgroundColor = new Color32(34, 14, 84, 255);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObj.AddComponent<AudioListener>();

        // 2. Fundo Real da Capa sem Emendas (overlap +0.08f elimina qualquer fresta de 1px!)
        GameObject bgRoot = new GameObject("01_Cenario_Parallax");
        const float tileW = 18.6f;

        Sprite skySp = LoadArt("bg_sky_galaxy.png");
        Transform skyA = MakeTile(bgRoot.transform, "Sky_A", skySp, new Vector3(0f, 1.75f, 0f), tileW + 0.08f, 7.6f, "Sky", 0);
        Transform skyB = MakeTile(bgRoot.transform, "Sky_B", skySp, new Vector3(-tileW, 1.75f, 0f), tileW + 0.08f, 7.6f, "Sky", 0);

        Sprite midSp = LoadArt("bg_candy_midground.png");
        Transform midA = MakeTile(bgRoot.transform, "Mid_A", midSp, new Vector3(0f, 0.40f, 0f), tileW + 0.08f, 4.95f, "Midground", 0);
        Transform midB = MakeTile(bgRoot.transform, "Mid_B", midSp, new Vector3(-tileW, 0.40f, 0f), tileW + 0.08f, 4.95f, "Midground", 0);

        Sprite roadSp = LoadArt("candy_road_surface.png");
        Transform roadA = MakeTile(bgRoot.transform, "Road_A", roadSp, new Vector3(0f, -2.55f, 0f), tileW + 0.08f, 5.15f, "Road", 0);
        Transform roadB = MakeTile(bgRoot.transform, "Road_B", roadSp, new Vector3(-tileW, -2.55f, 0f), tileW + 0.08f, 5.15f, "Road", 0);

        // Casinhas de Gengibre reais na frente da pista elevada (camada Midground order 8)!
        Sprite gingerSp = LoadArt("gingerbread_house.png");
        if (gingerSp != null)
        {
            MakeDecor(bgRoot.transform, "CasaGengibre_Oeste", gingerSp, new Vector3(-6.4f, 0.50f, 0f), 1.42f, "Midground", 8);
            MakeDecor(bgRoot.transform, "CasaGengibre_Leste", gingerSp, new Vector3(6.6f, 0.50f, 0f), 1.42f, "Midground", 8);
        }

        // Sprites persistentes em PNG (zero textura em memória serializada na cena!)
        Sprite speedMatSp = LoadArt("speed_boost_mat.png");
        Sprite glowSp = LoadArt("magic_glow.png");
        Sprite starSp = LoadArt("star_sparkle.png");
        Sprite shadowSp = LoadArt("soft_shadow.png");

        // 3. SEGUNDO NÍVEL: Pista Elevada Estrelada atrás das casas + Cancela Mágica + Vagão Doce!
        GameObject elevRoot = new GameObject("02_Pista_Elevada_e_Eco");
        Sprite elevTrackSp = LoadArt("elevated_star_track.png");
        if (elevTrackSp != null)
            MakeTile(elevRoot.transform, "PistaElevada_Estrelada", elevTrackSp, new Vector3(0f, 2.05f, 0f), 17.8f, 1.25f, "ElevatedTrack", 0);

        // Cancela Mágica compatível com o movimento de Suri
        GameObject gatePivot = new GameObject("CancelaMagica_Pivot");
        gatePivot.transform.SetParent(elevRoot.transform, false);
        gatePivot.transform.position = new Vector3(2.8f, 2.25f, 0f);
        gatePivot.transform.localRotation = Quaternion.Euler(0f, 0f, 76f);
        GameObject gateBar = new GameObject("CancelaBarra");
        gateBar.transform.SetParent(gatePivot.transform, false);
        gateBar.transform.localPosition = new Vector3(-0.95f, 0f, 0f);
        gateBar.transform.localScale = new Vector3(1.15f, 0.28f, 1f);
        SpriteRenderer gateSr = gateBar.AddComponent<SpriteRenderer>();
        gateSr.sprite = speedMatSp;
        gateSr.sortingLayerName = "ElevatedTrack";
        gateSr.sortingOrder = 10;

        GameObject gateGlowObj = new GameObject("CancelaBrilho");
        gateGlowObj.transform.SetParent(gatePivot.transform, false);
        gateGlowObj.transform.localScale = Vector3.one * 2.1f;
        SpriteRenderer gateGlowSr = gateGlowObj.AddComponent<SpriteRenderer>();
        gateGlowSr.sprite = glowSp;
        gateGlowSr.color = new Color(0.80f, 0.50f, 1f, 0.35f);
        gateGlowSr.sortingLayerName = "VFX";
        gateGlowSr.sortingOrder = 5;

        // Vagão Doce correndo sozinho na pista elevada
        GameObject wagonObj = new GameObject("VagaoDoce_Sozinho");
        wagonObj.transform.SetParent(elevRoot.transform, false);
        wagonObj.transform.position = new Vector3(-8.5f, 2.75f, 0f);
        wagonObj.transform.localScale = Vector3.one * 0.85f;
        SpriteRenderer wSr = wagonObj.AddComponent<SpriteRenderer>();
        wSr.sprite = LoadArt("runaway_candy_wagon.png");
        wSr.sortingLayerName = "ElevatedTrack";
        wSr.sortingOrder = 8;

        // Eco do Tempo (o outro carrinho lá embaixo com uma amiga acenando!)
        GameObject echoObj = new GameObject("EcoDoTempo_Carrinho");
        echoObj.transform.SetParent(elevRoot.transform, false);
        echoObj.transform.position = new Vector3(11.0f, -2.65f, 0f);
        echoObj.transform.localScale = Vector3.one * 0.62f;
        SpriteRenderer echoSr = echoObj.AddComponent<SpriteRenderer>();
        echoSr.sprite = LoadArt("hero_girl_power_cart.png");
        echoSr.color = new Color(0.82f, 0.88f, 1f, 0.72f);
        echoSr.sortingLayerName = "Entities";
        echoSr.sortingOrder = 180;

        GameObject waveObj = new GameObject("AcenoEstrela");
        waveObj.transform.SetParent(echoObj.transform, false);
        waveObj.transform.localPosition = new Vector3(0.35f, 1.95f, 0f);
        waveObj.transform.localScale = Vector3.one * 1.15f;
        SpriteRenderer waveSr = waveObj.AddComponent<SpriteRenderer>();
        waveSr.sprite = starSp;
        waveSr.color = new Color(1f, 0.90f, 0.32f, 1f);
        waveSr.sortingLayerName = "VFX";
        waveSr.sortingOrder = 25;
        echoObj.SetActive(false);

        ElevatedTrackAndGate elevCtrl = elevRoot.AddComponent<ElevatedTrackAndGate>();
        elevCtrl.barrierArmPivot = gatePivot.transform;
        elevCtrl.barrierGlow = gateGlowSr;
        elevCtrl.runawayWagon = wagonObj.transform;
        elevCtrl.timeEchoCart = echoObj.transform;
        elevCtrl.timeEchoWaveIcon = waveSr;

        // 4. PIRULITO GIGANTE JOGÁVEL INDEPENDENTE (Haste articulada + Cabeça espiral girando separada!)
        GameObject lolliRoot = new GameObject("03_Pirulito_Jogavel_Rampa");
        lolliRoot.transform.position = new Vector3(-9.5f, -0.82f, 0f);

        GameObject pivot = new GameObject("Haste_Pivot");
        pivot.transform.SetParent(lolliRoot.transform, false);

        // Haste listrada presa no pivô da base (inclina suavemente para virar rampa!)
        GameObject stickObj = new GameObject("Haste_Rampa");
        stickObj.transform.SetParent(pivot.transform, false);
        stickObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        stickObj.transform.localScale = Vector3.one * 1.02f;
        SpriteRenderer stickSr = stickObj.AddComponent<SpriteRenderer>();
        stickSr.sprite = LoadArt("lollipop_stick.png");
        stickSr.sortingLayerName = "Entities";
        stickSr.sortingOrder = 85;
        BoxCollider2D stickCol = stickObj.AddComponent<BoxCollider2D>();
        stickCol.isTrigger = true;
        stickCol.size = new Vector2(0.45f, 2.05f);

        // Cabeça espiral montada na ponta superior da haste (gira em torno do próprio eixo!)
        GameObject headObj = new GameObject("PirulitoCabeca_Giratoria");
        headObj.transform.SetParent(pivot.transform, false);
        headObj.transform.localPosition = new Vector3(0f, 2.22f, 0f);
        headObj.transform.localScale = Vector3.one * 1.15f;
        SpriteRenderer headSr = headObj.AddComponent<SpriteRenderer>();
        headSr.sprite = LoadArt("lollipop_head_only.png");
        headSr.sortingLayerName = "Entities";
        headSr.sortingOrder = 95;

        // Engrenagem dourada na base (alvo de Nara, com colisor real!)
        GameObject gearObj = new GameObject("Base_Engrenagem");
        gearObj.transform.SetParent(lolliRoot.transform, false);
        gearObj.transform.localPosition = new Vector3(0.48f, -0.18f, 0f);
        gearObj.transform.localScale = Vector3.one * 0.78f;
        SpriteRenderer gearSr = gearObj.AddComponent<SpriteRenderer>();
        gearSr.sprite = starSp;
        gearSr.color = new Color(1f, 0.80f, 0.25f, 1f);
        gearSr.sortingLayerName = "RoadDetails";
        gearSr.sortingOrder = 15;
        CircleCollider2D gearCol = gearObj.AddComponent<CircleCollider2D>();
        gearCol.isTrigger = true;
        gearCol.radius = 0.75f;

        GameObject gearGlow = new GameObject("GearGlow");
        gearGlow.transform.SetParent(gearObj.transform, false);
        gearGlow.transform.localScale = Vector3.one * 2.2f;
        SpriteRenderer ggSr = gearGlow.AddComponent<SpriteRenderer>();
        ggSr.sprite = glowSp;
        ggSr.sortingLayerName = "VFX";
        ggSr.sortingOrder = 4;

        GameObject rampGlow = new GameObject("MarcaDeSalto_Rampa");
        rampGlow.transform.SetParent(lolliRoot.transform, false);
        rampGlow.transform.localPosition = new Vector3(-1.05f, -0.62f, 0f);
        rampGlow.transform.localScale = new Vector3(1.55f, 1.15f, 1f);
        SpriteRenderer rgSr = rampGlow.AddComponent<SpriteRenderer>();
        rgSr.sprite = speedMatSp;
        rgSr.sortingLayerName = "RoadDetails";
        rgSr.sortingOrder = 12;

        LollipopRampMechanism rampMech = lolliRoot.AddComponent<LollipopRampMechanism>();
        rampMech.stemPivot = pivot.transform;
        rampMech.swirlHead = headObj.transform;
        rampMech.baseGear = gearObj.transform;
        rampMech.rampGlow = rgSr;
        rampMech.gearHighlight = ggSr;

        // 5. AS 3 AMIGAS NO CENÁRIO AO MESMO TEMPO: Íria (Carrinho), Nara (Mariposa), Suri (Personagem em pé!)
        GameObject iriaObj = new GameObject("Iria_Carrinho");
        iriaObj.transform.position = new Vector3(4.2f, -2.55f, 0f);
        iriaObj.transform.localScale = Vector3.one * 0.72f;
        SpriteRenderer iriaSr = iriaObj.AddComponent<SpriteRenderer>();
        iriaSr.sprite = LoadArt("hero_girl_power_cart.png");
        iriaSr.sortingLayerName = "Entities";
        iriaSr.sortingOrder = 260;
        BoxCollider2D iriaCol = iriaObj.AddComponent<BoxCollider2D>();
        iriaCol.isTrigger = true;
        iriaCol.size = new Vector2(2.6f, 1.3f);

        GameObject cartSh = new GameObject("SombraCarrinho");
        cartSh.transform.SetParent(iriaObj.transform, false);
        cartSh.transform.localPosition = new Vector3(0.05f, -1.95f, 0f);
        cartSh.transform.localScale = new Vector3(3.1f, 0.95f, 1f);
        SpriteRenderer cartShSr = cartSh.AddComponent<SpriteRenderer>();
        cartShSr.sprite = shadowSp;
        cartShSr.sortingLayerName = "RoadDetails";
        cartShSr.sortingOrder = 10;

        GameObject naraObj = new GameObject("Nara_MariposaMecanica");
        naraObj.transform.position = new Vector3(5.0f, -1.65f, 0f);
        naraObj.transform.localScale = Vector3.one * 0.58f;
        SpriteRenderer naraSr = naraObj.AddComponent<SpriteRenderer>();
        naraSr.sprite = LoadArt("clockwork_moth.png");
        naraSr.sortingLayerName = "VFX";
        naraSr.sortingOrder = 30;

        GameObject suriObj = new GameObject("Suri_Gravadora");
        suriObj.transform.position = new Vector3(5.6f, -2.35f, 0f);
        suriObj.transform.localScale = Vector3.one * 0.62f;
        SpriteRenderer suriSr = suriObj.AddComponent<SpriteRenderer>();
        suriSr.sprite = LoadArt("suri_character.png");
        suriSr.sortingLayerName = "Entities";
        suriSr.sortingOrder = 250;

        // 6. Controlador Cooperativo + HUD das 3 Amigas!
        GameObject ctrlObj = new GameObject("AvessoTrioManager");
        AvessoTrioController trio = ctrlObj.AddComponent<AvessoTrioController>();
        trio.iriaCart = iriaObj.transform;
        trio.naraMoth = naraObj.transform;
        trio.suriMarker = suriObj.transform;
        trio.lollipopRamp = rampMech;
        trio.elevatedSystem = elevCtrl;
        trio.mainCam = cam;
        trio.skyA = skyA; trio.skyB = skyB;
        trio.midA = midA; trio.midB = midB;
        trio.roadA = roadA; trio.roadB = roadB;
        trio.tileWidth = tileW;

        AvessoHUDController hud = ctrlObj.AddComponent<AvessoHUDController>();
        hud.portraitIria = LoadArt("portrait_iria.png");
        hud.portraitNara = LoadArt("portrait_nara.png");
        hud.portraitSuri = LoadArt("portrait_suri.png");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, AvessoScenePath);

        if (File.Exists(Path.Combine(Application.dataPath, "Scenes/GirlPower_SweetWorld.unity")))
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AvessoScenePath, true),
                new EditorBuildSettingsScene(SweetScenePath, true)
            };
        }
        else
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(AvessoScenePath, true) };
        }

        AssetDatabase.SaveAssets();
        Debug.Log("O Avesso das Horas: Cena atualizada (PNGs persistentes, haste articulada limpa, Suri ilustrada, 3 voltas reais e Pista Elevada atrás das casas)!");
    }

    public static void EnsureUnifiedSortingLayers()
    {
        string[] layers = { "Sky", "ElevatedTrack", "Midground", "Road", "RoadDetails", "Entities", "Foreground", "VFX", "UI" };
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
            e.FindPropertyRelative("uniqueID").intValue = 2200 + i * 29;
        }
        tm.ApplyModifiedProperties();
    }

    private static void ReimportRealProcessedArt()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string absDir = Path.Combine(Application.dataPath, ArtFolder.Replace("Assets/", ""));
        if (!Directory.Exists(absDir)) return;
        foreach (string f in Directory.GetFiles(absDir, "*.png"))
        {
            string rel = ArtFolder + "/" + Path.GetFileName(f);
            TextureImporter imp = AssetImporter.GetAtPath(rel) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = (Path.GetFileName(f).StartsWith("bg_") || Path.GetFileName(f).Contains("road")) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
    }

    private static Transform MakeTile(Transform parent, string name, Sprite sp, Vector3 pos, float w, float h, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        if (sp != null)
            go.transform.localScale = new Vector3(w / Mathf.Max(0.01f, sp.bounds.size.x), h / Mathf.Max(0.01f, sp.bounds.size.y), 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go.transform;
    }

    private static void MakeDecor(Transform parent, string name, Sprite sp, Vector3 pos, float scale, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
    }

    private static Sprite LoadArt(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
}
