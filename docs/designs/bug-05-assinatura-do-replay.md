# Design — BUG-05: a assinatura do replay cobre todos os campos

[← cronograma](../../ROADMAP.md) · cartão: [Trilha S, BUG-05](../roadmap/trilha-s-simulacao-em-paralelo-headless-worktree-sem-unity.md)

**Estado:** escrito e **aprovado pelo Felipe em 09/10/2026, com D1 a D5 como recomendado** (tarefa "architectural": mexe no
formato do replay). Implementação depois do BUG-04. Esforço estimado: uma sessão. Vem **depois** do BUG-04 (a assinatura tem
que ler os catálogos já completados e na ordem de fábrica) e usa o `StateHash` do TEC-31.

## 1. O problema, visto rodando no código de hoje

Mudei cada campo abaixo, um de cada vez, e li `Replay.CurrentCatalogSignature()`:

| Campo mudado | Assinatura reage? |
|---|---|
| `TowerType.Cost`, `SendUnit.Bounty` | sim |
| trocar a ordem de dois envios | sim |
| `TowerType.BurnPctPerSecond` e `BurnSeconds` (Fogo) | **não** |
| `TowerType.Knockback` (Ar) | **não** |
| `TowerType.Name`, `SendUnit.Name` | **não** |
| `TowerWarsConfig.AttritionPctPerSecond` (atrito) | **não** |
| `TowerWarsConfig.SendScalePerMinute` (escalada) | **não** |

Ou seja: um replay gravado com outro fogo, outro atrito ou outra escalada **se reproduz como outra partida e a
ferramenta diz que está tudo igual**. É a pior falha de um replay (manda a investigação atrás de um bug que não existe).

Outros fatos do mesmo código:

1. A assinatura é um `int` de **32 bits** (`h * 31 + ...`, 8 dígitos hexa) com `float.GetHashCode()`. Funciona hoje, mas
   não é um formato: nada garante a mesma conta em outro runtime.
2. `AttritionPctPerSecond` e `SendScalePerMinute`, que o cartão cita como "do catálogo", são de `TowerWarsConfig` (e são
   `static`, não `const`, para o laboratório varrê-los). Esse arquivo tem **20 campos públicos** e **nenhum** entra na
   assinatura: `StartGold`, `StartLives`, `BaseIncome`, `MaxTowerLevel`, `SellRefund`, `SuddenDeathMinutes`, o gelo,
   o fogo, o passo fixo...
3. As **personalidades da IA** (Fácil, Normal, Difícil: 6 campos cada) também decidem a partida e não entram.
4. `"Rato".GetHashCode()` deu `-1245068465` num processo e `781430047` no outro: o hash de string do .NET é aleatório
   por execução. Os nomes **não podem** entrar por `GetHashCode`.
5. O catálogo hoje tem 8 campos por envio e 13 por torre. Cada campo novo (vida de torre, tier, ramo) repetiria o buraco
   se a assinatura continuar sendo uma lista escrita à mão sem guarda.
6. Há coisa que **nenhuma assinatura de dados** pega: fórmulas em código (`ChillPerHit`, `FreezeSeconds`, `SendScale`),
   mira da torre, pathfinding, lógica da IA. Isso é tratado na seção 3.5 e 3.6.

## 2. Objetivo e o que fica de fora

**Objetivo:** o replay diz "isto foi gravado com **outras regras**" quando qualquer **dado** de balanceamento mudou, e
diz **qual parte** mudou (envios, torres ou regras).
**Fora do escopo:** autenticar o arquivo (ninguém forja replay hoje); cobrir mudança de **lógica** por assinatura (isso
é o `SimRules.Version` e o fingerprint final, abaixo); o formato dos comandos (não muda).

## 3. Design

### 3.1 `SimSignature` (pura, `Sim/SimSignature.cs`)

- Hash FNV-1a de 64 bits (o mesmo núcleo do `StateHash` do TEC-31, com `Add(string)` novo), saída de **16 dígitos hexa**.
- Cada campo entra como **nome do campo + tipo + valor** num fluxo de bytes canônico, em ordem fixa. Assim, acrescentar,
  tirar ou renomear um campo muda a assinatura, e dois campos de mesmo valor não se confundem.
