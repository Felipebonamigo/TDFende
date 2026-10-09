// Auditoria de licenças da arte (TEC-22). Veja AuditaLicencas.csproj e docs/MANUAL.md (seção 7).
//
//   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --git      (o que está no índice do git)
//   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --disco    (o que está no disco, inclusive ignorado pelo git)
//   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --gera     (escreve THIRD_PARTY.md e creditos.txt)
//   dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --autoteste
//
// Código de saída: 0 = passou (pode ter avisos de "pendente"), 1 = reprovou, 2 = uso errado.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

sealed class Manifesto
{
    public int Versao { get; set; }
    public List<string> Raizes { get; set; } = new List<string>();
    public List<string> Ignorar { get; set; } = new List<string>();
    public List<string> Cabecalho { get; set; } = new List<string>();
    public List<Entrada> Entradas { get; set; } = new List<Entrada>();
}

sealed class Entrada
{
    public string Id { get; set; }
    public string Grupo { get; set; }
    public string Titulo { get; set; }
    public List<string> Caminhos { get; set; } = new List<string>();
    /// <summary>Arquivos que os caminhos pegariam mas que pertencem a outra entrada.</summary>
    public List<string> Excecoes { get; set; } = new List<string>();
    public string Origem { get; set; }
    public string Autor { get; set; }
    public string Licenca { get; set; }
    public string Link { get; set; }
    public string Derivacao { get; set; }
    /// <summary>propria (gerado por nós com IA), terceiro (gerado por IA por outra pessoa) ou nao.</summary>
    public string Ia { get; set; }
    /// <summary>Só para origem Meshy: pago, gratis, desconhecido, comunidade.</summary>
    public string PlanoMeshy { get; set; }
    /// <summary>false = o arquivo não pode ir para o git público (Mixamo, lojas).</summary>
    public bool RepoPublico { get; set; } = true;
    /// <summary>obrigatorio, cortesia ou nenhum.</summary>
    public string Credito { get; set; } = "nenhum";
    public string CreditoTexto { get; set; }
    /// <summary>ok ou pendente (pendente só avisa).</summary>
    public string Estado { get; set; } = "ok";
    public string Pendencia { get; set; }
    public string Nota { get; set; }

    [JsonIgnore] public List<Regex> Padroes { get; set; } = new List<Regex>();
    [JsonIgnore] public List<Regex> PadroesExcecao { get; set; } = new List<Regex>();
    public bool Casa(string arquivo) => Padroes.Any(r => r.IsMatch(arquivo)) && !PadroesExcecao.Any(r => r.IsMatch(arquivo));
    [JsonIgnore] public int Arquivos { get; set; }
}

static class Program
{
    const string ManifestoRel = "docs/licencas/manifesto.json";
    const string MitRel = "docs/licencas/MIT-O3DE.txt";
    const string ThirdPartyRel = "THIRD_PARTY.md";
    const string CreditosRel = "Assets/Resources/TDFende/creditos.txt";

    sealed class RegraLicenca
    {
        public bool CreditoObrigatorio;
        public bool ExigeRepoPrivado;
    }

    // Licenças que o projeto aceita, e o que cada uma exige. O que não está aqui reprova: obriga
    // alguém a olhar a licença nova antes de ela entrar.
    static readonly Dictionary<string, RegraLicenca> Licencas = new Dictionary<string, RegraLicenca>(StringComparer.OrdinalIgnoreCase)
    {
        ["CC0"] = new RegraLicenca(),
        ["CC-BY-4.0"] = new RegraLicenca { CreditoObrigatorio = true },
        ["MIT"] = new RegraLicenca { CreditoObrigatorio = true },
        ["Apache-2.0"] = new RegraLicenca { CreditoObrigatorio = true },
        ["Propria"] = new RegraLicenca(),
        // modelo gerado no Meshy pela conta do Felipe: no plano pago é dele; no grátis é CC BY 4.0
        ["Meshy-conta-Felipe"] = new RegraLicenca { CreditoObrigatorio = true },
        ["Adobe-Mixamo"] = new RegraLicenca { ExigeRepoPrivado = true },
        ["EULA-loja"] = new RegraLicenca { ExigeRepoPrivado = true },
    };

