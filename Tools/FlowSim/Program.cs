// Testes headless da lógica pura do TDFende (mesmos .cs do projeto Unity).
using System;
using System.Collections.Generic;
using TDFende;
using UnityEngine;

class Program
{
    static int _failed;

    static void Check(bool cond, string name)
    {
        Console.WriteLine($"{(cond ? "OK  " : "FALHOU")}  {name}");
        if (!cond) _failed++;
    }

    static int Main(string[] args)
    {
        // `dotnet run -- match [N]` roda o laboratório de balanceamento em vez dos testes.
        if (args.Length > 0 && args[0] == "match")
        {
            int n = args.Length > 1 ? int.Parse(args[1]) : 100;
            BalanceLab.Run(n);
            return 0;
        }

        // `dotnet run -- sweep [N]` varre os knobs de balanceamento e recomenda valores.
        if (args.Length > 0 && args[0] == "sweep")
        {
            int n = args.Length > 1 ? int.Parse(args[1]) : 30;
            bool fine = args.Length > 2 && args[2] == "fine";
            BalanceLab.Sweep(n, fine);
            return 0;
        }

        // `dotnet run -- dump-catalogs [pasta]` escreve os catálogos como arquivo de
        // balanceamento, para o Felipe editar número sem tocar em C# nem recompilar.
        if (args.Length > 0 && args[0] == "dump-catalogs")
        {
            string dir = args.Length > 1
                ? args[1]
                : System.IO.Path.Combine(AppContext.BaseDirectory, "../../../../../Balanceamento");
            System.IO.Directory.CreateDirectory(dir);
            string sendsPath = System.IO.Path.Combine(dir, "envios.txt");
            string towersPath = System.IO.Path.Combine(dir, "torres.txt");
            // UTF-8 com BOM: ver CatalogLoader — protege os acentos de editor que assume ANSI
            var utf8Bom = new System.Text.UTF8Encoding(true);
            System.IO.File.WriteAllText(sendsPath, CatalogJson.SerializeSends(), utf8Bom);
            System.IO.File.WriteAllText(towersPath, CatalogJson.SerializeTowers(), utf8Bom);
            Console.WriteLine($"escrito: {System.IO.Path.GetFullPath(sendsPath)}");
            Console.WriteLine($"escrito: {System.IO.Path.GetFullPath(towersPath)}");
            return 0;
        }

        // `dotnet run -- replay <arquivo>` reproduz uma partida gravada no jogo (F9)
        // e imprime o que aconteceu. É como um "achei estranho" vira estado inspecionável.
        if (args.Length > 1 && args[0] == "replay")
            return RunReplay(args[1], System.Array.IndexOf(args, "--forcar") >= 0);

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

        // ================= TOWER WARS =================
        Console.WriteLine();
        TowerWarsTests();
        ArtLayerTests();
        FingerprintTests();
        CatalogMergeTests();
        CatalogKeyTests();
        SimSignatureTests();

        // ================= ESTÁGIOS DA TORRE (modelo 3D por par de níveis) =================
        Console.WriteLine();
        Check(TowerStages.ForLevel(1) == 1 && TowerStages.ForLevel(2) == 1,
            "Estágio: níveis 1-2 usam o modelo 1");
        Check(TowerStages.ForLevel(3) == 2 && TowerStages.ForLevel(4) == 2,
            "Estágio: níveis 3-4 usam o modelo 2");
        Check(TowerStages.ForLevel(5) == 3 && TowerStages.ForLevel(6) == 3,
            "Estágio: níveis 5-6 usam o modelo 3");
        Check(TowerStages.ForLevel(0) == 1 && TowerStages.ForLevel(99) == TowerStages.Count,
            "Estágio: nível fora da faixa fica no primeiro ou no último modelo");
        Check(TowerStages.FirstLevel(1) == 1 && TowerStages.FirstLevel(2) == 3 && TowerStages.FirstLevel(3) == 5,
            "Estágio: primeiro nível de cada modelo (o crescimento recomeça nele)");
        Check(TowerStages.ModelName("Torre_Gelo", 2) == "Torre_Gelo_2",
            "Estágio: nome do modelo do estágio (Resources/TDFende/Torres/Torre_Gelo_2)");
        Check(TowerStages.ForLevel(TowerWarsConfig.MaxTowerLevel) == TowerStages.Count,
            "Estágio: o nível máximo cai no último modelo");

        // prova do CI (.github/workflows/flowsim.yml, "falha_de_proposito"): o job fica vermelho
        Check(Environment.GetEnvironmentVariable("TDFENDE_FALHA_DE_PROPOSITO") != "1",
            "CI: sem falha de propósito");

        Console.WriteLine();
        Console.WriteLine(_failed == 0 ? ">>> TODOS OS TESTES PASSARAM" : $">>> {_failed} TESTE(S) FALHARAM");
        return _failed;
    }

    // ---- ajudantes dos testes de torre: mesmo cenário, só muda o tipo de torre ----

    /// <param name="sends">
    /// Quantas compras. Um só quando a pergunta é sobre ALVO ÚNICO — com vários, eles
    /// se aglomeram e o dano em área acerta o grupo, o que mede outra coisa.
    /// </param>
    static LaneSim Duel(int sendId, int towerType, float seconds, int sends = 4)
    {
        // Atrito desligado durante a medição: o raio de fronteira varia por TIPO de
        // torre, então com atrito ligado esta comparação media território, não dano —
        // e trocar o atrito de 0,16 para 0,19 chegou a inverter o resultado.
        float savedAttrition = TowerWarsConfig.AttritionPctPerSecond;
        TowerWarsConfig.AttritionPctPerSecond = 0f;
        try
        {
            var lane = new LaneSim(24, 16);
            var feeder = new LaneSim(24, 16);
            lane.DebugGrantGold(5000);
            // três torres na frente do caminho, para o alvo passar pelo alcance
            lane.TryBuildTower(new Vector2Int(10, 8), towerType);
            lane.TryBuildTower(new Vector2Int(12, 7), towerType);
            lane.TryBuildTower(new Vector2Int(12, 9), towerType);
            feeder.DebugGrantGold(5000);
            for (int i = 0; i < sends; i++) feeder.TrySend(sendId, lane, new Random(77 + i));
            Advance(lane, seconds);
            return lane;
        }
        finally
        {
            TowerWarsConfig.AttritionPctPerSecond = savedAttrition;
        }
    }

    static int KillsAgainst(int sendId, int towerType, float seconds) =>
        Duel(sendId, towerType, seconds).KilledByTower;

    /// <summary>
    /// Dano de TORRE entregue = o que falta nos vivos + a vida cheia de quem morreu.
    /// Devolve -1 se alguém VAZOU: o dano levado por quem escapou some do placar, e a
    /// comparação viraria um número inventado. É assim que o teste da Sentinela comparava
    /// contra um zero fabricado em vez de contra o dano real do Canhão.
    /// </summary>
    static float DamageDealt(int sendId, int towerType, float seconds, int sends = 4)
    {
        var lane = Duel(sendId, towerType, seconds, sends);
        if (lane.TotalLeaked > 0) return -1f;

        float dealt = 0f;
        float maxHp = SendCatalog.Get(sendId).Hp; // escala ~1 nos primeiros segundos
        for (int s = 0; s < lane.EnemySlotCount; s++)
            if (lane.TryGetEnemy(s, out var e)) dealt += e.MaxHp - e.Hp;
        dealt += lane.KilledByTower * maxHp;
        return dealt;
    }

    /// <summary>
    /// Segundos até a torre derrubar um único alvo, ou -1 se ele vazou antes.
    /// É a métrica certa quando as duas torres MATAM: aí o dano acumulado empata no teto
    /// da vida do alvo e não distingue nada.
    /// </summary>
    static float TimeToKill(int sendId, int towerType, float limit = 12f)
    {
        float saved = TowerWarsConfig.AttritionPctPerSecond;
        TowerWarsConfig.AttritionPctPerSecond = 0f; // isolar a torre do território
        try
        {
            var lane = new LaneSim(24, 16);
            var feeder = new LaneSim(24, 16);
            lane.DebugGrantGold(5000);
            lane.TryBuildTower(new Vector2Int(10, 8), towerType);
            lane.TryBuildTower(new Vector2Int(12, 7), towerType);
            lane.TryBuildTower(new Vector2Int(12, 9), towerType);
            feeder.DebugGrantGold(5000);
            feeder.TrySend(sendId, lane, new Random(77));

            int steps = (int)System.Math.Round(limit / (double)TowerWarsConfig.FixedStep);
            for (int i = 0; i < steps; i++)
            {
                lane.Tick(TowerWarsConfig.FixedStep);
                if (lane.KilledByTower > 0) return (i + 1) * TowerWarsConfig.FixedStep;
                if (lane.TotalLeaked > 0) return -1f; // escapou: medição inválida
            }
            return -1f;
        }
        finally { TowerWarsConfig.AttritionPctPerSecond = saved; }
    }

    /// <summary>Avanço do inimigo mais adiantado, com uma torre do tipo dado numa célula.</summary>
    static float DeepestXWith(int towerType, Vector2Int cell, float seconds)
    {
        float saved = TowerWarsConfig.AttritionPctPerSecond;
        TowerWarsConfig.AttritionPctPerSecond = 0f; // raio de fronteira varia por tipo
        try
        {
            var lane = new LaneSim(24, 16);
            var feeder = new LaneSim(24, 16);
            lane.DebugGrantGold(3000);
            lane.TryBuildTower(cell, towerType);
            feeder.DebugGrantGold(3000);
            feeder.TrySend(SendCatalog.IdOf("lobo"), lane, new Random(5)); // Corredor: rápido, sente a lentidão
            Advance(lane, seconds);
            return DeepestX(lane);
        }
        finally { TowerWarsConfig.AttritionPctPerSecond = saved; }
    }

    static float DeepestX(LaneSim lane)
    {
        float x = float.NegativeInfinity;
        for (int s = 0; s < lane.EnemySlotCount; s++)
            if (lane.TryGetEnemy(s, out var e) && e.Pos.x > x) x = e.Pos.x;
        return x == float.NegativeInfinity ? 0f : x;
    }

    static int TerritoryCells(LaneSim lane)
    {
        int n = 0;
        for (int y = 0; y < lane.Map.Height; y++)
        for (int x = 0; x < lane.Map.Width; x++)
            if (lane.Territory.Contains(x, y)) n++;
        return n;
    }

    static int RunReplay(string path, bool force)
    {
        if (!System.IO.File.Exists(path))
        {
            Console.WriteLine($"arquivo não encontrado: {path}");
            return 2;
        }
        if (!Replay.TryParse(System.IO.File.ReadAllText(path), out var replay, out string err))
        {
            Console.WriteLine($"replay inválido: {err}");
            return 2;
        }

        Console.WriteLine($"Replay {path}");
        Console.WriteLine($"  semente {replay.Seed} | dificuldade {replay.Difficulty} | " +
                          $"grid {replay.Width}x{replay.Height} | {replay.Ticks} tiques " +
                          $"({replay.Ticks * TowerWarsConfig.FixedStep:0.0}s) | {replay.Commands.Count} comandos");

        // Regras diferentes = outros números = OUTRA partida. Sem esta recusa, o relatório sairia
        // plausível e errado, e a investigação perseguiria um bug que não existe (BUG-05).
        var diff = replay.Verify();
        if (diff != SignatureDiff.None)
        {
            Console.WriteLine($"  {(force ? "AVISO (--forcar)" : "ERRO")}: {Replay.Describe(diff)}.");
            if (!force)
            {
                Console.WriteLine("  A reprodução usaria outros números e daria um desfecho que nunca aconteceu.");
                Console.WriteLine("  Restaure o balanceamento da gravação (ou volte ao código dela) e rode de novo; " +
                                  "para reproduzir assim mesmo: replay <arquivo> --forcar.");
                return 3;
            }
        }

        // resumo do que o jogador fez, para ver a estratégia sem assistir
        int builds = 0, upgrades = 0;
        var sends = new int[SendCatalog.Count];
        foreach (var (_, c) in replay.Commands)
        {
            if (c.Kind == CommandKind.Build) builds++;
            else if (c.Kind == CommandKind.Upgrade) upgrades++;
            else if (c.SendId >= 0 && c.SendId < sends.Length) sends[c.SendId]++;
        }
        Console.WriteLine($"  jogador: {builds} torres, {upgrades} upgrades");
        for (int i = 0; i < sends.Length; i++)
            if (sends[i] > 0) Console.WriteLine($"    {SendCatalog.Get(i).Name,-10} x{sends[i]}");

        var r = replay.Run();
        Console.WriteLine();
        Console.WriteLine($"  VOCÊ  vidas {r.Player.Lives,3} | ouro {r.Player.Gold,5} | renda {r.Player.Income,4} | " +
                          $"torres {r.Player.TowerCount,3} (nv {r.Player.TotalTowerLevels})");
        Console.WriteLine($"  IA    vidas {r.Foe.Lives,3} | ouro {r.Foe.Gold,5} | renda {r.Foe.Income,4} | " +
                          $"torres {r.Foe.TowerCount,3} (nv {r.Foe.TotalTowerLevels})");
        Console.WriteLine($"  mortes na SUA lane: {r.Player.KilledByTower} por tiro, " +
                          $"{r.Player.KilledByAttrition} por atrito, {r.Player.TotalLeaked} vazaram");
        Console.WriteLine($"  mortes na lane da IA: {r.Foe.KilledByTower} por tiro, " +
                          $"{r.Foe.KilledByAttrition} por atrito, {r.Foe.TotalLeaked} vazaram");
        Console.WriteLine();
        Console.WriteLine($"  {(r.Over ? (r.Player.Dead ? "derrota" : "vitória") : "partida não terminou na gravação")}");
        Console.WriteLine($"  fingerprint: {r.StateFingerprint()}");
        bool? same = replay.FinalMatches(r);
        if (same == true) Console.WriteLine("  estado final igual ao gravado.");
        else if (same == false)
            Console.WriteLine($"  AVISO: estado final DIVERGIU do gravado ({replay.Final}); mudou a lógica da Sim, " +
                              "ou o float deu outro resultado neste runtime (TEC-27).");
        return 0;
    }

