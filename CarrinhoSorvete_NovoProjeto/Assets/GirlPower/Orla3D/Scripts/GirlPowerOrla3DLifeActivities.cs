using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Open-World Cozy Life Activities & Bottom-Left Illustrated HUD for Baía do Avesso:
/// - Golden Coin Counter + Visual Quick Inventory (Basil Leaves + Satchel Items)
/// - Diegetic coastal tile icons for Painting [P]/[C], Pets [B], and Friendship/Gifting [G]
/// - Floating world-space [E] prompts above the 6 interactive market stalls + rounded cozy shop modal
/// </summary>
public sealed class GirlPowerOrla3DLifeActivities : MonoBehaviour
{
    [Serializable]
    public struct ShopStall
    {
        public string shopName;
        public string category;
        public Transform stallTransform;
        public string[] itemNames;
        public int[] itemPrices;
        public bool isClothingShop;
    }

    [Serializable]
    public struct FriendEntry
    {
        public string friendName;
        public Transform target;
        public Color tagColor;
        public float voicePitch;
    }

    [Header("Core References")]
    public Transform player;
    public Camera sceneCamera;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;
    public GirlPowerOrla3DAudio audioController;
    public GirlPowerOrla3DDialogue dialogueController;
    public GirlPowerOrla3DPaintSystem paintSystem;
    public GirlPowerOrla3DPetCompanion petDog;
    public GirlPowerOrla3DPetCompanion petCat;
    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DFirstDelivery storyController;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DCardapioUI cardapioUI;
    public GirlPowerOrla3DFairBusinesses fairBusinesses;

    [Header("Shops & Friends")]
    public ShopStall[] shops;
    public FriendEntry[] friends;

    public int Coins { get; private set; } = 60;
    public bool IsShopMenuOpen => openShopIndex >= 0 || (cardapioUI != null && cardapioUI.IsOpen);

    private int openShopIndex = -1;
    private readonly List<string> bagItems = new List<string>();
    private readonly Dictionary<string, int> friendshipHearts = new Dictionary<string, int>();
    private string activityToast = "";
    private float toastTimer;

    private static readonly Color[] OutfitColors =
    {
        new Color(0.95f, 0.43f, 0.59f),
        new Color(0.31f, 0.78f, 0.71f),
        new Color(1.00f, 0.83f, 0.37f),
        new Color(0.42f, 0.75f, 0.94f),
        new Color(0.72f, 0.61f, 0.88f)
    };

    private void Start()
    {
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (storyController == null) storyController = FindObjectOfType<GirlPowerOrla3DFirstDelivery>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (cardapioUI == null) cardapioUI = FindObjectOfType<GirlPowerOrla3DCardapioUI>();
        if (fairBusinesses == null) fairBusinesses = FindObjectOfType<GirlPowerOrla3DFairBusinesses>();

        if (friends != null)
        {
            for (int i = 0; i < friends.Length; i++)
                if (!string.IsNullOrEmpty(friends[i].friendName))
                    friendshipHearts[friends[i].friendName] = 1;
        }
    }

    public void AddCoins(int amount)
    {
        Coins = Mathf.Max(0, Coins + amount);
    }

    public void SetCoinsDirect(int value)
    {
        Coins = Mathf.Max(0, value);
    }

    public string[] CaptureBagItemsForChapterAttempt()
    {
        return bagItems.ToArray();
    }

    public void RestoreBagItemsForChapterAttempt(string[] snapshot)
    {
        bagItems.Clear();
        if (snapshot != null) bagItems.AddRange(snapshot);
        openShopIndex = -1;
        activityToast = "";
        toastTimer = 0f;
    }

    public int BagItemsCount => bagItems.Count;

    public void AddItemToBag(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        bagItems.Add(itemName);
    }

