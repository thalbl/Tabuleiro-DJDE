using UnityEngine;

/// <summary>
/// Ajusta o RectTransform para respeitar a Safe Area de dispositivos móveis com entalhes (notches),
/// câmeras perfuradas (punch-holes) ou barras de gestos no Android e iOS.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour {

    [Header("Configurações")]
    [Tooltip("Ajustar Safe Area na horizontal (esquerda e direita)")]
    public bool conformX = true;

    [Tooltip("Ajustar Safe Area na vertical (topo e base)")]
    public bool conformY = true;

    private RectTransform panel;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake() {
        panel = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update() {
        // Se a orientação ou resolução mudar em tempo de execução, reajustar
        if (lastSafeArea != Screen.safeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height ||
            lastOrientation != Screen.orientation) {
            ApplySafeArea();
        }
    }

    public void ApplySafeArea() {
        if (panel == null) panel = GetComponent<RectTransform>();
        if (panel == null) return;

        Rect safeArea = Screen.safeArea;

        // Se por algum motivo o safeArea for 0 ou tela inteira sem restrições
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Converte coordenadas absolutas de pixel para coordenadas proporcionais de âncora (0.0 a 1.0)
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        if (conformX) {
            anchorMin.x /= Screen.width;
            anchorMax.x /= Screen.width;
        } else {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }

        if (conformY) {
            anchorMin.y /= Screen.height;
            anchorMax.y /= Screen.height;
        } else {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
        }

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
    }
}
