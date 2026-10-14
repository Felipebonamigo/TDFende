# Design — Migração para o Unreal Engine 5 (a Sim continua a mesma)

[← cronograma](../../ROADMAP.md) · decisão do Felipe em 14/10/2026: **"vamos mudar para o Unreal; não tem problema a demora, é melhor"**

**Estado:** escrito e **aprovado pelo Felipe em 14/10/2026 ("pode seguir, com o unreal engine"), com D1 a D7 como recomendado; U0 em andamento** (tarefa "architectural" na definição do CLAUDE.md: troca de engine, formato
de integração e multiplayer futuro). A decisão de *mudar* já é do Felipe; o que este documento pede é a
aprovação de **como**.

## 1. O que existe hoje e o que a troca atinge

| Parte | Tamanho | Depende do Unity? | Destino |
|---|---|---|---|
| **Sim** (`Runtime/Sim`, `Runtime/Grid`): regras, IA, catálogos, replay, assinatura, eventos, laboratório | ~4,2 mil linhas de C# | **Só `Vector3`, `Vector2Int` e `Mathf`**, que o FlowSim já substitui por stubs | **Fica.** É o coração do jogo e o que tem ~300 testes, BalanceLab, matriz de contras, replay e assinatura |
| **Vista** (`Art`, `Visuals`, `TowerWars`, `Core`, `Towers`, `Input`): modelos em código, efeitos, HUD OnGUI, câmera, captura | ~9,3 mil linhas de C# | Sim, tudo | **Reescrita no Unreal** (Blueprint e C++). Os modelos procedurais deixam de existir: o Unreal usa os GLB/FBX reais |
| Ferramentas (FlowSim, AuditaLicencas, AuditaAssets, conversores) | — | Não (a maioria) | Ficam; os de arte são adaptados |
| Arte (GLB do Meshy, Poly Haven, manifesto de licenças) | — | Não | Reaproveitada; importada no Unreal |

A tese deste design: **trocar só a vista**. Reescrever a Sim em C++ jogaria fora o que foi medido e provado (determinismo,
balanceamento, replays) e traria o risco de float entre linguagens (TEC-27) sem necessidade.

## 2. Como o Unreal fala com a Sim (a decisão central)

O Unreal não roda C#. Três caminhos:

| | Caminho | Prós | Contras |
|---|---|---|---|
| **A** | **Sim em C# compilada como DLL nativa (NativeAOT) com interface em C**, chamada por um plugin C++ do Unreal | A Sim, os testes e o BalanceLab continuam **os mesmos**; uma só fonte da verdade; eu trabalho na nuvem como hoje | Toolchain NativeAOT no PC; fronteira de interop para depurar; só Windows/PC (já é a meta) |
| B | Portar a Sim para C++ com os testes do FlowSim servindo de vetores de referência (fingerprint igual) | Tudo em um mundo só; sem DLL | Meses de port; duas Sims até acabar; risco de float; perde-se a velocidade de iterar a regra na nuvem |
| C | Embutir Mono/.NET dentro do Unreal | — | Frágil, sem suporte oficial; descartado |

**Recomendação: A.** O contrato com o Unreal é pequeno e plano (nada de objetos gerenciados atravessando a fronteira):

```
sim_create(seed, dificuldade, largura, altura) -> handle
sim_load_catalogs(handle, texto_envios, texto_torres) -> erro
sim_enqueue(handle, comando)                         // Build/Upgrade/Sell/Send, como o MatchRunner
sim_step(handle, n_tiques)
sim_snapshot(handle, buffer*)                        // structs planas: inimigos, torres, tiros, território, placar das 2 lanes
sim_events(handle, buffer*)                          // os eventos do TEC-17, drenados a cada quadro
sim_fingerprint(handle) / sim_signature()            // paridade e replay
```
A vista do Unreal só **lê o snapshot e os eventos** e desenha, como a vista do Unity já faz (`LaneView` lê a Sim por portas
públicas). Os ids são os índices do catálogo; a **chave** (TEC-12) liga cada bicho/torre ao seu Data Asset no Unreal
(modelo, animação, Niagara, som), então o TEC-12 já preparou exatamente isto.

## 3. Fases, com critério de pronto e ponto de parada

| Fase | O que entrega | Quem | Critério (go / no-go) |
|---|---|---|---|
| **U0 — Espinha** | `SimBridge` (C ABI) + build NativeAOT; plugin Unreal `TDFendeSim` que carrega a DLL, roda uma partida e desenha cubos/esferas instanciados | nuvem escreve a Sim-side e os testes da ponte; **PC** compila e abre o Unreal | O fingerprint de um replay no Unreal é **idêntico** ao do FlowSim, e o custo da ponte por quadro < 0,5 ms. Se falhar, voltamos ao caminho B ou ao Unity |
| **U1 — Fatia de beleza** | Cena fixa no tema decidido: relevo e chão (Poly Haven), Lumen, névoa, 3 torres e 3 bichos do Meshy, câmera da partida; comparação com a régua (Total War: Warhammer III) | **PC** (editor); nuvem prepara o protocolo | Aprovação visual do Felipe. Este é o "VIS-27" no Unreal |
| **U2 — Paridade de jogo** | Partida completa no Unreal: construir/subir/vender/enviar, território, HUD (UMG), VFX (Niagara), áudio, câmera, replay F9, captura automática de teste | PC + nuvem (código C++/Blueprint de jogo escrito na nuvem quando não depende do editor) | Joga-se uma partida inteira contra a IA com o mesmo balanceamento do Unity |
| **U3 — Conteúdo** | Todas as torres (3 estágios), bichos animados, tema, ambiente vivo | PC | Elenco do tema (`docs/arte/elenco.md`) |
| **U4 — Empacotar** | Build Windows, Steamworks, selo de build, orçamento de FPS | PC | Executável gerado da `main` |

