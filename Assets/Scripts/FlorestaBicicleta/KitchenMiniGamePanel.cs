using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Etapa 4 — Mini-Preparo Opcional de Sanduíche e Hot Dog na Estação de Abastecimento.
    /// O jogador pode montar os 4 ingredientes na ordem certa para ganhar Bônus Gourmet (+60 pts + bônus na entrega),
    /// ou clicar em "Preparar Automático" para pular o mini-jogo e pegar o lanche rapidamente.
    /// </summary>
    public class KitchenMiniGamePanel : MonoBehaviour
    {
        private GameObject panelRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI stepHintText;
        private TextMeshProUGUI progressLayersText;
        private Button[] ingredientButtons = new Button[4];
        private TextMeshProUGUI[] ingredientLabels = new TextMeshProUGUI[4];
        private Image[] ingredientBg = new Image[4];

        private FoodType activeFood;
        private string[] currentRecipe;
        private int[] shuffledToRecipeIndex = new int[4];
        private int currentStep = 0;
        private bool isOpen = false;

        private System.Action<FoodType, bool> onCompleted; // (foodType, isGourmet)
        private System.Action<FoodType> onQuickAutoPrep;

        public bool IsOpen => isOpen;

        public void Build(Transform canvasParent, System.Action<FoodType, bool> completedCb, System.Action<FoodType> quickAutoCb)
        {
            onCompleted = completedCb;
            onQuickAutoPrep = quickAutoCb;

            panelRoot = new GameObject("MiniPrepPanel");
            panelRoot.transform.SetParent(canvasParent, false);
            RectTransform rt = panelRoot.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(180f, 15f);
            rt.sizeDelta = new Vector2(400f, 340f);
            panelRoot.AddComponent<Image>().color = new Color(0.16f, 0.11f, 0.24f, 0.96f);

            titleText = MakeTMP(panelRoot.transform, "MTitle", "MINI-PREPARO GOURMET", 22, new Vector2(0f, 138f), new Vector2(370f, 34f));
            titleText.color = new Color(1f, 0.86f, 0.38f, 1f);

            stepHintText = MakeTMP(panelRoot.transform, "MHint", "Toque nos ingredientes na ordem!", 17, new Vector2(0f, 102f), new Vector2(370f, 28f));
            stepHintText.color = new Color(0.84f, 0.98f, 1f, 1f);

            progressLayersText = MakeTMP(panelRoot.transform, "MLayers", "", 16, new Vector2(0f, 66f), new Vector2(370f, 36f));
            progressLayersText.color = new Color(0.70f, 1f, 0.74f, 1f);

            // 4 botões de ingredientes em grade 2x2
            Vector2[] gridPos =
            {
                new Vector2(-92f, 4f),
                new Vector2( 92f, 4f),
                new Vector2(-92f, -54f),
                new Vector2( 92f, -54f)
            };

            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                GameObject btnObj = new GameObject($"IngBtn_{i}");
                btnObj.transform.SetParent(panelRoot.transform, false);
                RectTransform brt = btnObj.AddComponent<RectTransform>();
                brt.anchoredPosition = gridPos[i];
                brt.sizeDelta = new Vector2(170f, 46f);
                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.28f, 0.56f, 0.84f, 0.96f);
                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => OnClickIngredient(slot));

                TextMeshProUGUI lbl = MakeTMP(btnObj.transform, "Lbl", "Ingrediente", 18, Vector2.zero, new Vector2(162f, 42f));
                ingredientButtons[i] = btn;
                ingredientLabels[i] = lbl;
                ingredientBg[i] = img;
            }

            // Botão de Preparo Automático (100% opcional pular o mini-jogo!)
            MakeActionButton(panelRoot.transform, "BtnAutoPrep", "Preparo Automático (Sem Mini-Jogo)",
                new Vector2(0f, -112f), new Vector2(354f, 36f),
                new Color(0.76f, 0.46f, 0.16f, 0.94f), 16,
                () =>
                {
                    FoodType ft = activeFood;
                    Close();
                    onQuickAutoPrep?.Invoke(ft);
                });

            // Botão Fechar / Cancelar
            MakeActionButton(panelRoot.transform, "BtnCancel", "Cancelar",
                new Vector2(0f, -150f), new Vector2(140f, 30f),
                new Color(0.55f, 0.24f, 0.28f, 0.90f), 15,
                Close);

            panelRoot.SetActive(false);
        }

        public void OpenFor(FoodType foodType)
        {
            activeFood = foodType;
            currentStep = 0;
            isOpen = true;

            if (foodType == FoodType.Sanduiche)
            {
                titleText.text = "SANDUÍCHE DE MAÇÃ E MEL (+60 pts)";
                currentRecipe = new[] { "1. Pão", "2. Maçã", "3. Mel" };
            }
            else if (foodType == FoodType.HotDog)
            {
                titleText.text = "MONTAR HOT DOG (+60 pts)";
                currentRecipe = new[] { "1. Pão", "2. Salsicha", "3. Molho", "4. Mostarda" };
            }

            if (foodType == FoodType.SaladaFrutasBosque)
            {
                titleText.text = "SALADA DO BOSQUE (+60 pts)";
                currentRecipe = new[] { "1. Maca", "2. Cenoura", "3. Sementes", "4. Mel" };
            }
            else if (foodType == FoodType.CestaDaLua)
            {
                titleText.text = "CESTA DA LUA (+60 pts)";
                currentRecipe = new[] { "1. Nozes", "2. Maca", "3. Sementes", "4. Mel" };
            }
            else if (foodType == FoodType.TortinhaDeMaca)
            {
                titleText.text = "TORTINHA DE MAÇÃ (+60 pts)";
                currentRecipe = new[] { "1. Maçã", "2. Mel" };
            }
            else if (foodType == FoodType.GeleiaSilvestre)
            {
                titleText.text = "GELEIA DA FEIRA (+60 pts)";
                currentRecipe = new[] { "1. Frutas silvestres", "2. Mel" };
            }

            // Embaralha a posição dos 4 botões para ficar divertido de montar
            for (int i = 0; i < currentRecipe.Length; i++) shuffledToRecipeIndex[i] = i;
            for (int i = currentRecipe.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = shuffledToRecipeIndex[i];
                shuffledToRecipeIndex[i] = shuffledToRecipeIndex[j];
                shuffledToRecipeIndex[j] = tmp;
            }

            for (int i = 0; i < 4; i++)
            {
                ingredientButtons[i].gameObject.SetActive(i < currentRecipe.Length);
                if (i >= currentRecipe.Length) continue;
                int recipeIdx = shuffledToRecipeIndex[i];
                ingredientLabels[i].text = currentRecipe[recipeIdx];
                ingredientButtons[i].interactable = true;
                ingredientBg[i].color = new Color(0.26f, 0.56f, 0.86f, 0.96f);
            }

            RefreshStepUI();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            isOpen = false;
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnClickIngredient(int slot)
        {
            if (!isOpen || slot < 0 || slot >= currentRecipe.Length) return;
            int chosenRecipeIdx = shuffledToRecipeIndex[slot];

            if (chosenRecipeIdx == currentStep)
            {
                ingredientButtons[slot].interactable = false;
                ingredientBg[slot].color = new Color(0.24f, 0.72f, 0.36f, 0.95f);
                currentStep++;
                FlorestaSoundFX.Instance?.PlayStar();

                if (currentStep >= currentRecipe.Length)
                {
                    FoodType finished = activeFood;
                    Close();
                    onCompleted?.Invoke(finished, true);
                }
                else
                {
                    RefreshStepUI();
                }
            }
            else
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                stepHintText.text = $"Ops! Coloque agora: {currentRecipe[currentStep]}";
                stepHintText.color = new Color(1f, 0.72f, 0.45f, 1f);
            }
        }

        private void RefreshStepUI()
        {
            stepHintText.text = $"Próximo ingrediente: {currentRecipe[currentStep]}";
            stepHintText.color = new Color(0.84f, 0.98f, 1f, 1f);

            string built = "";
            for (int i = 0; i < currentStep; i++)
            {
                if (i > 0) built += " + ";
                built += currentRecipe[i].Substring(3);
            }
            progressLayersText.text = string.IsNullOrEmpty(built) ? "Prato pronto para montar..." : $"Montado: {built}";
        }

        private TextMeshProUGUI MakeTMP(Transform parent, string name, string txt, int fontSize, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            float boosted = Mathf.Round(fontSize * 1.26f);
            tmp.text = txt;
            tmp.fontSize = boosted;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = boosted;
            tmp.fontSizeMin = Mathf.Max(14f, Mathf.Round(boosted * 0.72f));
            tmp.extraPadding = true;
            return tmp;
        }

        private void MakeActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, int fontSize, System.Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = col;
            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            MakeTMP(go.transform, "Lbl", label, fontSize, Vector2.zero, size);
        }
    }
}
