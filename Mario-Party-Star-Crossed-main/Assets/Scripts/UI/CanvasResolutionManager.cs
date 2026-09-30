using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia a escala do Canvas para manter proporções independentes da resolução da tela.
/// Ajusta automaticamente quando a resolução muda em tempo real.
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public class CanvasResolutionManager : MonoBehaviour
{
    [Header("Configurações de Resolução")]
    [Tooltip("Resolução de referência para o design da UI (largura)")]
    public float referenceResolutionX = 1920f;
    
    [Tooltip("Resolução de referência para o design da UI (altura)")]
    public float referenceResolutionY = 1080f;
    
    [Tooltip("Se verdadeiro, mantém a proporção mesmo quando a janela é redimensionada")]
    public bool matchWidthOrHeight = true;
    
    [Tooltip("Valor entre 0 (largura) e 1 (altura). 0.5 = balanceado")]
    [Range(0f, 1f)]
    public float matchWidthOrHeightValue = 0.5f;
    
    [Header("Ajuste em Tempo Real")]
    [Tooltip("Se verdadeiro, monitora mudanças de resolução em tempo real")]
    public bool monitorResolutionChanges = true;
    
    [Tooltip("Intervalo em segundos para verificar mudanças de resolução (0 = a cada frame)")]
    public float checkInterval = 0.1f;

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private Vector2 lastScreenSize;
    private float lastCheckTime;
    private bool isInitialized = false;
    private bool isUpdating = false;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CanvasResolutionManager: Canvas component não encontrado!");
            enabled = false;
            return;
        }

        canvasScaler = GetComponent<CanvasScaler>();
        
        // Configurar Canvas Scaler se não estiver configurado
        SetupCanvasScaler();
        
        // Inicializar tamanho da tela
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        // Garante que o gerenciador de entrada e configurações mobile esteja ativo mesmo dando Play direto nesta cena
        MobileInputManager.EnsureExists();
    }

    void Start()
    {
        // Aplicar configuração inicial apenas uma vez
        if (!isInitialized)
        {
            isInitialized = true;
            UpdateCanvasScale();
        }
    }

    void Update()
    {
        if (monitorResolutionChanges)
        {
            // Verificar mudanças de resolução
            if (Time.time - lastCheckTime >= checkInterval)
            {
                Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
                
                if (currentScreenSize != lastScreenSize)
                {
                    UpdateCanvasScale();
                    lastScreenSize = currentScreenSize;
                }
                
                lastCheckTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Configura o Canvas Scaler com as configurações apropriadas
    /// </summary>
    private void SetupCanvasScaler()
    {
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        // Verificar se já está configurado corretamente para evitar sobrescrever configurações existentes
        if (canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        if (canvasScaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
        {
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        // Atualizar valores
        canvasScaler.referenceResolution = new Vector2(referenceResolutionX, referenceResolutionY);
        canvasScaler.matchWidthOrHeight = matchWidthOrHeightValue;
    }

    /// <summary>
    /// Atualiza a escala do Canvas baseado na resolução atual
    /// </summary>
    public void UpdateCanvasScale()
    {
        // Proteção contra loops infinitos
        if (isUpdating || canvasScaler == null || !isInitialized) return;

        try
        {
            isUpdating = true;

            // Atualizar resolução de referência se necessário
            canvasScaler.referenceResolution = new Vector2(referenceResolutionX, referenceResolutionY);
            canvasScaler.matchWidthOrHeight = matchWidthOrHeightValue;
            
            // Forçar atualização do Canvas Scaler apenas se necessário
            if (!canvasScaler.enabled)
            {
                canvasScaler.enabled = true;
            }
        }
        finally
        {
            isUpdating = false;
        }
    }

    /// <summary>
    /// Define a resolução de referência
    /// </summary>
    public void SetReferenceResolution(float width, float height)
    {
        referenceResolutionX = width;
        referenceResolutionY = height;
        UpdateCanvasScale();
    }

    /// <summary>
    /// Define o valor de match width or height (0 = largura, 1 = altura, 0.5 = balanceado)
    /// </summary>
    public void SetMatchWidthOrHeight(float value)
    {
        matchWidthOrHeightValue = Mathf.Clamp01(value);
        UpdateCanvasScale();
    }

    /// <summary>
    /// Obtém a escala atual do Canvas
    /// </summary>
    public float GetCurrentScale()
    {
        if (canvas == null || canvasScaler == null) return 1f;
        
        float scaleX = Screen.width / referenceResolutionX;
        float scaleY = Screen.height / referenceResolutionY;
        
        float scale = Mathf.Lerp(scaleX, scaleY, matchWidthOrHeightValue);
        return scale;
    }

    void OnValidate()
    {
        // Atualizar quando valores são alterados no Inspector
        // Mas apenas se já estiver inicializado e não estiver atualizando
        if (Application.isPlaying && canvasScaler != null && isInitialized && !isUpdating)
        {
            // Usar corrotina para evitar problemas durante OnValidate
            StartCoroutine(DelayedUpdate());
        }
    }

    private IEnumerator DelayedUpdate()
    {
        yield return null; // Esperar um frame
        if (!isUpdating)
        {
            UpdateCanvasScale();
        }
    }

    void OnRectTransformDimensionsChange()
    {
        // Removido para evitar loops infinitos
        // O CanvasScaler já lida com mudanças de dimensões automaticamente
    }
}

