using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ficha do jogador (abre ao tocar no cartão dele no HUD). Montada em código com o TemaUI:
/// cabeçalho na cor do jogador, números principais, progresso de carreira/experiência/educação,
/// finanças, itens no bolso e pontuação atual. Mantém a API usada pelo PlayerTracker.
/// </summary>
public class PlayerDetailMenu : MonoBehaviour
{
    const float Largura = 680f;
    const float Altura = 820f;
    const float Margem = 28f;

    private PlayerState currentPlayer;
    private bool isVisible = false;
    private GameObject overlayObj;
    private RectTransform cardRT;

    static readonly string[] NomesEducacao = { "Sem formação", "Ensino Médio", "Ensino Superior", "Pós-graduação" };

    // =================== API PÚBLICA ===================
    public void ShowPlayerDetails(PlayerState player)
    {
        currentPlayer = player;
        StopAllCoroutines();
        Construir();
        isVisible = true;
        StartCoroutine(AnimarAbertura());
    }

    public void CloseMenu()
    {
        if (overlayObj == null) return;
        StopAllCoroutines();
        StartCoroutine(AnimarFechamento());
    }

    public bool IsVisible() => isVisible;

    public void RefreshDisplay()
    {
        if (isVisible && currentPlayer != null) Construir();
    }

    void OnDisable() => StopAllCoroutines();

