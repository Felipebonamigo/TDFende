# TDFende

Tower defense com uma mecânica de **fronteira territorial + atrito** (inspirada em Rise of Nations):
torres projetam uma fronteira no terreno e inimigos dentro do seu território perdem vida com o tempo.
Projeto-treino antes do RTS — alvo: Steam, com arquitetura mobile-ready desde o dia 1.

## Como abrir (primeira vez)

1. Abra o **Unity Hub** → **Add** → **Add project from disk** → escolha esta pasta.
2. Abra o projeto com o **Unity 6000.3.11f1** (se o Hub reclamar da versão, escolha essa na lista).
3. A primeira abertura demora alguns minutos: o projeto instala e ativa o **URP sozinho**
   (acompanhe as mensagens `[TDFende]` no Console).
4. Aperte **Play**. Não precisa abrir cena nenhuma — o jogo se monta sozinho em qualquer cena vazia.

Aparece um seletor com dois modos:

- **TD clássico** — uma lane, ondas infinitas (a fase 0).
- **Tower Wars** — sua lane contra a da IA, em três dificuldades. Você defende **e** compra
  inimigos para mandar na lane dela; cada envio sobe a sua renda para sempre.

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

No **Tower Wars**, a sua lane é a de baixo (mais perto da câmera) e a da IA é a de cima,
do outro lado do rio. As teclas **1-6** (ou os botões do rodapé) compram e enviam. O nível
de cada torre se lê nela mesma: a torre cresce e ganha o estandarte do time a partir do nível 2.

## O que está implementado (fase 0)

- Grid lógico (linhas finas sobre a relva, só para mirar a construção)
- **Flow field pathfinding** (Dijkstra 8 direções, custo 10/14, sem cortar quinas) —
  todos os inimigos compartilham um único campo; é a técnica que o RTS usará depois
- Ondas infinitas com HP escalando; economia (ouro por abate + bônus por onda)
- Torres com mira, projéteis teleguiados, **object pooling** em tudo (zero alocação em regime)
- Câmera RTS (pan/zoom) e HUD provisório via OnGUI
- **Camada de input abstrata** (`IGameInput`): o jogo consome intenções, não cliques —
  é o que torna o porte mobile um adaptador novo, não um retrofit
- **Fronteira + atrito** — a mecânica-teste do projeto: cada torre projeta território
  (linha de fronteira na cor do dono, estilo RoN); inimigos dentro dele sofrem dano contínuo,
  sem ninguém atirar. Vencer controlando território, não só matando.

## Direção de arte: realista, fim da Idade Média (R$ 0, tudo em código)

Decidida em 26/09/2026, trocando o "cartoon colorido". Continua **sem asset comprado**:
modelo e luz são gerados em código; a textura é **foto de verdade** onde havia foto livre
(pedra, madeira, ferro, couro, pano, casca, rocha, terra — CC0 e MIT, ver
[`THIRD_PARTY.md`](THIRD_PARTY.md)) e procedural no resto (relva, telhado, pele, cavalo, gelo, água).

- **Modelos** — [`Art/ModelLib.cs`](Assets/_Project/Scripts/Runtime/Art/ModelLib.cs): cada torre,
  inimigo, a fortaleza, o acampamento, os projéteis e o cenário são montados de peças
  (caixa, cilindro, perfil torneado, esfera deformada) em escala de gente (~0,6 de altura
  para um soldado). Silhueta própria por tipo, como pede o `SendCatalog`:
  - Torres: **Canhão** (torre redonda de cantaria, canhão de bronze em reparo de madeira),
    **Morteiro** (bastião octogonal baixo, morteiro apontado ao céu, barris de pólvora),
    **Gelo** (torre de pedra clara, cristal de gelo sob telhado de ardósia),
    **Sentinela** (torre de vigia de madeira com balista no alto),
    **Fogo** (torre baixa com braseiro aceso e sifão de fogo grego),
    **Ar** (torre com cabeça de moinho; as pás giram e aceleram a cada rajada)
  - Inimigos (bichos, do menor ao maior): **Rato** (vem em bando de 4), **Cachorro**, **Lobo**
    (veloz), **Javali** (presas e cerdas), **Águia** (voa sobre a fronteira), **Urso**, **Tigre**
    (gordo e rápido), **Rinoceronte** (dois chifres), **Elefante** (com torre de combate no lombo).
    A cor do time vai na coleira dos pequenos e na manta dos grandes