    // Roda a lane por N segundos de tempo simulado, em passo fixo.
    // Round, não truncamento: 30f / (1f/30f) dá 899,9999 em float, e truncar
    // simularia um passo a MENOS do que o pedido — exatamente na fronteira
    // que os testes de tique de renda medem.
    static void Advance(LaneSim lane, float seconds)
    {
        int steps = (int)Math.Round(seconds / (double)TowerWarsConfig.FixedStep);
        for (int i = 0; i < steps; i++) lane.Tick(TowerWarsConfig.FixedStep);
    }

    /// <summary>
    /// BUG-04: o arquivo de balanceamento COMPLETA o catálogo de fábrica por nome, na ordem da fábrica, em vez de
    /// substituí-lo. Antes, um envios.txt de 9 linhas apagava o 10º bicho e um torres.txt com linhas trocadas trocava
    /// as torres de índice em silêncio. A fábrica entra como parâmetro: o teste usa um catálogo de 10 envios que só existe aqui.
    /// </summary>
    static void CatalogMergeTests()
    {
        SendCatalog.ResetToDefaults();
        TowerCatalog.ResetToDefaults();
        var factory9 = (SendUnit[])SendCatalog.All.Clone();
        var hiena = new SendUnit { Key = "hiena", Name = "Hiena", Cost = 33, Hp = 90f, Speed = 3f, IncomeBonus = 3, Bounty = 11, Count = 1, AttritionScale = 1f };
        var factory10 = new SendUnit[factory9.Length + 1];
        factory9.CopyTo(factory10, 0);
        factory10[factory9.Length] = hiena;
        string text9 = CatalogJson.SerializeSends(); // exportado quando o jogo tinha 9 bichos

        // 1) o caso do cartão: fábrica de 10 e arquivo de 9 linhas
        Check(SendCatalog.Merge(factory10, text9, out var m10, out string e10) && m10.Length == 10,
            $"Merge: arquivo de 9 linhas num jogo de 10 envios continua com 10 ({m10?.Length}; {e10})");
        Check(m10 != null && m10[9].Name == "Hiena" && m10[9].Cost == 33 && m10[9].Hp == 90f,
            "Merge: o 10º envio ausente do arquivo entra com os valores de fábrica");

        // 2) a ordem das linhas do arquivo não importa: o índice é a identidade
        var lines = text9.Split('\n');
        System.Array.Reverse(lines);
        bool sameOrder = SendCatalog.Merge(factory10, string.Join("\n", lines), out var mRev, out _);
        for (int i = 0; sameOrder && i < factory10.Length; i++) sameOrder &= mRev[i].Name == factory10[i].Name;
        Check(sameOrder, "Merge: arquivo com as linhas em ordem invertida sai na ordem da fábrica");

        // 3) campo omitido = valor de fábrica DO MESMO BICHO, não um genérico
        Check(SendCatalog.Merge(factory10, "name=Lobo;cost=40", out var mLobo, out _)
              && mLobo[2].Name == "Lobo" && mLobo[2].Cost == 40
              && mLobo[2].Hp == factory9[2].Hp && mLobo[2].Speed == factory9[2].Speed
              && mLobo[2].Bounty == factory9[2].Bounty && mLobo[0].Cost == factory9[0].Cost,
            "Merge: linha só com o custo muda o custo e mantém vida, velocidade e recompensa do Lobo de fábrica");

        // 4) nome que não existe: o arquivo é recusado, com a lista e a dica de acento
        Check(!SendCatalog.Merge(factory10, "name=Aguia;cost=1", out var mUnk, out string eUnk) && mUnk == null
              && eUnk.Contains("Aguia") && eUnk.Contains("Águia") && eUnk.Contains("Rato") && eUnk.Contains("linha 1"),
            "Merge: nome desconhecido recusa o arquivo, lista os nomes e sugere o acento (" + eUnk + ")");
        Check(!SendCatalog.Merge(factory10, "name=Lobo;cost=40\nname=Unicornio;cost=9", out _, out string eUnk2) && eUnk2.Contains("linha 2"),
            "Merge: um nome ruim recusa o arquivo inteiro e aponta a linha");

        // 5) nome repetido
        Check(!SendCatalog.Merge(factory10, "name=Lobo;cost=40\nname=Lobo;cost=50", out _, out string eDup)
              && eDup.Contains("Lobo") && eDup.Contains("linha 2") && eDup.Contains("linha 1"),
            "Merge: nome repetido é recusado e aponta as duas linhas (" + eDup + ")");

        // 6) arquivo de uma versão antiga: nenhum nome bate
        Check(!SendCatalog.Merge(factory10, "name=Recruta;cost=10;hp=40\nname=Colosso;cost=90;hp=400", out _, out string eOld)
              && eOld.Contains("versão antiga"), "Merge: arquivo de soldados (nenhum nome bate) segue recusado com a mensagem antiga");

        // 7) ida e volta: o que o jogo exporta, lido de novo, devolve exatamente a fábrica
        bool roundTrip = SendCatalog.Merge(factory9, text9, out var mRound, out _) && mRound.Length == factory9.Length;
        for (int i = 0; roundTrip && i < factory9.Length; i++)
            roundTrip &= mRound[i].Name == factory9[i].Name && mRound[i].Cost == factory9[i].Cost && mRound[i].Hp == factory9[i].Hp
                         && mRound[i].Speed == factory9[i].Speed && mRound[i].IncomeBonus == factory9[i].IncomeBonus
                         && mRound[i].Bounty == factory9[i].Bounty && mRound[i].Count == factory9[i].Count
                         && mRound[i].AttritionScale == factory9[i].AttritionScale;
        Check(roundTrip, "Merge: o arquivo exportado, relido, devolve exatamente o catálogo de fábrica");

        // 8) Get com id inválido FALHA ALTO, com mensagem, e nunca devolve outro bicho
        string getMsg = null;
        bool threw = false;
        foreach (int bad in new[] { -1, SendCatalog.Count, 99 })
        {
            try { SendCatalog.Get(bad); }
            catch (System.ArgumentOutOfRangeException ex) { threw = true; getMsg = ex.Message; }
            Check(threw, $"Get({bad}): lança ArgumentOutOfRangeException em vez de estourar sem explicação ou cair em outro id");
            threw = false;
        }
        try { SendCatalog.Get(99); } catch (System.ArgumentOutOfRangeException ex) { getMsg = ex.Message; }
        Check(getMsg != null && getMsg.Contains("99") && getMsg.Contains(SendCatalog.Count.ToString()),
            "Get(99): a mensagem diz o id e quantos envios existem (" + getMsg + ")");
        Check(SendCatalog.IsValidId(0) && SendCatalog.IsValidId(SendCatalog.Count - 1)
              && !SendCatalog.IsValidId(-1) && !SendCatalog.IsValidId(SendCatalog.Count),
            "IsValidId: 0 a Count-1");
        Check(SendCatalog.TryGet(1, out var ok1) && ok1.Name == SendCatalog.Get(SendCatalog.IdOf("cachorro")).Name && !SendCatalog.TryGet(99, out _),
            "TryGet: devolve o envio válido e false para o inválido");

        // 9) a borda do sistema RECUSA id inválido em vez de estourar
        var payer = new LaneSim(24, 16);
        var target = new LaneSim(24, 16);
        int g0 = payer.Gold, inc0 = payer.Income;
        Check(!payer.CanAfford(-1) && !payer.CanAfford(99), "CanAfford: id inválido é false");
        Check(!payer.TrySend(99, target, new Random(1)) && !payer.TrySend(-1, target, new Random(1))
              && payer.Gold == g0 && payer.Income == inc0 && payer.TotalSent == 0 && target.EnemiesAlive == 0,
            "TrySend: id inválido é recusado sem tocar ouro, renda, contadores nem a lane alvo");
        var runner = new MatchRunner(3, TowerWarsAi.Personality.Normal, 24, 16);
        int applied = 0;
        runner.CommandApplied += (t, c) => applied++;
        runner.Enqueue(MatchCommand.Send(99));
        runner.Step();
        Check(applied == 0 && runner.Player.TotalSent == 0, "MatchRunner: comando de envio com id inválido é recusado (não aplica nem entra no replay)");

        // 10) as torres (D3): mesma regra, e o caso que trocava as torres de lugar
        TowerCatalog.ResetToDefaults();
        var towers6 = (TowerType[])TowerCatalog.All.Clone();
        string gelo = "name=Gelo;cost=41;range=3.2;cooldown=0.9;damage=4;slow=0.55;slowsecs=1.6";
        string canhao = "name=Canhão;cost=30";
        Check(TowerCatalog.Merge(towers6, gelo + "\n" + canhao, out var mt, out string et) && mt.Length == towers6.Length
              && mt[0].Name == "Canhão" && mt[2].Name == "Gelo" && mt[0].Cost == 30 && mt[2].Cost == 41,
            $"Merge de torres: Gelo escrito antes do Canhão NÃO troca os índices ({(mt == null ? et : mt[0].Name + "/" + mt[2].Name)})");
        Check(mt != null && mt[0].BorderRadius == towers6[0].BorderRadius && mt[0].Range == towers6[0].Range
              && mt[4].Name == "Fogo" && mt[4].BurnPctPerSecond == towers6[4].BurnPctPerSecond,
            "Merge de torres: campo omitido mantém o da torre de fábrica (fronteira e alcance do Canhão) e as não citadas entram inteiras");
        Check(!TowerCatalog.Merge(towers6, "name=Cano;cost=9", out _, out string etUnk) && etUnk.Contains("Cano") && etUnk.Contains("Canhão"),
            "Merge de torres: nome desconhecido recusa o arquivo e lista as torres (" + etUnk + ")");
        Check(!TowerCatalog.Merge(towers6, canhao + "\n" + canhao, out _, out string etDup) && etDup.Contains("linha 2"),
            "Merge de torres: nome repetido é recusado");
    }

