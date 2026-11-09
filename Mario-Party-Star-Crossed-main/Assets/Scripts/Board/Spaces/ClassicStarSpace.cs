using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassicStarSpace : BoardSpace {
    // Preço progressivo: a 1ª estrela custa 20, cada estrela que o jogador já tem soma +10.
    // Antes era sempre 20 Moedas por 100 pontos (5 pontos por moeda), o que tornava todo o resto irrelevante.
    public const int PrecoBase = 20;
    public const int AcrescimoPorEstrela = 10;

    public static int PrecoEstrela(PlayerState s) => PrecoBase + AcrescimoPorEstrela * s.getStars();
    public static int PrecoDuasEstrelas(PlayerState s) => PrecoEstrela(s) * 2 + AcrescimoPorEstrela;

    public override IEnumerator pass(Player p) {
        this.canLandHere = false;
        donePassing = false;
        ui.MoveCounter(false);
        int preco1 = PrecoEstrela(p.state);
        int preco2 = PrecoDuasEstrelas(p.state);
        string opcao1 = $"{preco1} Moedas => 1 Estrela";
        string opcao2 = $"{preco2} Moedas => 2 Estrelas";
        if (p.state.hasItem(BoardItem.DoubleStarCard) && p.state.getCoins() >= preco2) {
            ui.Dialogue("Estrela", $"Você tem um Cartão de Renda Extra! Comprar 2 Estrelas por {preco2} Moedas?", new List<string>() { opcao2, opcao1, "Não, obrigado" }, true);
        }
        else if (p.state.getCoins() >= preco1) {
            ui.Dialogue("Estrela", $"Comprar uma Estrela por {preco1} Moedas?\n(Cada Estrela vale 100 pontos; a próxima custará {preco1 + AcrescimoPorEstrela}.)", new List<string>() { opcao1, "Não, obrigado" }, true);
        }
        else {
            ui.Dialogue("Estrela", $"Uma Estrela custa {preco1} Moedas para você. Junte mais e volte!", true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        string resposta = ui.MostRecentDialogueAnswer();
        if (resposta == opcao2) {
            p.state.changeCoins(-preco2);
            p.state.changeStars(2);
            p.state.removeItem(BoardItem.DoubleStarCard);
            ui.Dialogue("Estrela", "Você comprou 2 Estrelas! Boa jogada!", true);
        }
        else if (resposta == opcao1) {
            p.state.changeCoins(-preco1);
            p.state.changeStars(1);
            ui.Dialogue("Estrela", "Você comprou 1 Estrela! Continue assim!", true);
        }
        else {
            ui.Dialogue("Estrela", "Você decidiu não comprar nenhuma Estrela.", true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        donePassing = true;
        ui.MoveCounter(true);
    }

    public override int SpacesToStar(bool withKey) {
        return 0;
    }

    public override int SpacesToStar(bool withKey, int depth) {
        return 0;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        if (state.hasItem(BoardItem.DoubleStarCard) && state.getCoins() >= PrecoDuasEstrelas(state)) {
            return 40;
        } else if (state.getCoins() >= PrecoEstrela(state)) {
            return 20;
        } else {
            return -5;
        }
    }
}
