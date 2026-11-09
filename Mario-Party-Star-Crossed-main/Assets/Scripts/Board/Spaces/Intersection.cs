using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Intersection : BoardSpace {
    [Tooltip("The other space a player could move to from here.")]
    public BoardSpace option;
    [Tooltip("Material to use for active phantom.")]
    public Material activeMat;
    [Tooltip("Material to use for inactive phantom.")]
    public Material inactiveMat;
    [Tooltip("Key to press to select the default route.")]
    public KeyCode activeKey;
    [Tooltip("Key to press to select the alternative route.")]
    public KeyCode inactiveKey;
    [Tooltip("Gate attached to the base route.")]
    public SkeletonGate gate;
    [Tooltip("Gate attached to the alternate route.")]
    public SkeletonGate optionGate;

    private Renderer phantom;
    private Renderer phantomAlt;
    private bool goingAlt;
    private bool AIMadeChoice;

    // Lista para armazenar perguntas e respostas
    private List<(string pergunta, string opcao1, string opcao2)> perguntasRespostas;

    public override void setup() {
        this.canLandHere = false;
        phantom = transform.Find("PhantomPrism").gameObject.GetComponent<Renderer>();
        phantomAlt = transform.Find("PhantomPrism (ALT)").gameObject.GetComponent<Renderer>();
        phantom.material = activeMat;
        phantomAlt.material = inactiveMat;
        phantom.enabled = false;
        phantomAlt.enabled = false;

        // Carregar perguntas do arquivo
        CarregarPerguntas();
    }

    // Método para carregar perguntas do arquivo
    private void CarregarPerguntas() {
        perguntasRespostas = new List<(string, string, string)>();
        TextAsset arquivo = Resources.Load<TextAsset>("perguntas"); // Carrega o arquivo de texto
        if (arquivo != null) {
            string[] linhas = arquivo.text.Split('\n');
            foreach (string linha in linhas) {
                string[] partes = linha.Split('|');
                if (partes.Length == 3) {
                    perguntasRespostas.Add((partes[0].Trim(), partes[1].Trim(), partes[2].Trim()));
                }
            }
        }
        else {
            Debug.LogError("Arquivo de perguntas não encontrado!");
        }
    }

    // Método para fazer uma pergunta ao jogador
    private IEnumerator FazerPergunta() {
        if (perguntasRespostas.Count == 0) {
            Debug.LogError("Nenhuma pergunta carregada!");
            yield break;
        }

        // Seleciona uma pergunta aleatória
        int indice = Random.Range(0, perguntasRespostas.Count);
        var (pergunta, opcao1, opcao2) = perguntasRespostas[indice];

        // Exibe a pergunta ao jogador com duas opções
        ui.Dialogue("Pergunta", pergunta, new List<string>() { opcao1, opcao2 }, true);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        // Verifica a resposta
        string respostaJogador = ui.MostRecentDialogueAnswer();
        if (respostaJogador == opcao1) {
            goingAlt = false; // Escolhe o caminho padrão
        }
        else if (respostaJogador == opcao2) {
            goingAlt = true; // Escolhe o caminho alternativo
        }
        else {
            goingAlt = false;
        }
    }

    public override IEnumerator pass(Player p) {
        BoardSpace temp = next;
        if ((gate == null && optionGate == null) || p.state.hasItem(BoardItem.SkeletonKey) || p.state.getMovement() == 5) {
            donePassing = false;
            goingAlt = false;
            phantom.material = activeMat;
            phantomAlt.material = inactiveMat;
            phantom.enabled = true;
            phantomAlt.enabled = true;

            int ctrl = p.state.getController();
            if ((ctrl == 1 || ctrl == 2) && p.brain != null) {
                // Bot decide pelo valor de cada rota (antes respondia a pergunta de perfil com a 1ª opção)
                yield return StartCoroutine(AIPathChoice(p));
            } else {
                // Humano: pergunta de perfil determina o caminho
                yield return StartCoroutine(FazerPergunta());
            }

            // Define o próximo caminho com base na resposta
            if (goingAlt) {
                next = option;
            }

            phantom.enabled = false;
            phantomAlt.enabled = false;
            donePassing = true;
        }
        else if (gate != null) {
            next = option;
            donePassing = true;
        }

        yield return new WaitForSeconds(1.0f);
        if (goingAlt) {
            option = next;
            next = temp;
        }
        if (optionGate != null) {
            optionGate.Close();
        }
        if (gate != null) {
            gate.Close();
        }
    }

    // Restante do código permanece inalterado
    public IEnumerator AIPathChoice(Player p) {
        yield return new WaitForSeconds(1.0f);
        goingAlt = p.brain.Prompt(Prompts.EscolhaCaminho, new List<string>() { "Next", "Option" }, p.GetCurrentRollCount()) == "Option";
        phantom.material = goingAlt ? inactiveMat : activeMat;
        phantomAlt.material = goingAlt ? activeMat : inactiveMat;
        AIMadeChoice = true;
    }

    public override int SpacesToStar(bool withKey) {
        return SpacesToStar(withKey, 0);
    }

    public override int SpacesToStar(bool withKey, int depth) {
        if (depth >= 30) return 30;
        if (gate != null && !withKey) {
            return option != null ? option.SpacesToStar(false, depth + 1) : 30;
        }
        if (optionGate != null && !withKey) {
            return next != null ? next.SpacesToStar(false, depth + 1) : 30;
        }
        int nVal = next != null ? next.SpacesToStar(withKey, depth + 1) : 30;
        int oVal = option != null ? option.SpacesToStar(withKey, depth + 1) : 30;
        return Mathf.Min(nVal, oVal);
    }

    public override int CumulativeValue(PlayerState state, List<PlayerState> rivals, int roll) {
        return CumulativeValue(state, rivals, roll, 0);
    }

    public override int CumulativeValue(PlayerState state, List<PlayerState> rivals, int roll, int depth) {
        if (depth >= 40) return 0;
        if (gate != null && !state.getItems().Contains(BoardItem.SkeletonKey)) {
            return option != null ? option.CumulativeValue(state, rivals, roll, depth + 1) : 0;
        }
        if (optionGate != null && !state.getItems().Contains(BoardItem.SkeletonKey)) {
            return next != null ? next.CumulativeValue(state, rivals, roll, depth + 1) : 0;
        }
        int nVal = next != null ? next.CumulativeValue(state, rivals, roll, depth + 1) : 0;
        int oVal = option != null ? option.CumulativeValue(state, rivals, roll, depth + 1) : 0;
        return Mathf.Max(nVal, oVal);
    }

    public override int PlayersInRange(int range) {
        int nVal = next != null ? next.PlayersInRange(range) : 0;
        int oVal = option != null ? option.PlayersInRange(range) : 0;
        return Mathf.Max(nVal, oVal);
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 0;
    }
}
