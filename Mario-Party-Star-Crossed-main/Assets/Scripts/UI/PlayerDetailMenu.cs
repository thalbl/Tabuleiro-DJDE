using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Cartão de status do jogador estilo "Overcooked / Juicy".
/// Gera toda a UI programaticamente ao ser chamado com ShowPlayerDetails().
/// Mantém compatibilidade com PlayerTracker.cs (que chama ShowPlayerDetails).
/// </summary>
public class PlayerDetailMenu : MonoBehaviour
{
    // =================== CORES DO DESIGN ===================
    private Color corCardFundo = new Color(0.94f, 0.99f, 0.96f, 1f);       // #f0fdf4
    private Color corBordaEscura = new Color(0.02f, 0.31f, 0.23f, 1f);     // #064e3b
    private Color corHeaderVerde = new Color(0.09f, 0.40f, 0.20f, 1f);     // #166534
    private Color corJobBoxFundo = new Color(0.86f, 0.99f, 0.91f, 1f);     // #dcfce7
    private Color corJobBoxBorda = new Color(0.08f, 0.50f, 0.24f, 1f);     // #15803d
    private Color corCurrencyFundo = new Color(1f, 0.95f, 0.78f, 1f);      // #fef3c7
    private Color corCurrencyBorda = new Color(0.71f, 0.33f, 0.04f, 1f);   // #b45309
    private Color corCurrencyTexto = new Color(0.47f, 0.21f, 0.06f, 1f);   // #78350f
    private Color corInfoFundo = Color.white;
    private Color corInfoBorda = new Color(0.80f, 0.84f, 0.88f, 1f);       // #cbd5e1
    private Color corInfoTexto = new Color(0.20f, 0.25f, 0.33f, 1f);       // #334155
    private Color corInfoDestaque = new Color(0.09f, 0.40f, 0.20f, 1f);    // #166534
    private Color corBotaoDourado = new Color(0.98f, 0.75f, 0.14f, 1f);    // #fbbf24
    private Color corBotaoTexto = new Color(0.27f, 0.10f, 0.01f, 1f);      // #451a03
    private Color corBotaoBorda = new Color(0.47f, 0.21f, 0.06f, 1f);      // #78350f
    private Color corOverlayFundo = new Color(0, 0, 0, 0.5f);

    // =================== ESTADO ===================
    private PlayerState currentPlayer;
    private bool isVisible = false;
    private Font fonteFredoka;

    // Referências do painel gerado
    private GameObject overlayObj;
    private GameObject cardObj;
    private RectTransform cardRT;

    // Textos dinâmicos
    private Text txtNome;
    private Text txtProfissaoTitulo;
    private Text txtProfissaoNivel;
    private Text txtMoedas;
    private Text txtEstrelas;
    private Text txtAplicacoes;
    private Text txtEducacao;
    private Text txtMesada;

    void Start()
    {
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    // =================== API PÚBLICA ===================
    public void ShowPlayerDetails(PlayerState player)
    {
        currentPlayer = player;

        // Interromper qualquer animação em andamento antes de recriar
        StopAllCoroutines();

        // Destruir cartão anterior se existir
        if (overlayObj != null) {
            Destroy(overlayObj);
            overlayObj = null;
            cardRT = null;
        }

        ConstruirCard();
        AtualizarDados();

        isVisible = true;
        StartCoroutine(AnimarAbertura());
    }

    public void CloseMenu()
    {
        if (overlayObj != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimarFechamento());
        }
    }

    public bool IsVisible() => isVisible;

    public void RefreshDisplay()
    {
        if (isVisible && currentPlayer != null) AtualizarDados();
    }

