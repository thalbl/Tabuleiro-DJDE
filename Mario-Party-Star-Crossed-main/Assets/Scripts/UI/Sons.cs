using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efeitos sonoros do jogo sintetizados em código (arte sonora 100% original, sem arquivos).
/// Os clipes são gerados uma vez, na primeira vez que tocam. O volume segue "Efeitos sonoros"
/// do menu de Configurações (o volume geral já vem do AudioListener).
/// Nada aqui usa GameRng: é só apresentação, cada máquina toca o seu.
/// </summary>
public static class Sons {
    public enum Efeito { Acerto, Erro, Estrela, Promocao, Formatura, BolsaAlta, BolsaBaixa, Ganho, Perda }

    const int Taxa = 44100;
    const float VolumeBase = 0.8f;

    static AudioSource fonte;
    static readonly Dictionary<Efeito, AudioClip> clipes = new Dictionary<Efeito, AudioClip>();

    public static void Tocar(Efeito efeito, float volume = 1f) {
        if (!Application.isPlaying) return;
        if (fonte == null) {
            var go = new GameObject("[Sons]");
            Object.DontDestroyOnLoad(go);
            fonte = go.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.loop = false;
            fonte.spatialBlend = 0f;
        }
        if (!clipes.TryGetValue(efeito, out AudioClip clipe) || clipe == null) {
            clipe = Gerar(efeito);
            clipes[efeito] = clipe;
        }
        fonte.volume = VolumeBase * Configuracoes.VolumeEfeitos;
        fonte.PlayOneShot(clipe, Mathf.Clamp01(volume));
    }

    // ------------------------------------------------------------------ síntese
    enum Onda { Seno, Triangulo, Quadrada }

    static AudioClip Gerar(Efeito efeito) {
        float[] buf;
        switch (efeito) {
            case Efeito.Acerto:
                buf = Buffer(0.75f);
                Sino(buf, 0.00f, Nota(72), 0.45f, 0.35f);   // C5
                Sino(buf, 0.08f, Nota(76), 0.45f, 0.35f);   // E5
                Sino(buf, 0.16f, Nota(79), 0.55f, 0.45f);   // G5
                Sino(buf, 0.16f, Nota(84), 0.55f, 0.20f);   // C6 (brilho)
                break;
            case Efeito.Erro:
                buf = Buffer(0.6f);
                Tom(buf, 0.00f, 0.16f, Nota(64), Nota(63), Onda.Triangulo, 0.35f, 0.01f);
                Tom(buf, 0.15f, 0.38f, Nota(60), Nota(57), Onda.Triangulo, 0.35f, 0.01f);
                break;
            case Efeito.Estrela:
                buf = Buffer(1.1f);
                int[] arpejo = { 79, 84, 88, 91, 96 };
                for (int i = 0; i < arpejo.Length; i++) Sino(buf, i * 0.055f, Nota(arpejo[i]), 0.6f, 0.28f);
                for (int i = 0; i < 6; i++) Sino(buf, 0.3f + i * 0.07f, Nota(96 + (i % 2 == 0 ? 0 : 3)), 0.25f, 0.08f);
                break;
            case Efeito.Promocao:
                buf = Buffer(1.2f);
                Tom(buf, 0.00f, 0.12f, Nota(67), Nota(67), Onda.Quadrada, 0.14f, 0.005f);
                Tom(buf, 0.12f, 0.12f, Nota(72), Nota(72), Onda.Quadrada, 0.14f, 0.005f);
                Tom(buf, 0.24f, 0.12f, Nota(76), Nota(76), Onda.Quadrada, 0.14f, 0.005f);
                Tom(buf, 0.36f, 0.60f, Nota(79), Nota(79), Onda.Quadrada, 0.14f, 0.005f);
                Tom(buf, 0.36f, 0.60f, Nota(72), Nota(72), Onda.Triangulo, 0.22f, 0.005f);
                Sino(buf, 0.36f, Nota(84), 0.7f, 0.25f);
                break;
            case Efeito.Formatura:
                buf = Buffer(1.2f);
                Sino(buf, 0.00f, Nota(72), 0.5f, 0.3f);
                Sino(buf, 0.14f, Nota(79), 0.5f, 0.3f);
                Sino(buf, 0.28f, Nota(76), 0.5f, 0.3f);
                Sino(buf, 0.42f, Nota(84), 0.75f, 0.4f);
                Sino(buf, 0.42f, Nota(76), 0.75f, 0.2f);
                break;
            case Efeito.BolsaAlta:
                buf = Buffer(0.5f);
                Tom(buf, 0.00f, 0.22f, 520f, 1040f, Onda.Seno, 0.3f, 0.01f);
                Sino(buf, 0.20f, Nota(84), 0.3f, 0.25f);
                break;
            case Efeito.BolsaBaixa:
                buf = Buffer(0.5f);
                Tom(buf, 0.00f, 0.32f, 700f, 330f, Onda.Triangulo, 0.3f, 0.01f);
                break;
            case Efeito.Ganho:
                buf = Buffer(0.3f);
                Sino(buf, 0.00f, Nota(88), 0.2f, 0.25f);
                Sino(buf, 0.05f, Nota(93), 0.25f, 0.25f);
                break;
            default: // Perda
                buf = Buffer(0.3f);
                Tom(buf, 0.00f, 0.18f, Nota(55), Nota(52), Onda.Triangulo, 0.3f, 0.005f);
                break;
        }
        Normalizar(buf, 0.9f);
        var clipe = AudioClip.Create("Som_" + efeito, buf.Length, 1, Taxa, false);
        clipe.SetData(buf, 0);
        return clipe;
    }

