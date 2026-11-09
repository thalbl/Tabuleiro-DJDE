using System.Collections.Generic;

/// <summary>
/// Gerador aleatório da PARTIDA (resultados que mudam o estado do jogo).
///
/// Online é lockstep: só dados de humanos e escolhas trafegam pela rede, e cada máquina
/// simula o resto. Por isso todo sorteio que afeta o estado (profissão, bônus, penalidades,
/// quiz, itens, Ventania, dados de bot...) precisa sair DESTE gerador, semeado com a mesma
/// seed em todas as máquinas (o host envia no LobbyStartGame).
///
/// Use UnityEngine.Random só para efeitos puramente visuais (rotação de moeda, partículas).
/// </summary>
public static class GameRng {
    private static System.Random rng = new System.Random();

    public static int Seed { get; private set; }

    public static void Init(int seed) {
        Seed = seed;
        rng = new System.Random(seed);
    }

    public static int NovaSeed() => new System.Random().Next(1, int.MaxValue);

    /// <summary>Inteiro em [min, max) — mesma semântica de UnityEngine.Random.Range(int, int).</summary>
    public static int Range(int minInclusive, int maxExclusive) {
        if (maxExclusive <= minInclusive) return minInclusive;
        return rng.Next(minInclusive, maxExclusive);
    }

    /// <summary>Float em [min, max].</summary>
    public static float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

    /// <summary>Sorteia um elemento (lista não pode estar vazia).</summary>
    public static T Escolher<T>(IList<T> lista) => lista[Range(0, lista.Count)];

    /// <summary>Embaralha a lista no lugar (Fisher-Yates).</summary>
    public static void Embaralhar<T>(IList<T> lista) {
        for (int i = lista.Count - 1; i > 0; i--) {
            int j = Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }
}
