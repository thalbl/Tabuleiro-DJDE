using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        ui = FindObjectOfType<UIManager>();
        game = FindObjectOfType<BoardManager>();
        myCam = GetComponentsInChildren<Camera>()[0];
        myCam.enabled = false;
        // Moeda unificada: não precisa mais inicializar dinheiro separado
        List<PlayerState> rivals = new List<PlayerState>() { game.p1.state, game.p2.state, game.p3.state, game.p4.state };
        rivals.Remove(state);
        if (state.getController() == 0) {
            brain = new PlayerBrain(state, rivals[0], rivals[1], rivals[2], game);
        } else {
            brain = new EasyBrain(state, rivals[0], rivals[1], rivals[2], game);
        }
    }

    // Update is called once per frame
    void Update() {

    }

    public IEnumerator TakeTurn() {
        ui.SetController(state.getController());
        ui.SetDecisionAI(brain);
        myCam.enabled = true;

        // Avisos de promoção passiva e despesas extras serão exibidos após o jogador concluir suas ações.

        ui.YourTurn(state.charName(), state.charColor());
        usedItem = false;
        // Salário simplificado: deposita direto em moedas
        int salario = state.GetSalary();
        state.changeCoins(salario);

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        while (takingTurn) {
            // Limpar descrições de itens antes de mostrar o menu principal
            ui.ClearOptionDescriptions();
            
            if (usedItem || state.getItems().Count == 0) {
                ui.Dialogue("O que você fará?", new List<string>() { "Rolar Dados", "Ver Tabuleiro" }, true);
            }
            else {
                ui.Dialogue("O que você fará?", new List<string>() { "Rolar Dados", "Usar Item", "Ver Tabuleiro" }, true);
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
                    ui.Dialogue("Qual item você deseja usar?", itemOptions, true);
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
                            ui.Dialogue("Em quem você quer jogar o Buraco no Bolso?", new List<string>() { game.p1.state.charName(), game.p2.state.charName(), game.p3.state.charName(), game.p4.state.charName(), "Cancelar" }, true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            if (ui.MostRecentDialogueAnswer() == game.p1.state.charName()) {
                                game.p1.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p1.state.charName() + " caiu no Buraco no Bolso! Perdeu 4 passos.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p2.state.charName()) {
                                game.p2.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p2.state.charName() + " caiu no Buraco no Bolso! Perdeu 4 passos.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p3.state.charName()) {
                                game.p3.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p3.state.charName() + " caiu no Buraco no Bolso! Perdeu 4 passos.", true);
                            }
                            else if (ui.MostRecentDialogueAnswer() == game.p4.state.charName()) {
                                game.p4.state.setMovement(4);
                                state.removeItem(BoardItem.PoisonMushroom);
                                ui.Dialogue(game.p4.state.charName() + " caiu no Buraco no Bolso! Perdeu 4 passos.", true);
                            }
                            else {
                                usedItem = false;
                            }
                            break;
                        case "Ventania": // Tweester Totem -> Ventania
                            ui.Dialogue("Em quem você quer usar a Ventania?", new List<string>() { game.p1.state.charName(), game.p2.state.charName(), game.p3.state.charName(), game.p4.state.charName(), "Cancelar" }, true);
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
                                int swapWith = game.MyPlayerNumber(this) + Random.Range(1, 3);
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
                                ui.Dialogue("De quem você quer roubar um item?", theftChoices, true);
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
                            state.changeCoins(15);
                            ui.Dialogue("Chave Secreta", "Você sacou +15 Moedas!\n(Ainda pode abrir portões secretos.)", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            break;
                        case "Escudo Protetor": // Gaddlight -> Escudo Protetor
                            state.changeCoins(20);
                            ui.Dialogue("Escudo Protetor", "+20 Moedas!\n(Você está protegido contra o Boo!)", true);
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
                                    int coinsToSteal = Random.Range(Mathf.Max(1, booTarget.state.getCoins() / 4), Mathf.Max(2, booTarget.state.getCoins() / 2));
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
                        default:
                            usedItem = false;
                            break;
                    }
                    if (usedItem) {
                        state.UsedItemStatTrigger();
                    }
                    break;
                /*
                case "View Board":
                    //TODO: Implement
                */
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



                    takingTurn = false;
                    myCam.enabled = false;
                    break;
            }
        }
    }

    public void SetPlayer(PlayerState ps) {
        this.state = ps;
        this.setNextSpace(game.GetSpaceFromID(ps.getSpaceID()), true);
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
}
