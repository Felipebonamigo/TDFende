# Design — DES-04, BICHO-05 e BICHO-06: três campos novos no envio

[← cronograma](../../ROADMAP.md) · cartões: [DES-04, BICHO-05, BICHO-06 na Trilha S](../roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md)

**Estado:** escrito, **aguardando aprovação do Felipe** (tarefas "architectural": cada uma acrescenta campo em `SendUnit`,
muda o formato do catálogo e a assinatura do replay). Nenhum código escrito. Os três juntos porque usam o **mesmo mecanismo**
(campo no `SendUnit` → cópia no `SimEnemy` → coluna no `envios.txt` → linha na `SimSignature`) e se medem no mesmo BalanceLab.
Ordem proposta: **DES-04, depois BICHO-05, depois BICHO-06**, um commit cada, BalanceLab antes e depois de cada um.
Esforço: uma sessão para os três (cada um é pequeno; o trabalho é medir).

## 1. O mecanismo comum (vale para os três)

| Passo | O que muda |
|---|---|
| `SendUnit` | um campo novo por tarefa, com **valor neutro** que reproduz o jogo de hoje (`LivesCost = 1`, `Armor = 0`, `RegenPctPerSecond = 0`) |
| `SimEnemy` | copia o campo no nascimento (`SpawnIncoming`) e **mantém** no repasse (`SpawnCarried` já copia o struct inteiro) |
| `envios.txt` | coluna nova (`lives=`, `armor=`, `regen=`); campo ausente vale o da fábrica (BUG-04), então arquivo antigo continua lendo |
| `SimSignature` | linha nova em `Sends`; o teste de guarda do BUG-05 exige. Replay formato 2 antigo é recusado pela assinatura (sem formato novo) |
| `SimRules.Version` | sobe uma vez por tarefa (mudou lógica) |
| Testes | escritos antes; **com o valor neutro o BalanceLab fica idêntico** (prova de que o campo novo não vaza); depois o valor de fábrica muda de propósito e o BalanceLab é medido de novo |

## 2. DES-04 — vazamento pesa pelo porte (`LivesCost`)

**Hoje (código):** cada inimigo que cruza a base faz `Lives--`, não importa quem seja. Uma compra de Rato (22 de ouro, `Count = 4`)
pode tirar **4 vidas**; um Elefante de 90 de ouro tira **1**. O jogador que deixa o Elefante passar perde menos que o que deixa
passar um enxame que custou um quarto do preço, o oposto do que o corpo na tela sugere.

**Proposta:** `SendUnit.LivesCost` (inteiro, mínimo 1). No vazamento: `Lives -= LivesCost` (nunca abaixo de 0, e o evento
`LifeLost` carrega o valor). Valores de fábrica: Rato 1, Cachorro 1, Lobo 1, Javali 2, Águia 2, Urso 2, Tigre 2, Rinoceronte 3,
Elefante 3 (elite 4-5 quando existirem). **A volta repassada custa 1** (`Laps > 0`): a segunda base que ele cruza não pesa pelo porte,
senão a bola de neve do repasse explode.
- `TotalLeaked` continua contando **inimigos** (a conta dos eventos do TEC-17 fecha pelos dois números: `LifeLost` soma
  `LivesCost`, e o contador de inimigos segue como está).
- A IA pondera o perigo pelo porte: `ThreatHp` e a escolha de envio passam a contar `LivesCost` (um envio que tira 3 vidas vale mais
  contra quem está com poucas vidas). Reajuste medido no BalanceLab, não à mão.
- Vidas iniciais (`StartLives = 20`): **medir antes de mexer**. Com o Elefante tirando 3, a partida pode encurtar; se o BalanceLab sair
  da faixa de 6 a 12 min, o ajuste é `StartLives`, não os custos.
- HUD ("-3" flutuante) e tremor proporcional da fortaleza: Fase 5 / vista; a Sim só entrega o número no evento.

## 3. BICHO-05 — armadura (`Armor`)

