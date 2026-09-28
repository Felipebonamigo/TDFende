using System.Collections.Generic;
using UnityEngine;

namespace TDFende
{
    /// <summary>
    /// Modelos de cenário baixados da Poly Haven (Tools/BaixarArte.ps1), em
    /// Assets/Resources/TDFende/Cenario/&lt;Categoria&gt;/. Categorias que o jogo usa:
    /// Pedras, Tocos, Troncos, Barris, Caixas. Pasta vazia = o item continua procedural.
    ///
    /// Muito FBX da Poly Haven é um "mostruário": várias pedras lado a lado num arquivo
    /// só. Cada peça com malha vira uma variante própria, recentrada na sua base — senão
    /// uma fileira de cinco pedras apareceria como UMA pedra comprida no chão.
    /// </summary>
    public static class SceneryModels
    {
        public const string Folder = "TDFende/Cenario/";

        struct Variant
        {
            public Mesh Mesh;
            public Material[] Materials;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Bounds LocalBounds; // já girado e escalado, antes da escala do jogo
        }

        static readonly Dictionary<string, Variant[]> Cache = new Dictionary<string, Variant[]>();

        public static bool Has(string category) => Variants(category).Length > 0;

        static Variant[] Variants(string category)
        {
            if (Cache.TryGetValue(category, out var v)) return v;
            var list = new List<Variant>();
            foreach (var prefab in Resources.LoadAll<GameObject>(Folder + category))
            {
                foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh == null || mr == null) continue;
                    // rotação/escala do nó (o FBX do Blender traz a conversão de eixo ali);
                    // a translação é só a posição no mostruário e fica de fora
                    var rot = mf.transform.rotation;
                    var scl = mf.transform.lossyScale;
                    var b = TransformBounds(mf.sharedMesh.bounds, rot, scl);
                    if (b.size.magnitude < 0.001f) continue;
                    list.Add(new Variant
                    {
                        Mesh = mf.sharedMesh, Materials = mr.sharedMaterials,
                        Rotation = rot, Scale = scl, LocalBounds = b
                    });
                }
            }
            v = list.ToArray();
            Cache[category] = v;
            if (v.Length > 0) Debug.Log($"[TDFende] cenário: {v.Length} variante(s) em {category}");
            return v;
        }

        static Bounds TransformBounds(Bounds b, Quaternion rot, Vector3 scl)
        {
            var result = new Bounds();
            bool first = true;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3(
                    (i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = rot * Vector3.Scale(c, scl);
                if (first) { result = new Bounds(p, Vector3.zero); first = false; }
                else result.Encapsulate(p);
            }
            return result;
        }

        /// <summary>
        /// Põe uma variante sorteada da categoria em <paramref name="pos"/>, com a maior
        /// medida horizontal igual a <paramref name="size"/> (pedra, caixa) — ou a altura,
        /// se <paramref name="byHeight"/> (toco, barril). Devolve null se a categoria está vazia.
        /// </summary>
        public static GameObject Place(string category, Transform parent, Vector3 pos, float size, int seed,
            bool byHeight = false)
        {
            var all = Variants(category);
            if (all.Length == 0) return null;
            var v = all[Mathf.Abs(seed * 7919) % all.Length];

            float measure = byHeight ? v.LocalBounds.size.y : Mathf.Max(v.LocalBounds.size.x, v.LocalBounds.size.z);
            float k = measure > 0.0001f ? size / measure : 1f;

            var go = new GameObject(category);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, ProcNoise.Hash(seed, 3, 17) * 360f, 0f);

            var piece = new GameObject("Peca");
            piece.transform.SetParent(go.transform, false);
            piece.transform.localRotation = v.Rotation;
            piece.transform.localScale = v.Scale * k;
            // base da peça no chão e centro no ponto pedido (enterra 3% para não flutuar)
            var c = v.LocalBounds.center * k;
            piece.transform.localPosition = new Vector3(-c.x, -v.LocalBounds.min.y * k - size * 0.03f, -c.z);

            piece.AddComponent<MeshFilter>().sharedMesh = v.Mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterials = v.Materials;
            return go;
        }
    }
}