    // =================== CONSTRUÇÃO DO CARD ===================
    private void ConstruirCard()
    {
        // Encontrar ou criar Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // --- Overlay escuro (fundo clicável para fechar) ---
        overlayObj = new GameObject("PlayerCardOverlay");
        overlayObj.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayObj.AddComponent<RectTransform>();
        Expandir(overlayRT);
        Image overlayImg = overlayObj.AddComponent<Image>();
        overlayImg.color = corOverlayFundo;
        Button overlayBtn = overlayObj.AddComponent<Button>();
        overlayBtn.onClick.AddListener(CloseMenu);
        ColorBlock cb = overlayBtn.colors;
        cb.normalColor = Color.white; cb.highlightedColor = Color.white;
        cb.pressedColor = Color.white; cb.selectedColor = Color.white;
        overlayBtn.colors = cb;

        // --- Card Principal ---
        cardObj = CriarPainelArredondado("PlayerCard", overlayObj.transform, corCardFundo, corBordaEscura, 560, 650);
        cardRT = cardObj.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        // Impedir que clique no card feche
        Button cardBtn = cardObj.AddComponent<Button>();
        cardBtn.onClick.AddListener(() => { }); // Absorve o clique
        cardBtn.colors = cb;

        float yPos = 0;

        // --- HEADER (Nome do Jogador) ---
        GameObject header = CriarPainelSimples("Header", cardObj.transform, corHeaderVerde, 560, 90);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 1f);
        headerRT.anchorMax = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0, -45);

        txtNome = CriarTextoFilho("TxtNome", header.transform, "JOGADOR", 46, Color.white, FontStyle.Bold);
        AdicionarSombraTexto(txtNome, corBordaEscura, new Vector2(3, -3));

        yPos = -105;

        // --- JOB BOX (Profissão) ---
        GameObject jobBox = CriarPainelArredondado("JobBox", cardObj.transform, corJobBoxFundo, corJobBoxBorda, 510, 95);
        RectTransform jobRT = jobBox.GetComponent<RectTransform>();
        jobRT.anchorMin = new Vector2(0.5f, 1f);
        jobRT.anchorMax = new Vector2(0.5f, 1f);
        jobRT.anchoredPosition = new Vector2(0, yPos - 47);

        txtProfissaoTitulo = CriarTextoFilho("TxtProfTitulo", jobBox.transform, "Profissão", 30, corHeaderVerde, FontStyle.Bold);
        RectTransform profTituloRT = txtProfissaoTitulo.GetComponent<RectTransform>();
        profTituloRT.anchoredPosition = new Vector2(0, 14);

        txtProfissaoNivel = CriarTextoFilho("TxtProfNivel", jobBox.transform, "Nível", 20, corJobBoxBorda, FontStyle.Normal);
        RectTransform profNivelRT = txtProfissaoNivel.GetComponent<RectTransform>();
        profNivelRT.anchoredPosition = new Vector2(0, -18);

        yPos -= 115;

        // --- CURRENCY GRID (Moedas + Estrelas) ---
        float currencyBoxW = 240;
        float currencyBoxH = 80;

        // Moedas
        GameObject moedasBox = CriarPainelArredondado("MoedasBox", cardObj.transform, corCurrencyFundo, corCurrencyBorda, currencyBoxW, currencyBoxH);
        RectTransform moedasRT = moedasBox.GetComponent<RectTransform>();
        moedasRT.anchorMin = new Vector2(0.5f, 1f);
        moedasRT.anchorMax = new Vector2(0.5f, 1f);
        moedasRT.anchoredPosition = new Vector2(-130, yPos - 40);
        // Sombra do box
        AdicionarSombraObjeto(moedasBox, corCurrencyBorda);

        Text moedasLabel = CriarTextoFilho("MoedasIcon", moedasBox.transform, "Moedas", 18, corCurrencyBorda, FontStyle.Normal);
        RectTransform mlRT = moedasLabel.GetComponent<RectTransform>();
        mlRT.anchoredPosition = new Vector2(0, 20);

        txtMoedas = CriarTextoFilho("TxtMoedas", moedasBox.transform, "0", 36, corCurrencyTexto, FontStyle.Bold);
        RectTransform tmRT = txtMoedas.GetComponent<RectTransform>();
        tmRT.anchoredPosition = new Vector2(0, -10);

        // Estrelas
        GameObject estrelasBox = CriarPainelArredondado("EstrelasBox", cardObj.transform, corCurrencyFundo, corCurrencyBorda, currencyBoxW, currencyBoxH);
        RectTransform estrelasRT = estrelasBox.GetComponent<RectTransform>();
        estrelasRT.anchorMin = new Vector2(0.5f, 1f);
        estrelasRT.anchorMax = new Vector2(0.5f, 1f);
        estrelasRT.anchoredPosition = new Vector2(130, yPos - 40);
        AdicionarSombraObjeto(estrelasBox, corCurrencyBorda);

        Text estrelasLabel = CriarTextoFilho("EstrelasIcon", estrelasBox.transform, "Estrelas", 18, corCurrencyBorda, FontStyle.Normal);
        RectTransform elRT = estrelasLabel.GetComponent<RectTransform>();
        elRT.anchoredPosition = new Vector2(0, 20);

        txtEstrelas = CriarTextoFilho("TxtEstrelas", estrelasBox.transform, "0", 36, corCurrencyTexto, FontStyle.Bold);
        RectTransform teRT = txtEstrelas.GetComponent<RectTransform>();
        teRT.anchoredPosition = new Vector2(0, -10);

        yPos -= 105;

        // --- INFO LIST (Aplicações, Educação, Mesada) ---
        GameObject infoBox = CriarPainelArredondado("InfoBox", cardObj.transform, corInfoFundo, corInfoBorda, 510, 160);
        RectTransform infoRT = infoBox.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0.5f, 1f);
        infoRT.anchorMax = new Vector2(0.5f, 1f);
        infoRT.anchoredPosition = new Vector2(0, yPos - 80);

        // Aplicações
        CriarLinhaInfo(infoBox.transform, "Aplicações:", out txtAplicacoes, 45);
        // Educação
        CriarLinhaInfo(infoBox.transform, "Educação:", out txtEducacao, 0);
        // Mesada
        CriarLinhaInfo(infoBox.transform, "Mesada:", out txtMesada, -45);

        yPos -= 185;

        // --- BOTÃO PRONTO ---
        GameObject btnObj = CriarPainelArredondado("BtnPronto", cardObj.transform, corBotaoDourado, corBotaoBorda, 180, 60);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 0f);
        btnRT.anchorMax = new Vector2(1f, 0f);
        btnRT.anchoredPosition = new Vector2(-110, 45);
        AdicionarSombraObjeto(btnObj, corBotaoBorda);

        Text btnText = CriarTextoFilho("BtnTexto", btnObj.transform, "Pronto!", 28, corBotaoTexto, FontStyle.Bold);

        Button fecharBtn = btnObj.AddComponent<Button>();
        fecharBtn.targetGraphic = btnObj.GetComponent<Image>();
        fecharBtn.onClick.AddListener(CloseMenu);

        // Adicionar efeito de hover
        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = corBotaoDourado;
        fx.corHover = new Color(1f, 0.85f, 0.30f, 1f);
        fx.corTextoNormal = corBotaoTexto;
        fx.corTextoHover = corBotaoTexto;
        fx.escalaHover = 1.08f;
        fx.subirHover = 3f;
    }

    // =================== LINHA DE INFO ===================
    private void CriarLinhaInfo(Transform parent, string label, out Text valorText, float yOffset)
    {
        // Label (lado esquerdo)
        Text lblTxt = CriarTextoFilho("Lbl_" + label, parent, label, 22, corInfoTexto, FontStyle.Bold);
        RectTransform lblRT = lblTxt.GetComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0.5f);
        lblRT.anchorMax = new Vector2(0, 0.5f);
        lblRT.sizeDelta = new Vector2(240, 36);
        lblRT.anchoredPosition = new Vector2(135, yOffset);
        lblTxt.alignment = TextAnchor.MiddleLeft;

        // Valor (lado direito)
        valorText = CriarTextoFilho("Val_" + label, parent, "---", 22, corInfoDestaque, FontStyle.Bold);
        RectTransform valRT = valorText.GetComponent<RectTransform>();
        valRT.anchorMin = new Vector2(1, 0.5f);
        valRT.anchorMax = new Vector2(1, 0.5f);
        valRT.sizeDelta = new Vector2(240, 36);
        valRT.anchoredPosition = new Vector2(-135, yOffset);
        valorText.alignment = TextAnchor.MiddleRight;
    }

    // =================== ATUALIZAR DADOS ===================
    private void AtualizarDados()
    {
        if (currentPlayer == null) return;

        // Nome
        if (txtNome != null)
            txtNome.text = currentPlayer.charName().ToUpper();

        // Profissão
        if (txtProfissaoTitulo != null)
            txtProfissaoTitulo.text = currentPlayer.GetProfissao();

        if (txtProfissaoNivel != null)
            txtProfissaoNivel.text = currentPlayer.GetFormattedCareerLevel();

        // Moedas e Estrelas
        if (txtMoedas != null)
            txtMoedas.text = currentPlayer.getCoins().ToString();

        if (txtEstrelas != null)
            txtEstrelas.text = currentPlayer.getStars().ToString();

        // Aplicações
        if (txtAplicacoes != null)
        {
            var investments = currentPlayer.GetInvestments();
            txtAplicacoes.text = investments.Count > 0 ? string.Join(", ", investments) : "Nenhuma";
        }

        // Educação
        if (txtEducacao != null)
        {
            switch (currentPlayer.GetEducationLevel())
            {
                case 0: txtEducacao.text = "Sem formalidade"; break;
                case 1: txtEducacao.text = "Ensino Médio"; break;
                case 2: txtEducacao.text = "Ensino Superior"; break;
                case 3: txtEducacao.text = "Pós-graduação"; break;
                default: txtEducacao.text = "Avançada"; break;
            }
        }

        // Mesada
        if (txtMesada != null)
        {
            int sal = currentPlayer.GetSalary();
            int desp = currentPlayer.GetDespesaAtual();
            int liquido = sal - desp;
            txtMesada.text = $"{(liquido >= 0 ? "+" : "")}{liquido} / turno";
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // =================== ANIMAÇÕES ===================
    private IEnumerator AnimarAbertura()
    {
        if (cardRT == null) yield break;
        cardRT.localScale = Vector3.zero;
        float t = 0f;
        float duracao = 0.35f;

        while (t < duracao)
        {
            if (cardRT == null) yield break;
            t += Time.unscaledDeltaTime;
            float progresso = t / duracao;
            // Ease OutBack: ultrapassa levemente e volta
            float ease = 1f + 2.7f * Mathf.Pow(progresso - 1f, 3f) + 1.7f * Mathf.Pow(progresso - 1f, 2f);
            cardRT.localScale = Vector3.one * Mathf.Clamp(ease, 0f, 1.15f);
            yield return null;
        }
        if (cardRT != null) cardRT.localScale = Vector3.one;
    }

    private IEnumerator AnimarFechamento()
    {
        if (cardRT == null) {
            isVisible = false;
            if (overlayObj != null) Destroy(overlayObj);
            overlayObj = null;
            yield break;
        }

        float t = 0f;
        float duracao = 0.2f;

        while (t < duracao)
        {
            if (cardRT == null) yield break;
            t += Time.unscaledDeltaTime;
            float progresso = t / duracao;
            cardRT.localScale = Vector3.one * (1f - progresso);
            yield return null;
        }

        isVisible = false;
        if (overlayObj != null) Destroy(overlayObj);
        overlayObj = null;
        cardRT = null;
    }

    // =================== UTILITÁRIOS DE UI ===================
    private GameObject CriarPainelArredondado(string nome, Transform parent, Color corFundo, Color corBorda, float largura, float altura)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(largura, altura);

        Image img = obj.AddComponent<Image>();
        img.color = corFundo;

        // Simular borda com Outline
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = corBorda;
        outline.effectDistance = new Vector2(3, -3);

        return obj;
    }

    private GameObject CriarPainelSimples(string nome, Transform parent, Color cor, float largura, float altura)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(largura, altura);

        Image img = obj.AddComponent<Image>();
        img.color = cor;

        return obj;
    }

    private Text CriarTextoFilho(string nome, Transform parent, string conteudo, int tamanho, Color cor, FontStyle estilo)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, tamanho + 15);
        rt.anchoredPosition = Vector2.zero;

        Text txt = obj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonteFredoka;
        txt.fontSize = tamanho;
        txt.fontStyle = estilo;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = cor;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        return txt;
    }

    private void AdicionarSombraTexto(Text txt, Color cor, Vector2 distancia)
    {
        Shadow shadow = txt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = cor;
        shadow.effectDistance = distancia;
    }

    private void AdicionarSombraObjeto(GameObject obj, Color cor)
    {
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = cor;
        shadow.effectDistance = new Vector2(-3, -4);
    }

    private void Expandir(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}