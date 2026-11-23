using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the upgraded Matinhos golden-hour coastal scenery for the isolated
/// first delivery scene ("Girl Power - Primeira Entrega.unity").
/// Source and reference scenes remain completely untouched.
/// </summary>
public static class GirlPowerFirstDeliveryBuilder
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Primeira Entrega.unity";
    private const string ArtFolder = "Assets/GirlPower/Art";
    private const string WorldRootName = "Primeira Entrega - mundo de jogo";

    [MenuItem("Girl Power/Build First Delivery (working copy)")]
    public static void Build()
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "GirlPower/Art/PanelWhite.png")))
        {
            Debug.LogError("PanelWhite.png is missing; source scenes and artwork were not changed.");
            return;
        }

        GirlPowerArtGenerator.GenerateAndImportAll(ArtFolder);

        Sprite backdrop = LoadSprite("MatinhosEveningPixelArt.png");
        if (backdrop == null)
        {
            Debug.LogError("Could not load generated MatinhosEveningPixelArt.png.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == WorldRootName) Object.DestroyImmediate(root);
            else root.SetActive(false);
        }

        GameObject world = new GameObject(WorldRootName);
        Camera mainCam = CreateCamera(world.transform);

        List<Transform> clouds = new List<Transform>();
        List<SpriteRenderer> seagulls = new List<SpriteRenderer>();
        List<SpriteRenderer> waterShimmers = new List<SpriteRenderer>();
        List<Transform> swayingPalms = new List<Transform>();
        List<Transform> swayingFestoons = new List<Transform>();
        List<SpriteRenderer> warmGlows = new List<SpriteRenderer>();
        List<SpriteRenderer> goldenMotes = new List<SpriteRenderer>();
        List<Transform> idleCharacters = new List<Transform>();

        CreateBackdropAndAtmosphere(world.transform, backdrop, clouds, seagulls, waterShimmers);
        CreatePlazasAndRoutes(world.transform);
        CreateCoastalPropsAndFeira(world.transform, swayingPalms, swayingFestoons, warmGlows, goldenMotes);
        CreateShopfront(world.transform, warmGlows, goldenMotes);
        CreateFloristStand(world.transform, warmGlows, goldenMotes);
        CreateObstacles(world.transform, idleCharacters);
        CreateCharacters(world.transform, idleCharacters);
        CreateSceneryAnimator(world.transform, mainCam, clouds, seagulls, waterShimmers, swayingPalms, swayingFestoons, warmGlows, goldenMotes, idleCharacters);
        CreateMission(world.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: cenário costeiro de Matinhos atualizado com arte pixel art, calçadão petit-pavé, feirinha da orla, quiosque, florista, carrinho detalhado e iluminação animada.");
    }

    private static Camera CreateCamera(Transform root)
    {
        GameObject cameraObject = new GameObject("Delivery Camera");
        cameraObject.transform.SetParent(root, false);
        cameraObject.transform.position = new Vector3(-0.2f, 0.15f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color32(84, 72, 114, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
        return camera;
    }

    private static void CreateBackdropAndAtmosphere(
        Transform root,
        Sprite backdrop,
        List<Transform> clouds,
        List<SpriteRenderer> seagulls,
        List<SpriteRenderer> waterShimmers)
    {
        GameObject back = new GameObject("Arte de fundo - orla de Matinhos ao entardecer");
        back.transform.SetParent(root, false);
        back.transform.position = new Vector3(12f, 0.15f, 0.9f);
        back.transform.localScale = new Vector3(4.2f, 4.2f, 1f);
        SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
        sr.sprite = backdrop;
        sr.sortingOrder = -30;

        Sprite cloudSprite = LoadSprite("CloudEvening.png");
        float[] cloudXs = { -6.5f, 4.0f, 15.5f, 26.5f };
        float[] cloudYs = { 4.78f, 4.92f, 4.72f, 4.86f };
        float[] cloudScales = { 0.95f, 1.15f, 1.05f, 0.9f };
        for (int i = 0; i < cloudXs.Length; i++)
        {
            SpriteRenderer c = CreateSprite(root, "Nuvem ao entardecer " + (i + 1), cloudSprite, new Vector2(cloudXs[i], cloudYs[i]), cloudScales[i], -26);
            c.color = new Color(1f, 0.95f, 0.92f, 0.85f);
            clouds.Add(c.transform);
        }

        Sprite gullSprite = LoadSprite("SeagullA.png");
        float[] gullXs = { -2.5f, 11.5f, 21.0f };
        float[] gullYs = { 4.48f, 4.75f, 4.58f };
        for (int i = 0; i < gullXs.Length; i++)
        {
            SpriteRenderer g = CreateSprite(root, "Gaivota costeira " + (i + 1), gullSprite, new Vector2(gullXs[i], gullYs[i]), 0.68f, -25);
            seagulls.Add(g);
        }

        Sprite shimmerSprite = LoadSprite("WaterShimmer.png");
        float[] shimmerXs = { -3.5f, 2.8f, 8.2f, 13.0f, 17.5f, 22.8f, 28.0f };
        float[] shimmerYs = { 3.22f, 3.38f, 3.16f, 3.32f, 3.20f, 3.36f, 3.24f };
        for (int i = 0; i < shimmerXs.Length; i++)
        {
            SpriteRenderer w = CreateSprite(root, "Reflexo do mar " + (i + 1), shimmerSprite, new Vector2(shimmerXs[i], shimmerYs[i]), 1.1f, -24);
            w.color = new Color(1f, 0.96f, 0.88f, 0.65f);
            waterShimmers.Add(w);
        }
    }

    private static void CreatePlazasAndRoutes(Transform root)
    {
        Sprite orlaTile = LoadSprite("OrlaSidewalkTile.png");
        Sprite feiraTile = LoadSprite("FeiraCobbleTile.png");
        Sprite plazaTile = LoadSprite("PlazaTile.png");

        // Continuous Matinhos beachfront Portuguese stone (petit-pave) promenade
        CreateTiledSprite(root, "Rota ampla - calçadão de pedras portuguesas", orlaTile, new Vector2(12f, -1.65f), new Vector2(50f, 1.84f), -10);

        // Cobblestone Feira shortcut cleanly joining Bifurcation Plaza (x=8.8) to Praca da Florista (x=21.4)
        CreateTiledSprite(root, "Atalho - travessa da feira", feiraTile, new Vector2(15.1f, 1.10f), new Vector2(12.6f, 1.32f), -9);

        // Welcoming paved plazas aligned cleanly below the coastal mureta (top y = +1.75)
        CreateTiledSprite(root, "Praça inicial - Quiosque Girl Power", plazaTile, new Vector2(0.0f, 0.48f), new Vector2(7.2f, 2.54f), -13);
        CreateTiledSprite(root, "Bifurcação - ligação entre os caminhos", plazaTile, new Vector2(6.8f, 0.45f), new Vector2(4.0f, 2.60f), -12);
        CreateTiledSprite(root, "Praça da florista - chegada das rotas", plazaTile, new Vector2(24.0f, 0.48f), new Vector2(5.2f, 2.54f), -13);

        // Route directional plaques
        CreateRouteMarker(root, "ORLA  →  CAMINHO AMPLO", new Vector2(4.8f, -2.36f), new Color32(62, 118, 126, 245));
        CreateRouteMarker(root, "FEIRA  →  ATALHO COM MOVIMENTO", new Vector2(6.7f, 1.98f), new Color32(142, 82, 72, 245));
    }

    private static void CreateCoastalPropsAndFeira(
        Transform root,
        List<Transform> swayingPalms,
        List<Transform> swayingFestoons,
        List<SpriteRenderer> warmGlows,
        List<SpriteRenderer> goldenMotes)
    {
        Sprite palmSprite = LoadSprite("PalmTree.png");
        Sprite stallA = LoadSprite("MarketStallA.png");
        Sprite stallB = LoadSprite("MarketStallB.png");
        Sprite festoonSprite = LoadSprite("FestoonLights.png");
        Sprite lampSprite = LoadSprite("StreetLamp.png");
        Sprite benchSprite = LoadSprite("CoastalBench.png");
        Sprite bushSprite = LoadSprite("PlanterBush.png");
        Sprite glowSprite = LoadSprite("WarmGlow.png");

        // Arching coconut palms placed cleanly on the coastal mureta/grass edge (leaving the sunset bay open!)
        float[] palmXs = { -5.4f, 4.3f, 8.6f, 17.8f, 27.6f };
        float[] palmScales = { 1.08f, 1.02f, 0.98f, 1.04f, 1.10f };
        for (int i = 0; i < palmXs.Length; i++)
        {
            GameObject pivot = new GameObject("Coqueiro da orla " + (i + 1));
            pivot.transform.SetParent(root, false);
            // Anchor pivot near trunk base on the mureta/garden line so breeze sways from base!
            pivot.transform.localPosition = new Vector3(palmXs[i], 1.62f, 0f);
            SpriteRenderer sr = CreateSprite(pivot.transform, "Copa e tronco", palmSprite, new Vector2(0f, 1.58f * palmScales[i]), palmScales[i], -4);
            sr.flipX = (i % 2 == 1);
            swayingPalms.Add(pivot.transform);
        }

        // Feirinha da Orla stalls lining the upper edge of the Feira shortcut
        CreateSprite(root, "Barraca da feira - Água de Coco", stallA, new Vector2(11.1f, 2.74f), 0.94f, -3);
        CreateSprite(root, "Barraca da feira - Cangas e Artesanato", stallB, new Vector2(15.6f, 2.74f), 0.94f, -3);
        CreateSprite(root, "Barraca da feira - Frutas Tropicais", stallA, new Vector2(20.1f, 2.74f), 0.94f, -3);

        // Draped coastal festoon string lights & pennants along the Feira
        float[] festoonXs = { 11.1f, 15.6f, 20.1f };
        for (int i = 0; i < festoonXs.Length; i++)
        {
            SpriteRenderer fest = CreateSprite(root, "Varal de bandeirinhas e luzes " + (i + 1), festoonSprite, new Vector2(festoonXs[i], 2.28f), 0.92f, 4);
            swayingFestoons.Add(fest.transform);
            CreateGlow(root, glowSprite, new Vector2(festoonXs[i], 2.18f), 1.85f, new Color(1f, 0.84f, 0.52f, 0.28f), 3, warmGlows);
        }

        // Lush restinga planter bushes decorating the central coastal garden median
        float[] bushXs = { -4.4f, 10.6f, 13.8f, 17.0f, 20.1f, 27.6f };
        for (int i = 0; i < bushXs.Length; i++)
            CreateSprite(root, "Jardineira costeira " + (i + 1), bushSprite, new Vector2(bushXs[i], -0.15f), 0.96f, 2);

        // Classic evening streetlamps flanking the Kiosk, Florist, and along the central garden median
        Vector2[] lampBases =
        {
            new Vector2(-2.95f, 1.66f),
            new Vector2(2.95f, 1.66f),
            new Vector2(12.2f, -0.52f),
            new Vector2(18.6f, -0.52f),
            new Vector2(21.3f, 1.66f),
            new Vector2(26.7f, 1.66f)
        };
        for (int i = 0; i < lampBases.Length; i++)
        {
            Vector2 basePos = lampBases[i];
            int order = basePos.y < 0f ? 4 : 1;
            // Sprite is 84px tall at 32 PPU -> height = 2.625 -> center is basePos.y + 1.18
            CreateSprite(root, "Poste de luz costeiro " + (i + 1), lampSprite, basePos + new Vector2(0f, 1.18f), 0.94f, order);
            // Warm glowing lantern halo around the glass housing
            Vector2 lanternPos = basePos + new Vector2(0.32f, 1.82f);
            CreateGlow(root, glowSprite, lanternPos, 1.65f, new Color(1f, 0.86f, 0.56f, 0.55f), order + 1, warmGlows);
            // Soft warm evening light pool cast on the ground beneath the lamp
            CreateGlow(root, glowSprite, basePos + new Vector2(0.28f, -0.05f), 2.1f, new Color(1f, 0.84f, 0.54f, 0.20f), -6, warmGlows);
            // Floating golden-hour firefly / light mote near each lantern
            CreateMote(root, glowSprite, lanternPos + new Vector2(0.15f, -0.25f), 0.24f, 9, goldenMotes);
        }

        // Coastal boardwalk wooden benches facing the sea along the lower terrace
        float[] benchXs = { -1.4f, 4.0f, 9.2f, 14.4f, 19.6f, 25.0f };
        for (int i = 0; i < benchXs.Length; i++)
            CreateSprite(root, "Banco da orla " + (i + 1), benchSprite, new Vector2(benchXs[i], -2.98f), 0.94f, 1);
    }

    private static void CreateShopfront(Transform root, List<SpriteRenderer> warmGlows, List<SpriteRenderer> goldenMotes)
    {
        Sprite kioskSprite = LoadSprite("KioskGirlPower.png");
        Sprite signSprite = LoadSprite("SignboardNew.png");
        Sprite glowSprite = LoadSprite("WarmGlow.png");

        CreateSprite(root, "Quiosque Girl Power - fachada costeira", kioskSprite, new Vector2(0f, 2.86f), 0.98f, -1);

        // Header title centered cleanly inside the kiosk's purple-and-gold header plaque
        MakeText(root, "Nome da sorveteria", new Vector2(0f, 4.18f), "GIRL POWER", 2.7f, new Color32(255, 240, 212, 255), 5, new Vector2(2.6f, 0.45f));

        // Cozy golden evening light radiating from under the kiosk awning
        CreateGlow(root, glowSprite, new Vector2(-1.1f, 3.18f), 1.7f, new Color(1f, 0.86f, 0.56f, 0.36f), 0, warmGlows);
        CreateGlow(root, glowSprite, new Vector2(1.1f, 3.18f), 1.7f, new Color(1f, 0.86f, 0.56f, 0.36f), 0, warmGlows);
        CreateMote(root, glowSprite, new Vector2(-1.8f, 2.35f), 0.22f, 9, goldenMotes);
        CreateMote(root, glowSprite, new Vector2(1.9f, 2.45f), 0.20f, 9, goldenMotes);

        // Installable custom shop signboard (stays hidden until the player completes the first delivery!)
        GameObject sign = new GameObject("Placa nova - fica guardada até ser instalada");
        sign.transform.SetParent(root, false);
        sign.transform.localPosition = new Vector3(0f, 1.98f, 0f);
        sign.transform.localScale = Vector3.one * 0.92f;
        SpriteRenderer signSr = sign.AddComponent<SpriteRenderer>();
        signSr.sprite = signSprite;
        signSr.sortingOrder = 10;
        sign.SetActive(false);

        MakeText(sign.transform, "Nome instalado", Vector2.zero, "", 2.55f, new Color32(255, 238, 206, 255), 11, new Vector2(3.1f, 0.55f));
    }

    private static void CreateFloristStand(Transform root, List<SpriteRenderer> warmGlows, List<SpriteRenderer> goldenMotes)
    {
        Sprite floristStandSprite = LoadSprite("FloristStand.png");
        Sprite glowSprite = LoadSprite("WarmGlow.png");

        CreateSprite(root, "Banca da florista - Flores da Praça", floristStandSprite, new Vector2(24f, 2.82f), 0.98f, -1);

        MakeText(root, "Identificação da florista", new Vector2(24f, 4.18f), "FLORES DA PRAÇA", 2.35f, new Color32(255, 244, 216, 255), 5, new Vector2(3.2f, 0.45f));

        CreateGlow(root, glowSprite, new Vector2(24f, 3.1f), 2.3f, new Color(1f, 0.88f, 0.60f, 0.34f), 0, warmGlows);
        CreateMote(root, glowSprite, new Vector2(22.6f, 2.25f), 0.24f, 9, goldenMotes);
        CreateMote(root, glowSprite, new Vector2(25.3f, 2.40f), 0.22f, 9, goldenMotes);
    }

    private static void CreateObstacles(Transform root, List<Transform> idleCharacters)
    {
        Sprite shadowSprite = LoadSprite("ShadowOval.png");
        CreateObstacle(root, "Caixa de laranjas", LoadSprite("CrateOranges.png"), shadowSprite, new Vector2(10.2f, 1.05f), new Vector2(0.78f, 0.72f), 0.86f, false, idleCharacters);
        CreateObstacle(root, "Caixa de flores", LoadSprite("CrateFlowers.png"), shadowSprite, new Vector2(14.2f, 0.76f), new Vector2(0.85f, 0.84f), 0.88f, false, idleCharacters);
        CreateObstacle(root, "Pedestre azul", LoadSprite("PedestrianBlue.png"), shadowSprite, new Vector2(17.2f, 1.34f), new Vector2(0.54f, 0.76f), 0.86f, true, idleCharacters);
        CreateObstacle(root, "Pedestre coral", LoadSprite("PedestrianCoral.png"), shadowSprite, new Vector2(19.6f, 0.88f), new Vector2(0.54f, 0.76f), 0.86f, true, idleCharacters);
    }

    private static void CreateObstacle(
        Transform root,
        string name,
        Sprite sprite,
        Sprite shadowSprite,
        Vector2 position,
        Vector2 colliderSize,
        float scale,
        bool animateIdle,
        List<Transform> idleCharacters)
    {
        GameObject obstacle = new GameObject(name);
        obstacle.transform.SetParent(root, false);
        obstacle.transform.localPosition = new Vector3(position.x, position.y, 0f);
        BoxCollider2D box = obstacle.AddComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.isTrigger = false;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(obstacle.transform, false);
        visual.transform.localPosition = animateIdle ? new Vector3(0f, 0.16f, 0f) : Vector3.zero;
        visual.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        if (animateIdle)
        {
            CreateSprite(obstacle.transform, "Sombra", shadowSprite, new Vector2(0f, -0.42f), 0.75f, 3);
            idleCharacters.Add(visual.transform);
        }
    }

    private static void CreateCharacters(Transform root, List<Transform> idleCharacters)
    {
        Sprite idle = LoadSprite("Girl_Idle.png");
        Sprite cartSprite = LoadSprite("IceCreamCart.png");
        Sprite floristNpcSprite = LoadSprite("FloristNPC.png");
        Sprite shadowSprite = LoadSprite("ShadowOval.png");

        // Handcrafted Ice Cream Cart (exact name & trigger collider required by GirlPowerFirstDelivery)
        GameObject cart = new GameObject("Carrinho de sorvete - posição e colisão próprias");
        cart.transform.SetParent(root, false);
        cart.transform.localPosition = new Vector3(0.8f, -1.78f, 0f);
        SpriteRenderer cartSr = CreateSprite(cart.transform, "Visual do carrinho", cartSprite, new Vector2(0f, 0.24f), 0.96f, 6);
        BoxCollider2D cartCollider = cart.AddComponent<BoxCollider2D>();
        cartCollider.size = new Vector2(1.8f, 1.15f);
        cartCollider.isTrigger = true;
        if (cartSr == null) Debug.LogWarning("IceCreamCart.png missing.");

        // Protagonist (exact name & CircleCollider2D required by GirlPowerFirstDelivery)
        GameObject player = new GameObject("Protagonista - personagem independente");
        player.transform.SetParent(root, false);
        player.transform.localPosition = new Vector3(-1.25f, -1.68f, 0f);
        player.transform.localScale = Vector3.one * 0.86f;
        SpriteRenderer avatar = player.AddComponent<SpriteRenderer>();
        avatar.sprite = idle;
        avatar.sortingOrder = 8;
        CircleCollider2D body = player.AddComponent<CircleCollider2D>();
        body.radius = 0.24f;
        CreateSprite(player.transform, "Sombra da protagonista", shadowSprite, new Vector2(0f, -0.68f), 0.82f, 4);

        // Florist NPC with dedicated straw-hat & floral-apron pixel sprite
        GameObject florist = new GameObject("Florista - cliente da primeira entrega");
        florist.transform.SetParent(root, false);
        florist.transform.localPosition = new Vector3(24f, -0.15f, 0f);
        florist.transform.localScale = Vector3.one * 0.86f;
        SpriteRenderer npc = florist.AddComponent<SpriteRenderer>();
        npc.sprite = floristNpcSprite != null ? floristNpcSprite : idle;
        npc.color = Color.white;
        npc.sortingOrder = 7;
        CapsuleCollider2D npcCollider = florist.AddComponent<CapsuleCollider2D>();
        npcCollider.size = new Vector2(0.65f, 0.8f);
        npcCollider.isTrigger = true;
        CreateSprite(florist.transform, "Sombra da florista", shadowSprite, new Vector2(0f, -0.68f), 0.82f, 4);
        idleCharacters.Add(florist.transform);
    }

    private static void CreateSceneryAnimator(
        Transform root,
        Camera mainCam,
        List<Transform> clouds,
        List<SpriteRenderer> seagulls,
        List<SpriteRenderer> waterShimmers,
        List<Transform> swayingPalms,
        List<Transform> swayingFestoons,
        List<SpriteRenderer> warmGlows,
        List<SpriteRenderer> goldenMotes,
        List<Transform> idleCharacters)
    {
        GameObject ambient = new GameObject("Ambientacao Costeira - Matinhos");
        ambient.transform.SetParent(root, false);
        GirlPowerSceneryAnimator anim = ambient.AddComponent<GirlPowerSceneryAnimator>();
        anim.targetCamera = mainCam;
        anim.clouds = clouds.ToArray();
        anim.seagulls = seagulls.ToArray();
        anim.seagullFrameA = LoadSprite("SeagullA.png");
        anim.seagullFrameB = LoadSprite("SeagullB.png");
        anim.waterShimmers = waterShimmers.ToArray();
        anim.swayingPalms = swayingPalms.ToArray();
        anim.swayingFestoons = swayingFestoons.ToArray();
        anim.warmGlows = warmGlows.ToArray();
        anim.goldenMotes = goldenMotes.ToArray();
        anim.idleCharacters = idleCharacters.ToArray();
    }

    private static void CreateMission(Transform root)
    {
        GameObject missionObject = new GameObject("First Delivery Mission - no timer");
        missionObject.transform.SetParent(root, false);
        GirlPowerFirstDelivery mission = missionObject.AddComponent<GirlPowerFirstDelivery>();
        mission.player = root.Find("Protagonista - personagem independente");
        mission.cart = root.Find("Carrinho de sorvete - posição e colisão próprias");
        mission.florist = root.Find("Florista - cliente da primeira entrega");
        mission.sign = root.Find("Placa nova - fica guardada até ser instalada");
        mission.sceneCamera = root.Find("Delivery Camera").GetComponent<Camera>();
        mission.playerSprite = mission.player.GetComponent<SpriteRenderer>();
        mission.idle = LoadSprite("Girl_Idle.png");
        mission.walkA = LoadSprite("Girl_Walk_A.png");
        mission.walkB = LoadSprite("Girl_Walk_B.png");
        mission.signLabel = mission.sign.GetComponentInChildren<TextMeshPro>(true);
        mission.obstacles = Object.FindObjectsOfType<Collider2D>();
    }

    private static void CreateRouteMarker(Transform root, string label, Vector2 position, Color32 tint)
    {
        Sprite plaque = LoadSprite("RouteSignpost.png");
        SpriteRenderer sr = CreateSprite(root, "Sinalização da bifurcação", plaque, position, 1f, 0);
        sr.transform.localScale = new Vector3(1.32f, 0.56f, 1f);
        sr.color = tint;
        MakeText(root, "Direção da rota", position, label, 2.25f, new Color32(255, 245, 223, 255), 1, new Vector2(5.4f, 0.45f));
    }

    private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, Vector2 position, float scale, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private static SpriteRenderer CreateTiledSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = Vector3.one;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = order;
        return sr;
    }

    private static void CreateGlow(Transform parent, Sprite glowSprite, Vector2 pos, float scale, Color color, int order, List<SpriteRenderer> list)
    {
        SpriteRenderer sr = CreateSprite(parent, "Luz quente", glowSprite, pos, scale, order);
        sr.color = color;
        list.Add(sr);
    }

    private static void CreateMote(Transform parent, Sprite glowSprite, Vector2 pos, float scale, int order, List<SpriteRenderer> list)
    {
        SpriteRenderer sr = CreateSprite(parent, "Partícula dourada", glowSprite, pos, scale, order);
        sr.color = new Color(1f, 0.92f, 0.64f, 0.75f);
        list.Add(sr);
    }

    private static TextMeshPro MakeText(Transform parent, string name, Vector2 position, string content, float fontSize, Color color, int order, Vector2 boxSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.rectTransform.sizeDelta = boxSize;
        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = order;
        return text;
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + fileName);
    }
}
