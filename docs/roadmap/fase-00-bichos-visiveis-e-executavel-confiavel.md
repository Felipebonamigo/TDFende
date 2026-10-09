# Fase 0 — Bichos visíveis e executável confiável

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 5-6 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

O Felipe abre o TDFende.exe gerado da main e vê os 9 bichos com o FPS de volta. Um -captura com métricas e cenário de estresse reprova qualquer regressão. Infraestrutura de sessões, backup e repositório resolvida antes do primeiro asset novo.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Exe gerado da main com hash limpo passa no -captura com código 0, e os 9 bichos aparecem no close.
- Zero linhas 'Default clip' e zero exceções.
- p95 de volta ao patamar dos 104 fps, e -estresse medido.
- Pre-commit e Actions verdes; ROADMAP, regra de LFS e backup testado no MANUAL.
- Velocidade recalibrada.
- O Felipe recebe o print no celular.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [x] Manter ou reverter cada configuração regravada pelo Unity: **manter todas** (Felipe, 09/10/2026; detalhes no TEC-01).
- [x] Repositório: **público + arte pesada no PC**, sem LFS (Felipe, 09/10/2026). Licença do código: em aberto (decidir antes da página da Steam).
- [x] Critério de 'architectural' aprovado como proposto e escrito no CLAUDE.md (Felipe, 09/10/2026).
- [x] Sincronizar.ps1 **aposentado** (Felipe, 09/10/2026).
- [x] Backup no **disco D:** (Felipe, 09/10/2026).

## Tarefas, em ordem

- [x] BUG-01 — Bichos invisíveis no executável (primeiro item)
- [x] TEC-04 — Captura v2 (núcleo) com código de saída e estresse
- [x] BUG-02 — Clipe padrão dos bichos
- [x] VIS-11a — Grama: rarear ou desligar por flag até o chão novo
- [x] TEC-11 — Shaders sem surpresa no build (escopo dado pela causa do BUG-01)
- [x] TEC-01 — Base limpa: o executável sai da main
- [x] TEC-34 — Repositório: LFS, histórico e público × privado
- [x] TEC-20 — Sessões paralelas seguras (mínimo)
- [x] TEC-30 — FlowSim no GitHub Actions
- [x] TEC-03 — Selo de build no print e no log
- [x] TEC-06 — Orçamento de desempenho escrito (modo aviso)
- [x] TEC-26 — Backup do que não está no Git
- [x] TEC-35 — Tetos e políticas no MANUAL
- [x] PROC-01 — Caixa de aprovações e acompanhamento

---

### BUG-01 — Bichos invisíveis no executável (primeiro item)

- [x] **Status:** feito em 09/10/2026 (commit com "BUG-01" na mensagem: `git log --grep=BUG-01`).
  - **Causa:** não era shader nem configuração do URP. Em rato, cachorro, lobo e águia a malha
    animada (esqueleto × malha do modelo baixado) fica quase um ponto, enquanto a caixa guardada
    no `SkinnedMeshRenderer` diz o tamanho certo. O `AnimalLoader` media essa caixa, então não
    ampliava o bicho, e o recorte por caixa velha sumia com ele. O teste com material simples
    (`_bicho_<Nome>_simples.png`) sumiu igual, o que descartou material/variante.
  - **Correção:** o `AnimalLoader` toca a pose (Walk/Idle), mede a malha desenhada com `BakeMesh`
    (`RealBounds`), escala por essa medida e reajusta a caixa de recorte (`FitCullingBounds`).
    Vale para qualquer modelo novo, então não mexi no conversor.
  - **Verificado:** executável novo, `-captura` com retrato de cada bicho: os 9 aparecem
    (Águia, Cachorro, Elefante, Javali, Lobo, Rato, Rinoceronte, Tigre, Urso), todos
    "(animado)", FPS médio 96, nenhuma exceção.
  - **Pendente (vai para o TEC-04):** o assert automático de cobertura de pixels. Hoje a prova
    é o retrato no print, não um código de saída.
- **Categoria:** BUG · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2,5 · prioridade 5
- **Depende de:** TEC-01

**Por que, e nesta fase:**

Sem bicho na tela o Tower Wars não existe. O log diz que os 9 carregam e animam, então o defeito é de render, bounds ou culling. Dois suspeitos baratos estão no repositório: (a) o diff não commitado do URP_Asset liga prefiltering e remoção de variantes, enquanto o AnimalLoader liga _EMISSION (l. 232) e _ALPHATEST_ON (l. 263) em runtime; (b) cullingType BasedOnRenderers (l. 150) com updateWhenOffscreen = false (l. 227) e bounds errados cortam o mesh enquanto o anel continua visível.

**O que é:**

No executável, os 9 bichos aparecem só como o anel do time. Plano:
- Medir escala e bounds em 3 momentos: depois do TrySpawn, depois do primeiro anim.Sample e 2 s depois.
- Fazer bisseção com chaves do -captura: -bicho-unlit, -bicho-caixa e build com strictShaderVariantMatching.

