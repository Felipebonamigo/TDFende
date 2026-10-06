# Fase 9 — Fatia do conteúdo pedido

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 3-4 sessões
- **Créditos Meshy estimados:** 0-60 (só se o bicho novo não tiver modelo pronto) _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Antes do Portão 2, os testadores precisam ver o diferencial: a aba do Mercado 2 aberta, o primeiro inimigo que ataca torre, um bicho realmente novo e uma torre nova. A regra já vem pronta da Trilha S.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Exe com Mercado 2, fúria e torre nova.
- BalanceLab verde.
- O Felipe joga 3 partidas.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Aprovar o bicho novo (modelo, licença, créditos).

## Tarefas, em ordem

- [ ] MERC-01 — Mercado 2 aberto por tempo
- [ ] MERC-07 — Fúria que desliga torre (bicho novo)
- [ ] TORRE-08 — Primeira torre nova: anti-pesado tier 2

---

### MERC-01 — Mercado 2 aberto por tempo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 4 · risco 4 · prioridade 3
- **Depende de:** DES-04, BUG-04, UX-13, TEC-12

**Por que, e nesta fase:**

Pedido do Felipe: um meio de partida com cara própria.

**O que é:**

Campo Tier em SendUnit e TowerType.
Variante A: abre por tempo (3:00), sem comando novo.
Variante B: abre por investimento, com o comando Expandir de 150-250 de ouro e uma obra de 25-30 s visível ao adversário, que pode punir a janela.
Opcional: tier 3 aos 6:00. A elite tem pouca renda por ouro: é pressão, não investimento.

**Valor para o jogador:**

Um meio de partida com cara própria e uma decisão grande.

**Pronto quando:**

Aba com cofre e contagem até 3:00, anel de recarga da elite (MERC-02). Envio tier 2 é recusado antes e aceito depois.

**Como verificar:**

Envio tier 2 é recusado antes e aceito depois, o replay com 'expand' reproduz e o Difícil segue acima do Fácil.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 4) O Felipe pediu explicitamente, e um meio de partida com cara própria vende bem na página da Steam. Mas hoje não existe nenhuma unidade nem torre de elite para encher o mercado, nem UI que mostre isso. O mercado sem conteúdo é só um cadeado. Sugestão: validar primeiro a Variante A (por tempo, só um campo Tier e um Check), com 3 ou 4 elites prontos, e só fazer a B (Expandir com obra visível) se o playtest mostrar meio de jogo morno. A IA ainda precisa aprender quando expandir, e esse é o risco maior.
- (depois; valor 4, custo 4, risco 4) É pedido explícito do Felipe e dá cara própria ao meio da partida, mas tem 4 dependências (UX-13, TEC-12, DES-04, BUG-04). O custo de verdade é encher o mercado: cada envio ou torre de elite realista precisa de modelo animado, licença ou créditos Meshy. Sem conteúdo bom, é um cadeado. A variante B mexe em comando, replay e IA, e a IA precisa saber quando expandir, senão a ordem Difícil > Fácil quebra. Começar pela variante A (por tempo, sem comando) como teste barato.

</details>

---

### MERC-07 — Fúria que desliga torre (bicho novo)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** MERC · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 3 · prioridade 3
- **Depende de:** MERC-01

**Por que, e nesta fase:**

O primeiro inimigo que ataca torre, sem precisar de vida de torre.

**O que é:**

- Sabotador (ex.: Gorila): para 1 s e desliga uma torre por 5 s, sem tiro e sem fronteira.
- Rinoceronte em fúria: investe a cada 5 s e atordoa a torre mais próxima por 2,5 s, sem parar de andar.
Congelado não sabota. A torre apagada fica óbvia: luz desligada e fumaça.

**Valor para o jogador:**

80% da sensação do cerco com 20% do trabalho.

**Pronto quando:**

Búfalo ou bisão, conforme o bioma, investe e atordoa a torre mais próxima. A torre apagada fica óbvia (luz e fumaça) e sai do território.

**Como verificar:**

A torre desligada não dispara e sai do território, e o bicho congelado não sabota.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 3, risco 3) É o melhor primeiro passo para o 'inimigo que ataca torre' que o Felipe pediu. Não muda o modelo de dados e conversa com a tese, porque a torre desligada perde a fronteira. Dá para começar só com o Rinoceronte em fúria, que já tem modelo. O Gorila exige modelo novo e uma animação de arremesso ou soco, e o quadrúpede do Meshy só tem 'Andando'. A torre apagada, com luz desligada e fumaça, combina com o visual realista. Fica para depois do visual, da UI e do som.
- (depois; valor 4, custo 3, risco 3) É o melhor primeiro passo para o 'inimigo que ataca torre': dispensa vida de torre, ruína e reparo. A Sim já tem RebuildTerritory, então desligar é uma flag por torre que sai do território e não atira. Mas mexer no território toda vez muda o atrito e as metas do FlowSim. Começar SÓ pelo Rinoceronte em fúria, que reaproveita o modelo e cuja investida é velocidade, não animação nova. O Gorila sabotador exige modelo novo e animação de ataque, e o rig quadrúpede do Meshy só tem 'Andando'. A fumaça de torre apagada exige VFX melhor que as bolhas de hoje. Vem depois do MERC-01 e da passada visual e de UI.

</details>

---

### TORRE-08 — Primeira torre nova: anti-pesado tier 2

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TORRE · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** TORRE-01, TEC-12

**Por que, e nesta fase:**

Resposta ao Mercado 2, e prova de que o elenco cresce.

**O que é:**

Alcance longo, tiro lento e forte, mira 'mais forte', ignora armadura e tem bônus contra elite e chefe. Ruim contra enxame. Balista, arpão ou fuzil conforme o tema.

**Valor para o jogador:**

Resposta clara aos pesados e ao segundo mercado.

**Pronto quando:**

3 estágios do kit. Com o mesmo ouro, mata o Elefante antes do Canhão e perde para os Ratos.

**Como verificar:**

Com o mesmo ouro, mata o Elefante antes do Canhão e perde para os Ratos.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) É a resposta natural ao segundo mercado (elite, armadura, chefe). Fuzil, balista ou arpão rendem um modelo realista de ótima leitura. Sem o segundo mercado, se sobrepõe ao Fogo, que já é 'resposta ao gordo', e vira mais uma opção redundante na loja. Deve entrar junto com os pesados caros, não antes.
- (depois; valor 3, custo 3, risco 3) O nicho de design é claro, mas o alvo dele (elite, chefe, armadura, segundo mercado) ainda não existe. Hoje o Elefante já é respondido por Gelo mais atrito, e a regra do projeto é 'cada torre responde a um envio'. Na Sim é barato: a mira 'mais forte' vem do TORRE-01 e o bônus é um multiplicador. O custo real são 3 modelos realistas, o projétil e o tema. Entra junto com o segundo mercado, não antes.

</details>

---
