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
        if (turnoAtivo) {
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
    private void ExecutarTimeoutTurno() {
        turnoAtivo = false;
        Debug.LogWarning($"[NetworkTurnManager] Tempo limite de {timeoutTurnoSegundos}s atingido para o Jogador {jogadorAtivoIndex + 1}! Assumindo por IA.");

        if (uiManager != null) {
            uiManager.Dialogue("Tempo Esgotado", $"O jogador demorou para responder. A jogada foi realizada automaticamente!", true);
        }

        // Se houver dado ativo, força a rolagem
        DiceBlock dado = FindObjectOfType<DiceBlock>();
        if (dado != null) {
            dado.comTimer = 2.0f; // Força o disparo do dado
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
