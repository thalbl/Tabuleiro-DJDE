using System;
using UnityEngine;

/// <summary>
/// Gerenciador centralizado de configurações e detecção de entrada para Mobile (Touch) e Desktop.
/// Garante que o jogo rode em 60 FPS, orientação Paisagem fixa e que toques na tela
/// funcionem de forma transparente ao lado do mouse e teclado.
/// </summary>
public class MobileInputManager : MonoBehaviour {

    public static MobileInputManager Instance { get; private set; }

    [Header("Configurações Mobile")]
    [Tooltip("Define o FPS alvo no dispositivo (padrão: 60)")]
    public int targetFps = 60;

    [Tooltip("Impedir que a tela do celular apague durante a partida")]
    public bool keepScreenAwake = true;

    // Detecção de Swipe
    private static Vector2 touchStartPos;
    private static float touchStartTime;
    private const float MinSwipeDistance = 50f;
    private const float MaxSwipeTime = 0.5f;

    public static void EnsureExists() {
        if (Instance == null && FindObjectOfType<MobileInputManager>() == null) {
            GameObject go = new GameObject("[MobileInputManager]");
            go.AddComponent<MobileInputManager>();
        }
    }

    void Awake() {
        if (Instance == null) {
            Instance = this;
            if (transform.parent == null) {
                DontDestroyOnLoad(gameObject);
            }
            ApplyMobileSettings();
        } else if (Instance != this) {
            if (gameObject.name == "[MobileInputManager]") {
                Destroy(gameObject);
            } else {
                Destroy(this);
            }
        }
    }

    /// <summary>
    /// Configura orientações de tela, taxa de quadros e sleep timeout.
    /// </summary>
    public void ApplyMobileSettings() {
        // Fixar orientação para Paisagem
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        // Impedir tela de dormir
        if (keepScreenAwake) {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // Definir FPS suave para mobile
        Application.targetFrameRate = targetFps;
    }

    void Update() {
        TrackSwipe();
    }

    private void TrackSwipe() {
        if (Input.touchCount > 0) {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) {
                touchStartPos = t.position;
                touchStartTime = Time.unscaledTime;
            }
        } else if (Input.GetMouseButtonDown(0)) {
            touchStartPos = Input.mousePosition;
            touchStartTime = Time.unscaledTime;
        }
    }

    // =================== MÉTODOS ESTÁTICOS DE TOUCH / CLICK ===================

    /// <summary>
    /// Retorna verdadeiro se o jogador tocou na tela ou clicou com o botão esquerdo.
    /// </summary>
    public static bool IsTouchOrClickDown() {
        if (Input.GetMouseButtonDown(0)) {
            return true;
        }
        if (Input.touchCount > 0) {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }
        return false;
    }

    /// <summary>
    /// Retorna verdadeiro no frame em que o toque ou clique foi solto.
    /// </summary>
    public static bool IsTouchOrClickUp() {
        if (Input.GetMouseButtonUp(0)) {
            return true;
        }
        if (Input.touchCount > 0) {
            TouchPhase p = Input.GetTouch(0).phase;
            return p == TouchPhase.Ended || p == TouchPhase.Canceled;
        }
        return false;
    }

    /// <summary>
    /// Retorna verdadeiro se a tela está sendo pressionada continuamente.
    /// </summary>
    public static bool IsTouchOrClickHeld() {
        if (Input.GetMouseButton(0)) {
            return true;
        }
        if (Input.touchCount > 0) {
            TouchPhase p = Input.GetTouch(0).phase;
            return p == TouchPhase.Moved || p == TouchPhase.Stationary;
        }
        return false;
    }

    /// <summary>
    /// Retorna a posição atual do ponteiro (cursor ou primeiro dedo).
    /// </summary>
    public static Vector2 GetPointerPosition() {
        if (Input.touchCount > 0) {
            return Input.GetTouch(0).position;
        }
        return Input.mousePosition;
    }

    /// <summary>
    /// Detecta se um swipe horizontal ocorreu no frame em que o dedo foi solto.
    /// Retorna 1 para swipe direita, -1 para swipe esquerda, 0 para nenhum swipe.
    /// </summary>
    public static int GetHorizontalSwipe() {
        if (IsTouchOrClickUp()) {
            Vector2 endPos = GetPointerPosition();
            Vector2 delta = endPos - touchStartPos;
            float duration = Time.unscaledTime - touchStartTime;

            if (duration <= MaxSwipeTime && Mathf.Abs(delta.x) >= MinSwipeDistance && Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) {
                return delta.x > 0 ? 1 : -1;
            }
        }
        return 0;
    }

    /// <summary>
    /// Indica se a plataforma corrente possui tela de toque ou é ambiente mobile.
    /// </summary>
    public static bool IsTouchPlatform {
        get {
            return Application.isMobilePlatform || Input.touchSupported;
        }
    }

    public static bool IsTouchSupported {
        get {
            return IsTouchPlatform;
        }
    }
}
