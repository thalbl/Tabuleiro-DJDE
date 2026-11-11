using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerTracker : MonoBehaviour {
    private PlayerState tracking;
    private BoardManager game;

    private Text coinText;
    private Text starText;
    private Text placingText;
    private Text placingColor;
    private RawImage avatarFrame;
    private RawImage avatarPic;
    private RawImage itemImg1;
    private RawImage itemImg2;
    private RawImage itemImg3;
    private RawImage shroomStamp;
    private Image backPanel;

    // Header com Tag (VOCÊ, BOT, ONLINE) e Nome do Jogador
    private GameObject headerObj;
    private Text headerNameText;
    private Text headerTagText;
    private Image headerTagBg;
    private Outline headerOutline;
    private static readonly Dictionary<int, Sprite> spriteCache = new Dictionary<int, Sprite>();

    public List<Texture2D> items;
    public List<Texture2D> stamps;

    // Referência para o menu de detalhes
    private PlayerDetailMenu detailMenu;

    // Feedback de mudanças (Bloco E): o HUD percebe as variações e anima; não mexe em regra nenhuma
    private bool baseDefinida;
    private int ultimasMoedas, ultimasEstrelas, ultimaCarreira, ultimaEducacao;
    private float moedasExibidas;

    public void SetPlayer(PlayerState ps) {
        this.tracking = ps;
        baseDefinida = false;
        AtualizarHeaderJogador();
    }

    /// <summary>Compara com o quadro anterior e dispara números flutuantes, pulos e sons.</summary>
    private void DetectarMudancas() {
        int moedas = tracking.getCoins(), estrelas = tracking.getStars();
        int carreira = tracking.GetCareerLevel(), educacao = tracking.GetEducationLevel();
        if (!baseDefinida) {
            baseDefinida = true;
            ultimasMoedas = moedas; ultimasEstrelas = estrelas; ultimaCarreira = carreira; ultimaEducacao = educacao;
            moedasExibidas = moedas;
            return;
        }

        int dMoedas = moedas - ultimasMoedas;
        if (dMoedas != 0) {
            AvisosTela.Flutuar(coinText.rectTransform, (dMoedas > 0 ? "+" : "−") + Mathf.Abs(dMoedas),
                dMoedas > 0 ? TemaUI.Verde : TemaUI.Vermelho, 34, new Vector2(95, -6));
            AvisosTela.Pulo(coinText.transform, dMoedas > 0 ? 0.22f : 0.15f);
        }
        if (estrelas > ultimasEstrelas) {
            AvisosTela.Flutuar(starText.rectTransform, "+" + (estrelas - ultimasEstrelas) + " ★", TemaUI.Ouro, 36, new Vector2(95, -6));
            AvisosTela.Pulo(starText.transform, 0.3f);
            Sons.Tocar(Sons.Efeito.Estrela);
        } else if (estrelas < ultimasEstrelas) {
            AvisosTela.Flutuar(starText.rectTransform, "−" + (ultimasEstrelas - estrelas) + " ★", TemaUI.Vermelho, 34, new Vector2(95, -6));
        }
        if (carreira > ultimaCarreira) {
            AvisosTela.Flutuar((RectTransform)transform, "Promoção!", TemaUI.Ouro, 38, new Vector2(0, -150));
            Sons.Tocar(Sons.Efeito.Promocao);
        }
        if (educacao > ultimaEducacao) {
            AvisosTela.Flutuar((RectTransform)transform, "Formatura!", TemaUI.Hex("#C084FC"), 38, new Vector2(0, -150));
            Sons.Tocar(Sons.Efeito.Formatura);
        }
        ultimasMoedas = moedas; ultimasEstrelas = estrelas; ultimaCarreira = carreira; ultimaEducacao = educacao;
    }

    void Start() {
        game = FindObjectOfType<BoardManager>();
        coinText = transform.Find("Coins").transform.Find("CoinCount").gameObject.GetComponent<Text>();
        starText = transform.Find("Stars").transform.Find("StarCount").gameObject.GetComponent<Text>();
        placingText = transform.Find("Placing").gameObject.GetComponent<Text>();
        placingColor = placingText.gameObject.transform.Find("Drop").gameObject.GetComponent<Text>();
        avatarFrame = transform.Find("Avatar").gameObject.GetComponent<RawImage>();
        avatarPic = avatarFrame.gameObject.transform.Find("Picture").gameObject.GetComponent<RawImage>();
        itemImg1 = transform.Find("Items").transform.Find("Item (1)").gameObject.GetComponent<RawImage>();
        itemImg2 = transform.Find("Items").transform.Find("Item (2)").gameObject.GetComponent<RawImage>();
        itemImg3 = transform.Find("Items").transform.Find("Item (3)").gameObject.GetComponent<RawImage>();
        shroomStamp = transform.Find("Movement").gameObject.GetComponent<RawImage>();
        backPanel = this.GetComponent<Image>();

        // Configurar clique para abrir menu de detalhes
        SetupClickable();

        // Criar Header com Tag e Nome do Jogador
        CriarHeaderJogador();
        AplicarTema();

        // Tipografia moderna Fredoka
        Font fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (coinText != null) {
            if (fonteFredoka != null) coinText.font = fonteFredoka;
            coinText.fontSize = 38;
            coinText.fontStyle = FontStyle.Bold;
            coinText.horizontalOverflow = HorizontalWrapMode.Overflow;
            coinText.verticalOverflow = VerticalWrapMode.Overflow;
        }
        if (starText != null) {
            if (fonteFredoka != null) starText.font = fonteFredoka;
            starText.fontSize = 38;
            starText.fontStyle = FontStyle.Bold;
            starText.horizontalOverflow = HorizontalWrapMode.Overflow;
            starText.verticalOverflow = VerticalWrapMode.Overflow;
        }
        if (placingText != null) {
            if (fonteFredoka != null) placingText.font = fonteFredoka;
            placingText.fontSize = 36;
            placingText.fontStyle = FontStyle.Bold;
            placingText.horizontalOverflow = HorizontalWrapMode.Overflow;
            placingText.verticalOverflow = VerticalWrapMode.Overflow;
        }
        if (placingColor != null) {
            if (fonteFredoka != null) placingColor.font = fonteFredoka;
            placingColor.fontSize = 36;
            placingColor.fontStyle = FontStyle.Bold;
            placingColor.horizontalOverflow = HorizontalWrapMode.Overflow;
            placingColor.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private Text inicialAvatar;

    /// <summary>Painel no padrão do TemaUI: fundo verde-escuro, números claros com sombra e avatar com a inicial.</summary>
    private void AplicarTema() {
        if (backPanel != null) {
            TemaUI.EstilizarImagem(backPanel, TemaUI.Painel);
            foreach (var o in backPanel.GetComponents<Outline>()) o.enabled = false;
        }
        // Colocação desenhada por cima do avatar
        if (placingText != null) placingText.transform.SetAsLastSibling();
        foreach (Text t in new[] { coinText, starText }) {
            if (t == null) continue;
            t.color = TemaUI.TextoClaro;
            foreach (var o in t.GetComponents<Outline>()) o.enabled = false;
            TemaUI.SombraTexto(t, 2f, 0.7f);
        }
        if (placingText != null) placingText.color = new Color(0, 0, 0, 0.55f); // sombra do texto colorido "Drop"

        // Avatar: círculo na cor do jogador com a inicial do nome (antes era um quadrado preto sem imagem)
        if (avatarFrame != null) avatarFrame.texture = TemaUI.Circulo.texture;
        if (avatarPic != null) {
            avatarPic.texture = TemaUI.Circulo.texture;
            avatarPic.rectTransform.localRotation = Quaternion.identity;
            var t = TemaUI.Texto(avatarPic.transform, "", 40, TemaUI.TextoClaro, FontStyle.Bold, TextAnchor.MiddleCenter);
            TemaUI.Esticar(t.rectTransform);
            t.transform.rotation = Quaternion.identity;
            TemaUI.ContornoTexto(t, new Color(0, 0, 0, 0.45f), 2f);
            inicialAvatar = t;
        }
    }

    private void AtualizarAvatar() {
        if (tracking == null || avatarPic == null) return;
        Color cor = tracking.charColor();
        avatarPic.color = Color.Lerp(cor, Color.black, 0.15f);
        if (avatarFrame != null) avatarFrame.color = Color.Lerp(cor, Color.white, 0.35f);
        if (inicialAvatar != null) {
            string nome = tracking.charName();
            inicialAvatar.text = Iniciais(nome);
            inicialAvatar.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>"Maria Silva" -> "MS"; "Jogador 1" -> "J1"; "Ana" -> "A".</summary>
    private static string Iniciais(string nome) {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        string[] partes = nome.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        string r = partes[0].Substring(0, 1).ToUpper();
        if (partes.Length > 1) r += partes[partes.Length - 1].Substring(0, 1).ToUpper();
        return r;
    }

    private void CriarHeaderJogador() {
        if (headerObj != null) return;

        Font fonteFredoka = Resources.Load<Font>("font/Fredoka-VariableFont_wdth,wght");
        if (fonteFredoka == null) fonteFredoka = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        headerObj = new GameObject("PlayerHeader");
        headerObj.transform.SetParent(this.transform, false);

        RectTransform rt = headerObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(215, 34);
        rt.anchoredPosition = new Vector2(0, 4f);

        // Fundo do Header Dark Slate
        Image bg = headerObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.12f, 0.08f, 0.94f);
        bg.sprite = ObterSpriteArredondado(8);
        bg.type = Image.Type.Sliced;

        headerOutline = headerObj.AddComponent<Outline>();
        headerOutline.effectDistance = new Vector2(1, -1);
        headerOutline.effectColor = new Color(0.29f, 0.87f, 0.50f, 0.4f);

        // Tag Container (Pílula à esquerda)
        GameObject tagObj = new GameObject("TagPill");
        tagObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtTag = tagObj.AddComponent<RectTransform>();
        rtTag.anchorMin = new Vector2(0, 0.5f);
        rtTag.anchorMax = new Vector2(0, 0.5f);
        rtTag.pivot = new Vector2(0, 0.5f);
        rtTag.sizeDelta = new Vector2(74, 26);
        rtTag.anchoredPosition = new Vector2(4, 0);

        headerTagBg = tagObj.AddComponent<Image>();
        headerTagBg.sprite = ObterSpriteArredondado(6);
        headerTagBg.type = Image.Type.Sliced;

        GameObject tagTxtObj = new GameObject("TagText");
        tagTxtObj.transform.SetParent(tagObj.transform, false);
        RectTransform rtTagTxt = tagTxtObj.AddComponent<RectTransform>();
        rtTagTxt.anchorMin = Vector2.zero;
        rtTagTxt.anchorMax = Vector2.one;
        rtTagTxt.sizeDelta = Vector2.zero;

        headerTagText = tagTxtObj.AddComponent<Text>();
        headerTagText.font = fonteFredoka;
        headerTagText.fontSize = 16;
        headerTagText.fontStyle = FontStyle.Bold;
        headerTagText.resizeTextForBestFit = true;
        headerTagText.resizeTextMinSize = 12;
        headerTagText.resizeTextMaxSize = 17;
        headerTagText.alignment = TextAnchor.MiddleCenter;
        headerTagText.color = Color.white;
        headerTagText.raycastTarget = false;

        // Nome do Jogador (à direita da tag)
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(headerObj.transform, false);
        RectTransform rtName = nameObj.AddComponent<RectTransform>();
        rtName.anchorMin = new Vector2(0, 0);
        rtName.anchorMax = new Vector2(1, 1);
        rtName.offsetMin = new Vector2(82, 0);
        rtName.offsetMax = new Vector2(-6, 0);

        headerNameText = nameObj.AddComponent<Text>();
        headerNameText.font = fonteFredoka;
        headerNameText.fontSize = 18;
        headerNameText.fontStyle = FontStyle.Bold;
        headerNameText.resizeTextForBestFit = true;
        headerNameText.resizeTextMinSize = 13;
        headerNameText.resizeTextMaxSize = 19;
        headerNameText.alignment = TextAnchor.MiddleLeft;
        headerNameText.color = Color.white;
        headerNameText.raycastTarget = false;
        headerNameText.horizontalOverflow = HorizontalWrapMode.Overflow;

        Shadow sh = nameObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.8f);
        sh.effectDistance = new Vector2(1, -1);

        AtualizarHeaderJogador();
    }

    public void AtualizarHeaderJogador() {
        if (tracking == null || headerObj == null) return;

        string nome = tracking.charName();
        if (headerNameText != null) {
            headerNameText.text = nome;
        }

        int controller = tracking.getController();
        string tagTexto = "BOT";
        Color tagBgColor = new Color(0.20f, 0.28f, 0.36f, 1f); // Slate Blue
        Color outlineColor = new Color(0.35f, 0.45f, 0.55f, 0.5f);

        if (controller == 0) {
            // Humano Local
            tagTexto = GameMatchConfig.IsOnlineMatch ? "VOCÊ" : "LOCAL";
            tagBgColor = TemaUI.Hex("#15803D"); // verde do tema
            outlineColor = new Color(0.29f, 0.87f, 0.50f, 0.6f);
        } else if (controller == 5) {
            // Remoto Online
            tagTexto = "ONLINE";
            tagBgColor = new Color(0.49f, 0.15f, 0.80f, 1f); // Roxo
            outlineColor = new Color(0.68f, 0.35f, 0.95f, 0.6f);
        } else {
            // Bot / CPU
            tagTexto = "BOT";
            tagBgColor = TemaUI.Hex("#1E3A5F"); // azul-escuro
            outlineColor = new Color(0.40f, 0.48f, 0.58f, 0.5f);
        }

        if (headerTagText != null) headerTagText.text = tagTexto;
        if (headerTagBg != null) headerTagBg.color = tagBgColor;
        if (headerOutline != null) headerOutline.effectColor = outlineColor;
    }

    void Update() {
        if (tracking == null) return;

        if (headerObj == null) {
            CriarHeaderJogador();
        }

        DetectarMudancas();
        // O número "corre" até o valor novo em vez de pular
        int alvo = tracking.getCoins();
        float velocidade = Mathf.Max(30f, Mathf.Abs(alvo - moedasExibidas) * 3.5f);
        moedasExibidas = Mathf.MoveTowards(moedasExibidas, alvo, velocidade * Time.unscaledDeltaTime);
        int mostrar = Mathf.RoundToInt(moedasExibidas);
        coinText.text = (mostrar < 10 && mostrar >= 0 ? "0" : "") + mostrar;
        starText.text = (tracking.getStars() < 10 ? "0" : "") + tracking.getStars();

        if (backPanel != null) {
            // Destaque da vez: fundo mais claro (contorno sobre painel translúcido "mancha" a cor)
            bool vez = NetworkTurnManager.Instance != null && NetworkTurnManager.Instance.IsTurnoAtivo
                       && NetworkTurnManager.Instance.JogadorAtivoPlayer != null
                       && NetworkTurnManager.Instance.JogadorAtivoPlayer.state == tracking;
            backPanel.color = vez ? TemaUI.Hex("#2F7A55", 0.94f) : TemaUI.Painel;
        }
        AtualizarAvatar();

        switch (tracking.getPlacing()) {
            case 1:
                placingText.text = "1º";
                placingColor.text = "1º";
                placingColor.color = TemaUI.Ouro;
                break;
            case 2:
                placingText.text = "2º";
                placingColor.text = "2º";
                placingColor.color = new Color(0.85f, 0.88f, 0.92f, 1f);
                break;
            case 3:
                placingText.text = "3º";
                placingColor.text = "3º";
                placingColor.color = new Color(0.85f, 0.55f, 0.25f, 1f);
                break;
            default:
                placingText.text = "4º";
                placingColor.text = "4º";
                placingColor.color = TemaUI.TextoSuave;
                break;
        }

        if (tracking.getItems().Count >= 1) {
            itemImg1.texture = items[(int) tracking.getItems()[0]];
            itemImg1.gameObject.SetActive(true);
        } else {
            itemImg1.gameObject.SetActive(false);
        }
        if (tracking.getItems().Count >= 2) {
            itemImg2.texture = items[(int) tracking.getItems()[1]];
            itemImg2.gameObject.SetActive(true);
        } else {
            itemImg2.gameObject.SetActive(false);
        }
        if (tracking.getItems().Count >= 3) {
            itemImg3.texture = items[(int) tracking.getItems()[2]];
            itemImg3.gameObject.SetActive(true);
        } else {
            itemImg3.gameObject.SetActive(false);
        }
        shroomStamp.texture = stamps[tracking.getMovement()];
    }

    private void SetupClickable() {
        Button button = GetComponent<Button>();
        if (button == null) {
            button = gameObject.AddComponent<Button>();
        }
        
        button.onClick.AddListener(OnPlayerTrackerClicked);
        detailMenu = FindObjectOfType<PlayerDetailMenu>();
    }

    public void OnPlayerTrackerClicked() {
        if (tracking != null && detailMenu != null) {
            detailMenu.ShowPlayerDetails(tracking);
        }
    }

    public void SetDetailMenu(PlayerDetailMenu menu) {
        detailMenu = menu;
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
