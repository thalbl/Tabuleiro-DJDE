using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class StartMenuManager : MonoBehaviour {
    public Button continueButton;

    void Start() {
        // Verifica se existe um save para habilitar o botão Continuar
        continueButton.interactable = File.Exists(GetSavePath());
    }

    public void NewGame() {
        // Remove save existente se houver
        string savePath = GetSavePath();
        if (File.Exists(savePath)) {
            File.Delete(savePath);
        }

        // Seta flag para novo jogo
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        SceneManager.LoadScene("MVP"); // Nome da sua cena do tabuleiro
    }

    public void ContinueGame() {
        // Seta flag para carregar jogo salvo
        PlayerPrefs.SetInt("LoadSavedGame", 1);
        SceneManager.LoadScene("MVP");
    }

    public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    Application.Quit();
    }

    private string GetSavePath() {
        return Application.persistentDataPath + "/mario_party_save.dat";
    }
}