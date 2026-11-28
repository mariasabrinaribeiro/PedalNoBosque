using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Playable Fairground Business Loop, Friends Team & World-Transforming Upgrades for Orla das Fadas:
/// - Harvestable Orange Tree (with visible RipeOranges toggle & regrowth) + Mint Planter
/// - Playable Juice Bar (Suco de Laranja, Suco de Laranja c/ Hortelã, Limonada de Hortelã)
/// - Playable Candy Shop (Brigadeiro, Cocada da Praia, Doce de Fruta with order quantities)
/// - Playable Craft Studio (Brinco, Pulseira, Sandália customized by Color/Beads & Charm)
/// - Friends Team Participation (Protagonist, Nina & Bia can work ANY stall with unique specialty bonuses & low-stock alerts)
/// - 4 Visible 3D Fairground Upgrades that physically transform the scene and unlock recipes/materials!
/// </summary>
public sealed class GirlPowerOrla3DFairBusinesses : MonoBehaviour
{
    public enum StallKind
    {
        None = 0,
        Juice = 1,
        Candy = 2,
        Craft = 3
    }

    public enum BusinessModal
    {
        None = 0,
        Juice = 1,
        Candy = 2,
        Craft = 3,
        TeamAndUpgrades = 4
    }

    [Serializable]
    public sealed class JuiceOrder
    {
        public string customerName;
        public string drinkName;
        public int needOranges;
        public int needMint;
        public int rewardCoins;
    }

    [Serializable]
    public sealed class CandyOrder
    {
        public string customerName;
        public string sweetName;
        public int quantity;
        public int rewardCoins;
    }

    [Serializable]
    public sealed class CraftOrder
    {
        public string customerName;
        public string itemType;   // Brinco | Pulseira | Sandália
        public string colorName;  // Rosa | Turquesa | Lilás | Dourado
        public string charmName;  // Concha | Estrela | Flor | Pérola
        public int rewardCoins;
    }

    [Header("Core References")]
    public Transform player;
    public Camera sceneCamera;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DFirstDelivery storyController;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;

    [Header("Orchard & Stalls")]
    public Transform orangeTreePrimary;
    public GameObject orangeTreePrimaryFruitGroup;
    public Transform orangeTreeSecond;
    public GameObject orangeTreeSecondFruitGroup;
    public Transform mintPlanter;
    public Transform juiceStall;
    public Transform candyStall;
    public Transform craftStall;

    [Header("Waiting Customers (Visible beside each Stall)")]
    public Transform juiceCustomer;
    public Transform candyCustomer;
    public Transform craftCustomer;

    [Header("Friends (Flexible Assignment across ALL Stalls!)")]
    public Transform friendNina;
    public Transform friendBia;

    [Header("Visible 3D Fairground Upgrades (Activated when Purchased!)")]
    public GameObject upgradePinkAwningsGroup;
    public GameObject upgradeSecondOrangeTreeGroup;
    public GameObject upgradeShowcaseLightsGroup;
    public GameObject upgradeMeetingCornerGroup;

    // Live Ingredient Stock & Progress
    public int OrangesStock { get; private set; } = 1;
    public int MintStock { get; private set; } = 1;
    public int CandyStock { get; private set; } = 6;
    public int CraftMaterialsStock { get; private set; } = 6;
    public int FairOrdersCompleted { get; private set; }
    public bool OrangesReady { get; private set; } = true;
    public bool MintReady { get; private set; } = true;

    // Unlocked Visible Upgrades
    public bool HasPinkAwningsUpgrade { get; private set; }
    public bool HasSecondOrangeTreeUpgrade { get; private set; }
    public bool HasShowcaseLightsUpgrade { get; private set; }
    public bool HasMeetingCornerUpgrade { get; private set; }

    // Active Customer Orders
    public JuiceOrder CurrentJuiceOrder { get; private set; }
    public CandyOrder CurrentCandyOrder { get; private set; }
    public CraftOrder CurrentCraftOrder { get; private set; }

    // Friend Assignments (Neither Nina nor Bia is ever locked to one role!)
    public StallKind NinaAssignment { get; private set; } = StallKind.Candy;
    public StallKind BiaAssignment { get; private set; } = StallKind.Craft;

    public BusinessModal ActiveModal { get; private set; } = BusinessModal.None;
    public bool IsModalOpen => ActiveModal != BusinessModal.None;

    private float orangeRegrowTimer;
    private float mintRegrowTimer;
    private float ninaAutoTimer = 11f;
    private float biaAutoTimer = 14f;
    private float lowStockAlertCooldown;
    private string teamBannerMessage = "";
    private float teamBannerTimer;
    private Vector3 ninaHomePos;
    private Vector3 biaHomePos;

    // Interactive Modal Selections
    private int teamTabIndex; // 0 = Equipe de Amigas, 1 = Melhorias da Feira
    private string selectedDrink = "Suco de Laranja";
    private string selectedSweet = "Cocada da Praia";
    private int preparedSweetCount;
    private string selectedCraftItem = "Pulseira";
    private string selectedCraftColor = "Turquesa";
    private string selectedCraftCharm = "Concha";

    private int juiceOrderSeq;
    private int candyOrderSeq;
    private int craftOrderSeq;

    private const string SavePrefix = "OrlaFairBiz_v1_";

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (storyController == null) storyController = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (audioController == null) audioController = FindObjectOfType<GirlPowerOrla3DAudio>();
        if (characterAnimator == null) characterAnimator = FindObjectOfType<GirlPowerOrla3DCharacterAnimator>();

        if (friendNina != null) ninaHomePos = friendNina.position;
        if (friendBia != null) biaHomePos = friendBia.position;

        LoadState();
        ApplyVisualUpgrades();
        SyncFruitVisuals();

