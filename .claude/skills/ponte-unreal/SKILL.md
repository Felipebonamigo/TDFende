---
name: ponte-unreal
description: 'Ciclo da ponte Sim → Unreal do TDFende: na nuvem o Smoke.sh (fingerprint nativo × gerenciado), no PC o PublicarParaUnreal.ps1 e o RodarU0.ps1 (-AutoTeste, -SemCompilar, -ShotTick), as opções -Tdf* e a versão da ABI. Use quando mexer na Sim, em Tools/SimBridge ou em Unreal/TDFende, ou quando pedirem "testa a ponte", "publica a DLL", "roda a U0", "roda o autoteste", "sobe a ABI".'
---
# Ponte Sim → Unreal (NativeAOT)

## Quando usar
A Sim em C# (`Runtime/Sim`, `Runtime/Grid`) vira `tdfende_sim.dll` por NativeAOT e o Unreal 5.8 a carrega por uma interface C.
Mexeu na Sim, em `Tools/SimBridge/` ou em `Unreal/TDFende/Source/`: rode este ciclo. Design: `docs/designs/migracao-unreal.md`; estado e resultados: `Unreal/LEIA-ME.md`.

## Antes de escrever código
Mudança em struct da Sim, no formato do replay ou do catálogo, comando novo (um `TDF_CMD_*` novo é comando novo), vida de torre,
`MatchRules` ou multiplayer é tarefa **"architectural"**: design em `docs/designs/<tarefa>.md`, com a tabela "Decisões pedidas ao
Felipe", aprovado por ele **antes** do código. O resto é "bounded". A ABI 2 (só acréscimos) entrou dentro do design aprovado.

## Nuvem (Linux): a ponte dá o mesmo fingerprint
1. Rode a skill `verificar` (o FlowSim tem os testes `Ponte:` do `SimHost`, inclusive os tamanhos dos structs).
2. Teste de fumaça (precisa do .NET SDK 10, `clang`, `zlib1g-dev` e `gcc`; o CI instala o .NET, o `clang` e o `zlib1g-dev`):
   ```bash
   Tools/SimBridge/Smoke.sh [replay.txt]   # padrão: Tools/SimBridge/smoke/demo_replay.txt
   ```
   Publica para `linux-x64`, roda o replay pelo C (`smoke.c`) e compara com `FlowSim replay <arquivo> --forcar`.
   Certo: `PONTE OK: fingerprints iguais`, código 0. Errado: `PONTE FALHOU` (código 1), ou `ABI ou tamanhos de struct diferentes` na saída de erro e o script parando antes da comparação.
3. O job `ponte` do GitHub Actions roda o mesmo script a cada push na `main`.

## PC (Windows): DLL, compilação e prova no motor
1. **Feche o editor do Unreal e o jogo.** O Live Coding bloqueia o `Build.bat`. E a DLL é carregada uma vez por processo e nunca
   liberada (NativeAOT não descarrega): com o editor aberto a cópia falha, e Play/Stop continua usando a DLL velha.
