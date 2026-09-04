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
                        if (p.Length < 2) { error = $"linha {i + 1}: dificuldade ausente"; return false; }
                        replay.Difficulty = p[1];
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
                    cmd = p[1] == "build" ? MatchCommand.Build(x, y) : MatchCommand.Upgrade(x, y);
                    return true;

                case "send":
                    if (p.Length < 3 || !int.TryParse(p[2], out int id)) return false;
                    cmd = MatchCommand.Send(id);
                    return true;

                default:
                    return false;
            }
        }

        public static TowerWarsAi.Personality PersonalityFromName(string name)
        {
            switch (name)
            {
                case "Fácil": return TowerWarsAi.Personality.Easy;
                case "Difícil": return TowerWarsAi.Personality.Hard;
                default: return TowerWarsAi.Personality.Normal;
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
                while (next < Commands.Count && Commands[next].Tick == runner.TickCount)
                    runner.Enqueue(Commands[next++].Cmd);
                runner.Step();
            }
            return runner;
        }

        public int LastTick => Commands.Count == 0 ? 0 : Commands[Commands.Count - 1].Tick;
    }
}
