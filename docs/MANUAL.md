# Manual de trabalho do TDFende

Para qualquer sessão do Claude, inclusive de um modelo mais barato, fazer o trabalho do jeito
certo sem redescobrir o que já custou caro. Leia antes de mexer. O **o que fazer** está no
[`ROADMAP.md`](../ROADMAP.md); este arquivo é o **como fazer**.

Regra de ouro: **nunca diga que algo funciona sem ter visto** — no FlowSim (lógica), no
CompileCheck (compila no Unity) e no **print do executável** (visual). O editor do Unity e o
executável se comportam diferente; o que o Felipe roda é o executável.

---

## 1. Mapa rápido

- Unity **6000.3.11f1**, URP. Não há cena montada: `Runtime/Core/GameBootstrap` cria tudo em
  código ao dar Play (ou ao abrir o executável). A cena de build é `Assets/Scenes/Jogo.unity`
  (só a câmera).
- Dois modos: **TD clássico** (`Core/GameController`) e **Tower Wars** (`TowerWars/TowerWarsController`).
- **Regras = lógica pura** em `Runtime/Sim/` (sem `MonoBehaviour`, sem `Time`, sem `Random` do
  Unity; passo fixo de 30 Hz, determinístico por semente). A pasta `TowerWars/` é só **vista**
  (`LaneView`, `EnemyView`, `ProjectileView`), que lê o `LaneSim` e desenha.
- Arte: `Runtime/Art/` (`ModelLib` = modelos feitos em código; `ArtFactory` = troca por modelo
  baixado se existir em `Resources`; `AnimalLoader` = bichos baixados; `TowerStages` = 3
  modelos por torre), `Runtime/World/` (terreno e grama), `Visuals/`, `Vfx`, `SceneAmbience`.
- Assets: `Assets/Resources/TDFende/` (`Torres/`, `Bichos/`, `Cenario/`, `Textures/`,
  `ShaderKeep/`) e `Assets/Resources/Art/` (grama, chão, céu da Poly Haven).
- Créditos e licenças de tudo que veio de fora: [`THIRD_PARTY.md`](../THIRD_PARTY.md).
  Como baixar arte: [`ASSETS.md`](../ASSETS.md).

## 2. Git — a rotina que não perde trabalho

1. Começo: `git pull --rebase --autostash` na `main`.
2. Commit **direto na `main`**, mensagem em português, terminando com a linha
   `Co-Authored-By: Claude ... <noreply@anthropic.com>`.
3. O **GitHub Actions** (`.github/workflows/flowsim.yml`) roda o FlowSim a cada push na `main`
   (vale também para sessão na nuvem e clone novo). O **pre-commit** (`.githooks/pre-commit`) compila o Runtime contra as DLLs reais do Unity
   (`Tools/CompileCheck`), compila Editor + URP (`Tools/CompileCheckUrp`) e roda os testes
   (`Tools/FlowSim`, ~3 min). Se falhar, **conserte e rode `git commit` de novo com os mesmos
   arquivos já no índice**.
4. **NUNCA** faça `git stash` / `git stash pop` entre um commit que falhou e a nova tentativa:
   o `stash pop` devolve as mudanças mas **esvazia o índice**, e o commit seguinte sai só com
   parte dos arquivos. Isso já aconteceu (06/10/2026: a correção do chão ficou fora da `main`).
5. Depois de **todo** commit: `git show --stat HEAD` e confira que os arquivos esperados estão lá.
6. Push: `git pull --rebase --autostash` e `git push`. Depois, `git fetch` e
   `git status -sb` precisa mostrar `## main...origin/main` sem "ahead".
7. Arquivos que o Unity regrava sozinho ao rodar em batch (`Assets/Settings/URP_Asset.asset`,
   `URP_Renderer.asset`, `UniversalRenderPipelineGlobalSettings.asset`,
   `DefaultVolumeProfile.asset`, `ProjectSettings/*.asset`) **não entram em commit** sem o
   Felipe confirmar que abriu o editor e está tudo certo. A versão de 09/10/2026 já está
   commitada (TEC-01): dois builds seguidos deixam o git limpo. Se um build voltar a mexer
   neles, é mudança nova — mostre o diff e pergunte. Se o `git status` mostrar `M` neles mas
   `git diff` vier vazio, é só o Unity regravando com fim de linha LF: `git add` neles limpa
   o status sem mudar nada.
8. `Builds/`, `Tools/*/glb/`, `Tools/*/out/`, `Tools/*/v_*.jpg|png`, `__pycache__/` são ignorados.
9. **Repositório público, arte pesada no PC** (decisão do Felipe, 09/10/2026, TEC-34). O
   repositório no GitHub fica público até a página da Steam; o que entra no histórico não sai
   mais. Por isso:
   - nenhum arquivo acima de **10 MB** entra no git — o pre-commit bloqueia (o maior hoje tem
     6,8 MB). Sem Git LFS;
   - GLB original, fonte de modelo, quarentena e executável entregue ficam **no PC**, nas pastas
     ignoradas (`Tools/*/glb/`, `Builds/`, `C:\Users\Felip\TDFende-quarentena`), com cópia no
     D: (`Tools\Backup.ps1`, TEC-26);
   - asset da Asset Store/Fab e de origem não comprovada **nunca** entra no git (seção 7);
   - antes de commitar arte, confira a licença: público = redistribuído.
