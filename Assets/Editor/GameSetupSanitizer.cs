using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GameSetupSanitizer : EditorWindow
{
    private string logOutput = "Aguardando início do escaneamento...";
    private Vector2 scrollPos;

    [MenuItem("Sorvete/Corrigir e Alinhar Jogo")]
    public static void ShowWindow()
    {
        GetWindow<GameSetupSanitizer>("Corretor do Jogo");
    }

    private void OnGUI()
    {
        titleContent = new GUIContent("Corretor do Jogo", "Ferramentas de Saneamento do Carrinho de Sorvete");
        minSize = new Vector2(450, 400);

        GUILayout.Label("Ferramentas de Alinhamento e Correção do Jogo", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Esta ferramenta irá inspecionar a cena ativa e automaticamente:\n" +
            "1. Corrigir a Tag do jogador para 'Player' (evita spawn de monstros em cima de você).\n" +
            "2. Separar o SpriteRenderer do jogador para um objeto visual filho 'CarroSprite'.\n" +
            "3. Alinhar a rotação do sprite visual (-90 graus para a Van de Sorvete para a cabine apontar para cima).\n" +
            "4. Centralizar o 'Ponto do tiro' na frente do para-choque frontal (local Y = 1.2).\n" +
            "5. Associar o 'Ponto do tiro' ao campo do script 'DriverPro' no Inspector.\n" +
            "6. Configurar as propriedades ideais do Rigidbody2D físicas do carro.\n" +
            "7. Retirar o carro de dentro do Canvas de UI (caso tenha sido arrastado por engano).\n" +
            "8. Corrigir o EventSystem para evitar travamento de cliques da nova UI InputSystem.",
            MessageType.Info
        );

        if (GUILayout.Button("Executar Diagnóstico e Correção Completa", GUILayout.Height(40)))
        {
            RunSanitizer();
        }

        GUILayout.Space(10);
        GUILayout.Label("Relatório da Execução:", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(180));
        EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void RunSanitizer()
    {
        logOutput = "Iniciando escaneamento da cena...\n";

        // 1. Localizar o objeto do jogador
        GameObject playerObj = GameObject.Find("carro");
        if (playerObj == null) playerObj = GameObject.Find("carro-pai");
        if (playerObj == null)
        {
            DriverPro driver = FindObjectOfType<DriverPro>();
            if (driver != null) playerObj = driver.gameObject;
        }

        if (playerObj == null)
        {
            logOutput += "[ERRO] Carrinho de jogador não foi encontrado na cena ativa!\n";
            logOutput += "Certifique-se de que o objeto 'carro' ou 'carro-pai' existe na Hierarchy.\n";
            return;
        }

        // Registrar no histórico de Desfazer (Undo) da Unity
        Undo.RegisterCompleteObjectUndo(playerObj, "Sanitize Game Setup");

        logOutput += $"[INFO] Jogador encontrado: '{playerObj.name}'\n";

        logOutput += "[INFO] Rotação do objeto pai preservada (para permitir controle manual).\n";

        // 2. Corrigir a Tag do Jogador
        if (playerObj.tag != "Player")
        {
            playerObj.tag = "Player";
            logOutput += "[AJUSTE] Tag do jogador alterada para 'Player'.\n";
        }
        else
        {
            logOutput += "[OK] Tag do jogador já está configurada como 'Player'.\n";
        }

        // 3. Verificar se o jogador está dentro de um Canvas
        Canvas canvas = playerObj.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            playerObj.transform.SetParent(null);
            logOutput += "[ALERTA] Carrinho estava dentro de um Canvas de UI! Movido de volta para a raiz do cenário.\n";
        }

        // 4. Configurar propriedades de Rigidbody2D físicas
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = playerObj.AddComponent<Rigidbody2D>();
            logOutput += "[AJUSTE] Componente Rigidbody2D adicionado ao jogador.\n";
        }
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        logOutput += "[OK] Rigidbody2D configurado para física ideal de movimentação.\n";

        // 5. Garantir scripts anexados
        DriverPro driverPro = playerObj.GetComponent<DriverPro>();
        if (driverPro == null)
        {
            driverPro = playerObj.AddComponent<DriverPro>();
            logOutput += "[AJUSTE] Script 'DriverPro' adicionado ao jogador.\n";
        }

        Delivery delivery = playerObj.GetComponent<Delivery>();
        if (delivery == null)
        {
            delivery = playerObj.AddComponent<Delivery>();
            logOutput += "[AJUSTE] Script 'Delivery' adicionado ao jogador.\n";
        }

        // 6. Separar o SpriteRenderer para um objeto visual filho
        SpriteRenderer parentSr = playerObj.GetComponent<SpriteRenderer>();
        Transform visualChild = null;

        foreach (Transform child in playerObj.transform)
        {
            if (child.name == "CarroSprite" || child.name == "CarroSprite_Auto" || child.name == "CarroSprite-filho")
            {
                visualChild = child;
                break;
            }
        }

        if (parentSr != null && parentSr.sprite != null)
        {
            if (visualChild == null)
            {
                GameObject visualObj = new GameObject("CarroSprite");
                visualObj.transform.SetParent(playerObj.transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localScale = Vector3.one;
                visualObj.transform.localRotation = Quaternion.identity;

                SpriteRenderer childSr = visualObj.AddComponent<SpriteRenderer>();
                childSr.sprite = parentSr.sprite;
                childSr.material = parentSr.material;
                childSr.color = parentSr.color;
                childSr.sortingLayerID = parentSr.sortingLayerID;
                childSr.sortingOrder = parentSr.sortingOrder;
                childSr.flipX = parentSr.flipX;
                childSr.flipY = parentSr.flipY;

                visualChild = visualObj.transform;
                logOutput += "[AJUSTE] Objeto visual filho 'CarroSprite' criado com sucesso.\n";
            }
            DestroyImmediate(parentSr);
            logOutput += "[AJUSTE] SpriteRenderer duplicado removido do pai para evitar bugs.\n";
        }

        // 7. Alinhar a rotação visual (preserva a rotação manual feita pelo usuário)
        if (visualChild != null)
        {
            logOutput += "[OK] Rotação local do sprite preservada (para respeitar a rotação manual de " + visualChild.localEulerAngles.z + " graus).\n";
        }

        // 8. Corrigir Ponto do Tiro
        Transform bulletSpawn = null;
        foreach (Transform child in playerObj.transform)
        {
            string cName = child.name.ToLower();
            if (cName.Contains("tiro") || cName.Contains("bullet") || cName.Contains("spawn") || cName.Contains("point"))
            {
                bulletSpawn = child;
                break;
            }
        }

        if (bulletSpawn == null)
        {
            GameObject spawnObj = new GameObject("Ponto do tiro");
            spawnObj.transform.SetParent(playerObj.transform);
            bulletSpawn = spawnObj.transform;
            logOutput += "[AJUSTE] Ponto do tiro ('Ponto do tiro') criado automaticamente.\n";
        }

        // Forçar posicionamento no para-choque frontal (Y = 1.2) e zerar rotação local
        bulletSpawn.localPosition = new Vector3(0f, 1.2f, 0f);
        bulletSpawn.localRotation = Quaternion.identity;
        logOutput += "[AJUSTE] Ponto do tiro alinhado na frente da cabine (local Y = 1.2, rotação 0).\n";

        // Associar o ponto do tiro no DriverPro Inspector
        SerializedObject driverSo = new SerializedObject(driverPro);
        SerializedProperty spawnProp = driverSo.FindProperty("bulletSpawnPoint");
        if (spawnProp != null)
        {
            spawnProp.objectReferenceValue = bulletSpawn.gameObject;
            logOutput += "[OK] Objeto 'Ponto do tiro' associado ao campo 'Bullet Spawn Point' no script DriverPro.\n";
        }

        // Zerar o offset do script para evitar rotações duplas
        SerializedProperty offsetProp = driverSo.FindProperty("spriteRotationOffset");
        if (offsetProp != null)
        {
            offsetProp.floatValue = 0f;
            logOutput += "[OK] Campo 'Sprite Rotation Offset' zerado no script.\n";
        }

        driverSo.ApplyModifiedProperties();

        // 9. Consertar EventSystem InputSystemUIInputModule
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var oldModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                DestroyImmediate(oldModule);
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                logOutput += "[OK] Módulo StandaloneInputModule do EventSystem substituído por InputSystemUIInputModule.\n";
            }
        }

        // Marcar a cena como modificada para habilitar o salvamento
        EditorSceneManager.MarkSceneDirty(playerObj.scene);

        logOutput += "\n[SUCESSO] Configuração concluída! Salve a cena (Ctrl+S / Cmd+S) e teste o jogo!\n";
    }
}
