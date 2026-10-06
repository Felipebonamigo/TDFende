# Fase 13 — Mais evoluções, torres novas e trunfos

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 5-7 sessões
- **Créditos Meshy estimados:** 0-90 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

'Mais evoluções' com escolha de verdade, nas torres e nos envios, e mais ferramentas.

## Critério de saída (a fase só fecha com tudo isto verificado)

- BalanceLab verde, sem dupla dominante.
- Replays antigos lidos.
- O Felipe aprova.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Quais torres ganham ramos primeiro.
- [ ] Papel da torre tier 2.
- [ ] Créditos para peças de ramo.

## Tarefas, em ordem

- [ ] TORRE-07 — Evolução em ramos
- [ ] MERC-14 — Melhoria de envio
- [ ] TORRE-05 — Torre de apoio com aura
- [ ] TORRE-13 — Torre tier 2 cara (cerco ou fortificada)
- [ ] TORRE-04 — Marco de fronteira (se o Portão 1 confirmou a tese)
- [ ] TORRE-06 — Reações entre torres (2 primeiras)
- [ ] DES-10 — Trunfos do comandante

---

### TORRE-07 — Evolução em ramos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 5 · risco 4 · prioridade 3
- **Depende de:** UX-15, TORRE-01, TEC-14

**Por que, e nesta fase:**

Pedido explícito.

**O que é:**

No nível 3 ou 4 o jogador escolhe o ramo A ou B; opcionalmente há especialização no 5 ou 6. Os ramos só usam multiplicadores sobre campos que já existem. Comando Upgrade(x, y, ramo) com argumento opcional. A IA escolhe pela leitura de ameaça. Na v1, peças procedurais. Começar por 2 torres.

**Valor para o jogador:**

'Mais evoluções' com escolha de verdade.

**Pronto quando:**

Começa por 2 torres, e a meta do EA é as 6. Upgrade(x, y, ramo) com argumento opcional, IA escolhe o ramo, peças do kit, contrato por ramo. Replays antigos são lidos.

**Como verificar:**

Um contrato por ramo na matriz, e o replay antigo 'upgrade x y' continua lendo.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 5, risco 4) É o pedido explícito de 'mais evoluções' na forma que os jogadores conhecem (Kingdom Rush, Bloons): escolha de verdade. É também o item mais caro: sim, replay, IA, painel de seleção e arte por ramo. 'Peças procedurais na v1' contradiz a direção realista, porque um ramo que parece maquete ao lado de uma torre realista piora o jogo. Precisa do UX-15 e da mira (TORRE-01) antes. Começar por 2 torres está certo.
- (depois; valor 4, custo 5, risco 4) É o que o Felipe pediu, 'mais evoluções' com escolha de verdade, mas o tamanho é G mesmo. Pede campo de ramo no struct, CatalogJson, Upgrade com argumento no comando e no replay, IA escolhendo o ramo e o painel de seleção (UX-15). A matriz de balanceamento dobra a cada torre. 'Peças procedurais na v1' contradiz a direção realista: a torreta de código em cima do modelo baixado já é o ponto fraco visual. Para ser distinguível, cada ramo multiplica modelos. Fazer depois da UI e do tema, começando por 2 torres.

</details>

---

### MERC-14 — Melhoria de envio

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

'Mais evoluções' do lado dos bichos, um clássico do Line Tower Wars.

**Pronto quando:**

Pesquisa de +vida, +velocidade ou traço por tipo, com custo crescente. Teste visto falhar, assinatura e BalanceLab verde. Painel no mercado.

---

### TORRE-05 — Torre de apoio com aura

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 3

**Por que, e nesta fase:**

A posição passa a importar.

**O que é:**

Estandarte ou Posto de Vigia: não atira e dá +cadência (opcionalmente +alcance) às torres num raio, sem acumular com outra igual. O bônus só é recalculado quando as torres mudam. Pode revelar camuflados e forçar o escavador a emergir.

**Valor para o jogador:**

A posição importa além do labirinto, e nasce uma peça-chave a proteger.

**Pronto quando:**

+cadência no raio, sem acumular. 3 Canhões + apoio causam mais dano que 4 Canhões. A IA aprende a posicionar.

**Como verificar:**

3 Canhões + apoio causam mais dano que 4 Canhões, e duas auras não acumulam.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Aura de apoio é um padrão comprovado do gênero, cria uma peça-chave e dá motivo para a posição. Em tema realista vira um bom modelo (estandarte, mastro de rádio, posto de vigia). O risco de balanceamento é controlável, porque não acumula e só recalcula quando as torres mudam. Revelar camuflados depende de bichos que não existem. Fica na segunda leva de torres, depois de UI e visual, porque cada torre nova custa 3 modelos e um ícone de loja.
- (depois; valor 3, custo 3, risco 3) A Sim é tratável: o multiplicador é recalculado quando as torres mudam e não acumula. O caro está em volta. A IA precisa escolher ONDE pôr a aura, uma heurística de posicionamento que ela não tem, e o jogador precisa ver o raio e o bônus numa UI que ainda é OnGUI. 'Revelar camuflado/escavador' depende de inimigos que não existem: cortar até eles existirem. Mais 3 modelos que dependem do tema.

</details>

---

### TORRE-13 — Torre tier 2 cara (cerco ou fortificada)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

O Felipe pediu torres mais caras, não só inimigos.

**Pronto quando:**

Papel definido no DES-23, 3 estágios do kit, contrato na matriz.

---

### TORRE-04 — Marco de fronteira (se o Portão 1 confirmou a tese)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 4 · prioridade 2,5

**Por que, e nesta fase:**

A tese na forma pura.

