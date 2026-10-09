using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Linhas de giz sobre a grama: contorno forte da área jogável e grade fraca das células.
    ///
    /// O chão quadriculado antigo fazia esse papel de graça; com grama de verdade, sem linha
    /// nenhuma o jogador perde a noção de onde fica cada célula ao montar o labirinto.
    /// Giz claro, como marcação de campo esportivo, lê bem sobre verde e combina com o realista.
    ///
    /// Usa o mesmo shader transparente de cor-por-vértice da fronteira — sem shader novo.
    /// </summary>
    public static class GridOverlay
    {
        static readonly Color Border = new Color(0.96f, 0.95f, 0.88f, 0.62f);
        static readonly Color Cell = new Color(0.96f, 0.95f, 0.88f, 0.11f);
        const float BorderWidth = 0.07f;
        const float CellWidth = 0.025f;
        const float Y = 0.012f; // abaixo da fronteira (0,02) e do fantasma (0,05)

        // O R recria as lanes a cada partida, e malha/material criados em código não morrem
        // com o GameObject. A grade só depende das dimensões do mapa, então é uma por
        // formato de mapa, reaproveitada — em vez de uma nova (vazada) por partida.
        static readonly Dictionary<(int, int, float), Mesh> _meshes = new Dictionary<(int, int, float), Mesh>();
        static Material _material;

        public static GameObject Build(GridMap map, Transform parent)
        {
            var key = (map.Width, map.Height, map.CellSize);
            if (!_meshes.TryGetValue(key, out var mesh) || mesh == null)
                _meshes[key] = mesh = BuildMesh(map);

            if (_material == null)
            {
                var shader = ShaderRefs.TerritoryOverlay;
                _material = shader != null ? new Material(shader) : MaterialFactory.Get(Border);
            }

            var go = new GameObject("GradeGiz");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = _material;
            return go;
        }

        static Mesh BuildMesh(GridMap map)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float cs = map.CellSize;
            float w = map.Width * cs, h = map.Height * cs;
            // mesmo referencial do GridMap: grade centrada na origem local
            float x0 = -w * 0.5f, z0 = -h * 0.5f;

            for (int i = 0; i <= map.Width; i++)
            {
                bool edge = i == 0 || i == map.Width;
                float x = x0 + i * cs;
                AddQuad(verts, colors, tris, x, z0, x, z0 + h, edge ? BorderWidth : CellWidth, edge ? Border : Cell);
            }
            for (int j = 0; j <= map.Height; j++)
            {
                bool edge = j == 0 || j == map.Height;
                float z = z0 + j * cs;
                AddQuad(verts, colors, tris, x0, z, x0 + w, z, edge ? BorderWidth : CellWidth, edge ? Border : Cell);
            }

            var mesh = new Mesh { name = "GradeGiz" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // faixa fina entre (ax,az) e (bx,bz), com a espessura centrada na linha
        static void AddQuad(List<Vector3> v, List<Color> c, List<int> t,
            float ax, float az, float bx, float bz, float width, Color color)
        {
            var dir = new Vector3(bx - ax, 0f, bz - az).normalized;
            var side = new Vector3(-dir.z, 0f, dir.x) * (width * 0.5f);
            int i = v.Count;
            v.Add(new Vector3(ax, Y, az) - side);
            v.Add(new Vector3(ax, Y, az) + side);
            v.Add(new Vector3(bx, Y, bz) + side);
            v.Add(new Vector3(bx, Y, bz) - side);
            var vc = Palette.ForVertex(color);
            for (int k = 0; k < 4; k++) c.Add(vc);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
    }
}
