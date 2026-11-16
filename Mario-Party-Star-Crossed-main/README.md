# Caminho do Sucesso

Versão **1.0.0**.

Jogo de tabuleiro para até 4 jogadores sobre **vida financeira e carreira**. Cada jogador recebe uma profissão, recebe salário e paga despesas a cada turno, estuda, é promovido e decide como usar o dinheiro enquanto anda pelo tabuleiro.

**Como vencer:** chegue à meta de carreira do mapa (Nv. 6 na Cidade Clássica e na Ilha das Oportunidades, Nv. 5 no Circuito Rápido) ou tenha a maior pontuação ao fim das rodadas.
Pontuação: Estrela ×100 + Moedas + Carreira ×50 + Educação ×75.

## Casas do tabuleiro

| Casa | O que acontece |
|---|---|
| Azul | Bonificação: aumento de salário, bônus ou PLR |
| Vermelha | Penalidade: corte de salário, despesa imprevista ou rebaixamento |
| Quiz | Pergunta de educação financeira; acertar rende moedas |
| Universidade | Investir em educação (Ensino Médio → Superior → Pós) |
| Transição de carreira | Com os requisitos de nível, educação e reserva, promove e leva ao próximo circuito |
| Estrela | Compra de Estrela (o preço sobe a cada Estrela que você já tem) |
| Loja / Consultor | Itens que mudam o movimento, protegem ou atrapalham os rivais |

## Mapas

| Mapa | Cena | Meta | Rodadas sugeridas |
|---|---|---|---|
| Cidade Clássica | `MVP` | Nv. 6 | 20 |
| Circuito Rápido | `MapaRapido` | Nv. 5 | 15 |
| Ilha das Oportunidades | `IlhaOportunidades` | Nv. 6 | 20 |

A Ilha das Oportunidades tem três bairros, um por anel de carreira (Vila Aprendiz, Centro Comercial e Distrito Corporativo), ligados por pontes sobre um rio. O botão **MAPA** do lobby alterna entre os três.

## Ficha do jogador

Tocar no card do jogador no HUD abre a ficha: colocação, moedas, estrelas e patrimônio, progresso até a meta de carreira, experiência, educação, finanças detalhadas, itens no bolso e pontuação atual.

## Modos

- **Local:** até 4 jogadores no mesmo aparelho (com opção Passa-e-Joga) e bots.
- **Online:** host e clientes por TCP (porta 7777) na mesma rede, Hamachi/ZeroTier ou IP público.

## Rodando o projeto

- Unity **6000.3.9f1** com **URP**.
- Abra `Assets/Scenes/StartScene.unity` e aperte Play.
- Cenas do build: `StartScene`, `MVP` (Cidade Clássica), `MapaRapido` (Circuito Rápido), `IlhaOportunidades` (Ilha das Oportunidades) e `GameOverScene`.
- Build Android: menu **Mobile → 1. Configurar Projeto para Mobile**.
- A Ilha das Oportunidades é gerada por código: menu **Ferramentas → Mapas** (`Assets/Scripts/Editor/GeradorIlhaOportunidades.cs`).
- O save da partida fica em `Application.persistentDataPath`, fora do repositório.

### O que não vai para o Git

O `.gitignore` deixa de fora tudo o que o Unity ou as ferramentas geram em cada máquina: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, projetos `.csproj`/`.sln`, saves locais e os arquivos do workspace do Plastic SCM (`.plastic/`, `ignore.conf`). Ao clonar, o Unity recria esses arquivos ao abrir o projeto.

## Estrutura

```
Assets/Scripts/
  Board/      partida (BoardManager), jogador, dados, casas e IA
  Data/       estado da partida e do jogador, configuração, sorteios (GameRng)
  Editor/     ferramentas do Editor (gerador da Ilha, build mobile, ícones, URP)
  Misc/       moedas, vibração e entrada no celular
  Network/    protocolo e transporte do online
  UI/         menus, HUD, diálogos, tutorial
Assets/Resources/   textos das casas (bônus, penalidades, quiz), fontes e ícones
Assets/Settings/URP/ pipeline e pós-processamento
```

## Licença e créditos

Código sob licença MIT (veja `LICENSE`). A base de código deriva de um projeto open-source de Jackson, também MIT.
Cenário com pacotes de assets de terceiros em `Assets/Thirdparty`, cada um com a própria licença.
