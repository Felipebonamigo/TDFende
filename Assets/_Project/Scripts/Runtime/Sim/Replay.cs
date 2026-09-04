using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TDFende
{
    /// <summary>
    /// Gravação de uma partida: semente, dificuldade e cada comando com o tique
    /// em que valeu. Como a simulação é determinística, isso basta para reconstruir
    /// a partida inteira — nenhum estado de mundo é gravado.
    ///
    /// Serve a dois donos: o Felipe aperta uma tecla e me manda o arquivo em vez de
    /// descrever o bug em prosa, e eu reproduzo headless e leio o estado exato.
    ///
    /// Formato de texto simples de propósito: legível a olho, diffável, e sem
    /// depender de nenhuma biblioteca de JSON no lado headless.
    /// </summary>
    public class Replay
    {
        public const string Header = "tdfende-replay 1";

        public int Seed;
        public string Difficulty = "Normal";
        public int Width = GameConfig.GridWidth;
        public int Height = GameConfig.GridHeight;

        /// <summary>
        /// Quantos tiques a gravação cobre. Sem isto, uma partida salva no meio seria
        /// reproduzida ATÉ O FIM e divergiria do que foi gravado — foi exatamente assim
        /// que o teste de reprodução pegou o defeito.
        /// </summary>
        public int Ticks;

        /// <summary>Assinatura dos catálogos na gravação. Vazio = arquivo anterior a este campo.</summary>
        public string CatalogSignature = "";
        public readonly List<(int Tick, MatchCommand Cmd)> Commands = new List<(int, MatchCommand)>();

        public void Record(int tick, MatchCommand cmd) => Commands.Add((tick, cmd));

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            sb.Append("seed ").Append(Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("difficulty ").Append(Difficulty).Append('\n');
            sb.Append("grid ").Append(Width).Append(' ').Append(Height).Append('\n');
            sb.Append("ticks ").Append(Ticks.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("catalog ").Append(CurrentCatalogSignature()).Append('\n');
            foreach (var (tick, cmd) in Commands)
                sb.Append(tick.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(cmd).Append('\n');
            return sb.ToString();
        }

        public static bool TryParse(string text, out Replay replay, out string error)
        {
            replay = new Replay();
            error = null;
            if (string.IsNullOrWhiteSpace(text)) { error = "arquivo vazio"; return false; }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || lines[0].Trim() != Header)
            {
                error = $"cabeçalho inesperado (esperava \"{Header}\")";
                return false;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                var p = line.Split(' ');

                switch (p[0])
                {
                    case "seed":
                        if (p.Length < 2 || !int.TryParse(p[1], out replay.Seed))
                        { error = $"linha {i + 1}: seed inválida"; return false; }
                        break;

                    case "difficulty":
                        // Recusar nome desconhecido em vez de cair no Normal em silêncio:
                        // uma partida gravada no Difícil replicada como Normal é OUTRA
                        // partida, com IA, vidas e mortes diferentes — e o relatório ainda
                        // imprimiria "Difícil". Basta o acento se perder no caminho.
                        if (p.Length < 2 || !IsKnownDifficulty(p[1]))
                        {
                            error = $"linha {i + 1}: dificuldade desconhecida " +
                                    $"\"{(p.Length > 1 ? p[1] : "")}\" (esperava Fácil, Normal ou Difícil)";
                            return false;
                        }
                        replay.Difficulty = p[1];
                        break;

                    case "catalog":
                        if (p.Length < 2) { error = $"linha {i + 1}: assinatura de catálogo ausente"; return false; }
                        replay.CatalogSignature = p[1];
                        break;

                    case "ticks":
                        if (p.Length < 2 || !int.TryParse(p[1], out replay.Ticks))
                        { error = $"linha {i + 1}: ticks inválido"; return false; }
                        break;

                    case "grid":
                        if (p.Length < 3 || !int.TryParse(p[1], out replay.Width)
                            || !int.TryParse(p[2], out replay.Height))
                        { error = $"linha {i + 1}: grid inválido"; return false; }
                        break;

                    default:
                        if (!TryParseCommand(p, out int tick, out var cmd))
                        { error = $"linha {i + 1}: comando não reconhecido \"{line}\""; return false; }

                        // Fora de ordem, a reprodução travaria o cursor e descartaria TODO
                        // o resto em silêncio, reportando o desfecho de meia partida.
                        if (replay.Commands.Count > 0 && tick < replay.Commands[replay.Commands.Count - 1].Tick)
                        {
                            error = $"linha {i + 1}: tique {tick} vem depois de " +
                                    $"{replay.Commands[replay.Commands.Count - 1].Tick} (a lista tem que ser crescente)";
                            return false;
                        }
                        if (cmd.Kind == CommandKind.Send
                            && (cmd.SendId < 0 || cmd.SendId >= SendCatalog.Count))
                        {
                            error = $"linha {i + 1}: envio {cmd.SendId} não existe " +
                                    $"(catálogo atual tem {SendCatalog.Count})";
                            return false;
                        }
                        if (cmd.Kind == CommandKind.Build
                            && (cmd.TowerType < 0 || cmd.TowerType >= TowerCatalog.Count))
                        {
                            error = $"linha {i + 1}: torre {cmd.TowerType} não existe " +
                                    $"(catálogo atual tem {TowerCatalog.Count})";
                            return false;
                        }
                        replay.Commands.Add((tick, cmd));
                        break;
                }
            }
            return true;
        }

        static bool TryParseCommand(string[] p, out int tick, out MatchCommand cmd)
        {
            cmd = default;
            tick = 0;
            if (p.Length < 2 || !int.TryParse(p[0], out tick)) return false;

            switch (p[1])
            {
                case "build":
                case "upgrade":
                    if (p.Length < 4 || !int.TryParse(p[2], out int x) || !int.TryParse(p[3], out int y))
                        return false;
                    if (p[1] == "upgrade") { cmd = MatchCommand.Upgrade(x, y); return true; }
                    // tipo é opcional: gravação anterior aos tipos de torre continua legível
                    int type = 0;
                    if (p.Length >= 5 && !int.TryParse(p[4], out type)) return false;
                    cmd = MatchCommand.Build(x, y, type);
                    return true;

                case "send":
                    if (p.Length < 3 || !int.TryParse(p[2], out int id)) return false;
                    cmd = MatchCommand.Send(id);
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsKnownDifficulty(string name) =>
            name == TowerWarsAi.Personality.Easy.Name
            || name == TowerWarsAi.Personality.Normal.Name
            || name == TowerWarsAi.Personality.Hard.Name;

        public static TowerWarsAi.Personality PersonalityFromName(string name)
        {
            if (name == TowerWarsAi.Personality.Easy.Name) return TowerWarsAi.Personality.Easy;
            if (name == TowerWarsAi.Personality.Hard.Name) return TowerWarsAi.Personality.Hard;
            return TowerWarsAi.Personality.Normal;
        }

        /// <summary>
        /// Impressão digital dos catálogos em uso. Um arquivo de balanceamento editado
        /// muda a partida inteira, e sem isto a reprodução usaria números diferentes dos
        /// da gravação e reportaria um desfecho que nunca aconteceu.
        /// </summary>
        public static string CurrentCatalogSignature()
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < SendCatalog.Count; i++)
                {
                    var u = SendCatalog.Get(i);
                    h = h * 31 + u.Cost;
                    h = h * 31 + u.IncomeBonus;
                    h = h * 31 + u.Count;
                    h = h * 31 + u.Bounty;
                    h = h * 31 + u.Hp.GetHashCode();
                    h = h * 31 + u.Speed.GetHashCode();
                    h = h * 31 + u.AttritionScale.GetHashCode();
                }
                for (int i = 0; i < TowerCatalog.Count; i++)
                {
                    var t = TowerCatalog.Get(i);
                    h = h * 31 + t.Cost;
                    h = h * 31 + t.Range.GetHashCode();
                    h = h * 31 + t.Cooldown.GetHashCode();
                    h = h * 31 + t.Damage.GetHashCode();
                    h = h * 31 + t.SplashRadius.GetHashCode();
                    h = h * 31 + t.SlowFactor.GetHashCode();
                    h = h * 31 + t.SlowSeconds.GetHashCode();
                    h = h * 31 + t.VsFlyingMultiplier.GetHashCode();
                    h = h * 31 + t.BorderRadius.GetHashCode();
                }
                return h.ToString("x8", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Reconstrói a partida e devolve o runner no estado final.
        /// Cada comando é reenfileirado no MESMO tique em que foi aceito na gravação —
        /// é isso que faz a reprodução ser exata em vez de aproximada.
        /// </summary>
        public MatchRunner Run()
        {
            var runner = new MatchRunner(Seed, PersonalityFromName(Difficulty), Width, Height);
            // Para no tique gravado. Se a gravação não trouxer o total (arquivo antigo),
            // roda até a partida decidir, com o teto de 15 min da própria regra.
            int stop = Ticks > 0 ? Ticks : (int)(TowerWarsConfig.MatchTimeLimit / TowerWarsConfig.FixedStep) + 1;
            int next = 0;
            for (int t = 0; t < stop && !runner.Over; t++)
            {
                // <= e não ==: com == , um tique já passado travaria o cursor para sempre
                // e o resto da partida seria descartado sem um aviso sequer.
                while (next < Commands.Count && Commands[next].Tick <= runner.TickCount)
                    runner.Enqueue(Commands[next++].Cmd);
                runner.Step();
            }
            return runner;
        }

        public int LastTick => Commands.Count == 0 ? 0 : Commands[Commands.Count - 1].Tick;
    }
}
