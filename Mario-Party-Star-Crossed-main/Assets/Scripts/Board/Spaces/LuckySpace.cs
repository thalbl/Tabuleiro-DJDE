using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LuckySpace : BoardSpace {
    public bool booItemsAllowed = true;
    public bool chompCallAllowed = true;
    public bool magicLampAllowed = true;
    public bool doubleStarCardAllowed = true;
    public bool chompTreatAllowed = false;
    public GameObject coinPrefab;

    public override void setup() {
        this.blueChance = 100;
        doneLanding = false;
    }

    public override IEnumerator land(Player p) {
        doneLanding = false;
        List<string> normalEvents = new List<string>() {
            "Ganhe 5 Moedas",
            "Ganhe 7 Moedas",
            "Ganhe 10 Moedas",
            "Ganhe 12 Moedas",
            "Ganhe 15 Moedas",
            "Todos Ganham 3 Moedas",
            "Todos Ganham 5 Moedas",
            "Todos Ganham 7 Moedas",
            "Ganhe uma Poupança", // Mushroom -> Poupança
            "Ganhe uma Dívida", // Poison Mushroom -> Dívida
            "Ganhe um Fundo de Emergência", // Skeleton Key -> Fundo de Emergência
            "Ganhe um Fundo de Investimento", // Warp Pipe -> Fundo de Investimento
            "Ganhe uma Oportunidade de Ouro", // Golden Drink -> Oportunidade de Ouro
            "Ganhe um Baú do Tesouro" // Plunder Chest -> Baú do Tesouro
        };
        List<string> rareEvents = new List<string>() {
            "Todos Ganham 10 Moedas",
            "Todos Ganham 12 Moedas",
            "Ganhe 20 Moedas",
            "Ganhe uma Bolsa de Valores", // Golden Mushroom -> Bolsa de Valores
            "Ganhe um Imóvel", // Magic Mushroom -> Imóveis
            "Ganhe um Plano de Aposentadoria", // Vac Pack -> Plano de Aposentadoria
            "Ganhe um Bilhete de Loteria", // Tweester Totem -> Bilhete de Loteria
            "Ganhe uma Habilidade de Negociação", // Dueling Glove -> Habilidade de Negociação
            "Ganhe um Traje de Negócios" // Bowser Suit -> Traje de Negócios
        };
        List<string> superRareEvents = new List<string>() {
            "Ganhe 25 Moedas",
            "Ganhe 30 Moedas",
            "Todos Ganham 20 Moedas",
            "Ganhe um Pacote de Itens" // Item Bag -> Pacote de Itens
        };
        PlayerState[] st = game.state.GetStandings();
        if (game.state.GetType() == typeof(TreasureTempleGameState)) {
            normalEvents.Add("Invista 5 Moedas em um Santuário Aleatório");
            rareEvents.Add("Invista 15 Moedas em um Santuário Aleatório");
            rareEvents.Add("Invista 3 Moedas em Todos os Santuários");
            rareEvents.Add("Ganhe um Ganho Inesperado"); // Wiggler Whistle -> Ganho Inesperado
        }
        if (game.state.GetType() == typeof(MarvelousMetroGameState)) {
            rareEvents.Add("Reduza o Preço das Estrelas em 5 Moedas");
            rareEvents.Add("Reduza o Preço das Estrelas em 10 Moedas");
        }
        if (booItemsAllowed) {
            rareEvents.Add("Ganhe um Consultor Financeiro"); // Gaddlight -> Consultor Financeiro
            superRareEvents.Add("Ganhe um Empréstimo"); // Boo Bell -> Empréstimo
        }
        if (chompCallAllowed) {
            rareEvents.Add("Ganhe um Cartão de Crédito"); // Chomp Call -> Cartão de Crédito
        }
        if (magicLampAllowed) {
            superRareEvents.Add("Ganhe um Investimento Mágico"); // Magic Lamp -> Investimento Mágico
        }
        if (doubleStarCardAllowed) {
            superRareEvents.Add("Ganhe uma Renda Extra"); // Double Star Card -> Renda Extra
        }
        if (chompTreatAllowed) {
            rareEvents.Add("Ganhe uma Restituição de Impostos"); // Chomp Treat -> Restituição de Impostos
        }
        List<string> luckyOptions = new List<string>();
        string foo = normalEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        normalEvents.Remove(foo);
        foo = normalEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        normalEvents.Remove(foo);
        foo = normalEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        normalEvents.Remove(foo);
        foo = normalEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        normalEvents.Remove(foo);
        foo = rareEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        rareEvents.Remove(foo);
        foo = rareEvents[Random.Range(0, normalEvents.Count - 1)];
        luckyOptions.Add(foo);
        rareEvents.Remove(foo);
        luckyOptions.Add(superRareEvents[Random.Range(0, normalEvents.Count - 1)]);
        ui.Spinner("Roleta da Sorte!", new Color(0.0f, 0.68f, 1.0f, 1.0f), luckyOptions);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
        switch (ui.MostRecentDialogueAnswer()) {
            case "Ganhe 5 Moedas":
            for (int i = 0; i < 5; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(5);
            break;
        case "Ganhe 7 Moedas":
            for (int i = 0; i < 7; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(7);
            break;
        case "Ganhe 10 Moedas":
            for (int i = 0; i < 10; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(10);
            break;
        case "Ganhe 12 Moedas":
            for (int i = 0; i < 12; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(12);
            break;
        case "Ganhe 15 Moedas":
            for (int i = 0; i < 15; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(15);
            break;
        case "Ganhe 20 Moedas":
            for (int i = 0; i < 20; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(20);
            break;
        case "Ganhe 25 Moedas":
            for (int i = 0; i < 25; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(25);
            break;
        case "Ganhe 30 Moedas":
            for (int i = 0; i < 30; i++) {
                Instantiate(coinPrefab, new Vector3(transform.position.x, transform.position.y + 5.0f, transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            p.state.changeCoins(30);
            break;
        case "Todos Ganham 3 Moedas":
            for (int i = 0; i < 3; i++) {
                Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (PlayerState ps in st) {
                ps.changeCoins(3);
            }
            break;
        case "Todos Ganham 5 Moedas":
            for (int i = 0; i < 5; i++) {
                Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (PlayerState ps in st) {
                ps.changeCoins(5);
            }
            break;
        case "Todos Ganham 7 Moedas":
            for (int i = 0; i < 7; i++) {
                Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (PlayerState ps in st) {
                ps.changeCoins(7);
            }
            break;
        case "Todos Ganham 10 Moedas":
            for (int i = 0; i < 10; i++) {
                Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (PlayerState ps in st) {
                ps.changeCoins(10);
            }
            break;
        case "Todos Ganham 12 Moedas":
            for (int i = 0; i < 12; i++) {
                Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(1.2f);
            foreach (PlayerState ps in st) {
                ps.changeCoins(12);
            }
            break;
        case "Todos Ganham 20 Moedas":
            for (int i = 0; i < 20; i++) {
                    Instantiate(coinPrefab, new Vector3(game.p1.transform.position.x, game.p1.transform.position.y + 5.0f, game.p1.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    Instantiate(coinPrefab, new Vector3(game.p2.transform.position.x, game.p2.transform.position.y + 5.0f, game.p2.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    Instantiate(coinPrefab, new Vector3(game.p3.transform.position.x, game.p3.transform.position.y + 5.0f, game.p3.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    Instantiate(coinPrefab, new Vector3(game.p4.transform.position.x, game.p4.transform.position.y + 5.0f, game.p4.transform.position.z), Quaternion.Euler(0, Random.Range(0, 360), 0));
                    yield return new WaitForSeconds(0.1f);
                }
                yield return new WaitForSeconds(1.2f);
                foreach (PlayerState ps in st) {
                    ps.changeCoins(20);
                }
                break;
            case "Ganhe uma Poupança":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.Mushroom, true));
                break;
            case "Ganhe uma Bolsa de Valores":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.GoldenMushroom, true));
                break;
            case "Ganhe um Imóvel":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.MagicMushroom, true));
                break;
            case "Ganhe uma Dívida":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.PoisonMushroom, true));
                break;
            case "Ganhe um Fundo de Emergência":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.SkeletonKey, true));
                break;
            case "Ganhe um Fundo de Investimento":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.WarpPipe, true));
                break;
            case "Ganhe uma Oportunidade de Ouro":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.GoldenDrink, true));
                break;
            case "Ganhe um Plano de Aposentadoria":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.VacPack, true));
                break;
            case "Ganhe um Empréstimo":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.BooBell, true));
                break;
            case "Ganhe um Bilhete de Loteria":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.TweesterTotem, true));
                break;
            case "Ganhe uma Habilidade de Negociação":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.DuelingGlove, true));
                break;
            case "Ganhe um Cartão de Crédito":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.ChompCall, true));
                break;
            case "Ganhe um Consultor Financeiro":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.Gaddlight, true));
                break;
            case "Ganhe um Investimento Mágico":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.MagicLamp, true));
                break;
            case "Ganhe um Traje de Negócios":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.BowserSuit, true));
                break;
            case "Ganhe uma Renda Extra":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.DoubleStarCard, true));
                break;
            case "Ganhe uma Restituição de Impostos":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.ChompTreat, true));
                break;
            case "Ganhe um Ganho Inesperado":
                yield return StartCoroutine(GivePlayerItem(p, BoardItem.WigglerWhistle, true));
                break;
            case "Ganhe um Pacote de Itens":
                while (p.state.getItems().Count < 3) {
                    int ri = Random.Range(0, 18);
                    if ((!booItemsAllowed && (ri == 9 || ri == 13)) || (game.state.GetType() != typeof(MarvelousMetroGameState) && ri == 18) || (!chompCallAllowed && ri == 12) || (!magicLampAllowed && ri == 14) || (!chompTreatAllowed && ri == 17) || (!doubleStarCardAllowed && ri == 16)) {
                        ri = Random.Range(0, 8);
                    }
                    yield return StartCoroutine(GivePlayerItem(p, (BoardItem) ri, true));
                }
                break;
            default:
                break;
        }
        doneLanding = true;
    }

    public override int AIValue(PlayerState state, List<PlayerState> rivals) {
        return 10;
    }
}
