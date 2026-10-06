# Fase 6 — Loja e seleção de torres e bichos

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 5-7 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Comprar, selecionar e evoluir torre, e escolher e enviar bichos, viram cartões e painéis bonitos, com estrutura para mais conteúdo, abas e o Mercado 2.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Construir, selecionar, evoluir, vender e enviar só por cartões e painel, sem nenhuma ação destrutiva de um clique.
- Todas as unidades com retrato.
- Mais de 9 envios navegáveis.
- O Felipe aprova.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar cartões e painel contra os mockups.
- [ ] Revisar papel e frase de cada unidade.
- [ ] Seleção antes da partida (escolher o que levar) ou só dentro dela?

## Tarefas, em ordem

- [ ] VIS-25 — Retratos renderizados dos próprios modelos
- [ ] UX-16 — Ficha de apresentação por chave
- [ ] UX-15 — Painel da torre selecionada
- [ ] UX-12 — Loja de torres em cartões
- [ ] UX-13 — Mercado de envios em cartões

---

### VIS-25 — Retratos renderizados dos próprios modelos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 2 · prioridade 4
- **Depende de:** BUG-01, VIS-08

**Por que, e nesta fase:**

A loja precisa de rosto, e o projeto não tem nenhum ícone.

**O que é:**

Um estúdio com câmera 3/4, luz de 3 pontos e alfa renderiza cada torre nos 3 estágios e cada bicho em pose de passada, no boot (com cache) ou em batch de Editor sem -nographics. A silhueta de 'bloqueado' sai por tint. Um retrato vazio denuncia bicho invisível.

**Valor para o jogador:**

Loja e HUD com o rosto real das unidades, sem ícone feito à mão.

**Pronto quando:**

Estúdio em batch, sem PostFx, com fundo de alfa 0 e camada própria. Retratos versionados, com cobertura de alfa entre 15% e 80%.

**Como verificar:**

Folha de contato e cobertura de alfa entre 15% e 80% em cada retrato.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 2) A loja e a barra de envio bonitas precisam de rosto para cada unidade, e o projeto não tem nenhum ícone. Renderizar dos próprios modelos garante coerência com o que aparece no campo e se refaz sozinho quando o tema ou o modelo mudar. Isso vale ouro, já que a direção de arte mudou 3 vezes. Preferir o batch de Editor com resultado versionado ao render no boot, que é mais simples e não atrasa a entrada. O detector de bicho invisível é um bônus. Depende do BUG-01 e da escala (VIS-08).
- (logo; valor 4, custo 2, risco 2) É o melhor custo-benefício da lista para a loja. Não gasta arte, e se o tema ou os modelos mudarem basta renderizar de novo. São cerca de 27 renders (18 estágios de torre e 9 bichos), baratos de fazer no boot com cache. Pegadinha conhecida do URP: o pós-processo e a névoa estragam o alfa do RenderTexture, então a câmera do estúdio precisa ficar sem PostFx, com fundo de alfa 0 e numa camada própria. A pose de passada sai do Legacy Animation.Sample. Os bichos ficam travados pelo BUG-01. O retrato vazio serve de alarme, mas não substitui investigar o bug.

</details>

---

### UX-16 — Ficha de apresentação por chave

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 2 · risco 1,5 · prioridade 4
- **Depende de:** TEC-12, TEC-14

**Por que, e nesta fase:**

Sem papel, frase e números, o cartão bonito fica vazio.

**O que é:**

Catálogo só de vista, indexado por Name: papel, frase, tags, ícone e tier. Os números (DPS, payback, tempo de atrito até morrer) são calculados do catálogo. 'Bom contra' vem da matriz medida.

**Valor para o jogador:**

Entender cada unidade sem abrir o README.

**Pronto quando:**

Papel, frase, tags e tier. DPS, payback e tempo de atrito calculados do catálogo. 'Bom contra' do contras.txt. Teste: toda chave tem ficha.

**Como verificar:**

Teste exige ficha para todo Name, e os números derivados batem com a simulação.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 2) É a base de dados da loja e da seleção bonitas que o Felipe pediu. Sem papel, frase e números, o cartão bonito fica vazio. Calcular DPS e payback a partir do catálogo e testar que todo Name tem ficha é barato e impede ficha desatualizada. O 'bom contra' tirado da matriz medida é a parte cara e pode entrar depois: comece com papel e números.
- (logo; valor 4, custo 2, risco 1) É a base de dados da loja e da seleção bonitas que o Felipe pediu, e sem ela o cartão novo continua mostrando só nome e custo. Indexar por Name foge do acoplamento por índice e não toca a Sim, por isso o risco é baixo. DPS, payback e tempo de atrito saem do catálogo de forma barata. O 'bom contra' depende da matriz medida (TEC-14), que muda a cada rebalanceamento: na v1 vale texto escrito à mão, e a matriz entra depois.

</details>

---

### UX-15 — Painel da torre selecionada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2 · prioridade 4
- **Depende de:** UX-01, VIS-25

**Por que, e nesta fase:**

Hoje o clique evolui e o direito vende na hora. Selecionar torre foi pedido do Felipe.

**O que é:**

O clique passa a selecionar, sem evoluir nem vender na hora. O painel mostra o estágio, 6 marcas de nível, os atributos atual → próximo, os anéis fixos e o próximo modelo. Evoluir com U; vender segurando o botão. Espaço reservado para ramos, mira e reparo. Shift+clique continua evoluindo direto.

**Valor para o jogador:**

Acaba a venda acidental, e evoluir vira desejo.

**Pronto quando:**

