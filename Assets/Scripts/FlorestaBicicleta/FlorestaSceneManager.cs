using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Cena Lateral Ilustrada Completa da Floresta de Bicicleta:
    /// - Céu azul, sol, nuvens, montanhas nevadas, floresta e pista ilustrada;
    /// - Subidas e Descidas, bancos de praça, vasos de plantas, flores e Colunas de Água;
    /// - 7 animais com vozes próprias (latido, miado, uivo, esquilo, coelho, quati, raposa, cervo);
    /// - Cardápio completo escolhido pelos animais + Barracas da Cozinha (preparar Sanduíche e Hot Dog);
    /// - Arremesso de comida + animação de mastigar + chuva de corações;
    /// - Atropelar em alta velocidade perde pontos e faz chorar com lágrimas;
    /// - Segredo final: Esquilo saciado cava a terra e, com ajuda da menina, encontram diamantes!
    /// </summary>
    public class FlorestaSceneManager : MonoBehaviour
    {
        private Camera mainCam;
        private BikeGirlController player;
        private SideDiamondDigSite digSite;
        private ForestAnimal squirrelAnimal;

        private readonly List<ForestAnimal> animals = new List<ForestAnimal>();
        private readonly Dictionary<FoodType, Sprite> foodSprites = new Dictionary<FoodType, Sprite>();

        // HUD
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI squirrelText;
        private TextMeshProUGUI slopeText;
        private TextMeshProUGUI basketText;
        private TextMeshProUGUI nearbyOrderText;
        private TextMeshProUGUI messageText;
        private Slider waterSlider;
        private TextMeshProUGUI waterLabel;
        private Image sunsetOverlay;

        // Painel da Cozinha
        private GameObject kitchenPanel;
        private TextMeshProUGUI kitchenPrepStatus;
        private bool isPreparingFood = false;

        // Botão de Cavar com o Esquilo
        private GameObject digButtonObj;
        private TextMeshProUGUI digButtonLabel;

        // Tela de Vitória
        private GameObject victoryPanel;
        private TextMeshProUGUI victoryScoreText;

        private float messageExpireTime = 0f;
        private float dayCycleTime = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnForestScene()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawnInActiveScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawnInActiveScene();

        private static void TrySpawnInActiveScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "FlorestaBicicleta" && sceneName != "FlorestaBicicletaTopDown") return;
            if (FindObjectOfType<FlorestaSceneManager>() != null) return;

            GameObject go = new GameObject("FlorestaSceneManager");
            go.AddComponent<FlorestaSceneManager>();
        }

        private void Start()
        {
            gameObject.AddComponent<FlorestaSoundFX>();
            CacheFoodSprites();
            SetupCamera();
            EnsureEventSystem();
            BuildEnvironment();
            BuildRoadFeatures();
            SpawnAnimals();
            SpawnPlayer();
            BuildHUD();

            ShowMessage("Pedale pela floresta! Arremesse o lanche que cada animal pedir (ESPAÇO) e cuide da sede do cachorrinho!", 6f);
        }

        private void CacheFoodSprites()
        {
            foreach (FoodType ft in System.Enum.GetValues(typeof(FoodType)))
                foodSprites[ft] = ParkSpriteFactory.CreateFoodSprite(ft);
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
            mainCam.transform.position = new Vector3(2f, 0.65f, -10f);
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
            float camHeight = mainCam != null ? mainCam.orthographicSize * 2.08f : 11.4f;
            Sprite bgSprite = FlorestaSpriteFactory.LoadForestBackgroundSprite(camHeight);

            if (bgSprite != null)
            {
                float tileWidth = bgSprite.bounds.size.x;
                for (int i = -1; i <= 10; i++)
                {
                    GameObject bgObj = new GameObject($"CenarioFloresta_{i}");
                    bgObj.transform.position = new Vector3(i * tileWidth, 1.5f, 5f);
                    SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.flipX = (Mathf.Abs(i) % 2 == 1);
                    sr.sortingOrder = -20;
                }
            }
        }

        private void BuildRoadFeatures()
        {
            // Subidas e Descidas sinalizadas ao longo da estrada das montanhas
            CreateSlope(SlopeZone.SlopeType.Subida,  28f, 9f);
            CreateSlope(SlopeZone.SlopeType.Descida, 54f, 9f);
            CreateSlope(SlopeZone.SlopeType.Subida,  92f, 9f);
            CreateSlope(SlopeZone.SlopeType.Descida, 124f, 9f);

            // Rampas / Lombadas de salto com Estrelas Aéreas flutuando acima!
            float[] rampXs = { 34f, 75f, 116f, 146f };
            foreach (float rx in rampXs)
            {
                GameObject rObj = new GameObject($"RampaSalto_{rx}");
                rObj.transform.position = new Vector3(rx, -3.55f, 0f);
                rObj.AddComponent<JumpRamp>().Setup();

                // 3 estrelas douradas formando o arco do salto no ar
                for (int s = 0; s < 3; s++)
                {
                    GameObject stObj = new GameObject($"EstrelaAr_{rx}_{s}");
                    float sx = rx + 2.2f + s * 1.6f;
                    float sy = -0.9f + (s == 1 ? 0.55f : 0f);
                    stObj.transform.position = new Vector3(sx, sy, 0f);
                    stObj.AddComponent<AerialCollectible>().Setup(50);
                }
            }

            // Obstáculos naturais na trilha (Pedras, Troncos e Raízes) com espaço seguro para saltar
            var obstacleSpots = new (TrailObstacle.ObstacleKind kind, float x)[]
            {
                (TrailObstacle.ObstacleKind.Pedra,  47f),
                (TrailObstacle.ObstacleKind.Raiz,   87f),
                (TrailObstacle.ObstacleKind.Tronco, 100f),
                (TrailObstacle.ObstacleKind.Pedra,  138f),
            };
            foreach (var ob in obstacleSpots)
            {
                GameObject obObj = new GameObject($"Obstaculo_{ob.kind}_{ob.x}");
                obObj.transform.position = new Vector3(ob.x, -3.55f, 0f);
                obObj.AddComponent<TrailObstacle>().Setup(ob.kind);
            }

            // 3 Barracas da Cozinha ilustradas ao longo da estrada
            float[] kitchenXs = { 8f, 68f, 128f };
            foreach (float kx in kitchenXs)
            {
                GameObject kObj = new GameObject($"BarracaCozinha_{kx}");
                kObj.transform.position = new Vector3(kx, -2.55f, 0f);
                kObj.AddComponent<KitchenStation>().Setup();
            }

            // 4 Colunas de Água / Bebedouros ilustrados para o cachorrinho
            float[] waterXs = { 24f, 62f, 102f, 142f };
            foreach (float wx in waterXs)
            {
                GameObject wObj = new GameObject($"ColunaAgua_{wx}");
                wObj.transform.position = new Vector3(wx, -2.55f, 0f);
                wObj.AddComponent<WaterColumn>().Setup();
            }

            // Bancos de praça ilustrados com vaso de flores ao longo da pista
            Texture2D benchTex = Resources.Load<Texture2D>("Floresta/banco_praca");
            Sprite bench = benchTex != null
                ? Sprite.Create(benchTex, new Rect(0, 0, benchTex.width, benchTex.height), new Vector2(0.5f, 0.05f), benchTex.height / 2.2f)
                : ParkSpriteFactory.CreateParkBenchSprite();

            for (int x = 13; x <= 154; x += 22)
            {
                CreateProp(bench, new Vector3(x, -2.55f, 0f), 1f, 3);
            }

            // Local Secreto de Terra Fofa para o Esquilo cavar Diamantes!
            GameObject digObj = new GameObject("LocalDiamantesEsquilo");
            digObj.transform.position = new Vector3(15f, -2.45f, 0f);
            digSite = digObj.AddComponent<SideDiamondDigSite>();
            digSite.Setup(OnDiamondsVictory);
        }

        private void CreateSlope(SlopeZone.SlopeType type, float xCenter, float length)
        {
            GameObject obj = new GameObject(type == SlopeZone.SlopeType.Subida ? "ZonaSubida" : "ZonaDescida");
            obj.transform.position = new Vector3(xCenter, -3.15f, 0f);
            // Rotaciona a seta para apontar na direção da pista (+X subida / descida)
            float angle = type == SlopeZone.SlopeType.Subida ? -72f : -108f;
            obj.AddComponent<SlopeZone>().Setup(type, new Vector2(length, 2.2f), angle);
        }

        private void CreateProp(Sprite sp, Vector3 pos, float scale, int sortOrder)
        {
            GameObject go = new GameObject("PropFloresta");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;
        }

        private void SpawnAnimals()
        {
            Sprite happySp = ParkSpriteFactory.CreateEmotionIcon("happy");
            Sprite crySp = ParkSpriteFactory.CreateEmotionIcon("cry");
            Sprite tearSp = ParkSpriteFactory.CreateTearDropSprite();
            Sprite[] heartParticles =
            {
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(0.98f, 0.25f, 0.48f, 1f)),
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(1f, 0.52f, 0.78f, 1f)),
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(1f, 0.84f, 0.24f, 1f))
            };

            var configs = new (ParkSpriteFactory.AnimalKind kind, string name, int meals, float xPos)[]
            {
                (ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 6, 18f),
                (ParkSpriteFactory.AnimalKind.Coelho,  "Coelho",  2, 40f),
                (ParkSpriteFactory.AnimalKind.Gato,    "Gato",    2, 60f),
                (ParkSpriteFactory.AnimalKind.Lobo,    "Lobo",    4, 82f),
                (ParkSpriteFactory.AnimalKind.Quati,   "Quati",   4, 108f),
                (ParkSpriteFactory.AnimalKind.Raposa,  "Raposa",  3, 134f),
                (ParkSpriteFactory.AnimalKind.Cervo,   "Cervo",   5, 152f),
            };

            foreach (var c in configs)
            {
                GameObject obj = new GameObject($"Animal_{c.name}");
                obj.transform.position = new Vector3(c.xPos, -2.55f, 0f);
                ForestAnimal a = obj.AddComponent<ForestAnimal>();
                a.Setup(c.kind, c.name, c.meals, foodSprites, happySp, crySp, tearSp, heartParticles);
                animals.Add(a);
                if (a.isSquirrelSpecial) squirrelAnimal = a;
            }
        }

        private void SpawnPlayer()
        {
            GameObject playerObj = new GameObject("MeninaBicicleta");
            playerObj.transform.position = new Vector3(0f, -3.55f, 0f);
            player = playerObj.AddComponent<BikeGirlController>();
            player.Setup(foodSprites, digSite, OnFoodDelivered, OnAnimalRunOver, OnWaterRefilled, msg => ShowMessage(msg, 3.5f));
        }

        // ─── Eventos de Gameplay ─────────────────────────────────────

        private void OnFoodDelivered(ForestAnimal animal, FoodType given, ForestAnimal.FeedResult res)
        {
            string givenName = FoodInfo.GetDisplayName(given);
            string wantedName = FoodInfo.GetDisplayName(animal.wantedFood);

            switch (res)
            {
                case ForestAnimal.FeedResult.WrongFood:
                    ShowMessage($"O {animal.animalName} queria {wantedName}, não {givenName}! Passe na Cozinha!", 3.5f, new Color(1f, 0.80f, 0.45f, 1f));
                    break;
                case ForestAnimal.FeedResult.AteNeedsMore:
                    ShowMessage($"Nham! O {animal.animalName} amou ({animal.mealsEaten}/{animal.requiredMeals}) e soltou corações! +80 pts", 3.0f, new Color(0.75f, 1f, 0.78f, 1f));
                    break;
                case ForestAnimal.FeedResult.FullySatisfied:
                    ShowMessage($"O {animal.animalName} está SUPER SACIADO e cheio de corações! +150 pts", 3.5f, new Color(0.68f, 1f, 0.75f, 1f));
                    break;
                case ForestAnimal.FeedResult.TriggeredSquirrelSecret:
                    ShowMessage("SEGREDO DO ESQUILO! Ele começou a CAVAR A TERRA! Vá até ele e ajude a cavar para achar DIAMANTES!", 7.0f, new Color(0.60f, 0.96f, 1f, 1f));
                    digSite.ActivateDigging(squirrelAnimal);
                    break;
                case ForestAnimal.FeedResult.AlreadyFull:
                    ShowMessage($"O {animal.animalName} já está cheio, mas adorou o carinho! +20 pts", 2.2f, new Color(1f, 0.85f, 0.95f, 1f));
                    break;
            }
        }

        private void OnAnimalRunOver(ForestAnimal animal)
        {
            ShowMessage($"Ai! Você passou rápido e atropelou o {animal.animalName}! Ele está chorando... (-50 pts)", 3.5f, new Color(1f, 0.60f, 0.60f, 1f));
        }

        private void OnWaterRefilled()
        {
            ShowMessage("Au-au! O cachorrinho bebeu água fresquinha na coluna! Sede 100%!", 2.8f, new Color(0.65f, 0.92f, 1f, 1f));
        }

        private void OnDiamondsVictory()
        {
            player.score += 1000;
            if (victoryScoreText != null)
                victoryScoreText.text = $"Pontuação Final: {player.score} pontos!\nVocês cavaram fundo e encontraram vários DIAMANTES brilhantes!";
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }

        private void ShowMessage(string msg, float duration, Color? color = null)
        {
            if (messageText == null) return;
            messageText.text = msg;
            messageText.color = color ?? new Color(1f, 0.98f, 0.78f, 1f);
            messageExpireTime = Time.time + duration;
        }

        private void LateUpdate()
        {
            if (player == null) return;

            if (mainCam != null)
            {
                Vector3 camPos = mainCam.transform.position;
                float targetX = Mathf.Max(2f, player.transform.position.x + 2.8f);
                camPos.x = Mathf.Lerp(camPos.x, targetX, 5.5f * Time.deltaTime);
                mainCam.transform.position = camPos;
            }

            // Ciclo suave Dia -> Tarde Dourada
            dayCycleTime += Time.deltaTime;
            if (sunsetOverlay != null)
            {
                float golden = (Mathf.Sin(dayCycleTime * 0.035f - Mathf.PI * 0.5f) + 1f) * 0.5f;
                sunsetOverlay.color = new Color(1f, 0.60f, 0.24f, golden * 0.14f);
            }

            // Emoção e voz dos animais ao se aproximar
            ForestAnimal nearest = null;
            float bestD = 9f;
            foreach (var a in animals)
            {
                float d = Mathf.Abs(player.transform.position.x - a.transform.position.x);
                a.OnPlayerApproaching(d < 6.5f);
                if (d < bestD)
                {
                    bestD = d;
                    nearest = a;
                }
            }

            UpdateHUD(nearest);
        }

        // ─── Cozinha / Preparo de Lanches ────────────────────────────

        private void OnClickKitchenFood(FoodType type)
        {
            if (isPreparingFood || player == null) return;

            if (FoodInfo.NeedsPreparation(type))
            {
                StartCoroutine(PrepareFoodRoutine(type));
            }
            else
            {
                player.AddFoodToBasket(type);
                FlorestaSoundFX.Instance?.PlayHappy();
                if (kitchenPrepStatus != null)
                    kitchenPrepStatus.text = $"Pegou {FoodInfo.GetDisplayName(type)} na cesta!";
            }
        }

        private IEnumerator PrepareFoodRoutine(FoodType type)
        {
            isPreparingFood = true;
            string name = FoodInfo.GetDisplayName(type);
            float dur = 1.5f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                int pct = Mathf.RoundToInt((elapsed / dur) * 100f);
                if (kitchenPrepStatus != null)
                    kitchenPrepStatus.text = $"Preparando {name} quentinho... {pct}%";
                yield return null;
            }
            player.AddFoodToBasket(type);
            FlorestaSoundFX.Instance?.PlayHappy();
            if (kitchenPrepStatus != null)
                kitchenPrepStatus.text = $"{name} pronto na cesta!";
            isPreparingFood = false;
        }

        // ─── HUD Limpa (Topo e Bordas — Nunca Tampa a Menina!) ──────

        private void BuildHUD()
        {
            GameObject canvasObj = new GameObject("FlorestaHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Filtro de pôr do sol suave
            GameObject sunObj = new GameObject("SunsetTint");
            sunObj.transform.SetParent(canvasObj.transform, false);
            RectTransform sunRT = sunObj.AddComponent<RectTransform>();
            sunRT.anchorMin = Vector2.zero; sunRT.anchorMax = Vector2.one; sunRT.sizeDelta = Vector2.zero;
            sunsetOverlay = sunObj.AddComponent<Image>();
            sunsetOverlay.color = Color.clear;
            sunsetOverlay.raycastTarget = false;

            // Barra Superior
            GameObject topBar = CreatePanel(canvasObj.transform, "TopBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(1220f, 60f),
                new Color(0.09f, 0.22f, 0.14f, 0.88f));

            scoreText = CreateTMP(topBar.transform, "ScoreText", "Pontos: 0",
                25, new Vector2(-460f, 0f), new Vector2(250f, 44f), TextAlignmentOptions.Left);

            squirrelText = CreateTMP(topBar.transform, "SquirrelText", "Esquilo: 0 / 6",
                25, new Vector2(-155f, 0f), new Vector2(290f, 44f), TextAlignmentOptions.Center);
            squirrelText.color = new Color(1f, 0.88f, 0.45f, 1f);

            slopeText = CreateTMP(topBar.transform, "SlopeText", "Plano",
                23, new Vector2(145f, 0f), new Vector2(260f, 44f), TextAlignmentOptions.Center);

            waterLabel = CreateTMP(topBar.transform, "WaterLabel", "Água: 100%",
                19, new Vector2(455f, 13f), new Vector2(230f, 26f), TextAlignmentOptions.Center);
            waterLabel.color = new Color(0.60f, 0.90f, 1f, 1f);

            GameObject sliderObj = CreatePanel(topBar.transform, "WaterSlider",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(455f, -13f), new Vector2(210f, 16f),
                new Color(0.15f, 0.25f, 0.35f, 1f));
            waterSlider = sliderObj.AddComponent<Slider>();
            waterSlider.minValue = 0f; waterSlider.maxValue = 100f; waterSlider.value = 100f;

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRT = fill.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one; fRT.sizeDelta = Vector2.zero;
            fill.AddComponent<Image>().color = new Color(0.20f, 0.78f, 1f, 1f);
            waterSlider.fillRect = fRT;

            // Sub-barra: Cesta + Pedido do animal próximo
            GameObject subBar = CreatePanel(canvasObj.transform, "SubBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -84f), new Vector2(1220f, 36f),
                new Color(0.12f, 0.16f, 0.22f, 0.78f));

            basketText = CreateTMP(subBar.transform, "BasketText", "Cesta: ...",
                19, new Vector2(-280f, 0f), new Vector2(620f, 32f), TextAlignmentOptions.Left);
            basketText.color = new Color(1f, 0.95f, 0.80f, 1f);

            nearbyOrderText = CreateTMP(subBar.transform, "NearbyOrder", "",
                19, new Vector2(310f, 0f), new Vector2(560f, 32f), TextAlignmentOptions.Right);
            nearbyOrderText.color = new Color(0.80f, 1f, 0.85f, 1f);

            // Mensagem de feedback
            messageText = CreateTMP(canvasObj.transform, "MessageText", "",
                23, new Vector2(0f, -124f), new Vector2(1180f, 40f), TextAlignmentOptions.Center);
            RectTransform msgRT = messageText.GetComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0.5f, 1f);
            msgRT.anchorMax = new Vector2(0.5f, 1f);

            // Painel da Cozinha (lateral esquerda quando perto de uma barraca)
            BuildKitchenPanel(canvasObj.transform);

            // Botão especial de Cavar com o Esquilo
            digButtonObj = CreateClickButton(canvasObj.transform, "BtnCavar", "CAVAR JUNTO! (Espaço)",
                new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(340f, 64f),
                new Color(0.16f, 0.72f, 0.96f, 0.96f), 25,
                () => digSite.HelpDig());
            digButtonLabel = digButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            digButtonObj.SetActive(false);

            // Controles compactos na borda inferior (sem tampar a menina!)
            CreateHoldButton(canvasObj.transform, "BtnVoltar", "< Voltar",
                Vector2.zero, new Vector2(88f, 36f), new Vector2(146f, 54f),
                new Color(0.85f, 0.36f, 0.36f, 0.82f), 22,
                () => player.SetUIReverse(true), () => player.SetUIReverse(false));

            CreateHoldButton(canvasObj.transform, "BtnDevagar", "Devagar >",
                Vector2.zero, new Vector2(248f, 36f), new Vector2(158f, 54f),
                new Color(0.24f, 0.66f, 0.40f, 0.84f), 22,
                () => player.SetUISlow(true), () => player.SetUISlow(false));

            CreateHoldButton(canvasObj.transform, "BtnRapido", "Rápido >>",
                Vector2.zero, new Vector2(418f, 36f), new Vector2(162f, 54f),
                new Color(0.94f, 0.58f, 0.16f, 0.86f), 22,
                () => player.SetUIFast(true), () => player.SetUIFast(false));

            CreateClickButton(canvasObj.transform, "BtnPular", "PULAR ▲",
                new Vector2(1f, 0f), new Vector2(-345f, 38f), new Vector2(145f, 58f),
                new Color(0.22f, 0.62f, 0.96f, 0.90f), 22,
                () => player.TriggerJump());

            CreateClickButton(canvasObj.transform, "BtnComida", "JOGAR COMIDA",
                new Vector2(1f, 0f), new Vector2(-135f, 38f), new Vector2(238f, 58f),
                new Color(0.95f, 0.34f, 0.62f, 0.92f), 23,
                () => player.ThrowFood());

            // Painel de Vitória dos Diamantes
            victoryPanel = CreatePanel(canvasObj.transform, "VictoryPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 340f),
                new Color(0.08f, 0.22f, 0.30f, 0.96f));

            CreateTMP(victoryPanel.transform, "VTitle", "DIAMANTES ENCONTRADOS!",
                38, new Vector2(0f, 102f), new Vector2(620f, 56f), TextAlignmentOptions.Center)
                .color = new Color(0.52f, 0.95f, 1f, 1f);

            victoryScoreText = CreateTMP(victoryPanel.transform, "VSub",
                "Você e o Esquilo cavaram fundo e encontraram vários diamantes brilhantes!",
                24, new Vector2(0f, 18f), new Vector2(600f, 96f), TextAlignmentOptions.Center);

            CreateClickButton(victoryPanel.transform, "VReplay", "Jogar Novamente",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -105f), new Vector2(280f, 64f),
                new Color(0.28f, 0.78f, 0.46f, 1f), 26,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

            victoryPanel.SetActive(false);
        }

        private void BuildKitchenPanel(Transform canvasT)
        {
            kitchenPanel = CreatePanel(canvasT, "KitchenPanel",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(185f, 15f), new Vector2(330f, 385f),
                new Color(0.24f, 0.12f, 0.22f, 0.92f));

            CreateTMP(kitchenPanel.transform, "KTitle", "COZINHA DA FLORESTA",
                22, new Vector2(0f, 162f), new Vector2(310f, 34f), TextAlignmentOptions.Center)
                .color = new Color(1f, 0.65f, 0.82f, 1f);

            FoodType[] readyFoods = { FoodType.Refrigerante, FoodType.Suco, FoodType.Coxinha, FoodType.Doces, FoodType.Pipoca };
            for (int i = 0; i < readyFoods.Length; i++)
            {
                FoodType ft = readyFoods[i];
                float y = 118f - i * 43f;
                CreateClickButton(kitchenPanel.transform, $"Btn_{ft}", $"+ {FoodInfo.GetDisplayName(ft)}",
                    new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(290f, 37f),
                    new Color(0.24f, 0.58f, 0.42f, 0.95f), 20,
                    () => OnClickKitchenFood(ft));
            }

            CreateClickButton(kitchenPanel.transform, "BtnPrepSanduiche", "Preparar Sanduíche",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -106f), new Vector2(290f, 41f),
                new Color(0.92f, 0.54f, 0.16f, 0.96f), 20,
                () => OnClickKitchenFood(FoodType.Sanduiche));

            CreateClickButton(kitchenPanel.transform, "BtnPrepHotDog", "Preparar Hot Dog",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -153f), new Vector2(290f, 41f),
                new Color(0.90f, 0.30f, 0.48f, 0.96f), 20,
                () => OnClickKitchenFood(FoodType.HotDog));

            kitchenPrepStatus = CreateTMP(kitchenPanel.transform, "PrepStatus", "Escolha ou prepare!",
                17, new Vector2(0f, -182f), new Vector2(310f, 24f), TextAlignmentOptions.Center);
            kitchenPrepStatus.color = new Color(1f, 0.94f, 0.60f, 1f);

            kitchenPanel.SetActive(false);
        }

        private void UpdateHUD(ForestAnimal nearest)
        {
            if (scoreText != null)
            {
                string comboTxt = player.happyCombo >= 3 ? $" (x{player.happyCombo}!)" : "";
                scoreText.text = $"Pontos: {player.score}{comboTxt}";
            }

            if (squirrelText != null && squirrelAnimal != null)
            {
                squirrelText.text = digSite.isActive
                    ? $"Cavando: {Mathf.RoundToInt(digSite.digProgress)}%"
                    : $"Esquilo: {squirrelAnimal.mealsEaten} / {squirrelAnimal.requiredMeals}";
            }

            if (slopeText != null)
            {
                int meters = Mathf.RoundToInt(player.distanceMeters);
                slopeText.text = $"{player.currentSlopeLabel} | {meters}m";
            }
            if (basketText != null) basketText.text = player.GetBasketSummary();

            if (nearbyOrderText != null)
            {
                if (nearest != null)
                {
                    if (nearest.emotion == ForestAnimal.EmotionState.Saciado)
                        nearbyOrderText.text = $"{nearest.animalName}: Saciado e feliz!";
                    else if (nearest.emotion == ForestAnimal.EmotionState.Cavando)
                        nearbyOrderText.text = "Esquilo está cavando! Ajude a achar diamantes!";
                    else
                    {
                        string foodName = FoodInfo.GetDisplayName(nearest.wantedFood);
                        string prepTag = FoodInfo.NeedsPreparation(nearest.wantedFood) ? " (Preparar!)" : "";
                        nearbyOrderText.text = $"{nearest.animalName} quer: {foodName}{prepTag} ({nearest.mealsEaten}/{nearest.requiredMeals})";
                    }
                }
                else
                {
                    nearbyOrderText.text = "Aproxime-se de um animal para ver o pedido";
                }
            }

            if (waterSlider != null) waterSlider.value = player.waterLevel;
            if (waterLabel != null)
            {
                int wPct = Mathf.RoundToInt(player.waterLevel);
                waterLabel.text = wPct < 25 ? $"SEDE! {wPct}%" : $"Água: {wPct}%";
                waterLabel.color = wPct < 25 ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0.60f, 0.90f, 1f, 1f);
            }

            if (kitchenPanel != null) kitchenPanel.SetActive(player.isNearKitchen);

            if (digButtonObj != null && digSite != null)
            {
                bool showDig = digSite.isActive && digSite.playerNearby && !digSite.isCompleted;
                digButtonObj.SetActive(showDig);
                if (showDig && digButtonLabel != null)
                    digButtonLabel.text = $"CAVAR JUNTO! ({Mathf.RoundToInt(digSite.digProgress)}%)";
            }

            if (player.waterLevel < 25f && Time.time > messageExpireTime)
            {
                ShowMessage("O cachorrinho está com sede! Passe em uma Coluna de Água!", 2.5f, new Color(0.65f, 0.92f, 1f, 1f));
            }
            else if (messageText != null && Time.time > messageExpireTime && !string.IsNullOrEmpty(messageText.text))
            {
                messageText.text = "";
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
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private void CreateHoldButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, System.Action onDown, System.Action onUp)
        {
            GameObject btnObj = CreatePanel(parent, name, anchor, anchor, pos, size, color);
            CreateTMP(btnObj.transform, "Label", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center);

            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            var d = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            d.callback.AddListener(_ => onDown());
            trigger.triggers.Add(d);

            var u = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            u.callback.AddListener(_ => onUp());
            trigger.triggers.Add(u);

            var e = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            e.callback.AddListener(_ => onUp());
            trigger.triggers.Add(e);
        }

        private GameObject CreateClickButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, System.Action onClick)
        {
            GameObject btnObj = CreatePanel(parent, name, anchor, anchor, pos, size, color);
            CreateTMP(btnObj.transform, "Label", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btnObj;
        }
    }
}
