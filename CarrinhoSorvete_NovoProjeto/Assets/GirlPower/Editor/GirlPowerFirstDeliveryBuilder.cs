using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the first delivery only in its copied scene; source and reference scenes are never edited.</summary>
public static class GirlPowerFirstDeliveryBuilder
{
    private const string ScenePath = "Assets/Scenes/Girl Power - Primeira Entrega.unity";
    private const string ArtPath = "Assets/GirlPower/Art/";
    private static Sprite pixel;

    [MenuItem("Girl Power/Build First Delivery (working copy)")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects()) root.SetActive(false);
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "PanelWhite.png");
        if (pixel == null) { Debug.LogError("PanelWhite.png is missing; no source art was overwritten."); return; }

        GameObject world = new GameObject("Primeira Entrega - mundo de jogo");
        CreateCamera(world.transform);
        CreateBackdrop(world.transform);
        CreateRoutes(world.transform);
        CreateLandmarks(world.transform);
        CreateCharacters(world.transform);
        CreateMission(world.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Girl Power: cena de trabalho montada e salva. Referências preservadas; use WASD/setas, Shift, E e teclas 1–3.");
    }

    private static void CreateCamera(Transform root)
    {
        GameObject cameraObject = new GameObject("Delivery Camera");
        cameraObject.transform.SetParent(root, false);
        cameraObject.transform.position = new Vector3(0, 0.2f, -10);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = new Color(0.98f, 0.78f, 0.58f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateBackdrop(Transform root)
    {
        Sprite panorama = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "MatinhosStreet.png");
        if (panorama != null)
        {
            GameObject back = new GameObject("Fundo independente - orla de Matinhos ao entardecer");
            back.transform.SetParent(root, false);
            back.transform.position = new Vector3(10, 0.2f, 0.8f);
            back.transform.localScale = Vector3.one * 0.78f;
            SpriteRenderer sr = back.AddComponent<SpriteRenderer>();
            sr.sprite = panorama;
            sr.sortingOrder = -30;
        }
        CreateRect(root, "Base contínua da calçada", new Vector2(11, -1.25f), new Vector2(38, 6.6f), new Color32(228, 214, 187, 255), -20, false, false);
        CreateRect(root, "Orla - faixa do mar", new Vector2(11, 3.15f), new Vector2(38, 1.0f), new Color32(74, 163, 178, 255), -19, false, false);
        CreateRect(root, "Luz dourada do horizonte", new Vector2(11, 2.58f), new Vector2(38, 0.12f), new Color32(255, 204, 137, 255), -18, false, false);
    }

    private static void CreateRoutes(Transform root)
    {
        // A continuous wide promenade and a visibly narrower fair-market shortcut.
        CreateRect(root, "Rota ampla - calçada da orla", new Vector2(12, -1.65f), new Vector2(30, 1.75f), new Color32(196, 190, 171, 255), -10, false, false);
        CreateRect(root, "Rota curta - travessa da feira", new Vector2(12, 1.10f), new Vector2(30, 1.30f), new Color32(179, 155, 129, 255), -9, false, false);
        CreateRect(root, "Bifurcação - ligação de calçada", new Vector2(6.8f, -0.22f), new Vector2(4.1f, 3.4f), new Color32(204, 196, 174, 255), -11, false, false);
        for (int i = 0; i < 12; i++)
        {
            float x = -2 + i * 2.5f;
            CreateRect(root, "Junta de piso da orla", new Vector2(x, -1.65f), new Vector2(0.035f, 1.70f), new Color32(170, 165, 150, 255), -8, false, false);
            CreateRect(root, "Junta da travessa", new Vector2(x, 1.1f), new Vector2(0.035f, 1.24f), new Color32(147, 126, 106, 255), -7, false, false);
        }
        AddRouteLabel(root, "CALÇADA DA ORLA  ·  AMPLA", new Vector2(4.2f, -2.45f));
        AddRouteLabel(root, "ATALHO DA FEIRA  ·  MAIS MOVIMENTADO", new Vector2(4.9f, 1.95f));
        // Boxes and pedestrians make the market route navigable but slower by space, not by a timer.
        CreateRect(root, "Feira - caixa de frutas", new Vector2(10.2f, 1.12f), new Vector2(0.75f, 0.8f), new Color32(160, 104, 61, 255), 4, true, true);
        CreateRect(root, "Feira - caixa de flores", new Vector2(14.1f, 0.76f), new Vector2(0.85f, 0.95f), new Color32(186, 126, 66, 255), 4, true, true);
        CreatePedestrian(root, "Feira - pedestre azul", new Vector2(17.2f, 1.33f), new Color32(80, 119, 155, 255));
        CreatePedestrian(root, "Feira - pedestre coral", new Vector2(19.5f, 0.88f), new Color32(204, 107, 104, 255));
        // The promenade remains free of collision blockers.
        for (int i = 0; i < 5; i++)
        {
            float x = 3 + i * 4.4f;
            CreateRect(root, "Orla - banco baixo", new Vector2(x, -0.38f), new Vector2(0.8f, 0.18f), new Color32(135, 91, 63, 255), 0, false, false);
        }
    }

    private static void CreateLandmarks(Transform root)
    {
        // Small open-sided storefront at the start, on the back edge of the continuous sidewalk.
        CreateRect(root, "Sorveteria - balcão", new Vector2(0, 2.65f), new Vector2(3.5f, 0.52f), new Color32(105, 119, 111, 255), 3, false, false);
        CreateRect(root, "Sorveteria - toldo baixo", new Vector2(0, 3.44f), new Vector2(4.0f, 0.44f), new Color32(177, 79, 116, 255), 5, false, false);
        for (int i = 0; i < 5; i++)
            CreateRect(root, "Toldo - faixa creme", new Vector2(-1.6f + i * 0.8f, 3.44f), new Vector2(0.15f, 0.43f), new Color32(248, 224, 190, 255), 6, false, false);
        CreateRect(root, "Sorveteria - poste esquerdo", new Vector2(-1.6f, 2.92f), new Vector2(0.12f, 1.0f), new Color32(111, 75, 60, 255), 2, false, false);
        CreateRect(root, "Sorveteria - poste direito", new Vector2(1.6f, 2.92f), new Vector2(0.12f, 1.0f), new Color32(111, 75, 60, 255), 2, false, false);
        TextMeshPro awning = MakeText(root, "Nome temporário da sorveteria", new Vector2(0, 3.94f), "SORVETERIA", 2.0f, Color.white, 7);
        GameObject sign = new GameObject("Placa instalável - começa guardada");
        sign.transform.SetParent(root, false);
        sign.transform.position = new Vector3(0, 2.02f, 0);
        SpriteRenderer signRenderer = sign.AddComponent<SpriteRenderer>();
        signRenderer.sprite = pixel;
        signRenderer.color = new Color32(73, 54, 75, 255);
        signRenderer.sortingOrder = 9;
        sign.transform.localScale = new Vector3(3.0f, 0.4f, 1);
        sign.SetActive(false);
        TextMeshPro signText = MakeText(sign.transform, "Nome instalado", Vector2.zero, "", 1.4f, new Color32(255, 233, 200, 255), 10);
        signText.rectTransform.sizeDelta = new Vector2(3, 0.4f);
        signText.transform.localScale = new Vector3(1f / 3f, 2.5f, 1f);

        // Florist stall / destination landmark.
        CreateRect(root, "Florista - balcão", new Vector2(24, 2.05f), new Vector2(3.1f, 0.48f), new Color32(84, 121, 93, 255), 2, false, false);
        CreateRect(root, "Florista - cobertura", new Vector2(24, 3.0f), new Vector2(3.4f, 0.42f), new Color32(222, 148, 118, 255), 3, false, false);
        MakeText(root, "Identificação da florista", new Vector2(24, 3.55f), "FLORISTA", 2.0f, Color.white, 4);
    }

    private static void CreateCharacters(Transform root)
    {
        Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Idle.png");
        Sprite walkA = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_A.png");
        Sprite walkB = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_B.png");
        GameObject cart = new GameObject("Carrinho de sorvete - ponto inicial");
        cart.transform.SetParent(root, false);
        cart.transform.position = new Vector3(1.0f, -1.70f, 0);
        cart.transform.localScale = Vector3.one * 0.78f;
        // Draw the cart from the same crisp color blocks as the character, instead of mixing in the
        // painterly van illustration that exists in Resources and remains untouched.
        CreateRect(cart.transform, "Carrinho - rodas", new Vector2(1.0f, -2.15f), new Vector2(1.55f, 0.22f), new Color32(77, 60, 67, 255), 3, false, false);
        CreateRect(cart.transform, "Carrinho - corpo turquesa", new Vector2(1.0f, -1.75f), new Vector2(1.86f, 0.74f), new Color32(91, 190, 174, 255), 4, false, false);
        CreateRect(cart.transform, "Carrinho - painel rosa", new Vector2(1.0f, -1.94f), new Vector2(1.76f, 0.24f), new Color32(239, 113, 158, 255), 5, false, false);
        CreateRect(cart.transform, "Carrinho - toldo creme", new Vector2(1.0f, -1.25f), new Vector2(2.05f, 0.2f), new Color32(255, 231, 188, 255), 5, false, false);
        CreateRect(cart.transform, "Carrinho - letreiro coral", new Vector2(1.0f, -1.45f), new Vector2(1.82f, 0.16f), new Color32(194, 82, 116, 255), 6, false, false);
        CreateRect(cart.transform, "Sorvete - casquinha", new Vector2(1.0f, -0.98f), new Vector2(0.28f, 0.34f), new Color32(204, 147, 82, 255), 7, false, false);
        CreateRect(cart.transform, "Sorvete - bola", new Vector2(1.0f, -0.74f), new Vector2(0.38f, 0.25f), new Color32(245, 132, 171, 255), 8, false, false);
        CreateRect(cart.transform, "Sorvete - brilho", new Vector2(0.93f, -0.68f), new Vector2(0.08f, 0.06f), new Color32(255, 226, 220, 255), 9, false, false);
        BoxCollider2D cartCollider = cart.AddComponent<BoxCollider2D>();
        cartCollider.size = new Vector2(1.8f, 1.2f);
        cartCollider.isTrigger = true;

        GameObject player = new GameObject("Protagonista - independente do cenário");
        player.transform.SetParent(root, false);
        player.transform.position = new Vector3(-1.1f, -1.65f, 0);
        SpriteRenderer avatar = player.AddComponent<SpriteRenderer>();
        avatar.sprite = idle;
        avatar.sortingOrder = 8;
        CircleCollider2D body = player.AddComponent<CircleCollider2D>();
        body.radius = 0.24f;

        GameObject florist = new GameObject("Florista - cliente da primeira entrega");
        florist.transform.SetParent(root, false);
        florist.transform.position = new Vector3(24, -0.15f, 0);
        SpriteRenderer npc = florist.AddComponent<SpriteRenderer>();
        npc.sprite = idle;
        npc.color = new Color32(160, 103, 83, 255);
        npc.sortingOrder = 7;
        CapsuleCollider2D npcCollider = florist.AddComponent<CapsuleCollider2D>();
        npcCollider.size = new Vector2(0.65f, 0.8f);
        npcCollider.isTrigger = true;

    }

    private static void CreateMission(Transform root)
    {
        GameObject missionObject = new GameObject("First Delivery Mission - no timer");
        missionObject.transform.SetParent(root, false);
        GirlPowerFirstDelivery mission = missionObject.AddComponent<GirlPowerFirstDelivery>();
        mission.player = root.Find("Protagonista - independente do cenário");
        mission.cart = root.Find("Carrinho de sorvete - ponto inicial");
        mission.florist = root.Find("Florista - cliente da primeira entrega");
        mission.sign = root.Find("Placa instalável - começa guardada").transform;
        mission.sceneCamera = root.Find("Delivery Camera").GetComponent<Camera>();
        mission.playerSprite = mission.player.GetComponent<SpriteRenderer>();
        mission.idle = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Idle.png");
        mission.walkA = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_A.png");
        mission.walkB = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Girl_Walk_B.png");
        mission.signLabel = mission.sign.GetComponentInChildren<TextMeshPro>(true);
        mission.obstacles = Object.FindObjectsOfType<Collider2D>();
    }

    private static void CreatePedestrian(Transform root, string name, Vector2 position, Color32 color)
    {
        CreateRect(root, name + " - sombra", position + new Vector2(0, -0.34f), new Vector2(0.62f, 0.12f), new Color32(96, 79, 69, 130), 2, false, false);
        CreateRect(root, name + " - corpo", position, new Vector2(0.40f, 0.64f), color, 5, true, true);
        CreateRect(root, name + " - cabeça", position + new Vector2(0, 0.42f), new Vector2(0.34f, 0.34f), new Color32(185, 131, 96, 255), 6, false, false);
    }

    private static void AddRouteLabel(Transform root, string content, Vector2 position) => MakeText(root, "Orientação de rota", position, content, 1.15f, new Color32(74, 61, 62, 255), -2);

    private static TextMeshPro MakeText(Transform parent, string name, Vector2 position, string content, float fontSize, Color color, int order)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.position = new Vector3(position.x, position.y, 0);
        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.rectTransform.sizeDelta = new Vector2(5.5f, 0.5f);
        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sortingOrder = order;
        return text;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size, Color32 color, int order, bool collider, bool block)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, position.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = order;
        if (collider)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.isTrigger = !block;
        }
        return go;
    }
}
