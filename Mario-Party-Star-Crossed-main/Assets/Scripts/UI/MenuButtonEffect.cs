using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Efeito de hover estilo "Overcooked" para botões do menu.
/// Adicione este script a cada botão do menu.
/// 
/// Setup no Unity:
/// 1. Crie um botão com Image (fundo pill/arredondado)
/// 2. Adicione um filho com Image para a sombra (deslocado para baixo)
/// 3. Arraste as referências no Inspector
/// </summary>
public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {

    [Header("Cores")]
    [Tooltip("Cor normal do fundo do botão")]
    public Color corNormal = new Color(0.94f, 0.99f, 0.96f, 1f);       // #f0fdf4
    [Tooltip("Cor do botão ao passar o mouse")]
    public Color corHover = new Color(0.98f, 0.75f, 0.14f, 1f);        // #fbbf24
    [Tooltip("Cor normal do texto")]
    public Color corTextoNormal = new Color(0.09f, 0.40f, 0.20f, 1f);  // #166534
    [Tooltip("Cor do texto ao passar o mouse")]
    public Color corTextoHover = new Color(0.27f, 0.10f, 0.01f, 1f);   // #451a03

    [Header("Animação")]
    [Tooltip("Escala ao passar o mouse (1.1 = 10% maior)")]
    public float escalaHover = 1.1f;
    [Tooltip("Quanto o botão sobe ao hover (pixels)")]
    public float subirHover = 5f;
    [Tooltip("Velocidade da animação")]
    public float velocidade = 8f;

    [Header("Referências (Opcional)")]
    [Tooltip("Imagem de sombra (filho do botão, deslocado para baixo)")]
    public Image imagemSombra;

    // Referências internas
    private Image imagemFundo;
    private Text texto;
    private RectTransform rect;
    private Vector3 posicaoOriginal;
    private Vector3 escalaOriginal;
    private bool isHovering = false;

    void Awake() {
        imagemFundo = GetComponent<Image>();
        texto = GetComponentInChildren<Text>();
        rect = GetComponent<RectTransform>();
        escalaOriginal = rect.localScale;
    }

    void Start() {
        posicaoOriginal = rect.anchoredPosition;
        // Aplicar cores iniciais
        if (imagemFundo != null) imagemFundo.color = corNormal;
        if (texto != null) texto.color = corTextoNormal;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        // Em dispositivos touch nativos, ignorar simulação de hover para evitar botões presos no estado destacado
        if (MobileInputManager.IsTouchSupported && Input.touchCount > 0) return;

        isHovering = true;
        StopAllCoroutines();
        StartCoroutine(AnimarPara(escalaOriginal * escalaHover, posicaoOriginal + Vector3.up * subirHover, corHover, corTextoHover));
    }

    public void OnPointerExit(PointerEventData eventData) {
        isHovering = false;
        StopAllCoroutines();
        StartCoroutine(AnimarPara(escalaOriginal, posicaoOriginal, corNormal, corTextoNormal));
    }

    public void OnPointerDown(PointerEventData eventData) {
        // Feedback tátil ao pressionar no mobile
        HapticFeedback.Vibrate();

        // Efeito de "pressionar" — botão desce e encolhe
        StopAllCoroutines();
        StartCoroutine(AnimarPara(escalaOriginal * 0.95f, posicaoOriginal + Vector3.down * 3f, corHover, corTextoHover));
    }

    public void OnPointerUp(PointerEventData eventData) {
        // Se for dispositivo de toque, sempre restaura a cor e posição normal para não ficar preso em hover
        if (MobileInputManager.IsTouchSupported) {
            isHovering = false;
            StopAllCoroutines();
            StartCoroutine(AnimarPara(escalaOriginal, posicaoOriginal, corNormal, corTextoNormal));
            return;
        }

        if (isHovering) {
            StopAllCoroutines();
            StartCoroutine(AnimarPara(escalaOriginal * escalaHover, posicaoOriginal + Vector3.up * subirHover, corHover, corTextoHover));
        } else {
            StopAllCoroutines();
            StartCoroutine(AnimarPara(escalaOriginal, posicaoOriginal, corNormal, corTextoNormal));
        }
    }

    private IEnumerator AnimarPara(Vector3 escalaAlvo, Vector3 posicaoAlvo, Color corFundoAlvo, Color corTextoAlvo) {
        while (true) {
            float t = velocidade * Time.unscaledDeltaTime;

            rect.localScale = Vector3.Lerp(rect.localScale, escalaAlvo, t);
            rect.anchoredPosition = Vector3.Lerp(rect.anchoredPosition, posicaoAlvo, t);

            if (imagemFundo != null)
                imagemFundo.color = Color.Lerp(imagemFundo.color, corFundoAlvo, t);
            if (texto != null)
                texto.color = Color.Lerp(texto.color, corTextoAlvo, t);

            // Parar quando estiver perto o suficiente
            if (Vector3.Distance(rect.localScale, escalaAlvo) < 0.001f) {
                rect.localScale = escalaAlvo;
                rect.anchoredPosition = posicaoAlvo;
                if (imagemFundo != null) imagemFundo.color = corFundoAlvo;
                if (texto != null) texto.color = corTextoAlvo;
                yield break;
            }

            yield return null;
        }
    }
}
