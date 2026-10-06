# Fase 11 — Segundo mercado completo: elites e chefe

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 4-6 sessões
- **Créditos Meshy estimados:** 0-90 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

O Mercado 2 ganha elites e o primeiro chefe, cada mecânica numa espécie distinta e legível na câmera tele.

## Critério de saída (a fase só fecha com tudo isto verificado)

- BalanceLab verde com elites e chefe.
- Replay reproduz.
- O Felipe aprova 3 partidas.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Quais elites entram.
- [ ] Créditos para modelos sem opção pronta.

## Tarefas, em ordem

- [ ] DES-23b — Meta de elenco recalibrada
- [ ] BICHO-09 — Elite imune a controle (espécie própria)
- [ ] MERC-04 — Alfas que passam no teste cego
- [ ] MERC-11 — Chefe: Elefante ancestral (único papel do Elefante)

---

### DES-23b — Meta de elenco recalibrada

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

O inventário real e o Portão 2 mudam os números.

**Pronto quando:**

Meta final no ROADMAP. Métricas de elenco no BalanceLab, por exemplo pelo menos 6 tipos com 5% ou mais das compras.

---

### BICHO-09 — Elite imune a controle (espécie própria)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** BICHO · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2,5 · risco 2 · prioridade 3
- **Depende de:** BUG-01

**Por que, e nesta fase:**

Pune a defesa só de Gelo e Ar.

**O que é:**

Ignora lentidão, congelamento e empurrão. Pune defesa só de Gelo e Ar.

**Valor para o jogador:**

Obriga a ter dano bruto na defesa.

**Pronto quando:**

Espécie definida no DES-23, com modelo buscado ativamente. Ícone de 'imune'; o Gelo não muda o DeepestXWith dela.

**Como verificar:**

O Gelo não muda o DeepestXWith dele.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 2) A regra é simples e clara, e o contra é óbvio (dano bruto), coerente com a regra de que cada torre responde a um envio. Para o jogador, é fácil de entender com um ícone de 'imune'. Bisão realista animado é razoavelmente fácil de achar. Daria para lançar antes como propriedade do Elefante, se faltar modelo. Valor médio: pune a defesa só de controle, mas não cria cena nova.
- (depois; valor 3, custo 3, risco 2) Na Sim é trivial: três flags que pulam lentidão, congelamento e empurrão. É um contra-jogo saudável para a defesa só de controle. O custo é o modelo de bisão animado. Dá para fazer barato como traço de elite de um colosso que já existe (Elefante). A morte súbita já desliga congelar e empurrar, então o ganho de design é menor do que parece.

</details>

---

### MERC-04 — Alfas que passam no teste cego

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3
- **Depende de:** MERC-01, BICHO-05

**Por que, e nesta fase:**

Elite barata de arte, mas só se for legível.

**O que é:**

Elite sem modelo novo (escala maior, pelagem escura, anel e coroa):
- Lobo-alfa acelera a matilha;
- Urso-alfa entra em fúria abaixo de 50%;
- Tigre-alfa dá bote e sai da mira;
- Rinoceronte-alfa é blindado.

**Valor para o jogador:**

Primeiro conteúdo de elite barato de arte.

**Pronto quando:**

Lobo-alfa primeiro, sem coroa (escala e pelagem). Entra só se acertar 8 de 10 no teste cego na câmera tele. Elite em no máximo 25% das compras.

**Como verificar:**

Um contrato por alfa e a elite em ≤ 25% das compras.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) É a elite mais barata de arte se o segundo mercado for aprovado, mas 'coroa e anel' brigam com a direção realista. Use escala, pelagem escura, cicatrizes e porte. O Lobo-alfa e o Rinoceronte-blindado se leem bem. O 'Tigre dá bote e sai da mira' é confuso de ver e mexe com o código de mira. Só faz sentido depois de MERC-01 existir.
- (depois; valor 3, custo 3, risco 2) A arte é barata: escala, tinta no material e anel, nos modelos que já existem. Cada comportamento, porém, é campo novo no struct, no CatalogJson, na assinatura do Replay, na IA e num teste. O bote do tigre que sai da mira é o mais caro e arriscado, por causa de projétil em voo com alvo inválido. A blindagem e a fúria são fáceis. Também pressupõe o bug dos bichos invisíveis resolvido e o MERC-01 pronto. É o primeiro conteúdo do 2º mercado, mas não antes do visual base.

</details>

---

### MERC-11 — Chefe: Elefante ancestral (único papel do Elefante)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 4 · risco 3 · prioridade 3
- **Depende de:** MERC-01, DES-04, TORRE-08

**Por que, e nesta fase:**

O clímax e o momento de trailer.

**O que é:**

Um por jogador por partida, a partir de 5:00, custando 5-6 vidas se vazar. Imune a congelar e a empurrão, com aviso 'chefe a caminho' e barra no topo. Opções:
- Elefante ancestral, com pisoteio opcional;
- Leão-de-juba-negra: rugido atordoa torres e chama leoas aos 50%;
- Rinoceronte-negro: armadura que cai a cada 10 golpes.
Teto de queima.

**Valor para o jogador:**

O clímax e o momento de trailer.

**Pronto quando:**

Um por jogador, a partir de 5:00, custando 5-6 vidas. Imune a congelar, com barra de chefe. Decide no máximo 30% das partidas.

**Como verificar:**

Imune a congelar e empurrar, o Fogo sozinho não o mata em menos de 15 s, e ele decide no máximo 30% das partidas.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) Para a Steam é o que mais vende: dá o momento de trailer e a barra de chefe no topo. Comece pelo Elefante ancestral e pelo Rinoceronte-negro, que reaproveitam modelos que já existem (maiores, mais escuros, com cicatrizes). O Leão pede modelo e animações novos. O risco é decidir partidas demais, e a verificação de no máximo 30% cobre isso. Rugido que atordoa torres depende de MERC-07.
- (depois; valor 4, custo 4, risco 3) É clímax e material de trailer. O Elefante ancestral sai barato de arte (modelo atual escalado, com imunidades em flag) e deve ser o único da v1. O Leão de juba negra exige modelo novo, rugido, invocação de leoas e animação. A armadura que cai a cada 10 golpes pede contador por golpe. Exige barra de chefe e aviso na UI nova, e a meta '≤30% das partidas decididas' consome rodadas de BalanceLab. Vem depois do MERC-01, da UI e do som, porque chefe sem som e sem barra bonita não é clímax.

</details>

---
