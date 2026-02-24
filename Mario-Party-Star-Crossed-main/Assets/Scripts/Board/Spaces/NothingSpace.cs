using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NothingSpace : BoardSpace {
    public override void setup() {
        // Define a chance de ser um espao azul como 0 (j que no um espao azul)
        this.blueChance = 0;
    }

    public override IEnumerator land(Player p) {
        // Marca que o jogador est no espao
        doneLanding = false;

        // Aqui voc pode adicionar uma mensagem opcional, se quiser
        ui.Dialogue("Nada aconteceu!", "Este espaço não tem nenhum efeito.", true);

        // Espera o jogador confirmar a mensagem (se houver)
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        // Marca que o jogador terminou de "pousar" no espao
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        // Como este espao no tem efeito, o valor para a IA  neutro (0)
        return 0;
    }
}