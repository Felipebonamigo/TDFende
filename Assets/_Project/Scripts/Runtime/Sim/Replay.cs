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
    /// <summary>Quais blocos de regras diferem entre a gravação e o jogo atual (BUG-05).</summary>
    [System.Flags]
    public enum SignatureDiff
    {
        None = 0,
        Sends = 1,
        Towers = 2,
        Rules = 4,
        /// <summary>O arquivo não traz as três assinaturas: não dá para saber se os números batem.</summary>
        Missing = 8
    }

    public class Replay
    {
        /// <summary>
        /// Formato 2 (BUG-05): assinaturas completas em três blocos (sends, towers, rules) e a linha opcional final.
        /// O formato 1 tinha uma assinatura de 32 bits que não via queima, empurrão, nomes, atrito nem escalada;
        /// é recusado de propósito, e basta regravar.
        /// </summary>
        public const string Header = "tdfende-replay 2";
        const string OldHeader1 = "tdfende-replay 1";

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

        /// <summary>Assinaturas gravadas (16 hexa cada, ver <see cref="SimSignature"/>). Vazio = ausente no arquivo.</summary>
        public string SendsSignature = "", TowersSignature = "", RulesSignature = "";

        /// <summary>
        /// Fingerprint do estado final na gravação (opcional). Só diagnóstico: pega mudança de LÓGICA que a assinatura
        /// de dados não vê e mede se o float reproduz entre runtimes (TEC-27). Nunca recusa a reprodução.
        /// </summary>
        public string Final = "";

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
            sb.Append("sends ").Append(SimSignature.Sends()).Append('\n');
            sb.Append("towers ").Append(SimSignature.Towers()).Append('\n');
            sb.Append("rules ").Append(SimSignature.Rules()).Append('\n');
            if (!string.IsNullOrEmpty(Final)) sb.Append("final ").Append(Final).Append('\n');
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
            if (lines.Length > 0 && lines[0].Trim() == OldHeader1)
            {
                error = "gravação do formato 1 (anterior à assinatura completa): regrave a partida";
                return false;
            }
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

                    case "sends":
                    case "towers":
                    case "rules":
                        if (p.Length < 2 || p[1].Length != 16)
                        { error = $"linha {i + 1}: assinatura \"{p[0]}\" ausente ou fora do formato (16 dígitos hexa)"; return false; }
                        if (p[0] == "sends") replay.SendsSignature = p[1];
                        else if (p[0] == "towers") replay.TowersSignature = p[1];
                        else replay.RulesSignature = p[1];
                        break;

                    case "final":
                        // o resto da linha: o fingerprint tem espaços
                        replay.Final = line.Length > 6 ? line.Substring(6).Trim() : "";
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
                case "sell":
                    if (p.Length < 4 || !int.TryParse(p[2], out int x) || !int.TryParse(p[3], out int y))
                        return false;
                    if (p[1] == "upgrade") { cmd = MatchCommand.Upgrade(x, y); return true; }
                    if (p[1] == "sell") { cmd = MatchCommand.Sell(x, y); return true; }
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
        /// Compara as assinaturas gravadas com as regras em uso. Um balanceamento editado, um atrito ou uma
        /// escalada diferente mudam a partida inteira; sem esta conferência a reprodução usaria números diferentes
        /// dos da gravação e reportaria um desfecho que nunca aconteceu.
        /// </summary>
        public SignatureDiff Verify()
        {
            if (SendsSignature.Length == 0 || TowersSignature.Length == 0 || RulesSignature.Length == 0)
                return SignatureDiff.Missing;
            var d = SignatureDiff.None;
            if (SendsSignature != SimSignature.Sends()) d |= SignatureDiff.Sends;
            if (TowersSignature != SimSignature.Towers()) d |= SignatureDiff.Towers;
            if (RulesSignature != SimSignature.Rules()) d |= SignatureDiff.Rules;
            return d;
        }

        /// <summary>Texto para o aviso: o que mudou desde a gravação.</summary>
        public static string Describe(SignatureDiff d)
        {
            if (d == SignatureDiff.None) return "regras iguais às da gravação";
            if ((d & SignatureDiff.Missing) != 0)
                return "a gravação não traz as assinaturas (sends, towers, rules); não dá para saber se os números batem";
            var parts = new List<string>();
            if ((d & SignatureDiff.Sends) != 0) parts.Add("os envios (bichos)");
            if ((d & SignatureDiff.Towers) != 0) parts.Add("as torres");
            if ((d & SignatureDiff.Rules) != 0) parts.Add("as regras (economia, atrito, escalada, IA ou versão da Sim)");
            return "mudaram desde a gravação: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Confere o estado final da reprodução com o gravado. null = a gravação não traz a linha final.
        /// Só diagnóstico: quem chama avisa, nunca recusa.
        /// </summary>
        public bool? FinalMatches(MatchRunner runner) =>
            string.IsNullOrEmpty(Final) ? (bool?)null : runner.StateFingerprint() == Final;

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
