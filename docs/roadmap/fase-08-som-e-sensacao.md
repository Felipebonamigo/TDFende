# Fase 8 — Som e sensação

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 4-5 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

O jogo deixa de ser mudo. Cada ação tem resposta, e o som das torres é escolhido já conhecendo as torres.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Clipes tocados e cortados no log, todos no manifesto e sem Content ID.
- O Felipe joga de fone e aprova.
- p95 no orçamento.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Música e sons comprados (camada privada) ou só CC0/CC BY.
- [ ] Aprovar a mixagem ouvindo.

## Tarefas, em ordem

- [ ] BUG-03 — AudioListener garantido
- [ ] SOM-01 — Sistema de áudio
- [ ] SOM-02 — Banco de efeitos
- [ ] SOM-03 — Vozes dos bichos como aviso
- [ ] SOM-05 — Ambiente sonoro
- [ ] SOM-04 — Música segura para stream
- [ ] UX-08 — Alerta de bichos chegando
- [ ] UX-17 — Sensação de economia e combate

---

### BUG-03 — AudioListener garantido

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BUG · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Sem ele nenhum som toca.

**O que é:**

A câmera de Jogo.unity tem só Camera e Transform, e o código só cria o listener quando Camera.main é nulo. O BuildJogo.EnsureScene passa a garantir um AudioListener.

**Valor para o jogador:**

Pré-requisito para qualquer som tocar.

**Pronto quando:**

Criado em código quando falta; o -captura loga 'AudioListener: 1'.

**Como verificar:**

O -captura loga 'AudioListener: 1'.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) Conferido: o listener só é criado quando Camera.main é nulo, e a cena tem câmera. Sozinho não muda nada para o jogador, porque não há som. Entra no mesmo commit do primeiro som, que é onde vira valor de verdade.
- (logo; valor 2, custo 1, risco 1) Confirmado: TowerWarsController.cs:105 e GameController.cs:381 só criam o AudioListener quando o Camera.main é nulo, e a cena tem câmera. Sozinho não muda nada para o jogador, porque não existe som nenhum. Deve entrar no mesmo commit do primeiro som. Melhor degrau: garantir o listener em código (AddComponent se faltar) em vez de editar a cena no BuildJogo, porque assim também vale no Editor.

</details>

---

### SOM-01 — Sistema de áudio

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** SOM · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2 · prioridade 4
- **Depende de:** BUG-03, TEC-17

**Por que, e nesta fase:**

Som é metade da sensação.

**O que é:**

- SoundBank por chave, com OGG em Resources.
- Pool de ~24 AudioSources.
- Limite de vozes e intervalo mínimo por som; pitch ±6%.
- Pan pela tela e atenuação pela distância.
- 4 grupos de volume, com prioridade (vazamento nunca cortado).
- VoiceLimiter puro testado.

**Valor para o jogador:**

Som é metade do game feel, e hoje o jogo é mudo.

**Pronto quando:**

SoundBank por chave, pool de ~24 fontes, limite de vozes e pitch. 4 grupos com prioridade. VoiceLimiter puro testado.

**Como verificar:**

Teste do VoiceLimiter, e log com clipes tocados e cortados.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 2) Hoje o jogo é mudo, e na Steam isso o desclassifica. Dos itens desta lista, é o que entrega mais sensação de jogo pelo custo. Pool, VoiceLimiter puro e grupos de volume não dependem do tema, então dá para começar logo depois do bug dos bichos invisíveis. Os clipes em si (CC0 ou Sonniss) devem esperar a decisão de tema, porque um canhão medieval e um safári soam diferentes. Não ponho 'agora' porque sistema sem clipe bom não vale nada.
- (logo; valor 5, custo 3, risco 2) Melhor valor por custo do lote: o jogo é mudo e o sistema em si (pool, VoiceLimiter puro testável, 4 grupos) é M honesto. Os ganchos já existem: LaneSim expõe EnemyDespawned, TowerChanged, TowerFired e TowerSold, então TEC-17 é quase só costura. O custo escondido está no conteúdo: são 40 a 60 clipes CC0 com licença conferida, mixagem e música. Os sons das torres dependem do tema escolhido (canhão medieval ou artilharia moderna). Os dos bichos e da UI não dependem e podem começar já, em paralelo com BUG-01. O AudioListener só é criado quando falta câmera, então confirmar o BUG-03 antes.

</details>

---

### SOM-02 — Banco de efeitos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** SOM · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 2,5 · risco 1,5 · prioridade 4
- **Depende de:** SOM-01, TEC-22

**Por que, e nesta fase:**

Cada ação soa.

**O que é:**

Cerca de 35-60 clipes: tiro e impacto por torre, morte, vazamento, construir/evoluir/vender, compra, renda, UI, vitória/derrota e corneta da morte súbita. Primeiro o que não depende de tema. CC0 de Kenney e Freesound, em OGG normalizado.

**Valor para o jogador:**

Cada ação soa.

