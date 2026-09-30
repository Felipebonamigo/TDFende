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
        readonly Random _rng;
        readonly Queue<MatchCommand> _pending = new Queue<MatchCommand>();

        /// <summary>Tiques já simulados. É o relógio ao qual os comandos são presos.</summary>
        public int TickCount { get; private set; }

        public bool Over => Player.Dead || Foe.Dead;

        /// <summary>Disparado quando um comando é ACEITO, com o tique em que valeu.</summary>
        public event System.Action<int, MatchCommand> CommandApplied;

        public MatchRunner(int seed, TowerWarsAi.Personality difficulty, int width, int height)
        {
            Seed = seed;
            Difficulty = difficulty;
            _rng = new Random(seed);
            Player = new LaneSim(width, height);
            Foe = new LaneSim(width, height);
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
            }

            float dt = TowerWarsConfig.FixedStep;
            _foeAi.Tick(dt);
            Player.Tick(dt);
            Foe.Tick(dt);
            TickCount++;
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

        /// <summary>Assinatura do estado final. Duas execuções iguais têm que dar o mesmo.</summary>
        public string StateFingerprint() =>
            $"t{TickCount} pl{Player.Lives}/{Player.Gold}/{Player.Income}/{Player.TowerCount}/{Player.TotalTowerLevels}" +
            $" fo{Foe.Lives}/{Foe.Gold}/{Foe.Income}/{Foe.TowerCount}/{Foe.TotalTowerLevels}" +
            $" k{Player.KilledByTower}/{Player.KilledByAttrition}/{Player.TotalLeaked}" +
            $"/{Foe.KilledByTower}/{Foe.KilledByAttrition}/{Foe.TotalLeaked}";
    }
}
