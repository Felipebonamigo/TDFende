# Fase 14 — Bichos novos e voo de verdade

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 4-6 sessões
- **Créditos Meshy estimados:** 0-90 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Completar o elenco do EA com uma receita automatizada, mais um céu que vira um segundo problema de posição.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Meta de elenco atingida, ou o que falta listado no corte.
- BalanceLab verde.
- O Felipe aprova.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar cada bicho novo (modelo, licença, créditos).
- [ ] O voador sofre atrito?

## Tarefas, em ordem

- [ ] BICHO-01 — Voo de verdade
- [ ] TORRE-02 — Alvo terra/ar por torre
- [ ] BICHO-02 — Bando voador
- [ ] BICHO-10 — Javali-mãe com leitões
- [ ] TEC-21 — Receita automatizada de bicho e torre
- [ ] BICHO-05b — Portador próprio de armadura (tatu ou pangolim), se houver modelo

---

### BICHO-01 — Voo de verdade

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 3 · prioridade 3
- **Depende de:** TEC-12

**Por que, e nesta fase:**

A Águia 'voa' mas anda pelo labirinto.

**O que é:**

'Voa' vira um campo próprio, separado de 'imune ao atrito' (hoje derivado de AttritionScale <= 0). O voador vai em linha reta da entrada à base, e o empurrão o recua na reta. CanBuild deixa de recusar construção sob voador. A Águia é recalibrada ou fica como planadora baixa.

**Valor para o jogador:**

Pune quem só faz labirinto e cria um segundo problema de posição.

**Pronto quando:**

Voo em reta, empurrão na reta e CanBuild liberado sob voador. Decidir se o voador sofre atrito. Mesmo tempo até a base com labirinto curto e longo.

**Como verificar:**

O tempo do voador até a base é igual com labirinto curto e longo.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 3, risco 3) É a base de três outras ideias (BICHO-02, 03 e 04) e corrige uma incoerência que o jogador nota: a Águia 'voa' e mesmo assim anda pelo labirinto. Separar 'voa' de 'imune ao atrito' é ótimo para a tese, porque um voador sujeito ao atrito faz a fronteira cobrir a reta. O custo real está em recalibrar a Águia, nas metas estatísticas da IA, na nota da IA e no empurrão na reta, não no código de voo. Deve ser o primeiro item técnico do lote de conteúdo.
- (depois; valor 4, custo 3, risco 3) Não tem custo de arte, porque reforma a Águia. O movimento em reta, o empurrão na reta e o CanBuild são simples. O custo real está no rebalanceamento: a Águia foi calibrada por medição e passaria a ignorar labirinto e atrito ao mesmo tempo, o que esvazia a tese contra ela. Isso afeta as metas da IA e do BalanceLab. Só compensa junto com o BICHO-02/04, que dependem de arte e da loja nova. Fazer depois de testar se a fronteira tem graça.

</details>

---

### TORRE-02 — Alvo terra/ar por torre

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2 · risco 3 · prioridade 3
- **Depende de:** BICHO-01

**Por que, e nesta fase:**

Com 2 ou mais voadores, o céu pede resposta.

**O que é:**

Campos HitsAir/HitsGround: Morteiro e Fogo não acertam o ar, Sentinela e Ar têm bônus. Ícone 'antiaéreo' na loja.

**Valor para o jogador:**

Ameaça aérea pede resposta específica.

**Pronto quando:**

HitsAir/HitsGround e ícone antiaéreo. DamageDealt do Morteiro contra voador = 0.

**Como verificar:**

DamageDealt do Morteiro contra voador = 0.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 2, risco 3) Hoje só a Águia voa, e ela ainda anda pelo labirinto. O VsFlyingMultiplier já dá vantagem à Sentinela (2,6x) e ao Ar (1,8x). Proibir Morteiro e Fogo de acertar o ar agora só deixa a Águia mais forte, e ela já é imune ao atrito. Pode virar frustração de 'perdi porque não tinha a torre certa'. Só vale com o BICHO-01 (voo de verdade) e mais de um voador, com balanceamento re-medido.
- (depois; valor 2, custo 2, risco 3) Hoje só existe um voador, a Águia. Tirar Morteiro e Fogo do ar só fortalece a Águia e mexe nas metas do BalanceLab sem trazer decisão nova. Só vale quando houver vários voadores (BICHO-01). Na Sim é barato (dois bool e um filtro na mira); o caro é rebalancear e fazer o ícone de uma loja que ainda não existe.

</details>

---

### BICHO-02 — Bando voador

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3,5 · risco 3 · prioridade 3
- **Depende de:** BICHO-01, BUG-01

**Por que, e nesta fase:**

Enxame aéreo e cena forte.

**O que é:**

Bando de 5-6 frágeis com voo reto, imunes ao atrito. A resposta é Ar, Morteiro ou Sentinela. Modelo leve, com a fase da animação variada entre os indivíduos.

**Valor para o jogador:**

Pressão aérea de enxame e uma cena visual forte.

**Pronto quando:**