Hipótese principal: o converte.py exporta o Armature com escala 100, e o AnimalLoader mede antes do primeiro sample.
Concorrentes: falta a variante _ALPHATEST_ON+_EMISSION no ShaderKeep; culling por bounds velhos.

Correção na origem: assar a escala no conversor. Fica um assert permanente que mede a cobertura de pixels e a altura de cada bicho.

**Valor para o jogador:**

Sem isso não há inimigo na tela, e o mercado de envios inteiro deixa de existir para o jogador.

**Pronto quando:**

Bisseção, do mais barato ao mais caro:
1. Rodar a cena no Editor.
2. Build com os 6 arquivos regravados revertidos (git stash) contra build com eles.
3. Flag -bicho-sem-corte (updateWhenOffscreen = true e AlwaysAnimate).
4. Só então -bicho-unlit, -bicho-caixa e strictShaderVariantMatching.

Corrigir na origem. Pronto quando o assert de cobertura reprova o exe antigo e passa o novo nos 9 bichos, com o close de cada um. Três correções falhando: parar e rever o AnimalLoader inteiro, inclusive trocar Legacy por Animator.

**Como verificar:**

O assert de cobertura reprova os 9 bichos no build atual e passa depois da correção, com o close de cada bicho no print.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 5, custo 3, risco 3) Sem bicho visível o Tower Wars não existe para o jogador: é o bug nº 1, acima de qualquer arte nova. A bisseção com -bicho-unlit e -bicho-caixa é o caminho certo, e a hipótese da escala 100 do Armature é plausível. Não deve esperar o TEC-01 inteiro: basta o executável refeito a partir da main atual. O assert de cobertura de pixels é o que impede a volta do bug. A causa ainda não foi confirmada, então o custo pode subir.
- (agora; valor 5, custo 3, risco 2) Bloqueante: sem bicho na tela o Tower Wars não existe. O Player.log das 13:20 confirma que os 9 carregam e animam, e mede altura de 0,39 a 1,96. Então a malha existe e o problema é de render, skin ou bounds. A escala 100 do Armature é plausível, mas não está provada. Uma hipótese concorrente que o plano não cita: o build regravou o prefiltering de variantes do URP_Asset (diff não commitado), e isso pode cortar variantes no executável. Primeiro passo mais barato: rodar a mesma cena no Editor. O log de 03/10 nunca rodou estes bichos no Editor, então ainda não se sabe se o bug é só do executável. Se forem invisíveis lá também, a pista do stripping cai. M é realista se cada rodada da bisseção custar um build inteiro. Não precisa esperar o TEC-01 inteiro, basta refazer o executável a partir da main atual.

</details>

---

### TEC-04 — Captura v2 (núcleo) com código de saída e estresse

- [x] **Status:** núcleo feito em 09/10/2026 (commit com "TEC-04" na mensagem: `git log --grep=TEC-04`).
  - **Feito:** `Tools/Captura.ps1` (saída 0 passou, 1 reprovou, 2 travou); assert de cobertura
    de pixels de cada bicho (corpo sozinho na camada 31, mínimo 0,5%); tipo de bicho que não
    apareceu; shader não suportado ou material nulo; exceção no log; tempo esgotado (60 s);
    `print_metricas.json` com boot, quadro p50/p95/p99, triângulos/draws/SetPass (ProfilerRecorder,
    funciona no exe de release), exceções, erros e cobertura de cada bicho; `-estresse`.
  - **Visto falhar:** com o `AnimalLoader` de antes do BUG-01 sai com 1 (Rato, Águia e Cachorro
    com 0,00% de cobertura; o Lobo nem chegou ao retrato). Com o atual sai com 0.
  - **Medido (1600×900):** normal p95 7,0 ms, ~25 milhões de triângulos, 1.012 draws; estresse
    (60 bichos, 12 torres no nível 6) p95 41,7 ms, p99 48,6 ms, ~27-30 milhões de triângulos,
    ~2.100 draws, ~650 SetPass. Insumo para o TEC-06 (orçamento) e o VIS-11a (grama).
  - **Fica para depois:** lint do Player.log (depende do BUG-02), cor média do chão, altura em
    pixels, folha de contato, flags -menu/-fim/-captura-ui (entram com as telas).
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2 · prioridade 4,5
- **Depende de:** TEC-01

**Por que, e nesta fase:**

É a única verificação visual automática que existe; hoje ela não reprova nada, e foi assim que o bicho invisível passou. Um bicho de cada mede uma cena quase vazia, enquanto o fim de partida real tem dezenas de skinned em CPU.

**O que é:**

Roteiro fixo com semente: menu, início, meio, close de cada bicho, torres no nível 1 e no 6, fim de partida.

métricas.json com:
- p50/p95/p99 do tempo de quadro depois do aquecimento;
- triângulos, draws, SetPass e memória (ProfilerRecorder);
- tempo de boot;
- cor média do chão e altura em pixels de cada bicho e torre.