**O que é:**

Barata (~25-35), sem tiro, com a maior fronteira do jogo (3,5-4), que cresce por nível. Fraca contra voador. A IA ganha um termo de 'cobertura nova por ouro'. Nome conforme o tema: marco, posto de vigia ou poste de cerca.

**Valor para o jogador:**

A tese na forma mais pura, como escolha de compra.

**Pronto quando:**

Nunca dispara. Marco + Canhão vencem 2 Canhões contra Ratos. Não vira estratégia dominante no BalanceLab.

**Como verificar:**

Nunca dispara; Marco + Canhão vencem 2 Canhões contra Ratos; a IA compra em ≥ 5% das partidas.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 4) No papel é a tese pura. Na prática é uma torre que não atira, sem tiro nem impacto, a peça mais sem graça de ver num jogo que precisa vender pelo visual. Com atrito de 0,19 da vida máxima por segundo, uma fronteira de raio 4 barata arrisca virar estratégia degenerada. Também custa 3 modelos realistas novos. A pergunta central ('cercar tem graça?') ainda não tem resposta de playtest. Decida depois dela, e prefira o TORRE-03 como forma barata de dar mais território.
- (depois; valor 3, custo 3, risco 4) É a tese na forma pura, mas a pergunta que a sustenta, se cercar com fronteira tem graça (TESTE.md), nunca foi respondida: primeiro o playtest. Há risco de degenerar: fronteira de 3,5-4 por 25-35 de ouro, com atrito de 0,19/s, pode virar a resposta dominante. Dano zero no laço de tiro também pede uma guarda. Cada torre nova traz: 3 modelos realistas (níveis 1-2, 3-4 e 5-6), retrato, slot na loja e acréscimo por índice em ModelLib, Vfx, ProjectileView e ModelLibTiers. O modelo ainda depende do tema (VIS-01). Estimar como M é otimista.

</details>

---

### TORRE-06 — Reações entre torres (2 primeiras)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 4 · risco 4 · prioridade 3
- **Depende de:** TEC-17

**Por que, e nesta fase:**

Composição e 'eureca'.

**O que é:**

Combos que leem o estado do bicho:
- Estilhaçar: canhão ou morteiro em alvo congelado;
- Choque térmico: fogo em alvo congelado;
- Avivar: o Ar espalha a queima;
- Derrubar: o Ar tira a imunidade do voador ao atrito;
- Frio na fronteira;
- Incêndio em área.
Rótulo na primeira vez e glossário. A IA ganha um termo de complemento.

**Valor para o jogador:**

A defesa vira composição, com momentos de 'eureca'.

**Pronto quando:**

Estilhaçar e Avivar com VFX e rótulo. Nenhuma dupla domina.

**Como verificar:**

Gelo + Canhão mata o Elefante mais rápido que 2 Canhões do mesmo ouro, e nenhuma dupla domina o BalanceLab.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 4) Combos dão profundidade e momentos de 'eureca', e é o que faz voltar a jogar. Mas seis reações de uma vez são uma explosão combinatória para o BalanceLab. E sem VFX legível, hoje os efeitos são bolhas sem textura, o jogador nem percebe que a reação aconteceu. Comece por duas que já se leem sozinhas: Estilhaçar (gelo + impacto) e Derrubar (Ar contra voador). Só depois do pacote de VFX.
- (depois; valor 3, custo 4, risco 4) Ler Frozen/Burning no HitEnemy é fácil; o caro é tudo em volta. São 6 reações com balanceamento combinatório, e o critério 'nenhuma dupla domina' tende a ficar vermelho por semanas. Cada reação precisa de um efeito visual legível, e os VFX hoje são bolhas sem textura. Rótulo e glossário pedem uma UI que não existe. Sem leitura visual, o 'eureca' vira um número que o jogador não vê. 'Derrubar' mexe na única regra que justifica a Sentinela. Se for fazer, começar por 2 reações (Estilhaçar e Avivar), depois de TEC-17 e da UI nova.

</details>

---

### DES-10 — Trunfos do comandante

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 2,5 · prioridade 3,5
- **Depende de:** UX-19

**Por que, e nesta fase:**

O momento 'salvei a partida'.

**O que é:**

Duas habilidades ativas por partida, com recarga longa e custo em ouro: Bombardeio em área (sobre a vida-base × escala) e Alerta (atrito dobrado por 6 s). Depois, uma Barricada temporária. A IA usa os trunfos acima de um limiar de perigo.

**Valor para o jogador:**

O momento 'salvei a partida'.

**Pronto quando:**

Bombardeio e Alerta, com recarga e custo, mirados por clique. A IA usa acima de um limiar. As viradas sobem sem quebrar Difícil > Fácil.

**Como verificar:**

Recarga respeitada, e a taxa de viradas sobe sem quebrar Difícil > Fácil.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) Tem respaldo no gênero: o reforço e a chuva de fogo do Kingdom Rush são parte grande da diversão. Dá ao jogador o momento de agência e de virada, e o bombardeio é vitrine de VFX para o trailer. É uma extensão limpa na Sim (comando novo + recarga) e a IA usa por limiar. Entra junto com o HUD novo (UX-19), não em OnGUI.
- (depois; valor 4, custo 3, risco 3) A Sim é tranquila: comando novo pelo fluxo CommandKind→Replay→Apply já mapeado, recarga e limiar na IA. O custo escondido está na sensação. Bombardeio sem VFX bom é inútil, e os efeitos de hoje são bolhas sem textura. A mira precisa funcionar com toque (UX-19). Combina com um tema moderno ou realista (ataque de artilharia). Só vale depois de UI e VFX decentes.

</details>

---
