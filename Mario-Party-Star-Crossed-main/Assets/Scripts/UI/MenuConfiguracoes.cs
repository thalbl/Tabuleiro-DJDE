using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu de configurações. No tabuleiro funciona como pausa (botão no canto superior direito ou Esc/Voltar);
/// no menu principal abre pelo botão "Configurações". Construído em código com o TemaUI.
/// Partida local: pausa o jogo (timeScale 0). Online: a partida continua (não dá para pausar os outros).
/// </summary>
public class MenuConfiguracoes : MonoBehaviour {
    public enum Contexto { MenuPrincipal, Partida }

    public static MenuConfiguracoes Instance { get; private set; }
    /// <summary>Verdadeiro com a janela aberta: o jogo ignora toques/teclas de avançar e rolar.</summary>
    public static bool Aberto { get; private set; }

    Contexto contexto;
    Canvas canvas;
    GameObject janela, confirmacao, botaoPausa;
    float timeScaleAnterior = 1f;

    public static MenuConfiguracoes Garantir(Contexto contexto) {
        if (Instance != null) return Instance;
        var go = new GameObject("[MenuConfiguracoes]");
        var menu = go.AddComponent<MenuConfiguracoes>();
        menu.contexto = contexto;
        menu.Construir();
        return menu;
    }

    void Awake() { Instance = this; }

    void OnDestroy() {
        if (Instance == this) {
            Instance = null;
            if (Aberto) { Aberto = false; Time.timeScale = 1f; }
        }
    }

    bool PodePausar => contexto == Contexto.Partida && !GameMatchConfig.IsOnlineMatch;