    /// <summary>
    /// TEC-12: bicho e torre têm uma CHAVE estável (ASCII, não muda com o tema). O arquivo de balanceamento casa por
    /// chave, o nome exibido é só texto, e a vista escolhe modelo/efeito pela chave. Cobertura: toda chave de fábrica
    /// precisa ter entrada na vista (ViewKeys), senão o 10º bicho nasceria com o modelo de outro.
    /// </summary>
    static void CatalogKeyTests()
    {
        SendCatalog.ResetToDefaults();
        TowerCatalog.ResetToDefaults();
        var sends = (SendUnit[])SendCatalog.All.Clone();
        var towers = (TowerType[])TowerCatalog.All.Clone();

        // 1) chaves da fábrica: preenchidas, únicas, ASCII minúsculo
        bool Shape(string k)
        {
            if (string.IsNullOrEmpty(k)) return false;
            foreach (char c in k) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            return true;
        }
        var seenKeys = new HashSet<string>();
        bool sendKeysOk = true, towerKeysOk = true;
        foreach (var u in sends) sendKeysOk &= Shape(u.Key) && seenKeys.Add("s:" + u.Key);
        foreach (var t in towers) towerKeysOk &= Shape(t.Key) && seenKeys.Add("t:" + t.Key);
        Check(sendKeysOk, "Chaves dos envios: preenchidas, únicas, ASCII minúsculo");
        Check(towerKeysOk, "Chaves das torres: preenchidas, únicas, ASCII minúsculo");

        // 2) IdOf / TryIdOf
        Check(SendCatalog.IdOf("lobo") == 2 && SendCatalog.IdOf("nao_existe") == -1
              && SendCatalog.TryIdOf("elefante", out int idEl) && idEl == 8 && !SendCatalog.TryIdOf("Lobo", out _),
            "SendCatalog.IdOf/TryIdOf: acha pela chave (exata), -1/false se não existe");
        Check(TowerCatalog.IdOf("gelo") == 2 && TowerCatalog.IdOf("canhao") == 0 && TowerCatalog.IdOf("x") == -1
              && TowerCatalog.TryIdOf("ar", out int idAr) && idAr == 5,
            "TowerCatalog.IdOf/TryIdOf: acha pela chave");

        // 3) a vista conhece TODA chave de fábrica (o ganho central do TEC-12)
        var noModel = new List<string>();
        foreach (var u in sends) if (System.Array.IndexOf(ViewKeys.Enemy, u.Key) < 0) noModel.Add("bicho " + u.Key);
        foreach (var t in towers) if (System.Array.IndexOf(ViewKeys.Tower, t.Key) < 0) noModel.Add("torre " + t.Key);
        Check(noModel.Count == 0, "Vista: toda chave do catálogo de fábrica tem modelo e efeito (faltam: " + string.Join(", ", noModel) + ")");
        Check(ViewKeys.ArcShot("morteiro") && !ViewKeys.ArcShot("canhao") && !ViewKeys.ArcShot("gelo"),
            "Vista: só o Morteiro lança o tiro em arco");

        // 4) serialização escreve a chave primeiro e a ida e volta preserva a chave
        string text = CatalogJson.SerializeSends();
        Check(text.Contains("key=lobo;name=Lobo;"), "Serialize: a linha começa pela chave (key=lobo;name=Lobo;...)");
        Check(CatalogJson.SerializeTowers().Contains("key=gelo;name=Gelo;"), "Serialize das torres: key= primeiro");
        Check(SendCatalog.Merge(sends, text, out var back, out _) && back[4].Key == "aguia" && back[4].Name == "Águia",
            "Merge: ida e volta mantém chave e nome");

        // 5) o tema renomeia: chave casa, o nome do arquivo NÃO renomeia o jogo (D2), e o aviso diz isso
        Check(SendCatalog.Merge(sends, "key=lobo;name=Lupus;cost=40", out var ren, out string eRen, out string warn)
              && ren[2].Name == "Lobo" && ren[2].Cost == 40 && ren[2].Hp == sends[2].Hp
              && warn != null && warn.Contains("Lupus") && warn.Contains("lobo"),
            "Merge: key=lobo;name=Lupus casa pela chave, não renomeia e avisa (" + eRen + " | " + warn + ")");

        // 6) e o jogo renomeado (tema) lê o arquivo antigo: casa pela chave, mesmo com outro nome de fábrica
        var themed = (SendUnit[])sends.Clone();
        themed[2].Name = "Lupus";
        Check(SendCatalog.Merge(themed, text, out var th, out string eth) && th.Length == themed.Length
              && th[2].Name == "Lupus" && th[2].Key == "lobo" && th[2].Cost == sends[2].Cost,
            "Merge: tema renomeou o Lobo; o arquivo exportado antes continua casando pela chave (" + eth + ")");

        // 7) arquivo antigo (só name=) segue funcionando, casando pelo nome
        Check(SendCatalog.Merge(sends, "name=Lobo;cost=41", out var leg, out _, out string legWarn)
              && leg[2].Key == "lobo" && leg[2].Cost == 41 && legWarn != null && legWarn.Contains("sem chave"),
            "Merge: linha sem key= casa pelo nome e avisa para exportar de novo (" + legWarn + ")");

        // 8) chave desconhecida: recusa com a lista de chaves
        Check(!SendCatalog.Merge(sends, "key=hiena;cost=40", out _, out string eKey) && eKey.Contains("hiena") && eKey.Contains("lobo")
              && eKey.Contains("linha 1") && eKey.Contains("chave"),
            "Merge: chave desconhecida recusa o arquivo e lista as chaves (" + eKey + ")");
        Check(!SendCatalog.Merge(sends, "key=lobo;cost=40\nkey=lobo;cost=50", out _, out string eDupK)
              && eDupK.Contains("lobo") && eDupK.Contains("linha 2") && eDupK.Contains("linha 1"),
            "Merge: chave repetida é recusada, com as duas linhas");
        // mesmo bicho escrito uma vez pela chave e outra pelo nome: repetição
        Check(!SendCatalog.Merge(sends, "key=lobo;cost=40\nname=Lobo;cost=50", out _, out string eMix) && eMix.Contains("linha 2"),
            "Merge: o mesmo bicho pela chave e pelo nome é repetição");

        // 9) torres: mesma regra
        Check(TowerCatalog.Merge(towers, "key=gelo;name=Frost;cost=41", out var mt, out _, out string tw)
              && mt[2].Name == "Gelo" && mt[2].Cost == 41 && tw != null && tw.Contains("Frost"),
            "Torres: casa pela chave, nome do arquivo não renomeia");
        Check(!TowerCatalog.Merge(towers, "key=canhoes;cost=9", out _, out string etk) && etk.Contains("canhoes") && etk.Contains("canhao"),
            "Torres: chave desconhecida lista as chaves");

        // 10) a chave entra na assinatura do replay
        var k1 = (SendUnit[])sends.Clone();
        k1[1].Key = "cachorro2";
        Check(SimSignature.Sends(k1) != SimSignature.Sends(sends), "Assinatura: mudar só a Key de um envio muda o bloco sends");
        var k2 = (TowerType[])towers.Clone();
        k2[1].Key = "morteiro2";
        Check(SimSignature.Towers(k2) != SimSignature.Towers(towers), "Assinatura: mudar só a Key de uma torre muda o bloco towers");

        // 11) LoadFrom guarda o aviso para o log do boot; e o catálogo em uso continua o de fábrica na ordem
        SendCatalog.ResetToDefaults();
        Check(SendCatalog.LoadFrom("name=Lobo;cost=33", out _) && SendCatalog.LastWarning != null && SendCatalog.Get(SendCatalog.IdOf("lobo")).Cost == 33
              && SendCatalog.Get(SendCatalog.IdOf("lobo")).Key == "lobo" && SendCatalog.Count == 9, "LoadFrom: carrega, completa por chave e deixa o aviso em LastWarning");
        SendCatalog.ResetToDefaults();
        Check(SendCatalog.LastWarning == null, "ResetToDefaults limpa o aviso");
    }

    /// <summary>
    /// BUG-05: a assinatura do replay cobre TODOS os campos de balanceamento (envios, torres, regras), em 64 bits,
    /// sem GetHashCode. Antes, queima, empurrão, nomes, atrito e escalada podiam mudar sem a assinatura notar.
    /// </summary>
    static void SimSignatureTests()
    {
        SendCatalog.ResetToDefaults();
        TowerCatalog.ResetToDefaults();
        var sends = (SendUnit[])SendCatalog.All.Clone();
        var towers = (TowerType[])TowerCatalog.All.Clone();

        // 0) a implementação do FNV-1a confere com os vetores públicos
        var v0 = new StateHash();
        var v1 = new StateHash();
        v1.AddUtf8("a");
        Check(v0.Value == 0xcbf29ce484222325UL && v1.Value == 0xaf63dc4c8601ec8cUL,
            "FNV-1a: vetores públicos (\"\" e \"a\")");

        // 1) cada campo de SendUnit e de TowerType, mudado um a um, muda o bloco
        string sig0 = SimSignature.Sends(sends), tsig0 = SimSignature.Towers(towers);
        foreach (var f in typeof(SendUnit).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var alt = (SendUnit[])sends.Clone();
            object boxed = alt[3];
            f.SetValue(boxed, Bump(f.FieldType, f.GetValue(boxed)));
            alt[3] = (SendUnit)boxed;
            Check(SimSignature.Sends(alt) != sig0, $"Assinatura dos envios muda com o campo {f.Name}");
        }
        foreach (var f in typeof(TowerType).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var alt = (TowerType[])towers.Clone();
            object boxed = alt[4];
            f.SetValue(boxed, Bump(f.FieldType, f.GetValue(boxed)));
            alt[4] = (TowerType)boxed;
            Check(SimSignature.Towers(alt) != tsig0, $"Assinatura das torres muda com o campo {f.Name}");
        }

        // 2) guarda: todo campo público dos tipos de balanceamento está na lista da assinatura
        var seen = new HashSet<string>();
        SimSignature.Sends(sends, seen);
        SimSignature.Towers(towers, seen);
        SimSignature.Rules(seen);
        var missing = new List<string>();
        void Guard(System.Type t, System.Reflection.BindingFlags flags)
        {
            foreach (var f in t.GetFields(System.Reflection.BindingFlags.Public | flags))
                if (!seen.Contains(t.Name + "." + f.Name)) missing.Add(t.Name + "." + f.Name);
        }
        Guard(typeof(SendUnit), System.Reflection.BindingFlags.Instance);
        Guard(typeof(TowerType), System.Reflection.BindingFlags.Instance);
        Guard(typeof(TowerWarsAi.Personality), System.Reflection.BindingFlags.Instance);
        Guard(typeof(TowerWarsConfig), System.Reflection.BindingFlags.Static);
        if (!seen.Contains("GameConfig.CellSize")) missing.Add("GameConfig.CellSize");
        Check(missing.Count == 0,
            "Guarda: todo campo de balanceamento entra na assinatura (faltam: " + string.Join(", ", missing) + ")");

        // 3) regras: atrito, escalada, personalidade da IA e versão
        string r0 = SimSignature.Rules();
        float attr = TowerWarsConfig.AttritionPctPerSecond, scale = TowerWarsConfig.SendScalePerMinute;
        TowerWarsConfig.AttritionPctPerSecond = attr + 0.01f;
        Check(SimSignature.Rules() != r0, "Regras: mudar o atrito muda a assinatura");
        TowerWarsConfig.AttritionPctPerSecond = attr;
        TowerWarsConfig.SendScalePerMinute = scale + 0.01f;
        Check(SimSignature.Rules() != r0, "Regras: mudar a escalada dos envios muda a assinatura");
        TowerWarsConfig.SendScalePerMinute = scale;
        Check(SimSignature.Rules() == r0, "Regras: restaurados os valores, a assinatura volta");
        var hard = TowerWarsAi.Personality.Hard;
        hard.CounterStrength += 0.1f;
        Check(SimSignature.Rules(TowerWarsAi.Personality.Easy, TowerWarsAi.Personality.Normal, hard) != r0,
            "Regras: mudar a personalidade Difícil da IA muda a assinatura");
        Check(SimSignature.Rules(TowerWarsAi.Personality.Easy, TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Hard,
                  SimRules.Version + 1) != r0, "Regras: SimRules.Version entra na assinatura");

        // 4) ordem, determinismo, formato
        var swapped = (SendUnit[])sends.Clone();
        (swapped[0], swapped[1]) = (swapped[1], swapped[0]);
        Check(SimSignature.Sends(swapped) != sig0, "Envios: trocar a ordem de dois muda a assinatura");
        Check(SimSignature.Sends(sends) == sig0 && SimSignature.Rules() == r0, "Assinatura: mesma entrada, mesma saída");
        Check(sig0.Length == 16 && System.Text.RegularExpressions.Regex.IsMatch(sig0, "^[0-9a-f]{16}$"),
            "Assinatura: 16 dígitos hexa (64 bits)");
        Check(sig0 != tsig0 && tsig0 != r0, "Assinatura: os três blocos são diferentes entre si");
        // constante de fábrica: uma mudança involuntária de formato aparece no diff deste teste
        Check(sig0 == SimSignatureExpected.Sends && tsig0 == SimSignatureExpected.Towers && r0 == SimSignatureExpected.Rules,
            $"Assinatura de fábrica fixa (sends {sig0}, towers {tsig0}, rules {r0}); se mudou de propósito, atualize SimSignatureExpected");

        // 5) -0 = +0, qualquer NaN igual
        var negZero = (TowerType[])towers.Clone();
        var posZero = (TowerType[])towers.Clone();
        negZero[0].Knockback = -0f; posZero[0].Knockback = 0f;
        Check(SimSignature.Towers(negZero) == SimSignature.Towers(posZero), "Assinatura: -0 e +0 são o mesmo valor");
        var nan1 = (TowerType[])towers.Clone();
        var nan2 = (TowerType[])towers.Clone();
        nan1[0].Damage = float.NaN;
        nan2[0].Damage = System.BitConverter.Int32BitsToSingle(unchecked((int)0xFFC12345));
        Check(SimSignature.Towers(nan1) == SimSignature.Towers(nan2), "Assinatura: todo NaN é o mesmo valor");

        // 6) replay formato 2
        var rep = new Replay { Seed = 5, Difficulty = "Normal", Ticks = 30, Final = "t30 abc #123" };
        rep.Record(3, MatchCommand.Send(1));
        string txt = rep.Serialize();
        Check(txt.StartsWith("tdfende-replay 2\n") && txt.Contains("\nsends " + sig0 + "\n")
              && txt.Contains("\ntowers " + tsig0 + "\n") && txt.Contains("\nrules " + r0 + "\n"),
            "Replay 2: cabeçalho e os três blocos de assinatura no arquivo");
        Check(Replay.TryParse(txt, out var back, out string be) && back.Verify() == SignatureDiff.None
              && back.Final == "t30 abc #123" && back.Commands.Count == 1,
            "Replay 2: ida e volta preserva assinaturas, comando e a linha final (" + be + ")");

        Check(!Replay.TryParse("tdfende-replay 1\nseed 1\n", out _, out string old1) && old1.Contains("formato 1") && old1.Contains("regrave"),
            "Replay: cabeçalho do formato 1 é recusado com a instrução de regravar (" + old1 + ")");
        Check(!Replay.TryParse(txt.Replace("\n3 send 1", "\ncatalog 1234abcd\n3 send 1"), out _, out _),
            "Replay 2: linha desconhecida (a antiga 'catalog') continua falhando alto");

        Replay Tampered(string block)
        {
            var lines = txt.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith(block + " ")) lines[i] = block + " 0000000000000000";
            Replay.TryParse(string.Join("\n", lines), out var r, out _);
            return r;
        }
        Check(Tampered("sends").Verify() == SignatureDiff.Sends, "Verify: bloco sends adulterado é acusado");
        Check(Tampered("towers").Verify() == SignatureDiff.Towers, "Verify: bloco towers adulterado é acusado");
        Check(Tampered("rules").Verify() == SignatureDiff.Rules, "Verify: bloco rules adulterado é acusado");
        Check(Replay.TryParse(Replay.Header + "\nseed 1\n", out var bare, out _) && bare.Verify() == SignatureDiff.Missing,
            "Verify: arquivo sem assinaturas é acusado como sem assinatura");
        string described = Replay.Describe(SignatureDiff.Sends | SignatureDiff.Rules);
        Check(described.Contains("envios") && described.Contains("regras") && !described.Contains("torres"),
            "Describe: diz quais blocos diferem (" + described + ")");

