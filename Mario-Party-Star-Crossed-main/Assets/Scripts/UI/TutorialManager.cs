using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Tutorial interativo com slides estilo "Cartilha" que explica como o jogo funciona.
/// Adiciona um botão "Como Jogar" no menu principal.
/// Pode ser chamado do MainMenuBuilder ou exibido automaticamente no primeiro jogo.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // =================== CORES (Consistentes com MainMenuBuilder) ===================
    private Color corVerdeFundo = new Color(0.09f, 0.40f, 0.20f, 1f);      // #166534
    private Color corVerdeClaro = new Color(0.86f, 0.99f, 0.91f, 1f);      // #dcfce7
    private Color corBordaEscura = new Color(0.02f, 0.31f, 0.23f, 1f);     // #064e3b
    private Color corCardFundo = new Color(0.94f, 0.99f, 0.96f, 1f);       // #f0fdf4
    private Color corDourado = new Color(0.98f, 0.75f, 0.14f, 1f);         // #fbbf24
    private Color corDouradoBorda = new Color(0.47f, 0.21f, 0.06f, 1f);    // #78350f
    private Color corTextoEscuro = new Color(0.13f, 0.13f, 0.13f, 1f);
    private Color corDestaque = new Color(0.09f, 0.40f, 0.20f, 1f);        // #166534
    private Color corVermelho = new Color(0.94f, 0.27f, 0.27f, 1f);        // #ef4444
    private Color corAzul = new Color(0.23f, 0.51f, 0.96f, 1f);            // #3b82f6
    private Color corOverlay = new Color(0, 0, 0, 0.7f);

    // =================== ESTADO ===================
    private Font fonteFredoka;
    private GameObject overlayObj;
    private GameObject cardObj;
    private RectTransform cardRT;
    private Text txtTitulo;
    private Text txtConteudo;
    private Text txtPagina;
    private Text txtIcone;
    private Image headerImage;
    private int paginaAtual = 0;

    // =================== DADOS DO TUTORIAL ===================
    private struct SlideTutorial
    {
        public string icone;
        public string titulo;
        public string conteudo;
        public Color corHeader;

        public SlideTutorial(string icone, string titulo, string conteudo, Color corHeader)
        {
            this.icone = icone;
            this.titulo = titulo;
            this.conteudo = conteudo;
            this.corHeader = corHeader;
        }
    }

    private List<SlideTutorial> slides;

    void Awake()
    {
        CriarSlides();
    }

    private void CarregarFonte()
    {
        if (fonteFredoka != null) return;
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fonteFredoka == null) fonteFredoka = Font.CreateDynamicFontFromOSFont("Arial", 24);
    }

    // =================== CONTEÚDO DOS SLIDES ===================
    private void CriarSlides()
    {
        slides = new List<SlideTutorial>
        {
            new SlideTutorial(
                "?",
                "Bem-vindo ao Caminho do Sucesso!",
                "Este e um jogo de tabuleiro onde voce " +
                "avanca pelo caminho, coleta Moedas e " +
                "Estrelas, e evolui sua carreira!\n\n" +
                "Sua PONTUACAO FINAL e calculada assim:\n" +
                "Estrelas x100 + Moedas + Carreira x50 " +
                "+ Educacao x75\n\n" +
                "Vence quem tiver mais pontos!",
                corVerdeFundo
            ),

            new SlideTutorial(
                "!",
                "Seu Turno",
                "No seu turno, voce recebe seu Holerite " +
                "(Salario - Despesas = Liquido) e pode " +
                "escolher:\n\n" +
                "ROLAR DADOS - Avanca pelo tabuleiro\n\n" +
                "USAR ITEM - Usa um item do inventario\n\n" +
                "VER TABULEIRO - Olha o mapa completo\n\n" +
                "Profissoes com salario alto tem despesas " +
                "maiores! Fique de olho na renda liquida.",
                corVerdeFundo
            ),

            new SlideTutorial(
                "$",
                "Moedas e Estrelas",
                "MOEDAS sao ganhas ao cair em casas " +
                "azuis, acertar perguntas e receber " +
                "a mesada do seu trabalho.\n\n" +
                "ESTRELAS custam 20 Moedas e sao " +
                "compradas ao passar pela casa de Estrela.\n\n" +
                "Quem tem mais Estrelas esta em " +
                "primeiro lugar!",
                corDourado
            ),

            new SlideTutorial(
                "#",
                "Tipos de Casas",
                "AZUL - Voce ganha Moedas!\n\n" +
                "VERMELHA - Voce perde Moedas...\n\n" +
                "ESTRELA - Compre uma Estrela por 20 Moedas!\n\n" +
                "ITEM - Ganhe um item gratuito!\n\n" +
                "PERGUNTA - Responda certo e ganhe Moedas!\n\n" +
                "SORTE - Evento aleatorio!\n\n" +
                "CARREIRA - Evolua sua profissao!",
                corAzul
            ),

            new SlideTutorial(
                "%",
                "Itens de Movimento",
                "Voce pode carregar ate 3 itens e usar " +
                "1 por turno antes de rolar o dado:\n\n" +
                "COFRINHO - Avanca 1 casa extra\n\n" +
                "SUPER DADO - Avanca 2 casas extras\n\n" +
                "FOGUETE - Avanca 3 casas extras!\n\n" +
                "FANTASIA DO BOWSER - Avanca 5 casas " +
                "e cobra 20 Moedas de quem passar por voce!",
                corVermelho
            ),

            new SlideTutorial(
                "%",
                "Itens Estrategicos",
                "BURACO NO BOLSO - Um rival perde " +
                "4 passos de movimento!\n\n" +
                "PORTAL MAGICO - Troca de lugar " +
                "com outro jogador!\n\n" +
                "VENTANIA - Sopra um rival para " +
                "um lugar aleatorio do tabuleiro!\n\n" +
                "LAMPADA MAGICA - Te leva direto " +
                "ate a Estrela!\n\n" +
                "SINO DO BOO - O Boo rouba moedas " +
                "ou estrelas de um rival!",
                corVermelho
            ),

            new SlideTutorial(
                "~",
                "Carreira e Educacao",
                "Cada jogador tem uma Profissao que " +
                "determina sua Mesada (moedas por turno).\n\n" +
                "Na casa de CARREIRA, voce pode " +
                "evoluir seu nivel profissional, " +
                "aumentando sua Mesada!\n\n" +
                "Na casa de EDUCACAO, voce investe " +
                "em formacao (Ensino Medio, Superior, " +
                "Pos-graduacao), que desbloqueia " +
                "promocoes melhores!",
                corDestaque
            ),

            new SlideTutorial(
                ">",
                "Pronto para Jogar!",
                "Resumo rapido:\n\n" +
                "1. Receba seu Holerite (Salario - Despesas)\n" +
                "2. Role o dado e ande pelo tabuleiro\n" +
                "3. Casas azuis = bom, vermelhas = ruim\n" +
                "4. Compre Estrelas quando puder (20 Moedas)\n" +
                "5. Use itens estrategicamente\n" +
                "6. Evolua carreira e educacao!\n\n" +
                "Boa sorte no Caminho do Sucesso!",
                corDourado
            )
        };
    }

    // =================== API PÚBLICA ===================
    public void MostrarTutorial()
    {
        paginaAtual = 0;
        if (overlayObj != null) Destroy(overlayObj);
        CarregarFonte();
        ConstruirUI();
        AtualizarSlide();
        StartCoroutine(AnimarAbertura());
    }

    public void FecharTutorial()
    {
        StartCoroutine(AnimarFechamento());
        // Marcar que o jogador já viu o tutorial
        PlayerPrefs.SetInt("TutorialVisto", 1);
    }

    public static bool JaViuTutorial()
    {
        return PlayerPrefs.GetInt("TutorialVisto", 0) == 1;
    }

    // =================== CONSTRUÇÃO DA UI ===================
    private void ConstruirUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // --- Overlay escuro ---
        overlayObj = new GameObject("TutorialOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayObj.AddComponent<RectTransform>();
        Expandir(overlayRT);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = corOverlay;

        // --- Card Principal ---
        cardObj = new GameObject("TutorialCard");
        cardObj.transform.SetParent(overlayObj.transform, false);
        cardRT = cardObj.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(700, 550);
        cardRT.anchoredPosition = Vector2.zero;

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = corCardFundo;
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = corBordaEscura;
        cardOutline.effectDistance = new Vector2(4, -4);
        Shadow cardShadow = cardObj.AddComponent<Shadow>();
        cardShadow.effectColor = corBordaEscura;
        cardShadow.effectDistance = new Vector2(-6, -8);

        // --- Header (título + ícone) ---
        GameObject header = new GameObject("Header");
        header.transform.SetParent(cardObj.transform, false);
        RectTransform headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.sizeDelta = new Vector2(0, 100);
        headerRT.anchoredPosition = new Vector2(0, -50);
        headerImage = header.AddComponent<Image>();
        headerImage.color = corVerdeFundo;

        // Ícone do slide
        GameObject iconeObj = new GameObject("Icone");
        iconeObj.transform.SetParent(header.transform, false);
        RectTransform iconeRT = iconeObj.AddComponent<RectTransform>();
        iconeRT.anchorMin = new Vector2(0, 0.5f);
        iconeRT.anchorMax = new Vector2(0, 0.5f);
        iconeRT.sizeDelta = new Vector2(60, 60);
        iconeRT.anchoredPosition = new Vector2(50, 0);

        Image iconeBg = iconeObj.AddComponent<Image>();
        iconeBg.color = new Color(1, 1, 1, 0.2f);

        // Texto do ícone (filho separado — Image e Text não podem coexistir)
        GameObject iconeTxtObj = new GameObject("IconeTexto");
        iconeTxtObj.transform.SetParent(iconeObj.transform, false);
        RectTransform iconeTxtRT = iconeTxtObj.AddComponent<RectTransform>();
        iconeTxtRT.anchorMin = Vector2.zero;
        iconeTxtRT.anchorMax = Vector2.one;
        iconeTxtRT.sizeDelta = Vector2.zero;
        iconeTxtRT.anchoredPosition = Vector2.zero;

        txtIcone = iconeTxtObj.AddComponent<Text>();
        txtIcone.font = fonteFredoka;
        txtIcone.fontSize = 36;
        txtIcone.fontStyle = FontStyle.Bold;
        txtIcone.alignment = TextAnchor.MiddleCenter;
        txtIcone.color = Color.white;
        txtIcone.raycastTarget = false;

        // Título do slide
        GameObject tituloObj = new GameObject("Titulo");
        tituloObj.transform.SetParent(header.transform, false);
        RectTransform tituloRT = tituloObj.AddComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0, 0);
        tituloRT.anchorMax = new Vector2(1, 1);
        tituloRT.offsetMin = new Vector2(90, 0);
        tituloRT.offsetMax = new Vector2(-20, 0);

        txtTitulo = tituloObj.AddComponent<Text>();
        txtTitulo.font = fonteFredoka;
        txtTitulo.fontSize = 32;
        txtTitulo.fontStyle = FontStyle.Bold;
        txtTitulo.alignment = TextAnchor.MiddleLeft;
        txtTitulo.color = Color.white;
        Shadow titleShadow = tituloObj.AddComponent<Shadow>();
        titleShadow.effectColor = corBordaEscura;
        titleShadow.effectDistance = new Vector2(2, -2);

        // --- Conteúdo ---
        GameObject conteudoObj = new GameObject("Conteudo");
        conteudoObj.transform.SetParent(cardObj.transform, false);
        RectTransform conteudoRT = conteudoObj.AddComponent<RectTransform>();
        conteudoRT.anchorMin = new Vector2(0, 0);
        conteudoRT.anchorMax = new Vector2(1, 1);
        conteudoRT.offsetMin = new Vector2(35, 90);  // Bottom + padding
        conteudoRT.offsetMax = new Vector2(-35, -115); // Top abaixo do header

        txtConteudo = conteudoObj.AddComponent<Text>();
        txtConteudo.font = fonteFredoka;
        txtConteudo.fontSize = 24;
        txtConteudo.fontStyle = FontStyle.Normal;
        txtConteudo.alignment = TextAnchor.UpperLeft;
        txtConteudo.color = corTextoEscuro;
        txtConteudo.lineSpacing = 1.1f;
        txtConteudo.horizontalOverflow = HorizontalWrapMode.Wrap;
        txtConteudo.verticalOverflow = VerticalWrapMode.Overflow;

        // --- Barra inferior (navegação) ---
        GameObject footer = new GameObject("Footer");
        footer.transform.SetParent(cardObj.transform, false);
        RectTransform footerRT = footer.AddComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.sizeDelta = new Vector2(0, 70);
        footerRT.anchoredPosition = new Vector2(0, 35);

        Image footerBg = footer.AddComponent<Image>();
        footerBg.color = corVerdeClaro;

        // Indicador de página
        GameObject paginaObj = new GameObject("Pagina");
        paginaObj.transform.SetParent(footer.transform, false);
        RectTransform paginaRT = paginaObj.AddComponent<RectTransform>();
        paginaRT.anchorMin = new Vector2(0.5f, 0.5f);
        paginaRT.anchorMax = new Vector2(0.5f, 0.5f);
        paginaRT.sizeDelta = new Vector2(200, 40);
        paginaRT.anchoredPosition = Vector2.zero;

        txtPagina = paginaObj.AddComponent<Text>();
        txtPagina.font = fonteFredoka;
        txtPagina.fontSize = 22;
        txtPagina.fontStyle = FontStyle.Bold;
        txtPagina.alignment = TextAnchor.MiddleCenter;
        txtPagina.color = corDestaque;

        // Botão Anterior
        CriarBotaoNav(footer.transform, "<", new Vector2(60, 0), () => SlideAnterior());

        // Botão Próximo / Fechar
        CriarBotaoNav(footer.transform, ">", new Vector2(-60, 0), () => ProximoSlide(), true);
    }

    // =================== BOTÃO DE NAVEGAÇÃO ===================
    private GameObject CriarBotaoNav(Transform parent, string texto, Vector2 pos, UnityEngine.Events.UnityAction onClick, bool ladoDireito = false)
    {
        GameObject btnObj = new GameObject("Btn_" + texto);
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin = ladoDireito ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        btnRT.anchorMax = ladoDireito ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        btnRT.sizeDelta = new Vector2(90, 50);
        btnRT.anchoredPosition = pos;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = corDourado;
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = corDouradoBorda;
        btnOutline.effectDistance = new Vector2(2, -2);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        // Desativar transição de cor padrão
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white; cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(0.9f, 0.9f, 0.9f); cb.selectedColor = Color.white;
        btn.colors = cb;

        GameObject txtObj = new GameObject("Texto");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRT = txtObj.AddComponent<RectTransform>();
        Expandir(txtRT);

        Text btnText = txtObj.AddComponent<Text>();
        btnText.text = texto;
        btnText.font = fonteFredoka;
        btnText.fontSize = 30;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = corDouradoBorda;

        // Efeito hover
        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = corDourado;
        fx.corHover = new Color(1f, 0.85f, 0.35f, 1f);
        fx.corTextoNormal = corDouradoBorda;
        fx.corTextoHover = corDouradoBorda;
        fx.escalaHover = 1.08f;
        fx.subirHover = 3f;

        return btnObj;
    }

    // =================== NAVEGAÇÃO ===================
    private void ProximoSlide()
    {
        if (paginaAtual < slides.Count - 1)
        {
            paginaAtual++;
            StartCoroutine(TransicaoSlide());
        }
        else
        {
            FecharTutorial();
        }
    }

    private void SlideAnterior()
    {
        if (paginaAtual > 0)
        {
            paginaAtual--;
            StartCoroutine(TransicaoSlide());
        }
    }

    private void AtualizarSlide()
    {
        if (paginaAtual < 0 || paginaAtual >= slides.Count) return;
        SlideTutorial slide = slides[paginaAtual];

        txtIcone.text = slide.icone;
        txtTitulo.text = slide.titulo;
        txtConteudo.text = slide.conteudo;
        headerImage.color = slide.corHeader;
        txtPagina.text = (paginaAtual + 1) + " / " + slides.Count;
    }

    // =================== ANIMAÇÕES ===================
    private IEnumerator AnimarAbertura()
    {
        if (cardRT == null) yield break;
        cardRT.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float p = t / 0.35f;
            float ease = 1f + 2.7f * Mathf.Pow(p - 1f, 3f) + 1.7f * Mathf.Pow(p - 1f, 2f);
            cardRT.localScale = Vector3.one * Mathf.Clamp(ease, 0f, 1.15f);
            yield return null;
        }
        cardRT.localScale = Vector3.one;
    }

    private IEnumerator AnimarFechamento()
    {
        if (cardRT == null) yield break;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            cardRT.localScale = Vector3.one * (1f - t / 0.2f);
            yield return null;
        }
        if (overlayObj != null) Destroy(overlayObj);
    }

    private IEnumerator TransicaoSlide()
    {
        // Fade out rápido
        float t = 0f;
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();

        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - (t / 0.12f);
            yield return null;
        }

        AtualizarSlide();

        // Fade in
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = t / 0.12f;
            yield return null;
        }
        cg.alpha = 1f;
    }

    // =================== UTILITÁRIOS ===================
    private void Expandir(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
