using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoardItem {
    Mushroom,           // Corresponde a "Poupança"
    GoldenMushroom,     // Corresponde a "Bolsa de Valores"
    MagicMushroom,      // Corresponde a "Imóveis"
    PoisonMushroom,     // Corresponde a "Dívida"
    SkeletonKey,        // Corresponde a "Fundo de Emergência"
    WarpPipe,           // Corresponde a "Fundo de Investimento"
    PlunderChest,       // Corresponde a "Baú do Tesouro" (investimento de alto risco)
    GoldenDrink,        // Corresponde a "Oportunidade de Ouro"
    VacPack,            // Corresponde a "Plano de Aposentadoria"
    BooBell,            // Corresponde a "Empréstimo"
    TweesterTotem,      // Corresponde a "Bilhete de Loteria"
    DuelingGlove,       // Corresponde a "Habilidade de Negociação"
    ChompCall,          // Corresponde a "Cartão de Crédito"
    Gaddlight,          // Corresponde a "Consultor Financeiro"
    MagicLamp,          // Corresponde a "Investimento Mágico" (alto retorno)
    BowserSuit,         // Corresponde a "Traje de Negócios"
    DoubleStarCard,     // Corresponde a "Renda Extra"
    ChompTreat,         // Corresponde a "Restituição de Impostos"
    WigglerWhistle      // Corresponde a "Ganho Inesperado"
}

public class ItemSpace : BoardSpace {
    public static List<string> itemNames = new List<string>() {
    "Cofrinho",           // Mushroom -> Cofrinho
    "Super Dado",         // Golden Mushroom -> Super Dado
    "Foguete",            // Magic Mushroom -> Foguete
    "Buraco no Bolso",    // Poison Mushroom -> Buraco no Bolso
    "Chave Secreta",      // Skeleton Key -> Chave Secreta
    "Portal Mágico",      // Warp Pipe -> Portal Mágico
    "Baú do Tesouro",     // Plunder Chest -> Baú do Tesouro
    "Bebida Dourada",     // Golden Drink -> Bebida Dourada
    "Mochila Mágica",     // Vac Pack -> Mochila Mágica
    "Sino do Boo",        // Boo Bell -> Sino do Boo
    "Ventania",           // Tweester Totem -> Ventania
    "Luva de Duelo",      // Dueling Glove -> Luva de Duelo
    "Apito do Chomp",     // Chomp Call -> Apito do Chomp
    "Escudo Protetor",    // Gaddlight -> Escudo Protetor
    "Lâmpada Mágica",     // Magic Lamp -> Lâmpada Mágica
    "Fantasia do Bowser", // Bowser Suit -> Fantasia do Bowser
    "Moeda Dobrada",      // Double Star Card -> Moeda Dobrada
    "Presente Surpresa",  // Chomp Treat -> Presente Surpresa
    "Apito do Wiggler"    // Wiggler Whistle -> Apito do Wiggler
};

    public static List<string> itemDescriptions = new List<string>() {
    "Avança 1 casa extra.",
    "Avança 2 casas extras.",
    "Avança 3 casas extras!",
    "Jogador rival perde 4 passos de movimento.",
    "Abre portões secretos e atalhos.",
    "Troca de lugar com outro jogador!",
    "Rouba um item de um rival.",
    "Dá um bônus surpresa especial.",
    "Ganha um item extra aleatório.",
    "O Boo rouba moedas ou estrelas de um rival!",
    "Sopra um jogador para um lugar aleatório.",
    "Desafia um rival para um duelo!",
    "Chama o Chomp para uma missão especial.",
    "Protege contra o roubo do Boo.",
    "Te leva direto até a Estrela!",
    "Cobra 20 Moedas de quem passar por você.",
    "Receba +30 Moedas na hora!",
    "Receba +25 Moedas de volta!",
    "Chama um evento surpresa raro."
    };

    public bool booItemsAllowed = true;
    public bool chompCallAllowed = true;
    public bool magicLampAllowed = true;
    public bool doubleStarCardAllowed = true;
    public bool chompTreatAllowed = false;
    public bool wigglerWhistleAllowed = false;

    private List<BoardItem> tier1;
    private List<BoardItem> tier2;
    private List<BoardItem> tier3;
    private List<BoardItem> tier4;
    private List<BoardItem> tier5;

    private int[,,] odds = new int[4, 4, 5] {
        {{10, 8, 2, 0, 0}, {9, 8, 3, 0, 0}, {8, 7, 4, 1, 0}, {8, 6, 4, 2, 0}},
        {{10, 8, 2, 0, 0}, {8, 6, 3, 2, 1}, {6, 5, 4, 3, 2}, {4, 5, 5, 3, 3}},
        {{9, 8, 2, 1, 0}, {8, 6, 3, 2, 1}, {4, 5, 5, 4, 3}, {2, 4, 4, 6, 4}},
        {{8, 6, 4, 2, 0}, {6, 5, 4, 3, 2}, {2, 4, 4, 6, 4}, {1, 1, 4, 8, 6}}
    };

