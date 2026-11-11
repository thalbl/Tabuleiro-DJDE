---
name: contexto-projeto
description: Contexto do jogo "Caminho do Sucesso" (base de um fangame de tabuleiro, re-tematizado como simulador de vida financeira/carreira em PT-BR; Unity 6 + URP). Use SEMPRE antes de mexer em gameplay, espaços do tabuleiro, itens, economia (moedas/salário/despesas), carreira/educação, turnos, IA, UI de diálogo, multiplayer local/online, save, cenas, identidade/marca ou visual/shaders deste projeto.
---

# Contexto do Projeto — Caminho do Sucesso (simulador de vida financeira)

Fork de um fangame de Mario Party ("Star Crossed") cujos conceitos foram **re-tematizados como simulador de vida financeira e carreira**, todo em **português brasileiro**. O núcleo de tabuleiro (turnos, dados, casas, itens, estrelas) foi mantido; a camada de regras foi trocada por salário, despesas, carreira, educação e quizzes de educação financeira.

> Regra de ouro: texto exibido ao jogador é **PT-BR**. Código novo pode seguir o padrão misto existente (nomes em PT para sistemas novos, EN para o legado), mas não traduza identificadores legados — muita lógica depende de **strings exatas**.

---

## 1. Stack técnica

| Item | Valor |
|---|---|
| Engine | Unity **6000.3.9f1** (Unity 6.3) |
| Render pipeline | **URP 17.3** (migrado do Built-in em 2026-11-09, branch `feat/migracao-urp`) |
| Color space | **Linear** |
| UI | **uGUI** + TextMesh Pro; menus da StartScene são **construídos em código** (`MainMenuBuilder`, `LocalLobbyUI`, `OnlineMenuUI`, `TutorialManager`) |
| Rede | TCP puro próprio (`System.Net.Sockets`, porta 7777) — **não** usa Netcode/NGO |
| Pacotes notáveis | ProBuilder, Timeline, AI Navigation, `com.unity.pipeline` (Unity CLI) |
| Alvos | PC + **mobile** (Android minSdk 25, landscape, SafeArea, haptics, swipe) |
| VCS | Git **e** Plastic (`.plastic/`) convivem |
| Skill auxiliar | `.agents/skills/unity-cli` — preferir `unity command ...` para editar cenas num Editor aberto em vez de editar YAML à mão |

---

## 2. Cenas e fluxo de navegação

Build settings: `StartScene` → (`MVP` | `MapaRapido`) → `GameOverScene` → `StartScene`.

- **StartScene**: `MainMenuBuilder` monta todo o menu por código (Novo Jogo, Continuar, Lobby Local, Online, Tutorial, Sair).
  - `LocalLobbyUI` → configura 4 slots (nome, avatar, controlador), nº de turnos, mapa, Pass & Play.
  - `OnlineMenuUI` → host/cliente TCP, código de sala, slots remotos.
  - Tudo é persistido via `GameMatchConfig` (static) → `PlayerPrefs` (`Match_*`).
- **MVP** ("CIDADE CLÁSSICA") e **MapaRapido** ("CIRCUITO RÁPIDO"): cenas de tabuleiro. Mesmos scripts, layout diferente.
- **GameOverScene**: `GameOverController` lê `Player{n}Name/Money/Career/FinalScore/ScoreBreakdown...` de `PlayerPrefs` (gravados em `BoardManager.EndGame()`) e instancia `PlayerResultPanel`.
- `SampleScene` é resto do template — ignorar.

---

## 3. Arquitetura do jogo (scripts em `Assets/Scripts`)

```
Data/      GameState, PlayerState, GameMatchConfig, MinigameSet, PlayerResultPanel
Board/     BoardManager (máquina de fases), Player (turno/movimento/itens), DiceBlock,
           GameOverController, StartMenuManager
Board/Spaces/  BoardSpace (abstrata) + todas as casas
Board/AI/  AIBrain (abstrata), PlayerBrain (humano), EasyBrain (bot)
UI/        UIManager (diálogos, HUD), PlayerTracker, PlayerDetailMenu, TurnTimerUI,
           EmoteReactionUI, menus da StartScene, SafeAreaFitter, CanvasResolutionManager
Network/   NetworkProtocol (pacotes JSON), NetworkTransportService (TCP), NetworkTurnManager
Misc/      Coin, CoinCollector, MobileInputManager, HapticFeedback
Editor/    MobileBuildTools
```

### 3.1 BoardManager — loop de partida
`Update()` é uma **máquina de fases** (`phase` 0..10):
- 0: evento de turno (cerimônias — hoje vazios)
- 1–8: `pN.StartTurn()` / espera `pN.TurnOver()` para P1..P4, notificando `NetworkTurnManager`
- 9: "minigame" — **no-op** (não há minigames)
- 10: `HandleEndOfRound()` → diálogo "Fim da Rodada", `turnCompleted()`, `SaveGame()` (autosave a cada rodada)

Também: recalcula colocação **todo frame** por `GetFinalScore()`, registra casas por `BoardSpace.id` (registry), e adiciona em runtime `NetworkTurnManager`, `EmoteReactionUI`, `TurnTimerUI` se não existirem.

