#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Ferramentas de Editor e CI/CD para automação de build e configurações Mobile (Android/iOS).
/// Pode ser executado via menus no Unity Editor ou via linha de comando com Unity CLI.
/// </summary>
public static class MobileBuildTools {

    private const string PackageName = "com.djde.caminhodosucesso";
    private const string GameName = "Caminho do Sucesso";

    private static readonly string[] BuildScenes = new string[] {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/MVP.unity",
        "Assets/Scenes/GameOverScene.unity"
    };

    [MenuItem("Mobile/1. Configurar Projeto para Mobile (Android)")]
    public static void ConfigureMobileSettings() {
        Debug.Log("[MobileBuildTools] Configurando Player Settings para Android...");

        // Nome do produto e identificador do pacote
        PlayerSettings.productName = GameName;
        PlayerSettings.companyName = "DJDE";
#if UNITY_2021_2_OR_NEWER
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, PackageName);
#else
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
#endif

        // Orientações de tela (apenas Paisagem)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // Configurações Android
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0+
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        AssetDatabase.SaveAssets();
        Debug.Log("[MobileBuildTools] ✓ Player Settings configurado com sucesso para Paisagem e ARM64!");
    }

    [MenuItem("Mobile/2. Compilar APK (Desenvolvimento)")]
    public static void BuildAndroidApk() {
        ConfigureMobileSettings();

        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        if (!Directory.Exists(outputDir)) {
            Directory.CreateDirectory(outputDir);
        }

        string apkPath = Path.Combine(outputDir, "CaminhoDoSucesso.apk");
        Debug.Log($"[MobileBuildTools] Iniciando build do APK em: {apkPath}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions {
            scenes = BuildScenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
            Debug.Log($"[MobileBuildTools] ✓ Build concluída com sucesso! Tamanho: {report.summary.totalSize / (1024 * 1024)} MB");
        } else {
            Debug.LogError($"[MobileBuildTools] ✗ Falha na build: {report.summary.totalErrors} erros encontrados.");
        }
    }
}
#endif
