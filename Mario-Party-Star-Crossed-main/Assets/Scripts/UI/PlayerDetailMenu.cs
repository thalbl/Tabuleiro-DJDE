using UnityEngine;
using UnityEngine.UI;

public class PlayerDetailMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject detailPanel;
    public Text playerNameText;
    public Text playerProfessionText;
    public Text playerCareerLevelText;
    public Text playerMoneyText;
    public Text playerCoinsText;
    public Text playerStarsText;
    public Text playerEducationText;
    public Text playerInvestmentsText;
    public Text playerStatsText;
    public Button closeButton;
    public Image playerColorIndicator;

    private PlayerState currentPlayer;
    private bool isVisible = false;

    void Start()
    {
        // Esconder o painel inicialmente
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        // Configurar botão de fechar
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseMenu);
        }

        // Permitir fechar clicando fora do painel
        if (detailPanel != null)
        {
            Button backgroundButton = detailPanel.GetComponent<Button>();
            if (backgroundButton == null)
            {
                backgroundButton = detailPanel.AddComponent<Button>();
            }
            backgroundButton.onClick.AddListener(CloseMenu);
        }
    }

    public void ShowPlayerDetails(PlayerState player)
    {
        currentPlayer = player;
        UpdateDisplay();
        
        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
            isVisible = true;
        }
    }

    public void CloseMenu()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
            isVisible = false;
        }
    }

    private void UpdateDisplay()
    {
        if (currentPlayer == null) return;

        // Nome do jogador
        if (playerNameText != null)
        {
            playerNameText.text = currentPlayer.charName();
        }

        // Profissao e carreira
        if (playerProfessionText != null)
        {
            playerProfessionText.text = "Profissao: " + currentPlayer.GetProfissao();
        }

        if (playerCareerLevelText != null)
        {
            playerCareerLevelText.text = currentPlayer.GetFormattedCareerLevel();
        }

        // Moeda unificada
        if (playerMoneyText != null)
        {
            playerMoneyText.text = "Moedas: " + currentPlayer.getCoins().ToString();
        }

        if (playerCoinsText != null)
        {
            playerCoinsText.text = "Moedas: " + currentPlayer.getCoins().ToString();
        }

        // Estrelas
        if (playerStarsText != null)
        {
            playerStarsText.text = "Estrelas: " + currentPlayer.getStars().ToString();
        }

        // Educacao
        if (playerEducationText != null)
        {
            int eduLevel = currentPlayer.GetEducationLevel();
            string educationText = "";
            
            switch (eduLevel)
            {
                case 0:
                    educationText = "Sem educacao formal";
                    break;
                case 1:
                    educationText = "Ensino Medio";
                    break;
                case 2:
                    educationText = "Ensino Superior";
                    break;
                case 3:
                    educationText = "Pos-graduacao";
                    break;
                default:
                    educationText = "Educacao avancada";
                    break;
            }
            
            playerEducationText.text = "Educacao: " + educationText;
        }

        // Investimentos
        if (playerInvestmentsText != null)
        {
            var investments = currentPlayer.GetInvestments();
             if (investments.Count > 0)
             {
                 playerInvestmentsText.text = "Aplicações: " + string.Join(", ", investments);
             }
             else
             {
                 playerInvestmentsText.text = "Aplicações: Nenhuma";
             }
        }

        // Estatisticas do jogador
        if (playerStatsText != null)
        {
            string stats = "Mesada: " + currentPlayer.GetSalary().ToString() + " Moedas/turno\n" +
                          "Posi\u00e7\u00e3o: " + currentPlayer.getPlacing().ToString() + "\u00ba lugar\n" +
                          "Espa\u00e7os movidos: " + currentPlayer.GetTotalSpacesMoved().ToString();
            playerStatsText.text = stats;
        }

        // Cor do jogador
        if (playerColorIndicator != null)
        {
            playerColorIndicator.color = currentPlayer.charColor();
        }
    }

    public bool IsVisible()
    {
        return isVisible;
    }

    // Metodo para atualizar as informacoes se o jogador mudou
    public void RefreshDisplay()
    {
        if (isVisible && currentPlayer != null)
        {
            UpdateDisplay();
        }
    }
}