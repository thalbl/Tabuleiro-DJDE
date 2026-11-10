using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Padrão visual único do jogo (baseado no menu principal): painéis verde-escuros, cartões claros,
/// destaques dourados e a fonte Fredoka. Toda UI nova ou restilizada deve usar estas cores e fábricas
/// em vez de definir cores/fontes soltas.
/// </summary>
public static class TemaUI {
    // ------------------------------------------------------------------ paleta
    public static readonly Color Painel = Hex("#0F2A1F", 0.94f);       // fundo de caixas e barras
    public static readonly Color PainelSuave = Hex("#173B2C", 0.92f);  // painéis internos / linhas
    public static readonly Color Cartao = Hex("#F0FDF4");              // botões e cartões claros
    public static readonly Color Ouro = Hex("#FBBF24");                // destaque, foco, títulos
    public static readonly Color OuroEscuro = Hex("#92400E");          // texto sobre ouro
    public static readonly Color Verde = Hex("#22C55E");               // ação positiva
    public static readonly Color VerdeEscuro = Hex("#14532D");         // texto sobre cartão claro
    public static readonly Color Vermelho = Hex("#EF4444");            // perigo / sair
    public static readonly Color Azul = Hex("#3B82F6");                // informação
    public static readonly Color TextoClaro = Hex("#F8FAFC");
    public static readonly Color TextoSuave = Hex("#A7C4B5");
    public static readonly Color SombraVerde = Hex("#064E3B");
    public static readonly Color Veu = new Color(0.02f, 0.08f, 0.05f, 0.72f); // fundo atrás de janelas
    public static readonly Color Janela = Hex("#0F2A1F");               // janelas modais: opaco (contorno não "vaza")

    // ------------------------------------------------------------------ tipografia
    public const int TamTitulo = 56;
    public const int TamSubtitulo = 34;
    public const int TamCorpo = 30;
    public const int TamBotao = 30;
    public const int TamPequeno = 24;

    static Font fonte;
    public static Font Fonte {
        get {
            if (fonte == null) fonte = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
            if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fonte;
        }
    }

    public static Color Hex(string hex, float alfa = 1f) {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alfa;
        return c;
    }

    // ------------------------------------------------------------------ sprites gerados
    static Sprite arredondado, circulo;

    /// <summary>Retângulo arredondado 9-slice (raio 24 px na textura base).</summary>
    public static Sprite Arredondado {
        get {
            if (arredondado == null) arredondado = GerarArredondado(96, 24);
            return arredondado;
        }
    }

    public static Sprite Circulo {
        get {
            if (circulo == null) circulo = GerarArredondado(128, 64, false);
            return circulo;
        }
    }

