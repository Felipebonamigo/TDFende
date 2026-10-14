---
name: ler-crash
description: 'Lê o que o TDFende deixa quando trava ou fecha sozinho (Player.log do Unity e logs do Unreal), acha a linha de código e transforma em Issue "[Playtest]" com o passo a passo. Use quando o jogo travar, fechar sozinho, um testador mandar log ou pasta de crash, ou o Felipe disser "crashou", "fechou do nada" ou "deu erro".'
---
# Ler um crash

## Onde fica

- Unity (congelado): `%USERPROFILE%\AppData\LocalLow\DefaultCompany\TDFende\Player.log` e `Player-prev.log`. O
  caminho muda quando o TEC-02 trocar o `companyName`. A skill `captura-do-executavel` já lê esse log.
- Unreal pelos scripts: `Unreal\TDFende\Saved\U0\u0-<hora>.log` e `auto-<hora>.log` (`RodarU0.ps1`) e
  `Saved\Fps\<caso>.log` (`MedirFps.ps1`). Categoria `LogTdf`; linhas-chave `FINGERPRINT`, `AUTOTESTE`, `CUSTO` e
  `Ponte: ABI diferente do cabeçalho`.
- Crash do Unreal: `Unreal\TDFende\Saved\Crashes\<pasta>\` (`CrashContext.runtime-xml`, log e `UEMinidump.dmp`); no
  jogo empacotado, `%LOCALAPPDATA%\TDFende\Saved\Crashes\`.

## Passos

1. Junte os arquivos. Do PC do Felipe, leia direto. De um testador, peça a pasta zipada anexada à Issue "[Playtest]"
   (o modelo da Issue tem o campo de anexo). Anote a versão do jogo e a data.
2. Ache a primeira linha que é do jogo. No Unity, a exceção vem com a pilha no `Player.log`. No Unreal, procure `Fatal error`, `Unhandled Exception` ou
   `Assertion failed` no log e a `<CallStack>` do `CrashContext.runtime-xml`; a primeira linha com `UnrealEditor-TDFende`
   traz arquivo e linha. Se a pilha termina na `tdfende_sim.dll`, o erro é da Sim: reproduza na nuvem com o replay
   (`Saved\Tdf\partida.txt`, ou o F9 do jogo) em `dotnet run --project Tools/FlowSim -v quiet -- replay <arquivo>`.
3. Leia as 50 linhas de log antes do erro: o que o jogo estava fazendo (cena, modo, último comando).
4. Ligue ao código: abra o arquivo e a linha, e o `git log` daquele trecho perto da data da versão.
5. Reproduza com a skill `captura-do-executavel` ou `ponte-unreal` (no PC), ou o `replay` do FlowSim (na nuvem). Se reproduziu, o defeito vira teste visto falhar antes da correção.
6. Registre: Issue "[Playtest]" com gravidade "trava" ou "quebra a partida", o trecho da pilha, o arquivo e a linha, e
   como reproduzir. A correção segue a skill `triar-playtest`.

## Armadilhas

- Sem o `.pdb` da mesma compilação, a pilha vem só com endereços. O `.pdb` fica fora do git e fora do depot da Steam;
  guarde o de cada versão que foi para teste.
- `ensure` que falha não fecha o jogo, mas aparece no log como erro: leia também esses.
- O único crash registrado (cerca de 60 s de jogo) era o coletor de lixo apagando uma animação guardada em ponteiro cru
  (`Unreal/LEIA-ME.md`): ponteiro cru de UObject é o primeiro suspeito.
- ABI diferente entre a DLL e o cabeçalho para a ponte logo no início, com a linha `Ponte: ABI diferente`: publique a DLL
  de novo (`PublicarParaUnreal.ps1`).
- O plano de crash e relato do jogo (diário que sobrevive ao crash, botão "Reportar problema", símbolos por hash) é o
  TEC-29 da Fase 10, ainda a fazer.
- Log de testador pode ter nome de usuário e caminho do computador dele: não copie isso para a Issue pública.
