# Fase 3 — Tese à vista e Portão 1 (antes da UI)

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 3-4 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

A fronteira com atrito fica impossível de não ver, com render no mundo e sem UI Toolkit. A pergunta 'cercar com fronteira tem graça?' é respondida com A/B cego e com gente além do Felipe, sobre o núcleo já afiado pela Trilha S.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Diário com o A/B cego e as notas.
- Critério numérico avaliado e decisão no README.
- BalanceLab verde com o núcleo da Trilha S.
- Exe com fantasma, fronteira viva, leitura dos bichos, pausa e velocidade.
- Velocidade recalibrada.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] A fronteira com atrito diverte? Se não, raio, atrito, TORRE-04 e MERC-10 são replanejados. O conteúdo pedido (mercado, ataque a torres) continua, com outros parâmetros.
- [ ] Ajustes de BorderRadius e de atrito.
- [ ] Quem são as 2-3 pessoas do A/B.

## Tarefas, em ordem

- [ ] DOC-01 — Reescrever o TESTE.md
- [ ] TEC-18 — Entrada só por intenções
- [ ] UX-03 — Pausa e velocidade
- [ ] UX-05 — Fantasma de construção tático (enxuto)
- [ ] VIS-16 — Fronteira viva (versão legível)
- [ ] UX-18 — Leitura dos bichos
- [ ] TEC-19 — Diário automático de partidas
- [ ] DES-03 — Portão 1: A/B cego

---

### DOC-01 — Reescrever o TESTE.md

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** P (menos de 1 sessão)

**Por que, e nesta fase:**

Ele fala em 6 envios, 4 torres, teclas 1-6 e 'javali feito em código', e mesmo assim serve de base ao Portão 1 e ao META-01.

**Pronto quando:**

Protocolo atual: controles, 9 bichos, 6 torres e as perguntas da tese com escala de nota. Revisado pelo Felipe.

---

### TEC-18 — Entrada só por intenções

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 1 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Botão e tecla precisam disparar a mesma ação, e a pausa precisa existir antes do playtest. Também prepara toque e controle.

**O que é:**

Tirar do TowerWarsController as leituras diretas de F9, Q/E, 1-9 e R. Elas viram intenções: Pause, SpeedCycle, SelectTower, SendSlot, Confirm, Cancel e FocusLane.

**Valor para o jogador:**

Base para toque, controle, versus local e atalhos remapeáveis.

**Pronto quando:**

F9, Q/E, 1-9 e R saem do TowerWarsController e viram Pause, SpeedCycle, SelectTower, SendSlot (por aba), Confirm, Cancel e FocusLane. Um grep não acha Input.GetKey fora do DesktopInput.

**Como verificar:**

grep sem Input.GetKey fora das implementações de IGameInput, e o -captura continua passando.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 2, custo 1, risco 1) O jogador não percebe sozinho, mas a HUD nova em UI Toolkit (botões de pausa, velocidade, envio) vai precisar disparar as mesmas ações que o teclado. Sem isso, a lógica fica duplicada. São só 4 leituras diretas no TowerWarsController, então é barato. Fazer junto com a troca do OnGUI, não antes.
- (logo; valor 2, custo 1, risco 1) É barato: só 4 leituras diretas no TowerWarsController, e o DesktopInput já existe. Deve ser feito junto com a troca do OnGUI por UI Toolkit, para que os botões novos chamem intenções e não simulem tecla. Pausa e Esc são comportamentos novos, não só refatoração. Sozinho, o jogador não percebe nada. O valor aparece no mobile e no controle, que ainda estão longe.

</details>

---

### UX-03 — Pausa e velocidade

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 4 · custo 1 · risco 1 · prioridade 5
- **Depende de:** TEC-18

**Por que, e nesta fase:**

Um R acidental ou um alt-tab perde a partida e sabota o playtest.

**O que é:**

- Esc/P pausa: a sim para na fronteira do tique.
- Velocidade 1x/2x/3x contra a IA.
- R pede confirmação.
- Pausa automática ao perder o foco.
- Tremor zerado na pausa.

**Valor para o jogador:**

Acaba a partida perdida por tecla acidental ou alt-tab.

