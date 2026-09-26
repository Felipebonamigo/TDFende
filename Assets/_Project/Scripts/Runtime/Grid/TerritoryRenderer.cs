using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Desenha o território como um mesh único gerado em código:
    /// preenchimento translúcido + linha de fronteira mais forte nas bordas (estilo RoN).
    /// Reconstruído só quando uma torre entra/sai — custo zero por frame.
    /// A cor é a do dono: fronteira azul é sua, vermelha é da IA.
    /// </summary>
    public class TerritoryRenderer
    {
        readonly Color _fill;
        readonly Color _edge;
        const float Y = 0.02f;         // um tiquinho acima do chão, sem z-fighting
        const float EdgeWidth = 0.09f; // largura da linha de fronteira

        readonly Mesh _mesh;
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Color> _colors = new List<Color>();
        readonly List<int> _tris = new List<int>();

        /// <param name="parent">
        /// Opcional. No Tower Wars cada lane vive sob um pai deslocado, e o mesh é
        /// construído em coordenadas locais do grid — sem isso as duas lanes se
        /// desenhariam uma em cima da outra.
        /// </param>
        public TerritoryRenderer(Color team, Transform parent = null)
        {
            _fill = Palette.TerritoryFill(team);
            _edge = Palette.TerritoryEdge(team);
            var go = new GameObject("Fronteira");
            if (parent != null) go.transform.SetParent(parent, false);
            _mesh = new Mesh { name = "TerritoryMesh" };
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // TODO(build): Shader.Find sofre stripping em builds — quando formos buildar,
            // referenciar via material asset ou Always Included Shaders.
            var shader = Shader.Find("TDFende/TerritoryOverlay");
            if (shader != null)
            {
                mr.sharedMaterial = new Material(shader);
            }
            else
            {
                Debug.LogWarning("[TDFende] Shader da fronteira não encontrado; usando material opaco.");
                mr.sharedMaterial = MaterialFactory.Get(_edge);
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

                AddQuad(center, half, half, _fill);

                // linha de fronteira: tira fina em cada lado que faz divisa com o "fora"
                if (!territory.Contains(x + 1, y))
                    AddQuad(center + new Vector3(half - EdgeWidth * 0.5f, 0f, 0f), EdgeWidth * 0.5f, half, _edge);
                if (!territory.Contains(x - 1, y))
                    AddQuad(center + new Vector3(-half + EdgeWidth * 0.5f, 0f, 0f), EdgeWidth * 0.5f, half, _edge);
                if (!territory.Contains(x, y + 1))
                    AddQuad(center + new Vector3(0f, 0f, half - EdgeWidth * 0.5f), half, EdgeWidth * 0.5f, _edge);
                if (!territory.Contains(x, y - 1))
                    AddQuad(center + new Vector3(0f, 0f, -half + EdgeWidth * 0.5f), half, EdgeWidth * 0.5f, _edge);
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