**Fim de jogo**: `turnsCompleted >= maxTurns` **ou** algum jogador atinge **carreira Nv. 10** (`MAX_CAREER_LEVEL`).

### 3.2 Player — o turno de um jogador (`Player.TakeTurn()`)
1. (Pass & Play) aviso para passar o dispositivo.
2. `ui.YourTurn(...)`.
3. **Holerite Mensal**: `coins += salárioAtual − despesaAtual` (aplicado TODO turno).
4. Menu: **Rolar Dados / Usar Item / Ver Tabuleiro** (1 item por turno).
5. Rolar: instancia `dicePrefabs[state.getMovement()]`, soma rolagens (`SendRoll`), anda casa a casa chamando `space.passHere()`; casas com `canLandHere=false` (Intersection, ItemShop, CareerTransition) **não consomem passo**.
6. Ao parar: `space.landHere()` → espera `doneLandingYet()`.

### 3.3 Diálogos (UIManager) — padrão onipresente
```csharp
ui.Dialogue("Título", "Texto", new List<string>{"Opção A","Opção B"}, endOfChain: true);
yield return new WaitUntil(() => ui.WaitForDialogueAnswer());
switch (ui.MostRecentDialogueAnswer()) { case "Opção A": ... }
```
- Até **5 opções** (A–E); `SetOptionDescriptions()` para tooltips.
- Para bots, `UIManager` delega a escolha ao `AIBrain.Prompt(question, options, spacesLeft)`.
- Online: `ReceberEscolhaRemota()` injeta a escolha recebida pela rede.
- **As respostas são comparadas por string** — mudar o texto de uma opção exige mudar o `case` correspondente (e a IA).

---

## 4. Jogadores e economia (`PlayerState`)

- 4 jogadores fixos (`p1..p4`). Controlador: `0` humano local, `1` bot fácil, `2` bot difícil (hoje também `EasyBrain`), `5` remoto (rede).
- Avatares 1–13 com nomes brasileiros (`charName()`: João, Maria, Pedro, Ana, …) e cor (`charColor()`); nome customizado tem prioridade.
- **Moeda unificada**: `coins` é o "dinheiro" (`GetDinheiro/SetDinheiro` = coins). Limite 0..9999. Começa com **20**. Campo legado `dinheiro` é migrado (/100) no `OnAfterDeserialize`.
- **Estrelas**: 0..99, compradas na Star Space.
- **Itens**: máx. **3** no bolso.

### Profissão (sorteada ao criar o jogador)
13 profissões com salário/despesa base (despesa ≈ 25–40% do salário):
Médico 80/28 · Engenheiro 70/21 · Professor 40/10 · Advogado 75/26 · Designer 50/15 · Programador 65/20 · Enfermeiro 45/14 · Chef 55/17 · Jornalista 48/14 · Piloto 90/36 · Artista 35/9 · Cientista 72/22 · Empresário 100/40.

### Carreira (níveis 1–10)
Multiplicadores `{1.0, 1.15, 1.3, 1.5, 1.7, 1.9, 2.1, 2.35, 2.6, 3.0}` aplicados a salário **e** despesa.
Títulos: Estagiário, Júnior, Pleno I, Pleno II, Sênior I, Sênior II, Especialista I, Especialista II, Gerente, Diretor.
`Promote()` / `Demote()` recalculam a partir da base.

### Educação (0–3)
Sem formalidade → Ensino Médio → Ensino Superior → Pós-graduação.

### Pontuação final (vitória)
`Estrelas×100 + Moedas + Carreira×50 + Educação×75` (`GetFinalScore`, `GetScoreBreakdown`).

---

## 5. Espaços do tabuleiro (`Board/Spaces`)

Base `BoardSpace`: `next`, `id`, `canLandHere`, `blueChance`, corrotinas `pass(p)` / `land(p)` com flags `donePassing`/`doneLanding`, `GivePlayerItem()`, e heurísticas de IA (`AIValue`, `CumulativeValue`, `SpacesToStar`).

**Em uso nas cenas (MVP / MapaRapido):**

