# Fase 17 — Vitrine

[← cronograma](../../ROADMAP.md) · como trabalhar: [MANUAL](../MANUAL.md)

- **Duração estimada:** 5-6 sessões
- **Créditos Meshy estimados:** 0 _(sempre perguntar ao Felipe antes de gastar)_

## Objetivo

Trailer, demo para o Next Fest e Steamworks, com material gerado pelo próprio jogo.

## Critério de saída (a fase só fecha com tudo isto verificado)

- Trailer, demo e cápsula aprovados.
- Steamworks com o AppID real.

## Decisões do Felipe nesta fase

Pergunte antes de executar o item que depende da decisão; registre a resposta aqui.

- [ ] Cápsula feita por artista ou render, e com que verba.
- [ ] Data do Next Fest.

## Tarefas, em ordem

- [ ] META-12 — Modo cinema
- [ ] UX-21 — Menu principal vivo
- [ ] META-04 — Integração Steamworks
- [ ] META-05 — Conquistas
- [ ] META-13 — Marcadores na gravação da Steam
- [ ] MKT-03 — Demo, press kit e cápsula

---

### META-12 — Modo cinema

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2,5 · prioridade 3
- **Depende de:** TEC-16, BUG-01

**Por que, e nesta fase:**

Trailer regerado a cada salto de arte.

**O que é:**

Um roteiro de câmera ou um Diretor por regras toca um replay com captureFramerate 60, em 4K e sem HUD. Trailer e screenshots são regerados a cada salto de arte.

**Valor para o jogador:**

Página da Steam sempre atualizada, sem editor de vídeo.

**Pronto quando:**

Replay roteirizado em 4K, sem HUD. Mesma sequência de jogo em duas execuções. A edição final é manual.

**Como verificar:**

Duas execuções dão quadros idênticos, e o MP4 é aprovado pelo Felipe.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 3) Screenshots e trailer vendem na Steam, mas só depois que a arte estiver boa: gravar em 4K um jogo ainda feio não serve para nada. Hoje cabe só a versão mínima, que o -captura quase já é: ângulos fixos e repetíveis para comparar cada salto de arte. Um trailer montado por regras costuma ficar sem graça. Os quadros determinísticos servem de matéria-prima, e a edição final com música continua manual.
- (depois; valor 3, custo 3, risco 2) É útil de verdade para a página da Steam, e o SmokeCapture e o build sem editor já são meio caminho. Só faz sentido depois do salto de arte e do BUG-01: filmar bicho invisível e chão ocre não serve. Desconfio da verificação 'quadros idênticos'. captureFramerate fixa o deltaTime, mas partículas, UnityEngine.Random nas views e o ruído temporal do SSAO e do TAA podem variar. O critério razoável é 'mesma sequência de jogo', não igualdade bit a bit.

</details>

---

### UX-21 — Menu principal vivo

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** UX · **Esforço:** G (3-5 sessões)
- **Notas dos avaliadores (1-5):** valor 4,5 · custo 4 · risco 3 · prioridade 3,5
- **Depende de:** UX-01, UX-04, BUG-01, VIS-11

**Por que, e nesta fase:**

A primeira impressão vende.

**O que é:**

O menu fica sobre o mundo real, com uma partida IA × IA ao fundo e câmera em planos lentos. DOF só no menu, logo tipográfico e cartões de dificuldade com texto tirado da Personality. O menu vira tela de carregamento, e Jogar voa até a lane sem reconstruir o mundo.

**Valor para o jogador:**

A primeira impressão vende o jogo.

**Pronto quando:**

IA × IA ao fundo e planos lentos. Do clique até jogável em menos de 0,5 s.

**Como verificar:**

'-captura-menu' com FPS e tempo do clique até jogável < 0,5 s, e o -captura antigo intacto.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 4, custo 4, risco 3) A primeira impressão vende, mas um menu vivo só mostra o mundo que existe. Hoje esse mundo tem chão ocre, árvores facetadas e bichos invisíveis, então o menu vivo faria propaganda da feiura. Primeiro um menu estático bonito em UI Toolkit (UX-01). A partida IA × IA ao fundo sai barata porque a simulação já existe. A parte arriscada é o 'Jogar voa até a lane sem reconstruir o mundo', que mexe no GameBootstrap. Fica para depois que a direção de arte (VIS-11) estiver pronta.
- (logo; valor 5, custo 4, risco 3) O Felipe pediu menu bonito, e a primeira impressão vende na Steam. 'G' é honesto, talvez pouco. O MatchRunner só sabe jogador contra IA, e precisa ganhar um modo IA × IA (o laboratório do FlowSim já prova que a Sim aguenta). Também entram uma câmera de planos, DOF só no menu e a transição em menos de 0,5 s sem reconstruir um mundo que o GameBootstrap monta inteiro em código. Isso mexe no fluxo de bootstrap e pode quebrar o -captura. Também não adianta antes de o mundo ficar bonito (VIS-11) e de os bichos aparecerem (BUG-01). Fazer em degraus: um menu em UI Toolkit sobre o mundo com a câmera orbitando primeiro, e a partida de fundo depois.

</details>

---