    static readonly string[] IaValidos = { "propria", "terceiro", "nao" };
    static readonly string[] PlanosMeshy = { "pago", "gratis", "desconhecido", "comunidade" };
    static readonly string[] Creditos = { "obrigatorio", "cortesia", "nenhum" };
    static readonly Regex ProibidaNcNd = new Regex(@"(^|[^A-Za-z])(NC|ND)([^A-Za-z]|$)|personal|pessoal", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        bool git = args.Contains("--git"), disco = args.Contains("--disco"), gera = args.Contains("--gera");
        if (args.Contains("--autoteste")) return AutoTeste();
        if (!git && !disco && !gera)
        {
            Console.WriteLine("uso: --git | --disco | --gera | --autoteste   (opcional: --raiz <pasta>)");
            return 2;
        }
        int ir = Array.IndexOf(args, "--raiz");
        string raiz = ir >= 0 && ir + 1 < args.Length ? Path.GetFullPath(args[ir + 1]) : AcharRaiz();
        if (raiz == null) { Console.WriteLine("[licencas] não achei docs/licencas/manifesto.json (rode dentro do repositório ou use --raiz)."); return 2; }

        var erros = new List<string>();
        var avisos = new List<string>();
        Manifesto m;
        try
        {
            m = JsonSerializer.Deserialize<Manifesto>(File.ReadAllText(Path.Combine(raiz, ManifestoRel)), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
            });
        }
        catch (Exception e)
        {
            Console.WriteLine("[licencas] manifesto ilegível: " + e.Message);
            return 1;
        }

        Validar(m, erros);

        if (gera)
        {
            if (erros.Count > 0) return Relatar(erros, avisos, 0, m);
            Escrever(raiz, ThirdPartyRel, GerarThirdParty(m, raiz));
            Escrever(raiz, CreditosRel, GerarCreditos(m));
            Console.WriteLine($"[licencas] escrevi {ThirdPartyRel} e {CreditosRel} ({m.Entradas.Count} entradas).");
            return 0;
        }

        List<string> arquivos;
        try { arquivos = git ? ArquivosDoGit(raiz, m) : ArquivosDoDisco(raiz, m); }
        catch (Exception e) { Console.WriteLine("[licencas] não consegui listar os arquivos: " + e.Message); return 1; }

        var ignorar = m.Ignorar.Select(Glob).ToList();
        int conferidos = 0;
        foreach (var f in arquivos)
        {
            if (f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue; // o Unity gera
            if (ignorar.Any(r => r.IsMatch(f))) continue;
            conferidos++;
            var acertos = m.Entradas.Where(e => e.Casa(f)).ToList();
            if (acertos.Count == 0) { erros.Add($"sem entrada no manifesto: {f}"); continue; }
            if (acertos.Count > 1) { erros.Add($"ambíguo ({string.Join(" e ", acertos.Select(a => a.Id))}): {f}"); continue; }
            var e = acertos[0];
            e.Arquivos++;
            if (git && !e.RepoPublico)
                erros.Add($"não pode ir para o git público ({e.Licenca}, entrada {e.Id}): {f}");
        }

        foreach (var e in m.Entradas)
        {
            if (e.Estado == "pendente" && e.Arquivos > 0)
                avisos.Add($"PENDENTE {e.Id} ({e.Arquivos} arquivos): {e.Pendencia}");
        }
        var vazias = m.Entradas.Where(e => e.Arquivos == 0).Select(e => e.Id).ToList();
        if (vazias.Count > 0) avisos.Add($"entradas sem arquivo nesta máquina ({vazias.Count}, normal para o que só existe no PC): {string.Join(", ", vazias)}");

        // THIRD_PARTY.md e creditos.txt têm que refletir o manifesto
        if (erros.Count == 0)
        {
            ConferirGerado(raiz, ThirdPartyRel, GerarThirdParty(m, raiz), erros);
            ConferirGerado(raiz, CreditosRel, GerarCreditos(m), erros);
        }
        return Relatar(erros, avisos, conferidos, m);
    }

    // ------------------------------------------------------------------ validação