| Casa (classe) | Prefab | Tema financeiro | Efeito |
|---|---|---|---|
| `StartSpace` | — | Início | id 0 |
| `BlueSpace` (32/19) | Blue Space | **Bonificação** | Sorteia de `Resources/bonificacoes_nivel_{cat}.txt`: `AumentarSalario`, `GanharMoedasDinheiro`, `GanharDinheiro`, `Promocao` (bônus em moedas) |
| `RedSpace` (18/5) | Red Space | **Penalidade** | `penalidades_nivel_{cat}.txt`: `ReduzirSalario`, `GastarDinheiro`, `PerderMoedas`, `Demissao` (rebaixa ou multa). Valores escalam × nível de carreira |
| `QuestionSpace` (12/9) | Question Space | **Quiz financeiro** | `quiz_nivel_{cat}.txt` (25/nível); acerto = 6 + 2×nível + 5% do salário; explicação após responder; baralho sem repetição |
| `EducationSpace` (7/5) | EducationSpace | **Universidade** | Paga `educationCost` (75) → `IncreaseEducation()` |
| `CareerTransitionSpace` (3/3) | CareerTransitionSpace | **Transição de carreira** | Ao *passar*: se carreira ≥ req, educação ≥ req e moedas ≥ `minSavings` (200) → `Promote()` e desvia para `nextLoopStart` (próximo "anel"). Promoção por mérito automática (salário ≥75, moedas ≥80, nível <3). Não consome passo |
| `ClassicStarSpace` (2/2) | Star Space | **Estrela** | 20 moedas = 1 estrela (40 = 2 com "Moeda Dobrada") |
| `ItemSpace` (3/2) | Item Space | "Consultor Financeiro" | Dá item sorteado por tiers |
| `ItemShop` (5/2) | Shop | **Loja** | Não consome passo; preços fixos (2–15 moedas) |
| `Intersection` (2/2) | Intersection90 | **Bifurcação** | Escolha de caminho; usa `Resources/perguntas.txt` (perguntas de perfil 2 opções); prismas "phantom" mostram a rota |
| `NothingSpace` (13/1) | Board Space | Neutra | "Nada aconteceu!" |

**Categoria por nível de carreira** (Blue/Red/Question): Nv 1–3 → 0 · 4–6 → 1 · 7–8 → 2 · 9–10 → 3.
Formato dos `.txt`: uma entrada por linha, campos separados por `|` (quiz: `pergunta|correta|errada×4|explicação`; linhas `#` são comentário; `perguntas.txt` das bifurcações: `pergunta|caminho principal|alternativo`).

**Legado removido (2026-11-10):** `Boo`, `BowserSpace`, `DuelSpace`, `LuckySpace`, `TestHappeningSpace`, `MinigameSet`, as subclasses de `GameState` dos tabuleiros originais, `SampleScene` e o prefab/materiais da casa do Bowser. Não recriar conteúdo com personagens de terceiros.

---

## 6. Itens (`BoardItem` em `ItemSpace.cs`)

O enum mantém nomes originais; o texto exibido vem de `ItemSpace.itemNames[(int)item]` / `itemDescriptions`. Efeitos estão em `Player.TakeTurn()` num `switch` **pelo nome exibido**.

| Enum | Nome exibido | Efeito implementado | Preço loja |
|---|---|---|---|
| Mushroom | Cofrinho | dado duplo (+1 dado) | 2 |
| GoldenMushroom | Super Dado | dado triplo | 8 |
| MagicMushroom | Foguete | dado mágico | 12 |
| PoisonMushroom | Buraco no Bolso | rival: dado venenoso | 2 |
| SkeletonKey | Chave Secreta | +15 moedas (abre portões) | 4 |
| WarpPipe | Portal Mágico | troca de lugar com rival | 6 |
| PlunderChest | Baú do Tesouro | rouba item de rival | 7 |
| GoldenDrink | Bebida Dourada | — | 6 |
| VacPack | Mochila Mágica | — | 5 |
| BooBell | Espião Corporativo | desvia moedas (grátis) ou estrela (40); o item só é gasto se agir | 9 |
| TweesterTotem | Ventania | manda rival p/ casa aleatória | 5 |
| DuelingGlove | Luva de Duelo | — (duelo não existe) | 7 |
| ChompCall | Agência de Cobrança | cobra até 5 moedas de cada rival | 3 |
| Gaddlight | Escudo Protetor | protege do Espião enquanto no bolso; ou resgata +20 (consome) | 4 |
| MagicLamp | Lâmpada Mágica | — | 15 |
| BowserSuit | Traje de Executivo | 5 dados de 1 a 3; cobra 20 de quem alcançar | 10 |
| DoubleStarCard | Moeda Dobrada | +30 moedas / 2 estrelas na Star | 12 |
| ChompTreat | Presente Surpresa | +25 moedas | 3 |
| WigglerWhistle | Bilhete Premiado | evento raro: +25, aumento de salário ou item | 8 |

`state.setMovement(n)` escolhe o prefab de dado: 0 normal, 1 duplo, 2 triplo, 3 mágico, 4 veneno, 5 Bowser.
⚠️ Há **três vocabulários** para os mesmos itens: comentários do enum ("Poupança", "Bolsa de Valores"...), `itemNames` ("Cofrinho", "Super Dado"...) e textos do `LuckySpace` ("Ganhe uma Poupança"). O canônico na UI é `itemNames`.

---

## 7. Sistemas existentes

