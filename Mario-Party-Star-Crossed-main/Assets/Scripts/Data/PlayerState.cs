using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerState : ISerializationCallbackReceiver {
    // PLAYER CONFIG
    [SerializeField] private int controller;
    [SerializeField] private int avatar;
    [SerializeField] private string customPlayerName = "";

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
    // Aumentos/cortes acumulados (casas azul/vermelha). Somado ao salário do nível,
    // para não se perder quando Promote()/Demote() recalculam o salário.
    [SerializeField] private int ajusteSalario;
    [SerializeField] private bool ajusteSalarioMigrado;
    [SerializeField] private int despesaBase;
    [SerializeField] private int despesaAtual;


    // SISTEMA DE CARREIRA
    [SerializeField] private int careerLevel = 1;
    [SerializeField] private List<string> investments = new List<string>();
    [SerializeField] private int educationLevel = 0;
    [SerializeField] private int lapsCompleted = 0;

    // CARTEIRA FINANCEIRA (Banco)
    [SerializeField] private int poupanca;   // rende juros compostos por turno
    [SerializeField] private int reserva;    // não rende; paga imprevistos primeiro
    [SerializeField] private int acoes;      // varia com a Bolsa no fim de cada rodada
    [SerializeField] private int divida;     // juros por turno; parcela automática no holerite

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
    // Economia rebalanceada (2026-11): o líquido por turno (salário - despesa) ia de +26 (Artista)
    // a +60 (Empresário), 2,3x de diferença só pelo sorteio da profissão. Agora vai de +32 a +48:
    // quem ganha mais também gasta mais (inflação de estilo de vida), mantendo a ordem dos salários.
    private static readonly Dictionary<string, int> ProfissoesSalariosBase = new Dictionary<string, int> {
        {"Médico", 80}, {"Engenheiro", 70}, {"Professor", 45},
        {"Advogado", 75}, {"Designer", 55}, {"Programador", 65},
        {"Enfermeiro", 50}, {"Chef de Cozinha", 58}, {"Jornalista", 52},
        {"Piloto", 90}, {"Artista", 45}, {"Cientista", 72}, {"Empresário", 95}
    };

    private static readonly Dictionary<string, int> ProfissoesDespesasBase = new Dictionary<string, int> {
        {"Médico", 34},          // 80 - 34 = 46
        {"Engenheiro", 26},      // 70 - 26 = 44
        {"Professor", 12},       // 45 - 12 = 33
        {"Advogado", 30},        // 75 - 30 = 45
        {"Designer", 20},        // 55 - 20 = 35
        {"Programador", 24},     // 65 - 24 = 41
        {"Enfermeiro", 16},      // 50 - 16 = 34
        {"Chef de Cozinha", 22}, // 58 - 22 = 36
        {"Jornalista", 18},      // 52 - 18 = 34
        {"Piloto", 42},          // 90 - 42 = 48
        {"Artista", 13},         // 45 - 13 = 32
        {"Cientista", 28},       // 72 - 28 = 44
        {"Empresário", 47}       // 95 - 47 = 48
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
        // Saves antigos guardavam aumentos/cortes direto em salarioAtual: preserva como ajuste
        if (!ajusteSalarioMigrado) {
            ajusteSalario = salarioAtual - SalarioDoNivel();
            ajusteSalarioMigrado = true;
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
        this.ajusteSalario = 0;
        this.ajusteSalarioMigrado = true;
        AtribuirProfissao();
    }

    public PlayerState(int controller, int avatar, string customName, int initialCareerLevel = 1) : this(controller, avatar, initialCareerLevel) {
        this.customPlayerName = customName;
    }

    public void setCustomName(string name) {
        this.customPlayerName = name;
    }

    // ================== SISTEMA PROFISSIONAL ==================
    private void AtribuirProfissao() {
        int index = GameRng.Range(0, ProfissoesSalariosBase.Count);
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

    private int SalarioDoNivel() {
        return Mathf.RoundToInt(salarioBase * CareerMultipliers[Mathf.Clamp(careerLevel - 1, 0, CareerMultipliers.Length - 1)]);
    }

    private void AtualizarSalarioPorNivel() {
        this.salarioAtual = Mathf.Max(10, SalarioDoNivel() + ajusteSalario);
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

    // ================== EXPERIÊNCIA DE MERCADO ==================
    // A cada N casas percorridas o jogador é promovido por experiência. Antes era "a cada 2 voltas",
    // mas os anéis têm tamanhos muito diferentes (16 a 60 casas no MVP) e a contagem dependia da rota:
    // num anel grande levava ~18 rodadas por nível. Casas andadas é igual para qualquer rota.
    public const int CasasPorPromocaoExperiencia = 30;

    [SerializeField] private int casasDesdeUltimaExperiencia = 0;
    [SerializeField] private int promocoesExperienciaPendentes = 0;

    /// <summary>Promoções por experiência ganhas e ainda não anunciadas (zera ao ler).</summary>
    public int ConsumirPromocoesExperiencia() {
        int n = promocoesExperienciaPendentes;
        promocoesExperienciaPendentes = 0;
        return n;
    }

    private void ContarCasaParaExperiencia() {
        casasDesdeUltimaExperiencia++;
        if (casasDesdeUltimaExperiencia >= CasasPorPromocaoExperiencia) {
            casasDesdeUltimaExperiencia = 0;
            if (careerLevel < CareerMultipliers.Length) {
                Promote();
                promocoesExperienciaPendentes++;
            }
        }
    }



    public int GetDespesaAtual() => this.despesaAtual;
    public string GetProfissao() => this.profissao;
    public int GetSalary() => this.salarioAtual;
    // Moeda Unificada: GetDinheiro/SetDinheiro agora usam coins diretamente
    public int GetDinheiro() => this.coins;
    public void SetDinheiro(int novoDinheiro) => this.coins = Mathf.Clamp(novoDinheiro, 0, 9999);
    // Define o salário atual guardando a diferença como ajuste persistente (sobrevive a promoções)
    public void SetBaseSalary(int newSalary) {
        this.ajusteSalario = newSalary - SalarioDoNivel();
        AtualizarSalarioPorNivel();
    }
    // Método para obter o titulo da carreira
    public string GetCareerTitle() => ObterTituloCarreira(careerLevel);

    public static string ObterTituloCarreira(int level) {
        if (CareerTitles.ContainsKey(level)) {
            return CareerTitles[level];
        }
        return CareerTitles[10];
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
        return (stars * 100) + Mathf.Max(0, PatrimonioLiquido) + (careerLevel * 50) + (educationLevel * 75);
    }

    public string GetScoreBreakdown() {
        return $"Estrelas: {stars} x100 = {stars * 100}\n" +
               $"Patrimônio líquido: {PatrimonioLiquido} (moedas + aplicações - dívidas)\n" +
               $"Carreira Nv.{careerLevel} x50 = {careerLevel * 50}\n" +
               $"Educacao Nv.{educationLevel} x75 = {educationLevel * 75}\n" +
               $"_______________________\n" +
               $"TOTAL: {GetFinalScore()} pontos";
    }

    // ================== CARTEIRA / BANCO ==================
    public const float JurosPoupanca = 0.03f;      // por turno
    public const float JurosDivida = 0.08f;        // por turno
    public const float ParcelaDividaPct = 0.10f;   // parcela mínima automática
    public const int ParcelaDividaMinima = 5;
    public const int ValorEmprestimo = 50;
    public const int LimiteDivida = 300;           // teto de crédito (empréstimos + cheque especial)

    public int GetPoupanca() => poupanca;
    public int GetReserva() => reserva;
    public int GetAcoes() => acoes;
    public int GetDivida() => divida;
    public int Aplicacoes => poupanca + reserva + acoes;
    public int PatrimonioLiquido => coins + poupanca + reserva + acoes - divida;

    /// <summary>Move moedas para um produto. Retorna quanto foi movido de fato.</summary>
    int Aplicar(ref int destino, int valor) {
        valor = Mathf.Clamp(valor, 0, coins);
        coins -= valor;
        destino += valor;
        return valor;
    }
    int Resgatar(ref int origem, int valor) {
        valor = Mathf.Clamp(valor, 0, origem);
        origem -= valor;
        coins = Mathf.Min(coins + valor, 9999);
        return valor;
    }
    public int DepositarPoupanca(int v) => Aplicar(ref poupanca, v);
    public int SacarPoupanca(int v) => Resgatar(ref poupanca, v);
    public int GuardarReserva(int v) => Aplicar(ref reserva, v);
    public int ResgatarReserva(int v) => Resgatar(ref reserva, v);
    public int ComprarAcoes(int v) => Aplicar(ref acoes, v);
    public int VenderAcoes(int v) => Resgatar(ref acoes, v);

    public bool PodePegarEmprestimo => divida + ValorEmprestimo <= LimiteDivida;
    public int PegarEmprestimo() {
        if (!PodePegarEmprestimo) return 0;
        divida += ValorEmprestimo;
        coins = Mathf.Min(coins + ValorEmprestimo, 9999);
        return ValorEmprestimo;
    }
    public int PagarDivida(int v) {
        v = Mathf.Clamp(v, 0, Mathf.Min(coins, divida));
        coins -= v;
        divida -= v;
        return v;
    }

    /// <summary>
    /// Fechamento do mês (início do turno, depois do salário): juros da poupança, juros da dívida e
    /// parcela automática. Retorna o resumo para o holerite ("" se não houver nada a mostrar).
    /// </summary>
    public string ProcessarMesFinanceiro() {
        var partes = new List<string>();
        if (poupanca > 0) {
            int rendimento = Mathf.Max(1, Mathf.RoundToInt(poupanca * JurosPoupanca));
            poupanca += rendimento;
            partes.Add($"Poupança +{rendimento}");
        }
        if (divida > 0) {
            int juros = Mathf.Max(1, Mathf.RoundToInt(divida * JurosDivida));
            divida += juros;
            int parcela = Mathf.Min(divida, Mathf.Max(ParcelaDividaMinima, Mathf.RoundToInt(divida * ParcelaDividaPct)));
            int pago = Mathf.Min(parcela, coins);
            coins -= pago;
            divida -= pago;
            partes.Add($"Dívida: juros +{juros}, parcela -{pago}" + (divida > 0 ? $" (resta {divida})" : " (quitada!)"));
        }
        return string.Join("  ·  ", partes);
    }

    /// <summary>
    /// Paga um imprevisto: primeiro com a reserva de emergência, depois com as moedas;
    /// o que faltar vira dívida (cheque especial). Retorna como foi pago.
    /// </summary>
    public string PagarImprevisto(int valor) {
        if (valor <= 0) return "";
        var partes = new List<string>();
        int daReserva = Mathf.Min(valor, reserva);
        reserva -= daReserva;
        valor -= daReserva;
        if (daReserva > 0) partes.Add($"reserva cobriu {daReserva}");
        int dasMoedas = Mathf.Min(valor, coins);
        coins -= dasMoedas;
        valor -= dasMoedas;
        if (dasMoedas > 0) partes.Add($"{dasMoedas} das moedas");
        if (valor > 0) {
            divida += valor;
            partes.Add($"{valor} no cheque especial");
        }
        return string.Join(", ", partes);
    }

    /// <summary>Despesa obrigatória (ex.: holerite negativo): moedas e, se faltar, cheque especial.</summary>
    public int PagarDespesa(int valor) {
        int dasMoedas = Mathf.Min(valor, coins);
        coins -= dasMoedas;
        int falta = valor - dasMoedas;
        if (falta > 0) divida += falta;
        return falta;
    }

    /// <summary>Aplica a variação da Bolsa (%) nas ações.</summary>
    public int AtualizarAcoes(int variacaoPct) {
        if (acoes <= 0) return 0;
        int antes = acoes;
        acoes = Mathf.Max(0, Mathf.RoundToInt(acoes * (1f + variacaoPct / 100f)));
        return acoes - antes;
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
        int index = GameRng.Range(0, this.items.Count);
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
        if (!string.IsNullOrEmpty(this.customPlayerName)) {
            return this.customPlayerName;
        }
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

    public void MovedSpaceStatTrigger() {
        this.totalSpacesMoved += 1;
        ContarCasaParaExperiencia();
    }

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

