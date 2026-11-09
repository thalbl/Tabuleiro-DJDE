using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class StartMenuManager : MonoBehaviour {
    public Button continueButton;

    void Start() {
        if (continueButton != null) {
            continueButton.interactable = GameState.HasSaveGame();
        }
    }

    public void NewGame() {
        GameState.DeleteSaveGame();
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        GameMatchConfig.LoadFromPrefs();
        SceneManager.LoadScene(GameMatchConfig.SelectedMapScene);
    }

    public void ContinueGame() {
        PlayerPrefs.SetInt("LoadSavedGame", 1);
        SceneManager.LoadScene(GameState.GetSavedMapScene());
    }

    public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private string GetSavePath() {
        return GameState.SaveFilePath;
    }
}
