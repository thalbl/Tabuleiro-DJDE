using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections;

/// <summary>
/// Constrói o menu principal "Caminho do Sucesso" inteiramente via código.
/// Adicione este script a um GameObject vazio na cena StartScene.
/// Ele cria Canvas, background, título e botões automaticamente.
/// </summary>
public class MainMenuBuilder : MonoBehaviour {

    // =================== CORES DO DESIGN ===================
    // Fundo
    private Color corVerdeFundo = new Color(0.29f, 0.87f, 0.50f, 1f);      // #4ade80
    private Color corVerdeEscuro = new Color(0.09f, 0.40f, 0.20f, 1f);     // #166534

    // Botões normais
    private Color corBotaoNormal = new Color(0.94f, 0.99f, 0.96f, 1f);     // #f0fdf4
    private Color corTextoBotao = new Color(0.09f, 0.40f, 0.20f, 1f);      // #166534
    private Color corSombraBotao = new Color(0.02f, 0.31f, 0.23f, 1f);     // #064e3b

    // Hover
    private Color corBotaoHover = new Color(0.98f, 0.75f, 0.14f, 1f);      // #fbbf24
    private Color corTextoHover = new Color(0.27f, 0.10f, 0.01f, 1f);      // #451a03

    // Botão Sair
    private Color corBotaoSair = new Color(0.94f, 0.27f, 0.27f, 1f);       // #ef4444
    private Color corSombraSair = new Color(0.50f, 0.11f, 0.11f, 1f);      // #7f1d1d

    // Título
    private Color corTituloBranco = Color.white;
    private Color corTituloDourado = new Color(0.98f, 0.75f, 0.14f, 1f);   // #fbbf24

    // =================== CONFIGURAÇÕES ===================
    private float larguraBotao = 400f;
    private float alturaBotao = 80f;
    private float espacoEntreBotoes = 26f;
    private int tamFonteBotao = 38;
    private int tamFonteTitulo = 90;
    private int tamFonteDestaque = 120;

    // =================== REFERÊNCIAS ===================
    private Canvas canvas;
    private Button btnContinuar;
    private Font fonteFredoka;
    private RectTransform tituloRT;
    private TutorialManager tutorial;

    void Start() {
        fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CriarEventSystem();
        CriarCanvas();
        CriarFundo();
        CriarPadraoFundo();
        CriarTitulo();
        CriarBotoes();
        StartCoroutine(AnimarTitulo());

        // Tutorial: criar instância (abre via botão "Como Jogar")
        tutorial = gameObject.AddComponent<TutorialManager>();
    }

