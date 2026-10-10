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
                // CounterStrength 0,6 -> 0,5 com Fogo e Ar: com seis torres, ler a ameaça pela
                // metade já chegava perto do Normal (medido: Normal só 57% contra o Fácil).
                // Com os bichos (nove envios) e o repasse, amostras 14 -> 10: Normal 47% -> 77%
                Name = "Fácil", DecisionInterval = 1.45f, SafetyMargin = 1.5f,
                GreedBias = 0.85f, CounterStrength = 0.5f, PlacementSamples = 10
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
                return TryUpgrade() || BuildSomewhere(buildType);

            return BuildSomewhere(buildType) || TryUpgrade();
        }

        bool TryUpgrade() => _me.TryUpgradeBest();

        readonly float[] _towerScores = new float[TowerCatalog.Count];
        readonly int[] _owned = new int[TowerCatalog.Count];

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
            float swarmHp = 0f, flyerHp = 0f, fastHp = 0f, heavyHp = 0f, totalHp = 0.001f;
            for (int s = 0; s < _me.EnemySlotCount; s++)
            {
                if (!_me.TryGetEnemy(s, out var e)) continue;
                var u = SendCatalog.Get(e.TypeId);
                totalHp += e.Hp;
                if (u.Count > 1) swarmHp += e.Hp;
                if (u.IgnoresTerritory) flyerHp += e.Hp;
                if (u.Speed >= 3.5f) fastHp += e.Hp;
                if (u.Hp >= 150f) heavyHp += e.Hp;
            }

            // quantas de cada tipo já tenho: defesa de um tipo só tem um furo só
            for (int id = 0; id < _owned.Length; id++) _owned[id] = 0;
            for (int i = 0; i < _me.TowerCount; i++) _owned[_me.TowerTypeId(i)]++;

            for (int id = 0; id < TowerCatalog.Count; id++)
            {
                var t = TowerCatalog.Get(id);
                if (_me.Gold < t.Cost) { _towerScores[id] = 0f; continue; }

                // base: dano por ouro, com o território contando como valor
                // o fogo rende em função da vida do alvo: conta uma queima típica (120 de vida)
                float burnDps = t.BurnPctPerSecond * t.BurnSeconds * 120f / t.Cooldown;
                float score = (t.Dps + burnDps) / t.Cost + t.BorderRadius * 0.02f;
                // Gelo e Ar não se pagam em dano: rendem segurando o inimigo no alcance das
                // outras torres e dentro da fronteira. Sem contar isso, a nota deles era um
                // quarto da do Canhão e a IA quase nunca os construía (2% das torres).
                score += (1f - t.SlowFactor) * 0.5f + t.Knockback * 0.4f;

                float cs = _p.CounterStrength;
                if (t.SplashRadius > 0f) score *= 1f + 1.6f * cs * (swarmHp / totalHp);
                if (t.VsFlyingMultiplier > 1f) score *= 1f + 2.2f * cs * (flyerHp / totalHp);
                if (t.SlowFactor < 1f) score *= 1f + 1.4f * cs * (fastHp / totalHp);
                if (t.BurnPctPerSecond > 0f) score *= 1f + 1.8f * cs * (heavyHp / totalHp);
                if (t.Knockback > 0f) score *= 1f + 1.2f * cs * (fastHp / totalHp);

                // Variedade: cada torre repetida do tipo pesa. Sem isto, medido em 40 partidas,
                // Canhão e Fogo eram 78% das torres e Gelo, 2% — a IA parecia não conhecer o resto.
                score /= 1f + 0.6f * _owned[id];

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

                // Base: renda comprada por ouro gasto. Vale menos com o relógio (DES-05): renda comprada cedo
                // paga o resto da partida, renda comprada tarde quase não paga. Sem isso, com o espectro
                // investimento x pressão, o Cachorro (o melhor investimento) virava metade das compras.
                float horizon = Math.Max(IncomeHorizonFloor, 1f - _me.MatchTime / IncomeHorizonSeconds);
                float score = (u.IncomeBonus / (float)u.Cost) * _p.GreedBias * horizon;

                // Pressão: vida entregue por ouro, valendo mais quando anda rápido
                // (menos tempo exposto a torre e a atrito).
                float speedFactor = u.Speed / 2.2f;
                score += PressureWeight * (u.Hp * u.Count * speedFactor) / (u.Cost * 100f);

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

        // DES-05: o quanto a renda comprada vale ao longo da partida e o quanto a pressão pesa na escolha do envio.
        const float IncomeHorizonSeconds = 480f, IncomeHorizonFloor = 0.15f, PressureWeight = 0.5f;

        readonly System.Collections.Generic.List<Vector2Int> _path = new System.Collections.Generic.List<Vector2Int>();

        /// <summary>
        /// Coloca torre por amostragem: sorteia células válidas e fica com a de melhor
        /// nota. Mais amostras = maze mais esperto, e é assim que a dificuldade escala
        /// sem precisar de uma IA diferente por nível.
        ///
        /// A nota lê o CAMINHO de verdade: quantas células da marcha ficam no alcance e
        /// quanto a torre alonga a marcha. A versão anterior só olhava a linha do meio e
        /// "a metade da frente" — medido em 40 partidas, empilhava as torres coladas no
        /// próprio acampamento inimigo (e até atrás dele), cobrindo meia dúzia de passos.
        /// </summary>
        bool BuildSomewhere(int typeId = -1)
        {
            if (typeId < 0) typeId = ChooseTowerType();
            if (typeId < 0 || _me.SpawnCells.Count == 0) return false;

            int w = _me.Map.Width, h = _me.Map.Height;
            var spawn = _me.SpawnCells[0];
            var goal = _me.GoalCell;
            _me.Flow.Path(spawn, _path);
            if (_path.Count == 0) return false;
            int costNow = _me.Flow.CostAt(spawn);
            float range = TowerCatalog.Get(typeId).Range;
            float range2 = range * range;

            Vector2Int bestCell = default;
            float bestScore = float.NegativeInfinity;
            bool found = false;

            for (int s = 0; s < _p.PlacementSamples; s++)
            {
                // dois terços das amostras ao lado da marcha, o resto em qualquer lugar
                Vector2Int cell;
                if (s % 3 != 2)
                {
                    var along = _path[_rng.Next(_path.Count)];
                    int reach = Math.Max(1, (int)range);
                    cell = new Vector2Int(along.x + _rng.Next(-reach, reach + 1), along.y + _rng.Next(-reach, reach + 1));
                }
                else cell = new Vector2Int(_rng.Next(1, w - 1), _rng.Next(0, h));

                // nada colado no acampamento nem na porta da base: ali a torre cobre pouco
                // e fica com cara de engano
                if (Math.Max(Math.Abs(cell.x - spawn.x), Math.Abs(cell.y - spawn.y)) < 3) continue;
                if (Math.Max(Math.Abs(cell.x - goal.x), Math.Abs(cell.y - goal.y)) < 2) continue;
                if (!_me.CanBuild(cell, typeId)) continue;

                // cobertura: passos da marcha dentro do alcance, os do começo valendo um
                // pouco mais (quem é pego cedo apanha de mais torres no caminho)
                float cover = 0f;
                for (int i = 0; i < _path.Count; i++)
                {
                    float dx = _path[i].x - cell.x, dy = _path[i].y - cell.y;
                    if (dx * dx + dy * dy <= range2) cover += 1f - 0.4f * i / _path.Count;
                }
                float score = cover * 1.0f;

                // maze: quanto a marcha fica mais longa com a torre aqui (em passos)
                int costAfter = _me.Flow.CostIfBlocked(cell, spawn);
                if (costAfter != FlowField.Unreachable && costNow != FlowField.Unreachable)
                    score += (costAfter - costNow) / 10f * 0.8f;

                // encostada em torre existente adensa o território, sem virar o critério principal
                int neighbours = 0;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cell.x + dx, ny = cell.y + dy;
                    if (_me.Map.InBounds(nx, ny) && _me.Map.IsBlocked(nx, ny)) neighbours++;
                }
                score += Math.Min(neighbours, 3) * 0.5f;

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
