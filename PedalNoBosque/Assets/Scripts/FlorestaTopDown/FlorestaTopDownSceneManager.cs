using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace FlorestaTopDown
{
    /// <summary>
    /// Gerencia toda a aventura Top-Down no Parque da Floresta:
    /// - 7 animais com apetites variados e pedidos próprios (Refrigerante, Suco, Coxinha, Doces, Pipoca, Sanduíche, Hot Dog);
    /// - Barraca da Cozinha para pegar lanches prontos e preparar Sanduíche / Hot Dog;
    /// - Animação de mastigar + chuva de corações na cabeça dos animais;
    /// - Segredo final do Esquilo cavando a terra + menina ajudando a achar diamantes;
    /// - Subidas, Descidas, Colunas de Água para o cachorrinho, ciclo suave de Dia -> Tarde Dourada.
    /// </summary>
    public class FlorestaTopDownSceneManager : MonoBehaviour
    {
        private Camera mainCam;
        private TopDownBikeController player;
        private KitchenStation kitchenStation;
        private DiamondDigSite digSite;
        private ParkAnimal squirrelAnimal;

        private readonly List<ParkAnimal> animals = new List<ParkAnimal>();
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

        // Painel da Cozinha (abre automaticamente perto da barraca)
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
        private static void AutoBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TrySpawn();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TrySpawn();

        private static void TrySpawn()
        {
            // FlorestaBicicletaTopDown agora usa a versão lateral ilustrada (FlorestaSceneManager)
            return;
        }

        private void Start()
        {
            gameObject.AddComponent<ParkSoundFX>();
            CacheFoodSprites();
            SetupCamera();
            EnsureEventSystem();
            BuildParkGroundAndTrack();
            BuildSlopesWaterKitchenAndDigSite();
            BuildParkProps();
            SpawnAnimals();
            SpawnPlayer();
            BuildHUD();

            ShowMessage("Alimente os animais com o que eles pedirem! Passe na Barraca da Cozinha para pegar ou preparar lanches!", 6f);
        }

        private void CacheFoodSprites()
        {
            foreach (FoodType ft in System.Enum.GetValues(typeof(FoodType)))
            {
                foodSprites[ft] = ParkSpriteFactory.CreateFoodSprite(ft);
            }
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
            mainCam.orthographicSize = 7.2f;
            mainCam.backgroundColor = new Color(0.35f, 0.68f, 0.30f, 1f);
            mainCam.transform.position = new Vector3(0f, -14f, -10f);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildParkGroundAndTrack()
        {
            Sprite grassSprite = ParkSpriteFactory.CreateGrassTileSprite();
            for (int x = -8; x <= 8; x++)
            {
                for (int y = -8; y <= 8; y++)
                {
                    GameObject g = new GameObject($"Grama_{x}_{y}");
                    g.transform.position = new Vector3(x * 4f, y * 4f, 2f);
                    SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
                    sr.sprite = grassSprite;
                    sr.sortingOrder = -20;
                }
            }

            Sprite trackSprite = ParkSpriteFactory.CreateTrackTileSprite();
            for (int i = -4; i <= 4; i++)
            {
                CreateTrackTile(trackSprite, new Vector3(-14f, i * 3.5f, 0f), 0f);
                CreateTrackTile(trackSprite, new Vector3(14f, i * 3.5f, 0f), 0f);
            }
            for (int i = -3; i <= 3; i++)
            {
                CreateTrackTile(trackSprite, new Vector3(i * 3.5f, -14f, 0f), 90f);
                CreateTrackTile(trackSprite, new Vector3(i * 3.5f, 14f, 0f), 90f);
            }
            for (int i = -3; i <= 3; i++)
            {
                CreateTrackTile(trackSprite, new Vector3(0f, i * 3.5f, 0f), 0f);
            }
        }

        private void CreateTrackTile(Sprite sp, Vector3 pos, float rotZ)
        {
            GameObject t = new GameObject("PistaTile");
            t.transform.position = pos;
            t.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
            SpriteRenderer sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = -5;
        }

        private void BuildSlopesWaterKitchenAndDigSite()
        {
            // Subidas e Descidas
            CreateSlope(SlopeZone.SlopeType.Subida,  new Vector3(14f, -3f, 0f),  new Vector2(3.6f, 6.5f), 0f);
            CreateSlope(SlopeZone.SlopeType.Descida, new Vector3(14f, 6f, 0f),   new Vector2(3.6f, 6.5f), 0f);
            CreateSlope(SlopeZone.SlopeType.Subida,  new Vector3(-14f, 5f, 0f),  new Vector2(3.6f, 6.5f), 180f);
            CreateSlope(SlopeZone.SlopeType.Descida, new Vector3(-14f, -5f, 0f), new Vector2(3.6f, 6.5f), 180f);

            // 4 Colunas de Água
            Vector3[] waterPositions =
            {
                new Vector3(5.2f, -11.0f, 0f),
                new Vector3(10.8f, 10.8f, 0f),
                new Vector3(-10.8f, 10.8f, 0f),
                new Vector3(-10.8f, -10.8f, 0f),
            };
            for (int i = 0; i < waterPositions.Length; i++)
            {
                GameObject wcObj = new GameObject($"ColunaAgua_{i + 1}");
                wcObj.transform.position = waterPositions[i];
                wcObj.AddComponent<WaterColumn>().Setup();
            }

            // Barraca da Cozinha / Lanches bem acessível na pista sul-central
            GameObject kitchenObj = new GameObject("BarracaCozinha");
            kitchenObj.transform.position = new Vector3(-5.0f, -11.2f, 0f);
            kitchenStation = kitchenObj.AddComponent<KitchenStation>();
            kitchenStation.Setup();

            // Local Secreto de Terra Fofa para o Esquilo cavar Diamantes (clareira central)
            GameObject digObj = new GameObject("LocalDiamantesEsquilo");
            digObj.transform.position = new Vector3(4.2f, 4.2f, 0f);
            digSite = digObj.AddComponent<DiamondDigSite>();
            digSite.Setup(OnDiamondsVictory);
        }

        private void CreateSlope(SlopeZone.SlopeType type, Vector3 pos, Vector2 size, float angleDeg)
        {
            GameObject obj = new GameObject(type == SlopeZone.SlopeType.Subida ? "ZonaSubida" : "ZonaDescida");
            obj.transform.position = pos;
            obj.AddComponent<SlopeZone>().Setup(type, size, angleDeg);
        }

        private void BuildParkProps()
        {
            Sprite treeA = ParkSpriteFactory.CreateTopDownTreeSprite(0);
            Sprite treeB = ParkSpriteFactory.CreateTopDownTreeSprite(1);
            Sprite bench = ParkSpriteFactory.CreateParkBenchSprite();
            Sprite pot = ParkSpriteFactory.CreateFlowerPotSprite();
            Sprite flowerPink = ParkSpriteFactory.CreateFlowerClusterSprite(new Color(1f, 0.45f, 0.72f, 1f));
            Sprite flowerBlue = ParkSpriteFactory.CreateFlowerClusterSprite(new Color(0.42f, 0.70f, 1f, 1f));
            Sprite flowerGold = ParkSpriteFactory.CreateFlowerClusterSprite(new Color(1f, 0.85f, 0.25f, 1f));

            for (int i = -6; i <= 6; i++)
            {
                CreateProp(i % 2 == 0 ? treeA : treeB, new Vector3(i * 4.2f, 21f, 0f), 1.35f, 9);
                CreateProp(i % 2 == 0 ? treeB : treeA, new Vector3(i * 4.2f, -21f, 0f), 1.35f, 9);
                CreateProp(i % 2 == 0 ? treeA : treeB, new Vector3(21f, i * 4.2f, 0f), 1.35f, 9);
                CreateProp(i % 2 == 0 ? treeB : treeA, new Vector3(-21f, i * 4.2f, 0f), 1.35f, 9);
            }
            Vector3[] innerTrees = {
                new Vector3(-7.5f, 7.5f, 0f), new Vector3(8.5f, 7.5f, 0f),
                new Vector3(-7.5f, -6.5f, 0f), new Vector3(7.5f, -6.5f, 0f)
            };
            foreach (var p in innerTrees) CreateProp(treeA, p, 1.15f, 9);

            CreateProp(bench, new Vector3(4.5f, -17.2f, 0f), 1f, 4, 0f);
            CreateProp(bench, new Vector3(-5.5f, 17.2f, 0f), 1f, 4, 180f);
            CreateProp(bench, new Vector3(17.4f, 0.5f, 0f), 1f, 4, 90f);
            CreateProp(bench, new Vector3(-17.4f, 0.5f, 0f), 1f, 4, -90f);

            Vector3[] potPositions = {
                new Vector3(2.3f, -17.2f, 0f), new Vector3(6.7f, -17.2f, 0f),
                new Vector3(-3.3f, 17.2f, 0f), new Vector3(-7.7f, 17.2f, 0f),
                new Vector3(-2.6f, -11.2f, 0f), new Vector3(-7.4f, -11.2f, 0f),
                new Vector3(17.4f, -2.0f, 0f), new Vector3(-17.4f, 3.0f, 0f)
            };
            foreach (var p in potPositions) CreateProp(pot, p, 0.95f, 4);

            Vector3[] flowerSpots = {
                new Vector3(-9f, -17f, 0f), new Vector3(9f, -11f, 0f),
                new Vector3(-9f, -2f, 0f), new Vector3(9f, 2f, 0f),
                new Vector3(-4f, 6f, 0f), new Vector3(5f, 10f, 0f),
                new Vector3(-10f, 17f, 0f), new Vector3(10f, 17f, 0f),
                new Vector3(-17f, -9f, 0f), new Vector3(17f, 9f, 0f)
            };
            for (int i = 0; i < flowerSpots.Length; i++)
            {
                Sprite fSp = (i % 3 == 0) ? flowerPink : ((i % 3 == 1) ? flowerBlue : flowerGold);
                CreateProp(fSp, flowerSpots[i], 1.1f, 2);
            }
        }

        private void CreateProp(Sprite sp, Vector3 pos, float scale, int sortOrder, float rotZ = 0f)
        {
            GameObject go = new GameObject("PropParque");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
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

            // Uns ficam satisfeitos mais rápido (2 porções), outros precisam de bastante comida (4-6)!
            var configs = new (ParkSpriteFactory.AnimalKind kind, string name, int meals, Vector3 pos)[]
            {
                (ParkSpriteFactory.AnimalKind.Esquilo, "Esquilo", 6, new Vector3(9.5f, -16.8f, 0f)),
                (ParkSpriteFactory.AnimalKind.Coelho,  "Coelho",  2, new Vector3(17.2f, -6.5f, 0f)),
                (ParkSpriteFactory.AnimalKind.Gato,    "Gato",    2, new Vector3(17.2f, 10.5f, 0f)),
                (ParkSpriteFactory.AnimalKind.Lobo,    "Lobo",    4, new Vector3(4.5f, 17.2f, 0f)),
                (ParkSpriteFactory.AnimalKind.Quati,   "Quati",   4, new Vector3(-17.2f, 9.5f, 0f)),
                (ParkSpriteFactory.AnimalKind.Raposa,  "Raposa",  3, new Vector3(-17.2f, -8.5f, 0f)),
                (ParkSpriteFactory.AnimalKind.Cervo,   "Cervo",   5, new Vector3(-3.8f, 2.5f, 0f)),
            };

            foreach (var c in configs)
            {
                GameObject obj = new GameObject($"Animal_{c.name}");
                obj.transform.position = c.pos;
                ParkAnimal a = obj.AddComponent<ParkAnimal>();
                a.Setup(c.kind, c.name, c.meals, foodSprites, happySp, crySp, tearSp, heartParticles);
                animals.Add(a);
                if (a.isSquirrelSpecial) squirrelAnimal = a;
            }
        }

        private void SpawnPlayer()
        {
            GameObject playerObj = new GameObject("MeninaBicicletaTopDown");
            playerObj.transform.position = new Vector3(-1f, -14f, 0f);
            playerObj.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            player = playerObj.AddComponent<TopDownBikeController>();
            player.Setup(foodSprites, digSite, OnFoodDelivered, OnAnimalRunOver, OnWaterRefilled, msg => ShowMessage(msg, 3.5f));
        }

        // ─── Eventos de Gameplay ─────────────────────────────────────

        private void OnFoodDelivered(ParkAnimal animal, FoodType givenFood, ParkAnimal.FeedResult result)
        {
            string givenName = FoodInfo.GetDisplayName(givenFood);
            string wantedName = FoodInfo.GetDisplayName(animal.wantedFood);

            switch (result)
            {
                case ParkAnimal.FeedResult.WrongFood:
                    ShowMessage($"O {animal.animalName} queria {wantedName}, não {givenName}! Pegue na Barraca da Cozinha!", 3.5f, new Color(1f, 0.80f, 0.45f, 1f));
                    break;
                case ParkAnimal.FeedResult.AteNeedsMore:
                    ShowMessage($"Nham! O {animal.animalName} adorou ({animal.mealsEaten}/{animal.requiredMeals}) e ainda quer mais! +80 pts", 3.0f, new Color(0.75f, 1f, 0.78f, 1f));
                    break;
                case ParkAnimal.FeedResult.FullySatisfied:
                    ShowMessage($"O {animal.animalName} está SUPER SACIADO e cheio de corações! +150 pts", 3.5f, new Color(0.68f, 1f, 0.75f, 1f));
                    break;
                case ParkAnimal.FeedResult.TriggeredSquirrelSecret:
                    ShowMessage("SEGREDO DESBLOQUEADO! O Esquilo ficou feliz e foi CAVAR A TERRA no centro do parque! Vá ajudar a cavar!", 7.0f, new Color(0.60f, 0.96f, 1f, 1f));
                    digSite.ActivateDigging(squirrelAnimal);
                    break;
                case ParkAnimal.FeedResult.AlreadyFull:
                    ShowMessage($"O {animal.animalName} já está cheio, mas adorou o carinho! +20 pts", 2.2f, new Color(1f, 0.85f, 0.95f, 1f));
                    break;
            }
        }

        private void OnAnimalRunOver(ParkAnimal animal)
        {
            ShowMessage($"Ai! Você atropelou o {animal.animalName}! Ele está chorando... (-50 pts)", 3.5f, new Color(1f, 0.60f, 0.60f, 1f));
        }

        private void OnWaterRefilled()
        {
            ShowMessage("O cachorrinho bebeu água fresquinha na coluna! Sede 100%!", 2.8f, new Color(0.65f, 0.92f, 1f, 1f));
        }

        private void OnDiamondsVictory()
        {
            player.score += 1000;
            if (victoryScoreText != null)
            {
                victoryScoreText.text = $"Pontuação Final: {player.score} pontos!\nVocês cavaram fundo e encontraram vários DIAMANTES brilhantes!";
            }
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
                Vector3 desired = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
                mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, desired, 6f * Time.deltaTime);
            }

            // Ciclo suave de Dia -> Tarde Dourada
            dayCycleTime += Time.deltaTime;
            if (sunsetOverlay != null)
            {
                float golden = (Mathf.Sin(dayCycleTime * 0.04f - Mathf.PI * 0.5f) + 1f) * 0.5f;
                sunsetOverlay.color = new Color(1f, 0.62f, 0.25f, golden * 0.14f);
            }

            // Verifica animal mais próximo para mostrar pedido detalhado no HUD
            ParkAnimal nearest = null;
            float bestD = 7.5f;
            foreach (var a in animals)
            {
                float d = Vector3.Distance(player.transform.position, a.transform.position);
                a.SetExcited(d < 4.8f);
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
                ParkSoundFX.Instance?.PlayHappy();
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
            ParkSoundFX.Instance?.PlayHappy();
            if (kitchenPrepStatus != null)
                kitchenPrepStatus.text = $"{name} pronto e colocado na cesta!";
            isPreparingFood = false;
        }

        // ─── HUD ─────────────────────────────────────────────────────

        private void BuildHUD()
        {
            GameObject canvasObj = new GameObject("FlorestaTopDownHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Filtro suave de luz da tarde (não bloqueia cliques)
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

            // Faixa da Cesta + Pedido do animal próximo
            GameObject subBar = CreatePanel(canvasObj.transform, "SubBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -84f), new Vector2(1220f, 36f),
                new Color(0.12f, 0.16f, 0.22f, 0.76f));

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

            // Painel da Cozinha / Quiosque (aparece no topo-esquerdo quando perto da barraca, sem tampar a menina!)
            BuildKitchenPanel(canvasObj.transform);

            // Botão especial de Cavar com o Esquilo (aparece só perto do buraco dos diamantes)
            digButtonObj = CreateClickButton(canvasObj.transform, "BtnCavar", "CAVAR JUNTO! (Espaço)",
                new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 76f),
                new Color(0.16f, 0.72f, 0.96f, 0.96f), 26,
                () => digSite.HelpDig());
            digButtonLabel = digButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            digButtonObj.SetActive(false);

            // Controles nos cantos inferiores (centro livre para a menina!)
            CreateHoldButton(canvasObj.transform, "BtnEsq", "◀",
                Vector2.zero, new Vector2(72f, 62f), new Vector2(110f, 90f),
                new Color(0.22f, 0.48f, 0.34f, 0.82f), 40,
                () => player.SetUILeft(true), () => player.SetUILeft(false));

            CreateHoldButton(canvasObj.transform, "BtnDir", "▶",
                Vector2.zero, new Vector2(198f, 62f), new Vector2(110f, 90f),
                new Color(0.22f, 0.48f, 0.34f, 0.82f), 40,
                () => player.SetUIRight(true), () => player.SetUIRight(false));

            CreateHoldButton(canvasObj.transform, "BtnDevagar", "Devagar",
                new Vector2(1f, 0f), new Vector2(-235f, 52f), new Vector2(140f, 72f),
                new Color(0.24f, 0.66f, 0.40f, 0.84f), 23,
                () => player.SetUISlow(true), () => player.SetUISlow(false));

            CreateHoldButton(canvasObj.transform, "BtnRapido", "Rápido!",
                new Vector2(1f, 0f), new Vector2(-82f, 52f), new Vector2(140f, 72f),
                new Color(0.94f, 0.58f, 0.16f, 0.86f), 23,
                () => player.SetUIFast(true), () => player.SetUIFast(false));

            CreateClickButton(canvasObj.transform, "BtnComida", "JOGAR COMIDA",
                new Vector2(1f, 0f), new Vector2(-158f, 144f), new Vector2(256f, 76f),
                new Color(0.95f, 0.34f, 0.62f, 0.92f), 24,
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
                new Vector2(190f, 10f), new Vector2(340f, 390f),
                new Color(0.24f, 0.12f, 0.22f, 0.92f));

            CreateTMP(kitchenPanel.transform, "KTitle", "COZINHA DO PARQUE",
                23, new Vector2(0f, 165f), new Vector2(320f, 36f), TextAlignmentOptions.Center)
                .color = new Color(1f, 0.65f, 0.82f, 1f);

            // Lanches prontos (pegar na hora)
            FoodType[] readyFoods = { FoodType.Refrigerante, FoodType.Suco, FoodType.Coxinha, FoodType.Doces, FoodType.Pipoca };
            for (int i = 0; i < readyFoods.Length; i++)
            {
                FoodType ft = readyFoods[i];
                float y = 120f - i * 44f;
                CreateClickButton(kitchenPanel.transform, $"Btn_{ft}", $"+ {FoodInfo.GetDisplayName(ft)}",
                    new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(300f, 38f),
                    new Color(0.24f, 0.58f, 0.42f, 0.95f), 20,
                    () => OnClickKitchenFood(ft));
            }

            // Lanches para PREPARAR (Sanduíche e Hot Dog)
            CreateClickButton(kitchenPanel.transform, "BtnPrepSanduiche", "Preparar Sanduíche",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), new Vector2(300f, 42f),
                new Color(0.92f, 0.54f, 0.16f, 0.96f), 20,
                () => OnClickKitchenFood(FoodType.Sanduiche));

            CreateClickButton(kitchenPanel.transform, "BtnPrepHotDog", "Preparar Hot Dog",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -156f), new Vector2(300f, 42f),
                new Color(0.90f, 0.30f, 0.48f, 0.96f), 20,
                () => OnClickKitchenFood(FoodType.HotDog));

            kitchenPrepStatus = CreateTMP(kitchenPanel.transform, "PrepStatus", "Escolha ou prepare!",
                17, new Vector2(0f, -184f), new Vector2(320f, 24f), TextAlignmentOptions.Center);
            kitchenPrepStatus.color = new Color(1f, 0.94f, 0.60f, 1f);

            kitchenPanel.SetActive(false);
        }

        private void UpdateHUD(ParkAnimal nearest)
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

            if (slopeText != null) slopeText.text = player.currentSlopeLabel;
            if (basketText != null) basketText.text = player.GetBasketSummary();

            if (nearbyOrderText != null)
            {
                if (nearest != null)
                {
                    if (nearest.emotion == ParkAnimal.EmotionState.Saciado)
                        nearbyOrderText.text = $"{nearest.animalName}: Saciado e feliz!";
                    else if (nearest.emotion == ParkAnimal.EmotionState.Cavando)
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

            if (kitchenPanel != null && kitchenStation != null)
            {
                kitchenPanel.SetActive(kitchenStation.playerInside);
            }

            if (digButtonObj != null && digSite != null)
            {
                bool showDig = digSite.isActive && digSite.playerNearby && !digSite.isCompleted;
                digButtonObj.SetActive(showDig);
                if (showDig && digButtonLabel != null)
                    digButtonLabel.text = $"CAVAR JUNTO! ({Mathf.RoundToInt(digSite.digProgress)}%)";
            }

            if (player.waterLevel < 25f && Time.time > messageExpireTime)
            {
                ShowMessage("O cachorrinho está com sede! Passe perto de uma Coluna de Água!", 2.5f, new Color(0.65f, 0.92f, 1f, 1f));
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