**Pronto quando:**

~20 clipes neutros de tema primeiro, depois tiro e impacto das torres finais. CC0 ou CC BY no manifesto, loudness medido por ffmpeg. O Felipe ouve e aprova.

**Como verificar:**

Playtest de fone sem cansaço, e todos os clipes no manifesto.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 2, risco 1) Jogo sem nenhum som parece protótipo em qualquer vídeo da Steam. Nada mais barato aumenta tanto a percepção de qualidade. Kenney e Freesound CC0 cobrem quase tudo, e UI, construir, comprar e renda não dependem de tema. Um cuidado concreto: dezenas de tiros por segundo pedem limite de vozes por tipo e variação de pitch, senão o playtest de fone vira cansaço. Fica em 'logo' e não em 'agora' só porque os bichos invisíveis e o SmokeCapture vêm antes.
- (logo; valor 4, custo 3, risco 2) Hoje o jogo não tem nenhum som, então o ganho de sensação é enorme, e o trabalho não toca a Sim. O custo real é curadoria, não código. O Claude não ouve áudio: com ffmpeg só mede loudness, então cada clipe precisa passar pela audição do Felipe. O Freesound exige login e mistura CC0 com CC BY, então é filtrar só CC0 ou registrar cada clipe no THIRD_PARTY. Os ganchos são pobres: TowerFired só passa a posição, sem o tipo da torre, e não há evento de acerto nem de nascimento (daí TEC-17/TEC-22). Começar com ~20 clipes neutros de tema (UI, compra, construir, vender, vitória/derrota) e limitar as vozes simultâneas, por causa das rajadas de Morteiro e de Ratos. 35-60 clipes como M é otimista.

</details>

---

### SOM-03 — Vozes dos bichos como aviso

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** SOM · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 1,5 · prioridade 4
- **Depende de:** SOM-01, TEC-17

**Por que, e nesta fase:**

Ouvir o elefante antes de vê-lo.

**O que é:**

Gravação real de cada bicho (barrido, uivo, rugido, grito da águia) tocada quando ele nasce na sua lane.

**Valor para o jogador:**

Ouvir o que vem antes de ver.

**Pronto quando:**

Uma voz por envio, com recarga por tipo. Não irrita na 20ª leva.

**Como verificar:**

Teste de olhos fechados: distinguir Elefante de Lobo pelo som.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 2, risco 2) É barato e combina com o realismo: o barrido do elefante chegando é um momento de verdade. O risco é a cacofonia, porque Ratos e Cachorros vêm em bando. Tem que ser uma voz por envio, com recarga, e não uma por unidade. Rinoceronte quase não vocaliza e precisa de bufo ou passada pesada. O teste de olhos fechados vale pouco. O que importa é o aviso não irritar na 20ª onda. Entra junto com o SOM-02.
- (logo; valor 3, custo 2, risco 1) Os bichos sobrevivem a qualquer troca de tema (safári inclusive), então não precisa esperar o VIS-01, e são só 9 clipes. Custos reais: gravação boa de elefante ou tigre costuma ser CC BY e entra nos créditos, que ainda não existem. O rato some na mixagem. Envio em massa precisa de cooldown por tipo, senão vira barulho. E a LaneSim hoje não tem evento de nascimento, que precisa ser criado.

</details>

---

### SOM-05 — Ambiente sonoro

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** SOM · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 1 · risco 1 · prioridade 4
- **Depende de:** SOM-01

**Por que, e nesta fase:**

Separa cenário realista de maquete silenciosa.

**O que é:**

Loops de vento, pássaros e insetos, fogueira posicionada nos acampamentos, e um fundo sonoro por bioma.

**Valor para o jogador:**

Mundo cheio e realista.

**Pronto quando:**

3-4 loops sem costura audível, no manifesto.

**Como verificar:**

Clipes no manifesto, e playtest.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 1, risco 1) Vento, pássaros e insetos custam quase nada e são o que separa 'cenário realista' de 'maquete silenciosa'. Ajudam a vender a direção de arte nova. A fogueira posicionada é detalhe bonito. O fundo por bioma espera existir bioma ou tema decidido. Os loops genéricos de natureza servem para qualquer tema.
- (logo; valor 3, custo 1, risco 1) Melhor custo-benefício de realismo do bloco: 3-4 loops CC0 e AudioSource 3D, sem tocar a Sim. O 'fundo por bioma' é especulativo, porque só existe um mapa, então corta. Fogueira só se o acampamento continuar no tema escolhido. Cuidados: costura audível no loop, e no mobile usar stream do disco.

</details>

---

### SOM-04 — Música segura para stream

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** SOM · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3,5 · risco 2,5 · prioridade 3
- **Depende de:** SOM-01, VIS-01

**Por que, e nesta fase:**

Drama na partida, sem reivindicação de Content ID em vídeos de streamers e no trailer.

**O que é:**

