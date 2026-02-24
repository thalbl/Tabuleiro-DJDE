using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuestionSpace : BoardSpace {
    [Tooltip("Prefab of coin object.")]
    public GameObject coinPrefab;

    private Dictionary<int, List<(string pergunta, string respostaCorreta, List<string> opcoes)>> perguntasPorCategoria;

    public override void setup() {
        this.blueChance = 100;
        CarregarPerguntasPorCategoria();
    }

    private void CarregarPerguntasPorCategoria() {
        perguntasPorCategoria = new Dictionary<int, List<(string, string, List<string>)>>();

        // Carregar as 4 categorias
        for (int categoria = 0; categoria <= 3; categoria++) {
            perguntasPorCategoria[categoria] = new List<(string, string, List<string>)>();
            TextAsset arquivo = Resources.Load<TextAsset>($"quiz_nivel_{categoria}");

            if (arquivo != null) {
                string[] linhas = arquivo.text.Split('\n');
                foreach (string linha in linhas) {
                    string[] partes = linha.Split('|');
                    if (partes.Length == 6) {
                        string pergunta = partes[0].Trim();
                        string respostaCorreta = partes[1].Trim();
                        List<string> opcoes = new List<string> {
                            partes[1].Trim(),
                            partes[2].Trim(),
                            partes[3].Trim(),
                            partes[4].Trim(),
                            partes[5].Trim()
                        };
                        perguntasPorCategoria[categoria].Add((pergunta, respostaCorreta, opcoes));
                    }
                }
            }
            else {
                Debug.LogError($"Arquivo de quiz para categoria {categoria} não encontrado!");
            }
        }
    }

    // Método para embaralhar uma lista
    private List<T> EmbaralharLista<T>(List<T> lista) {
        System.Random rng = new System.Random();
        return lista.OrderBy(a => rng.Next()).ToList();
    }

    private int ObterCategoriaPorNivelCarreira(int nivelCarreira) {
        // Mapear níveis de carreira para categorias de perguntas
        if (nivelCarreira <= 3) return 0;      // Finanças Pessoais
        if (nivelCarreira <= 6) return 1;      // Gestão Profissional
        if (nivelCarreira <= 8) return 2;      // Investimentos
        return 3;                              // Estratégia Corporativa
    }

    private IEnumerator FazerPergunta(Player p) {
        int nivelCarreira = p.state.GetCareerLevel();
        int categoria = ObterCategoriaPorNivelCarreira(nivelCarreira);
        var perguntas = perguntasPorCategoria[categoria];

        if (perguntas.Count == 0) {
            Debug.LogError($"Nenhuma pergunta carregada para categoria {categoria}!");
            yield break;
        }

        int indice = Random.Range(0, perguntas.Count);
        var (pergunta, respostaCorreta, opcoes) = perguntas[indice];

        string tema;
        switch (categoria) {
            case 0: tema = "Finanças Pessoais"; break;
            case 1: tema = "Gestão Profissional"; break;
            case 2: tema = "Investimentos"; break;
            default: tema = "Estratégia Corporativa"; break;
        }

        // EMBARALHAR AS OPÇÕES ANTES DE EXIBIR
        List<string> opcoesEmbaralhadas = EmbaralharLista(opcoes);

        ui.Dialogue(tema, pergunta, opcoesEmbaralhadas, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        string respostaJogador = ui.MostRecentDialogueAnswer();
        if (respostaJogador == respostaCorreta) {
            // Recompensas escalonadas com o nível de carreira
            int moedasRecompensa = 3 + nivelCarreira;
            int bonusFinanceiro = Mathf.RoundToInt(p.state.GetSalary() * (0.01f * nivelCarreira));

            // Mostra moedas caindo
            for (int i = 0; i < moedasRecompensa; i++) {
                Instantiate(coinPrefab, transform.position + Vector3.up * 5, Quaternion.identity);
                yield return new WaitForSeconds(0.2f);
            }

            // Aplicar moedas
            p.state.changeCoins(moedasRecompensa + bonusFinanceiro);

            ui.Dialogue("Correto!", $"Resposta certa! Você recebeu {moedasRecompensa + bonusFinanceiro} Moedas!", true);
        }
        else {
            string feedback;
            switch (categoria) {
                case 0: feedback = "Continue estudando finanças pessoais!"; break;
                case 1: feedback = "A gestão profissional requer conhecimento constante"; break;
                case 2: feedback = "Investimentos exigem estudo e experiência"; break;
                default: feedback = "Estratégias corporativas são complexas, continue aprendendo!"; break;
            }
            ui.Dialogue("Ops...", feedback + "\nResposta correta: " + respostaCorreta, true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;
        yield return StartCoroutine(FazerPergunta(p));
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        // IA valoriza mais espaços de pergunta em níveis altos
        return 2 + Mathf.Min(state.GetCareerLevel(), 5);
    }
}