**Pronto quando:**

- Esc/P pausa na fronteira do tique, e 1x/2x/3x contra a IA, com o teto de tiques por quadro revisto.
- R pede confirmação; pausa automática ao perder o foco.
- Teste: TickCount parado na pausa e fingerprint quantizado igual em 1x e 3x.

**Como verificar:**

Teste: os mesmos comandos em 1x e em 3x dão o mesmo StateFingerprint, e o MatchTime fica parado durante a pausa.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 4, custo 1, risco 1) Hoje um R acidental ou um alt-tab perde a partida, e não ter pausa vira review negativa na certa. É barato: o Update já anda por acumulador e passo fixo, então pausar é não acumular e a velocidade é multiplicar o dt. O teste de fingerprint em 1x/3x sai quase de graça. Não precisa esperar o TEC-18; dá para fazer já no controlador atual. Ressalvas: no 3x em PC lento, rever o teto de 8 tiques por quadro (guard < 8), e lembrar que a velocidade só vale contra a IA.
- (agora; valor 4, custo 1, risco 1) É o item mais barato e de risco mais baixo do lote. Pausar é só não chamar _runner.Step(). A velocidade 3x é mais tique por quadro, e o laço do TowerWarsController já tem acumulador e guard<8. Hoje um R acidental joga fora a partida, e isso sabota o playtest da pergunta central. Não precisa esperar o TEC-18 nem o UI Toolkit: a lógica fica, só o overlay muda depois. Uma ressalva: o teste 1x = 3x no FlowSim é quase tautológico, porque o Step não depende do quadro. O teste que vale é o MatchTime/TickCount parado durante a pausa e o OnApplicationFocus.

</details>

---

### UX-05 — Fantasma de construção tático (enxuto)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2 · prioridade 4,5

**Por que, e nesta fase:**

A tese só pode ser julgada se o jogador vê o território que vai ganhar antes de pagar.

**O que é:**

O fantasma mostra antes de construir:
- a fronteira futura, com as células novas de território destacadas;
- o caminho dos bichos depois da torre ('+X s');
- uma estimativa de atrito;
- a silhueta translúcida da torre.
A recusa diz o motivo (BuildRefusal: fecha caminho, ocupado, bicho em cima, faltam N), com ✓/✕.

**Valor para o jogador:**

A tese fica visível antes de pagar, e acaba o clique morto.

**Pronto quando:**

Malha no mundo com a fronteira futura (já com o BorderAt), a silhueta da torre e o motivo da recusa (BuildRefusal). Recalcula só quando a célula muda. O FlowSim prova que prévia e motivo batem com o resultado.

**Como verificar:**

FlowSim: o motivo da recusa e a prévia do território batem com o resultado real; print do fantasma.

<details><summary>Parecer dos avaliadores</summary>

- (agora; valor 5, custo 3, risco 2) É a única ideia da lista que serve direto à tese. A pergunta 1 do TESTE.md ('cercar com fronteira tem graça?') só tem resposta se o jogador VÊ o território que vai ganhar antes de pagar. Também não depende do UI Toolkit, porque é malha no mundo. Fazer primeiro a versão enxuta: fronteira futura destacada, silhueta da torre e motivo da recusa. O '+X s' de caminho e a estimativa de atrito parecem ótimos no papel e poluem o fantasma. Ficam para depois de um playtest pedir. Recalcular só quando a célula muda.
- (logo; valor 5, custo 3, risco 2) É a ideia que mais serve à tese: a fronteira e o atrito aparecem antes de pagar, e a pergunta 1 do TESTE.md nunca foi respondida. Não depende do UI Toolkit, porque é malha no mundo. O CanBuild hoje devolve bool e tem 5 motivos claros, então trocar para um enum BuildRefusal é refatoração pequena, coberta pelo FlowSim (a IA também chama). A prévia do território é um TerritoryField de rascunho. Já o '+X s' e a estimativa de atrito pedem recalcular o fluxo hipotético, e isso só deve rodar quando a célula sob o cursor mudar, nunca a cada quadro. A estimativa de atrito é heurística e pode mentir; melhor mostrar só as células novas e o '+X s' primeiro.

