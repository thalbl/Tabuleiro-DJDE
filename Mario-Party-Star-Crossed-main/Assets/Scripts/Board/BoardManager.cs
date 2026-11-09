using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class BoardManager : MonoBehaviour {
    [HideInInspector]
    public GameState state;
    public string fileToLoad;
    public Player p1;
    public Player p2;
    public Player p3;
    public Player p4;

    private int phase;
    private PlayerState[] players;
    private List<BoardSpace> registry;
    private int maxID;

    public string gameOverSceneName = "GameOverScene";
    private bool gameEnded = false;
    private const int MAX_CAREER_LEVEL = 10; // N�vel m�ximo da carreira

    private UIManager ui;
    private bool processingEndPhase = false;
    void Awake() {
        // Verifica se deve carregar save ou criar novo jogo
        bool loadSavedGame = PlayerPrefs.GetInt("LoadSavedGame", 0) == 1;
        GameState loadedState = null;

        if (loadSavedGame) {
            loadedState = GameState.LoadGame();
        }

        if (loadedState != null) {
            state = loadedState;
        }
        else {
            // Cria novo jogo com n�vel de carreira inicial
            // Cria novo jogo a partir das configurações definidas no Lobby
            GameMatchConfig.LoadFromPrefs();
            int turns = GameMatchConfig.MaxTurns > 0 ? GameMatchConfig.MaxTurns : 30;

            PlayerState ps1 = new PlayerState(GameMatchConfig.Slots[0].ControllerInt, GameMatchConfig.Slots[0].avatarId, GameMatchConfig.Slots[0].name, 1);
            PlayerState ps2 = new PlayerState(GameMatchConfig.Slots[1].ControllerInt, GameMatchConfig.Slots[1].avatarId, GameMatchConfig.Slots[1].name, 1);
            PlayerState ps3 = new PlayerState(GameMatchConfig.Slots[2].ControllerInt, GameMatchConfig.Slots[2].avatarId, GameMatchConfig.Slots[2].name, 1);
            PlayerState ps4 = new PlayerState(GameMatchConfig.Slots[3].ControllerInt, GameMatchConfig.Slots[3].avatarId, GameMatchConfig.Slots[3].name, 1);

            state = new GameState(
                ps1, ps2, ps3, ps4,
                turns, 0, false, false, false,
                new List<int>() { 0, 0, 0, 0 },
                new List<bool>() { false }
            );
            state.SaveGame();
        }

        registry = new List<BoardSpace>();
        foreach (BoardSpace space in GetComponentsInChildren<BoardSpace>()) {
            if (space.id > maxID) {
                maxID = space.id;
            }
            if (space.id != -1) {
                while (registry.Count <= space.id) {
                    registry.Add(null);
                }
                registry[space.id] = space;
            }
        }
    }

    void Start() {
        ui = FindObjectOfType<UIManager>();
        players = state.GetPlayers();
        p1.SetPlayer(players[0]);
        p2.SetPlayer(players[1]);
        p3.SetPlayer(players[2]);
        p4.SetPlayer(players[3]);

        // Inicializa gerenciadores de rede, emotes e HUD de timer se não existirem
        if (FindObjectOfType<NetworkTurnManager>() == null) {
            gameObject.AddComponent<NetworkTurnManager>();
        }
        if (FindObjectOfType<EmoteReactionUI>() == null) {
            gameObject.AddComponent<EmoteReactionUI>();
        }
        if (FindObjectOfType<TurnTimerUI>() == null) {
            gameObject.AddComponent<TurnTimerUI>();
        }
    }

    void Update() {
        if (gameEnded) return;

        // Atualiza colocação dos jogadores pela pontuação composta
        foreach (PlayerState pA in players) {
            pA.setPlacing(1);
            foreach (PlayerState pB in players) {
                if (pA != pB && pB.GetFinalScore() > pA.GetFinalScore()) {
                    pA.setPlacing(pA.getPlacing() + 1);
                }
            }
        }

        // Verifica se o jogo acabou
        if (state.getCurrentTurn() >= state.getMaxTurns() || CheckCareerLevelComplete()) {
            EndGame();
            return;
        }

        // Fases do jogo
        switch (phase) {
            case 0:
                // Evento no início do turno (sem despesas automáticas)
                switch (state.getTurnEvent()) {
                    case 1: /* Opening Ceremony */ break;
                    case 2: /* Halfway There */ break;
                    case 3: /* Last Five Turns */ break;
                    case 4: /* Closing Ceremony */ break;
                    default: break;
                }
                phase = 1;
                break;
            case 1:
                NetworkTurnManager.Instance?.NotificarInicioTurno(0, p1);
                p1.StartTurn();
                phase = 2;
                break;
            case 2:
                if (p1.TurnOver()) {
                    NetworkTurnManager.Instance?.NotificarFimTurno(0);
                    phase = 3;
                }
                break;
            case 3:
                NetworkTurnManager.Instance?.NotificarInicioTurno(1, p2);
                p2.StartTurn();
                phase = 4;
                break;
            case 4:
                if (p2.TurnOver()) {
                    NetworkTurnManager.Instance?.NotificarFimTurno(1);
                    phase = 5;
                }
                break;
            case 5:
                NetworkTurnManager.Instance?.NotificarInicioTurno(2, p3);
                p3.StartTurn();
                phase = 6;
                break;
            case 6:
                if (p3.TurnOver()) {
                    NetworkTurnManager.Instance?.NotificarFimTurno(2);
                    phase = 7;
                }
                break;
            case 7:
                NetworkTurnManager.Instance?.NotificarInicioTurno(3, p4);
                p4.StartTurn();
                phase = 8;
                break;
            case 8:
                if (p4.TurnOver()) {
                    NetworkTurnManager.Instance?.NotificarFimTurno(3);
                    phase = 9;
                }
                break;
            case 9:
                // Fase de minigame
                phase = 10;
                break;
            case 10:
                if (!processingEndPhase) {
                    processingEndPhase = true;
                    StartCoroutine(HandleEndOfRound());
                }
                break;
            default:
                phase = 0;
                break;
        }
    }

    private IEnumerator HandleEndOfRound() {
        // Garante uma câmera ativa durante os diálogos de fim de rodada
        Camera tempCam = null;
        bool hadActiveCamera = Camera.allCamerasCount > 0;
        if (!hadActiveCamera) {
            if (p1 != null) {
                tempCam = p1.GetComponentInChildren<Camera>(true);
            }
            if (tempCam == null && p2 != null) tempCam = p2.GetComponentInChildren<Camera>(true);
            if (tempCam == null && p3 != null) tempCam = p3.GetComponentInChildren<Camera>(true);
            if (tempCam == null && p4 != null) tempCam = p4.GetComponentInChildren<Camera>(true);
            if (tempCam != null) {
                tempCam.enabled = true;
            }
        }

		// Fim de rodada simplificado - sem despesas automáticas
		if (ui != null) {
			ui.Dialogue("Fim da Rodada", $"Rodada {state.getCurrentTurn()} concluída!", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        }

        foreach (PlayerState ps in players) {
            ps.setTeam(0);
        }
        state.turnCompleted();
        state.SaveGame();

        if (state.getCurrentTurn() >= state.getMaxTurns() || CheckCareerLevelComplete()) {
            EndGame();
        } else {
            processingEndPhase = false;
            phase = 0;
        }

        // Desliga câmera temporária, se usada
        if (!hadActiveCamera && tempCam != null) {
            tempCam.enabled = false;
        }
    }

    // Verifica se algum jogador atingiu o n�vel m�ximo da carreira
    private bool CheckCareerLevelComplete() {
        foreach (PlayerState player in players) {
            if (player.GetCareerLevel() >= MAX_CAREER_LEVEL) {
                return true;
            }
        }
        return false;
    }

    private void EndGame() {
        gameEnded = true;

        // Ordenar jogadores pela pontuação composta final
        List<PlayerState> rankedPlayers = new List<PlayerState>(players);
        rankedPlayers.Sort((a, b) => b.GetFinalScore().CompareTo(a.GetFinalScore()));

        // Determinar quantidade real de jogadores
        int activePlayers = 0;
        foreach (PlayerState player in players) {
            if (player != null) activePlayers++;
        }

        // Salvar quantidade de jogadores
        PlayerPrefs.SetInt("ActivePlayers", activePlayers);

        for (int i = 0; i < rankedPlayers.Count; i++) {
            int position = i + 1;
            PlayerState player = rankedPlayers[i];

            if (player != null) {
                PlayerPrefs.SetString($"Player{position}Name", player.charName());
                PlayerPrefs.SetInt($"Player{position}Money", player.getCoins());
                PlayerPrefs.SetString($"Player{position}Career", player.GetFormattedCareerLevel());
                PlayerPrefs.SetInt($"Player{position}EducationLevel", player.GetEducationLevel());
                PlayerPrefs.SetInt($"Player{position}Stars", player.getStars());
                PlayerPrefs.SetInt($"Player{position}FinalScore", player.GetFinalScore());
                PlayerPrefs.SetString($"Player{position}ScoreBreakdown", player.GetScoreBreakdown());
            }
        }

        SceneManager.LoadScene(gameOverSceneName);
    }
    // Restante dos m�todos...
    public BoardSpace GetSpaceFromID(int id) {
        return this.registry[id];
    }

    public BoardSpace GetRandomSpace() {
        return this.registry[Random.Range(1, this.registry.Count - 1)];
    }

    public int MyPlayerNumber(Player p) {
        if (p == p1) return 1;
        if (p == p2) return 2;
        if (p == p3) return 3;
        if (p == p4) return 4;
        return 0;
    }

    public int MyPlayerNumber(PlayerState p) {
        if (p == p1.state) return 1;
        if (p == p2.state) return 2;
        if (p == p3.state) return 3;
        if (p == p4.state) return 4;
        return 0;
    }

    // Despesas são aplicadas por turno em Player.TakeTurn() via sistema de Holerite

    public List<Player> GetPlayers() {
        return new List<Player>() { p1, p2, p3, p4 };
    }
}