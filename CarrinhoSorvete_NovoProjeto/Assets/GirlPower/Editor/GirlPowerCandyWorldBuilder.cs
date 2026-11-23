using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the original "Girl power 1- Sorvete.unity" gameplay scene from the
/// preserved Reference scene — keeping ALL original driving, shooting, delivery,
/// mission, enemy, and audio gameplay intact — while completely replacing the
/// blurry ground and opaque-box houses with a cohesive, vibrant Candy World!
/// </summary>
public static class GirlPowerCandyWorldBuilder
{
    private const string ReferenceScenePath = "Assets/Scenes/Girl power 1 - Sorvete - Referencia.unity";
    private const string WorkingScenePath = "Assets/Scenes/Girl power 1- Sorvete.unity";
    private const string CandyArtFolder = "Assets/GirlPower/CandyWorld/Art";
    private const string CandyRootName = "CandyWorld_CenarioBonito";

    [MenuItem("Girl Power/Rebuild Candy World (Cena Original Melhorada)")]
    public static void BuildCandyWorld()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Girl power 1 - Sorvete - Referencia.unity")))
        {
            Debug.LogError("Cena de referência não encontrada em " + ReferenceScenePath);
            return;
        }

        // 1. Generate & import all crisp Candy World art + clean transparent house cutouts
        CandyWorldArtGenerator.GenerateAndImportAll(CandyArtFolder);

        // 2. Copy the clean Reference scene over the working scene so original gameplay
        //    (carro, DriverPro, Delivery, Canvas, Audio, ControladorJogo) is 100% restored!
        AssetDatabase.CopyAsset(ReferenceScenePath, WorkingScenePath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(WorkingScenePath, OpenSceneMode.Single);

        // 3. Preserve core gameplay roots; disable/remove old ugly scenery clutter
        GameObject carRoot = null;
        GameObject mainCamObj = null;

        List<GameObject> toDestroy = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            string n = root.name;
            if (n == "carro" || root.GetComponentInChildren<DriverPro>(true) != null)
            {
                carRoot = root;
                root.SetActive(true);
                continue;
            }
            if (n == "Main Camera" || root.CompareTag("MainCamera"))
            {
                mainCamObj = root;
                root.SetActive(true);
                continue;
            }
            if (n == "Canvas" || n == "EventSystem" || n == "ControladorJogo" ||
                n.Contains("Audio") || n.Contains("Musica") || n.Contains("Som") ||
                root.GetComponent<ControladorJogo>() != null || root.GetComponent<AudioSource>() != null)
            {
                root.SetActive(true);
                continue;
            }
            // Everything else in the reference scene is the old messy scenery, blurry roads,
            // opaque JPEG houses, stray fences/trees, old pickups, and old Virtual Camera!
            toDestroy.Add(root);
        }

        foreach (GameObject old in toDestroy)
            Object.DestroyImmediate(old);

        // 4. Clean up Main Camera children (old trees/fences were accidentally parented to Main Camera!)
        if (mainCamObj != null)
        {
            List<GameObject> camJunk = new List<GameObject>();
            foreach (Transform child in mainCamObj.transform)
            {
                if (child.name == "Canvas" || child.GetComponent<Canvas>() != null || child.GetComponent<AudioSource>() != null)
                {
                    // Reparent Canvas out of camera so it stays clean at scene root
                    if (child.GetComponent<Canvas>() != null) child.SetParent(null, true);
                    continue;
                }
                camJunk.Add(child.gameObject);
            }
            foreach (GameObject j in camJunk) Object.DestroyImmediate(j);

            Camera cam = mainCamObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 7.4f;
                cam.backgroundColor = new Color32(158, 226, 178, 255); // Matches Candy Meadow!
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            mainCamObj.transform.position = new Vector3(0f, -9.5f, -10f);
            if (mainCamObj.GetComponent<GameCameraController>() == null)
                mainCamObj.AddComponent<GameCameraController>();
        }

