using UnityEngine;

/// <summary>
/// Helper para fornecer feedback tátil (vibração) em dispositivos móveis.
/// Suporta ativação/desativação através de PlayerPrefs.
/// </summary>
public static class HapticFeedback {

    private const string HapticsPrefKey = "EnableHaptics";

    public static bool IsEnabled {
        get {
            return PlayerPrefs.GetInt(HapticsPrefKey, 1) == 1;
        }
        set {
            PlayerPrefs.SetInt(HapticsPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Dispara uma vibração padrão no dispositivo móvel.
    /// Seguro para chamar em qualquer plataforma (ignorado silenciosamente no Desktop).
    /// </summary>
    public static void Vibrate() {
        if (!IsEnabled) return;

        if (Application.isMobilePlatform) {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }

    /// <summary>
    /// Vibração específica para rolagem de dados ou cliques de botão.
    /// </summary>
    public static void VibrateDiceRoll() {
        Vibrate();
    }

    /// <summary>
    /// Vibração ao colidir ou sofrer penalidade em casas negativas (ex: Bowser).
    /// </summary>
    public static void VibrateImpact() {
        Vibrate();
    }
}
