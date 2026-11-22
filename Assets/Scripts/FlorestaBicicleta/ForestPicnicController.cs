using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FlorestaTopDown;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Uses a prepared forest recipe to set a small picnic and invite nearby
    /// animals to gather. Animals return to their home positions afterward.
    /// </summary>
    public sealed class ForestPicnicController : MonoBehaviour
    {
        private const float PicnicDuration = 18f;
        private const float AttractionRange = 16f;
        private BikeGirlController bike;
        private InfiniteTrackGenerator track;
        private PuppyCompanionController puppy;
        private Action<string> showMessage;
        private Button picnicButton;
        private TMP_Text buttonLabel;
        private GameObject picnicVisual;
        private float remainingTime;
        private FoodType activeMeal;
        private int gatheredCount;
        private static Sprite cachedBlanketSprite;
        private static Sprite cachedSaladSprite;
        private static Sprite cachedMoonBasketSprite;

        public void Initialize(BikeGirlController player, InfiniteTrackGenerator generator,
            PuppyCompanionController companion, Action<string> messageCallback)
        {
            bike = player;
            track = generator;
            puppy = companion;
            showMessage = messageCallback;
            CreateButton();
        }

        private void Update()
        {
            if (bike == null) return;
            if (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                if (picnicButton != null) picnicButton.interactable = false;
                if (buttonLabel != null)
                    buttonLabel.text = $"PIQUENIQUE  {Mathf.CeilToInt(remainingTime)}s";
                if (remainingTime <= 0f)
                {
                    if (picnicVisual != null) Destroy(picnicVisual);
                    if (buttonLabel != null) buttonLabel.text = "PIQUENIQUE";
                    showMessage?.Invoke("O piquenique terminou; os animais voltaram tranquilamente à trilha.");
                }
                return;
            }

            if (picnicButton != null && picnicButton.gameObject.activeSelf)
                picnicButton.gameObject.SetActive(false);
        }

        private void StartPicnic()
        {
            if (bike == null || track == null || remainingTime > 0f || Mathf.Abs(bike.currentSpeed) >= 0.25f) return;

            if (bike.TryConsumeFood(FoodType.SaladaFrutasBosque))
                activeMeal = FoodType.SaladaFrutasBosque;
            else if (bike.TryConsumeFood(FoodType.CestaDaLua))
                activeMeal = FoodType.CestaDaLua;
            else
            {
                showMessage?.Invoke("Prepare uma Salada do Bosque ou uma Cesta da Lua na cozinha antes do piquenique.");
                return;
            }

            Vector3 picnicPosition = bike.transform.position + new Vector3(-2.8f, 0.12f, 0f);
            CreatePicnicVisual(picnicPosition, activeMeal);
            gatheredCount = 0;
            for (int i = 0; i < track.ActiveAnimals.Count; i++)
            {
                ForestAnimal animal = track.ActiveAnimals[i];
                if (animal == null || Mathf.Abs(animal.transform.position.x - bike.transform.position.x) > AttractionRange) continue;
                animal.AttractToPicnic(picnicPosition, PicnicDuration - 2f);
                gatheredCount++;
            }

            remainingTime = PicnicDuration;
            puppy?.TriggerHappy(4f);
            FlorestaSoundFX.Instance?.PlayHappy();
            string mealName = FoodInfo.GetDisplayName(activeMeal);
            string arrival = gatheredCount > 0
                ? $"{gatheredCount} animais vieram compartilhar a comida."
                : "O aroma está chamando os animais que vivem por perto.";
            showMessage?.Invoke($"Piquenique preparado com {mealName}! {arrival} Observe quem aparece.");
        }

        private void CreateButton()
        {
            GameObject canvasObject = GameObject.Find("FlorestaHUD");
            if (canvasObject == null) return;

            GameObject buttonObject = new GameObject("BtnPiquenique", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(155f, 78f);
            rect.sizeDelta = new Vector2(138f, 36f);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.72f, 0.43f, 0.27f, 0.96f);

            picnicButton = buttonObject.GetComponent<Button>();
            picnicButton.targetGraphic = image;
            picnicButton.onClick.AddListener(StartPicnic);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            buttonLabel = labelObject.GetComponent<TextMeshProUGUI>();
            buttonLabel.text = "PIQUENIQUE";
            buttonLabel.font = TMP_Settings.defaultFontAsset;
            buttonLabel.fontSize = 14f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 10f;
            buttonLabel.fontSizeMax = 15f;
            buttonLabel.raycastTarget = false;
        }

        private void CreatePicnicVisual(Vector3 position, FoodType meal)
        {
            picnicVisual = new GameObject("PiqueniqueTemporario");
            picnicVisual.transform.position = position;

            GameObject blanket = new GameObject("ToalhaPiquenique");
            blanket.transform.SetParent(picnicVisual.transform, false);
            blanket.transform.localPosition = Vector3.zero;
            blanket.transform.localScale = new Vector3(2.3f, 0.72f, 1f);
            SpriteRenderer blanketRenderer = blanket.AddComponent<SpriteRenderer>();
            blanketRenderer.sprite = CreateBlanketSprite();
            blanketRenderer.sortingOrder = 8;

            GameObject mealObject = new GameObject("ComidaPiquenique");
            mealObject.transform.SetParent(picnicVisual.transform, false);
            mealObject.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            mealObject.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            SpriteRenderer mealRenderer = mealObject.AddComponent<SpriteRenderer>();
            mealRenderer.sprite = meal == FoodType.CestaDaLua
                ? (cachedMoonBasketSprite ?? (cachedMoonBasketSprite = ParkSpriteFactory.CreateFoodSprite(meal)))
                : (cachedSaladSprite ?? (cachedSaladSprite = ParkSpriteFactory.CreateFoodSprite(meal)));
            mealRenderer.sortingOrder = 10;
        }

        private static Sprite CreateBlanketSprite()
        {
            if (cachedBlanketSprite != null) return cachedBlanketSprite;
            const int width = 96, height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool stripe = (x / 12) % 2 == 0;
                    bool edge = y < 3 || y >= height - 3;
                    Color color = edge ? new Color(0.75f, 0.45f, 0.27f, 1f)
                        : (stripe ? new Color(0.98f, 0.70f, 0.72f, 1f) : new Color(1f, 0.94f, 0.80f, 1f));
                    texture.SetPixel(x, y, color);
                }
            texture.Apply();
            cachedBlanketSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 48f);
            return cachedBlanketSprite;
        }
    }
}