- **Número com vírgula:** os 32 bits do `float` (`BitConverter.SingleToInt32Bits`), com `-0` normalizado para `+0` e
  qualquer `NaN` para um só. Nada de quantizar: aqui o valor tem que ser **exato** (o arquivo de balanceamento é texto,
  `0.19` lê sempre o mesmo `float`).
- **Texto:** bytes UTF-8, com tamanho na frente. Nunca `GetHashCode`.
- **Ordem:** envios e torres na ordem do índice (o índice é a identidade).

### 3.2 O que entra, em três blocos

| Bloco no arquivo | Conteúdo | Campos hoje |
|---|---|---|
| `sends` | `Count` + cada `SendUnit`, **todos** os campos, nome inclusive | 8 por envio |
| `towers` | `Count` + cada `TowerType`, **todos** os campos, nome inclusive | 13 por torre |
| `rules` | `SimRules.Version` (3.5) + os 20 campos de `TowerWarsConfig` (constantes inclusive) + as 3 `Personality` + `GameConfig.CellSize` (a única coisa de `GameConfig` que a Sim lê além do grid, que o replay já grava: conferi por busca no código) | 20 + 18 + 1 |

Três blocos e não um: a mensagem de erro diz **o que** mudou ("os envios mudaram"), em vez de "a assinatura não bate".

### 3.3 Como garantir que nenhum campo fica de fora (o ponto onde divirjo do cartão)

O cartão pede cobrir "por reflexão". Proponho **lista explícita, escrita campo a campo, com um teste de guarda por
reflexão**:

- No jogo, o escritor chama `w.Int("cost", u.Cost)`, `w.Flt("hp", u.Hp)` etc. para cada campo. Sem reflexão em
  tempo de execução.
- No FlowSim (que roda em .NET 10, onde reflexão é confiável), um teste enumera por reflexão **todos** os campos
  públicos de `SendUnit`, `TowerType`, `Personality` e `TowerWarsConfig` e confere que cada um está na lista de nomes que
  o escritor registra. Campo novo sem entrada na assinatura **reprova o teste** com o nome do campo na mensagem.
- Mais: para cada campo que se pode mudar em tempo de execução, o teste **muda o valor e exige que a assinatura mude**
  (é o "teste que altera cada campo" do cartão).

Por que não reflexão no jogo: o backend de script ainda não foi decidido (TEC-32; hoje é o padrão do Unity), e com IL2CPP
e corte de código a reflexão sobre campos e constantes é frágil. A lista explícita são ~59 linhas e **não** depende disso;
o custo é lembrar de acrescentar a linha, e o teste não deixa esquecer.

### 3.4 Formato do arquivo, versão 2

```
tdfende-replay 2
seed 123
difficulty Normal
grid 24 16
ticks 4000
sends  <16 hexa>
towers <16 hexa>
rules  <16 hexa>
final  <fingerprint>          (opcional, ver 3.6)
<tique> <comando>
...
```

- O cabeçalho passa a **`tdfende-replay 2`**. O leitor recusa o `1` com
  `"gravação do formato 1 (anterior à assinatura completa): regrave a partida"`. É a "invalidação de propósito" do cartão.
- `Replay.Verify()` devolve **qual** bloco difere (`SendsDiffer`, `TowersDiffer`, `RulesDiffer`) ou `Ok`.
- Ferramenta (`FlowSim replay <arquivo>`): assinatura diferente **recusa a reprodução** e diz qual bloco difere, a menos
  que se passe `--forcar` (hoje só avisa e segue, que é o comportamento que enganou).
- Uma linha desconhecida continua falhando alto (hoje cai em "comando não reconhecido").

### 3.5 `SimRules.Version`

Um inteiro em código (`SimRules.Version = 1`), **incrementado à mão** por quem mudar uma regra que não é dado:
fórmula de gelo ou fogo, escalada, mira, pathfinding, lógica da IA. Entra no bloco `rules`, então replay antigo é
recusado e o motivo é dito. É uma regra de processo de uma linha no MANUAL, mais um teste que lembra: o teste de
regressão com replays guardados (se existir) quebra quando alguém esquece. Honestamente: depende de memória humana,
por isso o 3.6.