5-6 aves com fase variada e animação assada ou instancing, sujeitas ao atrito (a fronteira vira o contra). FPS cai no máximo 5%.

**Como verificar:**

O Ar mata o bando com menos ouro que a Sentinela, e o FPS cai no máximo 5%.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) Uma revoada de corvos ou urubus sobre o campo é uma cena forte e realista, e ave em loop de bater asa é a animação mais fácil de achar pronta. Contras: depende de BICHO-01 e de BUG-01, e 5 a 6 malhas com skin por envio, vezes vários envios, pesa no FPS (meta de no máximo 5% parece otimista sem LOD nem instancing). Sugiro que NÃO seja imune ao atrito: frágil e sujeito ao atrito, a fronteira vira o contra, o que fortalece a tese em vez de criar mais uma exceção ao lado da Águia.
- (depois; valor 3, custo 4, risco 3) Com o BICHO-01 pronto, a Sim é só uma linha de catálogo. O caro é a arte: uma ave realista com batida de asa e licença limpa é rara, e o Meshy não anima voo. Com 5-6 SkinnedMeshRenderer por envio e vários envios, a meta de FPS no máximo 5% abaixo exige animação assada ou instancing. Pegar o enxame já existe com o Rato, então o ganho de jogabilidade é parcial.

</details>

---

### BICHO-10 — Javali-mãe com leitões

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2,5 · risco 2,5 · prioridade 3
- **Depende de:** BUG-01

**Por que, e nesta fase:**

O 'ah, não', e o Morteiro brilha.

**O que é:**

Ao morrer solta 3 leitões rápidos: o javali que já existe, em escala 0,45. Nascem sem consumir rng fora da ordem.

**Valor para o jogador:**

O 'ah, não' que vira clipe, e o Morteiro brilha.

**Pronto quando:**

3 leitões como tipo oculto, nascimento adiado e rng em ordem definida. O Morteiro vence o Canhão. Replay determinístico.

**Como verificar:**

O Morteiro mata o conjunto mais rápido que o Canhão, e o replay é determinístico.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 2, risco 2) O momento 'ah, não' é bom para clipe e trailer na Steam e dá papel claro ao Morteiro. Reaproveita o modelo do javali, então não gasta crédito. Riscos: os leitões precisam entrar no fim do SendCatalog como unidade oculta, por causa do acoplamento por índice, e nascer em ordem determinística no replay. Num jogo realista, um adulto encolhido a 0,45 não parece leitão (leitão tem listras), mas visto de cima isso passa.
- (depois; valor 4, custo 3, risco 3) Reaproveita o javali em escala 0,45, sem arte nova. A Sim pede nascimento adiado, porque hoje Kill ocorre dentro do laço sobre um array fixo. Também precisa definir a ordem do rng, a contagem de vidas e recompensa dos leitões, os casos de morte na base, de repasse e de morte súbita, e o estouro do array. O momento de clipe vale, mas é um 10º envio que não cabe nas teclas 1-9 nem na barra OnGUI. Encaixa como envio do segundo mercado.

</details>

---

### TEC-21 — Receita automatizada de bicho e torre

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3
- **Depende de:** TEC-12, TEC-22, VIS-08

**Por que, e nesta fase:**

Fecha o elenco do DES-23 em série.

**O que é:**

Um skill e o script Tools/NovoBicho.ps1 encadeiam:
1. conferir a licença pela API;
2. escrever bichos.json ou torres.json;
3. baixar → converter → verificar;
4. copiar com a exceção no .gitignore;
5. registrar no manifesto;
6. criar a linha no catálogo e na tabela de vista;
7. rodar o FlowSim.

**Valor para o jogador:**

Mais bichos e torres em horas, com escala, licença e crédito sempre certos.

**Pronto quando:**

Skill e script encadeiam licença → conversão → manifesto → catálogo → FlowSim. Um bicho vai do zero ao print numa sessão, até atingir a meta.

**Como verificar:**

Um 10º bicho real vai do zero ao print da captura numa sessão, com o FlowSim verde.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Automatizar antes de fazer à mão 2-3 vezes no tema final é automatizar a receita errada. O tema e a fonte de arte (gratuito ou pacote pago) ainda vão mudar, e isso muda licença, conversão e escala. Depende de TEC-12, TEC-22 e VIS-08. Fazer quando estiverem entrando bichos novos em série.
- (depois; valor 3, custo 3, risco 2) O pipeline baixa → converte → verifica já existe em Python e Blender para torres e bichos, então a ideia é mais um invólucro do que algo novo. Automatizar agora seria automatizar o caminho errado: com a direção realista, a fonte provavelmente muda para Fab ou Asset Store (sem API de licença do Sketchfab, importada no editor), e o tema pode mudar. Vale automatizar depois de repetir a receita nova 2 ou 3 vezes à mão. Depende de TEC-12, TEC-22 e VIS-08.

</details>

---

### BICHO-05b — Portador próprio de armadura (tatu ou pangolim), se houver modelo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Mais bichos com papel.

**Pronto quando:**

Modelo animado com licença achado; sem modelo, o item sai.

---