10. **Sessões paralelas** (TEC-20): o `Tools/Sincronizar.ps1` (pull + commit automático a cada
    2 min) foi aposentado em 09/10/2026 — cada sessão faz pull/commit/push na mão, como acima.
    Só **um** Unity por vez nesta pasta: o `GerarExecutavel.ps1` cria a trava `Builds\.trava`
    e se recusa a rodar se ela existir (ou se o editor estiver aberto). Trabalho só de Sim
    (FlowSim) ou Python (conversores) pode ir num worktree à parte
    (`git worktree add ..\TDFende-sim main`), que não precisa da `Library` do Unity. Antes de
    pegar uma tarefa do `ROADMAP.md`, escreva nela `(reservado: sessão <data hora>)` e faça push;
    quem chegar depois pega a próxima.
11. **Backup do que não está no git** (TEC-26): `powershell -ExecutionPolicy Bypass -File
    Tools\Backup.ps1` copia para `D:\TDFende-backup` (outro disco físico: o C: é o disco 1, o D:
    o 0) os GLBs originais (`Tools/*/glb/`), a quarentena (`C:\Users\Felip\TDFende-quarentena`) e
    o último executável. `robocopy /E` sem apagar: o que some do PC continua no D:. Registro em
    `D:\TDFende-backup\backup.log`. **Restaurar:** copie a pasta de volta (`ConverterBichos-glb`
    → `Tools\ConverterBichos\glb`, `ConverterTorres-glb` → `Tools\ConverterTorres\glb`,
    `quarentena` → `C:\Users\Felip\TDFende-quarentena`). Teste de restauração em 09/10/2026:
    GLB do tigre, Fortaleza.glb, TDFende.exe e um arquivo da quarentena voltaram com o mesmo
    SHA-256. O D: não protege contra roubo ou incêndio do PC.

## 3. Comandos

| O quê | Comando |
|---|---|
| Testes da lógica | `dotnet run --project Tools/FlowSim -v quiet --nologo` (fim: `>>> TODOS OS TESTES PASSARAM`) |
| Compila o Runtime no Unity | `dotnet build Tools/CompileCheck -v quiet --nologo` (precisa `0 Erro(s)`) |
| Compila Editor + URP | `dotnet build Tools/CompileCheckUrp -v quiet --nologo` |
| Gera o executável | `powershell -ExecutionPolicy Bypass -File Tools\GerarExecutavel.ps1` (Unity **fechado**; ~1-2 min; log em `Builds\build.log`) |
| Teste de fumaça do executável | ver seção 4 |
| Preview da arte sem Unity | `cd Tools/ArtPreview && dotnet run -v quiet` → `out/art.json` (**não** passe `--nologo` depois do `dotnet run`: ele vira argumento do programa) |
| Python (conversores, Blender sem janela) | `"$LOCALAPPDATA/Programs/Python/Python311/python.exe"` (3.11.9, com `bpy` 5.0.1 e `pillow`). Não existe `python` no PATH. |

Notas:
- O `FlowSim.csproj` e o `CompileCheck.csproj` **listam arquivos/módulos explicitamente**:
  arquivo novo em `Runtime/Sim/` (ou lógica pura testada) precisa entrar no `FlowSim.csproj`;
  API do Unity de um módulo novo (ex.: `ScreenCapture`, `Animation`, `ImageConversion`) precisa
  da referência `UnityEngine.<Módulo>Module` no `CompileCheck.csproj` (e no `CompileCheckUrp`
  se o Editor usar).
- Scripts Python passados por *heredoc* no Bash do Windows perdem acentos (lê em cp1252). Para
  editar arquivo com acento, use a ferramenta de edição, não `python - <<EOF`.
- Windows **não diferencia maiúsculas**: `Bichos/textures` e `Bichos/Textures` são a MESMA
  pasta. Antes de mover/apagar pasta, liste o conteúdo e veja o que é versionado
  (`git ls-files <pasta>`).

### Fingerprint da partida (TEC-31)

`MatchRunner.StateFingerprint()` é a prova de que duas execuções (ao vivo, replay, arquivo) deram a mesma partida.
Termina com `#<16 dígitos hexa>`: um hash FNV-1a (`StateHash`) sobre as duas lanes (`SimFingerprint`: vidas, ouro,
placar, cada inimigo com posição e vida e gelo e fogo, cada torre, cada tiro em voo) e sobre o **número de sorteios**
(`CountingRandom`, que conta sem mudar a sequência: o FlowSim compara com um `Random` puro). Números com vírgula entram
em passos de 1/256: 0,01 de deslocamento muda o hash, ruído de 1e-3 não. **Não cobre** os temporizadores internos da
IA; o que ela decide aparece em torres, envios e ouro. Mexeu em regra da Sim: rode `dotnet run --project Tools/FlowSim
-v quiet -- match 12` antes e depois e compare a saída (deve ser idêntica se a regra não mudou de propósito).

## 4. Ver o jogo sem o Felipe: `-captura`

```
powershell -ExecutionPolicy Bypass -File Tools\GerarExecutavel.ps1
powershell -ExecutionPolicy Bypass -File Tools\Captura.ps1 [-Saida C:\caminho\print.png] [-Estresse]
```

`Captura.ps1` roda `TDFende.exe -captura <print.png>` em janela 1600×900, espera até 150 s e
**devolve o código de saída do jogo: 0 passou, 1 reprovou, 2 travou** (fechado à força). Na tela
saem a cobertura de cada bicho, o desempenho e o motivo de cada reprovação. O jogo entra no
Tower Wars (Normal), manda **um de cada bicho** na lane da IA, registra o movimento por 12 s,
tira `print.png`, depois aproxima a câmera da grama e tira `print_grama.png`, e fecha sozinho.
Leia os prints com a ferramenta de leitura de imagem.

