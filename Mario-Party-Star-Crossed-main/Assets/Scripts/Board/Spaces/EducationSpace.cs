using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EducationSpace : BoardSpace {
    public int educationCost = 75;

    private static readonly string[] NiveisEducacao = {
        "Sem formalidade",
        "Ensino Médio",
        "Ensino Superior",
        "Pós-graduação"
    };

    private string GetNivelNome(int nivel) {
        if (nivel >= 0 && nivel < NiveisEducacao.Length)
            return NiveisEducacao[nivel];
        return "Avançada";
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;

        int nivelAtual = p.state.GetEducationLevel();

        // Já está no nível máximo
        if (nivelAtual >= 3) {
            ui.Dialogue("Universidade",
                "Parabéns! Você já concluiu a Pós-graduação!\n" +
                "Não há mais níveis de educação disponíveis.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            doneLanding = true;
            yield break;
        }

        // Sem moedas suficientes
        if (p.state.getCoins() < educationCost) {
            ui.Dialogue("Universidade",
                $"Investir em educação custa {educationCost} Moedas.\n" +
                $"Seu saldo: {p.state.getCoins()} Moedas\n\n" +
                "Você não tem Moedas suficientes.", true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            doneLanding = true;
            yield break;
        }

        // Oferecer a escolha ao jogador
        string proximoNivel = GetNivelNome(nivelAtual + 1);
        ui.Dialogue("Universidade",
            $"Investir {educationCost} Moedas em educação?\n\n" +
            $"Nível atual: {GetNivelNome(nivelAtual)}\n" +
            $"Próximo: {proximoNivel}\n\n" +
            $"(Educação ajuda na sua pontuação final!)",
            new List<string>() { "Investir!", "Agora não" }, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        if (ui.MostRecentDialogueAnswer() == "Investir!") {
            p.state.changeCoins(-educationCost);
            p.state.IncreaseEducation();

            ui.Dialogue("Formatura!",
                $"Você agora tem: {proximoNivel}!\n" +
                $"-{educationCost} Moedas investidas\n" +
                $"+75 pontos na pontuação final!", true);
        } else {
            ui.Dialogue("Universidade",
                "Você decidiu economizar por enquanto.\n" +
                "A universidade estará aqui quando voltar!", true);
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
