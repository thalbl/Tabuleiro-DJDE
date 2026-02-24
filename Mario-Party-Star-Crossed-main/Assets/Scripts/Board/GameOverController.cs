using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameOverController : MonoBehaviour {
    [Header("UI Elements")]
    public Transform playersContainer;
    public GameObject playerPanelPrefab;
    public Button returnButton;

    // Dicionário de fallback para títulos de carreira
    private static readonly Dictionary<int, string> CareerTitles = new Dictionary<int, string> {
        {1, "Estagiário"},
        {2, "Júnior"},
        {3, "Pleno I"},
        {4, "Pleno II"},
        {5, "Sênior I"},
        {6, "Sênior II"},
        {7, "Especialista I"},
        {8, "Especialista II"},
        {9, "Gerente"},
        {10, "Diretor"}
    };

    void Start() {
        returnButton.onClick.AddListener(ReturnToMainMenu);
        CreatePlayerPanels();
    }

    void CreatePlayerPanels() {
        // Limpar container
        foreach (Transform child in playersContainer) {
            Destroy(child.gameObject);
        }

        int activePlayers = PlayerPrefs.GetInt("ActivePlayers", 4);
        int winnerPosition = 1;

        for (int position = 1; position <= activePlayers; position++) {
            GameObject panelObj = Instantiate(playerPanelPrefab, playersContainer);
            PlayerResultPanel panel = panelObj.GetComponent<PlayerResultPanel>();

            // Obter dados do jogador
            string playerName = PlayerPrefs.GetString($"Player{position}Name", $"Jogador {position}");
            int money = PlayerPrefs.GetInt($"Player{position}Money", 0);
            int educationLevel = PlayerPrefs.GetInt($"Player{position}EducationLevel", 0);
            int stars = PlayerPrefs.GetInt($"Player{position}Stars", 0);

            // Obter carreira formatada
            string career = PlayerPrefs.GetString($"Player{position}Career", "Nv. 1 - Profiss�o Estagi�rio");

            panel.SetPlayerData(
                position: position,
                playerName: playerName,
                money: money,
                career: career, // Passar a string formatada completa
                educationLevel: educationLevel,
                stars: stars,
                isWinner: position == winnerPosition
            );
        }
    }

    // M�todo de fallback para obter t�tulo se n�o estiver salvo
    private string GetCareerTitleFallback(int careerLevel) {
        if (CareerTitles.ContainsKey(careerLevel)) {
            return CareerTitles[careerLevel];
        }
        return CareerTitles[10]; // Retorna o n�vel m�ximo como fallback
    }

    public void ReturnToMainMenu() {
        PlayerPrefs.DeleteKey("LoadSavedGame");
        SceneManager.LoadScene("StartScene");
    }
}