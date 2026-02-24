using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerState : ISerializationCallbackReceiver {
    // PLAYER CONFIG
    [SerializeField] private int controller;
    [SerializeField] private int avatar;

    // VISIBLE PLAYER DATA
    [SerializeField] private int coins;
    [SerializeField] private int stars;
    [SerializeField] private int shroom;
    [SerializeField] private List<BoardItem> items;
    [SerializeField] private int minigameTeam;
    [SerializeField] private int externalPlacing;

    // SISTEMA PROFISSIONAL
    [SerializeField] private string profissao;
    [SerializeField] private int salarioBase;
    [SerializeField] private int salarioAtual;
    [SerializeField] private int despesaBase;
    [SerializeField] private int despesaAtual;


    // SISTEMA DE CARREIRA
    [SerializeField] private int careerLevel = 1;
    [SerializeField] private List<string> investments = new List<string>();
    [SerializeField] private int educationLevel = 0;
    [SerializeField] private int lapsCompleted = 0;

    // Outros campos...
    [SerializeField] private int space;
    [SerializeField] private List<int> coinTracking;
    [SerializeField] private List<int> starTracking;
    [SerializeField] private int totalSpacesMoved;
    [SerializeField] private int totalMinigameRewards;
    [SerializeField] private int totalDuelsPlayed;
    [SerializeField] private int totalUnluckySpaces;
    [SerializeField] private int totalHappeningSpaces;
    [SerializeField] private int totalItemsUsed;
    [SerializeField] private List<int> hiddenBlockCandidates;
    [SerializeField] private int p1Grudge;
    [SerializeField] private int p2Grudge;
    [SerializeField] private int p3Grudge;
    [SerializeField] private int p4Grudge;



    // Dicionários estáticos
    private static readonly Dictionary<string, int> ProfissoesSalariosBase = new Dictionary<string, int> {
        {"Médico", 80}, {"Engenheiro", 70}, {"Professor", 40},
        {"Advogado", 75}, {"Designer", 50}, {"Programador", 65},
        {"Enfermeiro", 45}, {"Chef de Cozinha", 55}, {"Jornalista", 48},
        {"Piloto", 90}, {"Artista", 35}, {"Cientista", 72}, {"Empresário", 100}
    };

    private static readonly Dictionary<string, int> ProfissoesDespesasBase = new Dictionary<string, int> {
        {"Médico", 8}, {"Engenheiro", 7}, {"Professor", 4},
        {"Advogado", 8}, {"Designer", 5}, {"Programador", 7},
        {"Enfermeiro", 5}, {"Chef de Cozinha", 6}, {"Jornalista", 5},
        {"Piloto", 9}, {"Artista", 4}, {"Cientista", 7}, {"Empresário", 10}
    };

    private static readonly float[] CareerMultipliers = { 1.0f, 1.3f, 1.7f, 2.2f, 2.8f, 3.5f, 4.3f, 5.2f, 6.2f, 7.3f };
    // Fatores de multiplicacao por nivel de carreira

    // Adicione este dicionario estatico para mapear niveis para titulos
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

    // LEGACY FIELD FOR MIGRATION
    [SerializeField] private int dinheiro;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize() {
        if (dinheiro > 0) {
            // Migrar dinheiro antigo (R$) para moedas (dividir por 100)
            this.coins += Mathf.Max(1, dinheiro / 100);
            this.dinheiro = 0;
        }
    }

    public PlayerState(int controller, int avatar, int initialCareerLevel = 1) {
        this.controller = controller;
        this.avatar = avatar;
        this.coins = 20;
        this.stars = 0;
        this.items = new List<BoardItem>();
        this.space = 0;
        this.shroom = 0;
        this.coinTracking = new List<int>();
        this.starTracking = new List<int>();
        this.totalSpacesMoved = 0;
        this.totalMinigameRewards = 0;
        this.totalDuelsPlayed = 0;
        this.totalUnluckySpaces = 0;
        this.totalHappeningSpaces = 0;
        this.totalItemsUsed = 0;
        this.hiddenBlockCandidates = new List<int>();
        this.p1Grudge = 0;
        this.p2Grudge = 0;
        this.p3Grudge = 0;
        this.p4Grudge = 0;
        this.externalPlacing = 1;

        this.careerLevel = initialCareerLevel;
        this.educationLevel = 0;
        AtribuirProfissao();
    }

    // ================== SISTEMA PROFISSIONAL ==================
    private void AtribuirProfissao() {
        int index = Random.Range(0, ProfissoesSalariosBase.Count);
        int count = 0;
        foreach (var profissaoSalario in ProfissoesSalariosBase) {
            if (count == index) {
                this.profissao = profissaoSalario.Key;
                this.salarioBase = profissaoSalario.Value;
                this.despesaBase = ProfissoesDespesasBase[profissaoSalario.Key];
                AtualizarSalarioPorNivel();
                AtualizarDespesaPorNivel();
                break;
            }
            count++;
        }
    }

    private void AtualizarDespesaPorNivel() {
        this.despesaAtual = Mathf.RoundToInt(despesaBase * CareerMultipliers[careerLevel - 1]);
    }

    private void AtualizarSalarioPorNivel() {
        this.salarioAtual = Mathf.RoundToInt(salarioBase * CareerMultipliers[Mathf.Clamp(careerLevel - 1, 0, CareerMultipliers.Length - 1)]);
    }

    // ================== SISTEMA DE CARREIRA ==================
    public int GetCareerLevel() => careerLevel;

    public void Promote() {
        if (careerLevel < CareerMultipliers.Length) {
            careerLevel++;
            AtualizarSalarioPorNivel();
            AtualizarDespesaPorNivel();
        }
    }

    public void Demote() {
        if (careerLevel > 1) {
            careerLevel--;
            AtualizarSalarioPorNivel();
            AtualizarDespesaPorNivel();
        }
    }

    public void CompleteLap() {
        lapsCompleted++;
        if (lapsCompleted % 2 == 0 && careerLevel < CareerMultipliers.Length) {
            Promote();
        }
    }



    public int GetDespesaAtual() => this.despesaAtual;
    public string GetProfissao() => this.profissao;
    public int GetSalary() => this.salarioAtual;
    // Moeda Unificada: GetDinheiro/SetDinheiro agora usam coins diretamente
    public int GetDinheiro() => this.coins;
    public void SetDinheiro(int novoDinheiro) => this.coins = Mathf.Clamp(novoDinheiro, 0, 9999);
    public void SetBaseSalary(int newSalary) => this.salarioAtual = newSalary;
    // Mï¿½todo para obter o titulo da carreira
    public string GetCareerTitle() {
        if (CareerTitles.ContainsKey(careerLevel)) {
            return CareerTitles[careerLevel];
        }
        return CareerTitles[10]; // Default para o nivel maximo
    }

    // Mï¿½todo para obter o titulo + nivel formatado
    public string GetFormattedCareerLevel() {
        return $"Nv. {careerLevel} - {GetProfissao()} {GetCareerTitle()}";
    }

    // ================== SISTEMA DE INVESTIMENTOS ==================
    public void AddInvestment(string investment) {
        if (!investments.Contains(investment)) {
            investments.Add(investment);
        }
    }

    public bool HasInvestment(string investment) => investments.Contains(investment);

    public List<string> GetInvestments() => investments;

    // ================== SISTEMA DE EDUCAÇÃOO ==================
    public void IncreaseEducation() {
        educationLevel = Mathf.Min(educationLevel + 1, 3);
    }

    public int GetEducationLevel() => educationLevel;


    // ================== FUNÇÕES EXISTENTES ==================
    public int getCoins() => this.coins;

    public bool setCoins(int c) {
        this.coins = c;
        if (this.coins < 0) {
            this.coins = 0;
            return false;
        }
        else if (this.coins > 9999) {
            this.coins = 9999;
            return false;
        }
        return true;
    }

    public bool changeCoins(int c) {
        this.coins += c;
        if (this.coins < 0) {
            this.coins = 0;
            return false;
        }
        else if (this.coins > 9999) {
            this.coins = 9999;
            return false;
        }
        return true;
    }

    public int getStars() => this.stars;

    public bool setStars(int s) {
        this.stars = s;
        if (this.stars < 0) {
            this.stars = 0;
            return false;
        }
        else if (this.stars > 99) {
            this.stars = 99;
            return false;
        }
        return true;
    }

    public bool changeStars(int s) {
        this.stars += s;
        if (this.stars < 0) {
            this.stars = 0;
            return false;
        }
        else if (this.stars > 99) {
            this.stars = 99;
            return false;
        }
        return true;
    }

    public List<BoardItem> getItems() => this.items;

    public void setItems(List<BoardItem> li) => this.items = li;

    public bool addItem(BoardItem i) {
        if (this.items.Count == 3) {
            return false;
        }
        this.items.Add(i);
        return true;
    }

    public bool removeItem(BoardItem i) {
        if (this.items.Contains(i)) {
            items.Remove(i);
            return true;
        }
        return false;
    }

    public BoardItem stealRandomItem() {
        int index = Random.Range(0, this.items.Count - 1);
        BoardItem bi = items[index];
        this.removeItem(bi);
        return bi;
    }

    public bool hasItem(BoardItem i) => this.items.Contains(i);

    public void setSpace(int s) => this.space = s;

    public int getSpaceID() => this.space;

    public void setMovement(int s) => this.shroom = s;

    public int getMovement() => this.shroom;

    public int getPlacing() => this.externalPlacing;

    public void setPlacing(int p) => this.externalPlacing = p;

    public int getTeam() => this.minigameTeam;

    public void setTeam(int t) => this.minigameTeam = t;



    public string charName() {
        switch (this.avatar) {
            case 1: return "João";
            case 2: return "Maria";
            case 3: return "Pedro";
            case 4: return "Ana";
            case 5: return "Carlos";
            case 6: return "Laura";
            case 7: return "Paulo";
            case 8: return "Fernanda";
            case 9: return "Ricardo";
            case 10: return "Beatriz";
            case 11: return "Marcos";
            case 12: return "Juliana";
            case 13: return "Rafael";
            default: return "José";
        }
    }

    public void UsedItemStatTrigger() => this.totalItemsUsed += 1;

    public void MovedSpaceStatTrigger() => this.totalSpacesMoved += 1;

    public void UnluckySpaceStatTrigger() => this.totalUnluckySpaces += 1;

    public void HappeningSpaceStatTrigger() => this.totalHappeningSpaces += 1;

    // Métodos getter para estatísticas
    public int GetTotalSpacesMoved() => this.totalSpacesMoved;
    public int GetTotalMinigameRewards() => this.totalMinigameRewards;
    public int GetTotalDuelsPlayed() => this.totalDuelsPlayed;
    public int GetTotalUnluckySpaces() => this.totalUnluckySpaces;
    public int GetTotalHappeningSpaces() => this.totalHappeningSpaces;
    public int GetTotalItemsUsed() => this.totalItemsUsed;

    public Color charColor() {
        switch (this.avatar) {
            case 1: return new Color(0.0f, 0.8f, 0.0f, 1.0f);
            case 2: return new Color(1.0f, 0.5f, 1.0f, 1.0f);
            case 3: return new Color(0.0f, 1.0f, 0.0f, 1.0f);
            case 4: return new Color(0.6f, 0.3f, 0.0f, 1.0f);
            case 5: return new Color(1.0f, 1.0f, 0.0f, 1.0f);
            case 6: return new Color(1.0f, 0.5f, 0.0f, 1.0f);
            case 7: return new Color(0.5f, 0.0f, 1.0f, 1.0f);
            case 8: return new Color(1.0f, 0.8f, 0.8f, 1.0f);
            case 9: return new Color(0.0f, 0.8f, 0.8f, 1.0f);
            case 10: return new Color(0.4f, 0.4f, 0.4f, 1.0f);
            case 11: return new Color(0.0f, 0.4f, 0.0f, 1.0f);
            case 12: return new Color(1.0f, 0.0f, 1.0f, 1.0f);
            case 13: return new Color(0.6f, 0.6f, 0.0f, 1.0f);
            default: return new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }

    public int getController() => this.controller;

    
}

