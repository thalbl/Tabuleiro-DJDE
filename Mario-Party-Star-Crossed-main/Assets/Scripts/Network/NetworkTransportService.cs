using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço de Transporte de Rede Real (TCP Sockets) para o Multiplayer Online.
/// Suporta conexão direta Host-Client via LAN/WiFi, Hamachi/ZeroTier ou IP Público.
/// Persiste entre cenas (DontDestroyOnLoad) e despacha mensagens com thread-safety na Main Thread.
/// </summary>
public class NetworkTransportService : MonoBehaviour {

    public static NetworkTransportService Instance { get; private set; }

    public const int DefaultPort = 7777;

    public enum ConnectionState {
        Disconnected,
        Hosting,
        Connecting,
        Connected
    }

    [Header("Status de Conexão")]
    public ConnectionState State = ConnectionState.Disconnected;
    public bool IsHost => State == ConnectionState.Hosting;
    public bool IsConnected => State == ConnectionState.Connected || State == ConnectionState.Hosting;
    public int LocalSlotIndex = 0; // Host é 0, Clientes são 1, 2 ou 3

    // Eventos de Notificação
    public event Action OnConectado;
    public event Action OnDesconectado;
    public event Action<string> OnFalhaConexao;          // (motivoDetalhado)
    public event Action<string> OnDesconectadoComMotivo; // (motivoDetalhado)
    public event Action<NetworkProtocol.NetworkPacket> OnPacoteRecebido;
    public event Action<int, string> OnClienteConectado; // (slot, nome)
    public event Action<int> OnClienteDesconectado;       // (slot)

    // Estruturas de Servidor (Host)
    private TcpListener tcpListener;
    private Thread hostListenerThread;
    private readonly Dictionary<int, ClientConnection> clientesConectados = new Dictionary<int, ClientConnection>();

    // Estruturas de Cliente
    private TcpClient clientSocket;
    private Thread clientReceiveThread;
    private NetworkStream clientStream;

    // Fila segura de mensagens para a thread principal da Unity
    private readonly ConcurrentQueue<NetworkProtocol.NetworkPacket> filaRecebimento = new ConcurrentQueue<NetworkProtocol.NetworkPacket>();
    private readonly ConcurrentQueue<Action> filaAcoesMainThread = new ConcurrentQueue<Action>();

    private class ClientConnection {
        public int slotIndex;
        public TcpClient socket;
        public NetworkStream stream;
        public Thread threadLeitura;
        public string nomeJogador = "Convidado";
    }

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update() {
        // Despacha ações pendentes na Main Thread da Unity
        while (filaAcoesMainThread.TryDequeue(out Action action)) {
            try {
                action?.Invoke();
            } catch (Exception e) {
                Debug.LogError($"[NetworkTransport] Erro ao executar ação main thread: {e.Message}");
            }
        }

        // Despacha pacotes recebidos
        while (filaRecebimento.TryDequeue(out NetworkProtocol.NetworkPacket packet)) {
            try {
                OnPacoteRecebido?.Invoke(packet);
            } catch (Exception e) {
                Debug.LogError($"[NetworkTransport] Erro ao processar pacote {packet.type}: {e.Message}");
            }
        }
    }

    void OnDestroy() {
        DesconectarTudo();
    }

    void OnApplicationQuit() {
        DesconectarTudo();
    }

    // =================== MODO HOST (CRIAR SALA) ===================

    public bool IniciarHost(int port = DefaultPort) {
        DesconectarTudo();

        try {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            State = ConnectionState.Hosting;
            LocalSlotIndex = 0;

            hostListenerThread = new Thread(LoopAceitarClientes) {
                IsBackground = true,
                Name = "HostListenerThread"
            };
            hostListenerThread.Start();

            Debug.Log($"[NetworkTransport] Servidor Host iniciado com sucesso na porta {port}. IP Local: {ObterIPLocal()}");
            filaAcoesMainThread.Enqueue(() => OnConectado?.Invoke());
            return true;
        } catch (Exception e) {
            Debug.LogError($"[NetworkTransport] Falha ao iniciar Host: {e.Message}");
            State = ConnectionState.Disconnected;
            return false;
        }
    }

