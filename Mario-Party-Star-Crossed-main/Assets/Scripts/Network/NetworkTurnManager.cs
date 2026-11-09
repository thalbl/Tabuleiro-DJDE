using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador de rede e sincronização de turnos para o Multiplayer Online.
/// Arquitetura Host-Authoritative: O Host possui a autoridade das regras e estado do jogo,
/// enquanto os Clientes enviam intenções e recebem as atualizações sincronizadas.
/// </summary>
public class NetworkTurnManager : MonoBehaviour {

    public static NetworkTurnManager Instance { get; private set; }

    [Header("Configurações Online e Timer")]
    [Tooltip("Tempo limite em segundos para o jogador realizar sua ação antes da IA assumir.")]
    public float timeoutTurnoSegundos = 45f;
    [Tooltip("Habilita o mostrador e o limite de tempo também em partidas locais.")]
    public bool habilitarTimerLocal = true;

    private float timerTurnoAtual = 0f;
    private bool turnoAtivo = false;
    private int jogadorAtivoIndex = 0;

    public float TimeoutTurnoSegundos => timeoutTurnoSegundos;
    public float TimerTurnoAtual => timerTurnoAtual;
    public float TempoRestante => Mathf.Max(0f, timeoutTurnoSegundos - timerTurnoAtual);
    public float ProgressoTimer => timeoutTurnoSegundos > 0 ? Mathf.Clamp01(1f - (timerTurnoAtual / timeoutTurnoSegundos)) : 1f;
    public bool IsTurnoAtivo => turnoAtivo;
    /// <summary>Pausa a contagem do turno (ex.: enquanto o jogador olha o tabuleiro).</summary>
    public bool TimerPausado { get; set; }
    public int JogadorAtivoIndex => jogadorAtivoIndex;
    public Player JogadorAtivoPlayer { get; private set; }

    [Header("Referências")]
    private BoardManager boardManager;
    private UIManager uiManager;

