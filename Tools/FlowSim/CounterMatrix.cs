// Matriz de contras (TEC-14): mede cada par torre x bicho com o MESMO ouro e diz quem responde a quem.
// Uso: dotnet run --project Tools/FlowSim -v quiet -- matriz [ouro] [contras.txt]
// É relatório: os contratos (papéis) são conferidos e impressos, e só viram portão quando o catálogo estabilizar.
using System;
using System.Collections.Generic;
using System.Text;
using TDFende;
using UnityEngine;
using Random = System.Random;

static class CounterMatrix
{
    /// <summary>Células para as torres, em volta do corredor do meio, perto da entrada primeiro.</summary>
    static readonly Vector2Int[] Layout =
    {
        new Vector2Int(8, 7), new Vector2Int(8, 9), new Vector2Int(10, 7), new Vector2Int(10, 9),
        new Vector2Int(12, 7), new Vector2Int(12, 9), new Vector2Int(14, 7), new Vector2Int(14, 9),
        new Vector2Int(9, 6), new Vector2Int(9, 10), new Vector2Int(11, 6), new Vector2Int(11, 10),
        new Vector2Int(13, 6), new Vector2Int(13, 10), new Vector2Int(15, 7), new Vector2Int(15, 9),
    };

    public const int WaveSends = 18;      // compras do mesmo bicho por rodada
    public const float SendEvery = 1.2f;  // segundos entre compras
    public const float Window = 100f;     // tempo simulado por par

    /// <summary>
    /// Nota do par: fração dos bichos enviados que NÃO chegaram à base (1 = defesa perfeita, 0 = passou tudo).
    /// Mesmo ouro, mesma entrada de ondas, sem upgrade: só o tipo da torre varia.
    /// </summary>
    public static double Score(int towerId, int enemyId, int gold, int seed = 17)
    {
        var lane = new LaneSim(24, 16);
        lane.DebugGrantGold(gold - lane.Gold);
        int built = 0;
        foreach (var c in Layout)
        {
            if (!lane.TryBuildTower(c, towerId)) continue;
            built++;
        }
        var feeder = new LaneSim(24, 16);
        feeder.DebugGrantGold(1000000);
        var rng = new Random(seed);
        float t = 0f, nextSend = 0f;
        int sent = 0, dt = 0;
        float step = TowerWarsConfig.FixedStep;
        int steps = (int)Math.Round(Window / step);
        for (int i = 0; i < steps; i++, t += step)
        {
            if (sent < WaveSends && t >= nextSend)
            {
                int before = feeder.TotalSent;
                feeder.TrySend(enemyId, lane, rng);
                sent += 1;
                nextSend += SendEvery;
                dt += feeder.TotalSent - before;
            }
            lane.Tick(step);
            feeder.Tick(step);
        }
        if (dt <= 0) return 0.0;
        return 1.0 - (double)lane.TotalLeaked / dt;
    }

    public static double[,] Measure(int gold)
    {
        int T = TowerCatalog.Count, E = SendCatalog.Count;
        var m = new double[T, E];
        for (int e = 0; e < E; e++)
        for (int tw = 0; tw < T; tw++)
            m[tw, e] = Score(tw, e, gold);
        return m;
    }

    /// <summary>Torre com a melhor nota contra o envio (empate: a de menor índice).</summary>
    public static int BestTower(double[,] m, int enemy)
    {
        int best = 0;
        for (int tw = 1; tw < m.GetLength(0); tw++)
            if (m[tw, enemy] > m[best, enemy] + 1e-9) best = tw;
        return best;
    }