    private void LoopAceitarClientes() {
        int proximoSlot = 1;

        while (State == ConnectionState.Hosting && tcpListener != null) {
            try {
                TcpClient novoCliente = tcpListener.AcceptTcpClient();
                novoCliente.NoDelay = true;

                if (proximoSlot > 3) {
                    Debug.LogWarning("[NetworkTransport] Sala cheia! Conexão rejeitada.");
                    try {
                        NetworkProtocol.NetworkPacket errPkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.LobbyError, 0);
                        errPkt.stringPayload = "A sala já está cheia (máximo de 4 jogadores atingido)!";
                        byte[] errBytes = Encoding.UTF8.GetBytes(errPkt.ToJson() + "\n");
                        novoCliente.GetStream().Write(errBytes, 0, errBytes.Length);
                        novoCliente.GetStream().Flush();
                    } catch { }
                    Thread.Sleep(100);
                    novoCliente.Close();
                    continue;
                }

                int slotAtribuido = proximoSlot++;
                ClientConnection conn = new ClientConnection {
                    slotIndex = slotAtribuido,
                    socket = novoCliente,
                    stream = novoCliente.GetStream()
                };

                lock (clientesConectados) {
                    clientesConectados[slotAtribuido] = conn;
                }

                conn.threadLeitura = new Thread(() => LoopLeituraCliente(conn)) {
                    IsBackground = true,
                    Name = $"ClientReader_{slotAtribuido}"
                };
                conn.threadLeitura.Start();

                Debug.Log($"[NetworkTransport] Novo cliente conectado no Slot {slotAtribuido}!");
                filaAcoesMainThread.Enqueue(() => OnClienteConectado?.Invoke(slotAtribuido, conn.nomeJogador));
            } catch (SocketException) {
                // Listener parado normalmente
                break;
            } catch (Exception e) {
                Debug.LogWarning($"[NetworkTransport] Listener finalizado: {e.Message}");
                break;
            }
        }
    }

    private void LoopLeituraCliente(ClientConnection conn) {
        byte[] buffer = new byte[8192];
        StringBuilder acumulador = new StringBuilder();

        while (State == ConnectionState.Hosting && conn.socket != null && conn.socket.Connected) {
            try {
                int bytesLidos = conn.stream.Read(buffer, 0, buffer.Length);
                if (bytesLidos <= 0) break; // Desconectou

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                acumulador.Append(chunk);

                ProcessarMensagensAcumuladas(acumulador, (packet) => {
                    packet.senderSlot = conn.slotIndex;
                    filaRecebimento.Enqueue(packet);

                    // Host retransmite para os outros clientes se necessário (Host-Authoritative Broadcast)
                    BroadcastParaOutros(packet, conn.slotIndex);
                });
            } catch (Exception) {
                break;
            }
        }

        // Trata desconexão do cliente
        lock (clientesConectados) {
            clientesConectados.Remove(conn.slotIndex);
        }
        try { conn.stream?.Close(); } catch { }
        try { conn.socket?.Close(); } catch { }

        filaAcoesMainThread.Enqueue(() => OnClienteDesconectado?.Invoke(conn.slotIndex));
    }

    // =================== MODO CLIENTE (ENTRAR NA SALA) ===================

    public void ConectarComoCliente(string hostOrIp, int port = DefaultPort, int timeoutMs = 4000) {
        DesconectarTudo();
        State = ConnectionState.Connecting;

        ThreadPool.QueueUserWorkItem(_ => {
            string ipAlvo = string.IsNullOrEmpty(hostOrIp) ? "127.0.0.1" : hostOrIp.Trim();
            try {
                clientSocket = new TcpClient();
                clientSocket.NoDelay = true;

                // Conexão assíncrona com timeout configurável (evita travar a UI por 20 segundos)
                IAsyncResult asyncResult = clientSocket.BeginConnect(ipAlvo, port, null, null);
                bool conectou = asyncResult.AsyncWaitHandle.WaitOne(timeoutMs);

                if (!conectou) {
                    try { clientSocket.Close(); } catch { }
                    throw new TimeoutException($"Tempo limite de {timeoutMs / 1000}s esgotado ao tentar alcançar o servidor.");
                }

                clientSocket.EndConnect(asyncResult);
                clientStream = clientSocket.GetStream();

                State = ConnectionState.Connected;
                Debug.Log($"[NetworkTransport] Conectado com sucesso ao Host {ipAlvo}:{port}!");

                clientReceiveThread = new Thread(LoopLeituraHost) {
                    IsBackground = true,
                    Name = "ClientReceiveThread"
                };
                clientReceiveThread.Start();

                filaAcoesMainThread.Enqueue(() => OnConectado?.Invoke());
            } catch (SocketException se) {
                string motivo;
                switch (se.SocketErrorCode) {
                    case SocketError.ConnectionRefused:
                        motivo = $"Conexão recusada em {ipAlvo}:{port}.\nNenhuma sala aberta neste endereço. O Host precisa clicar em 'CRIAR SALA' primeiro!";
                        break;
                    case SocketError.TimedOut:
                        motivo = $"Tempo limite esgotado ao tentar alcançar {ipAlvo}:{port}.\nVerifique se o Host está aberto e se ambos estão na mesma rede/Wi-Fi.";
                        break;
                    case SocketError.HostNotFound:
                    case SocketError.HostUnreachable:
                        motivo = $"O endereço '{ipAlvo}' não foi encontrado ou está inalcançável.";
                        break;
                    case SocketError.NetworkUnreachable:
                        motivo = "Rede inalcançável. Verifique sua conexão de rede ou Wi-Fi.";
                        break;
                    default:
                        motivo = $"Não foi possível conectar ({se.SocketErrorCode}): {se.Message}";
                        break;
                }

                Debug.LogWarning($"[NetworkTransport] {motivo}");
                State = ConnectionState.Disconnected;
                DesconectarTudo();
                filaAcoesMainThread.Enqueue(() => {
                    OnFalhaConexao?.Invoke(motivo);
                    OnDesconectadoComMotivo?.Invoke(motivo);
                    OnDesconectado?.Invoke();
                });
            } catch (TimeoutException te) {
                string motivo = $"Tempo limite esgotado ({timeoutMs / 1000}s).\nO Host em {ipAlvo}:{port} não respondeu. Verifique se o Host já iniciou a sala.";
                Debug.LogWarning($"[NetworkTransport] {motivo}");
                State = ConnectionState.Disconnected;
                DesconectarTudo();
                filaAcoesMainThread.Enqueue(() => {
                    OnFalhaConexao?.Invoke(motivo);
                    OnDesconectadoComMotivo?.Invoke(motivo);
                    OnDesconectado?.Invoke();
                });
            } catch (Exception e) {
                string motivo = $"Erro ao tentar conectar: {e.Message}";
                Debug.LogWarning($"[NetworkTransport] {motivo}");
                State = ConnectionState.Disconnected;
                DesconectarTudo();
                filaAcoesMainThread.Enqueue(() => {
                    OnFalhaConexao?.Invoke(motivo);
                    OnDesconectadoComMotivo?.Invoke(motivo);
                    OnDesconectado?.Invoke();
                });
            }
        });
    }

    private void LoopLeituraHost() {
        byte[] buffer = new byte[8192];
        StringBuilder acumulador = new StringBuilder();
        string motivoDesconexao = "A conexão com o Host foi encerrada.";

        while (State == ConnectionState.Connected && clientSocket != null && clientSocket.Connected) {
            try {
                int bytesLidos = clientStream.Read(buffer, 0, buffer.Length);
                if (bytesLidos <= 0) break;

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesLidos);
                acumulador.Append(chunk);

                ProcessarMensagensAcumuladas(acumulador, (packet) => {
                    if (packet.type == NetworkProtocol.MessageType.LobbyError) {
                        motivoDesconexao = packet.stringPayload;
                    }
                    filaRecebimento.Enqueue(packet);
                });
            } catch (Exception) {
                break;
            }
        }

        State = ConnectionState.Disconnected;
        filaAcoesMainThread.Enqueue(() => {
            OnDesconectadoComMotivo?.Invoke(motivoDesconexao);
            OnDesconectado?.Invoke();
        });
    }

    // =================== ENVIO E TRANSMISSÃO ===================

    public void EnviarPacote(NetworkProtocol.NetworkPacket packet) {
        if (packet == null) return;
        packet.senderSlot = LocalSlotIndex;
        string json = packet.ToJson() + "\n"; // Delimitador de nova linha
        byte[] dados = Encoding.UTF8.GetBytes(json);

        if (IsHost) {
            // Host processa localmente e envia para todos os clientes
            filaRecebimento.Enqueue(packet);
            Broadcast(packet);
        } else if (State == ConnectionState.Connected && clientStream != null) {
            try {
                clientStream.Write(dados, 0, dados.Length);
                clientStream.Flush();
            } catch (Exception e) {
                Debug.LogError($"[NetworkTransport] Erro ao enviar pacote para host: {e.Message}");
            }
        }
    }

    public void Broadcast(NetworkProtocol.NetworkPacket packet) {
        string json = packet.ToJson() + "\n";
        byte[] dados = Encoding.UTF8.GetBytes(json);

        lock (clientesConectados) {
            foreach (var kvp in clientesConectados) {
                try {
                    kvp.Value.stream.Write(dados, 0, dados.Length);
                    kvp.Value.stream.Flush();
                } catch { }
            }
        }
    }

    private void BroadcastParaOutros(NetworkProtocol.NetworkPacket packet, int remetenteSlot) {
        string json = packet.ToJson() + "\n";
        byte[] dados = Encoding.UTF8.GetBytes(json);

        lock (clientesConectados) {
            foreach (var kvp in clientesConectados) {
                if (kvp.Key == remetenteSlot) continue;
                try {
                    kvp.Value.stream.Write(dados, 0, dados.Length);
                    kvp.Value.stream.Flush();
                } catch { }
            }
        }
    }

    private void ProcessarMensagensAcumuladas(StringBuilder acumulador, Action<NetworkProtocol.NetworkPacket> onPacket) {
        string texto = acumulador.ToString();
        int newlineIdx;

        while ((newlineIdx = texto.IndexOf('\n')) >= 0) {
            string linha = texto.Substring(0, newlineIdx).Trim();
            texto = texto.Substring(newlineIdx + 1);

            if (!string.IsNullOrEmpty(linha)) {
                NetworkProtocol.NetworkPacket packet = NetworkProtocol.NetworkPacket.FromJson(linha);
                if (packet != null) {
                    onPacket(packet);
                }
            }
        }

        acumulador.Clear();
        acumulador.Append(texto);
    }

    // =================== UTILITÁRIOS E DESCONEXÃO ===================

    public void DesconectarTudo() {
        State = ConnectionState.Disconnected;

        try { tcpListener?.Stop(); } catch { }
        tcpListener = null;

        lock (clientesConectados) {
            foreach (var c in clientesConectados.Values) {
                try { c.stream?.Close(); } catch { }
                try { c.socket?.Close(); } catch { }
            }
            clientesConectados.Clear();
        }

        try { clientStream?.Close(); } catch { }
        try { clientSocket?.Close(); } catch { }
        clientStream = null;
        clientSocket = null;
    }

    public static string ObterIPLocal() {
        try {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)) {
                    return ip.ToString();
                }
            }
        } catch { }
        return "127.0.0.1";
    }
}
