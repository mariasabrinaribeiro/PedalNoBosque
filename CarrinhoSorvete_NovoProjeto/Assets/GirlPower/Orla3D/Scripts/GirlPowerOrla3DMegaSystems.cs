using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Complete Cozy Coastal Life, Economy & Minigame Core for Baía do Avesso:
/// - Auto-Save JSON persistence (coins, prestige, upgrades, crops, fish, quests, recipes, wardrobe, restorations)
/// - Garden & Planting Beds (Manjericão, Hortelã, Alecrim, Morango -> Organic 2x value when watered/grown!)
/// - Pier Fishing Minigame (catch fresh fish for Sushi & Restaurant meals!)
/// - Beach Tennis Volley Minigame (play on the sand court for coins & prestige!)
/// - Restaurant Kitchen ("Batata & Mar") & Seated Kiosk Tables (Batata Frita, Drinks, Grilled Fish, Sushi + VIP 2x!)
/// - Tactile Minigames:
///     1. Fatiar & Macerar / Fritar (rhythmic drag/click for "PERFEITO!" tip bonus)
///     2. Equilíbrio na Bandeja (balance bar while carrying restaurant dishes; careful sprinting!)
///     3. Pintura Manual com Rolo (paint house walls tile-by-tile for 100% coverage bonus + Prestige)
/// - Village Notice Board (daily side quests), Permanent Bay Restoration (Pier/Benches/Fairy Lights),
///   Seasonal Festivals (Luau, Feira, Torneio de Pesca) & Recipe Book!
/// </summary>
public sealed class GirlPowerOrla3DMegaSystems : MonoBehaviour
{
    [Serializable]
    public sealed class CropPlot
    {
        public string cropName;
        public float growth;       // 0..1
        public bool watered;
        public bool organic;
        public bool planted;
    }

    [Serializable]
    public sealed class TableOrder
    {
        public string tableName;
        public string dishName;
        public int baseReward;
        public bool isVip;
        public float patience;
        public float maxPatience;
        public bool active;
    }

    [Serializable]
    public sealed class NoticeQuest
    {
        public string id;
        public string title;
        public string requirement;
        public int progress;
        public int goal;
        public int rewardCoins;
        public int rewardPrestige;
        public bool claimed;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int coins = 60;
        public int prestige = 10;
        public int deliveriesTotal = 0;
        public int speedUpgrade = 0;
        public int bagUpgrade = 0;
        public int cartUpgrade = 0;
        public bool pierRestored = false;
        public bool benchesRestored = false;
        public bool fairyLightsRestored = false;
        public bool hasHat = false;
        public bool hasBandana = false;
        public int freshFish = 1;
        public int organicHerbs = 1;
        public int shells = 2;
        public int driftwood = 2;
        public int festivalIndex = 0;
        public CropPlot[] plots;
        public NoticeQuest[] quests;
        public List<string> unlockedRecipes = new List<string>();
    }

    public enum ActiveModal
    {
        None,
        Garden,
        Fishing,
        BeachTennis,
        NoticeBoard,
        Recipes,
        Restoration,
        SliceMixMinigame,
        PaintRollerMinigame
    }

    [Header("World Anchors (Blender 3D Props)")]
    public Transform player;
    public Camera sceneCamera;
    public Transform restaurantKitchen;
    public Transform[] restaurantTables;
    public GameObject[] tableCustomers;
    public Transform gardenBed;
    public Transform fishingPier;
    public Transform beachTennisCourt;
    public Transform noticeBoard;
    public Transform campingZone;
    public GameObject fairyLightsGroup;

    [Header("Connected Controllers")]
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DAmbient ambientController;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;
    public GirlPowerOrla3DPaintSystem paintSystem;
    public GirlPowerOrla3DPetCompanion petDog;

    public SaveData Data { get; private set; } = new SaveData();
    public ActiveModal CurrentModal { get; private set; } = ActiveModal.None;
    public bool IsAnyModalOpen => CurrentModal != ActiveModal.None;

    // Tray carrying & Balance mechanic
    public bool CarryingRestaurantTray { get; private set; }
    public string CarriedDishName { get; private set; } = "";
    public int CarriedTargetTableIndex { get; private set; } = -1;
    public bool CarriedDishPerfect { get; private set; }
    public float TrayBalance { get; private set; } = 0.5f; // 0..1 (0.5 centered)

    public TableOrder[] Orders { get; private set; }

    private static readonly string[] FestivalNames = { "Luau na Praia", "Feira de Artesanato", "Torneio de Pesca" };
    private static readonly string[] CropChoices = { "Manjericão", "Hortelã", "Limão", "Morango" };
    private static readonly string[] DishPool =
    {
        "Batata Frita Crocante",
        "Combo Batata + Refri",
        "Peixe Grelhado c/ Ervas",
        "Sushi Especial da Baía",
        "Sorvete Limão c/ Manjericão",
        "Suco Tropical + Pastel"
    };

    private static string GetDishPrepVerb(string dishName)
    {
        if (string.IsNullOrEmpty(dishName)) return "Preparar";
        string low = dishName.ToLowerInvariant();
        if (low.Contains("batata") || low.Contains("pastel")) return "Fritar";
        if (low.Contains("sorvete")) return "Montar";
        return "Preparar";
    }

    // Minigame states
    private float fishCursor;
    private float fishDir = 1f;
    private float fishZoneCenter = 0.55f;

    private float tennisBallPos;
    private float tennisBallDir = 1f;
    private int tennisRally;

    private int slicePendingTable = -1;
    private string sliceDishName = "";
    private float sliceCursor;
    private float sliceDir = 1f;
    private int sliceHits;
    private int sliceNeeded = 3;

    private int paintHouseIdx = -1;
    private bool[] paintCells = new bool[12];
    private int paintPaintedCount;