</details>

---

### VIS-16 — Fronteira viva (versão legível)

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** VIS · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 5 · custo 3 · risco 2 · prioridade 4
- **Depende de:** VIS-01, VIS-15

**Por que, e nesta fase:**

O jogador não vê o atrito acontecendo.

**O que é:**

Borda emissiva, ou estacas com fitas ou postes conforme o tema. O interior ganha um tom próprio. O bicho dentro mostra esgotamento (geada ou poeira, animação mais lenta), e a morte por atrito tem efeito próprio.

**Valor para o jogador:**

A tese do jogo fica visível e bonita, o que permite julgar se ela diverte.

**Pronto quando:**

- Borda legível e tom no interior.
- Bicho dentro com tint de esgotamento e passo mais lento.
- Morte por atrito com efeito próprio.
- Faísca da armadura e brilho da regeneração.
- Teste cego: pelo menos 8 de 10 clipes de morte por atrito reconhecidos.

**Como verificar:**

Print com bichos dentro do território e teste cego: o Felipe diz qual bicho morreu por atrito.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 5, custo 3, risco 2) É a tese do jogo, e a pergunta da semana 4 (cercar com fronteira tem graça?) nunca foi respondida porque o jogador não vê o atrito acontecendo. Para o jogador, valem mais a borda legível, o tom no interior e o bicho visivelmente se esgotando do que qualquer cenário. Fazer logo uma versão barata, sem esperar o VIS-15 completo: borda, tint, partícula simples de esgotamento e uma morte por atrito distinta. As estacas com fitas temáticas ficam para depois da decisão de tema (VIS-01).
- (logo; valor 5, custo 3, risco 2) Ataca direto a pergunta central que nunca teve resposta: cercar com fronteira tem graça? Para julgar o atrito, o jogador precisa vê-lo. O TerritoryRenderer já monta o mesh de preenchimento e borda, então a borda viva é só troca de shader ou material. O esgotamento do bicho é tint por MaterialPropertyBlock mais animação mais lenta, que é só visual. Não esperar o VIS-15: fazer já a versão legível com o Vfx que existe e deixar estacas e postes temáticos para depois do VIS-01. O teste cego é uma boa verificação. O tint do bicho depende do BUG-01.

</details>

---

### UX-18 — Leitura dos bichos

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3,5 · custo 2 · risco 1 · prioridade 4

**Por que, e nesta fase:**

Ver em meio segundo por que o bicho morre e se ele está dentro do território.

**O que é:**

Barra de vida fina e segmentada a cada 100, com largura pelo porte. Ícones de lento, congelado, queimando ×N e voa. Moldura azul quando o bicho está dentro do seu território. Formas além da cor.

**Valor para o jogador:**

Ver em meio segundo por que o bicho morre e o que responde a ele.

**Pronto quando:**

Barra segmentada pelo porte. Ícones de lento, congelado, queimando, voa, armadura e regenera, com forma além da cor e coerentes com a direção realista. Moldura de 'dentro do território'.

**Como verificar:**

Teste de segmentos e de prioridade de ícone, e print de perto.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 2, risco 1) É barato e sem dependência, e resolve 'por que esse bicho não morre'. A moldura azul dentro do território é a única forma de o jogador ver a tese (atrito) acontecendo, então ajuda a vender a ideia central. Barra segmentada é linguagem conhecida (Dota/WC3). Só faz sentido com os bichos visíveis, então vem logo depois do BUG-01.
- (logo; valor 3, custo 2, risco 1) É barato de verdade: o EnemyView já desenha a barra e o flash de acerto, e o SimEnemy já expõe Frozen, Burning, SlowLeft e BurnPct. O que falta é só a vista. A moldura de 'dentro do território' torna visível a tese do atrito, que é o que mais precisa ser lido. Ressalva: hoje os bichos estão invisíveis no executável, então isto só rende depois do BUG-01. Os ícones precisam seguir a direção realista, nada de cartoon.

</details>

---

### TEC-19 — Diário automático de partidas

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** TEC · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2 · risco 1 · prioridade 4
- **Depende de:** TEC-03

