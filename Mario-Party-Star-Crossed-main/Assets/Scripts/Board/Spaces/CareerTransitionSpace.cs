using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CareerTransitionSpace : BoardSpace {
    public BoardSpace nextLoopStart;
    public int requiredCareerLevel = 1;
    public int requiredEducation = 2;
    public int minSavings = 200;

    // Próximo espaço temporário apenas para o jogador atual que está passando por aqui
    private BoardSpace temporaryNextForPass;

    public override void setup() {
        base.setup();
        this.blueChance = 0;
    }

    public override BoardSpace getNextSpaceInSequence() {
        if (temporaryNextForPass != null) {
            BoardSpace chosen = temporaryNextForPass;
            // Limpa após consumir, para não afetar outros jogadores
            temporaryNextForPass = null;
            return chosen;
        }
        return base.getNextSpaceInSequence();
    }

    public override IEnumerator pass(Player p) {
        donePassing = false;
        // Garante que o próximo temporário esteja limpo no início do passe
        temporaryNextForPass = null;

        if (ShouldAutoPromote(p.state)) {
            yield return StartCoroutine(HandleAutoPromotion(p));
        }

        if (CheckTransitionConditions(p.state)) {
            yield return StartCoroutine(ExecutePromotionSequence(p));
        }
        else {
            ShowRequirementsMessage(p.state);
        }

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        donePassing = true;
    }
    
    private bool CheckTransitionConditions(PlayerState state) {
        return state.GetCareerLevel() >= requiredCareerLevel &&
               state.GetEducationLevel() >= requiredEducation &&
               state.GetDinheiro() >= minSavings;
    }

    private IEnumerator ExecutePromotionSequence(Player p) {
        // Efeito visual
        ui.Dialogue("Carreira Evoluindo!", $"Parabéns {p.state.charName()}!", true);
        yield return new WaitForSeconds(1f);

        // Executar promoção
        p.state.Promote();
        // Define o próximo espaço apenas para este jogador nesta passagem
        temporaryNextForPass = nextLoopStart;

        // Feedback
        ui.Dialogue("Nova Posição!", $"Agora você é: {p.state.GetProfissao()}", true);
        yield return new WaitForSeconds(1.5f);
    }

    private void ShowRequirementsMessage(PlayerState state) {
        string missing = "";
        if (state.GetCareerLevel() < requiredCareerLevel) missing += $"\n- Nível de Carreira {requiredCareerLevel}";
        if (state.GetEducationLevel() < requiredEducation) missing += $"\n- Educação Nível {requiredEducation}";
        if (state.GetDinheiro() < minSavings) missing += $"\n- Economias de {minSavings} Moedas";

        ui.Dialogue("Requisitos não atendidos",
            $"Faltam:{missing}\nContinue progredindo para avançar!", true);
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return CheckTransitionConditions(state) ? 50 : -10;
    }

    private bool ShouldAutoPromote(PlayerState state) {
        return state.GetSalary() > 150 &&
               state.GetDinheiro() > 500 &&
               state.GetCareerLevel() < 3;
    }

    private IEnumerator HandleAutoPromotion(Player p) {
        ui.Dialogue("Sucesso Financeiro!",
            "Seu desempenho excepcional rendeu uma promoção automática!", true);
        p.state.Promote();
        yield return new WaitForSeconds(1.5f);
    }
}