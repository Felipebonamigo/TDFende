// AuditaAssets (TEC-10a): orçamento de malha e textura da arte, sem abrir o Unity.
//
//   dotnet run --project Tools/AuditaAssets -v quiet --nologo -- --git       (arquivos do índice do git)
//   dotnet run --project Tools/AuditaAssets -v quiet --nologo -- --disco      (todos os arquivos do disco)
//   ... --detalhe   lista cada arquivo acima do teto (sem isso, só os piores de cada tipo)
//   ... --estrito   acima do teto passa a reprovar (hoje só avisa; o TEC-06 manda ligar quando a arte assentar)
//   ... --autoteste
//
// Tetos: MANUAL, seção 4 ("Orçamento de desempenho"). Os de malha são os mesmos do SmokeCapture.
// Código de saída: 0 = ok (avisos não reprovam), 1 = reprovou em modo estrito ou erro, 2 = uso errado.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static class Program
{
    static readonly string[] Raizes = { "Assets/Resources", "Assets/_Privado", "Assets/StreamingAssets" };

    // Teto de triângulos por modelo. Primeiro padrão que casar vale.
    static readonly (Regex padrao, int teto, string nome)[] TetosMalha =
    {
        (R(@"/Torres/Fortaleza\.fbx$"), 40_000, "fortaleza"),
        (R(@"/Torres/Acampamento\.fbx$"), 25_000, "acampamento (provisório: o MANUAL não define)"),
        (R(@"/Torres/Torre_[^/]*\.fbx$"), 25_000, "torre"),
        (R(@"/Bichos/[^/]*\.fbx$"), 15_000, "bicho"),
    };
    const int TetoTexturaPx = 2048; // provisório: o MANUAL ainda não define teto de textura

    static Regex R(string p) => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (args.Contains("--autoteste")) return AutoTeste();
        bool git = args.Contains("--git"), disco = args.Contains("--disco");
        bool detalhe = args.Contains("--detalhe"), estrito = args.Contains("--estrito");
        if (!git && !disco) { Console.WriteLine("uso: --git | --disco  [--detalhe] [--estrito] | --autoteste   (opcional: --raiz <pasta>)"); return 2; }
        int ir = Array.IndexOf(args, "--raiz");
        string raiz = ir >= 0 && ir + 1 < args.Length ? Path.GetFullPath(args[ir + 1]) : AcharRaiz();
        if (raiz == null) { Console.WriteLine("[assets] não achei a raiz do repositório (rode dentro dele ou use --raiz)."); return 2; }

        List<string> arquivos;
        try { arquivos = git ? ListaGit(raiz) : ListaDisco(raiz); }
        catch (Exception e) { Console.WriteLine("[assets] não consegui listar os arquivos: " + e.Message); return 1; }

        var malhasAcima = new List<(string arq, long tris, int teto, string nome)>();
        var malhasSemTeto = 0; int malhasOk = 0, ilegiveis = 0;
        var texAcima = new List<(string arq, int w, int h)>();
        var bytes = new List<string>();
        var semCompressao = new List<string>();
        int texLidas = 0;

        foreach (var rel in arquivos)
        {
            string abs = Path.Combine(raiz, rel);
            if (!File.Exists(abs)) continue;
            string ext = Path.GetExtension(rel).ToLowerInvariant();
            if (ext == ".fbx")
            {
                var teto = TetosMalha.FirstOrDefault(t => t.padrao.IsMatch("/" + rel));
                if (teto.padrao == null) { malhasSemTeto++; continue; }
                long tris = ContaTriangulos(abs);
                if (tris < 0) { ilegiveis++; continue; }
                if (tris > teto.teto) malhasAcima.Add((rel, tris, teto.teto, teto.nome)); else malhasOk++;
            }
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bytes")
            {
                if (ext == ".bytes") bytes.Add(rel);
                var (w, h) = TamanhoImagem(abs);
                if (w > 0) { texLidas++; if (Math.Max(w, h) > TetoTexturaPx) texAcima.Add((rel, w, h)); }
                string meta = abs + ".meta";
                if (File.Exists(meta) && Regex.IsMatch(File.ReadAllText(meta), @"(?m)^\s*textureCompression:\s*0\s*$"))
                    semCompressao.Add(rel);
            }
        }

        Console.WriteLine($"[assets] malhas medidas: {malhasOk + malhasAcima.Count} (acima do teto: {malhasAcima.Count}); sem teto definido: {malhasSemTeto}; ilegíveis: {ilegiveis}.");
        if (malhasAcima.Count > 0)
        {
            var lista = malhasAcima.OrderByDescending(m => (double)m.tris / m.teto).ToList();
            foreach (var m in detalhe ? lista : lista.Take(8))
                Console.WriteLine($"[assets] aviso: {m.arq}: {Mil(m.tris)} triângulos, teto de {m.nome} {Mil(m.teto)} ({(double)m.tris / m.teto:0.0}x)");
            if (!detalhe && lista.Count > 8) Console.WriteLine($"[assets] aviso: ... e mais {lista.Count - 8} (use --detalhe). O LOD do TEC-10 existe para isso: de perto o modelo cheio, de longe o reduzido.");
        }
        if (texAcima.Count > 0)
            foreach (var t in detalhe ? texAcima : texAcima.Take(8))
                Console.WriteLine($"[assets] aviso: {t.arq}: {t.w}x{t.h}, acima de {TetoTexturaPx} px");
        if (semCompressao.Count > 0)
            Console.WriteLine($"[assets] aviso: {semCompressao.Count} textura(s) importada(s) SEM compressão (textureCompression: 0): {string.Join(", ", semCompressao.Take(5))}");
        if (bytes.Count > 0)
            Console.WriteLine($"[assets] aviso: {bytes.Count} textura(s) em .bytes (JPG decodificado em runtime, RGBA32 na VRAM, sem BC7/BC5). A migração é o TEC-09; textura nova deve entrar como Texture2D importada.");

        int avisos = malhasAcima.Count + texAcima.Count + semCompressao.Count;
        Console.WriteLine($"[assets] {(avisos == 0 ? "dentro do orçamento" : $"{avisos} acima do teto")} ({texLidas} texturas lidas), modo {(estrito ? "estrito" : "aviso")}.");
        return estrito && avisos > 0 ? 1 : 0;
    }

    static string Mil(long n) => n >= 1000 ? (n / 1000.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "k" : n.ToString();

    // ------------------------------------------------------------------ listas

    static List<string> ListaGit(string raiz)
    {
        var psi = new ProcessStartInfo("git") { WorkingDirectory = raiz, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        psi.ArgumentList.Add("ls-files"); psi.ArgumentList.Add("-z"); psi.ArgumentList.Add("--");
        foreach (var r in Raizes) psi.ArgumentList.Add(r);
        using var p = Process.Start(psi);
        string saida = p.StandardOutput.ReadToEnd(), erro = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) throw new Exception("git ls-files: " + erro.Trim());
        return saida.Split('\0', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Replace('\\', '/')).ToList();
    }

    static List<string> ListaDisco(string raiz)
    {
        var l = new List<string>();
        foreach (var r in Raizes)
        {
            string d = Path.Combine(raiz, r);
            if (Directory.Exists(d))
                foreach (var f in Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories)) l.Add(Path.GetRelativePath(raiz, f).Replace('\\', '/'));
        }
        return l;
    }

    static string AcharRaiz()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var d = new DirectoryInfo(inicio); d != null; d = d.Parent)
                if (Directory.Exists(Path.Combine(d.FullName, "Assets")) && Directory.Exists(Path.Combine(d.FullName, "Tools"))) return d.FullName;
        return null;
    }

    // ------------------------------------------------------------------ FBX binário

    /// <summary>Soma os triângulos de todas as Geometry do FBX (cada polígono de n lados vale n-2). -1 = não consegui ler.</summary>
    public static long ContaTriangulos(string caminho)
    {
        try
        {
            using var fs = File.OpenRead(caminho);
            using var br = new BinaryReader(fs);
            return ContaTriangulos(br);
        }
        catch { return -1; }
    }

    static long ContaTriangulos(BinaryReader br)
    {
        byte[] assinatura = br.ReadBytes(21);
        if (Encoding.ASCII.GetString(assinatura) != "Kaydara FBX Binary  \0") return -1; // FBX em ASCII não suportado
        br.ReadBytes(2);
        uint versao = br.ReadUInt32();
        bool largo = versao >= 7500; // 64 bits nos cabeçalhos de nó
        long total = 0;
        long fim = br.BaseStream.Length;
        while (br.BaseStream.Position + (largo ? 25 : 13) <= fim)
        {
            if (!LeNo(br, largo, pai: null, ref total)) break;
        }
        return total;
    }

    /// <summary>Lê um nó e seus filhos. Falso no registro nulo (fim da lista).</summary>
    static bool LeNo(BinaryReader br, bool largo, string pai, ref long total)
    {
        long fimNo = largo ? (long)br.ReadUInt64() : br.ReadUInt32();
        long nProps = largo ? (long)br.ReadUInt64() : br.ReadUInt32();
        long tamProps = largo ? (long)br.ReadUInt64() : br.ReadUInt32();
        byte tamNome = br.ReadByte();
        if (fimNo == 0) return false;
        string nome = Encoding.ASCII.GetString(br.ReadBytes(tamNome));
        long iniProps = br.BaseStream.Position;

        if (nome == "PolygonVertexIndex" && pai == "Geometry")
        {
            int[] idx = LeArrayInt(br, nProps);
            if (idx != null)
            {
                int n = 0;
                foreach (int v in idx)
                {
                    n++;
                    if (v < 0) { if (n >= 3) total += n - 2; n = 0; } // o último índice do polígono vem negado (~i)
                }
            }
        }
        br.BaseStream.Position = iniProps + tamProps;
        // filhos, até o registro nulo ou o fim do nó
        while (br.BaseStream.Position < fimNo)
        {
            long antes = br.BaseStream.Position;
            if (!LeNo(br, largo, nome, ref total)) break;
            if (br.BaseStream.Position <= antes) break;
        }
        br.BaseStream.Position = fimNo;
        return true;
    }

    /// <summary>Primeira propriedade do nó, se for um array de inteiros (tipo 'i'), descomprimindo quando preciso.</summary>
    static int[] LeArrayInt(BinaryReader br, long nProps)
    {
        if (nProps < 1) return null;
        char tipo = (char)br.ReadByte();
        if (tipo != 'i') return null;
        uint n = br.ReadUInt32(), codificacao = br.ReadUInt32(), tamComprimido = br.ReadUInt32();
        byte[] dados = br.ReadBytes((int)tamComprimido);
        if (codificacao == 1)
        {
            using var ms = new MemoryStream(dados);
            using var z = new ZLibStream(ms, CompressionMode.Decompress);
            using var saida = new MemoryStream();
            z.CopyTo(saida);
            dados = saida.ToArray();
        }
        var r = new int[n];
        Buffer.BlockCopy(dados, 0, r, 0, (int)Math.Min(dados.Length, n * 4L));
        return r;
    }

    // ------------------------------------------------------------------ imagens

    /// <summary>Largura e altura de PNG ou JPEG (inclusive JPG guardado como .bytes). 0,0 = não reconheci.</summary>
    public static (int, int) TamanhoImagem(string caminho)
    {
        try
        {
            using var fs = File.OpenRead(caminho);
            var cab = new byte[32];
            int lidos = fs.Read(cab, 0, cab.Length);
            if (lidos >= 24 && cab[0] == 0x89 && cab[1] == 'P' && cab[2] == 'N' && cab[3] == 'G')
                return (cab[16] << 24 | cab[17] << 16 | cab[18] << 8 | cab[19], cab[20] << 24 | cab[21] << 16 | cab[22] << 8 | cab[23]);
            if (lidos >= 4 && cab[0] == 0xFF && cab[1] == 0xD8)
            {
                fs.Position = 2;
                while (fs.Position < fs.Length - 9)
                {
                    int b = fs.ReadByte();
                    if (b != 0xFF) continue;
                    int marca = fs.ReadByte();
                    while (marca == 0xFF) marca = fs.ReadByte();
                    if (marca == 0xD8 || marca == 0x01 || (marca >= 0xD0 && marca <= 0xD7)) continue;
                    int tam = fs.ReadByte() << 8 | fs.ReadByte();
                    if (marca >= 0xC0 && marca <= 0xCF && marca != 0xC4 && marca != 0xC8 && marca != 0xCC)
                    {
                        fs.ReadByte(); // precisão
                        int h = fs.ReadByte() << 8 | fs.ReadByte();
                        int w = fs.ReadByte() << 8 | fs.ReadByte();
                        return (w, h);
                    }
                    fs.Position += tam - 2;
                }
            }
        }
        catch { }
        return (0, 0);
    }

    // ------------------------------------------------------------------ autoteste

    static int AutoTeste()
    {
        int falhas = 0;
        void Confere(bool ok, string nome) { if (!ok) { Console.WriteLine("FALHOU: " + nome); falhas++; } }

        // FBX mínimo, versão 7400 (cabeçalho de 32 bits): Geometry com 1 triângulo, 1 quadrado e 1 pentágono = 1 + 2 + 3 = 6
        foreach (bool comprimido in new[] { false, true })
        {
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0")); w.Write(new byte[] { 0x1A, 0x00 }); w.Write(7400u);
            int[] idx = { 0, 1, ~2, 0, 1, 2, ~3, 0, 1, 2, 3, ~4 };
            byte[] bruto = new byte[idx.Length * 4]; Buffer.BlockCopy(idx, 0, bruto, 0, bruto.Length);
            byte[] corpo = bruto; uint cod = 0;
            if (comprimido)
            {
                var cm = new MemoryStream();
                using (var z = new ZLibStream(cm, CompressionLevel.Fastest, true)) z.Write(bruto, 0, bruto.Length);
                corpo = cm.ToArray(); cod = 1;
            }
            // nó PolygonVertexIndex
            var prop = new MemoryStream(); var pw = new BinaryWriter(prop);
            pw.Write((byte)'i'); pw.Write((uint)idx.Length); pw.Write(cod); pw.Write((uint)corpo.Length); pw.Write(corpo);
            byte[] nomePoly = Encoding.ASCII.GetBytes("PolygonVertexIndex");
            long inicioPoly = ms.Position;
            // Geometry { PolygonVertexIndex }
            byte[] nomeGeo = Encoding.ASCII.GetBytes("Geometry");
            long tamPoly = 13 + nomePoly.Length + prop.Length;
            long tamGeo = 13 + nomeGeo.Length + tamPoly + 13;
            long iniGeo = ms.Position;
            w.Write((uint)(iniGeo + tamGeo)); w.Write(0u); w.Write(0u); w.Write((byte)nomeGeo.Length); w.Write(nomeGeo);
            long iniP = ms.Position;
            w.Write((uint)(iniP + tamPoly)); w.Write(1u); w.Write((uint)prop.Length); w.Write((byte)nomePoly.Length); w.Write(nomePoly); w.Write(prop.ToArray());
            w.Write(new byte[13]); // registro nulo fecha Geometry
            w.Write(new byte[13]); // registro nulo fecha a raiz
            ms.Position = 0;
            long tris = ContaTriangulos(new BinaryReader(ms));
            Confere(tris == 6, $"FBX {(comprimido ? "comprimido" : "cru")}: esperava 6 triângulos, veio {tris}");
        }

        string tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tmp, new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R', 0, 0, 4, 0, 0, 0, 2, 0, 8, 6, 0, 0, 0 });
            Confere(TamanhoImagem(tmp) == (1024, 512), "PNG 1024x512");
            File.WriteAllBytes(tmp, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x4A, 0x46, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x02, 0x00, 0x04, 0x00, 0x03, 0x01, 0x22, 0x00, 0x00, 0x00, 0x00 });
            Confere(TamanhoImagem(tmp) == (1024, 512), "JPEG 1024x512");
        }
        finally { File.Delete(tmp); }

        Confere(TetosMalha.First(t => t.padrao.IsMatch("/Assets/Resources/TDFende/Torres/Torre_Gelo_3.fbx")).teto == 25_000, "teto da torre");
        Confere(TetosMalha.First(t => t.padrao.IsMatch("/Assets/Resources/TDFende/Torres/Fortaleza.fbx")).teto == 40_000, "teto da fortaleza");
        Confere(TetosMalha.First(t => t.padrao.IsMatch("/Assets/Resources/TDFende/Bichos/rato.fbx")).teto == 15_000, "teto do bicho");

        Console.WriteLine(falhas == 0 ? "[assets] autoteste: tudo certo." : $"[assets] autoteste: {falhas} falha(s).");
        return falhas == 0 ? 0 : 1;
    }
}