        // 5. Fix & ground the Player's Ice Cream Van ("carro") cleanly on the South Candy Boulevard!
        if (carRoot != null)
        {
            carRoot.transform.position = new Vector3(0f, -9.5f, 0f);
            carRoot.transform.rotation = Quaternion.identity;
            carRoot.transform.localScale = Vector3.one;
            TrySetTag(carRoot, "Player");

            SpriteRenderer rootSr = carRoot.GetComponent<SpriteRenderer>();
            if (rootSr != null) rootSr.sortingOrder = 12;

            // Align child "Carro1" and avoid duplicate stacked sprite/collider bugs
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

            // Ensure a clean, smooth collider on the van so it never snags on start
            BoxCollider2D vanCol = carRoot.GetComponent<BoxCollider2D>();
            if (vanCol != null)
            {
                vanCol.offset = Vector2.zero;
                vanCol.size = new Vector2(0.85f, 1.75f);
                vanCol.isTrigger = false;
            }

            // Add soft drop shadow beneath the ice cream van!
            Sprite shadowSprite = LoadCandy("CandyShadow.png");
            if (shadowSprite != null)
            {
                GameObject vanShadow = new GameObject("SombraDoCarrinho");
                vanShadow.transform.SetParent(carRoot.transform, false);
                vanShadow.transform.localPosition = new Vector3(0.08f, -0.14f, 0f);
                vanShadow.transform.localScale = new Vector3(1.1f, 1.55f, 1f);
                SpriteRenderer shSr = vanShadow.AddComponent<SpriteRenderer>();
                shSr.sprite = shadowSprite;
                shSr.sortingOrder = 8;
            }
        }

        // Ensure MissionManager exists so HUD immediately shows a real active mission!
        if (Object.FindObjectOfType<MissionManager>() == null)
        {
            GameObject mm = new GameObject("MissionManager");
            mm.AddComponent<MissionManager>();
        }

        // 6. Build the brand-new vibrant Candy World scenery!
        GameObject world = new GameObject(CandyRootName);
        List<Transform> swaying = new List<Transform>();
        List<SpriteRenderer> glows = new List<SpriteRenderer>();
        List<SpriteRenderer> sparkles = new List<SpriteRenderer>();
        List<Transform> pickups = new List<Transform>();

