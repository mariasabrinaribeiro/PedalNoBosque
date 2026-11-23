using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FlorestaBicicleta.EditorTools
{
    /// <summary>
    /// Menu e Painel unificado de Build, Deploy & Launch para o PEDAL NO BOSQUE:
    /// - Android (APK para testes em celular/tablet e AAB para Google Play Store)
    /// - Deploy & Launch direto no celular/tablet Android via USB (ADB)
    /// - iOS / iPadOS (Exportação de Projeto Xcode pronto para iPhone/iPad e App Store)
    /// - Windows 64-bit (.exe) com opção Build & Launch
    /// - WebGL (Navegador)
    /// - Checklist pré-build e ajuste rápido das configurações mobile (Orientação Paisagem, Bundle ID e Cena Principal).
    /// </summary>
    public class BuildDeployLaunchMenu : EditorWindow
    {
        private const string MenuRoot = "Pedal no Bosque/Build, Deploy & Launch/";
        private const string DefaultBundleId = "com.wisocki.pedalnobosque";
        private const string MainScenePath = "Assets/Scenes/FlorestaBicicleta.unity";

        private string productName = "Pedal no Bosque";
        private string companyName = "Wisocki";
        private string bundleId = DefaultBundleId;
        private string appVersion = "1.0.0";
        private int androidVersionCode = 1;
        private string iosBuildNumber = "1";
        private bool developmentBuild = false;
        private bool autoIncrementBuildNumber = true;

        private string statusLog = "Pronto. Escolha uma plataforma para Build, Deploy ou Launch.";
        private MessageType statusType = MessageType.Info;
        private string lastAndroidApkPath = "";
        private string lastWindowsExePath = "";
        private string adbDevicesOutput = "";
        private Vector2 scrollPos;

        // ─── Itens do Menu Superior ──────────────────────────────────

        [MenuItem(MenuRoot + "Abrir Painel de Build, Deploy & Launch...", false, 0)]
        public static void OpenWindow()
        {
            BuildDeployLaunchMenu win = GetWindow<BuildDeployLaunchMenu>("Build, Deploy & Launch");
            win.minSize = new Vector2(540f, 620f);
            win.LoadFromPlayerSettings();
            win.RefreshAdbDevices();
            win.Show();
        }

        [MenuItem(MenuRoot + "Preparar Configurações Mobile (Android + iPhone + Tablet)", false, 20)]
        public static void MenuApplyMobileSettings()
        {
            ApplyRecommendedMobileSettings(DefaultBundleId, "Pedal no Bosque", "Wisocki", PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode, PlayerSettings.iOS.buildNumber);
            EditorUtility.DisplayDialog(
                "Pedal no Bosque — Config Mobile",
                "Configurações aplicadas:\n\n" +
                "- Orientação travada em Paisagem (Landscape Left / Right)\n" +
                "- Bundle ID: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) + "\n" +
                "- Cena principal garantida nas Build Settings (" + MainScenePath + ")\n" +
                "- Tela cheia / Render fora da Safe Area ativado para celular, tablet e iPhone.",
                "OK");
        }

        [MenuItem(MenuRoot + "Build Android APK (Celular & Tablet)", false, 40)]
        public static void MenuBuildAndroidApk()
        {
            BuildAndroid(buildAppBundle: false, development: false, deployAndLaunch: false, out _, out _);
        }

        [MenuItem(MenuRoot + "Build Android AAB (Google Play Store)", false, 41)]
        public static void MenuBuildAndroidAab()
        {
            BuildAndroid(buildAppBundle: true, development: false, deployAndLaunch: false, out _, out _);
        }

        [MenuItem(MenuRoot + "Build, Deploy & Launch no Android (USB)", false, 42)]
        public static void MenuBuildDeployLaunchAndroid()
        {
            BuildAndroid(buildAppBundle: false, development: false, deployAndLaunch: true, out _, out _);
        }

        [MenuItem(MenuRoot + "Build iOS (Projeto Xcode para iPhone & iPad)", false, 60)]
        public static void MenuBuildIOS()
        {
            BuildIOS(development: false, out _, out _);
        }

        [MenuItem(MenuRoot + "Build Windows (PC .exe)", false, 80)]
        public static void MenuBuildWindows()
        {
            BuildWindows(development: false, launchAfterBuild: false, out _, out _);
        }

        [MenuItem(MenuRoot + "Build & Launch Windows (PC)", false, 81)]
        public static void MenuBuildAndLaunchWindows()
        {
            BuildWindows(development: false, launchAfterBuild: true, out _, out _);
        }

        [MenuItem(MenuRoot + "Build WebGL (Navegador)", false, 100)]
        public static void MenuBuildWebGL()
        {
            BuildWebGL(development: false, out _, out _);
        }

        [MenuItem(MenuRoot + "Abrir Pasta de Builds", false, 140)]
        public static void MenuOpenBuildsFolder()
        {
            string dir = GetBuildsRootDirectory();
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }

        // ─── Ciclo de Vida da Janela ─────────────────────────────────

        private void OnEnable()
        {
            LoadFromPlayerSettings();
        }

        private void LoadFromPlayerSettings()
        {
            productName = string.IsNullOrWhiteSpace(PlayerSettings.productName) ? "Pedal no Bosque" : PlayerSettings.productName;
            companyName = string.IsNullOrWhiteSpace(PlayerSettings.companyName) ? "Wisocki" : PlayerSettings.companyName;

            string currentId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (string.IsNullOrWhiteSpace(currentId) || currentId.Contains("DefaultCompany") || currentId.Contains("com.Company"))
                currentId = DefaultBundleId;
            bundleId = currentId;

            appVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "1.0.0" : PlayerSettings.bundleVersion;
            androidVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            iosBuildNumber = string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber) ? androidVersionCode.ToString() : PlayerSettings.iOS.buildNumber;

            lastAndroidApkPath = FindNewestFile(Path.Combine(GetBuildsRootDirectory(), "Android"), "*.apk");
            lastWindowsExePath = FindNewestFile(Path.Combine(GetBuildsRootDirectory(), "Windows"), "*.exe");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.Space(8f);
            EditorGUILayout.LabelField("PEDAL NO BOSQUE — BUILD, DEPLOY & LAUNCH", headerStyle);
            EditorGUILayout.LabelField("Painel de Empacotamento e Lançamento (Android, iPhone/iPad, Windows e WebGL)", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(8f);

            DrawReleaseSettingsSection();
            GUILayout.Space(8f);
            DrawPreflightStatusSection();
            GUILayout.Space(8f);
            DrawBuildSection();
            GUILayout.Space(8f);
            DrawDeployAndLaunchSection();
            GUILayout.Space(8f);

            EditorGUILayout.HelpBox(statusLog, statusType);
            GUILayout.Space(8f);

            EditorGUILayout.EndScrollView();
        }

        private void DrawReleaseSettingsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. Identidade e Versão do App", EditorStyles.boldLabel);

            productName = EditorGUILayout.TextField("Nome do Jogo", productName);
            companyName = EditorGUILayout.TextField("Estúdio / Empresa", companyName);
            bundleId = EditorGUILayout.TextField("Package / Bundle ID", bundleId);

            EditorGUILayout.BeginHorizontal();
            appVersion = EditorGUILayout.TextField("Versão (ex: 1.0.0)", appVersion);
            if (GUILayout.Button("Build +1", GUILayout.Width(80f)))
            {
                androidVersionCode++;
                iosBuildNumber = androidVersionCode.ToString();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            androidVersionCode = Mathf.Max(1, EditorGUILayout.IntField("Android Version Code", androidVersionCode));
            iosBuildNumber = EditorGUILayout.TextField("iOS Build Number", iosBuildNumber);
            EditorGUILayout.EndHorizontal();

            developmentBuild = EditorGUILayout.Toggle("Development Build (Logs / Profiler)", developmentBuild);
            autoIncrementBuildNumber = EditorGUILayout.Toggle("Incrementar Build Number após Build OK", autoIncrementBuildNumber);

            GUILayout.Space(4f);
            if (GUILayout.Button("Aplicar Configurações Mobile (Paisagem + Bundle ID + Cena Principal)", GUILayout.Height(28f)))
            {
                ApplyRecommendedMobileSettings(bundleId, productName, companyName, appVersion, androidVersionCode, iosBuildNumber);
                SetStatus("Configurações Mobile e Build Settings salvas com sucesso!", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPreflightStatusSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. Checklist de Plataformas no Unity Hub", EditorStyles.boldLabel);

            bool hasScene = File.Exists(Path.Combine(GetProjectRootDirectory(), MainScenePath));
            bool hasAndroid = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android);
            bool hasIOS = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS);
            bool hasWin = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            bool hasWeb = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            EditorGUILayout.LabelField($"Cena Principal ({MainScenePath}): {(hasScene ? "OK" : "NÃO ENCONTRADA")}");
            EditorGUILayout.LabelField(
                $"Módulos instalados ->  Android: {(hasAndroid ? "OK" : "Falta")}  |  iOS: {(hasIOS ? "OK" : "Falta")}  |  Windows: {(hasWin ? "OK" : "Falta")}  |  WebGL: {(hasWeb ? "OK" : "Falta")}");

            if (!hasAndroid || !hasIOS)
            {
                EditorGUILayout.HelpBox(
                    "Para gerar builds Android ou iOS, verifique no Unity Hub > Installs > Add Modules se 'Android Build Support' (com SDK/NDK/OpenJDK) e 'iOS Build Support' estão marcados.",
                    MessageType.None);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBuildSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. BUILD — Gerar Pacotes", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build Android APK\n(Celular & Tablet)", GUILayout.Height(42f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildAndroid(false, developmentBuild, false, out string outPath, out string msg))
                {
                    lastAndroidApkPath = outPath;
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }

            if (GUILayout.Button("Build Android AAB\n(Google Play Store)", GUILayout.Height(42f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildAndroid(true, developmentBuild, false, out _, out string msg))
                {
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build iOS / iPad\n(Exportar Projeto Xcode)", GUILayout.Height(42f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildIOS(developmentBuild, out _, out string msg))
                {
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }

            if (GUILayout.Button("Build Windows\n(PC 64-bit .exe)", GUILayout.Height(42f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildWindows(developmentBuild, false, out string outPath, out string msg))
                {
                    lastWindowsExePath = outPath;
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }

            if (GUILayout.Button("Build WebGL\n(Navegador)", GUILayout.Height(42f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildWebGL(developmentBuild, out _, out string msg))
                {
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawDeployAndLaunchSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("4. DEPLOY & LAUNCH — Instalar e Abrir", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build + Deploy + Launch Android (USB)", GUILayout.Height(36f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildAndroid(false, developmentBuild, true, out string outPath, out string msg))
                {
                    lastAndroidApkPath = outPath;
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }

            if (GUILayout.Button("Deploy & Launch Último APK", GUILayout.Height(36f)))
            {
                if (string.IsNullOrEmpty(lastAndroidApkPath) || !File.Exists(lastAndroidApkPath))
                    lastAndroidApkPath = FindNewestFile(Path.Combine(GetBuildsRootDirectory(), "Android"), "*.apk");

                if (DeployAndLaunchAndroidApk(lastAndroidApkPath, bundleId, out string msg))
                    SetStatus(msg, MessageType.Info);
                else
                    SetStatus(msg, MessageType.Error);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build & Launch Windows (PC)", GUILayout.Height(32f)))
            {
                ApplyCurrentWindowSettings();
                if (BuildWindows(developmentBuild, true, out string outPath, out string msg))
                {
                    lastWindowsExePath = outPath;
                    OnBuildSucceeded();
                    SetStatus(msg, MessageType.Info);
                }
                else SetStatus(msg, MessageType.Error);
            }

            if (GUILayout.Button("Abrir Último Windows .exe", GUILayout.Height(32f)))
            {
                if (string.IsNullOrEmpty(lastWindowsExePath) || !File.Exists(lastWindowsExePath))
                    lastWindowsExePath = FindNewestFile(Path.Combine(GetBuildsRootDirectory(), "Windows"), "*.exe");

                if (LaunchWindowsExe(lastWindowsExePath, out string msg))
                    SetStatus(msg, MessageType.Info);
                else
                    SetStatus(msg, MessageType.Warning);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Verificar Dispositivos Android (ADB)", GUILayout.Height(26f)))
            {
                RefreshAdbDevices();
            }
            if (GUILayout.Button("Abrir Pasta Builds/", GUILayout.Height(26f)))
            {
                MenuOpenBuildsFolder();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(adbDevicesOutput))
            {
                EditorGUILayout.LabelField("ADB: " + adbDevicesOutput, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void ApplyCurrentWindowSettings()
        {
            ApplyRecommendedMobileSettings(bundleId, productName, companyName, appVersion, androidVersionCode, iosBuildNumber);
        }

        private void OnBuildSucceeded()
        {
            if (!autoIncrementBuildNumber) return;
            androidVersionCode++;
            iosBuildNumber = androidVersionCode.ToString();
            PlayerSettings.Android.bundleVersionCode = androidVersionCode;
            PlayerSettings.iOS.buildNumber = iosBuildNumber;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusLog = $"[{DateTime.Now:HH:mm:ss}] {message}";
            statusType = type;
            Repaint();
        }

        private void RefreshAdbDevices()
        {
            string adb = ResolveAdbPath();
            if (string.IsNullOrEmpty(adb))
            {
                adbDevicesOutput = "adb não encontrado (instale Android SDK / Platform-Tools no Unity Hub).";
                return;
            }
            if (RunProcess(adb, "devices", out string stdout, out string stderr))
            {
                string[] lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                adbDevicesOutput = lines.Length == 0
                    ? "Nenhum dispositivo USB conectado (ative Depuração USB no celular)."
                    : string.Join(" | ", lines);
            }
            else
            {
                adbDevicesOutput = "Erro ao consultar ADB: " + stderr;
            }
        }

        // ─── Operações de Configuração e Build ───────────────────────

        public static void ApplyRecommendedMobileSettings(
            string packageId,
            string prodName,
            string compName,
            string version,
            int androidCode,
            string iosBuild)
        {
            PlayerSettings.productName = string.IsNullOrWhiteSpace(prodName) ? "Pedal no Bosque" : prodName.Trim();
            PlayerSettings.companyName = string.IsNullOrWhiteSpace(compName) ? "Wisocki" : compName.Trim();
            PlayerSettings.bundleVersion = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim();

            string safeId = string.IsNullOrWhiteSpace(packageId) ? DefaultBundleId : packageId.Trim();
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, safeId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, safeId);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, safeId);

            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, androidCode);
            PlayerSettings.iOS.buildNumber = string.IsNullOrWhiteSpace(iosBuild) ? Mathf.Max(1, androidCode).ToString() : iosBuild.Trim();

            // Orientação paisagem (ideal para jogo lateral 2D em celular, tablet e iPhone)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Configurações recomendadas de loja para Android e iPhone/iPad
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "13.0";

            EnsureMainSceneInBuildSettings();
            AssetDatabase.SaveAssets();
        }

        private static string[] EnsureMainSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes != null
                ? EditorBuildSettings.scenes.Where(s => s != null && !string.IsNullOrEmpty(s.path) && File.Exists(Path.Combine(GetProjectRootDirectory(), s.path))).ToList()
                : new List<EditorBuildSettingsScene>();

            bool hasMain = scenes.Any(s => string.Equals(s.path, MainScenePath, StringComparison.OrdinalIgnoreCase));
            if (!hasMain && File.Exists(Path.Combine(GetProjectRootDirectory(), MainScenePath)))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            string[] enabledPaths = scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (enabledPaths.Length == 0 && File.Exists(Path.Combine(GetProjectRootDirectory(), MainScenePath)))
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
                enabledPaths = new[] { MainScenePath };
            }
            return enabledPaths;
        }

        private static bool BuildAndroid(bool buildAppBundle, bool development, bool deployAndLaunch, out string outputPath, out string message)
        {
            outputPath = "";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                message = "Módulo Android Build Support não está instalado neste Unity Editor. Instale pelo Unity Hub > Installs > Add Modules.";
                Debug.LogError("[BuildDeployLaunch] " + message);
                return false;
            }

            string[] scenes = EnsureMainSceneInBuildSettings();
            if (scenes.Length == 0)
            {
                message = "Nenhuma cena válida encontrada nas Build Settings.";
                return false;
            }

            string dir = Path.Combine(GetBuildsRootDirectory(), "Android");
            Directory.CreateDirectory(dir);

            string safeVer = SanitizeFilePart(PlayerSettings.bundleVersion);
            string ext = buildAppBundle ? ".aab" : ".apk";
            outputPath = Path.Combine(dir, $"PedalNoBosque_v{safeVer}_b{PlayerSettings.Android.bundleVersionCode}{ext}");

            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            BuildOptions opts = development ? BuildOptions.Development | BuildOptions.AllowDebugging : BuildOptions.None;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = opts
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                message = $"Falha no Build Android ({report.summary.result}). Veja o Console para detalhes.";
                return false;
            }

            string builtMsg = $"Build Android concluído ({FormatBytes(report.summary.totalSize)}): {outputPath}";
            Debug.Log("[BuildDeployLaunch] " + builtMsg);

            if (deployAndLaunch && !buildAppBundle)
            {
                string pkg = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
                if (DeployAndLaunchAndroidApk(outputPath, pkg, out string deployMsg))
                {
                    message = builtMsg + "\n" + deployMsg;
                    return true;
                }
                message = builtMsg + "\nAviso Deploy: " + deployMsg;
                return false;
            }

            EditorUtility.RevealInFinder(outputPath);
            message = builtMsg;
            return true;
        }

        private static bool BuildIOS(bool development, out string outputDir, out string message)
        {
            outputDir = "";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                message = "Módulo iOS Build Support não está instalado neste Unity Editor. Instale pelo Unity Hub > Installs > Add Modules.";
                Debug.LogError("[BuildDeployLaunch] " + message);
                return false;
            }

            string[] scenes = EnsureMainSceneInBuildSettings();
            if (scenes.Length == 0)
            {
                message = "Nenhuma cena válida encontrada nas Build Settings.";
                return false;
            }

            string safeVer = SanitizeFilePart(PlayerSettings.bundleVersion);
            outputDir = Path.Combine(GetBuildsRootDirectory(), "iOS", $"PedalNoBosque_Xcode_v{safeVer}");
            Directory.CreateDirectory(outputDir);

            BuildOptions opts = development ? BuildOptions.Development : BuildOptions.None;
            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDir,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = opts
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                message = $"Falha no Build iOS ({report.summary.result}). Veja o Console para detalhes.";
                return false;
            }

            EditorUtility.RevealInFinder(outputDir);
            message = $"Projeto Xcode (iPhone/iPad) exportado em: {outputDir}\nAbra no Mac com Xcode para assinar e enviar ao TestFlight / App Store.";
            Debug.Log("[BuildDeployLaunch] " + message);
            return true;
        }

        private static bool BuildWindows(bool development, bool launchAfterBuild, out string outputPath, out string message)
        {
            outputPath = "";
            string[] scenes = EnsureMainSceneInBuildSettings();
            if (scenes.Length == 0)
            {
                message = "Nenhuma cena válida encontrada nas Build Settings.";
                return false;
            }

            string dir = Path.Combine(GetBuildsRootDirectory(), "Windows");
            Directory.CreateDirectory(dir);
            outputPath = Path.Combine(dir, "PedalNoBosque.exe");

            BuildOptions opts = development ? BuildOptions.Development : BuildOptions.None;
            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = opts
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                message = $"Falha no Build Windows ({report.summary.result}). Veja o Console para detalhes.";
                return false;
            }

            string builtMsg = $"Build Windows concluído ({FormatBytes(report.summary.totalSize)}): {outputPath}";
            Debug.Log("[BuildDeployLaunch] " + builtMsg);

            if (launchAfterBuild)
            {
                if (LaunchWindowsExe(outputPath, out string launchMsg))
                {
                    message = builtMsg + "\n" + launchMsg;
                    return true;
                }
                message = builtMsg + "\n" + launchMsg;
                return false;
            }

            EditorUtility.RevealInFinder(outputPath);
            message = builtMsg;
            return true;
        }

        private static bool BuildWebGL(bool development, out string outputDir, out string message)
        {
            outputDir = "";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                message = "Módulo WebGL Build Support não está instalado neste Unity Editor. Instale pelo Unity Hub.";
                Debug.LogError("[BuildDeployLaunch] " + message);
                return false;
            }

            string[] scenes = EnsureMainSceneInBuildSettings();
            if (scenes.Length == 0)
            {
                message = "Nenhuma cena válida encontrada nas Build Settings.";
                return false;
            }

            outputDir = Path.Combine(GetBuildsRootDirectory(), "WebGL");
            Directory.CreateDirectory(outputDir);

            BuildOptions opts = development ? BuildOptions.Development : BuildOptions.None;
            BuildPlayerOptions playerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = opts
            };

            BuildReport report = BuildPipeline.BuildPlayer(playerOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                message = $"Falha no Build WebGL ({report.summary.result}). Veja o Console para detalhes.";
                return false;
            }

            EditorUtility.RevealInFinder(outputDir);
            message = $"Build WebGL concluído em: {outputDir}";
            Debug.Log("[BuildDeployLaunch] " + message);
            return true;
        }

        // ─── Deploy & Launch Helpers ─────────────────────────────────

        private static bool DeployAndLaunchAndroidApk(string apkPath, string packageName, out string message)
        {
            if (string.IsNullOrEmpty(apkPath) || !File.Exists(apkPath))
            {
                message = "Nenhum APK encontrado. Faça primeiro o Build Android APK.";
                return false;
            }

            string adb = ResolveAdbPath();
            if (string.IsNullOrEmpty(adb))
            {
                message = "ADB não encontrado. Verifique se o Android SDK (Platform-Tools) está instalado no Unity Hub.";
                return false;
            }

            if (!RunProcess(adb, $"install -r \"{apkPath}\"", out string installOut, out string installErr))
            {
                message = $"Falha ao instalar APK via ADB (conecte o celular com Depuração USB ativada):\n{installErr}\n{installOut}";
                return false;
            }

            string safePkg = string.IsNullOrWhiteSpace(packageName) ? DefaultBundleId : packageName.Trim();
            RunProcess(adb, $"shell monkey -p {safePkg} -c android.intent.category.LAUNCHER 1", out _, out _);

            message = $"APK instalado e iniciado no dispositivo Android ({safePkg})!";
            Debug.Log("[BuildDeployLaunch] " + message);
            return true;
        }

        private static bool LaunchWindowsExe(string exePath, out string message)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                message = "Executável Windows não encontrado. Faça primeiro o Build Windows.";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                });
                message = "Jogo iniciado no Windows: " + exePath;
                return true;
            }
            catch (Exception ex)
            {
                message = "Erro ao abrir executável: " + ex.Message;
                return false;
            }
        }

        private static string ResolveAdbPath()
        {
            string exeName = Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";

            string sdkRoot = EditorPrefs.GetString("AndroidSdkRoot", "");
            if (!string.IsNullOrEmpty(sdkRoot))
            {
                string candidate = Path.Combine(sdkRoot, "platform-tools", exeName);
                if (File.Exists(candidate)) return candidate;
            }

            string editorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            if (!string.IsNullOrEmpty(editorDir))
            {
                string embedded = Path.Combine(editorDir, "Data", "PlaybackEngines", "AndroidPlayer", "SDK", "platform-tools", exeName);
                if (File.Exists(embedded)) return embedded;
            }

            string envSdk = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
            if (!string.IsNullOrEmpty(envSdk))
            {
                string candidate = Path.Combine(envSdk, "platform-tools", exeName);
                if (File.Exists(candidate)) return candidate;
            }

            return null;
        }

        private static bool RunProcess(string fileName, string arguments, out string stdout, out string stderr)
        {
            stdout = "";
            stderr = "";
            try
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    proc.Start();
                    stdout = proc.StandardOutput.ReadToEnd();
                    stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(120000);
                    return proc.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                stderr = ex.Message;
                return false;
            }
        }

        private static string GetProjectRootDirectory()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
        }

        private static string GetBuildsRootDirectory()
        {
            return Path.Combine(GetProjectRootDirectory(), "Builds");
        }

        private static string FindNewestFile(string directory, string pattern)
        {
            if (!Directory.Exists(directory)) return "";
            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault() ?? "";
        }

        private static string SanitizeFilePart(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "1_0_0";
            foreach (char c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');
            return raw.Replace(' ', '_');
        }

        private static string FormatBytes(ulong bytes)
        {
            if (bytes >= 1024UL * 1024UL) return $"{bytes / (1024.0 * 1024.0):0.0} MB";
            if (bytes >= 1024UL) return $"{bytes / 1024.0:0.0} KB";
            return $"{bytes} B";
        }
    }
}
