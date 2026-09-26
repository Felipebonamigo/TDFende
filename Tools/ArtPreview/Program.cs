// Exporta os modelos e texturas procedurais do jogo (Assets/.../Runtime/Art) para um
// JSON que o preview.html desenha com three.js. Existe para conferir a arte SEM abrir
// o Unity: o mesmo código que o jogo roda, visto num navegador.
//
//   dotnet run                 -> escreve out/art.json + out/index.html
//   npx http-server out        -> abra http://localhost:8080          (modelos lado a lado)
//                                 e http://localhost:8080/?scene=world (o Tower Wars montado)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using TDFende;
using UnityEngine;

static class Program
{
    static int Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : "out";
        Directory.CreateDirectory(outDir);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var models = new List<ModelDef>();
        for (int i = 0; i < 4; i++) models.Add(ModelLib.Tower(i));
        for (int i = 0; i < 6; i++) models.Add(ModelLib.Enemy(i));
        for (int i = 0; i < 4; i++) models.Add(ModelLib.Projectile(i));
        models.Add(ModelLib.Keep());
        models.Add(ModelLib.Camp());
        var scenery = new ModelDef { Name = "Cenario" };
        var sm = scenery.Part("Body", Vector3.zero).Mesh;
        ModelLib.Pine(sm, new Vector3(-1.2f, 0, 0), 1f, 1);
        ModelLib.Pine(sm, new Vector3(-0.4f, 0, 0.3f), 0.8f, 2);
        ModelLib.Oak(sm, new Vector3(0.7f, 0, 0), 1f, 3);
        ModelLib.Boulder(sm, new Vector3(1.8f, 0, 0.2f), 0.35f, 4);
        ModelLib.Bush(sm, new Vector3(1.5f, 0, -0.5f), 1f, 5);
        models.Add(scenery);

        // Tower Wars montado como o jogo monta: duas lanes 24x16, rio no meio, mata em volta
        var world = new WorldLayout { River = true, RiverZ = 0f };
        world.AddPlayArea(new Vector3(0, 0, -11), new Vector3(24, 0, 16));
        world.AddPlayArea(new Vector3(0, 0, 11), new Vector3(24, 0, 16));
        foreach (var (name, mb) in new[] { ("Terreno", world.BuildTerrain()), ("Rio", world.BuildWater()),
                     ("Mureta", world.BuildCurbs()), ("Mata", world.BuildScenery()) })
        {
            var d = new ModelDef { Name = name };
            var part = d.Part("Body", Vector3.zero);
            Copy(mb, part.Mesh);
            models.Add(d);
        }
        Console.WriteLine($"modelos: {sw.ElapsedMilliseconds} ms");