### 3.6 Fingerprint final como diagnóstico (opcional, recomendo)

Como o formato muda uma vez, vale gravar junto o `StateFingerprint()` do TEC-31 no fim da partida (`final ...`).
`Run()` compara no fim e imprime **"estado final igual"** ou **"divergiu"**. É a única coisa que pega **mudança de
lógica** que a assinatura de dados não vê. Fica **só como aviso, nunca como recusa**, porque ele também é o instrumento
que vai **medir** se o replay gravado no executável do Felipe (Mono) reproduz igual no .NET headless: a dúvida do TEC-27
(float entre runtimes), que ninguém mediu ainda.

## 4. Testes (escritos antes, vistos falhar pelo motivo certo)

1. Para **cada** um dos 8 campos de `SendUnit` e dos 13 de `TowerType` (por reflexão, no teste): mudar o valor muda
   `sends`/`towers`. (Hoje falham: nome, queima, empurrão e mais.)
2. `TowerWarsConfig`: mudar `AttritionPctPerSecond` e `SendScalePerMinute` muda `rules`; **todo** campo público do tipo
   está na lista coberta (guarda). Cada `Personality`: mudar `CounterStrength` do Difícil muda `rules`.
3. Trocar a ordem de dois envios muda `sends`. Mudar `SimRules.Version` muda `rules`.
4. Mesmo estado, duas chamadas: mesma assinatura. Vetores públicos do FNV-1a (`""` = `cbf29ce484222325`,
   `"a"` = `af63dc4c8601ec8c`) validam a implementação, e um teste fixa a assinatura do catálogo de fábrica em
   constante, para uma mudança involuntária de formato aparecer no diff.
5. `-0` e `+0` dão a mesma assinatura; qualquer `NaN` também.
6. Formato 2: ida e volta preserva tudo; cabeçalho 1 é recusado com a mensagem; um bloco adulterado diz **qual** bloco
   difere; linha desconhecida é recusada; `final` sobrevive à ida e volta e o `Run()` o confere.
7. O que já existe segue verde: o `rec.Run()` reproduz a partida, replay do arquivo reproduz o mesmo estado, comandos
   fora de ordem e envio fora do catálogo são recusados. O BalanceLab (`match 12`) fica **idêntico** (nada de regra muda).

## 5. Riscos

- **Todo replay já gravado deixa de valer** (cabeçalho 1). Aceito no cartão e hoje só o laboratório os usa. O Felipe, se
  tiver arquivos guardados, regrava.
- A assinatura passa a mudar **sempre** que alguém balanceia. É o objetivo; o custo é que replay usado como teste de
  regressão tem que ser regravado quando o balanceamento mudar de propósito (nota no MANUAL).
- Campo novo na Sim (vida de torre, tier, ramo: MERC-05, TORRE-07, TEC-15 `MatchRules`) agora **reprova o teste de
  guarda** até entrar na lista. É o comportamento desejado e a correção é uma linha.
- Os 20 campos de `TowerWarsConfig` (2 variáveis e 18 constantes) incluem constantes que só mudam recompilando; entram porque dois **binários**
  diferentes (o do Felipe e o meu) podem ter constantes diferentes.

## 6. Decisões pedidas ao Felipe

| | Pergunta | Recomendação |
|---|---|---|
| **D1** | Invalidar de propósito os replays do formato 1? | **Sim.** Nenhum jogador usa; o laboratório regrava em segundos. |
| **D2** | Cobertura por lista explícita + teste de guarda (A), ou reflexão em tempo de execução como no cartão (B)? | **A**, pelo IL2CPP/corte de código que o TEC-32 ainda vai decidir. |
| **D3** | Incluir o `SimRules.Version` manual (A) ou deixar a mudança de lógica para o fingerprint final (B)? | **A.** Uma linha, e o erro diz o motivo. |
| **D4** | Gravar e conferir o fingerprint final como diagnóstico (A) ou deixar para o TEC-16/TEC-27 (B)? | **A.** O formato só muda uma vez, e ele responde a pergunta do float entre runtimes. |
| **D5** | Assinatura diferente **recusa** a reprodução, com `--forcar` para passar por cima (A), ou continua só avisando (B)? | **A.** O "só avisa" de hoje é o que enganou. |