**Por que, e nesta fase:**

Cada partida vira dado para a tese, e todo bug vem com replay.

**O que é:**

Toda partida grava sozinha o replay e um resumo em JSON/CSV:
- vencedor, duração, % de atrito, tempo dentro do território;
- ouro em torre × envio, envios e torres por tipo, FPS p95.
No fim, uma nota opcional de 1 a 5. Um agregador no FlowSim compara o jogo humano com IA × IA. Rodízio das últimas 50-200 partidas.

**Valor para o jogador:**

Cada playtest vira dado para responder à tese, e todo bug vem com replay.

**Pronto quando:**

- Replay e resumo em JSON a cada partida, com flush por evento: vencedor, duração, % de atrito, ouro em torre × envio e p95.
- Nota de 1 a 5 no overlay de desenvolvedor.
- Rodízio das últimas 200.
- O replay reproduz o vencedor no FlowSim.

**Como verificar:**

Uma partida gera replay + resumo, e o replay reproduz o mesmo vencedor no FlowSim.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 3, custo 2, risco 1) A tese central (cercar com fronteira tem graça?) nunca foi respondida, e isto transforma cada partida do Felipe em dado. A nota de 1-5 no fim é o sinal mais honesto de diversão que existe, e é barato. Ressalva: o agregador IA × humano só vale depois de umas dezenas de partidas humanas, então comece pelo replay + resumo + nota.
- (logo; valor 3, custo 2, risco 1) É o instrumento mais barato para responder à pergunta central, que nunca foi respondida: cercar com fronteira tem graça? O replay e as estatísticas já existem na LaneSim. O FPS p95 por partida também serve para medir cada mudança visual (a grama derrubou de 104 para 54). Ressalvas honestas: um só jogador é amostra pequena e não substitui playtest com outras pessoas. A nota de 1 a 5 depende da UI nova. O agregador humano contra IA × IA é trabalho a mais além do P. Depende do TEC-03.

</details>

---

### DES-03 — Portão 1: A/B cego

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** DES · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 4 · custo 3 · risco 1,5 · prioridade 4
- **Depende de:** BUG-05, TEC-19, UX-19

**Por que, e nesta fase:**

Todo o conteúdo caro aposta que a fronteira diverte, e o autor da tese jogando sabendo o modo é evidência fraca.

**O que é:**

Chaves no menu (atrito ligado/desligado, ouro infinito, 2x) que voltam ao padrão ao sair. O BalanceLab ganha as métricas 'viradas por partida' e 'tensão'. Protocolo A/B de 3+3 partidas, com a resposta da 'decisão da semana 4' registrada no README.

**Valor para o jogador:**

Responde se cercar com fronteira tem graça antes de investir em conteúdo.

**Pronto quando:**

- O jogo sorteia e esconde o modo, que só é revelado no diário.
- Felipe 4+4 partidas, mais 2-3 pessoas de fora.
- Critério escrito antes: nota com atrito ≥ nota sem atrito + 0,5, e métricas de virada e tensão ao lado.
- Decisão no README.

**Como verificar:**

Detector de virada testado com fixture, e decisão escrita no README.

<details><summary>Parecer dos avaliadores</summary>

- (logo; valor 4, custo 3, risco 2) O jogador não vê isso diretamente, mas protege todo o resto: em dois meses ninguém respondeu se cercar com fronteira tem graça. Gastar em segundo mercado, chefes e auras antes disso é apostar no papel. Vem logo depois de os bichos voltarem a aparecer no executável. As chaves (atrito desligado, ouro infinito) podem depois virar o modo 'partida personalizada' para o jogador.
- (logo; valor 4, custo 3, risco 1) É o item mais importante desta lista para a produção: a pergunta da semana 4 nunca teve resposta, e todo o 2º mercado (hienas, sabotador, chefes) aposta que a fronteira é divertida. As chaves que voltam ao padrão são baratas. As métricas 'virada' e 'tensão' são vagas e precisam de definição testável com fixture. O A/B depende do Felipe jogar 6 partidas, e só vale com os bichos visíveis e uma UI mínima. Se rodar antes, a feiura contamina a resposta.

</details>

---