**Proposta:** `SendUnit.Armor` (decimal, 0 = sem). Cada **acerto de torre** perde `Armor` de dano, com **piso de 20% do dano**:
`dano_final = max(dano − Armor, 0,2 × dano)`, aplicado depois do multiplicador contra voador, **por inimigo atingido** (o splash do
Morteiro também). **Atrito e queima ignoram a armadura** (não são acerto). Valor de fábrica: **Rinoceronte `Armor = 6`**; o resto 0.
- Conta do cartão: a Sentinela (dano 9) causa `max(9 − 6; 1,8) = 3` por tiro no Rinoceronte; o Canhão (12) causa 6; o Gelo (4) cai no
  piso (0,8) mas continua **lentificando e congelando** (efeito de status não depende do dano); o **Fogo vence por ouro** porque a queima
  ignora a armadura. Isso é o ponto: o primeiro bicho que obriga a pensar em dano por tiro.
- `EnemyHit` (TEC-17) já informa o dano **depois** da armadura.
- O portador futuro (Tatu-bola, Búfalo de elite, Rinoceronte-alfa) só precisa da coluna `armor=`.
- Faísca no acerto e ícone na loja: Fase 3 (vista).

## 4. BICHO-06 — regeneração fora da fronteira (`RegenPctPerSecond`)

**Proposta:** `SendUnit.RegenPctPerSecond` (fração da **vida-base** por segundo; 0 = não regenera). Enquanto o bicho está **fora do
território**, `Hp += BaseHp × pct × dt`, **limitado a `MaxHp`** (o teto). **Não regenera queimando** (`BurnLeft > 0`) e **desliga na morte
súbita** (`InSuddenDeath`), as duas regras do cartão, para a partida sempre poder acabar. A base é a vida-**base**, a mesma régua do fogo
(proporcional à escalada o bicho viraria imortal com o relógio). Valor de fábrica: **Urso `RegenPctPerSecond = 0,015`** (≈3 de vida por
segundo num Urso de 200, em trecho sem fronteira).
- Ensina que **buraco na fronteira custa caro**: o Urso que passa por um trecho sem território sai com mais vida do que entrou.
- Não mexe em quem está dentro do território (lá o atrito o come), então reforça a tese em vez de competir com ela.
- Brilho verde no bicho: Fase 3 (vista).

## 5. Riscos

- **Valor de fábrica novo muda o balanceamento de propósito.** Por isso cada tarefa é medida em duas etapas: primeiro o campo com valor
  neutro (BalanceLab **idêntico**), depois o valor real (BalanceLab novo, registrado no commit, como no DES-05).
- Três colunas novas no `envios.txt`: arquivo exportado antes continua lendo (campo ausente = fábrica); a assinatura do replay muda.
- `LivesCost` mexe no fim de partida (a morte vem mais rápido): é o item de maior risco de balanceamento dos três.
- A armadura com piso de 20% pode deixar o Gelo "inútil" em dano contra o Rinoceronte; o efeito de lentidão é o papel dele, e a matriz de contras
  (TEC-14) é a régua para conferir se o papel se sustenta.

## 6. Decisões pedidas ao Felipe

| | Pergunta | Recomendação |
|---|---|---|
| **D1** | `LivesCost` como inteiro por envio (rato 1 … elefante 3) (A), ou integridade 100 com dano por porte (B)? | **A.** Mantém o HUD de vidas que o jogador já entende. B muda a escala de tudo. |
| **D2** | A volta repassada custa sempre 1 vida (A), ou pesa pelo porte também (B)? | **A**, como no cartão. |
| **D3** | Se o BalanceLab sair da faixa de 6-12 min depois do `LivesCost`, ajusto `StartLives` (A), ou os custos por porte (B)? | **A.** Os custos 1/2/3 são a ideia do cartão; as vidas iniciais são o botão de ajuste. |
| **D4** | Armadura: `max(dano − armadura; 20% do dano)` por acerto, atrito e queima ignoram (A)? | **A**, é o que o cartão pede. |
| **D5** | Regeneração por vida-**base** com teto na vida máxima, desligada queimando e na morte súbita (A)? | **A.** |
| **D6** | Valores de fábrica: Rinoceronte `Armor` 6, Urso `Regen` 1,5%/s, `LivesCost` 1/1/1/2/2/2/2/3/3. Aprovar como ponto de partida, a calibrar no BalanceLab? | **Sim.** São o ponto de partida; o commit de cada tarefa registra o que a medição mudou. |
