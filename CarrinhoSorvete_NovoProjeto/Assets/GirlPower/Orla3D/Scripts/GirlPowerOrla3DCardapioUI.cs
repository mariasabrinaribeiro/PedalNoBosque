using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Illustrated Grid Cardápio & Coastal Store Modal inspired by "Cozinha do Bosque":
/// - Cream rounded panel with dark-teal header & coral circular 'X' close button
/// - 2 rows of 6 illustrated pastel square cards (mint top row + warm cream bottom row)
/// - 2 rows of colorful pill category/action buttons underneath:
///   Comidas & Batata | Bebidas & Sucos | Sorvetes & Doces | Sushi & Mar |
///   Roupas & Pets    | Artesanatos     | Horta / Pescar   | Upgrades & Receitas
/// </summary>
public sealed class GirlPowerOrla3DCardapioUI : MonoBehaviour
{
    public enum Tab
    {
        Comidas = 0,
        Bebidas = 1,
        SorvetesDoces = 2,
        SushiMar = 3,
        RoupasPets = 4,
        Artesanatos = 5
    }

    private struct MenuItem
    {
        public string name;
        public int price;
        public Color iconBg;
        public int iconKind; // 0=fries, 1=drink, 2=icecream, 3=sushi, 4=shirt, 5=craft, 6=sweet, 7=mobility
        public Action onSelect;
    }

    public GirlPowerOrla3DGameFlow gameFlow;
    public GirlPowerOrla3DLifeActivities lifeActivities;
    public GirlPowerOrla3DMegaSystems megaSystems;
    public GirlPowerOrla3DCharacterAnimator characterAnimator;
    public GirlPowerOrla3DPetCompanion petDog;
    public GirlPowerOrla3DAudio audioController;

    public bool IsOpen { get; private set; }
    private Tab currentTab = Tab.Comidas;
    private readonly System.Collections.Generic.Dictionary<string, int> ownedCounts = new System.Collections.Generic.Dictionary<string, int>();
    private string lastBoughtName = "";
    private float buyFeedbackTimer;
    private string buyFeedbackBanner = "";

    private GUIStyle cardMintStyle;
    private GUIStyle cardWarmStyle;
    private GUIStyle tabInactiveStyle;
    private GUIStyle tabActiveStyle;
    private bool stylesReady;

    private static readonly Color[] ShirtPalette =
    {
        new Color(0.95f, 0.43f, 0.59f),
        new Color(0.31f, 0.78f, 0.71f),
        new Color(1.00f, 0.83f, 0.37f),
        new Color(0.42f, 0.75f, 0.94f),
        new Color(0.72f, 0.61f, 0.88f)
    };

    private void Start()
    {
        if (gameFlow == null) gameFlow = FindObjectOfType<GirlPowerOrla3DGameFlow>();
        if (lifeActivities == null) lifeActivities = FindObjectOfType<GirlPowerOrla3DLifeActivities>();
        if (megaSystems == null) megaSystems = FindObjectOfType<GirlPowerOrla3DMegaSystems>();
        if (characterAnimator == null) characterAnimator = FindObjectOfType<GirlPowerOrla3DCharacterAnimator>();
    }

    public void Open(Tab tab = Tab.Comidas)
    {
        currentTab = tab;
        IsOpen = true;
        if (audioController != null) audioController.PlayPaperFlip();
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open(currentTab);
    }

