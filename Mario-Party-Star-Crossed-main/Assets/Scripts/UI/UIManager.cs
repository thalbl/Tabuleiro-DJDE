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

    // Monitoramento de Safe Area para Mobile
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    void Awake() {
        // Configura CanvasScaler para manter proporção balanceada e legibilidade em qualquer tela
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null) {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Garante que o gerenciador de entrada e configurações mobile esteja ativo
        MobileInputManager.EnsureExists();
    }

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

        // Ajusta a posição da HUD para não ser cortada por entalhes, cantos curvos e bordas mobile
        AjustarHUDParaSafeArea();
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

        // Atualiza layout da HUD se a resolução ou Safe Area mudar
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y) {
            AjustarHUDParaSafeArea();
        }

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
                if ((Input.GetKeyDown(KeyCode.Space) || MobileInputManager.IsTouchOrClickDown()) && windDown < -50) {
                    HapticFeedback.Vibrate();
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

        // Estiliza e reposiciona as opções de ação dinamicamente
        EstilizarEReposicionarOpcoes();
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
                return Input.GetKeyDown(KeyCode.Space) || MobileInputManager.IsTouchOrClickDown();
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
        HapticFeedback.Vibrate();
        mostRecentAns = choices[0];
    }

    public void OptionBClicked() {
        HapticFeedback.Vibrate();
        mostRecentAns = choices[1];
    }

    public void OptionCClicked() {
        HapticFeedback.Vibrate();
        mostRecentAns = choices[2];
    }

    public void OptionDClicked() {
        HapticFeedback.Vibrate();
        mostRecentAns = choices[3];
    }

    public void OptionEClicked() {
        HapticFeedback.Vibrate();
        mostRecentAns = choices[4];
    }

    // Método para atualizar o menu de detalhes se estiver aberto
    public void RefreshPlayerDetailMenu() {
        if (playerDetailMenu != null && playerDetailMenu.IsVisible()) {
            playerDetailMenu.RefreshDisplay();
        }
    }

    private static Sprite roundedBoxSprite;

    private static Sprite ObterSpriteArredondado() {
        if (roundedBoxSprite != null) return roundedBoxSprite;

        int w = 64;
        int h = 64;
        int r = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color transparente = new Color(0, 0, 0, 0);

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                bool dentro = (x >= r && x < w - r) || (y >= r && y < h - r);
                if (!dentro) {
                    float cx = x < r ? r : w - r - 1;
                    float cy = y < r ? r : h - r - 1;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    dentro = dist <= r;
                }
                tex.SetPixel(x, y, dentro ? Color.white : transparente);
            }
        }
        tex.Apply();
        roundedBoxSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return roundedBoxSprite;
    }

    private bool dialogoEstilizado = false;

    private void EstilizarCaixaDeDialogo() {
        if (dialogoEstilizado) return;
        dialogoEstilizado = true;

        Sprite spriteArredondado = ObterSpriteArredondado();
        Font fonte = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Estilização da Caixa de Diálogo Principal (Fundo Dark Slate translúcido com borda dourada)
        if (dialogue != null) {
            Image dImg = dialogue.GetComponent<Image>();
            if (dImg != null) {
                dImg.sprite = spriteArredondado;
                dImg.type = Image.Type.Sliced;
                dImg.color = new Color(0.06f, 0.09f, 0.15f, 0.94f);
            }

            Outline dOutline = dialogue.GetComponent<Outline>();
            if (dOutline == null) dOutline = dialogue.AddComponent<Outline>();
            dOutline.effectColor = new Color(0.98f, 0.75f, 0.14f, 0.70f); // Dourado
            dOutline.effectDistance = new Vector2(2.5f, -2.5f);

            Shadow dShadow = dialogue.GetComponent<Shadow>();
            if (dShadow == null) dShadow = dialogue.AddComponent<Shadow>();
            dShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            dShadow.effectDistance = new Vector2(4f, -4f);

            // Ajuste e formatação com FONTE AUMENTADA para leitura clara em mobile
            if (dialogueText != null) {
                if (fonte != null) dialogueText.font = fonte;
                dialogueText.color = new Color(0.96f, 0.98f, 1f, 1f);
                dialogueText.fontSize = 40;
                dialogueText.lineSpacing = 1.25f;
                dialogueText.alignment = TextAnchor.UpperLeft;
                dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
                dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

                RectTransform dtRT = dialogueText.GetComponent<RectTransform>();
                if (dtRT != null) {
                    dtRT.anchorMin = Vector2.zero;
                    dtRT.anchorMax = Vector2.one;
                    dtRT.offsetMin = new Vector2(40f, 25f);
                    dtRT.offsetMax = new Vector2(-40f, -25f);
                }
            }

            // Indicador sutil de "Toque para continuar ▸"
            Transform indTrans = dialogue.transform.Find("IndicadorContinuar");
            if (indTrans == null) {
                GameObject indObj = new GameObject("IndicadorContinuar");
                indObj.transform.SetParent(dialogue.transform, false);
                RectTransform indRT = indObj.AddComponent<RectTransform>();
                indRT.anchorMin = new Vector2(1f, 0f);
                indRT.anchorMax = new Vector2(1f, 0f);
                indRT.pivot = new Vector2(1f, 0f);
                indRT.anchoredPosition = new Vector2(-25f, 14f);
                indRT.sizeDelta = new Vector2(320f, 34f);

                Text indTxt = indObj.AddComponent<Text>();
                if (fonte != null) indTxt.font = fonte;
                indTxt.text = "Toque para continuar ▸";
                indTxt.fontSize = 24;
                indTxt.fontStyle = FontStyle.Bold;
                indTxt.alignment = TextAnchor.MiddleRight;
                indTxt.color = new Color(0.98f, 0.75f, 0.14f, 0.85f);
            }
        }

        // 2. Estilização da Aba do Título / Locutor (Character) com FONTE AUMENTADA
        if (character != null) {
            Image cImg = character.GetComponent<Image>();
            if (cImg != null) {
                cImg.sprite = spriteArredondado;
                cImg.type = Image.Type.Sliced;
                cImg.color = new Color(0.98f, 0.75f, 0.14f, 1f); // Dourado vibrante
            }

            Outline cOutline = character.GetComponent<Outline>();
            if (cOutline == null) cOutline = character.AddComponent<Outline>();
            cOutline.effectColor = new Color(0.35f, 0.15f, 0.02f, 0.65f);
            cOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow cShadow = character.GetComponent<Shadow>();
            if (cShadow == null) cShadow = character.AddComponent<Shadow>();
            cShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            cShadow.effectDistance = new Vector2(2f, -2f);

            if (speakerText != null) {
                if (fonte != null) speakerText.font = fonte;
                speakerText.color = new Color(0.25f, 0.08f, 0.01f, 1f); // Marrom escuro contrastante
                speakerText.fontStyle = FontStyle.Bold;
                speakerText.fontSize = 34;
                speakerText.alignment = TextAnchor.MiddleCenter;
                speakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
                speakerText.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform stRT = speakerText.GetComponent<RectTransform>();
                if (stRT != null) {
                    stRT.anchorMin = Vector2.zero;
                    stRT.anchorMax = Vector2.one;
                    stRT.offsetMin = new Vector2(15f, 5f);
                    stRT.offsetMax = new Vector2(-15f, -5f);
                }
            }
        }

        // 3. Estilização do Contador de Turno na HUD com FONTE AUMENTADA
        if (turnTracker != null) {
            Text tMain = turnTracker.GetComponent<Text>();
            if (tMain != null) {
                if (fonte != null) tMain.font = fonte;
                tMain.fontSize = 46;
                tMain.fontStyle = FontStyle.Bold;
                tMain.horizontalOverflow = HorizontalWrapMode.Overflow;
                tMain.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }
        if (turnTrackerDrop != null) {
            Text tDrop = turnTrackerDrop.GetComponent<Text>();
            if (tDrop != null) {
                if (fonte != null) tDrop.font = fonte;
                tDrop.fontSize = 46;
                tDrop.fontStyle = FontStyle.Bold;
                tDrop.horizontalOverflow = HorizontalWrapMode.Overflow;
                tDrop.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        // 4. Estilização do Contador de Passos (Spaces Left) com FONTE AUMENTADA
        if (spacesLeft != null) {
            Text slText = spacesLeft.GetComponent<Text>();
            if (slText != null) {
                if (fonte != null) slText.font = fonte;
                slText.fontSize = 64;
                slText.fontStyle = FontStyle.Bold;
                slText.horizontalOverflow = HorizontalWrapMode.Overflow;
                slText.verticalOverflow = VerticalWrapMode.Overflow;
            }
            Outline slOutline = spacesLeft.GetComponent<Outline>();
            if (slOutline == null) slOutline = spacesLeft.AddComponent<Outline>();
            slOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
            slOutline.effectDistance = new Vector2(3f, -3f);
        }

        // 5. Estilização do Splash de Turno (Your Turn) com FONTE AUMENTADA
        if (yourTurn != null) {
            Text ytText = yourTurn.GetComponent<Text>();
            if (ytText != null) {
                if (fonte != null) ytText.font = fonte;
                ytText.fontSize = 76;
                ytText.fontStyle = FontStyle.Bold;
                ytText.horizontalOverflow = HorizontalWrapMode.Overflow;
                ytText.verticalOverflow = VerticalWrapMode.Overflow;
            }
            if (yourTurnColor != null) {
                if (fonte != null) yourTurnColor.font = fonte;
                yourTurnColor.fontSize = 76;
                yourTurnColor.fontStyle = FontStyle.Bold;
                yourTurnColor.horizontalOverflow = HorizontalWrapMode.Overflow;
                yourTurnColor.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }
    }

    /// <summary>
    /// Estiliza individualmente um botão de escolha de ações com cantos arredondados,
    /// fundo Dark Slate elegante, contorno dourado, sombra e fonte Fredoka legível.
    /// </summary>
    private void EstilizarBotaoOpcao(GameObject btnObj, Sprite sprite, Font fonte) {
        if (btnObj == null) return;

        // Fundo arredondado elegante Dark Slate
        Image img = btnObj.GetComponent<Image>();
        if (img != null) {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);
        }

        // Borda dourada refinada
        Outline outline = btnObj.GetComponent<Outline>();
        if (outline == null) outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.98f, 0.75f, 0.14f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Sombra para profundidade visual
        Shadow shadow = btnObj.GetComponent<Shadow>();
        if (shadow == null) shadow = btnObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.60f);
        shadow.effectDistance = new Vector2(3.5f, -3.5f);

        // Transição de cores interativa ao toque/hover
        Button btn = btnObj.GetComponent<Button>();
        if (btn != null) {
            btn.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            cb.highlightedColor = new Color(0.18f, 0.28f, 0.44f, 1f);
            cb.pressedColor = new Color(0.98f, 0.75f, 0.14f, 0.50f);
            cb.selectedColor = new Color(0.18f, 0.28f, 0.44f, 1f);
            btn.colors = cb;
        }

        // Texto do Botão em negrito e com tamanho calibrado
        Text btnText = btnObj.GetComponentInChildren<Text>();
        if (btnText != null) {
            if (fonte != null) btnText.font = fonte;
            btnText.fontSize = 28;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.96f, 0.98f, 1f, 1f);
            btnText.horizontalOverflow = HorizontalWrapMode.Wrap;
            btnText.verticalOverflow = VerticalWrapMode.Truncate;

            Shadow tShadow = btnText.GetComponent<Shadow>();
            if (tShadow == null) tShadow = btnText.gameObject.AddComponent<Shadow>();
            tShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            tShadow.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform textRT = btnText.GetComponent<RectTransform>();
            if (textRT != null) {
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(16f, 4f);
                textRT.offsetMax = new Vector2(-16f, -4f);
            }
        }
    }

    /// <summary>
    /// Reposiciona os botões de opção no canto inferior direito, empilhados verticalmente
    /// logo acima da caixa de diálogo, liberando completamente o centro da tela e o topo.
    /// </summary>
    private void EstilizarEReposicionarOpcoes() {
        Sprite spriteArredondado = ObterSpriteArredondado();
        Font fonte = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonte == null) fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // CRUCIAL: Garantir que o container pai "Options" ocupe a tela inteira
        // para que as âncoras dos botões filhos sejam relativas à tela e não a um ponto central
        if (options != null) {
            RectTransform optRT = options.GetComponent<RectTransform>();
            if (optRT != null) {
                optRT.anchorMin = Vector2.zero;
                optRT.anchorMax = Vector2.one;
                optRT.offsetMin = Vector2.zero;
                optRT.offsetMax = Vector2.zero;
                optRT.pivot = new Vector2(0.5f, 0.5f);
                optRT.localScale = Vector3.one;
                optRT.localPosition = Vector3.zero;
            }
        }

        // Obter paddings da Safe Area calculados
        float sidePadding = 40f;
        float bottomPadding = 35f;
        Rect safeArea = Screen.safeArea;
        if (Screen.height > 0 && Screen.width > 0) {
            float bottomRatio = safeArea.y / (float)Screen.height;
            float leftRatio = safeArea.x / (float)Screen.width;
            float rightRatio = (Screen.width - (safeArea.x + safeArea.width)) / (float)Screen.width;
            float sideRatio = Mathf.Max(leftRatio, rightRatio);
            bottomPadding = Mathf.Max(bottomRatio * 1080f, 35f);
            sidePadding = Mathf.Max(sideRatio * 1920f, 40f);
        }

        float dialogHeight = 210f;
        float baseGap = 12f;
        float btnWidth = 440f;
        float btnHeight = 58f;
        float btnSpacing = 8f;

        GameObject[] botoes = new GameObject[] { optionA, optionB, optionC, optionD, optionE };
        for (int i = 0; i < botoes.Length; i++) {
            GameObject btnObj = botoes[i];
            if (btnObj == null) continue;

            EstilizarBotaoOpcao(btnObj, spriteArredondado, fonte);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            if (rt != null) {
                // Ancorado no canto inferior direito da tela
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(btnWidth, btnHeight);

                // Posicionamento empilhado de baixo para cima acima da caixa de diálogo no canto direito
                float posX = -sidePadding - 25f;
                float posY = bottomPadding + dialogHeight + baseGap + (i * (btnHeight + btnSpacing));
                rt.anchoredPosition = new Vector2(posX, posY);
            }
        }
    }

    /// <summary>
    /// Ajusta os painéis da HUD para respeitar a Safe Area e cantos arredondados de dispositivos móveis.
    /// Posiciona a caixa de diálogo na base da tela e o título logo acima dela.
    /// </summary>
    public void AjustarHUDParaSafeArea() {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        // Aplica o novo design visual refinado e fontes aumentadas
        EstilizarCaixaDeDialogo();
        EstilizarEReposicionarOpcoes();

        // Proporções da Safe Area em relação à tela real
        float topRatio = 0f;
        float bottomRatio = 0f;
        float sideRatio = 0f;

        if (Screen.height > 0 && Screen.width > 0) {
            topRatio = (Screen.height - (safeArea.y + safeArea.height)) / (float)Screen.height;
            bottomRatio = safeArea.y / (float)Screen.height;
            float leftRatio = safeArea.x / (float)Screen.width;
            float rightRatio = (Screen.width - (safeArea.x + safeArea.width)) / (float)Screen.width;
            sideRatio = Mathf.Max(leftRatio, rightRatio);
        }

        // Converte para unidades do Canvas (base 1080p)
        float topPadding = Mathf.Max(topRatio * 1080f, 50f);
        float bottomPadding = Mathf.Max(bottomRatio * 1080f, 35f);
        float sidePadding = Mathf.Max(sideRatio * 1920f, 40f);

        // 1. Ajustar Barra Superior (Standings - Cartões dos Jogadores)
        if (standings != null) {
            RectTransform rt = standings.GetComponent<RectTransform>();
            if (rt != null) {
                Vector2 pos = rt.anchoredPosition;
                pos.y = -topPadding;
                rt.anchoredPosition = pos;
                rt.localScale = new Vector3(0.88f, 0.88f, 1f);
            }
        }

        // 2. Ajustar Caixa de Diálogo (Inferior - fixada na base da tela)
        float dialogHeight = 210f;
        if (dialogue != null) {
            RectTransform rt = dialogue.GetComponent<RectTransform>();
            if (rt != null) {
                // Força âncoras na base da tela para nunca subir ao centro
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);

                rt.anchoredPosition = new Vector2(0f, bottomPadding);
                rt.sizeDelta = new Vector2(-sidePadding * 2f, dialogHeight);
            }
        }

        // 3. Ajustar Título / Locutor (Character) - Posicionado LOGO ACIMA da caixa de diálogo
        if (character != null) {
            RectTransform rt = character.GetComponent<RectTransform>();
            if (rt != null) {
                // Força âncoras na base da tela alinhado com a caixa de diálogo
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);

                // Posicionado exatamente em cima do teto da caixa de diálogo no canto esquerdo
                rt.anchoredPosition = new Vector2(sidePadding + 25f, bottomPadding + dialogHeight - 4f);
                rt.sizeDelta = new Vector2(340f, 62f);
            }
        }

        // 4. Ajustar Contador de Passos (Spaces Left)
        if (spacesLeft != null) {
            RectTransform rt = spacesLeft.GetComponent<RectTransform>();
            if (rt != null) {
                Vector2 pos = rt.anchoredPosition;
                pos.y = -topPadding - 80f;
                rt.anchoredPosition = pos;
            }
        }
    }
}
