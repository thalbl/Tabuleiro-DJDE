using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// "Como Jogar": janela de páginas aberta pelo botão do menu principal (e na primeira vez que o jogo abre).
/// Construída em código com o TemaUI (mesmo padrão do menu de Configurações). Cada página tem título,
/// subtítulo e linhas "ícone + rótulo + descrição". Os itens vêm direto de ItemSpace.itemNames/itemDescriptions
/// e os ícones de Resources/texture/icons, então o texto acompanha o jogo.
/// Navegação: botões Voltar/Próximo, bolinhas clicáveis, setas ←/→, Enter e Esc.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // =================== MODELO ===================
    private class Linha
    {
        public string icone;    // caminho em Resources (Texture2D) ou null
        public string glifo;    // texto dentro do círculo quando não há ícone
        public Color cor;
        public string rotulo;
        public string texto;
    }

    private class Pagina
    {
        public string titulo;
        public string subtitulo;
        public string icone;
        public string glifo;
        public Color cor;
        public List<Linha> linhas = new List<Linha>();

        public Pagina L(Color cor, string glifo, string rotulo, string texto)
        {
            linhas.Add(new Linha { cor = cor, glifo = glifo, rotulo = rotulo, texto = texto });
            return this;
        }

        public Pagina I(string icone, Color cor, string rotulo, string texto)
        {
            linhas.Add(new Linha { icone = icone, cor = cor, rotulo = rotulo, texto = texto });
            return this;
        }
    }

    private const string IconeEstrela = "texture/icons/star";
    private const string IconeMoeda = "texture/icons/coin";
    private const string PastaIcones = "texture/icons/";

    // Layout (resolução de referência 1920x1080)
    private const float LarguraJanela = 1300f;
    private const float AlturaJanela = 960f;
    private const float AlturaCorpo = 620f;
    private const float EspacoLinhas = 10f;

    private List<Pagina> paginas;
    private int paginaAtual;

    // =================== UI ===================
    private GameObject raiz;
    private RectTransform janelaRT;
    private CanvasGroup conteudoGrupo;
    private Image iconeCirculo;
    private RawImage iconeImagem;
    private Text iconeGlifo;
    private Text txtTitulo, txtSubtitulo, txtContador;
    private RectTransform corpo;
    private Button btnVoltar, btnProximo;
    private Text txtProximo;
    private readonly List<Image> bolinhas = new List<Image>();
    private bool trocando;
    private bool fechando;

    void Awake()
    {
        CriarPaginas();
    }

    // =================== CONTEÚDO ===================
    private void CriarPaginas()
    {
        Color ouro = TemaUI.Ouro, verde = TemaUI.Verde, azul = TemaUI.Azul, vermelho = TemaUI.Vermelho;
        Color roxo = TemaUI.Hex("#A855F7"), ciano = TemaUI.Hex("#06B6D4"), laranja = TemaUI.Hex("#F97316");

        paginas = new List<Pagina>();

        paginas.Add(new Pagina {
            titulo = "Bem-vindo ao Caminho do Sucesso!",
            subtitulo = "Ande pelo tabuleiro, cuide do seu dinheiro e faça a carreira crescer. Cada turno é um mês.",
            icone = IconeEstrela, cor = ouro
        }
        .L(ouro, "1", "Meta de carreira", "Chegue ao nível de carreira pedido pelo mapa e vença na hora.")
        .L(azul, "2", "Fim das rodadas", "Se ninguém chegar à meta, vence quem tiver a maior pontuação.")
        .I(IconeEstrela, ouro, "Pontuação", "Estrelas × 100 + patrimônio líquido + carreira × 50 + educação × 75.")
        .I(IconeMoeda, verde, "Patrimônio líquido", "Moedas + poupança + reserva + ações, menos as dívidas."));

        paginas.Add(new Pagina {
            titulo = "Seu turno",
            subtitulo = "Todo turno começa com o holerite. Depois, escolha o que fazer.",
            glifo = "!", cor = verde
        }
        .I(IconeMoeda, verde, "Holerite", "Salário menos despesas, mais rendimentos e menos a parcela das dívidas.")
        .L(ouro, "▶", "Rolar dados", "Anda de 1 a 10 casas. Algumas casas agem quando você passa por elas.")
        .I(PastaIcones + BoardItem.Mushroom, laranja, "Usar item", "Um item por turno, antes de rolar os dados.")
        .L(ciano, "$", "Banco", "Guarde, invista, monte a reserva ou pegue um empréstimo.")
        .L(azul, "#", "Ver tabuleiro", "Olhe o mapa inteiro para planejar o caminho."));

        paginas.Add(new Pagina {
            titulo = "Banco e investimentos",
            subtitulo = "Dinheiro parado não cresce, e dívida cresce sozinha.",
            icone = IconeMoeda, cor = ciano
        }
        .L(verde, "%", "Poupança", $"Rende {PlayerState.JurosPoupanca * 100:0}% por turno, com juros compostos. Pode sacar quando quiser.")
        .L(azul, "+", "Reserva de emergência", "Não rende, mas paga os imprevistos das casas vermelhas antes do seu caixa.")
        .L(roxo, "↑", "Ações", "Sobem ou caem com a Bolsa a cada rodada (de −12% a +15%). Risco e retorno.")
        .L(laranja, "−", "Empréstimo", $"+{PlayerState.ValorEmprestimo} Moedas na hora, {PlayerState.JurosDivida * 100:0}% de juros por turno e parcela automática.")
        .L(vermelho, "!", "Cheque especial", $"Se faltar dinheiro, a diferença vira dívida (até {PlayerState.LimiteDivida} no total)."));

        paginas.Add(new Pagina {
            titulo = "Casas do tabuleiro",
            subtitulo = "O efeito depende da casa onde você para e muda conforme a sua carreira.",
            glifo = "#", cor = azul
        }
        .L(azul, "+", "Casa azul", "Bônus, aumento de salário ou participação nos lucros.")
        .L(vermelho, "−", "Casa vermelha", "Imprevistos, cortes e multas. A reserva paga primeiro.")
        .L(roxo, "?", "Pergunta", "Quiz de educação financeira: acerte e ganhe Moedas.")
        .I(IconeEstrela, ouro, "Estrela", "Compre Estrelas (100 pontos cada). Cada nova Estrela custa mais.")
        .I("texture/space/item", laranja, "Consultor e Loja", "Ganhe ou compre itens para a jornada."));

        paginas.Add(new Pagina {
            titulo = "Carreira e educação",
            subtitulo = "Sua profissão é sorteada no começo e define salário e despesas.",
            glifo = "↑", cor = verde
        }
        .L(verde, "★", "Portão de carreira", "Passe com os requisitos (nível, educação e reserva) para ser promovido.")
        .L(azul, "30", "Experiência", $"A cada {PlayerState.CasasPorPromocaoExperiencia} casas andadas você ganha uma promoção.")
        .L(roxo, "U", "Universidade", "Invista Moedas para subir: Ensino Médio, Superior e Pós-graduação.")
        .L(ouro, "?", "Bifurcação", "Uma pergunta de perfil decide o caminho. Não há resposta errada."));

        paginas.Add(new Pagina {
            titulo = "Perguntas do quiz",
            subtitulo = "Aprender também dá dinheiro. O tema acompanha a sua carreira.",
            glifo = "?", cor = roxo
        }
        .L(roxo, "1", "Quatro temas", "Finanças Pessoais, Trabalho e Crédito, Investimentos, Negócios e Estratégia.")
        .I(IconeMoeda, verde, "Acertou", "Ganha Moedas: quanto maior o nível do tema e o salário, maior o prêmio.")
        .L(vermelho, "✕", "Errou", "Você vê a resposta certa e uma explicação curta.")
        .L(azul, "↻", "Sem repetição", "Uma pergunta só volta depois que todas do tema saírem."));

        AdicionarPaginaItens("Itens de movimento", "Você carrega até 3 itens e usa 1 por turno.", laranja,
            BoardItem.Mushroom, BoardItem.GoldenMushroom, BoardItem.MagicMushroom,
            BoardItem.BowserSuit, BoardItem.MagicLamp, BoardItem.WarpPipe);

        AdicionarPaginaItens("Itens contra rivais", "Atrapalhe a vida financeira dos adversários.", vermelho,
            BoardItem.PoisonMushroom, BoardItem.TweesterTotem, BoardItem.BooBell,
            BoardItem.DuelingGlove, BoardItem.ChompCall, BoardItem.PlunderChest);

        AdicionarPaginaItens("Itens de proteção e renda", "Guarde para se proteger ou use para ganhar Moedas.", verde,
            BoardItem.Gaddlight, BoardItem.SkeletonKey, BoardItem.GoldenDrink, BoardItem.DoubleStarCard,
            BoardItem.ChompTreat, BoardItem.WigglerWhistle, BoardItem.VacPack);

        paginas.Add(new Pagina {
            titulo = "Dicas para vencer",
            subtitulo = "Boa sorte no Caminho do Sucesso!",
            icone = IconeEstrela, cor = ouro
        }
        .L(verde, "1", "Monte a reserva primeiro", "Ela segura as casas vermelhas sem tirar do seu caixa.")
        .L(vermelho, "2", "Fuja das dívidas", "Juros de 8% por turno comem o patrimônio rápido.")
        .L(roxo, "3", "Estude", "Educação abre o portão de carreira e vale pontos no final.")
        .L(ouro, "4", "Compre Estrelas com a sobra", "Cada uma vale 100 pontos, mas não fique sem reserva.")
        .L(azul, "5", "Mire na meta", "Chegar ao nível de carreira do mapa encerra a partida na hora."));
    }

    private void AdicionarPaginaItens(string titulo, string subtitulo, Color cor, params BoardItem[] itens)
    {
        var p = new Pagina { titulo = titulo, subtitulo = subtitulo, icone = PastaIcones + itens[0], cor = cor };
        foreach (BoardItem item in itens)
        {
            int i = (int)item;
            string nome = i < ItemSpace.itemNames.Count ? ItemSpace.itemNames[i] : item.ToString();
            string desc = i < ItemSpace.itemDescriptions.Count ? ItemSpace.itemDescriptions[i] : "";
            p.I(PastaIcones + item, cor, nome, desc);
        }
        paginas.Add(p);
    }

    // =================== API PÚBLICA ===================
    public void MostrarTutorial()
    {
        if (raiz != null) Destroy(raiz);
        paginaAtual = 0;
        fechando = false;
        trocando = false;
        Construir();
        MostrarPagina();
        StartCoroutine(AnimarAbertura());
    }

    public void FecharTutorial()
    {
        if (raiz == null || fechando) return;
        fechando = true;
        PlayerPrefs.SetInt("TutorialVisto", 1);
        StartCoroutine(AnimarFechamento());
    }

    public static bool JaViuTutorial()
    {
        return PlayerPrefs.GetInt("TutorialVisto", 0) == 1;
    }

    public bool Aberto => raiz != null && !fechando;

    void Update()
    {
        if (!Aberto) return;
        if (Input.GetKeyDown(KeyCode.Escape)) FecharTutorial();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Proximo();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Voltar();
    }

    // =================== NAVEGAÇÃO ===================
    private void Proximo()
    {
        if (paginaAtual < paginas.Count - 1) IrPara(paginaAtual + 1);
        else FecharTutorial();
    }

    private void Voltar()
    {
        if (paginaAtual > 0) IrPara(paginaAtual - 1);
    }

    private void IrPara(int indice)
    {
        if (trocando || indice == paginaAtual || indice < 0 || indice >= paginas.Count) return;
        StartCoroutine(TrocarPagina(indice));
    }

    // =================== CONSTRUÇÃO ===================
    private void Construir()
    {
        raiz = new GameObject("[ComoJogar]", typeof(RectTransform));
        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        var scaler = raiz.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        raiz.AddComponent<GraphicRaycaster>();

        // Véu: escurece e bloqueia cliques no menu
        var veu = TemaUI.Painel_(raiz.transform, "Veu", TemaUI.Veu);
        veu.sprite = null;
        TemaUI.Esticar(veu.rectTransform);

        // Janela
        var janela = TemaUI.Painel_(veu.transform, "Janela", TemaUI.Janela);
        janelaRT = janela.rectTransform;
        janelaRT.anchorMin = janelaRT.anchorMax = janelaRT.pivot = new Vector2(0.5f, 0.5f);
        janelaRT.sizeDelta = new Vector2(LarguraJanela, AlturaJanela);
        var borda = janela.gameObject.AddComponent<Outline>();
        borda.effectColor = new Color(TemaUI.Ouro.r, TemaUI.Ouro.g, TemaUI.Ouro.b, 0.55f);
        borda.effectDistance = new Vector2(3, -3);
        var sombra = janela.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0, 0, 0, 0.5f);
        sombra.effectDistance = new Vector2(0, -10);

        ConstruirCabecalho(janelaRT);

        // Corpo (a parte que troca a cada página)
        corpo = TemaUI.Criar("Corpo", janelaRT);
        corpo.anchorMin = new Vector2(0, 1);
        corpo.anchorMax = new Vector2(1, 1);
        corpo.pivot = new Vector2(0.5f, 1);
        corpo.anchoredPosition = new Vector2(0, -198);
        corpo.sizeDelta = new Vector2(-112, AlturaCorpo);
        var v = corpo.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = EspacoLinhas;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        conteudoGrupo = janela.gameObject.AddComponent<CanvasGroup>();

        ConstruirRodape(janelaRT);
    }

    private void ConstruirCabecalho(RectTransform janela)
    {
        // Círculo do ícone da página
        iconeCirculo = TemaUI.Painel_(janela, "IconePagina", TemaUI.Ouro);
        iconeCirculo.sprite = TemaUI.Circulo;
        iconeCirculo.type = Image.Type.Simple;
        var irt = iconeCirculo.rectTransform;
        irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0, 1);
        irt.sizeDelta = new Vector2(104, 104);
        irt.anchoredPosition = new Vector2(52, -44);

        var fundoIcone = TemaUI.Painel_(irt, "Miolo", TemaUI.Janela);
        fundoIcone.sprite = TemaUI.Circulo;
        fundoIcone.type = Image.Type.Simple;
        TemaUI.Esticar(fundoIcone.rectTransform, 6f);
        fundoIcone.raycastTarget = false;

        iconeImagem = TemaUI.Criar("Imagem", irt).gameObject.AddComponent<RawImage>();
        TemaUI.Esticar(iconeImagem.rectTransform, 14f);
        iconeImagem.raycastTarget = false;

        iconeGlifo = TemaUI.Texto(irt, "", 54, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(iconeGlifo.rectTransform);

        // Contador "COMO JOGAR · 1 de 10"
        txtContador = TemaUI.Texto(janela, "", TemaUI.TamPequeno, TemaUI.TextoSuave, FontStyle.Bold, TextAnchor.MiddleLeft);
        var crt = txtContador.rectTransform;
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0, 1);
        crt.anchoredPosition = new Vector2(180, -34);
        crt.sizeDelta = new Vector2(-310, 30);

        // Título
        txtTitulo = TemaUI.Texto(janela, "", 50, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleLeft);
        TemaUI.ContornoTexto(txtTitulo, TemaUI.SombraVerde, 3f);
        txtTitulo.horizontalOverflow = HorizontalWrapMode.Overflow;
        txtTitulo.resizeTextForBestFit = true;
        txtTitulo.resizeTextMinSize = 34;
        txtTitulo.resizeTextMaxSize = 50;
        var trt = txtTitulo.rectTransform;
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(180, -64);
        trt.sizeDelta = new Vector2(-310, 62);
        txtTitulo.horizontalOverflow = HorizontalWrapMode.Wrap;
        txtTitulo.verticalOverflow = VerticalWrapMode.Truncate;

        // Botão fechar (X)
        var fechar = TemaUI.Botao(janela, "✕", TemaUI.EstiloBotao.Escuro, FecharTutorial, 34);
        fechar.name = "BotaoFechar";
        var frt = (RectTransform)fechar.transform;
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(1, 1);
        frt.sizeDelta = new Vector2(72, 72);
        frt.anchoredPosition = new Vector2(-36, -36);

        // Linha divisória dourada + subtítulo
        var divisoria = TemaUI.Painel_(janela, "Divisoria", new Color(TemaUI.Ouro.r, TemaUI.Ouro.g, TemaUI.Ouro.b, 0.35f));
        divisoria.sprite = null;
        var drt = divisoria.rectTransform;
        drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1); drt.pivot = new Vector2(0.5f, 1);
        drt.anchoredPosition = new Vector2(0, -180);
        drt.sizeDelta = new Vector2(-112, 3);

        txtSubtitulo = TemaUI.Texto(janela, "", 28, TemaUI.TextoClaro, FontStyle.Normal, TextAnchor.MiddleLeft);
        txtSubtitulo.resizeTextForBestFit = true;
        txtSubtitulo.resizeTextMinSize = 22;
        txtSubtitulo.resizeTextMaxSize = 28;
        txtSubtitulo.verticalOverflow = VerticalWrapMode.Truncate;
        var srt = txtSubtitulo.rectTransform;
        srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(0, 1);
        srt.anchoredPosition = new Vector2(180, -128);
        srt.sizeDelta = new Vector2(-236, 40);
    }

    private void ConstruirRodape(RectTransform janela)
    {
        var rodape = TemaUI.Criar("Rodape", janela);
        rodape.anchorMin = new Vector2(0, 0); rodape.anchorMax = new Vector2(1, 0); rodape.pivot = new Vector2(0.5f, 0);
        rodape.anchoredPosition = new Vector2(0, 40);
        rodape.sizeDelta = new Vector2(-112, 84);

        btnVoltar = TemaUI.Botao(rodape, "Voltar", TemaUI.EstiloBotao.Escuro, Voltar);
        var vrt = (RectTransform)btnVoltar.transform;
        vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0, 0.5f);
        vrt.sizeDelta = new Vector2(260, 80);
        vrt.anchoredPosition = Vector2.zero;

        btnProximo = TemaUI.Botao(rodape, "Próximo", TemaUI.EstiloBotao.Destaque, Proximo);
        var prt = (RectTransform)btnProximo.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1, 0.5f);
        prt.sizeDelta = new Vector2(260, 80);
        prt.anchoredPosition = Vector2.zero;
        txtProximo = btnProximo.GetComponentInChildren<Text>();

        // Bolinhas de página (clicáveis)
        var pontos = TemaUI.Criar("Paginas", rodape);
        pontos.anchorMin = pontos.anchorMax = pontos.pivot = new Vector2(0.5f, 0.5f);
        pontos.sizeDelta = new Vector2(600, 40);
        var h = pontos.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        bolinhas.Clear();
        for (int i = 0; i < paginas.Count; i++)
        {
            int indice = i;
            var b = TemaUI.Painel_(pontos, "Pagina" + (i + 1), TemaUI.PainelSuave);
            b.sprite = TemaUI.Arredondado;
            b.type = Image.Type.Sliced;
            b.pixelsPerUnitMultiplier = 4f;
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 22; le.preferredHeight = 22;
            var btn = b.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => IrPara(indice));
            bolinhas.Add(b);
        }
    }

    // =================== PÁGINA ===================
    private void MostrarPagina()
    {
        Pagina p = paginas[paginaAtual];

        txtContador.text = $"COMO JOGAR  •  {paginaAtual + 1} DE {paginas.Count}";
        txtTitulo.text = p.titulo;
        txtSubtitulo.text = p.subtitulo;

        iconeCirculo.color = p.cor;
        Texture2D tex = string.IsNullOrEmpty(p.icone) ? null : Resources.Load<Texture2D>(p.icone);
        iconeImagem.texture = tex;
        iconeImagem.enabled = tex != null;
        iconeGlifo.text = tex != null ? "" : (p.glifo ?? "");
        iconeGlifo.color = p.cor;

        // Linhas
        for (int i = corpo.childCount - 1; i >= 0; i--) Destroy(corpo.GetChild(i).gameObject);
        corpo.DetachChildren();
        int n = Mathf.Max(1, p.linhas.Count);
        float altura = Mathf.Min(104f, (AlturaCorpo - EspacoLinhas * (n - 1)) / n);
        foreach (Linha l in p.linhas) CriarLinha(l, altura);

        // Rodapé
        bool primeira = paginaAtual == 0, ultima = paginaAtual == paginas.Count - 1;
        btnVoltar.interactable = !primeira;
        if (!btnVoltar.TryGetComponent(out CanvasGroup cgVoltar)) cgVoltar = btnVoltar.gameObject.AddComponent<CanvasGroup>();
        cgVoltar.alpha = primeira ? 0.35f : 1f;
        txtProximo.text = ultima ? "Começar!" : "Próximo";

        for (int i = 0; i < bolinhas.Count; i++)
        {
            bool atual = i == paginaAtual;
            bolinhas[i].color = atual ? TemaUI.Ouro : (i < paginaAtual ? TemaUI.TextoSuave : TemaUI.PainelSuave);
            var le = bolinhas[i].GetComponent<LayoutElement>();
            le.preferredWidth = atual ? 48 : 22;
        }
    }

    private void CriarLinha(Linha l, float altura)
    {
        var fundo = TemaUI.Painel_(corpo, "Linha_" + l.rotulo, TemaUI.PainelSuave);
        fundo.raycastTarget = false;
        var le = fundo.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = altura; le.minHeight = altura;

        // Faixa colorida à esquerda
        var faixa = TemaUI.Painel_(fundo.transform, "Faixa", l.cor);
        faixa.raycastTarget = false;
        var frt = faixa.rectTransform;
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0, 1); frt.pivot = new Vector2(0, 0.5f);
        frt.sizeDelta = new Vector2(10, -16);
        frt.anchoredPosition = new Vector2(10, 0);

        // Ícone (imagem ou glifo num círculo)
        float lado = Mathf.Min(altura - 18f, 76f);
        var chip = TemaUI.Painel_(fundo.transform, "Icone", new Color(l.cor.r, l.cor.g, l.cor.b, 0.22f));
        chip.sprite = TemaUI.Circulo;
        chip.type = Image.Type.Simple;
        chip.raycastTarget = false;
        var crt = chip.rectTransform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0, 0.5f);
        crt.sizeDelta = new Vector2(lado, lado);
        crt.anchoredPosition = new Vector2(34, 0);

        Texture2D tex = string.IsNullOrEmpty(l.icone) ? null : Resources.Load<Texture2D>(l.icone);
        if (tex != null)
        {
            var img = TemaUI.Criar("Imagem", crt).gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            TemaUI.Esticar(img.rectTransform, lado * 0.08f);
        }
        else
        {
            var g = TemaUI.Texto(crt, l.glifo ?? "", Mathf.RoundToInt(lado * 0.5f), l.cor, FontStyle.Bold, TextAnchor.MiddleCenter);
            TemaUI.Esticar(g.rectTransform);
            g.resizeTextForBestFit = true;
            g.resizeTextMinSize = 14;
            g.resizeTextMaxSize = Mathf.RoundToInt(lado * 0.5f);
        }

        // Rótulo + descrição
        float x = 34 + lado + 24;
        var rotulo = TemaUI.Texto(fundo.transform, l.rotulo, 30, ClarearSeEscura(l.cor), FontStyle.Bold, TextAnchor.LowerLeft);
        var rrt = rotulo.rectTransform;
        rrt.anchorMin = new Vector2(0, 0.5f); rrt.anchorMax = new Vector2(1, 1);
        rrt.offsetMin = new Vector2(x, 2); rrt.offsetMax = new Vector2(-24, -6);
        rotulo.resizeTextForBestFit = true;
        rotulo.resizeTextMinSize = 20; rotulo.resizeTextMaxSize = 30;
        rotulo.verticalOverflow = VerticalWrapMode.Truncate;

        var texto = TemaUI.Texto(fundo.transform, l.texto, 25, TemaUI.TextoClaro, FontStyle.Normal, TextAnchor.UpperLeft);
        var xrt = texto.rectTransform;
        xrt.anchorMin = new Vector2(0, 0); xrt.anchorMax = new Vector2(1, 0.5f);
        xrt.offsetMin = new Vector2(x, 4); xrt.offsetMax = new Vector2(-24, -2);
        texto.resizeTextForBestFit = true;
        texto.resizeTextMinSize = 18; texto.resizeTextMaxSize = 25;
        texto.verticalOverflow = VerticalWrapMode.Truncate;
    }

    /// <summary>Cores escuras/saturadas demais ficam ilegíveis sobre o painel: clareia um pouco o rótulo.</summary>
    private static Color ClarearSeEscura(Color c)
    {
        float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        return lum < 0.45f ? Color.Lerp(c, Color.white, 0.35f) : c;
    }

    // =================== ANIMAÇÕES ===================
    private IEnumerator AnimarAbertura()
    {
        janelaRT.localScale = Vector3.one * 0.85f;
        conteudoGrupo.alpha = 0f;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.25f);
            float ease = 1f + 2.2f * Mathf.Pow(p - 1f, 3f) + 1.2f * Mathf.Pow(p - 1f, 2f);
            janelaRT.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, ease);
            conteudoGrupo.alpha = p;
            yield return null;
        }
        janelaRT.localScale = Vector3.one;
        conteudoGrupo.alpha = 1f;
    }

    private IEnumerator AnimarFechamento()
    {
        float t = 0f;
        while (t < 0.15f && janelaRT != null)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.15f);
            janelaRT.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, p);
            conteudoGrupo.alpha = 1f - p;
            yield return null;
        }
        if (raiz != null) Destroy(raiz);
        raiz = null;
        fechando = false;
    }

    private IEnumerator TrocarPagina(int indice)
    {
        trocando = true;
        int direcao = indice > paginaAtual ? 1 : -1;
        if (!corpo.TryGetComponent(out CanvasGroup cg)) cg = corpo.gameObject.AddComponent<CanvasGroup>();
        Vector2 base_ = corpo.anchoredPosition;

        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.1f);
            cg.alpha = 1f - p;
            corpo.anchoredPosition = base_ + new Vector2(-40f * direcao * p, 0);
            yield return null;
        }

        paginaAtual = indice;
        MostrarPagina();

        t = 0f;
        while (t < 0.14f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.14f);
            cg.alpha = p;
            corpo.anchoredPosition = base_ + new Vector2(40f * direcao * (1f - p), 0);
            yield return null;
        }
        cg.alpha = 1f;
        corpo.anchoredPosition = base_;
        trocando = false;
    }
}