**Reprova (código 1):** bicho com cobertura abaixo de 0,5% no retrato (o corpo, sem o anel do
time, é desenhado sozinho na camada 31 sobre fundo preto e conta-se o que não é fundo), tipo de
bicho que não apareceu, shader não suportado ou material nulo num bicho, exceção no log, aviso
da lista `SmokeCapture.LogLint` no log (aviso que já foi defeito; acrescente lá), mais de 60 s
sem terminar. Tempo de quadro e triângulos **só são medidos** (o orçamento é o TEC-06).

`-Extra` passa argumentos a mais para o jogo (ex.: `-Extra -sem-grama`, que desliga a grama 3D
para medir quanto ela custa). `-Estresse` (no exe: `-estresse`) monta um fim de partida: as 6 torres no nível máximo em cada
lane e 4 rodadas de todos os bichos nas duas lanes. Referência de 09/10/2026 (1600×900), depois
do VIS-11a: normal p95 7,0 ms, ~14 milhões de triângulos; estresse p95 41,7 ms com 60 bichos,
~23 milhões (sem grama o p95 do estresse é o mesmo: o gargalo não é a grama).

**Selo:** todo executável mostra "build <hash>" no canto inferior direito e grava
`[TDFende] build <hash>` no Player.log (`git describe --always --dirty`; "-dirty" = gerado com
mudança sem commit). Executável sem selo reprova a captura.

### Orçamento de desempenho (TEC-06) — só aviso

A captura compara cada item com o teto e escreve `orcamento` no `print_metricas.json` e
`[TDFende] captura, orçamento: AVISO ...` no log (o `Captura.ps1` mostra). **Não reprova**: com
a arte mudando, um teto rígido deixaria a captura sempre com 1. **Regra: nenhuma mudança visual
sem a linha de desempenho de antes e de depois** (rode `Captura.ps1 -Estresse` nas duas pontas e
ponha os números no commit). Tetos em `SmokeCapture` (constantes `Budget*`).

Referência: RTX 4070 Ti, janela 1600×900, qualidade padrão, `-Estresse`. Medido em 09/10/2026
(build 97e943d):

| Item | Teto | Medido | |
|---|---|---|---|
| Quadro p95 | 8 ms | 41,7 ms (normal: 7,0 a 13,9 ms*) | acima |
| Boot | 5 s | 4,4 s | ok |
| Triângulos na cena | 2,5 M | 24,6 M (normal: ~17 M) | acima |
| Draws | 2.000 | 2.552 (normal: ~1.050) | acima |
| SetPass | 500 | 569 (normal: ~290) | acima |
| Grama (todas as touceiras) | 400 k | 4,0 M | acima |
| Fortaleza | 40 k | 92,6 k | acima |
| Torre (cada estágio) | 25 k | 43 k (Sentinela 3) a 95,6 k (Gelo 3) | todas acima |
| Bicho | 15 k | 1,6 k (águia) a 25,5 k (rato); acima: rato, cachorro 17,6 k, rinoceronte 16 k | 3 acima |

\* o p95 normal pula entre 7,0 e 13,9 ms porque o quadro trava no sincronismo vertical (144 Hz:
um quadro perdido vira dois). Nível Baixo, quando existir: ≤ 300 k triângulos na cena (meta antiga do celular; recalibrar na VIS-29).
Os tetos por asset pressupõem LOD (TEC-10): de perto o modelo cheio, de longe o reduzido.

**AuditaAssets (TEC-10a):** `dotnet run --project Tools/AuditaAssets -v quiet --nologo -- --git`
(ou `--disco`, `--detalhe`) mede cada FBX de `Assets/Resources` **sem abrir o Unity** (lê os índices de
polígonos do FBX binário; confere com o `-captura` nas peças que não têm código por cima) e compara com os
tetos acima: torre 25 k, fortaleza 40 k, bicho 15 k, acampamento 25 k (provisório). Também avisa de textura
acima de 2048 px (teto provisório), de textura importada sem compressão e das texturas em `.bytes` (JPG
decodificado em runtime: o TEC-09 as migra para BC7/BC5; textura nova entra como Texture2D importada).
Roda no pre-commit e no `GerarExecutavel.ps1`, **só avisando**; `--estrito` faz reprovar quando a arte
assentar. Em 09/10/2026: 21 dos 30 modelos medidos acima do teto, o pior a Torre_Gelo_3 (94,7 k).

Arquivos ao lado do print: `print_bicho_<Nome>.png` e `print_bicho_<Nome>_simples.png` (retrato
com o material do bicho e com um material simples) e `print_metricas.json` (resultado, falhas,
boot, quadro p50/p95/p99 em ms, triângulos/draws/SetPass máximos, exceções, erros, cobertura de
cada bicho).

No log `%USERPROFILE%\AppData\LocalLow\DefaultCompany\TDFende\Player.log`:
- `[TDFende] captura: PASSOU` ou `REPROVOU (n): motivo | motivo` — a linha final.
- `[TDFende] captura, cobertura <bicho>: x% (material simples y%)`.
- `[TDFende] captura, desempenho:` — quadro p50/p95/p99, triângulos, draws, SetPass, boot.
- `[TDFende] captura, cena:` — shader/keywords/instancing do terreno, texturas das camadas,
  céu, luz ambiente, sol.
