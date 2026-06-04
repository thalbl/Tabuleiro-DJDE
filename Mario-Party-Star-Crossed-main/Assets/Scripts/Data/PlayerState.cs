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

    // Despesas calibradas para 25-40% do salário base:
    // - Profissões de alto salário têm custo de vida maior (inflação de estilo de vida)
    // - Profissões de menor salário são mais econômicas
    private static readonly Dictionary<string, int> ProfissoesDespesasBase = new Dictionary<string, int> {
        {"Médico", 28},          // 28/80 = 35%
        {"Engenheiro", 21},      // 21/70 = 30%
        {"Professor", 10},       // 10/40 = 25%
        {"Advogado", 26},        // 26/75 = 35%
        {"Designer", 15},        // 15/50 = 30%
        {"Programador", 20},     // 20/65 = 31%
        {"Enfermeiro", 14},      // 14/45 = 31%
        {"Chef de Cozinha", 17}, // 17/55 = 31%
        {"Jornalista", 14},      // 14/48 = 29%
        {"Piloto", 36},          // 36/90 = 40%
        {"Artista", 9},          // 9/35  = 26%
        {"Cientista", 22},       // 22/72 = 31%
        {"Empresário", 40}       // 40/100 = 40%
    };

    // Multiplicadores lineares controlados (máx 3.0x em vez de 7.3x)
    // Mantém progressão significativa sem causar hiperinflação no late-game
    // Nv1=1.0  Nv2=1.15  Nv3=1.3  Nv4=1.5  Nv5=1.7  Nv6=1.9  Nv7=2.1  Nv8=2.35  Nv9=2.6  Nv10=3.0
    private static readonly float[] CareerMultipliers = { 1.0f, 1.15f, 1.3f, 1.5f, 1.7f, 1.9f, 2.1f, 2.35f, 2.6f, 3.0f };

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

    // ================== PONTUAÇÃO FINAL (CONDIÇÃO DE VITÓRIA) ==================
    // Pontuação composta que valoriza todas as mecânicas do jogo:
    //   Estrelas × 100 + Moedas + Nível de Carreira × 50 + Educação × 75
    public int GetFinalScore() {
        return (stars * 100) + coins + (careerLevel * 50) + (educationLevel * 75);
    }

    public string GetScoreBreakdown() {
        return $"Estrelas: {stars} x100 = {stars * 100}\n" +
               $"Moedas: {coins}\n" +
               $"Carreira Nv.{careerLevel} x50 = {careerLevel * 50}\n" +
               $"Educacao Nv.{educationLevel} x75 = {educationLevel * 75}\n" +
               $"_______________________\n" +
               $"TOTAL: {GetFinalScore()} pontos";
    }

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