Asserts com código de saída: bicho visível, shader suportado, nenhuma exceção, lint do Player.log, orçamento.

Folha de contato por build e flags -captura-ui, -menu, -fim e -qualidade.

**Valor para o jogador:**

Cada build chega ao celular do Felipe com prova visual e de desempenho, sem regressão boba.

**Pronto quando:**

- Primeiro, só o assert de cobertura em pixels do close e o código de saída, que sustentam o BUG-01.
- Depois: p95 após aquecimento, triângulos e draws (ProfilerRecorder), boot, lint do Player.log e nenhuma exceção.
- Modo -captura -estresse: 60-80 bichos, escalada de fim de partida e 6 torres por lane disparando, com p95 no métricas.json.
- No exe atual sai com 1 e, depois dos consertos, com 0.

As flags de menu e de fim entram junto com as telas.

**Como verificar:**

No build atual tem que sair com código 1 (bicho invisível, triângulos da grama), e com 0 depois dos consertos.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 3, risco 2) O núcleo é ótimo: um assert de bicho visível, nenhuma exceção e um tempo de quadro com código de saída. Isso teria pego o bug dos bichos invisíveis. O resto do pacote (p99, SetPass, memória, cor média do chão, folha de contato, 4 flags novas) tende a virar projeto próprio. Fazer a versão enxuta junto do BUG-01 e crescer só quando uma métrica faltar de fato.
- (agora; valor 4, custo 3, risco 2) É a rede de segurança da reforma visual que vem aí, e o assert 'bicho visível' é a verificação do BUG-01. O escopo descrito vale mais que M, porque ProfilerRecorder, cobertura de pixels, folha de contato e 4 flags somam vários builds. Os asserts de cor e de altura em pixel tendem a ficar instáveis com câmera, SSAO e animação. Fazer agora só o núcleo: código de saída, bicho visível, exceção, p95 e triângulos. As flags -menu, -fim e -captura-ui dependem de telas que ainda não existem e entram junto com elas. Começar os orçamentos como aviso, não como reprovação.

</details>

---

### BUG-02 — Clipe padrão dos bichos

- [x] **Status:** feito em 09/10/2026 (commit com "BUG-02" na mensagem: `git log --grep=BUG-02`).
  - `AnimalLoader.AddClips` reatribui `anim.clip` (Idle, senão Walk) depois de trocar os clipes.
  - A captura agora reprova com qualquer aviso da lista `SmokeCapture.LogLint` (começa com
    este). Visto falhar antes da correção (6 avisos, saída 1); depois, 0 avisos e saída 0, normal
    e estresse.
- **Categoria:** BUG · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 1 · risco 1 · prioridade 5

**Por que, e nesta fase:**

São 74 avisos 'Default clip could not be found' no mesmo arquivo do BUG-01, sujando o log que o lint lê.

**O que é:**

O AnimalLoader.AddClips não volta a atribuir anim.clip depois do RemoveClip. O resultado são ~75 avisos 'Default clip could not be found' no Player.log.

**Valor para o jogador:**

Elimina uma causa de bicho parado ou sem animação, e limpa o log que os outros testes leem.

**Pronto quando:**

anim.clip volta a ser atribuído no AnimalLoader.AddClips, e o Player.log do -captura fica sem nenhum aviso desses.

**Como verificar:**

O Player.log do -captura fica sem nenhuma linha 'Default clip could not be found'.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 3, custo 1, risco 1) Conferido: o AddClips faz RemoveClip e nunca volta a atribuir anim.clip. É barato e pode estar ligado ao bicho parado ou invisível. Entra junto da bisseção do BUG-01, e o log limpo ajuda os asserts de captura. Sozinho, o jogador quase não percebe.
- (agora; valor 2, custo 1, risco 1) Confirmado: o Player.log das 13:20 tem 74 linhas 'Default clip could not be found', e o AddClips faz RemoveClip sem reatribuir o anim.clip. A correção é uma linha no mesmo arquivo que o BUG-01 vai mexer, então entra junto. Para o jogador o ganho é pequeno, porque o log mostra os bichos animando mesmo assim. O valor real é o log limpo, sem o qual o lint do TEC-04 não funciona.

</details>

---

### VIS-11a — Grama: rarear ou desligar por flag até o chão novo

- [x] **Status:** feito em 09/10/2026 (commit com "VIS-11a" na mensagem: `git log --grep=VIS-11a`).
  - Densidade já estava de volta (orçamento de 4 M triângulos, a mudança de 06/10 foi revertida)
    e a grama já ficava fora das lanes. Novo: material da grama sem a passada `DepthNormals`
    (comentário de atalho com teto e saída no `GrassField.MakeFoliageMaterial`) e a flag
    `-sem-grama`.
  - **Medido (1600×900, -captura):** triângulos por quadro 24 M → 14,3 M (sem grama: 7,2 M).
    p95 normal 7,0 ms antes e depois (acima dos 104 fps pedidos). No estresse o p95 fica em
    41,7 ms **com ou sem grama**: o gargalo do fim de partida é outro (fica para o TEC-06).
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

