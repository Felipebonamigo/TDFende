using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace TDFende
{
    /// <summary>
    /// Conduz uma partida jogador × IA em passo fixo.
    ///
    /// A regra que faz tudo funcionar: comandos do jogador são aplicados em
    /// FRONTEIRA DE TIQUE, nunca no meio de um frame. Frame depende da máquina;
    /// tique não. Com isso, (semente + dificuldade + lista de comandos com o tique
    /// de cada um) reproduz a partida byte a byte — o que dá replay de graça hoje
    /// e é exatamente o que o multiplayer por eventos vai precisar depois.
    ///
    /// Lógica pura: roda dentro do Unity e no Tools/FlowSim igual.
    /// </summary>
    public class MatchRunner
    {
        public readonly LaneSim Player;
        public readonly LaneSim Foe;
        public readonly int Seed;
        public readonly TowerWarsAi.Personality Difficulty;

        readonly TowerWarsAi _foeAi;
        readonly CountingRandom _rng;
        readonly Queue<MatchCommand> _pending = new Queue<MatchCommand>();
        readonly LaneSim[] _lanes;

        /// <summary>Tiques já simulados. É o relógio ao qual os comandos são presos.</summary>
        public int TickCount { get; private set; }

        public bool Over => Player.Dead || Foe.Dead;

        /// <summary>Disparado quando um comando é ACEITO, com o tique em que valeu.</summary>
        public event System.Action<int, MatchCommand> CommandApplied;

        // ---- TEC-17: eventos só de vista (SimEvents.cs); não mudam estado nem sorteio ----

        /// <summary>Comando RECUSADO: tique, o comando e o motivo. Recusado não entra no replay.</summary>
        public event System.Action<int, MatchCommand, RejectReason> CommandRejected;

        /// <summary>A morte súbita começou (uma vez por partida), no tique informado.</summary>
        public event System.Action<int> SuddenDeathStarted;

        /// <summary>A partida terminou (uma vez): tique e id da lane que PERDEU (a que ficou sem vidas).</summary>
        public event System.Action<int, int> MatchEnded;

        bool _suddenDeathAnnounced, _endAnnounced;

        public MatchRunner(int seed, TowerWarsAi.Personality difficulty, int width, int height)
        {
            Seed = seed;
            Difficulty = difficulty;
            _rng = new CountingRandom(seed);
            Player = new LaneSim(width, height) { Id = 0, CarryLeaks = true };
            Foe = new LaneSim(width, height) { Id = 1, CarryLeaks = true };
            _lanes = new[] { Player, Foe };
            _foeAi = new TowerWarsAi(Foe, Player, difficulty, _rng);
        }

        /// <summary>Enfileira uma intenção. Ela vale no próximo tique, não agora.</summary>
        public void Enqueue(MatchCommand cmd) => _pending.Enqueue(cmd);

        /// <summary>Um passo fixo da partida.</summary>
        public void Step()
        {
            if (Over) return;

            while (_pending.Count > 0)
            {
                var cmd = _pending.Dequeue();
                if (Apply(cmd)) CommandApplied?.Invoke(TickCount, cmd);
                else CommandRejected?.Invoke(TickCount, cmd, WhyRejected(cmd));
            }

            float dt = TowerWarsConfig.FixedStep;
            _foeAi.Tick(dt);
            Player.Tick(dt);
            Foe.Tick(dt);
            // quem passou da base volta a correr, na lane do próximo adversário
            LeakRouter.Route(_lanes, _rng);
            TickCount++;

            if (!_suddenDeathAnnounced && Player.InSuddenDeath)
            {
                _suddenDeathAnnounced = true;
                SuddenDeathStarted?.Invoke(TickCount);
            }
            if (!_endAnnounced && Over)
            {
                _endAnnounced = true;
                MatchEnded?.Invoke(TickCount, Player.Dead ? Player.Id : Foe.Id);
            }
        }

        /// <summary>Motivo da recusa de um comando que acabou de falhar (o estado não mudou, então a pergunta ainda vale).</summary>
        RejectReason WhyRejected(MatchCommand cmd)
        {
            switch (cmd.Kind)
            {
                case CommandKind.Build:
                    if (cmd.TowerType < 0 || cmd.TowerType >= TowerCatalog.Count) return RejectReason.UnknownType;
                    return Player.BuildBlocker(new Vector2Int(cmd.X, cmd.Y), cmd.TowerType);
                case CommandKind.Upgrade: return Player.UpgradeBlocker(new Vector2Int(cmd.X, cmd.Y));
                case CommandKind.Send: return Player.SendBlocker(cmd.SendId);
                case CommandKind.Sell: return Player.SellBlocker(new Vector2Int(cmd.X, cmd.Y));
                default: return RejectReason.UnknownCommand;
            }
        }

        /// <summary>Devolve false se o comando foi recusado (sem ouro, célula inválida...).</summary>
        bool Apply(MatchCommand cmd)
        {
            switch (cmd.Kind)
            {
                case CommandKind.Build: return Player.TryBuildTower(new Vector2Int(cmd.X, cmd.Y), cmd.TowerType);
                case CommandKind.Upgrade: return Player.TryUpgradeTowerAt(new Vector2Int(cmd.X, cmd.Y));
                case CommandKind.Send: return Player.TrySend(cmd.SendId, Foe, _rng);
                case CommandKind.Sell: return Player.TrySellTowerAt(new Vector2Int(cmd.X, cmd.Y));
                default: return false;
            }
        }

        /// <summary>Sorteios feitos pela partida (entra no fingerprint).</summary>
        public long RngDraws => _rng.Draws;

        /// <summary>
        /// Assinatura do estado final. Duas execuções iguais têm que dar o mesmo. O texto legível (contadores)
        /// continua no começo; o hash de 64 bits no fim (TEC-31) acrescenta posição, vida e estado de cada
        /// inimigo, torre e tiro das duas lanes e o número de sorteios, que os contadores não enxergam.
        /// </summary>
        public string StateFingerprint()
        {
            var h = new StateHash();
            h.Add(TickCount);
            h.Add(RngDraws);
            h.Add(SimFingerprint.OfLane(Player));
            h.Add(SimFingerprint.OfLane(Foe));
            return $"t{TickCount} pl{Player.Lives}/{Player.Gold}/{Player.Income}/{Player.TowerCount}/{Player.TotalTowerLevels}" +
                   $" fo{Foe.Lives}/{Foe.Gold}/{Foe.Income}/{Foe.TowerCount}/{Foe.TotalTowerLevels}" +
                   $" k{Player.KilledByTower}/{Player.KilledByAttrition}/{Player.TotalLeaked}" +
                   $"/{Foe.KilledByTower}/{Foe.KilledByAttrition}/{Foe.TotalLeaked}" +
                   $" #{h.Value:x16}";
        }
    }
}