    static void Validar(Manifesto m, List<string> erros)
    {
        if (m.Versao != 1) erros.Add("manifesto: versao deve ser 1");
        if (m.Raizes.Count == 0) erros.Add("manifesto: sem raizes");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in m.Entradas)
        {
            string o = $"entrada '{e.Id}'";
            if (string.IsNullOrWhiteSpace(e.Id)) { erros.Add("entrada sem id"); continue; }
            if (!ids.Add(e.Id)) erros.Add($"{o}: id repetido");
            foreach (var (campo, valor) in new[] { ("titulo", e.Titulo), ("origem", e.Origem), ("autor", e.Autor), ("licenca", e.Licenca), ("derivacao", e.Derivacao), ("ia", e.Ia) })
                if (string.IsNullOrWhiteSpace(valor)) erros.Add($"{o}: falta {campo}");
            if (e.Caminhos.Count == 0) erros.Add($"{o}: sem caminhos");
            try { e.Padroes = e.Caminhos.Select(Glob).ToList(); e.PadroesExcecao = e.Excecoes.Select(Glob).ToList(); }
            catch (Exception ex) { erros.Add($"{o}: caminho inválido ({ex.Message})"); }
            foreach (var c in e.Caminhos)
                if (!m.Raizes.Any(r => c.StartsWith(r.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)))
                    erros.Add($"{o}: caminho fora das raízes do manifesto: {c}");

            if (e.Licenca != null)
            {
                if (ProibidaNcNd.IsMatch(e.Licenca)) { erros.Add($"{o}: licença proibida ({e.Licenca}): NC, ND ou \"uso pessoal\" não servem para um jogo à venda"); continue; }
                if (!Licencas.TryGetValue(e.Licenca, out var regra))
                {
                    erros.Add($"{o}: licença desconhecida '{e.Licenca}' (aceitas: {string.Join(", ", Licencas.Keys)}). Confira os termos e acrescente em Program.cs.");
                    continue;
                }
                if (regra.ExigeRepoPrivado && e.RepoPublico) erros.Add($"{o}: {e.Licenca} proíbe redistribuir; repoPublico tem que ser false");
                if (regra.CreditoObrigatorio && e.Credito != "obrigatorio") erros.Add($"{o}: {e.Licenca} exige crédito; credito tem que ser \"obrigatorio\"");
            }
            if (e.Ia != null && !IaValidos.Contains(e.Ia)) erros.Add($"{o}: ia deve ser {string.Join(", ", IaValidos)}");
            if (!Creditos.Contains(e.Credito)) erros.Add($"{o}: credito deve ser {string.Join(", ", Creditos)}");
            if (e.Credito == "obrigatorio" && string.IsNullOrWhiteSpace(e.CreditoTexto)) erros.Add($"{o}: credito obrigatório sem creditoTexto");
            if (e.Estado != "ok" && e.Estado != "pendente") erros.Add($"{o}: estado deve ser ok ou pendente");
            if (e.Estado == "pendente" && string.IsNullOrWhiteSpace(e.Pendencia)) erros.Add($"{o}: estado pendente sem pendencia (o que falta conferir)");
            bool meshy = (e.Origem ?? "").IndexOf("meshy", StringComparison.OrdinalIgnoreCase) >= 0;
            if (meshy)
            {
                if (e.PlanoMeshy == null || !PlanosMeshy.Contains(e.PlanoMeshy)) erros.Add($"{o}: origem Meshy pede planoMeshy ({string.Join(", ", PlanosMeshy)})");
                else if (e.PlanoMeshy == "desconhecido" && e.Estado != "pendente") erros.Add($"{o}: plano Meshy desconhecido só pode ficar como pendente");
                if (e.Ia == "nao") erros.Add($"{o}: modelo do Meshy é gerado por IA; ia não pode ser \"nao\"");
            }
        }
    }

    // ------------------------------------------------------------------ arquivos

    static List<string> ArquivosDoGit(string raiz, Manifesto m)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = raiz, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add("ls-files"); psi.ArgumentList.Add("-z"); psi.ArgumentList.Add("--");
        foreach (var r in m.Raizes) psi.ArgumentList.Add(r);
        using var p = Process.Start(psi);
        string saida = p.StandardOutput.ReadToEnd();
        string erro = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new Exception("git ls-files: " + erro.Trim());
        return saida.Split('\0', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Replace('\\', '/')).ToList();
    }

    static List<string> ArquivosDoDisco(string raiz, Manifesto m)
    {
        var lista = new List<string>();
        foreach (var r in m.Raizes)
        {
            string dir = Path.Combine(raiz, r);
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                lista.Add(Path.GetRelativePath(raiz, f).Replace('\\', '/'));
        }
        return lista;
    }

    static string AcharRaiz()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var d = new DirectoryInfo(inicio);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, ManifestoRel))) return d.FullName;
                d = d.Parent;
            }
        }
        return null;
    }

    /// <summary>Padrão de caminho: * dentro de uma pasta, ** atravessa pastas, ? um caractere. Sem diferenciar maiúsculas (Windows).</summary>
    public static Regex Glob(string g)
    {
        g = g.Replace('\\', '/');
        var sb = new StringBuilder("^");
        for (int i = 0; i < g.Length; i++)
        {
            char c = g[i];
            if (c == '*')
            {
                if (i + 1 < g.Length && g[i + 1] == '*')
                {
                    i++;
                    if (i + 1 < g.Length && g[i + 1] == '/') { i++; sb.Append("(?:.*/)?"); }
                    else sb.Append(".*");
                }
                else sb.Append("[^/]*");
            }
            else if (c == '?') sb.Append("[^/]");
            else sb.Append(Regex.Escape(c.ToString()));
        }
        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    // ------------------------------------------------------------------ saída

    static int Relatar(List<string> erros, List<string> avisos, int conferidos, Manifesto m)
    {
        foreach (var a in avisos) Console.WriteLine("[licencas] aviso: " + a);
        if (erros.Count > 0)
        {
            Console.WriteLine($"[licencas] REPROVOU ({erros.Count}):");
            foreach (var e in erros.Take(40)) Console.WriteLine("  - " + e);
            if (erros.Count > 40) Console.WriteLine($"  ... e mais {erros.Count - 40}");
            Console.WriteLine("[licencas] conserte docs/licencas/manifesto.json (MANUAL, seção 7) e rode --gera se o texto de créditos mudou.");
            return 1;
        }
        Console.WriteLine($"[licencas] passou: {conferidos} arquivos, {m.Entradas.Count} entradas, {avisos.Count(a => a.StartsWith("PENDENTE"))} pendentes.");
        return 0;
    }

    static void Escrever(string raiz, string rel, string texto)
    {
        string caminho = Path.Combine(raiz, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho));
        File.WriteAllText(caminho, texto, new UTF8Encoding(false));
    }

    static void ConferirGerado(string raiz, string rel, string esperado, List<string> erros)
    {
        string caminho = Path.Combine(raiz, rel);
        string atual = File.Exists(caminho) ? File.ReadAllText(caminho).Replace("\r\n", "\n") : null;
        if (atual != esperado.Replace("\r\n", "\n"))
            erros.Add($"{rel} está desatualizado em relação ao manifesto: rode  dotnet run --project Tools/AuditaLicencas -v quiet --nologo -- --gera");
    }

    // ------------------------------------------------------------------ geradores

    static string Md(string s) => (s ?? "").Replace("|", "/").Replace("\n", " ");

    static string GerarThirdParty(Manifesto m, string raiz)
    {
        var sb = new StringBuilder();
        sb.Append("# Conteúdo de terceiros\n\n");
        sb.Append("<!-- GERADO por Tools/AuditaLicencas (--gera) a partir de docs/licencas/manifesto.json. Não edite à mão: mude o manifesto. -->\n\n");
        foreach (var p in m.Cabecalho) sb.Append(p).Append("\n\n");

        sb.Append("**Legenda.** *IA*: `própria` = gerado por nós com IA; `terceiro` = gerado por IA por outra pessoa; `não` = sem declaração de IA. ");
        sb.Append("*Estado*: `pendente` = falta conferir algo antes de publicar (veja a nota do grupo).\n\n");

        foreach (var grupo in m.Entradas.GroupBy(e => e.Grupo ?? "Outros"))
        {
            var itens = grupo.ToList();
            bool meshy = itens.Any(e => e.PlanoMeshy != null);
            sb.Append("## ").Append(grupo.Key).Append("\n\n");
            sb.Append(meshy ? "| Obra | Autor | Licença | IA | Plano Meshy | Estado |\n|---|---|---|---|---|---|\n"
                            : "| Obra | Autor | Licença | IA | Estado |\n|---|---|---|---|---|\n");
            foreach (var e in itens)
            {
                string obra = string.IsNullOrWhiteSpace(e.Link) ? Md(e.Titulo) : $"[{Md(e.Titulo)}]({e.Link})";
                string ia = e.Ia == "nao" ? "não" : e.Ia == "propria" ? "própria" : e.Ia;
                sb.Append($"| {obra} | {Md(e.Autor)} | {Md(e.Licenca)} | {ia} | ");
                if (meshy) sb.Append(Md(e.PlanoMeshy ?? "—")).Append(" | ");
                sb.Append(e.Estado).Append(" |\n");
            }
            sb.Append('\n');
            foreach (var par in itens.Where(e => !string.IsNullOrWhiteSpace(e.Derivacao) || !string.IsNullOrWhiteSpace(e.Nota) || e.Estado == "pendente")
                                     .GroupBy(e => (e.Derivacao ?? "") + "\u0001" + (e.Nota ?? "") + "\u0001" + (e.Pendencia ?? "")))
            {
                var primeira = par.First();
                sb.Append("- **").Append(par.Count() > 4 ? $"{par.Count()} obras da tabela acima" : string.Join(", ", par.Select(e => Md(e.Titulo)))).Append("**");
                if (!string.IsNullOrWhiteSpace(primeira.Origem)) sb.Append(". Origem: ").Append(primeira.Origem.TrimEnd('.'));
                if (!string.IsNullOrWhiteSpace(primeira.Derivacao)) sb.Append(". Alterações nossas: ").Append(primeira.Derivacao.TrimEnd('.'));
                sb.Append('.');
                if (!string.IsNullOrWhiteSpace(primeira.Nota)) sb.Append(' ').Append(primeira.Nota.Trim());
                if (primeira.Estado == "pendente") sb.Append(" **Pendente:** ").Append(primeira.Pendencia.Trim());
                sb.Append('\n');
            }
            sb.Append("\n<details><summary>Arquivos deste grupo</summary>\n\n");
            foreach (var e in itens) sb.Append("- `").Append(e.Id).Append("`: ").Append(string.Join(", ", e.Caminhos.Select(c => "`" + c + "`"))).Append('\n');
            sb.Append("\n</details>\n\n");
        }

        string mitPath = Path.Combine(raiz, MitRel);
        if (File.Exists(mitPath))
        {
            sb.Append("**Ao publicar o jogo** (Steam), o aviso MIT abaixo precisa ir junto — nos créditos ou num arquivo de licenças que acompanha o executável.\n\n");
            sb.Append("## Open 3D Engine — licença MIT\n\n```\n").Append(File.ReadAllText(mitPath).Replace("\r\n", "\n").Trim()).Append("\n```\n");
        }
        return sb.ToString();
    }

    static string GerarCreditos(Manifesto m)
    {
        var sb = new StringBuilder();
        sb.Append("TDFende — créditos de arte de terceiros\n");
        sb.Append("(gerado por Tools/AuditaLicencas a partir de docs/licencas/manifesto.json; não edite à mão)\n\n");
        sb.Append("== Créditos exigidos pela licença ==\n\n");
        var vistos = new HashSet<string>();
        foreach (var e in m.Entradas.Where(e => e.Credito == "obrigatorio"))
            if (vistos.Add(e.CreditoTexto.Trim())) sb.Append(e.CreditoTexto.Trim()).Append('\n');
        sb.Append("\n== Agradecimentos (a licença é CC0 e não exige crédito) ==\n\n");
        vistos.Clear();
        foreach (var e in m.Entradas.Where(e => e.Credito == "cortesia"))
        {
            string linha = !string.IsNullOrWhiteSpace(e.CreditoTexto) ? e.CreditoTexto.Trim() : $"\"{e.Titulo}\" — {e.Autor}";
            if (vistos.Add(linha)) sb.Append(linha).Append('\n');
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ autoteste

    static int AutoTeste()
    {
        int falhas = 0;
        void Confere(bool ok, string nome) { if (!ok) { Console.WriteLine("FALHOU: " + nome); falhas++; } }
        Confere(Glob("Assets/Resources/A/*.fbx").IsMatch("Assets/Resources/A/x.fbx"), "* casa arquivo");
        Confere(!Glob("Assets/Resources/A/*.fbx").IsMatch("Assets/Resources/A/B/x.fbx"), "* não atravessa pasta");
        Confere(Glob("Assets/Resources/A/**").IsMatch("Assets/Resources/A/B/C/x.fbx"), "** atravessa pastas");
        Confere(Glob("Assets/Resources/**/LEIA-ME.txt").IsMatch("Assets/Resources/LEIA-ME.txt"), "**/ casa zero pastas");
        Confere(Glob("Assets/Resources/**/LEIA-ME.txt").IsMatch("Assets/Resources/X/Y/LEIA-ME.txt"), "**/ casa várias pastas");
        Confere(Glob("Assets/Resources/Torre_*").IsMatch("assets/resources/torre_x"), "sem diferenciar maiúsculas");
        var ex = new Entrada { Padroes = { Glob("A/**") }, PadroesExcecao = { Glob("A/LEIA-ME.txt") } };
        Confere(ex.Casa("A/x.fbx") && !ex.Casa("A/LEIA-ME.txt"), "exceção tira o arquivo da entrada");
        Confere(!Glob("Assets/Resources/a.b").IsMatch("Assets/Resources/aXb"), "ponto é literal");
        Confere(ProibidaNcNd.IsMatch("CC-BY-NC-4.0"), "NC proibido");
        Confere(ProibidaNcNd.IsMatch("CC BY-ND"), "ND proibido");
        Confere(ProibidaNcNd.IsMatch("personal use only"), "uso pessoal proibido");
        Confere(!ProibidaNcNd.IsMatch("CC-BY-4.0"), "CC-BY liberado");
        Confere(!ProibidaNcNd.IsMatch("Propria"), "Propria liberada");
        Confere(!ProibidaNcNd.IsMatch("Adobe-Mixamo"), "Adobe-Mixamo liberada");

        // manifesto errado precisa reprovar pelos motivos certos
        var m = new Manifesto { Versao = 1, Raizes = { "Assets/Resources" } };
        m.Entradas.Add(new Entrada { Id = "a", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "x", Autor = "y", Licenca = "CC-BY-NC-4.0", Derivacao = "n", Ia = "nao" });
        m.Entradas.Add(new Entrada { Id = "b", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "Meshy", Autor = "y", Licenca = "Meshy-conta-Felipe", Derivacao = "n", Ia = "nao", Credito = "obrigatorio", CreditoTexto = "c" });
        m.Entradas.Add(new Entrada { Id = "c", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "x", Autor = "y", Licenca = "CC-BY-4.0", Derivacao = "n", Ia = "nao" });
        m.Entradas.Add(new Entrada { Id = "d", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "x", Autor = "y", Licenca = "Adobe-Mixamo", Derivacao = "n", Ia = "nao" });
        m.Entradas.Add(new Entrada { Id = "e", Titulo = "t", Caminhos = { "Fora/**" }, Origem = "x", Autor = "y", Licenca = "CC0", Derivacao = "n", Ia = "nao" });
        m.Entradas.Add(new Entrada { Id = "f", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "x", Autor = "y", Licenca = "Qualquer", Derivacao = "n", Ia = "nao" });
        var erros = new List<string>();
        Validar(m, erros);
        string todos = string.Join("\n", erros);
        Confere(todos.Contains("entrada 'a': licença proibida"), "NC reprova");
        Confere(todos.Contains("entrada 'b': origem Meshy pede planoMeshy"), "Meshy sem plano reprova");
        Confere(todos.Contains("entrada 'b': modelo do Meshy é gerado por IA"), "Meshy com ia=nao reprova");
        Confere(todos.Contains("entrada 'c': CC-BY-4.0 exige crédito"), "CC-BY sem crédito reprova");
        Confere(todos.Contains("entrada 'd': Adobe-Mixamo proíbe redistribuir"), "Mixamo público reprova");
        Confere(todos.Contains("entrada 'e': caminho fora das raízes"), "caminho fora das raízes reprova");
        Confere(todos.Contains("entrada 'f': licença desconhecida"), "licença desconhecida reprova");

        var ok = new Manifesto { Versao = 1, Raizes = { "Assets/Resources" } };
        ok.Entradas.Add(new Entrada { Id = "ok", Titulo = "t", Caminhos = { "Assets/Resources/**" }, Origem = "Meshy", Autor = "y", Licenca = "Meshy-conta-Felipe", Derivacao = "n", Ia = "propria", PlanoMeshy = "desconhecido", Credito = "obrigatorio", CreditoTexto = "c", Estado = "pendente", Pendencia = "p" });
        erros.Clear();
        Validar(ok, erros);
        Confere(erros.Count == 0, "manifesto correto passa (erros: " + string.Join("; ", erros) + ")");
        ok.Entradas[0].Estado = "ok";
        erros.Clear();
        Validar(ok, erros);
        Confere(erros.Any(x => x.Contains("plano Meshy desconhecido só pode ficar como pendente")), "plano desconhecido não pode ser ok");

        Console.WriteLine(falhas == 0 ? "[licencas] autoteste: tudo certo." : $"[licencas] autoteste: {falhas} falha(s).");
        return falhas == 0 ? 0 : 1;
    }
}
