using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace TDFende
{
    /// <summary>
    /// Modelos de cenário baixados da Poly Haven (Tools/BaixarArte.ps1), em
    /// Assets/Resources/TDFende/Cenario/&lt;Categoria&gt;/. Categorias que o jogo usa:
    /// Arvores, Pedras, Tocos, Troncos, Barris, Caixas. Pasta vazia = procedural.
    ///
    /// Um FBX da Poly Haven pode ser um "mostruário" (várias pedras lado a lado) ou uma
    /// peça feita de várias malhas (árvore = tronco + copa). As malhas cujos contornos se
    /// sobrepõem no chão viram UMA variante; as que ficam separadas viram variantes
    /// diferentes. Assim a pedra do mostruário vem sozinha e a árvore vem inteira.
    /// </summary>
    public static class SceneryModels
    {
        public const string Folder = "TDFende/Cenario/";

        struct Piece
        {
            public Mesh Mesh;
            public Material[] Materials;
            public Vector3 Position;   // no espaço do prefab
            public Quaternion Rotation;
            public Vector3 Scale;
            public Bounds Bounds;      // já no espaço do prefab
        }

        struct Variant
        {
            public Piece[] Pieces;
            public Bounds Bounds;
        }

        static readonly Dictionary<string, Variant[]> Cache = new Dictionary<string, Variant[]>();

        public static bool Has(string category) => Variants(category).Length > 0;

        static Variant[] Variants(string category)
        {
            if (Cache.TryGetValue(category, out var v)) return v;
            var list = new List<Variant>();
            int rejected = 0;
            foreach (var prefab in Resources.LoadAll<GameObject>(Folder + category))
            {
                var pieces = new List<Piece>();
                bool usable = true;
                foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh == null || mr == null) continue;
                    if (!PrepareMaterials(mr.sharedMaterials)) usable = false;
                    var t = mf.transform;
                    var p = new Piece
                    {
                        Mesh = mf.sharedMesh, Materials = mr.sharedMaterials,
                        Position = t.position, Rotation = t.rotation, Scale = t.lossyScale,
                    };
                    p.Bounds = TransformBounds(mf.sharedMesh.bounds, p.Position, p.Rotation, p.Scale);
                    if (p.Bounds.size.magnitude > 0.0001f) pieces.Add(p);
                }
                // folha sem transparência vira placa sólida: melhor não usar este modelo
                if (!usable) { rejected++; continue; }
                list.AddRange(Group(pieces));
            }
            v = list.ToArray();
            Cache[category] = v;
            if (v.Length > 0 || rejected > 0)
                Debug.Log($"[TDFende] cenário {category}: {v.Length} variante(s)" +
                          (rejected > 0 ? $", {rejected} modelo(s) recusado(s) (folha sem transparência)" : ""));
            return v;
        }

        /// <summary>
        /// Folha, agulha e galho fino vêm como placa com recorte no canal alfa. O material
        /// importado do FBX não sabe disso: liga o recorte e as duas faces. Devolve false se
        /// um material de folhagem não tem alfa nenhum — aí não há como recortar.
        /// </summary>
        static bool PrepareMaterials(Material[] mats)
        {
            bool ok = true;
            foreach (var m in mats)
            {
                if (m == null) continue;
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : m.mainTexture;
                string names = (m.name + " " + (tex != null ? tex.name : "")).ToLowerInvariant();
                bool foliageName = names.Contains("leaf") || names.Contains("leaves") || names.Contains("needle")
                                   || names.Contains("foliage") || names.Contains("twig") || names.Contains("frond");
                bool hasAlpha = tex is Texture2D t2 && GraphicsFormatUtility.HasAlphaChannel(t2.graphicsFormat);

                if (hasAlpha && (foliageName || tex.name.ToLowerInvariant().Contains("alpha")))
                {
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", 0.45f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", (float)CullMode.Off);
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                    m.enableInstancing = true;
                }
                else if (foliageName && !hasAlpha) ok = false;
                else m.enableInstancing = true;
            }
            return ok;
        }

        /// <summary>Agrupa peças cujo contorno no chão (XZ) se sobrepõe: tronco + copa = uma árvore.</summary>
        static IEnumerable<Variant> Group(List<Piece> pieces)
        {
            int n = pieces.Count;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            int Find(int i) => parent[i] == i ? i : (parent[i] = Find(parent[i]));
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                Bounds a = pieces[i].Bounds, b = pieces[j].Bounds;
                bool overlap = a.min.x <= b.max.x && b.min.x <= a.max.x && a.min.z <= b.max.z && b.min.z <= a.max.z;
                if (overlap) parent[Find(i)] = Find(j);
            }
            var groups = new Dictionary<int, List<Piece>>();
            for (int i = 0; i < n; i++)
            {
                int r = Find(i);
                if (!groups.TryGetValue(r, out var g)) groups[r] = g = new List<Piece>();
                g.Add(pieces[i]);
            }
            foreach (var g in groups.Values)
            {
                var bounds = g[0].Bounds;
                foreach (var p in g) bounds.Encapsulate(p.Bounds);
                yield return new Variant { Pieces = g.ToArray(), Bounds = bounds };
            }
        }

        static Bounds TransformBounds(Bounds b, Vector3 pos, Quaternion rot, Vector3 scl)
        {
            var result = new Bounds();
            bool first = true;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = pos + rot * Vector3.Scale(c, scl);
                if (first) { result = new Bounds(p, Vector3.zero); first = false; }
                else result.Encapsulate(p);
            }
            return result;
        }

        /// <summary>
        /// Põe uma variante sorteada da categoria em <paramref name="pos"/>, com a maior
        /// medida horizontal igual a <paramref name="size"/> (pedra, caixa) — ou a altura,
        /// se <paramref name="byHeight"/> (árvore, toco, barril). Null se a categoria está vazia.
        /// </summary>
        public static GameObject Place(string category, Transform parent, Vector3 pos, float size, int seed,
            bool byHeight = false)
        {
            var all = Variants(category);
            if (all.Length == 0) return null;
            var v = all[Mathf.Abs(seed * 7919) % all.Length];

            float measure = byHeight ? v.Bounds.size.y : Mathf.Max(v.Bounds.size.x, v.Bounds.size.z);
            float k = measure > 0.0001f ? size / measure : 1f;
            // base do grupo no chão, centro no ponto pedido; enterra 3% para não flutuar
            var anchor = new Vector3(v.Bounds.center.x, v.Bounds.min.y + v.Bounds.size.y * 0.03f, v.Bounds.center.z);

            var go = new GameObject(category);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, ProcNoise.Hash(seed, 3, 17) * 360f, 0f);

            foreach (var p in v.Pieces)
            {
                var piece = new GameObject("Peca");
                piece.transform.SetParent(go.transform, false);
                piece.transform.localPosition = (p.Position - anchor) * k;
                piece.transform.localRotation = p.Rotation;
                piece.transform.localScale = p.Scale * k;
                piece.AddComponent<MeshFilter>().sharedMesh = p.Mesh;
                piece.AddComponent<MeshRenderer>().sharedMaterials = p.Materials;
            }
            return go;
        }
    }
}
