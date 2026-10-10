// Laboratório de balanceamento: roda lotes de partidas IA x IA e imprime os números
// que decidem se o triângulo econômico (torre / envio / renda) tem tensão de verdade.
// Uso: dotnet run -- match [quantidade]
using System;
using TDFende;

static class BalanceLab
{
    public static void Run(int matches)
    {
        Console.WriteLine($"Laboratório de balanceamento — {matches} partidas por confronto");
        Console.WriteLine();

        Duel("Normal x Normal", TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, matches);
        Duel("Difícil x Difícil", TowerWarsAi.Personality.Hard, TowerWarsAi.Personality.Hard, matches);
        Duel("Difícil x Normal", TowerWarsAi.Personality.Hard, TowerWarsAi.Personality.Normal, matches);
        Duel("Normal x Fácil", TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Easy, matches);
        Duel("Difícil x Fácil", TowerWarsAi.Personality.Hard, TowerWarsAi.Personality.Easy, matches);
        SendMix("Normal", TowerWarsAi.Personality.Normal, matches);
        SendMix("Fácil", TowerWarsAi.Personality.Easy, matches);
    }

    /// <summary>
    /// Distribuição de compras de uma personalidade. Um roster saudável tem várias
    /// linhas com uso relevante; se um tipo passa de ~60%, existe uma resposta certa
    /// e as outras cinco são enfeite.
    /// </summary>
    static void SendMix(string who, TowerWarsAi.Personality p, int matches)
    {
        var totals = new long[SendCatalog.Count];
        for (int s = 0; s < matches; s++)
        {
            var m = new MatchSim(p, p, 31000 + s);
            m.Run();
            for (int i = 0; i < SendCatalog.Count; i++)
                totals[i] += m.A.SendsByType[i] + m.B.SendsByType[i];
        }

        long all = 0;
        foreach (var t in totals) all += t;
        Console.WriteLine($"### Compras — {who}  (total {all})");
        for (int i = 0; i < SendCatalog.Count; i++)
        {
            double pct = all == 0 ? 0 : 100.0 * totals[i] / all;
            Console.WriteLine($"  {SendCatalog.Get(i).Name,-10} {pct,5:0.0}%  {new string('#', (int)(pct / 2))}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Varre atrito x escalada e mede o efeito de cada combinação. Substitui "achismo
    /// de número" por medição: as metas são partida terminando por morte (>= 75%),
    /// atrito relevante (>= 12% das mortes) e duração de 6 a 12 minutos.
    /// </summary>
    public static void Sweep(int matchesPerCombo, bool fine = false)
    {
        // Grade grossa encontra a região; a fina resolve dentro dela.
        float[] attritions = fine
            ? new[] { 0.13f, 0.16f, 0.19f, 0.22f }
            : new[] { 0.06f, 0.10f, 0.14f, 0.18f, 0.24f, 0.30f };
        float[] scales = fine
            ? new[] { 1.25f, 1.50f, 1.80f, 2.20f, 2.70f }
            : new[] { 0.55f, 0.90f, 1.30f, 1.80f, 2.40f };

        float keepAttr = TowerWarsConfig.AttritionPctPerSecond;
        float keepScale = TowerWarsConfig.SendScalePerMinute;

        Console.WriteLine($"Varredura — {matchesPerCombo} partidas por combinação (Normal x Normal)");
        Console.WriteLine("  metas: morte >= 75% | atrito >= 12% | duração 6-12 min");
        Console.WriteLine();
        Console.WriteLine("  atrito%/s  escala/min   morte%   atrito%   min    nota");

        float bestScore = float.NegativeInfinity;
        float bestA = keepAttr, bestS = keepScale;

        foreach (var atr in attritions)
        foreach (var sc in scales)
        {
            TowerWarsConfig.AttritionPctPerSecond = atr;
            TowerWarsConfig.SendScalePerMinute = sc;

            int deaths = 0;
            double share = 0, mins = 0;
            for (int s = 0; s < matchesPerCombo; s++)
            {
                var m = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 20000 + s);
                var r = m.Run();
                if (m.A.Dead || m.B.Dead) deaths++;
                share += r.AttritionShare;
                mins += r.Seconds / 60.0;
            }

            double deathPct = 100.0 * deaths / matchesPerCombo;
            double attrPct = 100.0 * share / matchesPerCombo;
            double avgMin = mins / matchesPerCombo;

            // Nota multiobjetivo. A duração pesa DOBRADO porque foi o que a grade grossa
            // mostrou ser fácil de estragar: escala alta zera o timeout mas entrega
            // partida de 3,5 min, que não é o ritmo do gênero.
            double score = Math.Min(deathPct, 95.0) / 95.0
                         + Math.Min(attrPct, 30.0) / 30.0
                         - 2.0 * Math.Abs(avgMin - 9.0) / 9.0;

            if (score > bestScore)
            {
                bestScore = (float)score;
                bestA = atr;
                bestS = sc;
            }

            Console.WriteLine($"  {atr,8:0.00}  {sc,10:0.00}   {deathPct,6:0}%  {attrPct,7:0.0}%  {avgMin,5:0.0}   {score:0.00}");
        }

        Console.WriteLine();
        Console.WriteLine($">>> melhor combinação: atrito {bestA:0.00}/s, escala {bestS:0.00}/min (nota {bestScore:0.00})");

        TowerWarsConfig.AttritionPctPerSecond = keepAttr;
        TowerWarsConfig.SendScalePerMinute = keepScale;
    }

    static void Duel(string label, TowerWarsAi.Personality pa, TowerWarsAi.Personality pb, int matches)
    {
        int winA = 0, winB = 0, draw = 0, timeouts = 0;
        double turns = 0, tension = 0, secs = 0, attritionShare = 0, incomeA = 0, incomeB = 0, towersA = 0, towersB = 0;

        for (int s = 0; s < matches; s++)
        {
            var r = new MatchSim(pa, pb, 9000 + s).Run();
            if (r.Winner == 1) winA++;
            else if (r.Winner == 2) winB++;
            else draw++;
            if (r.Seconds >= TowerWarsConfig.MatchTimeLimit - 0.05f) timeouts++;

            turns += r.Turnarounds;
            tension += r.Tension;
            secs += r.Seconds;
            attritionShare += r.AttritionShare;
            incomeA += r.IncomeA;
            incomeB += r.IncomeB;
            towersA += r.TowersA;
            towersB += r.TowersB;
        }

        double n = matches;
        Console.WriteLine($"### {label}");
        Console.WriteLine($"  vitórias        A {winA}  |  B {winB}  |  empate {draw}   ({100.0 * winA / n:0.0}% para A)");
        Console.WriteLine($"  duração média   {secs / n / 60.0:0.00} min      estouraram o tempo: {timeouts}/{matches}");
        Console.WriteLine($"  renda final     A {incomeA / n:0.0}  |  B {incomeB / n:0.0}        (base {TowerWarsConfig.BaseIncome})");
        Console.WriteLine($"  torres          A {towersA / n:0.0}  |  B {towersB / n:0.0}");
        Console.WriteLine($"  mortes por atrito: {100.0 * attritionShare / n:0.0}% do total");
        Console.WriteLine($"  viradas/partida {turns / n:0.0}   tensão {100.0 * tension / n:0}% das amostras (DES-03s)");
        Console.WriteLine();
    }
}
