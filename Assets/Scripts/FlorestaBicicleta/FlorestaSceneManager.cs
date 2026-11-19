using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Monta e gerencia toda a cena da Floresta de Bicicleta:
    /// céu azul, sol, nuvens, montanhas com parallax, floresta, pista,
    /// menina com cachorrinho na cesta e os 5 animais para alimentar.
    /// </summary>
    public class FlorestaSceneManager : MonoBehaviour
    {
        private Camera mainCam;
        private BikeGirlController player;
        private Sprite heartSprite;

        private readonly List<ForestAnimal> animals = new List<ForestAnimal>();
        private int fedCount = 0;

        // HUD
        private TextMeshProUGUI counterText;
        private TextMeshProUGUI speedText;
        private TextMeshProUGUI promptText;
        private GameObject feedButtonObj;
        private GameObject victoryPanel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnForestScene()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawnInActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TrySpawnInActiveScene();
        }

        private static void TrySpawnInActiveScene()
        {
            if (SceneManager.GetActiveScene().name != "FlorestaBicicleta") return;
            if (FindObjectOfType<FlorestaSceneManager>() != null) return;

            GameObject go = new GameObject("FlorestaSceneManager");
            go.AddComponent<FlorestaSceneManager>();
        }

        private void Start()
        {
            SetupCamera();
            EnsureEventSystem();
            BuildEnvironment();
            SpawnPlayer();
            SpawnAnimals();
            BuildHUD();
        }

        private void SetupCamera()
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.5f;
            mainCam.backgroundColor = new Color(0.46f, 0.78f, 0.99f, 1f);
            mainCam.transform.position = new Vector3(0f, 1.5f, -10f);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildEnvironment()
        {
            Transform camT = mainCam.transform;

            // 1. Céu Azul (segue a câmera)
            GameObject skyObj = new GameObject("CeuAzul");
            skyObj.transform.position = new Vector3(0f, 1.5f, 10f);
            skyObj.transform.localScale = new Vector3(120f, 1.8f, 1f);
            SpriteRenderer skySR = skyObj.AddComponent<SpriteRenderer>();
            skySR.sprite = FlorestaSpriteFactory.CreateSkyGradient();
            skySR.sortingOrder = -50;
            skyObj.AddComponent<ParallaxLayer>().Initialize(camT, 1f);

            // 2. Sol brilhante
            GameObject sunObj = new GameObject("Sol");
            sunObj.transform.position = new Vector3(7.5f, 5.0f, 9f);
            sunObj.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            SpriteRenderer sunSR = sunObj.AddComponent<SpriteRenderer>();
            sunSR.sprite = FlorestaSpriteFactory.CreateSunSprite();
            sunSR.sortingOrder = -45;
            sunObj.AddComponent<ParallaxLayer>().Initialize(camT, 0.96f);

            // 3. Nuvens flutuantes
            Sprite cloudSprite = FlorestaSpriteFactory.CreateCloudSprite();
            for (int i = 0; i < 8; i++)
            {
                GameObject cloud = new GameObject($"Nuvem_{i}");
                cloud.transform.position = new Vector3(-6f + i * 16f, 4.2f + (i % 3) * 0.8f, 8f);
                float scale = 0.85f + (i % 3) * 0.25f;
                cloud.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = cloud.AddComponent<SpriteRenderer>();
                sr.sprite = cloudSprite;
                sr.sortingOrder = -40;
                cloud.AddComponent<ParallaxLayer>().Initialize(camT, 0.85f, -0.18f);
            }

            // 4. Montanhas distantes (azuladas)
            Sprite farMtnSprite = FlorestaSpriteFactory.CreateMountainRangeSprite(
                new Color(0.42f, 0.58f, 0.78f, 1f),
                new Color(0.92f, 0.96f, 1f, 1f),
                seed: 3
            );
            for (int i = -1; i <= 6; i++)
            {
                GameObject mtn = new GameObject($"MontanhaDistante_{i}");
                mtn.transform.position = new Vector3(i * 10.6f, -1.6f, 6f);
                SpriteRenderer sr = mtn.AddComponent<SpriteRenderer>();
                sr.sprite = farMtnSprite;
                sr.sortingOrder = -30;
                mtn.AddComponent<ParallaxLayer>().Initialize(camT, 0.75f);
            }

            // 5. Montanhas médias (verde-floresta)
            Sprite midMtnSprite = FlorestaSpriteFactory.CreateMountainRangeSprite(
                new Color(0.22f, 0.48f, 0.36f, 1f),
                new Color(0.32f, 0.60f, 0.44f, 1f),
                seed: 9
            );
            for (int i = -1; i <= 10; i++)
            {
                GameObject mtn = new GameObject($"MontanhaMedia_{i}");
                mtn.transform.position = new Vector3(i * 10.6f, -2.0f, 5f);
                mtn.transform.localScale = new Vector3(1f, 0.78f, 1f);
                SpriteRenderer sr = mtn.AddComponent<SpriteRenderer>();
                sr.sprite = midMtnSprite;
                sr.sortingOrder = -25;
                mtn.AddComponent<ParallaxLayer>().Initialize(camT, 0.50f);
            }

            // 6. Floresta de Árvores (pinheiros e árvores frondosas atrás da pista)
            Sprite pineA = FlorestaSpriteFactory.CreatePineTreeSprite(0);
            Sprite pineB = FlorestaSpriteFactory.CreatePineTreeSprite(1);
            Sprite leafy = FlorestaSpriteFactory.CreateLeafyTreeSprite();

            for (int i = -2; i <= 42; i++)
            {
                float x = i * 3.1f + ((i % 2 == 0) ? 0.4f : -0.3f);
                GameObject tree = new GameObject($"Arvore_{i}");
                tree.transform.position = new Vector3(x, -1.95f, 2f);
                float s = 0.9f + (i % 4) * 0.12f;
                tree.transform.localScale = new Vector3(s, s, 1f);

                SpriteRenderer sr = tree.AddComponent<SpriteRenderer>();
                sr.sprite = (i % 3 == 0) ? leafy : ((i % 2 == 0) ? pineA : pineB);
                sr.sortingOrder = -10;
            }

            // 7. Pista contínua com grama e flores
            Sprite roadSprite = FlorestaSpriteFactory.CreateRoadTileSprite();
            for (int i = -3; i <= 34; i++)
            {
                GameObject road = new GameObject($"Pista_{i}");
                road.transform.position = new Vector3(i * 4f, -3.95f, 0f);
                SpriteRenderer sr = road.AddComponent<SpriteRenderer>();
                sr.sprite = roadSprite;
                sr.sortingOrder = 0;
            }
        }

        private void SpawnPlayer()
        {
            GameObject playerObj = new GameObject("MeninaBicicleta");
            playerObj.transform.position = new Vector3(0f, -2.85f, 0f);
            player = playerObj.AddComponent<BikeGirlController>();
            player.Setup(OnAnimalFed);
        }

        private void SpawnAnimals()
        {
            Sprite foodSprite = FlorestaSpriteFactory.CreateFoodIconSprite();
            heartSprite = FlorestaSpriteFactory.CreateHeartIconSprite();

            var animalConfigs = new (FlorestaSpriteFactory.AnimalKind kind, string name, float xPos)[]
            {
                (FlorestaSpriteFactory.AnimalKind.Esquilo, "Esquilo", 18f),
                (FlorestaSpriteFactory.AnimalKind.Coelho, "Coelho", 40f),
                (FlorestaSpriteFactory.AnimalKind.Gato, "Gato", 62f),
                (FlorestaSpriteFactory.AnimalKind.Lobo, "Lobo", 84f),
                (FlorestaSpriteFactory.AnimalKind.Quati, "Quati", 106f),
            };

            foreach (var cfg in animalConfigs)
            {
                GameObject animalObj = new GameObject($"Animal_{cfg.name}");
                animalObj.transform.position = new Vector3(cfg.xPos, -2.25f, 0f);
                ForestAnimal animal = animalObj.AddComponent<ForestAnimal>();
                animal.Setup(cfg.kind, cfg.name, foodSprite);
                animals.Add(animal);
            }
        }

        private void OnAnimalFed(ForestAnimal animal)
        {
            animal.Feed(heartSprite);
            fedCount++;
            UpdateHUD();

            if (promptText != null)
            {
                promptText.text = $"Você alimentou o {animal.animalName}! O cachorrinho adorou!";
            }

            if (fedCount >= animals.Count && victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
        }

        private void LateUpdate()
        {
            if (mainCam == null || player == null) return;

            Vector3 camPos = mainCam.transform.position;
            float targetX = Mathf.Max(2f, player.transform.position.x + 3.2f);
            camPos.x = Mathf.Lerp(camPos.x, targetX, 5f * Time.deltaTime);
            mainCam.transform.position = camPos;

            UpdateHUD();
        }

        // ─── HUD ─────────────────────────────────────────────────────

        private void BuildHUD()
        {
            GameObject canvasObj = new GameObject("FlorestaHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1152, 648);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Painel superior (maior para melhor leitura no celular)
            GameObject topBar = CreatePanel(canvasObj.transform, "TopBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(920f, 72f),
                new Color(0.10f, 0.22f, 0.14f, 0.84f));

            counterText = CreateTMP(topBar.transform, "CounterText", "Animais Alimentados: 0 / 5",
                30, new Vector2(-215f, 0f), new Vector2(450f, 54f), TextAlignmentOptions.Left);

            speedText = CreateTMP(topBar.transform, "SpeedText", "Velocidade: Parada",
                28, new Vector2(225f, 0f), new Vector2(420f, 54f), TextAlignmentOptions.Right);

            // Aviso central de instrução / proximidade
            promptText = CreateTMP(canvasObj.transform, "PromptText",
                "Use D / Setas para pedalar devagar, Shift+D (ou W) para rápido! Pare perto dos animais para alimentar.",
                26, new Vector2(0f, -110f), new Vector2(1040f, 54f), TextAlignmentOptions.Center);
            RectTransform promptRT = promptText.GetComponent<RectTransform>();
            promptRT.anchorMin = new Vector2(0.5f, 1f);
            promptRT.anchorMax = new Vector2(0.5f, 1f);
            promptText.color = new Color(1f, 0.98f, 0.78f, 1f);

            // Botões na tela maiores para celular/toque
            CreateHoldButton(canvasObj.transform, "BtnFrear", "< Frear",
                new Vector2(115f, 72f), new Vector2(170f, 78f),
                new Color(0.85f, 0.36f, 0.36f, 0.92f),
                () => player.SetUIReverse(true), () => player.SetUIReverse(false));

            CreateHoldButton(canvasObj.transform, "BtnDevagar", "Devagar >",
                new Vector2(310f, 72f), new Vector2(190f, 78f),
                new Color(0.26f, 0.68f, 0.42f, 0.92f),
                () => player.SetUISlow(true), () => player.SetUISlow(false));

            CreateHoldButton(canvasObj.transform, "BtnRapido", "Rápido >>",
                new Vector2(515f, 72f), new Vector2(195f, 78f),
                new Color(0.95f, 0.62f, 0.18f, 0.92f),
                () => player.SetUIFast(true), () => player.SetUIFast(false));

            feedButtonObj = CreateClickButton(canvasObj.transform, "BtnAlimentar", "Alimentar! (Espaço)",
                new Vector2(960f, 72f), new Vector2(290f, 84f),
                new Color(0.94f, 0.34f, 0.62f, 0.96f),
                () => player.TriggerFeed());
            feedButtonObj.SetActive(false);

            // Painel de Vitória
            victoryPanel = CreatePanel(canvasObj.transform, "VictoryPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560f, 280f),
                new Color(0.10f, 0.25f, 0.16f, 0.95f));

            CreateTMP(victoryPanel.transform, "VictoryTitle", "Parabéns!",
                38, new Vector2(0f, 75f), new Vector2(500f, 55f), TextAlignmentOptions.Center);

            CreateTMP(victoryPanel.transform, "VictorySub",
                "Todos os animais da floresta (Esquilo, Coelho, Gato, Lobo e Quati) foram alimentados!",
                22, new Vector2(0f, 5f), new Vector2(500f, 80f), TextAlignmentOptions.Center);

            GameObject replayBtn = CreateClickButton(victoryPanel.transform, "BtnReplay", "Pedalar Novamente",
                Vector2.zero, new Vector2(240f, 54f),
                new Color(0.28f, 0.74f, 0.44f, 1f),
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            RectTransform repRT = replayBtn.GetComponent<RectTransform>();
            repRT.anchorMin = new Vector2(0.5f, 0.5f);
            repRT.anchorMax = new Vector2(0.5f, 0.5f);
            repRT.anchoredPosition = new Vector2(0f, -85f);

            victoryPanel.SetActive(false);
        }

        private void UpdateHUD()
        {
            if (counterText != null)
                counterText.text = $"Animais Alimentados: {fedCount} / {animals.Count}";

            if (speedText != null && player != null)
                speedText.text = $"Velocidade: {player.speedStateLabel}";

            if (player != null)
            {
                bool nearHungry = player.nearbyHungryAnimal != null && !player.nearbyHungryAnimal.isFed;
                if (feedButtonObj != null)
                    feedButtonObj.SetActive(nearHungry);

                if (nearHungry && promptText != null)
                {
                    promptText.text = $"Pare perto do {player.nearbyHungryAnimal.animalName} e aperte ESPAÇO (ou Alimentar)!";
                }
            }
        }

        // ─── UI Helpers ──────────────────────────────────────────────

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private void CreateHoldButton(Transform parent, string name, string label, Vector2 screenPos, Vector2 size, Color color, System.Action onDown, System.Action onUp)
        {
            GameObject btnObj = CreatePanel(parent, name, Vector2.zero, Vector2.zero, screenPos, size, color);
            CreateTMP(btnObj.transform, "Label", label, 26, Vector2.zero, size, TextAlignmentOptions.Center);

            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener(_ => onDown());
            trigger.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener(_ => onUp());
            trigger.triggers.Add(upEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => onUp());
            trigger.triggers.Add(exitEntry);
        }

        private GameObject CreateClickButton(Transform parent, string name, string label, Vector2 screenPos, Vector2 size, Color color, System.Action onClick)
        {
            GameObject btnObj = CreatePanel(parent, name, Vector2.zero, Vector2.zero, screenPos, size, color);
            CreateTMP(btnObj.transform, "Label", label, 26, Vector2.zero, size, TextAlignmentOptions.Center);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btnObj;
        }
    }
}
