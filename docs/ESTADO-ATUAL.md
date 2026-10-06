# Estado atual do jogo (mapa do código)

Feito por leitores do código em 06/10/2026, antes do cronograma. Use como referência rápida; confirme no código antes de mexer.

## Simulação e regras do jogo (Assets/_Project/Scripts/Runtime/Sim/ + Grid/ + Core/GameConfig)

A simulação do Tower Wars é C# puro e determinístico. Roda em passo fixo de 1/30 s, usa um System.Random com semente e só aceita comandos na fronteira de tique. É data-driven em dois catálogos estáticos: TowerCatalog (6 torres, 6 níveis) e SendCatalog (9 bichos). Os 183 Check() do Tools/FlowSim cobrem regras, determinismo, replay e metas estatísticas de balanceamento, e rodam no pre-commit (.githooks, cerca de 3 min).

O modelo atual é estreito para o que o Felipe pediu:
- torre não tem vida e bicho não ataca;
- o nível é um inteiro linear, sem ramificação, e só o dano escala;
- bicho só tem vida, velocidade, quantidade e imunidade ao atrito (o "voador" ainda anda pelo labirinto);
- não existe tier, mercado, estoque nem desbloqueio de envio;
- a torre sempre mira o inimigo mais próximo;
- os catálogos não têm metadado para a loja (descrição, ícone, papel).

Torres, bichos e projéteis estão acoplados por ÍNDICE em ModelLib, Vfx, ProjectileView, ModelLibTiers, nos testes e no replay. Por isso conteúdo novo precisa ser só acrescentado no fim da lista. O TD clássico é uma implementação paralela e antiga (GameController + Enemy/Tower MonoBehaviour + GameConfig) que não enxerga nada disso.

Os pontos de extensão estão claros. Efeito novo vira campo no struct, em CatalogJson, na assinatura do Replay, em HitEnemy/TickEnemies, num termo de nota da IA e num teste FlowSim. Comando novo segue CommandKind → MatchCommand → Replay.TryParseCommand → MatchRunner.Apply. Remover torre por destruição pode reaproveitar a semântica de índice do TowerSold, que o LaneView já trata.

### O que existe

**Torres (TowerCatalog.All, ids estáveis 0-5)** — `Assets/_Project/Scripts/Runtime/Sim/TowerCatalog.cs`

Nível 1 (custo / alcance / cadência s / dano / DPS / área / fronteira):
- 0 Canhão: 25 / 3.5 / 0.65 / 12 / 18.5 / 0 / 2.75. É a régua.
- 1 Morteiro: 45 / 4.2 / 1.15 / 14 / 12.2 / 1.6 / 2.0. Contra enxame; a vista desenha arco.
- 2 Gelo: 40 / 3.2 / 0.9 / 4 / 4.4 / 0 / 3.25. Lentidão 0.55 por 1.6 s e congela.
- 3 Sentinela: 50 / 4.5 / 0.75 / 9 / 12 / 0 / 1.5. VsFlying x2.6.
- 4 Fogo: 45 / 3.0 / 0.9 / 5 / 5.6 / 0.8 / 2.0. Queima 5%/s da vida-BASE por 3 s, até 3 camadas.
- 5 Ar: 40 / 3.8 / 1.3 / 6 / 4.6 / 0.9 / 2.25. VsFlying x1.8, empurra 0.7 célula com guarda de 1.6 s.

Todos os campos de TowerType: Name, Cost, Range, Cooldown, Damage, SplashRadius, SlowFactor, SlowSeconds, VsFlyingMultiplier, BorderRadius, BurnPctPerSecond, BurnSeconds, Knockback. TowerCatalog.Get(id) cai no 0 se o id for inválido.

**Níveis de torre (1-6, MaxTowerLevel=6)** — `Assets/_Project/Scripts/Runtime/Sim/TowerCatalog.cs, Assets/_Project/Scripts/Runtime/Sim/TowerWarsConfig.cs, Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs (TryUpgradeTowerAt, SellValue), Assets/_Project/Scripts/Runtime/Art/TowerStages.cs`

- Dano: DamageAtLevel = Dano x (1 + 0.85 x (nv-1)), ou seja x5.25 no nv 6 (Canhão 12 → 63).
- Custo de subir: UpgradeCost = round(0.8 x Custo) x nvAtual. Canhão 20/40/60/80/100, 300 no total até o nv 6. Para todas, o total é 15x a base: Morteiro 540, Gelo 480, Sentinela 600, Fogo 540, Ar 480.
- Escala por nível só no dano e em dois efeitos:
  - Gelo: lentidão 0.55^(1+0.12(nv-1)), cerca de 0.38 no nv 6. Frio por acerto 1+0.25(nv-1); congela com frio 3; congelamento 0.45+0.07(nv-1) s; depois 2 s de imunidade; o frio cai 0.4/s.
  - Fogo: +15% por nível por camada, até 26.25%/s da vida-base no nv 6 com 3 camadas.
- Alcance, cadência, área, fronteira e empurrão NÃO mudam com o nível.
- Vista: TowerStages tem 3 modelos (nv 1-2, 3-4, 5-6).
- Venda devolve 70% de tudo o que foi investido (SellRefund).

**Bichos de envio (SendCatalog.All, ids 0-8, ordem = teclas 1-9)** — `Assets/_Project/Scripts/Runtime/Sim/SendCatalog.cs`

Custo / vida / velocidade / +renda / recompensa / quantidade / atrito:
- 0 Rato: 22 / 15 / 2.8 / +2 / 2 / 4 / 1.4
- 1 Cachorro: 10 / 40 / 2.3 / +1 / 4 / 1 / 1
- 2 Lobo: 35 / 70 / 4.0 / +3 / 10 / 1 / 1
- 3 Javali: 30 / 110 / 2.3 / +3 / 9 / 1 / 1
- 4 Águia: 55 / 120 / 2.8 / +5 / 20 / 1 / 0 (voador: imune ao atrito)
- 5 Urso: 40 / 180 / 1.6 / +4 / 14 / 1 / 1
- 6 Tigre: 60 / 190 / 3.3 / +5 / 20 / 1 / 1
- 7 Rinoceronte: 75 / 330 / 1.7 / +7 / 27 / 1 / 1
- 8 Elefante: 90 / 450 / 1.3 / +8 / 34 / 1 / 1

Não existe nenhuma outra habilidade. Campos de SendUnit: Name, Cost, Hp, Speed, IncomeBonus, Bounty, Count, AttritionScale; IgnoresTerritory é derivado de AttritionScale <= 0. Renda comprada por ouro fica entre 0.083 e 0.1, ou seja, o envio se paga em uns 100-120 s.

**Economia** — `Assets/_Project/Scripts/Runtime/Sim/TowerWarsConfig.cs, Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs (TickIncome, TrySend, SpawnIncoming, SendScale)`

- Início: StartGold 120, StartLives 20.
- Renda: BaseIncome 10 a cada 10 s (IncomeTickSeconds). Cada envio soma IncomeBonus para sempre na lane de quem ENVIA.
- Abate: a recompensa (Bounty x SendScale, em int) vai para o dono da lane onde o bicho morreu, por tiro, queima ou atrito.
- Escalada: SendScale = 1 + 1.8 x min; depois de 7 min soma 3 x (min-7)² (morte súbita). Vale cerca de x10 aos 5 min e x47 aos 10 min, e multiplica vida e recompensa de quem nasce.
- Vazamento: custa sempre 1 vida (Lives--), qualquer que seja o bicho.
- Ouro de teste: DebugGrantGold e GrantGoldForSmokeTest.
- Placar exposto: TotalLeaked, KilledByTower/Attrition, SendsByType, TotalUpgrades, GoldSpentOn*.

**Território e atrito** — `Assets/_Project/Scripts/Runtime/Grid/TerritoryField.cs, Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs (TickEnemies, RebuildTerritory)`

- O território é a união de círculos BorderRadius (por tipo de torre) em volta de cada torre, por célula. É refeito em TryBuildTower e TrySellTowerAt (RebuildTerritory + TowerVersion++).
- Atrito: dentro do território o bicho perde 0.19 x MaxHp x AttritionScale por segundo (AttritionPctPerSecond é static mutável, varrido pelo FlowSim). Em porcentagem, isso mata qualquer bicho normal em ~5.3 s dentro da fronteira, em qualquer escala; o rato (1.4) em ~3.8 s.
- A Águia (0) é imune ao atrito, mas segue o MESMO flow field dos outros.
- TerritoryField.IsEdge desenha a linha de fronteira.
- A meta testada é atrito em pelo menos 12% das mortes.

**Combate** — `Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs, Assets/_Project/Scripts/Runtime/Grid/FlowField.cs, Assets/_Project/Scripts/Runtime/Grid/GridMap.cs`

- TickTowers: cada torre mira o inimigo ATIVO MAIS PRÓXIMO dentro do alcance; TrackAim mantém a mira só para a vista.
- Projétil teleguiado resolvido por TEMPO (distância / 14). Se o alvo morrer antes, o tiro se perde; não existe errar o alvo.
- Área: HitEnemy em todos dentro de SplashRadius do ponto de impacto.
- Ordem em HitEnemy: multiplicador contra voador → lentidão e frio (sem congelar na morte súbita) → camada de queima → empurrão (desligado na morte súbita) → dano.
- Anti-muro: CanBuild chama Flow.PlacementBlocksPath (Dijkstra) e recusa construir em cima de inimigo.
- Flow field: Dijkstra em 8 direções, custo 10/14, sem cortar quina; uma entrada (2,linha) e uma base (w-3,linha) num grid 24x16 (GameConfig.GridWidth/Height).

**Repasse de vazamento (LeakRouter)** — `Assets/_Project/Scripts/Runtime/Sim/LeakRouter.cs, Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs (SpawnCarried, DrainLeaks)`

- Quem cruza a base sai por _outgoing (só com CarryLeaks) e volta a correr na próxima lane viva que não seja a de quem enviou, mantendo a vida que tinha. Status (lentidão, queima etc.) zeram e Laps++.
- No 1x1 ele volta a correr na MESMA lane e custa mais 1 vida a cada volta.
- O código já suporta N lanes, mas MatchSim e MatchRunner fixam 2.

**IA (TowerWarsAi, por utilidade)** — `Assets/_Project/Scripts/Runtime/Sim/TowerWarsAi.cs`

Personalidades (intervalo de decisão s / SafetyMargin / Greed / CounterStrength / amostras de posição):
- Fácil: 1.45 / 1.5 / 0.85 / 0.5 / 10
- Normal: 1.2 / 1.4 / 1.0 / 1.0 / 18
- Difícil: 0.6 / 1.0 / 1.35 / 1.3 / 40

Decide():
- perigo = (vida inimiga na tela + 1) / (DPS + 1) x (1 + 1.5 x avanço do mais adiantado), dobrado com 5 vidas ou menos.
- Quer torre se perigo > SafetyMargin. Envia se não quiser torre ou se tiver 75 de ouro ou mais. Reforça a defesa com 100 ou mais.
- Até 6 torres só abre torre nova; depois compara upgrade e torre nova por DPS por ouro.

ChooseTowerType: sorteio ponderado (nota²). Nota = DPS por ouro + bônus de fronteira + lentidão x0.5 + empurrão x0.4, multiplicada pela leitura de ameaça: enxame (Count > 1), voador, rápido (Speed >= 3.5) e gordo (Hp >= 150). Torre repetida pesa (/(1+0.6 x quantas já tem)).

ChooseSend: sorteio ponderado de renda por ouro x GreedBias + pressão (vida x quantidade x velocidade / custo) + overkill do enxame + voador contra fronteira (o adversário tem 4 torres ou mais) − rato contra fronteira.

BuildSomewhere: amostra células perto do caminho e dá nota por cobertura, alongamento do labirinto (CostIfBlocked) e vizinhas.

A IA nunca vende torre e só enxerga um adversário (_foe).

**Determinismo e replay** — `Assets/_Project/Scripts/Runtime/Sim/MatchRunner.cs, Assets/_Project/Scripts/Runtime/Sim/MatchSim.cs, Assets/_Project/Scripts/Runtime/Sim/MatchCommand.cs, Assets/_Project/Scripts/Runtime/Sim/Replay.cs, Assets/_Project/Scripts/Runtime/TowerWars/TowerWarsController.cs (SaveReplay)`

- Passo fixo de 1/30 s (FixedStep). Um único System.Random(seed) da partida serve à IA, ao espalhamento de quem nasce e ao repasse.
- MatchRunner: comandos do jogador numa fila, aplicados no começo do Step(), e só então a IA joga. O evento CommandApplied grava (tique, comando) no Replay.
- Replay em texto: "tdfende-replay 1", depois seed, difficulty, grid, ticks, catalog <hash>, e uma linha "tique build|upgrade|sell x y [tipo]" ou "tique send id" por comando. Recusa tique fora de ordem, id inexistente e dificuldade desconhecida.
- StateFingerprint serve para comparar duas execuções.
- MatchSim (IA x IA) alterna quem decide primeiro a cada passo e tem teto de 900 s. MatchRunner (o jogo de verdade) não tem teto e depende da morte súbita.
- F9 grava o replay no jogo; `dotnet run -- replay <arquivo>` reproduz no FlowSim.

**Balanceamento editável sem recompilar** — `Assets/_Project/Scripts/Runtime/Sim/CatalogJson.cs, Assets/_Project/Scripts/Runtime/Sim/CatalogLoader.cs, Balanceamento/envios.txt, Balanceamento/torres.txt, Balanceamento/LEIA-ME.txt`

- CatalogLoader.LoadIfPresent é chamado no GameBootstrap e lê envios.txt e torres.txt de persistentDataPath/Balanceamento.
- Formato: uma linha por tipo, chave=valor separados por ';', InvariantCulture, UTF-8 com BOM.
- Validação: custo > 0, cadência > 0, alcance > 0, slow entre 0 (exclusivo) e 1, burn em [0,1), push em [0,3].
- Na primeira LaneSim os catálogos travam (Lock).
- TowerCatalog.LoadFrom completa por Name as torres de fábrica que o arquivo não cita. SendCatalog.LoadFrom troca a lista inteira e só exige que pelo menos um nome seja conhecido.
- Cópias de fábrica ficam em Balanceamento/ na raiz do repo; ModeSelect tem o botão 'Exportar balanceamento'.

**Eventos da sim para a vista** — `Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs, Assets/_Project/Scripts/Runtime/TowerWars/LaneView.cs, Assets/_Project/Scripts/Runtime/TowerWars/EnemyView.cs`

- EnemyDespawned(pos, DespawnReason: KilledByTower / KilledByAttrition / Leaked)
- TowerChanged(pos, nível)
- TowerFired(boca do cano)
- TowerSold(pos, ÍNDICE, reembolso): a vista remove o rig no mesmo índice e as torres seguintes descem uma posição.

A vista lê o resto por cópia de struct: TryGetEnemy (SimEnemy público, incluindo Burning, Frozen, SlowLeft, Laps, SenderId), TryGetProjectile, TowerCell/Level/TypeId, TryGetTowerAim. LaneView assina os eventos no construtor e solta em Dispose.

**Testes e laboratório headless** — `Tools/FlowSim/Program.cs, Tools/FlowSim/BalanceLab.cs, Tools/FlowSim/FlowSim.csproj, Tools/CompileCheck/, .githooks/pre-commit`

- O FlowSim compila os MESMOS .cs (lista explícita no csproj) com UnityStubs.
- 183 Check(): grid, flow field, território, economia, cada torre vencendo seu alvo (Morteiro x Rato, Canhão x Elefante sozinho, Sentinela x Águia por tempo até matar, Gelo e Ar atrasando mais que o Canhão na mesma célula, Fogo x Elefante, Canhão x Cachorro contra o Fogo), upgrade, venda, repasse, replay byte a byte e determinismo.
- Metas estatísticas:
  - pelo menos 75% das partidas decididas e pelo menos 75% por morte;
  - atrito em pelo menos 12% das mortes;
  - Difícil ganha do Fácil em pelo menos 2/3 de 30 partidas;
  - Normal ganha do Fácil entre 60% e 90%;
  - espelho estoura o tempo em no máximo 25%;
  - nenhum envio passa de 60% das compras e pelo menos 4 tipos têm 5% ou mais;
  - a IA sobe torres que não são Canhão.
