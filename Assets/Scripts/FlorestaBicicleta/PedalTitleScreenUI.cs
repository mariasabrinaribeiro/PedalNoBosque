using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Fase D/E — Tela de Título Oficial do PEDAL NO BOSQUE + Seletor de Idioma (PT/EN/ES)
    /// + Barreira Parental (Parental Gate) para a Área dos Pais (conformidade LGPD / COPPA / Families).
    /// Fica sobre o cenário vivo (nuvens, pássaros, borboletas e flores já se movendo ao fundo!).
    /// </summary>
    public class PedalTitleScreenUI : MonoBehaviour
    {
        private GameObject titleRoot;
        private TextMeshProUGUI brandTitleTMP;
        private TextMeshProUGUI sloganTMP;
        private TextMeshProUGUI progressBadgeTMP;
        private TextMeshProUGUI playBtnTMP;
        private TextMeshProUGUI parentsBtnTMP;

        // Barreira Parental (Parental Gate) + Painel dos Pais
        private GameObject gateModal;
        private TextMeshProUGUI gateTitleTMP;
        private TextMeshProUGUI gateQuestionTMP;
        private TextMeshProUGUI[] gateOptionLabels = new TextMeshProUGUI[3];
        private int gateCorrectAnswer = 12;

        private GameObject parentsPanel;
        private TextMeshProUGUI parentsInfoTMP;
        private TextMeshProUGUI resetBtnTMP;
        private TextMeshProUGUI closeParentsTMP;

        private System.Action onPlayClicked;

        public bool IsShowing => titleRoot != null && titleRoot.activeSelf;

        public void Build(Transform canvasParent, System.Action playCb)
        {
            onPlayClicked = playCb;

            // Cartão central acolhedor de livro ilustrado sobre o cenário vivo
            titleRoot = CreateCard(canvasParent, "PedalTitleOverlay",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 16f), new Vector2(640f, 385f),
                new Color(0.97f, 0.94f, 0.87f, 0.94f));

            // Faixa superior em verde-bosque
            GameObject ribbon = CreatePill(titleRoot.transform, "BrandRibbon",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(540f, 64f),
                new Color(0.22f, 0.46f, 0.36f, 0.98f));

            brandTitleTMP = CreateTMP(ribbon.transform, "BrandTitle", LocalizationManager.Get("brand_title"),
                30, Vector2.zero, new Vector2(510f, 52f), new Color(1f, 0.96f, 0.80f, 1f));

            sloganTMP = CreateTMP(titleRoot.transform, "Slogan", LocalizationManager.Get("brand_slogan"),
                19, new Vector2(0f, 82f), new Vector2(580f, 32f), new Color(0.32f, 0.28f, 0.22f, 1f));

            // Resumo de progresso salvo
            progressBadgeTMP = CreateTMP(titleRoot.transform, "ProgressBadge", "",
                15, new Vector2(0f, 40f), new Vector2(560f, 28f), new Color(0.24f, 0.48f, 0.34f, 1f));

            // Botão principal JOGAR
            GameObject playBtn = CreatePillButton(titleRoot.transform, "BtnJogar", LocalizationManager.Get("btn_play"),
                new Vector2(0f, -20f), new Vector2(300f, 62f),
                new Color(0.26f, 0.62f, 0.40f, 0.98f), 23, () =>
                {
                    FlorestaSoundFX.Instance?.PlayHappy();
                    titleRoot.SetActive(false);
                    onPlayClicked?.Invoke();
                });
            playBtnTMP = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Seletor de Idiomas (PT | EN | ES)
            CreatePillButton(titleRoot.transform, "LangPT", "PT", new Vector2(-84f, -96f), new Vector2(68f, 34f),
                new Color(0.34f, 0.56f, 0.46f, 0.92f), 15, () => LocalizationManager.SetLanguage(GameLanguage.PT));
            CreatePillButton(titleRoot.transform, "LangEN", "EN", new Vector2(0f, -96f), new Vector2(68f, 34f),
                new Color(0.34f, 0.52f, 0.72f, 0.92f), 15, () => LocalizationManager.SetLanguage(GameLanguage.EN));
            CreatePillButton(titleRoot.transform, "LangES", "ES", new Vector2(84f, -96f), new Vector2(68f, 34f),
                new Color(0.76f, 0.48f, 0.28f, 0.92f), 15, () => LocalizationManager.SetLanguage(GameLanguage.ES));

            // Botão discreto Área dos Pais (com Barreira Parental)
            GameObject parentsBtn = CreatePillButton(titleRoot.transform, "BtnPais", LocalizationManager.Get("btn_parents"),
                new Vector2(0f, -150f), new Vector2(210f, 36f),
                new Color(0.50f, 0.44f, 0.62f, 0.90f), 15, OpenParentalGate);
            parentsBtnTMP = parentsBtn.GetComponentInChildren<TextMeshProUGUI>();

            BuildParentalGate(canvasParent);
            BuildParentsInfoPanel(canvasParent);

            LocalizationManager.OnLanguageChanged += RefreshLocalizedTexts;
            RefreshLocalizedTexts();
        }

        private void BuildParentalGate(Transform parent)
        {
            gateModal = CreateCard(parent, "ParentalGateModal",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(480f, 250f),
                new Color(0.97f, 0.94f, 0.88f, 0.99f));

            gateTitleTMP = CreateTMP(gateModal.transform, "GTitle", LocalizationManager.Get("parent_gate"),
                17, new Vector2(0f, 84f), new Vector2(440f, 32f), new Color(0.28f, 0.24f, 0.36f, 1f));

            gateQuestionTMP = CreateTMP(gateModal.transform, "GQuestion", "Quanto e 7 + 5?",
                24, new Vector2(0f, 34f), new Vector2(420f, 38f), new Color(0.18f, 0.34f, 0.26f, 1f));

            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                GameObject opt = CreatePillButton(gateModal.transform, $"GateOpt_{i}", "12",
                    new Vector2(-120f + i * 120f, -28f), new Vector2(96f, 44f),
                    new Color(0.32f, 0.56f, 0.72f, 0.96f), 20, () => OnAnswerGate(slot));
                gateOptionLabels[i] = opt.GetComponentInChildren<TextMeshProUGUI>();
            }

            CreatePillButton(gateModal.transform, "GateClose", "Fechar",
                new Vector2(0f, -92f), new Vector2(130f, 34f),
                new Color(0.72f, 0.36f, 0.40f, 0.94f), 15, () => gateModal.SetActive(false));

            gateModal.SetActive(false);
        }

        private void BuildParentsInfoPanel(Transform parent)
        {
            parentsPanel = CreateCard(parent, "ParentsInfoPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(560f, 300f),
                new Color(0.97f, 0.94f, 0.88f, 0.99f));

            CreateTMP(parentsPanel.transform, "PTitle", "PEDAL NO BOSQUE - Seguranca & Privacidade",
                18, new Vector2(0f, 114f), new Vector2(520f, 32f), new Color(0.22f, 0.42f, 0.34f, 1f));

            parentsInfoTMP = CreateTMP(parentsPanel.transform, "PBody", LocalizationManager.Get("parent_info"),
                15, new Vector2(0f, 18f), new Vector2(500f, 140f), new Color(0.26f, 0.24f, 0.22f, 1f));

            GameObject resetBtn = CreatePillButton(parentsPanel.transform, "BtnResetSave", LocalizationManager.Get("btn_reset"),
                new Vector2(-115f, -104f), new Vector2(210f, 38f),
                new Color(0.78f, 0.42f, 0.34f, 0.94f), 15, () =>
                {
                    PlayerSaveData fresh = SaveSystem.CreateDefault();
                    SaveSystem.Save(fresh);
                    parentsInfoTMP.text = LocalizationManager.Get("reset_done");
                });
            resetBtnTMP = resetBtn.GetComponentInChildren<TextMeshProUGUI>();

            GameObject closeBtn = CreatePillButton(parentsPanel.transform, "BtnCloseParents", LocalizationManager.Get("btn_close"),
                new Vector2(130f, -104f), new Vector2(150f, 38f),
                new Color(0.28f, 0.56f, 0.44f, 0.96f), 15, () => parentsPanel.SetActive(false));
            closeParentsTMP = closeBtn.GetComponentInChildren<TextMeshProUGUI>();

            parentsPanel.SetActive(false);
        }

        private void OpenParentalGate()
        {
            int a = Random.Range(4, 10);
            int b = Random.Range(3, 9);
            gateCorrectAnswer = a + b;
            gateQuestionTMP.text = $"{a} + {b} = ?";

            int correctSlot = Random.Range(0, 3);
            for (int i = 0; i < 3; i++)
            {
                int val = (i == correctSlot) ? gateCorrectAnswer : gateCorrectAnswer + (i == 0 ? -2 : 3);
                gateOptionLabels[i].text = val.ToString();
            }
            gateModal.SetActive(true);
        }

        private void OnAnswerGate(int slot)
        {
            if (int.TryParse(gateOptionLabels[slot].text, out int chosen) && chosen == gateCorrectAnswer)
            {
                gateModal.SetActive(false);
                parentsInfoTMP.text = LocalizationManager.Get("parent_info");
                parentsPanel.SetActive(true);
            }
            else
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                OpenParentalGate();
            }
        }

        private void RefreshLocalizedTexts()
        {
            if (brandTitleTMP != null) brandTitleTMP.text = LocalizationManager.Get("brand_title");
            if (sloganTMP != null) sloganTMP.text = LocalizationManager.Get("brand_slogan");
            if (playBtnTMP != null) playBtnTMP.text = LocalizationManager.Get("btn_play");
            if (parentsBtnTMP != null) parentsBtnTMP.text = LocalizationManager.Get("btn_parents");
            if (gateTitleTMP != null) gateTitleTMP.text = LocalizationManager.Get("parent_gate");
            if (parentsInfoTMP != null) parentsInfoTMP.text = LocalizationManager.Get("parent_info");
            if (resetBtnTMP != null) resetBtnTMP.text = LocalizationManager.Get("btn_reset");
            if (closeParentsTMP != null) closeParentsTMP.text = LocalizationManager.Get("btn_close");

            var pm = PlayerProgressManager.Instance;
            if (progressBadgeTMP != null && pm != null && pm.Data != null)
            {
                progressBadgeTMP.text = $"Recorde: {pm.Data.bestDistanceKm:0.00} km  |  Animais: {pm.Data.discoveredAnimals.Count}/14  |  Vila Nivel {pm.Data.villageEvolutionLevel}";
            }
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= RefreshLocalizedTexts;
        }

        // ─── UI Helpers ──────────────────────────────────────────────

        private GameObject CreateCard(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedCardSprite();
            img.type = Image.Type.Sliced;
            img.color = col;
            return go;
        }

        private GameObject CreatePill(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.anchoredPosition = pos; rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            img.type = Image.Type.Sliced;
            img.color = col;
            return go;
        }

        private GameObject CreatePillButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, int fontSize, System.Action onClick)
        {
            GameObject go = CreatePill(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, col);
            CreateTMP(go.transform, "Lbl", label, fontSize, Vector2.zero, size, Color.white);
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = col;
            return tmp;
        }
    }
}
