using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlorestaBicicleta
{
    /// <summary>
    /// Atividades Afetivas e Criativas do PEDAL NO BOSQUE (Cozy Storybook UI):
    /// 1. Música da Floresta: reproduzir sequência de 3 notas com instrumentos naturais (Flauta, Tronco, Gotas, Sino);
    /// 2. Cuidadora de Animais: escovar, montar abrigo de folhas e dar carinho especial;
    /// 3. História & Enigma da Coruja: pequeno enigma acolhedor sobre a natureza.
    /// </summary>
    public class ForestActivitiesUI : MonoBehaviour
    {
        public static ForestActivitiesUI Instance { get; private set; }

        // Música da Floresta
        private GameObject musicModal;
        private TextMeshProUGUI musicHintTMP;
        private Image[] instrumentBgs = new Image[4];
        private Color[] instrumentBaseColors =
        {
            new Color(0.32f, 0.64f, 0.44f, 0.96f), // Flauta de Bambu
            new Color(0.68f, 0.46f, 0.24f, 0.96f), // Tronco Oco
            new Color(0.28f, 0.64f, 0.84f, 0.96f), // Gotas d'Água
            new Color(0.84f, 0.50f, 0.68f, 0.96f)  // Sino de Flor
        };
        private int[] targetSequence = new int[3];
        private int musicInputIndex = 0;
        private bool isPlayingDemo = false;

        // Cuidadora de Animais
        private GameObject careModal;
        private TextMeshProUGUI careStatusTMP;
        private Button[] careStepButtons = new Button[3];
        private Image[] careStepBgs = new Image[3];
        private int careStepsDone = 0;

        // Enigma da Coruja
        private GameObject owlModal;
        private TextMeshProUGUI owlQuestionTMP;
        private TextMeshProUGUI owlHintTMP;
        private TextMeshProUGUI[] owlOptLabels = new TextMeshProUGUI[3];
        private int owlCorrectSlot = 0;

        private System.Action onActivityCompleted;

        private void Awake()
        {
            Instance = this;
        }

        public void Build(Transform canvasParent)
        {
            BuildMusicModal(canvasParent);
            BuildCareModal(canvasParent);
            BuildOwlRiddleModal(canvasParent);
        }

        // ─── 1. MÚSICA DA FLORESTA ───────────────────────────────────

        private void BuildMusicModal(Transform parent)
        {
            musicModal = CreateCard(parent, "MusicaFlorestaModal", new Vector2(540f, 330f), new Color(0.97f, 0.94f, 0.87f, 0.98f));

            CreateTMP(musicModal.transform, "MTitle", "Musica da Floresta", 24, new Vector2(0f, 126f), new Vector2(480f, 36f), new Color(0.22f, 0.44f, 0.34f, 1f));
            musicHintTMP = CreateTMP(musicModal.transform, "MHint", "Ouca a melodia e repita nos instrumentos naturais!", 16, new Vector2(0f, 84f), new Vector2(490f, 32f), new Color(0.32f, 0.28f, 0.22f, 1f));

            string[] names = { "Flauta de Bambu", "Tronco Oco", "Gotas d'Agua", "Sino de Flor" };
            Vector2[] positions =
            {
                new Vector2(-124f, 14f),
                new Vector2( 124f, 14f),
                new Vector2(-124f, -52f),
                new Vector2( 124f, -52f)
            };

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                GameObject btn = CreatePillButton(musicModal.transform, $"Inst_{i}", names[i], positions[i], new Vector2(226f, 50f), instrumentBaseColors[i], 17, () => OnTapInstrument(idx));
                instrumentBgs[i] = btn.GetComponent<Image>();
            }

            CreatePillButton(musicModal.transform, "BtnReplaySeq", "Ouvir de Novo", new Vector2(-100f, -124f), new Vector2(180f, 38f), new Color(0.34f, 0.56f, 0.72f, 0.95f), 16, () => StartCoroutine(PlayDemoRoutine()));
            CreatePillButton(musicModal.transform, "BtnCloseMusic", "Fechar", new Vector2(110f, -124f), new Vector2(140f, 38f), new Color(0.74f, 0.38f, 0.42f, 0.94f), 16, () => musicModal.SetActive(false));

            musicModal.SetActive(false);
        }

        public void OpenMusicActivity(System.Action completedCb)
        {
            onActivityCompleted = completedCb;
            musicInputIndex = 0;
            for (int i = 0; i < 3; i++) targetSequence[i] = Random.Range(0, 4);
            musicModal.SetActive(true);
            StartCoroutine(PlayDemoRoutine());
        }

        private IEnumerator PlayDemoRoutine()
        {
            isPlayingDemo = true;
            musicHintTMP.text = "Escute com atencao os sons da floresta...";
            yield return new WaitForSeconds(0.35f);

            for (int i = 0; i < targetSequence.Length; i++)
            {
                int inst = targetSequence[i];
                FlorestaSoundFX.Instance?.PlayNatureNote(inst);
                instrumentBgs[inst].color = new Color(1f, 0.92f, 0.44f, 1f);
                yield return new WaitForSeconds(0.45f);
                instrumentBgs[inst].color = instrumentBaseColors[inst];
                yield return new WaitForSeconds(0.18f);
            }

            musicHintTMP.text = "Sua vez! Toque os 3 instrumentos na mesma ordem!";
            musicInputIndex = 0;
            isPlayingDemo = false;
        }

        private void OnTapInstrument(int idx)
        {
            if (isPlayingDemo) return;
            FlorestaSoundFX.Instance?.PlayNatureNote(idx);

            if (idx == targetSequence[musicInputIndex])
            {
                musicInputIndex++;
                if (musicInputIndex >= targetSequence.Length)
                {
                    musicModal.SetActive(false);
                    onActivityCompleted?.Invoke();
                }
                else
                {
                    musicHintTMP.text = $"Muito bem! Faltam {targetSequence.Length - musicInputIndex} notas!";
                }
            }
            else
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                musicInputIndex = 0;
                musicHintTMP.text = "Quase! Clique em 'Ouvir de Novo' ou tente mais uma vez!";
            }
        }

        // ─── 2. CUIDADORA DE ANIMAIS ─────────────────────────────────

        private void BuildCareModal(Transform parent)
        {
            careModal = CreateCard(parent, "CuidadoraModal", new Vector2(520f, 320f), new Color(0.97f, 0.94f, 0.87f, 0.98f));

            CreateTMP(careModal.transform, "CTitle", "Cuidadora do Bosque", 24, new Vector2(0f, 122f), new Vector2(460f, 36f), new Color(0.24f, 0.46f, 0.34f, 1f));
            careStatusTMP = CreateTMP(careModal.transform, "CStatus", "Cuide com carinho deste amiguinho da floresta!", 16, new Vector2(0f, 80f), new Vector2(470f, 30f), new Color(0.32f, 0.28f, 0.22f, 1f));

            string[] stepLabels = { "1. Escovar o Pelinho", "2. Preparar Abrigo de Folhas", "3. Dar Carinho Especial" };
            Color[] stepCols =
            {
                new Color(0.36f, 0.62f, 0.78f, 0.96f),
                new Color(0.30f, 0.60f, 0.42f, 0.96f),
                new Color(0.84f, 0.46f, 0.58f, 0.96f)
            };

            for (int i = 0; i < 3; i++)
            {
                int step = i;
                GameObject btnObj = CreatePillButton(careModal.transform, $"CareStep_{i}", stepLabels[i], new Vector2(0f, 22f - i * 54f), new Vector2(360f, 44f), stepCols[i], 17, () => OnClickCareStep(step));
                careStepButtons[i] = btnObj.GetComponent<Button>();
                careStepBgs[i] = btnObj.GetComponent<Image>();
            }

            CreatePillButton(careModal.transform, "BtnCloseCare", "Fechar", new Vector2(0f, -128f), new Vector2(140f, 34f), new Color(0.74f, 0.38f, 0.42f, 0.94f), 15, () => careModal.SetActive(false));
            careModal.SetActive(false);
        }

        public void OpenCareActivity(System.Action completedCb)
        {
            onActivityCompleted = completedCb;
            careStepsDone = 0;
            careStatusTMP.text = "Complete os 3 cuidados com carinho!";
            for (int i = 0; i < 3; i++)
            {
                careStepButtons[i].interactable = true;
                careStepBgs[i].color = i == 0 ? new Color(0.36f, 0.62f, 0.78f, 0.96f)
                                     : (i == 1 ? new Color(0.30f, 0.60f, 0.42f, 0.96f) : new Color(0.84f, 0.46f, 0.58f, 0.96f));
            }
            careModal.SetActive(true);
        }

        private void OnClickCareStep(int step)
        {
            careStepButtons[step].interactable = false;
            careStepBgs[step].color = new Color(0.24f, 0.68f, 0.36f, 0.95f);
            careStepsDone++;
            FlorestaSoundFX.Instance?.PlayStar();

            if (careStepsDone >= 3)
            {
                careModal.SetActive(false);
                onActivityCompleted?.Invoke();
            }
            else
            {
                careStatusTMP.text = $"Que carinho gostoso! ({careStepsDone}/3 cuidados feitos)";
            }
        }

        // ─── 3. HISTÓRIA & ENIGMA DA CORUJA ──────────────────────────

        private void BuildOwlRiddleModal(Transform parent)
        {
            owlModal = CreateCard(parent, "EnigmaCorujaModal", new Vector2(560f, 320f), new Color(0.95f, 0.93f, 0.98f, 0.98f));

            CreateTMP(owlModal.transform, "OTitle", "Enigma da Coruja Sabia", 23, new Vector2(0f, 122f), new Vector2(500f, 34f), new Color(0.34f, 0.26f, 0.54f, 1f));
            owlQuestionTMP = CreateTMP(owlModal.transform, "OQuestion", "", 17, new Vector2(0f, 64f), new Vector2(510f, 68f), new Color(0.24f, 0.22f, 0.30f, 1f));

            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                GameObject opt = CreatePillButton(owlModal.transform, $"OwlOpt_{i}", "Opcao", new Vector2(0f, -4f - i * 44f), new Vector2(380f, 38f), new Color(0.46f, 0.42f, 0.72f, 0.95f), 16, () => OnAnswerOwl(slot));
                owlOptLabels[i] = opt.GetComponentInChildren<TextMeshProUGUI>();
            }

            owlHintTMP = CreateTMP(owlModal.transform, "OHint", "Escolha a resposta certa!", 14, new Vector2(0f, -134f), new Vector2(480f, 24f), new Color(0.42f, 0.34f, 0.54f, 1f));
            owlModal.SetActive(false);
        }

        public void OpenOwlRiddle(System.Action completedCb)
        {
            onActivityCompleted = completedCb;
            var riddles = new (string q, string a, string b, string c, int correct)[]
            {
                ("Brilho suavemente na noite do bosque e danco entre as flores. Quem sou eu?", "O Vaga-lume", "A Pedra", "O Tronco", 0),
                ("Construo pontes e barragens de madeira perto da agua. Quem sou eu?", "O Coelho", "O Castor", "O Sapo", 1),
                ("Guardo nozes na terra fofa e conheco o segredo dos diamantes. Quem sou eu?", "O Peixe", "A Borboleta", "O Esquilo", 2)
            };

            var chosen = riddles[Random.Range(0, riddles.Length)];
            owlQuestionTMP.text = chosen.q;
            owlOptLabels[0].text = chosen.a;
            owlOptLabels[1].text = chosen.b;
            owlOptLabels[2].text = chosen.c;
            owlCorrectSlot = chosen.correct;
            owlHintTMP.text = "A Coruja sorri esperando sua resposta!";
            owlModal.SetActive(true);
        }

        private void OnAnswerOwl(int slot)
        {
            if (slot == owlCorrectSlot)
            {
                owlModal.SetActive(false);
                onActivityCompleted?.Invoke();
            }
            else
            {
                FlorestaSoundFX.Instance?.PlayWrong();
                owlHintTMP.text = "Huu-huu! Tente outra resposta, voce consegue!";
            }
        }

        // ─── UI Helpers ──────────────────────────────────────────────

        private GameObject CreateCard(Transform parent, string name, Vector2 size, Color col)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedCardSprite();
            img.type = Image.Type.Sliced;
            img.color = col;
            return go;
        }

        private GameObject CreatePillButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color col, int fontSize, System.Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = StorybookUISpriteFactory.GetRoundedPillSprite();
            img.type = Image.Type.Sliced;
            img.color = col;
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
            float boosted = Mathf.Round(fontSize * 1.26f);
            tmp.text = text; tmp.fontSize = boosted; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.color = col;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = boosted;
            tmp.fontSizeMin = Mathf.Max(14f, Mathf.Round(boosted * 0.72f));
            tmp.extraPadding = true;
            return tmp;
        }
    }
}
