---
name: verificar
description: Roda a verificação do TDFende antes de commitar (FlowSim, CompileCheck, CompileCheckUrp, AuditaLicencas, AuditaAssets, hook pre-commit) e faz commit e push direto na main sem perder arquivos. Use quando pedirem "verifica", "roda os testes", "tá verde?", "pode commitar", "faz o push" ou quando o pre-commit barrar um commit.
---
# Verificar, commitar e subir na main

## Quando usar
Antes de todo commit, na nuvem e no PC. Nada está pronto sem FlowSim e CompileCheck verdes (CLAUDE.md).
Se a mudança é visível no jogo, depois disto vem a skill `captura-do-executavel` (só no PC).
Na nuvem, o hook de início de sessão (`.claude/hooks/session-start.sh`) instala o .NET 10 em `~/.dotnet`; se `dotnet` faltar, rode-o à mão com `CLAUDE_CODE_REMOTE=true CLAUDE_PROJECT_DIR=$PWD .claude/hooks/session-start.sh`.

## Passos
1. Comece atualizado (na `main`). Antes de pegar tarefa do `ROADMAP.md`, escreva nela `(reservado: sessão <data hora>)` e faça push.
   ```bash
   git pull --rebase --autostash
   ```
2. **FlowSim** (lógica pura, ~3 min). Última linha esperada: `>>> TODOS OS TESTES PASSARAM`. Teste quebrado sai como `FALHOU  <nome>`; o código de saída é o número de falhas.
   ```bash
   dotnet run --project Tools/FlowSim -v quiet --nologo
   ```
   Mexeu em regra da Sim: compare `dotnet run --project Tools/FlowSim -v quiet -- match 12` antes e depois (idêntico se a regra não mudou de propósito).
   Subcomando (`match`, `replay`, `matriz`, `sweep`) vai **sem** `--nologo`: o `dotnet run` repassa o `--nologo` ao programa e a suíte roda no lugar.
3. **CompileCheck** (Runtime contra as DLLs reais do Unity, menos `Runtime/Urp`). Tem que sair com código 0 (no PC: `0 Erro(s)`).
   ```bash
   dotnet build Tools/CompileCheck -v quiet --nologo
   ```
   - **PC:** acha as DLLs em `C:\Program Files\Unity\Hub\Editor\<versão de ProjectSettings/ProjectVersion.txt>\Editor\Data\Managed\UnityEngine` (hoje 6000.3.11f1).
   - **Nuvem (Linux):** não há Unity. Sem ajuda, o build para em `Não achei as DLLs do Unity em ...`. Aponte uma pasta com as DLLs: `dotnet build Tools/CompileCheck -v quiet --nologo -p:UnityManaged=<pasta>`. As sessões anteriores usaram o "Unity 2021 do NuGet" e registraram "sem erro novo": compare a lista de erros antes e depois da sua mudança. A palavra final é o pre-commit do PC.
4. **CompileCheckUrp** (scripts de Editor e `Runtime/Urp`). Só roda onde existe `Library/ScriptAssemblies/Unity.RenderPipelines.Universal.Runtime.dll`, ou seja, no PC depois que o Unity abriu o projeto. Na nuvem, pule.
   ```bash
   dotnet build Tools/CompileCheckUrp -v quiet --nologo
   ```
5. **Licenças** (código 0 passou, `PENDENTE` só avisa; 1 reprovou; 2 uso errado). Asset novo: entrada em `docs/licencas/manifesto.json` no mesmo commit e depois `--gera`.
   ```bash
   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --git
   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --gera   # reescreve THIRD_PARTY.md e creditos.txt
   ```
6. **Orçamento da arte** (só avisa; `--estrito` faz reprovar; `--disco` e `--detalhe` também existem).
   ```bash
   dotnet run --project Tools/AuditaAssets -v quiet --nologo -- --git
   ```
7. **Hook pre-commit.** Ligue uma vez por clone: `git config core.hooksPath .githooks`. Na ordem, ele: barra arquivo acima de **10 MB** no índice; roda AuditaLicencas `--git` (reprova); AuditaAssets (aviso); CompileCheck; CompileCheckUrp se houver `Library/`; FlowSim. Logs em `/tmp/tdfende-lic.log`, `tdfende-cc.log`, `tdfende-ccu.log`, `tdfende-tests.log`. `git commit --no-verify` só de propósito.
   - **Na nuvem** o hook não é ligado (o CompileCheck precisa das DLLs do Unity): rode os passos 2, 3, 5 e 6 à mão e confira os 10 MB:
   ```bash
   git diff --cached --name-only --diff-filter=AM -z | while IFS= read -r -d '' f; do t=$(git cat-file -s ":$f"); [ "$t" -gt 10485760 ] && echo "GRANDE: $f"; done; true
   ```
8. **Commit barrado:** conserte e rode `git commit` de novo com os mesmos arquivos no índice. **Nunca** `git stash` / `git stash pop` entre a tentativa barrada e a nova: o `pop` esvazia o índice e o commit sai incompleto (aconteceu em 06/10/2026).
9. Mensagem em português, terminando com as linhas de atribuição da sessão (`Co-Authored-By: ...`). Depois de **todo** commit:
   ```bash
   git show --stat HEAD
   ```
10. Push direto na `main` (sem branch):
    ```bash
    git pull --rebase --autostash && git push
    git fetch && git status -sb   # tem que mostrar "## main...origin/main" sem "ahead"
    ```
    O GitHub Actions (`.github/workflows/flowsim.yml`) roda AuditaLicencas, FlowSim e a ponte (`Tools/SimBridge/Smoke.sh`) a cada push. Ele **não** roda o CompileCheck.

## Armadilhas
- Arquivo novo de lógica em `Runtime/Sim/` entra à mão no `Tools/FlowSim/FlowSim.csproj` (e no `Tools/SimBridge/SimBridge.csproj` se a ponte usa). Módulo novo da API do Unity pede `<Reference>` `UnityEngine.<Módulo>Module` no `CompileCheck.csproj`.
- `.asset` que o Unity regrava (`Assets/Settings/*.asset`, `ProjectSettings/*.asset`, `DefaultVolumeProfile.asset`) não entram sem o OK do Felipe. `M` no status com `git diff` vazio é só fim de linha: `git add` limpa.
- Nunca edite `THIRD_PARTY.md` nem `creditos.txt` à mão. Nunca `git add -f` em `Assets/_Privado/`: entrada `repoPublico: false` no índice reprova.
- O hook roda em bash (`#!/usr/bin/env bash`, corrigido em 14/10/2026): no `sh` do Linux (o `dash`) o `read -d` falhava e a checagem de 10 MB passava calada.
- Windows não diferencia maiúsculas: liste com `git ls-files <pasta>` antes de mover ou apagar.

## Depois
- Marque `[x]` com data e hash do commit no `ROADMAP.md` **e** no arquivo da fase, no mesmo commit.
- Tarefa que passa para a outra sessão (nuvem ↔ PC): diga no commit o que falta e onde parou (MANUAL, seção 10).
- Feche a resposta com `DONE`, `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT` ou `BLOCKED`.