- **Save/Load**: `GameState` serializado com `JsonUtility` em `Application.persistentDataPath/caminho_sucesso_save.dat` (PC: `LocalLow/DJDE/Caminho do Sucesso`). `GameState.MigrarSaveLegado()` copia uma única vez o save antigo (`mario_party_save.dat`, inclusive de `LocalLow/DefaultCompany/My project (1)`), marcado pela pref `SaveLegadoMigrado`. O mapa da partida fica no próprio save (`cenaMapa`). `PlayerPrefs["LoadSavedGame"]==1` faz o `BoardManager` carregar. **Todo campo novo de estado deve ser `[SerializeField]` e compatível com JsonUtility** (sem Dictionary, sem propriedades).
- **IA**: `EasyBrain.Prompt()` decide por heurística usando `AIValue/CumulativeValue` das casas. Toda casa nova precisa implementar `AIValue` e a IA precisa reconhecer as strings de opção novas.
- **Multiplayer local**: até 4 humanos/bots, opção Pass & Play.
- **Multiplayer online**: `NetworkTransportService` (TCP host/cliente, LAN/Hamachi/IP público), `NetworkProtocol` (pacotes JSON: lobby, `GameTurnStart`, `GameDiceRoll`, `GameOptionChoice`, `GameEmote`, `GameTimeout`, heartbeat), `NetworkTurnManager` (timer de turno, sincroniza escolhas), `DiceBlock.ForcarRolagemRemota`.
- **HUD**: `PlayerTracker` (4 painéis), `PlayerDetailMenu`, `TurnTimerUI`, `EmoteReactionUI`, contador de movimento.
- **Mobile**: `MobileInputManager` (swipe/toque), `SafeAreaFitter`, `CanvasResolutionManager`, `HapticFeedback`, `MobileBuildTools` (editor).
- **Tutorial**: `TutorialManager` (slides em código).

---

## 8. Armadilhas conhecidas (verifique antes de "consertar" ou depender)

