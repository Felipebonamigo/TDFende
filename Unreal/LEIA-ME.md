# TDFende no Unreal 5.8 — U0 (ponte Sim → Unreal)

Design: [`docs/designs/migracao-unreal.md`](../docs/designs/migracao-unreal.md). Aqui só **código e configuração** (D4): `Content/`,
`Binaries/`, `Intermediate/` e `Saved/` ficam fora do git, no PC, com backup no D:.

**Estado:** o lado da Sim está pronto e provado na nuvem (a Sim em NativeAOT, chamada pelo C, dá o fingerprint do C# gerenciado).
O lado do Unreal (`Source/TDFende`) foi **escrito mas ainda não compilado**: a nuvem não tem o Unreal. Ele é o que a sessão do PC prova.

## Roteiro da U0 no PC (go / no-go)

0. **Antes de tudo:** abra a sessão do Age of Earth (`AgeOfEarth\unreal\AgeOfEarthUE`, `AoEBridgeActor`) e veja como ela liga a
   simulação ao Unreal. Se houver um padrão melhor que o `TdfSim` (carregar DLL, thread, formato do snapshot, armadilhas de include
   como `SkeletalMeshActor.h`), adapte o `TdfSim`/`TdfSimActor` a ele e me diga o que mudou.
1. **Publicar a DLL** (precisa do Visual Studio Build Tools com "Desktop development with C++"):
   `powershell -ExecutionPolicy Bypass -File Tools\SimBridge\PublicarParaUnreal.ps1`
   → copia `tdfende_sim.dll` para `Unreal\TDFende\Binaries\Win64\`.
2. **Gerar e compilar o projeto:** clique direito em `Unreal\TDFende\TDFende.uproject` → *Generate Visual Studio project files*, abra
   e compile `TDFendeEditor | Win64 | Development` (ou `Build.bat TDFendeEditor Win64 Development -Project=...`). Erros de
   compilação do C++ são esperados na primeira vez: corrija e me mande o diff.
3. **Abrir o projeto**, criar um nível vazio, arrastar um **`TdfSimActor`** (Place Actors → procure por TdfSim), deixar na origem.
4. **Teste de paridade (o critério):** no ator, preencha `ReplayPath` com o caminho absoluto de
   `Tools\SimBridge\smoke\demo_replay.txt`, dê **Play** e olhe o Output Log. A linha
   `FINGERPRINT (compare com o FlowSim): ...` tem que ser exatamente
   `t6000 pl20/775/100/56/66 fo22/372/206/9/36 k61/16/1/40/21/0 #835d1db265f91d80`
   (o mesmo de `dotnet run --project Tools/FlowSim -v quiet -- replay Tools/SimBridge/smoke/demo_replay.txt --forcar`).
5. **Custo da ponte:** com `stat unit`/Insights, a chamada de `Redraw` + `step` por quadro deve ficar abaixo de 0,5 ms.
6. Sem `ReplayPath`, a IA joga sozinha e você vê cubos (bichos), cilindros (torres) e esferas (tiros) nas duas lanes.

**Go:** fingerprint igual e custo < 0,5 ms → segue para a U1 (fatia de beleza). **No-go:** reabrimos o caminho B do design.

## Coordenadas
Sim (x direita, y cima, z frente; unidades de célula) → Unreal `X = z`, `Y = x`, `Z = y` em cm (`CellSizeCm`, padrão 100). A lane 1 é
deslocada em X. A vista de verdade (U1+) troca estes cubos por malhas por **chave** (`SendUnit.Key` / `TowerType.Key`, TEC-12).

## Resultado da U0 (14/10/2026) — **GO**

Rodado no PC (UE 5.8.3, Visual Studio 2026 com C++, .NET SDK 10.0.302, RTX 4070 Ti), em jogo (`-game`) com o replay `demo_replay.txt`
(6000 tiques = 200 s de jogo em tempo real, 1600×900).

- **Fingerprint obtido:** `t6000 pl20/775/100/56/66 fo22/372/206/9/36 k61/16/1/40/21/0 #835d1db265f91d80` — **idêntico** ao esperado (FlowSim),
  em 3 execuções seguidas. A assinatura dos catálogos foi `d610b62201892c46 7cb8cd3f7c5ce6cd 0d8715b99f2d0452`.
- **Custo da ponte por quadro** (tudo que o `Tick` faz com a Sim: passos, snapshot, cópia, instâncias, eventos; ~65 mil quadros a ~320 fps):
  média **0,036 ms**, p50 0,032, p95 0,055, p99 0,096, máx 3,0 ms (um quadro isolado). Meta < 0,5 ms: cumprida com folga.
  Numa 4ª execução, pelo `RodarU0.ps1` (com o PC mais ocupado, ~165 fps): média 0,041, p99 0,211, máx **15,3 ms** em um único quadro
  (não investigado: parece soluço de carga/GC; a média e o p99 seguem muito abaixo da meta).
  Só os passos da Sim: média 0,002 ms (um tique custa uns 31 µs; cada quadro roda 0 ou 1). `Redraw` + eventos: média 0,034 ms.
- **Ressalva da medida:** o replay de demonstração tem no máximo **10 inimigos vivos** ao mesmo tempo. O fim de partida real e o
  `-Estresse` do Unity (60 bichos, 12 torres no máximo) ainda não foram medidos no Unreal; repetir na U1.
- **Print do viewport (tique 2500):** `Unreal\TDFende\Saved\U0\viewport_t2500.png` (o `viewport_t4000.png` é a primeira tentativa, com
  enquadramento errado). Duas lanes com piso, torres e tiros sobre o piso; só formas básicas, como previsto para a U0.

### O que foi corrigido / mudou

1. **Passo 0 (Age of Earth):** o `AoEBridgeActor` liga a simulação por **WebSocket** a um servidor TypeScript (`ws://127.0.0.1:8790`, JSON,
   `state` a ~10 Hz com interpolação no cliente). É outro desenho (processo separado), então **não havia padrão melhor para a nossa ponte em
   processo** (DLL NativeAOT por ponteiros, snapshot plano, 30 Hz fixos): o `TdfSim`/`TdfSimActor` ficaram como estão. Do AoE aproveitei só a
   ideia do medidor de desempenho no log e o aviso de que o include certo é `Animation/SkeletalMeshActor.h` (vale para a U1).
2. **`PublicarParaUnreal.ps1`:** o NativeAOT falhava no link (`'vswhere.exe' não é reconhecido`): o ILCompiler 10.0.10 chama o `vswhere` pelo
   PATH e o Visual Studio 2026 não o põe lá. O script agora acrescenta `...\Microsoft Visual Studio\Installer` ao PATH. Nada para instalar:
   o Visual Studio Community 2026 já tinha "Desktop development with C++" (MSVC 14.51).
3. **Compilação C++:** compilou na primeira tentativa, sem erro. Só o aviso C4005 (`TDF_API` redefinida: o cabeçalho C declara `dllimport`),
   silenciado com `#pragma warning` ao redor do `#include` em `TdfSim.h`. O cabeçalho `tdfende_sim.h` **não foi tocado**.
4. **Coordenadas:** a grade da Sim é **centrada na origem** (`GridMap`: x em [-W/2, W/2], z em [-H/2, H/2]), não no canto. A lane 0 fica em
   X = 0 e a lane 1 em X = `(GridHeight+4) × CellSizeCm`. A nota "Coordenadas" acima continua valendo, com esse detalhe.
5. **Novo, para teste sem abrir o editor:** `TdfDemoGameMode` (em qualquer nível, inclusive o vazio `/Engine/Maps/Entry`, monta sozinho o
   `TdfSimActor`, pisos, câmera de cima e luz) e opções de linha de comando no ator: `-TdfReplay=`, `-TdfShot=` + `-TdfShotTick=`, `-TdfQuit`.
   No fim do replay o log traz o `FINGERPRINT` e o `CUSTO` (média, p50, p95, p99, máx). Tudo roda por
   `powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1` (compila, roda, confere o fingerprint; sai com 0 ou 1; ~3,5 min).
6. O roteiro manual (abrir o projeto, nível vazio, arrastar o `TdfSimActor`, Play) não foi feito à mão: o `TdfDemoGameMode` faz o mesmo
   pelo mesmo ator, e o resultado é o do log. Arrastar o ator num nível continua funcionando.

## U2, parte 1: jogo jogável com formas simples (14/10/2026)

O `TdfSimActor` virou a vista jogável da Sim (formas simples; as malhas reais são a U3). O que existe, tudo por **código C++** no git:

- **Ponte, ABI 2** (só acréscimos, o resto do contrato não mudou): `tdf_catalog_text` (catálogo de torres e de envios em texto,
  `chave;nome;custo;...`), `tdf_can_build(x,y,tipo)` (0 = dá; senão o `RejectReason`) e `tdf_upgrade_cost(x,y)`. Testados no FlowSim
  (9 verificações novas, vistas falhando antes de existir o código). A vista não conhece mais números de balanceamento: tudo vem do catálogo.
- **Controle** (`TdfPlayerController`): botão esquerdo constrói a torre selecionada na célula sob o mouse (ou sobe, se já há torre),
  botão direito vende, **Q/E** troca a torre, **1 a 9** envia o bicho. O mouse vira célula por raio contra o chão.
- **HUD provisório** (`TdfHUD`, Canvas): vidas, ouro, renda, torres, tempo (você e a IA), paleta de torres com custo, lista de envios,
  contorno da célula sob o mouse (verde = dá, vermelho = não, amarelo = subir) com o custo, e aviso curto quando um comando é recusado.
- **Território** desenhado (placas por célula, só reconstruídas quando muda), cor por tipo de torre e por estado do inimigo (normal,
  lento/congelado, queimando, voador).
- **Partida gravada** em `Saved/Tdf/partida.txt` (formato do replay), pronta para o F9 da U2 parte 2.
- **Autoteste** `-TdfSelfTest` (ou `RodarU0.ps1 -AutoTeste`, ~10 s, sai com 0 ou 1): 19 verificações — catálogo pela ponte, `CanBuild`,
  conversão célula↔mundo em todas as células, construir, subir, enviar, vender, comandos gravados. Passou.
- Opções novas de linha de comando: `-TdfQuitTick=<n>`; `-TdfSelfTest`.

**Limites conhecidos da U2 parte 1:** o HUD é de teste (a loja de verdade é a fase de UI); as formas ainda são cubos e cilindros (as cores
da engine saíram lavadas pela exposição automática); não há som, Niagara, câmera livre, nem replay F9 de leitura; nenhum teste com mouse
real, só com as mesmas chamadas (a conversão do raio do mouse foi testada pela volta célula↔mundo, não pela projeção da câmera).
