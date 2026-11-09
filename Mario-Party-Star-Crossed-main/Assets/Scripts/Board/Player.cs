using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Player : MonoBehaviour {
    public PlayerState state;
    private BoardSpace targetSpace;
    private int rollCount;
    private UIManager ui;
    private List<int> rolls;
    private bool usedItem;
    private Camera myCam;
    private bool takingTurn;
    public AIBrain brain;
    private int money;

    [Tooltip("Movement Speed of Player.")]
    public float moveSpeed = 1.0f;
    [Tooltip("The list of dice prefabs; Normal, Double, Triple, Magic, Poison, Bowser.")]
    public List<GameObject> dicePrefabs;
    [Tooltip("BoardManager of this board.")]
    public BoardManager game;

    // Start is called before the first frame update
    void Start() {
        if (ui == null) ui = FindObjectOfType<UIManager>();
        if (game == null) game = FindObjectOfType<BoardManager>();
        myCam = GetComponentsInChildren<Camera>()[0];
        myCam.enabled = false;
        AtualizarBrain();
    }

    public void AtualizarBrain() {
        if (game == null) game = FindObjectOfType<BoardManager>();
        if (game == null || state == null || game.p1 == null || game.p2 == null || game.p3 == null || game.p4 == null) return;
        if (game.p1.state == null || game.p2.state == null || game.p3.state == null || game.p4.state == null) return;

        List<PlayerState> rivals = new List<PlayerState>() { game.p1.state, game.p2.state, game.p3.state, game.p4.state };
        rivals.Remove(state);
        if (rivals.Count < 3) return;

        if (state.getController() == 0 || state.getController() == 5) {
            brain = new PlayerBrain(state, rivals[0], rivals[1], rivals[2], game);
        } else {
            brain = new EasyBrain(state, rivals[0], rivals[1], rivals[2], game);
        }
    }

    // Update is called once per frame
    void Update() {

    }

    public IEnumerator TakeTurn() {
        if (brain == null || (state.getController() != 0 && state.getController() != 5 && brain is PlayerBrain)) {
            AtualizarBrain();
        }
        ui.SetController(state.getController());
        ui.SetDecisionAI(brain);
        myCam.enabled = true;

        // Avisos de promoção passiva e despesas extras serão exibidos após o jogador concluir suas ações.

        if (GameMatchConfig.PassAndPlayEnabled && state.getController() == 0) {
            ui.Dialogue("Pass & Play", $"Vez de {state.charName()}!\nPasse o dispositivo para este jogador.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        }

        ui.YourTurn(state.charName(), state.charColor());
        usedItem = false;

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        // Sistema financeiro: salário líquido = salário - despesas
        int salario = state.GetSalary();
        int despesas = state.GetDespesaAtual();
        int liquido = salario - despesas;
        state.changeCoins(liquido);

        string resumoFinanceiro =
            $"Salario:    +{salario} Moedas\n" +
            $"Despesas: -{despesas} Moedas\n" +
            $"_______________________\n" +
            $"Liquido:    {(liquido >= 0 ? "+" : "")}{liquido} Moedas";
        ui.Dialogue("Holerite Mensal", resumoFinanceiro, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        while (takingTurn) {
            // Limpar descrições de itens antes de mostrar o menu principal
            ui.ClearOptionDescriptions();
            
            if (usedItem || state.getItems().Count == 0) {
                ui.Dialogue(Prompts.MenuTurno, new List<string>() { "Rolar Dados", "Ver Tabuleiro" }, true);
            }
            else {
                ui.Dialogue(Prompts.MenuTurno, new List<string>() { "Rolar Dados", "Usar Item", "Ver Tabuleiro" }, true);
            }
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            switch (ui.MostRecentDialogueAnswer()) {
                case "Usar Item":
                    List<string> itemOptions = new List<string>();
                    List<string> itemDescs = new List<string>();
                    foreach (BoardItem b in state.getItems()) {
                        itemOptions.Add(ItemSpace.itemNames[(int)b]);
                        itemDescs.Add(ItemSpace.itemDescriptions[(int)b]);
                    }
                    itemOptions.Add("Cancelar");
                    itemDescs.Add(""); // Descrição vazia para Cancelar
                    ui.Dialogue(Prompts.EscolhaItem, itemOptions, true);
                    ui.SetOptionDescriptions(itemDescs);
                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                    
                    // Se cancelou, voltar ao menu anterior
                    if (ui.MostRecentDialogueAnswer() == "Cancelar") {
                        break; // Volta ao início do loop while(takingTurn)
                    }
                    
                    usedItem = true;
                    switch (ui.MostRecentDialogueAnswer()) {
                        case "Cofrinho": // Mushroom -> Cofrinho
                            state.setMovement(1);
                            state.removeItem(BoardItem.Mushroom);
                            ui.Dialogue("Você usou o Cofrinho! Avança 1 casa.", true);
                            break;
                        case "Super Dado": // Golden Mushroom -> Super Dado
                            state.setMovement(2);
                            state.removeItem(BoardItem.GoldenMushroom);
                            ui.Dialogue("Você usou o Super Dado! Avança 2 casas!", true);
                            break;
                        case "Foguete": // Magic Mushroom -> Foguete
                            state.setMovement(3);
                            state.removeItem(BoardItem.MagicMushroom);
                            ui.Dialogue("Você usou o Foguete! Avança 3 casas!", true);
                            break;
                        case "Fantasia do Bowser": // Bowser Suit -> Fantasia do Bowser
                            state.setMovement(5);
                            state.removeItem(BoardItem.BowserSuit);
                            ui.Dialogue("Você vestiu a Fantasia do Bowser! Avança 5 casas!", true);
                            break;
                        case "Buraco no Bolso": // Poison Mushroom -> Buraco no Bolso
                            ui.Dialogue(Prompts.AlvoBuracoNoBolso,new List<string>() { game.p1.state.charName(), game.p2.state.charName(), game.p3.state.charName(), game.p4.state.charName(), "Cancelar" }, true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            if (ui.MostRecentDialogueAnswer() == game.p1.state.charName()) {
                                game.p1.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p1.state.charName() + " caiu no Buraco no Bolso! No próximo turno só rola de 1 a 3.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p2.state.charName()) {
                                game.p2.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p2.state.charName() + " caiu no Buraco no Bolso! No próximo turno só rola de 1 a 3.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p3.state.charName()) {
                                game.p3.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p3.state.charName() + " caiu no Buraco no Bolso! No próximo turno só rola de 1 a 3.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p4.state.charName()) {
                                game.p4.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p4.state.charName() + " caiu no Buraco no Bolso! No próximo turno só rola de 1 a 3.", true);
                            }
                            else {
                                usedItem = false;
                            }
                            break;
                        case "Ventania": // Tweester Totem -> Ventania
                            ui.Dialogue(Prompts.AlvoVentania,new List<string>() { game.p1.state.charName(), game.p2.state.charName(), game.p3.state.charName(), game.p4.state.charName(), "Cancelar" }, true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            if (ui.MostRecentDialogueAnswer() == game.p1.state.charName()) {
                                game.p1.setNextSpace(game.GetRandomSpace(), true);
                                state.removeItem(BoardItem.TweesterTotem);
                                ui.Dialogue(game.p1.state.charName() + " foi soprado pela Ventania!", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p2.state.charName()) {
                                game.p2.setNextSpace(game.GetRandomSpace(), true);
                                state.removeItem(BoardItem.TweesterTotem);
                                ui.Dialogue(game.p2.state.charName() + " foi soprado pela Ventania!", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p3.state.charName()) {
                                game.p3.setNextSpace(game.GetRandomSpace(), true);
                                state.removeItem(BoardItem.TweesterTotem);
                                ui.Dialogue(game.p3.state.charName() + " foi soprado pela Ventania!", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p4.state.charName()) {
                                game.p4.setNextSpace(game.GetRandomSpace(), true);
                                state.removeItem(BoardItem.TweesterTotem);
                                ui.Dialogue(game.p4.state.charName() + " foi soprado pela Ventania!", true);
                            }
                            else {
                                usedItem = false;
                            }
                            break;
                        case "Portal Mágico": // Warp Pipe -> Portal Mágico
                            // Mostrar confirmação antes de usar
                            ui.Dialogue("Confirmar Uso", "Usar o Portal Mágico para trocar de lugar com outro jogador?", new List<string>() {"Usar", "Cancelar"}, true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            
                            // Se cancelou, voltar ao menu anterior
                            if (ui.MostRecentDialogueAnswer() == "Cancelar") {
                                usedItem = false;
                                break;
                            }
                            
                            // Se confirmou, executar a troca
                            if (ui.MostRecentDialogueAnswer() == "Usar") {
                                int swapWith = game.MyPlayerNumber(this) + GameRng.Range(1, 3);
                                if (swapWith > 4) {
                                    swapWith -= 4;
                                }
                                int temp = state.getSpaceID();
                                switch (swapWith) {
                                    case 1:
                                        setNextSpace(game.GetSpaceFromID(game.p1.state.getSpaceID()), true);
                                        game.p1.setNextSpace(game.GetSpaceFromID(temp), true);
                                        state.removeItem(BoardItem.WarpPipe);
                                        ui.Dialogue("Você trocou de lugar com " + game.p1.state.charName() + " pelo Portal Mágico!", true);
                                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                        break;
                                    case 2:
                                        setNextSpace(game.GetSpaceFromID(game.p2.state.getSpaceID()), true);
                                        game.p2.setNextSpace(game.GetSpaceFromID(temp), true);
                                        state.removeItem(BoardItem.WarpPipe);
                                        ui.Dialogue("Você trocou de lugar com " + game.p2.state.charName() + " pelo Portal Mágico!", true);
                                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                        break;
                                    case 3:
                                        setNextSpace(game.GetSpaceFromID(game.p3.state.getSpaceID()), true);
                                        game.p3.setNextSpace(game.GetSpaceFromID(temp), true);
                                        state.removeItem(BoardItem.WarpPipe);
                                        ui.Dialogue("Você trocou de lugar com " + game.p3.state.charName() + " pelo Portal Mágico!", true);
                                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                        break;
                                    case 4:
                                        setNextSpace(game.GetSpaceFromID(game.p4.state.getSpaceID()), true);
                                        game.p4.setNextSpace(game.GetSpaceFromID(temp), true);
                                        state.removeItem(BoardItem.WarpPipe);
                                        ui.Dialogue("Você trocou de lugar com " + game.p4.state.charName() + " pelo Portal Mágico!", true);
                                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                        break;
                                    default:
                                        usedItem = false;
                                        break;
                                }
                            } else {
                                usedItem = false;
                            }
                            break;
                        case "Baú do Tesouro": // Plunder Chest -> Baú do Tesouro
                            List<string> theftChoices = new List<string>();
                            if (game.p1.state.getItems().Count > 0 && game.MyPlayerNumber(this) != 1) {
                                theftChoices.Add(game.p1.state.charName());
                            }
                            if (game.p2.state.getItems().Count > 0 && game.MyPlayerNumber(this) != 2) {
                                theftChoices.Add(game.p2.state.charName());
                            }
                            if (game.p3.state.getItems().Count > 0 && game.MyPlayerNumber(this) != 3) {
                                theftChoices.Add(game.p3.state.charName());
                            }
                            if (game.p4.state.getItems().Count > 0 && game.MyPlayerNumber(this) != 4) {
                                theftChoices.Add(game.p4.state.charName());
                            }
                            if (theftChoices.Count > 0) {
                                theftChoices.Add("Cancelar");
                                ui.Dialogue(Prompts.AlvoRouboItem, theftChoices, true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                
                                // Se cancelou, voltar ao menu anterior
                                if (ui.MostRecentDialogueAnswer() == "Cancelar") {
                                    usedItem = false;
                                    break;
                                }
                            }
                            else {
                                ui.Dialogue("Ninguém tem itens para roubar.", true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                usedItem = false;
                                break;
                            }
                            if (ui.MostRecentDialogueAnswer() == game.p1.state.charName()) {
                                state.removeItem(BoardItem.PlunderChest);
                                BoardItem stolenItem = game.p1.state.stealRandomItem();
                                state.addItem(stolenItem);
                                ui.Dialogue("Você roubou " + game.p1.state.charName() + "'s " + ItemSpace.itemNames[(int)stolenItem] + "!", true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p2.state.charName()) {
                                state.removeItem(BoardItem.PlunderChest);
                                BoardItem stolenItem = game.p2.state.stealRandomItem();
                                state.addItem(stolenItem);
                                ui.Dialogue("Você roubou " + game.p2.state.charName() + "'s " + ItemSpace.itemNames[(int)stolenItem] + "!", true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p3.state.charName()) {
                                state.removeItem(BoardItem.PlunderChest);
                                BoardItem stolenItem = game.p3.state.stealRandomItem();
                                state.addItem(stolenItem);
                                ui.Dialogue("Você roubou " + game.p3.state.charName() + "'s " + ItemSpace.itemNames[(int)stolenItem] + "!", true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p4.state.charName()) {
                                state.removeItem(BoardItem.PlunderChest);
                                BoardItem stolenItem = game.p4.state.stealRandomItem();
                                state.addItem(stolenItem);
                                ui.Dialogue("Você roubou " + game.p4.state.charName() + "'s " + ItemSpace.itemNames[(int)stolenItem] + "!", true);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            }
                            break;
                        case "Moeda Dobrada": // Double Star Card -> Moeda Dobrada
                            state.changeCoins(30);
                            state.removeItem(BoardItem.DoubleStarCard);
                            ui.Dialogue("Moeda Dobrada", "Você recebeu +30 Moedas!", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        case "Chave Secreta": // Skeleton Key -> Chave Secreta
                            // Resgatar a chave a consome (antes ficava no bolso e gerava +15 todo turno)
                            state.changeCoins(15);
                            state.removeItem(BoardItem.SkeletonKey);
                            ui.Dialogue("Chave Secreta", "Você vendeu a Chave Secreta por +15 Moedas!", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        case "Escudo Protetor": // Gaddlight -> Escudo Protetor
                            // Resgatar o escudo o consome (antes ficava no bolso e gerava +20 todo turno)
                            state.changeCoins(20);
                            state.removeItem(BoardItem.Gaddlight);
                            ui.Dialogue("Escudo Protetor", "Você resgatou o Escudo Protetor por +20 Moedas!\n(Sem ele, você não está mais protegido contra roubos.)", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        case "Presente Surpresa": // Chomp Treat -> Presente Surpresa
                            state.changeCoins(25);
                            state.removeItem(BoardItem.ChompTreat);
                            ui.Dialogue("Presente Surpresa", "Você ganhou +25 Moedas!", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        case "Sino do Boo":
                            state.removeItem(BoardItem.BooBell);
                            ui.Dialogue("Boo", "Ueeeheehee! Adoro causar confusão! Vou roubar algo de um dos seus rivais!", false);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            yield return new WaitForSeconds(0.1f);
                            bool stealingStars = false;
                            List<string> coinTheftChoices = new List<string>();
                            if (game.p1.state.getCoins() > 0 && game.MyPlayerNumber(this) != 1) {
                                coinTheftChoices.Add(game.p1.state.charName());
                            }
                            if (game.p2.state.getCoins() > 0 && game.MyPlayerNumber(this) != 2) {
                                coinTheftChoices.Add(game.p2.state.charName());
                            }
                            if (game.p3.state.getCoins() > 0 && game.MyPlayerNumber(this) != 3) {
                                coinTheftChoices.Add(game.p3.state.charName());
                            }
                            if (game.p4.state.getCoins() > 0 && game.MyPlayerNumber(this) != 4) {
                                coinTheftChoices.Add(game.p4.state.charName());
                            }
                            List<string> starTheftChoices = new List<string>();
                            if (game.p1.state.getStars() > 0 && game.MyPlayerNumber(this) != 1) {
                                starTheftChoices.Add(game.p1.state.charName());
                            }
                            if (game.p2.state.getStars() > 0 && game.MyPlayerNumber(this) != 2) {
                                starTheftChoices.Add(game.p2.state.charName());
                            }
                            if (game.p3.state.getStars() > 0 && game.MyPlayerNumber(this) != 3) {
                                starTheftChoices.Add(game.p3.state.charName());
                            }
                            if (game.p4.state.getStars() > 0 && game.MyPlayerNumber(this) != 4) {
                                starTheftChoices.Add(game.p4.state.charName());
                            }
                            if (state.getCoins() >= 40 && starTheftChoices.Count > 0) {
                                ui.Dialogue("Boo", "Roubar Moedas é de graça, mas Estrelas custam 40 Moedas!", new List<string>() {"Quero Moedas!", "Quero Estrelas!", "Não, obrigado!"}, false);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                yield return new WaitForSeconds(0.1f);
                                if (ui.MostRecentDialogueAnswer() == "Quero Estrelas!") {
                                    stealingStars = true;
                                }
                                if (ui.MostRecentDialogueAnswer() != "Não, obrigado!") {
                                    if (stealingStars) {
                                        ui.Dialogue("Boo", "De quem eu devo roubar?", starTheftChoices, false);
                                    } else {
                                        ui.Dialogue("Boo", "De quem eu devo roubar?", coinTheftChoices, false);
                                    }
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                }
                            } else if (coinTheftChoices.Count > 0) {
                                coinTheftChoices.Add("Não, obrigado!");
                                ui.Dialogue("Boo", "De quem você quer que eu roube Moedas?", coinTheftChoices, false);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                yield return new WaitForSeconds(0.1f);
                            } else {
                                ui.Dialogue("Boo", "Hmm... ninguém tem nada que valha a pena roubar.", false);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                yield return new WaitForSeconds(0.1f);
                            }
                            // Processar roubo para qualquer jogador alvo
                            Player booTarget = null;
                            string targetName = ui.MostRecentDialogueAnswer();
                            foreach (Player candidate in game.GetPlayers()) {
                                if (candidate.state.charName() == targetName) {
                                    booTarget = candidate;
                                    break;
                                }
                            }
                            if (booTarget != null) {
                                ui.Dialogue("Boo", "Lá vou eu!", false);
                                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                yield return new WaitForSeconds(0.1f);
                                if (booTarget.state.hasItem(BoardItem.Gaddlight)) {
                                    booTarget.state.removeItem(BoardItem.Gaddlight);
                                    ui.Dialogue("Boo", "Argh! Esse jogador tem um Escudo Protetor! Fui!", true);
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                } else if (stealingStars) {
                                    state.changeCoins(-40);
                                    state.changeStars(1);
                                    booTarget.state.changeStars(-1);
                                    ui.Dialogue("Boo", "Roubei uma Estrela! Sou malvado demais!", false);
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                    ui.Dialogue("Boo", "Ueeeheehee! Volte sempre!", true);
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                } else {
                                    int coinsToSteal = GameRng.Range(Mathf.Max(1, booTarget.state.getCoins() / 4), Mathf.Max(2, booTarget.state.getCoins() / 2));
                                    state.changeCoins(coinsToSteal);
                                    booTarget.state.changeCoins(-1 * coinsToSteal);
                                    ui.Dialogue("Boo", "Ueheehee! Roubei " + coinsToSteal + " Moedas!", false);
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                    ui.Dialogue("Boo", "Ueeeheehee! Volte sempre!", true);
                                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                                    yield return new WaitForSeconds(0.1f);
                                }
                            }
                            break;
                        case "Bebida Dourada": {
                            state.removeItem(BoardItem.GoldenDrink);
                            int bonus = 10 + 3 * state.GetCareerLevel();
                            state.changeCoins(bonus);
                            ui.Dialogue("Bebida Dourada", $"Energia total! Bônus de produtividade: +{bonus} Moedas!", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        }
                        case "Mochila Mágica":
                            state.removeItem(BoardItem.VacPack);
                            yield return StartCoroutine(UsarMochilaMagica());
                            break;
                        case "Luva de Duelo": {
                            bool concluiu = false;
                            yield return StartCoroutine(UsarLuvaDeDuelo(ok => concluiu = ok));
                            if (!concluiu) usedItem = false;
                            break;
                        }
                        case "Apito do Chomp":
                            state.removeItem(BoardItem.ChompCall);
                            yield return StartCoroutine(UsarApitoDoChomp());
                            break;
                        case "Lâmpada Mágica": {
                            bool concluiu = false;
                            yield return StartCoroutine(UsarLampadaMagica(ok => concluiu = ok));
                            if (!concluiu) usedItem = false;
                            break;
                        }
                        case "Apito do Wiggler":
                            state.removeItem(BoardItem.WigglerWhistle);
                            yield return StartCoroutine(UsarApitoDoWiggler());
                            break;
                        default:
                            usedItem = false;
                            break;
                    }
                    if (usedItem) {
                        state.UsedItemStatTrigger();
                    }
                    break;
                case "Ver Tabuleiro":
                    yield return StartCoroutine(VerTabuleiro());
                    break;
                case "Rolar Dados":
                default:
                    int mv = state.getMovement();
                    GameObject die = Instantiate(dicePrefabs[mv], transform);
                    rolls = new List<int>();
                    int targetRollCount = (mv == 1) ? 2 : ((mv == 2) ? 3 : ((mv == 5) ? 5 : 1));
                    yield return new WaitUntil(() => (rolls.Count >= targetRollCount));
                    for (int i = 0; i < targetRollCount; i++) {
                        this.rollCount += rolls[i];
                    }
                    ui.SetMoveCounterNumber(this.rollCount);
                    ui.MoveCounter(true);
                    setNextSpace(targetSpace.getNextSpaceInSequence(), false);
                    while (rollCount > 0) {
                        if (transform.position == targetSpace.transform.position) {
                            if (state.getMovement() == 5) {
                                if (game.MyPlayerNumber(this) != 1 && targetSpace.id == game.p1.state.getSpaceID()) {
                                    if (game.p1.state.getCoins() >= 20) {
                                        state.changeCoins(20);
                                        game.p1.state.changeCoins(-20);
                                    } else {
                                        state.changeCoins(game.p1.state.getCoins());
                                        game.p1.state.setCoins(0);
                                    }
                                }
                                if (game.MyPlayerNumber(this) != 2 && targetSpace.id == game.p2.state.getSpaceID()) {
                                    if (game.p2.state.getCoins() >= 20) {
                                        state.changeCoins(20);
                                        game.p2.state.changeCoins(-20);
                                    } else {
                                        state.changeCoins(game.p2.state.getCoins());
                                        game.p2.state.setCoins(0);
                                    }
                                } 
                                if (game.MyPlayerNumber(this) != 3 && targetSpace.id == game.p3.state.getSpaceID()) {
                                    if (game.p3.state.getCoins() >= 20) {
                                        state.changeCoins(20);
                                        game.p3.state.changeCoins(-20);
                                    } else {
                                        state.changeCoins(game.p3.state.getCoins());
                                        game.p3.state.setCoins(0);
                                    }
                                }
                                if (game.MyPlayerNumber(this) != 4 && targetSpace.id == game.p4.state.getSpaceID()) {
                                    if (game.p4.state.getCoins() >= 20) {
                                        state.changeCoins(20);
                                        game.p4.state.changeCoins(-20);
                                    } else {
                                        state.changeCoins(game.p4.state.getCoins());
                                        game.p4.state.setCoins(0);
                                    }
                                }
                            }
                            targetSpace.passHere(this);
                            yield return new WaitUntil(() => targetSpace.donePassingYet());
                            if (targetSpace.ableToLandHere()) {
                                rollCount -= 1;
                                state.MovedSpaceStatTrigger();
                            }
                            if (rollCount != 0) {
                                targetSpace = targetSpace.getNextSpaceInSequence();
                            }
                        } else {
                            transform.position = Vector3.MoveTowards(transform.position, targetSpace.transform.position, moveSpeed * Time.deltaTime);
                            yield return null;
                        }
                        ui.SetMoveCounterNumber(this.rollCount);
                        ui.MoveCounter(true);
                    }
                    targetSpace.landHere(this);
                    state.setMovement(0);
                    state.setSpace(targetSpace.id);
                    yield return new WaitUntil(() => targetSpace.doneLandingYet());
                    state.setSpace(targetSpace.id);

                    // Promoção por experiência (casas percorridas) acontece durante o movimento; anuncia aqui
                    if (state.ConsumirPromocoesExperiencia() > 0) {
                        ui.Dialogue("Experiência de Mercado",
                            $"{state.charName()} acumulou experiência e foi promovido(a)!\nAgora: {state.GetProfissao()} {state.GetCareerTitle()} (Nv. {state.GetCareerLevel()})", true);
                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                    }

                    takingTurn = false;
                    myCam.enabled = false;
                    break;
            }
        }
    }

    public void SetPlayer(PlayerState ps) {
        if (game == null) game = FindObjectOfType<BoardManager>();
        this.state = ps;
        this.setNextSpace(game.GetSpaceFromID(ps.getSpaceID()), true);
        AtualizarBrain();
    }

    public void setNextSpace(BoardSpace b, bool instant) {
        this.targetSpace = b;
        if (instant) {
            transform.position = b.transform.position;
        }
    }

    public void StartTurn() {
        takingTurn = true;
        StartCoroutine(TakeTurn());
    }

    public bool TurnOver() {
        return !takingTurn;
    }

    public void SendRoll(int r) {
        rolls.Add(r);
    }

    public int GetCurrentRollCount() {
        return rollCount;
    }

    // ================== VER TABULEIRO ==================
    // Câmera temporária vista de cima enquadrando todas as casas, com os peões ampliados
    // para ficarem visíveis, sem caixa de diálogo cobrindo o tabuleiro.
    private IEnumerator VerTabuleiro() {
        // Online: só a máquina dona do jogador abre a visão (as outras não teriam quem tocar para sair)
        if (state.getController() == 5) yield break;

        BoardSpace[] casas = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        if (casas.Length == 0) yield break;

        Bounds limites = new Bounds(casas[0].transform.position, Vector3.zero);
        foreach (BoardSpace c in casas) limites.Encapsulate(c.transform.position);

        GameObject camObj = new GameObject("CameraTabuleiro");
        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.depth = 10f;
        float meioFov = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float margem = 6f;
        // Folga de 30% na vertical: o HUD dos jogadores ocupa o topo da tela
        float distancia = Mathf.Max((limites.extents.z + margem) * 1.3f / meioFov,
                                    (limites.extents.x + margem) / (meioFov * Mathf.Max(0.1f, cam.aspect)));
        float alturaVisivel = 2f * distancia * meioFov;
        // Câmera olhando para baixo com "cima da tela" = +Z: deslocar +Z empurra o tabuleiro para baixo do HUD
        camObj.transform.position = limites.center + Vector3.up * distancia + Vector3.forward * (alturaVisivel * 0.08f);
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.farClipPlane = distancia + 200f;
        camObj.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        // Peões ampliados para serem achados de longe
        float fator = Mathf.Clamp(distancia / 12f, 2.5f, 8f);
        var escalas = new Dictionary<Transform, Vector3>();
        foreach (Player pl in game.GetPlayers()) {
            if (pl == null) continue;
            escalas[pl.transform] = pl.transform.localScale;
            pl.transform.localScale = pl.transform.localScale * fator;
        }

        GameObject aviso = CriarAvisoVerTabuleiro();
        if (NetworkTurnManager.Instance != null) NetworkTurnManager.Instance.TimerPausado = true;
        myCam.enabled = false;

        yield return null; // ignora o mesmo clique/toque que escolheu a opção
        while (!(Input.GetKeyDown(KeyCode.Space) || MobileInputManager.IsTouchOrClickDown())) {
            yield return null;
        }

        foreach (var kv in escalas) {
            if (kv.Key != null) kv.Key.localScale = kv.Value;
        }
        Destroy(aviso);
        Destroy(camObj);
        myCam.enabled = true;
        if (NetworkTurnManager.Instance != null) NetworkTurnManager.Instance.TimerPausado = false;
    }

    private GameObject CriarAvisoVerTabuleiro() {
        GameObject canvasObj = new GameObject("AvisoVerTabuleiro");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject faixa = new GameObject("Faixa");
        faixa.transform.SetParent(canvasObj.transform, false);
        var fundo = faixa.AddComponent<UnityEngine.UI.Image>();
        fundo.color = new Color(0.04f, 0.12f, 0.08f, 0.88f);
        fundo.raycastTarget = false;
        RectTransform rt = faixa.GetComponent<RectTransform>();
        // Lateral direita: o tabuleiro fica centralizado e a base da tela tem a barra de itens
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-24f, 0f);
        rt.sizeDelta = new Vector2(250f, 110f);

        GameObject textoObj = new GameObject("Texto");
        textoObj.transform.SetParent(faixa.transform, false);
        var texto = textoObj.AddComponent<UnityEngine.UI.Text>();
        texto.font = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = 24;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.text = "Visão do tabuleiro\n<size=20>Toque para voltar</size>";
        texto.supportRichText = true;
        RectTransform trt = textoObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        return canvasObj;
    }

    // ================== ITENS ==================
    private List<Player> Rivais() => game.GetPlayers().Where(pl => pl != null && pl != this).ToList();

    private static readonly BoardItem[] PoolMochila = {
        BoardItem.Mushroom, BoardItem.GoldenMushroom, BoardItem.PoisonMushroom, BoardItem.SkeletonKey,
        BoardItem.WarpPipe, BoardItem.PlunderChest, BoardItem.TweesterTotem, BoardItem.Gaddlight,
        BoardItem.ChompTreat, BoardItem.DoubleStarCard
    };

    private IEnumerator UsarMochilaMagica() {
        List<string> ganhos = new List<string>();
        for (int i = 0; i < 2 && state.getItems().Count < 3; i++) {
            BoardItem novo = PoolMochila[GameRng.Range(0, PoolMochila.Length)];
            state.addItem(novo);
            ganhos.Add(ItemSpace.itemNames[(int)novo]);
        }
        string texto = ganhos.Count > 0
            ? "Da mochila saíram:\n" + string.Join("\n", ganhos)
            : "A mochila estava vazia... seu bolso já está cheio!";
        ui.Dialogue("Mochila Mágica", texto, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }

    private IEnumerator UsarLuvaDeDuelo(System.Action<bool> concluido) {
        List<Player> rivais = Rivais();
        List<string> nomes = rivais.Select(r => r.state.charName()).ToList();
        nomes.Add(Prompts.Cancelar);
        ui.Dialogue("Luva de Duelo", Prompts.AlvoDuelo, nomes, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        string escolha = ui.MostRecentDialogueAnswer();
        Player rival = rivais.FirstOrDefault(r => r.state.charName() == escolha);
        if (rival == null) { concluido(false); yield break; }

        int aposta = Mathf.Min(15, state.getCoins(), rival.state.getCoins());
        if (aposta <= 0) {
            ui.Dialogue("Luva de Duelo", "Não há Moedas para apostar neste duelo. A luva continua com você.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            concluido(false);
            yield break;
        }

        state.removeItem(BoardItem.DuelingGlove);
        int meuDado = GameRng.Range(1, 11);
        int dadoRival = GameRng.Range(1, 11);
        string resultado;
        if (meuDado > dadoRival) {
            state.changeCoins(aposta);
            rival.state.changeCoins(-aposta);
            resultado = $"Você venceu e levou {aposta} Moedas!";
        } else if (dadoRival > meuDado) {
            state.changeCoins(-aposta);
            rival.state.changeCoins(aposta);
            resultado = $"{rival.state.charName()} venceu e levou {aposta} Moedas!";
        } else {
            resultado = "Empate! Ninguém perde nada.";
        }
        ui.Dialogue("Duelo", $"Aposta: {aposta} Moedas\n{state.charName()}: {meuDado}\n{rival.state.charName()}: {dadoRival}\n\n{resultado}", true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        concluido(true);
    }

    private IEnumerator UsarApitoDoChomp() {
        int total = 0;
        foreach (Player r in Rivais()) {
            int cobranca = Mathf.Min(5, r.state.getCoins());
            r.state.changeCoins(-cobranca);
            total += cobranca;
        }
        state.changeCoins(total);
        ui.Dialogue("Apito do Chomp", $"O Chomp fez a cobrança das dívidas!\nVocê recebeu {total} Moedas dos rivais.", true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }

    private IEnumerator UsarLampadaMagica(System.Action<bool> concluido) {
        ClassicStarSpace[] estrelas = FindObjectsByType<ClassicStarSpace>(FindObjectsSortMode.None);
        if (estrelas.Length == 0) {
            ui.Dialogue("Lâmpada Mágica", "Não há Estrela neste tabuleiro. A lâmpada continua com você.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            concluido(false);
            yield break;
        }

        state.removeItem(BoardItem.MagicLamp);
        ClassicStarSpace alvo = estrelas[GameRng.Range(0, estrelas.Length)];
        ui.Dialogue("Lâmpada Mágica", "Puf! O gênio levou você direto até a Estrela!", true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        // A Estrela é uma casa de passagem: a compra acontece em pass(), não em land()
        setNextSpace(alvo, true);
        alvo.passHere(this);
        yield return new WaitUntil(() => alvo.donePassingYet());
        concluido(true);
    }

    private IEnumerator UsarApitoDoWiggler() {
        string texto;
        switch (GameRng.Range(0, 3)) {
            case 0:
                state.changeCoins(25);
                texto = "Ganho inesperado: +25 Moedas!";
                break;
            case 1:
                int aumento = 5 * state.GetCareerLevel();
                state.SetBaseSalary(state.GetSalary() + aumento);
                texto = $"Aumento surpresa: salário +{aumento} Moedas/turno!";
                break;
            default:
                if (state.getItems().Count < 3) {
                    BoardItem novo = PoolMochila[GameRng.Range(0, PoolMochila.Length)];
                    state.addItem(novo);
                    texto = $"Presente raro: você ganhou {ItemSpace.itemNames[(int)novo]}!";
                } else {
                    state.changeCoins(25);
                    texto = "Ganho inesperado: +25 Moedas!";
                }
                break;
        }
        ui.Dialogue("Apito do Wiggler", texto, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }
}