1. **Salário = salário do nível + `ajusteSalario`** (mín. 10). `SetBaseSalary(novo)` grava a diferença em `ajusteSalario`, que sobrevive a `Promote/Demote` e ao save. Nunca escreva em `salarioAtual` direto; saves antigos são migrados em `OnAfterDeserialize` (flag `ajusteSalarioMigrado`).
2. `Range(int min, int max)` tem topo **exclusivo**: para sortear de uma lista use `GameRng.Range(0, lista.Count)` ou `GameRng.Escolher(lista)` (o padrão antigo `Count - 1` nunca sorteava o último; já corrigido no código).
3. `CompleteLap()` (promoção a cada 2 voltas) e `AddInvestment()` **não são chamados** em lugar nenhum.
4. Fase 9 (minigame) é vazia; `MinigameSet`, handicaps, bonusStars, hidden blocks são resquícios.
5. Subclasses de `GameState` (FantasticFactory, TwistedTowers, ...) são legado do jogo original — a partida usa `GameState` base.
6. Comparação por string de opções/itens: renomear texto quebra lógica silenciosamente.
7. Encoding: alguns arquivos têm `�` em comentários (salvos em ANSI). Salvar sempre **UTF-8**; os `.txt` de `Resources` têm acentos.
8. `BoardManager.Update` roda a colocação todo frame e cria componentes via `FindObjectOfType` (API obsoleta em Unity 6 → preferir `FindFirstObjectByType`).
9. **Identidade**: nada de nomes, textos ou arte de personagens de terceiros. Ícones de itens, Estrela, Moeda, selos de movimento e a textura da casa de item são **gerados por código** em `Assets/Scripts/Editor/IconesProcedurais.cs` (menu Ferramentas > Gerar Ícones Procedurais); para mudar um ícone, edite o desenho lá e gere de novo (sobrescreve os PNGs com o mesmo nome, então cenas e código não mudam). Fonte de UI: Fredoka. O dado especial chama `ExecutiveDiceBlock`. Os nomes internos do enum `BoardItem` e dos arquivos de ícone (BooBell.png, BowserSuit.png...) continuam os antigos e não aparecem na tela.
10. Placar compara jogadores com `>` — empates dividem colocação.
11. **IA ↔ jogo por texto**: `EasyBrain.Prompt` faz `switch` pelo texto da pergunta; sem match cai em `options[0]` (= 1ª opção na ordem original). **Use sempre as constantes de `Prompts` (`Board/AI/Prompts.cs`) nos dois lados** — nunca string literal. Alvos de item passam por `EasyBrain.EscolherAlvo` (nunca "Cancelar"/ele mesmo, salvo plano explícito). Bot decide bifurcação via `Intersection.AIPathChoice` (humano responde a pergunta de perfil).
12. `UIManager` guarda `choices` **invertida** (layout dos botões) e reverte de volta antes de chamar a IA. Não passe `choices` direto para a IA.
13. Dados múltiplos (Cofrinho/Super Dado/Fantasia): seguidores são filhos do dado líder; o líder dispara os seguidores em `TriggerRoll` (exceto dado remoto) e só se destrói quando todo o conjunto rolou. **Rede**: só a máquina do humano dono (`ctrl 0`) transmite, um pacote por dado com o índice no conjunto (`DiceBlock.IndiceNoConjunto`, ordem da hierarquia do prefab); quem recebe enfileira em `NetworkTurnManager.ReceberDadoRemoto` até o dado certo do jogador ativo existir (`Pronto`). Bots não transmitem (cada máquina calcula pelo `GameRng`). O timer de 45s só conta para humano local; o timeout transmite a opção escolhida automaticamente. Timer de turno (`NetworkTurnManager`, 45s, também local) **não conta em turno de bot** e, ao esgotar, chama `UIManager.ResponderPorTimeout()` (responde o prompt atual, nunca abre diálogo novo por cima).
13b. Itens usados que dão dinheiro são **consumidos** (Chave Secreta, Escudo Protetor — antes geravam dinheiro infinito). Os efeitos de Bebida Dourada, Mochila, Luva de Duelo, Apito do Chomp, Lâmpada (passa pela Estrela via `passHere`) e Apito do Wiggler ficam em `Player.cs` (`Usar*`). "Ver Tabuleiro" = câmera temporária de cima (`Player.VerTabuleiro`), peões ampliados, faixa lateral "toque para voltar", timer pausado (`NetworkTurnManager.TimerPausado`); não usa `ui.Dialogue` (a caixa cobria o tabuleiro).
13c. Testar partida só com bots: fazer backup de `PlayerPrefs` (`Match_*`, `LoadSavedGame`) e do save em `persistentDataPath`, pôr os 4 controllers = 1, `editor_play`, `Time.timeScale = 4`. O Editor tem **Error Pause** ligado: erros de timeout do próprio CLI pausam o Play — despausar (`EditorApplication.isPaused = false`) a cada consulta. Restaurar prefs/save no fim.
14. **Sorteio de jogo = `GameRng` (`Data/GameRng.cs`), nunca `UnityEngine.Random`**. Online é lockstep (só dados de humanos e escolhas trafegam); o host gera `GameMatchConfig.SeedPartida` e manda no `LobbyStartGame` (`intPayload`); `BoardManager.Awake` faz `GameRng.Init(seed)` ANTES de criar os `PlayerState` (profissão é sorteada no construtor) e salva a seed no `GameState`. Dado de bot também sai do `GameRng`. `UnityEngine.Random` só para efeito visual (rotação de moeda). Ao fim de cada rodada `NetworkTurnManager.RegistrarFimDeRodada` troca um checksum do estado (`GameChecksum`); divergência vira aviso no diálogo "Fim da Rodada" e `Debug.LogError` com a seed.
14b. **Economia (rebalanceada 2026-11)**: líquido por turno +32..+48 conforme a profissão (antes +26..+60). Estrela com preço progressivo `ClassicStarSpace.PrecoEstrela` = 20 + 10 × estrelas do jogador (casa de passagem: compra em `pass()`). Quiz certo = 6 + 2×nível + 5% do salário. Casas azul/vermelha: valores em moedas ×3 na categoria 0 (`BoardSpace.EscalarMoedasCasa`). **Promoção por experiência**: a cada 30 casas andadas (`PlayerState.CasasPorPromocaoExperiencia`), anunciada ao fim do movimento — substituiu "a cada 2 voltas", que dependia da rota (anéis de 16 a 60 casas). Tabuleiro é feito de **anéis** ligados por `CareerTransitionSpace.nextLoopStart`; nenhum anel tem rota que evite o portão.
14d. **Vitória**: `BoardManager.metaCarreira` por cena (MVP = 6, MapaRapido = 5; calibrado com bots: o líder chega por volta da rodada 12–13 de 20). Quem bate a meta encerra a partida e fica em 1º (`[META]` no detalhamento); senão vence a pontuação ao fim das rodadas. Partidas curtas (ex.: 10 rodadas) tendem a terminar por pontos. Diálogo "Objetivo da Partida" na abertura (`MostrarAbertura`). Save a cada turno com `GameState.proximoJogador`; carregar retoma na vez certa. Save de jogo fora do git (`.gitignore`).
14c. `getTurnStatus()` vai de 0 a 4, mas a tabela `odds` do `ItemSpace` só tem 0..3 (clamp aplicado). Retratos dos jogadores no HUD (`PlayerTracker` Avatar/Picture) não têm textura: aparecem como quadrados pretos (falta arte).
15. Identidade do projeto: produto **"Caminho do Sucesso"**, empresa **DJDE**, pacote **`com.djde.caminhodosucesso`** (Standalone/Android/iOS), igual ao `MobileBuildTools`. Mudar empresa/produto move `persistentDataPath` e as PlayerPrefs.

---

## 9. Motor gráfico, shaders e visual