    // =================== CONSTRUÇÃO ===================
    private void Construir()
    {
        if (overlayObj != null) { Destroy(overlayObj); overlayObj = null; cardRT = null; }
        Canvas canvas = TemaUI.CanvasPrincipal();
        if (canvas == null || currentPlayer == null) return;
        var p = currentPlayer;

        // Véu escuro: tocar fora fecha
        var veu = TemaUI.Criar("PlayerCardOverlay", canvas.transform);
        TemaUI.Esticar(veu);
        veu.SetAsLastSibling();
        overlayObj = veu.gameObject;
        // canvas próprio por cima do HUD e dos diálogos (abaixo da pausa/Como Jogar)
        var cv = overlayObj.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = 400;
        overlayObj.AddComponent<GraphicRaycaster>();
        var veuImg = overlayObj.AddComponent<Image>();
        veuImg.color = TemaUI.Veu;
        overlayObj.AddComponent<Button>().onClick.AddListener(CloseMenu);

        // Cartão
        var card = TemaUI.Painel_(veu, "PlayerCard", TemaUI.Janela);
        cardRT = card.rectTransform;
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(Largura, Altura);
        var sombra = TemaUI.SombraPura(card.gameObject);
        sombra.effectColor = new Color(0, 0, 0, 0.45f);
        sombra.effectDistance = new Vector2(0, -10);
        card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // absorve o toque

        Color corJog = p.charColor();
        int numero = NumeroDoJogador(p);

        // ── Cabeçalho na cor do jogador ──
        float y = 0;
        var cab = TemaUI.Painel_(card.transform, "Cabecalho", Color.Lerp(corJog, TemaUI.Janela, 0.55f));
        Colocar(cab.rectTransform, 0, y, Largura, 132);
        var faixa = TemaUI.Painel_(cab.transform, "Faixa", corJog);
        Colocar(faixa.rectTransform, 0, 126, Largura, 6, false);

        var avatar = TemaUI.Criar("Avatar", cab.transform);
        Colocar(avatar, Margem, 22, 88, 88);
        var avImg = avatar.gameObject.AddComponent<Image>();
        avImg.sprite = TemaUI.Circulo; avImg.color = corJog;
        var avContorno = avatar.gameObject.AddComponent<Outline>();
        avContorno.effectColor = TemaUI.TextoClaro; avContorno.effectDistance = new Vector2(3, -3);
        var avTxt = TemaUI.Texto(avatar, numero > 0 ? "J" + numero : "?", 36, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(avTxt.rectTransform);
        TemaUI.SombraTexto(avTxt, 2f, 0.5f);

        var nome = TemaUI.Texto(cab.transform, p.charName().ToUpper(), 40, TemaUI.TextoClaro, FontStyle.Bold);
        Colocar(nome.rectTransform, Margem + 106, 20, 360, 50);
        nome.horizontalOverflow = HorizontalWrapMode.Overflow;
        TemaUI.SombraTexto(nome, 2f, 0.5f);
        var cargo = TemaUI.Texto(cab.transform, $"{p.GetProfissao()} · {p.GetCareerTitle()}", 24, TemaUI.TextoClaro);
        Colocar(cargo.rectTransform, Margem + 106, 70, 380, 36);
        cargo.color = new Color(1, 1, 1, 0.85f);

        // Colocação e tipo de controle (canto direito)
        var lugar = TemaUI.Painel_(cab.transform, "Lugar", TemaUI.Ouro);
        Colocar(lugar.rectTransform, Largura - Margem - 100, 22, 100, 52);
        var lugarTxt = TemaUI.Texto(lugar.transform, $"{p.getPlacing()}º", 32, TemaUI.OuroEscuro, FontStyle.Bold, TextAnchor.MiddleCenter);
        TemaUI.Esticar(lugarTxt.rectTransform);
        var ctrl = TemaUI.Texto(cab.transform, RotuloControle(p), 18, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter);
        Colocar(ctrl.rectTransform, Largura - Margem - 100, 80, 100, 28);
        ctrl.color = new Color(1, 1, 1, 0.8f);

        // ── Números principais ──
        y = 152;
        float tileL = (Largura - 2 * Margem - 2 * 14) / 3f;
        Tile(card.transform, Margem, y, tileL, "coin", "Moedas", p.getCoins().ToString(), TemaUI.Ouro);
        Tile(card.transform, Margem + tileL + 14, y, tileL, "star", "Estrelas", p.getStars().ToString(), TemaUI.Ouro);
        int patr = p.PatrimonioLiquido;
        Tile(card.transform, Margem + 2 * (tileL + 14), y, tileL, null, "Patrimônio", patr.ToString(), patr < 0 ? TemaUI.Vermelho : TemaUI.Verde);

        // ── Carreira ──
        y = 274;
        int nivel = p.GetCareerLevel();
        int meta = MetaCarreira();
        Rotulo(card.transform, "CARREIRA", y);
        var metaTxt = TemaUI.Texto(card.transform, $"Meta: Nv. {meta} · {PlayerState.ObterTituloCarreira(meta)}", 20, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleRight);
        Colocar(metaTxt.rectTransform, Largura / 2f, y, Largura / 2f - Margem, 28);
        y += 34;
        float segL = (Largura - 2 * Margem - 9 * 6) / 10f;
        for (int i = 1; i <= 10; i++) {
            bool cheio = i <= nivel;
            var seg = TemaUI.Painel_(card.transform, "Nivel" + i, cheio ? TemaUI.Ouro : TemaUI.PainelSuave);
            Colocar(seg.rectTransform, Margem + (i - 1) * (segL + 6), y, segL, 30);
            if (i == meta) {
                var c = seg.gameObject.AddComponent<Outline>();
                c.effectColor = TemaUI.Verde; c.effectDistance = new Vector2(3, -3);
            }
            var n = TemaUI.Texto(seg.transform, i.ToString(), 18, cheio ? TemaUI.OuroEscuro : TemaUI.TextoSuave, FontStyle.Bold, TextAnchor.MiddleCenter);
            TemaUI.Esticar(n.rectTransform);
        }
        y += 44;
        int casas = p.CasasDesdeUltimaExperiencia, total = PlayerState.CasasPorPromocaoExperiencia;
        var expTxt = TemaUI.Texto(card.transform, $"Experiência: <b>{casas}/{total}</b> casas até a próxima promoção", 20, TemaUI.TextoClaro);
        Colocar(expTxt.rectTransform, Margem, y, Largura - 2 * Margem, 28);
        y += 32;
        Barra(card.transform, Margem, y, Largura - 2 * Margem, 10, casas / (float)total, TemaUI.Azul);

        // ── Educação ──
        y = 430;
        int edu = Mathf.Clamp(p.GetEducationLevel(), 0, 3);
        Rotulo(card.transform, "EDUCAÇÃO", y);
        float pipL = 46f;
        for (int i = 1; i <= 3; i++) {
            var pip = TemaUI.Painel_(card.transform, "Edu" + i, i <= edu ? TemaUI.Verde : TemaUI.PainelSuave);
            Colocar(pip.rectTransform, Margem + 150 + (i - 1) * (pipL + 8), y + 6, pipL, 18);
        }
        var eduTxt = TemaUI.Texto(card.transform, NomesEducacao[edu], 22, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleRight);
        Colocar(eduTxt.rectTransform, Largura / 2f, y, Largura / 2f - Margem, 30);

        // ── Finanças ──
        y = 482;
        Rotulo(card.transform, "FINANÇAS", y);
        y += 34;
        var fin = TemaUI.Painel_(card.transform, "Financas", TemaUI.PainelSuave);
        float finH = 148;
        Colocar(fin.rectTransform, Margem, y, Largura - 2 * Margem, finH);
        float colL = (Largura - 2 * Margem) / 2f;
        int sal = p.GetSalary(), desp = p.GetDespesaAtual(), liq = sal - desp;
        LinhaValor(fin.transform, 0, 12, colL, "Salário", $"+{sal}", TemaUI.Verde);
        LinhaValor(fin.transform, 0, 44, colL, "Despesas", $"-{desp}", TemaUI.Vermelho);
        LinhaValor(fin.transform, 0, 82, colL, "<b>Líquido por turno</b>", (liq >= 0 ? "+" : "-") + Mathf.Abs(liq), liq >= 0 ? TemaUI.Ouro : TemaUI.Vermelho, 24);
        var div = TemaUI.Painel_(fin.transform, "Divisor", new Color(1, 1, 1, 0.12f));
        Colocar(div.rectTransform, colL - 1, 14, 2, finH - 28);
        LinhaValor(fin.transform, colL, 12, colL, "Poupança", p.GetPoupanca().ToString(), TemaUI.TextoClaro);
        LinhaValor(fin.transform, colL, 44, colL, "Reserva", p.GetReserva().ToString(), TemaUI.TextoClaro);
        LinhaValor(fin.transform, colL, 76, colL, "Ações", p.GetAcoes().ToString(), TemaUI.TextoClaro);
        LinhaValor(fin.transform, colL, 108, colL, "Dívida", p.GetDivida() > 0 ? p.GetDivida().ToString() : "—", p.GetDivida() > 0 ? TemaUI.Vermelho : TemaUI.TextoSuave);

        // ── Itens ──
        y = 684;
        Rotulo(card.transform, "ITENS", y);
        var itens = p.getItems();
        float slotX = Margem + 110;
        for (int i = 0; i < 3; i++) {
            var slot = TemaUI.Painel_(card.transform, "Slot" + i, TemaUI.PainelSuave);
            Colocar(slot.rectTransform, slotX + i * 72, y - 10, 60, 60);
            if (itens != null && i < itens.Count) {
                var tex = Resources.Load<Texture2D>("texture/icons/" + itens[i]);
                if (tex != null) {
                    var ic = TemaUI.Criar("Icone", slot.transform);
                    TemaUI.Esticar(ic, 7f);
                    ic.gameObject.AddComponent<RawImage>().texture = tex;
                }
            }
        }
        string nomes = NomesItens(itens);
        var itTxt = TemaUI.Texto(card.transform, nomes, 18, TemaUI.TextoSuave, FontStyle.Normal, TextAnchor.MiddleRight);
        Colocar(itTxt.rectTransform, slotX + 3 * 72, y - 10, Largura - Margem - (slotX + 3 * 72), 60);
        itTxt.lineSpacing = 0.9f;

        // ── Rodapé: pontuação e fechar ──
        var pontos = TemaUI.Texto(card.transform, $"Pontuação atual: <b>{p.GetFinalScore()}</b>", 24, TemaUI.Ouro);
        Colocar(pontos.rectTransform, Margem, Altura - 74, 360, 50);
        var fechar = TemaUI.Botao(card.transform, "Fechar", TemaUI.EstiloBotao.Destaque, CloseMenu, 26);
        Colocar((RectTransform)fechar.transform, Largura - Margem - 170, Altura - 80, 170, 56);
    }

    // =================== PEÇAS ===================
    private void Tile(Transform pai, float x, float y, float l, string icone, string rotulo, string valor, Color corValor)
    {
        var t = TemaUI.Painel_(pai, "Tile_" + rotulo, TemaUI.PainelSuave);
        Colocar(t.rectTransform, x, y, l, 102);
        float xTexto = 16;
        if (icone != null) {
            var tex = Resources.Load<Texture2D>("texture/icons/" + icone);
            if (tex != null) {
                var ic = TemaUI.Criar("Icone", t.transform);
                Colocar(ic, 14, 30, 44, 44);
                ic.gameObject.AddComponent<RawImage>().texture = tex;
                xTexto = 66;
            }
        }
        var v = TemaUI.Texto(t.transform, valor, 40, corValor, FontStyle.Bold);
        Colocar(v.rectTransform, xTexto, 14, l - xTexto - 10, 50);
        v.resizeTextForBestFit = true; v.resizeTextMinSize = 22; v.resizeTextMaxSize = 40;
        var r = TemaUI.Texto(t.transform, rotulo, 18, TemaUI.TextoSuave);
        Colocar(r.rectTransform, xTexto, 64, l - xTexto - 10, 26);
    }

    private void Rotulo(Transform pai, string texto, float y)
    {
        var t = TemaUI.Texto(pai, texto, 20, TemaUI.Ouro, FontStyle.Bold);
        Colocar(t.rectTransform, Margem, y, 260, 28);
    }

    private void LinhaValor(Transform pai, float x, float y, float l, string rotulo, string valor, Color corValor, int tam = 20)
    {
        var r = TemaUI.Texto(pai, rotulo, tam, TemaUI.TextoClaro);
        Colocar(r.rectTransform, x + 18, y, l * 0.62f, 30);
        var v = TemaUI.Texto(pai, valor, tam, corValor, FontStyle.Bold, TextAnchor.MiddleRight);
        Colocar(v.rectTransform, x + l * 0.5f, y, l * 0.5f - 18, 30);
    }

    private void Barra(Transform pai, float x, float y, float l, float h, float fracao, Color cor)
    {
        var fundo = TemaUI.Painel_(pai, "Barra", TemaUI.PainelSuave);
        Colocar(fundo.rectTransform, x, y, l, h);
        if (fracao <= 0f) return;
        var cheio = TemaUI.Painel_(fundo.transform, "Cheio", cor);
        Colocar(cheio.rectTransform, 0, 0, Mathf.Max(h, l * Mathf.Clamp01(fracao)), h);
    }

    // Posiciona pelo canto superior esquerdo do pai, em pixels de referência.
    private static void Colocar(RectTransform rt, float x, float y, float l, float a, bool topo = true)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(l, a);
    }