    static float Nota(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    static float[] Buffer(float segundos) => new float[Mathf.CeilToInt(segundos * Taxa)];

    /// <summary>Sino: seno com harmônicos e decaimento exponencial.</summary>
    static void Sino(float[] buf, float inicio, float freq, float duracao, float volume) {
        int i0 = Mathf.RoundToInt(inicio * Taxa);
        int n = Mathf.Min(buf.Length - i0, Mathf.RoundToInt(duracao * Taxa));
        for (int i = 0; i < n; i++) {
            float t = i / (float)Taxa;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Exp(-t * 6f / duracao);
            float fase = 2f * Mathf.PI * freq * t;
            buf[i0 + i] += volume * env * (Mathf.Sin(fase) + 0.35f * Mathf.Sin(2f * fase) + 0.12f * Mathf.Sin(3f * fase));
        }
    }

    /// <summary>Tom com glissando linear de freqInicial a freqFinal e envelope ataque/sustentação/soltura.</summary>
    static void Tom(float[] buf, float inicio, float duracao, float freqInicial, float freqFinal, Onda onda, float volume, float ataque) {
        int i0 = Mathf.RoundToInt(inicio * Taxa);
        int n = Mathf.Min(buf.Length - i0, Mathf.RoundToInt(duracao * Taxa));
        float fase = 0f;
        float soltura = Mathf.Min(0.08f, duracao * 0.5f);
        for (int i = 0; i < n; i++) {
            float t = i / (float)Taxa;
            float p = t / duracao;
            float freq = Mathf.Lerp(freqInicial, freqFinal, p);
            fase += freq / Taxa;
            fase -= Mathf.Floor(fase);
            float env = Mathf.Min(1f, t / Mathf.Max(0.001f, ataque)) * Mathf.Clamp01((duracao - t) / soltura);
            float s;
            switch (onda) {
                case Onda.Triangulo: s = 1f - 4f * Mathf.Abs(fase - 0.5f); break;
                case Onda.Quadrada: s = fase < 0.5f ? 0.8f : -0.8f; break;
                default: s = Mathf.Sin(2f * Mathf.PI * fase); break;
            }
            buf[i0 + i] += volume * env * s;
        }
    }

    static void Normalizar(float[] buf, float pico) {
        float max = 0f;
        foreach (float s in buf) max = Mathf.Max(max, Mathf.Abs(s));
        if (max <= 0f) return;
        float k = Mathf.Min(1f, pico / max) ;
        if (max > pico) for (int i = 0; i < buf.Length; i++) buf[i] *= k;
    }
}
