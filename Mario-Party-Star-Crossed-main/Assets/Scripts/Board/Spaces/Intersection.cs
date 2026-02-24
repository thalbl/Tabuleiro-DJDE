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
            Debug.LogError("Resposta inválida!");
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

            // Faz uma pergunta ao jogador para determinar o caminho
            yield return StartCoroutine(FazerPergunta());

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
        goingAlt = p.brain.Prompt("Choose Path At Intersection", new List<string>() { "Next", "Option" }, p.GetCurrentRollCount()) == "Option";
        phantom.material = goingAlt ? inactiveMat : activeMat;
        phantomAlt.material = goingAlt ? activeMat : inactiveMat;
        AIMadeChoice = true;
    }

    public override int SpacesToStar(bool withKey) {
        if (gate != null && !withKey) {
            return option.SpacesToStar(false);
        }
        if (optionGate != null && !withKey) {
            return next.SpacesToStar(false);
        }
        return Mathf.Min(next.SpacesToStar(withKey), option.SpacesToStar(withKey));
    }

    public override int CumulativeValue(PlayerState state, List<PlayerState> rivals, int roll) {
        if (gate != null && !state.getItems().Contains(BoardItem.SkeletonKey)) {
            return option.CumulativeValue(state, rivals, roll);
        }
        if (optionGate != null && !state.getItems().Contains(BoardItem.SkeletonKey)) {
            return next.CumulativeValue(state, rivals, roll);
        }
        return Mathf.Max(next.CumulativeValue(state, rivals, roll), option.CumulativeValue(state, rivals, roll));
    }

    public override int PlayersInRange(int range) {
        return Mathf.Max(next.PlayersInRange(range), option.PlayersInRange(range));
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 0;
    }
}