using System;

namespace TDFende
{
    /// <summary>
    /// Uma partida Tower Wars entre dois lados. Passo fixo e Random com semente:
    /// mesma semente = mesma partida, sempre. É o que torna balanceamento uma medição
    /// em vez de uma impressão.
    /// </summary>
    public class MatchSim
    {
        public LaneSim A { get; }
        public LaneSim B { get; }
        public float Elapsed { get; private set; }

        readonly TowerWarsAi _aiA;
        readonly TowerWarsAi _aiB;
        readonly Random _rng;
        readonly LaneSim[] _lanes;

        public MatchSim(TowerWarsAi.Personality pa, TowerWarsAi.Personality pb, int seed,
            int width = 24, int height = 16)
        {
            var rng = new Random(seed);
            _rng = rng;
            A = new LaneSim(width, height) { Id = 0, CarryLeaks = true };
            B = new LaneSim(width, height) { Id = 1, CarryLeaks = true };
            _lanes = new[] { A, B };
            _aiA = new TowerWarsAi(A, B, pa, rng);
            _aiB = new TowerWarsAi(B, A, pb, rng);
        }

        /// <summary>Roda até alguém morrer ou o tempo limite. Devolve o resultado.</summary>
        public MatchResult Run()
        {
            float dt = TowerWarsConfig.FixedStep;
            bool flip = false;
            while (Elapsed < TowerWarsConfig.MatchTimeLimit && !A.Dead && !B.Dead)
            {
                // Alterna quem decide primeiro a cada passo. Com ordem fixa, o segundo
                // via as compras do primeiro DENTRO do mesmo passo — e como os timers de
                // decisão se realinham, a vantagem virava permanente na partida inteira
                // (medido: trocar as duas linhas invertia o vencedor em sementes
                // alinhadas). Alternar dá o primeiro movimento a cada lado metade do
                // tempo, e continua determinístico por semente.
                if (flip)
                {
                    _aiB.Tick(dt);
                    _aiA.Tick(dt);
                }
                else
                {
                    _aiA.Tick(dt);
                    _aiB.Tick(dt);
                }
                flip = !flip;

                A.Tick(dt);
                B.Tick(dt);
                LeakRouter.Route(_lanes, _rng);
                Elapsed += dt;
            }

            int winner;
            if (A.Dead && B.Dead) winner = 0;                 // morreram no mesmo passo
            else if (A.Dead) winner = 2;
            else if (B.Dead) winner = 1;
            else if (A.Lives != B.Lives) winner = A.Lives > B.Lives ? 1 : 2; // tempo esgotou
            else winner = 0;

            return new MatchResult
            {
                Winner = winner,
                Seconds = Elapsed,
                LivesA = A.Lives,
                LivesB = B.Lives,
                IncomeA = A.Income,
                IncomeB = B.Income,
                TowersA = A.TowerCount,
                TowersB = B.TowerCount,
                AttritionKillsA = A.KilledByAttrition,
                TowerKillsA = A.KilledByTower,
                AttritionKillsB = B.KilledByAttrition,
                TowerKillsB = B.KilledByTower,
            };
        }
    }

    public struct MatchResult
    {
        public int Winner; // 0 = empate, 1 = A, 2 = B
        public float Seconds;
        public int LivesA, LivesB;
        public int IncomeA, IncomeB;
        public int TowersA, TowersB;
        public int AttritionKillsA, TowerKillsA;
        public int AttritionKillsB, TowerKillsB;

        /// <summary>Fração das mortes causadas por atrito — mede se a fronteira importa de fato.</summary>
        public float AttritionShare
        {
            get
            {
                int att = AttritionKillsA + AttritionKillsB;
                int tot = att + TowerKillsA + TowerKillsB;
                return tot == 0 ? 0f : att / (float)tot;
            }
        }
    }
}
