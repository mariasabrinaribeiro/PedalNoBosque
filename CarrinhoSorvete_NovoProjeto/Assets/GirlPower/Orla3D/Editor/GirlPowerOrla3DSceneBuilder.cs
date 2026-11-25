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
    private const string FadeShaderName = "GirlPower/OrlaACNHCozyFade";

    private const string BuildVersionKey = "GirlPower_Orla3D_SceneVersion_v17_ArchetypesPetsAndWaterBowls";

    static GirlPowerOrla3DSceneBuilder()
    {
        EditorApplication.delayCall += TryAutoBuildOnce;
    }

    private static void TryAutoBuildOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
        string fullScenePath = Path.Combine(Application.dataPath, "Scenes/Girl Power - Orla 3D (Animal Crossing).unity");
        bool alreadyBuiltCurrent = EditorPrefs.GetBool(BuildVersionKey, false);
        if (File.Exists(fullScenePath) && alreadyBuiltCurrent) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + "/CoastalHouse.fbx") == null) return;
        Build();
        if (File.Exists(fullScenePath)) EditorPrefs.SetBool(BuildVersionKey, true);
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
        RenderSettings.fogColor = new Color(0.68f, 0.79f, 0.87f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 85f;

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

        // 4. Quiosque Sorvetes da Baía (Cohesive pastel coastal branding — zero clashing "GIRL POWER"!)
        List<BoxCollider> obstacles = new List<BoxCollider>();
        GameObject kiosk = PlaceModel(root.transform, "KioskGirlPower", "Quiosque Sorvetes da Baia", new Vector3(0f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (kiosk != null) AddBlockingBox(kiosk, new Vector3(0f, 2.0f, 0f), new Vector3(4.6f, 4.0f, 2.2f), obstacles);
        Make3DText(root.transform, "Titulo Quiosque", new Vector3(0f, 3.55f, 1.65f), "SORVETES DA BAÍA", 1.72f, new Color32(255, 246, 224, 255), new Vector2(2.65f, 0.48f));

        GameObject customSign = PlaceModel(root.transform, "SignboardNew", "Placa Personalizada - Sorveteria", new Vector3(0f, 3.56f, 1.54f), 0f, 0.96f, cozyMats);
        TextMeshPro customSignLabel = null;
        if (customSign != null)
        {
            customSignLabel = Make3DText(customSign.transform, "Nome Instalado", new Vector3(0f, 0f, -0.17f), "BAÍA DO AVESSO", 1.72f, new Color32(255, 246, 224, 255), new Vector2(2.65f, 0.48f));
            customSign.SetActive(false);
        }

        // Choice Upgrade 1: Volumetric Striped Fabric Awning (Blender FBX, hidden until chosen!)
        GameObject kioskAwningUpgrade = PlaceModel(root.transform, "KioskAwningUpgrade", "Melhoria - Toldo Listrado", new Vector3(0f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (kioskAwningUpgrade != null) kioskAwningUpgrade.SetActive(false);

        // 5. Banca da Florista + Swaying Breeze Curtains + Standardized Fascia Sign
        GameObject floristStand = PlaceModel(root.transform, "FloristStand", "Banca da Florista - Baia do Avesso", new Vector3(24f, 0f, 2.35f), 0f, 1f, cozyMats);
        if (floristStand != null) AddBlockingBox(floristStand, new Vector3(0f, 2.0f, 0f), new Vector3(4.3f, 4.0f, 2.1f), obstacles);
        Make3DText(root.transform, "Titulo Florista", new Vector3(24f, 3.16f, 1.62f), "FLORES DA BAÍA", 1.65f, new Color32(255, 246, 224, 255), new Vector2(2.50f, 0.45f));

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
            if (st != null) AddBlockingBox(st, new Vector3(0f, 2.0f, 0f), new Vector3(2.7f, 4.0f, 1.15f), obstacles);

        // Uniform, clean fascia signage across all stalls (consistent height, typography & alignment!)
        Make3DText(root.transform, "Placa Sushi", new Vector3(-7.4f, 2.48f, 1.72f), "SUSHI", 1.48f, new Color32(255, 246, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Comida", new Vector3(-11.8f, 2.48f, 1.72f), "COMIDAS", 1.48f, new Color32(255, 246, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Doces", new Vector3(-16.0f, 2.48f, 1.72f), "DOCES", 1.48f, new Color32(255, 246, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Plantas", new Vector3(20.1f, 2.48f, 2.02f), "PLANTAS", 1.48f, new Color32(255, 246, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Roupas", new Vector3(29.8f, 2.48f, 1.72f), "ROUPAS", 1.48f, new Color32(255, 246, 224, 255), new Vector2(2.2f, 0.40f));
        Make3DText(root.transform, "Placa Sucos", new Vector3(34.0f, 2.48f, 1.72f), "SUCOS & REFRI", 1.42f, new Color32(255, 246, 224, 255), new Vector2(2.5f, 0.40f));

        // 6a. Mega Life Blender Landmarks: Restaurant "Batata & Mar" facing the promenade cleanly (no buried sign!)
        GameObject restKitchen = PlaceModel(root.transform, "RestaurantFriesKitchen", "Restaurante Batata e Mar", new Vector3(-14.2f, 0f, -6.2f), 180f, 1f, cozyMats);
        if (restKitchen != null)
        {
            // Segment the collision perimeter and leave a real doorway on the promenade-facing side (local -Z).
            AddBlockingBox(restKitchen, new Vector3(0f, 1.8f, 0.92f), new Vector3(4.0f, 3.6f, 0.28f), obstacles);
            AddBlockingBox(restKitchen, new Vector3(-1.86f, 1.8f, 0f), new Vector3(0.28f, 3.6f, 1.85f), obstacles);
            AddBlockingBox(restKitchen, new Vector3( 1.86f, 1.8f, 0f), new Vector3(0.28f, 3.6f, 1.85f), obstacles);
            AddBlockingBox(restKitchen, new Vector3(-1.34f, 1.8f, -0.92f), new Vector3(1.32f, 3.6f, 0.28f), obstacles);
            AddBlockingBox(restKitchen, new Vector3( 1.34f, 1.8f, -0.92f), new Vector3(1.32f, 3.6f, 0.28f), obstacles);
            ApplyFadeMaterials(restKitchen, false);
        }
        // Pavilion: 4 solid corner pillars so stairs & interior festival table remain 100% walkable!
        if (pavilion != null)
        {
            AddBlockingBox(pavilion, new Vector3(-2.55f, 2.0f, -1.65f), new Vector3(0.45f, 4.0f, 0.45f), obstacles);
            AddBlockingBox(pavilion, new Vector3( 2.55f, 2.0f, -1.65f), new Vector3(0.45f, 4.0f, 0.45f), obstacles);
            AddBlockingBox(pavilion, new Vector3(-2.55f, 2.0f,  1.65f), new Vector3(0.45f, 4.0f, 0.45f), obstacles);
            AddBlockingBox(pavilion, new Vector3( 2.55f, 2.0f,  1.65f), new Vector3(0.45f, 4.0f, 0.45f), obstacles);
        }
        Make3DText(root.transform, "Placa Restaurante", new Vector3(-14.2f, 3.22f, -5.08f), "BATATA & MAR", 1.62f, new Color32(255, 246, 224, 255), new Vector2(2.65f, 0.45f));

        // 6a2. Enterable "Café das Fadas" (Seamless Roof-Fade Interior — NO loading screen!)
        Vector3 cafePos = new Vector3(5.6f, -0.08f, -8.2f);
        GameObject cafeBase = PlaceModel(root.transform, "CoastalCafe_Base", "Cafe das Fadas - Interior", cafePos, 0f, 1f, cozyMats);
        GameObject cafeRoof = PlaceModel(root.transform, "CoastalCafe_RoofFade", "Cafe das Fadas - Teto Transparente", cafePos, 0f, 1f, cozyMats);
        if (cafeBase != null)
        {
            // Solid outer walls with a 2.4m wide OPEN doorway facing north (+Z) toward the promenade!
            AddBlockingBox(cafeBase, new Vector3(0f, 1.8f, -2.05f), new Vector3(6.2f, 3.6f, 0.35f), obstacles);
            AddBlockingBox(cafeBase, new Vector3(-3.05f, 1.8f, 0f), new Vector3(0.35f, 3.6f, 4.2f), obstacles);
            AddBlockingBox(cafeBase, new Vector3( 3.05f, 1.8f, 0f), new Vector3(0.35f, 3.6f, 4.2f), obstacles);
            AddBlockingBox(cafeBase, new Vector3(-2.12f, 1.8f, 2.05f), new Vector3(1.85f, 3.6f, 0.35f), obstacles);
            AddBlockingBox(cafeBase, new Vector3( 2.12f, 1.8f, 2.05f), new Vector3(1.85f, 3.6f, 0.35f), obstacles);
            AddBlockingBox(cafeBase, new Vector3(0f, 0.7f, -1.25f), new Vector3(3.6f, 1.4f, 0.85f), obstacles); // Pastry counter
        }
        if (cafeRoof != null) ApplyFadeMaterials(cafeRoof, false);
        Make3DText(root.transform, "Placa Cafe das Fadas", cafePos + new Vector3(0f, 3.02f, 2.86f), "CAFÉ DAS FADAS", 1.55f, new Color32(255, 246, 224, 255), new Vector2(2.8f, 0.44f));

        // 6a3. Colorful Low Coastal Town Facades along the far south horizon (eliminates empty backdrop edge!)
        float[] facadeXs = { -11.5f, 11.5f, 34.0f };
        for (int i = 0; i < facadeXs.Length; i++)
            PlaceModel(root.transform, "CoastalFacadeRow", "Fachada Vila Costeira " + (i + 1), new Vector3(facadeXs[i], -0.12f, -17.8f), 0f, 1.04f, cozyMats);

        // 6a4. Original Tropical-Magical Props (Glowing Beach Mushrooms, Floating Lanterns & Flower Lamp Posts!)
        Vector3[] mushroomSpots =
        {
            new Vector3(-8.2f, -0.10f, 5.2f), new Vector3(0.6f, -0.10f, 5.0f), new Vector3(9.4f, -0.10f, 5.4f),
            new Vector3(19.2f, -0.10f, 5.3f), new Vector3(27.6f, -0.10f, 5.1f), new Vector3(2.0f, -0.05f, -6.2f)
        };
        for (int i = 0; i < mushroomSpots.Length; i++)
            PlaceModel(root.transform, "GlowingMushroomCluster", "Cogumelo Luminoso " + (i + 1), mushroomSpots[i], i * 52f, 0.88f + (i % 3) * 0.14f, cozyMats);

        List<Transform> floatingLanterns = new List<Transform>();
        Vector3[] lanternSpots =
        {
            new Vector3(-3.8f, 2.25f, -3.2f), new Vector3(2.8f, 2.35f, -5.4f), new Vector3(8.4f, 2.35f, -5.4f),
            new Vector3(14.5f, 2.30f, -3.2f), new Vector3(23.2f, 2.25f, -3.0f), new Vector3(15.6f, 2.65f, 3.6f)
        };
        for (int i = 0; i < lanternSpots.Length; i++)
        {
            GameObject fl = PlaceModel(root.transform, "FloatingLantern", "Lanterna Flutuante " + (i + 1), lanternSpots[i], i * 40f, 1.0f, cozyMats);
            if (fl != null) floatingLanterns.Add(fl.transform);
        }

        Vector3[] flowerLampSpots = { new Vector3(1.8f, 0f, -3.35f), new Vector3(9.4f, 0f, -3.35f), new Vector3(22.6f, 0f, -3.35f) };
        for (int i = 0; i < flowerLampSpots.Length; i++)
        {
            GameObject flp = PlaceModel(root.transform, "FlowerLampPost", "Poste com Flores " + (i + 1), flowerLampSpots[i], i == 1 ? 180f : 0f, 1.0f, cozyMats);
            if (flp != null) AddBlockingBox(flp, new Vector3(0f, 1.8f, 0f), new Vector3(0.45f, 3.6f, 0.45f), obstacles);
        }

        List<Transform> restaurantTables = new List<Transform>();
        List<GameObject> restaurantCustomers = new List<GameObject>();
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

                // Each restaurant order is represented by a visible guest beside its table.
                string guestModel = (i % 2 == 0) ? "Char_PedestrianBlue" : "Char_PedestrianCoral";
                Vector3 guestPos = tableCoords[i] + new Vector3(1.05f, 0f, 0.28f);
                GameObject guest = PlaceModel(root.transform, guestModel, "Cliente_Mesa_" + (i + 1), guestPos, -90f, 0.76f, cozyMats);
                if (guest != null)
                {
                    guest.SetActive(i < 2);
                }
                restaurantCustomers.Add(guest);
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

        // 6b. 3 Low-Profile Paintable Coastal Houses placed cleanly clear of the foreground camera corridor!
        List<Transform> paintableHouses = new List<Transform>();
        GameObject mainHouse = null;
        Vector3[] housePositions =
        {
            new Vector3(-14.8f, -0.08f, -12.8f),
            new Vector3(9.5f, -0.08f, -14.2f),
            new Vector3(34.2f, -0.08f, -12.8f)
        };
        for (int i = 0; i < housePositions.Length; i++)
        {
            GameObject house = PlaceModel(root.transform, "CoastalHouse", "Casa Costeira Pintavel " + (i + 1), housePositions[i], 0f, 0.82f, cozyMats);
            if (house != null)
            {
                AddBlockingBox(house, new Vector3(0f, 2.0f, 0f), new Vector3(3.8f, 4.0f, 3.1f), obstacles);
                paintableHouses.Add(house.transform);
                if (i == 1) mainHouse = house;
            }
        }
        if (mainHouse != null) ApplyFadeMaterials(mainHouse, true);

        List<Transform> swayingFestoons = new List<Transform>();
        float[] festoonXs = { 11.1f, 15.6f, 20.1f };
        for (int i = 0; i < festoonXs.Length; i++)
        {
            GameObject fest = PlaceModel(root.transform, "FestoonLights", "Varal de Bandeirinhas " + (i + 1), new Vector3(festoonXs[i], 2.75f, 2.05f), 0f, 1f, cozyMats);
            if (fest != null) swayingFestoons.Add(fest.transform);
        }

        // 7. Arched Coconut Palms framed cleanly away from the Pavilion stairs & Fishing Pier entrance!
        List<Transform> swayingPalms = new List<Transform>();
        Vector4[] palmSpecs =
        {
            new Vector4(-9.6f, 3.35f, -14f, 1.02f),
            new Vector4(4.4f, 3.35f, 18f, 0.96f),
            new Vector4(9.2f, 3.35f, -162f, 0.94f),
            new Vector4(22.4f, 3.35f, 16f, 1.02f),
            new Vector4(35.4f, 3.35f, -152f, 1.04f)
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

        // 8. Varied Coastal Vegetation Planters & Grass Clusters (Blender 5.3 — replacing repetitive identical bushes!)
        List<Light> warmLights = new List<Light>();
        List<Transform> idleCharacters = new List<Transform>();

        var vegSpecs = new (string model, Vector3 pos, float rotY, float scale, bool blocks)[]
        {
            ("CoastalVegetationB", new Vector3(-4.4f, 0f, -0.22f), 4f, 1.0f, true),
            ("CoastalVegetationA", new Vector3(7.4f, 0f, -0.35f), 22f, 1.05f, false),
            ("CoastalVegetationB", new Vector3(11.4f, 0f, -0.22f), -6f, 0.96f, true),
            ("CoastalVegetationA", new Vector3(15.6f, 0f, -0.40f), 145f, 0.92f, false),
            ("CoastalVegetationB", new Vector3(19.8f, 0f, -0.22f), 8f, 1.02f, true),
            ("CoastalVegetationA", new Vector3(26.8f, 0f, -0.32f), -35f, 1.08f, false),
            ("CoastalVegetationA", new Vector3(-8.4f, 0f, -0.35f), 68f, 1.0f, false)
        };
        for (int i = 0; i < vegSpecs.Length; i++)
        {
            var vs = vegSpecs[i];
            GameObject veg = PlaceModel(root.transform, vs.model, "Jardineira Costeira " + (i + 1), vs.pos, vs.rotY, vs.scale, cozyMats);
            if (veg != null) AddBlockingBox(veg, new Vector3(0f, 0.72f, 0f), new Vector3(1.48f, 1.44f, 0.72f), obstacles);
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

        // 11. Hero Ice Cream Cart (+ Front Plaque + Painted Sign Upgrade + Basil Pot), Two Friends (Nina & Bia), Protagonist & Florist
        GameObject cart = PlaceModel(root.transform, "IceCreamCart", "Carrinho de Sorvete", new Vector3(1.15f, 0f, -1.75f), 6f, 1.06f, cozyMats);

        // Clean front plaque typography on the Hero Ice Cream Cart + Choice Upgrade 2
        GameObject cartPaintedSignUpgrade = null;
        if (cart != null)
        {
            Make3DText(cart.transform, "Placa Frontal Sorvetes", new Vector3(0.10f, 0.81f, -0.54f), "SORVETES", 1.15f, new Color32(218, 72, 118, 255), new Vector2(1.05f, 0.34f));
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
        GameObject playerVisual = PlaceModel(playerRoot.transform, "Char_Protagonist", "Visual Protagonista", Vector3.zero, -20f, 1.12f, cozyMats);
        GameObject heldCone = PlaceModel(playerRoot.transform, "IceCreamConeLemonBasil", "Casquinha Limao Manjericao na Mao", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSurfboard = PlaceModel(playerRoot.transform, "Surfboard", "Prancha de Surf da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkate = PlaceModel(playerRoot.transform, "Skateboard", "Skate da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerScooter = PlaceModel(playerRoot.transform, "Scooter", "Patinete da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkates = PlaceModel(playerRoot.transform, "RollerSkates", "Patins da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerCanoe = PlaceModel(playerRoot.transform, "HawaiianCanoe", "Canoa Havaiana da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerHat = PlaceModel(playerRoot.transform, "WardrobeHatProp", "Chapeu e Oculos da Protagonista", Vector3.zero, 0f, 1f, cozyMats);

        // Decorative beach surfboard standing near Lifeguard Tower 1
        PlaceModel(root.transform, "Surfboard", "Prancha na Areia", new Vector3(8.0f, 0.25f, 6.8f), 32f, 1.05f, cozyMats);

        // Cute Pet Companions: Dog "Pipoca" (+ Bandana + Detective Cap!) & Cat "Mingau"!
        GameObject dogObj = PlaceModel(root.transform, "PetDog", "Cachorro Pipoca", new Vector3(-3.0f, 0f, -1.55f), -35f, 1.05f, cozyMats);
        GameObject pipocaBandana = dogObj != null ? PlaceModel(dogObj.transform, "PetBandanaProp", "Bandana do Pipoca", Vector3.zero, 0f, 1f, cozyMats) : null;
        GameObject pipocaDetHat = dogObj != null ? PlaceModel(dogObj.transform, "DetectiveHatPipoca", "Chapeu Detetive Pipoca", Vector3.zero, 0f, 1f, cozyMats) : null;
        GirlPowerOrla3DPetCompanion petDog = null;
        if (dogObj != null)
        {
            petDog = dogObj.AddComponent<GirlPowerOrla3DPetCompanion>();
            petDog.petName = "Pipoca";
            petDog.isCat = false;
            petDog.followTarget = playerRoot.transform;
            petDog.guardPoint = cart != null ? cart.transform : null;
            petDog.bandanaProp = pipocaBandana != null ? pipocaBandana.transform : null;
            petDog.detectiveHatProp = pipocaDetHat != null ? pipocaDetHat.transform : null;
            petDog.followOffset = new Vector3(-1.15f, 0f, -0.75f);
        }

        // Street Thief & Coastal Police Patrol (Blender 5.3 models!)
        GameObject thiefObj = PlaceModel(root.transform, "Char_Thief", "Ladrao de Rua - Baia", new Vector3(-18f, 0f, -2.1f), 0f, 1f, cozyMats);
        if (thiefObj != null) thiefObj.SetActive(false);
        GameObject policeOfficerObj = PlaceModel(root.transform, "Char_PoliceOfficer", "Policial Costeiro", new Vector3(-20f, 0f, -2.4f), 0f, 1f, cozyMats);
        if (policeOfficerObj != null) policeOfficerObj.SetActive(false);
        GameObject policeCarObj = PlaceModel(root.transform, "PolicePatrolCar", "Viatura Policia Costeira", new Vector3(-22f, 0f, -4.4f), -90f, 0.92f, cozyMats);
        if (policeCarObj != null) policeCarObj.SetActive(false);

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

        // Only the central coastal house fades; stalls, palms, umbrellas and other homes stay opaque.
        List<Transform> occluderRoots = new List<Transform>();
        if (mainHouse != null) occluderRoots.Add(mainHouse.transform);

        GameObject occObj = new GameObject("Oclusao_Transparencia_Construcoes");
        occObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DOcclusionFader occFader = occObj.AddComponent<GirlPowerOrla3DOcclusionFader>();
        occFader.player = playerRoot.transform;
        occFader.targetCamera = mainCam;
        occFader.occluderRoots = occluderRoots.ToArray();
        occFader.cutoutRadius = 1.15f;

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
        if (thiefObj != null) AddShadow(shadowEntries, thiefObj.transform, new Vector2(1.05f, 1.05f), true);
        if (policeOfficerObj != null) AddShadow(shadowEntries, policeOfficerObj.transform, new Vector2(1.05f, 1.05f), true);

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
        Vector3[] beachWalkerPositions =
        {
            new Vector3(-12.2f, -0.12f, 4.15f), new Vector3(-4.4f, -0.12f, 4.35f),
            new Vector3(4.4f, -0.12f, 4.25f), new Vector3(27.8f, -0.12f, 4.15f)
        };
        for (int i = 0; i < beachWalkerPositions.Length; i++)
        {
            string model = (i % 2 == 0) ? "Char_PedestrianCoral" : "Char_PedestrianBlue";
            GameObject walker = PlaceModel(root.transform, model, "Turista Praia " + (i + 1), beachWalkerPositions[i], (i % 2 == 0) ? 15f : -15f, 0.92f + i * 0.05f, cozyMats);
            if (walker != null)
            {
                strollingNpcs.Add(walker.transform);
                AddShadow(shadowEntries, walker.transform, new Vector2(1.0f, 1.0f), true);
            }
        }

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

        // Floating Fairy Waitresses ("Garçonetes Fadas" gliding between Café das Fadas, Quiosque & Beach Tables!)
        List<Transform> fairyList = new List<Transform>();
        Vector3[] fairySpawns =
        {
            new Vector3(5.6f, 0.65f, -7.8f),
            new Vector3(-11.2f, 0.72f, -1.6f),
            new Vector3(21.6f, 0.68f, 5.9f)
        };
        for (int i = 0; i < fairySpawns.Length; i++)
        {
            GameObject fy = PlaceModel(root.transform, "Char_FairyWaitress", "Fada Garconete " + (i + 1), fairySpawns[i], i * 45f, 1.0f, cozyMats);
            if (fy != null)
            {
                fairyList.Add(fy.transform);
                AddShadow(shadowEntries, fy.transform, new Vector2(0.75f, 0.75f), true);
            }
        }

        // Distant gliding horizon sailboats (background coastal motion!)
        List<Transform> distantBoats = new List<Transform>();
        for (int i = 0; i < 2; i++)
        {
            GameObject db = PlaceModel(root.transform, "HawaiianCanoe", "Barquinho Horizonte " + (i + 1), new Vector3(-8f + i * 24f, -0.22f, 25.5f + i * 4.0f), i == 0 ? 90f : -90f, 1.35f, cozyMats);
            if (db != null) distantBoats.Add(db.transform);
        }

        // Seamless Café das Fadas Roof Fade Controller (zero loading screen when walking indoors!)
        if (cafeRoof != null)
        {
            GameObject cafeFadeObj = new GameObject("Cafe_Teto_Fade_Automatico");
            cafeFadeObj.transform.SetParent(root.transform, false);
            GirlPowerOrla3DCafeRoofFade cafeFade = cafeFadeObj.AddComponent<GirlPowerOrla3DCafeRoofFade>();
            cafeFade.player = playerRoot.transform;
            cafeFade.roofObject = cafeRoof.transform;
            cafeFade.buildingCenter = cafePos;
            cafeFade.halfBoundsXZ = new Vector2(3.6f, 2.9f);
        }

        // Batata & Mar interior: fade the shell while the player is inside so its room remains visible.
        if (restKitchen != null)
        {
            GameObject restaurantFadeObj = new GameObject("Batata_Mar_Teto_Fade_Automatico");
            restaurantFadeObj.transform.SetParent(root.transform, false);
            GirlPowerOrla3DCafeRoofFade restaurantFade = restaurantFadeObj.AddComponent<GirlPowerOrla3DCafeRoofFade>();
            restaurantFade.player = playerRoot.transform;
            restaurantFade.roofObject = restKitchen.transform;
            restaurantFade.buildingCenter = new Vector3(-14.2f, 0f, -6.2f);
            restaurantFade.halfBoundsXZ = new Vector2(2.25f, 1.45f);
        }

        GameObject npcAiObj = new GameObject("IA_Moradores_e_Fauna_Local");
        npcAiObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DNpcAI npcAi = npcAiObj.AddComponent<GirlPowerOrla3DNpcAI>();
        npcAi.player = playerRoot.transform;
        npcAi.strollingNpcs = strollingNpcs.ToArray();
        npcAi.fairyWaitresses = fairyList.ToArray();
        npcAi.floatingLanterns = floatingLanterns.ToArray();
        npcAi.distantSailboats = distantBoats.ToArray();
        npcAi.beachSeagulls = beachSeagulls.ToArray();
        npcAi.shallowFish = shallowFish.ToArray();

        // 11h. 4 LOGICAL HARMONIZED ZONES: Orchard Camponesa, 10 Modular Archetypes, Pets (Dog/Cat/Calopsita) & 5 Water Bowls!
        // Zone 1: Fenced Orchard & Garden (Fundo/Lateral Verde) -> Camponesa harvesting Laranja, Limão, Manjericão & Maçã!
        List<Transform> orchardTrees = new List<Transform>();
        Vector3[] orchardCoords = { new Vector3(-5.8f, -0.05f, -8.4f), new Vector3(-2.0f, -0.05f, -8.8f), new Vector3(-8.2f, -0.05f, -7.8f) };
        for (int i = 0; i < orchardCoords.Length; i++)
        {
            GameObject ot = PlaceModel(root.transform, "OrchardFruitTree", "Arvore Pomar Horta " + (i + 1), orchardCoords[i], i * 55f, 0.92f + i * 0.08f, cozyMats);
            if (ot != null)
            {
                AddBlockingBox(ot, new Vector3(0f, 1.4f, 0f), new Vector3(0.95f, 2.8f, 0.95f), obstacles);
                orchardTrees.Add(ot.transform);
            }
        }
        GameObject camponesaObj = PlaceModel(root.transform, "Char_Camponesa", "Camponesa Colhedora da Horta", new Vector3(-4.6f, 0f, -7.2f), 20f, 1.0f, cozyMats);
        if (camponesaObj != null) AddShadow(shadowEntries, camponesaObj.transform, new Vector2(1.0f, 1.0f), true);

        // Zone 2 & 3 & 4: Modular Harmonized Archetypes + Pet Companions (Dog, Cat, Flying Calopsita!)
        var guestSpecs = new (GirlPowerOrla3DArchetypeConfig.ArchetypeKind kind, string fbx, string name, Vector3 pos, GirlPowerOrla3DArchetypeConfig.PetKind pet)[]
        {
            // Zone 2 (Central Promenade & Pet Restaurant guests)
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Gaucho,           "Char_Gaucho",           "Cliente - Gaucho do Sul",        new Vector3(-8.2f, 0f, -2.2f),   GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Espia,            "Char_Espia",            "Cliente - Espia Estilosa",       new Vector3(1.4f, 0f, -2.5f),    GirlPowerOrla3DArchetypeConfig.PetKind.Gato),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Barbie,           "Char_Barbie",           "Cliente - Estilo Barbie",        new Vector3(7.6f, 0f, -2.1f),    GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Princesa,         "Char_Princesa",         "Cliente - Princesa da Orla",     new Vector3(13.8f, 0f, -2.3f),   GirlPowerOrla3DArchetypeConfig.PetKind.Gato),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Vovo,             "Char_Vovo",             "Cliente - Vovo Carinhosa",       new Vector3(18.6f, 0f, -2.1f),   GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.KungFu,           "Char_KungFu",           "Cliente - Mestre Kung Fu",       new Vector3(23.0f, 0f, -2.4f),   GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Japones,          "Char_Japones",          "Cliente - Estilo Japones",       new Vector3(-3.8f, 0f, -2.4f),   GirlPowerOrla3DArchetypeConfig.PetKind.Gato),
            // Zone 4 (Beach Sand & Sea strictly for Beach/Sports outfits!)
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.PraiaFutebol,     "Char_FutebolPraia",     "Praiano - Futebol de Areia",     new Vector3(6.8f, -0.14f, 6.0f), GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.FutebolAmericano, "Char_FutebolAmericano", "Praiano - Futebol Americano",    new Vector3(19.2f, -0.14f, 6.2f),GirlPowerOrla3DArchetypeConfig.PetKind.None)
        };

        List<GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry> guestEntries = new List<GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry>();
        for (int i = 0; i < guestSpecs.Length; i++)
        {
            var gs = guestSpecs[i];
            GameObject actor = PlaceModel(root.transform, gs.fbx, gs.name, gs.pos, (i % 2 == 0) ? 18f : -18f, 1.0f, cozyMats);
            if (actor == null) continue;
            AddShadow(shadowEntries, actor.transform, new Vector2(0.95f, 0.95f), true);

            Transform petT = null;
            if (gs.pet != GirlPowerOrla3DArchetypeConfig.PetKind.None)
            {
                string petFbx = gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita ? "Pet_Calopsita"
                    : (gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Gato ? "PetCat" : "PetDog");
                GameObject petGo = PlaceModel(root.transform, petFbx, "Pet_" + gs.kind + "_" + gs.pet, gs.pos + new Vector3(-0.75f, 0f, -0.55f), 0f, gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita ? 1.0f : 0.78f, cozyMats);
                if (petGo != null)
                {
                    petT = petGo.transform;
                    if (gs.pet != GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
                        AddShadow(shadowEntries, petT, new Vector2(0.62f, 0.62f), true);
                }
            }

            guestEntries.Add(new GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry
            {
                actor = actor.transform,
                petCompanion = petT,
                kind = gs.kind,
                petKind = gs.pet
            });
        }

        // 5 Interactive Pet Water Bowls along the Central Promenade & Restaurant/Cafe entrances!
        var bowlSpecs = new (string label, Vector3 pos)[]
        {
            ("Pote Agua - Quiosque",      new Vector3(-2.6f, 0f, -0.85f)),
            ("Pote Agua - Cafe Fadas",    new Vector3(3.4f, -0.05f, -5.8f)),
            ("Pote Agua - Batata e Mar",  new Vector3(-11.4f, -0.05f, -4.4f)),
            ("Pote Agua - Feira Central", new Vector3(13.0f, 0f, -0.85f)),
            ("Pote Agua - Florista",      new Vector3(22.2f, 0f, -0.85f))
        };
        List<GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry> bowlEntries = new List<GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry>();
        for (int i = 0; i < bowlSpecs.Length; i++)
        {
            GameObject bowl = PlaceModel(root.transform, "PetWaterBowl", bowlSpecs[i].label, bowlSpecs[i].pos, i * 25f, 1.05f, cozyMats);
            if (bowl != null)
            {
                AddShadow(shadowEntries, bowl.transform, new Vector2(0.68f, 0.68f), false);
                bowlEntries.Add(new GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry
                {
                    label = bowlSpecs[i].label,
                    bowlRoot = bowl.transform
                });
            }
        }

        GameObject petSysObj = new GameObject("Sistema_PetFriendly_PotesAgua_e_Arquetipos");
        petSysObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DPetFriendlySystem petFriendlySys = petSysObj.AddComponent<GirlPowerOrla3DPetFriendlySystem>();
        petFriendlySys.player = playerRoot.transform;
        petFriendlySys.sceneCamera = mainCam;
        petFriendlySys.camponesaHarvester = camponesaObj != null ? camponesaObj.transform : null;
        petFriendlySys.orchardTreePoints = orchardTrees.ToArray();
        petFriendlySys.kitchenDropoffPoint = restKitchen != null ? restKitchen.transform : null;
        petFriendlySys.fairyHelpers = fairyList.ToArray();
        petFriendlySys.archetypeGuests = guestEntries.ToArray();
        petFriendlySys.petFriendlyTables = restaurantTables.ToArray();
        petFriendlySys.waterBowls = bowlEntries.ToArray();
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
        contactShadows.serializedBlobMat = BuildBlobShadowMaterial();
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
        ambient.playerTransform = playerRoot.transform;
        ambient.cameraPost = mainCam != null ? mainCam.GetComponent<GirlPowerOrla3DCameraPost>() : null;
        ambient.currentPreset = GirlPowerOrla3DAmbient.TimePreset.FimDeTarde;

        GameObject missionObj = new GameObject("Historia Baia do Avesso - 4 Capitulos");
        missionObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DFirstDelivery mission = missionObj.AddComponent<GirlPowerOrla3DFirstDelivery>();
        // Bring the player closer to the camera while preserving enough route visibility.
        mission.cameraOffset = new Vector3(0f, 7.25f, -8.1f);
        mission.cameraPitch = 42f;
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
        megaSys.tableCustomers = restaurantCustomers.ToArray();
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

        // Detective Pipoca vs. Street Thieves & Coastal Police Dispatch!
        GameObject detObj = new GameObject("Sistema_Detetive_Pipoca_e_Policia");
        detObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DThiefDetective thiefDet = detObj.AddComponent<GirlPowerOrla3DThiefDetective>();
        thiefDet.player = playerRoot.transform;
        thiefDet.sceneCamera = mainCam;
        thiefDet.thiefActor = thiefObj != null ? thiefObj.transform : null;
        thiefDet.policeOfficer = policeOfficerObj != null ? policeOfficerObj.transform : null;
        thiefDet.policeCar = policeCarObj != null ? policeCarObj.transform : null;
        thiefDet.pipocaDog = petDog;
        List<Transform> raidList = new List<Transform>();
        foreach (GameObject rt in new[] { kiosk, floristStand, restKitchen, stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (rt != null) raidList.Add(rt.transform);
        thiefDet.raidTargets = raidList.ToArray();
        List<Transform> workerList = new List<Transform>();
        foreach (GameObject wk in new[] { friendNina, florist, friendBia, pedBlue, pedCoral })
            if (wk != null) workerList.Add(wk.transform);
        thiefDet.workerNpcs = workerList.ToArray();
        thiefDet.gameFlow = gameFlow;
        thiefDet.lifeActivities = lifeAct;
        thiefDet.megaSystems = megaSys;
        thiefDet.audioController = audioCtrl;
        thiefDet.characterAnimator = charAnim;

        gameFlow.megaSystems = megaSys;
        gameFlow.cardapioUI = cardapio;
        gameFlow.thiefDetective = thiefDet;
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
        camObj.transform.position = new Vector3(-0.2f, 9.2f, -15.95f);
        camObj.transform.rotation = Quaternion.Euler(32.5f, 0f, 0f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 155f;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.64f, 0.77f, 0.87f);
        cam.depthTextureMode |= DepthTextureMode.Depth;
        camObj.AddComponent<AudioListener>();
        GirlPowerOrla3DCameraPost post = camObj.AddComponent<GirlPowerOrla3DCameraPost>();
        post.exposure = 0.90f;
        post.contrast = 1.07f;
        post.saturation = 1.10f;
        post.vignetteStrength = 0.10f;
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
        ocean.transform.position = new Vector3(12f, -0.29f, 26.2f);

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

    private static Material BuildBlobShadowMaterial()
    {
        Shader sh = Shader.Find("GirlPower/OrlaBlobShadow");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        string matPath = MaterialsFolder + "/AC_BlobShadow.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(sh);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = sh;
        }
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.14f, 0.10f, 0.24f, 0.42f));
        if (mat.HasProperty("_CurveStrength")) mat.SetFloat("_CurveStrength", 0.0008f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material BuildOceanMaterial()
    {
        Shader oceanShader = Shader.Find(OceanShaderName);
        bool customWaterSupported = oceanShader != null && oceanShader.isSupported;
        if (!customWaterSupported)
        {
            oceanShader = Shader.Find("Universal Render Pipeline/Lit");
            if (oceanShader == null) oceanShader = Shader.Find("Standard");
            Debug.LogWarning("[Orla3D] Animated ocean shader is unavailable in the active render pipeline. Using a teal fallback material instead of a default white plane.");
        }
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
        if (!customWaterSupported)
        {
            Color fallbackWater = new Color(0.14f, 0.58f, 0.67f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallbackWater);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", fallbackWater);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
        }
        if (mat.HasProperty("_DepthDistance")) mat.SetFloat("_DepthDistance", 2.5f);
        if (mat.HasProperty("_FoamDistance")) mat.SetFloat("_FoamDistance", 0.65f);
        if (mat.HasProperty("_FoamSpeed")) mat.SetFloat("_FoamSpeed", 0.35f);
        if (mat.HasProperty("_CurveStrength")) mat.SetFloat("_CurveStrength", 0.0008f);
        if (mat.HasProperty("_WaveHeight")) mat.SetFloat("_WaveHeight", 0.055f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ApplyFadeMaterials(GameObject root, bool roofOnly = true)
    {
        Shader fadeShader = Shader.Find(FadeShaderName);
        if (fadeShader == null || !fadeShader.isSupported)
        {
            Debug.LogWarning("[Orla3D] Smooth house fade shader is unavailable; the house will remain opaque rather than use a noisy cutout.");
            return;
        }

        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null) continue;
                if (source.shader == null || source.shader.name != CozyShaderName) continue;
                if (roofOnly && !source.name.Contains("HouseRoof")) continue;

                string key = SafeAssetFileName(source.name);
                string path = MaterialsFolder + "/AC_Fade_" + key + ".mat";
                Material fadeMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (fadeMaterial == null)
                {
                    fadeMaterial = new Material(source);
                    fadeMaterial.name = "AC_Fade_" + key;
                    fadeMaterial.shader = fadeShader;
                    AssetDatabase.CreateAsset(fadeMaterial, path);
                }
                else
                {
                    fadeMaterial.CopyPropertiesFromMaterial(source);
                    fadeMaterial.shader = fadeShader;
                }

                if (fadeMaterial.HasProperty("_OccluderEnabled")) fadeMaterial.SetFloat("_OccluderEnabled", 1f);
                materials[i] = fadeMaterial;
                changed = true;
            }

            if (changed) renderer.sharedMaterials = materials;
        }
        AssetDatabase.SaveAssets();
    }

    private static string SafeAssetFileName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Unnamed";
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
                chars[i] = '_';
        return new string(chars);
    }

    private static Dictionary<string, Material> BuildCozyMaterials()
    {
        Shader cozyShader = Shader.Find(CozyShaderName);
        if (cozyShader == null)
        {
            Debug.LogWarning("[Orla3D] Cozy shader not found, falling back to Standard.");
            cozyShader = Shader.Find("Standard");
        }

        // Calm, cohesive coastal palette: soft cream stone, light wood, desaturated coastal greens,
        // toned-down secondary reds/oranges, reserving vibrant Hero Berry Pink for Cart & Protagonist!
        var palette = new (string name, string hex, float gloss, string emitHex, float emitStr)[]
        {
            ("AC_Sand", "#F0CFA4", 0.10f, null, 0f),          // Golden-rose sunset beach sand!
            ("AC_SandWet", "#D6AA7E", 0.24f, null, 0f),
            ("AC_Grass", "#6AA676", 0.10f, null, 0f),
            ("AC_GrassLight", "#84BA8A", 0.12f, null, 0f),
            ("AC_Ocean", "#188BA3", 0.52f, null, 0f),          // Vibrant tropical turquoise sea!
            ("AC_OceanShallow", "#38D4D2", 0.48f, null, 0f),
            ("AC_Foam", "#F4FBFE", 0.20f, "#E2F4FA", 0.12f),
            ("AC_HillDistant", "#689488", 0.08f, null, 0f),
            ("AC_HillFar", "#7A98AA", 0.08f, null, 0f),

            ("AC_StoneWhite", "#EFE5D2", 0.14f, null, 0f),
            ("AC_StoneWarm", "#DDD0BA", 0.14f, null, 0f),
            ("AC_StoneDark", "#9E9382", 0.12f, null, 0f),
            ("AC_CobbleA", "#D8C2A8", 0.14f, null, 0f),
            ("AC_CobbleB", "#C5AC92", 0.14f, null, 0f),
            ("AC_PlazaTile", "#EAE0CC", 0.14f, null, 0f),
            ("AC_PlazaAccent", "#DEC7A8", 0.14f, null, 0f),
            ("AC_Asphalt", "#6D7682", 0.10f, null, 0f),
            ("AC_RoadLine", "#F0DFA8", 0.14f, null, 0f),

            ("AC_WoodWarm", "#9E6945", 0.18f, null, 0f),      // Richer tropical teak wood decks!
            ("AC_WoodLight", "#CFA074", 0.20f, null, 0f),
            ("AC_WoodDark", "#6E442A", 0.16f, null, 0f),
            ("AC_Cream", "#FAF2E4", 0.24f, null, 0f),
            ("AC_PinkBerry", "#F26392", 0.30f, null, 0f),     // Hero focal accent!
            ("AC_PinkSoft", "#F6A8C0", 0.28f, null, 0f),
            ("AC_MintTeal", "#4CC6B8", 0.30f, null, 0f),      // Hero cart body turquoise!
            ("AC_MintSoft", "#98DBD1", 0.26f, null, 0f),
            ("AC_PurplePlum", "#4A8C8B", 0.26f, null, 0f),    // Calm coastal teal fascia
            ("AC_Lavender", "#B3A0D4", 0.24f, null, 0f),
            ("AC_Coral", "#D68E7E", 0.24f, null, 0f),         // Toned-down coastal terracotta-coral
            ("AC_SunnyYellow", "#EED286", 0.26f, null, 0f),
            ("AC_SkyBlue", "#7BBAD8", 0.26f, null, 0f),
            ("AC_Glass", "#CBEFF7", 0.62f, "#B4E6F2", 0.18f),
            ("AC_MetalDark", "#475160", 0.42f, null, 0f),
            ("AC_MetalSilver", "#D6DDE6", 0.52f, null, 0f),
            ("AC_Gold", "#E8BE62", 0.48f, null, 0f),

            ("AC_LanternGlow", "#FFE69C", 0.20f, "#FFD26B", 1.35f),
            ("AC_BulbWarm", "#FFF0B3", 0.20f, "#FFE082", 1.25f),

            ("AC_PalmTrunk", "#C6966B", 0.14f, null, 0f),
            ("AC_PalmRing", "#A47852", 0.14f, null, 0f),
            ("AC_PalmLeaf", "#5B9B67", 0.22f, null, 0f),
            ("AC_PalmLeafTip", "#7BB57E", 0.24f, null, 0f),
            ("AC_Coconut", "#734B32", 0.26f, null, 0f),
            ("AC_CoconutGreen", "#6DA968", 0.30f, null, 0f),
            ("AC_BushGreen", "#639A6C", 0.16f, null, 0f),
            ("AC_BushLight", "#7FB182", 0.18f, null, 0f),
            ("AC_Terracotta", "#C97D60", 0.16f, null, 0f),
            ("AC_Orange", "#DF9F68", 0.30f, null, 0f),
            ("AC_Strawberry", "#F27B96", 0.32f, null, 0f),
            ("AC_Chocolate", "#724934", 0.30f, null, 0f),
            ("AC_Vanilla", "#FBF0D4", 0.28f, null, 0f),
            ("AC_Waffle", "#DFAA68", 0.20f, null, 0f),
            ("AC_FlowerPink", "#F290B2", 0.26f, null, 0f),
            ("AC_FlowerYellow", "#F2DA7A", 0.26f, null, 0f),
            ("AC_FlowerBlue", "#88C2E8", 0.26f, null, 0f),
            ("AC_FlowerWhite", "#FBF8F0", 0.26f, null, 0f),
            ("AC_Starfish", "#E28E76", 0.24f, null, 0f),
            ("AC_Shell", "#F8E8DA", 0.32f, null, 0f),

            ("AC_SkinWarm", "#C9855E", 0.24f, null, 0f),
            ("AC_SkinLight", "#F4C29F", 0.24f, null, 0f),
            ("AC_SkinTan", "#DF9F73", 0.24f, null, 0f),
            ("AC_CheekBlush", "#F694AA", 0.24f, null, 0f),
            ("AC_EyeDark", "#251D29", 0.65f, null, 0f),
            ("AC_EyeShine", "#FFFFFF", 0.40f, "#FFFFFF", 0.65f),
            ("AC_HairPlum", "#723678", 0.30f, null, 0f),
            ("AC_HairBrown", "#6B442A", 0.26f, null, 0f),
            ("AC_HairDark", "#332A38", 0.28f, null, 0f),
            ("AC_HairAuburn", "#B45C42", 0.28f, null, 0f),
            ("AC_Denim", "#567EA8", 0.16f, null, 0f),
            ("AC_StrawHat", "#ECCE86", 0.16f, null, 0f),
            ("AC_ShoeDark", "#4E3A4C", 0.24f, null, 0f),
            ("AC_HouseWall", "#F7ECDC", 0.14f, null, 0f),
            ("AC_HouseRoof", "#D98D74", 0.20f, null, 0f),
            ("AC_ProtagShirt", "#F26392", 0.28f, null, 0f),   // Hero focal accent!
            // GPU Vertex-Animated Breeze Hair Materials (upgraded to OrlaACNHHairWind shader below!)
            ("AC_HairWind_Brown", "#6B442A", 0.34f, null, 0f),
            ("AC_HairWind_Blonde", "#EBC772", 0.36f, null, 0f),
            ("AC_HairWind_Dark", "#2E2736", 0.36f, null, 0f),
            ("AC_HairWind_Silver", "#D6D8E0", 0.34f, null, 0f),
            ("AC_HairWind_Auburn", "#B4563C", 0.34f, null, 0f),
        };

        Shader hairWindShader = Shader.Find("GirlPower/OrlaACNHHairWind");
        var dict = new Dictionary<string, Material>();
        foreach (var item in palette)
        {
            Shader targetShader = (item.name.StartsWith("AC_HairWind_") && hairWindShader != null) ? hairWindShader : cozyShader;
            string matPath = MaterialsFolder + "/" + item.name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(targetShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = targetShader;
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
                mat.SetFloat("_CurveStrength", 0.0008f);

            // Bind seamless hand-painted coastal textures (world-space tiled so all meshes look crisp & tactile!)
            AssignHandPaintedTexture(mat, item.name);

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

    private static void AssignHandPaintedTexture(Material mat, string matName)
    {
        if (mat == null || !mat.HasProperty("_MainTex")) return;
        string texFile = null;
        float tileScale = 0.35f;
        switch (matName)
        {
            case "AC_StoneWhite":
            case "AC_StoneWarm":
            case "AC_PlazaTile":
            case "AC_PlazaAccent":
                texFile = "Tex_WalkwayTiles.png";
                tileScale = 0.34f;
                break;
            case "AC_Sand":
            case "AC_SandWet":
                texFile = "Tex_BeachSand.png";
                tileScale = 0.24f;
                break;
            case "AC_Grass":
            case "AC_GrassLight":
                texFile = "Tex_CoastalGrass.png";
                tileScale = 0.28f;
                break;
            case "AC_WoodLight":
            case "AC_WoodWarm":
            case "AC_WoodDark":
                texFile = "Tex_WoodPlanks.png";
                tileScale = 0.55f;
                break;
            case "AC_CobbleA":
            case "AC_CobbleB":
                texFile = "Tex_CobbleFeira.png";
                tileScale = 0.42f;
                break;
        }

        if (!string.IsNullOrEmpty(texFile))
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GirlPower/Orla3D/Textures/" + texFile);
            if (tex != null)
            {
                mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_UseWorldUV")) mat.SetFloat("_UseWorldUV", 1f);
                if (mat.HasProperty("_TileScale")) mat.SetFloat("_TileScale", tileScale);
                return;
            }
        }
        mat.SetTexture("_MainTex", null);
        if (mat.HasProperty("_UseWorldUV")) mat.SetFloat("_UseWorldUV", 0f);
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