    // Callbacks e Eventos
    public event Action<int, int> OnDadoSincronizado; // (playerIndex, valorRolado)
    public event Action<int, string> OnOpcaoSincronizada; // (playerIndex, opcaoEscolhida)
    public event Action<int> OnJogadorDesconectado; // (playerIndex)

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        boardManager = FindObjectOfType<BoardManager>();
        uiManager = FindObjectOfType<UIManager>();

        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnPacoteRecebido += ProcessarPacoteRede;
            NetworkTransportService.Instance.OnClienteDesconectado += OnClienteRemotoDesconectou;
        }
    }

    void OnDestroy() {
        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnPacoteRecebido -= ProcessarPacoteRede;
            NetworkTransportService.Instance.OnClienteDesconectado -= OnClienteRemotoDesconectou;
        }
    }

    void Update() {
        if (!GameMatchConfig.IsOnlineMatch && !habilitarTimerLocal) return;

        // Gerenciamento de Timeout de Turno para partidas online ou com timer local ativo
        if (turnoAtivo && !TimerPausado) {
            // Bots não têm limite de tempo (o timeout no turno do bot quebrava a rolagem de múltiplos dados)
            if (JogadorAtivoPlayer != null && JogadorAtivoPlayer.state != null) {
                int ctrl = JogadorAtivoPlayer.state.getController();
                if (ctrl == 1 || ctrl == 2) return;
            }
            timerTurnoAtual += Time.deltaTime;
            if (timerTurnoAtual >= timeoutTurnoSegundos) {
                ExecutarTimeoutTurno();
            }
        }
    }

    /// <summary>
    /// Retorna o índice do slot desta máquina (0 para host, 1..3 para clientes).
    /// </summary>
    public int ObterMeuSlot() {
        if (NetworkTransportService.Instance != null) {
            return NetworkTransportService.Instance.LocalSlotIndex;
        }
        return GameMatchConfig.LocalPlayerIndex;
    }

    /// <summary>
    /// Notifica o início de turno para sincronização de rede e timer.
    /// </summary>
    public void NotificarInicioTurno(int playerIndex, Player player) {
        jogadorAtivoIndex = playerIndex;
        JogadorAtivoPlayer = player;
        timerTurnoAtual = 0f;
        turnoAtivo = true;

        if (!GameMatchConfig.IsOnlineMatch) return;

        bool souEu = (playerIndex == ObterMeuSlot());
        Debug.Log($"[NetworkTurnManager] Início de turno do Jogador {playerIndex + 1} ({player.state.charName()}). SouEu: {souEu}");
    }

    /// <summary>
    /// Retorna verdadeiro se o jogador local tem autorização para interagir (rolar dado, escolher opções, etc.).
    /// Em partida Local com humanos (ou Pass & Play), apenas o jogador ativo humano pode interagir.
    /// Em partida Online, apenas o cliente local dono do turno ativo pode interagir.
    /// Se for turno de Bot (IA), nenhum jogador local pode interagir (a máquina decide sozinha).
    /// </summary>
    public bool PodeJogadorLocalInteragir() {
        if (JogadorAtivoPlayer == null) return false;
        int ctrl = JogadorAtivoPlayer.state.getController();

        // Se for Bot (IA), o jogador local não pode clicar
        if (ctrl == 1 || ctrl == 2) return false;

        // Se for partida Online: apenas o dono do turno atual pode clicar
        if (GameMatchConfig.IsOnlineMatch) {
            int meuSlot = ObterMeuSlot();
            return (jogadorAtivoIndex == meuSlot);
        }

        // Partida Local (Pass & Play ou Todos no mesmo aparelho):
        // Se o jogador ativo for humano (ctrl == 0), ele pode interagir
        return (ctrl == 0);
    }

    /// <summary>
    /// Notifica que o dado foi rolado pelo jogador ativo para sincronização.
    /// </summary>
    public void NotificarDadoRolado(int valor) {
        OnDadoSincronizado?.Invoke(jogadorAtivoIndex, valor);

        if (GameMatchConfig.IsOnlineMatch && NetworkTransportService.Instance != null) {
            NetworkProtocol.NetworkPacket packet = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.GameDiceRoll, ObterMeuSlot());
            packet.intPayload = valor;
            NetworkTransportService.Instance.EnviarPacote(packet);
        }
    }

    /// <summary>
    /// Notifica que uma opção de diálogo (compra de estrela, bifurcação, menu de itens) foi escolhida.
    /// </summary>
    public void NotificarOpcaoEscolhida(string opcao) {
        OnOpcaoSincronizada?.Invoke(jogadorAtivoIndex, opcao);

        if (GameMatchConfig.IsOnlineMatch && NetworkTransportService.Instance != null) {
            NetworkProtocol.NetworkPacket packet = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.GameOptionChoice, ObterMeuSlot());
            packet.stringPayload = opcao;
            NetworkTransportService.Instance.EnviarPacote(packet);
        }
    }

    /// <summary>
    /// Processa pacotes de gameplay recebidos via transporte de rede.
    /// </summary>
    private void ProcessarPacoteRede(NetworkProtocol.NetworkPacket packet) {
        if (packet == null) return;
        int meuSlot = ObterMeuSlot();

        switch (packet.type) {
            case NetworkProtocol.MessageType.GameDiceRoll:
                if (packet.senderSlot != meuSlot) {
                    Debug.Log($"[NetworkTurnManager] Dado remoto recebido do Jogador {packet.senderSlot + 1}: {packet.intPayload}");
                    ReceberAcaoRemota("dado", packet.intPayload.ToString());
                }
                break;

            case NetworkProtocol.MessageType.GameOptionChoice:
                if (packet.senderSlot != meuSlot) {
                    Debug.Log($"[NetworkTurnManager] Opção remota recebida do Jogador {packet.senderSlot + 1}: {packet.stringPayload}");
                    ReceberAcaoRemota("opcao", packet.stringPayload);
                }
                break;

            case NetworkProtocol.MessageType.GameTimeout:
                if (packet.senderSlot != meuSlot) {
                    ExecutarTimeoutTurno();
                }
                break;

            case NetworkProtocol.MessageType.GamePlayerDisconnect:
                TratarDesconexaoJogador(packet.senderSlot);
                break;

            case NetworkProtocol.MessageType.GameChecksum:
                if (packet.senderSlot != meuSlot && int.TryParse(packet.stringPayload, out int rodadaRemota)) {
                    CompararChecksum(rodadaRemota, packet.intPayload, packet.senderSlot);
                }
                break;
        }
    }

    private void OnClienteRemotoDesconectou(int slot) {
        TratarDesconexaoJogador(slot);
    }

    /// <summary>
    /// Recebe e aplica uma ação realizada por outro jogador remoto na partida online.
    /// </summary>
    public void ReceberAcaoRemota(string tipo, string parametro) {
        if (tipo == "dado") {
            if (int.TryParse(parametro, out int roll)) {
                DiceBlock dado = FindObjectOfType<DiceBlock>();
                if (dado != null) {
                    dado.ForcarRolagemRemota(roll);
                }
            }
        } else if (tipo == "opcao") {
            if (uiManager != null) {
                uiManager.ReceberEscolhaRemota(parametro);
            }
        }
    }

    /// <summary>
    /// Notifica o término do turno atual.
    /// </summary>
    public void NotificarFimTurno(int playerIndex) {
        turnoAtivo = false;
        timerTurnoAtual = 0f;
    }

    /// <summary>
    /// Chamado quando o tempo de ação do jogador expira no modo online.
    /// Transforma temporariamente o jogador em IA para executar a ação e não travar a partida.
    /// </summary>
    // ================== CHECKSUM DE ESTADO (online) ==================
    // Cada máquina calcula um hash do estado ao fim da rodada e envia; quem recebe compara com o seu.
    private readonly Dictionary<int, int> checksumsLocais = new Dictionary<int, int>();
    private readonly List<(int rodada, int hash, int slot)> checksumsPendentes = new List<(int, int, int)>();

    /// <summary>Verdadeiro se alguma rodada teve estado diferente entre as máquinas.</summary>
    public bool DessincroniaDetectada { get; private set; }
    public int RodadaDaDessincronia { get; private set; } = -1;

    public static int CalcularChecksum(GameState estado) {
        unchecked {
            int h = 17;
            h = h * 31 + estado.getCurrentTurn();
            foreach (PlayerState p in estado.GetPlayers()) {
                h = h * 31 + p.getCoins();
                h = h * 31 + p.getStars();
                h = h * 31 + p.getSpaceID();
                h = h * 31 + p.GetCareerLevel();
                h = h * 31 + p.GetEducationLevel();
                h = h * 31 + p.GetSalary();
                h = h * 31 + p.getItems().Count;
                foreach (BoardItem item in p.getItems()) h = h * 31 + (int)item;
            }
            return h;
        }
    }

    /// <summary>Chamado pelo BoardManager ao fim de cada rodada.</summary>
    public void RegistrarFimDeRodada(int rodada, GameState estado) {
        if (!GameMatchConfig.IsOnlineMatch) return;
        int hash = CalcularChecksum(estado);
        checksumsLocais[rodada] = hash;

        if (NetworkTransportService.Instance != null) {
            var pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.GameChecksum, ObterMeuSlot());
            pkt.intPayload = hash;
            pkt.stringPayload = rodada.ToString();
            NetworkTransportService.Instance.EnviarPacote(pkt);
        }

        // Compara os que chegaram antes do nosso cálculo
        for (int i = checksumsPendentes.Count - 1; i >= 0; i--) {
            if (checksumsPendentes[i].rodada == rodada) {
                var c = checksumsPendentes[i];
                checksumsPendentes.RemoveAt(i);
                CompararChecksum(c.rodada, c.hash, c.slot);
            }
        }
    }

    private void CompararChecksum(int rodada, int hashRemoto, int slotRemoto) {
        if (!checksumsLocais.TryGetValue(rodada, out int hashLocal)) {
            checksumsPendentes.Add((rodada, hashRemoto, slotRemoto));
            return;
        }
        if (hashLocal != hashRemoto && !DessincroniaDetectada) {
            DessincroniaDetectada = true;
            RodadaDaDessincronia = rodada;
            Debug.LogError($"[NetworkTurnManager] DESSINCRONIZAÇÃO na rodada {rodada}: local={hashLocal} jogador {slotRemoto + 1}={hashRemoto}. Seed={GameRng.Seed}");
        }
    }

    private void ExecutarTimeoutTurno() {
        turnoAtivo = false;
        Debug.LogWarning($"[NetworkTurnManager] Tempo limite de {timeoutTurnoSegundos}s atingido para o Jogador {jogadorAtivoIndex + 1}! Assumindo por IA.");

        // Responde o prompt atual em vez de substituí-lo por um novo diálogo
        // (substituir deixava a corrotina da casa/turno esperando uma resposta que nunca vinha)
        if (uiManager != null) {
            uiManager.ResponderPorTimeout();
        }

        // Rola TODOS os dados ativos (dado duplo/triplo têm vários DiceBlock)
        foreach (DiceBlock dado in FindObjectsByType<DiceBlock>(FindObjectsSortMode.None)) {
            dado.ForcarRolagemPorTimeout();
        }
    }

    /// <summary>
    /// Trata a queda ou desconexão de um jogador durante a partida online.
    /// Substitui o jogador por um Bot (EasyBrain) instantaneamente sem reiniciar a mesa.
    /// </summary>
    public void TratarDesconexaoJogador(int playerIndex) {
        if (boardManager == null) return;

        List<Player> jogadores = boardManager.GetPlayers();
        if (playerIndex >= 0 && playerIndex < jogadores.Count) {
            Player p = jogadores[playerIndex];
            if (p != null && p.state != null) {
                // Atualiza o slot para bot
                GameMatchConfig.Slots[playerIndex].controllerType = GameMatchConfig.ControllerType.BotEasy;

                // Atualiza o cérebro para EasyBrain
                List<PlayerState> rivais = new List<PlayerState>() {
                    boardManager.p1.state,
                    boardManager.p2.state,
                    boardManager.p3.state,
                    boardManager.p4.state
                };
                rivais.Remove(p.state);
                p.brain = new EasyBrain(p.state, rivais[0], rivais[1], rivais[2], boardManager);

                if (uiManager != null) {
                    uiManager.Dialogue("Jogador Desconectado", $"{p.state.charName()} foi desconectado. A IA assumirá o controle deste personagem.", true);
                }

                OnJogadorDesconectado?.Invoke(playerIndex);
            }
        }
    }

    /// <summary>
    /// Retorna o percentual restante do timer de turno (0 a 1).
    /// </summary>
    public float ObterProgressoTimerTurno() {
        if (!turnoAtivo || timeoutTurnoSegundos <= 0) return 0f;
        return Mathf.Clamp01(1f - (timerTurnoAtual / timeoutTurnoSegundos));
    }
}
