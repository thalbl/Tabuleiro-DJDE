using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Interface do Lobby Local (Seleção de Jogadores, Avatares, Tipos Humano/Bot e Regras).
/// Criada dinamicamente via código para garantir integridade e compatibilidade entre plataformas.
/// </summary>
public class LocalLobbyUI : MonoBehaviour {

    // =================== CORES DO DESIGN ===================
    private Color corFundoOverlay = new Color(0.04f, 0.14f, 0.09f, 1f); // opaco: tela própria, sem o menu vazando por trás
    private Color corCardFundo = new Color(0.12f, 0.25f, 0.18f, 0.95f);
    private Color corCardBorda = new Color(0.29f, 0.87f, 0.50f, 0.5f);
    private Color corTituloDourado = new Color(0.98f, 0.75f, 0.14f, 1f);
    private Color corTextoClaro = new Color(0.95f, 0.98f, 0.95f, 1f);
    private Color corTextoSuave = new Color(0.70f, 0.85f, 0.75f, 1f);

    private Color corBotaoAtivo = new Color(0.29f, 0.87f, 0.50f, 1f);
    private Color corBotaoInativo = new Color(0.20f, 0.35f, 0.25f, 1f);
    private Color corBotaoHumano = new Color(0.20f, 0.65f, 0.95f, 1f);
    private Color corBotaoCPU = new Color(0.95f, 0.55f, 0.15f, 1f);

    // =================== REFERÊNCIAS ===================
    private GameObject painelLobby;
    private Font fonteFredoka;
    private MainMenuBuilder menuPrincipal;

    // Componentes de Slots dos Jogadores
    private struct SlotUIElements {
        public Text txtSlotNumero;
        public Text txtNomeJogador;
        public InputField inputNome;
        public Image imgAvatarIcon;
        public Text txtAvatarNome;
        public Button btnAvatarPrev;
        public Button btnAvatarNext;
        public Button btnTipoControlador;
        public Text txtTipoControlador;
        public Image imgTipoFundo;
    }

    private SlotUIElements[] slotUI = new SlotUIElements[4];
    private List<Button> botoesTurnos = new List<Button>();
    private Text txtPassAndPlayStatus;
    private Text txtMapaStatus;

    public void Inicializar(MainMenuBuilder mainMenu, Transform parentTransform) {
        this.menuPrincipal = mainMenu;
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameMatchConfig.LoadFromPrefs();
        CriarPainelLobby(parentTransform);
        AtualizarTodaInterface();
        painelLobby.SetActive(false);
    }

    public void Mostrar() {
        if (painelLobby != null) {
            painelLobby.transform.SetAsLastSibling();
            painelLobby.SetActive(true);
            AtualizarTodaInterface();
        }
    }

    public void Ocultar() {
        if (painelLobby != null) {
            painelLobby.SetActive(false);
        }
    }

    // =================== CONSTRUÇÃO DO PAINEL ===================
    private void CriarPainelLobby(Transform parent) {
        painelLobby = new GameObject("PainelLobbyLocal");
        painelLobby.transform.SetParent(parent, false);

        RectTransform rt = painelLobby.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Fundo escuro com blur/overlay
        Image imgFundo = painelLobby.AddComponent<Image>();
        imgFundo.color = corFundoOverlay;

        // Container Central
        GameObject container = new GameObject("ContainerCentral");
        container.transform.SetParent(painelLobby.transform, false);
        RectTransform rtContainer = container.AddComponent<RectTransform>();
        rtContainer.anchorMin = new Vector2(0.5f, 0.5f);
        rtContainer.anchorMax = new Vector2(0.5f, 0.5f);
        rtContainer.sizeDelta = new Vector2(1400, 850);
        rtContainer.anchoredPosition = Vector2.zero;

        CriarCabecalho(container.transform);
        CriarCardsJogadores(container.transform);
        CriarPainelConfiguracoes(container.transform);
        CriarBotoesAcao(container.transform);
    }

    private void CriarCabecalho(Transform parent) {
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(parent, false);
        RectTransform rt = headerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.94f);
        rt.anchorMax = new Vector2(0.5f, 0.94f);
        rt.sizeDelta = new Vector2(1200, 100);
        rt.anchoredPosition = Vector2.zero;