2. Publique a DLL (precisa do "Desktop development with C++" do Visual Studio; o script já põe o `vswhere` no PATH):
   ```powershell
   powershell -ExecutionPolicy Bypass -File Tools\SimBridge\PublicarParaUnreal.ps1
   ```
   Copia `Tools\SimBridge\bin\Release\net10.0\win-x64\publish\tdfende_sim.dll` para `Unreal\TDFende\Binaries\Win64\` (fora do git).
3. Compile e rode a prova (Build.bat `TDFendeEditor Win64 Development`, depois o motor em `-game`):
   ```powershell
   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1                 # replay de 200 s, ~3,5 min
   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1 -AutoTeste      # autoteste de jogo, ~10 s
   powershell -ExecutionPolicy Bypass -File Unreal\TDFende\RodarU0.ps1 -SemCompilar -ShotTick 4000
   ```
   - Sem `-AutoTeste`: sai com **0** se a linha `FINGERPRINT (compare com o FlowSim): ...` termina no `$esperado` do script, **1** se não (ou se a compilação falhar, faltar a DLL, ou passar de `-Tempo`, padrão 420 s).
   - `-AutoTeste`: o autoteste no motor (24 verificações em 14/10/2026: catálogo, `CanBuild`, célula↔mundo, construir, subir, enviar, vender, GC, comandos gravados), com linhas `AUTOTESTE OK` / `AUTOTESTE FALHOU` e no fim `AUTOTESTE PASSOU (0 falha(s))` ou `REPROVOU (n falha(s))`. Depois reproduz `Saved\Tdf\partida.txt` no FlowSim e exige `estado final igual ao gravado`. Sai com 0 só se os dois passarem; 1 se travar em 120 s ou a gravação não reproduzir.
   - `-SemCompilar` pula o Build.bat (só quando nada em C++ mudou). `-ShotTick` (padrão 2500) é o tique do print do replay (o `-AutoTeste` não tira print).
4. Saída em `Unreal\TDFende\Saved\U0\` (fora do git): `u0-HHmmss.log` ou `auto-HHmmss.log`, e `viewport.png`. **Olhe o print com a ferramenta Read** antes de falar do visual.
   Custo da ponte: a linha `CUSTO ... média p50 p95 p99 máx` (meta < 0,5 ms/quadro; em 14/10/2026, média 0,056 ms). FPS: linha `FPS ...` a cada 5 s.

## Opções `-Tdf*` (lidas por `TdfSimActor`, `TdfDemoGameMode` e `TdfEnvironment`)
Rode `C:\Program Files\Epic Games\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe` com `"<repo>\Unreal\TDFende\TDFende.uproject" '/Engine/Maps/Entry?game=/Script/TDFende.TdfDemoGameMode' -game -windowed` e:
`-TdfReplay=<arquivo>` (reproduz um replay), `-TdfShot=<png>` + `-TdfShotTick=<n>` (print no tique), `-TdfQuit` (fecha no fim do replay),
`-TdfQuitTick=<n>` / `-TdfQuitSeconds=<s>` (fecha), `-TdfSelfTest` (o autoteste), `-TdfDificuldade=0|1|2`, `-TdfPreview` (vitrine da arte),
`-TdfSemArte`, `-TdfSemAmbiente`, `-TdfSemGrama`, `-TdfCamX/Y/Z/Pitch/Yaw/Fov=`. Partida jogada (não replay) fica gravada em `Saved/Tdf/partida.txt`,
no formato do `Replay.Serialize`. Para jogar: `Unreal\TDFende\Jogar.ps1 [-Dificuldade 0|1|2] [-TelaCheia]`; A/B de FPS: `Unreal\TDFende\MedirFps.ps1`.

## Versão da ABI (`TDF_ABI_VERSION`, hoje 2)
Sobe quando um struct ou função da interface muda. Mude **juntos**, no mesmo commit:
- `Tools/SimBridge/include/tdfende_sim.h`: o `#define`, os structs e o comentário de bytes (o Unreal inclui este arquivo direto pelo `TDFende.Build.cs`; não copie).
- `Tools/SimBridge/Exports.cs`: `AbiVersion() => N` e as funções exportadas.
- `Assets/_Project/Scripts/Runtime/Sim/Bridge/BridgeTypes.cs`: o espelho dos structs (só tipos planos).
- `Tools/FlowSim/Program.cs` (`SimHostTests`): os tamanhos esperados (hoje lane 64, inimigo 40, torre 48, tiro 32, evento 36) e o teste da função nova, visto falhar antes.
- `Tools/SimBridge/smoke/smoke.c` e `Unreal/TDFende/Source/TDFende/TdfSim.h/.cpp` (`TDF_BIND`), se usam a função.
Depois: `Smoke.sh` na nuvem; no PC, republique a DLL e rode o `RodarU0.ps1` **sem** `-SemCompilar`.
Sintoma de descompasso no log do Unreal: `Ponte: ABI diferente do cabeçalho (dll vN; structs ...)`.

## Armadilhas
- Arquivo novo da Sim entra à mão no `Tools/SimBridge/SimBridge.csproj` e no `Tools/FlowSim/FlowSim.csproj`.
- Mudou regra da Sim de propósito (`SimRules.Version` sobe): o `Smoke.sh` continua verde (compara as duas Sims), mas o `$esperado` do `RodarU0.ps1` fica velho. Pegue o novo valor e atualize o script e o `Unreal/LEIA-ME.md`:
  `dotnet run --project Tools/FlowSim -v quiet -- replay Tools/SimBridge/smoke/demo_replay.txt --forcar`
- Um host por processo, na thread do jogo. Nenhuma exceção atravessa: erro vem de `tdf_last_error`.
- `Content/`, `Binaries/`, `Intermediate/` e `Saved/` do Unreal ficam no PC (backup no D:). No git, só código e configuração.

## Depois
- Resultado no PC (fingerprint, custo, print) vai para o `Unreal/LEIA-ME.md` e para a linha U* do `ROADMAP.md`.
- Tarefa que passa da nuvem para o PC: diga no commit o que falta e onde parou. Commit e push pela skill `verificar`.
