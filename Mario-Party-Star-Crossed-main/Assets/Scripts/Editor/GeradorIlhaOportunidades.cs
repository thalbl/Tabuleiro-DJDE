using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Gera Assets/Scenes/IlhaOportunidades.unity: um tabuleiro novo em forma de ilha, com três bairros (um por anel de carreira)
/// separados por um rio. Cada anel tem caminhos alternativos:
///  - Vila Aprendiz (anel 1): volta longa com Estrela/Universidade ou atalho arriscado pela rua do meio.
///  - Centro Comercial (anel 2): um "8" — dois circuitos (Comércio a oeste, Campus a leste) que se cruzam no portão.
///  - Distrito Corporativo (anel 3): volta longa com Estrela ou atalho pela Avenida Central; o portão final promove a cada volta.
/// Os portões ligam os anéis por avenidas que atravessam o rio em pontes.
/// Parte da cena do Circuito Rápido (UI, jogadores, BoardManager, volume) e troca todo o tabuleiro e o cenário.
/// Idempotente: apaga a cena gerada e gera de novo.
/// </summary>
public static class GeradorIlhaOportunidades {
    const string Base = "Assets/Scenes/MapaRapido.unity";
    public const string Destino = "Assets/Scenes/IlhaOportunidades.unity";
    const string PastaMat = "Assets/Materials/IlhaOportunidades";
    const float U = 3.2f;            // tamanho de uma célula da grade (distância entre casas)
    const float TopoChao = 0f;       // altura do gramado
    const float TopoRua = 0.07f;     // altura do asfalto
    const float NivelRio = -0.45f;
    const float NivelMar = -0.6f;

    // Ilha (em células)
    const int XMin = -5, XMax = 40, YMin = -4, YMax = 31;

    static System.Random rng;
    static Transform raiz, tCasas, tRuas, tTerreno, tCenario, tCeu;
    static GameObject modeloInicio;
    static readonly Dictionary<Vector2Int, Celula> celulas = new Dictionary<Vector2Int, Celula>();
    static readonly HashSet<Vector2Int> ocupado = new HashSet<Vector2Int>();
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    class Celula {
        public Vector2Int p;
        public char tipo;
        public BoardSpace casa;
        public readonly List<Vector2Int> ligacoes = new List<Vector2Int>(); // vizinhos ligados (para desenhar a rua)
    }

    // ───────────────────────── entrada ─────────────────────────

