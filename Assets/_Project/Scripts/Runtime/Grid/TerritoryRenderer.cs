using System.Collections.Generic;
using UnityEngine;

namespace FrontierTD
{
    /// <summary>
    /// Desenha o território como um mesh único gerado em código:
    /// preenchimento translúcido + linha de fronteira mais forte nas bordas (estilo RoN).
    /// Reconstruído só quando uma torre entra/sai — custo zero por frame.
    /// </summary>
    public class TerritoryRenderer
    {
        static readonly Color Fill = new Color(0.25f, 0.60f, 1f, 0.16f);
        static readonly Color EdgeLine = new Color(0.35f, 0.85f, 1f, 0.65f);
        const float Y = 0.02f;         // um tiquinho acima do chão, sem z-fighting
        const float EdgeWidth = 0.13f; // largura da linha de fronteira

        readonly Mesh _mesh;
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();

        public TerritoryRenderer()
        {
            var go = new GameObject("Fronteira");
            _mesh = new Mesh { name = "TerritoryMesh" };
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // TODO(build): Shader.Find sofre stripping em builds — quando formos buildar,
            // referenciar via material asset ou Always Included Shaders.
            var shader = Shader.Find("FrontierTD/TerritoryOverlay");
            if (shader != null)
            {
                mr.sharedMaterial = new Material(shader);
            }
            else
            {
                Debug.LogWarning("[FrontierTD] Shader da fronteira não encontrado; usando material opaco.");
                mr.sharedMaterial = MaterialFactory.Get(new Color(0.25f, 0.5f, 0.9f));
            }
        }

        public void Rebuild(TerritoryField territory, GridMap map)
        {
            _verts.Clear();
            _colors.Clear();
            _tris.Clear();

            float cs = map.CellSize;
            float half = cs * 0.5f;
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!territory.Contains(x, y)) continue;
                var center = map.CellToWorld(x, y) + Vector3.up * Y;

                AddQuad(center, half, half, Fill);

                // linha de fronteira: tira fina em cada lado que faz divisa com o "fora"
                if (!territory.Contains(x + 1, y))
                    AddQuad(center + new Vector3(half - EdgeWidth * 0.5f, 0f, 0f), EdgeWidth * 0.5f, half, EdgeLine);
                if (!territory.Contains(x - 1, y))
                    AddQuad(center + new Vector3(-half + EdgeWidth * 0.5f, 0f, 0f), EdgeWidth * 0.5f, half, EdgeLine);
                if (!territory.Contains(x, y + 1))
                    AddQuad(center + new Vector3(0f, 0f, half - EdgeWidth * 0.5f), half, EdgeWidth * 0.5f, EdgeLine);
                if (!territory.Contains(x, y - 1))
                    AddQuad(center + new Vector3(0f, 0f, -half + EdgeWidth * 0.5f), half, EdgeWidth * 0.5f, EdgeLine);
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
        }

        // quad horizontal centrado em 'center', meia-largura hx (eixo X) e hz (eixo Z)
        void AddQuad(Vector3 center, float hx, float hz, Color color)
        {
            int i = _verts.Count;
            _verts.Add(center + new Vector3(-hx, 0f, -hz));
            _verts.Add(center + new Vector3(-hx, 0f, hz));
            _verts.Add(center + new Vector3(hx, 0f, hz));
            _verts.Add(center + new Vector3(hx, 0f, -hz));
            for (int k = 0; k < 4; k++) _colors.Add(color);
            _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
            _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 3);
        }
    }
}
