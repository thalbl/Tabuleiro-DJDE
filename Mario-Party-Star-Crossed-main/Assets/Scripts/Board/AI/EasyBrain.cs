using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EasyBrain : AIBrain {
    private BoardItem itemToUse;
    private int numberToRoll;
    private string playerToTarget;

    public EasyBrain(PlayerState state, PlayerState rival1, PlayerState rival2, PlayerState rival3, BoardManager game) : base(state, rival1, rival2, rival3, game) {
        numberToRoll = 0;
        playerToTarget = "";
    }

    public override string Prompt(string question, List<string> options, int spacesLeft) {
        switch (question) {
            case "O que você fará?":
                if (options.Contains("Usar Item") || state.getMovement() == 3) {
                    Dictionary<string, float> turnChoices = new Dictionary<string, float>();
                    float basicMovementValue = 0.0f;
                    switch (state.getMovement()) {
                        case 1:
                            float v2d10 = 0.0f;
                            for (int i = 2; i <= 20; i++) {
                                v2d10 += GetValueOfRolling(i) * rollPercentages[0][i - 2];
                            }
                            turnChoices.Add("Roll 2d10", v2d10);
                            basicMovementValue = v2d10;
                            break;
                        case 2:
                            float v3d10 = 0.0f;
                            for (int i = 3; i <= 30; i++) {
                                v3d10 += GetValueOfRolling(i) * rollPercentages[1][i - 3];
                            }
                            turnChoices.Add("Roll 3d10", v3d10);
                            basicMovementValue = v3d10;
                            break;
                        case 3:
                            for (int i = 1; i <= 10; i++) {
                                turnChoices.Add("Magic Roll " + i, GetValueOfRolling(i));
                            }
                            float avgMRoll = 0.0f;
                            for (int i = 1; i <= 10; i++) {
                                avgMRoll += GetValueOfRolling(i) / 10.0f;
                            }
                            basicMovementValue = avgMRoll;
                            break;
                        case 4:
                            float v1d3 = 0.0f;
                            for (int i = 1; i <= 3; i++) {
                                v1d3 += GetValueOfRolling(i) / 3.0f;
                            }
                            turnChoices.Add("Roll 1d3", v1d3);
                            basicMovementValue = v1d3;
                            break;
                        case 5:
                            float v5d3 = 0.0f;
                            for (int i = 5; i <= 15; i++) {
                                v5d3 += (GetValueOfRolling(i) + (15 * game.GetSpaceFromID(state.getSpaceID()).PlayersInRange(i) - 1)) * rollPercentages[2][i - 5];
                            }
                            turnChoices.Add("Roll 5d3", v5d3);
                            basicMovementValue = v5d3;
                            break;
                        default:
                            float v1d10 = 0.0f;
                            for (int i = 1; i <= 10; i++) {
                                v1d10 += GetValueOfRolling(i) / 10.0f;
                            }
                            turnChoices.Add("Roll 1d10", v1d10);
                            basicMovementValue = v1d10;
                            break;
                    }
                    foreach (BoardItem bi in state.getItems()) {
                        switch (bi) {
                            case BoardItem.Mushroom:
                                if (!turnChoices.ContainsKey("Roll 2d10")) {
                                    float v2d10 = 0.0f;
                                    for (int i = 2; i <= 20; i++) {
                                        v2d10 += GetValueOfRolling(i) * rollPercentages[0][i - 2];
                                    }
                                    turnChoices.Add("Roll 2d10", v2d10);
                                }
                                break;
                            case BoardItem.GoldenMushroom:
                                if (!turnChoices.ContainsKey("Roll 3d10")) {
                                    float v3d10 = 0.0f;
                                    for (int i = 3; i <= 30; i++) {
                                        v3d10 += GetValueOfRolling(i) * rollPercentages[1][i - 3];
                                    }
                                    turnChoices.Add("Roll 3d10", v3d10);
                                }
                                break;
                            case BoardItem.BowserSuit:
                                if (!turnChoices.ContainsKey("Roll 5d3")) {
                                    float v5d3 = 0.0f;
                                    for (int i = 5; i <= 15; i++) {
                                        v5d3 += (GetValueOfRolling(i) + (15 * game.GetSpaceFromID(state.getSpaceID()).PlayersInRange(i) - 1)) * rollPercentages[2][i - 5];
                                    }
                                    turnChoices.Add("Roll 5d3", v5d3);
                                }
                                break;
                            case BoardItem.PoisonMushroom:
                                if (!turnChoices.ContainsKey("Roll 1d3")) {
                                    float v1d3 = 0.0f;
                                    for (int i = 1; i <= 3; i++) {
                                        v1d3 += GetValueOfRolling(i) / 3.0f;
                                    }
                                    turnChoices.Add("Roll 1d3", v1d3);
                                }
                                if ((game.p1.state.charName() == state.charName()) ? (!turnChoices.ContainsKey("Sabotage " + game.p2.state.charName())) : (!turnChoices.ContainsKey("Sabotage " + game.p1.state.charName()))) {
                                    float dg10t3 = 0.0f;
                                    foreach (Player r in game.GetPlayers()) {
                                        if (r.state.charName() != state.charName()) {
                                            dg10t3 = basicMovementValue;
                                            for (int i = 1; i <= 10; i++) {
                                                dg10t3 += r.brain.GetValueOfRolling(i) / 10.0f;
                                            }
                                            for (int i = 1; i <= 3; i++) {
                                                dg10t3 -= r.brain.GetValueOfRolling(i) / 3.0f;
                                            }
                                            turnChoices.Add("Sabotage " + r.state.charName(), dg10t3);
                                        }
                                    }
                                }
                                break;
                            case BoardItem.MagicMushroom:
                                if (!turnChoices.ContainsKey("Magic Roll 1")) {
                                    for (int i = 1; i <= 10; i++) {
                                        turnChoices.Add("Magic Roll " + i, GetValueOfRolling(i));
                                    }
                                }
                                break;
                            case BoardItem.BooBell:
                                if (!turnChoices.ContainsKey("Boo Bell")) {
                                    if (state.getCoins() >= 40 && rivals.Any(p => p.getStars() >= 1)) {
                                        turnChoices.Add("Boo Bell", basicMovementValue + 30);
                                    } else {
                                        turnChoices.Add("Boo Bell", basicMovementValue + 15);
                                    }
                                }
                                break;
                            case BoardItem.GoldenDrink:
                                if (!turnChoices.ContainsKey("Golden Drink")) {
                                    turnChoices.Add("Golden Drink", basicMovementValue + 11);
                                }
                                break;
                            case BoardItem.VacPack:
                                if (!turnChoices.ContainsKey("Vac Pack")) {
                                    turnChoices.Add("Vac Pack", basicMovementValue + 15);
                                }
                                break;
                            case BoardItem.DuelingGlove:
                                if (!turnChoices.ContainsKey("Dueling Glove")) {
                                    if (state.getPlacing() == 1) {
                                        turnChoices.Add("Dueling Glove", basicMovementValue - 5);
                                    } else {
                                        turnChoices.Add("Dueling Glove", basicMovementValue + 15);
                                    }
                                }
                                break;
                            // Novos itens que antes eram stubs
                            case BoardItem.DoubleStarCard:
                                if (!turnChoices.ContainsKey("Dia de Sorte")) {
                                    turnChoices.Add("Dia de Sorte", basicMovementValue + 20);
                                }
                                break;
                            case BoardItem.ChompTreat:
                                if (!turnChoices.ContainsKey("Presente Surpresa")) {
                                    turnChoices.Add("Presente Surpresa", basicMovementValue + 18);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    KeyValuePair<string, float> decision = new KeyValuePair<string, float>("DUMMY", -420.69f);
                    foreach (KeyValuePair<string, float> kvp in turnChoices) {
                        if (decision.Key == "DUMMY" || kvp.Value > decision.Value) {
                            decision = kvp;
                        }
                    }
                    if (decision.Key == "Roll 1d10") {
                        return "Rolar Dados";
                    } else if (decision.Key == "Roll 2d10") {
                        if (state.getMovement() == 1) {
                            return "Rolar Dados";
                        } else {
                            itemToUse = BoardItem.Mushroom;
                            return "Usar Item";
                        }
                    } else if (decision.Key == "Roll 3d10") {
                        if (state.getMovement() == 2) {
                            return "Rolar Dados";
                        } else {
                            itemToUse = BoardItem.GoldenMushroom;
                            return "Usar Item";
                        }
                    } else if (decision.Key.Length >= 11 && decision.Key.Substring(0, 11) == "Magic Roll ") {
                        numberToRoll = int.Parse(decision.Key.Substring(11));
                        if (state.getMovement() == 3) {
                            return "Rolar Dados";
                        } else {
                            itemToUse = BoardItem.MagicMushroom;
                            return "Usar Item";
                        }
                    } else if (decision.Key == "Roll 1d3") {
                        if (state.getMovement() == 4) {
                            return "Rolar Dados";
                        } else {
                            itemToUse = BoardItem.PoisonMushroom;
                            playerToTarget = state.charName();
                            return "Usar Item";
                        }
                    } else if (decision.Key == "Roll 5d3") {
                        if (state.getMovement() == 5) {
                            return "Rolar Dados";
                        } else {
                            itemToUse = BoardItem.BowserSuit;
                            return "Usar Item";
                        }
                    } else if (decision.Key.Length >= 9 && decision.Key.Substring(0, 9) == "Sabotage ") {
                        playerToTarget = decision.Key.Substring(9);
                        itemToUse = BoardItem.PoisonMushroom;
                        return "Usar Item";
                    } else if (decision.Key == "Dia de Sorte") {
                        itemToUse = BoardItem.DoubleStarCard;
                        return "Usar Item";
                    } else if (decision.Key == "Presente Surpresa") {
                        itemToUse = BoardItem.ChompTreat;
                        return "Usar Item";
                    } else if (ItemSpace.itemNames.Contains(decision.Key)) {
                        itemToUse = (BoardItem) ItemSpace.itemNames.IndexOf(decision.Key);
                        return "Usar Item";
                    }
                }
                return "Rolar Dados";
            case "Qual item você deseja usar?":
                return ItemSpace.itemNames[(int) itemToUse];
            case "Em quem você quer aplicar a Dívida?":
            case "Em quem você quer usar o Bilhete de Loteria?":
            case "De quem você quer roubar um item?":
                return playerToTarget;
            case "Escolha o Caminho":
                int pNum = game.MyPlayerNumber(state);
                Player pl;
                if (pNum == 1) {
                    pl = game.p1;
                } else if (pNum == 2) {
                    pl = game.p2;
                } else if (pNum == 3) {
                    pl = game.p3;
                } else {
                    pl = game.p4;
                }
                Intersection fork = null;
                foreach (GameObject i in GameObject.FindGameObjectsWithTag("Intersection")) {
                    if (fork == null || Vector3.Distance(i.transform.position, pl.transform.position) < Vector3.Distance(fork.transform.position, pl.transform.position)) {
                        fork = i.GetComponent<Intersection>();
                    }
                }
                int nextValue;
                int optionValue;
                if (fork.gate == null || state.getItems().Contains(BoardItem.SkeletonKey) || state.getMovement() == 5) {
                    nextValue = fork.next.CumulativeValue(state, rivals, spacesLeft);
                } else {
                    nextValue = -2147483648;
                }
                if (fork.optionGate == null || state.getItems().Contains(BoardItem.SkeletonKey) || state.getMovement() == 5) {
                    optionValue = fork.option.CumulativeValue(state, rivals, spacesLeft);
                } else {
                    optionValue = -2147483648;
                }
                return (nextValue > optionValue) ? "Next" : "Option";
            case "Escolha o Dado Mágico":
                Debug.Log("Choosing to Roll " + numberToRoll);
                return "" + numberToRoll;
            default:
                if (options != null && options.Count > 0) {
                    return options[0];
                }
                return "";
        }
    }
}
