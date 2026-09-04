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
            return RunReplay(args[1]);

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
            feeder.TrySend(2, lane, new Random(5)); // Corredor: rápido, sente a lentidão
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

    static int RunReplay(string path)
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

        // Catálogo diferente = outros números = OUTRA partida. Sem este aviso, o relatório
        // sairia plausível e errado, e a investigação perseguiria um bug que não existe.
        string nowSig = Replay.CurrentCatalogSignature();
        if (replay.CatalogSignature.Length == 0)
            Console.WriteLine("  AVISO: gravação sem assinatura de catálogo (arquivo antigo); " +
                              "não dá para saber se os números batem.");
        else if (replay.CatalogSignature != nowSig)
        {
            Console.WriteLine($"  ERRO: catálogo diferente do da gravação " +
                              $"(arquivo {replay.CatalogSignature}, atual {nowSig}).");
            Console.WriteLine("  A reprodução usaria outros números e daria um desfecho que " +
                              "nunca aconteceu. Restaure o balanceamento da gravação e rode de novo.");
            return 3;
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
        var recruta = SendCatalog.Get(0);
        bool sent = a.TrySend(0, b, rng);
        Check(sent, "Envio: compra aceita com ouro suficiente");
        Check(a.Gold == goldBefore - recruta.Cost, "Envio: cobra o ouro de QUEM ENVIA");
        Check(a.Income == incomeBefore + recruta.IncomeBonus, "Envio: sobe a renda de QUEM ENVIA");
        Check(b.EnemiesAlive == recruta.Count, "Envio: inimigo nasce na lane do ADVERSÁRIO");
        Check(a.EnemiesAlive == 0, "Envio: não nasce nada na própria lane");

        // ---------- enxame gera vários bonecos ----------
        var c = new LaneSim(24, 16);
        var d = new LaneSim(24, 16);
        c.TrySend(1, d, rng);
        Check(d.EnemiesAlive == SendCatalog.Get(1).Count, "Envio: enxame gera Count bonecos");

        // ---------- sem ouro, sem envio ----------
        var poor = new LaneSim(24, 16);
        var poorFoe = new LaneSim(24, 16);
        while (poor.CanAfford(5)) poor.TrySend(5, poorFoe, rng);
        Check(!poor.TrySend(5, poorFoe, rng), "Envio: recusado sem ouro");

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
        atk.TrySend(0, undefended, rng);
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
        sender.TrySend(0, def, rng);
        Advance(def, 30f);
        Check(def.KilledByTower > 0, "Torre: mata o Recruta antes da base");
        Check(def.Gold > goldPre, "Torre: abate paga bounty para o DEFENSOR");

        // ---------- atrito mata sozinho ----------
        var attr = new LaneSim(24, 16);
        var attrFoe = new LaneSim(24, 16);
        for (int x = 6; x <= 16; x += 2) attr.TryBuildTower(new Vector2Int(x, 8));
        attrFoe.DebugGrantGold(2000);
        for (int i = 0; i < 6; i++) attrFoe.TrySend(1, attr, rng); // Enxame: frágil
        Advance(attr, 60f);
        Check(attr.KilledByAttrition > 0, "Atrito: fronteira mata sem tiro nenhum");

        // ---------- voador é imune ao atrito ----------
        var fly = new LaneSim(24, 16);
        var flyFoe = new LaneSim(24, 16);
        for (int x = 6; x <= 16; x += 2) fly.TryBuildTower(new Vector2Int(x, 8));
        flyFoe.DebugGrantGold(2000);
        for (int i = 0; i < 6; i++) flyFoe.TrySend(4, fly, rng); // Planador
        Advance(fly, 60f);
        Check(fly.KilledByAttrition == 0, "Contra-jogo: Planador atravessa o território sem sofrer atrito");

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
        evFeeder.TrySend(0, evLane, rng);            // sem defesa ainda: tem que vazar
        Advance(evLane, 20f);
        Check(evLeak == 1 && evTower == 0 && evAttrition == 0,
            $"Eventos: vazamento dispara uma vez ({evLeak})");
        Check(evLane.TotalLeaked == evLeak, "Eventos: contagem de vazamento bate com o placar");

        evLane.DebugGrantGold(4000);
        for (int x = 6; x <= 16; x += 2) evLane.TryBuildTower(new Vector2Int(x, 8));
        Check(evLane.TryUpgradeCheapestTower() && evUpgrade == 1,
            "Eventos: upgrade dispara TowerChanged com nível > 1");

        for (int i = 0; i < 8; i++) evFeeder.TrySend(1, evLane, rng);
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
            Check(roundSends[1].Count == SendCatalog.Get(1).Count
                  && Math.Abs(roundSends[1].AttritionScale - SendCatalog.Get(1).AttritionScale) < 0.001f
                  && roundSends[4].IgnoresTerritory == SendCatalog.Get(4).IgnoresTerritory,
                "Catálogo: envios preservam quantidade, atrito e a flag de voador");

            Check(CatalogJson.TryParseTowers(towersText, out var roundTowers, out string te),
                $"Catálogo: torres sobrevivem à ida e volta ({te})");
            Check(roundTowers.Length == TowerCatalog.Count, "Catálogo: nenhuma torre se perde no arquivo");
            Check(Math.Abs(roundTowers[2].SlowFactor - TowerCatalog.Get(2).SlowFactor) < 0.001f
                  && Math.Abs(roundTowers[1].SplashRadius - TowerCatalog.Get(1).SplashRadius) < 0.001f
                  && Math.Abs(roundTowers[3].VsFlyingMultiplier - TowerCatalog.Get(3).VsFlyingMultiplier) < 0.001f,
                "Catálogo: torres preservam lentidão, área e bônus anti-aéreo");

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
        int cannonSwarmKills = KillsAgainst(1, towerType: 0, seconds: 12f);
        int mortarSwarmKills = KillsAgainst(1, towerType: 1, seconds: 12f);
        Check(mortarSwarmKills > cannonSwarmKills,
            $"Torres: Morteiro mata mais Enxame que o Canhão ({mortarSwarmKills} vs {cannonSwarmKills})");

        // ...e contra ALVO ÚNICO (um Colosso só) o Canhão tem que ser melhor, senão o
        // Morteiro seria simplesmente superior e a escolha não existiria.
        // sends:1 é essencial — com vários, eles se aglomeram e a área acerta o grupo.
        float cannonSolo = DamageDealt(5, towerType: 0, seconds: 12f, sends: 1);
        float mortarSolo = DamageDealt(5, towerType: 1, seconds: 12f, sends: 1);
        Check(cannonSolo > mortarSolo,
            $"Torres: Canhão bate mais forte no alvo único que o Morteiro ({cannonSolo:0} vs {mortarSolo:0})");

        // Sentinela contra PLANADOR (voador) tem que superar o Canhão
        // Contra o Planador a pergunta é TEMPO ATÉ MATAR, não dano acumulado: as duas o
        // matam, então o dano bate no teto da vida dele (120) e os números empatam.
        float cannonKillTime = TimeToKill(4, towerType: 0);
        float sentryKillTime = TimeToKill(4, towerType: 3);
        Check(cannonKillTime > 0f && sentryKillTime > 0f,
            "Torres: as duas chegam a matar o Planador (medição válida)");
        Check(sentryKillTime < cannonKillTime,
            $"Torres: Sentinela derruba o Planador mais rápido que o Canhão " +
            $"({sentryKillTime:0.00}s vs {cannonKillTime:0.00}s)");

        // Gelo tem que atrasar POR CAUSA DA LENTIDÃO. Comparar contra lane vazia media o
        // desvio do labirinto, não o efeito: o teste passava mesmo com a lentidão desligada.
        // Contra um Canhão na MESMA célula, o labirinto é idêntico e só a lentidão difere.
        float iceX = DeepestXWith(towerType: 2, cell: new Vector2Int(6, 8), seconds: 4f);
        float cannonX = DeepestXWith(towerType: 0, cell: new Vector2Int(6, 8), seconds: 4f);
        Check(iceX < cannonX,
            $"Torres: Gelo atrasa mais que um Canhão na MESMA célula ({iceX:0.0} vs {cannonX:0.0})");

        // território varia por tipo: Gelo cobre mais chão que Sentinela
        var wideLane = new LaneSim(24, 16);
        var narrowLane = new LaneSim(24, 16);
        wideLane.DebugGrantGold(3000);
        narrowLane.DebugGrantGold(3000);
        wideLane.TryBuildTower(new Vector2Int(12, 8), 2);   // Gelo, raio 3.25
        narrowLane.TryBuildTower(new Vector2Int(12, 8), 3); // Sentinela, raio 1.5
        Check(TerritoryCells(wideLane) > TerritoryCells(narrowLane),
            $"Torres: fronteira do Gelo é maior que a da Sentinela " +
            $"({TerritoryCells(wideLane)} vs {TerritoryCells(narrowLane)} células)");

        // custo é POR TIPO: com pouco ouro, a cara é recusada e a barata ainda cabe
        var poorType = new LaneSim(24, 16);
        poorType.TryBuildTower(new Vector2Int(8, 8), 0);
        poorType.TryBuildTower(new Vector2Int(9, 8), 0);
        poorType.TryBuildTower(new Vector2Int(10, 8), 0); // 120 - 3x25 = 45 de ouro
        Check(!poorType.CanBuild(new Vector2Int(11, 8), 3),
            $"Torres: Sentinela (50) recusada com {poorType.Gold} de ouro");
        Check(poorType.CanBuild(new Vector2Int(11, 8), 0),
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
            // jogador sintético: constrói, sobe e envia em momentos irregulares
            if (t % 47 == 0)
                live.Enqueue(MatchCommand.Build(4 + recRng.Next(14), 2 + recRng.Next(12)));
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

        // assinatura do catálogo viaja com o arquivo
        Check(rec.Serialize().Contains("catalog "), "Replay: arquivo carrega a assinatura do catálogo");
        Check(parsed.CatalogSignature == Replay.CurrentCatalogSignature(),
            "Replay: assinatura lida bate com o catálogo em uso");

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
        Check(pick2.UpgradeCostAt(cellA) == TowerCatalog.UpgradeCost(0, 1),
            "Upgrade do jogador: custo da célula é o do nível atual");

        int goldPre2 = pick2.Gold;
        Check(pick2.TryUpgradeTowerAt(cellA), "Upgrade do jogador: aceito na torre escolhida");
        Check(pick2.Gold == goldPre2 - TowerCatalog.UpgradeCost(0, 1), "Upgrade do jogador: cobra o custo certo");
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
        shootFeeder.TrySend(5, shootLane, rng); // Colosso: aguenta vários tiros

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
        aimFeeder.TrySend(5, aimLane, rng);

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
        Check(up.Gold == goldB4 - TowerCatalog.UpgradeCost(0, 1), "Upgrade: cobra o custo do nível atual");
        Check(up.TowerDps > dpsB4, "Upgrade: aumenta o DPS da defesa");

        Check(TowerCatalog.UpgradeCost(0, 3) > TowerCatalog.UpgradeCost(0, 1),
            "Upgrade: custo cresce com o nível (torre nova segue competindo)");

        while (up.TryUpgradeCheapestTower()) { }
        Check(up.TotalTowerLevels == TowerWarsConfig.MaxTowerLevel, "Upgrade: para no nível máximo");

        var broke = new LaneSim(24, 16);
        broke.TryBuildTower(new Vector2Int(10, 8));
        // gasta até não caber mais: o custo sobe com o nível, então parar pelo custo
        // do nível 1 seria laço infinito — quem decide é a própria chamada.
        while (broke.TryUpgradeCheapestTower()) { }
        int lvlBefore = broke.TotalTowerLevels;
        Check(broke.Gold < TowerCatalog.UpgradeCost(0, broke.TotalTowerLevels),
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
        feeder.TrySend(5, lvl1, rng);   // Colosso nos dois, mesmo instante
        feeder.TrySend(5, lvl6, rng);
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