    // =================== EVENT SYSTEM ===================
    private void CriarEventSystem() {
        if (FindObjectOfType<EventSystem>() == null) {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // =================== CANVAS ===================
    private void CriarCanvas() {
        GameObject canvasObj = new GameObject("MenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    // =================== FUNDO GRADIENTE ===================
    private void CriarFundo() {
        // Fundo sólido verde escuro (base)
        GameObject bg = CriarPainel("Fundo", canvas.transform, Color.clear);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        ExpandirParaTela(bgRT);

        // Usar gradiente via textura
        Image bgImage = bg.GetComponent<Image>();
        Texture2D gradientTex = CriarTexturaGradienteRadial(256, 256, corVerdeFundo, corVerdeEscuro);
        bgImage.sprite = Sprite.Create(gradientTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        bgImage.type = Image.Type.Simple;
        bgImage.color = Color.white;
    }

    // =================== PADRÃO DE BOLINHAS ===================
    private void CriarPadraoFundo() {
        GameObject dots = CriarPainel("PadraoFundo", canvas.transform, new Color(1, 1, 1, 0.06f));
        ExpandirParaTela(dots.GetComponent<RectTransform>());
    }

    // =================== TÍTULO ===================
    private void CriarTitulo() {
        // Container do título
        GameObject tituloContainer = new GameObject("TituloContainer");
        tituloContainer.transform.SetParent(canvas.transform, false);
        tituloRT = tituloContainer.AddComponent<RectTransform>();
        tituloRT.anchorMin = new Vector2(0.5f, 0.75f);
        tituloRT.anchorMax = new Vector2(0.5f, 0.75f);
        tituloRT.sizeDelta = new Vector2(800, 250);
        tituloRT.anchoredPosition = Vector2.zero;
        tituloRT.localRotation = Quaternion.Euler(0, 0, -3f); // Inclinação leve

        // "CAMINHO DO"
        CriarTexto("TxtCaminho", tituloContainer.transform,
            "CAMINHO DO", tamFonteTitulo, corTituloBranco,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
            FontStyle.Bold, true);

        // "SUCESSO" (destaque dourado)
        CriarTexto("TxtSucesso", tituloContainer.transform,
            "SUCESSO", tamFonteDestaque, corTituloDourado,
            new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f),
            FontStyle.Bold, true);
    }

    // =================== BOTÕES ===================
    private void CriarBotoes() {
        // Container dos botões
        GameObject container = new GameObject("BotoesContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.35f);
        containerRT.anchorMax = new Vector2(0.5f, 0.35f);
        containerRT.sizeDelta = new Vector2(larguraBotao, 400);
        containerRT.anchoredPosition = Vector2.zero;

        float yOffset = 0;

        // Botão Jogar (Novo Jogo)
        CriarBotaoMenu(container.transform, "Jogar", yOffset, false, () => NovoJogo());
        yOffset -= (alturaBotao + espacoEntreBotoes);

        // Botão Continuar
        GameObject btnContinuarObj = CriarBotaoMenu(container.transform, "Continuar", yOffset, false, () => ContinuarJogo());
        btnContinuar = btnContinuarObj.GetComponent<Button>();
        yOffset -= (alturaBotao + espacoEntreBotoes);

        // Verificar se existe save
        string savePath = Application.dataPath + "/Data/mario_party_save.dat";
        bool temSave = File.Exists(savePath);
        btnContinuar.interactable = temSave;
        if (!temSave) {
            // Deixar visualmente desabilitado
            Image img = btnContinuarObj.GetComponent<Image>();
            if (img != null) img.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            Text txt = btnContinuarObj.GetComponentInChildren<Text>();
            if (txt != null) txt.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            // Desabilitar efeito de hover
            MenuButtonEffect fx = btnContinuarObj.GetComponent<MenuButtonEffect>();
            if (fx != null) fx.enabled = false;
        }

        // Botão Como Jogar
        CriarBotaoMenu(container.transform, "Como Jogar", yOffset, false, () => AbrirTutorial());
        yOffset -= (alturaBotao + espacoEntreBotoes);

        // Botão Sair
        CriarBotaoMenu(container.transform, "Sair", yOffset, true, () => SairJogo());
    }

    // =================== FACTORY DE BOTÃO ===================
    private GameObject CriarBotaoMenu(Transform parent, string texto, float yPos, bool isSair, UnityEngine.Events.UnityAction onClick) {
        // --- Sombra ---
        GameObject sombraObj = new GameObject(texto + "_Sombra");
        sombraObj.transform.SetParent(parent, false);
        RectTransform sombraRT = sombraObj.AddComponent<RectTransform>();
        sombraRT.anchorMin = new Vector2(0.5f, 1f);
        sombraRT.anchorMax = new Vector2(0.5f, 1f);
        sombraRT.sizeDelta = new Vector2(larguraBotao, alturaBotao);
        sombraRT.anchoredPosition = new Vector2(-5f, yPos - 6f); // Deslocamento da sombra
        sombraRT.localRotation = Quaternion.Euler(0, 0, 6f); // Skew

        Image sombraImg = sombraObj.AddComponent<Image>();
        sombraImg.color = isSair ? corSombraSair : corSombraBotao;
        sombraImg.raycastTarget = false;

        // Bordas arredondadas na sombra
        AplicarBordasArredondadas(sombraImg);

        // --- Botão ---
        GameObject btnObj = new GameObject(texto + "_Btn");
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 1f);
        btnRT.anchorMax = new Vector2(0.5f, 1f);
        btnRT.sizeDelta = new Vector2(larguraBotao, alturaBotao);
        btnRT.anchoredPosition = new Vector2(0, yPos);
        btnRT.localRotation = Quaternion.Euler(0, 0, 6f); // Skew (inclinação)

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = isSair ? corBotaoSair : corBotaoNormal;
        AplicarBordasArredondadas(btnImg);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        // Desabilitar transição de cor padrão (vamos usar MenuButtonEffect)
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor = Color.white;
        cb.selectedColor = Color.white;
        btn.colors = cb;

        btn.onClick.AddListener(onClick);

        // --- Texto ---
        GameObject txtObj = new GameObject("Texto");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRT = txtObj.AddComponent<RectTransform>();
        ExpandirParaTela(txtRT);

        Text btnText = txtObj.AddComponent<Text>();
        btnText.text = texto;
        btnText.font = fonteFredoka;
        btnText.fontSize = tamFonteBotao;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = isSair ? Color.white : corTextoBotao;

        // --- Efeito de Animação ---
        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = isSair ? corBotaoSair : corBotaoNormal;
        fx.corHover = isSair ? new Color(0.86f, 0.15f, 0.15f, 1f) : corBotaoHover;
        fx.corTextoNormal = isSair ? Color.white : corTextoBotao;
        fx.corTextoHover = isSair ? Color.white : corTextoHover;
        fx.escalaHover = 1.1f;
        fx.subirHover = 5f;

        return btnObj;
    }

    // =================== AÇÕES DOS BOTÕES ===================
    private void NovoJogo() {
        // Limpar save existente
        string savePath = Application.dataPath + "/Data/mario_party_save.dat";
        if (File.Exists(savePath)) {
            File.Delete(savePath);
        }
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        SceneManager.LoadScene("MVP");
    }

    private void ContinuarJogo() {
        PlayerPrefs.SetInt("LoadSavedGame", 1);
        SceneManager.LoadScene("MVP");
    }

    private void SairJogo() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    private void AbrirTutorial() {
        if (tutorial != null) tutorial.MostrarTutorial();
    }

    private IEnumerator MostrarTutorialAposDelay() {
        yield return new WaitForSeconds(0.8f);
        if (tutorial != null) tutorial.MostrarTutorial();
    }

    // =================== ANIMAÇÃO DO TÍTULO ===================
    private IEnumerator AnimarTitulo() {
        float tempo = 0;
        Vector2 posOriginal = tituloRT.anchoredPosition;
        while (true) {
            tempo += Time.deltaTime;
            float y = Mathf.Sin(tempo * 1.2f) * 15f;
            tituloRT.anchoredPosition = posOriginal + new Vector2(0, y);
            yield return null;
        }
    }

    // =================== UTILITÁRIOS ===================
    private GameObject CriarPainel(string nome, Transform parent, Color cor) {
        GameObject panel = new GameObject(nome);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
        return panel;
    }

    private void ExpandirParaTela(RectTransform rt) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private Text CriarTexto(string nome, Transform parent, string conteudo, int tamanho, Color cor,
                             Vector2 anchorMin, Vector2 anchorMax, FontStyle estilo, bool outline) {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = new Vector2(800, tamanho + 30);
        rt.anchoredPosition = Vector2.zero;

        Text txt = obj.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonteFredoka;
        txt.fontSize = tamanho;
        txt.fontStyle = estilo;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = cor;
        txt.raycastTarget = false;

        if (outline) {
            Outline ol = obj.AddComponent<Outline>();
            ol.effectColor = corSombraBotao;
            ol.effectDistance = new Vector2(3, -3);

            Shadow sh = obj.AddComponent<Shadow>();
            sh.effectColor = corSombraBotao;
            sh.effectDistance = new Vector2(5, -5);
        }

        return txt;
    }

    private Texture2D CriarTexturaGradienteRadial(int largura, int altura, Color centro, Color borda) {
        Texture2D tex = new Texture2D(largura, altura);
        Vector2 center = new Vector2(largura / 2f, altura / 2f);
        float maxDist = Vector2.Distance(Vector2.zero, center);

        for (int x = 0; x < largura; x++) {
            for (int y = 0; y < altura; y++) {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                tex.SetPixel(x, y, Color.Lerp(centro, borda, dist));
            }
        }
        tex.Apply();
        return tex;
    }

    private void AplicarBordasArredondadas(Image img) {
        // Unity UI não suporta border-radius nativamente
        // Usamos um sprite arredondado gerado proceduralmente
        Texture2D tex = CriarTexturaArredondada(128, 48, 24, Color.white);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f),
                                   100f, 0, SpriteMeshType.FullRect,
                                   new Vector4(24, 24, 24, 24)); // Border para 9-slice
        img.type = Image.Type.Sliced;
    }

    private Texture2D CriarTexturaArredondada(int largura, int altura, int raio, Color cor) {
        Texture2D tex = new Texture2D(largura, altura);
        Color transparente = new Color(0, 0, 0, 0);

        for (int x = 0; x < largura; x++) {
            for (int y = 0; y < altura; y++) {
                // Verificar se o pixel está dentro da forma arredondada
                bool dentroDoRetangulo =
                    (x >= raio && x < largura - raio) ||
                    (y >= raio && y < altura - raio);

                bool dentroDoCantoSuperiorEsquerdo = Vector2.Distance(new Vector2(x, y), new Vector2(raio, altura - raio)) <= raio;
                bool dentroDoCantoSuperiorDireito = Vector2.Distance(new Vector2(x, y), new Vector2(largura - raio, altura - raio)) <= raio;
                bool dentroDoCantoInferiorEsquerdo = Vector2.Distance(new Vector2(x, y), new Vector2(raio, raio)) <= raio;
                bool dentroDoCantoInferiorDireito = Vector2.Distance(new Vector2(x, y), new Vector2(largura - raio, raio)) <= raio;

                if (dentroDoRetangulo ||
                    dentroDoCantoSuperiorEsquerdo || dentroDoCantoSuperiorDireito ||
                    dentroDoCantoInferiorEsquerdo || dentroDoCantoInferiorDireito) {
                    tex.SetPixel(x, y, cor);
                } else {
                    tex.SetPixel(x, y, transparente);
                }
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }
}