    static Sprite GerarArredondado(int tam, int raio, bool fatiado = true) {
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color[tam * tam];
        for (int y = 0; y < tam; y++) {
            for (int x = 0; x < tam; x++) {
                float cx = Mathf.Clamp(x + 0.5f, raio, tam - raio), cy = Mathf.Clamp(y + 0.5f, raio, tam - raio);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - raio;
                px[y * tam + x] = new Color(1, 1, 1, Mathf.Clamp01(0.5f - d));
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        Vector4 borda = fatiado ? new Vector4(raio, raio, raio, raio) : Vector4.zero;
        var s = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borda);
        s.name = fatiado ? "TemaArredondado" : "TemaCirculo";
        return s;
    }

    // ------------------------------------------------------------------ estilização de elementos existentes
    public static void EstilizarImagem(UnityEngine.UI.Image img, Color cor, bool arredondar = true) {
        if (img == null) return;
        if (arredondar) {
            img.sprite = Arredondado;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }
        img.color = cor;
    }

    public static void EstilizarTexto(UnityEngine.UI.Text t, int tamanho, Color cor, FontStyle estilo = FontStyle.Normal) {
        if (t == null) return;
        t.font = Fonte;
        t.fontSize = tamanho;
        t.color = cor;
        t.fontStyle = estilo;
    }

    /// <summary>Sombra suave sob o texto: melhora a leitura sobre o cenário 3D.</summary>
    public static void SombraTexto(UnityEngine.UI.Text t, float distancia = 2f, float alfa = 0.6f) {
        if (t == null) return;
        var s = SombraPura(t.gameObject);
        s.effectColor = new Color(0, 0, 0, alfa);
        s.effectDistance = new Vector2(distancia, -distancia);
    }

    /// <summary>Shadow que não seja Outline (Outline herda de Shadow); cria se não houver. Evita empilhar componentes ao restilizar.</summary>
    static UnityEngine.UI.Shadow SombraPura(GameObject go) {
        foreach (var sh in go.GetComponents<UnityEngine.UI.Shadow>()) if (!(sh is UnityEngine.UI.Outline)) return sh;
        return go.AddComponent<UnityEngine.UI.Shadow>();
    }

    /// <summary>Contorno grosso (títulos, números grandes).</summary>
    public static void ContornoTexto(UnityEngine.UI.Text t, Color cor, float espessura = 3f) {
        if (t == null) return;
        var o = t.GetComponent<UnityEngine.UI.Outline>() ?? t.gameObject.AddComponent<UnityEngine.UI.Outline>();
        o.effectColor = cor;
        o.effectDistance = new Vector2(espessura, -espessura);
    }

    public enum EstiloBotao { Claro, Destaque, Perigo, Escuro }

    public static void EstilizarBotao(UnityEngine.UI.Button b, EstiloBotao estilo) {
        if (b == null) return;
        var img = b.GetComponent<UnityEngine.UI.Image>();
        Color fundo, texto;
        switch (estilo) {
            case EstiloBotao.Destaque: fundo = Ouro; texto = OuroEscuro; break;
            case EstiloBotao.Perigo: fundo = Vermelho; texto = TextoClaro; break;
            case EstiloBotao.Escuro: fundo = PainelSuave; texto = TextoClaro; break;
            default: fundo = Cartao; texto = VerdeEscuro; break;
        }
        EstilizarImagem(img, Color.white);
        b.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = fundo;
        cb.highlightedColor = estilo == EstiloBotao.Claro ? Ouro : Color.Lerp(fundo, Color.white, 0.18f);
        cb.selectedColor = cb.normalColor;
        cb.pressedColor = Color.Lerp(fundo, Color.black, 0.18f);
        cb.disabledColor = new Color(fundo.r, fundo.g, fundo.b, 0.45f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        var sombra = SombraPura(b.gameObject);
        sombra.effectColor = estilo == EstiloBotao.Perigo ? Hex("#7F1D1D") : SombraVerde;
        sombra.effectDistance = new Vector2(0, -5f);
        var contorno = b.GetComponent<UnityEngine.UI.Outline>();
        if (contorno != null) contorno.enabled = false;

        foreach (var t in b.GetComponentsInChildren<UnityEngine.UI.Text>(true)) {
            EstilizarTexto(t, t.fontSize > 0 ? Mathf.Max(t.fontSize, TamPequeno) : TamBotao, texto, FontStyle.Bold);
            t.alignment = TextAnchor.MiddleCenter;
            foreach (var ts in t.GetComponents<UnityEngine.UI.Shadow>()) ts.enabled = false;
        }
    }

    // ------------------------------------------------------------------ fábricas
    public static RectTransform Criar(string nome, Transform pai) {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    public static void Esticar(RectTransform rt, float margem = 0f) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, margem);
        rt.offsetMax = new Vector2(-margem, -margem);
    }

    public static UnityEngine.UI.Image Painel_(Transform pai, string nome, Color cor) {
        var rt = Criar(nome, pai);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        EstilizarImagem(img, cor);
        return img;
    }

    public static UnityEngine.UI.Text Texto(Transform pai, string conteudo, int tamanho, Color cor,
                                            FontStyle estilo = FontStyle.Normal, TextAnchor alinhamento = TextAnchor.MiddleLeft) {
        var rt = Criar("Texto", pai);
        var t = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
        EstilizarTexto(t, tamanho, cor, estilo);
        t.text = conteudo;
        t.alignment = alinhamento;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.supportRichText = true;
        return t;
    }

    public static UnityEngine.UI.Button Botao(Transform pai, string rotulo, EstiloBotao estilo, UnityAction aoClicar, int tamanhoFonte = TamBotao) {
        var rt = Criar("Botao_" + rotulo, pai);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = img;
        var t = Texto(rt, rotulo, tamanhoFonte, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
        Esticar(t.rectTransform, 8f);
        EstilizarBotao(b, estilo);
        if (aoClicar != null) b.onClick.AddListener(aoClicar);
        return b;
    }

    /// <summary>Slider 0..1 com trilho escuro, preenchimento dourado e botão claro.</summary>
    public static UnityEngine.UI.Slider Slider(Transform pai, float valor, Action<float> aoMudar) {
        var rt = Criar("Slider", pai);
        Esticar(rt);
        var s = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();

        var trilho = Painel_(rt, "Trilho", Hex("#0B1F17"));
        var trt = trilho.rectTransform;
        trt.anchorMin = new Vector2(0, 0.5f); trt.anchorMax = new Vector2(1, 0.5f);
        trt.sizeDelta = new Vector2(0, 14f);

        var areaPreenchimento = Criar("AreaPreenchimento", rt);
        areaPreenchimento.anchorMin = new Vector2(0, 0.5f); areaPreenchimento.anchorMax = new Vector2(1, 0.5f);
        areaPreenchimento.sizeDelta = new Vector2(0, 14f);
        var preenchimento = Painel_(areaPreenchimento, "Preenchimento", Ouro);
        preenchimento.rectTransform.sizeDelta = Vector2.zero;

        // Área da alça com altura zero: o Slider estica a alça na vertical até a área, então a altura vem só do sizeDelta
        var areaAlca = Criar("AreaAlca", rt);
        areaAlca.anchorMin = new Vector2(0, 0.5f); areaAlca.anchorMax = new Vector2(1, 0.5f);
        areaAlca.sizeDelta = new Vector2(-32, 0);
        var alca = Criar("Alca", areaAlca);
        var alcaImg = alca.gameObject.AddComponent<UnityEngine.UI.Image>();
        alcaImg.sprite = Circulo;
        alcaImg.color = Cartao;
        alca.sizeDelta = new Vector2(36, 36);
        var sombra = alca.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        sombra.effectColor = new Color(0, 0, 0, 0.5f);
        sombra.effectDistance = new Vector2(0, -3);

        s.fillRect = preenchimento.rectTransform;
        s.handleRect = alca;
        s.targetGraphic = alcaImg;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = valor;
        if (aoMudar != null) s.onValueChanged.AddListener(v => aoMudar(v));
        return s;
    }

    /// <summary>Interruptor liga/desliga (pílula) feito com Toggle.</summary>
    public static UnityEngine.UI.Toggle Interruptor(Transform pai, bool valor, Action<bool> aoMudar) {
        var rt = Criar("Interruptor", pai);
        var fundo = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        fundo.sprite = Arredondado; fundo.type = UnityEngine.UI.Image.Type.Sliced;
        var tg = rt.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        tg.transition = UnityEngine.UI.Selectable.Transition.None;

        var bolinha = Criar("Bolinha", rt);
        var bImg = bolinha.gameObject.AddComponent<UnityEngine.UI.Image>();
        bImg.sprite = Circulo;
        bImg.color = Cartao;
        bolinha.sizeDelta = new Vector2(34, 34);

        void Atualizar(bool ligado) {
            fundo.color = ligado ? Verde : Hex("#3F5249");
            bolinha.anchorMin = bolinha.anchorMax = new Vector2(ligado ? 1f : 0f, 0.5f);
            bolinha.pivot = new Vector2(ligado ? 1f : 0f, 0.5f);
            bolinha.anchoredPosition = new Vector2(ligado ? -5f : 5f, 0);
        }
        tg.isOn = valor;
        Atualizar(valor);
        tg.onValueChanged.AddListener(v => { Atualizar(v); aoMudar?.Invoke(v); });
        return tg;
    }

    /// <summary>Grupo de opções exclusivas (ex.: Baixa | Média | Alta).</summary>
    public static void Segmentado(Transform pai, string[] rotulos, int selecionado, Action<int> aoMudar) {
        var rt = Criar("Segmentado", pai);
        Esticar(rt);
        var layout = rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 8; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        var botoes = new UnityEngine.UI.Button[rotulos.Length];
        void Marcar(int i) {
            for (int k = 0; k < botoes.Length; k++) EstilizarBotao(botoes[k], k == i ? EstiloBotao.Destaque : EstiloBotao.Escuro);
        }
        for (int i = 0; i < rotulos.Length; i++) {
            int indice = i;
            botoes[i] = Botao(rt, rotulos[i], EstiloBotao.Escuro, () => { Marcar(indice); aoMudar?.Invoke(indice); }, TamPequeno);
        }
        Marcar(selecionado);
    }
}