    // =================== DADOS ===================
    private static int NumeroDoJogador(PlayerState p)
    {
        var bm = FindFirstObjectByType<BoardManager>();
        if (bm == null) return 0;
        if (bm.p1 != null && bm.p1.state == p) return 1;
        if (bm.p2 != null && bm.p2.state == p) return 2;
        if (bm.p3 != null && bm.p3.state == p) return 3;
        if (bm.p4 != null && bm.p4.state == p) return 4;
        return 0;
    }

    private static int MetaCarreira()
    {
        var bm = FindFirstObjectByType<BoardManager>();
        return bm != null ? bm.metaCarreira : 10;
    }

    private static string RotuloControle(PlayerState p)
    {
        switch (p.getController()) {
            case 0: return "LOCAL";
            case 5: return "ONLINE";
            default: return "BOT";
        }
    }

    private static string NomesItens(List<BoardItem> itens)
    {
        if (itens == null || itens.Count == 0) return "Bolso vazio";
        var nomes = new List<string>();
        foreach (var it in itens) {
            int i = (int)it;
            nomes.Add(i >= 0 && i < ItemSpace.itemNames.Count ? ItemSpace.itemNames[i] : it.ToString());
        }
        return string.Join("\n", nomes);
    }

    // =================== ANIMAÇÕES ===================
    private IEnumerator AnimarAbertura()
    {
        if (cardRT == null) yield break;
        cardRT.localScale = Vector3.one * 0.85f;
        var grupo = cardRT.gameObject.AddComponent<CanvasGroup>();
        float t = 0f, dur = 0.22f;
        while (t < dur) {
            if (cardRT == null) yield break;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float ease = 1f + 1.7f * Mathf.Pow(k - 1f, 3f) + 0.7f * Mathf.Pow(k - 1f, 2f);
            cardRT.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, ease);
            grupo.alpha = k;
            yield return null;
        }
        if (cardRT != null) { cardRT.localScale = Vector3.one; grupo.alpha = 1f; }
    }

    private IEnumerator AnimarFechamento()
    {
        if (cardRT != null) {
            if (!cardRT.TryGetComponent<CanvasGroup>(out var grupo)) grupo = cardRT.gameObject.AddComponent<CanvasGroup>();
            float t = 0f, dur = 0.14f;
            while (t < dur && cardRT != null) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                cardRT.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, k);
                grupo.alpha = 1f - k;
                yield return null;
            }
        }
        isVisible = false;
        if (overlayObj != null) Destroy(overlayObj);
        overlayObj = null;
        cardRT = null;
    }
}