- `[TDFende] captura, movimento` — por bicho: `(animado | animação parada | sem animação)`,
  passo médio, `saltos` (salto ~18,8 = volta para a outra lane, é regra; qualquer outro é
  defeito), FPS médio.
- `[TDFende] captura, bicho ...` — renderers, materiais, keywords e limites do bicho fotografado.
- Também: `[TDFende] bichos de verdade: ...` (quais modelos baixados carregaram) e
  `[TDFende] grama 3D: N touceiras ... M triângulos`.

Se precisar ver outra coisa, **acrescente ao `SmokeCapture.cs`** (é para isso que ele existe) em
vez de pedir print ao Felipe. `Application.runInBackground` é ligado ali porque, aberto por
script, a janela nasce sem foco e o Unity pausa o jogo.

## 5. Armadilhas do executável (já custaram horas)

1. **Shader achado por nome é descartado no build.** O código pega shader por nome
   (`Runtime/Core/ShaderRefs.cs`, a lista única; nada de `Shader.Find` solto) e nenhum
   material de cena referencia esses shaders. Solução: `Editor/BuildJogo.cs` cria, a cada
   build, um material por combinação **shader + keywords** em `Resources/TDFende/ShaderKeep/`.
   **Combinação nova de keywords no código = acrescentar em `BuildJogo.Shaders`.** A
   combinação tem que ser exata (`[_TERRAIN_INSTANCED_PERPIXEL_NORMAL]` sozinha é diferente de
   `[_NORMALMAP, _TERRAIN_INSTANCED_PERPIXEL_NORMAL]`).
2. **Variantes de instancing são descartadas** (`GraphicsSettings`: Instancing Variants = Strip
   Unused). Os materiais do ShaderKeep têm `enableInstancing = true` por isso. Sintoma: grama
   (`Graphics.RenderMeshInstanced`) invisível.
3. **Terreno com `drawInstanced = true` sai PRETO no executável** (no editor não). Está
   desligado em `World/GroundBuilder.cs`. Não religue sem provar com print.
4. **Objeto invisível**: primeiro descubra se é material ou malha. A captura fotografa cada
   bicho duas vezes (`_bicho_<Nome>.png` com o material dele e `_bicho_<Nome>_simples.png` com
   um material simples). Some nos dois = malha/esqueleto; aparece só no simples = material ou
   variante de shader (aí vale o item 1 ou 2).
5. **Bicho com esqueleto medido pela caixa errada.** A caixa guardada no `SkinnedMeshRenderer`
   pode não ter nada a ver com a malha animada: em cachorro, lobo, rato e águia a caixa dizia o
   tamanho certo e a malha animada era quase um ponto (escala do esqueleto × malha diferente em
   cada modelo baixado). Por isso o `AnimalLoader` mede com `BakeMesh` na pose de andar
   (`RealBounds`) e reajusta a caixa de recorte (`FitCullingBounds`). O log
   `captura, bicho ... pose real: tamanho` mostra a medida verdadeira. (BUG-01, 09/10/2026)
6. **Material criado em runtime com keyword ligada/desligada** (ex.: `AnimalLoader.FixSurface`)
   cai numa combinação que o build pode não ter, e o Unity troca **em silêncio** pela variante
   mais parecida (sem emissão, sem normal map...). A captura confere sozinha: todo material
   criado em runtime em uso tem que ter a mesma combinação de um material do ShaderKeep; se não
   tiver, reprova com `variante fora do ShaderKeep: <shader> [keywords]` — acrescente essa
   combinação em `BuildJogo.Shaders`.
7. **Névoa some no executável.** O corte automático de névoa (GraphicsSettings: Fog Modes =
   Automatic) só guarda `FOG_LINEAR` se uma cena do build usar névoa linear; o `SceneAmbience`
   liga a névoa em runtime. Por isso o `BuildJogo.EnsureScene` deixa a névoa linear ligada na
   `Jogo.unity`. (Achado pelo build de diagnóstico, 09/10/2026: até então o exe não tinha névoa.)
8. **Build de diagnóstico** (`GerarExecutavel.ps1 -Diagnostico` + `Captura.ps1 -Diagnostico`):
   gera `Builds\Diagnostico\TDFende.exe` com `strictShaderVariantMatching`. Variante faltando
   vira erro `...: variant X not found.` no Player.log (a captura lê o arquivo e reprova) e o
   objeto some. Rode depois de mexer em material, keyword, URP ou GraphicsSettings.
9. O `Player.log` só é escrito pelo executável; erros de shader às vezes **não** aparecem nele
   (fora do build de diagnóstico) — o print é a prova.

## 6. Pipelines de arte

### Torres (`Tools/ConverterTorres`)
- `torres.json`: uma entrada por modelo final (`Torre_<Tipo>_<estágio>`, `Fortaleza`,
  `Acampamento`). Campos explicados no `_comentario` do arquivo (raio, altura, base,
  piso_turret, piso auto/topo, giro, tom, modo inteiro, largura).
- `glb/<nome>.glb` (fora do Git) → `converte.py <nome>` → `out/<nome>.fbx` +
  `out/Textures/<nome>_cor.bytes|_normal.bytes` → `verifica.py <nome>` desenha `v_<nome>.jpg`
  (código × modelo, primeiro e último nível do estágio). Rode `dotnet run` no ArtPreview antes.
- Copie `out/*.fbx` para `Assets/Resources/TDFende/Torres/` e `out/Textures/*.bytes` para
  `Torres/Textures/`. Estágios: níveis 1-2 → `_1`, 3-4 → `_2`, 5-6 → `_3` (`TowerStages`).
