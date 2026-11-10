using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configurações do jogador (PlayerPrefs "Cfg_*"), aplicadas ao iniciar e a cada cena carregada.
/// Usadas pelo MenuConfiguracoes (menu principal e pausa no tabuleiro).
/// </summary>
public static class Configuracoes {
    const string KGeral = "Cfg_VolumeGeral", KMusica = "Cfg_VolumeMusica", KEfeitos = "Cfg_VolumeEfeitos";
    const string KTimer = "Cfg_TimerTurno", KQualidade = "Cfg_Qualidade", KFps = "Cfg_Fps", KTelaCheia = "Cfg_TelaCheia";

    public static float VolumeGeral { get => PlayerPrefs.GetFloat(KGeral, 1f); set { PlayerPrefs.SetFloat(KGeral, value); AplicarAudio(); } }
    public static float VolumeMusica { get => PlayerPrefs.GetFloat(KMusica, 0.7f); set { PlayerPrefs.SetFloat(KMusica, value); AplicarAudio(); } }
    public static float VolumeEfeitos { get => PlayerPrefs.GetFloat(KEfeitos, 1f); set { PlayerPrefs.SetFloat(KEfeitos, value); AplicarAudio(); } }

    public static bool Vibracao { get => HapticFeedback.IsEnabled; set => HapticFeedback.IsEnabled = value; }

    /// <summary>Limite de 45 s por turno em partidas locais (no online o limite é sempre ativo).</summary>
    public static bool TimerTurno {
        get => PlayerPrefs.GetInt(KTimer, 1) == 1;
        set {
            PlayerPrefs.SetInt(KTimer, value ? 1 : 0);
            if (NetworkTurnManager.Instance != null) NetworkTurnManager.Instance.habilitarTimerLocal = value;
        }
    }

    /// <summary>0 = Baixa, 1 = Média, 2 = Alta (mapeia para os níveis de qualidade/URP do projeto).</summary>
    public static int Qualidade {
        get => PlayerPrefs.GetInt(KQualidade, Application.isMobilePlatform ? 1 : 2);
        set { PlayerPrefs.SetInt(KQualidade, Mathf.Clamp(value, 0, 2)); AplicarVideo(); }
    }

    public static int FpsAlvo {
        get => PlayerPrefs.GetInt(KFps, 60);
        set { PlayerPrefs.SetInt(KFps, value); AplicarVideo(); }
    }

    public static bool TelaCheia {
        get => PlayerPrefs.GetInt(KTelaCheia, Screen.fullScreen ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(KTelaCheia, value ? 1 : 0); if (!Application.isMobilePlatform) Screen.fullScreen = value; }
    }

    public static void Salvar() => PlayerPrefs.Save();

    // ------------------------------------------------------------------ aplicação
    static readonly Dictionary<int, float> volumesBase = new Dictionary<int, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar() {
        SceneManager.sceneLoaded += (_, __) => AplicarTudo();
        AplicarTudo();
    }

    public static void AplicarTudo() {
        AplicarVideo();
        AplicarAudio();
        if (NetworkTurnManager.Instance != null) NetworkTurnManager.Instance.habilitarTimerLocal = TimerTurno;
    }

    public static void AplicarVideo() {
        // Níveis do projeto: 0-1 = URP_Low, 2-3 = URP_Medium, 4-5 = URP_High
        int[] nivelPorQualidade = { 1, 3, 5 };
        int alvo = nivelPorQualidade[Mathf.Clamp(Qualidade, 0, 2)];
        if (alvo < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != alvo) QualitySettings.SetQualityLevel(alvo, true);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FpsAlvo;
    }

    /// <summary>Música = fontes em loop; efeitos = o resto. O volume original de cada fonte é guardado.</summary>
    public static void AplicarAudio() {
        AudioListener.volume = VolumeGeral;
        foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            int id = src.GetInstanceID();
            if (!volumesBase.TryGetValue(id, out float baseVol)) { baseVol = src.volume; volumesBase[id] = baseVol; }
            src.volume = baseVol * (EhMusica(src) ? VolumeMusica : VolumeEfeitos);
        }
    }

    static bool EhMusica(AudioSource src) => src.loop || (src.clip != null && src.clip.name.ToLower().Contains("music"));

    /// <summary>Para efeitos criados em tempo de execução (ex.: moedas): ajusta antes de tocar.</summary>
    public static void PrepararEfeito(AudioSource src) {
        if (src == null) return;
        int id = src.GetInstanceID();
        if (!volumesBase.TryGetValue(id, out float baseVol)) { baseVol = src.volume; volumesBase[id] = baseVol; }
        src.volume = baseVol * VolumeEfeitos;
    }
}