- **Texturas fotográficas** — `Assets/Resources/TDFende/Textures`: cor + normal map, recoloridas
  para a paleta. `MatSpec.External` diz qual material usa qual foto; se o arquivo faltar, o jogo
  cai no procedural sozinho
- **Texturas procedurais** — [`Art/ProcTex.cs`](Assets/_Project/Scripts/Runtime/Art/ProcTex.cs): cantaria,
  tábua, bronze com pátina, ferro com ferrugem, telha, ardósia, pano, couro, relva, terra,
  rocha, água... cor + normal map, tileáveis, geradas em paralelo no boot (`ArtFactory.Preload`)
- **Animação** — [`Art/ModelRig.cs`](Assets/_Project/Scripts/Runtime/Art/ModelRig.cs): torreta
  gira para o alvo, cano dá coice, soldado anda (a perna acompanha o chão percorrido), cavalo
  galopa, torre de cerco rola, planador balança, estandarte da fortaleza tremula
- **Mundo** — [`Art/WorldLayout.cs`](Assets/_Project/Scripts/Runtime/Art/WorldLayout.cs): relevo
  que só começa longe do tabuleiro (o grid continua plano), rio entre as lanes no Tower Wars,
  mureta de pedra seca em volta de cada lane, mata de pinheiros e carvalhos em volta
- **Cor de time** — azul você, vermelho a IA: estandarte, tabardo, xairel, escudo e a linha
  de fronteira. É a única cor de "jogo" que sobrou; o resto vem do material
- **`SceneAmbience`** — um sol só, céu físico procedural, ambiente em três faixas, névoa de
  horizonte e uma sonda de reflexo (metal só parece metal se tiver o que refletir)
- **`Vfx`** — partículas macias (shader `SoftParticle`): clarão e fumaça de pólvora, poeira e
  faísca no impacto, poeira e lascas na morte. Morte por **atrito** continua com cor própria
  (geada azulada) — dá para *ver* qual mecânica está matando
- **Feedback nos inimigos** — barra de vida sobre a cabeça, clarão no impacto e **brilho
  gelado sob atrito** (emissão por cima do material, sem apagar a cor do time)
- **`Urp/PostFx`** — tonemap ACES (fílmico), bloom contido, vinheta de lente, saturação um tico
  abaixo do neutro. Assembly *opcional*: só compila se o URP existir (`defineConstraints`)
- **HUD** — ainda OnGUI provisório, mas com pele própria (`UI/UiSkin.cs`): painel escuro,
  texto cor de pergaminho, botão de madeira com acento dourado
- **[`Palette.cs`](Assets/_Project/Scripts/Runtime/Core/Palette.cs)** — agora só as cores de
  leitura (time, fronteira, efeitos, interface); a cor das coisas mora nos materiais

**Trocar por asset de verdade depois, peça por peça:** um prefab em
`Assets/Resources/TDFende/<nome>` (ex.: `Torre_Canhao`, `Inimigo_Corredor`, `Fortaleza`)
substitui o modelo procedural daquele nome. Se os filhos tiverem os mesmos nomes de peça
(`Turret`, `Barrel`, `Shaft`, `Top`, `Flag`, `LegL`, `LegR`...), a animação continua funcionando.

### Ver a arte sem abrir o Unity

```bash
cd Tools/ArtPreview
dotnet run                  # gera out/art.json + out/index.html
npx http-server out         # http://localhost:8080  e  http://localhost:8080/?scene=world
```

Roda o **mesmo** `Art/*.cs` do jogo (com stubs de matemática do Unity) e desenha no navegador
com three.js: os modelos lado a lado, ou o Tower Wars montado na câmera do jogo.

## Torres e o que cada uma responde

| Torre | Responde a | Como |
|---|---|---|
| Canhão | nada em especial (a régua) | dano único, muita fronteira |
| Morteiro | Rato (bando) | dano em área, bomba em arco |
| Gelo | Lobo, Tigre | lentidão, e **congela**: frio acumulado deixa o bicho parado por um instante (depois fica imune um pouco). Nível alto congela com menos tiros e segura mais |
| Sentinela | Águia | bônus contra voador |
| **Fogo** | Urso, Rinoceronte, Elefante | queima uma fração da vida MÁXIMA por segundo, acumula até 3 camadas; nível alto queima mais forte |
| **Ar** | quem atravessa a fronteira rápido | empurra de volta pelo caminho (mais tempo sob atrito); bônus contra voador. Desliga na morte súbita |

