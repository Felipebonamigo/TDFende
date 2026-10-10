using System;
using System.Collections.Generic;
using System.Globalization;

namespace TDFende
{
    /// <summary>
    /// Modos do laboratório da tese (DES-03s). O A/B do Portão 1 pergunta se cercar com fronteira tem graça, e para
    /// responder precisa ligar e desligar o atrito, dobrar a fronteira, dar ouro infinito ao jogador ou dobrar a renda,
    /// sempre voltando ao padrão ao sair.
    /// </summary>
    public enum LabMode { Normal, SemAtrito, FronteiraDobrada, OuroInfinito, Renda2x }

    /// <summary>
    /// As chaves em si. Mexem só em números de <see cref="TowerWarsConfig"/> (que a assinatura do replay já enxerga),
    /// então uma partida gravada num modo carrega o modo na assinatura <c>rules</c>. <see cref="Reset"/> sempre volta ao padrão.
    /// </summary>
    public static class LabSwitches
    {
        public static LabMode Current { get; private set; } = LabMode.Normal;

        static float _savedAttrition;

        public static void Apply(LabMode mode)
        {
            Reset();
            if (mode == LabMode.Normal) return;
            _savedAttrition = TowerWarsConfig.AttritionPctPerSecond;
            Current = mode;
            switch (mode)
            {
                case LabMode.SemAtrito: TowerWarsConfig.AttritionPctPerSecond = 0f; break;
                case LabMode.FronteiraDobrada: TowerWarsConfig.BorderScale = 2f; break;
                case LabMode.OuroInfinito: TowerWarsConfig.InfiniteGold = true; break;
                case LabMode.Renda2x: TowerWarsConfig.IncomeMultiplier = 2f; break;
            }
        }

        public static void Reset()
        {
            if (Current == LabMode.SemAtrito) TowerWarsConfig.AttritionPctPerSecond = _savedAttrition;
            TowerWarsConfig.BorderScale = 1f;
            TowerWarsConfig.IncomeMultiplier = 1f;
            TowerWarsConfig.InfiniteGold = false;
            Current = LabMode.Normal;
        }
    }

    /// <summary>
    /// Métricas de uma partida, calculadas sobre amostras a cada poucos segundos (o laboratório amostra de 5 em 5 s):
    /// <b>viradas</b> e <b>tensão</b>. Definições escritas antes da medição, para não ajustar a régua ao resultado.
    /// <list type="bullet">
    /// <item><b>Líder</b> de uma amostra: quem tem mais vidas; empate de vidas desempata por renda; empate total mantém o líder anterior.</item>
    /// <item><b>Virada</b>: o líder trocou de lado (A para B ou B para A). Amostras sem líder não contam.</item>
    /// <item><b>Tensão</b> (0 a 1): fração das amostras em que o jogo está aberto (diferença de vidas &lt;= 4) E alguém está em perigo (&lt;= 12 vidas).</item>
    /// </list>
    /// </summary>
    public static class MatchMetrics
    {
        public const int OpenGap = 4, DangerLives = 12;

        public readonly struct Sample
        {
            public readonly int LivesA, LivesB, IncomeA, IncomeB;
            public Sample(int livesA, int livesB, int incomeA, int incomeB)
            { LivesA = livesA; LivesB = livesB; IncomeA = incomeA; IncomeB = incomeB; }
        }

        public readonly struct Result
        {
            public readonly int Turnarounds, Samples;
            public readonly double Tension;
            public Result(int turnarounds, double tension, int samples) { Turnarounds = turnarounds; Tension = tension; Samples = samples; }
        }

        public static Result Compute(IReadOnlyList<Sample> samples)
        {
            int leader = 0, turns = 0, tense = 0; // leader: 0 nenhum, 1 = A, 2 = B
            foreach (var s in samples)
            {
                int now = s.LivesA != s.LivesB ? (s.LivesA > s.LivesB ? 1 : 2)
                        : s.IncomeA != s.IncomeB ? (s.IncomeA > s.IncomeB ? 1 : 2) : leader;
                if (leader != 0 && now != 0 && now != leader) turns++;
                if (now != 0) leader = now;

                if (Math.Abs(s.LivesA - s.LivesB) <= OpenGap && Math.Min(s.LivesA, s.LivesB) <= DangerLives) tense++;
            }
            return new Result(turns, samples.Count == 0 ? 0.0 : (double)tense / samples.Count, samples.Count);
        }
    }

    /// <summary>
    /// Sorteio CEGO do modo (DES-03s): o jogo sorteia um modo do conjunto, aplica e o esconde. O modo só é revelado
    /// depois da nota, e a linha do diário sai na revelação. Determinístico pela semente (a mesma semente, o mesmo modo).
    /// </summary>
    public sealed class BlindSession
    {
        public static BlindSession Current { get; private set; }

        public int Seed { get; }
        /// <summary>O modo sorteado. Só o laboratório e os testes devem ler isto antes da revelação.</summary>
        public LabMode ModeForLabOnly { get; }

        int _score;
        string _extra = "-;-";

        BlindSession(int seed, LabMode mode) { Seed = seed; ModeForLabOnly = mode; }

        public static BlindSession Start(int seed, LabMode[] pool)
        {
            if (pool == null || pool.Length == 0) throw new ArgumentException("conjunto de modos vazio", nameof(pool));
            var mode = pool[new Random(seed).Next(pool.Length)];
            LabSwitches.Apply(mode);
            Current = new BlindSession(seed, mode);
            return Current;
        }

        /// <summary>Guarda as métricas da partida que vão para o diário junto da nota.</summary>
        public void Attach(int turnarounds, double tension) =>
            _extra = turnarounds.ToString(CultureInfo.InvariantCulture) + ";" + tension.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>Nota de 1 a 10, uma vez só. Devolve false se fora da faixa ou se já foi dada.</summary>
        public bool Rate(int score)
        {
            if (_score != 0 || score < 1 || score > 10) return false;
            _score = score;
            return true;
        }

        /// <summary>
        /// Revela o modo: devolve a linha do diário (<c>semente;modo;nota;viradas;tensão</c>) e encerra a sessão, devolvendo
        /// as chaves ao padrão. Antes da nota devolve null e NÃO revela.
        /// </summary>
        public string Reveal()
        {
            if (_score == 0) return null;
            string line = string.Join(";", Seed.ToString(CultureInfo.InvariantCulture), ModeForLabOnly, _score.ToString(CultureInfo.InvariantCulture), _extra);
            Abort();
            return line;
        }

        /// <summary>Encerra sem revelar (partida abandonada).</summary>
        public void Abort()
        {
            if (Current == this) Current = null;
            LabSwitches.Reset();
        }
    }
}
