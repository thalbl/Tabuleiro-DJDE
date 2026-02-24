using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassicStarSpace : BoardSpace {
    public override IEnumerator pass(Player p) {
        this.canLandHere = false;
        donePassing = false;
        ui.MoveCounter(false);
        if (p.state.hasItem(BoardItem.DoubleStarCard) && p.state.getCoins() >= 40) {
            ui.Dialogue("Oh, sorte a sua! Você tem um Cartão de Renda Extra! Gostaria de comprar 2 Estrelas por 40 Moedas?", new List<string>() { "40 Moedas => 2 Estrelas", "20 Moedas => 1 Estrela", "Não, obrigado" }, true);
        }
        else if (p.state.getCoins() >= 20) {
            ui.Dialogue("Gostaria de comprar uma Estrela por 20 Moedas?", new List<string>() { "20 Moedas => 1 Estrela", "Não, obrigado" }, true);
        }
        else {
            ui.Dialogue("Você não tem moedas suficientes para comprar uma Estrela.", true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        if (ui.MostRecentDialogueAnswer() == "40 Moedas => 2 Estrelas") {
            p.state.changeCoins(-40);
            p.state.changeStars(2);
            p.state.removeItem(BoardItem.DoubleStarCard);
            ui.Dialogue("Você comprou 2 Estrelas! Boa jogada!", true);
        }
        else if (ui.MostRecentDialogueAnswer() == "20 Moedas => 1 Estrela") {
            p.state.changeCoins(-20);
            p.state.changeStars(1);
            ui.Dialogue("Você comprou 1 Estrela! Continue assim!", true);
        }
        else {
            ui.Dialogue("Você decidiu não comprar nenhuma Estrela.", true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        donePassing = true;
        ui.MoveCounter(true);
    }

public override int SpacesToStar(bool withKey) {
        return 0;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        if (state.hasItem(BoardItem.DoubleStarCard) && state.getCoins() >= 40) {
            return 40;
        } else if (state.getCoins() >= 20) {
            return 20;
        } else {
            return -5;
        }
    }
}
