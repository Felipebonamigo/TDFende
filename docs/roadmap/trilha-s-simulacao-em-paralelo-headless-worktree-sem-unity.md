# Trilha S — Simulação em paralelo (headless, worktree, sem Unity)

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 12-16, em paralelo (fora do caminho crítico) sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Adiantar tudo que é regra pura, para que o Portão 1 rode com o núcleo afiado e cada fase de conteúdo só precise da vista. Roda desde a Fase 0 até a Fase 12, em sessões próprias que não pisam no executável.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Antes do Portão 1: TEC-31, BUG-04, BUG-05, DES-05, DES-04, TORRE-03, TORRE-01, BICHO-05, BICHO-06 e DES-03s verdes.
- Antes da Fase 9: TEC-12, TEC-17, TEC-27 e MERC-01s.
- Cada item com teste visto falhar, assinatura de replay e BalanceLab verde, sem tocar o exe.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar a tabela econômica nova do envios.txt.
- [ ] Float ou ponto fixo (TEC-27), antes da Fase 11.
- [ ] Aprovar o design architectural da vida de torre quando estiver pronto.

## Tarefas, em ordem

- [x] TEC-31 — Fingerprint com hash quantizado de posições — 09/10/2026 (52accc2)
- [ ] BUG-04 — SendCatalog completa por nome e valida id
- [ ] BUG-05 — Assinatura do replay cobre todos os campos
- [ ] TEC-12 — Conteúdo por chave estável
- [ ] TEC-17 — Eventos só de vista
- [ ] DES-05 — Envios com papel econômico
- [ ] DES-04 — Vazamento pesa pelo porte
- [ ] TORRE-03 — Upgrade amplia a fronteira
- [ ] TORRE-01 — Mira inteligente (fase 1)
- [ ] BICHO-05 — Armadura no Rinoceronte (Sim)
- [ ] BICHO-06 — Urso regenera fora da fronteira (Sim)
- [ ] TEC-14 — Matriz de contras medida (relatório)
- [ ] DES-03s — Chaves do laboratório da tese
- [ ] TEC-27 — Spike de determinismo: float × ponto fixo, e lockstep virtual
- [ ] MERC-01s — Regras do segundo mercado e do primeiro sabotador
- [ ] CONT-S — Regras do conteúdo seguinte (em ordem)

---

### TEC-31 — Fingerprint com hash quantizado de posições

- [x] **Status:** feito em 09/10/2026, commit 52accc2. `StateHash` (FNV-1a 64), `SimFingerprint` (inimigos, torres e tiros pelas portas públicas) e `CountingRandom`; 14 testes novos vistos falhar antes; FlowSim todo verde; BalanceLab (`match 12`) idêntico antes e depois; Runtime compila contra o Unity 2021 do NuGet. Não cobre os temporizadores da IA (MANUAL, seção 4).  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O StateFingerprint (MatchRunner.cs:86) só tem contadores, sem posição nenhuma. Mesmo assim, UX-03, TEC-17 e TEC-19 o usam como prova. É a metade barata do TEC-16, e precisa vir antes deles.

**Pronto quando:**

O hash inclui posições quantizadas, vidas, torres e rng. Um teste visto falhar mostra que 0,01 de deslocamento muda o fingerprint. Os testes atuais seguem verdes.

---

### BUG-04 — SendCatalog completa por nome e valida id

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BUG · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 1,5 · risco 1,5 · prioridade 4

**Por que, e nesta fase:**

Um envios.txt antigo apaga em silêncio o 10º bicho ou a elite.

**O que é:**

Hoje o LoadFrom troca a lista inteira: um envios.txt antigo com 9 linhas apaga o 10º bicho ou a elite. A correção é completar por Name, como o TowerCatalog já faz, e validar o id no SendCatalog.Get.

**Valor para o jogador:**

Impede que conteúdo novo suma em silêncio quando o arquivo de balanceamento é antigo.

**Pronto quando:**

