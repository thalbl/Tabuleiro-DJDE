using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameState {
    // GAME SETTINGS
    [SerializeField] protected int maxTurn;
    [SerializeField] protected int bonusStars;
    [SerializeField] protected bool halfwayThere;
    [SerializeField] protected bool hiddenBlocksPresent;
    [SerializeField] protected bool consolationCoins;
    [SerializeField] protected List<int> handicaps;
    [SerializeField] protected List<bool> minigames;

    // VISIBLE GAME DATA
    [SerializeField] protected int turnsCompleted;

    // Seed do GameRng da partida (mesma em todas as máquinas no online)
    [SerializeField] protected int rngSeed;
    public int GetSeed() => rngSeed;
    public void SetSeed(int s) => rngSeed = s;

    // INTERNAL GAME DATA
    [SerializeField] private string filename;
    [SerializeField] protected List<int> mostRecentFFAs;
    [SerializeField] protected List<int> mostRecent2v2s;
    [SerializeField] protected List<int> mostRecent1v3s;
    [SerializeField] protected List<int> mostRecentDuels;
    [SerializeField] protected List<int> mostRecentBattles;
    [SerializeField] protected List<int> hiddenBlocks;
    [SerializeField] protected int chanceTimeCount;
    [SerializeField] protected int mostRecentBattleMinigame;
    [SerializeField] protected int battleChance;
    [SerializeField] protected int suspendedForMG;

    // PLAYER STATES
    [SerializeField] protected PlayerState p1;
    [SerializeField] protected PlayerState p2;
    [SerializeField] protected PlayerState p3;
    [SerializeField] protected PlayerState p4;

    public int getCurrentTurn() => turnsCompleted;
    public int getMaxTurns() => maxTurn;

    public GameState(PlayerState p1, PlayerState p2, PlayerState p3, PlayerState p4, int maxTurn, int bonusStars,
                    bool halfwayThere, bool hiddenBlocksPresent, bool consolationCoins,
                    List<int> handicaps, List<bool> minigames) {
        this.maxTurn = maxTurn;
        this.bonusStars = bonusStars;
        this.halfwayThere = halfwayThere;
        this.hiddenBlocksPresent = hiddenBlocksPresent;
        this.consolationCoins = consolationCoins;
        this.handicaps = handicaps;
        this.minigames = minigames;
        this.turnsCompleted = 0;
        this.mostRecentFFAs = new List<int>();
        this.mostRecent2v2s = new List<int>();
        this.mostRecent1v3s = new List<int>();
        this.mostRecentDuels = new List<int>();
        this.mostRecentBattles = new List<int>();
        this.suspendedForMG = 0;
        if (hiddenBlocksPresent) {
            this.hiddenBlocks = new List<int>();
        }
        this.chanceTimeCount = 0;
        this.mostRecentBattleMinigame = -1;
        this.battleChance = 0;
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        this.p4 = p4;

        p1.setStars(this.handicaps[0]);
        p2.setStars(this.handicaps[1]);
        p3.setStars(this.handicaps[2]);
        p4.setStars(this.handicaps[3]);
    }

    public PlayerState[] GetStandings() {
        PlayerState[] states = new PlayerState[] { p1, p2, p3, p4 };
        List<PlayerState> standings = new List<PlayerState>();
        foreach (PlayerState state in states) {
            if (state.getPlacing() == 1) standings.Add(state);
        }
        foreach (PlayerState state in states) {
            if (state.getPlacing() == 2) standings.Add(state);
        }
        foreach (PlayerState state in states) {
            if (state.getPlacing() == 3) standings.Add(state);
        }
        foreach (PlayerState state in states) {
            if (state.getPlacing() == 4) standings.Add(state);
        }
        return standings.ToArray();
    }

    public PlayerState[] GetPlayers() {
        return new PlayerState[] { p1, p2, p3, p4 };
    }

    public int getTurnStatus() {
        if (turnsCompleted < 5 && maxTurn != 30) return 0;
        else if (turnsCompleted <= maxTurn / 2) return 1;
        else if (turnsCompleted > maxTurn / 2 && turnsCompleted < maxTurn - 5) return 2;
        else if (turnsCompleted == maxTurn - 1) return 4;
        else return 3;
    }

    public string getTurnText() {
        return $"Rodada {this.turnsCompleted + 1}/{this.maxTurn}";
    }

    // Save a cada turno: guarda de quem é a vez dentro da rodada (0..3) para retomar no jogador certo
    [SerializeField] protected int proximoJogador;
    public int GetProximoJogador() => proximoJogador;
    public void SetProximoJogador(int i) => proximoJogador = Mathf.Clamp(i, 0, 3);

    public int getTurnEvent() {
        if (turnsCompleted == 0) return 1;
        else if (turnsCompleted == maxTurn / 2 && halfwayThere) return 2;
        else if (turnsCompleted == maxTurn - 5) return 3;
        else if (turnsCompleted == maxTurn) return 4;
        else return 0;
    }

    public void turnCompleted() {
        turnsCompleted += 1;
    }

    public const string NomeArquivoSave = "caminho_sucesso_save.dat";
    private const string NomeArquivoSaveLegado = "mario_party_save.dat";
    private const string ChaveMigracaoFeita = "SaveLegadoMigrado";

    // Mapa da partida salva fica no próprio save (as PlayerPrefs mudam de lugar junto com empresa/produto)
    [SerializeField] protected string cenaMapa;

    public static string SaveFilePath {
        get {
            return Path.Combine(Application.persistentDataPath, NomeArquivoSave);
        }
    }

    // Onde o save podia estar antes da mudança de identidade do projeto
    private static IEnumerable<string> CaminhosSaveLegado() {
        yield return Path.Combine(Application.persistentDataPath, NomeArquivoSaveLegado);
        // PC: persistentDataPath = .../LocalLow/<empresa>/<produto>; antes era DefaultCompany/My project (1)
        DirectoryInfo raiz = Directory.GetParent(Application.persistentDataPath)?.Parent;
        if (raiz != null) {
            yield return Path.Combine(raiz.FullName, "DefaultCompany", "My project (1)", NomeArquivoSaveLegado);
        }
        yield return Path.Combine(Application.dataPath, "Data", NomeArquivoSaveLegado);
    }

    /// <summary>
    /// Copia (uma única vez) o save do nome/pasta antigos para o caminho atual.
    /// Roda uma vez só para que "Novo jogo" (que apaga o save) não traga o save antigo de volta.
    /// </summary>
    public static void MigrarSaveLegado() {
        if (PlayerPrefs.GetInt(ChaveMigracaoFeita, 0) == 1) return;
        try {
            if (!File.Exists(SaveFilePath)) {
                foreach (string antigo in CaminhosSaveLegado()) {
                    if (File.Exists(antigo)) {
                        Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
                        File.Copy(antigo, SaveFilePath);
                        Debug.Log($"[GameState] Save migrado de {antigo} para {SaveFilePath}");
                        break;
                    }
                }
            }
        } catch (System.Exception e) {
            Debug.LogWarning($"[GameState] Não foi possível migrar o save antigo: {e.Message}");
        }
        PlayerPrefs.SetInt(ChaveMigracaoFeita, 1);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ gravação segura
    // No celular o sistema pode matar o app (ou a bateria acabar) no meio da gravação. Por isso:
    // 1) o JSON vai inteiro para ".tmp" (com flush no disco); 2) o save atual vira ".bak";
    // 3) o ".tmp" assume o nome oficial. Em qualquer ponto há pelo menos uma cópia íntegra,
    // e a leitura tenta oficial → .tmp → .bak, aceitando só um save válido.
    private static string CaminhoTmp => SaveFilePath + ".tmp";
    private static string CaminhoBak => SaveFilePath + ".bak";

    /// <summary>Save utilizável: JSON completo, rodadas definidas e os 4 jogadores com profissão.</summary>
    private bool Valido() {
        if (maxTurn <= 0) return false;
        foreach (PlayerState p in new[] { p1, p2, p3, p4 }) {
            if (p == null || string.IsNullOrEmpty(p.GetProfissao())) return false;
        }
        return true;
    }

    private static GameState LerArquivo(string caminho) {
        try {
            if (!File.Exists(caminho)) return null;
            string json = File.ReadAllText(caminho);
            if (string.IsNullOrWhiteSpace(json)) return null;
            GameState g = JsonUtility.FromJson<GameState>(json);
            return g != null && g.Valido() ? g : null;
        } catch (System.Exception e) {
            Debug.LogWarning($"[GameState] Save ilegível em {caminho}: {e.Message}");
            return null;
        }
    }

    public static bool HasSaveGame() {
        return LoadGame() != null;
    }

    public static void DeleteSaveGame() {
        MigrarSaveLegado(); // marca a migração como feita antes de apagar
        foreach (string caminho in new[] { SaveFilePath, CaminhoTmp, CaminhoBak }) {
            try {
                if (File.Exists(caminho)) File.Delete(caminho);
            } catch (System.Exception e) {
                Debug.LogWarning($"[GameState] Erro ao deletar save em {caminho}: {e.Message}");
            }
        }
    }

    public string SaveGame() {
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(activeScene) && activeScene != "StartScene" && activeScene != "GameOverScene") {
            cenaMapa = activeScene;
            PlayerPrefs.SetString("SavedGame_MapScene", activeScene);
            PlayerPrefs.Save();
        }
        string filePath = SaveFilePath;
        try {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            byte[] dados = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(this));
            using (var fs = new FileStream(CaminhoTmp, FileMode.Create, FileAccess.Write, FileShare.None)) {
                fs.Write(dados, 0, dados.Length);
                fs.Flush(true); // garante que chegou ao disco antes de trocar os arquivos
            }

            if (File.Exists(filePath)) {
                // Só vira backup um save que ainda está bom (não sobrescreve um .bak bom com lixo)
                if (LerArquivo(filePath) != null) File.Copy(filePath, CaminhoBak, true);
                File.Delete(filePath);
            }
            File.Move(CaminhoTmp, filePath);
        } catch (System.Exception e) {
            // Disco cheio, permissão etc.: a partida continua; o último save íntegro permanece
            Debug.LogWarning($"[GameState] Não foi possível salvar em {filePath}: {e.Message}");
        }
        return filePath;
    }

    public static string GetSavedMapScene() {
        // Preferência: o mapa gravado no próprio save; depois PlayerPrefs; por fim o MVP
        GameState salvo = LoadGame();
        if (salvo != null && !string.IsNullOrEmpty(salvo.cenaMapa)) return salvo.cenaMapa;
        string scene = PlayerPrefs.GetString("SavedGame_MapScene", "MVP");
        return string.IsNullOrEmpty(scene) ? "MVP" : scene;
    }

    /// <summary>
    /// Carrega o save mais recente que estiver íntegro (oficial → .tmp completo → .bak).
    /// Nenhum íntegro = null ("sem save": o Continuar fica desabilitado em vez de travar).
    /// </summary>
    public static GameState LoadGame() {
        MigrarSaveLegado();
        foreach (string caminho in new[] { SaveFilePath, CaminhoTmp, CaminhoBak }) {
            GameState g = LerArquivo(caminho);
            if (g != null) {
                if (caminho != SaveFilePath) Debug.LogWarning($"[GameState] Save principal ausente ou corrompido; usando {Path.GetFileName(caminho)}.");
                return g;
            }
        }
        return null;
    }
}
