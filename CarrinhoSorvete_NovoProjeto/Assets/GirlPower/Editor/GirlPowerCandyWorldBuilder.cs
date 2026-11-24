using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Builder técnico unificado para "CarrinhoSorvete_NovoProjeto" (Unity 2022.3):
/// 1. Registra exatamente as 7 Sorting Layers na ordem correta:
///    Ground -> Track -> GroundDetails -> PropsAndBuildings -> EntitiesAndPlayer -> VFX -> UI.
/// 2. Padroniza TODOS os sprites 2D da fase para FilterMode = Point e PPU = 32.
/// 3. Limpa contêineres legados/protótipos antes de reconstruir a cena.
/// 4. Constrói malha viária contínua com Grid + Tilemaps (GroundTilemap & TrackTilemap) em
///    tiles 192x192 (6.0 x 6.0 world units, faixa útil de biscoito = 4.3125 units),
///    usando entroncamentos em T (CandyRoadTee) e curvas suaves sem calçada cortando pista.
/// 5. Corrige a invisibilidade do carrinho (força rootSr.enabled = true), aplica escala
///    proporcional calibrada pelo bounding box opaco real (~1.95 x 3.72 units = 45% da via)
///    com sombra elíptica em GroundDetails, Y-sorting em todas as casas/árvores/props,
///    sombra de contato nas 8 casas recortadas, calçadinha ligando a Casa Arco-Íris à rua,
///    SlowZones de caramelo, SpeedDeliveryGates integrados à largura da faixa e paralaxe aérea.
/// </summary>
public static class GirlPowerCandyWorldBuilder
{
    private const string ReferenceScenePath = "Assets/Scenes/Girl power 1 - Sorvete - AntesRecomposicao.unity";
    private const string WorkingScenePath = "Assets/Scenes/Girl Power - Cenario Refinado.unity";
    private const string CandyArtFolder = "Assets/GirlPower/CandyWorld/Art";
    private const string TilesFolder = "Assets/GirlPower/CandyWorld/Tiles";
    private const string CandyRootName = "CandyWorld_CenarioBonito";

    // PPU = 32 -> 192px tile = 6.0 world units cellSize; pista interna dirigível = 138px = 4.3125 world units!
    private const float TileWorldSize = 6.0f;
    private const float DrivableLaneWidth = 4.3125f;

    [MenuItem("Girl Power/Rebuild Candy World (Cenario Refinado)")]
    public static void BuildCandyWorld()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl power 1 - Sorvete - Referencia.unity")))
        {
            Debug.LogError("Cena de referência não encontrada em " + ReferenceScenePath);
            return;
        }

        // 1. Garante exatamente as 7 Sorting Layers na ordem exigida
        EnsureExactSortingLayers(
            "Ground",
            "Track",
            "GroundDetails",
            "PropsAndBuildings",
            "EntitiesAndPlayer",
            "VFX",
            "UI");

        // 2. Gera todos os sprites Candy World em pixel art nítida (Point, PPU = 32)
        CandyWorldArtGenerator.GenerateAndImportAll(CandyArtFolder);
        StandardizeExistingStageSpritesToPointPPU32();