- Teste visto falhar com um catálogo de 10 envios só de teste e um envios.txt de 9 linhas.
- Merge mantém a ordem compilada.
- Id inválido falha alto, com erro visível, sem cair em outro id.

**Como verificar:**

Teste FlowSim, visto falhar antes: com 10 envios de fábrica e um envios.txt de 9 linhas, o Count tem que ser 10 (hoje dá 9). Get(99) não pode estourar.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) Conferido: o LoadFrom faz All = parsed e o Get(id) não valida. O jogador não edita o envios.txt, mas o Felipe edita. Sem isto, o primeiro bicho novo (10º) some em silêncio e vira caça a fantasma. Tem que estar pronto antes de acrescentar qualquer envio ou torre.
- (logo; valor 3, custo 2, risco 2) Confirmado: LoadFrom faz All = parsed, e SendCatalog.Get(id) não valida. É pré-requisito obrigatório antes do 10º bicho ou do segundo mercado. Sem isso, o envios.txt local do Felipe apaga conteúdo novo sem aviso. Cuidados: o merge por Name tem que manter a ordem compilada, porque o índice é identidade em ModelLib, Vfx e replay. Não copiar o clamp silencioso do TowerCatalog.Get (que devolve 0): id inválido tem que falhar alto. 'Get(99) não pode estourar' esconderia bug. O teste precisa injetar um catálogo de 10 envios só para o teste.

</details>

---

### BUG-05 — Assinatura do replay cobre todos os campos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BUG · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 1,5 · custo 2 · risco 1,5 · prioridade 3,5

**Por que, e nesta fase:**

Burn, empurrão, atrito, escalada e nomes ficam fora da assinatura. Todo campo novo repetiria o buraco.

**O que é:**

O Replay.CurrentCatalogSignature deixa de fora AttritionPctPerSecond, SendScalePerMinute, BurnPctPerSecond, BurnSeconds, Knockback e os nomes. Por isso um replay gravado com outras regras reproduz outra partida sem avisar. A correção é cobrir os campos por reflexão, com um teste que altera cada campo.

**Valor para o jogador:**

Replays, desafios e laboratório deixam de mentir quando o balanceamento muda.

**Pronto quando:**

- Teste visto falhar: mudar burn, push ou atrito muda a assinatura.
- Campos ordenados por nome e FNV para os nomes.
- Replays antigos invalidados de propósito.

**Como verificar:**

Teste FlowSim visto falhar antes: mudar o burn ou o push tem que mudar a assinatura.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 1, custo 2, risco 1) Conferido: a assinatura ignora atrito, escalada, burn e knockback. Hoje nenhum jogador vê replay, então o impacto é só no laboratório. Vale fazer junto do primeiro campo novo da Sim (vida de torre, tier), e não como tarefa isolada agora. A reflexão é razoável, mas cuidado com campos float e com a ordem dos campos.
- (logo; valor 2, custo 2, risco 2) Confirmado: a assinatura cobre 7 campos do envio e 9 da torre, e deixa de fora Burn, Knockback, AttritionPctPerSecond, SendScalePerMinute e os nomes. Fazer antes de criar os campos novos (vida de torre, ataque, tier), senão cada um repete o buraco. Para o jogador vale pouco hoje, porque replay ainda é ferramenta de desenvolvimento. Dois riscos que o plano não cita. Primeiro: o FlowSim roda em net10.0, onde string.GetHashCode é aleatório por processo, então os nomes precisam de hash estável (FNV). Segundo: GetFields não garante ordem, então é preciso ordenar por nome. A mudança invalida todos os replays já gravados, o que é aceitável agora.

</details>

---

### TEC-12 — Conteúdo por chave estável

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 4
- **Depende de:** BUG-04

**Por que, e nesta fase:**

É pré-requisito de qualquer bicho, torre ou mercado novo. Hoje um índice fora do lugar troca modelo e efeito em silêncio.

**O que é:**