    [MenuItem("Ferramentas/Mapas/Gerar Ilha das Oportunidades")]
    public static void Gerar() {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        rng = new System.Random(20261111);
        celulas.Clear(); ocupado.Clear(); mats.Clear();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destino) != null) AssetDatabase.DeleteAsset(Destino);
        if (!AssetDatabase.CopyAsset(Base, Destino)) throw new System.Exception("Falha ao copiar " + Base);
        var cena = EditorSceneManager.OpenScene(Destino, OpenSceneMode.Single);

        var bm = Object.FindFirstObjectByType<BoardManager>();
        PrepararBase(bm);
        CriarMateriais();

        raiz = Novo("Ilha", bm.transform).transform;
        tTerreno = Novo("Terreno", raiz).transform;
        tRuas = Novo("Ruas", raiz).transform;
        tCasas = Novo("Casas", raiz).transform;
        tCasas.localScale = new Vector3(0.67f, 0.57f, 0.67f); // mesma escala das casas do Circuito Rápido
        tCenario = Novo("Cenario", raiz).transform;
        tCeu = Novo("Ceu", raiz).transform;

        DefinirTabuleiro();
        CriarCasas();
        ConfigurarCasas();
        Object.DestroyImmediate(modeloInicio);

        ConstruirTerreno();
        ConstruirRuas();
        ConstruirCenario();
        ConstruirCeu();
        AjustarLuz();

        var so = new SerializedObject(bm);
        so.FindProperty("metaCarreira").intValue = 6;
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.GetComponent<BoardSpace>() == null && t.GetComponentInParent<BoardSpace>() == null)
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AdicionarAoBuild();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ilha] Cena gerada: {celulas.Count} casas.");
    }

    public static void GerarBatch() {
        try { Gerar(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    // ───────────────────────── base da cena ─────────────────────────

    static void PrepararBase(BoardManager bm) {
        // O tabuleiro antigo é um prefab aninhado: desempacota, guarda a casa de Início como modelo e apaga o resto.
        var antigos = new List<GameObject>();
        foreach (Transform c in bm.transform) antigos.Add(c.gameObject);
        foreach (var go in antigos) {
            if (PrefabUtility.IsPartOfPrefabInstance(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var inicio = go.GetComponentInChildren<StartSpace>(true);
            if (inicio != null && modeloInicio == null) {
                modeloInicio = Object.Instantiate(inicio.gameObject);
                modeloInicio.name = "Start Space";
                modeloInicio.transform.localScale = inicio.transform.lossyScale;
            }
            Object.DestroyImmediate(go);
        }
        if (modeloInicio == null) throw new System.Exception("Casa de Início não encontrada na cena base.");
        AlinharCamerasDosJogadores();
        bm.transform.position = Vector3.zero;
        bm.transform.rotation = Quaternion.identity;
        bm.transform.localScale = Vector3.one;
    }

    // A câmera do Jogador 2 da cena base estava deslocada (6.3, 12.1, -2.2) e deixava o peão fora do quadro.
    // Todas passam a usar o enquadramento dos outros jogadores (o mesmo do MVP), não o do prefab.
    static readonly Vector3 CamPos = new Vector3(-0.10892391f, 6.29f, -6.83f);
    static readonly Quaternion CamRot = new Quaternion(0.2911737f, 0.0067256307f, -0.0014277812f, 0.9566455f);
    public static void AlinharCamerasDosJogadores() {
        foreach (var p in Object.FindObjectsByType<Player>(FindObjectsSortMode.None)) {
            var cam = p.GetComponentInChildren<Camera>(true);
            if (cam == null) continue;
            cam.transform.localPosition = CamPos;
            cam.transform.localRotation = CamRot;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cam.transform);
        }
    }

    static void AdicionarAoBuild() {
        var lista = EditorBuildSettings.scenes.ToList();
        if (lista.Any(s => s.path == Destino)) return;
        lista.Add(new EditorBuildSettingsScene(Destino, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ───────────────────────── o tabuleiro ─────────────────────────
    // Tipos: B bonificação, R penalidade, Q quiz, U universidade, N neutra, * estrela, $ loja, I consultor,
    //        X bifurcação, G portão de carreira, S início.

    static void DefinirTabuleiro() {
        // ── Anel 1: Vila Aprendiz ──
        Trecho("S", 1, 2, 1, 2);
        Trecho("BQBUBRBQ", 2, 2, 9, 2);          // rua de baixo (todos passam)
        Trecho("BGQXBIBB", 10, 2, 10, 9);        // lateral leste: portão 1 em (10,3), bifurcação em (10,5)
        Trecho("BUB*QRB", 9, 9, 3, 9);           // rua de cima (volta longa, com Estrela)
        Trecho("QBRBQBB", 2, 9, 2, 3);           // lateral oeste
        Trecho("RB$RQRB", 9, 5, 3, 5);           // atalho: rua do meio
        Proxima(1, 2, 2, 2);
        Proxima(9, 2, 10, 2);
        Proxima(10, 9, 9, 9);
        Proxima(3, 9, 2, 9);
        Proxima(2, 3, 2, 2);
        Opcao(10, 5, 9, 5);
        Proxima(3, 5, 2, 5);

        // Avenida do Primeiro Emprego (portão 1 → anel 2), com ponte em x 14-15
        Trecho("BQBBBQB", 11, 3, 17, 3);
        Saida(10, 3, 11, 3);
        Proxima(17, 3, 18, 3);

        // ── Anel 2: Centro Comercial (um "8") ──
        Trecho("UBQBRBQB", 26, 9, 26, 2);        // avenida central (todos passam)
        Trecho("X", 26, 1, 26, 1);               // bifurcação: oeste (Comércio) ou leste (Campus)
        Trecho("BRB$BQRB", 25, 1, 18, 1);
        Trecho("BBQBRIBQB", 18, 2, 18, 10);
        Trecho("BRBQURB", 19, 10, 25, 10);
        Trecho("G", 26, 10, 26, 10);             // portão 2, onde os dois circuitos se encontram
        Trecho("BQBRUBRB", 27, 1, 34, 1);
        Trecho("BQR*BQBRB", 34, 2, 34, 10);
        Trecho("BQRBIBQ", 33, 10, 27, 10);
        Proxima(26, 2, 26, 1);
        Proxima(26, 1, 25, 1);
        Opcao(26, 1, 27, 1);
        Proxima(18, 1, 18, 2);
        Proxima(18, 10, 19, 10);
        Proxima(25, 10, 26, 10);
        Proxima(34, 1, 34, 2);
        Proxima(34, 10, 33, 10);
        Proxima(27, 10, 26, 10);
        Proxima(26, 10, 26, 9);

        // Avenida da Promoção (portão 2 → anel 3), com ponte em y 12-13
        Trecho("BQBBQ", 26, 11, 26, 15);
        Saida(26, 10, 26, 11);
        Proxima(26, 15, 26, 16);

        // ── Anel 3: Distrito Corporativo ──
        Trecho("BQRBUBQBRBGBR", 20, 16, 32, 16); // rua de baixo: portão 3 em (30,16)
        Trecho("BQURBQRBB", 32, 17, 32, 25);
        Trecho("BRQBRXBQBIR", 31, 25, 21, 25);   // rua de cima: bifurcação em (26,25)
        Trecho("BQB*RBUQB", 20, 25, 20, 17);     // volta longa, com Estrela
        Trecho("RQ$RBRQR", 26, 24, 26, 17);      // atalho: Avenida Central
        Proxima(32, 16, 32, 17);
        Proxima(32, 25, 31, 25);
        Proxima(21, 25, 20, 25);
        Proxima(20, 17, 20, 16);
        Opcao(26, 25, 26, 24);
        Proxima(26, 17, 26, 16);
        Saida(30, 16, 31, 16);                   // portão final: promove e segue no próprio anel
    }

    static readonly Dictionary<Vector2Int, (string campo, Vector2Int alvo)[]> ligacoesPendentes = new Dictionary<Vector2Int, (string, Vector2Int)[]>();
    static readonly List<(Vector2Int de, string campo, Vector2Int para)> ligacoes = new List<(Vector2Int, string, Vector2Int)>();

    static void Trecho(string tipos, int x0, int y0, int x1, int y1) {
        tipos = tipos.Replace(" ", "");
        int dx = System.Math.Sign(x1 - x0), dy = System.Math.Sign(y1 - y0);
        if (dx != 0 && dy != 0) throw new System.Exception("Trecho precisa ser reto");
        int n = System.Math.Max(System.Math.Abs(x1 - x0), System.Math.Abs(y1 - y0)) + 1;
        if (n != tipos.Length) throw new System.Exception($"Trecho {x0},{y0}→{x1},{y1}: {n} células, {tipos.Length} tipos");
        Vector2Int? ant = null;
        for (int i = 0; i < n; i++) {
            var p = new Vector2Int(x0 + dx * i, y0 + dy * i);
            if (celulas.ContainsKey(p)) throw new System.Exception("Célula repetida " + p);
            celulas[p] = new Celula { p = p, tipo = tipos[i] };
            if (ant.HasValue) ligacoes.Add((ant.Value, "next", p));
            ant = p;
        }
    }
    static void Proxima(int x0, int y0, int x1, int y1) => ligacoes.Add((new Vector2Int(x0, y0), "next", new Vector2Int(x1, y1)));
    static void Opcao(int x0, int y0, int x1, int y1) => ligacoes.Add((new Vector2Int(x0, y0), "option", new Vector2Int(x1, y1)));
    static void Saida(int x0, int y0, int x1, int y1) => ligacoes.Add((new Vector2Int(x0, y0), "nextLoopStart", new Vector2Int(x1, y1)));

    static readonly Dictionary<char, string> prefabDoTipo = new Dictionary<char, string> {
        { 'B', "Assets/Prefabs/Board/Blue Space.prefab" },
        { 'R', "Assets/Prefabs/Board/Red Space.prefab" },
        { 'Q', "Assets/Prefabs/Board/Question Space.prefab" },
        { 'U', "Assets/Prefabs/Board/EducationSpace.prefab" },
        { 'N', "Assets/Prefabs/Board/Board Space.prefab" },
        { '*', "Assets/Prefabs/Board/Star Space.prefab" },
        { '$', "Assets/Prefabs/Board/Shop.prefab" },
        { 'I', "Assets/Prefabs/Board/Item Space.prefab" },
        { 'X', "Assets/Prefabs/Board/Intersection90.prefab" },
        { 'G', "Assets/Prefabs/Board/CareerTransitionSpace.prefab" },
    };
    static readonly Dictionary<char, string> nomeDoTipo = new Dictionary<char, string> {
        { 'B', "Bonificacao" }, { 'R', "Penalidade" }, { 'Q', "Quiz" }, { 'U', "Universidade" }, { 'N', "Neutra" },
        { '*', "Estrela" }, { '$', "Loja" }, { 'I', "Consultor" }, { 'X', "Bifurcacao" }, { 'G', "Portao" }, { 'S', "Inicio" },
    };

    static Vector3 Mundo(Vector2Int c, float y = 0) => new Vector3(c.x * U, y, c.y * U);
    static Vector3 Mundo(float x, float y, float alt = 0) => new Vector3(x * U, alt, y * U);

    static void CriarCasas() {
        int id = 1;
        // ordem estável: pela ordem de definição (o dicionário preserva a inserção)
        foreach (var c in celulas.Values) {
            GameObject go;
            if (c.tipo == 'S') {
                go = Object.Instantiate(modeloInicio, tCasas);
                go.transform.localScale = Vector3.Scale(modeloInicio.transform.localScale, new Vector3(1 / tCasas.lossyScale.x, 1 / tCasas.lossyScale.y, 1 / tCasas.lossyScale.z));
            } else {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabDoTipo[c.tipo]);
                go = (GameObject)PrefabUtility.InstantiatePrefab(asset, tCasas);
            }
            go.transform.position = Mundo(c.p, TopoRua + 0.02f);
            go.name = $"{nomeDoTipo[c.tipo]} ({c.p.x},{c.p.y})";
            c.casa = go.GetComponent<BoardSpace>();
            int meuId = c.tipo == 'S' ? 0 : ("X$I*".IndexOf(c.tipo) >= 0 ? -1 : id++);
            Definir(c.casa, "id", meuId);
            ocupado.Add(c.p);
        }
        foreach (var (de, campo, para) in ligacoes) {
            if (!celulas.TryGetValue(de, out var a)) throw new System.Exception("Ligação de célula inexistente " + de);
            if (!celulas.TryGetValue(para, out var b)) throw new System.Exception("Ligação para célula inexistente " + para);
            var so = new SerializedObject(a.casa);
            var prop = so.FindProperty(campo);
            if (prop == null) throw new System.Exception($"{a.casa.name} não tem o campo {campo}");
            prop.objectReferenceValue = b.casa;
            so.ApplyModifiedPropertiesWithoutUndo();
            if ((de - para).sqrMagnitude == 1) { a.ligacoes.Add(para); b.ligacoes.Add(de); }
        }
    }

    static void ConfigurarCasas() {
        // Portões
        Portao(10, 3, carreira: 1, educacao: 1, moedas: 40, maxNivel: 1);
        Portao(26, 10, carreira: 2, educacao: 1, moedas: 80, maxNivel: 3);
        Portao(30, 16, carreira: 3, educacao: 1, moedas: 150, maxNivel: 10);
        // Universidades: mais caras nos bairros mais avançados
        foreach (var c in celulas.Values.Where(c => c.tipo == 'U')) {
            int custo = c.p.y >= 16 ? 70 : (c.p.x >= 11 ? 50 : 30);
            Definir(c.casa, "educationCost", custo);
        }
        // Bifurcações: sem portões de chave; prismas apontando para as duas saídas
        foreach (var c in celulas.Values.Where(c => c.tipo == 'X')) {
            var x = (Intersection)c.casa;
            Definir(x, "gate", (Object)null);
            Definir(x, "optionGate", (Object)null);
            ApontarPrisma(x, "PhantomPrism", x.next.transform.position);
            ApontarPrisma(x, "PhantomPrism (ALT)", x.option.transform.position);
        }
        // Loja e Estrela de frente para quem chega
        foreach (var c in celulas.Values.Where(c => c.tipo == '$' || c.tipo == '*')) {
            var ant = celulas.Values.FirstOrDefault(o => o.casa.next == c.casa);
            if (ant == null) continue;
            var dir = c.casa.transform.position - ant.casa.transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) c.casa.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
        }
    }

    static void Portao(int x, int y, int carreira, int educacao, int moedas, int maxNivel) {
        var g = celulas[new Vector2Int(x, y)].casa;
        Definir(g, "requiredCareerLevel", carreira);
        Definir(g, "requiredEducation", educacao);
        Definir(g, "minSavings", moedas);
        Definir(g, "maxPromotionCareerLevel", maxNivel);
    }

    static void Definir(Object alvo, string campo, int valor) {
        var so = new SerializedObject(alvo); var p = so.FindProperty(campo);
        if (p == null) throw new System.Exception($"{alvo.name}: campo {campo} não existe");
        p.intValue = valor; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Definir(Object alvo, string campo, Object valor) {
        var so = new SerializedObject(alvo); var p = so.FindProperty(campo);
        if (p == null) throw new System.Exception($"{alvo.name}: campo {campo} não existe");
        p.objectReferenceValue = valor; so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApontarPrisma(Intersection x, string filho, Vector3 destino) {
        var prisma = x.transform.Find(filho);
        if (prisma == null) return;
        Vector3 centro = x.transform.position;
        Vector3 atual = prisma.position - centro; atual.y = 0;
        Vector3 alvo = destino - centro; alvo.y = 0;
        if (atual.sqrMagnitude < 1e-4f || alvo.sqrMagnitude < 1e-4f) return;
        prisma.RotateAround(centro, Vector3.up, Vector3.SignedAngle(atual, alvo, Vector3.up));
    }

    // ───────────────────────── materiais ─────────────────────────

    static void CriarMateriais() {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(PastaMat)) AssetDatabase.CreateFolder("Assets/Materials", "IlhaOportunidades");
        Mat("Grama", new Color(0.47f, 0.73f, 0.33f), 0.05f);
        Mat("GramaParque", new Color(0.40f, 0.68f, 0.30f), 0.05f);
        Mat("Terra", new Color(0.56f, 0.41f, 0.27f), 0.05f);
        Mat("Areia", new Color(0.94f, 0.85f, 0.62f), 0.05f);
        Mat("Asfalto", new Color(0.33f, 0.34f, 0.38f), 0.15f);
        Mat("Calcada", new Color(0.80f, 0.79f, 0.75f), 0.1f);
        Mat("Piso", new Color(0.86f, 0.82f, 0.72f), 0.15f);
        Mat("Faixa", new Color(0.97f, 0.93f, 0.70f), 0.1f);
        Mat("Madeira", new Color(0.58f, 0.38f, 0.22f), 0.1f);
        Mat("Rio", new Color(0.27f, 0.63f, 0.84f), 0.9f);
        Mat("Mar", new Color(0.18f, 0.52f, 0.78f), 0.92f);
        Mat("Placa", new Color(0.06f, 0.16f, 0.12f), 0.2f);

        var ceuPath = PastaMat + "/Ceu.mat";
        var ceu = AssetDatabase.LoadAssetAtPath<Material>(ceuPath);
        if (ceu == null) { ceu = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(ceu, ceuPath); }
        ceu.shader = Shader.Find("Skybox/Procedural");
        ceu.SetFloat("_SunDisk", 2f);
        ceu.SetFloat("_SunSize", 0.045f);
        ceu.SetFloat("_SunSizeConvergence", 6f);
        ceu.SetFloat("_AtmosphereThickness", 0.75f);
        ceu.SetColor("_SkyTint", new Color(0.42f, 0.62f, 0.92f));
        ceu.SetColor("_GroundColor", new Color(0.30f, 0.52f, 0.66f));
        ceu.SetFloat("_Exposure", 1.25f);
        EditorUtility.SetDirty(ceu);
        mats["Ceu"] = ceu;
    }

    static void Mat(string nome, Color cor, float liso) {
        var path = $"{PastaMat}/{nome}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", cor);
        m.SetFloat("_Smoothness", liso);
        m.SetFloat("_Metallic", 0f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        mats[nome] = m;
    }

    // ───────────────────────── terreno, rio e mar ─────────────────────────

    enum Solo { Mar, Areia, Grama, Parque, Piso, Rio, Lago }
    static Solo[,] solo;
    static int W => XMax - XMin + 1;
    static int H => YMax - YMin + 1;
    static Solo SoloEm(int x, int y) => (x < XMin || x > XMax || y < YMin || y > YMax) ? Solo.Mar : solo[x - XMin, y - YMin];

    static bool NaIlha(int x, int y) {
        if (x < XMin || x > XMax || y < YMin || y > YMax) return false;
        // cantos arredondados + uma costa levemente irregular
        float r = 6f;
        float cx = Mathf.Clamp(x, XMin + r, XMax - r), cy = Mathf.Clamp(y, YMin + r, YMax - r);
        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        float ruido = Mathf.PerlinNoise(x * 0.31f, y * 0.29f) * 1.6f;
        return d <= r - ruido + 0.6f;
    }

    static bool EhRio(int x, int y) => ((x == 14 || x == 15) && y <= 13) || ((y == 12 || y == 13) && x >= 14);
    static bool EhLago(int x, int y) {
        float dx = (x - 4.5f) / 4.6f, dy = (y - 20.5f) / 3.4f;
        return dx * dx + dy * dy <= 1f;
    }

    static void ConstruirTerreno() {
        solo = new Solo[W, H];
        for (int x = XMin; x <= XMax; x++)
            for (int y = YMin; y <= YMax; y++) {
                Solo s;
                if (!NaIlha(x, y)) s = Solo.Mar;
                else if (EhRio(x, y)) s = Solo.Rio;
                else if (EhLago(x, y)) s = Solo.Lago;
                else if (x >= 21 && x <= 31 && y >= 17 && y <= 24) s = Solo.Piso;   // praças do distrito corporativo
                else if (x <= 13 && y >= 12) s = Solo.Parque;
                else s = Solo.Grama;
                solo[x - XMin, y - YMin] = s;
            }
        // praia: terra firme vizinha do mar vira areia
        var praia = new List<Vector2Int>();
        for (int x = XMin; x <= XMax; x++)
            for (int y = YMin; y <= YMax; y++) {
                var s = SoloEm(x, y);
                if (s == Solo.Mar || s == Solo.Rio || s == Solo.Lago) continue;
                if (celulas.ContainsKey(new Vector2Int(x, y))) continue;
                bool perto = false;
                for (int i = -1; i <= 1 && !perto; i++) for (int j = -1; j <= 1; j++) if (SoloEm(x + i, y + j) == Solo.Mar) { perto = true; break; }
                if (perto) praia.Add(new Vector2Int(x, y));
            }
        foreach (var p in praia) solo[p.x - XMin, p.y - YMin] = Solo.Areia;
        // o rio desce até o mar: as células do rio fora da ilha já são mar

        // blocos retangulares por tipo de solo (junta linhas iguais para economizar objetos)
        Retangulos(Solo.Grama, (r) => Bloco("Grama", r, TopoChao, mats["Grama"]));
        Retangulos(Solo.Parque, (r) => Bloco("Parque", r, TopoChao, mats["GramaParque"]));
        Retangulos(Solo.Piso, (r) => Bloco("Praca", r, TopoChao, mats["Piso"]));
        Retangulos(Solo.Areia, (r) => Bloco("Praia", r, -0.25f, mats["Areia"]));
        Retangulos(Solo.Rio, (r) => Agua("Rio", r, NivelRio, mats["Rio"]));
        Retangulos(Solo.Lago, (r) => Agua("Lago", r, NivelRio, mats["Rio"]));

        // fundo do rio/lago e base da ilha (terra), para não ver o vazio por baixo
        var b = Cubo("BaseDaIlha", tTerreno, mats["Terra"]);
        b.transform.position = new Vector3((XMin + XMax) * 0.5f * U, -1.6f, (YMin + YMax) * 0.5f * U);
        b.transform.localScale = new Vector3((W - 3) * U, 2.0f, (H - 3) * U);

        var mar = Cubo("Mar", tTerreno, mats["Mar"]);
        mar.transform.position = new Vector3((XMin + XMax) * 0.5f * U, NivelMar - 0.5f, (YMin + YMax) * 0.5f * U);
        mar.transform.localScale = new Vector3(1600f, 1f, 1600f);
    }

    struct Ret { public int x0, y0, x1, y1; }

    static void Retangulos(Solo tipo, System.Action<Ret> criar) {
        var usado = new bool[W, H];
        for (int y = YMin; y <= YMax; y++)
            for (int x = XMin; x <= XMax; x++) {
                if (usado[x - XMin, y - YMin] || SoloEm(x, y) != tipo) continue;
                int x1 = x;
                while (x1 + 1 <= XMax && !usado[x1 + 1 - XMin, y - YMin] && SoloEm(x1 + 1, y) == tipo) x1++;
                int y1 = y;
                while (true) {
                    int ny = y1 + 1; if (ny > YMax) break;
                    bool ok = true;
                    for (int i = x; i <= x1; i++) if (usado[i - XMin, ny - YMin] || SoloEm(i, ny) != tipo) { ok = false; break; }
                    if (!ok) break;
                    y1 = ny;
                }
                for (int i = x; i <= x1; i++) for (int j = y; j <= y1; j++) usado[i - XMin, j - YMin] = true;
                criar(new Ret { x0 = x, y0 = y, x1 = x1, y1 = y1 });
            }
    }

    static void Bloco(string nome, Ret r, float topo, Material topoMat) {
        float cx = (r.x0 + r.x1) * 0.5f * U, cz = (r.y0 + r.y1) * 0.5f * U;
        float sx = (r.x1 - r.x0 + 1) * U, sz = (r.y1 - r.y0 + 1) * U;
        var t = Cubo(nome, tTerreno, topoMat);
        t.transform.position = new Vector3(cx, topo - 0.15f, cz);
        t.transform.localScale = new Vector3(sx, 0.3f, sz);
        var d = Cubo(nome + "_Terra", tTerreno, mats["Terra"]);
        d.transform.position = new Vector3(cx, topo - 0.3f - 0.6f, cz);
        d.transform.localScale = new Vector3(sx, 1.2f, sz);
    }

    static void Agua(string nome, Ret r, float nivel, Material m) {
        float cx = (r.x0 + r.x1) * 0.5f * U, cz = (r.y0 + r.y1) * 0.5f * U;
        var t = Cubo(nome, tTerreno, m);
        t.transform.position = new Vector3(cx, nivel - 0.5f, cz);
        t.transform.localScale = new Vector3((r.x1 - r.x0 + 1) * U, 1f, (r.y1 - r.y0 + 1) * U);
    }

    // ───────────────────────── ruas e pontes ─────────────────────────

    static void ConstruirRuas() {
        foreach (var c in celulas.Values) {
            var centro = Mundo(c.p);
            bool ponte = SoloEm(c.p.x, c.p.y) == Solo.Rio;
            if (ponte) {
                var deck = Cubo($"Ponte {c.p}", tRuas, mats["Madeira"]);
                deck.transform.position = centro + Vector3.up * (TopoRua - 0.12f);
                deck.transform.localScale = new Vector3(U * 0.98f, 0.24f, U * 0.98f);
                // guarda-corpos dos dois lados, paralelos ao sentido de travessia
                bool horizontal = c.ligacoes.Any(v => v.y == c.p.y);
                for (int lado = -1; lado <= 1; lado += 2) {
                    var g = Cubo("GuardaCorpo", deck.transform.parent, mats["Madeira"]);
                    Vector3 off = horizontal ? new Vector3(0, 0, lado * U * 0.45f) : new Vector3(lado * U * 0.45f, 0, 0);
                    g.transform.position = centro + off + Vector3.up * 0.45f;
                    g.transform.localScale = horizontal ? new Vector3(U, 0.12f, 0.12f) : new Vector3(0.12f, 0.12f, U);
                    for (int k = -1; k <= 1; k++) {
                        var poste = Cubo("Poste", deck.transform.parent, mats["Madeira"]);
                        Vector3 ao = horizontal ? new Vector3(k * U * 0.45f, 0, 0) : new Vector3(0, 0, k * U * 0.45f);
                        poste.transform.position = centro + off + ao + Vector3.up * 0.25f;
                        poste.transform.localScale = new Vector3(0.14f, 0.5f, 0.14f);
                    }
                }
                var pilar = Cubo("Pilar", tRuas, mats["Terra"]);
                pilar.transform.position = centro + Vector3.down * 0.6f;
                pilar.transform.localScale = new Vector3(U * 0.3f, 1.0f, U * 0.3f);
                continue;
            }
            var calc = Cubo($"Calcada {c.p}", tRuas, SoloEm(c.p.x, c.p.y) == Solo.Piso ? mats["Piso"] : mats["Calcada"]);
            calc.transform.position = centro + Vector3.up * 0.02f;
            calc.transform.localScale = new Vector3(U, 0.06f, U);
            float largura = U * 0.62f;
            var meio = Cubo($"Asfalto {c.p}", tRuas, mats["Asfalto"]);
            meio.transform.position = centro + Vector3.up * (TopoRua - 0.02f);
            meio.transform.localScale = new Vector3(largura, 0.04f, largura);
            foreach (var v in c.ligacoes) {
                if (SoloEm(v.x, v.y) == Solo.Rio) { } // a rua encosta na ponte normalmente
                var d = new Vector3(v.x - c.p.x, 0, v.y - c.p.y);
                var braco = Cubo("Asfalto", tRuas, mats["Asfalto"]);
                braco.transform.position = centro + d * (U * 0.405f) + Vector3.up * (TopoRua - 0.02f);
                braco.transform.localScale = d.x != 0 ? new Vector3(U * 0.19f, 0.04f, largura) : new Vector3(largura, 0.04f, U * 0.19f);
                // faixa central tracejada
                var faixa = Cubo("Faixa", tRuas, mats["Faixa"]);
                faixa.transform.position = centro + d * (U * 0.36f) + Vector3.up * (TopoRua + 0.005f);
                faixa.transform.localScale = d.x != 0 ? new Vector3(U * 0.16f, 0.01f, 0.08f) : new Vector3(0.08f, 0.01f, U * 0.16f);
            }
        }
    }

    // ───────────────────────── cenário ─────────────────────────

    const string PB = "Assets/Thirdparty/Buildings/";
    const string PE = "Assets/Thirdparty/European Buildings/";
    const string PP = "Assets/Thirdparty/Palmov Island/Low Poly Houses Free Pack/Prefabs/";
    const string PK = "Assets/Thirdparty/Park/Prefabs/";
    const string PL = "Assets/Thirdparty/Low Poly Pack - Environment Lite/Prefabs/";
    const string PC = "Assets/Thirdparty/CubexCube/Prefabs/";

    static void ConstruirCenario() {
        // ── Vila Aprendiz: casas, escola e parquinho ──
        Predio(PP + "Houses/cute house.prefab", 3.5f, 3.5f, 2, 1.6f);
        Predio(PP + "Houses/big cottage 1 floor new.prefab", 5.8f, 3.5f, 2, 1.8f);
        Predio(PB + "house_2_v_5.fbx", 8.2f, 3.5f, 2, 1.6f);
        Predio(PB + "school.fbx", 6f, 7f, 3, 2.6f);
        Predio(PB + "house_2_v_12.fbx", 3.4f, 7.3f, 2, 1.4f);
        Predio(PC + "Building/Bld_5_Floor_2_Porch_House_1.prefab", 8.7f, 7.4f, 2, 1.4f);
        Predio(PB + "house_3.fbx", -0.5f, 3f, 2, 1.8f);
        Predio(PB + "house_4.fbx", -0.5f, 6.5f, 2, 1.8f);
        Predio(PB + "house_2_v_20.fbx", -0.5f, 9.8f, 2, 1.6f);
        Predio(PB + "house_2_v_3.fbx", 4f, 0f, 2, 1.6f);
        Predio(PB + "house_2_v_8.fbx", 7f, 0f, 2, 1.6f);
        Predio(PP + "Houses/post office.prefab", 1.2f, -0.2f, 2, 1.6f);
        Predio(PB + "grocery_store.fbx", 12f, 6.5f, 2, 2.0f);
        Predio(PB + "pharmacy.fbx", 12f, 9.5f, 2, 1.6f);
        Predio(PB + "house_2_v_25.fbx", 12f, 0.5f, 2, 1.6f);
        Predio(PC + "Props_Street/Env_Playground.prefab", 6f, 11f, 1, 1.3f);
        Predio(PP + "Grounds/tennis court brown.prefab", 2.5f, 11f, 2, 2.6f);

        // ── Centro Comercial ──
        Predio(PB + "cinema.fbx", 21f, 7.5f, 3, 3.0f);
        Predio(PB + "clothing_store.fbx", 24f, 7.6f, 2, 1.8f);
        Predio(PB + "bookstore.fbx", 20.2f, 3.7f, 2, 1.8f);
        Predio(PB + "computer_store.fbx", 22.6f, 3.7f, 2, 1.8f);
        Predio(PK + "Fountain1.prefab", 22.5f, 5.6f, 1, 1.4f);
        Predio(PB + "sport_store.fbx", 24.5f, 3.6f, 1, 1.4f);
        Predio(PB + "university.fbx", 30f, 6.4f, 4, 4.2f);
        Predio(PB + "publisher.fbx", 28.2f, 2.9f, 2, 1.6f);
        Predio(PB + "electronic_store.fbx", 32.2f, 2.9f, 2, 1.6f);
        for (int i = 0; i < 8; i++) Predio(PE + $"Buildings/building_{i + 1}.prefab", 18.5f + i * 2.1f, -1.4f, 2, 5.2f, porAltura: true);
        Predio(PB + "office_supply_store.fbx", 37f, 2f, 2, 2.0f);
        Predio(PB + "furniture_store.fbx", 37f, 5.5f, 2, 2.0f);
        Predio(PB + "toy_store.fbx", 37f, 9f, 2, 1.8f);
        Predio(PB + "video_games_store.fbx", 21f, 11.2f, 1, 1.4f);
        Predio(PB + "cosmetic_shop.fbx", 31f, 11.2f, 1, 1.4f);

        // ── Distrito Corporativo ──
        Predio(PB + "city_hall.fbx", 23f, 21.5f, 4, 4.0f);
        Predio(PB + "parlament.fbx", 23f, 18.3f, 2, 2.8f);
        Predio(PB + "software_company.fbx", 29f, 21.5f, 4, 3.8f);
        Predio(PB + "studio_video_games.fbx", 29f, 18.3f, 2, 2.8f);
        Predio(PB + "power_plant.fbx", 20f, 28.5f, 3, 3.6f);
        Predio(PB + "electronics_factory.fbx", 24.5f, 28.5f, 3, 3.4f);
        Predio(PB + "car_factory.fbx", 29f, 28.5f, 3, 3.6f);
        Predio(PB + "food_factory.fbx", 33.8f, 28.3f, 3, 3.0f);
        Predio(PB + "movie_studio.fbx", 36.8f, 23f, 3, 3.2f);
        Predio(PB + "heavy_equipment_factory.fbx", 36.8f, 18.5f, 3, 3.2f);
        Predio(PB + "industrial_factory.fbx", 36.5f, 15.6f, 2, 2.4f);
        Predio(PB + "police_academy.fbx", 17f, 22f, 3, 2.8f);
        Predio(PB + "software_company.fbx", 17f, 18f, 2, 2.0f);

        // ── Parque da Ilha (noroeste) ──
        Predio(PP + "Lunapark/ferris wheel.prefab", 9.5f, 27f, 3, 4.2f, porAltura: true);
        Predio(PK + "Fountain_1.prefab", 9.5f, 16f, 2, 1.6f);
        Passarela(-0.6f, 9.6f, 20.5f);
        Predio(PK + "Boat.prefab", 3f, 22.3f, 0, 1.0f, yaw: 75, chao: NivelRio, marcar: false);
        Predio(PK + "Boat.prefab", 6.5f, 18.6f, 0, 1.0f, yaw: 160, chao: NivelRio, marcar: false);
        Predio(PK + "Hovel_Green.prefab", 0.5f, 14.5f, 1, 1.4f);
        Predio(PK + "Hovel_Orange.prefab", 12f, 21.5f, 1, 1.4f);
        Predio(PK + "Hovel_Red.prefab", 1f, 27f, 1, 1.4f);
        Predio(PP + "Environment/fountain.prefab", 6f, 15f, 1, 1.2f);
        foreach (var (x, y) in new[] { (8f, 18f), (2f, 17f), (11f, 24f), (7f, 24.5f), (1.5f, 23.5f) })
            Predio(PK + "DoubleBench.prefab", x, y, 0, 0.8f);

        // ── Costa: pedras na praia e barcos no mar ──
        string[] pedras = { PL + "Rock_1_13.prefab", PL + "Rock_1_14.prefab", PL + "Rock_1_15.prefab", PL + "Stone_3_1.prefab", PL + "Stone_3_2.prefab" };
        for (int x = XMin; x <= XMax; x++)
            for (int y = YMin; y <= YMax; y++) {
                if (SoloEm(x, y) != Solo.Areia || ocupado.Contains(new Vector2Int(x, y))) continue;
                if (rng.NextDouble() < 0.16) {
                    Predio(pedras[rng.Next(pedras.Length)], x + (float)rng.NextDouble() * 0.6f - 0.3f, y + (float)rng.NextDouble() * 0.6f - 0.3f,
                        0, 0.5f + (float)rng.NextDouble() * 0.9f, yaw: rng.Next(360), chao: -0.25f, marcar: false);
                }
            }
        Predio(PK + "Boat.prefab", 44f, 8f, 0, 2.2f, yaw: 30, chao: NivelMar);
        Predio(PK + "Boat.prefab", -8f, 20f, 0, 2.2f, yaw: 200, chao: NivelMar);
        Predio(PK + "Boat.prefab", 18f, -8f, 0, 2.2f, yaw: 110, chao: NivelMar);

        // ── Postes de luz nas esquinas ──
        foreach (var c in celulas.Values) {
            if (c.ligacoes.Count < 2) continue;
            bool curva = !(c.ligacoes.All(v => v.x == c.p.x) || c.ligacoes.All(v => v.y == c.p.y));
            if (!curva) continue;
            // canto externo da curva
            var soma = Vector2.zero; foreach (var v in c.ligacoes) soma += (Vector2)(v - c.p);
            var fora = -soma.normalized * 0.47f;
            Predio(PP + "Environment/lamppost.prefab", c.p.x + fora.x, c.p.y + fora.y, 0, 1.6f, porAltura: true, marcar: false);
        }

        // ── Árvores preenchendo o que sobrou ──
        string[] arvVila = { PP + "Trees/round tree.prefab", PP + "Trees/tree.prefab", PK + "Tree_1.prefab", PK + "Tree_2.prefab" };
        string[] arvParque = { PK + "Tree_3.prefab", PK + "Tree_4.prefab", PK + "Tree_5.prefab", PK + "Tree_1_Autumn.prefab", PK + "Tree_3_Autumn.prefab", PP + "Trees/round tree.prefab" };
        string[] arvCosta = { PP + "Trees/fir tree 1.prefab", PP + "Trees/fir tree 2.prefab" };
        string[] arbustos = { PK + "Bush.prefab", PP + "Plants/bush 1.prefab", PP + "Plants/bush 2.prefab", PP + "Plants/bush 3.prefab" };
        for (int x = XMin; x <= XMax; x++)
            for (int y = YMin; y <= YMax; y++) {
                var p = new Vector2Int(x, y);
                var s = SoloEm(x, y);
                if (ocupado.Contains(p) || s == Solo.Mar || s == Solo.Rio || s == Solo.Lago || s == Solo.Areia) continue;
                if (PertoDeRua(p)) continue;
                bool costa = false;
                for (int i = -2; i <= 2 && !costa; i++) for (int j = -2; j <= 2; j++) if (SoloEm(x + i, y + j) == Solo.Areia) { costa = true; break; }
                double chance; string[] lista;
                if (costa) { chance = 0.55; lista = arvCosta; }
                else if (s == Solo.Parque) { chance = 0.6; lista = arvParque; }
                else if (s == Solo.Piso) { chance = 0.0; lista = arvVila; }
                else if (x <= 13) { chance = 0.35; lista = arvVila; }
                else if (y >= 14) { chance = 0.18; lista = arvVila; }
                else { chance = 0.2; lista = arvVila; }
                if (rng.NextDouble() >= chance) {
                    // onde não nasce árvore, às vezes nasce um arbusto ou canteiro
                    if (s != Solo.Piso && rng.NextDouble() < (s == Solo.Parque ? 0.45 : 0.15))
                        Predio(arbustos[rng.Next(arbustos.Length)], x + (float)rng.NextDouble() * 0.6f - 0.3f, y + (float)rng.NextDouble() * 0.6f - 0.3f,
                            0, 0.22f + (float)rng.NextDouble() * 0.15f, yaw: rng.Next(360), marcar: false);
                    continue;
                }
                float alt = costa ? 2.6f : (s == Solo.Parque ? 2.8f : 2.2f);
                alt *= 0.8f + (float)rng.NextDouble() * 0.45f;
                Predio(lista[rng.Next(lista.Length)], x + (float)rng.NextDouble() * 0.5f - 0.25f, y + (float)rng.NextDouble() * 0.5f - 0.25f,
                    0, alt, porAltura: true, yaw: rng.Next(360));
            }

        // ── Placas dos bairros ──
        Placa("VILA APRENDIZ", 6f, -2.6f);
        Placa("CENTRO COMERCIAL", 26f, -2.9f);
        Placa("DISTRITO CORPORATIVO", 26f, 30.1f);
        Placa("PARQUE DA ILHA", 5f, 13.2f);
    }

    // Passarela de madeira atravessando o lago no sentido leste-oeste
    static void Passarela(float x0, float x1, float y) {
        var pai = Novo("Passarela do Lago", tCenario).transform;
        float cx = (x0 + x1) * 0.5f * U, cz = y * U, comp = (x1 - x0) * U;
        var deck = Cubo("Deck", pai, mats["Madeira"]);
        deck.transform.position = new Vector3(cx, 0.05f, cz);
        deck.transform.localScale = new Vector3(comp, 0.18f, U * 0.42f);
        for (int lado = -1; lado <= 1; lado += 2) {
            var g = Cubo("GuardaCorpo", pai, mats["Madeira"]);
            g.transform.position = new Vector3(cx, 0.55f, cz + lado * U * 0.2f);
            g.transform.localScale = new Vector3(comp, 0.08f, 0.08f);
        }
        for (float x = x0; x <= x1 + 0.01f; x += 0.8f) {
            for (int lado = -1; lado <= 1; lado += 2) {
                var p = Cubo("Poste", pai, mats["Madeira"]);
                p.transform.position = new Vector3(x * U, 0.0f, cz + lado * U * 0.2f);
                p.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
            }
        }
        for (int i = Mathf.FloorToInt(x0); i <= Mathf.CeilToInt(x1); i++) ocupado.Add(new Vector2Int(i, Mathf.RoundToInt(y)));
    }

    static bool PertoDeRua(Vector2Int p) {
        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) if (celulas.ContainsKey(new Vector2Int(p.x + i, p.y + j))) return true;
        return false;
    }

    /// Coloca um prefab/modelo centrado em (x, y) da grade, escalado para caber em "tamanho" células
    /// (pela maior medida horizontal, ou pela altura), voltado para a rua mais próxima quando yaw não é dado.
    static GameObject Predio(string caminho, float x, float y, int celulasOcupadas, float tamanho, bool porAltura = false,
                             float? yaw = null, float chao = TopoChao, bool marcar = true) {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (asset == null) { Debug.LogWarning("[Ilha] não achei " + caminho); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, tCenario);
        float rot = yaw ?? DirecaoDaRua(x, y) + OffsetFrente(caminho);
        go.transform.rotation = Quaternion.Euler(0, rot, 0) * asset.transform.rotation;
        var b = Limites(go);
        float atual = porAltura ? b.size.y : Mathf.Max(b.size.x, b.size.z);
        if (atual < 1e-4f) { Object.DestroyImmediate(go); return null; }
        go.transform.localScale = asset.transform.localScale * (tamanho * (porAltura ? 1f : U) / atual);
        b = Limites(go);
        var alvo = Mundo(x, y, chao);
        go.transform.position += new Vector3(alvo.x - b.center.x, alvo.y - b.min.y, alvo.z - b.center.z);
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        if (marcar && celulasOcupadas > 0) {
            float meio = celulasOcupadas * 0.5f;
            for (int i = Mathf.FloorToInt(x - meio + 0.5f); i <= Mathf.CeilToInt(x + meio - 0.5f); i++)
                for (int j = Mathf.FloorToInt(y - meio + 0.5f); j <= Mathf.CeilToInt(y + meio - 0.5f); j++)
                    ocupado.Add(new Vector2Int(i, j));
        } else if (marcar) ocupado.Add(new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y)));
        return go;
    }

    // ângulo (graus) para o +Z do objeto apontar para a célula de rua mais próxima
    static float DirecaoDaRua(float x, float y) {
        Vector2 me = new Vector2(x, y); float melhor = float.MaxValue; Vector2 alvo = me + Vector2.down;
        foreach (var c in celulas.Keys) { float d = (c - me).sqrMagnitude; if (d < melhor) { melhor = d; alvo = c; } }
        var dir = alvo - me;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) dir.y = 0; else dir.x = 0; // só 4 direções
        return Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
    }

    // Lado da "frente" de cada pacote (graus a somar para a fachada ficar virada para +Z).
    static float OffsetFrente(string caminho) {
        if (caminho.StartsWith(PB)) return 180f;
        if (caminho.StartsWith(PE)) return 0f;
        if (caminho.StartsWith(PP)) return 0f;
        if (caminho.StartsWith(PC)) return 0f;
        return 0f;
    }

    static Bounds Limites(GameObject go) {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }

    static void Placa(string texto, float x, float y) {
        var placa = Novo("Placa " + texto, tCenario);
        var pos = Mundo(x, y);
        var fundo = Cubo("Fundo", placa.transform, mats["Placa"]);
        fundo.transform.position = pos + Vector3.up * 1.6f;
        float largura = 0.42f * texto.Length + 0.8f;
        fundo.transform.localScale = new Vector3(largura, 1.0f, 0.15f);
        for (int lado = -1; lado <= 1; lado += 2) {
            var poste = Cubo("Poste", placa.transform, mats["Madeira"]);
            poste.transform.position = pos + new Vector3(lado * (largura * 0.5f - 0.3f), 0.55f, 0);
            poste.transform.localScale = new Vector3(0.15f, 1.1f, 0.15f);
        }
        var tgo = new GameObject("Texto"); tgo.transform.SetParent(placa.transform);
        var tm = tgo.AddComponent<TextMesh>();
        tm.text = texto; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = 0.075f; tm.color = new Color(0.98f, 0.75f, 0.14f);
        var fonte = AssetDatabase.FindAssets("Fredoka t:Font").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Font>).FirstOrDefault(f => f != null);
        if (fonte != null) { tm.font = fonte; tgo.GetComponent<MeshRenderer>().sharedMaterial = fonte.material; }
        tgo.transform.position = pos + new Vector3(0, 1.6f, -0.09f);
        tgo.transform.rotation = Quaternion.identity;
    }

    // ───────────────────────── céu e luz ─────────────────────────

    static void ConstruirCeu() {
        var nuvem = PL + "Cloud_1_5.prefab";
        // nuvens longe da ilha, no horizonte (não atrapalham a câmera de cima)
        for (int i = 0; i < 14; i++) {
            float ang = i / 14f * Mathf.PI * 2f + (float)rng.NextDouble() * 0.3f;
            float raio = 170f + (float)rng.NextDouble() * 90f;
            var centro = new Vector3((XMin + XMax) * 0.5f * U, 0, (YMin + YMax) * 0.5f * U);
            var go = Predio(nuvem, 0, 0, 0, 7f + (float)rng.NextDouble() * 6f, yaw: rng.Next(360), marcar: false);
            if (go == null) continue;
            go.transform.SetParent(tCeu, true);
            go.transform.position = centro + new Vector3(Mathf.Cos(ang) * raio, 22f + (float)rng.NextDouble() * 18f, Mathf.Sin(ang) * raio);
        }
    }

    static void AjustarLuz() {
        RenderSettings.skybox = mats["Ceu"];
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.66f, 0.76f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.62f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.34f, 0.31f, 0.26f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.70f, 0.82f, 0.93f);
        RenderSettings.fogStartDistance = 200f;   // além da câmera "Ver Tabuleiro"
        RenderSettings.fogEndDistance = 600f;
        var sol = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sol != null) {
            sol.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            sol.color = new Color(1f, 0.95f, 0.84f);
            sol.intensity = 1.15f;
            sol.shadows = LightShadows.Soft;
            RenderSettings.sun = sol;
        }
    }

    // ───────────────────────── utilitários ─────────────────────────

    static GameObject Novo(string nome, Transform pai) {
        var go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        return go;
    }

    static GameObject Cubo(string nome, Transform pai, Material m) {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(pai, false);
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        return go;
    }
}