- A torreta do código fica no piso plano do topo; sem piso (telhado, cristal, braseiro), no
  ponto mais alto. Enfeites de nível do topo são omitidos em modelo de estágio.

### Bichos (`Tools/ConverterBichos`)
- Com esqueleto: `bichos.json` (campos: `uid` = nome do GLB em `glb/`, `acoes` = mapa
  Walk/Run/Idle/Death → nome da ação no GLB, `tris`, opcionais `cabeca`/`rabo` = ossos para
  achar a frente, `frente` = vetor, `giro` = graus forçados, `keep`/`drop`/`opacos`, `px`).
  `converte.py <nome>` → `out/<nome>.fbx` + `out/Textures/`; `verifica.py <nome>` → `v_<nome>.jpg`
  (4 quadros andando de lado + frente). A **vista de frente** tem que estar reta.
- Sem esqueleto: `estatico.py <nome> <arquivo.glb> [giro]` (o glTF importa em quatérnio; o
  script põe `rotation_mode = 'XYZ'` antes de girar).
- Instale em `Assets/Resources/TDFende/Bichos/<nome>.fbx` e `Bichos/Textures/`. O nome do
  arquivo só precisa conter o bicho (`AnimalLoader.Keywords`). Para ir ao Git, acrescente a
  exceção `!Assets/Resources/TDFende/Bichos/<nome>.fbx` no `.gitignore` **só** se a licença
  permitir redistribuir.
- GLB original (o que o Meshy entrega, antes de converter) **nunca** em `Resources`: fica em
  `Tools/ConverterBichos/glb/` (fora do git). Os do tigre e do javali feitos pelo Felipe
  (image-to-3d, 270 MB) estão em `glb/originais-meshy/`.

### Camada privada de arte (TEC-23)
Para pacote pago e arte cuja licença **proíbe redistribuir** (Asset Store, Fab, Mixamo...): o
repositório é público, então o arquivo fica só no PC.
- **Onde:** `Assets/_Privado/Resources/TDFende/Privado/`, **fora do git** (`.gitignore`) e dentro do
  `Tools\Backup.ps1` (item `camada-privada`). O Unity junta todas as pastas `Resources` no build, então
  o executável do Felipe leva a camada e o de um clone novo (ou da nuvem e do GitHub) roda sem ela.
- **Espelha a pública:** o que o jogo procura em `TDFende/Torres/Torre_Gelo_1` procura antes em
  `TDFende/Privado/Torres/Torre_Gelo_1`; `Art/Ground/x` em `TDFende/Privado/Art/Ground/x`. Achou, usa;
  não achou, cai no público; sem arquivo nenhum, o procedural. A regra está em `ArtLayerPaths` (pura,
  com teste no FlowSim) e `ArtLayers` (todo carregador de arte passa por ela, e o áudio da Fase 8 também
  deve). Bicho privado do mesmo animal vence o público (`AnimalLoader`); os clipes são procurados na pasta
  da camada de onde o bicho veio.
- **Importação:** as regras do editor (`ArtRoots`) valem nas duas pastas, então um pacote entra com as
  mesmas configurações (Legacy, clipes em loop, normal map) dos baixados.
- **Licença:** todo arquivo da camada privada também entra em `docs/licencas/manifesto.json`, com
  `"repoPublico": false` e licença `EULA-loja` ou `Adobe-Mixamo` (seção 7). O `GerarExecutavel.ps1`
  (modo `--disco`) recusa o build com arquivo privado sem entrada; o pre-commit reprova se algum
  arquivo `repoPublico: false` entrar no índice. **Nunca** `git add -f` na pasta.
- **Prova no executável:** o `-captura` escreve `[TDFende] captura, camada privada: com privado (N
  arquivos ...)` ou `sem privado`, e `camada_privada` no `print_metricas.json`. O log do jogo diz cada
  arquivo servido: `[TDFende] camada privada: <caminho> (no lugar de <público>)`. Um print "com privado"
  não vale como prova do que um clone novo vai ver.
- **Testar a camada** sem comprar nada: copie um FBX de bicho para
  `Assets/_Privado/Resources/TDFende/Privado/Bichos/` (e dê a ele a entrada do manifesto), gere o exe e
  confira o log; apague a pasta, gere de novo e confira que cai no público.

### Poly Haven (CC0)
- API: `https://api.polyhaven.com/assets?t=models|textures`, `.../files/<id>`; mande
  User-Agent `TDFende-BaixarArte/1.0`. Script: `Tools/BaixarArte.ps1`.
- Árvores da Poly Haven têm **milhões de triângulos** (pinheiro 17 M) — não servem para
  floresta inteira.
- A grama `grass_medium_*` é feita de lâminas finas recortadas por alfa (10-16% de cobertura,
  oliva-escuro): de longe vira fiapos. Aumentar tamanho/densidade derrubou o FPS de ~100 para 54.

### Comunidade do Meshy (CC0) — baixar pronto
- Busca pública: `https://api.meshy.ai/web/public/v2/showcases?search=<termo>&pageNum=1&pageSize=50`
  (ou `author=<nome>`). Filtre `license == "cc0"`. `Tools/ComunidadeMeshy/busca.py` monta folhas.
- Página de um modelo: `https://www.meshy.ai/posts/<id>`. Download exige estar **logado** (o
  Chrome do Felipe, ferramentas Claude in Chrome) e **não gastou crédito** em 04/10/2026.