        var sb = new StringBuilder();
        sb.Append("{\"models\":[");
        int verts = 0;
        for (int mi = 0; mi < models.Count; mi++)
        {
            var m = models[mi];
            if (mi > 0) sb.Append(',');
            sb.Append("{\"name\":\"").Append(m.Name).Append("\",\"height\":").Append(F(m.Height)).Append(",\"parts\":[");
            for (int pi = 0; pi < m.Parts.Count; pi++)
            {
                var p = m.Parts[pi];
                if (pi > 0) sb.Append(',');
                sb.Append("{\"name\":\"").Append(p.Name).Append("\",\"parent\":")
                  .Append(p.Parent == null ? "null" : "\"" + p.Parent + "\"")
                  .Append(",\"hidden\":").Append(p.StartHidden ? "true" : "false")
                  .Append(",\"pivot\":").Append(V(p.Pivot));
                var mb = p.Mesh;
                verts += mb.Vertices.Count;
                sb.Append(",\"pos\":[");
                for (int i = 0; i < mb.Vertices.Count; i++) { if (i > 0) sb.Append(','); var v = mb.Vertices[i] - p.Pivot; sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z)); }
                sb.Append("],\"nrm\":[");
                for (int i = 0; i < mb.Normals.Count; i++) { if (i > 0) sb.Append(','); var v = mb.Normals[i]; sb.Append(F(v.x)).Append(',').Append(F(v.y)).Append(',').Append(F(v.z)); }
                sb.Append("],\"uv\":[");
                for (int i = 0; i < mb.Uvs.Count; i++) { if (i > 0) sb.Append(','); sb.Append(F(mb.Uvs[i].x)).Append(',').Append(F(mb.Uvs[i].y)); }
                sb.Append("],\"groups\":[");
                for (int g = 0; g < mb.Materials.Count; g++)
                {
                    if (g > 0) sb.Append(',');
                    sb.Append("{\"mat\":\"").Append(mb.Materials[g]).Append("\",\"idx\":[").Append(string.Join(",", mb.Triangles[g])).Append("]}");
                }
                sb.Append("]}");
            }
            sb.Append("]}");
        }
        sb.Append("],\"materials\":{");
        var mats = (ArtMat[])Enum.GetValues(typeof(ArtMat));
        sw.Restart();
        for (int i = 0; i < mats.Length; i++)
        {
            var spec = MatSpec.Of(mats[i]);
            var tex = ProcTex.Generate(mats[i]);
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(mats[i]).Append("\":{\"base\":[").Append(F(spec.Base.r)).Append(',').Append(F(spec.Base.g)).Append(',').Append(F(spec.Base.b))
              .Append("],\"smooth\":").Append(F(spec.Smoothness)).Append(",\"metal\":").Append(F(spec.Metallic))
              .Append(",\"tile\":").Append(F(spec.UnitsPerTile))
              .Append(",\"emit\":[").Append(F(spec.Emission.r)).Append(',').Append(F(spec.Emission.g)).Append(',').Append(F(spec.Emission.b)).Append(']')
              .Append(",\"albedo\":\"data:image/png;base64,").Append(Convert.ToBase64String(Png(tex.Albedo, tex.Size)))
              .Append("\",\"normal\":\"data:image/png;base64,").Append(Convert.ToBase64String(Png(tex.Normal, tex.Size))).Append("\"}");
            File.WriteAllBytes(Path.Combine(outDir, mats[i] + ".png"), Png(tex.Albedo, tex.Size));
        }
        sb.Append("}}");
        Console.WriteLine($"texturas: {sw.ElapsedMilliseconds} ms, vértices: {verts}");
        File.WriteAllText(Path.Combine(outDir, "art.json"), sb.ToString());
        if (File.Exists("preview.html")) File.Copy("preview.html", Path.Combine(outDir, "index.html"), true);
        Console.WriteLine($"escrito {Path.Combine(outDir, "art.json")}");
        return 0;
    }

    static void Copy(MeshBuilder from, MeshBuilder to)
    {
        to.Vertices.AddRange(from.Vertices);
        to.Normals.AddRange(from.Normals);
        to.Uvs.AddRange(from.Uvs);
        to.Materials.AddRange(from.Materials);
        foreach (var t in from.Triangles) to.Triangles.Add(t);
    }

    static string F(float f) => f.ToString("0.####", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => $"[{F(v.x)},{F(v.y)},{F(v.z)}]";

    // PNG RGBA mínimo. Linha 0 do Unity é a BASE da imagem; no PNG a linha 0 é o topo.
    static byte[] Png(Color32[] px, int s)
    {
        var raw = new byte[s * (s * 4 + 1)];
        for (int y = 0; y < s; y++)
        {
            int row = (s - 1 - y) * (s * 4 + 1);
            raw[row] = 0;
            for (int x = 0; x < s; x++)
            {
                var c = px[y * s + x];
                int o = row + 1 + x * 4;
                raw[o] = c.r; raw[o + 1] = c.g; raw[o + 2] = c.b; raw[o + 3] = c.a;
            }
        }
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, s); BE(ihdr, 4, s); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(ms, "IHDR", ihdr);
        using (var z = new MemoryStream())
        {
            using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) zs.Write(raw);
            Chunk(ms, "IDAT", z.ToArray());
        }
        Chunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var td = new byte[4 + data.Length];
        Encoding.ASCII.GetBytes(type).CopyTo(td, 0); data.CopyTo(td, 4);
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, (int)Crc(td)); s.Write(crc);
    }

    static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in d)
        {
            c ^= b;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        }
        return c ^ 0xFFFFFFFF;
    }
}