- Modos: match N, sweep N [fine], dump-catalogs, replay.
- CompileCheck compila Runtime/** contra as DLLs reais. O pre-commit em .githooks/pre-commit (core.hooksPath=.githooks) roda CompileCheck, CompileCheckUrp e FlowSim.

**GameConfig (TD clássico, separado)** — `Assets/_Project/Scripts/Runtime/Core/GameConfig.cs, Assets/_Project/Scripts/Runtime/Core/GameController.cs, Assets/_Project/Scripts/Runtime/Units/Enemy.cs, Assets/_Project/Scripts/Runtime/Towers/Tower.cs`

- Grid 24x16, CellSize 1. GridWidth/Height também são usados pelo Tower Wars e pelo Replay.
- Clássico: StartGold 100, vidas 20, uma torre só (custo 25, alcance 3.5, cadência 0.65, dano 12), BorderRadius 2.75, AttritionDps fixo de 4/s (dano fixo, não porcentagem), KillReward 5.
- Ondas: vida 30 x 1.18^(onda-1), velocidade 2.2 + 0.03 x onda (máximo 3.6), EnemiesInWave = 5 + 3 x onda, bônus de 25 por onda limpa.
- Roda em GameController + Units/Enemy.cs + Towers/Tower.cs com UnityEngine.Random e Time.deltaTime. NÃO usa LaneSim, TowerCatalog nem SendCatalog.

### Lacunas

- **[alto] Torre não tem vida e inimigo não ataca: não existe base para o 'segundo mercado' de inimigos que atacam torres.** — - struct SimTower (LaneSim.cs:50-59) tem só Cell, Pos, Cooldown, Level, TypeId, Aim, HasAim. - SendUnit (SendCatalog.cs:8-20) não tem dano, alcance nem cadência de ataque. - SimEnemy não tem estado de ataque; CurrentSpeed só considera Frozen e Slow. - A única saída de uma torre é TrySellTowerAt, com o evento TowerSold por índice. - O _bestUpgradeIndex é zerado manualmente na venda e teria que ser zerado também na destruição.
- **[alto] Evolução sem ramificação: nível é um inteiro linear e só o dano escala.** — - SimTower.Level é int de 1 a 6. - DamageAtLevel é linear (+85% por nível) e UpgradeCost = round(0.8 x custo) x nível. - MatchCommand.Upgrade(x, y) não tem parâmetro de ramo, e Replay.TryParseCommand espera 'upgrade x y'. - Alcance, cadência, área, fronteira e empurrão não mudam com o nível. - A vista só tem 3 estágios (TowerStages).
- **[alto] Conteúdo acoplado por ÍNDICE do catálogo: inserir ou reordenar torre ou bicho troca modelo, efeito e teste em silêncio.** — - Vista: ModelLib.Tower/Enemy/Projectile fazem switch por typeId (ModelLib.cs:93-125); Vfx.Muzzle/Impact/AddTrail têm case 1..5 (Vfx.cs:87-193); ProjectileView.cs:56 faz _arc = TowerTypeId == 1; ModelLibTiers.cs:29 e :112 fazem switch por tipo. - Testes usam ids fixos: TrySend(2) com o comentário 'Corredor' (hoje é o Lobo), DamageDealt(8), TimeToKill(4), TrySend(5). - O replay grava ids inteiros. - Um torres.txt com as linhas em outra ordem troca os modelos das torres. - Um id fora do switch cai no Cachorro ou no Canhão.
- **[alto] Inimigos sem habilidades: só vida, velocidade, quantidade e imunidade ao atrito. O 'voador' anda pelo labirinto.** — - SendUnit tem 8 campos e nenhum de habilidade: sem armadura, resistência, regeneração, escudo, divisão, invisibilidade, curandeiro, aura ou chefe. - TickEnemies usa Flow.SampleDirection para todos, inclusive IgnoresTerritory, então a Águia contorna as torres como os outros. - Todas as torres acertam voador; o que muda é só o multiplicador VsFlyingMultiplier.
- **[alto] Não há tier, mercado, desbloqueio, estoque nem recarga de envio. O segundo mercado precisa de regra nova.** — - CanAfford(sendId) = !Dead && Gold >= Cost (LaneSim.cs:509). - TrySend gera os bichos na hora. - Nenhum campo de Tier ou Unlock em SendUnit ou TowerType. - A IA ChooseSend considera o catálogo inteiro.
- **[alto] Catálogos sem metadado para a loja ou seleção bonita (descrição, papel, 'responde a', ícone, raridade, tier).** — - TowerType e SendUnit só têm Name de texto. - Os textos de papel ('Responde a') existem só em comentários do código e no README. - O HUD OnGUI monta o rótulo a partir de Name, Cost e IncomeBonus (TowerWarsController.cs:402, 420).
- **[alto] Interface não escala com mais conteúdo: teclas 1-9 e barras de largura linear.** — - HandleSendInput: 'i < SendCatalog.Count && i < 9' (TowerWarsController.cs:327). - SendPanelRect usa Count x (118+6) px, ou seja 1116 px com 9 bichos. - TowerPanelRect usa Count x (132+6) px. - A troca de torre é Q/E cíclica. Não há abas nem agrupamento por mercado.
- **[alto] TD clássico é uma implementação paralela e antiga: nenhum conteúdo novo (torres, bichos, níveis, efeitos) chega nele.** — - GameController usa GameConfig.TowerCost/TowerDamage, uma torre só (ModelLib.Tower(0)) e um inimigo só (ModelLib.Enemy(0)). - Enemy.cs:88 aplica atrito como GameConfig.AttritionDps fixo de 4/s, não em porcentagem. - Usa UnityEngine.Random e Time.deltaTime: não é determinístico nem testado no FlowSim.
- **[medio] A torre mira sempre o mais próximo: não há modo de mira, e a Sentinela não prioriza voador.** — - TickTowers (LaneSim.cs:706-720) escolhe o menor sqrMagnitude entre todos os ativos. - Não existe primeiro/último/mais forte/voador primeiro, nem restrição chão/ar.
- **[medio] A assinatura de catálogo do Replay não cobre todos os campos: mudar queima ou empurrão no torres.txt reproduz outra partida sem avisar.** — - Replay.CurrentCatalogSignature (Replay.cs:191-222) faz hash de Cost, Range, Cooldown, Damage, SplashRadius, SlowFactor, SlowSeconds, VsFlyingMultiplier e BorderRadius. - Ficam de fora BurnPctPerSecond, BurnSeconds, Knockback, os nomes e os statics mutáveis TowerWarsConfig.AttritionPctPerSecond e SendScalePerMinute.
- **[medio] SendCatalog.LoadFrom não completa o catálogo com os padrões e SendCatalog.Get não valida o id.** — - SendCatalog.cs:92 faz 'All = parsed'. Um envios.txt exportado com 9 bichos apaga o 10º quando ele existir, e mudar a ordem desalinha ModelLib.Enemy. - TowerCatalog.LoadFrom completa por Name. - SendCatalog.Get(id) => All[id] sem checagem (TowerCatalog.Get cai no 0).
- **[medio] Vazamento custa 1 vida, qualquer que seja o bicho.** — - LaneSim.cs:677 faz 'Lives--' para todo vazamento. Um Elefante de 90 de ouro dói o mesmo que um Rato. - Para inimigos caros do segundo mercado faltaria um campo LivesCost ou um dano à fortaleza.
- **[medio] Cada mecânica nova exige um termo manual na IA e reequilibrar os testes estatísticos.** — - ChooseTowerType soma termos fixos por campo: (1-SlowFactor) x 0.5, Knockback x 0.4, burnDps com 120 de vida típica. - A leitura de ameaça usa limiares fixos: Speed >= 3.5 e Hp >= 150. - Os testes pedem Normal ganhando do Fácil entre 60% e 90%, nenhum envio acima de 60% e pelo menos 4 tipos com 5% ou mais. - A IA nunca vende torre.
- **[medio] Faltam eventos de simulação para som e feedback rico.** — - Só existem EnemyDespawned, TowerChanged, TowerFired e TowerSold. - Não há EnemySpawned, EnemyHit, congelou, pegou fogo, TowerDestroyed nem fim de partida. - EnemyView descobre o acerto comparando a vida (EnemyView.cs:113) e o congelamento por estado. - O som (problema 8) precisa desses ganchos.
- **[medio] Determinismo em float: serve para replay no mesmo binário, mas não é garantido entre runtimes ou plataformas (multiplayer ou mobile ARM).** — - A sim usa float e Math.Pow/Math.Sqrt (HitEnemy, FireProjectile). - O FlowSim roda em CoreCLR net10.0 e o jogo em Mono/IL2CPP; o teste de replay só compara execuções no mesmo runtime.
- **[medio] Mapa fixo: uma entrada, uma base, 24x16 e nenhum dado de mapa.** — - O construtor de LaneSim fixa _goalCell = (width-3, row) e spawn = (2, row). - MatchRunner recebe GameConfig.GridWidth/Height. - Não existe formato de mapa nem obstáculos pré-colocados.
- **[baixo] Estado global mutável estático impede regras por partida (mutadores ou modos) e simulações em paralelo.** — - TowerCatalog.All, SendCatalog.All e os flags Locked são static. - TowerWarsConfig.AttritionPctPerSecond e SendScalePerMinute são static e mutáveis; os testes os trocam com try/finally. - LeakRouter.Buffer é static.
- **[baixo] Partida limitada a 2 lanes e uma IA com um único adversário.** — - MatchSim (A, B) e MatchRunner (Player, Foe) fixam 2 lanes; TowerWarsAi guarda um só _foe. - LeakRouter já suporta N lanes.
- **[baixo] Inconsistências entre MatchRunner e MatchSim.** — - MatchRunner.Over só olha morte, sem teto de 900 s. - MatchRunner aplica o jogador e depois a IA no mesmo passo; o MatchSim alterna a ordem a cada passo.
- **[baixo] Documentação e comentários desatualizados.** — - README fala em '66 verificações' (hoje são 183 Check), teclas '1-6' e 'quatro tipos de torre / seis tipos de inimigo'. - O resumo de TowerType.BurnPctPerSecond diz 'vida MÁXIMA', mas a sim usa BaseHp. - A mensagem do teste de roster diz '/6 tipos'. - TowerWarsConfig.BorderRadius (2.75) não é usada pelo Tower Wars. - GameConfig ainda diz 'fase 0'.
- **[baixo] Custo de desempenho quadrático por tique.** — - TickTowers é O(torres x vagas de inimigo, 256 ou mais), e a área percorre todas as vagas. - A IA no Difícil faz 40 amostras, cada uma com 2 Dijkstras (CanBuild e CostIfBlocked). - Hoje está ok; pode pesar com enxames grandes ou invocações.

### Pontos de extensão

- TORRE NOVA (sempre no fim da lista, id estável): 1. Novo TowerType em TowerCatalog.All. 2. Se tiver efeito novo: campo no TowerType, em CatalogJson.SerializeTowers/TryParseTowers (com validação), em Replay.CurrentCatalogSignature e em LaneSim.HitEnemy, ou em TickTowers se for um modo de disparo. 3. Termo de nota em TowerWarsAi.ChooseTowerType. 4. Vista: ModelLib.Tower/Projectile, ModelLibTiers.GeomFor/AddTiers, Vfx.Muzzle/Impact/AddTrail e ProjectileView._arc. 5. Modelos em Resources/TDFende/Torres/Torre_<Nome>_{1,2,3} (TowerStages.ModelName). 6. Teste no FlowSim com Duel, DamageDealt, TimeToKill ou DeepestXWith provando que ela vence algum envio melhor que o Canhão. 7. O HUD lista sozinho por TowerCatalog.Count.
- BICHO NOVO (no fim da lista): 1. Novo SendUnit em SendCatalog.All. 2. Vista: case em ModelLib.Enemy e modelo 'Inimigo_<Nome>' (AnimalLoader procura por def.Name). 3. A IA ChooseSend já pontua por estatística. 4. Testes de roster (nenhum acima de 60%, pelo menos 4 com 5% ou mais). 5. Hoje só os 9 primeiros têm tecla.
- EFEITO NOVO DE INIMIGO (armadura, regeneração, escudo, divisão, chefe): - Campo em SendUnit e em SimEnemy. - Cópia em SpawnIncoming e reset ou manutenção em SpawnCarried. - Lógica em TickEnemies ou HitEnemy. - Campo em CatalogJson e na assinatura do Replay. - Leitura de ameaça em ChooseTowerType. - Teste no FlowSim.
- COMANDO NOVO (ramo de upgrade, comprar tier, desbloquear mercado, habilidade): - CommandKind, fábrica e ToString em MatchCommand. - Replay.TryParseCommand: argumento opcional no fim, como o tipo do 'build', para continuar lendo replays antigos. - MatchRunner.Apply e um LaneSim.TryX(...). - Entrada em TowerWarsController enfileirando em _runner.Enqueue.
- INIMIGO QUE ATACA TORRE: - Hp/MaxHp em SimTower; AttackDamage, AttackRange e AttackCooldown em SendUnit e SimEnemy, com estado 'atacando' que zera a velocidade. - Extrair de TrySellTowerAt um RemoveTowerAt(i), comum à venda e à destruição: Map.SetBlocked false, Flow.Rebuild, RebuildTerritory, TowerVersion++ e _bestUpgradeIndex = -1. - Disparar um evento TowerDestroyed com o MESMO índice do TowerSold, para o LaneView reaproveitar OnTowerSold e AnimateRazing. - Atenção: torre destruída abre caminho e encolhe a fronteira, o que interage direto com a tese do atrito.
- RAMIFICAÇÃO DE EVOLUÇÃO: - Campo Branch em SimTower e tabela de modificadores por tipo e ramo, por exemplo TowerCatalog.Branches[typeId][ramo], com escalas de alcance, cadência, área e efeito. - DamageAtLevel e HitEnemy passam a ler o ramo. - Comando Upgrade(x, y, ramo) com argumento opcional no replay. - UpgradeCostAt devolve o custo por ramo para a loja. - TowerStages e ArtFactory.StageFor escolhem o modelo por ramo.
- SEGUNDO MERCADO / TIERS: - Campo Tier (ou Market) e regra de desbloqueio em SendUnit e TowerType: por MatchTime, por renda acumulada ou por comando de compra de 'desbloqueio'. - Checagem em CanAfford e CanBuild, filtro na IA (ChooseSend e ChooseTowerType) e agrupamento na UI. - Talvez um LivesCost por unidade em vez de 'Lives--'.
- METADADOS DE UI E LOJA: - Campos Description, Role ('responde a'), IconKey e Tier nos structs. Podem morar num catálogo paralelo só de vista, indexado por Name, para não afetar a sim nem a assinatura. - A loja pode derivar as estatísticas (DPS = DamageAtLevel / Cooldown, alcance, fronteira, renda por ouro) direto do catálogo.
- GANCHOS PARA SOM E FEEDBACK: - Novos eventos em LaneSim, por exemplo EnemySpawned(tipo), EnemyHit(slot, tipo da torre), Frozen, Ignited, TowerDestroyed e MatchEnded. - Assinar no construtor do LaneView e soltar em Dispose.
- BALANCEAMENTO SEM RECOMPILAR: - Balanceamento/envios.txt e torres.txt via CatalogLoader no boot. - Medição: Tools/FlowSim com `dotnet run -- match 60` e `sweep 25 fine`. - Constantes globais em TowerWarsConfig.

### Restrições

- A pasta Sim/ tem que continuar C# puro: sem MonoBehaviour, sem UnityEngine.Random, sem Time.*. Do UnityEngine só os tipos Vector3 e Vector2Int que existem em Tools/FlowSim/UnityStubs.cs. Qualquer outra API do Unity quebra o FlowSim.
- Arquivo novo de lógica tem que entrar à mão na lista <Compile Include> de Tools/FlowSim/FlowSim.csproj. O CompileCheck pega Runtime/** sozinho, menos Urp.
- Determinismo: - toda aleatoriedade sai do Random da partida, chamado sempre na mesma ordem; - estado só muda dentro de Tick() ou via MatchCommand na fronteira de tique; - a vista nunca escreve na sim e só lê cópias de struct.
- Os catálogos travam na primeira LaneSim. Vetores são dimensionados por Count na construção: TowerWarsAi._towerScores, _owned e _sendScores, e LaneSim.SendsByType.
- Ids de torre e de bicho só podem ser ACRESCENTADOS no fim. Vista (ModelLib, Vfx, ProjectileView, ModelLibTiers), testes e replays guardam ids inteiros.
- Replay: manter o cabeçalho 'tdfende-replay 1' legível. Argumento novo entra como opcional no fim da linha. Todo campo novo de catálogo entra em Replay.CurrentCatalogSignature.
- Morte súbita (depois de 7 min) existe para garantir que a partida ACABE. Controle novo (congelar, empurrar, atordoar, curar) precisa da guarda InSuddenDeath. Efeito em porcentagem da vida tem que usar BaseHp, não MaxHp, para não anular a escalada (fogo já faz assim).
- O pre-commit (.githooks/pre-commit) roda CompileCheck, CompileCheckUrp e FlowSim, uns 3 min. As metas estatísticas têm que continuar verdes a cada conteúdo novo: - Difícil ganha do Fácil em pelo menos 2/3; - Normal ganha do Fácil entre 60% e 90%; - espelho estoura o tempo em no máximo 25%; - atrito em pelo menos 12% das mortes; - pelo menos 75% das partidas decididas; - nenhum envio acima de 60% e pelo menos 4 tipos com 5% ou mais.
- Regras do Felipe: - teste escrito antes e visto falhar; - nada entregue com teste vermelho; - commit e push direto na main, com git pull --rebase antes; - commits e comentários em português; - modelo 3D: procurar pronto (CC0 ou CC BY, conferindo licença) antes de gerar no Meshy, e perguntar antes de gastar créditos (~30 por modelo com textura).
- Mobile-ready: a entrada passa por IGameInput (DesktopInput). Loja e seleção novas não podem depender só de teclado (hoje são 1-9 e Q/E).
- O TD clássico (GameController, GameConfig, Units/, Towers/) não compartilha a sim. Conteúdo novo só aparece nele se ele for portado para LaneSim com um gerador de ondas, ou se for aposentado.

## Arte e visual (Runtime/Art, World, Visuals, Urp/PostFx, Resources/TDFende + Resources/Art, pipeline Tools/)

O visual é um híbrido em camadas, resolvido em runtime por nome de arquivo em Resources. A base é 100% procedural: ModelLib + ModelLibAnimals + ModelLibTiers + MeshBuilder (~2.300 linhas) montam as 6 torres, os 9 bichos, projéteis, fortaleza, acampamento, muralha, tendas, pinheiros, carvalhos, arbustos e pedras. A escala é 1 unidade = 1 célula ≈ 3 m. ProcTex gera 18 texturas e 14 fotos CC0/MIT entram via MatSpec.External. Por cima disso, ArtFactory.Spawn troca peças por asset baixado, nesta ordem: bicho (AnimalLoader), personagem Mixamo (código morto), estágio de torre Torres/<Tipo>_<1..3>, depois prefab por nome. Hoje o asset baixado cobre: 18 torres (Karrades, CC0 da comunidade Meshy, 10,6k–95k tris, sem LOD), Fortaleza (92,6k), Acampamento, 9 bichos (7 Sketchfab CC BY e 2 do Meshy, 9k–16k tris, Legacy), pedras, tocos, troncos, barris e caixas da Poly Haven, chão em Terrain com 2 camadas Poly Haven 2k, grama 3D Poly Haven (7.448 touceiras ≈ 4,0M tris) e céu HDRI. Árvores, tendas de fundo, muralha e mureta continuam procedurais. Os prints reais do executável (Temp/tdf_g2.png e tdf_g2_grama.png) e o Player.log de 06/10 13:20 mostram o problema: ~80% da tela é chão (a câmera fica a 36–58°). Esse chão é ocre, com ladrilho visível e grade branca por cima, e a grama 3D aparece como pontos escuros e ralos. As árvores são cones e esferas facetados. Os bichos somem (o log diz que os 9 carregaram e estão 'animado'). As tendas são cones vermelhos e azuis de brinquedo. Torres e fortaleza ficam miúdas perto de um elefante escalado para 1,96 unidade. O FPS médio cai de 104 para 54 numa RTX 4070 Ti quando a grama sobe de 4.699 para 7.448 touceiras. Não há LOD, nem tiers de qualidade (um único URP_Asset para os 6 níveis, Android incluso), nem compressão nas texturas carregadas em runtime. Os efeitos são bolhas sem textura. O pedido relayado do Felipe ('realista, não precisa necessariamente ser medieval') tira a amarra medieval: a Sim não depende de tema, então trocar o tema é só trabalho de arte (ModelLib, torres.json/buscas.json, BaixarArte.ps1).

### O que existe

**ArtFactory: ponte procedural → motor e porta de troca por asset** — `Assets/_Project/Scripts/Runtime/Art/ArtFactory.cs`

Spawn(def, team, parent, name, stage), com esta precedência: (1) def.Name começa com 'Inimigo_' → AnimalLoader.TrySpawn; (2) AnimKind.Walker → CharacterLoader (Mixamo, sem arquivos hoje); (3) prefab Resources/TDFende/Torres/<nome>_<estágio> (StageFor/TowerStages); (4) TDFende/<nome>; (5) Torres/<nome>; (6) peças do ModelDef. Um prefab com peças nomeadas (Shaft, Top, Turret, Barrel, Flag, LegL...) vira híbrido: a torre baixada traz Shaft/Top, e torreta, cano, base e enfeites Lv{n} vêm do código. SkinBody veste o corpo baixado com <nome>_cor/_normal.bytes de Torres/Textures (smoothness fixo 0,15, emissão ligada para o SetGlow). Material URP Lit por (ArtMat, cor do time), em cache, com _NORMALMAP e _EMISSION sempre ligados. Preload gera as texturas em paralelo; o log do build mostra '14 fotográficas + 18 procedurais em 1051 ms'.

**Modelos procedurais (fallback e peças do híbrido)** — `Assets/_Project/Scripts/Runtime/Art/ModelLib.cs, ModelLibAnimals.cs, ModelLibTiers.cs, MeshBuilder.cs`

ModelLib.cs (1175 linhas): Tower(typeId) para 6 torres, Enemy(typeId) para 9 bichos, Projectile, Keep 'Fortaleza', Camp 'Acampamento', CastleWall, ArmyCamp (tendas com a cor do time + fogueiras), Pine (4 cones Lathe de 8 lados), Oak (4–5 esferas 8x6), Boulder, Bush. ModelLibAnimals.cs (405): quadrúpedes por parâmetros Quad (Hip, Len, W, H...), que definem Def.Height; o AnimalLoader usa esse Height como alvo de escala do bicho baixado. ModelLibTiers.cs (332): enfeites Lv2..Lv6 por torre. Escala declarada: 1 unidade = 1 célula ≈ 3 m, soldado ≈ 0,6.

**Materiais: MatSpec, ProcTex e fotos** — `Assets/_Project/Scripts/Runtime/Art/ProcTex.cs, Assets/Resources/TDFende/Textures/`

O enum ArtMat tem 33 materiais. MatSpec.Of define cor sRGB, smoothness, metallic, UnitsPerTile e emissão (Rune e Ember com emissão >1 para o bloom). MatSpec.External mapeia 14 materiais para fotos em Resources/TDFende/Textures/*.bytes (JPG): O3DE MIT/Apache, Poly Haven, ambientCG e RoofTile/Slate do BaixarArte. ArtFactory.GradeToPalette recolore a média da foto para a cor do MatSpec. O resto é gerado em ProcTex (256 px; Grass 512). MatSpec fica dentro de ProcTex.cs (não existe arquivo MatSpec).

**Torres baixadas em 3 estágios** — `Assets/Resources/TDFende/Torres/, Assets/_Project/Scripts/Runtime/Art/TowerStages.cs, Tools/ConverterTorres/comunidade.json`

TowerStages: Count=3, LevelsPerStage=2 (níveis 1-2/3-4/5-6), lógica pura testável no FlowSim. São 18 FBX Torre_<Canhao|Morteiro|Gelo|Sentinela|Fogo|Ar>_<1..3>, todos do autor Karrades (comunidade Meshy, CC0), com 10.576 (Gelo_1) a 95.078 (Gelo_3) triângulos. Fortaleza.fbx (Matson, 92.577 tris) e Acampamento.fbx (nathi.mashabane, 9.994 tris) usam o modo 'inteiro'. Também existe a Torre_Canhao.fbx antiga, gerada no Meshy pela conta do Felipe. Textura _cor 1024 JPG em .bytes; só 3 de 21 têm _normal (Torre_Canhao, Gelo_3, Sentinela_3). Pasta: 50 MB.

**Bichos baixados com esqueleto (AnimalLoader)** — `Assets/_Project/Scripts/Runtime/Art/AnimalLoader.cs, Assets/Resources/TDFende/Bichos/, Assets/_Project/Scripts/Editor/AnimalImportRules.cs`

Identifica o bicho pela palavra-chave no nome do arquivo (tabela Keywords, PT/EN, mais a lista NotThese). Escala o modelo para Def.Height do procedural; ave pela envergadura 0,9 e lift de 1,1. Põe o pé no chão, troca Animator por Animation legacy e renomeia os clipes para Walk/Run/Idle/Death. RootLock prende o osso-raiz; GaitBob faz a passada quando não há clipe. FixSurface força opaco, ou recorte se a textura termina em '_alfa', e liga _EMISSION em cópias dos materiais. Por cima vai o anel do time (Overlays.Ring). Arquivos: rato/cachorro/lobo/aguia/urso/rinoceronte/elefante (Sketchfab CC BY) e javali/tigre (Meshy, rig quadrúpede, só a ação 'baselayer' → Walk). Orçamento de 9.000–16.000 tris, texturas 1024; 4 bichos usam recorte por alfa (rato_1, cachorro_2, lobo_5, aguia_1).

**ModelRig: animação procedural e por clipe** — `Assets/_Project/Scripts/Runtime/Art/ModelRig.cs`

Torre mira (AimAt), dá coice (Kick), cresce com o nível (SetLevel/GrowFromLevel/ShaftHeight medido no modelo baixado), mostra peças Lv{n}, gira Rotor (Ar) e acende FirePoints (Fogo). Inimigo tem Walk/Gallop/Roll/Glide procedurais, ou AnimateClips com velocidade casada ao deslocamento (0,5–2,2x) e PlayDeath. SetGlow usa MaterialPropertyBlock na emissão; SetHealth desenha a barra de vida.

**Chão: Terrain nativo (GroundBuilder)** — `Assets/_Project/Scripts/Runtime/World/GroundBuilder.cs, Assets/Resources/Art/Ground/`

Terrain de 220x220, heightmap 129, alphamap 512, MaxHeight 2. Duas TerrainLayer Poly Haven 2k: aerial_grass_rock (lane, tile 4x4) e leafy_grass (campo, tile 3x3), misturadas com manchas de Perlin. Plano exato em y=0 até FlatMargin=10 em volta das lanes; ondulação de BumpHeight 0,35 fora disso. Collider desligado (o clique é raio contra plano). Material via Shader.Find('Universal Render Pipeline/Terrain/Lit') com _TERRAIN_INSTANCED_PERPIXEL_NORMAL. drawInstanced=false (mudança não commitada: com instancing o terreno saía preto no build). basemapDistance 1000. Se a arte faltar, devolve null e o WorldLayout desenha relevo, rio e chão procedurais.

**Grama 3D instanciada (GrassField)** — `Assets/_Project/Scripts/Runtime/World/GrassField.cs, Assets/Resources/Art/Grass/`

grass_medium_01/02 da Poly Haven (1k, diffalpha com mipMapsPreserveCoverage via ArtImportRules). Cada nó do 'mostruário' vira uma variante (17 no último build). Espalhamento por orçamento: TriangleBudget 4.000.000, MaxInstances 20.000, ScatterRadius 80, Falloff 16, LaneClearance 0,4, TargetHeight 0,42, sorteio com peso 1/tris. Graphics.RenderMeshInstanced a cada Update em lotes de 1023, sem sombra, worldBounds de um cubo de 170. Log: '7448 touceiras de 17 variantes, ~4,0 M triângulos'. Cor de base puxada para (1.15, 1.35, 0.95) (não commitado).

**Cenário em volta (WorldLayout + WorldView + SceneryModels)** — `Assets/_Project/Scripts/Runtime/Art/WorldLayout.cs, Visuals/WorldView.cs, Visuals/SceneryModels.cs, Assets/Resources/TDFende/Cenario/`

WorldLayout.ScatterScenery faz 560 sorteios (Pine/Oak/Bush/Boulder/Stump), com densidade crescente com a distância, mais 90 pedras de margem. WorldView.Build põe modelo da Poly Haven onde a categoria existe (MaxModelTrees=60 dentro de 22 unidades, TreeWind) e combina o resto numa malha 'Cenario'. Também põe a Mureta de pedras-caixa em volta das lanes, um depósito de barris/caixas/troncos junto da fortaleza e o pano de fundo procedural (CastleWall com a cor do dono, ArmyCamp com a cor do atacante, CampFires via Vfx.Brazier). SceneryModels junta as peças sobrepostas de um FBX em variantes e recusa folhagem sem alfa. Categorias presentes no build: Pedras 11 variantes, Tocos 2, Troncos 3, Barris 3, Caixas 4. Arvores não existe.

**Luz, céu e pós-processamento** — `Assets/_Project/Scripts/Runtime/Visuals/SceneAmbience.cs, Assets/_Project/Scripts/Runtime/Urp/PostFx.cs, Assets/Settings/URP_Asset.asset, Assets/Settings/URP_Renderer.asset`

SceneAmbience: céu HDRI kloofendal_48d_partly_cloudy_puresky_2k (Skybox/Panoramic), ambiente pelo skybox com intensidade 0,5, um sol 'Sol' de intensidade 1,6 em (52°, -35°), cor (1, .955, .885), sombra Soft 0,85, névoa linear de 55 a 170 em (.70, .78, .86). Fallbacks: Skybox/Procedural, depois cor chapada. PostFx (assembly opcional TDFende.Urp): ACES, Bloom 0,25 (thr 1, scatter 0,65), Vignette 0,18, ColorAdjustments (postExposure 0,1, contrast 6, saturation 4), SMAA High. URP_Asset: HDR ligado, MSAA desligado, renderScale 1, sombra 4096 com 4 cascatas, distância 80, soft high, SRP Batcher ligado, GPU Resident Drawer desligado. URP_Renderer: SSAO (DepthNormals, raio 0,3, intensidade 1,4) via Editor/SsaoSetup (mudança não commitada).

**Efeitos, sobreposições e retorno visual** — `Assets/_Project/Scripts/Runtime/Visuals/Vfx.cs, Overlays.cs, Juice.cs, FloatingText.cs, Assets/_Project/Shaders/SoftParticle.shader, TerritoryOverlay.shader`

Vfx tem 12 ParticleSystems em espaço de mundo (Clarão, Fumaça, Poeira, Lascas, Faíscas, Geada, Chama, Fagulha, Vento, Névoa...), com emissão manual Emit e maxParticles 800 cada. As receitas são por tipo de torre (Muzzle/Impact(towerType), Burn, Freeze, Thaw, KillBurst por atrito com cor própria). O shader TDFende/SoftParticle é uma bolha redonda gerada pela UV, sem textura e sem luz. Outras peças: Overlays (grade, fantasma, anel de alcance com o shader TerritoryOverlay), Juice (tremor de tela), FloatingText e a barra de vida no ModelRig.

**Pipeline de conversão de torres (Meshy → FBX)** — `Tools/ComunidadeMeshy/, Tools/ConverterTorres/{converte.py,verifica.py,baixa.py,torres.json,comunidade.json}, Assets/_Project/Scripts/Editor/TowerImportRules.cs`

ComunidadeMeshy/busca.py usa a API pública de showcases e só aceita CC0. Os termos vêm de buscas.json (medievais: 'cannon tower', 'castle keep', 'medieval tent'...). Gera folhas de miniaturas com número, autor e tris. O GLB se baixa logado no site para ConverterTorres/glb/. converte.py (Blender bpy): fica com a maior peça, centra, aplica giro, escala por raio/altura do torres.json e corta em Shaft/Top na altura do piso da torreta (piso auto/topo); no modo inteiro sai uma malha 'Corpo'. Texturas em JPG 1024 com tom por canal, gravadas como .bytes. verifica.py desenha níveis 1 e 6 usando o art.json do ArtPreview. Copiar out/ para Resources/TDFende/Torres. TowerImportRules: sem material, sem animação, escala 1. baixa.py precisa de MESHY_API_KEY para tarefas próprias.

**Pipeline de conversão de bichos (Sketchfab/Meshy → FBX)** — `Tools/ConverterBichos/{bichos.json,baixa.py,converte.py,estatico.py,verifica.py}, Tools/BaixarBichos.ps1, .gitignore`

bichos.json guarda por bicho: uid, acoes→Walk/Run/Idle/Death, tris, drop/keep, opacos, frente/giro/cabeca/rabo. baixa.py usa o token da API do Sketchfab. converte.py: só malhas com esqueleto; só as ações pedidas, renomeadas; esqueleto como raiz; frente para +Z do Unity; decimate só nas malhas opacas até o orçamento; texturas até 1024 com sufixos _alfa/_normal; tira ossos sem peso; exporta FBX com animação assada. estatico.py cobre bicho sem rig (~15k tris, GaitBob). verifica.py renderiza o andar. Para entrar no Git é preciso também a linha de exceção no .gitignore (Bichos/* é ignorado por padrão).

**Download de cenário CC0 (Poly Haven)** — `Tools/BaixarArte.ps1, Tools/Sincronizar.ps1`

BaixarArte.ps1 usa api.polyhaven.com. $plan define categoria → regex por id com Max (Arvores 5, Pedras 6, Tocos 3, Troncos 3, Barris 3, Caixas 3); $exclude barra coisa moderna ('plastic|metal' nas caixas). Baixa o FBX 1k com as texturas incluídas e, para RoofTile/Slate, o JPG 1k como .bytes. A sincronização automática (InstalarSincronizacao/Sincronizar.ps1) roda de novo quando o script muda.

**Verificação visual** — `Tools/ArtPreview/, Assets/_Project/Scripts/Runtime/Core/SmokeCapture.cs, Assets/_Project/Scripts/Editor/BuildJogo.cs`

Tools/ArtPreview (dotnet + three.js) compila só MeshBuilder, ProcTex, ModelLib*, WorldLayout e desenha o procedural (?scene=world). SmokeCapture ('TDFende.exe -captura x.png') entra no Tower Wars, manda um de cada bicho, mede o movimento, loga a cena (LogScene: shader/keywords do terreno, céu, ambiente, sol) e tira o print. BuildJogo.ShaderKeep cria 14 materiais em Resources/TDFende/ShaderKeep para segurar as variantes que o código liga via Shader.Find.

### Lacunas

- **[critico] Bichos invisíveis no executável (só o anel do time aparece). A causa não está confirmada.** — Player.log de 06/10 13:20 lista os 9 bichos carregados ('bichos de verdade: Inimigo_Aguia, ...') e todos 'animado' no log de movimento, mas Temp/tdf_g2.png mostra só anéis e pontinhos na lane. O mesmo log repete ~75 vezes 'Default clip could not be found in attached animations list.': AnimalLoader.AddClips (linhas 178-181) faz RemoveClip de todos os clipes do Animation importado e não reatribui anim.clip. Hipóteses a instrumentar com o LogEnemy (que está quebrado): (1) Bounds(inst) lê SkinnedMeshRenderer.bounds logo após o Instantiate (AnimalLoader.cs:123) e pode divergir no player, gerando escala k errada; (2) FixSurface liga _EMISSION em material _ALPHATEST_ON (+_NORMALMAP), combinações que não estão na lista de BuildJogo.Shaders (só 6 combos de Lit); (3) a mesma família do terreno que saiu preto só no build (drawInstanced).
- **[critico] Edição não commitada quebra a compilação do SmokeCapture, a ferramenta de diagnóstico visual do build** — git diff Assets/_Project/Scripts/Runtime/Core/SmokeCapture.cs: em LogEnemy, as strings interpoladas comuns ($"...:" / $"... escala={...}" / "    material NULO") têm quebra de linha literal no lugar de \n (o diff mostra a string continuando na linha seguinte). Isso dá CS1010 (newline in constant) e bloqueia o pre-commit (CompileCheck) e o build.
- **[alto] Árvores 100% procedurais e low-poly; nenhuma árvore baixada existe** — Assets/Resources/TDFende/Cenario/Arvores não existe e nunca entrou no git (git log vazio para o caminho). WorldView.MaxModelTrees=60 nunca é usado. ModelLib.Pine são 4 cones Lathe de 8 lados; Oak são esferas 8x6 com Foliage procedural de 256 px (#3F5B2B). Em tdf_g2_grama.png aparecem cones e bolhas facetados. O filtro de árvores do BaixarArte ($exclude 'island|tropical|palm|...') provavelmente zerou a busca.
- **[alto] Chão amarelo-seco, com ladrilho visível e grade branca permanente: é o fator visual nº 1, porque ocupa ~80% da tela** — tdf_g2.png: o campo leafy_grass sai ocre e a lane aerial_grass_rock mostra o padrão repetido a cada 4 unidades (tileSize 4x4 / 3x3 num terreno de 220). Só há 2 TerrainLayers: sem terra batida, trilha, cascalho, flores ou variação de macro-cor. O tom é empurrado por ACES + postExposure 0,1 + contrast 6 (PostFx.cs) e pela mudança não commitada m_LightsUseLinearIntensity 0→1 (GraphicsSettings). O ajuste de cor no material da grama (1.15,1.35,0.95, 'a foto é oliva-escuro') ataca o sintoma localmente. A câmera fica a 36–58° (CameraRigDriver PitchNear/PitchFar), então o chão domina. As linhas da grade (Overlays) cobrem a lane inteira o tempo todo.
- **[alto] Grama 3D cara e ineficaz: ~4M triângulos para fiapos escuros e ralos** — Player.log: 'grama 3D: 7448 touceiras ... ~4,0 M triângulos', FPS médio 54. Player-prev.log, com 4699 touceiras, dava FPS médio 104. Isso numa RTX 4070 Ti (D3D12). Não há LOD nem corte por distância ou frustum: worldBounds é um cubo de 170 e RenderMeshInstanced roda todo frame. O SSAO usa DepthNormals (URP_Renderer Source:1), então a grama é desenhada duas vezes. grass_medium da Poly Haven é touceira de folhas finas feita para close-up. TargetHeight 0,42 ≈ 1,26 m na escala do jogo. De cima ela vira pontos escuros (tdf_g2_grama.png).
- **[alto] Escala incoerente entre bichos, torres e fortaleza** — 1 unidade ≈ 3 m (ModelLib). As torres têm 'altura' de 0,71 a 1,29 no torres.json (≈2–4 m). O elefante baixado é escalado para Def.Height = 1,96 (Player.log 'Inimigo_Elefante ... altura=1,96'), porque ModelLibAnimals.Elephant faz Height = seat.y + 0.9f, que inclui o howdah e o mastro do elefante de guerra procedural, que o modelo real não tem. Resultado: o elefante fica mais alto que qualquer torre. Fortaleza e acampamento aparecem miúdos (anel de ~60 px em tdf_g2.png). Os bichos pequenos viram pontos de ~10 px na câmera inicial (tdf_bichos.png).
- **[alto] Estilo incoerente: bicho fotorrealista, torre gerada por IA, enfeite procedural, tenda de brinquedo e caixa de plástico** — Bichos Sketchfab realistas. Torres Karrades geradas por IA, só albedo (18 de 21 sem _normal), com luz assada típica, smoothness fixo 0,15 (ArtFactory.SkinBody). Em cima delas vão torreta, cano de bronze, bandeira e enfeites Lv procedurais. ModelLib.ArmyCamp desenha cones vermelhos e azuis (tdf_g2.png, topo). Cenario/Caixas tem plastic_crate_01/02 e old_military_crate apesar do $exclude 'plastic|metal'. O Acampamento.fbx dentro da lane convive com as tendas procedurais do fundo. O README fixa 'fim da Idade Média', mas o Felipe agora diz que não precisa ser medieval: é hora de escolher um tema coerente.
- **[alto] Nenhum LOD em lugar nenhum e malhas pesadas projetando sombra em 4 cascatas** — grep LODGroup/decimate em Runtime e ConverterTorres não acha nada. Torres de 10,6k a 95k tris (comunidade.json) e Fortaleza de 92,6k, x2 lanes. Todas projetam sombra (SkinBody: ShadowCastingMode.On) num shadowmap de 4096 com 4 cascatas e distância 80 (URP_Asset). Também passam pelo prepass DepthNormals do SSAO.
- **[alto] Não há tiers de qualidade nem caminho mobile, apesar do alvo mobile-ready** — QualitySettings: só 'Ultra' tem customRenderPipeline; Very Low..High herdam o mesmo URP_Asset (GraphicsSettings m_CustomRenderPipeline b271b8f6). Android usa o nível 2 por padrão e recebe a mesma sombra 4096 com 4 cascatas, SSAO, SMAA, Terrain e 4M tris de grama. O CPU skinning (ProjectSettings gpuSkinning: 0) pesa com dezenas de bichos.
- **[medio] Efeitos sem textura: fogo, fumaça e explosão viram bolinhas** — SoftParticle.shader: o alfa sai só de 1-dot(uv) elevado a _Softness. Não há textura, flipbook, soft-particle por profundidade (apesar do nome) nem luz. Vfx.Create usa Billboard puro. Não há decal (queimado, cratera, gelo no chão), rastro de projétil nem luz pontual nas explosões ou no braseiro.
- **[medio] Texturas carregadas em runtime sem compressão e com engasgo na primeira aparição** — ArtFactory.TryLoadExternal/LoadBodyTex fazem LoadImage de JPG .bytes para RGBA32, sem compressão de GPU: ≈5,6 MB de VRAM por 1024² com mips, para 24 fotos e 24 texturas de torre. O boot gasta 1051 ms em texturas (log). SkinBody é preguiçoso: o primeiro surgimento de cada estágio decodifica o JPG na thread principal, durante a partida. GradeToPalette faz GetPixels32 em CPU.
- **[medio] O rio entre as lanes some quando o terreno existe; o vão vira campo seco** — WorldView.Build, com terrain != null, faz layout.River = false. TowerWarsController.BuildWorld pede River=true, RiverAlongZ=true, e o README promete 'rio entre as lanes'. O vão de LaneGap=6 aparece vazio em tdf_g2.png.
- **[medio] Névoa e céu não contribuem na câmera de jogo** — Névoa linear de 55 a 170 com a câmera a 8–~60 unidades (MinDist 8): quase nenhuma perspectiva atmosférica. O céu nunca aparece com pitch de 36–58°; o HDRI só alimenta o ambiente (0,5). Não há nuvens projetando sombra, hora do dia nem color grading por LUT.
- **[medio] Build depende de uma lista manual de variantes de shader (Shader.Find)** — Há 5 comentários 'TODO(build): Shader.Find sofre stripping' (GroundBuilder, GrassField, SceneAmbience, TerritoryRenderer). BuildJogo.Shaders lista 6 combos de Lit e 4 de Terrain. O código liga _EMISSION em material _ALPHATEST_ON (AnimalLoader.FixSurface, ArtFactory.Mat) sem combo correspondente. As mudanças de prefiltering no URP_Asset também estão pendentes de commit.
- **[medio] Ferramenta de preview não mostra o jogo real** — Tools/ArtPreview/ArtPreview.csproj compila só MeshBuilder, ProcTex, ModelLib*, WorldLayout. Não aparecem terreno, grama, torres FBX, bichos, luz nem pós. O único retrato fiel é build + SmokeCapture (~2 min, com o Unity fechado).
- **[medio] Bichos com animação pobre para os planos novos (inimigo que ataca torre)** — bichos.json: javali e tigre só têm Walk ('baselayer'); rato, lobo e rinoceronte não têm Death; nenhum tem Attack. ModelRig só conhece Walk/Run/Idle/Death. O pipeline Legacy (AnimalImportRules) não tem blend ou layers.
- **[baixo] Lixo e código morto na área de arte** — Bichos/Gerados/ tem 2 GLBs do Meshy de 71 MB e 198 MB dentro de Resources (DefaultImporter, ignorados pelo git). Torre_Canhao.fbx com texturas antigas fica sem uso quando existem os _1/_2/_3. CharacterLoader/CharacterImportRules e Resources/TDFende/Personagens servem a soldados que não existem mais. O LEIA-ME de Bichos ainda diz que javali e tigre usam o modelo em código. As roughness EXR da Poly Haven não chegam ao smoothness do URP Lit.

### Pontos de extensão

- Modelo novo de estágio de torre: entrada em Tools/ConverterTorres/torres.json (raio, altura, base, piso_turret, piso, giro, tom) e em comunidade.json (autor, licença, tris); rodar converte.py e verifica.py; copiar para Assets/Resources/TDFende/Torres/Torre_<Tipo>_<n>.fbx + Textures/<nome>_cor/_normal.bytes. Para mais evoluções: TowerStages.Count/LevelsPerStage.
- Tipo novo de torre (arte): case em ModelLib.Tower(typeId) + construtor procedural (Shaft/Top/Turret/Barrel/Flag) + enfeites em ModelLibTiers + ModelLib.Projectile + receitas em Vfx.Muzzle/Impact(towerType) + termos em ComunidadeMeshy/buscas.json.
- Bicho novo: bichos.json + baixa.py/converte.py (ou estatico.py se não tiver rig) → Assets/Resources/TDFende/Bichos/<nome>.fbx + Textures; linha de exceção no .gitignore; palavra-chave em AnimalLoader.Keywords; case em ModelLib.Enemy + fallback em ModelLibAnimals (o Def.Height manda na escala do modelo real); crédito no THIRD_PARTY.md.
- Prefab por nome: qualquer Resources/TDFende/<def.Name> substitui o modelo procedural. Sem peças conhecidas troca o modelo inteiro; com peças nomeadas (Turret, Barrel, Shaft, Top, Flag, LegL, LegR, Rotor, Wing) vira híbrido e a animação continua.
- Cenário: BaixarArte.ps1 ($plan/$exclude por categoria) → Resources/TDFende/Cenario/<Categoria>/; SceneryModels.Place(categoria) agrupa variantes; o mapeamento PropKind→categoria fica em WorldView.Build; tipos novos entram no enum WorldLayout.PropKind e em ScatterScenery.
- Chão: GroundBuilder (constantes LaneTex/FieldTex, array terrainLayers e cálculo do alphamap) aceita mais camadas (terra, trilha, cascalho, flores) e regras por distância/ruído; tileSize por camada.
- Grama/folhagem: GrassField.Kinds, TriangleBudget, Falloff, TargetHeight, MinVisibleHeight. O ponto natural para LOD/corte por distância é o laço de Update/_draws.
- Luz e atmosfera: SceneAmbience (SkyPath, SunColor, rotação e intensidade do sol, Haze, névoa) e PostFx (perfil de Volume criado em código; dá para trocar tonemap, exposição e LUT).
- Material por foto: MatSpec.External(ArtMat) + Resources/TDFende/Textures/<Nome>_albedo/_normal.bytes; GradeToPalette ajusta o tom.
- Variantes de shader no build: lista BuildJogo.Shaders (toda keyword nova ligada em runtime precisa entrar ali).
- Diagnóstico visual do executável: SmokeCapture (LogScene, LogMovement e LogEnemy quando consertado) e prints em -captura; ArtPreview para o procedural.
- Efeitos: Vfx.Create(Recipe) e o shader SoftParticle são o ponto para flipbooks/texturas; CampFires e Vfx.Brazier para fogo ambiente.

### Restrições

- Direção pedida pelo Felipe (relayada): realista, mas NÃO precisa ser medieval. Isso derruba a amarra 'fim da Idade Média' do README/ModelLib, o $exclude 'plastic|metal' do BaixarArte.ps1 e os termos medievais do buscas.json. O tema tem de ser decidido e mantido coerente.
- A Sim é pura e determinística; a arte é só vista. O tabuleiro tem de ficar plano em y=0 (GroundBuilder FlatMargin=10, giz e overlays a y=0,012; o clique é raio contra plano matemático, sem collider).
- Tudo é montado em código, sem cena e sem prefab; os assets entram por nome via Resources. Todo shader ou combinação de keyword ligada em runtime por Shader.Find precisa estar em BuildJogo.Shaders/ShaderKeep, senão some ou sai preto no executável.
- Licenças: CC0 ou CC BY com crédito (THIRD_PARTY.md). Mixamo e pacotes de Asset Store/Fab ficam só no PC: o repositório é público e o .gitignore barra Bichos/* e Personagens/*.fbx. Meshy: procurar pronto primeiro (comunidade CC0, Sketchfab) e perguntar antes de gastar créditos (~30 por modelo com textura; ~842 disponíveis; o rig pareceu gratuito). O token do Sketchfab e a MESHY_API_KEY são do Felipe.
- Alvo Steam e mobile-ready: hoje não existe orçamento por plataforma. Qualquer arte nova precisa nascer com LOD e orçamento de tris (os conversores já têm 'tris' para bichos, não para torres).
- Git sem LFS (não há .gitattributes; .git já tem 191 MB). As pastas versionadas Torres (50 MB), Cenario (86 MB) e Bichos (~23 MB de FBX) crescem com cada asset novo.
- Regras de trabalho: teste antes da correção (o FlowSim cobre TowerStages, mas não enxerga visual; a prova visual é build + -captura com o Unity fechado, ~2 min); nada entregue com teste vermelho; commit e push direto na main com git pull --rebase (mais de uma sessão no repositório); mensagens e comentários em português.
- Há mudanças não commitadas que mexem no visual: GraphicsSettings m_LightsUseLinearIntensity 0→1, URP_Renderer com SSAO, URP_Asset prefiltering, GroundBuilder drawInstanced=false, cor da grama, materiais do ShaderKeep e DefaultVolumeProfile. Entre elas, o SmokeCapture.cs quebrado precisa ser resolvido antes de qualquer outra mudança de arte.

## Interface, fluxo e sensação de jogo (menu, HUD, loja/seleção de torres e bichos, câmera, input, feedback)

Hoje toda a interface é IMGUI (OnGUI) em 4 lugares: ModeSelect, DebugHud, o OnGUI do TowerWarsController e FloatingText. Tudo tem medida fixa em pixels, usa a fonte padrão do Unity e a pele de UiSkin.cs, que gera texturas de 1 px e molduras de 6x6. O projeto não tem nenhum ícone, retrato, fonte, som, USS/UXML, PanelSettings, nem TMP Essentials.

**Menu.** É um painel de 520x330 px desenhado sobre o Default-Skybox de uma cena vazia (Assets/Scenes/Jogo.unity tem só uma câmera). Mostra o título "TDFende", um botão "TD clássico", três botões de dificuldade do Tower Wars e um botão de desenvolvedor, "Exportar balanceamento". Não tem Sair, Configurações nem Créditos.

**Como se joga o Tower Wars hoje.**
- Q/E ou uma barra de 6 botões (132x54 px, com nome e custo) escolhem a torre.
- Um clique na lane da esquerda constrói. Clicar numa torre sua sobe o nível na hora, sem painel de seleção. O botão direito, X ou Delete vende na hora.
- As teclas 1-9 ou uma barra de 9 botões (118x62 px, com nome, custo e renda) compram um bicho e o enviam na hora.
- R reinicia sem pedir confirmação, e F9 grava o replay.
- O HUD tem uma caixa de informação no topo (vidas, ouro e renda dos dois lados, tempo, escala, torres) e uma caixa de ajuda em texto corrido.
- O fim de partida mostra só "VITÓRIA"/"DERROTA" e "R para jogar de novo".

**O que não existe.** Pausa e Esc, voltar ao menu, configurações, tutorial, som (nem AudioListener no executável), tela de carregamento, estatísticas de fim de partida (os dados já existem no LaneSim), créditos (que são obrigatórios pela CC BY), escala de UI e safe area, retratos e ícones, ficha de atributos das torres e bichos, prévia da fronteira no fantasma de construção, e qualquer aviso de bichos chegando ou de compra feita, recusada, renda ou recompensa.

**Tecnologias disponíveis.**
- **UI Toolkit:** o módulo com.unity.modules.uielements está ligado e a UnityEngine.UIElementsModule.dll fica nas DLLs do editor, então basta uma referência no CompileCheck. As telas podem ser escritas em UXML/USS de texto. Falta um PanelSettings e um tema .tss, que um script de Editor pode gerar, no mesmo padrão do BuildJogo.
- **uGUI 2.0.0 + TextMeshPro:** estão no manifest, mas as DLLs só existem em Library/ScriptAssemblies, o asmdef do Runtime não tem nenhuma referência e o TMP Essentials não foi importado.
- O input é o Input Manager legado (activeInputHandler 0, sem o pacote Input System). As duas tecnologias funcionam com ele.

**Recomendação.** UI Toolkit para menu, HUD, loja, pausa e fim de partida. As barras de vida e o fantasma de construção continuam como malhas no mundo. Os retratos podem ser renderizados em código a partir de ArtFactory.Spawn(ModelLib.Tower/Enemy).

**Bloqueio antes de qualquer commit.** O SmokeCapture.cs não commitado tem strings quebradas por quebra de linha literal em LogEnemy. Isso quebra o CompileCheck do pre-commit.

### O que existe

**Tela inicial (ModeSelect)** — `Assets/_Project/Scripts/Runtime/Core/ModeSelect.cs, Assets/_Project/Scripts/Runtime/Core/GameBootstrap.cs, Assets/Scenes/Jogo.unity`

OnGUI: GUI.Box de (w+60)x330 px, com w=460, a y=22% da altura. Título 'TDFende' em 44 px e subtítulo. Botão 'TD clássico — uma lane, ondas infinitas', três botões Fácil/Normal/Difícil para o Tower Wars e o botão de desenvolvedor 'Exportar balanceamento' (CatalogLoader.ExportDefaults). Nada é desenhado atrás: a cena só tem câmera, com Default-Skybox fileID 10304 e m_ClearFlags 1. Ao escolher um modo, o componente é destruído com Destroy(gameObject), então não há como voltar. Start() (ainda não commitado) desvia para SmokeCapture quando o jogo abre com '-captura'.

**Pele de UI (UiSkin + Palette)** — `Assets/_Project/Scripts/Runtime/UI/UiSkin.cs, Assets/_Project/Scripts/Runtime/Core/Palette.cs`

GUIStyles gerados em código: Label 15 px negrito, LabelSmall 12, Big 34, Title 44, Subtitle 14, Floating 17. Painel com textura de 1 px na cor UiPanel #17130F a 78%. Botão de 6x6 px com moldura de 1 px: UiButton #3A2C20, hover #54402C, selecionado #7A5A2E, acento #C9A24A. Tinta UiInk #EDE3CF. Shadowed() desenha o texto duas vezes para fazer a sombra. Identidade 'madeira escura + dourado + pergaminho'. Usa a fonte padrão do Unity; não há nenhum .ttf/.otf no projeto.

**HUD do Tower Wars (OnGUI do TowerWarsController)** — `Assets/_Project/Scripts/Runtime/TowerWars/TowerWarsController.cs (linhas 58-96, 372-458)`

InfoRect (12,10,470x82) com 3 linhas: 'VOCÊ vidas/ouro/renda', 'IA (dificuldade) vidas/renda' e 'tempo, escala dos envios xN, torres N (nv M)'.

TowerPanelRect: 6 botões de 132x54 px com nome e custo. A torre selecionada usa ButtonSelected; as outras ficam com alpha 0.65, ou 0.35 sem ouro.

SendPanelRect: 9 botões de 118x62 px com '[i] Nome', custo e '+N/renda', alpha 0.45 sem ouro. A largura é SendCatalog.Count*124 = 1116 px.

HelpBoxRect: 640x50 px de texto corrido com controles e a dica de upgrade/venda sob o cursor.

Na vitória ou derrota, um véu de tela cheia com 'VITÓRIA'/'DERROTA' e 'R para jogar de novo'. PointerOverHud() testa os 4 retângulos para o clique não construir por baixo da UI, porque o Input legado não é consumido pela IMGUI.

**Construção, upgrade e venda no Tower Wars** — `Assets/_Project/Scripts/Runtime/TowerWars/TowerWarsController.cs (252-344), Assets/_Project/Scripts/Runtime/Sim/MatchRunner.cs, Assets/_Project/Scripts/Runtime/Sim/MatchCommand.cs`

HandleBuildInput faz um raycast num plano Y=0 e chama LaneView.WorldToCell. Sobre uma torre própria, o clique esquerdo ou U enfileira MatchCommand.Upgrade na hora; o botão direito, X ou Delete enfileira Sell na hora. Numa célula livre, o clique enfileira Build com _selectedTower. Tudo passa por MatchRunner.Enqueue e vale no próximo tique (determinismo e replay). Não existe estado de 'torre selecionada'.

**Escolha de torre e envio de bichos** — `Assets/_Project/Scripts/Runtime/TowerWars/TowerWarsController.cs (313-344, 413-427)`

Q/E ciclam _selectedTower com Input.GetKeyDown direto. As teclas 1-9 (laço 'i < 9') e os botões chamam TrySelectedSend, que enfileira MatchCommand.Send; compra e envio são uma ação só, e _selectedSend não tem papel visível. Quando falta ouro, o botão não faz nada; pelo teclado, o comando é recusado em silêncio em MatchRunner.Apply.

**Catálogos que alimentam a loja** — `Assets/_Project/Scripts/Runtime/Sim/TowerCatalog.cs, Assets/_Project/Scripts/Runtime/Sim/SendCatalog.cs, Assets/_Project/Scripts/Runtime/Art/ModelLib.cs (93-125)`

TowerCatalog: 6 torres (Canhão 25, Morteiro 45, Gelo 40, Sentinela 50, Fogo 45, Ar 40) com Range, Cooldown, Damage, SplashRadius, SlowFactor, VsFlyingMultiplier, BorderRadius, Burn e Knockback; UpgradeCost = Cost*0.8*nível; MaxTowerLevel 6.

SendCatalog: 9 bichos (Rato 22 x4, Cachorro 10, Lobo 35, Javali 30, Águia 55 voa, Urso 40, Tigre 60, Rinoceronte 75, Elefante 90) com Hp, Speed, IncomeBonus, Bounty, Count e AttritionScale.

Nenhum campo de descrição, ícone ou contra-quê. A tabela 'responde a' só existe no README.

ModelLib.Tower/Enemy mapeiam modelo por ÍNDICE (switch), e um arquivo de balanceamento pode reordenar os envios.

**TD clássico (GameController + DebugHud + TowerPlacer)** — `Assets/_Project/Scripts/Runtime/Core/GameController.cs, Assets/_Project/Scripts/Runtime/UI/DebugHud.cs, Assets/_Project/Scripts/Runtime/Towers/TowerPlacer.cs, Assets/_Project/Scripts/Runtime/Core/GameConfig.cs`

Código separado e mais antigo, com MonoBehaviours Tower/Enemy/Projectile e SimplePool; não usa o LaneSim. Tem um tipo de torre só (25 de ouro, GameConfig.TowerCost) e um inimigo procedural (ModelLib.Enemy(0)), sem upgrade. O DebugHud mostra vidas, ouro, onda, inimigos e FPS em InfoRect 380x84, mais uma caixa de ajuda de 500x116. Espaço chama a onda e R reinicia. No game over, Time.timeScale=0 e o véu mostra 'A fortaleza caiu na onda N'.

**Câmera (CameraRigDriver)** — `Assets/_Project/Scripts/Runtime/Camera/CameraRigDriver.cs, TowerWarsController.cs (124-133)`

Pivô no chão com pan por WASD/setas (12 u/s escalado pelo zoom) ou arrasto do botão do meio, e zoom no scroll (passo 2.5, mínimo 8, máximo max(30, startDist*1.25)). A inclinação acompanha o zoom: 36° perto, 58° longe. Limites de worldSize*0.6. Focus(point, dist) é instantâneo. No Tower Wars a câmera começa sobre a SUA lane, a da esquerda, na distância depth*0.62 (~18.6). Não tem rotação, edge-pan, atalho de trocar de lane nem minimapa. O shake vem de Juice.ShakeOffset.

**Input (IGameInput / DesktopInput)** — `Assets/_Project/Scripts/Runtime/Input/IGameInput.cs, Assets/_Project/Scripts/Runtime/Input/DesktopInput.cs`

A interface expõe as intenções PanAxis, DragPanDelta, ZoomDelta, PointerPos, PlacePressed, UpgradePressed (U), SellPressed (RMB/X/Delete), CallWavePressed (Espaço) e RestartPressed (R). Ela existe para o porte mobile (TouchInput), mas o TowerWarsController a contorna: Q/E, 1-9 e F9 são lidos direto de Input.GetKeyDown. Não há Esc/pausa. ProjectSettings activeInputHandler: 0 (Input Manager legado).

**Feedback: FloatingText, Juice, Vfx, barras de vida** — `Assets/_Project/Scripts/Runtime/Visuals/FloatingText.cs, Assets/_Project/Scripts/Runtime/Visuals/Juice.cs, Assets/_Project/Scripts/Runtime/Visuals/Vfx.cs, Assets/_Project/Scripts/Runtime/Art/ModelRig.cs (341-442), Assets/_Project/Scripts/Runtime/TowerWars/LaneView.cs (114-209)`

FloatingText: OnGUI, até 32 entradas, vida de 1.1 s, sobe a 1.4 u/s, 300 px de largura. No Tower Wars aparece só '-1' no vazamento, '+N' na venda e 'nível N' no upgrade; não há '+recompensa' no abate, nem renda, nem compra.

Juice: shake de 0.55 no vazamento (só na sua lane), 0.10 no upgrade e 0.08 na venda.

Vfx: KillBurst (atrito com cor de geada), Leak, Build, Muzzle e Impact por tipo, Burn, Freeze/Thaw, Brazier, rastros.

HealthBar: malhas no mundo viradas para a câmera, verde/amarelo/vermelho, escondida com vida cheia. Os bichos baixados têm um anel colorido de time no chão (AnimalLoader.AddTeamRing).

**Fantasma de construção e overlays** — `Assets/_Project/Scripts/Runtime/Visuals/Overlays.cs`

PlacementGhost = célula de 0.96 u pulsando em verde #6FCF7A, vermelho #E0523C ou dourado #E8C15A (upgrade possível), com alpha 0.45, mais um anel de ALCANCE (Range*CellSize, cor RangeRing). Não mostra a fronteira (BorderRadius) que a torre vai projetar nem o modelo da torre. Overlays.Grid/Ring/Tile usam o material Overlay sem luz, tingido por MaterialPropertyBlock.

**Teste de fumaça do executável (única verificação visual automatizada)** — `Assets/_Project/Scripts/Runtime/Core/SmokeCapture.cs, Assets/_Project/Scripts/Editor/BuildJogo.cs, Tools/GerarExecutavel.ps1`

'TDFende.exe -captura print.png' entra no Tower Wars Normal, manda um de cada bicho, mede o movimento, tira um print e loga shaders e terreno. A edição em andamento acrescenta um segundo print em close-up e LogEnemy, mas está quebrada. Não há testes de UI nem dos controladores; o FlowSim só compila Sim/Grid.

**Tecnologia de UI disponível** — `Packages/manifest.json, Assets/_Project/Scripts/Runtime/TDFende.Runtime.asmdef, Tools/CompileCheck/CompileCheck.csproj, ProjectSettings/ProjectSettings.asset`

IMGUI: em uso.

UI Toolkit: com.unity.modules.uielements no manifest; UnityEngine.UIElementsModule.dll, TextCoreTextEngineModule e InputForUIModule em C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Data/Managed/UnityEngine/. Ainda não existem PanelSettings, tema .tss, UXML ou USS.

uGUI: com.unity.ugui 2.0.0 (inclui TMP); UnityEngine.UI.dll e Unity.TextMeshPro.dll só em Library/ScriptAssemblies. TDFende.Runtime.asmdef tem 'references': [] e não existe a pasta 'Assets/TextMesh Pro'.

Áudio: com.unity.modules.audio no manifest e AudioModule já referenciado no CompileCheck, mas nenhum AudioSource ou AudioClip no código ou nos assets.

### Lacunas

- **[critico] Edição não commitada e quebrada no SmokeCapture.cs: as strings de LogEnemy têm quebra de linha literal (CS1010). O CompileCheck do pre-commit compila todo o Runtime, então nenhum commit de UI passa até isso ser corrigido ou revertido. Além disso, o '-captura' é a única forma de conferir UI num build.** — git diff de SmokeCapture.cs: `$"[TDFende] captura, bicho {v.Rig.Def.Name} ... lossyScale}:` seguido de uma quebra de linha real dentro do literal (o mesmo em 'material NULO' e 'cor=...'). .githooks/pre-commit roda `dotnet build Tools/CompileCheck`, que inclui Runtime/**/*.cs.
- **[critico] A loja e a seleção de torres e bichos (pedido explícito do Felipe) são botões retangulares de texto: sem ícone ou retrato 3D, sem ficha (dano, DPS, alcance, fronteira, área, lentidão, queima, empurrão; vida, velocidade, quantidade, voa ou é imune ao atrito, recompensa), sem 'responde a / bom contra', sem tooltip, sem estado 'sem ouro' explicado e sem prévia do nível seguinte. Compra e envio são um clique só, sem quantidade (x5) e sem fila visível.** — TowerWarsController.cs:402: `$"{tt.Name}\n<color=#E8C15A>{tt.Cost} ouro</color>"`; linha 420: `$"[{i + 1}] {u.Name}\n...{u.Cost} ouro</color>  +{u.IncomeBonus}/renda"`. A tabela 'Torres e o que cada uma responde' só existe no README.md. Não há PNG de UI, sprite nem RenderTexture no código (grep RenderTexture/targetTexture vazio).
- **[critico] A tela inicial é um painel OnGUI sobre o Default-Skybox de uma cena vazia: sem logo nem arte, sem mundo 3D ao fundo, sem animação, com fonte padrão. O botão de desenvolvedor 'Exportar balanceamento' fica exposto ao jogador. Não há Sair, Configurações, Créditos, Como jogar nem versão.** — ModeSelect.cs:33-69. Jogo.unity:29 tem `m_SkyboxMaterial: {fileID: 10304...}` (Default-Skybox) e a câmera é o único objeto. SceneAmbience e WorldView só rodam no Start dos controladores.
- **[alto] Não há nenhum som: nem efeitos, nem música, nem som de interface. O executável também não tem AudioListener: a câmera de Jogo.unity só tem Camera e Transform, e os controladores só criam o AudioListener quando Camera.main é nulo, o que não acontece no build.** — grep 'AudioSource|AudioClip|PlayOneShot' em Assets/_Project sem resultado. Jogo.unity só tem os blocos !u!20 (Camera) e !u!4 (Transform), sem !u!81. TowerWarsController.cs:100-106 e GameController.cs:374-383 só adicionam o AudioListener quando a câmera não existe. BuildJogo.EnsureScene só faz AddComponent<Camera>().
- **[alto] Faltam pausa, Esc, volta ao menu e sair. R reinicia a partida na hora e sem confirmação, e fica ao lado do E (trocar torre) e do W/D (pan): é fácil perder a partida por acidente. O ModeSelect se destrói ao começar, então não dá para voltar ao menu sem fechar o jogo.** — DesktopInput.cs:150 `RestartPressed = Input.GetKeyDown(KeyCode.R)`. TowerWarsController.cs:212-216 chama NewMatch() direto. grep 'Escape|Pause' sem resultado. ModeSelect.Launch/LaunchWars fazem Destroy(gameObject).
- **[alto] Não existe torre selecionada nem painel de inspeção: o clique esquerdo numa torre própria SOBE o nível na hora e gasta o ouro, e o botão direito VENDE na hora, sem confirmação. Não há como ver o nível, os atributos ou o que o próximo nível dá, nem escolher alvo. A única informação é uma frase acrescentada à caixa de ajuda.** — TowerWarsController.cs:292-307 enfileira Sell/Upgrade direto. Linhas 431-437 montam a dica de hover dentro do HelpBox.
- **[alto] Falta feedback nas ações econômicas. Mandar um bicho com a câmera na sua lane não mostra nada (ele nasce na lane da IA, fora da tela). Compra recusada por falta de ouro é silenciosa, e a construção recusada não diz por quê (ocupada, fecharia o caminho ou sem ouro). Abate não mostra '+recompensa'. O pingo de renda a cada 10 s não aparece, e o LaneSim.TimeToIncome já existe sem ser mostrado. Também não há aviso de 'bichos chegando'.** — LaneSim.TrySend (linha 515) não levanta evento. LaneView.OnEnemyDespawned (188-200) só chama Vfx no abate. MatchRunner.Apply devolve false em silêncio. LaneSim.CanBuild devolve só bool. LaneSim.cs:218 `TimeToIncome` não é usado no HUD.
- **[alto] O fantasma de construção não mostra a FRONTEIRA que a torre vai projetar, que é a mecânica-tese do jogo; só mostra o alcance. Também não mostra o modelo da torre nem como o caminho vai mudar.** — Overlays.cs PlacementGhost.Show(cellCenter, tile, range) só tem o anel de Range. TowerWarsController.cs:289 passa `TowerCatalog.Get(rangeType).Range`. O BorderRadius varia de 1.5 (Sentinela) a 3.25 (Gelo) e nunca aparece antes de construir.
- **[alto] O layout é em pixels fixos, sem escala nem safe area: fica minúsculo em 4K e transborda em telas com menos de ~1130 px. As barras crescem linearmente com o tamanho dos catálogos, e as teclas param no 9. 'Mais bichos' e 'mais torres' quebram o HUD atual. Também não está pronto para mobile, apesar de ser alvo.** — SendPanelRect = SendCatalog.Count*(118+6) = 1116 px. TowerPanelRect = TowerCatalog.Count*(132+6) = 828 px. HandleSendInput: `i < SendCatalog.Count && i < 9`. grep 'GUI.matrix|Screen.dpi|safeArea' sem resultado. A barra de envio, a ajuda e a barra de torre ocupam ~190 px no canto inferior esquerdo, o mesmo lado da sua lane.
- **[alto] É difícil acompanhar as duas lanes: não há atalho para focar a sua lane ou a da IA, nem minimapa ou picture-in-picture. Não há indicador do que está vindo para você, nem resumo do que a IA construiu ou enviou. A IA aparece só como 'vidas/renda'. É a pergunta 3 do TESTE.md.** — CameraRigDriver tem Focus(), mas nada no TowerWarsController o chama depois do Start. A linha da IA em TowerWarsController.cs:386 mostra só Lives e Income. TESTE.md seção 3, pergunta 3: 'Dá para acompanhar as duas lanes ao mesmo tempo'.
- **[alto] O fim de partida é pobre: só 'VITÓRIA'/'DERROTA' e 'R para jogar de novo', embora o LaneSim já guarde estatísticas ricas. Não há revanche com outra dificuldade, volta ao menu nem botão de replay (o F9 só funciona durante a partida e não é anunciado no fim).** — TowerWarsController.cs:449-457. LaneSim expõe KilledByTower, KilledByAttrition, TotalLeaked, TotalSent, GoldSpentOnTowers, GoldSpentOnSends, TotalUpgrades, TotalSold, SendsByType[] e MatchTime, e nada disso é mostrado.
- **[alto] Não há tela de créditos, que é obrigatória para os 7 bichos CC BY 4.0 do Sketchfab, possivelmente para o Meshy em plano grátis, e para o aviso MIT.** — THIRD_PARTY.md:21-22 diz que o crédito é obrigatório nos créditos do jogo. A linha 36 já tem o 'Texto pronto para a tela de créditos'. A linha 83 diz que o aviso MIT precisa ir junto. ModeSelect não tem opção de créditos.
- **[medio] A abstração de input é contornada no Tower Wars: Q/E, 1-9 e F9 são lidos direto de Input.GetKeyDown. O porte para toque (fase 5) exigiria mexer no controlador, contra o que o IGameInput promete.** — TowerWarsController.cs:217 (F9), 317-320 (Q/E), 329 (Alpha1+i). IGameInput.cs:4-6: 'nenhum outro arquivo do jogo muda'.
- **[medio] Não há tutorial nem onboarding. A ajuda é uma parede de texto, e os documentos estão desatualizados sobre a interface: o README diz 'lane de baixo' e 'teclas 1-6', e o TESTE.md fala em 'quatro tipos de torre' e 'seis tipos de inimigo'.** — README.md, seção Controles: 'a sua lane é a de baixo... As teclas 1-6'. TESTE.md seção 3: 'Sua lane é a de baixo'. O código diz 'Você à esquerda' (TowerWarsController.cs:162) e tem 9 envios e 6 torres.
- **[medio] A caixa de ajuda (640x50 px, padding 8/8, fonte de 13 px) provavelmente corta a dica de upgrade/venda sob o cursor: a primeira linha com a dica passa de ~200 caracteres e quebra em 3 ou 4 linhas, mas só cabem ~2. Isso foi inferido pelas medidas, sem rodar.** — TowerWarsController.cs:72-73 `HelpBoxRect ... 640f, 50f`; linhas 439-443 montam a string longa. UiSkin.Panel tem padding RectOffset(10,10,8,8).
- **[medio] Não há tela de carregamento: o clique no menu dispara, no mesmo quadro, ArtFactory.Preload, a construção do terreno, a grama (~4M triângulos) e o cenário. O jogo congela sem aviso.** — TowerWarsController.Start (98-134) faz ArtFactory.Preload(), SceneAmbience.Apply, BuildWorld() e NewMatch() tudo de forma síncrona. O TESTE.md pede para medir 'N ms' de textura no boot.
- **[medio] Não há configurações: volume (não existe som), resolução e janela (fullscreenMode 1, resizableWindow 0, fixos no ProjectSettings), qualidade gráfica (relevante com o FPS caindo para 54 por causa da grama), escala de UI, teclas e idioma.** — ProjectSettings.asset: defaultScreenWidth 1920, fullscreenMode 1, resizableWindow 0. Nenhuma chamada a Screen.SetResolution ou QualitySettings em Runtime/.
- **[medio] Acessibilidade e legibilidade: o fantasma usa verde contra vermelho (#6FCF7A / #E0523C) e as barras de vida verde, amarelo e vermelho, sem forma ou ícone redundante, o que é ruim para daltônicos (deuteranopia). As fontes são fixas de 12 a 15 px, sem modo daltônico e sem escala de texto. O número de atrito (azul-gelo #8FD3F0) contra o de tiro (dourado) é distinguido só pela cor.** — Palette.cs: GhostValid/GhostInvalid e TextFrost/TextGold. ModelRig.cs HealthBar.EnsureMeshes usa as cores verde/amarelo/vermelho. UiSkin.Ensure tem fontSize 12/14/15.
- **[medio] Os retratos e modelos da loja ficariam frágeis: o ModelLib escolhe o modelo pelo índice do catálogo, mas SendCatalog.LoadFrom aceita a ordem que vier do arquivo de balanceamento. Reordenar o envios.txt troca o modelo e o retrato de bicho.** — ModelLib.cs:104-114 `Enemy(int typeId) => typeId switch { 0 => Rato, 2 => Lobo ... }`. SendCatalog.cs:242 faz `All = parsed` sem reordenar pelo nome.
- **[medio] O TD clássico é um segundo caminho de código, com a própria interface: DebugHud mostrando FPS ao jogador, uma torre só, sem upgrade e sem LaneSim. Qualquer UI nova precisa ser feita duas vezes ou o modo precisa ser aposentado ou migrado.** — GameController.cs usa Tower/Enemy/Projectile MonoBehaviours e GameConfig.TowerCost=25. DebugHud.cs:41 mostra 'FPS: {_fps:0}'. TowerPlacer.cs depende de DebugHud.PointerOverHud.
- **[medio] Não há nenhum teste de interface ou dos controladores; a UI só é verificável por build + print. O FlowSim compila só Sim/Grid/Config.** — Tools/FlowSim/FlowSim.csproj inclui só GridMap, FlowField, TerritoryField, GameConfig, Sim/* e TowerStages. Não há pasta de testes do Unity em Assets.
- **[medio] Pouco 'juice' e pouca animação de interface: botões sem animação de press/hover, contador de ouro sem tick-up e sem efeito ao ganhar ou gastar, nenhum flash de tela ao perder vida (só shake), nenhum hit-stop. A partida também não diz quando entra em morte súbita (InSuddenDeath existe).** — Juice.cs só tem shake. LaneSim.cs:119 `InSuddenDeath` não é lido em nenhum ponto da UI.
- **[baixo] OnGUI aloca strings interpoladas em todo passe de GUI, várias vezes por quadro, gerando GC. É pequeno, mas soma no mobile.** — TowerWarsController.cs:384-443 e DebugHud.cs:42-60 interpolam strings em cada OnGUI.
- **[baixo] Faltam conveniências de câmera: rotação, edge-pan, duplo clique para focar e 'centralizar na fortaleza' (Home).** — CameraRigDriver.Tick só tem pan, arrasto e zoom; a inclinação é derivada do zoom.

### Pontos de extensão

- MatchRunner.Enqueue(MatchCommand) é a ÚNICA porta de ação da loja e da seleção (Build/Upgrade/Sell/Send). MatchRunner.CommandApplied(tick, cmd) já serve como confirmação de compra para a interface.
- Leitura de estado para HUD e loja em LaneSim: Gold, Income, Lives, MatchTime, SendScale, InSuddenDeath, TimeToIncome, CanAfford(id), CanBuild(cell,type), UpgradeCostAt, SellValueAt, TowerIndexAt, TowerLevel(i), TowerTypeAt, TowerCount, TotalTowerLevels e TowerDps.
- Estatísticas prontas para a tela de fim de partida em LaneSim: KilledByTower, KilledByAttrition, TotalLeaked, TotalSent, GoldSpentOnTowers, GoldSpentOnSends, TotalUpgrades, TotalSold e SendsByType[].
- Eventos de vista do LaneSim (EnemyDespawned, TowerChanged, TowerFired, TowerSold): dá para acrescentar SendBought, IncomeTick, EnemySpawned e BountyPaid sem mudar o estado da simulação, o que mantém o determinismo e o FlowSim.
- TowerType e SendUnit (structs de dados) podem ganhar Description, Counters e IconKey. O CatalogJson e o exportador de balanceamento já percorrem esses campos.
- Retratos e ícones gerados em código: ArtFactory.Spawn(ModelLib.Tower(type), team, parent, name, ArtFactory.StageFor(def, level)) e ModelLib.Enemy(type), renderizados por uma câmera fora da tela para RenderTexture/Texture2D no boot, ou gravados em PNG por um script de Editor como o BuildJogo.
- UiSkin e as cores Palette.Ui* (#17130F, #3A2C20, #54402C, #7A5A2E, #C9A24A, #EDE3CF) são os tokens de design para virar variáveis USS.
- PlacementGhost.Show(...) pode ganhar um segundo anel de fronteira (TowerCatalog.Get(t).BorderRadius * CellSize, na cor Palette.TerritoryEdge) e um fantasma do modelo via ArtFactory.Spawn semitransparente.
- CameraRigDriver.Focus(point, dist) para os atalhos de lane (Tab/F1/F2), o foco em eventos ('bichos chegando') e um sobrevoo de câmera no menu, com o mundo montado por WorldView.Build e SceneAmbience.Apply atrás do menu.
- IGameInput: acrescentar as intenções PausePressed, CycleTower, SendSlot(i), FocusLane e ConfirmCancel e tirar os Input.GetKeyDown do controlador. Uma TouchInput futura entra pela mesma interface.
- FloatingText.Instance.Show, Juice.Shake e Vfx.Instance para o feedback de compra, renda, recompensa e alerta.
- SmokeCapture e '-captura': podem ser estendidos para fotografar menu, loja aberta, pausa e fim de partida no build, que é a única verificação visual automatizada que existe.
- TowerWarsAi.Personality (Name, DecisionInterval etc.) alimenta os cartões de dificuldade do menu.
- Replay (F9 / SaveReplay) pode virar botão no menu de pausa e no fim de partida.
- Padrão de script de Editor que gera assets (BuildJogo.EnsureScene, ShaderKeep, UrpAutoSetup): mesmo caminho para gerar PanelSettings, tema .tss e FontAsset do UI Toolkit sem montagem manual.

### Restrições

- Regra de ouro da arquitetura: a interface nunca mexe no LaneSim direto. Toda ação passa por MatchRunner.Enqueue e vale no próximo tique (determinismo, replay e futuro multiplayer). Eventos novos no LaneSim só podem ser de vista e não podem mudar o estado.
- Catálogos trancados depois da primeira lane (SendCatalog.Lock/TowerCatalog.Lock), com tamanho e ordem vindos de arquivo de balanceamento. A loja precisa ser montada a partir de Count, nunca com 6 ou 9 fixos, e precisa aguentar os 'mais bichos/torres' pedidos (rolagem, abas ou grade, não barra linear com teclas 1-9).
- O Input é o Input Manager legado (activeInputHandler 0, sem o pacote Input System). A UI nova precisa de bloqueio de clique próprio: hoje é PointerOverHud por retângulos, e com UI Toolkit passaria a ser panel.Pick ou o equivalente.
- O CompileCheck só referencia DLLs de módulo do editor. UI Toolkit (UnityEngine.UIElementsModule e TextCoreTextEngineModule) entra com uma <Reference> simples. uGUI e TMP dependem de Library/ScriptAssemblies e de referências no TDFende.Runtime.asmdef (hoje vazias), no padrão do CompileCheckUrp, e o TMP Essentials não está importado.
- O projeto monta tudo em código e o Claude não abre o editor: assets de UI (PanelSettings, tema, fontes) precisam ser gerados por script de Editor em batchmode (GerarExecutavel.ps1) ou ser arquivos-texto (UXML/USS). A verificação visual só existe via 'TDFende.exe -captura'.
- O caminho '-captura' (ModeSelect.Start → SmokeCapture.Begin → LaunchWars Normal) precisa sobreviver a qualquer reescrita do menu.
- O SmokeCapture.cs não commitado está quebrado e trava o pre-commit (CompileCheck + ~3 min de testes do FlowSim): corrigir antes de qualquer trabalho de UI. Há várias sessões do Claude na main: git pull antes e git pull --rebase antes do push.
- Regras do Felipe: teste visto falhando antes da correção, nada entregue com teste vermelho, commit e push direto na main, mensagens e comentários em português.
- Licenças: fontes, ícones e sons precisam ser CC0 ou OFL (ou CC BY com crédito) e registrados no THIRD_PARTY.md. Asset Store/Fab só no PC, fora do GitHub, então um kit de UI comprado não entra no repositório. Os 7 bichos CC BY já exigem tela de créditos.
- Os modelos 3D seguem a regra de procurar pronto primeiro e perguntar antes de gastar créditos do Meshy (~30 por modelo com textura). Ícones e retratos podem sair dos modelos existentes, sem crédito novo.
- Performance: o FPS já caiu para 54 com a grama (~4M triângulos). Retratos ao vivo (RenderTexture por cartão, todo quadro) seriam caros; melhor renderizar uma vez no boot e guardar em cache.
- Mobile-ready é alvo declarado: escala de UI por resolução, safe area, alvos de toque grandes e nada que dependa de hover para informação essencial (tooltip só no hover não serve no toque).
- O TD clássico usa outro controlador e outro HUD (GameController/DebugHud/TowerPlacer). Antes de redesenhar, decidir se ele ganha a UI nova, é migrado para o LaneSim ou é aposentado.

## Tecnologia e produção: build, testes, ferramentas, desempenho, mobile, multiplayer e saúde da base de código

A base aguenta crescer. As regras do jogo vivem numa simulação pura e determinística (Runtime/Sim, passo fixo de 1/30 s, System.Random com semente, comandos aplicados no limite entre tiques e replay), coberta por 183 Check() no Tools/FlowSim. Há duas compilações contra as DLLs reais do Unity 6000.3.11f1 e um pre-commit de ~3 min. O executável sai sem abrir o Unity (GerarExecutavel.ps1 → BuildJogo.Windows, 280 MB, D3D12) e tem teste de fumaça automático ("-captura"). O que trava é a ponta visual e o processo de produção.

(1) Bichos invisíveis: a causa não foi confirmada. O Player.log das 13:20 mostra que os 9 modelos carregam e que 13 bichos andam "animado", então o problema é de render ou de onde/em que escala a malha fica, não de simulação. O log do editor (03/10) nunca rodou estes 9 bichos.
(2) A edição quebrada do SmokeCapture.cs já foi corrigida e enviada por outra sessão (commit 1800cbd, 13:25 de hoje). O executável atual ainda é de antes dessa correção: o build das 13:06 falhou com CS1039/CS1010.
(3) Grama: 4,0 M triângulos por quadro, sem culling, desenhada também na passada de profundidade do SSAO. A média de FPS da captura caiu de 104 para 54.
(4) "Pronto para mobile" é só intenção: HUD em OnGUI, Input legado lido direto no controlador, um único URP de PC (sombra 4096 com 4 cascatas e SSAO), texturas sem compressão de GPU.
(5) Multiplayer tem boa fundação (MatchCommand, Replay), mas o MatchRunner é fixo em jogador contra IA, a simulação usa float sem teste entre runtimes, e a assinatura de estado é grossa demais para pegar dessincronia.
(6) Não há nenhum som, e toda a UI é OnGUI.
(7) O processo é frágil: várias sessões no mesmo diretório (o HEAD mudou de 32b8690 para 1800cbd e o CLAUDE.md e o docs/MANUAL.md foram reescritos enquanto eu lia), e o editor em batch reescreve assets versionados a cada build.

### O que existe

**Simulação pura determinística** — `Assets/_Project/Scripts/Runtime/Sim/LaneSim.cs, MatchRunner.cs, MatchSim.cs, TowerWarsAi.cs, LeakRouter.cs, TowerWarsConfig.cs`

LaneSim (1013 linhas) guarda torres, inimigos (vetor de 256 que dobra quando enche), projéteis (512), fronteira, atrito e economia. MatchRunner aplica comandos na fronteira do tique, com FixedStep = 1/30 s. MatchSim alterna quem decide primeiro. TowerWarsAi é uma IA por utilidade com 3 personalidades. LeakRouter já repassa vazamentos entre N lanes. Os efeitos vêm de dados (SplashRadius, SlowFactor, BurnPctPerSecond, Knockback, AttritionScale, VsFlyingMultiplier), não de if por tipo.

**Comandos e replay** — `Runtime/Sim/MatchCommand.cs, Replay.cs; TowerWarsController.SaveReplay; Tools/FlowSim/Program.cs`

MatchCommand é um struct (Build/Upgrade/Send/Sell). Replay grava em texto: 'tdfende-replay 1', seed, difficulty, grid, ticks, assinatura do catálogo e um comando por tique. F9 salva em persistentDataPath/replays, e 'dotnet run -- replay <arq>' reproduz no FlowSim. Há teste de que reproduzir duas vezes dá o mesmo StateFingerprint.

**Tools/FlowSim (testes headless)** — `Tools/FlowSim/FlowSim.csproj, Program.cs, BalanceLab.cs, UnityStubs.cs`

Roda em .NET 10 e compila 17 .cs do Runtime (Grid, Sim, TowerStages) com stubs mínimos (UnityStubs.cs: Vector3, Vector2Int e Mathf.FloorToInt). São 183 chamadas Check() (o README ainda diz 66). Modos: match N (BalanceLab, 300 partidas IA×IA), sweep N fine, dump-catalogs, replay.

**CompileCheck e CompileCheckUrp** — `Tools/CompileCheck/CompileCheck.csproj, Tools/CompileCheckUrp/CompileCheckUrp.csproj`

CompileCheck (netstandard2.1) compila Runtime/** menos Urp contra as DLLs do editor achado pelo ProjectVersion.txt. Pega CS0104 (Random ambíguo), que os stubs não pegam. CompileCheckUrp compila Editor/** e Runtime/Urp contra Library/ScriptAssemblies; só roda se o Unity já abriu o projeto.

**Pre-commit** — `.githooks/pre-commit`

Três portões: CompileCheck, depois CompileCheckUrp (se existir Library/), depois FlowSim (~3 min). Depende de core.hooksPath=.githooks, configurado nesta máquina. Não existe CI no GitHub (sem .github/) e o repositório é público.

**Build sem abrir o Unity** — `Tools/GerarExecutavel.ps1, Assets/_Project/Scripts/Editor/BuildJogo.cs, Assets/Resources/TDFende/ShaderKeep/`

GerarExecutavel.ps1 chama Unity.exe -batchmode -nographics -executeMethod TDFende.EditorTools.BuildJogo.Windows e loga em Builds/build.log. BuildJogo cria Assets/Scenes/Jogo.unity (só a câmera) e 14 materiais ShaderKeep em Resources: URP Lit com 6 combos de keyword, Terrain/Lit com 4, Skybox Panoramic, Skybox Procedural, SoftParticle e TerritoryOverlay, todos com enableInstancing=true. Alvo StandaloneWindows64 com BuildOptions.None, ou seja, sem development build e sem profiler. Saída de 280 MB (resources.assets 44 MB + .resS 149 MB), rodando em Direct3D 12.

**Teste de fumaça '-captura'** — `Runtime/Core/SmokeCapture.cs, Runtime/Core/ModeSelect.cs, docs/MANUAL.md seção 4`

ModeSelect.Start entra direto no Tower Wars Normal. SmokeCapture manda um de cada bicho (LaneSim.GrantGoldForSmokeTest), mede o movimento de 8 a 20 s (saltos e o vai e volta do corpo), tira o print, loga a cena (shader, keywords, camadas, céu, sol) e depois tira um segundo print de perto com LogEnemy (renderers, materiais, keywords, limites). A versão corrigida está em 1800cbd, mas ainda não rodou em nenhum executável.

**Assemblies e pacotes** — `Packages/manifest.json, Scripts/Runtime/TDFende.Runtime.asmdef, Runtime/Urp/TDFende.Urp.asmdef, Editor/TDFende.Editor.asmdef`

Assemblies: TDFende.Runtime (sem referências), TDFende.Urp (defineConstraints TDFENDE_URP, só o PostFx.cs) e TDFende.Editor (referencia URP e Runtime). Pacotes: URP 17.3.0, ugui 2.0.0 (que já traz TextMeshPro), test-framework 1.4.5 sem nenhum teste, módulos de áudio e uielements. Faltam Input System, Addressables, glTFast, Cinemachine e Steamworks.

**Configuração de render** — `Assets/Settings/URP_Asset.asset, URP_Renderer.asset, ProjectSettings/GraphicsSettings.asset, QualitySettings.asset, Runtime/Urp/PostFx.cs, Editor/SsaoSetup.cs`

Um único URP_Asset serve aos 6 níveis de qualidade (customRenderPipeline 0 em todos). Sombra principal de 4096 com 4 cascatas e 80 de distância, sombra suave, HDR, MSAA desligado, SRP Batcher ligado, GPU Resident Drawer desligado, caminho Forward. SSAO por DepthNormals (intensidade 1.4, raio 0.3) é posto pelo SsaoSetup [InitializeOnLoad]. PostFx em código: ACES, bloom 0.25, vinheta 0.18, contraste 6, saturação 4. Instancing Stripping = Strip Unused, strictShaderVariantMatching 0, gpuSkinning 0, espaço de cor Linear.

**Chão e grama** — `Runtime/World/GroundBuilder.cs, Runtime/World/GrassField.cs, Assets/Resources/Art/`

GroundBuilder monta um Terrain de 220×220 (heightmap 129, alphamap 512) com 2 camadas 2k da Poly Haven: aerial_grass_rock (seca, amarelada) na lane e leafy_grass fora, com drawInstanced=false porque o instanciado saía preto no executável. GrassField desenha tufos com Graphics.RenderMeshInstanced: TriangleBudget 4.000.000, MaxInstances 20.000, um worldBounds único de ~170×170, sem sombra, sem LOD e sem culling. A matriz de cada tufo é calculada uma vez só.

**Abstração de input** — `Runtime/Input/IGameInput.cs, DesktopInput.cs`

IGameInput expõe intenções (pan, zoom, colocar, subir, vender) e DesktopInput lê o Input legado. Foi pensada para receber uma TouchInput.

**Pipelines de arte e importação** — `Tools/Converter*, Tools/ComunidadeMeshy, Tools/BaixarArte.ps1, Tools/ArtPreview, Editor/*ImportRules.cs, Runtime/Art/ArtFactory.cs, AnimalLoader.cs`

Ferramentas: ConverterBichos (Blender bpy + Sketchfab), ConverterTorres (Meshy), ComunidadeMeshy (busca CC0), BaixarArte.ps1 (Poly Haven) e ArtPreview (three.js com os mesmos Art/*.cs). Postprocessors: AnimalImportRules (Legacy, ImportViaMaterialDescription), ArtImportRules (mipMapsPreserveCoverage na grama), TowerImportRules e CharacterImportRules. ArtFactory troca o modelo procedural por um prefab de mesmo nome (Torres/<torre>_<estágio>, TowerStages.Count=3).

**Balanceamento sem recompilar** — `Runtime/Sim/CatalogLoader.cs, CatalogJson.cs, SendCatalog.cs, TowerCatalog.cs, Balanceamento/`

CatalogLoader lê envios.txt (9 bichos) e torres.txt (6 torres) no boot. Os catálogos são trancados quando nasce a primeira lane, e arquivo torto vira aviso no log, sem derrubar o jogo.

**Tamanho da base** — `Assets/_Project/Scripts/`

61 .cs no Runtime e ~13,2 mil linhas em _Project/Scripts. Os maiores arquivos são ModelLib.cs (1175), LaneSim.cs (1013) e ProcTex.cs (753). São 60 commits desde 25/09, ~200 MB de binários versionados e pack git de 51 MB.

**Documentação nova (outra sessão, hoje)** — `CLAUDE.md, docs/MANUAL.md`

O CLAUDE.md agora manda ler docs/MANUAL.md e pegar tarefa no ROADMAP.md, que ainda não existe. O MANUAL.md (223 linhas, staged, sem commit) traz as armadilhas do executável, as regras de git (proíbe stash entre um commit barrado e a nova tentativa) e a direção de arte: realista de última geração, não precisa ser medieval. Arquivos que o Unity regrava em batch só entram em commit com o Felipe confirmando.

### Lacunas

- **[critico] Bug (1): bichos invisíveis no executável, aparece só o anel colorido. Causa não confirmada.** — No Player.log (13:20) a carga está certa: '[TDFende] bichos de verdade: Inimigo_Aguia ... Inimigo_Urso' (9 modelos) e o bloco 'captura, movimento' lista 13 bichos 'animado' andando, sem nenhuma Exception. Logo a simulação e a animação funcionam; a falha é de render ou de posição/escala da malha. AnimalLoader.TrySpawn põe o anel em `root` (AddTeamRing) e o bicho num `holder` escalado e recentrado pelos Renderer.bounds (k = target/b.size.y; holder.localPosition += -local.x, -floor, -local.z). Bounds errados deixam exatamente o sintoma 'só o anel'. Segunda hipótese: PrepareRenderers liga _EMISSION em todo material, o que gera `_ALPHATEST_ON _EMISSION` e `_ALPHATEST_ON _NORMALMAP _EMISSION` nos pelos '_alfa' (rato, cachorro, lobo, águia); essas combinações não estão em BuildJogo.Shaders, e strictShaderVariantMatching=0 esconde a falta. Essa hipótese sozinha não explica elefante e urso, que são opacos. O 'no editor funciona' não está provado: o Editor.log mais recente (03/10 21:35) só carregou Inimigo_Javali/Lobo/Urso do pacote em quarentena, e os FBX atuais (c25a8a5 de 04/10; Meshy de 06/10) nunca rodaram no editor. O LogEnemy (1800cbd) que separaria as hipóteses ainda não rodou.
- **[medio] Bug (2): edição quebrada no SmokeCapture.cs. Já foi corrigida, mas o executável é anterior à correção.** — Builds/build.log (13:06): 'SmokeCapture.cs(141,154): error CS1039 / (142,1) CS1010 Newline in constant' e 'Scripts have compiler errors'. A correção (\n escapado) entrou em 1800cbd às 13:25:55, já com push. O TDFende.Runtime.dll do executável é das 13:04, então é preciso gerar um build novo antes de investigar o bug (1).
- **[alto] Grama 3D cara e feia: 4 M triângulos desenhados inteiros todo quadro, sem culling nem LOD, e repetidos na passada DepthNormals do SSAO.** — GrassField: TriangleBudget 4_000_000, worldBounds = Bounds(0, 170) para todos os lotes, Update chama RenderMeshInstanced em tudo. Player-prev.log (12:55): 4699 touceiras, ~4,0 M tri, 'fps médio 104'. Player.log (13:20): 7448 touceiras, ~4,0 M tri, 'fps médio 54'. Na segunda captura o 'passo' por quadro do Tigre e do Javali foi 0,31 (antes 0,02), o que indica ~7-10 fps durante o jogo. URP_Renderer tem SSAO Source=1 (DepthNormals). O MANUAL registra cobertura alfa de 10-16% (oliva-escuro), que de longe vira fiapos. MSAA está desligado e o recorte por alfa serrilha.
- **[alto] Não há medição de desempenho confiável.** — SmokeCapture.LogMovement calcula 'fps médio' como Time.frameCount / Time.realtimeSinceStartup, o que mistura boot e menu. Não mede tempo de quadro p50/p95/máximo, nem conta draw calls ou triângulos (ProfilerRecorder). BuildJogo usa BuildOptions.None: sem Development e sem ConnectWithProfiler. Nenhum orçamento numérico está escrito (fps alvo, triângulos, draw calls, memória).
- **[alto] Processo multi-sessão frágil: commits incompletos e HEAD mudando no meio do trabalho.** — A mensagem de 1800cbd diz: 'o commit chão aparece (de71e7c) saiu só com parte dos arquivos: ... o stash/pull/stash pop desfez o que estava no índice'. Durante esta leitura o HEAD foi de 32b8690 para 1800cbd e o CLAUDE.md e o docs/MANUAL.md foram reescritos e staged por outra sessão no mesmo diretório. Tools/Sincronizar.ps1 faria 'git pull --rebase --autostash' a cada 2 min se fosse instalado; hoje não está (schtasks não acha a tarefa).
- **[medio] Editor e build sujam a árvore com assets versionados e .meta de script sem versão.** — Sem commit agora: URP_Asset.asset (prefiltering, 60 linhas), URP_Renderer.asset (SSAO posto pelo SsaoSetup), DefaultVolumeProfile.asset (+785 linhas), UniversalRenderPipelineGlobalSettings.asset, ProjectSettings.asset (static batching) e GraphicsSettings m_LightsUseLinearIntensity de 0 para 1. Essa última muda como a intensidade da luz é lida, ou seja, muda cor e brilho da cena, e pode ter relação com o 'chão amarelo'. Oito .meta nunca foram versionados (SmokeCapture.cs.meta, GrassField.cs.meta, GroundBuilder.cs.meta, TowerStages.cs.meta, GridOverlay.cs.meta, World.meta, ArtImportRules.cs.meta, SsaoSetup.cs.meta), então o GUID muda de máquina para máquina. Os ShaderKeep/*.mat são regravados a cada build.
- **[alto] Variantes de shader no build dependem de uma lista manual e de Shader.Find.** — Shader.Find aparece em 10 lugares (ArtFactory:51,452; MaterialFactory:32; GridOverlay:37; TerritoryRenderer:43; SceneAmbience:85,106; Vfx:242; GrassField:83; GroundBuilder:130). Ainda há 'TODO(build): Shader.Find sofre stripping' em GrassField.cs e GroundBuilder.cs. Os materiais importados por Material Description (bichos, cenário Poly Haven, torres) têm keywords que não estão listadas. Com strictShaderVariantMatching=0 a falta não aparece rosa, só fica errado em silêncio. O terreno instanciado saía preto: a correção desligou drawInstanced e perdeu a normal por pixel no relevo.
- **[alto] Pronto para mobile só no papel.** — TowerWarsController lê Input.GetKeyDown direto (F9 na linha 217, E/Q nas 317-319, 1-9 na 329), pulando o IGameInput. HUD e menu em OnGUI com pixels fixos (SendButtonWidth 118 × 9 = 1116 px, teclas só até 9), sem Screen.dpi e sem safeArea. activeInputHandler=0 (só o Input legado). Nenhum alvo Android/iOS configurado e scriptingBackend vazio (iOS exige IL2CPP). Um URP_Asset só, de PC (sombra 4096 com 4 cascatas, SSAO, HDR), para Android e iPhone também (padrão de qualidade 2). Não há targetFrameRate nem escala de qualidade. Texturas .bytes vão por LoadImage em RGBA32, sem ASTC/ETC. ProcTex gera 18 texturas em ~1051 ms no boot de um PC com RTX 4070 Ti.
- **[medio] Multiplayer: a fundação é boa, mas o código ainda é de um jogador contra IA.** — MatchRunner.Apply só aplica comandos no Player e MatchCommand não tem PlayerId. Um único _rng é compartilhado por TowerWarsAi, TrySend e LeakRouter.Route. A simulação é toda em float, com Math.Pow (LaneSim.cs:773) e Math.Sqrt (729). Nenhum teste compara Mono (jogo) com .NET 10 (FlowSim) ou com IL2CPP/ARM64. StateFingerprint compara só contadores inteiros (vidas, ouro, renda, torres, kills), sem posição nem vida dos inimigos, então não detecta dessincronia. SendCatalog.All e TowerCatalog.All são estáticos, mutáveis e carregados de arquivo local. Não há transporte, lobby nem atraso de input.
- **[alto] Nenhum som no jogo.** — Não há nenhuma ocorrência de AudioSource, AudioClip ou PlayOneShot em Assets/_Project/Scripts. O módulo com.unity.modules.audio está no manifest. Os eventos da simulação já servem de gancho: LaneSim.EnemyDespawned, TowerFired, TowerChanged.
- **[alto] UI inteira em OnGUI, provisória.** — ModeSelect.OnGUI, TowerWarsController.OnGUI (linhas 372-460), DebugHud e FloatingText.OnGUI. Interpolam strings a cada passada (OnGUI roda várias vezes por quadro), não têm layout responsivo nem animação, e usam a fonte padrão. Não há Canvas nem UIDocument. uGUI 2.0 (com TMP) e UI Toolkit já estão instalados.
- **[medio] Duas simulações paralelas: o TD clássico não usa o LaneSim.** — GameController, Units/Enemy.cs, Towers/Tower.cs, Projectile.cs e TowerPlacer são MonoBehaviours com Time.deltaTime e Enemy.Alive estático. O FlowSim só testa Grid, Flow e Territory desse modo. Toda torre, bicho ou mercado novo teria que ser feito duas vezes, ou o modo clássico fica para trás.
- **[medio] Tipo novo de torre ou bicho exige mudar vários switch por índice, e as torres não têm vida (necessário para o segundo mercado).** — ModelLib.Tower, Enemy e Projectile são switch por int; tipo desconhecido cai em Torre_Canhao ou Inimigo_Cachorro. ModelLibTiers tem switch com case 0 a 5. ProjectileView.cs:56 testa `TowerTypeId == 1` para o arco do morteiro. AnimalLoader.Keywords é fixo. O HUD só cobre as teclas 1-9 e tem largura fixa. LaneSim.SimTower não tem Hp: inimigo que ataca torre precisa de vida de torre, alvo, dano e destruição. Refazer o caminho já existe, no vender (TowerVersion).
- **[medio] Peso morto e risco de licença dentro de Resources (tudo ali vai para o build, sem Addressables).** — Assets/Resources/TDFende/Bichos/Gerados tem 2 GLB brutos do Meshy (71 MB e 199 MB). Hoje são DefaultAsset e ficam fora do build, mas entrariam se alguém instalasse o glTFast. O AnimalPackSplitter ainda escreve prefabs ali e eles vencem os FBX (AnimalLoader.Index prefere 'Inimigo_*'). Torre_Canhao.fbx antigo continua ao lado dos estágios _1 a _3. Caixas/plastic_crate_01 e _02 continuam apesar da regra 'plastic|metal'. Resources ocupa ~459 MB no disco e Resources.LoadAll carrega pastas inteiras.
- **[medio] O cenário em volta é todo procedural low-poly.** — O Player.log lista só 'cenário Troncos, Pedras, Tocos, Barris, Caixas'; a pasta Cenario/Arvores não existe. O MANUAL explica que o pinheiro da Poly Haven tem 17 M triângulos. WorldLayout.ScatterScenery sorteia 560 pontos e WorldView usa no máximo MaxModelTrees=60 árvores de modelo; o resto é procedural.
- **[medio] Nenhum teste dentro do Unity e nenhum CI.** — com.unity.test-framework está no manifest, mas não existe nenhum asmdef de teste. A camada de vista (LaneView, EnemyView, AnimalLoader, ArtFactory) não tem teste além do print do -captura. O pre-commit só roda localmente e precisa de Windows com o Unity instalado em C:\Program Files\Unity\Hub\Editor.
- **[medio] Prontidão para a Steam: identidade, versão e créditos.** — companyName é 'DefaultCompany'; ao trocar, o persistentDataPath muda e replays e balanceamento se perdem. bundleVersion 1.0 sem versionamento, sem Steamworks. Não há tela de créditos, mas a CC BY dos 7 bichos do Sketchfab exige crédito (o texto pronto está no THIRD_PARTY.md). O plano da conta Meshy (grátis com CC BY ou pago) ainda não foi conferido. resizableWindow 0, sem menu de opções de vídeo.
- **[baixo] Código morto que pesa na leitura.** — ModelLib tem Spearman, Skirmisher, Knight, Horseman, Glider e SiegeTower sem nenhuma referência. CharacterLoader e CharacterImportRules (Mixamo) só são alcançáveis por AnimKind.Walker, e a pasta Personagens só tem um LEIA-ME. O fallback para o 'Standard' do Built-in em ArtFactory e MaterialFactory e o PackageBootstrap sobram desde que o URP é obrigatório no manifest.
- **[baixo] Ruído e pequenos defeitos na captura e na animação.** — O Player.log termina com 'Default clip could not be found in attached animations list.' 10 vezes: AnimalLoader.AddClips remove os clipes, mas o anim.clip padrão fica apontando para o removido. SmokeCapture chama CameraRig.Focus(..., 3f) e (..., 4f), mas CameraRigDriver.MinDist=8 limita, então o print 'de perto' sai a 8 unidades.
- **[baixo] Documentação desatualizada.** — README: '66 verificações' (são 183 Check), 'As teclas 1-6' (o código usa 1-9), 'Direção de arte: realista, fim da Idade Média' (o pedido agora é realista, não necessariamente medieval). TESTE.md: 'os seis tipos de inimigo', 'os quatro tipos de torre', 'javali e tigre seguem feitos em código'. Bichos/LEIA-ME.txt repete o javali e o tigre. O CLAUDE.md aponta para um ROADMAP.md que não existe.

### Pontos de extensão

- Torres e bichos são quase só dados. Um tipo novo é uma linha em TowerCatalog.All ou SendCatalog.All, combinando campos que já existem (SplashRadius, SlowFactor/SlowSeconds, BurnPctPerSecond, Knockback, VsFlyingMultiplier, AttritionScale, Count). TowerWarsAi, CatalogJson e BalanceLab já iteram Count.
- Os eventos da LaneSim (EnemyDespawned com DespawnReason, TowerFired, TowerChanged) são o gancho para som, VFX, UI e replicação em rede, sem tocar nas regras.
- Mercado novo ou ação nova (segundo mercado, ramo de evolução, habilidade) cabe num CommandKind novo em MatchCommand, com case em MatchRunner.Apply e no parser do Replay. O replay e o determinismo vêm junto.
- LeakRouter.NextLane já resolve 3 ou mais lanes, o que abre 2v2 e FFA. MatchSim e MatchRunner precisariam generalizar de 2 lanes para N.
- Inimigo que ataca torre: a LaneSim já reconstrói fronteira e flow field quando uma torre some (TrySellTowerAt e TowerVersion). Destruir torre pode reaproveitar esse caminho, somando Hp em SimTower e campos de ataque em SendUnit.
- IGameInput recebe uma TouchInput com pinça, arraste e toque longo para vender. Antes é preciso mover para a interface as leituras diretas do TowerWarsController (Q/E/1-9/F9).
- Arte sem código: ArtFactory usa o prefab de mesmo nome (Resources/TDFende/<nome> ou Torres/<torre>_<estágio>) e mantém as peças nomeadas (Turret, Barrel, Shaft, Top, Flag). AnimalLoader acha o bicho e os clipes Walk/Run/Idle/Death pelo nome. TowerStages.Count=3 controla os estágios de evolução.
- A lista BuildJogo.Shaders gera um material ShaderKeep por combinação. Cada material ou keyword nova entra ali, e o SmokeCapture.LogScene/LogEnemy confirma no executável.
- O SmokeCapture pode virar suíte de regressão visual e de desempenho: várias câmeras, ProfilerRecorder para tempo de quadro, triângulos e draw calls, e saída que dá para comparar entre builds.
- No FlowSim, o BalanceLab (match, sweep) e o replay medem o efeito de torres, bichos e do segundo mercado antes de ir para a vista.
- Pelo CatalogLoader (envios.txt, torres.txt) o Felipe ajusta números sem recompilar.
- No URP: QualitySettings tem 6 níveis que podem apontar para URP assets diferentes (PC alto e mobile). GPU Resident Drawer e oclusão por GPU exigem Forward+ e BRG variants. Renderer features (o SSAO já está) e um Volume em PostFx.cs.
- uGUI 2.0 com TextMeshPro e UI Toolkit (com.unity.modules.uielements) já estão instalados, o que permite menu, loja e HUD novos sem pacote novo. O áudio nativo (AudioSource, AudioMixer) também já existe.

### Restrições

- Git: commit e push direto na main. Começar com git pull --rebase --autostash; nunca stash ou stash pop entre um commit barrado pelo pre-commit e a nova tentativa; conferir git show --stat HEAD depois de cada commit. Várias sessões do Claude usam o mesmo diretório (o HEAD mudou durante esta leitura).
- Os assets que o Unity regrava em batch (Assets/Settings/*.asset, ProjectSettings/*.asset, DefaultVolumeProfile.asset) só entram em commit com o Felipe confirmando. Isso inclui o SSAO no URP_Renderer e m_LightsUseLinearIntensity=1, hoje sem commit.
- Teste antes da correção, visto falhando pelo motivo certo. Nada vai com teste vermelho. O pre-commit (CompileCheck + CompileCheckUrp + FlowSim, ~3 min) é obrigatório; --no-verify só de propósito.
- O Felipe joga pelo executável, não pelo editor. Algo visível só está pronto depois de gerar o build (Unity fechado, Tools/GerarExecutavel.ps1) e conferir o print e os logs do '-captura'.
- Regra de jogo nova vai em Runtime/Sim como lógica pura: sem MonoBehaviour nem Time.deltaTime, System.Random com semente (`using Random = System.Random` por causa do CS0104), só os tipos que o UnityStubs.cs do FlowSim tem. Cada arquivo novo precisa ser listado à mão em Tools/FlowSim/FlowSim.csproj.
- Determinismo: passo fixo de 1/30 s e comandos só na fronteira do tique. Nada da vista volta para a simulação.
- Material, shader ou keyword nova criada em runtime precisa de entrada em BuildJogo.Shaders. Não religar Terrain.drawInstanced sem prova por print. Tudo em Resources vai para o build.
- Modelos 3D: procurar pronto primeiro (Sketchfab CC BY, galeria Meshy CC0) e registrar autor, link e licença no THIRD_PARTY.md no mesmo commit. Gerar no Meshy só perguntando ao Felipe e dizendo o custo (~30 créditos por modelo com textura; há ~842). Aceitar EULA, logar ou comprar só o Felipe autoriza. Asset Store e Fab ficam só no PC, fora do Git, porque o repositório é público (.gitignore só deixa passar os 9 FBX e as texturas dos bichos).
- Direção de arte: realista e de última geração, não precisa ser medieval (pedido do Felipe de 06/10). Todo ganho visual tem que caber no orçamento de FPS, medido pelo -captura. Hoje não há orçamento numérico escrito.
- Stack fixa: Unity 6000.3.11f1, URP 17.3.0, Windows e D3D12 no PC do Felipe (RTX 4070 Ti). Pacote novo só quando nada instalado resolve (escada do CLAUDE.md global).
- Metas de arquitetura que não podem regredir: mobile-ready (IGameInput, sem custo fixo de PC no caminho principal) e multiplayer por comandos (MatchCommand + Replay).
- Commits e comentários em português. Fechar cada bloco de trabalho com DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT ou BLOCKED.

## Visão, decisões e histórico do TDFende (README.md, TESTE.md, ASSETS.md, THIRD_PARTY.md, CLAUDE.md, 59 commits de 29/07 a 06/10/2026, mais o estado não commitado)

O TDFende é um Line Tower Wars em Unity 6000.3.11f1 com URP 17.3.0. A tese é a fronteira territorial com atrito, herdada do Rise of Nations: cada torre projeta território e o inimigo dentro dele perde 0,19 da vida máxima por segundo. O comentário em TerritoryField.cs:9 resume: "Se ela for divertida aqui, o RTS tem alma". O alvo é Steam com arquitetura mobile-ready, e o projeto é treino antes de um RTS. Em 06/10/2026 o núcleo de simulação está maduro e travado por testes: lógica pura em Runtime/Sim, passo fixo de 1/30 s com semente, replay, laboratório de balanceamento com 300 partidas IA contra IA, cerca de 175 testes headless e um pre-commit que roda CompileCheck e FlowSim.

O núcleo de design foi decidido por medição, não por chute: atrito em porcentagem, escalada dos envios de 1,80 por minuto, morte súbita quadrática depois de 7 min, Águia imune ao atrito e regra de que cada torre responde a um envio.

A direção de arte mudou três vezes em dois meses: neon escuro em 31/07, cartoon em 25/09 e realista de fim da Idade Média em 26/09. Hoje mistura modelos em código, bichos CC BY do Sketchfab, torres CC0 da comunidade do Meshy e cenário da Poly Haven.

A pergunta central nunca teve resposta registrada: a "decisão da semana 4" no README e a pergunta 1 do TESTE.md, se cercar o inimigo com fronteira tem graça. Toda a interface (menu, loja de torres, envio de bichos, fim de jogo) ainda é OnGUI provisória com pixels fixos. Não há áudio, tela de créditos, pausa nem configurações.

O estado da main não reproduz o que os commits de 06/10 dizem ter verificado. O commit de71e7c descreve correções que estão só na árvore de trabalho. E o SmokeCapture.cs editado e quebrado barra a compilação do Unity e o pre-commit de qualquer sessão.

### O que existe

**Tese do projeto: fronteira + atrito** — `Assets/_Project/Scripts/Runtime/Grid/TerritoryField.cs, Runtime/Sim/TowerWarsConfig.cs, Runtime/Sim/TowerCatalog.cs, README.md`

Torre projeta território num raio por tipo: Canhão 2,75, Gelo 3,25, Morteiro 2,0, Sentinela 1,5, Fogo 2,0, Ar 2,25. Inimigo dentro perde AttritionPctPerSecond = 0,19 da vida MÁXIMA por segundo. Era dano fixo e virou porcentagem em c33f012, porque com dano fixo o atrito respondia por só 1,7% das mortes. O laboratório mede o atrito em cerca de 41% das mortes (a15b426). A morte por atrito tem cor própria (geada azulada) para dar leitura de qual mecânica está matando.

**Gênero alvo: Line Tower Wars** — `Runtime/Sim/LaneSim.cs (1013 linhas), MatchSim.cs, MatchRunner.cs, TowerWarsAi.cs, LeakRouter.cs, Core/ModeSelect.cs`

Você defende a sua lane e compra envios para a lane da IA. Cada envio custa ouro agora e soma renda para sempre: o triângulo torre × envio × renda (TowerWarsConfig: StartGold 120, BaseIncome 10 a cada 10 s, 20 vidas). Há 3 dificuldades de IA por utilidade (Fácil, Normal, Difícil, com CounterStrength contínuo). Quem passa da fortaleza custa 1 vida e volta a correr na lane do próximo adversário (LeakRouter, que já suporta mais de 2 jogadores). O TD clássico de uma lane (fase 0) foi mantido intocado ao lado.

**Objetivo e plataforma** — `README.md, Runtime/Input/IGameInput.cs, Runtime/Grid/FlowField.cs, Runtime/Sim/MatchCommand.cs, Runtime/Sim/Replay.cs`

Steam, mobile-ready desde o dia 1 (IGameInput, SimplePool, zero alocação em regime) e treino antes do RTS: flow field compartilhado é 'a técnica que o RTS vai usar'. O multiplayer por eventos foi planejado, mas não feito: MatchCommand na fila por tique, Replay e semente já têm o formato certo para isso.

**Roster atual** — `Runtime/Sim/SendCatalog.cs, Runtime/Sim/TowerCatalog.cs, Balanceamento/envios.txt, Balanceamento/torres.txt`

9 envios (SendCatalog, teclas 1-9):
- Rato 22 ouro, bando de 4, 15 HP
- Cachorro 10 ouro, 40 HP
- Lobo 35, veloz (4,0)
- Javali 30, 110 HP
- Águia 55, voa e ignora atrito
- Urso 40, 180 HP
- Tigre 60, 190 HP e rápido
- Rinoceronte 75, 330 HP
- Elefante 90, 450 HP

6 torres (TowerCatalog), 6 níveis cada, +85% de dano por nível. Venda devolve 70%. Gelo congela (0,45 a 0,8 s, depois 2 s imune). Fogo queima 5%/s da vida BASE, até 3 camadas, +15% por nível. Ar empurra 0,7 célula. Gelo e Ar desligam na morte súbita.

**Decisões de balanceamento e por quê (commits)** — `Runtime/Sim/TowerWarsConfig.cs, Tools/FlowSim/BalanceLab.cs`

- Envios escalam com o relógio, 1,80 por minuto: sem isso, 99% das partidas não acabavam (c33f012).
- Upgrades são a metade defensiva da escalada: sem eles o jogo era bimodal.
- Morte súbita quadrática depois de 7 min, aceleração 3,0 (1,6 → 2,2 → 3,0): garantia estrutural de que a partida termina (91fe458, 8254c5b, 0d088e1).
- Fogo queima a vida-base: proporcional à escalada, ele anulava a morte súbita, com 0 de 20 partidas decididas (5d652d3).
- IA sorteia com peso em vez de argmax: um envio dominava 68-94% das compras (1dad9dc).
- Ordem de tique da IA alterna: B via as compras de A e nunca o contrário.
- Águia imune ao atrito por design, senão território vira vitória automática.

Resultados medidos: Normal vence o Fácil em 73-83%, Difícil vence o Normal em 88%, mediana de 8,6 min (45ce22b).

**Regra de design para conteúdo novo** — `Runtime/Sim/TowerCatalog.cs, Runtime/Sim/SendCatalog.cs, Tools/FlowSim/Program.cs`

Cada torre precisa RESPONDER a um envio melhor que as outras. Nas palavras de 91fe458: 'A type that beats Cannon at nothing does not deserve to exist'. Os testes são escritos como contrato: Morteiro mata mais enxame que o Canhão, Sentinela mata a Águia mais rápido (3,23 s contra 4,40 s), Fogo vence o Canhão no Colosso. Cada envio precisa de silhueta distinta (SendCatalog.cs). Torres e envios são dados, não subclasses.

**Arquitetura e infraestrutura de verificação** — `.githooks/pre-commit, Tools/*, Runtime/Core/GameBootstrap.cs, Runtime/TowerWars/*`

Tudo nasce do GameBootstrap, sem cena nem prefab; Assets/Scenes/Jogo.unity só existe para o build. A simulação é pura: sem MonoBehaviour, sem Time.deltaTime, Random com alias para System.Random. A vista (LaneView, EnemyView, ProjectileView) só lê a simulação e interpola entre tiques de 30 Hz.

Ferramentas fora do Unity:
- Tools/FlowSim: cerca de 175 testes; Program.cs tem 183 chamadas Check(; modos match, sweep e replay.
- Tools/CompileCheck: compila Runtime/ contra as DLLs reais do editor.
- Tools/CompileCheckUrp: compila Editor e Urp quando Library/ existe.
- Tools/ArtPreview: three.js para conferir a arte.
- Tools/GerarExecutavel.ps1 e Editor/BuildJogo.cs: geram o exe de 280 MB sem abrir o Unity.
- SmokeCapture: modo -captura.
- F9 grava o replay.

.githooks/pre-commit roda CompileCheck, CompileCheckUrp e FlowSim (cerca de 3 min).

**Histórico da direção de arte** — `README.md, THIRD_PARTY.md, ASSETS.md, Runtime/Art/*, Tools/ConverterTorres/comunidade.json`

- 29/07: fase 0, primitivas.
- 31/07: minimalista escuro neon (648971b).
- 25/09: cartoon colorido, decidido pelo Felipe no primeiro Play real (4179d57).
- 25-26/09: realista. Primeiro o chão de grama da Poly Haven (92325c5, 'solo de grama primeiramente'), depois realista de fim da Idade Média tudo em código, com ProcTex e ModelLib (f077c2c).
- 30/09: soldados viram 9 bichos (0d088e1).
- 03-04/10: bichos reais do Sketchfab, torres em 3 estágios com 18 modelos CC0 do Karrades, fortaleza do Matson, acampamento do nathi.mashabane.
- 06/10: javali e tigre do Meshy com rig e animação 'Andando'.

O orçamento 'R$ 0, nada comprado' se manteve, mas virou 'baixar pronto grátis com licença conferida'.

**Regras de licença de assets** — `THIRD_PARTY.md, ASSETS.md, CLAUDE.md, .gitignore, Tools/ConverterBichos/bichos.json, Tools/ConverterTorres/comunidade.json`

- CC0 (Poly Haven, ambientCG, comunidade do Meshy): sem crédito, pode ir para o Git.
- CC BY 4.0 (7 bichos do Sketchfab): crédito obrigatório nos créditos do jogo; o texto pronto está no THIRD_PARTY.md.
- MIT (texturas do O3DE): o aviso precisa acompanhar o exe.
- Gerado no Meshy pela conta do Felipe: no plano pago é dele; no grátis é CC BY com crédito ao Meshy. Uma geração de 03/10 às 23:06 pode ter sido no plano grátis, então pôr '3D models generated with Meshy' nos créditos.
- Mixamo e Asset Store/Fab: só no PC, porque o repositório github.com/Felipebonamigo/TDFende é PÚBLICO. O .gitignore deixa passar só os 9 FBX de Bichos e Textures.
- Recusar 'personal use only', NonCommercial e reenvios de pacote pago. O 'Realistic Animated Pack' foi para a quarentena em C:\Users\Felip\TDFende-quarentena.
- CLAUDE.md: procurar pronto primeiro (Tools/ComunidadeMeshy/busca.py); gerar no Meshy só perguntando, a cerca de 30 créditos por modelo (20 do modelo e 10 da textura).

**Interface atual (pedido de agora: menu e loja bonitos)** — `Runtime/Core/ModeSelect.cs, Runtime/UI/UiSkin.cs, Runtime/TowerWars/TowerWarsController.cs:55-95 e 372-458, Runtime/UI/DebugHud.cs, Runtime/Core/Palette.cs`

Tudo em OnGUI com a pele UiSkin: painel escuro, texto cor de pergaminho, botão de madeira com borda dourada, texturas de 1 px.
- ModeSelect: uma caixa de 520×330 com título 'TDFende', botão TD clássico, três botões Fácil/Normal/Difícil e 'Exportar balanceamento'.
- Tower Wars:
  - InfoRect: 470×82 de texto (vidas, ouro, renda).
  - Barra de torres: 6 botões de 132×54, só nome e custo; Q/E trocam.
  - Barra de envios: 9 botões de 118×62 com '[n] Nome / custo / +renda', totalizando 1116 px de largura fixa.
  - HelpBox: 640×50 de texto corrido.
  - Fim de jogo: VITÓRIA/DERROTA com 'R para jogar de novo'.
- Sem ícones, retratos, stats (HP, velocidade, papel), tooltip ou painel de torre selecionada: clicar numa torre já sobe o nível.
- Sem pausa, sem Esc ou volta ao menu, sem configurações.
- Os retângulos do HUD ficam num lugar só (PointerOverHud) para o clique não vazar para o mundo; já foram 2 bugs reais (e7d9c39, 514fc45).

**Perguntas em aberto que dependem de playtest (TESTE.md §3 e README 'Próximo passo')** — `TESTE.md, README.md`

1. Cercar com fronteira e ver derreter tem graça? É a tese inteira; o README:222-225 a chama de 'decisão da semana 4'.
2. Existe a tensão 'gasto em defesa ou em ataque?'?
3. Dá para acompanhar as duas lanes?
4. O ritmo está bom? A IA contra IA leva cerca de 7-9 min.
5. A IA parece jogar ou trapacear?

Também: Fácil é ganhável e Difícil dói? Tuning de BorderRadius e atrito.

Visual (§0): já respondido em parte pelo Felipe. Está feio; FPS caiu para 54 com a grama; chão amarelo; cenário procedural pobre.

**Estado não commitado em 06/10** — `git status; Builds/build.log`

Scripts modificados:
- SmokeCapture.cs: close-up do bicho, QUEBRADO.
- ModeSelect.cs: Start() que dispara o -captura.
- GroundBuilder.cs: drawInstanced = false.
- BuildJogo.cs: enableInstancing nos materiais do ShaderKeep e variante _TERRAIN_INSTANCED_PERPIXEL_NORMAL.
- GrassField.cs: _BaseColor 1.15/1.35/0.95.
- TowerWarsController.cs: CameraRig e LaneGapForTests expostos.

Configurações regravadas pelo build: URP_Renderer com o recurso SSAO; URP_Asset com prefiltering de keywords; GraphicsSettings com m_LightsUseLinearIntensity 0→1; ProjectSettings com static batching; DefaultVolumeProfile com +785 linhas.

8 .meta não rastreados: TowerStages.cs, GridOverlay.cs, SmokeCapture.cs, World/, GrassField.cs, GroundBuilder.cs, ArtImportRules.cs, SsaoSetup.cs.

### Lacunas

- **[critico] Uma edição quebrada em SmokeCapture.cs barra o Unity e o pre-commit de TODAS as sessões. LogEnemy tem quebra de linha literal dentro de string interpolada; o \n virou newline de verdade.** — Builds/build.log: 'SmokeCapture.cs(141,154): error CS1039: Unterminated string literal', '(142,1): error CS1010: Newline in constant', idem em (146,105) e (147,1), e 'Scripts have compiler errors'. Tools/CompileCheck/CompileCheck.csproj:72 compila Runtime/**/*.cs da ÁRVORE DE TRABALHO, então qualquer commit é recusado até corrigir ou reverter.
- **[critico] Bichos invisíveis no executável, só o anel do time aparece. Não investigado.** — Relato do Felipe. Pistas para investigar, NÃO verificadas: - AnimalLoader.cs:232 liga _EMISSION em todo material de bicho, e :263 liga ou desliga _ALPHATEST_ON em runtime. - Assets/Resources/TDFende/ShaderKeep só guarda Lit puro, _ALPHATEST_ON, _ALPHATEST_ON+_NORMALMAP, _EMISSION, _NORMALMAP e _NORMALMAP+_EMISSION. Não há _ALPHATEST_ON+_EMISSION nem as três keywords juntas. - O build regravou o prefiltering do URP_Asset.asset (não commitado). - Os .meta dos FBX de Bichos estão fora do Git (.gitignore).
- **[alto] O commit de71e7c descreve correções que não estão no commit. A main no GitHub não reproduz o que foi 'conferido no executável' em 47eb2ab, 472cdac e 32b8690.** — git show --stat de71e7c lista só SmokeCapture.cs, CompileCheck.csproj e 1 material. Ficaram só na árvore de trabalho: - GroundBuilder drawInstanced = false: no HEAD, GroundBuilder.cs:140 ainda tem 'terrain.drawInstanced = true'. - O gancho ModeSelect.Start() que chama SmokeCapture.Begin: no HEAD, RequestedPath e Begin não têm chamador. - BuildJogo com mat.enableInstancing = true. Outra sessão que puxar a main e gerar o build volta a ter terreno preto e um '-captura' que não faz nada. Pela regra do CLAUDE.md, mais de uma sessão trabalha na main.
- **[alto] A tese central nunca foi validada nem a decisão registrada, enquanto o escopo cresce: mais bichos, mais torres, segundo mercado, visual.** — README.md:222-225: 'Playtest do Felipe → tuning de BorderRadius/AttritionDps → decisão da semana 4: fronteira + atrito diverte, ou o jogo vira TD clássico?'. TESTE.md §3, pergunta 1: 'Se a resposta for meh, a gente corta'. Nenhum doc nem commit registra a resposta; o projeto começou em 29/07, faz mais de 10 semanas.
- **[alto] A interface inteira é OnGUI provisória, com pixels fixos, sem escala por DPI nem safe area. Faltam: ícone e retrato na loja, stats, tooltip, painel de torre selecionada (o clique já sobe o nível), pausa, Esc ou volta ao menu, configurações e tela de créditos.** — TowerWarsController.cs:60-83: SendButtonWidth 118 × 9 envios = 1116 px; TowerButtonWidth 132 × 6; HelpBox de 640 px; InfoRect de 470×82. ModeSelect.cs: um GUI.Box de 520×330. DebugHud.cs:7 promete 'Vira UI de verdade (com âncoras e safe area, mobile-ready) na fase 2'. Nenhum uso de Canvas, UIDocument ou TMP no código, embora com.unity.ugui 2.0.0 e com.unity.modules.uielements estejam no Packages/manifest.json. Nenhum 'Escape', PlayerPrefs ou safeArea no código.
- **[alto] Sem nenhum áudio: música, efeitos de tiro, morte, compra e interface.** — grep por AudioSource/AudioClip em Assets/_Project/Scripts: zero resultados. Nenhum .wav, .ogg ou .mp3 em Assets.
- **[alto] Orçamento de desempenho estourado para um alvo mobile-ready: grama de cerca de 4M triângulos, torres sem redução de malha, árvores pesadas.** — 92325c5: GrassField 'stops on a 4M-triangle budget'. Felipe relatou FPS 54. Tools/ConverterTorres/comunidade.json: - Torre_Gelo_3: 95.078 - Torre_Ar_3: 85.514 - Torre_Morteiro_2 e _3: cerca de 73.470 cada - Fortaleza: 92.577 converte.py não tem decimate. 2283653: árvores reais 'cada uma pesa dezenas de milhares de triângulos', limitadas às 60 mais perto.
- **[alto] O conteúdo não escala além de 9 envios, e o segundo mercado (inimigo que ataca torre) é mudança de arquitetura na simulação.** — - TowerWarsController.cs:329: 'for (int i = 0; i < SendCatalog.Count && i < 9; i++)', com teclas Alpha1+i; a barra de envios tem largura fixa por contagem. - AnimalLoader.Keywords está fixo nos 9 nomes. - SendCatalog.LoadFrom recusa arquivo sem nenhum nome conhecido. - LaneSim.cs:50-58: SimTower não tem vida (Cell, Pos, Cooldown, Level, TypeId, Aim). Inimigo que ataca torre exige mudar a simulação, o formato dos catálogos de texto, a assinatura do replay, a IA e refazer a varredura de balanceamento.
- **[alto] Os créditos obrigatórios não existem no jogo, e a licença de parte dos modelos do Meshy está em dúvida. Isso bloqueia a Steam.** — THIRD_PARTY.md: CC BY 4.0 com 'crédito obrigatório nos créditos do jogo' para 7 bichos; 'Ao publicar o jogo (Steam), o aviso MIT abaixo precisa ir junto'; javali e tigre podem ter saído no plano grátis do Meshy (geração de 03/10, 23:06); Torre_Canhao.fbx do Meshy com 'Conferir o plano da conta antes de publicar'. O jogo não tem tela de créditos.
- **[medio] O 'mobile-ready' foi violado na prática: o Tower Wars lê o teclado direto, sem passar pela IGameInput. A TouchInput (fase 5) não existe.** — TowerWarsController.cs:217 (KeyCode.F9), :317-319 (KeyCode.E e Q), :329 (KeyCode.Alpha1 + i), todos via Input.GetKeyDown. IGameInput.cs:7: 'na fase 5 entra uma TouchInput... e nenhum outro arquivo do jogo muda'. ProjectSettings: activeInputHandler 0 (Input legado); sem o pacote Input System.
- **[medio] O CLAUDE.md do projeto não define o critério de mudança 'architectural', que o CLAUDE.md global exige que more no projeto.** — ~/.claude/CLAUDE.md: 'O critério de qual pedido é architectural é por projeto, e mora no CLAUDE.md dele'. TDFende/CLAUDE.md tem só 3 regras: git, idioma e modelos 3D. Mudanças como torre com vida, catálogo novo, formato de replay e UI nova serão reclassificadas a cada pedido.
- **[medio] Arquivos .meta de scripts commitados estão fora do Git, e configurações de render usadas no build verificado não estão commitadas.** — git status '??': TowerStages.cs.meta, GridOverlay.cs.meta, SmokeCapture.cs.meta, World.meta, GrassField.cs.meta, GroundBuilder.cs.meta, ArtImportRules.cs.meta, SsaoSetup.cs.meta, o que deixa o GUID instável entre clones. Também modificados e não commitados: URP_Renderer.asset (recurso SSAO), GraphicsSettings (m_LightsUseLinearIntensity 0→1), ProjectSettings (static batching) e DefaultVolumeProfile (+785 linhas).
- **[medio] Não há guia de estilo visual depois de 3 trocas de direção de arte. Modelos de 4 origens convivem: código, Sketchfab realista, comunidade do Meshy e Poly Haven.** — 648971b (neon, 31/07), 4179d57 (cartoon, 25/09), f077c2c (realista, 26/09), c25a8a5 e 4934b75 (modelos baixados, 04/10). README ainda descreve 'tudo em código'. Felipe: 'o jogo ainda não está bonito'; chão amarelo-seco, árvores procedurais low-poly.
- **[medio] Os shaders achados por nome somem no build se não forem guardados. Cada keyword ou shader novo precisa entrar na lista do BuildJogo.** — TODO(build) em TerritoryRenderer.cs:41, GroundBuilder.cs:128, GrassField.cs:82 e SceneAmbience.cs:84: 'Shader.Find sofre stripping em build'. A lista do ShaderKeep está em Editor/BuildJogo.cs. Ligado ao bug dos bichos invisíveis.
- **[medio] Faltam itens de publicação: nome da empresa, versão, Steamworks, save e configurações.** — ProjectSettings.asset: companyName 'DefaultCompany' (o README aponta para .../LocalLow/DefaultCompany/TDFende/Player.log), bundleVersion 1.0; sem Steamworks em Packages/manifest.json; nenhum PlayerPrefs.
- **[baixo] A documentação está desatualizada e contradiz o estado atual.** — - README: '66 verificações' (b3429ef diz 175); controle '1-6' e 'Construir torre (25 de ouro)', mas são 9 envios e 6 torres de 25 a 50 ouro; não cita Q/E, U, X/Delete nem F9. - TESTE.md: 'seis tipos de inimigo', 'quatro tipos de torre', 'javali e tigre seguem feitos em código', 'Normal ganha do Fácil em 95%' (commits: 73-83%), '~7 min' (45ce22b: mediana de 8,6). - ASSETS.md §2 e Bichos/LEIA-ME.txt: 'Javali e tigre ficam com o modelo feito em código' e 'só os 7'.
- **[baixo] Restam caminhos mortos de decisões revertidas.** — - Personagens Mixamo: CharacterLoader.cs, Editor/CharacterImportRules.cs e a pasta Personagens/. Os soldados viraram bichos em 0d088e1. - Editor/AnimalPackSplitter.cs: o pacote foi para a quarentena em 9a86e59. - Tools/BaixarBichos.ps1. - Torres/Torre_Canhao.fbx gerado pelo Meshy, ao lado de Torre_Canhao_1..3.
- **[baixo] A sincronização automática documentada não está instalada neste PC.** — 9cdd652 e ASSETS.md descrevem a tarefa agendada 'TDFende - Sincronizar' a cada 2 min. schtasks /query responde 'O sistema não pode encontrar o arquivo especificado', e Tools/sincronizar.log não existe.
- **[baixo] O modo clássico e o multiplayer foram prometidos e não feitos.** — GameConfig.cs:5: 'Vira data-driven (ScriptableObject/JSON) na fase 1'; continua const. O multiplayer por eventos aparece em MatchRunner.cs:14, LaneSim.cs:17 e LaneView.cs:10, mas não existe. O determinismo usa float e só foi testado no mesmo processo; não há prova de que vale entre máquinas.

### Pontos de extensão

- UI nova: o HUD só precisa enfileirar MatchCommand (Build, Send, Sell, Upgrade) no MatchRunner. A regra fica na simulação, então a loja e a seleção podem ser reescritas (uGUI ou UI Toolkit, os dois já instalados) sem tocar em regra, IA nem replay. O que precisa ser preservado: PointerOverHud (TowerWarsController.cs:90) ou equivalente, para o clique não vazar para o mundo.
- Tema: UiSkin.cs e as cores Palette.Ui* (UiPanel, UiInk, UiInkDim, UiAccent, UiButton*) são o ponto único de pele da interface atual.
- Conteúdo de envio: uma linha em SendCatalog.All (Name, Cost, Hp, Speed, IncomeBonus, Bounty, Count, AttritionScale), também editável em Balanceamento/envios.txt. O modelo entra por Resources/TDFende/Bichos/<nome>.fbx, com nome reconhecido em AnimalLoader.Keywords e clipes walk/run/idle/death/fly; a conversão é Tools/ConverterBichos/bichos.json → converte.py → verifica.py. O fallback procedural fica em ModelLibAnimals.cs. Os créditos vão no THIRD_PARTY.md.
- Conteúdo de torre: uma linha em TowerCatalog.All, com os campos de dados burn, burnsecs, push, splash, slow e vsflying. Os modelos são Resources/TDFende/Torres/Torre_<tipo>_1..3, escolhidos por TowerStages e ArtFactory.StageFor (níveis 1-2, 3-4, 5-6). As peças nomeadas Turret/Barrel/Shaft/Top/Flag/Lv2..Lv6 mantêm a animação do ModelRig. Pipeline: Tools/ConverterTorres/torres.json e comunidade.json.
- Modelo por nome: um prefab em Resources/TDFende/<nome> substitui o modelo procedural do mesmo nome (ArtFactory). Fortaleza e Acampamento usam o modo 'inteiro'.
- Economia e ritmo: TowerWarsConfig.cs (renda, morte súbita, gelo e fogo por nível, MaxTowerLevel = 6). Medir com 'dotnet run -- match 60' e 'sweep 25 fine' no Tools/FlowSim.
- Eventos da lane (TowerFired, TowerChanged, TowerSold, EnemyDespawned) e Vfx/FloatingText: ganchos naturais para áudio e feedback.
- LeakRouter já roteia para N lanes: base para modo com mais de 2 jogadores.
- IGameInput: lugar da TouchInput e de novas intenções (seleção de torre, compra). Hoje parte do input contorna essa interface.
- Mundo: WorldLayout (onde) e WorldView (como), GroundBuilder (terreno e camadas), GrassField (orçamento de triângulos), SceneAmbience e PostFx (luz, céu, ACES, bloom).
- Verificação: SmokeCapture ('TDFende.exe -captura print.png') para conferir build e movimento sem clicar, Tools/ArtPreview para a arte fora do Unity e F9 com 'dotnet run -- replay <arquivo>' para reproduzir bug.
- Busca de assets: Tools/ComunidadeMeshy/busca.py com buscas.json, só CC0, gera folha de miniaturas com autor e triângulos.

### Restrições

- Git: commit e push direto na main; 'git pull' antes de começar e 'git pull --rebase' antes do push; várias sessões no mesmo repositório; mensagens e comentários em português (CLAUDE.md).
- Pre-commit obrigatório (.githooks/pre-commit): CompileCheck da árvore de trabalho, mais CompileCheckUrp se houver Library/, mais FlowSim (cerca de 3 min). Nada entra vermelho; pular o hook vai contra a regra do Felipe 'nada é entregue com teste vermelho'.
- Regras globais do Felipe: - Teste escrito e visto falhar antes da correção. - Nenhuma alegação sem rodar o comando no mesmo turno. - Três correções falhando: parar e questionar a arquitetura. - Uma coisa de cada vez. - Fechar com DONE, DONE_WITH_CONCERNS, NEEDS_CONTEXT ou BLOCKED. - Na falta de critério do projeto, é 'architectural', com design escrito e aprovado, toda mudança de schema, de contrato ou de régua de acesso.
- Simulação pura em Runtime/Sim: sem MonoBehaviour, Time.deltaTime nem UnityEngine.Random; passo fixo de 1/30 s; comandos aplicados no início do tique; replay com assinatura de catálogo. Qualquer mudança de regra exige teste no FlowSim e nova varredura de balanceamento.
- O catálogo trava no boot (SendCatalog.Lock): o número de tipos não muda no meio da partida. Arquivo de balanceamento é texto, uma linha por unidade, decimal com ponto (InvariantCulture), UTF-8 com BOM. 'Voador' é derivado de attrition=0.
- Tudo montado em código a partir do GameBootstrap; a única cena é Assets/Scenes/Jogo.unity, para o build. Shader achado por nome precisa de material no ShaderKeep (lista em Editor/BuildJogo.cs), senão some no executável.
- Stack: Unity 6000.3.11f1, URP 17.3.0, Input legado (activeInputHandler 0), com.unity.ugui 2.0.0 e o módulo uielements disponíveis. O executável sai de Tools/GerarExecutavel.ps1 com o Unity FECHADO, em cerca de 2 min, em Builds/Windows/TDFende.exe (cerca de 280 MB).
- Assets: - Procurar pronto primeiro (Sketchfab, galeria CC0 do Meshy) e conferir licença e autoria. - Gerar no Meshy só em último caso e SEMPRE perguntar antes, dizendo quantos créditos: cerca de 30 por modelo com textura. Saldo de cerca de 842; o rig pareceu gratuito. - Recusar personal-use, NonCommercial e reenvio de pacote pago.
- Repositório PÚBLICO (github.com/Felipebonamigo/TDFende): Mixamo, Asset Store e Fab ficam só no PC, fora do Git. Em Bichos/ o .gitignore só deixa passar os 9 FBX listados e Textures/.
- Créditos: CC BY 4.0 dos 7 bichos do Sketchfab e aviso MIT do O3DE são obrigatórios no jogo publicado; o crédito ao Meshy é recomendado pela dúvida de plano.
- Mobile-ready é requisito declarado: pooling, zero alocação em regime, input por intenção. A UI nova precisa de âncoras e safe area, e o orçamento de triângulos precisa caber em mobile.
- Toda torre nova precisa responder a um envio melhor que as outras, com teste de contrato no FlowSim. Todo envio novo precisa de silhueta distinta e papel claro: enxame, régua, veloz, voador, gordo ou colosso.
