# FrontierTD *(título provisório)*

Tower defense com uma mecânica de **fronteira territorial + atrito** (inspirada em Rise of Nations):
torres projetam uma fronteira no terreno e inimigos dentro do seu território perdem vida com o tempo.
Projeto-treino antes do RTS — alvo: Steam, com arquitetura mobile-ready desde o dia 1.

## Como abrir (primeira vez)

1. Abra o **Unity Hub** → **Add** → **Add project from disk** → escolha esta pasta.
2. Abra o projeto com o **Unity 6000.3.11f1** (se o Hub reclamar da versão, escolha essa na lista).
3. A primeira abertura demora alguns minutos: o projeto instala e ativa o **URP sozinho**
   (acompanhe as mensagens `[FrontierTD]` no Console).
4. Aperte **Play**. Não precisa abrir cena nenhuma — o jogo se monta sozinho em qualquer cena vazia.

## Controles

| Ação | Controle |
|---|---|
| Construir torre (25 de ouro) | Clique esquerdo |
| Mover câmera | WASD / setas / arrastar com botão do meio |
| Zoom | Scroll |
| Chamar a próxima onda | Espaço |
| Reiniciar | R |

O fantasma verde/vermelho mostra onde pode construir. Não dá para murar o caminho por
completo — o jogo bloqueia a torre que fecharia a última passagem.

## O que está implementado (fase 0)

- Grid lógico + chão quadriculado gerado em código
- **Flow field pathfinding** (Dijkstra 8 direções, custo 10/14, sem cortar quinas) —
  todos os inimigos compartilham um único campo; é a técnica que o RTS usará depois
- Ondas infinitas com HP escalando; economia (ouro por abate + bônus por onda)
- Torres com mira, projéteis teleguiados, **object pooling** em tudo (zero alocação em regime)
- Câmera RTS (pan/zoom) e HUD provisório via OnGUI
- **Camada de input abstrata** (`IGameInput`): o jogo consome intenções, não cliques —
  é o que torna o porte mobile um adaptador novo, não um retrofit
- **Fronteira + atrito** — a mecânica-teste do projeto: cada torre projeta território
  (overlay azul com linha de fronteira); inimigos dentro dele sofrem dano contínuo,
  sem ninguém atirar. Vencer controlando território, não só matando.

## Testes headless

A lógica pura (grid, flow field, território) compila e roda **fora do Unity** — os mesmos
`.cs` do projeto, com stubs mínimos:

```bash
cd Tools/FlowSim
dotnet run
```

22 verificações: conversões de grid, alcançabilidade, anti-muro (`PlacementBlocksPath`),
proibição de corte de quina, simulação de caminhada spawn→base e geometria do território.
É o embrião da ferramenta de balanceamento da fase 3 (simular milhares de ondas sem abrir o editor).

## Próximo passo

Playtest do Felipe → tuning de `BorderRadius`/`AttritionDps` em `GameConfig.cs` →
**decisão da semana 4**: fronteira + atrito diverte, ou o jogo vira TD clássico?

## Arquitetura em uma linha

Tudo nasce de `GameBootstrap` → `GameController` monta o mundo inteiro em código
(primitivas + materiais gerados em runtime, sem prefabs e sem cena montada à mão) e
dirige os sistemas por frame em ordem determinística. Balanceamento: `GameConfig.cs`.