**Cada nível muda a torre**, não só a altura: nv 2 cinta de bronze e bandeira, nv 3 contrafortes,
nv 4 coroa de pontas, nv 5 runas acesas, nv 6 remate e anel aceso no chão. E cada tipo ganha o seu:
cristais e pingentes no Gelo, braseiros extras e lava no Fogo, canhão duplo no Canhão, pás extras no Ar.

A vista desenha **entre** os tiques da simulação (30 por segundo): sem isso, soldado e tiro
andavam em degraus numa tela de 144 Hz ou mais.

## Modo Tower Wars

O jogo mira o gênero **Line Tower Wars**: você defende a sua lane *e* compra inimigos para
mandar na lane do adversário. Cada envio custa ouro agora e **sobe a sua renda para sempre** —
o jogo inteiro é o triângulo *torre × envio × renda*.

Está tudo em `Assets/_Project/Scripts/Runtime/Sim/`, como **lógica pura** (sem MonoBehaviour,
sem `Time.deltaTime`, sem `Random` do Unity). Isso dá três coisas de uma vez:

1. roda no `Tools/FlowSim`, então dá para balancear com milhares de partidas;
2. o lado Unity vira uma *vista* disto, em vez de duplicar as regras;
3. passo fixo + semente = partida reprodutível — que é o que o multiplayer por eventos vai pedir.

| Arquivo | Papel |
|---|---|
| `SendCatalog.cs` | Roster de envios (custo, vida, renda, recompensa, silhueta) |
| `TowerWarsConfig.cs` | Todos os números do modo |
| `LaneSim.cs` | Tabuleiro de um jogador: torres, inimigos, projéteis, fronteira, economia |
| `TowerWarsAi.cs` | IA por utilidade (defender × atacar × esperar), 3 dificuldades |
| `MatchSim.cs` | Partida entre dois lados, determinística por semente |

A camada Unity fica em `Runtime/TowerWars/` e é só **vista**: `LaneView` lê o `LaneSim`
todo frame e espelha na tela, sem guardar estado de jogo. Por isso a lane do adversário
usa exatamente o mesmo código de desenho da sua — e no multiplayer ela vira só um `LaneSim`
alimentado pela rede. O jogo roda em **passo fixo**, o mesmo do `FlowSim`: o que você joga
é a simulação que foi balanceada com 300 partidas headless.

A **Águia** existe por design: ignora o atrito. Sem ele, investir em fronteira seria vitória
automática e o território deixaria de ser uma decisão.

## Testes headless

A lógica pura (grid, flow field, território) compila e roda **fora do Unity** — os mesmos
`.cs` do projeto, com stubs mínimos:

```bash
cd Tools/FlowSim
dotnet run
```

66 verificações: grid, pathfinding, anti-muro, território, economia de envios, upgrades,
determinismo por semente, ritmo de partida e diversidade do roster.

Outros modos:

```bash
dotnet run -- match 60
```

Relatório de 300 partidas IA×IA (vitórias, duração, renda, % de mortes por atrito, mix de compras).

```bash
dotnet run -- sweep 25 fine
```

Varre atrito × escalada e recomenda os valores por medição — foi assim que
`AttritionPctPerSecond` e `SendScalePerMinute` foram escolhidos.

## Verificação de compilação do lado Unity

```bash
cd Tools/CompileCheck
dotnet build
```

Compila **todos** os scripts de `Runtime/` contra as **DLLs reais** do editor instalado
(referenciar assembly não exige licença; só abrir o editor exige). Acha a versão sozinho
via `ProjectSettings/ProjectVersion.txt`.

Existe porque o `FlowSim` usa stubs mínimos de `UnityEngine` e por isso é **cego** para
erros que só aparecem com a API completa. Caso real: `Random` sem qualificação compilava
headless mas é ambíguo com `UnityEngine.Random` (CS0104) — o projeto não abriria, e nenhum
dos 66 testes pegava. Esta verificação reproduz o erro.

Não cobre: comportamento em runtime, importação de assets, shaders, e a assembly opcional
`Runtime/Urp` (que depende do pacote URP, ausente até a primeira abertura do editor).

## Próximo passo

Playtest do Felipe → tuning de `BorderRadius`/`AttritionDps` em `GameConfig.cs` →
**decisão da semana 4**: fronteira + atrito diverte, ou o jogo vira TD clássico?

## Arquitetura em uma linha

Tudo nasce de `GameBootstrap` → `GameController` monta o mundo inteiro em código
(modelos, texturas e materiais procedurais gerados em runtime, sem prefabs e sem cena
montada à mão) e
dirige os sistemas por frame em ordem determinística. Balanceamento: `GameConfig.cs`.