São 4,0 M triângulos, desenhados também no DepthNormals do SSAO: o FPS foi de 104 para 54. O visual certo da grama vem com o chão novo (VIS-11, Fase 4). Pela escada, o degrau certo agora é 'isso precisa existir?'.

**Pronto quando:**

- Densidade de volta ao patamar anterior ou desligada por flag.
- Grama fora da lane e fora do DepthNormals.
- p95 de volta ao patamar dos 104 fps no -captura.
- Comentário com o teto e a saída ('atalho: grama rala até a VIS-11').

---

### TEC-11 — Shaders sem surpresa no build (escopo dado pela causa do BUG-01)

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-11" na mensagem: `git log --grep=TEC-11`).
  A causa do BUG-01 não foi shader, então ficou o escopo menor (diagnóstico + ShaderRefs), mais
  uma conferência automática na captura:
  - **Build de diagnóstico** com `strictShaderVariantMatching` (`BuildJogo.Diagnostico`,
    `GerarExecutavel.ps1 -Diagnostico`, `Captura.ps1 -Diagnostico`); a opção liga só durante o
    build e volta ao que era no ProjectSettings. A captura lê o Player.log e reprova com
    `: variant ... not found`.
  - **Censo de variantes na captura**: todo material criado em runtime em uso tem que ter a mesma
    combinação shader + keywords de um material do ShaderKeep.
  - **ShaderRefs** no lugar dos 10 `Shader.Find` do Runtime; `BuildJogo.Shaders` usa os nomes de
    lá; nenhum `TODO(build)`.
  - **Visto falhar:** o censo reprovou `Lit [_ALPHATEST_ON _EMISSION _NORMALMAP]` (pelo do rato)
    e `Lit [_ALPHATEST_ON _EMISSION]` (lobo e águia), que faltavam no ShaderKeep (o Unity trocava
    em silêncio pela variante mais parecida). O build estrito acusou ~74 mil erros: todos com
    `FOG_LINEAR` — o exe nunca teve a névoa do `SceneAmbience`, porque o corte automático de
    névoa só guarda o que alguma cena do build usa. No estrito o urso sumia (0% de cobertura).
  - **Correção:** as duas combinações em `BuildJogo.Shaders` e névoa linear ligada na
    `Jogo.unity` pelo `EnsureScene`. Depois: estrito com 0 variantes faltando e saída 0; normal e
    estresse com saída 0. Retirar uma combinação reprova (era o estado de antes).
  - Materiais-modelo clonados em vez de `EnableKeyword` solto: não feito (só valia se a causa do
    BUG-01 fosse shader; o censo já pega combinação nova).
  - Para a decisão do Felipe sobre as configurações regravadas pelo Unity: com as atuais
    (prefiltering do URP_Asset regravado) o build estrito não acusa variante faltando.
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2 · prioridade 4,5
- **Depende de:** TEC-01

**Por que, e nesta fase:**

Terreno preto e bicho sumido são a mesma classe de bug, e a troca de arte vai multiplicar os materiais.

**O que é:**

Materiais-modelo em Resources clonados em runtime (nada de EnableKeyword solto). Um ShaderRefs substitui os 10 Shader.Find. As variantes são coletadas automaticamente rodando as funções de keyword no Editor, e há um build de diagnóstico com strictShaderVariantMatching.

**Valor para o jogador:**

Acaba a classe de bug 'só no executável': terreno preto, bicho sumido, efeito sem cor.

**Pronto quando:**

- Build de diagnóstico com strictShaderVariantMatching acusando variante faltando.
- ShaderRefs no lugar dos Shader.Find.
- Materiais-modelo clonados em vez de EnableKeyword solto.
- Remover uma combinação de propósito reprova o diagnóstico.
- Nenhum 'TODO(build)'.

Se a causa do BUG-01 não for shader, fica só o diagnóstico e o ShaderRefs.

**Como verificar:**