Trilha de menu e trilha de partida em camadas (calma, tensão, morte súbita), com crossfade guiado por um nível de perigo puro com histerese, extraído da IA. Stingers de vitória e derrota. A música espera o tema.

**Valor para o jogador:**

Drama que sobe sozinho quando aperta.

**Pronto quando:**

Faixas de menu, partida e morte súbita, com crossfade e stingers. Licença que cobre trailer e stream, sem registro no Content ID. No manifesto.

**Como verificar:**

Teste do DangerMeter, e log das transições.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 3, risco 3) Música é essencial, mas a versão adaptativa em camadas exige stems feitos para isso, e eles quase não existem em CC0. Stems ruins em crossfade soam pior que uma boa faixa linear. O degrau certo agora é mais simples: faixa de menu, faixa de partida e troca para a de morte súbita, que já é um gatilho determinístico, mais os stingers. O DangerMeter com histerese fica para quando houver stems e o tema estiver fechado.
- (depois; valor 3, custo 4, risco 2) O DangerMeter puro com histerese é barato e testável no FlowSim. O problema é a música: trilha CC0 em camadas sincronizadas praticamente não existe, então vai exigir compra (Asset Store, que fica fora do GitHub), encomenda ou música de IA com licença duvidosa. A estimativa M é otimista. Versão realista: 3 faixas inteiras com crossfade e 2 stingers. Espera o tema, como a própria ideia diz.

</details>

---

### UX-08 — Alerta de bichos chegando

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 1 · prioridade 4
- **Depende de:** TEC-17

**Por que, e nesta fase:**

Antecipar o elefante.

**O que é:**

Quando a IA envia, um banner sobre o acampamento mostra retrato ×N e a vida escalada. Envios grandes ganham destaque. Se a câmera estiver na outra lane, aparece um indicador clicável na borda. Agrupado por tipo.

**Valor para o jogador:**

Antecipar o elefante em vez de descobrir quando ele chega.

**Pronto quando:**

Banner com retrato ×N, agrupado por tipo, e indicador na borda. EnemySpawned certo (Rato = 4).

**Como verificar:**

EnemySpawned com tipo e quantidade certos (Rato = 4), e print do banner.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 1) Ver '×1 Elefante, 4.200 de vida' chegando cria a tensão que faz um Line TW viciar, e custa pouco: o evento já existe e só falta tipo, quantidade e remetente (TEC-17). Agrupar por tipo é essencial, senão os ratos viram spam. Enquanto não houver retrato (VIS-25), um ícone serve. O mesmo sistema de banner deve atender o UX-09, sem duas implementações.
- (logo; valor 4, custo 2, risco 1) Tem bom retorno pelo custo: antecipar o envio grande é decisão real de defesa. A simulação hoje só tem EnemyDespawned, TowerChanged, TowerFired e TowerSold, então depende de fato do TEC-17 (evento de spawn com tipo e quantidade). Tem também uma dependência escondida: o 'retrato ×N' pede o VIS-25. A primeira versão pode sair com nome e ícone provisório. O indicador clicável na borda depende do UX-10.

</details>

---

### UX-17 — Sensação de economia e combate

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 3 · risco 2 · prioridade 4
- **Depende de:** TEC-17, UX-01

**Por que, e nesta fase:**

A diferença entre planilha e jogo.

**O que é:**

- Recompensa agrupada, com moedas voando até o contador; '+renda' no pingo.
- Recusa com motivo e som.
- Vinheta e tremor no vazamento.
- Flash de acerto; hit-stop só de vista nos abates grandes.
- Morte por atrito com número e ícone próprios.
Com limite de efeitos simultâneos e opção de reduzir.

**Valor para o jogador:**

Cada ação tem resposta: a diferença entre planilha e jogo.

**Pronto quando:**

Moedas voando, recusa com motivo e som, vinheta no vazamento, hit-stop só na vista, ícone de morte por atrito e limite de efeitos.

**Como verificar:**

Contagem de eventos no log, e print no meio da luta.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 2) É o que transforma planilha em jogo. Moedas voando, motivo da recusa, flash de acerto e tremor no vazamento são o padrão que o jogador de TD espera. Os eventos já existem no LaneSim (EnemyDespawned, contadores de atrito e de torre), então o custo é de apresentação. Hit-stop só na vista não mexe no determinismo. O risco é poluir a tela com 30 bichos, por isso o limite de efeitos e a opção de reduzir são obrigatórios. Vem logo depois de UI e som.
- (logo; valor 4, custo 3, risco 2) É o que transforma planilha em jogo, mas é uma lista de 6 coisas e não um M. Uma parte já existe: Juice.Shake no vazamento e o hit flash no EnemyView. Moedas voando até o contador exigem ligar a posição do mundo à UI Toolkit, e a recusa com som depende do SOM-01. O hit-stop tem que ficar só na vista para não mexer no determinismo, o que está certo na proposta. Há risco de excesso de efeitos e de FPS, que já está apertado com a grama. Entra depois do UX-01 e do SOM-01.

</details>

---
