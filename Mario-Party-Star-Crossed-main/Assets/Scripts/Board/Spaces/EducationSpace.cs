using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EducationSpace : BoardSpace {
    public int educationCost = 75;

    private static readonly string[] NiveisEducacao = {
        "Sem formalidade",
        "Ensino Medio",
        "Ensino Superior",
        "Pos-graduacao"
    };

    private string GetNivelNome(int nivel) {
        if (nivel >= 0 && nivel < NiveisEducacao.Length)
            return NiveisEducacao[nivel];
        return "Avancada";
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;

        int nivelAtual = p.state.GetEducationLevel();

        // Já está no nível máximo
        if (nivelAtual >= 3) {
            ui.Dialogue("Universidade",
                "Parabens! Voce ja concluiu a Pos-graduacao!\n" +
                "Nao ha mais niveis de educacao disponiveis.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            doneLanding = true;
            yield break;
        }

        // Sem moedas suficientes
        if (p.state.getCoins() < educationCost) {
            ui.Dialogue("Universidade",
                $"Investir em educacao custa {educationCost} Moedas.\n" +
                $"Seu saldo: {p.state.getCoins()} Moedas\n\n" +
                "Voce nao tem Moedas suficientes.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            doneLanding = true;
            yield break;
        }

        // Oferecer a escolha ao jogador
        string proximoNivel = GetNivelNome(nivelAtual + 1);
        ui.Dialogue("Universidade",
            $"Investir {educationCost} Moedas em educacao?\n\n" +
            $"Nivel atual: {GetNivelNome(nivelAtual)}\n" +
            $"Proximo: {proximoNivel}\n\n" +
            $"(Educacao ajuda na sua pontuacao final!)",
            new List<string>() { "Investir!", "Nao agora" }, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        if (ui.MostRecentDialogueAnswer() == "Investir!") {
            p.state.changeCoins(-educationCost);
            p.state.IncreaseEducation();

            ui.Dialogue("Formatura!",
                $"Voce agora tem: {proximoNivel}!\n" +
                $"-{educationCost} Moedas investidas\n" +
                $"+75 pontos na pontuacao final!", true);
        } else {
            ui.Dialogue("Universidade",
                "Voce decidiu economizar por enquanto.\n" +
                "A universidade estara aqui quando voltar!", true);
        }

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        // IA considera: tem moedas suficientes e ainda pode subir de nível?
        if (state.GetEducationLevel() >= 3) return 0;
        return (state.getCoins() > educationCost * 2) ? 30 : 5;
    }
}
