using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BoardSpace : MonoBehaviour {
    [Tooltip("The Space directly after this one in the board layout.")]
    public BoardSpace next;
    [Tooltip("This space's internal ID Number.")]
    public int id;

    protected UIManager ui;
    protected BoardManager game;
    protected bool doneLanding;
    protected bool donePassing;
    protected int blueChance;
    protected bool canLandHere;

    // Start is called before the first frame update
    void Start() {
        this.blueChance = 75;
        this.canLandHere = true;
        ui = FindObjectOfType<UIManager>();
        game = FindObjectOfType<BoardManager>();
        this.setup();
        CasaVisual.Preparar(this);
        donePassing = true;
        doneLanding = true;
    }

    // Update is called once per frame
    void Update() {
        
    }

    public virtual void setup() {}

    // Casas azul/vermelha na categoria 0 (carreira Nv. 1-3, onde passa a maior parte da partida)
    // rendiam/custavam 7-20% do líquido de um turno, então cair nelas quase não fazia diferença.
    // Da categoria 1 em diante os valores já ficam em ~40-80% de um turno e não mudam.
    public const float FatorMoedasCategoriaInicial = 3f;

    protected static int EscalarMoedasCasa(int valor, int categoria) {
        return categoria == 0 ? Mathf.RoundToInt(valor * FatorMoedasCategoriaInicial) : valor;
    }

    public void passHere(Player p) {
        StartCoroutine(pass(p));
    }

    public void landHere(Player p) {
        CasaVisual.Pulsar(this);
        StartCoroutine(setMGTeam(p));
        StartCoroutine(land(p));
    }

    public bool ableToLandHere() {
        return canLandHere;
    }

    public virtual BoardSpace getNextSpaceInSequence() {
        return this.next;
    }

    public IEnumerator setMGTeam(Player p) {
        if (blueChance == 100) {
            p.state.setTeam(1);
            yield return null;
        } else if (blueChance == 0) {
            p.state.setTeam(2);
            yield return null;
        } else {
            p.state.setTeam(1);
            yield return new WaitForSeconds(0.1f);
            p.state.setTeam(2);
            yield return new WaitForSeconds(0.1f);
            p.state.setTeam(1);
            yield return new WaitForSeconds(0.1f);
            p.state.setTeam(2);
            yield return new WaitForSeconds(0.1f);
            p.state.setTeam(1);
            yield return new WaitForSeconds(0.1f);
            p.state.setTeam(2);
            yield return new WaitForSeconds(0.1f);
            if (GameRng.Range(1, 100) <= blueChance) {
                p.state.setTeam(1);
            }
        } 
    }

    public virtual IEnumerator pass(Player p) {
        donePassing = true;
        yield return null;
    }

    public virtual IEnumerator land(Player p) {
        doneLanding = true;
        yield return null;
    }

    public bool doneLandingYet() {
        return doneLanding;
    }

    public bool donePassingYet() {
        return donePassing;
    }

    public IEnumerator GivePlayerItem(Player p, BoardItem i, bool endOfChain) {
        bool bolsoCheio = !p.state.addItem(i); // Verifica se o inventário está cheio
        ui.Dialogue("Você obteve um(a) " + ItemSpace.itemNames[(int)i] + "!", !bolsoCheio && endOfChain);
        yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

        if (bolsoCheio) {
            // Lista os itens que o jogador está carregando
            List<string> itensDoJogador = new List<string>();
            foreach (BoardItem b in p.state.getItems()) {
                itensDoJogador.Add(ItemSpace.itemNames[(int)b]);
            }
            itensDoJogador.Add(ItemSpace.itemNames[(int)i]); // Adiciona o novo item à lista

            // Pergunta ao jogador qual item deseja descartar
            ui.Dialogue("Você está carregando muitos itens. Escolha um para descartar.", itensDoJogador, false);
            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());

            string itemDescartado = ui.MostRecentDialogueAnswer(); // Item escolhido para descartar
            yield return new WaitForSeconds(0.1f);

            // Converte o nome do item de volta para o tipo BoardItem
            BoardItem descartando = (BoardItem)ItemSpace.itemNames.IndexOf(itemDescartado);

            // Confirma o descarte e adiciona o novo item
            ui.Dialogue("Você descartou " + itemDescartado + ".", endOfChain);
            p.state.removeItem(descartando); // Remove o item descartado
            p.state.addItem(i); // Adiciona o novo item

            yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
            yield return new WaitForSeconds(0.1f);
        }
    }

    public virtual int SpacesToStar(bool withKey) {
        return SpacesToStar(withKey, 0);
    }

    public virtual int SpacesToStar(bool withKey, int depth) {
        if (depth >= 30 || next == null) {
            return 30;
        }
        return next.SpacesToStar(withKey, depth + 1) + (this.canLandHere ? 1 : 0);
    }

    public virtual int PlayersInRange(int range) {
        int playersHere = 0;
        if (id == game.p1.state.getSpaceID()) {
            playersHere += 1;
        }
        if (id == game.p2.state.getSpaceID()) {
            playersHere += 1;
        } 
        if (id == game.p3.state.getSpaceID()) {
            playersHere += 1;
        }
        if (id == game.p4.state.getSpaceID()) {
            playersHere += 1;
        }
        if ((range <= 0 && this.canLandHere) || next == null) {
            return playersHere;
        } else {
            return next.PlayersInRange(range - (this.canLandHere ? 1 : 0)) + playersHere;
        }
    }

    public abstract int AIValue(PlayerState state, List<PlayerState> rivals);

    public virtual int CumulativeValue(PlayerState state, List<PlayerState> rivals, int roll) {
        return CumulativeValue(state, rivals, roll, 0);
    }

    public virtual int CumulativeValue(PlayerState state, List<PlayerState> rivals, int roll, int depth) {
        if (depth >= 40 || next == null) {
            return AIValue(state, rivals);
        }
        if (canLandHere) {
            if (roll <= 0) {
                return AIValue(state, rivals) - (state.getCoins() >= ClassicStarSpace.PrecoEstrela(state) ? SpacesToStar(state.getItems().Contains(BoardItem.SkeletonKey)) : 0);
            } else {
                return next.CumulativeValue(state, rivals, roll - 1, depth + 1);
            }
        } else {
            return AIValue(state, rivals) + next.CumulativeValue(state, rivals, roll, depth + 1);
        }
    }

    public bool CanLandHere() {
        return canLandHere;
    }
}