        // Título
        GameObject txtTituloObj = new GameObject("TxtTitulo");
        txtTituloObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtT = txtTituloObj.AddComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0.5f, 0.65f);
        rtT.anchorMax = new Vector2(0.5f, 0.65f);
        rtT.sizeDelta = new Vector2(1000, 50);
        rtT.anchoredPosition = Vector2.zero;

        Text txtT = txtTituloObj.AddComponent<Text>();
        txtT.text = "CONFIGURAÇÃO DA PARTIDA LOCAL";
        txtT.font = fonteFredoka;
        txtT.fontSize = 42;
        txtT.fontStyle = FontStyle.Bold;
        txtT.alignment = TextAnchor.MiddleCenter;
        txtT.color = corTituloDourado;
        // Fredoka tem entrelinha alta: sem Overflow, o título não cabia nos 50 px e não era desenhado
        txtT.verticalOverflow = VerticalWrapMode.Overflow;
        TemaUI.ContornoTexto(txtT, TemaUI.SombraVerde, 3f);

        // Subtítulo
        GameObject txtSubObj = new GameObject("TxtSub");
        txtSubObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtS = txtSubObj.AddComponent<RectTransform>();
        rtS.anchorMin = new Vector2(0.5f, 0.15f);
        rtS.anchorMax = new Vector2(0.5f, 0.15f);
        rtS.sizeDelta = new Vector2(800, 30);
        rtS.anchoredPosition = Vector2.zero;

        Text txtS = txtSubObj.AddComponent<Text>();
        txtS.text = "Defina os jogadores (Humano ou CPU), nomes e duração da partida";
        txtS.font = fonteFredoka;
        txtS.fontSize = 22;
        txtS.alignment = TextAnchor.MiddleCenter;
        txtS.color = corTextoSuave;
    }

    private void CriarCardsJogadores(Transform parent) {
        GameObject cardsContainer = new GameObject("CardsContainer");
        cardsContainer.transform.SetParent(parent, false);
        RectTransform rt = cardsContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.58f);
        rt.anchorMax = new Vector2(0.5f, 0.58f);
        rt.sizeDelta = new Vector2(1360, 400);
        rt.anchoredPosition = Vector2.zero;

        float cardWidth = 310f;
        float spacing = 26f;
        float startX = -((4 * cardWidth + 3 * spacing) / 2f) + (cardWidth / 2f);

        for (int i = 0; i < 4; i++) {
            float posX = startX + i * (cardWidth + spacing);
            CriarCardJogador(cardsContainer.transform, i, posX);
        }
    }

    private void CriarCardJogador(Transform parent, int slotIndex, float posX) {
        GameObject card = new GameObject($"Card_P{slotIndex + 1}");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(310, 390);
        rt.anchoredPosition = new Vector2(posX, 0);

        // Fundo do card
        Image imgCard = card.AddComponent<Image>();
        imgCard.color = corCardFundo;
        imgCard.sprite = ObterSpriteArredondado(16);
        imgCard.type = Image.Type.Sliced;

        // Outline suave
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = corCardBorda;
        outline.effectDistance = new Vector2(2, -2);

        // Badge do Jogador (P1, P2, P3, P4)
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform rtB = badgeObj.AddComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0.5f, 1f);
        rtB.anchorMax = new Vector2(0.5f, 1f);
        rtB.sizeDelta = new Vector2(100, 36);
        rtB.anchoredPosition = new Vector2(0, -22);

        Image imgBadge = badgeObj.AddComponent<Image>();
        imgBadge.color = GameMatchConfig.GetAvatarColor(GameMatchConfig.Slots[slotIndex].avatarId);
        imgBadge.sprite = ObterSpriteArredondado(10);
        imgBadge.type = Image.Type.Sliced;

        Text txtB = CriarTextoInterno(badgeObj.transform, $"P{slotIndex + 1}", 20, Color.white, FontStyle.Bold);

        // Avatar Visual (Círculo de Cor com Nome do Avatar)
        GameObject avatarCircle = new GameObject("AvatarCircle");
        avatarCircle.transform.SetParent(card.transform, false);
        RectTransform rtAv = avatarCircle.AddComponent<RectTransform>();
        rtAv.anchorMin = new Vector2(0.5f, 0.68f);
        rtAv.anchorMax = new Vector2(0.5f, 0.68f);
        rtAv.sizeDelta = new Vector2(110, 110);
        rtAv.anchoredPosition = Vector2.zero;

        Image imgAv = avatarCircle.AddComponent<Image>();
        imgAv.sprite = ObterSpriteArredondado(60);
        imgAv.color = GameMatchConfig.GetAvatarColor(GameMatchConfig.Slots[slotIndex].avatarId);

        // Botão Avatar Anterior (<)
        Button btnPrev = CriarBotaoMini(card.transform, "<", new Vector2(-80, 70), () => AlterarAvatar(slotIndex, -1));

        // Botão Avatar Próximo (>)
        Button btnNext = CriarBotaoMini(card.transform, ">", new Vector2(80, 70), () => AlterarAvatar(slotIndex, 1));

        // Nome do Avatar
        GameObject txtAvNomeObj = new GameObject("TxtAvatarNome");
        txtAvNomeObj.transform.SetParent(card.transform, false);
        RectTransform rtAvN = txtAvNomeObj.AddComponent<RectTransform>();
        rtAvN.anchorMin = new Vector2(0.5f, 0.46f);
        rtAvN.anchorMax = new Vector2(0.5f, 0.46f);
        rtAvN.sizeDelta = new Vector2(250, 30);
        rtAvN.anchoredPosition = Vector2.zero;
        Text txtAvN = txtAvNomeObj.AddComponent<Text>();
        txtAvN.text = GameMatchConfig.GetAvatarDefaultName(GameMatchConfig.Slots[slotIndex].avatarId);
        txtAvN.font = fonteFredoka;
        txtAvN.fontSize = 20;
        txtAvN.alignment = TextAnchor.MiddleCenter;
        txtAvN.color = corTextoClaro;

        // InputField de Nome Customizado
        GameObject inputObj = new GameObject("InputNome");
        inputObj.transform.SetParent(card.transform, false);
        RectTransform rtIn = inputObj.AddComponent<RectTransform>();
        rtIn.anchorMin = new Vector2(0.5f, 0.30f);
        rtIn.anchorMax = new Vector2(0.5f, 0.30f);
        rtIn.sizeDelta = new Vector2(250, 42);
        rtIn.anchoredPosition = Vector2.zero;

        Image imgInput = inputObj.AddComponent<Image>();
        imgInput.color = new Color(0.08f, 0.18f, 0.12f, 1f);
        imgInput.sprite = ObterSpriteArredondado(8);
        imgInput.type = Image.Type.Sliced;

        InputField inputField = inputObj.AddComponent<InputField>();

        GameObject txtInObj = new GameObject("Text");
        txtInObj.transform.SetParent(inputObj.transform, false);
        RectTransform rtTxtIn = txtInObj.AddComponent<RectTransform>();
        rtTxtIn.anchorMin = Vector2.zero;
        rtTxtIn.anchorMax = Vector2.one;
        rtTxtIn.offsetMin = new Vector2(10, 0);
        rtTxtIn.offsetMax = new Vector2(-10, 0);

        Text txtInput = txtInObj.AddComponent<Text>();
        txtInput.font = fonteFredoka;
        txtInput.fontSize = 20;
        txtInput.color = Color.white;
        txtInput.alignment = TextAnchor.MiddleCenter;
        inputField.textComponent = txtInput;
        inputField.text = GameMatchConfig.Slots[slotIndex].name;
        int localIdx = slotIndex;
        inputField.onValueChanged.AddListener((val) => {
            GameMatchConfig.Slots[localIdx].name = string.IsNullOrEmpty(val.Trim()) ? $"Jogador {localIdx + 1}" : val.Trim();
        });

        // Botão de Tipo (HUMANO / CPU)
        GameObject btnTipoObj = new GameObject("BtnTipo");
        btnTipoObj.transform.SetParent(card.transform, false);
        RectTransform rtTipo = btnTipoObj.AddComponent<RectTransform>();
        rtTipo.anchorMin = new Vector2(0.5f, 0.11f);
        rtTipo.anchorMax = new Vector2(0.5f, 0.11f);
        rtTipo.sizeDelta = new Vector2(240, 46);
        rtTipo.anchoredPosition = Vector2.zero;

        Image imgTipo = btnTipoObj.AddComponent<Image>();
        imgTipo.sprite = ObterSpriteArredondado(10);
        imgTipo.type = Image.Type.Sliced;
        imgTipo.color = GameMatchConfig.Slots[slotIndex].IsHuman ? corBotaoHumano : corBotaoCPU;

        Button btnTipo = btnTipoObj.AddComponent<Button>();
        btnTipo.onClick.AddListener(() => AlternarTipoControlador(slotIndex));

        Text txtTipo = CriarTextoInterno(btnTipoObj.transform, GameMatchConfig.Slots[slotIndex].IsHuman ? "HUMANO" : "BOT (CPU)", 20, Color.white, FontStyle.Bold);

        slotUI[slotIndex] = new SlotUIElements {
            txtSlotNumero = txtB,
            txtNomeJogador = txtInput,
            inputNome = inputField,
            imgAvatarIcon = imgAv,
            txtAvatarNome = txtAvN,
            btnAvatarPrev = btnPrev,
            btnAvatarNext = btnNext,
            btnTipoControlador = btnTipo,
            txtTipoControlador = txtTipo,
            imgTipoFundo = imgTipo
        };
    }

    private void CriarPainelConfiguracoes(Transform parent) {
        GameObject configPanel = new GameObject("PainelRegras");
        configPanel.transform.SetParent(parent, false);
        RectTransform rt = configPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.22f);
        rt.anchorMax = new Vector2(0.5f, 0.22f);
        rt.sizeDelta = new Vector2(1360, 90);
        rt.anchoredPosition = Vector2.zero;

        // Seletor de Mapa (Cidade Clássica vs Circuito Rápido)
        GameObject mapaObj = new GameObject("BtnMapa");
        mapaObj.transform.SetParent(configPanel.transform, false);
        RectTransform rtM = mapaObj.AddComponent<RectTransform>();
        rtM.anchorMin = new Vector2(0.15f, 0.5f);
        rtM.anchorMax = new Vector2(0.15f, 0.5f);
        rtM.sizeDelta = new Vector2(380, 46);
        rtM.anchoredPosition = Vector2.zero;

        Image imgMapa = mapaObj.AddComponent<Image>();
        imgMapa.sprite = ObterSpriteArredondado(10);
        imgMapa.type = Image.Type.Sliced;
        imgMapa.color = CorDoMapa();

        Button btnMapa = mapaObj.AddComponent<Button>();
        btnMapa.onClick.AddListener(AlternarMapa);

        txtMapaStatus = CriarTextoInterno(mapaObj.transform, $"MAPA: {GameMatchConfig.GetMapDisplayName(GameMatchConfig.SelectedMapScene)}", 18, Color.white, FontStyle.Bold);
        txtMapaStatus.resizeTextForBestFit = true;
        txtMapaStatus.resizeTextMinSize = 12;
        txtMapaStatus.resizeTextMaxSize = 18;

        // Label Turnos
        GameObject lblTurnos = new GameObject("LblTurnos");
        lblTurnos.transform.SetParent(configPanel.transform, false);
        RectTransform rtLT = lblTurnos.AddComponent<RectTransform>();
        rtLT.anchorMin = new Vector2(0.33f, 0.5f);
        rtLT.anchorMax = new Vector2(0.33f, 0.5f);
        rtLT.sizeDelta = new Vector2(160, 40);
        rtLT.anchoredPosition = Vector2.zero;
        Text txtLT = lblTurnos.AddComponent<Text>();
        txtLT.text = "RODADAS:";
        txtLT.font = fonteFredoka;
        txtLT.fontSize = 22;
        txtLT.fontStyle = FontStyle.Bold;
        txtLT.alignment = TextAnchor.MiddleRight;
        txtLT.color = corTituloDourado;

        // Botões de Turnos: 10, 15, 20, 30
        int[] opcoesTurnos = new int[] { 10, 15, 20, 30 };
        botoesTurnos.Clear();
        float btnStartX = 590f;
        for (int i = 0; i < opcoesTurnos.Length; i++) {
            int turnosVal = opcoesTurnos[i];
            float posX = btnStartX + i * 92f;
            Button b = CriarBotaoOpcaoTurno(configPanel.transform, $"{turnosVal}", posX, turnosVal == GameMatchConfig.MaxTurns, () => SelecionarTurnos(turnosVal));
            botoesTurnos.Add(b);
        }

        // Toggle Pass & Play
        GameObject passObj = new GameObject("BtnPassAndPlay");
        passObj.transform.SetParent(configPanel.transform, false);
        RectTransform rtP = passObj.AddComponent<RectTransform>();
        rtP.anchorMin = new Vector2(0.86f, 0.5f);
        rtP.anchorMax = new Vector2(0.86f, 0.5f);
        rtP.sizeDelta = new Vector2(340, 46);
        rtP.anchoredPosition = Vector2.zero;

        Image imgPass = passObj.AddComponent<Image>();
        imgPass.sprite = ObterSpriteArredondado(10);
        imgPass.type = Image.Type.Sliced;
        imgPass.color = GameMatchConfig.PassAndPlayEnabled ? corBotaoAtivo : corBotaoInativo;

        Button btnPass = passObj.AddComponent<Button>();
        btnPass.onClick.AddListener(AlternarPassAndPlay);

        txtPassAndPlayStatus = CriarTextoInterno(passObj.transform, GameMatchConfig.PassAndPlayEnabled ? "PASS & PLAY: ATIVADO" : "PASS & PLAY: DESATIVADO", 18, Color.white, FontStyle.Bold);
    }

    private void CriarBotoesAcao(Transform parent) {
        GameObject actionsContainer = new GameObject("BotoesAcao");
        actionsContainer.transform.SetParent(parent, false);
        RectTransform rt = actionsContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.08f);
        rt.anchorMax = new Vector2(0.5f, 0.08f);
        rt.sizeDelta = new Vector2(800, 75);
        rt.anchoredPosition = Vector2.zero;

        // Botão Voltar
        CriarBotaoGrande(actionsContainer.transform, "VOLTAR", -220, new Color(0.85f, 0.25f, 0.25f, 1f), new Color(0.45f, 0.1f, 0.1f, 1f), () => Ocultar());

        // Botão Iniciar Partida
        CriarBotaoGrande(actionsContainer.transform, "INICIAR PARTIDA", 220, new Color(0.29f, 0.87f, 0.50f, 1f), new Color(0.06f, 0.40f, 0.20f, 1f), () => IniciarJogoLocal());
    }

    // =================== FACTORIES UI ===================
    private Text CriarTextoInterno(Transform parent, string conteudo, int tamanho, Color cor, FontStyle estilo) {
        GameObject txtObj = new GameObject("Texto");
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonteFredoka;
        txt.fontSize = tamanho;
        txt.fontStyle = estilo;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = cor;
        return txt;
    }

    private Button CriarBotaoMini(Transform parent, string texto, Vector2 pos, UnityEngine.Events.UnityAction onClick) {
        GameObject btnObj = new GameObject($"Btn_{texto}");
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(36, 36);
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = ObterSpriteArredondado(10);
        img.type = Image.Type.Sliced;
        img.color = new Color(0.25f, 0.45f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        CriarTextoInterno(btnObj.transform, texto, 22, Color.white, FontStyle.Bold);
        return btn;
    }

    private Button CriarBotaoOpcaoTurno(Transform parent, string texto, float posX, bool ativo, UnityEngine.Events.UnityAction onClick) {
        GameObject btnObj = new GameObject($"BtnTurno_{texto}");
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(85, 46);
        rt.anchoredPosition = new Vector2(posX, 0);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = ObterSpriteArredondado(10);
        img.type = Image.Type.Sliced;
        img.color = ativo ? corBotaoAtivo : corBotaoInativo;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        CriarTextoInterno(btnObj.transform, texto, 20, ativo ? new Color(0.09f, 0.40f, 0.20f, 1f) : Color.white, FontStyle.Bold);
        return btn;
    }

    private void CriarBotaoGrande(Transform parent, string texto, float posX, Color corBase, Color corSombra, UnityEngine.Events.UnityAction onClick) {
        // Sombra
        GameObject sombra = new GameObject($"{texto}_Sombra");
        sombra.transform.SetParent(parent, false);
        RectTransform rtS = sombra.AddComponent<RectTransform>();
        rtS.anchorMin = new Vector2(0.5f, 0.5f);
        rtS.anchorMax = new Vector2(0.5f, 0.5f);
        rtS.sizeDelta = new Vector2(340, 68);
        rtS.anchoredPosition = new Vector2(posX - 4, -4);

        Image imgS = sombra.AddComponent<Image>();
        imgS.sprite = ObterSpriteArredondado(14);
        imgS.type = Image.Type.Sliced;
        imgS.color = corSombra;

        // Botão Frontal
        GameObject btnObj = new GameObject($"{texto}_Btn");
        btnObj.transform.SetParent(parent, false);
        RectTransform rtB = btnObj.AddComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0.5f, 0.5f);
        rtB.anchorMax = new Vector2(0.5f, 0.5f);
        rtB.sizeDelta = new Vector2(340, 68);
        rtB.anchoredPosition = new Vector2(posX, 0);

        Image imgB = btnObj.AddComponent<Image>();
        imgB.sprite = ObterSpriteArredondado(14);
        imgB.type = Image.Type.Sliced;
        imgB.color = corBase;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = corBase;
        fx.corHover = corTituloDourado;
        fx.escalaHover = 1.05f;

        CriarTextoInterno(btnObj.transform, texto, 26, new Color(0.05f, 0.25f, 0.12f, 1f), FontStyle.Bold);
    }

    // =================== LÓGICA E EVENTOS ===================
    private void AlterarAvatar(int slotIndex, int direcao) {
        int atual = GameMatchConfig.Slots[slotIndex].avatarId;
        atual += direcao;
        if (atual < 1) atual = 6;
        if (atual > 6) atual = 1;
        GameMatchConfig.Slots[slotIndex].avatarId = atual;

        // Se o nome atual do jogador for o padrão, atualiza junto com o avatar
        string defaultNameAnterior = GameMatchConfig.GetAvatarDefaultName(atual - direcao);
        if (GameMatchConfig.Slots[slotIndex].name == defaultNameAnterior || string.IsNullOrEmpty(GameMatchConfig.Slots[slotIndex].name)) {
            GameMatchConfig.Slots[slotIndex].name = GameMatchConfig.GetAvatarDefaultName(atual);
        }

        AtualizarSlot(slotIndex);
    }

    private void AlternarTipoControlador(int slotIndex) {
        if (GameMatchConfig.Slots[slotIndex].IsHuman) {
            GameMatchConfig.Slots[slotIndex].controllerType = GameMatchConfig.ControllerType.BotEasy;
        } else {
            GameMatchConfig.Slots[slotIndex].controllerType = GameMatchConfig.ControllerType.LocalHuman;
        }
        AtualizarSlot(slotIndex);
    }

    private void SelecionarTurnos(int turnos) {
        GameMatchConfig.MaxTurns = turnos;
        int[] opcoes = new int[] { 10, 15, 20, 30 };
        for (int i = 0; i < botoesTurnos.Count; i++) {
            bool ativo = (opcoes[i] == turnos);
            Image img = botoesTurnos[i].GetComponent<Image>();
            if (img != null) img.color = ativo ? corBotaoAtivo : corBotaoInativo;
            Text txt = botoesTurnos[i].GetComponentInChildren<Text>();
            if (txt != null) txt.color = ativo ? new Color(0.09f, 0.40f, 0.20f, 1f) : Color.white;
        }
    }

    private void AlternarMapa() {
        GameMatchConfig.SelectedMapScene = GameMatchConfig.ProximoMapa(GameMatchConfig.SelectedMapScene);
        int sugeridos = GameMatchConfig.TurnosSugeridos(GameMatchConfig.SelectedMapScene);
        if ((sugeridos < 20 && GameMatchConfig.MaxTurns > sugeridos) || (sugeridos >= 20 && GameMatchConfig.MaxTurns < sugeridos)) {
            SelecionarTurnos(sugeridos);
        }
        AtualizarBotaoMapa();
    }

    private Color CorDoMapa() {
        switch (GameMatchConfig.SelectedMapScene) {
            case "MapaRapido": return corBotaoHumano;
            case "IlhaOportunidades": return new Color(0.96f, 0.62f, 0.12f, 1f);
            default: return corBotaoAtivo;
        }
    }

    private void AtualizarBotaoMapa() {
        if (txtMapaStatus != null) {
            txtMapaStatus.text = $"MAPA: {GameMatchConfig.GetMapDisplayName(GameMatchConfig.SelectedMapScene)}";
            Image img = txtMapaStatus.transform.parent.GetComponent<Image>();
            if (img != null) img.color = CorDoMapa();
        }
    }

    private void AlternarPassAndPlay() {
        GameMatchConfig.PassAndPlayEnabled = !GameMatchConfig.PassAndPlayEnabled;
        if (txtPassAndPlayStatus != null) {
            txtPassAndPlayStatus.text = GameMatchConfig.PassAndPlayEnabled ? "PASS & PLAY: ATIVADO" : "PASS & PLAY: DESATIVADO";
            Image img = txtPassAndPlayStatus.transform.parent.GetComponent<Image>();
            if (img != null) img.color = GameMatchConfig.PassAndPlayEnabled ? corBotaoAtivo : corBotaoInativo;
        }
    }

    private void AtualizarSlot(int slotIndex) {
        var slot = GameMatchConfig.Slots[slotIndex];
        var ui = slotUI[slotIndex];

        if (ui.imgAvatarIcon != null) {
            ui.imgAvatarIcon.color = GameMatchConfig.GetAvatarColor(slot.avatarId);
        }
        if (ui.txtAvatarNome != null) {
            ui.txtAvatarNome.text = GameMatchConfig.GetAvatarDefaultName(slot.avatarId);
        }
        if (ui.inputNome != null) {
            ui.inputNome.text = slot.name;
        }
        if (ui.txtTipoControlador != null) {
            ui.txtTipoControlador.text = slot.IsHuman ? "HUMANO" : "BOT (CPU)";
        }
        if (ui.imgTipoFundo != null) {
            ui.imgTipoFundo.color = slot.IsHuman ? corBotaoHumano : corBotaoCPU;
        }
    }

    private void AtualizarTodaInterface() {
        for (int i = 0; i < 4; i++) {
            AtualizarSlot(i);
        }
        SelecionarTurnos(GameMatchConfig.MaxTurns);
        AtualizarBotaoMapa();
        if (txtPassAndPlayStatus != null) {
            txtPassAndPlayStatus.text = GameMatchConfig.PassAndPlayEnabled ? "PASS & PLAY: ATIVADO" : "PASS & PLAY: DESATIVADO";
            Image img = txtPassAndPlayStatus.transform.parent.GetComponent<Image>();
            if (img != null) img.color = GameMatchConfig.PassAndPlayEnabled ? corBotaoAtivo : corBotaoInativo;
        }
    }

    private void IniciarJogoLocal() {
        // Salvar configurações
        GameMatchConfig.IsOnlineMatch = false;
        GameMatchConfig.SaveToPrefs();

        // Limpar save anterior
        GameState.DeleteSaveGame();
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        string targetScene = string.IsNullOrEmpty(GameMatchConfig.SelectedMapScene) ? "MVP" : GameMatchConfig.SelectedMapScene;
        SceneManager.LoadScene(targetScene);
    }

    // =================== UTILITÁRIOS GRÁFICOS ===================
    private static Sprite roundedSprite;

    private static Sprite ObterSpriteArredondado(int radius) {
        if (roundedSprite != null) return roundedSprite;
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        float r = radius;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(0, Mathf.Abs(x - (size / 2f)) - ((size / 2f) - r));
                float dy = Mathf.Max(0, Mathf.Abs(y - (size / 2f)) - ((size / 2f) - r));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                cols[y * size + x] = (dist <= r) ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedSprite;
    }
}