        BuildGroundAndRoads(world.transform);
        BuildDeliveryPlazas(world.transform, glows);
        BuildCupcakeHouses(world.transform);
        BuildPondsAndParks(world.transform);
        BuildCandyPropsAndForest(world.transform, swaying, glows, sparkles);
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
        Debug.Log("Girl Power: Mundo dos Doces reconstruído em 'Girl power 1- Sorvete.unity' com prado de menta, ruas de biscoito, praças de entrega, 8 casas cupcake recortadas sem fundo bege, lagoa de soda, pirulitos e pickups organizados!");
    }

    private static void BuildGroundAndRoads(Transform root)
    {
        Transform groundParent = CreateGroup(root, "01_Chao_e_Ruas");

        // Lush seamless pastel mint candy meadow spanning far beyond playable bounds
        CreateTiled(groundParent, "PradoDeMenta", LoadCandy("CandyMeadowTile.png"), Vector2.zero, new Vector2(112f, 88f), -30);

        Sprite roadH = LoadCandy("CandyRoadH.png");
        Sprite roadV = LoadCandy("CandyRoadV.png");
        Sprite roadCross = LoadCandy("CandyRoadCross.png");
        Sprite roadCorner = LoadCandy("CandyRoadCorner.png");

        const float W = 5.8f; // Spacious drivable road width!

        // Horizontal boulevards: y = 0 (Main), y = +18 (North), y = -18 (South)
        CreateTiled(groundParent, "RuaCentral_Oeste", roadH, new Vector2(-12f, 0f), new Vector2(18.6f, W), -15);
        CreateTiled(groundParent, "RuaCentral_Leste", roadH, new Vector2(12f, 0f), new Vector2(18.6f, W), -15);
        CreateTiled(groundParent, "RuaNorte_Oeste", roadH, new Vector2(-12f, 18f), new Vector2(18.6f, W), -15);
        CreateTiled(groundParent, "RuaNorte_Leste", roadH, new Vector2(12f, 18f), new Vector2(18.6f, W), -15);
        CreateTiled(groundParent, "RuaSul_Oeste", roadH, new Vector2(-12f, -18f), new Vector2(18.6f, W), -15);
        CreateTiled(groundParent, "RuaSul_Leste", roadH, new Vector2(12f, -18f), new Vector2(18.6f, W), -15);

        // Vertical avenues: x = 0 (Main), x = -24 (West), x = +24 (East)
        CreateTiled(groundParent, "AvCentral_Sul", roadV, new Vector2(0f, -9f), new Vector2(W, 12.6f), -15);
        CreateTiled(groundParent, "AvCentral_Norte", roadV, new Vector2(0f, 9f), new Vector2(W, 12.6f), -15);
        CreateTiled(groundParent, "AvOeste_Sul", roadV, new Vector2(-24f, -9f), new Vector2(W, 12.6f), -15);
        CreateTiled(groundParent, "AvOeste_Norte", roadV, new Vector2(-24f, 9f), new Vector2(W, 12.6f), -15);
        CreateTiled(groundParent, "AvLeste_Sul", roadV, new Vector2(24f, -9f), new Vector2(W, 12.6f), -15);
        CreateTiled(groundParent, "AvLeste_Norte", roadV, new Vector2(24f, 9f), new Vector2(W, 12.6f), -15);

        // 5 Crisp Crossroad Intersections
        Vector2[] crosses = { new Vector2(0, 0), new Vector2(0, 18), new Vector2(0, -18), new Vector2(-24, 0), new Vector2(24, 0) };
        for (int i = 0; i < crosses.Length; i++)
            CreateSprite(groundParent, "Cruzamento_" + (i + 1), roadCross, crosses[i], Vector2.one, -12, 0f);

        // 4 Rounded Boulevard Corners (CandyRoadCorner natively connects East + North!)
        CreateSprite(groundParent, "Curva_SO", roadCorner, new Vector2(-24f, -18f), Vector2.one, -12, 0f);
        CreateSprite(groundParent, "Curva_SE", roadCorner, new Vector2(24f, -18f), Vector2.one, -12, 90f);
        CreateSprite(groundParent, "Curva_NE", roadCorner, new Vector2(24f, 18f), Vector2.one, -12, 180f);
        CreateSprite(groundParent, "Curva_NO", roadCorner, new Vector2(-24f, 18f), Vector2.one, -12, 270f);
    }

    private static void BuildDeliveryPlazas(Transform root, List<SpriteRenderer> glows)
    {
        Transform plazas = CreateGroup(root, "02_Pracas_de_Entrega");
        Sprite plazaSprite = LoadCandy("CandyDeliveryPlaza.png");
        Sprite glowSprite = LoadCandy("CandyGlow.png");

        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Central", plazaSprite, glowSprite, new Vector2(0f, 0f), glows);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça Arco-Íris", plazaSprite, glowSprite, new Vector2(24f, 0f), glows);
        CreateDeliveryPlaza(plazas, "Descarga de sorvete - Praça da Lagoa", plazaSprite, glowSprite, new Vector2(-24f, 0f), glows);
    }

    private static void CreateDeliveryPlaza(Transform parent, string name, Sprite plazaSprite, Sprite glowSprite, Vector2 pos, List<SpriteRenderer> glows)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one * 0.92f;
        TrySetTag(go, "Customer");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = plazaSprite;
        sr.sortingOrder = -5;

        CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 3.2f;

        // Soft pulsing strawberry-gold welcome glow
        SpriteRenderer g = CreateSprite(go.transform, "BrilhoPraca", glowSprite, Vector2.zero, Vector2.one * 3.4f, -4, 0f);
        g.color = new Color(1f, 0.72f, 0.88f, 0.36f);
        glows.Add(g);
    }

    private static void BuildCupcakeHouses(Transform root)
    {
        Transform houses = CreateGroup(root, "03_Casas_Cupcake");

        // All 8 clean transparent cutout houses arranged across the 4 cozy Candy Districts!
        PlaceHouse(houses, "Casa_Morango", "CandyHouse_Strawberry.png", new Vector2(-8.2f, 6.8f), 0.78f);
        PlaceHouse(houses, "Casa_ArcoIris", "CandyHouse_Rainbow.png", new Vector2(8.2f, 6.8f), 0.80f);
        PlaceHouse(houses, "Casa_FogueteAzul", "CandyHouse_RocketBlue.png", new Vector2(-8.6f, -7.2f), 0.78f);
        PlaceHouse(houses, "Casa_ChocoMenta", "CandyHouse_ChocoMint.png", new Vector2(8.6f, -7.2f), 0.80f);

        PlaceHouse(houses, "Casa_Galaxia", "CandyHouse_Galaxy.png", new Vector2(-17.0f, -10.8f), 0.80f);
        PlaceHouse(houses, "Casa_Nautica", "CandyHouse_Nautical.png", new Vector2(17.0f, -10.8f), 0.80f);
        PlaceHouse(houses, "Casa_EstufaBotanica", "CandyHouse_Botanical.png", new Vector2(-16.5f, 11.4f), 0.82f);
        PlaceHouse(houses, "Casa_Matcha", "CandyHouse_Matcha.png", new Vector2(16.8f, 11.2f), 0.80f);

        // Extra charming houses lining the outer avenues
        PlaceHouse(houses, "Casa_Morango_Norte", "CandyHouse_Strawberry.png", new Vector2(-10.5f, 23.6f), 0.76f);
        PlaceHouse(houses, "Casa_ArcoIris_Norte", "CandyHouse_Rainbow.png", new Vector2(10.5f, 23.6f), 0.76f);
        PlaceHouse(houses, "Casa_Galaxia_Leste", "CandyHouse_Galaxy.png", new Vector2(30.4f, 8.6f), 0.76f);
        PlaceHouse(houses, "Casa_Foguete_Oeste", "CandyHouse_RocketBlue.png", new Vector2(-30.4f, -8.2f), 0.76f);
    }

    private static void PlaceHouse(Transform parent, string name, string spriteFile, Vector2 pos, float scale)
    {
        Sprite sp = LoadCandy(spriteFile);
        if (sp == null) return;
        SpriteRenderer sr = CreateSprite(parent, name, sp, pos, Vector2.one * scale, 2, 0f);
        BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4.2f, 3.4f);
        col.offset = new Vector2(0f, -0.45f);
    }

    private static void BuildPondsAndParks(Transform root)
    {
        Transform parks = CreateGroup(root, "04_Lagoas_de_Soda");
        Sprite pond = LoadCandy("CandyBerryPond.png");

        PlacePond(parks, "LagoaSoda_NO", pond, new Vector2(-15.8f, 5.8f), 0.66f);
        PlacePond(parks, "LagoaSoda_SE", pond, new Vector2(15.6f, -5.8f), 0.62f);
    }

    private static void PlacePond(Transform parent, string name, Sprite pond, Vector2 pos, float scale)
    {
        SpriteRenderer sr = CreateSprite(parent, name, pond, pos, Vector2.one * scale, -8, 0f);
        CapsuleCollider2D col = sr.gameObject.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = new Vector2(8.6f, 4.8f);
    }

    private static void BuildCandyPropsAndForest(
        Transform root,
        List<Transform> swaying,
        List<SpriteRenderer> glows,
        List<SpriteRenderer> sparkles)
    {
        Transform props = CreateGroup(root, "05_Arvores_Pirulitos_e_Luzes");

        // User's adorable existing transparent candy trees & wafer fences!
        Sprite treePink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/download__2_-removebg-preview.png");
        Sprite treePine = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (67).png");
        Sprite waferFence = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (63).png");

        // Our new glossy cartoon Candy World props!
        Sprite lolliA = LoadCandy("GiantLollipopA.png");
        Sprite lolliB = LoadCandy("GiantLollipopB.png");
        Sprite cane = LoadCandy("CandyCanePost.png");
        Sprite lamp = LoadCandy("CandyLampPost.png");
        Sprite flowerBed = LoadCandy("SugarFlowerBed.png");
        Sprite marsh = LoadCandy("MarshmallowCluster.png");
        Sprite glow = LoadCandy("CandyGlow.png");
        Sprite sparkle = LoadCandy("CandySparkle.png");

        // Iconic Giant Swirled Lollipops framing intersections & plazas!
        Vector2[] lolliPositions =
        {
            new Vector2(-4.4f, 4.5f), new Vector2(4.4f, 4.5f),
            new Vector2(-4.4f, -4.5f), new Vector2(4.4f, -4.5f),
            new Vector2(-20.0f, 4.5f), new Vector2(20.0f, 4.5f),
            new Vector2(-4.4f, 13.8f), new Vector2(4.4f, 13.8f),
            new Vector2(-4.4f, -13.8f), new Vector2(4.4f, -13.8f)
        };
        for (int i = 0; i < lolliPositions.Length; i++)
        {
            SpriteRenderer sr = CreateSprite(props, "PirulitoGigante_" + (i + 1), i % 2 == 0 ? lolliA : lolliB, lolliPositions[i], Vector2.one * 0.62f, 4, 0f);
            CircleCollider2D c = sr.gameObject.AddComponent<CircleCollider2D>();
            c.radius = 0.7f;
            c.offset = new Vector2(0f, -1.0f);
            swaying.Add(sr.transform);
        }

        // Giant Candy Canes lining the central & outer gardens
        Vector2[] canePositions =
        {
            new Vector2(-12.2f, 4.3f), new Vector2(12.2f, 4.3f),
            new Vector2(-12.2f, -4.3f), new Vector2(12.2f, -4.3f),
            new Vector2(-19.8f, -4.5f), new Vector2(19.8f, -4.5f),
            new Vector2(19.8f, 13.6f), new Vector2(-19.8f, 13.6f)
        };
        for (int i = 0; i < canePositions.Length; i++)
        {
            SpriteRenderer sr = CreateSprite(props, "BengalaDoce_" + (i + 1), cane, canePositions[i], Vector2.one * 0.60f, 3, 0f);
            sr.flipX = (i % 2 == 1);
            swaying.Add(sr.transform);
        }

        // Glowing Candy Streetlamps along the avenues
        Vector2[] lampPositions =
        {
            new Vector2(-4.2f, 9.2f), new Vector2(4.2f, 9.2f),
            new Vector2(-4.2f, -9.2f), new Vector2(4.2f, -9.2f),
            new Vector2(-14.5f, 3.9f), new Vector2(14.5f, 3.9f),
            new Vector2(-14.5f, -3.9f), new Vector2(14.5f, -3.9f),
            new Vector2(-20.2f, 9.2f), new Vector2(20.2f, 9.2f)
        };
        for (int i = 0; i < lampPositions.Length; i++)
        {
            CreateSprite(props, "PosteDoce_" + (i + 1), lamp, lampPositions[i], Vector2.one * 0.56f, 5, 0f);
            SpriteRenderer g = CreateSprite(props, "LuzPoste_" + (i + 1), glow, lampPositions[i] + new Vector2(0f, 0.85f), Vector2.one * 1.45f, 6, 0f);
            g.color = new Color(1f, 0.90f, 0.62f, 0.42f);
            glows.Add(g);
        }

        // Lush Sugar Flowerbeds & Pillow Marshmallow clusters decorating yards!
        Vector2[] flowerPositions =
        {
            new Vector2(-5.4f, 11.4f), new Vector2(5.4f, 11.4f),
            new Vector2(-5.4f, -11.4f), new Vector2(5.4f, -11.4f),
            new Vector2(-11.4f, 11.8f), new Vector2(11.4f, 11.8f),
            new Vector2(-11.8f, -11.8f), new Vector2(11.8f, -11.8f)
        };
        for (int i = 0; i < flowerPositions.Length; i++)
            CreateSprite(props, "CanteiroAcucar_" + (i + 1), flowerBed, flowerPositions[i], Vector2.one * 0.72f, 1, 0f);

        Vector2[] marshPositions =
        {
            new Vector2(-11.8f, 6.2f), new Vector2(12.4f, 7.6f),
            new Vector2(-12.6f, -6.4f), new Vector2(11.8f, -6.4f),
            new Vector2(-20.4f, 7.8f), new Vector2(20.4f, -7.8f)
        };
        for (int i = 0; i < marshPositions.Length; i++)
            CreateSprite(props, "Marshmallows_" + (i + 1), marsh, marshPositions[i], Vector2.one * 0.72f, 1, 0f);

        // User's adorable Pink Cotton-Candy Trees & Pink-Blue Candy Pines in scenic groves!
        Vector2[] treePositions =
        {
            new Vector2(-12.5f, 13.4f), new Vector2(-8.2f, 13.6f), new Vector2(12.5f, 13.4f), new Vector2(8.2f, 13.6f),
            new Vector2(-12.8f, -13.4f), new Vector2(-8.2f, -13.6f), new Vector2(12.8f, -13.4f), new Vector2(8.2f, -13.6f),
            new Vector2(-19.5f, -6.6f), new Vector2(19.5f, 6.6f),
            new Vector2(-30.0f, 14.0f), new Vector2(-30.5f, 4.0f), new Vector2(-30.0f, -14.5f),
            new Vector2(30.0f, 14.5f), new Vector2(30.5f, -4.0f), new Vector2(30.0f, -14.5f),
            new Vector2(-18.5f, 23.5f), new Vector2(0f, 23.8f), new Vector2(18.5f, 23.5f),
            new Vector2(-18.5f, -23.5f), new Vector2(-6.5f, -23.5f), new Vector2(6.5f, -23.5f), new Vector2(18.5f, -23.5f)
        };
        for (int i = 0; i < treePositions.Length; i++)
        {
            Sprite ts = (i % 2 == 0) ? treePink : treePine;
            if (ts == null) continue;
            float sc = (i % 2 == 0) ? 0.50f : 0.42f;
            SpriteRenderer sr = CreateSprite(props, "ArvoreDoce_" + (i + 1), ts, treePositions[i], Vector2.one * sc, 3, 0f);
            CircleCollider2D col = sr.gameObject.AddComponent<CircleCollider2D>();
            col.radius = 1.15f;
            col.offset = new Vector2(0f, -0.6f);
            swaying.Add(sr.transform);
        }

        // User's cute Chocolate Wafer Fences framing house yards neatly!
        if (waferFence != null)
        {
            Vector2[] fencePositions =
            {
                new Vector2(-10.5f, -11.2f), new Vector2(-7.5f, -11.2f),
                new Vector2(7.5f, -11.2f), new Vector2(10.5f, -11.2f),
                new Vector2(-10.5f, 10.6f), new Vector2(10.5f, 10.6f)
            };
            for (int i = 0; i < fencePositions.Length; i++)
            {
                SpriteRenderer sr = CreateSprite(props, "CercaWafer_" + (i + 1), waferFence, fencePositions[i], Vector2.one * 0.36f, 2, 0f);
                BoxCollider2D bc = sr.gameObject.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(6.2f, 2.2f);
            }
        }

        // Floating pastel sugar-star sparkles scattered across the Candy World!
        for (int i = 0; i < 24; i++)
        {
            float sx = -26f + (i * 11.3f % 52f);
            float sy = -19f + (i * 7.9f % 38f);
            SpriteRenderer sp = CreateSprite(props, "BrilhoEstrela_" + (i + 1), sparkle, new Vector2(sx, sy), Vector2.one * (0.45f + (i % 3) * 0.14f), 9, 0f);
            sp.color = (i % 2 == 0) ? new Color(1f, 0.85f, 0.96f, 0.7f) : new Color(1f, 0.96f, 0.68f, 0.7f);
            sparkles.Add(sp);
        }
    }

    private static void BuildPickupsAndBoosts(Transform root, List<Transform> pickups)
    {
        Transform items = CreateGroup(root, "06_Sorvetes_e_Boosts");
        Sprite donutSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/a casa de mosntros/background-removed (77).png");
        Sprite boostSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameDev.tv - Delivery Dash Assets - cópia/Props & Stuff/pickupspeed.png");
        Sprite glowSprite = LoadCandy("CandyGlow.png");

        // 16 inviting Donut/Ice Cream pickups cleanly centered on the Candy Boulevards!
        Vector2[] packagePositions =
        {
            new Vector2(-8f, 0f), new Vector2(-16f, 0f),
            new Vector2(8f, 0f), new Vector2(16f, 0f),
            new Vector2(0f, 8f), new Vector2(0f, 14f),
            new Vector2(0f, -15f),
            new Vector2(-12f, 18f), new Vector2(12f, 18f),
            new Vector2(-12f, -18f), new Vector2(12f, -18f),
            new Vector2(-24f, 10f), new Vector2(-24f, -10f),
            new Vector2(24f, 10f), new Vector2(24f, -10f),
            new Vector2(18f, 18f)
        };
        for (int i = 0; i < packagePositions.Length; i++)
        {
            GameObject go = new GameObject("pickupitem (" + (i + 1) + ")");
            go.transform.SetParent(items, false);
            go.transform.position = new Vector3(packagePositions[i].x, packagePositions[i].y, 0f);
            go.transform.localScale = Vector3.one * 0.28f;
            TrySetTag(go, "package");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = donutSprite;
            sr.sortingOrder = 10;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 2.4f;

            if (glowSprite != null)
            {
                SpriteRenderer g = CreateSprite(go.transform, "BrilhoItem", glowSprite, Vector2.zero, Vector2.one * 3.2f, 9, 0f);
                g.color = new Color(1f, 0.72f, 0.90f, 0.45f);
            }
            pickups.Add(go.transform);
        }

        // 4 Speed Boost pads placed on long straightaways
        Vector2[] boostPositions =
        {
            new Vector2(-12f, 0f), new Vector2(12f, 0f),
            new Vector2(0f, 18f), new Vector2(0f, -18f)
        };
        for (int i = 0; i < boostPositions.Length; i++)
        {
            GameObject go = new GameObject("Boost item (" + (i + 1) + ")");
            go.transform.SetParent(items, false);
            go.transform.position = new Vector3(boostPositions[i].x, boostPositions[i].y, 0f);
            go.transform.localScale = Vector3.one * 0.85f;
            TrySetTag(go, "Boost item");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = boostSprite;
            sr.sortingOrder = 6;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
        }
    }

    private static void BuildPerimeterBoundary(Transform root)
    {
        Transform bounds = CreateGroup(root, "07_Limites_do_Mapa");
        CreateBoundaryWall(bounds, "Limite_Norte", new Vector2(0f, 26.5f), new Vector2(76f, 2f));
        CreateBoundaryWall(bounds, "Limite_Sul", new Vector2(0f, -26.5f), new Vector2(76f, 2f));
        CreateBoundaryWall(bounds, "Limite_Oeste", new Vector2(-34.5f, 0f), new Vector2(2f, 56f));
        CreateBoundaryWall(bounds, "Limite_Leste", new Vector2(34.5f, 0f), new Vector2(2f, 56f));
    }

    private static void CreateBoundaryWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = size;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 scale, int order, float rotZ)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static SpriteRenderer CreateTiled(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = Vector3.one;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = order;
        return sr;
    }

    private static Sprite LoadCandy(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(CandyArtFolder + "/" + fileName);
    }

    private static void TrySetTag(GameObject go, string tagName)
    {
        try { go.tag = tagName; }
        catch { /* Tag not defined in TagManager; leave default safely */ }
    }
}