Uma tabela de vista por Name/Key substitui os switch por índice: ModelLib, Vfx, ModelLibTiers e o _arc do ProjectileView. O fallback loga 'sem modelo para X'. Os testes do FlowSim passam a usar IdOf('Lobo'). Os ids inteiros continuam no replay.

**Valor para o jogador:**

O 10º bicho nunca nasce com o modelo errado.

**Pronto quando:**

- Tabela de vista por chave (que não é o nome exibido) em ModelLib, Vfx, ModelLibTiers e ProjectileView.
- Testes usam IdOf.
- Com envios.txt embaralhado, cada bicho mantém o modelo.

A parte de vista é conferida no -captura da próxima sessão Unity.

**Como verificar:**

Com um envios.txt embaralhado, cada bicho continua com o próprio modelo no -captura.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 3, risco 3) O jogador não vê, mas é pré-requisito barato para o que o Felipe mais pediu: mais bichos, mais torres e um segundo mercado. Com o índice espalhado em 5 lugares, cada bicho novo arrisca nascer com o modelo ou projétil errado. Manter os ids inteiros no replay limita o risco. Fazer antes de acrescentar o 10º bicho, não antes.
- (logo; valor 3, custo 3, risco 2) É pré-requisito de qualquer 10º bicho, 7ª torre ou segundo mercado. Hoje são cerca de 20 'case N' em ModelLib/Vfx, mais o _arc do ProjectileView e os testes por índice. Um bicho novo no lugar errado aparece silenciosamente com o modelo de outro. O trabalho é mecânico e os 183 Check() o protegem, e os ids do replay não mudam, então o risco é baixo. A troca de tema pede um cuidado: a chave não pode ser o nome exibido (Canhão, Morteiro...), porque o tema pode renomear tudo. Depende do BUG-04.

</details>

---

### TEC-17 — Eventos só de vista

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 1,5 · prioridade 4

**Por que, e nesta fase:**

São os ganchos de recusa com motivo, compra, renda, alerta, som e fim de partida.

**O que é:**

Eventos novos na LaneSim e no MatchRunner que não mudam estado: EnemySpawned, EnemyHit, StatusApplied, BountyPaid, IncomeTick, SendBought, LifeLost, CommandRejected(motivo), SuddenDeathStarted e MatchEnded. São enfileirados e consumidos no Sync.

**Valor para o jogador:**

São os ganchos de todo o som, feedback, alerta e feed.

**Pronto quando:**

EnemySpawned, EnemyHit, StatusApplied, BountyPaid, IncomeTick, SendBought, LifeLost, CommandRejected(motivo), SuddenDeathStarted e MatchEnded. Pronto quando o fingerprint quantizado é igual com e sem assinantes.

**Como verificar:**

StateFingerprint igual com e sem assinantes, e a soma dos BountyPaid igual ao ouro ganho em abates.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 2) É o item técnico de maior alavanca para o 'feel'. O jogo tem zero som e zero feedback de compra, recusa, renda ou vida perdida, e tudo isso pendura nestes eventos. É barato porque não muda estado (o fingerprint igual prova isso). Fazer como primeiro passo do bloco de som e da HUD nova, não isolado.
- (logo; valor 4, custo 2, risco 1) É o caminho mais barato para som (problema 8) e para o feedback de compra, renda e alerta (problema 7). O padrão já existe: LaneSim já publica EnemyDespawned, TowerChanged, TowerFired e TowerSold, então a ideia só estende o que está lá. O risco é baixo porque a verificação por StateFingerprint com e sem assinantes é objetiva. Não faz sentido sozinha: deve entrar na mesma leva da primeira sessão de som e HUD novo. O CommandRejected(motivo) exige mexer na validação do MatchRunner, que é a parte menos trivial.

</details>

---

### DES-05 — Envios com papel econômico

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 2 · prioridade 4

**Por que, e nesta fase:**