- Na página: o 3D só carrega se a aba for "vista" — tire um `screenshot` logo após navegar; o
  primeiro clique no ícone de download (verde, barra de baixo) às vezes não abre o menu: clique,
  confirme com `zoom` que abriu "Configurações de Download", formato `glb`, botão **Baixar**.
- Modelos antigos podem vir **sem botão de download**, e modelos listados na busca podem estar
  **removidos** ("Este modelo não está mais disponível"). Escolha outro.
- O arquivo cai em `%USERPROFILE%\Downloads\Meshy_AI_<prompt>_<data>_texture.glb`. Outra
  sessão/pessoa pode estar baixando ao mesmo tempo: identifique o arquivo pelo prefixo do
  prompt, não só pelo mais novo.

### Meshy na conta do Felipe — gerar / rig / animar
- **Sempre perguntar antes de gastar crédito** (dizer quantos). Geração ~30 créditos por modelo
  com textura. Tabela oficial da API: rig 5, animação 3 por ação, remesh 5. Em 06/10/2026 o
  remesh e o rig no site saíram **sem descontar** (saldo continuou 842).
- Fluxo de rig de quadrúpede (web): Espaço de Trabalho → **Animar** (barra lateral) → escolher
  o modelo na biblioteca → **Rig** → (1) redução de malha (10K, Triângulo, 0 créditos) →
  Confirmar → esperar → selecionar a versão reduzida → **Rig** → tipo **Quadrúpede de
  Cachorro** → Próximo → orientação (bicho virado para a **esquerda**) → Próximo → marcadores
  (queixo, ombros, antebraço, metacarpo, pata, cintura, nádegas, cauda, canela, garfos, pé):
  **confira e arraste** os que caírem fora do corpo → Confirmar.
- Quadrúpede só tem a animação **"Andando"** (já vem com o rig). Download: ícone verde →
  `glb`, Animação **Todos Adicionados**, **Arquivo único** ligado → Baixar
  (`Meshy_AI_<nome>_All_Animations.glb`). A ação no GLB se chama `Armature|Unreal Take|baselayer`
  (em `bichos.json`: `"acoes": {"Walk": "baselayer"}`); ossos `Hips`, `chest`, `head`, `tail*`.

### Sketchfab
- Busca pública: `https://api.sketchfab.com/v3/search?type=models&q=<termo>&downloadable=true&animated=true`;
  detalhes em `.../v3/models/<uid>`. Download pelo site logado (o Chrome do Felipe está logado).
- **Rejeite**: NonCommercial, NoDerivs, "personal use only" na descrição, "from <jogo>"/ripped,
  dezenas de animações com nome de jogo, e modelos cujo arquivo interno tenha o nome de **outro
  uid do Sketchfab** (sinal de reupload; ex.: o "Running Tiger" carregava
  `2f51f75f...fbx`, de um modelo apagado).

### Asset Store / Fab
- Licença padrão: uso comercial em jogo permitido, **redistribuir proibido** → arquivo só no PC
  (fora do Git). Pegar exige login da Unity/Epic (o Felipe faz) e importar pelo editor
  (Package Manager → My Assets). Clicar "Add to My Assets" aceita o EULA: **pergunte antes**.

## 7. Licenças (regras do projeto)

**Fonte única: [`docs/licencas/manifesto.json`](licencas/manifesto.json)** (TEC-22). Toda arte em
`Assets/Resources` (e em `Assets/_Privado`, `Assets/StreamingAssets`) tem que casar com **uma**
entrada: origem, autor, licença, link, o que alteramos, `ia` (propria/terceiro/nao), plano do
Meshy quando a origem é o Meshy, crédito e estado. Dali saem o `THIRD_PARTY.md` e o
`Assets/Resources/TDFende/creditos.txt` (a tela de créditos, META-02, lê este).

- **Asset novo = entrada nova no mesmo commit**, e depois
  `dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --gera`. Não edite
  `THIRD_PARTY.md` nem `creditos.txt` à mão: o pre-commit reprova se estiverem fora de sincronia.
- **Quem confere:** o pre-commit (`--git`: o que está no índice), o GitHub Actions (igual) e o
  `GerarExecutavel.ps1` (`--disco`: o disco inteiro, inclusive o que o `.gitignore` esconde, como
  Mixamo e pacotes pagos; `-PularLicencas` só em emergência). Reprova: arquivo sem entrada,
  entrada ambígua (dois padrões casando), licença com NC/ND/"uso pessoal" ou fora da lista,
  CC BY/MIT sem crédito, origem Meshy sem plano, Meshy com `ia: nao`, e arquivo de licença que
  proíbe redistribuir (`repoPublico: false`: Mixamo, Asset Store, Fab) **dentro do git**.
- **`estado: "pendente"` só avisa**, e diz o que falta conferir (hoje: o plano do Meshy na data
  em que o javali, o tigre e a primeira Torre_Canhao foram gerados). Pendente precisa fechar
  antes da página da Steam.
- Licença nova (ex.: uma de loja) não passa em silêncio: leia os termos, acrescente em
  `Program.cs` (tabela `Licencas`, com o que ela exige) e só então use no manifesto.
- Arquivo que **não** pode ir para o GitHub público (EULA que proíbe redistribuir): entrada com
  `"repoPublico": false`; fica só no PC. Vale também para a futura camada privada (TEC-23).

Regras gerais:
- Pode ir para o GitHub: CC0, CC BY (com crédito), MIT/Apache (com aviso), modelos gerados pela
  conta do Felipe no Meshy, e o que a comunidade do Meshy publicou (a galeria é CC0).
