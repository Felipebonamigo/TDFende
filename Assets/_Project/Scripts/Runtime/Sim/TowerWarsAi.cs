using System;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// IA de Tower Wars por utilidade: a cada janela de decisão ela compara o valor
    /// de DEFENDER (torre), ATACAR (envio, que também sobe renda) e ESPERAR.
    ///
    /// É o sistema que decide se o modo vs. IA presta — e é justamente o que não dá
    /// para ajustar "no olho": os pesos abaixo saem das partidas IA×IA do FlowSim.
    /// </summary>
    public class TowerWarsAi
    {
        public struct Personality
        {
            public string Name;
            public float DecisionInterval; // segundos entre decisões (reação)
            public float SafetyMargin;     // quanto de folga defensiva exige antes de atacar
            public float GreedBias;        // >1 favorece renda; <1 favorece defesa
            public bool PlaysCounters;     // percebe território inimigo e manda voador
            public int PlacementSamples;   // quantas células avalia por torre (esperteza do maze)

            public static Personality Easy => new Personality
            {
                Name = "Fácil", DecisionInterval = 2.5f, SafetyMargin = 2.2f,
                GreedBias = 0.6f, PlaysCounters = false, PlacementSamples = 6
            };

            public static Personality Normal => new Personality
            {
                Name = "Normal", DecisionInterval = 1.2f, SafetyMargin = 1.4f,
                GreedBias = 1.0f, PlaysCounters = true, PlacementSamples = 18
            };

            public static Personality Hard => new Personality
            {
                Name = "Difícil", DecisionInterval = 0.6f, SafetyMargin = 1.0f,
                GreedBias = 1.35f, PlaysCounters = true, PlacementSamples = 40
            };
        }

        readonly LaneSim _me;
        readonly LaneSim _foe;
        readonly Personality _p;
        readonly Random _rng;
        float _timer;

        public TowerWarsAi(LaneSim me, LaneSim foe, Personality p, Random rng)
        {
            _me = me;
            _foe = foe;
            _p = p;
            _rng = rng;
            // desencontra as decisões dos dois lados: sem isso, agem sempre no mesmo passo
            _timer = (float)_rng.NextDouble() * p.DecisionInterval;
        }

        public void Tick(float dt)
        {
            if (_me.Dead) return;
            _timer -= dt;
            if (_timer > 0f) return;
            _timer = _p.DecisionInterval;
            Decide();
        }

        void Decide()
        {
            // Capacidade de matar já contando o nível das torres.
            // Superestima (ignora alcance e tiro perdido), e é por isso que SafetyMargin existe.
            float myDps = _me.TowerDps;
            float threat = _me.ThreatHp();
            float deepest = _me.DeepestProgress();

            // Perigo cresce com HP na tela, com o quanto o mais adiantado já andou,
            // e desaba conforme eu tenho DPS para responder.
            float danger = (threat + 1f) / (myDps + 1f);
            danger *= 1f + deepest * 1.5f;
            if (_me.Lives <= 5) danger *= 2f; // quase morto: para de brincar de economia

            bool wantsTower = danger > _p.SafetyMargin || _me.TowerCount == 0;

            if (wantsTower && ReinforceDefense()) return;

            // Sem pressão: converte ouro em renda + pressão no adversário.
            if (!wantsTower || _me.Gold >= TowerWarsConfig.TowerCost * 3)
            {
                int pick = ChooseSend();
                if (pick >= 0 && _me.TrySend(pick, _foe, _rng)) return;
            }

            // Sobrou ouro e nada urgente: engrossa a defesa.
            if (_me.Gold >= TowerWarsConfig.TowerCost * 4)
                ReinforceDefense();
        }

        /// <summary>
        /// Torre nova x subir torre existente. Cobertura primeiro (torre isolada não
        /// segura nada), potência depois — e o custo crescente do upgrade faz as duas
        /// opções continuarem competindo até o fim da partida.
        /// </summary>
        bool ReinforceDefense()
        {
            const int CoverageTarget = 6;

            if (_me.TowerCount < CoverageTarget)
                return BuildSomewhere() || TryUpgrade();

            int up = _me.CheapestUpgradeCost();
            bool canUpgrade = up >= 0 && _me.Gold >= up;
            bool canBuild = _me.Gold >= TowerWarsConfig.TowerCost;

            // Com cobertura feita, sobe o que for mais barato por dano entregue.
            if (canUpgrade && (!canBuild || up <= TowerWarsConfig.TowerCost))
                return TryUpgrade() || BuildSomewhere();

            return BuildSomewhere() || TryUpgrade();
        }

        bool TryUpgrade() => _me.TryUpgradeCheapestTower();

        /// <summary>
        /// Escolhe o envio de melhor utilidade: renda por ouro, com bônus para o que
        /// o adversário tem dificuldade de matar.
        /// </summary>
        int ChooseSend()
        {
            int best = -1;
            float bestScore = 0f;
            bool foeHasTerritory = _foe.TowerCount >= 4;

            for (int id = 0; id < SendCatalog.Count; id++)
            {
                if (!_me.CanAfford(id)) continue;
                var u = SendCatalog.Get(id);

                // Base: renda comprada por ouro gasto.
                float score = u.IncomeBonus / (float)u.Cost;
                score *= _p.GreedBias;

                // Pressão: HP entregue por ouro (o que realmente ameaça vazar).
                score += 0.35f * (u.Hp * u.Count) / (u.Cost * 100f);

                if (_p.PlaysCounters)
                {
                    // Voador contra quem investiu em território é o contra-jogo do modo.
                    if (u.IgnoresTerritory && foeHasTerritory) score *= 1.6f;
                    // Enxame frágil derrete no atrito: evita quando o outro tem fronteira.
                    if (u.AttritionScale > 1.2f && foeHasTerritory) score *= 0.55f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = id;
                }
            }
            return best;
        }

        /// <summary>
        /// Coloca torre por amostragem: sorteia células válidas e fica com a de melhor
        /// nota. Mais amostras = maze mais esperto, e é assim que a dificuldade escala
        /// sem precisar de uma IA diferente por nível.
        /// </summary>
        bool BuildSomewhere()
        {
            int w = _me.Map.Width, h = _me.Map.Height;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int s = 0; s < _p.PlacementSamples; s++)
            {
                var cell = new Vector2Int(_rng.Next(1, w - 1), _rng.Next(0, h));
                if (!_me.CanBuild(cell)) continue;

                float score = 0f;

                // Perto do corredor central: é por onde o fluxo passa.
                float rowDist = Math.Abs(cell.y - h * 0.5f);
                score -= rowDist * 0.6f;

                // Encostada em torre existente: adensa o território e alonga o maze.
                int neighbours = 0;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cell.x + dx, ny = cell.y + dy;
                    if (_me.Map.InBounds(nx, ny) && _me.Map.IsBlocked(nx, ny)) neighbours++;
                }
                score += neighbours * 1.4f;

                // Prefere a metade da frente: intercepta cedo em vez de na porta da base.
                score += (1f - cell.x / (float)w) * 2.0f;

                score += (float)_rng.NextDouble() * 0.8f; // desempate, evita padrão robótico

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                    found = true;
                }
            }

            return found && _me.TryBuildTower(bestCell);
        }
    }
}