    private void Update()
    {
        if (player == null) return;
        if (gameFlow != null && !gameFlow.IsPlaying)
        {
            openShopIndex = -1;
            return;
        }

        if (toastTimer > 0f) toastTimer = Mathf.Max(0f, toastTimer - Time.deltaTime);

        bool talking = dialogueController != null && dialogueController.IsOpen;
        if (!talking && !IsShopMenuOpen && (megaSystems == null || !megaSystems.IsAnyModalOpen))
            SubmitShopCandidate();

        Keyboard keys = Keyboard.current;
        if (keys == null || talking) return;

        if (keys.escapeKey.wasPressedThisFrame && openShopIndex >= 0)
            openShopIndex = -1;

        if (keys.cKey.wasPressedThisFrame && paintSystem != null)
        {
            paintSystem.CycleColor();
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "Tinta: " + paintSystem.CurrentColorName, paintSystem.CurrentColor, 1.2f);
        }

        if (keys.pKey.wasPressedThisFrame)
            TriggerPaintAction();

        if (keys.gKey.wasPressedThisFrame)
            TriggerFriendAction();

        if (keys.bKey.wasPressedThisFrame)
            TriggerPetAction();

        if (keys.eKey.wasPressedThisFrame && GirlPowerOrla3DInteractionHub.Instance == null)
        {
            int nearShop = FindNearestShop(2.8f);
            if (nearShop >= 0)
            {
                OpenShopAsCardapio(nearShop);
            }
        }

