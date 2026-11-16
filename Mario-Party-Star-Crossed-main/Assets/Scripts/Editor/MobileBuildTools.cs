#if UNITY_EDITOR
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Cenas habilitadas em File > Build Profiles (a mesma lista do build de PC).
    /// Antes era uma lista fixa aqui e o mapa Ilha das Oportunidades ficava fora do APK.
    /// </summary>
    private static string[] BuildScenes =>
        EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();

    [MenuItem("Mobile/1. Configurar Projeto para Mobile (Android)")]
    public static void ConfigureMobileSettings() {
        Debug.Log("[MobileBuildTools] Configurando Player Settings para Android...");
        var android = NamedBuildTarget.Android;

        // Nome do produto e identificador do pacote
        PlayerSettings.productName = GameName;
        PlayerSettings.companyName = "DJDE";
        PlayerSettings.SetApplicationIdentifier(android, PackageName);

        // Orientações de tela (apenas Paisagem) — vale desde a splash, antes de qualquer script rodar
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        // Android 7.1+ (API 25); alvo = maior SDK instalado (exigência da Play Store)
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // IL2CPP + 64 bits (exigência da Play Store); ARMv7 mantém aparelhos antigos/Android Go
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        // O online usa TCP direto (System.Net.Sockets): garante a permissão INTERNET no manifesto
        PlayerSettings.Android.forceInternetPermission = true;

        // O notch é tratado pela área segura da UI (TemaUI.AreaSegura)
        PlayerSettings.Android.renderOutsideSafeArea = true;

        AssetDatabase.SaveAssets();
        Debug.Log("[MobileBuildTools] ✓ Player Settings: paisagem, API 25+, IL2CPP, ARM64+ARMv7, INTERNET.");
    }

    [MenuItem("Mobile/2. Compilar APK (Desenvolvimento)")]
    public static void BuildAndroidApk() {
        Compilar(appBundle: false, release: false);
    }

    /// <summary>
    /// AAB assinado para a Play Store. Exige a keystore configurada em
    /// Project Settings > Player > Android > Publishing Settings (a senha não fica no projeto).
    /// </summary>
    [MenuItem("Mobile/3. Compilar AAB (Release - Play Store)")]
    public static void BuildAndroidAab() {
        if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName)) {
            Debug.LogError("[MobileBuildTools] ✗ Configure a keystore em Project Settings > Player > Android > Publishing Settings antes do build de release.");
            return;
        }
        Compilar(appBundle: true, release: true);
    }

    private static void Compilar(bool appBundle, bool release) {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) {
            Debug.LogError("[MobileBuildTools] ✗ Módulo Android não instalado nesta versão do Unity. Instale pelo Unity Hub: Installs > ⚙ > Add modules > Android Build Support (com OpenJDK e Android SDK & NDK Tools).");
            return;
        }

        ConfigureMobileSettings();

        string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
        Directory.CreateDirectory(outputDir);

        string versao = PlayerSettings.bundleVersion;
        string caminho = Path.Combine(outputDir, appBundle ? $"CaminhoDoSucesso-{versao}.aab" : "CaminhoDoSucesso.apk");
        EditorUserBuildSettings.buildAppBundle = appBundle;
        Debug.Log($"[MobileBuildTools] Iniciando build em: {caminho} ({BuildScenes.Length} cenas)");

        var opcoes = new BuildPlayerOptions {
            scenes = BuildScenes,
            locationPathName = caminho,
            target = BuildTarget.Android,
            options = release ? BuildOptions.None : (BuildOptions.Development | BuildOptions.AllowDebugging)
        };

        var report = BuildPipeline.BuildPlayer(opcoes);
        EditorUserBuildSettings.buildAppBundle = false;
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) {
            Debug.Log($"[MobileBuildTools] ✓ Build concluída! Tamanho: {report.summary.totalSize / (1024 * 1024)} MB");
        } else {
            Debug.LogError($"[MobileBuildTools] ✗ Falha na build: {report.summary.totalErrors} erros encontrados.");
        }
    }
}
#endif