A renda por ouro é plana (0,083 a 0,10) e o Elefante domina. Sem isso, o A/B do Portão 1 mede um núcleo cego.

**O que é:**

Redesenhar o envios.txt num espectro: investimento (Cachorro com mais renda e corpo fraco) contra pressão (ratos, lobo, tigre, com pouca renda e muita vida por ouro). A renda por ouro cai conforme a pressão sobe.

**Valor para o jogador:**

A tensão clássica: investir e ficar frágil, ou apertar e ficar pobre.

**Pronto quando:**

envios.txt num espectro investimento × pressão, com a IA reajustada. Correlação negativa no BalanceLab, vista falhar com a tabela de hoje.

**Como verificar:**

Correlação negativa no BalanceLab, falhando hoje com a tabela plana.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 2) No envios.txt a renda por ouro é praticamente igual em todos (de 0,083 a 0,10), então escolher qual bicho comprar quase não tem consequência econômica. Separar 'investir' de 'apertar' é o que dá peso a cada compra (é assim no Legion TD), e isso se faz mexendo em números já existentes, sem código novo. Entre as ideias de design, é a de melhor relação valor/custo. O teste de correlação negativa falha hoje com a tabela plana, como a regra pede.
- (logo; valor 4, custo 2, risco 2) É a ideia de design com melhor relação custo/valor, porque quase tudo é dado no envios.txt. A tabela de hoje é plana de fato: renda/custo fica entre 0,083 e 0,10 para todos. O Elefante tem a melhor vida por ouro (5,0) com renda média, então domina Lobo e Tigre sem nenhuma troca. Corrigir isso cria a tensão investir contra apertar. Risco: a IA pondera envios por nota e precisa de ajuste, e o BalanceLab tem de rodar de novo. Nenhuma arte nova.

</details>

---

### DES-04 — Vazamento pesa pelo porte

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 2 · prioridade 4
- **Depende de:** BUG-05

**Por que, e nesta fase:**

Hoje 4 ratos tiram 4 vidas, e o Elefante de 90 de ouro tira 1.

**O que é:**

LivesCost por porte (rato 1 a elefante 3, elite 4-5), ou integridade 100 com dano por porte. Voltas repassadas custam 1. Tremor proporcional na fortaleza. A IA pondera o perigo pelo porte.

**Valor para o jogador:**

Deixar passar um elefante passa a doer mais que um rato.

**Pronto quando:**

LivesCost do rato (1) ao elefante (3), e a volta repassada custa 1. Teste e BalanceLab verdes. O '-3' no HUD entra na Fase 5.

**Como verificar:**

O Elefante tira 3 e, na 2ª volta, 1; o BalanceLab fica verde.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 2, risco 2) Corrige uma incoerência real: hoje todo vazamento tira 1 vida (LaneSim.cs:677). Com isso o rato, que vem em 4 por 22 de ouro, tira 4 vidas, e o elefante, que custa 90, tira só 1. O jogador espera que um elefante passando doa. É barato (tabela + Lives -= custo) e o tremor dá um feedback forte. Requer re-rodar o BalanceLab, porque mexe no valor do rato como ferramenta de vazar.
- (logo; valor 3, custo 2, risco 2) Barato na Sim: hoje é um Lives-- fixo em LaneSim.cs:677. Basta um campo LivesCost no SendUnit e uma marca 'repassado' no SimEnemy, para que a 2ª volta custe 1. Dá legibilidade real. Mas encurta a partida, que hoje tem média de 9,1 min com 20 vidas: as metas e a varredura de atrito e escalada têm de rodar de novo, e a assinatura do replay muda. Também precisa de feedback no HUD ('-3') para o jogador entender. Vem depois do BUG-05 e dos bloqueios visuais.

</details>

---

### TORRE-03 — Upgrade amplia a fronteira

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 1,5 · risco 2 · prioridade 4

**Por que, e nesta fase:**

Subir de nível passa a mudar o mapa, o que reforça a tese.

**O que é:**

