# Instruções de Configuração - Menu de Detalhes do Jogador

## Como configurar o sistema de menu detalhado dos jogadores

### 1. Criar o GameObject do Menu de Detalhes

1. Na hierarquia do Unity, crie um novo GameObject chamado "PlayerDetailMenu"
2. Adicione o componente `PlayerDetailMenu` ao GameObject
3. Configure a estrutura de UI conforme descrito abaixo

### 2. Estrutura de UI Recomendada

Crie a seguinte hierarquia de UI:

```
PlayerDetailMenu (Canvas ou Panel)
├── DetailPanel (Panel)
│   ├── Background (Image) - Para detectar cliques fora do menu
│   ├── Header (Panel)
│   │   ├── PlayerName (Text)
│   │   └── PlayerColorIndicator (Image)
│   ├── Content (Panel)
│   │   ├── ProfessionInfo (Panel)
│   │   │   ├── PlayerProfession (Text)
│   │   │   └── PlayerCareerLevel (Text)
│   │   ├── FinancialInfo (Panel)
│   │   │   ├── PlayerMoney (Text)
│   │   │   ├── PlayerCoins (Text)
│   │   │   └── PlayerStars (Text)
│   │   ├── EducationInfo (Panel)
│   │   │   └── PlayerEducation (Text)
│   │   ├── InvestmentInfo (Panel)
│   │   │   └── PlayerInvestments (Text)
│   │   └── StatsInfo (Panel)
│   │       └── PlayerStats (Text)
│   └── Footer (Panel)
│       └── CloseButton (Button)
```

### 3. Configuração dos Componentes

No script `PlayerDetailMenu`, arraste os seguintes elementos para os campos correspondentes:

- **Detail Panel**: O painel principal do menu
- **Player Name Text**: Texto que mostra o nome do jogador
- **Player Profession Text**: Texto que mostra a profissão
- **Player Career Level Text**: Texto que mostra o nível de carreira
- **Player Money Text**: Texto que mostra o dinheiro
- **Player Coins Text**: Texto que mostra as moedas
- **Player Stars Text**: Texto que mostra as estrelas
- **Player Education Text**: Texto que mostra o nível de educação
- **Player Investments Text**: Texto que mostra os investimentos
- **Player Stats Text**: Texto que mostra estatísticas gerais
- **Close Button**: Botão para fechar o menu
- **Player Color Indicator**: Imagem que mostra a cor do jogador

### 4. Estilização Sugerida

- Use um fundo semi-transparente para o painel principal
- Configure o `Background` como um botão para detectar cliques fora do menu
- Use cores contrastantes para melhor legibilidade
- Considere usar um layout responsivo que se adapte a diferentes resoluções

### 5. Funcionalidades

O sistema agora oferece:

- **Clique nos PlayerTrackers**: Clique em qualquer caixa de jogador para abrir o menu detalhado
- **Informações Completas**: Nome, profissão, carreira, dinheiro, educação, investimentos e estatísticas
- **Fechamento Intuitivo**: Clique no botão fechar ou fora do menu para fechar
- **Atualização Automática**: O menu se atualiza quando as informações do jogador mudam

### 6. Integração Automática

O sistema se integra automaticamente com:
- `PlayerTracker`: Adiciona funcionalidade de clique automaticamente
- `UIManager`: Gerencia a conexão entre os componentes
- `PlayerState`: Acessa todas as informações do jogador

### 7. Personalização

Você pode personalizar:
- O layout e design do menu
- Quais informações são exibidas
- O formato dos textos
- As cores e estilos visuais

O sistema é flexível e pode ser facilmente modificado para atender às suas necessidades específicas.
