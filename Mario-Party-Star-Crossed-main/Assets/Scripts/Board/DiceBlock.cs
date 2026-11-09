using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceBlock : MonoBehaviour {
    public List<Material> faces;
    public float rotationSpeed;
    public List<ParticleSystem> particles;
    public bool magic;
    [Tooltip("Set to anything other than 0 to force a certain roll.")]
    public int debugRoll = 0;

    private int currentRoll;
    private int maxRoll;
    private int degree;
    private Renderer rend;
    private List<Transform> followers;
    private bool leader;
    [HideInInspector]
    public float comTimer;
    private int aiMagicRoll;

    // Start is called before the first frame update
    void Start() {
        maxRoll = faces.Count;
        currentRoll = Random.Range(1, maxRoll);
        rend = GetComponent<Renderer>();
        degree = 0;
        leader = true;
        followers = new List<Transform>();
        aiMagicRoll = 0;

        // Garante a existência de um Collider para detectar toques e cliques diretos no modelo 3D
        if (GetComponent<Collider>() == null) {
            gameObject.AddComponent<BoxCollider>();
        }

        if (transform.parent.gameObject.GetComponent<Player>() != null) {
            transform.position = transform.parent.position + (Vector3.up * 3.0f);
            if (transform.parent.gameObject.GetComponent<Player>().state.getController() == 0) {
                comTimer = -8.5f;
            } else {
                comTimer = 0.0f;
                if (magic) {
                    string resposta = transform.parent.gameObject.GetComponent<Player>().brain.Prompt(Prompts.DadoMagico, new List<string>(), 0);
                    if (!int.TryParse(resposta, out aiMagicRoll) || aiMagicRoll < 1 || aiMagicRoll > maxRoll) {
                        aiMagicRoll = GameRng.Range(1, maxRoll + 1);
                    }
                }
            }
        } else if (transform.parent.gameObject.GetComponent<DiceBlock>() != null) {
            leader = false;
            transform.parent.gameObject.GetComponent<DiceBlock>().AskForDirection(transform);
        }
    }

    // Update is called once per frame
    void Update() {
        comTimer += Time.deltaTime;
        transform.Rotate(new Vector3(rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime));
        if (magic) {
            int swipe = MobileInputManager.GetHorizontalSwipe();
            if (aiMagicRoll != 0) {
                currentRoll = aiMagicRoll;
            } else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.D) || swipe > 0) {
                currentRoll += 1;
                if (currentRoll > maxRoll) {
                    currentRoll = 1;
                }
            } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.A) || swipe < 0) {
                currentRoll -= 1;
                if (currentRoll < 1) {
                    currentRoll = maxRoll;
                }
            }
        } else {
            if (currentRoll == maxRoll) {
                currentRoll = 1;
            } else {
                currentRoll += 1;
            }
        }
        
        if (debugRoll != 0) {
            currentRoll = debugRoll;
        }
        rend.material = faces[currentRoll - 1];

        if (followers.Count > 0) {
            degree += 2;
            if (degree >= 360 / followers.Count) {
                degree = degree % (360 / followers.Count);
            }
            for (int f = 0; f < followers.Count; f++) {
                followers[f].position = (Quaternion.AngleAxis(degree + ((360 / followers.Count) * f), Vector3.up) * Vector3.forward * 1.5f) + transform.position;
            }
        }

        // Verificação de autoridade: apenas o jogador local dono do turno pode rolar por toque ou tecla
        bool podeInteragir = (NetworkTurnManager.Instance == null || NetworkTurnManager.Instance.PodeJogadorLocalInteragir());
        int ctrl = 0;
        if (transform.parent != null && transform.parent.gameObject.GetComponent<Player>() != null) {
            ctrl = transform.parent.gameObject.GetComponent<Player>().state.getController();
        }
        bool isHumanTurn = podeInteragir && (ctrl == 0 || ctrl == 5);
        bool isBotTurn = (ctrl == 1 || ctrl == 2);

        bool touchTriggered = isHumanTurn && MobileInputManager.IsTouchOrClickDown();

        // Se for a vez do jogador local, ele dispara por Espaço/Toque. Se for Bot, dispara por tempo
        if (((Input.GetKeyDown(KeyCode.Space) || touchTriggered) && isHumanTurn) || (comTimer >= 1.5f && isBotTurn)) {
            TriggerRoll();
        }
    }

    /// <summary>
    /// Permite rolar tocando ou clicando diretamente sobre o modelo 3D do dado.
    /// </summary>
    void OnMouseDown() {
        bool podeInteragir = (NetworkTurnManager.Instance == null || NetworkTurnManager.Instance.PodeJogadorLocalInteragir());
        int ctrl = 0;
        if (transform.parent != null && transform.parent.gameObject.GetComponent<Player>() != null) {
            ctrl = transform.parent.gameObject.GetComponent<Player>().state.getController();
        }
        bool isHumanTurn = podeInteragir && (ctrl == 0 || ctrl == 5);

        if (isHumanTurn && rend != null && rend.enabled) {
            TriggerRoll();
        }
    }

    /// <summary>Já foi inicializado (Start rodou) e pode receber um valor.</summary>
    public bool Pronto => maxRoll > 0;
    /// <summary>Já rolou (não aceita outra rolagem).</summary>
    public bool JaRolou => comTimer <= -90.0f;

    private int ControllerDoDono() {
        Player dono = GetComponentInParent<Player>(true);
        return (dono != null && dono.state != null) ? dono.state.getController() : 0;
    }

    /// <summary>
    /// Posição deste dado no conjunto do jogador (0 = líder, 1.. = seguidores na ordem da hierarquia).
    /// A hierarquia vem do prefab, então é a mesma em todas as máquinas.
    /// </summary>
    public int IndiceNoConjunto() {
        Player dono = GetComponentInParent<Player>(true);
        if (dono == null) return 0;
        DiceBlock[] conjunto = dono.GetComponentsInChildren<DiceBlock>(true);
        return System.Array.IndexOf(conjunto, this);
    }

    private void TriggerRoll() {
        if (JaRolou) return;
        HapticFeedback.VibrateDiceRoll();
        comTimer = -99.9f;

        int ctrl = ControllerDoDono();
        bool dadoRemoto = ctrl == 5; // valor vem pela rede (ForcarRolagemRemota), nunca do quadro local

        // Dado de bot (não mágico): valor sai do GameRng em vez do quadro em que disparou,
        // para todas as máquinas do online chegarem ao mesmo resultado
        if (!magic && (ctrl == 1 || ctrl == 2)) {
            currentRoll = GameRng.Range(1, maxRoll + 1);
        }
        if (rend != null) rend.enabled = false;
        foreach (ParticleSystem part in particles) {
            if (part != null) part.Play();
        }

        // Só a máquina do humano dono do dado transmite o valor (bots são calculados em todas as máquinas)
        if (ctrl == 0) {
            NetworkTurnManager.Instance?.NotificarDadoRolado(currentRoll, IndiceNoConjunto());
        }

        if (leader) {
            transform.parent.gameObject.GetComponent<Player>().SendRoll(currentRoll);
            // Dados seguidores (dado duplo/triplo) são filhos do líder e morrem junto com ele.
            // No turno do bot eles nunca disparavam sozinhos (o Player não é pai direto deles),
            // então o líder rola todos juntos. Dado remoto NÃO: cada seguidor espera o próprio valor da rede.
            if (!dadoRemoto) {
                foreach (Transform f in followers) {
                    if (f != null) {
                        DiceBlock seguidor = f.GetComponent<DiceBlock>();
                        if (seguidor != null) seguidor.TriggerRoll();
                    }
                }
            }
            // O líder só some quando todos os seguidores rolaram (no remoto os valores chegam aos poucos)
            StartCoroutine(DestruirQuandoConjuntoRolar());
        } else {
            transform.parent.gameObject.GetComponent<DiceBlock>().PassAlongRoll(currentRoll);
            // Seguidor não se destrói sozinho: o líder destrói o conjunto quando todos rolaram
        }
    }

    private IEnumerator DestruirQuandoConjuntoRolar() {
        float limite = Time.time + 30f;
        while (Time.time < limite) {
            bool todos = true;
            foreach (Transform f in followers) {
                if (f != null && !f.GetComponent<DiceBlock>().JaRolou) { todos = false; break; }
            }
            if (todos) break;
            yield return null;
        }
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Força o dado a parar no valor recebido do adversário remoto via rede.
    /// </summary>
    /// <summary>Rola agora (se ainda não rolou), como se o jogador tivesse tocado no dado.</summary>
    public void ForcarRolagemPorTimeout() {
        TriggerRoll();
    }

    /// <summary>Faz ESTE dado parar no valor recebido do dono remoto (não rola os seguidores).</summary>
    public void ForcarRolagemRemota(int roll) {
        if (JaRolou || !Pronto) return;
        currentRoll = Mathf.Clamp(roll, 1, maxRoll);
        TriggerRoll();
    }

    public void AskForDirection(Transform t) {
        followers.Add(t);
        t.GetComponent<DiceBlock>().comTimer = this.comTimer;
    }

    public void PassAlongRoll(int i) {
        if (leader) {
            transform.parent.gameObject.GetComponent<Player>().SendRoll(i);
        }
    }
}