Cada nível acima do 1 soma ~0,2 célula ao BorderRadius, com teto por tipo. O upgrade refaz o território. Uma função pura BorderAt(tipo, nível) serve à sim, ao fantasma e à IA.

**Valor para o jogador:**

Subir de nível muda o mapa, não só um número.

**Pronto quando:**

BorderAt(tipo, nível) puro, usado pela Sim, pelo fantasma e pela IA: +~0,2 por nível, com teto e com a constante na assinatura. BalanceLab verde.

**Como verificar:**

Canhão nv 4 cobre mais células que nv 1, e o BalanceLab fica verde.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 1, risco 2) É barato: o BorderRadius já é por tipo, e o território é recalculado ao construir. Reforça a tese, porque subir de nível passa a mudar o mapa e não só um número. Só funciona para o jogador se a fronteira for bem visível. Território crescendo sem um visual forte é invisível. O atrito de 0,19 da vida máxima por segundo é forte, então o teto por tipo e o BalanceLab verde são obrigatórios.
- (logo; valor 3, custo 2, risco 2) Uma das poucas ideias que reforçam a tese sem pedir arte nova. BorderAt(tipo, nível) é uma função pura, e RebuildTerritory já existe e é barato. Riscos: o atrito de 0,19/s é forte, então mais cobertura por upgrade pode baratear demais a defesa (o BalanceLab decide). A constante de crescimento precisa entrar na assinatura do replay. A IA hoje sobe nível só por DPS/ouro e precisa de um termo de cobertura. O jogador só percebe a mudança se a fronteira for bem desenhada, e a prévia no fantasma ainda não existe.

</details>

---

### TORRE-01 — Mira inteligente (fase 1)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2,5 · prioridade 4
- **Depende de:** TEC-12, UX-15

**Por que, e nesta fase:**

A torre que deixa o Lobo passar atrás do Elefante parece bug.

**O que é:**

Fase 1: o alvo padrão vira 'mais adiantado' (menor custo do flow field), com regra por tipo (Sentinela mira voador primeiro; Gelo e Fogo preferem quem ainda não tem o efeito) e desempate por índice.
Fase 2: modo por torre (Primeiro, Mais forte, Mais perto, Voador, Suporte) pelo comando SetTargetMode, com argumento opcional no replay.

**Valor para o jogador:**

Acaba a torre 'burra' que deixa o Lobo passar atrás do Elefante.

**Pronto quando:**

- 'Mais adiantado' pelo custo do flow field.
- Sentinela prioriza voador; Gelo e Fogo preferem quem não tem o efeito.
- Versão de regras no replay.
- Metas recalibradas.

**Como verificar:**

FlowSim: a Sentinela acerta a Águia antes do Rato, e um replay antigo sem o argumento é lido igual.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) A torre que mira o mais próximo e deixa o Lobo passar atrás do Elefante parece bug para o jogador. 'Mais adiantado' pelo custo do flow field é o padrão do gênero e sai barato na sim. A fase 1 é logo, mas mexe nas metas estatísticas do BalanceLab, então tem que ser re-medida. A fase 2 (modo por torre) só faz sentido com o painel de seleção (UX-15). Antes disso é tecla escondida que ninguém usa.
- (logo; valor 4, custo 3, risco 3) Corrige uma falha real de jogo. A fase 1 é pequena em código: o laço de mira em LaneSim.cs (~linha 708) troca 'menor distância' por 'menor custo no flow field', que já existe. Só que isso muda todo o balanceamento: as metas estatísticas do BalanceLab e a nota da IA vão precisar de recalibração. Pior: CurrentCatalogSignature só hasheia os números do catálogo, então um replay antigo roda sem erro e chega a outro desfecho. A verificação 'lido igual' não pega isso, e o replay precisa ganhar uma versão de regras. A fase 2 (modo por torre) espera o painel de seleção (UX-15); a fase 1 não precisa esperar.

</details>

---

