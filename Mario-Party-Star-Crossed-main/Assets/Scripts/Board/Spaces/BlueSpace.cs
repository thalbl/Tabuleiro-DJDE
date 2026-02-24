using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueSpace : BoardSpace {
    [Tooltip("Prefab of coin object.")]
    public GameObject coinPrefab;

    private Dictionary<int, List<Dictionary<string, object>>> bonificacoesPorCategoria;

    public override void setup() {
        this.blueChance = 100;
        CarregarBonificacoesPorCategoria();
    }

    private void CarregarBonificacoesPorCategoria() {
        bonificacoesPorCategoria = new Dictionary<int, List<Dictionary<string, object>>>();

        // Carrega bonificações para cada categoria
        for (int categoria = 0; categoria <= 3; categoria++) {
            bonificacoesPorCategoria[categoria] = new List<Dictionary<string, object>>();
            TextAsset arquivo = Resources.Load<TextAsset>($"bonificacoes_nivel_{categoria}");

            if (arquivo != null) {
                string[] linhas = arquivo.text.Split('\n');
                foreach (string linha in linhas) {
                    if (string.IsNullOrEmpty(linha.Trim())) continue;

                    string[] partes = linha.Trim().Split('|');
                    Dictionary<string, object> bonus = new Dictionary<string, object>();

                    bonus["tipo"] = partes[0];
                    bonus["valores"] = new List<int>();

                    for (int i = 1; i < partes.Length; i++) {
                        ((List<int>)bonus["valores"]).Add(int.Parse(partes[i]));
                    }

                    bonificacoesPorCategoria[categoria].Add(bonus);
                }
            }
            else {
                Debug.LogError($"Arquivo de bonificações para categoria {categoria} não encontrado!");
            }
        }
    }

    private int ObterCategoriaPorNivelCarreira(int nivelCarreira) {
        // Mapear níveis de carreira para categorias de bonificações
        if (nivelCarreira <= 3) return 0;      // Categoria 0: Níveis 1-3
        if (nivelCarreira <= 6) return 1;      // Categoria 1: Níveis 4-6
        if (nivelCarreira <= 8) return 2;      // Categoria 2: Níveis 7-8
        return 3;                              // Categoria 3: Níveis 9-10
    }

    private IEnumerator AplicarBonificacao(Player p) {
        int nivelCarreira = p.state.GetCareerLevel();
        int categoria = ObterCategoriaPorNivelCarreira(nivelCarreira);
        var bonificacoes = bonificacoesPorCategoria[categoria];

        if (bonificacoes.Count == 0) {
            Debug.LogError($"Nenhuma bonificação carregada para categoria {categoria}!");
            yield break;
        }

        // Seleciona aleatoriamente baseado na categoria
        int indice = Random.Range(0, bonificacoes.Count);
        Dictionary<string, object> bonus = bonificacoes[indice];
        string tipo = (string)bonus["tipo"];
        List<int> valores = (List<int>)bonus["valores"];

        string titulo = categoria switch {
            0 => "Bom Trabalho!",
            1 => "Sorte Grande!",
            2 => "Bônus!",
            _ => "Incrível!"
        };

        switch (tipo) {
            case "AumentarSalario":
                // O valor base é multiplicado por um fator baseado no nível
                int aumentoBase = valores[0];
                int aumento = aumentoBase * nivelCarreira;
                int novoSalario = p.state.GetSalary() + aumento;
                p.state.SetBaseSalary(novoSalario);
                ui.Dialogue(titulo, $"Mesada aumentada em {aumento} Moedas/turno!", true);
                break;

            case "GanharMoedasDinheiro":
                int moedasBase = valores[0];
                int dinheiroBase = valores.Count > 1 ? valores[1] : 0;
                int moedas = moedasBase + nivelCarreira;
                int dinheiro = Mathf.RoundToInt(dinheiroBase * nivelCarreira / 100f);

                p.state.changeCoins(moedas + dinheiro);

                for (int i = 0; i < moedas; i++) {
                    Instantiate(coinPrefab, transform.position + Vector3.up * 5, Quaternion.Euler(0, Random.Range(0, 360), 0));
                    yield return new WaitForSeconds(0.1f);
                }
                ui.Dialogue(titulo, $"+{moedas + dinheiro} Moedas!", true);
                break;

            case "GanharDinheiro":
                int bonusDinheiroBase = valores[0];
                int bonusDinheiro = Mathf.RoundToInt(bonusDinheiroBase * nivelCarreira / 100f);
                p.state.changeCoins(Mathf.Max(1, bonusDinheiro));

                string motivo = categoria switch {
                    0 => "Bom comportamento",
                    1 => "Notas boas",
                    2 => "Ajudou alguém",
                    _ => "Encontrou no chão"
                };
                ui.Dialogue(titulo, $"{motivo}: +{Mathf.Max(1, bonusDinheiro)} Moedas!", true);
                break;

            case "Promocao":
                // Só promove se não estiver no nível máximo (10)
                if (p.state.GetCareerLevel() < 10) {
                    p.state.Promote();
                    ui.Dialogue("Promoção!", $"Agora você é {p.state.GetCareerTitle()}!", true);
                }
                else {
                    // Bônus alternativo para quem já está no topo
                    int bonusTopo = 100;
                    p.state.changeCoins(bonusTopo);
                    ui.Dialogue("Reconhecimento", $"Bônus de liderança: +{bonusTopo} Moedas!", true);
                }
                break;
        }

        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;
        yield return StartCoroutine(AplicarBonificacao(p));
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 10;
    }
}