    public static string Report(double[,] m, int gold)
    {
        int T = m.GetLength(0), E = m.GetLength(1);
        var sb = new StringBuilder();
        sb.AppendLine($"Matriz de contras — {gold} de ouro em torres (sem upgrade), {WaveSends} compras de cada bicho, {Window:0} s");
        sb.AppendLine("nota = fração dos bichos que NÃO chegaram à base (maior = a torre responde melhor)");
        sb.AppendLine();
        sb.Append(string.Format("{0,-12}", ""));
        for (int tw = 0; tw < T; tw++) sb.Append(string.Format("{0,10}", TowerCatalog.Get(tw).Name));
        sb.AppendLine("   melhor");
        for (int e = 0; e < E; e++)
        {
            sb.Append(string.Format("{0,-12}", SendCatalog.Get(e).Name));
            int best = BestTower(m, e);
            for (int tw = 0; tw < T; tw++) sb.Append(string.Format("{0,9:0.00}{1}", m[tw, e], tw == best ? "*" : " "));
            sb.AppendLine("   " + TowerCatalog.Get(best).Name);
        }
        return sb.ToString();
    }

    /// <summary>Resultado de um contrato: nome, passou, detalhe.</summary>
    public static List<(string Name, bool Ok, string Detail)> Contracts(double[,] m)
    {
        int T = m.GetLength(0), E = m.GetLength(1);
        var list = new List<(string, bool, string)>();
        var wins = new int[T];
        for (int e = 0; e < E; e++) wins[BestTower(m, e)]++;

        for (int tw = 0; tw < T; tw++)
            list.Add(($"{TowerCatalog.Get(tw).Name} é a melhor contra pelo menos 1 envio", wins[tw] >= 1, $"{wins[tw]} envio(s)"));
        for (int tw = 0; tw < T; tw++)
            list.Add(($"{TowerCatalog.Get(tw).Name} é a melhor contra no máximo 40% dos envios", wins[tw] <= Math.Floor(E * 0.4),
                $"{wins[tw]} de {E}"));
        for (int e = 0; e < E; e++)
        {
            int best = BestTower(m, e);
            double canhao = m[0, e], melhor = m[best, e];
            double gain = canhao <= 1e-9 ? (melhor > 1e-9 ? double.PositiveInfinity : 0.0) : melhor / canhao - 1.0;
            list.Add(($"{SendCatalog.Get(e).Name} tem uma resposta 15% a 30% melhor que o Canhão", gain >= 0.15 && gain <= 0.30,
                $"{TowerCatalog.Get(best).Name} +{gain * 100:0}%"));
        }
        return list;
    }

    /// <summary>contras.txt para a loja: o "bom contra" MEDIDO de cada torre, em chaves (não em nomes exibidos).</summary>
    public static string ToContrasTxt(double[,] m)
    {
        int T = m.GetLength(0), E = m.GetLength(1);
        var sb = new StringBuilder();
        sb.Append("# TDFende — contras medidos (FlowSim matriz). Uma linha por torre: as chaves dos envios que ela responde melhor.\n");
        sb.Append("# Gerado; não edite à mão.\n");
        for (int tw = 0; tw < T; tw++)
        {
            var good = new List<string>();
            for (int e = 0; e < E; e++)
                if (BestTower(m, e) == tw) good.Add(SendCatalog.Get(e).Key);
            sb.Append($"tower={TowerCatalog.Get(tw).Key};goodvs={string.Join(",", good)}\n");
        }
        return sb.ToString();
    }

    public static int Run(int gold, string outPath)
    {
        var m = Measure(gold);
        Console.Write(Report(m, gold));
        Console.WriteLine();
        Console.WriteLine("Contratos (relatório; só viram portão quando o catálogo estabilizar):");
        int fails = 0;
        foreach (var (name, ok, detail) in Contracts(m))
        {
            Console.WriteLine($"  {(ok ? "OK    " : "FALHA ")}{name}  [{detail}]");
            if (!ok) fails++;
        }
        Console.WriteLine();
        Console.WriteLine($"{fails} contrato(s) fora do alvo.");
        if (outPath != null)
        {
            System.IO.File.WriteAllText(outPath, ToContrasTxt(m), new UTF8Encoding(true));
            Console.WriteLine($"escrito: {System.IO.Path.GetFullPath(outPath)}");
        }
        return 0;
    }
}