    public override void setup() {
        this.canLandHere = false;
        // Definição dos tiers com os itens originais
        tier5 = new List<BoardItem>() {
            BoardItem.Mushroom,       // Poupança
            BoardItem.PoisonMushroom, // Dívida
            BoardItem.SkeletonKey    // Fundo de Emergência
        };
        tier4 = new List<BoardItem>() {
            BoardItem.WarpPipe,      // Fundo de Investimento
            BoardItem.PlunderChest,  // Baú do Tesouro
            BoardItem.GoldenDrink    // Oportunidade de Ouro
        };
        tier3 = new List<BoardItem>() {
            BoardItem.GoldenMushroom, // Bolsa de Valores
            BoardItem.MagicMushroom,  // Imóveis
            BoardItem.VacPack,        // Plano de Aposentadoria
            BoardItem.TweesterTotem   // Bilhete de Loteria
        };
        tier2 = new List<BoardItem>() {
            BoardItem.DuelingGlove,   // Habilidade de Negociação
            BoardItem.BowserSuit      // Traje de Negócios
        };
        tier1 = new List<BoardItem>();
        if (booItemsAllowed) {
            tier3.Add(BoardItem.Gaddlight); // Consultor Financeiro
            tier1.Add(BoardItem.BooBell);   // Empréstimo
        };
        if (chompCallAllowed) {
            tier2.Add(BoardItem.ChompCall); // Cartão de Crédito
        }
        if (magicLampAllowed) {
            tier1.Add(BoardItem.MagicLamp); // Investimento Mágico
        }
        if (doubleStarCardAllowed) {
            tier1.Add(BoardItem.DoubleStarCard); // Renda Extra
        }
        if (chompTreatAllowed) {
            tier2.Add(BoardItem.ChompTreat); // Restituição de Impostos
        }
        if (wigglerWhistleAllowed) {
            tier3.Add(BoardItem.WigglerWhistle); // Ganho Inesperado
        }
        if (tier1.Count < 1) {
            tier1.AddRange(tier2);
            tier1.AddRange(tier3);
        }
    }

    public override IEnumerator pass(Player p) {
        if (game.state.getTurnStatus() != 4) {
            donePassing = false;
            ui.MoveCounter(false);
            BoardItem choice = CalcOdds(Random.Range(1, 20), p.state.getPlacing(), game.state.getTurnStatus());
            GetComponentsInChildren<AudioSource>()[0].Play();
            yield return StartCoroutine(GivePlayerItem(p, choice, true));

            // Diálogo adaptado para educação financeira
            string itemDescription = "";
            switch (choice) {
                case BoardItem.Mushroom:
                    itemDescription = "Você adquiriu uma Poupança! Um investimento seguro e confiável.";
                    break;
                case BoardItem.GoldenMushroom:
                    itemDescription = "Você investiu na Bolsa de Valores! Alto risco, alto retorno.";
                    break;
                case BoardItem.PoisonMushroom:
                    itemDescription = "Cuidado! Você contraiu uma Dívida. É hora de se organizar.";
                    break;
                case BoardItem.SkeletonKey:
                    itemDescription = "Você criou um Fundo de Emergência! Segurança para imprevistos.";
                    break;
                // Adicione mais casos conforme necessário
                default:
                    itemDescription = "Você adquiriu um item útil para sua jornada financeira!";
                    break;
            }
            ui.Dialogue("Consultor Financeiro", itemDescription, true);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        }
        donePassing = true;
        ui.MoveCounter(true);
    }

    private BoardItem CalcOdds(int r, int p, int t) {
        int i = 0;
        while (r > odds[p - 1, t, i]) {
            r -= odds[p - 1, t, i];
            i += 1;
        }
        if (i == 4) {
            return tier1[Random.Range(0, tier1.Count - 1)];
        }
        else if (i == 3) {
            return tier2[Random.Range(0, tier2.Count - 1)];
        }
        else if (i == 2) {
            return tier3[Random.Range(0, tier3.Count - 1)];
        }
        else if (i == 1) {
            return tier4[Random.Range(0, tier4.Count - 1)];
        }
        else {
            return tier5[Random.Range(0, tier5.Count - 1)];
        }
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        switch (state.getPlacing()) {
            case 4:
                return 16;
            case 3:
                return 13;
            case 2:
                return 10;
            default:
                return 7;
        }
    }
}