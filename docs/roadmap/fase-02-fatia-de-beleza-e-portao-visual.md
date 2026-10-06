# Fase 2 — Fatia de beleza e Portão Visual

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 5-7 sessões
- **Créditos Meshy estimados:** 0 (piloto só com aprovação, e só se faltar peça) _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Uma lane que já parece jogo, com arte real no tema favorito, medida contra os jogos-régua e dentro do orçamento. Resolve já os problemas 4 e 5 do Felipe (chão e cenário) na versão 1.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Fatia aprovada contra os jogos-régua e registrada no MANUAL como tema definitivo.
- Exe com câmera tele, grade só ao construir, régua de escala (FlowSim verde), disco de contato e sem plastic.
- Desfile enviado; p95 e -estresse dentro do aviso no perfil mínimo.
- Decisões de AA, GI e enquadramento registradas.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Portão Visual: o Felipe diz 'é isso' com a fatia ao lado dos jogos-régua, e o p95 fica dentro do aviso? Se falhar, o segundo tema vira fatia.
- [ ] Enquadramento: morre, foge ou cai exausto.
- [ ] TAA/STP ou SMAA; assar a parte estática com APV e probes ou só skybox + SSAO.
- [ ] Hardware mínimo: GTX 1060, Iris Xe ou Steam Deck.

## Tarefas, em ordem

- [ ] VIS-04 — Faxina de coerência
- [ ] VIS-05 — Grade só ao construir
- [ ] VIS-09 — Câmera teleobjetiva
- [ ] VIS-08 — Régua de escala única
- [ ] VIS-10 — Sombra de contato no lugar do anel
- [ ] TEC-25 — Desfile de torres e bichos
- [ ] VIS-27 — Fatia de beleza no tema favorito
- [ ] VIS-28 — Spike de estabilidade temporal e luz indireta
- [ ] VIS-02p — Protótipo de enquadramento: morre, foge ou cai exausto
- [ ] TEC-33 — Perfil de hardware mínimo (aviso)
- [ ] MKT-02 — Hábito de captura para divulgação

---

### VIS-04 — Faxina de coerência

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 1 · risco 1 · prioridade 5

**Por que, e nesta fase:**

Tenda em cone e caixa de plástico denunciam maquete em qualquer tema.

**O que é:**

Saem as tendas em cone, as caixas plastic_crate, as árvores procedurais no campo de visão e o Torre_Canhao antigo. A cor do time fica só em bandeira e anel.

**Valor para o jogador:**

Melhora imediata: some a cara de maquete.

**Pronto quando:**

Print sem cones nem plastic_crate. Árvores procedurais fora do quadro de jogo. Cor do time só em bandeira e anel.

**Como verificar:**

Print sem cones nem tendas, e o log de cenário sem 'plastic'.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 1, risco 1) Ganho imediato e quase de graça: tenda em cone e caixa de plástico denunciam 'maquete' em qualquer print da Steam. Mesmo sem saber o tema final, isso nunca vai combinar com 'realista'. Cuidado único: tirar as árvores procedurais sem pôr nada no lugar pode expor a borda do terreno. Até a VIS-12, segurar com névoa e enquadramento, ou com poucas árvores da Poly Haven.
- (agora; valor 3, custo 1, risco 1) É remoção pura de coisa que denuncia maquete (tendas em cone, plastic_crate, Torre_Canhao antigo) e vale para qualquer tema. Ressalva: tirar as árvores procedurais antes de ter mata substituta (VIS-12) deixa o campo pelado. Tirar agora só o que está dentro do quadro, e esperar a troca para o resto. A verificação por log ('sem plastic') é boa e barata.

</details>

---

### VIS-05 — Grade só ao construir

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 1 · risco 1,5 · prioridade 5

**Por que, e nesta fase:**

A grade permanente faz a cena parecer planilha.

**O que é:**

A grade aparece só com uma torre selecionada, num raio de ~4 células em volta do cursor e com fade. O contorno da lane fica mais fino.

**Valor para o jogador:**

A cena para de parecer planilha fora da construção.

**Pronto quando:**

Com o cursor fora da sua lane, a grade some. Sobre a lane, fica fraca na lane inteira e forte num raio de ~4 células, com fade. Dois prints.

**Como verificar:**

Print sem grade e outro print com o cursor mostrando a grade local.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 1, risco 2) Grade branca em tudo é o que mais faz a cena parecer planilha, e esconder fora da construção é barato. Risco de design: no Line Tower Wars, planejar o labirinto é o coração do jogo, e um raio de só ~4 células pode atrapalhar. Sugiro mostrar a lane inteira bem fraca no modo construção e mais forte perto do cursor. E precisa existir um estado 'sem torre selecionada' de verdade: hoje Q/E sempre tem uma escolhida, então a grade nunca sumiria.
- (agora; valor 3, custo 1, risco 1) O GridOverlay tem 100 linhas e uma malha só por mapa. Trocar a malha por uma máscara radial em volta do cursor é barato. Atenção: no Tower Wars sempre há um tipo de torre selecionado (Q/E), então 'só com torre selecionada' não existe como estado hoje. O gatilho viável é 'cursor sobre a própria lane'. Sem dependência e sem risco para a Sim.

