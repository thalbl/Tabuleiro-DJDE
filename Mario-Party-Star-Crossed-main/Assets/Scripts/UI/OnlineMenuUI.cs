using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Interface do Multiplayer Online: Menu de Conexão (Criar Sala / Entrar com Código)
/// e Sala de Espera (Lobby com Ready Check e preenchimento de vagas com bots).
/// </summary>
public class OnlineMenuUI : MonoBehaviour {

    // =================== CORES DO DESIGN ===================
    private Color corFundoOverlay = new Color(0.04f, 0.14f, 0.09f, 1f); // mesmo verde opaco do lobby local (TemaUI)
    private Color corCardFundo = new Color(0.09f, 0.23f, 0.17f, 0.96f); // verde do tema
    private Color corCardBorda = new Color(0.29f, 0.87f, 0.50f, 0.6f);
    private Color corDourado = new Color(0.98f, 0.75f, 0.14f, 1f);
    private Color corTextoClaro = new Color(0.92f, 0.97f, 1f, 1f);
    private Color corTextoSuave = new Color(0.65f, 0.77f, 0.71f, 1f);

    private Color corBotaoAzul = new Color(0.20f, 0.65f, 0.95f, 1f);
    private Color corBotaoVerde = new Color(0.29f, 0.87f, 0.50f, 1f);
    private Color corBotaoLaranja = new Color(0.95f, 0.55f, 0.15f, 1f);
    private Color corBotaoVermelho = new Color(0.85f, 0.25f, 0.25f, 1f);

    // =================== REFERÊNCIAS ===================
    private GameObject painelPrincipal;
    private GameObject painelSalaEspera;
    private Font fonteFredoka;

    // Elementos do Menu Inicial Online
    private InputField inputApelido;
    private InputField inputCodigoSala;

    // Elementos da Sala de Espera
    private Text txtCodigoSalaGrande;
    private Text txtStatusGeral;
    private Button btnIniciarPartida;
    private Button btnReadyCheck;
    private Text txtBtnReady;
    private Button btnMapaOnline;
    private Text txtMapaOnline;

    private struct SlotOnlineUI {
        public GameObject slotObj;
        public Text txtSlotNumero;
        public Text txtNomeJogador;
        public Image imgAvatar;
        public Text txtStatusSlot;
        public Button btnAdicionarBot;
    }

    private SlotOnlineUI[] slotsOnline = new SlotOnlineUI[4];
    private bool isHost = false;
    private bool isReady = false;
    private string codigoSalaAtual = "";

    // Modais de Feedback, Timeout e Alerta
    private GameObject modalConectando;
    private Text txtModalConectando;
    private GameObject modalAlerta;
    private Text txtAlertaTitulo;
    private Text txtAlertaMensagem;
    private Button btnAlertaAcao;
    private Text txtBtnAlerta;
    private System.Action onAlertaAcaoCallback;

    public void Inicializar(Transform parentTransform) {
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GarantirTransporteRede();

        CriarPainelPrincipal(parentTransform);
        CriarPainelSalaEspera(parentTransform);
        CriarModaisFeedback(parentTransform);

        painelPrincipal.SetActive(false);
        painelSalaEspera.SetActive(false);
    }

    private void GarantirTransporteRede() {
        if (NetworkTransportService.Instance == null) {
            GameObject netObj = new GameObject("NetworkTransportService");
            netObj.AddComponent<NetworkTransportService>();
        }
    }

    void OnDestroy() {
        DesregistrarEventosRede();
    }

    public void Mostrar() {
        if (painelPrincipal != null) {
            painelPrincipal.SetActive(true);
            painelSalaEspera.SetActive(false);
        }
    }

    public void Ocultar() {
        OcultarModalConectando();
        FecharModalAlerta();
        if (painelPrincipal != null) painelPrincipal.SetActive(false);
        if (painelSalaEspera != null) painelSalaEspera.SetActive(false);
    }

    // =================== TELA 1: MENU DE CONEXÃO ===================
    private void CriarPainelPrincipal(Transform parent) {
        painelPrincipal = new GameObject("PainelOnlineMenu");
        painelPrincipal.transform.SetParent(parent, false);
        Expandir(painelPrincipal.AddComponent<RectTransform>());

        Image imgFundo = painelPrincipal.AddComponent<Image>();
        imgFundo.color = corFundoOverlay;

        GameObject container = new GameObject("ContainerOnline");
        container.transform.SetParent(painelPrincipal.transform, false);
        RectTransform rtC = container.AddComponent<RectTransform>();
        rtC.anchorMin = new Vector2(0.5f, 0.5f);
        rtC.anchorMax = new Vector2(0.5f, 0.5f);
        rtC.sizeDelta = new Vector2(1100, 750);
        rtC.anchoredPosition = Vector2.zero;

        // Título
        CriarTexto(container.transform, "TxtTitulo", "MULTIPLAYER ONLINE", 44, corDourado, new Vector2(0.5f, 0.92f), new Vector2(900, 50), FontStyle.Bold);
        CriarTexto(container.transform, "TxtSub", "Jogue com amigos em qualquer lugar através de um código de sala", 22, corTextoSuave, new Vector2(0.5f, 0.85f), new Vector2(900, 30), FontStyle.Normal);

        // Campo Apelido
        CriarCampoApelido(container.transform);

        // Cards: Criar Sala vs Entrar em Sala
        CriarCardCriarSala(container.transform);
        CriarCardEntrarSala(container.transform);

        // Botão Voltar
        CriarBotaoEstilizado(container.transform, "BtnVoltar", "VOLTAR AO MENU", new Vector2(0.5f, 0.08f), new Vector2(280, 55), corBotaoVermelho, () => Ocultar());
    }

