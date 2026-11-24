using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Assembles the 3D Animal Crossing: New Horizons-inspired Matinhos Orla scene
/// from the Blender-generated FBX models in Assets/GirlPower/Orla3D/Models.
/// Keeps all original 2D scenes completely untouched.
/// </summary>
[InitializeOnLoad]
public static class GirlPowerOrla3DSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Orla 3D (Animal Crossing).unity";
    private const string ModelsFolder = "Assets/GirlPower/Orla3D/Models";
    private const string MaterialsFolder = "Assets/GirlPower/Orla3D/Materials";
    private const string CozyShaderName = "GirlPower/OrlaACNHCozy";
    private const string OceanShaderName = "GirlPower/OrlaACNHOcean";

    private const string BuildVersionKey = "GirlPower_Orla3D_SceneVersion_v7_MegaLifeAndCardapio";

    static GirlPowerOrla3DSceneBuilder()
    {
        EditorApplication.delayCall += TryAutoBuildOnce;
    }

    private static void TryAutoBuildOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
        string fullScenePath = Path.Combine(Application.dataPath, "Scenes/Girl Power - Orla 3D (Animal Crossing).unity");
        bool alreadyBuiltV5 = EditorPrefs.GetBool(BuildVersionKey, false);
        if (File.Exists(fullScenePath) && alreadyBuiltV5) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + "/CoastalHouse.fbx") == null) return;
        EditorPrefs.SetBool(BuildVersionKey, true);
        Build();
    }

    [MenuItem("Girl Power/Build Orla 3D (Animal Crossing Style)")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "GirlPower/Orla3D/Materials"));
        Dictionary<string, Material> cozyMats = BuildCozyMaterials();
        Material oceanMat = BuildOceanMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.52f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.52f, 0.48f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.22f, 0.26f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.66f, 0.62f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 108f;

        GameObject root = new GameObject("Orla de Matinhos 3D - Animal Crossing Style");

        // 1. Camera (Iconic Animal Crossing tilted miniature diorama camera!)
        Camera mainCam = CreateCamera(root.transform);

        // 2. Golden-Hour Directional Sun Light
        Light sunLight = CreateSunLight(root.transform);

        // 3. Environment Base + Animated Ocean Surface (with Baía do Avesso dynamic tide!)
        GameObject envBase = PlaceModel(root.transform, "OrlaEnvironmentBase", "Terreno Costeiro - Baia do Avesso", Vector3.zero, 0f, 1f, cozyMats);
        if (envBase != null)
        {
            Renderer r = envBase.GetComponentInChildren<Renderer>();
            if (r != null) Debug.Log("[Orla3D] OrlaEnvironmentBase Bounds: " + r.bounds);
        }
        Transform oceanSurface = CreateAnimatedOcean(root.transform, oceanMat);

        // 4. Quiosque Girl Power + Volumetric Striped Awning Upgrade + 3D Signs
        List<BoxCollider> obstacles = new List<BoxCollider>();
        GameObject kiosk = PlaceModel(root.transform, "KioskGirlPower", "Quiosque Girl Power", new Vector3(0f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (kiosk != null) AddBlockingBox(kiosk, new Vector3(0f, 1.2f, 0f), new Vector3(4.6f, 2.4f, 2.2f), obstacles);
        Make3DText(root.transform, "Titulo Quiosque", new Vector3(0f, 3.55f, 1.66f), "GIRL POWER", 2.35f, new Color32(255, 244, 218, 255), new Vector2(2.4f, 0.55f));

        GameObject customSign = PlaceModel(root.transform, "SignboardNew", "Placa Personalizada - Sorveteria", new Vector3(0f, 3.56f, 1.54f), 0f, 0.96f, cozyMats);
        TextMeshPro customSignLabel = null;
        if (customSign != null)
        {
            customSignLabel = Make3DText(customSign.transform, "Nome Instalado", new Vector3(0f, 0f, -0.17f), "BAÍA DO AVESSO", 2.15f, new Color32(255, 244, 214, 255), new Vector2(2.85f, 0.55f));
            customSign.SetActive(false);
        }

        // Choice Upgrade 1: Volumetric Striped Fabric Awning (Blender FBX, hidden until chosen!)
        GameObject kioskAwningUpgrade = PlaceModel(root.transform, "KioskAwningUpgrade", "Melhoria - Toldo Listrado", new Vector3(0f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (kioskAwningUpgrade != null) kioskAwningUpgrade.SetActive(false);

        // 5. Banca da Florista + Swaying Breeze Curtains + 3D Sign
        GameObject floristStand = PlaceModel(root.transform, "FloristStand", "Banca da Florista - Baia do Avesso", new Vector3(24f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (floristStand != null) AddBlockingBox(floristStand, new Vector3(0f, 1.2f, 0f), new Vector3(4.3f, 2.4f, 2.1f), obstacles);
        Make3DText(root.transform, "Titulo Florista", new Vector3(24f, 3.44f, 1.72f), "FLORES DA BAÍA", 2.0f, new Color32(255, 245, 220, 255), new Vector2(2.6f, 0.50f));

        List<Transform> breezeCurtains = new List<Transform>();
        GameObject curtL = PlaceModel(root.transform, "FloristCurtain", "Cortina Florista Esq", new Vector3(22.35f, 2.46f, 1.48f), 0f, 0.92f, cozyMats);
        GameObject curtR = PlaceModel(root.transform, "FloristCurtain", "Cortina Florista Dir", new Vector3(25.65f, 2.46f, 1.48f), 0f, 0.92f, cozyMats);
        if (curtL != null) breezeCurtains.Add(curtL.transform);
        if (curtR != null) breezeCurtains.Add(curtR.transform);

        // 5b. Pavilhão da Baía do Avesso & Acesso à Areia + Chapter 3/Finale Restoration Props!
        GameObject pavilion = PlaceModel(root.transform, "PavilionBay", "Pavilhao da Baia do Avesso", new Vector3(15.6f, -0.02f, 5.45f), 0f, 1f, cozyMats);
        TextMeshPro pavilionFadedText = Make3DText(root.transform, "Letreiro Apagado Pavilhao", new Vector3(15.6f, 3.34f, 3.40f), "BAÍA DO AVESSO", 1.55f, new Color32(198, 186, 166, 175), new Vector2(2.8f, 0.45f));

        // Festival restoration props (hidden until Chapter 3/Finale restores the Pavilion!)
        GameObject pavilionFestivalTable = PlaceModel(root.transform, "PavilionFestivalTable", "Pavilhao - Mesa Comprida", new Vector3(15.6f, 0.36f, 5.45f), 0f, 1f, cozyMats);
        if (pavilionFestivalTable != null) pavilionFestivalTable.SetActive(false);

        GameObject pavilionFestivalLights = PlaceModel(root.transform, "PavilionFestivalLights", "Pavilhao - Luzes e Letreiro Restaurado", new Vector3(15.6f, -0.02f, 5.45f), 0f, 1f, cozyMats);
        if (pavilionFestivalLights != null) pavilionFestivalLights.SetActive(false);

        GameObject pavilionFestivalDishes = PlaceModel(root.transform, "FestivalDishesProp", "Pavilhao - Pratos e Vasinhos da Festa", new Vector3(15.6f, 0.36f, 5.45f), 0f, 1f, cozyMats);
        if (pavilionFestivalDishes != null) pavilionFestivalDishes.SetActive(false);

        TextMeshPro pavilionRestoredText = Make3DText(root.transform, "Letreiro Restaurado Pavilhao", new Vector3(15.6f, 3.36f, 3.32f), "BAÍA DO AVESSO", 1.75f, new Color32(255, 245, 205, 255), new Vector2(3.0f, 0.50f));
        pavilionRestoredText.gameObject.SetActive(false);

        List<Light> pavilionPointLights = new List<Light>();
        pavilionPointLights.Add(CreateWarmLight(root.transform, "Luz Pavilhao Centro", new Vector3(15.6f, 2.5f, 5.45f), 2.1f, 11.5f));
        pavilionPointLights.Add(CreateWarmLight(root.transform, "Luz Pavilhao Frente", new Vector3(15.6f, 2.9f, 3.4f), 1.6f, 9.0f));
        foreach (Light pl in pavilionPointLights) pl.enabled = false;

        // 6. Feirinha da Orla & 6 Barracas Temáticas (Água de Coco, Artesanato, Plantas, Sushi, Comida e Roupas!)
        GameObject stallCoco = PlaceModel(root.transform, "MarketStall_Coco", "Barraca - Agua de Coco", new Vector3(11.1f, 0f, 2.85f), 0f, 1f, cozyMats);
        GameObject stallArt = PlaceModel(root.transform, "MarketStall_Artesanato", "Barraca - Artesanato", new Vector3(15.6f, 0f, 2.85f), 0f, 1f, cozyMats);
        GameObject stallPlantas = PlaceModel(root.transform, "MarketStall_Plantas", "Barraca - Plantas Naturais", new Vector3(20.1f, 0f, 2.85f), 0f, 1f, cozyMats);
        GameObject stallSushi = PlaceModel(root.transform, "MarketStall_Sushi", "Barraca - Sushi da Baia", new Vector3(-7.4f, 0f, 2.55f), 0f, 1f, cozyMats);
        GameObject stallComida = PlaceModel(root.transform, "MarketStall_Comida", "Barraca - Comida e Acai", new Vector3(-11.8f, 0f, 2.55f), 0f, 1f, cozyMats);
        GameObject stallRoupas = PlaceModel(root.transform, "MarketStall_Roupas", "Barraca - Roupas da Orla", new Vector3(29.8f, 0f, 2.55f), 0f, 1f, cozyMats);
        GameObject stallDoces = PlaceModel(root.transform, "MarketStall_Doces", "Barraca - Doces e Cupcakes", new Vector3(-16.0f, 0f, 2.55f), 0f, 1f, cozyMats);
        GameObject stallSucos = PlaceModel(root.transform, "MarketStall_SucosRefri", "Barraca - Sucos, Agua e Refri", new Vector3(34.0f, 0f, 2.55f), 0f, 1f, cozyMats);
        foreach (GameObject st in new[] { stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (st != null) AddBlockingBox(st, new Vector3(0f, 1.0f, 0f), new Vector3(2.7f, 2.0f, 1.15f), obstacles);

        // 3D labels above the new specialty stalls
        Make3DText(root.transform, "Placa Sushi", new Vector3(-7.4f, 2.58f, 1.72f), "SUSHI", 1.45f, new Color32(255, 245, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Comida", new Vector3(-11.8f, 2.58f, 1.72f), "COMIDAS", 1.45f, new Color32(255, 245, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Doces", new Vector3(-16.0f, 2.58f, 1.72f), "DOCES", 1.45f, new Color32(255, 245, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Plantas", new Vector3(20.1f, 2.58f, 2.02f), "PLANTAS", 1.45f, new Color32(255, 245, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Roupas", new Vector3(29.8f, 2.58f, 1.72f), "ROUPAS", 1.45f, new Color32(255, 245, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Sucos", new Vector3(34.0f, 2.58f, 1.72f), "SUCOS & REFRI", 1.30f, new Color32(255, 245, 224, 255), new Vector2(2.6f, 0.40f));

        // 6a. Mega Life Blender Landmarks: Restaurant "Batata & Mar" + 3 Seated Tables, Garden Bed, Fishing Pier, Beach Tennis, Camping, Notice Board!
        GameObject restKitchen = PlaceModel(root.transform, "RestaurantFriesKitchen", "Restaurante Batata e Mar", new Vector3(-14.2f, 0f, -6.2f), 180f, 1f, cozyMats);
        if (restKitchen != null) AddBlockingBox(restKitchen, new Vector3(0f, 1.3f, 0f), new Vector3(4.0f, 2.6f, 2.1f), obstacles);
        Make3DText(root.transform, "Placa Restaurante", new Vector3(-14.2f, 3.05f, -5.15f), "BATATA & MAR", 1.75f, new Color32(255, 246, 218, 255), new Vector2(2.8f, 0.48f));

        List<Transform> restaurantTables = new List<Transform>();
        Vector3[] tableCoords =
        {
            new Vector3(-11.2f, 0f, -1.6f),
            new Vector3(-15.2f, 0f, -1.6f),
            new Vector3(31.8f, 0f, -1.5f)
        };
        for (int i = 0; i < tableCoords.Length; i++)
        {
            GameObject tbl = PlaceModel(root.transform, "RestaurantKioskTable", "Mesa Quiosque Batata Frita " + (i + 1), tableCoords[i], i * 25f, 1f, cozyMats);
            if (tbl != null)
            {
                AddBlockingBox(tbl, new Vector3(0f, 0.7f, 0f), new Vector3(1.65f, 1.4f, 1.65f), obstacles);
                restaurantTables.Add(tbl.transform);
            }
        }

        GameObject gardenBed = PlaceModel(root.transform, "GardenPlanterBed", "Horta da Baia - Canteiros", new Vector3(-3.8f, 0f, -6.4f), 0f, 1f, cozyMats);
        if (gardenBed != null) AddBlockingBox(gardenBed, new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.8f, 1.4f), obstacles);

        GameObject fishingPier = PlaceModel(root.transform, "FishingPierDock", "Pier de Pesca da Baia", new Vector3(31.0f, -0.20f, 8.8f), 0f, 1f, cozyMats);
        GameObject beachTennis = PlaceModel(root.transform, "BeachTennisCourt", "Quadra de Beach Tennis", new Vector3(2.2f, -0.14f, 7.2f), 0f, 1f, cozyMats);
        GameObject campingZone = PlaceModel(root.transform, "CampingTent", "Acampamento Familiar da Praia", new Vector3(34.5f, -0.14f, 6.2f), 168f, 1f, cozyMats);
        if (campingZone != null) AddBlockingBox(campingZone, new Vector3(0f, 0.8f, 0f), new Vector3(2.3f, 1.6f, 2.3f), obstacles);

        GameObject noticeBoard = PlaceModel(root.transform, "VillageNoticeBoard", "Mural de Avisos da Vila", new Vector3(3.6f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (noticeBoard != null) AddBlockingBox(noticeBoard, new Vector3(0f, 1.1f, 0f), new Vector3(2.1f, 2.2f, 0.45f), obstacles);

        // Moored decorative Hawaiian Canoe on the beach
        PlaceModel(root.transform, "HawaiianCanoe", "Canoa Havaiana na Areia", new Vector3(-5.8f, -0.16f, 8.2f), 22f, 1.0f, cozyMats);

        // 6b. 3 Paintable Coastal Houses lining the south side of the street!
        List<Transform> paintableHouses = new List<Transform>();
        float[] houseXs = { -7.2f, 6.2f, 20.4f };
        for (int i = 0; i < houseXs.Length; i++)
        {
            GameObject house = PlaceModel(root.transform, "CoastalHouse", "Casa Costeira Pintavel " + (i + 1), new Vector3(houseXs[i], -0.08f, -10.2f), 0f, 1f, cozyMats);
            if (house != null)
            {
                AddBlockingBox(house, new Vector3(0f, 1.5f, 0f), new Vector3(4.2f, 3.0f, 3.3f), obstacles);
                paintableHouses.Add(house.transform);
            }
        }

        List<Transform> swayingFestoons = new List<Transform>();
        float[] festoonXs = { 11.1f, 15.6f, 20.1f };
        for (int i = 0; i < festoonXs.Length; i++)
        {
            GameObject fest = PlaceModel(root.transform, "FestoonLights", "Varal de Bandeirinhas " + (i + 1), new Vector3(festoonXs[i], 2.75f, 2.05f), 0f, 1f, cozyMats);
            if (fest != null) swayingFestoons.Add(fest.transform);
        }

        // 7. Arched Coconut Palms along the coastal mureta
        List<Transform> swayingPalms = new List<Transform>();
        Vector4[] palmSpecs =
        {
            new Vector4(-5.2f, 3.15f, -12f, 1.05f),
            new Vector4(4.3f, 3.15f, 18f, 0.98f),
            new Vector4(8.6f, 3.15f, -160f, 0.96f),
            new Vector4(17.8f, 3.15f, 14f, 1.04f),
            new Vector4(27.8f, 3.15f, -155f, 1.08f)
        };
        for (int i = 0; i < palmSpecs.Length; i++)
        {
            Vector4 p = palmSpecs[i];
            GameObject palm = PlaceModel(root.transform, "PalmTree", "Coqueiro da Orla " + (i + 1), new Vector3(p.x, 0f, p.y), p.z, p.w, cozyMats);
            if (palm != null)
            {
                swayingPalms.Add(palm.transform);
                AddBlockingBox(palm, new Vector3(0f, 2.3f, 0f), new Vector3(0.58f, 4.6f, 0.58f), obstacles);
            }
        }

        // 8. Planter Bushes, Streetlamps + Warm Lights, Coastal Benches, Route Signpost
        List<Light> warmLights = new List<Light>();
        List<Transform> idleCharacters = new List<Transform>();

        float[] bushXs = { -4.4f, 10.6f, 13.8f, 17.0f, 20.1f, 27.6f };
        for (int i = 0; i < bushXs.Length; i++)
        {
            GameObject bush = PlaceModel(root.transform, "PlanterBush", "Jardineira Costeira " + (i + 1), new Vector3(bushXs[i], 0f, -0.20f), 0f, 1f, cozyMats);
            if (bush != null) AddBlockingBox(bush, new Vector3(0f, 0.45f, 0f), new Vector3(1.65f, 0.9f, 0.80f), obstacles);
        }

        Vector2[] lampCoords =
        {
            new Vector2(-3.0f, 1.85f),
            new Vector2(3.0f, 1.85f),
            new Vector2(12.2f, -0.35f),
            new Vector2(18.6f, -0.35f),
            new Vector2(21.2f, 1.85f),
            new Vector2(26.8f, 1.85f)
        };
        for (int i = 0; i < lampCoords.Length; i++)
        {
            float rotY = lampCoords[i].x > 15f ? 180f : 0f;
            GameObject lamp = PlaceModel(root.transform, "StreetLamp", "Poste Costeiro " + (i + 1), new Vector3(lampCoords[i].x, 0f, lampCoords[i].y), rotY, 1f, cozyMats);
            if (lamp != null) AddBlockingBox(lamp, new Vector3(0f, 2.1f, 0f), new Vector3(0.48f, 4.2f, 0.48f), obstacles);
            float lightOffsetX = rotY > 90f ? -0.52f : 0.52f;
            warmLights.Add(CreateWarmLight(root.transform, "Luz Poste " + (i + 1), new Vector3(lampCoords[i].x + lightOffsetX, 2.75f, lampCoords[i].y), 1.45f, 7.5f));
        }
        // Cozy kiosk & florist awning glow
        warmLights.Add(CreateWarmLight(root.transform, "Luz Quiosque", new Vector3(0f, 2.4f, 1.35f), 1.7f, 8.5f));
        warmLights.Add(CreateWarmLight(root.transform, "Luz Florista", new Vector3(24f, 2.4f, 1.35f), 1.6f, 8.5f));

        float[] benchXs = { -1.6f, 4.0f, 9.2f, 14.4f, 19.6f, 25.0f };
        for (int i = 0; i < benchXs.Length; i++)
        {
            GameObject bench = PlaceModel(root.transform, "CoastalBench", "Banco da Orla " + (i + 1), new Vector3(benchXs[i], 0f, -3.10f), 180f, 1f, cozyMats);
            if (bench != null) AddBlockingBox(bench, new Vector3(0f, 0.48f, 0f), new Vector3(2.0f, 0.96f, 0.82f), obstacles);
        }

        // Route Signpost at Bifurcation
        GameObject signpost = PlaceModel(root.transform, "RouteSignpost", "Placa da Bifurcacao", new Vector3(6.4f, 0f, 0.35f), 10f, 1f, cozyMats);
        if (signpost != null)
        {
            AddBlockingBox(signpost, new Vector3(0f, 0.8f, 0f), new Vector3(0.62f, 1.6f, 0.62f), obstacles);
            Make3DText(signpost.transform, "Placa Feira", new Vector3(0.14f, 1.54f, -0.07f), "FEIRA →", 1.25f, new Color32(255, 248, 232, 255), new Vector2(1.4f, 0.35f));
            Make3DText(signpost.transform, "Placa Orla", new Vector3(0.14f, 1.12f, -0.07f), "ORLA →", 1.25f, new Color32(255, 248, 232, 255), new Vector2(1.4f, 0.35f));
        }

        // 9. Beach Props (Umbrellas, Lounge Chairs, Lifeguard Towers — spaced cleanly around the Pavilion!)
        float[] umbXs = { -2.5f, 5.2f, 21.8f };
        for (int i = 0; i < umbXs.Length; i++)
        {
            GameObject umbrella = PlaceModel(root.transform, "BeachUmbrella", "Guarda-Sol " + (i + 1), new Vector3(umbXs[i], -0.10f, 6.2f), -i * 35f, 1f, cozyMats);
            if (umbrella != null) AddBlockingBox(umbrella, new Vector3(0f, 1.3f, 0f), new Vector3(0.38f, 2.6f, 0.38f), obstacles);
            GameObject chair = PlaceModel(root.transform, "BeachChair", "Cadeira de Praia " + (i + 1), new Vector3(umbXs[i] + 1.1f, -0.10f, 6.1f), -165f, 1f, cozyMats);
            if (chair != null) AddBlockingBox(chair, new Vector3(0f, 0.32f, 0f), new Vector3(1.25f, 0.64f, 0.90f), obstacles);
        }
        GameObject towerA = PlaceModel(root.transform, "LifeguardTower", "Posto Salva-Vidas 1", new Vector3(9.4f, -0.14f, 7.0f), 180f, 1f, cozyMats);
        if (towerA != null) AddBlockingBox(towerA, new Vector3(0f, 1.25f, 0f), new Vector3(1.8f, 2.5f, 1.8f), obstacles);
        GameObject towerB = PlaceModel(root.transform, "LifeguardTower", "Posto Salva-Vidas 2", new Vector3(26.2f, -0.14f, 6.8f), 175f, 1f, cozyMats);
        if (towerB != null) AddBlockingBox(towerB, new Vector3(0f, 1.25f, 0f), new Vector3(1.8f, 2.5f, 1.8f), obstacles);

        // 10. Feira Obstacles (Crates & Chibi Pedestrians)
        GameObject crateOr = PlaceModel(root.transform, "CrateOranges", "Caixa de Laranjas", new Vector3(10.2f, 0f, 1.35f), -12f, 1f, cozyMats);
        if (crateOr != null) AddBlockingBox(crateOr, new Vector3(0f, 0.4f, 0f), new Vector3(0.95f, 0.8f, 0.95f), obstacles);

        GameObject crateFl = PlaceModel(root.transform, "CrateFlowers", "Caixa de Flores", new Vector3(14.2f, 0f, 1.15f), 8f, 1f, cozyMats);
        if (crateFl != null) AddBlockingBox(crateFl, new Vector3(0f, 0.4f, 0f), new Vector3(0.95f, 0.8f, 0.95f), obstacles);

        GameObject pedBlue = PlaceModel(root.transform, "Char_PedestrianBlue", "Beto Praiano", new Vector3(17.2f, 0f, 1.55f), 25f, 1f, cozyMats);
        if (pedBlue != null)
        {
            AddBlockingBox(pedBlue, new Vector3(0f, 0.75f, 0f), new Vector3(0.75f, 1.5f, 0.75f), obstacles);
            idleCharacters.Add(pedBlue.transform);
        }

        GameObject pedCoral = PlaceModel(root.transform, "Char_PedestrianCoral", "Clara da Feira", new Vector3(19.6f, 0f, 1.15f), -22f, 1f, cozyMats);
        if (pedCoral != null)
        {
            AddBlockingBox(pedCoral, new Vector3(0f, 0.75f, 0f), new Vector3(0.75f, 1.5f, 0.75f), obstacles);
            idleCharacters.Add(pedCoral.transform);
        }

        // 11. Ice Cream Cart (+ Painted Sign Upgrade + Basil Pot), Two Friends (Nina & Bia), Articulated Protagonist & Florist
        GameObject cart = PlaceModel(root.transform, "IceCreamCart", "Carrinho de Sorvete", new Vector3(1.15f, 0f, -1.75f), 6f, 1f, cozyMats);

        // Choice Upgrade 2: Hand-painted Wooden Crest Sign on Cart (hidden until chosen!)
        GameObject cartPaintedSignUpgrade = null;
        if (cart != null)
        {
            cartPaintedSignUpgrade = PlaceModel(cart.transform, "CartPaintedSignUpgrade", "Melhoria - Placa Pintada", new Vector3(0f, 1.62f, -0.52f), 0f, 0.95f, cozyMats);
            if (cartPaintedSignUpgrade != null) cartPaintedSignUpgrade.SetActive(false);
        }

        // Florist's gifted Basil Pot (appears beside cart after first delivery!)
        GameObject basilPot = PlaceModel(root.transform, "BasilPot", "Vaso de Manjericao da Florista", new Vector3(2.42f, 0f, -1.65f), -12f, 1.15f, cozyMats);
        if (basilPot != null) basilPot.SetActive(false);

        // Amiga 1: Nina (already at the Kiosk helping prepare the order!)
        GameObject friendNina = PlaceModel(root.transform, "Char_FriendNina", "Amiga 1 - Nina (Quiosque)", new Vector3(-2.15f, 0f, 0.95f), -18f, 1f, cozyMats);
        if (friendNina != null)
        {
            AddBlockingBox(friendNina, new Vector3(0f, 0.75f, 0f), new Vector3(0.70f, 1.5f, 0.70f), obstacles);
            idleCharacters.Add(friendNina.transform);
        }

        // Amiga 2: Bia (at the Square/Bifurcation guiding to the Feira shortcut!)
        GameObject friendBia = PlaceModel(root.transform, "Char_FriendBia", "Amiga 2 - Bia (Praca)", new Vector3(5.85f, 0f, 0.72f), 14f, 1f, cozyMats);
        if (friendBia != null)
        {
            AddBlockingBox(friendBia, new Vector3(0f, 0.75f, 0f), new Vector3(0.70f, 1.5f, 0.70f), obstacles);
            idleCharacters.Add(friendBia.transform);
        }

        GameObject playerRoot = new GameObject("Protagonista 3D");
        playerRoot.transform.SetParent(root.transform, false);
        playerRoot.transform.position = new Vector3(-1.25f, 0f, -1.75f);
        GameObject playerVisual = PlaceModel(playerRoot.transform, "Char_Protagonist", "Visual Protagonista", Vector3.zero, -20f, 1f, cozyMats);
        GameObject heldCone = PlaceModel(playerRoot.transform, "IceCreamConeLemonBasil", "Casquinha Limao Manjericao na Mao", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSurfboard = PlaceModel(playerRoot.transform, "Surfboard", "Prancha de Surf da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkate = PlaceModel(playerRoot.transform, "Skateboard", "Skate da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerScooter = PlaceModel(playerRoot.transform, "Scooter", "Patinete da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkates = PlaceModel(playerRoot.transform, "RollerSkates", "Patins da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerCanoe = PlaceModel(playerRoot.transform, "HawaiianCanoe", "Canoa Havaiana da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerHat = PlaceModel(playerRoot.transform, "WardrobeHatProp", "Chapeu e Oculos da Protagonista", Vector3.zero, 0f, 1f, cozyMats);

        // Decorative beach surfboard standing near Lifeguard Tower 1
        PlaceModel(root.transform, "Surfboard", "Prancha na Areia", new Vector3(8.0f, 0.25f, 6.8f), 32f, 1.05f, cozyMats);

        // Cute Pet Companions: Dog "Pipoca" (+ Bandana Prop!) & Cat "Mingau"!
        GameObject dogObj = PlaceModel(root.transform, "PetDog", "Cachorro Pipoca", new Vector3(-3.0f, 0f, -1.55f), -35f, 1.05f, cozyMats);
        GameObject pipocaBandana = dogObj != null ? PlaceModel(dogObj.transform, "PetBandanaProp", "Bandana do Pipoca", Vector3.zero, 0f, 1f, cozyMats) : null;
        GirlPowerOrla3DPetCompanion petDog = null;
        if (dogObj != null)
        {
            petDog = dogObj.AddComponent<GirlPowerOrla3DPetCompanion>();
            petDog.petName = "Pipoca";
            petDog.isCat = false;
            petDog.followTarget = playerRoot.transform;
            petDog.guardPoint = cart != null ? cart.transform : null;
            petDog.bandanaProp = pipocaBandana != null ? pipocaBandana.transform : null;
            petDog.followOffset = new Vector3(-1.15f, 0f, -0.75f);
        }

        GameObject catObj = PlaceModel(root.transform, "PetCat", "Gato Mingau", new Vector3(3.6f, 0f, -1.45f), 25f, 1.05f, cozyMats);
        GirlPowerOrla3DPetCompanion petCat = null;
        if (catObj != null)
        {
            petCat = catObj.AddComponent<GirlPowerOrla3DPetCompanion>();
            petCat.petName = "Mingau";
            petCat.isCat = true;
            petCat.followTarget = playerRoot.transform;
            petCat.followOffset = new Vector3(1.15f, 0f, -0.80f);
        }

        GameObject florist = PlaceModel(root.transform, "Char_Florist", "Florista Luna", new Vector3(24.0f, 0f, 0.25f), 15f, 1f, cozyMats);
        if (florist != null) idleCharacters.Add(florist.transform);

        // 11b. Living Sky & Coastal Wildlife (Clouds, Seagulls, Butterflies, Beach Crabs, Fireflies!)
        List<Transform> clouds = new List<Transform>();
        Vector3[] cloudPositions =
        {
            new Vector3(-14f, 11.5f, 22f),
            new Vector3(1f, 13.0f, 28f),
            new Vector3(16f, 12.2f, 25f),
            new Vector3(32f, 13.4f, 30f),
            new Vector3(44f, 11.8f, 23f)
        };
        for (int i = 0; i < cloudPositions.Length; i++)
        {
            GameObject c = PlaceModel(root.transform, "CloudFluffy", "Nuvem Fofinha " + (i + 1), cloudPositions[i], i * 18f, 1.45f + (i % 2) * 0.35f, cozyMats);
            if (c != null) clouds.Add(c.transform);
        }

        List<Transform> seagulls = new List<Transform>();
        Vector3[] gullPositions =
        {
            new Vector3(-4.5f, 5.1f, 13.8f),
            new Vector3(1.0f, 5.4f, 12.5f),
            new Vector3(14.5f, 6.2f, 14.5f),
            new Vector3(22.0f, 5.3f, 15.2f),
            new Vector3(28.5f, 5.7f, 12.2f)
        };
        for (int i = 0; i < gullPositions.Length; i++)
        {
            GameObject g = PlaceModel(root.transform, "Seagull3D", "Gaivota Costeira " + (i + 1), gullPositions[i], 0f, 1.15f, cozyMats);
            if (g != null) seagulls.Add(g.transform);
        }

        List<Transform> butterflies = new List<Transform>();
        Vector3[] bfPositions =
        {
            new Vector3(-3.2f, 1.35f, 0.6f),
            new Vector3(13.8f, 1.25f, -0.1f),
            new Vector3(22.6f, 1.45f, 1.2f),
            new Vector3(25.2f, 1.55f, 1.5f)
        };
        for (int i = 0; i < bfPositions.Length; i++)
        {
            GameObject bf = PlaceModel(root.transform, "Butterfly3D", "Borboleta da Orla " + (i + 1), bfPositions[i], 0f, 1.1f, cozyMats);
            if (bf != null) butterflies.Add(bf.transform);
        }

        List<Transform> crabs = new List<Transform>();
        Vector3[] crabPositions =
        {
            new Vector3(1.5f, -0.15f, 6.8f),
            new Vector3(11.8f, -0.18f, 7.6f),
            new Vector3(22.2f, -0.16f, 7.1f)
        };
        for (int i = 0; i < crabPositions.Length; i++)
        {
            GameObject crab = PlaceModel(root.transform, "BeachCrab", "Caranguejo da Praia " + (i + 1), crabPositions[i], 0f, 1.0f, cozyMats);
            if (crab != null) crabs.Add(crab.transform);
        }

        List<Transform> fireflies = new List<Transform>();
        Material glowMat = cozyMats.TryGetValue("AC_LanternGlow", out Material lm) ? lm : null;
        Vector3[] fireflyPositions =
        {
            new Vector3(-3.0f, 1.6f, 1.5f),
            new Vector3(3.0f, 1.7f, 1.5f),
            new Vector3(11.8f, 1.4f, -0.1f),
            new Vector3(18.2f, 1.5f, -0.1f),
            new Vector3(21.4f, 1.6f, 1.5f),
            new Vector3(26.4f, 1.6f, 1.5f)
        };
        for (int i = 0; i < fireflyPositions.Length; i++)
        {
            GameObject ff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ff.name = "Vagalume Dourado " + (i + 1);
            ff.transform.SetParent(root.transform, false);
            ff.transform.position = fireflyPositions[i];
            ff.transform.localScale = Vector3.one * 0.10f;
            Object.DestroyImmediate(ff.GetComponent<Collider>());
            Renderer r = ff.GetComponent<Renderer>();
            if (glowMat != null) r.sharedMaterial = glowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fireflies.Add(ff.transform);
        }

        // 11c. Chapter 2 & 3 Story Props: Sister Marina, 4 Scattered Florist Cards & Wind Gust Petals!
        GameObject sisterMarina = PlaceModel(root.transform, "Char_SisterMarina", "Marina - Irma da Florista", new Vector3(-7.6f, 0f, -1.65f), -25f, 1f, cozyMats);
        if (sisterMarina != null)
        {
            idleCharacters.Add(sisterMarina.transform);
            sisterMarina.SetActive(false);
        }

        Vector3[] cardPositions =
        {
            new Vector3(4.2f, 0.05f, -2.1f),   // Boardwalk near benches
            new Vector3(7.6f, 0.05f, 0.85f),   // Square near Bia
            new Vector3(16.4f, 0.05f, 1.35f),  // Feira cobblestone lane
            new Vector3(15.6f, 0.40f, 4.15f)   // Pavilion entrance steps (the Sister's invitation!)
        };
        List<GameObject> scatteredCards = new List<GameObject>();
        List<Transform> cardTransforms = new List<Transform>();
        for (int i = 0; i < cardPositions.Length; i++)
        {
            GameObject card = PlaceModel(root.transform, "FloristCard", "Cartao da Florista " + (i + 1), cardPositions[i], i * 28f, 1.05f, cozyMats);
            if (card != null)
            {
                card.SetActive(false);
                scatteredCards.Add(card);
                cardTransforms.Add(card.transform);
            }
        }

        // Drifting pastel petals during Chapter 2 Wind Gust
        List<Transform> windPetals = new List<Transform>();
        string[] petalMatNames = { "AC_PinkSoft", "AC_FlowerYellow", "AC_Cream", "AC_MintSoft" };
        for (int i = 0; i < 18; i++)
        {
            GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            petal.name = "Petala Ventania " + (i + 1);
            petal.transform.SetParent(root.transform, false);
            petal.transform.position = new Vector3(-8f + (i * 2.4f), 1.4f + (i % 4) * 0.55f, -2.5f + (i % 5) * 2.1f);
            petal.transform.localScale = new Vector3(0.18f, 0.04f, 0.12f);
            Object.DestroyImmediate(petal.GetComponent<Collider>());
            Renderer pr = petal.GetComponent<Renderer>();
            if (cozyMats.TryGetValue(petalMatNames[i % petalMatNames.Length], out Material pMat))
                pr.sharedMaterial = pMat;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            petal.SetActive(false);
            windPetals.Add(petal.transform);
        }

        // 11d. Rich Coastal Microdetails, Boats, Rocks, Pier, Trash Bins, Signs, Cables, Towels & Ambient Particles!
        GirlPowerOrla3DPropsBuilder.PropsResult coastalDetails =
            GirlPowerOrla3DPropsBuilder.PopulateCoastalDetails(root.transform, cozyMats, obstacles, lampCoords);

        // 11e. Camera Occlusion Dither Fader (houses, kiosk, florist stand, market stalls, restaurant & pavilion fade cleanly when avatar walks behind them!)
        List<Transform> occluderRoots = new List<Transform>();
        occluderRoots.AddRange(paintableHouses);
        foreach (GameObject occ in new[] { kiosk, kioskAwningUpgrade, floristStand, pavilion, restKitchen, stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (occ != null) occluderRoots.Add(occ.transform);

        GameObject occObj = new GameObject("Oclusao_Transparencia_Construcoes");
        occObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DOcclusionFader occFader = occObj.AddComponent<GirlPowerOrla3DOcclusionFader>();
        occFader.player = playerRoot.transform;
        occFader.targetCamera = mainCam;
        occFader.occluderRoots = occluderRoots.ToArray();
        occFader.cutoutRadius = 2.35f;

        // 11f. Contact / Blob Shadows anchoring every character, pet, cart, bench, stall, table & prop to the ground!
        List<GirlPowerOrla3DContactShadows.ShadowEntry> shadowEntries = new List<GirlPowerOrla3DContactShadows.ShadowEntry>();
        AddShadow(shadowEntries, playerRoot.transform, new Vector2(1.05f, 1.05f), true);
        if (friendNina != null) AddShadow(shadowEntries, friendNina.transform, new Vector2(1.0f, 1.0f), true);
        if (friendBia != null) AddShadow(shadowEntries, friendBia.transform, new Vector2(1.0f, 1.0f), true);
        if (florist != null) AddShadow(shadowEntries, florist.transform, new Vector2(1.05f, 1.05f), true);
        if (sisterMarina != null) AddShadow(shadowEntries, sisterMarina.transform, new Vector2(1.0f, 1.0f), true);
        if (pedBlue != null) AddShadow(shadowEntries, pedBlue.transform, new Vector2(1.0f, 1.0f), true);
        if (pedCoral != null) AddShadow(shadowEntries, pedCoral.transform, new Vector2(1.0f, 1.0f), true);
        if (dogObj != null) AddShadow(shadowEntries, dogObj.transform, new Vector2(0.82f, 0.82f), true);
        if (catObj != null) AddShadow(shadowEntries, catObj.transform, new Vector2(0.75f, 0.75f), true);

        if (cart != null) AddShadow(shadowEntries, cart.transform, new Vector2(2.35f, 1.65f), false);
        if (basilPot != null) AddShadow(shadowEntries, basilPot.transform, new Vector2(0.75f, 0.75f), true);
        if (signpost != null) AddShadow(shadowEntries, signpost.transform, new Vector2(0.85f, 0.85f), false);
        if (gardenBed != null) AddShadow(shadowEntries, gardenBed.transform, new Vector2(2.90f, 1.65f), false);
        if (noticeBoard != null) AddShadow(shadowEntries, noticeBoard.transform, new Vector2(2.35f, 0.85f), false);
        if (campingZone != null) AddShadow(shadowEntries, campingZone.transform, new Vector2(2.80f, 2.80f), false);
        foreach (Transform tbl in restaurantTables)
            if (tbl != null) AddShadow(shadowEntries, tbl, new Vector2(2.75f, 2.75f), false);
        if (crateOr != null) AddShadow(shadowEntries, crateOr.transform, new Vector2(1.25f, 1.25f), false);
        if (crateFl != null) AddShadow(shadowEntries, crateFl.transform, new Vector2(1.25f, 1.25f), false);
        foreach (GameObject st in new[] { stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (st != null) AddShadow(shadowEntries, st.transform, new Vector2(3.15f, 1.80f), false);

        // 11g. Living Ecology & Roaming Villagers (Strolling Tourists, Startled Beach Seagulls & Swimming Shallow Fish!)
        List<Transform> strollingNpcs = new List<Transform>();
        GameObject walker1 = PlaceModel(root.transform, "Char_PedestrianBlue", "Turista Calcadão 1", new Vector3(-6.2f, 0f, -1.2f), 20f, 1f, cozyMats);
        GameObject walker2 = PlaceModel(root.transform, "Char_PedestrianCoral", "Turista Calcadão 2", new Vector3(11.4f, 0f, -1.4f), -18f, 1f, cozyMats);
        GameObject camper1 = PlaceModel(root.transform, "Char_FriendBia", "Familia Acampamento", new Vector3(33.2f, -0.14f, 4.9f), 145f, 1f, cozyMats);
        if (walker1 != null) { strollingNpcs.Add(walker1.transform); AddShadow(shadowEntries, walker1.transform, new Vector2(1.0f, 1.0f), true); }
        if (walker2 != null) { strollingNpcs.Add(walker2.transform); AddShadow(shadowEntries, walker2.transform, new Vector2(1.0f, 1.0f), true); }
        if (camper1 != null) { idleCharacters.Add(camper1.transform); AddShadow(shadowEntries, camper1.transform, new Vector2(1.0f, 1.0f), true); }

        List<Transform> beachSeagulls = new List<Transform>();
        Vector3[] perchedGulls = { new Vector3(-2.0f, -0.14f, 7.6f), new Vector3(10.8f, -0.15f, 7.8f), new Vector3(23.2f, -0.14f, 7.5f) };
        for (int i = 0; i < perchedGulls.Length; i++)
        {
            GameObject bg = PlaceModel(root.transform, "Seagull3D", "Gaivota na Areia " + (i + 1), perchedGulls[i], i * 45f, 0.92f, cozyMats);
            if (bg != null) beachSeagulls.Add(bg.transform);
        }

        List<Transform> shallowFish = new List<Transform>();
        for (int i = 0; i < 6; i++)
        {
            GameObject fish = PlaceModel(root.transform, "FishProp", "Peixe Tropical " + (i + 1), new Vector3(-6f + i * 7.2f, -0.26f, 11.4f + (i % 2) * 1.4f), i * 35f, 1.0f, cozyMats);
            if (fish != null) shallowFish.Add(fish.transform);
        }

        GameObject npcAiObj = new GameObject("IA_Moradores_e_Fauna_Local");
        npcAiObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DNpcAI npcAi = npcAiObj.AddComponent<GirlPowerOrla3DNpcAI>();
        npcAi.player = playerRoot.transform;
        npcAi.strollingNpcs = strollingNpcs.ToArray();
        npcAi.beachSeagulls = beachSeagulls.ToArray();
        npcAi.shallowFish = shallowFish.ToArray();
        foreach (Transform child in root.transform)
        {
            if (child.name.StartsWith("Banco da Orla")) AddShadow(shadowEntries, child, new Vector2(2.30f, 1.15f), false);
            else if (child.name.StartsWith("Jardineira Costeira")) AddShadow(shadowEntries, child, new Vector2(1.95f, 1.05f), false);
            else if (child.name.StartsWith("Guarda-Sol")) AddShadow(shadowEntries, child, new Vector2(2.60f, 2.60f), false);
            else if (child.name.StartsWith("Cadeira de Praia")) AddShadow(shadowEntries, child, new Vector2(1.55f, 1.20f), false);
            else if (child.name.StartsWith("Poste Costeiro")) AddShadow(shadowEntries, child, new Vector2(0.95f, 0.95f), false);
        }
        shadowEntries.AddRange(coastalDetails.extraShadows);

        GameObject shadowObj = new GameObject("Sombras_de_Contato_Orla");
        shadowObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DContactShadows contactShadows = shadowObj.AddComponent<GirlPowerOrla3DContactShadows>();
        contactShadows.entries = shadowEntries.ToArray();

        // 12. Audio, Dialogue, Polaroid Photo Overlay, Character Animator, Ambient, Story & GameFlow Controllers
        GameObject audioObj = new GameObject("Audio Costeiro - Baia do Avesso");
        audioObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DAudio audioCtrl = audioObj.AddComponent<GirlPowerOrla3DAudio>();

        GameObject dialogueObj = new GameObject("Dialogo Animal Crossing");
        dialogueObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DDialogue dialogueCtrl = dialogueObj.AddComponent<GirlPowerOrla3DDialogue>();
        dialogueCtrl.audioController = audioCtrl;

        GameObject photoObj = new GameObject("Fotografia Polaroid - Duas Irmas");
        photoObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DPhotoOverlay photoCtrl = photoObj.AddComponent<GirlPowerOrla3DPhotoOverlay>();

        GirlPowerOrla3DCharacterAnimator charAnim = playerRoot.AddComponent<GirlPowerOrla3DCharacterAnimator>();
        charAnim.visualRoot = playerVisual != null ? playerVisual.transform : playerRoot.transform;
        charAnim.heldIceCreamCone = heldCone != null ? heldCone.transform : null;
        charAnim.surfboardProp = playerSurfboard != null ? playerSurfboard.transform : null;
        charAnim.skateboardProp = playerSkate != null ? playerSkate.transform : null;
        charAnim.scooterProp = playerScooter != null ? playerScooter.transform : null;
        charAnim.rollerSkatesProp = playerSkates != null ? playerSkates.transform : null;
        charAnim.hawaiianCanoeProp = playerCanoe != null ? playerCanoe.transform : null;
        charAnim.wardrobeHatProp = playerHat != null ? playerHat.transform : null;
        charAnim.audioController = audioCtrl;
        if (petDog != null) petDog.audioController = audioCtrl;
        if (petCat != null) petCat.audioController = audioCtrl;

        GameObject juiceObj = new GameObject("Juice_e_MicroInteracoes");
        juiceObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DJuice juice = juiceObj.AddComponent<GirlPowerOrla3DJuice>();
        juice.sceneCamera = mainCam;
        juice.audioController = audioCtrl;

        GameObject paintObj = new GameObject("Sistema de Pintura - Rua e Casas");
        paintObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DPaintSystem paintSys = paintObj.AddComponent<GirlPowerOrla3DPaintSystem>();
        paintSys.paintableHouses = paintableHouses.ToArray();
        paintSys.audioController = audioCtrl;

        GameObject lifeObj = new GameObject("Atividades da Orla - Lojas, Amizades, Pets e Pintura");
        lifeObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DLifeActivities lifeAct = lifeObj.AddComponent<GirlPowerOrla3DLifeActivities>();
        lifeAct.player = playerRoot.transform;
        lifeAct.sceneCamera = mainCam;
        lifeAct.characterAnimator = charAnim;
        lifeAct.audioController = audioCtrl;
        lifeAct.dialogueController = dialogueCtrl;
        lifeAct.paintSystem = paintSys;
        lifeAct.petDog = petDog;
        lifeAct.petCat = petCat;
        lifeAct.shops = new[]
        {
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Água de Coco da Orla", category = "Bebidas Tropicais",
                stallTransform = stallCoco != null ? stallCoco.transform : null,
                itemNames = new[] { "Água de Coco Gelada", "Coco com Limão", "Cocada Cremosa" },
                itemPrices = new[] { 4, 5, 6 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Artesanato da Baía", category = "Feito à Mão",
                stallTransform = stallArt != null ? stallArt.transform : null,
                itemNames = new[] { "Colar de Conchas", "Pulseira da Baía", "Mini Quadro Costeiro" },
                itemPrices = new[] { 6, 5, 8 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Plantas Naturais", category = "Jardinagem",
                stallTransform = stallPlantas != null ? stallPlantas.transform : null,
                itemNames = new[] { "Mudinha de Suculenta", "Samambaia da Orla", "Vaso Flor do Sol" },
                itemPrices = new[] { 5, 7, 8 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Sushi da Baía", category = "Culinária Japonesa",
                stallTransform = stallSushi != null ? stallSushi.transform : null,
                itemNames = new[] { "Combinado de Salmão", "Hot Roll Tropical", "Temaki Especial" },
                itemPrices = new[] { 8, 7, 9 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Comidas & Açaí", category = "Lanches da Orla",
                stallTransform = stallComida != null ? stallComida.transform : null,
                itemNames = new[] { "Pastel Dourado", "Tigela de Açaí", "Hambúrguer Praiano" },
                itemPrices = new[] { 5, 7, 8 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Moda Praia & Roupas", category = "Trocar Roupa Agora!",
                stallTransform = stallRoupas != null ? stallRoupas.transform : null,
                itemNames = new[] { "Blusa Rosa Berry", "Blusa Menta Praia", "Blusa Amarelo Sol", "Blusa Azul Mar", "Blusa Lavanda" },
                itemPrices = new[] { 6, 6, 6, 6, 6 }, isClothingShop = true
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Doces & Cupcakes da Baía", category = "Guloseimas",
                stallTransform = stallDoces != null ? stallDoces.transform : null,
                itemNames = new[] { "Brigadeiro", "Cupcake Berry", "Tortinha de Limão" },
                itemPrices = new[] { 4, 6, 7 }, isClothingShop = false
            },
            new GirlPowerOrla3DLifeActivities.ShopStall
            {
                shopName = "Sucos, Água & Refri", category = "Bebidas Geladas",
                stallTransform = stallSucos != null ? stallSucos.transform : null,
                itemNames = new[] { "Água Mineral", "Refri Guaraná", "Suco de Limão" },
                itemPrices = new[] { 3, 5, 5 }, isClothingShop = false
            }
        };
        lifeAct.friends = new[]
        {
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Nina", target = friendNina != null ? friendNina.transform : null, tagColor = new Color(0.24f, 0.70f, 0.62f, 1f), voicePitch = 1.18f },
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Bia", target = friendBia != null ? friendBia.transform : null, tagColor = new Color(0.62f, 0.44f, 0.84f, 1f), voicePitch = 1.24f },
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Florista Luna", target = florist != null ? florist.transform : null, tagColor = new Color(0.76f, 0.44f, 0.72f, 1f), voicePitch = 1.20f },
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Marina", target = sisterMarina != null ? sisterMarina.transform : null, tagColor = new Color(0.92f, 0.46f, 0.38f, 1f), voicePitch = 1.12f },
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Beto", target = pedBlue != null ? pedBlue.transform : null, tagColor = new Color(0.28f, 0.64f, 0.86f, 1f), voicePitch = 0.88f },
            new GirlPowerOrla3DLifeActivities.FriendEntry { friendName = "Clara", target = pedCoral != null ? pedCoral.transform : null, tagColor = new Color(0.92f, 0.48f, 0.38f, 1f), voicePitch = 1.18f }
        };

        GameObject ambientObj = new GameObject("Ambientacao Baia do Avesso");
        ambientObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DAmbient ambient = ambientObj.AddComponent<GirlPowerOrla3DAmbient>();
        ambient.swayingPalms = swayingPalms.ToArray();
        ambient.swayingFestoons = swayingFestoons.ToArray();
        ambient.idleCharacters = idleCharacters.ToArray();
        ambient.warmLanternLights = warmLights.ToArray();
        ambient.clouds = clouds.ToArray();
        ambient.seagulls = seagulls.ToArray();
        ambient.butterflies = butterflies.ToArray();
        ambient.beachCrabs = crabs.ToArray();
        ambient.fireflies = fireflies.ToArray();
        ambient.breezeCurtains = breezeCurtains.ToArray();
        ambient.floatingBoats = coastalDetails.floatingBoats.ToArray();
        ambient.floatingBuoys = coastalDetails.floatingBuoys.ToArray();
        ambient.ambientLeaves = coastalDetails.ambientLeaves.ToArray();
        ambient.waterSparkles = coastalDetails.waterSparkles.ToArray();
        ambient.windPetals = windPetals.ToArray();
        ambient.collectibleCards = cardTransforms.ToArray();
        ambient.pavilionLights = pavilionPointLights.ToArray();
        ambient.oceanSurface = oceanSurface;
        ambient.floristRoutineTarget = florist != null ? florist.transform : null;
        ambient.floristMorningPos = new Vector3(24.0f, 0f, 1.35f);
        ambient.floristEveningPos = new Vector3(24.0f, 0f, 0.20f);
        ambient.sunLight = sunLight;
        ambient.targetCamera = mainCam;
        ambient.cameraPost = mainCam != null ? mainCam.GetComponent<GirlPowerOrla3DCameraPost>() : null;
        ambient.currentPreset = GirlPowerOrla3DAmbient.TimePreset.FimDeTarde;

        GameObject missionObj = new GameObject("Historia Baia do Avesso - 4 Capitulos");
        missionObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DFirstDelivery mission = missionObj.AddComponent<GirlPowerOrla3DFirstDelivery>();
        mission.player = playerRoot.transform;
        mission.playerVisual = playerVisual != null ? playerVisual.transform : playerRoot.transform;
        mission.characterAnimator = charAnim;
        mission.cart = cart != null ? cart.transform : null;
        mission.friendNina = friendNina != null ? friendNina.transform : null;
        mission.friendBia = friendBia != null ? friendBia.transform : null;
        mission.florist = florist != null ? florist.transform : null;
        mission.sisterMarina = sisterMarina != null ? sisterMarina.transform : null;
        mission.pavilion = pavilion != null ? pavilion.transform : null;
        mission.basilPot = basilPot != null ? basilPot.transform : null;
        mission.pedestrianBlue = pedBlue != null ? pedBlue.transform : null;
        mission.pedestrianCoral = pedCoral != null ? pedCoral.transform : null;
        mission.kioskAwningUpgrade = kioskAwningUpgrade;
        mission.cartPaintedSignUpgrade = cartPaintedSignUpgrade;
        mission.scatteredCards = scatteredCards.ToArray();
        mission.pavilionFestivalTable = pavilionFestivalTable;
        mission.pavilionFestivalLights = pavilionFestivalLights;
        mission.pavilionFestivalDishes = pavilionFestivalDishes;
        mission.pavilionFadedText = pavilionFadedText;
        mission.pavilionRestoredText = pavilionRestoredText;
        mission.customSignboard = customSign != null ? customSign.transform : null;
        mission.customSignLabel = customSignLabel;
        mission.sceneCamera = mainCam;
        mission.ambientController = ambient;
        mission.audioController = audioCtrl;
        mission.dialogueController = dialogueCtrl;
        mission.photoOverlay = photoCtrl;
        mission.lifeActivities = lifeAct;
        mission.blockingObstacles = obstacles.ToArray();

        // Complete Game State Machine (Title Menu, Chapters, Settings, Pause, Victory, Game Over)
        GameObject flowObj = new GameObject("Fluxo do Jogo - Menu, Pausa, Vitoria e Derrota");
        flowObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DGameFlow gameFlow = flowObj.AddComponent<GirlPowerOrla3DGameFlow>();
        gameFlow.sceneCamera = mainCam;
        gameFlow.storyController = mission;
        gameFlow.lifeActivities = lifeAct;
        gameFlow.ambientController = ambient;
        gameFlow.audioController = audioCtrl;
        gameFlow.dialogueController = dialogueCtrl;
        gameFlow.photoOverlay = photoCtrl;
        gameFlow.characterAnimator = charAnim;

        // Mega Life Systems (Garden, Fishing, Beach Tennis, Restaurant Tables, Minigames, Notice Board, Restoration, JSON Auto-Save!)
        GameObject megaObj = new GameObject("Sistemas_MegaLife_Horta_Pesca_Restaurante");
        megaObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DMegaSystems megaSys = megaObj.AddComponent<GirlPowerOrla3DMegaSystems>();
        megaSys.player = playerRoot.transform;
        megaSys.sceneCamera = mainCam;
        megaSys.restaurantKitchen = restKitchen != null ? restKitchen.transform : null;
        megaSys.restaurantTables = restaurantTables.ToArray();
        megaSys.gardenBed = gardenBed != null ? gardenBed.transform : null;
        megaSys.fishingPier = fishingPier != null ? fishingPier.transform : null;
        megaSys.beachTennisCourt = beachTennis != null ? beachTennis.transform : null;
        megaSys.noticeBoard = noticeBoard != null ? noticeBoard.transform : null;
        megaSys.campingZone = campingZone != null ? campingZone.transform : null;
        megaSys.fairyLightsGroup = pavilionFestivalLights;
        megaSys.gameFlow = gameFlow;
        megaSys.lifeActivities = lifeAct;
        megaSys.audioController = audioCtrl;
        megaSys.ambientController = ambient;
        megaSys.characterAnimator = charAnim;
        megaSys.paintSystem = paintSys;
        megaSys.petDog = petDog;

        // Illustrated Cardápio Modal (Cozinha do Bosque style!)
        GameObject cardapioObj = new GameObject("Cardapio_Ilustrado_Baia");
        cardapioObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DCardapioUI cardapio = cardapioObj.AddComponent<GirlPowerOrla3DCardapioUI>();
        cardapio.gameFlow = gameFlow;
        cardapio.lifeActivities = lifeAct;
        cardapio.megaSystems = megaSys;
        cardapio.characterAnimator = charAnim;
        cardapio.petDog = petDog;
        cardapio.audioController = audioCtrl;

        gameFlow.megaSystems = megaSys;
        gameFlow.cardapioUI = cardapio;
        mission.gameFlow = gameFlow;
        mission.megaSystems = megaSys;
        mission.cardapioUI = cardapio;
        lifeAct.gameFlow = gameFlow;
        lifeAct.storyController = mission;
        lifeAct.megaSystems = megaSys;
        lifeAct.cardapioUI = cardapio;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[Girl Power Orla 3D] Cena 3D estilo Animal Crossing montada com sucesso em: " + ScenePath);
    }

    public static void BuildFromBatch()
    {
        Build();
    }

    private static void AddShadow(
        List<GirlPowerOrla3DContactShadows.ShadowEntry> list,
        Transform target,
        Vector2 sizeXZ,
        bool isDynamic)
    {
        list.Add(new GirlPowerOrla3DContactShadows.ShadowEntry
        {
            target = target,
            sizeXZ = sizeXZ,
            yOffset = 0f,
            isDynamic = isDynamic
        });
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject camObj = new GameObject("ACNH Diorama Camera");
        camObj.transform.SetParent(parent, false);
        camObj.transform.position = new Vector3(10.5f, 8.1f, -9.8f);
        camObj.transform.rotation = Quaternion.Euler(38.5f, 0f, 0f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 140f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.62f, 0.70f, 0.82f);
        camObj.AddComponent<AudioListener>();
        GirlPowerOrla3DCameraPost post = camObj.AddComponent<GirlPowerOrla3DCameraPost>();
        post.exposure = 0.89f;
        post.contrast = 1.08f;
        post.saturation = 1.14f;
        post.vignetteStrength = 0.17f;
        return cam;
    }

    private static Light CreateSunLight(Transform parent)
    {
        GameObject sunObj = new GameObject("Sol Fim de Tarde - Matinhos");
        sunObj.transform.SetParent(parent, false);
        sunObj.transform.rotation = Quaternion.Euler(33f, -38f, 0f);
        Light l = sunObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1.0f, 0.80f, 0.56f);
        l.intensity = 0.78f;
        l.shadows = LightShadows.Soft;
        l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        l.shadowStrength = 0.70f;
        l.shadowBias = 0.03f;
        l.shadowNormalBias = 0.24f;
        return l;
    }

    private static Light CreateWarmLight(Transform parent, string name, Vector3 pos, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.83f, 0.48f);
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }

    private static Transform CreateAnimatedOcean(Transform parent, Material oceanMat)
    {
        GameObject ocean = new GameObject("Mar Estilizado - Animado");
        ocean.transform.SetParent(parent, false);
        ocean.transform.position = new Vector3(12f, -0.35f, 26f);

        MeshFilter mf = ocean.AddComponent<MeshFilter>();
        MeshRenderer mr = ocean.AddComponent<MeshRenderer>();

        // Subdivided grid mesh for smooth vertex waves & horizon curvature
        Mesh mesh = new Mesh { name = "OrlaOceanGrid" };
        int segsX = 64;
        int segsZ = 28;
        float sizeX = 110f;
        float sizeZ = 32f;
        Vector3[] verts = new Vector3[(segsX + 1) * (segsZ + 1)];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[segsX * segsZ * 6];

        int v = 0;
        for (int z = 0; z <= segsZ; z++)
        {
            float tz = (float)z / segsZ;
            for (int x = 0; x <= segsX; x++)
            {
                float tx = (float)x / segsX;
                verts[v] = new Vector3((tx - 0.5f) * sizeX, 0f, (tz - 0.5f) * sizeZ);
                uvs[v] = new Vector2(tx, tz);
                v++;
            }
        }
        int ti = 0;
        for (int z = 0; z < segsZ; z++)
        {
            for (int x = 0; x < segsX; x++)
            {
                int i0 = z * (segsX + 1) + x;
                int i1 = i0 + 1;
                int i2 = i0 + (segsX + 1);
                int i3 = i2 + 1;
                tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1;
                tris[ti++] = i1; tris[ti++] = i2; tris[ti++] = i3;
            }
        }
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        string meshPath = "Assets/GirlPower/Orla3D/Materials/OrlaOceanGrid.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(meshPath) != null)
            AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);
        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        mr.sharedMaterial = oceanMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return ocean.transform;
    }

    private static GameObject PlaceModel(
        Transform parent,
        string fbxName,
        string instanceName,
        Vector3 position,
        float rotYDeg,
        float scale,
        Dictionary<string, Material> cozyMats)
    {
        string path = ModelsFolder + "/" + fbxName + ".fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[Orla3D] FBX not found: " + path);
            return null;
        }

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = instanceName;
        inst.transform.localPosition = position;
        inst.transform.localRotation = Quaternion.Euler(0f, rotYDeg, 0f);
        inst.transform.localScale = Vector3.one * scale;

        // Upgrade every material slot to our custom Animal Crossing Cozy shader material!
        foreach (MeshRenderer mr in inst.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] shared = mr.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null) continue;
                string key = CleanMatName(shared[i].name);
                if (cozyMats.TryGetValue(key, out Material replacement))
                {
                    shared[i] = replacement;
                    changed = true;
                }
            }
            if (changed) mr.sharedMaterials = shared;
        }

        return inst;
    }

    private static string CleanMatName(string raw)
    {
        int dot = raw.IndexOf('.');
        if (dot > 0) raw = raw.Substring(0, dot);
        int space = raw.IndexOf(' ');
        if (space > 0) raw = raw.Substring(0, space);
        return raw.Trim();
    }

    private static void AddBlockingBox(GameObject go, Vector3 center, Vector3 size, List<BoxCollider> list)
    {
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = false;
        list.Add(box);
    }

    private static TextMeshPro Make3DText(Transform parent, string name, Vector3 localPos, string content, float fontSize, Color color, Vector2 rectSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = rectSize;
        return tmp;
    }

    private static Material BuildOceanMaterial()
    {
        Shader oceanShader = Shader.Find(OceanShaderName);
        if (oceanShader == null) oceanShader = Shader.Find("Standard");
        string matPath = MaterialsFolder + "/AC_OceanAnimated.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(oceanShader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = oceanShader;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Dictionary<string, Material> BuildCozyMaterials()
    {
        Shader cozyShader = Shader.Find(CozyShaderName);
        if (cozyShader == null)
        {
            Debug.LogWarning("[Orla3D] Cozy shader not found, falling back to Standard.");
            cozyShader = Shader.Find("Standard");
        }

        // Exact palette matching build_orla_acnh.py!
        var palette = new (string name, string hex, float gloss, string emitHex, float emitStr)[]
        {
            ("AC_Sand", "#E8CB98", 0.10f, null, 0f),
            ("AC_SandWet", "#CFA974", 0.28f, null, 0f),
            ("AC_Grass", "#5EB55B", 0.12f, null, 0f),
            ("AC_GrassLight", "#7ECA6B", 0.14f, null, 0f),
            ("AC_Ocean", "#289CB0", 0.55f, null, 0f),
            ("AC_OceanShallow", "#54CFCB", 0.50f, null, 0f),
            ("AC_Foam", "#EAF6FA", 0.22f, "#D8F2F8", 0.14f),
            ("AC_HillDistant", "#5A937C", 0.08f, null, 0f),
            ("AC_HillFar", "#6A8AA0", 0.08f, null, 0f),

            ("AC_StoneWhite", "#E2D9CA", 0.14f, null, 0f),
            ("AC_StoneWarm", "#D2C2AA", 0.14f, null, 0f),
            ("AC_StoneDark", "#A4947E", 0.12f, null, 0f),
            ("AC_CobbleA", "#CCA989", 0.14f, null, 0f),
            ("AC_CobbleB", "#B78F72", 0.14f, null, 0f),
            ("AC_PlazaTile", "#E0CEB3", 0.14f, null, 0f),
            ("AC_PlazaAccent", "#D6B48F", 0.14f, null, 0f),
            ("AC_Asphalt", "#636E7B", 0.10f, null, 0f),
            ("AC_RoadLine", "#F6E2A2", 0.15f, null, 0f),

            ("AC_WoodWarm", "#B97A4B", 0.20f, null, 0f),
            ("AC_WoodLight", "#D89E6A", 0.22f, null, 0f),
            ("AC_WoodDark", "#7B4C2F", 0.18f, null, 0f),
            ("AC_Cream", "#FFF6E5", 0.28f, null, 0f),
            ("AC_PinkBerry", "#F26E97", 0.30f, null, 0f),
            ("AC_PinkSoft", "#FFA9C2", 0.30f, null, 0f),
            ("AC_MintTeal", "#4EC7B6", 0.32f, null, 0f),
            ("AC_MintSoft", "#8FE3D6", 0.30f, null, 0f),
            ("AC_PurplePlum", "#5B3B6E", 0.28f, null, 0f),
            ("AC_Lavender", "#B89CE0", 0.28f, null, 0f),
            ("AC_Coral", "#F27B66", 0.28f, null, 0f),
            ("AC_SunnyYellow", "#FFD45E", 0.30f, null, 0f),
            ("AC_SkyBlue", "#6CC0F0", 0.30f, null, 0f),
            ("AC_Glass", "#B8F0FA", 0.65f, "#9EE6F5", 0.25f),
            ("AC_MetalDark", "#3F4959", 0.45f, null, 0f),
            ("AC_MetalSilver", "#D4DCE6", 0.55f, null, 0f),
            ("AC_Gold", "#F5BE4F", 0.50f, null, 0f),

            ("AC_LanternGlow", "#FFE69C", 0.20f, "#FFD26B", 1.35f),
            ("AC_BulbWarm", "#FFF0B3", 0.20f, "#FFE082", 1.25f),

            ("AC_PalmTrunk", "#C48A5A", 0.15f, null, 0f),
            ("AC_PalmRing", "#9F6A40", 0.14f, null, 0f),
            ("AC_PalmLeaf", "#4EBF5C", 0.26f, null, 0f),
            ("AC_PalmLeafTip", "#7BE06C", 0.28f, null, 0f),
            ("AC_Coconut", "#6D4127", 0.30f, null, 0f),
            ("AC_CoconutGreen", "#5DBB53", 0.36f, null, 0f),
            ("AC_BushGreen", "#52B55E", 0.18f, null, 0f),
            ("AC_BushLight", "#7CD46E", 0.20f, null, 0f),
            ("AC_Terracotta", "#D47551", 0.18f, null, 0f),
            ("AC_Orange", "#FF9333", 0.36f, null, 0f),
            ("AC_Strawberry", "#FF6E8D", 0.35f, null, 0f),
            ("AC_Chocolate", "#6E422C", 0.32f, null, 0f),
            ("AC_Vanilla", "#FFF3D1", 0.30f, null, 0f),
            ("AC_Waffle", "#E3A75B", 0.22f, null, 0f),
            ("AC_FlowerPink", "#FF7AA8", 0.28f, null, 0f),
            ("AC_FlowerYellow", "#FFE055", 0.28f, null, 0f),
            ("AC_FlowerBlue", "#75BFFF", 0.28f, null, 0f),
            ("AC_FlowerWhite", "#FFFDF7", 0.28f, null, 0f),
            ("AC_Starfish", "#FF8566", 0.25f, null, 0f),
            ("AC_Shell", "#FFE8D6", 0.35f, null, 0f),

            ("AC_SkinWarm", "#C9855E", 0.25f, null, 0f),
            ("AC_SkinLight", "#F4C29F", 0.25f, null, 0f),
            ("AC_SkinTan", "#DF9F73", 0.25f, null, 0f),
            ("AC_CheekBlush", "#FF8EA6", 0.25f, null, 0f),
            ("AC_EyeDark", "#251D29", 0.65f, null, 0f),
            ("AC_EyeShine", "#FFFFFF", 0.40f, "#FFFFFF", 0.65f),
            ("AC_HairPlum", "#723678", 0.32f, null, 0f),
            ("AC_HairBrown", "#6B442A", 0.28f, null, 0f),
            ("AC_HairDark", "#332A38", 0.30f, null, 0f),
            ("AC_HairAuburn", "#B85337", 0.30f, null, 0f),
            ("AC_Denim", "#4E78A8", 0.16f, null, 0f),
            ("AC_StrawHat", "#F2CF7E", 0.16f, null, 0f),
            ("AC_ShoeDark", "#4E3A4C", 0.25f, null, 0f),
            ("AC_HouseWall", "#FFF2DB", 0.16f, null, 0f),
            ("AC_HouseRoof", "#F27B66", 0.22f, null, 0f),
            ("AC_ProtagShirt", "#F26E97", 0.28f, null, 0f),
        };

        var dict = new Dictionary<string, Material>();
        foreach (var item in palette)
        {
            string matPath = MaterialsFolder + "/" + item.name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(cozyShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = cozyShader;
            }

            if (ColorUtility.TryParseHtmlString(item.hex, out Color baseCol))
                mat.SetColor("_Color", baseCol);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", item.gloss);
            if (mat.HasProperty("_ShadowColor"))
                mat.SetColor("_ShadowColor", new Color(0.38f, 0.34f, 0.52f, 1f));
            if (mat.HasProperty("_RimColor"))
                mat.SetColor("_RimColor", new Color(1.0f, 0.90f, 0.72f, 1f));
            if (mat.HasProperty("_RimPower"))
                mat.SetFloat("_RimPower", 4.2f);
            if (mat.HasProperty("_RimStrength"))
                mat.SetFloat("_RimStrength", 0.14f);
            if (mat.HasProperty("_WrapAmount"))
                mat.SetFloat("_WrapAmount", 0.28f);
            if (mat.HasProperty("_GroundAO"))
                mat.SetFloat("_GroundAO", 0.34f);
            if (mat.HasProperty("_CurveStrength"))
                mat.SetFloat("_CurveStrength", 0.0016f);

            if (!string.IsNullOrEmpty(item.emitHex) && ColorUtility.TryParseHtmlString(item.emitHex, out Color emCol))
            {
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emCol * item.emitStr);
            }
            else if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            dict[item.name] = mat;
        }

        AssetDatabase.SaveAssets();
        return dict;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
