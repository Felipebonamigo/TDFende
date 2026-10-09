using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace TDFende
{
    /// <summary>
    /// Tufos de grama 3D (Poly Haven, CC0) espalhados FORA das áreas de jogo.
    ///
    /// Dentro das lanes não entra tufo nenhum: é onde se lê o labirinto, as torres e os
    /// inimigos, e grama alta ali esconderia exatamente o que o jogador precisa ver.
    ///
    /// Cada FBX da Poly Haven é um MOSTRUÁRIO: várias touceiras diferentes (tiny, small,
    /// mid, large, tall) lado a lado ao longo de X, cada uma num nó. Cada nó vira uma
    /// variante independente, recentrada na própria base — desenhar o arquivo inteiro
    /// como um tufo põe uma fileira reta de 5 a 9 unidades no chão, repetida e girada.
    ///
    /// A densidade cai com a distância das lanes: é perto delas que a câmera olha, e o
    /// orçamento de triângulos rende mais ali. Longe, a textura do terreno basta — e a
    /// queda gradual não deixa uma borda reta onde a grama acaba.
    ///
    /// Desenho por instanciamento na GPU (Graphics.RenderMeshInstanced), sem GameObject
    /// por tufo: milhares de objetos custariam mais que o jogo inteiro.
    ///
    /// Não dá para saber quantos triângulos cada touceira tem antes de o Unity carregar o
    /// modelo, então a quantidade é decidida por ORÇAMENTO medido em runtime, e o número
    /// final vai para o log — se ficar pesado, é esse número que se ajusta.
    /// </summary>
    public class GrassField : MonoBehaviour
    {
        const string ArtPath = "Art/Grass/";
        static readonly string[] Kinds = { "grass_medium_01", "grass_medium_02" };

        const float TargetHeight = 0.42f;      // altura da MAIOR touceira de cada arquivo (~m)
        const float MinVisibleHeight = 0.15f;  // menor que isto some a 40 unidades da câmera
        const float LaneClearance = 0.4f;      // folga entre a borda da touceira e a da lane
        const float ScatterRadius = 80f;       // até onde espalhar em volta do centro
        const float Falloff = 16f;             // a cada 16 unidades da lane, densidade ÷ e
        const int MaxInstances = 20_000;
        const int TriangleBudget = 4_000_000;  // teto de triângulos de grama por quadro
        const int BatchSize = 1023;            // limite do RenderMeshInstanced por chamada

        struct Part
        {
            public Mesh Mesh;
            public int Submesh;
            public Matrix4x4 Base; // eixo do FBX + escala normalizada + base no chão
        }

        class Variant
        {
            public Part[] Parts;
            public float Radius; // meia-largura no chão, já na escala normalizada
            public int Triangles;
            public readonly List<Matrix4x4> Instances = new List<Matrix4x4>();
        }

        class Kind
        {
            public Variant[] Variants;
            public Material Material;
        }

        /// <summary>Um lote pronto para desenhar: as matrizes já são finais, calculadas uma vez.</summary>
        struct Draw
        {
            public int Params;
            public Mesh Mesh;
            public int Submesh;
            public Matrix4x4[] Matrices;
            public int Count;
        }

        readonly List<Kind> _kinds = new List<Kind>();
        readonly List<Draw> _draws = new List<Draw>();
        RenderParams[] _params;

        /// <summary>Triângulos de grama por quadro (todas as touceiras), para o orçamento da captura.</summary>
        public long Triangles { get; private set; }

        /// <summary>Materiais da grama: não estão em Renderer nenhum, a captura confere por aqui.</summary>
        public IEnumerable<Material> Materials
        {
            get { foreach (var k in _kinds) yield return k.Material; }
        }

        /// <summary>false se a arte não estiver presente — aí simplesmente não há grama 3D.</summary>
        public bool Init(Terrain terrain, IReadOnlyList<Rect> playAreas, int seed = 11)
        {
            // "-sem-grama": mede quanto a grama custa (Tools\Captura.ps1 -Extra -sem-grama)
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-sem-grama") >= 0) return false;
            var lit = ShaderRefs.Lit;
            if (lit == null) return false;

            foreach (var name in Kinds)
            {
                var prefab = ArtLayers.Load<GameObject>(ArtPath + name + "_1k");
                var tex = ArtLayers.Load<Texture2D>(ArtPath + name + "_diffalpha_1k");
                if (prefab == null || tex == null) continue;

                var variants = ExtractVariants(prefab);
                if (variants.Length == 0) continue;

                _kinds.Add(new Kind { Variants = variants, Material = MakeFoliageMaterial(lit, tex) });
            }
            if (_kinds.Count == 0)
            {
                Debug.LogWarning("[TDFende] modelos de grama não encontrados em Resources/Art/Grass.");
                return false;
            }

            Scatter(terrain, playAreas, seed);

            _params = new RenderParams[_kinds.Count];
            for (int k = 0; k < _kinds.Count; k++)
            {
                _params[k] = new RenderParams(_kinds[k].Material)
                {
                    // grama projetando sombra em 4 cascatas custaria mais que o resto do quadro;
                    // o contato com o chão vem do SSAO
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = new Bounds(Vector3.zero, Vector3.one * (ScatterRadius * 2f + 10f))
                };

                // a grama é estática: matriz final (instância × base da peça) calculada uma vez
                foreach (var v in _kinds[k].Variants)
                foreach (var part in v.Parts)
                {
                    var inst = v.Instances;
                    for (int start = 0; start < inst.Count; start += BatchSize)
                    {
                        int n = Mathf.Min(BatchSize, inst.Count - start);
                        var mats = new Matrix4x4[n];
                        for (int i = 0; i < n; i++) mats[i] = inst[start + i] * part.Base;
                        _draws.Add(new Draw
                        {
                            Params = k, Mesh = part.Mesh, Submesh = part.Submesh, Matrices = mats, Count = n
                        });
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Uma variante por nó com malha. Do nó fica a rotação/escala (o FBX do Blender traz
        /// a conversão de eixo ali) e sai a TRANSLAÇÃO, que é só a posição no mostruário.
        /// Todas as variantes do arquivo levam a MESMA escala — a da mais alta vira
        /// TargetHeight — para a tiny continuar menor que a tall.
        /// </summary>
        static Variant[] ExtractVariants(GameObject prefab)
        {
            var raw = new List<(Mesh mesh, Matrix4x4 m, Bounds b)>();
            float tallest = 0f;

            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var m = mf.transform.localToWorldMatrix; // hierarquia do asset, raiz na origem
                m.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));

                var b = new Bounds();
                bool first = true;
                foreach (var corner in Corners(mf.sharedMesh.bounds))
                {
                    var p = m.MultiplyPoint3x4(corner);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                    else b.Encapsulate(p);
                }
                raw.Add((mf.sharedMesh, m, b));
                tallest = Mathf.Max(tallest, b.size.y);
            }
            if (raw.Count == 0 || tallest <= 0.0001f) return new Variant[0];

            float s = TargetHeight / tallest;
            var variants = new List<Variant>();
            foreach (var (mesh, m, b) in raw)
            {
                // as "tiny" gastam orçamento e não aparecem da altura da câmera
                if (b.size.y * s < MinVisibleHeight) continue;

                // base da touceira no chão, centro no eixo de rotação da instância
                var normalize = Matrix4x4.TRS(
                    new Vector3(-b.center.x * s, -b.min.y * s, -b.center.z * s),
                    Quaternion.identity, Vector3.one * s);

                var parts = new Part[mesh.subMeshCount];
                int tris = 0;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    parts[sub] = new Part { Mesh = mesh, Submesh = sub, Base = normalize * m };
                    tris += (int)mesh.GetIndexCount(sub) / 3;
                }
                variants.Add(new Variant
                {
                    Parts = parts,
                    Radius = Mathf.Max(b.extents.x, b.extents.z) * s,
                    Triangles = Mathf.Max(1, tris)
                });
            }
            return variants.ToArray();
        }

        static IEnumerable<Vector3> Corners(Bounds b)
        {
            var mn = b.min;
            var mx = b.max;
            for (int i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? mn.x : mx.x,
                                         (i & 2) == 0 ? mn.y : mx.y,
                                         (i & 4) == 0 ? mn.z : mx.z);
        }

        /// <summary>URP Lit recortado por alfa e de dupla face — o jeito padrão de folhagem.</summary>
        static Material MakeFoliageMaterial(Shader lit, Texture2D tex)
        {
            var m = new Material(lit) { name = "Grama", enableInstancing = true };
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", new Color(1.15f, 1.35f, 0.95f)); // a foto é oliva-escuro: puxa para o verde vivo
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON"); // vale para todas as passadas, sombra e profundidade inclusas
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            // atalho: grama fora da passada de profundidade+normal do SSAO, que desenhava os
            // ~4 M triângulos de novo (sem contato escurecido na base do tufo). Teto: 4 M
            // triângulos de grama. Saída: a VIS-11 (chão novo, Fase 4) refaz a grama inteira.
            m.SetShaderPassEnabled("DepthNormals", false);
            return m;
        }

        void Scatter(Terrain terrain, IReadOnlyList<Rect> playAreas, int seed)
        {
            var rng = new Random(seed);

            // Sorteio com peso 1/triângulos: as três "large" do arquivo 01 são metade dos
            // triângulos do conjunto; sem peso, elas comem o orçamento e a grama sai rala.
            var pool = new List<Variant>();
            var cumulative = new List<float>();
            float totalWeight = 0f;
            foreach (var k in _kinds)
            foreach (var v in k.Variants)
            {
                totalWeight += 1f / v.Triangles;
                pool.Add(v);
                cumulative.Add(totalWeight);
            }

            float noiseOff = seed * 3.7f;
            int placed = 0, attempts = 0;
            long tris = 0;
            // a queda com a distância rejeita a maioria dos sorteios longe das lanes
            while (tris < TriangleBudget && placed < MaxInstances && attempts < MaxInstances * 60)
            {
                attempts++;
                float x = (float)(rng.NextDouble() * 2 - 1) * ScatterRadius;
                float z = (float)(rng.NextDouble() * 2 - 1) * ScatterRadius;

                float r = (float)rng.NextDouble() * totalWeight;
                int pick = 0;
                while (pick < pool.Count - 1 && r >= cumulative[pick]) pick++;
                var variant = pool[pick];
                float scale = 0.75f + (float)rng.NextDouble() * 0.6f;

                // a touceira inteira fora da lane, não só o centro dela
                float d = GroundBuilder.DistanceToAreas(playAreas, x, z);
                if (d < LaneClearance + variant.Radius * scale) continue;

                // em touceiras, não uniforme: grama de verdade nasce em manchas
                float n = Mathf.PerlinNoise(x * 0.11f + noiseOff, z * 0.11f + noiseOff);
                float keep = Mathf.Exp(-d / Falloff) * (0.25f + 0.75f * n);
                if (rng.NextDouble() > keep) continue;

                float y = terrain != null
                    ? terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y
                    : 0f;
                var rot = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                variant.Instances.Add(Matrix4x4.TRS(new Vector3(x, y, z), rot, Vector3.one * scale));
                placed++;
                tris += variant.Triangles;
            }

            Triangles = tris;
            Debug.Log($"[TDFende] grama 3D: {placed} touceiras de {pool.Count} variantes, " +
                      $"~{tris / 1_000_000.0:0.0} M triângulos (orçamento {TriangleBudget / 1_000_000} M).");
        }

        void Update()
        {
            for (int i = 0; i < _draws.Count; i++)
            {
                var d = _draws[i];
                Graphics.RenderMeshInstanced(_params[d.Params], d.Mesh, d.Submesh, d.Matrices, d.Count);
            }
        }
    }
}