    private void CriarCampoApelido(Transform parent) {
        GameObject obj = new GameObject("ContainerApelido");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.74f);
        rt.anchorMax = new Vector2(0.5f, 0.74f);
        rt.sizeDelta = new Vector2(500, 50);
        rt.anchoredPosition = Vector2.zero;

        CriarTexto(obj.transform, "Lbl", "Seu Apelido:", 22, corTextoClaro, new Vector2(0.2f, 0.5f), new Vector2(160, 40), FontStyle.Bold);

        GameObject inputObj = new GameObject("InputApelido");
        inputObj.transform.SetParent(obj.transform, false);
        RectTransform rtIn = inputObj.AddComponent<RectTransform>();
        rtIn.anchorMin = new Vector2(0.65f, 0.5f);
        rtIn.anchorMax = new Vector2(0.65f, 0.5f);
        rtIn.sizeDelta = new Vector2(260, 42);
        rtIn.anchoredPosition = Vector2.zero;

        Image img = inputObj.AddComponent<Image>();
        img.color = new Color(0.04f, 0.12f, 0.18f, 1f);
        img.sprite = ObterSpriteArredondado(8);
        img.type = Image.Type.Sliced;

        inputApelido = inputObj.AddComponent<InputField>();
        Text txt = CriarTextoInterno(inputObj.transform, "Jogador 1", 20, Color.white);
        inputApelido.textComponent = txt;
        inputApelido.text = PlayerPrefs.GetString("Online_PlayerName", "Jogador 1");
        inputApelido.onValueChanged.AddListener((v) => PlayerPrefs.SetString("Online_PlayerName", v));
    }

    private void CriarCardCriarSala(Transform parent) {
        GameObject card = new GameObject("CardCriar");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.28f, 0.44f);
        rt.anchorMax = new Vector2(0.28f, 0.44f);
        rt.sizeDelta = new Vector2(420, 320);
        rt.anchoredPosition = Vector2.zero;

        Image img = card.AddComponent<Image>();
        img.color = corCardFundo;
        img.sprite = ObterSpriteArredondado(16);
        img.type = Image.Type.Sliced;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = corCardBorda;
        outline.effectDistance = new Vector2(2, -2);

        CriarTexto(card.transform, "T", "CRIAR NOVA SALA", 28, corDourado, new Vector2(0.5f, 0.82f), new Vector2(380, 40), FontStyle.Bold);
        CriarTexto(card.transform, "D", "Gera um código exclusivo para compartilhar com seus amigos.", 18, corTextoSuave, new Vector2(0.5f, 0.60f), new Vector2(360, 50), FontStyle.Normal);

        CriarBotaoEstilizado(card.transform, "BtnCriar", "CRIAR SALA", new Vector2(0.5f, 0.25f), new Vector2(320, 60), corBotaoVerde, () => ExecutarCriarSala());
    }

    private void CriarCardEntrarSala(Transform parent) {
        GameObject card = new GameObject("CardEntrar");
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.72f, 0.44f);
        rt.anchorMax = new Vector2(0.72f, 0.44f);
        rt.sizeDelta = new Vector2(420, 320);
        rt.anchoredPosition = Vector2.zero;

        Image img = card.AddComponent<Image>();
        img.color = corCardFundo;
        img.sprite = ObterSpriteArredondado(16);
        img.type = Image.Type.Sliced;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = corCardBorda;
        outline.effectDistance = new Vector2(2, -2);

        CriarTexto(card.transform, "T", "ENTRAR EM UMA SALA", 28, corBotaoAzul, new Vector2(0.5f, 0.82f), new Vector2(380, 40), FontStyle.Bold);

        // Input de Código
        GameObject inputObj = new GameObject("InputCodigo");
        inputObj.transform.SetParent(card.transform, false);
        RectTransform rtIn = inputObj.AddComponent<RectTransform>();
        rtIn.anchorMin = new Vector2(0.5f, 0.58f);
        rtIn.anchorMax = new Vector2(0.5f, 0.58f);
        rtIn.sizeDelta = new Vector2(280, 48);
        rtIn.anchoredPosition = Vector2.zero;

        Image imgIn = inputObj.AddComponent<Image>();
        imgIn.color = new Color(0.04f, 0.12f, 0.18f, 1f);
        imgIn.sprite = ObterSpriteArredondado(8);
        imgIn.type = Image.Type.Sliced;

        inputCodigoSala = inputObj.AddComponent<InputField>();
        Text txt = CriarTextoInterno(inputObj.transform, "IP OU CÓDIGO (127.0.0.1)", 18, Color.gray);
        inputCodigoSala.textComponent = txt;
        inputCodigoSala.characterLimit = 32;
        inputCodigoSala.text = "127.0.0.1";
        inputCodigoSala.onValueChanged.AddListener((v) => inputCodigoSala.text = v.Trim());

        CriarBotaoEstilizado(card.transform, "BtnEntrar", "CONECTAR", new Vector2(0.5f, 0.25f), new Vector2(320, 60), corBotaoAzul, () => ExecutarEntrarSala());
    }

    // =================== TELA 2: SALA DE ESPERA (LOBBY ROOM) ===================
    private void CriarPainelSalaEspera(Transform parent) {
        painelSalaEspera = new GameObject("PainelSalaEspera");
        painelSalaEspera.transform.SetParent(parent, false);
        Expandir(painelSalaEspera.AddComponent<RectTransform>());

        Image imgFundo = painelSalaEspera.AddComponent<Image>();
        imgFundo.color = corFundoOverlay;

        GameObject container = new GameObject("ContainerLobby");
        container.transform.SetParent(painelSalaEspera.transform, false);
        RectTransform rtC = container.AddComponent<RectTransform>();
        rtC.anchorMin = new Vector2(0.5f, 0.5f);
        rtC.anchorMax = new Vector2(0.5f, 0.5f);
        rtC.sizeDelta = new Vector2(1360, 820);
        rtC.anchoredPosition = Vector2.zero;

        // Cabeçalho da Sala
        CriarCabecalhoSala(container.transform);

        // Grid com os 4 Slots dos Jogadores
        CriarSlotsOnline(container.transform);

        // Painel Inferior de Status e Ações
        CriarPainelAcoesSala(container.transform);
    }

    private void CriarCabecalhoSala(Transform parent) {
        GameObject header = new GameObject("HeaderSala");
        header.transform.SetParent(parent, false);
        RectTransform rt = header.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.90f);
        rt.anchorMax = new Vector2(0.5f, 0.90f);
        rt.sizeDelta = new Vector2(1200, 100);
        rt.anchoredPosition = Vector2.zero;

        CriarTexto(header.transform, "LblSala", "SALA DE ESPERA ONLINE", 32, corTextoClaro, new Vector2(0.5f, 0.75f), new Vector2(600, 40), FontStyle.Bold);

        // Código da Sala com destaque
        GameObject codObj = new GameObject("CodigoContainer");
        codObj.transform.SetParent(header.transform, false);
        RectTransform rtCod = codObj.AddComponent<RectTransform>();
        rtCod.anchorMin = new Vector2(0.5f, 0.25f);
        rtCod.anchorMax = new Vector2(0.5f, 0.25f);
        rtCod.sizeDelta = new Vector2(400, 44);
        rtCod.anchoredPosition = Vector2.zero;

        Image imgCod = codObj.AddComponent<Image>();
        imgCod.color = new Color(0.12f, 0.32f, 0.45f, 1f);
        imgCod.sprite = ObterSpriteArredondado(8);
        imgCod.type = Image.Type.Sliced;

        txtCodigoSalaGrande = CriarTextoInterno(codObj.transform, "CÓDIGO: CARREGANDO...", 22, corDourado);

        // Botão Seletor de Mapa (Host alterna, Clientes visualizam)
        btnMapaOnline = CriarBotaoEstilizado(header.transform, "BtnMapaOnline", $"MAPA: {GameMatchConfig.GetMapDisplayName(GameMatchConfig.SelectedMapScene)}", new Vector2(0.18f, 0.25f), new Vector2(320, 40), corBotaoVerde, () => AlternarMapaOnline());
        txtMapaOnline = btnMapaOnline.GetComponentInChildren<Text>();

        // Botão Copiar
        CriarBotaoEstilizado(header.transform, "BtnCopiar", "COPIAR", new Vector2(0.72f, 0.25f), new Vector2(110, 40), corBotaoAzul, () => CopiarCodigo());
    }

    private void CriarSlotsOnline(Transform parent) {
        GameObject slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(parent, false);
        RectTransform rt = slotsContainer.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.55f);
        rt.anchorMax = new Vector2(0.5f, 0.55f);
        rt.sizeDelta = new Vector2(1300, 360);
        rt.anchoredPosition = Vector2.zero;

        float width = 300f;
        float spacing = 26f;
        float startX = -((4 * width + 3 * spacing) / 2f) + (width / 2f);

        for (int i = 0; i < 4; i++) {
            float posX = startX + i * (width + spacing);
            CriarSlotOnline(slotsContainer.transform, i, posX);
        }
    }

    private void CriarSlotOnline(Transform parent, int slotIndex, float posX) {
        GameObject slotObj = new GameObject($"SlotOnline_{slotIndex + 1}");
        slotObj.transform.SetParent(parent, false);
        RectTransform rt = slotObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 340);
        rt.anchoredPosition = new Vector2(posX, 0);

        Image img = slotObj.AddComponent<Image>();
        img.color = corCardFundo;
        img.sprite = ObterSpriteArredondado(14);
        img.type = Image.Type.Sliced;

        Outline outline = slotObj.AddComponent<Outline>();
        outline.effectColor = corCardBorda;
        outline.effectDistance = new Vector2(2, -2);

        // Badge Slot
        Text txtNum = CriarTexto(slotObj.transform, "Badge", $"SLOT {slotIndex + 1}", 18, corTextoSuave, new Vector2(0.5f, 0.90f), new Vector2(200, 30), FontStyle.Bold);

        // Avatar
        GameObject avObj = new GameObject("Avatar");
        avObj.transform.SetParent(slotObj.transform, false);
        RectTransform rtAv = avObj.AddComponent<RectTransform>();
        rtAv.anchorMin = new Vector2(0.5f, 0.65f);
        rtAv.anchorMax = new Vector2(0.5f, 0.65f);
        rtAv.sizeDelta = new Vector2(100, 100);
        rtAv.anchoredPosition = Vector2.zero;

        Image imgAv = avObj.AddComponent<Image>();
        imgAv.sprite = ObterSpriteArredondado(50);
        imgAv.color = GameMatchConfig.GetAvatarColor(slotIndex + 1);

        // Nome
        Text txtNome = CriarTexto(slotObj.transform, "Nome", slotIndex == 0 ? "Host (Você)" : "Aguardando...", 22, corTextoClaro, new Vector2(0.5f, 0.42f), new Vector2(260, 30), FontStyle.Bold);

        // Status
        Text txtStatus = CriarTexto(slotObj.transform, "Status", slotIndex == 0 ? "PRONTO" : "VAGO", 18, slotIndex == 0 ? corBotaoVerde : Color.gray, new Vector2(0.5f, 0.28f), new Vector2(260, 30), FontStyle.Bold);

        // Botão Adicionar Bot (visível apenas para o Host em slots 2 a 4)
        int localIdx = slotIndex;
        Button btnBot = CriarBotaoEstilizado(slotObj.transform, "BtnBot", "+ ADICIONAR BOT", new Vector2(0.5f, 0.12f), new Vector2(220, 42), corBotaoLaranja, () => AdicionarBotNoSlot(localIdx));
        btnBot.gameObject.SetActive(slotIndex > 0);

        slotsOnline[slotIndex] = new SlotOnlineUI {
            slotObj = slotObj,
            txtSlotNumero = txtNum,
            txtNomeJogador = txtNome,
            imgAvatar = imgAv,
            txtStatusSlot = txtStatus,
            btnAdicionarBot = btnBot
        };
    }

    private void CriarPainelAcoesSala(Transform parent) {
        GameObject painel = new GameObject("PainelAcoes");
        painel.transform.SetParent(parent, false);
        RectTransform rt = painel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.12f);
        rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.sizeDelta = new Vector2(1200, 80);
        rt.anchoredPosition = Vector2.zero;

        // Botão Sair da Sala
        CriarBotaoEstilizado(painel.transform, "BtnSair", "SAIR DA SALA", new Vector2(0.18f, 0.5f), new Vector2(240, 60), corBotaoVermelho, () => SairDaSala());

        // Botão Ready Check (Para Clientes)
        btnReadyCheck = CriarBotaoEstilizado(painel.transform, "BtnReady", "ESTOU PRONTO!", new Vector2(0.50f, 0.5f), new Vector2(280, 60), corBotaoAzul, () => AlternarReady());
        txtBtnReady = btnReadyCheck.GetComponentInChildren<Text>();

        // Botão Iniciar Partida (Para o Host)
        btnIniciarPartida = CriarBotaoEstilizado(painel.transform, "BtnIniciar", "INICIAR PARTIDA ONLINE", new Vector2(0.82f, 0.5f), new Vector2(340, 60), corBotaoVerde, () => IniciarPartidaOnline());
    }

    // =================== LÓGICA E EVENTOS DE REDE ===================
    private void RegistrarEventosRede() {
        DesregistrarEventosRede();

        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnConectado += HandleOnConectado;
            NetworkTransportService.Instance.OnDesconectado += HandleOnDesconectado;
            NetworkTransportService.Instance.OnFalhaConexao += HandleOnFalhaConexao;
            NetworkTransportService.Instance.OnDesconectadoComMotivo += HandleOnDesconectadoComMotivo;
            NetworkTransportService.Instance.OnClienteConectado += HandleClienteConectado;
            NetworkTransportService.Instance.OnClienteDesconectado += HandleClienteDesconectado;
            NetworkTransportService.Instance.OnPacoteRecebido += HandlePacoteRecebido;
        }
    }

    private void DesregistrarEventosRede() {
        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnConectado -= HandleOnConectado;
            NetworkTransportService.Instance.OnDesconectado -= HandleOnDesconectado;
            NetworkTransportService.Instance.OnFalhaConexao -= HandleOnFalhaConexao;
            NetworkTransportService.Instance.OnDesconectadoComMotivo -= HandleOnDesconectadoComMotivo;
            NetworkTransportService.Instance.OnClienteConectado -= HandleClienteConectado;
            NetworkTransportService.Instance.OnClienteDesconectado -= HandleClienteDesconectado;
            NetworkTransportService.Instance.OnPacoteRecebido -= HandlePacoteRecebido;
        }
    }

    private void ExecutarCriarSala() {
        GarantirTransporteRede();
        isHost = true;
        isReady = true;

        string apelido = inputApelido != null ? inputApelido.text.Trim() : "Host";
        if (string.IsNullOrEmpty(apelido)) apelido = "Host";

        // Gerar código de 5 caracteres amigáveis
        string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        codigoSalaAtual = "";
        for (int i = 0; i < 5; i++) {
            codigoSalaAtual += chars[Random.Range(0, chars.Length)];
        }

        GameMatchConfig.IsOnlineMatch = true;
        GameMatchConfig.RoomCode = codigoSalaAtual;
        GameMatchConfig.LocalPlayerIndex = 0;
        GameMatchConfig.Slots[0] = new GameMatchConfig.PlayerSlot {
            name = apelido,
            controllerType = GameMatchConfig.ControllerType.LocalHuman,
            avatarId = 1,
            isReady = true
        };

        // Resetar os outros slots para aguardando
        for (int i = 1; i < 4; i++) {
            GameMatchConfig.Slots[i] = new GameMatchConfig.PlayerSlot {
                name = $"Aguardando...",
                controllerType = GameMatchConfig.ControllerType.RemoteNetwork,
                avatarId = i + 1,
                isReady = false
            };
        }

        RegistrarEventosRede();
        NetworkTransportService.Instance.IniciarHost(NetworkTransportService.DefaultPort);

        AbrirSalaEspera();
    }

    private void ExecutarEntrarSala() {
        string input = inputCodigoSala != null ? inputCodigoSala.text.Trim() : "";
        string hostOuIp = input;

        // Se o usuário deixou vazio ou digitou LOCAL/LOCALHOST, conecta localmente
        if (string.IsNullOrEmpty(hostOuIp) || hostOuIp.Equals("LOCAL", System.StringComparison.OrdinalIgnoreCase)) {
            hostOuIp = "127.0.0.1";
        }

        isHost = false;
        isReady = false;
        codigoSalaAtual = hostOuIp;

        string apelido = inputApelido != null ? inputApelido.text.Trim() : "Jogador";
        if (string.IsNullOrEmpty(apelido)) apelido = "Jogador";

        GarantirTransporteRede();
        RegistrarEventosRede();

        GameMatchConfig.IsOnlineMatch = true;
        GameMatchConfig.RoomCode = codigoSalaAtual;

        // Limpa slots prévios até receber dados do Host
        for (int i = 0; i < 4; i++) {
            GameMatchConfig.Slots[i] = new GameMatchConfig.PlayerSlot {
                name = "Aguardando Host...",
                controllerType = GameMatchConfig.ControllerType.RemoteNetwork,
                avatarId = i + 1,
                isReady = false
            };
        }

        // Exibe modal com feedback de tentativa e opção de cancelar
        ExibirModalConectando($"Conectando a {hostOuIp}:7777...\nAguardando resposta do servidor.");

        NetworkTransportService.Instance.ConectarComoCliente(hostOuIp, NetworkTransportService.DefaultPort, 4000);
    }

    private void HandleOnConectado() {
        OcultarModalConectando();

        if (!isHost) {
            AbrirSalaEspera();

            string apelido = inputApelido != null ? inputApelido.text.Trim() : "Jogador";
            if (string.IsNullOrEmpty(apelido)) apelido = "Jogador";

            NetworkProtocol.NetworkPacket joinPkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyJoinRequest, -1);
            joinPkt.stringPayload = apelido;
            NetworkTransportService.Instance.EnviarPacote(joinPkt);

            if (txtCodigoSalaGrande != null) {
                txtCodigoSalaGrande.text = $"CONECTADO! AGUARDANDO ATRIBUIÇÃO DE SLOT...";
            }
        }
        AtualizarSlotsOnline();
    }

    private void HandleOnDesconectado() {
        OcultarModalConectando();
    }

    private void HandleOnFalhaConexao(string motivo) {
        OcultarModalConectando();
        ExibirModalAlerta("FALHA NA CONEXÃO", motivo, "ENTENDIDO", null);
    }

    private void HandleOnDesconectadoComMotivo(string motivo) {
        OcultarModalConectando();
        if (painelSalaEspera != null && painelSalaEspera.activeSelf && !isHost) {
            ExibirModalAlerta("CONEXÃO ENCERRADA", motivo, "VOLTAR AO MENU", () => SairDaSala());
        }
    }

    private void HandleClienteConectado(int slot, string nome) {
        if (!isHost) return;

        GameMatchConfig.Slots[slot].name = string.IsNullOrEmpty(nome) ? $"Jogador {slot + 1}" : nome;
        GameMatchConfig.Slots[slot].controllerType = GameMatchConfig.ControllerType.RemoteNetwork;
        GameMatchConfig.Slots[slot].isReady = false;

        // Envia resposta para o cliente recém-conectado com seu slot e o estado atual do lobby
        NetworkProtocol.LobbyStatePayload payload = NetworkProtocol.LobbyStatePayload.CriarDeConfig();
        string json = JsonUtility.ToJson(payload);

        NetworkProtocol.NetworkPacket respPkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyJoinResponse, 0);
        respPkt.intPayload = slot;
        respPkt.stringPayload = json;
        NetworkTransportService.Instance.EnviarPacote(respPkt);

        // Notifica todos com os slots atualizados
        BroadcastLobbyState();
        AtualizarSlotsOnline();
    }

    private void HandleClienteDesconectado(int slot) {
        if (!isHost) return;

        GameMatchConfig.Slots[slot].name = "Aguardando...";
        GameMatchConfig.Slots[slot].controllerType = GameMatchConfig.ControllerType.RemoteNetwork;
        GameMatchConfig.Slots[slot].isReady = false;

        BroadcastLobbyState();
        AtualizarSlotsOnline();
    }

    private void HandlePacoteRecebido(NetworkProtocol.NetworkPacket packet) {
        if (packet == null) return;

        switch (packet.type) {
            case NetworkProtocol.MessageType.LobbyError:
                OcultarModalConectando();
                ExibirModalAlerta("ACESSO NEGADO", packet.stringPayload, "VOLTAR AO MENU", () => SairDaSala());
                break;

            case NetworkProtocol.MessageType.LobbyJoinRequest:
                if (isHost) {
                    int slotRemetente = packet.senderSlot;
                    if (slotRemetente >= 1 && slotRemetente < 4) {
                        if (!string.IsNullOrEmpty(packet.stringPayload)) {
                            GameMatchConfig.Slots[slotRemetente].name = packet.stringPayload;
                        }
                        BroadcastLobbyState();
                        AtualizarSlotsOnline();
                    }
                }
                break;

            case NetworkProtocol.MessageType.LobbyJoinResponse:
                if (!isHost) {
                    int meuSlot = packet.intPayload;
                    NetworkTransportService.Instance.LocalSlotIndex = meuSlot;
                    GameMatchConfig.LocalPlayerIndex = meuSlot;

                    if (!string.IsNullOrEmpty(packet.stringPayload)) {
                        NetworkProtocol.LobbyStatePayload payload = JsonUtility.FromJson<NetworkProtocol.LobbyStatePayload>(packet.stringPayload);
                        if (payload != null) {
                            payload.AplicarParaConfig(meuSlot);
                        }
                    }
                    if (txtCodigoSalaGrande != null) {
                        txtCodigoSalaGrande.text = $"CONECTADO NO SLOT {meuSlot + 1}!";
                    }
                    AtualizarSlotsOnline();
                }
                break;

            case NetworkProtocol.MessageType.LobbySlotUpdate:
                if (!isHost) {
                    if (!string.IsNullOrEmpty(packet.stringPayload)) {
                        NetworkProtocol.LobbyStatePayload payload = JsonUtility.FromJson<NetworkProtocol.LobbyStatePayload>(packet.stringPayload);
                        if (payload != null) {
                            int meuSlot = NetworkTransportService.Instance.LocalSlotIndex;
                            payload.AplicarParaConfig(meuSlot);
                            AtualizarSlotsOnline();
                        }
                    }
                }
                break;

            case NetworkProtocol.MessageType.LobbyReadyToggle:
                if (isHost) {
                    int slot = packet.senderSlot;
                    if (slot >= 1 && slot < 4) {
                        GameMatchConfig.Slots[slot].isReady = packet.boolPayload;
                        BroadcastLobbyState();
                        AtualizarSlotsOnline();
                    }
                }
                break;

            case NetworkProtocol.MessageType.LobbyStartGame:
                if (!isHost) {
                    if (!string.IsNullOrEmpty(packet.stringPayload)) {
                        NetworkProtocol.LobbyStatePayload payload = JsonUtility.FromJson<NetworkProtocol.LobbyStatePayload>(packet.stringPayload);
                        if (payload != null) {
                            int meuSlot = NetworkTransportService.Instance.LocalSlotIndex;
                            payload.AplicarParaConfig(meuSlot);
                        }
                    }
                    GameMatchConfig.SeedPartida = packet.intPayload;
                    IniciarTransiçãoCenaMVP();
                }
                break;
        }
    }

    private void BroadcastLobbyState() {
        if (!isHost || NetworkTransportService.Instance == null) return;
        NetworkProtocol.LobbyStatePayload payload = NetworkProtocol.LobbyStatePayload.CriarDeConfig();
        string json = JsonUtility.ToJson(payload);

        NetworkProtocol.NetworkPacket pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbySlotUpdate, 0);
        pkt.stringPayload = json;
        NetworkTransportService.Instance.EnviarPacote(pkt);
    }

    private void AbrirSalaEspera() {
        painelPrincipal.SetActive(false);
        painelSalaEspera.SetActive(true);

        string ipLocal = NetworkTransportService.ObterIPLocal();
        if (txtCodigoSalaGrande != null) {
            if (isHost) {
                txtCodigoSalaGrande.text = $"IP HOST: {ipLocal}:7777  •  CÓDIGO: {codigoSalaAtual}";
            } else {
                txtCodigoSalaGrande.text = $"CONECTADO AO HOST: {codigoSalaAtual}";
            }
        }

        btnIniciarPartida.gameObject.SetActive(isHost);
        btnReadyCheck.gameObject.SetActive(!isHost);

        AtualizarSlotsOnline();
    }

    // =================== MODAIS DE FEEDBACK E DIÁLOGOS ===================

    private void CriarModaisFeedback(Transform parent) {
        // 1. Modal Conectando (Overlay com Spinner/Texto e Botão Cancelar)
        modalConectando = new GameObject("ModalConectando");
        modalConectando.transform.SetParent(parent, false);
        Expandir(modalConectando.AddComponent<RectTransform>());

        Image imgOverlayConn = modalConectando.AddComponent<Image>();
        imgOverlayConn.color = new Color(0.02f, 0.06f, 0.10f, 0.82f);

        GameObject cardConn = new GameObject("CardConectando");
        cardConn.transform.SetParent(modalConectando.transform, false);
        RectTransform rtConn = cardConn.AddComponent<RectTransform>();
        rtConn.anchorMin = new Vector2(0.5f, 0.5f);
        rtConn.anchorMax = new Vector2(0.5f, 0.5f);
        rtConn.sizeDelta = new Vector2(580, 260);

        Image imgCardConn = cardConn.AddComponent<Image>();
        imgCardConn.color = corCardFundo;
        imgCardConn.sprite = ObterSpriteArredondado(16);
        imgCardConn.type = Image.Type.Sliced;

        Outline outConn = cardConn.AddComponent<Outline>();
        outConn.effectColor = corBotaoAzul;
        outConn.effectDistance = new Vector2(2, -2);

        CriarTexto(cardConn.transform, "Titulo", "CONECTANDO...", 28, corDourado, new Vector2(0.5f, 0.78f), new Vector2(520, 40), FontStyle.Bold);
        txtModalConectando = CriarTexto(cardConn.transform, "Status", "Aguardando resposta do Host...", 20, corTextoClaro, new Vector2(0.5f, 0.52f), new Vector2(520, 50), FontStyle.Normal);

        CriarBotaoEstilizado(cardConn.transform, "BtnCancelar", "CANCELAR", new Vector2(0.5f, 0.22f), new Vector2(200, 48), corBotaoVermelho, () => CancelarTentativaConexao());

        modalConectando.SetActive(false);

        // 2. Modal Alerta / Erro (Exibição amigável para recusa, timeout, sala cheia)
        modalAlerta = new GameObject("ModalAlerta");
        modalAlerta.transform.SetParent(parent, false);
        Expandir(modalAlerta.AddComponent<RectTransform>());

        Image imgOverlayAlert = modalAlerta.AddComponent<Image>();
        imgOverlayAlert.color = new Color(0.02f, 0.06f, 0.10f, 0.88f);

        GameObject cardAlert = new GameObject("CardAlerta");
        cardAlert.transform.SetParent(modalAlerta.transform, false);
        RectTransform rtAlert = cardAlert.AddComponent<RectTransform>();
        rtAlert.anchorMin = new Vector2(0.5f, 0.5f);
        rtAlert.anchorMax = new Vector2(0.5f, 0.5f);
        rtAlert.sizeDelta = new Vector2(680, 360);

        Image imgCardAlert = cardAlert.AddComponent<Image>();
        imgCardAlert.color = corCardFundo;
        imgCardAlert.sprite = ObterSpriteArredondado(16);
        imgCardAlert.type = Image.Type.Sliced;

        Outline outAlert = cardAlert.AddComponent<Outline>();
        outAlert.effectColor = corBotaoLaranja;
        outAlert.effectDistance = new Vector2(2, -2);

        txtAlertaTitulo = CriarTexto(cardAlert.transform, "Titulo", "AVISO", 28, corDourado, new Vector2(0.5f, 0.82f), new Vector2(620, 45), FontStyle.Bold);
        txtAlertaMensagem = CriarTexto(cardAlert.transform, "Mensagem", "Mensagem de alerta", 19, corTextoClaro, new Vector2(0.5f, 0.52f), new Vector2(620, 130), FontStyle.Normal);

        btnAlertaAcao = CriarBotaoEstilizado(cardAlert.transform, "BtnAcao", "ENTENDIDO", new Vector2(0.5f, 0.18f), new Vector2(240, 52), corBotaoAzul, () => FecharModalAlerta());
        txtBtnAlerta = btnAlertaAcao.GetComponentInChildren<Text>();

        modalAlerta.SetActive(false);
    }

    public void ExibirModalConectando(string mensagem) {
        if (modalConectando != null) {
            if (txtModalConectando != null) txtModalConectando.text = mensagem;
            modalConectando.SetActive(true);
            modalConectando.transform.SetAsLastSibling();
        }
    }

    public void OcultarModalConectando() {
        if (modalConectando != null) modalConectando.SetActive(false);
    }

    public void CancelarTentativaConexao() {
        OcultarModalConectando();
        DesregistrarEventosRede();
        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.DesconectarTudo();
        }
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelSalaEspera != null) painelSalaEspera.SetActive(false);
    }

    public void ExibirModalAlerta(string titulo, string mensagem, string textoBotao = "ENTENDIDO", System.Action onAcao = null) {
        OcultarModalConectando();
        onAlertaAcaoCallback = onAcao;

        if (modalAlerta != null) {
            if (txtAlertaTitulo != null) txtAlertaTitulo.text = titulo;
            if (txtAlertaMensagem != null) txtAlertaMensagem.text = mensagem;
            if (txtBtnAlerta != null) txtBtnAlerta.text = textoBotao;
            modalAlerta.SetActive(true);
            modalAlerta.transform.SetAsLastSibling();
        }
    }

    public void FecharModalAlerta() {
        if (modalAlerta != null) modalAlerta.SetActive(false);
        System.Action cb = onAlertaAcaoCallback;
        onAlertaAcaoCallback = null;
        cb?.Invoke();
    }

    private void AtualizarSlotsOnline() {
        for (int i = 0; i < 4; i++) {
            var slot = GameMatchConfig.Slots[i];
            var ui = slotsOnline[i];

            ui.txtNomeJogador.text = slot.name;
            ui.imgAvatar.color = GameMatchConfig.GetAvatarColor(slot.avatarId);

            if ( slot.controllerType == GameMatchConfig.ControllerType.BotEasy) {
                ui.txtStatusSlot.text = "BOT (CPU)";
                ui.txtStatusSlot.color = corBotaoLaranja;
                ui.btnAdicionarBot.GetComponentInChildren<Text>().text = "REMOVER BOT";
                ui.btnAdicionarBot.gameObject.SetActive(isHost);
            } else if (slot.isReady) {
                ui.txtStatusSlot.text = "PRONTO";
                ui.txtStatusSlot.color = corBotaoVerde;
                ui.btnAdicionarBot.gameObject.SetActive(false);
            } else if (slot.controllerType == GameMatchConfig.ControllerType.LocalHuman && i == 0) {
                ui.txtStatusSlot.text = "HOST (PRONTO)";
                ui.txtStatusSlot.color = corBotaoVerde;
                ui.btnAdicionarBot.gameObject.SetActive(false);
            } else {
                ui.txtStatusSlot.text = "AGUARDANDO...";
                ui.txtStatusSlot.color = Color.gray;
                ui.btnAdicionarBot.gameObject.SetActive(isHost);
                ui.btnAdicionarBot.GetComponentInChildren<Text>().text = "+ ADICIONAR BOT";
            }
        }

        if (txtMapaOnline != null) {
            txtMapaOnline.text = $"MAPA: {GameMatchConfig.GetMapDisplayName(GameMatchConfig.SelectedMapScene)}";
        }
        if (btnMapaOnline != null) {
            btnMapaOnline.interactable = isHost;
        }
    }

    private void AlternarMapaOnline() {
        if (!isHost) return;
        GameMatchConfig.SelectedMapScene = (GameMatchConfig.SelectedMapScene == "MapaRapido") ? "MVP" : "MapaRapido";
        if (GameMatchConfig.SelectedMapScene == "MapaRapido" && GameMatchConfig.MaxTurns > 15) {
            GameMatchConfig.MaxTurns = 15;
        } else if (GameMatchConfig.SelectedMapScene == "MVP" && GameMatchConfig.MaxTurns < 20) {
            GameMatchConfig.MaxTurns = 20;
        }
        BroadcastLobbyState();
        AtualizarSlotsOnline();
    }

    private void AdicionarBotNoSlot(int slotIndex) {
        if (!isHost) return;

        if (GameMatchConfig.Slots[slotIndex].controllerType == GameMatchConfig.ControllerType.BotEasy) {
            // Remove bot
            GameMatchConfig.Slots[slotIndex].name = "Aguardando...";
            GameMatchConfig.Slots[slotIndex].controllerType = GameMatchConfig.ControllerType.RemoteNetwork;
            GameMatchConfig.Slots[slotIndex].isReady = false;
        } else {
            // Adiciona bot
            GameMatchConfig.Slots[slotIndex].name = $"Bot {GameMatchConfig.GetAvatarDefaultName(slotIndex + 1)}";
            GameMatchConfig.Slots[slotIndex].controllerType = GameMatchConfig.ControllerType.BotEasy;
            GameMatchConfig.Slots[slotIndex].isReady = true;
        }

        BroadcastLobbyState();
        AtualizarSlotsOnline();
    }

    private void AlternarReady() {
        isReady = !isReady;
        if (txtBtnReady != null) {
            txtBtnReady.text = isReady ? "CANCELAR PRONTO" : "ESTOU PRONTO!";
            btnReadyCheck.GetComponent<Image>().color = isReady ? corBotaoVerde : corBotaoAzul;
        }

        if (!isHost && NetworkTransportService.Instance != null) {
            NetworkProtocol.NetworkPacket pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyReadyToggle, NetworkTransportService.Instance.LocalSlotIndex);
            pkt.boolPayload = isReady;
            NetworkTransportService.Instance.EnviarPacote(pkt);
        }
    }

    private void IniciarPartidaOnline() {
        // Preenche vagas restantes com bots caso o host inicie com menos de 4 humanos
        for (int i = 1; i < 4; i++) {
            if (!GameMatchConfig.Slots[i].isReady && GameMatchConfig.Slots[i].controllerType == GameMatchConfig.ControllerType.RemoteNetwork) {
                GameMatchConfig.Slots[i].name = $"Bot {GameMatchConfig.GetAvatarDefaultName(i + 1)}";
                GameMatchConfig.Slots[i].controllerType = GameMatchConfig.ControllerType.BotEasy;
                GameMatchConfig.Slots[i].isReady = true;
            }
        }

        // Seed única da partida: todas as máquinas sorteiam igual (lockstep)
        GameMatchConfig.SeedPartida = GameRng.NovaSeed();

        // Host avisa todos os clientes para iniciarem a partida
        if (NetworkTransportService.Instance != null) {
            NetworkProtocol.LobbyStatePayload payload = NetworkProtocol.LobbyStatePayload.CriarDeConfig();
            string json = JsonUtility.ToJson(payload);

            NetworkProtocol.NetworkPacket pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyStartGame, 0);
            pkt.stringPayload = json;
            pkt.intPayload = GameMatchConfig.SeedPartida;
            NetworkTransportService.Instance.EnviarPacote(pkt);
        }

        IniciarTransiçãoCenaMVP();
    }

    private void IniciarTransiçãoCenaMVP() {
        GameMatchConfig.SaveToPrefs();

        // Limpa save anterior
        GameState.DeleteSaveGame();
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        string targetScene = string.IsNullOrEmpty(GameMatchConfig.SelectedMapScene) ? "MVP" : GameMatchConfig.SelectedMapScene;
        SceneManager.LoadScene(targetScene);
    }

    private void SairDaSala() {
        if (isHost && NetworkTransportService.Instance != null && NetworkTransportService.Instance.IsHost) {
            try {
                NetworkProtocol.NetworkPacket pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyError, 0);
                pkt.stringPayload = "O Host encerrou a sala.";
                NetworkTransportService.Instance.EnviarPacote(pkt);
            } catch { }
        }

        DesregistrarEventosRede();
        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.DesconectarTudo();
        }
        painelSalaEspera.SetActive(false);
        painelPrincipal.SetActive(true);
    }

    private void CopiarCodigo() {
        string ipLocal = NetworkTransportService.ObterIPLocal();
        string textoParaCopiar = isHost ? $"{ipLocal}:7777" : codigoSalaAtual;
        GUIUtility.systemCopyBuffer = textoParaCopiar;
        if (txtCodigoSalaGrande != null) {
            txtCodigoSalaGrande.text = $"COPIADO: {textoParaCopiar}!";
        }
    }

    // =================== FACTORIES UI ===================
    private Text CriarTexto(Transform parent, string nome, string conteudo, int tamanho, Color cor, Vector2 anchor, Vector2 size, FontStyle estilo) {
        GameObject txtObj = new GameObject(nome);
        txtObj.transform.SetParent(parent, false);
        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonteFredoka;
        txt.fontSize = tamanho;
        txt.fontStyle = estilo;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = cor;
        // Fredoka tem entrelinha alta: sem Overflow, uma linha que "não cabe" na caixa simplesmente não aparece
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        if (tamanho >= 40) TemaUI.ContornoTexto(txt, TemaUI.SombraVerde, 3f);
        return txt;
    }

    private Text CriarTextoInterno(Transform parent, string conteudo, int tamanho, Color cor) {
        GameObject txtObj = new GameObject("Texto");
        txtObj.transform.SetParent(parent, false);
        Expandir(txtObj.AddComponent<RectTransform>());

        Text txt = txtObj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonteFredoka;
        txt.fontSize = tamanho;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = cor;
        return txt;
    }

    private Button CriarBotaoEstilizado(Transform parent, string nome, string texto, Vector2 anchor, Vector2 size, Color cor, UnityEngine.Events.UnityAction onClick) {
        GameObject btnObj = new GameObject(nome);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = ObterSpriteArredondado(12);
        img.type = Image.Type.Sliced;
        img.color = cor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = cor;
        fx.corHover = corDourado;
        fx.escalaHover = 1.05f;

        CriarTextoInterno(btnObj.transform, texto, 20, Color.white);
        return btn;
    }

    private void Expandir(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

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
