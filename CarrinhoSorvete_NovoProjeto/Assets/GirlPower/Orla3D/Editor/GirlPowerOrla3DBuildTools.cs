using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-click multiplatform release build tools for Baía do Avesso (PC Windows, WebGL, Android).
/// Configures Product Name, Company Name, and Primary Scene (index 0) cleanly before building.
/// </summary>
public static class GirlPowerOrla3DBuildTools
{
    private const string MainScenePath = "Assets/Scenes/Girl Power - Orla 3D (Animal Crossing).unity";

    [MenuItem("Girl Power/Preparar Build Settings (Cena Principal Orla 3D)")]
    public static void ConfigureBuildSettings()
    {
        PlayerSettings.companyName = "Girl Power Studio";
        PlayerSettings.productName = "Baia do Avesso";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[Baía do Avesso] Build Settings configurados! Cena principal: " + MainScenePath);
    }

    [MenuItem("Girl Power/Build Windows PC (.exe)")]
    public static void BuildWindowsPC()
    {
        ConfigureBuildSettings();
        string outDir = Path.GetFullPath("Builds/Windows");
        Directory.CreateDirectory(outDir);
        string exePath = Path.Combine(outDir, "BaiaDoAvesso.exe");
        RunBuild(BuildTarget.StandaloneWindows64, exePath);
    }

    [MenuItem("Girl Power/Build WebGL (Navegador / itch.io)")]
    public static void BuildWebGL()
    {
        ConfigureBuildSettings();
        string outDir = Path.GetFullPath("Builds/WebGL");
        Directory.CreateDirectory(outDir);
        RunBuild(BuildTarget.WebGL, outDir);
    }

    [MenuItem("Girl Power/Build Android (.apk)")]
    public static void BuildAndroidAPK()
    {
        ConfigureBuildSettings();
        string outDir = Path.GetFullPath("Builds/Android");
        Directory.CreateDirectory(outDir);
        string apkPath = Path.Combine(outDir, "BaiaDoAvesso.apk");
        RunBuild(BuildTarget.Android, apkPath);
    }

    private static void RunBuild(BuildTarget target, string locationPath)
    {
        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = locationPath,
            target = target,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Baía do Avesso] Build concluído com sucesso em: " + locationPath + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
            EditorUtility.RevealInFinder(locationPath);
        }
        else
        {
            Debug.LogError("[Baía do Avesso] Build falhou (" + target + "): " + report.summary.result);
        }
    }
}
