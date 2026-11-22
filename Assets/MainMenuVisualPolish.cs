using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a cohesive, responsive visual treatment to the existing sorvete menu
/// while leaving its scene navigation and button callbacks intact.
/// </summary>
public sealed class MainMenuVisualPolish : MonoBehaviour
{
    private static readonly Color Ink = Hex("#34264B");
    private static readonly Color Coral = Hex("#F36F79");
    private static readonly Color Mint = Hex("#8BCBB5");
    private static readonly Color Lavender = Hex("#C7B4E8");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyToMenu()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu") return;
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null || canvas.GetComponent<MainMenuVisualPolish>() != null) return;
        canvas.gameObject.AddComponent<MainMenuVisualPolish>();
    }

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform main = transform.Find("MainMenuPanel");
        Transform levels = transform.Find("LevelSelectionPanel");
        if (main == null || levels == null) return;

        StylePanel(main, "MenuBackdrop", new Color(0.98f, 0.84f, 0.75f));
        StylePanel(levels, "LevelBackdrop", new Color(0.98f, 0.84f, 0.75f));
        AddBackdropDecoration(main);
        AddBackdropDecoration(levels);
        StyleMainMenu(main);
        StyleLevelSelection(levels);
    }

    private static void StylePanel(Transform panel, string backdropName, Color tint)
    {
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null) panelImage.enabled = false;
        Transform old = panel.Find(backdropName);
        if (old != null) Object.Destroy(old.gameObject);

        GameObject backdrop = new GameObject(backdropName, typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(panel, false);
        backdrop.transform.SetAsFirstSibling();
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = backdrop.GetComponent<Image>();
        image.sprite = MakeGradientSprite(new Color(0.99f, 0.91f, 0.80f), tint);
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
    }

    private static void AddBackdropDecoration(Transform panel)
    {
        AddCircle(panel, "CandyOrbCoral", new Vector2(-570, 330), 220, new Color(Coral.r, Coral.g, Coral.b, 0.28f));
        AddCircle(panel, "CandyOrbMint", new Vector2(620, -330), 310, new Color(Mint.r, Mint.g, Mint.b, 0.32f));
        AddCircle(panel, "CandyOrbLilac", new Vector2(650, 350), 110, new Color(Lavender.r, Lavender.g, Lavender.b, 0.48f));
        AddText(panel, "MenuEyebrow", "UMA AVENTURA GELADINHA", 20, Ink, new Vector2(0, 315), new Vector2(620, 42), FontStyles.Bold);
        AddText(panel, "MenuTagline", "Entregue alegria, uma casquinha de cada vez.", 23, Ink, new Vector2(0, -8), new Vector2(760, 54), FontStyles.Normal);
        AddText(panel, "MenuFooter", "FEITO COM DO\u00C7URA  \u2022  PRONTO PARA A DIVERS\u00C3O", 14, new Color(Ink.r, Ink.g, Ink.b, 0.70f), new Vector2(0, -400), new Vector2(700, 32), FontStyles.Bold);
    }

    private static void StyleMainMenu(Transform panel)
    {
        TMP_Text title = panel.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (title != null)
        {
            title.text = "CARRINHO\nDE SORVETE";
            title.fontSize = 66;
            title.fontStyle = FontStyles.Bold;
            title.color = Ink;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = true;
            title.fontSizeMin = 42;
            title.fontSizeMax = 68;
            title.outlineWidth = 0.12f;
            RectTransform titleRect = title.rectTransform;
            titleRect.anchoredPosition = new Vector2(0, 150);
            titleRect.sizeDelta = new Vector2(900, 210);
        }

        Transform play = panel.Find("BtnIniciarJogo");
        StyleButton(play, Coral, "COME\u00C7AR A JOGAR");
        if (play != null) play.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -155);
    }

    private static void StyleLevelSelection(Transform panel)
    {
        foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.text.Contains("SELECIONE"))
            {
                label.text = "ESCOLHA SUA AVENTURA";
                label.color = Ink;
                label.fontSize = 42;
                label.fontStyle = FontStyles.Bold;
                label.rectTransform.anchoredPosition = new Vector2(0, 235);
                label.rectTransform.sizeDelta = new Vector2(900, 90);
            }
        }
        StyleButton(panel.Find("BtnSorvete"), Coral, "SORVETE");
        StyleButton(panel.Find("BtnDonuts"), Lavender, "DONUTS");
        StyleButton(panel.Find("BtnVoltar"), Mint, "VOLTAR");
        AddText(panel, "ChooseSubtitle", "Dois sabores de divers\u00E3o esperam por voc\u00EA.", 22, Ink,
            new Vector2(0, 165), new Vector2(760, 48), FontStyles.Normal);
    }

    private static void StyleButton(Transform buttonTransform, Color color, string labelText)
    {
        if (buttonTransform == null) return;
        Button button = buttonTransform.GetComponent<Button>();
        Image image = buttonTransform.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = MakeRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = true;
        }
        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, Color.white, 0.18f),
                pressedColor = Color.Lerp(color, Ink, 0.15f),
                selectedColor = color,
                disabledColor = new Color(color.r, color.g, color.b, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };
        }
        TMP_Text text = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = labelText;
            text.color = labelText == "DONUTS" ? Ink : Color.white;
            text.fontSize = 25;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 17;
            text.fontSizeMax = 28;
        }
        RectTransform rect = buttonTransform.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = labelText == "VOLTAR" ? new Vector2(270, 72) : new Vector2(430, 92);
        }
    }

    private static void AddCircle(Transform parent, string name, Vector2 position, float size, Color color)
    {
        GameObject orb = new GameObject(name, typeof(RectTransform), typeof(Image));
        orb.transform.SetParent(parent, false);
        orb.transform.SetSiblingIndex(1);
        RectTransform rect = orb.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        Image image = orb.GetComponent<Image>();
        image.sprite = MakeCircleSprite();
        image.color = color;
        image.raycastTarget = false;
    }

    private static TMP_Text AddText(Transform parent, string name, string value, float fontSize, Color color, Vector2 position, Vector2 size, FontStyles style)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = item.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite MakeGradientSprite(Color top, Color bottom)
    {
        const int width = 2, height = 128;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < height; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(height - 1));
            texture.SetPixel(0, y, c);
            texture.SetPixel(1, y, c);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.48f);
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * size)));
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeRoundedSprite()
    {
        const int size = 48, radius = 12;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0, x - (size - 1 - radius));
                float dy = Mathf.Max(radius - y, 0, y - (size - 1 - radius));
                float alpha = dx * dx + dy * dy <= radius * radius ? 1 : 0;
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