    private GirlPowerOrla3DAmbient.TimePreset lastPresetSeen;
    private float orderSpawnTimer = 4f;
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "baia_do_avesso_save.json");
        LoadJsonSave();
    }

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (ambientController != null) lastPresetSeen = ambientController.currentPreset;

        InitTableOrders();
        SyncVisibleTableCustomers();
        ApplySavedVisualStates();
    }

    private void InitTableOrders()
    {
        int n = restaurantTables != null && restaurantTables.Length > 0 ? restaurantTables.Length : 3;
        Orders = new TableOrder[n];
        for (int i = 0; i < n; i++)
        {
            Orders[i] = new TableOrder
            {
                tableName = "Mesa " + (i + 1),
                dishName = DishPool[i % DishPool.Length],
                baseReward = 14 + (i % 3) * 4,
                isVip = i == 0 && Data.prestige >= 25,
                maxPatience = 95f,
                patience = 95f,
                active = i < 2
            };
        }
    }

    private void ApplySavedVisualStates()
    {
        if (lifeActivities != null) lifeActivities.SetCoinsDirect(Data.coins);
        if (characterAnimator != null) characterAnimator.SetWardrobeHatVisible(Data.hasHat);
        if (petDog != null) petDog.SetBandanaVisible(Data.hasBandana);
        if (fairyLightsGroup != null) fairyLightsGroup.SetActive(Data.fairyLightsRestored);
    }

    public string GetCurrentFestivalName()
    {
        return FestivalNames[Mathf.Abs(Data.festivalIndex) % FestivalNames.Length];
    }

    public float GetSpeedUpgradeMultiplier()
    {
        return 1f + Data.speedUpgrade * 0.14f;
    }

    public int GetMaxBagCapacity()
    {
        return 6 + Data.bagUpgrade * 4;
    }

    public void AddPrestige(int amount, Vector3 popPos)
    {
        Data.prestige = Mathf.Max(0, Data.prestige + amount);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(popPos, "+" + amount + " Prestígio da Baía!", new Color(0.95f, 0.72f, 1f));
        SaveJson();
    }

    public void RecordDelivery(int coinsEarned, Vector3 popPos)
    {
        Data.deliveriesTotal++;
        Data.prestige += 4;
        if (lifeActivities != null)
        {
            lifeActivities.AddCoins(coinsEarned);
            Data.coins = lifeActivities.Coins;
        }
        GirlPowerOrla3DJuice.SpawnFlyingCoins(popPos, Mathf.Clamp(coinsEarned / 3, 3, 10));
        GirlPowerOrla3DJuice.ShakeCamera(0.11f, 0.14f);
        SaveJson();
    }

    public void OnPipocaFoundTreasure()
    {
        Data.shells++;
        Data.organicHerbs++;
        if (lifeActivities != null)
        {
            lifeActivities.AddCoins(8);
            Data.coins = lifeActivities.Coins;
        }
        IncrementQuest("shells", 1);
        SaveJson();
    }

    public void OnPipocaTrickNearTables()
    {
        int entertained = 0;
        if (Orders != null)
        {
            for (int i = 0; i < Orders.Length; i++)
            {
                if (Orders[i].active)
                {
                    Orders[i].patience = Mathf.Min(Orders[i].maxPatience, Orders[i].patience + 22f);
                    entertained++;
                }
            }
        }
        if (entertained > 0 && lifeActivities != null)
        {
            lifeActivities.AddCoins(5);
            Data.coins = lifeActivities.Coins;
            GirlPowerOrla3DJuice.SpawnFlyingCoins(petDog != null ? petDog.transform.position : player.position, 4);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "+5 Gorjeta! Paciência das mesas restaurada!", new Color(1f, 0.86f, 0.36f));
            SaveJson();
        }
    }

    public void OpenManualPaintRoller(int houseIndex)
    {
        paintHouseIdx = houseIndex;
        paintPaintedCount = 0;
        for (int i = 0; i < paintCells.Length; i++) paintCells[i] = false;
        CurrentModal = ActiveModal.PaintRollerMinigame;
        if (audioController != null) audioController.PlayPaperFlip();
    }

    public void OpenModal(ActiveModal modal)
    {
        CurrentModal = modal;
        if (audioController != null) audioController.PlayPaperFlip();
        if (modal == ActiveModal.Fishing)
        {
            fishCursor = 0.1f;
            fishZoneCenter = UnityEngine.Random.Range(0.32f, 0.72f);
        }
        else if (modal == ActiveModal.BeachTennis)
        {
            tennisBallPos = 0.1f;
            tennisRally = 0;
        }
    }

    public void CloseModal()
    {
        CurrentModal = ActiveModal.None;
    }

    public void StartSliceMixForTable(int tableIdx)
    {
        if (Orders == null || tableIdx < 0 || tableIdx >= Orders.Length || !Orders[tableIdx].active) return;
        slicePendingTable = tableIdx;
        sliceDishName = Orders[tableIdx].dishName;
        sliceCursor = 0.15f;
        sliceHits = 0;
        CurrentModal = ActiveModal.SliceMixMinigame;
        if (audioController != null) audioController.PlaySizzle();
    }

    public void UnlockRecipe(string recipeName)
    {
        if (!Data.unlockedRecipes.Contains(recipeName))
        {
            Data.unlockedRecipes.Add(recipeName);
            SaveJson();
        }
    }

    public void IncrementQuest(string questId, int amount)
    {
        if (Data.quests == null) return;
        for (int i = 0; i < Data.quests.Length; i++)
        {
            if (Data.quests[i].id == questId && !Data.quests[i].claimed)
                Data.quests[i].progress = Mathf.Min(Data.quests[i].goal, Data.quests[i].progress + amount);
        }
        SaveJson();
    }

    public void CycleFestival()
    {
        Data.festivalIndex = (Data.festivalIndex + 1) % FestivalNames.Length;
        SaveJson();
        if (audioController != null) audioController.PlayFanfare();
        if (player != null)
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "★ Festival Ativo: " + GetCurrentFestivalName() + "!", new Color(1f, 0.84f, 0.34f));
    }

    public void UpdateTrayBalance(bool moving, bool sprinting, float dt)
    {
        if (!CarryingRestaurantTray) return;
        if (sprinting && moving)
        {
            float drift = Mathf.Sin(Time.time * 7.5f) * 0.48f * dt;
            TrayBalance += drift;
            if (TrayBalance < 0.12f || TrayBalance > 0.88f)
            {
                // Careful! Wobble warning & slow recovery instead of harsh delete
                TrayBalance = Mathf.Clamp(TrayBalance, 0.14f, 0.86f);
                CarriedDishPerfect = false;
                if (player != null)
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "Cuidado com a bandeja! Solte o Shift!", new Color(1f, 0.56f, 0.42f), 1.0f);
            }
        }
        else
        {
            TrayBalance = Mathf.MoveTowards(TrayBalance, 0.5f, dt * 0.45f);
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (gameFlow != null && !gameFlow.IsPlaying) return;

        float dt = Time.deltaTime;
        if (lifeActivities != null) Data.coins = lifeActivities.Coins;

        // Grow garden crops smoothly over time AND advance on Time-of-Day transitions!
        bool timeShifted = false;
        if (ambientController != null && ambientController.currentPreset != lastPresetSeen)
        {
            lastPresetSeen = ambientController.currentPreset;
            timeShifted = true;
        }
        if (Data.plots != null)
        {
            for (int i = 0; i < Data.plots.Length; i++)
            {
                CropPlot p = Data.plots[i];
                if (!p.planted || p.growth >= 1f) continue;
                float rate = p.watered ? 0.045f : 0.022f;
                p.growth = Mathf.Min(1f, p.growth + dt * rate + (timeShifted ? 0.55f : 0f));
            }
        }

        // Restaurant table orders lifecycle
        orderSpawnTimer -= dt;
        if (Orders != null)
        {
            for (int i = 0; i < Orders.Length; i++)
            {
                if (!Orders[i].active)
                {
                    if (orderSpawnTimer <= 0f)
                    {
                        Orders[i].active = true;
                        Orders[i].dishName = DishPool[UnityEngine.Random.Range(0, DishPool.Length)];
                        Orders[i].isVip = Data.prestige >= 25 && UnityEngine.Random.value < 0.42f;
                        Orders[i].baseReward = 14 + UnityEngine.Random.Range(0, 9);
                        Orders[i].maxPatience = 105f;
                        Orders[i].patience = Orders[i].maxPatience;
                        orderSpawnTimer = 11f;
                    }
                    continue;
                }

                Orders[i].patience -= dt * 0.65f;
                if (Orders[i].patience <= 0f)
                {
                    Orders[i].active = false;
                    if (CarryingRestaurantTray && CarriedTargetTableIndex == i)
                        CarryingRestaurantTray = false;
                }
            }
        }
        SyncVisibleTableCustomers();

        // Update minigame cursors
        if (CurrentModal == ActiveModal.Fishing)
        {
            fishCursor += fishDir * dt * 1.18f;
            if (fishCursor >= 1f) { fishCursor = 1f; fishDir = -1f; }
            else if (fishCursor <= 0f) { fishCursor = 0f; fishDir = 1f; }
        }
        else if (CurrentModal == ActiveModal.BeachTennis)
        {
            tennisBallPos += tennisBallDir * dt * (1.25f + tennisRally * 0.14f);
            if (tennisBallPos >= 1f) { tennisBallPos = 1f; tennisBallDir = -1f; }
            else if (tennisBallPos <= 0f) { tennisBallPos = 0f; tennisBallDir = 1f; }
        }
        else if (CurrentModal == ActiveModal.SliceMixMinigame)
        {
            sliceCursor += sliceDir * dt * 1.35f;
            if (sliceCursor >= 1f) { sliceCursor = 1f; sliceDir = -1f; }
            else if (sliceCursor <= 0f) { sliceCursor = 0f; sliceDir = 1f; }
        }

        // Keyboard shortcuts for modals & interactions
        Keyboard keys = Keyboard.current;
        if (keys == null) return;
        if (keys.escapeKey.wasPressedThisFrame && CurrentModal != ActiveModal.None)
        {
            CurrentModal = ActiveModal.None;
            return;
        }
        if (keys.kKey.wasPressedThisFrame && petDog != null)
        {
            petDog.PerformTrick();
            OnPipocaTrickNearTables();
        }
        if (keys.eKey.wasPressedThisFrame && CurrentModal == ActiveModal.None)
            TryWorldStationInteract();
    }

    public bool TryWorldStationInteract()
    {
        if (Near(gardenBed, 3.2f)) { OpenModal(ActiveModal.Garden); return true; }
        if (Near(fishingPier, 3.8f)) { OpenModal(ActiveModal.Fishing); return true; }
        if (Near(beachTennisCourt, 4.4f)) { OpenModal(ActiveModal.BeachTennis); return true; }
        if (Near(noticeBoard, 3.0f)) { OpenModal(ActiveModal.NoticeBoard); return true; }

        // Restaurant Kitchen: prepare active table order via Fatiar & Fritar minigame!
        if (Near(restaurantKitchen, 3.4f))
        {
            int nextOrder = FindFirstActiveOrderIndex();
            if (nextOrder >= 0)
            {
                StartSliceMixForTable(nextOrder);
                return true;
            }
        }

        // Deliver carried restaurant tray to the seated Kiosk Table!
        if (CarryingRestaurantTray && restaurantTables != null)
        {
            for (int i = 0; i < restaurantTables.Length; i++)
            {
                if (Near(restaurantTables[i], 3.2f) && Orders != null && i < Orders.Length && Orders[i].active && IsTableCustomerVisible(i))
                {
                    DeliverTableOrder(i);
                    return true;
                }
            }
        }

        // Camping family chat & bonus
        if (Near(campingZone, 3.6f))
        {
            GirlPowerOrla3DJuice.PopScale(campingZone, 1.08f, 0.30f);
            if (audioController != null) audioController.PlayChime();
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(campingZone.position, "Família acampando: 'Adoramos a Baía do Avesso!'", new Color(1f, 0.90f, 0.62f));
            return true;
        }
        return false;
    }

    private void DeliverTableOrder(int idx)
    {
        if (Orders == null || idx < 0 || idx >= Orders.Length || !Orders[idx].active || !IsTableCustomerVisible(idx)) return;
        TableOrder ord = Orders[idx];
        int reward = ord.baseReward;
        if (ord.isVip) reward *= 2;
        if (CarriedDishPerfect) reward += 8;
        if (Data.organicHerbs > 0) { Data.organicHerbs--; reward += 6; }

        ord.active = false;
        CarryingRestaurantTray = false;
        CarriedDishName = "";
        CarriedTargetTableIndex = -1;

        Transform tbl = (restaurantTables != null && idx < restaurantTables.Length) ? restaurantTables[idx] : player;
        GirlPowerOrla3DJuice.PopScale(tbl, 1.14f, 0.34f);
        if (audioController != null) audioController.PlayFanfare();
        if (characterAnimator != null) characterAnimator.TriggerCelebration();

        RecordDelivery(reward, tbl.position);
        IncrementQuest("fries", 1);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(tbl.position,
            (ord.isVip ? "★ VIP 2x! " : "") + "+" + reward + " moedas!",
            new Color(1f, 0.88f, 0.32f));
    }

    private int FindFirstActiveOrderIndex()
    {
        if (Orders == null) return -1;
        for (int i = 0; i < Orders.Length; i++)
            if (Orders[i].active) return i;
        return -1;
    }

    private void SyncVisibleTableCustomers()
    {
        if (tableCustomers == null) return;
        for (int i = 0; i < tableCustomers.Length; i++)
        {
            GameObject guest = tableCustomers[i];
            if (guest == null) continue;
            bool shouldBeVisible = Orders != null && i < Orders.Length && Orders[i].active;
            if (guest.activeSelf != shouldBeVisible) guest.SetActive(shouldBeVisible);
        }
    }

    private bool IsTableCustomerVisible(int tableIndex)
    {
        return tableCustomers != null && tableIndex >= 0 && tableIndex < tableCustomers.Length
            && tableCustomers[tableIndex] != null && tableCustomers[tableIndex].activeInHierarchy;
    }

    private bool Near(Transform t, float r)
    {
        if (t == null || player == null) return false;
        return Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(t.position.x, t.position.z)) <= r;
    }

    // ------------------------------------------------------------------
    // ONGUI: World Prompts, Tray Balance Bar & All Interactive Modals
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        float s = Mathf.Clamp(Screen.width / 1600f, 0.68f, 1.15f);

        // Carrying Restaurant Tray & Balance Bar at Top-Center
        if (CarryingRestaurantTray)
        {
            float bw = 310f * s;
            Rect trayBox = new Rect((Screen.width - bw) * 0.5f, 18f * s, bw, 58f * s);
            GUI.Box(trayBox, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            string lbl = "Bandeja: " + CarriedDishName + " → Mesa " + (CarriedTargetTableIndex + 1);
            GUI.Label(new Rect(trayBox.x + 12f * s, trayBox.y + 6f * s, bw - 24f * s, 22f * s), lbl, new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(12f * s), alignment = TextAnchor.MiddleCenter });

            Rect bar = new Rect(trayBox.x + 18f * s, trayBox.y + 34f * s, bw - 36f * s, 14f * s);
            GUI.Box(bar, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            float markerX = bar.x + Mathf.Clamp01(TrayBalance) * (bar.width - 16f * s);
            Color balCol = (TrayBalance > 0.25f && TrayBalance < 0.75f) ? new Color(0.32f, 0.86f, 0.64f) : new Color(0.96f, 0.36f, 0.42f);
            Color prev = GUI.color;
            GUI.color = balCol;
            GUI.DrawTexture(new Rect(markerX, bar.y - 2f * s, 16f * s, 18f * s), GirlPowerOrla3DUITheme.CircleWhiteTex);
            GUI.color = prev;
        }

        // Contextual world prompts for new stations when no modal is open
        if (CurrentModal == ActiveModal.None)
            DrawWorldStationPrompts(s);
        else
            DrawActiveModal(s);
    }

    private void DrawWorldStationPrompts(float s)
    {
        if (sceneCamera == null) return;
        if (Near(gardenBed, 3.2f)) DrawFloatingStationBtn(gardenBed, "[E] Horta & Plantação", s, () => OpenModal(ActiveModal.Garden));
        else if (Near(fishingPier, 3.8f)) DrawFloatingStationBtn(fishingPier, "[E] Pescar no Píer", s, () => OpenModal(ActiveModal.Fishing));
        else if (Near(beachTennisCourt, 4.4f)) DrawFloatingStationBtn(beachTennisCourt, "[E] Jogar Beach Tennis", s, () => OpenModal(ActiveModal.BeachTennis));
        else if (Near(noticeBoard, 3.0f)) DrawFloatingStationBtn(noticeBoard, "[E] Mural de Avisos", s, () => OpenModal(ActiveModal.NoticeBoard));
        else if (Near(restaurantKitchen, 3.4f))
        {
            int ord = FindFirstActiveOrderIndex();
            string txt = ord >= 0
                ? ("[E] " + GetDishPrepVerb(Orders[ord].dishName) + " " + Orders[ord].dishName)
                : "[E] Restaurante Batata & Mar";
            DrawFloatingStationBtn(restaurantKitchen, txt, s, () => { if (ord >= 0) StartSliceMixForTable(ord); });
        }
        else if (CarryingRestaurantTray && restaurantTables != null)
        {
            for (int i = 0; i < restaurantTables.Length; i++)
            {
                if (Near(restaurantTables[i], 3.2f) && Orders != null && i < Orders.Length && Orders[i].active && IsTableCustomerVisible(i))
                {
                    int idx = i;
                    DrawFloatingStationBtn(restaurantTables[i], "[E] Servir " + Orders[i].tableName, s, () => DeliverTableOrder(idx));
                    break;
                }
            }
        }
    }

    private void DrawFloatingStationBtn(Transform target, string label, float s, Action onClick)
    {
        if (target == null) return;
        // Consistent bottom-center primary action region (matches FirstDelivery & Shops!)
        float w = Mathf.Clamp(92f * s + label.Length * 7.8f * s, 215f * s, 360f * s);
        float h = 46f * s;
        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height - 118f * s;
        if (GUI.Button(new Rect(x, y, w, h), label, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            onClick?.Invoke();
    }

    private void DrawActiveModal(float s)
    {
        GUI.depth = -22;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);

        float w = 590f * s;
        float h = 430f * s;
        Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        // Top-right coral X button (matching Cozinha do Bosque reference!)
        float xSz = 38f * s;
        Rect closeRect = new Rect(card.xMax - xSz - 12f * s, card.y + 10f * s, xSz, xSz);
        GUI.DrawTexture(closeRect, GirlPowerOrla3DUITheme.CircleCoralTex);
        if (GUI.Button(closeRect, "X", GirlPowerOrla3DUITheme.BadgeTextStyle))
        {
            CurrentModal = ActiveModal.None;
            return;
        }

        switch (CurrentModal)
        {
            case ActiveModal.Garden: DrawGardenModal(card, s); break;
            case ActiveModal.Fishing: DrawFishingModal(card, s); break;
            case ActiveModal.BeachTennis: DrawBeachTennisModal(card, s); break;
            case ActiveModal.NoticeBoard: DrawNoticeBoardModal(card, s); break;
            case ActiveModal.Recipes: DrawRecipesModal(card, s); break;
            case ActiveModal.Restoration: DrawRestorationModal(card, s); break;
            case ActiveModal.SliceMixMinigame: DrawSliceMixModal(card, s); break;
            case ActiveModal.PaintRollerMinigame: DrawPaintRollerModal(card, s); break;
        }
    }

    private void DrawModalTitle(Rect card, float s, string title)
    {
        Rect titleRect = new Rect(card.x + 24f * s, card.y + 12f * s, card.width - 80f * s, 36f * s);
        GUIStyle st = new GUIStyle(GirlPowerOrla3DUITheme.HeadingStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = Mathf.RoundToInt(22f * s),
            wordWrap = false,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.18f, 0.44f, 0.36f) }
        };
        while (st.fontSize > Mathf.RoundToInt(13f * s) && st.CalcSize(new GUIContent(title)).x > titleRect.width)
            st.fontSize--;
        GUI.Label(titleRect, title, st);
    }

    private void DrawGardenModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Horta da Baía  •  Orgânicos: " + Data.organicHerbs);
        float y = card.y + 62f * s;
        for (int i = 0; i < Data.plots.Length; i++)
        {
            CropPlot p = Data.plots[i];
            Rect row = new Rect(card.x + 24f * s, y, card.width - 48f * s, 74f * s);
            GUI.Box(row, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            string status = !p.planted
                ? "Canteiro vazio"
                : (p.growth >= 1f
                    ? (p.cropName + (p.organic ? " ★ ORGÂNICO PRONTO!" : " PRONTO!"))
                    : (p.cropName + " — " + Mathf.RoundToInt(p.growth * 100f) + "%" + (p.watered ? " (Regado)" : "")));
            GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 210f * s, 26f * s), status, new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft });
            GirlPowerOrla3DUITheme.DrawProgressBar(new Rect(row.x + 14f * s, row.y + 42f * s, row.width - 220f * s, 14f * s), p.planted ? p.growth : 0f, p.organic ? new Color(0.42f, 0.92f, 0.48f) : new Color(0.95f, 0.78f, 0.32f));

            if (!p.planted)
            {
                if (GUI.Button(new Rect(row.xMax - 175f * s, row.y + 18f * s, 160f * s, 38f * s), "Plantar " + CropChoices[i % CropChoices.Length], GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                {
                    p.cropName = CropChoices[i % CropChoices.Length];
                    p.planted = true;
                    p.growth = 0.25f;
                    p.watered = false;
                    p.organic = false;
                    if (audioController != null) audioController.PlayChime();
                    GirlPowerOrla3DJuice.PopScale(gardenBed, 1.12f, 0.28f);
                    SaveJson();
                }
            }
            else if (p.growth < 1f)
            {
                if (GUI.Button(new Rect(row.xMax - 175f * s, row.y + 18f * s, 160f * s, 38f * s), p.watered ? "Adubar (+40%)" : "Regar", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
                {
                    p.watered = true;
                    p.organic = true;
                    p.growth = Mathf.Min(1f, p.growth + 0.42f);
                    if (audioController != null) audioController.PlayFootstep(2);
                    GirlPowerOrla3DJuice.PopScale(gardenBed, 1.12f, 0.28f);
                    SaveJson();
                }
            }
            else
            {
                if (GUI.Button(new Rect(row.xMax - 175f * s, row.y + 18f * s, 160f * s, 38f * s), "Colher!", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                {
                    int yield = p.organic ? 2 : 1;
                    Data.organicHerbs += yield;
                    p.planted = false;
                    p.growth = 0f;
                    if (audioController != null) audioController.PlayFanfare();
                    if (characterAnimator != null && gardenBed != null) characterAnimator.SpawnLeafBurst(gardenBed.position);
                    GirlPowerOrla3DJuice.PopScale(gardenBed, 1.20f, 0.34f);
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(gardenBed != null ? gardenBed.position : player.position, "+" + yield + " " + p.cropName + " Orgânico!", new Color(0.50f, 0.96f, 0.52f));
                    IncrementQuest("herbs", yield);
                    SaveJson();
                }
            }
            y += 82f * s;
        }
    }

    private void DrawFishingModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Pescaria no Píer  •  Peixes Frescos: " + Data.freshFish);
        GUI.Label(new Rect(card.x + 28f * s, card.y + 66f * s, card.width - 56f * s, 56f * s),
            "Clique em FISGAR quando o peixinho cruzar a faixa dourada!\nPeixes frescos abastecem o Sushi e o Restaurante da Baía.", GirlPowerOrla3DUITheme.BodyDarkStyle);

        Rect track = new Rect(card.x + 36f * s, card.y + 158f * s, card.width - 72f * s, 44f * s);
        GUI.Box(track, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

        // Sweet spot golden zone
        float zoneW = track.width * 0.22f;
        float zoneX = track.x + fishZoneCenter * (track.width - zoneW);
        GUI.Box(new Rect(zoneX, track.y + 4f * s, zoneW, track.height - 8f * s), GUIContent.none, GirlPowerOrla3DUITheme.PillGoldStyle);

        // Moving fish indicator
        float curX = track.x + fishCursor * (track.width - 28f * s);
        GUI.DrawTexture(new Rect(curX, track.y + 6f * s, 28f * s, 32f * s), GirlPowerOrla3DUITheme.CircleCoralTex);

        if (GUI.Button(new Rect(card.x + (card.width - 230f * s) * 0.5f, card.y + 240f * s, 230f * s, 58f * s), "FISGAR AGORA!", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            float zMin = (zoneX - track.x) / track.width;
            float zMax = (zoneX + zoneW - track.x) / track.width;
            if (fishCursor >= zMin && fishCursor <= zMax)
            {
                Data.freshFish++;
                if (lifeActivities != null) { lifeActivities.AddCoins(10); Data.coins = lifeActivities.Coins; }
                IncrementQuest("fish", 1);
                if (audioController != null) audioController.PlayFanfare();
                GirlPowerOrla3DJuice.SpawnFlyingCoins(fishingPier != null ? fishingPier.position : player.position, 5);
                GirlPowerOrla3DJuice.ShakeCamera(0.12f, 0.16f);
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "★ Peixe Dourado Pescado! (+10 moedas)", new Color(0.54f, 0.94f, 1f));
                fishZoneCenter = UnityEngine.Random.Range(0.24f, 0.76f);
                SaveJson();
            }
            else
            {
                if (audioController != null) audioController.PlayFootstep(2);
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "Quase! Tente fisgar na faixa dourada!", new Color(1f, 0.78f, 0.45f));
            }
        }
    }

    private void DrawBeachTennisModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Beach Tennis na Orla  •  Sequência: " + tennisRally + "/5");
        GUI.Label(new Rect(card.x + 28f * s, card.y + 66f * s, card.width - 56f * s, 46f * s),
            "Rebata a bolinha quando ela entrar na zona verde para completar 5 pontos e ganhar prêmio!", GirlPowerOrla3DUITheme.BodyDarkStyle);

        Rect track = new Rect(card.x + 36f * s, card.y + 150f * s, card.width - 72f * s, 44f * s);
        GUI.Box(track, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        float zX = track.x + track.width * 0.38f;
        float zW = track.width * 0.24f;
        GUI.Box(new Rect(zX, track.y + 4f * s, zW, track.height - 8f * s), GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);

        float bx = track.x + tennisBallPos * (track.width - 28f * s);
        Color prev = GUI.color;
        GUI.color = new Color(1f, 0.90f, 0.25f);
        GUI.DrawTexture(new Rect(bx, track.y + 8f * s, 28f * s, 28f * s), GirlPowerOrla3DUITheme.CircleWhiteTex);
        GUI.color = prev;

        if (GUI.Button(new Rect(card.x + (card.width - 230f * s) * 0.5f, card.y + 235f * s, 230f * s, 58f * s), "REBATER!", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            if (tennisBallPos >= 0.35f && tennisBallPos <= 0.65f)
            {
                tennisRally++;
                if (audioController != null) audioController.PlayTennisHit();
                GirlPowerOrla3DJuice.ShakeCamera(0.08f, 0.10f);
                if (tennisRally >= 5)
                {
                    if (lifeActivities != null) { lifeActivities.AddCoins(18); Data.coins = lifeActivities.Coins; }
                    AddPrestige(10, beachTennisCourt != null ? beachTennisCourt.position : player.position);
                    GirlPowerOrla3DJuice.SpawnFlyingCoins(player.position, 7);
                    if (audioController != null) audioController.PlayVictoryFanfare();
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "★ Vitória no Beach Tennis! +18 moedas!", new Color(1f, 0.88f, 0.34f));
                    CurrentModal = ActiveModal.None;
                }
            }
            else
            {
                tennisRally = 0;
                if (audioController != null) audioController.PlayFailChime();
            }
        }
    }

    private void DrawSliceMixModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Fatiar, Macerar & Fritar  •  " + sliceDishName);
        GUI.Label(new Rect(card.x + 28f * s, card.y + 62f * s, card.width - 56f * s, 44f * s),
            "Acerte o ritmo (" + sliceHits + "/" + sliceNeeded + ") na faixa dourada para qualidade PERFEITO (+8 gorjeta)!", GirlPowerOrla3DUITheme.BodyDarkStyle);

        Rect track = new Rect(card.x + 36f * s, card.y + 148f * s, card.width - 72f * s, 44f * s);
        GUI.Box(track, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        GUI.Box(new Rect(track.x + track.width * 0.36f, track.y + 4f * s, track.width * 0.28f, track.height - 8f * s), GUIContent.none, GirlPowerOrla3DUITheme.PillGoldStyle);

        float cx = track.x + sliceCursor * (track.width - 26f * s);
        GUI.DrawTexture(new Rect(cx, track.y + 6f * s, 26f * s, 32f * s), GirlPowerOrla3DUITheme.CircleCoralTex);

        if (GUI.Button(new Rect(card.x + (card.width - 240f * s) * 0.5f, card.y + 230f * s, 240f * s, 56f * s), "FATIAR / MISTURAR!", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            bool perfectHit = sliceCursor >= 0.34f && sliceCursor <= 0.66f;
            sliceHits++;
            if (audioController != null) audioController.PlaySizzle();
            GirlPowerOrla3DJuice.ShakeCamera(0.08f, 0.10f);

            if (sliceHits >= sliceNeeded)
            {
                CarryingRestaurantTray = true;
                CarriedDishName = sliceDishName;
                CarriedTargetTableIndex = slicePendingTable;
                CarriedDishPerfect = perfectHit;
                TrayBalance = 0.5f;
                CurrentModal = ActiveModal.None;
                if (audioController != null) audioController.PlayFanfare();
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position,
                    (perfectHit ? "★ PERFEITO! " : "") + "Leve à Mesa " + (slicePendingTable + 1) + "!",
                    new Color(1f, 0.88f, 0.36f));
            }
        }
    }

    private void DrawPaintRollerModal(Rect card, float s)
    {
        string colName = paintSystem != null ? paintSystem.CurrentColorName : "ROSA";
        Color col = paintSystem != null ? paintSystem.CurrentColor : new Color(0.95f, 0.43f, 0.59f);
        DrawModalTitle(card, s, "Pintura Manual com Rolo (" + colName + ")  •  " + paintPaintedCount + "/12");
        GUI.Label(new Rect(card.x + 28f * s, card.y + 52f * s, card.width - 56f * s, 28f * s),
            "Clique ou arraste o rolo nos blocos da parede para pintar sem falhas!", GirlPowerOrla3DUITheme.BodyDarkStyle);

        int cols = 4, rows = 3;
        float gx = card.x + 48f * s;
        float gy = card.y + 92f * s;
        float cw = (card.width - 96f * s) / cols;
        float ch = 76f * s;
        Event ev = Event.current;
        bool mouseDown = (ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                Rect cell = new Rect(gx + c * cw + 4f * s, gy + r * ch + 4f * s, cw - 8f * s, ch - 8f * s);
                Color prev = GUI.color;
                GUI.color = paintCells[idx] ? col : new Color(0.92f, 0.88f, 0.80f);
                GUI.DrawTexture(cell, GirlPowerOrla3DUITheme.PillWhiteTex);
                GUI.color = prev;

                if (!paintCells[idx] && mouseDown && cell.Contains(ev.mousePosition))
                {
                    paintCells[idx] = true;
                    paintPaintedCount++;
                    if (audioController != null) audioController.PlayPaintBrush();
                }
            }
        }

        if (paintPaintedCount >= 12 || GUI.Button(new Rect(card.x + (card.width - 240f * s) * 0.5f, card.yMax - 64f * s, 240f * s, 44f * s), "CONCLUIR PINTURA", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            bool flawless = paintPaintedCount >= 12;
            if (paintSystem != null && paintHouseIdx >= 0)
            {
                paintSystem.PaintHouse(paintHouseIdx);
                if (paintSystem.paintableHouses != null && paintHouseIdx < paintSystem.paintableHouses.Length)
                    GirlPowerOrla3DJuice.PopScale(paintSystem.paintableHouses[paintHouseIdx], 1.12f, 0.36f);
            }
            int reward = flawless ? 12 : 4;
            if (lifeActivities != null) { lifeActivities.AddCoins(reward); Data.coins = lifeActivities.Coins; }
            AddPrestige(flawless ? 10 : 3, player.position);
            GirlPowerOrla3DJuice.SpawnFlyingCoins(player.position, flawless ? 6 : 2);
            GirlPowerOrla3DJuice.ShakeCamera(0.11f, 0.16f);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, flawless ? "★ Pintura Impecável! +12 moedas!" : "Casa pintada! +4 moedas", col);
            CurrentModal = ActiveModal.None;
            SaveJson();
        }
    }

    private void DrawNoticeBoardModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Mural de Avisos da Vila  •  Festival: " + GetCurrentFestivalName());
        float y = card.y + 60f * s;
        for (int i = 0; i < Data.quests.Length; i++)
        {
            NoticeQuest q = Data.quests[i];
            Rect row = new Rect(card.x + 24f * s, y, card.width - 48f * s, 72f * s);
            GUI.Box(row, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);

            GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 175f * s, 26f * s), q.title, new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft });
            GUI.Label(new Rect(row.x + 14f * s, row.y + 36f * s, row.width - 175f * s, 26f * s), q.requirement + "  (" + q.progress + "/" + q.goal + ")", new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle) { normal = { textColor = new Color(0.88f, 0.94f, 0.96f) } });

            if (q.claimed)
            {
                GUI.Label(new Rect(row.xMax - 140f * s, row.y + 20f * s, 126f * s, 32f * s), "✓ CONCLUÍDA", GirlPowerOrla3DUITheme.BadgeTextStyle);
            }
            else if (q.progress >= q.goal)
            {
                if (GUI.Button(new Rect(row.xMax - 148f * s, row.y + 18f * s, 134f * s, 36f * s), "Resgatar +" + q.rewardCoins, GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                {
                    q.claimed = true;
                    if (lifeActivities != null) { lifeActivities.AddCoins(q.rewardCoins); Data.coins = lifeActivities.Coins; }
                    AddPrestige(q.rewardPrestige, player.position);
                    GirlPowerOrla3DJuice.SpawnFlyingCoins(player.position, 6);
                    if (audioController != null) audioController.PlayVictoryFanfare();
                    SaveJson();
                }
            }
            else
            {
                GUI.Label(new Rect(row.xMax - 140f * s, row.y + 20f * s, 126f * s, 32f * s), "+" + q.rewardCoins + " moedas", GirlPowerOrla3DUITheme.BadgeTextStyle);
            }
            y += 78f * s;
        }

        if (GUI.Button(new Rect(card.x + (card.width - 250f * s) * 0.5f, card.yMax - 48f * s, 250f * s, 36f * s), "Alternar Festival Sazonal", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            CycleFestival();
    }

    private void DrawRecipesModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Caderno de Receitas da Baía");
        string[] allRecipes =
        {
            "Limão com Manjericão — Sorvete clássico da Florista Luna",
            "Morango com Hortelã — Refrescante da horta orgânica",
            "Batata Frita Crocante — Porção especial do Quiosque Batata & Mar",
            "Peixe Grelhado c/ Alecrim — Peixe pescado no píer + erva fresca",
            "Sushi Especial da Baía — Combinado fresco com peixe da costa",
            "Trio da Festa — Receita lendária do Pavilhão da Baía"
        };
        float y = card.y + 62f * s;
        for (int i = 0; i < allRecipes.Length; i++)
        {
            Rect row = new Rect(card.x + 24f * s, y, card.width - 48f * s, 48f * s);
            GUI.Box(row, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 28f * s, 32f * s), "★  " + allRecipes[i], new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(13f * s) });
            y += 54f * s;
        }
    }

    private void DrawRestorationModal(Rect card, float s)
    {
        DrawModalTitle(card, s, "Reconstrução & Upgrades  •  Prestígio: " + Data.prestige);
        float y = card.y + 60f * s;

        DrawUpgradeRow(card, s, ref y, "Tênis Velozes (Nv. " + Data.speedUpgrade + "/3)", "Aumenta velocidade base na orla", 20 + Data.speedUpgrade * 15, Data.speedUpgrade >= 3, () =>
        {
            Data.speedUpgrade++;
            AddPrestige(8, player.position);
        });
        DrawUpgradeRow(card, s, ref y, "Mochila Expandida (Cap. " + GetMaxBagCapacity() + ")", "Carrega mais itens & presentes", 18 + Data.bagUpgrade * 14, Data.bagUpgrade >= 3, () =>
        {
            Data.bagUpgrade++;
            AddPrestige(6, player.position);
        });
        DrawUpgradeRow(card, s, ref y, "Restaurar Píer de Pesca", "Atrai peixes raros + clientes VIP", 25, Data.pierRestored, () =>
        {
            Data.pierRestored = true;
            if (fishingPier != null) GirlPowerOrla3DJuice.PopScale(fishingPier, 1.15f, 0.4f);
            AddPrestige(18, player.position);
        });
        DrawUpgradeRow(card, s, ref y, "Restaurar Cordão de Luzes (Fairy Lights)", "Ilumina a orla e dobra gorjetas VIP", 30, Data.fairyLightsRestored, () =>
        {
            Data.fairyLightsRestored = true;
            if (fairyLightsGroup != null) fairyLightsGroup.SetActive(true);
            AddPrestige(22, player.position);
        });
    }

    private void DrawUpgradeRow(Rect card, float s, ref float y, string title, string desc, int cost, bool maxed, Action onBuy)
    {
        Rect row = new Rect(card.x + 24f * s, y, card.width - 48f * s, 72f * s);
        GUI.Box(row, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width - 170f * s, 26f * s), title, new GUIStyle(GirlPowerOrla3DUITheme.BodyLightStyle) { alignment = TextAnchor.MiddleLeft });
        GUI.Label(new Rect(row.x + 14f * s, row.y + 36f * s, row.width - 170f * s, 24f * s), desc, new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle) { normal = { textColor = new Color(0.88f, 0.94f, 0.96f) } });

        if (maxed)
        {
            GUI.Label(new Rect(row.xMax - 140f * s, row.y + 20f * s, 124f * s, 32f * s), "✓ COMPLETO", GirlPowerOrla3DUITheme.BadgeTextStyle);
        }
        else if (GUI.Button(new Rect(row.xMax - 148f * s, row.y + 18f * s, 134f * s, 36f * s), cost + " moedas", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            if (lifeActivities != null && lifeActivities.Coins >= cost)
            {
                lifeActivities.AddCoins(-cost);
                Data.coins = lifeActivities.Coins;
                if (audioController != null) audioController.PlayVictoryFanfare();
                onBuy?.Invoke();
                SaveJson();
            }
            else
            {
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "Faltam moedas (" + cost + ")!", new Color(1f, 0.68f, 0.45f));
            }
        }
        y += 80f * s;
    }

    // ------------------------------------------------------------------
    // AUTO-SAVE JSON PERSISTENCE
    // ------------------------------------------------------------------
    public void SaveJson()
    {
        try
        {
            if (lifeActivities != null) Data.coins = lifeActivities.Coins;
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception) { }
    }

    public string CaptureChapterAttemptSnapshot()
    {
        return Data != null ? JsonUtility.ToJson(Data) : "";
    }

    public void RestoreChapterAttemptSnapshot(string snapshot)
    {
        if (string.IsNullOrEmpty(snapshot)) return;
        try
        {
            SaveData restored = JsonUtility.FromJson<SaveData>(snapshot);
            if (restored == null) return;
            Data = restored;
            ApplySavedVisualStates();
            SaveJson();
        }
        catch (Exception) { }
    }

    private void LoadJsonSave()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded != null) Data = loaded;
            }
        }
        catch (Exception) { }

        if (Data.plots == null || Data.plots.Length != 4)
        {
            Data.plots = new[]
            {
                new CropPlot { cropName = "Manjericão", planted = true, growth = 0.85f, watered = true, organic = true },
                new CropPlot { cropName = "Hortelã", planted = true, growth = 0.55f, watered = false, organic = false },
                new CropPlot { cropName = "Limão", planted = false, growth = 0f },
                new CropPlot { cropName = "Morango", planted = false, growth = 0f }
            };
        }

        if (Data.quests == null || Data.quests.Length != 4)
        {
            Data.quests = new[]
            {
                new NoticeQuest { id = "shells", title = "Colar da Maré Baixa", requirement = "Encontre conchas com o Pipoca [B]", goal = 3, rewardCoins = 22, rewardPrestige = 10 },
                new NoticeQuest { id = "fish", title = "Peixe Fresco pro Sushi", requirement = "Pesque peixes no Píer de Pesca", goal = 2, rewardCoins = 24, rewardPrestige = 12 },
                new NoticeQuest { id = "fries", title = "Horário de Pico: Batata Frita!", requirement = "Sirva pedidos nas mesas do Restaurante", goal = 2, rewardCoins = 26, rewardPrestige = 14 },
                new NoticeQuest { id = "herbs", title = "Colheita Orgânica da Horta", requirement = "Colha ervas frescas nos canteiros", goal = 3, rewardCoins = 20, rewardPrestige = 10 }
            };
        }

        if (Data.unlockedRecipes == null || Data.unlockedRecipes.Count == 0)
        {
            Data.unlockedRecipes = new List<string> { "Limão com Manjericão", "Batata Frita Crocante", "Morango com Hortelã" };
        }
    }
}