Remover uma combinação de propósito reprova o build de diagnóstico, e a coleta automática passa.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Ataca exatamente a classe de bug que deixa o jogo invendável: bicho invisível, terreno preto e efeito sem cor só no exe. São 16 Shader.Find espalhados em 9 arquivos, um convite a stripping no build. O bicho invisível precisa ser diagnosticado primeiro (BUG); se a causa for shader, a parte ShaderRefs + materiais-modelo vira 'agora'. A coleta automática de variantes e o build strict podem vir depois, mas devem estar prontos antes da leva grande de materiais realistas.
- (agora; valor 4, custo 3, risco 2) Esse tipo de bug já apareceu duas vezes (chão preto e agora bicho invisível). Parte da ideia já existe: Resources/TDFende/ShaderKeep tem 8 materiais de Lit mantidos à mão. Só que o AnimalLoader liga _EMISSION em todo material e _ALPHATEST_ON nos cartões de pelo, e ShaderKeep não tem _ALPHATEST_ON+_EMISSION nem _ALPHATEST_ON+_NORMALMAP+_EMISSION. Isso é suspeito forte do bug (1), mas ainda não está confirmado. Outro suspeito é updateWhenOffscreen=false com bounds errados (AnimalLoader.cs:227). Ordem: primeiro o build de diagnóstico com strictShaderVariantMatching, que confirma ou descarta a hipótese em uma sessão. A coleta automática e o ShaderRefs vêm em seguida. Isso precisa ficar pronto antes da troca de arte, que vai multiplicar os materiais. Depende do build voltar a ficar verde (TEC-01).

</details>

---

### TEC-01 — Base limpa: o executável sai da main

- [x] **Status:** feito em 09/10/2026 (commits com "TEC-01" na mensagem: `git log --grep=TEC-01`).
  O Felipe confirmou commitar os 7 arquivos regravados (09/10/2026).
  - Feito: os 8 `.meta` soltos entraram no commit do BUG-01; os 2 GLBs originais do Meshy (270 MB)
    saíram de `Resources/TDFende/Bichos/Gerados` para `Tools/ConverterBichos/glb/originais-meshy/`
    (fora do git; nunca entraram no build, eram só importação lenta); `ROADMAP.md` existe.
  - **Idempotência medida:** dois builds seguidos não mudam nada no git além dos 6 arquivos que o
    Unity/URP regrava + `ProjectSettings/SceneTemplateSettings.json`. Todos os executáveis testados
    desde 06/10 foram gerados **com** essas versões regravadas.
  - **O que cada um é** (para a decisão): `GraphicsSettings` m_LightsUseLinearIntensity 0→1 (o
    URP liga isso sozinho ao iniciar em espaço linear, então o valor salvo não muda o jogo);
    `URP_Asset` prefiltering (recalculado pelo URP a cada build a partir das opções do asset; o
    build estrito do TEC-11 não acusa variante faltando com ele); `URP_Renderer` com o SSAO (posto
    pelo `Editor/SsaoSetup.cs`, de propósito, desde 92325c5); `DefaultVolumeProfile` com os
    efeitos do URP 6 em valor neutro (o Unity preenche sozinho); `UniversalRenderPipelineGlobalSettings`
    (registro interno); `ProjectSettings` com static batching do Standalone (padrão do Unity).
    Reverter não adianta: o Unity regrava no próximo build. Commitados com o ok do Felipe.
  - Fica: o `Torre_Canhao.fbx` antigo (sem estágio, 1,2 MB no build) é o modelo do modo clássico
    (`GameController`, estágio 0). Tirar troca o visual do clássico, que a captura não testa;
    fica para quando o clássico for revisto.
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2,5 · risco 2,5 · prioridade 5

**Por que, e nesta fase:**

O exe atual é anterior à correção do SmokeCapture (1800cbd). São 6 configurações regravadas pelo Unity sem commit, uma delas m_LightsUseLinearIntensity, que muda a luz de tudo, e há 8 .meta soltos. Enquanto o exe não sair da main, todo print é suspeito.

**O que é:**

Fechar as pendências antes de qualquer feature:
- Commitar os 8 .meta soltos.
- Fazer A/B por print de cada configuração regravada (m_LightsUseLinearIntensity, SSAO, prefiltering, VolumeProfile), com decisão do Felipe para cada uma.
- Tornar o build idempotente.
- Tirar de Resources os GLBs de 271 MB e o Torre_Canhao antigo.
- Provar que um build a partir de um worktree limpo dá o mesmo resultado.
- Criar o ROADMAP.md.

**Valor para o jogador:**

O Felipe joga exatamente o que está na main, e acaba o 'corrigido no commit, quebrado no build'.

**Pronto quando:**

- 8 .meta commitados no começo da fase.
- Cada configuração mantida ou revertida pelo A/B de prints que sai da própria bisseção do BUG-01, com decisão do Felipe.
- GLBs de 271 MB e Torre_Canhao antigo fora de Resources.
- Dois builds seguidos deixam o git status vazio.
- ROADMAP.md criado.

A prova por worktree limpo fica para depois.

**Como verificar:**

