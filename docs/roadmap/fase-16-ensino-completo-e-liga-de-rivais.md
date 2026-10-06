# Fase 16 — Ensino completo e liga de rivais

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 7-9 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Quem nunca jogou entende a tese em minutos, e quem joga sozinho ganha objetivo.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Tutorial e escada de rivais verdes.
- O Felipe aprova.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Roteiro do tutorial e falas dos rivais.

## Tarefas, em ordem

- [ ] UX-23 — Primeira partida guiada completa
- [ ] META-07 — Liga de rivais nomeados

---

### UX-23 — Primeira partida guiada completa

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 4,5 · risco 3 · prioridade 3
- **Depende de:** TEC-15, UX-05, UX-12, UX-13, TEC-17

**Por que, e nesta fase:**

Estende a versão mínima.

**O que é:**

Treino com uma IA roteirizada (comandos por tique). Os passos esperam condições, não tempo: construir, ver a fronteira e o primeiro abate por atrito, enviar e ver a renda, evoluir, responder à Águia, aviso da morte súbita. Dicas não-modais com Pular.

**Valor para o jogador:**

Entender a tese e o triângulo torre × envio × renda em minutos.

**Pronto quando:**

Evoluir, responder à Águia, Mercado 2 e morte súbita. Headless no FlowSim. Uma pessoa nova conclui sem ajuda.

**Como verificar:**

FlowSim roda o tutorial headless até o fim, e alguém novo conclui sem ajuda.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 5, custo 4, risco 3) Line Tower Wars com atrito territorial não é óbvio, e sem guia o jogador novo não entende a tese nem o triângulo torre × envio × renda, o que pesa muito na retenção e nas análises da Steam. Só que um tutorial feito antes de a UI, os ícones e o elenco estabilizarem é reescrito do zero. A IA roteirizada por tique e o teste headless no FlowSim são o jeito certo. Tem que estar pronto antes de qualquer demo pública.
- (depois; valor 4, custo 5, risco 3) Ensinaria a tese, mas é caro e frágil no momento atual. Depende de quatro itens que ainda não existem (TEC-15, UX-05, UX-12 e UX-13), de uma IA roteirizada e de um roteiro preso às regras. O cronograma ainda vai trazer mais torres, mais evoluções, um segundo mercado e bichos que atacam torres, e cada mudança invalida o tutorial. A pergunta central (cercar com fronteira tem graça?) também segue sem resposta. Fazer quando o design congelar. Até lá, UX-24 e UX-16 ensinam por bem menos. Rodar headless no FlowSim é um bom critério.

</details>

---

### META-07 — Liga de rivais nomeados

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 4 · risco 2,5 · prioridade 3
- **Depende de:** TEC-15, UX-01

**Por que, e nesta fase:**

Objetivo solo e IA com personalidade.

**O que é:**

Oito rivais, cada um com Personality, pesos de assinatura, falas e um emblema (não rosto). A liga tem dificuldade crescente, com mapa e clima por rival e um relatório do rival depois da derrota. Desbloqueia conteúdo, nunca poder.

**Valor para o jogador:**

IA com personalidade e objetivo para um jogador.

**Pronto quando:**

8 rivais com variação de luz e clima no mapa único. O rival k vence o k−1 em pelo menos 60%. Desbloqueia conteúdo, nunca poder. Salvo no SaveStore.

**Como verificar:**

No BalanceLab, o rival k vence o k−1 em ≥ 60%.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) Para um jogo que na prática é solo contra IA, uma liga de rivais com estilo próprio é o que dá objetivo e progressão. A Personality já existe no TowerWarsAi, o que barateia a parte de IA, e o critério de o rival k vencer o k-1 em 60% é bom. O caro é o resto: falas, emblemas e principalmente um mapa e clima por rival em arte realista, o que multiplica o cenário. Comece com 1-2 mapas e varie a luz e o clima.
- (depois; valor 4, custo 4, risco 2) A Personality já existe com 5 parâmetros e o BalanceLab mede quem vence quem, então 8 rivais escalonados (k vence k−1 em ≥ 60%) é viável em código. Os 'pesos de assinatura' por envio ainda não existem na IA. O que encarece é o 'mapa e clima por rival': no padrão realista, cada mapa é trabalho de arte e de desempenho, e o projeto ainda não tem nem um cenário bonito. Mais tarde, só com rivais e falas sobre um ou dois mapas, o custo cai para 3.

</details>

---
