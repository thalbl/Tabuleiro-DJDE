---
name: contexto-projeto
description: Contexto do jogo "Mario Party Star Crossed" re-tematizado como simulador de vida financeira/carreira em PT-BR (Unity 6, Built-in RP). Use SEMPRE antes de mexer em gameplay, espaços do tabuleiro, itens, economia (moedas/salário/despesas), carreira/educação, turnos, IA, UI de diálogo, multiplayer local/online, save, cenas ou visual/shaders deste projeto.
---

# Contexto do Projeto — Simulador de Vida Financeira (base Mario Party)

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
| `QuestionSpace` (12/9) | Question Space | **Quiz financeiro** | `quiz_nivel_{cat}.txt`; acerto = `3+nível` moedas + `salário×1%×nível`; opções embaralhadas |
| `EducationSpace` (7/5) | EducationSpace | **Universidade** | Paga `educationCost` (75) → `IncreaseEducation()` |
| `CareerTransitionSpace` (3/3) | CareerTransitionSpace | **Transição de carreira** | Ao *passar*: se carreira ≥ req, educação ≥ req e moedas ≥ `minSavings` (200) → `Promote()` e desvia para `nextLoopStart` (próximo "anel"). Promoção por mérito automática (salário ≥75, moedas ≥80, nível <3). Não consome passo |
| `ClassicStarSpace` (2/2) | Star Space | **Estrela** | 20 moedas = 1 estrela (40 = 2 com "Moeda Dobrada") |
| `ItemSpace` (3/2) | Item Space | "Consultor Financeiro" | Dá item sorteado por tiers |
| `ItemShop` (5/2) | Shop | **Loja** | Não consome passo; preços fixos (2–15 moedas) |
| `Intersection` (2/2) | Intersection90 | **Bifurcação** | Escolha de caminho; usa `Resources/perguntas.txt` (perguntas de perfil 2 opções); prismas "phantom" mostram a rota |
| `NothingSpace` (13/1) | Board Space | Neutra | "Nada aconteceu!" |

**Categoria por nível de carreira** (Blue/Red/Question): Nv 1–3 → 0 · 4–6 → 1 · 7–8 → 2 · 9–10 → 3.
Formato dos `.txt`: uma entrada por linha, campos separados por `|` (quiz: `pergunta|correta|op2|op3|op4|op5`).

**Legado não usado nas cenas (inglês, temática Mario):** `Boo`, `BowserSpace`, `DuelSpace`, `LuckySpace` (já parcialmente traduzida), `TestHappeningSpace`, `SkeletonGate` (só 1 no MVP). Ao reaproveitar, traduzir e re-tematizar.

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
| BooBell | Sino do Boo | rouba moedas (grátis) ou estrela (40) | 9 |
| TweesterTotem | Ventania | manda rival p/ casa aleatória | 5 |
| DuelingGlove | Luva de Duelo | — (duelo não existe) | 7 |
| ChompCall | Apito do Chomp | — | 3 |
| Gaddlight | Escudo Protetor | +20 moedas; protege do Boo | 4 |
| MagicLamp | Lâmpada Mágica | — | 15 |
| BowserSuit | Fantasia do Bowser | dado "Bowser"; rouba 20 de quem passar | 10 |
| DoubleStarCard | Moeda Dobrada | +30 moedas / 2 estrelas na Star | 12 |
| ChompTreat | Presente Surpresa | +25 moedas | 3 |
| WigglerWhistle | Apito do Wiggler | — | 8 |

`state.setMovement(n)` escolhe o prefab de dado: 0 normal, 1 duplo, 2 triplo, 3 mágico, 4 veneno, 5 Bowser.
⚠️ Há **três vocabulários** para os mesmos itens: comentários do enum ("Poupança", "Bolsa de Valores"...), `itemNames` ("Cofrinho", "Super Dado"...) e textos do `LuckySpace` ("Ganhe uma Poupança"). O canônico na UI é `itemNames`.

---

## 7. Sistemas existentes

