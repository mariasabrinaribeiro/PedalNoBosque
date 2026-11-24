using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builder técnico unificado das Etapas 3 & 4 para "CarrinhoSorvete_NovoProjeto":
/// 1. Registra rigorosamente as 7 Sorting Layers:
///    Ground -> Track -> GroundDetails -> PropsAndBuildings -> EntitiesAndPlayer -> VFX -> UI.
/// 2. Padroniza TODOS os sprites 2D da fase para FilterMode = Point e PPU = 32.
/// 3. Limpa contêineres legados duplicados antes de reconstruir.
/// 4. Constrói pista 2D contínua com Grid + Tilemaps (GroundTilemap & TrackTilemap),
///    bordas nítidas de confeitos, 8 casas recortadas sem fundo bege, agrupamentos temáticos
///    de árvores/cercas e áreas sinalizadas de SlowZone e SpeedDeliveryGate.
/// 5. Redimensiona o carrinho para ocupar 60% da largura da via com sombra elíptica em GroundDetails.
/// </summary>
public static class GirlPowerCandyWorldBuilder
{
    private const string ReferenceScenePath = "Assets/Scenes/Girl power 1 - Sorvete - Referencia.unity";
    private const string WorkingScenePath = "Assets/Scenes/Girl power 1- Sorvete.unity";
    private const string CandyArtFolder = "Assets/GirlPower/CandyWorld/Art";
    private const string TilesFolder = "Assets/GirlPower/CandyWorld/Tiles";
    private const string CandyRootName = "CandyWorld_CenarioBonito";

    // PPU = 32 -> 256px road tile = 8.0 world units cellSize; drivable inner road width = 5.875 units!
    private const float TileWorldSize = 8.0f;
    private const float DrivableLaneWidth = 5.875f;

    [MenuItem("Girl Power/Rebuild Candy World (Cena Original Melhorada)")]
    public static void BuildCandyWorld()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl power 1 - Sorvete - Referencia.unity")))
        {
            Debug.LogError("Cena de referência não encontrada em " + ReferenceScenePath);
            return;
        }

        // 1. Garante exatamente a hierarquia de 7 Sorting Layers na ordem especificada
        EnsureExactSortingLayers(
            "Ground",
            "Track",
            "GroundDetails",
            "PropsAndBuildings",
            "EntitiesAndPlayer",
            "VFX",
            "UI");

        // 2. Gera todos os sprites Candy World + recortes limpos das 8 casas em Point + PPU 32
        CandyWorldArtGenerator.GenerateAndImportAll(CandyArtFolder);
        StandardizeExistingStageSpritesToPointPPU32();

        // 3. Restaura cópia limpa a partir da cena de referência (preservando a referência intacta)
        AssetDatabase.CopyAsset(ReferenceScenePath, WorkingScenePath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(WorkingScenePath, OpenSceneMode.Single);

        // 4. Limpeza completa de contêineres legados/protótipos antes de construir a fase
        GameObject carRoot = null;
        GameObject mainCamObj = null;
        List<GameObject> legacyToDestroy = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            DriverPro foundDriver = root.GetComponentInChildren<DriverPro>(true);
            if (foundDriver != null)
            {
                carRoot = foundDriver.gameObject;
                carRoot.transform.SetParent(null, true);
                carRoot.SetActive(true);
                if (root != carRoot) legacyToDestroy.Add(root);
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
            legacyToDestroy.Add(root);
        }

        foreach (GameObject old in legacyToDestroy)
            Object.DestroyImmediate(old);

        // 5. Limpa filhos legados da Main Camera e ajusta enquadramento
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
                cam.orthographicSize = 8.4f;
                cam.backgroundColor = new Color32(158, 226, 178, 255);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            mainCamObj.transform.position = new Vector3(0f, -8f, -10f);
            if (mainCamObj.GetComponent<GameCameraController>() == null)
                mainCamObj.AddComponent<GameCameraController>();
        }

