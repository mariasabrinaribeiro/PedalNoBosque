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
        private InfiniteTrackGenerator trackGenerator;
        private AmbientSceneryPolisher sceneryPolisher;
        private DayNightCycleController dayNightCycle;
        private WeatherAndPhenomenaController weatherController;
        private PuppyCompanionController puppyCompanion;
        private PlayerProgressManager progressManager;
        private DiscoveryBookAndWorkshopUI discoveryUI;
        private PedalTitleScreenUI titleScreenUI;

        private readonly Dictionary<FoodType, Sprite> foodSprites = new Dictionary<FoodType, Sprite>();

        // HUD
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI squirrelText;
        private TextMeshProUGUI biomeSlopeText;
        private TextMeshProUGUI distanceKmText;
        private TextMeshProUGUI basketText;
        private TextMeshProUGUI nearbyOrderText;
        private TextMeshProUGUI messageText;
        private Slider waterSlider;
        private TextMeshProUGUI waterLabel;
        private Image sunsetOverlay;

        // Estado da Aventura Infinita (Etapa 2)
        private BiomeType currentBiome = BiomeType.CampoFlorido;
        private int nextKmMilestone = 1;

        // Painel da Cozinha e Mini-Preparo Opcional (Etapa 4)
        private GameObject kitchenPanel;
        private TextMeshProUGUI kitchenPrepStatus;
        private KitchenMiniGamePanel miniPrepPanel;
        private bool isPreparingFood = false;
        private bool kitchenManuallyClosed = false;

        // Botão de Cavar com o Esquilo + Botão Contextual de Aventura
        private GameObject digButtonObj;
        private TextMeshProUGUI digButtonLabel;
        private GameObject adventureEventButtonObj;
        private TextMeshProUGUI adventureEventButtonLabel;
        private TrailAdventureEvent nearbyAdventureEvent;

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
            progressManager = gameObject.AddComponent<PlayerProgressManager>();
            LocalizationManager.LoadFromSave(progressManager.Data);
            progressManager.OnMilestoneBanner += (msg, col) => ShowMessage(msg, 4.4f, col);

            CacheFoodSprites();
            SetupCamera();
            EnsureEventSystem();
            BuildInfiniteWorld();
            SpawnPlayer();
            BuildHUD();

            progressManager.OnCustomizationChanged += () => player?.ApplyCustomizationVisuals();
            progressManager.RecordBiomeVisited(currentBiome);

            // Ciclo completo de Dia e Noite (5 períodos: Manhã, Tarde, Pôr do Sol, Noite e Madrugada)
            dayNightCycle = gameObject.AddComponent<DayNightCycleController>();
            dayNightCycle.Initialize(mainCam, player.transform, sunsetOverlay);
            dayNightCycle.SetBiome(currentBiome);
            dayNightCycle.OnPeriodChanged += OnDayPeriodChanged;

            // Fenômenos Naturais (Chuva leve, Arco-íris, Neblina e Estrela Cadente em pool)
            weatherController = gameObject.AddComponent<WeatherAndPhenomenaController>();
            weatherController.Initialize(mainCam, dayNightCycle);
            weatherController.SetBiome(currentBiome);
            weatherController.OnWeatherMessage += (msg, col) => ShowMessage(msg, 4.0f, col);

            // Cachorrinho Companheiro Inteligente na cestinha (Feliz, Curioso, Dormindo, Sede)
            puppyCompanion = gameObject.AddComponent<PuppyCompanionController>();
            puppyCompanion.Initialize(player);

            ShowMessage("PEDAL NO BOSQUE — Explore as 6 regiões, descubra animais, moradores e segredos!", 6f);
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
            mainCam.backgroundColor = new Color32(54, 81, 48, 255);
            mainCam.transform.position = new Vector3(2f, 0.65f, -10f);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildInfiniteWorld()
        {
            float camHeight = mainCam != null ? mainCam.orthographicSize * 2.08f : 11.4f;

            Sprite happySp = ParkSpriteFactory.CreateEmotionIcon("happy");
            Sprite crySp = ParkSpriteFactory.CreateEmotionIcon("cry");
            Sprite tearSp = ParkSpriteFactory.CreateTearDropSprite();
            Sprite[] heartParticles =
            {
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(0.98f, 0.25f, 0.48f, 1f)),
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(1f, 0.52f, 0.78f, 1f)),
                ParkSpriteFactory.CreateHeartParticleSprite(new Color(1f, 0.84f, 0.24f, 1f))
            };

            GameObject genObj = new GameObject("InfiniteTrackGenerator");
            trackGenerator = genObj.AddComponent<InfiniteTrackGenerator>();
            trackGenerator.Initialize(camHeight, foodSprites, happySp, crySp, tearSp, heartParticles);
            currentBiome = trackGenerator.GetBiomeAtX(0f);

            // Camada de polimento visual (nuvens em paralaxe, pétalas/folhas/luz solar e grama em primeiro plano)
            GameObject polisherObj = new GameObject("AmbientSceneryPolisher");
            sceneryPolisher = polisherObj.AddComponent<AmbientSceneryPolisher>();
            sceneryPolisher.Initialize(mainCam);
            sceneryPolisher.SetBiome(currentBiome);

            // Local Secreto de Terra Fofa para o Esquilo cavar Diamantes (posicionado junto ao esquilo que ativar!)
            GameObject digObj = new GameObject("LocalDiamantesEsquilo");
            digObj.transform.position = new Vector3(15f, -2.45f, 0f);
            digSite = digObj.AddComponent<SideDiamondDigSite>();
            digSite.Setup(OnDiamondsVictory);
        }

        private void SpawnPlayer()
        {
            GameObject playerObj = new GameObject("MeninaBicicleta");
            playerObj.transform.position = new Vector3(0f, -3.55f, 0f);
            player = playerObj.AddComponent<BikeGirlController>();
            player.Setup(foodSprites, digSite, OnFoodDelivered, OnAnimalRunOver, OnWaterRefilled, msg => ShowMessage(msg, 3.5f));
            player.SetTrackGenerator(trackGenerator);
        }

        private void OnDayPeriodChanged(DayPeriod period)
        {
            string msg = DayNightCycleController.GetPeriodNotification(period);
            if (!string.IsNullOrEmpty(msg))
            {
                Color tint = (period == DayPeriod.Noite || period == DayPeriod.Madrugada)
                    ? new Color(0.76f, 0.88f, 1f, 1f)
                    : new Color(1f, 0.94f, 0.68f, 1f);
                ShowMessage(msg, 4.2f, tint);
            }
            if (period == DayPeriod.Noite)
                FlorestaSoundFX.Instance?.PlayAnimalVoice(ParkSpriteFactory.AnimalKind.CorujaNoturna);

            // Aplica a rotina natural por horário em todos os animais ativos (manhã, entardecer, noite)
            if (trackGenerator != null)
            {
                var activeAnimals = trackGenerator.ActiveAnimals;
                for (int i = 0; i < activeAnimals.Count; i++)
                    if (activeAnimals[i] != null) activeAnimals[i].ApplyDayPeriodRoutine(period);
            }
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
                case ForestAnimal.FeedResult.WolfStartled:
                    ShowMessage("O Lobo está assustado com a velocidade! Pedale bem devagar para acalmá-lo antes de alimentar!", 4.0f, new Color(1f, 0.72f, 0.48f, 1f));
                    break;
                case ForestAnimal.FeedResult.AteNeedsMore:
                    puppyCompanion?.TriggerHappy();
                    if (animal.kind == ParkSpriteFactory.AnimalKind.Lobo)
                        ShowMessage($"Uau! Você acalmou o Lobo com carinho ({animal.mealsEaten}/{animal.requiredMeals})! +120 pts", 3.2f, new Color(0.75f, 1f, 0.82f, 1f));
                    else
                        ShowMessage($"Nham! O {animal.animalName} amou ({animal.mealsEaten}/{animal.requiredMeals}) e soltou corações! +80 pts", 3.0f, new Color(0.75f, 1f, 0.78f, 1f));
                    break;
                case ForestAnimal.FeedResult.FullySatisfied:
                    puppyCompanion?.TriggerHappy(3.5f);
                    if (animal.kind == ParkSpriteFactory.AnimalKind.Gato)
                        ShowMessage("Miau! O Gato ficou super feliz e agora está acompanhando a sua bicicleta! +175 pts", 4.5f, new Color(1f, 0.82f, 0.94f, 1f));
                    else if (animal.kind == ParkSpriteFactory.AnimalKind.Lobo)
                        ShowMessage("O Lobo confiou em você e ficou seu amigo protetor na floresta! +220 pts", 4.2f, new Color(0.72f, 1f, 0.86f, 1f));
                    else
                        ShowMessage($"O {animal.animalName} está SUPER SACIADO e cheio de corações! +150 pts", 3.5f, new Color(0.68f, 1f, 0.75f, 1f));
                    break;
                case ForestAnimal.FeedResult.TriggeredSquirrelSecret:
                    ShowMessage("SEGREDO DO ESQUILO! Ele começou a CAVAR A TERRA! Vá até ele e ajude a cavar para achar DIAMANTES!", 7.0f, new Color(0.60f, 0.96f, 1f, 1f));
                    digSite.ActivateDigging(animal);
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

            // Atualiza geração procedural infinita de fundo e segmentos
            if (trackGenerator != null)
            {
                float camX = mainCam != null ? mainCam.transform.position.x : player.transform.position.x;
                trackGenerator.UpdateWorld(player.transform.position.x, camX);
                player.SetMinX(trackGenerator.MinSafeBacktrackX);

                BiomeType newBiome = trackGenerator.GetBiomeAtX(player.transform.position.x);
                if (newBiome != currentBiome)
                {
                    currentBiome = newBiome;
                    if (sceneryPolisher != null) sceneryPolisher.SetBiome(currentBiome);
                    if (dayNightCycle != null) dayNightCycle.SetBiome(currentBiome);
                    if (weatherController != null) weatherController.SetBiome(currentBiome);
                    progressManager?.RecordBiomeVisited(currentBiome);
                    FlorestaSoundFX.Instance?.PlayBiomeCue(currentBiome);
                    ShowMessage(
                        InfiniteTrackGenerator.GetBiomeArrivalTip(currentBiome),
                        4.2f,
                        InfiniteTrackGenerator.GetBiomeAccentColor(currentBiome));
                }
            }

            // Celebração a cada 1.0 km pedalado!
            progressManager?.RecordDistanceKm(player.maxDistanceKm);
            if (player.maxDistanceKm >= nextKmMilestone)
            {
                player.score += 100;
                progressManager?.AddStars(1);
                FlorestaSoundFX.Instance?.PlayStar();
                ShowMessage($"Marco atingido: {nextKmMilestone:0.0} km pedalados! +1 Estrela e +100 pts!", 4.0f, new Color(1f, 0.92f, 0.42f, 1f));
                nextKmMilestone++;
            }

            // Ciclo Dia/Noite agora é gerenciado pelo DayNightCycleController (5 períodos)
            dayCycleTime += Time.deltaTime;

            // Emoção e voz dos animais + detecção de moradores próximos (Etapa 5)
            ForestAnimal nearest = null;
            float bestD = 9f;
            VillageResident nearestResident = null;
            float bestResD = 9f;
            if (trackGenerator != null)
            {
                var activeAnimals = trackGenerator.ActiveAnimals;
                for (int i = 0; i < activeAnimals.Count; i++)
                {
                    ForestAnimal a = activeAnimals[i];
                    if (a == null) continue;
                    float d = Mathf.Abs(player.transform.position.x - a.transform.position.x);
                    if (d < 6.5f) progressManager?.RecordAnimalDiscovery(a.kind);
                    bool wolfStartled = a.OnPlayerApproaching(d < 6.5f, player.currentSpeed, player.transform);
                    if (wolfStartled)
                    {
                        ShowMessage("Cuidado! O Lobo se assustou com a velocidade! Diminua o ritmo para alimentá-lo!", 3.6f, new Color(1f, 0.72f, 0.48f, 1f));
                    }
                    if (d < bestD)
                    {
                        bestD = d;
                        nearest = a;
                    }
                }

                var activeResidents = trackGenerator.ActiveResidents;
                for (int i = 0; i < activeResidents.Count; i++)
                {
                    VillageResident r = activeResidents[i];
                    if (r == null) continue;
                    float d = Mathf.Abs(player.transform.position.x - r.transform.position.x);
                    if (d < bestResD)
                    {
                        bestResD = d;
                        nearestResident = r;
                    }
                }

                // Eventos contextuais de aventura + farejamento do cachorrinho!
                nearbyAdventureEvent = null;
                float bestEvD = 4.6f;
                var activeEvents = trackGenerator.ActiveEvents;
                for (int i = 0; i < activeEvents.Count; i++)
                {
                    TrailAdventureEvent ev = activeEvents[i];
                    if (ev == null || ev.isCompleted) continue;
                    float d = Mathf.Abs(player.transform.position.x - ev.transform.position.x);
                    if (ev.CheckPuppyAlert(d))
                    {
                        puppyCompanion?.TriggerCurious(3.8f);
                        ShowMessage("Au-au! O cachorrinho farejou um Tesouro Escondido por perto!", 3.6f, new Color(1f, 0.92f, 0.50f, 1f));
                    }
                    if (d < bestEvD)
                    {
                        bestEvD = d;
                        nearbyAdventureEvent = ev;
                    }
                }
            }

            UpdateHUD(nearest, bestD, nearestResident, bestResD);
        }

        // ─── Cozinha / Preparo de Lanches ────────────────────────────

        private void OnClickKitchenFood(FoodType type)
        {
            if (isPreparingFood || player == null) return;

            if (FoodInfo.NeedsPreparation(type))
            {
                if (miniPrepPanel != null)
                {
                    miniPrepPanel.OpenFor(type);
                    if (kitchenPrepStatus != null)
                        kitchenPrepStatus.text = $"Monte o {FoodInfo.GetDisplayName(type)} ou use Preparo Automático!";
                }
                else
                {
                    StartCoroutine(PrepareFoodRoutine(type));
                }
            }
            else
            {
                string replaced = player.foodBasket.Count == player.maxBasketSize
                    ? FoodInfo.GetDisplayName(player.foodBasket[0]) : null;
                player.AddFoodToBasket(type);
                FlorestaSoundFX.Instance?.PlayHappy();
                if (kitchenPrepStatus != null)
                    kitchenPrepStatus.text = replaced == null
                        ? $"{FoodInfo.GetDisplayName(type)} na cesta!"
                        : $"Cesta cheia! {FoodInfo.GetDisplayName(type)} entrou no lugar de {replaced}.";
            }
        }

        private void OnMiniPrepCompleted(FoodType type, bool isGourmet)
        {
            if (player == null) return;
            player.AddFoodToBasket(type);
            if (isGourmet)
            {
                player.score += 60;
                player.gourmetBonusCharges++;
                FlorestaSoundFX.Instance?.PlayDiamond();
                string name = FoodInfo.GetDisplayName(type);
                if (kitchenPrepStatus != null)
                    kitchenPrepStatus.text = $"{name} GOURMET pronto! (+60 pts + bônus na entrega!)";
                ShowMessage($"Chef da Floresta! {name} Gourmet montado com perfeição! +60 pts!", 3.5f, new Color(1f, 0.90f, 0.42f, 1f));
            }
        }

        private void OnClickQuickRestockBasket()
        {
            if (player == null || isPreparingFood) return;
            FoodType[] quickKit = { FoodType.Nozes, FoodType.Cenoura, FoodType.Maca, FoodType.Mel, FoodType.Sementes, FoodType.Suco };
            player.foodBasket.Clear();
            for (int i = 0; i < quickKit.Length && i < player.maxBasketSize; i++)
                player.AddFoodToBasket(quickKit[i]);

            FlorestaSoundFX.Instance?.PlayHappy();
            if (kitchenPrepStatus != null)
                kitchenPrepStatus.text = "Cesta cheia com o Kit Rápido da Floresta!";
            ShowMessage("Estação de Abastecimento: Cesta abastecida rapidamente!", 2.6f, new Color(0.72f, 1f, 0.78f, 1f));
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

            // Barra Superior Orgânica Arredondada (apenas informações essenciais: Estrelas, Região/Período, Distância e Água)
            GameObject topBar = CreateRoundedPanel(canvasObj.transform, "TopBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(1060f, 44f),
                new Color(0.14f, 0.28f, 0.22f, 0.80f));

            // Ícone real de Estrela + contador (sem caractere Unicode quadrado!)
            CreateUIIcon(topBar.transform, "StarIcon", StorybookUISpriteFactory.UIIconKind.Star,
                new Vector2(-478f, 0f), new Vector2(28f, 28f));
            scoreText = CreateTMP(topBar.transform, "ScoreText", "0  (0 pts)",
                18, new Vector2(-368f, 0f), new Vector2(185f, 34f), TextAlignmentOptions.Left);
            scoreText.color = new Color(1f, 0.96f, 0.80f, 1f);

            // Esquilo / Progresso discreto
            squirrelText = CreateTMP(topBar.transform, "SquirrelText", "Esquilo 0/6",
                15, new Vector2(-195f, 0f), new Vector2(145f, 30f), TextAlignmentOptions.Center);
            squirrelText.color = new Color(1f, 0.90f, 0.58f, 0.95f);

            // Região Atual + Período do Dia
            biomeSlopeText = CreateTMP(topBar.transform, "BiomeSlopeText", "Vale das Flores - Manha",
                17, new Vector2(30f, 0f), new Vector2(290f, 34f), TextAlignmentOptions.Center);
            biomeSlopeText.color = InfiniteTrackGenerator.GetBiomeAccentColor(BiomeType.CampoFlorido);

            // Distância (km)
            distanceKmText = CreateTMP(topBar.transform, "DistanceKmText", "0.00 km",
                18, new Vector2(235f, 0f), new Vector2(115f, 34f), TextAlignmentOptions.Center);
            distanceKmText.color = new Color(1f, 0.96f, 0.72f, 1f);

            // Ícone real de Gota d'Água + Barra visual
            CreateUIIcon(topBar.transform, "WaterDropIcon", StorybookUISpriteFactory.UIIconKind.WaterDrop,
                new Vector2(330f, 0f), new Vector2(26f, 26f));

            waterLabel = CreateTMP(topBar.transform, "WaterLabel", "100%",
                14, new Vector2(492f, 0f), new Vector2(56f, 24f), TextAlignmentOptions.Left);
            waterLabel.color = new Color(0.76f, 0.96f, 1f, 1f);

            GameObject sliderObj = CreateRoundedPanel(topBar.transform, "WaterSlider",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(405f, 0f), new Vector2(118f, 14f),
                new Color(0.12f, 0.22f, 0.28f, 0.90f));
            waterSlider = sliderObj.AddComponent<Slider>();
            waterSlider.minValue = 0f; waterSlider.maxValue = 100f; waterSlider.value = 100f;

            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one; faRT.sizeDelta = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRT = fill.AddComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one; fRT.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.32f, 0.84f, 0.98f, 0.98f);
            waterSlider.fillRect = fRT;

            // Sub-barra secundária discreta arredondada (Cesta + Pedido próximo)
            GameObject subBar = CreateRoundedPanel(canvasObj.transform, "SubBar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -64f), new Vector2(1060f, 26f),
                new Color(0.12f, 0.18f, 0.16f, 0.62f));

            basketText = CreateTMP(subBar.transform, "BasketText", "Cesta: ...",
                14, new Vector2(-245f, 0f), new Vector2(540f, 22f), TextAlignmentOptions.Left);
            basketText.color = new Color(1f, 0.96f, 0.84f, 0.95f);

            nearbyOrderText = CreateTMP(subBar.transform, "NearbyOrder", "",
                14, new Vector2(255f, 0f), new Vector2(520f, 22f), TextAlignmentOptions.Right);
            nearbyOrderText.color = new Color(0.84f, 1f, 0.88f, 0.96f);

            // Mensagem de feedback suave
            messageText = CreateTMP(canvasObj.transform, "MessageText", "",
                18, new Vector2(0f, -94f), new Vector2(1060f, 30f), TextAlignmentOptions.Center);
            messageText.outlineColor = new Color32(28, 49, 40, 255);
            messageText.outlineWidth = 0.25f;
            RectTransform msgRT = messageText.GetComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0.5f, 1f);
            msgRT.anchorMax = new Vector2(0.5f, 1f);

            // Painel Compacto da Cozinha (canto superior esquerdo — nunca cobre a protagonista!)
            BuildKitchenPanel(canvasObj.transform);

            // Botão especial de Cavar com o Esquilo
            digButtonObj = CreateIconClickButton(canvasObj.transform, "BtnCavar", "Cavar Junto",
                StorybookUISpriteFactory.UIIconKind.Star,
                new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(220f, 44f),
                new Color(0.24f, 0.62f, 0.78f, 0.92f), 17,
                () => digSite.HelpDig());
            digButtonLabel = digButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            digButtonObj.SetActive(false);

            // Botão contextual de Evento de Aventura (regar flores, ajudar passarinho, consertar ponte, abrir tesouro)
            adventureEventButtonObj = CreateIconClickButton(canvasObj.transform, "BtnEventoAventura", "Interagir",
                StorybookUISpriteFactory.UIIconKind.Star,
                new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(260f, 44f),
                new Color(0.28f, 0.62f, 0.44f, 0.94f), 15,
                () => player.TryInteractNearbyEvent());
            adventureEventButtonLabel = adventureEventButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            adventureEventButtonObj.SetActive(false);

            // Controles inferiores arredondados com ícones ilustrados reais (zero caracteres quadrados!)
            CreateIconHoldButton(canvasObj.transform, "BtnVoltar", "Voltar",
                StorybookUISpriteFactory.UIIconKind.BackArrow,
                Vector2.zero, new Vector2(72f, 28f), new Vector2(114f, 42f),
                new Color(0.50f, 0.35f, 0.25f, 0.84f), 16,
                () => player.SetUIReverse(true), () => player.SetUIReverse(false));

            CreateIconHoldButton(canvasObj.transform, "BtnDevagar", "Passear",
                StorybookUISpriteFactory.UIIconKind.Stroll,
                Vector2.zero, new Vector2(196f, 28f), new Vector2(122f, 42f),
                new Color(0.24f, 0.52f, 0.36f, 0.86f), 16,
                () => player.SetUISlow(true), () => player.SetUISlow(false));

            CreateIconHoldButton(canvasObj.transform, "BtnRapido", "Pedalar",
                StorybookUISpriteFactory.UIIconKind.Pedal,
                Vector2.zero, new Vector2(328f, 28f), new Vector2(130f, 42f),
                new Color(0.78f, 0.54f, 0.22f, 0.88f), 16,
                () => player.SetUIFast(true), () => player.SetUIFast(false));

            CreateIconClickButton(canvasObj.transform, "BtnPular", "Pular",
                StorybookUISpriteFactory.UIIconKind.Jump,
                new Vector2(1f, 0f), new Vector2(-248f, 28f), new Vector2(114f, 44f),
                new Color(0.26f, 0.56f, 0.68f, 0.88f), 16,
                () => player.TriggerJump());

            CreateIconClickButton(canvasObj.transform, "BtnComida", "Entregar",
                StorybookUISpriteFactory.UIIconKind.Basket,
                new Vector2(1f, 0f), new Vector2(-102f, 28f), new Vector2(158f, 44f),
                new Color(0.80f, 0.40f, 0.48f, 0.90f), 17,
                () => player.ThrowFood());

            // Botões delicados para o Diário de Descobertas, Oficina e Campainha (B)
            CreateIconClickButton(canvasObj.transform, "BtnDiario", "Livro",
                StorybookUISpriteFactory.UIIconKind.Book,
                new Vector2(0.5f, 0f), new Vector2(-106f, 28f), new Vector2(96f, 38f),
                new Color(0.30f, 0.56f, 0.44f, 0.86f), 15,
                () => discoveryUI?.Open(0));

            CreateIconClickButton(canvasObj.transform, "BtnOficina", "Oficina",
                StorybookUISpriteFactory.UIIconKind.Workshop,
                new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(102f, 38f),
                new Color(0.58f, 0.44f, 0.74f, 0.86f), 15,
                () => discoveryUI?.Open(2));

            CreateIconClickButton(canvasObj.transform, "BtnSino", "Sino",
                StorybookUISpriteFactory.UIIconKind.Star,
                new Vector2(0.5f, 0f), new Vector2(102f, 28f), new Vector2(88f, 38f),
                new Color(0.82f, 0.62f, 0.24f, 0.86f), 15,
                () => player.RingBell());

            discoveryUI = gameObject.AddComponent<DiscoveryBookAndWorkshopUI>();
            discoveryUI.Build(canvasObj.transform);

            // Atividades afetivas e criativas (Música da Floresta, Cuidadora e Enigma da Coruja)
            gameObject.AddComponent<ForestActivitiesUI>().Build(canvasObj.transform);

            // Tela de Título Oficial PEDAL NO BOSQUE + Idiomas (PT/EN/ES) + Barreira Parental
            titleScreenUI = gameObject.AddComponent<PedalTitleScreenUI>();
            titleScreenUI.Build(canvasObj.transform, () =>
            {
                ShowMessage("PEDAL NO BOSQUE - Explore as 6 regioes, ajude os amigos e descubra segredos!", 5.5f);
            });

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

            CreateClickButton(victoryPanel.transform, "VContinue", "Continuar Pedalando!",
                new Vector2(0.5f, 0.5f), new Vector2(-155f, -105f), new Vector2(275f, 64f),
                new Color(0.20f, 0.66f, 0.94f, 1f), 23,
                () => victoryPanel.SetActive(false));

            CreateClickButton(victoryPanel.transform, "VReplay", "Jogar Novamente",
                new Vector2(0.5f, 0.5f), new Vector2(155f, -105f), new Vector2(260f, 64f),
                new Color(0.28f, 0.78f, 0.46f, 1f), 23,
                () => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

            victoryPanel.SetActive(false);
        }

        private void BuildKitchenPanel(Transform canvasT)
        {
            // Painel compacto de cartões ilustrados no canto superior esquerdo (nunca cobre a protagonista!)
            kitchenPanel = CreateRoundedCardPanel(canvasT, "KitchenPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(228f, -238f), new Vector2(420f, 290f),
                new Color(0.97f, 0.94f, 0.88f, 0.95f));

            CreateTMP(kitchenPanel.transform, "KTitle", "Cozinha do Bosque",
                17, new Vector2(-40f, 124f), new Vector2(280f, 26f), TextAlignmentOptions.Left)
                .color = new Color(0.24f, 0.44f, 0.32f, 1f);

            // Botão Fechar fácil (ícone X real)
            CreateIconClickButton(kitchenPanel.transform, "BtnFecharCozinha", "",
                StorybookUISpriteFactory.UIIconKind.CloseX,
                new Vector2(1f, 1f), new Vector2(-26f, -22f), new Vector2(32f, 30f),
                new Color(0.76f, 0.36f, 0.42f, 0.92f), 14,
                () =>
                {
                    kitchenManuallyClosed = true;
                    kitchenPanel.SetActive(false);
                });

            // 10 cartões compactos com ícones reais dos alimentos (2 linhas x 5 colunas)
            FoodType[] gridFoods =
            {
                FoodType.Nozes, FoodType.Cenoura, FoodType.Maca, FoodType.Mel, FoodType.Sementes,
                FoodType.Suco, FoodType.Pipoca, FoodType.Coxinha, FoodType.Doces, FoodType.Refrigerante
            };

            for (int i = 0; i < gridFoods.Length; i++)
            {
                FoodType ft = gridFoods[i];
                int col = i % 5;
                int row = i / 5;
                float x = -158f + col * 79f;
                float y = 66f - row * 74f;

                GameObject card = CreateRoundedCardPanel(kitchenPanel.transform, $"FoodCard_{ft}",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(72f, 66f),
                    row == 0 ? new Color(0.88f, 0.96f, 0.88f, 1f) : new Color(0.98f, 0.94f, 0.86f, 1f));

                if (foodSprites.TryGetValue(ft, out Sprite fsp) && fsp != null)
                {
                    GameObject imgObj = new GameObject("Icon");
                    imgObj.transform.SetParent(card.transform, false);
                    RectTransform irt = imgObj.AddComponent<RectTransform>();
                    irt.anchoredPosition = new Vector2(0f, 9f);
                    irt.sizeDelta = new Vector2(38f, 38f);
                    Image im = imgObj.AddComponent<Image>();
                    im.sprite = fsp;
                    im.preserveAspect = true;
                }

                TextMeshProUGUI lbl = CreateTMP(card.transform, "Lbl", FoodInfo.GetDisplayName(ft),
                    11, new Vector2(0f, -20f), new Vector2(68f, 18f), TextAlignmentOptions.Center);
                lbl.color = new Color(0.24f, 0.22f, 0.20f, 1f);

                Button btn = card.AddComponent<Button>();
                btn.onClick.AddListener(() => OnClickKitchenFood(ft));
            }

            // Linha inferior: Preparar Sanduíche, Preparar Hot Dog e Kit Rápido
            CreateRoundedClickButton(kitchenPanel.transform, "BtnPrepSanduiche", "Sanduiche",
                new Vector2(0.5f, 0.5f), new Vector2(-132f, -86f), new Vector2(124f, 32f),
                new Color(0.84f, 0.54f, 0.22f, 0.95f), 14,
                () => OnClickKitchenFood(FoodType.Sanduiche));

            CreateRoundedClickButton(kitchenPanel.transform, "BtnPrepHotDog", "Hot Dog",
                new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), new Vector2(120f, 32f),
                new Color(0.82f, 0.36f, 0.46f, 0.95f), 14,
                () => OnClickKitchenFood(FoodType.HotDog));

            CreateRoundedClickButton(kitchenPanel.transform, "BtnKitRapido", "Kit Rapido",
                new Vector2(0.5f, 0.5f), new Vector2(132f, -86f), new Vector2(124f, 32f),
                new Color(0.28f, 0.58f, 0.46f, 0.95f), 14,
                OnClickQuickRestockBasket);

            kitchenPrepStatus = CreateTMP(kitchenPanel.transform, "PrepStatus", "Toque em um cartao para pegar na cesta!",
                13, new Vector2(0f, -124f), new Vector2(396f, 22f), TextAlignmentOptions.Center);
            kitchenPrepStatus.color = new Color(0.32f, 0.28f, 0.22f, 1f);

            miniPrepPanel = gameObject.AddComponent<KitchenMiniGamePanel>();
            miniPrepPanel.Build(canvasT, OnMiniPrepCompleted, ft => StartCoroutine(PrepareFoodRoutine(ft)));

            kitchenPanel.SetActive(false);
        }

        private void UpdateHUD(ForestAnimal nearest, float nearestAnimalDist, VillageResident nearestResident, float nearestResidentDist)
        {
            if (scoreText != null)
            {
                string comboTxt = player.happyCombo >= 3 ? $" x{player.happyCombo}" : "";
                int savedStars = progressManager != null && progressManager.Data != null ? progressManager.Data.starsAvailable : player.starsCollected;
                scoreText.text = $"{savedStars}  ({player.score} pts{comboTxt})";
            }

            if (squirrelText != null)
            {
                ForestAnimal sq = trackGenerator != null ? trackGenerator.PrimarySquirrel : null;
                if (digSite != null && digSite.isActive && !digSite.isCompleted)
                    squirrelText.text = $"Cavando {Mathf.RoundToInt(digSite.digProgress)}%";
                else if (sq != null)
                    squirrelText.text = $"Esquilo {sq.mealsEaten}/{sq.requiredMeals}";
                else
                    squirrelText.text = "Pedal no Bosque";
            }

            if (biomeSlopeText != null)
            {
                string biomeName = InfiniteTrackGenerator.GetBiomeDisplayName(currentBiome);
                string periodName = dayNightCycle != null ? DayNightCycleController.GetPeriodDisplayName(dayNightCycle.CurrentPeriod) : "Dia";
                biomeSlopeText.text = $"{biomeName} - {periodName}";
                biomeSlopeText.color = InfiniteTrackGenerator.GetBiomeAccentColor(currentBiome);
            }

            if (distanceKmText != null)
            {
                distanceKmText.text = $"{player.distanceKm:0.00} km";
            }
            if (basketText != null) basketText.text = player.GetBasketSummary();

            if (nearbyOrderText != null)
            {
                bool showResident = nearestResident != null && (nearest == null || nearestResidentDist < nearestAnimalDist);
                if (nearbyAdventureEvent != null && !nearbyAdventureEvent.isCompleted)
                {
                    nearbyOrderText.text = nearbyAdventureEvent.NearbyPrompt;
                }
                else if (showResident)
                {
                    if (nearestResident.isHelped)
                        nearbyOrderText.text = $"{nearestResident.residentName}: Missão concluída com alegria!";
                    else
                        nearbyOrderText.text = $"{nearestResident.residentName} ({nearestResident.missionTitle}): {FoodInfo.GetDisplayName(nearestResident.wantedFood)} (+{nearestResident.starReward} Estrelas)";
                }
                else if (nearest != null)
                {
                    if (nearest.isCatFollowing)
                        nearbyOrderText.text = "Gato está te acompanhando feliz pela floresta!";
                    else if (nearest.isWolfCautious)
                        nearbyOrderText.text = "Lobo assustado! Pedale devagar para acalmá-lo!";
                    else if (nearest.emotion == ForestAnimal.EmotionState.Saciado)
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
                    nearbyOrderText.text = "Aproxime-se de um animal ou morador para ver o pedido";
                }
            }

            if (waterSlider != null) waterSlider.value = player.waterLevel;
            if (waterLabel != null)
            {
                int wPct = Mathf.RoundToInt(player.waterLevel);
                waterLabel.text = $"{wPct}%";
                waterLabel.color = wPct < 25 ? new Color(1f, 0.50f, 0.48f, 1f) : new Color(0.76f, 0.96f, 1f, 1f);
            }

            if (!player.isNearKitchen) kitchenManuallyClosed = false;
            if (kitchenPanel != null) kitchenPanel.SetActive(player.isNearKitchen && !kitchenManuallyClosed);
            if ((!player.isNearKitchen || kitchenManuallyClosed) && miniPrepPanel != null && miniPrepPanel.IsOpen)
                miniPrepPanel.Close();

            if (digButtonObj != null && digSite != null)
            {
                bool showDig = digSite.isActive && digSite.playerNearby && !digSite.isCompleted;
                digButtonObj.SetActive(showDig);
                if (showDig && digButtonLabel != null)
                    digButtonLabel.text = $"Cavar Junto ({Mathf.RoundToInt(digSite.digProgress)}%)";
            }

            if (adventureEventButtonObj != null)
            {
                bool showDigNow = digButtonObj != null && digButtonObj.activeSelf;
                bool showEvent = !showDigNow && nearbyAdventureEvent != null && !nearbyAdventureEvent.isCompleted;
                adventureEventButtonObj.SetActive(showEvent);
                if (showEvent && adventureEventButtonLabel != null)
                    adventureEventButtonLabel.text = nearbyAdventureEvent.ActionButtonLabel;
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
            GameObject btnObj = CreateRoundedPanel(parent, name, anchor, anchor, pos, size, color);
            CreateTMP(btnObj.transform, "Label", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btnObj;
        }

        private GameObject CreateRoundedPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = CreatePanel(parent, name, anchorMin, anchorMax, pos, size, color);
            Image img = go.GetComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            img.type = Image.Type.Sliced;
            return go;
        }

        private GameObject CreateRoundedCardPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = CreatePanel(parent, name, anchorMin, anchorMax, pos, size, color);
            Image img = go.GetComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedCardSprite();
            img.type = Image.Type.Sliced;
            return go;
        }

        private void CreateUIIcon(Transform parent, string name, StorybookUISpriteFactory.UIIconKind iconKind, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetIcon(iconKind);
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private GameObject CreateRoundedClickButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, System.Action onClick)
        {
            GameObject btnObj = CreateRoundedPanel(parent, name, anchor, anchor, pos, size, color);
            CreateTMP(btnObj.transform, "Label", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btnObj;
        }

        private GameObject CreateIconClickButton(Transform parent, string name, string label, StorybookUISpriteFactory.UIIconKind iconKind, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, System.Action onClick)
        {
            GameObject btnObj = CreateRoundedPanel(parent, name, anchor, anchor, pos, size, color);
            bool hasLabel = !string.IsNullOrEmpty(label);
            CreateUIIcon(btnObj.transform, "Icon", iconKind,
                hasLabel ? new Vector2(-size.x * 0.5f + 22f, 0f) : Vector2.zero,
                new Vector2(24f, 24f));
            if (hasLabel)
                CreateTMP(btnObj.transform, "Label", label, fontSize, new Vector2(12f, 0f), new Vector2(size.x - 30f, size.y), TextAlignmentOptions.Center);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btnObj;
        }

        private void CreateIconHoldButton(Transform parent, string name, string label, StorybookUISpriteFactory.UIIconKind iconKind, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int fontSize, System.Action onDown, System.Action onUp)
        {
            GameObject btnObj = CreateRoundedPanel(parent, name, anchor, anchor, pos, size, color);
            CreateUIIcon(btnObj.transform, "Icon", iconKind, new Vector2(-size.x * 0.5f + 22f, 0f), new Vector2(24f, 24f));
            CreateTMP(btnObj.transform, "Label", label, fontSize, new Vector2(12f, 0f), new Vector2(size.x - 30f, size.y), TextAlignmentOptions.Center);

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
    }
}