- **Save/Load**: `GameState` serializado com `JsonUtility` em `Application.persistentDataPath/mario_party_save.dat` (fallback legado `Assets/Data/`). Cena salva em `PlayerPrefs["SavedGame_MapScene"]`. `PlayerPrefs["LoadSavedGame"]==1` faz o `BoardManager` carregar. **Todo campo novo de estado deve ser `[SerializeField]` e compatível com JsonUtility** (sem Dictionary, sem propriedades).
- **IA**: `EasyBrain.Prompt()` decide por heurística usando `AIValue/CumulativeValue` das casas. Toda casa nova precisa implementar `AIValue` e a IA precisa reconhecer as strings de opção novas.
- **Multiplayer local**: até 4 humanos/bots, opção Pass & Play.
- **Multiplayer online**: `NetworkTransportService` (TCP host/cliente, LAN/Hamachi/IP público), `NetworkProtocol` (pacotes JSON: lobby, `GameTurnStart`, `GameDiceRoll`, `GameOptionChoice`, `GameEmote`, `GameTimeout`, heartbeat), `NetworkTurnManager` (timer de turno, sincroniza escolhas), `DiceBlock.ForcarRolagemRemota`.
- **HUD**: `PlayerTracker` (4 painéis), `PlayerDetailMenu`, `TurnTimerUI`, `EmoteReactionUI`, contador de movimento.
- **Mobile**: `MobileInputManager` (swipe/toque), `SafeAreaFitter`, `CanvasResolutionManager`, `HapticFeedback`, `MobileBuildTools` (editor).
- **Tutorial**: `TutorialManager` (slides em código).

---

## 8. Armadilhas conhecidas (verifique antes de "consertar" ou depender)

1. **Salário = salário do nível + `ajusteSalario`** (mín. 10). `SetBaseSalary(novo)` grava a diferença em `ajusteSalario`, que sobrevive a `Promote/Demote` e ao save. Nunca escreva em `salarioAtual` direto; saves antigos são migrados em `OnAfterDeserialize` (flag `ajusteSalarioMigrado`).
2. `Random.Range(0, list.Count - 1)` (int, topo exclusivo) em `stealRandomItem`, `NewStarPos`, etc. → o último elemento nunca é sorteado.
3. `CompleteLap()` (promoção a cada 2 voltas) e `AddInvestment()` **não são chamados** em lugar nenhum.
4. Fase 9 (minigame) é vazia; `MinigameSet`, handicaps, bonusStars, hidden blocks são resquícios.
5. Subclasses de `GameState` (FantasticFactory, TwistedTowers, ...) são legado do jogo original — a partida usa `GameState` base.
6. Comparação por string de opções/itens: renomear texto quebra lógica silenciosamente.
7. Encoding: alguns arquivos têm `�` em comentários (salvos em ANSI). Salvar sempre **UTF-8**; os `.txt` de `Resources` têm acentos.
8. `BoardManager.Update` roda a colocação todo frame e cria componentes via `FindObjectOfType` (API obsoleta em Unity 6 → preferir `FindFirstObjectByType`).
9. Texto legado em inglês com personagens Nintendo (Bowser, Boo, Boom Boom). Não introduzir mais conteúdo/nomes/arte de Mario: o objetivo é um jogo **original** de educação financeira; ao tocar nesses pontos, re-tematizar.
10. Placar compara jogadores com `>` — empates dividem colocação.

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

## 10. Convenções ao contribuir

- Toda string de jogador em **PT-BR** com acentuação correta; arquivos UTF-8.
- Nova casa: herdar `BoardSpace`, sobrescrever `setup()` (definir `blueChance`, `canLandHere`), `land()`/`pass()` controlando `doneLanding`/`donePassing`, implementar `AIValue()`, criar prefab em `Prefabs/Board`, dar `id` único e ligar `next` na cena.
- Novo conteúdo de bônus/penalidade/quiz: editar `Assets/Resources/*_nivel_{0..3}.txt` (um por linha, `|` como separador) e tratar o novo `tipo` no `switch` da casa.
- Novo item: adicionar ao **fim** do enum `BoardItem` (o índice é usado em `itemNames`/save), adicionar nome e descrição nas listas na mesma posição, preço em `ItemShop.FixedPrices`, tier, e o `case` em `Player.TakeTurn()`.
- Mudança de economia: considerar o holerite por turno, multiplicadores de carreira e a fórmula de pontuação juntos.
- Estado novo de jogador/partida → campo `[SerializeField]` em `PlayerState`/`GameState` (save JSON) e, se for online, mensagem em `NetworkProtocol`.
- Preferir `unity command` (Unity CLI/Pipeline) para editar cenas/prefabs com o Editor aberto.
