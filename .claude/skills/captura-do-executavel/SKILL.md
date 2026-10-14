---
name: captura-do-executavel
description: Gera o executável do Unity (Tools\GerarExecutavel.ps1) e roda o teste de fumaça -captura (Tools\Captura.ps1) no PC do Felipe, lendo prints, print_metricas.json e Player.log. Use quando pedirem "gera o exe", "tira o print", "roda a captura", "confere no executável", "mede o FPS" ou depois de qualquer mudança visível.
---
# Executável e captura (só no PC)

## Quando usar
Toda mudança visível só está pronta com o print do executável e `Tools\Captura.ps1` saindo com **0** (CLAUDE.md).
O Felipe joga pelo executável, não pelo editor. **Só no PC** (Windows, Unity 6000.3.11f1 pelo Hub). Na nuvem não tente:
deixe no commit o que falta conferir para a sessão do PC. A vista do Unity está congelada até a U2 do Unreal, mas a
captura continua sendo a prova do executável de referência.

## Passos
1. Antes: `git pull --rebase --autostash` e a skill `verificar` (FlowSim + CompileCheck verdes).
2. **Feche o editor do Unity** neste projeto. Um Unity por vez: se existir `Builds\.trava` sem build rodando, apague-a.
3. Gere o executável (~1-2 min; na primeira vez bem mais, compila os shaders):
   ```powershell
   powershell -ExecutionPolicy Bypass -File Tools\GerarExecutavel.ps1 [-Diagnostico] [-PularLicencas]
   ```
   - Antes do Unity ele roda `AuditaLicencas --disco` (o disco inteiro, inclusive o que o `.gitignore` esconde) e `AuditaAssets --disco` (só aviso).
   - Sai com **3** se houver trava ou Unity aberto, **4** se faltar licença no manifesto, e com o código do Unity se o build falhar.
   - Log em `Builds\build.log`. Saída `Builds\Windows\TDFende.exe` (ou `Builds\Diagnostico\TDFende.exe` com `-Diagnostico`).
   - `-PularLicencas` só em emergência.
4. Rode a captura:
   ```powershell
   powershell -ExecutionPolicy Bypass -File Tools\Captura.ps1 -Saida C:\caminho\print.png [-Estresse] [-Extra -sem-grama] [-Diagnostico] [-Tempo 150]
   ```
   Abre o jogo em 1600×900, entra no Tower Wars (Normal), manda um de cada bicho, registra 12 s de movimento, tira os prints e fecha.
   `-Estresse` monta fim de partida (6 torres no máximo por lane, 4 rodadas de todos os bichos). Sem `-Saida`, vai para `%TEMP%\tdf_captura.png`.
5. Leia o código de saída: **0** passou, **1** reprovou (motivo na tela), **2** travou (passou do `-Tempo`, padrão 150 s) ou não há executável.
6. Arquivos ao lado do `-Saida` (com `print.png`): `print.png`, `print_grama.png`, `print_bicho_<Nome>.png` e `print_bicho_<Nome>_simples.png`
   (material do bicho × material simples) e `print_metricas.json` (resultado, falhas, boot, quadro p50/p95/p99, triângulos, draws, SetPass, cobertura, `orcamento`, `camada_privada`).
7. Log: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\TDFende\Player.log`. Linhas que importam:
   `[TDFende] captura: PASSOU` ou `REPROVOU (n): motivo | motivo`; `captura, cobertura <bicho>`; `captura, desempenho:`;
   `captura, movimento` (salto ~18,8 é a volta de lane, qualquer outro é defeito); `captura, orçamento: AVISO ...`; `[TDFende] build <hash>`.
8. **Olhe cada print com a ferramenta Read antes de afirmar qualquer coisa visual.** Código 0 não prova que ficou bonito nem que o objeto aparece certo.

## O que reprova (código 1)
- Bicho com cobertura abaixo de 0,5% no retrato, ou tipo de bicho que não apareceu.
- Shader não suportado ou material nulo num bicho; `variante fora do ShaderKeep: <shader> [keywords]`.
- Exceção no log ou aviso da lista `SmokeCapture.LogLint`; mais de 60 s sem terminar.
- Executável sem selo `build <hash>`; tabelas do `ModelLib` sem bater com `ViewKeys`, ou "sem modelo para" no log.
- No build de diagnóstico: `variant X not found` no Player.log.
- Tempo de quadro e triângulos **só avisam** (orçamento do TEC-06, tetos `Budget*` no `SmokeCapture`).

## Armadilhas (MANUAL, seção 5)
- Shader pego por nome some do build: combinação nova de shader + keywords entra em `BuildJogo.Shaders` (exata).
- Grama invisível = variante de instancing descartada. Terreno com `drawInstanced = true` sai **preto** no exe: não religue sem print.
- Objeto invisível: some nos dois retratos = malha ou esqueleto; aparece só no `_simples` = material ou variante.
- Mexeu em material, keyword, URP ou GraphicsSettings: rode `GerarExecutavel.ps1 -Diagnostico` e `Captura.ps1 -Diagnostico`.
- O Player.log às vezes não mostra erro de shader: o print é a prova.
- Selo com `-dirty` = gerado com mudança sem commit. Print "com privado" não prova o que um clone novo vê.
- p95 normal pula entre 7,0 e 13,9 ms pelo sincronismo vertical (144 Hz). Compare sempre `-Estresse` com `-Estresse`.
- Dois builds seguidos deixam o git limpo. Se o build mexer nos `.asset` do URP ou em `ProjectSettings/`, mostre o diff e pergunte ao Felipe.
- Precisa ver outra coisa? Acrescente ao `SmokeCapture.cs` em vez de pedir print ao Felipe.

## Depois
- Mudança visual: rode `Captura.ps1 -Estresse` antes e depois e ponha os números (p95, triângulos, draws, SetPass) no commit.
- Cite no commit o hash do selo e o código de saída. Prints para aprovação vão juntos numa página (artifact) com sim/não por item.
- Marque o cartão no `ROADMAP.md` e no arquivo da fase. Feche com `DONE`, `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT` ou `BLOCKED`.