- O clique seleciona; o painel mostra estágio, 6 marcas e atual → próximo.
- U evolui, vender exige segurar, Shift+clique evolui direto.
- A seleção sobrevive à venda de outra torre.
- 3 partidas sem venda acidental.

**Como verificar:**

A seleção sobrevive à venda de outra torre, e 3 partidas de playtest sem venda acidental.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 2) Hoje o botão direito vende na hora e o clique evolui sem perguntar, e isso causa erro caro em partida apertada. O painel é ainda a base obrigatória para 'mais evoluções' e ramos, um pedido do Felipe. Ver o próximo modelo da torre é o que transforma evoluir em desejo. Manter Shift+clique para quem quer velocidade está certo. Até o painel chegar, uma confirmação na venda em OnGUI resolve o pior por quase nada.
- (logo; valor 5, custo 3, risco 2) Hoje o clique evolui e o botão direito vende na hora. Isso é perda real de partida, e selecionar torre foi pedido do Felipe. A mudança é só de vista e entrada; a simulação não muda. O ponto delicado é a seleção sobreviver ao reindexamento quando outra torre é vendida (a semântica de índice do TowerSold), e o teste proposto vai bem nisso. O espaço reservado para ramos, mira e reparo deve ser só layout: nada disso existe na simulação e não pode virar código especulativo. Dá para fazer uma versão provisória antes do UX-01, mas o ideal é já nascer no UI Toolkit.

</details>

---

### UX-12 — Loja de torres em cartões

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3,5 · risco 2 · prioridade 4
- **Depende de:** UX-01, UX-16, VIS-25

**Por que, e nesta fase:**

Pedido explícito do Felipe.

**O que é:**

Cartões gerados a partir de TowerCatalog.Count, cada um com:
- retrato, custo e papel;
- barras de dano/s, alcance e fronteira;
- ícones de efeito e atalho.
Estados: disponível; 'faltam N' com barra; bloqueado; selecionado. A ficha abre no tooltip, arrastar o cartão constrói, e os atalhos não brigam com WASD.

**Valor para o jogador:**

Escolher torre pelo que ela faz, como o Felipe pediu.

**Pronto quando:**

Cartões por Count, com retrato, custo, papel, barras e efeitos. Estados disponível, 'faltam N' e selecionado. Prints com ouro baixo e alto.

**Como verificar:**

Teste do estado do cartão, e prints com ouro baixo e alto.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 2) O Felipe pediu isso explicitamente, e escolher torre pelo papel e pelos atributos é o que tira o jogo da cara de protótipo. Os cartões aparecem em todo print. Gerar a partir do TowerCatalog.Count prepara para mais torres. Depende de metadado no catálogo (descrição, papel), que hoje não existe, e de retrato (VIS-25). Cortar o 'arrastar o cartão constrói' na primeira versão: clicar no cartão e depois no chão é o padrão que todo mundo entende, e o arrastar fica para o mobile.
- (logo; valor 5, custo 4, risco 2) O Felipe pediu isto com todas as letras. Mas M é otimista: depende de três coisas não feitas (UX-01, UX-16 com metadado no catálogo, VIS-25 com retratos). Arrastar para construir, com Input legado e raycast saindo de um painel UI Toolkit, dá trabalho. O custo real é a iteração visual até o Felipe aprovar os prints. O risco para a simulação é baixo, porque é só leitura do TowerCatalog e envio de comandos que já existem. Para não parar tudo, dá para fazer a primeira versão sem arrastar e com retrato provisório.

</details>

---

### UX-13 — Mercado de envios em cartões

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 4 · risco 3 · prioridade 4
- **Depende de:** UX-01, UX-16, VIS-25, TEC-17

**Por que, e nesta fase:**

A decisão econômica central, pronta para mais bichos e o Mercado 2.

**O que é:**

Grade de cartões com:
- custo, '+renda' e payback;
- vida × quantidade já escalada;
- tags.
Shift envia ×5. Abas Mercado 1/2, com o cofre trancado mostrando a condição de abertura. O ícone voa até a lane inimiga, e a recusa faz o cartão tremer.

**Valor para o jogador:**

Enviar vira decisão econômica legível, pronta para mais bichos e para o segundo mercado.

**Pronto quando:**

- Grade por Count, com payback e vida escalada.
- Abas com teclas 1-9 dentro de cada aba, testadas com o catálogo de 10 envios do BUG-04.
- Shift ×5 prevendo recusa; recusa treme o cartão e mostra o motivo.

**Como verificar:**

Testes de payback e de vida escalada, e ×5 com ouro para 3 gera 3 comandos.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 4, risco 3) É a decisão econômica central do Tower Wars e foi pedida explicitamente. O payback ('se paga em 48 s') torna legível o dilema torre × envio × renda melhor que qualquer tutorial. O risco está na aba do Mercado 2: ela depende de um segundo mercado cuja simulação ainda nem existe. Fazer o Mercado 1 primeiro, com a grade preparada para abas, e só desenhar o cofre trancado quando a regra de abertura estiver testada no FlowSim. O ícone voando e o cartão tremendo são polimento barato e valem.
- (logo; valor 5, custo 4, risco 3) É pedido explícito e o centro do Line TW, mas o pacote mistura duas coisas. A grade do Mercado 1 (custo, renda, payback, vida escalada, Shift ×5) cabe agora em cima do UX-01. A aba do Mercado 2 com cofre depende de tier, estoque e desbloqueio, que não existem na simulação. Isso é design e simulação novos, com teste e balanceamento, não interface. Recomendo entregar o Mercado 1 e só reservar o espaço da aba. O ×5 precisa prever o aceite, porque o comando vale no próximo tique e pode ser recusado; o teste proposto cobre isso.

</details>

---