</details>

---

### VIS-09 — Câmera teleobjetiva

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 1 · risco 1,5 · prioridade 5

**Por que, e nesta fase:**

Com 60° a cena parece miniatura. Com 30-35° a perspectiva fica de estratégia e o horizonte feio some.

**O que é:**

O FOV passa do padrão de 60° para 30-35°, com a câmera mais longe. Névoa, far clip e distância de sombra acompanham, e o MinDist do close é revisto.

**Valor para o jogador:**

Some o efeito de miniatura e a perspectiva fica de jogo de estratégia.

**Pronto quando:**

FOV, névoa, far clip, sombra e MinDist revistos. A altura em pixels do rato fica acima do mínimo no métricas.json.

**Como verificar:**

Altura em pixels de bichos e torres no -captura acima do mínimo, e print aprovado.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 1, risco 1) Uma linha de código com efeito grande para o jogador. FOV de 60° numa câmera de estratégia dá distorção de grande-angular e cara de miniatura. Com 30-35° e a câmera mais longe, a perspectiva fica de jogo atual, e aparece menos horizonte, o que de quebra esconde o cenário feio em volta. Só vale conferir que bicho pequeno (rato) não vira um ponto: o critério de pixels mínimos na captura cobre isso.
- (agora; valor 4, custo 1, risco 2) O CameraRigDriver (83 linhas) nunca define o FOV e fica no padrão de 60°, então a mudança é pequena e isolada. Ela reduz o horizonte visível, o que também diminui a demanda por mata (VIS-12). O risco real é a distância de sombra: com a câmera ~1,8x mais longe, a sombra perde resolução por unidade (pesa no mobile). Também é preciso rever névoa, far clip e MinDist=8. A Sim e o raycast do mouse não são afetados.

</details>

---

### VIS-08 — Régua de escala única

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 2 · prioridade 4,5
- **Depende de:** BUG-01

**Por que, e nesta fase:**

O elefante (1,96 u) passa das torres e a fortaleza parece miúda.

**O que é:**

Altura real em metros comprimida por uma curva (ex.: 0,6×√h) e assada no conversor. A torre domina, o rato fica visível e o elefante deixa de ter 1,96 u, altura herdada do howdah do procedural.

**Valor para o jogador:**

Cena crível e legível, com a fortaleza parecendo fortaleza.

**Pronto quando:**

Curva 0,6×√h com piso, assada no conversor. Teste FlowSim visto falhar com os valores de hoje. Alturas medidas dentro de ±10%.

**Como verificar:**

Teste FlowSim de ordem e razões, visto falhar com os valores de hoje, e o log de alturas medidas ±10%.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 2, risco 2) Hoje o elefante é maior que as torres e a fortaleza não parece fortaleza: o jogador lê a cena errada. Custa pouco, porque é só vista e não mexe na Sim. Vale casar com o BUG-01: bicho invisível pode ser escala ou pivô, e a mesma medição de alturas serve aos dois. A curva 0,6×√h precisa de um piso, senão o rato some na câmera teleobjetiva. O teste visto falhar com os valores de hoje é bom.
- (logo; valor 4, custo 2, risco 2) A escala é necessária e a função pura de escala testável no FlowSim é um bom padrão. O custo real não é P: a altura hoje vem de def.Height do procedural (AnimalLoader:125) e as torres vêm de 3 estágios baixados com escalas próprias. É preciso medir os 27 modelos e rever o anel e o GaitBob, que usam a altura. Depende de ver os bichos (BUG-01), e pode até ser a causa do BUG-01. Por isso convém investigar os dois juntos, sem misturar os commits.

</details>

---

### VIS-10 — Sombra de contato no lugar do anel

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 1,5 · risco 1,5 · prioridade 4

**Por que, e nesta fase:**

O anel duro dá cara de brinquedo.

**O que é:**

Um disco suave tingido da cor do time substitui o anel duro. Realce sutil por MaterialPropertyBlock, e sombra da águia no chão.

**Valor para o jogador:**

Lê-se de relance quem é de quem, sem cara de brinquedo.

**Pronto quando:**

Disco neutro e suave, com a cor do time só numa borda fina. Sombra falsa da águia. Checagem com simulação de daltonismo.

**Como verificar:**