- Só no PC: Asset Store, Fab, Mixamo, qualquer EULA que proíba redistribuir.
- Não usar: origem não comprovada (reuploads, ripados de jogo, pacotes pagos republicados).
  O pacote "Realistic Animated Pack" está em `C:\Users\Felip\TDFende-quarentena` por isso.
- **IA e Steam (regra de jan/2026):** conteúdo gerado por IA que o jogador vê precisa ser
  declarado na página; ferramenta de desenvolvimento (assistente de código) não. O campo `ia` do
  manifesto alimenta essa declaração: hoje há modelos do Meshy `propria` (conta do Felipe) e
  `terceiro` (comunidade).

## 8. Direção de arte

**Realista e de última geração** (como um jogo atual da Steam), **não** low-poly, **não**
cartoon. Não precisa ser medieval. Prefira arte real (Poly Haven, comunidade Meshy CC0, Meshy da
conta do Felipe) a formas procedurais. Todo ganho visual tem que caber no orçamento de FPS
(medir no `-captura`).

## 9. Tetos e políticas (TEC-35)

- **Plataforma e pipeline (Felipe, 09/10/2026; substitui "URP é o teto").** O alvo é a Steam
  no PC (Windows). **Celular saiu da meta**; Steam Deck só se der, em qualidade baixa, sem
  compromisso. Com isso o teto sobe: o **HDRP** passa a ser o pipeline pretendido, desde que
  ganhe do URP na comparação **VIS-29** (início da Fase 2), medida no executável contra os
  jogos-régua. **Unreal 5 é plano B**: só entra se nem o HDRP passar no Portão Visual, e aí com
  uma fatia de comparação própria antes de qualquer porte. Até a VIS-29 decidir, o projeto
  continua em URP e nenhuma sessão troca de pipeline fora dela. O hardware de referência é a
  RTX 4070 Ti do Felipe (nível Ultra), sem largar o perfil mínimo do TEC-33.
- **Patch do Unity:** o projeto fica na linha 6000.3 (hoje 6000.3.11f1). Antes de cada build
  **público** (Steam, itch, demo), atualize para o último 6000.3.x pelo Hub, rode o pre-commit,
  gere o executável e a captura. Motivo concreto: a CVE-2025-59489 (carregamento de arquivo
  inseguro, nota 8,4) atingia executáveis de 2017.1 até 6000.3.0b3; a 6000.3.11f1 já tem a
  correção, mas a próxima falha só se corrige recompilando. Avisos em
  https://unity.com/security.
- **Unity Personal (gratuito):** vale enquanto a receita **e** o financiamento somados dos últimos
  12 meses ficarem até US$ 200 mil (regra de 2026). Acima disso, Unity Pro por assento
  (~US$ 2.310/ano). Com o Unity 6 a tela "Made with Unity" é opcional. Confira
  https://unity.com/products/pricing-updates antes de lançar — muda de um ano para outro.
- **Critério de "architectural"** (aprovado pelo Felipe em 09/10/2026, está no CLAUDE.md):
  mudança em struct da Sim, no formato do replay ou do catálogo, comando novo, vida de torre,
  `MatchRules` e multiplayer. Isso pede design escrito e aprovado antes do código; o resto é
  "bounded". Os designs ficam em [`docs/designs/`](designs/), um arquivo por tarefa, com o problema visto
  rodando, o desenho, os testes e uma tabela "Decisões pedidas ao Felipe"; o código só começa depois do aval.
- **Repositório:** público, arte pesada no PC, nada acima de 10 MB no git (seção 2, item 9).

## 10. Nuvem × PC: quem faz o quê (09/10/2026)

Duas sessões do Claude trabalham no mesmo repositório, sempre pela `main`:

- **Sessão na nuvem** (claude.ai/code): simulação e Trilha S, ferramentas, conversão no Blender
  sem janela, downloads automáticos (Sketchfab pela API, Poly Haven), busca na comunidade do
  Meshy (`Tools/ComunidadeMeshy`), documentação e cronograma. Não tem Unity aberto nem Unreal:
  verifica com FlowSim e CompileCheck (DLLs do Unity vindas do NuGet, quando faltar o editor).
- **Sessão no PC do Felipe** (Claude Desktop, pasta do projeto): Unity e Unreal com as skills
  instaladas lá, executável e `-captura` na qualidade da 4070 Ti, prints para aprovação, e
  downloads que pedem login (GLB da comunidade do Meshy pelo Chrome do Felipe, já logado;
  cada download conta na cota do plano). Toda tarefa que precisa do editor ou do executável vai
  para esta sessão.

Uma tarefa que passa de uma sessão para a outra deixa no commit o que falta e onde parou.

## 11. Como o Felipe trabalha

- Respostas em português, resultado primeiro, curtas. Fechar com `DONE`,
  `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT` ou `BLOCKED`.
- Teste antes da correção, visto falhar pelo motivo certo (regra global dele).
- Uma coisa de cada vez, até o fim; fechou, commit + push.
- Três tentativas de correção falhando: parar, instrumentar cada fronteira (o `-captura` é o
  instrumento) em vez de chutar.
- Ele testa pelo **executável**, não pelo editor.

## 12. Mercado do gênero (MKT-01, 09/10/2026)

Relatório completo, com fontes: [`docs/mercado.md`](mercado.md). O que importa:

