using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemShop : BoardSpace {
        // Preços em Moedas (unificado)
        private static readonly Dictionary<BoardItem, int> FixedPrices = new Dictionary<BoardItem, int>() {
        { BoardItem.Mushroom, 2 },
        { BoardItem.PoisonMushroom, 2 },
        { BoardItem.SkeletonKey, 4 },
        { BoardItem.WarpPipe, 6 },
        { BoardItem.PlunderChest, 7 },
        { BoardItem.GoldenDrink, 6 },
        { BoardItem.GoldenMushroom, 8 },
        { BoardItem.MagicMushroom, 12 },
        { BoardItem.VacPack, 5 },
        { BoardItem.TweesterTotem, 5 },
        { BoardItem.DuelingGlove, 7 },
        { BoardItem.BowserSuit, 10 },
        { BoardItem.Gaddlight, 4 },
        { BoardItem.BooBell, 9 },
        { BoardItem.ChompCall, 3 },
        { BoardItem.MagicLamp, 15 },
        { BoardItem.DoubleStarCard, 12 },
        { BoardItem.ChompTreat, 3 },
        { BoardItem.WigglerWhistle, 8 }
    };

        public bool booItemsAllowed = true;
        public bool chompCallAllowed = true;
        public bool magicLampAllowed = true;
        public bool doubleStarCardAllowed = true;
        public bool chompTreatAllowed = false;
        public bool wigglerWhistleAllowed = false;

        private List<BoardItem> bucketA;
        private List<BoardItem> bucketB;
        private List<BoardItem> bucketC;

    public override void setup() {
        this.canLandHere = false;
        // Definição dos tiers com os itens originais e correspondências financeiras
        List<BoardItem> tier5 = new List<BoardItem>() {
            BoardItem.Mushroom,       // Corresponde a "Poupança"
            BoardItem.PoisonMushroom, // Corresponde a "Dívida"
            BoardItem.SkeletonKey    // Corresponde a "Fundo de Emergência"
        };
        List<BoardItem> tier4 = new List<BoardItem>() {
            BoardItem.WarpPipe,      // Corresponde a "Fundo de Investimento"
            BoardItem.PlunderChest,  // Corresponde a "Baú do Tesouro" (investimento de alto risco)
            BoardItem.GoldenDrink    // Corresponde a "Oportunidade de Ouro"
        };
        List<BoardItem> tier3 = new List<BoardItem>() {
            BoardItem.GoldenMushroom, // Corresponde a "Bolsa de Valores"
            BoardItem.MagicMushroom,  // Corresponde a "Imóveis"
            BoardItem.VacPack,        // Corresponde a "Plano de Aposentadoria"
            BoardItem.TweesterTotem   // Corresponde a "Bilhete de Loteria"
        };
        List<BoardItem> tier2 = new List<BoardItem>() {
            BoardItem.DuelingGlove,   // Corresponde a "Habilidade de Negociação"
            BoardItem.BowserSuit      // Corresponde a "Traje de Negócios"
        };
        List<BoardItem> tier1 = new List<BoardItem>();
        if (booItemsAllowed) {
            tier3.Add(BoardItem.Gaddlight); // Corresponde a "Consultor Financeiro"
            tier1.Add(BoardItem.BooBell);   // Corresponde a "Empréstimo"
        };
        if (chompCallAllowed) {
            tier2.Add(BoardItem.ChompCall); // Corresponde a "Cartão de Crédito"
        }
        if (magicLampAllowed) {
            tier1.Add(BoardItem.MagicLamp); // Corresponde a "Investimento Mágico" (alto retorno)
        }
        if (doubleStarCardAllowed) {
            tier1.Add(BoardItem.DoubleStarCard); // Corresponde a "Renda Extra"
        }
        if (chompTreatAllowed) {
            tier2.Add(BoardItem.ChompTreat); // Corresponde a "Restituição de Impostos"
        }
        if (wigglerWhistleAllowed) {
            tier3.Add(BoardItem.WigglerWhistle); // Corresponde a "Ganho Inesperado"
        }

        bucketA = new List<BoardItem>();
        bucketB = new List<BoardItem>();
        bucketC = new List<BoardItem>();

        bucketA.AddRange(tier5);
        bucketA.AddRange(tier4);
        bucketB.AddRange(tier3);
        bucketC.AddRange(tier3);
        bucketC.AddRange(tier2);
        bucketC.AddRange(tier1);
    }

    public override IEnumerator pass(Player p) {
        if (game.state.getTurnStatus() != 4) {
            donePassing = false;
            ui.MoveCounter(false);

            int dinheiro = p.state.GetDinheiro();
            if (dinheiro < 2) {
                ui.Dialogue("Loja", "Você não tem Moedas suficientes. Volte quando tiver mais!", true);
                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                ui.MoveCounter(true);
                donePassing = true;
                yield break;
            }
            else {
                // Gera lista de escolhas por preços fixos
                List<BoardItem> possible = new List<BoardItem>(bucketA);
                List<BoardItem> choices = new List<BoardItem>();

                // Seleciona até 4 opções aleatórias que o jogador pode pagar
                Shuffle(possible);
                foreach (var bi in possible) {
                    int price = FixedPrices.ContainsKey(bi) ? FixedPrices[bi] : 5;
                    if (price <= dinheiro) {
                        choices.Add(bi);
                        if (choices.Count >= 4) break;
                    }
                }

                bool shopping = true;
                while (shopping) {
                    // Prepara nomes visuais e envia descrições para hover/seleção
                    List<string> choiceNames = new List<string>();
                    List<string> descs = new List<string>();
                    foreach (BoardItem bi in choices) {
                        int price = FixedPrices.ContainsKey(bi) ? FixedPrices[bi] : 5;
                        choiceNames.Add(ItemSpace.itemNames[(int)bi] + " - " + price + " Moedas");
                        descs.Add(ItemSpace.itemDescriptions[(int)bi]);
                    }
                    choiceNames.Add("Não, obrigado.");
                    descs.Add(""); // Descrição vazia para "Não, obrigado."
                    ui.SetOptionDescriptions(descs);

                    ui.Dialogue("Loja", "Bem-vindo à Loja! O que você gostaria de comprar?", choiceNames, false);
                    yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

                    int answerIndex = choiceNames.IndexOf(ui.MostRecentDialogueAnswer());
                    if (answerIndex < 0 || answerIndex >= choices.Count) {
                        ui.Dialogue("Loja", "Tudo bem. Volte sempre!", true);
                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                        ui.MoveCounter(true);
                        donePassing = true;
                        yield break;
                    }
                    else {
                        BoardItem selected = choices[answerIndex];
                        int cost = FixedPrices.ContainsKey(selected) ? FixedPrices[selected] : 5;
                        // Confirmação de compra com descrição
                        ui.Dialogue("Confirmar Compra", ItemSpace.itemNames[(int)selected] + " por " + cost + " Moedas?\n" + ItemSpace.itemDescriptions[(int)selected], new List<string>() {"Comprar", "Cancelar"}, true);
                        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                        
                        // Se cancelou, voltar ao menu de seleção de itens
                        if (ui.MostRecentDialogueAnswer() == "Cancelar") {
                            // Continuar o loop para voltar ao menu
                            continue;
                        }
                        
                        if (ui.MostRecentDialogueAnswer() == "Comprar" && cost <= p.state.GetDinheiro()) {
                            p.state.SetDinheiro(p.state.GetDinheiro() - cost);
                            yield return StartCoroutine(GivePlayerItem(p, selected, false));
                            yield return new WaitForSeconds(0.1f);
                            ui.Dialogue("Loja", "Compra realizada!", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            ui.MoveCounter(true);
                            donePassing = true;
                            shopping = false; // Sair do loop
                            yield break;
                        } else {
                            ui.Dialogue("Loja", "Compra cancelada.", true);
                            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
                            ui.MoveCounter(true);
                            donePassing = true;
                            shopping = false; // Sair do loop
                            yield break;
                        }
                    }
                }
            }
        }
        // fallback
        ui.MoveCounter(true);
        donePassing = true;
    }

    // Helper para embaralhar lista
    private void Shuffle<T>(List<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 10;
    }
}
