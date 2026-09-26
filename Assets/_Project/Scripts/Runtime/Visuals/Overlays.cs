using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Desenhos de interface no chão, sem luz: grade de construção, fantasma da
    /// célula e anel de alcance. Todos usam o material Overlay (cor por vértice +
    /// _Color), então tingir é um MaterialPropertyBlock, não um material novo.
    /// </summary>
    public static class Overlays
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Linhas finas entre as células: dá para mirar a construção sem virar tabuleiro de xadrez.</summary>
        public static GameObject Grid(GridMap map, Transform parent)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            const float w = 0.012f, y = 0.008f;
            var size = map.WorldSize;
            float x0 = -size.x * 0.5f, z0 = -size.z * 0.5f;
            for (int i = 1; i < map.Width; i++)
                Strip(verts, cols, tris, new Vector3(x0 + i * map.CellSize, y, 0f), w, size.z * 0.5f);
            for (int j = 1; j < map.Height; j++)
                Strip(verts, cols, tris, new Vector3(0f, y, z0 + j * map.CellSize), size.x * 0.5f, w);

            var mesh = new Mesh { name = "Grade" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return MakeObject("Grade", mesh, parent, Palette.GridLine);
        }

        static void Strip(List<Vector3> v, List<Color> c, List<int> t, Vector3 center, float hx, float hz)
        {
            int i = v.Count;
            v.Add(center + new Vector3(-hx, 0f, -hz));
            v.Add(center + new Vector3(-hx, 0f, hz));
            v.Add(center + new Vector3(hx, 0f, hz));
            v.Add(center + new Vector3(hx, 0f, -hz));
            for (int k = 0; k < 4; k++) c.Add(Color.white);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        public static GameObject MakeObject(string name, Mesh mesh, Transform parent, Color tint)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ArtFactory.Overlay;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            Tint(mr, tint);
            return go;
        }

        static MaterialPropertyBlock _mpb;

        public static void Tint(Renderer r, Color c)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Anel de raio 1 (espessura relativa), escalado pelo alcance da torre.</summary>
        public static Mesh Ring(float thickness)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            const int seg = 72;
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts.Add(d * (1f - thickness));
                verts.Add(d);
                cols.Add(Color.white);
                cols.Add(Color.white);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 3);
                tris.Add(a); tris.Add(a + 3); tris.Add(a + 2);
            }
            var m = new Mesh { name = "Anel" };
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        public static Mesh Tile(float half)
        {
            var m = new Mesh { name = "Celula" };
            m.SetVertices(new List<Vector3>
            {
                new Vector3(-half, 0f, -half), new Vector3(-half, 0f, half), new Vector3(half, 0f, half), new Vector3(half, 0f, -half)
            });
            m.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
            m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Fantasma de construção: célula colorida (pode/não pode/subir) + anel do alcance.</summary>
    public class PlacementGhost
    {
        readonly Transform _root;
        readonly Renderer _tile;
        readonly Transform _ring;
        readonly Renderer _ringRenderer;

        public PlacementGhost()
        {
            _root = new GameObject("GhostTorre").transform;
            _tile = Overlays.MakeObject("Celula", Overlays.Tile(0.48f), _root, Palette.GhostValid).GetComponent<Renderer>();
            _tile.transform.localPosition = Vector3.up * 0.015f;
            var ring = Overlays.MakeObject("Alcance", Overlays.Ring(0.018f), _root, Palette.RangeRing);
            ring.transform.localPosition = Vector3.up * 0.02f;
            _ring = ring.transform;
            _ringRenderer = ring.GetComponent<Renderer>();
            _root.gameObject.SetActive(false);
        }

        public void Show(Vector3 cellCenter, Color tile, float range)
        {
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            _root.position = cellCenter;
            // pulsa devagar: o fantasma não se confunde com o chão
            var c = tile;
            c.a *= 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
            Overlays.Tint(_tile, c);
            _ring.gameObject.SetActive(range > 0f);
            if (range > 0f)
            {
                _ring.localScale = new Vector3(range, 1f, range);
                Overlays.Tint(_ringRenderer, Palette.RangeRing);
            }
        }

        public void Hide()
        {
            if (_root.gameObject.activeSelf) _root.gameObject.SetActive(false);
        }
    }
}
