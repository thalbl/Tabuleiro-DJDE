using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedSpace : BoardSpace {
    [Tooltip("Prefab of coin object.")]
    public GameObject coinPrefab;

    private Dictionary<int, List<Dictionary<string, object>>> penalidadesPorCategoria;

    public override void setup() {
        this.blueChance = 0;
        CarregarPenalidadesPorCategoria();
    }

    private void CarregarPenalidadesPorCategoria() {
        penalidadesPorCategoria = new Dictionary<int, List<Dictionary<string, object>>>();

        for (int categoria = 0; categoria <= 3; categoria++) {
            penalidadesPorCategoria[categoria] = new List<Dictionary<string, object>>();
            TextAsset arquivo = Resources.Load<TextAsset>($"penalidades_nivel_{categoria}");

            if (arquivo != null) {
                string[] linhas = arquivo.text.Split('\n');
                foreach (string linha in linhas) {
                    if (string.IsNullOrEmpty(linha.Trim())) continue;

                    string[] partes = linha.Trim().Split('|');
                    Dictionary<string, object> penalidade = new Dictionary<string, object>();

                    penalidade["tipo"] = partes[0];
                    penalidade["valores"] = new List<int>();

                    for (int i = 1; i < partes.Length; i++) {
                        ((List<int>)penalidade["valores"]).Add(int.Parse(partes[i]));
                    }

                    penalidadesPorCategoria[categoria].Add(penalidade);
                }
            }
            else {
                Debug.LogError($"Arquivo de penalidades para categoria {categoria} não encontrado!");
            }
        }
    }

    private int ObterCategoriaPorNivelCarreira(int nivelCarreira) {
        // Mapear nï¿½veis de carreira para categorias de penalidades
        if (nivelCarreira <= 3) return 0;      // Categoria 0: Nï¿½veis 1-3
        if (nivelCarreira <= 6) return 1;      // Categoria 1: Nï¿½veis 4-6
        if (nivelCarreira <= 8) return 2;      // Categoria 2: Nï¿½veis 7-8
        return 3;                              // Categoria 3: Nï¿½veis 9-10
    }

    private IEnumerator AplicarPenalidade(Player p) {
        int nivelCarreira = p.state.GetCareerLevel();
        int categoria = ObterCategoriaPorNivelCarreira(nivelCarreira);
        var penalidades = penalidadesPorCategoria[categoria];

        if (penalidades.Count == 0) {
            Debug.LogError($"Nenhuma penalidade carregada para categoria {categoria}!");
            yield break;
        }

        int indice = Random.Range(0, penalidades.Count);
        Dictionary<string, object> penalidade = penalidades[indice];
        string tipo = (string)penalidade["tipo"];
        List<int> valores = (List<int>)penalidade["valores"];

        string titulo = categoria switch {
            0 => "Atenção!",
            1 => "Problema!",
            2 => "Crise!",
            _ => "Emergência!"
        };

        switch (tipo) {
            case "ReduzirSalario":
                int reducaoBase = valores[0];
                int reducao = reducaoBase * nivelCarreira;
                int novoSalario = Mathf.Max(10, p.state.GetSalary() - reducao);
                p.state.SetBaseSalary(novoSalario);

                ui.Dialogue(titulo, $"Mesada reduzida em {reducao}/turno!", true);
                break;

            case "GastarDinheiro":
                int gastoBase = valores[0];
                int gasto = gastoBase * nivelCarreira;
                p.state.SetDinheiro(Mathf.Max(0, p.state.GetDinheiro() - gasto));

                string motivo = categoria switch {
                    0 => "Perdeu o brinquedo",
                    1 => "Comprou doces demais",
                    2 => "Quebrou algo",
                    _ => "Esqueceu o troco"
                };
                ui.Dialogue(titulo, $"{motivo}: {gasto} Moedas", true);
                break;

            case "PerderMoedas":
                int moedasPerdidasBase = valores[0];
                int moedasPerdidas = Mathf.Min(moedasPerdidasBase + nivelCarreira, p.state.getCoins());
                p.state.changeCoins(-moedasPerdidas);
                ui.Dialogue(titulo, $"-{moedasPerdidas} moedas!", true);
                break;

            case "Demissao":
                // Sï¿½ rebaixa se nï¿½o estiver no nï¿½vel mï¿½nimo (1)
                if (p.state.GetCareerLevel() > 1) {
                    p.state.Demote();
                    ui.Dialogue("Retrocesso", $"Você foi rebaixado para {p.state.GetCareerTitle()}!", true);
                }
                else {
                    // Penalidade alternativa para quem jï¿½ estï¿½ no nï¿½vel mais baixo
                    int perdaAlternativa = 5 * nivelCarreira;
                    p.state.SetDinheiro(p.state.GetDinheiro() - perdaAlternativa);
                    ui.Dialogue("Problema Profissional", $"Você perdeu ${perdaAlternativa} devido a um erro profissional!", true);
                }
                break;
        }

        p.state.UnluckySpaceStatTrigger();
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }


    public override IEnumerator land(Player p) {
        doneLanding = false;
        yield return StartCoroutine(AplicarPenalidade(p));
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return -10; // Valor negativo para indicar que ï¿½ ruim para o jogador
    }
}