**Estado atual (pós-migração URP)**
- **URP 17.3 + Linear**. Assets em `Assets/Settings/URP/`: `URP_Renderer` (Forward, com PostProcessData) e 3 tiers — `URP_Low` (Very Low/Low: sem HDR, sem sombra, sem MSAA), `URP_Medium` (Medium/High + Graphics default: HDR, MSAA 2x, sombra 1024/50m), `URP_High` (Very High/Ultra: MSAA 4x, sombra 2048/80m, 2 cascatas).
- Câmera perspectiva FOV 60 **por jogador** (filha do prefab Player, habilitada só no turno).
- 1 luz direcional, skybox procedural padrão, **ambiente Trilight** (MVP e MapaRapido) — o modo Skybox deixava as sombras pretas sem "Generate Lighting". Sem bake, sem probes, sem fog.
- **Nenhum shader customizado**: materiais `Standard` → `URP/Lit` (122), `Legacy/Mobile Diffuse` → `URP/Simple Lit` (99), `Sparkle.mat` → `URP/Particles/Unlit` (cutout). TMP, `GUI/Text Shader` e skybox ficaram como estão (funcionam em URP).
- Script de migração idempotente: `Assets/Scripts/Editor/URPMigration.cs` (`URPMigration.Report()` mostra o estado; dá para apagar quando não for mais útil).
- **Pós-processamento**: `Global Volume` (layer Default) em MVP e MapaRapido com `Assets/Settings/URP/PostFX_Tabuleiro.asset` — Tonemapping Neutral, Color Adjustments (exposure +0.15, contraste 12, saturação 18), Bloom (threshold 1, intensidade 0.6), Vignette 0.22. Câmera do prefab `Player` com `renderPostProcessing = true` (instâncias herdam). Em `URP_Low` (sem HDR) o tonemapping/bloom praticamente não atuam. Setup idempotente: `URPPostProcessing.CreateProfile()` / `SetupScene(path)` / `SetupPlayerPrefab()`.
- Cenário de asset packs low-poly em `Assets/Thirdparty` (Buildings, European Buildings, Low Poly Pack, Palmov Island, Park, CubexCube, Decorations). Tabuleiro: `Models/Paws/Tabletop.fbx` + prefabs em `Resources/Prefabs/Stages/MVP`.
- Sem pós-processamento, sem tweening (movimento via `Vector3.MoveTowards`), quase nenhum áudio (`coin.ogg`, `powerup.ogg`, `tempMusic.ogg`).
- Qualidade: perfis Very Low → Ultra; AA desligado até High.

**Caminhos de melhoria (ordem sugerida)**
1. ~~Migrar para URP~~, ~~Linear~~ e ~~Volume global de pós~~ — feito. Ajuste fino de valores no perfil `PostFX_Tabuleiro` conforme gosto.
2. Revisar cores de UI/HUD em Linear (podem ter ficado um pouco mais claras).
3. **Estilo toon/low-poly coeso**: Shader Graph com ramp/cel shading + rim light + outline (inverted hull ou renderer feature) para unificar asset packs diferentes; casas com emissão por tipo (azul bonificação, vermelho penalidade, roxo quiz, dourado estrela) e pulso ao pousar.
4. **Iluminação**: bake de lightmaps + light probes para o cenário estático; 1 direcional realtime para sombras dos peões; reflection probe único.
5. **Juice**: tween de pulo casa-a-casa (curva parabólica em vez de `MoveTowards`), squash & stretch, partículas de moedas/estrela, câmera com follow suave/Cinemachine, shake leve em penalidades (skill `unity:craft-game-floors`).
6. **Performance mobile**: combinar meshes estáticas / static batching, atlas de texturas dos asset packs, GPU instancing nos materiais das casas, limitar `pixelLightCount`, remover recalcs por frame no `BoardManager`.
7. **UI**: menus feitos em código — manter o padrão; se for refazer, considerar UI Toolkit (skill `unity:ui`).

---

## 9a. Sistema financeiro / Banco (2026-11-10)

- **Carteira** em `PlayerState` (serializada, no checksum): `poupanca` (3%/turno, compostos), `reserva` (não rende; paga imprevistos primeiro), `acoes` (variam com a Bolsa), `divida` (8%/turno; parcela automática de 10%, mín. 5, no holerite). Empréstimo de 50 por vez até o teto `LimiteDivida` (300).
- **Holerite** (`Player.TakeTurn`): salário − despesas (se negativo, a falta vira dívida) + `ProcessarMesFinanceiro()` (juros da poupança, juros e parcela da dívida).
- **Imprevistos** (casa vermelha: gasto, prejuízo, multa) usam `PagarImprevisto`: reserva → moedas → cheque especial (vira dívida). Cortes de salário/rebaixamento não são cobertos.
- **Bolsa**: `BoardManager.HandleEndOfRound` sorteia uma variação de −12% a +15% (`GameRng`, igual para todos) e mostra no "Fim da Rodada".
- **Banco no menu do turno** (não gasta o turno): Poupança / Reserva / Ações / Empréstimo / Voltar, com submenus 20/50/resgatar tudo. Textos começam com as constantes `Prompts.Banco*` (1ª linha fixa, 2ª linha com saldos) e a IA reconhece por `StartsWith`.
- **IA** (`EasyBrain.PlanejarBanco`, uma operação por turno, zerada em `InicioDoTurno`): quitar/pagar dívida → sacar se sem liquidez → montar reserva (até 30) → poupança/ações com o excedente acima do preço da Estrela + margem. Bots não pegam empréstimo.
- **Pontuação** usa **patrimônio líquido** (moedas + aplicações − dívidas); Estrelas só se compram com moedas (liquidez). Resultado grava o patrimônio em `Player{n}Money`.

## 9b. Padrão de UI (2026-11-10)