        if (openShopIndex >= 0 && FindNearestShop(3.8f) != openShopIndex)
            openShopIndex = -1;
    }

    private void SubmitShopCandidate()
    {
        var hub = GirlPowerOrla3DInteractionHub.Instance;
        if (hub == null || shops == null || player == null) return;
        int nearShop = FindNearestShop(2.8f);
        if (nearShop < 0 || shops[nearShop].stallTransform == null) return;
        // Let GirlPowerOrla3DFairBusinesses own the interactive Juice Bar, Candy Shop & Craft Studio!
        if (fairBusinesses != null && fairBusinesses.OwnsStallTransform(shops[nearShop].stallTransform))
            return;
        float dist = Vector2.Distance(
            new Vector2(player.position.x, player.position.z),
            new Vector2(shops[nearShop].stallTransform.position.x, shops[nearShop].stallTransform.position.z));
        int capturedIdx = nearShop;
        hub.SubmitCandidate(new GirlPowerOrla3DInteractionHub.Candidate
        {
            id = "shop_" + capturedIdx,
            target = shops[capturedIdx].stallTransform,
            buttonLabel = "Ver " + shops[capturedIdx].shopName,
            targetBadgeLabel = shops[capturedIdx].shopName,
            distance = dist,
            markerHeight = 2.15f,
            isPrimaryMission = false,
            onExecute = () => OpenShopAsCardapio(capturedIdx)
        });
    }

    public void OpenShopAsCardapio(int shopIdx)
    {
        if (cardapioUI != null && shops != null && shopIdx >= 0 && shopIdx < shops.Length)
        {
            string nm = shops[shopIdx].shopName.ToLowerInvariant();
            GirlPowerOrla3DCardapioUI.Tab tab = GirlPowerOrla3DCardapioUI.Tab.Comidas;
            if (nm.Contains("roupa")) tab = GirlPowerOrla3DCardapioUI.Tab.RoupasPets;
            else if (nm.Contains("sushi")) tab = GirlPowerOrla3DCardapioUI.Tab.SushiMar;
            else if (nm.Contains("doce")) tab = GirlPowerOrla3DCardapioUI.Tab.SorvetesDoces;
            else if (nm.Contains("coco") || nm.Contains("suco") || nm.Contains("refri")) tab = GirlPowerOrla3DCardapioUI.Tab.Bebidas;
            else if (nm.Contains("artesanato") || nm.Contains("planta")) tab = GirlPowerOrla3DCardapioUI.Tab.Artesanatos;
            cardapioUI.Open(tab);
            return;
        }
        openShopIndex = (openShopIndex == shopIdx) ? -1 : shopIdx;
        if (audioController != null) audioController.PlayChime();
    }

    public void TriggerPaintAction()
    {
        if (paintSystem == null || player == null) return;
        int nearHouse = paintSystem.FindNearestHouse(player.position, 5.4f);
        if (nearHouse >= 0)
        {
            if (megaSystems != null)
            {
                megaSystems.OpenManualPaintRoller(nearHouse);
                return;
            }
            string msg = paintSystem.PaintHouse(nearHouse);
            Coins += 3;
            if (characterAnimator != null) characterAnimator.TriggerCelebration();
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "+3 moedas  •  Casa Pintada!", new Color(1f, 0.88f, 0.36f));
            ShowToast(msg);
            return;
        }

        if (paintSystem.PaintStreetAt(player.position))
        {
            Coins += 1;
            if (audioController != null) audioController.PlayPaintBrush();
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "+1 moeda", new Color(1f, 0.88f, 0.36f), 1.1f);
        }
        else
        {
            ShowToast("Vá até o calçadão ou perto de uma casa para pintar!");
        }
    }

    public void TriggerPetAction()
    {
        if (player == null) return;
        GirlPowerOrla3DPetCompanion targetPet = FindNearestPet(3.5f);
        if (targetPet == null)
            targetPet = petDog != null ? petDog : petCat;
        if (targetPet == null) return;

        if (!targetPet.isCat && targetPet.isFollowing)
        {
            targetPet.TriggerSniffTreasure();
            return;
        }

        targetPet.ToggleFollow();
        if (audioController != null) audioController.PlayPetSound(targetPet.isCat);
        if (characterAnimator != null) characterAnimator.SpawnHeartBurst(targetPet.transform.position);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(targetPet.transform.position,
            targetPet.isFollowing ? ("♥ " + targetPet.petName + " veio passear! [B fareja]") : (targetPet.petName + " descansando"),
            new Color(1f, 0.72f, 0.84f));
    }

    public void TriggerFriendAction()
    {
        int idx = FindNearestFriend(3.5f);
        if (idx < 0)
        {
            ShowToast("Chegue perto de um vizinho para conversar ou presentear!");
            return;
        }

        FriendEntry f = friends[idx];
        int hearts = friendshipHearts.TryGetValue(f.friendName, out int cur) ? cur : 1;
        string gifted = "";
        if (bagItems.Count > 0)
        {
            gifted = bagItems[bagItems.Count - 1];
            bagItems.RemoveAt(bagItems.Count - 1);
            hearts = Mathf.Min(5, hearts + 1);
        }
        else
        {
            hearts = Mathf.Min(5, hearts + 1);
        }
        friendshipHearts[f.friendName] = hearts;
        Coins += 4;

        if (audioController != null) audioController.PlayFanfare();
        if (characterAnimator != null) characterAnimator.SpawnHeartBurst(f.target.position);
        GirlPowerOrla3DUITheme.SpawnFloatingFeedback(f.target.position, "+♥ Amizade  (+4 moedas)", new Color(1f, 0.56f, 0.72f));

        string heartBar = new string('♥', hearts) + new string('♡', 5 - hearts);
        if (dialogueController != null)
        {
            string line = !string.IsNullOrEmpty(gifted)
                ? ("Amei o presente (" + gifted + ")!! Que carinho incrível — nossa amizade agora está " + heartBar + "!")
                : ("Que delícia passear pela Baía do Avesso com você! Nossa amizade agora está " + heartBar + "!");
            dialogueController.ShowDialogue(f.friendName + "  " + heartBar, f.tagColor, f.voicePitch, new[] { line });
        }
    }

    private void BuyItem(int shopIdx, int itemIdx)
    {
        ShopStall shop = shops[shopIdx];
        string item = shop.itemNames[itemIdx];
        int price = shop.itemPrices[itemIdx];

        if (Coins < price)
        {
            ShowToast("Faltam moedas (" + price + ")! Faça entregas ou pinte a orla para ganhar mais.");
            return;
        }

        Coins -= price;
        if (audioController != null) audioController.PlayFanfare();

        if (shop.isClothingShop)
        {
            Color col = OutfitColors[itemIdx % OutfitColors.Length];
            if (characterAnimator != null) characterAnimator.SetOutfitColor(col);
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "★ " + item + " equipada!", col);
            openShopIndex = -1;
        }
        else
        {
            bagItems.Add(item);
            if (characterAnimator != null) characterAnimator.TriggerCelebration();
            GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, "+1 " + item, new Color(0.64f, 0.96f, 0.88f));
        }
    }

    private int FindNearestShop(float radius)
    {
        if (shops == null || player == null) return -1;
        int best = -1;
        float bestD = radius;
        Vector2 p = new Vector2(player.position.x, player.position.z);
        for (int i = 0; i < shops.Length; i++)
        {
            if (shops[i].stallTransform == null) continue;
            Vector2 s = new Vector2(shops[i].stallTransform.position.x, shops[i].stallTransform.position.z);
            float d = Vector2.Distance(p, s);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private int FindNearestFriend(float radius)
    {
        if (friends == null || player == null) return -1;
        int best = -1;
        float bestD = radius;
        Vector2 p = new Vector2(player.position.x, player.position.z);
        for (int i = 0; i < friends.Length; i++)
        {
            if (friends[i].target == null || !friends[i].target.gameObject.activeInHierarchy) continue;
            Vector2 f = new Vector2(friends[i].target.position.x, friends[i].target.position.z);
            float d = Vector2.Distance(p, f);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private GirlPowerOrla3DPetCompanion FindNearestPet(float radius)
    {
        GirlPowerOrla3DPetCompanion best = null;
        float bestD = radius;
        Vector2 p = new Vector2(player.position.x, player.position.z);
        GirlPowerOrla3DPetCompanion[] list = { petDog, petCat };
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == null) continue;
            Vector2 t = new Vector2(list[i].transform.position.x, list[i].transform.position.z);
            float d = Vector2.Distance(p, t);
            if (d < bestD) { bestD = d; best = list[i]; }
        }
        return best;
    }

    private void ShowToast(string text)
    {
        activityToast = text;
        toastTimer = 3.2f;
    }

    private void OnGUI()
    {
        if (player == null) return;
        if (gameFlow != null && !gameFlow.IsPlaying) return;
        if (dialogueController != null && dialogueController.IsOpen) return;

        GirlPowerOrla3DUITheme.EnsureInitialized();
        GUI.color = Color.white;
        float s = Mathf.Clamp(Screen.width / 1440f, 0.82f, 1.15f);

        // ------------------------------------------------------------------
        // 15%-SCREEN RULE: Bottom & Center 85%+ remain 100% CLEAN for finger dragging!
        // Coins, Bag, Cardápio, Mobility & Pipoca live in compact Top-Left / Top-Right corners.
        // Only tiny contextual icon buttons (Paint / Friend Gift) tuck under Top-Right when nearby!
        // ------------------------------------------------------------------
        float ctxX = Screen.width - 16f * s;
        float ctxY = 82f * s;
        float iconSz = 44f * s;
        float gap = 8f * s;

        int nearHouse = paintSystem != null ? paintSystem.FindNearestHouse(player.position, 5.8f) : -1;
        if (nearHouse >= 0)
        {
            ctxX -= iconSz;
            Rect swatchRect = new Rect(ctxX, ctxY, iconSz, iconSz);
            if (GUI.Button(swatchRect, GUIContent.none, GirlPowerOrla3DUITheme.BtnTileStyle))
            {
                paintSystem.CycleColor();
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(player.position, paintSystem.CurrentColorName, paintSystem.CurrentColor, 1.1f);
            }
            GUI.color = paintSystem.CurrentColor;
            GUI.DrawTexture(new Rect(swatchRect.x + 10f * s, swatchRect.y + 10f * s, 24f * s, 24f * s), GirlPowerOrla3DUITheme.CircleWhiteTex);
            GUI.color = Color.white;
            ctxX -= (iconSz + gap);

            Rect paintBtn = new Rect(ctxX, ctxY, iconSz, iconSz);
            if (GUI.Button(paintBtn, GUIContent.none, GirlPowerOrla3DUITheme.BtnGlassIconStyle))
                TriggerPaintAction();
            GUI.DrawTexture(new Rect(paintBtn.x + 8f * s, paintBtn.y + 8f * s, 28f * s, 28f * s), GirlPowerOrla3DUITheme.IconBrush);
            ctxX -= gap;
        }

        int nearFriend = FindNearestFriend(3.5f);
        if (nearFriend >= 0)
        {
            ctxX -= iconSz;
            Rect heartRect = new Rect(ctxX, ctxY, iconSz, iconSz);
            if (GUI.Button(heartRect, GUIContent.none, GirlPowerOrla3DUITheme.BtnGlassIconStyle))
                TriggerFriendAction();
            GUI.DrawTexture(new Rect(heartRect.x + 8f * s, heartRect.y + 8f * s, 28f * s, 28f * s), GirlPowerOrla3DUITheme.IconHeart);
        }

        // Shop action button is unified inside GirlPowerOrla3DInteractionHub when present!
        if (GirlPowerOrla3DInteractionHub.Instance == null)
        {
            bool storyHasAction = storyController != null && storyController.HasNearbyStoryInteractable;
            int nearShop = FindNearestShop(2.8f);
            if (nearShop >= 0 && !storyHasAction && !IsShopMenuOpen && (megaSystems == null || !megaSystems.IsAnyModalOpen))
            {
                string shopTxt = "[E]  " + shops[nearShop].shopName;
                float sw = Mathf.Clamp(92f * s + shopTxt.Length * 7.8f * s, 215f * s, 360f * s);
                float sh = 46f * s;
                float sx = (Screen.width - sw) * 0.5f;
                float sy = Screen.height - 118f * s;
                if (GUI.Button(new Rect(sx, sy, sw, sh), shopTxt, GirlPowerOrla3DUITheme.BtnSecondaryStyle))
                    OpenShopAsCardapio(nearShop);
            }
        }

        // Rounded Cozy Shop Modal
        if (openShopIndex >= 0 && shops != null && openShopIndex < shops.Length)
        {
            ShopStall shop = shops[openShopIndex];
            float sw = 520f * s;
            float sh = 274f * s;
            Rect modal = new Rect((Screen.width - sw) * 0.5f, (Screen.height - sh) * 0.5f, sw, sh);
            GUI.Box(modal, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

            Rect head = new Rect(modal.x + 32f * s, modal.y - 18f * s, sw - 64f * s, 40f * s);
            GUI.Box(head, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
            GUI.Label(head, shop.shopName + "  •  " + shop.category, GirlPowerOrla3DUITheme.BadgeTextStyle);

            int count = shop.itemNames != null ? shop.itemNames.Length : 0;
            for (int i = 0; i < count; i++)
            {
                float rowY = modal.y + 40f * s + i * 38f * s;
                string lbl = shop.itemNames[i] + "   —   " + shop.itemPrices[i] + " moedas";
                if (GUI.Button(new Rect(modal.x + 26f * s, rowY, sw - 52f * s, 34f * s), lbl, GirlPowerOrla3DUITheme.BtnTileStyle))
                    BuyItem(openShopIndex, i);
            }

            if (GUI.Button(new Rect(modal.x + (sw - 140f * s) * 0.5f, modal.yMax - 44f * s, 140f * s, 34f * s), "FECHAR", GirlPowerOrla3DUITheme.BtnSecondaryStyle))
                openShopIndex = -1;
        }

        // Water sports (Surf / Nadar / Canoa) intentionally removed — ocean is purely scenic!
        if (toastTimer > 0f && !string.IsNullOrEmpty(activityToast))
        {
            Rect tr = new Rect((Screen.width - 480f * s) * 0.5f, 70f * s, 480f * s, 36f * s);
            GUI.Box(tr, GUIContent.none, GirlPowerOrla3DUITheme.CardGlassStyle);
            GUI.Label(tr, activityToast, GirlPowerOrla3DUITheme.BodyLightStyle);
        }
    }

    private static void DrawKeyCornerBadge(Rect parent, string key, float s)
    {
        float bw = 18f * s;
        Rect br = new Rect(parent.xMax - bw + 2f * s, parent.y - 4f * s, bw, bw);
        GUI.Box(br, GUIContent.none, GirlPowerOrla3DUITheme.PillTealStyle);
        GUIStyle st = new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(10f * s) };
        GUI.Label(br, key, st);
    }
}
