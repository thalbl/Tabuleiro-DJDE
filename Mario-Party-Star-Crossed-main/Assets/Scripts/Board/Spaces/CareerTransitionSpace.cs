using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CareerTransitionSpace : BoardSpace {
    public BoardSpace nextLoopStart;
    public int requiredCareerLevel = 1;
    public int requiredEducation = 2;
    public int minSavings = 200;
    [Tooltip("Nível máximo de carreira em que este espaço concede promoção de transição (use < 10 para transições intermediárias).")]
    public int maxPromotionCareerLevel = 10;

    // Próximo espaço temporário apenas para o jogador atual que está passando por aqui
    private BoardSpace temporaryNextForPass;
    private Dictionary<Player, BoardSpace> nextByPlayer = new Dictionary<Player, BoardSpace>();

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
        if (game != null) {
            foreach (Player pl in game.GetPlayers()) {
                if (pl != null && Vector3.Distance(pl.transform.position, transform.position) < 0.5f) {
                    if (nextByPlayer.TryGetValue(pl, out BoardSpace savedNext) && savedNext != null) {
                        nextByPlayer.Remove(pl);
                        return savedNext;
                    }
                    if (pl.state != null && pl.state.GetCareerLevel() > maxPromotionCareerLevel && nextLoopStart != null) {
                        return nextLoopStart;
                    }
                }
            }
        }
        return base.getNextSpaceInSequence();
    }

    public override IEnumerator pass(Player p) {
        donePassing = false;
        // Garante que o próximo temporário esteja limpo no início do passe
        temporaryNextForPass = null;

        // Se o jogador já superou o nível máximo desta transição intermediária, avança direto para o próximo ciclo
        if (p.state.GetCareerLevel() > maxPromotionCareerLevel && nextLoopStart != null) {
            temporaryNextForPass = nextLoopStart;
            nextByPlayer[p] = nextLoopStart;
            donePassing = true;
            yield break;
        }

        if (ShouldAutoPromote(p.state) && p.state.GetCareerLevel() < requiredCareerLevel) {
            yield return StartCoroutine(HandleAutoPromotion(p));
        }

        if (CheckTransitionConditions(p.state)) {
            yield return StartCoroutine(ExecutePromotionSequence(p));
        }
        else {
            nextByPlayer[p] = this.next;
            // Passar pelo portão sem os requisitos = completou uma volta neste anel.
            // A cada 2 voltas, CompleteLap() promove por experiência (antes nunca era chamado).
            int nivelAntes = p.state.GetCareerLevel();
            p.state.CompleteLap();
            if (p.state.GetCareerLevel() > nivelAntes) {
                ui.Dialogue("Experiência de Mercado",
                    $"Depois de 2 voltas no circuito, {p.state.charName()} foi promovido(a) por experiência!\nAgora: {p.state.GetProfissao()} {p.state.GetCareerTitle()} (Nv. {p.state.GetCareerLevel()})", true);
                yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            }
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
        // Executar promoção
        p.state.Promote();
        // Define o próximo espaço apenas para este jogador nesta passagem
        BoardSpace targetNext = nextLoopStart != null ? nextLoopStart : this.next;
        temporaryNextForPass = targetNext;
        nextByPlayer[p] = targetNext;

        // Feedback
        ui.Dialogue("Carreira Evoluindo!", $"Parabéns {p.state.charName()}!\nAgora você é:\n{p.state.GetProfissao()} {p.state.GetCareerTitle()} (Nv. {p.state.GetCareerLevel()})!", true);
        yield return new WaitForSeconds(0.5f);
    }

    private void ShowRequirementsMessage(PlayerState state) {
        string missing = "";
        if (state.GetCareerLevel() < requiredCareerLevel) missing += $"\n- Nível de Carreira {requiredCareerLevel} ({PlayerState.ObterTituloCarreira(requiredCareerLevel)})";
        if (state.GetEducationLevel() < requiredEducation) missing += $"\n- Educação Nível {requiredEducation}";
        if (state.GetDinheiro() < minSavings) missing += $"\n- Reserva de Emergência: {minSavings} Moedas (Você tem: {state.GetDinheiro()})";

        ui.Dialogue("Requisitos para Avançar",
            $"Para passar para a próxima etapa da carreira:{missing}\n\nContinue se capacitando no circuito!", true);
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        if (state.GetCareerLevel() > maxPromotionCareerLevel) return 5;
        return CheckTransitionConditions(state) ? 50 : -10;
    }

    private bool ShouldAutoPromote(PlayerState state) {
        return state.GetSalary() >= 75 &&
               state.GetDinheiro() >= 80 &&
               state.GetCareerLevel() < 3;
    }

    private IEnumerator HandleAutoPromotion(Player p) {
        ui.Dialogue("Sucesso Financeiro!",
            "Seu desempenho e gestão financeira renderam uma promoção por mérito!", true);
        p.state.Promote();
        yield return new WaitForSeconds(1.5f);
    }
}
