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
