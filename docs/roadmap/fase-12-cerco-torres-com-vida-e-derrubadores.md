# Fase 12 — Cerco: torres com vida e derrubadores

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 4-6 sessões
- **Créditos Meshy estimados:** 0-60 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Inimigo que ataca torre de verdade, como foi pedido. A fase não é condicional: o playtest da fúria só decide os parâmetros.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Sem atacante, metas inalteradas.
- Contrato do derrubador batendo e BalanceLab verde.
- O Felipe aprova.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Parâmetros do cerco (dano, vida, quem ataca), tirados do playtest da fúria.
- [ ] Comando Reparar (MERC-06) ou só Oficina.

## Tarefas, em ordem

- [ ] MERC-05 — Torre com vida e destruição
- [ ] MERC-08 — Derrubador (v1)
- [ ] VIS-20 — Ataque procedural (plano B)
- [ ] TORRE-12 — Oficina de reparo

---

### MERC-05 — Torre com vida e destruição

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2,5 · custo 3 · risco 3,5 · prioridade 2,5
- **Depende de:** TEC-17

**Por que, e nesta fase:**

Fundação do cerco.

**O que é:**

Hp por torre, crescendo por nível. Um RemoveTowerAt comum à venda e à destruição, e um evento TowerDestroyed com o mesmo índice do TowerSold. Barra de vida só quando danificada. Opcional: ruína que bloqueia 8 s e reconstrução pela metade. Sem atacante, nenhum número muda. É architectural.

**Valor para o jogador:**

Fundação dos inimigos que atacam torres.

**Pronto quando:**

Design architectural aprovado. Hp por nível e RemoveTowerAt comum à venda. Sem atacante, nenhuma meta muda. Destruir libera a célula e encolhe o território.

**Como verificar:**

Destruir libera a célula, o território encolhe, o evento sai com o índice certo e as metas ficam inalteradas.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 4) Sozinha, a vida da torre não muda nada para o jogador. Num TD de labirinto, perder a torre é punitivo e mexe no caminho e no território de uma vez. MERC-07 entrega quase a mesma sensação sem esse risco. Só vale a pena se o desligamento se mostrar fraco nos testes de jogo. Nesse caso, a torre desmoronando com destroços realistas seria um bom momento visual.
- (depois; valor 2, custo 3, risco 3) É uma fundação e, sozinha, o jogador não vê nada. É architectural de verdade: mexe no acoplamento por índice (LaneView/TowerSold), no replay e na IA. A ruína que bloqueia 8 s mexe no caminho do labirinto e sobe muito o risco, então deixar fora da v1. Só faz sentido quando houver um atacante concreto (MERC-08) logo em seguida. Antes disso, prefira o MERC-07, que entrega a sensação sem este custo.

</details>

---

### MERC-08 — Derrubador (v1)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 5 · risco 4 · prioridade 2,5
- **Depende de:** MERC-05, MERC-01, VIS-20, TORRE-01

**Por que, e nesta fase:**

Acaba o 'labirinto e esqueço'.

**O que é:**

Três comportamentos distintos:
- parar e roer: Castor, Hipopótamo, Texugo;
- investida de passagem: Elefante em fúria, Búfalo;
- arremesso à distância: Gorila, que prioriza a torre de maior nível.
Atrito baixo, para não morrer parado dentro da fronteira. A IA aprende a proteger torre.

**Valor para o jogador:**

O inimigo que ataca torre que o Felipe pediu, e acaba o 'labirinto e esqueço'.

**Pronto quando:**

Uma espécie com clipe de ataque da fonte única. Derruba um Canhão nv 1 sem fronteira em ~10 s e morre antes com a fronteira de 2 Canhões. A IA protege torre.

**Como verificar:**

