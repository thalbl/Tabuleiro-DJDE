# Instruções de Configuração - Canvas Resolution Manager

## Objetivo
Este script garante que a UI mantenha a proporção correta independente da resolução da tela, ajustando-se automaticamente quando a resolução muda em tempo real.

## Como Configurar

### Passo 1: Adicionar o Script ao Canvas
1. Selecione o GameObject **Canvas** na hierarquia da sua cena
2. No Inspector, clique em **Add Component**
3. Procure por **Canvas Resolution Manager** e adicione-o
4. O script automaticamente adicionará os componentes necessários (Canvas e CanvasScaler) se não existirem

### Passo 2: Configurar as Propriedades

#### Resolução de Referência
- **Reference Resolution X**: Largura da resolução de referência (padrão: 1920)
- **Reference Resolution Y**: Altura da resolução de referência (padrão: 1080)
- Configure estes valores para a resolução na qual você projetou sua UI

#### Match Width or Height
- **Match Width Or Height Value**: Valor entre 0 e 1
  - **0.0** = Prioriza largura (UI escala baseado na largura da tela)
  - **1.0** = Prioriza altura (UI escala baseado na altura da tela)
  - **0.5** = Balanceado (recomendado para a maioria dos casos)

#### Monitoramento em Tempo Real
- **Monitor Resolution Changes**: Deixe marcado para ajustar automaticamente quando a resolução mudar
- **Check Interval**: Intervalo em segundos para verificar mudanças (0.1 = verifica 10 vezes por segundo)

### Passo 3: Verificar o Canvas Scaler
O script configura automaticamente o Canvas Scaler, mas você pode verificar:
1. Selecione o Canvas
2. No componente **Canvas Scaler**:
   - **UI Scale Mode**: Deve estar em "Scale With Screen Size"
   - **Reference Resolution**: Deve corresponder aos valores configurados no script
   - **Screen Match Mode**: Deve estar em "Match Width Or Height"

## Dicas Importantes

### Para Elementos de UI Responsivos
- Use **Anchors** e **Pivots** corretamente nos RectTransforms
- Evite posições fixas em pixels - use anchors relativos
- Para elementos que devem manter tamanho fixo, use anchors no centro

### Para Textos
- Configure o tamanho da fonte considerando a resolução de referência
- Use **Best Fit** com cuidado (pode causar problemas de layout)

### Testando
1. Execute o jogo no Editor
2. Redimensione a janela do Game View
3. A UI deve ajustar-se automaticamente mantendo as proporções

## Solução de Problemas

### UI não está escalando
- Verifique se o Canvas tem o componente CanvasScaler
- Certifique-se de que "Monitor Resolution Changes" está ativado
- Verifique se a resolução de referência está correta

### UI está muito grande/pequena
- Ajuste a resolução de referência para corresponder ao design original
- Experimente diferentes valores de "Match Width Or Height Value"

### Performance
- Se notar problemas de performance, aumente o "Check Interval" para reduzir verificações

## Exemplo de Uso via Código

```csharp
// Obter referência ao manager
CanvasResolutionManager manager = FindObjectOfType<CanvasResolutionManager>();

// Alterar resolução de referência em tempo de execução
manager.SetReferenceResolution(1280, 720);

// Alterar o match value
manager.SetMatchWidthOrHeight(0.3f); // Prioriza largura

// Obter escala atual
float currentScale = manager.GetCurrentScale();
```

