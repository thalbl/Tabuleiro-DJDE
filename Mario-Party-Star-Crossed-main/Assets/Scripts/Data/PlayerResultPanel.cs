using UnityEngine;
using UnityEngine.UI;

public class PlayerResultPanel : MonoBehaviour {
    [Header("UI References")]
    public Text positionText;
    public Text nameText;
    public Text moneyText;
    public Text careerLevelText; // Agora mostra ttulo + nvel
    public Text educationLevelText;
    public Text starsText;
    public Text scoreText;
    public Image backgroundImage;

    public void SetPlayerData(
         int position,
         string playerName,
         int money,
         string career,
         int educationLevel,
         int stars,
         int finalScore,
         bool isWinner
     ) {
        positionText.text = GetPositionString(position);
        nameText.text = playerName;
        moneyText.text = $"{money} Moedas";

        careerLevelText.text = career;

        educationLevelText.text = $"Educação: Nv. {educationLevel}";
        starsText.text = $"{stars} Estrelas";

        if (scoreText != null)
            scoreText.text = $"Pontuação: {finalScore} pts";

        GetColorFromPosition(position, backgroundImage);

        // Destacar o vencedor
        if (isWinner) {
            backgroundImage.color = new Color(1f, 0.84f, 0f, 0.3f); // Dourado
            positionText.color = Color.yellow;
            nameText.fontStyle = FontStyle.Bold;
        }
    }

    private string GetPositionString(int position) {
        switch (position) {
            case 1: return "1";
            case 2: return "2";
            case 3: return "3";
            case 4: return "4";
            default: return $"{position}";
        }
    }

    private void GetColorFromPosition(int position, Image backGroundImage) {
        switch (position) {
            case 1:
                backgroundImage.color = new Color(1f, 0.843f, 0f); // Ouro
                break;
            case 2:
                backgroundImage.color = new Color(0.753f, 0.753f, 0.753f); // Prata
                break;
            case 3:
                backgroundImage.color = new Color(0.804f, 0.498f, 0.196f); // Bronze
                break;
            default:
                backgroundImage.color = new Color(1f, 1f, 1f); // Branco
                break;
        }
    }
}