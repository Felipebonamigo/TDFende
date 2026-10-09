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
3. O **pre-commit** (`.githooks/pre-commit`) compila o Runtime contra as DLLs reais do Unity
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
   Felipe confirmar que abriu o editor e está tudo certo.
8. `Builds/`, `Tools/*/glb/`, `Tools/*/out/`, `Tools/*/v_*.jpg|png`, `__pycache__/` são ignorados.

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

1. **Shader achado por nome é descartado no build.** O código usa `Shader.Find(...)` e nenhum
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
   cai numa combinação que o build pode não ter. Liste as keywords no log e garanta a combinação
   no ShaderKeep.
7. O `Player.log` só é escrito pelo executável; erros de shader às vezes **não** aparecem nele —
   o print é a prova.

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

- Pode ir para o GitHub: CC0, CC BY (com crédito no `THIRD_PARTY.md`), modelos gerados pela
  conta do Felipe no Meshy.
- Só no PC: Asset Store, Fab, qualquer EULA que proíba redistribuir.
- Não usar: origem não comprovada (reuploads, ripados de jogos, pacotes pagos republicados).
  O pacote "Realistic Animated Pack" está em `C:\Users\Felip\TDFende-quarentena` por isso.
- Todo asset novo: registrar autor, link e licença no `THIRD_PARTY.md` no mesmo commit.

## 8. Direção de arte

**Realista e de última geração** (como um jogo atual da Steam), **não** low-poly, **não**
cartoon. Não precisa ser medieval. Prefira arte real (Poly Haven, comunidade Meshy CC0, Meshy da
conta do Felipe) a formas procedurais. Todo ganho visual tem que caber no orçamento de FPS
(medir no `-captura`).

## 9. Como o Felipe trabalha

- Respostas em português, resultado primeiro, curtas. Fechar com `DONE`,
  `DONE_WITH_CONCERNS`, `NEEDS_CONTEXT` ou `BLOCKED`.
- Teste antes da correção, visto falhar pelo motivo certo (regra global dele).
- Uma coisa de cada vez, até o fim; fechou, commit + push.
- Três tentativas de correção falhando: parar, instrumentar cada fronteira (o `-captura` é o
  instrumento) em vez de chutar.
- Ele testa pelo **executável**, não pelo editor.
