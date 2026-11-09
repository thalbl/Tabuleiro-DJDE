using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe o HUD do Timer de Turno para partidas multiplayer (Online e Local).
/// Posicionado elegantemente no canto superior direito abaixo do contador de turnos,
/// com barra de progresso suave, tipografia Fredoka limpa e sem efeitos de tremor/glitch.
/// </summary>
public class TurnTimerUI : MonoBehaviour {

    public static TurnTimerUI Instance { get; private set; }

    private GameObject containerTimer;
    private CanvasGroup canvasGroup;
    private RectTransform containerRT;
    private Image imgFundo;
    private Outline outline;
    private Image imgFillBarra;
    private Text txtTempo;
    private Text txtJogador;
    private Font fonteFredoka;

    // Paleta de Cores Suaves e Harmoniosas
    private readonly Color corNormal = new Color(0.13f, 0.77f, 0.37f, 1f);   // Verde Esmeralda #22c55e
    private readonly Color corAlerta = new Color(0.96f, 0.62f, 0.04f, 1f);   // Âmbar #f59e0b
    private readonly Color corCritica = new Color(0.94f, 0.27f, 0.27f, 1f);  // Vermelho Coral #ef4444

    private static readonly Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CriarHUDTimer();
    }

    private void CriarHUDTimer() {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (containerTimer != null) Destroy(containerTimer);

        containerTimer = new GameObject("TurnTimerHUD");
        containerTimer.transform.SetParent(canvas.transform, false);

        canvasGroup = containerTimer.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        containerRT = containerTimer.AddComponent<RectTransform>();
        // Posicionado no Canto Superior Direito, bem acima do contador de turnos
        containerRT.anchorMin = new Vector2(1f, 1f);
        containerRT.anchorMax = new Vector2(1f, 1f);
        containerRT.pivot = new Vector2(1f, 1f);
        containerRT.sizeDelta = new Vector2(280f, 56f);

        // Aplica posição inicial alinhada no canto superior direito
        AjustarPosicaoComSafeArea();

        // Fundo Dark Slate Glass
        imgFundo = containerTimer.AddComponent<Image>();
        imgFundo.color = new Color(0.04f, 0.12f, 0.08f, 0.94f);
        imgFundo.sprite = ObterSpriteArredondado(12);
        imgFundo.type = Image.Type.Sliced;

        outline = containerTimer.AddComponent<Outline>();
        outline.effectColor = new Color(0.29f, 0.87f, 0.50f, 0.45f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Sombra suave para destacar do tabuleiro
        Shadow shContainer = containerTimer.AddComponent<Shadow>();
        shContainer.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shContainer.effectDistance = new Vector2(2f, -2f);

        // --- LINHA SUPERIOR: Nome do Jogador e Segundos ---
        // Nome do Jogador Ativo (Alinhado à esquerda)
        GameObject txtJogObj = new GameObject("TxtJogador");
        txtJogObj.transform.SetParent(containerTimer.transform, false);
        RectTransform rtJog = txtJogObj.AddComponent<RectTransform>();
        rtJog.anchorMin = new Vector2(0, 0.42f);
        rtJog.anchorMax = new Vector2(1, 1f);
        rtJog.offsetMin = new Vector2(16, 0);
        rtJog.offsetMax = new Vector2(-76, -4);

        txtJogador = txtJogObj.AddComponent<Text>();
        txtJogador.text = "Vez de Jogador";
        txtJogador.font = fonteFredoka;
        txtJogador.fontSize = 17;
        txtJogador.fontStyle = FontStyle.Bold;
        txtJogador.resizeTextForBestFit = true;
        txtJogador.resizeTextMinSize = 13;
        txtJogador.resizeTextMaxSize = 18;
        txtJogador.alignment = TextAnchor.MiddleLeft;
        txtJogador.color = new Color(0.65f, 0.95f, 0.80f, 1f);
        txtJogador.horizontalOverflow = HorizontalWrapMode.Wrap;
        txtJogador.raycastTarget = false;

        Shadow shJog = txtJogObj.AddComponent<Shadow>();
        shJog.effectColor = new Color(0, 0, 0, 0.7f);
        shJog.effectDistance = new Vector2(1, -1);

        // Tempo Digital Restante (Alinhado à direita)
        GameObject txtTempoObj = new GameObject("TxtSegundos");
        txtTempoObj.transform.SetParent(containerTimer.transform, false);
        RectTransform rtTempo = txtTempoObj.AddComponent<RectTransform>();
        rtTempo.anchorMin = new Vector2(1, 0.42f);
        rtTempo.anchorMax = new Vector2(1, 1f);
        rtTempo.pivot = new Vector2(1, 0.5f);
        rtTempo.sizeDelta = new Vector2(65, 28);
        rtTempo.anchoredPosition = new Vector2(-16, -2);

        txtTempo = txtTempoObj.AddComponent<Text>();
        txtTempo.text = "45s";
        txtTempo.font = fonteFredoka;
        txtTempo.fontSize = 22;
        txtTempo.fontStyle = FontStyle.Bold;
        txtTempo.alignment = TextAnchor.MiddleRight;
        txtTempo.color = Color.white;
        txtTempo.raycastTarget = false;

        Shadow shTempo = txtTempoObj.AddComponent<Shadow>();
        shTempo.effectColor = new Color(0, 0, 0, 0.7f);
        shTempo.effectDistance = new Vector2(1, -1);

        // --- LINHA INFERIOR: Barra de Progresso Suave ---
        GameObject barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(containerTimer.transform, false);
        RectTransform rtBarBg = barBgObj.AddComponent<RectTransform>();
        rtBarBg.anchorMin = new Vector2(0, 0);
        rtBarBg.anchorMax = new Vector2(1, 0.42f);
        rtBarBg.offsetMin = new Vector2(16, 8);
        rtBarBg.offsetMax = new Vector2(-16, 0);

        Image imgBarBg = barBgObj.AddComponent<Image>();
        imgBarBg.color = new Color(0.06f, 0.16f, 0.11f, 0.95f);
        imgBarBg.sprite = ObterSpriteArredondado(6);
        imgBarBg.type = Image.Type.Sliced;

        // Preenchimento (Fill)
        GameObject barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform rtFill = barFillObj.AddComponent<RectTransform>();
        rtFill.anchorMin = Vector2.zero;
        rtFill.anchorMax = Vector2.one;
        rtFill.sizeDelta = Vector2.zero;

        imgFillBarra = barFillObj.AddComponent<Image>();
        imgFillBarra.sprite = ObterSpriteArredondado(6);
        imgFillBarra.type = Image.Type.Filled;
        imgFillBarra.fillMethod = Image.FillMethod.Horizontal;
        imgFillBarra.fillOrigin = 0; // Preenchimento da esquerda para direita
        imgFillBarra.fillAmount = 1f;
        imgFillBarra.color = corNormal;
    }

    /// <summary>
    /// Reposiciona o Timer no Canto Superior Direito (bem acima do contador de turnos).
    /// </summary>
    public void AtualizarPosicao(float sidePadding, float topPadding) {
        if (containerRT == null) return;
        containerRT.anchorMin = new Vector2(1f, 1f);
        containerRT.anchorMax = new Vector2(1f, 1f);
        containerRT.pivot = new Vector2(1f, 1f);
        containerRT.anchoredPosition = new Vector2(-sidePadding - 25f, -topPadding - 8f);
    }

    public void AjustarPosicaoComSafeArea() {
        Rect safeArea = Screen.safeArea;
        float topRatio = 0f;
        float sideRatio = 0f;

        if (Screen.height > 0 && Screen.width > 0) {
            topRatio = (Screen.height - (safeArea.y + safeArea.height)) / (float)Screen.height;
            float leftRatio = safeArea.x / (float)Screen.width;
            float rightRatio = (Screen.width - (safeArea.x + safeArea.width)) / (float)Screen.width;
            sideRatio = Mathf.Max(leftRatio, rightRatio);
        }

        float topPadding = Mathf.Max(topRatio * 1080f, 50f);
        float sidePadding = Mathf.Max(sideRatio * 1920f, 40f);

        AtualizarPosicao(sidePadding, topPadding);
    }

    void Update() {
        if (containerTimer == null || canvasGroup == null) return;

        NetworkTurnManager netMgr = NetworkTurnManager.Instance;
        bool ativo = (netMgr != null && netMgr.IsTurnoAtivo);

        // Transição suave de fade in/out
        float targetAlpha = ativo ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);

        if (!ativo && canvasGroup.alpha <= 0.01f) {
            return;
        }

        if (netMgr == null) return;

        float restante = netMgr.TempoRestante;
        float progresso = netMgr.ProgressoTimer;
        int segundosInt = Mathf.CeilToInt(restante);

        // Atualizar barra e texto
        imgFillBarra.fillAmount = progresso;
        txtTempo.text = $"{segundosInt}s";

        // Nome do jogador ativo
        string nome = netMgr.JogadorAtivoPlayer != null ? netMgr.JogadorAtivoPlayer.state.charName() : $"Jogador {netMgr.JogadorAtivoIndex + 1}";
        txtJogador.text = $"Vez de {nome}";

        // Escala 100% estável (SEM NENHUM TREMOR OU EFEITO DE GLITCH)
        containerRT.localScale = Vector3.one;

        // Transição suave de cores por faixas de tempo
        if (progresso > 0.45f) {
            imgFillBarra.color = corNormal;
            txtTempo.color = Color.white;
            outline.effectColor = new Color(0.29f, 0.87f, 0.50f, 0.40f);
        } else if (progresso > 0.20f) {
            imgFillBarra.color = corAlerta;
            txtTempo.color = new Color(1f, 0.92f, 0.60f, 1f);
            outline.effectColor = new Color(0.96f, 0.62f, 0.04f, 0.45f);
        } else {
            // Últimos segundos: destaque vermelho suave e estável
            imgFillBarra.color = corCritica;
            txtTempo.color = corCritica;
            outline.effectColor = new Color(0.94f, 0.27f, 0.27f, 0.60f);
        }
    }

    private static Sprite ObterSpriteArredondado(int radius) {
        if (spriteCache.TryGetValue(radius, out Sprite cached)) return cached;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        float r = radius;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(0, Mathf.Abs(x - (size / 2f)) - ((size / 2f) - r));
                float dy = Mathf.Max(0, Mathf.Abs(y - (size / 2f)) - ((size / 2f) - r));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                cols[y * size + x] = (dist <= r) ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        spriteCache[radius] = s;
        return s;
    }
}