- **Tudo passa pelo `TemaUI` (`Assets/Scripts/UI/TemaUI.cs`)**: paleta (painel verde-escuro `#0F2A1F`, cartão claro `#F0FDF4`, ouro `#FBBF24`, verde/vermelho/azul), fonte Fredoka, sprites arredondado/círculo gerados, fábricas `Texto`, `Botao` (estilos Claro/Destaque/Perigo/Escuro), `Slider`, `Interruptor`, `Segmentado`, e `EstilizarBotao/EstilizarTexto/SombraTexto/ContornoTexto` para UI existente. Não definir cores/fontes soltas em UI nova.
- **uGUI legado (`UnityEngine.UI.Text`)**: com Fredoka a entrelinha é alta; caixa de texto baixa demais + `VerticalWrapMode.Truncate` = o texto **some**. Use `Overflow` ou caixa alta o bastante.
- **Filhos desenham por cima do pai**: fundo criado como filho de um Text cobre o texto (era o bug do "Rodada X/Y" ilegível).
- **`Outline` sobre imagem semitransparente "mancha" a cor** (as cópias do contorno aparecem através). Janelas modais usam `TemaUI.Janela` (opaco); destaque de vez no HUD é cor de fundo mais clara, não contorno.
- **Transparência em Linear**: a UI mistura em espaço linear, então véus/sombras escuros semitransparentes escurecem bem menos do que o alfa sugere (preto 50% ≈ 73% do brilho; 72% ≈ 57%). Para escurecer de verdade use alfa alto (`TemaUI.Veu` = 0.86 ≈ 40%). Não é mistura aditiva (já foi medido: preto 60% sobre branco = 0.667).
- `GetComponent<Shadow>()` pode devolver um `Outline` (herança): use `SombraPura` do TemaUI para não empilhar componentes ao restilizar.
- **Configurações**: `Data/Configuracoes.cs` (PlayerPrefs `Cfg_*`, aplicado em toda cena) + `UI/MenuConfiguracoes.cs` (pausa no tabuleiro com botão II e Esc/Voltar; "Configurações" no canto do menu principal). Partida local pausa com `Time.timeScale = 0`; online não pausa. Com o menu aberto, `MenuConfiguracoes.Aberto` bloqueia avançar/rolar: use sempre `MobileInputManager.ConfirmarPressionado()` em vez de ler Espaço/toque direto.
- **Tela de resultado** (`GameOverController`) é montada em código com o tema; a UI antiga da cena é desligada no Start.

## 9c. Quiz (Bloco D, 2026-11-10)
- **Banco**: `quiz_nivel_0..3.txt` com 25 perguntas cada (Finanças Pessoais / Trabalho e Crédito / Investimentos / Negócios e Estratégia — nome da categoria aparece no speaker). Limites para caber na UI: pergunta ≤ ~90, opção ≤ ~48, explicação ≤ ~120 caracteres (diálogo cabe 3 linhas a 40px; tem best-fit 28–40 como rede de segurança).
- **Sem repetição**: `QuestionSpace.Proxima(cat)` usa um baralho (`Queue<int>`) por categoria, embaralhado com `GameRng` e refeito quando `GameRng.Seed` muda — determinístico no online. Só repete depois de esgotar as 25.
- **Feedback**: acerto → speaker `Correto! +N Moedas` + explicação; erro → speaker `Quase!` + `Resposta certa: X` + explicação.
- **IA**: `QuestionSpace.PerguntaAtual` fica setada enquanto a pergunta está na tela; `EasyBrain.Prompt` acerta com chance 35% + 15% × nível de educação (via `GameRng`).
- **Bots decidem uma vez por prompt**: o ramo de bot do `UIManager` só chama `Prompt` com `mostRecentAns` vazio (antes era chamado várias vezes no mesmo frame e consumia `GameRng` a mais). Botões de opção: 560×64 com best-fit 17–28.

