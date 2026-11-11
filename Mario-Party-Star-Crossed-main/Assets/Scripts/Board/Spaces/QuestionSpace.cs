using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Casa de quiz de educação financeira. Perguntas em Resources/quiz_nivel_{0..3}.txt, uma por linha:
///   pergunta|resposta certa|errada 1|errada 2|errada 3|errada 4|explicação
/// Linhas vazias ou começando com # são ignoradas. A explicação é mostrada depois da resposta.
/// As perguntas saem de um "baralho" embaralhado por categoria (GameRng): não repetem até acabar o baralho.
/// </summary>
public class QuestionSpace : BoardSpace {
    [Tooltip("Prefab of coin object.")]
    public GameObject coinPrefab;

    public class Pergunta {
        public string Texto, Correta, Explicacao;
        public List<string> Opcoes;
    }

    // Banco e baralhos compartilhados por todas as casas de quiz
    static Dictionary<int, List<Pergunta>> banco;
    static readonly Dictionary<int, Queue<int>> baralhos = new Dictionary<int, Queue<int>>();
    static int seedDosBaralhos = int.MinValue;

    /// <summary>Pergunta em exibição (a IA usa para decidir se acerta).</summary>
    public static Pergunta PerguntaAtual { get; private set; }

    static readonly string[] Temas = { "Finanças Pessoais", "Trabalho e Crédito", "Investimentos", "Negócios e Estratégia" };

    public override void setup() {
        this.blueChance = 100;
        if (banco == null) CarregarBanco();
    }

    static void CarregarBanco() {
        banco = new Dictionary<int, List<Pergunta>>();
        for (int categoria = 0; categoria <= 3; categoria++) {
            var lista = new List<Pergunta>();
            TextAsset arquivo = Resources.Load<TextAsset>($"quiz_nivel_{categoria}");
            if (arquivo == null) {
                Debug.LogError($"Arquivo de quiz para categoria {categoria} não encontrado!");
            } else {
                foreach (string bruta in arquivo.text.Split('\n')) {
                    string linha = bruta.Trim();
                    if (linha.Length == 0 || linha.StartsWith("#")) continue;
                    string[] p = linha.Split('|');
                    if (p.Length < 6) { Debug.LogWarning($"[Quiz] Linha ignorada (campos insuficientes): {linha}"); continue; }
                    lista.Add(new Pergunta {
                        Texto = p[0].Trim(),
                        Correta = p[1].Trim(),
                        Opcoes = new List<string> { p[1].Trim(), p[2].Trim(), p[3].Trim(), p[4].Trim(), p[5].Trim() },
                        Explicacao = p.Length > 6 ? p[6].Trim() : ""
                    });
                }
            }
            banco[categoria] = lista;
        }
    }

    /// <summary>Próxima pergunta da categoria sem repetir; reembaralha quando o baralho acaba ou a partida muda.</summary>
    static Pergunta Proxima(int categoria) {
        if (seedDosBaralhos != GameRng.Seed) {
            baralhos.Clear();
            seedDosBaralhos = GameRng.Seed;
        }
        var lista = banco[categoria];
        if (lista.Count == 0) return null;
        if (!baralhos.TryGetValue(categoria, out var fila) || fila.Count == 0) {
            var ordem = new List<int>();
            for (int i = 0; i < lista.Count; i++) ordem.Add(i);
            GameRng.Embaralhar(ordem);
            fila = new Queue<int>(ordem);
            baralhos[categoria] = fila;
        }
        return lista[fila.Dequeue()];
    }

    static int Categoria(int nivelCarreira) {
        if (nivelCarreira <= 3) return 0;
        if (nivelCarreira <= 6) return 1;
        if (nivelCarreira <= 8) return 2;
        return 3;
    }

    private IEnumerator FazerPergunta(Player p) {
        int nivelCarreira = p.state.GetCareerLevel();
        int categoria = Categoria(nivelCarreira);
        Pergunta pergunta = Proxima(categoria);
        if (pergunta == null) {
            Debug.LogError($"Nenhuma pergunta carregada para categoria {categoria}!");
            yield break;
        }

        var opcoes = new List<string>(pergunta.Opcoes);
        GameRng.Embaralhar(opcoes);

        PerguntaAtual = pergunta;
        ui.Dialogue(Temas[categoria], pergunta.Texto, opcoes, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        string respostaJogador = ui.MostRecentDialogueAnswer();
        PerguntaAtual = null;

        if (respostaJogador == pergunta.Correta) {
            Sons.Tocar(Sons.Efeito.Acerto);
            int moedasRecompensa = 6 + 2 * nivelCarreira;
            int bonusFinanceiro = Mathf.RoundToInt(p.state.GetSalary() * 0.05f);
            for (int i = 0; i < moedasRecompensa; i++) {
                Instantiate(coinPrefab, transform.position + Vector3.up * 5, Quaternion.identity);
                yield return new WaitForSeconds(0.2f);
            }
            int total = moedasRecompensa + bonusFinanceiro;
            p.state.changeCoins(total);
            string texto = string.IsNullOrEmpty(pergunta.Explicacao) ? "Resposta certa!" : pergunta.Explicacao;
            ui.Dialogue($"Correto! +{total} Moedas", texto, true);
        } else {
            Sons.Tocar(Sons.Efeito.Erro);
            string texto = $"Resposta certa: {pergunta.Correta}";
            if (!string.IsNullOrEmpty(pergunta.Explicacao)) texto += "\n" + pergunta.Explicacao;
            ui.Dialogue("Quase!", texto, true);
        }
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;
        yield return StartCoroutine(FazerPergunta(p));
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 2 + Mathf.Min(state.GetCareerLevel(), 5);
    }
}