- **Colisão de nome.** Já existe um **"Line Tower Wars" grátis na Steam** (Mithryl Labs, 30/07/2026,
  bots + até 12 online, 7 reviews). Usar "Line Tower Wars" só como nome de gênero, nunca como título.
- **Líderes:** Legion TD 2 (US$24,99, 15,3 mil reviews, 86%, PvP ranqueado; campanhas solo são DLC de
  US$9,99 com pouquíssimas reviews) e Element TD 2 (EA a US$9,99, 1.0 a US$14,99; 3,2 mil reviews, 90%).
  Onda 2024-26 de indies do mesmo formato, quase todos com menos de 50 reviews e visual 2D ou estilizado.
- **Sem PvP:** não há caso comprovado de Line TD solo que tenha vendido bem; os que dependem só de PvP
  morrem (Tower Wars 2012, Warstone). Os TDs solo vizinhos vendem (Defense Grid, Sanctum 2). Nunca travar
  progressão atrás de PvP.
- **Lacuna:** não achei Line TD **realista** e de aparência atual (só por busca de tags; trate como
  indício). Pode ser falta de oferta ou custo de arte e de legibilidade.
- **Preço:** US$9,99 no EA, US$14,99 no 1.0; a faixa US$15-30 converte pior (amostra enviesada para sucessos).
- **Tags:** Tower Defense, Strategy, Singleplayer, Replay Value, Real Time Tactics, Realistic/3D. Não marcar
  PvP nem Multiplayer enquanto não existirem.
- **IA:** um review do Legion TD 2 já reclama de "AI slop". Declarar o uso de IA na página (campo `ia` do
  manifesto, seção 7) e pesar isso na escolha dos modelos do Meshy.
- **Recomendação do relatório:** EA sem multiplayer só se vendido como TD de estratégia solo contra IA,
  com o envio de bichos e a fronteira como diferencial; página cedo, demo no Next Fest, 3 dificuldades de IA.
  A decisão é do Felipe (ROADMAP, "Decisões").

## 13. Tema e direção de arte (VIS-01, 09/10/2026)

**Decidido pelo Felipe em 09/10/2026:**
- **Tema: fantasia realista baixa com natureza** — vale, floresta e planalto de rochas musgosas, ruínas de pedra,
  bestas realistas, luz de fim de tarde. Plano B se a fatia de beleza reprovar: natureza/expedição.
- **Jogo-régua: Total War: Warhammer III.** A fatia de beleza (VIS-27) é medida ao lado dele.
- **Verba para pacotes pagos: nenhuma.** Arte nova só de CC0, CC BY ou do Meshy da conta do Felipe (com aprovação
  antes, seção 14). Não comprar o pacote de animais nem kit de castelo.
- **Declarar IA na Steam: sim** (campo `ia` do manifesto, seção 7).

Análise completa, bíblia de arte e relatórios dos três temas: [`docs/arte/tema.md`](arte/tema.md) e
[`docs/arte/`](arte/). Em resumo, o que levou a essa escolha:
- as notas dos três temas empatam (diferença menor que o ruído de quem olhou só miniaturas);
- Gelo, Fogo e Ar realistas prontos não existem em nenhum tema; o que é de graça e bom em torre, fortaleza e
  acampamento foi gerado por IA no Meshy;
- fantasia realista baixa é o único tema em que as 21 peças que já estão no jogo são o próprio tema;
- o pacote pago de animais traz "Editorial Use Only" na listagem e, de qualquer forma, está fora da verba.

**Efeito sobre os bichos:** sem pacote, o elenco não passa de 9 a 11 espécies (`docs/arte/elenco.md`). O
Portão Visual (Fase 2) confirma ou derruba o tema.

## 14. Plano de gasto dos créditos Meshy (TEC-24, 09/10/2026)

Saldo lido na API em 09/10/2026: **842 créditos**. Livro-caixa: [`docs/meshy-livro-caixa.md`](meshy-livro-caixa.md)
(há 150 créditos gastos entre 04 e 09/10 sem registro; o Felipe confere no histórico do site).

1. **Meshy só onde não há CC0 ou CC BY de qualidade.** Hoje a galeria da comunidade já cobre torres, fortaleza e
   acampamento; gerar torre inteira (30 por modelo, 18 estágios = ~540) está fora de questão.
2. **Peças e lacunas, não conjuntos:** um prop, um adereço de topo, um bicho que o inventário não achou, retextura.
3. **Piloto antes de lote.** Um modelo, aprovado por critério medido: normal map presente, albedo sem sombra
   assada, triângulos dentro do teto (`Tools/AuditaAssets`). Só então o lote.
4. **Um pedido ao Felipe por lote**, dizendo quantos créditos e o que sai. Sem resposta "sim", não gasta.
5. **Tetos e reservas (sugestão, a aprovar):** gasto acumulado até 600, com **pelo menos 200 de reserva** para
   bichos e para o tier 2 (Fase 13).
6. **Preços:** só valem os que o piloto confirmar no saldo. Observado: texto para 3D com textura = 30 (preview
   20 + textura 10, 04/10). Pela tabela da API: remesh 5, rig 5, animação 3 por ação. Em 06/10 o remesh e o rig
   no site saíram sem descontar, mas **não conte com isso**. "Ataque e morte a ~3 créditos" para bípede: não
   confirmado.
7. **Depois de cada operação:** ler o saldo (`GET /openapi/v1/balance`), anotar no livro-caixa e, se o modelo
   entra no jogo, no manifesto (seção 7: origem Meshy pede `planoMeshy` e `ia: propria`).