        // 6. Configura o carrinho (60% da largura da via + camada EntitiesAndPlayer + sombra elíptica em GroundDetails)
        if (carRoot != null)
        {
            carRoot.transform.position = new Vector3(0f, -8f, 0f);
            carRoot.transform.rotation = Quaternion.identity;
            TrySetTag(carRoot, "Player");

            SpriteRenderer rootSr = carRoot.GetComponent<SpriteRenderer>();
            float targetVisualWidth = DrivableLaneWidth * 0.60f; // Exatamente 60% da largura útil da via!
            float spriteWidth = (rootSr != null && rootSr.sprite != null) ? rootSr.sprite.bounds.size.x : 16f;
            // O desenho da van ocupa ~36% da largura do canvas transparente 512x512 de icecreamvan.png
            float visibleBodyWidth = Mathf.Max(0.25f, spriteWidth * 0.36f);
            float scaleFor60Percent = Mathf.Clamp(targetVisualWidth / visibleBodyWidth, 0.25f, 2.5f);
            carRoot.transform.localScale = Vector3.one * scaleFor60Percent;

            if (rootSr != null)
            {
                rootSr.sortingLayerName = "EntitiesAndPlayer";
                rootSr.sortingOrder = 10;
            }

            Transform carroChild = carRoot.transform.Find("Carro1");
            if (carroChild != null)
            {
                carroChild.localPosition = Vector3.zero;
                carroChild.localRotation = Quaternion.identity;
                if (rootSr != null && rootSr.sprite != null)
                {
                    SpriteRenderer childSr = carroChild.GetComponent<SpriteRenderer>();
                    if (childSr != null) childSr.enabled = false;
                }
                foreach (Collider2D cc in carroChild.GetComponents<Collider2D>())
                    cc.enabled = false;
            }

            BoxCollider2D vanCol = carRoot.GetComponent<BoxCollider2D>();
            if (vanCol != null)
            {
                vanCol.offset = Vector2.zero;
                vanCol.size = new Vector2(visibleBodyWidth * 0.85f, visibleBodyWidth * 1.65f);
                vanCol.isTrigger = false;
            }

            // ETAPA 4: Sombra elíptica na camada GroundDetails sob o carrinho!
            Sprite shadowSprite = LoadCandy("CandyShadow.png");
            if (shadowSprite != null)
            {
                GameObject vanShadow = new GameObject("SombraEliptica_Carrinho");
                vanShadow.transform.SetParent(carRoot.transform, false);
                vanShadow.transform.localPosition = new Vector3(0.12f, -0.22f, 0f);
                vanShadow.transform.localScale = new Vector3(visibleBodyWidth * 0.32f, visibleBodyWidth * 0.88f, 1f);
                SpriteRenderer shSr = vanShadow.AddComponent<SpriteRenderer>();
                shSr.sprite = shadowSprite;
                shSr.sortingLayerName = "GroundDetails";
                shSr.sortingOrder = 8;
            }
        }

        // Garante gerenciadores essenciais
        if (Object.FindObjectOfType<MissionManager>() == null)
            new GameObject("MissionManager").AddComponent<MissionManager>();
        if (Object.FindObjectOfType<DynamicEnvironmentInitializer>() == null)
            new GameObject("DynamicEnvironmentInitializer").AddComponent<DynamicEnvironmentInitializer>();

        // 7. Constrói o cenário completo com Grid + Tilemaps + Props agrupados + SlowZone + SpeedDeliveryGate
        GameObject world = new GameObject(CandyRootName);
        List<Transform> swaying = new List<Transform>();
        List<SpriteRenderer> glows = new List<SpriteRenderer>();
        List<SpriteRenderer> sparkles = new List<SpriteRenderer>();
        List<Transform> pickups = new List<Transform>();