### BICHO-05 — Armadura no Rinoceronte (Sim)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 2 · prioridade 3,5
- **Depende de:** TEC-12, BUG-01

**Por que, e nesta fase:**

É o primeiro motivo real para pensar em dano por tiro. O Rinoceronte já parece blindado, e com isso esse passa a ser o único papel especial dele.

**O que é:**

Campo Armor: cada acerto perde esse valor, com piso de 20% do dano. Atrito e queima ignoram a armadura. Portadores: Tatu-bola ou Pangolim (enrola com o Roll do ModelRig), Búfalo de elite e Rinoceronte-alfa.

**Valor para o jogador:**

Primeiro bicho que obriga a pensar em dano por tiro.

**Pronto quando:**

Armor com piso de 20%; atrito e queima ignoram a armadura. Teste max(9 − 6; 1,8) e Fogo vencendo por ouro. Faísca e ícone entram na vista na Fase 3.

**Como verificar:**

Sentinela causa max(9 − 6; 1,8) por tiro, e o Fogo vence por ouro.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 2, risco 2) É o melhor custo-benefício de conteúdo da lista. É uma mecânica clássica e legível (faísca e som metálico no acerto), faz a escolha de torre importar e, como o atrito ignora a armadura, reforça a tese. Dá para lançar sem arte nova, dando armadura ao Rinoceronte ou ao Elefante que já existem. Ressalva: o Roll do ModelRig gira roda (AnimKind.Wheels), não enrola tatu. Pangolim realista enrolando precisa de animação própria, que é rara de achar pronta. Deve ser o primeiro do lote de conteúdo, depois de BUG-01 e da UI.
- (logo; valor 4, custo 2, risco 2) Na Sim é barato: um campo Armor, uma subtração com piso em HitEnemy, e atrito e queima passando direto. Começar pelo Rinoceronte/Elefante, que já existem, não custa nada de arte. É a primeira razão real para escolher torre por dano por tiro. Risco: a Sentinela perde força e as metas do BalanceLab precisam ser recalibradas. Pangolim e Búfalo, que precisam de modelo novo, ficam para depois.

</details>

---

### BICHO-06 — Urso regenera fora da fronteira (Sim)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 2 · prioridade 3,5
- **Depende de:** BUG-01

**Por que, e nesta fase:**

Buraco na fronteira passa a custar caro.

**O que é:**

Fora do território, regenera um % da vida por segundo. Queimando não regenera. Tem teto ou desliga na morte súbita. Portadores: Urso, matilha de Hienas ou Hipopótamo.

**Valor para o jogador:**

Ensina que buraco na fronteira custa caro.

**Pronto quando:**

Regenera com teto; não regenera queimando e desliga na morte súbita. Teste de mais vida na saída do trecho sem fronteira. O brilho entra na Fase 3.

**Como verificar:**

Sai de um trecho sem fronteira com mais vida do que entrou, e não regenera queimando.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 2, risco 2) É a ideia que melhor serve a pergunta central ainda sem resposta, se a fronteira tem graça: buraco na fronteira passa a custar caro. A mecânica fica só na Sim e é barata. Lança com o Urso que já existe, sem arte nova, e um brilho verde com partícula basta para ler. Hiena e hipopótamo só depois, porque cada bicho realista novo custa caça de modelo animado e licença. Precisa de teto, senão vira tanque infinito contra quem tem pouco território.
- (logo; valor 4, custo 2, risco 2) Reforça a tese: buraco na fronteira passa a custar caro. O Territory.Contains já roda por tique para o atrito, então a regeneração é um else ao lado. O Urso já existe, sem custo de arte. Precisa de um retorno visual barato (barra de vida subindo, brilho) para o jogador entender. Cuidado com a vida que vai junto no repasse entre lanes.

</details>

---

### TEC-14 — Matriz de contras medida (relatório)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 3 · risco 2,5 · prioridade 3,5
- **Depende de:** TEC-12

**Por que, e nesta fase:**