        // Initialize playable Vertical Slice orders:
        // 1) Customer 1 wants Suco de Laranja
        // 2) Customer 2 wants 2x Cocada da Praia (Candy Shop)
        // 3) Customer 3 wants Pulseira • Turquesa • Concha (Craft Shop)
        CurrentJuiceOrder = CreateNextJuiceOrder();
        CurrentCandyOrder = CreateNextCandyOrder();
        CurrentCraftOrder = CreateNextCraftOrder();
    }

    private void Update()
    {
        if (gameFlow != null && !gameFlow.IsPlaying)
        {
            if (ActiveModal != BusinessModal.None) ActiveModal = BusinessModal.None;
            return;
        }

        float dt = Time.deltaTime;
        if (teamBannerTimer > 0f) teamBannerTimer = Mathf.Max(0f, teamBannerTimer - dt);
        if (lowStockAlertCooldown > 0f) lowStockAlertCooldown = Mathf.Max(0f, lowStockAlertCooldown - dt);

        // Regrow Oranges & Mint visibly
        if (!OrangesReady)
        {
            orangeRegrowTimer -= dt;
            if (orangeRegrowTimer <= 0f)
            {
                OrangesReady = true;
                SyncFruitVisuals();
                if (orangeTreePrimary != null)
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(orangeTreePrimary.position + Vector3.up * 2.2f, "Laranjas maduras!", new Color(1f, 0.72f, 0.24f), 2.0f);
            }
        }
        if (!MintReady)
        {
            mintRegrowTimer -= dt;
            if (mintRegrowTimer <= 0f)
            {
                MintReady = true;
                if (mintPlanter != null)
                    GirlPowerOrla3DUITheme.SpawnFloatingFeedback(mintPlanter.position + Vector3.up * 1.1f, "Hortelã pronta!", new Color(0.42f, 0.92f, 0.62f), 1.8f);
            }
        }

        UpdateFriendHelper(friendNina, ninaHomePos, NinaAssignment, "Nina", ref ninaAutoTimer, dt);
        UpdateFriendHelper(friendBia, biaHomePos, BiaAssignment, "Bia", ref biaAutoTimer, dt);

        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.escapeKey.wasPressedThisFrame && ActiveModal != BusinessModal.None)
                ActiveModal = BusinessModal.None;
            else if (keys.tabKey.wasPressedThisFrame && !IsOtherModalBlocking())
                ActiveModal = (ActiveModal == BusinessModal.TeamAndUpgrades) ? BusinessModal.None : BusinessModal.TeamAndUpgrades;
        }

        if (ActiveModal == BusinessModal.None && !IsOtherModalBlocking())
            SubmitInteractionCandidates();
    }

    private bool IsOtherModalBlocking()
    {
        if (storyController != null && (storyController.HasChoiceMenuOpen || storyController.IsCraftingNow)) return true;
        if (megaSystems != null && megaSystems.IsAnyModalOpen) return true;
        if (lifeActivities != null && lifeActivities.IsShopMenuOpen) return true;
        if (gameFlow != null && !gameFlow.IsPlaying) return true;
        return false;
    }

    public bool OwnsStallTransform(Transform t)
    {
        if (t == null) return false;
        return t == juiceStall || t == candyStall || t == craftStall;
    }

    private void SubmitInteractionCandidates()
    {
        var hub = GirlPowerOrla3DInteractionHub.Instance;
        if (hub == null || player == null) return;

        // 1. Primary Orange Tree (or Second Orange Tree when upgraded!)
        Transform bestTree = GetNearestOrangeTree(3.2f);
        if (bestTree != null)
        {
            float d = DistXZ(player.position, bestTree.position);
            hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
            {
                id = "fair_orange_tree",
                target = bestTree,
                buttonLabel = OrangesReady ? "Colher Laranjas" : "Laranjeira (crescendo...)",
                targetBadgeLabel = OrangesReady ? "Laranjas prontas" : "Laranjeira",
                distance = d,
                markerHeight = 2.65f,
                isPrimaryMission = OrangesReady && OrangesStock == 0,
                onExecute = () => TryHarvestOranges()
            });
        }

        // 2. Mint Planter (Hortelã)
        if (mintPlanter != null)
        {
            float d = DistXZ(player.position, mintPlanter.position);
            if (d <= 2.9f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "fair_mint_planter",
                    target = mintPlanter,
                    buttonLabel = MintReady ? "Colher Hortelã" : "Hortelã (crescendo...)",
                    targetBadgeLabel = "Hortelã",
                    distance = d,
                    markerHeight = 1.35f,
                    isPrimaryMission = false,
                    onExecute = () => TryHarvestMint()
                });
            }
        }

        // 3. Playable Juice Bar
        if (juiceStall != null)
        {
            float d = DistXZ(player.position, juiceStall.position);
            if (d <= 3.3f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "fair_stall_juice",
                    target = juiceStall,
                    buttonLabel = "Atender Sucos",
                    targetBadgeLabel = "Barraca de Sucos",
                    distance = d,
                    markerHeight = 2.45f,
                    isPrimaryMission = false,
                    onExecute = () => OpenStallModal(BusinessModal.Juice)
                });
            }
        }

        // 4. Playable Candy Shop
        if (candyStall != null)
        {
            float d = DistXZ(player.position, candyStall.position);
            if (d <= 3.3f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "fair_stall_candy",
                    target = candyStall,
                    buttonLabel = "Atender Loja de Doces",
                    targetBadgeLabel = "Loja de Doces",
                    distance = d,
                    markerHeight = 2.45f,
                    isPrimaryMission = false,
                    onExecute = () => OpenStallModal(BusinessModal.Candy)
                });
            }
        }

        // 5. Playable Craft Studio
        if (craftStall != null)
        {
            float d = DistXZ(player.position, craftStall.position);
            if (d <= 3.3f)
            {
                hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
                {
                    id = "fair_stall_craft",
                    target = craftStall,
                    buttonLabel = "Criar Artesanato",
                    targetBadgeLabel = "Loja de Artesanato",
                    distance = d,
                    markerHeight = 2.45f,
                    isPrimaryMission = false,
                    onExecute = () => OpenStallModal(BusinessModal.Craft)
                });
            }
        }
    }

    private Transform GetNearestOrangeTree(float maxDist)
    {
        Transform best = null;
        float bestD = maxDist;
        if (orangeTreePrimary != null && orangeTreePrimary.gameObject.activeInHierarchy)
        {
            float d = DistXZ(player.position, orangeTreePrimary.position);
            if (d <= bestD) { bestD = d; best = orangeTreePrimary; }
        }
        if (HasSecondOrangeTreeUpgrade && orangeTreeSecond != null && orangeTreeSecond.gameObject.activeInHierarchy)
        {
            float d = DistXZ(player.position, orangeTreeSecond.position);
            if (d <= bestD) { bestD = d; best = orangeTreeSecond; }
        }
        return best;
    }

    public void OpenStallModal(BusinessModal modal)
    {
        ActiveModal = modal;
        preparedSweetCount = 0;
        if (modal == BusinessModal.Juice && CurrentJuiceOrder != null)
            selectedDrink = CurrentJuiceOrder.drinkName;
        else if (modal == BusinessModal.Candy && CurrentCandyOrder != null)
            selectedSweet = CurrentCandyOrder.sweetName;
        if (audioController != null) audioController.PlayChime();
    }

    // ------------------------------------------------------------------
    // HARVESTING (REAL STOCK CHANGES + VISUAL FRUIT TOGGLE)
    // ------------------------------------------------------------------
    public bool TryHarvestOranges()
    {
        if (!OrangesReady)
        {
            ShowTeamAlert("As laranjas ainda estão amadurecendo (" + Mathf.CeilToInt(orangeRegrowTimer) + "s).", 2.0f);
            return false;
        }
        int yield = HasSecondOrangeTreeUpgrade ? 6 : 3;
        OrangesStock += yield;
        OrangesReady = false;
        orangeRegrowTimer = HasSecondOrangeTreeUpgrade ? 14f : 24f;
        SyncFruitVisuals();
        SaveState();

        if (audioController != null) audioController.PlayChime();
        if (characterAnimator != null && orangeTreePrimary != null) characterAnimator.SpawnLeafBurst(orangeTreePrimary.position + Vector3.up * 1.4f);
        Vector3 pop = orangeTreePrimary != null ? orangeTreePrimary.position + Vector3.up * 2.1f : Vector3.zero;
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pop, "+" + yield + " Laranjas colhidas!", new Color(1f, 0.72f, 0.24f), 2.2f);
        return true;
    }

    public bool TryHarvestMint()
    {
        if (!MintReady)
        {
            ShowTeamAlert("A hortelã está brotando (" + Mathf.CeilToInt(mintRegrowTimer) + "s).", 2.0f);
            return false;
        }
        MintStock += 2;
        MintReady = false;
        mintRegrowTimer = 16f;
        if (megaSystems != null) megaSystems.Data.organicHerbs += 1;
        SaveState();

        if (audioController != null) audioController.PlayChime();
        Vector3 pop = mintPlanter != null ? mintPlanter.position + Vector3.up * 1.0f : Vector3.zero;
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pop, "+2 Hortelã fresca!", new Color(0.36f, 0.90f, 0.58f), 2.0f);
        return true;
    }

    private void SyncFruitVisuals()
    {
        if (orangeTreePrimaryFruitGroup != null)
            orangeTreePrimaryFruitGroup.SetActive(OrangesReady);
        if (orangeTreeSecondFruitGroup != null)
            orangeTreeSecondFruitGroup.SetActive(HasSecondOrangeTreeUpgrade && OrangesReady);
    }

    // ------------------------------------------------------------------
    // SERVING ORDERS (STRICT STOCK & RECIPE VALIDATION)
    // ------------------------------------------------------------------
    public bool TryServeJuiceOrder(string drinkChoice, bool servedByFriend = false, string friendWhoServed = "")
    {
        if (CurrentJuiceOrder == null) return false;
        if (drinkChoice != CurrentJuiceOrder.drinkName)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("A cliente pediu " + CurrentJuiceOrder.drinkName + "!", 2.4f);
            return false;
        }
        if (OrangesStock < CurrentJuiceOrder.needOranges)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Sem Laranjas! Colha na Laranjeira perto da Horta.", 2.8f);
            return false;
        }
        if (MintStock < CurrentJuiceOrder.needMint)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Sem Hortelã! Colha no canteiro da Horta.", 2.8f);
            return false;
        }

        OrangesStock -= CurrentJuiceOrder.needOranges;
        MintStock -= CurrentJuiceOrder.needMint;
        int bonus = (!servedByFriend ? 4 : 0) + (HasPinkAwningsUpgrade ? 4 : 0) + (HasMeetingCornerUpgrade ? 5 : 0);
        int earned = CurrentJuiceOrder.rewardCoins + bonus;
        AwardFairCoins(earned, juiceStall != null ? juiceStall.position : Vector3.zero,
            (servedByFriend ? (friendWhoServed + " serviu ") : "Serviu ") + CurrentJuiceOrder.drinkName + "! +" + earned + " moedas");

        FairOrdersCompleted++;
        CurrentJuiceOrder = CreateNextJuiceOrder();
        if (!servedByFriend) ActiveModal = BusinessModal.None;
        SaveState();
        return true;
    }

    public bool TryServeCandyOrder(string sweetChoice, int qtyPrepared, bool servedByFriend = false, string friendWhoServed = "")
    {
        if (CurrentCandyOrder == null) return false;
        if (sweetChoice != CurrentCandyOrder.sweetName)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("A cliente pediu " + CurrentCandyOrder.quantity + "× " + CurrentCandyOrder.sweetName + "!", 2.4f);
            return false;
        }
        if (qtyPrepared < CurrentCandyOrder.quantity)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Prepare " + CurrentCandyOrder.quantity + " unidades antes de entregar!", 2.2f);
            return false;
        }
        if (CandyStock < CurrentCandyOrder.quantity)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Estoque de doces baixo! Clique em Repor Estoque.", 2.6f);
            return false;
        }

        CandyStock -= CurrentCandyOrder.quantity;
        int ninaBonus = (servedByFriend && friendWhoServed == "Nina") || NinaAssignment == StallKind.Candy ? 4 : 0;
        int bonus = ninaBonus + (HasShowcaseLightsUpgrade ? 5 : 0) + (HasMeetingCornerUpgrade ? 5 : 0);
        int earned = CurrentCandyOrder.rewardCoins + bonus;
        AwardFairCoins(earned, candyStall != null ? candyStall.position : Vector3.zero,
            (servedByFriend ? (friendWhoServed + " entregou ") : "Entregou ") + CurrentCandyOrder.quantity + "× " + CurrentCandyOrder.sweetName + "! +" + earned + " moedas");

        FairOrdersCompleted++;
        preparedSweetCount = 0;
        CurrentCandyOrder = CreateNextCandyOrder();
        if (!servedByFriend) ActiveModal = BusinessModal.None;
        SaveState();
        return true;
    }

    public bool TryServeCraftOrder(string itemType, string colorName, string charmName, bool servedByFriend = false, string friendWhoServed = "")
    {
        if (CurrentCraftOrder == null) return false;
        if (CraftMaterialsStock < 1)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Sem contas/pingentes! Clique em Repor Materiais.", 2.6f);
            return false;
        }
        if (itemType != CurrentCraftOrder.itemType || colorName != CurrentCraftOrder.colorName || charmName != CurrentCraftOrder.charmName)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Confira o pedido: " + CurrentCraftOrder.itemType + " • " + CurrentCraftOrder.colorName + " • " + CurrentCraftOrder.charmName, 2.8f);
            return false;
        }

        CraftMaterialsStock -= 1;
        int biaBonus = (servedByFriend && friendWhoServed == "Bia") || BiaAssignment == StallKind.Craft ? 4 : 0;
        int bonus = biaBonus + (HasShowcaseLightsUpgrade ? 6 : 0) + (HasMeetingCornerUpgrade ? 5 : 0);
        int earned = CurrentCraftOrder.rewardCoins + bonus;
        AwardFairCoins(earned, craftStall != null ? craftStall.position : Vector3.zero,
            (servedByFriend ? (friendWhoServed + " criou ") : "Criou ") + itemType + " (" + colorName + ")! +" + earned + " moedas");

        FairOrdersCompleted++;
        CurrentCraftOrder = CreateNextCraftOrder();
        if (!servedByFriend) ActiveModal = BusinessModal.None;
        SaveState();
        return true;
    }

    public void RestockCandy()
    {
        CandyStock += 5;
        if (audioController != null) audioController.PlayChime();
        ShowTeamAlert("Estoque da Loja de Doces reposto (+5)!", 1.8f);
        SaveState();
    }

    public void RestockCraftMaterials()
    {
        CraftMaterialsStock += 5;
        if (audioController != null) audioController.PlayChime();
        ShowTeamAlert("Contas e pingentes repostos (+5)!", 1.8f);
        SaveState();
    }

    // ------------------------------------------------------------------
    // FRIENDS TEAM FLEXIBLE ASSIGNMENT (NO PERMANENT SINGLE-ROLE LOCK!)
    // ------------------------------------------------------------------
    public void AssignFriend(string friendName, StallKind stall)
    {
        if (friendName == "Nina")
        {
            NinaAssignment = stall;
            ninaAutoTimer = 9f;
        }
        else if (friendName == "Bia")
        {
            BiaAssignment = stall;
            biaAutoTimer = 9f;
        }
        if (audioController != null) audioController.PlayChime();
        ShowTeamAlert(friendName + " agora está em: " + StallDisplayName(stall), 2.2f);
        SaveState();
    }

    private void UpdateFriendHelper(Transform friend, Vector3 homePos, StallKind assignment, string friendName, ref float autoTimer, float dt)
    {
        if (friend == null) return;
        Transform stallTarget = GetStallTransform(assignment);
        Vector3 desired = stallTarget != null
            ? (stallTarget.position + new Vector3(friendName == "Nina" ? -1.45f : 1.45f, 0f, -1.25f))
            : homePos;

        Vector3 cur = friend.position;
        Vector2 delta = new Vector2(desired.x - cur.x, desired.z - cur.z);
        if (delta.magnitude > 0.25f)
        {
            Vector2 step = delta.normalized * Mathf.Min(delta.magnitude, 3.2f * dt);
            friend.position = new Vector3(cur.x + step.x, cur.y, cur.z + step.y);
            if (GirlPowerOrla3DCharacterFacing.TryGetFaceRotation(new Vector3(step.x, 0f, step.y), 1f, 0.0001f, out Quaternion rot))
                friend.rotation = Quaternion.Slerp(friend.rotation, rot, dt * 8f);
            return;
        }

        // Face customer/camera when stationed at a counter
        friend.rotation = Quaternion.Slerp(friend.rotation, Quaternion.Euler(0f, 186f, 0f), dt * 6f);
        if (assignment == StallKind.None || ActiveModal != BusinessModal.None) return;

        autoTimer -= dt;
        if (autoTimer > 0f) return;
        autoTimer = 15f;

        if (assignment == StallKind.Juice && CurrentJuiceOrder != null)
        {
            if (OrangesStock >= CurrentJuiceOrder.needOranges && MintStock >= CurrentJuiceOrder.needMint)
                TryServeJuiceOrder(CurrentJuiceOrder.drinkName, true, friendName);
            else if (lowStockAlertCooldown <= 0f)
            {
                lowStockAlertCooldown = 14f;
                ShowTeamAlert("⚠ " + friendName + " (Sucos): Falta ingrediente! Colha na Laranjeira/Horta.", 3.4f);
            }
        }
        else if (assignment == StallKind.Candy && CurrentCandyOrder != null)
        {
            if (CandyStock >= CurrentCandyOrder.quantity)
                TryServeCandyOrder(CurrentCandyOrder.sweetName, CurrentCandyOrder.quantity, true, friendName);
            else if (lowStockAlertCooldown <= 0f)
            {
                lowStockAlertCooldown = 14f;
                ShowTeamAlert("⚠ " + friendName + " (Doces): Estoque baixo! Passe na Loja de Doces.", 3.4f);
            }
        }
        else if (assignment == StallKind.Craft && CurrentCraftOrder != null)
        {
            if (CraftMaterialsStock >= 1)
                TryServeCraftOrder(CurrentCraftOrder.itemType, CurrentCraftOrder.colorName, CurrentCraftOrder.charmName, true, friendName);
            else if (lowStockAlertCooldown <= 0f)
            {
                lowStockAlertCooldown = 14f;
                ShowTeamAlert("⚠ " + friendName + " (Artesanato): Faltam contas e pingentes!", 3.4f);
            }
        }
    }

    // ------------------------------------------------------------------
    // VISIBLE WORLD-TRANSFORMING UPGRADES (PRIORITY 5!)
    // ------------------------------------------------------------------
    public bool TryPurchaseUpgrade(int upgradeIndex)
    {
        int cost;
        string title;
        switch (upgradeIndex)
        {
            case 0:
                if (HasPinkAwningsUpgrade) return false;
                cost = 25; title = "Toldo Rosa da Feira";
                break;
            case 1:
                if (HasSecondOrangeTreeUpgrade) return false;
                cost = 35; title = "Nova Laranjeira do Pomar";
                break;
            case 2:
                if (HasShowcaseLightsUpgrade) return false;
                cost = 45; title = "Vitrine Iluminada";
                break;
            case 3:
                if (HasMeetingCornerUpgrade) return false;
                cost = 55; title = "Cantinho de Encontro";
                break;
            default:
                return false;
        }

        int currentCoins = GetCoins();
        if (currentCoins < cost)
        {
            if (audioController != null) audioController.PlayFailChime();
            ShowTeamAlert("Moedas insuficientes (" + currentCoins + "/" + cost + "). Atenda mais pedidos!", 2.5f);
            return false;
        }

        SpendCoins(cost);
        switch (upgradeIndex)
        {
            case 0: HasPinkAwningsUpgrade = true; break;
            case 1: HasSecondOrangeTreeUpgrade = true; OrangesReady = true; break;
            case 2: HasShowcaseLightsUpgrade = true; break;
            case 3: HasMeetingCornerUpgrade = true; break;
        }

        ApplyVisualUpgrades();
        SyncFruitVisuals();
        SaveState();

        if (audioController != null) audioController.PlayFanfare();
        if (characterAnimator != null) characterAnimator.TriggerCelebration();
        Vector3 pop = player != null ? player.position + Vector3.up * 1.6f : Vector3.zero;
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(pop, "★ " + title + " instalado na Orla!", new Color(1f, 0.84f, 0.34f), 2.8f);
        ShowTeamAlert("✓ " + title + " agora aparece no cenário!", 3.0f);
        return true;
    }

    private void ApplyVisualUpgrades()
    {
        if (upgradePinkAwningsGroup != null) upgradePinkAwningsGroup.SetActive(HasPinkAwningsUpgrade);
        if (upgradeSecondOrangeTreeGroup != null) upgradeSecondOrangeTreeGroup.SetActive(HasSecondOrangeTreeUpgrade);
        if (orangeTreeSecond != null) orangeTreeSecond.gameObject.SetActive(HasSecondOrangeTreeUpgrade);
        if (upgradeShowcaseLightsGroup != null) upgradeShowcaseLightsGroup.SetActive(HasShowcaseLightsUpgrade);
        if (upgradeMeetingCornerGroup != null) upgradeMeetingCornerGroup.SetActive(HasMeetingCornerUpgrade);
    }

    // ------------------------------------------------------------------
    // ORDER GENERATORS (PROGRESSIVELY EXPAND MENU!)
    // ------------------------------------------------------------------
    private JuiceOrder CreateNextJuiceOrder()
    {
        string[] names = { "Camila", "Sofia", "Lívia", "Helena" };
        string cust = names[juiceOrderSeq % names.Length];
        int step = juiceOrderSeq++;
        if (step == 0)
            return new JuiceOrder { customerName = cust, drinkName = "Suco de Laranja", needOranges = 1, needMint = 0, rewardCoins = 14 };

        bool mintUnlocked = HasPinkAwningsUpgrade || FairOrdersCompleted >= 1;
        bool lemonadeUnlocked = HasSecondOrangeTreeUpgrade || FairOrdersCompleted >= 3;
        if (lemonadeUnlocked && step % 3 == 2)
            return new JuiceOrder { customerName = cust, drinkName = "Limonada de Hortelã", needOranges = 0, needMint = 1, rewardCoins = 20 };
        if (mintUnlocked && step % 2 == 1)
            return new JuiceOrder { customerName = cust, drinkName = "Suco de Laranja c/ Hortelã", needOranges = 1, needMint = 1, rewardCoins = 22 };
        return new JuiceOrder { customerName = cust, drinkName = "Suco de Laranja", needOranges = 1, needMint = 0, rewardCoins = 14 };
    }

    private CandyOrder CreateNextCandyOrder()
    {
        string[] names = { "Clara", "Beatriz", "Manu", "Júlia" };
        string[] sweets = { "Cocada da Praia", "Brigadeiro", "Doce de Fruta" };
        int idx = candyOrderSeq++;
        string sw = sweets[idx % sweets.Length];
        int qty = (idx % 2 == 0) ? 2 : 1;
        int unit = sw == "Doce de Fruta" ? 10 : 8;
        return new CandyOrder { customerName = names[idx % names.Length], sweetName = sw, quantity = qty, rewardCoins = unit * qty };
    }

    private CraftOrder CreateNextCraftOrder()
    {
        string[] names = { "Amanda", "Lara", "Alice", "Rafa" };
        int idx = craftOrderSeq++;
        if (idx == 0)
            return new CraftOrder { customerName = "Amanda", itemType = "Pulseira", colorName = "Turquesa", charmName = "Concha", rewardCoins = 20 };

        string[] items = HasShowcaseLightsUpgrade ? new[] { "Brinco", "Pulseira", "Sandália" } : new[] { "Brinco", "Pulseira" };
        string[] colors = { "Rosa", "Turquesa", "Lilás", "Dourado" };
        string[] charms = HasShowcaseLightsUpgrade ? new[] { "Concha", "Estrela", "Flor", "Pérola" } : new[] { "Concha", "Estrela", "Flor" };
        string it = items[idx % items.Length];
        return new CraftOrder
        {
            customerName = names[idx % names.Length],
            itemType = it,
            colorName = colors[(idx * 3 + 1) % colors.Length],
            charmName = charms[(idx * 2) % charms.Length],
            rewardCoins = it == "Sandália" ? 28 : 20
        };
    }

    // ------------------------------------------------------------------
    // COINS & PERSISTENCE
    // ------------------------------------------------------------------
    private int GetCoins()
    {
        if (lifeActivities != null) return lifeActivities.Coins;
        if (megaSystems != null) return megaSystems.Data.coins;
        return 60;
    }

    private void SpendCoins(int amount)
    {
        if (lifeActivities != null) lifeActivities.AddCoins(-amount);
        if (megaSystems != null) megaSystems.Data.coins = Mathf.Max(0, megaSystems.Data.coins - amount);
    }

    private void AwardFairCoins(int amount, Vector3 worldPos, string label)
    {
        if (lifeActivities != null) lifeActivities.AddCoins(amount);
        if (megaSystems != null)
        {
            megaSystems.Data.coins += amount;
            megaSystems.Data.deliveriesTotal += 1;
            megaSystems.Data.prestige += 2;
        }
        if (audioController != null) audioController.PlayFanfare();
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(worldPos + Vector3.up * 2.1f, label, new Color(1f, 0.88f, 0.36f), 2.5f);
    }

    private void ShowTeamAlert(string msg, float duration)
    {
        teamBannerMessage = msg;
        teamBannerTimer = duration;
    }

    private Transform GetStallTransform(StallKind kind)
    {
        switch (kind)
        {
            case StallKind.Juice: return juiceStall;
            case StallKind.Candy: return candyStall;
            case StallKind.Craft: return craftStall;
            default: return null;
        }
    }

    private static string StallDisplayName(StallKind kind)
    {
        switch (kind)
        {
            case StallKind.Juice: return "Barraca de Sucos";
            case StallKind.Candy: return "Loja de Doces";
            case StallKind.Craft: return "Artesanato";
            default: return "Livre na Orla";
        }
    }

    private static float DistXZ(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(SavePrefix + "Oranges", OrangesStock);
        PlayerPrefs.SetInt(SavePrefix + "Mint", MintStock);
        PlayerPrefs.SetInt(SavePrefix + "Candy", CandyStock);
        PlayerPrefs.SetInt(SavePrefix + "Craft", CraftMaterialsStock);
        PlayerPrefs.SetInt(SavePrefix + "Orders", FairOrdersCompleted);
        PlayerPrefs.SetInt(SavePrefix + "Upg0", HasPinkAwningsUpgrade ? 1 : 0);
        PlayerPrefs.SetInt(SavePrefix + "Upg1", HasSecondOrangeTreeUpgrade ? 1 : 0);
        PlayerPrefs.SetInt(SavePrefix + "Upg2", HasShowcaseLightsUpgrade ? 1 : 0);
        PlayerPrefs.SetInt(SavePrefix + "Upg3", HasMeetingCornerUpgrade ? 1 : 0);
        PlayerPrefs.SetInt(SavePrefix + "NinaRole", (int)NinaAssignment);
        PlayerPrefs.SetInt(SavePrefix + "BiaRole", (int)BiaAssignment);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        OrangesStock = PlayerPrefs.GetInt(SavePrefix + "Oranges", 1);
        MintStock = PlayerPrefs.GetInt(SavePrefix + "Mint", 1);
        CandyStock = PlayerPrefs.GetInt(SavePrefix + "Candy", 6);
        CraftMaterialsStock = PlayerPrefs.GetInt(SavePrefix + "Craft", 6);
        FairOrdersCompleted = PlayerPrefs.GetInt(SavePrefix + "Orders", 0);
        HasPinkAwningsUpgrade = PlayerPrefs.GetInt(SavePrefix + "Upg0", 0) == 1;
        HasSecondOrangeTreeUpgrade = PlayerPrefs.GetInt(SavePrefix + "Upg1", 0) == 1;
        HasShowcaseLightsUpgrade = PlayerPrefs.GetInt(SavePrefix + "Upg2", 0) == 1;
        HasMeetingCornerUpgrade = PlayerPrefs.GetInt(SavePrefix + "Upg3", 0) == 1;
        NinaAssignment = (StallKind)Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "NinaRole", (int)StallKind.Candy), 0, 3);
        BiaAssignment = (StallKind)Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix + "BiaRole", (int)StallKind.Craft), 0, 3);
    }

    // ------------------------------------------------------------------
    // COMPACT MOBILE-SAFE UI (WORLD CUSTOMER TAGS + STALL & TEAM MODALS)
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        GirlPowerOrla3DUITheme.EnsureInitialized();
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        float s = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 810f), 0.68f, 1.15f);
        Rect sa = Screen.safeArea;
        Rect safe = (sa.width < 100f || sa.height < 100f)
            ? new Rect(0f, 0f, Screen.width, Screen.height)
            : new Rect(sa.x, Screen.height - (sa.y + sa.height), sa.width, sa.height);

        // 1. Discreet "Feira & Amigas" quick pill tucked cleanly below top-right coin bar
        if (ActiveModal == BusinessModal.None && !IsOtherModalBlocking())
        {
            float bw = 148f * s;
            float bh = 32f * s;
            Rect teamBtn = new Rect(safe.xMax - bw - 14f * s, safe.y + 58f * s, bw, bh);
            if (GUI.Button(teamBtn, "★ Feira & Amigas", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            {
                ActiveModal = BusinessModal.TeamAndUpgrades;
                if (audioController != null) audioController.PlayChime();
            }

            // World-space customer order chips over waiting customers when visible & nearby (< 14m)
            DrawCustomerBubble(juiceCustomer, CurrentJuiceOrder != null ? ("Suco: " + CurrentJuiceOrder.drinkName) : "", s);
            DrawCustomerBubble(candyCustomer, CurrentCandyOrder != null ? (CurrentCandyOrder.quantity + "× " + CurrentCandyOrder.sweetName) : "", s);
            DrawCustomerBubble(craftCustomer, CurrentCraftOrder != null ? (CurrentCraftOrder.itemType + " • " + CurrentCraftOrder.colorName) : "", s);
        }

        // 2. Short temporary Friend / Low-Stock alert banner (bottom-center, disappears automatically)
        if (teamBannerTimer > 0f && !string.IsNullOrEmpty(teamBannerMessage))
        {
            float tw = Mathf.Min(460f * s, safe.width - 28f * s);
            float th = 36f * s;
            Rect tr = new Rect(safe.x + (safe.width - tw) * 0.5f, safe.y + 12f * s, tw, th);
            GUI.Box(tr, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
            GUIStyle st = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
            {
                fontSize = Mathf.RoundToInt(14f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            st.normal.textColor = new Color(0.24f, 0.08f, 0.14f, 1f);
            GUI.Label(tr, teamBannerMessage, st);
        }

        // 3. Active Interactive Stall / Team Modal
        switch (ActiveModal)
        {
            case BusinessModal.Juice: DrawJuiceModal(safe, s); break;
            case BusinessModal.Candy: DrawCandyModal(safe, s); break;
            case BusinessModal.Craft: DrawCraftModal(safe, s); break;
            case BusinessModal.TeamAndUpgrades: DrawTeamAndUpgradesModal(safe, s); break;
        }
    }

    private void DrawCustomerBubble(Transform customer, string text, float s)
    {
        if (customer == null || player == null || sceneCamera == null || string.IsNullOrEmpty(text)) return;
        if (DistXZ(player.position, customer.position) > 13.5f) return;
        Vector3 sp = sceneCamera.WorldToScreenPoint(customer.position + Vector3.up * 2.1f);
        if (sp.z <= 0.3f || sp.z > 24f) return;
        if (sp.x < 50f || sp.x > Screen.width - 50f || sp.y < 60f || sp.y > Screen.height - 60f) return;

        GUIStyle st = new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle)
        {
            fontSize = Mathf.RoundToInt(12f * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        st.normal.textColor = new Color(0.22f, 0.08f, 0.14f, 1f);
        float w = Mathf.Clamp(st.CalcSize(new GUIContent(text)).x + 22f * s, 108f * s, 210f * s);
        float h = 24f * s;
        Rect r = new Rect(sp.x - w * 0.5f, (Screen.height - sp.y) - h, w, h);
        GUI.Box(r, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);
        GUI.Label(r, text, st);
    }

    private void DrawJuiceModal(Rect safe, float s)
    {
        float pw = Mathf.Min(640f * s, safe.width - 20f * s);
        float ph = Mathf.Min(390f * s, safe.height - 24f * s);
        Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.y + (safe.height - ph) * 0.5f, pw, ph);
        GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect hdr = new Rect(box.x + 14f * s, box.y + 10f * s, pw - 28f * s, 42f * s);
        GUI.Box(hdr, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
        string title = "BARRACA DE SUCOS  •  Laranjas: " + OrangesStock + "  |  Hortelã: " + MintStock;
        GirlPowerOrla3DUITheme.DrawOutlinedLabel(hdr, title, new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(15.5f * s) }, Color.white, new Color(0.08f, 0.16f, 0.18f, 0.9f), 1.1f);

        float y = hdr.yMax + 12f * s;
        if (CurrentJuiceOrder != null)
        {
            Rect ord = new Rect(box.x + 14f * s, y, pw - 28f * s, 54f * s);
            GUI.Box(ord, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            string req = "Pedido de " + CurrentJuiceOrder.customerName + ":  " + CurrentJuiceOrder.drinkName
                + "  (" + (CurrentJuiceOrder.needOranges > 0 ? CurrentJuiceOrder.needOranges + " Laranja" : "")
                + (CurrentJuiceOrder.needOranges > 0 && CurrentJuiceOrder.needMint > 0 ? " + " : "")
                + (CurrentJuiceOrder.needMint > 0 ? CurrentJuiceOrder.needMint + " Hortelã" : "")
                + "  •  +" + CurrentJuiceOrder.rewardCoins + " moedas)";
            GUIStyle oSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(15f * s), fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            oSt.normal.textColor = new Color(0.20f, 0.08f, 0.14f, 1f);
            GUI.Label(ord, req, oSt);
            y = ord.yMax + 14f * s;
        }

        // Recipes Row
        bool mintUnlocked = HasPinkAwningsUpgrade || FairOrdersCompleted >= 1;
        bool lemUnlocked = HasSecondOrangeTreeUpgrade || FairOrdersCompleted >= 3;
        var drinks = new (string name, string ing, bool unlocked)[]
        {
            ("Suco de Laranja", "1 Laranja", true),
            ("Suco de Laranja c/ Hortelã", "1 Laranja + 1 Hortelã", mintUnlocked),
            ("Limonada de Hortelã", "1 Hortelã", lemUnlocked)
        };
        float gap = 10f * s;
        float cw = (pw - 28f * s - gap * 2f) / 3f;
        float ch = 128f * s;
        for (int i = 0; i < drinks.Length; i++)
        {
            Rect dr = new Rect(box.x + 14f * s + i * (cw + gap), y, cw, ch);
            bool sel = selectedDrink == drinks[i].name;
            GUIStyle bSt = new GUIStyle(sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle) { fontSize = Mathf.RoundToInt(14f * s), wordWrap = true };
            string lbl = drinks[i].unlocked
                ? ((sel ? "✓ " : "") + drinks[i].name + "\n(" + drinks[i].ing + ")")
                : (drinks[i].name + "\n[Desbloqueia c/ Melhoria]");
            if (GUI.Button(dr, lbl, bSt) && drinks[i].unlocked)
            {
                selectedDrink = drinks[i].name;
                if (audioController != null) audioController.PlayChime();
            }
        }

        float footY = box.yMax - 56f * s;
        float backW = 120f * s;
        if (GUI.Button(new Rect(box.x + 14f * s, footY, backW, 46f * s), "Voltar", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            ActiveModal = BusinessModal.None;
        if (GUI.Button(new Rect(box.x + 24f * s + backW, footY, pw - 38f * s - backW, 46f * s), "✓  PREPARAR E SERVIR SUCO", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            TryServeJuiceOrder(selectedDrink);
    }

    private void DrawCandyModal(Rect safe, float s)
    {
        float pw = Mathf.Min(640f * s, safe.width - 20f * s);
        float ph = Mathf.Min(400f * s, safe.height - 24f * s);
        Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.y + (safe.height - ph) * 0.5f, pw, ph);
        GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect hdr = new Rect(box.x + 14f * s, box.y + 10f * s, pw - 28f * s, 42f * s);
        GUI.Box(hdr, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
        GirlPowerOrla3DUITheme.DrawOutlinedLabel(hdr, "LOJA DE DOCES DA PRAIA  •  Estoque: " + CandyStock, new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(15.5f * s) }, Color.white, new Color(0.24f, 0.06f, 0.14f, 0.9f), 1.1f);

        float y = hdr.yMax + 12f * s;
        if (CurrentCandyOrder != null)
        {
            Rect ord = new Rect(box.x + 14f * s, y, pw - 28f * s, 50f * s);
            GUI.Box(ord, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            string req = "Pedido de " + CurrentCandyOrder.customerName + ":  " + CurrentCandyOrder.quantity + "× " + CurrentCandyOrder.sweetName + "   •   Prontos: " + preparedSweetCount + "/" + CurrentCandyOrder.quantity;
            GUIStyle oSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(15.5f * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            oSt.normal.textColor = new Color(0.20f, 0.08f, 0.14f, 1f);
            GUI.Label(ord, req, oSt);
            y = ord.yMax + 12f * s;
        }

        string[] sweets = { "Brigadeiro", "Cocada da Praia", "Doce de Fruta" };
        float gap = 10f * s;
        float cw = (pw - 28f * s - gap * 2f) / 3f;
        for (int i = 0; i < sweets.Length; i++)
        {
            bool sel = selectedSweet == sweets[i];
            Rect sr = new Rect(box.x + 14f * s + i * (cw + gap), y, cw, 64f * s);
            if (GUI.Button(sr, (sel ? "✓ " : "") + sweets[i], sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            {
                selectedSweet = sweets[i];
                preparedSweetCount = 0;
                if (audioController != null) audioController.PlayChime();
            }
        }

        float prepY = y + 78f * s;
        float halfW = (pw - 38f * s) * 0.5f;
        if (GUI.Button(new Rect(box.x + 14f * s, prepY, halfW, 52f * s), "+ Preparar 1 unidade (" + preparedSweetCount + ")", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
        {
            int maxQ = CurrentCandyOrder != null ? CurrentCandyOrder.quantity : 2;
            if (preparedSweetCount < maxQ)
            {
                preparedSweetCount++;
                if (audioController != null) audioController.PlayChime();
            }
        }
        if (GUI.Button(new Rect(box.x + 24f * s + halfW, prepY, halfW, 52f * s), "↻ Repor Estoque (+5)", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            RestockCandy();

        float footY = box.yMax - 56f * s;
        float backW = 120f * s;
        if (GUI.Button(new Rect(box.x + 14f * s, footY, backW, 46f * s), "Voltar", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            ActiveModal = BusinessModal.None;
        if (GUI.Button(new Rect(box.x + 24f * s + backW, footY, pw - 38f * s - backW, 46f * s), "✓  ENTREGAR DOCES", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            TryServeCandyOrder(selectedSweet, preparedSweetCount);
    }

    private void DrawCraftModal(Rect safe, float s)
    {
        float pw = Mathf.Min(700f * s, safe.width - 18f * s);
        float ph = Mathf.Min(445f * s, safe.height - 20f * s);
        Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.y + (safe.height - ph) * 0.5f, pw, ph);
        GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        Rect hdr = new Rect(box.x + 14f * s, box.y + 10f * s, pw - 28f * s, 40f * s);
        GUI.Box(hdr, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
        GirlPowerOrla3DUITheme.DrawOutlinedLabel(hdr, "LOJA DE ARTESANATO  •  Materiais: " + CraftMaterialsStock, new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(15.5f * s) }, Color.white, new Color(0.24f, 0.06f, 0.14f, 0.9f), 1.1f);

        float y = hdr.yMax + 10f * s;
        if (CurrentCraftOrder != null)
        {
            bool okItem = selectedCraftItem == CurrentCraftOrder.itemType;
            bool okCol = selectedCraftColor == CurrentCraftOrder.colorName;
            bool okChm = selectedCraftCharm == CurrentCraftOrder.charmName;
            string checkLine = "Pedido de " + CurrentCraftOrder.customerName + ":  "
                + (okItem ? "✓ " : "○ ") + CurrentCraftOrder.itemType + "   •   "
                + (okCol ? "✓ " : "○ ") + CurrentCraftOrder.colorName + "   •   "
                + (okChm ? "✓ " : "○ ") + CurrentCraftOrder.charmName
                + (BiaAssignment == StallKind.Craft ? "   [+4★ Bia]" : "");
            Rect ord = new Rect(box.x + 14f * s, y, pw - 28f * s, 44f * s);
            GUI.Box(ord, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUIStyle oSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(14.5f * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            oSt.normal.textColor = new Color(0.20f, 0.08f, 0.14f, 1f);
            GUI.Label(ord, checkLine, oSt);
            y = ord.yMax + 10f * s;
        }

        float gap = 8f * s;
        float rowH = 44f * s;
        GUIStyle lblSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(13.5f * s), fontStyle = FontStyle.Bold };
        lblSt.normal.textColor = new Color(0.24f, 0.12f, 0.18f, 1f);

        // Row 1: Peça
        GUI.Label(new Rect(box.x + 14f * s, y, pw - 28f * s, 20f * s), "1. Peça:", lblSt);
        y += 21f * s;
        string[] items = { "Brinco", "Pulseira", "Sandália" };
        float w3 = (pw - 28f * s - gap * 2f) / 3f;
        for (int i = 0; i < items.Length; i++)
        {
            bool locked = i == 2 && !HasShowcaseLightsUpgrade && FairOrdersCompleted < 2;
            bool sel = selectedCraftItem == items[i];
            string txt = locked ? (items[i] + " [Vitrine]") : ((sel ? "✓ " : "") + items[i]);
            if (GUI.Button(new Rect(box.x + 14f * s + i * (w3 + gap), y, w3, rowH), txt, sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle) && !locked)
            {
                selectedCraftItem = items[i];
                if (audioController != null) audioController.PlayChime();
            }
        }
        y += rowH + 8f * s;

        // Row 2: Cor / Contas
        GUI.Label(new Rect(box.x + 14f * s, y, pw - 28f * s, 20f * s), "2. Cor e Contas:", lblSt);
        y += 21f * s;
        string[] colors = { "Rosa", "Turquesa", "Lilás", "Dourado" };
        float w4 = (pw - 28f * s - gap * 3f) / 4f;
        for (int i = 0; i < colors.Length; i++)
        {
            bool sel = selectedCraftColor == colors[i];
            if (GUI.Button(new Rect(box.x + 14f * s + i * (w4 + gap), y, w4, rowH), (sel ? "✓ " : "") + colors[i], sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            {
                selectedCraftColor = colors[i];
                if (audioController != null) audioController.PlayChime();
            }
        }
        y += rowH + 8f * s;

        // Row 3: Pingente
        GUI.Label(new Rect(box.x + 14f * s, y, pw - 28f * s, 20f * s), "3. Pingente:", lblSt);
        y += 21f * s;
        string[] charms = { "Concha", "Estrela", "Flor", "Pérola" };
        for (int i = 0; i < charms.Length; i++)
        {
            bool locked = i == 3 && !HasShowcaseLightsUpgrade;
            bool sel = selectedCraftCharm == charms[i];
            string txt = locked ? "Pérola [Vitrine]" : ((sel ? "✓ " : "") + charms[i]);
            if (GUI.Button(new Rect(box.x + 14f * s + i * (w4 + gap), y, w4, rowH), txt, sel ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle) && !locked)
            {
                selectedCraftCharm = charms[i];
                if (audioController != null) audioController.PlayChime();
            }
        }

        float footY = box.yMax - 54f * s;
        float backW = 104f * s;
        float restockW = 152f * s;
        if (GUI.Button(new Rect(box.x + 14f * s, footY, backW, 44f * s), "Voltar", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            ActiveModal = BusinessModal.None;
        if (GUI.Button(new Rect(box.x + 22f * s + backW, footY, restockW, 44f * s), "↻ Repor (+5)", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            RestockCraftMaterials();
        float serveX = box.x + 30f * s + backW + restockW;
        if (GUI.Button(new Rect(serveX, footY, box.xMax - 14f * s - serveX, 44f * s), "✓  ENTREGAR PEÇA", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
            TryServeCraftOrder(selectedCraftItem, selectedCraftColor, selectedCraftCharm);
    }

    private void DrawTeamAndUpgradesModal(Rect safe, float s)
    {
        float pw = Mathf.Min(720f * s, safe.width - 18f * s);
        float ph = Mathf.Min(455f * s, safe.height - 20f * s);
        Rect box = new Rect(safe.x + (safe.width - pw) * 0.5f, safe.y + (safe.height - ph) * 0.5f, pw, ph);
        GUI.Box(box, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        // Top Tabs: Equipe de Amigas | Melhorias da Feira
        float tabW = (pw - 38f * s) * 0.5f;
        if (GUI.Button(new Rect(box.x + 14f * s, box.y + 12f * s, tabW, 40f * s), "Equipe de Amigas", teamTabIndex == 0 ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            teamTabIndex = 0;
        if (GUI.Button(new Rect(box.x + 24f * s + tabW, box.y + 12f * s, tabW, 40f * s), "Melhorias da Feira (" + GetCoins() + " moedas)", teamTabIndex == 1 ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
            teamTabIndex = 1;

        float y = box.y + 62f * s;
        GUIStyle darkSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(14f * s), wordWrap = true };
        darkSt.normal.textColor = new Color(0.20f, 0.08f, 0.14f, 1f);

        if (teamTabIndex == 0)
        {
            // Stock Summary Line
            Rect stBar = new Rect(box.x + 14f * s, y, pw - 28f * s, 36f * s);
            GUI.Box(stBar, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUIStyle centSt = new GUIStyle(darkSt) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(stBar, "Estoque  •  Laranjas: " + OrangesStock + "  |  Hortelã: " + MintStock + "  |  Doces: " + CandyStock + "  |  Artesanato: " + CraftMaterialsStock + "  |  Pedidos: " + FairOrdersCompleted, centSt);
            y += 46f * s;

            // Protagonist card (can work any station!)
            Rect pCard = new Rect(box.x + 14f * s, y, pw - 28f * s, 56f * s);
            GUI.Box(pCard, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUI.Label(new Rect(pCard.x + 12f * s, pCard.y + 8f * s, pCard.width - 24f * s, 40f * s),
                "Protagonista (Você) — Vantagem: +4 moedas em Sorvetes e Sucos. Pode atender qualquer barraca!", darkSt);
            y += 66f * s;

            DrawFriendAssignRow(box.x + 14f * s, y, pw - 28f * s, s, "Nina", "Vantagem: +4 moedas em Doces", NinaAssignment);
            y += 96f * s;
            DrawFriendAssignRow(box.x + 14f * s, y, pw - 28f * s, s, "Bia", "Vantagem: +4 moedas em Artesanato", BiaAssignment);
        }
        else
        {
            var upgs = new (int idx, string name, string desc, int cost, bool owned)[]
            {
                (0, "Toldo Rosa da Feira", "Instala toldos rosa/creme nas barracas e libera Suco de Laranja c/ Hortelã.", 25, HasPinkAwningsUpgrade),
                (1, "Nova Laranjeira do Pomar", "Planta uma 2ª Laranjeira com frutos (colheita 2×) e libera Limonada de Hortelã.", 35, HasSecondOrangeTreeUpgrade),
                (2, "Vitrine Iluminada", "Acende luzes nas barracas de Doces/Artesanato e libera Sandália e Pérola.", 45, HasShowcaseLightsUpgrade),
                (3, "Cantinho de Encontro", "Cria área florida rosa/turquesa na praça (+5 moedas em toda venda).", 55, HasMeetingCornerUpgrade)
            };
            for (int i = 0; i < upgs.Length; i++)
            {
                var u = upgs[i];
                Rect ur = new Rect(box.x + 14f * s, y + i * 76f * s, pw - 28f * s, 68f * s);
                GUI.Box(ur, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
                GUIStyle tSt = new GUIStyle(darkSt) { fontSize = Mathf.RoundToInt(15.5f * s), fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(ur.x + 12f * s, ur.y + 6f * s, ur.width - 170f * s, 24f * s), u.name, tSt);
                GUI.Label(new Rect(ur.x + 12f * s, ur.y + 30f * s, ur.width - 170f * s, 34f * s), u.desc, darkSt);
                Rect br = new Rect(ur.xMax - 150f * s, ur.y + 14f * s, 138f * s, 40f * s);
                if (u.owned)
                {
                    GUI.Box(br, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
                    GirlPowerOrla3DUITheme.DrawOutlinedLabel(br, "✓ Instalado", new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(13.5f * s) }, Color.white, new Color(0.08f, 0.16f, 0.18f, 0.9f), 1.0f);
                }
                else if (GUI.Button(br, u.cost + " moedas", GirlPowerOrla3DUITheme.BtnPrimaryStyle))
                {
                    TryPurchaseUpgrade(u.idx);
                }
            }
        }

        if (GUI.Button(new Rect(box.x + (pw - 160f * s) * 0.5f, box.yMax - 50f * s, 160f * s, 40f * s), "Fechar", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
            ActiveModal = BusinessModal.None;
    }

    private void DrawFriendAssignRow(float x, float y, float w, float s, string friendName, string perk, StallKind current)
    {
        Rect card = new Rect(x, y, w, 86f * s);
        GUI.Box(card, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
        GUIStyle tSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle) { fontSize = Mathf.RoundToInt(14.5f * s), fontStyle = FontStyle.Bold };
        tSt.normal.textColor = new Color(0.20f, 0.08f, 0.14f, 1f);
        GUI.Label(new Rect(card.x + 12f * s, card.y + 6f * s, card.width - 24f * s, 24f * s), friendName + "  —  " + perk, tSt);

        var opts = new (StallKind kind, string lbl)[]
        {
            (StallKind.Juice, "Sucos"),
            (StallKind.Candy, "Doces"),
            (StallKind.Craft, "Artesanato"),
            (StallKind.None,  "Livre")
        };
        float gap = 8f * s;
        float bw = (card.width - 24f * s - gap * 3f) / 4f;
        for (int i = 0; i < opts.Length; i++)
        {
            bool active = current == opts[i].kind;
            Rect br = new Rect(card.x + 12f * s + i * (bw + gap), card.y + 38f * s, bw, 36f * s);
            if (GUI.Button(br, (active ? "✓ " : "") + opts[i].lbl, active ? GirlPowerOrla3DUITheme.BtnPrimaryStyle : GirlPowerOrla3DUITheme.BtnTileStyle))
                AssignFriend(friendName, opts[i].kind);
        }
    }
}
