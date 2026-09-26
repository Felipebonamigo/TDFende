using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Geometria procedural PURA: listas de vértice/normal/UV e um grupo de triângulos
    /// por material. Não toca em Mesh nem em nada do motor — por isso o mesmo código que
    /// o jogo usa roda no Tools/ArtPreview, que exporta os modelos para conferir fora do
    /// editor.
    ///
    /// UV em unidades de MUNDO (1 UV = 1 unidade): a densidade da textura fica igual em
    /// qualquer peça, e o material decide quantas repetições cabem por unidade. Em faces
    /// verticais o V sobe com o Y, então fiada de pedra fica sempre deitada.
    ///
    /// Ordem dos vértices não precisa de cuidado: todo triângulo é conferido contra a
    /// normal esperada e virado se estiver ao contrário (Unity: frente = horário).
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        /// <summary>Um grupo de triângulos por material, na mesma ordem desta lista.</summary>
        public readonly List<ArtMat> Materials = new List<ArtMat>();
        public readonly List<List<int>> Triangles = new List<List<int>>();

        Vector3 _pos = Vector3.zero;
        Quaternion _rot = Quaternion.identity;

        /// <summary>Tudo que for adicionado depois disto sai girado e deslocado.</summary>
        public void SetTransform(Vector3 pos, Quaternion rot)
        {
            _pos = pos;
            _rot = rot;
        }

        public void ResetTransform() => SetTransform(Vector3.zero, Quaternion.identity);

        public int VertexCount => Vertices.Count;

        List<int> Group(ArtMat m)
        {
            int i = Materials.IndexOf(m);
            if (i >= 0) return Triangles[i];
            Materials.Add(m);
            var list = new List<int>();
            Triangles.Add(list);
            return list;
        }

        int V(Vector3 p, Vector3 n, Vector2 uv)
        {
            Vertices.Add(_pos + _rot * p);
            Normals.Add((_rot * n).normalized);
            Uvs.Add(uv);
            return Vertices.Count - 1;
        }

        /// <summary>Triângulo com a frente virada para <paramref name="facing"/> (espaço local).</summary>
        void Tri(List<int> g, int a, int b, int c, Vector3 facing)
        {
            var pa = Vertices[a];
            var n = Vector3.Cross(Vertices[b] - pa, Vertices[c] - pa);
            if (Vector3.Dot(n, _rot * facing) < 0f)
            {
                int t = b;
                b = c;
                c = t;
            }
            g.Add(a);
            g.Add(b);
            g.Add(c);
        }

        void Quad(List<int> g, int a, int b, int c, int d, Vector3 facing)
        {
            Tri(g, a, b, c, facing);
            Tri(g, a, c, d, facing);
        }

        // ------------------------------------------------------------------ caixas

        /// <summary>Caixa centrada em <paramref name="c"/>, faces planas.</summary>
        public void Box(ArtMat m, Vector3 c, Vector3 size, Quaternion? rot = null)
        {
            var r = rot ?? Quaternion.identity;
            var g = Group(m);
            var h = size * 0.5f;
            // (normal, eixo U, eixo V) por face; V vertical nas laterais
            Face(g, c, r, Vector3.right, Vector3.forward, Vector3.up, h.x, h.z, h.y);
            Face(g, c, r, Vector3.left, Vector3.back, Vector3.up, h.x, h.z, h.y);
            Face(g, c, r, Vector3.forward, Vector3.left, Vector3.up, h.z, h.x, h.y);
            Face(g, c, r, Vector3.back, Vector3.right, Vector3.up, h.z, h.x, h.y);
            Face(g, c, r, Vector3.up, Vector3.right, Vector3.forward, h.y, h.x, h.z);
            Face(g, c, r, Vector3.down, Vector3.right, Vector3.back, h.y, h.x, h.z);
        }

        void Face(List<int> g, Vector3 c, Quaternion r, Vector3 n, Vector3 u, Vector3 v,
            float dn, float du, float dv)
        {
            var center = c + r * (n * dn);
            var wn = r * n;
            var wu = r * u * du;
            var wv = r * v * dv;
            int a = V(center - wu - wv, wn, new Vector2(-du, -dv));
            int b = V(center - wu + wv, wn, new Vector2(-du, dv));
            int cc = V(center + wu + wv, wn, new Vector2(du, dv));
            int d = V(center + wu - wv, wn, new Vector2(du, -dv));
            Quad(g, a, b, cc, d, wn);
        }

        // ------------------------------------------------------------- revolução

        /// <summary>
        /// Tronco de cone (cilindro se r0 == r1, cone se r1 == 0) em pé sobre
        /// <paramref name="baseCenter"/>, girado por <paramref name="rot"/> em torno da base.
        /// <paramref name="flat"/> = facetado (bastião octogonal, telhado piramidal).
        /// </summary>
        public void Cylinder(ArtMat m, Vector3 baseCenter, float r0, float r1, float height, int sides,
            Quaternion? rot = null, bool capTop = true, bool capBottom = true, bool flat = false,
            float angleOffset = 0f)
        {
            var r = rot ?? Quaternion.identity;
            Lathe(m, baseCenter, new[] { r0, r1 }, new[] { 0f, height }, sides, r, flat, angleOffset);
            var g = Group(m);
            if (capTop && r1 > 0.0001f) Cap(g, baseCenter, r, r1, height, sides, true, angleOffset);
            if (capBottom && r0 > 0.0001f) Cap(g, baseCenter, r, r0, 0f, sides, false, angleOffset);
        }

        void Cap(List<int> g, Vector3 baseCenter, Quaternion r, float radius, float y, int sides, bool up,
            float angleOffset)
        {
            var n = r * (up ? Vector3.up : Vector3.down);
            var center = baseCenter + r * new Vector3(0f, y, 0f);
            int ci = V(center, n, Vector2.zero);
            int prev = -1;
            for (int i = 0; i <= sides; i++)
            {
                float a = angleOffset + i * Mathf.PI * 2f / sides;
                var local = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                int vi = V(center + r * local, n, new Vector2(local.x, local.z));
                if (i > 0) Tri(g, ci, prev, vi, n);
                prev = vi;
            }
        }

        /// <summary>
        /// Perfil girado em torno do eixo Y local: raio[i] na altura[i]. Serve para cano
        /// de canhão, capacete, cúpula, barril — qualquer coisa torneada.
        /// </summary>
        public void Lathe(ArtMat m, Vector3 baseCenter, float[] radii, float[] heights, int sides,
            Quaternion? rot = null, bool flat = false, float angleOffset = 0f)
        {
            var r = rot ?? Quaternion.identity;
            var g = Group(m);
            int rings = radii.Length;

            // comprimento acumulado do perfil vira o V da textura
            var vAt = new float[rings];
            for (int k = 1; k < rings; k++)
                vAt[k] = vAt[k - 1] + Mathf.Sqrt(Sq(radii[k] - radii[k - 1]) + Sq(heights[k] - heights[k - 1]));

            for (int k = 0; k < rings - 1; k++)
            {
                float dr = radii[k + 1] - radii[k];
                float dh = heights[k + 1] - heights[k];
                // inclinação do segmento: normal aponta para fora e um pouco para cima/baixo
                var slope = new Vector2(dh, -dr).normalized;
                float avgR = (radii[k] + radii[k + 1]) * 0.5f;

                if (flat)
                {
                    for (int i = 0; i < sides; i++)
                    {
                        float a0 = angleOffset + i * Mathf.PI * 2f / sides;
                        float a1 = angleOffset + (i + 1) * Mathf.PI * 2f / sides;
                        float am = (a0 + a1) * 0.5f;
                        var n = new Vector3(Mathf.Cos(am) * slope.x, slope.y, Mathf.Sin(am) * slope.x);
                        var wn = r * n;
                        float u0 = 0f, u1 = 2f * avgR * Mathf.Sin(Mathf.PI / sides);
                        int p0 = V(baseCenter + r * Ring(a0, radii[k], heights[k]), wn, new Vector2(u0, vAt[k]));
                        int p1 = V(baseCenter + r * Ring(a0, radii[k + 1], heights[k + 1]), wn, new Vector2(u0, vAt[k + 1]));
                        int p2 = V(baseCenter + r * Ring(a1, radii[k + 1], heights[k + 1]), wn, new Vector2(u1, vAt[k + 1]));
                        int p3 = V(baseCenter + r * Ring(a1, radii[k], heights[k]), wn, new Vector2(u1, vAt[k]));
                        if (radii[k + 1] < 0.0001f) Tri(g, p0, p1, p3, wn);
                        else if (radii[k] < 0.0001f) Tri(g, p0, p2, p3, wn);
                        else Quad(g, p0, p1, p2, p3, wn);
                    }
                    continue;
                }

                int start = Vertices.Count;
                for (int i = 0; i <= sides; i++)
                {
                    float a = angleOffset + i * Mathf.PI * 2f / sides;
                    var n = new Vector3(Mathf.Cos(a) * slope.x, slope.y, Mathf.Sin(a) * slope.x);
                    float u = a * avgR;
                    V(baseCenter + r * Ring(a, radii[k], heights[k]), r * n, new Vector2(u, vAt[k]));
                    V(baseCenter + r * Ring(a, radii[k + 1], heights[k + 1]), r * n, new Vector2(u, vAt[k + 1]));
                }
                for (int i = 0; i < sides; i++)
                {
                    int a0 = start + i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
                    float am = angleOffset + (i + 0.5f) * Mathf.PI * 2f / sides;
                    var facing = r * new Vector3(Mathf.Cos(am) * slope.x, slope.y, Mathf.Sin(am) * slope.x);
                    if (radii[k + 1] < 0.0001f) Tri(g, a0, a1, b0, facing);
                    else if (radii[k] < 0.0001f) Tri(g, a0, a1, b1, facing);
                    else Quad(g, a0, a1, b1, b0, facing);
                }
            }
        }

        static Vector3 Ring(float a, float radius, float y) =>
            new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);

        static float Sq(float x) => x * x;

        // ---------------------------------------------------------------- esferas

        /// <summary>
        /// Elipsoide. <paramref name="lumpiness"/> &gt; 0 deforma a superfície com ruído
        /// (pedra, copa de árvore) e recalcula as normais da peça.
        /// </summary>
        public void Sphere(ArtMat m, Vector3 c, Vector3 radii, int segments = 12, int rings = 8,
            float lumpiness = 0f, int seed = 0)
        {
            var g = Group(m);
            int start = Vertices.Count;
            for (int j = 0; j <= rings; j++)
            {
                float lat = Mathf.PI * j / rings; // 0 = topo
                for (int i = 0; i <= segments; i++)
                {
                    float lon = Mathf.PI * 2f * (i % segments) / segments;
                    var dir = new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon));
                    float k = 1f;
                    if (lumpiness > 0f)
                        k += lumpiness * (ProcNoise.Value3(dir * 2.3f, seed) * 2f - 1f)
                             + lumpiness * 0.5f * (ProcNoise.Value3(dir * 5.1f, seed + 7) * 2f - 1f);
                    var p = new Vector3(dir.x * radii.x, dir.y * radii.y, dir.z * radii.z) * k;
                    var n = new Vector3(dir.x / radii.x, dir.y / radii.y, dir.z / radii.z).normalized;
                    float u = (Mathf.PI * 2f * i / segments) * (radii.x + radii.z) * 0.5f;
                    V(c + p, n, new Vector2(u, lat * radii.y));
                }
            }
            int row = segments + 1;
            for (int j = 0; j < rings; j++)
            for (int i = 0; i < segments; i++)
            {
                int a = start + j * row + i, b = a + 1, d = a + row, e = d + 1;
                // Normals já saíram giradas por _rot; Tri espera a direção no espaço local
                var facing = Quaternion.Inverse(_rot) * (Normals[a] + Normals[e]);
                // os polos viram triângulo único: o quad degenerado geraria área zero
                if (j == 0) Tri(g, a, d, e, facing);
                else if (j == rings - 1) Tri(g, a, b, d, facing);
                else Quad(g, a, b, e, d, facing);
            }
            if (lumpiness > 0f) SmoothNormals(start, g);
        }

        /// <summary>Normais pela média das faces, só na faixa de vértices de uma peça.</summary>
        void SmoothNormals(int start, List<int> g)
        {
            for (int i = start; i < Normals.Count; i++) Normals[i] = Vector3.zero;
            for (int t = 0; t < g.Count; t += 3)
            {
                int a = g[t], b = g[t + 1], c = g[t + 2];
                if (a < start) continue;
                var n = Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
                Normals[a] += n;
                Normals[b] += n;
                Normals[c] += n;
            }
            // costura da esfera: vértice da coluna 0 e da última são o mesmo ponto
            for (int i = start; i < Normals.Count; i++) Normals[i] = Normals[i].normalized;
        }

        // ----------------------------------------------------------------- planos

        /// <summary>Quadrilátero de duas faces (bandeira, lona de asa). Cantos em ordem ao redor.</summary>
        public void DoubleQuad(ArtMat m, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var g = Group(m);
            var n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            float w = (p3 - p0).magnitude, h = (p1 - p0).magnitude;
            for (int side = 0; side < 2; side++)
            {
                var nn = side == 0 ? n : -n;
                int a = V(p0, nn, new Vector2(0f, 0f));
                int b = V(p1, nn, new Vector2(0f, h));
                int c = V(p2, nn, new Vector2(w, h));
                int d = V(p3, nn, new Vector2(w, 0f));
                Quad(g, a, b, c, d, nn);
            }
        }

        /// <summary>Triângulo de duas faces (asa, flâmula).</summary>
        public void DoubleTri(ArtMat m, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var g = Group(m);
            var n = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            for (int side = 0; side < 2; side++)
            {
                var nn = side == 0 ? n : -n;
                int a = V(p0, nn, new Vector2(0f, 0f));
                int b = V(p1, nn, new Vector2(0f, (p1 - p0).magnitude));
                int c = V(p2, nn, new Vector2((p2 - p0).magnitude, 0f));
                Tri(g, a, b, c, nn);
            }
        }

        /// <summary>Quadrilátero horizontal de uma face, virado para cima (chão, remendo de terra).</summary>
        public void GroundQuad(ArtMat m, Vector3 c, float hx, float hz)
        {
            var g = Group(m);
            var n = Vector3.up;
            int a = V(c + new Vector3(-hx, 0f, -hz), n, new Vector2(c.x - hx, c.z - hz));
            int b = V(c + new Vector3(-hx, 0f, hz), n, new Vector2(c.x - hx, c.z + hz));
            int d = V(c + new Vector3(hx, 0f, hz), n, new Vector2(c.x + hx, c.z + hz));
            int e = V(c + new Vector3(hx, 0f, -hz), n, new Vector2(c.x + hx, c.z - hz));
            Quad(g, a, b, d, e, n);
        }

        /// <summary>
        /// Terreno em grade regular, altura dada por função. Célula cuja altura média
        /// fica abaixo de <paramref name="lowY"/> usa <paramref name="low"/> (leito de rio).
        /// </summary>
        public void Heightfield(ArtMat top, ArtMat low, float lowY, Vector3 min, float sizeX, float sizeZ,
            int nx, int nz, System.Func<float, float, float> height)
        {
            var gTop = Group(top);
            var gLow = Group(low);
            int start = Vertices.Count;
            float dx = sizeX / nx, dz = sizeZ / nz, e = Mathf.Min(dx, dz) * 0.5f;
            for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
            {
                float x = min.x + i * dx, z = min.z + j * dz;
                float y = height(x, z);
                float sx = (height(x + e, z) - height(x - e, z)) / (2f * e);
                float sz = (height(x, z + e) - height(x, z - e)) / (2f * e);
                V(new Vector3(x, min.y + y, z), new Vector3(-sx, 1f, -sz).normalized, new Vector2(x, z));
            }
            int row = nx + 1;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = start + j * row + i, b = a + row, c = b + 1, d = a + 1;
                float avg = (Vertices[a].y + Vertices[b].y + Vertices[c].y + Vertices[d].y) * 0.25f;
                Quad(avg < lowY ? gLow : gTop, a, b, c, d, Vector3.up);
            }
        }

        /// <summary>Viga entre dois pontos (seção quadrada): mastro, trave, perna de andaime.</summary>
        public void Beam(ArtMat m, Vector3 from, Vector3 to, float thickness)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            var rot = Quaternion.FromToRotation(Vector3.up, d / len);
            Box(m, (from + to) * 0.5f, new Vector3(thickness, len, thickness), rot);
        }

        /// <summary>Haste redonda entre dois pontos: lança, eixo, cano fino.</summary>
        public void Rod(ArtMat m, Vector3 from, Vector3 to, float radius, int sides = 6)
        {
            var d = to - from;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            var rot = Quaternion.FromToRotation(Vector3.up, d / len);
            Cylinder(m, from, radius, radius, len, sides, rot);
        }
    }
}