Dois builds seguidos deixam o git status vazio, e o build do worktree limpo gera o mesmo Player.log de captura.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 2, risco 2) Os 6 assets de URP/Graphics regravados e não commitados são exatamente o que define o visual. Enquanto o executável não sair da main, todo print e todo 'corrigido' é suspeito, e isso já aconteceu (de71e7c, 1800cbd). Tirar os 271 MB de GLB de Resources também reduz o build. Dá para cortar escopo: o A/B por print só nas 2 ou 3 configurações que mudam o visual, não em todas. O ROADMAP.md é opcional.
- (agora; valor 4, custo 3, risco 3) Certo no mérito, mas 'P' é otimista. Quatro pontos que pesam:
- Os 6 assets modificados foram regravados pelo Unity/URP. O m_LightsUseLinearIntensity passou de 0 para 1, o que muda a intensidade de toda a iluminação. O prefiltering de variantes também mudou, e isso é suspeito no BUG-01. Cada um é decisão visual com A/B.
- O SmokeCapture já está limpo na main (1800cbd), então a pendência real é o executável velho.
- O 'worktree limpo' não reproduz o jogo: os .meta dos bichos, a pasta Gerados e as fontes do Meshy estão no .gitignore. Reimportar 459 MB de Resources leva horas.
- Os 2 GLBs (271 MB) usam DefaultImporter e são locais. Tirá-los é barato e só afeta o build se o Unity os empacotar.
Cortar para: commitar os .meta, decidir os 6 settings, refazer o executável pela main. A prova do worktree limpo fica para depois.

</details>

---

### TEC-34 — Repositório: LFS, histórico e público × privado

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-34" na mensagem). Decisão do Felipe:
  público até a página da Steam, arte pesada no PC, sem LFS. Regra no MANUAL (seção 2, item 9)
  e no CLAUDE.md; o pre-commit bloqueia arquivo acima de 10 MB (testado com um de 12 MB: saída
  1). O maior arquivo do repositório hoje tem 6,8 MB e nenhum blob do histórico passa de 10 MB,
  então não há o que reescrever. O `.gitignore` já cobria `Tools/*/glb/` e `Builds/`.
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O .git já tem 191 MB, o repositório é público, e qualquer binário commitado antes da decisão fica para sempre no histórico. A camada privada, a licença do código e quem consegue montar o jogo de graça dependem da mesma decisão.

**Pronto quando:**

Regra escrita no MANUAL antes do primeiro asset da Fase 1, a partir das opções:
- Git LFS;
- arte pesada só no PC, com backup;
- reescrita do histórico;
- repositório privado antes da página da Steam.

O .gitignore é ajustado conforme a decisão.

---

### TEC-20 — Sessões paralelas seguras (mínimo)

- [x] **Status:** mínimo feito em 09/10/2026 (commit com "TEC-20" na mensagem).
  - Trava `Builds\.trava` no `GerarExecutavel.ps1` (testado: com a trava sai com 3 e não
    builda; sem ela builda e apaga a trava) e recusa rodar com o Unity aberto.
  - `Sincronizar.ps1` e `InstalarSincronizacao.ps1` aposentados (não estavam agendados);
    `ASSETS.md` atualizado.
  - MANUAL, seção 2, item 10: um Unity por vez, worktree para Sim/Python, reserva de tarefa
    no `ROADMAP.md`. Critério de "architectural" no CLAUDE.md.
  - Não feito: a rodada de prova com duas sessões paralelas (precisa de duas sessões abertas)
    e o build noturno com prints.
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3 · risco 2,5 · prioridade 4,5
- **Depende de:** TEC-01, TEC-04

**Por que, e nesta fase:**

Já houve commit incompleto (de71e7c) e HEAD mudando no meio da leitura. O Tools/Sincronizar.ps1, recomendado no ASSETS.md, faz pull --rebase e commit automático a cada 2 min na mesma árvore do Unity. É incompatível com a trava e é candidato a explicar o HEAD mudando.

**O que é:**

- Um clone por sessão, com push direto na main.
- ROADMAP.md como quadro de reservas.
- Trilhas de Sim e Python em worktree; uma única trilha Unity com trava em Builds/.trava.
- Build noturno com relatório de prints para o celular.
- Critério de 'architectural' escrito no CLAUDE.md.

**Valor para o jogador:**

Mais coisas prontas por semana sem trabalho perdido.

**Pronto quando:**

- Trava em Builds/.trava impede dois builds ou dois Unity ao mesmo tempo.
- Trilhas de Sim e Python em worktree próprio.
- Reservas de tarefa no ROADMAP.md.
- Sincronizar.ps1 respeita a trava e não roda com o Unity aberto, ou é aposentado; ASSETS.md atualizado; schtasks conferido.
- Uma rodada com duas sessões paralelas termina com git show --stat íntegro nas duas.

**Como verificar:**

