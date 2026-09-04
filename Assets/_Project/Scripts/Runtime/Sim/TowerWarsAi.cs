using System;
using Random = System.Random; // dentro do Unity, "Random" puro colide com UnityEngine.Random (CS0104)
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
            /// <summary>
            /// Quanto ela percebe contra-jogo: 0 = cega, 1 = leitura cheia. Era um bool,
            /// mas com quatro tipos de torre "cega" virou catastrófico — o Fácil construía
            /// pelo dano por ouro, nunca fazia Sentinela e perdia 97% para o Normal.
            /// Difficuldade agora é um botão contínuo, não um interruptor.
            /// </summary>
            public float CounterStrength;
            public int PlacementSamples;   // quantas células avalia por torre (esperteza do maze)

            // Fácil erra de um jeito específico: lê contra-jogo pela metade e reage
            // devagar. Não é uma versão "capada" em tudo — quando era, perdia 100% das
            // partidas, e adversário que nunca ganha não ensina o jogo a ninguém.
            public static Personality Easy => new Personality
            {
                Name = "Fácil", DecisionInterval = 1.35f, SafetyMargin = 1.5f,
                GreedBias = 0.85f, CounterStrength = 0.6f, PlacementSamples = 14
            };

            public static Personality Normal => new Personality
            {
                Name = "Normal", DecisionInterval = 1.2f, SafetyMargin = 1.4f,
                GreedBias = 1.0f, CounterStrength = 1.0f, PlacementSamples = 18
            };

            public static Personality Hard => new Personality
            {
                Name = "Difícil", DecisionInterval = 0.6f, SafetyMargin = 1.0f,
                GreedBias = 1.35f, CounterStrength = 1.3f, PlacementSamples = 40
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
            if (!wantsTower || _me.Gold >= TowerCatalog.Get(0).Cost * 3)
            {
                int pick = ChooseSend();
                if (pick >= 0 && _me.TrySend(pick, _foe, _rng)) return;
            }

            // Sobrou ouro e nada urgente: engrossa a defesa.
            if (_me.Gold >= TowerCatalog.Get(0).Cost * 4)
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

            // Comparar DANO POR OURO dos dois caminhos, não preço bruto. Com preço bruto,
            // só o upgrade de nível 1 do Canhão (20) passava por ser o único abaixo do
            // custo de um Canhão novo (25) — Morteiro, Gelo e Sentinela nunca subiam de
            // nível na partida inteira, medido em 995 torres construídas.
            bool canUpgrade = _me.TryGetBestUpgrade(out int upCost, out float upDps);
            float upgradeValue = canUpgrade ? upDps / upCost : -1f;

            int buildType = ChooseTowerType();
            float buildValue = -1f;
            if (buildType >= 0)
            {
                var bt = TowerCatalog.Get(buildType);
                buildValue = bt.Dps / bt.Cost;
            }

            if (canUpgrade && upgradeValue >= buildValue)
                return TryUpgrade() || BuildSomewhere();

            return BuildSomewhere() || TryUpgrade();
        }

        bool TryUpgrade() => _me.TryUpgradeBest();

        readonly float[] _towerScores = new float[TowerCatalog.Count];

        /// <summary>
        /// Escolhe o TIPO de torre pela ameaça que está na tela agora: enxame pede área,
        /// voador pede Sentinela, gordo-e-rápido pede Gelo. Sem isto a IA construiria só
        /// Canhão e as outras três seriam decoração no catálogo.
        ///
        /// CounterStrength gradua o quanto ela lê a ameaça: é o que separa o Fácil do
        /// Difícil sem transformar o Fácil em adversário inútil.
        /// </summary>
        int ChooseTowerType()
        {
            float swarmHp = 0f, flyerHp = 0f, fastHp = 0f, totalHp = 0.001f;
            for (int s = 0; s < _me.EnemySlotCount; s++)
            {
                if (!_me.TryGetEnemy(s, out var e)) continue;
                var u = SendCatalog.Get(e.TypeId);
                totalHp += e.Hp;
                if (u.Count > 1) swarmHp += e.Hp;
                if (u.IgnoresTerritory) flyerHp += e.Hp;
                if (u.Speed >= 3.5f) fastHp += e.Hp;
            }

            for (int id = 0; id < TowerCatalog.Count; id++)
            {
                var t = TowerCatalog.Get(id);
                if (_me.Gold < t.Cost) { _towerScores[id] = 0f; continue; }

                // base: dano por ouro, com o território contando como valor
                float score = t.Dps / t.Cost + t.BorderRadius * 0.02f;

                float cs = _p.CounterStrength;
                if (t.SplashRadius > 0f) score *= 1f + 1.6f * cs * (swarmHp / totalHp);
                if (t.VsFlyingMultiplier > 1f) score *= 1f + 2.2f * cs * (flyerHp / totalHp);
                if (t.SlowFactor < 1f) score *= 1f + 1.4f * cs * (fastHp / totalHp);

                _towerScores[id] = score * score; // acentua o favorito sem zerar o resto
            }

            float total = 0f;
            foreach (var s in _towerScores) total += s;
            if (total <= 0f) return -1;

            double roll = _rng.NextDouble() * total;
            int last = -1;
            for (int id = 0; id < TowerCatalog.Count; id++)
            {
                if (_towerScores[id] <= 0f) continue;
                last = id;
                roll -= _towerScores[id];
                if (roll <= 0.0) return id;
            }
            return last;
        }

        readonly float[] _sendScores = new float[SendCatalog.Count];

        /// <summary>
        /// Escolhe o envio por SORTEIO PONDERADO, não por argmax. Argmax sobre pontuação
        /// quase estática degenerava em compra única (medido: Planador 68% no Normal,
        /// Couraçado 94% no Fácil, três tipos com 0%) — com sorteio o melhor continua
        /// favorito, mas o roster inteiro participa. O rng é o da partida, então a
        /// escolha continua determinística por semente.
        /// </summary>
        int ChooseSend()
        {
            bool foeHasTerritory = _foe.TowerCount >= 4;
            float foeShot = _foe.AvgShotDamage;
            float total = 0f;

            for (int id = 0; id < SendCatalog.Count; id++)
            {
                _sendScores[id] = 0f;
                if (!_me.CanAfford(id)) continue;
                var u = SendCatalog.Get(id);

                // Base: renda comprada por ouro gasto.
                float score = (u.IncomeBonus / (float)u.Cost) * _p.GreedBias;

                // Pressão: vida entregue por ouro, valendo mais quando anda rápido
                // (menos tempo exposto a torre e a atrito).
                float speedFactor = u.Speed / 2.2f;
                score += 0.35f * (u.Hp * u.Count * speedFactor) / (u.Cost * 100f);

                // Enxame explora overkill: torre de tiro forte desperdiça o excedente
                // num corpo fraco, e o tempo de voo já perdido não volta.
                // A HP comparada é a ESCALADA (a que vai nascer de fato na lane do
                // adversário) — contra a HP de catálogo, o overkill computado só crescia
                // com o relógio enquanto o real caía, e a IA comprava Enxame justamente
                // quando ele era o pior envio.
                if (u.Count > 1 && foeShot > 0f)
                {
                    float bodyHp = u.Hp * _foe.SendScale;
                    float waste = Math.Min(foeShot / bodyHp, 3f);
                    score *= 1f + 0.25f * waste;
                }

                // Voador contra quem investiu em território é o contra-jogo do modo.
                if (u.IgnoresTerritory && foeHasTerritory)
                    score *= 1f + 0.5f * _p.CounterStrength;
                // Enxame frágil derrete no atrito: evita quando o outro tem fronteira.
                if (u.AttritionScale > 1.2f && foeHasTerritory)
                    score *= 1f - 0.4f * _p.CounterStrength;

                if (score <= 0f) continue;
                // Quadrado antes do sorteio: acentua o favorito sem matar a variedade.
                _sendScores[id] = score * score;
                total += _sendScores[id];
            }

            if (total <= 0f) return -1;

            double roll = _rng.NextDouble() * total;
            int last = -1; // fallback: arredondamento float pode deixar o roll "vazar" pelo fim
            for (int id = 0; id < SendCatalog.Count; id++)
            {
                if (_sendScores[id] <= 0f) continue;
                last = id;
                roll -= _sendScores[id];
                if (roll <= 0.0) return id;
            }
            return last;
        }

        /// <summary>
        /// Coloca torre por amostragem: sorteia células válidas e fica com a de melhor
        /// nota. Mais amostras = maze mais esperto, e é assim que a dificuldade escala
        /// sem precisar de uma IA diferente por nível.
        /// </summary>
        bool BuildSomewhere()
        {
            int typeId = ChooseTowerType();
            if (typeId < 0) return false;

            int w = _me.Map.Width, h = _me.Map.Height;
            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int s = 0; s < _p.PlacementSamples; s++)
            {
                var cell = new Vector2Int(_rng.Next(1, w - 1), _rng.Next(0, h));
                if (!_me.CanBuild(cell, typeId)) continue;

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

            return found && _me.TryBuildTower(bestCell, typeId);
        }
    }
}
