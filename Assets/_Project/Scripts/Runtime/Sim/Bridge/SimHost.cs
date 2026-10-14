using System;
using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// A Sim vista de fora (U0 da migração para o Unreal): uma partida jogador x IA, comandos que entram, tiques que avançam,
    /// e DOIS fluxos que saem — o snapshot do estado (tipos planos de BridgeTypes.cs) e a fila de eventos. Quem desenha só lê
    /// isto; nada volta. É a mesma MatchRunner do jogo (mesmo fingerprint, mesmos replays). A camada nativa
    /// (Tools/SimBridge) só empacota esta classe atrás de uma interface C.
    ///
    /// Os catálogos são ESTÁTICOS e trancam na primeira lane: um processo, um conjunto de catálogos. Crie o host depois de
    /// carregar o balanceamento (os textos vão em <see cref="Create"/>).
    /// </summary>
    public sealed class SimHost
    {
        public MatchRunner Runner { get; }
        public int Width { get; }
        public int Height { get; }

        BridgeLane[] _lanes = new BridgeLane[2];
        BridgeEnemy[] _enemies = new BridgeEnemy[256];
        BridgeTower[] _towers = new BridgeTower[64];
        BridgeProjectile[] _shots = new BridgeProjectile[64];
        byte[] _territory;
        int _enemyCount, _towerCount, _shotCount;

        BridgeEvent[] _events = new BridgeEvent[256];
        int _eventCount;

        SimHost(MatchRunner runner, int width, int height)
        {
            Runner = runner;
            Width = width;
            Height = height;
            _territory = new byte[2 * width * height];
        }

        /// <param name="difficulty">0 Fácil, 1 Normal, 2 Difícil.</param>
        /// <param name="sendsText">envios.txt (vazio = fábrica). Só vale antes da primeira partida do processo.</param>
        public static SimHost Create(int seed, int difficulty, int width, int height, string sendsText, string towersText,
                                     out string error)
        {
            error = null;
            if (!string.IsNullOrWhiteSpace(sendsText) && !SendCatalog.LoadFrom(sendsText, out error)) return null;
            if (!string.IsNullOrWhiteSpace(towersText) && !TowerCatalog.LoadFrom(towersText, out error)) return null;
            if (width < 8 || height < 8) { error = "mapa pequeno demais"; return null; }
            var p = difficulty == 0 ? TowerWarsAi.Personality.Easy
                  : difficulty == 2 ? TowerWarsAi.Personality.Hard : TowerWarsAi.Personality.Normal;
            var runner = new MatchRunner(seed, p, width, height);
            var host = new SimHost(runner, width, height);
            host.Hook();
            return host;
        }

        // ---------------- entrada ----------------

        /// <summary>Enfileira um comando do jogador (vale no próximo tique). Devolve false se o tipo é desconhecido.</summary>
        public bool Enqueue(int kind, int x, int y, int arg)
        {
            switch (kind)
            {
                case BridgeCommand.Build: Runner.Enqueue(MatchCommand.Build(x, y, arg)); return true;
                case BridgeCommand.Upgrade: Runner.Enqueue(MatchCommand.Upgrade(x, y)); return true;
                case BridgeCommand.Sell: Runner.Enqueue(MatchCommand.Sell(x, y)); return true;
                case BridgeCommand.Send: Runner.Enqueue(MatchCommand.Send(arg)); return true;
                default: return false;
            }
        }

        // ---------------- perguntas da vista (U2): o que dá para fazer agora, sem conhecer C# ----------------

        /// <summary>
        /// Catálogo em texto, uma linha por tipo, campos separados por ';' (parta por ';', todo número em ponto decimal):
        /// kind 0 = torres: chave;nome;custo;alcance;recarga;dano;raio_de_área;raio_de_fronteira;fator_de_lentidão;
        /// kind 1 = envios: chave;nome;custo;vida;velocidade;quantidade;bônus_de_renda;recompensa;vidas_que_tira.
        /// Nulo se o kind é desconhecido. Campos novos entram SEMPRE no fim da linha (quem lê ignora o que não conhece).
        /// </summary>
        public string CatalogText(int kind)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            if (kind == 0)
            {
                for (int i = 0; i < TowerCatalog.Count; i++)
                {
                    var t = TowerCatalog.Get(i);
                    sb.Append(string.Format(inv, "{0};{1};{2};{3};{4};{5};{6};{7};{8}\n", t.Key, t.Name, t.Cost, t.Range, t.Cooldown,
                        t.Damage, t.SplashRadius, t.BorderRadius, t.SlowFactor));
                }
                return sb.ToString();
            }
            if (kind == 1)
            {
                for (int i = 0; i < SendCatalog.Count; i++)
                {
                    var u = SendCatalog.Get(i);
                    sb.Append(string.Format(inv, "{0};{1};{2};{3};{4};{5};{6};{7};{8}\n", u.Key, u.Name, u.Cost, u.Hp, u.Speed, u.Count,
                        u.IncomeBonus, u.Bounty, u.LivesCost));
                }
                return sb.ToString();
            }
            return null;
        }

        /// <summary>Por que NÃO dá para construir a torre <paramref name="type"/> na célula do jogador (0 = dá; senão um <see cref="RejectReason"/>).</summary>
        public int CanBuild(int x, int y, int type)
        {
            if (type < 0 || type >= TowerCatalog.Count) return (int)RejectReason.UnknownType;
            return (int)Runner.Player.BuildBlocker(new Vector2Int(x, y), type);
        }

        /// <summary>Custo de subir a torre da célula do jogador: -1 sem torre, 0 no nível máximo.</summary>
        public int UpgradeCost(int x, int y) => Runner.Player.UpgradeCostAt(new Vector2Int(x, y));

        /// <summary>Avança até <paramref name="ticks"/> tiques (para quando a partida acaba). Devolve quantos rodou.</summary>
        public int Step(int ticks)
        {
            int done = 0;
            for (; done < ticks && !Runner.Over; done++) Runner.Step();
            return done;
        }

        public int TickCount => Runner.TickCount;
        public bool Over => Runner.Over;
        public string Fingerprint() => Runner.StateFingerprint();

        // ---------------- snapshot ----------------

        public BridgeLane[] Lanes => _lanes;
        public BridgeEnemy[] Enemies => _enemies;
        public int EnemyCount => _enemyCount;
        public BridgeTower[] Towers => _towers;
        public int TowerCount => _towerCount;
        public BridgeProjectile[] Projectiles => _shots;
        public int ProjectileCount => _shotCount;
        /// <summary>Território: 1 byte por célula, lane 0 depois lane 1, linha a linha (y * Width + x). 1 = dentro.</summary>
        public byte[] Territory => _territory;

        /// <summary>Fotografa o estado atual nas matrizes planas acima. Sem alocação em regime.</summary>
        public void Snapshot()
        {
            _enemyCount = _towerCount = _shotCount = 0;
            var lanes = new[] { Runner.Player, Runner.Foe };
            for (int li = 0; li < lanes.Length; li++)
            {
                var lane = lanes[li];
                _lanes[li] = new BridgeLane
                {
                    Id = li, Lives = lane.Lives, Gold = lane.Gold, Income = lane.Income, TowerCount = lane.TowerCount,
                    EnemyCount = lane.EnemiesAlive, Dead = lane.Dead ? 1 : 0, InSuddenDeath = lane.InSuddenDeath ? 1 : 0,
                    TotalLeaked = lane.TotalLeaked, KilledByTower = lane.KilledByTower, KilledByAttrition = lane.KilledByAttrition,
                    TotalSent = lane.TotalSent, MatchTime = lane.MatchTime,
                    GoalX = lane.GoalWorld.x, GoalY = lane.GoalWorld.y, GoalZ = lane.GoalWorld.z
                };

                for (int s = 0; s < lane.EnemySlotCount; s++)
                {
                    if (!lane.TryGetEnemy(s, out var e)) continue;
                    Grow(ref _enemies, _enemyCount);
                    int flags = (e.IgnoresTerritory ? EnemyFlags.Flying : 0) | (e.Burning ? EnemyFlags.Burning : 0)
                              | (e.Frozen ? EnemyFlags.Frozen : 0) | (e.SlowLeft > 0f ? EnemyFlags.Slowed : 0)
                              | (e.Laps > 0 ? EnemyFlags.Carried : 0);
                    _enemies[_enemyCount++] = new BridgeEnemy
                    {
                        Lane = li, Slot = s, Generation = e.Generation, TypeId = e.TypeId, Flags = flags,
                        X = e.Pos.x, Y = e.Pos.y, Z = e.Pos.z, Hp = e.Hp, MaxHp = e.MaxHp
                    };
                }

                for (int i = 0; i < lane.TowerCount; i++)
                {
                    Grow(ref _towers, _towerCount);
                    var cell = lane.TowerCell(i);
                    var w = lane.Map.CellToWorld(cell);
                    bool aim = lane.TryGetTowerAim(i, out var a);
                    _towers[_towerCount++] = new BridgeTower
                    {
                        Lane = li, CellX = cell.x, CellY = cell.y, TypeId = lane.TowerTypeId(i), Level = lane.TowerLevel(i),
                        HasAim = aim ? 1 : 0, X = w.x, Y = w.y, Z = w.z, AimX = aim ? a.x : 0f, AimY = aim ? a.y : 0f, AimZ = aim ? a.z : 0f
                    };
                }

                for (int p = 0; p < lane.ProjectileSlotCount; p++)
                {
                    if (!lane.TryGetProjectile(p, out var pos, out float progress, out int tt, out bool flies)) continue;
                    lane.TryGetProjectileState(p, out var st);
                    Grow(ref _shots, _shotCount);
                    _shots[_shotCount++] = new BridgeProjectile
                    {
                        Lane = li, TowerType = tt, Level = st.Level, TargetFlies = flies ? 1 : 0,
                        X = pos.x, Y = pos.y, Z = pos.z, Progress = progress
                    };
                }

                int n = Width * Height, baseIdx = li * n;
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        _territory[baseIdx + y * Width + x] = (byte)(lane.Territory.Contains(x, y) ? 1 : 0);
            }
        }

        static void Grow<T>(ref T[] arr, int needIndex)
        {
            if (needIndex < arr.Length) return;
            Array.Resize(ref arr, arr.Length * 2);
        }

        // ---------------- eventos ----------------

        public BridgeEvent[] Events => _events;
        public int EventCount => _eventCount;

        /// <summary>Esvazia a fila de eventos (a vista já os consumiu). O snapshot não depende disto.</summary>
        public void ClearEvents() => _eventCount = 0;

        /// <summary>Mantém só os primeiros <paramref name="count"/> eventos (a ponte consumiu os outros).</summary>
        public void TrimEvents(int count) => _eventCount = count < 0 ? 0 : Math.Min(count, _eventCount);

        void Push(in BridgeEvent e)
        {
            Grow(ref _events, _eventCount);
            _events[_eventCount++] = e;
        }

        void Hook()
        {
            var lanes = new[] { Runner.Player, Runner.Foe };
            for (int li = 0; li < lanes.Length; li++)
            {
                int id = li;
                var lane = lanes[li];
                lane.EnemySpawned += e => Push(new BridgeEvent { Kind = BridgeEventKind.EnemySpawned, Lane = id, A = e.Slot, B = e.TypeId, C = e.Carried ? 1 : 0, X = e.Pos.x, Y = e.Pos.y, Z = e.Pos.z });
                lane.EnemyHit += e => Push(new BridgeEvent { Kind = BridgeEventKind.EnemyHit, Lane = id, A = e.Slot, B = e.TypeId, C = e.TowerTypeId, F = e.Damage, X = e.HpAfter, Y = e.Killed ? 1f : 0f });
                lane.StatusApplied += e => Push(new BridgeEvent { Kind = BridgeEventKind.StatusApplied, Lane = id, A = e.Slot, B = e.TypeId, C = (int)e.Kind, F = e.Seconds });
                lane.BountyPaid += e => Push(new BridgeEvent { Kind = BridgeEventKind.BountyPaid, Lane = id, A = e.Slot, B = e.Amount, C = (int)e.Reason, X = e.Pos.x, Y = e.Pos.y, Z = e.Pos.z });
                lane.IncomeTick += e => Push(new BridgeEvent { Kind = BridgeEventKind.IncomeTick, Lane = id, A = e.Amount });
                lane.SendBought += e => Push(new BridgeEvent { Kind = BridgeEventKind.SendBought, Lane = id, A = e.SendId, B = e.Cost, C = e.TargetLane });
                lane.LifeLost += e => Push(new BridgeEvent { Kind = BridgeEventKind.LifeLost, Lane = id, A = e.LivesLeft, B = e.TypeId, C = e.Cost });
                lane.TowerFired += p => Push(new BridgeEvent { Kind = BridgeEventKind.TowerFired, Lane = id, X = p.x, Y = p.y, Z = p.z });
                lane.TowerChanged += (p, level) => Push(new BridgeEvent { Kind = BridgeEventKind.TowerChanged, Lane = id, A = level, X = p.x, Y = p.y, Z = p.z });
                lane.TowerSold += (p, index, refund) => Push(new BridgeEvent { Kind = BridgeEventKind.TowerSold, Lane = id, A = index, B = refund, X = p.x, Y = p.y, Z = p.z });
                lane.EnemyDespawned += (p, why) => Push(new BridgeEvent { Kind = BridgeEventKind.EnemyDespawned, Lane = id, A = (int)why, X = p.x, Y = p.y, Z = p.z });
            }
            Runner.CommandRejected += (tick, cmd, why) => Push(new BridgeEvent { Kind = BridgeEventKind.CommandRejected, Lane = 0, A = (int)cmd.Kind, B = (int)why, C = tick });
            Runner.SuddenDeathStarted += tick => Push(new BridgeEvent { Kind = BridgeEventKind.SuddenDeathStarted, C = tick });
            Runner.MatchEnded += (tick, loser) => Push(new BridgeEvent { Kind = BridgeEventKind.MatchEnded, A = loser, C = tick });
        }
    }
}
