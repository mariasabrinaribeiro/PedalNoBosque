using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD infantil e expressivo de "O Avesso das Horas - A Corrida da Meia-Noite":
/// - 3 Retratos vivos (Íria, Nara, Suri) mostrando o que cada amiga está fazendo em tempo real
/// - 1 Botão de Ação Contextual grande que muda automaticamente conforme o objeto próximo
/// - Banner narrativo limpo e botão de reinício instantâneo
/// </summary>
public sealed class AvessoHUDController : MonoBehaviour
{
    public Sprite portraitIria;
    public Sprite portraitNara;
    public Sprite portraitSuri;

    private Image[] ringImgs = new Image[3];
    private Text[] statusTexts = new Text[3];
    private Text txtStory;
    private Text txtActionLabel;
    private Image imgActionBtn;

    private void Start()
    {
        BuildHUD();
    }

    private void Update()
    {
        AvessoTrioController ctrl = AvessoTrioController.Instance;
        if (ctrl == null) return;

        for (int i = 0; i < 3; i++)
        {
            bool active = ((int)ctrl.ActiveFriend == i);
            if (ringImgs[i] != null)
                ringImgs[i].color = active ? new Color(1f, 0.88f, 0.30f, 1f) : new Color(0.28f, 0.12f, 0.48f, 0.85f);
        }

        if (statusTexts[0] != null) statusTexts[0].text = "1 • ÍRIA\n" + ctrl.IriaStatus;
        if (statusTexts[1] != null) statusTexts[1].text = "2 • NARA\n" + ctrl.NaraStatus;
        if (statusTexts[2] != null) statusTexts[2].text = "3 • SURI\n" + ctrl.SuriStatus;
        if (txtStory != null) txtStory.text = ctrl.StoryBanner;

        if (txtActionLabel != null && imgActionBtn != null)
        {
            Color col;
            txtActionLabel.text = ctrl.GetContextualActionLabel(out col);
            imgActionBtn.color = col;
        }
    }

    private void BuildHUD()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        GameObject canvasObj = new GameObject("AvessoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler sc = canvasObj.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Sprite pill = SweetWorldArtUtility.GetUiPillSprite();

        // 1. Três Retratos Vivos no canto superior esquerdo (clique para trocar de amiga!)
        Sprite[] ports = { portraitIria, portraitNara, portraitSuri };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            GameObject card = CreatePanel(canvasObj.transform, "Retrato_" + i, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(158f + i * 265f, -72f), new Vector2(248f, 112f), new Color(0.14f, 0.05f, 0.28f, 0.88f), pill);
            ringImgs[i] = card.GetComponent<Image>();

            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (AvessoTrioController.Instance != null)
                    AvessoTrioController.Instance.SelectFriend((AvessoTrioController.Friend)idx);
            });

            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(card.transform, false);
            RectTransform art = avatar.AddComponent<RectTransform>();
            art.anchoredPosition = new Vector2(-72f, 0f);
            art.sizeDelta = new Vector2(88f, 88f);
            Image aImg = avatar.AddComponent<Image>();
            aImg.sprite = ports[i];
            aImg.preserveAspect = true;

            statusTexts[i] = CreateText(card.transform, "Status", "", font, 19, TextAnchor.MiddleLeft, new Vector2(46f, 0f), new Vector2(146f, 92f));
        }

        // 2. Banner Narrativo no topo-direito/centro (não bloqueia a Pista Elevada!)
        GameObject storyBox = CreatePanel(canvasObj.transform, "BannerHistoria", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-465f, -66f), new Vector2(880f, 88f), new Color(0.102f, 0.043f, 0.180f, 0.88f), pill);
        txtStory = CreateText(storyBox.transform, "TxtStory", "", font, 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(830f, 76f));
        txtStory.color = new Color(1f, 0.92f, 0.48f, 1f);

        // 3. Um único Botão de Ação Contextual grande no canto inferior direito!
        GameObject actObj = CreatePanel(canvasObj.transform, "BtnAcaoContextual", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-225f, 86f), new Vector2(390f, 108f), new Color(1f, 0.36f, 0.74f, 1f), pill);
        imgActionBtn = actObj.GetComponent<Image>();
        Button actBtn = actObj.AddComponent<Button>();
        actBtn.onClick.AddListener(() =>
        {
            if (AvessoTrioController.Instance != null)
                AvessoTrioController.Instance.PerformContextualAction();
        });
        txtActionLabel = CreateText(actObj.transform, "TxtAction", "PARAR NO ACOSTAMENTO", font, 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(360f, 92f));

        // 4. Dica de teclas + Botão Reiniciar no canto inferior esquerdo
        GameObject hint = CreatePanel(canvasObj.transform, "HintBar", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(345f, 48f), new Vector2(640f, 52f), new Color(0.10f, 0.04f, 0.20f, 0.78f), pill);
        CreateText(hint.transform, "TxtHint", "1: Íria  •  2: Nara  •  3: Suri  •  WASD: Mover  •  ESPAÇO/R: Ação", font, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(620f, 42f));

        GameObject rst = CreatePanel(canvasObj.transform, "BtnReiniciar", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(765f, 48f), new Vector2(165f, 52f), new Color(0.38f, 0.22f, 0.64f, 0.92f), pill);
        rst.AddComponent<Button>().onClick.AddListener(() =>
        {
            if (AvessoTrioController.Instance != null) AvessoTrioController.Instance.ResetSceneState();
        });
        CreateText(rst.transform, "LblRst", "REINICIAR", font, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 40f));
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color col, Sprite sp)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = col;
        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
        return go;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size, TextAnchor align, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = box;
        Text t = go.AddComponent<Text>();
        t.font = font; t.text = content; t.fontSize = size;
        t.fontStyle = FontStyle.Bold; t.alignment = align; t.color = Color.white;
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.14f, 0.04f, 0.24f, 0.95f);
        ol.effectDistance = new Vector2(2f, -2f);
        return t;
    }
}