        // a linha final é conferida na reprodução e só avisa
        var live2 = new MatchRunner(31, TowerWarsAi.Personality.Normal, 24, 16);
        var rec2 = new Replay { Seed = 31, Difficulty = "Normal" };
        live2.CommandApplied += rec2.Record;
        for (int t = 0; t < 600; t++) { if (t == 5) live2.Enqueue(MatchCommand.Build(8, 8, 0)); live2.Step(); }
        rec2.Ticks = live2.TickCount;
        rec2.Final = live2.StateFingerprint();
        Replay.TryParse(rec2.Serialize(), out var back2, out _);
        Check(back2.FinalMatches(back2.Run()) == true, "Replay: estado final igual ao gravado é confirmado");
        back2.Final = "t600 errado";
        Check(back2.FinalMatches(back2.Run()) == false, "Replay: estado final diferente é acusado");
        back2.Final = "";
        Check(back2.FinalMatches(back2.Run()) == null, "Replay: sem linha final não há o que conferir");
    }

    // Soma o mínimo que muda o valor, qualquer que seja o tipo do campo.
    static object Bump(System.Type t, object v)
    {
        if (t == typeof(int)) return (int)v + 1;
        if (t == typeof(float)) return (float)v + 0.5f;
        if (t == typeof(string)) return (string)v + "x";
        throw new System.InvalidOperationException("tipo de campo sem regra de teste: " + t.Name);
    }

    /// <summary>
    /// TEC-31: o fingerprint da partida precisa ENXERGAR posição, vida, torres, tiros e o sorteio. Antes só tinha
    /// contadores: duas partidas com os inimigos em lugares diferentes davam o mesmo número e "provavam" igualdade.
    /// </summary>
    static void FingerprintTests()
    {
        // --- o hash quantiza: 0,01 de deslocamento muda, ruído de 0,001 não
        ulong H(Vector3 v) { var h = new StateHash(); h.Add(v); return h.Value; }
        var p0 = new Vector3(1f, 2f, 3f);
        Check(H(p0) != H(p0 + new Vector3(0.01f, 0f, 0f)), "Fingerprint: 0,01 de deslocamento em X muda o hash");
        Check(H(p0) != H(p0 + new Vector3(0f, 0f, 0.01f)), "Fingerprint: 0,01 de deslocamento em Z muda o hash");
        Check(H(p0) == H(p0 + new Vector3(0.001f, 0f, 0f)), "Fingerprint: ruído de 0,001 não muda o hash (quantizado)");
        var a1 = new StateHash(); a1.Add(1); a1.Add(2);
        var a2 = new StateHash(); a2.Add(2); a2.Add(1);
        Check(a1.Value != a2.Value, "Fingerprint: a ordem dos campos conta");

        // --- duas lanes com os MESMOS contadores e o inimigo em lugar diferente: o hash tem que separar
        LaneSim Lane(int ticks)
        {
            var feeder = new LaneSim(24, 16); feeder.DebugGrantGold(5000);
            var lane = new LaneSim(24, 16);
            feeder.TrySend(SendCatalog.IdOf("rato"), lane, new Random(11));
            for (int i = 0; i < ticks; i++) lane.Tick(TowerWarsConfig.FixedStep);
            return lane;
        }
        var near = Lane(30); var far = Lane(31);
        Check(near.Lives == far.Lives && near.Gold == far.Gold && near.EnemiesAlive == far.EnemiesAlive
              && near.TotalLeaked == far.TotalLeaked, "Fingerprint (base): os contadores antigos são iguais nas duas lanes");
        Check(SimFingerprint.OfLane(near) != SimFingerprint.OfLane(far),
            "Fingerprint: inimigo em outro lugar muda o hash mesmo com contadores iguais");
        Check(SimFingerprint.OfLane(Lane(30)) == SimFingerprint.OfLane(near), "Fingerprint: a mesma lane duas vezes dá o mesmo hash");

        // --- torre entra no hash (tipo e nível)
        var t1 = new LaneSim(24, 16); var t2 = new LaneSim(24, 16);
        t1.DebugGrantGold(5000); t2.DebugGrantGold(5000);
        t1.TryBuildTower(new Vector2Int(8, 8), TowerCatalog.IdOf("canhao")); t2.TryBuildTower(new Vector2Int(8, 8), TowerCatalog.IdOf("morteiro"));
        Check(t1.TowerCount == t2.TowerCount, "Fingerprint (base): as duas lanes têm 1 torre");
        Check(SimFingerprint.OfLane(t1) != SimFingerprint.OfLane(t2), "Fingerprint: torre de tipo diferente muda o hash");

        // --- partida inteira: o fingerprint carrega o hash, e continua igual entre execuções iguais
        MatchRunner Play(int seed, int extraSend)
        {
            var m = new MatchRunner(seed, TowerWarsAi.Personality.Normal, 24, 16);
            for (int t = 0; t < 600; t++)
            {
                if (t % 97 == 0) m.Enqueue(MatchCommand.Build(5 + t % 9, 6));
                if (t == 300 && extraSend >= 0) m.Enqueue(MatchCommand.Send(extraSend));
                m.Step();
            }
            return m;
        }
        string fp = Play(5, -1).StateFingerprint();
        Check(System.Text.RegularExpressions.Regex.IsMatch(fp, @" #[0-9a-f]{16}$"), "Fingerprint: termina com o hash de 16 dígitos (" + fp + ")");
        Check(Play(5, -1).StateFingerprint() == fp, "Fingerprint: mesma semente e comandos dão o mesmo fingerprint");
        Check(Play(6, -1).StateFingerprint() != fp, "Fingerprint: outra semente dá outro fingerprint");

        // --- o contador de sorteios não pode alterar a sequência (senão quebra todo replay)
        var plain = new Random(1234); var counted = new CountingRandom(1234);
        bool same = true;
        for (int i = 0; i < 200; i++)
        {
            same &= plain.Next() == counted.Next();
            same &= plain.Next(10) == counted.Next(10);
            same &= plain.Next(3, 9) == counted.Next(3, 9);
            same &= plain.NextDouble() == counted.NextDouble();
        }
        Check(same, "CountingRandom: sequência idêntica à do Random puro");
        Check(counted.Draws == 800, $"CountingRandom: conta cada sorteio ({counted.Draws})");
    }

    /// <summary>Camada privada de arte (TEC-23): o caminho privado espelha o público dentro de TDFende/Privado.</summary>
    static void ArtLayerTests()
    {
        Check(ArtLayerPaths.PrivatePath("TDFende/Torres/Torre_Gelo_1") == "TDFende/Privado/Torres/Torre_Gelo_1", "camada privada: torre vira TDFende/Privado/Torres/...");
        Check(ArtLayerPaths.PrivatePath("TDFende/Bichos") == "TDFende/Privado/Bichos", "camada privada: pasta de bichos");
        Check(ArtLayerPaths.PrivatePath("TDFende/Bichos/") == "TDFende/Privado/Bichos/", "camada privada: barra final preservada");
        Check(ArtLayerPaths.PrivatePath("Art/Ground/leafy_grass_diff_2k") == "TDFende/Privado/Art/Ground/leafy_grass_diff_2k", "camada privada: Art/ também espelha");
        Check(ArtLayerPaths.PrivatePath("TDFende/Privado/Bichos/lobo") == null, "camada privada: caminho já privado não tem camada acima");
        Check(ArtLayerPaths.PrivatePath("TDFende/Privado") == null, "camada privada: a própria pasta privada não tem camada acima");
        Check(ArtLayerPaths.PrivatePath("") == null && ArtLayerPaths.PrivatePath(null) == null, "camada privada: caminho vazio ou nulo");
        Check(ArtLayerPaths.PrivatePath("TDFendeX/Torres/a") == "TDFende/Privado/TDFendeX/Torres/a", "camada privada: TDFendeX não é TDFende/");
        // regras de importação do editor valem na pasta privada igual na pública
        const string pub = "Assets/Resources/TDFende/Bichos/", priv = "Assets/_Privado/Resources/TDFende/Privado/Bichos/";
        Check(TDFende.EditorTools.ArtRoots.Under(pub + "lobo.fbx", pub), "camada privada (editor): arquivo público casa a pasta pública");
        Check(TDFende.EditorTools.ArtRoots.Under(priv + "lobo.fbx", pub), "camada privada (editor): arquivo privado casa o espelho da pasta pública");
        Check(!TDFende.EditorTools.ArtRoots.Under(priv + "lobo.fbx", "Assets/Resources/TDFende/Torres/"), "camada privada (editor): bicho privado não casa a pasta de torres");
        Check(TDFende.EditorTools.ArtRoots.Under("Assets/_Privado/Resources/TDFende/Privado/Art/Sky/c.hdr", "Assets/Resources/Art/Sky/"), "camada privada (editor): Art/Sky espelha");
        Check(!TDFende.EditorTools.ArtRoots.Under("Assets/_Privado/Resources/TDFende/Privado2/Bichos/x", pub) && !TDFende.EditorTools.ArtRoots.Under("Assets/Outra/Bichos/x", pub), "camada privada (editor): pasta parecida não casa");
        Check(ArtLayerPaths.IsPrivate("TDFende/Privado/Torres/a") && !ArtLayerPaths.IsPrivate("TDFende/Torres/a") && !ArtLayerPaths.IsPrivate("TDFende/PrivadoX/a"), "camada privada: IsPrivate");
        Check(ArtLayerPaths.PublicPath("TDFende/Privado/Torres/a") == "TDFende/Torres/a" && ArtLayerPaths.PublicPath("TDFende/Privado/Art/Sky/x") == "Art/Sky/x", "camada privada: PublicPath desfaz o espelho");
    }

    static void TowerWarsTests()
    {
        var rng = new Random(1234);

        // ---------- catálogo ----------
        Check(SendCatalog.Count >= 5, "Catálogo: tem variedade de envios");
        int flying = 0, swarm = 0;
        for (int i = 0; i < SendCatalog.Count; i++)
        {
            if (SendCatalog.Get(i).IgnoresTerritory) flying++;
            if (SendCatalog.Get(i).Count > 1) swarm++;
        }
        Check(flying >= 1, "Catálogo: existe contra-jogo da fronteira (unidade que ignora território)");
        Check(swarm >= 1, "Catálogo: existe enxame (silhueta distinta)");

        // ---------- estado inicial ----------
        var a = new LaneSim(24, 16);
        var b = new LaneSim(24, 16);
        Check(a.Gold == TowerWarsConfig.StartGold && a.Lives == TowerWarsConfig.StartLives
              && a.Income == TowerWarsConfig.BaseIncome, "Lane: estado inicial correto");

        // ---------- envio: paga aqui, nasce lá, renda sobe aqui ----------
        int goldBefore = a.Gold, incomeBefore = a.Income;
        var recruta = SendCatalog.Get(SendCatalog.IdOf("rato"));
        bool sent = a.TrySend(SendCatalog.IdOf("rato"), b, rng);
        Check(sent, "Envio: compra aceita com ouro suficiente");
        Check(a.Gold == goldBefore - recruta.Cost, "Envio: cobra o ouro de QUEM ENVIA");
        Check(a.Income == incomeBefore + recruta.IncomeBonus, "Envio: sobe a renda de QUEM ENVIA");
        Check(b.EnemiesAlive == recruta.Count, "Envio: inimigo nasce na lane do ADVERSÁRIO");
        Check(a.EnemiesAlive == 0, "Envio: não nasce nada na própria lane");

        // ---------- enxame gera vários bonecos ----------
        var c = new LaneSim(24, 16);
        var d = new LaneSim(24, 16);
        c.TrySend(SendCatalog.IdOf("rato"), d, rng);
        Check(d.EnemiesAlive == SendCatalog.Get(SendCatalog.IdOf("rato")).Count, "Envio: enxame gera Count bonecos");

        // ---------- sem ouro, sem envio ----------
        var poor = new LaneSim(24, 16);
        var poorFoe = new LaneSim(24, 16);
        while (poor.CanAfford(SendCatalog.IdOf("urso"))) poor.TrySend(SendCatalog.IdOf("urso"), poorFoe, rng);
        Check(!poor.TrySend(SendCatalog.IdOf("urso"), poorFoe, rng), "Envio: recusado sem ouro");

        // ---------- renda pinga no relógio ----------
        var inc = new LaneSim(24, 16);
        int g0 = inc.Gold;
        Advance(inc, TowerWarsConfig.IncomeTickSeconds + 0.2f);
        Check(inc.Gold == g0 + inc.Income, "Renda: pinga uma vez por tique de renda");

        // sem folga: pedir EXATAMENTE um tique tem que entregar um tique
        // (pega regressão do truncamento de passos no próprio Advance)
        var incExact = new LaneSim(24, 16);
        int gE = incExact.Gold;
        Advance(incExact, TowerWarsConfig.IncomeTickSeconds);
        Check(incExact.Gold == gE + incExact.Income, "Renda: Advance com duração exata dispara o tique");

        // ---------- vazamento sem defesa ----------
        var atk = new LaneSim(24, 16);
        var undefended = new LaneSim(24, 16);
        atk.TrySend(SendCatalog.IdOf("cachorro"), undefended, rng);
        int livesBefore = undefended.Lives;
        Advance(undefended, 30f);
        Check(undefended.Lives == livesBefore - 1, "Vazamento: inimigo sem defesa tira exatamente 1 vida");
        Check(undefended.TotalLeaked == 1, "Vazamento: contabilizado");
        Check(undefended.EnemiesAlive == 0, "Vazamento: inimigo sai da lane");

        // ---------- torre mata e paga bounty ----------
        var def = new LaneSim(24, 16);
        var sender = new LaneSim(24, 16);
        int built = 0;
        for (int x = 8; x <= 14 && built < 4; x += 2)
            if (def.TryBuildTower(new Vector2Int(x, 8))) built++;
        Check(built > 0, "Construção: torres colocadas no meio do caminho");
        int goldPre = def.Gold;
        sender.DebugGrantGold(500);
        sender.TrySend(SendCatalog.IdOf("cachorro"), def, rng);
        Advance(def, 30f);
        Check(def.KilledByTower > 0, "Torre: mata o Cachorro antes da base");
        Check(def.Gold > goldPre, "Torre: abate paga bounty para o DEFENSOR");

        // ---------- atrito mata sozinho ----------
        var attr = new LaneSim(24, 16);
        var attrFoe = new LaneSim(24, 16);
        for (int x = 6; x <= 16; x += 2) attr.TryBuildTower(new Vector2Int(x, 8));
        attrFoe.DebugGrantGold(2000);
        for (int i = 0; i < 6; i++) attrFoe.TrySend(SendCatalog.IdOf("rato"), attr, rng); // Ratos: frágeis
        Advance(attr, 60f);
        Check(attr.KilledByAttrition > 0, "Atrito: fronteira mata sem tiro nenhum");

        // ---------- voador é imune ao atrito ----------
        var fly = new LaneSim(24, 16);
        var flyFoe = new LaneSim(24, 16);
        for (int x = 6; x <= 16; x += 2) fly.TryBuildTower(new Vector2Int(x, 8));
        flyFoe.DebugGrantGold(2000);
        for (int i = 0; i < 6; i++) flyFoe.TrySend(SendCatalog.IdOf("aguia"), fly, rng); // Águia
        Advance(fly, 60f);
        Check(fly.KilledByAttrition == 0, "Contra-jogo: Águia atravessa o território sem sofrer atrito");

        // ---------- eventos que alimentam o feedback visual ----------
        // A vista só desenha explosão/tremor porque estes eventos disparam. Se pararem,
        // o jogo fica mudo e nenhum outro teste percebe.
        var evLane = new LaneSim(24, 16);
        var evFeeder = new LaneSim(24, 16);
        int evTower = 0, evAttrition = 0, evLeak = 0, evUpgrade = 0;
        var evPositions = new List<Vector3>();

        evLane.EnemyDespawned += (pos, reason) =>
        {
            evPositions.Add(pos);
            if (reason == DespawnReason.KilledByTower) evTower++;
            else if (reason == DespawnReason.KilledByAttrition) evAttrition++;
            else evLeak++;
        };
        evLane.TowerChanged += (pos, level) => { if (level > 1) evUpgrade++; };

        evFeeder.DebugGrantGold(4000);
        evFeeder.TrySend(SendCatalog.IdOf("cachorro"), evLane, rng);            // sem defesa ainda: tem que vazar
        Advance(evLane, 20f);
        Check(evLeak == 1 && evTower == 0 && evAttrition == 0,
            $"Eventos: vazamento dispara uma vez ({evLeak})");
        Check(evLane.TotalLeaked == evLeak, "Eventos: contagem de vazamento bate com o placar");

        evLane.DebugGrantGold(4000);
        for (int x = 6; x <= 16; x += 2) evLane.TryBuildTower(new Vector2Int(x, 8));
        Check(evLane.TryUpgradeCheapestTower() && evUpgrade == 1,
            "Eventos: upgrade dispara TowerChanged com nível > 1");

        for (int i = 0; i < 8; i++) evFeeder.TrySend(SendCatalog.IdOf("rato"), evLane, rng);
        Advance(evLane, 40f);
        Check(evTower > 0, $"Eventos: morte por tiro dispara ({evTower})");
        Check(evAttrition > 0, $"Eventos: morte por atrito dispara ({evAttrition})");
        Check(evTower + evAttrition == evLane.KilledByTower + evLane.KilledByAttrition,
            "Eventos: total de mortes bate com o placar");
        bool posInBounds = true;
        foreach (var p in evPositions)
        {
            var evCell = evLane.Map.WorldToCell(p);
            if (!evLane.Map.InBounds(evCell.x, evCell.y)) posInBounds = false;
        }
        Check(posInBounds, "Eventos: posição reportada cai dentro do mapa (a vista desenha ali)");

        // ---------- catálogo em arquivo: balancear sem recompilar ----------
        {
            // Testes anteriores já criaram lanes, o que TRANCA os catálogos.
            // Destrancar aqui é legítimo: é o teste da trava, não uso de produção.
            SendCatalog.ResetToDefaults();
            TowerCatalog.ResetToDefaults();

            string sendsText = CatalogJson.SerializeSends();
            string towersText = CatalogJson.SerializeTowers();

            Check(CatalogJson.TryParseSends(sendsText, out var roundSends, out string se),
                $"Catálogo: envios sobrevivem à ida e volta ({se})");
            Check(roundSends.Length == SendCatalog.Count, "Catálogo: nenhum envio se perde no arquivo");
            Check(roundSends[1].Count == SendCatalog.Get(SendCatalog.IdOf("cachorro")).Count
                  && Math.Abs(roundSends[1].AttritionScale - SendCatalog.Get(SendCatalog.IdOf("cachorro")).AttritionScale) < 0.001f
                  && roundSends[4].IgnoresTerritory == SendCatalog.Get(SendCatalog.IdOf("aguia")).IgnoresTerritory,
                "Catálogo: envios preservam quantidade, atrito e a flag de voador");

            Check(CatalogJson.TryParseTowers(towersText, out var roundTowers, out string te),
                $"Catálogo: torres sobrevivem à ida e volta ({te})");
            Check(roundTowers.Length == TowerCatalog.Count, "Catálogo: nenhuma torre se perde no arquivo");
            Check(Math.Abs(roundTowers[2].SlowFactor - TowerCatalog.Get(TowerCatalog.IdOf("gelo")).SlowFactor) < 0.001f
                  && Math.Abs(roundTowers[1].SplashRadius - TowerCatalog.Get(TowerCatalog.IdOf("morteiro")).SplashRadius) < 0.001f
                  && Math.Abs(roundTowers[3].VsFlyingMultiplier - TowerCatalog.Get(TowerCatalog.IdOf("sentinela")).VsFlyingMultiplier) < 0.001f,
                "Catálogo: torres preservam lentidão, área e bônus anti-aéreo");

            // arquivo exportado ANTES do Fogo e do Ar existirem (4 torres): carregar não
            // pode fazê-las sumir — entram com o valor de fábrica, e o que o arquivo diz vale
            TowerCatalog.ResetToDefaults();
            int factory = TowerCatalog.Count;
            Check(TowerCatalog.LoadFrom(
                    "name=Canhão;cost=30;range=3.5;cooldown=0.65;damage=12\nname=Gelo;cost=40;range=3.2;cooldown=0.9;damage=4;slow=0.55;slowsecs=1.6",
                    out string oldErr)
                  && TowerCatalog.Count == factory
                  && TowerCatalog.Get(TowerCatalog.IdOf("canhao")).Cost == 30,
                $"Catálogo: arquivo antigo não apaga torre nova ({TowerCatalog.Count} de {factory}; {oldErr})");
            TowerCatalog.ResetToDefaults();

            // decimal com PONTO sempre: numa máquina com vírgula, "2.75" viraria 275
            Check(CatalogJson.TryParseTowers(
                    "name=Teste;cost=30;range=2.5;cooldown=0.5;damage=7.25;slow=1;border=1.5",
                    out var dec, out _)
                  && Math.Abs(dec[0].Damage - 7.25f) < 0.001f && Math.Abs(dec[0].Range - 2.5f) < 0.001f,
                "Catálogo: decimal com ponto é lido igual em qualquer idioma da máquina");

            // campo ausente usa o padrão: adicionar campo novo não invalida arquivo antigo
            Check(CatalogJson.TryParseSends("name=Simples;cost=12;hp=50", out var partial, out _)
                  && partial[0].Count == 1 && Math.Abs(partial[0].AttritionScale - 1f) < 0.001f,
                "Catálogo: campo ausente cai no padrão");

            // comentário e linha em branco são ignorados
            Check(CatalogJson.TryParseSends("# comentário\n\nname=X;cost=5;hp=9\n", out var cmt, out _)
                  && cmt.Length == 1, "Catálogo: comentário e linha vazia são ignorados");

            // recusas: sem elas, um arquivo torto viraria jogo quebrado em silêncio
            Check(!CatalogJson.TryParseSends("", out _, out _), "Catálogo: arquivo vazio é recusado");
            Check(!CatalogJson.TryParseSends("name=X;cost=0;hp=9", out _, out _),
                "Catálogo: custo zero é recusado");
            Check(!CatalogJson.TryParseTowers("name=T;cost=10;cooldown=0", out _, out _),
                "Catálogo: cadência zero é recusada (divisão por zero no DPS)");
            Check(!CatalogJson.TryParseTowers("name=T;cost=10;slow=0", out _, out _),
                "Catálogo: slow=0 é recusado (pararia o inimigo para sempre)");
            Check(!CatalogJson.TryParseTowers("name=T;cost=10;slow=1.5", out _, out _),
                "Catálogo: slow>1 é recusado (aceleraria o inimigo)");

            // carregar de verdade, e a trava proteger depois que a partida começa
            Check(SendCatalog.LoadFrom(sendsText, out _), "Catálogo: LoadFrom aceita antes da 1ª partida");
            var locker = new LaneSim(24, 16); // nascer uma lane tranca os catálogos
            Check(SendCatalog.Locked && TowerCatalog.Locked, "Catálogo: primeira partida tranca");
            Check(!SendCatalog.LoadFrom(sendsText, out string lockErr) && lockErr.Length > 0,
                "Catálogo: LoadFrom recusado com partida em andamento (evita índice fora do vetor)");
            Check(locker.TowerCount == 0, "Catálogo: a lane de teste segue utilizável");

            SendCatalog.ResetToDefaults();
            TowerCatalog.ResetToDefaults();
        }

        // ---------- morte súbita: a partida SEMPRE termina ----------
        // É garantia estrutural, não ajuste: se a defesa ficar mais forte no futuro, a
        // escalada quadrática ainda a ultrapassa em tempo finito.
        var sdLane = new LaneSim(24, 16);
        Check(!sdLane.InSuddenDeath, "Morte súbita: não começa ligada");
        float scaleEarly = sdLane.SendScale;
        Advance(sdLane, TowerWarsConfig.SuddenDeathMinutes * 60f + 1f);
        Check(sdLane.InSuddenDeath, "Morte súbita: liga depois do limiar");

        float scaleAt8 = sdLane.SendScale;
        Advance(sdLane, 60f);
        float scaleAt9 = sdLane.SendScale;
        Advance(sdLane, 60f);
        float scaleAt10 = sdLane.SendScale;
        Check(scaleAt8 > scaleEarly, "Morte súbita: escala sobe depois do limiar");
        Check(scaleAt10 - scaleAt9 > scaleAt9 - scaleAt8,
            "Morte súbita: aceleração é crescente (quadrática, não linear)");

        int sdDecided = 0, sdTimedOut = 0;
        for (int s = 0; s < 20; s++)
        {
            var m = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 8800 + s);
            m.Run();
            if (m.A.Dead || m.B.Dead) sdDecided++; else sdTimedOut++;
        }
        Check(sdTimedOut == 0, $"Morte súbita: nenhuma partida bate no teto de tempo ({sdDecided}/20 decididas)");

        // ---------- tipos de torre: cada uma responde a alguma coisa ----------
        // Uma torre que não vence NENHUM envio melhor que o Canhão não precisa existir;
        // estes testes são a régua que impede o catálogo de virar enfeite.
        Check(TowerCatalog.Count >= 4, "Torres: catálogo tem variedade");

        int splashers = 0, slowers = 0, antiAir = 0;
        for (int i = 0; i < TowerCatalog.Count; i++)
        {
            var tt = TowerCatalog.Get(i);
            if (tt.SplashRadius > 0f) splashers++;
            if (tt.SlowFactor < 1f) slowers++;
            if (tt.VsFlyingMultiplier > 1f) antiAir++;
        }
        Check(splashers >= 1 && slowers >= 1 && antiAir >= 1,
            "Torres: existe resposta para enxame, para velocidade e para voador");

        // Morteiro (área) contra ENXAME tem que matar mais que o Canhão no mesmo tempo
        int cannonSwarmKills = KillsAgainst(0, towerType: 0, seconds: 12f);
        int mortarSwarmKills = KillsAgainst(0, towerType: 1, seconds: 12f);
        Check(mortarSwarmKills > cannonSwarmKills,
            $"Torres: Morteiro mata mais Rato que o Canhão ({mortarSwarmKills} vs {cannonSwarmKills})");

        // ...e contra ALVO ÚNICO (um Elefante só) o Canhão tem que ser melhor, senão o
        // Morteiro seria simplesmente superior e a escolha não existiria.
        // sends:1 é essencial — com vários, eles se aglomeram e a área acerta o grupo.
        float cannonSolo = DamageDealt(8, towerType: 0, seconds: 12f, sends: 1);
        float mortarSolo = DamageDealt(8, towerType: 1, seconds: 12f, sends: 1);
        Check(cannonSolo > mortarSolo,
            $"Torres: Canhão bate mais forte no alvo único que o Morteiro ({cannonSolo:0} vs {mortarSolo:0})");

        // Sentinela contra PLANADOR (voador) tem que superar o Canhão
        // Contra a Águia a pergunta é TEMPO ATÉ MATAR, não dano acumulado: as duas o
        // matam, então o dano bate no teto da vida dele (120) e os números empatam.
        float cannonKillTime = TimeToKill(4, towerType: 0);
        float sentryKillTime = TimeToKill(4, towerType: 3);
        Check(cannonKillTime > 0f && sentryKillTime > 0f,
            "Torres: as duas chegam a matar a Águia (medição válida)");
        Check(sentryKillTime < cannonKillTime,
            $"Torres: Sentinela derruba a Águia mais rápido que o Canhão " +
            $"({sentryKillTime:0.00}s vs {cannonKillTime:0.00}s)");

        // Gelo tem que atrasar POR CAUSA DA LENTIDÃO. Comparar contra lane vazia media o
        // desvio do labirinto, não o efeito: o teste passava mesmo com a lentidão desligada.
        // Contra um Canhão na MESMA célula, o labirinto é idêntico e só a lentidão difere.
        float iceX = DeepestXWith(towerType: 2, cell: new Vector2Int(6, 8), seconds: 4f);
        float cannonX = DeepestXWith(towerType: 0, cell: new Vector2Int(6, 8), seconds: 4f);
        Check(iceX < cannonX,
            $"Torres: Gelo atrasa mais que um Canhão na MESMA célula ({iceX:0.0} vs {cannonX:0.0})");

        // Fogo contra o GORDO (Elefante sozinho): a queima por fração da vida tem que render
        // mais que o Canhão — é a razão de o Fogo existir
        float cannonHeavy = DamageDealt(8, towerType: 0, seconds: 12f, sends: 1);
        float fireHeavy = DamageDealt(8, towerType: 4, seconds: 12f, sends: 1);
        Check(fireHeavy > cannonHeavy,
            $"Torres: Fogo machuca mais o Elefante que o Canhão ({fireHeavy:0} vs {cannonHeavy:0})");

        // ...e contra o Cachorro (vida baixa) o Canhão continua melhor: senão o Fogo seria
        // simplesmente superior
        float cannonLight = DamageDealt(1, towerType: 0, seconds: 12f, sends: 1);
        float fireLight = DamageDealt(1, towerType: 4, seconds: 12f, sends: 1);
        Check(cannonLight >= fireLight,
            $"Torres: Canhão rende mais que o Fogo no Cachorro ({cannonLight:0} vs {fireLight:0})");

        // Ar tem que atrasar POR CAUSA DO EMPURRÃO: mesma célula, mesmo labirinto
        float airX = DeepestXWith(towerType: 5, cell: new Vector2Int(6, 8), seconds: 4f);
        Check(airX < cannonX,
            $"Torres: Ar atrasa mais que um Canhão na MESMA célula ({airX:0.0} vs {cannonX:0.0})");

        // território varia por tipo: Gelo cobre mais chão que Sentinela
        var wideLane = new LaneSim(24, 16);
        var narrowLane = new LaneSim(24, 16);
        wideLane.DebugGrantGold(3000);
        narrowLane.DebugGrantGold(3000);
        wideLane.TryBuildTower(new Vector2Int(12, 8), TowerCatalog.IdOf("gelo"));   // Gelo, raio 3.25
        narrowLane.TryBuildTower(new Vector2Int(12, 8), TowerCatalog.IdOf("sentinela")); // Sentinela, raio 1.5
        Check(TerritoryCells(wideLane) > TerritoryCells(narrowLane),
            $"Torres: fronteira do Gelo é maior que a da Sentinela " +
            $"({TerritoryCells(wideLane)} vs {TerritoryCells(narrowLane)} células)");

        // custo é POR TIPO: com pouco ouro, a cara é recusada e a barata ainda cabe
        var poorType = new LaneSim(24, 16);
        poorType.TryBuildTower(new Vector2Int(8, 8), TowerCatalog.IdOf("canhao"));
        poorType.TryBuildTower(new Vector2Int(9, 8), TowerCatalog.IdOf("canhao"));
        poorType.TryBuildTower(new Vector2Int(10, 8), TowerCatalog.IdOf("canhao")); // 120 - 3x25 = 45 de ouro
        Check(!poorType.CanBuild(new Vector2Int(11, 8), TowerCatalog.IdOf("sentinela")),
            $"Torres: Sentinela (50) recusada com {poorType.Gold} de ouro");
        Check(poorType.CanBuild(new Vector2Int(11, 8), TowerCatalog.IdOf("canhao")),
            "Torres: Canhão (25) ainda cabe com o mesmo ouro");

        // ---------- replay: a partida reproduz byte a byte ----------
        // É o que transforma "achei estranho no editor" num arquivo que eu reproduzo
        // aqui e leio o estado exato. Se este teste cair, o replay virou ficção.
        var recRng = new Random(9182);
        var live = new MatchRunner(777, TowerWarsAi.Personality.Normal, 24, 16);
        var rec = new Replay { Seed = 777, Difficulty = TowerWarsAi.Personality.Normal.Name };
        live.CommandApplied += rec.Record;

        for (int t = 0; t < 9000 && !live.Over; t++)
        {
            // jogador sintético: constrói, sobe e envia em momentos irregulares. Constrói perto
            // do corredor do meio: com o repasse (quem vaza volta a correr), defesa espalhada
            // ao acaso perdia a partida em meio minuto e o replay gravava quase nada
            if (t % 47 == 0)
                live.Enqueue(MatchCommand.Build(4 + recRng.Next(14), 5 + recRng.Next(7)));
            if (t % 131 == 0)
                live.Enqueue(MatchCommand.Upgrade(4 + recRng.Next(14), 2 + recRng.Next(12)));
            if (t % 89 == 0)
                live.Enqueue(MatchCommand.Send(recRng.Next(SendCatalog.Count)));
            live.Step();
        }
        rec.Ticks = live.TickCount; // a gravação cobre exatamente o que foi jogado

        Check(rec.Commands.Count > 20, $"Replay: gravou comandos aceitos ({rec.Commands.Count})");

        var again = rec.Run();
        Check(again.StateFingerprint() == live.StateFingerprint(),
            "Replay: reprodução bate com a partida original");

        // e tem que sobreviver ao formato de texto, que é como ele chega até mim
        string text = rec.Serialize();
        Check(Replay.TryParse(text, out var parsed, out string perr), $"Replay: arquivo lê de volta ({perr})");
        Check(parsed.Commands.Count == rec.Commands.Count && parsed.Seed == rec.Seed
              && parsed.Difficulty == rec.Difficulty, "Replay: arquivo preserva semente, dificuldade e comandos");
        Check(parsed.Run().StateFingerprint() == live.StateFingerprint(),
            "Replay: partida lida do ARQUIVO reproduz o mesmo estado");

        Check(!Replay.TryParse("lixo\nqualquer", out _, out _), "Replay: cabeçalho errado é recusado");
        Check(!Replay.TryParse(Replay.Header + "\n12 voar 3", out _, out _),
            "Replay: comando desconhecido é recusado");

        // Recusas que evitam "número plausível e errado" — a pior falha para um replay,
        // porque manda a investigação atrás de um bug que não existe.
        Check(!Replay.TryParse(Replay.Header + "\ndifficulty Dificil\n", out _, out string dErr)
              && dErr.Contains("desconhecida"),
            "Replay: dificuldade sem acento é RECUSADA (não vira Normal em silêncio)");
        Check(Replay.TryParse(Replay.Header + "\ndifficulty Difícil\n", out _, out _),
            "Replay: dificuldade escrita certo é aceita");
        Check(!Replay.TryParse(Replay.Header + "\n50 send 0\n10 send 0\n", out _, out string oErr)
              && oErr.Contains("crescente"),
            "Replay: tique fora de ordem é recusado (senão o resto sumia em silêncio)");
        Check(!Replay.TryParse(Replay.Header + $"\n10 send {SendCatalog.Count}\n", out _, out _),
            "Replay: envio fora do catálogo é recusado (em vez de estourar na reprodução)");
        Check(!Replay.TryParse(Replay.Header + $"\n10 build 5 5 {TowerCatalog.Count}\n", out _, out _),
            "Replay: torre fora do catálogo é recusada");

        // as assinaturas viajam com o arquivo (BUG-05)
        Check(rec.Serialize().Contains("\nsends ") && rec.Serialize().Contains("\nrules "),
            "Replay: arquivo carrega as assinaturas dos envios, torres e regras");
        Check(parsed.Verify() == SignatureDiff.None, "Replay: assinaturas lidas batem com as regras em uso");

        // comando aplicado em fronteira de TIQUE: mesma lista, mesmo resultado, sempre
        var r1 = rec.Run().StateFingerprint();
        var r2 = rec.Run().StateFingerprint();
        Check(r1 == r2, "Replay: reproduzir duas vezes dá o mesmo estado");

        // ---------- upgrade pelo jogador (célula escolhida) ----------
        // A IA usa TryUpgradeCheapestTower; o jogador precisa escolher QUAL torre sobe,
        // senão só um lado escala a defesa e o balanceamento medido não vale.
        var pick2 = new LaneSim(24, 16);
        pick2.DebugGrantGold(5000);
        var cellA = new Vector2Int(8, 8);
        var cellB = new Vector2Int(14, 8);
        pick2.TryBuildTower(cellA);
        pick2.TryBuildTower(cellB);

        Check(pick2.UpgradeCostAt(new Vector2Int(3, 3)) == -1, "Upgrade do jogador: célula vazia devolve -1");
        Check(pick2.UpgradeCostAt(cellA) == TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), 1),
            "Upgrade do jogador: custo da célula é o do nível atual");

        int goldPre2 = pick2.Gold;
        Check(pick2.TryUpgradeTowerAt(cellA), "Upgrade do jogador: aceito na torre escolhida");
        Check(pick2.Gold == goldPre2 - TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), 1), "Upgrade do jogador: cobra o custo certo");
        Check(pick2.TowerLevel(pick2.TowerIndexAt(cellA)) == 2
              && pick2.TowerLevel(pick2.TowerIndexAt(cellB)) == 1,
            "Upgrade do jogador: sobe SÓ a torre escolhida");
        Check(!pick2.TryUpgradeTowerAt(new Vector2Int(3, 3)), "Upgrade do jogador: recusado em célula sem torre");

        while (pick2.TryUpgradeTowerAt(cellA)) { }
        Check(pick2.TowerLevel(pick2.TowerIndexAt(cellA)) == TowerWarsConfig.MaxTowerLevel,
            "Upgrade do jogador: chega ao nível máximo");
        Check(pick2.UpgradeCostAt(cellA) == 0, "Upgrade do jogador: no máximo, custo devolve 0");
        Check(!pick2.TryUpgradeTowerAt(cellA), "Upgrade do jogador: recusado no nível máximo");

        int upEvents2 = 0;
        pick2.TowerChanged += (_, __) => upEvents2++;
        pick2.TryUpgradeTowerAt(cellB);
        Check(upEvents2 == 1, "Upgrade do jogador: dispara TowerChanged (partícula e texto)");

        var poorUp = new LaneSim(24, 16);
        poorUp.TryBuildTower(new Vector2Int(9, 8));
        while (poorUp.TryUpgradeTowerAt(new Vector2Int(9, 8))) { }
        Check(poorUp.Gold < poorUp.UpgradeCostAt(new Vector2Int(9, 8)),
            "Upgrade do jogador: para quando o ouro acaba");

        // ---------- Gelo congela, Fogo queima: e crescem com o nível ----------
        {
            // sem dano de verdade no teste: só a regra do frio
            int HitsToFreeze(int level)
            {
                var lane = new LaneSim(24, 16);
                var feeder = new LaneSim(24, 16);
                lane.DebugGrantGold(20000);
                feeder.DebugGrantGold(500);
                lane.TryBuildTower(new Vector2Int(8, 6), TowerCatalog.IdOf("gelo"));
                for (int l = 1; l < level; l++) lane.TryUpgradeTowerAt(new Vector2Int(8, 6));
                feeder.TrySend(SendCatalog.IdOf("elefante"), lane, new Random(3)); // Elefante: não morre do gelo
                int shots = 0;
                lane.TowerFired += _ => shots++;
                for (int t = 0; t < 30 * 20; t++)
                {
                    lane.Tick(TowerWarsConfig.FixedStep);
                    for (int sl = 0; sl < lane.EnemySlotCount; sl++)
                        if (lane.TryGetEnemy(sl, out var e) && e.Frozen) return shots;
                }
                return -1;
            }
            int h1 = HitsToFreeze(1), h6 = HitsToFreeze(6);
            Check(h1 > 0 && h6 > 0 && h6 < h1, $"Gelo: congela, e o nível alto congela com menos tiros ({h1} vs {h6})");

            var fz = new LaneSim(24, 16);
            var fzFeeder = new LaneSim(24, 16);
            fz.DebugGrantGold(20000);
            fzFeeder.DebugGrantGold(500);
            fz.TryBuildTower(new Vector2Int(8, 6), TowerCatalog.IdOf("gelo"));
            fzFeeder.TrySend(SendCatalog.IdOf("elefante"), fz, new Random(4));
            bool sawFrozenStill = false, sawGuard = false;
            Vector3 lastPos = default;
            bool wasFrozen = false;
            for (int t = 0; t < 30 * 20; t++)
            {
                fz.Tick(TowerWarsConfig.FixedStep);
                for (int sl = 0; sl < fz.EnemySlotCount; sl++)
                {
                    if (!fz.TryGetEnemy(sl, out var e)) continue;
                    if (e.Frozen && wasFrozen && (e.Pos - lastPos).sqrMagnitude < 1e-8f) sawFrozenStill = true;
                    if (!e.Frozen && e.FreezeGuard > 0f) sawGuard = true;
                    wasFrozen = e.Frozen;
                    lastPos = e.Pos;
                }
            }
            Check(sawFrozenStill, "Gelo: congelado fica parado no lugar");
            Check(sawGuard, "Gelo: depois de descongelar fica um tempo imune");

            float BurnDamage(int level)
            {
                var lane = new LaneSim(24, 16);
                var feeder = new LaneSim(24, 16);
                lane.DebugGrantGold(20000);
                feeder.DebugGrantGold(500);
                lane.TryBuildTower(new Vector2Int(8, 6), TowerCatalog.IdOf("fogo"));
                for (int l = 1; l < level; l++) lane.TryUpgradeTowerAt(new Vector2Int(8, 6));
                feeder.TrySend(SendCatalog.IdOf("elefante"), lane, new Random(5));
                float maxBurn = 0f;
                for (int t = 0; t < 30 * 12; t++)
                {
                    lane.Tick(TowerWarsConfig.FixedStep);
                    for (int sl = 0; sl < lane.EnemySlotCount; sl++)
                        if (lane.TryGetEnemy(sl, out var e)) maxBurn = Math.Max(maxBurn, e.BurnPct);
                }
                return maxBurn;
            }
            float b1 = BurnDamage(1), b6 = BurnDamage(6);
            Check(b1 > 0f && b6 > b1 * 1.5f, $"Fogo: nível 6 queima mais forte que o 1 ({b1:0.000} vs {b6:0.000})");
            Check(b6 <= LaneSim.MaxBurnPct + 1e-4f, "Fogo: brilho da vista cobre a queima mais forte (MaxBurnPct)");
        }

        // ---------- quem passa da base volta a correr ----------
        // Não some: volta com a vida que tinha, na lane do próximo adversário (nunca na de
        // quem enviou). Num 1x1, corre de novo na mesma lane. Cada base cruzada custa uma vida.
        {
            var l0 = new LaneSim(24, 16) { Id = 0, CarryLeaks = true };
            var l1 = new LaneSim(24, 16) { Id = 1, CarryLeaks = true };
            var two = new[] { l0, l1 };
            Check(LeakRouter.NextLane(two, 1, 0) == 1 && LeakRouter.NextLane(two, 0, 1) == 0,
                "Repasse 1x1: sem outro adversário, corre de novo na mesma lane");
            var l2 = new LaneSim(24, 16) { Id = 2, CarryLeaks = true };
            var three = new[] { l0, l1, l2 };
            Check(LeakRouter.NextLane(three, 1, 0) == 2 && LeakRouter.NextLane(three, 2, 0) == 1
                  && LeakRouter.NextLane(three, 0, 1) == 2,
                "Repasse com 3: vai para o próximo adversário, pulando quem enviou");

            var leakRng = new Random(5);
            l0.DebugGrantGold(500);
            l1.DebugGrantGold(500);
            l1.TryBuildTower(new Vector2Int(10, 6)); // fere de passagem, sem matar
            l0.TrySend(SendCatalog.IdOf("elefante"), l1, leakRng);              // Elefante: aguenta a torre
            var runLanes = new[] { l0, l1 };
            float hpAtGoal = -1f;
            int livesStart = l1.Lives;
            bool reentered = false;
            float hpBack = -1f;
            int lapsBack = -1;
            for (int t = 0; t < 30 * 60 && !reentered; t++)
            {
                for (int s = 0; s < l1.EnemySlotCount; s++)
                    if (l1.TryGetEnemy(s, out var e) && e.Laps == 0) hpAtGoal = e.Hp;
                l0.Tick(TowerWarsConfig.FixedStep);
                l1.Tick(TowerWarsConfig.FixedStep);
                LeakRouter.Route(runLanes, leakRng);
                for (int s = 0; s < l1.EnemySlotCount; s++)
                    if (l1.TryGetEnemy(s, out var e) && e.Laps == 1) { reentered = true; hpBack = e.Hp; lapsBack = e.Laps; }
            }
            Check(reentered && l1.Lives == livesStart - 1, "Repasse: passou da base, custou 1 vida e voltou a correr");
            Check(hpAtGoal > 0f && hpAtGoal < 449f && Math.Abs(hpBack - hpAtGoal) < 0.01f,
                $"Repasse: volta com a vida que tinha ({hpBack:0.0} de {hpAtGoal:0.0})");
            Check(l0.EnemiesAlive == 0 && l1.EnemiesAlive == 1, "Repasse 1x1: não volta para quem enviou");

            var loose = new LaneSim(24, 16);
            var looseFeeder = new LaneSim(24, 16);
            looseFeeder.DebugGrantGold(100);
            looseFeeder.TrySend(SendCatalog.IdOf("cachorro"), loose, leakRng);
            for (int t = 0; t < 30 * 60; t++) loose.Tick(TowerWarsConfig.FixedStep);
            var drained = new System.Collections.Generic.List<LaneSim.SimEnemy>();
            loose.DrainLeaks(drained);
            Check(loose.TotalLeaked == 1 && drained.Count == 0, "Repasse: lane avulsa (sem partida) não acumula nada");
        }

        // ---------- grid em pé (modo clássico) ----------
        // Só a conversão mundo <-> célula gira: a marcha (+X do grid) desce em -Z de mundo.
        var upGrid = new GridMap(24, 16, 1f, upright: true);
        bool roundTrip = true;
        for (int x = 0; x < 24; x++)
        for (int y = 0; y < 16; y++)
            if (upGrid.WorldToCell(upGrid.CellToWorld(x, y)) != new Vector2Int(x, y)) roundTrip = false;
        Check(roundTrip, "Grid em pé: célula -> mundo -> célula volta igual");
        var upSpawn = upGrid.CellToWorld(2, 8);
        var upGoal = upGrid.CellToWorld(21, 8);
        Check(upSpawn.z > upGoal.z + 18f && Math.Abs(upSpawn.x - upGoal.x) < 0.01f,
            "Grid em pé: acampamento em cima (Z maior), base embaixo");
        Check(Math.Abs(upGrid.Extent.x - 16f) < 0.01f && Math.Abs(upGrid.Extent.z - 24f) < 0.01f,
            "Grid em pé: ocupa 16 de largura e 24 de fundo");
        var upFlow = new FlowField(upGrid);
        upFlow.Rebuild(new Vector2Int(21, 8));
        var upDir = upFlow.SampleDirection(upSpawn);
        Check(upDir.z < -0.9f, "Grid em pé: o fluxo desce em -Z");

        // ---------- venda de torre ----------
        // Devolve parte do que a torre custou (construção + upgrades), libera a célula,
        // refaz caminho e fronteira, e avisa a vista qual índice saiu.
        var sell = new LaneSim(24, 16);
        sell.DebugGrantGold(5000);
        var sA = new Vector2Int(8, 8);
        var sB = new Vector2Int(12, 5);
        var sC = new Vector2Int(15, 10);
        sell.TryBuildTower(sA, TowerCatalog.IdOf("canhao"));
        sell.TryBuildTower(sB, TowerCatalog.IdOf("gelo"));
        sell.TryBuildTower(sC, TowerCatalog.IdOf("sentinela"));
        sell.TryUpgradeTowerAt(sB);
        sell.TryUpgradeTowerAt(sB);
        int investedB = TowerCatalog.Get(TowerCatalog.IdOf("gelo")).Cost + TowerCatalog.UpgradeCost(TowerCatalog.IdOf("gelo"), 1) + TowerCatalog.UpgradeCost(TowerCatalog.IdOf("gelo"), 2);
        int expectB = (int)(investedB * TowerWarsConfig.SellRefund);
        Check(sell.SellValueAt(sB) == expectB, "Venda: vale a fração certa de construção + upgrades");
        Check(sell.SellValueAt(new Vector2Int(3, 3)) == -1, "Venda: célula vazia devolve -1");
        Check(expectB < investedB, "Venda: nunca devolve tudo");

        int soldIdx = -1, soldRefund = -1;
        sell.TowerSold += (_, idx, refund) => { soldIdx = idx; soldRefund = refund; };
        int goldPreSell = sell.Gold, versionPre = sell.TowerVersion;
        Check(sell.TrySellTowerAt(sB), "Venda: aceita em torre própria");
        Check(sell.Gold == goldPreSell + expectB, "Venda: devolve o ouro");
        Check(soldIdx == 1 && soldRefund == expectB, "Venda: evento traz o índice e o valor");
        Check(sell.TowerCount == 2 && sell.TowerIndexAt(sB) == -1, "Venda: a torre some");
        Check(sell.TowerTypeAt(sC) == 3 && sell.TowerIndexAt(sC) == 1, "Venda: as de depois descem uma casa");
        Check(!sell.Map.IsBlocked(sB) && sell.TowerVersion != versionPre, "Venda: célula liberada e território refeito");
        Check(!sell.TrySellTowerAt(sB), "Venda: recusada em célula sem torre");
        Check(sell.TryBuildTower(sB, TowerCatalog.IdOf("canhao")), "Venda: dá para construir de novo no lugar");

        // vender abre caminho: com a muralha só com uma brecha, fechar a brecha é proibido;
        // vendida uma torre do meio, surge outra passagem e a brecha pode ser fechada
        var detour = new LaneSim(24, 16);
        detour.DebugGrantGold(5000);
        for (int y = 0; y < 15; y++) detour.TryBuildTower(new Vector2Int(10, y));
        var gap = new Vector2Int(10, 15);
        bool sealedBefore = detour.CanBuild(gap);
        detour.TrySellTowerAt(new Vector2Int(10, 8));
        Check(detour.TowerCount == 14 && !sealedBefore && detour.CanBuild(gap), "Venda: o caminho se refaz na hora");

        // replay com venda: grava, relê e reproduz igual
        var sellCmd = MatchCommand.Sell(8, 8);
        var sellReplay = new Replay { Seed = 3 };
        sellReplay.Record(10, MatchCommand.Build(8, 8, 0));
        sellReplay.Record(40, sellCmd);
        Replay.TryParse(sellReplay.Serialize(), out var parsedSell, out string sellErr);
        Check(parsedSell != null && parsedSell.Commands.Count == 2 && parsedSell.Commands[1].Cmd.Kind == CommandKind.Sell
              && parsedSell.Commands[1].Cmd.X == 8, "Venda: comando atravessa o replay (" + sellErr + ")");

        // ---------- tiro visível: mira e projétil em voo ----------
        // A vista não tem como desenhar tiro nenhum sem estas duas leituras. Sem elas,
        // a torre mata mas parece desligada.
        var shootLane = new LaneSim(24, 16);
        var shootFeeder = new LaneSim(24, 16);
        shootLane.DebugGrantGold(500);
        shootLane.TryBuildTower(new Vector2Int(12, 8));
        Check(!shootLane.TryGetTowerAim(0, out _), "Tiro: torre sem alvo não tem mira");

        int firedEvents = 0;
        shootLane.TowerFired += _ => firedEvents++;
        shootFeeder.DebugGrantGold(500);
        shootFeeder.TrySend(SendCatalog.IdOf("elefante"), shootLane, rng); // Elefante: aguenta vários tiros

        bool sawAim = false, sawProjectile = false, projInBounds = true;
        for (int i = 0; i < 400; i++)
        {
            shootLane.Tick(TowerWarsConfig.FixedStep);
            if (shootLane.TryGetTowerAim(0, out _)) sawAim = true;
            for (int s = 0; s < shootLane.ProjectileSlotCount; s++)
            {
                if (!shootLane.TryGetProjectile(s, out var pp)) continue;
                sawProjectile = true;
                var pc = shootLane.Map.WorldToCell(pp);
                if (!shootLane.Map.InBounds(pc.x, pc.y)) projInBounds = false;
            }
        }
        Check(sawAim, "Tiro: torre com inimigo no alcance reporta mira");

        // A mira tem que ser ATUAL, não a do último disparo. Com a busca de alvo atrás
        // do cooldown ela só mudava a cada 0,65s e o cano apontava para o passado.
        var aimLane = new LaneSim(24, 16) { TrackAim = true };
        var aimFeeder = new LaneSim(24, 16);
        aimLane.DebugGrantGold(500);
        aimLane.TryBuildTower(new Vector2Int(12, 8));
        aimFeeder.DebugGrantGold(500);
        aimFeeder.TrySend(SendCatalog.IdOf("elefante"), aimLane, rng);

        Vector3 prevAim = default;
        bool hadPrev = false, aimMovedBetweenShots = false;
        int shots = 0;
        aimLane.TowerFired += _ => shots++;
        for (int i = 0; i < 400; i++)
        {
            int shotsBefore = shots;
            aimLane.Tick(TowerWarsConfig.FixedStep);
            if (shots != shotsBefore) { hadPrev = false; continue; } // pula o tique do tiro
            if (!aimLane.TryGetTowerAim(0, out var curAim)) { hadPrev = false; continue; }
            if (hadPrev && (curAim - prevAim).sqrMagnitude > 1e-6f) aimMovedBetweenShots = true;
            prevAim = curAim;
            hadPrev = true;
        }
        Check(aimMovedBetweenShots, "Tiro: mira acompanha o alvo ENTRE disparos (não congela até o próximo tiro)");

        // TrackAim é só custo de vista: ligar ou desligar não pode mudar a partida.
        var trackOff = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 4242).Run();
        var trackOnSim = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 4242);
        trackOnSim.A.TrackAim = true;
        trackOnSim.B.TrackAim = true;
        var trackOn = trackOnSim.Run();
        Check(trackOff.Winner == trackOn.Winner && trackOff.LivesA == trackOn.LivesA
              && trackOff.LivesB == trackOn.LivesB && Math.Abs(trackOff.Seconds - trackOn.Seconds) < 0.001f,
            "Tiro: TrackAim não altera a simulação (é só custo de vista)");
        Check(firedEvents > 0, $"Tiro: TowerFired dispara ({firedEvents}x)");
        Check(sawProjectile, "Tiro: projétil em voo é visível para a vista");
        Check(projInBounds, "Tiro: projétil desenhado cai dentro do mapa");

        // ---------- contrato de leitura da vista ----------
        // A camada Unity desenha iterando compartimentos; se esta contagem divergir,
        // aparecem inimigos fantasma na tela sem nenhum teste reclamar.
        var viewLane = new LaneSim(24, 16);
        var viewFeeder = new LaneSim(24, 16);
        viewFeeder.DebugGrantGold(3000);
        for (int i = 0; i < 5; i++) viewFeeder.TrySend(i % SendCatalog.Count, viewLane, rng);
        Advance(viewLane, 2f);

        int seen = 0;
        bool hpCoerente = true;
        for (int s = 0; s < viewLane.EnemySlotCount; s++)
        {
            if (!viewLane.TryGetEnemy(s, out var ev)) continue;
            seen++;
            if (ev.MaxHp <= 0f || ev.Hp > ev.MaxHp) hpCoerente = false;
        }
        Check(seen > 0 && hpCoerente, "Vista: todo inimigo exposto tem vida coerente");
        Check(seen == viewLane.EnemiesAlive,
            $"Vista: compartimentos ativos batem com EnemiesAlive ({seen} vs {viewLane.EnemiesAlive})");

        viewLane.TryBuildTower(new Vector2Int(9, 8));
        Check(viewLane.TowerCount == 1 && viewLane.TowerCell(0) == new Vector2Int(9, 8)
              && viewLane.TowerLevel(0) == 1, "Vista: torre exposta com célula e nível corretos");
        Check(viewLane.Map.CellToWorld(viewLane.GoalCell) == viewLane.GoalWorld,
            "Vista: GoalWorld corresponde a GoalCell");

        // ---------- upgrade de torre ----------
        var up = new LaneSim(24, 16);
        up.DebugGrantGold(5000);
        up.TryBuildTower(new Vector2Int(10, 8));
        Check(up.TotalTowerLevels == 1, "Upgrade: torre nasce no nível 1");

        int goldB4 = up.Gold;
        float dpsB4 = up.TowerDps;
        Check(up.TryUpgradeCheapestTower(), "Upgrade: aceito com ouro");
        Check(up.TotalTowerLevels == 2, "Upgrade: sobe o nível");
        Check(up.Gold == goldB4 - TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), 1), "Upgrade: cobra o custo do nível atual");
        Check(up.TowerDps > dpsB4, "Upgrade: aumenta o DPS da defesa");

        Check(TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), 3) > TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), 1),
            "Upgrade: custo cresce com o nível (torre nova segue competindo)");

        while (up.TryUpgradeCheapestTower()) { }
        Check(up.TotalTowerLevels == TowerWarsConfig.MaxTowerLevel, "Upgrade: para no nível máximo");

        var broke = new LaneSim(24, 16);
        broke.TryBuildTower(new Vector2Int(10, 8));
        // gasta até não caber mais: o custo sobe com o nível, então parar pelo custo
        // do nível 1 seria laço infinito — quem decide é a própria chamada.
        while (broke.TryUpgradeCheapestTower()) { }
        int lvlBefore = broke.TotalTowerLevels;
        Check(broke.Gold < TowerCatalog.UpgradeCost(TowerCatalog.IdOf("canhao"), broke.TotalTowerLevels),
            "Upgrade: sobrou ouro, mas menos que o próximo nível custa");
        Check(!broke.TryUpgradeCheapestTower() && broke.TotalTowerLevels == lvlBefore,
            "Upgrade: recusado sem ouro");

        // torre subida mata mais rápido que torre nível 1 — o efeito tem que aparecer na simulação
        var lvl1 = new LaneSim(24, 16);
        var lvl6 = new LaneSim(24, 16);
        var feeder = new LaneSim(24, 16);
        lvl1.TryBuildTower(new Vector2Int(12, 8));
        lvl6.DebugGrantGold(5000);
        lvl6.TryBuildTower(new Vector2Int(12, 8));
        while (lvl6.TryUpgradeCheapestTower()) { }
        feeder.DebugGrantGold(5000);
        feeder.TrySend(SendCatalog.IdOf("elefante"), lvl1, rng);   // Elefante nos dois, mesmo instante
        feeder.TrySend(SendCatalog.IdOf("elefante"), lvl6, rng);
        Advance(lvl1, 25f);
        Advance(lvl6, 25f);
        Check(lvl6.KilledByTower >= lvl1.KilledByTower && lvl6.TotalLeaked <= lvl1.TotalLeaked,
            "Upgrade: torre nível 6 segura o que a nível 1 deixa passar");

        // ---------- não dá para murar ----------
        var wall = new LaneSim(24, 16);
        wall.DebugGrantGold(100000);
        int placed = 0;
        for (int y = 0; y < wall.Map.Height; y++)
            if (wall.TryBuildTower(new Vector2Int(12, y))) placed++;
        Check(placed < wall.Map.Height, "Anti-muro: a última célula da parede é recusada");

        // ---------- determinismo ----------
        var m1 = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 777).Run();
        var m2 = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 777).Run();
        Check(m1.Winner == m2.Winner && Math.Abs(m1.Seconds - m2.Seconds) < 0.001f
              && m1.LivesA == m2.LivesA && m1.LivesB == m2.LivesB,
            "Determinismo: mesma semente devolve exatamente a mesma partida");

        var m3 = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 778).Run();
        Check(m1.Seconds != m3.Seconds || m1.LivesA != m3.LivesA,
            "Determinismo: sementes diferentes dão partidas diferentes");

        // ---------- a partida termina ----------
        int decided = 0, matches = 20;
        for (int s = 0; s < matches; s++)
            if (new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 100 + s).Run().Winner != 0)
                decided++;
        Check(decided >= matches * 3 / 4, $"Partida: decide no tempo em {decided}/{matches} sementes");

        // ---------- a partida precisa ACABAR, não expirar ----------
        // Sem escalada, o ataque nunca alcança a defesa e todo jogo bate no teto de tempo.
        int byDeath = 0, sample = 20;
        for (int s = 0; s < sample; s++)
        {
            var m = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 300 + s);
            m.Run();
            if (m.A.Dead || m.B.Dead) byDeath++;
        }
        Check(byDeath >= sample * 3 / 4,
            $"Ritmo: partida termina por morte (não por tempo) em {byDeath}/{sample}");

        // ---------- o atrito precisa PESAR ----------
        // Se a fronteira não mata, ela é enfeite e o gancho do jogo não existe.
        double attrShare = 0;
        for (int s = 0; s < sample; s++)
            attrShare += new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 400 + s)
                .Run().AttritionShare;
        attrShare /= sample;
        Check(attrShare >= 0.12,
            $"Gancho: atrito responde por parte relevante das mortes ({attrShare * 100:0.0}%, meta >= 12%)");

        // ---------- dificuldade significa alguma coisa ----------
        int hardWins = 0, n = 30;
        for (int s = 0; s < n; s++)
        {
            var r = new MatchSim(TowerWarsAi.Personality.Hard, TowerWarsAi.Personality.Easy, 500 + s).Run();
            if (r.Winner == 1) hardWins++;
        }
        Check(hardWins >= n * 2 / 3, $"IA: Difícil ganha do Fácil em {hardWins}/{n}");

        // ---------- roster precisa ser usado, não decorativo ----------
        // Um tipo dominando >60% significa que existe resposta certa e cinco enfeites.
        var mixTotals = new long[SendCatalog.Count];
        long mixAll = 0;
        for (int s = 0; s < 12; s++)
        {
            var m = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 800 + s);
            m.Run();
            for (int i = 0; i < SendCatalog.Count; i++)
            {
                mixTotals[i] += m.A.SendsByType[i] + m.B.SendsByType[i];
                mixAll += m.A.SendsByType[i] + m.B.SendsByType[i];
            }
        }
        double maxShare = 0;
        int usedTypes = 0;
        for (int i = 0; i < SendCatalog.Count; i++)
        {
            double share = mixAll == 0 ? 0 : mixTotals[i] / (double)mixAll;
            if (share > maxShare) maxShare = share;
            if (share >= 0.05) usedTypes++;
        }
        Check(maxShare <= 0.60, $"Roster: nenhum envio domina ({maxShare * 100:0}% o maior, meta <= 60%)");
        Check(usedTypes >= 4, $"Roster: {usedTypes}/6 tipos com uso >= 5% (meta >= 4)");

        // ---------- Fácil precisa ser fácil, não inútil ----------
        // Adversário que perde 100% não ensina o jogo a ninguém: não dá para ver
        // o que se fez de certo. A escada de dificuldade tem que ter degrau, não penhasco.
        int normalOverEasy = 0;
        for (int s = 0; s < n; s++)
            if (new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Easy, 600 + s).Run().Winner == 1)
                normalOverEasy++;
        float easyLossRate = normalOverEasy / (float)n;
        Check(easyLossRate >= 0.60f && easyLossRate <= 0.90f,
            $"IA: Fácil perde para Normal em {normalOverEasy}/{n} ({easyLossRate * 100:0}%, meta 60-90%)");

        // ---------- espelho não pode empatar por tempo ----------
        int mirrorTimeouts = 0;
        for (int s = 0; s < n; s++)
        {
            var m = new MatchSim(TowerWarsAi.Personality.Normal, TowerWarsAi.Personality.Normal, 700 + s);
            m.Run();
            if (!m.A.Dead && !m.B.Dead) mirrorTimeouts++;
        }
        Check(mirrorTimeouts <= n / 4,
            $"IA: Normal x Normal estoura o tempo em {mirrorTimeouts}/{n} (meta <= 25%)");

        // A IA precisa subir TODOS os tipos, não só o Canhão. Comparar preço bruto fazia
        // o upgrade de nível 1 do Canhão (20) ser o único abaixo do custo de um Canhão
        // novo (25): 995 torres de contra-jogo construídas e nenhuma subida de nível.
        int upgradesTotal = 0, levelsAboveOne = 0;
        for (int s = 0; s < 12; s++)
        {
            var m = new MatchSim(TowerWarsAi.Personality.Hard, TowerWarsAi.Personality.Normal, 9100 + s);
            m.Run();
            foreach (var lane in new[] { m.A, m.B })
            {
                upgradesTotal += lane.TotalUpgrades;
                for (int i = 0; i < lane.TowerCount; i++)
                    if (lane.TowerTypeId(i) != 0 && lane.TowerLevel(i) > 1) levelsAboveOne++;
            }
        }
        Check(upgradesTotal > 0, $"IA: sobe torres de nível ({upgradesTotal} upgrades)");
        Check(levelsAboveOne > 0,
            $"IA: sobe também torres que NÃO são Canhão ({levelsAboveOne} acima do nível 1)");
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

/// <summary>Assinaturas do catálogo e das regras de FÁBRICA (BUG-05). Muda de propósito => atualizar aqui e dizer no commit.</summary>
static class SimSignatureExpected
{
    public const string Sends = "c69ccd7da93bfd61";
    public const string Towers = "7cb8cd3f7c5ce6cd";
    public const string Rules = "567e6995bddd6d61";
}