Duas sessões em clones paralelos com commits íntegros (git show --stat), e três noites de relatório.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 3, risco 2) O problema é real: uma edição quebrada não commitada de outra sessão travou a compilação, e o HEAD mudou durante a leitura. Isso já custou trabalho e vai custar mais com o Opus em paralelo. Fazer só o mínimo: um clone por sessão (push direto na main, como manda o CLAUDE.md) e a trava única da trilha Unity. O quadro de reservas e o relatório noturno para o celular são cerimônia e podem esperar.
- (agora; valor 3, custo 3, risco 3) Já houve perda real de trabalho: o commit de71e7c descrevia correções que só estavam na árvore de trabalho, o HEAD e o CLAUDE.md mudaram durante a leitura, e há assets do URP modificados sem commit. Usar muitas sessões do Opus 5.5 ao mesmo tempo sem isolamento multiplica esse dano. Fazer agora só o mínimo: worktree ou clone para as trilhas de Sim e Python (FlowSim e CompileCheck não precisam de Library), uma única trilha Unity com trava em Builds/.trava, e reservas no ROADMAP.md. O build noturno com prints fica para depois. Os riscos estão subestimados: cada clone com Unity precisa importar a Library de novo (GBs e dezenas de minutos), o editor em batch reescreve os .asset do URP e isso vai gerar conflito no push direto na main, e o Sincronizar.ps1 pode brigar com os clones. O critério de 'architectural' no CLAUDE.md quem decide é o Felipe.

</details>

---

### TEC-30 — FlowSim no GitHub Actions

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-30" na mensagem).
  `.github/workflows/flowsim.yml` roda o FlowSim (net10.0, ubuntu) a cada push na `main`.
  Provado no GitHub: o push de ca6468e passou (run 37954502327) e o disparo manual com
  `falha_de_proposito` ficou vermelho (run 37954500082). O CompileCheck continua só no
  pre-commit local (precisa das DLLs do Unity).
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O pre-commit depende do core.hooksPath local. Sessão na nuvem, clone novo ou --no-verify passam sem teste.

**Pronto quando:**

Workflow que roda o FlowSim (net10.0) a cada push na main e fica vermelho com um teste quebrado de propósito. O CompileCheck, que precisa das DLLs do Unity, continua local.

---

### TEC-03 — Selo de build no print e no log

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-03" na mensagem).
  - `BuildJogo` põe `git describe --always --dirty` na versão do executável só durante o build
    (o ProjectSettings volta ao que era); `BuildStamp` mostra "build <hash>" no canto inferior
    direito (menu e partida) e grava `[TDFende] build <hash>` no Player.log; o
    `print_metricas.json` tem o campo `build`.
  - A captura reprova executável sem selo (versão "1.0"): visto falhar antes de ligar o
    carimbo, passa depois.
  - Não feito: a linha `version` no replay (opcional; builds antigos recusariam replays novos).
  - De quebra: a cobertura de cada bicho passou a ser medida no quadro em que ele aparece
    (antes esperava a fila de retratos, e o cachorro morria antes da vez dele: "só 8 de 9").
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Acaba a dúvida sobre qual executável o Felipe está vendo.

**O que é:**

Versão gerada por git describe no bundleVersion. O hash e o estado sujo/limpo da árvore são gravados em Resources e aparecem no menu, no Player.log, no canto do print e numa linha opcional 'version' do replay.

**Valor para o jogador:**

Todo print e todo bug report diz de que build veio, e replays e desafios ficam comparáveis.

**Pronto quando:**

git describe --always --dirty vira o bundleVersion. O hash e o estado limpo ou sujo aparecem no Player.log, no canto do print e no menu.

**Como verificar:**

O Player.log e o print mostram o hash, e um replay com e sem a linha é lido.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) Barato. Com várias sessões no mesmo repositório e o histórico de 'executável velho', o hash no canto do print acaba com a dúvida de qual build o Felipe está vendo. Para o jogador final é invisível. A linha 'version' no replay pode esperar.
- (logo; valor 2, custo 1, risco 1) Barato e ataca direto o problema real de hoje: um executável anterior à correção, com várias sessões no mesmo repositório. O repositório não tem nenhuma tag, então o comando precisa ser git describe --always --dirty. A linha 'version' no replay precisa de um case próprio no TryParse. Hoje uma linha desconhecida cai em 'comando não reconhecido', e builds antigos vão recusar replays novos. É aceitável, mas tem que ser intencional. Pode entrar junto com o TEC-01.

</details>

---

### TEC-06 — Orçamento de desempenho escrito (modo aviso)

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-06" na mensagem). Tabela no MANUAL
  (seção 4, "Orçamento de desempenho"), medida no `-Estresse`; veredito por item em
  `orcamento` no `print_metricas.json`, só como aviso (constantes `Budget*` no `SmokeCapture`).
  Como o cartão previa, o build atual fica acima na grama (4,0 M), nas torres (43 k a 95,6 k),
  na fortaleza (92,6 k), em 3 bichos e no p95 do estresse (41,7 ms) — o que volta ao teto com
  a VIS-11 (grama nova) e a TEC-10 (LOD). Regra de antes/depois no MANUAL.
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 2 · risco 2 · prioridade 4
- **Depende de:** TEC-04

**Por que, e nesta fase:**

Sem números, 'realista' vira 40 fps. O orçamento também filtra que arte dá para baixar.

**O que é:**

Números escritos e reprovação automática.

Cena (RTX 4070 Ti, 1080p, Alto): p95 ≤ ~7-8 ms; 1,5-2,5 M triângulos; teto de draws, SetPass e VRAM; boot ≤ 3-5 s.

