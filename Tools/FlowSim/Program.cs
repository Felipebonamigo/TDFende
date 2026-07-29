// Testes headless da lógica pura do FrontierTD (mesmos .cs do projeto Unity).
using System;
using System.Collections.Generic;
using FrontierTD;
using UnityEngine;

class Program
{
    static int _failed;

    static void Check(bool cond, string name)
    {
        Console.WriteLine($"{(cond ? "OK  " : "FALHOU")}  {name}");
        if (!cond) _failed++;
    }

    static int Main()
    {
        const int W = 24, H = 16;

        // ---------- GridMap: conversões ----------
        var map = new GridMap(W, H, 1f);
        bool roundtrip = true;
        for (int y = 0; y < H && roundtrip; y++)
        for (int x = 0; x < W && roundtrip; x++)
        {
            var c = map.WorldToCell(map.CellToWorld(x, y));
            roundtrip = c.x == x && c.y == y;
        }
        Check(roundtrip, "GridMap: WorldToCell(CellToWorld) devolve a mesma célula (todas)");
        Check(map.WorldToCell(new Vector3(0.1f, 0f, 0.1f)).x == W / 2, "GridMap: origem centralizada");

        // ---------- FlowField: grid vazio ----------
        var goal = new Vector2Int(W - 3, H / 2);
        var spawn = new Vector2Int(2, H / 2);
        var spawns = new List<Vector2Int> { spawn };
        var flow = new FlowField(map);
        flow.Rebuild(goal);

        bool allReachable = true;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            if (!flow.IsReachable(new Vector2Int(x, y))) allReachable = false;
        Check(allReachable, "FlowField: grid vazio, todas as células alcançam a base");

        Check(Walk(map, flow, spawn, goal, out int steps1), $"FlowField: caminhada do spawn chega na base ({steps1} passos)");

        // ---------- FlowField: parede com um vão ----------
        // parede vertical em x=12, vão só em y=1
        for (int y = 0; y < H; y++)
            if (y != 1) map.SetBlocked(new Vector2Int(12, y), true);
        flow.Rebuild(goal);

        Check(flow.IsReachable(spawn), "FlowField: com vão, spawn ainda alcança a base");
        Check(Walk(map, flow, spawn, goal, out int steps2), $"FlowField: caminhada atravessa o vão ({steps2} passos)");
        Check(steps2 > steps1, "FlowField: caminho desviado é mais longo que o direto");

        // fechar o único vão tem que ser detectado como bloqueio total
        Check(flow.PlacementBlocksPath(new Vector2Int(12, 1), spawns),
            "FlowField: fechar o único vão -> PlacementBlocksPath = true");
        Check(!flow.PlacementBlocksPath(new Vector2Int(5, 5), spawns),
            "FlowField: célula inocente -> PlacementBlocksPath = false");
        // e o teste não pode ter deixado sujeira no mapa
        Check(!map.IsBlocked(12, 1), "FlowField: PlacementBlocksPath não corrompe o mapa");
        Check(Walk(map, flow, spawn, goal, out _), "FlowField: campo continua íntegro após os testes de bloqueio");

        // ---------- FlowField: não corta quina ----------
        var map2 = new GridMap(W, H, 1f);
        var flow2 = new FlowField(map2);
        var goal2 = new Vector2Int(7, 7);
        map2.SetBlocked(new Vector2Int(6, 7), true);
        map2.SetBlocked(new Vector2Int(7, 6), true);
        flow2.Rebuild(goal2);
        // (6,6) é diagonal à base, mas os dois ortogonais estão bloqueados:
        // se cortar quina fosse permitido, a direção apontaria direto pra base
        var dirAt66 = flow2.SampleDirection(map2.CellToWorld(6, 6));
        var diagonal = new Vector3(1, 0, 1).normalized;
        bool cutsCorner = (dirAt66 - diagonal).sqrMagnitude < 0.01f;
        Check(!cutsCorner, "FlowField: não corta quina entre duas torres");
        Check(flow2.IsReachable(new Vector2Int(6, 6)), "FlowField: célula na quina continua alcançável (dá a volta)");

        // ---------- TerritoryField ----------
        var terr = new TerritoryField(map2);
        var towers = new List<Vector2Int> { new Vector2Int(10, 8) };
        terr.Rebuild(towers, 2.75f);

        Check(terr.Contains(10, 8), "Territory: célula da torre está dentro");
        Check(terr.Contains(12, 8), "Territory: distância 2 está dentro (raio 2.75)");
        Check(!terr.Contains(13, 8), "Territory: distância 3 está fora");
        Check(!terr.Contains(12, 10), "Territory: raio é circular (2,2 -> dist 2.83 fora)");
        Check(terr.IsEdge(12, 8), "Territory: borda detectada onde faz divisa");
        Check(!terr.IsEdge(10, 8), "Territory: centro não é borda");
        Check(terr.Contains(map2.CellToWorld(10, 8)), "Territory: consulta por posição de mundo");

        terr.Rebuild(new List<Vector2Int>(), 2.75f);
        Check(!terr.Contains(10, 8), "Territory: sem torres, sem território");

        // duas torres afastadas: território não vira uma bolha só
        terr.Rebuild(new List<Vector2Int> { new Vector2Int(4, 8), new Vector2Int(16, 8) }, 2.75f);
        Check(terr.Contains(4, 8) && terr.Contains(16, 8) && !terr.Contains(10, 8),
            "Territory: torres afastadas geram bolhas separadas");

        Console.WriteLine();
        Console.WriteLine(_failed == 0 ? ">>> TODOS OS TESTES PASSARAM" : $">>> {_failed} TESTE(S) FALHARAM");
        return _failed;
    }

    // Simula um inimigo: anda seguindo SampleDirection em passos de 0.05
    // e reporta se chega ao centro da base sem estourar o limite de passos.
    static bool Walk(GridMap map, FlowField flow, Vector2Int from, Vector2Int goal, out int steps)
    {
        var pos = map.CellToWorld(from);
        var goalPos = map.CellToWorld(goal);
        for (steps = 0; steps < 20000; steps++)
        {
            if ((pos - goalPos).sqrMagnitude < 0.4f * 0.4f) return true;
            var dir = flow.SampleDirection(pos);
            if (dir == Vector3.zero) return false;
            pos += dir * 0.05f;
            // inimigo nunca deve pisar em célula bloqueada
            var c = map.WorldToCell(pos);
            if (map.InBounds(c.x, c.y) && map.IsBlocked(c.x, c.y)) return false;
        }
        return false;
    }
}