    void Update() {
        // Esc no PC / botão Voltar no Android
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (confirmacao != null && confirmacao.activeSelf) confirmacao.SetActive(false);
            else if (Aberto) Fechar();
            else if (contexto == Contexto.Partida) Abrir();
        }
    }

    // ------------------------------------------------------------------ abrir / fechar
    public void Abrir() {
        if (Aberto) return;
        Aberto = true;
        janela.SetActive(true);
        if (botaoPausa != null) botaoPausa.SetActive(false);
        if (PodePausar) {
            timeScaleAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
    }

    public void Fechar() {
        if (!Aberto) return;
        Aberto = false;
        janela.SetActive(false);
        if (confirmacao != null) confirmacao.SetActive(false);
        if (botaoPausa != null) botaoPausa.SetActive(true);
        if (PodePausar) Time.timeScale = timeScaleAnterior;
        Configuracoes.Salvar();
    }

    void SairParaMenu() {
        Aberto = false;
        Time.timeScale = 1f;
        Configuracoes.Salvar();
        if (GameMatchConfig.IsOnlineMatch && NetworkTransportService.Instance != null) NetworkTransportService.Instance.DesconectarTudo();
        SceneManager.LoadScene("StartScene");
    }

    // ------------------------------------------------------------------ construção
    void Construir() {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        if (contexto == Contexto.Partida) ConstruirBotaoPausa();
        ConstruirJanela();
        janela.SetActive(false);
    }

    void ConstruirBotaoPausa() {
        var b = TemaUI.Botao(transform, "", TemaUI.EstiloBotao.Escuro, Abrir);
        b.name = "BotaoPausa";
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(76, 76);
        rt.anchoredPosition = new Vector2(-65f, -186f);
        // Ícone de pausa: duas barras
        for (int i = 0; i < 2; i++) {
            var barra = TemaUI.Painel_(rt, "Barra", TemaUI.TextoClaro);
            barra.raycastTarget = false;
            var brt = barra.rectTransform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(12, 32);
            brt.anchoredPosition = new Vector2(i == 0 ? -10f : 10f, 0);
        }
        botaoPausa = b.gameObject;
    }

    void ConstruirJanela() {
        // Véu de fundo: escurece e bloqueia cliques no jogo
        var veu = TemaUI.Painel_(transform, "Janela", TemaUI.Veu);
        veu.sprite = null;
        TemaUI.Esticar(veu.rectTransform);
        janela = veu.gameObject;

        // Janela compacta em duas colunas: a altura se ajusta ao conteúdo (~60% da tela em 16:9)
        var painel = TemaUI.Painel_(veu.transform, "Painel", TemaUI.Janela);
        var prt = painel.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(LarguraJanela, 0);
        var borda = painel.gameObject.AddComponent<UnityEngine.UI.Outline>();
        borda.effectColor = new Color(TemaUI.Ouro.r, TemaUI.Ouro.g, TemaUI.Ouro.b, 0.55f);
        borda.effectDistance = new Vector2(3, -3);

        var layout = painel.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(44, 44, 22, 30);
        layout.spacing = 6;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var ajuste = painel.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        ajuste.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        // Cabeçalho
        bool partida = contexto == Contexto.Partida;
        var titulo = TemaUI.Texto(painel.transform, partida ? "Pausa" : "Configurações", 46, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.ContornoTexto(titulo, TemaUI.SombraVerde, 3f);
        Altura(titulo.gameObject, 58);
        if (partida) {
            string sub = GameMatchConfig.IsOnlineMatch
                ? "Partida online: o jogo continua enquanto você ajusta."
                : "Jogo pausado. Seu progresso é salvo no início de cada turno.";
            Altura(TemaUI.Texto(painel.transform, sub, 22, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleCenter).gameObject, 28);
        }

        var colunas = TemaUI.Criar("Colunas", painel.transform);
        var hc = colunas.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        hc.spacing = 32; hc.childAlignment = TextAnchor.UpperCenter;
        hc.childControlWidth = true; hc.childControlHeight = true;
        hc.childForceExpandWidth = true; hc.childForceExpandHeight = false;
        Transform esquerda = Coluna(colunas), direita = Coluna(colunas);

        Secao(esquerda, "ÁUDIO");
        Linha(esquerda, "Volume geral", c => TemaUI.Slider(c, Configuracoes.VolumeGeral, v => Configuracoes.VolumeGeral = v));
        Linha(esquerda, "Música", c => TemaUI.Slider(c, Configuracoes.VolumeMusica, v => Configuracoes.VolumeMusica = v));
        Linha(esquerda, "Efeitos sonoros", c => TemaUI.Slider(c, Configuracoes.VolumeEfeitos, v => Configuracoes.VolumeEfeitos = v));
        Secao(esquerda, "JOGO");
        Linha(esquerda, "Vibração", c => Pequeno(TemaUI.Interruptor(c, Configuracoes.Vibracao, v => Configuracoes.Vibracao = v)));
        Linha(esquerda, "Tempo limite (45 s)", c => Pequeno(TemaUI.Interruptor(c, Configuracoes.TimerTurno, v => Configuracoes.TimerTurno = v)));

        Secao(direita, "VÍDEO");
        Linha(direita, "Qualidade", c => TemaUI.Segmentado(c, new[] { "Baixa", "Média", "Alta" }, Configuracoes.Qualidade, i => Configuracoes.Qualidade = i));
        Linha(direita, "Quadros/s", c => TemaUI.Segmentado(c, new[] { "30", "60" }, Configuracoes.FpsAlvo >= 60 ? 1 : 0, i => Configuracoes.FpsAlvo = i == 1 ? 60 : 30));
        if (!Application.isMobilePlatform) {
            Linha(direita, "Tela cheia", c => Pequeno(TemaUI.Interruptor(c, Configuracoes.TelaCheia, v => Configuracoes.TelaCheia = v)));
        }

        // Rodapé
        Altura(TemaUI.Criar("Espaco", painel.transform).gameObject, 10);
        var rodape = TemaUI.Criar("Rodape", painel.transform);
        Altura(rodape.gameObject, 66);
        var h = rodape.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        h.spacing = 24; h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = true; h.childForceExpandHeight = true;
        if (partida) {
            TemaUI.Botao(rodape, "Sair para o menu", TemaUI.EstiloBotao.Perigo, () => confirmacao.SetActive(true));
            TemaUI.Botao(rodape, "Continuar", TemaUI.EstiloBotao.Destaque, Fechar);
            ConstruirConfirmacao();
        } else {
            TemaUI.Botao(rodape, "Fechar", TemaUI.EstiloBotao.Destaque, Fechar);
        }
    }

    void ConstruirConfirmacao() {
        var veu = TemaUI.Painel_(janela.transform, "Confirmacao", new Color(0, 0, 0, 0.55f));
        veu.sprite = null;
        TemaUI.Esticar(veu.rectTransform);
        confirmacao = veu.gameObject;

        var caixa = TemaUI.Painel_(veu.transform, "Caixa", TemaUI.Janela);
        var crt = caixa.rectTransform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(760, 330);
        var borda = caixa.gameObject.AddComponent<UnityEngine.UI.Outline>();
        borda.effectColor = new Color(TemaUI.Vermelho.r, TemaUI.Vermelho.g, TemaUI.Vermelho.b, 0.7f);
        borda.effectDistance = new Vector2(3, -3);

        var v = caixa.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        v.padding = new RectOffset(44, 44, 34, 34); v.spacing = 14;
        v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        Altura(TemaUI.Texto(caixa.transform, "Sair para o menu?", TemaUI.TamSubtitulo, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter).gameObject, 48);
        string aviso = GameMatchConfig.IsOnlineMatch
            ? "Você vai sair da partida online."
            : "A partida fica salva no início do turno atual. Use \"Continuar\" no menu para voltar.";
        Altura(TemaUI.Texto(caixa.transform, aviso, TemaUI.TamPequeno, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleCenter).gameObject, 70);

        var botoes = TemaUI.Criar("Botoes", caixa.transform);
        Altura(botoes.gameObject, 72);
        var h = botoes.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        h.spacing = 20; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
        TemaUI.Botao(botoes, "Cancelar", TemaUI.EstiloBotao.Claro, () => confirmacao.SetActive(false));
        TemaUI.Botao(botoes, "Sair", TemaUI.EstiloBotao.Perigo, SairParaMenu);
        confirmacao.SetActive(false);
    }

    // ------------------------------------------------------------------ helpers de layout
    static void Altura(GameObject go, float altura) {
        if (!go.TryGetComponent(out UnityEngine.UI.LayoutElement le)) le = go.AddComponent<UnityEngine.UI.LayoutElement>();
        le.minHeight = altura; le.preferredHeight = altura; le.flexibleHeight = 0;
    }

    const float LarguraJanela = 1260f;

    static Transform Coluna(Transform pai) {
        var c = TemaUI.Criar("Coluna", pai);
        var v = c.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        v.spacing = 6;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        return c;
    }

    static void Secao(Transform pai, string nome) {
        var t = TemaUI.Texto(pai, nome, 22, TemaUI.Ouro, FontStyle.Bold, TextAnchor.LowerLeft);
        Altura(t.gameObject, 32);
    }

    /// <summary>Linha "rótulo à esquerda | controle à direita".</summary>
    static void Linha(Transform pai, string rotulo, System.Action<Transform> criarControle) {
        var linha = TemaUI.Painel_(pai, "Linha_" + rotulo, TemaUI.PainelSuave);
        Altura(linha.gameObject, 54);
        var t = TemaUI.Texto(linha.transform, rotulo, 25, TemaUI.TextoClaro);
        t.resizeTextForBestFit = true; t.resizeTextMinSize = 18; t.resizeTextMaxSize = 25;
        t.rectTransform.anchorMin = new Vector2(0, 0); t.rectTransform.anchorMax = new Vector2(0.46f, 1);
        t.rectTransform.offsetMin = new Vector2(20, 0); t.rectTransform.offsetMax = Vector2.zero;

        var area = TemaUI.Criar("Controle", linha.transform);
        area.anchorMin = new Vector2(0.46f, 0); area.anchorMax = new Vector2(1, 1);
        area.offsetMin = new Vector2(8, 7); area.offsetMax = new Vector2(-16, -7);
        criarControle(area);
    }

    /// <summary>Interruptor com tamanho fixo, alinhado à direita da linha.</summary>
    static void Pequeno(UnityEngine.UI.Toggle tg) {
        var rt = (RectTransform)tg.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(80, 40);
        rt.anchoredPosition = Vector2.zero;
    }
}
