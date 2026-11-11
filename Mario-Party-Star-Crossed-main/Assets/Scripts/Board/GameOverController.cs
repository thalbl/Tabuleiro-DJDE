using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tela de resultado. Construída em código no padrão do TemaUI (a UI antiga da cena é desligada).
/// Lê os dados gravados em PlayerPrefs pelo BoardManager.EndGame().
/// </summary>
public class GameOverController : MonoBehaviour {
    [Header("UI antiga da cena (desligada ao iniciar)")]
    public Transform playersContainer;
    public GameObject playerPanelPrefab;
    public UnityEngine.UI.Button returnButton;

    static readonly string[] NiveisEducacao = { "Sem formação", "Ensino Médio", "Ensino Superior", "Pós-graduação" };

    void Start() {
        DesligarUIAntiga();
        Construir();
    }

    void DesligarUIAntiga() {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
            c.enabled = false;
            var gr = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
        }
        if (Camera.main != null) {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = TemaUI.Hex("#0B1F17");
        }
    }

    void Construir() {
        var go = new GameObject("[Resultado]");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null) {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Fundo
        var fundo = TemaUI.Painel_(go.transform, "Fundo", TemaUI.Hex("#14532D"));
        fundo.sprite = null;
        TemaUI.Esticar(fundo.rectTransform);
        var vinheta = TemaUI.Painel_(fundo.transform, "Faixa", TemaUI.Hex("#0B1F17", 0.55f));
        vinheta.sprite = null;
        var vrt = vinheta.rectTransform;
        vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 0.18f); vrt.offsetMin = vrt.offsetMax = Vector2.zero;

        int jogadores = Mathf.Clamp(PlayerPrefs.GetInt("ActivePlayers", 4), 1, 4);
        string vencedor = PlayerPrefs.GetString("Player1Name", "Jogador 1");
        bool porMeta = PlayerPrefs.GetString("Player1ScoreBreakdown", "").StartsWith("META");

        // Título
        var titulo = TemaUI.Texto(fundo.transform, "Fim de Jogo", 84, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.ContornoTexto(titulo, TemaUI.SombraVerde, 5f);
        Posicionar(titulo.rectTransform, 0.5f, 1f, new Vector2(1400, 110), new Vector2(0, -70));

        string sub = porMeta ? $"{vencedor} alcançou a meta de carreira e venceu!" : $"{vencedor} venceu com a maior pontuação!";
        var subtitulo = TemaUI.Texto(fundo.transform, sub, TemaUI.TamSubtitulo, TemaUI.TextoClaro, FontStyle.Normal, TextAnchor.MiddleCenter);
        TemaUI.SombraTexto(subtitulo);
        Posicionar(subtitulo.rectTransform, 0.5f, 1f, new Vector2(1400, 50), new Vector2(0, -180));

        // Lista
        var lista = TemaUI.Criar("Lista", fundo.transform);
        Posicionar(lista, 0.5f, 1f, new Vector2(1440, 4 * 142), new Vector2(0, -255));
        var v = lista.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        v.spacing = 16; v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        for (int pos = 1; pos <= jogadores; pos++) Linha(lista, pos);

        // Botão
        var voltar = TemaUI.Botao(fundo.transform, "Voltar ao Menu", TemaUI.EstiloBotao.Destaque, ReturnToMainMenu, 34);
        Posicionar((RectTransform)voltar.transform, 0.5f, 0f, new Vector2(440, 90), new Vector2(0, 60));
    }

    void Linha(Transform pai, int pos) {
        string nome = PlayerPrefs.GetString($"Player{pos}Name", $"Jogador {pos}");
        string carreira = PlayerPrefs.GetString($"Player{pos}Career", "Nv. 1 - Estagiário");
        int moedas = PlayerPrefs.GetInt($"Player{pos}Money", 0);
        int estrelas = PlayerPrefs.GetInt($"Player{pos}Stars", 0);
        int educacao = Mathf.Clamp(PlayerPrefs.GetInt($"Player{pos}EducationLevel", 0), 0, NiveisEducacao.Length - 1);
        int pontos = PlayerPrefs.GetInt($"Player{pos}FinalScore", 0);
        bool meta = PlayerPrefs.GetString($"Player{pos}ScoreBreakdown", "").StartsWith("META");
        bool primeiro = pos == 1;

        var cartao = TemaUI.Painel_(pai, "Jogador" + pos, primeiro ? TemaUI.Hex("#1F5A3F") : TemaUI.Painel);
        var le = cartao.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = le.minHeight = 126;
        if (primeiro) {
            var borda = cartao.gameObject.AddComponent<UnityEngine.UI.Outline>();
            borda.effectColor = TemaUI.Ouro; borda.effectDistance = new Vector2(3, -3);
        }

        // Medalha de posição
        Color[] medalhas = { TemaUI.Ouro, TemaUI.Hex("#CBD5E1"), TemaUI.Hex("#D9884A"), TemaUI.Hex("#3F5249") };
        var medalha = TemaUI.Painel_(cartao.transform, "Medalha", medalhas[pos - 1]);
        medalha.sprite = TemaUI.Circulo; medalha.type = UnityEngine.UI.Image.Type.Simple;
        Posicionar(medalha.rectTransform, 0f, 0.5f, new Vector2(88, 88), new Vector2(64, 0), new Vector2(0.5f, 0.5f));
        var tPos = TemaUI.Texto(medalha.transform, pos + "º", 40, pos <= 3 ? TemaUI.Hex("#1C1917") : TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(tPos.rectTransform);

        // Nome + carreira
        var tNome = TemaUI.Texto(cartao.transform, nome, 38, TemaUI.TextoClaro, FontStyle.Bold);
        Ancorar(tNome.rectTransform, 0.08f, 0.52f, 0.45f, 0.95f);
        var tCarreira = TemaUI.Texto(cartao.transform, carreira + "  ·  " + NiveisEducacao[educacao], TemaUI.TamPequeno, TemaUI.TextoSuave);
        Ancorar(tCarreira.rectTransform, 0.08f, 0.1f, 0.6f, 0.5f);
        if (meta) {
            var etiqueta = TemaUI.Painel_(cartao.transform, "Meta", TemaUI.Ouro);
            Posicionar(etiqueta.rectTransform, 0.43f, 0.73f, new Vector2(150, 40), Vector2.zero, new Vector2(0.5f, 0.5f));
            var tMeta = TemaUI.Texto(etiqueta.transform, "META", 24, TemaUI.OuroEscuro, FontStyle.Bold, TextAnchor.MiddleCenter);
            TemaUI.Esticar(tMeta.rectTransform);
        }

        // Estatísticas
        Chip(cartao.transform, 0.6f, "texture/icons/star", estrelas.ToString());
        Chip(cartao.transform, 0.7f, "texture/icons/coin", moedas.ToString()); // patrimônio líquido (moedas + aplicações - dívidas)

        var tPontos = TemaUI.Texto(cartao.transform, pontos + "\n<size=22>pontos</size>", 46, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleCenter);
        tPontos.lineSpacing = 0.8f;
        TemaUI.SombraTexto(tPontos);
        Ancorar(tPontos.rectTransform, 0.82f, 0.05f, 0.98f, 0.95f);
    }

    static void Chip(Transform pai, float x, string icone, string valor) {
        var chip = TemaUI.Painel_(pai, "Chip", TemaUI.Hex("#0B1F17", 0.8f));
        Posicionar(chip.rectTransform, x, 0.5f, new Vector2(150, 64), new Vector2(0, 0), new Vector2(0, 0.5f));
        var img = TemaUI.Criar("Icone", chip.transform).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        img.texture = Resources.Load<Texture2D>(icone);
        img.raycastTarget = false;
        Posicionar(img.rectTransform, 0f, 0.5f, new Vector2(48, 48), new Vector2(14, 0), new Vector2(0, 0.5f));
        var t = TemaUI.Texto(chip.transform, valor, 32, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleLeft);
        Ancorar(t.rectTransform, 0.46f, 0f, 1f, 1f);
    }

    static void Posicionar(RectTransform rt, float ax, float ay, Vector2 tamanho, Vector2 pos, Vector2? pivo = null) {
        rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
        rt.pivot = pivo ?? new Vector2(0.5f, ay);
        rt.sizeDelta = tamanho;
        rt.anchoredPosition = pos;
    }

    static void Ancorar(RectTransform rt, float x0, float y0, float x1, float y1) {
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public void ReturnToMainMenu() {
        PlayerPrefs.DeleteKey("LoadSavedGame");
        SceneManager.LoadScene("StartScene");
    }
}