Por asset: torre ≤ 15-25k, fortaleza ≤ 40k, bicho ≤ 12-15k, grama ≤ 400k.

Nível Baixo/mobile: ≤ 300k triângulos.

Regra: nenhuma mudança visual sem a linha de desempenho de antes e de depois.

**Valor para o jogador:**

Jogo liso no Steam Deck e em PC comum, e cada ganho visual chega com o preço conhecido.

**Pronto quando:**

O MANUAL tem a tabela calibrada sobre o -captura -estresse, não sobre um bicho de cada:
- cena: p95, triângulos, draws, boot;
- por asset: torre, fortaleza, bicho, grama.

Veredito por item no métricas.json, só como aviso.

**Como verificar:**

O build atual reprova na grama e nas torres pesadas e volta a passar depois de VIS-11 e TEC-10.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 2, risco 2) Ter números escritos é o que impede o 'realista' de virar 40 fps. A grama já mostrou o problema: 4M tris e 104 fps virando 54. Mas no começo deve ser aviso, não reprovação: com a arte em fluxo, um teto rígido trava a experimentação. Os tetos por asset (torre ≤ 15-25k) precisam de LOD para não empobrecer o realismo de perto. A regra de antes/depois em toda mudança visual é o melhor da ideia.
- (logo; valor 4, custo 2, risco 2) Necessário antes de escolher a arte realista: o orçamento por asset filtra o que dá para baixar do Sketchfab ou do Meshy, e evita repetir as torres de 95k tris e a grama de 4M. O FPS medido caindo de 104 para 54 numa 4070 Ti justifica. Ressalvas: o build atual reprova de saída nas torres de 10,6k a 95k, e a 'TEC-10' (LOD/decimação) não está nesta lista. Por isso precisa começar em modo aviso, senão o código de saída fica sempre 1 e perde sentido. Os números de p95 e draws devem ser calibrados medindo, não chutados.

</details>

---

### TEC-26 — Backup do que não está no Git

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-26" na mensagem). `Tools/Backup.ps1`
  copia GLBs originais, quarentena e último executável para `D:\TDFende-backup` (disco físico
  diferente do C:), 801 MB na primeira cópia. Teste de restauração com SHA-256 registrado no
  MANUAL (seção 2, item 11). **Agendamento diário: aguardando o ok do Felipe** (tarefa agendada
  é configuração permanente do PC).
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Camada privada, GLBs originais do Meshy (javali e tigre inclusive), Mixamo, quarentena e builds entregues estão num único disco.

**Pronto quando:**

Script agendado copia essas pastas para a nuvem (OneDrive) ou para um disco externo. Um teste de restauração fica registrado no MANUAL.

---

### TEC-35 — Tetos e políticas no MANUAL

- [x] **Status:** feito em 09/10/2026 (commit com "TEC-35" na mensagem). MANUAL, seção 9:
  URP como teto (HDRP descartado), política de patch do 6000.3.x antes de build público (com a
  CVE-2025-59489 como motivo), Unity Personal até US$ 200 mil de receita + financiamento em 12
  meses (regra de 2026, splash opcional no Unity 6), critério de "architectural" (também no
  CLAUDE.md).
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Sem isso escrito, a próxima sessão que perseguir 'última geração' propõe HDRP, e o build público sai com um Unity vulnerável.

**Pronto quando:**

MANUAL com:
- 'URP é o teto; HDRP descartado por causa do mobile';
- política de patch: atualizar o 6000.3.x e recompilar antes de cada build público;
- obrigações do Unity Personal (teto de receita);
- critério de 'architectural' aprovado no CLAUDE.md.

---

### PROC-01 — Caixa de aprovações e acompanhamento

- [x] **Status:** feito em 09/10/2026 (commit com "PROC-01" na mensagem), com uma parte adiada.
  - Regra "o que não depende de aprovação segue" no `ROADMAP.md` (Como usar, item 8).
  - Tabela de acompanhamento com a Fase 0 preenchida e a recalibração formal (≈ 0,2 medido;
    0,5 provisório para as fases de arte/conteúdo; refazer no Portão 1).
  - Adiado: a página semanal de aprovações. Hoje não há lote nenhum esperando o Felipe (a única
    pendência é o ok para agendar o backup); a página nasce no primeiro lote real da Fase 1/2.
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

São mais de 30 pontos de 'o Felipe aprova'. Sem fila, as sessões paralelas param. E sem medição, a estimativa nunca vira velocidade.

**Pronto quando:**

- Página semanal (artifact) com prints, sons e lotes Meshy, cada um com sim ou não.
- Regra 'o que não depende de aprovação segue'.
- No ROADMAP.md, por fase: sessões reais × estimadas, custo de Opus e lição para o RTS.
- Recalibração formal no fim da Fase 0, aplicando a razão real/estimado ao resto.

---
