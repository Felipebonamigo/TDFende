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
    /// Campo que faltar usa o padrão do código, então adicionar um campo novo não
    /// invalida os arquivos que já existem.
    /// </summary>
    public static class CatalogJson
    {
        // ---------------- envios ----------------

        public static string SerializeSends()
        {
            var sb = new StringBuilder();
            sb.Append("# TDFende — envios. Uma linha por tipo; campo ausente usa o padrão.\n");
            sb.Append("# custo/renda/quantidade são inteiros; o resto é decimal com PONTO.\n");
            sb.Append("# attrition=0 significa VOADOR: ignora a fronteira e o atrito.\n");
            for (int i = 0; i < SendCatalog.Count; i++)
            {
                var u = SendCatalog.Get(i);
                sb.Append($"name={u.Name}");
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

        public static bool TryParseSends(string text, out SendUnit[] units, out string error)
        {
            units = null;
            var list = new List<SendUnit>();
            foreach (var (fields, lineNo) in Lines(text))
            {
                var u = new SendUnit
                {
                    Name = Str(fields, "name", "?"),
                    Cost = Int(fields, "cost", 10),
                    IncomeBonus = Int(fields, "income", 1),
                    Hp = Flt(fields, "hp", 40f),
                    Speed = Flt(fields, "speed", 2.2f),
                    Count = Int(fields, "count", 1),
                    Bounty = Int(fields, "bounty", 4),
                    AttritionScale = Flt(fields, "attrition", 1f)
                };
                if (u.Cost <= 0) { error = $"linha {lineNo}: custo tem que ser > 0"; return false; }
                if (u.Hp <= 0f) { error = $"linha {lineNo}: vida tem que ser > 0"; return false; }
                if (u.Count <= 0) { error = $"linha {lineNo}: quantidade tem que ser > 0"; return false; }
                list.Add(u);
            }
            if (list.Count == 0) { error = "nenhum envio no arquivo"; return false; }
            units = list.ToArray();
            error = null;
            return true;
        }

        // ---------------- torres ----------------

        public static string SerializeTowers()
        {
            var sb = new StringBuilder();
            sb.Append("# TDFende — torres. Uma linha por tipo; campo ausente usa o padrão.\n");
            sb.Append("# border=0 significa que a torre não projeta fronteira.\n");
            for (int i = 0; i < TowerCatalog.Count; i++)
            {
                var t = TowerCatalog.Get(i);
                sb.Append($"name={t.Name}");
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

        public static bool TryParseTowers(string text, out TowerType[] towers, out string error)
        {
            towers = null;
            var list = new List<TowerType>();
            foreach (var (fields, lineNo) in Lines(text))
            {
                var t = new TowerType
                {
                    Name = Str(fields, "name", "?"),
                    Cost = Int(fields, "cost", 25),
                    Range = Flt(fields, "range", 3.5f),
                    Cooldown = Flt(fields, "cooldown", 0.65f),
                    Damage = Flt(fields, "damage", 12f),
                    SplashRadius = Flt(fields, "splash", 0f),
                    SlowFactor = Flt(fields, "slow", 1f),
                    SlowSeconds = Flt(fields, "slowsecs", 0f),
                    VsFlyingMultiplier = Flt(fields, "vsflying", 1f),
                    BorderRadius = Flt(fields, "border", 0f),
                    BurnPctPerSecond = Flt(fields, "burn", 0f),
                    BurnSeconds = Flt(fields, "burnsecs", 0f),
                    Knockback = Flt(fields, "push", 0f)
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
            }
            if (list.Count == 0) { error = "nenhuma torre no arquivo"; return false; }
            towers = list.ToArray();
            error = null;
            return true;
        }

        // ---------------- utilidades ----------------

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
