using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EducationSpace : BoardSpace {
    public int educationCost = 75;
    public int educationReward = 1;

    public override IEnumerator land(Player p) {
        doneLanding = false;

        if (p.state.getCoins() >= educationCost) {
            p.state.changeCoins(-educationCost);
            p.state.IncreaseEducation();

            ui.Dialogue("Educação",
                $"Você investiu {educationCost} Moedas em educação!\nNível atual: {p.state.GetEducationLevel()}", true);
        }
        else {
            ui.Dialogue("Sem Moedas",
                $"Necessário: {educationCost} Moedas\nSeu saldo: {p.state.getCoins()} Moedas", true);
        }

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return (state.getCoins() > educationCost * 2) ? 30 : 5;
    }
}