        BuildTilemapGroundAndTrack(world.transform);
        BuildDeliveryPlazas(world.transform, glows);
        BuildSlowZonesAndSpeedGates(world.transform);
        BuildCupcakeHousesAndGroupedProps(world.transform, swaying, glows, sparkles);
        BuildPickupsAndBoosts(world.transform, pickups);
        BuildPerimeterBoundary(world.transform);

        CandyWorldAmbient ambient = world.AddComponent<CandyWorldAmbient>();
        ambient.swayingProps = swaying.ToArray();
        ambient.pulsingGlows = glows.ToArray();
        ambient.floatingSparkles = sparkles.ToArray();
        ambient.bobbingPickups = pickups.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, WorkingScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power (Etapas 3 & 4): Cena reconstruída com Grid/Tilemap contínuo, 7 Sorting Layers, sprites Point/PPU=32, SlowZones, SpeedDeliveryGates, 8 casas recortadas e carrinho a 60% da via com sombra em GroundDetails.");
    }

    // =========================================================================
    // ETAPA 3: PISTA CONTÍNUA COM UNITY GRID + TILEMAP (Ground + Track)
    // =========================================================================
    private static void BuildTilemapGroundAndTrack(Transform root)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, TilesFolder.Replace("Assets/", "")));

        Tile meadowTile = CreateOrUpdateTile("Tile_Meadow.asset", LoadCandy("CandyMeadowTile.png"), 0f);
        Tile roadH = CreateOrUpdateTile("Tile_RoadH.asset", LoadCandy("CandyRoadH.png"), 0f);
        Tile roadV = CreateOrUpdateTile("Tile_RoadV.asset", LoadCandy("CandyRoadV.png"), 0f);
        Tile roadCross = CreateOrUpdateTile("Tile_RoadCross.asset", LoadCandy("CandyRoadCross.png"), 0f);
        Tile cornerSW = CreateOrUpdateTile("Tile_CornerSW.asset", LoadCandy("CandyRoadCorner.png"), 0f);
        Tile cornerSE = CreateOrUpdateTile("Tile_CornerSE.asset", LoadCandy("CandyRoadCorner.png"), 90f);
        Tile cornerNE = CreateOrUpdateTile("Tile_CornerNE.asset", LoadCandy("CandyRoadCorner.png"), 180f);
        Tile cornerNW = CreateOrUpdateTile("Tile_CornerNW.asset", LoadCandy("CandyRoadCorner.png"), 270f);
        AssetDatabase.SaveAssets();

        GameObject gridObj = new GameObject("CandyWorld_Grid");
        gridObj.transform.SetParent(root, false);
        // Offset by (-4, -4) so cell (0,0) center is at world (0,0)!
        gridObj.transform.localPosition = new Vector3(-TileWorldSize * 0.5f, -TileWorldSize * 0.5f, 0f);
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 0f);

        // Layer 1: Ground Tilemap (Pastel Candy Meadow)
        GameObject groundObj = new GameObject("GroundTilemap");
        groundObj.transform.SetParent(gridObj.transform, false);
        Tilemap groundMap = groundObj.AddComponent<Tilemap>();
        TilemapRenderer groundRen = groundObj.AddComponent<TilemapRenderer>();
        groundRen.sortingLayerName = "Ground";
        groundRen.sortingOrder = 0;

        for (int y = -6; y <= 6; y++)
            for (int x = -7; x <= 7; x++)
                groundMap.SetTile(new Vector3Int(x, y, 0), meadowTile);

        // Layer 2: Track Tilemap (Continuous Candy Biscuit Loop + Central Crossroads!)
        // Grid coordinates: x in [-3..+3] (-24..+24 world), y in [-2..+2] (-16..+16 world)
        GameObject trackObj = new GameObject("TrackTilemap");
        trackObj.transform.SetParent(gridObj.transform, false);
        Tilemap trackMap = trackObj.AddComponent<Tilemap>();
        TilemapRenderer trackRen = trackObj.AddComponent<TilemapRenderer>();
        trackRen.sortingLayerName = "Track";
        trackRen.sortingOrder = 0;

        // Horizontal boulevards (y = -2, 0, +2)
        for (int x = -2; x <= 2; x++)
        {
            if (x == 0) continue;
            trackMap.SetTile(new Vector3Int(x, 0, 0), roadH);
            trackMap.SetTile(new Vector3Int(x, 2, 0), roadH);
            trackMap.SetTile(new Vector3Int(x, -2, 0), roadH);
        }

        // Vertical avenues (x = -3, 0, +3)
        trackMap.SetTile(new Vector3Int(0, -1, 0), roadV);
        trackMap.SetTile(new Vector3Int(0, 1, 0), roadV);
        trackMap.SetTile(new Vector3Int(-3, -1, 0), roadV);
        trackMap.SetTile(new Vector3Int(-3, 1, 0), roadV);
        trackMap.SetTile(new Vector3Int(3, -1, 0), roadV);
        trackMap.SetTile(new Vector3Int(3, 1, 0), roadV);

        // Intersections
        trackMap.SetTile(new Vector3Int(0, 0, 0), roadCross);
        trackMap.SetTile(new Vector3Int(0, 2, 0), roadCross);
        trackMap.SetTile(new Vector3Int(0, -2, 0), roadCross);
        trackMap.SetTile(new Vector3Int(-3, 0, 0), roadCross);
        trackMap.SetTile(new Vector3Int(3, 0, 0), roadCross);

        // Smooth rounded corners
        trackMap.SetTile(new Vector3Int(-3, -2, 0), cornerSW);
        trackMap.SetTile(new Vector3Int(3, -2, 0), cornerSE);
        trackMap.SetTile(new Vector3Int(3, 2, 0), cornerNE);
        trackMap.SetTile(new Vector3Int(-3, 2, 0), cornerNW);
    }

    private static Tile CreateOrUpdateTile(string assetName, Sprite sprite, float rotZ)
    {
        string path = TilesFolder + "/" + assetName;
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.color = Color.white;
        tile.transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, rotZ), Vector3.one);
        tile.flags = TileFlags.LockTransform;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void BuildDeliveryPlazas(Transform root, List<SpriteRenderer> glows)
    {
        Transform plazas = CreateGroup(root, "02_Pracas_de_Entrega");
        Sprite plazaSprite = LoadCandy("CandyDeliveryPlaza.png");
        Sprite glowSprite = LoadCandy("CandyGlow.png");

        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Central", plazaSprite, glowSprite, new Vector2(0f, 0f), glows);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Leste", plazaSprite, glowSprite, new Vector2(24f, 0f), glows);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Oeste", plazaSprite, glowSprite, new Vector2(-24f, 0f), glows);
    }

    private static void CreateDeliveryPlaza(Transform parent, string name, Sprite plazaSprite, Sprite glowSprite, Vector2 pos, List<SpriteRenderer> glows)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * 0.45f; // 512px / 32 PPU = 16 -> 0.45 = 7.2 world units!
        TrySetTag(go, "Customer");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = plazaSprite;
        sr.sortingLayerName = "GroundDetails";
        sr.sortingOrder = 1;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 7.2f;

        SpriteRenderer g = CreateSprite(go.transform, "BrilhoPraca", glowSprite, Vector2.zero, Vector2.one * 3.8f, "VFX", 1, 0f);
        g.color = new Color(1f, 0.72f, 0.88f, 0.30f);
        glows.Add(g);
    }

    // =========================================================================
    // ETAPA 3: SINALIZAÇÃO CLARA DE SLOWZONE E SPEEDDELIVERYGATE NA PISTA
    // =========================================================================
    private static void BuildSlowZonesAndSpeedGates(Transform root)
    {
        Transform zones = CreateGroup(root, "03_SlowZones_e_SpeedGates");

        // 2 SlowZones permanentes (calda de caramelo/chocolate em GroundDetails) em trechos estratégicos
        Vector2[] slowSpots = { new Vector2(-12f, 16f), new Vector2(12f, -16f) };
        for (int i = 0; i < slowSpots.Length; i++)
        {
            GameObject sz = new GameObject("SlowZone_Caramelo_" + (i + 1));
            sz.transform.SetParent(zones, false);
            sz.transform.position = new Vector3(slowSpots[i].x, slowSpots[i].y, 0f);
            sz.transform.localScale = new Vector3(2.0f, 1.5f, 1f);
            SlowZone script = sz.AddComponent<SlowZone>();
            script.isPermanentZone = true;
        }

        // 2 SpeedDeliveryGates sobre as retas principais (postes em PropsAndBuildings + feixe em VFX)
        Vector2[] gateSpots = { new Vector2(12f, 0f), new Vector2(-12f, 0f) };
        for (int i = 0; i < gateSpots.Length; i++)
        {
            GameObject gate = new GameObject("SpeedDeliveryGate_" + (i + 1));
            gate.transform.SetParent(zones, false);
            gate.transform.position = new Vector3(gateSpots[i].x, gateSpots[i].y, 0f);
            gate.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // Alinhado transversalmente à avenida horizontal!
            gate.AddComponent<SpeedDeliveryGate>();
        }
    }

    private static void BuildCupcakeHousesAndGroupedProps(
        Transform root,
        List<Transform> swaying,
        List<SpriteRenderer> glows,
        List<SpriteRenderer> sparkles)
    {
        Transform houses = CreateGroup(root, "04_Casas_e_Cenario_Agrupado");

        // 8 casas cupcake recortadas (PPU=32 -> ~12-16 world units -> escala 0.36 = ~5.2 units harmônicos!)
        PlaceHouse(houses, "Casa_Morango", "CandyHouse_Strawberry.png", new Vector2(-11.5f, 7.6f), 0.38f);
        PlaceHouse(houses, "Casa_ArcoIris", "CandyHouse_Rainbow.png", new Vector2(11.5f, 7.6f), 0.38f);
        PlaceHouse(houses, "Casa_FogueteAzul", "CandyHouse_RocketBlue.png", new Vector2(-11.5f, -7.8f), 0.36f);
        PlaceHouse(houses, "Casa_ChocoMenta", "CandyHouse_ChocoMint.png", new Vector2(11.5f, -7.8f), 0.38f);
        PlaceHouse(houses, "Casa_EstufaBotanica", "CandyHouse_Botanical.png", new Vector2(-12f, 22.8f), 0.36f);
        PlaceHouse(houses, "Casa_Matcha", "CandyHouse_Matcha.png", new Vector2(12f, 22.8f), 0.38f);
        PlaceHouse(houses, "Casa_Galaxia", "CandyHouse_Galaxy.png", new Vector2(-12f, -22.8f), 0.38f);
        PlaceHouse(houses, "Casa_Nautica", "CandyHouse_Nautical.png", new Vector2(12f, -22.8f), 0.38f);

        // Lagoas de soda na camada GroundDetails
        Sprite pond = LoadCandy("CandyBerryPond.png");
        SpriteRenderer pond1 = CreateSprite(houses, "LagoaSoda_NO", pond, new Vector2(-18.2f, 8.0f), Vector2.one * 0.42f, "GroundDetails", 0, 0f);
        CapsuleCollider2D pc1 = pond1.gameObject.AddComponent<CapsuleCollider2D>();
        pc1.direction = CapsuleDirection2D.Horizontal; pc1.size = new Vector2(13f, 7f);

        SpriteRenderer pond2 = CreateSprite(houses, "LagoaSoda_SE", pond, new Vector2(18.2f, -8.0f), Vector2.one * 0.42f, "GroundDetails", 0, 0f);
        CapsuleCollider2D pc2 = pond2.gameObject.AddComponent<CapsuleCollider2D>();
        pc2.direction = CapsuleDirection2D.Horizontal; pc2.size = new Vector2(13f, 7f);

        // Agrupamentos temáticos de árvores de algodão-doce e cercas de wafer em PropsAndBuildings
        Sprite treePink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/download__2_-removebg-preview.png");
        Sprite treePine = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (67).png");
        Sprite wafer = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (63).png");
        Sprite lolliA = LoadCandy("GiantLollipopA.png");
        Sprite lolliB = LoadCandy("GiantLollipopB.png");
        Sprite lamp = LoadCandy("CandyLampPost.png");
        Sprite flower = LoadCandy("SugarFlowerBed.png");
        Sprite glow = LoadCandy("CandyGlow.png");
        Sprite sparkle = LoadCandy("CandySparkle.png");

        // Grupos de 2-3 árvores abraçando cada casa (sem ilhas soltas aleatórias!)
        Vector2[] treeClusters =
        {
            new Vector2(-7.2f, 8.6f), new Vector2(-15.2f, 10.2f),
            new Vector2(7.2f, 8.6f), new Vector2(15.6f, 9.6f),
            new Vector2(-7.2f, -8.6f), new Vector2(-15.6f, -9.2f),
            new Vector2(7.2f, -8.6f), new Vector2(15.2f, -10.8f),
            new Vector2(-17.8f, 22.8f), new Vector2(-6.8f, 22.8f),
            new Vector2(6.8f, 22.8f), new Vector2(17.8f, 22.8f),
            new Vector2(-17.8f, -22.8f), new Vector2(-6.8f, -22.8f),
            new Vector2(6.8f, -22.8f), new Vector2(17.8f, -22.8f)
        };
        for (int i = 0; i < treeClusters.Length; i++)
        {
            Sprite ts = (i % 2 == 0) ? treePink : treePine;
            if (ts == null) continue;
            float sc = NormalizeSpriteScale(ts, 3.6f);
            SpriteRenderer sr = CreateSprite(houses, "ArvoreAgrupada_" + (i + 1), ts, treeClusters[i], Vector2.one * sc, "PropsAndBuildings", 4, 0f);
            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = (ts.bounds.size.x * 0.20f);
            swaying.Add(sr.transform);
        }

        // Cercas de wafer agrupadas em duplas delimitando os quintais das 4 casas centrais
        if (wafer != null)
        {
            float fenceSc = NormalizeSpriteScale(wafer, 3.2f);
            Vector2[] fences =
            {
                new Vector2(-13.2f, 4.4f), new Vector2(-9.8f, 4.4f),
                new Vector2(9.8f, 4.4f), new Vector2(13.2f, 4.4f),
                new Vector2(-13.2f, -4.4f), new Vector2(-9.8f, -4.4f),
                new Vector2(9.8f, -4.4f), new Vector2(13.2f, -4.4f)
            };
            for (int i = 0; i < fences.Length; i++)
            {
                SpriteRenderer sr = CreateSprite(houses, "CercaQuintal_" + (i + 1), wafer, fences[i], Vector2.one * fenceSc, "PropsAndBuildings", 3, 0f);
                BoxCollider2D bc = sr.gameObject.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(wafer.bounds.size.x * 0.85f, wafer.bounds.size.y * 0.45f);
            }
        }

        // Pirulitos e postes luminosos emoldurando a Praça Central e os cruzamentos
        Vector2[] lollis = { new Vector2(-4.8f, 4.8f), new Vector2(4.8f, 4.8f), new Vector2(-4.8f, -4.8f), new Vector2(4.8f, -4.8f) };
        for (int i = 0; i < lollis.Length; i++)
        {
            SpriteRenderer sr = CreateSprite(houses, "PirulitoMarco_" + (i + 1), i % 2 == 0 ? lolliA : lolliB, lollis[i], Vector2.one * 0.34f, "PropsAndBuildings", 5, 0f);
            swaying.Add(sr.transform);
        }

        Vector2[] lamps = { new Vector2(-4.5f, 11.2f), new Vector2(4.5f, 11.2f), new Vector2(-4.5f, -11.2f), new Vector2(4.5f, -11.2f) };
        for (int i = 0; i < lamps.Length; i++)
        {
            CreateSprite(houses, "PosteDoce_" + (i + 1), lamp, lamps[i], Vector2.one * 0.32f, "PropsAndBuildings", 5, 0f);
            SpriteRenderer g = CreateSprite(houses, "LuzPoste_" + (i + 1), glow, lamps[i] + new Vector2(0f, 1.0f), Vector2.one * 0.85f, "VFX", 2, 0f);
            g.color = new Color(1f, 0.90f, 0.62f, 0.32f);
            glows.Add(g);
        }

        Vector2[] flowers = { new Vector2(-11.5f, 11.2f), new Vector2(11.5f, 11.2f), new Vector2(-11.5f, -11.2f), new Vector2(11.5f, -11.2f) };
        for (int i = 0; i < flowers.Length; i++)
            CreateSprite(houses, "Canteiro_" + (i + 1), flower, flowers[i], Vector2.one * 0.38f, "GroundDetails", 3, 0f);

        // Partículas sutis (opacidade <= 35% na camada VFX)
        for (int i = 0; i < 12; i++)
        {
            float sx = -24f + (i * 11.7f % 48f);
            float sy = -16f + (i * 7.3f % 32f);
            SpriteRenderer sp = CreateSprite(houses, "BrilhoVFX_" + (i + 1), sparkle, new Vector2(sx, sy), Vector2.one * 0.32f, "VFX", 3, 0f);
            sp.color = new Color(1f, 0.88f, 0.96f, 0.34f);
            sparkles.Add(sp);
        }
    }

    private static void PlaceHouse(Transform parent, string name, string spriteFile, Vector2 pos, float scale)
    {
        Sprite sp = LoadCandy(spriteFile);
        if (sp == null) return;
        SpriteRenderer sr = CreateSprite(parent, name, sp, pos, Vector2.one * scale, "PropsAndBuildings", 2, 0f);
        BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(sp.bounds.size.x * 0.72f, sp.bounds.size.y * 0.56f);
        col.offset = new Vector2(0f, -sp.bounds.size.y * 0.12f);
    }

    private static void BuildPickupsAndBoosts(Transform root, List<Transform> pickups)
    {
        Transform items = CreateGroup(root, "05_Itens_e_Boosts");
        Sprite donutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (77).png");
        Sprite boostSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameDev.tv - Delivery Dash Assets - cópia/Props & Stuff/pickupspeed.png");

        float donutSc = donutSprite != null ? NormalizeSpriteScale(donutSprite, 1.8f) : 0.25f;
        float boostSc = boostSprite != null ? NormalizeSpriteScale(boostSprite, 2.0f) : 0.6f;

        Vector2[] packagePositions =
        {
            new Vector2(-8f, 0f), new Vector2(-18f, 0f), new Vector2(8f, 0f), new Vector2(18f, 0f),
            new Vector2(0f, 8f), new Vector2(0f, 16f), new Vector2(0f, -16f),
            new Vector2(-16f, 16f), new Vector2(16f, 16f), new Vector2(-16f, -16f), new Vector2(16f, -16f),
            new Vector2(-24f, 8f), new Vector2(-24f, -8f), new Vector2(24f, 8f), new Vector2(24f, -8f)
        };
        for (int i = 0; i < packagePositions.Length; i++)
        {
            GameObject go = new GameObject("pickupitem (" + (i + 1) + ")");
            go.transform.SetParent(items, false);
            go.transform.position = new Vector3(packagePositions[i].x, packagePositions[i].y, 0f);
            go.transform.localScale = Vector3.one * donutSc;
            TrySetTag(go, "package");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = donutSprite;
            sr.sortingLayerName = "EntitiesAndPlayer";
            sr.sortingOrder = 2;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = donutSprite != null ? donutSprite.bounds.size.x * 0.35f : 1.0f;
            pickups.Add(go.transform);
        }

        Vector2[] boostPositions = { new Vector2(0f, 12f), new Vector2(0f, -12f), new Vector2(24f, 12f), new Vector2(-24f, -12f) };
        for (int i = 0; i < boostPositions.Length; i++)
        {
            GameObject go = new GameObject("Boost item (" + (i + 1) + ")");
            go.transform.SetParent(items, false);
            go.transform.position = new Vector3(boostPositions[i].x, boostPositions[i].y, 0f);
            go.transform.localScale = Vector3.one * boostSc;
            TrySetTag(go, "Boost item");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = boostSprite;
            sr.sortingLayerName = "GroundDetails";
            sr.sortingOrder = 4;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = boostSprite != null ? boostSprite.bounds.size.x * 0.42f : 0.85f;
        }
    }

    private static void BuildPerimeterBoundary(Transform root)
    {
        Transform bounds = CreateGroup(root, "06_Limites_do_Mapa");
        CreateWall(bounds, "Limite_Norte", new Vector2(0f, 27.5f), new Vector2(72f, 2f));
        CreateWall(bounds, "Limite_Sul", new Vector2(0f, -27.5f), new Vector2(72f, 2f));
        CreateWall(bounds, "Limite_Oeste", new Vector2(-33.5f, 0f), new Vector2(2f, 58f));
        CreateWall(bounds, "Limite_Leste", new Vector2(33.5f, 0f), new Vector2(2f, 58f));
    }

    private static void StandardizeExistingStageSpritesToPointPPU32()
    {
        string[] stageSprites =
        {
            "Assets/Resources/icecreamvan.png",
            "Assets/Resources/monster.png",
            "Assets/a casa de mosntros/download__2_-removebg-preview.png",
            "Assets/a casa de mosntros/background-removed (67).png",
            "Assets/a casa de mosntros/background-removed (63).png",
            "Assets/a casa de mosntros/background-removed (77).png",
            "Assets/GameDev.tv - Delivery Dash Assets - cópia/Props & Stuff/pickupspeed.png"
        };
        foreach (string path in stageSprites)
        {
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    private static void EnsureExactSortingLayers(params string[] orderedLayers)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
        if (sortingLayers == null) return;

        // Keep Default at index 0, then enforce the exact 7 layers in exact order
        while (sortingLayers.arraySize > 1)
            sortingLayers.DeleteArrayElementAtIndex(sortingLayers.arraySize - 1);

        for (int i = 0; i < orderedLayers.Length; i++)
        {
            int idx = sortingLayers.arraySize;
            sortingLayers.InsertArrayElementAtIndex(idx);
            SerializedProperty entry = sortingLayers.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("name").stringValue = orderedLayers[i];
            entry.FindPropertyRelative("uniqueID").intValue = 1000 + i * 17;
        }
        tagManager.ApplyModifiedProperties();
    }

    private static float NormalizeSpriteScale(Sprite sp, float desiredWorldWidth)
    {
        if (sp == null || sp.bounds.size.x <= 0.001f) return 1f;
        return desiredWorldWidth / sp.bounds.size.x;
    }

    private static void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.AddComponent<BoxCollider2D>().size = size;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 scale, string sortingLayer, int order, float rotZ)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    private static Sprite LoadCandy(string fileName) => AssetDatabase.LoadAssetAtPath<Sprite>(CandyArtFolder + "/" + fileName);

    private static void TrySetTag(GameObject go, string tagName)
    {
        try { go.tag = tagName; } catch { }
    }
}
