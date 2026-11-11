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

        // Dados de todos antes de montar (as barras usam a mesma escala para comparar)
        var dados = new System.Collections.Generic.List<Dados>();
        for (int pos = 1; pos <= jogadores; pos++) dados.Add(Ler(pos));
        GarantirCoresDistintas(dados);
        int maiorTotal = 1;
        foreach (var d in dados) maiorTotal = Mathf.Max(maiorTotal, d.PontosEstrelas + d.PontosPatrimonio + d.PontosCarreira + d.PontosEducacao);

        // Lista (esquerda)
        var lista = TemaUI.Criar("Lista", fundo.transform);
        lista.anchorMin = lista.anchorMax = lista.pivot = new Vector2(0f, 1f);
        lista.sizeDelta = new Vector2(1080, AlturaLista);
        lista.anchoredPosition = new Vector2(50, -250);
        var v = lista.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        v.spacing = 14; v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        foreach (var d in dados) Linha(lista, d, maiorTotal);

        // Gráfico (direita)
        Grafico(fundo.transform, dados);

        // Botão
        var voltar = TemaUI.Botao(fundo.transform, "Voltar ao Menu", TemaUI.EstiloBotao.Destaque, ReturnToMainMenu, 34);
        Posicionar((RectTransform)voltar.transform, 0.5f, 0f, new Vector2(440, 90), new Vector2(0, 50));

        Sons.Tocar(Sons.Efeito.Estrela);
    }

    // ------------------------------------------------------------------ dados
    const float AlturaLista = 4 * 150 + 3 * 14;
    static readonly Color CorEstrelas = TemaUI.Ouro;
    static readonly Color CorPatrimonio = TemaUI.Verde;
    static readonly Color CorCarreira = TemaUI.Azul;
    static readonly Color CorEducacao = TemaUI.Hex("#C084FC");
    static readonly Color[] CoresPadrao = { TemaUI.Hex("#22C55E"), TemaUI.Hex("#F472B6"), TemaUI.Hex("#38BDF8"), TemaUI.Hex("#F59E0B") };

    class Dados {
        public int pos, estrelas, patrimonio, carreiraNivel, educacao, pontos;
        public string nome, carreira;
        public bool meta;
        public Color cor;
        public System.Collections.Generic.List<int> historico = new System.Collections.Generic.List<int>();
        public int PontosEstrelas => estrelas * 100;
        public int PontosPatrimonio => Mathf.Max(0, patrimonio);
        public int PontosCarreira => carreiraNivel * 50;
        public int PontosEducacao => educacao * 75;
    }

    /// <summary>Jogadores podem ter cores quase iguais (ex.: dois verdes): troca a repetida por uma da paleta.</summary>
    static void GarantirCoresDistintas(System.Collections.Generic.List<Dados> dados) {
        Color[] paleta = { TemaUI.Hex("#38BDF8"), TemaUI.Hex("#FACC15"), TemaUI.Hex("#A78BFA"), TemaUI.Hex("#F87171"),
                           TemaUI.Hex("#2DD4BF"), TemaUI.Hex("#F59E0B"), TemaUI.Hex("#22C55E"), TemaUI.Hex("#F472B6") };
        // Compara pelo matiz (dois verdes diferentes ainda parecem iguais numa linha fina)
        bool Perto(Color a, Color b) {
            Color.RGBToHSV(a, out float ha, out float sa, out float va);
            Color.RGBToHSV(b, out float hb, out float sb, out float vb);
            if (sa < 0.25f || sb < 0.25f) return Mathf.Abs(va - vb) < 0.25f && Mathf.Abs(sa - sb) < 0.25f; // tons de cinza
            float dh = Mathf.Abs(ha - hb);
            return Mathf.Min(dh, 1f - dh) < 0.12f;
        }
        for (int i = 0; i < dados.Count; i++) {
            bool repetida = false;
            for (int j = 0; j < i; j++) if (Perto(dados[i].cor, dados[j].cor)) { repetida = true; break; }
            if (!repetida) continue;
            foreach (var c in paleta) {
                bool livre = true;
                for (int j = 0; j < dados.Count; j++) if (j != i && Perto(c, dados[j].cor)) { livre = false; break; }
                if (livre) { dados[i].cor = c; break; }
            }
        }
    }

    static Dados Ler(int pos) {
        var d = new Dados {
            pos = pos,
            nome = PlayerPrefs.GetString($"Player{pos}Name", $"Jogador {pos}"),
            carreira = PlayerPrefs.GetString($"Player{pos}Career", "Nv. 1 - Estagiário"),
            patrimonio = PlayerPrefs.GetInt($"Player{pos}Money", 0),
            estrelas = PlayerPrefs.GetInt($"Player{pos}Stars", 0),
            educacao = Mathf.Clamp(PlayerPrefs.GetInt($"Player{pos}EducationLevel", 0), 0, NiveisEducacao.Length - 1),
            pontos = PlayerPrefs.GetInt($"Player{pos}FinalScore", 0),
            meta = PlayerPrefs.GetString($"Player{pos}ScoreBreakdown", "").StartsWith("META"),
            carreiraNivel = PlayerPrefs.GetInt($"Player{pos}CareerLevel", 1),
        };
        d.cor = ColorUtility.TryParseHtmlString(PlayerPrefs.GetString($"Player{pos}Color", ""), out Color c) ? c : CoresPadrao[(pos - 1) % 4];
        foreach (string parte in PlayerPrefs.GetString($"Player{pos}Historico", "").Split(','))
            if (int.TryParse(parte, out int valor)) d.historico.Add(valor);
        return d;
    }

    // ------------------------------------------------------------------ linha do ranking
    void Linha(Transform pai, Dados d, int maiorTotal) {
        bool primeiro = d.pos == 1;
        var cartao = TemaUI.Painel_(pai, "Jogador" + d.pos, primeiro ? TemaUI.Hex("#1F5A3F") : TemaUI.Painel);
        var le = cartao.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = le.minHeight = 150;
        if (primeiro) {
            var borda = cartao.gameObject.AddComponent<UnityEngine.UI.Outline>();
            borda.effectColor = TemaUI.Ouro; borda.effectDistance = new Vector2(3, -3);
        }

        // Medalha de posição
        Color[] medalhas = { TemaUI.Ouro, TemaUI.Hex("#CBD5E1"), TemaUI.Hex("#D9884A"), TemaUI.Hex("#3F5249") };
        var medalha = TemaUI.Painel_(cartao.transform, "Medalha", medalhas[d.pos - 1]);
        medalha.sprite = TemaUI.Circulo; medalha.type = UnityEngine.UI.Image.Type.Simple;
        Posicionar(medalha.rectTransform, 0f, 0.5f, new Vector2(84, 84), new Vector2(60, 0), new Vector2(0.5f, 0.5f));
        var tPos = TemaUI.Texto(medalha.transform, d.pos + "º", 38, d.pos <= 3 ? TemaUI.Hex("#1C1917") : TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(tPos.rectTransform);

        // Nome (com a cor do jogador) + carreira
        var marcador = TemaUI.Painel_(cartao.transform, "Cor", d.cor);
        marcador.sprite = TemaUI.Circulo; marcador.type = UnityEngine.UI.Image.Type.Simple;
        Posicionar(marcador.rectTransform, 0.11f, 0.83f, new Vector2(20, 20), Vector2.zero, new Vector2(0f, 0.5f));
        var tNome = TemaUI.Texto(cartao.transform, d.nome, 36, TemaUI.TextoClaro, FontStyle.Bold);
        Ancorar(tNome.rectTransform, 0.135f, 0.68f, 0.6f, 0.98f);
        var tCarreira = TemaUI.Texto(cartao.transform, d.carreira + "  ·  " + NiveisEducacao[d.educacao], 22, TemaUI.TextoSuave);
        Ancorar(tCarreira.rectTransform, 0.11f, 0.5f, 0.8f, 0.7f);
        if (d.meta) {
            var etiqueta = TemaUI.Painel_(cartao.transform, "Meta", TemaUI.Ouro);
            Posicionar(etiqueta.rectTransform, 0.66f, 0.83f, new Vector2(120, 36), Vector2.zero, new Vector2(0.5f, 0.5f));
            var tMeta = TemaUI.Texto(etiqueta.transform, "META", 22, TemaUI.OuroEscuro, FontStyle.Bold, TextAnchor.MiddleCenter);
            TemaUI.Esticar(tMeta.rectTransform);
        }

        // Composição da pontuação: legenda colorida + barra empilhada (mesma escala para todos)
        string legenda = $"<color=#{Hex(CorEstrelas)}>★ {d.estrelas} = {d.PontosEstrelas}</color>     " +
                         $"<color=#{Hex(CorPatrimonio)}>Patrimônio {d.patrimonio}</color>     " +
                         $"<color=#{Hex(CorCarreira)}>Carreira {d.PontosCarreira}</color>     " +
                         $"<color=#{Hex(CorEducacao)}>Educação {d.PontosEducacao}</color>";
        var tLegenda = TemaUI.Texto(cartao.transform, legenda, 22, TemaUI.TextoClaro, FontStyle.Bold);
        tLegenda.resizeTextForBestFit = true; tLegenda.resizeTextMinSize = 16; tLegenda.resizeTextMaxSize = 22;
        Ancorar(tLegenda.rectTransform, 0.11f, 0.27f, 0.8f, 0.5f);

        var trilho = TemaUI.Painel_(cartao.transform, "Barra", TemaUI.Hex("#0B1F17", 0.85f));
        Ancorar(trilho.rectTransform, 0.11f, 0.09f, 0.8f, 0.24f);
        int[] valores = { d.PontosEstrelas, d.PontosPatrimonio, d.PontosCarreira, d.PontosEducacao };
        Color[] cores = { CorEstrelas, CorPatrimonio, CorCarreira, CorEducacao };
        float x = 0f;
        for (int i = 0; i < valores.Length; i++) {
            if (valores[i] <= 0) continue;
            float w = valores[i] / (float)maiorTotal;
            var seg = TemaUI.Painel_(trilho.transform, "Segmento", cores[i]);
            seg.sprite = null;
            seg.raycastTarget = false;
            var srt = seg.rectTransform;
            srt.anchorMin = new Vector2(x, 0.18f); srt.anchorMax = new Vector2(Mathf.Min(1f, x + w), 0.82f);
            srt.offsetMin = new Vector2(x <= 0f ? 6f : 1f, 0); srt.offsetMax = new Vector2(-1f, 0);
            x += w;
        }

        var tPontos = TemaUI.Texto(cartao.transform, d.pontos + "\n<size=22>pontos</size>", 46, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleCenter);
        tPontos.lineSpacing = 0.8f;
        TemaUI.SombraTexto(tPontos);
        Ancorar(tPontos.rectTransform, 0.82f, 0.05f, 0.98f, 0.95f);
    }

    static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    // ------------------------------------------------------------------ gráfico de patrimônio
    void Grafico(Transform pai, System.Collections.Generic.List<Dados> dados) {
        var painel = TemaUI.Painel_(pai, "Grafico", TemaUI.Painel);
        var prt = painel.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
        prt.sizeDelta = new Vector2(690, AlturaLista);
        prt.anchoredPosition = new Vector2(-50, -250);

        var titulo = TemaUI.Texto(prt, "Patrimônio por rodada", 30, TemaUI.Ouro, FontStyle.Bold, TextAnchor.MiddleLeft);
        Topo(titulo.rectTransform, 28, 22, 42);

        int pontos = 0, min = 0, max = 10;
        foreach (var d in dados) {
            pontos = Mathf.Max(pontos, d.historico.Count);
            foreach (int val in d.historico) { min = Mathf.Min(min, val); max = Mathf.Max(max, val); }
        }

        if (pontos < 2) {
            var aviso = TemaUI.Texto(prt, "Sem histórico de rodadas para esta partida.", 24, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleCenter);
            TemaUI.Esticar(aviso.rectTransform, 40f);
            return;
        }

        // Área de plotagem
        const float esq = 100f, dir = 34f, baixo = 116f, cima = 92f;
        var area = TemaUI.Criar("Area", prt);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(esq, baixo); area.offsetMax = new Vector2(-dir, -cima);
        float largura = 690f - esq - dir, altura = AlturaLista - baixo - cima;

        float topoY = NumeroRedondo(max * 1.1f);
        float baseY = min < 0 ? -NumeroRedondo(-min * 1.1f) : 0f;
        System.Func<int, float, Vector2> ponto = (i, val) =>
            new Vector2(largura * i / (float)(pontos - 1), altura * (val - baseY) / (topoY - baseY));

        // Grade + eixo Y
        for (int g = 0; g <= 4; g++) {
            float val = Mathf.Lerp(baseY, topoY, g / 4f);
            bool destaque = g == 0 || Mathf.Abs(val) < 0.5f;
            var linha = TemaUI.Painel_(area, "Grade", TemaUI.Hex("#A7C4B5", destaque ? 0.35f : 0.12f));
            linha.sprite = null; linha.raycastTarget = false;
            var lrt = linha.rectTransform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = Vector2.zero;
            lrt.sizeDelta = new Vector2(largura, 2);
            lrt.anchoredPosition = new Vector2(0, altura * g / 4f);
            var rotulo = TemaUI.Texto(area, Mathf.RoundToInt(val).ToString(), 20, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleRight);
            var rrt = rotulo.rectTransform;
            rrt.anchorMin = rrt.anchorMax = Vector2.zero; rrt.pivot = new Vector2(1, 0.5f);
            rrt.sizeDelta = new Vector2(84, 30);
            rrt.anchoredPosition = new Vector2(-12, altura * g / 4f);
        }

        // Eixo X (no máximo ~6 rótulos)
        int passo = Mathf.Max(1, Mathf.CeilToInt((pontos - 1) / 5f));
        for (int i = 0; i < pontos; i += passo) RotuloX(area, ponto(i, baseY).x, i == 0 ? "Início" : i.ToString());
        if ((pontos - 1) % passo != 0) RotuloX(area, ponto(pontos - 1, baseY).x, (pontos - 1).ToString());
        var tituloX = TemaUI.Texto(prt, "rodada", 18, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleRight);
        var txr = tituloX.rectTransform;
        txr.anchorMin = txr.anchorMax = txr.pivot = new Vector2(1, 0);
        txr.sizeDelta = new Vector2(120, 24);
        txr.anchoredPosition = new Vector2(-dir, baixo - 62);

        // Uma linha por jogador; o 1º lugar fica por cima
        for (int k = dados.Count - 1; k >= 0; k--) {
            var d = dados[k];
            float espessura = d.pos == 1 ? 6f : 4f;
            for (int i = 1; i < d.historico.Count; i++)
                Segmento(area, ponto(i - 1, d.historico[i - 1]), ponto(i, d.historico[i]), d.cor, espessura);
            for (int i = 0; i < d.historico.Count; i++) {
                var bola = TemaUI.Painel_(area, "Ponto", d.cor);
                bola.sprite = TemaUI.Circulo; bola.type = UnityEngine.UI.Image.Type.Simple; bola.raycastTarget = false;
                var brt = bola.rectTransform;
                brt.anchorMin = brt.anchorMax = Vector2.zero; brt.pivot = new Vector2(0.5f, 0.5f);
                float tam = i == d.historico.Count - 1 ? 16f : 9f;
                brt.sizeDelta = new Vector2(tam, tam);
                brt.anchoredPosition = ponto(i, d.historico[i]);
            }
        }

        // Legenda
        var legenda = TemaUI.Criar("Legenda", prt);
        legenda.anchorMin = Vector2.zero; legenda.anchorMax = new Vector2(1, 0);
        legenda.pivot = new Vector2(0.5f, 0);
        legenda.offsetMin = new Vector2(24, 18); legenda.offsetMax = new Vector2(-24, 18 + 36);
        var h = legenda.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        h.spacing = 22; h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        foreach (var d in dados) {
            var item = TemaUI.Criar("Item", legenda);
            var ih = item.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            ih.spacing = 8; ih.childAlignment = TextAnchor.MiddleLeft;
            ih.childControlWidth = true; ih.childControlHeight = true; ih.childForceExpandWidth = false; ih.childForceExpandHeight = false;
            var cor = TemaUI.Painel_(item, "Cor", d.cor);
            cor.sprite = TemaUI.Circulo; cor.type = UnityEngine.UI.Image.Type.Simple;
            var cle = cor.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            cle.preferredWidth = cle.preferredHeight = 18;
            var t = TemaUI.Texto(item, d.nome, 22, TemaUI.TextoClaro, FontStyle.Bold);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var tle = t.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            tle.preferredWidth = Mathf.Min(140f, t.preferredWidth); tle.preferredHeight = 30;
        }
    }

    static void RotuloX(RectTransform area, float x, string texto) {
        var t = TemaUI.Texto(area, texto, 20, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.UpperCenter);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(90, 28);
        rt.anchoredPosition = new Vector2(x, -10);
    }

    static void Segmento(RectTransform area, Vector2 a, Vector2 b, Color cor, float espessura) {
        var img = TemaUI.Painel_(area, "Linha", cor);
        img.sprite = null; img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0, 0.5f);
        Vector2 d = b - a;
        rt.sizeDelta = new Vector2(d.magnitude, espessura);
        rt.anchoredPosition = a;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    /// <summary>Arredonda para cima num número "bonito" (1; 1,5; 2; 2,5; 3; 4; 5; 6 ou 8 × 10^n) para a escala do gráfico.</summary>
    static float NumeroRedondo(float v) {
        if (v <= 10f) return 10f;
        float p = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(v)));
        foreach (float m in new[] { 1f, 1.5f, 2f, 2.5f, 3f, 4f, 5f, 6f, 8f, 10f }) if (m * p >= v) return m * p;
        return 10f * p;
    }

    static void Topo(RectTransform rt, float margem, float topo, float altura) {
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(margem, -topo - altura); rt.offsetMax = new Vector2(-margem, -topo);
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
