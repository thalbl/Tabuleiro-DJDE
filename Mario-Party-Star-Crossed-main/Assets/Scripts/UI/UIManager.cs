using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIManager : MonoBehaviour {
    private GameObject dialogue;
    private GameObject character;
    private GameObject yourTurn;
    private GameObject options;
    private GameObject standings;
    private GameObject counter;
    private GameObject spacesLeft;
    private GameObject spinner;
    private GameObject turnTracker;
    private GameObject turnTrackerDrop;

    private GameObject optionA;
    private GameObject optionB;
    private GameObject optionC;
    private GameObject optionD;
    private GameObject optionE;

    private GameObject outcome1;
    private GameObject outcome2;
    private GameObject outcome3;
    private GameObject outcome4;
    private GameObject outcome5;
    private GameObject outcome6;
    private GameObject outcome7;
    private GameObject spinnerTitle;
    private GameObject spinnerTitleDrop;

    private BoardManager game;
    private PlayerTracker pt1;
    private PlayerTracker pt2;
    private PlayerTracker pt3;
    private PlayerTracker pt4;
    private PlayerDetailMenu playerDetailMenu;

    private int charsShown = 0;
    private string targetText = "";
    private Text dialogueText;
    private Text speakerText;
    private bool endOfChain;
    private string mostRecentAns;
    private List<string> choices;
    private List<string> spinnerChoices;
    private int spinnerLoc;
    private bool spinning;
    private int windDown;
    private int spinnerFrame;
    private Text yourTurnColor;
    private string mostRecentPrompt;
    private int counterMin;
    private int counterMax;
    private int counterStatus;
    private int controller;
    private float comTime;
    private AIBrain decisionAI;

    // Novos: descrições das opções
    private List<string> choiceDescriptions;

    // Start is called before the first frame update
    void Start() {
        game = FindObjectOfType<BoardManager>();
        comTime = 0.0f;

        dialogue = transform.Find("Dialogue").gameObject;
        character = transform.Find("Character").gameObject;
        yourTurn = transform.Find("Your Turn").gameObject;
        options = transform.Find("Options").gameObject;
        standings = transform.Find("Standings").gameObject;
        counter = transform.Find("Counter").gameObject;
        spacesLeft = transform.Find("Spaces Left").gameObject;
        spinner = transform.Find("Spinner").gameObject;
        turnTracker = standings.transform.Find("Turn").gameObject;
        turnTrackerDrop = turnTracker.transform.Find("Drop").gameObject;

        optionA = options.transform.Find("Option A").gameObject;
        optionB = options.transform.Find("Option B").gameObject;
        optionC = options.transform.Find("Option C").gameObject;
        optionD = options.transform.Find("Option D").gameObject;
        optionE = options.transform.Find("Option E").gameObject;

        outcome1 = spinner.transform.Find("Panel (1)").gameObject;
        outcome2 = spinner.transform.Find("Panel (2)").gameObject;
        outcome3 = spinner.transform.Find("Panel (3)").gameObject;
        outcome4 = spinner.transform.Find("Panel (4)").gameObject;
        outcome5 = spinner.transform.Find("Panel (5)").gameObject;
        outcome6 = spinner.transform.Find("Panel (6)").gameObject;
        outcome7 = spinner.transform.Find("Panel (7)").gameObject;
        spinnerTitle = spinner.transform.Find("Title").gameObject;
        spinnerTitleDrop = spinnerTitle.transform.Find("Drop").gameObject;

        yourTurnColor = yourTurn.transform.Find("Drop").gameObject.GetComponent<Text>();

        ToggleActivity(-1, -1, -1, -1, 1, -1, -1, -1);

        PlayerState[] players = game.state.GetPlayers();
        pt1 = standings.transform.Find("P1").gameObject.GetComponent<PlayerTracker>();
        pt2 = standings.transform.Find("P2").gameObject.GetComponent<PlayerTracker>();
        pt3 = standings.transform.Find("P3").gameObject.GetComponent<PlayerTracker>();
        pt4 = standings.transform.Find("P4").gameObject.GetComponent<PlayerTracker>();
        pt1.SetPlayer(players[0]);
        pt2.SetPlayer(players[1]);
        pt3.SetPlayer(players[2]);
        pt4.SetPlayer(players[3]);

        // Configurar menu de detalhes
        playerDetailMenu = FindObjectOfType<PlayerDetailMenu>();
        if (playerDetailMenu == null) {
            Debug.LogWarning("PlayerDetailMenu não encontrado na cena. Certifique-se de que o GameObject com o script PlayerDetailMenu está presente.");
        } else {
            // Conectar os PlayerTrackers ao menu de detalhes
            pt1.SetDetailMenu(playerDetailMenu);
            pt2.SetDetailMenu(playerDetailMenu);
            pt3.SetDetailMenu(playerDetailMenu);
            pt4.SetDetailMenu(playerDetailMenu);
        }

        dialogueText = dialogue.GetComponentsInChildren<Text>()[0];
        speakerText = character.GetComponentsInChildren<Text>()[0];
        dialogueText.text = "";

        spinning = false;
        windDown = -100;
        spinnerFrame = 0;
        targetText = "";

        choiceDescriptions = new List<string>();

        // Wire automático de hover com EventTrigger
        AttachHover(optionA, OptionAHovered);
        AttachHover(optionB, OptionBHovered);
        AttachHover(optionC, OptionCHovered);
        AttachHover(optionD, OptionDHovered);
        AttachHover(optionE, OptionEHovered);
    }

    private void AttachHover(GameObject go, UnityAction callback) {
        if (go == null) return;
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) {
            trigger = go.AddComponent<EventTrigger>();
        }

        // --- Eventos que MOSTRAM a descrição ---

        // Evento para HOVER do mouse (Já existe)
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener((BaseEventData _) => callback());
        trigger.triggers.Add(entry);

        // Evento para SELEÇÃO (teclado/controle) (Já existe)
        var selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        selectEntry.callback = new EventTrigger.TriggerEvent();
        selectEntry.callback.AddListener((BaseEventData _) => callback());
        trigger.triggers.Add(selectEntry);

        // --- Adições para CORRIGIR o bug ---

        // Evento para SAÍDA do mouse (restaura o diálogo)
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback = new EventTrigger.TriggerEvent();
        exitEntry.callback.AddListener((BaseEventData _) => RestoreOriginalDialogue());
        trigger.triggers.Add(exitEntry);

        // Evento para DES-SELEÇÃO (teclado/controle - restaura o diálogo)
        var deselectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Deselect };
        deselectEntry.callback = new EventTrigger.TriggerEvent();
        deselectEntry.callback.AddListener((BaseEventData _) => RestoreOriginalDialogue());
        trigger.triggers.Add(deselectEntry);
    }

    // Update is called once per frame
    void Update() {
        comTime += Time.deltaTime;

        turnTracker.GetComponent<Text>().text = game.state.getTurnText();
        turnTrackerDrop.GetComponent<Text>().text = game.state.getTurnText();

        if (dialogueText.text != targetText) {
            charsShown += 1;
            dialogueText.text = targetText.Substring(0, charsShown);
        } else {
            if (WaitForDialogueAnswer()) {
                if (endOfChain) {
                    ToggleActivity(-1, -1, -1, -1, 1, -1, -1, -1);
                }
            }
        }

        // Limpar descrições apenas quando mudamos para um contexto que não precisa delas
        // (como Dialogue, Spinner, etc.) mas não quando estamos em transição
        if (mostRecentPrompt != "Options" && (mostRecentPrompt == "Dialogue" || mostRecentPrompt == "Splash Screen")) {
            if (choiceDescriptions != null && choiceDescriptions.Count > 0) {
                choiceDescriptions.Clear();
            }
        }

        if (spinning) {
            if (spinnerFrame == 0) {
                windDown--;
                spinnerLoc++;
                if (spinnerLoc > 7) {
                    spinnerLoc = 1;
                }
                if (Input.GetKeyDown(KeyCode.Space) && windDown < -50) {
                    windDown = Random.Range(6, 9);
                } else if (windDown <= 0 && mostRecentPrompt == "Spinner") {
                    spinning = false;
                    mostRecentAns = spinnerChoices[spinnerLoc - 1];
                }
                if (windDown <= 3) {
                    spinnerFrame = 3;
                } else {
                    spinnerFrame = 1;
                }
            } else {
                spinnerFrame--;
            }
        }

        if (mostRecentPrompt == "Counter") {
            mostRecentAns = "" + counterStatus;
        }

        outcome1.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 1 ? 1.0f : 0.5f);
        outcome2.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 2 ? 1.0f : 0.5f);
        outcome3.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 3 ? 1.0f : 0.5f);
        outcome4.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 4 ? 1.0f : 0.5f);
        outcome5.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 5 ? 1.0f : 0.5f);
        outcome6.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 6 ? 1.0f : 0.5f);
        outcome7.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, spinnerLoc == 7 ? 1.0f : 0.5f);
    }

    public void YourTurn(string character, Color color) {
        comTime = 0.0f;
        yourTurn.GetComponent<Text>().text = "Go, " + character + "!";
        mostRecentPrompt = "Splash Screen";
        yourTurnColor.color = color;
        ToggleActivity(-1, -1, 1, -1, 0, -1, -1, -1);
    }

    public void Dialogue(string targetText, bool endOfChain) {
        comTime = 0.0f;
        charsShown = 0;
        this.endOfChain = endOfChain;
        mostRecentPrompt = "Dialogue";
        ToggleActivity(1, -1, -1, -1, 0, -1, 0, -1);
        this.targetText = targetText;
    }

    public void Dialogue(string speaker, string targetText, bool endOfChain) {
        comTime = 0.0f;
        charsShown = 0;
        this.endOfChain = endOfChain;
        speakerText.text = speaker;
        mostRecentPrompt = "Dialogue";
        ToggleActivity(1, 1, -1, -1, 0, -1, 0, -1);
        this.targetText = targetText;
    }

    public void Dialogue(string targetText, List<string> choices, bool endOfChain) {
        comTime = 0.0f;
        charsShown = 0;
        this.endOfChain = endOfChain;
        this.mostRecentAns = "";
        mostRecentPrompt = "Options";
        ToggleActivity(1, -1, -1, 1, 0, -1, 0, -1);
        this.choices = choices;
        this.choices.Reverse();
        SetOptions();
        this.targetText = targetText;
        // Armazenar o texto original para restauração
        originalDialogueText = targetText;
        originalSpeakerText = "";
    }

    public void Dialogue(string speaker, string targetText, List<string> choices, bool endOfChain) {
        comTime = 0.0f;
        charsShown = 0;
        this.endOfChain = endOfChain;
        speakerText.text = speaker;
        this.mostRecentAns = "";
        mostRecentPrompt = "Options";
        ToggleActivity(1, 1, -1, 1, 0, -1, 0, -1);
        this.choices = choices;
        this.choices.Reverse();
        SetOptions();
        this.targetText = targetText;
        // Armazenar o texto original para restauração
        originalDialogueText = targetText;
        originalSpeakerText = speaker;
    }

    // Novos: descrições das opções
    public void SetOptionDescriptions(List<string> descriptions) {
        choiceDescriptions = descriptions ?? new List<string>();
        choiceDescriptions.Reverse(); // alinhar com a ordem invertida das opções
        
        // Garantir que o número de descrições corresponde ao número de opções
        if (choices != null && choiceDescriptions.Count != choices.Count) {
            // Se não há correspondência, limpar as descrições para evitar confusão
            choiceDescriptions = new List<string>();
        }
    }

    // Método para limpar descrições quando apropriado
    public void ClearOptionDescriptions() {
        choiceDescriptions = new List<string>();
    }

    // Variável para armazenar o texto original do diálogo
    private string originalDialogueText = "";
    private string originalSpeakerText = "";

    // Método para restaurar o texto original do diálogo
    private void RestoreOriginalDialogue() {
        if (mostRecentPrompt == "Options" && !string.IsNullOrEmpty(originalDialogueText)) {
            targetText = originalDialogueText;
            dialogueText.text = targetText;
            charsShown = targetText.Length;
            if (!string.IsNullOrEmpty(originalSpeakerText)) {
                speakerText.text = originalSpeakerText;
            }
        }
    }

    private void ShowOptionDescription(string title, string description) {
        if (mostRecentPrompt != "Options") return;
        
        // Só exibe descrição se ela não estiver vazia e for válida
        if (string.IsNullOrEmpty(description)) {
            // Se não há descrição, restaura o texto original
            RestoreOriginalDialogue();
            return;
        } else {
            // Se há descrição, mostra normalmente (sem animação)
            character.SetActive(true);
            speakerText.text = title;
            targetText = description;
            charsShown = targetText.Length; // Definir charsShown ANTES de atualizar dialogueText para evitar animação
            dialogueText.text = targetText; // Atualizar texto imediatamente
        }
    }

    public void OptionAHovered() {
        if (choices == null || choices.Count < 1) return;
        string t = choices[0];
        string d = (choiceDescriptions != null && choiceDescriptions.Count > 0) ? choiceDescriptions[0] : "";
        ShowOptionDescription(t, d);
    }
    public void OptionBHovered() {
        if (choices == null || choices.Count < 2) return;
        string t = choices[1];
        string d = (choiceDescriptions != null && choiceDescriptions.Count > 1) ? choiceDescriptions[1] : "";
        ShowOptionDescription(t, d);
    }
    public void OptionCHovered() {
        if (choices == null || choices.Count < 3) return;
        string t = choices[2];
        string d = (choiceDescriptions != null && choiceDescriptions.Count > 2) ? choiceDescriptions[2] : "";
        ShowOptionDescription(t, d);
    }
    public void OptionDHovered() {
        if (choices == null || choices.Count < 4) return;
        string t = choices[3];
        string d = (choiceDescriptions != null && choiceDescriptions.Count > 3) ? choiceDescriptions[3] : "";
        ShowOptionDescription(t, d);
    }
    public void OptionEHovered() {
        if (choices == null || choices.Count < 5) return;
        string t = choices[4];
        string d = (choiceDescriptions != null && choiceDescriptions.Count > 4) ? choiceDescriptions[4] : "";
        ShowOptionDescription(t, d);
    }

    public void Spinner(string title, Color titleColor, List<string> options) {
        comTime = 0.0f;
        outcome1.GetComponentsInChildren<Text>()[0].text = options[0];
        outcome2.GetComponentsInChildren<Text>()[0].text = options[1];
        outcome3.GetComponentsInChildren<Text>()[0].text = options[2];
        outcome4.GetComponentsInChildren<Text>()[0].text = options[3];
        outcome5.GetComponentsInChildren<Text>()[0].text = options[4];
        outcome6.GetComponentsInChildren<Text>()[0].text = options[5];
        outcome7.GetComponentsInChildren<Text>()[0].text = options[6];
        spinnerTitle.GetComponent<Text>().text = title;
        spinnerTitle.GetComponent<Text>().color = titleColor;
        spinnerTitleDrop.GetComponent<Text>().text = title;
        mostRecentPrompt = "Spinner";
        spinnerChoices = options;
        spinnerLoc = Random.Range(1, 7);
        spinning = true;
        windDown = -100;
        ToggleActivity(-1, -1, -1, -1, 0, -1, -1, 1);
    }

    public void Counter(int min, int max, bool countStars) {
        counterMin = min;
        counterMax = max;
        counterStatus = counterMin;
        ToggleActivity(0, -1, -1, -1, 0, 1, -1, -1);
        mostRecentPrompt = "Counter";
    }

    public void IncrementCounter() {
        counterStatus += 1;
        if (counterStatus > counterMax) {
            counterStatus = counterMax;
        }
    }

    public void DecrementCounter() {
        counterStatus -= 1;
        if (counterStatus < counterMin) {
            counterStatus = counterMin;
        }
    }

    private void ToggleActivity(int d, int c, int y, int o, int s, int cc, int sl, int sp) {
        dialogue.SetActive(d == 0 ? dialogue.activeInHierarchy : d > 0);
        character.SetActive(c == 0 ? character.activeInHierarchy : c > 0);
        yourTurn.SetActive(y == 0 ? yourTurn.activeInHierarchy : y > 0);
        options.SetActive(o == 0 ? options.activeInHierarchy : o > 0);
        counter.SetActive(cc == 0 ? counter.activeInHierarchy : cc > 0);
        spacesLeft.SetActive(sl == 0 ? spacesLeft.activeInHierarchy : sl > 0);
        spinner.SetActive(sp == 0 ? spinner.activeInHierarchy : sp > 0);
        if (s > 0 && standings.transform.position.y == 200) {
            StartCoroutine(MoveStandings(true));
        } else if (s < 0 && standings.transform.position.y == 0) {
            StartCoroutine(MoveStandings(false));
        }
    }

    private IEnumerator MoveStandings(bool moveIn) {
        if (moveIn) {
            for (int i = 0; i <= 10; i++) {
                standings.transform.position = new Vector3(0, i * 20, 0);
                yield return new WaitForSeconds(0.05f);
            }
        } else {
            for (int i = 10; i >= 0; i--) {
                standings.transform.position = new Vector3(0, i * 20, 0);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    private void SetOptions() {
        int c = choices.Count;
        optionA.GetComponentsInChildren<Text>()[0].text = choices[0];
        optionB.GetComponentsInChildren<Text>()[0].text = choices[1];
        if (c > 2) {
            optionC.GetComponentsInChildren<Text>()[0].text = choices[2];
            optionC.SetActive(true);
            if (c > 3) {
                optionD.GetComponentsInChildren<Text>()[0].text = choices[3];
                optionD.SetActive(true);
                if (c > 4) {    
                    optionE.GetComponentsInChildren<Text>()[0].text = choices[4];
                    optionE.SetActive(true);
                } else {
                    optionE.SetActive(false);
                }
            } else {
                optionD.SetActive(false);
                optionE.SetActive(false);
            }
        } else {
            optionC.SetActive(false);
            optionD.SetActive(false);
            optionE.SetActive(false);
        }
    }

    public void SetMoveCounterNumber(int i) {
        spacesLeft.GetComponent<Text>().text = "" + i;
    }

    public void MoveCounter(bool show) {
        spacesLeft.SetActive(show);
    }

    public bool WaitForDialogueAnswer() {
        if (controller == 0) {
            if (mostRecentPrompt == "Spinner") {
                return !spinning;
            } else if (mostRecentPrompt == "Options") {
                return mostRecentAns != "";
            } else {
                return Input.GetKeyDown(KeyCode.Space);
            }
        } else if (controller == 5) {
            // REMOTE PLAYER
            return false;
        } else {
            if (comTime >= 1.0f) {
                if (mostRecentPrompt == "Spinner") {
                    spinnerLoc = Random.Range(1, 7);
                    mostRecentAns = spinnerChoices[spinnerLoc];
                    spinning = false;
                }
                if (mostRecentPrompt == "Options") {
                    mostRecentAns = decisionAI.Prompt(targetText, choices, 0);
                }
                return true;
            }
            return false;
        }
    }

    public string MostRecentDialogueAnswer() {
        return mostRecentAns;
    }

    public void SetController(int c) {
        this.controller = c;
    }

    public void SetDecisionAI(AIBrain aib) {
        this.decisionAI = aib;
    }

    public void OptionAClicked() {
        mostRecentAns = choices[0];
    }

    public void OptionBClicked() {
        mostRecentAns = choices[1];
    }

    public void OptionCClicked() {
        mostRecentAns = choices[2];
    }

    public void OptionDClicked() {
        mostRecentAns = choices[3];
    }

    public void OptionEClicked() {
        mostRecentAns = choices[4];
    }

    // Método para atualizar o menu de detalhes se estiver aberto
    public void RefreshPlayerDetailMenu() {
        if (playerDetailMenu != null && playerDetailMenu.IsVisible()) {
            playerDetailMenu.RefreshDisplay();
        }
    }
}
