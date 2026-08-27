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
        }

        struct SimTower
        {
            public Vector2Int Cell;
            public Vector3 Pos;
            public float Cooldown;
            public int Level;
            public Vector3 Aim;      // último alvo visto; a vista gira o canhão para cá
            public bool HasAim;
        }

        struct SimProjectile
        {
            public bool Active;
            public int TargetSlot;
            public int TargetGeneration;
            public float Damage;
            public float TimeLeft;
            public float TotalTime;  // com Origin, permite à vista desenhar o tiro em voo
            public Vector3 Origin;
        }

        // ---- estado do tabuleiro ----
        public readonly GridMap Map;
        public readonly FlowField Flow;
        public readonly TerritoryField Territory;

        readonly List<SimTower> _towers = new List<SimTower>();
        readonly List<Vector2Int> _towerCells = new List<Vector2Int>();
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
        public float SendScale => 1f + (MatchTime / 60f) * TowerWarsConfig.SendScalePerMinute;

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
            Territory.Rebuild(_towerCells, TowerWarsConfig.BorderRadius);

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

        // ---------------- construção ----------------

        public bool CanBuild(Vector2Int cell)
        {
            if (Dead) return false;
            if (!Map.InBounds(cell.x, cell.y) || Map.IsBlocked(cell)) return false;
            if (cell == _goalCell) return false;
            for (int i = 0; i < _spawnCells.Count; i++)
                if (cell == _spawnCells[i]) return false;
            if (Gold < TowerWarsConfig.TowerCost) return false;

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

        public bool TryBuildTower(Vector2Int cell)
        {
            if (!CanBuild(cell)) return false;
            Gold -= TowerWarsConfig.TowerCost;
            GoldSpentOnTowers += TowerWarsConfig.TowerCost;

            Map.SetBlocked(cell, true);
            _towerCells.Add(cell);
            _towers.Add(new SimTower { Cell = cell, Pos = Map.CellToWorld(cell), Cooldown = 0f, Level = 1 });
            TowerChanged?.Invoke(Map.CellToWorld(cell), 1);

            Flow.Rebuild(_goalCell);
            Territory.Rebuild(_towerCells, TowerWarsConfig.BorderRadius);
            return true;
        }

        /// <summary>Custo de subir a torre mais barata de subir, ou -1 se nada é possível.</summary>
        public int CheapestUpgradeCost()
        {
            int best = -1;
            for (int i = 0; i < _towers.Count; i++)
            {
                if (_towers[i].Level >= TowerWarsConfig.MaxTowerLevel) continue;
                int c = TowerWarsConfig.UpgradeCost(_towers[i].Level);
                if (best < 0 || c < best) best = c;
            }
            return best;
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
                int c = TowerWarsConfig.UpgradeCost(_towers[i].Level);
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
                    dps += TowerWarsConfig.DamageAtLevel(_towers[i].Level) / TowerWarsConfig.TowerCooldown;
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
                    sum += TowerWarsConfig.DamageAtLevel(_towers[i].Level);
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
                target.SpawnIncoming(sendId, rng);
            return true;
        }

        void SpawnIncoming(int sendId, Random rng)
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
            _enemies[slot].Hp = _enemies[slot].MaxHp;
            _enemies[slot].Speed = u.Speed;
            _enemies[slot].AttritionScale = u.AttritionScale;
            // recompensa acompanha a vida, senão o defensor quebra no fim da partida
            _enemies[slot].Bounty = (int)(u.Bounty * scale);
            _enemies[slot].TypeId = sendId;
            _enemyCount++;
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

                var dir = Flow.SampleDirection(_enemies[i].Pos);
                _enemies[i].Pos += dir * (_enemies[i].Speed * dt);

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
                if (tw.Cooldown > 0f)
                {
                    _towers[t] = tw;
                    continue;
                }

                int target = -1;
                float best = TowerWarsConfig.TowerRange * TowerWarsConfig.TowerRange;
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

                if (target >= 0)
                {
                    // mira sempre que enxerga alguém, mesmo sem poder atirar ainda:
                    // é o que faz o canhão acompanhar o inimigo em vez de saltar no tiro
                    tw.Aim = _enemies[target].Pos;
                    tw.HasAim = true;

                    tw.Cooldown = TowerWarsConfig.TowerCooldown;
                    var muzzle = tw.Pos + Vector3.up * 0.95f;
                    FireProjectile(target, (float)Math.Sqrt(best), TowerWarsConfig.DamageAtLevel(tw.Level), muzzle);
                    TowerFired?.Invoke(muzzle);
                }
                else
                {
                    tw.HasAim = false;
                }
                _towers[t] = tw;
            }
        }

        void FireProjectile(int targetSlot, float distance, float damage, Vector3 origin)
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
            // tempo de voo importa: dano em trânsito para um alvo que já morreu é DPS jogado fora,
            // e é isso que faz torre empilhada render menos do que a conta ingênua diz.
            _projectiles[slot].TimeLeft = _projectiles[slot].TotalTime =
                Math.Max(distance / TowerWarsConfig.ProjectileSpeed, 0.0001f);
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

                _enemies[s].Hp -= _projectiles[p].Damage;
                if (_enemies[s].Hp <= 0f)
                {
                    Gold += _enemies[s].Bounty;
                    KilledByTower++;
                    EnemyDespawned?.Invoke(_enemies[s].Pos, DespawnReason.KilledByTower);
                    Kill(s);
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

        /// <summary>Para onde a torre está mirando; false se não viu ninguém no último tiro.</summary>
        public bool TryGetTowerAim(int index, out Vector3 aim)
        {
            aim = _towers[index].Aim;
            return _towers[index].HasAim;
        }

        public int ProjectileSlotCount => _projectiles.Length;

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
