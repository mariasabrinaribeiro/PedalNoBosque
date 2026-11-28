using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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

    private const string BuildVersionKey = "GirlPower_Orla3D_SceneVersion_v50_CampoDeVisao_ComercioAberto_CameraLivre";

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
        RenderSettings.ambientSkyColor = new Color(0.54f, 0.64f, 0.78f);
        RenderSettings.ambientEquatorColor = new Color(0.72f, 0.68f, 0.64f);
        RenderSettings.ambientGroundColor = new Color(0.34f, 0.30f, 0.32f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.90f);
        RenderSettings.fogStartDistance = 58f;
        RenderSettings.fogEndDistance = 135f;

        GameObject root = new GameObject("Orla das Fadas 3D - Resort Magico Pet Friendly");

        // 1. Camera (Iconic Animal Crossing tilted miniature diorama camera!)
        Camera mainCam = CreateCamera(root.transform);

        // 2. Golden-Hour Directional Sun Light
        Light sunLight = CreateSunLight(root.transform);

        // 3. Environment Base (Subdivided 3-surface continuous terrain: ZERO vertical stripes, ZERO self-shadow bands!) + Ocean
        GameObject envBase = PlaceModel(root.transform, "OrlaEnvironmentBase", "Terreno Costeiro - Orla das Fadas", Vector3.zero, 0f, 1f, cozyMats);
        if (envBase != null)
        {
            envBase.transform.localScale = Vector3.one;
            foreach (Renderer er in envBase.GetComponentsInChildren<Renderer>(true))
            {
                er.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                er.receiveShadows = true;
            }
            Renderer r = envBase.GetComponentInChildren<Renderer>();
            if (r != null) Debug.Log("[Orla3D] OrlaEnvironmentBase Bounds: " + r.bounds);
        }
        Transform oceanSurface = CreateAnimatedOcean(root.transform, oceanMat);
        // Surf & swimming intentionally removed — ocean is purely scenic right behind the dry sandy beach!
        BoxCollider waterSurfZone = null;

        // 4. Barraca Central (Quiosque Orla das Fadas) aberta para a praça (sem árvores ou muretas na frente!)
        List<BoxCollider> obstacles = new List<BoxCollider>();
        GameObject kiosk = PlaceModel(root.transform, "KioskGirlPower", "Quiosque Orla das Fadas", new Vector3(-2.2f, 0f, -14.8f), 0f, 1f, cozyMats);
        if (kiosk != null)
        {
            AddBlockingBox(kiosk, new Vector3(0f, 1.9f, 0.06f), new Vector3(4.1f, 3.8f, 1.9f), obstacles);
            Make3DText(kiosk.transform, "Titulo Quiosque", new Vector3(0f, 2.74f, -0.82f), "ORLA DAS FADAS", 1.18f, new Color32(42, 30, 26, 255), new Vector2(2.45f, 0.36f));
        }

        GameObject customSign = PlaceModel(root.transform, "SignboardNew", "Placa Personalizada - Orla das Fadas", new Vector3(-2.2f, 2.74f, -15.65f), 0f, 0.96f, cozyMats);
        TextMeshPro customSignLabel = null;
        if (customSign != null)
        {
            customSignLabel = Make3DText(customSign.transform, "Nome Instalado", new Vector3(0f, 0f, -0.17f), "ORLA DAS FADAS", 1.25f, new Color32(255, 248, 234, 255), new Vector2(2.55f, 0.42f));
            customSign.SetActive(false);
        }

        // Choice Upgrade 1: Volumetric Striped Fabric Awning (Blender FBX, hidden until chosen!)
        GameObject kioskAwningUpgrade = PlaceModel(root.transform, "KioskAwningUpgrade", "Melhoria - Toldo Listrado", new Vector3(-2.2f, 0f, -14.8f), 0f, 1f, cozyMats);
        if (kioskAwningUpgrade != null) kioskAwningUpgrade.SetActive(false);

        // 5. Floricultura da Luna proporcionada (escala 0.82, recuada em z = -5.2) com detalhes rosa da paleta da abertura e amplo espaço à frente!
        GameObject floristStand = PlaceModel(root.transform, "FloristStand", "Banca da Florista - Orla das Fadas", new Vector3(21.8f, 0f, -5.2f), 0f, 0.82f, cozyMats);
        if (floristStand != null)
        {
            AddBlockingBox(floristStand, new Vector3(0f, 1.9f, 0.12f), new Vector3(3.8f, 3.6f, 1.80f), obstacles);
            Make3DText(floristStand.transform, "Titulo Florista", new Vector3(0f, 2.54f, -0.92f), "FLORES DA LUNA", 1.16f, new Color32(58, 22, 38, 255), new Vector2(2.45f, 0.38f));
        }

        List<Transform> breezeCurtains = new List<Transform>();
        GameObject curtL = PlaceModel(root.transform, "FloristCurtain", "Cortina Florista Esq", new Vector3(20.45f, 1.96f, -5.92f), 0f, 0.74f, cozyMats);
        GameObject curtR = PlaceModel(root.transform, "FloristCurtain", "Cortina Florista Dir", new Vector3(23.15f, 1.96f, -5.92f), 0f, 0.74f, cozyMats);
        if (cozyMats.TryGetValue("AC_PinkSoft", out Material roseCurtMat))
        {
            foreach (GameObject cObj in new[] { curtL, curtR })
            {
                if (cObj == null) continue;
                foreach (MeshRenderer cmr in cObj.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Material[] sm = cmr.sharedMaterials;
                    for (int mi = 0; mi < sm.Length; mi++) sm[mi] = roseCurtMat;
                    cmr.sharedMaterials = sm;
                }
            }
        }
        if (curtL != null) breezeCurtains.Add(curtL.transform);
        if (curtR != null) breezeCurtains.Add(curtR.transform);

        // 5b. Pavilhão Orla das Fadas (v49: aberto sem pilar central, teto com fade automático, letreiro frontal voltado à câmera!)
        Vector3 pavilionPos = new Vector3(30.8f, -0.02f, -10.8f);
        GameObject pavilion = PlaceModel(root.transform, "PavilionBay", "Pavilhao Orla das Fadas", pavilionPos, 0f, 1f, cozyMats);
        GameObject pavilionRoof = PlaceModel(root.transform, "PavilionBay_RoofFade", "Pavilhao - Teto Transparente", pavilionPos, 0f, 1f, cozyMats);
        Transform pavSignParent = pavilionRoof != null ? pavilionRoof.transform : root.transform;
        Vector3 pavSignBase = pavilionRoof != null ? Vector3.zero : pavilionPos;
        TextMeshPro pavilionFadedText = Make3DText(pavSignParent, "Letreiro Apagado Pavilhao", pavSignBase + new Vector3(0f, 3.58f, -2.14f), "ORLA DAS FADAS", 1.55f, new Color32(198, 186, 166, 195), new Vector2(2.8f, 0.45f));

        // Festival restoration props (hidden until Chapter 3/Finale restores the Pavilion!)
        GameObject pavilionFestivalTable = PlaceModel(root.transform, "PavilionFestivalTable", "Pavilhao - Mesa Comprida", new Vector3(30.8f, 0.385f, -10.8f), 0f, 1f, cozyMats);
        if (pavilionFestivalTable != null) pavilionFestivalTable.SetActive(false);

        GameObject pavilionFestivalLights = PlaceModel(pavSignParent, "PavilionFestivalLights", "Pavilhao - Luzes e Letreiro Restaurado", pavSignBase, 0f, 1f, cozyMats);
        if (pavilionFestivalLights != null) pavilionFestivalLights.SetActive(false);

        GameObject pavilionFestivalDishes = PlaceModel(root.transform, "FestivalDishesProp", "Pavilhao - Pratos e Vasinhos da Festa", new Vector3(30.8f, 0.385f, -10.8f), 0f, 1f, cozyMats);
        if (pavilionFestivalDishes != null) pavilionFestivalDishes.SetActive(false);

        TextMeshPro pavilionRestoredText = Make3DText(pavSignParent, "Letreiro Restaurado Pavilhao", pavSignBase + new Vector3(0f, 3.60f, -2.18f), "ORLA DAS FADAS", 1.75f, new Color32(255, 245, 205, 255), new Vector2(3.0f, 0.50f));
        pavilionRestoredText.gameObject.SetActive(false);

        List<Light> pavilionPointLights = new List<Light>();
        pavilionPointLights.Add(CreateWarmLight(root.transform, "Luz Pavilhao Centro", new Vector3(30.8f, 2.5f, -10.8f), 2.1f, 11.5f));
        pavilionPointLights.Add(CreateWarmLight(root.transform, "Luz Pavilhao Frente", new Vector3(30.8f, 2.9f, -12.65f), 1.6f, 9.0f));
        foreach (Light pl in pavilionPointLights) pl.enabled = false;

        // Barracas organizadas com corredores amplos (2.4m–4.4m entre cada comércio!) para circular livremente sem bloqueio de visão!
        GameObject stallCoco = PlaceModel(root.transform, "MarketStall_Frutas", "Barraca - Frutas e Sucos", new Vector3(13.2f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallArt = PlaceModel(root.transform, "MarketStall_Artesanato", "Barraca - Artesanato", new Vector3(18.0f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallPlantas = PlaceModel(root.transform, "MarketStall_Plantas", "Barraca - Plantas Naturais", new Vector3(22.8f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallSushi = PlaceModel(root.transform, "MarketStall_Sushi", "Barraca - Sushi da Baia", new Vector3(-11.2f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallComida = PlaceModel(root.transform, "MarketStall_Comida", "Barraca - Pastel e Caldo", new Vector3(-21.0f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallRoupas = PlaceModel(root.transform, "MarketStall_Roupas", "Barraca - Roupas da Orla", new Vector3(27.6f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallDoces = PlaceModel(root.transform, "MarketStall_Doces", "Barraca - Doces e Cupcakes", new Vector3(-25.6f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        GameObject stallSucos = PlaceModel(root.transform, "MarketStall_SucosRefri", "Barraca - Sucos, Agua e Refri", new Vector3(34.8f, 0f, -16.6f), 0f, 0.90f, cozyMats);
        var stallSigns = new (GameObject stall, string signName, string label)[]
        {
            (stallCoco,    "Placa Frutas e Sucos", "FRUTAS & SUCOS"),
            (stallArt,     "Placa Artesanato",     "ARTESANATO"),
            (stallPlantas, "Placa Plantas",        "PLANTAS"),
            (stallSushi,   "Placa Sushi",          "SUSHI"),
            (stallComida,  "Placa Comida",         "PASTEL & CALDO"),
            (stallRoupas,  "Placa Roupas",         "ROUPAS"),
            (stallDoces,   "Placa Doces",          "DOCES"),
            (stallSucos,   "Placa Sucos",          "SUCOS")
        };
        foreach (var entry in stallSigns)
        {
            if (entry.stall == null) continue;
            AddBlockingBox(entry.stall, new Vector3(0f, 1.25f, 0.04f), new Vector3(2.40f, 2.5f, 1.10f), obstacles);
            // Mount each sign flush onto the built-in SOLID CREAM fascia board (z = 2.34m) with crisp DARK ESPRESSO text!
            Make3DText(entry.stall.transform, entry.signName, new Vector3(0f, 2.34f, -0.68f), entry.label, 1.04f, new Color32(42, 30, 26, 255), new Vector2(2.15f, 0.34f));
        }

        // 6a. Restaurante "Batata & Mar" alinhado na ala gastronômica oeste com corredor aberto dos dois lados (ZERO sobreposição!)
        GameObject restKitchen = PlaceModel(root.transform, "RestaurantFriesKitchen", "Restaurante Batata e Mar", new Vector3(-16.0f, 0f, -16.6f), 0f, 1f, cozyMats);
        if (restKitchen != null)
        {
            AddBlockingBox(restKitchen, new Vector3(0f, 1.3f, 0.05f), new Vector3(3.45f, 2.6f, 1.35f), obstacles);
            Make3DText(restKitchen.transform, "Placa Restaurante", new Vector3(0f, 2.34f, -0.82f), "BATATA & MAR", 1.12f, new Color32(42, 30, 26, 255), new Vector2(2.85f, 0.36f));
        }
        // Pavilion: 4 exact corner pillars + back-right chest + solid festival table/benches collider (only active during Finale)!
        if (pavilion != null)
        {
            AddBlockingBox(pavilion, new Vector3(-2.45f, 1.8f, -1.75f), new Vector3(0.44f, 3.6f, 0.44f), obstacles);
            AddBlockingBox(pavilion, new Vector3( 2.45f, 1.8f, -1.75f), new Vector3(0.44f, 3.6f, 0.44f), obstacles);
            AddBlockingBox(pavilion, new Vector3(-2.45f, 1.8f,  1.75f), new Vector3(0.44f, 3.6f, 0.44f), obstacles);
            AddBlockingBox(pavilion, new Vector3( 2.45f, 1.8f,  1.75f), new Vector3(0.44f, 3.6f, 0.44f), obstacles);
            AddBlockingBox(pavilion, new Vector3( 2.08f, 0.62f, 1.32f), new Vector3(0.76f, 1.1f, 0.54f), obstacles);
        }
        if (pavilionFestivalTable != null)
        {
            AddBlockingBox(pavilionFestivalTable, new Vector3(0f, 0.65f, 0f), new Vector3(3.08f, 1.3f, 2.18f), obstacles);
        }

        // 6a2. Enterable "Café das Fadas" (Lanchonete) recuada em z = -15.6 com praça de 3.9m à frente e corredores largos dos dois lados!
        Vector3 cafePos = new Vector3(5.8f, -0.08f, -15.6f);
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
        // Legible "CAFÉ DAS FADAS" signage on BOTH the boardwalk entrance (+Z, rotated 180°) AND south fascia (-Z), fading smoothly with cafeRoof!
        Transform cafeSignParent = cafeRoof != null ? cafeRoof.transform : root.transform;
        Vector3 cafeSignBase = cafeRoof != null ? Vector3.zero : cafePos;
        TextMeshPro cafeFrontSign = Make3DText(cafeSignParent, "Placa Cafe das Fadas", cafeSignBase + new Vector3(0f, 3.02f, 2.88f), "CAFÉ DAS FADAS", 1.35f, new Color32(58, 36, 26, 255), new Vector2(2.8f, 0.44f));
        if (cafeFrontSign != null) cafeFrontSign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Make3DText(cafeSignParent, "Placa Cafe das Fadas Sul", cafeSignBase + new Vector3(0f, 3.02f, -2.22f), "CAFÉ DAS FADAS", 1.25f, new Color32(58, 36, 26, 255), new Vector2(2.8f, 0.44f));

        // 6a3. Low Coastal Town Facades placed well behind the furthest camera Z (z = -36.0) & outer flanks -> ZERO foreground camera blocking!
        float[] facadeXs = { -28.0f, 42.0f };
        for (int i = 0; i < facadeXs.Length; i++)
        {
            GameObject fac = PlaceModel(root.transform, "CoastalFacadeRow", "Fachada Vila Costeira " + (i + 1), new Vector3(facadeXs[i], -0.16f, -36.0f), 0f, 1.0f, cozyMats);
            if (fac != null) fac.transform.localScale = new Vector3(1.04f, 0.58f, 1.0f);
        }

        // 6a4. Clean coastal garden ambiance (out-of-place neon mushrooms removed from sunny beach sand!)

        // Floating lanterns & decorative flower lamps concentrated strictly along the Lateral Plaza & Facades (ZERO crossing obstructions!)
        List<Transform> floatingLanterns = new List<Transform>();
        Vector3[] lanternSpots =
        {
            new Vector3(-7.4f, 2.95f, -8.8f), new Vector3(2.4f, 2.95f, -11.4f), new Vector3(9.4f, 2.95f, -11.4f),
            new Vector3(15.6f, 2.95f, -9.8f), new Vector3(24.0f, 3.05f, -7.2f)
        };
        for (int i = 0; i < lanternSpots.Length; i++)
        {
            GameObject fl = PlaceModel(root.transform, "FloatingLantern", "Lanterna Flutuante " + (i + 1), lanternSpots[i], i * 40f, 0.92f, cozyMats);
            if (fl != null) floatingLanterns.Add(fl.transform);
        }

        // Flower lamp posts placed far to outer corners — ZERO poles crossing protagonist, Cart, Florist Luna, Café, or Pavilion!
        Vector3[] flowerLampSpots = { new Vector3(-6.4f, 0f, -9.8f), new Vector3(11.6f, 0f, -11.4f), new Vector3(24.6f, 0f, -11.8f) };
        for (int i = 0; i < flowerLampSpots.Length; i++)
        {
            GameObject flp = PlaceModel(root.transform, "FlowerLampPost", "Poste com Flores " + (i + 1), flowerLampSpots[i], i == 1 ? 180f : 0f, 0.96f, cozyMats);
            if (flp != null) AddBlockingBox(flp, new Vector3(0f, 1.6f, 0f), new Vector3(0.36f, 3.2f, 0.36f), obstacles);
        }

        // Pet-Friendly Café Tables nicely spaced in front of Café das Fadas (3.9m wide open plaza between doorway & tables!)
        List<Transform> restaurantTables = new List<Transform>();
        List<GameObject> restaurantCustomers = new List<GameObject>();
        Vector3[] tableCoords =
        {
            new Vector3(2.6f, 0f, -9.6f),
            new Vector3(6.2f, 0f, -9.6f),
            new Vector3(9.8f, 0f, -9.6f)
        };
        string[] umbrellaAccentMatNames = { "AC_MintTeal", "AC_PinkSoft", "AC_Lavender" };
        for (int i = 0; i < tableCoords.Length; i++)
        {
            GameObject tbl = PlaceModel(root.transform, "RestaurantKioskTable", "Mesa Quiosque Batata Frita " + (i + 1), tableCoords[i], i * 18f, 1.04f, cozyMats);
            if (tbl != null)
            {
                if (cozyMats.TryGetValue(umbrellaAccentMatNames[i % umbrellaAccentMatNames.Length], out Material umbTint))
                    TintTableUmbrellaAccent(tbl, umbTint);
                AddBlockingBox(tbl, new Vector3(0f, 0.65f, 0f), new Vector3(1.38f, 1.3f, 1.38f), obstacles);
                // Keep 100% opaque (ZWrite On) by default; GirlPowerOrla3DOcclusionFader fades Table_CanopyFade ONLY when crossing the camera ray!
                restaurantTables.Add(tbl.transform);

                // Each restaurant order is represented by a visible guest beside its table.
                string guestModel = (i % 2 == 0) ? "Char_PedestrianBlue" : "Char_PedestrianCoral";
                Vector3 guestPos = tableCoords[i] + new Vector3(1.08f, 0f, 0.25f);
                GameObject guest = PlaceModel(root.transform, guestModel, "Cliente_Mesa_" + (i + 1), guestPos, 90f, 0.78f, cozyMats);
                if (guest != null)
                {
                    guest.SetActive(i < 2);
                }
                restaurantCustomers.Add(guest);
            }
        }

        GameObject gardenBed = PlaceModel(root.transform, "GardenPlanterBed", "Horta da Baia - Canteiros", new Vector3(-5.0f, 0f, -18.1f), 0f, 1f, cozyMats);
        if (gardenBed != null) AddBlockingBox(gardenBed, new Vector3(0f, 0.4f, 0f), new Vector3(2.6f, 0.8f, 1.4f), obstacles);

        GameObject fishingPier = PlaceModel(root.transform, "FishingPierDock", "Pier de Pesca da Baia", new Vector3(31.2f, -0.16f, 7.8f), 0f, 1f, cozyMats);
        GameObject beachTennis = PlaceModel(root.transform, "BeachTennisCourt", "Quadra de Beach Tennis", new Vector3(3.2f, -0.115f, 6.0f), 0f, 1f, cozyMats);
        Transform beachTennisEntrance = null;
        if (beachTennis != null)
        {
            // Solid net/posts blocker across the middle of the court while leaving BOTH lateral accesses wide open!
            AddBlockingBox(beachTennis, new Vector3(0f, 1.0f, 0f), new Vector3(3.6f, 2.0f, 0.28f), obstacles);
            GameObject btEnt = new GameObject("Entrada_Lateral_BeachTennis");
            btEnt.transform.SetParent(beachTennis.transform, false);
            btEnt.transform.localPosition = new Vector3(-2.45f, 0f, -0.35f);
            beachTennisEntrance = btEnt.transform;
        }
        GameObject campingZone = PlaceModel(root.transform, "CampingTent", "Acampamento Familiar da Praia", new Vector3(34.5f, -0.14f, 6.2f), 168f, 1f, cozyMats);
        if (campingZone != null) AddBlockingBox(campingZone, new Vector3(0f, 0.8f, 0f), new Vector3(2.3f, 1.6f, 2.3f), obstacles);

        // Notice board tucked neatly to the left of Bench #1 (leaving the central open passage x = -7.2..+2.2 100% clear!)
        GameObject noticeBoard = PlaceModel(root.transform, "VillageNoticeBoard", "Mural de Avisos da Vila", new Vector3(-12.6f, 0f, -3.75f), 0f, 1f, cozyMats);
        if (noticeBoard != null) AddBlockingBox(noticeBoard, new Vector3(0f, 1.1f, 0f), new Vector3(1.9f, 2.2f, 0.36f), obstacles);

        // Moored Kayak/Canoe resting right along the animated turquoise shoreline!
        // Beach canoe intentionally omitted (surf/swim/canoe water sports removed for clean dry beach!)

        // 6b. Low-Profile Paintable Coastal Houses framing ONLY the OUTER flanks (never passing in front of the camera when walking south!)
        List<Transform> paintableHouses = new List<Transform>();
        Vector3[] housePositions =
        {
            new Vector3(-24.5f, -0.10f, -25.5f),
            new Vector3(42.5f, -0.10f, -25.5f)
        };
        for (int i = 0; i < housePositions.Length; i++)
        {
            GameObject house = PlaceModel(root.transform, "CoastalHouse", "Casa Costeira Pintavel " + (i + 1), housePositions[i], 0f, 0.82f, cozyMats);
            if (house != null)
            {
                house.transform.localScale = new Vector3(0.82f, 0.60f, 0.82f);
                AddBlockingBox(house, new Vector3(0f, 2.0f, 0f), new Vector3(3.8f, 4.0f, 3.1f), obstacles);
                paintableHouses.Add(house.transform);
            }
        }

        List<Transform> swayingFestoons = new List<Transform>();
        float[] festoonXs = { 6.2f, 10.8f, 15.4f };
        for (int i = 0; i < festoonXs.Length; i++)
        {
            GameObject fest = PlaceModel(root.transform, "FestoonLights", "Varal de Bandeirinhas " + (i + 1), new Vector3(festoonXs[i], 3.70f, -15.4f), 0f, 0.80f, cozyMats);
            if (fest != null) swayingFestoons.Add(fest.transform);
        }

        // 7. Flanking Coconut Palms firmly rooted IN the beach sand slope (y = -0.11f -> ZERO floating trunks!)
        List<Transform> swayingPalms = new List<Transform>();
        Vector4[] palmSpecs =
        {
            new Vector4(-15.5f, 4.85f, -24f, 0.92f),
            new Vector4(-7.8f,  5.10f,  32f, 0.78f),
            new Vector4(18.6f,  5.05f, -152f, 0.86f),
            new Vector4(30.6f,  4.85f,  48f, 0.84f),
            new Vector4(36.4f,  5.00f, -138f, 0.94f)
        };
        float[] palmTilts = { -4.5f, 3.5f, -2.5f, 5.0f, -3.5f };
        for (int i = 0; i < palmSpecs.Length; i++)
        {
            Vector4 p = palmSpecs[i];
            GameObject palm = PlaceModel(root.transform, "PalmTree", "Coqueiro da Orla " + (i + 1), new Vector3(p.x, -0.11f, p.y), p.z, p.w, cozyMats);
            if (palm != null)
            {
                palm.transform.localRotation = Quaternion.Euler(palmTilts[i] * 0.5f, p.z, palmTilts[i]);
                swayingPalms.Add(palm.transform);
                AddBlockingBox(palm, new Vector3(0f, 2.3f, 0f), new Vector3(0.58f, 4.6f, 0.58f), obstacles);
            }
        }

        // 8. Varied Coastal Vegetation Planters & Grass Clusters (Blender 5.3 — replacing repetitive identical bushes!)
        List<Light> warmLights = new List<Light>();
        List<Transform> idleCharacters = new List<Transform>();

        var vegSpecs = new (string model, Vector3 pos, float rotY, float scale, bool blocks)[]
        {
            ("CoastalVegetationB", new Vector3(-18.2f, 0f, -3.9f), 4f, 1.0f, true),
            ("CoastalVegetationA", new Vector3(37.0f, 0f, -3.9f), 22f, 1.05f, false),
            ("CoastalVegetationB", new Vector3(-18.5f, 0f, -8.2f), -6f, 0.96f, true),
            ("CoastalVegetationA", new Vector3(37.0f, 0f, -8.2f), 145f, 0.92f, false),
            ("CoastalVegetationB", new Vector3(-22.8f, 0f, -11.6f), 8f, 1.02f, true),
            ("CoastalVegetationA", new Vector3(37.4f, 0f, -11.6f), -35f, 1.08f, false),
            ("CoastalVegetationA", new Vector3(9.6f, 0f, -11.8f), 68f, 0.96f, false)
        };
        for (int i = 0; i < vegSpecs.Length; i++)
        {
            var vs = vegSpecs[i];
            GameObject veg = PlaceModel(root.transform, vs.model, "Jardineira Costeira " + (i + 1), vs.pos, vs.rotY, vs.scale, cozyMats);
            if (veg != null) AddBlockingBox(veg, new Vector3(0f, 0.72f, 0f), new Vector3(1.48f, 1.44f, 0.72f), obstacles);
        }

        // Street lamps placed cleanly between stalls (subtle daytime glow -> zero harsh amber glare on matte wood!)
        Vector2[] lampCoords =
        {
            new Vector2(-17.5f, -9.8f),
            new Vector2(-7.8f, -9.8f),
            new Vector2(10.0f, -9.8f),
            new Vector2(27.6f, -9.8f),
            new Vector2(36.8f, -9.8f)
        };
        for (int i = 0; i < lampCoords.Length; i++)
        {
            float rotY = lampCoords[i].x > 15f ? 180f : 0f;
            GameObject lamp = PlaceModel(root.transform, "StreetLamp", "Poste Costeiro " + (i + 1), new Vector3(lampCoords[i].x, 0f, lampCoords[i].y), rotY, 0.84f, cozyMats);
            if (lamp != null) AddBlockingBox(lamp, new Vector3(0f, 1.8f, 0f), new Vector3(0.34f, 3.6f, 0.34f), obstacles);
            float lightOffsetX = rotY > 90f ? -0.52f : 0.52f;
            warmLights.Add(CreateWarmLight(root.transform, "Luz Poste " + (i + 1), new Vector3(lampCoords[i].x + lightOffsetX, 2.55f, lampCoords[i].y), 0.55f, 7.2f));
        }
        // Subtle kiosk & florist awning glow (balanced so wood/canopy colors stay coherent!)
        warmLights.Add(CreateWarmLight(root.transform, "Luz Quiosque", new Vector3(0f, 2.4f, -11.2f), 0.62f, 7.5f));
        warmLights.Add(CreateWarmLight(root.transform, "Luz Florista", new Vector3(24f, 2.4f, -6.0f), 0.58f, 7.5f));

        // Cohesive lateral rest nook: Trash Bin (-9.6) + Bench #1 (-8.2) + Water Spout #1 (-6.4), with wide open passage beside it!
        float[] benchXs = { -8.2f, 15.2f, 19.8f, 28.6f };
        for (int i = 0; i < benchXs.Length; i++)
        {
            GameObject bench = PlaceModel(root.transform, "CoastalBench", "Banco da Orla " + (i + 1), new Vector3(benchXs[i], 0f, -3.45f), 180f, 1f, cozyMats);
            if (bench != null) AddBlockingBox(bench, new Vector3(0f, 0.48f, 0f), new Vector3(1.80f, 0.96f, 0.65f), obstacles);
        }

        // Route Signpost placed cleanly clear of the Ice Cream Cart & aligned flat so 3D text NEVER clips inside the wood!
        GameObject signpost = PlaceModel(root.transform, "RouteSignpost", "Placa da Bifurcacao", new Vector3(3.45f, 0f, -3.65f), 0f, 1f, cozyMats);
        if (signpost != null)
        {
            AddBlockingBox(signpost, new Vector3(0f, 0.8f, 0f), new Vector3(0.42f, 1.6f, 0.42f), obstacles);
            Make3DText(signpost.transform, "Placa Feira", new Vector3(0.14f, 1.54f, -0.068f), "FEIRA →", 0.88f, new Color32(255, 250, 236, 255), new Vector2(1.25f, 0.30f));
            Make3DText(signpost.transform, "Placa Orla", new Vector3(0.14f, 1.12f, -0.068f), "ORLA →", 0.88f, new Color32(255, 250, 236, 255), new Vector2(1.25f, 0.30f));
        }

        // 9. Beach Split into clean, non-overlapping relaxation & landmark zones:
        //    - WEST: Umbrella 1 + Chair 1 cleanly clear of palm trees & towels
        //    - CENTER: Uncluttered Beach Tennis Court (x=3.2, z=6.0)
        //    - EAST: Umbrella 2 + Chair 2 (x=19.2) with generous 6m spacing before Lifeguard Tower (x=26.0) & Pier (x=31.2)!
        float[] umbXs = { -13.2f, 19.2f };
        for (int i = 0; i < umbXs.Length; i++)
        {
            GameObject umbrella = PlaceModel(root.transform, "BeachUmbrella", "Guarda-Sol " + (i + 1), new Vector3(umbXs[i], -0.115f, 5.85f), -i * 30f, 0.96f, cozyMats);
            if (umbrella != null) AddBlockingBox(umbrella, new Vector3(0f, 1.2f, 0f), new Vector3(0.32f, 2.4f, 0.32f), obstacles);
            GameObject chair = PlaceModel(root.transform, "BeachChair", "Cadeira de Praia " + (i + 1), new Vector3(umbXs[i] + 1.45f, -0.115f, 5.75f), -165f, 1f, cozyMats);
            if (chair != null) AddBlockingBox(chair, new Vector3(0f, 0.32f, 0f), new Vector3(1.12f, 0.64f, 0.78f), obstacles);
        }
        // ONE proportioned Lifeguard Tower spaced cleanly between East Umbrella and Fishing Pier!
        GameObject towerA = PlaceModel(root.transform, "LifeguardTower", "Posto Salva-Vidas 1", new Vector3(26.0f, -0.135f, 6.2f), 180f, 0.86f, cozyMats);
        if (towerA != null) AddBlockingBox(towerA, new Vector3(0f, 1.15f, 0f), new Vector3(1.48f, 2.3f, 1.48f), obstacles);

        // 10. Feira Obstacles (Crates tucked to the sides of stalls & Florist; NPCs at border destinations off main walkway!)
        GameObject crateOr = PlaceModel(root.transform, "CrateOranges", "Caixa de Laranjas", new Vector3(11.4f, 0f, -12.6f), -12f, 1f, cozyMats);
        if (crateOr != null) AddBlockingBox(crateOr, new Vector3(0f, 0.4f, 0f), new Vector3(0.78f, 0.8f, 0.78f), obstacles);

        GameObject crateFl = PlaceModel(root.transform, "CrateFlowers", "Caixa de Flores", new Vector3(18.8f, 0f, -6.4f), 8f, 1f, cozyMats);
        if (crateFl != null) AddBlockingBox(crateFl, new Vector3(0f, 0.4f, 0f), new Vector3(0.78f, 0.8f, 0.78f), obstacles);

        GameObject pedBlue = PlaceModel(root.transform, "Char_PedestrianBlue", "Beto Praiano", new Vector3(15.8f, 0f, -5.2f), 205f, 1f, cozyMats);
        if (pedBlue != null)
        {
            AddBlockingBox(pedBlue, new Vector3(0f, 0.75f, 0f), new Vector3(0.62f, 1.5f, 0.62f), obstacles);
            idleCharacters.Add(pedBlue.transform);
        }

        GameObject pedCoral = PlaceModel(root.transform, "Char_PedestrianCoral", "Clara da Feira", new Vector3(17.8f, 0f, -11.4f), 165f, 1f, cozyMats);
        if (pedCoral != null)
        {
            AddBlockingBox(pedCoral, new Vector3(0f, 0.75f, 0f), new Vector3(0.62f, 1.5f, 0.62f), obstacles);
            idleCharacters.Add(pedCoral.transform);
        }

        // 11. Hero Ice Cream Cart in dedicated open recess (100% opaque ZWrite On + tight physical counter collider!)
        GameObject cart = PlaceModel(root.transform, "IceCreamCart", "Carrinho de Sorvete", new Vector3(0.4f, 0f, -3.45f), 0f, 1.08f, cozyMats);
        if (cart != null)
            AddBlockingBox(cart, new Vector3(0f, 0.9f, 0.05f), new Vector3(1.60f, 1.8f, 0.92f), obstacles);

        // Crisp cream-white typography on the Hero Strawberry-Pink Ice Cream Cart + Choice Upgrade 2
        GameObject cartPaintedSignUpgrade = null;
        if (cart != null)
        {
            Make3DText(cart.transform, "Placa Frontal Sorvetes", new Vector3(0f, 0.88f, -0.57f), "SORVETES", 0.92f, new Color32(255, 250, 238, 255), new Vector2(1.10f, 0.30f));
            cartPaintedSignUpgrade = PlaceModel(cart.transform, "CartPaintedSignUpgrade", "Melhoria - Placa Pintada", new Vector3(0f, 1.62f, -0.52f), 0f, 0.95f, cozyMats);
            if (cartPaintedSignUpgrade != null) cartPaintedSignUpgrade.SetActive(false);
        }

        // Florist's gifted Basil Pot (appears beside cart pocket after first delivery!)
        GameObject basilPot = PlaceModel(root.transform, "BasilPot", "Vaso de Manjericao da Florista", new Vector3(2.35f, 0f, -3.55f), -12f, 1.15f, cozyMats);
        if (basilPot != null) basilPot.SetActive(false);

        // Amiga 1: Nina (at the Kiosk entrance — clear of the Ice Cream Cart & main promenade!)
        GameObject friendNina = PlaceModel(root.transform, "Char_FriendNina", "Amiga 1 - Nina (Quiosque)", new Vector3(-4.6f, 0f, -11.8f), 165f, 1.0f, cozyMats);
        if (friendNina != null)
        {
            AddBlockingBox(friendNina, new Vector3(0f, 0.75f, 0f), new Vector3(0.60f, 1.5f, 0.60f), obstacles);
            idleCharacters.Add(friendNina.transform);
        }

        // Amiga 2: Bia cheerfully waiting near the right side of the Cart (featured in the right-side Opening Showcase, completely clear of the walkway!)
        GameObject friendBia = PlaceModel(root.transform, "Char_FriendBia", "Amiga 2 - Bia (Praca)", new Vector3(2.45f, 0f, -3.85f), 212f, 1.0f, cozyMats);
        if (friendBia != null)
        {
            AddBlockingBox(friendBia, new Vector3(0f, 0.75f, 0f), new Vector3(0.60f, 1.5f, 0.60f), obstacles);
            idleCharacters.Add(friendBia.transform);
        }

        GameObject playerRoot = new GameObject("Protagonista 3D");
        playerRoot.transform.SetParent(root.transform, false);
        // Hero Protagonist starts in wide-open sunlit corridor facing the player & cart with generous breathing room!
        playerRoot.transform.position = new Vector3(-1.4f, 0f, -2.0f);
        GameObject playerVisual = PlaceModel(playerRoot.transform, "Char_Protagonist", "Visual Protagonista", Vector3.zero, 165f, 1.42f, cozyMats);
        GameObject heldCone = PlaceModel(playerRoot.transform, "IceCreamConeLemonBasil", "Casquinha Limao Manjericao na Mao", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkate = PlaceModel(playerRoot.transform, "Skateboard", "Skate da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerScooter = PlaceModel(playerRoot.transform, "Scooter", "Patinete da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerSkates = PlaceModel(playerRoot.transform, "RollerSkates", "Patins da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        GameObject playerHat = PlaceModel(playerRoot.transform, "WardrobeHatProp", "Chapeu e Oculos da Protagonista", Vector3.zero, 0f, 1f, cozyMats);
        // Surfboard & Hawaiian Canoe intentionally removed (surf & swimming eliminated from the game!)

        // Cute Pet Companions spaced comfortably on the left & right flanks (never crowding the Hero or Cart!)
        GameObject dogObj = PlaceModel(root.transform, "PetDog", "Cachorro Pipoca", new Vector3(-4.2f, 0f, -1.6f), 145f, 1.05f, cozyMats);
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
            petDog.followOffset = new Vector3(-1.45f, 0f, -0.85f);
        }

        // Street Thief & Coastal Police Patrol (Blender 5.3 models!)
        GameObject thiefObj = PlaceModel(root.transform, "Char_Thief", "Ladrao de Rua - Baia", new Vector3(-18f, 0f, -2.1f), 0f, 1f, cozyMats);
        if (thiefObj != null) thiefObj.SetActive(false);
        GameObject policeOfficerObj = PlaceModel(root.transform, "Char_PoliceOfficer", "Policial Costeiro", new Vector3(-20f, 0f, -2.4f), 0f, 1f, cozyMats);
        if (policeOfficerObj != null) policeOfficerObj.SetActive(false);
        GameObject policeCarObj = PlaceModel(root.transform, "PolicePatrolCar", "Viatura Policia Costeira", new Vector3(-22f, 0f, -4.4f), -90f, 0.92f, cozyMats);
        if (policeCarObj != null) policeCarObj.SetActive(false);

        GameObject catObj = PlaceModel(root.transform, "PetCat", "Gato Mingau", new Vector3(3.6f, 0f, -1.2f), 205f, 1.05f, cozyMats);
        GirlPowerOrla3DPetCompanion petCat = null;
        if (catObj != null)
        {
            petCat = catObj.AddComponent<GirlPowerOrla3DPetCompanion>();
            petCat.petName = "Mingau";
            petCat.isCat = true;
            petCat.followTarget = playerRoot.transform;
            petCat.followOffset = new Vector3(1.45f, 0f, -0.85f);
        }

        // Florista Luna standing cleanly with 2m open space in front of her counter facing the promenade & camera (-Z)!
        GameObject florist = PlaceModel(root.transform, "Char_Florist", "Florista Luna", new Vector3(21.8f, 0f, -7.25f), 186f, 1f, cozyMats);
        if (florist != null) idleCharacters.Add(florist.transform);

        // 11b. Living Sky & Coastal Wildlife (Wide 2D Horizon Clouds live in Parallax_Horizonte_3Camadas -> ZERO loose floating discs!)
        List<Transform> clouds = new List<Transform>();

        // Seagulls grouped into 2 small quiet flocks at outer sides, leaving wide open central sea for ivory sailboats!
        List<Transform> seagulls = new List<Transform>();
        Vector3[] gullPositions =
        {
            new Vector3(-11.8f, 5.2f, 14.2f),
            new Vector3(-9.8f,  5.6f, 15.0f),
            new Vector3(24.6f,  5.5f, 14.8f),
            new Vector3(26.8f,  5.2f, 13.8f)
        };
        for (int i = 0; i < gullPositions.Length; i++)
        {
            GameObject g = PlaceModel(root.transform, "Seagull3D", "Gaivota Costeira " + (i + 1), gullPositions[i], 0f, 0.78f, cozyMats);
            if (g != null) seagulls.Add(g.transform);
        }

        List<Transform> butterflies = new List<Transform>();
        Vector3[] bfPositions =
        {
            new Vector3(-3.2f, 1.35f, 0.6f),
            new Vector3(13.8f, 1.25f, -0.1f),
            new Vector3(20.6f, 1.45f, -4.8f),
            new Vector3(23.0f, 1.55f, -4.6f)
        };
        for (int i = 0; i < bfPositions.Length; i++)
        {
            GameObject bf = PlaceModel(root.transform, "Butterfly3D", "Borboleta da Orla " + (i + 1), bfPositions[i], 0f, 1.1f, cozyMats);
            if (bf != null) butterflies.Add(bf.transform);
        }

        // Crabs firmly grounded ON THE DRY/MOIST GOLDEN BEACH SAND (z = 6.95 + curve*0.45 -> y = -0.145m, NEVER over the ocean!)
        List<Transform> crabs = new List<Transform>();
        float[] crabXs = { 1.5f, 11.8f, 22.2f };
        for (int i = 0; i < crabXs.Length; i++)
        {
            float cx = crabXs[i];
            float curve = Mathf.Sin(cx * 0.045f) * 0.65f + Mathf.Cos(cx * 0.022f) * 0.30f;
            GameObject crab = PlaceModel(root.transform, "BeachCrab", "Caranguejo da Praia " + (i + 1), new Vector3(cx, -0.148f, 6.95f + curve * 0.45f), 0f, 1.0f, cozyMats);
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
        GameObject sisterMarina = PlaceModel(root.transform, "Char_SisterMarina", "Marina - Irma da Florista", new Vector3(-7.6f, 0f, -1.65f), 155f, 1f, cozyMats);
        if (sisterMarina != null)
        {
            idleCharacters.Add(sisterMarina.transform);
            sisterMarina.SetActive(false);
        }

        Vector3[] cardPositions =
        {
            new Vector3(4.2f, 0.05f, -2.1f),    // Boardwalk near benches
            new Vector3(7.6f, 0.05f, 0.85f),    // Square near Bia
            new Vector3(16.4f, 0.05f, -3.1f),   // Feira cobblestone lane
            new Vector3(30.8f, 0.14f, -7.8f)    // Pavilion entrance steps (the Sister's invitation!)
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

        // Canopy-only smooth occlusion: whenever ANY stall awning, table umbrella, cart canopy, or house roof crosses the camera-to-player ray, ONLY that overhead cover fades smoothly while counters/bases remain 100% solid!
        List<Transform> occluderRoots = new List<Transform>();
        foreach (Transform h in paintableHouses) if (h != null) occluderRoots.Add(h);
        foreach (GameObject coverObj in new[] { kiosk, floristStand, cart, cafeBase, restKitchen, towerA, stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (coverObj != null) occluderRoots.Add(coverObj.transform);
        foreach (Transform tbl in restaurantTables)
            if (tbl != null) occluderRoots.Add(tbl);

        GameObject occObj = new GameObject("Oclusao_Transparencia_Construcoes");
        occObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DOcclusionFader occFader = occObj.AddComponent<GirlPowerOrla3DOcclusionFader>();
        occFader.player = playerRoot.transform;
        occFader.targetCamera = mainCam;
        occFader.occluderRoots = occluderRoots.ToArray();
        occFader.cutoutRadius = 1.45f;

        // 11f. Contact / Blob Shadows anchoring every character, pet, cart, bench, stall, table & prop to the ground!
        List<GirlPowerOrla3DContactShadows.ShadowEntry> shadowEntries = new List<GirlPowerOrla3DContactShadows.ShadowEntry>();
        AddShadow(shadowEntries, playerRoot.transform, new Vector2(1.28f, 1.28f), true);
        if (friendNina != null) AddShadow(shadowEntries, friendNina.transform, new Vector2(1.14f, 1.14f), true);
        if (friendBia != null) AddShadow(shadowEntries, friendBia.transform, new Vector2(1.14f, 1.14f), true);
        if (florist != null) AddShadow(shadowEntries, florist.transform, new Vector2(1.18f, 1.18f), true);
        if (sisterMarina != null) AddShadow(shadowEntries, sisterMarina.transform, new Vector2(1.14f, 1.14f), true);
        if (pedBlue != null) AddShadow(shadowEntries, pedBlue.transform, new Vector2(1.14f, 1.14f), true);
        if (pedCoral != null) AddShadow(shadowEntries, pedCoral.transform, new Vector2(1.14f, 1.14f), true);
        if (dogObj != null) AddShadow(shadowEntries, dogObj.transform, new Vector2(0.92f, 0.92f), true);
        if (catObj != null) AddShadow(shadowEntries, catObj.transform, new Vector2(0.84f, 0.84f), true);
        if (thiefObj != null) AddShadow(shadowEntries, thiefObj.transform, new Vector2(1.14f, 1.14f), true);
        if (policeOfficerObj != null) AddShadow(shadowEntries, policeOfficerObj.transform, new Vector2(1.14f, 1.14f), true);

        if (cart != null) AddShadow(shadowEntries, cart.transform, new Vector2(2.35f, 1.65f), false);
        if (kiosk != null) AddShadow(shadowEntries, kiosk.transform, new Vector2(4.90f, 2.55f), false);
        if (floristStand != null) AddShadow(shadowEntries, floristStand.transform, new Vector2(4.55f, 2.35f), false);
        if (restKitchen != null) AddShadow(shadowEntries, restKitchen.transform, new Vector2(4.10f, 2.15f), false);
        if (basilPot != null) AddShadow(shadowEntries, basilPot.transform, new Vector2(0.75f, 0.75f), true);
        if (signpost != null) AddShadow(shadowEntries, signpost.transform, new Vector2(0.85f, 0.85f), false);
        if (gardenBed != null) AddShadow(shadowEntries, gardenBed.transform, new Vector2(2.90f, 1.65f), false);
        if (noticeBoard != null) AddShadow(shadowEntries, noticeBoard.transform, new Vector2(2.35f, 0.85f), false);
        if (campingZone != null) AddShadow(shadowEntries, campingZone.transform, new Vector2(2.80f, 2.80f), false);
        foreach (Transform tbl in restaurantTables)
            if (tbl != null) AddShadow(shadowEntries, tbl, new Vector2(2.55f, 2.55f), false);
        if (crateOr != null) AddShadow(shadowEntries, crateOr.transform, new Vector2(1.25f, 1.25f), false);
        if (crateFl != null) AddShadow(shadowEntries, crateFl.transform, new Vector2(1.25f, 1.25f), false);
        foreach (GameObject st in new[] { stallCoco, stallArt, stallPlantas, stallSushi, stallComida, stallRoupas, stallDoces, stallSucos })
            if (st != null) AddShadow(shadowEntries, st.transform, new Vector2(3.15f, 1.85f), false);

        // 11g. Living Ecology & Roaming Villagers spaced generously along flanks (ZERO crowding around the Cart!)
        List<Transform> strollingNpcs = new List<Transform>();
        GameObject walker1 = PlaceModel(root.transform, "Char_PedestrianBlue", "Turista Calcadão 1", new Vector3(-10.8f, 0f, 1.5f), 20f, 1.0f, cozyMats);
        GameObject walker2 = PlaceModel(root.transform, "Char_PedestrianCoral", "Turista Calcadão 2", new Vector3(14.2f, 0f, 1.5f), -18f, 1.0f, cozyMats);
        GameObject camper1 = PlaceModel(root.transform, "Char_FriendBia", "Familia Acampamento", new Vector3(33.2f, -0.14f, 4.9f), 145f, 1.0f, cozyMats);
        if (walker1 != null) { strollingNpcs.Add(walker1.transform); AddShadow(shadowEntries, walker1.transform, new Vector2(1.0f, 1.0f), true); }
        if (walker2 != null) { strollingNpcs.Add(walker2.transform); AddShadow(shadowEntries, walker2.transform, new Vector2(1.0f, 1.0f), true); }
        if (camper1 != null) { idleCharacters.Add(camper1.transform); AddShadow(shadowEntries, camper1.transform, new Vector2(1.0f, 1.0f), true); }
        Vector3[] beachWalkerPositions =
        {
            new Vector3(-12.6f, -0.12f, 4.85f), new Vector3(-7.6f, -0.12f, 5.15f),
            new Vector3(11.4f, -0.12f, 5.15f), new Vector3(27.8f, -0.12f, 4.85f)
        };
        for (int i = 0; i < beachWalkerPositions.Length; i++)
        {
            string model = (i % 2 == 0) ? "Char_PedestrianCoral" : "Char_PedestrianBlue";
            GameObject walker = PlaceModel(root.transform, model, "Turista Praia " + (i + 1), beachWalkerPositions[i], (i % 2 == 0) ? 15f : -15f, 0.96f + (i % 2) * 0.06f, cozyMats);
            if (walker != null)
            {
                strollingNpcs.Add(walker.transform);
                AddShadow(shadowEntries, walker.transform, new Vector2(1.0f, 1.0f), true);
            }
        }

        List<Transform> beachSeagulls = new List<Transform>();
        Vector3[] perchedGulls = { new Vector3(-9.2f, -0.168f, 7.35f), new Vector3(23.8f, -0.165f, 7.25f) };
        for (int i = 0; i < perchedGulls.Length; i++)
        {
            GameObject bg = PlaceModel(root.transform, "Seagull3D", "Gaivota na Areia " + (i + 1), perchedGulls[i], i * 45f, 0.76f, cozyMats);
            if (bg != null)
            {
                beachSeagulls.Add(bg.transform);
                AddShadow(shadowEntries, bg.transform, new Vector2(0.55f, 0.55f), false);
            }
        }
        foreach (Transform crabTr in crabs)
            if (crabTr != null) AddShadow(shadowEntries, crabTr, new Vector2(0.50f, 0.50f), true);

        List<Transform> shallowFish = new List<Transform>();
        for (int i = 0; i < 6; i++)
        {
            GameObject fish = PlaceModel(root.transform, "FishProp", "Peixe Tropical " + (i + 1), new Vector3(-6f + i * 7.2f, -0.26f, 11.4f + (i % 2) * 1.4f), i * 35f, 1.0f, cozyMats);
            if (fish != null) shallowFish.Add(fish.transform);
        }

        // Phase 2 Expansion Group (disabled during focused Phase 1 Vertical Slice for crisp 60 FPS mobile clarity!)
        GameObject phase2ExpansionGroup = new GameObject("Fase2_Expansao_Conteudo");
        phase2ExpansionGroup.transform.SetParent(root.transform, false);
        phase2ExpansionGroup.SetActive(false);

        // Floating Fairy Waitress centered cleanly in Café das Fadas courtyard (never crowding the Cart or Beach Tennis net!)
        List<Transform> fairyList = new List<Transform>();
        Vector3[] fairySpawns =
        {
            new Vector3(6.2f, 0.75f, -9.8f),
            new Vector3(-11.2f, 0.72f, -4.6f),
            new Vector3(24.0f, 0.74f, -6.2f)
        };
        for (int i = 0; i < fairySpawns.Length; i++)
        {
            Transform parentForFairy = (i == 0) ? root.transform : phase2ExpansionGroup.transform;
            GameObject fy = PlaceModel(parentForFairy, "Char_FairyWaitress", "Fada Garconete " + (i + 1), fairySpawns[i], i * 45f, 1.05f, cozyMats);
            if (fy != null && i == 0)
            {
                fairyList.Add(fy.transform);
                AddShadow(shadowEntries, fy.transform, new Vector2(0.82f, 0.82f), true);
            }
        }

        // Distant sailboats are handled exclusively by the 3 well-spaced, low-contrast Parallax Horizon vessels (no clutter!)
        List<Transform> distantBoats = new List<Transform>();

        // Seamless Café das Fadas Roof Fade Controller (strict interior 0.5m inset margin -> never flickers on outside sidewalk!)
        if (cafeRoof != null)
        {
            GameObject cafeFadeObj = new GameObject("Cafe_Teto_Fade_Automatico");
            cafeFadeObj.transform.SetParent(root.transform, false);
            GirlPowerOrla3DCafeRoofFade cafeFade = cafeFadeObj.AddComponent<GirlPowerOrla3DCafeRoofFade>();
            cafeFade.player = playerRoot.transform;
            cafeFade.roofObject = cafeRoof.transform;
            cafeFade.targetCamera = mainCam;
            cafeFade.buildingCenter = cafePos;
            cafeFade.halfBoundsXZ = new Vector2(3.05f, 2.05f);
            cafeFade.interiorWallMargin = 0.50f;
        }

        // Seamless Pavilion Roof Fade Controller: stays 100% opaque outside, smoothly vanishes whenever player steps onto/behind Pavilion!
        if (pavilionRoof != null)
        {
            GameObject pavFadeObj = new GameObject("Pavilhao_Teto_Fade_Automatico");
            pavFadeObj.transform.SetParent(root.transform, false);
            GirlPowerOrla3DCafeRoofFade pavFade = pavFadeObj.AddComponent<GirlPowerOrla3DCafeRoofFade>();
            pavFade.player = playerRoot.transform;
            pavFade.roofObject = pavilionRoof.transform;
            pavFade.targetCamera = mainCam;
            pavFade.buildingCenter = pavilionPos;
            pavFade.halfBoundsXZ = new Vector2(3.95f, 3.65f);
            pavFade.interiorWallMargin = 0.50f;
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
        Vector3[] orchardCoords = { new Vector3(-8.0f, -0.05f, -16.5f), new Vector3(-3.0f, -0.05f, -20.1f), new Vector3(-8.0f, -0.05f, -21.0f) };
        for (int i = 0; i < orchardCoords.Length; i++)
        {
            GameObject ot = PlaceModel(root.transform, "OrchardFruitTree", "Arvore Pomar Horta " + (i + 1), orchardCoords[i], i * 55f, 0.92f + i * 0.08f, cozyMats);
            if (ot != null)
            {
                AddBlockingBox(ot, new Vector3(0f, 1.4f, 0f), new Vector3(0.95f, 2.8f, 0.95f), obstacles);
                orchardTrees.Add(ot.transform);
            }
        }
        GameObject camponesaObj = PlaceModel(root.transform, "Char_Camponesa", "Camponesa Colhedora da Horta", new Vector3(-4.6f, 0f, -16.2f), 20f, 1.0f, cozyMats);
        if (camponesaObj != null) AddShadow(shadowEntries, camponesaObj.transform, new Vector2(1.0f, 1.0f), true);

        // Thick, rounded mobile-legible wood fence (0.36m robust beams -> zero jagged thin lines on mobile!)
        Material gardenFenceMat = cozyMats.TryGetValue("AC_WoodLight", out Material gardenWood) ? gardenWood : null;
        CreateGardenFencePart(root.transform, obstacles, gardenFenceMat, "Cerca_Horta_Fundo", new Vector3(-5.0f, 0.48f, -22.8f), new Vector3(11.4f, 0.96f, 0.36f));
        CreateGardenFencePart(root.transform, obstacles, gardenFenceMat, "Cerca_Horta_Lateral_Oeste", new Vector3(-10.6f, 0.48f, -18.0f), new Vector3(0.36f, 0.96f, 9.8f));
        CreateGardenFencePart(root.transform, obstacles, gardenFenceMat, "Cerca_Horta_Lateral_Leste", new Vector3(0.6f, 0.48f, -18.0f), new Vector3(0.36f, 0.96f, 9.8f));
        CreateGardenFencePart(root.transform, obstacles, gardenFenceMat, "Cerca_Horta_Entrada_Oeste", new Vector3(-8.65f, 0.48f, -13.2f), new Vector3(3.9f, 0.96f, 0.36f));
        CreateGardenFencePart(root.transform, obstacles, gardenFenceMat, "Cerca_Horta_Entrada_Leste", new Vector3(-1.35f, 0.48f, -13.2f), new Vector3(3.9f, 0.96f, 0.36f));
        Make3DText(root.transform, "Placa Horta e Pomar", new Vector3(-5.0f, 1.72f, -12.95f), "HORTA & POMAR", 1.15f, new Color32(58, 36, 26, 255), new Vector2(2.6f, 0.42f));

        // PHASE 1 VERTICAL SLICE: 3 Unmistakable Vacation Guests with 3 Pets (Cachorro, Gato, Calopsita)!
        // Additional resort archetypes live cleanly in Fase2_Expansao_Conteudo for Etapa 5 expansion at 60 FPS!
        var guestSpecs = new (GirlPowerOrla3DArchetypeConfig.ArchetypeKind kind, string fbx, string name, Vector3 pos, GirlPowerOrla3DArchetypeConfig.PetKind pet, bool phase1Active)[]
        {
            // Phase 1 Active Core Trio well spaced along lateral plazas (ZERO crowding near Cart, Luna, or central walkway!)
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Barbie,           "Char_Barbie",           "Turista - Estilo Barbie",        new Vector3(9.4f, 0f, -8.2f),    GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro,  true),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Japones,          "Char_Japones",          "Turista - Estilo Japones",       new Vector3(-9.2f, 0f, -7.8f),   GirlPowerOrla3DArchetypeConfig.PetKind.Gato,      true),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Vovo,             "Char_Vovo",             "Turista - Vovo Carinhosa",       new Vector3(13.2f, 0f, -8.2f),   GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita, true),
            // Phase 2 Resort Expansion Guests (ready to activate in Etapa 5!)
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Gaucho,           "Char_Gaucho",           "Turista - Gaucho do Sul",        new Vector3(-8.2f, 0f, -6.6f),   GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro,  false),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Espia,            "Char_Espia",            "Turista - Espia Estilosa",       new Vector3(1.4f, 0f, -7.1f),    GirlPowerOrla3DArchetypeConfig.PetKind.Gato,      false),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.Princesa,         "Char_Princesa",         "Turista - Princesa da Orla",     new Vector3(13.8f, 0f, -7.0f),   GirlPowerOrla3DArchetypeConfig.PetKind.Gato,      false),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.KungFu,           "Char_KungFu",           "Turista - Mestre Kung Fu",       new Vector3(23.0f, 0f, -7.0f),   GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro,  false),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.PraiaFutebol,     "Char_FutebolPraia",     "Praiano - Futebol de Areia",     new Vector3(6.8f, -0.14f, 6.0f), GirlPowerOrla3DArchetypeConfig.PetKind.Cachorro,  false),
            (GirlPowerOrla3DArchetypeConfig.ArchetypeKind.FutebolAmericano, "Char_FutebolAmericano", "Praiano - Futebol Americano",    new Vector3(19.2f, -0.14f, 6.2f),GirlPowerOrla3DArchetypeConfig.PetKind.None,      false)
        };

        List<GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry> guestEntries = new List<GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry>();
        for (int i = 0; i < guestSpecs.Length; i++)
        {
            var gs = guestSpecs[i];
            Transform parentForGuest = gs.phase1Active ? root.transform : phase2ExpansionGroup.transform;
            GameObject actor = PlaceModel(parentForGuest, gs.fbx, gs.name, gs.pos, (i % 2 == 0) ? 18f : -18f, 1.0f, cozyMats);
            if (actor == null) continue;
            if (gs.phase1Active)
                AddShadow(shadowEntries, actor.transform, new Vector2(1.02f, 1.02f), true);

            Transform petT = null;
            if (gs.pet != GirlPowerOrla3DArchetypeConfig.PetKind.None)
            {
                string petFbx = gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita ? "Pet_Calopsita"
                    : (gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Gato ? "PetCat" : "PetDog");
                GameObject petGo = PlaceModel(parentForGuest, petFbx, "Pet_" + gs.kind + "_" + gs.pet, gs.pos + new Vector3(-0.75f, 0f, -0.55f), 0f, gs.pet == GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita ? 1.05f : 0.82f, cozyMats);
                if (petGo != null)
                {
                    petT = petGo.transform;
                    if (gs.phase1Active && gs.pet != GirlPowerOrla3DArchetypeConfig.PetKind.Calopsita)
                        AddShadow(shadowEntries, petT, new Vector2(0.68f, 0.68f), true);
                }
            }

            if (gs.phase1Active)
            {
                guestEntries.Add(new GirlPowerOrla3DPetFriendlySystem.ArchetypeActorEntry
                {
                    actor = actor.transform,
                    petCompanion = petT,
                    kind = gs.kind,
                    petKind = gs.pet
                });
            }
        }

        // PHASE 1 VERTICAL SLICE: 2 Prominent Coastal Water Spout Stations grouped with the lateral rest nook!
        // Station #1 (-6.4, -3.45) sits right beside Bench #1 (-8.2) & Trash Bin #1 (-9.6), leaving the passage at x=-5.4..+2.6 wide open!
        var bowlSpecs = new (string label, Vector3 pos, bool phase1Active)[]
        {
            ("Pote Agua - Banco Orla",    new Vector3(-6.4f, 0f, -3.45f),  true),
            ("Pote Agua - Recuo Mesas",   new Vector3(13.4f, 0f, -3.45f),  true),
            ("Pote Agua - Cafe Fadas",    new Vector3(3.0f, 0f, -12.60f),  false),
            ("Pote Agua - Feira Central", new Vector3(17.8f, 0f, -3.55f),  false),
            ("Pote Agua - Florista",      new Vector3(21.4f, 0f, -6.20f),  false)
        };
        List<GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry> bowlEntries = new List<GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry>();
        for (int i = 0; i < bowlSpecs.Length; i++)
        {
            Transform parentForBowl = bowlSpecs[i].phase1Active ? root.transform : phase2ExpansionGroup.transform;
            GameObject bowl = PlaceModel(parentForBowl, "PetWaterBowl", bowlSpecs[i].label, bowlSpecs[i].pos, 0f, 1.12f, cozyMats);
            if (bowl != null && bowlSpecs[i].phase1Active)
            {
                // Tight base collider on the wooden water post so the player approaches closely without clipping through the post
                AddBlockingBox(bowl, new Vector3(0f, 0.52f, 0.14f), new Vector3(0.56f, 1.04f, 0.54f), obstacles);
                AddShadow(shadowEntries, bowl.transform, new Vector2(1.08f, 1.08f), false);
                bowlEntries.Add(new GirlPowerOrla3DPetFriendlySystem.WaterBowlEntry
                {
                    label = bowlSpecs[i].label,
                    bowlRoot = bowl.transform
                });
            }
        }

        // Cloud shadow quads intentionally removed to eliminate giant semi-transparent ground rectangles & Z-overlap!
        List<Transform> cloudShadows = new List<Transform>();

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
        charAnim.surfboardProp = null;
        charAnim.skateboardProp = playerSkate != null ? playerSkate.transform : null;
        charAnim.scooterProp = playerScooter != null ? playerScooter.transform : null;
        charAnim.rollerSkatesProp = playerSkates != null ? playerSkates.transform : null;
        charAnim.hawaiianCanoeProp = null;
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
        ambient.cloudShadows = cloudShadows.ToArray();
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
        ambient.floristMorningPos = new Vector3(21.8f, 0f, -7.85f);
        ambient.floristEveningPos = new Vector3(21.8f, 0f, -8.15f);
        ambient.sunLight = sunLight;
        ambient.targetCamera = mainCam;
        ambient.playerTransform = playerRoot.transform;
        ambient.cameraPost = mainCam != null ? mainCam.GetComponent<GirlPowerOrla3DCameraPost>() : null;
        ambient.currentPreset = GirlPowerOrla3DAmbient.TimePreset.FimDeTarde;

        GameObject missionObj = new GameObject("Historia Baia do Avesso - 4 Capitulos");
        missionObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DFirstDelivery mission = missionObj.AddComponent<GirlPowerOrla3DFirstDelivery>();
        // Balanced hero diorama framing: protagonist stays close & readable while ocean shoreline shows cleanly behind the pier!
        mission.cameraOffset = new Vector3(0f, 8.2f, -11.8f);
        mission.cameraPitch = 33.5f;
        mission.minWorldX = -28.0f;
        mission.maxWorldX = 40.5f;
        mission.minWorldZ = -22.2f;
        mission.maxWorldZ = 7.85f;
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
        BoxCollider[] obstacleArray = obstacles.ToArray();
        mission.blockingObstacles = obstacleArray;
        mission.waterSurfZone = waterSurfZone;
        if (petDog != null)
        {
            petDog.blockingObstacles = obstacleArray;
            petDog.pavilionTransform = pavilion != null ? pavilion.transform : null;
        }
        if (petCat != null)
        {
            petCat.blockingObstacles = obstacleArray;
            petCat.pavilionTransform = pavilion != null ? pavilion.transform : null;
        }

        // One shared, oriented-box path grid drives both tap navigation and strolling residents.
        GameObject pathfinderObj = new GameObject("Navegacao_Orla_AStar_OBB");
        pathfinderObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DPathfinder pathfinder = pathfinderObj.AddComponent<GirlPowerOrla3DPathfinder>();
        pathfinder.blockingObstacles = obstacles.ToArray();
        pathfinder.minX = mission.minWorldX;
        pathfinder.maxX = mission.maxWorldX;
        pathfinder.minZ = mission.minWorldZ;
        pathfinder.maxZ = mission.maxWorldZ;

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
        megaSys.beachTennisEntrance = beachTennisEntrance;
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
        // Hide police car permanently — replaced by magical Fairy Guardian / Coastal Lifeguard theme!
        if (policeCarObj != null) policeCarObj.SetActive(false);
        thiefDet.policeCar = null;
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
        mission.petFriendlySystem = petFriendlySys;
        lifeAct.gameFlow = gameFlow;
        lifeAct.storyController = mission;
        lifeAct.megaSystems = megaSys;
        lifeAct.cardapioUI = cardapio;

        // 13. MOBILE GOLDEN RULE (+40% Oversized Touch Hitboxes) & Exclusive 3-Layer Touch Controller!
        for (int i = 0; i < fairyList.Count; i++)
            GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(fairyList[i].gameObject, GirlPowerOrla3DTouchTarget.TargetRole.FairyWaitress, i, new Vector3(0f, 0.85f, 0f), 0.88f, "Fada");
        for (int i = 0; i < bowlEntries.Count; i++)
            if (bowlEntries[i].bowlRoot != null)
                GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(bowlEntries[i].bowlRoot.gameObject, GirlPowerOrla3DTouchTarget.TargetRole.WaterBowl, i, new Vector3(0f, 0.35f, 0f), 0.68f, "Agua");
        for (int i = 0; i < restaurantTables.Count; i++)
            if (restaurantTables[i] != null)
                GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(restaurantTables[i].gameObject, GirlPowerOrla3DTouchTarget.TargetRole.PetTable, i, new Vector3(0f, 0.75f, 0f), 1.18f, "Mesa");
        if (cart != null)
            GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(cart, GirlPowerOrla3DTouchTarget.TargetRole.CartOrKitchen, 0, new Vector3(0f, 0.95f, 0f), 1.25f, "Carrinho");
        if (restKitchen != null)
            GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(restKitchen, GirlPowerOrla3DTouchTarget.TargetRole.CartOrKitchen, 1, new Vector3(0f, 1.1f, 0f), 1.55f, "Cozinha");
        if (basilPot != null)
            GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(basilPot, GirlPowerOrla3DTouchTarget.TargetRole.GardenOrchard, 0, new Vector3(0f, 0.55f, 0f), 0.80f, "Manjericao");
        for (int i = 0; i < orchardTrees.Count; i++)
            GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(orchardTrees[i].gameObject, GirlPowerOrla3DTouchTarget.TargetRole.GardenOrchard, i + 1, new Vector3(0f, 1.1f, 0f), 1.05f, "Pomar");
        if (lifeAct.shops != null)
        {
            for (int i = 0; i < lifeAct.shops.Length; i++)
                if (lifeAct.shops[i].stallTransform != null)
                    GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(lifeAct.shops[i].stallTransform.gameObject, GirlPowerOrla3DTouchTarget.TargetRole.ShopStall, i, new Vector3(0f, 1.0f, 0f), 1.22f, lifeAct.shops[i].shopName);
        }
        foreach (GameObject storyNpc in new[] { friendNina, florist, sisterMarina, friendBia })
            if (storyNpc != null)
                GirlPowerOrla3DTouchTarget.AttachOversizedHitbox(storyNpc, GirlPowerOrla3DTouchTarget.TargetRole.StoryNpc, 0, new Vector3(0f, 0.95f, 0f), 0.85f, storyNpc.name);

        // 13b. Playable Fairground Businesses (Orange Tree, Mint, Juice Bar, Candy Shop, Craft Studio, Friends & Visible 3D Upgrades!)
        GameObject fairBizRoot = new GameObject("Feira_Jogavel_Amigas_e_Melhorias");
        fairBizRoot.transform.SetParent(root.transform, false);

        // Primary Orange Tree tucked cleanly beside the Orchard corner (ZERO walkway obstruction!)
        GameObject orangeTree1 = PlaceModel(fairBizRoot.transform, "OrangeTreeRipe", "Laranjeira Principal da Horta", new Vector3(-1.4f, 0f, -15.8f), 14f, 0.96f, cozyMats);
        GameObject orangeFruit1 = null;
        if (orangeTree1 != null)
        {
            AddBlockingBox(orangeTree1, new Vector3(0f, 1.3f, 0f), new Vector3(0.75f, 2.6f, 0.75f), obstacles);
            Transform rf = FindDescendant(orangeTree1.transform, "RipeOranges");
            if (rf != null) orangeFruit1 = rf.gameObject;
        }

        // Mint Planter right at the Orchard entrance for quick Hortelã harvesting!
        GameObject mintPot = PlaceModel(fairBizRoot.transform, "BasilPot", "Canteiro de Hortela Fresca", new Vector3(-2.6f, 0f, -13.6f), 18f, 1.25f, cozyMats);

        // 3 Visible Waiting Customers cleanly spaced at Juice Bar, Candy Shop & Craft Studio!
        GameObject juiceCust = PlaceModel(fairBizRoot.transform, "Char_PedestrianCoral", "Cliente_Feira_Sucos", new Vector3(12.2f, 0f, -15.4f), 188f, 0.96f, cozyMats);
        GameObject candyCust = PlaceModel(fairBizRoot.transform, "Char_FriendBia", "Cliente_Feira_Doces", new Vector3(-20.6f, 0f, -13.0f), 178f, 0.95f, cozyMats);
        GameObject craftCust = PlaceModel(fairBizRoot.transform, "Char_Barbie", "Cliente_Feira_Artesanato", new Vector3(16.4f, 0f, -15.4f), 184f, 0.96f, cozyMats);

        // 4 VISIBLE 3D WORLD-TRANSFORMING UPGRADES (hidden until purchased with fair coins!)
        GameObject upgPinkAwnings = new GameObject("Melhoria_ToldoRosa_Feira");
        upgPinkAwnings.transform.SetParent(fairBizRoot.transform, false);
        Material pinkBerryM = cozyMats.TryGetValue("AC_PinkBerry", out Material pbm) ? pbm : null;
        Material pinkSoftM = cozyMats.TryGetValue("AC_PinkSoft", out Material psm) ? psm : null;
        Material creamM = cozyMats.TryGetValue("AC_Cream", out Material crm) ? crm : null;
        Material tealM = cozyMats.TryGetValue("AC_MintTeal", out Material tlm) ? tlm : null;
        Material glowM = cozyMats.TryGetValue("AC_LanternGlow", out Material glm) ? glm : null;
        foreach (GameObject st in new[] { stallCoco, stallArt, stallDoces })
        {
            if (st == null) continue;
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "ToldoRosa_" + st.name;
            canopy.transform.SetParent(upgPinkAwnings.transform, false);
            canopy.transform.position = st.transform.position + new Vector3(0f, 2.72f, -0.15f);
            canopy.transform.localScale = new Vector3(2.85f, 0.14f, 1.85f);
            Object.DestroyImmediate(canopy.GetComponent<Collider>());
            if (pinkBerryM != null) canopy.GetComponent<Renderer>().sharedMaterial = pinkBerryM;

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "FranjaCreme_" + st.name;
            trim.transform.SetParent(upgPinkAwnings.transform, false);
            trim.transform.position = st.transform.position + new Vector3(0f, 2.60f, -1.04f);
            trim.transform.localScale = new Vector3(2.88f, 0.12f, 0.08f);
            Object.DestroyImmediate(trim.GetComponent<Collider>());
            if (creamM != null) trim.GetComponent<Renderer>().sharedMaterial = creamM;
        }
        upgPinkAwnings.SetActive(false);

        GameObject upgOrangeTree2 = PlaceModel(fairBizRoot.transform, "OrangeTreeRipe", "Melhoria_Segunda_Laranjeira", new Vector3(-9.4f, 0f, -15.2f), -26f, 0.94f, cozyMats);
        GameObject orangeFruit2 = null;
        if (upgOrangeTree2 != null)
        {
            Transform rf2 = FindDescendant(upgOrangeTree2.transform, "RipeOranges");
            if (rf2 != null) orangeFruit2 = rf2.gameObject;
            upgOrangeTree2.SetActive(false);
        }

        GameObject upgShowcase = new GameObject("Melhoria_VitrineIluminada");
        upgShowcase.transform.SetParent(fairBizRoot.transform, false);
        foreach (GameObject st in new[] { stallArt, stallDoces })
        {
            if (st == null) continue;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject lampBulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lampBulb.name = "VitrineLuz_" + st.name + "_" + side;
                lampBulb.transform.SetParent(upgShowcase.transform, false);
                lampBulb.transform.position = st.transform.position + new Vector3(side * 1.15f, 2.38f, -0.78f);
                lampBulb.transform.localScale = Vector3.one * 0.28f;
                Object.DestroyImmediate(lampBulb.GetComponent<Collider>());
                if (glowM != null) lampBulb.GetComponent<Renderer>().sharedMaterial = glowM;
            }
            CreateWarmLight(upgShowcase.transform, "LuzVitrine_" + st.name, st.transform.position + new Vector3(0f, 2.1f, -1.0f), 1.3f, 6.5f);
        }
        upgShowcase.SetActive(false);

        GameObject upgMeetingCorner = new GameObject("Melhoria_CantinhoEncontro");
        upgMeetingCorner.transform.SetParent(fairBizRoot.transform, false);
        upgMeetingCorner.transform.position = new Vector3(1.6f, 0f, -11.6f);
        PlaceModel(upgMeetingCorner.transform, "CoastalBench", "Banco Cantinho Rosa", new Vector3(0f, 0f, 0.85f), 180f, 0.96f, cozyMats);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "ArcoPilar_" + side;
            pillar.transform.SetParent(upgMeetingCorner.transform, false);
            pillar.transform.localPosition = new Vector3(side * 1.45f, 1.15f, 0.85f);
            pillar.transform.localScale = new Vector3(0.18f, 1.15f, 0.18f);
            Object.DestroyImmediate(pillar.GetComponent<Collider>());
            if (creamM != null) pillar.GetComponent<Renderer>().sharedMaterial = creamM;

            GameObject flowerCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flowerCap.name = "ArcoFlor_" + side;
            flowerCap.transform.SetParent(upgMeetingCorner.transform, false);
            flowerCap.transform.localPosition = new Vector3(side * 1.45f, 2.32f, 0.85f);
            flowerCap.transform.localScale = new Vector3(0.46f, 0.34f, 0.46f);
            Object.DestroyImmediate(flowerCap.GetComponent<Collider>());
            if (pinkSoftM != null) flowerCap.GetComponent<Renderer>().sharedMaterial = pinkSoftM;
        }
        GameObject archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        archTop.name = "ArcoTopoRosa";
        archTop.transform.SetParent(upgMeetingCorner.transform, false);
        archTop.transform.localPosition = new Vector3(0f, 2.36f, 0.85f);
        archTop.transform.localScale = new Vector3(3.15f, 0.18f, 0.28f);
        Object.DestroyImmediate(archTop.GetComponent<Collider>());
        if (tealM != null) archTop.GetComponent<Renderer>().sharedMaterial = tealM;
        upgMeetingCorner.SetActive(false);

        GirlPowerOrla3DFairBusinesses fairBiz = fairBizRoot.AddComponent<GirlPowerOrla3DFairBusinesses>();
        fairBiz.player = playerRoot.transform;
        fairBiz.sceneCamera = mainCam;
        fairBiz.gameFlow = gameFlow;
        fairBiz.storyController = mission;
        fairBiz.lifeActivities = lifeAct;
        fairBiz.megaSystems = megaSys;
        fairBiz.audioController = audioCtrl;
        fairBiz.characterAnimator = charAnim;
        fairBiz.orangeTreePrimary = orangeTree1 != null ? orangeTree1.transform : null;
        fairBiz.orangeTreePrimaryFruitGroup = orangeFruit1;
        fairBiz.orangeTreeSecond = upgOrangeTree2 != null ? upgOrangeTree2.transform : null;
        fairBiz.orangeTreeSecondFruitGroup = orangeFruit2;
        fairBiz.mintPlanter = mintPot != null ? mintPot.transform : null;
        fairBiz.juiceStall = stallCoco != null ? stallCoco.transform : null;
        fairBiz.candyStall = stallDoces != null ? stallDoces.transform : null;
        fairBiz.craftStall = stallArt != null ? stallArt.transform : null;
        fairBiz.juiceCustomer = juiceCust != null ? juiceCust.transform : null;
        fairBiz.candyCustomer = candyCust != null ? candyCust.transform : null;
        fairBiz.craftCustomer = craftCust != null ? craftCust.transform : null;
        fairBiz.friendNina = friendNina != null ? friendNina.transform : null;
        fairBiz.friendBia = friendBia != null ? friendBia.transform : null;
        fairBiz.upgradePinkAwningsGroup = upgPinkAwnings;
        fairBiz.upgradeSecondOrangeTreeGroup = upgOrangeTree2;
        fairBiz.upgradeShowcaseLightsGroup = upgShowcase;
        fairBiz.upgradeMeetingCornerGroup = upgMeetingCorner;
        lifeAct.fairBusinesses = fairBiz;
        mission.fairBusinesses = fairBiz;

        // 13c. Single Unified Interaction Hub (one clear target marker + one large thumb action button!)
        GameObject hubObj = new GameObject("Coordenador_Interacao_Unica_Orla");
        hubObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DInteractionHub interactionHub = hubObj.AddComponent<GirlPowerOrla3DInteractionHub>();
        interactionHub.sceneCamera = mainCam;
        interactionHub.gameFlow = gameFlow;
        interactionHub.storyController = mission;
        interactionHub.dialogueController = dialogueCtrl;
        interactionHub.photoOverlay = photoCtrl;
        interactionHub.lifeActivities = lifeAct;
        interactionHub.megaSystems = megaSys;
        interactionHub.fairBusinesses = fairBiz;

        GameObject mobileTouchObj = new GameObject("Controle_Toque_Mobile_3Camadas");
        mobileTouchObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DMobileTouchController mobileTouch = mobileTouchObj.AddComponent<GirlPowerOrla3DMobileTouchController>();
        mobileTouch.sceneCamera = mainCam;
        mobileTouch.storyController = mission;
        mobileTouch.gameFlow = gameFlow;
        mobileTouch.dialogueController = dialogueCtrl;
        mobileTouch.photoOverlay = photoCtrl;
        mobileTouch.cardapioUI = cardapio;
        mobileTouch.megaSystems = megaSys;
        mobileTouch.fairBusinesses = fairBiz;
        mobileTouch.lifeActivities = lifeAct;
        mobileTouch.petFriendlySystem = petFriendlySys;
        mobileTouch.npcAI = npcAi;

        // 14. NavMeshAgent tuning only. Character movement remains owned by the player controller
        // and NPC waypoint/path controller; LateUpdate transform separation is disabled.
        List<Transform> avoidanceActors = new List<Transform>();
        int nextPriority = 20;
        AttachConfiguredNavMeshAgent(playerRoot, nextPriority);
        nextPriority += 3;
        foreach (GameObject coreNpc in new[] { friendNina, friendBia, florist, sisterMarina, pedBlue, pedCoral, camponesaObj })
        {
            if (coreNpc == null) continue;
            AttachConfiguredNavMeshAgent(coreNpc, nextPriority);
            avoidanceActors.Add(coreNpc.transform);
            nextPriority = Mathf.Min(96, nextPriority + 3);
        }
        for (int i = 0; i < strollingNpcs.Count; i++)
        {
            if (strollingNpcs[i] == null) continue;
            AttachConfiguredNavMeshAgent(strollingNpcs[i].gameObject, nextPriority);
            avoidanceActors.Add(strollingNpcs[i]);
            nextPriority = Mathf.Min(96, nextPriority + 3);
        }
        for (int i = 0; i < guestEntries.Count; i++)
        {
            if (guestEntries[i].actor == null) continue;
            AttachConfiguredNavMeshAgent(guestEntries[i].actor.gameObject, nextPriority);
            avoidanceActors.Add(guestEntries[i].actor);
            nextPriority = Mathf.Min(96, nextPriority + 3);
        }
        for (int i = 0; i < fairyList.Count; i++)
        {
            if (fairyList[i] == null) continue;
            AttachConfiguredNavMeshAgent(fairyList[i].gameObject, nextPriority);
            avoidanceActors.Add(fairyList[i]);
            nextPriority = Mathf.Min(96, nextPriority + 3);
        }

        GameObject avoidObj = new GameObject("Sistema_Colisao_e_Desvio_Personagens");
        avoidObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DCharacterAvoidance charAvoid = avoidObj.AddComponent<GirlPowerOrla3DCharacterAvoidance>();
        charAvoid.player = playerRoot.transform;
        charAvoid.npcActors = avoidanceActors.ToArray();
        charAvoid.blockingObstacles = obstacles.ToArray();
        charAvoid.characterRadius = 0.45f;
        charAvoid.applyTransformSeparation = false;

        // 15. PHYSICALLY COHERENT 3D DEPTH OCEAN & HORIZON CONTROLLER (Real 3D perspective — ZERO artificial sliding!)
        GameObject parallaxObj = new GameObject("Parallax_Horizonte_Mar_3Camadas");
        parallaxObj.transform.SetParent(root.transform, false);
        GirlPowerOrla3DParallaxHorizon parallax = parallaxObj.AddComponent<GirlPowerOrla3DParallaxHorizon>();
        parallax.sceneCamera = mainCam;
        parallax.oceanSurface = oceanSurface;
        parallax.farHorizonIslands = coastalDetails.parallaxFarIslands.ToArray();
        parallax.farHorizonClouds = coastalDetails.parallaxFarClouds.ToArray();
        parallax.midSeaSailboats = coastalDetails.parallaxMidSailboats.ToArray();
        parallax.shorelineFoamRibbons = coastalDetails.parallaxShoreFoam.ToArray();
        parallax.farLayerFollowFactor = 0.0f;
        parallax.midLayerFollowFactor = 0.0f;

        // Keep kiosk upgrade synced; stall, kiosk & florist signs are parented directly onto their solid cream plaques!
        if (kioskAwningUpgrade != null && kiosk != null) kioskAwningUpgrade.transform.position = kiosk.transform.position;

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
        camObj.transform.position = new Vector3(-1.6f, 8.6f, -14.7f);
        camObj.transform.rotation = Quaternion.Euler(39.5f, 0f, 0f);
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 44f;
        cam.nearClipPlane = 0.25f;
        cam.farClipPlane = 185f;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.72f, 0.90f);
        cam.depthTextureMode |= DepthTextureMode.Depth;
        camObj.AddComponent<AudioListener>();
        GirlPowerOrla3DCameraPost post = camObj.AddComponent<GirlPowerOrla3DCameraPost>();
        post.exposure = 0.94f;
        post.contrast = 1.07f;
        post.saturation = 1.08f;
        post.vignetteStrength = 0.06f;
        post.warmTint = new Color(1.00f, 1.00f, 1.00f, 1f);
        return cam;
    }

    private static Light CreateSunLight(Transform parent)
    {
        GameObject sunObj = new GameObject("Sol Fim de Tarde - Matinhos");
        sunObj.transform.SetParent(parent, false);
        // Elevated sun (52°) casts short, breezy soft shadows instead of long dark streaks across the walkway!
        sunObj.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        Light l = sunObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1.0f, 0.96f, 0.90f);
        l.intensity = 0.90f;
        l.shadows = LightShadows.Soft;
        l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        l.shadowStrength = 0.34f;
        l.shadowBias = 0.035f;
        l.shadowNormalBias = 0.16f;
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
        // Expanded 260m x 76m ocean plane keeping the shoreline at exact z = 8.15m & y = -0.18m right behind the dry sand!
        ocean.transform.position = new Vector3(12f, -0.18f, 46.15f);

        MeshFilter mf = ocean.AddComponent<MeshFilter>();
        MeshRenderer mr = ocean.AddComponent<MeshRenderer>();

        // Subdivided grid mesh for smooth vertex waves & horizon curvature
        Mesh mesh = new Mesh { name = "OrlaOceanGrid" };
        int segsX = 84;
        int segsZ = 34;
        float sizeX = 260f;
        float sizeZ = 76f;
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
        mesh.RecalculateTangents();
        // Generous vertical bounds so horizon curvature never triggers frustum culling!
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(sizeX, 40f, sizeZ));

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

    private static void CreateGardenFencePart(Transform parent, List<BoxCollider> obstacles, Material material, string name, Vector3 position, Vector3 size)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.position = position;
        part.transform.localScale = size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        BoxCollider collider = part.GetComponent<BoxCollider>();
        if (collider != null) obstacles.Add(collider);
    }

    private static void SetNamedChildPosition(Transform root, string objectName, Vector3 worldPosition)
    {
        Transform target = FindDescendant(root, objectName);
        if (target != null) target.position = worldPosition;
    }

    private static void SetNamedChildText(Transform root, string objectName, string value)
    {
        Transform target = FindDescendant(root, objectName);
        TextMeshPro label = target != null ? target.GetComponent<TextMeshPro>() : null;
        if (label != null) label.text = value;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), objectName);
            if (found != null) return found;
        }
        return null;
    }

    private static void AttachConfiguredNavMeshAgent(GameObject go, int avoidancePriority)
    {
        if (go == null) return;
        // Never attach NavMeshAgent onto a static NavMeshObstacle (prevents Unity Console conflict warning!)
        if (go.GetComponent<NavMeshObstacle>() != null) return;
        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        if (agent == null) agent = go.AddComponent<NavMeshAgent>();
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.radius = 0.45f;
        agent.height = 1.65f;
        agent.angularSpeed = 420f;
        agent.avoidancePriority = Mathf.Clamp(avoidancePriority, 1, 98);
        agent.updatePosition = false;
        agent.updateRotation = false;
        // Keep disabled by default since Orla das Fadas uses zero-bake OBB + A* Pathfinder (eliminates 18x "no valid NavMesh" errors!)
        agent.enabled = false;
    }

    private static void AddBlockingBox(GameObject go, Vector3 center, Vector3 size, List<BoxCollider> list)
    {
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = false;
        list.Add(box);

        // Add carving NavMeshObstacle on EVERY stall, planter, bush, bench, lamp post, tree & house!
        NavMeshObstacle nav = go.GetComponent<NavMeshObstacle>();
        if (nav == null)
        {
            nav = go.AddComponent<NavMeshObstacle>();
            nav.shape = NavMeshObstacleShape.Box;
            nav.center = center;
            nav.size = size;
            nav.carving = true;
        }
    }

    private static TextMeshPro Make3DText(Transform parent, string name, Vector3 localPos, string content, float fontSize, Color color, Vector2 rectSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = content;
        // Mobile Legibility (+70% font size + subtle dark outline for instant readability on phones!)
        tmp.fontSize = fontSize * 1.70f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.margin = Vector4.zero;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.rectTransform.sizeDelta = rectSize * 1.48f;
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
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.22f, 0.10f, 0.22f, 0.52f));
        if (mat.HasProperty("_CurveStrength")) mat.SetFloat("_CurveStrength", 0.0008f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material BuildCloudShadowMaterial()
    {
        Shader sh = Shader.Find("GirlPower/OrlaBlobShadow");
        if (sh == null) sh = Shader.Find("Unlit/Transparent");
        string matPath = MaterialsFolder + "/AC_CloudShadow.mat";
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
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.16f, 0.24f, 0.36f, 0.14f));
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
        if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", new Color(0.20f, 0.82f, 0.86f, 0.86f));
        if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", new Color(0.06f, 0.42f, 0.62f, 0.97f));
        if (mat.HasProperty("_DepthDistance")) mat.SetFloat("_DepthDistance", 2.8f);
        if (mat.HasProperty("_FoamDistance")) mat.SetFloat("_FoamDistance", 0.65f);
        if (mat.HasProperty("_FoamSpeed")) mat.SetFloat("_FoamSpeed", 0.42f);
        if (mat.HasProperty("_CurveStrength")) mat.SetFloat("_CurveStrength", 0.0008f);
        if (mat.HasProperty("_WaveHeight")) mat.SetFloat("_WaveHeight", 0.055f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void TintTableUmbrellaAccent(GameObject tableRoot, Material accentMat)
    {
        if (tableRoot == null || accentMat == null) return;
        foreach (MeshRenderer renderer in tableRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.gameObject.name.Contains("CanopyFade")) continue;
            Material[] mats = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string n = mats[i].name;
                // Replace only the non-cream accent stripe material on the table umbrella so each table has a distinct color/value!
                if (!n.Contains("Cream") && !n.Contains("Wood") && !n.Contains("Metal"))
                {
                    mats[i] = accentMat;
                    changed = true;
                }
            }
            if (changed) renderer.sharedMaterials = mats;
        }
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
            string objName = renderer.gameObject.name;
            bool isOverheadCoverMesh =
                objName.Contains("CanopyFade") ||
                objName.Contains("RoofFade") ||
                objName.Contains("Umb");

            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null) continue;
                if (source.shader == null || source.shader.name != CozyShaderName) continue;
                // When roofOnly is true, fade ONLY elevated canopy/umbrella/roof covers — counters & bases stay 100% solid!
                if (roofOnly && !isOverheadCoverMesh && !source.name.Contains("HouseRoof")) continue;

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
                if (fadeMaterial.HasProperty("_FadeStartHeight")) fadeMaterial.SetFloat("_FadeStartHeight", 1.70f);
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

        // Opening-Pink-Harmonized Coastal Palette:
        // Base: Warm Peach-Cream Sand (#F6E2C4) + Rosy-Cream Promenade & Plaza (#FAF0EE / #F5E5E0 / #E9D4CE)
        // Hero Accent: Strawberry Pink (#EE5690) & Pastel Blush (#F7B4CE / #E888A6) echoing the opening screen,
        // while the Protagonist wears Sunny Yellow (#FAD04E) + Turquoise Ribbon (#16C0CA) for instant silhouette pop!
        var palette = new (string name, string hex, float gloss, string emitHex, float emitStr)[]
        {
            ("AC_Sand", "#F6E2C4", 0.04f, null, 0f),          // Warm Peach-Golden Coastal Sand
            ("AC_SandWet", "#E2C6A6", 0.14f, null, 0f),
            ("AC_Grass", "#62A876", 0.04f, null, 0f),         // Vibrant Tropical Park Grass
            ("AC_GrassLight", "#80C08E", 0.05f, null, 0f),
            ("AC_Ocean", "#1D7C8F", 0.44f, null, 0f),         // Soft Petrol Blue sea
            ("AC_OceanShallow", "#35A8B4", 0.40f, null, 0f),
            ("AC_Foam", "#F8FBFE", 0.12f, "#E8F4FA", 0.10f),
            ("AC_HillDistant", "#9BB4CC", 0.03f, null, 0f),
            ("AC_HillFar", "#B8C8DF", 0.03f, null, 0f),

            ("AC_StoneWhite", "#FBF7F2", 0.05f, null, 0f),    // Clean Warm-Cream Boardwalk Stone (high contrast for characters!)
            ("AC_StoneWarm", "#F0E8DE", 0.05f, null, 0f),     // Soft Warm-Cream Plaza Stone
            ("AC_StoneDark", "#9C9296", 0.05f, null, 0f),
            ("AC_CobbleA", "#EAE0D6", 0.05f, null, 0f),       // Subtle Cream Cobblestone
            ("AC_CobbleB", "#DDD2C8", 0.05f, null, 0f),
            ("AC_PlazaTile", "#F7F1EA", 0.05f, null, 0f),     // Crisp Cream Plaza Tiles
            ("AC_PlazaAccent", "#F2D2DE", 0.05f, null, 0f),   // Subtle Pastel-Rose Plaza Accent
            ("AC_Asphalt", "#747686", 0.04f, null, 0f),
            ("AC_RoadLine", "#FAF0E8", 0.05f, null, 0f),

            ("AC_WoodWarm", "#BE9068", 0.05f, null, 0f),
            ("AC_WoodLight", "#D8B28C", 0.05f, null, 0f),
            ("AC_WoodDark", "#7A523E", 0.05f, null, 0f),
            ("AC_Cream", "#FCF2F0", 0.06f, null, 0f),
            ("AC_SailIvory", "#FAF1EC", 0.08f, "#FFF5F0", 0.14f),
            ("AC_PinkBerry", "#EE5690", 0.08f, null, 0f),     // Hero Strawberry Pink (Opening Palette Anchor!)
            ("AC_PinkSoft", "#F7B4CE", 0.07f, null, 0f),      // Cotton-Candy Pastel Pink
            ("AC_MintTeal", "#35A8B4", 0.07f, null, 0f),
            ("AC_MintSoft", "#9ED6C8", 0.06f, null, 0f),
            ("AC_PurplePlum", "#6C4C78", 0.06f, null, 0f),
            ("AC_Lavender", "#D4AEE8", 0.06f, null, 0f),
            ("AC_Coral", "#E48678", 0.07f, null, 0f),
            ("AC_SunnyYellow", "#F6D87C", 0.07f, null, 0f),
            ("AC_SkyBlue", "#6EBAD2", 0.07f, null, 0f),
            ("AC_Glass", "#D2EFF7", 0.42f, "#BCE6F4", 0.12f),
            ("AC_MetalDark", "#3E3C4C", 0.12f, null, 0f),
            ("AC_MetalSilver", "#DCE0EA", 0.34f, null, 0f),
            ("AC_Gold", "#ECC678", 0.30f, null, 0f),

            ("AC_LanternGlow", "#FFE6A8", 0.10f, "#FFD278", 1.10f),
            ("AC_BulbWarm", "#FFF0BE", 0.10f, "#FFE090", 1.00f),

            ("AC_PalmTrunk", "#C89E72", 0.05f, null, 0f),
            ("AC_PalmRing", "#BE946A", 0.05f, null, 0f),
            ("AC_PalmLeaf", "#5B9E78", 0.08f, null, 0f),
            ("AC_PalmLeafTip", "#7BB892", 0.08f, null, 0f),
            ("AC_Coconut", "#764E34", 0.08f, null, 0f),
            ("AC_CoconutGreen", "#6BA880", 0.10f, null, 0f),
            ("AC_BushGreen", "#4D9E65", 0.06f, null, 0f),
            ("AC_BushLight", "#7AC286", 0.06f, null, 0f),
            ("AC_Terracotta", "#D28472", 0.06f, null, 0f),
            ("AC_Orange", "#EC9C56", 0.12f, null, 0f),
            ("AC_Strawberry", "#F06EA6", 0.14f, null, 0f),
            ("AC_Chocolate", "#6E4632", 0.10f, null, 0f),
            ("AC_Vanilla", "#FBF0DA", 0.10f, null, 0f),
            ("AC_Waffle", "#DFAA68", 0.08f, null, 0f),
            ("AC_FlowerPink", "#F26CA4", 0.10f, null, 0f),
            ("AC_FlowerYellow", "#F2D486", 0.10f, null, 0f),
            ("AC_FlowerBlue", "#6CC4D8", 0.10f, null, 0f),
            ("AC_FlowerWhite", "#FDF8F4", 0.10f, null, 0f),
            ("AC_Starfish", "#E68E86", 0.10f, null, 0f),
            ("AC_Shell", "#FAE8E2", 0.14f, null, 0f),

            ("AC_SkinWarm", "#C98862", 0.06f, null, 0f),
            ("AC_SkinLight", "#F7CAA8", 0.06f, null, 0f),
            ("AC_SkinTan", "#E2A67C", 0.06f, null, 0f),
            ("AC_CheekBlush", "#F586A8", 0.06f, null, 0f),
            ("AC_EyeDark", "#201824", 0.52f, null, 0f),
            ("AC_EyeShine", "#FFFFFF", 0.40f, "#FFFFFF", 0.65f),
            ("AC_HairPlum", "#8A4A98", 0.22f, null, 0f),
            ("AC_HairBrown", "#664028", 0.22f, null, 0f),
            ("AC_HairDark", "#2C2534", 0.22f, null, 0f),
            ("AC_HairAuburn", "#B8563C", 0.22f, null, 0f),
            ("AC_Denim", "#3E6F8E", 0.04f, null, 0f),
            ("AC_StrawHat", "#EED29E", 0.05f, null, 0f),
            ("AC_ShoeDark", "#483246", 0.09f, null, 0f),
            ("AC_HouseWall", "#FFF1EE", 0.05f, null, 0f),     // Creamy Blush Walls
            ("AC_HouseRoof", "#E888A6", 0.07f, null, 0f),     // Cozy Strawberry-Rose Roofs harmonizing with opening!
            ("AC_ProtagHero", "#FAD04E", 0.05f, null, 0f),    // Exclusive Hero Sunny Yellow (pops sharply against pink scene & cart!)
            ("AC_ProtagRibbon", "#16C0CA", 0.10f, null, 0f),  // Exclusive Hero Turquoise Bow & Backpack!
            ("AC_ProtagShirt", "#FAD04E", 0.05f, null, 0f),
            // GPU Vertex-Animated Breeze Hair Materials (upgraded to OrlaACNHHairWind shader below!)
            ("AC_HairWind_Brown", "#664028", 0.22f, null, 0f),
            ("AC_HairWind_Blonde", "#ECC66E", 0.22f, null, 0f),
            ("AC_HairWind_Dark", "#2C2534", 0.22f, null, 0f),
            ("AC_HairWind_Silver", "#D6DAE2", 0.22f, null, 0f),
            ("AC_HairWind_Auburn", "#B8563C", 0.22f, null, 0f),
        };

        var characterOutlineMats = new HashSet<string>
        {
            "AC_SkinWarm", "AC_SkinLight", "AC_SkinTan", "AC_CheekBlush",
            "AC_HairPlum", "AC_HairBrown", "AC_HairDark", "AC_HairAuburn",
            "AC_HairWind_Brown", "AC_HairWind_Blonde", "AC_HairWind_Dark", "AC_HairWind_Silver", "AC_HairWind_Auburn",
            "AC_ProtagHero", "AC_ProtagRibbon", "AC_ProtagShirt",
            "AC_Denim", "AC_StrawHat", "AC_ShoeDark",
            "AC_PinkSoft", "AC_Lavender", "AC_Coral", "AC_SkyBlue", "AC_MintSoft", "AC_Terracotta"
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

            bool isCharMat = characterOutlineMats.Contains(item.name);
            bool isHeroMat = item.name.StartsWith("AC_Protag");

            if (ColorUtility.TryParseHtmlString(item.hex, out Color baseCol))
                mat.SetColor("_Color", baseCol);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", item.gloss);
            if (mat.HasProperty("_ShadowColor"))
                mat.SetColor("_ShadowColor", new Color(0.46f, 0.34f, 0.52f, 1f));
            if (mat.HasProperty("_RimColor"))
                mat.SetColor("_RimColor", new Color(1.0f, 0.90f, 0.92f, 1f));
            if (mat.HasProperty("_RimPower"))
                mat.SetFloat("_RimPower", isCharMat ? 3.2f : 4.2f);
            if (mat.HasProperty("_RimStrength"))
                mat.SetFloat("_RimStrength", isHeroMat ? 0.22f : (isCharMat ? 0.14f : 0.05f));
            if (mat.HasProperty("_OutlineSoft"))
                mat.SetFloat("_OutlineSoft", isHeroMat ? 0.84f : (isCharMat ? 0.68f : 0.0f));
            if (mat.HasProperty("_WrapAmount"))
                mat.SetFloat("_WrapAmount", 0.28f);
            if (mat.HasProperty("_GroundAO"))
                mat.SetFloat("_GroundAO", isCharMat ? 0.18f : 0.34f);
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
        float texStrength = 0.24f;
        switch (matName)
        {
            case "AC_StoneWhite":
            case "AC_StoneWarm":
            case "AC_PlazaTile":
            case "AC_PlazaAccent":
                texFile = "Tex_WalkwayTiles.png";
                tileScale = 0.26f;
                texStrength = 0.18f; // Subtle tactile stone variation without dark vertical tile grids!
                break;
            case "AC_Sand":
            case "AC_SandWet":
                texFile = "Tex_BeachSand.png";
                tileScale = 0.22f;
                texStrength = 0.14f; // Smooth organic cream sand with zero tiling bands!
                break;
            case "AC_Grass":
            case "AC_GrassLight":
                texFile = "Tex_CoastalGrass.png";
                tileScale = 0.28f;
                texStrength = 0.28f;
                break;
            case "AC_WoodLight":
            case "AC_WoodWarm":
            case "AC_WoodDark":
                texFile = "Tex_WoodPlanks.png";
                tileScale = 0.55f;
                texStrength = 0.38f;
                break;
            case "AC_CobbleA":
            case "AC_CobbleB":
                texFile = "Tex_CobbleFeira.png";
                tileScale = 0.42f;
                texStrength = 0.26f;
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
                if (mat.HasProperty("_TexStrength")) mat.SetFloat("_TexStrength", texStrength);
                return;
            }
        }
        mat.SetTexture("_MainTex", null);
        if (mat.HasProperty("_UseWorldUV")) mat.SetFloat("_UseWorldUV", 0f);
        if (mat.HasProperty("_TexStrength")) mat.SetFloat("_TexStrength", 0f);
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
