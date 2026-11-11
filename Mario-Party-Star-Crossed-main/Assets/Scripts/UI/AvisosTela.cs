using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feedback visual rápido, no padrão do TemaUI:
///  - Faixa(): aviso que desce do topo da tela e some sozinho (ex.: Bolsa subiu/caiu na virada da rodada).
///  - Flutuar(): texto que sobe e desaparece a partir de um elemento de UI (ex.: "+40" sobre as Moedas do HUD).
/// Tudo em tempo não escalado: funciona com o jogo pausado e não interfere na lógica.
/// </summary>
public class AvisosTela : MonoBehaviour {
    static AvisosTela instancia;
    RectTransform raiz;

    static AvisosTela Instancia {
        get {
            if (instancia == null) {
                var go = new GameObject("[AvisosTela]", typeof(RectTransform));
                instancia = go.AddComponent<AvisosTela>();
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 800; // acima do HUD, abaixo de pausa/Como Jogar
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                instancia.raiz = (RectTransform)go.transform;
            }
            return instancia;
        }
    }

    void OnDestroy() { if (instancia == this) instancia = null; }

    // ------------------------------------------------------------------ faixa no topo
    /// <summary>Mostra uma faixa no topo da tela por alguns segundos.</summary>
    public static void Faixa(string titulo, string texto, Color cor, string glifo = null, float duracao = 3f) {
        if (!Application.isPlaying) return;
        Instancia.StartCoroutine(Instancia.MostrarFaixa(titulo, texto, cor, glifo, duracao));
    }

    IEnumerator MostrarFaixa(string titulo, string texto, Color cor, string glifo, float duracao) {
        var painel = TemaUI.Painel_(raiz, "Faixa", TemaUI.Janela);
        painel.raycastTarget = false;
        var rt = painel.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(820, 120);
        var borda = painel.gameObject.AddComponent<Outline>();
        borda.effectColor = cor;
        borda.effectDistance = new Vector2(3, -3);
        var grupo = painel.gameObject.AddComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;

        var chip = TemaUI.Painel_(rt, "Icone", cor);
        chip.sprite = TemaUI.Circulo;
        chip.type = Image.Type.Simple;
        chip.raycastTarget = false;
        var crt = chip.rectTransform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0, 0.5f);
        crt.sizeDelta = new Vector2(80, 80);
        crt.anchoredPosition = new Vector2(24, 0);
        var g = TemaUI.Texto(crt, glifo ?? "!", 48, TemaUI.Janela, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(g.rectTransform);

        var t1 = TemaUI.Texto(rt, titulo, 38, cor, FontStyle.Bold, TextAnchor.LowerLeft);
        t1.rectTransform.anchorMin = new Vector2(0, 0.5f); t1.rectTransform.anchorMax = new Vector2(1, 1);
        t1.rectTransform.offsetMin = new Vector2(124, 0); t1.rectTransform.offsetMax = new Vector2(-24, -10);
        var t2 = TemaUI.Texto(rt, texto, TemaUI.TamPequeno, TemaUI.TextoClaro, FontStyle.Normal, TextAnchor.UpperLeft);
        t2.rectTransform.anchorMin = new Vector2(0, 0); t2.rectTransform.anchorMax = new Vector2(1, 0.5f);
        t2.rectTransform.offsetMin = new Vector2(124, 10); t2.rectTransform.offsetMax = new Vector2(-24, -2);
        t2.resizeTextForBestFit = true; t2.resizeTextMinSize = 18; t2.resizeTextMaxSize = TemaUI.TamPequeno;

        // Entra (desce do topo), espera, sai
        float topo = 150f, final = -268f; // abaixo dos cartões dos jogadores (que ocupam ~250 px do topo)
        yield return Animar(0.3f, p => {
            float e = 1f - Mathf.Pow(1f - p, 3f);
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(topo, final, e));
            grupo.alpha = p;
        });
        float t = 0f;
        while (t < duracao) { t += Passo; yield return null; }
        yield return Animar(0.25f, p => {
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(final, final + 40f, p));
            grupo.alpha = 1f - p;
        });
        Destroy(painel.gameObject);
    }

    // ------------------------------------------------------------------ texto flutuante
    /// <summary>Texto que nasce sobre <paramref name="alvo"/>, sobe e some. Usa o canvas do próprio alvo.</summary>
    public static void Flutuar(RectTransform alvo, string texto, Color cor, int tamanho = 36, Vector2? deslocamento = null) {
        if (!Application.isPlaying || alvo == null) return;
        var canvas = alvo.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var pai = (RectTransform)canvas.rootCanvas.transform;

        var t = TemaUI.Texto(pai, texto, tamanho, cor, FontStyle.Bold, TextAnchor.MiddleCenter);
        t.name = "Flutuante";
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        TemaUI.ContornoTexto(t, new Color(0.02f, 0.08f, 0.05f, 0.9f), 2.5f);
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(300, 60);
        rt.position = alvo.position;
        rt.anchoredPosition += deslocamento ?? new Vector2(0, -10);
        Instancia.StartCoroutine(Instancia.AnimarFlutuante(t));
    }

    IEnumerator AnimarFlutuante(Text t) {
        var rt = t.rectTransform;
        Vector2 inicio = rt.anchoredPosition;
        Color cor = t.color;
        yield return Animar(1.2f, p => {
            if (t == null) return;
            float sobe = 1f - Mathf.Pow(1f - p, 2f);
            rt.anchoredPosition = inicio + new Vector2(0, 48f * sobe);
            float escala = p < 0.15f ? Mathf.Lerp(0.6f, 1.15f, p / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((p - 0.15f) / 0.2f));
            rt.localScale = Vector3.one * escala;
            t.color = new Color(cor.r, cor.g, cor.b, p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f);
        });
        if (t != null) Destroy(t.gameObject);
    }

    /// <summary>Pulo rápido de escala (ex.: número do HUD que acabou de mudar).</summary>
    public static void Pulo(Transform alvo, float intensidade = 0.25f) {
        if (!Application.isPlaying || alvo == null) return;
        Instancia.StartCoroutine(Instancia.AnimarPulo(alvo, intensidade));
    }

    // Escala original de cada alvo: dois pulos seguidos não "inflam" o elemento
    readonly System.Collections.Generic.Dictionary<Transform, Vector3> escalasBase = new System.Collections.Generic.Dictionary<Transform, Vector3>();

    IEnumerator AnimarPulo(Transform alvo, float intensidade) {
        if (!escalasBase.TryGetValue(alvo, out Vector3 baseEscala)) { baseEscala = alvo.localScale; escalasBase[alvo] = baseEscala; }
        yield return Animar(0.35f, p => {
            if (alvo != null) alvo.localScale = baseEscala * (1f + intensidade * Mathf.Sin(p * Mathf.PI));
        });
        if (alvo != null) alvo.localScale = baseEscala;
    }

    /// <summary>Tempo do quadro limitado: uma pausa longa (troca de janela, pausa do Editor) não pula a animação.</summary>
    static float Passo => Mathf.Min(Time.unscaledDeltaTime, 0.1f);

    static IEnumerator Animar(float duracao, System.Action<float> passo) {
        float t = 0f;
        while (t < duracao) {
            t += Passo;
            passo(Mathf.Clamp01(t / duracao));
            yield return null;
        }
        passo(1f);
    }
}
