using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 7 — Diário de Exploração & Oficina do PEDAL NO BOSQUE:
    /// Interface delicada em paleta de livro ilustrado (creme, sálvia, lavanda, rosa e dourado) com 3 abas:
    /// 1. Livro dos Animais (14 páginas com silhuetas suaves para espécies ainda não descobertas);
    /// 2. Colecionáveis (7 categorias) e Conquistas (9 medalhas);
    /// 3. Oficina de Personalização (Bicicletas, Cestas, Acessórios, Personagem e Cachorrinho).
    /// </summary>
    public class DiscoveryBookAndWorkshopUI : MonoBehaviour
    {
        private GameObject modalRoot;
        private GameObject tabAnimalsObj;
        private GameObject tabCollectionsObj;
        private GameObject tabWorkshopObj;

        private TextMeshProUGUI headerStarsText;
        private TextMeshProUGUI footerFeedbackText;

        // Aba 1: Livro dos Animais
        private int currentAnimalPage = 0;
        private Image animalIllustration;
        private TextMeshProUGUI animalPageCounter;
        private TextMeshProUGUI animalNameText;
        private TextMeshProUGUI animalDetailsText;
        private TextMeshProUGUI animalCuriosityText;

        // Aba 2: Colecionáveis & Conquistas
        private TextMeshProUGUI[] collectibleLabels = new TextMeshProUGUI[7];
        private TextMeshProUGUI[] achievementLabels = new TextMeshProUGUI[9];
        private Image[] achievementBadges = new Image[9];

        // Aba 3: Oficina de Personalização
        private CustomizationCategory currentCategory = CustomizationCategory.Bicicletas;
        private Transform workshopItemsContainer;
        private readonly List<GameObject> spawnedItemCards = new List<GameObject>();

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        public void Build(Transform canvasParent)
        {
            // Fundo translúcido acolhedor
            modalRoot = CreatePanel(canvasParent, "DiarioExploracaoModal",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 560f),
                new Color(0.97f, 0.94f, 0.87f, 0.98f)); // Papel pergaminho suave!

            // Faixa superior do Diário em verde-sálvia delicado
            GameObject header = CreatePanel(modalRoot.transform, "Header",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -32f), new Vector2(960f, 64f),
                new Color(0.24f, 0.46f, 0.38f, 1f));

            CreateTMP(header.transform, "Title", "DIÁRIO DO PEDAL NO BOSQUE",
                23, new Vector2(-240f, 0f), new Vector2(440f, 40f), TextAlignmentOptions.Left,
                new Color(1f, 0.97f, 0.86f, 1f));

            headerStarsText = CreateTMP(header.transform, "Stars", "★ 0 Estrelas",
                21, new Vector2(215f, 0f), new Vector2(220f, 36f), TextAlignmentOptions.Right,
                new Color(1f, 0.90f, 0.42f, 1f));

            CreateButton(header.transform, "BtnFechar", "✕ Fechar",
                new Vector2(410f, 0f), new Vector2(110f, 38f),
                new Color(0.76f, 0.36f, 0.42f, 0.95f), 17, Close);

            // 3 Abas Principais
            CreateButton(modalRoot.transform, "TabBtnAnimais", "Livro dos Animais",
                new Vector2(-260f, 186f), new Vector2(240f, 40f),
                new Color(0.32f, 0.58f, 0.44f, 0.96f), 17, () => SwitchTab(0));

            CreateButton(modalRoot.transform, "TabBtnColecoes", "Coleções & Medalhas",
                new Vector2(0f, 186f), new Vector2(240f, 40f),
                new Color(0.54f, 0.44f, 0.72f, 0.96f), 17, () => SwitchTab(1));

            CreateButton(modalRoot.transform, "TabBtnOficina", "Personalização",
                new Vector2(260f, 186f), new Vector2(240f, 40f),
                new Color(0.84f, 0.46f, 0.58f, 0.96f), 17, () => SwitchTab(2));

            BuildAnimalsTab(modalRoot.transform);
            BuildCollectionsTab(modalRoot.transform);
            BuildWorkshopTab(modalRoot.transform);

            footerFeedbackText = CreateTMP(modalRoot.transform, "FooterHint",
                "Explore as 6 regiões para descobrir animais, colecionáveis e desbloquear personalizações!",
                15, new Vector2(0f, -252f), new Vector2(900f, 28f), TextAlignmentOptions.Center,
                new Color(0.34f, 0.28f, 0.22f, 0.92f));

            modalRoot.SetActive(false);
        }

        public void Open(int initialTab = 0)
        {
            if (modalRoot == null) return;
            modalRoot.SetActive(true);
            SwitchTab(initialTab);
            RefreshHeader();
        }

        public void Close()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void SwitchTab(int tabIndex)
        {
            tabAnimalsObj.SetActive(tabIndex == 0);
            tabCollectionsObj.SetActive(tabIndex == 1);
            tabWorkshopObj.SetActive(tabIndex == 2);

            RefreshHeader();
            if (tabIndex == 0) RefreshAnimalPage();
            else if (tabIndex == 1) RefreshCollectionsAndAchievements();
            else RefreshWorkshopItems();
        }

        private void RefreshHeader()
        {
            var pm = PlayerProgressManager.Instance;
            if (pm != null && headerStarsText != null)
                headerStarsText.text = $"★ {pm.Data.starsAvailable} Estrelas";
        }

        // ─── ABA 1: LIVRO DOS ANIMAIS ────────────────────────────────

        private void BuildAnimalsTab(Transform parent)
        {
            tabAnimalsObj = new GameObject("TabAnimais");
            tabAnimalsObj.transform.SetParent(parent, false);
            tabAnimalsObj.AddComponent<RectTransform>().sizeDelta = new Vector2(920f, 380f);

            // Moldura esquerda da ilustração
            GameObject frame = CreatePanel(tabAnimalsObj.transform, "IllustrationFrame",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-240f, -10f), new Vector2(320f, 300f),
                new Color(0.90f, 0.95f, 0.88f, 1f));

            GameObject imgObj = new GameObject("AnimalImg");
            imgObj.transform.SetParent(frame.transform, false);
            RectTransform irt = imgObj.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(230f, 230f);
            animalIllustration = imgObj.AddComponent<Image>();
            animalIllustration.preserveAspect = true;

            // Página direita: texto da enciclopédia
            GameObject page = CreatePanel(tabAnimalsObj.transform, "InfoPage",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(180f, -10f), new Vector2(460f, 300f),
                new Color(1f, 0.98f, 0.94f, 1f));

            animalNameText = CreateTMP(page.transform, "Name", "Esquilo",
                28, new Vector2(0f, 108f), new Vector2(420f, 40f), TextAlignmentOptions.Left,
                new Color(0.20f, 0.36f, 0.26f, 1f));

            animalDetailsText = CreateTMP(page.transform, "Details", "",
                17, new Vector2(0f, 20f), new Vector2(420f, 118f), TextAlignmentOptions.TopLeft,
                new Color(0.30f, 0.26f, 0.22f, 1f));

            animalCuriosityText = CreateTMP(page.transform, "Curiosity", "",
                16, new Vector2(0f, -92f), new Vector2(420f, 86f), TextAlignmentOptions.TopLeft,
                new Color(0.36f, 0.28f, 0.44f, 1f));

            // Navegação de páginas do Diário
            CreateButton(tabAnimalsObj.transform, "BtnPrevPage", "◀ Anterior",
                new Vector2(-190f, -196f), new Vector2(140f, 38f),
                new Color(0.34f, 0.54f, 0.46f, 0.95f), 16, () =>
                {
                    var pm = PlayerProgressManager.Instance;
                    if (pm == null || pm.AnimalCatalog.Count == 0) return;
                    currentAnimalPage = (currentAnimalPage - 1 + pm.AnimalCatalog.Count) % pm.AnimalCatalog.Count;
                    RefreshAnimalPage();
                });

            animalPageCounter = CreateTMP(tabAnimalsObj.transform, "PageCount", "Página 1 / 14",
                17, new Vector2(0f, -196f), new Vector2(220f, 32f), TextAlignmentOptions.Center,
                new Color(0.26f, 0.24f, 0.20f, 1f));

            CreateButton(tabAnimalsObj.transform, "BtnNextPage", "Próxima ▶",
                new Vector2(190f, -196f), new Vector2(140f, 38f),
                new Color(0.34f, 0.54f, 0.46f, 0.95f), 16, () =>
                {
                    var pm = PlayerProgressManager.Instance;
                    if (pm == null || pm.AnimalCatalog.Count == 0) return;
                    currentAnimalPage = (currentAnimalPage + 1) % pm.AnimalCatalog.Count;
                    RefreshAnimalPage();
                });
        }

        private void RefreshAnimalPage()
        {
            var pm = PlayerProgressManager.Instance;
            if (pm == null || pm.AnimalCatalog.Count == 0) return;

            AnimalBookEntry entry = pm.AnimalCatalog[currentAnimalPage];
            bool discovered = pm.IsAnimalDiscovered(entry.kind);

            animalIllustration.sprite = EcosystemSpriteFactory.LoadOrCreateAnimalSprite(entry.kind);
            animalIllustration.color = discovered ? Color.white : new Color(0.18f, 0.24f, 0.22f, 0.45f); // Silhueta suave!

            animalPageCounter.text = $"Página {currentAnimalPage + 1} / {pm.AnimalCatalog.Count}  ({pm.Data.discoveredAnimals.Count} descobertos)";

            if (discovered)
            {
                animalNameText.text = entry.displayName;
                animalDetailsText.text =
                    $"<b>Personalidade:</b> {entry.personality}\n" +
                    $"<b>Alimento favorito:</b> {entry.favoriteFood}\n" +
                    $"<b>Região:</b> {entry.nativeRegion}\n" +
                    $"<b>Estado:</b> Descoberto no Bosque ✓";
                animalCuriosityText.text = $"<i>Curiosidade:</i> {entry.curiosity}";
            }
            else
            {
                animalNameText.text = "??? (Espécie Misteriosa)";
                animalDetailsText.text =
                    $"<b>Região provável:</b> {entry.nativeRegion}\n" +
                    $"<b>Estado:</b> Ainda não descoberto\n\n" +
                    "Continue pedalando com carinho e aproxime-se dos animais para registrar esta página!";
                animalCuriosityText.text = "";
            }
        }

        // ─── ABA 2: COLECIONÁVEIS & CONQUISTAS ───────────────────────

        private void BuildCollectionsTab(Transform parent)
        {
            tabCollectionsObj = new GameObject("TabColecoes");
            tabCollectionsObj.transform.SetParent(parent, false);
            tabCollectionsObj.AddComponent<RectTransform>().sizeDelta = new Vector2(920f, 380f);

            // Coluna Esquerda: 7 Colecionáveis
            GameObject colPanel = CreatePanel(tabCollectionsObj.transform, "CollectiblesBox",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-245f, -20f), new Vector2(390f, 355f),
                new Color(1f, 0.98f, 0.92f, 1f));

            CreateTMP(colPanel.transform, "CTitle", "Colecionáveis da Natureza",
                19, new Vector2(0f, 152f), new Vector2(360f, 30f), TextAlignmentOptions.Center,
                new Color(0.24f, 0.46f, 0.34f, 1f));

            for (int i = 0; i < 7; i++)
            {
                CollectibleKind kind = (CollectibleKind)i;
                float y = 108f - i * 42f;

                GameObject iconObj = new GameObject($"ColIcon_{kind}");
                iconObj.transform.SetParent(colPanel.transform, false);
                RectTransform irt = iconObj.AddComponent<RectTransform>();
                irt.anchoredPosition = new Vector2(-150f, y);
                irt.sizeDelta = new Vector2(34f, 34f);
                Image img = iconObj.AddComponent<Image>();
                img.sprite = TrailCollectibleItem.GetOrCreateSprite(kind);
                img.preserveAspect = true;

                collectibleLabels[i] = CreateTMP(colPanel.transform, $"ColLbl_{kind}",
                    $"{PlayerProgressManager.GetCollectibleDisplayName(kind)}: 0",
                    17, new Vector2(20f, y), new Vector2(290f, 32f), TextAlignmentOptions.Left,
                    new Color(0.26f, 0.24f, 0.20f, 1f));
            }

            // Coluna Direita: 9 Medalhas de Conquistas
            GameObject achPanel = CreatePanel(tabCollectionsObj.transform, "AchievementsBox",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(205f, -20f), new Vector2(470f, 355f),
                new Color(0.96f, 0.94f, 1f, 1f));

            CreateTMP(achPanel.transform, "ATitle", "Medalhas & Conquistas",
                19, new Vector2(0f, 152f), new Vector2(440f, 30f), TextAlignmentOptions.Center,
                new Color(0.42f, 0.30f, 0.58f, 1f));

            for (int i = 0; i < 9; i++)
            {
                float y = 114f - i * 33f;
                GameObject badgeObj = CreatePanel(achPanel.transform, $"Badge_{i}",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-206f, y), new Vector2(22f, 22f),
                    new Color(0.75f, 0.75f, 0.75f, 1f));
                achievementBadges[i] = badgeObj.GetComponent<Image>();

                achievementLabels[i] = CreateTMP(achPanel.transform, $"AchLbl_{i}", "",
                    14, new Vector2(18f, y), new Vector2(400f, 28f), TextAlignmentOptions.Left,
                    new Color(0.28f, 0.24f, 0.32f, 1f));
            }
        }

        private void RefreshCollectionsAndAchievements()
        {
            var pm = PlayerProgressManager.Instance;
            if (pm == null) return;

            for (int i = 0; i < 7; i++)
            {
                CollectibleKind k = (CollectibleKind)i;
                collectibleLabels[i].text = $"{PlayerProgressManager.GetCollectibleDisplayName(k)}: <b>{pm.GetCollectibleCount(k)}</b>";
            }

            for (int i = 0; i < pm.Achievements.Count && i < achievementLabels.Length; i++)
            {
                AchievementDef def = pm.Achievements[i];
                bool unlocked = pm.IsAchievementUnlocked(def.id);
                achievementBadges[i].color = unlocked ? new Color(1f, 0.80f, 0.20f, 1f) : new Color(0.74f, 0.74f, 0.78f, 0.7f);
                achievementLabels[i].text = unlocked
                    ? $"<b>{def.title} ✓</b> — {def.description}"
                    : $"{def.title} (+{def.starReward} ★) — {def.description}";
                achievementLabels[i].color = unlocked ? new Color(0.18f, 0.42f, 0.24f, 1f) : new Color(0.45f, 0.42f, 0.48f, 0.9f);
            }
        }

        // ─── ABA 3: OFICINA DE PERSONALIZAÇÃO ────────────────────────

        private void BuildWorkshopTab(Transform parent)
        {
            tabWorkshopObj = new GameObject("TabOficina");
            tabWorkshopObj.transform.SetParent(parent, false);
            tabWorkshopObj.AddComponent<RectTransform>().sizeDelta = new Vector2(920f, 380f);

            string[] catNames = { "Bicicletas", "Cestas", "Acessórios", "Personagem", "Cachorrinho" };
            Color[] catCols =
            {
                new Color(0.86f, 0.48f, 0.62f, 0.95f),
                new Color(0.74f, 0.52f, 0.30f, 0.95f),
                new Color(0.36f, 0.62f, 0.76f, 0.95f),
                new Color(0.58f, 0.46f, 0.76f, 0.95f),
                new Color(0.34f, 0.62f, 0.44f, 0.95f)
            };

            for (int i = 0; i < catNames.Length; i++)
            {
                CustomizationCategory cat = (CustomizationCategory)i;
                float x = -340f + i * 170f;
                CreateButton(tabWorkshopObj.transform, $"Cat_{cat}", catNames[i],
                    new Vector2(x, 128f), new Vector2(158f, 34f), catCols[i], 15, () =>
                    {
                        currentCategory = cat;
                        RefreshWorkshopItems();
                    });
            }

            GameObject container = CreatePanel(tabWorkshopObj.transform, "ItemsGrid",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -48f), new Vector2(880f, 305f),
                new Color(1f, 0.98f, 0.94f, 0.96f));
            workshopItemsContainer = container.transform;
        }

        private void RefreshWorkshopItems()
        {
            var pm = PlayerProgressManager.Instance;
            if (pm == null || workshopItemsContainer == null) return;

            for (int i = 0; i < spawnedItemCards.Count; i++)
                if (spawnedItemCards[i] != null) Destroy(spawnedItemCards[i]);
            spawnedItemCards.Clear();

            List<CustomizationItemDef> items = pm.Customizations.FindAll(c => c.category == currentCategory);
            string equippedId = pm.GetEquippedId(currentCategory);

            for (int i = 0; i < items.Count; i++)
            {
                CustomizationItemDef item = items[i];
                int col = i % 3;
                int row = i / 3;
                float x = -275f + col * 275f;
                float y = 84f - row * 102f;

                bool unlocked = pm.IsCustomizationUnlocked(item.id);
                bool equipped = (equippedId == item.id);

                Color cardBg = equipped
                    ? new Color(0.84f, 0.96f, 0.86f, 1f)
                    : (unlocked ? new Color(0.95f, 0.95f, 0.98f, 1f) : new Color(0.96f, 0.92f, 0.88f, 1f));

                GameObject card = CreatePanel(workshopItemsContainer, $"Card_{item.id}",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(256f, 88f), cardBg);
                spawnedItemCards.Add(card);

                // Amostra de cor do item
                CreatePanel(card.transform, "Swatch",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-100f, 0f), new Vector2(36f, 54f), item.tintColor);

                CreateTMP(card.transform, "IName", item.displayName,
                    16, new Vector2(22f, 18f), new Vector2(185f, 28f), TextAlignmentOptions.Left,
                    new Color(0.22f, 0.20f, 0.24f, 1f));

                string btnText = equipped ? "Equipado ✓ (Retirar)" : (unlocked ? "Equipar" : $"Desbloquear ({item.starCost} ★)");
                Color btnCol = equipped
                    ? new Color(0.26f, 0.60f, 0.36f, 0.96f)
                    : (unlocked ? new Color(0.32f, 0.56f, 0.78f, 0.96f) : new Color(0.82f, 0.52f, 0.24f, 0.96f));

                string itemId = item.id;
                CreateButton(card.transform, "ActionBtn", btnText,
                    new Vector2(22f, -18f), new Vector2(180f, 32f), btnCol, 14, () =>
                    {
                        pm.TryUnlockOrToggleEquip(itemId, out string fb);
                        if (footerFeedbackText != null) footerFeedbackText.text = fb;
                        RefreshHeader();
                        RefreshWorkshopItems();
                    });
            }
        }

        // ─── UI Helpers ──────────────────────────────────────────────

        private GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = col;
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align, Color col)
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
            tmp.color = col;
            return tmp;
        }

        private void CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, int fontSize, System.Action onClick)
        {
            GameObject go = CreatePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, col);
            CreateTMP(go.transform, "Lbl", label, fontSize, Vector2.zero, size, TextAlignmentOptions.Center, Color.white);
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
        }
    }
}
