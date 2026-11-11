using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual das casas do tabuleiro (Bloco E): brilho (emissão) por tipo de casa e pulso quando alguém para nela.
/// Os materiais do projeto não são alterados: cada material original ganha UMA cópia em tempo de execução
/// com emissão ligada (compartilhada por todas as casas daquele tipo, então o SRP Batcher continua valendo).
/// Estrela e Carreira "respiram" devagar para chamar atenção. É só apresentação: não usa GameRng.
/// </summary>
public class CasaVisual : MonoBehaviour {
    // Intensidade da emissão em repouso, por tipo de casa (0 = sem brilho)
    static float BrilhoDoTipo(BoardSpace casa) {
        switch (casa) {
            case ClassicStarSpace _: return 0.55f;
            case CareerTransitionSpace _: return 0.45f;
            case QuestionSpace _: return 0.40f;
            case EducationSpace _: return 0.40f;
            case ItemSpace _: return 0.35f;
            case ItemShop _: return 0.30f;
            case BlueSpace _: return 0.30f;
            case RedSpace _: return 0.30f;
            default: return 0f; // casa vazia, início, bifurcação
        }
    }

    static bool Respira(BoardSpace casa) => casa is ClassicStarSpace || casa is CareerTransitionSpace;

    static readonly int IdEmissao = Shader.PropertyToID("_EmissionColor");
    static readonly int IdCorBase = Shader.PropertyToID("_BaseColor");

    // material original -> cópia com emissão
    static readonly Dictionary<Material, Material> copias = new Dictionary<Material, Material>();
    // cópias que "respiram" -> (cor base, brilho)
    static readonly Dictionary<Material, (Color cor, float brilho)> respirando = new Dictionary<Material, (Color, float)>();
    static CasaVisual atualizador;

    BoardSpace casa;
    readonly List<Renderer> renderizadores = new List<Renderer>();
    readonly List<Color> coresBase = new List<Color>();
    float brilho;
    Vector3 escalaBase;
    Coroutine pulso;
    MaterialPropertyBlock bloco;

    /// <summary>Chamado pelo BoardSpace ao iniciar.</summary>
    public static void Preparar(BoardSpace casa) {
        if (casa == null || casa.GetComponent<CasaVisual>() != null) return;
        var v = casa.gameObject.AddComponent<CasaVisual>();
        v.casa = casa;
        v.Configurar();
    }

    /// <summary>Pulso de "pousou aqui": cresce um pouco e brilha forte por meio segundo.</summary>
    public static void Pulsar(BoardSpace casa) {
        if (casa == null) return;
        var v = casa.GetComponent<CasaVisual>();
        if (v == null) { Preparar(casa); v = casa.GetComponent<CasaVisual>(); }
        if (v != null && v.isActiveAndEnabled) {
            if (v.pulso != null) v.StopCoroutine(v.pulso);
            v.pulso = v.StartCoroutine(v.AnimarPulso());
        }
    }

    void Configurar() {
        escalaBase = transform.localScale;
        brilho = BrilhoDoTipo(casa);
        foreach (var r in GetComponentsInChildren<Renderer>(true)) {
            var mats = r.sharedMaterials;
            bool usa = false;
            for (int i = 0; i < mats.Length; i++) {
                var m = mats[i];
                if (m == null || EhContorno(m)) continue;
                usa = true;
                mats[i] = CopiaComEmissao(m, brilho, Respira(casa)); // mesmo sem brilho: o pulso precisa da emissão ligada
            }
            if (!usa) continue;
            r.sharedMaterials = mats;
            renderizadores.Add(r);
            coresBase.Add(CorDe(r.sharedMaterial));
        }
        if (atualizador == null && respirando.Count > 0) {
            var go = new GameObject("[CasaVisual]");
            atualizador = go.AddComponent<CasaVisual>();
        }
    }

    static bool EhContorno(Material m) {
        string n = m.name;
        return n.StartsWith("Border") || n.Contains("Phantom");
    }

    static Color CorDe(Material m) {
        if (m == null) return Color.white;
        if (m.HasProperty(IdCorBase)) return m.GetColor(IdCorBase);
        return m.HasProperty("_Color") ? m.color : Color.white;
    }

    static Material CopiaComEmissao(Material original, float brilho, bool respira) {
        if (copias.TryGetValue(original, out Material copia) && copia != null) {
            if (respira && !respirando.ContainsKey(copia)) respirando[copia] = (CorDe(original), brilho);
            return copia;
        }
        copia = new Material(original) { name = original.name + " (brilho)" };
        Color cor = CorDe(original);
        copia.EnableKeyword("_EMISSION");
        copia.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        if (copia.HasProperty(IdEmissao)) copia.SetColor(IdEmissao, cor * brilho);
        copias[original] = copia;
        if (respira) respirando[copia] = (cor, brilho);
        return copia;
    }

    void Update() {
        // Só o objeto atualizador mexe na respiração (compartilhada por material)
        if (this != atualizador) return;
        float onda = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
        foreach (var kv in respirando) {
            if (kv.Key == null) continue;
            kv.Key.SetColor(IdEmissao, kv.Value.cor * (kv.Value.brilho * (0.6f + 0.9f * onda)));
        }
    }

    IEnumerator AnimarPulso() {
        if (bloco == null) bloco = new MaterialPropertyBlock();
        const float duracao = 0.55f;
        float t = 0f;
        while (t < duracao) {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracao);
            float s = Mathf.Sin(p * Mathf.PI);
            transform.localScale = escalaBase * (1f + 0.2f * s);
            float flash = Mathf.Max(brilho, 0.25f) + 3.2f * (1f - p);
            for (int i = 0; i < renderizadores.Count; i++) {
                var r = renderizadores[i];
                if (r == null) continue;
                r.GetPropertyBlock(bloco);
                bloco.SetColor(IdEmissao, coresBase[i] * flash);
                r.SetPropertyBlock(bloco);
            }
            yield return null;
        }
        transform.localScale = escalaBase;
        // Sem bloco de propriedades: volta ao material compartilhado (e ao SRP Batcher)
        foreach (var r in renderizadores) if (r != null) r.SetPropertyBlock(null);
        pulso = null;
    }

    void OnDestroy() {
        if (atualizador == this) {
            atualizador = null;
            respirando.Clear();
        }
    }
}
