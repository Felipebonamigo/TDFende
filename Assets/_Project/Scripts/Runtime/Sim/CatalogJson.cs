using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TDFende
{
    /// <summary>
    /// Leitura e escrita dos catálogos em texto, para o balanceamento sair do C#.
    ///
    /// Formato deliberadamente simples — uma linha por entrada, `chave=valor` separado
    /// por `;` — em vez de JSON de verdade. Três motivos:
    ///   1. o lado headless não tem biblioteca de JSON e não vale adicionar uma;
    ///   2. `JsonUtility` do Unity não lê array na raiz nem tipo aninhado sem ginástica;
    ///   3. arquivo de balanceamento é lido e editado por HUMANO, e uma linha por
    ///      unidade compara melhor num diff do que um objeto espalhado em 12 linhas.
    ///
    /// Campo que faltar numa linha usa o valor de FÁBRICA do bicho/torre de mesmo nome (BUG-04),
    /// então adicionar um campo novo não invalida os arquivos que já existem. Sem a fábrica de
    /// referência (nome desconhecido, ou o parser chamado sem ela) valem os padrões genéricos.
    /// </summary>
    public static class CatalogJson
    {
        // ---------------- envios ----------------

        public static string SerializeSends()
        {
            var sb = new StringBuilder();
            sb.Append("# TDFende — envios. Uma linha por tipo; campo ausente usa o padrão.\n");
            sb.Append("# key= identifica o bicho (não mude); name= é só o texto exibido e o jogo ignora o que estiver aqui.\n");
            sb.Append("# custo/renda/quantidade são inteiros; o resto é decimal com PONTO.\n");
            sb.Append("# attrition=0 significa VOADOR: ignora a fronteira e o atrito.\n");
            for (int i = 0; i < SendCatalog.Count; i++)
            {
                var u = SendCatalog.Get(i);
                sb.Append($"key={u.Key}");
                sb.Append($";name={u.Name}");
                sb.Append($";cost={u.Cost}");
                sb.Append($";income={u.IncomeBonus}");
                sb.Append($";hp={F(u.Hp)}");
                sb.Append($";speed={F(u.Speed)}");
                sb.Append($";count={u.Count}");
                sb.Append($";bounty={u.Bounty}");
                // "voador" NÃO é campo próprio: é derivado de attrition=0. Emitir os dois
                // permitiria um arquivo onde eles se contradizem.
                sb.Append($";attrition={F(u.AttritionScale)}");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Lê só o texto, com os padrões genéricos nos campos que faltam (ida e volta, testes de parser).</summary>
        public static bool TryParseSends(string text, out SendUnit[] units, out string error) =>
            TryParseSends(text, null, out units, out _, out error);

        /// <summary>
        /// Lê o texto usando, para cada linha, a unidade de fábrica de mesmo nome como base dos campos
        /// que faltam. Devolve as unidades NA ORDEM DO ARQUIVO e a linha de cada uma; quem junta com a
        /// fábrica é o <see cref="CatalogMerge"/>.
        /// </summary>
        public static bool TryParseSends(string text, SendUnit[] factory, out SendUnit[] units, out int[] lineNos, out string error,
                                         List<string> notes = null)
        {
            units = null;
            lineNos = null;
            var list = new List<SendUnit>();
            var lns = new List<int>();
            foreach (var (fields, lineNo) in Lines(text))
            {
                var b = new SendUnit { Key = "", Name = "?", Cost = 10, IncomeBonus = 1, Hp = 40f, Speed = 2.2f, Count = 1, Bounty = 4, AttritionScale = 1f };
                string key = Str(fields, "key", ""), name = Str(fields, "name", "?");
                if (factory != null)
                    foreach (var f in factory)
                        if (key.Length > 0 ? f.Key == key : f.Name == name) { b = f; break; }
                // Casou com a fábrica: a chave e o nome são os dela (o nome do arquivo é só informativo).
                bool known = b.Key != "";
                Identify(notes, lineNo, key, name, known, b.Key, b.Name, ref key, ref name);
                var u = new SendUnit
                {
                    Key = key,
                    Name = name,
                    Cost = Int(fields, "cost", b.Cost),
                    IncomeBonus = Int(fields, "income", b.IncomeBonus),
                    Hp = Flt(fields, "hp", b.Hp),
                    Speed = Flt(fields, "speed", b.Speed),
                    Count = Int(fields, "count", b.Count),
                    Bounty = Int(fields, "bounty", b.Bounty),
                    AttritionScale = Flt(fields, "attrition", b.AttritionScale)
                };
                if (u.Cost <= 0) { error = $"linha {lineNo}: custo tem que ser > 0"; return false; }
                if (u.Hp <= 0f) { error = $"linha {lineNo}: vida tem que ser > 0"; return false; }
                if (u.Count <= 0) { error = $"linha {lineNo}: quantidade tem que ser > 0"; return false; }
                list.Add(u);
                lns.Add(lineNo);
            }
            if (list.Count == 0) { error = "nenhum envio no arquivo"; return false; }
            units = list.ToArray();
            lineNos = lns.ToArray();
            error = null;
            return true;
        }

        // ---------------- torres ----------------

        public static string SerializeTowers()
        {
            var sb = new StringBuilder();
            sb.Append("# TDFende — torres. Uma linha por tipo; campo ausente usa o padrão.\n");
            sb.Append("# key= identifica a torre (não mude); name= é só o texto exibido e o jogo ignora o que estiver aqui.\n");
            sb.Append("# border=0 significa que a torre não projeta fronteira.\n");
            for (int i = 0; i < TowerCatalog.Count; i++)
            {
                var t = TowerCatalog.Get(i);
                sb.Append($"key={t.Key}");
                sb.Append($";name={t.Name}");
                sb.Append($";cost={t.Cost}");
                sb.Append($";range={F(t.Range)}");
                sb.Append($";cooldown={F(t.Cooldown)}");
                sb.Append($";damage={F(t.Damage)}");
                sb.Append($";splash={F(t.SplashRadius)}");
                sb.Append($";slow={F(t.SlowFactor)}");
                sb.Append($";slowsecs={F(t.SlowSeconds)}");
                sb.Append($";vsflying={F(t.VsFlyingMultiplier)}");
                sb.Append($";border={F(t.BorderRadius)}");
                sb.Append($";burn={F(t.BurnPctPerSecond)}");
                sb.Append($";burnsecs={F(t.BurnSeconds)}");
                sb.Append($";push={F(t.Knockback)}");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Lê só o texto, com os padrões genéricos nos campos que faltam.</summary>
        public static bool TryParseTowers(string text, out TowerType[] towers, out string error) =>
            TryParseTowers(text, null, out towers, out _, out error);

        /// <summary>Como <see cref="TryParseSends(string,SendUnit[],out SendUnit[],out int[],out string)"/>, para torres.</summary>
        public static bool TryParseTowers(string text, TowerType[] factory, out TowerType[] towers, out int[] lineNos, out string error,
                                          List<string> notes = null)
        {
            towers = null;
            lineNos = null;
            var list = new List<TowerType>();
            var lns = new List<int>();
            foreach (var (fields, lineNo) in Lines(text))
            {
                var b = new TowerType { Key = "", Name = "?", Cost = 25, Range = 3.5f, Cooldown = 0.65f, Damage = 12f, SlowFactor = 1f, VsFlyingMultiplier = 1f };
                string key = Str(fields, "key", ""), name = Str(fields, "name", "?");
                if (factory != null)
                    foreach (var f in factory)
                        if (key.Length > 0 ? f.Key == key : f.Name == name) { b = f; break; }
                bool known = b.Key != "";
                Identify(notes, lineNo, key, name, known, b.Key, b.Name, ref key, ref name);
                var t = new TowerType
                {
                    Key = key,
                    Name = name,
                    Cost = Int(fields, "cost", b.Cost),
                    Range = Flt(fields, "range", b.Range),
                    Cooldown = Flt(fields, "cooldown", b.Cooldown),
                    Damage = Flt(fields, "damage", b.Damage),
                    SplashRadius = Flt(fields, "splash", b.SplashRadius),
                    SlowFactor = Flt(fields, "slow", b.SlowFactor),
                    SlowSeconds = Flt(fields, "slowsecs", b.SlowSeconds),
                    VsFlyingMultiplier = Flt(fields, "vsflying", b.VsFlyingMultiplier),
                    BorderRadius = Flt(fields, "border", b.BorderRadius),
                    BurnPctPerSecond = Flt(fields, "burn", b.BurnPctPerSecond),
                    BurnSeconds = Flt(fields, "burnsecs", b.BurnSeconds),
                    Knockback = Flt(fields, "push", b.Knockback)
                };
                if (t.Cost <= 0) { error = $"linha {lineNo}: custo tem que ser > 0"; return false; }
                if (t.Cooldown <= 0f) { error = $"linha {lineNo}: cadência tem que ser > 0"; return false; }
                if (t.Range <= 0f) { error = $"linha {lineNo}: alcance tem que ser > 0"; return false; }
                // slow=0 pararia o inimigo para sempre; slow>1 seria acelerar
                if (t.SlowFactor <= 0f || t.SlowFactor > 1f)
                { error = $"linha {lineNo}: slow tem que estar entre 0 (exclusivo) e 1"; return false; }
                // burn>=1 mataria qualquer coisa em um segundo; push grande teleporta
                if (t.BurnPctPerSecond < 0f || t.BurnPctPerSecond >= 1f || t.BurnSeconds < 0f)
                { error = $"linha {lineNo}: burn tem que estar entre 0 e 1, burnsecs >= 0"; return false; }
                if (t.Knockback < 0f || t.Knockback > 3f)
                { error = $"linha {lineNo}: push tem que estar entre 0 e 3"; return false; }
                list.Add(t);
                lns.Add(lineNo);
            }
            if (list.Count == 0) { error = "nenhuma torre no arquivo"; return false; }
            towers = list.ToArray();
            lineNos = lns.ToArray();
            error = null;
            return true;
        }

        // ---------------- utilidades ----------------

        /// <summary>
        /// Decide chave e nome de uma linha. Casou com a fábrica: vale a chave e o nome DELA, e uma nota diz o que o
        /// arquivo tinha de diferente (linha sem key=; name= diferente do nome do jogo). Não casou: o que o arquivo
        /// disse, para o merge recusar com a mensagem certa.
        /// </summary>
        static void Identify(List<string> notes, int lineNo, string fileKey, string fileName, bool known,
                             string factoryKey, string factoryName, ref string key, ref string name)
        {
            if (!known) return;
            if (fileKey.Length == 0)
                notes?.Add($"linha {lineNo}: sem chave (key=), casado pelo nome \"{fileName}\"; exporte o balanceamento de novo");
            else if (fileName != "?" && fileName != factoryName)
                notes?.Add($"linha {lineNo}: name=\"{fileName}\" ignorado; o jogo chama \"{factoryName}\" (chave {fileKey})");
            key = factoryKey;
            name = factoryName;
        }

        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        static IEnumerable<(Dictionary<string, string>, int)> Lines(string text)
        {
            if (text == null) yield break;
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                var fields = new Dictionary<string, string>();
                foreach (var pair in line.Split(';'))
                {
                    int eq = pair.IndexOf('=');
                    if (eq <= 0) continue;
                    fields[pair.Substring(0, eq).Trim().ToLowerInvariant()] = pair.Substring(eq + 1).Trim();
                }
                yield return (fields, i + 1);
            }
        }

        static string Str(Dictionary<string, string> f, string k, string def) =>
            f.TryGetValue(k, out var v) && v.Length > 0 ? v : def;

        static int Int(Dictionary<string, string> f, string k, int def) =>
            f.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int r) ? r : def;

        // InvariantCulture explícito: numa máquina com vírgula decimal, "2.75" viraria
        // 275 em silêncio e o balanceamento inteiro mudaria sem ninguém perceber.
        static float Flt(Dictionary<string, string> f, string k, float def) =>
            f.TryGetValue(k, out var v) && float.TryParse(v, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float r) ? r : def;
    }
}