        // 3. Restaura cópia limpa da cena de referência (preservando Referencia.unity intacta)
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl Power - Cenario Refinado.unity")))
        {
            if (!AssetDatabase.CopyAsset(ReferenceScenePath, WorkingScenePath))
            {
                Debug.LogError("Nao foi possivel criar a copia de trabalho: " + WorkingScenePath);
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        Scene scene = EditorSceneManager.OpenScene(WorkingScenePath, OpenSceneMode.Single);

        // 4. Limpa completamente contêineres legados/protótipos preservando apenas player, câmera e gerenciadores
        GameObject carRoot = null;
        GameObject mainCamObj = null;
        List<GameObject> legacyToDestroy = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CandyRootName)
            {
                legacyToDestroy.Add(root);
                continue;
            }
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
                root.GetComponent<ControladorJogo>() != null || root.GetComponent<AudioSource>() != null ||
                root.GetComponent<EnemySpawner>() != null || root.GetComponent<ObjectPoolManager>() != null)
            {
                root.SetActive(true);
                continue;
            }
            legacyToDestroy.Add(root);
        }

        foreach (GameObject old in legacyToDestroy)
            Object.DestroyImmediate(old);

        // 5. Enquadramento limpo da Main Camera (vista de cima focada no carrinho e na Casa Arco-Íris)
        Vector3 startPos = new Vector3(0f, -18.0f, 0f);
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
                cam.orthographicSize = 8.8f;
                cam.backgroundColor = new Color32(158, 224, 178, 255);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            mainCamObj.transform.position = new Vector3(startPos.x, startPos.y + 3.5f, -10f);
            if (mainCamObj.GetComponent<GameCameraController>() == null)
                mainCamObj.AddComponent<GameCameraController>();
        }

        // 6. Configura o carrinho (CORRIGE INVISIBILIDADE + escala 1.7x proporcional à via + sombra em GroundDetails)
        if (carRoot != null)
        {
            carRoot.transform.position = startPos;
            carRoot.transform.rotation = Quaternion.identity;
            TrySetTag(carRoot, "Player");

            SpriteRenderer rootSr = carRoot.GetComponent<SpriteRenderer>();
            if (rootSr == null) rootSr = carRoot.AddComponent<SpriteRenderer>();
            if (rootSr.sprite == null)
                rootSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/icecreamvan.png");

            // CORREÇÃO CRÍTICA: Na cena Referencia.unity, carro tinha m_Enabled = 0 (ficava invisível!)
            rootSr.enabled = true;
            rootSr.color = Color.white;
            rootSr.sortingLayerName = "EntitiesAndPlayer";
            rootSr.sortingOrder = 10;

            // Medição real do bounding box opaco de icecreamvan.png: corpo ocupa 30.9% da largura e 59.0% da altura
            float targetVisibleWidth = 2.12f;  // ~50% da pista dirigível (4.31 units) -> protagonismo claro e curvas folgadas!
            float spriteCanvasW = (rootSr.sprite != null && rootSr.sprite.bounds.size.x > 0.01f) ? rootSr.sprite.bounds.size.x : 8f;
            float spriteCanvasH = (rootSr.sprite != null && rootSr.sprite.bounds.size.y > 0.01f) ? rootSr.sprite.bounds.size.y : 8f;
            float visibleBodyWidthInLocal = spriteCanvasW * 0.309f;
            float visibleBodyHeightInLocal = spriteCanvasH * 0.590f;
            float vanScale = Mathf.Clamp(targetVisibleWidth / visibleBodyWidthInLocal, 0.20f, 3.0f);
            carRoot.transform.localScale = Vector3.one * vanScale;

            // Desativa duplicata visual Carro1 (já renderizamos no rootSr habilitado)
            Transform carroChild = carRoot.transform.Find("Carro1");
            if (carroChild != null)
            {
                carroChild.localPosition = Vector3.zero;
                carroChild.localRotation = Quaternion.identity;
                SpriteRenderer childSr = carroChild.GetComponent<SpriteRenderer>();
                if (childSr != null) childSr.enabled = false;
                foreach (Collider2D cc in carroChild.GetComponents<Collider2D>())
                    cc.enabled = false;
            }

            // Alinha o colisor exatamente ao corpo visível da van
            BoxCollider2D vanCol = carRoot.GetComponent<BoxCollider2D>();
            if (vanCol != null)
            {
                vanCol.offset = Vector2.zero;
                vanCol.size = new Vector2(visibleBodyWidthInLocal * 0.86f, visibleBodyHeightInLocal * 0.88f);
                vanCol.isTrigger = false;
            }

            // Remove sombra antiga se existir e cria sombra elíptica calibrada em GroundDetails
            Transform oldShadow = carRoot.transform.Find("SombraEliptica_Carrinho");
            if (oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);

            Sprite shadowSprite = LoadCandy("CandyShadow.png");
            if (shadowSprite != null)
            {
                GameObject vanShadow = new GameObject("SombraEliptica_Carrinho");
                vanShadow.transform.SetParent(carRoot.transform, false);
                vanShadow.transform.localPosition = new Vector3(0.08f / vanScale, -0.18f / vanScale, 0f);
                float shW = (targetVisibleWidth * 1.12f) / (Mathf.Max(0.01f, shadowSprite.bounds.size.x) * vanScale);
                float shH = ((visibleBodyHeightInLocal * vanScale) * 0.92f) / (Mathf.Max(0.01f, shadowSprite.bounds.size.y) * vanScale);
                vanShadow.transform.localScale = new Vector3(shW, shH, 1f);
                SpriteRenderer shSr = vanShadow.AddComponent<SpriteRenderer>();
                shSr.sprite = shadowSprite;
                shSr.color = new Color(1f, 1f, 1f, 0.72f);
                shSr.sortingLayerName = "GroundDetails";
                shSr.sortingOrder = 8;
            }
        }

        // Garante gerenciadores essenciais
        if (Object.FindObjectOfType<MissionManager>() == null)
            new GameObject("MissionManager").AddComponent<MissionManager>();
        if (Object.FindObjectOfType<DynamicEnvironmentInitializer>() == null)
            new GameObject("DynamicEnvironmentInitializer").AddComponent<DynamicEnvironmentInitializer>();

        // 7. Constrói o cenário Candy World vivo com marcos infantis, bichinhos doces, WobbleOnPass e moldura de parque
        GameObject world = new GameObject(CandyRootName);
        List<Transform> swaying = new List<Transform>();
        List<SpriteRenderer> glows = new List<SpriteRenderer>();
        List<SpriteRenderer> sparkles = new List<SpriteRenderer>();
        List<Transform> pickups = new List<Transform>();
        List<Transform> beacons = new List<Transform>();
        List<Transform> critters = new List<Transform>();
        List<Transform> parallaxClouds = new List<Transform>();

        BuildTilemapGroundAndTrack(world.transform);
        BuildDeliveryPlazas(world.transform, glows, beacons, critters);
        BuildSlowZonesAndSpeedGates(world.transform);
        BuildCupcakeHousesAndGroupedProps(world.transform, swaying, glows, sparkles, critters);
        BuildPickupsAndBoosts(world.transform, pickups);
        BuildParallaxSkyLayer(world.transform, parallaxClouds);
        BuildPerimeterBoundary(world.transform, swaying);

        CandyWorldAmbient ambient = world.AddComponent<CandyWorldAmbient>();
        ambient.swayingProps = swaying.ToArray();
        ambient.pulsingGlows = glows.ToArray();
        ambient.floatingSparkles = sparkles.ToArray();
        ambient.bobbingPickups = pickups.ToArray();
        ambient.bobbingBeacons = beacons.ToArray();
        ambient.cheeringCritters = critters.ToArray();
        ambient.parallaxClouds = parallaxClouds.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, WorkingScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: Cena reconstruída com carrinho visível e calibrado, malha 6x6 com T-junctions limpos, Y-sorting, paralaxe aérea, Casa Arco-Íris conectada à pista, SlowZones e SpeedDeliveryGates temáticos.");
    }

    // =========================================================================
    // 1. PISTA CONTÍNUA DE BISCOITO & GRAMADO MENTA PIXEL ART (Grid + Tilemap)
    //    Malha 2x2 de quarteirões espaçosos (12 x 12 world units livres cada!)
    //    Eixos viários: x = -18, 0, +18 e y = -18, 0, +18
    // =========================================================================
    private static void BuildTilemapGroundAndTrack(Transform root)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, TilesFolder.Replace("Assets/", "")));

        Tile meadowTile = CreateOrUpdateTile("Tile_Meadow.asset", LoadCandy("CandyMeadowTile.png"), 0f);
        Tile roadH = CreateOrUpdateTile("Tile_RoadH.asset", LoadCandy("CandyRoadH.png"), 0f);
        Tile roadV = CreateOrUpdateTile("Tile_RoadV.asset", LoadCandy("CandyRoadV.png"), 0f);
        Tile roadCross = CreateOrUpdateTile("Tile_RoadCross.asset", LoadCandy("CandyRoadCross.png"), 0f);
        Tile teeNorth = CreateOrUpdateTile("Tile_TeeNorth.asset", LoadCandy("CandyRoadTee.png"), 0f);
        Tile teeWest = CreateOrUpdateTile("Tile_TeeWest.asset", LoadCandy("CandyRoadTee.png"), 90f);
        Tile teeSouth = CreateOrUpdateTile("Tile_TeeSouth.asset", LoadCandy("CandyRoadTee.png"), 180f);
        Tile teeEast = CreateOrUpdateTile("Tile_TeeEast.asset", LoadCandy("CandyRoadTee.png"), 270f);
        Tile cornerSW = CreateOrUpdateTile("Tile_CornerSW.asset", LoadCandy("CandyRoadCorner.png"), 0f);
        Tile cornerSE = CreateOrUpdateTile("Tile_CornerSE.asset", LoadCandy("CandyRoadCorner.png"), 90f);
        Tile cornerNE = CreateOrUpdateTile("Tile_CornerNE.asset", LoadCandy("CandyRoadCorner.png"), 180f);
        Tile cornerNW = CreateOrUpdateTile("Tile_CornerNW.asset", LoadCandy("CandyRoadCorner.png"), 270f);
        AssetDatabase.SaveAssets();

        GameObject gridObj = new GameObject("CandyWorld_Grid");
        gridObj.transform.SetParent(root, false);
        gridObj.transform.localPosition = new Vector3(-TileWorldSize * 0.5f, -TileWorldSize * 0.5f, 0f);
        Grid grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 0f);

        // Layer 1: Ground Tilemap (Gramado menta pixel art contínuo, sem borrões nem faixas cinzas)
        GameObject groundObj = new GameObject("GroundTilemap");
        groundObj.transform.SetParent(gridObj.transform, false);
        Tilemap groundMap = groundObj.AddComponent<Tilemap>();
        TilemapRenderer groundRen = groundObj.AddComponent<TilemapRenderer>();
        groundRen.sortingLayerName = "Ground";
        groundRen.sortingOrder = 0;

        for (int y = -8; y <= 8; y++)
            for (int x = -10; x <= 10; x++)
                groundMap.SetTile(new Vector3Int(x, y, 0), meadowTile);

        // Layer 2: Track Tilemap (Anel externo em ±3 + avenidas centrais em 0 = blocos 12x12!)
        GameObject trackObj = new GameObject("TrackTilemap");
        trackObj.transform.SetParent(gridObj.transform, false);
        Tilemap trackMap = trackObj.AddComponent<Tilemap>();
        TilemapRenderer trackRen = trackObj.AddComponent<TilemapRenderer>();
        trackRen.sortingLayerName = "Track";
        trackRen.sortingOrder = 0;

        // Avenidas horizontais (y = -3, 0, +3; x = -2, -1, +1, +2)
        int[] hRows = { -3, 0, 3 };
        foreach (int ry in hRows)
        {
            for (int x = -2; x <= 2; x++)
            {
                if (x == 0) continue;
                trackMap.SetTile(new Vector3Int(x, ry, 0), roadH);
            }
        }

        // Avenidas verticais (x = -3, 0, +3; y = -2, -1, +1, +2)
        int[] vCols = { -3, 0, 3 };
        foreach (int cx in vCols)
        {
            for (int y = -2; y <= 2; y++)
            {
                if (y == 0) continue;
                trackMap.SetTile(new Vector3Int(cx, y, 0), roadV);
            }
        }

        // Rua transversal no quarteirao inicial: liga a saida as casas Azul e Arco-iris.
        // As tres juncoes usam cruzamentos completos para manter a pista continua.
        for (int x = -2; x <= 2; x++)
            trackMap.SetTile(new Vector3Int(x, -1, 0), roadH);
        trackMap.SetTile(new Vector3Int(-3, -1, 0), roadCross);
        trackMap.SetTile(new Vector3Int(0, -1, 0), roadCross);
        trackMap.SetTile(new Vector3Int(3, -1, 0), roadCross);

        // Cruzamento central 4 vias
        trackMap.SetTile(new Vector3Int(0, 0, 0), roadCross);

        // 4 Entroncamentos em T sem pontas soltas (meio-fio externo contínuo!)
        trackMap.SetTile(new Vector3Int(0, 3, 0), teeNorth);
        trackMap.SetTile(new Vector3Int(-3, 0, 0), teeWest);
        trackMap.SetTile(new Vector3Int(0, -3, 0), teeSouth);
        trackMap.SetTile(new Vector3Int(3, 0, 0), teeEast);

        // 4 Esquinas arredondadas suaves
        trackMap.SetTile(new Vector3Int(-3, -3, 0), cornerSW);
        trackMap.SetTile(new Vector3Int(3, -3, 0), cornerSE);
        trackMap.SetTile(new Vector3Int(3, 3, 0), cornerNE);
        trackMap.SetTile(new Vector3Int(-3, 3, 0), cornerNW);
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

    // =========================================================================
    // 2. PRAÇAS DE ENTREGA IRRESISTÍVEIS (Cone gigante animado + Placa + Bichinhos torcendo!)
    // =========================================================================
    private static void BuildDeliveryPlazas(
        Transform root,
        List<SpriteRenderer> glows,
        List<Transform> beacons,
        List<Transform> critters)
    {
        Transform plazas = CreateGroup(root, "02_Pracas_de_Entrega");
        Sprite plazaSprite = LoadCandy("CandyDeliveryPlaza.png");
        Sprite glowSprite = LoadCandy("CandyGlow.png");
        Sprite coneBeacon = LoadCandy("CandyDeliveryConeBeacon.png");
        Sprite signSprite = LoadCandy("CandyIceCreamSign.png");
        Sprite gummy = LoadCandy("CandyCritterGummy.png");
        Sprite mallow = LoadCandy("CandyCritterMarshmallow.png");

        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Central", plazaSprite, glowSprite, coneBeacon, signSprite, gummy, mallow, new Vector2(0f, 0f), glows, beacons, critters);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Leste", plazaSprite, glowSprite, coneBeacon, signSprite, gummy, mallow, new Vector2(18f, 0f), glows, beacons, critters);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Oeste", plazaSprite, glowSprite, coneBeacon, signSprite, gummy, mallow, new Vector2(-18f, 0f), glows, beacons, critters);
    }

    private static void CreateDeliveryPlaza(
        Transform parent,
        string name,
        Sprite plazaSprite,
        Sprite glowSprite,
        Sprite coneBeacon,
        Sprite signSprite,
        Sprite gummy,
        Sprite mallow,
        Vector2 pos,
        List<SpriteRenderer> glows,
        List<Transform> beacons,
        List<Transform> critters)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        float sc = NormalizeSpriteScale(plazaSprite, 5.4f); // Cabe folgado dentro do cruzamento 6x6!
        go.transform.localScale = Vector3.one * sc;
        TrySetTag(go, "Customer");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = plazaSprite;
        sr.sortingLayerName = "GroundDetails";
        sr.sortingOrder = 1;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = plazaSprite != null ? plazaSprite.bounds.size.x * 0.48f : 2.8f;

        if (glowSprite != null)
        {
            float glowSc = NormalizeSpriteScale(glowSprite, 6.2f) / Mathf.Max(0.01f, sc);
            SpriteRenderer g = CreateSprite(go.transform, "BrilhoPraca", glowSprite, Vector2.zero, Vector2.one * glowSc, "VFX", 1, 0f);
            g.color = new Color(1f, 0.72f, 0.88f, 0.24f);
            glows.Add(g);
        }

        // Cone gigante de sorvete + seta dourada flutuando acima da praça (leitura imediata para crianças!)
        if (coneBeacon != null)
        {
            float beaconSc = NormalizeSpriteScale(coneBeacon, 1.75f);
            SpriteRenderer bSr = CreateSprite(parent, name + "_MarcoCone", coneBeacon, pos + new Vector2(0f, 1.35f), Vector2.one * beaconSc, "VFX", 6, 0f);
            beacons.Add(bSr.transform);
        }

        // Placa ilustrada de sorvete ao lado da praça (dá pulinho ao entregar!)
        if (signSprite != null)
        {
            Vector2 signPos = pos + new Vector2(pos.x > 10f ? -3.8f : 3.8f, 3.6f);
            float signSc = NormalizeSpriteScale(signSprite, 1.35f);
            SpriteRenderer sSr = CreateSprite(parent, name + "_PlacaSorvete", signSprite, signPos, Vector2.one * signSc, "PropsAndBuildings", YOrder(signPos.y), 0f);
            AddWobble(sSr.gameObject, 5.5f, 0.26f);
        }

        // Bichinhos doces torcendo alegremente nos cantos da praça!
        if (gummy != null)
        {
            Vector2 gp = pos + new Vector2(pos.x < -10f ? 3.9f : -3.9f, 3.6f);
            float gSc = NormalizeSpriteScale(gummy, 1.15f);
            SpriteRenderer gSr = CreateSprite(parent, name + "_UrsinhoTorcida", gummy, gp, Vector2.one * gSc, "PropsAndBuildings", YOrder(gp.y), 0f);
            AddWobble(gSr.gameObject, 5.5f, 0.30f);
            critters.Add(gSr.transform);
        }
        if (mallow != null)
        {
            Vector2 mp = pos + new Vector2(pos.x > 10f ? -3.9f : 3.9f, -3.7f);
            float mSc = NormalizeSpriteScale(mallow, 1.10f);
            SpriteRenderer mSr = CreateSprite(parent, name + "_MarshmallowTorcida", mallow, mp, Vector2.one * mSc, "PropsAndBuildings", YOrder(mp.y), 0f);
            AddWobble(mSr.gameObject, 5.5f, 0.30f);
            critters.Add(mSr.transform);
        }
    }

    private static void CreateHouseDeliveryPoint(Transform parent, string name, Sprite markerSprite, Vector2 pos)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.position = new Vector3(pos.x, pos.y, 0f);
        TrySetTag(point, "Customer");

        float markerWidth = 2.4f;
        float scale = markerSprite != null ? NormalizeSpriteScale(markerSprite, markerWidth) : 1f;
        point.transform.localScale = Vector3.one * scale;

        if (markerSprite != null)
        {
            SpriteRenderer marker = point.AddComponent<SpriteRenderer>();
            marker.sprite = markerSprite;
            marker.color = new Color(1f, 0.88f, 0.96f, 0.88f);
            marker.sortingLayerName = "GroundDetails";
            marker.sortingOrder = 4;
        }

        CircleCollider2D trigger = point.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1f / Mathf.Max(0.01f, scale);
    }

    // =========================================================================
    // 3. SLOWZONES DE CARAMELO & PORTÕES DE ENTREGA VELOZ INTEGRADOS À VIA
    // =========================================================================
    private static void BuildSlowZonesAndSpeedGates(Transform root)
    {
        Transform zones = CreateGroup(root, "03_SlowZones_e_SpeedGates");

        // 2 Poças de caramelo nítidas centralizadas nas avenidas norte/sul
        Vector2[] slowSpots = { new Vector2(-9f, 18f), new Vector2(9f, -18f) };
        for (int i = 0; i < slowSpots.Length; i++)
        {
            GameObject sz = new GameObject("SlowZone_Caramelo_" + (i + 1));
            sz.transform.SetParent(zones, false);
            sz.transform.position = new Vector3(slowSpots[i].x, slowSpots[i].y, 0f);
            sz.transform.localScale = new Vector3(1.45f, 1.15f, 1f);
            SlowZone script = sz.AddComponent<SlowZone>();
            script.isPermanentZone = true;
        }

        // Portão 1 logo à frente do jogador no trecho inicial (vertical, 0 deg) + Portão 2 na avenida leste (90 deg)
        CreateSpeedGate(zones, "SpeedDeliveryGate_Inicial", new Vector2(0f, -4.5f), 0f);
        CreateSpeedGate(zones, "SpeedDeliveryGate_Leste", new Vector2(9f, 0f), 90f);
        CreateSpeedGate(zones, "SpeedDeliveryGate_Oeste", new Vector2(-9f, 0f), 90f);
    }

    private static void CreateSpeedGate(Transform parent, string name, Vector2 pos, float rotZ)
    {
        GameObject gate = new GameObject(name);
        gate.transform.SetParent(parent, false);
        gate.transform.position = new Vector3(pos.x, pos.y, 0f);
        gate.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        SpeedDeliveryGate sdg = gate.AddComponent<SpeedDeliveryGate>();
        sdg.laneWidth = DrivableLaneWidth;
    }

    // =========================================================================
    // 4. 8 CASAS CUPCAKE RECORTADAS + SOMBRAS DE CONTATO + CALÇADA ARCO-ÍRIS + Y-SORTING
    // =========================================================================
    private static void BuildCupcakeHousesAndGroupedProps(
        Transform root,
        List<Transform> swaying,
        List<SpriteRenderer> glows,
        List<SpriteRenderer> sparkles,
        List<Transform> critters)
    {
        Transform houses = CreateGroup(root, "04_Casas_e_Cenario_Agrupado");
        Sprite shadowSp = LoadCandy("CandyShadow.png");
        Sprite awningSp = LoadCandy("CandyStripedAwning.png");
        Sprite signSp = LoadCandy("CandyIceCreamSign.png");
        Sprite gummySp = LoadCandy("CandyCritterGummy.png");
        Sprite mallowSp = LoadCandy("CandyCritterMarshmallow.png");

        // Place initial houses at the center of their 12x12 blocks so their art stays clear of the road curb.
        PlaceHouse(houses, "Casa_ArcoIris", "CandyHouse_Rainbow.png", new Vector2(9.0f, -12.0f), 5.2f, shadowSp);
        BuildSteppingStoneWalkway(houses, new Vector2(3.3f, -9.2f), new Vector2(8.7f, -12.0f));
        CreateHouseDeliveryPoint(houses, "Descarga_Casa_ArcoIris", LoadCandy("CandyDeliveryPlaza.png"), new Vector2(9.0f, -14.4f));
        if (awningSp != null)
        {
            float awSc = NormalizeSpriteScale(awningSp, 2.25f);
            SpriteRenderer aw = CreateSprite(houses, "Toldo_ArcoIris", awningSp, new Vector2(9.0f, -13.05f), Vector2.one * awSc, "PropsAndBuildings", YOrder(-13.05f), 0f);
            AddWobble(aw.gameObject, 5.2f, 0.16f);
        }
        if (signSp != null)
        {
            float sSc = NormalizeSpriteScale(signSp, 1.30f);
            SpriteRenderer sg = CreateSprite(houses, "PlacaSorvete_ArcoIris", signSp, new Vector2(4.4f, -10.8f), Vector2.one * sSc, "PropsAndBuildings", YOrder(-10.8f), 0f);
            AddWobble(sg.gameObject, 5.0f, 0.24f);
        }
        if (gummySp != null)
        {
            float cSc = NormalizeSpriteScale(gummySp, 1.15f);
            SpriteRenderer cr = CreateSprite(houses, "UrsinhoBoasVindas_Dir", gummySp, new Vector2(4.4f, -14.2f), Vector2.one * cSc, "PropsAndBuildings", YOrder(-14.2f), 0f);
            AddWobble(cr.gameObject, 5.0f, 0.28f);
            critters.Add(cr.transform);
        }

        // Bloco inicial à esquerda (visível na primeira tela!): Casa Foguete Azul + bichinho acenando!
        PlaceHouse(houses, "Casa_FogueteAzul", "CandyHouse_RocketBlue.png", new Vector2(-9.0f, -12.0f), 5.1f, shadowSp);
        BuildSteppingStoneWalkway(houses, new Vector2(-3.3f, -9.2f), new Vector2(-8.7f, -12.0f));
        if (mallowSp != null)
        {
            float cSc = NormalizeSpriteScale(mallowSp, 1.10f);
            SpriteRenderer cr = CreateSprite(houses, "MarshmallowBoasVindas_Esq", mallowSp, new Vector2(-4.4f, -14.2f), Vector2.one * cSc, "PropsAndBuildings", YOrder(-14.2f), 0f);
            AddWobble(cr.gameObject, 5.0f, 0.28f);
            critters.Add(cr.transform);
        }

        // Blocos internos superiores (norte da Praça Central) + caminhos e toldo!
        PlaceHouse(houses, "Casa_Morango", "CandyHouse_Strawberry.png", new Vector2(-9.0f, 12.0f), 5.2f, shadowSp);
        BuildSteppingStoneWalkway(houses, new Vector2(-3.3f, 9.2f), new Vector2(-7.0f, 11.0f));
        if (awningSp != null)
        {
            float awSc = NormalizeSpriteScale(awningSp, 2.25f);
            SpriteRenderer aw = CreateSprite(houses, "Toldo_Morango", awningSp, new Vector2(-9.0f, 10.95f), Vector2.one * awSc, "PropsAndBuildings", YOrder(10.95f), 0f);
            AddWobble(aw.gameObject, 5.2f, 0.16f);
        }

        PlaceHouse(houses, "Casa_ChocoMenta", "CandyHouse_ChocoMint.png", new Vector2(9.0f, 12.0f), 5.2f, shadowSp);
        BuildSteppingStoneWalkway(houses, new Vector2(3.3f, 9.2f), new Vector2(7.0f, 11.0f));

        // Quintais externos norte e sul (fora do anel viário ±18)
        PlaceHouse(houses, "Casa_EstufaBotanica", "CandyHouse_Botanical.png", new Vector2(-9.0f, 24.5f), 5.2f, shadowSp);
        PlaceHouse(houses, "Casa_Matcha", "CandyHouse_Matcha.png", new Vector2(9.0f, 24.5f), 5.2f, shadowSp);
        PlaceHouse(houses, "Casa_Galaxia", "CandyHouse_Galaxy.png", new Vector2(-9.0f, -24.5f), 5.2f, shadowSp);
        PlaceHouse(houses, "Casa_Nautica", "CandyHouse_Nautical.png", new Vector2(9.0f, -24.5f), 5.2f, shadowSp);

        // Lagoas de soda (camada GroundDetails, centradas nos jardins externos leste/oeste)
        Sprite pond = LoadCandy("CandyBerryPond.png");
        if (pond != null)
        {
            float pondSc = NormalizeSpriteScale(pond, 4.4f);
            PlacePond(houses, "LagoaSoda_Oeste", pond, new Vector2(-24.2f, 9.0f), pondSc);
            PlacePond(houses, "LagoaSoda_Leste", pond, new Vector2(24.2f, -9.0f), pondSc);
        }

        // Props agrupados em torno das casas com escalas normalizadas pelo tamanho opaco real + Y-sorting!
        Sprite treePink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/download__2_-removebg-preview.png");
        Sprite treePine = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (67).png");
        Sprite lolliA = LoadCandy("GiantLollipopA.png");
        Sprite lolliB = LoadCandy("GiantLollipopB.png");
        Sprite cane = LoadCandy("CandyCanePost.png");
        Sprite lamp = LoadCandy("CandyLampPost.png");
        Sprite flower = LoadCandy("SugarFlowerBed.png");
        Sprite mallow = LoadCandy("MarshmallowCluster.png");
        Sprite glow = LoadCandy("CandyGlow.png");
        Sprite sparkle = LoadCandy("CandySparkle.png");

        // Árvores agrupadas nos cantos dos quintais (opaco: treePink 40.7%, treePine 28.7% -> visível ~2.1 units!)
        float pinkScale = treePink != null ? NormalizeSpriteScale(treePink, 2.2f / 0.407f) : 0.5f;
        float pineScale = treePine != null ? NormalizeSpriteScale(treePine, 1.85f / 0.287f) : 0.5f;

        Vector2[] treeSpots =
        {
            // Quintal Casa Arco-Íris (SE)
            new Vector2(5.2f, -6.0f), new Vector2(12.8f, -6.2f), new Vector2(12.6f, -12.0f),
            // Quintal Casa Foguete Azul (SO)
            new Vector2(-5.2f, -6.0f), new Vector2(-12.8f, -6.2f), new Vector2(-12.6f, -12.0f),
            // Quintal Casa Morango (NO)
            new Vector2(-5.2f, 12.2f), new Vector2(-12.8f, 12.0f), new Vector2(-12.8f, 6.2f),
            // Quintal Casa ChocoMenta (NE)
            new Vector2(5.2f, 12.2f), new Vector2(12.8f, 12.0f), new Vector2(12.8f, 6.2f),
            // Quintais externos
            new Vector2(-13.8f, 24.5f), new Vector2(-4.4f, 24.5f),
            new Vector2(4.4f, 24.5f), new Vector2(13.8f, 24.5f),
            new Vector2(-13.8f, -24.5f), new Vector2(-4.4f, -24.5f),
            new Vector2(4.4f, -24.5f), new Vector2(13.8f, -24.5f)
        };
        for (int i = 0; i < treeSpots.Length; i++)
        {
            bool usePink = (i % 2 == 0);
            Sprite ts = usePink ? treePink : treePine;
            if (ts == null) continue;
            float sc = usePink ? pinkScale : pineScale;
            SpriteRenderer sr = CreateSprite(houses, "ArvoreAgrupada_" + (i + 1), ts, treeSpots[i], Vector2.one * sc, "PropsAndBuildings", YOrder(treeSpots[i].y), 0f);
            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = ts.bounds.size.x * (usePink ? 0.14f : 0.11f);
            col.offset = new Vector2(0f, -ts.bounds.size.y * 0.18f);
            swaying.Add(sr.transform);
        }

        // Pirulitos-marco nos 4 cantos internos da Praça Central (bem recuados do meio-fio, visível = 1.15 units!)
        float lolliSc = NormalizeSpriteScale(lolliA, 1.15f);
        Vector2[] lollis = { new Vector2(-3.9f, 3.9f), new Vector2(3.9f, 3.9f), new Vector2(-3.9f, -3.9f), new Vector2(3.9f, -3.9f) };
        for (int i = 0; i < lollis.Length; i++)
        {
            Sprite sp = (i % 2 == 0) ? lolliA : lolliB;
            if (sp == null) continue;
            SpriteRenderer sr = CreateSprite(houses, "PirulitoMarco_" + (i + 1), sp, lollis[i], Vector2.one * lolliSc, "PropsAndBuildings", YOrder(lollis[i].y), 0f);
            swaying.Add(sr.transform);
        }

        // Bengalas doces nas esquinas externas
        if (cane != null)
        {
            float caneSc = NormalizeSpriteScale(cane, 1.05f);
            Vector2[] canes = { new Vector2(-14.2f, -14.2f), new Vector2(14.2f, -14.2f), new Vector2(-14.2f, 14.2f), new Vector2(14.2f, 14.2f) };
            for (int i = 0; i < canes.Length; i++)
                CreateSprite(houses, "BengalaMarco_" + (i + 1), cane, canes[i], Vector2.one * caneSc, "PropsAndBuildings", YOrder(canes[i].y), 0f);
        }

        // Postes luminosos ao longo das avenidas centrais (visível = 0.85 units!)
        if (lamp != null)
        {
            float lampSc = NormalizeSpriteScale(lamp, 0.85f);
            float glowSc = NormalizeSpriteScale(glow, 2.2f);
            Vector2[] lamps = { new Vector2(-3.7f, 12.0f), new Vector2(3.7f, 12.0f), new Vector2(-3.7f, -12.0f), new Vector2(3.7f, -12.0f) };
            for (int i = 0; i < lamps.Length; i++)
            {
                CreateSprite(houses, "PosteDoce_" + (i + 1), lamp, lamps[i], Vector2.one * lampSc, "PropsAndBuildings", YOrder(lamps[i].y), 0f);
                if (glow != null)
                {
                    SpriteRenderer g = CreateSprite(houses, "LuzPoste_" + (i + 1), glow, lamps[i] + new Vector2(0f, 0.75f), Vector2.one * glowSc, "VFX", 2, 0f);
                    g.color = new Color(1f, 0.90f, 0.62f, 0.26f);
                    glows.Add(g);
                }
            }
        }

        // Canteiros de flores e marshmallows nos jardins (GroundDetails)
        if (flower != null)
        {
            float flSc = NormalizeSpriteScale(flower, 1.85f);
            Vector2[] flowers = { new Vector2(-9.0f, 5.4f), new Vector2(9.0f, 5.4f), new Vector2(-9.0f, -12.2f), new Vector2(9.0f, -12.2f) };
            for (int i = 0; i < flowers.Length; i++)
                CreateSprite(houses, "Canteiro_" + (i + 1), flower, flowers[i], Vector2.one * flSc, "GroundDetails", 3, 0f);
        }

        if (mallow != null)
        {
            float mlSc = NormalizeSpriteScale(mallow, 1.45f);
            Vector2[] mallows = { new Vector2(5.4f, -12.4f), new Vector2(-5.4f, -12.4f), new Vector2(5.4f, 6.0f), new Vector2(-5.4f, 6.0f) };
            for (int i = 0; i < mallows.Length; i++)
                CreateSprite(houses, "Marshmallows_" + (i + 1), mallow, mallows[i], Vector2.one * mlSc, "PropsAndBuildings", YOrder(mallows[i].y), 0f);
        }

        // Partículas cintilantes sutis (opacidade <= 0.30 na camada VFX)
        if (sparkle != null)
        {
            float spSc = NormalizeSpriteScale(sparkle, 0.45f);
            for (int i = 0; i < 10; i++)
            {
                float sx = -18f + (i * 9.7f % 36f);
                float sy = -15f + (i * 6.9f % 30f);
                SpriteRenderer sp = CreateSprite(houses, "BrilhoVFX_" + (i + 1), sparkle, new Vector2(sx, sy), Vector2.one * spSc, "VFX", 3, 0f);
                sp.color = new Color(1f, 0.90f, 0.96f, 0.28f);
                sparkles.Add(sp);
            }
        }
    }

    private static void PlaceHouse(Transform parent, string name, string spriteFile, Vector2 pos, float targetWorldWidth, Sprite shadowSprite)
    {
        Sprite sp = LoadCandy(spriteFile);
        if (sp == null) return;
        float sc = NormalizeSpriteScale(sp, targetWorldWidth);

        // Sombra de contato suave na base da casa (GroundDetails)
        if (shadowSprite != null)
        {
            float shW = NormalizeSpriteScale(shadowSprite, targetWorldWidth * 0.96f);
            float shH = shW * 0.65f;
            SpriteRenderer sh = CreateSprite(parent, name + "_Sombra", shadowSprite, pos + new Vector2(0.08f, -targetWorldWidth * 0.36f), new Vector2(shW, shH), "GroundDetails", 2, 0f);
            sh.color = new Color(1f, 1f, 1f, 0.58f);
        }

        SpriteRenderer sr = CreateSprite(parent, name, sp, pos, Vector2.one * sc, "PropsAndBuildings", YOrder(pos.y), 0f);
        BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(sp.bounds.size.x * 0.72f, sp.bounds.size.y * 0.54f);
        col.offset = new Vector2(0f, -sp.bounds.size.y * 0.14f);
        AddWobble(sr.gameObject, 6.4f, 0.16f);
    }

    private static void AddWobble(GameObject go, float radius, float hopHeight)
    {
        CandyWobbleOnPass w = go.AddComponent<CandyWobbleOnPass>();
        w.triggerRadius = radius;
        w.hopHeight = hopHeight;
    }

    private static void BuildSteppingStoneWalkway(Transform parent, Vector2 start, Vector2 end)
    {
        Sprite mallow = LoadCandy("MarshmallowCluster.png");
        Sprite plaza = LoadCandy("CandyDeliveryPlaza.png");
        Sprite stoneSprite = plaza != null ? plaza : mallow;
        if (stoneSprite == null) return;

        int steps = 4;
        float stoneSc = NormalizeSpriteScale(stoneSprite, 1.05f);
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            Vector2 p = Vector2.Lerp(start, end, t);
            SpriteRenderer sr = CreateSprite(parent, "PedraCaminho_" + (i + 1), stoneSprite, p, new Vector2(stoneSc, stoneSc * 0.68f), "GroundDetails", 2, 0f);
            sr.color = new Color(1f, 0.94f, 0.98f, 0.92f);
        }
    }

    private static void PlacePond(Transform parent, string name, Sprite pond, Vector2 pos, float scale)
    {
        SpriteRenderer sr = CreateSprite(parent, name, pond, pos, Vector2.one * scale, "GroundDetails", 0, 0f);
        CapsuleCollider2D pc = sr.gameObject.AddComponent<CapsuleCollider2D>();
        pc.direction = CapsuleDirection2D.Horizontal;
        pc.size = new Vector2(pond.bounds.size.x * 0.78f, pond.bounds.size.y * 0.65f);
    }

    // =========================================================================
    // 5. ITENS COLETÁVEIS (DONUTS PROPORCIONAIS) & BOOSTS
    // =========================================================================
    private static void BuildPickupsAndBoosts(Transform root, List<Transform> pickups)
    {
        Transform items = CreateGroup(root, "05_Itens_e_Boosts");
        Sprite donutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (77).png");
        Sprite boostSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameDev.tv - Delivery Dash Assets - cópia/Props & Stuff/pickupspeed.png");

        // Donut opaco = 51.0% da largura -> visível = 1.12 units (menor que o carrinho de 1.95 units!)
        float donutSc = donutSprite != null ? NormalizeSpriteScale(donutSprite, 1.12f / 0.510f) : 0.20f;
        float boostSc = boostSprite != null ? NormalizeSpriteScale(boostSprite, 1.55f) : 0.45f;

        Vector2[] packagePositions =
        {
            new Vector2(0f, -14f), new Vector2(0f, 8f), new Vector2(0f, 18f),
            new Vector2(-6f, 0f), new Vector2(-14f, 0f), new Vector2(6f, 0f), new Vector2(14f, 0f),
            new Vector2(-12f, 18f), new Vector2(12f, 18f), new Vector2(-12f, -18f), new Vector2(12f, -18f),
            new Vector2(-18f, 9f), new Vector2(-18f, -9f), new Vector2(18f, 9f), new Vector2(18f, -9f)
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
            sr.sortingOrder = 4;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = donutSprite != null ? donutSprite.bounds.size.x * 0.28f : 0.85f;
            pickups.Add(go.transform);
        }

        Vector2[] boostPositions = { new Vector2(0f, 13f), new Vector2(18f, 12f), new Vector2(-18f, -12f), new Vector2(18f, -12f) };
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

    // =========================================================================
    // 6. CAMADA DE PARALAXE AÉREA (NUVENS DE ALGODÃO-DOCE TRANSLÚCIDAS EM VFX)
    // =========================================================================
    private static void BuildParallaxSkyLayer(Transform root, List<Transform> parallaxClouds)
    {
        Sprite cloudSp = LoadCandy("ParallaxCloudWisp.png");
        if (cloudSp == null) return;

        Transform sky = CreateGroup(root, "06_Paralaxe_Nuvens");
        float baseSc = NormalizeSpriteScale(cloudSp, 5.8f);
        Vector2[] spots =
        {
            new Vector2(-22f, 16f), new Vector2(4f, 20f), new Vector2(26f, 14f),
            new Vector2(-16f, -4f), new Vector2(14f, -6f),
            new Vector2(-26f, -18f), new Vector2(6f, -20f), new Vector2(24f, -16f)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            float sizeMul = 0.85f + (i % 3) * 0.18f;
            SpriteRenderer sr = CreateSprite(sky, "NuvemParalaxe_" + (i + 1), cloudSp, spots[i], Vector2.one * (baseSc * sizeMul), "VFX", 8, 0f);
            sr.color = new Color(1f, 0.96f, 1f, 0.22f); // Bem sutil para dar profundidade sem poluir a pista
            parallaxClouds.Add(sr.transform);
        }
    }

    private static void BuildPerimeterBoundary(Transform root, List<Transform> swaying)
    {
        Transform bounds = CreateGroup(root, "07_Limites_e_Moldura_Parque");
        CreateWall(bounds, "Limite_Norte", new Vector2(0f, 28.5f), new Vector2(64f, 2f));
        CreateWall(bounds, "Limite_Sul", new Vector2(0f, -28.5f), new Vector2(64f, 2f));
        CreateWall(bounds, "Limite_Oeste", new Vector2(-28.5f, 0f), new Vector2(2f, 60f));
        CreateWall(bounds, "Limite_Leste", new Vector2(28.5f, 0f), new Vector2(2f, 60f));

        // Moldura do parque com vegetação e pirulitos; cercas em segmentos criavam barras soltas no terreno.
        Sprite bush = LoadCandy("CandyCottonBush.png");
        Sprite lolliA = LoadCandy("GiantLollipopA.png");
        Sprite lolliB = LoadCandy("GiantLollipopB.png");

        float bushSc = bush != null ? NormalizeSpriteScale(bush, 2.6f) : 1f;
        float lolSc = lolliA != null ? NormalizeSpriteScale(lolliA, 1.45f) : 1f;

        // Bordas norte e sul do parque (y = ±27.2)
        for (int i = -6; i <= 6; i++)
        {
            float x = i * 4.2f;
            foreach (float y in new[] { 27.2f, -27.2f })
            {
                if (bush != null)
                    CreateSprite(bounds, "ArbustoParque_" + x + "_" + y, bush, new Vector2(x, y), Vector2.one * bushSc, "PropsAndBuildings", YOrder(y), 0f);
            }
        }

        // Bordas leste e oeste do parque (x = ±22.8, logo fora do anel viário ±18!)
        for (int i = -5; i <= 5; i++)
        {
            float y = i * 4.4f;
            foreach (float x in new[] { -22.8f, 22.8f })
            {
                if (Mathf.Abs(y - 9f) < 3f && x < 0f) continue; // Espaço livre para Lagoa Oeste
                if (Mathf.Abs(y + 9f) < 3f && x > 0f) continue; // Espaço livre para Lagoa Leste

                if (Mathf.Abs(i) % 2 == 0 && bush != null)
                    CreateSprite(bounds, "ArbustoLat_" + x + "_" + y, bush, new Vector2(x, y), Vector2.one * bushSc, "PropsAndBuildings", YOrder(y), 0f);
                else if (lolliA != null)
                {
                    Sprite sp = (i % 2 == 0) ? lolliA : lolliB;
                    SpriteRenderer lSr = CreateSprite(bounds, "PirulitoBorda_" + x + "_" + y, sp, new Vector2(x, y), Vector2.one * lolSc, "PropsAndBuildings", YOrder(y), 0f);
                    swaying.Add(lSr.transform);
                }
            }
        }
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

    // Y-Sorting determinístico: objetos mais ao sul (menor Y) recebem sortingOrder maior!
    private static int YOrder(float worldY) => Mathf.RoundToInt(-worldY * 10f);

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
