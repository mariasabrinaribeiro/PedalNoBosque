#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using FlorestaBicicleta;
using FlorestaTopDown;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Batch entry point: -executeMethod VerticalSliceValidator.Run with a unique
// PEDAL_TEST_PROFILE under Temp. Reopen with PEDAL_TEST_REOPEN=1 to verify persistence.
public sealed class VerticalSliceValidator : MonoBehaviour
{
    private const string ProfileKey = "Pedal.Validation.Profile";
    internal static string TestDirectory => SessionState.GetString(ProfileKey, "") is string p && p.Length > 0 ? p : null;
    private static bool failed;

    public static void Run()
    {
        var profile = Environment.GetEnvironmentVariable("PEDAL_TEST_PROFILE");
        if (!Guid.TryParseExact(profile, "N", out _)) throw new ArgumentException("Missing unique PEDAL_TEST_PROFILE");
        string directory = Path.Combine(Path.GetTempPath(), "PedalValidation", profile);
        if (!Reopening && Directory.Exists(directory)) throw new IOException("Test profile already exists");
        SessionState.SetString(ProfileKey, directory);
        EditorSceneManager.OpenScene("Assets/Scenes/FlorestaBicicleta.unity");
        EditorApplication.EnterPlaymode();
    }

    private const string InteractivePhase = "Pedal.Validation.Phase";