Cada unidade precisa de papel, e o 'bom contra' da loja não pode mentir.

**O que é:**

Um modo do FlowSim mede cada par torre × bicho com o mesmo ouro. Contratos no pre-commit:
- cada torre é a melhor contra pelo menos 1 envio;
- cada envio tem uma resposta 15-30% melhor que o Canhão;
- nenhuma torre é a melhor contra mais de 40%.
Papéis declarados geram contratos. A matriz exporta contras.txt para a loja ('bom contra' medido). Opcional: a IA usa a matriz.

**Valor para o jogador:**

Toda unidade tem papel claro, e a loja nunca mente.

**Pronto quando:**

FlowSim mede cada par torre × bicho com o mesmo ouro e exporta contras.txt. Roda como relatório e só vira portão quando o catálogo estabilizar.

**Como verificar:**

A matriz reproduz os contratos escritos à mão, e um bicho 'inútil' de teste faz o commit falhar.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Em Line TD, a graça é saber que 'contra águia uso Sentinela'. Torre ou bicho sem papel é conteúdo morto, e a loja 'bom contra' medida vale muito para o jogador novo. É o que guia quais torres e bichos novos criar. Ressalva: medir primeiro e só depois transformar em contrato de pre-commit, porque os limites 15-30%/40% são chute e o pre-commit já leva ~3 min.
- (depois; valor 3, custo 3, risco 3) Encaixa no laboratório do FlowSim, que já roda 300 partidas, e alimenta o 'bom contra' da loja com dado medido. Hoje só remede um 6×9 cuja regra 'cada torre responde a um envio' já foi decidida por medição. Ganha valor no começo da fase de conteúdo novo, depois do TEC-12. Risco subestimado: contratos de balanceamento no pre-commit (que já leva cerca de 3 min) travam commits que não têm nada a ver com balanceamento, assim que alguém mexe num número. Começar como relatório e só virar gate quando o catálogo estabilizar.

</details>

---

### DES-03s — Chaves do laboratório da tese

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O A/B do Portão 1 precisa de atrito ligado/desligado sorteável e de métricas definidas.

**Pronto quando:**

Chaves de atrito, fronteira dobrada, ouro infinito e 2x, que voltam ao padrão. Métricas de virada e tensão testadas com fixture. Sorteio cego do modo, revelado só no diário.

---

### TEC-27 — Spike de determinismo: float × ponto fixo, e lockstep virtual

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Online e placar exigem determinismo entre máquinas, e a escolha tem de vir antes de as Fases 11-14 empilharem regras em float. Lockstep é a tecnologia central do RTS.

**Pronto quando:**

- Mesmos replays no FlowSim (.NET), no exe Mono e num exe IL2CPP, com o fingerprint comparado.
- Decisão float ou ponto fixo registrada.
- PlayerId no MatchCommand.
- Lockstep virtual no FlowSim: 500 partidas com hash igual, e uma dessincronia plantada é detectada.

---

### MERC-01s — Regras do segundo mercado e do primeiro sabotador

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Quando a Fase 9 chegar, falta só a vista do pedido explícito.

**Pronto quando:**

- Tier em SendUnit e TowerType, abertura aos 3:00 (variante A).
- Estoque e recarga da elite (MERC-02).
- Fúria que atordoa a torre mais próxima (MERC-07), testada com a espécie definida no DES-23.
- BalanceLab verde e Difícil > Fácil.

---

### CONT-S — Regras do conteúdo seguinte (em ordem)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** G (3-5 sessões)

**Por que, e nesta fase:**

Encurta as Fases 11-14 para quase só vista.

**Pronto quando:**

Com teste visto falhar e BalanceLab verde:
- chefe (MERC-11), anti-pesado (TORRE-08), imune a controle (BICHO-09), apoio (TORRE-05);
- voo (BICHO-01), leitões (BICHO-10), melhoria de envio (MERC-14);
- design architectural da vida de torre (MERC-05) entregue para aprovação.

---
