using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gera os ícones do jogo (itens, Estrela, Moeda, selos de movimento e textura da casa de item)
/// de forma procedural, com campos de distância (SDF): arte 100% original do projeto.
/// Os arquivos são sobrescritos com o MESMO nome, então cenas e código continuam apontando para eles.
/// Menu: Ferramentas > Gerar Ícones Procedurais (ou IconesProcedurais.GerarTodos()).
/// </summary>
public static class IconesProcedurais {
    const string PastaIcones = "Assets/Resources/texture/icons/";
    const string PastaSelos = "Assets/Resources/texture/itemStamps/";
    const string TexturaCasaItem = "Assets/Resources/texture/space/item.png";

    // ------------------------------------------------------------------ Tela SDF
    delegate float Sdf(Vector2 p);

    class Tela {
        public readonly int N;
        readonly Color[] px;
        public Tela(int n, Color fundo) { N = n; px = new Color[n * n]; for (int i = 0; i < px.Length; i++) px[i] = fundo; }

        float Pixel => 2f / N;

        public void Pintar(Sdf f, Func<Vector2, Color> cor, float suavidade = 1f) {
            float largura = Pixel * suavidade;
            for (int y = 0; y < N; y++) {
                for (int x = 0; x < N; x++) {
                    Vector2 p = new Vector2((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f);
                    float d = f(p);
                    if (d > largura) continue;
                    float cob = Mathf.Clamp01(0.5f - d / largura);
                    if (cob <= 0f) continue;
                    Color c = cor(p);
                    float a = c.a * cob;
                    int i = y * N + x;
                    Color baixo = px[i];
                    float aOut = a + baixo.a * (1f - a);
                    if (aOut <= 0f) continue;
                    Color r = (c * a + baixo * baixo.a * (1f - a)) / aOut;
                    r.a = aOut;
                    px[i] = r;
                }
            }
        }

        public void Pintar(Sdf f, Color cor, float suavidade = 1f) => Pintar(f, _ => cor, suavidade);

        public Texture2D ParaTextura() {
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    // ------------------------------------------------------------------ primitivas
    static Sdf Circulo(float cx, float cy, float r) => p => Vector2.Distance(p, new Vector2(cx, cy)) - r;
    static Sdf Elipse(float cx, float cy, float rx, float ry) => p => {
        Vector2 q = new Vector2((p.x - cx) / rx, (p.y - cy) / ry);
        return (q.magnitude - 1f) * Mathf.Min(rx, ry);
    };
    static Sdf Caixa(float cx, float cy, float hx, float hy, float raio = 0f) => p => {
        Vector2 q = new Vector2(Mathf.Abs(p.x - cx) - hx + raio, Mathf.Abs(p.y - cy) - hy + raio);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - raio;
    };
    static Sdf Capsula(float ax, float ay, float bx, float by, float r) => p => {
        Vector2 a = new Vector2(ax, ay), b = new Vector2(bx, by);
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    };
    static Sdf Poligono(params float[] xy) {
        int n = xy.Length / 2;
        var v = new Vector2[n];
        for (int i = 0; i < n; i++) v[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
        return p => {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++) {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        };
    }
    static Sdf Estrela(float cx, float cy, float rExt, float rInt, int pontas = 5, float giro = 90f) {
        var xy = new float[pontas * 4];
        for (int i = 0; i < pontas * 2; i++) {
            float ang = (giro + i * 180f / pontas) * Mathf.Deg2Rad;
            float r = i % 2 == 0 ? rExt : rInt;
            xy[2 * i] = cx + Mathf.Cos(ang) * r;
            xy[2 * i + 1] = cy + Mathf.Sin(ang) * r;
        }
        return Poligono(xy);
    }
    static Sdf Anel(float cx, float cy, float r, float espessura) { var c = Circulo(cx, cy, r); return p => Mathf.Abs(c(p)) - espessura; }
    static Sdf Uniao(params Sdf[] fs) => p => { float d = float.MaxValue; foreach (var f in fs) d = Mathf.Min(d, f(p)); return d; };
    static Sdf Menos(Sdf a, Sdf b) => p => Mathf.Max(a(p), -b(p));
    static Sdf Intersecao(Sdf a, Sdf b) => p => Mathf.Max(a(p), b(p));
    static Sdf Arredondar(Sdf a, float r) => p => a(p) - r;
    static Sdf Girar(Sdf a, float graus, float cx = 0, float cy = 0) {
        float c = Mathf.Cos(-graus * Mathf.Deg2Rad), s = Mathf.Sin(-graus * Mathf.Deg2Rad);
        return p => { Vector2 q = p - new Vector2(cx, cy); return a(new Vector2(q.x * c - q.y * s, q.x * s + q.y * c) + new Vector2(cx, cy)); };
    }
    static Sdf Escalar(Sdf a, float k, float cx = 0, float cy = 0) => p => a((p - new Vector2(cx, cy)) / k + new Vector2(cx, cy)) * k;
    static Sdf Mover(Sdf a, float dx, float dy) => p => a(p - new Vector2(dx, dy));

    // ------------------------------------------------------------------ estilo
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out Color c); return c; }
    static Color Escurecer(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    static Color Clarear(Color c, float k) => Color.Lerp(c, Color.white, k);

    /// <summary>Peça principal: sombra suave, contorno escuro, preenchimento com degradê e brilho.</summary>
    static void Peca(Tela t, Sdf f, Color cor, float contorno = 0.028f, bool brilho = true) {
        t.Pintar(Mover(Arredondar(f, contorno), 0.025f, -0.04f), new Color(0, 0, 0, 0.22f), 9f);
        t.Pintar(Arredondar(f, contorno), Escurecer(cor, 0.5f));
        t.Pintar(f, p => Color.Lerp(Escurecer(cor, 0.82f), Clarear(cor, 0.18f), Mathf.Clamp01((p.y + 0.9f) / 1.8f)));
        if (brilho) t.Pintar(Intersecao(f, Mover(Arredondar(f, -0.06f), -0.04f, 0.05f)), new Color(1, 1, 1, 0.10f));
    }

    /// <summary>Detalhe sem sombra (com contorno fino opcional).</summary>
    static void Detalhe(Tela t, Sdf f, Color cor, float contorno = 0f) {
        if (contorno > 0f) t.Pintar(Arredondar(f, contorno), Escurecer(cor, 0.5f));
        t.Pintar(f, cor);
    }

    static void Brilho(Tela t, float cx, float cy, float r) {
        t.Pintar(Estrela(cx, cy, r, r * 0.28f, 4, 90f), new Color(1, 1, 1, 0.95f));
    }

    // ------------------------------------------------------------------ paleta
    static readonly Color Ouro = Hex("#F5B82E");
    static readonly Color OuroClaro = Hex("#FFE07A");
    static readonly Color Rosa = Hex("#F28DB2");
    static readonly Color Vermelho = Hex("#E2453C");
    static readonly Color Azul = Hex("#3B82D9");
    static readonly Color AzulClaro = Hex("#8FD3F4");
    static readonly Color Verde = Hex("#3DAA6D");
    static readonly Color Roxo = Hex("#8A5CD6");
    static readonly Color Marrom = Hex("#9A5B2E");
    static readonly Color Grafite = Hex("#3A3F4B");
    static readonly Color Branco = Hex("#F7F7F2");
    static readonly Color Laranja = Hex("#F28C28");

    // ------------------------------------------------------------------ desenhos
    static void Cofrinho(Tela t) {
        Sdf corpo = Elipse(-0.05f, -0.08f, 0.66f, 0.5f);
        Sdf orelha = Poligono(-0.42f, 0.25f, -0.2f, 0.62f, -0.05f, 0.32f);
        Sdf focinho = Elipse(0.6f, -0.05f, 0.16f, 0.2f);
        Sdf pernas = Uniao(Caixa(-0.38f, -0.6f, 0.1f, 0.14f, 0.05f), Caixa(0.22f, -0.6f, 0.1f, 0.14f, 0.05f));
        Sdf rabo = Anel(-0.76f, 0.02f, 0.08f, 0.025f);
        Peca(t, Uniao(corpo, orelha, focinho, pernas, Intersecao(rabo, Caixa(-0.8f, 0.02f, 0.06f, 0.12f))), Rosa);
        Detalhe(t, Caixa(-0.05f, 0.37f, 0.18f, 0.035f, 0.03f), Escurecer(Rosa, 0.45f));
        Detalhe(t, Circulo(0.56f, -0.01f, 0.035f), Escurecer(Rosa, 0.45f));
        Detalhe(t, Circulo(0.66f, -0.09f, 0.035f), Escurecer(Rosa, 0.45f));
        Detalhe(t, Circulo(0.28f, 0.12f, 0.045f), Grafite);
        Peca(t, Circulo(-0.05f, 0.66f, 0.2f), Ouro, 0.022f);
        Detalhe(t, Anel(-0.05f, 0.66f, 0.12f, 0.018f), OuroClaro);
    }

    static void Dado(Tela t, Color cor, Color pinta, int pintas) {
        Peca(t, Caixa(0, -0.02f, 0.6f, 0.6f, 0.16f), cor);
        var pos = new Dictionary<int, Vector2[]> {
            {3, new[]{ new Vector2(-0.3f, 0.28f), new Vector2(0, -0.02f), new Vector2(0.3f, -0.32f) }},
            {5, new[]{ new Vector2(-0.3f, 0.28f), new Vector2(0.3f, 0.28f), new Vector2(0, -0.02f), new Vector2(-0.3f, -0.32f), new Vector2(0.3f, -0.32f) }},
        };
        foreach (var v in pos[pintas]) Detalhe(t, Circulo(v.x, v.y, 0.09f), pinta);
    }

    static void SuperDado(Tela t) {
        Dado(t, Ouro, Hex("#7A4A0E"), 5);
        Brilho(t, 0.62f, 0.66f, 0.2f);
        Brilho(t, -0.7f, -0.6f, 0.13f);
    }

    static void Foguete(Tela t) {
        Sdf corpo = Uniao(Elipse(0, 0.05f, 0.24f, 0.55f), Caixa(0, -0.25f, 0.24f, 0.25f, 0.05f));
        Sdf aletas = Uniao(Poligono(-0.24f, -0.15f, -0.48f, -0.52f, -0.22f, -0.42f), Poligono(0.24f, -0.15f, 0.48f, -0.52f, 0.22f, -0.42f));
        Sdf chama = Poligono(-0.15f, -0.5f, 0.15f, -0.5f, 0.0f, -0.92f);
        Sdf g(Sdf s) => Girar(s, -40f);
        Peca(t, g(Arredondar(chama, 0.04f)), Laranja, 0.02f, false);
        Detalhe(t, g(Escalar(chama, 0.55f, 0, -0.5f)), OuroClaro);
        Peca(t, g(aletas), Vermelho);
        Peca(t, g(corpo), Branco);
        Detalhe(t, g(Intersecao(corpo, Caixa(0, 0.45f, 0.3f, 0.2f))), Vermelho);
        Peca(t, g(Circulo(0, 0.08f, 0.11f)), AzulClaro, 0.03f);
    }

    static void BuracoNoBolso(Tela t) {
        Sdf bolso = Poligono(-0.55f, 0.55f, 0.55f, 0.55f, 0.5f, -0.25f, 0f, -0.55f, -0.5f, -0.25f);
        Peca(t, Arredondar(bolso, 0.05f), Azul);
        Detalhe(t, Menos(Arredondar(bolso, -0.06f), Arredondar(bolso, -0.085f)), new Color(1f, 0.85f, 0.4f, 0.8f));
        Detalhe(t, Caixa(0, 0.5f, 0.6f, 0.06f, 0.03f), Escurecer(Azul, 0.6f));
        Detalhe(t, Elipse(0.05f, -0.12f, 0.22f, 0.13f), Hex("#14161C"));
        Peca(t, Elipse(0.38f, -0.75f, 0.17f, 0.12f), Ouro, 0.02f);
    }

    static void ChaveSecreta(Tela t) {
        Sdf chave = Uniao(
            Anel(-0.42f, 0.18f, 0.26f, 0.1f),
            Capsula(-0.15f, 0.0f, 0.62f, -0.42f, 0.09f),
            Girar(Caixa(0.38f, -0.42f, 0.06f, 0.12f), -29f, 0.38f, -0.28f),
            Girar(Caixa(0.55f, -0.52f, 0.06f, 0.12f), -29f, 0.55f, -0.38f));
        Peca(t, chave, Ouro);
        Brilho(t, -0.55f, 0.55f, 0.14f);
    }

    static void Portal(Tela t) {
        Peca(t, Circulo(0, 0, 0.72f), Roxo);
        Detalhe(t, Anel(0, 0, 0.52f, 0.06f), Clarear(Roxo, 0.35f));
        Detalhe(t, Anel(0, 0, 0.33f, 0.05f), Clarear(Roxo, 0.6f));
        Detalhe(t, Circulo(0, 0, 0.16f), Hex("#1B1035"));
        Brilho(t, 0.5f, 0.55f, 0.18f);
        Brilho(t, -0.58f, -0.45f, 0.12f);
    }

    static void Bau(Tela t) {
        Peca(t, Caixa(0, -0.22f, 0.66f, 0.38f, 0.08f), Marrom);
        Peca(t, Intersecao(Elipse(0, 0.16f, 0.66f, 0.42f), Caixa(0, 0.38f, 0.7f, 0.22f)), Escurecer(Marrom, 1.1f));
        Detalhe(t, Caixa(-0.42f, 0.0f, 0.07f, 0.62f), Ouro, 0.015f);
        Detalhe(t, Caixa(0.42f, 0.0f, 0.07f, 0.62f), Ouro, 0.015f);
        Peca(t, Caixa(0, 0.12f, 0.13f, 0.15f, 0.05f), Ouro, 0.02f);
        Detalhe(t, Capsula(0, 0.12f, 0, 0.04f, 0.03f), Hex("#5A3A0A"));
    }

    static void BebidaDourada(Tela t) {
        Sdf copo = Poligono(-0.42f, 0.6f, 0.42f, 0.6f, 0.28f, -0.68f, -0.28f, -0.68f);
        Peca(t, Capsula(0.15f, 0.25f, 0.42f, 0.88f, 0.05f), Vermelho, 0.02f, false);
        Peca(t, copo, new Color(0.85f, 0.95f, 1f, 1f));
        Detalhe(t, Intersecao(Arredondar(copo, -0.05f), Caixa(0, -0.28f, 0.6f, 0.45f)), Ouro);
        Detalhe(t, Intersecao(Arredondar(copo, -0.05f), Caixa(0, 0.16f, 0.6f, 0.035f)), OuroClaro);
        Detalhe(t, Circulo(-0.08f, -0.25f, 0.05f), OuroClaro);
        Detalhe(t, Circulo(0.1f, -0.45f, 0.035f), OuroClaro);
        Detalhe(t, Circulo(0.02f, -0.05f, 0.03f), OuroClaro);
    }

    static void Mochila(Tela t) {
        Peca(t, Intersecao(Anel(0, 0.52f, 0.2f, 0.06f), Caixa(0, 0.72f, 0.3f, 0.2f)), Escurecer(Verde, 0.7f), 0.02f, false);
        Peca(t, Caixa(0, -0.05f, 0.52f, 0.62f, 0.22f), Verde);
        Peca(t, Caixa(0, -0.3f, 0.36f, 0.24f, 0.1f), Escurecer(Verde, 0.85f), 0.02f);
        Detalhe(t, Caixa(0, -0.12f, 0.3f, 0.025f), Escurecer(Verde, 0.55f));
        Detalhe(t, Circulo(0, -0.12f, 0.05f), Ouro);
        Brilho(t, 0.55f, 0.6f, 0.17f);
    }

    static void Espiao(Tela t) {
        Color chapeu = Hex("#4B4F5C");
        Peca(t, Elipse(0, 0.12f, 0.78f, 0.15f), chapeu);
        Peca(t, Caixa(0, 0.42f, 0.42f, 0.3f, 0.16f), chapeu);
        Detalhe(t, Caixa(0, 0.22f, 0.42f, 0.07f), Vermelho);
        Peca(t, Caixa(-0.27f, -0.32f, 0.23f, 0.16f, 0.1f), Hex("#1A1C22"), 0.02f);
        Peca(t, Caixa(0.27f, -0.32f, 0.23f, 0.16f, 0.1f), Hex("#1A1C22"), 0.02f);
        Detalhe(t, Capsula(-0.06f, -0.28f, 0.06f, -0.28f, 0.03f), Hex("#1A1C22"));
        Detalhe(t, Capsula(-0.36f, -0.24f, -0.24f, -0.24f, 0.03f), new Color(1, 1, 1, 0.55f));
        Detalhe(t, Capsula(0.18f, -0.24f, 0.3f, -0.24f, 0.03f), new Color(1, 1, 1, 0.55f));
    }

    static void Ventania(Tela t) {
        Color vento = AzulClaro;
        // Linha reta + meia-volta para cima no fim (gancho aberto, não um laço fechado)
        void Rajada(float y, float x0, float x1, float rc) {
            Sdf meiaVolta = Intersecao(Anel(x1, y + rc, rc, 0.07f), p => x1 - p.x);
            Sdf ponta = Capsula(x1, y + 2f * rc, x1 - rc * 0.6f, y + 2f * rc, 0.07f);
            Peca(t, Uniao(Capsula(x0, y, x1, y, 0.07f), meiaVolta, ponta), vento, 0.025f, false);
        }
        Rajada(0.42f, -0.72f, 0.3f, 0.16f);
        Rajada(0.0f, -0.6f, 0.5f, 0.2f);
        Rajada(-0.42f, -0.76f, 0.2f, 0.14f);
    }

    static void Duelo(Tela t) {
        Peca(t, Circulo(0, 0, 0.72f), Vermelho);
        Detalhe(t, Anel(0, 0, 0.58f, 0.03f), Clarear(Vermelho, 0.4f));
        Peca(t, Poligono(0.12f, 0.58f, -0.32f, -0.04f, -0.02f, -0.04f, -0.14f, -0.58f, 0.34f, 0.08f, 0.04f, 0.08f), OuroClaro, 0.025f);
    }

    static void AgenciaCobranca(Tela t) {
        Peca(t, Caixa(-0.08f, 0.04f, 0.5f, 0.68f, 0.07f), Branco);
        for (int i = 0; i < 4; i++) Detalhe(t, Capsula(-0.38f, 0.4f - i * 0.2f, 0.22f - (i % 2) * 0.12f, 0.4f - i * 0.2f, 0.03f), Hex("#B8BCC6"));
        Peca(t, Circulo(0.4f, -0.42f, 0.3f), Vermelho);
        Detalhe(t, Capsula(0.4f, -0.3f, 0.4f, -0.45f, 0.05f), Branco);
        Detalhe(t, Circulo(0.4f, -0.58f, 0.05f), Branco);
    }

    static void Escudo(Tela t) {
        Sdf escudo = Arredondar(Poligono(-0.55f, 0.62f, 0.55f, 0.62f, 0.55f, 0.05f, 0f, -0.68f, -0.55f, 0.05f), 0.06f);
        Peca(t, escudo, Azul);
        Detalhe(t, Menos(Arredondar(escudo, -0.07f), Arredondar(escudo, -0.11f)), Clarear(Azul, 0.45f));
        Detalhe(t, Uniao(Capsula(-0.22f, 0.02f, -0.05f, -0.18f, 0.07f), Capsula(-0.05f, -0.18f, 0.28f, 0.28f, 0.07f)), Branco);
    }

    static void Lampada(Tela t) {
        Sdf corpo = Intersecao(Elipse(-0.05f, -0.15f, 0.55f, 0.36f), Caixa(0, -0.3f, 0.7f, 0.5f));
        Sdf bico = Poligono(0.35f, -0.05f, 0.82f, 0.18f, 0.88f, 0.12f, 0.42f, -0.28f);
        Sdf alca = Intersecao(Anel(-0.6f, -0.12f, 0.17f, 0.05f), Caixa(-0.75f, -0.12f, 0.18f, 0.3f));
        Peca(t, Uniao(corpo, bico, alca, Caixa(-0.05f, -0.5f, 0.3f, 0.06f, 0.03f)), Ouro);
        Peca(t, Elipse(-0.05f, 0.08f, 0.24f, 0.08f), Escurecer(Ouro, 0.9f), 0.02f);
        Peca(t, Circulo(-0.05f, 0.18f, 0.07f), OuroClaro, 0.02f);
        Brilho(t, 0.5f, 0.55f, 0.17f);
        Brilho(t, 0.15f, 0.72f, 0.11f);
    }

    static void TrajeExecutivo(Tela t) {
        Color terno = Hex("#2D3A5A");
        Peca(t, Caixa(0, -0.08f, 0.62f, 0.68f, 0.2f), terno);
        Detalhe(t, Poligono(-0.3f, 0.6f, 0.3f, 0.6f, 0f, -0.15f), Branco);
        Detalhe(t, Poligono(-0.3f, 0.6f, -0.02f, 0.38f, -0.18f, 0.25f), Hex("#D9DCE3"));
        Detalhe(t, Poligono(0.3f, 0.6f, 0.02f, 0.38f, 0.18f, 0.25f), Hex("#D9DCE3"));
        Peca(t, Poligono(-0.08f, 0.42f, 0.08f, 0.42f, 0.05f, 0.3f, -0.05f, 0.3f), Vermelho, 0.015f, false);
        Peca(t, Poligono(-0.05f, 0.3f, 0.05f, 0.3f, 0.11f, -0.25f, 0f, -0.38f, -0.11f, -0.25f), Vermelho, 0.015f, false);
    }

    static void MoedaDesenho(Tela t, float cx, float cy, float r) {
        Peca(t, Circulo(cx, cy, r), Ouro, 0.03f);
        Detalhe(t, Anel(cx, cy, r * 0.74f, r * 0.05f), Escurecer(Ouro, 0.75f));
        Detalhe(t, Estrela(cx, cy - r * 0.02f, r * 0.42f, r * 0.19f), OuroClaro, 0.0f);
    }

    static void MoedaDobrada(Tela t) {
        MoedaDesenho(t, -0.22f, -0.18f, 0.5f);
        MoedaDesenho(t, 0.25f, 0.22f, 0.5f);
        Brilho(t, 0.66f, 0.68f, 0.15f);
    }

    static void Presente(Tela t) {
        Peca(t, Caixa(0, -0.25f, 0.55f, 0.42f, 0.06f), Vermelho);
        Peca(t, Caixa(0, 0.24f, 0.64f, 0.13f, 0.05f), Escurecer(Vermelho, 1.05f));
        Detalhe(t, Caixa(0, -0.12f, 0.1f, 0.55f), Ouro, 0.015f);
        Peca(t, Uniao(Elipse(-0.2f, 0.5f, 0.22f, 0.13f), Elipse(0.2f, 0.5f, 0.22f, 0.13f)), Ouro, 0.022f);
        Detalhe(t, Circulo(0, 0.46f, 0.08f), OuroClaro, 0.015f);
    }

    static void Bilhete(Tela t) {
        Sdf bilhete = Menos(Menos(Caixa(0, 0, 0.78f, 0.45f, 0.08f), Circulo(-0.78f, 0, 0.16f)), Circulo(0.78f, 0, 0.16f));
        Peca(t, Girar(bilhete, 12f), Laranja);
        for (int i = -3; i <= 3; i++) Detalhe(t, Girar(Caixa(0.36f, i * 0.12f, 0.018f, 0.035f), 12f), Clarear(Laranja, 0.55f));
        Detalhe(t, Girar(Estrela(-0.18f, 0f, 0.3f, 0.13f), 12f), OuroClaro, 0.02f);
    }

    static void EstrelaGrande(Tela t) {
        Sdf e = Arredondar(Estrela(0, -0.04f, 0.78f, 0.36f), 0.06f);
        Peca(t, e, Ouro, 0.035f);
        Detalhe(t, Arredondar(Estrela(-0.02f, 0.0f, 0.42f, 0.2f), 0.03f), new Color(1, 1, 1, 0.16f));
        Brilho(t, 0.52f, 0.62f, 0.18f);
    }

    static void MoedaGrande(Tela t) {
        MoedaDesenho(t, 0, 0, 0.78f);
        Brilho(t, 0.52f, 0.6f, 0.16f);
    }

    static void CasaDeItem(Tela t) {
        Detalhe(t, Caixa(0, 0, 1f, 1f), Hex("#6C4AB6"));
        Detalhe(t, Menos(Caixa(0, 0, 0.9f, 0.9f, 0.12f), Caixa(0, 0, 0.82f, 0.82f, 0.09f)), Clarear(Hex("#6C4AB6"), 0.35f));
        // Presente branco no centro
        Detalhe(t, Caixa(0, -0.2f, 0.45f, 0.36f, 0.05f), Branco);
        Detalhe(t, Caixa(0, 0.24f, 0.52f, 0.11f, 0.04f), Branco);
        Detalhe(t, Caixa(0, -0.08f, 0.08f, 0.46f), Hex("#6C4AB6"));
        Detalhe(t, Uniao(Elipse(-0.17f, 0.48f, 0.18f, 0.11f), Elipse(0.17f, 0.48f, 0.18f, 0.11f)), Branco);
    }

    // ------------------------------------------------------------------ selos de movimento
    static void Selo(Tela t, Color cor, Action<Tela> desenho, int n) {
        t.Pintar(Circulo(0, 0, 0.94f), cor);
        t.Pintar(Circulo(0, 0, 0.8f), Branco);
        var interno = new Tela(n, new Color(0, 0, 0, 0));
        desenho(interno);
        // compõe o ícone a 72% do tamanho no centro do selo
        var tex = interno.ParaTextura();
        var px = tex.GetPixels();
        UnityEngine.Object.DestroyImmediate(tex);
        t.Pintar(p => {
            Vector2 q = p / 0.72f;
            if (Mathf.Abs(q.x) > 1f || Mathf.Abs(q.y) > 1f) return 1f;
            int x = Mathf.Clamp((int)((q.x + 1f) / 2f * n), 0, n - 1), y = Mathf.Clamp((int)((q.y + 1f) / 2f * n), 0, n - 1);
            return px[y * n + x].a > 0.01f ? -1f : 1f;
        }, p => {
            Vector2 q = p / 0.72f;
            int x = Mathf.Clamp((int)((q.x + 1f) / 2f * n), 0, n - 1), y = Mathf.Clamp((int)((q.y + 1f) / 2f * n), 0, n - 1);
            return px[y * n + x];
        }, 0.001f);
    }

    // ------------------------------------------------------------------ geração
    static void Salvar(Tela t, string caminho) {
        var tex = t.ParaTextura();
        File.WriteAllBytes(caminho, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static string Gerar(string caminho, int n, Action<Tela> desenho, bool fundoTransparente = true) {
        var t = new Tela(n, fundoTransparente ? new Color(0, 0, 0, 0) : Color.white);
        desenho(t);
        Salvar(t, caminho);
        return Path.GetFileName(caminho);
    }

    [MenuItem("Ferramentas/Gerar Ícones Procedurais")]
    public static string GerarTodos() {
        const int n = 512;
        var feitos = new List<string>();
        var itens = new Dictionary<string, Action<Tela>> {
            {"Mushroom", Cofrinho}, {"GoldenMushroom", SuperDado}, {"MagicMushroom", Foguete},
            {"PoisonMushroom", BuracoNoBolso}, {"SkeletonKey", ChaveSecreta}, {"WarpPipe", Portal},
            {"PlunderChest", Bau}, {"GoldenDrink", BebidaDourada}, {"VacPack", Mochila},
            {"BooBell", Espiao}, {"TweesterTotem", Ventania}, {"DuelingGlove", Duelo},
            {"ChompCall", AgenciaCobranca}, {"Gaddlight", Escudo}, {"MagicLamp", Lampada},
            {"BowserSuit", TrajeExecutivo}, {"DoubleStarCard", MoedaDobrada}, {"ChompTreat", Presente},
            {"WigglerWhistle", Bilhete}, {"star", EstrelaGrande}, {"coin", MoedaGrande},
        };
        foreach (var kv in itens) feitos.Add(Gerar(PastaIcones + kv.Key + ".png", n, kv.Value));

        var selos = new Dictionary<string, (Color, Action<Tela>)> {
            {"mushroom", (Rosa, Cofrinho)}, {"goldenMushroom", (Ouro, SuperDado)}, {"magicMushroom", (Vermelho, Foguete)},
            {"poisonMushroom", (Azul, BuracoNoBolso)}, {"bowserSuit", (Hex("#2D3A5A"), TrajeExecutivo)},
        };
        foreach (var kv in selos) feitos.Add(Gerar(PastaSelos + kv.Key + ".png", 768, t => Selo(t, kv.Value.Item1, kv.Value.Item2, 512)));

        feitos.Add(Gerar(TexturaCasaItem, n, CasaDeItem, false));

        AssetDatabase.Refresh();
        return string.Join(", ", feitos);
    }
}