O **Unity fica congelado** como referência jogável até a U2 fechar: sem recurso novo na vista dele (a Sim continua evoluindo e
serve aos dois). Depois da U2, o código de vista do Unity é arquivado (tag no git), não apagado.

## 4. O que muda no repositório e no jeito de trabalhar

- Projeto Unreal em `Unreal/TDFende/` (`.uproject`, `Source/`, `Config/`, plugin `TDFendeSim`). **No git público: só código e
  configuração.** `Content/` (uassets binários, pesados) fica **no PC com backup no D:**, como a arte pesada de hoje (regra do
  CLAUDE.md: nada acima de 10 MB; repositório público). A nuvem não vê o `Content/`.
- A sessão da nuvem escreve: Sim, ponte (C#, cabeçalho C, plugin C++), testes, ferramentas de arte (Python/Blender),
  protocolos, documentação. A sessão do PC faz: abrir o Unreal, importar, montar nível e materiais, rodar e capturar.
  Há um skill de Unreal MCP disponível para a sessão do PC dirigir o editor.
- `Tools/FlowSim` continua o laboratório e o portão do pre-commit. Entra uma verificação nova: o teste da ponte (a DLL gera o
  mesmo fingerprint que o FlowSim).
- **Tarefas do ROADMAP que são específicas de Unity** (VIS-04/05/08/09/10, TEC-09/10/25, UX-*, captura do Unity, BuildJogo etc.)
  passam a **"substituída pela migração"** e são reescritas no formato Unreal depois da aprovação deste design. Não apago nada
  antes disso.
- URP × HDRP (VIS-29) deixa de existir: o Unreal já é o pipeline escolhido. O Unity continua só até a paridade.

## 5. Riscos

- **A ponte é o ponto frágil.** Por isso a U0 vem primeiro e é eliminatória. NativeAOT tem restrições (sem reflexão
  dinâmica; a Sim já evita, e `SimSignature` foi escrita sem reflexão em tempo de execução pensando nisso).
- **Perda de velocidade de iteração visual**: no Unreal o ciclo editor→captura exige o PC; a nuvem fica com a lógica.
- **Custo de reescrever a vista** (~9 mil linhas viram nível, Blueprint, Niagara e UMG). É o preço da qualidade visual; sem prazo,
  como o Felipe pediu, mas deve ser fatiado em U0 a U4 para sempre existir algo jogável.
- **Licença e royalties do Unreal**: gratuito até o primeiro US$ 1 milhão de receita bruta, depois 5% (regra vigente a conferir
  pelo Felipe no contrato da Epic; vale mesmo na Steam). Não é decisão minha.
- **IA (Meshy) e assets** seguem o manifesto de licenças; o Unreal não muda isso.
- Multiplayer futuro: a Sim determinística com replay e assinatura já é a base de lockstep; o Unreal só exibe.

## 5b. Já existe prática de Unreal nas suas outras sessões

O Felipe já usa o Unreal 5.8 em outros projetos (Age of Earth, com `AoEBridgeActor` em C++ e `Config/AoEVisuais.json`; Detonauto).
Antes de escrever a ponte da U0, a sessão do PC deve **descrever como o Age of Earth liga a simulação ao Unreal** (formato do
snapshot, quem compila, armadilhas de include como `SkeletalMeshActor.h`) para reaproveitarmos o padrão que já funciona em vez de
inventar outro. Isso entra como passo 0 da U0.

## 6. Decisões pedidas ao Felipe

| | Pergunta | Recomendação |
|---|---|---|
| **D1** | Sim em C# como DLL nativa (A), porta para C++ (B)? | **A**, com a U0 como teste eliminatório (se falhar, reabrimos B). |
| **D2** | Versão do Unreal | **Respondida: UE 5.8** (`C:\Program Files\Epic Games\UE_5.8`, vista na sessão "Age of Earth Local", que já compila um projeto C++ em `AgeOfEarth\unreal\AgeOfEarthUE`). |
| **D3** | Unity congelado como referência até a U2 fechar, depois arquivado (A); ou apagar já (B)? | **A.** |
| **D4** | `Content/` do Unreal fora do git público, com backup no D: (A); ou um repositório privado à parte para o conteúdo (B)? | **A** hoje (zero custo); B se você quiser que a nuvem enxergue o Content algum dia. |
| **D5** | Reescrever o ROADMAP para as fases U0 a U4, marcando as tarefas Unity como "substituídas" (A)? | **A**, depois desta aprovação. |
| **D6** | Começar pela U0 (ponte + cubos) antes de qualquer arte (A)? | **A.** Sem a ponte provada, nada do resto vale. |
| **D7** | Alvo: Windows/Steam apenas (A)? | **A**, como já decidido (celular fora da meta). |