### META-04 — Integração Steamworks

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 3 · risco 2 · prioridade 3
- **Depende de:** META-03, TEC-03

**Por que, e nesta fase:**

Cloud, Rich Presence e placar.

**O que é:**

Steamworks.NET atrás de uma IPlataforma com implementação Nula. Placares com replay anexado (UGC), Rich Presence e Steam Cloud. Testes com o AppID 480.

**Valor para o jogador:**

O que o jogador da Steam espera, e ranking verificável sem servidor.

**Pronto quando:**

IPlataforma com implementação Nula, Cloud ligado ao SaveStore. Placar só se o TEC-16 provar o determinismo.

**Como verificar:**

Com o AppID 480, uma pontuação com replay sobe, desce e reproduz, e o FlowSim segue verde com a Nula.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 3, risco 2) Steam Cloud, Rich Presence e placar são o que o jogador espera, e a IPlataforma com implementação Nula mantém o FlowSim limpo. Mas não há jogador nem AppID ainda, então hoje não entrega nada. Placar com replay verificável depende de determinismo entre máquinas, e a Sim usa float sem teste entre runtimes. Faça junto com a página da Steam.
- (depois; valor 3, custo 3, risco 2) A IPlataforma com implementação Nula protege o FlowSim, e o AppID 480 permite testar. Mas o Steamworks.NET traz DLL nativa para o pipeline do BuildJogo e steam_appid.txt, e pede cuidado no headless. Placar com replay 'verificável' esbarra num ponto: a simulação usa float sem teste entre runtimes, então verificar o replay de outra máquina não está garantido. Sem AppID próprio nem página publicada, ainda não tem para quem servir.

</details>

---

### META-05 — Conquistas

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** M (1-2 sessões)
- **Notas dos avaliadores (1-5):** valor 3 · custo 2,5 · risco 1 · prioridade 3
- **Depende de:** UX-19, META-04

**Por que, e nesta fase:**

Metas que ensinam a tese.

**O que é:**

Cerca de 25-30 conquistas avaliadas por uma função pura sobre o resumo da partida, metade ensinando a tese ('60% dos abates por atrito'). Salvas localmente e espelhadas na Steam.

**Valor para o jogador:**

Metas curtas que ensinam a mecânica.

**Pronto quando:**

8 a 10 por função pura, cada uma com partida que desbloqueia e partida que não. Espelhadas na Steam.

**Como verificar:**

Para cada conquista, uma partida roteirizada que desbloqueia e outra que não.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 3, custo 2, risco 1) Jogador da Steam espera conquistas, e as que ensinam a tese ('60% dos abates por atrito') são o melhor tipo. Uma função pura sobre o resumo da partida é barata e testável. Ainda assim, só tem palco depois de existir resumo de fim de partida, UI e Steamworks. É polimento de pré-lançamento, não fundação.
- (depois; valor 3, custo 3, risco 1) Avaliar por função pura sobre o resumo da partida casa bem com a arquitetura, e conquistas que ensinam a tese são boa ideia. Mas 25 a 30 conquistas com duas partidas roteirizadas cada dão 50 a 60 testes, e o pre-commit já leva ~3 min. Depende do resumo (UX-19) e da Steam (META-04). Comece com 8 a 10 ligadas à tese quando o resumo de fim de partida existir.

</details>

---

### META-13 — Marcadores na gravação da Steam

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Categoria:** META · **Esforço:** P (menos de 1 sessão)
- **Notas dos avaliadores (1-5):** valor 2 · custo 2 · risco 1,5 · prioridade 3
- **Depende de:** META-04, TEC-17

**Por que, e nesta fase:**

Divulgação em vídeo curto.

**O que é:**

Os eventos da partida viram marcadores no ISteamTimeline, para o jogador cortar clipes.

**Valor para o jogador:**

Divulgação orgânica em vídeo curto.

**Pronto quando:**

Os marcadores aparecem numa gravação de teste.

**Como verificar:**

Os marcadores aparecem numa gravação de teste.

<details><summary>Parecer dos avaliadores</summary>

- (depois; valor 2, custo 2, risco 2) Custa pouco quando o Steamworks estiver integrado, mas o ganho é marginal: poucos jogadores cortam clipes pela Gravação de Jogo. Vale fazer quando houver um fluxo de eventos pronto (TEC-17) e Steam no build. Antes disso, não.
- (depois; valor 2, custo 2, risco 1) Custa pouco só depois que a Steam estiver integrada, e hoje não há nenhuma linha de Steamworks no repositório. A Timeline exige SDK recente, um wrapper que exponha ISteamTimeline e um AppID pago. O ganho é marginal e só aparece com jogadores. Entra como item de 1 dia no pacote de integração com a Steam, não antes.

</details>

---

### MKT-03 — Demo, press kit e cápsula

- [ ] **Status:** a fazer  _(ao concluir: marque [x], escreva data e commit, e marque também no ROADMAP.md)_
- **Esforço:** M (1-2 sessões)

**Por que, e nesta fase:**

A cápsula decide o CTR, e render cru de 3D costuma ficar fraco.

**Pronto quando:**

Demo gerada da main, press kit e cápsula com arte não-IA (artista ou render tratado). Inscrição no Next Fest.

---
