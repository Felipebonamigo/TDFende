using System;
using Random = System.Random; // dentro do Unity, "Random" puro colide com UnityEngine.Random (CS0104)
using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Simulação headless de UMA lane (o tabuleiro de um jogador): grid, torres,
    /// inimigos, projéteis, fronteira, atrito e economia.
    ///
    /// É lógica pura de propósito — sem MonoBehaviour, sem Time.deltaTime, sem
    /// UnityEngine.Random. Isso dá três coisas de uma vez:
    ///   1. roda no Tools/FlowSim, então dá para balancear com milhares de partidas;
    ///   2. o lado Unity vira só uma VISTA disto, em vez de duplicar as regras;
    ///   3. passo fixo + Random injetado = partida reprodutível a partir da semente,
    ///      que é exatamente o que o multiplayer por eventos vai precisar depois.
    /// </summary>
    public class LaneSim
    {
        public struct SimEnemy
        {
            public bool Active;
            public int Generation;   // invalida projétil mirado num slot já reciclado
            public Vector3 Pos;
            public float Hp;
            public float MaxHp;
            public float Speed;
            public float AttritionScale;
            public int Bounty;
            public int TypeId;
            public bool IgnoresTerritory; // voador: alvo da Sentinela, imune ao atrito
            public float SlowFactor;      // 1 = velocidade cheia
            public float SlowLeft;        // segundos restantes de lentidão
            public float BaseHp;          // vida do tipo SEM a escalada: a régua do fogo
            public float BurnPct;         // fração da vida-base por segundo enquanto arde
            public float BurnLeft;        // segundos restantes de fogo (a vista desenha chama)
            public float KnockGuard;      // imune a novo empurrão enquanto > 0
            public float Chill;           // frio acumulado pelos tiros de Gelo; cheio = congela
            public float FrozenLeft;      // segundos congelado (parado no lugar)
            public float FreezeGuard;     // imune a congelar de novo enquanto > 0
            public int SenderId;          // lane de quem comprou o envio (-1 = ninguém)
            public int Laps;              // quantas bases já atravessou vivo
            public bool Burning => BurnLeft > 0f;

            public bool Frozen => FrozenLeft > 0f;
            public float CurrentSpeed => FrozenLeft > 0f ? 0f : SlowLeft > 0f ? Speed * SlowFactor : Speed;
        }

        struct SimTower
        {
            public Vector2Int Cell;
            public Vector3 Pos;
            public float Cooldown;
            public int Level;
            public int TypeId;
            public Vector3 Aim;      // último alvo visto; a vista gira o canhão para cá
            public bool HasAim;
        }

        /// <summary>Tiro em voo. Público só para leitura: a vista recebe CÓPIAS (struct).</summary>
        public struct SimProjectile
        {
            public bool Active;
            public int TargetSlot;
            public int TargetGeneration;
            public float Damage;
            public float TimeLeft;
            public float TotalTime;  // com Origin, permite à vista desenhar o tiro em voo
            public Vector3 Origin;
            public int TowerTypeId;  // decide área e bônus anti-aéreo no impacto
            public int Level;        // nível da torre que atirou: Gelo e Fogo ficam mais fortes
        }

        // ---- estado do tabuleiro ----
        public readonly GridMap Map;
        public readonly FlowField Flow;
        public readonly TerritoryField Territory;

        readonly List<SimTower> _towers = new List<SimTower>();
        readonly List<Vector2Int> _towerCells = new List<Vector2Int>();
        readonly List<float> _towerRadii = new List<float>();
        readonly List<Vector2Int> _spawnCells = new List<Vector2Int>();
        SimEnemy[] _enemies = new SimEnemy[256];
        SimProjectile[] _projectiles = new SimProjectile[512];
        int _enemyCount;

        Vector2Int _goalCell;
        Vector3 _goalWorld;

        // ---- economia ----
        public int Gold { get; private set; }
        public int Income { get; private set; }
        public int Lives { get; private set; }
        float _incomeTimer;

        /// <summary>Relógio da partida. Envios escalam com ele — é o que faz o jogo terminar.</summary>
        public float MatchTime { get; private set; }

        /// <summary>Multiplicador de vida/recompensa aplicado a quem nasce agora.</summary>
        public float SendScale
        {
            get
            {
                float minutes = MatchTime / 60f;
                float scale = 1f + minutes * TowerWarsConfig.SendScalePerMinute;

                // morte súbita: termo quadrático que garante o fim da partida
                float over = minutes - TowerWarsConfig.SuddenDeathMinutes;
                if (over > 0f) scale += over * over * TowerWarsConfig.SuddenDeathAccel;
                return scale;
            }
        }

        /// <summary>true depois do limiar de morte súbita — o HUD avisa.</summary>
        public bool InSuddenDeath => MatchTime / 60f > TowerWarsConfig.SuddenDeathMinutes;

        // ---- placar (para o balanceamento ler) ----
        public int TotalLeaked { get; private set; }
        public int KilledByTower { get; private set; }
        public int KilledByAttrition { get; private set; }
        public int TotalSent { get; private set; }
        public int GoldSpentOnTowers { get; private set; }
        public int GoldSpentOnSends { get; private set; }
        public int TotalUpgrades { get; private set; }

        /// <summary>Compras por tipo de envio. Se um tipo domina, o roster é decorativo.</summary>
        public readonly int[] SendsByType = new int[SendCatalog.Count];

        /// <summary>
        /// Disparado quando um inimigo sai do mapa, com onde e por quê.
        /// A simulação não sabe o que é partícula: quem escuta decide se desenha.
        /// É assim que a vista ganha feedback sem a lógica depender do Unity — e
        /// é o mesmo gancho que a rede vai usar para replicar eventos.
        /// </summary>
        public event System.Action<Vector3, DespawnReason> EnemyDespawned;

        /// <summary>Disparado quando uma torre é construída ou sobe de nível.</summary>
        public event System.Action<Vector3, int> TowerChanged;

        /// <summary>Disparado a cada tiro, na boca do cano — a vista faz o clarão.</summary>
        public event System.Action<Vector3> TowerFired;

        /// <summary>
        /// Disparado quando uma torre é vendida: posição, ÍNDICE que ela ocupava (as de
        /// depois descem uma casa — a vista tira o rig do mesmo lugar) e ouro devolvido.
        /// </summary>
        public event System.Action<Vector3, int, int> TowerSold;

        /// <summary>Sobe a cada torre construída ou vendida: a vista sabe quando refazer o território.</summary>
        public int TowerVersion { get; private set; }
        public int TotalSold { get; private set; }

        /// <summary>Posição desta lane na partida (0, 1, ...). -1 = lane avulsa, sem adversários.</summary>
        public int Id = -1;

        /// <summary>
        /// Quem passa da base volta ao jogo (ver <see cref="LeakRouter"/>): em vez de sumir, o
        /// inimigo sai para a fila de repasse com a vida que tinha. Desligado numa lane avulsa
        /// de teste, onde não há ninguém para receber.
        /// </summary>
        public bool CarryLeaks;
        readonly List<SimEnemy> _outgoing = new List<SimEnemy>();

        /// <summary>Tira da lane os inimigos que passaram da base neste tique (para o repasse).</summary>
        public void DrainLeaks(List<SimEnemy> into)
        {
            into.AddRange(_outgoing);
            _outgoing.Clear();
        }

        /// <summary>Quantas vezes, somando tudo, um inimigo voltou a correr nesta lane.</summary>
        public int TotalReentries { get; private set; }

        public int TowerCount => _towers.Count;
        public int EnemiesAlive => _enemyCount;
        public bool Dead => Lives <= 0;
        public IReadOnlyList<Vector2Int> SpawnCells => _spawnCells;

        public LaneSim(int width, int height, int seedlessSpawnRow = -1)
        {
            Map = new GridMap(width, height, GameConfig.CellSize);
            Flow = new FlowField(Map);
            Territory = new TerritoryField(Map);

            int row = seedlessSpawnRow >= 0 ? seedlessSpawnRow : height / 2;
            _goalCell = new Vector2Int(width - 3, row);
            _goalWorld = Map.CellToWorld(_goalCell);
            _spawnCells.Add(new Vector2Int(2, row));
            Flow.Rebuild(_goalCell);
            RebuildTerritory();

            // a partir daqui os catálogos não podem mais mudar: vetores já dimensionados
            SendCatalog.Lock();
            TowerCatalog.Lock();

            Gold = TowerWarsConfig.StartGold;
            Income = TowerWarsConfig.BaseIncome;
            Lives = TowerWarsConfig.StartLives;
        }

        // ---------------- economia ----------------

        /// <summary>Renda por tique. Devolve quanto pingou neste passo (0 na maioria).</summary>
        int TickIncome(float dt)
        {
            _incomeTimer += dt;
            if (_incomeTimer < TowerWarsConfig.IncomeTickSeconds) return 0;
            _incomeTimer -= TowerWarsConfig.IncomeTickSeconds;
            Gold += Income;
            return Income;
        }

        /// <summary>Segundos até o próximo pingo de renda — a IA usa para decidir se espera.</summary>
        public float TimeToIncome => TowerWarsConfig.IncomeTickSeconds - _incomeTimer;

        /// <summary>Ouro de graça, para montar cenário em teste e para o modo sandbox de playtest.</summary>
        public void DebugGrantGold(int amount) => Gold += amount;

        /// <summary>
        /// Manter a mira das torres viva entre disparos. É custo de VISTA: obriga a busca
        /// de alvo a rodar todo tique em vez de só no tique do tiro, e isso triplicou o
        /// tempo do laboratório de balanceamento quando estava sempre ligado.
        /// O disparo não muda em nenhum dos casos, então ligar ou não é indiferente para
        /// a simulação — e por isso fica desligado para quem só quer medir partidas.
        /// </summary>
        public bool TrackAim;

        // ---------------- construção ----------------

        public bool CanBuild(Vector2Int cell, int typeId = 0)
        {
            if (Dead) return false;
            if (!Map.InBounds(cell.x, cell.y) || Map.IsBlocked(cell)) return false;
            if (cell == _goalCell) return false;
            for (int i = 0; i < _spawnCells.Count; i++)
                if (cell == _spawnCells[i]) return false;
            if (Gold < TowerCatalog.Get(typeId).Cost) return false;

            // não construir em cima de inimigo
            var center = Map.CellToWorld(cell);
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (!_enemies[i].Active) continue;
                var d = _enemies[i].Pos - center;
                d.y = 0f;
                if (d.sqrMagnitude < 0.8f * 0.8f) return false;
            }

            // nunca deixar murar por completo
            return !Flow.PlacementBlocksPath(cell, _spawnCells);
        }

        public bool TryBuildTower(Vector2Int cell, int typeId = 0)
        {
            if (!CanBuild(cell, typeId)) return false;
            int cost = TowerCatalog.Get(typeId).Cost;
            Gold -= cost;
            GoldSpentOnTowers += cost;

            Map.SetBlocked(cell, true);
            _towerCells.Add(cell);
            _towers.Add(new SimTower
            {
                Cell = cell, Pos = Map.CellToWorld(cell), Cooldown = 0f, Level = 1, TypeId = typeId
            });
            TowerVersion++;
            TowerChanged?.Invoke(Map.CellToWorld(cell), 1);

            Flow.Rebuild(_goalCell);
            RebuildTerritory();
            return true;
        }

        /// <summary>
        /// Território é a UNIÃO dos raios de cada torre — e o raio varia por tipo, então
        /// uma linha de Gelo projeta fronteira bem mais larga que uma de Sentinela.
        /// É o que liga a escolha de torre à mecânica central do jogo.
        /// </summary>
        void RebuildTerritory()
        {
            _towerRadii.Clear();
            for (int i = 0; i < _towers.Count; i++)
                _towerRadii.Add(TowerCatalog.Get(_towers[i].TypeId).BorderRadius);
            Territory.Rebuild(_towerCells, _towerRadii);
        }

        /// <summary>
        /// Melhor upgrade disponível por DANO POR OURO, não por preço.
        /// Comparar preço bruto fazia a IA subir só Canhão: o upgrade de nível 1 dele custa
        /// 20, e nenhum outro upgrade do catálogo fica abaixo do preço de um Canhão novo (25).
        /// Medido: 995 torres de contra-jogo construídas, ZERO subidas de nível.
        /// </summary>
        public bool TryGetBestUpgrade(out int cost, out float dpsGain)
        {
            cost = 0;
            dpsGain = 0f;
            float bestRatio = -1f;

            for (int i = 0; i < _towers.Count; i++)
            {
                if (_towers[i].Level >= TowerWarsConfig.MaxTowerLevel) continue;
                int c = TowerCatalog.UpgradeCost(_towers[i].TypeId, _towers[i].Level);
                if (c <= 0 || Gold < c) continue;

                var type = TowerCatalog.Get(_towers[i].TypeId);
                float gain = (TowerCatalog.DamageAtLevel(_towers[i].TypeId, _towers[i].Level + 1)
                              - TowerCatalog.DamageAtLevel(_towers[i].TypeId, _towers[i].Level))
                             / type.Cooldown;
                float ratio = gain / c;
                if (ratio <= bestRatio) continue;

                bestRatio = ratio;
                cost = c;
                dpsGain = gain;
                _bestUpgradeIndex = i;
            }
            return bestRatio > 0f;
        }

        int _bestUpgradeIndex = -1;

        /// <summary>Aplica o upgrade escolhido por TryGetBestUpgrade.</summary>
        public bool TryUpgradeBest()
        {
            if (Dead || !TryGetBestUpgrade(out int cost, out _)) return false;
            int i = _bestUpgradeIndex;
            if (i < 0 || i >= _towers.Count) return false;

            Gold -= cost;
            GoldSpentOnTowers += cost;
            var t = _towers[i];
            t.Level++;
            _towers[i] = t;
            TotalUpgrades++;
            TowerChanged?.Invoke(t.Pos, t.Level);
            return true;
        }

        /// <summary>Custo de subir a torre mais barata de subir, ou -1 se nada é possível.</summary>
        public int CheapestUpgradeCost()
        {
            int best = -1;
            for (int i = 0; i < _towers.Count; i++)
            {
                if (_towers[i].Level >= TowerWarsConfig.MaxTowerLevel) continue;
                int c = TowerCatalog.UpgradeCost(_towers[i].TypeId, _towers[i].Level);
                if (best < 0 || c < best) best = c;
            }
            return best;
        }

        /// <summary>Índice da torre naquela célula, ou -1.</summary>
        public int TowerIndexAt(Vector2Int cell)
        {
            for (int i = 0; i < _towers.Count; i++)
                if (_towers[i].Cell == cell) return i;
            return -1;
        }

        /// <summary>
        /// Custo para subir a torre daquela célula. -1 se não há torre, e 0 se ela já
        /// está no nível máximo — a vista usa a diferença para explicar o porquê.
        /// </summary>
        public int UpgradeCostAt(Vector2Int cell)
        {
            int i = TowerIndexAt(cell);
            if (i < 0) return -1;
            if (_towers[i].Level >= TowerWarsConfig.MaxTowerLevel) return 0;
            return TowerCatalog.UpgradeCost(_towers[i].TypeId, _towers[i].Level);
        }

        /// <summary>
        /// Sobe a torre de uma célula escolhida. É a versão do jogador; a IA usa
        /// TryUpgradeCheapestTower. Sem isto, só a IA escalaria a defesa — e o
        /// balanceamento foi medido com os dois lados fazendo upgrade.
        /// </summary>
        public bool TryUpgradeTowerAt(Vector2Int cell)
        {
            if (Dead) return false;
            int i = TowerIndexAt(cell);
            if (i < 0) return false;

            var t = _towers[i];
            if (t.Level >= TowerWarsConfig.MaxTowerLevel) return false;
            int cost = TowerCatalog.UpgradeCost(t.TypeId, t.Level);
            if (Gold < cost) return false;

            Gold -= cost;
            GoldSpentOnTowers += cost;
            t.Level++;
            _towers[i] = t;
            TotalUpgrades++;
            TowerChanged?.Invoke(t.Pos, t.Level);
            return true;
        }

        /// <summary>Quanto a venda da torre daquela célula devolve, ou -1 se não há torre.</summary>
        public int SellValueAt(Vector2Int cell)
        {
            int i = TowerIndexAt(cell);
            return i < 0 ? -1 : SellValue(i);
        }

        int SellValue(int i)
        {
            var t = _towers[i];
            int invested = TowerCatalog.Get(t.TypeId).Cost;
            for (int level = 1; level < t.Level; level++)
                invested += TowerCatalog.UpgradeCost(t.TypeId, level);
            return (int)(invested * TowerWarsConfig.SellRefund);
        }

        /// <summary>
        /// Vende a torre da célula: devolve parte do ouro, libera a célula e refaz caminho
        /// e fronteira. Quem estava marchando só ganha um atalho — nunca fica preso,
        /// porque tirar bloqueio não fecha rota nenhuma.
        /// </summary>
        public bool TrySellTowerAt(Vector2Int cell)
        {
            if (Dead) return false;
            int i = TowerIndexAt(cell);
            if (i < 0) return false;

            int refund = SellValue(i);
            var pos = _towers[i].Pos;
            _towers.RemoveAt(i);
            _towerCells.RemoveAt(i);
            _bestUpgradeIndex = -1;
            Map.SetBlocked(cell, false);
            Gold += refund;
            TotalSold++;
            TowerVersion++;

            Flow.Rebuild(_goalCell);
            RebuildTerritory();
            TowerSold?.Invoke(pos, i, refund);
            return true;
        }

        /// <summary>
        /// Sobe a torre de menor nível que couber no bolso. Espalhar upgrade antes de
        /// concentrar rende mais dano por ouro, porque o custo cresce com o nível.
        /// </summary>
        public bool TryUpgradeCheapestTower()
        {
            if (Dead) return false;
            int pick = -1, pickCost = int.MaxValue;
            for (int i = 0; i < _towers.Count; i++)
            {
                if (_towers[i].Level >= TowerWarsConfig.MaxTowerLevel) continue;
                int c = TowerCatalog.UpgradeCost(_towers[i].TypeId, _towers[i].Level);
                if (c < pickCost) { pickCost = c; pick = i; }
            }
            if (pick < 0 || Gold < pickCost) return false;

            Gold -= pickCost;
            GoldSpentOnTowers += pickCost;
            var t = _towers[pick];
            t.Level++;
            _towers[pick] = t;
            TotalUpgrades++;
            TowerChanged?.Invoke(t.Pos, t.Level);
            return true;
        }

        /// <summary>DPS bruto real da defesa, já contando o nível de cada torre.</summary>
        public float TowerDps
        {
            get
            {
                float dps = 0f;
                for (int i = 0; i < _towers.Count; i++)
                    dps += TowerCatalog.DamageAtLevel(_towers[i].TypeId, _towers[i].Level)
                           / TowerCatalog.Get(_towers[i].TypeId).Cooldown;
                return dps;
            }
        }

        /// <summary>Dano médio por tiro da defesa — a IA usa para ler overkill contra enxame.</summary>
        public float AvgShotDamage
        {
            get
            {
                if (_towers.Count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _towers.Count; i++)
                    sum += TowerCatalog.DamageAtLevel(_towers[i].TypeId, _towers[i].Level);
                return sum / _towers.Count;
            }
        }

        /// <summary>Soma dos níveis — mede quanto a defesa acompanhou a escalada do ataque.</summary>
        public int TotalTowerLevels
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < _towers.Count; i++) sum += _towers[i].Level;
                return sum;
            }
        }

        // ---------------- envio ----------------

        public bool CanAfford(int sendId) => !Dead && Gold >= SendCatalog.Get(sendId).Cost;

        /// <summary>
        /// Compra um envio: paga daqui, sobe a renda DAQUI, e os bonecos nascem
        /// na lane do adversário (por isso o alvo é parâmetro).
        /// </summary>
        public bool TrySend(int sendId, LaneSim target, Random rng)
        {
            if (!CanAfford(sendId)) return false;
            var u = SendCatalog.Get(sendId);

            Gold -= u.Cost;
            GoldSpentOnSends += u.Cost;
            Income += u.IncomeBonus;
            TotalSent += u.Count;
            SendsByType[sendId]++;

            for (int i = 0; i < u.Count; i++)
                target.SpawnIncoming(sendId, rng, Id);
            return true;
        }

        void SpawnIncoming(int sendId, Random rng, int senderId = -1)
        {
            var u = SendCatalog.Get(sendId);
            var cell = _spawnCells[rng.Next(_spawnCells.Count)];
            var pos = Map.CellToWorld(cell);
            // espalha um pouco para não andarem em fila indiana
            pos.x += (float)(rng.NextDouble() * 0.6 - 0.3);
            pos.z += (float)(rng.NextDouble() * 0.6 - 0.3);
            pos.y = 0.5f;

            // escala pelo relógio DESTA lane (as duas correm juntas)
            float scale = SendScale;

            int slot = AllocEnemy();
            _enemies[slot].Active = true;
            _enemies[slot].Generation++;
            _enemies[slot].Pos = pos;
            _enemies[slot].MaxHp = u.Hp * scale;
            _enemies[slot].BaseHp = u.Hp;
            _enemies[slot].Hp = _enemies[slot].MaxHp;
            _enemies[slot].Speed = u.Speed;
            _enemies[slot].AttritionScale = u.AttritionScale;
            _enemies[slot].IgnoresTerritory = u.IgnoresTerritory;
            _enemies[slot].SlowFactor = 1f;
            _enemies[slot].SlowLeft = 0f;
            _enemies[slot].BurnPct = 0f;
            _enemies[slot].BurnLeft = 0f;
            _enemies[slot].KnockGuard = 0f;
            _enemies[slot].Chill = 0f;
            _enemies[slot].FrozenLeft = 0f;
            _enemies[slot].FreezeGuard = 0f;
            // recompensa acompanha a vida, senão o defensor quebra no fim da partida
            _enemies[slot].Bounty = (int)(u.Bounty * scale);
            _enemies[slot].TypeId = sendId;
            _enemies[slot].SenderId = senderId;
            _enemies[slot].Laps = 0;
            _enemyCount++;
        }

        /// <summary>
        /// Um inimigo que passou da base de alguém entra de novo, pelo acampamento DESTA lane,
        /// com a vida que tinha ao cruzar a base. Status (lentidão, fogo, empurrão) zeram:
        /// é uma corrida nova, só a ferida continua.
        /// </summary>
        public void SpawnCarried(in SimEnemy carried, Random rng)
        {
            if (Dead) return;
            var cell = _spawnCells[rng.Next(_spawnCells.Count)];
            var pos = Map.CellToWorld(cell);
            pos.x += (float)(rng.NextDouble() * 0.6 - 0.3);
            pos.z += (float)(rng.NextDouble() * 0.6 - 0.3);
            pos.y = 0.5f;

            int slot = AllocEnemy();
            int generation = _enemies[slot].Generation + 1;
            _enemies[slot] = carried;
            _enemies[slot].Active = true;
            _enemies[slot].Generation = generation;
            _enemies[slot].Pos = pos;
            _enemies[slot].SlowFactor = 1f;
            _enemies[slot].SlowLeft = 0f;
            _enemies[slot].BurnPct = 0f;
            _enemies[slot].BurnLeft = 0f;
            _enemies[slot].KnockGuard = 0f;
            _enemies[slot].Chill = 0f;
            _enemies[slot].FrozenLeft = 0f;
            _enemies[slot].FreezeGuard = 0f;
            _enemies[slot].Laps = carried.Laps + 1;
            _enemyCount++;
            TotalReentries++;
        }

        int AllocEnemy()
        {
            for (int i = 0; i < _enemies.Length; i++)
                if (!_enemies[i].Active) return i;
            int old = _enemies.Length;
            Array.Resize(ref _enemies, old * 2);
            return old;
        }

        // ---------------- passo da simulação ----------------

        public void Tick(float dt)
        {
            if (Dead) return;
            MatchTime += dt;
            TickIncome(dt);
            TickEnemies(dt);
            TickTowers(dt);
            TickProjectiles(dt);
        }

        void TickEnemies(float dt)
        {
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (!_enemies[i].Active) continue;

                if (_enemies[i].SlowLeft > 0f) _enemies[i].SlowLeft -= dt;
                if (_enemies[i].KnockGuard > 0f) _enemies[i].KnockGuard -= dt;
                if (_enemies[i].FrozenLeft > 0f) _enemies[i].FrozenLeft -= dt;
                if (_enemies[i].FreezeGuard > 0f) _enemies[i].FreezeGuard -= dt;
                // o frio se dissipa se ninguém continuar gelando
                if (_enemies[i].Chill > 0f) _enemies[i].Chill = Math.Max(0f, _enemies[i].Chill - 0.4f * dt);

                // fogo: dano contínuo proporcional à vida-BASE do tipo, não à vida escalada.
                // Proporcional à escalada, o fogo anularia a morte súbita (que existe
                // justamente para a vida subir e a partida acabar) — medido: 0/20 decididas.
                if (_enemies[i].BurnLeft > 0f)
                {
                    _enemies[i].BurnLeft -= dt;
                    _enemies[i].Hp -= _enemies[i].BaseHp * _enemies[i].BurnPct * dt;
                    if (_enemies[i].BurnLeft <= 0f) _enemies[i].BurnPct = 0f; // apagou: camadas zeram
                    if (_enemies[i].Hp <= 0f)
                    {
                        Gold += _enemies[i].Bounty;
                        KilledByTower++;
                        EnemyDespawned?.Invoke(_enemies[i].Pos, DespawnReason.KilledByTower);
                        Kill(i);
                        continue;
                    }
                }

                var dir = Flow.SampleDirection(_enemies[i].Pos);
                _enemies[i].Pos += dir * (_enemies[i].CurrentSpeed * dt);

                // atrito: perde vida só por estar dentro do território
                if (_enemies[i].AttritionScale > 0f && Territory.Contains(_enemies[i].Pos))
                {
                    _enemies[i].Hp -= _enemies[i].MaxHp * TowerWarsConfig.AttritionPctPerSecond
                                      * _enemies[i].AttritionScale * dt;
                    if (_enemies[i].Hp <= 0f)
                    {
                        Gold += _enemies[i].Bounty;
                        KilledByAttrition++;
                        EnemyDespawned?.Invoke(_enemies[i].Pos, DespawnReason.KilledByAttrition);
                        Kill(i);
                        continue;
                    }
                }

                var flat = _enemies[i].Pos;
                flat.y = 0f;
                if ((flat - _goalWorld).sqrMagnitude < 0.4f * 0.4f)
                {
                    Lives--;
                    TotalLeaked++;
                    EnemyDespawned?.Invoke(_enemies[i].Pos, DespawnReason.Leaked);
                    // não morreu: segue para a próxima base com a vida que tem
                    if (CarryLeaks) _outgoing.Add(_enemies[i]);
                    Kill(i);
                }
            }
        }

        void TickTowers(float dt)
        {
            for (int t = 0; t < _towers.Count; t++)
            {
                var tw = _towers[t];
                tw.Cooldown -= dt;

                // Sem vista escutando, basta procurar alvo quando dá para atirar.
                if (!TrackAim && tw.Cooldown > 0f)
                {
                    _towers[t] = tw;
                    continue;
                }

                // Com vista, a busca de alvo roda TODO tique, não só no tique do tiro. Com ela
                // atrás do cooldown, a mira só era atualizada a cada 0,65s: o cano
                // apontava para onde o inimigo ESTAVA, dava um tranco a cada tiro, e
                // o projétil saía de lado. O disparo em si continua preso ao cooldown,
                // então o comportamento da simulação não muda — só a mira fica viva.
                var type = TowerCatalog.Get(tw.TypeId);
                int target = -1;
                float best = type.Range * type.Range;
                for (int i = 0; i < _enemies.Length; i++)
                {
                    if (!_enemies[i].Active) continue;
                    var d = _enemies[i].Pos - tw.Pos;
                    d.y = 0f;
                    float sq = d.sqrMagnitude;
                    if (sq < best)
                    {
                        best = sq;
                        target = i;
                    }
                }

                tw.HasAim = target >= 0;
                if (tw.HasAim) tw.Aim = _enemies[target].Pos;

                if (target >= 0 && tw.Cooldown <= 0f)
                {
                    tw.Cooldown = type.Cooldown;
                    var muzzle = tw.Pos + Vector3.up * 0.95f;
                    FireProjectile(target, (float)Math.Sqrt(best),
                        TowerCatalog.DamageAtLevel(tw.TypeId, tw.Level), muzzle, tw.TypeId, tw.Level);
                    TowerFired?.Invoke(muzzle);
                }
                _towers[t] = tw;
            }
        }

        void FireProjectile(int targetSlot, float distance, float damage, Vector3 origin, int towerTypeId,
            int level = 1)
        {
            int slot = -1;
            for (int i = 0; i < _projectiles.Length; i++)
                if (!_projectiles[i].Active) { slot = i; break; }
            if (slot < 0)
            {
                slot = _projectiles.Length;
                Array.Resize(ref _projectiles, slot * 2);
            }

            _projectiles[slot].Active = true;
            _projectiles[slot].TargetSlot = targetSlot;
            _projectiles[slot].TargetGeneration = _enemies[targetSlot].Generation;
            _projectiles[slot].Damage = damage;
            _projectiles[slot].Origin = origin;
            _projectiles[slot].TowerTypeId = towerTypeId;
            _projectiles[slot].Level = level;
            // tempo de voo importa: dano em trânsito para um alvo que já morreu é DPS jogado fora,
            // e é isso que faz torre empilhada render menos do que a conta ingênua diz.
            _projectiles[slot].TimeLeft = _projectiles[slot].TotalTime =
                Math.Max(distance / TowerWarsConfig.ProjectileSpeed, 0.0001f);
        }

        /// <summary>Aplica dano e lentidão de um tipo de torre a um inimigo.</summary>
        void HitEnemy(int slot, float damage, TowerType type, int level = 1)
        {
            if (!_enemies[slot].Active) return;

            // Sentinela é a resposta ao voador: contra o resto ela é fraca de propósito.
            if (_enemies[slot].IgnoresTerritory) damage *= type.VsFlyingMultiplier;

            if (type.SlowSeconds > 0f)
            {
                // Gelo de nível alto segura mais: o fator de lentidão aprofunda com o nível
                float factor = (float)Math.Pow(type.SlowFactor, 1.0 + TowerWarsConfig.IceSlowPerLevel * (level - 1));
                _enemies[slot].SlowFactor = _enemies[slot].SlowLeft > 0f
                    ? Math.Min(_enemies[slot].SlowFactor, factor) : factor;
                _enemies[slot].SlowLeft = type.SlowSeconds;

                // Congelar: cada acerto junta frio; frio cheio = parado no lugar por um instante.
                // Depois fica um tempo imune, senão uma bateria de Gelo travava a marcha para
                // sempre. Na morte súbita não congela: a partida TEM que acabar.
                if (!InSuddenDeath && _enemies[slot].FreezeGuard <= 0f && _enemies[slot].FrozenLeft <= 0f)
                {
                    _enemies[slot].Chill += TowerWarsConfig.ChillPerHit(level);
                    if (_enemies[slot].Chill >= TowerWarsConfig.ChillToFreeze)
                    {
                        float freeze = TowerWarsConfig.FreezeSeconds(level);
                        _enemies[slot].Chill = 0f;
                        _enemies[slot].FrozenLeft = freeze;
                        _enemies[slot].FreezeGuard = freeze + TowerWarsConfig.FreezeGuardSeconds;
                    }
                }
            }

            // fogo acumula até 3 camadas: uma bateria de torres de Fogo queima de verdade,
            // uma torre sozinha é pouco contra o miúdo. Cada acerto renova a duração.
            // Torre de nível alto queima mais forte por camada.
            if (type.BurnSeconds > 0f && type.BurnPctPerSecond > 0f)
            {
                float perStack = type.BurnPctPerSecond * (1f + TowerWarsConfig.FireBurnPerLevel * (level - 1));
                _enemies[slot].BurnPct = Math.Min(_enemies[slot].BurnPct + perStack,
                    perStack * TowerWarsConfig.MaxBurnStacks);
                _enemies[slot].BurnLeft = type.BurnSeconds;
            }

            // Na morte súbita o vento não segura mais ninguém: a partida TEM que acabar.
            if (type.Knockback > 0f && _enemies[slot].KnockGuard <= 0f && !InSuddenDeath)
                Knock(slot, type.Knockback);

            _enemies[slot].Hp -= damage;
            if (_enemies[slot].Hp > 0f) return;

            Gold += _enemies[slot].Bounty;
            KilledByTower++;
            EnemyDespawned?.Invoke(_enemies[slot].Pos, DespawnReason.KilledByTower);
            Kill(slot);
        }

        /// <summary>
        /// Empurra o inimigo de volta pelo caminho: anda CONTRA o flow field em passos
        /// curtos, parando antes de entrar em célula bloqueada ou sair do mapa. Depois
        /// fica imune por um tempo — sem isso, três torres de Ar prenderiam o inimigo no
        /// lugar para sempre e a partida não acabaria.
        /// </summary>
        void Knock(int slot, float distance)
        {
            const float step = 0.1f;
            var pos = _enemies[slot].Pos;
            for (float moved = 0f; moved < distance; moved += step)
            {
                var dir = Flow.SampleDirection(pos);
                if (dir.sqrMagnitude < 0.0001f) break;
                var next = pos - dir * step;
                var cell = Map.WorldToCell(next);
                if (!Map.IsWalkable(cell.x, cell.y)) break;
                pos = next;
            }
            _enemies[slot].Pos = pos;
            _enemies[slot].KnockGuard = 1.6f;
        }

        void TickProjectiles(float dt)
        {
            for (int p = 0; p < _projectiles.Length; p++)
            {
                if (!_projectiles[p].Active) continue;
                _projectiles[p].TimeLeft -= dt;
                if (_projectiles[p].TimeLeft > 0f) continue;

                _projectiles[p].Active = false;
                int s = _projectiles[p].TargetSlot;
                if (!_enemies[s].Active || _enemies[s].Generation != _projectiles[p].TargetGeneration)
                    continue; // alvo morreu antes do impacto: tiro perdido

                var type = TowerCatalog.Get(_projectiles[p].TowerTypeId);
                var impact = _enemies[s].Pos;

                if (type.SplashRadius <= 0f)
                {
                    HitEnemy(s, _projectiles[p].Damage, type, _projectiles[p].Level);
                    continue;
                }

                // Área: pega o alvo e a vizinhança. É o que faz o Morteiro responder ao
                // Enxame — contra alvo único ele continua pior que o Canhão.
                float r2 = type.SplashRadius * type.SplashRadius;
                for (int i = _enemies.Length - 1; i >= 0; i--)
                {
                    if (!_enemies[i].Active) continue;
                    var d = _enemies[i].Pos - impact;
                    d.y = 0f;
                    if (d.sqrMagnitude > r2) continue;
                    HitEnemy(i, _projectiles[p].Damage, type, _projectiles[p].Level);
                }
            }
        }

        void Kill(int slot)
        {
            _enemies[slot].Active = false;
            _enemyCount--;
        }

        // ---------------- leitura para a VISTA ----------------
        // A camada Unity desenha a partir daqui em vez de manter estado próprio:
        // uma fonte da verdade só, e o que aparece na tela é o que a simulação diz.

        /// <summary>Número de compartimentos de inimigo (nem todos ativos). Iterar com TryGetEnemy.</summary>
        public int EnemySlotCount => _enemies.Length;

        /// <summary>Cópia do inimigo naquele compartimento; false se estiver vazio.</summary>
        public bool TryGetEnemy(int slot, out SimEnemy enemy)
        {
            enemy = _enemies[slot];
            return enemy.Active;
        }

        public Vector2Int TowerCell(int index) => _towers[index].Cell;
        public int TowerLevel(int index) => _towers[index].Level;
        public int TowerTypeId(int index) => _towers[index].TypeId;

        /// <summary>Tipo da torre naquela célula, ou -1 se não há torre.</summary>
        public int TowerTypeAt(Vector2Int cell)
        {
            int i = TowerIndexAt(cell);
            return i < 0 ? -1 : _towers[i].TypeId;
        }

        /// <summary>Para onde a torre está mirando; false se não viu ninguém no último tiro.</summary>
        public bool TryGetTowerAim(int index, out Vector3 aim)
        {
            aim = _towers[index].Aim;
            return _towers[index].HasAim;
        }

        public int ProjectileSlotCount => _projectiles.Length;

        /// <summary>
        /// Cópia crua do tiro naquele compartimento; false se estiver vazio. Por ser struct,
        /// quem lê não tem como alterar a simulação — é a porta da camada de vista.
        /// </summary>
        public bool TryGetProjectileState(int slot, out SimProjectile projectile)
        {
            projectile = _projectiles[slot];
            return projectile.Active;
        }

        /// <summary>O maior BurnPct que um inimigo consegue ter com o catálogo atual (camadas cheias).</summary>
        public static float MaxBurnPct
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < TowerCatalog.Count; i++)
                    max = Math.Max(max, TowerCatalog.Get(i).BurnPctPerSecond * TowerWarsConfig.MaxBurnStacks
                        * (1f + TowerWarsConfig.FireBurnPerLevel * (TowerWarsConfig.MaxTowerLevel - 1)));
                return max;
            }
        }

        /// <summary>
        /// Posição do tiro em voo, interpolada entre a boca do cano e o alvo.
        /// A simulação resolve o acerto por tempo, não por posição — isto existe só
        /// para a vista ter o que desenhar entre a torre e o inimigo.
        /// </summary>
        public bool TryGetProjectile(int slot, out Vector3 pos)
        {
            pos = default;
            if (!_projectiles[slot].Active) return false;

            int target = _projectiles[slot].TargetSlot;
            var dest = _enemies[target].Active
                       && _enemies[target].Generation == _projectiles[slot].TargetGeneration
                ? _enemies[target].Pos
                : _projectiles[slot].Origin; // alvo já morreu: some no lugar

            float t = 1f - _projectiles[slot].TimeLeft / _projectiles[slot].TotalTime;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            pos = _projectiles[slot].Origin + (dest - _projectiles[slot].Origin) * t;
            return true;
        }

        /// <summary>
        /// Como <see cref="TryGetProjectile(int, out Vector3)"/>, mais o que só a vista usa:
        /// quanto do voo já passou (0..1), qual torre atirou (bala, bomba, gelo, virote) e
        /// se o alvo voa (o tiro sobe até o planador em vez de mirar o chão).
        /// </summary>
        public bool TryGetProjectile(int slot, out Vector3 pos, out float progress, out int towerType,
            out bool targetFlies)
        {
            progress = 0f;
            towerType = 0;
            targetFlies = false;
            if (!TryGetProjectile(slot, out pos)) return false;
            var p = _projectiles[slot];
            progress = p.TotalTime > 0f ? 1f - p.TimeLeft / p.TotalTime : 1f;
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            towerType = p.TowerTypeId;
            targetFlies = _enemies[p.TargetSlot].IgnoresTerritory;
            return true;
        }

        public Vector2Int GoalCell => _goalCell;
        public Vector3 GoalWorld => _goalWorld;

        /// <summary>Soma de HP dos inimigos vivos — leitura de ameaça para a IA e para o HUD.</summary>
        public float ThreatHp()
        {
            float sum = 0f;
            for (int i = 0; i < _enemies.Length; i++)
                if (_enemies[i].Active) sum += _enemies[i].Hp;
            return sum;
        }

        /// <summary>Fração do caminho já vencida pelo inimigo mais adiantado (0 = spawn, 1 = base).</summary>
        public float DeepestProgress()
        {
            float startX = Map.CellToWorld(_spawnCells[0]).x;
            float span = _goalWorld.x - startX;
            if (span <= 0.001f) return 0f;

            float deepest = 0f;
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (!_enemies[i].Active) continue;
                float p = (_enemies[i].Pos.x - startX) / span;
                if (p > deepest) deepest = p;
            }
            return deepest;
        }
    }
}
