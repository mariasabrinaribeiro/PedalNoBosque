using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Duplicates "Assets/Scenes/Girl power 1- Sorvete.unity" into
/// "Assets/Scenes/Girl Power - Cenario Refinado.unity" (leaving the original scene
/// and all shared prefabs 100% untouched) and refines the initial stretch, ground
/// surfaces, curves, 14 unique cutout confectionery houses, grouped decorations,
/// fence passages, and top-down camera framing.
/// </summary>
public static class GirlPowerCenarioRefinadoBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Girl power 1- Sorvete.unity";
    private const string RefinedScenePath = "Assets/Scenes/Girl Power - Cenario Refinado.unity";
    private const string ArtFolder = "Assets/CandyRefinado/Art";
    private const string RefinedRootName = "CenarioRefinado_MundoDosDoces";

    // Exact original start coordinate of "carro" in Girl power 1- Sorvete.unity
    private static readonly Vector2 StartCarPos = new Vector2(59.3f, 20.0f);

    [MenuItem("Girl Power/1. Criar ou Atualizar Cena Refinada (Girl Power - Cenario Refinado)")]
    public static void BuildRefinedScene()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl power 1- Sorvete.unity")))
        {
            Debug.LogError("Cena original não encontrada: " + SourceScenePath);
            return;
        }

        // 1. Register clean Sorting Layers (Ground -> GroundDetails -> PropsAndVehicles -> UI)
        EnsureSortingLayers("Ground", "GroundDetails", "PropsAndVehicles", "UI");

        // 2. Generate & import all 3-surface textures, props & 14 clean transparent house cutouts
        CandyRefinadoArtGenerator.GenerateAndImportAll(ArtFolder);

        // 2. Duplicate the original scene cleanly so Girl power 1- Sorvete.unity is NEVER modified
        if (File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl Power - Cenario Refinado.unity")))
            AssetDatabase.DeleteAsset(RefinedScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, RefinedScenePath))
        {
            Debug.LogError("Falha ao duplicar cena para: " + RefinedScenePath);
            return;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(RefinedScenePath, OpenSceneMode.Single);

        // 3. Keep core gameplay objects (carro, Main Camera, Canvas, EventSystem, Audio, ControladorJogo)
        //    and remove the broken blurry ground, 512dirt colliders, opaque JPEG houses, and scattered clutter.
        GameObject carRoot = null;
        GameObject mainCamObj = null;
        List<GameObject> oldRoots = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            DriverPro foundDriver = root.GetComponentInChildren<DriverPro>(true);
            if (foundDriver != null)
            {
                // Unparent carro cleanly to scene root before removing old Evironment clutter!
                carRoot = foundDriver.gameObject;
                carRoot.transform.SetParent(null, true);
                carRoot.SetActive(true);
                if (root != carRoot) oldRoots.Add(root);
                continue;
            }
            if (root.name == "Main Camera" || root.CompareTag("MainCamera"))
            {
                mainCamObj = root;
                root.SetActive(true);
                continue;
            }
            if (root.name == "Canvas" || root.name == "EventSystem" || root.name == "ControladorJogo" ||
                root.name.Contains("Audio") || root.name.Contains("Musica") || root.name.Contains("Som") ||
                root.GetComponent<ControladorJogo>() != null || root.GetComponent<AudioSource>() != null)
            {
                root.SetActive(true);
                continue;
            }
            oldRoots.Add(root);
        }

        foreach (GameObject old in oldRoots)
            Object.DestroyImmediate(old);

        // 4. Clean Main Camera & tune top-down framing so the pink cart stays clearly recognizable
        if (mainCamObj != null)
        {
            List<GameObject> camJunk = new List<GameObject>();
            foreach (Transform child in mainCamObj.transform)
            {
                if (child.GetComponent<Canvas>() != null) { child.SetParent(null, true); continue; }
                if (child.GetComponent<AudioSource>() != null) continue;
                camJunk.Add(child.gameObject);
            }
            foreach (GameObject j in camJunk) Object.DestroyImmediate(j);

            Camera cam = mainCamObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6.2f; // Recognizable pink cart + generous view of smooth curves ahead!
                cam.backgroundColor = new Color32(174, 232, 194, 255);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            mainCamObj.transform.position = new Vector3(StartCarPos.x, StartCarPos.y, -10f);
            if (mainCamObj.GetComponent<GameCameraController>() == null)
                mainCamObj.AddComponent<GameCameraController>();
        }

        // 5. Align & fix the pink ice cream cart ("carro") at the exact original starting spot (59.3, 20.0)
        if (carRoot != null)
        {
            carRoot.transform.position = new Vector3(StartCarPos.x, StartCarPos.y, 0f);
            carRoot.transform.rotation = Quaternion.identity;
            carRoot.transform.localScale = Vector3.one * 1.15f; // Crisp & unmistakable while moving!
            TrySetTag(carRoot, "Player");

            SpriteRenderer rootSr = carRoot.GetComponent<SpriteRenderer>();
            if (rootSr != null)
            {
                rootSr.sortingLayerName = "PropsAndVehicles";
                rootSr.sortingOrder = 14;
            }

            Transform childCar = carRoot.transform.Find("Carro1");
            if (childCar != null)
            {
                childCar.localPosition = Vector3.zero;
                childCar.localRotation = Quaternion.identity;
                if (rootSr != null && rootSr.sprite != null)
                {
                    SpriteRenderer csr = childCar.GetComponent<SpriteRenderer>();
                    if (csr != null) csr.enabled = false;
                }
                foreach (Collider2D c in childCar.GetComponents<Collider2D>()) c.enabled = false;
            }

            BoxCollider2D col = carRoot.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.offset = Vector2.zero;
                col.size = new Vector2(0.82f, 1.65f);
                col.isTrigger = false;
            }

            Sprite shadow = LoadArt("CandyShadow.png");
            if (shadow != null)
            {
                SpriteRenderer sh = CreateSprite(carRoot.transform, "SombraDoCarrinho", shadow, new Vector2(0.08f, -0.14f), new Vector2(1.05f, 1.50f), 10, 0f);
                sh.color = new Color(1f, 1f, 1f, 0.85f);
            }
        }

        if (Object.FindObjectOfType<MissionManager>() == null)
            new GameObject("MissionManager").AddComponent<MissionManager>();

        // 6. Build the non-repetitive 3-surface Candy World aligned around the exact route (28..72, 10..40)
        GameObject world = new GameObject(RefinedRootName);
        List<Transform> swaying = new List<Transform>();
        List<SpriteRenderer> glows = new List<SpriteRenderer>();
        List<SpriteRenderer> sparkles = new List<SpriteRenderer>();
        List<Transform> pickups = new List<Transform>();

        BuildThreeSurfaceGroundAndPath(world.transform);
        BuildDeliveryPlazas(world.transform, glows);
        BuildFourteenUniqueHousesAndYards(world.transform, swaying);
        BuildPondsAndGroupedDecor(world.transform, swaying, glows, sparkles);
        BuildPickupsAndBoosts(world.transform, pickups);
        BuildOuterBoundary(world.transform);

        CandyRefinadoAmbient ambient = world.AddComponent<CandyRefinadoAmbient>();
        ambient.swayingProps = swaying.ToArray();
        ambient.pulsingGlows = glows.ToArray();
        ambient.floatingSparkles = sparkles.ToArray();
        ambient.bobbingPickups = pickups.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, RefinedScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: Cena 'Girl Power - Cenario Refinado' criada com sucesso a partir de 'Girl power 1- Sorvete.unity'! Superfícies diferenciadas (biscoito dourado liso, bordas creme suave, prados/quintais pastel variados) e 14 casas únicas sem manchas ou recortes.");
    }

    [MenuItem("Girl Power/2. Capturar Antes e Depois (Mesmo Enquadramento)")]
    public static void CaptureBeforeAndAfter()
    {
        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Relatorios");
        Directory.CreateDirectory(outDir);

        CaptureSceneShot(SourceScenePath, Path.Combine(outDir, "Antes_GirlPower_Original.png"));
        if (File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl Power - Cenario Refinado.unity")))
            CaptureSceneShot(RefinedScenePath, Path.Combine(outDir, "Depois_GirlPower_CenarioRefinado.png"));

        Debug.Log("Capturas salvas no mesmo enquadramento em: " + outDir);
    }

    private static void CaptureSceneShot(string scenePath, string outPngPath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Camera cam = Camera.main ?? Object.FindObjectOfType<Camera>();
        if (cam == null) return;

        Vector3 oldPos = cam.transform.position;
        float oldSize = cam.orthographicSize;
        cam.transform.position = new Vector3(StartCarPos.x, StartCarPos.y, -10f);
        cam.orthographic = true;
        cam.orthographicSize = 6.2f;

        RenderTexture rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(outPngPath, tex.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        cam.transform.position = oldPos;
        cam.orthographicSize = oldSize;
    }

    // =========================================================================
    // 1. THREE-SURFACE GROUND & SMOOTH CURVED GOLDEN-BISCUIT ROUTE
    //    Route follows the real corridor of Girl power 1- Sorvete.unity:
    //    - Start Avenue: x = 59.3, y = 14.2..25.8 (through South & North fence gates!)
    //    - South Boulevard: y = 14.2, x = 28.5..70.5
    //    - North Boulevard: y = 25.8, x = 28.5..70.5 -> Praça Oeste (35.3, 25.8)
    //    - North Delivery Loop: x = 53.1 / 59.3 up to y = 37.4 (Descarga)
    // =========================================================================
    private static void BuildThreeSurfaceGroundAndPath(Transform root)
    {
        Transform g = CreateGroup(root, "01_Chao_TresSuperficies");

        // Surface 3 base: calm, seamless Pastel Mint Meadow (no ugly stretched download(1).png!)
        CreateTiled(g, "PradoPastel_Menta", LoadArt("PastelMintGround.png"), new Vector2(50f, 24f), new Vector2(110f, 84f), -30);

        // Varied pastel organic yard pads (Rose, Lavender, Lemon) under distinct house neighborhoods
        // so every block looks handcrafted and NEVER repetitive!
        Sprite rose = LoadArt("PastelYardRose.png");
        Sprite lav = LoadArt("PastelYardLavender.png");
        Sprite lemon = LoadArt("PastelYardLemon.png");

        CreateSprite(g, "QuintalPastel_Rosa_CentroOeste", rose, new Vector2(50.2f, 20.0f), new Vector2(1.55f, 1.45f), -24, 0f);
        CreateSprite(g, "QuintalPastel_Lavanda_Norte", lav, new Vector2(52.0f, 31.2f), new Vector2(1.75f, 1.35f), -24, 4f);
        CreateSprite(g, "QuintalPastel_Limao_Leste", lemon, new Vector2(65.0f, 20.2f), new Vector2(1.35f, 1.35f), -24, -3f);
        CreateSprite(g, "QuintalPastel_Lavanda_Sul", lav, new Vector2(49.5f, 8.8f), new Vector2(1.80f, 1.25f), -24, 2f);
        CreateSprite(g, "QuintalPastel_Rosa_Oeste", rose, new Vector2(36.8f, 20.0f), new Vector2(1.55f, 1.40f), -24, -2f);
        CreateSprite(g, "QuintalPastel_Limao_Noroeste", lemon, new Vector2(38.0f, 31.6f), new Vector2(1.60f, 1.35f), -24, 3f);
        CreateSprite(g, "QuintalPastel_Rosa_LesteExt", rose, new Vector2(76.4f, 20.0f), new Vector2(1.40f, 1.65f), -24, 0f);
        CreateSprite(g, "QuintalPastel_Lavanda_SulLeste", lav, new Vector2(65.5f, 8.5f), new Vector2(1.50f, 1.25f), -24, -4f);

        // Surfaces 1 & 2: Smooth Golden Biscuit Path + Soft Vanilla Cream Borders
        Sprite roadH = LoadArt("BiscuitRoadH.png");
        Sprite roadV = LoadArt("BiscuitRoadV.png");
        Sprite cross = LoadArt("BiscuitRoadCross.png");
        Sprite corner = LoadArt("BiscuitRoadCorner.png");
        Sprite tJunc = LoadArt("BiscuitRoadT.png");
        const float W = 5.8f;

        // Horizontal boulevards (y = 14.2 South, y = 25.8 North, y = 37.4 Far North Delivery)
        CreateTiled(g, "CaminhoSul_Oeste", roadH, new Vector2(43.9f, 14.2f), new Vector2(26.2f, W), -15);
        CreateTiled(g, "CaminhoSul_Leste", roadH, new Vector2(64.9f, 14.2f), new Vector2(6.8f, W), -15);

        CreateTiled(g, "CaminhoNorte_Oeste", roadH, new Vector2(43.9f, 25.8f), new Vector2(26.2f, W), -15);
        CreateTiled(g, "CaminhoNorte_Leste", roadH, new Vector2(64.9f, 25.8f), new Vector2(6.8f, W), -15);

        CreateTiled(g, "CaminhoTopo_Descarga", roadH, new Vector2(47.2f, 37.4f), new Vector2(20.0f, W), -15);

        // Vertical avenues (x = 59.3 Start Avenue, x = 28.5 West, x = 70.5 East, x = 35.3 NW link)
        CreateTiled(g, "AvInicial_Centro", roadV, new Vector2(59.3f, 20.0f), new Vector2(W, 6.8f), -15);
        CreateTiled(g, "AvInicial_Norte", roadV, new Vector2(59.3f, 31.6f), new Vector2(W, 6.8f), -15);
        CreateTiled(g, "AvOeste", roadV, new Vector2(28.5f, 20.0f), new Vector2(W, 6.8f), -15);
        CreateTiled(g, "AvLeste", roadV, new Vector2(70.5f, 20.0f), new Vector2(W, 6.8f), -15);
        CreateTiled(g, "AvPracaOeste_Norte", roadV, new Vector2(35.3f, 31.6f), new Vector2(W, 6.8f), -15);

        // Intersections & T-junctions
        CreateSprite(g, "Entroncamento_InicialNorte", cross, new Vector2(59.3f, 25.8f), Vector2.one, -12, 0f);
        CreateSprite(g, "Entroncamento_InicialSul", tJunc, new Vector2(59.3f, 14.2f), Vector2.one, -12, 180f);
        CreateSprite(g, "Entroncamento_PracaOeste", tJunc, new Vector2(35.3f, 25.8f), Vector2.one, -12, 180f);

        // Smooth curved biscuit corners around the entire loop!
        CreateSprite(g, "Curva_SO", corner, new Vector2(28.5f, 14.2f), Vector2.one, -12, 0f);
        CreateSprite(g, "Curva_SE", corner, new Vector2(70.5f, 14.2f), Vector2.one, -12, 90f);
        CreateSprite(g, "Curva_NE", corner, new Vector2(70.5f, 25.8f), Vector2.one, -12, 180f);
        CreateSprite(g, "Curva_NO", corner, new Vector2(28.5f, 25.8f), Vector2.one, -12, 270f);
        CreateSprite(g, "Curva_TopoNO", corner, new Vector2(35.3f, 37.4f), Vector2.one, -12, 270f);
        CreateSprite(g, "Curva_TopoNE", corner, new Vector2(59.3f, 37.4f), Vector2.one, -12, 180f);
    }

    private static void BuildDeliveryPlazas(Transform root, List<SpriteRenderer> glows)
    {
        Transform p = CreateGroup(root, "02_Pracas_de_Entrega");
        Sprite plaza = LoadArt("CandyDeliveryPlaza.png");
        Sprite glow = LoadArt("CandyGlow.png");

        // Exact original delivery spots preserved & upgraded into glowing Candy Star Plazas!
        CreateDeliveryPlaza(p, "Descarga de sorvete - Praça Oeste", plaza, glow, new Vector2(35.3f, 25.8f), glows);
        CreateDeliveryPlaza(p, "Descarga de sorvete - Praça Norte", plaza, glow, new Vector2(53.1f, 37.4f), glows);
    }

    private static void CreateDeliveryPlaza(Transform parent, string name, Sprite plaza, Sprite glow, Vector2 pos, List<SpriteRenderer> glows)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * 0.84f;
        TrySetTag(go, "Customer");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = plaza;
        sr.sortingOrder = -5;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 3.1f;

        SpriteRenderer g = CreateSprite(go.transform, "BrilhoPraca", glow, Vector2.zero, Vector2.one * 3.2f, -4, 0f);
        g.color = new Color(1f, 0.74f, 0.90f, 0.34f);
        glows.Add(g);
    }

    // =========================================================================
    // 2. ALL 14 UNIQUE CONFECTIONERY & CAFE HOUSES (ZERO REPETITION!)
    //    Grouped with yards, wafer fences, cotton-candy trees & edge sprinkles!
    // =========================================================================
    private static void BuildFourteenUniqueHousesAndYards(Transform root, List<Transform> swaying)
    {
        Transform h = CreateGroup(root, "03_Casas_de_Confeitaria_Unicas");

        // Start Area (immediately visible from camera at 59.3, 20.0 — matching & refining Image 2 & Image 3!)
        PlaceHouse(h, "Casa_ArcoIris_Inicial", "House_Rainbow.png", new Vector2(52.4f, 20.4f), 0.72f);
        PlaceHouse(h, "Casa_ChocoMenta_Inicial", "House_ChocoMint.png", new Vector2(46.8f, 20.4f), 0.72f);
        PlaceHouse(h, "Casa_FogueteAzul_Norte", "House_RocketBlue.png", new Vector2(52.6f, 31.4f), 0.74f);
        PlaceHouse(h, "Casa_Morango_Leste", "House_Strawberry.png", new Vector2(65.0f, 20.4f), 0.70f);

        // South Boulevard Yards (y ~ 8.8)
        PlaceHouse(h, "Casa_Cappuccino_Sul", "House_Cappuccino.png", new Vector2(52.2f, 8.8f), 0.72f);
        PlaceHouse(h, "Casa_BoloCereja_Sul", "House_CherryCake.png", new Vector2(65.2f, 8.8f), 0.72f);
        PlaceHouse(h, "Casa_XadrezVermelho_Sul", "House_Gingham.png", new Vector2(43.2f, 8.8f), 0.72f);
        PlaceHouse(h, "Casa_Tiramisu_SulOeste", "House_Tiramisu.png", new Vector2(34.2f, 8.8f), 0.72f);

        // Central-West & North Park Yards
        PlaceHouse(h, "Casa_EstufaBotanica_Norte", "House_Botanical.png", new Vector2(44.2f, 31.5f), 0.76f);
        PlaceHouse(h, "Casa_ChaDeRosas_Oeste", "House_TeaRose.png", new Vector2(39.4f, 20.2f), 0.72f);
        PlaceHouse(h, "Casa_Galaxia_Noroeste", "House_Galaxy.png", new Vector2(28.5f, 32.0f), 0.74f);
        PlaceHouse(h, "Casa_Matcha_Topo", "House_Matcha.png", new Vector2(43.5f, 43.2f), 0.74f);
        PlaceHouse(h, "Casa_Nautica_Leste", "House_Nautical.png", new Vector2(76.6f, 22.8f), 0.74f);
        PlaceHouse(h, "Casa_Samovar_LesteSul", "House_Samovar.png", new Vector2(76.6f, 15.6f), 0.72f);

        // Grouped Chocolate Wafer Fences creating the iconic clean passages at Start (y=17.1 & y=28.7)
        Sprite wafer = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (63).png");
        if (wafer != null)
        {
            // South gate framing the start avenue (clean 6.2-unit wide passage centered on x = 59.3!)
            Vector2[] fences =
            {
                new Vector2(54.8f, 17.4f), new Vector2(52.2f, 17.4f), new Vector2(49.6f, 17.4f),
                new Vector2(63.8f, 17.4f), new Vector2(66.4f, 17.4f),
                // North gate framing the start avenue toward northern houses
                new Vector2(54.8f, 28.8f), new Vector2(63.8f, 28.8f), new Vector2(66.4f, 28.8f),
                // Yard fences around South houses
                new Vector2(47.6f, 11.1f), new Vector2(56.2f, 11.1f)
            };
            for (int i = 0; i < fences.Length; i++)
            {
                SpriteRenderer sr = CreateSprite(h, "CercaWafer_" + (i + 1), wafer, fences[i], Vector2.one * 0.34f, 3, 0f);
                BoxCollider2D bc = sr.gameObject.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(5.6f, 1.8f);
            }
        }
    }

    private static void PlaceHouse(Transform parent, string name, string fileName, Vector2 pos, float scale)
    {
        Sprite sp = LoadArt(fileName);
        if (sp == null) return;
        SpriteRenderer sr = CreateSprite(parent, name, sp, pos, Vector2.one * scale, 2, 0f);
        BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4.0f, 3.2f);
        col.offset = new Vector2(0f, -0.4f);
    }

    // =========================================================================
    // 3. GROUPED DECORATIONS AROUND HOUSES, SIDES & FLOWERBEDS (NEVER IN LANE CENTER!)
    // =========================================================================
    private static void BuildPondsAndGroupedDecor(
        Transform root,
        List<Transform> swaying,
        List<SpriteRenderer> glows,
        List<SpriteRenderer> sparkles)
    {
        Transform d = CreateGroup(root, "04_Decoracao_Agrupada_e_Lagoas");

        Sprite pond = LoadArt("CandyBerryPond.png");
        Sprite treePink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/download__2_-removebg-preview.png");
        Sprite treePine = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (67).png");
        Sprite lolliA = LoadArt("GiantLollipopA.png");
        Sprite lolliB = LoadArt("GiantLollipopB.png");
        Sprite cane = LoadArt("CandyCanePost.png");
        Sprite lamp = LoadArt("CandyLampPost.png");
        Sprite flower = LoadArt("SugarFlowerBed.png");
        Sprite marsh = LoadArt("MarshmallowCluster.png");
        Sprite sprinkles = LoadArt("SprinkleEdgeCluster.png");
        Sprite glow = LoadArt("CandyGlow.png");
        Sprite sparkle = LoadArt("CandySparkle.png");

        // 2 Sparkling Berry-Soda Ponds tucked in scenic gardens
        SpriteRenderer pond1 = CreateSprite(d, "LagoaSoda_NorteLeste", pond, new Vector2(66.2f, 32.8f), Vector2.one * 0.56f, -8, 0f);
        CapsuleCollider2D pc1 = pond1.gameObject.AddComponent<CapsuleCollider2D>();
        pc1.direction = CapsuleDirection2D.Horizontal; pc1.size = new Vector2(8.2f, 4.4f);

        SpriteRenderer pond2 = CreateSprite(d, "LagoaSoda_CentroOeste", pond, new Vector2(34.2f, 20.0f), Vector2.one * 0.50f, -8, 0f);
        CapsuleCollider2D pc2 = pond2.gameObject.AddComponent<CapsuleCollider2D>();
        pc2.direction = CapsuleDirection2D.Horizontal; pc2.size = new Vector2(8.2f, 4.4f);

        // Concentrate sprinkle & tiny flower clusters ONLY on road sides, near houses & flowerbeds!
        Vector2[] sprinkleSpots =
        {
            new Vector2(55.4f, 18.8f), new Vector2(55.4f, 22.0f), new Vector2(63.2f, 18.8f), new Vector2(63.2f, 22.2f),
            new Vector2(50.2f, 17.5f), new Vector2(46.0f, 17.5f), new Vector2(52.8f, 29.1f), new Vector2(44.2f, 29.1f),
            new Vector2(39.4f, 17.6f), new Vector2(64.8f, 11.2f), new Vector2(52.0f, 11.2f), new Vector2(43.2f, 11.2f)
        };
        for (int i = 0; i < sprinkleSpots.Length; i++)
            CreateSprite(d, "ConfeitosLateral_" + (i + 1), sprinkles, sprinkleSpots[i], Vector2.one * 0.72f, -10, (i % 3 - 1) * 14f);

        // Cotton-candy trees & candy pines grouped naturally around house yards (NEVER blocking the biscuit road!)
        Vector2[] pinkTrees =
        {
            new Vector2(55.2f, 22.8f), new Vector2(43.6f, 21.6f), new Vector2(62.8f, 22.8f),
            new Vector2(55.8f, 33.4f), new Vector2(48.8f, 32.8f), new Vector2(61.4f, 8.8f),
            new Vector2(47.8f, 8.2f), new Vector2(38.6f, 9.2f), new Vector2(23.5f, 20.5f),
            new Vector2(75.5f, 26.8f), new Vector2(57.8f, 42.2f), new Vector2(33.2f, 42.0f)
        };
        for (int i = 0; i < pinkTrees.Length; i++)
        {
            if (treePink == null) break;
            SpriteRenderer sr = CreateSprite(d, "ArvoreAlgodaoDoce_" + (i + 1), treePink, pinkTrees[i], Vector2.one * (0.42f + (i % 3) * 0.05f), 4, 0f);
            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 1.0f; col.offset = new Vector2(0f, -0.55f);
            swaying.Add(sr.transform);
        }

        Vector2[] pineTrees =
        {
            new Vector2(67.2f, 22.6f), new Vector2(49.6f, 22.6f), new Vector2(41.2f, 32.8f),
            new Vector2(68.2f, 9.8f), new Vector2(55.6f, 8.2f), new Vector2(30.4f, 9.6f),
            new Vector2(23.8f, 14.5f), new Vector2(23.8f, 26.5f), new Vector2(75.8f, 11.2f),
            new Vector2(62.6f, 34.4f), new Vector2(48.5f, 42.5f)
        };
        for (int i = 0; i < pineTrees.Length; i++)
        {
            if (treePine == null) break;
            SpriteRenderer sr = CreateSprite(d, "PinheiroDoce_" + (i + 1), treePine, pineTrees[i], Vector2.one * (0.36f + (i % 2) * 0.05f), 4, 0f);
            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 1.0f; col.offset = new Vector2(0f, -0.55f);
            swaying.Add(sr.transform);
        }

        // Giant swirled lollipops & candy canes marking intersections & house entrances
        Vector2[] lollis =
        {
            new Vector2(55.6f, 17.6f), new Vector2(63.0f, 17.6f),
            new Vector2(55.6f, 22.4f), new Vector2(63.0f, 22.4f),
            new Vector2(39.0f, 29.2f), new Vector2(31.8f, 29.2f),
            new Vector2(56.0f, 34.4f), new Vector2(49.8f, 34.4f)
        };
        for (int i = 0; i < lollis.Length; i++)
        {
            SpriteRenderer sr = CreateSprite(d, "Pirulito_" + (i + 1), i % 2 == 0 ? lolliA : lolliB, lollis[i], Vector2.one * 0.50f, 5, 0f);
            swaying.Add(sr.transform);
        }

        Vector2[] canes = { new Vector2(49.6f, 17.8f), new Vector2(66.4f, 17.8f), new Vector2(42.2f, 17.8f), new Vector2(62.6f, 29.2f) };
        for (int i = 0; i < canes.Length; i++)
        {
            SpriteRenderer sr = CreateSprite(d, "BengalaDoce_" + (i + 1), cane, canes[i], Vector2.one * 0.52f, 4, 0f);
            sr.flipX = (i % 2 == 1);
            swaying.Add(sr.transform);
        }

        // Candy Streetlamps lighting the start corridor & boulevards softly
        Vector2[] lamps =
        {
            new Vector2(55.8f, 20.0f), new Vector2(62.8f, 20.0f),
            new Vector2(47.2f, 17.6f), new Vector2(47.2f, 29.2f),
            new Vector2(39.4f, 29.2f), new Vector2(55.8f, 31.6f)
        };
        for (int i = 0; i < lamps.Length; i++)
        {
            CreateSprite(d, "PosteDoce_" + (i + 1), lamp, lamps[i], Vector2.one * 0.50f, 5, 0f);
            SpriteRenderer g = CreateSprite(d, "LuzPoste_" + (i + 1), glow, lamps[i] + new Vector2(0f, 0.78f), Vector2.one * 1.3f, 6, 0f);
            g.color = new Color(1f, 0.90f, 0.64f, 0.38f);
            glows.Add(g);
        }

        // Sugar Flowerbeds & Marshmallows tucked next to houses & ponds
        Vector2[] flowers =
        {
            new Vector2(54.4f, 19.2f), new Vector2(49.6f, 19.6f), new Vector2(67.2f, 19.2f),
            new Vector2(55.2f, 30.2f), new Vector2(41.4f, 19.2f), new Vector2(59.4f, 9.8f)
        };
        for (int i = 0; i < flowers.Length; i++)
            CreateSprite(d, "CanteiroDoce_" + (i + 1), flower, flowers[i], Vector2.one * 0.62f, 1, 0f);

        Vector2[] marshes = { new Vector2(63.4f, 31.2f), new Vector2(36.4f, 18.2f), new Vector2(45.2f, 22.8f), new Vector2(67.8f, 17.8f) };
        for (int i = 0; i < marshes.Length; i++)
            CreateSprite(d, "Marshmallows_" + (i + 1), marsh, marshes[i], Vector2.one * 0.62f, 1, 0f);

        // Subtle floating pastel sparkles near houses & plazas
        for (int i = 0; i < 18; i++)
        {
            float sx = 30f + (i * 9.7f % 42f);
            float sy = 11f + (i * 6.3f % 28f);
            SpriteRenderer sp = CreateSprite(d, "EstrelaAcucar_" + (i + 1), sparkle, new Vector2(sx, sy), Vector2.one * (0.40f + (i % 3) * 0.12f), 9, 0f);
            sp.color = (i % 2 == 0) ? new Color(1f, 0.86f, 0.96f, 0.65f) : new Color(1f, 0.96f, 0.70f, 0.65f);
            sparkles.Add(sp);
        }
    }

    private static void BuildPickupsAndBoosts(Transform root, List<Transform> pickups)
    {
        Transform g = CreateGroup(root, "05_Itens_e_Boosts");
        Sprite donut = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (77).png");
        Sprite boost = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameDev.tv - Delivery Dash Assets - cópia/Props & Stuff/pickupspeed.png");
        Sprite glow = LoadArt("CandyGlow.png");

        // Crisp collectible donuts right along the smooth low-contrast golden biscuit center lane!
        Vector2[] spots =
        {
            new Vector2(59.3f, 23.4f), new Vector2(59.3f, 16.2f),
            new Vector2(52.0f, 14.2f), new Vector2(44.0f, 14.2f), new Vector2(36.0f, 14.2f),
            new Vector2(66.0f, 14.2f), new Vector2(70.5f, 20.0f), new Vector2(65.2f, 25.8f),
            new Vector2(51.5f, 25.8f), new Vector2(43.0f, 25.8f), new Vector2(28.5f, 20.0f),
            new Vector2(59.3f, 32.0f), new Vector2(45.0f, 37.4f), new Vector2(35.3f, 32.0f)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            GameObject go = new GameObject("pickupitem (" + (i + 1) + ")");
            go.transform.SetParent(g, false);
            go.transform.position = new Vector3(spots[i].x, spots[i].y, 0f);
            go.transform.localScale = Vector3.one * 0.28f;
            TrySetTag(go, "package");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = donut;
            sr.sortingOrder = 11;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.4f;
            if (glow != null)
            {
                SpriteRenderer gl = CreateSprite(go.transform, "BrilhoItem", glow, Vector2.zero, Vector2.one * 3.0f, 10, 0f);
                gl.color = new Color(1f, 0.72f, 0.90f, 0.42f);
            }
            pickups.Add(go.transform);
        }

        Vector2[] boosts = { new Vector2(48.0f, 14.2f), new Vector2(48.0f, 25.8f), new Vector2(66.0f, 14.2f) };
        for (int i = 0; i < boosts.Length; i++)
        {
            GameObject go = new GameObject("Boost item (" + (i + 1) + ")");
            go.transform.SetParent(g, false);
            go.transform.position = new Vector3(boosts[i].x, boosts[i].y, 0f);
            go.transform.localScale = Vector3.one * 0.85f;
            TrySetTag(go, "Boost item");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = boost;
            sr.sortingOrder = 6;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
        }
    }

    private static void BuildOuterBoundary(Transform root)
    {
        Transform b = CreateGroup(root, "06_Limites_Externos");
        CreateWall(b, "Limite_Norte", new Vector2(51f, 46.5f), new Vector2(68f, 2f));
        CreateWall(b, "Limite_Sul", new Vector2(51f, 4.5f), new Vector2(68f, 2f));
        CreateWall(b, "Limite_Oeste", new Vector2(21f, 25.5f), new Vector2(2f, 44f));
        CreateWall(b, "Limite_Leste", new Vector2(81f, 25.5f), new Vector2(2f, 44f));
    }

    private static void CreateWall(Transform p, string n, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.AddComponent<BoxCollider2D>().size = size;
    }

    private static Transform CreateGroup(Transform p, string n)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        return go.transform;
    }

    private static SpriteRenderer CreateSprite(Transform p, string n, Sprite sp, Vector2 pos, Vector2 scale, int order, float rotZ)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = LayerForOrder(order);
        sr.sortingOrder = order;
        return sr;
    }

    private static SpriteRenderer CreateTiled(Transform p, string n, Sprite sp, Vector2 pos, Vector2 size, int order)
    {
        GameObject go = new GameObject(n);
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingLayerName = LayerForOrder(order);
        sr.sortingOrder = order;
        return sr;
    }

    private static string LayerForOrder(int order)
    {
        if (order <= -12) return "Ground";
        if (order <= -3) return "GroundDetails";
        return "PropsAndVehicles";
    }

    private static void EnsureSortingLayers(params string[] layerNames)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
        if (sortingLayers == null) return;

        foreach (string layerName in layerNames)
        {
            bool exists = false;
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                SerializedProperty entry = sortingLayers.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("name").stringValue == layerName) { exists = true; break; }
            }
            if (exists) continue;
            int idx = sortingLayers.arraySize;
            sortingLayers.InsertArrayElementAtIndex(idx);
            SerializedProperty newEntry = sortingLayers.GetArrayElementAtIndex(idx);
            newEntry.FindPropertyRelative("name").stringValue = layerName;
            newEntry.FindPropertyRelative("uniqueID").intValue = Mathf.Abs(layerName.GetHashCode()) + idx + 101;
        }
        tagManager.ApplyModifiedProperties();
    }

    private static Sprite LoadArt(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + fileName);

    private static void TrySetTag(GameObject go, string tag)
    {
        try { go.tag = tag; } catch { }
    }
}