Print do desfile com todos os bichos e teste de daltonismo.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 1, risco 1) Disco de contato suave tira a cara de brinquedo do anel duro e ancora o bicho no chão, o que é bom e barato. Mas no Tower Wars a lane já diz de quem é o bicho (na sua lane só vêm os da IA), então a cor do time é quase redundante. Melhor usar o disco neutro como sombra e reservar a cor para estado (lento, queimando, dentro do território), que é a informação que a tese do jogo precisa mostrar. A sombra da águia no chão vale muito para leitura.
- (logo; valor 3, custo 2, risco 2) O disco suave no lugar do anel é barato (AddTeamRing já existe). O realce por MaterialPropertyBlock tem custo escondido: os modelos baixados usam materiais variados, o URP Lit não tem rim, e MPB tira o renderer do SRP Batcher, aumentando os draw calls com muitos bichos. Uma sombra falsa para a águia é trivial. Na prática depende de BUG-01, porque não dá para julgar realce de bicho invisível.

</details>

---

### TEC-25 — Desfile de torres e bichos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3 · risco 2 · prioridade 3,5
- **Depende de:** TEC-04, BUG-01

**Por que, e nesta fase:**

Verifica a escala, a coerência de fonte e todo modelo novo, na mesma luz e dentro do exe.

**O que é:**

Modos -captura estudio, desfile, vfx e lookdev com esfera cinza de 18%, esfera cromada, carta ColorChecker e cada torre e bicho numa plataforma. Métricas de albedo médio e desvio de tom revelam luz assada e a origem do amarelo.

**Valor para o jogador:**

Cada ganho visual é medido onde o Felipe joga.

**Pronto quando:**

-captura desfile gera a folha com as torres nos 3 estágios e os bichos lado a lado. Mede variância de luminância do albedo para detectar sombra assada.

**Como verificar:**

Função pura de métrica testada no FlowSim, e folha de contato aprovada pelo Felipe.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 3, risco 2) Esfera cromada, ColorChecker e métrica de albedo são prática de filme, exagero para um TD com assets baixados. A 'origem do amarelo' já é conhecida (a textura leafy_grass é marrom), e a métrica não descobriria nada novo. A parte útil é o 'desfile': todas as torres e bichos lado a lado, na mesma luz e no executável. Ela ajuda a diagnosticar o BUG-01, a validar a VIS-08 e a gerar retratos para a loja e prints para a Steam. Fazer só o desfile, junto com BUG-01 e VIS-08; o resto, talvez nunca.
- (logo; valor 3, custo 3, risco 2) Do pacote, só o modo 'desfile' (cada torre e bicho numa plataforma, em print) se paga já: ele verifica BUG-01, VIS-08, VIS-10 e qualquer modelo novo, e reaproveita o SmokeCapture. Esfera cinza, esfera cromada, ColorChecker e métricas de albedo são rigor de estúdio AAA para um projeto solo. A origem do amarelo já é conhecida (leafy_grass dá RGB 151,131,89). Recomendo cortar para desfile e estúdio, e deixar o lookdev como 'talvez'.

</details>

---

### VIS-27 — Fatia de beleza no tema favorito

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** G (3-5 sessões)

**Por que, e nesta fase:**

É a pergunta 'dá para chegar em cara de jogo atual da Steam com arte que existe?', que antes não tinha portão.

**Pronto quando:**

- Chão v1: conjunto PBR do bioma, ladrilho em escala real, borda natural da lane, lane plana em y = 0.
- Luz v1 (VIS-06): sol a 28-38°, HDRI alinhado, grading nativo no lugar de contraste 6 e saturação 4, névoa.
- Mata parcial na moldura.
- 1 torre e 2 bichos no tema, com arte real.
- Lado a lado com os jogos-régua, no mesmo ângulo, com p95 e triângulos.

---

### VIS-28 — Spike de estabilidade temporal e luz indireta

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

Grama e pelo com alpha-cutout cintilam com SMAA, e a luz vem só do skybox. É o maior teto de 'realista' que ainda não foi testado.

**Pronto quando:**

- A/B gravado de STP ou TAA (com motion vectors nos skinned) contra o SMAA atual, na grama e no pelo em movimento.
- A/B de APV ou reflection probe sobre a parte estática assada por script de Editor.
- Custo de p95 de cada um e recomendação registrada.

---

### VIS-02p — Protótipo de enquadramento: morre, foge ou cai exausto

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

A escolha muda torres, sons e animações (VIS-19, SOM-02, SOM-03), e precisa vir antes deles.

**Pronto quando:**

Um bicho com as variantes lado a lado no exe e o print. A decisão do Felipe é registrada.

---

### TEC-33 — Perfil de hardware mínimo (aviso)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Todo número de FPS sai da 4070 Ti. O nível Baixo não pode ser chute.

**Pronto quando:**

-captura -perfil-minimo, com renderScale, limite de quadro e qualidade reduzidos, simula o alvo escolhido. Medido também numa segunda máquina, se houver. Fica só no métricas.json, como aviso.

---

### MKT-02 — Hábito de captura para divulgação

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Wishlist se acumula com o tempo. A cada salto visual, deve sobrar material.

**Pronto quando:**

O -captura salva também um GIF ou MP4 curto de cada salto aprovado, numa pasta fora do Git, com backup. Uma pasta 'devlog' é iniciada.

---
