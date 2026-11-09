using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema de Reações Rápidas e Emotes para os jogadores interagirem durante a partida.
/// Utiliza as texturas de ícones oficiais do jogo (Estrela, Moeda, Cogumelo, Boo, Duelo)
/// e tipografia Fredoka, garantindo compatibilidade multiplataforma perfeita sem dependência
/// de fontes de emojis Unicode de terceiros.
/// </summary>
public class EmoteReactionUI : MonoBehaviour {

    public static EmoteReactionUI Instance { get; private set; }

    [System.Serializable]
    public class ReacaoConfig {
        public string id;
        public string label;
        public string iconResourcePath;
        public Color corTema;
        public Texture2D iconTexture;
    }

    private List<ReacaoConfig> reacoes = new List<ReacaoConfig>();
    private GameObject barraEmotes;
    private Font fonteFredoka;
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

        CarregarReacoes();
        CriarBarraEmotes();

        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnPacoteRecebido += ProcessarPacoteEmote;
        }
    }

    void OnDestroy() {
        if (NetworkTransportService.Instance != null) {
            NetworkTransportService.Instance.OnPacoteRecebido -= ProcessarPacoteEmote;
        }
    }

    private void ProcessarPacoteEmote(NetworkProtocol.NetworkPacket packet) {
        if (packet != null && packet.type == NetworkProtocol.MessageType.GameEmote) {
            int meuSlot = (NetworkTransportService.Instance != null) ? NetworkTransportService.Instance.LocalSlotIndex : GameMatchConfig.LocalPlayerIndex;
            if (packet.senderSlot != meuSlot) {
                DispararReacao(packet.intPayload, packet.senderSlot, false);
            }
        }
    }

    void Update() {
        // Atalhos de teclado (1-5) para disparar reações rapidamente
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) DispararReacao(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) DispararReacao(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) DispararReacao(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) DispararReacao(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) DispararReacao(4);
    }

    private void CarregarReacoes() {
        reacoes = new List<ReacaoConfig> {
            new ReacaoConfig {
                id = "star",
                label = "GG!",
                iconResourcePath = "texture/icons/star",
                corTema = new Color(1.0f, 0.82f, 0.15f, 1f) // Dourado
            },
            new ReacaoConfig {
                id = "coin",
                label = "RICO!",
                iconResourcePath = "texture/icons/coin",
                corTema = new Color(1.0f, 0.90f, 0.25f, 1f) // Amarelo
            },
            new ReacaoConfig {
                id = "mushroom",
                label = "SORTE!",
                iconResourcePath = "texture/icons/Mushroom",
                corTema = new Color(1.0f, 0.35f, 0.35f, 1f) // Vermelho
            },
            new ReacaoConfig {
                id = "boo",
                label = "OPS!",
                iconResourcePath = "texture/icons/BooBell",
                corTema = new Color(0.75f, 0.55f, 1.0f, 1f) // Roxo
            },
            new ReacaoConfig {
                id = "duel",
                label = "DUELO!",
                iconResourcePath = "texture/icons/DuelingGlove",
                corTema = new Color(1.0f, 0.60f, 0.20f, 1f) // Laranja
            }
        };

        foreach (var r in reacoes) {
            r.iconTexture = Resources.Load<Texture2D>(r.iconResourcePath);
        }
    }

    public void CriarBarraEmotes() {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Se já existir, remove para recriar
        if (barraEmotes != null) {
            Destroy(barraEmotes);
        }

        barraEmotes = new GameObject("BarraEmotes");
        barraEmotes.transform.SetParent(canvas.transform, false);

        RectTransform rt = barraEmotes.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.04f);
        rt.anchorMax = new Vector2(0.5f, 0.04f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(330, 56);
        rt.anchoredPosition = Vector2.zero;

        // Fundo estilo pílula Dark Slate
        Image imgFundo = barraEmotes.AddComponent<Image>();
        imgFundo.color = new Color(0.06f, 0.16f, 0.11f, 0.94f);
        imgFundo.sprite = ObterSpriteArredondado(16);
        imgFundo.type = Image.Type.Sliced;

        Outline outline = barraEmotes.AddComponent<Outline>();
        outline.effectColor = new Color(0.29f, 0.87f, 0.50f, 0.5f);
        outline.effectDistance = new Vector2(1, -1);

        float spacing = 60f;
        float startX = -((reacoes.Count - 1) * spacing) / 2f;

        for (int i = 0; i < reacoes.Count; i++) {
            int idx = i;
            CriarBotaoReacao(barraEmotes.transform, reacoes[i], startX + i * spacing, () => DispararReacao(idx));
        }
    }

    private void CriarBotaoReacao(Transform parent, ReacaoConfig config, float posX, UnityEngine.Events.UnityAction onClick) {
        GameObject btnObj = new GameObject($"BtnReacao_{config.id}");
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48, 44);
        rt.anchoredPosition = new Vector2(posX, 0);

        Image imgFundo = btnObj.AddComponent<Image>();
        imgFundo.color = new Color(0.10f, 0.22f, 0.15f, 0.95f);
        imgFundo.sprite = ObterSpriteArredondado(10);
        imgFundo.type = Image.Type.Sliced;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(config.corTema.r, config.corTema.g, config.corTema.b, 0.55f);
        outline.effectDistance = new Vector2(1, -1);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        // Ícone visual da reação
        if (config.iconTexture != null) {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
            rtIcon.anchorMin = new Vector2(0.5f, 0.5f);
            rtIcon.anchorMax = new Vector2(0.5f, 0.5f);
            rtIcon.pivot = new Vector2(0.5f, 0.5f);
            rtIcon.sizeDelta = new Vector2(32, 32);
            rtIcon.anchoredPosition = Vector2.zero;

            RawImage rawImg = iconObj.AddComponent<RawImage>();
            rawImg.texture = config.iconTexture;
            rawImg.raycastTarget = false;
        } else {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform rtT = txtObj.AddComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero;
            rtT.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.text = config.label;
            txt.font = fonteFredoka;
            txt.fontSize = 12;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = config.corTema;
            txt.raycastTarget = false;
        }

        // Efeito de hover e clique
        MenuButtonEffect fx = btnObj.AddComponent<MenuButtonEffect>();
        fx.corNormal = new Color(0.10f, 0.22f, 0.15f, 0.95f);
        fx.corHover = new Color(config.corTema.r * 0.4f, config.corTema.g * 0.4f, config.corTema.b * 0.4f, 1f);
        fx.escalaHover = 1.15f;
    }

    public void DispararReacao(int index, int playerSlot = -1, bool enviarRede = true) {
        if (index < 0 || index >= reacoes.Count) return;

        int meuSlot = (NetworkTransportService.Instance != null) ? NetworkTransportService.Instance.LocalSlotIndex : GameMatchConfig.LocalPlayerIndex;
        if (playerSlot < 0) playerSlot = meuSlot;

        HapticFeedback.Vibrate();

        // Dispara o balão visual sobre o slot indicado
        StartCoroutine(AnimarBalaoReacao(playerSlot, reacoes[index]));

        // Envia pacote via transporte de rede
        if (enviarRede && GameMatchConfig.IsOnlineMatch && NetworkTransportService.Instance != null) {
            NetworkProtocol.NetworkPacket pkt = new NetworkProtocol.NetworkPacket(NetworkProtocol.MessageType.GameEmote, meuSlot);
            pkt.intPayload = index;
            NetworkTransportService.Instance.EnviarPacote(pkt);
        }
    }

    private IEnumerator AnimarBalaoReacao(int playerSlot, ReacaoConfig config) {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        float scaleFactor = canvas.scaleFactor > 0 ? canvas.scaleFactor : 1f;

        // Container do Balão
        GameObject balao = new GameObject("BalaoReacao");
        balao.transform.SetParent(canvas.transform, false);

        RectTransform rt = balao.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(164, 52);

        // Busca o tracker correspondente ao jogador (P1..P4 no Standings)
        string slotName = $"P{playerSlot + 1}";
        Transform targetTracker = null;

        GameObject trackerObj = GameObject.Find(slotName);
        if (trackerObj != null) {
            targetTracker = trackerObj.transform;
        } else {
            PlayerTracker[] trackers = FindObjectsOfType<PlayerTracker>();
            foreach (var t in trackers) {
                if (t.gameObject.name.Equals(slotName, System.StringComparison.OrdinalIgnoreCase)) {
                    targetTracker = t.transform;
                    break;
                }
            }
        }

        // Posiciona logo abaixo do tracker do jogador (ou no topo inferior caso não encontre)
        if (targetTracker != null) {
            balao.transform.position = targetTracker.position + new Vector3(0, -68f * scaleFactor, 0);
        } else {
            rt.anchorMin = new Vector2(0.5f, 0.22f);
            rt.anchorMax = new Vector2(0.5f, 0.22f);
            rt.anchoredPosition = Vector2.zero;
        }

        // Fundo do Balão (Dark Slate com acabamento premium)
        Image imgFundo = balao.AddComponent<Image>();
        imgFundo.color = new Color(0.08f, 0.18f, 0.13f, 0.98f);
        imgFundo.sprite = ObterSpriteArredondado(14);
        imgFundo.type = Image.Type.Sliced;

        Outline outline = balao.AddComponent<Outline>();
        outline.effectColor = config.corTema;
        outline.effectDistance = new Vector2(2, -2);

        // Ícone no Balão (RawImage com a textura oficial correspondente)
        RawImage iconImg = null;
        if (config.iconTexture != null) {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(balao.transform, false);
            RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
            rtIcon.anchorMin = new Vector2(0, 0.5f);
            rtIcon.anchorMax = new Vector2(0, 0.5f);
            rtIcon.pivot = new Vector2(0, 0.5f);
            rtIcon.sizeDelta = new Vector2(36, 36);
            rtIcon.anchoredPosition = new Vector2(12, 0);

            iconImg = iconObj.AddComponent<RawImage>();
            iconImg.texture = config.iconTexture;
            iconImg.raycastTarget = false;
        }

        // Texto no Balão (Fredoka Bold)
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(balao.transform, false);
        RectTransform rtT = txtObj.AddComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0, 0);
        rtT.anchorMax = new Vector2(1, 1);
        rtT.offsetMin = new Vector2(config.iconTexture != null ? 52 : 12, 2);
        rtT.offsetMax = new Vector2(-12, -2);

        Text txtLabel = txtObj.AddComponent<Text>();
        txtLabel.text = config.label;
        txtLabel.font = fonteFredoka;
        txtLabel.fontSize = 20;
        txtLabel.fontStyle = FontStyle.Bold;
        txtLabel.alignment = TextAnchor.MiddleCenter;
        txtLabel.color = Color.white;
        txtLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        txtLabel.verticalOverflow = VerticalWrapMode.Overflow;
        txtLabel.raycastTarget = false;

        Shadow sh = txtObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.85f);
        sh.effectDistance = new Vector2(1.5f, -1.5f);

        // Animação: Pop-up elástico + flutuação + fade out
        float duracao = 2.4f;
        float elapsed = 0f;
        Vector3 basePosition = balao.transform.position;
        balao.transform.localScale = Vector3.one * 0.3f;

        while (elapsed < duracao) {
            if (balao == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duracao;

            // Escala com efeito elástico no início
            if (t < 0.15f) {
                float popT = t / 0.15f;
                balao.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, popT);
            } else if (t < 0.25f) {
                float popT = (t - 0.15f) / 0.10f;
                balao.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.0f, popT);
            } else {
                balao.transform.localScale = Vector3.one;
            }

            // Flutuação suave
            balao.transform.position = basePosition + new Vector3(0, Mathf.Sin(t * Mathf.PI) * 6f * scaleFactor, 0);

            // Fade out nos últimos 25% da animação
            if (t > 0.75f) {
                float alpha = 1f - ((t - 0.75f) / 0.25f);
                imgFundo.color = new Color(0.08f, 0.18f, 0.13f, 0.98f * alpha);
                txtLabel.color = new Color(1f, 1f, 1f, alpha);
                if (iconImg != null) iconImg.color = new Color(1f, 1f, 1f, alpha);
                outline.effectColor = new Color(config.corTema.r, config.corTema.g, config.corTema.b, config.corTema.a * alpha);
            }

            yield return null;
        }

        if (balao != null) {
            Destroy(balao);
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