    private void Update()
    {
        if (buyFeedbackTimer > 0f)
            buyFeedbackTimer = Mathf.Max(0f, buyFeedbackTimer - Time.unscaledDeltaTime);

        if (gameFlow != null && !gameFlow.IsPlaying)
        {
            IsOpen = false;
            return;
        }
        Keyboard keys = Keyboard.current;
        if (keys == null) return;
        if (keys.tabKey.wasPressedThisFrame)
            Toggle();
        else if (IsOpen && keys.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void OnGUI()
    {
        if (!IsOpen) return;
        if (gameFlow != null && !gameFlow.IsPlaying) return;

        GirlPowerOrla3DUITheme.EnsureInitialized();
        EnsureCardapioStyles();

        GUI.depth = -24;
        GUI.color = Color.white;
        float s = Mathf.Clamp(Screen.width / 1600f, 0.66f, 1.15f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), GirlPowerOrla3DUITheme.DimOverlayTex);

        float mw = 820f * s;
        float mh = 524f * s;
        Rect modal = new Rect((Screen.width - mw) * 0.5f, (Screen.height - mh) * 0.5f, mw, mh);
        GUI.Box(modal, GUIContent.none, GirlPowerOrla3DUITheme.CardCreamStyle);

        // Header: Clean, coherent economic title (Feira de Insumos & Presentes vs Moda & Esportes)
        int coins = lifeActivities != null ? lifeActivities.Coins : 0;
        GUIStyle headSt = new GUIStyle(GirlPowerOrla3DUITheme.HeadingStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = Mathf.RoundToInt(22f * s),
            normal = { textColor = new Color(0.18f, 0.44f, 0.34f) }
        };
        string headerTitle = currentTab == Tab.RoupasPets
            ? ("Moda Praia & Esportes  •  " + coins + " moedas")
            : ("Feira da Baía — " + GetTabDisplayName(currentTab) + "  •  " + coins + " moedas");
        GUI.Label(new Rect(modal.x + 28f * s, modal.y + 14f * s, mw - 120f * s, 36f * s), headerTitle, headSt);

        float xSz = 40f * s;
        Rect closeRect = new Rect(modal.xMax - xSz - 14f * s, modal.y + 12f * s, xSz, xSz);
        GUI.DrawTexture(closeRect, GirlPowerOrla3DUITheme.CircleCoralTex);
        if (GUI.Button(closeRect, "X", GirlPowerOrla3DUITheme.BadgeTextStyle))
        {
            Close();
            return;
        }

        // Clean 2x3 Grid of 6 Large, Legible Illustrated Cards (spacious typography & large icons!)
        MenuItem[] items = GetItemsForTab(currentTab);
        int cols = 3;
        float padX = 28f * s;
        float gap = 14f * s;
        float cardW = (mw - padX * 2f - gap * (cols - 1)) / cols;
        float cardH = 122f * s;
        float gridY = modal.y + 62f * s;

        GUIStyle lblSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = Mathf.RoundToInt(14.5f * s),
            wordWrap = true
        };
        GUIStyle priceSt = new GUIStyle(GirlPowerOrla3DUITheme.SmallMutedStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = Mathf.RoundToInt(12.5f * s),
            normal = { textColor = new Color(0.18f, 0.44f, 0.34f) }
        };

        for (int i = 0; i < items.Length && i < 6; i++)
        {
            int r = i / cols;
            int c = i % cols;
            Rect cr = new Rect(modal.x + padX + c * (cardW + gap), gridY + r * (cardH + gap), cardW, cardH);

            bool justBought = buyFeedbackTimer > 0.7f && lastBoughtName == items[i].name;
            if (justBought)
            {
                float expand = 4f * s;
                cr = new Rect(cr.x - expand, cr.y - expand, cr.width + expand * 2f, cr.height + expand * 2f);
            }

            GUIStyle st = r == 0 ? cardMintStyle : cardWarmStyle;
            if (GUI.Button(cr, GUIContent.none, st))
                HandleItemPurchase(items[i]);

            // Large circular illustrated emblem on the left of each spacious card
            float emSz = 56f * s;
            Rect emRect = new Rect(cr.x + 14f * s, cr.y + (cr.height - emSz) * 0.5f, emSz, emSz);
            GUI.color = items[i].iconBg;
            GUI.DrawTexture(emRect, GirlPowerOrla3DUITheme.CircleWhiteTex);
            GUI.color = Color.white;
            Texture2D ic = ResolveItemIcon(items[i].iconKind);
            if (ic != null)
                GUI.DrawTexture(new Rect(emRect.x + 8f * s, emRect.y + 8f * s, emSz - 16f * s, emSz - 16f * s), ic);

            // Owned quantity badge (x1, x2...) on top-right corner
            if (ownedCounts.TryGetValue(items[i].name, out int qty) && qty > 0)
            {
                Rect qBadge = new Rect(cr.xMax - 36f * s, cr.y + 8f * s, 28f * s, 22f * s);
                GUI.Box(qBadge, GUIContent.none, GirlPowerOrla3DUITheme.PillCoralStyle);
                GUI.Label(qBadge, "x" + qty, new GUIStyle(GirlPowerOrla3DUITheme.BadgeTextStyle) { fontSize = Mathf.RoundToInt(11f * s) });
            }

            float textX = emRect.xMax + 12f * s;
            float textW = cr.xMax - textX - 12f * s;
            GUI.Label(new Rect(textX, cr.y + 20f * s, textW, 48f * s), items[i].name, lblSt);
            string pTxt = items[i].price > 0 ? (items[i].price + " moedas") : "Equipar agora";
            GUI.Label(new Rect(textX, cr.yMax - 36f * s, textW, 24f * s), pTxt, priceSt);
        }

        // Calm neutral category tabs — strong accent color reserved STRICTLY for the active tab!
        float pillY = gridY + 2f * (cardH + gap) + 14f * s;
        float pillGap = 12f * s;
        float pillW = (mw - padX * 2f - pillGap * 2f) / 3f;
        float pillH = 40f * s;

        DrawCategoryTab(new Rect(modal.x + padX + 0f * (pillW + pillGap), pillY, pillW, pillH), "Comidas & Batata", Tab.Comidas);
        DrawCategoryTab(new Rect(modal.x + padX + 1f * (pillW + pillGap), pillY, pillW, pillH), "Água, Refri & Sucos", Tab.Bebidas);
        DrawCategoryTab(new Rect(modal.x + padX + 2f * (pillW + pillGap), pillY, pillW, pillH), "Sorvetes & Doces", Tab.SorvetesDoces);

        float pillY2 = pillY + pillH + 10f * s;
        DrawCategoryTab(new Rect(modal.x + padX + 0f * (pillW + pillGap), pillY2, pillW, pillH), "Sushi & Mar", Tab.SushiMar);
        DrawCategoryTab(new Rect(modal.x + padX + 1f * (pillW + pillGap), pillY2, pillW, pillH), "Artesanatos", Tab.Artesanatos);
        DrawCategoryTab(new Rect(modal.x + padX + 2f * (pillW + pillGap), pillY2, pillW, pillH), "Moda & Esportes", Tab.RoupasPets);

        // Live purchase confirmation banner OR cozy hint at bottom!
        GUIStyle footSt = new GUIStyle(GirlPowerOrla3DUITheme.BodyDarkStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13.5f * s),
            normal = { textColor = buyFeedbackTimer > 0f ? new Color(0.14f, 0.48f, 0.28f) : new Color(0.32f, 0.24f, 0.18f) }
        };
        string footTxt = buyFeedbackTimer > 0f && !string.IsNullOrEmpty(buyFeedbackBanner)
            ? buyFeedbackBanner
            : "Compre insumos e presentes da feira para presentear vizinhos [G] ou equipe roupas e esportes!";
        GUI.Label(new Rect(modal.x + 20f * s, modal.yMax - 36f * s, mw - 40f * s, 26f * s), footTxt, footSt);
    }

    private void DrawCategoryTab(Rect rect, string label, Tab tab)
    {
        bool isActive = currentTab == tab;
        if (GUI.Button(rect, label, isActive ? tabActiveStyle : tabInactiveStyle))
            SelectTab(tab);
    }

    private void SelectTab(Tab t)
    {
        currentTab = t;
        if (audioController != null) audioController.PlayPaperFlip();
    }

    private void HandleItemPurchase(MenuItem item)
    {
        if (item.price > 0)
        {
            if (lifeActivities == null || lifeActivities.Coins < item.price)
            {
                if (audioController != null) audioController.PlayFailChime();
                buyFeedbackBanner = "Moedas insuficientes (" + item.price + "m necessários)!";
                buyFeedbackTimer = 2.0f;
                return;
            }
            lifeActivities.AddCoins(-item.price);
            if (megaSystems != null) megaSystems.SaveJson();
        }

        lastBoughtName = item.name;
        buyFeedbackTimer = 1.6f;

        if (item.onSelect != null)
        {
            item.onSelect.Invoke();
            buyFeedbackBanner = "✓ " + item.name + (item.price > 0 ? (" (-" + item.price + "m)") : " equipado!");
            if (audioController != null) audioController.PlayCoinClink(1.15f);
        }
        else if (lifeActivities != null)
        {
            lifeActivities.AddItemToBag(item.name);
            int nextQty = ownedCounts.TryGetValue(item.name, out int cur) ? cur + 1 : 1;
            ownedCounts[item.name] = nextQty;
            buyFeedbackBanner = "✓ Adquirido: " + item.name + " (x" + nextQty + ")  -" + item.price + "m";
            if (audioController != null) audioController.PlayCoinClink(1.05f + Mathf.Min(0.4f, nextQty * 0.05f));
            if (lifeActivities.player != null)
                GirlPowerOrla3DUITheme.SpawnFloatingFeedback(lifeActivities.player.position, "+1 " + item.name + " (-" + item.price + "m)", new Color(0.58f, 0.96f, 0.86f));
        }
    }

    private MenuItem[] GetItemsForTab(Tab tab)
    {
        Color mint = new Color(0.84f, 0.96f, 0.88f);
        Color warm = new Color(1.0f, 0.95f, 0.84f);
        Color pink = new Color(1.0f, 0.86f, 0.91f);
        Color sky = new Color(0.84f, 0.94f, 1.0f);

        switch (tab)
        {
            case Tab.Comidas:
                return new[]
                {
                    new MenuItem { name = "Batata Frita", price = 6, iconBg = warm, iconKind = 0 },
                    new MenuItem { name = "Batata Cheddar", price = 8, iconBg = warm, iconKind = 0 },
                    new MenuItem { name = "Pastel Dourado", price = 5, iconBg = warm, iconKind = 0 },
                    new MenuItem { name = "Tigela de Açaí", price = 7, iconBg = pink, iconKind = 2 },
                    new MenuItem { name = "Peixe Grelhado", price = 9, iconBg = sky, iconKind = 3 },
                    new MenuItem { name = "Hambúrguer", price = 8, iconBg = mint, iconKind = 0 }
                };

            case Tab.Bebidas:
                return new[]
                {
                    new MenuItem { name = "Água Mineral", price = 3, iconBg = sky, iconKind = 1 },
                    new MenuItem { name = "Água de Coco", price = 4, iconBg = mint, iconKind = 1 },
                    new MenuItem { name = "Refri Guaraná", price = 5, iconBg = mint, iconKind = 1 },
                    new MenuItem { name = "Suco de Limão", price = 5, iconBg = mint, iconKind = 1 },
                    new MenuItem { name = "Suco Morango", price = 6, iconBg = pink, iconKind = 1 },
                    new MenuItem { name = "Limonada Suíça", price = 6, iconBg = sky, iconKind = 1 }
                };

            case Tab.SorvetesDoces:
                return new[]
                {
                    new MenuItem { name = "Limão Manjericão", price = 6, iconBg = mint, iconKind = 2 },
                    new MenuItem { name = "Morango Hortelã", price = 6, iconBg = pink, iconKind = 2 },
                    new MenuItem { name = "Trio da Festa", price = 9, iconBg = pink, iconKind = 2 },
                    new MenuItem { name = "Brigadeiro", price = 4, iconBg = warm, iconKind = 6 },
                    new MenuItem { name = "Cocada Cremosa", price = 5, iconBg = warm, iconKind = 6 },
                    new MenuItem { name = "Cupcake Berry", price = 6, iconBg = pink, iconKind = 6 }
                };

            case Tab.SushiMar:
                return new[]
                {
                    new MenuItem { name = "Combo Salmão", price = 10, iconBg = pink, iconKind = 3 },
                    new MenuItem { name = "Hot Roll Praia", price = 9, iconBg = warm, iconKind = 3 },
                    new MenuItem { name = "Temaki Especial", price = 9, iconBg = mint, iconKind = 3 },
                    new MenuItem { name = "Ceviche Baía", price = 10, iconBg = sky, iconKind = 3 },
                    new MenuItem { name = "Sunomono", price = 5, iconBg = mint, iconKind = 3 },
                    new MenuItem { name = "Barca Família", price = 16, iconBg = pink, iconKind = 3 }
                };

            case Tab.RoupasPets:
                return new[]
                {
                    MakeOutfitItem("Blusa Rosa Berry", 6, 0, pink),
                    MakeOutfitItem("Blusa Menta Praia", 6, 1, mint),
                    new MenuItem
                    {
                        name = "Chapéu + Óculos", price = 10, iconBg = warm, iconKind = 4,
                        onSelect = () =>
                        {
                            if (characterAnimator != null) characterAnimator.SetWardrobeHatVisible(!characterAnimator.HasHatEquipped);
                            if (megaSystems != null) { megaSystems.Data.hasHat = characterAnimator != null && characterAnimator.HasHatEquipped; megaSystems.SaveJson(); }
                            if (audioController != null) audioController.PlayFanfare();
                        }
                    },
                    new MenuItem
                    {
                        name = "Bandana Pipoca", price = 8, iconBg = pink, iconKind = 6,
                        onSelect = () =>
                        {
                            if (petDog != null) petDog.SetBandanaVisible(!petDog.HasBandanaEquipped);
                            if (megaSystems != null) { megaSystems.Data.hasBandana = petDog != null && petDog.HasBandanaEquipped; megaSystems.SaveJson(); }
                            if (audioController != null) audioController.PlayPetSound(false);
                        }
                    },
                    MakeMobilityItem("Skate / Patins", GirlPowerOrla3DCharacterAnimator.LandMode.Skate, pink),
                    MakeMobilityItem("Patinete da Orla", GirlPowerOrla3DCharacterAnimator.LandMode.Scooter, mint)
                };

            default: // Artesanatos
                return new[]
                {
                    new MenuItem { name = "Colar Conchas", price = 6, iconBg = sky, iconKind = 5 },
                    new MenuItem { name = "Pulseira Baía", price = 5, iconBg = pink, iconKind = 5 },
                    new MenuItem { name = "Mini Quadro", price = 8, iconBg = warm, iconKind = 5 },
                    new MenuItem { name = "Filtro Sonhos", price = 9, iconBg = mint, iconKind = 5 },
                    new MenuItem { name = "Vela Aromática", price = 6, iconBg = pink, iconKind = 5 },
                    new MenuItem { name = "Kit Presente", price = 12, iconBg = pink, iconKind = 5 }
                };
        }
    }

    private MenuItem MakeOutfitItem(string label, int price, int paletteIdx, Color bg)
    {
        return new MenuItem
        {
            name = label,
            price = price,
            iconBg = bg,
            iconKind = 4,
            onSelect = () =>
            {
                if (characterAnimator != null) characterAnimator.SetOutfitColor(ShirtPalette[paletteIdx % ShirtPalette.Length]);
                if (audioController != null) audioController.PlayFanfare();
            }
        };
    }

    private MenuItem MakeMobilityItem(string label, GirlPowerOrla3DCharacterAnimator.LandMode mode, Color bg)
    {
        return new MenuItem
        {
            name = label,
            price = 0,
            iconBg = bg,
            iconKind = 7,
            onSelect = () =>
            {
                if (characterAnimator != null) characterAnimator.SetLandMode(mode);
                if (audioController != null) audioController.PlayChime();
                Close();
            }
        };
    }

    private static Texture2D ResolveItemIcon(int kind)
    {
        switch (kind)
        {
            case 0: return GirlPowerOrla3DUITheme.IconFestivalTrio;
            case 1: return GirlPowerOrla3DUITheme.IconWaveLow;
            case 2: return GirlPowerOrla3DUITheme.IconIceCreamLemonBasil;
            case 3: return GirlPowerOrla3DUITheme.IconWaveHigh;
            case 4: return GirlPowerOrla3DUITheme.IconHeart;
            case 5: return GirlPowerOrla3DUITheme.IconBag;
            case 6: return GirlPowerOrla3DUITheme.IconPaw;
            default: return GirlPowerOrla3DUITheme.IconStarFilled;
        }
    }

    private static string GetTabDisplayName(Tab t)
    {
        switch (t)
        {
            case Tab.Bebidas: return "Água, Refri & Sucos";
            case Tab.SorvetesDoces: return "Sorvetes & Doces";
            case Tab.SushiMar: return "Sushi & Frutos do Mar";
            case Tab.RoupasPets: return "Roupas, Pets & Esportes";
            case Tab.Artesanatos: return "Artesanatos da Baía";
            default: return "Restaurante & Batata Frita";
        }
    }

    private void EnsureCardapioStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        cardMintStyle = MakeColoredStyle(new Color(0.91f, 0.96f, 0.91f, 0.98f), new Color(0.70f, 0.82f, 0.72f, 1f), new Color(0.22f, 0.28f, 0.24f), 13);
        cardWarmStyle = MakeColoredStyle(new Color(0.98f, 0.95f, 0.89f, 0.98f), new Color(0.84f, 0.76f, 0.64f, 1f), new Color(0.28f, 0.22f, 0.18f), 13);

        // Calm neutral cream-sand inactive tabs; vibrant Hero Berry Pink reserved STRICTLY for the active tab!
        tabInactiveStyle = MakeColoredStyle(new Color(0.93f, 0.89f, 0.82f, 1f), new Color(0.78f, 0.72f, 0.62f, 1f), new Color(0.30f, 0.24f, 0.20f), 13);
        tabActiveStyle = MakeColoredStyle(new Color(0.91f, 0.36f, 0.54f, 1f), new Color(1.00f, 0.64f, 0.76f, 1f), Color.white, 13);
    }

    private static GUIStyle MakeColoredStyle(Color fill, Color border, Color txt, int fontSize)
    {
        Texture2D texN = MakeSimpleRoundedTex(fill, border);
        Texture2D texH = MakeSimpleRoundedTex(Color.Lerp(fill, Color.white, 0.14f), Color.white);
        return new GUIStyle(GUI.skin.button)
        {
            normal = { background = texN, textColor = txt },
            hover = { background = texH, textColor = txt },
            active = { background = texN, textColor = txt },
            border = new RectOffset(14, 14, 14, 14),
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private static Texture2D MakeSimpleRoundedTex(Color fill, Color border)
    {
        int w = 48, h = 48, r = 14;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - r - 1f);
                float qy = Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - r - 1f);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                        + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                float cov = Mathf.Clamp01(0.5f - d);
                float inCov = Mathf.Clamp01(0.5f - (d + 2f));
                Color c = Color.Lerp(border, fill, inCov);
                c.a *= cov;
                px[y * w + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }
}