## 9d. Como Jogar (TutorialManager, 2026-11-10)
- Janela própria (`[ComoJogar]`, canvas overlay ordem 950) feita com o TemaUI: 10 páginas com título, subtítulo e linhas "ícone + rótulo + descrição"; Voltar/Próximo ("Começar!" na última), bolinhas clicáveis, ✕, teclas ←/→/Enter/Esc. Abre pelo botão do menu e na primeira execução (`TutorialVisto`).
- Números vêm das constantes (`PlayerState.JurosPoupanca`, `JurosDivida`, `ValorEmprestimo`, `LimiteDivida`, `CasasPorPromocaoExperiencia`) e os itens de `ItemSpace.itemNames/itemDescriptions` + `texture/icons/{BoardItem}`: ao mudar uma regra, confira os textos fixos das páginas (pontuação, casas, quiz).
- Máx. ~7 linhas por página (a altura da linha se ajusta). Em Unity, não use `GetComponent<T>() ?? AddComponent<T>()` (o "null" do Unity não é null em C#); use `TryGetComponent`.

## 9e. Acabamento / feedback (Bloco E, 2026-11-11)
- **HUD (`PlayerTracker.DetectarMudancas`)**: compara moedas/estrelas/carreira/educação com o quadro anterior e dispara números flutuantes (`AvisosTela.Flutuar`), pulo do número e sons. O contador de moedas "corre" até o valor novo. Não precisa chamar nada nos pontos onde o dinheiro muda.
- **`AvisosTela`**: `Faixa()` (aviso no topo, abaixo dos cartões; usado pela Bolsa em `BoardManager.AvisarBolsa`), `Flutuar()` e `Pulo()`. Tempo não escalado com passo limitado a 0,1 s.
- **`Sons`**: efeitos sintetizados em código (Acerto, Erro, Estrela, Promocao, Formatura, BolsaAlta/Baixa, Ganho/Perda), volume = "Efeitos sonoros". Gatilhos: quiz (QuestionSpace), holerite (Player), Bolsa (BoardManager), estrela/promoção/formatura (detectados no HUD), fim de jogo.
- **Casas (`CasaVisual`)**: cópia em runtime de cada material de casa com emissão (brilho por tipo; Estrela e Carreira "respiram"); `BoardSpace.landHere` chama `CasaVisual.Pulsar` (escala + flash via MaterialPropertyBlock, depois limpa). Os assets de material não são alterados.
- **Resultado**: `PlayerState.historicoPatrimonio` (índice 0 = início, depois 1 por rodada; vai no save) → `EndGame` grava `Player{n}Historico/CareerLevel/Color` → `GameOverController` mostra barra empilhada da pontuação (mesma escala para todos) e gráfico de linhas; cores parecidas de jogadores são trocadas por outras da paleta (comparação por matiz).
- Nada disso usa `GameRng` (só apresentação): não afeta o lockstep online.

## 9f. Mapas gerados por código (2026-11-11)
- **Ilha das Oportunidades** (`IlhaOportunidades.unity`, 3º mapa do lobby): gerada por `Editor/GeradorIlhaOportunidades.cs` (menu Ferramentas > Mapas). Parte da cena do Circuito Rápido (UI, jogadores, BoardManager) e troca todo o tabuleiro. **Não edite a cena à mão**: mude o gerador e gere de novo (ele apaga e recria a cena).
  - Grade de células (`U = 3.2`); cada `Trecho("tipos", x0,y0,x1,y1)` é uma rua reta com uma casa por célula; `Proxima/Opcao/Saida` ligam `next`/`option`/`nextLoopStart`. Ruas, calçadas e pontes saem das ligações entre células vizinhas.
  - Anel 1 Vila Aprendiz (atalho arriscado), anel 2 Centro Comercial (um "8" com dois circuitos que se cruzam no portão), anel 3 Distrito Corporativo (atalho pela Avenida Central; portão final promove a cada volta). Rio em L separa os bairros; avenidas de promoção cruzam em pontes.
  - Portões: Nv1/Edu1/40 (máx 1) → Nv2/Edu1/80 (máx 3) → Nv3/Edu1/150. Meta Nv6. Calibrado com bots: termina por volta da rodada 13–19.
  - As casas ficam sob `Board/Ilha/Casas` (o registro do BoardManager só vê filhos do `Board`), com escala (0.67, 0.57, 0.67) para igualar o Circuito Rápido. Materiais próprios em `Assets/Materials/IlhaOportunidades`.
  - Frente dos modelos: pacote `Buildings` olha para −Z (offset 180°), `European Buildings`/Palmov/CubexCube para +Z (`OffsetFrente`).
- **MVP.unity corrigido (release 1.0.0)**: o MVP original tinha um beco sem saída depois do portão 40, 22 casas soltas empilhadas, trechos com casas sobrepostas e ids repetidos. Foi substituído pela versão corrigida (3º anel ligado por uma bifurcação, pistas paralelas onde os anéis 1 e 2 se cruzavam). O script de correção foi removido (estava no histórico do git).
- Lobby: `GameMatchConfig.Mapas` / `ProximoMapa` / `TurnosSugeridos` centralizam a lista de mapas (Local e Online).
- Pela Unity CLI, `eval` que demora mais de 5 s dá timeout no CLI mas continua rodando no Editor (confira o log). Capturas da Scene View só podem ser salvas dentro do projeto.

## 10. Convenções ao contribuir

- Toda string de jogador em **PT-BR** com acentuação correta; arquivos UTF-8.
- Nova casa: herdar `BoardSpace`, sobrescrever `setup()` (definir `blueChance`, `canLandHere`), `land()`/`pass()` controlando `doneLanding`/`donePassing`, implementar `AIValue()`, criar prefab em `Prefabs/Board`, dar `id` único e ligar `next` na cena.
- Novo conteúdo de bônus/penalidade/quiz: editar `Assets/Resources/*_nivel_{0..3}.txt` (um por linha, `|` como separador) e tratar o novo `tipo` no `switch` da casa.
- Novo item: adicionar ao **fim** do enum `BoardItem` (o índice é usado em `itemNames`/save), adicionar nome e descrição nas listas na mesma posição, preço em `ItemShop.FixedPrices`, tier, e o `case` em `Player.TakeTurn()`.
- Mudança de economia: considerar o holerite por turno, multiplicadores de carreira e a fórmula de pontuação juntos.
- Estado novo de jogador/partida → campo `[SerializeField]` em `PlayerState`/`GameState` (save JSON) e, se for online, mensagem em `NetworkProtocol`.
- Preferir `unity command` (Unity CLI/Pipeline) para editar cenas/prefabs com o Editor aberto.