    [MenuItem("Tools/Pedal no Bosque/Validar cena %#F8")]
    private static void RunInOpenEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/FlorestaBicicleta.unity")
        {
            Debug.LogWarning("Abra a cena FlorestaBicicleta em modo de edição para validar.");
            return;
        }
        string profile = Path.Combine(Path.GetTempPath(), "PedalValidation", Guid.NewGuid().ToString("N"));
        SessionState.SetString(ProfileKey, profile);
        SessionState.SetInt(InteractivePhase, 1);
        Record("Inicio do teste interativo");
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void WatchPlayMode()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode || SessionState.GetInt(InteractivePhase, 0) == 0) return;
            if (SessionState.GetInt(InteractivePhase, 0) == 2)
            {
                SessionState.SetInt(InteractivePhase, 3);
                EditorApplication.delayCall += EditorApplication.EnterPlaymode;
            }
            else
            {
                SessionState.EraseInt(InteractivePhase);
                SessionState.EraseString(ProfileKey);
            }
        };
    }

    private static void Record(string message)
    {
        Debug.Log("[Pedal Test] " + message);
        if (SessionState.GetInt(InteractivePhase, 0) > 0)
            File.AppendAllText("Temp/PedalPlayMode.log", DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
    }

    private static void Finish(bool success)
    {
        Record(success ? "PASSED" : "FAILED");
        if (SessionState.GetInt(InteractivePhase, 0) == 0)
        {
            EditorApplication.Exit(success ? 0 : 1);
            return;
        }
        if (success && !Reopening) SessionState.SetInt(InteractivePhase, 2);
        EditorApplication.ExitPlaymode();
    }

    private static bool Reopening => SessionState.GetInt(InteractivePhase, 0) == 3 ||
        Environment.GetEnvironmentVariable("PEDAL_TEST_REOPEN") == "1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartValidation()
    {
        if (TestDirectory == null) return;
        failed = false;
        Application.logMessageReceived += CaptureError;
        new GameObject("VerticalSliceValidator").AddComponent<VerticalSliceValidator>();
    }

    private static void CaptureError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        Record("OK " + label);
    }

    private static IEnumerator Until(Func<bool> predicate, float seconds, string label)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
        Check(predicate(), label);
    }

    private static void Click(string name)
    {
        var button = GameObject.Find(name)?.GetComponent<Button>();
        Check(button && button.isActiveAndEnabled && button.IsInteractable(), name);
        button.onClick.Invoke();
    }

    private IEnumerator Start()
    {
        // Flatten nested coroutines so any failed step exits the batch process.
        var stack = new System.Collections.Generic.Stack<IEnumerator>();
        stack.Push(Validate());
        while (stack.Count > 0)
        {
            object current;
            try
            {
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                current = stack.Peek().Current;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Record(e.Message);
                Finish(false);
                yield break;
            }
            if (current is IEnumerator nested) stack.Push(nested);
            else yield return current;
        }
        Finish(!failed);
    }

    private IEnumerator Validate()
    {
        yield return null;
        var progress = PlayerProgressManager.Instance;
        Check(progress && progress.Data != null, "Progresso inicializado");
        var sceneMgr = FindObjectOfType<FlorestaSceneManager>();
        var title = sceneMgr != null ? sceneMgr.TitleScreen : FindObjectOfType<PedalTitleScreenUI>();
        Check(title != null, "Controlador de front-end presente");
        if (Reopening)
        {
            Check(progress.Data.totalDeliveries == 1, "Entrega preservada em outra sessao");
            Check(GameObject.Find("BtnContinuar") != null, "Botao Continuar ativo ao reabrir com save");
            Check(SaveSystem.Save(progress.Data), "Backup de teste criado");
            string primary = Path.Combine(TestDirectory, "pedal_no_bosque_save.json");
            string backup = Path.Combine(TestDirectory, "pedal_no_bosque_save.bak");
            string original = File.ReadAllText(backup);
            File.WriteAllText(primary, "{");
            Check(SaveSystem.Load().totalDeliveries == 1, "Entrega recuperada do backup");
            Check(File.ReadAllText(backup) == original, "Backup preservado durante a recuperacao");
            Check(JsonUtility.FromJson<PlayerSaveData>(File.ReadAllText(primary)).totalDeliveries == 1,
                "Arquivo principal reparado");
            yield break;
        }
        Check(progress.Data.totalDeliveries == 0, "Perfil isolado novo");
        Check(title.CurrentState == GameFlowState.TitleScreen, "Estado inicial na Tela Inicial");
        Check(GameObject.Find("BtnContinuar") == null, "Botao Continuar oculto sem progresso salvo");

        // Testa Tela de Configuracoes a partir da Tela Inicial
        Click("BtnConfiguracoes");
        Check(title.CurrentState == GameFlowState.Settings && GameObject.Find("SettingsPanel"), "Tela de Configuracoes aberta");
        float prevMusic = progress.Data.musicVolume;
        Click("BtnMusicaMenos");
        Click("BtnSalvarConfiguracoes");
        Check(title.CurrentState == GameFlowState.TitleScreen && SaveSystem.Load().musicVolume < prevMusic, "Configuracoes salvas e retorno ao menu");

        var cover = GameObject.Find("BosqueAoAmanhecer")?.GetComponent<RawImage>();
        Check(cover && cover.texture && cover.isActiveAndEnabled, "Ilustracao de abertura carregada");
        Check(GameObject.Find("BtnJogar/Play"), "Icone do botao Jogar presente");
        Click("BtnJogar");
        Check(!cover.gameObject.activeInHierarchy, "Abertura fechada ao jogar");
        Check(title.CurrentState == GameFlowState.Playing, "Estado Playing ativo apos Jogar");
        var bike = FindObjectOfType<BikeGirlController>();
        var day = FindObjectOfType<DayNightCycleController>();
        var initialPeriod = day.CurrentPeriod;
        var animal = FindObjectOfType<InfiniteTrackGenerator>().PrimarySquirrel;
        Check(animal, "Esquilo inicial presente");
        float initialX = bike.transform.position.x;
        bike.SetUISlow(true);
        yield return Until(() => bike.isNearKitchen && GameObject.Find("KitchenPanel"), 30, "Cozinha acessivel pela estrada");
        bike.SetUISlow(false);
        Check(bike.transform.position.x > initialX, "Pedalada deslocou a bicicleta");

        // Testa Pausa real e Retomada
        Click("BtnPausa");
        Check(title.CurrentState == GameFlowState.Paused && GameObject.Find("PausePanel") && Time.timeScale == 0f && bike.inputLocked,
            "Jogo pausado de verdade com Time.timeScale=0");
        Click("BtnPausaContinuar");
        Check(title.CurrentState == GameFlowState.Playing && Time.timeScale == 1f && !bike.inputLocked,
            "Partida retomada apos pausa");

        Click("BtnPrepSanduiche");
        Click("BtnAutoPrep");
        yield return Until(() => bike.foodBasket.Contains(FoodType.Sanduiche), 15, "Sanduiche preparado");
        var order = animal.wantedFood;
        var expected = bike.foodBasket.ToList();
        if (expected.Count == bike.maxBasketSize) expected.RemoveAt(0);
        expected.Add(order);
        Click("FoodCard_" + order);
        Check(bike.foodBasket.SequenceEqual(expected), "Cesta abastecida com o pedido");
        bike.SetUISlow(true);
        yield return Until(() => Mathf.Abs(animal.transform.position.x - bike.transform.position.x) < 5f, 30, "Animal ao alcance");
        bike.SetUISlow(false);

        // Testa Desafio Nao Concluido e opcao Tentar Novamente sem perder progresso
        sceneMgr.StartSpecialChallenge(0.3f);
        yield return Until(() => title.CurrentState == GameFlowState.ChallengeFailed && GameObject.Find("ChallengeFailedPanel"),
            5, "Tela de Desafio Nao Concluido exibida ao expirar tempo");
        Click("BtnTentarNovamente");
        Check(title.CurrentState == GameFlowState.Playing && sceneMgr.isSpecialChallengeActive,
            "Desafio reiniciado via Tentar Novamente");

        // Garante que a cesta mantem a lista esperada antes da entrega
        bike.foodBasket.Clear();
        foreach (var item in expected) bike.TryAddFoodToBasket(item);

        expected.Remove(order);
        int meals = animal.mealsEaten;
        Click("BtnComida");
        yield return Until(() => animal.mealsEaten > meals, 10, "Animal recebeu o alimento");
        yield return Until(() => title.CurrentState == GameFlowState.MissionComplete && GameObject.Find("MissionCompletePanel"),
            5, "Tela de Missao Concluida e recompensas exibidas");
        Click("BtnContinuarExplorando");
        Check(title.CurrentState == GameFlowState.Playing, "Retorno a exploracao apos concluir missao");

        yield return new WaitForSeconds(1);
        Check(animal.mealsEaten == meals + 1 && bike.foodBasket.SequenceEqual(expected)
            && progress.Data.totalDeliveries == 1, "Uma refeicao, um consumo e uma entrega registrada");
        Check(SaveSystem.Load().totalDeliveries == 1, "Entrega gravada no disco");

        // Testa Pausar -> Salvar e Voltar ao Menu -> Continuar Partida -> Sair
        Click("BtnPausa");
        Click("BtnPausaMenuPrincipal");
        Check(title.CurrentState == GameFlowState.TitleScreen && GameObject.Find("BtnContinuar") != null,
            "Retorno ao Menu Principal com botao Continuar habilitado");
        Click("BtnSair");
        Check(title.CurrentState == GameFlowState.ExitConfirm && GameObject.Find("ExitConfirmPanel"),
            "Modal de Confirmacao de Saida exibido");
        Click("BtnConfirmarSaida");
        Check(title.LastExitConfirmed, "Saida confirmada com salvamento seguro");
        Click("BtnContinuar");
        Check(title.CurrentState == GameFlowState.Playing, "Partida retomada pelo botao Continuar");
        bike.SetUIFast(true);
        yield return Until(() => bike.transform.position.x > 50f, 35, "Travessia da primeira emenda");
        yield return Until(() => bike.currentSlopeLabel.StartsWith("SUBIDA", StringComparison.Ordinal), 180, "Entrada na subida");
        yield return Until(() => bike.currentSlopeLabel.StartsWith("DESCIDA", StringComparison.Ordinal), 45, "Entrada na descida");
        bike.SetUIFast(false);
        yield return Until(() => day.CurrentPeriod != initialPeriod, day.secondsPerPeriod + 10, "Mudanca do periodo do dia");
    }
}
#endif