O Castor derruba um Canhão nv 1 sem fronteira em ~10 s e morre antes com fronteira de 2 Canhões.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 4, custo 5, risco 4) No papel é exatamente o que o Felipe pediu, mas o custo escondido é a arte. São 5 bichos novos e realistas (castor, hipopótamo, texugo, búfalo, gorila), cada um com animação de roer, investir ou arremessar além de andar. O rig do Meshy hoje só entrega 'Andando'. Depende de 4 itens, entre eles a vida da torre. Faça só se MERC-07 provar que atacar torre é divertido, e comece com um único comportamento.
- (depois; valor 5, custo 5, risco 4) É o pedido explícito do Felipe e muda o 'labirinto e esqueço', mas é o item mais caro da lista. Pede 3 comportamentos de IA de bicho, até 6 animais novos com animação de ATAQUE (roer, investir, arremessar), que nem o Meshy quadrúpede nem a maioria dos CC BY do Sketchfab têm, além de 4 dependências e a IA aprendendo a proteger torre. O balanceamento de bicho parado dentro da fronteira contra o atrito é delicado. Cortar a v1 para 1 comportamento e 1 bicho (Elefante em fúria, que reaproveita o modelo). Outro ponto: Castor e Texugo não combinam com um tema de savana, e Hipopótamo e Gorila destoam de um tema temperado. Decidir o tema antes de escolher os bichos.

</details>

---

### VIS-20 — Ataque procedural (plano B)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 4 · prioridade 2,5
- **Depende de:** BUG-01, TEC-17

**Por que, e nesta fase:**

Só se nenhum modelo com ataque servir.

**O que é:**

Uma camada aditiva em LateUpdate move ossos achados pelo nome (mapa no bichos.json): investida, patada, pancada de tromba, bicada. O quadro do golpe dispara VFX, som e tremor da torre.

**Valor para o jogador:**

Bichos que atacam torres com leitura clara, sem gastar crédito.

**Pronto quando:**

Camada aditiva por osso, quadro do golpe com VFX e som. Aprovado por print.

**Como verificar:**

O log mede deslocamento ≥ 0,2 u na cabeça durante o golpe, e o Felipe aprova o print.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 3, custo 3, risco 4) Só existe se o segundo mercado, com bichos que atacam torres (TEC-17), for validado como divertido, e isso ainda não foi. Investida e patada aditivas por osso em 9 esqueletos de fontes diferentes tendem a sair duras e esquisitas, justamente o que denuncia o 'não realista'. Nenhum dos 9 tem clipe de ataque no bichos.json. Antes, procurar bichos com ataque animado pronto ou a biblioteca de animação do Meshy (perguntando antes, por créditos). O procedural fica como último recurso.
- (depois; valor 3, custo 3, risco 4) Depende do TEC-17 (torre com vida e bicho que ataca, o segundo mercado). Isso é mudança architectural na Sim e ainda nem tem design validado. Ataque procedural por camada aditiva em 9 rigs diferentes (nomes de osso do Sketchfab variam, e o rig de quadrúpede do Meshy é outro) costuma parecer robótico, o que bate de frente com o 'realista'. A verificação ≥0,2 u mede que a cabeça se moveu, não se o golpe ficou bom. Antes, procurar modelos que já tragam clipe de ataque. O procedural fica como plano B.

</details>

---

### TORRE-12 — Oficina de reparo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2 · risco 2 · prioridade 2,5
- **Depende de:** MERC-05

**Por que, e nesta fase:**

Contra ao derrubador (volta do estacionamento).

**O que é:**

Cura 3% da vida máxima por segundo das torres num raio de 2,5. Resposta anti-cerco.

**Valor para o jogador:**

Contra-jogo aos derrubadores.

**Pronto quando:**

Cura no raio. Uma torre ao lado dela sobrevive a um derrubador que a derrubaria sem ela.

**Como verificar:**

Uma torre ao lado da Oficina sobrevive a um derrubador que derrubaria sem ela.

<details><summary>Parecer dos avaliadores</summary>

- (talvez; valor 2, custo 2, risco 2) Só existe se MERC-05 (bicho que ataca torre) existir e se mostrar divertido. Torre de cura é passiva e pouco visível para o jogador. Um botão 'Reparar' pago no painel de seleção da torre resolve o mesmo contra-jogo sem gastar célula, modelo, ícone nem termo de IA. Decidir só depois de jogar com derrubadores.
- (depois; valor 2, custo 2, risco 2) Não existe sem o MERC-05: hoje a torre não tem vida, então não há o que curar. Quando houver derrubadores, a aura de cura é um laço simples e a arte é um prédio estático. Deve sair junto com o MERC-05 como contra-jogo, não antes. O valor sozinho é baixo, porque é uma torre reativa.

</details>

---
